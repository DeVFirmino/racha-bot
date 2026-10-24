namespace PeladaBot.Respostas;

/// <summary>
/// Quem enviou a lista que um comando usou, e quando. Só uma lista que o comando não citou tem uma: ela se
/// torna a linha "📋".
/// </summary>
public sealed record OrigemDaLista
{
    /// <summary>O nome no WhatsApp de quem enviou a lista. Null quando desconhecido: a linha então só mostra quando.</summary>
    public required string? NomeDoRemetente { get; init; }

    /// <summary>Quando a lista foi enviada no WhatsApp. A resposta mostra no fuso configurado.</summary>
    public required DateTimeOffset EnviadaEm { get; init; }
}
