namespace PeladaBot.Configuracao;

/// <summary>Um tema da legenda na configuração; ver <see cref="Respostas.ModeloDaLegenda"/>.</summary>
public sealed class OpcoesDoModeloDaLegenda
{
    public string Cabecalho { get; init; } = string.Empty;

    /// <summary>Tem de ter "{hora}"; "{relogio}" vira o relógio da hora do sorteio.</summary>
    public string LinhaDoHorario { get; init; } = string.Empty;

    public string ApelidoLaranja { get; init; } = string.Empty;

    public string ApelidoVerde { get; init; } = string.Empty;

    public string ApelidoCinza { get; init; } = string.Empty;

    public string ApelidoAzul { get; init; } = string.Empty;

    public string ApelidoSemColete { get; init; } = string.Empty;
}
