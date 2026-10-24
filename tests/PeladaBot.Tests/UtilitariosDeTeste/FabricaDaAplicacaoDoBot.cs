using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Inicia o bot com uma configuração de teste válida: os limites e frases do appsettings.json mais ids falsos, e o WAHA trocado
/// por <see cref="Waha"/>, um stub sob o ClienteWaha real. Roda como "Testing" para os user-secrets do desenvolvedor nunca
/// chegarem a um teste, com as verificações de serviço que o Development ativa.
/// </summary>
public sealed class FabricaDaAplicacaoDoBot : WebApplicationFactory<Program>
{
    public const string IdDoGrupo = "120363000000001234@g.us";
    public const string NomeDoPresidente = "Presidente Fulano";
    public const string IdDoTelefoneDoPresidente = "35600000101@c.us";
    public const string LidDoPresidente = "100000000000202@lid";
    public const string NomeDoVice = "Vice Ciclano";
    public const string IdDoTelefoneDoVice = "35600000303@c.us";
    public const string UrlBaseDoWaha = "http://waha.test:3000";
    public const string ChaveDaApiDoWaha = "test-waha-api-key";
    public const string ChaveHmacDoWebhookDoWaha = "test-waha-webhook-hmac-key-of-at-least-32-characters";
    public const string SessaoDoWaha = "default";

    private const string AmbienteDeTeste = "Testing";

    private readonly ColetorDeLogs _coletorDeLogs = new();
    private readonly List<Action<IServiceCollection>> _substituicoesDeServico = [];

    private readonly Dictionary<string, string?> _configuracoes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bot:Grupos:0:Id"] = IdDoGrupo,
        ["Bot:Grupos:0:Presidencia:0:Nome"] = NomeDoPresidente,
        ["Bot:Grupos:0:Presidencia:0:Ids:0"] = IdDoTelefoneDoPresidente,
        ["Bot:Grupos:0:Presidencia:0:Ids:1"] = LidDoPresidente,
        ["Bot:Grupos:0:Presidencia:1:Nome"] = NomeDoVice,
        ["Bot:Grupos:0:Presidencia:1:Ids:0"] = IdDoTelefoneDoVice,
        ["Bot:Fuso"] = "Europe/Rome",
        ["Waha:UrlBase"] = UrlBaseDoWaha,
        ["Waha:ChaveDaApi"] = ChaveDaApiDoWaha,
        ["Waha:ChaveHmacDoWebhook"] = ChaveHmacDoWebhookDoWaha,
        ["Waha:Sessao"] = SessaoDoWaha,
    };

    public FabricaDaAplicacaoDoBot()
        : this(new Dictionary<string, string?>())
    {
    }

    private FabricaDaAplicacaoDoBot(IReadOnlyDictionary<string, string?> substituicoes)
    {
        foreach (KeyValuePair<string, string?> configuracao in substituicoes)
        {
            _configuracoes[configuracao.Key] = configuracao.Value;
        }
    }

    /// <summary>Todo log que a aplicação escreveu até agora, incluindo os de uma inicialização que falhou.</summary>
    public IReadOnlyList<LogColetado> Logs => _coletorDeLogs.Entradas;

    /// <summary>Espera o primeiro log da <paramref name="categoria"/>, que um serviço em segundo plano pode escrever depois.</summary>
    public Task<LogColetado> EsperarLogAsync(string categoria) => _coletorDeLogs.EsperarLogAsync(categoria);

    /// <summary>O que o WAHA recebeu e o que ele responde.</summary>
    public ManipuladorFalsoDoWaha Waha { get; } = new();

    /// <summary>Uma fábrica cuja configuração substitui ou soma estas chaves, como variáveis de ambiente fazem em produção.</summary>
    public static FabricaDaAplicacaoDoBot ComConfiguracoes(IReadOnlyDictionary<string, string?> substituicoes) => new(substituicoes);

    /// <summary>Substitui serviços depois que a aplicação registrou os seus, como um mensageiro falso ou um relógio falso.</summary>
    public FabricaDaAplicacaoDoBot ComServicos(Action<IServiceCollection> configurar)
    {
        _substituicoesDeServico.Add(configurar);

        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(AmbienteDeTeste);
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(_configuracoes));
        builder.ConfigureLogging(logging => logging.AddProvider(_coletorDeLogs));
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<ClienteWaha>().ConfigurePrimaryHttpMessageHandler(() => Waha);

            foreach (Action<IServiceCollection> configurar in _substituicoesDeServico)
            {
                configurar(services);
            }
        });
    }
}
