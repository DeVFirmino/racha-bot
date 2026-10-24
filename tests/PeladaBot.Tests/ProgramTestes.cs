using System.Net;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests;

public sealed class ProgramTestes : IClassFixture<FabricaDaAplicacaoDoBot>
{
    private readonly FabricaDaAplicacaoDoBot _fabrica;

    public ProgramTestes(FabricaDaAplicacaoDoBot fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task DeveResponderHttpQuandoAAplicacaoInicia()
    {
        HttpClient cliente = _fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/", TestContext.Current.CancellationToken);

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
