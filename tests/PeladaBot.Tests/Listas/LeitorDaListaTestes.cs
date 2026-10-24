using System.Text.Json;
using PeladaBot.Listas;

namespace PeladaBot.Tests.Listas;

public sealed class LeitorDaListaTestes
{
    private static readonly JsonSerializerOptions _opcoesDeJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DeveLerNomesSemNumeracaoQuandoEstaoNasSecoesDaLista()
    {
        string texto = """
            Goleiros fixos:
            Téo Vargas🧤
            Cauã Luz 🧤

            Lista de jogadores:
            Hugo ✅
            Duda Neri

            Lista de Espera:
            Hélio Pires ✅
            Davi
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().Equal("Téo Vargas", "Cauã Luz");
        lista.Jogadores.Should().Equal(Pago("Hugo"), NaoPago("Duda Neri"));
        lista.Espera.Should().Equal(Pago("Hélio Pires"), NaoPago("Davi"));
        lista.Avisos.Should().BeEmpty();
    }

    [Fact]
    public void DeveManterAOrdemQuandoMisturaNomesNumeradosESemNumeracao()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅
            *Hugo* ✅
            3) Duda Neri
            ✅ Luca P 22💚
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"), Pago("Hugo"), NaoPago("Duda Neri"), Pago("Luca P 22"));
    }

    [Fact]
    public void DeveIgnorarCamposLinksEVagasQuandoANumeracaoEhOpcional()
    {
        string texto = """
            Hugo ✅
            Lista de jogadores:
            Nico Braga ✅
            Horário: 20:30h
            Local: Campo Municipal
            Goleiros: FREE ✅
            pagamento.exemplo/presidente
            https://pagamento.exemplo
            ✅
            2.
            3
            Duda Neri
            Nota 1: regras do jogo
            Hugo ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"), NaoPago("Duda Neri"));
        lista.Jogo.Should().Be(new InfoDoJogo { Horario = "20:30h", Local = "Campo Municipal" });
    }

    [Fact]
    public void DeveManterAPrimeiraMarcaQuandoONomeSeRepeteComESemNumeracao()
    {
        string texto = """
            Lista de jogadores:
            Hugo
            2. hugo ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(NaoPago("Hugo"));
        lista.Avisos.Should().Equal(new NomeRepetidoComMarcaDiferente { Nome = "Hugo" });
    }

    [Theory]
    [InlineData("Hugo Paco")]
    [InlineData("Jo\u0061\u0303o")]
    [InlineData("D'Ávila")]
    [InlineData("Hugo-Paco")]
    [InlineData("Renzo M.")]
    [InlineData("Hugo (22)")]
    public void DevePreservarONomeQuandoNaoTemNumeracao(string nome)
    {
        ListaDoGrupo lista = LerListaValida($"Lista de jogadores:\n{nome} ✅");

        lista.Jogadores.Should().Equal(Pago(nome));
    }

    [Theory]
    [InlineData("- Hugo ✅")]
    [InlineData("• Hugo ✅")]
    public void DeveLerNomeSemNumeroQuandoALinhaUsaMarcador(string linha)
    {
        ListaDoGrupo lista = LerListaValida($"Lista de jogadores:\n{linha}");

        lista.Jogadores.Should().Equal(Pago("Hugo"));
    }

    [Fact]
    public void DeveRespeitarPrioridadeDasSecoesQuandoOsNomesNaoTemNumero()
    {
        string texto = """
            Goleiros fixos:
            Téo Vargas🧤
            Lista de jogadores:
            Téo Vargas ✅
            Hugo ✅
            Lista de Espera:
            hugo ✅
            Ciro ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().Equal("Téo Vargas");
        lista.Jogadores.Should().Equal(Pago("Hugo"));
        lista.Espera.Should().Equal(Pago("Ciro"));
        lista.Avisos.Should().Equal(
            new NomeEmDuasSecoes { Nome = "Téo Vargas", MantidoEm = SecaoDaLista.Goleiros },
            new NomeEmDuasSecoes { Nome = "Hugo", MantidoEm = SecaoDaLista.Jogadores });
    }

    [Fact]
    public void DeveDividirGoleirosJogadoresEEsperaQuandoAListaTemTodasAsSecoes()
    {
        string texto = """
            ⚽PRÓXIMO JOGO⚽

            Goleiros fixos:
            1. Téo Vargas🧤
            2. Cauã Luz 🧤

            Lista de jogadores:

            1. Nico Braga ✅
            2. Duda Neri

            Lista de Espera:
            1. Hélio Pires
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().Equal("Téo Vargas", "Cauã Luz");
        lista.Jogadores.Should().Equal(Pago("Nico Braga"), NaoPago("Duda Neri"));
        lista.Espera.Select(jogador => jogador.Nome).Should().Equal("Hélio Pires");
    }

    [Fact]
    public void DeveIgnorarCabecalhoENotasQuandoEstaoForaDasSecoes()
    {
        string texto = """
            ⚽PRÓXIMO JOGO⚽

            Data: Quarta-Feira – 14/10
            Goleiros: FREE ✅

            pagamento.exemplo/presidente

            Lista de jogadores:
            1. Nico Braga ✅

            Nota 1: Para confirmar sua presença, realize o pagamento.
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().BeEmpty();
        lista.Jogadores.Should().Equal(Pago("Nico Braga"));
        lista.Espera.Should().BeEmpty();
    }

    [Fact]
    public void DeveIgnorarVagaQuandoOItemTemSoONumero()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅
            2.
            3.
            4.  Duda Neri
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"), NaoPago("Duda Neri"));
    }

    [Fact]
    public void DeveRemoverEmojisEManterNumerosQuandoONomeOsTem()
    {
        string texto = """
            Goleiros fixos:
            1. Goleiro A🧤

            Lista de jogadores:
            14.  Jogador 14 22💚 ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().Equal("Goleiro A");
        lista.Jogadores.Should().Equal(Pago("Jogador 14 22"));
    }

    [Fact]
    public void DeveMarcarJogadorComoNaoPagoQuandoALinhaNaoTemMarca()
    {
        string texto = """
            Lista de jogadores:
            1. Tales
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(NaoPago("Tales"));
    }

    [Fact]
    public void DeveMarcarJogadorComoPagoQuandoAMarcaEstaAntesOuDepoisDoNome()
    {
        string texto = """
            Lista de jogadores:
            1. ✅ Nico Braga
            2. Duda Neri✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"), Pago("Duda Neri"));
    }

    [Fact]
    public void DeveManterUmJogadorQuandoOMesmoNomeApareceComCaixaEAcentoDiferentes()
    {
        string texto = """
            Lista de jogadores:
            1. Hugo ✅
            2. hugo ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Hugo"));
        lista.Avisos.Should().BeEmpty();
    }

    [Fact]
    public void DeveManterOsDoisJogadoresQuandoOsNomesSoSeAssemelham()
    {
        string texto = """
            Lista de jogadores:
            1. Luan Ferraz ✅
            2. Renzo M ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Luan Ferraz"), Pago("Renzo M"));
    }

    [Theory]
    [InlineData("2026-10-14-v1")]
    [InlineData("2026-10-14-v2")]
    [InlineData("2026-10-14-v3")]
    public void DeveLerTodoNomeQuandoAListaVemDoCorpusReal(string nomeDoCorpus)
    {
        string texto = File.ReadAllText(CaminhoDoCorpus($"{nomeDoCorpus}.txt"));
        ListaEsperada esperada = LerEsperada(nomeDoCorpus);

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().Equal(esperada.Goleiros);
        lista.Jogadores.Should().Equal(esperada.Jogadores);
        lista.Espera.Select(jogador => jogador.Nome).Should().Equal(esperada.Espera);
        lista.Jogo.Should().Be(esperada.Jogo);
        lista.Avisos.Should().BeEmpty();
    }

    // Cabeçalho lido para o card
    [Fact]
    public void DeveLerInfoDoJogoQuandoOCabecalhoTemDataHorarioELocal()
    {
        string texto = """
            Data: Quarta-Feira – 14/10
            Horário: 20:30h
            Local:  Campo Municipal

            Lista de jogadores:
            1. Nico Braga ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogo.Should().Be(new InfoDoJogo
        {
            Data = "Quarta-Feira – 14/10",
            Horario = "20:30h",
            Local = "Campo Municipal",
        });
    }

    // Cabeçalho lido para o card
    [Fact]
    public void DeveDeixarInfoDoJogoVaziaQuandoFaltamAsLinhasDeCabecalho()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogo.Should().Be(new InfoDoJogo());
    }

    [Fact]
    public void DeveManterOGoleiroEAvisarQuandoONomeTambemEstaEmJogadores()
    {
        string texto = """
            Goleiros fixos:
            1. Téo Vargas🧤

            Lista de jogadores:
            1. Téo Vargas ✅
            2. Nico Braga ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().Equal("Téo Vargas");
        lista.Jogadores.Should().Equal(Pago("Nico Braga"));
        lista.Avisos.Should().Equal(new NomeEmDuasSecoes { Nome = "Téo Vargas", MantidoEm = SecaoDaLista.Goleiros });
    }

    [Fact]
    public void DeveManterOJogadorEAvisarQuandoONomeTambemEstaEmEspera()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅

            Lista de Espera:
            1. nico braga
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"));
        lista.Espera.Should().BeEmpty();
        lista.Avisos.Should().Equal(new NomeEmDuasSecoes { Nome = "Nico Braga", MantidoEm = SecaoDaLista.Jogadores });
    }

    [Fact]
    public void DeveManterAPrimeiraOcorrenciaEAvisarQuandoUmNomeRepetidoTemMarcaDiferente()
    {
        string texto = """
            Lista de jogadores:
            1. Hugo
            2. hugo ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(NaoPago("Hugo"));
        lista.Avisos.Should().Equal(new NomeRepetidoComMarcaDiferente { Nome = "Hugo" });
    }

    [Fact]
    public void DeveLerSecoesQuandoOsTitulosEstaoEmNegritoSemDoisPontosOuEmOutraCaixa()
    {
        string texto = """
            *Goleiros fixos*
            1. Téo Vargas

            *LISTA DE JOGADORES*
            1. Nico Braga ✅

            lista de espera
            1. Hélio Pires
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().Equal("Téo Vargas");
        lista.Jogadores.Should().Equal(Pago("Nico Braga"));
        lista.Espera.Select(jogador => jogador.Nome).Should().Equal("Hélio Pires");
    }

    [Fact]
    public void DeveLerItensQuandoANumeracaoUsaParentesesTracoOuZeroNaFrente()
    {
        string texto = """
            Lista de jogadores:
            1) Nico Braga ✅
            2 - Duda Neri
            03 Cael Prado ✅
            4.Kaio
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"), NaoPago("Duda Neri"), Pago("Cael Prado"), NaoPago("Kaio"));
    }

    [Fact]
    public void DeveLerAListaQuandoAsLinhasTerminamComRetornoDeCarro()
    {
        string texto = "Lista de jogadores:\r\n1. Nico Braga ✅\r\n2. Duda Neri\r\n";

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"), NaoPago("Duda Neri"));
    }

    [Fact]
    public void DevePararDeLerQuandoNotasNumeradasSeguemALista()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅

            Nota 1: regras do jogo
            1. Pagar até quarta
            2. Chegar 20:15
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"));
    }

    // "Notas:" encerra a leitura como "Nota"
    [Theory]
    [InlineData("Notas:")]
    [InlineData("NOTAS")]
    [InlineData("*Notas that matter*")]
    public void DevePararDeLerQuandoUmTituloDeNotasNoPluralSegueALista(string titulo)
    {
        string texto = $"""
            Lista de jogadores:
            1. Nico Braga ✅

            {titulo}
            1. Pagar até quarta
            2. Chegar 20:15
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"));
    }

    // Um nome que só começa com "nota" não encerra a leitura
    [Fact]
    public void DeveContinuarLendoQuandoUmaLinhaSoComecaComAsLetrasDeNota()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅
            Notário
            2. Duda Neri
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"), NaoPago("Notário"), NaoPago("Duda Neri"));
    }

    [Fact]
    public void DeveRetornarSecoesVaziasQuandoGoleirosEEsperaEstaoAusentes()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Goleiros.Should().BeEmpty();
        lista.Espera.Should().BeEmpty();
    }

    [Fact]
    public void DeveTratarJogadorComoNaoPagoQuandoAMarcaNaoEhOCheckVerde()
    {
        string texto = """
            Lista de jogadores:
            1. Paco ✔️
            2. Cael Prado ☑️
            3. Kaio 👍
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(NaoPago("Paco"), NaoPago("Cael Prado"), NaoPago("Kaio"));
    }

    // O ✅ na espera não tira ninguém de lá, mas marca que já pagou
    [Fact]
    public void DeveManterNomeEmEsperaComoPagoQuandoTemOCheckVerde()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅

            Lista de Espera:
            1. Davi ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"));
        lista.Espera.Should().Equal(Pago("Davi"));
    }

    // Na espera, sem ✅ é quem ainda espera vaga
    [Fact]
    public void DeveLerNomeEmEsperaComoNaoPagoQuandoNaoTemOCheckVerde()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅

            Lista de Espera:
            1. Davi
            2. Ciro ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Espera.Should().Equal(NaoPago("Davi"), Pago("Ciro"));
    }

    [Fact]
    public void DeveTratarNomesComoIguaisQuandoOAcentoEhComposoOuDecomposto()
    {
        string composto = "Hugo";
        string decomposto = "Hugo";
        string texto = $"Lista de jogadores:\n1. {composto} ✅\n2. {decomposto} ✅\n";

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago(composto));
    }

    [Fact]
    public void DeveIgnorarItemQuandoSoRestaAMarcaDeCheck()
    {
        string texto = """
            Lista de jogadores:
            1. Nico Braga ✅
            2. ✅
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Nico Braga"));
    }

    [Fact]
    public void DeveRemoverMarcacaoDoWhatsAppQuandoONomeEstaFormatado()
    {
        string texto = """
            Lista de jogadores:
            1. *Ivo* ✅
            2. _Lia_
            3. ~Milo Castro~
            """;

        ListaDoGrupo lista = LerListaValida(texto);

        lista.Jogadores.Should().Equal(Pago("Ivo"), NaoPago("Lia"), NaoPago("Milo Castro"));
    }

    // "É lista"
    [Theory]
    [InlineData("Bom dia galera, bora jogar hoje?")]
    [InlineData("Goleiros fixos:\n1. Téo Vargas")]
    [InlineData("Hugo ✅")]
    public void DeveRetornarNuloQuandoOTextoNaoTemOTituloDeJogadores(string texto)
    {
        ListaDoGrupo? lista = LeitorDaLista.Ler(texto);

        lista.Should().BeNull();
    }

    private static ListaDoGrupo LerListaValida(string texto)
    {
        ListaDoGrupo? lista = LeitorDaLista.Ler(texto);

        lista.Should().NotBeNull();

        return lista ?? throw new InvalidOperationException("A verificação acima garante uma lista.");
    }

    private static JogadorListado Pago(string nome) => new() { Nome = nome, Pago = true };

    private static JogadorListado NaoPago(string nome) => new() { Nome = nome, Pago = false };

    private static string CaminhoDoCorpus(string nomeDoArquivo) => Path.Combine(AppContext.BaseDirectory, "Corpus", nomeDoArquivo);

    private static ListaEsperada LerEsperada(string nomeDoCorpus)
    {
        string json = File.ReadAllText(CaminhoDoCorpus($"{nomeDoCorpus}.expected.json"));

        return JsonSerializer.Deserialize<ListaEsperada>(json, _opcoesDeJson)
            ?? throw new InvalidOperationException($"Arquivo de expectativa vazio para {nomeDoCorpus}.");
    }

    private sealed record ListaEsperada
    {
        public required IReadOnlyList<string> Goleiros { get; init; }

        public required IReadOnlyList<JogadorListado> Jogadores { get; init; }

        public required IReadOnlyList<string> Espera { get; init; }

        public required InfoDoJogo Jogo { get; init; }
    }
}
