using Mapster;
using PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;
using PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;
using PeladaBot.CasosDeUso.Sorteio.SortearTimes;

namespace PeladaBot.Mensageria;

/// <summary>
/// Os mapeamentos 1:1 do <see cref="ConsumidorDaFila"/>: um <see cref="ComandoRecebido"/> vira cada Pedido* do seu
/// caso de uso, campo a campo. <see cref="TypeAdapterConfig.RequireDestinationMemberSource"/> faz um campo novo no
/// pedido, sem fonte em <see cref="ComandoRecebido"/>, quebrar <c>Config.Compile()</c> (testado), não a produção.
/// Uma instância própria, nunca <see cref="TypeAdapterConfig.GlobalSettings"/>, para não depender da ordem dos testes.
/// </summary>
public static class ConfiguracaoDeMapeamento
{
    public static readonly TypeAdapterConfig Config = Construir();

    private static TypeAdapterConfig Construir()
    {
        TypeAdapterConfig configuracao = new() { RequireDestinationMemberSource = true };

        configuracao.NewConfig<ComandoRecebido, PedidoSortearTimes>();
        configuracao.NewConfig<ComandoRecebido, PedidoEnviarAjuda>();
        configuracao.NewConfig<ComandoRecebido, PedidoCobrarPendentes>();

        return configuracao;
    }
}
