using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using PeladaBot.Acesso;
using PeladaBot.Comandos;
using PeladaBot.Configuracao;
using PeladaBot.Mensageria;
using PeladaBot.Respostas;

namespace PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;

/// <summary>
/// !ajuda: o guia de como participar, para qualquer integrante, uma vez por janela de recusa fora da presidência
/// a presidência recebe também o bloco do que só ela usa.
/// </summary>
public sealed class CasoDeUsoEnviarAjuda : ICasoDeUsoEnviarAjuda
{
    private readonly IValidator<PedidoEnviarAjuda> _validador;
    private readonly ControleDeAcesso _controle;
    private readonly EnviadorDeRespostas _respostas;
    private readonly Presidencia _presidencia;
    private readonly IOptions<OpcoesDoBot> _opcoes;

    public CasoDeUsoEnviarAjuda(
        IValidator<PedidoEnviarAjuda> validador,
        ControleDeAcesso controle,
        EnviadorDeRespostas respostas,
        Presidencia presidencia,
        IOptions<OpcoesDoBot> opcoes)
    {
        _validador = validador;
        _controle = controle;
        _respostas = respostas;
        _presidencia = presidencia;
        _opcoes = opcoes;
    }

    public async Task Executar(PedidoEnviarAjuda pedido, CancellationToken cancellationToken)
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
            Comando = TipoDeComando.Ajuda,
        };

        Admissao admissao = _controle.Verificar(pedido.IdDoGrupo, pedido.IdDoRemetente, TipoDeComando.Ajuda);

        string? resposta = admissao switch
        {
            Permitido => Guia(pedido),
            Recusado recusado => recusado.Resposta,
            Ignorado => null,
            _ => throw new InvalidOperationException($"Uma admissão sem tratamento: {admissao.GetType().Name}."),
        };

        if (resposta is null)
        {
            return;
        }

        await _respostas.EnviarTextoAsync(destino, resposta, cancellationToken);
    }

    private string Guia(PedidoEnviarAjuda pedido)
    {
        OpcoesDoGrupo grupo = _opcoes.Value.Grupos.Single(grupo => string.Equals(grupo.Id, pedido.IdDoGrupo, StringComparison.Ordinal));

        return TextoDeAjuda.Compor(
            grupo.TituloDoCard,
            grupo.ValorDaPelada,
            grupo.LinkDePagamento,
            _opcoes.Value.Limites.JanelaDeCobranca,
            _presidencia.Inclui(pedido.IdDoGrupo, pedido.IdDoRemetente));
    }
}
