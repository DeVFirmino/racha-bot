using PeladaBot.Acesso;
using PeladaBot.Comandos;
using PeladaBot.Configuracao;
using PeladaBot.WhatsApp;

namespace PeladaBot.Mensageria;

/// <summary>
/// Envia toda resposta do mesmo jeito cuidadoso: primeiro uma reserva no disjuntor, depois "digitando..."
/// e uma pausa aleatória, depois a mensagem, citando o comando. Registra um envio com
/// <see cref="ControleDeAcesso"/>.
/// </summary>
public sealed partial class EnviadorDeRespostas
{
    /// <summary>A pausa antes de cada resposta é no mínimo isso...</summary>
    public static readonly TimeSpan PausaMaisCurta = TimeSpan.FromMilliseconds(1500);

    /// <summary>...e no máximo isso, escolhido ao acaso a cada vez.</summary>
    public static readonly TimeSpan PausaMaisLonga = TimeSpan.FromMilliseconds(3000);

    private readonly IMensageiroDoGrupo _mensageiro;
    private readonly DisjuntorDeEnvio _disjuntor;
    private readonly ControleDeAcesso _controle;
    private readonly TimeProvider _timeProvider;
    private readonly Random _random;
    private readonly ILogger<EnviadorDeRespostas> _logger;

    public EnviadorDeRespostas(
        IMensageiroDoGrupo mensageiro,
        DisjuntorDeEnvio disjuntor,
        ControleDeAcesso controle,
        TimeProvider timeProvider,
        Random random,
        ILogger<EnviadorDeRespostas> logger)
    {
        _mensageiro = mensageiro;
        _disjuntor = disjuntor;
        _controle = controle;
        _timeProvider = timeProvider;
        _random = random;
        _logger = logger;
    }

    /// <summary>Envia <paramref name="texto"/> como resposta; null quando o disjuntor a reteve (logado).</summary>
    public Task<ResultadoDoEnvio?> EnviarTextoAsync(DestinoDaResposta destino, string texto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destino);
        ArgumentNullException.ThrowIfNull(texto);

        return EnviarAsync(
            destino,
            TipoDeResposta.Texto,
            token => _mensageiro.EnviarTextoAsync(destino.IdDoGrupo, texto, destino.IdDaMensagem, token),
            cancellationToken);
    }

    /// <summary>
    /// Envia a imagem com <paramref name="legenda"/> abaixo como uma única resposta, ou sem legenda quando ela é null;
    /// null quando retida.
    /// </summary>
    public Task<ResultadoDoEnvio?> EnviarImagemAsync(
        DestinoDaResposta destino, byte[] png, string? legenda, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destino);
        ArgumentNullException.ThrowIfNull(png);

        return EnviarAsync(
            destino,
            TipoDeResposta.Imagem,
            token => _mensageiro.EnviarImagemAsync(destino.IdDoGrupo, png, legenda, destino.IdDaMensagem, token),
            cancellationToken);
    }

    private async Task<ResultadoDoEnvio?> EnviarAsync(
        DestinoDaResposta destino,
        TipoDeResposta tipo,
        Func<CancellationToken, Task<ResultadoDoEnvio>> enviar,
        CancellationToken cancellationToken)
    {
        string idDaMensagem = MascaraDeId.AplicarNaMensagem(destino.IdDaMensagem);

        if (_disjuntor.TentarReservar() is false)
        {
            RegistrarRetidoPeloDisjuntor(_logger, tipo, destino.Comando, idDaMensagem);

            return null;
        }

        // A pausa começa antes de "digitando..." aparecer, para uma chamada de digitação lenta não a alongar.
        Task pausa = Task.Delay(ProximaPausa(), _timeProvider, cancellationToken);

        if (await _mensageiro.IniciarDigitandoAsync(destino.IdDoGrupo, cancellationToken) is false)
        {
            RegistrarDigitacaoFalhou(_logger, destino.Comando, idDaMensagem);
        }

        await pausa;
        await _mensageiro.PararDigitandoAsync(destino.IdDoGrupo, cancellationToken);

        ResultadoDoEnvio resultado = await enviar(cancellationToken);

        switch (resultado)
        {
            case Enviado:
                _controle.RegistrarRespostaEnviada(destino.IdDoGrupo, destino.IdDoRemetente, destino.Comando);
                RegistrarEnviado(_logger, tipo, destino.Comando, idDaMensagem);
                break;
            case Incerto incerto:
                _controle.RegistrarRespostaEnviada(destino.IdDoGrupo, destino.IdDoRemetente, destino.Comando);
                RegistrarIncerto(_logger, tipo, destino.Comando, idDaMensagem, incerto.Motivo);
                break;
            case Rejeitado rejeitado:
                RegistrarRejeitado(_logger, tipo, destino.Comando, idDaMensagem, rejeitado.Motivo);
                break;
            default:
                throw new InvalidOperationException($"Um resultado de envio sem tratamento: {resultado.GetType().Name}.");
        }

        return resultado;
    }

    private TimeSpan ProximaPausa()
    {
        int maisCurtaMs = (int)PausaMaisCurta.TotalMilliseconds;
        int maisLongaMs = (int)PausaMaisLonga.TotalMilliseconds;

        return TimeSpan.FromMilliseconds(_random.Next(maisCurtaMs, maisLongaMs + 1));
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Disjuntor cheio: a resposta de {Tipo} a {Comando} {IdDaMensagem} não foi enviada; o bot tenta "
            + "de novo quando a mensagem mais antiga sair da janela")]
    private static partial void RegistrarRetidoPeloDisjuntor(
        ILogger logger, TipoDeResposta tipo, TipoDeComando comando, string idDaMensagem);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Não foi possível mostrar digitando antes da resposta a {Comando} {IdDaMensagem}; a resposta segue")]
    private static partial void RegistrarDigitacaoFalhou(ILogger logger, TipoDeComando comando, string idDaMensagem);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resposta de {Tipo} a {Comando} {IdDaMensagem} enviada")]
    private static partial void RegistrarEnviado(
        ILogger logger, TipoDeResposta tipo, TipoDeComando comando, string idDaMensagem);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Resposta de {Tipo} a {Comando} {IdDaMensagem} pode ou não ter saído: {Motivo}")]
    private static partial void RegistrarIncerto(
        ILogger logger, TipoDeResposta tipo, TipoDeComando comando, string idDaMensagem, string motivo);

    [LoggerMessage(Level = LogLevel.Error, Message = "Resposta de {Tipo} a {Comando} {IdDaMensagem} não enviada: {Motivo}")]
    private static partial void RegistrarRejeitado(
        ILogger logger, TipoDeResposta tipo, TipoDeComando comando, string idDaMensagem, string motivo);
}
