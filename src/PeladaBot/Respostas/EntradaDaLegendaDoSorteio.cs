using PeladaBot.Listas;
using PeladaBot.Sorteio;

namespace PeladaBot.Respostas;

public sealed record EntradaDaLegendaDoSorteio
{
    public required TimesSorteados Sorteado { get; init; }

    /// <summary>A lista de onde os times saíram: avisos, quem ainda não pagou e a waiting list.</summary>
    public required ListaDoGrupo Lista { get; init; }

    /// <summary>Null quando o comando citou a lista: a legenda então não tem a linha "📋".</summary>
    public required OrigemDaLista? Origem { get; init; }

    /// <summary>Quando o sorteio aconteceu. O cabeçalho mostra no <see cref="Fuso"/>.</summary>
    public required DateTimeOffset SorteadoEm { get; init; }

    /// <summary>O fuso da hora na legenda. Os testes que não escolhem usam <see cref="HorarioDoGrupo.FusoDaSuite"/>.</summary>
    public TimeZoneInfo Fuso { get; init; } = HorarioDoGrupo.FusoDaSuite;

    /// <summary>
    /// Os temas da legenda. Um, escolhido ao acaso, dá o cabeçalho e a linha do horário; os apelidos de todos eles
    /// entram no sorteio do apelido de cada time.
    /// </summary>
    public required IReadOnlyList<ModeloDaLegenda> Modelos { get; init; }

    /// <summary>Apelidos fora dos temas, que entram no sorteio do apelido junto com os dos temas.</summary>
    public ApelidosAvulsos ApelidosAvulsos { get; init; } = ApelidosAvulsos.Nenhum;

    /// <summary>
    /// As frases do rodapé da configuração, já com a marcação do WhatsApp; cada uma pode ter mais de uma linha. Uma,
    /// escolhida ao acaso, vai como está antes da despedida.
    /// </summary>
    public required IReadOnlyList<string> FrasesDoRodape { get; init; }
}
