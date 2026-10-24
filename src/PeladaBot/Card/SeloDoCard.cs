using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>Uma caixa com um texto curto por cima: o "X" do placar, ou a pílula "FALTAM 2".</summary>
public sealed record SeloDoCard
{
    public required RectangleF Limites { get; init; }

    public required TextoDoCard Texto { get; init; }
}
