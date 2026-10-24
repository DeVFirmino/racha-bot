using Microsoft.Extensions.Options;
using PeladaBot.Acesso;
using PeladaBot.Configuracao;
using PeladaBot.Listas;
using PeladaBot.Respostas;
using PeladaBot.WhatsApp;

namespace PeladaBot.EscolhaDaLista;

/// <summary>
/// Escolhe a lista que um comando usa: uma mensagem citada vence por mais antiga que seja; senão a
/// lista mais recente dentro de <see cref="OpcoesDeLimites.IdadeMaximaDaLista"/>, da presidência para !sortear ou de
/// qualquer um para !pendentes. Os logs carregam só quantidades e idades, nunca um nome ou o texto de uma lista.
/// </summary>
public sealed partial class SeletorDeLista
{
    private readonly IHistoricoDoGrupo _historico;
    private readonly Presidencia _presidencia;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<OpcoesDoBot> _opcoes;
    private readonly ILogger<SeletorDeLista> _logger;

    public SeletorDeLista(
        IHistoricoDoGrupo historico,
        Presidencia presidencia,
        TimeProvider timeProvider,
        IOptions<OpcoesDoBot> opcoes,
        ILogger<SeletorDeLista> logger)
    {
        _historico = historico;
        _presidencia = presidencia;
        _timeProvider = timeProvider;
        _opcoes = opcoes;
        _logger = logger;
    }

    public async Task<ListaEscolhida> SelecionarAsync(
        string idDoGrupo, MensagemCitada? citada, AutoresDaLista autores, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(idDoGrupo);

        if (citada is not null)
        {
            return await SelecionarCitadaAsync(idDoGrupo, citada, cancellationToken);
        }

        return await SelecionarMaisRecenteAsync(idDoGrupo, autores, cancellationToken);
    }

    private async Task<ListaEscolhida> SelecionarCitadaAsync(
        string idDoGrupo, MensagemCitada citada, CancellationToken cancellationToken)
    {
        string? texto = citada.Texto;

        if (string.IsNullOrWhiteSpace(texto))
        {
            MensagemDoGrupo? mensagem = await _historico.ObterMensagemAsync(idDoGrupo, citada.Id, cancellationToken);

            if (mensagem is null)
            {
                RegistrarMensagemCitadaIlegivel(_logger, MascaraDeId.AplicarNaMensagem(citada.Id));

                return new ListaCitadaIlegivel();
            }

            texto = mensagem.Texto;
        }

        ListaDoGrupo? lista = texto is null ? null : LeitorDaLista.Ler(texto);

        if (lista is null)
        {
            RegistrarMensagemCitadaNaoEhLista(_logger, MascaraDeId.AplicarNaMensagem(citada.Id));

            return new ListaNaoEncontrada();
        }

        return new ListaEncontrada { Lista = lista, Origem = null };
    }

    private async Task<ListaEscolhida> SelecionarMaisRecenteAsync(
        string idDoGrupo, AutoresDaLista autores, CancellationToken cancellationToken)
    {
        DateTimeOffset agora = _timeProvider.GetUtcNow();
        TimeSpan idadeMaxima = _opcoes.Value.Limites.IdadeMaximaDaLista;
        DateTimeOffset desde = agora - idadeMaxima;

        IReadOnlyList<MensagemDoGrupo>? mensagens =
            await _historico.ObterMensagensDesdeAsync(idDoGrupo, desde, cancellationToken);

        if (mensagens is null)
        {
            RegistrarHistoricoIndisponivel(_logger, autores);

            return new ListaNaoEncontrada();
        }

        // OrderBy é estável: mensagens enviadas no mesmo segundo mantêm a ordem de chegada, e a última vence.
        MensagemDoGrupo[] candidatas = [.. mensagens
            .Where(mensagem => agora - mensagem.EnviadaEm <= idadeMaxima)
            .Where(mensagem => EhAutor(idDoGrupo, mensagem, autores))
            .OrderBy(mensagem => mensagem.EnviadaEm)];

        for (int indice = candidatas.Length - 1; indice >= 0; indice--)
        {
            MensagemDoGrupo mensagem = candidatas[indice];
            ListaDoGrupo? lista = mensagem.Texto is null ? null : LeitorDaLista.Ler(mensagem.Texto);

            if (lista is not null)
            {
                RegistrarUltimaListaEncontrada(_logger, autores, agora - mensagem.EnviadaEm, mensagens.Count);

                return new ListaEncontrada
                {
                    Lista = lista,
                    Origem = new OrigemDaLista { NomeDoRemetente = mensagem.NomeDoRemetente, EnviadaEm = mensagem.EnviadaEm },
                };
            }
        }

        RegistrarNenhumaListaRecente(_logger, autores, idadeMaxima, mensagens.Count);

        return new ListaNaoEncontrada();
    }

    private bool EhAutor(string idDoGrupo, MensagemDoGrupo mensagem, AutoresDaLista autores) => autores switch
    {
        AutoresDaLista.QualquerUm => true,
        AutoresDaLista.Presidencia =>
            mensagem.IdDoRemetente is not null && _presidencia.Inclui(idDoGrupo, mensagem.IdDoRemetente),
        _ => throw new ArgumentOutOfRangeException(nameof(autores), autores, "Autores de lista desconhecidos."),
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mensagem citada {IdDaMensagem}: sem texto, ilegível")]
    private static partial void RegistrarMensagemCitadaIlegivel(ILogger logger, string idDaMensagem);

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensagem citada {IdDaMensagem} não é uma lista")]
    private static partial void RegistrarMensagemCitadaNaoEhLista(ILogger logger, string idDaMensagem);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Última lista de {Autores}: o histórico do grupo não pôde ser lido")]
    private static partial void RegistrarHistoricoIndisponivel(ILogger logger, AutoresDaLista autores);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Última lista de {Autores}: {Idade} de idade ({QuantidadeDeMensagens} msgs)")]
    private static partial void RegistrarUltimaListaEncontrada(
        ILogger logger, AutoresDaLista autores, TimeSpan idade, int quantidadeDeMensagens);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Nenhuma lista de {Autores} em {IdadeMaxima} ({QuantidadeDeMensagens} msgs)")]
    private static partial void RegistrarNenhumaListaRecente(
        ILogger logger, AutoresDaLista autores, TimeSpan idadeMaxima, int quantidadeDeMensagens);
}
