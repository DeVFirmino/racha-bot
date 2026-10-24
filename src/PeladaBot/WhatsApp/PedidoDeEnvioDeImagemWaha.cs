using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>O corpo de POST /api/sendImage (esquema OpenAPI MessageImageRequest), com a imagem embutida.</summary>
public sealed record PedidoDeEnvioDeImagemWaha
{
    [JsonPropertyName("session")]
    public required string Sessao { get; init; }

    [JsonPropertyName("chatId")]
    public required string IdDoChat { get; init; }

    [JsonPropertyName("file")]
    public required ArquivoWaha Arquivo { get; init; }

    /// <summary>O texto sob a imagem, na mesma mensagem; null manda a imagem sem legenda, sem o campo.</summary>
    [JsonPropertyName("caption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required string? Legenda { get; init; }

    /// <summary>O id do WAHA da mensagem que a imagem responde, que o WhatsApp então mostra citada.</summary>
    [JsonPropertyName("reply_to")]
    public required string RespondeA { get; init; }
}
