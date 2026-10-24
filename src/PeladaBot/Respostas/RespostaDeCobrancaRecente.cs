using System.Globalization;

namespace PeladaBot.Respostas;

/// <summary>A resenha para quem tenta !cobrar logo depois de uma cobrança no grupo, fora da presidência.</summary>
public static class RespostaDeCobrancaRecente
{
    private const string MarcaDoTempo = "{tempo}";

    /// <summary>As resenhas de espera; "{tempo}" vira quanto falta, como "18 minutos".</summary>
    public static IReadOnlyList<string> Resenhas { get; } =
    [
        """
        ⏳ *Calma, fiscal da diretoria!* 😂

        A cobrança acabou de sair: quem deve já foi avisado. A próxima libera em {tempo}.

        💶 *Enquanto isso, confere se o seu ✅ tá na lista!*
        """,
        """
        🚔 *Opa, delegado da cobrança!* 😂

        Os devedores já foram intimados agorinha. Nova intimação liberada em {tempo}.

        🍻 *Guarda essa energia pro jogo, fiscal!*
        """,
        """
        📺 *O VAR já revisou a cobrança!* 😂

        A lista de devedores acabou de sair. A próxima revisão abre em {tempo}.

        ⚽ *Enquanto isso, vai aquecendo que o jogo é logo ali!*
        """,
        """
        💶 *Calma, ministro da economia!* 😂

        O financeiro já passou a régua há pouco. Nova cobrança só daqui a {tempo}.

        ✅ *Já pagou? Então relaxa e confere o seu ✅ na lista!*
        """,
    ];

    /// <summary>
    /// Uma das resenhas, escolhida com <paramref name="random"/>, com quanto falta em minutos inteiros arredondados
    /// para cima, nunca menos de 1.
    /// </summary>
    public static string Compor(TimeSpan tempoAteAProximaCobranca, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        int minutos = Math.Max(1, (int)Math.Ceiling(tempoAteAProximaCobranca.TotalMinutes));
        string tempo = minutos == 1 ? "1 minuto" : $"{minutos.ToString(CultureInfo.InvariantCulture)} minutos";

        return Resenhas[random.Next(Resenhas.Count)].Replace(MarcaDoTempo, tempo, StringComparison.Ordinal);
    }
}
