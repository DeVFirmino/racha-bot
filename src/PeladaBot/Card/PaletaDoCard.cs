using PeladaBot.Sorteio;
using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>As cores do card: gramado neutro, linhas brancas e os coletes.</summary>
internal static class PaletaDoCard
{
    public static readonly Color GramaTopo = Color.ParseHex("1B7A43");
    public static readonly Color GramaBase = Color.ParseHex("145C32");
    public static readonly Color FaixaClara = Color.ParseHex("FFFFFF").WithAlpha(0.045f);
    public static readonly Color FaixaEscura = Color.ParseHex("000000").WithAlpha(0.08f);
    public static readonly Color Linha = Color.ParseHex("FFFFFF").WithAlpha(0.88f);
    public static readonly Color PontoCentral = Color.ParseHex("FFFFFF");
    public static readonly Color Branco = Color.ParseHex("FFFFFF");
    public static readonly Color Placa = Color.ParseHex("102418");
    public static readonly Color TextoClaro = Color.ParseHex("F4F7F2");
    public static readonly Color Titulo = Color.ParseHex("F4F7F2");
    public static readonly Color Sombra = Color.ParseHex("04140C");
    public static readonly Color AroExterno = Color.ParseHex("0B1F14");

    public static readonly Color CamisaDeGoleiro = Color.ParseHex("2B2B2E");
    public static readonly Color ContornoDaCamisa = Color.ParseHex("04140C").WithAlpha(0.35f);

    public static readonly Color Laranja = Color.ParseHex("F47B20");
    public static readonly Color Verde = Color.ParseHex("2E9E4F");
    public static readonly Color Cinza = Color.ParseHex("A7ADB4");
    public static readonly Color Azul = Color.ParseHex("2F6FD6");

    /// <summary>
    /// O 5º time não tem colete; ganha um roxo que nenhum colete tem (o azul-claro de antes se confundia com o Azul).
    /// </summary>
    public static readonly Color SemColete = Color.ParseHex("8E44AD");

    public static Color Colete(CorDoTime? cor) => cor switch
    {
        CorDoTime.Laranja => Laranja,
        CorDoTime.Verde => Verde,
        CorDoTime.Cinza => Cinza,
        CorDoTime.Azul => Azul,
        null => SemColete,
        _ => throw new ArgumentOutOfRangeException(nameof(cor), cor, "Cor de colete desconhecida."),
    };
}
