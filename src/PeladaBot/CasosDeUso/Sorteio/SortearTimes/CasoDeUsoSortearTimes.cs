using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using PeladaBot.Acesso;
using PeladaBot.Card;
using PeladaBot.Comandos;
using PeladaBot.Configuracao;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Listas;
using PeladaBot.Mensageria;
using PeladaBot.Respostas;
using PeladaBot.Sorteio;
using PeladaBot.WhatsApp;

namespace PeladaBot.CasosDeUso.Sorteio.SortearTimes;

/// <summary>
/// !sortear e !times: a presidência sorteia times de uma lista, um card por campo, o primeiro com a legenda (,
/// ). Sem sorteio a resposta é só texto; uma falha no card recua para a legenda sozinha
/// .
/// </summary>
public sealed partial class CasoDeUsoSortearTimes : ICasoDeUsoSortearTimes
{
    private readonly IValidator<PedidoSortearTimes> _validador;
    private readonly ControleDeAcesso _controle;
    private readonly SeletorDeLista _seletorDeLista;
    private readonly EnviadorDeRespostas _respostas;
    private readonly TimeProvider _timeProvider;
    private readonly Random _random;
    private readonly IOptions<OpcoesDoBot> _opcoes;
    private readonly ProvedorDeIdentidadeDoCard _identidades;
    private readonly ILogger<CasoDeUsoSortearTimes> _logger;

    public CasoDeUsoSortearTimes(
        IValidator<PedidoSortearTimes> validador,
        ControleDeAcesso controle,
        SeletorDeLista seletorDeLista,
        EnviadorDeRespostas respostas,
        TimeProvider timeProvider,
        Random random,
        IOptions<OpcoesDoBot> opcoes,
        ProvedorDeIdentidadeDoCard identidades,
        ILogger<CasoDeUsoSortearTimes> logger)
    {
        _validador = validador;
        _controle = controle;
        _seletorDeLista = seletorDeLista;
        _respostas = respostas;
        _timeProvider = timeProvider;
        _random = random;
        _opcoes = opcoes;
        _identidades = identidades;
        _logger = logger;
    }

    public async Task Executar(PedidoSortearTimes pedido, CancellationToken cancellationToken)
    {
        ValidationResult validacao = _validador.Validate(pedido);

        if (validacao.IsValid is false)
        {
            throw new ExcecaoDeValidacao([.. validacao.Errors.Select(erro => erro.ErrorMessage)]);
        }

        DestinoDaResposta resposta = new()
        {
            IdDoGrupo = pedido.IdDoGrupo,
            IdDaMensagem = pedido.IdDaMensagem,
            IdDoRemetente = pedido.IdDoRemetente,
            Comando = TipoDeComando.Sortear,
        };

        Admissao admissao = _controle.Verificar(pedido.IdDoGrupo, pedido.IdDoRemetente, TipoDeComando.Sortear);

        if (admissao is Ignorado)
        {
            return;
        }

        if (admissao is Recusado recusado)
        {
            await _respostas.EnviarTextoAsync(resposta, recusado.Resposta, cancellationToken);

            return;
        }

        ListaEscolhida escolha = await _seletorDeLista.SelecionarAsync(
            pedido.IdDoGrupo, pedido.Citada, AutoresDaLista.Presidencia, cancellationToken);

        switch (escolha)
        {
            case ListaEncontrada encontrada:
                await SortearAsync(resposta, encontrada, pedido.SomentePagos, cancellationToken);
                break;
            case ListaNaoEncontrada:
                await _respostas.EnviarTextoAsync(resposta, RespostasDeErro.CitarAListaParaSortear, cancellationToken);
                break;
            case ListaCitadaIlegivel:
                await _respostas.EnviarTextoAsync(resposta, RespostasDeErro.ListaCitadaIlegivel, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Uma escolha de lista sem tratamento: {escolha.GetType().Name}.");
        }
    }

    private async Task SortearAsync(
        DestinoDaResposta resposta, ListaEncontrada encontrada, bool somentePagos, CancellationToken cancellationToken)
    {
        ResultadoDoSorteio resultado = MotorDoSorteio.Sortear(encontrada.Lista, _random, somentePagos);
        string idDaMensagem = MascaraDeId.AplicarNaMensagem(resposta.IdDaMensagem);

        if (resultado is not TimesSorteados sorteados)
        {
            int quantidadePaga = encontrada.Lista.Jogadores.Count(jogador => jogador.Pago);
            string respostaSemSorteio = RespostaSemSorteio.Compor(
                resultado,
                encontrada.Lista,
                encontrada.Origem,
                Grupo(resposta.IdDoGrupo).MensagensDeCobranca,
                _random,
                new DadosDoPagamento { Valor = Grupo(resposta.IdDoGrupo).ValorDaPelada, Link = Grupo(resposta.IdDoGrupo).LinkDePagamento },
                Fuso());

            RegistrarSemSorteio(_logger, idDaMensagem, resultado.GetType().Name, quantidadePaga);
            await _respostas.EnviarTextoAsync(resposta, respostaSemSorteio, cancellationToken);

            return;
        }

        RegistrarSorteado(_logger, idDaMensagem, sorteados.Times.Count, sorteados.IgnorandoPendentes.Count);

        OpcoesDoGrupo grupo = Grupo(resposta.IdDoGrupo);
        string legenda = LegendaDoSorteio.Compor(
            new EntradaDaLegendaDoSorteio
            {
                Sorteado = sorteados,
                Lista = encontrada.Lista,
                Origem = encontrada.Origem,
                SorteadoEm = _timeProvider.GetUtcNow(),
                Modelos = [.. grupo.ModelosDaLegenda.Select(ParaModeloDaLegenda)],
                ApelidosAvulsos = ParaApelidosAvulsos(grupo.ApelidosAvulsos),
                FrasesDoRodape = grupo.FrasesDoRodape,
                Fuso = Fuso(),
            },
            _random);
        IReadOnlyList<byte[]>? cards = DesenharCards(sorteados, resposta.IdDoGrupo, idDaMensagem);

        if (cards is null)
        {
            await _respostas.EnviarTextoAsync(resposta, legenda, cancellationToken);

            return;
        }

        await EnviarCardsAsync(resposta, cards, legenda, idDaMensagem, cancellationToken);
    }

    /// <summary>
    /// Os cards, ou null em qualquer exceção ao desenhar qualquer um deles, para o grupo ainda receber os times como
    /// texto.
    /// </summary>
    private IReadOnlyList<byte[]>? DesenharCards(TimesSorteados sorteados, string idDoGrupo, string idDaMensagem)
    {
        // Mais times do que o card comporta é um sorteio normal, que vai só em texto; não é falha.
        if (LayoutDoCard.Comporta(sorteados) is false)
        {
            RegistrarSoTexto(_logger, idDaMensagem, sorteados.Times.Count);

            return null;
        }

        try
        {
            IReadOnlyList<CardDoTime> cards = LayoutDoCard.Compor(sorteados, _identidades.Obter(idDoGrupo));

            return [.. cards.Select(DesenhistaDoCard.Desenhar)];
        }
        catch (Exception excecao)
        {
            RegistrarCardFalhou(_logger, idDaMensagem, MascaraDeExcecao.Aplicar(excecao));

            return null;
        }
    }

    /// <summary>
    /// Um card de cada vez, a legenda só no primeiro. Se o WAHA rejeita um card, a legenda vai sozinha em texto, com
    /// todos os times, e nada mais sai. Se o envio fica incerto, ou o disjuntor o retém, nada mais sai: a
    /// legenda sozinha poderia repetir o que já chegou.
    /// </summary>
    private async Task EnviarCardsAsync(
        DestinoDaResposta resposta, IReadOnlyList<byte[]> cards, string legenda, string idDaMensagem, CancellationToken cancellationToken)
    {
        for (int indice = 0; indice < cards.Count; indice++)
        {
            string? legendaDoCard = indice == 0 ? legenda : null;
            ResultadoDoEnvio? resultado = await _respostas.EnviarImagemAsync(resposta, cards[indice], legendaDoCard, cancellationToken);

            switch (resultado)
            {
                case Enviado:
                    continue;
                case Rejeitado:
                    RegistrarLegendaSozinha(_logger, idDaMensagem);
                    await _respostas.EnviarTextoAsync(resposta, legenda, cancellationToken);

                    return;
                case Incerto:
                    RegistrarNadaMais(_logger, idDaMensagem);

                    return;
                default:
                    return;
            }
        }
    }

    private static ModeloDaLegenda ParaModeloDaLegenda(OpcoesDoModeloDaLegenda modelo) => new()
    {
        Cabecalho = modelo.Cabecalho,
        LinhaDoHorario = modelo.LinhaDoHorario,
        ApelidoLaranja = modelo.ApelidoLaranja,
        ApelidoVerde = modelo.ApelidoVerde,
        ApelidoCinza = modelo.ApelidoCinza,
        ApelidoAzul = modelo.ApelidoAzul,
        ApelidoSemColete = modelo.ApelidoSemColete,
    };

    private static ApelidosAvulsos ParaApelidosAvulsos(OpcoesDosApelidosAvulsos apelidos) => new()
    {
        Laranja = apelidos.Laranja,
        Verde = apelidos.Verde,
        Cinza = apelidos.Cinza,
        Azul = apelidos.Azul,
        SemColete = apelidos.SemColete,
    };

    private OpcoesDoGrupo Grupo(string idDoGrupo) =>
        _opcoes.Value.Grupos.Single(grupo => string.Equals(grupo.Id, idDoGrupo, StringComparison.Ordinal));

    private TimeZoneInfo Fuso() => HorarioDoGrupo.Carregar(_opcoes.Value.Fuso);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Sorteio de {IdDaMensagem}: sem sorteio ({Resultado}), {QuantidadePaga} pagos")]
    private static partial void RegistrarSemSorteio(ILogger logger, string idDaMensagem, string resultado, int quantidadePaga);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Sorteio de {IdDaMensagem}: {QuantidadeDeTimes} times, {QuantidadeIgnorada} ignorados sem ✅")]
    private static partial void RegistrarSorteado(
        ILogger logger, string idDaMensagem, int quantidadeDeTimes, int quantidadeIgnorada);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Sorteio de {IdDaMensagem}: {QuantidadeDeTimes} times não cabem no card, vai só o texto")]
    private static partial void RegistrarSoTexto(ILogger logger, string idDaMensagem, int quantidadeDeTimes);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "O card do sorteio de {IdDaMensagem} não pôde ser desenhado: {Falha}")]
    private static partial void RegistrarCardFalhou(ILogger logger, string idDaMensagem, string falha);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "WAHA rejeitou o card do sorteio de {IdDaMensagem}: a legenda vai sozinha")]
    private static partial void RegistrarLegendaSozinha(ILogger logger, string idDaMensagem);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "O card do sorteio de {IdDaMensagem} já pode estar no grupo: sem legenda sozinha (evita duas)")]
    private static partial void RegistrarNadaMais(ILogger logger, string idDaMensagem);
}
