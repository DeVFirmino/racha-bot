using System.Globalization;
using System.Text;

namespace PeladaBot.Listas;

/// <summary>Regras de limpeza e comparação para o texto da lista do grupo.</summary>
internal static class TextoDaLista
{
    private const int SeletorDeVariacaoDeTexto = 0xFE0E;
    private const int SeletorDeVariacaoDeEmoji = 0xFE0F;

    /// <summary>Remove emojis, marcação do WhatsApp e caracteres invisíveis, depois colapsa os espaços.</summary>
    public static string Limpar(string texto)
    {
        StringBuilder construtor = new(texto.Length);

        foreach (Rune rune in texto.EnumerateRunes())
        {
            if (EhDecoracao(rune) is false)
            {
                construtor.Append(rune.ToString());
            }
        }

        return string.Join(' ', construtor.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>A chave usada para comparar nomes: sem acentos e em minúsculas, qualquer que seja a forma Unicode.</summary>
    public static string ChaveDeComparacao(string texto)
    {
        string decomposta = texto.Normalize(NormalizationForm.FormD);
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

    private static bool EhDecoracao(Rune rune)
    {
        if (rune.Value is '*' or '_' or '~' or SeletorDeVariacaoDeTexto or SeletorDeVariacaoDeEmoji)
        {
            return true;
        }

        return Rune.GetUnicodeCategory(rune) is UnicodeCategory.OtherSymbol
            or UnicodeCategory.ModifierSymbol
            or UnicodeCategory.Format
            or UnicodeCategory.EnclosingMark
            or UnicodeCategory.PrivateUse
            or UnicodeCategory.OtherNotAssigned;
    }
}
