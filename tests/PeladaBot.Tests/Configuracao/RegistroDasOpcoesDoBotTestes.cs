using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Configuracao;

public sealed class RegistroDasOpcoesDoBotTestes
{
    [Fact]
    public void DeveRegistrarGrupoPresidenciaELimitesQuandoOBotInicia()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        fabrica.StartServer();

        LogDeInicializacao(fabrica).Should().Contain("grupo ***1234@g.us")
            .And.Contain("presidência [***0101@c.us, ***0202@lid] [***0303@c.us]")
            .And.Contain("janela de resposta não presidente 00:10:00")
            .And.Contain("janela de cobrança 00:30:00")
            .And.Contain("disjuntor 30 mensagens por 01:00:00")
            .And.Contain("idade máxima do comando 00:02:00")
            .And.Contain("idade máxima da lista 3.00:00:00");
    }

    [Fact]
    public void DeveManterIdsCompletosENomesForaDoLogQuandoOBotInicia()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        fabrica.StartServer();

        fabrica.Logs.Should().AllSatisfy(log => log.Mensagem.Should().NotContainAny(
            Numero(FabricaDaAplicacaoDoBot.IdDoGrupo),
            Numero(FabricaDaAplicacaoDoBot.IdDoTelefoneDoPresidente),
            Numero(FabricaDaAplicacaoDoBot.LidDoPresidente),
            Numero(FabricaDaAplicacaoDoBot.IdDoTelefoneDoVice),
            FabricaDaAplicacaoDoBot.NomeDoPresidente,
            FabricaDaAplicacaoDoBot.NomeDoVice));
    }

    private static string LogDeInicializacao(FabricaDaAplicacaoDoBot fabrica) =>
        fabrica.Logs.Should().ContainSingle(log => log.Categoria == typeof(RegistroDasOpcoesDoBot).FullName).Which.Mensagem;

    private static string Numero(string id) => id[..id.IndexOf('@', StringComparison.Ordinal)];
}
