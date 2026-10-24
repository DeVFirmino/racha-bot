using PeladaBot.CasosDeUso;
using PeladaBot.CasosDeUso.Pagamentos.CobrarPendentes;
using PeladaBot.Comandos;
using PeladaBot.EscolhaDaLista;
using PeladaBot.Respostas;
using PeladaBot.Tests.UtilitariosDeTeste;

namespace PeladaBot.Tests.CasosDeUso.Pagamentos;

public sealed class CasoDeUsoCobrarPendentesTestes : IDisposable
{
    private const string IdDoComando = "false_120363000000001234@g.us_3EB0C0B2A2000000AA01_35600000101@c.us";
    private const string MensagemDeCobranca = "🚨 *Quem ainda não pagou, favor acertar!*";
    private const string IdDoOutroIntegrante = "35600000505@c.us";

    private readonly ConstrutorDeCasoDeUso _bot = new();
    private string _mensagem = MensagemDeCobranca;

    public void Dispose() => _bot.Dispose();

    // A cobrança cita o comando, só texto, com a mensagem e os nomes
    [Fact]
    public async Task DeveCobrarQuemNaoPagouCitandoOComandoQuandoOPresidenteCitaALista()
    {
        string lista = new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresPagos("Nico Braga").ComJogadoresNaoPagos("Paco", "Luan Ferraz").Build();

        await Cobrar(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, Citar(lista));

        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);
        mensagem.IdDaMensagemRespondida.Should().Be(IdDoComando);
        mensagem.Texto.Should().Be($"{MensagemDeCobranca}\n\n💸 *Falta pagar (2):*\n👀 Paco\n👀 Luan Ferraz");
    }

    // Sem citar, a lista mais nova de qualquer um, como o !pendentes
    [Fact]
    public async Task DeveUsarAListaMaisNovaDeQualquerUmQuandoOComandoNaoCitaNada()
    {
        _bot.Historico.Com(
            new ConstrutorDeMensagemDoGrupo()
                .De(ConstrutorDeCasoDeUso.IdDoIntegrante, "Integrante Beltrano")
                .EnviadaEm(new DateTimeOffset(2026, 10, 14, 10, 15, 0, TimeSpan.Zero))
                .ComTexto(new ConstrutorDeTextoDaListaDoGrupo().ComJogadoresPagos("Nico Braga").ComJogadoresNaoPagos("Novato").Build())
                .Build());

        await Cobrar(ConstrutorDeCasoDeUso.IdDoTelefoneDoVice, citada: null);

        OUnicoTexto().Should().Be(
            $"{MensagemDeCobranca}\n\n💸 *Falta pagar (1):*\n👀 Novato\n📋 Lista enviada por Integrante Beltrano em 14/10 às 12:15");
    }

    // Uma cobrança que lista os nomes, de ponta a ponta, com a quantidade e o plural certos
    [Fact]
    public async Task DeveCobrarComOsNomesNaMensagemQuandoAMensagemDaConfiguracaoTemAsMarcas()
    {
        string lista = new ConstrutorDeTextoDaListaDoGrupo().ComJogadoresPagos("Nico Braga").ComJogadoresNaoPagos("Paco").Build();
        _mensagem = "🌍 *{quantidade} atleta{s} aguardando liberação:*\n💸 {nome}";

        await Cobrar(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, Citar(lista));

        OUnicoTexto().Should().Be("🌍 *1 atleta aguardando liberação:*\n💸 Paco");
    }

    // Com todo mundo pago, só o agradecimento
    [Fact]
    public async Task DeveAgradecerSemCobrarQuandoTodoMundoDaListaPagou()
    {
        string lista = new ConstrutorDeTextoDaListaDoGrupo().ComGoleiros(2).ComJogadoresPagos("Nico Braga", "Paco").Build();

        await Cobrar(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, Citar(lista));

        OUnicoTexto().Should().Be("✅ Todo mundo pagou, valeu craques!");
    }

    // Um integrante cobra; outro, logo depois, recebe a resenha de espera; insistindo, nada
    [Fact]
    public async Task DeveCobrarEDepoisPedirCalmaUmaVezQuandoIntegrantesCobramDentroDeTrintaMinutos()
    {
        string lista = new ConstrutorDeTextoDaListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();

        await Cobrar(ConstrutorDeCasoDeUso.IdDoIntegrante, Citar(lista));
        _bot.Relogio.Advance(TimeSpan.FromMinutes(10));
        await Cobrar(IdDoOutroIntegrante, Citar(lista));
        await Cobrar(IdDoOutroIntegrante, Citar(lista));

        IReadOnlyList<ChamadaDoMensageiro> mensagens = _bot.Mensageiro.Mensagens;
        mensagens.Should().HaveCount(2);
        mensagens[0].Texto.Should().Be($"{MensagemDeCobranca}\n\n💸 *Falta pagar (1):*\n👀 Paco");
        mensagens[1].Texto.Should().BeOneOf(RespostaDeCobrancaRecente.Resenhas.Select(resenha => resenha.Replace("{tempo}", "20 minutos", StringComparison.Ordinal)));
    }

    // O presidente cobra de novo logo depois de um integrante
    [Fact]
    public async Task DeveCobrarDeNovoQuandoOPresidentePedeLogoDepoisDeUmIntegrante()
    {
        string lista = new ConstrutorDeTextoDaListaDoGrupo().ComJogadoresNaoPagos("Paco").Build();

        await Cobrar(ConstrutorDeCasoDeUso.IdDoIntegrante, Citar(lista));
        await Cobrar(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, Citar(lista));

        _bot.Mensageiro.Mensagens.Should().HaveCount(2).And.OnlyContain(mensagem => mensagem.Texto!.StartsWith(MensagemDeCobranca, StringComparison.Ordinal));
    }

    // !pendentes é o mesmo comando
    [Fact]
    public async Task DeveCobrarQuandoOIntegranteUsaOPendentes()
    {
        ComandoLido comando = LeitorDeComando.Ler("!pendentes")!;

        comando.Tipo.Should().Be(TipoDeComando.Cobrar);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task DevePedirAListaComCobrarQuandoAMensagemCitadaNaoEhUmaLista()
    {
        await Cobrar(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, Citar("bora que amanhã tem jogo"));

        OUnicoTexto().Should().Be(RespostasDeErro.CitarAListaParaCobrar);
    }

    [Fact]
    public async Task DeveDizerQueAListaCitadaEhIlegivelQuandoSeuTextoNaoPodeSerLido()
    {
        await Cobrar(ConstrutorDeCasoDeUso.IdDoTelefoneDoPresidente, new MensagemCitada { Id = "3EB0FFFFFFFFFFFF", Texto = null });

        OUnicoTexto().Should().Be(RespostasDeErro.ListaCitadaIlegivel);
    }

    // FluentValidation (pedido malformado nunca chega a tocar o grupo)
    [Fact]
    public async Task DeveLancarENaoEnviarNadaQuandoOPedidoEhInvalido()
    {
        CasoDeUsoCobrarPendentes casoDeUso = _bot.ConstruirCobrarPendentes();
        PedidoCobrarPendentes pedidoInvalido = new() { IdDoGrupo = string.Empty, IdDoRemetente = string.Empty, IdDaMensagem = string.Empty, Citada = null };

        Func<Task> executar = () => casoDeUso.Executar(pedidoInvalido, TestContext.Current.CancellationToken);

        await executar.Should().ThrowAsync<ExcecaoDeValidacao>();
        _bot.Mensageiro.Chamadas.Should().BeEmpty();
    }

    private async Task Cobrar(string idDoRemetente, MensagemCitada? citada)
    {
        CasoDeUsoCobrarPendentes casoDeUso = _bot
            .ComOpcoes(new ConstrutorDeOpcoesDoBot().ComFrasesDoRodape(ConstrutorDeCasoDeUso.FraseDoRodape).ComMensagensDeCobranca(_mensagem).Build())
            .ConstruirCobrarPendentes();
        PedidoCobrarPendentes pedido = new()
        {
            IdDoGrupo = ConstrutorDeCasoDeUso.IdDoGrupo,
            IdDoRemetente = idDoRemetente,
            IdDaMensagem = IdDoComando,
            Citada = citada,
        };

        await _bot.ExecutarAsync(() => casoDeUso.Executar(pedido, TestContext.Current.CancellationToken));
    }

    private string? OUnicoTexto()
    {
        ChamadaDoMensageiro mensagem = _bot.Mensageiro.Mensagens.Should().ContainSingle().Which;
        mensagem.Tipo.Should().Be(TipoDeChamadaDoMensageiro.Texto);

        return mensagem.Texto;
    }

    private static MensagemCitada Citar(string texto) => new() { Id = "3EB05B1F2A9C4D7E0B02", Texto = texto };
}
