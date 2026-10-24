namespace PeladaBot.WhatsApp;

/// <summary>Lê mensagens passadas de um grupo do WhatsApp; nunca lança, null significa que o histórico não pôde dizer.</summary>
public interface IHistoricoDoGrupo
{
    /// <summary>A mensagem com esse id, ou null quando o WAHA não a tem ou não pôde ser consultado.</summary>
    Task<MensagemDoGrupo?> ObterMensagemAsync(string idDoChat, string idDaMensagem, CancellationToken cancellationToken);

    /// <summary>Mensagens de outros enviadas desde <paramref name="desde"/>, mais antiga primeiro; null quando ilegível.</summary>
    Task<IReadOnlyList<MensagemDoGrupo>?> ObterMensagensDesdeAsync(
        string idDoChat, DateTimeOffset desde, CancellationToken cancellationToken);
}
