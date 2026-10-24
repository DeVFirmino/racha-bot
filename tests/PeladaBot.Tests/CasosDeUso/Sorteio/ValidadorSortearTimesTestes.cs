using FluentValidation.Results;
using PeladaBot.CasosDeUso;
using PeladaBot.CasosDeUso.Sorteio.SortearTimes;
using PeladaBot.EscolhaDaLista;

namespace PeladaBot.Tests.CasosDeUso.Sorteio;

public sealed class ValidadorSortearTimesTestes
{
    private readonly ValidadorSortearTimes _validador = new();

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

    private static PedidoSortearTimes PedidoValido() => new()
    {
        IdDoGrupo = "120363000000001234@g.us",
        IdDoRemetente = "35600000101@c.us",
        IdDaMensagem = "3EB0C767D26A1D8E0A01",
        Citada = new MensagemCitada { Id = "3EB05B1F2A9C4D7E0B02", Texto = "Lista de jogadores:" },
        SomentePagos = false,
    };
}
