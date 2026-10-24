using Microsoft.Extensions.Options;
using PeladaBot.Configuracao;

namespace PeladaBot.Card;

/// <summary>
/// A identidade do card de cada grupo, montada uma vez a partir da configuração: o escudo é lido do disco no
/// startup por VerificacaoDaIdentidadeDoCard, não a cada sorteio. Uma falha de leitura impede a inicialização.
/// </summary>
public sealed class ProvedorDeIdentidadeDoCard
{
    private readonly Dictionary<string, IdentidadeDoCard> _porGrupo;

    public ProvedorDeIdentidadeDoCard(IOptions<OpcoesDoBot> opcoes)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        _porGrupo = opcoes.Value.Grupos
            .GroupBy(grupo => grupo.Id, StringComparer.Ordinal)
            .ToDictionary(grupos => grupos.Key, grupos => Montar(grupos.First()), StringComparer.Ordinal);
    }

    public IdentidadeDoCard Obter(string idDoGrupo) =>
        _porGrupo.GetValueOrDefault(idDoGrupo) ?? IdentidadeDoCard.Padrao;

    private static IdentidadeDoCard Montar(OpcoesDoGrupo grupo) => new()
    {
        Titulo = string.IsNullOrWhiteSpace(grupo.TituloDoCard) ? IdentidadeDoCard.Padrao.Titulo : grupo.TituloDoCard.Trim(),
        Escudo = string.IsNullOrWhiteSpace(grupo.CaminhoDoEscudo) ? null : LerEscudo(grupo.CaminhoDoEscudo),
    };

    private static byte[] LerEscudo(string caminho)
    {
        try
        {
            return File.ReadAllBytes(caminho);
        }
        catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
        {
            // A exceção original pode conter o caminho com dados pessoais.
            throw new InvalidOperationException("Não foi possível ler o escudo do card. Verifique o arquivo configurado e suas permissões antes de iniciar o bot.");
        }
    }
}
