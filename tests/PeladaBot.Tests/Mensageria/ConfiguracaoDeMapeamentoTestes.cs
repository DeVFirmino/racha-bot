using Mapster;
using PeladaBot.Mensageria;

namespace PeladaBot.Tests.Mensageria;

public sealed class ConfiguracaoDeMapeamentoTestes
{
    // Compila e valida toda a configuração: um campo novo em um Pedido* sem fonte em ComandoRecebido quebra aqui,
    // nunca em produção (RequireDestinationMemberSource).
    [Fact]
    public void NaoDeveLancarQuandoCompilaOsMapeamentosDoComandoRecebido()
    {
        Action compilar = () => ConfiguracaoDeMapeamento.Config.Compile();

        compilar.Should().NotThrow();
    }

    // A mesma regra da configuração, provada com um destino que tem um campo sem fonte: Compile() tem de falhar.
    [Fact]
    public void DeveLancarQuandoUmCampoDoDestinoNaoTemFonteNoComandoRecebido()
    {
        TypeAdapterConfig configuracao = ConfiguracaoDeMapeamento.Config.Clone();
        configuracao.NewConfig<ComandoRecebido, PedidoComCampoSemFonte>();

        Action compilar = () => configuracao.Compile();

        compilar.Should().Throw<Exception>();
    }

    private sealed record PedidoComCampoSemFonte
    {
        public string IdDoGrupo { get; init; } = string.Empty;

        public string CampoQueNaoExisteNoComando { get; init; } = string.Empty;
    }
}
