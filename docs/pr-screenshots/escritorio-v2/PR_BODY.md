## Summary

- Corrige a tela preta do Escritório: Phaser carrega imagens via `HTMLImageElement` (same-origin), sem `blob:` na CSP global.
- Substitui atlas/IDs mágicos e samurais recoloridos por catálogo nomeado (`catalog.json`) + packs CC0 (Kenney Urban/Indoors, 2dPig Pixel Office, Ninja Adventure `Actor/Character/*`).
- Front: XSS-safe (DOM/`textContent`), Tab só destaca / Enter abre / Esc fecha, painel overlay (sem coluna 360 px vazia), lazy Phaser no mobile, nametags sem `innerHTML` por frame, hover com contorno, retrato `{slug}_face`.

## Base

`feat/escritorio-backend` (não `main`). Draft até as provas do §5.

## Como testar

1. `dotnet run --project src/PedroHub.Web` → login Owner → `/admin/escritorio`
2. Console: zero de CSP / Failed to process / Texture / `__MISSING` / `[escritorio] falha ao carregar`
3. `dotnet test` + `node --test tests/escritorio-js/*.test.js`
4. Tab×2 no mapa: 2º agente destacado, painel fechado → Enter → Esc
5. Mobile 390: lista default; toggle Mapa; Phaser só após toggle

## Captura (Pedro)

```powershell
# terminal 1
dotnet run --project src/PedroHub.Web
# terminal 2
cd docs/pr-screenshots
npm ci
npx playwright install chromium
$env:HUB_CAPTURE_BASE="http://localhost:5276"
$env:HUB_CAPTURE_EMAIL="<email do Owner>"
$env:HUB_CAPTURE_PASSWORD="<senha do Owner>"
$env:HUB_GIDEON_API_KEY="chave-fake-dev"
node capture-escritorio.mjs
```

## Assets

- `tools/office-assets/catalog.json` + `sources.lock.json` (SHA-256)
- Build: `py -3 tools/office-assets/fetch_and_build.py` (exige zips itch em `_downloads/`)
- Render offline: `mapa-completo-2x.png` + `tiles/catalog.png`
- Licenças: `lib/office/assets/CREDITS.md` + `LICENSES/*`

## Fora de escopo

LimeZu pago, chat/multiplayer, merge deste PR.
