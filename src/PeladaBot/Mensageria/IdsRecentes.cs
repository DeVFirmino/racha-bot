namespace PeladaBot.Mensageria;

/// <summary>Ids de mensagem que o consumidor pegou, lembrados por 10 min para uma entrega repetida ser respondida uma vez.</summary>
public sealed class IdsRecentes
{
    /// <summary>Por quanto tempo um id é lembrado. Spec: 10 minutos.</summary>
    public static readonly TimeSpan Memoria = TimeSpan.FromMinutes(10);

    private readonly TimeProvider _timeProvider;

    // Quando cada id foi lembrado, mais antigo primeiro, como timestamps monotônicos do TimeProvider.
    private readonly Queue<(string IdDaMensagem, long LembradoEm)> _porIdade = new();
    private readonly HashSet<string> _idsDeMensagem = new(StringComparer.Ordinal);

    public IdsRecentes(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Lembra o id; false quando já lembrado, significando que a mensagem é uma repetição.</summary>
    public bool TentarLembrar(string idDaMensagem)
    {
        ArgumentNullException.ThrowIfNull(idDaMensagem);

        while (_porIdade.TryPeek(out (string IdDaMensagem, long LembradoEm) maisAntigo)
            && _timeProvider.GetElapsedTime(maisAntigo.LembradoEm) >= Memoria)
        {
            _porIdade.Dequeue();
            _idsDeMensagem.Remove(maisAntigo.IdDaMensagem);
        }

        if (_idsDeMensagem.Add(idDaMensagem) is false)
        {
            return false;
        }

        _porIdade.Enqueue((idDaMensagem, _timeProvider.GetTimestamp()));

        return true;
    }
}
