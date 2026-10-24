using Microsoft.Extensions.Options;

namespace PeladaBot.Configuracao;

/// <summary>Registra a configuração em uma linha na inicialização, ids mascarados e nomes omitidos.</summary>
public sealed partial class RegistroDasOpcoesDoBot : IHostedService
{
    private readonly IOptions<OpcoesDoBot> _opcoes;
    private readonly ILogger<RegistroDasOpcoesDoBot> _logger;

    public RegistroDasOpcoesDoBot(IOptions<OpcoesDoBot> opcoes, ILogger<RegistroDasOpcoesDoBot> logger)
    {
        _opcoes = opcoes;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        OpcoesDoBot opcoes = _opcoes.Value;
        OpcoesDoGrupo grupo = opcoes.Grupos.Single();
        OpcoesDeLimites limites = opcoes.Limites;

        RegistrarConfiguracao(
            _logger,
            MascaraDeId.Aplicar(grupo.Id),
            DescreverPresidencia(grupo.Presidencia),
            grupo.FrasesDoRodape.Count,
            limites.JanelaDeRespostaNaoPresidente,
            limites.JanelaDeCobranca,
            limites.MaximoDeMensagensDoDisjuntor,
            limites.JanelaDoDisjuntor,
            limites.IdadeMaximaDoComando,
            limites.IdadeMaximaDaLista);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Um colchete por pessoa com ids mascarados, ex.: "[***0101@c.us, ***0202@lid] [***0303@c.us]".</summary>
    private static string DescreverPresidencia(IReadOnlyList<OpcoesDoMembroDaPresidencia> presidencia) =>
        string.Join(' ', presidencia.Select(membro => $"[{string.Join(", ", membro.Ids.Select(MascaraDeId.Aplicar))}]"));

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Bot configurado para o grupo {IdDoGrupo}, presidência {Presidencia}, {QuantidadeDeFrasesDoRodape} "
            + "frases de rodapé; limites: janela de resposta não presidente {JanelaDeRespostaNaoPresidente}, "
            + "janela de cobrança {JanelaDeCobranca}, "
            + "disjuntor {MaximoDeMensagensDoDisjuntor} mensagens por {JanelaDoDisjuntor}, "
            + "idade máxima do comando {IdadeMaximaDoComando}, idade máxima da lista {IdadeMaximaDaLista}")]
    private static partial void RegistrarConfiguracao(
        ILogger logger,
        string idDoGrupo,
        string presidencia,
        int quantidadeDeFrasesDoRodape,
        TimeSpan janelaDeRespostaNaoPresidente,
        TimeSpan janelaDeCobranca,
        int maximoDeMensagensDoDisjuntor,
        TimeSpan janelaDoDisjuntor,
        TimeSpan idadeMaximaDoComando,
        TimeSpan idadeMaximaDaLista);
}
