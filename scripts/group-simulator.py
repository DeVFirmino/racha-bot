#!/usr/bin/env python3
"""Group simulator: try the bot on this machine, with no phone and no WhatsApp.

Starts a fake WAHA and the bot (dotnet run) pointed at it. You type in the terminal as the
presidente or as a member, the bot answers here, and the draw card opens on screen. Webhooks
are signed like WAHA signs them (HMAC-SHA512), so the bot runs exactly as in production.
Only the Python standard library is used.

    python3 scripts/group-simulator.py          # no terminal
    python3 scripts/group-simulator.py --web    # chat page in the browser

See the README ("Para desenvolvedores").
"""

import argparse
import base64
import hashlib
import hmac
import http.server
import json
import os
import pathlib
import random
import secrets
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parent.parent
CORPUS = ROOT / "tests" / "PeladaBot.Tests" / "Corpus"
OUTPUT = ROOT / ".local" / "simulator"
# Real lists and the group's crest live here, outside Git: .local/ is ignored.
LOCAL_LISTS = ROOT / ".local" / "listas"
LOCAL_CRESTS = [ROOT / ".local" / name for name in ("escudo.png", "escudo.jpeg", "escudo.jpg")]
# A list with names already paid: the corpus versions of 14/10 were taken before anyone got a ✅.
PAID_LIST = ROOT / "tests" / "PeladaBot.Tests" / "Fixtures" / "waha-chat-message.json"

# The same fake ids the tests use: nothing here is a real person or group.
GROUP_ID = "120363000000001234@g.us"
PEOPLE = {
    "presidente": ("35600000101@c.us", "Presidente Fulano"),
    "admin": ("35600000103@c.us", "Admin Ciclano"),
    "integrante": ("35600000102@c.us", "Integrante Beltrano"),
    "integrante2": ("35600000104@c.us", "Integrante Sicrano"),
}

# Fake names for the generated lists; none is a real member of the group.
FAKE_NAMES = [
    "Nico", "Duda", "Ruy", "Iago", "Bento", "Luan", "Enzo", "Gael", "Heitor", "Otto", "Milo", "Cael",
    "Luca", "Renzo", "Nilo", "Hélio", "Ivo", "Rômulo", "Wilson", "Tales", "Kaio", "Davi", "Hugo",
    "Samuel", "Tomás", "Vitor", "Nicolas", "Paulo", "Ricardo", "Eduardo", "Léo", "Mauro", "Ciro",
]
FAKE_GOALKEEPERS = ["Téo", "Cauã", "Dico", "Nuno", "Guto"]
LONG_NAMES = [
    "Maximiliano Albuquerque", "Bartolomeu Vasconcelos", "Cristiano Bittencourt", "Leopoldino Figueiredo",
    "Washington Nascimento", "Valdemar Cavalcanti", "Florisvaldo Medeiros", "Anacleto Montenegro",
    "Deoclécio Brandão", "Epaminondas Siqueira", "Hermenegildo Paranhos", "Juscelino Damasceno",
]

# Scenario id -> (label, paid, unpaid, goalkeepers, waiting, special). Paid players go first in the list.
# De 10 a 15 de linha com 2 ou mais goleiros fixos (uma quadra), os fixos ficam no gol e todo time é de
# 5 na linha, sem rodízio; acima de 15 (duas quadras) ou com menos de 2 fixos, time = 1 goleiro + 5 de linha, com
# rodízio. O que sobra fica num time incompleto; de 4 a 9, dois times. Quem está na espera com ✅ é sorteado.
SCENARIOS = {
    "aleatoria": ("🎲 Random list: players, paid, goalkeepers and the waiting list change on every send", "aleatoria"),
    "paga": ("13 paid, 2 goalkeepers: 2 full teams and 1 short team of 3 (need 2), keepers in goal", None),
    "3pagos": ("3 paid: not enough to draw", dict(pagos=3, nao_pagos=0, goleiros=2, espera=0)),
    "5pagos": (
        "5 paid, 1 goalkeeper: the bigger side keeps the fixed keeper (3 outfield); the smaller side draws a rotating keeper from its 2",
        dict(pagos=5, nao_pagos=0, goleiros=1, espera=0),
    ),
    "9pagos": ("9 paid, 2 goalkeepers: 2 full teams (5 and 4 outfield)", dict(pagos=9, nao_pagos=0, goleiros=2, espera=0)),
    "13linha": (
        "13 paid, 2 goalkeepers: 2 full teams and 1 short team of 3 (need 2), keepers in goal",
        dict(pagos=13, nao_pagos=0, goleiros=2, espera=0),
    ),
    "15quadra": (
        "15 paid, 2 goalkeepers (one full pitch): 3 teams of 5 outfield, the 3rd plays with the keepers in goal",
        dict(pagos=15, nao_pagos=0, goleiros=2, espera=3),
    ),
    "esperapaga": (
        "12 paid + 2 on the waiting list with ✅ (and 2 without): the 2 who paid join the draw",
        dict(pagos=12, nao_pagos=0, goleiros=2, espera=2, espera_paga=2),
    ),
    "goleiros0": (
        "12 paid, 0 goalkeepers: 2 teams of 6, each with a rotating keeper",
        dict(pagos=12, nao_pagos=0, goleiros=0, espera=0),
    ),
    "15pagos": ("15 paid, 3 goalkeepers: 3 full teams, each with a fixed keeper", dict(pagos=15, nao_pagos=0, goleiros=3, espera=1)),
    "18pagos": (
        "18 paid, 2 goalkeepers (two pitches): 3 full teams (1 with a rotating keeper) and 1 short team of 2",
        dict(pagos=18, nao_pagos=0, goleiros=2, espera=2),
    ),
    "20pagos": ("20 paid, 4 goalkeepers: 4 full teams", dict(pagos=20, nao_pagos=0, goleiros=4, espera=0)),
    "24pagos": (
        "24 paid, 5 goalkeepers: 4 full teams and 1 short team with the 5th keeper and 4 outfield",
        dict(pagos=24, nao_pagos=0, goleiros=5, espera=2),
    ),
    "26pessoas": (
        "26 paid, 3 goalkeepers: 5 teams, in 3 images (the 5th with no bib)",
        dict(pagos=26, nao_pagos=0, goleiros=3, espera=3),
    ),
    "25pagos": ("25 paid, 3 goalkeepers: 5 teams, in 3 images (the 5th with no bib)", dict(pagos=25, nao_pagos=0, goleiros=3, espera=0)),
    "nomeslongos": ("12 paid, with long names", dict(pagos=12, nao_pagos=0, goleiros=2, espera=0, longos=True)),
    "repetido": ("The same name twice, with and without ✅ (warning)", dict(pagos=11, nao_pagos=0, goleiros=2, espera=0, repetido=True)),
    "faltam2": ("13 on the list, 2 without ✅: the bot waits until everyone has paid", dict(pagos=11, nao_pagos=2, goleiros=2, espera=0)),
    "ninguem": ("Nobody has paid yet", dict(pagos=0, nao_pagos=10, goleiros=2, espera=1)),
    "v3": ("14/10 corpus list, no ✅ yet", None),
}
API_KEY = "simulator-api-key"

HELP = """Commands:
  /lista [scenario]           sends a list to the group (default: paga). Scenarios:
                              aleatoria, paga, 3pagos, 5pagos, 9pagos, 13linha, 15quadra, esperapaga, goleiros0, 15pagos, 18pagos, 20pagos,
                              24pagos, 26pessoas, 25pagos, nomeslongos, repetido, faltam2, ninguem, v1, v2, v3
                              or a file path
  /citar <text>               sends <text> replying to the latest list (example: /citar !sortear)
  /eu <person>                switches who is sending: presidente, admin, integrante or integrante2
  /ajuda                      shows these commands
  /sair                       stops the bot and the simulator
Anything else goes to the group as a message: !sortear, !sortear pagos, !times, !pendentes, !cobrar, !ajuda, hi..."""

WEB_PAGE = pathlib.Path(__file__).with_name("group-simulator.html")

print_lock = threading.Lock()
web_mode = False


def say(text):
    if web_mode:
        return
    with print_lock:
        print(f"\n{text}\n> ", end="", flush=True)


class Timeline:
    """Everything that happens in the fake group, in order, for the web page to poll."""

    def __init__(self):
        self._lock = threading.Lock()
        self._events = []

    def add(self, kind, who, text="", image=None, quoting=None):
        with self._lock:
            self._events.append({
                "n": len(self._events),
                "tipo": kind,
                "quem": who,
                "texto": text,
                "imagem": image,
                "citando": quoting,
                "hora": time.strftime("%H:%M"),
            })

    def since(self, n):
        with self._lock:
            return self._events[n:]


timeline = Timeline()


class Group:
    """The messages of the fake group, oldest first, shared by the prompt and the fake WAHA."""

    def __init__(self):
        self._lock = threading.Lock()
        self._messages = []
        self.last_list = None

    def post(self, sender, body, reply_to=None):
        participant, name = PEOPLE[sender]
        key = "3EB0" + secrets.token_hex(8).upper()
        message = {
            "id": f"false_{GROUP_ID}_{key}_{participant}",
            "timestamp": int(time.time()),
            "from": GROUP_ID,
            "fromMe": False,
            "participant": participant,
            "body": body,
            "replyTo": reply_to,
            "_data": {"Info": {"PushName": name}},
        }
        with self._lock:
            self._messages.append(message)
        return message, key

    def page(self, query):
        """GET /api/{session}/chats/{chatId}/messages: newest first, from filter.timestamp.gte, by limit and offset."""
        limit = int(query.get("limit", ["100"])[0])
        offset = int(query.get("offset", ["0"])[0])
        since = int(query.get("filter.timestamp.gte", ["0"])[0])
        with self._lock:
            newest_first = [message for message in reversed(self._messages) if message["timestamp"] >= since]
        return newest_first[offset:offset + limit]

    def quoted_text(self, message_id):
        """The text of the command a bot reply quotes, for the page to show above the reply."""
        message = self.find(message_id) if message_id else None
        return message["body"] if message is not None else None

    def find(self, message_id):
        """GET .../messages/{messageId}: by the full id or by the short key, as GOWS accepts both."""
        with self._lock:
            for message in self._messages:
                if message_id in (message["id"], message["id"].split("_")[2]):
                    return message
        return None


def fake_waha(group, open_cards):
    cards = {"count": 0}

    class FakeWaha(http.server.BaseHTTPRequestHandler):
        def log_message(self, *args):
            pass

        def answer(self, status, body):
            data = json.dumps(body).encode()
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)

        def do_GET(self):
            url = urllib.parse.urlparse(self.path)
            parts = [urllib.parse.unquote(part) for part in url.path.split("/") if part]
            if parts[:2] == ["api", "sessions"]:
                return self.answer(200, {"name": "default", "status": "WORKING"})
            if len(parts) == 5 and parts[2] == "chats" and parts[4] == "messages":
                return self.answer(200, group.page(urllib.parse.parse_qs(url.query)))
            if len(parts) == 6 and parts[2] == "chats" and parts[4] == "messages":
                message = group.find(parts[5])
                if message is not None:
                    return self.answer(200, message)
                return self.answer(404, {"statusCode": 404, "message": "Message not found"})
            self.answer(404, {"statusCode": 404})

        def read_body(self):
            # HttpClient sends JSON chunked, without Content-Length.
            if self.headers.get("Transfer-Encoding", "").lower() != "chunked":
                return self.rfile.read(int(self.headers.get("Content-Length", "0")))
            data = b""
            while True:
                size = int(self.rfile.readline().split(b";")[0].strip(), 16)
                if size == 0:
                    self.rfile.readline()
                    return data
                data += self.rfile.read(size)
                self.rfile.readline()

        def do_POST(self):
            body = json.loads(self.read_body() or b"{}")
            path = urllib.parse.urlparse(self.path).path
            if path == "/api/startTyping":
                say("🤖 typing...")
                timeline.add("digitando", "Pelada bot")
            elif path == "/api/sendText":
                say(f"🤖 Bot:\n{body.get('text', '')}")
                timeline.add("texto", "Pelada bot", body.get("text", ""), quoting=group.quoted_text(body.get("reply_to")))
            elif path == "/api/sendImage":
                cards["count"] += 1
                card = OUTPUT / f"card-{cards['count']}.png"
                card.write_bytes(base64.b64decode(body["file"]["data"]))
                say(f"🤖 Bot: [image {card.relative_to(ROOT)}]\n{body.get('caption', '')}")
                timeline.add("imagem", "Pelada bot", body.get("caption", ""), card.name,
                             quoting=group.quoted_text(body.get("reply_to")))
                if open_cards and sys.platform == "darwin":
                    subprocess.Popen(["open", str(card)])
            self.answer(201, {})

    return FakeWaha


def signed_webhook(bot_url, key, message):
    """Posts the message as WAHA would, signed with X-Webhook-Hmac."""
    event = {
        "id": "evt_" + secrets.token_hex(8),
        "timestamp": int(time.time() * 1000),
        "event": "message",
        "session": "default",
        "payload": message,
    }
    raw = json.dumps(event, ensure_ascii=False).encode()
    signature = hmac.new(key.encode(), raw, hashlib.sha512).hexdigest()
    request = urllib.request.Request(
        bot_url,
        data=raw,
        method="POST",
        headers={"Content-Type": "application/json", "X-Webhook-Hmac": signature, "X-Webhook-Hmac-Algorithm": "sha512"},
    )
    try:
        with urllib.request.urlopen(request, timeout=10) as response:
            return response.status
    except urllib.error.HTTPError as error:
        return error.code


def start_bot(args, key, log_path):
    env = dict(os.environ)
    env.update({
        "ASPNETCORE_URLS": f"http://127.0.0.1:{args.bot_port}",
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_NOLOGO": "1",
        "Waha__UrlBase": f"http://127.0.0.1:{args.waha_port}",
        "Waha__ChaveDaApi": API_KEY,
        "Waha__ChaveHmacDoWebhook": key,
        "Bot__Grupos__0__Id": GROUP_ID,
        "Bot__Grupos__0__Presidencia__0__Nome": PEOPLE["presidente"][1],
        "Bot__Grupos__0__Presidencia__0__Ids__0": PEOPLE["presidente"][0],
        "Bot__Grupos__0__Presidencia__1__Nome": PEOPLE["admin"][1],
        "Bot__Grupos__0__Presidencia__1__Ids__0": PEOPLE["admin"][0],
        "Bot__Grupos__0__TituloDoCard": args.titulo,
        "Bot__Grupos__0__CaminhoDoEscudo": args.escudo,
    })
    log = open(log_path, "w")
    # Built first and run from its output folder, so stopping the simulator stops the bot itself, not a `dotnet run` wrapper.
    build = subprocess.run(
        ["dotnet", "build", str(ROOT / "src" / "PeladaBot"), "--nologo", "-v", "quiet", "--disable-build-servers"],
        cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT)
    if build.returncode != 0:
        sys.exit(f"O build falhou. Veja {log_path}.")
    output = ROOT / "src" / "PeladaBot" / "bin" / "Debug" / "net10.0"
    return subprocess.Popen(["dotnet", str(output / "PeladaBot.dll")], cwd=output, env=env, stdout=log, stderr=subprocess.STDOUT)


def wait_for_bot(bot, args, log_path):
    deadline = time.time() + 180
    while time.time() < deadline:
        if bot.poll() is not None:
            sys.exit(f"O bot não subiu. Últimas linhas de {log_path}:\n" + "".join(open(log_path).readlines()[-20:]))
        try:
            urllib.request.urlopen(f"http://127.0.0.1:{args.bot_port}/", timeout=2)
            return
        except urllib.error.HTTPError:
            return  # GET / answers 404: the bot is up.
        except OSError:
            time.sleep(0.5)
    sys.exit(f"O bot não respondeu em 3 minutos. Veja {log_path}.")


def send_to_group(group, bot_url, key, sender, text, quote_last_list=False, is_list=False):
    """Posts a message to the fake group as `sender` and delivers it to the bot as a signed webhook."""
    reply_to = group.last_list if quote_last_list else None
    message, message_key = group.post(sender, text, reply_to)
    if is_list:
        group.last_list = {"id": message_key, "participant": message["participant"], "body": text}
    timeline.add("lista" if is_list else "usuario", PEOPLE[sender][1], text,
                 quoting=reply_to["body"] if reply_to else None)
    return signed_webhook(bot_url, key, message)


def web_page(group, bot_url, key):
    class WebPage(http.server.BaseHTTPRequestHandler):
        def log_message(self, *args):
            pass

        def answer(self, status, body, content_type="application/json"):
            data = body if isinstance(body, bytes) else json.dumps(body, ensure_ascii=False).encode()
            self.send_response(status)
            self.send_header("Content-Type", content_type)
            self.send_header("Content-Length", str(len(data)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(data)

        def do_GET(self):
            url = urllib.parse.urlparse(self.path)
            if url.path == "/":
                return self.answer(200, WEB_PAGE.read_bytes(), "text/html; charset=utf-8")
            if url.path == "/api/config":
                return self.answer(200, {
                    "pessoas": [{"id": key, "nome": name} for key, (_, name) in PEOPLE.items()],
                    "cenarios": [{"id": key, "nome": label} for key, (label, _) in SCENARIOS.items()],
                })
            if url.path == "/api/ultima-lista":
                return self.answer(200, {"texto": (group.last_list or {}).get("body")})
            if url.path == "/api/eventos":
                since = int(urllib.parse.parse_qs(url.query).get("desde", ["0"])[0])
                return self.answer(200, timeline.since(since))
            if url.path.startswith("/card/"):
                card = OUTPUT / pathlib.Path(url.path).name
                if card.suffix == ".png" and card.is_file():
                    return self.answer(200, card.read_bytes(), "image/png")
            self.answer(404, {"erro": "not found"})

        def do_POST(self):
            length = int(self.headers.get("Content-Length", "0"))
            body = json.loads(self.rfile.read(length) or b"{}")
            sender = body.get("remetente", "presidente")
            if sender not in PEOPLE:
                return self.answer(400, {"erro": "unknown sender"})
            if self.path == "/api/lista":
                text = body.get("texto") or read_list(body.get("lista", "paga"))
                if text is None:
                    return self.answer(400, {"erro": "list not found"})
                status = send_to_group(group, bot_url, key, sender, text, is_list=True)
            elif self.path == "/api/mensagem":
                text = str(body.get("texto", "")).strip()
                quote = bool(body.get("citarLista"))
                if text == "" or (quote and group.last_list is None):
                    return self.answer(400, {"erro": "send a list before you reply to it" if quote else "empty message"})
                status = send_to_group(group, bot_url, key, sender, text, quote_last_list=quote)
            else:
                return self.answer(404, {"erro": "not found"})
            self.answer(200, {"webhook": status})

    return WebPage


def load_local_lists():
    """Adds every .local/listas/*.txt as a scenario, so real lists can be tried without ever reaching Git."""
    if LOCAL_LISTS.is_dir():
        for path in sorted(LOCAL_LISTS.glob("*.txt")):
            SCENARIOS[f"local:{path.stem}"] = (f"Local list: {path.stem}", str(path))


def build_list(pagos, nao_pagos, goleiros, espera, longos=False, repetido=False, espera_paga=0, nomes=None):
    """A list in the group's own format, with fake names: paid first, then the ones without ✅. The first
    espera_paga names on the waiting list already paid (someone dropped out, or the second pitch was booked)."""
    names = nomes or (LONG_NAMES if longos else FAKE_NAMES)
    players = [f"{name} ✅" for name in names[:pagos]] + list(names[pagos:pagos + nao_pagos])
    if repetido:
        players.append(names[0])  # the first paid name again, now without ✅
    waiting = names[pagos + nao_pagos:pagos + nao_pagos + espera + espera_paga] if not longos else []
    lines = [
        "⚽PRÓXIMO JOGO⚽", "",
        "Data: Quarta-Feira – 14/10", "Horário: 20:30h", "Local: Campo Municipal",
        "Goleiros: FREE ✅", "",
        "Goleiros fixos:",
        *[f"{i}. {name}🧤" for i, name in enumerate(FAKE_GOALKEEPERS[:goleiros], 1)], "",
        "Lista de jogadores:", "",
        *[f"{i}. {name}" for i, name in enumerate(players, 1)], "",
        "Lista de espera:",
        *[f"{i}. {name}{' ✅' if i <= espera_paga else ''}" for i, name in enumerate(waiting, 1)], "",
        "Nota 1: Para confirmar sua presença, realize o pagamento e adicione o seu nome na lista.",
    ]
    return "\n".join(lines)


def random_list():
    """A different list on every send: how many play, how many paid, the fixed goalkeepers and the waiting list are
    drawn, and the names come shuffled, so a few clicks cover one pitch, two pitches, pending payments and 5 teams."""
    draw = random.Random()
    pagos = draw.randint(4, 26)
    nao_pagos = 0 if draw.random() < 0.6 else draw.randint(1, 4)
    espera = draw.randint(0, 3)
    espera_paga = draw.randint(1, 2) if draw.random() < 0.3 else 0
    nomes = FAKE_NAMES[:]
    draw.shuffle(nomes)
    return build_list(pagos, nao_pagos, draw.choice([0, 1, 2, 2, 2, 3, 4]), espera, espera_paga=espera_paga, nomes=nomes)


def read_list(choice):
    if choice == "aleatoria":
        return random_list()
    scenario = SCENARIOS.get(choice)
    if scenario is not None and isinstance(scenario[1], str):
        return pathlib.Path(scenario[1]).read_text(encoding="utf-8")
    if scenario is not None and scenario[1] is not None:
        return build_list(**scenario[1])
    if choice == "paga":
        return json.loads(PAID_LIST.read_text(encoding="utf-8"))["body"]
    path = CORPUS / f"2026-10-14-{choice}.txt" if choice in ("v1", "v2", "v3") else pathlib.Path(choice)
    if path.is_file() is False:
        return None
    return path.read_text(encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description="Simulates the pelada group so you can try the bot with no phone.")
    parser.add_argument("--waha-port", type=int, default=3100)
    parser.add_argument("--bot-port", type=int, default=8090)
    parser.add_argument("--no-open", action="store_true", help="do not open the card on screen")
    parser.add_argument("--web", action="store_true", help="open a chat page in the browser instead of the terminal")
    parser.add_argument("--web-port", type=int, default=8095)
    parser.add_argument("--titulo", default="", help='the name at the top of the card, such as "PELADA"')
    parser.add_argument("--escudo", default=None, help="crest image; default: .local/escudo.png or .jpeg, when it exists")
    args = parser.parse_args()
    if args.escudo is None:
        args.escudo = next((str(path) for path in LOCAL_CRESTS if path.is_file()), "")
    elif args.escudo:
        args.escudo = str(pathlib.Path(args.escudo).resolve())
    load_local_lists()
    global web_mode
    web_mode = args.web

    OUTPUT.mkdir(parents=True, exist_ok=True)
    log_path = OUTPUT / "bot.log"
    key = secrets.token_hex(32)
    group = Group()

    server = http.server.ThreadingHTTPServer(
        ("127.0.0.1", args.waha_port), fake_waha(group, args.no_open is False and args.web is False))
    threading.Thread(target=server.serve_forever, daemon=True).start()

    print(f"Starting the bot (log at {log_path.relative_to(ROOT)})...", flush=True)
    bot = start_bot(args, key, log_path)
    bot_url = f"http://127.0.0.1:{args.bot_port}/webhooks/waha"
    sender = "presidente"
    try:
        wait_for_bot(bot, args, log_path)
        if args.web:
            page = http.server.ThreadingHTTPServer(("127.0.0.1", args.web_port), web_page(group, bot_url, key))
            threading.Thread(target=page.serve_forever, daemon=True).start()
            address = f"http://127.0.0.1:{args.web_port}/"
            print(f"Bot is up. Open {address} (Ctrl+C stops it).", flush=True)
            if args.no_open is False and sys.platform == "darwin":
                subprocess.Popen(["open", address])
            try:
                threading.Event().wait()
            except KeyboardInterrupt:
                return
        print(f"Bot is up. You are {PEOPLE[sender][1]}.\n{HELP}")
        while True:
            try:
                line = input("> ").strip()
            except EOFError:
                break
            if line == "":
                continue
            command, _, rest = line.partition(" ")
            reply_to = None
            if command == "/sair":
                break
            if command == "/ajuda":
                print(HELP)
                continue
            if command == "/eu":
                if rest not in PEOPLE:
                    print("Use /eu presidente, /eu admin, /eu integrante or /eu integrante2.")
                    continue
                sender = rest
                print(f"You are now {PEOPLE[sender][1]}.")
                continue
            if command == "/lista":
                text = read_list(rest or "paga")
                if text is None:
                    print("List not found: use a scenario from /ajuda, or a file path.")
                    continue
            elif command == "/citar":
                if group.last_list is None or rest == "":
                    print("Send a list first (/lista), then the text: /citar !sortear")
                    continue
                text = rest
                reply_to = group.last_list
            else:
                text = line
            if command == "/lista":
                shown = f"[lista com {len(text.splitlines())} linhas]"
            else:
                shown = text if reply_to is None else f"(respondendo à lista) {text}"
            print(f"{PEOPLE[sender][1]}: {shown}")
            status = send_to_group(group, bot_url, key, sender, text,
                                   quote_last_list=reply_to is not None, is_list=command == "/lista")
            if status != 200:
                print(f"The bot answered {status} to the webhook. See {log_path.relative_to(ROOT)}.")
    finally:
        bot.terminate()
        try:
            bot.wait(timeout=15)
        except subprocess.TimeoutExpired:
            bot.kill()
        server.shutdown()


if __name__ == "__main__":
    main()
