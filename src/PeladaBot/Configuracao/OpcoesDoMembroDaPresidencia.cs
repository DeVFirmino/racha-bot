namespace PeladaBot.Configuracao;

public sealed class OpcoesDoMembroDaPresidencia
{
    /// <summary>Um nome de exibição para o arquivo de configuração; nunca usado para reconhecer ninguém, nunca logado.</summary>
    public string Nome { get; init; } = string.Empty;

    /// <summary>Todo id do WhatsApp de onde a pessoa pode escrever, ex.: o id do telefone e o LID.</summary>
    public IReadOnlyList<string> Ids { get; init; } = [];
}
