using System.Globalization;

namespace PeladaBot.Card;

/// <summary>
/// Ajusta uma linha à largura que tem, medida com as fontes embutidas. A fonte encolhe primeiro, até o
/// <see cref="EstiloDoTexto.TamanhoMinimo"/> do estilo; só então o texto é cortado com "…".
/// </summary>
internal static class AjustadorDeTexto
{
    private const string Reticencias = "…";

    public static TextoAjustado AjustarLinha(string texto, EstiloDoTexto estilo, float larguraMaxima)
    {
        string linha = LinhaUnica(texto);

        for (float tamanho = estilo.TamanhoPreferido; tamanho > estilo.TamanhoMinimo; tamanho--)
        {
            if (Cabe(linha, estilo, tamanho, larguraMaxima))
            {
                return Ajustada(linha, estilo, tamanho);
            }
        }

        string cabivel = Cabe(linha, estilo, estilo.TamanhoMinimo, larguraMaxima) ? linha : Cortar(linha, estilo, larguraMaxima);

        return Ajustada(cabivel, estilo, estilo.TamanhoMinimo);
    }

    /// <summary>
    /// Colapsa toda sequência de espaços, quebras de linha inclusive, em um espaço: cada texto é uma linha.
    /// </summary>
    private static string LinhaUnica(string texto) =>
        string.Join(' ', texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// O maior início de <paramref name="texto"/> que cabe no tamanho mínimo com "…" depois, cortado entre
    /// caracteres inteiros.
    /// </summary>
    private static string Cortar(string texto, EstiloDoTexto estilo, float larguraMaxima)
    {
        List<int> finaisDosElementos = FinaisDosElementosDeTexto(texto);
        int menos = 0;
        int mais = finaisDosElementos.Count;

        while (menos < mais)
        {
            int meio = (menos + mais + 1) / 2;

            if (Cabe(ManterComReticencias(texto, finaisDosElementos, meio), estilo, estilo.TamanhoMinimo, larguraMaxima))
            {
                menos = meio;
            }
            else
            {
                mais = meio - 1;
            }
        }

        return ManterComReticencias(texto, finaisDosElementos, menos);
    }

    private static string ManterComReticencias(string texto, List<int> finaisDosElementos, int quantidadeDeElementos)
    {
        string mantido = quantidadeDeElementos == 0 ? string.Empty : texto[..finaisDosElementos[quantidadeDeElementos - 1]];

        return mantido.TrimEnd() + Reticencias;
    }

    /// <summary>
    /// Onde cada caractere percebido pelo usuário termina, para um corte nunca separar um acento ou emoji de sua base.
    /// </summary>
    private static List<int> FinaisDosElementosDeTexto(string texto)
    {
        List<int> finais = [];
        int indice = 0;

        while (indice < texto.Length)
        {
            indice += StringInfo.GetNextTextElementLength(texto, indice);
            finais.Add(indice);
        }

        return finais;
    }

    private static bool Cabe(string texto, EstiloDoTexto estilo, float tamanho, float larguraMaxima) =>
        FontesDoCard.Medir(texto, estilo.Peso, tamanho, estilo.Espacamento).Tinta.Width <= larguraMaxima;

    private static TextoAjustado Ajustada(string linha, EstiloDoTexto estilo, float tamanho) => new()
    {
        Texto = linha,
        Peso = estilo.Peso,
        TamanhoDaFonte = tamanho,
        Espacamento = estilo.Espacamento,
        Extensao = FontesDoCard.Medir(linha, estilo.Peso, tamanho, estilo.Espacamento),
    };
}
