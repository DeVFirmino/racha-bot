using PeladaBot.Comandos;
using PeladaBot.EscolhaDaLista;

namespace PeladaBot.Mensageria;

/// <summary>Um comando que passou pelos filtros do webhook e espera na fila pelo consumidor.</summary>
public sealed record ComandoRecebido
{
    public required TipoDeComando Comando { get; init; }

    /// <summary>
    /// True em "!sortear pagos"/"!times pagos"; só <see cref="TipoDeComando.Sortear"/> usa. Vale false para os
    /// outros comandos.
    /// </summary>
    public required bool SomentePagos { get; init; }

    /// <summary>O id do WAHA da mensagem do comando, estável entre entregas; dirige a dedupe.</summary>
    public required string IdDaMensagem { get; init; }

    public required string IdDoGrupo { get; init; }

    /// <summary>Quem enviou o comando, telefone (@c.us) ou LID (@lid); só esse id autoriza.</summary>
    public required string IdDoRemetente { get; init; }

    /// <summary>A mensagem que o comando cita, ou null.</summary>
    public required MensagemCitada? Citada { get; init; }
}
