namespace PeladaBot.Configuracao;

/// <summary>
/// Apelidos de time fora dos temas, por cor de colete. Entram no sorteio do apelido junto com os dos temas; ver
/// <see cref="Respostas.ApelidosAvulsos"/>.
/// </summary>
public sealed class OpcoesDosApelidosAvulsos
{
    public IReadOnlyList<string> Laranja { get; init; } = [];

    public IReadOnlyList<string> Verde { get; init; } = [];

    public IReadOnlyList<string> Cinza { get; init; } = [];

    public IReadOnlyList<string> Azul { get; init; } = [];

    /// <summary>Do time sem colete, do 5º em diante.</summary>
    public IReadOnlyList<string> SemColete { get; init; } = [];
}
