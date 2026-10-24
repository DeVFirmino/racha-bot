namespace PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;

public interface ICasoDeUsoEnviarAjuda
{
    Task Executar(PedidoEnviarAjuda pedido, CancellationToken cancellationToken);
}
