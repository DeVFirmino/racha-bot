namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>Uma chamada que o bot fez ao grupo do WhatsApp, com a hora do relógio em que aconteceu.</summary>
public sealed record ChamadaDoMensageiro
{
    public required TipoDeChamadaDoMensageiro Tipo { get; init; }

    public required string IdDoChat { get; init; }

    /// <summary>O texto de uma mensagem de texto, ou a legenda de uma imagem. Null para digitação.</summary>
    public required string? Texto { get; init; }

    /// <summary>O PNG de uma imagem. Null nos demais casos.</summary>
    public required byte[]? Png { get; init; }

    /// <summary>A mensagem que a resposta cita. Null para digitação.</summary>
    public required string? IdDaMensagemRespondida { get; init; }

    public required DateTimeOffset Em { get; init; }
}
