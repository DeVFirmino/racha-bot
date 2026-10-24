using System.Globalization;
using System.Text;

namespace PeladaBot.Comandos;

/// <summary>
/// Encontra o comando que uma mensagem pede: a primeira palavra do próprio texto da mensagem, como palavra inteira,
/// ignorando caixa e acentos. Para "!sortear"/"!times", a segunda palavra "pagos" (mesma tolerância) pede
/// o sorteio de emergência, que ignora quem está sem ✅. Quem chama passa só esse texto, nunca o texto de uma
/// mensagem citada.
/// </summary>
public static class LeitorDeComando
{
    private const string PalavraPagos = "pagos";

    private static readonly Dictionary<string, TipoDeComando> _comandosPorPalavra = new(StringComparer.Ordinal)
    {
        ["!sortear"] = TipoDeComando.Sortear,
        ["!times"] = TipoDeComando.Sortear,
        ["!pendentes"] = TipoDeComando.Cobrar,
        ["!ajuda"] = TipoDeComando.Ajuda,
        ["!cobrar"] = TipoDeComando.Cobrar,
    };

    /// <summary>
    /// O comando de <paramref name="texto"/>, ou null quando sua primeira palavra não é um: "!SORTÉAR" é um sorteio,
    /// enquanto "!sortearabc", "bora !sortear" e um texto vazio não são nada. "!sortear pagosx" não conta como
    /// "!sortear pagos": a segunda palavra tem de ser exatamente "pagos".
    /// </summary>
    public static ComandoLido? Ler(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        string[] palavras = texto.Split((char[]?)null, 3, StringSplitOptions.RemoveEmptyEntries);
        bool encontrado =
            _comandosPorPalavra.TryGetValue(ChaveDeComparacao(palavras[0]), out TipoDeComando comando);

        if (encontrado is false)
        {
            return null;
        }

        bool somentePagos = comando is TipoDeComando.Sortear
            && palavras.Length > 1
            && string.Equals(ChaveDeComparacao(palavras[1]), PalavraPagos, StringComparison.Ordinal);

        return new ComandoLido { Tipo = comando, SomentePagos = somentePagos };
    }

    /// <summary>A palavra sem acentos e em minúsculas, qualquer que seja a forma Unicode em que foi digitada.</summary>
    private static string ChaveDeComparacao(string palavra)
    {
        string decomposta = palavra.Normalize(NormalizationForm.FormD);
        StringBuilder construtor = new(decomposta.Length);

        foreach (char caractere in decomposta)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) is not UnicodeCategory.NonSpacingMark)
            {
                construtor.Append(char.ToLowerInvariant(caractere));
            }
        }

        return construtor.ToString();
    }
}
