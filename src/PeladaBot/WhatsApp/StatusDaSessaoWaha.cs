namespace PeladaBot.WhatsApp;

/// <summary>Os status de sessão sobre os quais o bot age. O WAHA 2026.9.2 também tem SCAN_QR_CODE e os de passkey.</summary>
public static class StatusDaSessaoWaha
{
    /// <summary>Conectada: o único status em que o WAHA envia e lê mensagens.</summary>
    public const string Funcionando = "WORKING";

    /// <summary>Ainda conectando, como logo depois que o WAHA inicia; não é erro até o próximo status dizer que é.</summary>
    public const string Iniciando = "STARTING";

    /// <summary>A sessão quebrou. O bot loga um erro.</summary>
    public const string Falhou = "FAILED";

    /// <summary>A sessão foi parada. O bot loga um erro.</summary>
    public const string Parada = "STOPPED";
}
