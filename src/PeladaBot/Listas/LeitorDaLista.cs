using System.Text.RegularExpressions;
using PeladaBot.Textos;

namespace PeladaBot.Listas;

public static partial class LeitorDaLista
{
    private const char MarcaDePago = '✅';
    private const string PalavraChaveDeNota = "nota";

    private static readonly Dictionary<string, SecaoDaLista> _titulosDeSecao = new(StringComparer.Ordinal)
    {
        ["goleiros fixos"] = SecaoDaLista.Goleiros,
        ["lista de jogadores"] = SecaoDaLista.Jogadores,
        ["lista de espera"] = SecaoDaLista.Espera,
    };

    /// <summary>Lê a lista colada do grupo. Null quando o texto não tem o título "Lista de jogadores".</summary>
    public static ListaDoGrupo? Ler(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        ItensDaSecao itens = new();
        InfoDoJogo jogo = new();
        SecaoDaLista? secao = null;
        bool temTituloDeJogadores = false;

        foreach (string linhaBruta in texto.ReplaceLineEndings("\n").Split('\n'))
        {
            string linha = TextoDaLista.Limpar(linhaBruta);

            if (EhNota(linha))
            {
                break;
            }

            if (TentarLerTituloDaSecao(linha, out SecaoDaLista titulo))
            {
                secao = titulo;
                temTituloDeJogadores |= titulo is SecaoDaLista.Jogadores;
                continue;
            }

            Match item = ItemNumerado().Match(linha);

            if (item.Success is false)
            {
                item = ItemSemNumeracao().Match(linha);
            }

            if (secao is SecaoDaLista atual && item.Success)
            {
                // Goleiro não paga; na espera, o ✅ diz que a pessoa já ganhou a vaga.
                bool secaoQuePaga = atual is SecaoDaLista.Jogadores or SecaoDaLista.Espera;
                bool pago = secaoQuePaga && linhaBruta.Contains(MarcaDePago);

                AdicionarItem(itens.Em(atual), item.Groups["nome"].Value, pago);
                continue;
            }

            jogo = LerCampoDoJogo(linha, jogo);
        }

        if (temTituloDeJogadores is false)
        {
            return null;
        }

        return MontarLista(itens, jogo);
    }

    /// <summary>
    /// "Nota", "Nota 1:", "Notas:" e "NOTAS" encerram a leitura; um nome que só começa com as mesmas letras, como
    /// "Notário", não encerra.
    /// </summary>
    private static bool EhNota(string linha)
    {
        string chave = ComparacaoDeTexto.CriarChave(linha);

        if (chave.StartsWith(PalavraChaveDeNota, StringComparison.Ordinal) is false)
        {
            return false;
        }

        bool temSPlural = chave.Length > PalavraChaveDeNota.Length && chave[PalavraChaveDeNota.Length] == 's';
        int fim = temSPlural ? PalavraChaveDeNota.Length + 1 : PalavraChaveDeNota.Length;

        return chave.Length == fim || char.IsLetter(chave[fim]) is false;
    }

    private static bool TentarLerTituloDaSecao(string linha, out SecaoDaLista secao)
    {
        string chaveDoTitulo = ComparacaoDeTexto.CriarChave(linha.TrimEnd(':').TrimEnd());

        return _titulosDeSecao.TryGetValue(chaveDoTitulo, out secao);
    }

    private static void AdicionarItem(List<ItemDaLista> secao, string nome, bool pago)
    {
        if (nome.Length == 0)
        {
            return;
        }

        secao.Add(new ItemDaLista
        {
            Nome = nome,
            Chave = ComparacaoDeTexto.CriarChave(nome),
            Pago = pago,
        });
    }

    private static InfoDoJogo LerCampoDoJogo(string linha, InfoDoJogo jogo)
    {
        int separador = linha.IndexOf(':', StringComparison.Ordinal);

        if (separador <= 0)
        {
            return jogo;
        }

        string rotulo = ComparacaoDeTexto.CriarChave(linha[..separador].TrimEnd());
        string valor = linha[(separador + 1)..].Trim();

        if (valor.Length == 0)
        {
            return jogo;
        }

        return rotulo switch
        {
            "data" => jogo with { Data = jogo.Data ?? valor },
            "horario" => jogo with { Horario = jogo.Horario ?? valor },
            "local" => jogo with { Local = jogo.Local ?? valor },
            _ => jogo,
        };
    }

    private static ListaDoGrupo MontarLista(ItensDaSecao itens, InfoDoJogo jogo)
    {
        List<AvisoDaLista> avisos = [];

        List<ItemDaLista> goleiros = ManterPrimeiraOcorrencia(itens.Goleiros, avisos);
        List<ItemDaLista> jogadores = ManterPrimeiraOcorrencia(itens.Jogadores, avisos);
        List<ItemDaLista> espera = ManterPrimeiraOcorrencia(itens.Espera, avisos);

        jogadores = RemoverNomesMantidosEm(jogadores, goleiros, SecaoDaLista.Goleiros, avisos);
        espera = RemoverNomesMantidosEm(espera, goleiros, SecaoDaLista.Goleiros, avisos);
        espera = RemoverNomesMantidosEm(espera, jogadores, SecaoDaLista.Jogadores, avisos);

        return new ListaDoGrupo
        {
            Goleiros = [.. goleiros.Select(goleiro => goleiro.Nome)],
            Jogadores = [.. jogadores.Select(jogador => new JogadorListado { Nome = jogador.Nome, Pago = jogador.Pago })],
            Espera = [.. espera.Select(jogadorEmEspera => new JogadorListado { Nome = jogadorEmEspera.Nome, Pago = jogadorEmEspera.Pago })],
            Jogo = jogo,
            Avisos = avisos,
        };
    }

    private static List<ItemDaLista> ManterPrimeiraOcorrencia(List<ItemDaLista> itens, List<AvisoDaLista> avisos)
    {
        List<ItemDaLista> mantidos = [];
        Dictionary<string, ItemDaLista> mantidosPorChave = new(StringComparer.Ordinal);

        foreach (ItemDaLista item in itens)
        {
            if (mantidosPorChave.TryGetValue(item.Chave, out ItemDaLista? primeiro) is false)
            {
                mantidosPorChave.Add(item.Chave, item);
                mantidos.Add(item);
                continue;
            }

            if (primeiro.Pago != item.Pago)
            {
                AdicionarUmaVez(avisos, new NomeRepetidoComMarcaDiferente { Nome = primeiro.Nome });
            }
        }

        return mantidos;
    }

    private static List<ItemDaLista> RemoverNomesMantidosEm(
        List<ItemDaLista> itens,
        List<ItemDaLista> maiorPrioridade,
        SecaoDaLista mantidoEm,
        List<AvisoDaLista> avisos)
    {
        Dictionary<string, ItemDaLista> maiorPrioridadePorChave =
            maiorPrioridade.ToDictionary(item => item.Chave, StringComparer.Ordinal);
        List<ItemDaLista> restantes = [];

        foreach (ItemDaLista item in itens)
        {
            if (maiorPrioridadePorChave.TryGetValue(item.Chave, out ItemDaLista? mantido))
            {
                AdicionarUmaVez(avisos, new NomeEmDuasSecoes { Nome = mantido.Nome, MantidoEm = mantidoEm });
                continue;
            }

            restantes.Add(item);
        }

        return restantes;
    }

    private static void AdicionarUmaVez(List<AvisoDaLista> avisos, AvisoDaLista aviso)
    {
        if (avisos.Contains(aviso) is false)
        {
            avisos.Add(aviso);
        }
    }

    [GeneratedRegex(@"^[0-9]+(?:\s*[.)\-]\s*|\s+|$)(?<nome>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex ItemNumerado();

    [GeneratedRegex(@"^(?:[-•] )?(?<nome>[\p{L}\p{M}](?:[\p{L}\p{M}\p{Nd} '’()\-]|\.(?= |$))*)$", RegexOptions.CultureInvariant)]
    private static partial Regex ItemSemNumeracao();

    private sealed record ItemDaLista
    {
        public required string Nome { get; init; }

        public required string Chave { get; init; }

        public required bool Pago { get; init; }
    }

    private sealed class ItensDaSecao
    {
        public List<ItemDaLista> Goleiros { get; } = [];

        public List<ItemDaLista> Jogadores { get; } = [];

        public List<ItemDaLista> Espera { get; } = [];

        public List<ItemDaLista> Em(SecaoDaLista secao) => secao switch
        {
            SecaoDaLista.Goleiros => Goleiros,
            SecaoDaLista.Jogadores => Jogadores,
            SecaoDaLista.Espera => Espera,
            _ => throw new ArgumentOutOfRangeException(nameof(secao), secao, "Seção de lista desconhecida."),
        };
    }
}
