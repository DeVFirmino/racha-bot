using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>
/// Tudo que o desenhista precisa para desenhar um card da escalação, já medido e posicionado por
/// <see cref="LayoutDoCard"/>: o cabeçalho e um campo. O fundo é um gramado.
/// </summary>
public sealed record CardDoTime
{
    public required int Largura { get; init; }

    public required int Altura { get; init; }

    /// <summary>O escudo do grupo, no meio do cabeçalho; null quando o grupo não configurou um.</summary>
    public required EscudoDoCard? Escudo { get; init; }

    /// <summary>O nome do grupo no lugar do escudo, quando não há escudo; null quando há (o escudo já traz o nome).</summary>
    public required TextoDoCard? NomeDoGrupo { get; init; }

    /// <summary>"ESCALAÇÕES".</summary>
    public required TextoDoCard Titulo { get; init; }

    /// <summary>O campo do confronto, com o placar: um ou dois times.</summary>
    public required BlocoDoCard Bloco { get; init; }
}
