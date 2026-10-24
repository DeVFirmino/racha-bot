using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PeladaBot.Acesso;
using PeladaBot.Configuracao;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.Acesso;

public sealed class PresidenciaTestes
{
    private const string IdDoGrupo = "120363000000001234@g.us";
    private const string IdDoOutroGrupo = "120363000000005678@g.us";
    private const string NomeDoPresidente = "Presidente Fulano";
    private const string IdDoTelefoneDoPresidente = "35600000101@c.us";
    private const string LidDoPresidente = "100000000000202@lid";
    private const string IdDoTelefoneDoVice = "35600000303@c.us";
    private const string IdDoIntegrante = "35600000404@c.us";

    [Fact]
    public void DeveReconhecerOViceQuandoOViceEstaConfigurado()
    {
        Presidencia presidencia = CriarPresidencia();

        bool ehPresidencia = presidencia.Inclui(IdDoGrupo, IdDoTelefoneDoVice);

        ehPresidencia.Should().BeTrue();
    }

    [Theory]
    [InlineData(IdDoTelefoneDoPresidente)]
    [InlineData(LidDoPresidente)]
    public void DeveReconhecerOPresidenteQuandoAMensagemVemDeQualquerUmDosSeusIds(string idDoRemetente)
    {
        Presidencia presidencia = CriarPresidencia();

        bool ehPresidencia = presidencia.Inclui(IdDoGrupo, idDoRemetente);

        ehPresidencia.Should().BeTrue();
    }

    // Só o ID autoriza: o nome configurado, usado como se fosse um ID, não vale
    [Fact]
    public void NaoDeveReconhecerOPresidenteQuandoSoONomeExibidoCombina()
    {
        Presidencia presidencia = CriarPresidencia();

        bool ehPresidencia = presidencia.Inclui(IdDoGrupo, NomeDoPresidente);

        ehPresidencia.Should().BeFalse();
    }

    // Outro admin do grupo é um integrante comum
    [Fact]
    public void NaoDeveReconhecerUmRemetenteDesconhecidoQuandoSeuIdNaoEstaConfigurado()
    {
        Presidencia presidencia = CriarPresidencia();

        bool ehPresidencia = presidencia.Inclui(IdDoGrupo, IdDoIntegrante);

        ehPresidencia.Should().BeFalse();
    }

    // O ID tem que ser exatamente um dos configurados
    [Theory]
    [InlineData("35600000101")]
    [InlineData("35600000101@lid")]
    [InlineData("5600000101@c.us")]
    [InlineData("135600000101@c.us")]
    public void NaoDeveReconhecerORemetenteQuandoOIdSoSeAssemelhaAUmConfigurado(string idDoRemetente)
    {
        Presidencia presidencia = CriarPresidencia();

        bool ehPresidencia = presidencia.Inclui(IdDoGrupo, idDoRemetente);

        ehPresidencia.Should().BeFalse();
    }

    [Fact]
    public void NaoDeveReconhecerNinguemQuandoOGrupoNaoEstaConfigurado()
    {
        Presidencia presidencia = CriarPresidencia();

        bool ehPresidencia = presidencia.Inclui(IdDoOutroGrupo, IdDoTelefoneDoPresidente);

        ehPresidencia.Should().BeFalse();
    }

    // Os IDs chegam pela configuração da aplicação, telefone e LID
    [Fact]
    public void DeveReconhecerOPresidenteDaConfiguracaoQuandoAAplicacaoRoda()
    {
        using FabricaDaAplicacaoDoBot fabrica = new();
        Presidencia presidencia = fabrica.Services.GetRequiredService<Presidencia>();

        bool ehPresidencia = presidencia.Inclui(FabricaDaAplicacaoDoBot.IdDoGrupo, FabricaDaAplicacaoDoBot.LidDoPresidente);

        ehPresidencia.Should().BeTrue();
    }

    private static Presidencia CriarPresidencia()
    {
        OpcoesDoBot opcoes = new ConstrutorDeOpcoesDoBot()
            .ComIdDoGrupo(IdDoGrupo)
            .ComPresidencia(
                new OpcoesDoMembroDaPresidencia { Nome = NomeDoPresidente, Ids = [IdDoTelefoneDoPresidente, LidDoPresidente] },
                new OpcoesDoMembroDaPresidencia { Nome = "Vice Ciclano", Ids = [IdDoTelefoneDoVice] })
            .Build();

        return new Presidencia(Options.Create(opcoes));
    }
}
