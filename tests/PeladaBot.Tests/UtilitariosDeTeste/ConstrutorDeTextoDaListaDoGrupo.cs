using System.Text;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>
/// Escreve uma lista do jeito que o grupo cola no WhatsApp, cabeçalho e notas incluídos. Dada uma quantidade, os nomes são
/// numerados: "Goleiro 1", "Jogador 1" (com ✅), "Pendente 1" (sem) e "Espera 1". Dados nomes, eles são mantidos na ordem dada.
/// </summary>
public sealed class ConstrutorDeTextoDaListaDoGrupo
{
    private readonly List<string> _goleiros = [];
    private readonly List<string> _jogadores = [];
    private readonly List<string> _espera = [];
    private int _quantidadePaga;
    private int _quantidadeNaoPaga;

    public ConstrutorDeTextoDaListaDoGrupo ComGoleiros(int quantidade)
    {
        _goleiros.AddRange(Enumerable.Range(_goleiros.Count + 1, quantidade).Select(numero => $"Goleiro {numero}🧤"));

        return this;
    }

    public ConstrutorDeTextoDaListaDoGrupo ComGoleiros(params string[] nomes)
    {
        _goleiros.AddRange(nomes.Select(nome => $"{nome}🧤"));

        return this;
    }

    public ConstrutorDeTextoDaListaDoGrupo ComJogadoresPagos(int quantidade)
    {
        _jogadores.AddRange(Enumerable.Range(_quantidadePaga + 1, quantidade).Select(numero => $"Jogador {numero} ✅"));
        _quantidadePaga += quantidade;

        return this;
    }

    public ConstrutorDeTextoDaListaDoGrupo ComJogadoresPagos(params string[] nomes)
    {
        _jogadores.AddRange(nomes.Select(nome => $"{nome} ✅"));
        _quantidadePaga += nomes.Length;

        return this;
    }

    public ConstrutorDeTextoDaListaDoGrupo ComJogadoresNaoPagos(int quantidade)
    {
        _jogadores.AddRange(Enumerable.Range(_quantidadeNaoPaga + 1, quantidade).Select(numero => $"Pendente {numero}"));
        _quantidadeNaoPaga += quantidade;

        return this;
    }

    public ConstrutorDeTextoDaListaDoGrupo ComJogadoresNaoPagos(params string[] nomes)
    {
        _jogadores.AddRange(nomes);
        _quantidadeNaoPaga += nomes.Length;

        return this;
    }

    public ConstrutorDeTextoDaListaDoGrupo ComEspera(params string[] nomes)
    {
        _espera.AddRange(nomes);

        return this;
    }

    public string Build()
    {
        StringBuilder texto = new();

        texto.Append("⚽PRÓXIMO JOGO⚽\n\n");
        texto.Append("Data: Quarta-Feira – 14/10\nHorário: 20:30h\nLocal: Campo Municipal\n\n");
        AdicionarSecao(texto, "Goleiros fixos:", _goleiros);
        AdicionarSecao(texto, "Lista de jogadores:", _jogadores);
        AdicionarSecao(texto, "Lista de Espera:", _espera);
        texto.Append("Nota 1: Para confirmar sua presença, realize o pagamento e depois adicione o seu nome na lista.");

        return texto.ToString();
    }

    private static void AdicionarSecao(StringBuilder texto, string titulo, List<string> itens)
    {
        texto.Append(titulo).Append('\n');

        for (int indice = 0; indice < itens.Count; indice++)
        {
            texto.Append(indice + 1).Append(". ").Append(itens[indice]).Append('\n');
        }

        texto.Append(itens.Count + 1).Append(".\n\n");
    }
}
