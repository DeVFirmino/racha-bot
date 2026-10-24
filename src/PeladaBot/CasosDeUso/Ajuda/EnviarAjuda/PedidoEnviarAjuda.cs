namespace PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;

/// <summary>Um !ajuda do grupo.</summary>
public sealed record PedidoEnviarAjuda
{
    public required string IdDoGrupo { get; init; }

    public required string IdDoRemetente { get; init; }

    /// <summary>O id do WAHA da mensagem do comando, que a resposta cita.</summary>
    public required string IdDaMensagem { get; init; }
}
