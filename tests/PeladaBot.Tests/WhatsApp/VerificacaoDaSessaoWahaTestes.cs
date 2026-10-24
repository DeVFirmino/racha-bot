using System.Net;
using Microsoft.Extensions.Logging;
using PeladaBot.Tests.UtilitariosDeTeste;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.WhatsApp;

public sealed class VerificacaoDaSessaoWahaTestes
{
    private static readonly string _categoriaDaVerificacao =
        typeof(VerificacaoDaSessaoWaha).FullName ?? nameof(VerificacaoDaSessaoWaha);

    [Theory]
    [InlineData("STOPPED")]
    [InlineData("FAILED")]
    [InlineData("SCAN_QR_CODE")]
    public async Task DeveLogarUmErroQuandoOBotIniciaEASessaoNaoEstaFuncionando(string status)
    {
        using FabricaDaAplicacaoDoBot fabrica = new();
        fabrica.Waha.Responder(HttpMethod.Get, "/api/sessions/default", HttpStatusCode.OK, $$"""{"name":"default","status":"{{status}}"}""");

        fabrica.StartServer();

        LogColetado log = await LogDaVerificacao(fabrica);
        log.Nivel.Should().Be(LogLevel.Error);
        log.Mensagem.Should().Be($"Sessão do WAHA default está {status}: sem leituras ou respostas");
    }

    // Sessão ainda subindo, como numa partida a frio: não é erro
    [Fact]
    public async Task DeveSoAvisarQuandoOBotIniciaEASessaoAindaEstaIniciando()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();
        fabrica.Waha.Responder(HttpMethod.Get, "/api/sessions/default", HttpStatusCode.OK, """{"name":"default","status":"STARTING"}""");

        fabrica.StartServer();

        LogColetado log = await LogDaVerificacao(fabrica);
        log.Nivel.Should().Be(LogLevel.Warning);
        log.Mensagem.Should().Be("Sessão do WAHA default ainda está STARTING: ainda não é erro");
    }

    // WAHA fora do ar no startup
    [Fact]
    public async Task DeveLogarUmErroQuandoOBotIniciaEOWahaNaoPodeSerAlcancado()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();
        fabrica.Waha.Programar(HttpMethod.Get, "/api/sessions/default", () => throw new HttpRequestException(HttpRequestError.ConnectionError));

        fabrica.StartServer();

        LogColetado log = await LogDaVerificacao(fabrica);
        log.Nivel.Should().Be(LogLevel.Error);
        log.Mensagem.Should().Be("Sessão do WAHA default: seu status não pôde ser lido na inicialização");
    }

    [Fact]
    public async Task DeveSoInformarQuandoOBotIniciaEASessaoEstaFuncionando()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        fabrica.StartServer();

        LogColetado log = await LogDaVerificacao(fabrica);
        log.Nivel.Should().Be(LogLevel.Information);
        log.Mensagem.Should().Be("Sessão do WAHA default está WORKING");
    }

    /// <summary>A verificação roda em segundo plano, logo depois que o servidor sobe: espera o log dela.</summary>
    private static Task<LogColetado> LogDaVerificacao(FabricaDaAplicacaoDoBot fabrica) =>
        fabrica.EsperarLogAsync(_categoriaDaVerificacao);
}
