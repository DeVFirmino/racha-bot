using System.Text.Json;
using System.Text.Json.Serialization;

namespace PeladaBot.CasosDeUso.Webhooks.ReceberEventoDoWaha;

/// <summary>
/// O envelope de todo webhook do WAHA, só com os campos que o bot lê; o payload fica bruto até o caso de uso
/// conhecer o evento. Nada é obrigatório: um webhook malformado é descartado com um log, nunca um pedido com
/// falha.
/// </summary>
public sealed record PedidoReceberEventoDoWaha
{
    /// <summary>Como "message" ou "session.status".</summary>
    [JsonPropertyName("event")]
    public string? Evento { get; init; }

    /// <summary>Uma mensagem para "message", uma sessão para "session.status".</summary>
    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; init; }
}
