namespace PeladaBot.Listas;

/// <summary>As linhas de cabeçalho "Data:", "Horário:" e "Local:", já sem espaços nas bordas. Uma linha ausente fica null.</summary>
public sealed record InfoDoJogo
{
    public string? Data { get; init; }

    public string? Horario { get; init; }

    public string? Local { get; init; }
}
