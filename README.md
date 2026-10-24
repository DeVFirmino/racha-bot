# racha-bot

**O sorteio dos times da pelada, direto no grupo do WhatsApp.**

<p align="center">
  <a href="docs/img/racha-bot-fluxo.mp4"><img src="docs/img/racha-bot-fluxo.gif" alt="Vídeo de 15 segundos: a lista da pelada chega no grupo, os jogadores marcam o ✅ de pago, o presidente responde com !sortear e o bot devolve o card com os dois times" width="860"></a>
</p>

Você conhece a cena. A lista passa de mão em mão, alguém reenvia uma cópia velha e um nome some, ninguém lembra quem pagou, e na hora do jogo o sorteio sai no papelzinho.

O racha-bot entra no grupo e resolve isso. Ele lê a lista do jeito que o grupo já escreve, confere quem marcou ✅ e devolve os times prontos numa imagem. Ninguém precisa instalar app nem aprender nada novo.

É grátis, de código aberto e roda num servidor seu.

## Como funciona

1. **O grupo manda a lista como sempre.** Com os goleiros fixos, os jogadores e a lista de espera.
2. **Quem paga marca o ✅** ao lado do próprio nome.
3. **O presidente responde a lista com `!sortear`.** Em segundos, o bot manda o card dos times e a legenda com as escalações.

<p align="center"><img src="docs/img/card-exemplo.png" alt="Card do sorteio: Pelada de Quarta, time laranja contra time verde, cada jogador com camisa e nome, goleiros de preto" width="420"></p>

## O que ele faz por você

- **Entende a lista bagunçada:** numeração, ✅, 🧤, emojis e até as linhas de Pix, valor e horário no meio.
- **Monta times justos:** times de 6 (1 goleiro + 5 na linha) quando dá. Com menos gente, dois times com o mesmo número de jogadores, ou com um de diferença. O goleiro fixo fica no gol, e o time sem goleiro ganha um de rodízio.
- **Só sorteia com todo mundo pago.** Se faltar ✅, ele diz quem falta. Na emergência, `!sortear pagos` sorteia só quem já pagou.
- **Cobra por você:** `!pendentes` mostra no grupo quem ainda não pagou, com o valor e o link de pagamento.
- **Avisa quando a lista está estranha:** nome repetido, nome em duas seções, cópia antiga de outra pessoa.
- **Não vira spam:** só responde a comandos, só no seu grupo e com limite de mensagens.

## Comandos

| Comando | Quem usa | O que faz |
|---|---|---|
| `!sortear` (ou `!times`) | presidência | Sorteia os times. Respondendo uma lista, usa aquela; sem responder, usa a mais recente da presidência. |
| `!sortear pagos` | presidência | Sorteia só quem tem ✅ (mínimo de 4 jogadores de linha pagos). |
| `!pendentes` (ou `!cobrar`) | todo mundo | Mostra quem ainda não pagou. Quem não é da presidência pode usar de tempos em tempos. |
| `!ajuda` | todo mundo | Explica como participar e lista os comandos. |

A lista precisa dos títulos `Goleiros fixos`, `Lista de jogadores` e `Lista de espera`:

```text
⚽ PELADA DE QUARTA ⚽
Quarta, 20:30 · Quadra do Parque

Goleiros fixos:
1. Marcelo Pereira 🧤
2. Fernando Dias 🧤

Lista de jogadores:
1. Rafael Souza ✅
2. Thiago Lima ✅
3. Bruno Carvalho ✅
4. Lucas Martins ✅
...

Lista de espera:
1. Caio Mendes
```

A presidência é quem você cadastra no bot (até 2 pessoas). Ser admin do grupo no WhatsApp não dá acesso ao sorteio.

Os nomes e lugares dos exemplos são fictícios.

## Antes de instalar

- **Um número de WhatsApp só para o bot.** Use um chip dedicado, nunca o seu número pessoal. O bot entra no grupo como um aparelho conectado desse número, pelo [WAHA](https://waha.devlike.pro/), um gateway **não oficial**. O WhatsApp não garante que o número não será bloqueado. O bot foi feito para reduzir esse risco, mas ele existe.
- **Um computador ou servidor com Docker** (e Docker Compose).
- **Uma chave gratuita da Six Labors**, a biblioteca que desenha o card. Projetos open source não pagam: peça em [licensing.sixlabors.com](https://licensing.sixlabors.com) para o assembly `PeladaBot`.

## Instalação

1. **Baixe o projeto e crie o `.env`:**

   ```sh
   git clone https://github.com/DeVFirmino/racha-bot.git
   cd racha-bot/deploy
   cp .env.example .env
   ```

   No `.env`, gere `WAHA_API_KEY` e `WAHA_WEBHOOK_HMAC_KEY` com `openssl rand -hex 32`, `WAHA_DASHBOARD_PASSWORD` com `openssl rand -hex 16`, e coloque a sua chave em `SIXLABORS_LICENSE_KEY`. Em `WAHA_ARCH_SUFFIX`, deixe `-arm` num Mac com Apple Silicon ou outra máquina ARM, e vazio numa máquina Intel ou AMD.

2. **Suba o WAHA e conecte o número:**

   ```sh
   docker compose up -d waha
   ```

   Abra http://localhost:3000/dashboard, entre com o login do `.env`, inicie a sessão `default` e escaneie o QR code no celular do chip (WhatsApp → Aparelhos conectados).

3. **Coloque o número do bot no grupo** e pegue dois ids no Swagger do WAHA (http://localhost:3000, com a `WAHA_API_KEY`): o do grupo, em `GET /api/default/groups` (termina em `@g.us`), e o seu, entre os participantes do grupo em `GET /api/default/groups/{id}/participants` (termina em `@c.us` ou `@lid`). Coloque em `BOT_GROUP_ID` e `BOT_PRESIDENT_ID` no `.env`.

4. **Ligue o bot:**

   ```sh
   docker compose up --build -d
   ```

   Mande `!ajuda` no grupo. Se ele responder, está pronto.

Para deixar ligado de vez, rode os mesmos dois containers em qualquer servidor com Docker. Guarde o volume do WAHA (`/app/.sessions`): é ele que mantém o número conectado sem pedir o QR code de novo.

## Personalizar

Tudo fica no [`appsettings.json`](src/PeladaBot/appsettings.json), e cada item também pode vir por variável de ambiente.

| O quê | Onde |
|---|---|
| Fuso dos horários | `Bot:Fuso` (ex.: `America/Sao_Paulo`) |
| A presidência (até 2 pessoas, com um ou mais ids cada) | `Bot:Grupos:0:Presidencia` |
| Valor e link de pagamento | `Bot:Grupos:0:ValorDaPelada` e `LinkDePagamento` |
| Nome e escudo do grupo no card | `Bot:Grupos:0:TituloDoCard` e `CaminhoDoEscudo` |
| Frases do rodapé, temas da legenda e recados de cobrança | `Bot:Grupos:0:FrasesDoRodape`, `ModelosDaLegenda` e `MensagensDeCobranca` |

## Para desenvolvedores

O bot é uma API em .NET 10 que recebe os webhooks do WAHA, coloca os comandos numa fila e responde pelo próprio WAHA. Não tem banco de dados.

```sh
dotnet build
dotnet test --solution PeladaBot.slnx
```

Para ver o bot responder sem celular e sem WhatsApp, use o simulador do grupo. Ele sobe o bot com um WAHA falso e abre uma tela parecida com a do WhatsApp em http://127.0.0.1:8095/:

```sh
python3 scripts/group-simulator.py --web
```

Lá você manda a lista, marca quem pagou, troca entre presidente e integrante e usa os comandos. O card aparece no chat.

Quer ajudar? Veja o [guia de contribuição](.github/CONTRIBUTING.md).

## Licença

[MIT](LICENSE). As fontes do card (Barlow Condensed e Noto Emoji) estão sob a [SIL Open Font License](src/PeladaBot/Card/Fonts/BarlowCondensed/OFL.txt).
