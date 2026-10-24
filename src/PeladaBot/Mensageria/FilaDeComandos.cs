using System.Threading.Channels;

namespace PeladaBot.Mensageria;

/// <summary>A fila limitada entre o webhook e o único consumidor; uma queda a perde.</summary>
public sealed class FilaDeComandos
{
    /// <summary>Bem acima do que o grupo envia; o consumidor leva só alguns segundos por comando.</summary>
    public const int Capacidade = 100;

    private static readonly BoundedChannelOptions _opcoes = new(Capacidade)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait,
    };

    private readonly Channel<ComandoRecebido> _canal = Channel.CreateBounded<ComandoRecebido>(_opcoes);

    /// <summary>False quando a fila está cheia ou fechada: o comando é descartado, nunca esperado.</summary>
    public bool TentarEnfileirar(ComandoRecebido comando)
    {
        ArgumentNullException.ThrowIfNull(comando);

        return _canal.Writer.TryWrite(comando);
    }

    /// <summary>Todo comando enfileirado, em ordem, até fechar e esvaziar.</summary>
    public IAsyncEnumerable<ComandoRecebido> LerTudoAsync(CancellationToken cancellationToken) =>
        _canal.Reader.ReadAllAsync(cancellationToken);

    /// <summary>Fecha a fila para novos comandos; os que já estão nela ainda são lidos.</summary>
    public void Completar() => _canal.Writer.TryComplete();
}
