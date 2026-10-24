using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PeladaBot.Acesso;
using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Acesso;

public sealed class DisjuntorDeEnvioTestes
{
    private const int MaximoDaSpec = 30;

    private static readonly DateTimeOffset _diaDoJogo = new(2026, 10, 14, 19, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan _janelaDaSpec = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan _umMinuto = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _umSegundo = TimeSpan.FromSeconds(1);

    private readonly FakeTimeProvider _relogio = new(_diaDoJogo);

    [Fact]
    public void DeveDeixarCincoMensagensSairemQuandoODisjuntorTemEspaco()
    {
        DisjuntorDeEnvio disjuntor = CriarDisjuntor(new ConstrutorDeOpcoesDoBot().Build());
        ReservarSeguidamente(disjuntor, MaximoDaSpec - 5);

        int reservadas = ReservarSeguidamente(disjuntor, 5);

        reservadas.Should().Be(5);
    }

    [Fact]
    public void DeveReterATrigesimaPrimeiraMensagemQuandoTrintaSairamNaUltimaHora()
    {
        DisjuntorDeEnvio disjuntor = CriarDisjuntor(new ConstrutorDeOpcoesDoBot().Build());
        ReservarUmaPorMinuto(disjuntor, MaximoDaSpec);

        bool reservada = disjuntor.TentarReservar();

        reservada.Should().BeFalse();
    }

    [Fact]
    public void DeveEnviarDeNovoQuandoAMensagemMaisAntigaSaiDaJanela()
    {
        DisjuntorDeEnvio disjuntor = CriarDisjuntor(new ConstrutorDeOpcoesDoBot().Build());
        ReservarUmaPorMinuto(disjuntor, MaximoDaSpec);
        _relogio.Advance(_janelaDaSpec - (MaximoDaSpec * _umMinuto));

        bool reservada = disjuntor.TentarReservar();

        reservada.Should().BeTrue();
    }

    // Janela móvel: volta uma vaga por mensagem que sai, não as 30 de uma vez
    [Fact]
    public void DeveLiberarSoAsMensagensQueSairamDaJanelaQuandoOTempoPassa()
    {
        DisjuntorDeEnvio disjuntor = CriarDisjuntor(new ConstrutorDeOpcoesDoBot().Build());
        ReservarUmaPorMinuto(disjuntor, MaximoDaSpec);
        _relogio.Advance(_janelaDaSpec - (MaximoDaSpec * _umMinuto) + _umMinuto);

        int reservadas = ReservarSeguidamente(disjuntor, 5);

        reservadas.Should().Be(2);
    }

    // A mensagem ainda conta até completar 60 minutos
    [Fact]
    public void DeveManterAMensagemMaisAntigaNaJanelaQuandoFaltaUmSegundoParaUmaHora()
    {
        DisjuntorDeEnvio disjuntor = CriarDisjuntor(new ConstrutorDeOpcoesDoBot().Build());
        ReservarSeguidamente(disjuntor, MaximoDaSpec);
        _relogio.Advance(_janelaDaSpec - _umSegundo);

        bool reservada = disjuntor.TentarReservar();

        reservada.Should().BeFalse();
    }

    // A mensagem que não saiu não ocupa vaga
    [Fact]
    public void NaoDeveContarAsMensagensRetidasQuandoODisjuntorEstaCheio()
    {
        DisjuntorDeEnvio disjuntor = CriarDisjuntor(new ConstrutorDeOpcoesDoBot().Build());
        ReservarSeguidamente(disjuntor, MaximoDaSpec);
        _relogio.Advance(_janelaDaSpec / 2);
        ReservarSeguidamente(disjuntor, 5);
        _relogio.Advance(_janelaDaSpec / 2);

        int reservadas = ReservarSeguidamente(disjuntor, MaximoDaSpec + 1);

        reservadas.Should().Be(MaximoDaSpec);
    }

    [Fact]
    public void DeveReterAQuartaMensagemQuandoAConfiguracaoReduzODisjuntorParaTres()
    {
        DisjuntorDeEnvio disjuntor = CriarDisjuntor(new ConstrutorDeOpcoesDoBot().ComMaximoDeMensagensDoDisjuntor(3).Build());

        int reservadas = ReservarSeguidamente(disjuntor, 4);

        reservadas.Should().Be(3);
    }

    [Fact]
    public void DeveSeguirAJanelaConfiguradaQuandoAConfiguracaoAEncurta()
    {
        DisjuntorDeEnvio disjuntor = CriarDisjuntor(new ConstrutorDeOpcoesDoBot()
            .ComMaximoDeMensagensDoDisjuntor(3)
            .ComLimiteDeDuracao(nameof(OpcoesDeLimites.JanelaDoDisjuntor), TimeSpan.FromMinutes(5))
            .Build());
        ReservarSeguidamente(disjuntor, 3);
        _relogio.Advance(TimeSpan.FromMinutes(5));

        bool reservada = disjuntor.TentarReservar();

        reservada.Should().BeTrue();
    }

    // Um disjuntor só para o número do bot, com o limite lido da configuração
    [Fact]
    public void DeveCompartilharUmDisjuntorPelaAplicacaoQuandoMensagensSaemDeEscoposDiferentes()
    {
        using FabricaDaAplicacaoDoBot fabrica = FabricaDaAplicacaoDoBot.ComConfiguracoes(new Dictionary<string, string?>
        {
            ["Bot:Limites:MaximoDeMensagensDoDisjuntor"] = "3",
        });
        using IServiceScope primeiroEscopo = fabrica.Services.CreateScope();
        using IServiceScope segundoEscopo = fabrica.Services.CreateScope();
        ReservarSeguidamente(primeiroEscopo.ServiceProvider.GetRequiredService<DisjuntorDeEnvio>(), 3);

        bool reservada = segundoEscopo.ServiceProvider.GetRequiredService<DisjuntorDeEnvio>().TentarReservar();

        reservada.Should().BeFalse();
    }

    /// <summary>Tenta reservar <paramref name="tentativas"/> mensagens no instante atual e retorna quantas conseguiu.</summary>
    private static int ReservarSeguidamente(DisjuntorDeEnvio disjuntor, int tentativas) =>
        Enumerable.Range(0, tentativas).Count(tentativa => disjuntor.TentarReservar());

    /// <summary>Reserva uma mensagem por minuto, a partir de agora: a última sai <paramref name="mensagens"/> - 1 minutos daqui, e o relógio termina um minuto depois dela.</summary>
    private void ReservarUmaPorMinuto(DisjuntorDeEnvio disjuntor, int mensagens)
    {
        for (int mensagem = 0; mensagem < mensagens; mensagem++)
        {
            disjuntor.TentarReservar().Should().BeTrue();
            _relogio.Advance(_umMinuto);
        }
    }

    private DisjuntorDeEnvio CriarDisjuntor(OpcoesDoBot opcoes) => new(_relogio, Options.Create(opcoes));
}
