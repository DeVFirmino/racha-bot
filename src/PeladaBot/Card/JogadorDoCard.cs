using PeladaBot.Sorteio;
using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>Uma posição desenhada no campo: a camisa e, logo abaixo, a placa com o nome.</summary>
public sealed record JogadorDoCard
{
    public required PapelNoCard Papel { get; init; }

    /// <summary>A cor do colete do time; null a partir do quinto time, que não tem colete.</summary>
    public required CorDoTime? Cor { get; init; }

    /// <summary>True na metade esquerda do campo.</summary>
    public required bool NoLadoBranco { get; init; }

    /// <summary>O quadrado onde a camisa é desenhada.</summary>
    public required RectangleF Camisa { get; init; }

    /// <summary>A placa com uma faixa na cor do colete na base, atrás de <see cref="Nome"/>.</summary>
    public required RectangleF Placa { get; init; }

    /// <summary>O nome em maiúsculas, "🧤 NOME" para o goleiro e "VAGA" para a posição vazia.</summary>
    public required TextoDoCard Nome { get; init; }

    /// <summary>"RODÍZIO" sob a placa do goleiro de rodízio; null para os demais.</summary>
    public required TextoDoCard? Selo { get; init; }
}
