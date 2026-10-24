using FluentValidation;

namespace PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;

/// <summary>Só a forma do pedido: ids presentes e, quando a citação existe, o id dela também presente.</summary>
public sealed class ValidadorCobrarPendentes : AbstractValidator<PedidoCobrarPendentes>
{
    public ValidadorCobrarPendentes()
    {
        RuleFor(pedido => pedido.IdDoGrupo).NotEmpty().WithMessage(MensagensDeValidacao.IdDoGrupoObrigatorio);
        RuleFor(pedido => pedido.IdDoRemetente).NotEmpty().WithMessage(MensagensDeValidacao.IdDoRemetenteObrigatorio);
        RuleFor(pedido => pedido.IdDaMensagem).NotEmpty().WithMessage(MensagensDeValidacao.IdDaMensagemObrigatorio);

        When(pedido => pedido.Citada is not null, () =>
            RuleFor(pedido => pedido.Citada!.Id).NotEmpty().WithMessage(MensagensDeValidacao.IdDaCitacaoObrigatorio));
    }
}
