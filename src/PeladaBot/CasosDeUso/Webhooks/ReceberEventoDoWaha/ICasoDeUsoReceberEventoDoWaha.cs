namespace PeladaBot.CasosDeUso.Webhooks.ReceberEventoDoWaha;

public interface ICasoDeUsoReceberEventoDoWaha
{
    /// <summary>Filtra o evento e enfileira um comando, sem esperar; null significa que não é um evento do WAHA.</summary>
    void Executar(PedidoReceberEventoDoWaha? pedido);
}
