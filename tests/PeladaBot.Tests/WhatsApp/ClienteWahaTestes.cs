using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using PeladaBot.Tests.UtilitariosDeTeste;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.WhatsApp;

/// <summary>
/// O ClienteWaha real, com o HttpClient e o pipeline de resiliência que o Program.cs registra, contra um stub do WAHA. Caminhos
/// e corpos seguem o documento OpenAPI do WAHA 2026.9.2 fixado.
/// </summary>
public sealed class ClienteWahaTestes : IAsyncDisposable
{
    private const string IdDoGrupo = FabricaDaAplicacaoDoBot.IdDoGrupo;
    private const string IdDoComando = "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000101@c.us";
    private const string ChaveDaLista = "3EB05B1F2A9C4D7E0B02";
    private const string CaminhoDeMensagens = "/api/default/chats/120363000000001234%40g.us/messages";

    private static readonly DateTimeOffset _desde = new(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);

    private readonly FabricaDaAplicacaoDoBot _fabrica = new();
    private readonly AsyncServiceScope _escopo;
    private readonly ClienteWaha _cliente;

    public ClienteWahaTestes()
    {
        _escopo = _fabrica.Services.CreateAsyncScope();
        _cliente = _escopo.ServiceProvider.GetRequiredService<ClienteWaha>();
    }

    public async ValueTask DisposeAsync()
    {
        await _escopo.DisposeAsync();
        await _fabrica.DisposeAsync();
    }

    // O texto responde ao comando, citando-o
    [Fact]
    public async Task DeveEnviarOTextoRespondendoAoComandoComAChaveDaApiQuandoEnviaUmTexto()
    {
        ResultadoDoEnvio resultado = await _cliente.EnviarTextoAsync(IdDoGrupo, "Responde a lista com !sortear", IdDoComando, TestContext.Current.CancellationToken);

        resultado.Should().BeOfType<Enviado>();
        PedidoWaha pedido = _fabrica.Waha.PedidosPara("/api/sendText").Should().ContainSingle().Which;
        pedido.Metodo.Should().Be("POST");
        pedido.ChaveDaApi.Should().Be(FabricaDaAplicacaoDoBot.ChaveDaApiDoWaha);
        pedido.TipoDeConteudo.Should().Be("application/json");
        DeveSerJson(pedido, """
            {
              "session": "default",
              "chatId": "120363000000001234@g.us",
              "text": "Responde a lista com !sortear",
              "reply_to": "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000101@c.us",
              "linkPreview": false
            }
            """);
    }

    // Uma mensagem só: a imagem com a legenda, citando o comando
    [Fact]
    public async Task DeveEnviarAImagemEmbutidaComSuaLegendaRespondendoAoComandoQuandoEnviaUmaImagem()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        ResultadoDoEnvio resultado = await _cliente.EnviarImagemAsync(IdDoGrupo, png, "⚽ Sorteio das 21:04", IdDoComando, TestContext.Current.CancellationToken);

        resultado.Should().BeOfType<Enviado>();
        PedidoWaha pedido = _fabrica.Waha.PedidosPara("/api/sendImage").Should().ContainSingle().Which;
        pedido.Metodo.Should().Be("POST");
        pedido.ChaveDaApi.Should().Be(FabricaDaAplicacaoDoBot.ChaveDaApiDoWaha);
        DeveSerJson(pedido, $$"""
            {
              "session": "default",
              "chatId": "120363000000001234@g.us",
              "file": { "mimetype": "image/png", "filename": "times.png", "data": "{{Convert.ToBase64String(png)}}" },
              "caption": "⚽ Sorteio das 21:04",
              "reply_to": "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000101@c.us"
            }
            """);
    }

    // O segundo card vai sem legenda, sem o campo, ainda citando o comando
    [Fact]
    public async Task DeveEnviarAImagemSemOCampoDeLegendaQuandoEnviaUmCardSemLegenda()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        ResultadoDoEnvio resultado = await _cliente.EnviarImagemAsync(IdDoGrupo, png, legenda: null, IdDoComando, TestContext.Current.CancellationToken);

        resultado.Should().BeOfType<Enviado>();
        PedidoWaha pedido = _fabrica.Waha.PedidosPara("/api/sendImage").Should().ContainSingle().Which;
        DeveSerJson(pedido, $$"""
            {
              "session": "default",
              "chatId": "120363000000001234@g.us",
              "file": { "mimetype": "image/png", "filename": "times.png", "data": "{{Convert.ToBase64String(png)}}" },
              "reply_to": "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000101@c.us"
            }
            """);
    }

    [Theory]
    [InlineData(true, "/api/startTyping")]
    [InlineData(false, "/api/stopTyping")]
    public async Task DeveEnviarOChatParaOEndpointDeDigitacaoQuandoMostraOuEscondeADigitacao(bool iniciar, string caminho)
    {
        bool feito = iniciar
            ? await _cliente.IniciarDigitandoAsync(IdDoGrupo, TestContext.Current.CancellationToken)
            : await _cliente.PararDigitandoAsync(IdDoGrupo, TestContext.Current.CancellationToken);

        feito.Should().BeTrue();
        PedidoWaha pedido = _fabrica.Waha.PedidosPara(caminho).Should().ContainSingle().Which;
        pedido.Metodo.Should().Be("POST");
        pedido.ChaveDaApi.Should().Be(FabricaDaAplicacaoDoBot.ChaveDaApiDoWaha);
        DeveSerJson(pedido, """{ "session": "default", "chatId": "120363000000001234@g.us" }""");
    }

    [Fact]
    public async Task DeveDizerQueADigitacaoNaoFoiMostradaQuandoOWahaARecusa()
    {
        _fabrica.Waha.Responder(HttpMethod.Post, "/api/startTyping", HttpStatusCode.UnprocessableEntity, """{"error":"Session status is not as expected"}""");

        bool mostrada = await _cliente.IniciarDigitandoAsync(IdDoGrupo, TestContext.Current.CancellationToken);

        mostrada.Should().BeFalse();
    }

    // O WAHA recusou: não saiu
    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task DeveReportarOEnvioComoRejeitadoQuandoOWahaRespondeUmErro(HttpStatusCode status)
    {
        _fabrica.Waha.Responder(HttpMethod.Post, "/api/sendImage", status, """{"error":"Session status is not as expected"}""");

        ResultadoDoEnvio resultado = await _cliente.EnviarImagemAsync(IdDoGrupo, [1, 2, 3], "legenda", IdDoComando, TestContext.Current.CancellationToken);

        resultado.Should().Be(new Rejeitado { Motivo = $"WAHA respondeu {(int)status}" });
    }

    // A conexão nem abriu: não saiu
    [Fact]
    public async Task DeveReportarOEnvioComoRejeitadoQuandoOWahaNaoPodeSerAlcancado()
    {
        _fabrica.Waha.Programar(HttpMethod.Post, "/api/sendImage", () => throw new HttpRequestException(HttpRequestError.ConnectionError, "Connection refused"));

        ResultadoDoEnvio resultado = await _cliente.EnviarImagemAsync(IdDoGrupo, [1, 2, 3], "legenda", IdDoComando, TestContext.Current.CancellationToken);

        resultado.Should().BeOfType<Rejeitado>();
    }

    // Pedido enviado e sem resposta: incerto
    [Fact]
    public async Task DeveReportarOEnvioComoIncertoQuandoAConexaoCaiAntesDaResposta()
    {
        _fabrica.Waha.Programar(HttpMethod.Post, "/api/sendImage", () => throw new HttpRequestException(HttpRequestError.ResponseEnded, "The response ended prematurely"));

        ResultadoDoEnvio resultado = await _cliente.EnviarImagemAsync(IdDoGrupo, [1, 2, 3], "legenda", IdDoComando, TestContext.Current.CancellationToken);

        resultado.Should().BeOfType<Incerto>();
    }

    // Timeout depois de enviar: incerto
    [Fact]
    public async Task DeveReportarOEnvioComoIncertoQuandoOWahaNaoRespondeATempo()
    {
        _fabrica.Waha.Programar(HttpMethod.Post, "/api/sendImage", () => throw new TaskCanceledException("The request was canceled due to the configured timeout."));

        ResultadoDoEnvio resultado = await _cliente.EnviarImagemAsync(IdDoGrupo, [1, 2, 3], "legenda", IdDoComando, TestContext.Current.CancellationToken);

        resultado.Should().Be(new Incerto { Motivo = "WAHA não respondeu a tempo" });
    }

    // Um envio nunca é repetido, nem numa falha que uma leitura repetiria: repetir pode postar duas vezes
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task NuncaDeveRepetirUmEnvioQuandoOWahaRespondeUmErroTransitorio(HttpStatusCode status)
    {
        _fabrica.Waha
            .Responder(HttpMethod.Post, "/api/sendText", status)
            .Responder(HttpMethod.Post, "/api/sendImage", status);

        await _cliente.EnviarTextoAsync(IdDoGrupo, "texto", IdDoComando, TestContext.Current.CancellationToken);
        await _cliente.EnviarImagemAsync(IdDoGrupo, [1, 2, 3], "legenda", IdDoComando, TestContext.Current.CancellationToken);

        _fabrica.Waha.PedidosPara("/api/sendText").Should().ContainSingle();
        _fabrica.Waha.PedidosPara("/api/sendImage").Should().ContainSingle();
    }

    // Nem quando a conexão cai depois de enviar
    [Fact]
    public async Task NuncaDeveRepetirUmEnvioQuandoAConexaoCai()
    {
        _fabrica.Waha.Programar(HttpMethod.Post, "/api/sendImage", () => throw new HttpRequestException(HttpRequestError.ResponseEnded));

        await _cliente.EnviarImagemAsync(IdDoGrupo, [1, 2, 3], "legenda", IdDoComando, TestContext.Current.CancellationToken);

        _fabrica.Waha.PedidosPara("/api/sendImage").Should().ContainSingle();
    }

    // Deferred Decisions ("Como achar a lista": histórico do grupo pela API do WAHA)
    [Fact]
    public async Task DeveLerOHistoricoDoChatDesdeOInstanteDadoERetornarMaisAntigoPrimeiro()
    {
        _fabrica.Waha.Programar(
            HttpMethod.Get,
            CaminhoDeMensagens,
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, FixturesWaha.Ler(FixturesWaha.MensagensDoChat)),
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, "[]"));

        IReadOnlyList<MensagemDoGrupo>? mensagens = await _cliente.ObterMensagensDesdeAsync(IdDoGrupo, _desde, TestContext.Current.CancellationToken);

        PedidoWaha pedido = _fabrica.Waha.PedidosPara(CaminhoDeMensagens)[0];
        pedido.Metodo.Should().Be("GET");
        pedido.ChaveDaApi.Should().Be(FabricaDaAplicacaoDoBot.ChaveDaApiDoWaha);
        Query(pedido).Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["limit"] = "100",
            ["offset"] = "0",
            ["sortBy"] = "timestamp",
            ["sortOrder"] = "desc",
            ["downloadMedia"] = "false",
            ["filter.fromMe"] = "false",
            ["filter.timestamp.gte"] = _desde.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
        });
        mensagens.Should().SatisfyRespectively(
            lista =>
            {
                lista.Id.Should().Be($"false_120363000000001234@g.us_{ChaveDaLista}_35600000101@c.us");
                lista.IdDoRemetente.Should().Be("35600000101@c.us");
                lista.NomeDoRemetente.Should().Be("Presidente Fulano");
                lista.EnviadaEm.Should().Be(new DateTimeOffset(2026, 10, 12, 11, 29, 0, TimeSpan.Zero));
                lista.Texto.Should().StartWith("⚽PRÓXIMO JOGO⚽").And.Contain("Lista de jogadores:");
            },
            chat => chat.Texto.Should().Be("bora que amanhã tem jogo"));
    }

    // Deferred Decisions ("paginando até cobrir 72 h")
    [Fact]
    public async Task DeveLerAProximaPaginaQuandoUmaPaginaChegaCheia()
    {
        string paginaCheia = JsonSerializer.Serialize(Enumerable.Range(0, ClienteWaha.TamanhoDaPaginaDoHistorico).Select(indice => new
        {
            id = $"false_120363000000001234@g.us_3EB0{indice:X12}_35600000404@c.us",
            timestamp = 1791999000 - indice,
            from = IdDoGrupo,
            fromMe = false,
            participant = "35600000404@c.us",
            body = "mensagem",
        }));
        _fabrica.Waha.Programar(
            HttpMethod.Get,
            CaminhoDeMensagens,
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, paginaCheia),
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, FixturesWaha.Ler(FixturesWaha.MensagensDoChat)),
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, "[]"));

        IReadOnlyList<MensagemDoGrupo>? mensagens = await _cliente.ObterMensagensDesdeAsync(IdDoGrupo, _desde, TestContext.Current.CancellationToken);

        _fabrica.Waha.PedidosPara(CaminhoDeMensagens).Select(pedido => Query(pedido)["offset"]).Should().Equal("0", "100", "200");
        mensagens.Should().HaveCount(ClienteWaha.TamanhoDaPaginaDoHistorico + 2).And.BeInAscendingOrder(mensagem => mensagem.EnviadaEm);
    }

    // WAHA tira reações e edições depois de aplicar o limit: uma página curta não é o fim do histórico
    [Fact]
    public async Task DeveLerAProximaPaginaQuandoUmaPaginaChegaCurtaMasTrazMensagens()
    {
        _fabrica.Waha.Programar(
            HttpMethod.Get,
            CaminhoDeMensagens,
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, FixturesWaha.Ler(FixturesWaha.MensagensDoChat)),
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, PaginaDe("3EB0000000000000AA01", 1791990000)),
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, "[]"));

        IReadOnlyList<MensagemDoGrupo>? mensagens = await _cliente.ObterMensagensDesdeAsync(IdDoGrupo, _desde, TestContext.Current.CancellationToken);

        _fabrica.Waha.PedidosPara(CaminhoDeMensagens).Select(pedido => Query(pedido)["offset"]).Should().Equal("0", "100", "200");
        mensagens.Should().HaveCount(3).And.BeInAscendingOrder(mensagem => mensagem.EnviadaEm);
    }

    // Se o WAHA ignorar o offset, a mesma página volta: ela não traz nada novo e a leitura para
    [Fact]
    public async Task DeveParaDeLerQuandoUmaPaginaNaoTrazNadaNovo()
    {
        _fabrica.Waha.Responder(HttpMethod.Get, CaminhoDeMensagens, HttpStatusCode.OK, FixturesWaha.Ler(FixturesWaha.MensagensDoChat));

        IReadOnlyList<MensagemDoGrupo>? mensagens = await _cliente.ObterMensagensDesdeAsync(IdDoGrupo, _desde, TestContext.Current.CancellationToken);

        _fabrica.Waha.PedidosPara(CaminhoDeMensagens).Should().HaveCount(2);
        mensagens.Should().HaveCount(2);
    }

    // Uma leitura é idempotente: a falha passageira é tentada de novo
    [Fact]
    public async Task DeveTentarDeNovoUmaLeituraQuandoOWahaFalhaUmaVez()
    {
        _fabrica.Waha.Programar(
            HttpMethod.Get,
            CaminhoDeMensagens,
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.ServiceUnavailable, "{}"),
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, FixturesWaha.Ler(FixturesWaha.MensagensDoChat)),
            () => ManipuladorFalsoDoWaha.Json(HttpStatusCode.OK, "[]"));

        IReadOnlyList<MensagemDoGrupo>? mensagens = await _cliente.ObterMensagensDesdeAsync(IdDoGrupo, _desde, TestContext.Current.CancellationToken);

        mensagens.Should().HaveCount(2);
        _fabrica.Waha.PedidosPara(CaminhoDeMensagens).Select(pedido => Query(pedido)["offset"]).Should().Equal("0", "0", "100");
    }

    // Nem o cliente HTTP nem a retentativa levam o ID do grupo ou da mensagem para o log
    [Fact]
    public async Task DeveManterOsIdsDoGrupoEDaMensagemForaDoLogQuandoUmaLeituraEhRetentadaEFalha()
    {
        _fabrica.Waha.Responder(HttpMethod.Get, CaminhoDeMensagens, HttpStatusCode.ServiceUnavailable);
        _fabrica.Waha.Responder(HttpMethod.Get, $"{CaminhoDeMensagens}/{ChaveDaLista}", HttpStatusCode.ServiceUnavailable);

        await _cliente.ObterMensagensDesdeAsync(IdDoGrupo, _desde, TestContext.Current.CancellationToken);
        await _cliente.ObterMensagemAsync(IdDoGrupo, ChaveDaLista, TestContext.Current.CancellationToken);
        await _cliente.EnviarTextoAsync(IdDoGrupo, "texto", IdDoComando, TestContext.Current.CancellationToken);

        _fabrica.Logs.Should().Contain(log => log.Mensagem == "Chamada ao WAHA obter mensagens falhou: WAHA respondeu 503")
            .And.AllSatisfy(log => log.Mensagem.Should().NotContainAny("120363000000001234", ChaveDaLista, "35600000101", "texto"));
    }

    // Edge Cases ("Lista não encontrada": sem histórico, o bot pede para citar)
    [Fact]
    public async Task DeveDizerQueOHistoricoNaoPodeSerLidoQuandoOWahaORecusa()
    {
        _fabrica.Waha.Responder(HttpMethod.Get, CaminhoDeMensagens, HttpStatusCode.UnprocessableEntity, """{"error":"Session status is not as expected"}""");

        IReadOnlyList<MensagemDoGrupo>? mensagens = await _cliente.ObterMensagensDesdeAsync(IdDoGrupo, _desde, TestContext.Current.CancellationToken);

        mensagens.Should().BeNull();
    }

    // Deferred Decisions ("Texto completo da mensagem citada": buscar a mensagem pelo ID)
    [Fact]
    public async Task DeveObterAMensagemCitadaPeloSeuIdQuandoOWahaATem()
    {
        string caminho = $"{CaminhoDeMensagens}/{ChaveDaLista}";
        _fabrica.Waha.Responder(HttpMethod.Get, caminho, HttpStatusCode.OK, FixturesWaha.Ler(FixturesWaha.MensagemDoChat));

        MensagemDoGrupo? mensagem = await _cliente.ObterMensagemAsync(IdDoGrupo, ChaveDaLista, TestContext.Current.CancellationToken);

        PedidoWaha pedido = _fabrica.Waha.PedidosPara(caminho).Should().ContainSingle().Which;
        pedido.ChaveDaApi.Should().Be(FabricaDaAplicacaoDoBot.ChaveDaApiDoWaha);
        pedido.Query.Should().Be("?downloadMedia=false");
        mensagem.Should().BeEquivalentTo(new { IdDoRemetente = "35600000101@c.us", NomeDoRemetente = "Presidente Fulano" });
        (mensagem?.Texto).Should().Contain("Lista de jogadores:");
    }

    [Fact]
    public async Task DeveDizerQueAMensagemEstaFaltandoQuandoOWahaNaoATem()
    {
        MensagemDoGrupo? mensagem = await _cliente.ObterMensagemAsync(IdDoGrupo, "3EB0FFFFFFFFFFFF", TestContext.Current.CancellationToken);

        mensagem.Should().BeNull();
    }

    [Fact]
    public async Task DeveLerOStatusDaSessaoQuandoPedeASessao()
    {
        SessaoWaha? sessao = await _cliente.ObterSessaoAsync(TestContext.Current.CancellationToken);

        sessao.Should().Be(new SessaoWaha { Nome = "default", Status = "WORKING" });
        _fabrica.Waha.PedidosPara("/api/sessions/default").Should().NotBeEmpty().And.AllSatisfy(pedido =>
        {
            pedido.Metodo.Should().Be("GET");
            pedido.ChaveDaApi.Should().Be(FabricaDaAplicacaoDoBot.ChaveDaApiDoWaha);
        });
    }

    private static void DeveSerJson(PedidoWaha pedido, string esperado)
    {
        JsonNode? corpo = JsonNode.Parse(pedido.Corpo ?? "null");

        JsonNode.DeepEquals(corpo, JsonNode.Parse(esperado)).Should().BeTrue($"o WAHA deveria receber {esperado}, não {pedido.Corpo}");
    }

    private static Dictionary<string, string> Query(PedidoWaha pedido) =>
        QueryHelpers.ParseQuery(pedido.Query).ToDictionary(par => par.Key, par => par.Value.ToString());

    /// <summary>Uma página de histórico com uma mensagem de chat de um integrante, como o WAHA a responde.</summary>
    private static string PaginaDe(string chave, long timestamp) => JsonSerializer.Serialize(new[]
    {
        new
        {
            id = $"false_120363000000001234@g.us_{chave}_35600000404@c.us",
            timestamp,
            from = IdDoGrupo,
            fromMe = false,
            participant = "35600000404@c.us",
            body = "mensagem antiga",
        },
    });
}
