namespace PeladaBot.CasosDeUso;

/// <summary>
/// Um pedido malformado chegou a um caso de uso (comandos só correm pela fila, nunca pelo HTTP): o consumidor da
/// fila captura, loga sem dado pessoal e segue para o próximo comando.
/// </summary>
public sealed class ExcecaoDeValidacao : Exception
{
    public ExcecaoDeValidacao(IReadOnlyList<string> erros)
        : base(string.Join(" ", erros))
    {
        Erros = erros;
    }

    public IReadOnlyList<string> Erros { get; }
}
