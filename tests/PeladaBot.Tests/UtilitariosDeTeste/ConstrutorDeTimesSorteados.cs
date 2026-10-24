using PeladaBot.Sorteio;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>Constrói um resultado de sorteio time por time, com nomes exatos, para um teste que precisa deles em vez de um sorteio embaralhado.</summary>
public sealed class ConstrutorDeTimesSorteados
{
    private const int TamanhoDoTimeCompleto = 5;

    private static readonly CorDoTime[] _coresDeColete = [CorDoTime.Laranja, CorDoTime.Verde, CorDoTime.Cinza, CorDoTime.Azul];

    private readonly List<Time> _times = [];
    private readonly List<string> _ignorandoPendentes = [];

    /// <summary>
    /// Adiciona o próximo time, completo, com um goleiro fixo. Um <paramref name="goleiro"/> null deixa "(sem
    /// goleiro)"; uma <paramref name="cor"/> null faz "Time N".
    /// </summary>
    public ConstrutorDeTimesSorteados ComTime(CorDoTime? cor, string? goleiro, params string[] jogadoresDeLinha) =>
        Adicionar(cor, goleiro, goleiroDeRodizio: false, completo: true, jogadoresDeLinha);

    /// <summary>Adiciona o próximo time, completo, cujo goleiro é de rodízio (tirado dos próprios jogadores de linha).</summary>
    public ConstrutorDeTimesSorteados ComTimeDeRodizio(CorDoTime? cor, string goleiro, params string[] jogadoresDeLinha) =>
        Adicionar(cor, goleiro, goleiroDeRodizio: true, completo: true, jogadoresDeLinha);

    /// <summary>Adiciona o time extra, incompleto, formado com quem sobrou no fim do sorteio (time de 6, com goleiro).</summary>
    public ConstrutorDeTimesSorteados ComTimeIncompleto(CorDoTime? cor, string? goleiroFixo, params string[] jogadoresDeLinha) =>
        Adicionar(cor, goleiroFixo, goleiroDeRodizio: false, completo: false, jogadoresDeLinha);

    /// <summary>Adiciona um time de 5 na linha sem goleiro próprio: numa quadra só, joga com os fixed goalkeepers do gol.</summary>
    public ConstrutorDeTimesSorteados ComTimeComOsGoleirosDoGol(CorDoTime? cor, params string[] jogadoresDeLinha) =>
        Adicionar(cor, null, goleiroDeRodizio: false, completo: true, jogadoresDeLinha, comOsGoleirosDoGol: true);

    /// <summary>Adiciona o time incompleto de uma quadra só: precisa só de 5 na linha, os fixed goalkeepers ficam no gol.</summary>
    public ConstrutorDeTimesSorteados ComTimeIncompletoComOsGoleirosDoGol(CorDoTime? cor, params string[] jogadoresDeLinha) =>
        Adicionar(cor, null, goleiroDeRodizio: false, completo: false, jogadoresDeLinha, comOsGoleirosDoGol: true);

    /// <summary>Adiciona times de um goleiro fixo e cinco jogadores numerados ("Goleiro 1", "Jogador 1" a "Jogador 5"...), nas cores Laranja, Verde, Cinza e Azul, depois nenhuma.</summary>
    public ConstrutorDeTimesSorteados ComTimesCompletos(int quantidade)
    {
        for (int indice = 0; indice < quantidade; indice++)
        {
            int numero = _times.Count + 1;
            CorDoTime? cor = numero <= _coresDeColete.Length ? _coresDeColete[numero - 1] : null;
            string[] jogadores =
                [.. Enumerable.Range(((numero - 1) * TamanhoDoTimeCompleto) + 1, TamanhoDoTimeCompleto).Select(jogador => $"Jogador {jogador}")];

            ComTime(cor, $"Goleiro {numero}", jogadores);
        }

        return this;
    }

    /// <summary>Quem foi sorteado sem ✅, por "!sortear pagos"/"!times pagos".</summary>
    public ConstrutorDeTimesSorteados ComIgnorandoPendentes(params string[] nomes)
    {
        _ignorandoPendentes.AddRange(nomes);

        return this;
    }

    public TimesSorteados Build() => new() { Times = [.. _times], IgnorandoPendentes = [.. _ignorandoPendentes] };

    private ConstrutorDeTimesSorteados Adicionar(
        CorDoTime? cor, string? goleiro, bool goleiroDeRodizio, bool completo, string[] jogadoresDeLinha, bool comOsGoleirosDoGol = false)
    {
        int faltam = completo ? 0 : comOsGoleirosDoGol ? 5 - jogadoresDeLinha.Length : 6 - (goleiro is null ? 0 : 1) - jogadoresDeLinha.Length;

        _times.Add(new Time
        {
            Numero = _times.Count + 1,
            Cor = cor,
            Goleiro = goleiro,
            GoleiroDeRodizio = goleiroDeRodizio,
            JogaComOsGoleirosDoGol = comOsGoleirosDoGol,
            Completo = completo,
            Faltam = faltam,
            JogadoresDeLinha = [.. jogadoresDeLinha],
        });

        return this;
    }
}
