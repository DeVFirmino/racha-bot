namespace PeladaBot.Sorteio;

/// <summary>
/// Os times, na ordem do sorteio. Não há mais reservas: todo mundo pago entra em algum time, o último
/// deles podendo ficar incompleto.
/// </summary>
public sealed record TimesSorteados : ResultadoDoSorteio
{
    public required IReadOnlyList<Time> Times { get; init; }

    /// <summary>
    /// Quem tinha ✅ pendente e foi sorteado assim mesmo, por "!sortear pagos"/"!times pagos". Vazio no
    /// sorteio normal, que nunca chega a um <see cref="TimesSorteados"/> com alguém pendente.
    /// </summary>
    public required IReadOnlyList<string> IgnorandoPendentes { get; init; }

    /// <summary>
    /// Goleiros fixos que sobraram com todos os times já fechados: não formam time, revezam no gol.
    /// Quase sempre vazio.
    /// </summary>
    public IReadOnlyList<string> RevezandoNoGol { get; init; } = [];
}
