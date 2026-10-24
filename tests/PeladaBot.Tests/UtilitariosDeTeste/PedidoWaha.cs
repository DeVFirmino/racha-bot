namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>Um pedido que o stub do WAHA recebeu, como ele foi pelo fio.</summary>
public sealed record PedidoWaha
{
    public required string Metodo { get; init; }

    /// <summary>O caminho, ainda escapado, como "/api/default/chats/120363000000001234%40g.us/messages".</summary>
    public required string Caminho { get; init; }

    /// <summary>A query string com seu "?", ou vazia.</summary>
    public required string Query { get; init; }

    /// <summary>O cabeçalho X-Api-Key, ou null quando o pedido não tinha nenhum.</summary>
    public required string? ChaveDaApi { get; init; }

    public required string? TipoDeConteudo { get; init; }

    public required string? Corpo { get; init; }
}
