using Microsoft.Extensions.Options;

namespace PeladaBot.WhatsApp;

/// <summary>Pergunta ao WAHA uma vez na inicialização se a sessão funciona, logando um erro quando não.</summary>
public sealed partial class VerificacaoDaSessaoWaha : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OpcoesDoWaha> _opcoes;
    private readonly ILogger<VerificacaoDaSessaoWaha> _logger;

    public VerificacaoDaSessaoWaha(
        IServiceScopeFactory scopeFactory, IOptions<OpcoesDoWaha> opcoes, ILogger<VerificacaoDaSessaoWaha> logger)
    {
        _scopeFactory = scopeFactory;
        _opcoes = opcoes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string sessao = _opcoes.Value.Sessao;

        // O cliente tipado vive em um escopo, nunca neste singleton, para a factory de HttpClient poder girar seu handler.
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        ClienteWaha waha = scope.ServiceProvider.GetRequiredService<ClienteWaha>();

        SessaoWaha? encontrada = await waha.ObterSessaoAsync(stoppingToken);

        if (encontrada is null)
        {
            RegistrarSessaoDesconhecida(_logger, sessao);
        }
        else if (encontrada.Status is StatusDaSessaoWaha.Funcionando)
        {
            RegistrarSessaoFuncionando(_logger, sessao);
        }
        else if (encontrada.Status is StatusDaSessaoWaha.Iniciando)
        {
            RegistrarSessaoIniciando(_logger, sessao);
        }
        else
        {
            RegistrarSessaoNaoFuncionando(_logger, sessao, encontrada.Status ?? "sem status");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sessão do WAHA {Sessao} está WORKING")]
    private static partial void RegistrarSessaoFuncionando(ILogger logger, string sessao);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sessão do WAHA {Sessao} ainda está STARTING: ainda não é erro")]
    private static partial void RegistrarSessaoIniciando(ILogger logger, string sessao);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sessão do WAHA {Sessao} está {Status}: sem leituras ou respostas")]
    private static partial void RegistrarSessaoNaoFuncionando(ILogger logger, string sessao, string status);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sessão do WAHA {Sessao}: seu status não pôde ser lido na inicialização")]
    private static partial void RegistrarSessaoDesconhecida(ILogger logger, string sessao);
}
