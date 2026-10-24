namespace PeladaBot.Respostas;

/// <summary>Respostas curtas quando um comando não pode seguir. Nenhuma delas tem rodapé.</summary>
public static class RespostasDeErro
{
    /// <summary>!sortear ou !times citou algo que não é uma lista, ou não achou lista para usar.</summary>
    public const string CitarAListaParaSortear = "Não achei uma lista para sortear. Envie a lista completa com o título Lista de jogadores. Depois, segure essa mensagem, toque em Responder e envie !sortear.";

    /// <summary>!pendentes ou !cobrar citou algo que não é uma lista, ou não achou lista para usar.</summary>
    public const string CitarAListaParaCobrar = "Não achei uma lista para cobrar. Envie a lista completa com o título Lista de jogadores. Depois, segure essa mensagem, toque em Responder e envie !pendentes.";

    /// <summary>O bot não conseguiu ler a mensagem citada por completo. Nunca a troca por outra lista.</summary>
    public const string ListaCitadaIlegivel = "Não consegui ler essa lista. Reenvie a lista completa e responda à nova mensagem com o comando.";

    /// <summary>!sortear ou !times de alguém que não é o presidente nem um admin.</summary>
    public const string SoOPresidentePodeSortear = "Esse comando é só para presidente ou admin cadastrados no bot. Para ver quem falta pagar, use !pendentes.";
}
