namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Acorda quem espera uma condição a cada mudança, no lugar de conferir de tempos em tempos em tempo real. O prazo é só
/// uma rede de segurança para um teste quebrado: um teste que passa nunca chega nele.
/// </summary>
public sealed class AvisoDeMudanca : IDisposable
{
    private static readonly TimeSpan _prazo = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _mudou = new(0);

    public void Avisar() => _mudou.Release();

    /// <summary>Espera até <paramref name="condicao"/> valer, conferindo de novo a cada aviso.</summary>
    public async Task EsperarAsync(Func<bool> condicao)
    {
        using CancellationTokenSource prazo = new(_prazo);

        while (condicao() is false)
        {
            await EsperarProximoAsync(prazo.Token);
        }
    }

    /// <summary>Espera o próximo aviso, ou falha com <see cref="TimeoutException"/> quando o prazo acaba.</summary>
    public async Task EsperarProximoAsync(CancellationToken prazo)
    {
        try
        {
            await _mudou.WaitAsync(prazo);
        }
        catch (OperationCanceledException excecao)
        {
            throw new TimeoutException($"Nada mudou em {_prazo.TotalSeconds:0} s: a condição esperada nunca chegou.", excecao);
        }
    }

    public void Dispose() => _mudou.Dispose();
}
