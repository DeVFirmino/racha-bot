using System.Globalization;
using Microsoft.Extensions.Options;

namespace PeladaBot.WhatsApp;

/// <summary>Rejeita uma configuração do WAHA com a qual o bot não consegue falar.</summary>
public sealed class ValidadorDasOpcoesDoWaha : IValidateOptions<OpcoesDoWaha>
{
    public const string UrlBaseNaoEhHttp = "Waha:UrlBase deve ser uma URL http ou https absoluta; encontrado '{0}'.";
    public const string ChaveDaApiEmBranco = "Waha:ChaveDaApi não pode ser vazia: o WAHA recusa toda chamada sem ela.";
    public const string ChaveHmacDoWebhookEmBranco =
        "Waha:ChaveHmacDoWebhook não pode ser vazia: só webhooks assinados são aceitos.";
    public const string ChaveHmacDoWebhookCurta = "Waha:ChaveHmacDoWebhook deve ter ao menos {0} caracteres.";
    public const string SessaoEmBranco = "Waha:Sessao não pode ser vazia.";

    public const string SessaoComCaracteresInvalidos =
        "Waha:Sessao deve ser um nome simples, sem espaços ou '/'; encontrado '{0}'.";

    /// <summary>Longa o bastante para não ser adivinhada; descarta um placeholder como "changeme".</summary>
    private const int TamanhoMinimoDaChaveHmacDoWebhook = 32;

    public ValidateOptionsResult Validate(string? name, OpcoesDoWaha options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> falhas = [];

        if (EhUrlHttp(options.UrlBase) is false)
        {
            falhas.Add(string.Format(CultureInfo.InvariantCulture, UrlBaseNaoEhHttp, options.UrlBase));
        }

        if (string.IsNullOrWhiteSpace(options.ChaveDaApi))
        {
            falhas.Add(ChaveDaApiEmBranco);
        }

        if (string.IsNullOrWhiteSpace(options.ChaveHmacDoWebhook))
        {
            falhas.Add(ChaveHmacDoWebhookEmBranco);
        }
        else if (options.ChaveHmacDoWebhook.Length < TamanhoMinimoDaChaveHmacDoWebhook)
        {
            falhas.Add(string.Format(
                CultureInfo.InvariantCulture, ChaveHmacDoWebhookCurta, TamanhoMinimoDaChaveHmacDoWebhook));
        }

        if (string.IsNullOrWhiteSpace(options.Sessao))
        {
            falhas.Add(SessaoEmBranco);
        }
        else if (options.Sessao.Any(caractere => char.IsWhiteSpace(caractere) || caractere is '/'))
        {
            falhas.Add(string.Format(CultureInfo.InvariantCulture, SessaoComCaracteresInvalidos, options.Sessao));
        }

        return falhas.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(falhas);
    }

    private static bool EhUrlHttp(string valor) =>
        Uri.TryCreate(valor, UriKind.Absolute, out Uri? url)
        && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);
}
