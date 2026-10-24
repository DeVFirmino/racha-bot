using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>O corpo de POST /api/startTyping e /api/stopTyping (esquema OpenAPI ChatRequest).</summary>
public sealed record PedidoDeChatWaha
{
    [JsonPropertyName("session")]
    public required string Sessao { get; init; }

    [JsonPropertyName("chatId")]
    public required string IdDoChat { get; init; }
}
