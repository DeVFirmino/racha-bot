using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PeladaBot.Acesso;
using PeladaBot.Comandos;
using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Acesso;

public sealed class ControleDeAcessoTestes
{
    private const string IdDoGrupo = "120363000000001234@g.us";
    private const string NomeDoPresidente = "Presidente Fulano";
    private const string IdDoTelefoneDoPresidente = "35600000101@c.us";
    private const string LidDoPresidente = "100000000000202@lid";
    private const string IdDoTelefoneDoVice = "35600000303@c.us";
    private const string IdDoIntegrante = "35600000404@c.us";
    private const string IdDoOutroIntegrante = "35600000505@c.us";
    private const string RespostaSoOPresidente = "Esse comando é só para presidente ou admin cadastrados no bot. Para ver quem falta pagar, use !pendentes.";

    private static readonly DateTimeOffset _diaDoJogo = new(2026, 10, 14, 19, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan _janelaDaSpec = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _umMinuto = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _umSegundo = TimeSpan.FromSeconds(1);

    private readonly FakeTimeProvider _relogio = new(_diaDoJogo);

    private readonly OpcoesDoBot _opcoes = new ConstrutorDeOpcoesDoBot()
        .ComIdDoGrupo(IdDoGrupo)
        .ComPresidencia(
            new OpcoesDoMembroDaPresidencia { Nome = NomeDoPresidente, Ids = [IdDoTelefoneDoPresidente, LidDoPresidente] },
            new OpcoesDoMembroDaPresidencia { Nome = "Vice Ciclano", Ids = [IdDoTelefoneDoVice] })
        .Build();

    [Fact]
    public void DevePermitirOSorteioQuandoOVicePede()
    {
        ControleDeAcesso controle = CriarControle();

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoTelefoneDoVice, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Permitido>();
    }

    [Fact]
    public void DeveRecusarOSorteioComARespostaSoOPresidenteQuandoOutroAdminDoGrupoPede()
    {
        ControleDeAcesso controle = CriarControle();

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Recusado>().Which.Resposta.Should().Be(RespostaSoOPresidente);
    }

    [Theory]
    [InlineData(IdDoTelefoneDoPresidente)]
    [InlineData(LidDoPresidente)]
    public void DevePermitirOSorteioQuandoOPresidenteEscreveDeQualquerUmDosSeusIds(string idDoRemetente)
    {
        ControleDeAcesso controle = CriarControle();

        Admissao admissao = controle.Verificar(IdDoGrupo, idDoRemetente, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Permitido>();
    }

    // O controle só recebe IDs: o nome exibido do presidente, usado como se fosse um ID, não autoriza
    [Fact]
    public void DeveRecusarOSorteioQuandoORemetenteSoCompartilhaONomeDoPresidente()
    {
        ControleDeAcesso controle = CriarControle();

        Admissao admissao = controle.Verificar(IdDoGrupo, NomeDoPresidente, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Recusado>().Which.Resposta.Should().Be(RespostaSoOPresidente);
    }

    // Remetente desconhecido
    [Theory]
    [InlineData(TipoDeComando.Sortear)]
    public void DeveRecusarComARespostaSoOPresidenteQuandoUmRemetenteDesconhecidoPedeUmComandoDaPresidencia(TipoDeComando comando)
    {
        ControleDeAcesso controle = CriarControle();

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, comando);

        admissao.Should().BeOfType<Recusado>().Which.Resposta.Should().Be(RespostaSoOPresidente);
    }

    [Theory]
    [InlineData(IdDoTelefoneDoPresidente)]
    [InlineData(IdDoTelefoneDoVice)]
    public void DevePermitirCobrancaQuandoAPresidenciaPede(string idDoRemetente)
    {
        ControleDeAcesso controle = CriarControle();

        Admissao admissao = controle.Verificar(IdDoGrupo, idDoRemetente, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Permitido>();
    }

    // O !ajuda é do presidente e do vice
    [Theory]
    [InlineData(LidDoPresidente)]
    [InlineData(IdDoTelefoneDoVice)]
    public void DevePermitirAjudaQuandoAPresidenciaPede(string idDoRemetente)
    {
        ControleDeAcesso controle = CriarControle();

        Admissao admissao = controle.Verificar(IdDoGrupo, idDoRemetente, TipoDeComando.Ajuda);

        admissao.Should().BeOfType<Permitido>();
    }

    [Fact]
    public void DeveIgnorarOSorteioQuandoOMesmoRemetentePedeDeNovoMenosDeDezMinutosDepoisDaRecusa()
    {
        ControleDeAcesso controle = CriarControle();
        RecusarEEnviar(controle, IdDoIntegrante, TipoDeComando.Sortear);
        _relogio.Advance(_janelaDaSpec - _umSegundo);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Ignorado>();
    }

    // O !ajuda tem janela própria; uma recusa de !sortear ou uma resenha de espera não a gastam
    [Fact]
    public void DevePermitirAjudaQuandoOMesmoRemetenteAcabouDeSerRecusadoEmOutroComando()
    {
        ControleDeAcesso controle = CriarControle();
        RecusarEEnviar(controle, IdDoIntegrante, TipoDeComando.Sortear);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Ajuda);

        admissao.Should().BeOfType<Permitido>();
    }

    // Quem recebeu a resenha de espera do !pendentes ainda recebe o guia
    [Fact]
    public void DevePermitirAjudaQuandoOMesmoRemetenteAcabouDeReceberAResenhaDeEspera()
    {
        ControleDeAcesso controle = CriarControle();
        PermitirEEnviar(controle, IdDoOutroIntegrante, TipoDeComando.Cobrar);
        RecusarEEnviar(controle, IdDoIntegrante, TipoDeComando.Cobrar);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Ajuda);

        admissao.Should().BeOfType<Permitido>();
    }

    // Depois do guia, o mesmo integrante espera a janela para recebê-lo de novo
    [Fact]
    public void DeveIgnorarAjudaQuandoOMesmoRemetenteRecebeuOGuiaMenosDeDezMinutosAtras()
    {
        ControleDeAcesso controle = CriarControle();
        PermitirEEnviar(controle, IdDoIntegrante, TipoDeComando.Ajuda);
        _relogio.Advance(_janelaDaSpec - _umSegundo);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Ajuda);

        admissao.Should().BeOfType<Ignorado>();
    }

    [Fact]
    public void DeveRecusarDeNovoQuandoDezMinutosPassaramDesdeQueARecusaSaiu()
    {
        ControleDeAcesso controle = CriarControle();
        RecusarEEnviar(controle, IdDoIntegrante, TipoDeComando.Sortear);
        _relogio.Advance(_janelaDaSpec);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Recusado>().Which.Resposta.Should().Be(RespostaSoOPresidente);
    }

    // A recusa que não saiu não gasta a janela
    [Fact]
    public void DeveRecusarDeNovoQuandoARecusaAnteriorNuncaFoiEnviada()
    {
        ControleDeAcesso controle = CriarControle();
        controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Sortear);
        _relogio.Advance(_umMinuto);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Recusado>();
    }

    // Cada pessoa tem a sua janela
    [Fact]
    public void DeveRecusarOutroRemetenteQuandoSoOPrimeiroRecebeuARecusa()
    {
        ControleDeAcesso controle = CriarControle();
        RecusarEEnviar(controle, IdDoIntegrante, TipoDeComando.Sortear);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoOutroIntegrante, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Recusado>();
    }

    [Fact]
    public void DeveResponderATodoSorteioQuandoOPresidentePedeCincoVezesSeguidas()
    {
        ControleDeAcesso controle = CriarControle();
        DisjuntorDeEnvio disjuntor = CriarDisjuntor();

        int respostas = RespostasSeguidas(controle, disjuntor, IdDoTelefoneDoPresidente, TipoDeComando.Sortear, 5);

        respostas.Should().Be(5);
    }

    // Inclusive o presidente: o controle deixa passar, o disjuntor segura
    [Fact]
    public void NaoDeveResponderAoPresidenteQuandoTrintaMensagensSairamNaUltimaHora()
    {
        ControleDeAcesso controle = CriarControle();
        DisjuntorDeEnvio disjuntor = CriarDisjuntor();
        RespostasSeguidas(controle, disjuntor, LidDoPresidente, TipoDeComando.Ajuda, 30);
        _relogio.Advance(TimeSpan.FromMinutes(59));

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoTelefoneDoPresidente, TipoDeComando.Sortear);
        bool reservado = disjuntor.TentarReservar();

        admissao.Should().BeOfType<Permitido>();
        reservado.Should().BeFalse();
    }

    // Fora da presidência, a primeira cobrança no grupo sai
    [Fact]
    public void DevePermitirCobrancaQuandoUmIntegrantePedeENaoHouveCobrancaRecente()
    {
        ControleDeAcesso controle = CriarControle();

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Permitido>();
    }

    // Dentro dos 30 min, quem está fora da presidência recebe a resenha de espera, com quanto falta
    [Fact]
    public void DeveResponderComAResenhaDeEsperaQuandoOutroIntegranteCobraDentroDaJanela()
    {
        ControleDeAcesso controle = CriarControle();
        PermitirEEnviar(controle, IdDoIntegrante, TipoDeComando.Cobrar);
        _relogio.Advance(TimeSpan.FromMinutes(12));

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoOutroIntegrante, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Recusado>().Which.Resposta.Should().Contain("18 minutos");
    }

    // Depois da resenha de espera, o mesmo integrante fica sem resposta até a janela de recusa passar
    [Fact]
    public void DeveIgnorarOIntegranteQuandoEleJaRecebeuAResenhaDeEsperaNessaJanela()
    {
        ControleDeAcesso controle = CriarControle();
        PermitirEEnviar(controle, IdDoIntegrante, TipoDeComando.Cobrar);
        RecusarEEnviar(controle, IdDoOutroIntegrante, TipoDeComando.Cobrar);
        _relogio.Advance(_janelaDaSpec - _umSegundo);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoOutroIntegrante, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Ignorado>();
    }

    // A resenha de espera não gasta a janela de cobrança do grupo
    [Fact]
    public void DevePermitirACobrancaQuandoTrintaMinutosPassaramMesmoDepoisDeUmaResenhaDeEspera()
    {
        ControleDeAcesso controle = CriarControle();
        PermitirEEnviar(controle, IdDoIntegrante, TipoDeComando.Cobrar);
        _relogio.Advance(TimeSpan.FromMinutes(29));
        RecusarEEnviar(controle, IdDoOutroIntegrante, TipoDeComando.Cobrar);
        _relogio.Advance(_umMinuto);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Permitido>();
    }

    // Presidente e vice cobram a qualquer hora, mesmo logo depois de outra cobrança
    [Theory]
    [InlineData(IdDoTelefoneDoPresidente)]
    [InlineData(IdDoTelefoneDoVice)]
    public void DevePermitirCobrancaDaPresidenciaQuandoOGrupoFoiCobradoUmMinutoAtras(string idDoRemetente)
    {
        ControleDeAcesso controle = CriarControle();
        PermitirEEnviar(controle, IdDoIntegrante, TipoDeComando.Cobrar);
        _relogio.Advance(_umMinuto);
        PermitirEEnviar(controle, idDoRemetente, TipoDeComando.Cobrar);

        Admissao admissao = controle.Verificar(IdDoGrupo, idDoRemetente, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Permitido>();
    }

    // A janela conta a partir da última cobrança, inclusive a da presidência
    [Fact]
    public void DeveSegurarOIntegranteQuandoAPresidenciaCobrouHaMenosDeTrintaMinutos()
    {
        ControleDeAcesso controle = CriarControle();
        PermitirEEnviar(controle, IdDoIntegrante, TipoDeComando.Cobrar);
        _relogio.Advance(TimeSpan.FromMinutes(25));
        PermitirEEnviar(controle, IdDoTelefoneDoPresidente, TipoDeComando.Cobrar);
        _relogio.Advance(TimeSpan.FromMinutes(25));

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoOutroIntegrante, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Recusado>().Which.Resposta.Should().Contain("5 minutos");
    }

    // A janela fecha exatamente em 30 minutos
    [Fact]
    public void DevePermitirACobrancaDeUmIntegranteDeNovoQuandoTrintaMinutosPassaram()
    {
        ControleDeAcesso controle = CriarControle();
        PermitirEEnviar(controle, IdDoIntegrante, TipoDeComando.Cobrar);
        _relogio.Advance(TimeSpan.FromMinutes(30));

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoOutroIntegrante, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Permitido>();
    }

    // A cobrança que não saiu não gasta a janela
    [Fact]
    public void DevePermitirCobrancaDeOutroIntegranteQuandoACobrancaAnteriorNuncaFoiEnviada()
    {
        ControleDeAcesso controle = CriarControle();
        controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Cobrar);
        _relogio.Advance(_umMinuto);

        Admissao admissao = controle.Verificar(IdDoGrupo, IdDoOutroIntegrante, TipoDeComando.Cobrar);

        admissao.Should().BeOfType<Permitido>();
    }

    // E Authorization (o log diz só que o ID não é da presidência, mascarado)
    [Fact]
    public void DeveLogarCadaRecusaESilencioComOIdDoRemetenteMascaradoQuandoAlguemForaDaPresidenciaEhRetido()
    {
        ColetorDeLogs logs = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(logs));
        ControleDeAcesso controle = CriarControle(loggerFactory.CreateLogger<ControleDeAcesso>());
        RecusarEEnviar(controle, IdDoIntegrante, TipoDeComando.Sortear);
        PermitirEEnviar(controle, IdDoOutroIntegrante, TipoDeComando.Cobrar);
        PermitirEEnviar(controle, IdDoIntegrante, TipoDeComando.Ajuda);

        controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Ajuda);
        controle.Verificar(IdDoGrupo, IdDoIntegrante, TipoDeComando.Cobrar);

        logs.Entradas.Should().HaveCount(3).And.AllSatisfy(log =>
        {
            log.Nivel.Should().Be(LogLevel.Information);
            log.Mensagem.Should().Contain("***0404@c.us").And.NotContain("35600000404");
        });
    }

    // A janela vale para a aplicação inteira enquanto o processo vive, não para cada escopo
    [Fact]
    public void DeveManterAJanelaDeRecusaEntreEscoposQuandoAAplicacaoRoda()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();
        using IServiceScope primeiroEscopo = fabrica.Services.CreateScope();
        using IServiceScope segundoEscopo = fabrica.Services.CreateScope();
        ControleDeAcesso primeiroControle = primeiroEscopo.ServiceProvider.GetRequiredService<ControleDeAcesso>();
        primeiroControle.Verificar(FabricaDaAplicacaoDoBot.IdDoGrupo, IdDoIntegrante, TipoDeComando.Sortear);
        primeiroControle.RegistrarRespostaEnviada(FabricaDaAplicacaoDoBot.IdDoGrupo, IdDoIntegrante, TipoDeComando.Sortear);

        Admissao admissao = segundoEscopo.ServiceProvider.GetRequiredService<ControleDeAcesso>()
            .Verificar(FabricaDaAplicacaoDoBot.IdDoGrupo, IdDoIntegrante, TipoDeComando.Sortear);

        admissao.Should().BeOfType<Ignorado>();
    }

    /// <summary>O que um caso de uso faz com um comando: perguntar ao controle, reservar o disjuntor antes de enviar, e registrar a resposta quando ela sai.</summary>
    private static bool BotResponde(ControleDeAcesso controle, DisjuntorDeEnvio disjuntor, string idDoRemetente, TipoDeComando comando)
    {
        if (controle.Verificar(IdDoGrupo, idDoRemetente, comando) is Ignorado || disjuntor.TentarReservar() is false)
        {
            return false;
        }

        controle.RegistrarRespostaEnviada(IdDoGrupo, idDoRemetente, comando);

        return true;
    }

    /// <summary>Envia o mesmo comando <paramref name="pedidos"/> vezes no instante atual e retorna quantos receberam resposta.</summary>
    private static int RespostasSeguidas(
        ControleDeAcesso controle, DisjuntorDeEnvio disjuntor, string idDoRemetente, TipoDeComando comando, int pedidos) =>
        Enumerable.Range(0, pedidos).Count(pedido => BotResponde(controle, disjuntor, idDoRemetente, comando));

    private static void RecusarEEnviar(ControleDeAcesso controle, string idDoRemetente, TipoDeComando comando)
    {
        controle.Verificar(IdDoGrupo, idDoRemetente, comando).Should().BeOfType<Recusado>();
        controle.RegistrarRespostaEnviada(IdDoGrupo, idDoRemetente, comando);
    }

    private static void PermitirEEnviar(ControleDeAcesso controle, string idDoRemetente, TipoDeComando comando)
    {
        controle.Verificar(IdDoGrupo, idDoRemetente, comando).Should().BeOfType<Permitido>();
        controle.RegistrarRespostaEnviada(IdDoGrupo, idDoRemetente, comando);
    }

    private ControleDeAcesso CriarControle() => CriarControle(NullLogger<ControleDeAcesso>.Instance);

    private ControleDeAcesso CriarControle(ILogger<ControleDeAcesso> logger)
    {
        IOptions<OpcoesDoBot> opcoes = Options.Create(_opcoes);

        return new ControleDeAcesso(new Presidencia(opcoes), new LimiteDeRespostas(_relogio, opcoes), new Random(14), logger);
    }

    private DisjuntorDeEnvio CriarDisjuntor() => new(_relogio, Options.Create(_opcoes));
}
