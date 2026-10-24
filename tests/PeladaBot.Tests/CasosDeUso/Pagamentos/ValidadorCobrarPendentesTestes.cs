using FluentValidation.Results;
using PeladaBot.CasosDeUso;
using PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;
using PeladaBot.EscolhaDaLista;

namespace PeladaBot.Tests.CasosDeUso.Pagamentos;

public sealed class ValidadorCobrarPendentesTestes
{
    private readonly ValidadorCobrarPendentes _validador = new();

    [Fact]
    public void NaoDeveTerErrosQuandoOPedidoEhValido()
    {
        ValidationResult resultado = _validador.Validate(PedidoValido());

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NaoDeveTerErrosQuandoOPedidoEhValidoSemCitacao()
    {
        ValidationResult resultado = _validador.Validate(PedidoValido() with { Citada = null });

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeveFalharComAMensagemDoIdDoGrupoQuandoEleEstaVazio()
    {
        ValidationResult resultado = _validador.Validate(PedidoValido() with { IdDoGrupo = string.Empty });

        resultado.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(MensagensDeValidacao.IdDoGrupoObrigatorio);
    }

    [Fact]
    public void DeveFalharComAMensagemDoIdDoRemetenteQuandoEleEstaVazio()
    {
        ValidationResult resultado = _validador.Validate(PedidoValido() with { IdDoRemetente = string.Empty });

        resultado.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(MensagensDeValidacao.IdDoRemetenteObrigatorio);
    }

    [Fact]
    public void DeveFalharComAMensagemDoIdDaMensagemQuandoEleEstaVazio()
    {
        ValidationResult resultado = _validador.Validate(PedidoValido() with { IdDaMensagem = string.Empty });

        resultado.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(MensagensDeValidacao.IdDaMensagemObrigatorio);
    }

    [Fact]
    public void DeveFalharComAMensagemDoIdDaCitacaoQuandoACitacaoTemIdVazio()
    {
        ValidationResult resultado = _validador.Validate(PedidoValido() with { Citada = new MensagemCitada { Id = string.Empty, Texto = null } });

        resultado.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(MensagensDeValidacao.IdDaCitacaoObrigatorio);
    }

    private static PedidoCobrarPendentes PedidoValido() => new()
    {
        IdDoGrupo = "120363000000001234@g.us",
        IdDoRemetente = "35600000404@c.us",
        IdDaMensagem = "3EB0D1E2F3A4B5C60A02",
        Citada = new MensagemCitada { Id = "3EB05B1F2A9C4D7E0B02", Texto = "Lista de jogadores:" },
    };
}
