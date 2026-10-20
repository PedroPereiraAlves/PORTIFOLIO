# Prova — Escritório visual v2 (PR B / ajustes)

## Causa-raiz da tela preta

Phaser 3.80 baixava imagens como `blob:` → CSP `img-src 'self' data:` bloqueava. Correção: `loader: { imageLoadType: "HTMLImageElement" }` em `boot.js` (sem `blob:` na CSP global).

## Build / testes

```
dotnet test PedroHub.sln --configuration Release
node --test tests/escritorio-js/agents.test.js
py -3 tools/office-assets/fetch_and_build.py
py -3 tools/office-assets/render_map.py
```

## Assets (CC0, sem fallback procedural)

| Uso | Pack |
|-----|------|
| Paredes/portas + 6 chars Urban | Kenney RPG Urban |
| Piso madeira/tapete | Kenney Roguelike Indoors |
| Piso azul, mesas, cadeiras, monitores, props | 2dPig Pixel Office |
| 6 chars (Gideon = Master + coroa) | Ninja Adventure `Actor/Character/*` |

- Catálogo por nome → GID: `tools/office-assets/catalog.json`
- Lock SHA-256: `tools/office-assets/sources.lock.json`
- Contato: `lib/office/assets/tiles/catalog.png`
- Mapa offline: `docs/pr-screenshots/escritorio-v2/mapa-completo-2x.png`
- Cast: `lib/office/assets/chars/cast.json` (12 bases distintas)
- type: frames derivados idle-up ±1 px (não walk)

## Front (ajustes)

- XSS: `renderLista` / painel com `textContent` + `createElement`
- Teclado: Tab destaca; Enter abre; Esc fecha e devolve foco
- Mobile: um toggle; Phaser injetado só ao mostrar mapa
- Layout: painel `position:absolute` overlay
- Nametags: cria uma vez, atualiza `left`/`top`
- Hover: contorno 1 px; retrato frame `_face` em canvas 64×64

## Prints / demo

Pasta: `docs/pr-screenshots/escritorio-v2/`

- [x] `mapa-completo-2x.png` (render offline)
- [ ] `mapa-desktop-1440.png`, `mapa-painel-1440.png`, `mapa-dark-1440.png`
- [ ] `mapa-mobile-390.png`, `lista-mobile-390.png`
- [ ] `comparativo-skyoffice.png`, `demo.webm`/`demo.gif`

Captura: ver `PR_BODY.md` (credenciais `HUB_CAPTURE_*` — não commitadas). O script falha se o console tiver CSP/load errors ou se `#escritorio-game[data-load-error]` existir.

## Não feito nesta sessão do agent

- Prints Playwright / GIF (sem `HUB_CAPTURE_EMAIL`/`PASSWORD` no ambiente)
- Merge; PRs abertos via compare URLs (gh sem auth)
