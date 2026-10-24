using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>
/// Um confronto: o placar com os coletes e, embaixo, o campo dividido ao meio, o primeiro time no lado
/// branco e o segundo no vermelho. Com um time só, ele ocupa o campo inteiro.
/// </summary>
public sealed record BlocoDoCard
{
    /// <summary>Um ou dois times, na ordem do sorteio: o primeiro à esquerda, no branco.</summary>
    public required IReadOnlyList<TimeNoPlacar> Times { get; init; }

    /// <summary>O "X" entre os dois coletes; null com um time só.</summary>
    public required SeloDoCard? Versus { get; init; }

    public required RectangleF Campo { get; init; }

    public required IReadOnlyList<JogadorDoCard> Jogadores { get; init; }
}
