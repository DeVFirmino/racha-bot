using System.Globalization;
using PeladaBot.Listas;
using PeladaBot.Respostas;
using PeladaBot.Sorteio;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Respostas;

public sealed class LegendaDoSorteioTestes
{
    private const int SementeDaPrimeiraFrase = 14;
    private const int QuantidadeDeSementes = 100;
    private const string Frase = "⏰ *Por favor, não se atrasem, galera!*";

    /// <summary>O tema "Champions da pelada" do grupo, com os apelidos de todas as cores.</summary>
    private static readonly ModeloDaLegenda _champions = new()
    {
        Cabecalho = "🏆 *PELADA | CHAMPIONS DA PELADA*",
        LinhaDoHorario = "{relogio} *Sorteio das {hora}*",
        ApelidoCinza = "OS GALÁCTICOS DO BAIRRO",
        ApelidoAzul = "A ELITE DO PASSE ERRADO",
        ApelidoVerde = "OS DONOS DA RESENHA",
        ApelidoLaranja = "OS REIS DO CHUTÃO",
        ApelidoSemColete = "VAGA NA REPESCAGEM",
    };

    /// <summary>Frases de teste, já com a marcação do WhatsApp. Em produção vêm da configuração.</summary>
    private static readonly IReadOnlyList<string> _frasesDoRodape =
    [
        Frase,
        "⏰ *Chega uns 10 min antes pra dar tempo de aquecer.*",
        "⏰ *Pontualidade, rapaziada. Bola rola no horário!*",
        "⏰ *Sem atraso hoje, hein!*",
        "⏰ *Bora chegar cedo, ninguém gosta de esperar.*",
    ];

    /// <summary>21:04 no fuso da suíte, no horário de verão (UTC+2).</summary>
    private static readonly DateTimeOffset _sorteadoEm = new(2026, 10, 12, 19, 4, 0, TimeSpan.Zero);

    /// <summary>12/10 às 13:29 no fuso da suíte.</summary>
    private static readonly DateTimeOffset _listaEnviadaEm = new(2026, 10, 12, 11, 29, 0, TimeSpan.Zero);

    // O modelo "Champions da pelada", fechando com uma das resenhas
    [Fact]
    public void DeveCombinarComOModeloDoGrupoQuandoOComandoCitouALista()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("Cauã Luz", "Téo Vargas")
            .ComJogadoresPagos("Bento Cruz", "Nico Braga", "Iago Leme", "Duda Neri", "Enzo Paiva", "Gael Moura", "Milo Castro", "Hélio Pires")
            .ComJogadoresPagos("Heitor Dias", "Luca P 22", "Otto Klein", "Ruy Pacheco", "Luan Ferraz", "Nilo Bastos", "Renzo M", "Cael Prado")
            .Build();
        TimesSorteados sorteados = new()
        {
            Times =
            [
                TimeCom(1, CorDoTime.Cinza, "Cauã Luz", "Bento Cruz", "Nico Braga", "Iago Leme", "Duda Neri", "Enzo Paiva"),
                TimeCom(2, CorDoTime.Azul, "Téo Vargas", "Gael Moura", "Milo Castro", "Hélio Pires", "Heitor Dias", "Luca P 22"),
                TimeDeRodizioCom(3, CorDoTime.Verde, "Otto Klein", "Ruy Pacheco", "Luan Ferraz", "Nilo Bastos", "Renzo M", "Cael Prado"),
            ],
            IgnorandoPendentes = [],
        };
        EntradaDaLegendaDoSorteio entrada = Entrada(sorteados, lista, origem: null) with
        {
            SorteadoEm = Instante("2026-10-12T14:42:00+00:00"),
            FrasesDoRodape =
            [
                "⚽ *Hoje tem jogo. A bola não espera, e o apito também não.*",
            ],
        };

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be("""
            🏆 *PELADA | CHAMPIONS DA PELADA*
            🕓 *Sorteio das 16:42*
            ━━━━━━━━━━━━━━━━━━
            ⚪ *CINZA | OS GALÁCTICOS DO BAIRRO*
            ───────────────
            🧤 Cauã Luz
            👕 Bento Cruz
            👕 Nico Braga
            👕 Iago Leme
            👕 Duda Neri
            👕 Enzo Paiva

            🔵 *AZUL | A ELITE DO PASSE ERRADO*
            ───────────────
            🧤 Téo Vargas
            👕 Gael Moura
            👕 Milo Castro
            👕 Hélio Pires
            👕 Heitor Dias
            👕 Luca P 22

            🟢 *VERDE | OS DONOS DA RESENHA*
            ───────────────
            🧤 Otto Klein (rodízio)
            👕 Ruy Pacheco
            👕 Luan Ferraz
            👕 Nilo Bastos
            👕 Renzo M
            👕 Cael Prado
            ━━━━━━━━━━━━━━━━━━
            ⚽ *Hoje tem jogo. A bola não espera, e o apito também não.*
            """);
    }

    // Numa quadra só, o 3º time, incompleto, joga com os fixed goalkeepers do gol
    [Fact]
    public void DeveMostrarAOrigemEOsAvisosAbaixoDoHorarioQuandoAListaNaoFoiCitada()
    {
        EntradaDaLegendaDoSorteio entrada = AmostraDaSpec(EnviadaPor("Tales"));

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be("""
            🏆 *PELADA | CHAMPIONS DA PELADA*
            🕘 *Sorteio das 21:04*
            📋 Lista enviada por Tales em 12/10 às 13:29
            ⚠️ Téo Vargas estava em duas seções, ficou como goleiro
            ━━━━━━━━━━━━━━━━━━
            🟢 *VERDE | OS DONOS DA RESENHA*
            ───────────────
            🧤 Nuno
            👕 Tales
            👕 Wilson
            👕 Samuel
            👕 Hugo Paco
            👕 Ivo

            ⚪ *CINZA | OS GALÁCTICOS DO BAIRRO*
            ───────────────
            🧤 Cauã Luz
            👕 Rico
            👕 Edu
            👕 Mauro
            👕 Ciro
            👕 Kaio

            🟠 *LARANJA | OS REIS DO CHUTÃO* (faltam 3)
            ───────────────
            👕 Paulo
            👕 Tomás
            ━━━━━━━━━━━━━━━━━━
            ⏰ *Por favor, não se atrasem, galera!*
            """);
    }

    [Fact]
    public void DeveDeixarDeForaALinhaDeOrigemQuandoOComandoCitouALista()
    {
        string legenda = LegendaDoSorteio.Compor(AmostraDaSpec(origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().NotContain("📋").And.Contain("""
            🕘 *Sorteio das 21:04*
            ⚠️ Téo Vargas estava em duas seções, ficou como goleiro
            ━━━━━━━━━━━━━━━━━━
            """);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeveMostrarSoADataEHoraDaListaQuandoONomeDoRemetenteEhDesconhecido(string? nomeDoRemetente)
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), EnviadaPor(nomeDoRemetente));

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be(LegendaDeDoisTimesDeDois("📋 Lista de 12/10 às 13:29"));
    }

    // "{relogio}" vira o relógio da hora do sorteio no fuso da suíte
    [Theory]
    [InlineData("2026-10-12T22:30:00+00:00", "🕛 *Sorteio das 00:30*")]
    [InlineData("2026-10-12T11:05:00+00:00", "🕐 *Sorteio das 13:05*")]
    [InlineData("2026-10-12T14:42:00+00:00", "🕓 *Sorteio das 16:42*")]
    [InlineData("2026-10-12T19:04:00+00:00", "🕘 *Sorteio das 21:04*")]
    public void DeveMostrarORelogioDaHoraDoSorteioQuandoOModeloPedeORelogio(string sorteadoEm, string segundaLinha)
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with { SorteadoEm = Instante(sorteadoEm) };

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        Linhas(legenda)[1].Should().Be(segundaLinha);
    }

    // Sem "{relogio}", a linha do horário fica com o emoji do tema
    [Fact]
    public void DeveManterOEmojiDoModeloQuandoALinhaDoHorarioNaoTemRelogio()
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with
        {
            Modelos = [Modelo("💶 *PELADA | MERCADO EUROPEU*", "⚽ *Escalação das {hora}*")],
        };

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        Linhas(legenda)[..2].Should().Equal("💶 *PELADA | MERCADO EUROPEU*", "⚽ *Escalação das 21:04*");
    }

    // Um tema por sorteio, escolhido ao acaso
    [Fact]
    public void DeveSortearUmDosModelosQuandoOsTimesSaoSorteadosVariasVezes()
    {
        ModeloDaLegenda[] modelos =
        [
            _champions,
            Modelo("💶 *PELADA | MERCADO EUROPEU*", "⚽ *Escalação das {hora}*"),
            Modelo("🍻 *PELADA | RUMO AO TERCEIRO TEMPO*", "⚽ *Times sorteados — {hora}*"),
        ];
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with { Modelos = modelos };

        string[] cabecalhos = [.. Enumerable.Range(0, QuantidadeDeSementes).Select(semente => Linhas(LegendaDoSorteio.Compor(entrada, new Random(semente)))[0])];

        cabecalhos.Distinct().Should().BeEquivalentTo(modelos.Select(modelo => modelo.Cabecalho));
    }

    // Cada cor ganha o apelido do tema; do 5º time em diante, "SEM COLETE"
    [Fact]
    public void DeveDarOApelidoDoModeloACadaCorQuandoHaCincoTimes()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros("A", "B", "C", "D").ComJogadoresPagos("E", "F", "G", "H", "I").Build();
        TimesSorteados sorteados = new()
        {
            Times =
            [
                TimeCom(1, CorDoTime.Cinza, "A", "E"),
                TimeCom(2, CorDoTime.Azul, "B", "F"),
                TimeCom(3, CorDoTime.Verde, "C", "G"),
                TimeCom(4, CorDoTime.Laranja, "D", "H"),
                TimeIncompletoCom(5, cor: null, goleiroFixo: null, "I"),
            ],
            IgnorandoPendentes = [],
        };

        string[] linhas = Linhas(LegendaDoSorteio.Compor(Entrada(sorteados, lista, origem: null), new Random(SementeDaPrimeiraFrase)));

        linhas.Where(linha => linha.EndsWith("*", StringComparison.Ordinal) && linha.Contains(" | ", StringComparison.Ordinal)).Skip(1)
            .Should().Equal(
                "⚪ *CINZA | OS GALÁCTICOS DO BAIRRO*",
                "🔵 *AZUL | A ELITE DO PASSE ERRADO*",
                "🟢 *VERDE | OS DONOS DA RESENHA*",
                "🟠 *LARANJA | OS REIS DO CHUTÃO*");
        linhas.Should().Contain("⚫ *SEM COLETE | VAGA NA REPESCAGEM* (faltam 5)");
    }

    [Fact]
    public void DeveEsconderEsperaQuandoNinguemEstaNaEspera()
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null);

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be(LegendaDeDoisTimesDeDois());
    }

    // O goleiro de rodízio aparece com "(rodízio)" na legenda
    [Fact]
    public void DeveMostrarRodizioNaLegendaQuandoOGoleiroNaoEhFixo()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("Nuno")
            .ComJogadoresPagos("Nico Braga", "Duda Neri", "Cael Prado", "Kaio", "Paulo", "Rico", "Edu", "Mauro", "Ciro", "Tomás")
            .Build();
        TimesSorteados sorteados = new()
        {
            Times =
            [
                TimeCom(1, CorDoTime.Laranja, "Nuno", "Nico Braga", "Duda Neri", "Cael Prado", "Kaio", "Paulo"),
                TimeDeRodizioCom(2, CorDoTime.Verde, "Rico", "Edu", "Mauro", "Ciro", "Tomás"),
            ],
            IgnorandoPendentes = [],
        };

        string legenda = LegendaDoSorteio.Compor(Entrada(sorteados, lista, origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().Contain("🧤 Rico (rodízio)");
    }

    // O time incompleto mostra "(faltam N)" no cabeçalho, sem "(sem goleiro)" e sem "*Reservas*"
    [Fact]
    public void DeveMostrarFaltamNoCabecalhoQuandoOTimeEstaIncompleto()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("Nuno")
            .ComJogadoresPagos("Nico Braga", "Duda Neri", "Cael Prado", "Kaio", "Paulo", "Rico", "Edu")
            .Build();
        TimesSorteados sorteados = new()
        {
            Times =
            [
                TimeCom(1, CorDoTime.Laranja, "Nuno", "Nico Braga", "Duda Neri", "Cael Prado", "Kaio", "Paulo"),
                TimeIncompletoCom(2, CorDoTime.Cinza, null, "Rico", "Edu"),
            ],
            IgnorandoPendentes = [],
        };

        string legenda = LegendaDoSorteio.Compor(Entrada(sorteados, lista, origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().Contain("⚪ *CINZA | OS GALÁCTICOS DO BAIRRO* (faltam 4)").And.NotContain("sem goleiro").And.NotContain("*Reservas*");
    }

    // O goleiro fixo que sobra vai para o gol do time incompleto
    [Fact]
    public void DeveColocarOGoleiroExtraNoTimeIncompletoQuandoHaMaisGoleirosQueTimes()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("Nuno", "Cauã Luz", "Dico")
            .ComJogadoresPagos("Nico Braga", "Duda Neri", "Cael Prado", "Kaio", "Paulo", "Rico", "Edu", "Mauro", "Ciro", "Tales", "Wilson", "Tomás")
            .Build();
        TimesSorteados sorteados = new()
        {
            Times =
            [
                TimeCom(1, CorDoTime.Laranja, "Nuno", "Nico Braga", "Duda Neri", "Cael Prado", "Kaio", "Paulo"),
                TimeCom(2, CorDoTime.Cinza, "Cauã Luz", "Rico", "Edu", "Mauro", "Ciro", "Tales"),
                TimeIncompletoCom(3, CorDoTime.Verde, "Dico", "Wilson", "Tomás"),
            ],
            IgnorandoPendentes = [],
        };

        string legenda = LegendaDoSorteio.Compor(Entrada(sorteados, lista, origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().Contain("""
            🟢 *VERDE | OS DONOS DA RESENHA* (faltam 3)
            ───────────────
            🧤 Dico
            👕 Wilson
            👕 Tomás
            ━━━━━━━━━━━━━━━━━━
            """);
    }

    // O apelido de cada time é sorteado entre os de todos os temas e os avulsos da sua cor, então um
    // sorteio mistura apelidos de temas diferentes
    [Fact]
    public void DeveSortearOApelidoEntreOsDeTodosOsTemasEOsAvulsosQuandoHaVariasOpcoes()
    {
        ModeloDaLegenda outroTema = _champions with { ApelidoLaranja = "PRIMEIRO TEMPO", ApelidoVerde = "SEGUNDO TEMPO" };
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with
        {
            Modelos = [_champions, outroTema],
            ApelidosAvulsos = ApelidosAvulsos.Nenhum with { Laranja = ["ATLÉTICO LARANJA"], Verde = ["SPORTING VERDE"] },
        };
        string[] cabecalhosDosTimes =
        [
            .. Enumerable.Range(0, 200)
                .SelectMany(semente => LegendaDoSorteio.Compor(entrada, new Random(semente)).Split('\n'))
                .Where(linha => linha.StartsWith("🟠 *LARANJA | ", StringComparison.Ordinal) || linha.StartsWith("🟢 *VERDE | ", StringComparison.Ordinal)),
        ];

        cabecalhosDosTimes.Distinct().Should().BeEquivalentTo(
            "🟠 *LARANJA | " + _champions.ApelidoLaranja + "*",
            "🟠 *LARANJA | PRIMEIRO TEMPO*",
            "🟠 *LARANJA | ATLÉTICO LARANJA*",
            "🟢 *VERDE | " + _champions.ApelidoVerde + "*",
            "🟢 *VERDE | SEGUNDO TEMPO*",
            "🟢 *VERDE | SPORTING VERDE*");
    }

    // O goleiro fixo que sobra com todos os times fechados aparece depois deles, revezando no gol
    [Fact]
    public void DeveMostrarQuemRevezaNoGolDepoisDosTimesQuandoSobraGoleiroFixo()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("Nuno", "Cauã Luz", "Dico")
            .ComJogadoresPagos("Nico Braga", "Duda Neri", "Cael Prado", "Kaio", "Paulo", "Rico", "Edu", "Mauro", "Ciro", "Tales")
            .Build();
        TimesSorteados sorteados = new()
        {
            Times =
            [
                TimeCom(1, CorDoTime.Laranja, "Nuno", "Nico Braga", "Duda Neri", "Cael Prado", "Kaio", "Paulo"),
                TimeCom(2, CorDoTime.Cinza, "Cauã Luz", "Rico", "Edu", "Mauro", "Ciro", "Tales"),
            ],
            IgnorandoPendentes = [],
            RevezandoNoGol = ["Dico"],
        };

        string legenda = LegendaDoSorteio.Compor(Entrada(sorteados, lista, origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().Contain("""
            👕 Tales

            🧤 *Revezando no gol:* Dico
            ━━━━━━━━━━━━━━━━━━
            """);
    }

    // A legenda nunca mostra "Não pagou ainda" - o sorteio só sai quando todo mundo da
    // lista já pagou
    [Fact]
    public void NaoDeveMostrarQuemNaoPagouMesmoQuandoAListaTemAlguemSemAMarca()
    {
        ListaDoGrupo lista = QuatroPagosEDoisGoleiros().ComJogadoresNaoPagos("Paco", "Luan Ferraz").Build();

        string legenda = LegendaDoSorteio.Compor(Entrada(DoisTimesDeDois(), lista, origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().NotContain("Não pagou ainda").And.NotContain("Paco").And.NotContain("Luan Ferraz");
    }

    // "!sortear pagos" avisa quem foi sorteado sem ✅
    [Fact]
    public void DeveAvisarQuemFoiIgnoradoQuandoSomentePagos()
    {
        EntradaDaLegendaDoSorteio entrada =
            Entrada(DoisTimesDeDois() with { IgnorandoPendentes = ["Paco", "Luan Ferraz"] }, QuatroPagosEDoisGoleiros().Build(), origem: null);

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be(LegendaDeDoisTimesDeDois("⚠️ Sorteado sem: Paco, Luan Ferraz (sem ✅)"));
    }

    // Quem está na espera sem ✅ ainda espera uma vaga e não aparece na legenda
    [Fact]
    public void NaoDeveMostrarQuemEstaNaEsperaSemPagarQuandoAListaTemEspera()
    {
        ListaDoGrupo lista = QuatroPagosEDoisGoleiros().ComEspera("Davi", "Hélio Pires").Build();

        string legenda = LegendaDoSorteio.Compor(Entrada(DoisTimesDeDois(), lista, origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be(LegendaDeDoisTimesDeDois());
    }

    [Theory]
    [MemberData(nameof(AvisosESuasLinhas))]
    public void DeveAvisarLogoDepoisDoCabecalhoQuandoAListaTemUmAviso(AvisoDaLista aviso, string linhaDoAviso)
    {
        ListaDoGrupo lista = QuatroPagosEDoisGoleiros().ComAvisos(aviso).Build();

        string legenda = LegendaDoSorteio.Compor(Entrada(DoisTimesDeDois(), lista, origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be(LegendaDeDoisTimesDeDois(linhaDoAviso));
    }

    [Fact]
    public void DeveMostrarALinhaDeOrigemEDepoisUmAvisoPorLinhaQuandoAListaTemVariosAvisos()
    {
        ListaDoGrupo lista = QuatroPagosEDoisGoleiros()
            .ComJogadoresNaoPagos("Paco")
            .ComAvisos(
                new NomeEmDuasSecoes { Nome = "Nuno", MantidoEm = SecaoDaLista.Goleiros },
                new NomeRepetidoComMarcaDiferente { Nome = "Paco" })
            .Build();

        string legenda = LegendaDoSorteio.Compor(Entrada(DoisTimesDeDois(), lista, EnviadaPor("Tales")), new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be(LegendaDeDoisTimesDeDois(
            "📋 Lista enviada por Tales em 12/10 às 13:29",
            "⚠️ Nuno estava em duas seções, ficou como goleiro",
            "⚠️ Paco aparece duas vezes na lista (valeu a primeira)"));
    }

    [Theory]
    [InlineData("2026-03-29T00:59:00+00:00", "01:59")] // último minuto do horário de inverno, UTC+1
    [InlineData("2026-03-29T01:00:00+00:00", "03:00")] // começa o horário de verão, UTC+2
    [InlineData("2026-10-25T00:59:00+00:00", "02:59")] // último minuto do horário de verão
    [InlineData("2026-10-25T01:00:00+00:00", "02:00")] // volta ao horário de inverno
    [InlineData("2026-10-13T00:04:00+05:00", "21:04")] // 19:04 UTC, escrito com outro offset
    public void DeveMostrarOHorarioDoFusoQuandoEhPertoDeUmaMudancaDeHorario(string sorteadoEm, string horarioDoFuso)
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with { SorteadoEm = Instante(sorteadoEm) };

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        Linhas(legenda)[1].Should().EndWith($" *Sorteio das {horarioDoFuso}*");
    }

    [Theory]
    [InlineData("2026-03-28T23:30:00+00:00", "29/03 às 00:30")] // horário de inverno: já é o dia seguinte no fuso da suíte
    [InlineData("2026-03-29T23:30:00+00:00", "30/03 às 01:30")] // horário de verão
    [InlineData("2026-10-24T23:30:00+00:00", "25/10 às 01:30")] // horário de verão, horas antes de terminar
    [InlineData("2026-10-25T23:30:00+00:00", "26/10 às 00:30")] // horário de inverno de novo
    public void DeveMostrarADataEHoraDoFusoDaListaQuandoFoiEnviadaPertoDeUmaMudancaDeHorario(string enviadaEm, string dataEHoraDoFuso)
    {
        OrigemDaLista origem = new() { NomeDoRemetente = "Tales", EnviadaEm = Instante(enviadaEm) };

        string legenda = LegendaDoSorteio.Compor(Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem), new Random(SementeDaPrimeiraFrase));

        legenda.Should().Be(LegendaDeDoisTimesDeDois($"📋 Lista enviada por Tales em {dataEHoraDoFuso}"));
    }

    [Fact]
    public void DeveNeutralizarAMarcacaoDoWhatsAppQuandoUmNomeATem()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("_Cauã Luz_", "Nuno", "*Dico*")
            .ComJogadoresPagos("*Ivo*", "Nico Braga", "~Milo Castro~", "Lia_Neri")
            .ComEspera("~Davi~")
            .ComAvisos(new NomeRepetidoComMarcaDiferente { Nome = "*Ivo*" })
            .Build();
        TimesSorteados sorteados = new()
        {
            Times =
            [
                TimeCom(1, CorDoTime.Laranja, "_Cauã Luz_", "*Ivo*", "Nico Braga"),
                TimeCom(2, CorDoTime.Verde, "Nuno", "~Milo Castro~", "Lia_Neri"),
                TimeIncompletoCom(3, CorDoTime.Cinza, "*Dico*"),
            ],
            IgnorandoPendentes = [],
        };

        string legenda = LegendaDoSorteio.Compor(Entrada(sorteados, lista, EnviadaPor("*Tales*")), new Random(SementeDaPrimeiraFrase));

        // ∗ (U+2217), ˍ (U+02CD) e ∼ (U+223C) substituem *, _ e ~ em nomes: o WhatsApp não lê nenhum deles como marcação.
        legenda.Should().Be("""
            🏆 *PELADA | CHAMPIONS DA PELADA*
            🕘 *Sorteio das 21:04*
            📋 Lista enviada por ∗Tales∗ em 12/10 às 13:29
            ⚠️ ∗Ivo∗ aparece duas vezes na lista (valeu a primeira)
            ━━━━━━━━━━━━━━━━━━
            🟠 *LARANJA | OS REIS DO CHUTÃO*
            ───────────────
            🧤 ˍCauã Luzˍ
            👕 ∗Ivo∗
            👕 Nico Braga

            🟢 *VERDE | OS DONOS DA RESENHA*
            ───────────────
            🧤 Nuno
            👕 ∼Milo Castro∼
            👕 LiaˍNeri

            ⚪ *CINZA | OS GALÁCTICOS DO BAIRRO* (faltam 5)
            ───────────────
            🧤 ∗Dico∗
            ━━━━━━━━━━━━━━━━━━
            ⏰ *Por favor, não se atrasem, galera!*
            """);
    }

    // Uma das frases, sorteada, fecha a legenda depois do separador
    [Fact]
    public void DeveSortearUmaDasFrasesConfiguradasNoFimQuandoOsTimesSaoSorteados()
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with { FrasesDoRodape = _frasesDoRodape };

        string[] ultimasLinhas = [.. Enumerable.Range(0, QuantidadeDeSementes).Select(semente => Linhas(LegendaDoSorteio.Compor(entrada, new Random(semente)))[^1])];

        ultimasLinhas.Distinct().Should().BeEquivalentTo(_frasesDoRodape);
    }

    // A resenha do rodapé vai como está na configuração, em quantas linhas tiver
    [Fact]
    public void DeveEscreverAResenhaComoEstaNaConfiguracaoQuandoElaTemDuasLinhas()
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with
        {
            FrasesDoRodape = ["🍺 *Terceiro tempo garantido!* 🍻\n\n⏰ *Cheguem no horário!* ⚽"],
        };

        string legenda = LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        legenda.Should().EndWith("""
            👕 Kaio
            ━━━━━━━━━━━━━━━━━━
            🍺 *Terceiro tempo garantido!* 🍻

            ⏰ *Cheguem no horário!* ⚽
            """);
    }

    [Fact]
    public void DeveRecusarComporQuandoNaoHaFraseDeRodape()
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with { FrasesDoRodape = [] };

        Action compor = () => LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        compor.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeveRecusarComporQuandoNaoHaModelo()
    {
        EntradaDaLegendaDoSorteio entrada = Entrada(DoisTimesDeDois(), QuatroPagosEDoisGoleiros().Build(), origem: null) with { Modelos = [] };

        Action compor = () => LegendaDoSorteio.Compor(entrada, new Random(SementeDaPrimeiraFrase));

        compor.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeveMostrarCadaPessoaUmaVezQuandoOsTimesVemDoMotorDoSorteio()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(3).ComJogadoresPagos(12).ComEspera(2).Build();
        TimesSorteados sorteados = MotorDoSorteio.Sortear(lista, new Random(SementeDaPrimeiraFrase)).Should().BeOfType<TimesSorteados>().Which;

        string[] linhas = Linhas(LegendaDoSorteio.Compor(Entrada(sorteados, lista, origem: null), new Random(SementeDaPrimeiraFrase)));

        linhas.Where(linha => linha.StartsWith("🧤 Goleiro", StringComparison.Ordinal)).Should().BeEquivalentTo("🧤 Goleiro 1", "🧤 Goleiro 2", "🧤 Goleiro 3");
        linhas.Where(linha => linha.StartsWith("👕 ", StringComparison.Ordinal)).Select(linha => linha["👕 ".Length..])
            .Should().BeEquivalentTo(NomesPagos(lista));
        linhas.Should().NotContain(linha => linha.Contains("Espera", StringComparison.Ordinal));
    }

    // O time que joga com os fixos do gol não ganha aviso nem "(faltam)"
    [Fact]
    public void NaoDeveAvisarDosGoleirosDoGolQuandoUmTimeJogaComOsDoGol()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(15).Build();
        TimesSorteados sorteados = MotorDoSorteio.Sortear(lista, new Random(SementeDaPrimeiraFrase)).Should().BeOfType<TimesSorteados>().Which;

        string legenda = LegendaDoSorteio.Compor(Entrada(sorteados, lista, origem: null), new Random(SementeDaPrimeiraFrase));

        legenda.Should().NotContain("ficam no gol").And.NotContain("(faltam");
    }

    public static TheoryData<AvisoDaLista, string> AvisosESuasLinhas() => new()
    {
        { new NomeEmDuasSecoes { Nome = "Nuno", MantidoEm = SecaoDaLista.Goleiros }, "⚠️ Nuno estava em duas seções, ficou como goleiro" },
        { new NomeEmDuasSecoes { Nome = "Nico Braga", MantidoEm = SecaoDaLista.Jogadores }, "⚠️ Nico Braga estava em duas seções, ficou na linha" },
        { new NomeRepetidoComMarcaDiferente { Nome = "Cael Prado" }, "⚠️ Cael Prado aparece duas vezes na lista (valeu a primeira)" },
    };

    /// <summary>
    /// O exemplo de legenda de "Respostas" na spec: 12 de linha e 2 goleiros fixos, 2 times completos e, no
    /// lugar das antigas Reservas, 1 time incompleto. Téo Vargas, no aviso, também não está entre os goleiros do
    /// exemplo: a legenda mostra os avisos como a lista os dá.
    /// </summary>
    private static EntradaDaLegendaDoSorteio AmostraDaSpec(OrigemDaLista? origem)
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("Nuno", "Cauã Luz")
            .ComJogadoresPagos("Tales", "Wilson", "Samuel", "Hugo Paco", "Ivo", "Rico", "Edu", "Mauro", "Ciro", "Kaio", "Paulo", "Tomás")
            .ComEspera("Davi")
            .ComAvisos(new NomeEmDuasSecoes { Nome = "Téo Vargas", MantidoEm = SecaoDaLista.Goleiros })
            .Build();
        TimesSorteados sorteados = new()
        {
            Times =
            [
                TimeCom(1, CorDoTime.Verde, "Nuno", "Tales", "Wilson", "Samuel", "Hugo Paco", "Ivo"),
                TimeCom(2, CorDoTime.Cinza, "Cauã Luz", "Rico", "Edu", "Mauro", "Ciro", "Kaio"),
                TimeIncompletoComOsGoleirosDoGol(3, CorDoTime.Laranja, "Paulo", "Tomás"),
            ],
            IgnorandoPendentes = [],
        };

        return Entrada(sorteados, lista, origem);
    }

    /// <summary>Nuno e Cauã Luz no gol; Nico Braga, Duda Neri, Cael Prado e Kaio pagos.</summary>
    private static ConstrutorDeListaDoGrupo QuatroPagosEDoisGoleiros() => new ConstrutorDeListaDoGrupo()
        .ComGoleiros("Nuno", "Cauã Luz")
        .ComJogadoresPagos("Nico Braga", "Duda Neri", "Cael Prado", "Kaio");

    /// <summary>Laranja com Nuno, Nico Braga e Duda Neri; Verde com Cauã Luz, Cael Prado e Kaio. Dois times completos, sem incompleto.</summary>
    private static TimesSorteados DoisTimesDeDois() => new()
    {
        Times =
        [
            TimeCom(1, CorDoTime.Laranja, "Nuno", "Nico Braga", "Duda Neri"),
            TimeCom(2, CorDoTime.Verde, "Cauã Luz", "Cael Prado", "Kaio"),
        ],
        IgnorandoPendentes = [],
    };

    private static Time TimeCom(int numero, CorDoTime? cor, string? goleiro, params string[] jogadoresDeLinha) =>
        TimeCom(numero, cor, goleiro, goleiroDeRodizio: false, completo: true, jogadoresDeLinha);

    /// <summary>Um time incompleto: o que sobrou no fim do sorteio, com ou sem um goleiro fixo remanescente.</summary>
    private static Time TimeIncompletoCom(int numero, CorDoTime? cor, string? goleiroFixo, params string[] jogadoresDeLinha) =>
        TimeCom(numero, cor, goleiroFixo, goleiroDeRodizio: false, completo: false, jogadoresDeLinha);

    /// <summary>O incompleto de uma quadra só: os fixed goalkeepers ficam no gol, então só falta gente na linha.</summary>
    private static Time TimeIncompletoComOsGoleirosDoGol(int numero, CorDoTime? cor, params string[] jogadoresDeLinha) =>
        TimeCom(numero, cor, goleiro: null, goleiroDeRodizio: false, completo: false, jogadoresDeLinha) with
        {
            JogaComOsGoleirosDoGol = true,
            Faltam = 5 - jogadoresDeLinha.Length,
        };

    /// <summary>Um time completo cujo goleiro é de rodízio, tirado dos próprios jogadores de linha.</summary>
    private static Time TimeDeRodizioCom(int numero, CorDoTime? cor, string goleiro, params string[] jogadoresDeLinha) =>
        TimeCom(numero, cor, goleiro, goleiroDeRodizio: true, completo: true, jogadoresDeLinha);

    private static Time TimeCom(
        int numero, CorDoTime? cor, string? goleiro, bool goleiroDeRodizio, bool completo, string[] jogadoresDeLinha) => new()
        {
            Numero = numero,
            Cor = cor,
            Goleiro = goleiro,
            GoleiroDeRodizio = goleiroDeRodizio,
            JogaComOsGoleirosDoGol = false,
            Completo = completo,
            Faltam = completo ? 0 : 6 - (goleiro is null ? 0 : 1) - jogadoresDeLinha.Length,
            JogadoresDeLinha = jogadoresDeLinha,
        };

    private static EntradaDaLegendaDoSorteio Entrada(TimesSorteados sorteado, ListaDoGrupo lista, OrigemDaLista? origem) => new()
    {
        Sorteado = sorteado,
        Lista = lista,
        Origem = origem,
        SorteadoEm = _sorteadoEm,
        Modelos = [_champions],
        FrasesDoRodape = [Frase],
    };

    private static OrigemDaLista EnviadaPor(string? nomeDoRemetente) => new() { NomeDoRemetente = nomeDoRemetente, EnviadaEm = _listaEnviadaEm };

    private static DateTimeOffset Instante(string texto) => DateTimeOffset.Parse(texto, CultureInfo.InvariantCulture);

    private static string[] Linhas(string legenda) => legenda.Split('\n');

    /// <summary>A legenda de <see cref="DoisTimesDeDois"/> às 21:04, com o tema Champions: muda só o que vem entre o horário e o separador.</summary>
    private static string LegendaDeDoisTimesDeDois(params string[] avisos)
    {
        string linhasDosAvisos = string.Concat(avisos.Select(aviso => aviso + "\n"));

        return $"""
            🏆 *PELADA | CHAMPIONS DA PELADA*
            🕘 *Sorteio das 21:04*
            {linhasDosAvisos}━━━━━━━━━━━━━━━━━━
            🟠 *LARANJA | OS REIS DO CHUTÃO*
            ───────────────
            🧤 Nuno
            👕 Nico Braga
            👕 Duda Neri

            🟢 *VERDE | OS DONOS DA RESENHA*
            ───────────────
            🧤 Cauã Luz
            👕 Cael Prado
            👕 Kaio
            ━━━━━━━━━━━━━━━━━━
            ⏰ *Por favor, não se atrasem, galera!*
            """;
    }

    private static ModeloDaLegenda Modelo(string cabecalho, string linhaDoHorario) => _champions with
    {
        Cabecalho = cabecalho,
        LinhaDoHorario = linhaDoHorario,
    };

    private static IEnumerable<string> NomesPagos(ListaDoGrupo lista) =>
        lista.Jogadores.Where(jogador => jogador.Pago).Select(jogador => jogador.Nome);
}
