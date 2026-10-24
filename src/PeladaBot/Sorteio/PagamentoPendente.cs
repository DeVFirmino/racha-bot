namespace PeladaBot.Sorteio;

/// <summary>
/// Um ou mais jogadores de "Lista de jogadores" estão sem ✅. Não há sorteio: ele só sai quando todo mundo da
/// lista pagar.
/// </summary>
public sealed record PagamentoPendente : ResultadoDoSorteio
{
    /// <summary>Quem está sem ✅, na ordem da lista.</summary>
    public required IReadOnlyList<string> NomesPendentes { get; init; }
}
