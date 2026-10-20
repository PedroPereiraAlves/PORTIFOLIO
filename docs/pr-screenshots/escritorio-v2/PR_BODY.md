## O quê

Mapa **Gather-like** do Escritório: Tiled JSON + Phaser 3 (pixelArt, zoom inteiro), atlas CC0, pathfinding A*, comportamentos por status, painel com retrato/timeline, lista acessível e teclado.

## Por quê

A v1 (retângulos + tint) foi reprovada. Este PR substitui o visual inteiro, mantendo o backend do PR A.

## Como testar

1. Merge do PR A (`feat/escritorio-backend`) na `main` (ou abrir contra essa branch).
2. `dotnet run` + login Owner → `/admin/escritorio`
3. `dotnet test` + `node --test tests/escritorio-js/agents.test.js`
4. Mobile 390: lista default; “Ver mapa”; sem scroll horizontal
5. Tab/Enter/Esc no canvas; status muda animação (trabalhando/revisando/ocioso/…)

## Riscos / limitações

- Personagens: sheets humanas do Ninja Adventure (Godot 4) + recolors CC0 — pack itch completo `Actor/Characters/*` não veio no mirror GitHub
- Props 2dPig: fallback Hub CC0 no mesmo grid (documentado)
- Prints/demo dependem de `HUB_CAPTURE_*`

## Fora de escopo

Comandos Hub→Gideon, chat, multiplayer, LimeZu (pago).
