using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>Uma sessão do WAHA, de GET /api/sessions/{session} e do payload do webhook "session.status".</summary>
public sealed record SessaoWaha
{
    /// <summary>O nome da sessão, como "default".</summary>
    [JsonPropertyName("name")]
    public string? Nome { get; init; }

    /// <summary>Um dos valores de <see cref="StatusDaSessaoWaha"/>, ou outro status que o WAHA possa somar, como SCAN_QR_CODE.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
