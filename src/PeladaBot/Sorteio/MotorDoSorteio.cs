using PeladaBot.Listas;

namespace PeladaBot.Sorteio;

public static class MotorDoSorteio
{
    private const int MinimoDeJogadoresPagos = 4;
    private const int JogadoresDeLinhaPorTime = 5;
    private const int PessoasPorTimeCompleto = JogadoresDeLinhaPorTime + 1;
    private const int MinimoDeLinhaParaTimesCompletos = 2 * JogadoresDeLinhaPorTime;

    /// <summary>A primeira quadra fecha com 15 de linha: três times de 5, e quem passa disso espera a segunda.</summary>
    private const int MaximoDeLinhaNumaQuadra = 3 * JogadoresDeLinhaPorTime;

    /// <summary>Uma quadra tem dois gols: com dois fixed goalkeepers, os dois ficam no gol a noite toda.</summary>
    private const int GoleirosParaFecharOsDoisGols = 2;

    private static readonly CorDoTime[] _coresDeColete =
        [CorDoTime.Laranja, CorDoTime.Verde, CorDoTime.Cinza, CorDoTime.Azul];

    /// <summary>
    /// Sorteia os times a partir dos jogadores de linha pagos — da "Lista de jogadores" e da "Lista de Espera" — e dos
    /// goleiros da lista. Sem <paramref name="somentePagos"/>, o sorteio só sai quando todo mundo da "Lista de
    /// jogadores" tem ✅; com ele ("!sortear pagos"/"!times pagos"), quem está sem ✅ é ignorado. Quem está na
    /// espera sem ✅ não bloqueia nem entra: ainda espera uma vaga. Puro: a única fonte de acaso é
    /// <paramref name="random"/>.
    /// </summary>
    public static ResultadoDoSorteio Sortear(ListaDoGrupo lista, Random random, bool somentePagos = false)
    {
        ArgumentNullException.ThrowIfNull(lista);
        ArgumentNullException.ThrowIfNull(random);

        if (lista.Jogadores.Count == 0)
        {
            return new NinguemPagou();
        }

        string[] nomesPendentes =
            [.. lista.Jogadores.Where(jogador => jogador.Pago is false).Select(jogador => jogador.Nome)];

        if (somentePagos is false && nomesPendentes.Length > 0)
        {
            return new PagamentoPendente { NomesPendentes = nomesPendentes };
        }

        string[] jogadoresDeLinha =
            [.. lista.Jogadores.Concat(lista.Espera).Where(jogador => jogador.Pago).Select(jogador => jogador.Nome)];

        if (jogadoresDeLinha.Length == 0)
        {
            return new NinguemPagou();
        }

        if (jogadoresDeLinha.Length < MinimoDeJogadoresPagos)
        {
            return new PagosInsuficientes { QuantidadePaga = jogadoresDeLinha.Length };
        }

        string[] goleiros = [.. lista.Goleiros];
        CorDoTime[] cores = [.. _coresDeColete];

        random.Shuffle(jogadoresDeLinha);
        random.Shuffle(goleiros);
        random.Shuffle(cores);

        List<string> revezandoNoGol = [];
        List<Time> times = jogadoresDeLinha.Length switch
        {
            < MinimoDeLinhaParaTimesCompletos => ConstruirTimesDeDoisGrupos(jogadoresDeLinha, goleiros, revezandoNoGol),
            <= MaximoDeLinhaNumaQuadra when goleiros.Length >= GoleirosParaFecharOsDoisGols =>
                ConstruirTimesComOsGoleirosNoGol(jogadoresDeLinha, goleiros, revezandoNoGol),
            _ => ConstruirTimesCompletos(jogadoresDeLinha, goleiros, revezandoNoGol),
        };

        for (int indice = 0; indice < times.Count; indice++)
        {
            times[indice] = times[indice] with
            {
                Numero = indice + 1,
                Cor = indice < cores.Length ? cores[indice] : null,
            };
        }

        return new TimesSorteados
        {
            Times = times,
            IgnorandoPendentes = somentePagos ? nomesPendentes : [],
            RevezandoNoGol = revezandoNoGol,
        };
    }

    /// <summary>
    /// 4 a 9 de linha: dois times cujos tamanhos diferem em no máximo um. O time sem goleiro fixo sorteia o seu
    /// de rodízio entre os próprios jogadores de linha. Um goleiro fixo que não cabe no gol de nenhum dos dois
    /// completa a linha de um deles.
    /// </summary>
    private static List<Time> ConstruirTimesDeDoisGrupos(string[] jogadoresDeLinha, string[] goleiros, List<string> revezandoNoGol)
    {
        int timeMenor = jogadoresDeLinha.Length / 2;
        int[] tamanhos = [jogadoresDeLinha.Length - timeMenor, timeMenor];
        List<Time> times = [];
        int distribuidos = 0;

        for (int indice = 0; indice < tamanhos.Length; indice++)
        {
            string[] linhaDoTime = jogadoresDeLinha[distribuidos..(distribuidos + tamanhos[indice])];
            distribuidos += tamanhos[indice];

            times.Add(indice < goleiros.Length
                ? TimeCompletoComGoleiroFixo(goleiros[indice], linhaDoTime)
                : TimeCompletoComGoleiroDeRodizio(linhaDoTime));
        }

        string[] goleirosExtras = goleiros.Length > tamanhos.Length ? goleiros[tamanhos.Length..] : [];
        DistribuirQuemSobrou(times, [], goleirosExtras, osGoleirosFicamNoGol: false, revezandoNoGol);

        return times;
    }

    /// <summary>
    /// Uma quadra só (10 a 15 de linha) com ao menos dois fixed goalkeepers: os fixos ficam no gol e jogam com quem
    /// entrar, então todo time é de 5 na linha, sem rodízio. Os fixos vão para os primeiros times, um por time; o
    /// time que fica sem um joga com os do gol. Quem sobra forma um último time, incompleto.
    /// </summary>
    private static List<Time> ConstruirTimesComOsGoleirosNoGol(string[] jogadoresDeLinha, string[] goleiros, List<string> revezandoNoGol)
    {
        List<Time> times = [];
        int indiceDoGoleiro = 0;
        int completos = jogadoresDeLinha.Length / JogadoresDeLinhaPorTime;

        for (int indice = 0; indice < completos; indice++)
        {
            string[] linhaDoTime = jogadoresDeLinha[(indice * JogadoresDeLinhaPorTime)..((indice + 1) * JogadoresDeLinhaPorTime)];

            if (indiceDoGoleiro < goleiros.Length)
            {
                times.Add(TimeCompletoComGoleiroFixo(goleiros[indiceDoGoleiro], linhaDoTime));
                indiceDoGoleiro++;
            }
            else
            {
                times.Add(TimeCompletoComOsGoleirosDoGol(linhaDoTime));
            }
        }

        DistribuirQuemSobrou(
            times,
            jogadoresDeLinha[(completos * JogadoresDeLinhaPorTime)..],
            goleiros[indiceDoGoleiro..],
            osGoleirosFicamNoGol: true,
            revezandoNoGol);

        return times;
    }

    /// <summary>
    /// Duas quadras (mais de 15 de linha), ou menos de dois fixed goalkeepers: tantos times completos (1 goleiro + 5 de
    /// linha) quanto couberem, um por fixo disponível e depois de rodízio (o primeiro jogador de cada time de 6),
    /// sem reservas. Quem sobra — de linha, e o goleiro fixo que não coube em nenhum time — forma um último time,
    /// incompleto.
    /// </summary>
    private static List<Time> ConstruirTimesCompletos(string[] jogadoresDeLinha, string[] goleiros, List<string> revezandoNoGol)
    {
        List<Time> times = [];
        int indiceDoGoleiro = 0;
        int indiceDaLinha = 0;

        while (true)
        {
            bool sobraGoleiroFixo = indiceDoGoleiro < goleiros.Length;
            int linhaNecessaria = sobraGoleiroFixo ? JogadoresDeLinhaPorTime : PessoasPorTimeCompleto;

            if (jogadoresDeLinha.Length - indiceDaLinha < linhaNecessaria)
            {
                break;
            }

            if (sobraGoleiroFixo)
            {
                times.Add(TimeCompletoComGoleiroFixo(
                    goleiros[indiceDoGoleiro], jogadoresDeLinha[indiceDaLinha..(indiceDaLinha + JogadoresDeLinhaPorTime)]));
                indiceDoGoleiro++;
                indiceDaLinha += JogadoresDeLinhaPorTime;
            }
            else
            {
                times.Add(TimeCompletoComGoleiroDeRodizio(
                    jogadoresDeLinha[indiceDaLinha..(indiceDaLinha + PessoasPorTimeCompleto)]));
                indiceDaLinha += PessoasPorTimeCompleto;
            }
        }

        DistribuirQuemSobrou(
            times, jogadoresDeLinha[indiceDaLinha..], goleiros[indiceDoGoleiro..], osGoleirosFicamNoGol: false, revezandoNoGol);

        return times;
    }

    /// <summary>
    /// Quem sobrou: a linha restante vira o último time, incompleto, com o primeiro goleiro fixo que restar, se houver
    /// algum. Goleiro fixo nunca forma time sozinho: o que sobra além desse joga na linha do time com
    /// menos gente na linha, até 5; com todos os times fechados, fica revezando no gol. Com
    /// <paramref name="osGoleirosFicamNoGol"/>, o time incompleto precisa só de 5 na linha; sem, precisa também de
    /// um goleiro.
    /// </summary>
    private static void DistribuirQuemSobrou(
        List<Time> times,
        string[] linhaRestante,
        string[] goleirosRestantes,
        bool osGoleirosFicamNoGol,
        List<string> revezandoNoGol)
    {
        int indiceDoGoleiro = 0;

        if (linhaRestante.Length > 0)
        {
            string? goleiroDoIncompleto = goleirosRestantes.Length > 0 ? goleirosRestantes[0] : null;
            times.Add(TimeIncompleto(goleiroDoIncompleto, linhaRestante, osGoleirosFicamNoGol));
            indiceDoGoleiro = goleiroDoIncompleto is null ? 0 : 1;
        }

        for (; indiceDoGoleiro < goleirosRestantes.Length; indiceDoGoleiro++)
        {
            int indiceDoTime = TimeComMaisVagaNaLinha(times);

            if (indiceDoTime < 0)
            {
                revezandoNoGol.Add(goleirosRestantes[indiceDoGoleiro]);
                continue;
            }

            times[indiceDoTime] = ComMaisUmNaLinha(times[indiceDoTime], goleirosRestantes[indiceDoGoleiro]);
        }
    }

    /// <summary>O time com menos gente na linha, entre os que ainda têm vaga nela; o primeiro deles no empate. -1 sem vaga.</summary>
    private static int TimeComMaisVagaNaLinha(List<Time> times)
    {
        int escolhido = -1;

        for (int indice = 0; indice < times.Count; indice++)
        {
            int naLinha = times[indice].JogadoresDeLinha.Count;

            if (naLinha < JogadoresDeLinhaPorTime && (escolhido < 0 || naLinha < times[escolhido].JogadoresDeLinha.Count))
            {
                escolhido = indice;
            }
        }

        return escolhido;
    }

    private static Time ComMaisUmNaLinha(Time time, string jogador)
    {
        int faltam = time.Completo ? 0 : time.Faltam - 1;

        return time with
        {
            JogadoresDeLinha = [.. time.JogadoresDeLinha, jogador],
            Faltam = faltam,
            Completo = faltam == 0,
        };
    }

    private static Time TimeCompletoComGoleiroFixo(string goleiro, IReadOnlyList<string> jogadoresDeLinha) => new()
    {
        Numero = 0,
        Cor = null,
        Goleiro = goleiro,
        GoleiroDeRodizio = false,
        JogaComOsGoleirosDoGol = false,
        Completo = true,
        Faltam = 0,
        JogadoresDeLinha = jogadoresDeLinha,
    };

    /// <summary>O primeiro de <paramref name="pessoas"/> joga de goleiro nesse sorteio; os outros ficam na linha.</summary>
    private static Time TimeCompletoComGoleiroDeRodizio(string[] pessoas) => new()
    {
        Numero = 0,
        Cor = null,
        Goleiro = pessoas[0],
        GoleiroDeRodizio = true,
        JogaComOsGoleirosDoGol = false,
        Completo = true,
        Faltam = 0,
        JogadoresDeLinha = pessoas[1..],
    };

    /// <summary>Cinco na linha e nenhum goleiro próprio: quando entra, joga com os fixed goalkeepers do gol.</summary>
    private static Time TimeCompletoComOsGoleirosDoGol(IReadOnlyList<string> jogadoresDeLinha) => new()
    {
        Numero = 0,
        Cor = null,
        Goleiro = null,
        GoleiroDeRodizio = false,
        JogaComOsGoleirosDoGol = true,
        Completo = true,
        Faltam = 0,
        JogadoresDeLinha = jogadoresDeLinha,
    };

    private static Time TimeIncompleto(string? goleiroFixo, IReadOnlyList<string> jogadoresDeLinha, bool osGoleirosFicamNoGol) => new()
    {
        Numero = 0,
        Cor = null,
        Goleiro = goleiroFixo,
        GoleiroDeRodizio = false,
        JogaComOsGoleirosDoGol = osGoleirosFicamNoGol && goleiroFixo is null,
        Completo = false,
        Faltam = osGoleirosFicamNoGol
            ? JogadoresDeLinhaPorTime - jogadoresDeLinha.Count
            : PessoasPorTimeCompleto - (goleiroFixo is null ? 0 : 1) - jogadoresDeLinha.Count,
        JogadoresDeLinha = jogadoresDeLinha,
    };
}
