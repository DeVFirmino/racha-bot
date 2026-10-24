using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Configuracao;

public sealed class VerificacaoDoFusoTestes
{
    // O fuso configurado é carregado no startup, e sem tzdata o bot nem sobe
    [Fact]
    public void DeveCarregarOFusoConfiguradoQuandoOBotInicia()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        fabrica.StartServer();

        fabrica.Logs.Should().ContainSingle(log => log.Categoria == typeof(VerificacaoDoFuso).FullName)
            .Which.Mensagem.Should().StartWith("Fuso horário Europe/Rome carregado: offset UTC ");
    }

    [Fact]
    public void DeveCarregarAmericaSaoPauloQuandoAConfiguracaoPede()
    {
        using FabricaDaAplicacaoDoBot fabrica = FabricaDaAplicacaoDoBot.ComConfiguracoes(new Dictionary<string, string?>
        {
            ["Bot:Fuso"] = "America/Sao_Paulo",
        });

        fabrica.StartServer();

        fabrica.Logs.Should().ContainSingle(log => log.Categoria == typeof(VerificacaoDoFuso).FullName)
            .Which.Mensagem.Should().StartWith("Fuso horário America/Sao_Paulo carregado: offset UTC -03:00:00");
    }
}
