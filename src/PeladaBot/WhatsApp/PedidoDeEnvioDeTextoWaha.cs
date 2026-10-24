using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>O corpo de POST /api/sendText no WAHA 2026.9.2 (esquema OpenAPI MessageTextRequest).</summary>
public sealed record PedidoDeEnvioDeTextoWaha
{
    [JsonPropertyName("session")]
    public required string Sessao { get; init; }

    [JsonPropertyName("chatId")]
    public required string IdDoChat { get; init; }

    [JsonPropertyName("text")]
    public required string Texto { get; init; }

    /// <summary>O id do WAHA da mensagem que o texto responde, que o WhatsApp então mostra citada.</summary>
    [JsonPropertyName("reply_to")]
    public required string RespondeA { get; init; }

    /// <summary>Sempre false: nenhuma resposta carrega um link, então nunca há uma prévia para buscar.</summary>
    [JsonPropertyName("linkPreview")]
    public required bool PreviaDeLink { get; init; }
}
