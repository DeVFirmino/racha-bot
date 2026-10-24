using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>A info de mensagem que o GOWS envia dentro de "_data". Só o nome do remetente é lido.</summary>
public sealed record InfoDaMensagemWaha
{
    /// <summary>O nome do remetente no WhatsApp; pode ir para a linha "📋 Lista enviada por", nunca para um log.</summary>
    [JsonPropertyName("PushName")]
    public string? NomeDeExibicao { get; init; }
}
