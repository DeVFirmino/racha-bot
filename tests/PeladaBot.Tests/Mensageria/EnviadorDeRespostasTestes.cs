using Microsoft.Extensions.Logging;
using PeladaBot.Acesso;
using PeladaBot.Comandos;
using PeladaBot.Mensageria;
using PeladaBot.Tests.UtilitariosDeTeste;
using PeladaBot.WhatsApp;

namespace PeladaBot.Tests.Mensageria;

public sealed class EnviadorDeRespostasTestes : IDisposable
{
    private const string IdDoComando = "false_120363000000001234@g.us_3EB0C767D26A1D8E0A01_35600000404@c.us";

    private static readonly DestinoDaResposta _integrantePedeOSorteio = new()
    {
        IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo,
        IdDaMensagem = IdDoComando,
        IdDoRemetente = ConstrutorDeCasoDeUso.IdDoIntegrante,
        Comando = TipoDeComando.Sortear,
    };

    private readonly ConstrutorDeCasoDeUso _bot = new();

    public void Dispose() => _bot.Dispose();

    [Fact]
    public async Task DeveResponderAoGrupoCitandoOComandoQuandoEnviaUmTexto()
    {
        EnviadorDeRespostas enviador = _bot.ConstruirEnviadorDeRespostas();

        await _bot.ExecutarAsync(() => enviador.EnviarTextoAsync(_integrantePedeOSorteio, "oi", TestContext.Current.CancellationToken));

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);
        mensagem.IdDoChat.Should().Be(ConstrutorDeCasoDeUso.IdDoGrupo);
        mensagem.Texto.Should().Be("oi");
        mensagem.IdDaMensagemRespondida.Should().Be(IdDoComando);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(14)]
    [InlineData(2026)]
    public async Task DeveMostrarDigitandoEEsperarDeUmESeisDecimosATresSegundosAntesDeEnviarQuandoResponde(int semente)
    {
        EnviadorDeRespostas enviador = _bot.ComSemente(semente).ConstruirEnviadorDeRespostas();
        Task resposta = enviador.EnviarTextoAsync(_integrantePedeOSorteio, "oi", TestContext.Current.CancellationToken);

        // A pausa espera num timer do relógio falso: antes de 1,5 s ele não dispara, e nada pode ter ido para o grupo.
        // Se ela fosse mais curta, a hora registrada no envio também denunciaria, na verificação do fim.
        _bot.Relogio.Advance(TimeSpan.FromMilliseconds(1499));
        IReadOnlyList<ChamadaDoMensageiro> enviadasAntesDaPausaMaisCurta = _bot.Mensageiro.Mensagens;
        _bot.Relogio.Advance(TimeSpan.FromMilliseconds(1501));
        await resposta.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        enviadasAntesDaPausaMaisCurta.Should().BeEmpty();
        _bot.Mensageiro.Chamadas.Select(chamada => chamada.Tipo).Should().Equal(
            TipoDeChamadaDoMensageiro.IniciarDigitacao,
            TipoDeChamadaDoMensageiro.PararDigitacao,
            TipoDeChamadaDoMensageiro.Texto);
        TimeSpan pausa = _bot.Mensageiro.Chamadas[2].Em - _bot.Mensageiro.Chamadas[0].Em;
        pausa.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1500)).And.BeLessThanOrEqualTo(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task DeveAindaEnviarELogarAFalhaQuandoADigitacaoFalha()
    {
        _bot.Mensageiro.DigitacaoFalha = true;
        EnviadorDeRespostas enviador = _bot.ConstruirEnviadorDeRespostas();

        ResultadoDoEnvio? resultado = await EnviarTexto(enviador);

        resultado.Should().BeOfType<Enviado>();
        _bot.Mensageiro.Mensagens.Should().ContainSingle();
        _bot.Logs.Entradas.Should().ContainSingle(log => log.Nivel == LogLevel.Warning && log.Mensagem.Contains("Não foi possível mostrar digitando"));
    }

    [Fact]
    public async Task NaoDeveEnviarNadaELogarUmErroQuandoODisjuntorEstaCheio()
    {
        EnviadorDeRespostas enviador = _bot.ConstruirEnviadorDeRespostas();
        PreencherODisjuntor();

        ResultadoDoEnvio? resultado = await EnviarTexto(enviador);

        resultado.Should().BeNull();
        _bot.Mensageiro.Chamadas.Should().BeEmpty();
        _bot.Logs.Entradas.Should().ContainSingle(log => log.Nivel == LogLevel.Error && log.Mensagem.StartsWith("Disjuntor cheio"));
    }

    // A mensagem mais antiga sai da janela e o bot volta a enviar
    [Fact]
    public async Task DeveEnviarDeNovoQuandoAMensagemMaisAntigaSaiDaJanelaDoDisjuntor()
    {
        EnviadorDeRespostas enviador = _bot.ConstruirEnviadorDeRespostas();
        PreencherODisjuntor();
        _bot.Relogio.Advance(TimeSpan.FromHours(1));

        ResultadoDoEnvio? resultado = await EnviarTexto(enviador);

        resultado.Should().BeOfType<Enviado>();
    }

    // A recusa que saiu, ou que pode ter saído, abre a janela de quem pediu
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeveIniciarAJanelaDoRemetenteQuandoARecusaSaiuOuPodeTerSaido(bool certo)
    {
        ResultadoDoEnvio resposta = certo ? new Enviado() : new Incerto { Motivo = "WAHA não respondeu a tempo" };
        _bot.Mensageiro.ResponderTextos(resposta);
        EnviadorDeRespostas enviador = _bot.ConstruirEnviadorDeRespostas();
        _bot.Controle.Verificar(ConstrutorDeCasoDeUso.IdDoGrupo, ConstrutorDeCasoDeUso.IdDoIntegrante, TipoDeComando.Sortear).Should().BeOfType<Recusado>();

        await EnviarTexto(enviador);

        _bot.Controle.Verificar(ConstrutorDeCasoDeUso.IdDoGrupo, ConstrutorDeCasoDeUso.IdDoIntegrante, TipoDeComando.Sortear).Should().BeOfType<Ignorado>();
    }

    // A recusa que o WAHA recusou não gasta a janela
    [Fact]
    public async Task DeveManterAJanelaDoRemetenteFechadaQuandoOWahaRejeitouAResposta()
    {
        _bot.Mensageiro.ResponderTextos(new Rejeitado { Motivo = "WAHA respondeu 422" });
        EnviadorDeRespostas enviador = _bot.ConstruirEnviadorDeRespostas();

        ResultadoDoEnvio? resultado = await EnviarTexto(enviador);

        resultado.Should().BeOfType<Rejeitado>();
        _bot.Controle.Verificar(ConstrutorDeCasoDeUso.IdDoGrupo, ConstrutorDeCasoDeUso.IdDoIntegrante, TipoDeComando.Sortear).Should().BeOfType<Recusado>();
        _bot.Logs.Entradas.Should().ContainSingle(log => log.Nivel == LogLevel.Error && log.Mensagem.Contains("WAHA respondeu 422"));
    }

    // Imagem e legenda numa mensagem só, citando o comando
    [Fact]
    public async Task DeveEnviarAImagemComALegendaEmUmaSoMensagemQuandoEnviaUmaImagem()
    {
        EnviadorDeRespostas enviador = _bot.ConstruirEnviadorDeRespostas();
        byte[] png = [0x89, 0x50, 0x4E, 0x47];

        await _bot.ExecutarAsync(() => enviador.EnviarImagemAsync(_integrantePedeOSorteio, png, "legenda", TestContext.Current.CancellationToken));

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Imagem);
        mensagem.Png.Should().Equal(png);
        mensagem.Texto.Should().Be("legenda");
        mensagem.IdDaMensagemRespondida.Should().Be(IdDoComando);
    }

    [Fact]
    public async Task DeveLogarSoOIdMascaradoDaMensagemQuandoResponde()
    {
        EnviadorDeRespostas enviador = _bot.ConstruirEnviadorDeRespostas();

        await EnviarTexto(enviador);

        _bot.Logs.Entradas.Should().ContainSingle().Which.Mensagem.Should().Contain("***0A01").And.NotContain("35600000404").And.NotContain("oi");
    }

    private async Task<ResultadoDoEnvio?> EnviarTexto(EnviadorDeRespostas enviador)
    {
        ResultadoDoEnvio? resultado = null;

        await _bot.ExecutarAsync(async () => resultado = await enviador.EnviarTextoAsync(_integrantePedeOSorteio, "oi", TestContext.Current.CancellationToken));

        return resultado;
    }

    private void PreencherODisjuntor()
    {
        for (int mensagem = 0; mensagem < 30; mensagem++)
        {
            _bot.Disjuntor.TentarReservar().Should().BeTrue();
        }
    }
}
