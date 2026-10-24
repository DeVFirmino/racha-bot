namespace PeladaBot.Respostas;

/// <summary>O valor da pelada e o link de pagamento da configuração, para as marcas {valor} e {link} da cobrança.</summary>
public sealed record DadosDoPagamento
{
    /// <summary>Sem valor nem link: as linhas com {valor} ou {link} somem.</summary>
    public static DadosDoPagamento Nenhum { get; } = new() { Valor = string.Empty, Link = string.Empty };

    /// <summary>O valor como o grupo escreve; vazio quando a configuração não tem.</summary>
    public required string Valor { get; init; }

    /// <summary>Como "pagamento.exemplo/grupo"; vazio quando a configuração não tem.</summary>
    public required string Link { get; init; }
}
