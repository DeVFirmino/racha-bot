using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>O escudo do grupo e o quadrado onde o desenhista o recorta num círculo.</summary>
public sealed record EscudoDoCard
{
    public required byte[] Imagem { get; init; }

    public required RectangleF Limites { get; init; }
}
