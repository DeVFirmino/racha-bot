namespace PeladaBot.Comandos;

/// <summary>O que o <see cref="LeitorDeComando"/> achou na primeira(s) palavra(s) de uma mensagem.</summary>
public sealed record ComandoLido
{
    public required TipoDeComando Tipo { get; init; }

    /// <summary>
    /// True quando a segunda palavra é "pagos" ("!sortear pagos"/"!times pagos"); só vale para <see cref="TipoDeComando.Sortear"/>.
    /// </summary>
    public required bool SomentePagos { get; init; }
}
