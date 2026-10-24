using System.Collections.Concurrent;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Faz as vezes do histórico do grupo. Mensagens ficam na ordem em que foram adicionadas, que representa a ordem de chegada.
/// Acha uma mensagem pelo id completo do WAHA ou só pela sua chave do WhatsApp, do jeito que uma citação a referencia.
/// </summary>
public sealed class HistoricoDoGrupoFalso : IHistoricoDoGrupo
{
    private readonly List<MensagemDoGrupo> _mensagens = [];
    private readonly ConcurrentQueue<DateTimeOffset> _consultasDeHistorico = new();
    private readonly ConcurrentQueue<string> _consultasDeMensagem = new();

    /// <summary>Quando true, toda leitura falha, como quando o WAHA está fora do ar.</summary>
    public bool Indisponivel { get; set; }

    /// <summary>O "desde" de toda leitura de histórico, em ordem.</summary>
    public IReadOnlyList<DateTimeOffset> ConsultasDeHistorico => [.. _consultasDeHistorico];

    /// <summary>Os ids de toda mensagem pedida, em ordem.</summary>
    public IReadOnlyList<string> ConsultasDeMensagem => [.. _consultasDeMensagem];

    public HistoricoDoGrupoFalso Com(params MensagemDoGrupo[] mensagens)
    {
        _mensagens.AddRange(mensagens);

        return this;
    }

    public Task<MensagemDoGrupo?> ObterMensagemAsync(string idDoChat, string idDaMensagem, CancellationToken cancellationToken)
    {
        _consultasDeMensagem.Enqueue(idDaMensagem);

        if (Indisponivel)
        {
            return Task.FromResult<MensagemDoGrupo?>(null);
        }

        return Task.FromResult(_mensagens.LastOrDefault(
            mensagem => mensagem.Id == idDaMensagem || mensagem.Id.Split('_').Contains(idDaMensagem)));
    }

    public Task<IReadOnlyList<MensagemDoGrupo>?> ObterMensagensDesdeAsync(
        string idDoChat, DateTimeOffset desde, CancellationToken cancellationToken)
    {
        _consultasDeHistorico.Enqueue(desde);

        if (Indisponivel)
        {
            return Task.FromResult<IReadOnlyList<MensagemDoGrupo>?>(null);
        }

        IReadOnlyList<MensagemDoGrupo> mensagens = [.. _mensagens.Where(mensagem => mensagem.EnviadaEm >= desde)];

        return Task.FromResult<IReadOnlyList<MensagemDoGrupo>?>(mensagens);
    }
}
