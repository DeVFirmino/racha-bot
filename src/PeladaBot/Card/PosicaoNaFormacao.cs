namespace PeladaBot.Card;

/// <summary>Quem ocupa uma posição do time, e como: o nome já em maiúsculas, com a luva no goleiro.</summary>
internal sealed record PosicaoNaFormacao
{
    public required PosicaoNoCampo Posicao { get; init; }

    public required PapelNoCard Papel { get; init; }

    public required string Rotulo { get; init; }
}
