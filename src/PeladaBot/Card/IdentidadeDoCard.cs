namespace PeladaBot.Card;

/// <summary>O nome e o escudo no topo do card. Sem configuração, "⚽ PELADA ⚽" e nenhum escudo.</summary>
public sealed record IdentidadeDoCard
{
    public static IdentidadeDoCard Padrao { get; } = new() { Titulo = "⚽ PELADA ⚽", Escudo = null };

    public required string Titulo { get; init; }

    /// <summary>A imagem do escudo (PNG ou JPEG), desenhada num círculo ao lado do título; null sem escudo.</summary>
    public required byte[]? Escudo { get; init; }
}
