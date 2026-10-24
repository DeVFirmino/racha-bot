using Microsoft.Extensions.Logging;

namespace PeladaBot.Tests.UtilitariosDeTeste;

public sealed record LogColetado
{
    public required string Categoria { get; init; }

    public required LogLevel Nivel { get; init; }

    public required string Mensagem { get; init; }

    public Exception? Excecao { get; init; }
}
