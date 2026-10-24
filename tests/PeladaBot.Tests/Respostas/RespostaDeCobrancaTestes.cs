using PeladaBot.Listas;
using PeladaBot.Respostas;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Respostas;

public sealed class RespostaDeCobrancaTestes
{
    private const int QuantidadeDeSementes = 100;

    private static readonly IReadOnlyList<string> _mensagens =
    [
        "💶 *Quem ainda não pagou, favor acertar.*\n\n😂 Pra jogar todo mundo aparece. Na hora de pagar, a lista encolhe.",
        "🚨 *Quem ainda não pagou, favor regularizar!*",
        "💶 *Bora acertar, craques!*",
    ];

    // A mensagem como está na configuração e, embaixo, quem está sem ✅
    [Fact]
    public void DeveCobrarComAMensagemEOsNomesQuandoAlguemNaoPagou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros("Nuno").ComJogadoresPagos("Nico Braga").ComJogadoresNaoPagos("Paco", "Luan Ferraz").Build();

        string resposta = RespostaDeCobranca.Compor(lista, origem: null, [_mensagens[0]], new Random(14));

        resposta.Should().Be("""
            💶 *Quem ainda não pagou, favor acertar.*

            😂 Pra jogar todo mundo aparece. Na hora de pagar, a lista encolhe.

            💸 *Falta pagar (2):*
            👀 Paco
            👀 Luan Ferraz
            """);
    }

    // As mensagens vêm ao acaso, todas elas ao longo de muitas cobranças
    [Fact]
    public void DeveSortearUmaDasMensagensQuandoCobraVariasVezes()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();

        string[] mensagensUsadas =
        [
            .. Enumerable.Range(0, QuantidadeDeSementes)
                .Select(semente => RespostaDeCobranca.Compor(lista, origem: null, _mensagens, new Random(semente)))
                .Select(resposta => resposta[..resposta.LastIndexOf("\n\n💸", StringComparison.Ordinal)]),
        ];

        mensagensUsadas.Distinct().Should().BeEquivalentTo(_mensagens);
    }

    // Com todo mundo pago, não há cobrança
    [Fact]
    public void DeveAgradecerSemCobrarQuandoTodoMundoPagou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros("Nuno").ComJogadoresPagos("Nico Braga", "Paco").ComEspera("Davi").Build();

        string resposta = RespostaDeCobranca.Compor(lista, origem: null, _mensagens, new Random(14));

        resposta.Should().Be("✅ Todo mundo pagou, valeu craques!");
    }

    [Fact]
    public void DeveDizerQueNinguemEstaNaListaQuandoAListaDeJogadoresEstaVazia()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros("Nuno").Build();

        string resposta = RespostaDeCobranca.Compor(lista, origem: null, _mensagens, new Random(14));

        resposta.Should().StartWith("Ninguém na lista ainda");
    }

    // Sem citar, a origem da lista vem no fim, e um nome com marcação do WhatsApp é neutralizado
    [Fact]
    public void DeveMostrarDeOndeVeioALIstaENeutralizarOsNomesQuandoOComandoNaoCitou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("*Ivo*").Build();
        OrigemDaLista origem = new() { NomeDoRemetente = "Tales", EnviadaEm = new DateTimeOffset(2026, 10, 12, 11, 29, 0, TimeSpan.Zero) };

        string resposta = RespostaDeCobranca.Compor(lista, origem, ["💶 *Bora acertar, craques!*"], new Random(14));

        resposta.Should().Be("""
            💶 *Bora acertar, craques!*

            💸 *Falta pagar (1):*
            👀 ∗Ivo∗
            📋 Lista enviada por Tales em 12/10 às 13:29
            """);
    }

    // A mensagem que já lista os pendentes não ganha o "Não pagou ainda" embaixo
    [Fact]
    public void DeveListarOsPendentesSoNaMensagemQuandoElaTemAMarcaDoNome()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresPagos("Nico Braga").ComJogadoresNaoPagos("Davi Leme 22", "Nicolas").Build();

        string resposta = RespostaDeCobranca.Compor(
            lista, origem: null, ["🚫 *SORTEIO BLOQUEADO!*\n\n💸 *Falta pagar ({quantidade}):*\n👀 {nome}"], new Random(14));

        resposta.Should().Be("""
            🚫 *SORTEIO BLOQUEADO!*

            💸 *Falta pagar (2):*
            👀 Davi Leme 22
            👀 Nicolas
            """);
    }

    // Sem citar, a origem da lista ainda vem no fim, mesmo quando a mensagem lista os nomes
    [Fact]
    public void DeveMostrarDeOndeVeioALIstaQuandoAMensagemListaOsNomesEOComandoNaoCitou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();
        OrigemDaLista origem = new() { NomeDoRemetente = "Tales", EnviadaEm = new DateTimeOffset(2026, 10, 12, 11, 29, 0, TimeSpan.Zero) };

        string resposta = RespostaDeCobranca.Compor(lista, origem, ["💰 {nome}"], new Random(14));

        resposta.Should().Be("💰 Paco\n📋 Lista enviada por Tales em 12/10 às 13:29");
    }

    // Toda cobrança termina com o valor e o link configurados
    [Fact]
    public void DeveTerminarComOValorEOLinkQuandoOPagamentoEstaConfigurado()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();
        DadosDoPagamento pagamento = new() { Valor = "valor-de-exemplo", Link = "pagamento.exemplo/grupo" };

        string semNomes = RespostaDeCobranca.Compor(lista, origem: null, ["💶 *Bora acertar!*"], new Random(14), pagamento);
        string comNomes = RespostaDeCobranca.Compor(lista, origem: null, ["💰 {nome}"], new Random(14), pagamento);

        semNomes.Should().Be("💶 *Bora acertar!*\n\n💸 *Falta pagar (1):*\n👀 Paco\n\n💶 *Valor: valor-de-exemplo*\n📲 pagamento.exemplo/grupo");
        comNomes.Should().Be("💰 Paco\n\n💶 *Valor: valor-de-exemplo*\n📲 pagamento.exemplo/grupo");
    }

    // A mensagem que já mostra o pagamento não ganha o bloco repetido
    [Fact]
    public void NaoDeveRepetirOPagamentoQuandoAMensagemJaMostraOLink()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();
        DadosDoPagamento pagamento = new() { Valor = "valor-de-exemplo", Link = "pagamento.exemplo/grupo" };

        string resposta = RespostaDeCobranca.Compor(lista, origem: null, ["👕 {nome}\n📲 {link}"], new Random(14), pagamento);

        resposta.Should().Be("👕 Paco\n📲 pagamento.exemplo/grupo");
    }

    // Só com o link configurado, só a linha do link
    [Fact]
    public void DeveMostrarSoOLinkQuandoNaoHaValorConfigurado()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();

        string resposta = RespostaDeCobranca.Compor(
            lista, origem: null, ["💰 {nome}"], new Random(14), new DadosDoPagamento { Valor = string.Empty, Link = "pagamento.exemplo/grupo" });

        resposta.Should().Be("💰 Paco\n\n📲 pagamento.exemplo/grupo");
    }

    [Fact]
    public void DeveRecusarComporQuandoNaoHaMensagemDeCobranca()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();

        Action compor = () => RespostaDeCobranca.Compor(lista, origem: null, [], new Random(14));

        compor.Should().Throw<ArgumentException>();
    }
}
