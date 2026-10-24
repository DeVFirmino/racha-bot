using PeladaBot.Listas;

namespace PeladaBot.Respostas;

/// <summary>
/// As linhas que mais de uma resposta compartilha. Todo nome sai com sua marcação do WhatsApp neutralizada.
/// </summary>
internal static class LinhasDeResposta
{
    public const string NinguemNaLista = "Ninguém na lista ainda. Reenvie a lista completa com os nomes abaixo de Lista de jogadores.";

    /// <summary>
    /// A linha "📋" quando o comando não citou a lista, depois uma linha "⚠️" por aviso da lista, na ordem da lista.
    /// </summary>
    public static IReadOnlyList<string> Avisos(ListaDoGrupo lista, OrigemDaLista? origem, TimeZoneInfo? fuso = null)
    {
        List<string> avisos = [];

        if (origem is not null)
        {
            avisos.Add(LinhaDeOrigem(origem, fuso ?? HorarioDoGrupo.FusoDaSuite));
        }

        avisos.AddRange(lista.Avisos.Select(LinhaDeAviso));

        return avisos;
    }

    /// <summary>Os jogadores de "Lista de jogadores" sem ✅, na ordem da lista. Só o !pendentes usa.</summary>
    public static IReadOnlyList<string> NomesNaoPagos(ListaDoGrupo lista) =>
        [.. lista.Jogadores.Where(jogador => jogador.Pago is false).Select(jogador => jogador.Nome)];

    /// <summary>"💸 *Falta pagar (2):*" e um pendente por linha, "👀 Pedro": a lista de toda cobrança.</summary>
    public static IReadOnlyList<string> ListaDePendentes(IReadOnlyList<string> nomesNaoPagos) =>
        [$"💸 *Falta pagar ({nomesNaoPagos.Count}):*", .. nomesNaoPagos.Select(nome => $"👀 {MarcacaoDoWhatsApp.Neutralizar(nome)}")];

    /// <summary>"Pedro, Lucas".</summary>
    public static string Nomes(IEnumerable<string> nomes) => string.Join(", ", nomes.Select(MarcacaoDoWhatsApp.Neutralizar));

    /// <summary>As linhas de uma mensagem, quebradas com \n como o WhatsApp faz.</summary>
    public static string Juntar(IEnumerable<string> linhas) => string.Join('\n', linhas);

    private static string LinhaDeOrigem(OrigemDaLista origem, TimeZoneInfo fuso)
    {
        string enviadaEm = $"{HorarioDoGrupo.DiaEMes(origem.EnviadaEm, fuso)} às {HorarioDoGrupo.HoraEMinuto(origem.EnviadaEm, fuso)}";

        if (string.IsNullOrWhiteSpace(origem.NomeDoRemetente))
        {
            return $"📋 Lista de {enviadaEm}";
        }

        return $"📋 Lista enviada por {MarcacaoDoWhatsApp.Neutralizar(origem.NomeDoRemetente.Trim())} em {enviadaEm}";
    }

    private static string LinhaDeAviso(AvisoDaLista aviso)
    {
        string nome = MarcacaoDoWhatsApp.Neutralizar(aviso.Nome);

        return aviso switch
        {
            NomeEmDuasSecoes { MantidoEm: SecaoDaLista.Goleiros } =>
                $"⚠️ {nome} estava em duas seções, ficou como goleiro",
            NomeEmDuasSecoes { MantidoEm: SecaoDaLista.Jogadores } =>
                $"⚠️ {nome} estava em duas seções, ficou na linha",
            NomeRepetidoComMarcaDiferente => $"⚠️ {nome} aparece duas vezes na lista (valeu a primeira)",
            _ => throw new ArgumentOutOfRangeException(nameof(aviso), aviso, "Sem linha de resposta para esse aviso."),
        };
    }
}
