using PeladaBot.Listas;

namespace PeladaBot.Respostas;

/// <summary>
/// A resposta ao !pendentes (ou !cobrar): uma mensagem da diretoria, sorteada, quem ainda está sem ✅ e como pagar.
/// </summary>
public static class RespostaDeCobranca
{
    public const string TodoMundoPagou = "✅ Todo mundo pagou, valeu craques!";

    /// <summary>
    /// Com alguém sem ✅ em "Lista de jogadores", uma das <paramref name="mensagens"/>, preenchida com os pendentes
    /// (<see cref="ModeloDeCobranca"/>), e embaixo a lista "💸 *Falta pagar (N):*", um nome por linha, a não ser que a mensagem já liste os
    /// nomes; com todo mundo pago, só o agradecimento, sem cobrança. Os avisos da
    /// lista vêm no fim, como no !pendentes. Pura: a única fonte de acaso é <paramref name="random"/>.
    /// </summary>
    public static string Compor(
        ListaDoGrupo lista, OrigemDaLista? origem, IReadOnlyList<string> mensagens, Random random, DadosDoPagamento? pagamento = null, TimeZoneInfo? fuso = null)
    {
        ArgumentNullException.ThrowIfNull(lista);
        ArgumentNullException.ThrowIfNull(mensagens);
        ArgumentNullException.ThrowIfNull(random);

        if (mensagens.Count == 0)
        {
            throw new ArgumentException("Uma cobrança sempre usa uma mensagem, então é preciso ao menos uma.", nameof(mensagens));
        }

        IReadOnlyList<string> avisos = LinhasDeResposta.Avisos(lista, origem, fuso);

        if (lista.Jogadores.Count == 0)
        {
            return LinhasDeResposta.Juntar([LinhasDeResposta.NinguemNaLista, .. avisos]);
        }

        IReadOnlyList<string> nomesNaoPagos = LinhasDeResposta.NomesNaoPagos(lista);

        if (nomesNaoPagos.Count == 0)
        {
            return LinhasDeResposta.Juntar([TodoMundoPagou, .. avisos]);
        }

        string modelo = mensagens[random.Next(mensagens.Count)];
        List<string> linhas = [ModeloDeCobranca.Preencher(modelo, nomesNaoPagos, pagamento)];

        if (ModeloDeCobranca.ListaOsNomes(modelo) is false)
        {
            linhas.AddRange([string.Empty, .. LinhasDeResposta.ListaDePendentes(nomesNaoPagos)]);
        }

        IReadOnlyList<string> linhasDoPagamento = ModeloDeCobranca.LinhasDoPagamento(pagamento);

        if (ModeloDeCobranca.MostraOPagamento(modelo) is false && linhasDoPagamento.Count > 0)
        {
            linhas.AddRange([string.Empty, .. linhasDoPagamento]);
        }

        return LinhasDeResposta.Juntar([.. linhas, .. avisos]);
    }
}
