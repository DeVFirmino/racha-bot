using Microsoft.Extensions.Options;
using PeladaBot.Tests.UtilitariosDeTeste;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.WhatsApp;

public sealed class ValidadorDasOpcoesDoWahaTestes
{
    [Theory]
    [InlineData("http://waha:3000")]
    [InlineData("http://waha:3000/")]
    [InlineData("https://waha.railway.internal")]
    public void DeveAceitarAsOpcoesQuandoAUrlBaseEhUmEnderecoHttp(string urlBase)
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(urlBase: urlBase));

        resultado.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("waha:3000")]
    [InlineData("/api")]
    [InlineData("ftp://waha:3000")]
    public void DeveRecusarAsOpcoesQuandoAUrlBaseNaoEhUmEnderecoHttp(string urlBase)
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(urlBase: urlBase));

        resultado.Failures.Should().ContainSingle().Which.Should().StartWith("Waha:UrlBase");
    }

    // A chave da API vem de variável de ambiente; sem ela, o WAHA recusa tudo
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeveRecusarAsOpcoesQuandoAChaveDaApiEstaFaltando(string chaveDaApi)
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(chaveDaApi: chaveDaApi));

        resultado.Failures.Should().ContainSingle().Which.Should().StartWith("Waha:ChaveDaApi");
    }

    // A chave do webhook vem de variável de ambiente; sem ela, o bot não reconhece o que o WAHA assina
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeveRecusarAsOpcoesQuandoAChaveHmacDoWebhookEstaFaltando(string chaveHmacDoWebhook)
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(chaveHmacDoWebhook: chaveHmacDoWebhook));

        resultado.Failures.Should().ContainSingle().Which.Should().Be(ValidadorDasOpcoesDoWaha.ChaveHmacDoWebhookEmBranco);
    }

    // Chave curta é fácil de adivinhar; 31 caracteres ainda é pouco
    [Theory]
    [InlineData("changeme")]
    [InlineData("0123456789abcdef0123456789abcde")]
    public void DeveRecusarAsOpcoesQuandoAChaveHmacDoWebhookTemMenosDeTrintaEDoisCaracteres(string chaveHmacDoWebhook)
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(chaveHmacDoWebhook: chaveHmacDoWebhook));

        resultado.Failures.Should().ContainSingle().Which.Should().StartWith("Waha:ChaveHmacDoWebhook deve ter ao menos 32 caracteres");
    }

    // 32 caracteres já valem; openssl rand -hex 32 dá 64
    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef")]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    public void DeveAceitarAsOpcoesQuandoAChaveHmacDoWebhookTemAoMenosTrintaEDoisCaracteres(string chaveHmacDoWebhook)
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(chaveHmacDoWebhook: chaveHmacDoWebhook));

        resultado.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("de fault")]
    [InlineData("default/x")]
    public void DeveRecusarAsOpcoesQuandoASessaoNaoEhUmNomeSimples(string sessao)
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(sessao: sessao));

        resultado.Failures.Should().ContainSingle().Which.Should().StartWith("Waha:Sessao");
    }

    // A chave nunca vai para o log
    [Fact]
    public void DeveManterAChaveDaApiForaDeTodaFalhaQuandoAsOpcoesSaoInvalidas()
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(urlBase: "nada", chaveDaApi: "chave-secreta", sessao: ""));

        resultado.Failures.Should().HaveCount(2).And.AllSatisfy(falha => falha.Should().NotContain("chave-secreta"));
    }

    // A chave do webhook nunca vai para o log, nem o tamanho dela: esta tem 23 caracteres
    [Fact]
    public void DeveManterAChaveHmacDoWebhookESeuTamanhoForaDaFalhaQuandoAChaveEhCurtaDemais()
    {
        ValidateOptionsResult resultado = Validar(OpcoesValidas(chaveHmacDoWebhook: "chave-curta-de-23-chars"));

        resultado.Failures.Should().ContainSingle().Which.Should().NotContain("chave-curta").And.NotContain("23");
    }

    // O bot não sobe e o log diz o motivo
    [Theory]
    [InlineData("Waha:UrlBase", "")]
    [InlineData("Waha:ChaveDaApi", "")]
    [InlineData("Waha:ChaveHmacDoWebhook", "")]
    public void DeveRecusarIniciarELogarOMotivoQuandoUmaConfiguracaoDoWahaEhInvalida(string chave, string valor)
    {
        using FabricaDaAplicacaoDoBot fabrica = FabricaDaAplicacaoDoBot.ComConfiguracoes(new Dictionary<string, string?> { [chave] = valor });

        Action iniciar = fabrica.StartServer;

        iniciar.Should().Throw<Exception>();
        fabrica.Logs
            .Select(log => log.Excecao)
            .OfType<OptionsValidationException>()
            .Should().ContainSingle()
            .Which.Failures.Should().ContainSingle()
            .Which.Should().StartWith(chave);
    }

    private static ValidateOptionsResult Validar(OpcoesDoWaha opcoes) => new ValidadorDasOpcoesDoWaha().Validate(name: null, opcoes);

    /// <summary>Opções válidas do WAHA, com uma delas alterada pelo teste.</summary>
    private static OpcoesDoWaha OpcoesValidas(
        string urlBase = "http://waha:3000",
        string chaveDaApi = "chave-de-teste",
        string chaveHmacDoWebhook = "chave-do-webhook-de-teste-com-mais-de-32-caracteres",
        string sessao = "default") =>
        new() { UrlBase = urlBase, ChaveDaApi = chaveDaApi, ChaveHmacDoWebhook = chaveHmacDoWebhook, Sessao = sessao };
}
