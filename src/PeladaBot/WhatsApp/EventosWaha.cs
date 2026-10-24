namespace PeladaBot.WhatsApp;

/// <summary>Os eventos de webhook que o bot assina; qualquer outro evento, edições incluídas, é ignorado.</summary>
public static class EventosWaha
{
    /// <summary>Uma mensagem nova de outra pessoa. O GOWS deixa edições, exclusões e reações fora disso.</summary>
    public const string Mensagem = "message";

    /// <summary>A sessão mudou de status.</summary>
    public const string StatusDaSessao = "session.status";
}
