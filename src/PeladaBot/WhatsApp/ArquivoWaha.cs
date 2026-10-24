using System.Text.Json.Serialization;

namespace PeladaBot.WhatsApp;

/// <summary>Um arquivo enviado embutido em base64 (OpenAPI BinaryFile); o card é desenhado em memória, nunca em disco.</summary>
public sealed record ArquivoWaha
{
    [JsonPropertyName("mimetype")]
    public required string Mimetype { get; init; }

    [JsonPropertyName("filename")]
    public required string NomeDoArquivo { get; init; }

    /// <summary>O conteúdo do arquivo em base64.</summary>
    [JsonPropertyName("data")]
    public required string Dados { get; init; }
}
