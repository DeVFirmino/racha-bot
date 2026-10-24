namespace PeladaBot.Listas;

/// <summary>
/// O mesmo nome aparece em duas seções. Ele é mantido só na de maior prioridade: Goleiros, depois Jogadores,
/// depois Espera.
/// </summary>
public sealed record NomeEmDuasSecoes : AvisoDaLista
{
    public required SecaoDaLista MantidoEm { get; init; }
}
