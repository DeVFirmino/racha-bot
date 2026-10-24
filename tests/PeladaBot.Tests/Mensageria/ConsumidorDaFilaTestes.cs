using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;
using PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;
using PeladaBot.CasosDeUso.Sorteio.SortearTimes;
using PeladaBot.Comandos;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Mensageria;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Mensageria;

public sealed class ConsumidorDaFilaTestes : IAsyncDisposable
{
    private const string PrimeiroId = "false_120363000000001234@g.us_3EB0000000000001_35600000101@c.us";
    private const string SegundoId = "false_120363000000001234@g.us_3EB0000000000002_35600000101@c.us";

    private readonly FakeTimeProvider _relogio = new(ConstrutorDeCasoDeUso.Agora);
    private readonly FilaDeComandos _fila = new();
    private readonly CasosDeUsoGravadores _casosDeUso = new();
    private readonly ColetorDeLogs _logs = new();
    private readonly ServiceProvider _servicos;
    private readonly ConsumidorDaFila _consumidor;

    public ConsumidorDaFilaTestes()
    {
        ServiceCollection servicos = new();
        servicos.AddLogging(logging => logging.AddProvider(_logs).SetMinimumLevel(LogLevel.Trace));
        servicos.AddSingleton<ICasoDeUsoSortearTimes>(_casosDeUso);
        servicos.AddSingleton<ICasoDeUsoEnviarAjuda>(_casosDeUso);
        servicos.AddSingleton<ICasoDeUsoCobrarPendentes>(_casosDeUso);
        _servicos = servicos.BuildServiceProvider();

        _consumidor = new ConsumidorDaFila(
            _fila,
            new IdsRecentes(_relogio),
            _servicos.GetRequiredService<IServiceScopeFactory>(),
            _servicos.GetRequiredService<ILogger<ConsumidorDaFila>>());
    }

    public async ValueTask DisposeAsync()
    {
        _consumidor.Dispose();
        await _servicos.DisposeAsync();
    }

    // Switch explícito: cada comando vai para o seu caso de uso, com o que ele precisa
    [Fact]
    public async Task DeveRotearCadaComandoParaOSeuCasoDeUsoQuandoChegamEmOrdem()
    {
        MensagemCitada citada = new() { Id = "3EB05B1F2A9C4D7E0B02", Texto = "Lista de jogadores:" };

        await Consumir(
            Comando(TipoDeComando.Sortear, PrimeiroId, citada),
            Comando(TipoDeComando.Ajuda, "false_120363000000001234@g.us_3EB0000000000003_35600000404@c.us", citada: null),
            Comando(TipoDeComando.Cobrar, "false_120363000000001234@g.us_3EB0000000000004_35600000404@c.us", citada));

        _casosDeUso.Pedidos.Should().Equal(
            new PedidoSortearTimes
            {
                IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo,
                IdDoRemetente = ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
                IdDaMensagem = PrimeiroId,
                Citada = citada,
                SomentePagos = false,
            },
            new PedidoEnviarAjuda
            {
                IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo,
                IdDoRemetente = ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
                IdDaMensagem = "false_120363000000001234@g.us_3EB0000000000003_35600000404@c.us",
            },
            new PedidoCobrarPendentes
            {
                IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo,
                IdDoRemetente = ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
                IdDaMensagem = "false_120363000000001234@g.us_3EB0000000000004_35600000404@c.us",
                Citada = citada,
            });
    }

    // "!sortear pagos" repassa SomentePagos até o pedido do caso de uso
    [Fact]
    public async Task DeveRepassarSomentePagosParaOPedidoQuandoOComandoPedeSortearPagos()
    {
        await Consumir(Comando(TipoDeComando.Sortear, PrimeiroId, somentePagos: true));

        _casosDeUso.Pedidos.Should().ContainSingle().Which.Should().BeOfType<PedidoSortearTimes>()
            .Which.SomentePagos.Should().BeTrue();
    }

    [Fact]
    public async Task DeveResponderUmaVezQuandoAMesmaMensagemChegaDuasVezes()
    {
        await Consumir(Comando(TipoDeComando.Sortear, PrimeiroId), Comando(TipoDeComando.Sortear, PrimeiroId));

        _casosDeUso.Pedidos.Should().ContainSingle();
        _logs.Entradas.Should().Contain(log => log.Mensagem == "Sortear ***0001 já atendido nos últimos 10 minutos: repetição ignorada");
    }

    // Inclusive chegando ao mesmo tempo
    [Fact]
    public async Task DeveResponderUmaVezQuandoAMesmaMensagemChegaVariasVezesNoMesmoInstante()
    {
        await _consumidor.StartAsync(TestContext.Current.CancellationToken);
        using Barrier mesmoInstante = new(4);
        Task[] entregas =
        [
            .. Enumerable.Range(0, 4).Select(_ => Task.Run(
                () =>
                {
                    mesmoInstante.SignalAndWait(TestContext.Current.CancellationToken);
                    _fila.TentarEnfileirar(Comando(TipoDeComando.Sortear, PrimeiroId)).Should().BeTrue();
                },
                TestContext.Current.CancellationToken)),
        ];

        await Task.WhenAll(entregas);
        await _consumidor.StopAsync(TestContext.Current.CancellationToken);

        _casosDeUso.Pedidos.Should().ContainSingle();
    }

    // Os IDs ficam lembrados por 10 minutos
    [Fact]
    public async Task DeveResponderAMesmaMensagemDeNovoQuandoDezMinutosPassaram()
    {
        await _consumidor.StartAsync(TestContext.Current.CancellationToken);
        _fila.TentarEnfileirar(Comando(TipoDeComando.Ajuda, PrimeiroId));
        await EsperarPedidosAsync(1);
        _relogio.Advance(TimeSpan.FromMinutes(10));

        _fila.TentarEnfileirar(Comando(TipoDeComando.Ajuda, PrimeiroId));
        await _consumidor.StopAsync(TestContext.Current.CancellationToken);

        _casosDeUso.Pedidos.Should().HaveCount(2);
    }

    // Falha de um comando não derruba o próximo
    [Fact]
    public async Task DeveLogarAFalhaEResponderOProximoComandoQuandoUmComandoFalha()
    {
        _casosDeUso.FalharEm(PrimeiroId);

        await Consumir(Comando(TipoDeComando.Sortear, PrimeiroId), Comando(TipoDeComando.Ajuda, SegundoId));

        _casosDeUso.Pedidos.Should().HaveCount(2);
        LogColetado falha = _logs.Entradas.Should().ContainSingle(log => log.Nivel == LogLevel.Error).Which;
        falha.Mensagem.Should().StartWith("Sortear ***0001 falhou; o próximo comando segue.").And.Contain("System.InvalidOperationException");
    }

    // A mensagem da exceção pode carregar um nome; o log leva só o tipo e onde falhou
    [Fact]
    public async Task DeveManterAMensagemDaExcecaoForaDoLogQuandoUmComandoFalha()
    {
        _casosDeUso.FalharEm(PrimeiroId);

        await Consumir(Comando(TipoDeComando.Sortear, PrimeiroId));

        _logs.Entradas.Should().AllSatisfy(log =>
        {
            log.Mensagem.Should().NotContain(CasosDeUsoGravadores.NomeNaFalha).And.NotContain("35600000101");
            log.Excecao.Should().BeNull();
        });
    }

    // Edge Cases ("Desligamento: ordenado, esvazia a fila")
    [Fact]
    public async Task DeveResponderTodoComandoEnfileiradoQuandoOBotDesliga()
    {
        for (int indice = 1; indice <= 20; indice++)
        {
            _fila.TentarEnfileirar(Comando(TipoDeComando.Ajuda, $"false_120363000000001234@g.us_3EB0{indice:X12}_35600000101@c.us"));
        }

        await _consumidor.StartAsync(TestContext.Current.CancellationToken);
        await _consumidor.StopAsync(TestContext.Current.CancellationToken);

        _casosDeUso.Pedidos.Should().HaveCount(20);
        _fila.TentarEnfileirar(Comando(TipoDeComando.Ajuda, PrimeiroId)).Should().BeFalse();
    }

    private async Task Consumir(params ComandoRecebido[] comandos)
    {
        await _consumidor.StartAsync(TestContext.Current.CancellationToken);

        foreach (ComandoRecebido comando in comandos)
        {
            _fila.TentarEnfileirar(comando).Should().BeTrue();
        }

        await _consumidor.StopAsync(TestContext.Current.CancellationToken);
    }

    private Task EsperarPedidosAsync(int quantidade) => _casosDeUso.EsperarPedidosAsync(quantidade);

    private static ComandoRecebido Comando(
        TipoDeComando tipo, string idDaMensagem, MensagemCitada? citada = null, bool somentePagos = false) => new()
        {
            Comando = tipo,
            SomentePagos = somentePagos,
            IdDaMensagem = idDaMensagem,
            IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo,
            IdDoRemetente = ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
            Citada = citada,
        };
}
