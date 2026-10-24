using System.Text.Json.Nodes;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Os payloads do WAHA em Fixtures/. São sintéticos e anonimizados: modelados pelo documento OpenAPI do WAHA 2026.9.2
/// fixado e pelo código do GOWS dentro da sua imagem, com ids e nomes falsos. Payloads do número real os substituem no spike.
/// </summary>
public static class FixturesWaha
{
    /// <summary>!sortear do presidente, citando uma lista com 2 goleiros, 12 pagos, 1 não pago e 1 em espera.</summary>
    public const string SorteioCitandoLista = "waha-webhook-message-draw-quoting-list.json";

    /// <summary>Um webhook session.status, como o WAHA o enviou localmente para uma sessão parada antes do pareamento.</summary>
    public const string StatusDaSessao = "waha-webhook-session-status.json";

    /// <summary>A resposta de GET /api/{session}/chats/{chatId}/messages: uma mensagem de chat e uma lista mais antiga, mais recente primeiro.</summary>
    public const string MensagensDoChat = "waha-chat-messages.json";

    /// <summary>A resposta de GET /api/{session}/chats/{chatId}/messages/{messageId}: a lista.</summary>
    public const string MensagemDoChat = "waha-chat-message.json";

    /// <summary>A resposta de GET /api/sessions/{session} para uma sessão funcionando.</summary>
    public const string Sessao = "waha-session.json";

    public static string Ler(string nomeDoArquivo) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", nomeDoArquivo));

    /// <summary>A fixture como uma árvore JSON, para um teste que muda um campo.</summary>
    public static JsonNode LerNo(string nomeDoArquivo) =>
        JsonNode.Parse(Ler(nomeDoArquivo)) ?? throw new InvalidOperationException($"A fixture {nomeDoArquivo} está vazia.");
}
