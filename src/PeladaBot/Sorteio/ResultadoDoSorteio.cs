namespace PeladaBot.Sorteio;

/// <summary>
/// Como um sorteio termina: ninguém na lista, alguém sem pagar, poucos pagos demais, ou os times. Um resultado de
/// negócio, nunca uma exceção.
/// </summary>
public abstract record ResultadoDoSorteio;
