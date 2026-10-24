using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>
/// Uma linha de texto já ajustada e posicionada. O desenhista desenha <see cref="Texto"/> a partir de
/// <see cref="Origem"/> e não pinta nada fora de <see cref="Limites"/>.
/// </summary>
public sealed record TextoDoCard
{
    /// <summary>
    /// O que é desenhado: o nome como escrito, ou cortado com "…" quando não cabe nem no tamanho mínimo.
    /// </summary>
    public required string Texto { get; init; }

    public required PesoDaFonteDoCard Peso { get; init; }

    public required float TamanhoDaFonte { get; init; }

    /// <summary>Pixels a mais entre uma letra e a seguinte; 0 deixa o espaçamento natural da fonte.</summary>
    public required float Espacamento { get; init; }

    /// <summary>Canto superior esquerdo da caixa da linha, onde o desenho começa.</summary>
    public required PointF Origem { get; init; }

    /// <summary>A caixa da linha mais a tinta de cada glifo, medida com as fontes embutidas.</summary>
    public required RectangleF Limites { get; init; }
}
