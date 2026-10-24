using PeladaBot.Sorteio;

namespace PeladaBot.Respostas;

/// <summary>O texto sob o card dos times. Vai sozinho quando a imagem falha.</summary>
public static class LegendaDoSorteio
{
    private const string Separador = "━━━━━━━━━━━━━━━━━━";
    private const string SublinhadoDaEquipe = "───────────────";
    private const string MarcaDaHora = "{hora}";
    private const string MarcaDoRelogio = "{relogio}";

    /// <summary>Um relógio por hora, da meia-noite às 11, para o cabeçalho mostrar a hora do sorteio também no emoji.</summary>
    private static readonly string[] _relogios = ["🕛", "🕐", "🕑", "🕒", "🕓", "🕔", "🕕", "🕖", "🕗", "🕘", "🕙", "🕚"];

    /// <summary>
    /// Escreve a legenda com um dos temas, escolhido ao acaso: o cabeçalho e a linha do horário do tema, os avisos
    /// sobre a lista, cada equipe com um apelido da sua cor sorteado entre os de todos os temas e os avulsos
    /// , e, depois do separador, uma das frases do
    /// rodapé, também ao acaso e escrita como está na configuração. Quem está na espera sem ✅ não aparece: ainda
    /// espera uma vaga, não ficou de fora do sorteio. Pura: a única fonte de acaso é <paramref name="random"/>.
    /// </summary>
    public static string Compor(EntradaDaLegendaDoSorteio entrada, Random random)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        ArgumentNullException.ThrowIfNull(random);

        if (entrada.Modelos.Count == 0)
        {
            throw new ArgumentException("Uma legenda sempre usa um tema, então é preciso ao menos um.", nameof(entrada));
        }

        if (entrada.FrasesDoRodape.Count == 0)
        {
            throw new ArgumentException(
                "Um sorteio sempre termina com uma frase do rodapé, então é preciso ao menos uma.",
                nameof(entrada));
        }

        ModeloDaLegenda modelo = entrada.Modelos[random.Next(entrada.Modelos.Count)];
        string frase = entrada.FrasesDoRodape[random.Next(entrada.FrasesDoRodape.Count)].Trim();
        IReadOnlyList<Time> times = entrada.Sorteado.Times;
        List<string> linhas =
        [
            modelo.Cabecalho.Trim(),
            LinhaDoHorario(modelo, entrada.SorteadoEm, entrada.Fuso),
            .. LinhasDeResposta.Avisos(entrada.Lista, entrada.Origem, entrada.Fuso),
            .. LinhaDeIgnorados(entrada.Sorteado.IgnorandoPendentes),
            Separador,
        ];

        for (int indice = 0; indice < times.Count; indice++)
        {
            if (indice > 0)
            {
                linhas.Add(string.Empty);
            }

            linhas.AddRange(Equipe(times[indice], SortearApelido(times[indice], entrada, random)));
        }

        if (entrada.Sorteado.RevezandoNoGol.Count > 0)
        {
            linhas.AddRange([string.Empty, LinhaDeQuemRevezaNoGol(entrada.Sorteado.RevezandoNoGol)]);
        }

        linhas.AddRange([Separador, frase]);

        return LinhasDeResposta.Juntar(linhas);
    }

    /// <summary>Goleiros fixos que sobraram com todos os times fechados: "🧤 *Revezando no gol:* A, B".</summary>
    private static string LinhaDeQuemRevezaNoGol(IReadOnlyList<string> goleiros) =>
        $"🧤 *Revezando no gol:* {string.Join(", ", goleiros.Select(MarcacaoDoWhatsApp.Neutralizar))}";

    /// <summary>A linha do tema com a hora do sorteio no fuso do grupo e, onde o tema pede, o relógio dessa hora (16:42 → 🕓).</summary>
    private static string LinhaDoHorario(ModeloDaLegenda modelo, DateTimeOffset sorteadoEm, TimeZoneInfo fuso) =>
        modelo.LinhaDoHorario.Trim()
            .Replace(MarcaDoRelogio, _relogios[HorarioDoGrupo.Hora(sorteadoEm, fuso) % 12], StringComparison.Ordinal)
            .Replace(MarcaDaHora, HorarioDoGrupo.HoraEMinuto(sorteadoEm, fuso), StringComparison.Ordinal);

    /// <summary>
    /// "⚪ *CINZA | OS GALÁCTICOS DO BAIRRO*", com "(faltam N)" quando o time está incompleto, sublinhado; depois o
    /// goleiro com a luva e um jogador de linha por linha, cada um com a camisa.
    /// </summary>
    private static IReadOnlyList<string> Equipe(Time time, string apelido)
    {
        string nome = NomeDaEquipe(time, apelido);
        string cabecalho = time.Completo ? nome : $"{nome} (faltam {time.Faltam})";
        IEnumerable<string> jogadoresDeLinha = time.JogadoresDeLinha.Select(jogador => $"👕 {MarcacaoDoWhatsApp.Neutralizar(jogador)}");

        if (time.Goleiro is null)
        {
            return [cabecalho, SublinhadoDaEquipe, .. jogadoresDeLinha];
        }

        return [cabecalho, SublinhadoDaEquipe, LinhaDoGoleiro(time), .. jogadoresDeLinha];
    }

    /// <summary>A cor do colete e o apelido sorteado para o time; "SEM COLETE" quando as cores acabaram.</summary>
    private static string NomeDaEquipe(Time time, string apelido) => time.Cor switch
    {
        CorDoTime.Laranja => $"🟠 *LARANJA | {apelido}*",
        CorDoTime.Verde => $"🟢 *VERDE | {apelido}*",
        CorDoTime.Cinza => $"⚪ *CINZA | {apelido}*",
        CorDoTime.Azul => $"🔵 *AZUL | {apelido}*",
        null => $"⚫ *SEM COLETE | {apelido}*",
        _ => throw new ArgumentOutOfRangeException(nameof(time), time.Cor, "Uma cor de colete sem nome."),
    };

    /// <summary>Um apelido da cor do time, ao acaso entre os que todos os temas dão a ela e os avulsos dela.</summary>
    private static string SortearApelido(Time time, EntradaDaLegendaDoSorteio entrada, Random random)
    {
        string[] apelidos = time.Cor switch
        {
            CorDoTime.Laranja => [.. entrada.Modelos.Select(modelo => modelo.ApelidoLaranja), .. entrada.ApelidosAvulsos.Laranja],
            CorDoTime.Verde => [.. entrada.Modelos.Select(modelo => modelo.ApelidoVerde), .. entrada.ApelidosAvulsos.Verde],
            CorDoTime.Cinza => [.. entrada.Modelos.Select(modelo => modelo.ApelidoCinza), .. entrada.ApelidosAvulsos.Cinza],
            CorDoTime.Azul => [.. entrada.Modelos.Select(modelo => modelo.ApelidoAzul), .. entrada.ApelidosAvulsos.Azul],
            null => [.. entrada.Modelos.Select(modelo => modelo.ApelidoSemColete), .. entrada.ApelidosAvulsos.SemColete],
            _ => throw new ArgumentOutOfRangeException(nameof(time), time.Cor, "Uma cor de colete sem nome."),
        };

        return apelidos[random.Next(apelidos.Length)].Trim();
    }

    /// <summary>"🧤 Nome" para um goleiro fixo; "🧤 Nome (rodízio)" para o goleiro do time nesse sorteio.</summary>
    private static string LinhaDoGoleiro(Time time)
    {
        string nome = MarcacaoDoWhatsApp.Neutralizar(time.Goleiro!);

        return time.GoleiroDeRodizio ? $"🧤 {nome} (rodízio)" : $"🧤 {nome}";
    }

    /// <summary>"⚠️ Sorteado sem: A, B (sem ✅)". Vazio no sorteio normal, que nunca ignora pendente.</summary>
    private static IReadOnlyList<string> LinhaDeIgnorados(IReadOnlyList<string> ignorados) =>
        ignorados.Count == 0 ? [] : [$"⚠️ Sorteado sem: {LinhasDeResposta.Nomes(ignorados)} (sem ✅)"];
}
