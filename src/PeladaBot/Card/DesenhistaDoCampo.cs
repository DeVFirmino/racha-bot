using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace PeladaBot.Card;

/// <summary>
/// O campo de um confronto, visto de cima e deitado: faixas de grama e linhas brancas. A emenda faz as vezes da
/// linha do meio-campo.
/// </summary>
internal static class DesenhistaDoCampo
{
    private const float RaioDoCampo = 18f;
    private const int Faixas = 14;
    private const float RecuoDasLinhas = 12f;
    private const float RaioDasLinhas = 6f;
    private const float LarguraDasLinhas = 2.4f;
    private const float RaioDoPontoCentral = 4.5f;

    public static void Desenhar(DrawingCanvas tela, RectangleF campo, float emenda)
    {
        IPath recorte = Formas.RetanguloArredondado(campo, RaioDoCampo);
        float larguraDaFaixa = campo.Width / Faixas;

        // As faixas só dentro do campo; o recorte termina no Restore.
        tela.Save();
        tela.Clip(recorte);

        for (int faixa = 0; faixa < Faixas; faixa++)
        {
            float esquerda = campo.Left + (faixa * larguraDaFaixa);

            tela.Fill(
                Brushes.Solid(faixa % 2 == 0 ? PaletaDoCard.FaixaClara : PaletaDoCard.FaixaEscura),
                new RectanglePolygon(esquerda, campo.Top, larguraDaFaixa, campo.Height));
        }

        tela.Restore();

        DesenharLinhas(tela, campo, Pens.Solid(PaletaDoCard.Linha, LarguraDasLinhas));
        tela.Fill(Brushes.Solid(PaletaDoCard.PontoCentral), new EllipsePolygon(emenda, campo.Top + (campo.Height / 2), RaioDoPontoCentral));
    }

    /// <summary>A borda, o círculo central e, em cada ponta, a grande e a pequena área.</summary>
    private static void DesenharLinhas(DrawingCanvas tela, RectangleF campo, Pen linha)
    {
        RectangleF borda = RectangleF.Inflate(campo, -RecuoDasLinhas, -RecuoDasLinhas);
        float meioX = campo.Left + (campo.Width / 2);
        float meioY = campo.Top + (campo.Height / 2);

        tela.Draw(linha, Formas.RetanguloArredondado(borda, RaioDasLinhas));
        tela.Draw(linha, new EllipsePolygon(meioX, meioY, campo.Height * 0.14f));

        DesenharAreas(tela, linha, campo, borda, direita: false);
        DesenharAreas(tela, linha, campo, borda, direita: true);
    }

    private static void DesenharAreas(DrawingCanvas tela, Pen linha, RectangleF campo, RectangleF borda, bool direita)
    {
        DesenharArea(tela, linha, borda, direita, campo.Width * 0.12f, campo.Height * 0.58f);
        DesenharArea(tela, linha, borda, direita, campo.Width * 0.045f, campo.Height * 0.3f);
    }

    private static void DesenharArea(DrawingCanvas tela, Pen linha, RectangleF borda, bool direita, float largura, float altura)
    {
        float esquerda = direita ? borda.Right - largura : borda.Left;

        tela.Draw(linha, new RectanglePolygon(esquerda, borda.Top + ((borda.Height - altura) / 2), largura, altura));
    }
}
