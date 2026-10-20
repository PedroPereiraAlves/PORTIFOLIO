## O quê

Backend do **Escritório virtual**: domínio/persistência, ingestão Gideon (`X-Gideon-Key`), retenção, páginas admin e **visão em lista acessível** (UI provisória). Sem Phaser/mapa neste PR.

## Por quê

Separar backend corrigido do visual reprovado: auth na ordem certa, allowlist de PR sem bypass de prefixo, testes de integração e lista usável enquanto o mapa Gather-like chega no PR B.

## Como testar

1. Base: `main` (já com Monitoramento / PR #4).
2. `dotnet ef database update` + `HUB_GIDEON_API_KEY=chave-fake-dev`
3. `dotnet test` — 99 aprovados (inclui Web.Tests)
4. Login Owner → `/admin/escritorio` (lista + painel; polling 20 s)
5. Ver `PROVA.md` para curls / cenários de status code

## Riscos

- Sem chave configurada → 503
- Mapa pixel art **não** está neste PR (próximo: `feat/escritorio-visual`)
- Ligação real Gideon→Hub fica para depois do deploy

## Fora de escopo

Mapa Phaser/Tiled, assets, chat, comandos Hub→Gideon, Cérebro.
