using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>A parte da mensagem bruta do GOWS (o campo "_data" de uma mensagem do WAHA) que o bot lê.</summary>
public sealed record DadosDaMensagemWaha
{
    /// <summary>A info de mensagem do whatsmeow, a biblioteca sob o GOWS.</summary>
    [JsonPropertyName("Info")]
    public InfoDaMensagemWaha? Info { get; init; }
}
