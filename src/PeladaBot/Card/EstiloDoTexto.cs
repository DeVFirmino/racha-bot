namespace PeladaBot.Card;

/// <summary>
/// Como um texto do card é escrito: o peso, o tamanho que ele quer ter, o menor a que pode encolher para caber e o
/// espaço extra entre as letras.
/// </summary>
internal sealed record EstiloDoTexto
{
    public required PesoDaFonteDoCard Peso { get; init; }

    public required float TamanhoPreferido { get; init; }

    /// <summary>Abaixo disso o texto não encolhe mais: é cortado com "…".</summary>
    public required float TamanhoMinimo { get; init; }

    /// <summary>Pixels a mais entre uma letra e a seguinte; 0 deixa o espaçamento natural da fonte.</summary>
    public required float Espacamento { get; init; }
}
