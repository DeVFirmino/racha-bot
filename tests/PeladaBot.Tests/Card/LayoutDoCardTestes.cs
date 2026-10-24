using PeladaBot.Card;
using PeladaBot.Listas;
using PeladaBot.Sorteio;
using PeladaBot.Tests.UtilitariosDeTeste;
using SixLabors.ImageSharp;

namespace PeladaBot.Tests.Card;

public sealed class LayoutDoCardTestes
{
    private const int TamanhoDoCard = 1080;
    private const float FolgaEntrePlacas = 12f;
    private const float Emenda = TamanhoDoCard / 2f;
    private const string NomeEnorme = "Pedro de Alcântara Francisco Antônio João Carlos Xavier de Paula";

    private static readonly RectangleF _tela = new(0, 0, TamanhoDoCard, TamanhoDoCard);
    private static readonly IdentidadeDoCard _comEscudo = new() { Titulo = "PELADA", Escudo = [1, 2, 3] };

    private static readonly string[] _nomesLongos =
    [
        "Bartolomeu Gusmão Neto",
        "Pedro de Alcântara Francisco Antônio João Carlos",
        "Sebastião Brandão Magalhães",
        "Gonçalo Ferreira da Conceição",
        "Maximiliano Fernandes",
    ];

    private static readonly CorDoTime[] _coresNaOrdemDoSorteio = [CorDoTime.Verde, CorDoTime.Cinza, CorDoTime.Laranja, CorDoTime.Azul];

    [Fact]
    public void DevePorOsDoisTimesFrenteAFrenteNumCampoDivididoPelaBandeiraQuandoOSorteioTemDoisTimes()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados().ComTimesCompletos(2).Build();

        CardDoTime card = LayoutDoCard.Compor(sorteados).Should().ContainSingle().Which;

        card.Largura.Should().Be(TamanhoDoCard);
        card.Altura.Should().Be(TamanhoDoCard);
        BlocoDoCard bloco = card.Bloco;
        bloco.Times.Select(time => time.Nome.Texto).Should().Equal("LARANJA", "VERDE");
        bloco.Times.Select(time => time.NoLadoBranco).Should().Equal(true, false);
        bloco.Versus!.Texto.Texto.Should().Be("X");
        bloco.Jogadores.Should().HaveCount(12);
        bloco.Jogadores.Take(6).Should().AllSatisfy(jogador => jogador.NoLadoBranco.Should().BeTrue());
        bloco.Jogadores.Skip(6).Should().AllSatisfy(jogador => jogador.NoLadoBranco.Should().BeFalse());
    }

    // O primeiro time joga na metade branca, à esquerda; o segundo na vermelha, à direita
    [Fact]
    public void DevePorCadaTimeNaSuaMetadeQuandoOsDoisDividemOCampo()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados().ComTimesCompletos(2).Build();

        CardDoTime card = LayoutDoCard.Compor(sorteados).Should().ContainSingle().Which;

        IReadOnlyList<JogadorDoCard> jogadores = card.Bloco.Jogadores;
        jogadores.Take(6).Should().AllSatisfy(jogador => jogador.Placa.Right.Should().BeLessThanOrEqualTo(Emenda));
        jogadores.Skip(6).Should().AllSatisfy(jogador => jogador.Placa.Left.Should().BeGreaterThanOrEqualTo(Emenda));
    }

    // Cinco times, três cards; o quinto, sem colete, sozinho no terceiro
    [Fact]
    public void DeveFazerTresCardsComOQuintoTimeSozinhoQuandoOSorteioTemCincoTimes()
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(new ConstrutorDeTimesSorteados().ComTimesCompletos(5).Build());

        cards.Select(card => card.Bloco.Times.Count).Should().Equal(2, 2, 1);
        TimeNoPlacar quinto = cards[2].Bloco.Times.Should().ContainSingle().Which;
        quinto.Cor.Should().BeNull();
        quinto.Nome.Texto.Should().Be("SEM COLETE");
        cards[2].Bloco.Versus.Should().BeNull();
    }

    // Um card por campo, para os nomes continuarem grandes; cada um repete o cabeçalho
    [Theory]
    [InlineData(3, new[] { 2, 1 })]
    [InlineData(4, new[] { 2, 2 })]
    public void DeveFazerUmCardPorCampoComOMesmoCabecalhoQuandoOSorteioTemTresOuQuatroTimes(int quantidadeDeTimes, int[] timesPorCampo)
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(TimesComNomesLongos(quantidadeDeTimes), _comEscudo);

        cards.Select(card => card.Bloco.Times.Count).Should().Equal(timesPorCampo);
        cards.SelectMany(card => card.Bloco.Times).Select(time => time.Cor)
            .Should().Equal(_coresNaOrdemDoSorteio.Take(quantidadeDeTimes).Cast<CorDoTime?>());
        cards[1].Escudo.Should().BeEquivalentTo(cards[0].Escudo);
        cards[1].Titulo.Should().BeEquivalentTo(cards[0].Titulo);
        cards[1].Bloco.Campo.Should().Be(cards[0].Bloco.Campo);
    }

    // O terceiro time, sozinho no segundo campo, não tem "X"
    [Fact]
    public void DeveDeixarOTerceiroTimeSozinhoNoSegundoCampoQuandoOSorteioTemTresTimes()
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(TresTimes());

        BlocoDoCard segundo = cards[1].Bloco;
        segundo.Times.Should().ContainSingle().Which.Nome.Texto.Should().Be("AZUL");
        segundo.Versus.Should().BeNull();
        segundo.Jogadores.Should().HaveCount(6);
    }

    [Fact]
    public void DeveEscalarCadaTimeNumDoisUmDoisComOsNomesEmMaiusculasNaOrdemDoSorteio()
    {
        CardDoTime primeiro = LayoutDoCard.Compor(TresTimes())[0];

        primeiro.Bloco.Jogadores.Take(6).Select(jogador => jogador.Nome.Texto)
            .Should().Equal("🧤 NUNO", "TALES", "WILSON", "SAMUEL", "HUGO PACO", "IVO");
        primeiro.Bloco.Jogadores.Take(6).Select(jogador => jogador.Papel)
            .Should().Equal(PapelNoCard.GoleiroFixo, PapelNoCard.Linha, PapelNoCard.Linha, PapelNoCard.Linha, PapelNoCard.Linha, PapelNoCard.Linha);
    }

    // O goleiro de rodízio joga com a camisa do time e ganha o selo "RODÍZIO"
    [Fact]
    public void DeveMarcarOGoleiroComOSeloDeRodizioQuandoOTimeNaoTemGoleiroFixo()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTimeDeRodizio(CorDoTime.Laranja, "Nuno", "Ivo", "Rico", "Edu", "Mauro", "Kaio")
            .ComTimesCompletos(1)
            .Build();

        CardDoTime card = LayoutDoCard.Compor(sorteados).Should().ContainSingle().Which;

        JogadorDoCard goleiro = card.Bloco.Jogadores[0];
        goleiro.Papel.Should().Be(PapelNoCard.GoleiroDeRodizio);
        goleiro.Nome.Texto.Should().Be("🧤 NUNO");
        goleiro.Selo!.Texto.Should().Be("RODÍZIO");
        card.Bloco.Jogadores.Skip(1).Should().AllSatisfy(jogador => jogador.Selo.Should().BeNull());
    }

    // Quem falta no time incompleto aparece como vaga, e o placar diz quantos faltam
    [Fact]
    public void DeveMostrarQuantosFaltamEAsVagasQuandoOTimeEstaIncompleto()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTimesCompletos(1)
            .ComTimeIncompleto(CorDoTime.Verde, null, "Paulo", "Tomás")
            .Build();

        CardDoTime card = LayoutDoCard.Compor(sorteados).Should().ContainSingle().Which;

        TimeNoPlacar incompleto = card.Bloco.Times[1];
        incompleto.Faltam!.Texto.Texto.Should().Be("FALTAM 4");
        card.Bloco.Times[0].Faltam.Should().BeNull();
        card.Bloco.Jogadores.Skip(6).Select(jogador => jogador.Nome.Texto).Should().Equal("VAGA", "PAULO", "TOMÁS", "VAGA", "VAGA", "VAGA");
    }

    // Numa quadra só, o time sem goleiro próprio joga com os fixos do gol: 5 na linha, sem vaga no gol
    [Fact]
    public void DeveEscalarSoOsCincoDaLinhaQuandoOTimeJogaComOsGoleirosDoGol()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTimesCompletos(2)
            .ComTimeComOsGoleirosDoGol(CorDoTime.Cinza, "Paulo", "Tomás", "Elias", "Luan Ferraz", "Kiko")
            .Build();

        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(sorteados);

        BlocoDoCard segundo = cards[1].Bloco;
        segundo.Times[0].Faltam.Should().BeNull();
        segundo.Jogadores.Select(jogador => jogador.Nome.Texto).Should().Equal("PAULO", "TOMÁS", "ELIAS", "LUAN FERRAZ", "KIKO");
        segundo.Jogadores.Should().AllSatisfy(jogador => jogador.Papel.Should().Be(PapelNoCard.Linha));
    }

    // O incompleto de uma quadra só precisa só de linha; o gol fica com os fixos
    [Fact]
    public void DeveMostrarVagasSoNaLinhaQuandoOIncompletoJogaComOsGoleirosDoGol()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTimesCompletos(2)
            .ComTimeIncompletoComOsGoleirosDoGol(CorDoTime.Cinza, "Paulo", "Tomás", "Elias")
            .Build();

        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(sorteados);

        BlocoDoCard segundo = cards[1].Bloco;
        segundo.Times[0].Faltam!.Texto.Texto.Should().Be("FALTAM 2");
        segundo.Jogadores.Select(jogador => jogador.Nome.Texto).Should().Equal("PAULO", "TOMÁS", "ELIAS", "VAGA", "VAGA");
    }

    // De 4 a 9 pagos saem dois times menores, completos: a formação encolhe, sem vagas
    [Theory]
    [InlineData(4)]
    [InlineData(3)]
    [InlineData(2)]
    [InlineData(1)]
    public void DeveEscalarSoQuemJogaSemVagasQuandoOTimeCompletoTemMenosDeCincoDeLinha(int deLinha)
    {
        string[] linha = [.. Enumerable.Range(1, deLinha).Select(numero => $"Jogador {numero}")];
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTime(CorDoTime.Laranja, "Nuno", linha)
            .ComTimeDeRodizio(CorDoTime.Verde, "Cauã Luz", linha)
            .Build();

        CardDoTime card = LayoutDoCard.Compor(sorteados).Should().ContainSingle().Which;

        card.Bloco.Jogadores.Should().HaveCount(2 * (1 + deLinha));
        card.Bloco.Jogadores.Should().NotContain(jogador => jogador.Papel == PapelNoCard.Vaga);
    }

    // Identidade do grupo: sem escudo, o nome do grupo fica no lugar dele
    [Fact]
    public void DevePorONomeDoGrupoNoLugarDoEscudoQuandoOGrupoNaoTemEscudo()
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(TresTimes());

        cards.Should().AllSatisfy(card =>
        {
            card.Escudo.Should().BeNull();
            card.NomeDoGrupo!.Texto.Should().Be("⚽ PELADA ⚽");
            card.NomeDoGrupo.Limites.Bottom.Should().BeLessThanOrEqualTo(card.Titulo.Limites.Top);
        });
    }

    // Identidade do grupo: o escudo na emenda da bandeira, acima do título, e o nome dele não se repete
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void DevePorOEscudoNaEmendaAcimaDoTituloQuandoOGrupoTemEscudo(int quantidadeDeTimes)
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(TimesComNomesLongos(quantidadeDeTimes), _comEscudo);

        cards.Should().AllSatisfy(card =>
        {
            EscudoDoCard escudo = card.Escudo.Should().BeOfType<EscudoDoCard>().Which;
            escudo.Imagem.Should().Equal(1, 2, 3);
            escudo.Limites.Width.Should().Be(escudo.Limites.Height);
            (escudo.Limites.Left + (escudo.Limites.Width / 2)).Should().Be(Emenda);
            DeveEstarDentro(escudo.Limites, _tela, "o escudo");
            escudo.Limites.Bottom.Should().BeLessThanOrEqualTo(card.Titulo.Limites.Top);
            card.NomeDoGrupo.Should().BeNull();
            card.Titulo.Limites.Bottom.Should().BeLessThanOrEqualTo(card.Bloco.Times[0].Colete.Top);
        });
    }

    [Fact]
    public void DeveEncolherAFonteDaPlacaQuandoONomeEhMaisLargoQueEla()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTime(CorDoTime.Laranja, "Nuno", "Bartolomeu Albuquerque", "Ivo", "Rico", "Edu", "Mauro")
            .ComTimesCompletos(1)
            .Build();

        CardDoTime card = LayoutDoCard.Compor(sorteados).Should().ContainSingle().Which;

        IReadOnlyList<JogadorDoCard> jogadores = card.Bloco.Jogadores;
        TextoDoCard nomeLongo = jogadores[1].Nome;
        nomeLongo.Texto.Should().Be("BARTOLOMEU ALBUQUERQUE");
        nomeLongo.TamanhoDaFonte.Should().BeLessThan(jogadores[2].Nome.TamanhoDaFonte)
            .And.BeGreaterThanOrEqualTo(LayoutDoCard.TamanhoMinimoDaFonte);
        DeveEstarDentro(nomeLongo.Limites, jogadores[1].Placa, "o nome longo");
    }

    [Fact]
    public void DeveCortarONomeComReticenciasQuandoNaoCabeNemNoTamanhoMinimo()
    {
        TimesSorteados sorteados = TresTimesComPrimeiroJogador(NomeEnorme);

        CardDoTime primeiro = LayoutDoCard.Compor(sorteados)[0];

        TextoDoCard nomeCortado = primeiro.Bloco.Jogadores[1].Nome;
        nomeCortado.Texto.Should().EndWith("…");
        NomeEnorme.ToUpperInvariant().Should().StartWith(nomeCortado.Texto.TrimEnd('…'));
        nomeCortado.TamanhoDaFonte.Should().Be(LayoutDoCard.TamanhoMinimoDaFonte);
    }

    [Fact]
    public void DeveManterONomeEmUmaLinhaQuandoEleTemQuebrasDeLinha()
    {
        TimesSorteados sorteados = TresTimesComPrimeiroJogador("Hugo\nPaco\t Silva");

        CardDoTime primeiro = LayoutDoCard.Compor(sorteados)[0];

        primeiro.Bloco.Jogadores[1].Nome.Texto.Should().Be("HUGO PACO SILVA");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DeveManterTudoDentroDoCardAcimaDaFonteMinimaQuandoOsNomesSaoLongos(int quantidadeDeTimes)
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(TimesComNomesLongos(quantidadeDeTimes), _comEscudo);

        cards.SelectMany(TodosOsTextos).Should().AllSatisfy(texto =>
        {
            DeveEstarDentro(texto.Limites, _tela, $"\"{texto.Texto}\"");
            texto.TamanhoDaFonte.Should().BeGreaterThanOrEqualTo(LayoutDoCard.TamanhoMinimoDaFonte, "\"{0}\" precisa continuar legível", texto.Texto);
        });
        cards.Select(card => card.Bloco).Should().AllSatisfy(bloco =>
        {
            DeveEstarDentro(bloco.Campo, _tela, "o campo");
            bloco.Jogadores.Should().AllSatisfy(jogador =>
            {
                DeveEstarDentro(jogador.Camisa, bloco.Campo, $"a camisa de {jogador.Nome.Texto}");
                DeveEstarDentro(jogador.Placa, bloco.Campo, $"a placa de {jogador.Nome.Texto}");
                DeveEstarDentro(jogador.Nome.Limites, jogador.Placa, $"\"{jogador.Nome.Texto}\"");
            });
        });
    }

    // Cada placa fica do seu lado da bandeira, para a cor dela se ler sobre o fundo
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DeveManterCadaPlacaDoSeuLadoDaBandeiraQuandoOsNomesSaoLongos(int quantidadeDeTimes)
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(TimesComNomesLongos(quantidadeDeTimes));

        cards.SelectMany(card => card.Bloco.Jogadores).Should().AllSatisfy(jogador =>
        {
            if (jogador.NoLadoBranco)
            {
                jogador.Placa.Right.Should().BeLessThanOrEqualTo(Emenda, "a placa de {0} é do lado branco", jogador.Nome.Texto);
            }
            else
            {
                jogador.Placa.Left.Should().BeGreaterThanOrEqualTo(Emenda, "a placa de {0} é do lado vermelho", jogador.Nome.Texto);
            }
        });
    }

    // Nenhuma placa toca outra placa nem cobre a camisa de outro jogador
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DeveSepararCadaPlacaDasOutrasPlacasEDasOutrasCamisasQuandoOsNomesSaoLongos(int quantidadeDeTimes)
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(TimesComNomesLongos(quantidadeDeTimes));

        foreach (CardDoTime card in cards)
        {
            IReadOnlyList<JogadorDoCard> jogadores = card.Bloco.Jogadores;

            for (int um = 0; um < jogadores.Count; um++)
            {
                for (int outro = 0; outro < jogadores.Count; outro++)
                {
                    if (um == outro)
                    {
                        continue;
                    }

                    ComFolga(jogadores[um].Placa).IntersectsWith(ComFolga(jogadores[outro].Placa)).Should()
                        .BeFalse("a placa de {0} precisa de folga da de {1}", jogadores[um].Nome.Texto, jogadores[outro].Nome.Texto);
                    jogadores[um].Placa.IntersectsWith(CorpoDaCamisa(jogadores[outro])).Should()
                        .BeFalse("a placa de {0} não pode cobrir a camisa de {1}", jogadores[um].Nome.Texto, jogadores[outro].Nome.Texto);
                }
            }
        }
    }

    // O selo "RODÍZIO" fica no campo e não toca a placa nem a camisa, mangas incluídas, de outro jogador
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void DeveSepararOSeloDeRodizioDasOutrasPlacasEDasOutrasCamisasQuandoTodosOsTimesSaoDeRodizio(int quantidadeDeTimes)
    {
        IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(TimesDeRodizio(quantidadeDeTimes));

        foreach (BlocoDoCard bloco in cards.Select(card => card.Bloco))
        {
            IReadOnlyList<JogadorDoCard> jogadores = bloco.Jogadores;

            for (int goleiro = 0; goleiro < jogadores.Count; goleiro++)
            {
                if (jogadores[goleiro].Selo is not { } selo)
                {
                    continue;
                }

                DeveEstarDentro(selo.Limites, bloco.Campo, "o selo de rodízio");

                for (int outro = 0; outro < jogadores.Count; outro++)
                {
                    if (outro == goleiro)
                    {
                        continue;
                    }

                    selo.Limites.IntersectsWith(jogadores[outro].Placa).Should()
                        .BeFalse("o selo de {0} não pode tocar a placa de {1}", jogadores[goleiro].Nome.Texto, jogadores[outro].Nome.Texto);
                    selo.Limites.IntersectsWith(jogadores[outro].Camisa).Should()
                        .BeFalse("o selo de {0} não pode tocar a camisa de {1}", jogadores[goleiro].Nome.Texto, jogadores[outro].Nome.Texto);
                }
            }
        }
    }

    // O nome do time e o "FALTAM N" ficam por fora do colete, dentro do card
    [Fact]
    public void DevePorONomeEOSeloDeFaltamPorForaDoColeteQuandoOTimeEstaIncompleto()
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados()
            .ComTimeIncompleto(CorDoTime.Laranja, "Nuno", "Ivo")
            .ComTimeIncompleto(CorDoTime.Verde, null, "Paulo", "Tomás")
            .Build();

        CardDoTime card = LayoutDoCard.Compor(sorteados).Should().ContainSingle().Which;

        TimeNoPlacar branco = card.Bloco.Times[0];
        TimeNoPlacar vermelho = card.Bloco.Times[1];
        branco.Nome.Limites.Right.Should().BeLessThanOrEqualTo(branco.Colete.Left);
        branco.Faltam!.Limites.Right.Should().BeLessThanOrEqualTo(branco.Colete.Left);
        vermelho.Nome.Limites.Left.Should().BeGreaterThanOrEqualTo(vermelho.Colete.Right);
        vermelho.Faltam!.Limites.Left.Should().BeGreaterThanOrEqualTo(vermelho.Colete.Right);
        branco.Nome.Limites.Bottom.Should().BeLessThanOrEqualTo(branco.Faltam.Limites.Top + 2);
    }

    // O card vai de 1 a 5 times; acima de 5 o grupo recebe os times só como texto
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void DeveRecusarOSorteioQuandoOCardNaoComportaSeusTimes(int quantidadeDeTimes)
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados().ComTimesCompletos(quantidadeDeTimes).Build();

        Action compor = () => LayoutDoCard.Compor(sorteados);

        compor.Should().Throw<ArgumentException>();
    }

    // O caso de uso pergunta antes de compor, para 6 times não virar erro
    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void DeveDizerSeOCardComportaOSorteioQuandoPerguntado(int quantidadeDeTimes, bool comporta)
    {
        TimesSorteados sorteados = new ConstrutorDeTimesSorteados().ComTimesCompletos(quantidadeDeTimes).Build();

        LayoutDoCard.Comporta(sorteados).Should().Be(comporta);
    }

    private static TimesSorteados TresTimes() =>
        new ConstrutorDeTimesSorteados()
            .ComTime(CorDoTime.Cinza, "Nuno", "Tales", "Wilson", "Samuel", "Hugo Paco", "Ivo")
            .ComTime(CorDoTime.Laranja, "Cauã Luz", "Rico", "Edu", "Mauro", "Ciro", "Kaio")
            .ComTimeIncompleto(CorDoTime.Azul, null, "Flavio", "Leandro", "Duda Neri", "Sergio", "Alvaro")
            .Build();

    /// <summary>Três times, o primeiro com <paramref name="nome"/> como primeiro jogador de linha.</summary>
    private static TimesSorteados TresTimesComPrimeiroJogador(string nome) =>
        new ConstrutorDeTimesSorteados()
            .ComTime(CorDoTime.Laranja, "Nuno", nome, "Ivo", "Rico", "Edu", "Mauro")
            .ComTimesCompletos(2)
            .Build();

    /// <summary>Times completos de nomes longos: o máximo que o card já precisou comportar.</summary>
    private static TimesSorteados TimesComNomesLongos(int quantidadeDeTimes)
    {
        ConstrutorDeTimesSorteados construtor = new();

        for (int indice = 0; indice < quantidadeDeTimes; indice++)
        {
            construtor.ComTime(_coresNaOrdemDoSorteio[indice], "Marcão Gusmão Albuquerque", _nomesLongos);
        }

        return construtor.Build();
    }

    /// <summary>Times sem goleiro fixo, cada um com o seu goleiro de rodízio e nomes de tamanho comum.</summary>
    private static TimesSorteados TimesDeRodizio(int quantidadeDeTimes)
    {
        ConstrutorDeTimesSorteados construtor = new();

        for (int indice = 0; indice < quantidadeDeTimes; indice++)
        {
            construtor.ComTimeDeRodizio(
                _coresNaOrdemDoSorteio[indice], "Paulo Reis", "Gael Rocha", "Breno", "Renzo P", "Elias", "Kiko Mendes");
        }

        return construtor.Build();
    }

    private static IEnumerable<TextoDoCard> TodosOsTextos(CardDoTime card)
    {
        List<TextoDoCard?> textos = [card.NomeDoGrupo, card.Titulo, card.Bloco.Versus?.Texto];

        textos.AddRange(card.Bloco.Times.SelectMany(time => new[] { time.Nome, time.Faltam?.Texto }));
        textos.AddRange(card.Bloco.Jogadores.SelectMany(jogador => new[] { jogador.Nome, jogador.Selo }));

        return textos.OfType<TextoDoCard>();
    }

    /// <summary>
    /// A placa com metade da folga mínima entre placas em volta: duas placas com folga não se tocam nem com a sombra
    /// .
    /// </summary>
    private static RectangleF ComFolga(RectangleF placa) =>
        new(placa.X - (FolgaEntrePlacas / 2), placa.Y - (FolgaEntrePlacas / 2), placa.Width + FolgaEntrePlacas, placa.Height + FolgaEntrePlacas);

    /// <summary>O tronco da camisa, sem as pontas das mangas, que podem passar por baixo de uma placa vizinha.</summary>
    private static RectangleF CorpoDaCamisa(JogadorDoCard jogador)
    {
        RectangleF camisa = jogador.Camisa;

        return new RectangleF(camisa.Left + (camisa.Width * 0.26f), camisa.Top + (camisa.Height * 0.04f), camisa.Width * 0.48f, camisa.Height * 0.95f);
    }

    private static void DeveEstarDentro(RectangleF limites, RectangleF area, string oque)
    {
        limites.Left.Should().BeGreaterThanOrEqualTo(area.Left, "{0} começa dentro", oque);
        limites.Top.Should().BeGreaterThanOrEqualTo(area.Top, "{0} começa dentro", oque);
        limites.Right.Should().BeLessThanOrEqualTo(area.Right, "{0} termina dentro", oque);
        limites.Bottom.Should().BeLessThanOrEqualTo(area.Bottom, "{0} termina dentro", oque);
    }
}
