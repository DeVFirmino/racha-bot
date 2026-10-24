namespace PeladaBot.Sorteio;

public sealed record Time
{
    /// <summary>Posição no sorteio, a partir de 1. Um time sem cor de colete é chamado por ela: "Time 5".</summary>
    public required int Numero { get; init; }

    /// <summary>A cor do colete. Null a partir do quinto time, porque só há quatro cores.</summary>
    public required CorDoTime? Cor { get; init; }

    /// <summary>
    /// Null quando o time não tem goleiro próprio: numa quadra só, ele joga com os fixed goalkeepers do gol
    /// (<see cref="JogaComOsGoleirosDoGol"/>); ou é o time incompleto e não sobrou goleiro fixo para ele.
    /// </summary>
    public required string? Goleiro { get; init; }

    /// <summary>
    /// True quando <see cref="Goleiro"/> não é um goleiro fixo: foi tirado dos próprios jogadores de linha do
    /// time, e muda a cada sorteio. A legenda mostra "(rodízio)" nesse caso.
    /// </summary>
    public required bool GoleiroDeRodizio { get; init; }

    /// <summary>
    /// True quando o time não tem goleiro próprio porque, numa quadra só, os fixed goalkeepers ficam no gol e jogam
    /// com quem entrar. O time é de 5 na linha, sem rodízio.
    /// </summary>
    public required bool JogaComOsGoleirosDoGol { get; init; }

    /// <summary>
    /// False só no time extra formado com quem sobrou no fim do sorteio. A legenda mostra "(faltam N)" no seu
    /// cabeçalho, com N em <see cref="Faltam"/>.
    /// </summary>
    public required bool Completo { get; init; }

    /// <summary>Quantas pessoas faltam para o time ficar completo; 0 no time completo.</summary>
    public required int Faltam { get; init; }

    public required IReadOnlyList<string> JogadoresDeLinha { get; init; }
}
