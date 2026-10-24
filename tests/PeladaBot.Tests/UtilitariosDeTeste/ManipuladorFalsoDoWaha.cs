using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Faz as vezes do WAHA sob o ClienteWaha real e seu pipeline de resiliência: registra todo pedido que chega a ele e
/// responde o que o teste programou para aquele método e caminho, ou um padrão funcionando: a sessão está WORKING, um
/// envio é aceito, o histórico está vazio.
/// </summary>
public sealed class ManipuladorFalsoDoWaha : HttpMessageHandler
{
    private readonly ConcurrentQueue<PedidoWaha> _pedidos = new();
    private readonly ConcurrentDictionary<(string Metodo, string Caminho), Queue<Func<HttpResponseMessage>>> _programados = new();

    public IReadOnlyList<PedidoWaha> Pedidos => [.. _pedidos];

    public IReadOnlyList<PedidoWaha> PedidosPara(string caminho) => [.. _pedidos.Where(pedido => pedido.Caminho == caminho)];

    /// <summary>
    /// Responde os próximos pedidos a <paramref name="metodo"/> <paramref name="caminho"/> com esses resultados, um por
    /// pedido em ordem; o último se repete. Um resultado ou devolve uma resposta ou lança, como a rede faria.
    /// </summary>
    public ManipuladorFalsoDoWaha Programar(HttpMethod metodo, string caminho, params Func<HttpResponseMessage>[] resultados)
    {
        ArgumentNullException.ThrowIfNull(metodo);

        _programados[(metodo.Method, caminho)] = new Queue<Func<HttpResponseMessage>>(resultados);

        return this;
    }

    /// <summary>Responde todo pedido a <paramref name="metodo"/> <paramref name="caminho"/> com este status e corpo JSON.</summary>
    public ManipuladorFalsoDoWaha Responder(HttpMethod metodo, string caminho, HttpStatusCode status, string json = "{}") =>
        Programar(metodo, caminho, () => Json(status, json));

    public static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Uri uri = request.RequestUri ?? throw new InvalidOperationException("Um pedido sem URI.");

        _pedidos.Enqueue(new PedidoWaha
        {
            Metodo = request.Method.Method,
            Caminho = uri.AbsolutePath,
            Query = uri.Query,
            ChaveDaApi = request.Headers.TryGetValues("X-Api-Key", out IEnumerable<string>? chaves) ? chaves.Single() : null,
            TipoDeConteudo = request.Content?.Headers.ContentType?.MediaType,
            Corpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
        });

        return ProximoResultado(request.Method.Method, uri.AbsolutePath)();
    }

    private Func<HttpResponseMessage> ProximoResultado(string metodo, string caminho)
    {
        if (_programados.TryGetValue((metodo, caminho), out Queue<Func<HttpResponseMessage>>? resultados))
        {
            lock (resultados)
            {
                return resultados.Count > 1 ? resultados.Dequeue() : resultados.Peek();
            }
        }

        return ResultadoPadrao(metodo, caminho);
    }

    private static Func<HttpResponseMessage> ResultadoPadrao(string metodo, string caminho)
    {
        if (metodo == HttpMethod.Post.Method)
        {
            return () => Json(HttpStatusCode.Created, "{}");
        }

        if (caminho.StartsWith("/api/sessions/", StringComparison.Ordinal))
        {
            return () => Json(HttpStatusCode.OK, FixturesWaha.Ler(FixturesWaha.Sessao));
        }

        if (caminho.EndsWith("/messages", StringComparison.Ordinal))
        {
            return () => Json(HttpStatusCode.OK, "[]");
        }

        return () => Json(HttpStatusCode.NotFound, """{"statusCode":404,"message":"Message not found"}""");
    }
}
