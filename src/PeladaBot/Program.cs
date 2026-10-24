using FluentValidation;
using Microsoft.Extensions.Options;
using PeladaBot.Acesso;
using PeladaBot.Card;
using PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;
using PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;
using PeladaBot.CasosDeUso.Sorteio.SortearTimes;
using PeladaBot.CasosDeUso.Webhooks.ReceberEventoDoWaha;
using PeladaBot.Configuracao;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Filtros;
using PeladaBot.Mensageria;
using PeladaBot.WhatsApp;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Um webhook que falha o bind chega ao caso de uso como null, logado e descartado com um 200, não um 400
// automático que faria o WAHA reentregá-lo para sempre.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

// Antes do corpo ser vinculado, o webhook verifica a assinatura do WAHA; não assinado ou forjado recebe 401.
builder.Services.AddSingleton<FiltroDeAssinaturaDoWaha>();

// Configuração: validada na inicialização; o bot recusa iniciar e diz por quê.
builder.Services.AddOptions<OpcoesDoBot>()
    .BindConfiguration(OpcoesDoBot.SectionName, binder => binder.ErrorOnUnknownConfiguration = true)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<OpcoesDoBot>, ValidadorDasOpcoesDoBot>();
builder.Services.AddOptions<OpcoesDoWaha>()
    .BindConfiguration(OpcoesDoWaha.SectionName, binder => binder.ErrorOnUnknownConfiguration = true)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<OpcoesDoWaha>, ValidadorDasOpcoesDoWaha>();

// Tempo e acaso entram de fora, para os testes poderem fixá-los.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(Random.Shared);

// Acesso: singletons, porque as janelas e o disjuntor devem viver pelo tempo do processo.
builder.Services.AddSingleton<Presidencia>();
builder.Services.AddSingleton<LimiteDeRespostas>();
builder.Services.AddSingleton<DisjuntorDeEnvio>();
builder.Services.AddSingleton<ControleDeAcesso>();

// WhatsApp: o WAHA atrás das duas portas, com um prazo em toda chamada e sem repetição de um envio.
builder.Services.AddHttpClient<ClienteWaha>((services, httpClient) =>
        ClienteWaha.Configurar(httpClient, services.GetRequiredService<IOptions<OpcoesDoWaha>>().Value))
    .RemoveAllLoggers()
    .AddResilienceHandler(ResilienciaWaha.NomeDoPipeline, ResilienciaWaha.Configurar);
builder.Services.AddTransient<IMensageiroDoGrupo>(services => services.GetRequiredService<ClienteWaha>());
builder.Services.AddTransient<IHistoricoDoGrupo>(services => services.GetRequiredService<ClienteWaha>());

// Mensageria: uma fila limitada, a dedupe do seu consumidor, e o que os casos de uso compartilham para escolher
// uma lista e responder.
builder.Services.AddSingleton<FilaDeComandos>();
builder.Services.AddSingleton<IdsRecentes>();
builder.Services.AddScoped<SeletorDeLista>();
builder.Services.AddScoped<EnviadorDeRespostas>();
builder.Services.AddSingleton<ProvedorDeIdentidadeDoCard>();

// Casos de uso, um por operação.
builder.Services.AddScoped<ICasoDeUsoCobrarPendentes, CasoDeUsoCobrarPendentes>();
builder.Services.AddScoped<ICasoDeUsoEnviarAjuda, CasoDeUsoEnviarAjuda>();
builder.Services.AddScoped<ICasoDeUsoReceberEventoDoWaha, CasoDeUsoReceberEventoDoWaha>();
builder.Services.AddScoped<ICasoDeUsoSortearTimes, CasoDeUsoSortearTimes>();

// Validadores dos pedidos, um por caso de uso, corridos na primeira linha de Executar; sem estado, então
// singleton, como o pedido de um caso de uso já é.
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

// Mapster: ComandoRecebido -> Pedido* do ConsumidorDaFila, campo a campo; falha rápido na inicialização se um
// campo novo ficar sem fonte (RequireDestinationMemberSource).
ConfiguracaoDeMapeamento.Config.Compile();

// Trabalho em segundo plano, na ordem de início: o registro da configuração, o fuso ou nenhum
// início, a leitura dos escudos, o status da sessão do WAHA, depois o consumidor da fila. Param na ordem inversa, depois do
// servidor web: o consumidor esgota primeiro.
builder.Services.AddHostedService<RegistroDasOpcoesDoBot>();
builder.Services.AddHostedService<VerificacaoDoFuso>();
builder.Services.AddHostedService<VerificacaoDaIdentidadeDoCard>();
builder.Services.AddHostedService<VerificacaoDaSessaoWaha>();
builder.Services.AddHostedService<ConsumidorDaFila>();

WebApplication app = builder.Build();

app.MapControllers();

app.Run();
