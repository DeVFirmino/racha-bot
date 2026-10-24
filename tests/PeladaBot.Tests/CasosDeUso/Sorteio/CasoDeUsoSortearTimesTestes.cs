using Microsoft.Extensions.Logging;
using PeladaBot.CasosDeUso;
using PeladaBot.CasosDeUso.Sorteio.SortearTimes;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Tests.UtilitariosDeTeste;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.CasosDeUso.Sorteio;

public sealed class CasoDeUsoSortearTimesTestes : IDisposable
{
    private const string IdDoComando = "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000101@c.us";
    private const string RespostaSoOPresidente = "Esse comando é só para presidente ou admin cadastrados no bot. Para ver quem falta pagar, use !pendentes.";

    /// <summary>O grupo de teste não tem título e o sorteio sai às 21:00 no fuso da suíte.</summary>
    private const string CabecalhoDaLegenda = "⚽ *PELADA*\n🕘 *SORTEIO — 21:00*";

    /// <summary>O separador do fim e a única frase do grupo de teste, como está na configuração.</summary>
    private const string FimDaLegenda = "━━━━━━━━━━━━━━━━━━\n⏰ Sem atraso hoje, hein!";

    private static readonly byte[] _assinaturaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly ConstrutorDeCasoDeUso _bot = new();

    public void Dispose() => _bot.Dispose();

    // Dois times cabem num card só
    [Fact]
    public async Task DeveEnviarUmaImagemComALegendaCitandoOComandoQuandoOPresidenteCitaUmaLista()
    {
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 10)));

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Imagem);
        mensagem.IdDoChat.Should().Be(ConstrutorDeCasoDeUso.IdDoGrupo);
        mensagem.IdDaMensagemRespondida.Should().Be(IdDoComando);
        mensagem.Png.Should().StartWith(_assinaturaPng);
        mensagem.Texto.Should().StartWith($"{CabecalhoDaLegenda}\n━").And.NotContain("📋");
    }

    // Três times, um card por campo, os dois citando o comando e a legenda só no primeiro
    [Fact]
    public async Task DeveEnviarUmCardPorCampoComALegendaSoNoPrimeiroQuandoOSorteioTemTresTimes()
    {
        // 12 de linha e 2 goleiros fixos, numa quadra só: dois times de 5 e um terceiro, incompleto.
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 12)));

        IReadOnlyList<ChamadaDoMensageiro> mensagens = _bot.Mensageiro.Mensagens;
        mensagens.Should().HaveCount(2).And.AllSatisfy(mensagem =>
        {
            mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Imagem);
            mensagem.IdDaMensagemRespondida.Should().Be(IdDoComando);
            mensagem.Png.Should().StartWith(_assinaturaPng);
        });
        mensagens[0].Texto.Should().StartWith($"{CabecalhoDaLegenda}\n━").And.EndWith(FimDaLegenda);
        mensagens[1].Texto.Should().BeNull();
        mensagens[1].Png.Should().NotEqual(mensagens[0].Png);
    }

    // Cinco times saem em três cards, a legenda só no primeiro
    [Fact]
    public async Task DeveEnviarTresCardsQuandoOSorteioTemCincoTimes()
    {
        // 25 pagos e 2 goleiros fixos, mais de uma quadra: 2 times com fixo, 2 de rodízio e 1 incompleto.
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 25)));

        IReadOnlyList<ChamadaDoMensageiro> mensagens = _bot.Mensageiro.Mensagens;
        mensagens.Should().HaveCount(3).And.OnlyContain(mensagem => mensagem.Tipo == TipoDeChamadaDoMensageiro.Imagem);
        mensagens[0].Texto.Should().StartWith(CabecalhoDaLegenda);
        mensagens.Skip(1).Should().OnlyContain(mensagem => mensagem.Texto == null);
    }

    // O WAHA recusou o segundo card, então a legenda vai sozinha, com todos os times
    [Fact]
    public async Task DeveEnviarALegendaSozinhaQuandoOWahaRejeitaOSegundoCard()
    {
        _bot.Mensageiro.ResponderImagens(new Enviado(), new Rejeitado { Motivo = "WAHA respondeu 422" });

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 12)));

        IReadOnlyList<ChamadaDoMensageiro> mensagens = _bot.Mensageiro.Mensagens;
        mensagens.Select(mensagem => mensagem.Tipo).Should()
            .Equal(TipoDeChamadaDoMensageiro.Imagem, TipoDeChamadaDoMensageiro.Imagem, TipoDeChamadaDoMensageiro.Texto);
        mensagens[2].Texto.Should().Be(mensagens[0].Texto);
        mensagens[2].IdDaMensagemRespondida.Should().Be(IdDoComando);
        _bot.Logs.Entradas.Should().Contain(log => log.Nivel == LogLevel.Error && log.Mensagem.Contains("rejeitou o card"));
    }

    // O segundo card ficou incerto; a legenda não vai, para não sair em dobro
    [Fact]
    public async Task NaoDeveEnviarMaisNadaQuandoOEnvioDoSegundoCardEhIncerto()
    {
        _bot.Mensageiro.ResponderImagens(new Enviado(), new Incerto { Motivo = "WAHA não respondeu a tempo" });

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 12)));

        _bot.Mensageiro.Mensagens.Select(mensagem => mensagem.Tipo).Should()
            .Equal(TipoDeChamadaDoMensageiro.Imagem, TipoDeChamadaDoMensageiro.Imagem);
        _bot.Logs.Entradas.Should().Contain(log => log.Nivel == LogLevel.Error && log.Mensagem.Contains("já pode estar no grupo"));
    }

    [Fact]
    public async Task DeveTerminarALegendaComAFraseDePontualidadeConfiguradaQuandoOsTimesSaoSorteados()
    {
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 8)));

        _bot.Mensageiro.Mensagens.Should().ContainSingle().Which.Texto.Should().EndWith($"\n{FimDaLegenda}");
    }

    // Sem citar, a legenda traz a origem da lista no fuso da suíte
    [Fact]
    public async Task DeveUsarAUltimaListaDoPresidenteEMostrarDeOndeVeioQuandoOComandoNaoCitaNada()
    {
        _bot.Historico.Com(new ConstrutorDeMensagemDoGrupo()
            .De(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, "Presidente Fulano")
            .EnviadaEm(new DateTimeOffset(2026, 10, 12, 11, 29, 0, TimeSpan.Zero))
            .ComTexto(ListaCom(pagos: 10))
            .Build());

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, citada: null);

        _bot.Mensageiro.Mensagens.Should().ContainSingle().Which.Texto.Should()
            .StartWith($"{CabecalhoDaLegenda}\n📋 Lista enviada por Presidente Fulano em 12/10 às 13:29\n");
    }

    // Um jogador sem ✅ já basta para esperar todo mundo pagar, sem imagem
    [Fact]
    public async Task DeveResponderQuemFaltaPagarSemSortearENemEnviarImagemQuandoUmJogadorNaoPagou()
    {
        await Sortear(
            ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
            CitacaoDe(new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(2).ComJogadoresNaoPagos("Paco").Build()));

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);
        mensagem.Texto.Should().Be("🚨 *Quem ainda não pagou, favor acertar!*\n\n💸 *Falta pagar (1):*\n👀 Paco\n\nPara sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).");
    }

    // A cobrança da configuração lista os pendentes da lista, na ordem dela, com a quantidade certa
    [Fact]
    public async Task DeveCobrarComOsNomesDaListaQuandoAMensagemDeCobrancaTemAsMarcas()
    {
        _bot.ComOpcoes(new ConstrutorDeOpcoesDoBot()
            .ComFrasesDoRodape(ConstrutorDeCasoDeUso.FraseDoRodape)
            .ComMensagensDeCobranca("💸 *Falta pagar ({quantidade}):*\n👀 {nome}")
            .Build());
        string lista = new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(9).ComJogadoresNaoPagos("Paco", "Luan Ferraz").Build();

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(lista));

        OUnicoTexto().Should().Be("""
            💸 *Falta pagar (2):*
            👀 Paco
            👀 Luan Ferraz

            Para sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).
            """);
    }

    // Várias pessoas sem ✅ aparecem na ordem da lista, e o sorteio continua sem sair
    [Fact]
    public async Task DeveResponderQuemFaltaPagarNaOrdemDaListaSemSortearQuandoVariosJogadoresNaoPagaram()
    {
        await Sortear(
            ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
            CitacaoDe(new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresNaoPagos("Paco", "Luan Ferraz").Build()));

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);
        mensagem.Texto.Should().Be("🚨 *Quem ainda não pagou, favor acertar!*\n\n💸 *Falta pagar (2):*\n👀 Paco\n👀 Luan Ferraz\n\nPara sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).");
    }

    // A resposta de pendência também mostra de onde veio a lista, quando ela não foi citada
    [Fact]
    public async Task DeveMostrarDeOndeVeioALIstaQuandoFaltaAlguemPagarEOComandoNaoCitaNada()
    {
        _bot.Historico.Com(new ConstrutorDeMensagemDoGrupo()
            .De(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, "Presidente Fulano")
            .EnviadaEm(new DateTimeOffset(2026, 10, 12, 11, 29, 0, TimeSpan.Zero))
            .ComTexto(new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresNaoPagos("Paco").Build())
            .Build());

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, citada: null);

        OUnicoTexto().Should().Be("""
            🚨 *Quem ainda não pagou, favor acertar!*

            💸 *Falta pagar (1):*
            👀 Paco

            Para sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).
            📋 Lista enviada por Presidente Fulano em 12/10 às 13:29
            """);
    }

    // "!sortear pagos" ignora quem está sem ✅ e avisa na legenda
    [Fact]
    public async Task DeveSortearIgnorandoQuemNaoPagouEAvisarNaLegendaQuandoSomentePagos()
    {
        await Sortear(
            ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
            CitacaoDe(new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(10).ComJogadoresNaoPagos("Paco", "Luan Ferraz").Build()),
            somentePagos: true);

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Imagem);
        mensagem.Texto.Should().Contain("⚠️ Sorteado sem: Paco, Luan Ferraz (sem ✅)");
    }

    // "!sortear pagos" com ninguém pago responde como o sorteio normal sem ninguém pago
    [Fact]
    public async Task DeveResponderNinguemNaListaQuandoSomentePagosENenhumJogadorPagou()
    {
        await Sortear(
            ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
            CitacaoDe(new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresNaoPagos(3).Build()),
            somentePagos: true);

        OUnicoTexto().Should().Be("Ninguém na lista ainda. Reenvie a lista completa com os nomes abaixo de Lista de jogadores.");
    }

    // "!sortear pagos" com menos de 4 pagos continua sem sortear
    [Fact]
    public async Task DeveResponderPoucosPagosQuandoSomentePagosEMenosDeQuatroPagaram()
    {
        await Sortear(
            ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente,
            CitacaoDe(new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(2).ComJogadoresNaoPagos("Paco").Build()),
            somentePagos: true);

        OUnicoTexto().Should().Be("Temos 2 pagos; faltam 2 para sortear (mínimo de 4).");
    }

    [Theory]
    [InlineData(1, "Temos 1 pago; faltam 3 para sortear (mínimo de 4).")]
    [InlineData(2, "Temos 2 pagos; faltam 2 para sortear (mínimo de 4).")]
    [InlineData(3, "Temos 3 pagos; falta 1 para sortear (mínimo de 4).")]
    public async Task DeveResponderSemImagemNemRodapeQuandoSoUmATresPagaram(int pagos, string esperado)
    {
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos)));

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);
        mensagem.Texto.Should().Be(esperado).And.NotContain("⏰");
    }

    [Fact]
    public async Task DevePedirAListaQuandoAMensagemCitadaNaoEhUmaLista()
    {
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, new MensagemCitada { Id = "3EB0AAAAAAAAAAAA", Texto = "bora que amanhã tem jogo" });

        OUnicoTexto().Should().Be("Não achei uma lista para sortear. Envie a lista completa com o título Lista de jogadores. Depois, segure essa mensagem, toque em Responder e envie !sortear.");
    }

    [Fact]
    public async Task DevePedirAListaQuandoAPresidenciaNaoPostouListaRecente()
    {
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, citada: null);

        OUnicoTexto().Should().Be("Não achei uma lista para sortear. Envie a lista completa com o título Lista de jogadores. Depois, segure essa mensagem, toque em Responder e envie !sortear.");
    }

    [Fact]
    public async Task DeveDizerQueAListaCitadaEhIlegivelQuandoSeuTextoNaoPodeSerLido()
    {
        _bot.Historico.Indisponivel = true;

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, new MensagemCitada { Id = "3EB0AAAAAAAAAAAA", Texto = null });

        OUnicoTexto().Should().Be("Não consegui ler essa lista. Reenvie a lista completa e responda à nova mensagem com o comando.");
    }

    [Fact]
    public async Task DeveRecusarUmaVezEDepoisIgnorarQuandoAlguemForaDaPresidenciaPedeDuasVezesEmDezMinutos()
    {
        await Sortear(ConstrutorDeCasoDeUso.IdDoIntegrante, CitacaoDe(ListaCom(pagos: 12)));
        _bot.Relogio.Advance(TimeSpan.FromMinutes(5));
        await Sortear(ConstrutorDeCasoDeUso.IdDoIntegrante, CitacaoDe(ListaCom(pagos: 12)));

        OUnicoTexto().Should().Be(RespostaSoOPresidente);
    }

    [Fact]
    public async Task DeveSortearQuandoOVicePede()
    {
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoVice, CitacaoDe(ListaCom(pagos: 10)));

        _bot.Mensageiro.Mensagens.Should().ContainSingle().Which.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Imagem);
    }

    [Fact]
    public async Task DeveResponderATodoSorteioQuandoOPresidentePedeCincoVezesSeguidas()
    {
        for (int pedido = 0; pedido < 5; pedido++)
        {
            await Sortear(ConstrutorDeCasoDeUso.LidDoPresidente, CitacaoDe(ListaCom(pagos: 10)));
        }

        _bot.Mensageiro.Mensagens.Should().HaveCount(5).And.OnlyContain(mensagem => mensagem.Tipo == TipoDeChamadaDoMensageiro.Imagem);
    }

    [Fact]
    public async Task NaoDeveEnviarNadaELogarUmErroQuandoTrintaMensagensSairamNaUltimaHoraInclusiveParaOPresidente()
    {
        for (int mensagem = 0; mensagem < 30; mensagem++)
        {
            _bot.Disjuntor.TentarReservar().Should().BeTrue();
        }

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 12)));

        _bot.Mensageiro.Chamadas.Should().BeEmpty();
        _bot.Logs.Entradas.Should().Contain(log => log.Nivel == LogLevel.Error && log.Mensagem.StartsWith("Disjuntor cheio"));
    }

    // O WAHA recusou o primeiro card: vai só o texto, e o segundo card nem sai
    [Fact]
    public async Task DeveEnviarALegendaSozinhaQuandoOWahaRejeitaAImagem()
    {
        _bot.Mensageiro.ResponderImagens(new Rejeitado { Motivo = "WAHA respondeu 422" });

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 12)));

        IReadOnlyList<ChamadaDoMensageiro> mensagens = _bot.Mensageiro.Mensagens;
        mensagens.Select(mensagem => mensagem.Tipo).Should().Equal(TipoDeChamadaDoMensageiro.Imagem, TipoDeChamadaDoMensageiro.Texto);
        mensagens[1].Texto.Should().Be(mensagens[0].Texto);
        mensagens[1].IdDaMensagemRespondida.Should().Be(IdDoComando);
        _bot.Logs.Entradas.Should().Contain(log => log.Nivel == LogLevel.Error && log.Mensagem.Contains("rejeitou o card"));
    }

    // Resultado incerto: nem o texto nem o segundo card vão, para não sair em dobro
    [Fact]
    public async Task NaoDeveEnviarALegendaELogarUmErroQuandoOEnvioDaImagemEhIncerto()
    {
        _bot.Mensageiro.ResponderImagens(new Incerto { Motivo = "WAHA não respondeu a tempo" });

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 12)));

        _bot.Mensageiro.Mensagens.Should().ContainSingle().Which.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Imagem);
        _bot.Logs.Entradas.Should().Contain(log => log.Nivel == LogLevel.Error && log.Mensagem.Contains("já pode estar no grupo"));
    }

    // A imagem falhou antes do envio: vai só o texto
    [Fact]
    public async Task DeveEnviarALegendaSozinhaELogarUmErroQuandoOCardNaoPodeSerDesenhado()
    {
        // Um escudo que não é imagem faz o desenho falhar.
        string escudo = Path.GetTempFileName();
        await File.WriteAllTextAsync(escudo, "isto não é uma imagem", TestContext.Current.CancellationToken);
        _bot.ComOpcoes(new ConstrutorDeOpcoesDoBot()
            .ComFrasesDoRodape(ConstrutorDeCasoDeUso.FraseDoRodape)
            .ComIdentidadeDoCard(string.Empty, escudo)
            .Build());

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 10)));
        File.Delete(escudo);

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);
        mensagem.Texto.Should().StartWith(CabecalhoDaLegenda).And.EndWith(FimDaLegenda);
        _bot.Logs.Entradas.Should().Contain(log => log.Nivel == LogLevel.Error && log.Mensagem.Contains("não pôde ser desenhado"));
    }

    // 6 times vão só em texto, sem log de erro
    [Fact]
    public async Task DeveEnviarSoOTextoSemLogarErroQuandoOSorteioTemSeisTimes()
    {
        // 31 pagos e 2 goleiros fixos formam 6 times, e o card comporta no máximo 5.
        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, CitacaoDe(ListaCom(pagos: 31)));

        _bot.Mensageiro.Mensagens.Should().ContainSingle().Which.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);
        _bot.Logs.Entradas.Should().NotContain(log => log.Nivel >= LogLevel.Warning)
            .And.Contain(log => log.Mensagem.Contains("6 times não cabem no card", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeveManterNomesTelefonesETextoDaListaForaDoLogQuandoOsTimesSaoSorteados()
    {
        string lista = new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros("Nuno Sigiloso").ComJogadoresPagos(11).ComJogadoresPagos("Ivo Sigiloso").Build();
        _bot.Historico.Com(new ConstrutorDeMensagemDoGrupo()
            .De(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, "Presidente Fulano")
            .EnviadaEm(ConstrutorDeCasoDeUso.Agora.AddHours(-1))
            .ComTexto(lista)
            .Build());

        await Sortear(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, citada: null);
        await Sortear(ConstrutorDeCasoDeUso.IdDoIntegrante, citada: null);

        _bot.Logs.Entradas.Should().NotBeEmpty().And.AllSatisfy(log => log.Mensagem.Should().NotContainAny(
            "Sigiloso", "Presidente Fulano", "Jogador", "Lista de jogadores", "35600000101", "35600000404", "120363000000001234"));
    }

    // FluentValidation (pedido malformado nunca chega a tocar o grupo)
    [Fact]
    public async Task DeveLancarENaoEnviarNadaQuandoOPedidoEhInvalido()
    {
        CasoDeUsoSortearTimes casoDeUso = _bot.ConstruirSortearTimes();
        PedidoSortearTimes pedidoInvalido = new()
        {
            IdDoGrupo = string.Empty,
            IdDoRemetente = string.Empty,
            IdDaMensagem = string.Empty,
            Citada = null,
            SomentePagos = false,
        };

        Func<Task> executar = () => casoDeUso.Executar(pedidoInvalido, TestContext.Current.CancellationToken);

        await executar.Should().ThrowAsync<ExcecaoDeValidacao>();
        _bot.Mensageiro.Chamadas.Should().BeEmpty();
    }

    private Task Sortear(string idDoRemetente, MensagemCitada? citada) => Sortear(idDoRemetente, citada, somentePagos: false);

    private async Task Sortear(string idDoRemetente, MensagemCitada? citada, bool somentePagos)
    {
        CasoDeUsoSortearTimes casoDeUso = _bot.ConstruirSortearTimes();
        PedidoSortearTimes pedido = new()
        {
            IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo,
            IdDoRemetente = idDoRemetente,
            IdDaMensagem = IdDoComando,
            Citada = citada,
            SomentePagos = somentePagos,
        };

        await _bot.ExecutarAsync(() => casoDeUso.Executar(pedido, TestContext.Current.CancellationToken));
    }

    private string? OUnicoTexto()
    {
        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);

        return mensagem.Texto;
    }

    private static string ListaCom(int pagos) => new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(pagos).Build();

    private static MensagemCitada CitacaoDe(string textoDaLista) => new() { Id = "3EB05B1F2A9C4D7E0B02", Texto = textoDaLista };
}
