namespace PeladaBot.WhatsApp;

/// <summary>Nenhuma resposta voltou; a mensagem pode ou não estar no grupo, nunca reenviada.</summary>
public sealed record Incerto : ResultadoDoEnvio
{
    /// <summary>Por quê, sem nada da mensagem em si, como "o WAHA não respondeu a tempo". Seguro para logar.</summary>
    public required string Motivo { get; init; }
}
