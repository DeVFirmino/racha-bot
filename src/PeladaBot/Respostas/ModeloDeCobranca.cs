using System.Globalization;

namespace PeladaBot.Respostas;

/// <summary>
/// Preenche uma mensagem de cobrança da configuração: a linha com "{nome}" se repete uma vez por pendente, na ordem
/// da lista; "{quantidade}" vira quantos são, e "{s}" vira o plural ("s", a não ser quando é um só); "{valor}" e
/// "{link}" vêm do pagamento configurado, e a linha some quando ele não tem o que ela pede. Uma mensagem sem marcas
/// sai como está.
/// </summary>
public static class ModeloDeCobranca
{
    public const string MarcaDoNome = "{nome}";
    public const string MarcaDaQuantidade = "{quantidade}";
    public const string MarcaDoPlural = "{s}";
    public const string MarcaDoValor = "{valor}";
    public const string MarcaDoLink = "{link}";

    /// <summary>As únicas marcas que uma mensagem de cobrança pode ter.</summary>
    public static IReadOnlyList<string> Marcas { get; } = [MarcaDoNome, MarcaDaQuantidade, MarcaDoPlural, MarcaDoValor, MarcaDoLink];

    /// <summary>True quando a mensagem já lista os pendentes, e a resposta não precisa listar de novo.</summary>
    public static bool ListaOsNomes(string modelo)
    {
        ArgumentNullException.ThrowIfNull(modelo);

        return modelo.Contains(MarcaDoNome, StringComparison.Ordinal);
    }

    public static string Preencher(string modelo, IReadOnlyList<string> nomesNaoPagos, DadosDoPagamento? pagamento = null)
    {
        ArgumentNullException.ThrowIfNull(modelo);
        ArgumentNullException.ThrowIfNull(nomesNaoPagos);

        DadosDoPagamento dados = pagamento ?? DadosDoPagamento.Nenhum;

        string quantidade = nomesNaoPagos.Count.ToString(CultureInfo.InvariantCulture);
        string plural = nomesNaoPagos.Count == 1 ? string.Empty : "s";
        List<string> linhas = [];

        // A quantidade e o plural entram antes dos nomes: um nome nunca é lido como marca.
        foreach (string linhaDoModelo in modelo.Trim().Split('\n'))
        {
            if (FaltaOPagamento(linhaDoModelo, dados))
            {
                continue;
            }

            string linha = linhaDoModelo
                .Replace(MarcaDaQuantidade, quantidade, StringComparison.Ordinal)
                .Replace(MarcaDoPlural, plural, StringComparison.Ordinal)
                .Replace(MarcaDoValor, dados.Valor.Trim(), StringComparison.Ordinal)
                .Replace(MarcaDoLink, dados.Link.Trim(), StringComparison.Ordinal);

            if (linha.Contains(MarcaDoNome, StringComparison.Ordinal))
            {
                linhas.AddRange(nomesNaoPagos.Select(nome =>
                    linha.Replace(MarcaDoNome, MarcacaoDoWhatsApp.Neutralizar(nome), StringComparison.Ordinal)));
            }
            else
            {
                linhas.Add(linha);
            }
        }

        return LinhasDeResposta.Juntar(linhas);
    }

    /// <summary>True quando a mensagem já mostra o valor ou o link, e a resposta não precisa mostrar de novo.</summary>
    public static bool MostraOPagamento(string modelo)
    {
        ArgumentNullException.ThrowIfNull(modelo);

        return modelo.Contains(MarcaDoValor, StringComparison.Ordinal) || modelo.Contains(MarcaDoLink, StringComparison.Ordinal);
    }

    /// <summary>
    /// "💶 *Valor: <valor>*" e "📲 pagamento.exemplo/grupo", o que a configuração tiver, para ir no fim de toda cobrança que não
    /// mostra o pagamento; vazio sem valor nem link.
    /// </summary>
    public static IReadOnlyList<string> LinhasDoPagamento(DadosDoPagamento? pagamento)
    {
        DadosDoPagamento dados = pagamento ?? DadosDoPagamento.Nenhum;
        List<string> linhas = [];

        if (string.IsNullOrWhiteSpace(dados.Valor) is false)
        {
            linhas.Add($"💶 *Valor: {dados.Valor.Trim()}*");
        }

        if (string.IsNullOrWhiteSpace(dados.Link) is false)
        {
            linhas.Add($"📲 {dados.Link.Trim()}");
        }

        return linhas;
    }

    /// <summary>A linha pede o valor ou o link, e a configuração não tem.</summary>
    private static bool FaltaOPagamento(string linha, DadosDoPagamento dados) =>
        (linha.Contains(MarcaDoValor, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(dados.Valor))
        || (linha.Contains(MarcaDoLink, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(dados.Link));
}
