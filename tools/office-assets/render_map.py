#!/usr/bin/env python3
"""Render offline do mapa Tiled JSON → mapa-completo-2x.png."""
from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "src/PedroHub.Web/wwwroot/lib/office/assets"
OUT = ROOT / "docs/pr-screenshots/escritorio-v2/mapa-completo-2x.png"
TILE = 16
CELL = 18  # extrude


def main() -> None:
    mapa = json.loads((ASSETS / "map/escritorio.json").read_text(encoding="utf-8"))
    atlas = Image.open(ASSETS / "tiles/office-16.png").convert("RGBA")
    ts = mapa["tilesets"][0]
    cols = ts["columns"]
    margin = ts.get("margin", 1)
    spacing = ts.get("spacing", 2)
    firstgid = ts["firstgid"]

    w, h = mapa["width"], mapa["height"]
    canvas = Image.new("RGBA", (w * TILE, h * TILE), (20, 24, 32, 255))

    def blit_gid(gid: int, dx: int, dy: int) -> None:
        if gid <= 0:
            return
        tid = gid - firstgid
        if tid < 0:
            return
        col = tid % cols
        row = tid // cols
        sx = margin + col * (TILE + spacing)
        sy = margin + row * (TILE + spacing)
        tile = atlas.crop((sx, sy, sx + TILE, sy + TILE))
        canvas.paste(tile, (dx, dy), tile)

    for layer in mapa["layers"]:
        if layer.get("type") != "tilelayer":
            continue
        if layer.get("name") == "Colisao":
            continue
        data = layer["data"]
        for i, gid in enumerate(data):
            x, y = i % w, i // w
            blit_gid(gid, x * TILE, y * TILE)

    out = canvas.resize((canvas.width * 2, canvas.height * 2), Image.NEAREST)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    out.save(OUT, optimize=True)
    print("wrote", OUT, out.size)


if __name__ == "__main__":
    main()
