using PeladaBot.Comandos;

namespace PeladaBot.Mensageria;

/// <summary>O comando que uma resposta responde: para onde vai, o que cita, e quem pediu, para as janelas de resposta.</summary>
public sealed record DestinoDaResposta
{
    public required string IdDoGrupo { get; init; }

    /// <summary>O id do WAHA da mensagem do comando. A resposta a cita.</summary>
    public required string IdDaMensagem { get; init; }

    public required string IdDoRemetente { get; init; }

    public required TipoDeComando Comando { get; init; }
}
