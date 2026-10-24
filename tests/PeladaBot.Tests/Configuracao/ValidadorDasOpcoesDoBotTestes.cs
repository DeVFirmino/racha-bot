using System.Globalization;
using Microsoft.Extensions.Options;
using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Configuracao;

public sealed class ValidadorDasOpcoesDoBotTestes
{
    private readonly ValidadorDasOpcoesDoBot _validador = new();

    [Fact]
    public void DevePassarQuandoAConfiguracaoEhValida()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void DevePassarQuandoAPresidenciaTemSoOPresidente()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComPresidencia(ConstrutorDeOpcoesDoBot.Membro("35600000101@c.us")).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void DevePassarQuandoOsLimitesSaoReduzidosAosMenoresValoresPositivos()
    {
        TimeSpan menor = TimeSpan.FromTicks(1);
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComMaximoDeMensagensDoDisjuntor(1)
            .ComLimiteDeDuracao(nameof(OpcoesDeLimites.JanelaDeRespostaNaoPresidente), menor)
            .ComLimiteDeDuracao(nameof(OpcoesDeLimites.JanelaDeCobranca), menor)
            .ComLimiteDeDuracao(nameof(OpcoesDeLimites.JanelaDoDisjuntor), menor)
            .ComLimiteDeDuracao(nameof(OpcoesDeLimites.IdadeMaximaDoComando), menor)
            .ComLimiteDeDuracao(nameof(OpcoesDeLimites.IdadeMaximaDaLista), menor)
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void DeveFalharQuandoNaoHaExatamenteUmGrupo(int quantidadeDeGrupos)
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComQuantidadeDeGrupos(quantidadeDeGrupos).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.QuantidadeDeGruposErrada, quantidadeDeGrupos));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void DeveFalharQuandoOIdDoGrupoEstaEmBranco(string idDoGrupo)
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComIdDoGrupo(idDoGrupo).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.IdDoGrupoEmBranco, 0));
    }

    [Fact]
    public void DeveFalharQuandoNaoHaPresidencia()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComPresidencia().Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.TamanhoDaPresidenciaErrado, 0, 0));
    }

    [Fact]
    public void DeveFalharQuandoAPresidenciaTemTresMembros()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComPresidencia(
                ConstrutorDeOpcoesDoBot.Membro("35600000101@c.us"),
                ConstrutorDeOpcoesDoBot.Membro("35600000303@c.us"),
                ConstrutorDeOpcoesDoBot.Membro("35600000404@c.us"))
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.TamanhoDaPresidenciaErrado, 0, 3));
    }

    [Fact]
    public void DeveFalharQuandoUmMembroDaPresidenciaNaoTemIds()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComPresidencia(ConstrutorDeOpcoesDoBot.Membro("35600000101@c.us"), ConstrutorDeOpcoesDoBot.Membro())
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.MembroSemIds, 0, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void DeveFalharQuandoUmMembroDaPresidenciaTemIdEmBranco(string idEmBranco)
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComPresidencia(ConstrutorDeOpcoesDoBot.Membro("35600000101@c.us", idEmBranco))
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.IdDoMembroEmBranco, 0, 0, 1));
    }

    [Theory]
    [InlineData(nameof(OpcoesDeLimites.JanelaDeRespostaNaoPresidente), 0)]
    [InlineData(nameof(OpcoesDeLimites.JanelaDeCobranca), 0)]
    [InlineData(nameof(OpcoesDeLimites.JanelaDoDisjuntor), 0)]
    [InlineData(nameof(OpcoesDeLimites.IdadeMaximaDoComando), 0)]
    [InlineData(nameof(OpcoesDeLimites.IdadeMaximaDaLista), 0)]
    [InlineData(nameof(OpcoesDeLimites.IdadeMaximaDoComando), -120)]
    public void DeveFalharQuandoOLimiteDeDuracaoNaoEhPositivo(string limite, int segundos)
    {
        TimeSpan valor = TimeSpan.FromSeconds(segundos);
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComLimiteDeDuracao(limite, valor).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.LimiteNaoPositivo, limite, valor));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void DeveFalharQuandoOMaximoDeMensagensDoDisjuntorNaoEhPositivo(int maximoDeMensagens)
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComMaximoDeMensagensDoDisjuntor(maximoDeMensagens).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(
            Falha(ValidadorDasOpcoesDoBot.LimiteNaoPositivo, nameof(OpcoesDeLimites.MaximoDeMensagensDoDisjuntor), maximoDeMensagens));
    }

    // Teto dos limites
    [Theory]
    [InlineData(nameof(OpcoesDeLimites.JanelaDeRespostaNaoPresidente), "1.00:00:00")]
    [InlineData(nameof(OpcoesDeLimites.JanelaDeCobranca), "1.00:00:00")]
    [InlineData(nameof(OpcoesDeLimites.JanelaDoDisjuntor), "1.00:00:00")]
    [InlineData(nameof(OpcoesDeLimites.IdadeMaximaDoComando), "00:10:00")]
    [InlineData(nameof(OpcoesDeLimites.IdadeMaximaDaLista), "7.00:00:00")]
    public void DevePassarQuandoOLimiteDeDuracaoEstaNoSeuTeto(string limite, string teto)
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComLimiteDeDuracao(limite, Duracao(teto)).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Succeeded.Should().BeTrue();
    }

    // Teto dos limites
    [Theory]
    [InlineData(nameof(OpcoesDeLimites.JanelaDeRespostaNaoPresidente), "1.00:00:00")]
    [InlineData(nameof(OpcoesDeLimites.JanelaDeCobranca), "1.00:00:00")]
    [InlineData(nameof(OpcoesDeLimites.JanelaDoDisjuntor), "1.00:00:00")]
    [InlineData(nameof(OpcoesDeLimites.IdadeMaximaDoComando), "00:10:00")]
    [InlineData(nameof(OpcoesDeLimites.IdadeMaximaDaLista), "7.00:00:00")]
    public void DeveFalharQuandoOLimiteDeDuracaoPassaOTetoPorPouco(string limite, string teto)
    {
        TimeSpan umPoucoAcima = Duracao(teto) + TimeSpan.FromTicks(1);
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComLimiteDeDuracao(limite, umPoucoAcima).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.DuracaoMuitoLonga, limite, Duracao(teto), umPoucoAcima));
    }

    // Teto dos limites
    [Fact]
    public void DevePassarQuandoOMaximoDeMensagensDoDisjuntorEstaNoSeuTeto()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComMaximoDeMensagensDoDisjuntor(1000).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Succeeded.Should().BeTrue();
    }

    // Teto dos limites
    [Fact]
    public void DeveFalharQuandoOMaximoDeMensagensDoDisjuntorPassaOTetoPorPouco()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComMaximoDeMensagensDoDisjuntor(1001).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(
            Falha(ValidadorDasOpcoesDoBot.QuantidadeMuitoGrande, nameof(OpcoesDeLimites.MaximoDeMensagensDoDisjuntor), 1000, 1001));
    }

    // ID com espaço nas pontas não bate com o remetente
    [Fact]
    public void DeveFalharQuandoIdsComecamOuTerminamComEspaco()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComIdDoGrupo(" 120363000000001234@g.us")
            .ComPresidencia(ConstrutorDeOpcoesDoBot.Membro("35600000101@c.us "))
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(
            Falha(ValidadorDasOpcoesDoBot.IdDoGrupoComEspacos, 0),
            Falha(ValidadorDasOpcoesDoBot.IdDoMembroComEspacos, 0, 0, 0));
    }

    [Fact]
    public void DeveFalharQuandoNaoHaFrasesDeRodape()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComFrasesDoRodape().Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.SemFrasesDeRodape, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void DeveFalharQuandoUmaFraseDeRodapeEstaEmBranco(string fraseEmBranco)
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComFrasesDoRodape("⏰ Sem atraso hoje, hein!", fraseEmBranco).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.FraseDeRodapeEmBranco, 0, 1));
    }

    // A legenda sempre tem ao menos um tema
    [Fact]
    public void DeveFalharQuandoNaoHaModelosDaLegenda()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComModelosDaLegenda().Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.SemModelosDaLegenda, 0));
    }

    // Cada campo do tema preenchido, e a linha do horário com {hora}
    [Fact]
    public void DeveFalharQuandoUmModeloDaLegendaTemCampoEmBrancoOuNaoTemAHora()
    {
        OpcoesDoModeloDaLegenda modelo = ConstrutorDeOpcoesDoBot.ModeloDaLegenda("🏆 *AC | CHAMPIONS*", "*Sorteio*");
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComModelosDaLegenda(modelo, new OpcoesDoModeloDaLegenda { Cabecalho = "x", LinhaDoHorario = "{hora}", ApelidoLaranja = "a", ApelidoVerde = "b", ApelidoCinza = "c", ApelidoAzul = "d", ApelidoSemColete = " " })
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(
            Falha(ValidadorDasOpcoesDoBot.LinhaDoHorarioSemHora, 0, 0),
            Falha(ValidadorDasOpcoesDoBot.CampoDoModeloEmBranco, 0, 1, nameof(OpcoesDoModeloDaLegenda.ApelidoSemColete)));
    }

    // Um apelido avulso em branco sairia como "VERDE | " na legenda
    [Fact]
    public void DeveFalharQuandoUmApelidoAvulsoEstaEmBranco()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComApelidosAvulsos(new OpcoesDosApelidosAvulsos { Verde = ["SPORTING VERDE", " "] })
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.ApelidoAvulsoEmBranco, 0, "Verde", 1));
    }

    // O !cobrar sempre tem ao menos uma mensagem
    [Fact]
    public void DeveFalharQuandoNaoHaMensagensDeCobranca()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComMensagensDeCobranca().Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.SemMensagensDeCobranca, 0));
    }

    // 
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void DeveFalharQuandoUmaMensagemDeCobrancaEstaEmBranco(string mensagemEmBranco)
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComMensagensDeCobranca("💶 *Bora acertar!*", mensagemEmBranco).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.MensagemDeCobrancaEmBranco, 0, 1));
    }

    // Uma cobrança só pode ter as marcas {nome}, {quantidade} e {s}
    [Fact]
    public void DeveFalharQuandoUmaMensagemDeCobrancaTemUmaMarcaDesconhecida()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComMensagensDeCobranca("💸 {nome} deve {multa}", "⏳ {quantidade} transferência{s} de {nome}: {valor} em {link}")
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.MarcaDesconhecidaNaCobranca, 0, 0, "{multa}"));
    }

    // Identidade do grupo: um escudo configurado tem que existir, ou o bot não sobe
    [Fact]
    public void DeveFalharQuandoOCaminhoDoEscudoNaoExiste()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComIdentidadeDoCard("PELADA", Path.Combine(Path.GetTempPath(), "escudo-que-nao-existe.jpeg"))
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(Falha(ValidadorDasOpcoesDoBot.EscudoInexistente, 0));
    }

    // Identidade do grupo: sem escudo configurado, nada a conferir
    [Fact]
    public void DevePassarQuandoOGrupoTemNomeNoCardESemEscudo()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComIdentidadeDoCard("PELADA", string.Empty).Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void DeveReportarTodaFalhaQuandoVariasConfiguracoesSaoInvalidas()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComIdDoGrupo(string.Empty)
            .ComPresidencia(ConstrutorDeOpcoesDoBot.Membro(string.Empty))
            .ComMaximoDeMensagensDoDisjuntor(0)
            .Build();

        ValidateOptionsResult resultado = _validador.Validate(Options.DefaultName, opcoes);

        resultado.Failures.Should().Equal(
            Falha(ValidadorDasOpcoesDoBot.IdDoGrupoEmBranco, 0),
            Falha(ValidadorDasOpcoesDoBot.IdDoMembroEmBranco, 0, 0, 0),
            Falha(ValidadorDasOpcoesDoBot.LimiteNaoPositivo, nameof(OpcoesDeLimites.MaximoDeMensagensDoDisjuntor), 0));
    }

    private static string Falha(string mensagem, params object[] argumentos) =>
        string.Format(CultureInfo.InvariantCulture, mensagem, argumentos);

    private static TimeSpan Duracao(string valor) => TimeSpan.Parse(valor, CultureInfo.InvariantCulture);
}
