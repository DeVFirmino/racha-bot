using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Configuracao;

public sealed class OpcoesDoBotTestes
{
    // Grupo e presidência vêm da configuração
    [Fact]
    public void DeveLerGrupoEPresidenciaDaConfiguracaoQuandoOBotInicia()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        OpcoesDoBot opcoes = OpcoesLigadas(fabrica);

        OpcoesDoGrupo grupo = opcoes.Grupos.Should().ContainSingle().Which;
        grupo.Id.Should().Be(FabricaDaAplicacaoDoBot.IdDoGrupo);
        grupo.Presidencia.Should().HaveCount(2);
        grupo.Presidencia[0].Ids.Should().Equal(FabricaDaAplicacaoDoBot.IdDoTelefoneDoPresidente, FabricaDaAplicacaoDoBot.LidDoPresidente);
        grupo.Presidencia[1].Ids.Should().Equal(FabricaDaAplicacaoDoBot.IdDoTelefoneDoVice);
    }

    [Fact]
    public void DeveUsarOsLimitesDaSpecQuandoAConfiguracaoMantemOsPadroes()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        OpcoesDeLimites limites = OpcoesLigadas(fabrica).Limites;

        limites.Should().BeEquivalentTo(new OpcoesDeLimites
        {
            JanelaDeRespostaNaoPresidente = TimeSpan.FromMinutes(10),
            JanelaDeCobranca = TimeSpan.FromMinutes(30),
            MaximoDeMensagensDoDisjuntor = 30,
            JanelaDoDisjuntor = TimeSpan.FromMinutes(60),
            IdadeMaximaDoComando = TimeSpan.FromSeconds(120),
            IdadeMaximaDaLista = TimeSpan.FromHours(72),
        });
    }

    [Fact]
    public void DeveSeguirOsLimitesConfiguradosQuandoUmAmbienteOsReduz()
    {
        using FabricaDaAplicacaoDoBot fabrica = FabricaDaAplicacaoDoBot.ComConfiguracoes(new Dictionary<string, string?>
        {
            ["Bot:Limites:MaximoDeMensagensDoDisjuntor"] = "3",
            ["Bot:Limites:JanelaDeRespostaNaoPresidente"] = "00:01:00",
        });

        OpcoesDeLimites limites = OpcoesLigadas(fabrica).Limites;

        limites.MaximoDeMensagensDoDisjuntor.Should().Be(3);
        limites.JanelaDeRespostaNaoPresidente.Should().Be(TimeSpan.FromMinutes(1));
        limites.JanelaDoDisjuntor.Should().Be(TimeSpan.FromMinutes(60));
    }

    // Frases do rodapé vêm da configuração
    [Fact]
    public void DeveTrazerAsFrasesDoRodapeQuandoORodapeNaoEhSubstituido()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        OpcoesDoGrupo grupo = OpcoesLigadas(fabrica).Grupos.Single();

        grupo.FrasesDoRodape.Should().Equal(
            "⚽ *Hoje tem jogo. A bola não espera, e o apito também não.*",
            "⏰ *Chegue no horário. O aquecimento é curto.*",
            "😂 *O sorteio é justo. O futebol acontece em campo.*",
            "🧤 *Goleiro não escolhe time. Quem escolheu foi a lista.*",
            "👕 *Colete não mente: tá na lista, tá no jogo.*",
            "⚽ *Jogo de dois tempos. Apareça nos dois.*",
            "📣 *Jogue até o apito, e chegue antes dele.*",
            "🚌 *Retranca só depois de chegar.*",
            "🏆 *Ninguém é maior que a pelada. Pagou, jogou.*",
            "⏱️ *Um jogo de cada vez. Este começa na hora.*");
    }

    [Fact]
    public void DeveTrazerOsModelosDaLegendaQuandoALegendaNaoEhSubstituida()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        OpcoesDoGrupo grupo = OpcoesLigadas(fabrica).Grupos.Single();

        grupo.ModelosDaLegenda.Should().HaveCount(4).And.AllSatisfy(modelo =>
        {
            modelo.Cabecalho.Should().Contain("*PELADA | ");
            modelo.LinhaDoHorario.Should().Contain("{hora}");
            new[] { modelo.ApelidoLaranja, modelo.ApelidoVerde, modelo.ApelidoCinza, modelo.ApelidoAzul, modelo.ApelidoSemColete }
                .Should().OnlyContain(apelido => string.IsNullOrWhiteSpace(apelido) == false);
        });
        grupo.ModelosDaLegenda[0].Cabecalho.Should().Be("⚽ *PELADA | DIA DE JOGO*");
    }

    [Fact]
    public void DeveTrazerTresApelidosAvulsosPorCorQuandoOsApelidosNaoSaoSubstituidos()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        OpcoesDosApelidosAvulsos apelidos = OpcoesLigadas(fabrica).Grupos.Single().ApelidosAvulsos;

        apelidos.Laranja.Should().Equal("LARANJA UNIDO", "QUARTA LARANJA", "JOGO DE DOIS TEMPOS");
        apelidos.Verde.Should().Equal("VERDE DA VILA", "ATÉ O APITO", "VERDE E PONTUAL");
        apelidos.Cinza.Should().Equal("CINZA ATLÉTICO", "RETRANCA TOTAL", "UM JOGO DE CADA VEZ");
        apelidos.Azul.Should().Equal("AZUL FC", "NÃO TEM JOGO FÁCIL", "AZUL ATÉ O APITO");
        apelidos.SemColete.Should().Equal("SEM COLETE FC", "NOMES NA SÚMULA", "AINDA CHEGANDO");
    }

    [Fact]
    public void DeveTrazerAsMensagensDeCobrancaQuandoACobrancaNaoEhSubstituida()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();

        OpcoesDoGrupo grupo = OpcoesLigadas(fabrica).Grupos.Single();

        grupo.MensagensDeCobranca.Should().Equal(
            "💶 *Se ainda não pagou, acerta aí.*\n\n😂 Pra jogar todo mundo aparece. Na hora de pagar, a lista encolhe.",
            "⏰ *O sorteio espera o pagamento.*\n\n⚽ *Sem ✅, sem time.*",
            "💸 *Falta pagar ({quantidade}):*\n👀 {nome}\n\n😂 *Pagou, tá dentro. Não pagou, espera.*\n💶 *Valor: {valor}*\n📲 {link}",
            "⚽ *Jogo de dois tempos.*\n\n⏰ *Esteja nos dois, e pague antes do sorteio.*",
            "📣 *Jogue até o apito.*\n\n💸 *Falta pagar ({quantidade}):*\n👀 {nome}",
            "🚌 *Nada de retranca no pagamento.*\n\n🏆 *Ninguém é maior que a pelada.*\n💶 *{valor}*\n📲 {link}");
    }

    [Theory]
    [InlineData("Bot:Grupos:0:Id", "")]
    [InlineData("Bot:Grupos:0:Presidencia:0:Ids:0", " ")]
    [InlineData("Bot:Grupos:0:FrasesDoRodape:0", "")]
    [InlineData("Bot:Limites:MaximoDeMensagensDoDisjuntor", "0")]
    [InlineData("Bot:Limites:IdadeMaximaDoComando", "00:00:00")]
    [InlineData("Bot:Limites:IdadeMaximaDaLista", "72:00:00")]
    [InlineData("Bot:Fuso", "Nao/Existe")]
    public void DeveRecusarIniciarELogarOMotivoQuandoUmaConfiguracaoEhInvalida(string chave, string valor)
    {
        using FabricaDaAplicacaoDoBot fabrica = FabricaDaAplicacaoDoBot.ComConfiguracoes(new Dictionary<string, string?>
        {
            [chave] = valor,
        });

        Action iniciar = fabrica.StartServer;

        iniciar.Should().Throw<Exception>();
        fabrica.Logs
            .Select(log => log.Excecao)
            .OfType<OptionsValidationException>()
            .Should().ContainSingle()
            .Which.Failures.Should().ContainSingle()
            .Which.Should().StartWith(chave);
    }

    // Chave com erro de digitação
    [Fact]
    public void DeveRecusarIniciarENomearAChaveQuandoUmaConfiguracaoEstaComErroDeDigitacao()
    {
        using FabricaDaAplicacaoDoBot fabrica = FabricaDaAplicacaoDoBot.ComConfiguracoes(new Dictionary<string, string?>
        {
            ["Bot:Grupos:0:Presidenciaa:0:Ids:0"] = FabricaDaAplicacaoDoBot.IdDoTelefoneDoPresidente,
        });

        Action iniciar = fabrica.StartServer;

        iniciar.Should().Throw<Exception>();
        fabrica.Logs
            .Select(log => log.Excecao)
            .OfType<InvalidOperationException>()
            .Should().ContainSingle()
            .Which.GetBaseException().Message.Should().Contain("'Presidenciaa'");
    }

    private static OpcoesDoBot OpcoesLigadas(FabricaDaAplicacaoDoBot fabrica) =>
        fabrica.Services.GetRequiredService<IOptions<OpcoesDoBot>>().Value;
}
