using Microsoft.Extensions.Time.Testing;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Um <see cref="FakeTimeProvider"/> que avisa quando alguém começa a esperar nele. Um teste da aplicação inteira, cujo
/// consumidor roda numa thread própria, avança o tempo só quando o consumidor de fato parou num timer — como a pausa antes
/// de cada resposta —, em vez de dormir em tempo real esperando por ele.
/// </summary>
public sealed class RelogioDeTeste : FakeTimeProvider
{
    private int _timersNovos;

    public RelogioDeTeste(DateTimeOffset inicio)
        : base(inicio)
    {
    }

    /// <summary>Disparado a cada timer criado, na thread de quem o criou.</summary>
    public event Action? TimerCriado;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ITimer timer = base.CreateTimer(callback, state, dueTime, period);

        Interlocked.Increment(ref _timersNovos);
        TimerCriado?.Invoke();

        return timer;
    }

    /// <summary>True quando alguém criou um timer desde a última consulta; a consulta zera a contagem.</summary>
    public bool AlguemComecouAEsperar() => Interlocked.Exchange(ref _timersNovos, 0) > 0;
}
