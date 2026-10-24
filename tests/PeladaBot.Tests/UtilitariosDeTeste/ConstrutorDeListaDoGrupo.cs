using PeladaBot.Listas;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Constrói uma lista do grupo. Dada uma quantidade, os nomes são numerados: "Goleiro 1", "Jogador 1" (pago), "Pendente 1"
/// (no ✅), "Espera 1" (no ✅) e "Promovido 1" (na espera, com ✅). Dados nomes, eles são mantidos na ordem dada.
/// </summary>
public sealed class ConstrutorDeListaDoGrupo
{
    private readonly List<string> _goleiros = [];
    private readonly List<JogadorListado> _jogadores = [];
    private readonly List<JogadorListado> _espera = [];
    private readonly List<AvisoDaLista> _avisos = [];

    public ConstrutorDeListaDoGrupo ComGoleiros(int quantidade)
    {
        _goleiros.AddRange(NomesNumerados("Goleiro", _goleiros.Count, quantidade));

        return this;
    }

    public ConstrutorDeListaDoGrupo ComJogadoresPagos(int quantidade)
    {
        int jaPagos = _jogadores.Count(jogador => jogador.Pago);

        _jogadores.AddRange(NomesNumerados("Jogador", jaPagos, quantidade).Select(nome => new JogadorListado { Nome = nome, Pago = true }));

        return this;
    }

    public ConstrutorDeListaDoGrupo ComJogadoresNaoPagos(int quantidade)
    {
        int jaNaoPagos = _jogadores.Count(jogador => jogador.Pago is false);

        _jogadores.AddRange(
            NomesNumerados("Pendente", jaNaoPagos, quantidade).Select(nome => new JogadorListado { Nome = nome, Pago = false }));

        return this;
    }

    public ConstrutorDeListaDoGrupo ComEspera(int quantidade)
    {
        int jaNaEspera = _espera.Count(jogador => jogador.Pago is false);

        _espera.AddRange(NomesNumerados("Espera", jaNaEspera, quantidade).Select(nome => new JogadorListado { Nome = nome, Pago = false }));

        return this;
    }

    /// <summary>Gente da espera que já pagou: entra no sorteio como qualquer pago.</summary>
    public ConstrutorDeListaDoGrupo ComEsperaPaga(int quantidade)
    {
        int jaPromovidos = _espera.Count(jogador => jogador.Pago);

        _espera.AddRange(NomesNumerados("Promovido", jaPromovidos, quantidade).Select(nome => new JogadorListado { Nome = nome, Pago = true }));

        return this;
    }

    public ConstrutorDeListaDoGrupo ComGoleiros(params string[] nomes)
    {
        _goleiros.AddRange(nomes);

        return this;
    }

    public ConstrutorDeListaDoGrupo ComJogadoresPagos(params string[] nomes)
    {
        _jogadores.AddRange(nomes.Select(nome => new JogadorListado { Nome = nome, Pago = true }));

        return this;
    }

    public ConstrutorDeListaDoGrupo ComJogadoresNaoPagos(params string[] nomes)
    {
        _jogadores.AddRange(nomes.Select(nome => new JogadorListado { Nome = nome, Pago = false }));

        return this;
    }

    public ConstrutorDeListaDoGrupo ComEspera(params string[] nomes)
    {
        _espera.AddRange(nomes.Select(nome => new JogadorListado { Nome = nome, Pago = false }));

        return this;
    }

    public ConstrutorDeListaDoGrupo ComEsperaPaga(params string[] nomes)
    {
        _espera.AddRange(nomes.Select(nome => new JogadorListado { Nome = nome, Pago = true }));

        return this;
    }

    public ConstrutorDeListaDoGrupo ComAvisos(params AvisoDaLista[] avisos)
    {
        _avisos.AddRange(avisos);

        return this;
    }

    public ListaDoGrupo Build() => new()
    {
        Goleiros = [.. _goleiros],
        Jogadores = [.. _jogadores],
        Espera = [.. _espera],
        Jogo = new InfoDoJogo(),
        Avisos = [.. _avisos],
    };

    private static IEnumerable<string> NomesNumerados(string prefixo, int jaAdicionados, int quantidade) =>
        Enumerable.Range(jaAdicionados + 1, quantidade).Select(numero => $"{prefixo} {numero}");
}
