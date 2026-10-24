using System.Text.Json;
using Microsoft.Extensions.Options;
using PeladaBot.Comandos;
using PeladaBot.Configuracao;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Mensageria;
using PeladaBot.WhatsApp;

namespace PeladaBot.CasosDeUso.Webhooks.ReceberEventoDoWaha;

/// <summary>
/// Os filtros baratos do webhook, para ele poder responder de imediato: um comando, no grupo
/// configurado, de outra pessoa, não muito antigo, vai para a fila. Um evento malformado é
/// descartado com um log.
/// </summary>
public sealed partial class CasoDeUsoReceberEventoDoWaha : ICasoDeUsoReceberEventoDoWaha
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly FilaDeComandos _fila;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<OpcoesDoBot> _opcoes;
    private readonly ILogger<CasoDeUsoReceberEventoDoWaha> _logger;

    public CasoDeUsoReceberEventoDoWaha(
        FilaDeComandos fila,
        TimeProvider timeProvider,
        IOptions<OpcoesDoBot> opcoes,
        ILogger<CasoDeUsoReceberEventoDoWaha> logger)
    {
        _fila = fila;
        _timeProvider = timeProvider;
        _opcoes = opcoes;
        _logger = logger;
    }

    public void Executar(PedidoReceberEventoDoWaha? pedido)
    {
        if (pedido is null)
        {
            RegistrarMalformado(_logger, "o corpo não é um evento do WAHA");

            return;
        }

        switch (pedido.Evento)
        {
            case EventosWaha.Mensagem:
                ReceberMensagem(pedido.Payload);
                break;
            case EventosWaha.StatusDaSessao:
                ReceberStatusDaSessao(pedido.Payload);
                break;
            default:
                RegistrarEventoIgnorado(_logger, pedido.Evento);
                break;
        }
    }

    private void ReceberMensagem(JsonElement? payload)
    {
        MensagemWaha? mensagem = Ler<MensagemWaha>(payload);

        if (mensagem?.Id is null || mensagem.De is null || mensagem.EnviadaEm is not DateTimeOffset enviadaEm)
        {
            RegistrarMalformado(_logger, "uma mensagem sem id, chat ou hora de envio");

            return;
        }

        if (EhGrupoConfigurado(mensagem.De) is false || mensagem.FromMe)
        {
            return;
        }

        ComandoLido? comando = LeitorDeComando.Ler(mensagem.Corpo);

        if (comando is null)
        {
            return;
        }

        TipoDeComando tipo = comando.Tipo;

        if (mensagem.Participante is null)
        {
            RegistrarMalformado(_logger, "uma mensagem de grupo sem remetente");

            return;
        }

        string idDaMensagem = MascaraDeId.AplicarNaMensagem(mensagem.Id);
        TimeSpan idade = _timeProvider.GetUtcNow() - enviadaEm;

        if (idade > _opcoes.Value.Limites.IdadeMaximaDoComando)
        {
            RegistrarMuitoAntigo(_logger, tipo, idDaMensagem, idade);

            return;
        }

        ComandoRecebido recebido = new()
        {
            Comando = tipo,
            SomentePagos = comando.SomentePagos,
            IdDaMensagem = mensagem.Id,
            IdDoGrupo = mensagem.De,
            IdDoRemetente = mensagem.Participante,
            Citada = Citada(mensagem.RespondeA),
        };

        if (_fila.TentarEnfileirar(recebido) is false)
        {
            RegistrarFilaRecusou(_logger, tipo, idDaMensagem);

            return;
        }

        RegistrarEnfileirado(_logger, tipo, idDaMensagem, MascaraDeId.Aplicar(mensagem.Participante), recebido.Citada is not null);
    }

    private void ReceberStatusDaSessao(JsonElement? payload)
    {
        SessaoWaha? sessao = Ler<SessaoWaha>(payload);

        if (sessao?.Status is null)
        {
            RegistrarMalformado(_logger, "um status de sessão sem status");

            return;
        }

        if (sessao.Status is StatusDaSessaoWaha.Falhou or StatusDaSessaoWaha.Parada)
        {
            RegistrarSessaoFora(_logger, sessao.Nome, sessao.Status);

            return;
        }

        RegistrarStatusDaSessao(_logger, sessao.Nome, sessao.Status);
    }

    private bool EhGrupoConfigurado(string idDoChat) =>
        _opcoes.Value.Grupos.Any(grupo => string.Equals(grupo.Id, idDoChat, StringComparison.Ordinal));

    /// <summary>A citação; texto null significa que o WAHA a omitiu, então a seleção pede a mensagem.</summary>
    private static MensagemCitada? Citada(RespostaAMensagemWaha? respondeA)
    {
        if (string.IsNullOrWhiteSpace(respondeA?.Id))
        {
            return null;
        }

        return new MensagemCitada { Id = respondeA.Id, Texto = respondeA.Corpo };
    }

    private static T? Ler<T>(JsonElement? payload)
        where T : class
    {
        if (payload is not JsonElement elemento || elemento.ValueKind is not JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return elemento.Deserialize<T>(_json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook do WAHA descartado: {Motivo}")]
    private static partial void RegistrarMalformado(ILogger logger, string motivo);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Evento do WAHA {Evento} ignorado")]
    private static partial void RegistrarEventoIgnorado(ILogger logger, string? evento);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Comando} {IdDaMensagem} descartado: {Idade} de idade quando chegou")]
    private static partial void RegistrarMuitoAntigo(ILogger logger, TipoDeComando comando, string idDaMensagem, TimeSpan idade);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Comando} {IdDaMensagem} descartado: a fila está cheia ou fechada")]
    private static partial void RegistrarFilaRecusou(ILogger logger, TipoDeComando comando, string idDaMensagem);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Comando} {IdDaMensagem} de {IdDoRemetente} enfileirado (cita uma mensagem: {CitaUmaMensagem})")]
    private static partial void RegistrarEnfileirado(
        ILogger logger, TipoDeComando comando, string idDaMensagem, string idDoRemetente, bool citaUmaMensagem);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Sessão do WAHA {Sessao} está {Status}: o bot não lê nem responde até estar WORKING de novo")]
    private static partial void RegistrarSessaoFora(ILogger logger, string? sessao, string status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sessão do WAHA {Sessao} está {Status}")]
    private static partial void RegistrarStatusDaSessao(ILogger logger, string? sessao, string status);
}
