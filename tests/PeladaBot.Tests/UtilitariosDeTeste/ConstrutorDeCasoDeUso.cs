using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PeladaBot.Acesso;
using PeladaBot.Card;
using PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;
using PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;
using PeladaBot.CasosDeUso.Sorteio.SortearTimes;
using PeladaBot.Configuracao;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Mensageria;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Monta os casos de uso do jeito que a aplicação faz, com o controle, o disjuntor, a escolha de lista e o enviador de
/// respostas reais, e com o WhatsApp, o relógio e o acaso trocados: um grupo falso, um histórico falso, um relógio falso
/// e um Random com semente. O controle e o disjuntor são montados uma vez, para suas janelas passarem de um comando
/// para o outro, como os singletons da aplicação fazem.
/// </summary>
public sealed class ConstrutorDeCasoDeUso : IDisposable
{
    public const string IdDoGrupo = "120363000000001234@g.us";
    public const string IdDoTelefoneDoPresidente = "35600000101@c.us";
    public const string LidDoPresidente = "100000000000202@lid";
    public const string IdDoTelefoneDoVice = "35600000303@c.us";
    public const string IdDoIntegrante = "35600000404@c.us";
    public const string FraseDoRodape = "⏰ Sem atraso hoje, hein!";

    private const int MaximoDePassosDoRelogio = 2000;

    /// <summary>14/10, 21:00 no fuso da suíte (horário de verão, UTC+2).</summary>
    public static readonly DateTimeOffset Agora = new(2026, 10, 14, 19, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan _passoDoRelogio = TimeSpan.FromMilliseconds(100);

    private readonly ILoggerFactory _loggerFactory;
    private OpcoesDoBot _opcoes = new ConstrutorDeOpcoesDoBot().ComFrasesDoRodape(FraseDoRodape).Build();
    private int _semente = 14;
    private ControleDeAcesso? _controle;
    private DisjuntorDeEnvio? _disjuntor;

    public ConstrutorDeCasoDeUso()
    {
        Relogio = new FakeTimeProvider(Agora);
        Mensageiro = new MensageiroDoGrupoFalso(Relogio);
        _loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(Logs).SetMinimumLevel(LogLevel.Trace));
    }

    public FakeTimeProvider Relogio { get; }

    public MensageiroDoGrupoFalso Mensageiro { get; }

    public HistoricoDoGrupoFalso Historico { get; } = new();

    public ColetorDeLogs Logs { get; } = new();

    public ControleDeAcesso Controle => _controle ??=
        new ControleDeAcesso(
            ConstruirPresidencia(), new LimiteDeRespostas(Relogio, OpcoesLigadas()), new Random(_semente), Logger<ControleDeAcesso>());

    public DisjuntorDeEnvio Disjuntor => _disjuntor ??= new DisjuntorDeEnvio(Relogio, OpcoesLigadas());

    /// <summary>Substitui a configuração. Chame antes de montar qualquer coisa.</summary>
    public ConstrutorDeCasoDeUso ComOpcoes(OpcoesDoBot opcoes)
    {
        _opcoes = opcoes;

        return this;
    }

    /// <summary>A semente do Random atrás do sorteio, do rodapé e da pausa antes de cada resposta.</summary>
    public ConstrutorDeCasoDeUso ComSemente(int semente)
    {
        _semente = semente;

        return this;
    }

    public CasoDeUsoSortearTimes ConstruirSortearTimes() => new(
        new ValidadorSortearTimes(),
        Controle,
        ConstruirEscolhaDaLista(),
        ConstruirEnviadorDeRespostas(),
        Relogio,
        new Random(_semente),
        OpcoesLigadas(),
        new ProvedorDeIdentidadeDoCard(OpcoesLigadas()),
        Logger<CasoDeUsoSortearTimes>());

    public CasoDeUsoCobrarPendentes ConstruirCobrarPendentes() => new(
        new ValidadorCobrarPendentes(),
        Controle,
        ConstruirEscolhaDaLista(),
        ConstruirEnviadorDeRespostas(),
        new Random(_semente),
        OpcoesLigadas());

    public CasoDeUsoEnviarAjuda ConstruirEnviarAjuda() =>
        new(new ValidadorEnviarAjuda(), Controle, ConstruirEnviadorDeRespostas(), ConstruirPresidencia(), OpcoesLigadas());

    public SeletorDeLista ConstruirEscolhaDaLista() =>
        new(Historico, ConstruirPresidencia(), Relogio, OpcoesLigadas(), Logger<SeletorDeLista>());

    public EnviadorDeRespostas ConstruirEnviadorDeRespostas() =>
        new(Mensageiro, Disjuntor, Controle, Relogio, new Random(_semente), Logger<EnviadorDeRespostas>());

    /// <summary>Executa <paramref name="executar"/> e avança o relógio falso até terminar, por toda pausa antes de uma resposta.</summary>
    public async Task ExecutarAsync(Func<Task> executar)
    {
        ArgumentNullException.ThrowIfNull(executar);

        Task execucao = executar();

        for (int passo = 0; execucao.IsCompleted is false; passo++)
        {
            if (passo == MaximoDePassosDoRelogio)
            {
                throw new TimeoutException($"Ainda rodando depois de {MaximoDePassosDoRelogio * _passoDoRelogio} no relógio falso.");
            }

            Relogio.Advance(_passoDoRelogio);
            await Task.Yield();
        }

        await execucao;
    }

    public void Dispose() => _loggerFactory.Dispose();

    private IOptions<OpcoesDoBot> OpcoesLigadas() => Options.Create(_opcoes);

    private Presidencia ConstruirPresidencia() => new(OpcoesLigadas());

    private ILogger<T> Logger<T>() => _loggerFactory.CreateLogger<T>();
}
