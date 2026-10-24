using PeladaBot.Configuracao;

namespace PeladaBot.Tests.Configuracao;

public sealed class MascaraDeIdTestes
{
    [Theory]
    [InlineData("35699123456@c.us", "***3456@c.us")]
    [InlineData("123456789012345@lid", "***2345@lid")]
    [InlineData("120363012345678901@g.us", "***8901@g.us")]
    public void DeveManterSoOsUltimosQuatroCaracteresEOSufixoQuandoOIdTemSufixo(string id, string esperado)
    {
        string mascarado = MascaraDeId.Aplicar(id);

        mascarado.Should().Be(esperado);
    }

    [Fact]
    public void DeveManterSoOsUltimosQuatroCaracteresQuandoOIdNaoTemSufixo()
    {
        string mascarado = MascaraDeId.Aplicar("35699123456");

        mascarado.Should().Be("***3456");
    }

    [Theory]
    [InlineData("1234", "***")]
    [InlineData("12@c.us", "***@c.us")]
    [InlineData("", "***")]
    public void DeveEsconderOIdTodoQuandoElePossuiQuatroCaracteresOuMenos(string id, string esperado)
    {
        string mascarado = MascaraDeId.Aplicar(id);

        mascarado.Should().Be(esperado);
    }

    // O ID completo da mensagem no WAHA carrega o grupo e o telefone de quem mandou
    [Theory]
    [InlineData("false_120363000000001234@g.us_3EB0C767D26A1D8E_35600000101@c.us")]
    [InlineData("false_120363000000001234@g.us_3EB0C767D26A1D8E_100000000000202@lid")]
    [InlineData("true_120363000000001234@g.us_3EB0C767D26A1D8E")]
    [InlineData("3EB0C767D26A1D8E")]
    public void DeveManterSoOFinalDaChaveDoWhatsAppQuandoOIdEhDeUmaMensagem(string idDaMensagem)
    {
        string mascarado = MascaraDeId.AplicarNaMensagem(idDaMensagem);

        mascarado.Should().Be("***1D8E");
    }

    [Theory]
    [InlineData("algo@estranho_aqui")]
    [InlineData("1D8E")]
    [InlineData("")]
    public void DeveEsconderOIdDaMensagemTodoQuandoTemUmFormatoInesperado(string idDaMensagem)
    {
        string mascarado = MascaraDeId.AplicarNaMensagem(idDaMensagem);

        mascarado.Should().Be("***");
    }
}
