using PeladaBot.Listas;
using PeladaBot.Sorteio;

namespace PeladaBot.Respostas;

/// <summary>
/// A resposta ao !sortear quando não há sorteio: ninguém na lista, falta alguém pagar, ou pagaram poucos demais.
/// Só texto, sem rodapé. Quando falta alguém pagar, vem antes uma das mensagens de cobrança, como no !cobrar.
/// </summary>
public static class RespostaSemSorteio
{
    /// <summary>
    /// Escreve por que não há sorteio. <paramref name="origem"/> é null quando o comando citou a lista. Com
    /// pagamento pendente, uma das <paramref name="mensagensDeCobranca"/>, escolhida com <paramref name="random"/>,
    /// vai em cima, como está na configuração.
    /// </summary>
    public static string Compor(
        ResultadoDoSorteio resultado,
        ListaDoGrupo lista,
        OrigemDaLista? origem,
        IReadOnlyList<string> mensagensDeCobranca,
        Random random,
        DadosDoPagamento? pagamento = null,
        TimeZoneInfo? fuso = null)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        ArgumentNullException.ThrowIfNull(lista);
        ArgumentNullException.ThrowIfNull(mensagensDeCobranca);
        ArgumentNullException.ThrowIfNull(random);

        IReadOnlyList<string> resposta = resultado switch
        {
            NinguemPagou => [LinhasDeResposta.NinguemNaLista],
            PagamentoPendente pendente => FaltaPagar(pendente.NomesPendentes, mensagensDeCobranca, random, pagamento),
            PagosInsuficientes poucos => PoucosPagos(poucos.QuantidadePaga),
            _ => throw new ArgumentException(
                "Times sorteados vão para a legenda do sorteio, não para esta resposta.", nameof(resultado)),
        };

        return LinhasDeResposta.Juntar([.. resposta, .. LinhasDeResposta.Avisos(lista, origem, fuso)]);
    }

    /// <summary>
    /// Um ou mais jogadores de linha estão sem ✅: o sorteio espera todo mundo pagar. Uma das mensagens de cobrança,
    /// preenchida com os pendentes, vai em cima; a lista "💸 *Falta pagar (N):*" só vem quando a mensagem não lista. A
    /// saída pelo !sortear pagos vem sempre no fim.
    /// </summary>
    private static IReadOnlyList<string> FaltaPagar(
        IReadOnlyList<string> nomesPendentes, IReadOnlyList<string> mensagensDeCobranca, Random random, DadosDoPagamento? pagamento)
    {
        if (mensagensDeCobranca.Count == 0)
        {
            throw new ArgumentException(
                "Falta de pagamento sempre vem com uma cobrança, então é preciso ao menos uma mensagem.",
                nameof(mensagensDeCobranca));
        }

        string modelo = mensagensDeCobranca[random.Next(mensagensDeCobranca.Count)];
        const string SortearPagos = "Para sortear só os pagos, use !sortear pagos (mínimo de 4 pagos).";
        List<string> linhas = LinhasDeResposta.Cobranca(modelo, nomesPendentes, pagamento);

        linhas.AddRange([string.Empty, SortearPagos]);

        return linhas;
    }

    /// <summary>N = 1 a 3, todos com ✅: poucos pagos para formar dois times.</summary>
    private static IReadOnlyList<string> PoucosPagos(int quantidadePaga)
    {
        int quantidadeFaltante = 4 - quantidadePaga;
        string verboFaltar = quantidadeFaltante == 1 ? "falta 1" : $"faltam {quantidadeFaltante}";

        string poucosPagos = quantidadePaga == 1
            ? "Temos 1 pago; faltam 3 para sortear (mínimo de 4)."
            : $"Temos {quantidadePaga} pagos; {verboFaltar} para sortear (mínimo de 4).";

        return [poucosPagos];
    }
}
