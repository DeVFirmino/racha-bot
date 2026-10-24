using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace PeladaBot.Card;

/// <summary>
/// Cada jogador escalado: a camisa e a placa escura com o nome claro, dos dois lados do campo.
/// As sombras saem antes, na camada de sombras do card.
/// </summary>
internal static class DesenhistaDoJogador
{
    private const float RaioDaPlaca = 6f;
    private const float AlturaDaFaixaDaPlaca = 4f;
    private const float AlfaDaPlacaDaVaga = 0.4f;
    private const float AlfaDaFaixaDaVaga = 0.5f;
    private const float AlfaDoNomeDaVaga = 0.6f;
    private const float AlfaDaGola = 0.45f;
    private const float AlfaDoPunho = 0.55f;
    private const float AlfaDoBrilho = 0.25f;
    private const float AlfaDoFundoDaVaga = 0.05f;
    private const float AlfaDoContornoDaVaga = 0.6f;

    /// <summary>Traço de 9 e espaço de 7, em múltiplos da largura de 3,5 do contorno da vaga.</summary>
    private static readonly float[] _tracejadoDaVaga = [2.6f, 2f];

    public static void Desenhar(DrawingCanvas tela, IReadOnlyList<JogadorDoCard> jogadores)
    {
        foreach (JogadorDoCard jogador in jogadores)
        {
            DesenharCamisa(tela, jogador);
            DesenharPlaca(tela, jogador);
        }
    }

    /// <summary>As formas que fazem sombra: a camisa (menos a vaga) e a placa.</summary>
    public static IEnumerable<IPath> FormasComSombra(JogadorDoCard jogador)
    {
        if (jogador.Papel != PapelNoCard.Vaga)
        {
            yield return Molde(jogador);
        }

        yield return Formas.RetanguloArredondado(jogador.Placa, RaioDaPlaca);
    }

    /// <summary>
    /// A camisa na cor do colete, ou preta no goleiro fixo, com gola, bainhas e um brilho no peito. A vaga é só o
    /// contorno tracejado claro.
    /// </summary>
    private static void DesenharCamisa(DrawingCanvas tela, JogadorDoCard jogador)
    {
        float escala = jogador.Camisa.Width / Formas.LadoDoMolde;
        Matrix4x4 noLugar = NoLugar(jogador);
        IPath molde = Molde(jogador);

        if (jogador.Papel == PapelNoCard.Vaga)
        {
            tela.Fill(Brushes.Solid(Color.Black.WithAlpha(AlfaDoFundoDaVaga)), molde);
            tela.Draw(
                new PatternPen(PaletaDoCard.TextoClaro.WithAlpha(AlfaDoContornoDaVaga), 3.5f * escala, _tracejadoDaVaga),
                molde);

            return;
        }

        bool goleiroFixo = jogador.Papel == PapelNoCard.GoleiroFixo;
        Color cor = goleiroFixo ? PaletaDoCard.CamisaDeGoleiro : PaletaDoCard.Colete(jogador.Cor);
        Color gola = (goleiroFixo ? Color.White : PaletaDoCard.Sombra).WithAlpha(AlfaDaGola);

        tela.Fill(Brushes.Solid(cor), molde);
        tela.Draw(Pens.Solid(PaletaDoCard.ContornoDaCamisa, 2 * escala), molde);
        tela.Draw(Pens.Solid(gola, 4 * escala), Formas.Gola.Transform(noLugar));

        Pen punho = Pens.Solid(Color.White.WithAlpha(AlfaDoPunho), 4 * escala);

        foreach ((PointF de, PointF ate) in Formas.Punhos)
        {
            tela.DrawLine(punho, Vector2.Transform(de, noLugar), Vector2.Transform(ate, noLugar));
        }

        tela.Draw(Pens.Solid(Color.White.WithAlpha(AlfaDoBrilho), 3 * escala), Formas.Brilho.Transform(noLugar));
    }

    /// <summary>A placa com a faixa do colete na base, o nome por cima e, no rodízio, o selo embaixo.</summary>
    private static void DesenharPlaca(DrawingCanvas tela, JogadorDoCard jogador)
    {
        bool vaga = jogador.Papel == PapelNoCard.Vaga;
        RectangleF placa = jogador.Placa;
        RectangleF faixa = new(placa.Left, placa.Bottom - AlturaDaFaixaDaPlaca, placa.Width, AlturaDaFaixaDaPlaca);
        Color fundo = PaletaDoCard.Placa;
        Color nome = PaletaDoCard.TextoClaro;

        tela.Fill(Brushes.Solid(fundo.WithAlpha(vaga ? AlfaDaPlacaDaVaga : 1f)), Formas.RetanguloArredondado(placa, RaioDaPlaca));
        tela.Fill(
            Brushes.Solid(PaletaDoCard.Colete(jogador.Cor).WithAlpha(vaga ? AlfaDaFaixaDaVaga : 1f)),
            Formas.RetanguloArredondado(faixa, AlturaDaFaixaDaPlaca / 2));
        DesenhistaDoCard.Escrever(tela, jogador.Nome, nome.WithAlpha(vaga ? AlfaDoNomeDaVaga : 1f));

        if (jogador.Selo is not null)
        {
            DesenhistaDoCard.Escrever(tela, jogador.Selo, PaletaDoCard.TextoClaro);
        }
    }

    private static Matrix4x4 NoLugar(JogadorDoCard jogador) =>
        Matrix4x4.CreateScale(jogador.Camisa.Width / Formas.LadoDoMolde)
        * Matrix4x4.CreateTranslation(jogador.Camisa.X, jogador.Camisa.Y, 0);

    private static IPath Molde(JogadorDoCard jogador) => Formas.MoldeDaCamisa.Transform(NoLugar(jogador));
}
