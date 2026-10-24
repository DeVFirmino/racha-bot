using Microsoft.Extensions.Options;
using PeladaBot.Configuracao;

namespace PeladaBot.Acesso;

/// <summary>As duas janelas de resposta em memória; não thread-safe.</summary>
public sealed class LimiteDeRespostas
{
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<OpcoesDoBot> _opcoes;

    // Timestamp monotônico da última resposta, por escopo: uma mudança no relógio do servidor não abre nem fecha uma janela.
    private readonly Dictionary<(string IdDoGrupo, string IdDoRemetente), long> _ultimasRespostasNaoPresidente = new();
    private readonly Dictionary<string, long> _ultimasCobrancas = new(StringComparer.Ordinal);
    private readonly Dictionary<(string IdDoGrupo, string IdDoRemetente), long> _ultimasAjudas = new();

    public LimiteDeRespostas(TimeProvider timeProvider, IOptions<OpcoesDoBot> opcoes)
    {
        _timeProvider = timeProvider;
        _opcoes = opcoes;
    }

    private OpcoesDeLimites Limites => _opcoes.Value.Limites;

    /// <summary>True quando esse remetente não recebeu recusa nesse grupo dentro da janela.</summary>
    public bool PodeResponderNaoPresidente(string idDoGrupo, string idDoRemetente)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);
        ArgumentNullException.ThrowIfNull(idDoRemetente);

        return _ultimasRespostasNaoPresidente.TryGetValue((idDoGrupo, idDoRemetente), out long ultimaResposta) is false
            || SaiuDaJanela(ultimaResposta, Limites.JanelaDeRespostaNaoPresidente);
    }

    /// <summary>Inicia a janela desse remetente; chame só depois que a recusa foi realmente enviada.</summary>
    public void RegistrarRespostaNaoPresidenteEnviada(string idDoGrupo, string idDoRemetente)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);
        ArgumentNullException.ThrowIfNull(idDoRemetente);

        _ultimasRespostasNaoPresidente[(idDoGrupo, idDoRemetente)] = _timeProvider.GetTimestamp();
    }

    /// <summary>
    /// True quando esse integrante não recebeu o !ajuda nesse grupo dentro da janela de recusa. A janela é dele e só do
    /// !ajuda: uma recusa ou uma resenha de espera de outro comando não a gasta.
    /// </summary>
    public bool PodeResponderAjuda(string idDoGrupo, string idDoRemetente)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);
        ArgumentNullException.ThrowIfNull(idDoRemetente);

        return _ultimasAjudas.TryGetValue((idDoGrupo, idDoRemetente), out long ultimaAjuda) is false
            || SaiuDaJanela(ultimaAjuda, Limites.JanelaDeRespostaNaoPresidente);
    }

    /// <summary>Inicia a janela de !ajuda desse integrante; chame só depois que o guia foi realmente enviado.</summary>
    public void RegistrarAjudaEnviada(string idDoGrupo, string idDoRemetente)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);
        ArgumentNullException.ThrowIfNull(idDoRemetente);

        _ultimasAjudas[(idDoGrupo, idDoRemetente)] = _timeProvider.GetTimestamp();
    }

    /// <summary>True quando o grupo não recebeu cobrança, de ninguém, dentro da janela de cobrança.</summary>
    public bool PodeCobrar(string idDoGrupo) => TempoAteAProximaCobranca(idDoGrupo) == TimeSpan.Zero;

    /// <summary>Quanto falta para a janela de cobrança do grupo fechar; zero quando ela já fechou ou nunca abriu.</summary>
    public TimeSpan TempoAteAProximaCobranca(string idDoGrupo)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);

        if (_ultimasCobrancas.TryGetValue(idDoGrupo, out long ultimaCobranca) is false)
        {
            return TimeSpan.Zero;
        }

        TimeSpan restante = Limites.JanelaDeCobranca - _timeProvider.GetElapsedTime(ultimaCobranca);

        return restante > TimeSpan.Zero ? restante : TimeSpan.Zero;
    }

    /// <summary>Inicia a janela de cobrança do grupo; chame após toda cobrança enviada, presidência incluída.</summary>
    public void RegistrarCobrancaEnviada(string idDoGrupo)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);

        _ultimasCobrancas[idDoGrupo] = _timeProvider.GetTimestamp();
    }

    private bool SaiuDaJanela(long timestampDaResposta, TimeSpan janela) =>
        _timeProvider.GetElapsedTime(timestampDaResposta) >= janela;
}
