using PeladaBot.EscolhaDaLista;

namespace PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;

/// <summary>Um !cobrar do grupo.</summary>
public sealed record PedidoCobrarPendentes
{
    public required string IdDoGrupo { get; init; }

    public required string IdDoRemetente { get; init; }

    /// <summary>O id do WAHA da mensagem do comando, que a resposta cita.</summary>
    public required string IdDaMensagem { get; init; }

    /// <summary>A lista que o comando cita, ou null para pegar a mais recente de qualquer um.</summary>
    public required MensagemCitada? Citada { get; init; }
}
