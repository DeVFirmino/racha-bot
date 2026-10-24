using PeladaBot.EscolhaDaLista;
using PeladaBot.Listas;
using PeladaBot.Respostas;
using PeladaBot.Tests.UtilitariosDeTeste;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.EscolhaDaLista;

public sealed class SeletorDeListaTestes : IDisposable
{
    private const string IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo;
    private const string NomeDoPresidente = "Presidente Fulano";
    private const string NomeDoIntegrante = "Integrante Beltrano";

    private static readonly TimeSpan _idadeMaximaDaLista = TimeSpan.FromHours(72);

    private readonly ConstrutorDeCasoDeUso _bot = new();

    public void Dispose() => _bot.Dispose();

    [Fact]
    public async Task DeveUsarAListaCitadaQuandoUmaListaMaisNovaExiste()
    {
        _bot.Historico.Com(ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromMinutes(5), "Mais Novo"));
        MensagemCitada citada = Citar(TextoDaLista("Citado"));

        ListaEscolhida escolha = await Selecionar(citada, AutoresDaLista.Presidencia);

        ListaEncontrada encontrada = escolha.Should().BeOfType<ListaEncontrada>().Which;
        NomesPagos(encontrada.Lista).Should().Equal("Citado");
        encontrada.Origem.Should().BeNull();
        _bot.Historico.ConsultasDeHistorico.Should().BeEmpty();
    }

    // A citada vale de qualquer idade
    [Fact]
    public async Task DeveUsarAListaCitadaQuandoElaEhMaisAntigaQueSetentaEDuasHoras()
    {
        MensagemDoGrupo antiga = new ConstrutorDeMensagemDoGrupo()
            .De(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, NomeDoPresidente)
            .EnviadaEm(ConstrutorDeCasoDeUso.Agora - TimeSpan.FromDays(10))
            .ComTexto(TextoDaLista("Antigo"))
            .Build();
        _bot.Historico.Com(antiga);

        ListaEscolhida escolha =
            await Selecionar(new MensagemCitada { Id = Chave(antiga), Texto = null }, AutoresDaLista.Presidencia);

        NomesPagos(escolha.Should().BeOfType<ListaEncontrada>().Which.Lista).Should().Equal("Antigo");
    }

    [Fact]
    public async Task NaoDeveEncontrarListaQuandoAMensagemCitadaNaoEhUmaLista()
    {
        _bot.Historico.Com(ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromMinutes(5), "Recente"));

        ListaEscolhida escolha = await Selecionar(Citar("bora que amanhã tem jogo"), AutoresDaLista.Presidencia);

        escolha.Should().BeOfType<ListaNaoEncontrada>();
    }

    // Deferred Decisions ("Texto completo da mensagem citada": sem o texto no webhook, busca pelo ID)
    [Fact]
    public async Task DeveBuscarAMensagemCitadaQuandoSeuTextoNaoVeioComOComando()
    {
        MensagemDoGrupo lista = ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromHours(1), "Buscado");
        _bot.Historico.Com(lista);

        ListaEscolhida escolha =
            await Selecionar(new MensagemCitada { Id = Chave(lista), Texto = null }, AutoresDaLista.QualquerUm);

        ListaEncontrada encontrada = escolha.Should().BeOfType<ListaEncontrada>().Which;
        NomesPagos(encontrada.Lista).Should().Equal("Buscado");
        encontrada.Origem.Should().BeNull();
        _bot.Historico.ConsultasDeMensagem.Should().Equal(Chave(lista));
    }

    // A citada buscada pelo ID também precisa ser lista
    [Fact]
    public async Task NaoDeveEncontrarListaQuandoAMensagemCitadaBuscadaPeloIdNaoTemTexto()
    {
        MensagemDoGrupo figurinha = new ConstrutorDeMensagemDoGrupo().ComTexto(null).Build();
        _bot.Historico.Com(figurinha);

        ListaEscolhida escolha =
            await Selecionar(new MensagemCitada { Id = Chave(figurinha), Texto = null }, AutoresDaLista.Presidencia);

        escolha.Should().BeOfType<ListaNaoEncontrada>();
    }

    [Fact]
    public async Task DeveDizerQueAListaCitadaEhIlegivelENaoPegarOutraListaQuandoSeuTextoNaoPodeSerBuscado()
    {
        _bot.Historico.Com(ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromMinutes(5), "Outra"));

        ListaEscolhida escolha =
            await Selecionar(new MensagemCitada { Id = "3EB0FFFFFFFFFFFF", Texto = null }, AutoresDaLista.Presidencia);

        escolha.Should().BeOfType<ListaCitadaIlegivel>();
        _bot.Historico.ConsultasDeHistorico.Should().BeEmpty();
    }

    // WAHA fora do ar
    [Fact]
    public async Task DeveDizerQueAListaCitadaEhIlegivelQuandoOHistoricoEstaIndisponivel()
    {
        MensagemDoGrupo lista = ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromMinutes(5), "Citado");
        _bot.Historico.Com(lista);
        _bot.Historico.Indisponivel = true;

        ListaEscolhida escolha =
            await Selecionar(new MensagemCitada { Id = Chave(lista), Texto = " " }, AutoresDaLista.Presidencia);

        escolha.Should().BeOfType<ListaCitadaIlegivel>();
    }

    [Theory]
    [InlineData(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente)]
    [InlineData(ConstrutorDeCasoDeUso.LidDoPresidente)]
    [InlineData(ConstrutorDeCasoDeUso.IdDoTelefoneDoVice)]
    public async Task DeveUsarAListaMaisRecenteDaPresidenciaComSuaOrigemQuandoOSorteioNaoCitaNada(string idDoRemetente)
    {
        DateTimeOffset enviadaEm = ConstrutorDeCasoDeUso.Agora - TimeSpan.FromHours(2);
        _bot.Historico.Com(
            new ConstrutorDeMensagemDoGrupo().De(idDoRemetente, NomeDoPresidente).EnviadaEm(enviadaEm).ComTexto(TextoDaLista("Paco")).Build());

        ListaEscolhida escolha = await Selecionar(citada: null, AutoresDaLista.Presidencia);

        ListaEncontrada encontrada = escolha.Should().BeOfType<ListaEncontrada>().Which;
        NomesPagos(encontrada.Lista).Should().Equal("Paco");
        encontrada.Origem.Should().Be(new OrigemDaLista { NomeDoRemetente = NomeDoPresidente, EnviadaEm = enviadaEm });
    }

    // A lista mais nova do grupo é de outra pessoa, inclusive outro admin
    [Fact]
    public async Task DeveIgnorarUmaListaMaisNovaDeAlguemForaDaPresidenciaQuandoOSorteioNaoCitaNada()
    {
        MensagemDoGrupo listaDoPresidente = new ConstrutorDeMensagemDoGrupo()
            .De(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, NomeDoPresidente)
            .EnviadaEm(ConstrutorDeCasoDeUso.Agora - TimeSpan.FromHours(3))
            .ComTexto(new ConstrutorDeTextoDaListaDoGrupo().ComJogadoresPagos("Paco").ComJogadoresNaoPagos("Samuel").Build())
            .Build();
        MensagemDoGrupo repostagemDoIntegrante = new ConstrutorDeMensagemDoGrupo()
            .De(ConstrutorDeCasoDeUso.IdDoIntegrante, NomeDoIntegrante)
            .EnviadaEm(ConstrutorDeCasoDeUso.Agora - TimeSpan.FromHours(1))
            .ComTexto(new ConstrutorDeTextoDaListaDoGrupo().ComJogadoresNaoPagos("Paco").ComJogadoresPagos("Samuel").Build())
            .Build();
        _bot.Historico.Com(listaDoPresidente, repostagemDoIntegrante);

        ListaEscolhida escolha = await Selecionar(citada: null, AutoresDaLista.Presidencia);

        ListaDoGrupo lista = escolha.Should().BeOfType<ListaEncontrada>().Which.Lista;
        lista.Jogadores.Should().Equal(
            new JogadorListado { Nome = "Paco", Pago = true },
            new JogadorListado { Nome = "Samuel", Pago = false });
    }

    [Fact]
    public async Task DeveUsarAListaMaisNovaDeQualquerUmQuandoPendentesNaoCitaNada()
    {
        DateTimeOffset enviadaPeloIntegranteEm = ConstrutorDeCasoDeUso.Agora - TimeSpan.FromHours(1);
        _bot.Historico.Com(
            ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromHours(3), "Do Presidente"),
            new ConstrutorDeMensagemDoGrupo()
                .De(ConstrutorDeCasoDeUso.IdDoIntegrante, NomeDoIntegrante)
                .EnviadaEm(enviadaPeloIntegranteEm)
                .ComTexto(TextoDaLista("Do Integrante"))
                .Build());

        ListaEscolhida escolha = await Selecionar(citada: null, AutoresDaLista.QualquerUm);

        ListaEncontrada encontrada = escolha.Should().BeOfType<ListaEncontrada>().Which;
        NomesPagos(encontrada.Lista).Should().Equal("Do Integrante");
        encontrada.Origem.Should().Be(new OrigemDaLista { NomeDoRemetente = NomeDoIntegrante, EnviadaEm = enviadaPeloIntegranteEm });
    }

    [Fact]
    public async Task NaoDeveEncontrarListaQuandoAPresidenciaNaoPostouNenhumaNasUltimasSetentaEDuasHoras()
    {
        _bot.Historico.Com(
            ListaDoPresidente(enviadaHaQuanto: _idadeMaximaDaLista + TimeSpan.FromMinutes(1), "Velha"),
            new ConstrutorDeMensagemDoGrupo()
                .De(ConstrutorDeCasoDeUso.IdDoIntegrante, NomeDoIntegrante)
                .EnviadaEm(ConstrutorDeCasoDeUso.Agora - TimeSpan.FromHours(1))
                .ComTexto(TextoDaLista("De Outro"))
                .Build(),
            new ConstrutorDeMensagemDoGrupo()
                .De(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, NomeDoPresidente)
                .EnviadaEm(ConstrutorDeCasoDeUso.Agora - TimeSpan.FromHours(1))
                .Build());

        ListaEscolhida escolha = await Selecionar(citada: null, AutoresDaLista.Presidencia);

        escolha.Should().BeOfType<ListaNaoEncontrada>();
    }

    // Exatamente 72 h ainda vale; 72 h e 1 s não
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task DeveContarAsSetentaEDuasHorasEmInstantesUtcQuandoAListaEstaNoLimite(int segundosApósOLimite, bool encontradaEsperada)
    {
        _bot.Historico.Com(ListaDoPresidente(enviadaHaQuanto: _idadeMaximaDaLista + TimeSpan.FromSeconds(segundosApósOLimite), "No Limite"));

        ListaEscolhida escolha = await Selecionar(citada: null, AutoresDaLista.Presidencia);

        (escolha is ListaEncontrada).Should().Be(encontradaEsperada);
    }

    // Mais recente = hora de envio no WhatsApp, não a que chegou por último
    [Fact]
    public async Task DevePegarAUltimaHoraDeEnvioQuandoUmaListaMaisAntigaChegouPorUltimo()
    {
        _bot.Historico.Com(
            ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromHours(1), "Enviada Depois"),
            ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromHours(2), "Chegou Depois"));

        ListaEscolhida escolha = await Selecionar(citada: null, AutoresDaLista.Presidencia);

        NomesPagos(escolha.Should().BeOfType<ListaEncontrada>().Which.Lista).Should().Equal("Enviada Depois");
    }

    // Empate de horário: vale a que chegou por último
    [Fact]
    public async Task DevePegarAQueChegouPorUltimoQuandoDuasListasTemAMesmaHoraDeEnvio()
    {
        _bot.Historico.Com(
            ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromHours(1), "Chegou Antes"),
            ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromHours(1), "Chegou Por Ultimo"));

        ListaEscolhida escolha = await Selecionar(citada: null, AutoresDaLista.Presidencia);

        NomesPagos(escolha.Should().BeOfType<ListaEncontrada>().Which.Lista).Should().Equal("Chegou Por Ultimo");
    }

    // O histórico é lido só nas últimas 72 h
    [Fact]
    public async Task DeveLerOHistoricoDasUltimasSetentaEDuasHorasQuandoOComandoNaoCitaNada()
    {
        await Selecionar(citada: null, AutoresDaLista.QualquerUm);

        _bot.Historico.ConsultasDeHistorico.Should().Equal(ConstrutorDeCasoDeUso.Agora - _idadeMaximaDaLista);
    }

    // Edge Cases ("Lista não encontrada": sem como ver o histórico, o bot pede para citar)
    [Fact]
    public async Task NaoDeveEncontrarListaQuandoOHistoricoEstaIndisponivel()
    {
        _bot.Historico.Com(ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromHours(1), "Paco"));
        _bot.Historico.Indisponivel = true;

        ListaEscolhida escolha = await Selecionar(citada: null, AutoresDaLista.Presidencia);

        escolha.Should().BeOfType<ListaNaoEncontrada>();
    }

    [Fact]
    public async Task DeveManterNomesETextoDaListaForaDoLogQuandoEscolheUmaLista()
    {
        _bot.Historico.Com(ListaDoPresidente(enviadaHaQuanto: TimeSpan.FromHours(1), "Paco Sigiloso"));

        await Selecionar(citada: null, AutoresDaLista.Presidencia);
        await Selecionar(Citar("texto qualquer"), AutoresDaLista.Presidencia);

        _bot.Logs.Entradas.Should().NotBeEmpty().And.AllSatisfy(log =>
            log.Mensagem.Should().NotContainAny("Paco Sigiloso", NomeDoPresidente, "Lista de jogadores", "texto qualquer", "35600000101"));
    }

    private Task<ListaEscolhida> Selecionar(MensagemCitada? citada, AutoresDaLista autores) =>
        _bot.ConstruirEscolhaDaLista().SelecionarAsync(IdDoGrupo, citada, autores, TestContext.Current.CancellationToken);

    private static MensagemDoGrupo ListaDoPresidente(TimeSpan enviadaHaQuanto, string nomePago) => new ConstrutorDeMensagemDoGrupo()
        .De(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, NomeDoPresidente)
        .EnviadaEm(ConstrutorDeCasoDeUso.Agora - enviadaHaQuanto)
        .ComTexto(TextoDaLista(nomePago))
        .Build();

    private static string TextoDaLista(string nomePago) => new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresPagos(nomePago).Build();

    private static MensagemCitada Citar(string texto) => new() { Id = "3EB0AAAAAAAAAAAA", Texto = texto };

    private static string Chave(MensagemDoGrupo mensagem) => mensagem.Id.Split('_')[2];

    private static IEnumerable<string> NomesPagos(ListaDoGrupo lista) => lista.Jogadores.Where(jogador => jogador.Pago).Select(jogador => jogador.Nome);
}
