using PeladaBot.Listas;
using PeladaBot.Sorteio;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Sorteio;

public sealed class MotorDoSorteioTestes
{
    private const int Semente = 42;
    private const int QuantidadeDeSementes = 100;
    private const int JogadoresDeLinhaPorTime = 5;
    private const int MaximoDeGoleiros = 5;

    // 10 de linha e 2 goleiros fixos formam 2 times completos, sem sobra
    [Fact]
    public void DeveFormarDoisTimesCompletosQuandoDoisGoleirosEDezDeLinha()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(10).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(2).And.AllSatisfy(time =>
        {
            time.Completo.Should().BeTrue();
            time.GoleiroDeRodizio.Should().BeFalse();
            time.Goleiro.Should().NotBeNull();
            time.JogadoresDeLinha.Should().HaveCount(JogadoresDeLinhaPorTime);
        });
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // 13 de linha e 2 goleiros fixos, numa quadra só: 2 times completos e 1 incompleto de 3, que joga com
    // os goleiros do gol e precisa de mais 2 na linha
    [Fact]
    public void DeveFormarDoisTimesCompletosEUmIncompletoDeTresQuandoDoisGoleirosETrezeDeLinha()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(13).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(3);
        sorteados.Times.Take(2).Should().AllSatisfy(time =>
        {
            time.Completo.Should().BeTrue();
            time.JogadoresDeLinha.Should().HaveCount(JogadoresDeLinhaPorTime);
        });
        Time incompleto = sorteados.Times[2];
        incompleto.Completo.Should().BeFalse();
        incompleto.Goleiro.Should().BeNull();
        incompleto.JogaComOsGoleirosDoGol.Should().BeTrue();
        incompleto.Faltam.Should().Be(2);
        incompleto.JogadoresDeLinha.Should().HaveCount(3);
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // A primeira quadra cheia (15 de linha e 2 goleiros fixos) dá 3 times de 5 na linha; os fixos ficam
    // no gol, então o terceiro está completo e não sorteia goleiro de rodízio
    [Fact]
    public void DeveFormarTresTimesDeCincoNaLinhaSemRodizioQuandoDoisGoleirosEQuinzeDeLinha()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(15).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(3).And.AllSatisfy(time =>
        {
            time.Completo.Should().BeTrue();
            time.Faltam.Should().Be(0);
            time.GoleiroDeRodizio.Should().BeFalse();
            time.JogadoresDeLinha.Should().HaveCount(JogadoresDeLinhaPorTime);
        });
        sorteados.Times.Take(2).Should().AllSatisfy(time => time.Goleiro.Should().NotBeNull());
        sorteados.Times[2].Goleiro.Should().BeNull();
        sorteados.Times[2].JogaComOsGoleirosDoGol.Should().BeTrue();
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // Com um goleiro fixo só, um gol fica sem fixo, e a regra do time de 6 com rodízio continua
    [Fact]
    public void DeveSortearGoleiroDeRodizioQuandoUmGoleiroEQuinzeDeLinha()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(1).ComJogadoresPagos(15).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(3);
        sorteados.Times[0].GoleiroDeRodizio.Should().BeFalse();
        sorteados.Times[1].GoleiroDeRodizio.Should().BeTrue();
        sorteados.Times.Should().AllSatisfy(time => time.JogaComOsGoleirosDoGol.Should().BeFalse());
        Time incompleto = sorteados.Times[2];
        incompleto.Completo.Should().BeFalse();
        incompleto.JogadoresDeLinha.Should().HaveCount(4);
        incompleto.Faltam.Should().Be(2);
    }

    // Quem estava na espera e já pagou (alguém desistiu ou fechou a segunda quadra) entra no sorteio,
    // misturado com todo mundo
    [Fact]
    public void DeveSortearQuemEstaNaEsperaEJaPagouQuandoAListaTemPromovidos()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(12).ComEsperaPaga(3).ComEspera(2).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        TodosEm(sorteados).Should().Contain(["Promovido 1", "Promovido 2", "Promovido 3"]).And.NotContain(["Espera 1", "Espera 2"]);
        sorteados.Times.Should().HaveCount(3).And.AllSatisfy(time => time.Completo.Should().BeTrue());
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    //, do texto da lista ao sorteio: o ✅ na Lista de espera é lido e quem pagou entra no time
    [Fact]
    public void DeveSortearQuemPagouNaEsperaQuandoAListaVemDoTextoDoGrupo()
    {
        ListaDoGrupo lista = LeitorDaLista.Ler("""
            Goleiros fixos:
            1. Téo Vargas 🧤
            2. Cauã Luz 🧤

            Lista de jogadores:
            1. Nico Braga ✅
            2. Duda Neri ✅
            3. Kiko ✅
            4. Bento Cruz ✅
            5. Enzo Paiva ✅
            6. Nilo Bastos ✅
            7. Kaio ✅
            8. Milo Castro ✅
            9. Luan Ferraz ✅
            10. Cael Prado ✅

            Lista de Espera:
            1. Hélio Pires ✅
            2. Davi
            """) ?? throw new InvalidOperationException("O texto é uma lista válida.");

        TimesSorteados sorteados = Sortear(lista, Semente);

        TodosEm(sorteados).Should().Contain("Hélio Pires").And.NotContain("Davi");
        TodosEm(sorteados).Should().HaveCount(13);
    }

    // Quem está na espera sem ✅ ainda espera uma vaga: não bloqueia o sorteio estrito
    [Fact]
    public void DeveSortearSemEsperarQuemEstaNaEsperaSemPagarQuandoTodaALinhaPagou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(10).ComEspera(5).Build();

        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente));

        resultado.Should().BeOfType<TimesSorteados>().Which.IgnorandoPendentes.Should().BeEmpty();
    }

    // Um promovido da espera conta para o mínimo de 4 pagos
    [Fact]
    public void DeveContarQuemFoiPromovidoDaEsperaQuandoFaltaUmParaQuatroPagos()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(3).ComEsperaPaga(1).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        TodosEm(sorteados).Should().Contain("Promovido 1");
    }

    // 16 de linha e 2 goleiros fixos formam 3 times completos, o terceiro com goleiro de rodízio
    [Fact]
    public void DeveFormarTresTimesCompletosComGoleiroDeRodizioNoTerceiroQuandoDoisGoleirosEDezesseisDeLinha()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(16).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(3).And.AllSatisfy(time => time.Completo.Should().BeTrue());
        sorteados.Times.Take(2).Should().AllSatisfy(time => time.GoleiroDeRodizio.Should().BeFalse());
        Time terceiro = sorteados.Times[2];
        terceiro.GoleiroDeRodizio.Should().BeTrue();
        terceiro.Goleiro.Should().NotBeNull();
        terceiro.JogadoresDeLinha.Should().HaveCount(JogadoresDeLinhaPorTime);
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // 22 de linha e 2 goleiros fixos formam 4 times completos, com as 4 cores em alguma ordem
    [Fact]
    public void DeveFormarQuatroTimesCompletosComAsQuatroCoresQuandoDoisGoleirosEVinteEDoisDeLinha()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(22).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(4).And.AllSatisfy(time => time.Completo.Should().BeTrue());
        sorteados.Times.Select(time => time.Cor).Should().BeEquivalentTo(Enum.GetValues<CorDoTime>());
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // Sem goleiro fixo, 12 de linha formam 2 times de 6, cada um com seu goleiro de rodízio
    [Fact]
    public void DeveFormarDoisTimesDeSeisComGoleiroDeRodizioQuandoZeroGoleirosEDozeDeLinha()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresPagos(12).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(2).And.AllSatisfy(time =>
        {
            time.Completo.Should().BeTrue();
            time.GoleiroDeRodizio.Should().BeTrue();
            time.Goleiro.Should().NotBeNull();
            time.JogadoresDeLinha.Should().HaveCount(JogadoresDeLinhaPorTime);
        });
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // Com os 2 times fechados (goleiro + 5), o 3º goleiro fixo não forma time sozinho, fica revezando no gol
    [Fact]
    public void DeveDeixarOGoleiroExtraRevezandoNoGolQuandoTresGoleirosEDezDeLinha()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(3).ComJogadoresPagos(10).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(2).And.AllSatisfy(time =>
        {
            time.Completo.Should().BeTrue();
            time.JogadoresDeLinha.Should().HaveCount(JogadoresDeLinhaPorTime);
        });
        sorteados.RevezandoNoGol.Should().ContainSingle();
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // Quem fica no time incompleto varia entre sementes, não são sempre os últimos da lista
    [Fact]
    public void DeveVariarQuemFicaNoTimeIncompletoQuandoASementeMuda()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(13).Build();

        IEnumerable<string[]> incompletos = Sementes().Select(semente => Sortear(lista, semente).Times[^1].JogadoresDeLinha.ToArray());

        incompletos.Select(nomes => string.Join(",", nomes)).Distinct().Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // De 4 a 9 de linha: com um goleiro fixo por time, nada muda; o time sem goleiro fixo sorteia o seu de rodízio
    [Theory]
    [InlineData(4, 2, 2)]
    [InlineData(8, 4, 4)]
    [InlineData(9, 5, 4)]
    public void DeveFormarDoisTimesComUmGoleiroFixoCadaQuandoQuatroANovePagaramComDoisGoleiros(
        int quantidadePaga, int tamanhoDoTimeMaior, int tamanhoDoTimeMenor)
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(quantidadePaga).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Select(time => time.JogadoresDeLinha.Count).Should().BeEquivalentTo([tamanhoDoTimeMaior, tamanhoDoTimeMenor]);
        sorteados.Times.Should().AllSatisfy(time =>
        {
            time.Completo.Should().BeTrue();
            time.GoleiroDeRodizio.Should().BeFalse();
            time.Goleiro.Should().NotBeNull();
        });
    }

    // De 4 a 9 de linha, 1 goleiro fixo: o time maior leva o goleiro fixo; o menor sorteia o seu de rodízio
    [Fact]
    public void DeveDarOGoleiroFixoAoTimeMaiorEDeRodizioAoMenorQuandoUmGoleiroENovePagaram()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(1).ComJogadoresPagos(9).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(2);
        sorteados.Times[0].GoleiroDeRodizio.Should().BeFalse();
        sorteados.Times[0].JogadoresDeLinha.Should().HaveCount(5);
        sorteados.Times[1].GoleiroDeRodizio.Should().BeTrue();
        sorteados.Times[1].JogadoresDeLinha.Should().HaveCount(3);
        sorteados.Times.Should().AllSatisfy(time => time.Goleiro.Should().NotBeNull());
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // De 4 a 9 de linha, 0 goleiros: os dois times sorteiam o seu goleiro de rodízio
    [Fact]
    public void DeveDarGoleiroDeRodizioAosDoisTimesQuandoZeroGoleirosESeisPagaram()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComJogadoresPagos(6).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(2).And.AllSatisfy(time =>
        {
            time.GoleiroDeRodizio.Should().BeTrue();
            time.Goleiro.Should().NotBeNull();
            time.JogadoresDeLinha.Should().HaveCount(2);
        });
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // De 4 a 9 de linha, goleiro fixo extra: o 3º goleiro completa a linha de um dos 2 times
    [Fact]
    public void DeveColocarOGoleiroExtraNaLinhaQuandoTresGoleirosESeisPagaram()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(3).ComJogadoresPagos(6).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(2).And.AllSatisfy(time =>
        {
            time.Completo.Should().BeTrue();
            time.GoleiroDeRodizio.Should().BeFalse();
        });
        sorteados.Times.Select(time => time.JogadoresDeLinha.Count).Should().BeEquivalentTo([4, 3]);
        sorteados.RevezandoNoGol.Should().BeEmpty();
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // 4 fixed goalkeepers e 6 de linha davam 2 times de goleiro + 3 e mais 2 "times" de um goleiro só;
    // agora os 2 goleiros que sobram vão para a linha, um em cada time
    [Fact]
    public void DeveDividirOsGoleirosExtrasEntreAsLinhasQuandoQuatroGoleirosESeisPagaram()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo()
            .ComGoleiros("Téo Vargas", "Cauã Luz", "Guto", "Neco")
            .ComJogadoresPagos("Lino", "Saulo", "Kiko", "Tadeu", "Enzo Paiva", "Ruy Pacheco")
            .Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(2).And.AllSatisfy(time =>
        {
            time.Completo.Should().BeTrue();
            time.Goleiro.Should().NotBeNull();
            time.JogadoresDeLinha.Should().HaveCount(4);
        });
        sorteados.RevezandoNoGol.Should().BeEmpty();
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // Nenhum time sai só com goleiro, ninguém passa de 5 na linha e todo elegível aparece uma vez
    [Theory]
    [MemberData(nameof(QuantidadesPagasEDeGoleiros), 4, 25)]
    public void NuncaDeveFormarTimeSoComGoleiroQuandoSobramGoleirosFixos(int quantidadePaga, int quantidadeDeGoleiros)
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(quantidadeDeGoleiros).ComJogadoresPagos(quantidadePaga).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().AllSatisfy(time =>
            time.JogadoresDeLinha.Should().NotBeEmpty().And.HaveCountLessThanOrEqualTo(JogadoresDeLinhaPorTime));
        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
    }

    // Jogadores sem ✅ bloqueiam o sorteio inteiro, mesmo havendo pagos suficientes, e
    // aparecem na ordem da lista
    [Fact]
    public void NaoDeveSortearEApontarQuemFaltaPagarNaOrdemDaListaQuandoAlgunsJogadoresNaoTemAMarca()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(10).ComJogadoresNaoPagos("Luan Ferraz", "Paco").Build();

        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente));

        resultado.Should().BeOfType<PagamentoPendente>().Which.NomesPendentes.Should().Equal("Luan Ferraz", "Paco");
    }

    [Fact]
    public void NaoDeveSortearQuandoAListaDeJogadoresEstaVazia()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComEspera(2).Build();

        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente));

        resultado.Should().BeOfType<NinguemPagou>();
    }

    // Ninguém pagou, mas a lista não está vazia: falta pagar, não "ninguém na lista"
    [Fact]
    public void NaoDeveSortearEApontarQuemFaltaPagarQuandoNenhumJogadorDeLinhaPagou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresNaoPagos(3).ComEspera(2).Build();

        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente));

        resultado.Should().BeOfType<PagamentoPendente>().Which.NomesPendentes.Should().Equal("Pendente 1", "Pendente 2", "Pendente 3");
    }

    [Fact]
    public void DeveDeixarForaDoSorteioQuemEstaNaEsperaSemPagarQuandoTemNomes()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(12).ComEspera(3).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        TodosEm(sorteados).Should().NotIntersectWith(lista.Espera.Select(jogador => jogador.Nome));
    }

    [Fact]
    public void DeveSortearOsMesmosTimesQuandoListaESementeSaoAsMesmas()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(4).ComJogadoresPagos(17).ComEspera(2).Build();

        TimesSorteados primeiro = Sortear(lista, Semente);
        TimesSorteados segundo = Sortear(lista, Semente);

        segundo.Should().BeEquivalentTo(primeiro, opcoes => opcoes.WithStrictOrdering());
    }

    [Fact]
    public void DeveSortearAoMenosDuasDivisoesDiferentesQuandoASementeMuda()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(12).Build();

        string[] divisoes = [.. Sementes().Select(semente => Divisao(Sortear(lista, semente)))];

        divisoes.Distinct().Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void DeveDarACadaTimeUmaCorDiferenteQuandoDoisTimesSaoSorteados()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(10).Build();

        TimesSorteados[] sorteios = [.. Sementes().Select(semente => Sortear(lista, semente))];

        sorteios.Should().AllSatisfy(sorteados => sorteados.Times.Select(time => time.Cor).Should().NotContainNulls().And.OnlyHaveUniqueItems());
    }

    [Fact]
    public void DeveUsarTodaCorEntreSorteiosQuandoDoisTimesSaoSorteados()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(10).Build();

        TimesSorteados[] sorteios = [.. Sementes().Select(semente => Sortear(lista, semente))];

        sorteios.SelectMany(sorteados => sorteados.Times).Select(time => time.Cor).Distinct().Should().BeEquivalentTo(Enum.GetValues<CorDoTime>());
    }

    // A 5ª cor em diante não existe, o time é chamado pelo número
    [Fact]
    public void DeveChamarOQuintoTimePeloSeuNumeroQuandoCincoTimesSaoSorteados()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(5).ComJogadoresPagos(25).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(5);
        sorteados.Times.Select(time => time.Numero).Should().Equal(1, 2, 3, 4, 5);
        sorteados.Times.Take(4).Select(time => time.Cor).Should().BeEquivalentTo(Enum.GetValues<CorDoTime>());
        sorteados.Times[4].Cor.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void NaoDeveSortearEManterAQuantidadePagaQuandoSoUmATresPagaram(int quantidadePaga)
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(quantidadePaga).Build();

        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente));

        resultado.Should().Be(new PagosInsuficientes { QuantidadePaga = quantidadePaga });
    }

    // "!sortear pagos" ignora quem está sem ✅ e devolve quem foi ignorado
    [Fact]
    public void DeveSortearIgnorandoQuemNaoPagouQuandoSomentePagos()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(10).ComJogadoresNaoPagos("Luan Ferraz", "Paco").Build();

        TimesSorteados sorteados = Sortear(lista, Semente, somentePagos: true);

        sorteados.IgnorandoPendentes.Should().Equal("Luan Ferraz", "Paco");
        TodosEm(sorteados).Should().NotContain("Luan Ferraz").And.NotContain("Paco");
    }

    // "!sortear pagos" com ninguém pago responde como o sorteio sem ninguém pago (não bloqueia por pendência)
    [Fact]
    public void NaoDeveSortearQuandoSomentePagosENenhumJogadorPagou()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresNaoPagos(3).Build();

        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente), somentePagos: true);

        resultado.Should().BeOfType<NinguemPagou>();
    }

    // "!sortear pagos" com menos de 4 pagos continua sem sortear
    [Fact]
    public void NaoDeveSortearQuandoSomentePagosEMenosDeQuatroPagaram()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(2).ComJogadoresNaoPagos("Paco").Build();

        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(Semente), somentePagos: true);

        resultado.Should().Be(new PagosInsuficientes { QuantidadePaga = 2 });
    }

    // O sorteio normal nunca ignora pendente
    [Fact]
    public void NaoDeveIgnorarNinguemQuandoOSorteioEhEstrito()
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(10).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.IgnorandoPendentes.Should().BeEmpty();
    }

    // De 4 a 9 de linha
    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(9)]
    public void NaoDeveSortearMaisDeDoisTimesQuandoQuatroANovePagaram(int quantidadePaga)
    {
        ListaDoGrupo lista = new ConstrutorDeListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(quantidadePaga).Build();

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCount(2);
        Math.Abs(BrutoDeLinha(sorteados.Times[0]) - BrutoDeLinha(sorteados.Times[1])).Should().BeLessThanOrEqualTo(1);
    }

    // Cada elegível aparece exatamente uma vez, nenhum time tem mais de 5 na linha,
    // e um time incompleto alem do primeiro (goleiro fixo demais para os times que existem, caso raríssimo) só
    // tem o seu goleiro, nunca gente na linha
    [Theory]
    [MemberData(nameof(QuantidadesPagasEDeGoleiros), 4, 21)]
    public void DeveColocarCadaPessoaElegivelExatamenteUmaVezQuandoQuatroOuMaisPagaram(int quantidadePaga, int quantidadeDeGoleiros)
    {
        ListaDoGrupo lista = ListaComEspera(quantidadePaga, quantidadeDeGoleiros);

        TimesSorteados sorteados = Sortear(lista, Semente);

        TodosEm(sorteados).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(NomesElegiveis(lista));
        sorteados.Times.Should().AllSatisfy(time => time.JogadoresDeLinha.Should().HaveCountLessThanOrEqualTo(JogadoresDeLinhaPorTime));
        sorteados.Times.Where(time => time.Completo is false).Skip(1).Should().AllSatisfy(time => time.JogadoresDeLinha.Should().BeEmpty());
    }

    // De 4 a 9 pagos seguem 2 times quase iguais, sempre completos
    [Theory]
    [MemberData(nameof(QuantidadesPagasEDeGoleiros), 4, 9)]
    public void DeveFormarDoisTimesDeTamanhoQuaseIgualESempreCompletosQuandoQuatroANovePagaram(int quantidadePaga, int quantidadeDeGoleiros)
    {
        ListaDoGrupo lista = ListaComEspera(quantidadePaga, quantidadeDeGoleiros);

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Should().HaveCountGreaterThanOrEqualTo(2);
        sorteados.Times.Take(2).Should().AllSatisfy(time => time.Completo.Should().BeTrue());
        Math.Abs(BrutoDeLinha(sorteados.Times[0]) - BrutoDeLinha(sorteados.Times[1])).Should().BeLessThanOrEqualTo(1);
    }

    // 10 ou mais pagos formam times completos de 5 na linha, cada um com goleiro
    // próprio (fixo ou de rodízio) ou jogando com os fixos do gol; sem reservas, e quem falta é contado
    [Theory]
    [MemberData(nameof(QuantidadesPagasEDeGoleiros), 10, 21)]
    public void DeveFormarTimesCompletosDeCincoNaLinhaQuandoDezOuMaisPagaram(int quantidadePaga, int quantidadeDeGoleiros)
    {
        ListaDoGrupo lista = ListaComEspera(quantidadePaga, quantidadeDeGoleiros);

        TimesSorteados sorteados = Sortear(lista, Semente);

        sorteados.Times.Where(time => time.Completo).Should().AllSatisfy(time =>
        {
            time.JogadoresDeLinha.Should().HaveCount(JogadoresDeLinhaPorTime);
            (time.Goleiro is not null || time.JogaComOsGoleirosDoGol).Should().BeTrue();
            time.Faltam.Should().Be(0);
        });
        sorteados.Times.Where(time => time.Completo is false).Should().AllSatisfy(time => time.Faltam.Should().BePositive());
        sorteados.Times.Where(time => time.Completo is false).Skip(1).Should().AllSatisfy(time => time.JogadoresDeLinha.Should().BeEmpty());
    }

    // Os goleiros ficam no gol só numa quadra (até 15 de linha) e com ao menos 2 fixos
    [Theory]
    [MemberData(nameof(QuantidadesPagasEDeGoleiros), 10, 21)]
    public void DeveDeixarOsGoleirosNoGolSoNumaQuadraComDoisFixosQuandoDezOuMaisPagaram(int quantidadePaga, int quantidadeDeGoleiros)
    {
        ListaDoGrupo lista = ListaComEspera(quantidadePaga, quantidadeDeGoleiros);

        TimesSorteados sorteados = Sortear(lista, Semente);

        if (quantidadePaga <= 15 && quantidadeDeGoleiros >= 2)
        {
            sorteados.Times.Should().AllSatisfy(time => time.GoleiroDeRodizio.Should().BeFalse());
        }
        else
        {
            sorteados.Times.Should().AllSatisfy(time => time.JogaComOsGoleirosDoGol.Should().BeFalse());
        }
    }

    public static TheoryData<int, int> QuantidadesPagasEDeGoleiros(int minimoPago, int maximoPago)
    {
        TheoryData<int, int> quantidades = [];

        for (int quantidadePaga = minimoPago; quantidadePaga <= maximoPago; quantidadePaga++)
        {
            for (int quantidadeDeGoleiros = 0; quantidadeDeGoleiros <= MaximoDeGoleiros; quantidadeDeGoleiros++)
            {
                quantidades.Add(quantidadePaga, quantidadeDeGoleiros);
            }
        }

        return quantidades;
    }

    private static TimesSorteados Sortear(ListaDoGrupo lista, int semente, bool somentePagos = false)
    {
        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(lista, new Random(semente), somentePagos);

        return resultado.Should().BeOfType<TimesSorteados>().Which;
    }

    private static IEnumerable<int> Sementes() => Enumerable.Range(0, QuantidadeDeSementes);

    /// <summary>Uma lista toda paga, que também tem uma waiting list, que nunca deve ser sorteada.</summary>
    private static ListaDoGrupo ListaComEspera(int quantidadePaga, int quantidadeDeGoleiros) =>
        new ConstrutorDeListaDoGrupo()
            .ComGoleiros(quantidadeDeGoleiros)
            .ComJogadoresPagos(quantidadePaga)
            .ComEspera(2)
            .Build();

    private static IEnumerable<string> NomesPagos(ListaDoGrupo lista) =>
        lista.Jogadores.Concat(lista.Espera).Where(jogador => jogador.Pago).Select(jogador => jogador.Nome);

    private static IEnumerable<string> NomesElegiveis(ListaDoGrupo lista) => lista.Goleiros.Concat(NomesPagos(lista));

    /// <summary>
    /// Quantos jogadores de linha o time recebeu antes de um deles, se for o caso, ser promovido a goleiro de
    /// rodízio: o tamanho bruto que a divisão em dois grupos deu a ele.
    /// </summary>
    private static int BrutoDeLinha(Time time) => time.JogadoresDeLinha.Count + (time.GoleiroDeRodizio ? 1 : 0);

    /// <summary>Todo elegível que o sorteio colocou em algum time: o goleiro (fixo ou de rodízio) e os de linha.</summary>
    private static IEnumerable<string> TodosEm(TimesSorteados sorteados) =>
        sorteados.Times
            .SelectMany(time => (time.Goleiro is null ? [] : (IEnumerable<string>)[time.Goleiro]).Concat(time.JogadoresDeLinha))
            .Concat(sorteados.RevezandoNoGol);

    /// <summary>Quem joga com quem: os jogadores de linha de cada time, ignorando sua ordem, a ordem dos times, cores e goleiros.</summary>
    private static string Divisao(TimesSorteados sorteados) =>
        string.Join(" | ", sorteados.Times
            .Select(time => string.Join(", ", time.JogadoresDeLinha.Order(StringComparer.Ordinal)))
            .Order(StringComparer.Ordinal));
}
