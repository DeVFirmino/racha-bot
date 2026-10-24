namespace PeladaBot.WhatsApp;

/// <summary>Escreve em um grupo do WhatsApp; toda mensagem cita a que ela responde. Nunca lança.</summary>
public interface IMensageiroDoGrupo
{
    Task<ResultadoDoEnvio> EnviarTextoAsync(
        string idDoChat, string texto, string idDaMensagemRespondida, CancellationToken cancellationToken);

    /// <summary>
    /// Envia <paramref name="png"/> como uma imagem com <paramref name="legenda"/> abaixo, ou sem legenda
    /// quando ela é null.
    /// </summary>
    Task<ResultadoDoEnvio> EnviarImagemAsync(
        string idDoChat, byte[] png, string? legenda, string idDaMensagemRespondida, CancellationToken cancellationToken);

    /// <summary>Mostra "digitando..."; false quando não conseguiu, a resposta segue mesmo assim.</summary>
    Task<bool> IniciarDigitandoAsync(string idDoChat, CancellationToken cancellationToken);

    /// <summary>Tira "digitando...". False quando não conseguiu; a resposta segue mesmo assim.</summary>
    Task<bool> PararDigitandoAsync(string idDoChat, CancellationToken cancellationToken);
}
