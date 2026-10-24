using Microsoft.Extensions.Options;
using PeladaBot.Configuracao;

namespace PeladaBot.Acesso;

/// <summary>Se um remetente é presidente ou admin de um grupo, combinado só pelo id configurado.</summary>
public sealed class Presidencia
{
    private readonly IOptions<OpcoesDoBot> _opcoes;

    public Presidencia(IOptions<OpcoesDoBot> opcoes)
    {
        _opcoes = opcoes;
    }

    /// <summary>True quando <paramref name="idDoRemetente"/> é um id da presidência de <paramref name="idDoGrupo"/>.</summary>
    public bool Inclui(string idDoGrupo, string idDoRemetente)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);
        ArgumentNullException.ThrowIfNull(idDoRemetente);

        return _opcoes.Value.Grupos
            .Where(grupo => string.Equals(grupo.Id, idDoGrupo, StringComparison.Ordinal))
            .SelectMany(grupo => grupo.Presidencia)
            .Any(membro => membro.Ids.Contains(idDoRemetente, StringComparer.Ordinal));
    }
}
