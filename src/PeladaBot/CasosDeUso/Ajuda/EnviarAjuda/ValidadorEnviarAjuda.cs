using FluentValidation;

namespace PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;

/// <summary>Só a forma do pedido: os três ids presentes. Esse pedido não cita mensagem.</summary>
public sealed class ValidadorEnviarAjuda : AbstractValidator<PedidoEnviarAjuda>
{
    public ValidadorEnviarAjuda()
    {
        RuleFor(pedido => pedido.IdDoGrupo).NotEmpty().WithMessage(MensagensDeValidacao.IdDoGrupoObrigatorio);
        RuleFor(pedido => pedido.IdDoRemetente).NotEmpty().WithMessage(MensagensDeValidacao.IdDoRemetenteObrigatorio);
        RuleFor(pedido => pedido.IdDaMensagem).NotEmpty().WithMessage(MensagensDeValidacao.IdDaMensagemObrigatorio);
    }
}
