namespace PeladaBot.Respostas;

/// <summary>
/// A resposta ao !ajuda: como participar da pelada, as regras do grupo e os comandos, para qualquer integrante. Quem
/// é da presidência recebe também o bloco com o que só ela usa.
/// </summary>
public static class TextoDeAjuda
{
    private const string Separador = "━━━━━━━━━━━━━━━━";
    private const string TituloSemNome = "PELADA";

    /// <summary>
    /// Escreve o guia. Sem <paramref name="linkDePagamento"/>, a linha do link não aparece; sem
    /// <paramref name="valorDaPelada"/>, o pagamento sai sem valor.
    /// </summary>
    public static string Compor(
        string tituloDoGrupo, string valorDaPelada, string linkDePagamento, TimeSpan janelaDeCobranca, bool paraAPresidencia)
    {
        ArgumentNullException.ThrowIfNull(tituloDoGrupo);
        ArgumentNullException.ThrowIfNull(valorDaPelada);
        ArgumentNullException.ThrowIfNull(linkDePagamento);

        string titulo = string.IsNullOrWhiteSpace(tituloDoGrupo) ? TituloSemNome : tituloDoGrupo.Trim();
        bool temLink = string.IsNullOrWhiteSpace(linkDePagamento) is false;
        string valor = string.IsNullOrWhiteSpace(valorDaPelada) ? string.Empty : $" de *{valorDaPelada.Trim()}*";
        List<string> linhas =
        [
            $"🤖⚽ *{titulo} | !ajuda*",
            string.Empty,
            "📋 *COMO PARTICIPAR DA PELADA?*",
            string.Empty,
            .. Secao("📝 *1. PAGAMENTO E INSCRIÇÃO*"),
            $"💶 Faça o pagamento{valor}{(temLink ? ":" : ".")}",
            string.Empty,
        ];

        if (temLink)
        {
            linhas.AddRange([$"📲 {linkDePagamento.Trim()}", string.Empty]);
        }

        linhas.AddRange(
        [
            "✅ Depois, adicione seu nome na próxima posição disponível da lista.",
            string.Empty,
            "⚠️ Cada jogador deve incluir apenas o próprio nome, para manter a organização.",
            string.Empty,
            "🧤 Goleiros fixos não pagam.",
            string.Empty,
            .. Secao("💚 *2. CONFIRMAÇÃO DA VAGA*"),
            "✅ Pagamento confirmado = presença garantida. Pagou? Coloque o ✅ ao lado do seu nome na lista.",
            string.Empty,
            "⏳ Sem pagamento, a vaga ainda não está confirmada.",
            string.Empty,
            "😂 A primeira assistência da partida é o pagamento, e essa não pode sair errada!",
            string.Empty,
            .. Secao("👀 *3. LISTA DE ESPERA*"),
            "⚽ Se a lista principal estiver completa, coloque seu nome na lista de espera.",
            string.Empty,
            "📋 Em caso de desistência, os jogadores serão chamados conforme a ordem da fila.",
            string.Empty,
            "🤝 Assim, todo mundo tem uma oportunidade justa de participar.",
            string.Empty,
            .. Secao("🚨 *4. REGRAS IMPORTANTES*"),
            "⏰ Chegue no horário para o aquecimento e o início da partida. A pontualidade ajuda a garantir um jogo mais organizado e divertido para todos.",
            string.Empty,
            "❌ Após o pagamento, desistências ou faltas não dão direito a reembolso, conforme as regras do grupo.",
            string.Empty,
            "🟨 Atrasos superiores a 15 minutos podem resultar em revezamento com um substituto.",
            string.Empty,
            "⚠️ Faltas e atrasos podem gerar suspensão de até 2 jogos, conforme as regras do grupo.",
            string.Empty,
            .. Secao("🤖 *5. COMANDOS DO BOT*"),
            "🎰 *!sortear* — Sorteia os times quando todo mundo da lista está com ✅. Só presidente e admins.",
            string.Empty,
            "🎰 *!times* — O mesmo que !sortear.",
            string.Empty,
            $"💶 *!pendentes* — Mostra quem ainda não pagou. Presidente e admins a qualquer hora; os outros, uma vez a cada {Duracao(janelaDeCobranca)}.",
            string.Empty,
            "💶 *!cobrar* — O mesmo que !pendentes.",
            string.Empty,
            "❓ *!ajuda* — Exibe estas instruções.",
            string.Empty,
        ]);

        if (paraAPresidencia)
        {
            linhas.AddRange(
            [
                .. Secao("👑 *SÓ PARA PRESIDENTE E ADMINS*"),
                "📋 Segure a mensagem da lista, toque em Responder e envie *!sortear*: o bot usa exatamente aquela lista. Sem responder, usa a última lista do presidente ou de um admin.",
                string.Empty,
                "🆘 *!sortear pagos* — Sorteia só com quem tem ✅; quem não pagou fica fora. Precisa de pelo menos 4 pagos. *!times pagos* faz o mesmo.",
                string.Empty,
                "✏️ Editar a mensagem não atualiza o bot: reenvie a lista completa corrigida e responda à nova. Pode sortear de novo; vale o último sorteio.",
                string.Empty,
                "🔐 Só presidente e admins cadastrados no bot sorteiam; ser admin do grupo no WhatsApp, sem cadastro no bot, não basta.",
                string.Empty,
            ]);
        }

        linhas.AddRange([Separador, string.Empty, "🤝 *Respeitem a lista, os horários e os companheiros. Bom jogo, craques!*"]);

        return LinhasDeResposta.Juntar(linhas);
    }

    private static IReadOnlyList<string> Secao(string titulo) => [Separador, titulo, Separador];

    /// <summary>"30 minutos", "1 minuto", "2 horas", "1 hora e 30 minutos": a janela de cobrança configurada.</summary>
    private static string Duracao(TimeSpan janela)
    {
        int horas = (int)janela.TotalHours;
        int minutos = Math.Max(janela.Minutes, horas == 0 ? 1 : 0);
        string textoDasHoras = horas == 1 ? "1 hora" : $"{horas} horas";
        string textoDosMinutos = minutos == 1 ? "1 minuto" : $"{minutos} minutos";

        return (horas, minutos) switch
        {
            (0, _) => textoDosMinutos,
            (_, 0) => textoDasHoras,
            _ => $"{textoDasHoras} e {textoDosMinutos}",
        };
    }
}
