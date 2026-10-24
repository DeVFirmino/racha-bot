namespace PeladaBot.Respostas;

/// <summary>
/// Um tema da legenda do sorteio: o cabeçalho, a linha do horário (com "{hora}" e, se quiser, "{relogio}") e o
/// apelido de cada cor de colete, com mais um para o time sem colete.
/// </summary>
public sealed record ModeloDaLegenda
{
    /// <summary>A primeira linha, como "⚽ *PELADA | DIA DE JOGO*".</summary>
    public required string Cabecalho { get; init; }

    /// <summary>A segunda linha, como "{relogio} *Sorteio das {hora}*".</summary>
    public required string LinhaDoHorario { get; init; }

    public required string ApelidoLaranja { get; init; }

    public required string ApelidoVerde { get; init; }

    public required string ApelidoCinza { get; init; }

    public required string ApelidoAzul { get; init; }

    /// <summary>O apelido do time sem colete, do 5º em diante.</summary>
    public required string ApelidoSemColete { get; init; }
}
