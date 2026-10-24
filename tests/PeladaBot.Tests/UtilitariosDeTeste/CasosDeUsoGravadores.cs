using System.Collections.Concurrent;
using PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;
using PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;
using PeladaBot.CasosDeUso.Sorteio.SortearTimes;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Faz as vezes dos três casos de uso de comando atrás do consumidor: grava todo pedido, na ordem em que chegou, e falha o
/// comando cujo id de mensagem o teste escolheu, com uma mensagem que carrega um nome, como uma exceção descuidada faria.
/// </summary>
public sealed class CasosDeUsoGravadores
    : ICasoDeUsoSortearTimes, ICasoDeUsoEnviarAjuda, ICasoDeUsoCobrarPendentes
{
    public const string NomeNaFalha = "Paco Sigiloso";

    private readonly ConcurrentQueue<object> _pedidos = new();
    private readonly AvisoDeMudanca _aviso = new();
    private string? _idDaMensagemQueFalha;

    /// <summary>
    /// Todo pedido que os casos de uso receberam: PedidoSortearTimes, PedidoEnviarAjuda ou
    /// PedidoCobrarPendentes.
    /// </summary>
    public IReadOnlyList<object> Pedidos => [.. _pedidos];

    /// <summary>Espera o consumidor, na sua própria thread, entregar <paramref name="quantidade"/> pedidos.</summary>
    public Task EsperarPedidosAsync(int quantidade) => _aviso.EsperarAsync(() => _pedidos.Count >= quantidade);

    /// <summary>O caso de uso lança uma exceção para o comando com esse id de mensagem, depois de registrá-lo.</summary>
    public CasosDeUsoGravadores FalharEm(string idDaMensagem)
    {
        _idDaMensagemQueFalha = idDaMensagem;

        return this;
    }

    public Task Executar(PedidoSortearTimes pedido, CancellationToken cancellationToken) => Registrar(pedido, pedido.IdDaMensagem);

    public Task Executar(PedidoEnviarAjuda pedido, CancellationToken cancellationToken) => Registrar(pedido, pedido.IdDaMensagem);

    public Task Executar(PedidoCobrarPendentes pedido, CancellationToken cancellationToken) => Registrar(pedido, pedido.IdDaMensagem);

    private Task Registrar(object pedido, string idDaMensagem)
    {
        _pedidos.Enqueue(pedido);
        _aviso.Avisar();

        if (idDaMensagem == _idDaMensagemQueFalha)
        {
            throw new InvalidOperationException($"Falhou ao sortear {NomeNaFalha}.");
        }

        return Task.CompletedTask;
    }
}
