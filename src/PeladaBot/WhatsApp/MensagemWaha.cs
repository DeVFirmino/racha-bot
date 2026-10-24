using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>Uma mensagem como o WAHA a descreve; só os campos que o bot lê, nenhum deles obrigatório.</summary>
public sealed record MensagemWaha
{
    /// <summary>O último segundo que <see cref="DateTimeOffset"/> comporta, 9999-12-31T23:59:59Z.</summary>
    private const long UltimoSegundoUnix = 253_402_300_799;

    /// <summary>O id do WAHA, estável em toda entrega da mensagem; uma resposta cita a mensagem por ele.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Quando a mensagem foi enviada no WhatsApp, em segundos Unix.</summary>
    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; init; }

    /// <summary><see cref="Timestamp"/> como um instante; null quando ausente ou fora do intervalo em algumas mensagens.</summary>
    [JsonIgnore]
    public DateTimeOffset? EnviadaEm =>
        Timestamp is > 0 and <= UltimoSegundoUnix ? DateTimeOffset.FromUnixTimeSeconds(Timestamp.Value) : null;

    /// <summary>O chat: o grupo (@g.us) para uma mensagem de grupo, a pessoa (@c.us ou @lid) no privado.</summary>
    [JsonPropertyName("from")]
    public string? De { get; init; }

    /// <summary>True quando o número do bot a enviou.</summary>
    [JsonPropertyName("fromMe")]
    public bool FromMe { get; init; }

    /// <summary>Em um grupo, quem enviou a mensagem: telefone (@c.us) ou LID (@lid).</summary>
    [JsonPropertyName("participant")]
    public string? Participante { get; init; }

    /// <summary>O texto da mensagem em si, ou a legenda de uma mensagem com mídia. Nunca o texto que ela cita.</summary>
    [JsonPropertyName("body")]
    public string? Corpo { get; init; }

    /// <summary>A mensagem a que esta responde. Null quando não cita nada.</summary>
    [JsonPropertyName("replyTo")]
    public RespostaAMensagemWaha? RespondeA { get; init; }

    /// <summary>A mensagem bruta da engine; pode mudar, então o bot só lê o nome do remetente dela.</summary>
    [JsonPropertyName("_data")]
    public DadosDaMensagemWaha? Dados { get; init; }
}
