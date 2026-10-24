using PeladaBot.Listas;
using PeladaBot.Respostas;
using PeladaBot.Sorteio;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Respostas;

public sealed class RespostaSemSorteioTestes
{
    private const int Semente = 42;

    /// <summary>12/10 às 13:29 no fuso da suíte.</summary>
    private static readonly DateTimeOffset _listaEnviadaEm = new(2026, 10, 12, 11, 29, 0, TimeSpan.Zero);

    // A lista não está vazia, mas ninguém pagou ainda: falta pagar, não "ninguém na lista"
    [Fact]
    public void DeveApontarQuemFaltaPagarQuandoNinguemPagou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros("Nuno").ComJogadoresNaoPagos("Paco", "Luan Ferraz").ComEspera("Davi").Build();

        string resposta = Compor(new PagamentoPendente { NomesPendentes = ["Paco", "Luan Ferraz"] }, lista, origem: null);

        resposta.Should().Be("🚨 *Quem ainda não pagou, favor acertar!*\n\n💸 *Falta pagar (2):*\n👀 Paco\n👀 Luan Ferraz\n\nPara sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).");
    }

    [Fact]
    public void DeveDizerQueNinguemEstaNaListaQuandoAListaDeJogadoresEstaVazia()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros("Nuno", "Cauã Luz").ComEspera("Davi").Build();

        string resposta = Compor(new NinguemPagou(), lista, origem: null);

        resposta.Should().Be("Ninguém na lista ainda. Reenvie a lista completa com os nomes abaixo de Lista de jogadores.");
    }

    [Theory]
    [InlineData(2, "Temos 2 pagos; faltam 2 para sortear (mínimo de 4).")]
    [InlineData(3, "Temos 3 pagos; falta 1 para sortear (mínimo de 4).")]
    public void DeveDizerQuantosPagaramQuandoDoisOuTresPagaram(int quantidadePaga, string esperado)
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros("Nuno").ComJogadoresPagos(quantidadePaga).Build();

        string resposta = Compor(new PagosInsuficientes { QuantidadePaga = quantidadePaga }, lista, origem: null);

        resposta.Should().Be(esperado);
    }

    [Fact]
    public void DeveUsarOSingularQuandoSoUmPagou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresPagos("Nico Braga").Build();

        string resposta = Compor(new PagosInsuficientes { QuantidadePaga = 1 }, lista, origem: null);

        resposta.Should().Be("Temos 1 pago; faltam 3 para sortear (mínimo de 4).");
    }

    // , pending reply: várias pessoas faltando, na ordem da lista
    [Fact]
    public void DeveApontarQuemFaltaPagarNaOrdemDaListaQuandoVariosNaoPagaram()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Luan Ferraz", "Paco", "Ivo").Build();

        string resposta = Compor(new PagamentoPendente { NomesPendentes = ["Luan Ferraz", "Paco", "Ivo"] }, lista, origem: null);

        resposta.Should().Be("🚨 *Quem ainda não pagou, favor acertar!*\n\n💸 *Falta pagar (3):*\n👀 Luan Ferraz\n👀 Paco\n👀 Ivo\n\nPara sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).");
    }

    // Resposta sem sorteio
    [Fact]
    public void DeveAdicionarALinhaDeOrigemQuandoFaltaAlguemPagarEAListaNaoFoiCitada()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco", "Luan Ferraz").Build();

        string resposta = Compor(new PagamentoPendente { NomesPendentes = ["Paco", "Luan Ferraz"] }, lista, EnviadaPor("Tales"));

        resposta.Should().Be("""
            🚨 *Quem ainda não pagou, favor acertar!*

            💸 *Falta pagar (2):*
            👀 Paco
            👀 Luan Ferraz

            Para sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).
            📋 Lista enviada por Tales em 12/10 às 13:29
            """);
    }

    // Resposta sem sorteio
    [Fact]
    public void DeveAdicionarALinhaDeOrigemQuandoAListaEstaVaziaENaoFoiCitada()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().Build();

        string resposta = Compor(new NinguemPagou(), lista, EnviadaPor("Tales"));

        resposta.Should().Be("""
            Ninguém na lista ainda. Reenvie a lista completa com os nomes abaixo de Lista de jogadores.
            📋 Lista enviada por Tales em 12/10 às 13:29
            """);
    }

    // Resposta sem sorteio
    [Fact]
    public void DeveAdicionarALinhaDeOrigemSemNomeQuandoPoucosPagaramEORemetenteEhDesconhecido()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresPagos("Nico Braga", "Duda Neri").Build();

        string resposta = Compor(new PagosInsuficientes { QuantidadePaga = 2 }, lista, EnviadaPor(nomeDoRemetente: null));

        resposta.Should().Be("""
            Temos 2 pagos; faltam 2 para sortear (mínimo de 4).
            📋 Lista de 12/10 às 13:29
            """);
    }

    // A resposta de pendência também carrega 📋 e os avisos "⚠️"
    [Fact]
    public void DeveAdicionarOsAvisosDaListaQuandoElaOsTem()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("Nuno")
            .ComJogadoresNaoPagos("Hugo")
            .ComAvisos(
                new NomeEmDuasSecoes { Nome = "Nuno", MantidoEm = SecaoDaLista.Goleiros },
                new NomeRepetidoComMarcaDiferente { Nome = "Hugo" })
            .Build();

        string resposta = Compor(new PagamentoPendente { NomesPendentes = ["Hugo"] }, lista, EnviadaPor("Tales"));

        resposta.Should().Be("""
            🚨 *Quem ainda não pagou, favor acertar!*

            💸 *Falta pagar (1):*
            👀 Hugo

            Para sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).
            📋 Lista enviada por Tales em 12/10 às 13:29
            ⚠️ Nuno estava em duas seções, ficou como goleiro
            ⚠️ Hugo aparece duas vezes na lista (valeu a primeira)
            """);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void NaoDeveTerRodapeQuandoOMotorDoSorteioNaoSorteou(int quantidadePaga)
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(quantidadePaga).ComJogadoresNaoPagos(2).Build();
        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente));

        string resposta = Compor(resultado, lista, EnviadaPor("Tales"));

        resposta.Should().NotContain("⏰");
    }

    [Fact]
    public void DeveNeutralizarAMarcacaoDoWhatsAppQuandoUmNomeATem()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("*Paco*", "_Luan Ferraz_").Build();

        string resposta = Compor(
            new PagamentoPendente { NomesPendentes = ["*Paco*", "_Luan Ferraz_"] }, lista, EnviadaPor("~Tales~"));

        // ∗ (U+2217), ˍ (U+02CD) e ∼ (U+223C) substituem *, _ e ~ em nomes: o WhatsApp não lê nenhum deles como marcação.
        resposta.Should().Be("""
            🚨 *Quem ainda não pagou, favor acertar!*

            💸 *Falta pagar (2):*
            👀 ∗Paco∗
            👀 ˍLuan Ferrazˍ

            Para sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).
            📋 Lista enviada por ∼Tales∼ em 12/10 às 13:29
            """);
    }

    [Fact]
    public void DeveRecusarOResultadoQuandoOsTimesForamSorteados()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(4).Build();
        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente));

        Action compor = () => Compor(resultado, lista, origem: null);

        compor.Should().Throw<ArgumentException>();
    }

    private static OrigemDaLista EnviadaPor(string? nomeDoRemetente) => new() { NomeDoRemetente = nomeDoRemetente, EnviadaEm = _listaEnviadaEm };

    // O "Falta pagar" vem com uma das mensagens de cobrança, ao acaso, como o !cobrar
    [Fact]
    public void DeveSortearAMensagemDeCobrancaQuandoFaltaAlguemPagar()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();
        string[] mensagens = ["💶 *Bora acertar!*", "🚨 *Atenção, caloteiros!*", "🍺 *Recado da diretoria*"];

        string[] usadas =
        [
            .. Enumerable.Range(0, 100).Select(semente => RespostaSemSorteio.Compor(
                new PagamentoPendente { NomesPendentes = ["Paco"] }, lista, origem: null, mensagens, new Random(semente)).Split('\n')[0]),
        ];

        usadas.Distinct().Should().BeEquivalentTo(mensagens);
    }

    // A cobrança que já lista os pendentes substitui o "⏳ Falta pagar", e a saída pelo !sortear pagos fica
    [Fact]
    public void DeveUsarSoACobrancaComOsNomesQuandoAMensagemListaOsPendentes()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco", "Luan Ferraz").Build();

        string resposta = RespostaSemSorteio.Compor(
            new PagamentoPendente { NomesPendentes = ["Paco", "Luan Ferraz"] },
            lista,
            origem: null,
            ["🤖 *ERRO 404*\n🔎 {nome}"],
            new Random(14));

        resposta.Should().Be("""
            🤖 *ERRO 404*
            🔎 Paco
            🔎 Luan Ferraz

            Para sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).
            """);
    }

    // O "Falta pagar" também traz o valor e o link, antes da saída pelo !sortear pagos
    [Fact]
    public void DeveMostrarOPagamentoAntesDaSaidaPeloSortearPagosQuandoOPagamentoEstaConfigurado()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();

        string resposta = RespostaSemSorteio.Compor(
            new PagamentoPendente { NomesPendentes = ["Paco"] },
            lista,
            origem: null,
            ["🚨 *Bora acertar!*"],
            new Random(14),
            new DadosDoPagamento { Valor = "valor-de-exemplo", Link = "pagamento.exemplo/grupo" });

        resposta.Should().Be("""
            🚨 *Bora acertar!*

            💸 *Falta pagar (1):*
            👀 Paco

            💶 *Valor: valor-de-exemplo*
            📲 pagamento.exemplo/grupo

            Para sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).
            """);
    }

    // Sem pendência não há cobrança
    [Fact]
    public void NaoDeveCobrarQuandoOMotivoNaoEhFaltaDePagamento()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresPagos("Paco", "Luan Ferraz").Build();

        string resposta = Compor(new PagosInsuficientes { QuantidadePaga = 2 }, lista, origem: null);

        resposta.Should().NotContain(Cobranca);
    }

    /// <summary>A única mensagem de cobrança dos testes, que vai em cima do "Falta pagar".</summary>
    private const string Cobranca = "🚨 *Quem ainda não pagou, favor acertar!*";

    private static string Compor(ResultadoDoSorteio resultado, ListaDoGrupo lista, OrigemDaLista? origem) =>
        RespostaSemSorteio.Compor(resultado, lista, origem, [Cobranca], new Random(14));
}
