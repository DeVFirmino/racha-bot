using System.Security.Cryptography;
using System.Text;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Assina um webhook do jeito que o WAHA 2026.9.2 faz com WHATSAPP_HOOK_HMAC_KEY definida (modules/waha-webhook/WebhookPlugin.sender.js
/// na imagem): X-Webhook-Hmac carrega o HMAC-SHA512 do corpo bruto, em hex minúsculo. Escrito aqui por conta própria, não com o
/// código do bot, para um teste falhar quando o bot se desviar do que o WAHA envia.
/// </summary>
public static class AssinaturaWaha
{
    public const string Cabecalho = "X-Webhook-Hmac";

    /// <summary>A assinatura do corpo com a chave do bot de teste, ou com <paramref name="chave"/>.</summary>
    public static string De(string corpo, string chave = FabricaDaAplicacaoDoBot.ChaveHmacDoWebhookDoWaha) =>
        Convert.ToHexStringLower(HMACSHA512.HashData(Encoding.UTF8.GetBytes(chave), Encoding.UTF8.GetBytes(corpo)));
}
