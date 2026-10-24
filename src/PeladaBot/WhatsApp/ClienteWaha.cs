using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace PeladaBot.WhatsApp;

/// <summary>
/// A API REST do WAHA como um HttpClient tipado; nunca lança (um envio retorna um <see cref="ResultadoDoEnvio"/>,
/// uma leitura retorna null). Os logs nunca carregam um id de chat, nome ou texto.
/// </summary>
public sealed partial class ClienteWaha : IMensageiroDoGrupo, IHistoricoDoGrupo
{
    /// <summary>O cabeçalho de onde o WAHA lê a chave da api.</summary>
    public const string CabecalhoDaChaveDeApi = "X-Api-Key";

    /// <summary>O histórico é lido em páginas desse tamanho, mais recente primeiro.</summary>
    public const int TamanhoDaPaginaDoHistorico = 100;

    /// <summary>No máximo essa quantidade de páginas por comando, bem mais do que o grupo escreve em 72 horas.</summary>
    public const int MaximoDePaginasDoHistorico = 10;

    private const string NomeDoArquivoDoCard = "times.png";
    private const string MimetypeDoCard = "image/png";

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IOptions<OpcoesDoWaha> _opcoes;
    private readonly ILogger<ClienteWaha> _logger;

    public ClienteWaha(HttpClient httpClient, IOptions<OpcoesDoWaha> opcoes, ILogger<ClienteWaha> logger)
    {
        _httpClient = httpClient;
        _opcoes = opcoes;
        _logger = logger;
    }

    private string Sessao => _opcoes.Value.Sessao;

    /// <summary>Aponta o cliente para o WAHA e envia a chave da api; chamado uma vez por cliente pela factory.</summary>
    public static void Configurar(HttpClient httpClient, OpcoesDoWaha opcoes)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(opcoes);

        httpClient.BaseAddress = new Uri(opcoes.UrlBase.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Add(CabecalhoDaChaveDeApi, opcoes.ChaveDaApi);
    }

    public Task<ResultadoDoEnvio> EnviarTextoAsync(
        string idDoChat, string texto, string idDaMensagemRespondida, CancellationToken cancellationToken)
    {
        PedidoDeEnvioDeTextoWaha pedido = new()
        {
            Sessao = Sessao,
            IdDoChat = idDoChat,
            Texto = texto,
            RespondeA = idDaMensagemRespondida,
            PreviaDeLink = false,
        };

        return EnviarAsync("api/sendText", pedido, cancellationToken);
    }

    public Task<ResultadoDoEnvio> EnviarImagemAsync(
        string idDoChat, byte[] png, string? legenda, string idDaMensagemRespondida, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(png);

        ArquivoWaha arquivo = new()
        {
            Mimetype = MimetypeDoCard,
            NomeDoArquivo = NomeDoArquivoDoCard,
            Dados = Convert.ToBase64String(png),
        };

        PedidoDeEnvioDeImagemWaha pedido = new()
        {
            Sessao = Sessao,
            IdDoChat = idDoChat,
            Arquivo = arquivo,
            Legenda = legenda,
            RespondeA = idDaMensagemRespondida,
        };

        return EnviarAsync("api/sendImage", pedido, cancellationToken);
    }

    public Task<bool> IniciarDigitandoAsync(string idDoChat, CancellationToken cancellationToken) =>
        DefinirDigitandoAsync("api/startTyping", idDoChat, cancellationToken);

    public Task<bool> PararDigitandoAsync(string idDoChat, CancellationToken cancellationToken) =>
        DefinirDigitandoAsync("api/stopTyping", idDoChat, cancellationToken);

    public async Task<MensagemDoGrupo?> ObterMensagemAsync(
        string idDoChat, string idDaMensagem, CancellationToken cancellationToken)
    {
        string caminho =
            $"api/{Escapar(Sessao)}/chats/{Escapar(idDoChat)}/messages/{Escapar(idDaMensagem)}?downloadMedia=false";
        MensagemWaha? mensagem = await ObterAsync<MensagemWaha>(caminho, "obter mensagem", cancellationToken);

        return mensagem is null ? null : ParaMensagemDoGrupo(mensagem);
    }

    public async Task<IReadOnlyList<MensagemDoGrupo>?> ObterMensagensDesdeAsync(
        string idDoChat, DateTimeOffset desde, CancellationToken cancellationToken)
    {
        List<MensagemDoGrupo> maisRecentesPrimeiro = [];
        HashSet<string> idsVistos = new(StringComparer.Ordinal);

        for (int pagina = 0; pagina < MaximoDePaginasDoHistorico; pagina++)
        {
            string caminho = CaminhoDaPaginaDoHistorico(idDoChat, desde, pagina);
            MensagemWaha[]? lote = await ObterAsync<MensagemWaha[]>(caminho, "obter mensagens", cancellationToken);

            if (lote is null)
            {
                return null;
            }

            int lidasAntes = maisRecentesPrimeiro.Count;

            // Uma mensagem que chegou entre duas páginas desloca os offsets: a mesma mensagem pode vir duas vezes.
            maisRecentesPrimeiro.AddRange(lote
                .Where(mensagem => mensagem.FromMe is false)
                .Select(ParaMensagemDoGrupo)
                .OfType<MensagemDoGrupo>()
                .Where(mensagem => idsVistos.Add(mensagem.Id)));

            // Só uma página sem nada novo termina a leitura: fora da janela o WAHA responde vazio, mas uma página
            // curta ainda pode ter mensagens mais antigas, já que o WAHA descarta reações e mensagens de protocolo
            // depois do limite.
            if (maisRecentesPrimeiro.Count == lidasAntes)
            {
                return MaisAntigasPrimeiro(maisRecentesPrimeiro);
            }
        }

        RegistrarHistoricoLimitado(_logger, MaximoDePaginasDoHistorico * TamanhoDaPaginaDoHistorico);

        return MaisAntigasPrimeiro(maisRecentesPrimeiro);
    }

    /// <summary>A sessão do bot, ou null quando o WAHA não pôde ser consultado; usado uma vez, na inicialização.</summary>
    public Task<SessaoWaha?> ObterSessaoAsync(CancellationToken cancellationToken) =>
        ObterAsync<SessaoWaha>($"api/sessions/{Escapar(Sessao)}", "obter sessão", cancellationToken);

    private async Task<ResultadoDoEnvio> EnviarAsync<TPedido>(
        string caminho, TPedido pedido, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage resposta =
                await _httpClient.PostAsJsonAsync(caminho, pedido, _json, cancellationToken);

            if (resposta.IsSuccessStatusCode)
            {
                return new Enviado();
            }

            return new Rejeitado { Motivo = $"WAHA respondeu {(int)resposta.StatusCode}" };
        }
        catch (HttpRequestException excecao)
            when (excecao.HttpRequestError is
                HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError)
        {
            // A conexão nunca abriu, então o pedido nunca chegou ao WAHA.
            return new Rejeitado { Motivo = $"WAHA não pôde ser alcançado ({excecao.HttpRequestError})" };
        }
        catch (HttpRequestException excecao)
        {
            return new Incerto { Motivo = $"a conexão quebrou durante o envio ({excecao.HttpRequestError})" };
        }
        catch (TimeoutRejectedException)
        {
            return new Incerto { Motivo = "WAHA não respondeu a tempo" };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            return new Incerto { Motivo = "WAHA não respondeu a tempo" };
        }
    }

    private async Task<bool> DefinirDigitandoAsync(string caminho, string idDoChat, CancellationToken cancellationToken)
    {
        PedidoDeChatWaha pedido = new() { Sessao = Sessao, IdDoChat = idDoChat };
        ResultadoDoEnvio resultado = await EnviarAsync(caminho, pedido, cancellationToken);

        if (resultado is Rejeitado or Incerto)
        {
            RegistrarChamadaFalhou(_logger, caminho, Motivo(resultado));
        }

        return resultado is Enviado;
    }

    private async Task<T?> ObterAsync<T>(string caminho, string operacao, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using HttpResponseMessage resposta = await _httpClient.GetAsync(caminho, cancellationToken);

            if (resposta.StatusCode is HttpStatusCode.NotFound)
            {
                RegistrarChamadaFalhou(_logger, operacao, "WAHA respondeu 404");

                return null;
            }

            if (resposta.IsSuccessStatusCode is false)
            {
                RegistrarChamadaFalhou(_logger, operacao, $"WAHA respondeu {(int)resposta.StatusCode}");

                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<T>(_json, cancellationToken);
        }
        catch (HttpRequestException excecao)
        {
            RegistrarChamadaFalhou(_logger, operacao, $"a conexão com o WAHA falhou ({excecao.HttpRequestError})");
        }
        catch (TimeoutRejectedException)
        {
            RegistrarChamadaFalhou(_logger, operacao, "WAHA não respondeu a tempo");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            RegistrarChamadaFalhou(_logger, operacao, "WAHA não respondeu a tempo");
        }
        catch (JsonException)
        {
            RegistrarChamadaFalhou(_logger, operacao, "WAHA respondeu um JSON em um formato inesperado");
        }

        return null;
    }

    private string CaminhoDaPaginaDoHistorico(string idDoChat, DateTimeOffset desde, int pagina)
    {
        int deslocamento = pagina * TamanhoDaPaginaDoHistorico;
        long desdeEmSegundosUnix = desde.ToUnixTimeSeconds();

        return string.Create(
            CultureInfo.InvariantCulture,
            $"api/{Escapar(Sessao)}/chats/{Escapar(idDoChat)}/messages?limit={TamanhoDaPaginaDoHistorico}&offset={deslocamento}"
                + $"&sortBy=timestamp&sortOrder=desc&downloadMedia=false&filter.fromMe=false"
                + $"&filter.timestamp.gte={desdeEmSegundosUnix}");
    }

    /// <summary>Inverte páginas lidas mais recente primeiro; uma ordenação estável corrige uma mensagem que mudou de página.</summary>
    private static List<MensagemDoGrupo> MaisAntigasPrimeiro(List<MensagemDoGrupo> maisRecentesPrimeiro) =>
        [.. Enumerable.Reverse(maisRecentesPrimeiro).OrderBy(mensagem => mensagem.EnviadaEm)];

    /// <summary>Null para uma mensagem sem id ou hora de envio, como o WAHA dá para algumas mensagens de sistema.</summary>
    private static MensagemDoGrupo? ParaMensagemDoGrupo(MensagemWaha mensagem)
    {
        if (mensagem.Id is null || mensagem.EnviadaEm is not DateTimeOffset enviadaEm)
        {
            return null;
        }

        return new MensagemDoGrupo
        {
            Id = mensagem.Id,
            IdDoRemetente = mensagem.Participante,
            NomeDoRemetente = mensagem.Dados?.Info?.NomeDeExibicao,
            EnviadaEm = enviadaEm,
            Texto = mensagem.Corpo,
        };
    }

    private static string Escapar(string valor) => Uri.EscapeDataString(valor);

    private static string Motivo(ResultadoDoEnvio resultado) => resultado switch
    {
        Rejeitado rejeitado => rejeitado.Motivo,
        Incerto incerto => incerto.Motivo,
        _ => string.Empty,
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Chamada ao WAHA {Operacao} falhou: {Motivo}")]
    private static partial void RegistrarChamadaFalhou(ILogger logger, string operacao, string motivo);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Histórico do grupo limitado a {MaximoDeMensagens} mensagens")]
    private static partial void RegistrarHistoricoLimitado(ILogger logger, int maximoDeMensagens);
}
