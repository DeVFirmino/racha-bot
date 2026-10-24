using Mapster;
using PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;
using PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;
using PeladaBot.CasosDeUso.Sorteio.SortearTimes;
using PeladaBot.Comandos;
using PeladaBot.Configuracao;

namespace PeladaBot.Mensageria;

/// <summary>
/// O único leitor da fila: um comando por vez, para os limites, o disjuntor e a dedupe nunca correrem
/// uma corrida. Descarta uma repetição, roteia cada comando para seu caso de uso, e loga uma falha sem parar.
/// </summary>
public sealed partial class ConsumidorDaFila : BackgroundService
{
    private readonly FilaDeComandos _fila;
    private readonly IdsRecentes _idsRecentes;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConsumidorDaFila> _logger;

    // Cancelado só quando o host desiste do esgotamento, para uma resposta em andamento sobreviver ao primeiro sinal de parada.
    private readonly CancellationTokenSource _prazoDeEsgotamento = new();

    // Definido quando o laço roda; uma parada que corre com o início de ExecuteAsync na thread pool perderia a fila.
    private readonly TaskCompletionSource _lacoIniciado = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ConsumidorDaFila(
        FilaDeComandos fila,
        IdsRecentes idsRecentes,
        IServiceScopeFactory scopeFactory,
        ILogger<ConsumidorDaFila> logger)
    {
        _fila = fila;
        _idsRecentes = idsRecentes;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Fecha a fila e deixa o laço responder o que resta, até o tempo limite de parada do host.</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _fila.Completar();

        using CancellationTokenRegistration prazoDeEsgotamento = cancellationToken.Register(_prazoDeEsgotamento.Cancel);

        if (ExecuteTask is not null)
        {
            await Task.WhenAny(_lacoIniciado.Task, Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _prazoDeEsgotamento.Dispose();
        base.Dispose();
    }

    /// <summary>Termina só quando a fila está fechada e vazia: isso é o esgotamento, não o sinal de parada do host.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _lacoIniciado.TrySetResult();

        await foreach (ComandoRecebido comando in _fila.LerTudoAsync(CancellationToken.None))
        {
            await ProcessarAsync(comando, _prazoDeEsgotamento.Token);
        }
    }

    private async Task ProcessarAsync(ComandoRecebido comando, CancellationToken cancellationToken)
    {
        string idDaMensagem = MascaraDeId.AplicarNaMensagem(comando.IdDaMensagem);

        if (_idsRecentes.TentarLembrar(comando.IdDaMensagem) is false)
        {
            RegistrarRepetido(_logger, comando.Comando, idDaMensagem);

            return;
        }

        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

            await RotearAsync(scope.ServiceProvider, comando, cancellationToken);
        }
        catch (Exception excecao)
            when (excecao is not OperationCanceledException || cancellationToken.IsCancellationRequested is false)
        {
            RegistrarFalhou(_logger, comando.Comando, idDaMensagem, MascaraDeExcecao.Aplicar(excecao));
        }
    }

    private static Task RotearAsync(
        IServiceProvider services, ComandoRecebido comando, CancellationToken cancellationToken) =>
        comando.Comando switch
        {
            TipoDeComando.Sortear => RotearSorteio(services, comando, cancellationToken),
            TipoDeComando.Ajuda => RotearAjuda(services, comando, cancellationToken),
            TipoDeComando.Cobrar => RotearCobranca(services, comando, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(comando), comando.Comando, "Comando desconhecido."),
        };

    private static Task RotearSorteio(
        IServiceProvider services, ComandoRecebido comando, CancellationToken cancellationToken)
    {
        PedidoSortearTimes pedido = comando.Adapt<PedidoSortearTimes>(ConfiguracaoDeMapeamento.Config);

        return services.GetRequiredService<ICasoDeUsoSortearTimes>().Executar(pedido, cancellationToken);
    }

    private static Task RotearAjuda(
        IServiceProvider services, ComandoRecebido comando, CancellationToken cancellationToken)
    {
        PedidoEnviarAjuda pedido = comando.Adapt<PedidoEnviarAjuda>(ConfiguracaoDeMapeamento.Config);

        return services.GetRequiredService<ICasoDeUsoEnviarAjuda>().Executar(pedido, cancellationToken);
    }

    private static Task RotearCobranca(
        IServiceProvider services, ComandoRecebido comando, CancellationToken cancellationToken)
    {
        PedidoCobrarPendentes pedido = comando.Adapt<PedidoCobrarPendentes>(ConfiguracaoDeMapeamento.Config);

        return services.GetRequiredService<ICasoDeUsoCobrarPendentes>().Executar(pedido, cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Comando} {IdDaMensagem} já atendido nos últimos 10 minutos: repetição ignorada")]
    private static partial void RegistrarRepetido(ILogger logger, TipoDeComando comando, string idDaMensagem);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "{Comando} {IdDaMensagem} falhou; o próximo comando segue. {Falha}")]
    private static partial void RegistrarFalhou(
        ILogger logger, TipoDeComando comando, string idDaMensagem, string falha);
}
