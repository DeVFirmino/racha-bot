namespace PeladaBot.Respostas;

/// <summary>
/// Apelidos de time que não pertencem a nenhum tema, por cor de colete. Cada time sorteia o seu entre estes e os que
/// os temas dão à sua cor.
/// </summary>
public sealed record ApelidosAvulsos
{
    public static readonly ApelidosAvulsos Nenhum = new()
    {
        Laranja = [],
        Verde = [],
        Cinza = [],
        Azul = [],
        SemColete = [],
    };

    public required IReadOnlyList<string> Laranja { get; init; }

    public required IReadOnlyList<string> Verde { get; init; }

    public required IReadOnlyList<string> Cinza { get; init; }

    public required IReadOnlyList<string> Azul { get; init; }

    /// <summary>Do time sem colete, do 5º em diante.</summary>
    public required IReadOnlyList<string> SemColete { get; init; }
}
