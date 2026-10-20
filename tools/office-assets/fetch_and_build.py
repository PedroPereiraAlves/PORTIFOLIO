#!/usr/bin/env python3
"""Build office assets from CC0 packs. Sem fallback procedural — zips obrigatórios."""
from __future__ import annotations

import hashlib
import io
import json
import re
import shutil
import sys
import tempfile
import urllib.request
import zipfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
TOOLS = Path(__file__).resolve().parent
DL = TOOLS / "_downloads"
ASSETS = ROOT / "src/PedroHub.Web/wwwroot/lib/office/assets"
TILE = 16
MAP_W, MAP_H = 56, 34
CELL = 18  # 16 + extrude 1 each side

KENNEY_URLS = {
    "kenney_rpg-urban-pack.zip": "https://kenney.nl/media/pages/assets/rpg-urban-pack/0a097d1dc7-1677578575/kenney_rpg-urban-pack.zip",
    "kenney_roguelike-indoors.zip": "https://kenney.nl/media/pages/assets/roguelike-indoors/4d5b520b03-1702169567/kenney_roguelike-indoors.zip",
}

# 6 Ninja humans (Actor/Character singular) + Gideon = Master
NINJA_CAST = [
    ("gideon", "Master"),
    ("backend-hub", "Villager"),
    ("front-hub", "Villager2"),
    ("monitoramento-hub", "Woman"),
    ("seguranca-hub", "Inspector"),
    ("infra-testes-hub", "OldMan"),
]
# remaining 6 from Kenney urban people (cols 23-26) — mapped by index
URBAN_SLUGS = [
    "backend-clinica",
    "front-clinica",
    "seguranca-clinica",
    "infra-clinica",
    "design-clinica",
    "testes-clinica",
]


def die(msg: str) -> None:
    print("ERROR:", msg, file=sys.stderr)
    sys.exit(1)


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def fetch(url: str, dest: Path) -> None:
    if dest.exists() and dest.stat().st_size > 1000:
        print("OK cached", dest.name)
        return
    print("GET", url)
    req = urllib.request.Request(url, headers={"User-Agent": "PedroHub-asset-build/1.0"})
    with urllib.request.urlopen(req, timeout=120) as r:
        dest.write_bytes(r.read())
    print(" ->", dest.stat().st_size)


def require_itch_zips() -> tuple[Path, Path]:
    pig = DL / "PixelOffice.zip"
    ninja = None
    for p in DL.glob("Ninja Adventure*.zip"):
        ninja = p
        break
    if not pig.exists() or not ninja:
        die(
            "Baixe manualmente (grátis, \"No thanks, just take me to the downloads\"):\n"
            "  https://2dpig.itch.io/pixel-office            -> PixelOffice.zip\n"
            "  https://pixel-boy.itch.io/ninja-adventure-asset-pack -> Ninja Adventure - Asset Pack.zip\n"
            "e coloque em tools/office-assets/_downloads/"
        )
    # ignore old clone
    old = DL / "NinjaAdventure"
    if old.exists():
        print("Removendo clone antigo NinjaAdventure/")
        shutil.rmtree(old)
    return pig, ninja


def update_lock(files: list[Path]) -> None:
    lock_path = TOOLS / "sources.lock.json"
    prev = {}
    if lock_path.exists():
        prev = json.loads(lock_path.read_text(encoding="utf-8"))
    lock = {}
    for f in files:
        digest = sha256(f)
        key = f.name
        if key in prev and prev[key] != digest:
            print(f"AVISO: hash mudou para {key}: {prev[key][:12]}… → {digest[:12]}…")
        lock[key] = digest
        print(f"SHA256 {key}: {digest}")
    lock_path.write_text(json.dumps(lock, indent=2), encoding="utf-8")


def crop_tile(sheet: Image.Image, col: int, row: int, spacing: int) -> Image.Image:
    step = TILE + spacing
    x, y = col * step, row * step
    return sheet.crop((x, y, x + TILE, y + TILE)).convert("RGBA")


def extrude_atlas(tiles: list[tuple[str, Image.Image]]) -> tuple[Image.Image, dict[str, int]]:
    cols = 16
    rows = (len(tiles) + cols - 1) // cols
    atlas = Image.new("RGBA", (cols * CELL, rows * CELL), (0, 0, 0, 0))
    name_to_id: dict[str, int] = {}
    for i, (name, tile) in enumerate(tiles):
        name_to_id[name] = i
        ax = (i % cols) * CELL
        ay = (i // cols) * CELL
        atlas.paste(tile, (ax + 1, ay + 1))
        px = tile.load()
        for x in range(TILE):
            atlas.putpixel((ax + 1 + x, ay), px[x, 0])
            atlas.putpixel((ax + 1 + x, ay + 17), px[x, 15])
        for y in range(TILE):
            atlas.putpixel((ax, ay + 1 + y), px[0, y])
            atlas.putpixel((ax + 17, ay + 1 + y), px[15, y])
    return atlas, name_to_id


def contact_sheet(tiles: list[tuple[str, Image.Image]], path: Path) -> None:
    cols = 8
    rows = (len(tiles) + cols - 1) // cols
    cell_w, cell_h = 80, 48
    img = Image.new("RGBA", (cols * cell_w, rows * cell_h), (30, 30, 40, 255))
    draw = ImageDraw.Draw(img)
    for i, (name, tile) in enumerate(tiles):
        ax = (i % cols) * cell_w
        ay = (i // cols) * cell_h
        scaled = tile.resize((32, 32), Image.NEAREST)
        img.paste(scaled, (ax + 4, ay + 2), scaled)
        draw.text((ax + 4, ay + 34), name[:14], fill=(220, 220, 220, 255))
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path)


def open_from_zip(zpath: Path, inner: str) -> Image.Image:
    with zipfile.ZipFile(zpath) as z:
        return Image.open(io.BytesIO(z.read(inner))).convert("RGBA")


def build_tiles(urban_zip: Path, indoors_zip: Path, pig_zip: Path) -> dict[str, int]:
    catalog_raw = json.loads((TOOLS / "catalog.json").read_text(encoding="utf-8"))
    catalog_tiles = catalog_raw["tiles"] if isinstance(catalog_raw, dict) else catalog_raw
    sheets: dict[tuple[str, str], Image.Image] = {}
    pack_zip = {
        "rpg-urban": urban_zip,
        "roguelike-indoors": indoors_zip,
        "pixel-office": pig_zip,
    }
    tiles: list[tuple[str, Image.Image]] = []
    for entry in catalog_tiles:
        key = (entry["pack"], entry["sheet"])
        if key not in sheets:
            sheets[key] = open_from_zip(pack_zip[entry["pack"]], entry["sheet"])
        tile = crop_tile(sheets[key], entry["col"], entry["row"], entry.get("spacing", 0))
        tiles.append((entry["nome"], tile))

    atlas, name_to_id = extrude_atlas(tiles)
    out = ASSETS / "tiles"
    out.mkdir(parents=True, exist_ok=True)
    atlas.save(out / "office-16.png", optimize=True)
    contact_sheet(tiles, out / "catalog.png")
    (out / "office-16.tsx").write_text(
        f"""<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.10" tiledversion="1.10.2" name="office-16" tilewidth="16" tileheight="16" tilecount="{len(tiles)}" columns="16" margin="1" spacing="2">
 <image source="office-16.png" width="{atlas.width}" height="{atlas.height}"/>
</tileset>
""",
        encoding="utf-8",
    )
    print("tiles", len(tiles), "atlas", atlas.size)
    return name_to_id


def crown(frame: Image.Image) -> Image.Image:
    im = frame.copy()
    px = im.load()
    gold = (245, 183, 0, 255)
    for x in range(5, 11):
        px[x, 1] = gold
    px[7, 0] = gold
    px[8, 0] = gold
    px[6, 0] = gold
    px[9, 0] = gold
    return im


def type_frames(idle_up: Image.Image) -> tuple[Image.Image, Image.Image]:
    """2 frames sentado de costas: braço/cabeça ±1px (não walk)."""
    a = idle_up.copy()
    b = idle_up.copy()
    # shift head region 1px
    region = idle_up.crop((5, 1, 11, 6))
    b.paste((0, 0, 0, 0), (5, 1, 11, 6))
    b.paste(region, (5, 0), region)
    return a, b


def extract_ninja_char(z: zipfile.ZipFile, folder: str) -> tuple[list[Image.Image], Image.Image]:
    prefix = f"Ninja Adventure - Asset Pack/Actor/Character/{folder}/"
    sheet = Image.open(io.BytesIO(z.read(prefix + "SpriteSheet.png"))).convert("RGBA")
    face = Image.open(io.BytesIO(z.read(prefix + "Faceset.png"))).convert("RGBA")
    frames = []
    for row in range(4):
        for col in range(4):
            frames.append(sheet.crop((col * 16, row * 16, col * 16 + 16, row * 16 + 16)))
    face = face.resize((32, 32), Image.NEAREST)
    return frames, face


def extract_urban_people(urban_zip: Path) -> list[tuple[list[Image.Image], Image.Image]]:
    """6 people from tilemap cols 23-26 — take distinct rows with character-like tiles."""
    sheet = open_from_zip(urban_zip, "Tilemap/tilemap_packed.png")
    # Kenney urban packed: people often in lower rows; sample 6 vertical strips
    people = []
    # Use columns 23-26, pick 6 different starting rows that look like characters
    starts = [(23, 10), (23, 12), (24, 10), (24, 12), (25, 10), (25, 12)]
    for col, row0 in starts:
        frames = []
        for d, r in enumerate(range(row0, row0 + 4)):
            for c in range(4):
                # walk cycle approx from neighboring cols
                cc = min(26, col + (c % 2))
                frames.append(crop_tile(sheet, cc, r, 0))
        face = frames[0].resize((32, 32), Image.NEAREST)
        people.append((frames, face))
    return people


def build_chars(ninja_zip: Path, urban_zip: Path) -> None:
    dirs = ["down", "left", "right", "up"]
    frames_json = {"frames": {}, "meta": {"image": "agents.png", "size": {"w": 0, "h": 0}, "scale": "1"}}
    cast = []
    strips: list[tuple[str, list[Image.Image], Image.Image, Image.Image, Image.Image]] = []

    with zipfile.ZipFile(ninja_zip) as z:
        for slug, folder in NINJA_CAST:
            frames, face = extract_ninja_char(z, folder)
            if slug == "gideon":
                frames = [crown(f) if i < 4 else crown(f) for i, f in enumerate(frames)]
                # crown all
                frames = [crown(f) for f in frames]
            t0, t1 = type_frames(frames[12])  # up idle-ish
            strips.append((slug, frames, face, t0, t1))
            cast.append(
                {
                    "slug": slug,
                    "pack": "ninja-adventure",
                    "source": f"Actor/Character/{folder}/SpriteSheet.png",
                    "license": "CC0",
                }
            )

    urban_people = extract_urban_people(urban_zip)
    for slug, (frames, face) in zip(URBAN_SLUGS, urban_people):
        t0, t1 = type_frames(frames[12] if len(frames) > 12 else frames[0])
        strips.append((slug, frames, face, t0, t1))
        cast.append(
            {
                "slug": slug,
                "pack": "rpg-urban",
                "source": "Tilemap/tilemap_packed.png",
                "license": "CC0",
            }
        )

    # Pack atlas: each agent 4x4 frames + type 2 + face
    agent_w = 64
    agent_h = 64 + 16 + 32  # walk block + type row + face
    atlas = Image.new("RGBA", (agent_w * 12, agent_h), (0, 0, 0, 0))
    for i, (slug, frames, face, t0, t1) in enumerate(strips):
        ox = i * agent_w
        for fi, fr in enumerate(frames[:16]):
            col, row = fi % 4, fi // 4
            atlas.paste(fr, (ox + col * 16, row * 16))
            key = f"{slug}_{dirs[row]}_{col}"
            frames_json["frames"][key] = {
                "frame": {"x": ox + col * 16, "y": row * 16, "w": 16, "h": 16},
                "rotated": False,
                "trimmed": False,
                "spriteSourceSize": {"x": 0, "y": 0, "w": 16, "h": 16},
                "sourceSize": {"w": 16, "h": 16},
            }
        atlas.paste(t0, (ox, 64))
        atlas.paste(t1, (ox + 16, 64))
        frames_json["frames"][f"{slug}_type_0"] = {
            "frame": {"x": ox, "y": 64, "w": 16, "h": 16},
            "rotated": False,
            "trimmed": False,
            "spriteSourceSize": {"x": 0, "y": 0, "w": 16, "h": 16},
            "sourceSize": {"w": 16, "h": 16},
        }
        frames_json["frames"][f"{slug}_type_1"] = {
            "frame": {"x": ox + 16, "y": 64, "w": 16, "h": 16},
            "rotated": False,
            "trimmed": False,
            "spriteSourceSize": {"x": 0, "y": 0, "w": 16, "h": 16},
            "sourceSize": {"w": 16, "h": 16},
        }
        atlas.paste(face, (ox, 80))
        frames_json["frames"][f"{slug}_face"] = {
            "frame": {"x": ox, "y": 80, "w": 32, "h": 32},
            "rotated": False,
            "trimmed": False,
            "spriteSourceSize": {"x": 0, "y": 0, "w": 32, "h": 32},
            "sourceSize": {"w": 32, "h": 32},
        }

    frames_json["meta"]["size"] = {"w": atlas.width, "h": atlas.height}
    chars = ASSETS / "chars"
    chars.mkdir(parents=True, exist_ok=True)
    atlas.save(chars / "agents.png", optimize=True)
    (chars / "agents.json").write_text(json.dumps(frames_json, indent=2), encoding="utf-8")
    (chars / "cast.json").write_text(json.dumps(cast, indent=2), encoding="utf-8")
    print("chars", [c["slug"] for c in cast])


def build_bubbles() -> None:
    atlas = Image.new("RGBA", (80, 16), (0, 0, 0, 0))
    draw = ImageDraw.Draw(atlas)
    for i, lab in enumerate(["!", "z", ".", "v", "*"]):
        x = i * 16
        draw.ellipse([x + 1, 1, x + 14, 14], fill=(20, 20, 30, 200))
        draw.text((x + 5, 2), lab, fill=(255, 255, 255, 255))
    ui = ASSETS / "ui"
    ui.mkdir(parents=True, exist_ok=True)
    atlas.save(ui / "bubbles.png", optimize=True)
    frames = {
        "frames": {
            k: {"frame": {"x": i * 16, "y": 0, "w": 16, "h": 16}}
            for i, k in enumerate(["bang", "sleep", "dots", "check", "lamp"])
        },
        "meta": {"image": "bubbles.png", "size": {"w": 80, "h": 16}, "scale": "1"},
    }
    (ui / "bubbles.json").write_text(json.dumps(frames, indent=2), encoding="utf-8")


def gid(name_to_id: dict[str, int], name: str) -> int:
    if name not in name_to_id:
        die(f"tile '{name}' ausente do catálogo")
    return name_to_id[name] + 1  # firstgid=1


def build_map(name_to_id: dict[str, int]) -> None:
    def blank():
        return [0] * (MAP_W * MAP_H)

    chao, paredes, mov_ab, mov_ac, col = blank(), blank(), blank(), blank(), blank()

    def fill(layer, x, y, w, h, name):
        tid = gid(name_to_id, name)
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                layer[yy * MAP_W + xx] = tid

    def wall_box(x, y, w, h):
        fill(paredes, x, y, w, 1, "parede-topo")
        fill(col, x, y, w, 1, "parede-topo")
        fill(paredes, x, y + h - 1, w, 1, "parede-face")
        fill(col, x, y + h - 1, w, 1, "parede-face")
        fill(paredes, x, y, 1, h, "parede-face")
        fill(col, x, y, 1, h, "parede-face")
        fill(paredes, x + w - 1, y, 1, h, "parede-face")
        fill(col, x + w - 1, y, 1, h, "parede-face")

    def door(x, y, horizontal=True, size=3):
        for i in range(size):
            xx, yy = (x + i, y) if horizontal else (x, y + i)
            paredes[yy * MAP_W + xx] = 0
            col[yy * MAP_W + xx] = 0
            chao[yy * MAP_W + xx] = gid(name_to_id, "porta")

    # Hub
    fill(chao, 1, 8, 18, 17, "piso-hub")
    wall_box(1, 8, 18, 17)
    door(17, 15, False, 3)
    # Clinica
    fill(chao, 37, 8, 18, 17, "piso-clinica")
    wall_box(37, 8, 18, 17)
    door(37, 15, False, 3)
    # Gideon
    fill(chao, 22, 12, 12, 11, "piso-corredor")
    wall_box(22, 12, 12, 11)
    door(26, 12, True, 4)
    door(26, 22, True, 4)
    # Meeting
    fill(chao, 20, 1, 16, 10, "piso-reuniao")
    wall_box(20, 1, 16, 10)
    door(26, 10, True, 4)
    # Lounge
    fill(chao, 20, 25, 16, 8, "piso-lounge")
    wall_box(20, 25, 16, 8)
    door(26, 25, True, 4)
    # Corridors
    fill(chao, 19, 14, 18, 4, "piso-corredor")
    fill(chao, 26, 10, 4, 16, "piso-corredor")

    objects = []
    oid = 1

    def add_obj(typ, x, y, **props):
        nonlocal oid
        objects.append(
            {
                "id": oid,
                "name": props.get("slug", typ),
                "type": typ,
                "x": x * TILE,
                "y": y * TILE,
                "width": 16,
                "height": 16,
                "visible": True,
                "properties": [{"name": k, "type": "string", "value": str(v)} for k, v in props.items()],
            }
        )
        oid += 1

    def desk_station(slug, x, y):
        mov_ab[y * MAP_W + x] = gid(name_to_id, "mesa-trabalho")
        mov_ab[y * MAP_W + x + 1] = gid(name_to_id, "mesa-trabalho-b")
        col[y * MAP_W + x] = 1
        col[y * MAP_W + x + 1] = 1
        mov_ac[y * MAP_W + x] = gid(name_to_id, "monitor")
        mov_ac[y * MAP_W + x + 1] = gid(name_to_id, "pc")
        mov_ab[(y + 1) * MAP_W + x] = gid(name_to_id, "cadeira-escritorio-up")
        add_obj("spawn", x, y + 1, slug=slug)
        add_obj("cadeira", x, y + 1, slug=slug, facing="up")

    for slug, x, y in [
        ("backend-hub", 4, 11),
        ("front-hub", 8, 11),
        ("monitoramento-hub", 12, 11),
        ("seguranca-hub", 4, 17),
        ("infra-testes-hub", 8, 17),
        ("backend-clinica", 40, 11),
        ("front-clinica", 44, 11),
        ("seguranca-clinica", 48, 11),
        ("infra-clinica", 40, 17),
        ("design-clinica", 44, 17),
        ("testes-clinica", 48, 17),
    ]:
        desk_station(slug, x, y)

    # Gideon big desk
    for yy in range(16, 18):
        for xx in range(25, 29):
            mov_ab[yy * MAP_W + xx] = gid(name_to_id, "mesa-trabalho")
            col[yy * MAP_W + xx] = 1
    mov_ac[16 * MAP_W + 26] = gid(name_to_id, "monitor")
    mov_ac[16 * MAP_W + 27] = gid(name_to_id, "pc")
    add_obj("spawn", 26, 18, slug="gideon")
    add_obj("cadeira", 26, 18, slug="gideon", facing="up")
    mov_ab[18 * MAP_W + 26] = gid(name_to_id, "cadeira-escritorio-up")

    # Meeting
    fill(mov_ab, 24, 4, 8, 3, "mesa-reuniao")
    fill(col, 24, 4, 8, 3, "mesa-reuniao")
    for i, (cx, cy, facing) in enumerate(
        [(24, 3, "down"), (27, 3, "down"), (30, 3, "down"), (24, 7, "up"), (27, 7, "up"), (30, 7, "up")]
    ):
        name = "cadeira-reuniao-down" if facing == "down" else "cadeira-reuniao-up"
        mov_ab[cy * MAP_W + cx] = gid(name_to_id, name)
        add_obj("ponto-reuniao", cx, cy, facing=facing, seat=str(i))
    mov_ac[2 * MAP_W + 27] = gid(name_to_id, "quadro-branco")
    mov_ac[2 * MAP_W + 28] = gid(name_to_id, "quadro-branco")

    # Lounge
    fill(mov_ab, 22, 28, 3, 2, "sofa")
    fill(col, 22, 28, 3, 2, "sofa")
    fill(mov_ab, 28, 29, 2, 2, "tapete")
    mov_ab[27 * MAP_W + 33] = gid(name_to_id, "bebedouro")
    col[27 * MAP_W + 33] = 1
    mov_ab[27 * MAP_W + 32] = gid(name_to_id, "cafeteira")
    for px, py in [(21, 27), (34, 27), (21, 31), (34, 31), (28, 30), (32, 30)]:
        mov_ab[py * MAP_W + px] = gid(name_to_id, "planta-baixa")
        col[py * MAP_W + px] = 1
        mov_ac[(py - 1) * MAP_W + px] = gid(name_to_id, "planta-alta")
    mov_ab[13 * MAP_W + 23] = gid(name_to_id, "estante")
    mov_ac[12 * MAP_W + 23] = gid(name_to_id, "estante")
    col[13 * MAP_W + 23] = 1
    mov_ab[13 * MAP_W + 32] = gid(name_to_id, "estante")
    mov_ac[12 * MAP_W + 32] = gid(name_to_id, "estante")
    col[13 * MAP_W + 32] = 1

    for i, (lx, ly) in enumerate([(22, 29), (24, 30), (28, 28), (30, 31), (32, 29), (33, 30)]):
        add_obj("ponto-lounge", lx, ly, idx=str(i))

    for nome, x, y in [
        ("Sala Hub", 6, 9),
        ("Sala Clínica", 42, 9),
        ("Aquário Gideon", 24, 13),
        ("Reunião", 24, 2),
        ("Copa / Lounge", 24, 26),
    ]:
        objects.append(
            {
                "id": oid,
                "name": nome,
                "type": "rotulo-sala",
                "x": x * TILE,
                "y": y * TILE,
                "width": 96,
                "height": 16,
                "visible": True,
                "properties": [{"name": "nome", "type": "string", "value": nome}],
            }
        )
        oid += 1

    objects.append(
        {
            "id": oid,
            "name": "area-gideon",
            "type": "area-privada",
            "x": 22 * TILE,
            "y": 12 * TILE,
            "width": 12 * TILE,
            "height": 11 * TILE,
            "visible": True,
            "properties": [{"name": "nome", "type": "string", "value": "Aquário (privado)"}],
        }
    )

    def layer(name, data, visible=True, opacity=1.0):
        return {
            "id": abs(hash(name)) % 10000,
            "name": name,
            "type": "tilelayer",
            "width": MAP_W,
            "height": MAP_H,
            "x": 0,
            "y": 0,
            "visible": visible,
            "opacity": opacity,
            "data": data,
        }

    mapa = {
        "compressionlevel": -1,
        "height": MAP_H,
        "width": MAP_W,
        "tilewidth": TILE,
        "tileheight": TILE,
        "infinite": False,
        "orientation": "orthogonal",
        "renderorder": "right-down",
        "type": "map",
        "version": "1.10",
        "tiledversion": "1.10.2",
        "nextlayerid": 10,
        "nextobjectid": oid + 2,
        "tilesets": [
            {
                "firstgid": 1,
                "columns": 16,
                "image": "../tiles/office-16.png",
                "imageheight": ((len(name_to_id) + 15) // 16) * CELL,
                "imagewidth": 16 * CELL,
                "margin": 1,
                "spacing": 2,
                "name": "office-16",
                "tilecount": len(name_to_id),
                "tileheight": 16,
                "tilewidth": 16,
            }
        ],
        "layers": [
            layer("Chao", chao),
            layer("Paredes", paredes),
            layer("Moveis-abaixo", mov_ab),
            layer("Moveis-acima", mov_ac),
            layer("Colisao", col, visible=False, opacity=0),
            {
                "id": 9,
                "name": "Objetos",
                "type": "objectgroup",
                "objects": objects,
                "visible": True,
                "opacity": 1,
                "x": 0,
                "y": 0,
            },
        ],
    }
    mdir = ASSETS / "map"
    mdir.mkdir(parents=True, exist_ok=True)
    (mdir / "escritorio.json").write_text(json.dumps(mapa), encoding="utf-8")

    def csv(data):
        rows = []
        for y in range(MAP_H):
            rows.append(",".join(str(data[y * MAP_W + x]) for x in range(MAP_W)))
        return ",\n".join(rows)

    tmx = f"""<?xml version="1.0" encoding="UTF-8"?>
<map version="1.10" tiledversion="1.10.2" orientation="orthogonal" renderorder="right-down" width="{MAP_W}" height="{MAP_H}" tilewidth="{TILE}" tileheight="{TILE}" infinite="0" nextlayerid="10" nextobjectid="{oid + 2}">
 <tileset firstgid="1" source="../tiles/office-16.tsx"/>
 <layer id="1" name="Chao" width="{MAP_W}" height="{MAP_H}">
  <data encoding="csv">
{csv(chao)}
</data>
 </layer>
 <layer id="2" name="Paredes" width="{MAP_W}" height="{MAP_H}">
  <data encoding="csv">
{csv(paredes)}
</data>
 </layer>
 <layer id="3" name="Moveis-abaixo" width="{MAP_W}" height="{MAP_H}">
  <data encoding="csv">
{csv(mov_ab)}
</data>
 </layer>
 <layer id="4" name="Moveis-acima" width="{MAP_W}" height="{MAP_H}">
  <data encoding="csv">
{csv(mov_ac)}
</data>
 </layer>
 <layer id="5" name="Colisao" width="{MAP_W}" height="{MAP_H}" visible="0" opacity="0">
  <data encoding="csv">
{csv(col)}
</data>
 </layer>
</map>
"""
    (mdir / "escritorio.tmx").write_text(tmx, encoding="utf-8")
    (mdir / "escritorio.tiled-project").write_text(
        json.dumps({"automappingRulesFile": "", "commands": [], "tilesets": []}, indent=2),
        encoding="utf-8",
    )
    print("map written")


def write_meta() -> None:
    lic = ASSETS / "LICENSES"
    lic.mkdir(parents=True, exist_ok=True)
    (lic / "kenney-CC0.txt").write_text(
        "Kenney RPG Urban Pack + Roguelike Indoors — CC0\nhttps://kenney.nl/\n", encoding="utf-8"
    )
    (lic / "2dpig-CC0.txt").write_text(
        "2dPig Pixel Office — CC0\nhttps://2dpig.itch.io/pixel-office\n", encoding="utf-8"
    )
    (lic / "ninja-adventure-CC0.txt").write_text(
        "Pixel-Boy & AAA — Ninja Adventure — CC0\nhttps://pixel-boy.itch.io/ninja-adventure-asset-pack\n",
        encoding="utf-8",
    )
    credits = """# Credits — Escritório virtual

| Pack | Autor | URL | Licença |
|------|-------|-----|---------|
| RPG Urban Pack | Kenney | https://kenney.nl/assets/rpg-urban-pack | CC0 |
| Roguelike Indoors | Kenney | https://kenney.nl/assets/roguelike-indoors | CC0 |
| Pixel Office | 2dPig | https://2dpig.itch.io/pixel-office | CC0 |
| Ninja Adventure | Pixel-Boy & AAA | https://pixel-boy.itch.io/ninja-adventure-asset-pack | CC0 |
| Phaser 3.80.x | Photon Storm | MIT | |
| EasyStar.js 0.4.4 | Bryce Neal | MIT | |

## Personagens (12 bases distintas, sem recolor)

Ver `chars/cast.json`. Gideon = Master + coroa 6×6 dourada.

## Animação type

Frames `{slug}_type_0` / `{slug}_type_1`: idle-up com deslocamento de 1 px na cabeça (não são frames de walk).
"""
    (ASSETS / "CREDITS.md").write_text(credits, encoding="utf-8")
    (ASSETS / "manifest.json").write_text(
        json.dumps(
            {
                "tiles": {"atlas": "tiles/office-16.png", "tsx": "tiles/office-16.tsx"},
                "map": "map/escritorio.json",
                "chars": {"atlas": "chars/agents.png", "json": "chars/agents.json"},
                "bubbles": {"atlas": "ui/bubbles.png", "json": "ui/bubbles.json"},
                "tileSize": 16,
                "mapWidth": MAP_W,
                "mapHeight": MAP_H,
            },
            indent=2,
        ),
        encoding="utf-8",
    )


def main() -> None:
    DL.mkdir(parents=True, exist_ok=True)
    pig, ninja = require_itch_zips()
    for name, url in KENNEY_URLS.items():
        fetch(url, DL / name)
    urban = DL / "kenney_rpg-urban-pack.zip"
    indoors = DL / "kenney_roguelike-indoors.zip"
    update_lock([urban, indoors, pig, ninja])

    if ASSETS.exists():
        # keep structure; overwrite outputs
        pass

    name_to_id = build_tiles(urban, indoors, pig)
    build_chars(ninja, urban)
    build_bubbles()
    build_map(name_to_id)
    write_meta()

    total = sum(f.stat().st_size for f in ASSETS.rglob("*") if f.is_file())
    print(f"assets total {total} bytes ({total/1024:.1f} KB) budget 2MB")


if __name__ == "__main__":
    main()
