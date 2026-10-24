using System.Globalization;
using PeladaBot.Sorteio;

namespace PeladaBot.Card;

/// <summary>
/// Escala um time num 2-1-2. O time incompleto ocupa as seis posições, e quem falta vira vaga, do ataque para trás;
/// um time completo com menos de 5 de linha (sorteio de 4 a 9 pagos) ganha uma formação menor, sem vagas.
/// </summary>
internal static class FormacaoDoTime
{
    private const string Luva = "🧤";
    private const string Vaga = "VAGA";

    private static readonly CultureInfo _portugues = CultureInfo.GetCultureInfo("pt-BR");

    private static readonly PosicaoNoCampo[] _linhaCompleta =
    [
        PosicaoNoCampo.DefensorEsquerdo,
        PosicaoNoCampo.DefensorDireito,
        PosicaoNoCampo.Meio,
        PosicaoNoCampo.AtacanteEsquerdo,
        PosicaoNoCampo.AtacanteDireito,
    ];

    public static IReadOnlyList<PosicaoNaFormacao> Escalar(Time time)
    {
        // Quem joga com os fixed goalkeepers do gol não tem goleiro próprio para desenhar.
        List<PosicaoNaFormacao> posicoes = time.JogaComOsGoleirosDoGol ? [] : [Goleiro(time)];
        PosicaoNoCampo[] linha = time.Completo ? LinhaDeTimeCompleto(time.JogadoresDeLinha.Count) : _linhaCompleta;

        for (int indice = 0; indice < linha.Length; indice++)
        {
            posicoes.Add(indice < time.JogadoresDeLinha.Count
                ? new PosicaoNaFormacao { Posicao = linha[indice], Papel = PapelNoCard.Linha, Rotulo = Maiusculas(time.JogadoresDeLinha[indice]) }
                : new PosicaoNaFormacao { Posicao = linha[indice], Papel = PapelNoCard.Vaga, Rotulo = Vaga });
        }

        return posicoes;
    }

    private static PosicaoNaFormacao Goleiro(Time time)
    {
        if (time.Goleiro is null)
        {
            return new PosicaoNaFormacao { Posicao = PosicaoNoCampo.Goleiro, Papel = PapelNoCard.Vaga, Rotulo = Vaga };
        }

        return new PosicaoNaFormacao
        {
            Posicao = PosicaoNoCampo.Goleiro,
            Papel = time.GoleiroDeRodizio ? PapelNoCard.GoleiroDeRodizio : PapelNoCard.GoleiroFixo,
            Rotulo = $"{Luva} {Maiusculas(time.Goleiro)}",
        };
    }

    /// <summary>Quantos de linha o time tem decide a formação: 2-1-2, 2-2, 2-1, 2 ou 1 no meio.</summary>
    private static PosicaoNoCampo[] LinhaDeTimeCompleto(int jogadoresDeLinha) => jogadoresDeLinha switch
    {
        >= 5 => _linhaCompleta,
        4 => [PosicaoNoCampo.DefensorEsquerdo, PosicaoNoCampo.DefensorDireito, PosicaoNoCampo.AtacanteEsquerdo, PosicaoNoCampo.AtacanteDireito],
        3 => [PosicaoNoCampo.DefensorEsquerdo, PosicaoNoCampo.DefensorDireito, PosicaoNoCampo.Meio],
        2 => [PosicaoNoCampo.DefensorEsquerdo, PosicaoNoCampo.DefensorDireito],
        1 => [PosicaoNoCampo.Meio],
        _ => [],
    };

    private static string Maiusculas(string nome) => nome.Trim().ToUpper(_portugues);
}
