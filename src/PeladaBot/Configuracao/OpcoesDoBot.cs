namespace PeladaBot.Configuracao;

/// <summary>A seção de configuração "Bot": grupos, presidência e limites.</summary>
public sealed class OpcoesDoBot
{
    public const string SectionName = "Bot";

    /// <summary>O fuso IANA em que a legenda mostra a hora, como "America/Sao_Paulo".</summary>
    public string Fuso { get; init; } = "America/Sao_Paulo";

    /// <summary>Uma seção por grupo do WhatsApp. A versão 1 roda em exatamente um grupo.</summary>
    public IReadOnlyList<OpcoesDoGrupo> Grupos { get; init; } = [];

    /// <summary>Limites globais, iguais para todo grupo.</summary>
    public OpcoesDeLimites Limites { get; init; } = new();
}
