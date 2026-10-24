using Microsoft.AspNetCore.Mvc;
using PeladaBot.CasosDeUso.Webhooks.ReceberEventoDoWaha;
using PeladaBot.Filtros;

namespace PeladaBot.Controllers;

/// <summary>
/// Onde o WAHA entrega seus eventos. Sempre responde 200 de imediato; a resposta vem depois, da fila.
/// Antes, o <see cref="FiltroDeAssinaturaDoWaha"/> recusa com 401 um webhook não assinado ou mal assinado, e com
/// 413 um corpo grande demais.
/// </summary>
[ApiController]
[Route("webhooks/waha")]
public sealed class WebhooksController : ControllerBase
{
    /// <summary>Um corpo que não é um evento do WAHA chega como null; o caso de uso loga e o descarta.</summary>
    [HttpPost]
    // A autenticação deste endpoint é o HMAC validado pelo filtro, sem [Authorize].
    [ServiceFilter<FiltroDeAssinaturaDoWaha>]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public IActionResult ReceberEventoDoWaha(
        [FromServices] ICasoDeUsoReceberEventoDoWaha casoDeUso,
        [FromBody] PedidoReceberEventoDoWaha? pedido)
    {
        casoDeUso.Executar(pedido);

        return Ok();
    }
}
