# Prova — Escritório visual v2 (PR B)

## Build / testes

```
dotnet test PedroHub.sln --configuration Release
→ 99 testes .NET aprovados

node --test tests/escritorio-js/agents.test.js
→ 8 testes JS aprovados (status→comportamento, A*, Tab order, name tags)
```

## Assets

- Fonte: Kenney Roguelike Indoors + RPG Urban Pack (CC0), Ninja Adventure samurai sheets (CC0) + recolors, props Hub CC0 fallback
- Orçamento: ~188 KB assets (sem Phaser) ≪ 2 MB
- `lib/office/assets/CREDITS.md` + `LICENSES/*`
- Build: `py -3 tools/office-assets/fetch_and_build.py`

## Prints / demo

Pasta: `docs/pr-screenshots/escritorio-v2/`

Geração: `node docs/pr-screenshots/capture-escritorio.mjs` com `HUB_CAPTURE_EMAIL` / `HUB_CAPTURE_PASSWORD`.

Esperados:

- mapa-desktop-1440.png
- mapa-painel-1440.png
- mapa-mobile-390.png / lista-mobile-390.png
- mapa-dark-1440.png
- comparativo-skyoffice.png
- demo.gif (≤15 s)

## Não testado nesta sessão do agent

- Prints Playwright / GIF (credenciais `HUB_CAPTURE_*` ausentes no ambiente)
- curl manual com Postgres local (status codes cobertos por Web.Tests)
- Ligação real Gideon → Hub em produção
- Pack completo itch “Actor/Characters/*” (repo Godot 4 só traz samurai_*; variantes por recolor CC0 documentadas)
- 2dPig Pixel Office (download itch); props de monitor/PC gerados no grid 16×16 Hub CC0

## Config

- `HUB_GIDEON_API_KEY` → `Gideon:ApiKey`
- `Escritorio:RetencaoDias`, `Escritorio:MinutosDesatualizado`, `Escritorio:PollingSegundos`
