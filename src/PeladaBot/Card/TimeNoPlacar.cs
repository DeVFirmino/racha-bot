using PeladaBot.Sorteio;
using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>Um time no placar: o colete na sua cor e o nome por fora, com "FALTAM N" embaixo quando incompleto.</summary>
public sealed record TimeNoPlacar
{
    public required CorDoTime? Cor { get; init; }

    /// <summary>True na metade esquerda do campo.</summary>
    public required bool NoLadoBranco { get; init; }

    public required RectangleF Colete { get; init; }

    public required TextoDoCard Nome { get; init; }

    /// <summary>"FALTAM N" sob o nome do time incompleto; null no completo.</summary>
    public required SeloDoCard? Faltam { get; init; }
}
