using PeladaBot.Respostas;

namespace PeladaBot.Tests.Respostas;

public sealed class RespostasDeErroTestes
{
    [Fact]
    public void DevePedirParaCitarAListaComSortearQuandoOSorteioNaoTemLista()
    {
        string resposta = RespostasDeErro.CitarAListaParaSortear;

        resposta.Should().Be("Não achei uma lista para sortear. Envie a lista completa com o título Lista de jogadores. Depois, segure essa mensagem, toque em Responder e envie !sortear.");
    }

    [Fact]
    public void DevePedirParaCitarAListaComCobrarQuandoACobrancaNaoTemLista()
    {
        string resposta = RespostasDeErro.CitarAListaParaCobrar;

        resposta.Should().Be("Não achei uma lista para cobrar. Envie a lista completa com o título Lista de jogadores. Depois, segure essa mensagem, toque em Responder e envie !pendentes.");
    }

    [Fact]
    public void DevePedirParaEnviarAListaDeNovoQuandoAListaCitadaNaoPodeSerLida()
    {
        string resposta = RespostasDeErro.ListaCitadaIlegivel;

        resposta.Should().Be("Não consegui ler essa lista. Reenvie a lista completa e responda à nova mensagem com o comando.");
    }

    [Fact]
    public void DeveDizerQueSoOPresidenteSorteiaQuandoOutraPessoaPede()
    {
        string resposta = RespostasDeErro.SoOPresidentePodeSortear;

        resposta.Should().Be("Esse comando é só para presidente ou admin cadastrados no bot. Para ver quem falta pagar, use !pendentes.");
    }
}
