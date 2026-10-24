using PeladaBot.Comandos;
using PeladaBot.Configuracao;
using PeladaBot.Respostas;

namespace PeladaBot.Acesso;

/// <summary>Primeira verificação de acesso de todo caso de uso: só a presidência, ou limitada.</summary>
public sealed partial class ControleDeAcesso
{
    private readonly Presidencia _presidencia;
    private readonly LimiteDeRespostas _limiteDeRespostas;
    private readonly Random _random;
    private readonly ILogger<ControleDeAcesso> _logger;

    public ControleDeAcesso(
        Presidencia presidencia, LimiteDeRespostas limiteDeRespostas, Random random, ILogger<ControleDeAcesso> logger)
    {
        _presidencia = presidencia;
        _limiteDeRespostas = limiteDeRespostas;
        _random = random;
        _logger = logger;
    }

    /// <summary>Decide se prossegue, é recusado, ou fica em silêncio (loga o id mascarado do remetente).</summary>
    public Admissao Verificar(string idDoGrupo, string idDoRemetente, TipoDeComando comando)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);
        ArgumentNullException.ThrowIfNull(idDoRemetente);

        return comando switch
        {
            TipoDeComando.Sortear => VerificarComandoDaPresidencia(idDoGrupo, idDoRemetente, comando),
            TipoDeComando.Ajuda => VerificarAjuda(idDoGrupo, idDoRemetente),
            TipoDeComando.Cobrar => VerificarCobranca(idDoGrupo, idDoRemetente),
            _ => throw new ArgumentOutOfRangeException(nameof(comando), comando, "Comando desconhecido."),
        };
    }

    /// <summary>Inicia a janela certa; chame só depois que uma resposta saiu, nunca uma que falhou ou foi retida.</summary>
    public void RegistrarRespostaEnviada(string idDoGrupo, string idDoRemetente, TipoDeComando comando)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);
        ArgumentNullException.ThrowIfNull(idDoRemetente);

        switch (comando)
        {
            case TipoDeComando.Sortear:
                RegistrarRecusaEnviada(idDoGrupo, idDoRemetente);
                break;
            case TipoDeComando.Ajuda:
                if (_presidencia.Inclui(idDoGrupo, idDoRemetente) is false)
                {
                    _limiteDeRespostas.RegistrarAjudaEnviada(idDoGrupo, idDoRemetente);
                }

                break;
            case TipoDeComando.Cobrar:
                RegistrarRespostaDeCobrancaEnviada(idDoGrupo, idDoRemetente);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(comando), comando, "Comando desconhecido.");
        }
    }

    private Admissao VerificarComandoDaPresidencia(string idDoGrupo, string idDoRemetente, TipoDeComando comando)
    {
        if (_presidencia.Inclui(idDoGrupo, idDoRemetente))
        {
            return new Permitido();
        }

        if (_limiteDeRespostas.PodeResponderNaoPresidente(idDoGrupo, idDoRemetente) is false)
        {
            RegistrarRecusaJaEnviada(_logger, comando, MascaraDeId.Aplicar(idDoRemetente));

            return new Ignorado();
        }

        RegistrarRecusa(_logger, comando, MascaraDeId.Aplicar(idDoRemetente));

        return new Recusado { Resposta = RespostasDeErro.SoOPresidentePodeSortear };
    }

    /// <summary>
    /// A presidência pede ajuda a qualquer hora; os outros, uma vez por janela de recusa cada um, numa janela só do
    /// !ajuda, e no resto dela o bot fica em silêncio.
    /// </summary>
    private Admissao VerificarAjuda(string idDoGrupo, string idDoRemetente)
    {
        if (_presidencia.Inclui(idDoGrupo, idDoRemetente) || _limiteDeRespostas.PodeResponderAjuda(idDoGrupo, idDoRemetente))
        {
            return new Permitido();
        }

        RegistrarRecusaJaEnviada(_logger, TipoDeComando.Ajuda, MascaraDeId.Aplicar(idDoRemetente));

        return new Ignorado();
    }

    /// <summary>
    /// A presidência cobra a qualquer hora. Os outros cobram uma vez por janela no grupo; dentro dela, recebem uma
    /// resenha dizendo quanto falta, uma vez por janela de recusa, e depois o bot fica em silêncio.
    /// </summary>
    private Admissao VerificarCobranca(string idDoGrupo, string idDoRemetente)
    {
        if (_presidencia.Inclui(idDoGrupo, idDoRemetente) || _limiteDeRespostas.PodeCobrar(idDoGrupo))
        {
            return new Permitido();
        }

        if (_limiteDeRespostas.PodeResponderNaoPresidente(idDoGrupo, idDoRemetente) is false)
        {
            RegistrarCobrancaJaRespondida(_logger, MascaraDeId.Aplicar(idDoRemetente));

            return new Ignorado();
        }

        RegistrarCobrancaRecente(_logger, MascaraDeId.Aplicar(idDoRemetente));

        return new Recusado { Resposta = RespostaDeCobrancaRecente.Compor(_limiteDeRespostas.TempoAteAProximaCobranca(idDoGrupo), _random) };
    }

    /// <summary>
    /// Com a janela de cobrança fechada, o que saiu foi a cobrança, e ela abre a janela; com ela aberta, só pode ter
    /// saído a resenha de "calma" a quem está fora da presidência, que abre a janela de recusa dessa pessoa.
    /// </summary>
    private void RegistrarRespostaDeCobrancaEnviada(string idDoGrupo, string idDoRemetente)
    {
        if (_presidencia.Inclui(idDoGrupo, idDoRemetente) || _limiteDeRespostas.PodeCobrar(idDoGrupo))
        {
            _limiteDeRespostas.RegistrarCobrancaEnviada(idDoGrupo);
        }
        else
        {
            _limiteDeRespostas.RegistrarRespostaNaoPresidenteEnviada(idDoGrupo, idDoRemetente);
        }
    }

    /// <summary>A presidência não tem janela; só uma recusa a quem está fora dela inicia uma.</summary>
    private void RegistrarRecusaEnviada(string idDoGrupo, string idDoRemetente)
    {
        if (_presidencia.Inclui(idDoGrupo, idDoRemetente) is false)
        {
            _limiteDeRespostas.RegistrarRespostaNaoPresidenteEnviada(idDoGrupo, idDoRemetente);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Comando} de {IdDoRemetente}: não é presidência, recusado")]
    private static partial void RegistrarRecusa(ILogger logger, TipoDeComando comando, string idDoRemetente);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Comando} de {IdDoRemetente}: ignorado, ainda na janela")]
    private static partial void RegistrarRecusaJaEnviada(ILogger logger, TipoDeComando comando, string idDoRemetente);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cobrar de {IdDoRemetente}: cobrança recente no grupo, resenha de espera")]
    private static partial void RegistrarCobrancaRecente(ILogger logger, string idDoRemetente);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cobrar de {IdDoRemetente}: ignorado, ainda na janela")]
    private static partial void RegistrarCobrancaJaRespondida(ILogger logger, string idDoRemetente);
}
