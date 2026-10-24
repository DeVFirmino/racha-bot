namespace PeladaBot.CasosDeUso;

/// <summary>Mensagens dos validadores de pedido dos casos de uso, num só lugar para os testes checarem a mesma constante.</summary>
public static class MensagensDeValidacao
{
    public const string IdDoGrupoObrigatorio = "O id do grupo é obrigatório.";

    public const string IdDoRemetenteObrigatorio = "O id do remetente é obrigatório.";

    public const string IdDaMensagemObrigatorio = "O id da mensagem é obrigatório.";

    /// <summary>Quando o pedido cita uma mensagem, a citação precisa trazer o id dela.</summary>
    public const string IdDaCitacaoObrigatorio = "O id da mensagem citada é obrigatório.";
}
