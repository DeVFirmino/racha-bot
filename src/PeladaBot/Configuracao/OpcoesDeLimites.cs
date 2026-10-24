namespace PeladaBot.Configuracao;

/// <summary>Os limites do bot; um ambiente pode reduzir os valores padrão.</summary>
public sealed class OpcoesDeLimites
{
    /// <summary>A janela de recusa para quem está fora da presidência. Spec: 10 minutos.</summary>
    public TimeSpan JanelaDeRespostaNaoPresidente { get; init; }

    /// <summary>
    /// Depois de uma cobrança no grupo, quem está fora da presidência só cobra de novo passado isso. Spec: 30
    /// minutos.
    /// </summary>
    public TimeSpan JanelaDeCobranca { get; init; }

    /// <summary>O disjuntor deixa passar no máximo esta quantidade de mensagens em qualquer <see cref="JanelaDoDisjuntor"/>. Spec: 30.</summary>
    public int MaximoDeMensagensDoDisjuntor { get; init; }

    /// <summary>A janela deslizante do disjuntor. Spec: 60 minutos.</summary>
    public TimeSpan JanelaDoDisjuntor { get; init; }

    /// <summary>Um comando mais antigo que isso quando o bot o recebe é ignorado. Spec: 120 segundos.</summary>
    public TimeSpan IdadeMaximaDoComando { get; init; }

    /// <summary>Uma lista que o comando não cita só é usada até esta idade. Spec: 72 horas.</summary>
    public TimeSpan IdadeMaximaDaLista { get; init; }
}
