namespace PeladaBot.Sorteio;

/// <summary>Só 1 a 3 jogadores de linha têm ✅, poucos demais para dois times. Não há sorteio.</summary>
public sealed record PagosInsuficientes : ResultadoDoSorteio
{
    public required int QuantidadePaga { get; init; }
}
