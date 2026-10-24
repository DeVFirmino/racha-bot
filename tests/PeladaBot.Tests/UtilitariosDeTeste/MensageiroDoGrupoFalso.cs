using System.Collections.Concurrent;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Faz as vezes do grupo do WhatsApp: registra toda chamada com a hora do relógio, em ordem, e responde cada envio com o
/// próximo resultado programado, ou <see cref="Enviado"/>. Toda chamada termina na hora, então a única espera em uma
/// resposta é a pausa no relógio.
/// </summary>
public sealed class MensageiroDoGrupoFalso : IMensageiroDoGrupo
{
    /// <summary>A pausa antes de cada resposta vai de 1,5 a 3 s: um avanço desses sempre a encerra.</summary>
    private static readonly TimeSpan _pausaMaxima = TimeSpan.FromSeconds(3);

    private static readonly TimeSpan _tempoLimiteReal = TimeSpan.FromSeconds(10);

    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentQueue<ChamadaDoMensageiro> _chamadas = new();
    private readonly ConcurrentQueue<ResultadoDoEnvio> _resultadosDeTexto = new();
    private readonly ConcurrentQueue<ResultadoDoEnvio> _resultadosDeImagem = new();
    private readonly AvisoDeMudanca _aviso = new();

    public MensageiroDoGrupoFalso(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Quando true, "digitando..." falha, como um erro do WAHA faria.</summary>
    public bool DigitacaoFalha { get; set; }

    public IReadOnlyList<ChamadaDoMensageiro> Chamadas => [.. _chamadas];

    /// <summary>Os textos e imagens que foram para o grupo, sem a digitação.</summary>
    public IReadOnlyList<ChamadaDoMensageiro> Mensagens =>
        [.. _chamadas.Where(chamada => chamada.Tipo is TipoDeChamadaDoMensageiro.Texto or TipoDeChamadaDoMensageiro.Imagem)];

    /// <summary>Os resultados dos próximos envios de texto, em ordem. Envios além deles são <see cref="Enviado"/>.</summary>
    public MensageiroDoGrupoFalso ResponderTextos(params ResultadoDoEnvio[] resultados)
    {
        foreach (ResultadoDoEnvio resultado in resultados)
        {
            _resultadosDeTexto.Enqueue(resultado);
        }

        return this;
    }

    /// <summary>Os resultados dos próximos envios de imagem, em ordem. Envios além deles são <see cref="Enviado"/>.</summary>
    public MensageiroDoGrupoFalso ResponderImagens(params ResultadoDoEnvio[] resultados)
    {
        foreach (ResultadoDoEnvio resultado in resultados)
        {
            _resultadosDeImagem.Enqueue(resultado);
        }

        return this;
    }

    /// <summary>
    /// Espera <paramref name="quantidade"/> mensagens irem para o grupo, avançando <paramref name="relogio"/> só quando o
    /// consumidor, na sua própria thread, para na pausa antes de responder. Acorda a cada chamada registrada e a cada
    /// timer criado; nunca dorme em tempo real.
    /// </summary>
    public async Task AvancarAteMensagensAsync(RelogioDeTeste relogio, int quantidade)
    {
        ArgumentNullException.ThrowIfNull(relogio);

        relogio.TimerCriado += _aviso.Avisar;

        try
        {
            using CancellationTokenSource prazo = new(_tempoLimiteReal);

            while (Mensagens.Count < quantidade)
            {
                if (relogio.AlguemComecouAEsperar())
                {
                    relogio.Advance(_pausaMaxima);

                    continue;
                }

                await _aviso.EsperarProximoAsync(prazo.Token);
            }
        }
        finally
        {
            relogio.TimerCriado -= _aviso.Avisar;
        }
    }

    public Task<ResultadoDoEnvio> EnviarTextoAsync(string idDoChat, string texto, string idDaMensagemRespondida, CancellationToken cancellationToken)
    {
        Registrar(TipoDeChamadaDoMensageiro.Texto, idDoChat, texto, png: null, idDaMensagemRespondida);

        return Task.FromResult(_resultadosDeTexto.TryDequeue(out ResultadoDoEnvio? resultado) ? resultado : new Enviado());
    }

    public Task<ResultadoDoEnvio> EnviarImagemAsync(
        string idDoChat, byte[] png, string? legenda, string idDaMensagemRespondida, CancellationToken cancellationToken)
    {
        Registrar(TipoDeChamadaDoMensageiro.Imagem, idDoChat, legenda, png, idDaMensagemRespondida);

        return Task.FromResult(_resultadosDeImagem.TryDequeue(out ResultadoDoEnvio? resultado) ? resultado : new Enviado());
    }

    public Task<bool> IniciarDigitandoAsync(string idDoChat, CancellationToken cancellationToken)
    {
        Registrar(TipoDeChamadaDoMensageiro.IniciarDigitacao, idDoChat, texto: null, png: null, idDaMensagemRespondida: null);

        return Task.FromResult(DigitacaoFalha is false);
    }

    public Task<bool> PararDigitandoAsync(string idDoChat, CancellationToken cancellationToken)
    {
        Registrar(TipoDeChamadaDoMensageiro.PararDigitacao, idDoChat, texto: null, png: null, idDaMensagemRespondida: null);

        return Task.FromResult(DigitacaoFalha is false);
    }

    private void Registrar(TipoDeChamadaDoMensageiro tipo, string idDoChat, string? texto, byte[]? png, string? idDaMensagemRespondida)
    {
        _chamadas.Enqueue(new ChamadaDoMensageiro
        {
            Tipo = tipo,
            IdDoChat = idDoChat,
            Texto = texto,
            Png = png,
            IdDaMensagemRespondida = idDaMensagemRespondida,
            Em = _timeProvider.GetUtcNow(),
        });
        _aviso.Avisar();
    }
}
