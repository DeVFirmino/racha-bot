using System.Globalization;
using System.Text;

namespace PeladaBot.Textos;

/// <summary>Chaves de comparação sem acentos e em minúsculas, independentes da forma Unicode do texto.</summary>
internal static class ComparacaoDeTexto
{
    public static string CriarChave(string texto)
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
}
