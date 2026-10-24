namespace PeladaBot.WhatsApp;

/// <summary>
/// O que aconteceu a uma mensagem que o bot tentou enviar: saiu, não saiu, ou talvez tenha saído.
/// Um tipo e não uma exceção, porque o fallback da imagem do sorteio depende de qual é.
/// </summary>
public abstract record ResultadoDoEnvio;
