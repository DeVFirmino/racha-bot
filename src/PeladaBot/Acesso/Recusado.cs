namespace PeladaBot.Acesso;

/// <summary>Fora da presidência: o bot responde <see cref="Resposta"/> e não faz mais nada.</summary>
public sealed record Recusado : Admissao
{
    public required string Resposta { get; init; }
}
