using PeladaBot.Sorteio;
using SixLabors.ImageSharp;

namespace PeladaBot.Card;

/// <summary>
/// Decide o que vai onde nos cards da escalação. Puro: mede o texto com as fontes com que o desenhista desenha, e não
/// desenha nada. O fundo é um gramado; cada confronto é um campo dividido ao meio, o primeiro time à esquerda e o
/// segundo à direita. Cada campo é um card: um ou dois times cabem
/// num card; três ou quatro vão em dois, e o quinto num terceiro, para os nomes continuarem grandes no celular. Acima
/// de cinco não há card: o grupo recebe os times só em texto. O card não mostra data, horário nem local: o jogo é toda quarta e o horário
/// pode mudar, e uma data errada no card confundiria mais do que ajudaria.
/// </summary>
public static class LayoutDoCard
{
    /// <summary>Nenhum texto é desenhado menor que isso. Um nome que não cabe nesse tamanho é cortado com "…".</summary>
    public const float TamanhoMinimoDaFonte = 16f;

    /// <summary>O card é um quadrado de 1080×1080, que a prévia do WhatsApp mostra inteiro.</summary>
    private const int TamanhoDaTela = 1080;

    /// <summary>
    /// Do 5º time em diante não há colete: o 5º ganha uma cor que nenhum colete tem, mas um 6º teria a mesma, então o
    /// card para no 5º e, acima disso, o grupo recebe os times só em texto.
    /// </summary>
    private const int MaximoDeTimes = 5;
    private const int TimesPorCampo = 2;
    private const int MaximoDeJogadoresDeLinhaPorTime = 5;

    /// <summary>A emenda do campo, entre a metade esquerda e a direita.</summary>
    private const float Emenda = TamanhoDaTela / 2f;

    private const string TextoDoTitulo = "ESCALAÇÕES";
    private const string TextoDoVersus = "X";
    private const string TextoDoRodizio = "RODÍZIO";

    private const float MargemLateral = 40f;
    private const float MargemDoCampo = 28f;
    private const float LarguraMaximaDoCabecalho = TamanhoDaTela - (2 * MargemLateral);

    // O cabeçalho: o escudo (ou o nome do grupo) na emenda e o título
    private const float CentroDoEscudoY = 102f;
    private const float RaioDoEscudo = 84f;
    private const float LinhaDeBaseDoNomeDoGrupo = 140f;
    private const float FonteDoNomeDoGrupo = 72f;
    private const float LinhaDeBaseDoTitulo = 270f;
    private const float FonteDoTitulo = 86f;

    /// <summary>Onde o cabeçalho termina e o placar começa.</summary>
    private const float Topo = 300f;

    // O placar: coletes de 132×70 a 30 px da emenda, o "X" no meio e o nome por fora
    private const float LarguraDoColete = 132f;
    private const float AlturaDoColete = 70f;
    private const float AfastamentoDoColete = 30f;
    private const float LadoDoVersus = 48f;
    private const float DistanciaDoNome = 20f;
    private const float FonteDoNomeNoPlacar = 30f;
    private const float EspacoAteOCampo = 18f;

    // O jogador: a camisa e, embaixo, a placa com o nome, no tamanho que se lê no celular
    private const float Camisa = 84f;
    private const float FonteDaPlaca = 36f;
    /// <summary>
    /// Na mesma linha, o defensor (a 0,32 da metade) e o atacante (encostado na emenda) cabem com 12 px de folga
    /// mesmo com as duas placas no máximo: 192 + L/2 + 12 ≤ 532 − L dá L ≤ 218.
    /// </summary>
    private const float LarguraMaximaDaPlaca = 216f;

    // A placa: largura do nome mais um respiro, altura proporcional à fonte
    private const float RespiroDaPlaca = 0.9f;
    private const float AlturaDaPlaca = 1.34f;
    private const float LinhaDeBaseNaPlaca = 0.98f;
    private const float DistanciaDoSelo = 0.8f;
    private const float FonteDoSelo = 0.64f;
    private const float MargemDaPlacaNaLateral = 10f;
    private const float MargemDaPlacaNaEmenda = 8f;

    /// <summary>
    /// Escala os times sorteados, sempre na ordem do sorteio, em um card por campo: o primeiro e o segundo time no
    /// primeiro card; o terceiro e o quarto (ou só o terceiro) no segundo.
    /// </summary>
    public static IReadOnlyList<CardDoTime> Compor(TimesSorteados sorteados) => Compor(sorteados, IdentidadeDoCard.Padrao);

    /// <summary>
    /// Como <see cref="Compor(TimesSorteados)"/>, com o escudo do grupo no meio do cabeçalho. Todo card repete o
    /// cabeçalho, para valer sozinho se for encaminhado.
    /// </summary>
    public static IReadOnlyList<CardDoTime> Compor(TimesSorteados sorteados, IdentidadeDoCard identidade)
    {
        ArgumentNullException.ThrowIfNull(identidade);
        ArgumentNullException.ThrowIfNull(sorteados);
        GarantirQueOsTimesCabem(sorteados);

        return [.. sorteados.Times.Chunk(TimesPorCampo).Select(times => Card(times, identidade))];
    }

    /// <summary>
    /// True quando o card comporta o sorteio: de 1 a 5 times, cada um com até 5 de linha. Acima disso, o grupo recebe
    /// os times só em texto, e isso não é erro.
    /// </summary>
    public static bool Comporta(TimesSorteados sorteados)
    {
        ArgumentNullException.ThrowIfNull(sorteados);

        return sorteados.Times.Count is > 0 and <= MaximoDeTimes
            && sorteados.Times.All(time => time.JogadoresDeLinha.Count <= MaximoDeJogadoresDeLinhaPorTime);
    }

    private static void GarantirQueOsTimesCabem(TimesSorteados sorteados)
    {
        if (sorteados.Times.Count is 0 or > MaximoDeTimes)
        {
            throw new ArgumentException(
                $"O card dispõe de 1 a {MaximoDeTimes} times, não {sorteados.Times.Count}.",
                nameof(sorteados));
        }

        if (sorteados.Times.Any(time => time.JogadoresDeLinha.Count > MaximoDeJogadoresDeLinhaPorTime))
        {
            throw new ArgumentException(
                $"O card dispõe no máximo {MaximoDeJogadoresDeLinhaPorTime} jogadores de linha por time.",
                nameof(sorteados));
        }
    }

    /// <summary>O cabeçalho e, embaixo, o campo de um confronto: um ou dois times.</summary>
    private static CardDoTime Card(Time[] times, IdentidadeDoCard identidade) => new()
    {
        Largura = TamanhoDaTela,
        Altura = TamanhoDaTela,
        Escudo = identidade.Escudo is { } imagem
            ? new EscudoDoCard
            {
                Imagem = imagem,
                Limites = new RectangleF(
                    Emenda - RaioDoEscudo,
                    CentroDoEscudoY - RaioDoEscudo,
                    2 * RaioDoEscudo,
                    2 * RaioDoEscudo),
            }
            : null,
        NomeDoGrupo = identidade.Escudo is null
            ? Centralizado(
                identidade.Titulo,
                Estilo(PesoDaFonteDoCard.ExtraNegrito, FonteDoNomeDoGrupo, 32, 0),
                LinhaDeBaseDoNomeDoGrupo)
            : null,
        Titulo = Centralizado(
            TextoDoTitulo,
            Estilo(PesoDaFonteDoCard.ExtraNegrito, FonteDoTitulo, FonteDoTitulo, 3),
            LinhaDeBaseDoTitulo),
        Bloco = Bloco(times),
    };

    /// <summary>O placar no alto, logo abaixo do cabeçalho, e o campo até a margem de baixo do card.</summary>
    private static BlocoDoCard Bloco(Time[] times)
    {
        float centroDoPlacar = Topo + (AlturaDoColete / 2);
        float topoDoCampo = Topo + AlturaDoColete + EspacoAteOCampo;
        RectangleF campo = new(MargemDoCampo, topoDoCampo, TamanhoDaTela - (2 * MargemDoCampo), TamanhoDaTela - MargemDoCampo - topoDoCampo);
        List<TimeNoPlacar> noPlacar = [];
        List<JogadorDoCard> jogadores = [];
        SeloDoCard? versus = null;

        if (times.Length == 2)
        {
            float esquerdaDoPrimeiro = Emenda - AfastamentoDoColete - LarguraDoColete;

            noPlacar.Add(NoPlacar(times[0], noLadoBranco: true, esquerdaDoPrimeiro, centroDoPlacar));
            noPlacar.Add(NoPlacar(times[1], noLadoBranco: false, Emenda + AfastamentoDoColete, centroDoPlacar));
            versus = Versus(centroDoPlacar);
            jogadores.AddRange(NaMetade(times[0], campo, noLadoBranco: true));
            jogadores.AddRange(NaMetade(times[1], campo, noLadoBranco: false));
        }
        else
        {
            noPlacar.Add(NoPlacar(times[0], noLadoBranco: false, Emenda - (LarguraDoColete / 2), centroDoPlacar));
            jogadores.AddRange(NoCampoInteiro(times[0], campo));
        }

        return new BlocoDoCard { Times = noPlacar, Versus = versus, Campo = campo, Jogadores = jogadores };
    }

    /// <summary>
    /// O colete do time e o nome por fora dele — à esquerda no lado branco, à direita no vermelho —, com "FALTAM N"
    /// embaixo quando o time está incompleto.
    /// </summary>
    private static TimeNoPlacar NoPlacar(Time time, bool noLadoBranco, float esquerdaDoColete, float centro)
    {
        RectangleF colete = new(esquerdaDoColete, centro - (AlturaDoColete / 2), LarguraDoColete, AlturaDoColete);
        float fonte = FonteDoNomeNoPlacar;
        float ancora = noLadoBranco ? colete.Left - DistanciaDoNome : colete.Right + DistanciaDoNome;
        float larguraMaxima = noLadoBranco ? ancora - MargemLateral : TamanhoDaTela - MargemLateral - ancora;
        bool incompleto = time.Completo is false;
        TextoAjustado nome = AjustadorDeTexto.AjustarLinha(
            NomeDoTime(time), Estilo(PesoDaFonteDoCard.ExtraNegrito, fonte, TamanhoMinimoDaFonte, 3), larguraMaxima);
        SeloDoCard? faltam = null;

        if (incompleto)
        {
            float fonteDoSelo = Math.Max(TamanhoMinimoDaFonte, fonte * 0.56f);
            TextoAjustado texto = AjustadorDeTexto.AjustarLinha(
                $"FALTAM {time.Faltam}", Estilo(PesoDaFonteDoCard.ExtraNegrito, fonteDoSelo, fonteDoSelo, 1.5f), larguraMaxima);
            float largura = texto.Extensao.CaixaDaLinha.Width + (fonteDoSelo * 1.2f);
            float altura = fonteDoSelo * 1.4f;
            float esquerda = noLadoBranco ? ancora - largura : ancora;
            float topo = centro + (fonte * 0.2f);

            faltam = new SeloDoCard
            {
                Limites = new RectangleF(esquerda, topo, largura, altura),
                Texto = NaLinhaDeBase(texto, esquerda + (largura / 2), topo + (fonteDoSelo * 1.02f), Alinhamento.Centro),
            };
        }

        return new TimeNoPlacar
        {
            Cor = time.Cor,
            NoLadoBranco = noLadoBranco,
            Colete = colete,
            Nome = NaLinhaDeBase(
                nome,
                ancora,
                incompleto ? centro - (fonte * 0.12f) : centro + (fonte * 0.34f),
                noLadoBranco ? Alinhamento.Fim : Alinhamento.Inicio),
            Faltam = faltam,
        };
    }

    /// <summary>O "X" vermelho entre os coletes, em cima da emenda.</summary>
    private static SeloDoCard Versus(float centro)
    {
        TextoAjustado x = AjustadorDeTexto.AjustarLinha(
            TextoDoVersus, Estilo(PesoDaFonteDoCard.ExtraNegrito, LadoDoVersus * 0.74f, LadoDoVersus * 0.74f, 0), LadoDoVersus);

        return new SeloDoCard
        {
            Limites = new RectangleF(Emenda - (LadoDoVersus / 2), centro - (LadoDoVersus / 2), LadoDoVersus, LadoDoVersus),
            Texto = NaLinhaDeBase(x, Emenda, centro + (LadoDoVersus * 0.27f), Alinhamento.Centro),
        };
    }

    /// <summary>
    /// Um time na sua metade do campo, o gol na lateral do card e o ataque perto da emenda: o primeiro time na
    /// metade branca, o segundo, espelhado, na vermelha.
    /// </summary>
    private static IEnumerable<JogadorDoCard> NaMetade(Time time, RectangleF campo, bool noLadoBranco)
    {
        float meia = campo.Width / 2;

        foreach (PosicaoNaFormacao posicao in FormacaoDoTime.Escalar(time))
        {
            float profundidade = posicao.Posicao switch
            {
                PosicaoNoCampo.Goleiro => 0.09f,
                PosicaoNoCampo.DefensorEsquerdo or PosicaoNoCampo.DefensorDireito => 0.32f,
                PosicaoNoCampo.Meio => 0.7f,
                _ => 0.86f,
            };
            float x = noLadoBranco ? campo.Left + (profundidade * meia) : campo.Right - (profundidade * meia);

            yield return Jogador(posicao, time.Cor, new PointF(x, AlturaNoCampo(posicao, campo)), noLadoBranco, campo);
        }
    }

    /// <summary>Um time sozinho no campo, como o terceiro: o gol na lateral esquerda, o ataque na direita.</summary>
    private static IEnumerable<JogadorDoCard> NoCampoInteiro(Time time, RectangleF campo)
    {
        foreach (PosicaoNaFormacao posicao in FormacaoDoTime.Escalar(time))
        {
            float comprimento = posicao.Posicao switch
            {
                PosicaoNoCampo.Goleiro => 0.05f,
                PosicaoNoCampo.DefensorEsquerdo or PosicaoNoCampo.DefensorDireito => 0.27f,
                PosicaoNoCampo.Meio => 0.5f,
                _ => 0.76f,
            };
            float x = campo.Left + (comprimento * campo.Width);

            yield return Jogador(posicao, time.Cor, new PointF(x, AlturaNoCampo(posicao, campo)), x <= Emenda, campo);
        }
    }

    /// <summary>Os pares em cima e embaixo; o goleiro e o meio na altura do meio do campo.</summary>
    private static float AlturaNoCampo(PosicaoNaFormacao posicao, RectangleF campo)
    {
        float altura = posicao.Posicao switch
        {
            PosicaoNoCampo.DefensorEsquerdo or PosicaoNoCampo.AtacanteEsquerdo => 0.2f,
            PosicaoNoCampo.DefensorDireito or PosicaoNoCampo.AtacanteDireito => 0.8f,
            _ => 0.5f,
        };

        return campo.Top + (altura * campo.Height) - (Camisa * 0.1f);
    }

    /// <summary>
    /// A camisa sobre <paramref name="pe"/> e, logo abaixo, a placa com o nome. Um nome longo encolhe até o mínimo e
    /// então é cortado com "…"; a placa é empurrada para dentro da metade do jogador, sem passar da emenda nem da
    /// lateral, e a camisa fica no lugar. O selo "RODÍZIO" fica embaixo da camisa, não da placa: uma placa larga,
    /// empurrada para dentro, levaria o selo para cima da camisa do vizinho.
    /// </summary>
    private static JogadorDoCard Jogador(PosicaoNaFormacao posicao, CorDoTime? cor, PointF pe, bool noLadoBranco, RectangleF campo)
    {
        float topoDaPlaca = pe.Y + (Camisa * 0.24f);
        float esquerdaMinima = noLadoBranco ? campo.Left + MargemDaPlacaNaLateral : Emenda + MargemDaPlacaNaEmenda;
        float direitaMaxima = noLadoBranco ? Emenda - MargemDaPlacaNaEmenda : campo.Right - MargemDaPlacaNaLateral;
        TextoAjustado nome = AjustadorDeTexto.AjustarLinha(
            posicao.Rotulo,
            Estilo(PesoDaFonteDoCard.ExtraNegrito, FonteDaPlaca, TamanhoMinimoDaFonte, 0),
            LarguraMaximaDaPlaca - (FonteDaPlaca * RespiroDaPlaca));
        float tamanho = nome.TamanhoDaFonte;
        float larguraDaPlaca = nome.Extensao.Tinta.Width + (tamanho * RespiroDaPlaca);
        float alturaDaPlaca = tamanho * AlturaDaPlaca;
        float centro = Math.Clamp(pe.X, esquerdaMinima + (larguraDaPlaca / 2), direitaMaxima - (larguraDaPlaca / 2));
        TextoDoCard? selo = null;

        if (posicao.Papel == PapelNoCard.GoleiroDeRodizio)
        {
            float fonteDoSelo = Math.Max(TamanhoMinimoDaFonte, MathF.Round(tamanho * FonteDoSelo));
            TextoAjustado rodizio = AjustadorDeTexto.AjustarLinha(
                TextoDoRodizio, Estilo(PesoDaFonteDoCard.ExtraNegrito, fonteDoSelo, fonteDoSelo, 2), LarguraMaximaDaPlaca);
            float larguraDoSelo = rodizio.Extensao.Tinta.Width;
            float centroDoSelo = Math.Clamp(pe.X, esquerdaMinima + (larguraDoSelo / 2), direitaMaxima - (larguraDoSelo / 2));
            selo = NaLinhaDeBase(rodizio, centroDoSelo, topoDaPlaca + alturaDaPlaca + (tamanho * DistanciaDoSelo), Alinhamento.Centro);
        }

        return new JogadorDoCard
        {
            Papel = posicao.Papel,
            Cor = cor,
            NoLadoBranco = noLadoBranco,
            Camisa = new RectangleF(pe.X - (Camisa / 2), pe.Y - (Camisa * 0.85f), Camisa, Camisa),
            Placa = new RectangleF(centro - (larguraDaPlaca / 2), topoDaPlaca, larguraDaPlaca, alturaDaPlaca),
            Nome = NaLinhaDeBase(nome, centro, topoDaPlaca + (tamanho * LinhaDeBaseNaPlaca), Alinhamento.Centro),
            Selo = selo,
        };
    }

    private static string NomeDoTime(Time time) => time.Cor switch
    {
        CorDoTime.Laranja => "LARANJA",
        CorDoTime.Verde => "VERDE",
        CorDoTime.Cinza => "CINZA",
        CorDoTime.Azul => "AZUL",
        null => "SEM COLETE",
        _ => throw new ArgumentOutOfRangeException(nameof(time), time.Cor, "Cor de colete desconhecida."),
    };

    private static TextoDoCard Centralizado(string texto, EstiloDoTexto estilo, float linhaDeBase) =>
        NaLinhaDeBase(AjustadorDeTexto.AjustarLinha(texto, estilo, LarguraMaximaDoCabecalho), Emenda, linhaDeBase, Alinhamento.Centro);

    /// <summary>
    /// Põe a linha com as letras apoiadas em <paramref name="linhaDeBase"/>: começando, centralizada ou terminando em
    /// <paramref name="x"/>, pelo avanço do texto.
    /// </summary>
    private static TextoDoCard NaLinhaDeBase(TextoAjustado ajustado, float x, float linhaDeBase, Alinhamento alinhamento)
    {
        float avanco = ajustado.Extensao.CaixaDaLinha.Width;
        float origemX = alinhamento switch
        {
            Alinhamento.Inicio => x,
            Alinhamento.Centro => x - (avanco / 2),
            _ => x - avanco,
        };
        float origemY = linhaDeBase - ajustado.Extensao.LinhaDeBase;

        return new TextoDoCard
        {
            Texto = ajustado.Texto,
            Peso = ajustado.Peso,
            TamanhoDaFonte = ajustado.TamanhoDaFonte,
            Espacamento = ajustado.Espacamento,
            Origem = new PointF(origemX, origemY),
            Limites = new RectangleF(
                origemX + ajustado.Extensao.Tinta.X,
                origemY + ajustado.Extensao.Tinta.Y,
                ajustado.Extensao.Tinta.Width,
                ajustado.Extensao.Tinta.Height),
        };
    }

    private static EstiloDoTexto Estilo(PesoDaFonteDoCard peso, float tamanhoPreferido, float tamanhoMinimo, float espacamento) => new()
    {
        Peso = peso,
        TamanhoPreferido = tamanhoPreferido,
        TamanhoMinimo = tamanhoMinimo,
        Espacamento = espacamento,
    };

    private enum Alinhamento
    {
        Inicio,
        Centro,
        Fim,
    }
}
