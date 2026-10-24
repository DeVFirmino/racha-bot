namespace PeladaBot.WhatsApp;

/// <summary>
/// A mensagem não saiu: o WAHA respondeu com um erro, ou nem pôde ser alcançado. Nada está no grupo, então
/// enviar algo no lugar é seguro, como a legenda sozinha quando a imagem é rejeitada.
/// </summary>
public sealed record Rejeitado : ResultadoDoEnvio
{
    /// <summary>Por quê, sem nada da mensagem em si, como "WAHA respondeu 422". Seguro para logar.</summary>
    public required string Motivo { get; init; }
}
