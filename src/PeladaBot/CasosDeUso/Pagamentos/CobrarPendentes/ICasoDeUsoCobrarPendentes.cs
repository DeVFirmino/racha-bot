namespace PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;

public interface ICasoDeUsoCobrarPendentes
{
    Task Executar(PedidoCobrarPendentes pedido, CancellationToken cancellationToken);
}
