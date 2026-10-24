using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PeladaBot.Card;

/// <summary>
/// Desenha um <see cref="CardDoTime"/> como PNG, sobre um gramado: o escudo na emenda, o título, e cada confronto
/// com o seu placar e o seu campo, tudo com as fontes embutidas.
/// </summary>
public static class DesenhistaDoCard
{
    private const float LarguraDaBordaDoColete = 3f;
    private const float AlfaDasListrasDoColete = 0.16f;
    private const float RaioDoVersus = 8f;
    private const float LarguraDaBordaDoVersus = 3f;
    private const float LarguraDoAroDoEscudo = 6f;
    private const float ZoomDoEscudo = 1.08f;
    private const float AlfaDaSombraDoEscudo = 0.3f;
    private const float AlcanceDaSombraDoEscudo = 16f;
    private const float QuedaDaSombraDoEscudo = 6f;
    private const float AlfaDasSombras = 0.35f;
    private const float QuedaDasSombras = 3f;
    private const float DesfoqueDasSombras = 3f;

    private static readonly PngEncoder _codificador = new()
    {
        ColorType = PngColorType.Rgb,
        CompressionLevel = PngCompressionLevel.DefaultCompression,
    };

    public static byte[] Desenhar(CardDoTime card)
    {
        ArgumentNullException.ThrowIfNull(card);

        using Image<Rgba32> imagem = new(card.Largura, card.Altura);
        float emenda = card.Largura / 2f;
        Size tamanho = new(card.Largura, card.Altura);

        // O canvas grava os comandos e só desenha quando o Paint termina: as imagens que ele copia ficam vivas até lá.
        using Image<Rgba32> sombras = CamadaDeSombras(card.Bloco, tamanho);
        using Image<Rgba32>? escudo = card.Escudo is null ? null : EscudoRecortado(card.Escudo);

        imagem.Mutate(contexto => contexto.Paint(tela =>
        {
            DesenharGramado(tela, card);
            DesenhistaDoCampo.Desenhar(tela, card.Bloco.Campo, emenda);
            tela.DrawImage(sombras, sombras.Bounds, sombras.Bounds, null);
            DesenharCabecalho(tela, card, escudo);
            DesenharPlacar(tela, card.Bloco);
            DesenhistaDoJogador.Desenhar(tela, card.Bloco.Jogadores);
        }));

        using MemoryStream png = new();
        imagem.SaveAsPng(png, _codificador);

        return png.ToArray();
    }

    /// <summary>Uma linha de texto exatamente como o layout a mediu, letra a letra quando ela tem espaçamento.</summary>
    internal static void Escrever(DrawingCanvas tela, TextoDoCard texto, Color cor)
    {
        Brush pincel = Brushes.Solid(cor);

        foreach ((string parte, RichTextOptions opcoes) in FontesDoCard.PartesDoDesenho(texto))
        {
            tela.DrawText(opcoes, parte, pincel, null);
        }
    }

    /// <summary>Gramado em degradê, do verde claro no alto ao mais fechado embaixo.</summary>
    private static void DesenharGramado(DrawingCanvas tela, CardDoTime card)
    {
        tela.Fill(
            Vertical(0, card.Altura, PaletaDoCard.GramaTopo, PaletaDoCard.GramaBase),
            new RectanglePolygon(0, 0, card.Largura, card.Altura));
    }

    /// <summary>
    /// As sombras de coletes, do "X", das camisas e das placas numa camada só, desfocada uma vez, antes de tudo isso: a
    /// sombra de um jogador nunca escurece a camisa de outro.
    /// </summary>
    private static Image<Rgba32> CamadaDeSombras(BlocoDoCard bloco, Size tamanho)
    {
        Image<Rgba32> sombras = new(tamanho.Width, tamanho.Height);
        Brush sombra = Brushes.Solid(PaletaDoCard.Sombra.WithAlpha(AlfaDasSombras));
        Matrix4x4 queda = Matrix4x4.CreateTranslation(0, QuedaDasSombras, 0);

        sombras.Mutate(camada => camada
            .Paint(formas =>
            {
                foreach (TimeNoPlacar time in bloco.Times)
                {
                    formas.Fill(sombra, Formas.Colete(time.Colete).Transform(queda));
                }

                if (bloco.Versus is not null)
                {
                    formas.Fill(sombra, Formas.RetanguloArredondado(bloco.Versus.Limites, RaioDoVersus).Transform(queda));
                }

                foreach (JogadorDoCard jogador in bloco.Jogadores)
                {
                    foreach (IPath forma in DesenhistaDoJogador.FormasComSombra(jogador))
                    {
                        formas.Fill(sombra, forma.Transform(queda));
                    }
                }
            })
            .GaussianBlur(DesfoqueDasSombras));

        return sombras;
    }

    private static void DesenharCabecalho(DrawingCanvas tela, CardDoTime card, Image<Rgba32>? escudoRecortado)
    {
        if (card.Escudo is not null && escudoRecortado is not null)
        {
            DesenharEscudo(tela, card.Escudo, escudoRecortado);
        }

        if (card.NomeDoGrupo is not null)
        {
            Escrever(tela, card.NomeDoGrupo, PaletaDoCard.Titulo);
        }

        Escrever(tela, card.Titulo, PaletaDoCard.Titulo);
    }

    /// <summary>Os coletes com as listras, o "X" no meio, e o nome de cada time em claro sobre o gramado.</summary>
    private static void DesenharPlacar(DrawingCanvas tela, BlocoDoCard bloco)
    {
        foreach (TimeNoPlacar time in bloco.Times)
        {
            IPath colete = Formas.Colete(time.Colete);
            RectangleF limites = time.Colete;

            tela.Fill(Brushes.Solid(PaletaDoCard.Colete(time.Cor)), colete);

            for (int listra = 1; listra <= 3; listra++)
            {
                tela.Fill(
                    Brushes.Solid(Color.White.WithAlpha(AlfaDasListrasDoColete)),
                    new RectanglePolygon(
                        limites.X + (limites.Width * (0.18f + (listra * 0.16f))),
                        limites.Y + 4,
                        limites.Width * 0.05f,
                        limites.Height - 8));
            }

            tela.Draw(Pens.Solid(PaletaDoCard.Branco, LarguraDaBordaDoColete), colete);
            Escrever(tela, time.Nome, PaletaDoCard.TextoClaro);

            if (time.Faltam is not null)
            {
                tela.Fill(Brushes.Solid(PaletaDoCard.Placa), Formas.RetanguloArredondado(time.Faltam.Limites, time.Faltam.Limites.Height / 2));
                Escrever(tela, time.Faltam.Texto, PaletaDoCard.TextoClaro);
            }
        }

        if (bloco.Versus is not null)
        {
            IPath caixa = Formas.RetanguloArredondado(bloco.Versus.Limites, RaioDoVersus);

            tela.Fill(Brushes.Solid(PaletaDoCard.Placa), caixa);
            tela.Draw(Pens.Solid(PaletaDoCard.Branco, LarguraDaBordaDoVersus), caixa);
            Escrever(tela, bloco.Versus.Texto, PaletaDoCard.TextoClaro);
        }
    }

    /// <summary>
    /// O escudo recortado num círculo, como a foto de um grupo no WhatsApp, um pouco ampliado para o fundo dele não
    /// aparecer nas bordas, sobre uma sombra suave e com um aro branco.
    /// </summary>
    private static void DesenharEscudo(DrawingCanvas tela, EscudoDoCard escudo, Image<Rgba32> recorte)
    {
        float raio = recorte.Width / 2f;
        Point canto = new((int)Math.Round(escudo.Limites.X), (int)Math.Round(escudo.Limites.Y));
        PointF centro = new(canto.X + raio, canto.Y + raio);
        PointF centroDaSombra = new(centro.X, centro.Y + QuedaDaSombraDoEscudo);
        float raioDaSombra = raio + AlcanceDaSombraDoEscudo;

        tela.Fill(
            new RadialGradientBrush(
                centroDaSombra,
                raioDaSombra,
                GradientRepetitionMode.None,
                new ColorStop(0.75f, PaletaDoCard.Sombra.WithAlpha(AlfaDaSombraDoEscudo)),
                new ColorStop(1, Color.Transparent)),
            new EllipsePolygon(centroDaSombra, raioDaSombra));

        tela.DrawImage(recorte, recorte.Bounds, new RectangleF(canto, recorte.Size), null);
        tela.Draw(Pens.Solid(PaletaDoCard.Branco, LarguraDoAroDoEscudo), new EllipsePolygon(centro, raio));
        tela.Draw(
            Pens.Solid(PaletaDoCard.AroExterno.WithAlpha(0.5f), 1.5f),
            new EllipsePolygon(centro, raio + (LarguraDoAroDoEscudo / 2) + 1));
    }

    /// <summary>A imagem do escudo ampliada e recortada num círculo: um círculo pintado com ela, nada fora dele.</summary>
    private static Image<Rgba32> EscudoRecortado(EscudoDoCard escudo)
    {
        int diametro = (int)Math.Round(escudo.Limites.Width);
        float raio = diametro / 2f;
        int ampliado = (int)Math.Round(diametro * ZoomDoEscudo);
        int sobra = (ampliado - diametro) / 2;

        using Image<Rgba32> imagem = Image.Load<Rgba32>(escudo.Imagem);
        imagem.Mutate(contexto => contexto.Resize(ampliado, ampliado).Crop(new Rectangle(sobra, sobra, diametro, diametro)));

        Image<Rgba32> recorte = new(diametro, diametro);
        recorte.Mutate(contexto => contexto.Paint(circulo =>
            circulo.Fill(new ImageBrush<Rgba32>(imagem), new EllipsePolygon(raio, raio, raio))));

        return recorte;
    }

    private static LinearGradientBrush Vertical(float topo, float @base, Color cima, Color baixo) => new(
        new PointF(0, topo),
        new PointF(0, @base),
        GradientRepetitionMode.None,
        new ColorStop(0, cima),
        new ColorStop(1, baixo));
}
