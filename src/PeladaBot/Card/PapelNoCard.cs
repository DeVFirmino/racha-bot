namespace PeladaBot.Card;

/// <summary>O que a camisa e a placa de uma posição mostram.</summary>
public enum PapelNoCard
{
    /// <summary>Jogador de linha: camisa na cor do colete.</summary>
    Linha,

    /// <summary>Goleiro fixo: camisa clara de goleiro e a luva na placa.</summary>
    GoleiroFixo,

    /// <summary>Goleiro de rodízio: camisa do time, a luva na placa e o selo "RODÍZIO" embaixo.</summary>
    GoleiroDeRodizio,

    /// <summary>Posição sem ninguém, no time incompleto: camisa tracejada e "VAGA" apagado.</summary>
    Vaga,
}
