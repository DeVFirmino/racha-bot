namespace PeladaBot.WhatsApp;

/// <summary>A seção de configuração "Waha"; as duas chaves vêm de variáveis de ambiente, nunca do Git.</summary>
public sealed class OpcoesDoWaha
{
    public const string SectionName = "Waha";

    /// <summary>A raiz da API do WAHA, como "http://waha:3000" na rede privada.</summary>
    public string UrlBase { get; init; } = string.Empty;

    /// <summary>Vai no cabeçalho X-Api-Key de toda chamada. Nunca logada.</summary>
    public string ChaveDaApi { get; init; } = string.Empty;

    /// <summary>O segredo com que o WAHA assina webhooks; só um corpo correspondente é aceito. Nunca logada.</summary>
    public string ChaveHmacDoWebhook { get; init; } = string.Empty;

    /// <summary>A sessão do WAHA do número do bot, "default"; o bot usa só essa sessão.</summary>
    public string Sessao { get; init; } = string.Empty;
}
