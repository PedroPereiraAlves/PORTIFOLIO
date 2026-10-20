# Office assets

Gera o tileset, mapa Tiled e atlas de agentes do Escritório.

## Pré-requisitos

1. Python 3 + Pillow (`pip install pillow`)
2. Zips itch (grátis) em `_downloads/` (gitignored):
   - `PixelOffice.zip` — https://2dpig.itch.io/pixel-office
   - `Ninja Adventure - Asset Pack.zip` — https://pixel-boy.itch.io/ninja-adventure-asset-pack
3. Kenney: o script baixa sozinho (cache em `_downloads/`).

## Build

```powershell
py -3 tools/office-assets/fetch_and_build.py
py -3 tools/office-assets/render_map.py
```

Tiles por **nome** em `catalog.json` (proibido GID mágico). SHA-256 em `sources.lock.json`.

## Editar o mapa no Tiled

1. Abra `src/PedroHub.Web/wwwroot/lib/office/assets/map/escritorio.tmx` no Tiled 1.10+
2. Edite camadas `Chao` / `Paredes` / `Moveis-*` / `Colisao` / `Objetos`
3. **File → Export As → JSON** → sobrescreva `escritorio.json` (mesmo diretório)
4. Confira que o tileset relativo `../tiles/office-16.tsx` abre sem erro
