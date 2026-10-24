using Microsoft.Extensions.Options;
using PeladaBot.Configuracao;

namespace PeladaBot.Acesso;

/// <summary>Uma janela de envio deslizante para todo o número do bot; não thread-safe.</summary>
public sealed class DisjuntorDeEnvio
{
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<OpcoesDoBot> _opcoes;

    // Timestamp monotônico de cada mensagem reservada, mais antiga primeiro; uma mudança no relógio não abre nem fecha a janela.
    private readonly Queue<long> _reservas = new();

    public DisjuntorDeEnvio(TimeProvider timeProvider, IOptions<OpcoesDoBot> opcoes)
    {
        _timeProvider = timeProvider;
        _opcoes = opcoes;
    }

    /// <summary>Reserva um envio na janela; false significa cheio, nada reservado, quem chamou não deve enviar.</summary>
    public bool TentarReservar()
    {
        OpcoesDeLimites limites = _opcoes.Value.Limites;

        while (_reservas.TryPeek(out long maisAntiga) && _timeProvider.GetElapsedTime(maisAntiga) >= limites.JanelaDoDisjuntor)
        {
            _reservas.Dequeue();
        }

        if (_reservas.Count >= limites.MaximoDeMensagensDoDisjuntor)
        {
            return false;
        }

        _reservas.Enqueue(_timeProvider.GetTimestamp());

        return true;
    }
}
