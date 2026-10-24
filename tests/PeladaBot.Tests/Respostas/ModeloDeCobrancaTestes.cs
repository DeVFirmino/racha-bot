using Microsoft.Extensions.DependencyInjection;
using PeladaBot.Configuracao;
using PeladaBot.Respostas;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Respostas;

public sealed class ModeloDeCobrancaTestes
{
    // A linha com {nome} se repete por pendente, na ordem da lista
    [Fact]
    public void DeveRepetirALinhaDoNomeParaCadaPendenteQuandoOModeloTemNome()
    {
        string mensagem = ModeloDeCobranca.Preencher("🚨 *Pendências:*\n👀 {nome}\n😂 Bora!", ["Paco", "Luan Ferraz", "Ivo"]);

        mensagem.Should().Be("🚨 *Pendências:*\n👀 Paco\n👀 Luan Ferraz\n👀 Ivo\n😂 Bora!");
    }

    // {quantidade} e {s} acompanham quantos são
    [Theory]
    [InlineData(new[] { "Paco" }, "⏳ *Aguardando 1 transferência...*")]
    [InlineData(new[] { "Paco", "Luan Ferraz" }, "⏳ *Aguardando 2 transferências...*")]
    [InlineData(new[] { "Paco", "Luan Ferraz", "Ivo", "Milo Castro", "Cael Prado" }, "⏳ *Aguardando 5 transferências...*")]
    public void DeveEscreverAQuantidadeEOPluralQuandoOModeloTemAsMarcas(string[] nomes, string esperado)
    {
        string mensagem = ModeloDeCobranca.Preencher("⏳ *Aguardando {quantidade} transferência{s}...*", nomes);

        mensagem.Should().Be(esperado);
    }

    // Uma mensagem sem marcas sai como está, só sem os espaços das pontas
    [Fact]
    public void DeveManterAMensagemQuandoElaNaoTemMarcas()
    {
        string mensagem = ModeloDeCobranca.Preencher("  💶 *Bora acertar, craques!*\n\n😂 Sem desculpa!  ", ["Paco"]);

        mensagem.Should().Be("💶 *Bora acertar, craques!*\n\n😂 Sem desculpa!");
    }

    // Um nome com marcação do WhatsApp sai neutralizado, e um nome que parece marca não é lido como marca
    [Fact]
    public void DeveNeutralizarONomeENaoLerMarcasDentroDeleQuandoONomeTemSimbolos()
    {
        string mensagem = ModeloDeCobranca.Preencher("💰 {nome} ({quantidade})", ["*Ivo*", "{s}{quantidade}"]);

        mensagem.Should().Be("💰 ∗Ivo∗ (2)\n💰 {s}{quantidade} (2)");
    }

    // Cada linha com {nome} se repete por conta própria
    [Fact]
    public void DeveRepetirCadaLinhaDeNomeQuandoOModeloTemMaisDeUma()
    {
        string mensagem = ModeloDeCobranca.Preencher("✅ {nome} sent you money\n—\n💸 {nome}", ["Paco", "Luan Ferraz"]);

        mensagem.Should().Be("✅ Paco sent you money\n✅ Luan Ferraz sent you money\n—\n💸 Paco\n💸 Luan Ferraz");
    }

    [Theory]
    [InlineData("💸 {nome}", true)]
    [InlineData("💸 *Falta pagar ({quantidade}):*", false)]
    [InlineData("💶 *Bora acertar!*", false)]
    public void DeveDizerSeAMensagemListaOsNomesQuandoPerguntado(string modelo, bool listaOsNomes)
    {
        ModeloDeCobranca.ListaOsNomes(modelo).Should().Be(listaOsNomes);
    }

    [Theory]
    [MemberData(nameof(CobrancasDeExemplo))]
    public void DeveReproduzirACobrancaDeExemploQuandoOsPendentesSaoDois(int indiceNaConfiguracao, string esperado)
    {
        using FabricaDaAplicacaoDoBot fabrica = new();
        string modelo = OpcoesDoGrupo(fabrica).MensagensDeCobranca[indiceNaConfiguracao];

        string mensagem = ModeloDeCobranca.Preencher(
            modelo, ["Paco", "Luan Ferraz"], new DadosDoPagamento { Valor = "valor-de-exemplo", Link = string.Empty });

        mensagem.Should().Be(esperado);
    }

    // Toda cobrança da configuração, com 1 ou 3 pendentes, sai sem marca sobrando e sem nome fixo
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void DevePreencherTodaCobrancaDaConfiguracaoQuandoHaPendentes(int quantidade)
    {
        string[] nomes = [.. new[] { "Nico Braga", "Duda Neri", "Luca P 22" }.Take(quantidade)];
        using FabricaDaAplicacaoDoBot fabrica = new();

        OpcoesDoGrupo(fabrica).MensagensDeCobranca.Should().HaveCount(6).And.AllSatisfy(modelo =>
        {
            string mensagem = ModeloDeCobranca.Preencher(modelo, nomes, new DadosDoPagamento { Valor = "valor-de-exemplo", Link = "pagamento.exemplo/grupo" });

            mensagem.Should().NotContain("{").And.NotContain("}");
            if (ModeloDeCobranca.ListaOsNomes(modelo))
            {
                mensagem.Should().ContainAll(nomes);
            }
        });
    }

    // {valor} e {link} vêm do pagamento configurado
    [Fact]
    public void DevePreencherOValorEOLinkQuandoOPagamentoEstaConfigurado()
    {
        string mensagem = ModeloDeCobranca.Preencher(
            "👕 {nome}\n💶 *Valor: {valor}*\n📲 {link}", ["Paco"], new DadosDoPagamento { Valor = "valor-de-exemplo", Link = "pagamento.exemplo/grupo" });

        mensagem.Should().Be("👕 Paco\n💶 *Valor: valor-de-exemplo*\n📲 pagamento.exemplo/grupo");
    }

    // Sem link (ou sem valor) configurado, a linha que pede some
    [Theory]
    [InlineData("valor-de-exemplo", "", "👕 Paco\n💶 *Valor: valor-de-exemplo*\n⚽ Bora!")]
    [InlineData("", "pagamento.exemplo/grupo", "👕 Paco\n📲 pagamento.exemplo/grupo\n⚽ Bora!")]
    [InlineData("", "", "👕 Paco\n⚽ Bora!")]
    public void DeveTirarALinhaDoPagamentoQuandoAConfiguracaoNaoTem(string valor, string link, string esperado)
    {
        string mensagem = ModeloDeCobranca.Preencher(
            "👕 {nome}\n💶 *Valor: {valor}*\n📲 {link}\n⚽ Bora!", ["Paco"], new DadosDoPagamento { Valor = valor, Link = link });

        mensagem.Should().Be(esperado);
    }

    public static TheoryData<int, string> CobrancasDeExemplo() => new()
    {
        { 2, "💸 *Falta pagar (2):*\n👀 Paco\n👀 Luan Ferraz\n\n😂 *Pagou, tá dentro. Não pagou, espera.*\n💶 *Valor: valor-de-exemplo*" },
        { 4, "📣 *Jogue até o apito.*\n\n💸 *Falta pagar (2):*\n👀 Paco\n👀 Luan Ferraz" },
        { 5, "🚌 *Nada de retranca no pagamento.*\n\n🏆 *Ninguém é maior que a pelada.*\n💶 *valor-de-exemplo*" },
    };

    private static OpcoesDoGrupo OpcoesDoGrupo(FabricaDaAplicacaoDoBot fabrica) =>
        fabrica.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpcoesDoBot>>().Value.Grupos.Single();
}
