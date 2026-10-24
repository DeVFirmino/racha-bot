# Contribuindo

Issues e pull requests são bem-vindos.

## Antes de abrir um pull request

- Faça o build e os testes: `dotnet build` e `dotnet test --solution PeladaBot.slnx`. O CI também roda `dotnet format --verify-no-changes`.
- Mantenha uma preocupação por pull request, e explique o porquê na descrição.
- Tudo fica em português: código, logs, comentários, docs, respostas do bot e comandos.
- Nunca comite números de telefone reais, IDs do WhatsApp, nomes de um grupo real ou payloads crus do WAHA. As fixtures usam os IDs falsos dos testes.

## Builds de Release

O ImageSharp 4 precisa de uma chave de licença da Six Labors para um build Release. Um build Debug só avisa, então você pode trabalhar sem uma chave. O CI builda o pull request de um fork em Debug.
