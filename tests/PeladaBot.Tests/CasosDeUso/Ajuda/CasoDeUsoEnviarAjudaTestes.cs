using PeladaBot.CasosDeUso;
using PeladaBot.CasosDeUso.Ajuda.EnviarAjuda;
using PeladaBot.Respostas;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.CasosDeUso.Ajuda;

public sealed class CasoDeUsoEnviarAjudaTestes : IDisposable
{
    private const string IdDoComando = "false_120363000000001234@g.us_3EB0E5F6A7B8C9D00A03_35600000303@c.us";

    private readonly ConstrutorDeCasoDeUso _bot = new();

    public void Dispose() => _bot.Dispose();

    // A presidência recebe o guia com o bloco do que só ela usa
    [Theory]
    [InlineData(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente)]
    [InlineData(ConstrutorDeCasoDeUso.LidDoPresidente)]
    [InlineData(ConstrutorDeCasoDeUso.IdDoTelefoneDoVice)]
    public async Task DeveResponderOGuiaComOBlocoDaPresidenciaCitandoOComandoQuandoAPresidenciaPede(string idDoRemetente)
    {
        await Ajuda(idDoRemetente);

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);
        mensagem.Texto.Should().Be(TextoDeAjuda.Compor(string.Empty, string.Empty, string.Empty, TimeSpan.FromMinutes(30), paraAPresidencia: true));
        mensagem.IdDaMensagemRespondida.Should().Be(IdDoComando);
    }

    // Qualquer integrante recebe o guia, sem o bloco da presidência
    [Fact]
    public async Task DeveResponderOGuiaSemOBlocoDaPresidenciaQuandoUmIntegrantePede()
    {
        await Ajuda(ConstrutorDeCasoDeUso.IdDoIntegrante);

        string? texto = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which.Texto;
        texto.Should().Be(TextoDeAjuda.Compor(string.Empty, string.Empty, string.Empty, TimeSpan.FromMinutes(30), paraAPresidencia: false));
        texto.Should().NotContain("PRESIDENT AND ADMINS ONLY");
    }

    // Fora da presidência, uma vez a cada 10 minutos por pessoa
    [Fact]
    public async Task DeveIgnorarOIntegranteQuandoElePedeDeNovoEmMenosDeDezMinutos()
    {
        await Ajuda(ConstrutorDeCasoDeUso.IdDoIntegrante);
        _bot.Relogio.Advance(TimeSpan.FromMinutes(9));
        await Ajuda(ConstrutorDeCasoDeUso.IdDoIntegrante);

        _bot.Mensageiro.Mensagens.Should().ContainSingle();
    }

    [Fact]
    public async Task DeveResponderDeNovoQuandoDezMinutosPassaramDesdeOGuia()
    {
        await Ajuda(ConstrutorDeCasoDeUso.IdDoIntegrante);
        _bot.Relogio.Advance(TimeSpan.FromMinutes(10));
        await Ajuda(ConstrutorDeCasoDeUso.IdDoIntegrante);

        _bot.Mensageiro.Mensagens.Should().HaveCount(2);
    }

    // A presidência pede quantas vezes quiser
    [Fact]
    public async Task DeveResponderAPresidenciaSempreQuandoElaPedeVariasVezes()
    {
        await Ajuda(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente);
        await Ajuda(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente);
        await Ajuda(ConstrutorDeCasoDeUso.IdDoTelefoneDoVice);

        _bot.Mensageiro.Mensagens.Should().HaveCount(3);
    }

    // FluentValidation (pedido malformado nunca chega a tocar o grupo)
    [Fact]
    public async Task DeveLancarENaoEnviarNadaQuandoOPedidoEhInvalido()
    {
        CasoDeUsoEnviarAjuda casoDeUso = _bot.ConstruirEnviarAjuda();
        PedidoEnviarAjuda pedidoInvalido = new() { IdDoGrupo = string.Empty, IdDoRemetente = string.Empty, IdDaMensagem = string.Empty };

        Func<Task> executar = () => casoDeUso.Executar(pedidoInvalido, TestContext.Current.CancellationToken);

        await executar.Should().ThrowAsync<ExcecaoDeValidacao>();
        _bot.Mensageiro.Chamadas.Should().BeEmpty();
    }

    private async Task Ajuda(string idDoRemetente)
    {
        CasoDeUsoEnviarAjuda casoDeUso = _bot.ConstruirEnviarAjuda();
        PedidoEnviarAjuda pedido = new() { IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo, IdDoRemetente = idDoRemetente, IdDaMensagem = IdDoComando };

        await _bot.ExecutarAsync(() => casoDeUso.Executar(pedido, TestContext.Current.CancellationToken));
    }
}
