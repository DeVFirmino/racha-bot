namespace PeladaBot.Listas;

/// <summary>Um item de "Lista de jogadores" ou da "Lista de Espera", com a marca de pago (✅).</summary>
public sealed record JogadorListado
{
    public required string Nome { get; init; }

    public required bool Pago { get; init; }
}
