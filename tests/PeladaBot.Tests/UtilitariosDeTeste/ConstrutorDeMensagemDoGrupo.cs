using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.UtilitariosDeTeste;

/// <summary>Constrói uma mensagem do histórico do grupo. Por padrão uma mensagem de chat de um integrante, enviada em <see cref="EnviadaEmPadrao"/>.</summary>
public sealed class ConstrutorDeMensagemDoGrupo
{
    private const string IdDoGrupo = "120363000000001234@g.us";

    public static readonly DateTimeOffset EnviadaEmPadrao = new(2026, 10, 12, 11, 29, 0, TimeSpan.Zero);

    private static int _proximaChave;

    private readonly string _chave = $"3EB0{Interlocked.Increment(ref _proximaChave):X12}";
    private string _idDoRemetente = "35600000404@c.us";
    private string? _nomeDoRemetente = "Integrante Beltrano";
    private DateTimeOffset _enviadaEm = EnviadaEmPadrao;
    private string? _texto = "bora que amanhã tem jogo";

    public ConstrutorDeMensagemDoGrupo De(string idDoRemetente, string? nomeDoRemetente)
    {
        _idDoRemetente = idDoRemetente;
        _nomeDoRemetente = nomeDoRemetente;

        return this;
    }

    public ConstrutorDeMensagemDoGrupo EnviadaEm(DateTimeOffset enviadaEm)
    {
        _enviadaEm = enviadaEm;

        return this;
    }

    public ConstrutorDeMensagemDoGrupo ComTexto(string? texto)
    {
        _texto = texto;

        return this;
    }

    public MensagemDoGrupo Build() => new()
    {
        Id = $"false_{IdDoGrupo}_{_chave}_{_idDoRemetente}",
        IdDoRemetente = _idDoRemetente,
        NomeDoRemetente = _nomeDoRemetente,
        EnviadaEm = _enviadaEm,
        Texto = _texto,
    };
}
