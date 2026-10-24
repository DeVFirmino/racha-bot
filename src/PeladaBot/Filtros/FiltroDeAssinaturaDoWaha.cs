using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PeladaBot.WhatsApp;

namespace PeladaBot.Filtros;

/// <summary>
/// Só deixa passar um webhook cujo X-Webhook-Hmac combina com o HMAC-SHA512 do corpo bruto, verificado antes do
/// model binding; uma assinatura ausente ou errada recebe um 401, e um corpo acima de
/// <see cref="TamanhoMaximoDoCorpo"/> recebe um 413 antes de ser lido. O log diz o motivo, nunca o corpo ou o
/// cabeçalho.
/// </summary>
public sealed partial class FiltroDeAssinaturaDoWaha : IAsyncAuthorizationFilter
{
    /// <summary>O cabeçalho com a assinatura; X-Webhook-Hmac-Algorithm é sempre "sha512" e não é usado aqui.</summary>
    public const string CabecalhoDaAssinatura = "X-Webhook-Hmac";

    /// <summary>
    /// 1 MB: uma lista real tem uns 4 KB e um evento do WAHA, poucas dezenas. O corpo é lido inteiro, em memória, antes
    /// de a assinatura ser conferida; o limite impede que alguém sem a chave mande corpos enormes para gastar memória.
    /// </summary>
    public const long TamanhoMaximoDoCorpo = 1024 * 1024;

    private readonly byte[] _chave;
    private readonly ILogger<FiltroDeAssinaturaDoWaha> _logger;

    public FiltroDeAssinaturaDoWaha(IOptions<OpcoesDoWaha> opcoes, ILogger<FiltroDeAssinaturaDoWaha> logger)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        _chave = Encoding.UTF8.GetBytes(opcoes.Value.ChaveHmacDoWebhook);
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpRequest request = context.HttpContext.Request;

        if (request.ContentLength > TamanhoMaximoDoCorpo)
        {
            Recusar(context, "corpo maior que o limite", new StatusCodeResult(StatusCodes.Status413PayloadTooLarge));

            return;
        }

        // Sem Content-Length (corpo em chunks), o Kestrel corta a leitura no mesmo limite.
        if (context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limite)
        {
            limite.MaxRequestBodySize = TamanhoMaximoDoCorpo;
        }

        string? assinatura = request.Headers[CabecalhoDaAssinatura];

        if (string.IsNullOrEmpty(assinatura))
        {
            Recusar(context, "sem cabeçalho X-Webhook-Hmac", new UnauthorizedResult());

            return;
        }

        // Só em memória, nunca em um arquivo temporário; o Kestrel ainda limita o tamanho do corpo.
        request.EnableBuffering(bufferThreshold: int.MaxValue);
        byte[] esperado = await HMACSHA512.HashDataAsync(_chave, request.Body, context.HttpContext.RequestAborted);
        request.Body.Position = 0;

        if (Corresponde(assinatura, esperado) is false)
        {
            Recusar(context, "X-Webhook-Hmac não combina com o corpo", new UnauthorizedResult());
        }
    }

    /// <summary>Comparação em tempo constante, para o tempo nunca revelar quanto de uma assinatura forjada estava certo.</summary>
    private static bool Corresponde(string assinatura, byte[] esperado)
    {
        Span<byte> recebido = stackalloc byte[HMACSHA512.HashSizeInBytes];

        return Convert.FromHexString(assinatura, recebido, out _, out int bytesEscritos) is OperationStatus.Done
            && CryptographicOperations.FixedTimeEquals(recebido[..bytesEscritos], esperado);
    }

    private void Recusar(AuthorizationFilterContext context, string motivo, StatusCodeResult resultado)
    {
        RegistrarRecusado(_logger, resultado.StatusCode, motivo);

        context.Result = resultado;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook do WAHA recusado com {Status}: {Motivo}")]
    private static partial void RegistrarRecusado(ILogger logger, int status, string motivo);
}
