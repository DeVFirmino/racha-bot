using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PeladaBot.Card;
using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Card;

public sealed class ProvedorDeIdentidadeDoCardTestes : IDisposable
{
    private readonly string _escudo = Path.Combine(Path.GetTempPath(), $"escudo-{Guid.NewGuid():N}.jpeg");

    public void Dispose() => File.Delete(_escudo);

    // Sem configuração, o card continua "⚽ PELADA ⚽" e sem escudo
    [Fact]
    public void DeveUsarAIdentidadePadraoQuandoOGrupoNaoConfigurouNomeNemEscudo()
    {
        ProvedorDeIdentidadeDoCard provedor = Provedor(new ConstrutorDeOpcoesDoBot().Build());

        IdentidadeDoCard identidade = provedor.Obter(ConstrutorDeOpcoesDoBot.IdDoGrupo);

        identidade.Should().Be(IdentidadeDoCard.Padrao);
    }

    // Identidade do grupo: o nome sem espaços nas bordas e o escudo lido do arquivo
    [Fact]
    public void DeveLerOEscudoEONomeQuandoOGrupoConfigurouOsDois()
    {
        File.WriteAllBytes(_escudo, [7, 8, 9]);
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComIdentidadeDoCard("  PELADA ", _escudo).Build();

        IdentidadeDoCard identidade = Provedor(opcoes).Obter(ConstrutorDeOpcoesDoBot.IdDoGrupo);

        identidade.Titulo.Should().Be("PELADA");
        identidade.Escudo.Should().Equal(7, 8, 9);
    }

    // Um grupo que não está na configuração recebe o card padrão
    [Fact]
    public void DeveUsarAIdentidadePadraoQuandoOGrupoNaoEstaNaConfiguracao()
    {
        ProvedorDeIdentidadeDoCard provedor = Provedor(new ConstrutorDeOpcoesDoBot().ComIdentidadeDoCard("PELADA", string.Empty).Build());

        IdentidadeDoCard identidade = provedor.Obter("120363000000009999@g.us");

        identidade.Should().Be(IdentidadeDoCard.Padrao);
    }

    private static ProvedorDeIdentidadeDoCard Provedor(OpcoesDoBot opcoes) => new(Options.Create(opcoes));

    [Fact]
    public void DeveCarregarOEscudoNaInicializacaoQuandoNenhumSorteioFoiExecutado()
    {
        File.WriteAllBytes(_escudo, [7, 8, 9]);
        using FabricaDaAplicacaoDoBot fabrica = FabricaDaAplicacaoDoBot.ComConfiguracoes(new Dictionary<string, string?>
        {
            ["Bot:Grupos:0:CaminhoDoEscudo"] = _escudo,
        });
        using HttpClient cliente = fabrica.CreateClient();
        File.Delete(_escudo);

        ProvedorDeIdentidadeDoCard provedor = fabrica.Services.GetRequiredService<ProvedorDeIdentidadeDoCard>();

        provedor.Obter(FabricaDaAplicacaoDoBot.IdDoGrupo).Escudo.Should().Equal(7, 8, 9);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeveRecusarAInicializacaoComMensagemClaraESemDadosPessoaisQuandoOEscudoNaoPodeSerLido(bool ehDiretorio)
    {
        string caminho = ehDiretorio ? Path.GetTempPath() : _escudo;
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot().ComIdentidadeDoCard("Grupo de teste", caminho).Build();
        using FabricaDaAplicacaoDoBot fabrica = new FabricaDaAplicacaoDoBot().ComServicos(servicos =>
            servicos.AddSingleton<IOptions<OpcoesDoBot>>(Options.Create(opcoes)));

        Action iniciar = () => fabrica.CreateClient();

        InvalidOperationException excecao = iniciar.Should().Throw<InvalidOperationException>()
            .WithMessage("Não foi possível ler o escudo do card. Verifique o arquivo configurado e suas permissões antes de iniciar o bot.")
            .Which;
        excecao.InnerException.Should().BeNull();
        excecao.ToString().Should().NotContain(caminho).And.NotContain("Grupo de teste");
    }
}
