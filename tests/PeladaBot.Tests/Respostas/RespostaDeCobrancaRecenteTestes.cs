using PeladaBot.Respostas;

namespace PeladaBot.Tests.Respostas;

public sealed class RespostaDeCobrancaRecenteTestes
{
    // Uma das quatro resenhas de espera, ao acaso
    [Fact]
    public void DeveSortearUmaDasResenhasDeEsperaQuandoAlguemTentaCobrarDeNovo()
    {
        string[] respostas =
            [.. Enumerable.Range(0, 100).Select(semente => RespostaDeCobrancaRecente.Compor(TimeSpan.FromMinutes(18), new Random(semente)))];

        respostas.Distinct().Should().HaveCount(4).And.OnlyContain(resposta => resposta.Contains("18 minutos", StringComparison.Ordinal));
        respostas.Select(resposta => resposta.Split('\n')[0]).Distinct().Should().BeEquivalentTo(
            "⏳ *Calma, fiscal da diretoria!* 😂",
            "🚔 *Opa, delegado da cobrança!* 😂",
            "📺 *O VAR já revisou a cobrança!* 😂",
            "💶 *Calma, ministro da economia!* 😂");
    }

    // Os minutos arredondam para cima, no singular quando é um só, nunca menos de 1
    [Theory]
    [InlineData(17.2, "18 minutos")]
    [InlineData(1, "1 minuto")]
    [InlineData(0.1, "1 minuto")]
    [InlineData(0, "1 minuto")]
    public void DeveDizerQuantosMinutosFaltamQuandoAJanelaEstaAberta(double minutosQueFaltam, string esperado)
    {
        string resposta = RespostaDeCobrancaRecente.Compor(TimeSpan.FromMinutes(minutosQueFaltam), new Random(14));

        resposta.Should().Contain(esperado).And.NotContain("{tempo}");
    }

    [Fact]
    public void DeveTerAMarcaDoTempoEmTodaResenhaQuandoOBotSobe()
    {
        RespostaDeCobrancaRecente.Resenhas.Should().OnlyContain(resenha => resenha.Contains("{tempo}", StringComparison.Ordinal));
    }
}
