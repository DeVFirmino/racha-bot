using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace PeladaBot.Card;

/// <summary>Os contornos que o card repete: retângulos arredondados e o molde da camisa.</summary>
internal static class Formas
{
    /// <summary>O lado da caixa em que o molde da camisa é desenhado.</summary>
    public const float LadoDoMolde = 100f;

    /// <summary>
    /// Distância do ponto de controle, como fração do raio, de uma Bézier cúbica que desenha um quarto de círculo.
    /// </summary>
    private const float KappaDoQuartoDeCirculo = 0.5523f;

    /// <summary>Uma camisa de manga curta, de frente, numa caixa de 100×100.</summary>
    public static IPath MoldeDaCamisa { get; } = ConstruirMoldeDaCamisa();

    /// <summary>A gola em V.</summary>
    public static IPath Gola { get; } = Curva(new PointF(43, 4), new PointF(50, 16), new PointF(57, 4));

    /// <summary>O brilho no peito, do lado de onde vem a luz.</summary>
    public static IPath Brilho { get; } = Curva(new PointF(32, 30), new PointF(40, 22), new PointF(46, 30));

    /// <summary>As bainhas das duas mangas.</summary>
    public static IReadOnlyList<(PointF De, PointF Ate)> Punhos { get; } =
    [
        (new PointF(84, 43), new PointF(93, 25)),
        (new PointF(16, 43), new PointF(7, 25)),
    ];

    public static IPath RetanguloArredondado(RectangleF limites, float raio)
    {
        float r = Math.Min(raio, Math.Min(limites.Width, limites.Height) / 2);
        float controle = r * (1 - KappaDoQuartoDeCirculo);
        PathBuilder construtor = new();

        construtor.AddLine(new PointF(limites.Left + r, limites.Top), new PointF(limites.Right - r, limites.Top));
        construtor.AddCubicBezier(
            new PointF(limites.Right - r, limites.Top),
            new PointF(limites.Right - controle, limites.Top),
            new PointF(limites.Right, limites.Top + controle),
            new PointF(limites.Right, limites.Top + r));
        construtor.AddLine(new PointF(limites.Right, limites.Top + r), new PointF(limites.Right, limites.Bottom - r));
        construtor.AddCubicBezier(
            new PointF(limites.Right, limites.Bottom - r),
            new PointF(limites.Right, limites.Bottom - controle),
            new PointF(limites.Right - controle, limites.Bottom),
            new PointF(limites.Right - r, limites.Bottom));
        construtor.AddLine(new PointF(limites.Right - r, limites.Bottom), new PointF(limites.Left + r, limites.Bottom));
        construtor.AddCubicBezier(
            new PointF(limites.Left + r, limites.Bottom),
            new PointF(limites.Left + controle, limites.Bottom),
            new PointF(limites.Left, limites.Bottom - controle),
            new PointF(limites.Left, limites.Bottom - r));
        construtor.AddLine(new PointF(limites.Left, limites.Bottom - r), new PointF(limites.Left, limites.Top + r));
        construtor.AddCubicBezier(
            new PointF(limites.Left, limites.Top + r),
            new PointF(limites.Left, limites.Top + controle),
            new PointF(limites.Left + controle, limites.Top),
            new PointF(limites.Left + r, limites.Top));
        construtor.CloseFigure();

        return construtor.Build();
    }

    /// <summary>
    /// O colete no placar, no formato das bandeiras do GE: cantos bem arredondados no alto à esquerda e embaixo à
    /// direita, quase retos nos outros dois.
    /// </summary>
    public static IPath Colete(RectangleF limites)
    {
        float grande = limites.Height * 0.28f;
        const float Pequeno = 8f;
        float x = limites.X;
        float y = limites.Y;
        float w = limites.Width;
        float h = limites.Height;
        PathBuilder colete = new();

        colete.AddLine(new PointF(x + grande, y), new PointF(x + w - Pequeno, y));
        colete.AddQuadraticBezier(new PointF(x + w - Pequeno, y), new PointF(x + w, y), new PointF(x + w, y + Pequeno));
        colete.AddLine(new PointF(x + w, y + Pequeno), new PointF(x + w, y + h - grande));
        colete.AddQuadraticBezier(new PointF(x + w, y + h - grande), new PointF(x + w, y + h), new PointF(x + w - grande, y + h));
        colete.AddLine(new PointF(x + w - grande, y + h), new PointF(x + Pequeno, y + h));
        colete.AddQuadraticBezier(new PointF(x + Pequeno, y + h), new PointF(x, y + h), new PointF(x, y + h - Pequeno));
        colete.AddLine(new PointF(x, y + h - Pequeno), new PointF(x, y + grande));
        colete.AddQuadraticBezier(new PointF(x, y + grande), new PointF(x, y), new PointF(x + grande, y));
        colete.CloseFigure();

        return colete.Build();
    }

    private static IPath ConstruirMoldeDaCamisa()
    {
        PathBuilder molde = new();

        molde.AddLine(new PointF(30, 10), new PointF(43, 4));
        molde.AddQuadraticBezier(new PointF(43, 4), new PointF(50, 13), new PointF(57, 4));
        molde.AddLines(new PointF(57, 4), new PointF(70, 10), new PointF(93, 25), new PointF(84, 43), new PointF(74, 37), new PointF(74, 93));
        molde.AddQuadraticBezier(new PointF(74, 93), new PointF(50, 99), new PointF(26, 93));
        molde.AddLines(new PointF(26, 93), new PointF(26, 37), new PointF(16, 43), new PointF(7, 25), new PointF(30, 10));
        molde.CloseFigure();

        return molde.Build();
    }

    private static IPath Curva(PointF de, PointF controle, PointF ate)
    {
        PathBuilder curva = new();
        curva.AddQuadraticBezier(de, controle, ate);

        return curva.Build();
    }
}
