using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PeladaBot.CasosDeUso.Webhooks.ReceberEventoDoWaha;
using PeladaBot.Comandos;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Mensageria;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.CasosDeUso.Webhooks;

public sealed class CasoDeUsoReceberEventoDoWahaTestes : IDisposable
{
    private const string IdDoComando = "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000101@c.us";

    /// <summary>Quando o comando da fixture foi enviado: 2026-10-14T18:59:58Z.</summary>
    private static readonly DateTimeOffset _comandoEnviadoEm = DateTimeOffset.FromUnixTimeSeconds(1792004398);

    private readonly FakeTimeProvider _relogio = new(_comandoEnviadoEm + TimeSpan.FromSeconds(2));
    private readonly FilaDeComandos _fila = new();
    private readonly ColetorDeLogs _logs = new();
    private readonly ILoggerFactory _loggerFactory;

    public CasoDeUsoReceberEventoDoWahaTestes()
    {
        _loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(_logs).SetMinimumLevel(LogLevel.Trace));
    }

    public void Dispose() => _loggerFactory.Dispose();

    // O comando entra na fila com o que a resposta precisa
    [Fact]
    public async Task DeveEnfileirarOComandoComSuaCitacaoQuandoOPresidenteRespondeAUmaListaNoGrupo()
    {
        Receber(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));

        ComandoRecebido enfileirado = (await Enfileirados()).Should().ContainSingle().Which;
        enfileirado.Comando.Should().Be(TipoDeComando.Sortear);
        enfileirado.IdDaMensagem.Should().Be(IdDoComando);
        enfileirado.IdDoGrupo.Should().Be(ConstrutorDeCasoDeUso.IdDoGrupo);
        enfileirado.IdDoRemetente.Should().Be(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente);
        MensagemCitada citada = enfileirado.Citada.Should().BeOfType<MensagemCitada>().Which;
        citada.Id.Should().Be("3EB05B1F2A9C4D7E0B02");
        citada.Texto.Should().StartWith("⚽PRÓXIMO JOGO⚽").And.Contain("Lista de jogadores:");
    }

    // Deferred Decisions ("Texto completo da mensagem citada": o webhook pode vir sem o texto)
    [Fact]
    public async Task DeveEnfileirarACitacaoSemTextoQuandoOWebhookNaoOTrouxer()
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        RespondeA(webhook).Remove("body");

        Receber(webhook);

        MensagemCitada? citada = (await Enfileirados()).Should().ContainSingle().Which.Citada;
        citada.Should().Be(new MensagemCitada { Id = "3EB05B1F2A9C4D7E0B02", Texto = null });
    }

    [Theory]
    [InlineData("120363999999999999@g.us")]
    [InlineData("35600000101@c.us")]
    [InlineData("100000000000202@lid")]
    public async Task NaoDeveFazerNadaQuandoAMensagemVemDeOutroGrupoOuDeUmChatPrivado(string idDoChat)
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        Payload(webhook)["from"] = idDoChat;

        Receber(webhook);

        (await Enfileirados()).Should().BeEmpty();
    }

    [Fact]
    public async Task NaoDeveFazerNadaQuandoOProprioBotEnviouAMensagem()
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        Payload(webhook)["fromMe"] = true;

        Receber(webhook);

        (await Enfileirados()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(120, 1)]
    [InlineData(121, 0)]
    public async Task DeveDescartarOComandoQuandoEleTemMaisDeCentoEVinteSegundosQuandoChega(int idadeEmSegundos, int enfileiradosEsperados)
    {
        _relogio.SetUtcNow(_comandoEnviadoEm + TimeSpan.FromSeconds(idadeEmSegundos));

        Receber(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));

        (await Enfileirados()).Should().HaveCount(enfileiradosEsperados);
    }

    // Comando antigo cai com log, sem dado pessoal
    [Fact]
    public void DeveLogarODescarteComOIdMascaradoQuandoOComandoEhMuitoAntigo()
    {
        _relogio.SetUtcNow(_comandoEnviadoEm + TimeSpan.FromMinutes(5));

        Receber(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));

        _logs.Entradas.Should().ContainSingle().Which.Mensagem.Should().Be("Sortear ***0A01 descartado: 00:05:00 de idade quando chegou");
    }

    [Theory]
    [InlineData("!sortearabc")]
    [InlineData("valeu, presidente")]
    [InlineData("bora !sortear")]
    public async Task NaoDeveFazerNadaQuandoOTextoDaMensagemNaoComecaComUmComando(string corpo)
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        Payload(webhook)["body"] = corpo;
        RespondeA(webhook)["body"] = "!sortear";

        Receber(webhook);

        (await Enfileirados()).Should().BeEmpty();
    }

    // Comando sem diferenciar maiúsculas nem acentos
    [Fact]
    public async Task DeveEnfileirarOSorteioQuandoOComandoTemOutraCaixaEAcentos()
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        Payload(webhook)["body"] = "!SORTÉAR";

        Receber(webhook);

        (await Enfileirados()).Should().ContainSingle().Which.Comando.Should().Be(TipoDeComando.Sortear);
    }

    // Mensagem editada nunca dispara comando: chega como message.edited
    [Fact]
    public async Task NaoDeveFazerNadaQuandoAMensagemFoiEditadaParaUmComando()
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        webhook["event"] = "message.edited";
        Payload(webhook)["editedMessageId"] = "3EB0C767D26A1D8E0A01";

        Receber(webhook);

        (await Enfileirados()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("STOPPED")]
    [InlineData("FAILED")]
    public void DeveLogarUmErroQuandoASessaoParaOuFalha(string status)
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.StatusDaSessao);
        Payload(webhook)["status"] = status;

        Receber(webhook);

        LogColetado log = _logs.Entradas.Should().ContainSingle().Which;
        log.Nivel.Should().Be(LogLevel.Error);
        log.Mensagem.Should().StartWith($"Sessão do WAHA default está {status}");
    }

    // Outros status só informam
    [Theory]
    [InlineData("WORKING")]
    [InlineData("STARTING")]
    [InlineData("SCAN_QR_CODE")]
    public void DeveSoInformarQuandoASessaoMudaParaOutroStatus(string status)
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.StatusDaSessao);
        Payload(webhook)["status"] = status;

        Receber(webhook);

        _logs.Entradas.Should().ContainSingle().Which.Nivel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void DeveDescartarComUmAvisoQuandoOCorpoNaoEhUmEventoDoWaha()
    {
        CasoDeUsoReceberEventoDoWaha casoDeUso = CriarCasoDeUso();

        casoDeUso.Executar(null);

        _logs.Entradas.Should().ContainSingle().Which.Nivel.Should().Be(LogLevel.Warning);
    }

    [Theory]
    [InlineData("""{"event":"message","payload":{"fromMe":"talvez"}}""")]
    [InlineData("""{"event":"message","payload":[]}""")]
    [InlineData("""{"event":"message"}""")]
    [InlineData("""{"event":"message","payload":{"id":"x","from":"120363000000001234@g.us","body":"!sortear"}}""")]
    [InlineData("""{"event":"session.status","payload":{"name":"default"}}""")]
    public async Task DeveDescartarComUmAvisoENaoEnfileirarNadaQuandoOPayloadEhMalformado(string json)
    {
        Receber(JsonNode.Parse(json) ?? throw new InvalidOperationException("Não é JSON."));

        (await Enfileirados()).Should().BeEmpty();
        _logs.Entradas.Should().ContainSingle().Which.Nivel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void DeveLogarOComandoEnfileiradoComSoIdsMascaradosQuandoOEnfileira()
    {
        Receber(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));

        _logs.Entradas.Should().ContainSingle().Which.Mensagem.Should()
            .Be("Sortear ***0A01 de ***0101@c.us enfileirado (cita uma mensagem: True)");
    }

    // Edge Cases ("A fila tem tamanho limitado")
    [Fact]
    public void DeveDescartarOComandoELogarUmErroQuandoAFilaEstaCheia()
    {
        for (int indice = 0; indice < FilaDeComandos.Capacidade; indice++)
        {
            Receber(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));
        }

        Receber(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));

        _logs.Entradas.Should().ContainSingle(log => log.Nivel == LogLevel.Error).Which.Mensagem.Should().Contain("fila está cheia");
    }

    private void Receber(JsonNode webhook)
    {
        PedidoReceberEventoDoWaha? pedido = webhook.Deserialize<PedidoReceberEventoDoWaha>(JsonSerializerOptions.Web);

        CriarCasoDeUso().Executar(pedido);
    }

    private static JsonObject Payload(JsonNode webhook) =>
        webhook["payload"]?.AsObject() ?? throw new InvalidOperationException("A fixture não tem payload.");

    private static JsonObject RespondeA(JsonNode webhook) =>
        Payload(webhook)["replyTo"]?.AsObject() ?? throw new InvalidOperationException("A fixture não cita nenhuma mensagem.");

    private CasoDeUsoReceberEventoDoWaha CriarCasoDeUso() => new(
        _fila,
        _relogio,
        Options.Create(new ConstrutorDeOpcoesDoBot().Build()),
        _loggerFactory.CreateLogger<CasoDeUsoReceberEventoDoWaha>());

    private async Task<IReadOnlyList<ComandoRecebido>> Enfileirados()
    {
        _fila.Completar();

        List<ComandoRecebido> enfileirados = [];

        await foreach (ComandoRecebido comando in _fila.LerTudoAsync(TestContext.Current.CancellationToken))
        {
            enfileirados.Add(comando);
        }

        return enfileirados;
    }
}
