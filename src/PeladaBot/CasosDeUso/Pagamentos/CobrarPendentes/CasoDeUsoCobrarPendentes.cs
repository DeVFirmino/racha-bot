using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using PeladaBot.Acesso;
using PeladaBot.Comandos;
using PeladaBot.Configuracao;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Mensageria;
using PeladaBot.Respostas;

namespace PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;

/// <summary>
/// !cobrar (ou !pendentes): cobra quem está sem ✅ com uma das mensagens da diretoria, sorteada e preenchida com os
/// nomes. A presidência cobra a qualquer hora; os outros, uma vez por janela de cobrança no grupo. Sem
/// citação pega a lista mais recente de qualquer um, onde aparece quem chegou depois e não pagou. Só responde
/// ao comando; nunca cobra sozinho.
/// </summary>
public sealed class CasoDeUsoCobrarPendentes : ICasoDeUsoCobrarPendentes
{
    private readonly IValidator<PedidoCobrarPendentes> _validador;
    private readonly ControleDeAcesso _controle;
    private readonly SeletorDeLista _seletorDeLista;
    private readonly EnviadorDeRespostas _respostas;
    private readonly Random _random;
    private readonly IOptions<OpcoesDoBot> _opcoes;

    public CasoDeUsoCobrarPendentes(
        IValidator<PedidoCobrarPendentes> validador,
        ControleDeAcesso controle,
        SeletorDeLista seletorDeLista,
        EnviadorDeRespostas respostas,
        Random random,
        IOptions<OpcoesDoBot> opcoes)
    {
        _validador = validador;
        _controle = controle;
        _seletorDeLista = seletorDeLista;
        _respostas = respostas;
        _random = random;
        _opcoes = opcoes;
    }

    public async Task Executar(PedidoCobrarPendentes pedido, CancellationToken cancellationToken)
    {
        ValidationResult validacao = _validador.Validate(pedido);

        if (validacao.IsValid is false)
        {
            throw new ExcecaoDeValidacao([.. validacao.Errors.Select(erro => erro.ErrorMessage)]);
        }

        DestinoDaResposta destino = new()
        {
            IdDoGrupo = pedido.IdDoGrupo,
            IdDaMensagem = pedido.IdDaMensagem,
            IdDoRemetente = pedido.IdDoRemetente,
            Comando = TipoDeComando.Cobrar,
        };

        Admissao admissao = _controle.Verificar(pedido.IdDoGrupo, pedido.IdDoRemetente, TipoDeComando.Cobrar);

        if (admissao is Ignorado)
        {
            return;
        }

        if (admissao is Recusado recusado)
        {
            await _respostas.EnviarTextoAsync(destino, recusado.Resposta, cancellationToken);

            return;
        }

        ListaEscolhida escolha = await _seletorDeLista.SelecionarAsync(
            pedido.IdDoGrupo, pedido.Citada, AutoresDaLista.QualquerUm, cancellationToken);

        string resposta = escolha switch
        {
            ListaEncontrada encontrada =>
                RespostaDeCobranca.Compor(encontrada.Lista, encontrada.Origem, _opcoes.Value.ObterGrupo(pedido.IdDoGrupo).MensagensDeCobranca, _random, Pagamento(pedido.IdDoGrupo), HorarioDoGrupo.Carregar(_opcoes.Value.Fuso)),
            ListaNaoEncontrada => RespostasDeErro.CitarAListaParaCobrar,
            ListaCitadaIlegivel => RespostasDeErro.ListaCitadaIlegivel,
            _ => throw new InvalidOperationException($"Uma escolha de lista sem tratamento: {escolha.GetType().Name}."),
        };

        await _respostas.EnviarTextoAsync(destino, resposta, cancellationToken);
    }

    private DadosDoPagamento Pagamento(string idDoGrupo)
    {
        OpcoesDoGrupo grupo = _opcoes.Value.ObterGrupo(idDoGrupo);

        return new DadosDoPagamento { Valor = grupo.ValorDaPelada, Link = grupo.LinkDePagamento };
    }
}
