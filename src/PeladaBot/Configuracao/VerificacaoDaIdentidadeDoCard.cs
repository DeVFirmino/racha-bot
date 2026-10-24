using PeladaBot.Card;

namespace PeladaBot.Configuracao;

/// <summary>Carrega os escudos após validar as opções e antes de iniciar o consumidor da fila.</summary>
public sealed class VerificacaoDaIdentidadeDoCard : IHostedService
{
    private readonly IServiceProvider _servicos;

    public VerificacaoDaIdentidadeDoCard(IServiceProvider servicos)
    {
        _servicos = servicos;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _servicos.GetRequiredService<ProvedorDeIdentidadeDoCard>();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
