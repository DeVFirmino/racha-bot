namespace PeladaBot.CasosDeUso.Sorteio.SortearTimes;

public interface ICasoDeUsoSortearTimes
{
    Task Executar(PedidoSortearTimes pedido, CancellationToken cancellationToken);
}
