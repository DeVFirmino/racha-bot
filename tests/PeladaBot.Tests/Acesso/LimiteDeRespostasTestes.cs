using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PeladaBot.Acesso;
using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Acesso;

public sealed class LimiteDeRespostasTestes
{
    private const string IdDoGrupo = "120363000000001234@g.us";
    private const string IdDoOutroGrupo = "120363000000005678@g.us";
    private const string IdDoIntegrante = "35600000404@c.us";
    private const string IdDoOutroIntegrante = "35600000505@c.us";

    private static readonly DateTimeOffset _diaDoJogo = new(2026, 10, 14, 19, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan _janelaDeCobranca = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan _janelaDaSpec = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _umSegundo = TimeSpan.FromSeconds(1);

    private readonly FakeTimeProvider _relogio = new(_diaDoJogo);

    [Fact]
    public void DevePermitirARecusaQuandoAPessoaNaoRecebeuNenhumaAinda()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());

        bool podeResponder = limite.PodeResponderNaoPresidente(IdDoGrupo, IdDoIntegrante);

        podeResponder.Should().BeTrue();
    }

    [Fact]
    public void DeveReterARecusaQuandoAPessoaRecebeuUmaMenosDeDezMinutosAtras()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarRespostaNaoPresidenteEnviada(IdDoGrupo, IdDoIntegrante);
        _relogio.Advance(_janelaDaSpec - _umSegundo);

        bool podeResponder = limite.PodeResponderNaoPresidente(IdDoGrupo, IdDoIntegrante);

        podeResponder.Should().BeFalse();
    }

    [Fact]
    public void DevePermitirARecusaDeNovoQuandoDezMinutosPassaram()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarRespostaNaoPresidenteEnviada(IdDoGrupo, IdDoIntegrante);
        _relogio.Advance(_janelaDaSpec);

        bool podeResponder = limite.PodeResponderNaoPresidente(IdDoGrupo, IdDoIntegrante);

        podeResponder.Should().BeTrue();
    }

    // A janela é de cada pessoa
    [Fact]
    public void DevePermitirARecusaAOutraPessoaQuandoSoAPrimeiraFoiRecusada()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarRespostaNaoPresidenteEnviada(IdDoGrupo, IdDoIntegrante);

        bool podeResponder = limite.PodeResponderNaoPresidente(IdDoGrupo, IdDoOutroIntegrante);

        podeResponder.Should().BeTrue();
    }

    // A janela só começa quando a recusa sai de fato
    [Fact]
    public void NaoDeveIniciarAJanelaDeRecusaQuandoNenhumaRecusaFoiRegistradaComoEnviada()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.PodeResponderNaoPresidente(IdDoGrupo, IdDoIntegrante);

        bool podeResponder = limite.PodeResponderNaoPresidente(IdDoGrupo, IdDoIntegrante);

        podeResponder.Should().BeTrue();
    }

    [Fact]
    public void DevePermitirUmaRespostaDePendentesQuandoOGrupoNaoRecebeuNenhumaAinda()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());

        bool podeResponder = limite.PodeCobrar(IdDoGrupo);

        podeResponder.Should().BeTrue();
    }

    [Fact]
    public void DeveSegurarACobrancaQuandoOGrupoFoiCobradoHaMenosDeTrintaMinutos()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarCobrancaEnviada(IdDoGrupo);
        _relogio.Advance(_janelaDeCobranca - _umSegundo);

        bool podeResponder = limite.PodeCobrar(IdDoGrupo);

        podeResponder.Should().BeFalse();
    }

    [Fact]
    public void DevePermitirACobrancaDeNovoQuandoTrintaMinutosPassaram()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarCobrancaEnviada(IdDoGrupo);
        _relogio.Advance(_janelaDeCobranca);

        bool podeResponder = limite.PodeCobrar(IdDoGrupo);

        podeResponder.Should().BeTrue();
    }

    // A janela só começa quando a resposta sai de fato
    [Fact]
    public void NaoDeveIniciarAJanelaDeCobrancaQuandoNenhumaCobrancaFoiRegistradaComoEnviada()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.PodeCobrar(IdDoGrupo);

        bool podeResponder = limite.PodeCobrar(IdDoGrupo);

        podeResponder.Should().BeTrue();
    }

    // As duas janelas são independentes
    [Fact]
    public void DeveAindaPermitirACobrancaQuandoAPessoaAcabouDeSerRecusada()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarRespostaNaoPresidenteEnviada(IdDoGrupo, IdDoIntegrante);

        bool podeResponder = limite.PodeCobrar(IdDoGrupo);

        podeResponder.Should().BeTrue();
    }

    // As duas janelas são independentes
    [Fact]
    public void DeveAindaPermitirARecusaQuandoOGrupoAcabouDeSerCobrado()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarCobrancaEnviada(IdDoGrupo);

        bool podeResponder = limite.PodeResponderNaoPresidente(IdDoGrupo, IdDoIntegrante);

        podeResponder.Should().BeTrue();
    }

    [Fact]
    public void DeveSeguirAsJanelasConfiguradasQuandoUmAmbienteAsEncurta()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot()
            .ComLimiteDeDuracao(nameof(OpcoesDeLimites.JanelaDeRespostaNaoPresidente), TimeSpan.FromMinutes(1))
            .ComLimiteDeDuracao(nameof(OpcoesDeLimites.JanelaDeCobranca), TimeSpan.FromMinutes(2))
            .Build());
        limite.RegistrarRespostaNaoPresidenteEnviada(IdDoGrupo, IdDoIntegrante);
        limite.RegistrarCobrancaEnviada(IdDoGrupo);
        _relogio.Advance(TimeSpan.FromMinutes(1));

        bool podeRecusar = limite.PodeResponderNaoPresidente(IdDoGrupo, IdDoIntegrante);
        bool podeCobrar = limite.PodeCobrar(IdDoGrupo);

        podeRecusar.Should().BeTrue();
        podeCobrar.Should().BeFalse();
    }

    // Estado em memória com chave por grupo
    [Fact]
    public void DeveManterAsJanelasDeCadaGrupoSeparadasQuandoOBotEstaEmDoisGrupos()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarRespostaNaoPresidenteEnviada(IdDoGrupo, IdDoIntegrante);
        limite.RegistrarCobrancaEnviada(IdDoGrupo);

        bool podeRecusarNoOutroGrupo = limite.PodeResponderNaoPresidente(IdDoOutroGrupo, IdDoIntegrante);
        bool podeCobrarNoOutroGrupo = limite.PodeCobrar(IdDoOutroGrupo);

        podeRecusarNoOutroGrupo.Should().BeTrue();
        podeCobrarNoOutroGrupo.Should().BeTrue();
    }

    // Quanto falta para a próxima cobrança, e zero quando a janela fechou
    [Fact]
    public void DeveDizerQuantoFaltaParaAProximaCobrancaQuandoAJanelaEstaAberta()
    {
        LimiteDeRespostas limite = CriarLimite(new ConstrutorDeOpcoesDoBot().Build());
        limite.RegistrarCobrancaEnviada(IdDoGrupo);
        _relogio.Advance(TimeSpan.FromMinutes(12));

        TimeSpan faltaAgora = limite.TempoAteAProximaCobranca(IdDoGrupo);
        _relogio.Advance(TimeSpan.FromMinutes(20));
        TimeSpan faltaDepois = limite.TempoAteAProximaCobranca(IdDoGrupo);

        faltaAgora.Should().Be(TimeSpan.FromMinutes(18));
        faltaDepois.Should().Be(TimeSpan.Zero);
        limite.TempoAteAProximaCobranca(IdDoOutroGrupo).Should().Be(TimeSpan.Zero);
    }

    private LimiteDeRespostas CriarLimite(OpcoesDoBot opcoes) => new(_relogio, Options.Create(opcoes));
}
