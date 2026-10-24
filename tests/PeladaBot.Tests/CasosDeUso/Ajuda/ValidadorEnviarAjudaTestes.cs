using FluentValidation.Results;
using PeladaBot.CasosDeUso;
using PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;

namespace PeladaBot.Tests.CasosDeUso.Ajuda;

public sealed class ValidadorEnviarAjudaTestes
{
    private readonly ValidadorEnviarAjuda _validador = new();

    [Fact]
    public void NaoDeveTerErrosQuandoOPedidoEhValido()
    {
        ValidationResult resultado = _validador.Validate(PedidoValido());

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

    private static PedidoEnviarAjuda PedidoValido() => new()
    {
        IdDoGrupo = "120363000000001234@g.us",
        IdDoRemetente = "35600000303@c.us",
        IdDaMensagem = "3EB0E5F6A7B8C9D00A03",
    };
}
