using PeladaBot.Respostas;

namespace PeladaBot.Tests.Respostas;

public sealed class TextoDeAjudaTestes
{
    // O guia do grupo, com o valor e o link configurados
    [Fact]
    public void DeveSerOGuiaDoGrupoQuandoUmIntegrantePedeAjuda()
    {
        string guia = TextoDeAjuda.Compor("PELADA", "valor-de-exemplo", "pagamento.exemplo/grupo", TimeSpan.FromMinutes(30), paraAPresidencia: false);

        guia.Should().Be("""
            🤖⚽ *PELADA | !ajuda*

            📋 *COMO PARTICIPAR DA PELADA?*

            ━━━━━━━━━━━━━━━━
            📝 *1. PAGAMENTO E INSCRIÇÃO*
            ━━━━━━━━━━━━━━━━
            💶 Faça o pagamento de *valor-de-exemplo*:

            📲 pagamento.exemplo/grupo

            ✅ Depois, adicione seu nome na próxima posição disponível da lista.

            ⚠️ Cada jogador deve incluir apenas o próprio nome, para manter a organização.

            🧤 Goleiros fixos não pagam.

            ━━━━━━━━━━━━━━━━
            💚 *2. CONFIRMAÇÃO DA VAGA*
            ━━━━━━━━━━━━━━━━
            ✅ Pagamento confirmado = presença garantida. Pagou? Coloque o ✅ ao lado do seu nome na lista.

            ⏳ Sem pagamento, a vaga ainda não está confirmada.

            😂 A primeira assistência da partida é o pagamento, e essa não pode sair errada!

            ━━━━━━━━━━━━━━━━
            👀 *3. LISTA DE ESPERA*
            ━━━━━━━━━━━━━━━━
            ⚽ Se a lista principal estiver completa, coloque seu nome na lista de espera.

            📋 Em caso de desistência, os jogadores serão chamados conforme a ordem da fila.

            🤝 Assim, todo mundo tem uma oportunidade justa de participar.

            ━━━━━━━━━━━━━━━━
            🚨 *4. REGRAS IMPORTANTES*
            ━━━━━━━━━━━━━━━━
            ⏰ Chegue no horário para o aquecimento e o início da partida. A pontualidade ajuda a garantir um jogo mais organizado e divertido para todos.

            ❌ Após o pagamento, desistências ou faltas não dão direito a reembolso, conforme as regras do grupo.

            🟨 Atrasos superiores a 15 minutos podem resultar em revezamento com um substituto.

            ⚠️ Faltas e atrasos podem gerar suspensão de até 2 jogos, conforme as regras do grupo.

            ━━━━━━━━━━━━━━━━
            🤖 *5. COMANDOS DO BOT*
            ━━━━━━━━━━━━━━━━
            🎰 *!sortear* — Sorteia os times quando todo mundo da lista está com ✅. Só presidente e admins.

            🎰 *!times* — O mesmo que !sortear.

            💶 *!pendentes* — Mostra quem ainda não pagou. Presidente e admins a qualquer hora; os outros, uma vez a cada 30 minutos.

            💶 *!cobrar* — O mesmo que !pendentes.

            ❓ *!ajuda* — Exibe estas instruções.

            ━━━━━━━━━━━━━━━━

            🤝 *Respeitem a lista, os horários e os companheiros. Bom jogo, craques!*
            """);
    }

    // A presidência recebe também o bloco do que só ela usa, antes do fecho
    [Fact]
    public void DeveIncluirOBlocoDaPresidenciaQuandoAPresidenciaPedeAjuda()
    {
        string guia = TextoDeAjuda.Compor("PELADA", "valor-de-exemplo", "pagamento.exemplo/grupo", TimeSpan.FromMinutes(30), paraAPresidencia: true);

        guia.Should().Contain("""
            ❓ *!ajuda* — Exibe estas instruções.

            ━━━━━━━━━━━━━━━━
            👑 *SÓ PARA PRESIDENTE E ADMINS*
            ━━━━━━━━━━━━━━━━
            """).And.Contain("🆘 *!sortear pagos*").And.Contain("!times pagos").And.EndWith("🤝 *Respeitem a lista, os horários e os companheiros. Bom jogo, craques!*");
    }

    // Sem link configurado, a linha do link some; sem valor, o pagamento sai sem valor; sem título, PELADA
    [Fact]
    public void DeveTirarOLinkEOValorQuandoAConfiguracaoNaoTem()
    {
        string guia = TextoDeAjuda.Compor(string.Empty, string.Empty, string.Empty, TimeSpan.FromMinutes(30), paraAPresidencia: false);

        guia.Should().StartWith("🤖⚽ *PELADA | !ajuda*")
            .And.Contain("💶 Faça o pagamento.\n\n✅ Depois")
            .And.NotContain("📲");
    }

    // A regra do !pendentes no guia segue a janela configurada
    [Theory]
    [InlineData(30, "uma vez a cada 30 minutos.")]
    [InlineData(1, "uma vez a cada 1 minuto.")]
    [InlineData(60, "uma vez a cada 1 hora.")]
    [InlineData(90, "uma vez a cada 1 hora e 30 minutos.")]
    public void DeveMostrarAJanelaDeCobrancaConfiguradaQuandoOGuiaEhComposto(int minutos, string esperado)
    {
        string guia = TextoDeAjuda.Compor(string.Empty, string.Empty, string.Empty, TimeSpan.FromMinutes(minutos), paraAPresidencia: false);

        guia.Should().Contain($"os outros, {esperado}");
    }
}
