using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PeladaBot.Filtros;
using PeladaBot.Respostas;
using PeladaBot.Tests.UtilitariosDeTeste;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.Controllers;

/// <summary>
/// O bot inteiro do webhook à resposta: um payload do WAHA é enviado para a aplicação real, assinado do jeito que o WAHA assina, e
/// o grupo falso recebe o que o bot envia. Só o WhatsApp, o relógio e o acaso são trocados.
/// </summary>
public sealed class WebhooksControllerTestes : IAsyncDisposable
{
    private const string Webhook = "/webhooks/waha";
    private const string TipoDeMidiaJson = "application/json";
    private const string IdDoComando = "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000101@c.us";
    private const string IdDaAjuda = "false_120363000000001234@g.us_3EB0E5F6A7B8C9D00A03_35600000101@c.us";

    /// <summary>Uma chave do tamanho certo que não é a do bot de teste.</summary>
    private const string OutraChave = "another-waha-webhook-hmac-key-of-at-least-32-characters";

    /// <summary>Quando o comando da fixture foi enviado: 2026-10-14T18:59:58Z, 20:59:58 no fuso da suíte.</summary>
    private static readonly DateTimeOffset _comandoEnviadoEm = DateTimeOffset.FromUnixTimeSeconds(1792004398);

    private static readonly byte[] _assinaturaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly RelogioDeTeste _relogio = new(_comandoEnviadoEm + TimeSpan.FromSeconds(2));
    private readonly MensageiroDoGrupoFalso _mensageiro;
    private readonly HistoricoDoGrupoFalso _historico = new();
    private readonly FabricaDaAplicacaoDoBot _fabrica;
    private readonly HttpClient _cliente;

    public WebhooksControllerTestes()
    {
        _mensageiro = new MensageiroDoGrupoFalso(_relogio);
        _fabrica = new FabricaDaAplicacaoDoBot().ComServicos(services =>
        {
            services.AddSingleton<TimeProvider>(_relogio);
            services.AddSingleton(new Random(14));
            services.AddSingleton<IMensageiroDoGrupo>(_mensageiro);
            services.AddSingleton<IHistoricoDoGrupo>(_historico);
        });
        _cliente = _fabrica.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
    }

    // Três times, um card por campo, a legenda só no primeiro
    [Fact]
    public async Task DeveResponderComUmCardPorCampoEALegendaNoPrimeiroCitandoOComandoQuandoOPresidenteRespondeAUmaListaComOComandoDeSorteio()
    {
        HttpResponseMessage resposta = await Postar(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));
        await _mensageiro.AvancarAteMensagensAsync(_relogio, 2);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        _mensageiro.Mensagens.Should().HaveCount(2).And.AllSatisfy(card =>
        {
            card.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Imagem);
            card.IdDoChat.Should().Be(FabricaDaAplicacaoDoBot.IdDoGrupo);
            card.IdDaMensagemRespondida.Should().Be(IdDoComando);
            card.Png.Should().StartWith(_assinaturaPng);
        });
        _mensageiro.Mensagens[1].Texto.Should().BeNull();
        ChamadaDoMensageiro envio = _mensageiro.Mensagens[0];
        envio.Texto.Should().Contain("*PELADA | ").And.Contain("21:00*\n━━━").And.NotContain("📋 Lista");
        // 13 de linha e 2 goleiros, numa quadra só: 2 times completos e um 3º incompleto, que joga com os goleiros
        // do gol e precisa de mais 2 na linha. Quem está na espera sem ✅ não aparece.
        envio.Texto.Should().Contain("(faltam 2)").And.NotContain("ficam no gol").And.NotContain("Espera:")
            .And.NotContain("Não pagou ainda").And.NotContain("*Reservas*");
    }

    [Fact]
    public async Task DeveFecharALegendaComUmaDasResenhasConfiguradasQuandoOsTimesSaoSorteados()
    {
        string[] resenhas =
        [
            "⚽ *Hoje tem jogo. A bola não espera, e o apito também não.*",
            "⏰ *Chegue no horário. O aquecimento é curto.*",
            "😂 *O sorteio é justo. O futebol acontece em campo.*",
            "🧤 *Goleiro não escolhe time. Quem escolheu foi a lista.*",
            "👕 *Colete não mente: tá na lista, tá no jogo.*",
            "⚽ *Jogo de dois tempos. Apareça nos dois.*",
            "📣 *Jogue até o apito, e chegue antes dele.*",
            "🚌 *Retranca só depois de chegar.*",
            "🏆 *Ninguém é maior que a pelada. Pagou, jogou.*",
            "⏱️ *Um jogo de cada vez. Este começa na hora.*",
        ];

        await Postar(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));
        await _mensageiro.AvancarAteMensagensAsync(_relogio, 2);

        string legenda = _mensageiro.Mensagens.Should().HaveCount(2).And.Subject.First().Texto ?? string.Empty;
        resenhas.Should().ContainSingle(resenha => legenda.EndsWith($"━━━━━━━━━━━━━━━━━━\n{resenha}", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("120363999999999999@g.us")]
    [InlineData("35600000101@c.us")]
    public async Task NaoDeveFazerNadaQuandoOComandoVemDeOutroGrupoOuDeUmChatPrivado(string idDoChat)
    {
        JsonNode outroLugar = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        Payload(outroLugar)["from"] = idDoChat;

        await Postar(outroLugar);

        await DeveSoResponderAAjudaQueSegue();
    }

    [Fact]
    public async Task NaoDeveFazerNadaQuandoOProprioBotEnviouOComando()
    {
        JsonNode doBot = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        Payload(doBot)["fromMe"] = true;

        await Postar(doBot);

        await DeveSoResponderAAjudaQueSegue();
    }

    [Fact]
    public async Task NaoDeveFazerNadaQuandoOComandoTemMaisDeCentoEVinteSegundosQuandoChega()
    {
        JsonNode antigo = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        Payload(antigo)["timestamp"] = (_relogio.GetUtcNow() - TimeSpan.FromSeconds(121)).ToUnixTimeSeconds();

        await Postar(antigo);

        await DeveSoResponderAAjudaQueSegue();
    }

    // Exatamente 120 s ainda vale
    [Fact]
    public async Task DeveResponderQuandoOComandoTemExatamenteCentoEVinteSegundosQuandoChega()
    {
        JsonNode noLimite = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        Payload(noLimite)["timestamp"] = (_relogio.GetUtcNow() - TimeSpan.FromSeconds(120)).ToUnixTimeSeconds();

        await Postar(noLimite);
        await _mensageiro.AvancarAteMensagensAsync(_relogio, 2);

        _mensageiro.Mensagens.Should().HaveCount(2).And.OnlyContain(mensagem => mensagem.Tipo == TipoDeChamadaDoMensageiro.Imagem);
    }

    // Mesmo ID de mensagem, IDs de evento diferentes, chegando juntos
    [Fact]
    public async Task DeveResponderUmaVezQuandoOMesmoComandoChegaDuasVezesAoMesmoTempoEmWebhooksDiferentes()
    {
        JsonNode primeiro = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        JsonNode segundo = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        segundo["id"] = "evt_01m4jqk2w00q3stea60vqnn8zz";

        HttpResponseMessage[] respostas = await Task.WhenAll(Postar(primeiro), Postar(segundo));
        await Postar(Ajuda());
        await _mensageiro.AvancarAteMensagensAsync(_relogio, 3);

        respostas.Should().AllSatisfy(resposta => resposta.StatusCode.Should().Be(HttpStatusCode.OK));
        _mensageiro.Mensagens.Select(mensagem => mensagem.Tipo).Should()
            .Equal(TipoDeChamadaDoMensageiro.Imagem, TipoDeChamadaDoMensageiro.Imagem, TipoDeChamadaDoMensageiro.Texto);
    }

    [Theory]
    [InlineData("{ isto não é json")]
    [InlineData("")]
    [InlineData("[1, 2, 3]")]
    [InlineData("""{"event":"message","payload":{"fromMe":"talvez"}}""")]
    public async Task DeveDescartarOWebhookMalformadoComUmLogEResponderOProximoComandoQuandoOWahaEnviaLixo(string corpo)
    {
        HttpResponseMessage resposta = await PostarAssinado(corpo);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        _fabrica.Logs.Should().Contain(log => log.Mensagem.StartsWith("Webhook do WAHA descartado"));
        await DeveSoResponderAAjudaQueSegue();
    }

    // WAHA sempre manda JSON; outro tipo de corpo nem chega ao caso de uso
    [Fact]
    public async Task DeveResponderTipoDeMidiaNaoSuportadoQuandoOCorpoNaoEhJson()
    {
        HttpResponseMessage resposta = await PostarAssinado("!sortear", "text/plain");

        resposta.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        await DeveSoResponderAAjudaQueSegue();
    }

    [Fact]
    public async Task DeveLogarUmErroQuandoOWahaDizQueASessaoParou()
    {
        HttpResponseMessage resposta = await Postar(FixturesWaha.LerNo(FixturesWaha.StatusDaSessao));

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        _fabrica.Logs.Should().Contain(log =>
            log.Nivel == LogLevel.Error && log.Mensagem.StartsWith("Sessão do WAHA default está STOPPED"));
    }

    [Fact]
    public async Task DeveManterNomesTelefonesETextoDaListaForaDeTodoLogQuandoResponderUmSorteio()
    {
        await Postar(FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista));
        await _mensageiro.AvancarAteMensagensAsync(_relogio, 2);

        _fabrica.Logs.Should().NotBeEmpty().And.AllSatisfy(log => log.Mensagem.Should().NotContainAny(
            "Presidente Fulano", "Téo Vargas", "Cauã Luz", "Nico Braga", "Duda Neri", "Paco", "Davi", "Lista de jogadores", "Campo Municipal",
            "35600000101", "120363000000001234"));
    }

    // Webhook assinado: o WAHA assina o corpo cru com HMAC-SHA512
    [Fact]
    public async Task DeveResponderOComandoQuandoAAssinaturaCombinaComOCorpo()
    {
        string corpo = Ajuda().ToJsonString();

        HttpResponseMessage resposta = await Postar(corpo, AssinaturaWaha.De(corpo));
        await _mensageiro.AvancarAteMensagensAsync(_relogio, 1);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        _mensageiro.Mensagens.Should().ContainSingle().Which.Texto.Should().Be(GuiaDaPresidencia);
    }

    // Webhook assinado (o WAHA manda o hexadecimal em minúsculas; em maiúsculas vale igual)
    [Fact]
    public async Task DeveResponderOComandoQuandoAAssinaturaEstaEmHexMaiusculo()
    {
        string corpo = Ajuda().ToJsonString();

        HttpResponseMessage resposta = await Postar(corpo, AssinaturaWaha.De(corpo).ToUpperInvariant());
        await _mensageiro.AvancarAteMensagensAsync(_relogio, 1);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        _mensageiro.Mensagens.Should().ContainSingle().Which.Texto.Should().Be(GuiaDaPresidencia);
    }

    // Webhook assinado (sem assinatura, o comando nem chega à fila)
    [Fact]
    public async Task DeveRecusarComNaoAutorizadoELogarUmAvisoQuandoAAssinaturaEstaFaltando()
    {
        HttpResponseMessage resposta = await Postar(FixturesWaha.Ler(FixturesWaha.SorteioCitandoLista), assinatura: null);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _fabrica.Logs.Should().Contain(log => log.Nivel == LogLevel.Warning && log.Mensagem.StartsWith("Webhook do WAHA recusado"));
        await DeveSoResponderAAjudaQueSegue();
    }

    // Webhook assinado (corpo grande demais é recusado antes de ser lido inteiro e assinado)
    [Fact]
    public async Task DeveRecusarComCorpoGrandeDemaisQuandoOWebhookPassaDoLimite()
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        webhook["preenchimento"] = new string('x', (int)FiltroDeAssinaturaDoWaha.TamanhoMaximoDoCorpo);
        string corpo = webhook.ToJsonString();

        HttpResponseMessage resposta = await Postar(corpo, AssinaturaWaha.De(corpo));

        resposta.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        _fabrica.Logs.Should().Contain(log => log.Nivel == LogLevel.Warning && log.Mensagem.StartsWith("Webhook do WAHA recusado"));
        await DeveSoResponderAAjudaQueSegue();
    }

    // Webhook assinado (assinatura feita com outra chave)
    [Fact]
    public async Task DeveRecusarComNaoAutorizadoEIgnorarOComandoQuandoAAssinaturaFoiFeitaComOutraChave()
    {
        string corpo = FixturesWaha.Ler(FixturesWaha.SorteioCitandoLista);

        HttpResponseMessage resposta = await Postar(corpo, AssinaturaWaha.De(corpo, OutraChave));

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _fabrica.Logs.Should().Contain(log => log.Nivel == LogLevel.Warning && log.Mensagem.StartsWith("Webhook do WAHA recusado"));
        await DeveSoResponderAAjudaQueSegue();
    }

    // Webhook assinado (corpo mudado depois de assinado)
    [Fact]
    public async Task DeveRecusarComNaoAutorizadoEIgnorarOComandoQuandoOCorpoMudouDepoisDeAssinado()
    {
        JsonNode webhook = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        string assinatura = AssinaturaWaha.De(webhook.ToJsonString());
        Payload(webhook)["body"] = "!pendentes";

        HttpResponseMessage resposta = await Postar(webhook.ToJsonString(), assinatura);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await DeveSoResponderAAjudaQueSegue();
    }

    // Webhook assinado (assinatura que não é o hexadecimal de um HMAC-SHA512)
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("isto-nao-e-hexadecimal")]
    public async Task DeveRecusarComNaoAutorizadoQuandoAAssinaturaNaoEhHex(string assinatura)
    {
        HttpResponseMessage resposta = await Postar(FixturesWaha.Ler(FixturesWaha.SorteioCitandoLista), assinatura);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Webhook assinado (só o começo da assinatura certa não basta)
    [Fact]
    public async Task DeveRecusarComNaoAutorizadoQuandoAAssinaturaEhSoAPrimeiraMetadeDaCerta()
    {
        string corpo = FixturesWaha.Ler(FixturesWaha.SorteioCitandoLista);

        HttpResponseMessage resposta = await Postar(corpo, AssinaturaWaha.De(corpo)[..64]);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // A recusa vai para o log sem o corpo, sem a assinatura e sem IDs
    [Fact]
    public async Task DeveManterOCorpoAAssinaturaEOsIdsForaDoLogQuandoRecusaUmWebhook()
    {
        string corpo = FixturesWaha.Ler(FixturesWaha.SorteioCitandoLista);
        string assinatura = AssinaturaWaha.De(corpo, OutraChave);

        await Postar(corpo, assinatura);

        _fabrica.Logs.Should().Contain(log => log.Mensagem.StartsWith("Webhook do WAHA recusado")).And.AllSatisfy(log =>
            log.Mensagem.Should().NotContainAny(
                assinatura, "!sortear", "Téo Vargas", "Lista de jogadores", "evt_01m4jqk2w00q3stea60vqnn8rf", "3EB0C767D26A1D8E0A01",
                "35600000101", "120363000000001234"));
    }

    /// <summary>
    /// A fila mantém a ordem, então quando o !ajuda postado depois de um webhook descartado é a única resposta, o descartado não enviou nada.
    /// </summary>
    private async Task DeveSoResponderAAjudaQueSegue()
    {
        await Postar(Ajuda());
        await _mensageiro.AvancarAteMensagensAsync(_relogio, 1);

        ChamadaDoMensageiro envio = _mensageiro.Mensagens.Should().ContainSingle().Which;
        envio.Texto.Should().Be(GuiaDaPresidencia);
        envio.IdDaMensagemRespondida.Should().Be(IdDaAjuda);
    }

    /// <summary>O !ajuda da presidência com a configuração de teste: sem título, sem link, com o valor do appsettings.</summary>
    private static string GuiaDaPresidencia => TextoDeAjuda.Compor(string.Empty, string.Empty, string.Empty, TimeSpan.FromMinutes(30), paraAPresidencia: true);

    private async Task<HttpResponseMessage> Postar(JsonNode webhook) => await PostarAssinado(webhook.ToJsonString());

    /// <summary>Envia o corpo assinado do jeito que o WAHA assina, com a chave do bot de teste.</summary>
    private async Task<HttpResponseMessage> PostarAssinado(string corpo, string tipoDeMidia = TipoDeMidiaJson) =>
        await Postar(corpo, AssinaturaWaha.De(corpo), tipoDeMidia);

    /// <summary>Envia o corpo com este X-Webhook-Hmac, ou sem o cabeçalho quando a assinatura é null.</summary>
    private async Task<HttpResponseMessage> Postar(string corpo, string? assinatura, string tipoDeMidia = TipoDeMidiaJson)
    {
        using HttpRequestMessage pedido = new(HttpMethod.Post, Webhook)
        {
            Content = new StringContent(corpo, Encoding.UTF8, tipoDeMidia),
        };

        if (assinatura is not null)
        {
            pedido.Headers.Add(AssinaturaWaha.Cabecalho, assinatura);
        }

        return await _cliente.SendAsync(pedido, TestContext.Current.CancellationToken);
    }

    /// <summary>!ajuda do presidente, enviado agora, sem citar nada.</summary>
    private JsonNode Ajuda()
    {
        JsonNode ajuda = FixturesWaha.LerNo(FixturesWaha.SorteioCitandoLista);
        JsonObject payload = Payload(ajuda);
        payload["id"] = IdDaAjuda;
        payload["body"] = "!ajuda";
        payload["timestamp"] = _relogio.GetUtcNow().ToUnixTimeSeconds();
        payload.Remove("replyTo");

        return ajuda;
    }

    private static JsonObject Payload(JsonNode webhook) =>
        webhook["payload"]?.AsObject() ?? throw new InvalidOperationException("A fixture não tem payload.");
}
