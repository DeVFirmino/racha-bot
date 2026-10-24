using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace PeladaBot.WhatsApp;

/// <summary>Prazos para toda chamada ao WAHA; só leituras repetem, nunca um envio, que poderia postar duas vezes.</summary>
public static class ResilienciaWaha
{
    public const string NomeDoPipeline = "waha";

    /// <summary>A chamada inteira, repetições incluídas.</summary>
    public static readonly TimeSpan TempoLimiteTotal = TimeSpan.FromSeconds(30);

    /// <summary>Uma tentativa. Um envio tem só uma, então depois desse prazo seu resultado é incerto.</summary>
    public static readonly TimeSpan TempoLimiteDaTentativa = TimeSpan.FromSeconds(15);

    /// <summary>Uma leitura que falha é tentada de novo duas vezes, depois de cerca de 0,25 s e 0,5 s.</summary>
    public const int MaximoDeTentativasDeLeitura = 2;

    private static readonly TimeSpan _atrasoDaPrimeiraTentativa = TimeSpan.FromMilliseconds(250);

    /// <summary>De fora para dentro: o prazo total, a repetição de leituras, o prazo de cada tentativa.</summary>
    public static void Configurar(ResiliencePipelineBuilder<HttpResponseMessage> pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        HttpRetryStrategyOptions repeticao = new()
        {
            MaxRetryAttempts = MaximoDeTentativasDeLeitura,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = _atrasoDaPrimeiraTentativa,
        };
        repeticao.DisableForUnsafeHttpMethods();

        pipeline
            .AddTimeout(TempoLimiteTotal)
            .AddRetry(repeticao)
            .AddTimeout(TempoLimiteDaTentativa);
    }
}
