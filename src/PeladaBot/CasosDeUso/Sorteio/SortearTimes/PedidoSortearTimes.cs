using PeladaBot.EscolhaDaLista;

namespace PeladaBot.CasosDeUso.Sorteio.SortearTimes;

/// <summary>Um !sortear ou !times do grupo.</summary>
public sealed record PedidoSortearTimes
{
    public required string IdDoGrupo { get; init; }

    public required string IdDoRemetente { get; init; }

    /// <summary>O id do WAHA da mensagem do comando, que a resposta cita.</summary>
    public required string IdDaMensagem { get; init; }

    /// <summary>A lista que o comando cita, ou null para pegar a mais recente da presidência.</summary>
    public required MensagemCitada? Citada { get; init; }

    /// <summary>
    /// True em "!sortear pagos"/"!times pagos": sorteia só entre quem já tem ✅, ignorando os pendentes.
    /// </summary>
    public required bool SomentePagos { get; init; }
}
