using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>A mensagem citada dentro de uma mensagem do WAHA (esquema OpenAPI ReplyToMessage).</summary>
public sealed record RespostaAMensagemWaha
{
    /// <summary>A chave do WhatsApp da mensagem citada; o WAHA pode achá-la por essa ou pelo id completo.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>O texto citado que o WhatsApp embute na resposta; ausente quando o bot pede a mensagem pelo id.</summary>
    [JsonPropertyName("body")]
    public string? Corpo { get; init; }
}
