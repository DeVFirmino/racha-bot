using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>Guarda todo log que a aplicação escreve, para um teste poder verificar o que chegou ao log e o que nunca chegou.</summary>
public sealed class ColetorDeLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<LogColetado> _logs = new();
    private readonly AvisoDeMudanca _aviso = new();

    public IReadOnlyList<LogColetado> Entradas => [.. _logs];

    public ILogger CreateLogger(string categoryName) => new LoggerColetor(categoryName, _logs, _aviso);

    /// <summary>Espera o primeiro log da <paramref name="categoria"/>, escrito por qualquer thread da aplicação.</summary>
    public async Task<LogColetado> EsperarLogAsync(string categoria)
    {
        await _aviso.EsperarAsync(() => _logs.Any(log => log.Categoria == categoria));

        return _logs.First(log => log.Categoria == categoria);
    }

    public void Dispose() => _aviso.Dispose();

    private sealed class LoggerColetor : ILogger
    {
        private readonly string _categoria;
        private readonly ConcurrentQueue<LogColetado> _logs;
        private readonly AvisoDeMudanca _aviso;

        public LoggerColetor(string categoria, ConcurrentQueue<LogColetado> logs, AvisoDeMudanca aviso)
        {
            _categoria = categoria;
            _logs = logs;
            _aviso = aviso;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel is not LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _logs.Enqueue(new LogColetado
            {
                Categoria = _categoria,
                Nivel = logLevel,
                Mensagem = formatter(state, exception),
                Excecao = exception,
            });
            _aviso.Avisar();
        }
    }
}
