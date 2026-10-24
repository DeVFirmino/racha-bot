using Microsoft.Extensions.Options;
using PeladaBot.Respostas;

namespace PeladaBot.Configuracao;

/// <summary>Carrega o fuso da configuração na inicialização, para uma tzdata ausente falhar rápido, não no primeiro sorteio.</summary>
public sealed partial class VerificacaoDoFuso : IHostedService
{
    private readonly IOptions<OpcoesDoBot> _opcoes;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VerificacaoDoFuso> _logger;

    public VerificacaoDoFuso(IOptions<OpcoesDoBot> opcoes, TimeProvider timeProvider, ILogger<VerificacaoDoFuso> logger)
    {
        _opcoes = opcoes;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TimeZoneInfo fuso = HorarioDoGrupo.Carregar(_opcoes.Value.Fuso);

        RegistrarFusoCarregado(_logger, fuso.Id, fuso.GetUtcOffset(_timeProvider.GetUtcNow()));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Fuso horário {Fuso} carregado: offset UTC {Offset} agora")]
    private static partial void RegistrarFusoCarregado(ILogger logger, string fuso, TimeSpan offset);
}
