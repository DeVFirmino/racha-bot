using System.Reflection;
using System.Text;
using PeladaBot.Card;
using PeladaBot.Listas;
using PeladaBot.Sorteio;
using PeladaBot.Tests.UtilitariosDeTeste;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace PeladaBot.Tests.Card;

/// <summary>Testes de fumaça do desenho. Cada card renderizado também é salvo em card-samples/, ao lado do assembly de teste, para uma olhada visual.</summary>
public sealed class DesenhistaDoCardTestes
{
    private const int TamanhoDoCard = 1080;
    private const string PastaDeAmostras = "card-samples";
    private const string PrefixoDoRecursoDeFonte = "PeladaBot.Card.Fonts.";
    private const ushort IdDeGlifoAusente = 0;

    // Um PNG quadrado por campo
    [Fact]
    public void DeveCodificarUmPngQuadradoDe1080PixelsPorCampoQuandoOCardEhRenderizado()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTime(CorDoTime.Cinza, "Nuno", "Tales", "Wilson", "Samuel", "Hugo Paco", "Ivo")
            .ComTime(CorDoTime.Laranja, "Cauã Luz", "Rico", "Edu", "Mauro", "Ciro", "Kaio")
            .ComTimeIncompleto(CorDoTime.Azul, null, "Flavio", "Leandro", "Duda Neri", "Sergio", "Alvaro")
            .Build();

        IReadOnlyList<byte[]> pngs = Desenhar(LayoutDoCard.Compor(sorteados));

        SalvarAmostras("3-teams-default-identity", pngs);
        pngs.Should().HaveCount(2).And.AllSatisfy(png =>
        {
            ImageInfo imagem = Image.Identify(png);
            imagem.Width.Should().Be(TamanhoDoCard);
            imagem.Height.Should().Be(TamanhoDoCard);
            imagem.Metadata.DecodedImageFormat.Should().Be(PngFormat.Instance);
        });
    }

    // Os cenários do card, salvos em card-samples/ para conferir no olho
    [Theory]
    [InlineData("2-teams")]
    [InlineData("2-teams-rotating-keeper")]
    [InlineData("2-teams-one-incomplete")]
    [InlineData("3-teams")]
    [InlineData("3-teams-one-incomplete")]
    [InlineData("4-teams-one-incomplete")]
    [InlineData("5-teams")]
    public void DeveRenderizarCadaCenarioDaEscalacaoQuandoOGrupoTemIdentidade(string cenario)
    {
        TimesSorteados sorteados = Cenario(cenario);
        IdentidadeDoCard identidade = new() { Titulo = "PELADA", Escudo = EscudoDeTeste() };

        IReadOnlyList<byte[]> pngs = Desenhar(LayoutDoCard.Compor(sorteados, identidade));

        SalvarAmostras(cenario, pngs);
        DevemSerQuadradosDe1080(pngs);
    }

    // O mesmo sorteio sai sempre no mesmo card, pixel por pixel: o casario não sorteia nada
    [Fact]
    public void DeveGerarOMesmoPngQuandoOMesmoSorteioEhDesenhadoDuasVezes()
    {
        TimesSorteados sorteados = Cenario("3-teams");

        IReadOnlyList<byte[]> primeiro = Desenhar(LayoutDoCard.Compor(sorteados));
        IReadOnlyList<byte[]> segundo = Desenhar(LayoutDoCard.Compor(sorteados));

        primeiro.Should().HaveSameCount(segundo);
        primeiro.Zip(segundo).Should().AllSatisfy(par => par.First.Should().Equal(par.Second));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DeveRenderizarNomesLongosEAcentuadosQuandoOSorteioTemDeDoisAQuatroTimes(int quantidadeDeTimes)
    {
        TimesSorteados sorteados = SorteioComNomesLongosEAcentuados(quantidadeDeTimes);

        IReadOnlyList<byte[]> pngs = Desenhar(LayoutDoCard.Compor(sorteados));

        SalvarAmostras($"{quantidadeDeTimes}-teams-long-names", pngs);
        DevemSerQuadradosDe1080(pngs);
    }

    // Times pequenos, de 4 a 9 pagos
    [Fact]
    public void DeveRenderizarOCardQuandoOsTimesSaoPequenos()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTime(CorDoTime.Verde, "Nuno", "Tales", "Wilson")
            .ComTime(CorDoTime.Laranja, "Cauã Luz", "Rico", "Edu")
            .Build();

        IReadOnlyList<byte[]> pngs = Desenhar(LayoutDoCard.Compor(sorteados));

        SalvarAmostras("2-small-teams", pngs);
        DevemSerQuadradosDe1080(pngs);
    }

    // Identidade do grupo: o escudo recortado num círculo ao lado do nome
    [Fact]
    public void DeveRenderizarOEscudoEONomeDoGrupoQuandoOGrupoTemIdentidade()
    {
        TimesSorteados sorteados = SorteioComNomesLongosEAcentuados(3);
        IdentidadeDoCard identidade = new() { Titulo = "PELADA", Escudo = EscudoDeTeste() };

        IReadOnlyList<byte[]> pngs = Desenhar(LayoutDoCard.Compor(sorteados, identidade));

        SalvarAmostras("3-teams-group-crest", pngs);
        DevemSerQuadradosDe1080(pngs);
    }

    // "As fontes (texto e luva) ficam embutidas no projeto" (Imagem dos times)
    [Theory]
    [InlineData("BarlowCondensed-SemiBold.ttf", "ãáâàéêíóôõúüçÃÁÂÉÊÍÓÔÕÚÇñ…·–ċġħżĊĠĦŻESCALAÇÕESVAGA")]
    [InlineData("BarlowCondensed-ExtraBold.ttf", "LARANJAVERDECINZAAZULNOBIBNEEDRODÍZIO1234PÃÁÂÀÉÊÍÓÔÕÚÜÇÑŽĊĠĦŻ…·")]
    [InlineData("NotoEmoji[wght].ttf", "⚽🧤")]
    public void DeveTrazerTodaFonteComQueOCardDesenhaQuandoOAssemblyEhConstruido(string nomeDoArquivo, string caracteres)
    {
        Assembly assembly = typeof(DesenhistaDoCard).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(PrefixoDoRecursoDeFonte + nomeDoArquivo)
            ?? throw new InvalidOperationException($"{nomeDoArquivo} não está embutido: o card dependeria de uma fonte do sistema.");

        FontCollection colecao = new();
        Font fonte = colecao.Add(stream).CreateFont(TamanhoDoCard);

        caracteres.EnumerateRunes().Should().AllSatisfy(caractere =>
            IdDoGlifoDe(fonte, caractere).Should().NotBe(IdDeGlifoAusente, $"{nomeDoArquivo} tem que desenhar {caractere}"));
    }

    private static IReadOnlyList<byte[]> Desenhar(IReadOnlyList<CardDoTime> cards) => [.. cards.Select(DesenhistaDoCard.Desenhar)];

    private static void DevemSerQuadradosDe1080(IReadOnlyList<byte[]> pngs) =>
        pngs.Should().NotBeEmpty().And.AllSatisfy(png =>
        {
            ImageInfo imagem = Image.Identify(png);
            imagem.Width.Should().Be(TamanhoDoCard);
            imagem.Height.Should().Be(TamanhoDoCard);
        });

    /// <summary>Um caractere que a fonte não tem mapeia para o glifo 0, a caixa vazia.</summary>
    private static ushort IdDoGlifoDe(Font fonte, Rune caractere) =>
        fonte.TryGetGlyphId(new CodePoint(caractere.Value), out ushort idDoGlifo) ? idDoGlifo : IdDeGlifoAusente;

    /// <summary>Um escudo de mentira: um JPEG quadrado de fundo preto com um losango verde, como um escudo real.</summary>
    private static byte[] EscudoDeTeste()
    {
        using Image<SixLabors.ImageSharp.PixelFormats.Rgba32> imagem = new(320, 320, Color.Black.ToPixel<SixLabors.ImageSharp.PixelFormats.Rgba32>());
        imagem.Mutate(contexto => contexto.Paint(tela => tela.Fill(
            Brushes.Solid(Color.ParseHex("1E8C3A")),
            new SixLabors.ImageSharp.Drawing.Polygon(new SixLabors.ImageSharp.Drawing.LinearLineSegment(
                new PointF(160, 20), new PointF(300, 160), new PointF(160, 300), new PointF(20, 160))))));
        using MemoryStream jpeg = new();
        imagem.SaveAsJpeg(jpeg);

        return jpeg.ToArray();
    }

    /// <summary>Os cenários do card com nomes de mentira, do tamanho dos de um grupo de verdade.</summary>
    private static TimesSorteados Cenario(string nome)
    {
        string[] laranja = ["Nico Braga", "Duda Neri", "Ruy Pacheco", "Iago Leme", "Bento Cruz"];
        string[] verde = ["Luan Ferraz", "Enzo Paiva", "Gael Moura", "Heitor Dias", "Otto Klein"];
        string[] cinza = ["Milo Castro", "Cael Prado", "Luca P", "Renzo", "Nilo Bastos"];

        return nome switch
        {
            "2-teams" => new ConstrutorDeTimesSorteados()
                .ComTime(CorDoTime.Laranja, "Téo Vargas", laranja).ComTime(CorDoTime.Verde, "Cauã Luz", verde).Build(),
            "2-teams-rotating-keeper" => new ConstrutorDeTimesSorteados()
                .ComTime(CorDoTime.Laranja, "Téo Vargas", laranja).ComTimeDeRodizio(CorDoTime.Verde, "Cauã Luz", verde).Build(),
            "2-teams-one-incomplete" => new ConstrutorDeTimesSorteados()
                .ComTime(CorDoTime.Laranja, "Téo Vargas", laranja).ComTimeIncompleto(CorDoTime.Verde, "Cauã Luz", verde[..3]).Build(),
            "3-teams" => new ConstrutorDeTimesSorteados()
                .ComTime(CorDoTime.Laranja, "Téo Vargas", laranja).ComTime(CorDoTime.Verde, "Cauã Luz", verde)
                .ComTimeComOsGoleirosDoGol(CorDoTime.Cinza, cinza).Build(),
            "3-teams-one-incomplete" => new ConstrutorDeTimesSorteados()
                .ComTime(CorDoTime.Laranja, "Téo Vargas", laranja).ComTime(CorDoTime.Verde, "Cauã Luz", verde)
                .ComTimeIncompletoComOsGoleirosDoGol(CorDoTime.Cinza, cinza[..3]).Build(),
            "5-teams" => new ConstrutorDeTimesSorteados()
                .ComTime(CorDoTime.Laranja, "Téo Vargas", laranja).ComTime(CorDoTime.Verde, "Cauã Luz", verde)
                .ComTimeDeRodizio(CorDoTime.Cinza, "Dico Souza", cinza)
                .ComTimeDeRodizio(CorDoTime.Azul, "Hélio", "Ivo Santos", "Rômulo", "Wilson", "Tales", "Kaio")
                .ComTimeIncompleto(null, null, "Nuno", "Guto", "Mauro").Build(),
            "4-teams-one-incomplete" => new ConstrutorDeTimesSorteados()
                .ComTime(CorDoTime.Laranja, "Téo Vargas", laranja).ComTime(CorDoTime.Verde, "Cauã Luz", verde)
                .ComTimeDeRodizio(CorDoTime.Cinza, "Dico Souza", cinza)
                .ComTimeIncompleto(CorDoTime.Azul, null, "Hélio", "Ivo Santos", "Rômulo", "Wilson").Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(nome), nome, "Cenário desconhecido."),
        };
    }

    private static TimesSorteados SorteioComNomesLongosEAcentuados(int quantidadeDeTimes)
    {
        string[][] times =
        [
            ["Bartolomeu Gusmão Neto", "Hugo Paco", "Leandro", "Pedro de Alcântara Francisco Antônio João Carlos", "Ivo"],
            ["Alvaro Gonçalves", "Conceição Araújo", "Sergio", "Sebastião Brandão Magalhães", "Duda Neri"],
            ["Ezequiel Damásio Vasconcelos", "Raí", "Inês", "Lúcio Antônio", "Gonçalo Ferreira da Conceição"],
            ["Ćiril Żammit", "Ġorġ Ħabib", "Maximiliano Fernandes", "Caçapava", "Jaqueline Lopes"],
        ];
        string?[] goleiros = ["Marcão Gusmão Albuquerque", "Tião", "Sebastião", null];
        CorDoTime?[] cores = [CorDoTime.Verde, CorDoTime.Cinza, CorDoTime.Laranja, null];

        ConstrutorDeTimesSorteados construtor = new();

        for (int indice = 0; indice < quantidadeDeTimes; indice++)
        {
            if (goleiros[indice] is null)
            {
                construtor.ComTimeIncompleto(cores[indice], null, times[indice]);
            }
            else
            {
                construtor.ComTime(cores[indice], goleiros[indice], times[indice]);
            }
        }

        return construtor.Build();
    }

    /// <summary>Um card vira nome.png; dois viram nome-1.png e nome-2.png, na ordem em que vão para o grupo.</summary>
    private static void SalvarAmostras(string nome, IReadOnlyList<byte[]> pngs)
    {
        string pasta = Path.Combine(AppContext.BaseDirectory, PastaDeAmostras);

        Directory.CreateDirectory(pasta);

        for (int indice = 0; indice < pngs.Count; indice++)
        {
            string arquivo = pngs.Count == 1 ? nome : $"{nome}-{indice + 1}";

            File.WriteAllBytes(Path.Combine(pasta, arquivo + ".png"), pngs[indice]);
        }
    }
}
