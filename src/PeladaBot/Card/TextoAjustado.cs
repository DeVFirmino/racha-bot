namespace PeladaBot.Card;

/// <summary>Uma linha cujo texto e tamanho já estão definidos, ainda sem lugar no card.</summary>
internal sealed record TextoAjustado
{
    public required string Texto { get; init; }

    public required PesoDaFonteDoCard Peso { get; init; }

    public required float TamanhoDaFonte { get; init; }

    public required float Espacamento { get; init; }

    public required ExtensaoDoTexto Extensao { get; init; }

    public float AlturaDaLinha => Extensao.CaixaDaLinha.Height;
}
