namespace PeladaBot.EscolhaDaLista;

/// <summary>A mensagem que um comando cita. Quando é uma lista, o comando a usa, por mais antiga que seja.</summary>
public sealed record MensagemCitada
{
    /// <summary>O id que o WAHA dá à mensagem citada, suficiente para pedir ao WAHA a mensagem em si.</summary>
    public required string Id { get; init; }

    /// <summary>O texto citado como veio com o comando; null significa pedir a mensagem ao WAHA.</summary>
    public required string? Texto { get; init; }
}
