using System.Globalization;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;

namespace PeladaBot.Card;

/// <summary>
/// As fontes embutidas no assembly; o card nunca usa uma fonte do sistema. O layout mede com as mesmas fontes
/// com que o desenhista desenha, então um nome que cabe no layout cabe no card.
/// </summary>
internal static class FontesDoCard
{
    private const string PrefixoDoRecurso = "PeladaBot.Card.Fonts.";

    /// <summary>
    /// Os acentos mais altos, os descendentes mais profundos e os emojis que o card usa: toda linha ganha a
    /// mesma caixa, para as linhas de base se alinharem.
    /// </summary>
    private const string ReferenciaDaLinha = "ÁÉÔÃÇjgpqy|🧤⚽";

    /// <summary>Uma letra sem descendente: o pé dela é a linha de base.</summary>
    private const string ReferenciaDaLinhaDeBase = "H";

    private static readonly FontFamily _semiNegrito = Carregar("BarlowCondensed-SemiBold.ttf");
    private static readonly FontFamily _extraNegrito = Carregar("BarlowCondensed-ExtraBold.ttf");

    /// <summary>A Noto Emoji desenha o que a Barlow não tem: o ⚽ do título e o 🧤 dos goleiros.</summary>
    private static readonly FontFamily[] _fontesDeReserva = [Carregar("NotoEmoji[wght].ttf")];

    public static ExtensaoDoTexto Medir(string texto, PesoDaFonteDoCard peso, float tamanho, float espacamento)
    {
        TextOptions opcoes = Opcoes(peso, tamanho);

        float avanco = espacamento == 0
            ? TextMeasurer.MeasureAdvance(texto, opcoes).Width
            : AvancosDosGrafemas(texto, opcoes).Sum(grafema => grafema.Avanco) + (espacamento * Math.Max(0, ContarGrafemas(texto) - 1));
        FontRectangle referencia = TextMeasurer.MeasureBounds(ReferenciaDaLinha, opcoes);
        FontRectangle tinta = TextMeasurer.MeasureBounds(texto, opcoes);
        FontRectangle caixaDaLinha = new(0, referencia.Top, avanco, referencia.Height);
        float linhaDeBase = TextMeasurer.MeasureBounds(ReferenciaDaLinhaDeBase, opcoes).Bottom;

        // Com espaçamento as letras se afastam: na horizontal, a tinta vai até onde o avanço vai.
        if (espacamento != 0)
        {
            tinta = new FontRectangle(0, tinta.Y, avanco, tinta.Height);
        }

        return new ExtensaoDoTexto(caixaDaLinha, FontRectangle.Union(caixaDaLinha, tinta), linhaDeBase);
    }

    /// <summary>
    /// O que desenhar para <paramref name="texto"/> sair exatamente como o layout mediu: o texto inteiro de uma vez,
    /// ou, com espaçamento, uma letra por vez, cada uma afastada da anterior.
    /// </summary>
    public static IEnumerable<(string Texto, RichTextOptions Opcoes)> PartesDoDesenho(TextoDoCard texto)
    {
        if (texto.Espacamento == 0)
        {
            yield return (texto.Texto, OpcoesDeDesenho(texto, texto.Origem));

            yield break;
        }

        float x = texto.Origem.X;

        foreach ((string grafema, float avanco) in AvancosDosGrafemas(texto.Texto, Opcoes(texto.Peso, texto.TamanhoDaFonte)))
        {
            yield return (grafema, OpcoesDeDesenho(texto, new PointF(x, texto.Origem.Y)));
            x += avanco + texto.Espacamento;
        }
    }

    private static RichTextOptions OpcoesDeDesenho(TextoDoCard texto, PointF origem) => new(CriarFonte(texto.Peso, texto.TamanhoDaFonte))
    {
        FallbackFontFamilies = _fontesDeReserva,
        Origin = origem,
    };

    private static TextOptions Opcoes(PesoDaFonteDoCard peso, float tamanho) => new(CriarFonte(peso, tamanho))
    {
        FallbackFontFamilies = _fontesDeReserva,
    };

    /// <summary>Cada caractere percebido pelo usuário, um acento nunca separado da sua letra, e quanto ele avança.</summary>
    private static IEnumerable<(string Grafema, float Avanco)> AvancosDosGrafemas(string texto, TextOptions opcoes)
    {
        TextElementEnumerator grafemas = StringInfo.GetTextElementEnumerator(texto);

        while (grafemas.MoveNext())
        {
            string grafema = grafemas.GetTextElement();

            yield return (grafema, TextMeasurer.MeasureAdvance(grafema, opcoes).Width);
        }
    }

    private static int ContarGrafemas(string texto) => new StringInfo(texto).LengthInTextElements;

    private static Font CriarFonte(PesoDaFonteDoCard peso, float tamanho)
    {
        FontFamily familia = peso switch
        {
            PesoDaFonteDoCard.SemiNegrito => _semiNegrito,
            PesoDaFonteDoCard.ExtraNegrito => _extraNegrito,
            _ => throw new ArgumentOutOfRangeException(nameof(peso), peso, "Peso de fonte do card desconhecido."),
        };

        return familia.CreateFont(tamanho);
    }

    /// <summary>
    /// Cada arquivo vai para uma coleção própria, então uma fonte é alcançada pelo arquivo de onde veio, nunca
    /// por um nome de família.
    /// </summary>
    private static FontFamily Carregar(string nomeDoArquivo)
    {
        using Stream stream = typeof(FontesDoCard).Assembly.GetManifestResourceStream(PrefixoDoRecurso + nomeDoArquivo)
            ?? throw new InvalidOperationException($"A fonte {nomeDoArquivo} não está embutida no assembly.");

        FontCollection colecao = new();

        return colecao.Add(stream);
    }
}
