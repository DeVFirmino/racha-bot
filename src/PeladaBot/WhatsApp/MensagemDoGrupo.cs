namespace PeladaBot.WhatsApp;

/// <summary>Uma mensagem lida do histórico de um grupo; vive só enquanto o bot responde a um comando.</summary>
public sealed record MensagemDoGrupo
{
    /// <summary>O id que o WAHA dá à mensagem.</summary>
    public required string Id { get; init; }

    /// <summary>O id do WhatsApp de quem enviou, telefone (@c.us) ou LID (@lid). Null quando o WAHA não informa.</summary>
    public required string? IdDoRemetente { get; init; }

    /// <summary>O nome no WhatsApp de quem enviou, como configurado no telefone. Null quando o WAHA não informa.</summary>
    public required string? NomeDoRemetente { get; init; }

    /// <summary>Quando foi enviada no WhatsApp, até o segundo.</summary>
    public required DateTimeOffset EnviadaEm { get; init; }

    /// <summary>O texto, ou a legenda de uma mensagem com mídia. Null quando não tem nenhum dos dois.</summary>
    public required string? Texto { get; init; }
}
