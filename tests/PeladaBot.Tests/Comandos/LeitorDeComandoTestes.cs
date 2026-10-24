using PeladaBot.Comandos;

namespace PeladaBot.Tests.Comandos;

public sealed class LeitorDeComandoTestes
{
    [Theory]
    [InlineData("!sortear", TipoDeComando.Sortear)]
    [InlineData("!times", TipoDeComando.Sortear)]
    [InlineData("!pendentes", TipoDeComando.Cobrar)]
    [InlineData("!ajuda", TipoDeComando.Ajuda)]
    [InlineData("!cobrar", TipoDeComando.Cobrar)]
    [InlineData("!PENDENTES", TipoDeComando.Cobrar)]
    public void DeveReconhecerOComandoQuandoAMensagemEhExatamenteOComando(string texto, TipoDeComando esperado)
    {
        ComandoLido? comando = LeitorDeComando.Ler(texto);

        comando.Should().BeEquivalentTo(new ComandoLido { Tipo = esperado, SomentePagos = false });
    }

    // Sem diferenciar maiúsculas nem acentos
    [Theory]
    [InlineData("!SORTEAR")]
    [InlineData("!sortéar")]
    [InlineData("!SORTÉAR")]
    [InlineData("!Times")]
    [InlineData("!sorte\u0301ar")] // "á" decomposto: "a" seguido do acento agudo combinado
    public void DeveReconhecerOSorteioQuandoOComandoTemOutraCaixaOuAcentos(string texto)
    {
        ComandoLido? comando = LeitorDeComando.Ler(texto);

        comando.Should().NotBeNull();
        comando!.Tipo.Should().Be(TipoDeComando.Sortear);
    }

    // O comando é a primeira palavra; o resto da mensagem não muda nada
    [Theory]
    [InlineData("!sortear agora")]
    [InlineData("   !sortear")]
    [InlineData("!sortear\nvaleu, presidente")]
    [InlineData("\t!sortear\t")]
    public void DeveReconhecerOSorteioQuandoOComandoEhAPrimeiraPalavraDoTexto(string texto)
    {
        ComandoLido? comando = LeitorDeComando.Ler(texto);

        comando.Should().NotBeNull();
        comando!.Tipo.Should().Be(TipoDeComando.Sortear);
    }

    // Palavra inteira, primeira palavra, e só comandos conhecidos
    [Theory]
    [InlineData("!sortearabc")]
    [InlineData("!sortear!")]
    [InlineData("sortear")]
    [InlineData("bora !sortear")]
    [InlineData("Lista de jogadores:\n1. Nico Braga ✅\n!sortear")]
    [InlineData("!sair")]
    public void NaoDeveEncontrarComandoQuandoAPrimeiraPalavraNaoEhUmComando(string texto)
    {
        ComandoLido? comando = LeitorDeComando.Ler(texto);

        comando.Should().BeNull();
    }

    // Mensagem sem texto, como uma figurinha
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n ")]
    public void NaoDeveEncontrarComandoQuandoAMensagemNaoTemTexto(string? texto)
    {
        ComandoLido? comando = LeitorDeComando.Ler(texto);

        comando.Should().BeNull();
    }

    // "!sortear pagos"/"!times pagos" pede o sorteio de emergência, sem diferenciar caixa nem acentos
    [Theory]
    [InlineData("!sortear pagos")]
    [InlineData("!times pagos")]
    [InlineData("!SORTEAR PAGOS")]
    [InlineData("!sortear Pagos")]
    [InlineData("!sortear pagós")]
    [InlineData("!sortear  pagos")]
    [InlineData("!sortear pagos agora")]
    public void DevePedirSomentePagosQuandoASegundaPalavraEhPagos(string texto)
    {
        ComandoLido? comando = LeitorDeComando.Ler(texto);

        comando.Should().BeEquivalentTo(new ComandoLido { Tipo = TipoDeComando.Sortear, SomentePagos = true });
    }

    // "pagosx" não é "pagos"; a segunda palavra tem de ser exatamente ela
    [Theory]
    [InlineData("!sortear pagosx")]
    [InlineData("!sortear xpagos")]
    [InlineData("!sortear pay")]
    public void NaoDevePedirSomentePagosQuandoASegundaPalavraNaoEhExatamentePagos(string texto)
    {
        ComandoLido? comando = LeitorDeComando.Ler(texto);

        comando.Should().BeEquivalentTo(new ComandoLido { Tipo = TipoDeComando.Sortear, SomentePagos = false });
    }

    // "pagos" só vale depois de !sortear/!times; não é um comando por si, nem muda !pendentes/!ajuda
    [Theory]
    [InlineData("!pendentes pagos", TipoDeComando.Cobrar)]
    [InlineData("!ajuda pagos", TipoDeComando.Ajuda)]
    public void NaoDevePedirSomentePagosQuandoOComandoNaoEhSortear(string texto, TipoDeComando esperado)
    {
        ComandoLido? comando = LeitorDeComando.Ler(texto);

        comando.Should().BeEquivalentTo(new ComandoLido { Tipo = esperado, SomentePagos = false });
    }
}
