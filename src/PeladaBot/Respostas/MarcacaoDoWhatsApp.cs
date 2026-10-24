namespace PeladaBot.Respostas;

/// <summary>
/// O WhatsApp deixa *texto* em negrito, _texto_ em itálico e ~texto~ riscado. Um nome nunca deve mudar de
/// estilo, então em nomes cada marcador se torna um caractere parecido que nenhum cliente WhatsApp lê como marcação.
/// </summary>
internal static class MarcacaoDoWhatsApp
{
    /// <summary>∗ ASTERISK OPERATOR, no lugar de *.</summary>
    private const char AsteriscoSemelhante = '∗';

    /// <summary>ˍ MODIFIER LETTER LOW MACRON, no lugar de _.</summary>
    private const char SublinhadoSemelhante = 'ˍ';

    /// <summary>∼ TILDE OPERATOR, no lugar de ~.</summary>
    private const char TildeSemelhante = '∼';

    public static string Neutralizar(string nome) => nome
        .Replace('*', AsteriscoSemelhante)
        .Replace('_', SublinhadoSemelhante)
        .Replace('~', TildeSemelhante);
}
