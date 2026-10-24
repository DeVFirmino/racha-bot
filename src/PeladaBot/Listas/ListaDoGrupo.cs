namespace PeladaBot.Listas;

public sealed record ListaDoGrupo
{
    public required IReadOnlyList<string> Goleiros { get; init; }

    public required IReadOnlyList<JogadorListado> Jogadores { get; init; }

    /// <summary>
    /// Quem espera uma vaga. Só joga quem já pagou (✅): pagou porque alguém desistiu ou porque fechou a segunda quadra.
    /// </summary>
    public required IReadOnlyList<JogadorListado> Espera { get; init; }

    public required InfoDoJogo Jogo { get; init; }

    public required IReadOnlyList<AvisoDaLista> Avisos { get; init; }
}
