## O quê

Página privada **Escritório** com mapa 2D pixel art (Phaser 3 local), visão em lista acessível, ingestão `POST /api/gideon/agentes/status|eventos` protegida por `X-Gideon-Key`, elenco seed idempotente e edição admin simples.

## Por quê

Item aprovado do roadmap (“Escritório virtual estilo Gather”): o Owner vê o que cada agente está fazendo com dados reais publicados pelo Gideon — sem o Hub comandar o Gideon.

## Como testar

1. Merge/rebase após o PR #4 (Monitoramento), se ainda não estiver na `main`.
2. `dotnet ef database update` + `HUB_GIDEON_API_KEY=chave-fake-dev` + `dotnet run`
3. curl com/sem chave (ver `PROVA.md`)
4. Login Owner → `/admin/escritorio` (polling 20 s; lista no mobile)
5. `dotnet test` — 78 aprovados

## Riscos

- Phaser vendored (~1 MB) em `wwwroot/lib/phaser/`
- Sem chave configurada o endpoint responde 503 (não aceita)
- Ligação real Gideon→Hub fica para depois do deploy

## Fora de escopo

Comandos Hub→Gideon, chat, Cérebro, Painel do dia, rotinas, deploy em um clique.
