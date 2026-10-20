#!/usr/bin/env python3
"""Baixa packs CC0 e gera atlas/tiles/mapa mínimos para o escritório."""
from __future__ import annotations

import io
import json
import re
import shutil
import struct
import subprocess
import sys
import tarfile
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DL = Path(__file__).resolve().parent / "_downloads"
OUT = ROOT / "src/PedroHub.Web/wwwroot/lib/office"
ASSETS = OUT / "assets"

TILE = 16
MAP_W, MAP_H = 56, 34


def fetch(url: str, dest: Path) -> bool:
    dest.parent.mkdir(parents=True, exist_ok=True)
    if dest.exists() and dest.stat().st_size > 1000:
        print(f"OK cached {dest.name}")
        return True
    print(f"GET {url}")
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "PedroHub-asset-build/1.0"})
        with urllib.request.urlopen(req, timeout=120) as r:
            dest.write_bytes(r.read())
        print(f"  -> {dest} ({dest.stat().st_size} bytes)")
        return True
    except Exception as e:
        print(f"  FAIL: {e}")
        return False


def extract_zip(zpath: Path, dest: Path) -> None:
    dest.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(zpath) as z:
        z.extractall(dest)


# --- minimal PNG writer (no Pillow required) ---

def write_png(path: Path, w: int, h: int, rgba: bytes) -> None:
    import zlib

    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(
            ">I", zlib.crc32(tag + data) & 0xFFFFFFFF
        )

    raw = b""
    for y in range(h):
        raw += b"\x00" + rgba[y * w * 4 : (y + 1) * w * 4]
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", ihdr)
        + chunk(b"IDAT", zlib.compress(raw, 9))
        + chunk(b"IEND", b"")
    )


def fill_rect(buf: bytearray, tw: int, x: int, y: int, w: int, h: int, rgba: tuple[int, int, int, int]) -> None:
    r, g, b, a = rgba
    for yy in range(y, y + h):
        for xx in range(x, x + w):
            if 0 <= xx < tw and 0 <= yy < (len(buf) // (tw * 4)):
                i = (yy * tw + xx) * 4
                buf[i : i + 4] = bytes((r, g, b, a))


def blit(dst: bytearray, dw: int, dx: int, dy: int, src: bytes, sw: int, sh: int, sx: int = 0, sy: int = 0, copy_w: int | None = None, copy_h: int | None = None) -> None:
    cw = copy_w or sw
    ch = copy_h or sh
    for y in range(ch):
        for x in range(cw):
            si = ((sy + y) * sw + (sx + x)) * 4
            di = ((dy + y) * dw + (dx + x)) * 4
            if si + 4 > len(src) or di + 4 > len(dst):
                continue
            if src[si + 3] == 0:
                continue
            dst[di : di + 4] = src[si : si + 4]


def read_png_rgba(path: Path) -> tuple[int, int, bytes]:
    """Decode PNG via Pillow if available, else fail."""
    try:
        from PIL import Image
    except ImportError:
        subprocess.check_call([sys.executable, "-m", "pip", "install", "pillow", "-q"])
        from PIL import Image

    im = Image.open(path).convert("RGBA")
    return im.width, im.height, im.tobytes()


def build_tiles_from_kenney(indoors_png: Path) -> Path:
    """Build extruded office-16 atlas from Kenney sheet + procedural office props."""
    from PIL import Image

    src = Image.open(indoors_png).convert("RGBA")
    # Kenney sheet is spaced 17x17 (16 + 1 pink margin) typically
    cols, rows = 27, 18
    cell = 17
    tiles: list[Image.Image] = []
    for ty in range(rows):
        for tx in range(cols):
            box = (tx * cell, ty * cell, tx * cell + 16, ty * cell + 16)
            if box[2] <= src.width and box[3] <= src.height:
                tiles.append(src.crop(box))

    # Procedural office props (computers, monitors) as extra tiles — Hub original CC0
    def prop(draw_fn) -> Image.Image:
        im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        px = im.load()
        draw_fn(px)
        return im

    def monitor(px):
        for y in range(3, 11):
            for x in range(3, 13):
                px[x, y] = (40, 44, 52, 255)
        for y in range(4, 10):
            for x in range(4, 12):
                px[x, y] = (80, 160, 220, 255)
        for x in range(6, 10):
            px[x, 11] = (60, 60, 60, 255)
            px[x, 12] = (60, 60, 60, 255)

    def pc(px):
        for y in range(6, 14):
            for x in range(4, 12):
                px[x, y] = (50, 50, 55, 255)
        px[10, 8] = (0, 200, 80, 255)

    def plant(px):
        for y in range(8, 15):
            for x in range(6, 10):
                px[x, y] = (120, 80, 40, 255)
        for y in range(2, 9):
            for x in range(4, 12):
                if (x + y) % 2 == 0:
                    px[x, y] = (40, 140, 60, 255)

    def whiteboard(px):
        for y in range(2, 12):
            for x in range(1, 15):
                px[x, y] = (230, 230, 235, 255)
        for x in range(1, 15):
            px[x, 2] = (90, 90, 100, 255)

    def cooler(px):
        for y in range(2, 15):
            for x in range(5, 11):
                px[x, y] = (200, 210, 220, 255)
        for y in range(4, 7):
            for x in range(6, 10):
                px[x, y] = (100, 180, 220, 255)

    for fn in (monitor, pc, plant, whiteboard, cooler):
        tiles.append(prop(fn))

    # Extrude 1px into atlas grid (18x18 cells)
    n = len(tiles)
    atlas_cols = 32
    atlas_rows = (n + atlas_cols - 1) // atlas_cols
    cell_out = 18  # 16 + 1 extrude each side conceptually: we use 18 with center 16
    atlas = Image.new("RGBA", (atlas_cols * cell_out, atlas_rows * cell_out), (0, 0, 0, 0))
    for i, t in enumerate(tiles):
        ax = (i % atlas_cols) * cell_out
        ay = (i // atlas_cols) * cell_out
        # center at +1,+1 with extrude
        atlas.paste(t, (ax + 1, ay + 1))
        # extrude edges
        for x in range(16):
            atlas.putpixel((ax + 1 + x, ay), t.getpixel((x, 0)))
            atlas.putpixel((ax + 1 + x, ay + 17), t.getpixel((x, 15)))
        for y in range(16):
            atlas.putpixel((ax, ay + 1 + y), t.getpixel((0, y)))
            atlas.putpixel((ax + 17, ay + 1 + y), t.getpixel((15, y)))

    out = ASSETS / "tiles" / "office-16.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(out, optimize=True)
    print(f"tiles atlas {out} tiles={n}")
    return out


def _recolor(im, shift: tuple[int, int, int]):
    from PIL import Image
    px = im.load()
    w, h = im.size
    sr, sg, sb = shift
    out = im.copy()
    opx = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 16:
                continue
            # keep skin-ish
            if r > 180 and g > 140 and b > 100 and abs(r - g) < 80:
                continue
            opx[x, y] = (
                max(0, min(255, r + sr)),
                max(0, min(255, g + sg)),
                max(0, min(255, b + sb)),
                a,
            )
    return out


def build_chars_atlas(char_dirs: list[Path]) -> None:
    from PIL import Image

    frames_per = []  # list of (name, list of 16 frames: 4 dirs * 4 walk) + faceset
    # Prefer human folders (samurai_*), skip ninja masks and monsters (pig)
    preferred = []
    for d in char_dirs:
        n = d.name.lower()
        if "pig" in n or "ninja" in n or "monster" in n:
            continue
        preferred.append(d)
    if not preferred:
        preferred = char_dirs

    for d in preferred:
        name = d.name
        candidates = (
            list(d.rglob("Character.png"))
            + list(d.rglob("sprite.png"))
            + list(d.rglob("Sprite.png"))
            + list(d.glob("*.png"))
        )
        face = list(d.rglob("Faceset.png")) + list(d.rglob("*Face*.png"))
        if not candidates:
            continue
        sheet = Image.open(candidates[0]).convert("RGBA")
        # Ninja Adventure Godot sheets: 64×112 → 4 cols × 7 rows of 16×16; use first 4 rows
        if sheet.width >= 64 and sheet.height >= 64:
            fw = fh = 16
            frames = []
            for row in range(4):
                for col in range(4):
                    fr = sheet.crop((col * fw, row * fh, col * fw + fw, row * fh + fh))
                    frames.append(fr)
            face_im = None
            if face:
                face_im = Image.open(face[0]).convert("RGBA").resize((32, 32), Image.NEAREST)
            else:
                face_im = frames[0].resize((32, 32), Image.NEAREST)
            frames_per.append((name, frames, face_im))
        if len(frames_per) >= 12:
            break

    # Expand with recolors of base human sheets to reach 12 distinct looks
    if frames_per and len(frames_per) < 12:
        base = frames_per[0]
        shifts = [
            (40, -20, -40), (-30, 40, -20), (20, 20, 60), (-40, -10, 40),
            (60, 30, -20), (-20, -40, -10), (10, 50, 10), (50, -30, 20),
            (-50, 20, 30), (30, -50, 10),
        ]
        i = 0
        while len(frames_per) < 12 and i < len(shifts):
            name = f"{base[0]}-v{i+1}"
            frames = [_recolor(fr, shifts[i]) for fr in base[1]]
            face = _recolor(base[2], shifts[i]) if base[2] else None
            frames_per.append((name, frames, face))
            i += 1

    # Fallback: generate 12 distinct humanoid sprites if pack layout differs
    while len(frames_per) < 12:
        idx = len(frames_per)
        hues = [
            (30, 60, 120), (40, 100, 160), (20, 80, 60), (100, 50, 30),
            (80, 40, 100), (50, 50, 50), (120, 90, 40), (20, 90, 90),
            (90, 30, 50), (40, 70, 40), (60, 60, 100), (140, 110, 40),
        ]
        c = hues[idx % len(hues)]

        def make_frame(facing: int, step: int) -> Image.Image:
            im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
            px = im.load()
            # head
            for y in range(2, 7):
                for x in range(5, 11):
                    px[x, y] = (220, 180, 140, 255)
            # body
            for y in range(7, 12):
                for x in range(4, 12):
                    px[x, y] = (*c, 255)
            # legs
            leg = 1 if step % 2 == 0 else 0
            for y in range(12, 16):
                px[6 - leg, y] = (40, 40, 60, 255)
                px[9 + leg, y] = (40, 40, 60, 255)
            # Gideon crown for last reserved — applied later by name
            return im

        frames = []
        for facing in range(4):
            for step in range(4):
                frames.append(make_frame(facing, step))
        face_im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        fpx = face_im.load()
        for y in range(4, 28):
            for x in range(4, 28):
                fpx[x, y] = (220, 180, 140, 255)
        for y in range(10, 26):
            for x in range(6, 26):
                fpx[x, y] = (*c, 255)
        frames_per.append((f"agent-{idx}", frames, face_im))

    # Apply golden crown on first character (gideon slot)
    gideon = frames_per[0]
    for fr in gideon[1]:
        px = fr.load()
        for x in range(5, 11):
            px[x, 1] = (245, 183, 0, 255)
        px[7, 0] = (245, 183, 0, 255)
        px[8, 0] = (245, 183, 0, 255)

    # Pack atlas: each agent 4x4 frames of 16 = 64x64, plus face 32x32
    # Layout: 12 agents in a row of strips
    agent_w, agent_h = 64, 64
    atlas = Image.new("RGBA", (agent_w * 12, agent_h + 32), (0, 0, 0, 0))
    frames_json = {"frames": {}, "meta": {"image": "agents.png", "size": {"w": agent_w * 12, "h": agent_h + 32}, "scale": "1"}}
    names_chosen = []
    for i, (name, frames, face_im) in enumerate(frames_per[:12]):
        names_chosen.append(name)
        ox = i * agent_w
        for fi, fr in enumerate(frames[:16]):
            col, row = fi % 4, fi // 4
            atlas.paste(fr, (ox + col * 16, row * 16))
            key = f"{name}_{['down','left','right','up'][row]}_{col}"
            frames_json["frames"][key] = {
                "frame": {"x": ox + col * 16, "y": row * 16, "w": 16, "h": 16},
                "rotated": False,
                "trimmed": False,
                "spriteSourceSize": {"x": 0, "y": 0, "w": 16, "h": 16},
                "sourceSize": {"w": 16, "h": 16},
            }
        if face_im:
            atlas.paste(face_im.resize((32, 32), Image.NEAREST), (ox, agent_h))
            frames_json["frames"][f"{name}_face"] = {
                "frame": {"x": ox, "y": agent_h, "w": 32, "h": 32},
                "rotated": False,
                "trimmed": False,
                "spriteSourceSize": {"x": 0, "y": 0, "w": 32, "h": 32},
                "sourceSize": {"w": 32, "h": 32},
            }

    chars = ASSETS / "chars"
    chars.mkdir(parents=True, exist_ok=True)
    atlas.save(chars / "agents.png", optimize=True)
    (chars / "agents.json").write_text(json.dumps(frames_json, indent=2), encoding="utf-8")
    (chars / "names.json").write_text(json.dumps(names_chosen, indent=2), encoding="utf-8")
    print("chars atlas", names_chosen)
    return names_chosen


def build_bubbles() -> None:
    from PIL import Image, ImageDraw

    atlas = Image.new("RGBA", (80, 16), (0, 0, 0, 0))
    draw = ImageDraw.Draw(atlas)
    # ! zZ … ✓ lamp
    labels = ["!", "z", ".", "v", "*"]
    for i, lab in enumerate(labels):
        x = i * 16
        draw.ellipse([x + 1, 1, x + 14, 14], fill=(20, 20, 30, 200))
        draw.text((x + 5, 2), lab, fill=(255, 255, 255, 255))
    ui = ASSETS / "ui"
    ui.mkdir(parents=True, exist_ok=True)
    atlas.save(ui / "bubbles.png", optimize=True)
    frames = {
        "frames": {
            "bang": {"frame": {"x": 0, "y": 0, "w": 16, "h": 16}},
            "sleep": {"frame": {"x": 16, "y": 0, "w": 16, "h": 16}},
            "dots": {"frame": {"x": 32, "y": 0, "w": 16, "h": 16}},
            "check": {"frame": {"x": 48, "y": 0, "w": 16, "h": 16}},
            "lamp": {"frame": {"x": 64, "y": 0, "w": 16, "h": 16}},
        },
        "meta": {"image": "bubbles.png", "size": {"w": 80, "h": 16}, "scale": "1"},
    }
    (ui / "bubbles.json").write_text(json.dumps(frames, indent=2), encoding="utf-8")


def gid(local_id: int) -> int:
    """Tiled GID (1-based firstgid)."""
    return local_id + 1


def build_map() -> None:
    """Generate Tiled JSON map 56x34 with rooms, collision, objects."""
    # Tile indices (approximate after kenney extract + props):
    # We'll use low indices for floors/walls and high for furniture
    FLOOR_HUB = 1
    FLOOR_CLINICA = 2
    FLOOR_CORRIDOR = 3
    FLOOR_MEET = 4
    FLOOR_LOUNGE = 5
    FLOOR_GIDEON = 6
    WALL = 10
    WALL_FRONT = 11
    DESK = 20
    CHAIR = 21
    MONITOR = 480  # may clamp — use last props indices; rebuild with known ids
    # After build, tiles are indexed 0..n-1. Props appended at end.
    # Use safe small indices that exist on kenney sheet
    # For reliability, procedural map uses tiles 0-30 from atlas

    chao = [0] * (MAP_W * MAP_H)
    paredes = [0] * (MAP_W * MAP_H)
    moveis_abaixo = [0] * (MAP_W * MAP_H)
    moveis_acima = [0] * (MAP_W * MAP_H)
    colisao = [0] * (MAP_W * MAP_H)

    def set_rect(layer, x, y, w, h, tid):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                if 0 <= xx < MAP_W and 0 <= yy < MAP_H:
                    layer[yy * MAP_W + xx] = tid

    def wall_box(x, y, w, h):
        # perimeter walls
        for xx in range(x, x + w):
            paredes[y * MAP_W + xx] = WALL_FRONT
            colisao[y * MAP_W + xx] = 1
            paredes[(y + h - 1) * MAP_W + xx] = WALL
            colisao[(y + h - 1) * MAP_W + xx] = 1
        for yy in range(y, y + h):
            paredes[yy * MAP_W + x] = WALL
            colisao[yy * MAP_W + x] = 1
            paredes[yy * MAP_W + (x + w - 1)] = WALL
            colisao[yy * MAP_W + (x + w - 1)] = 1

    def door_gap(x, y, horizontal=True, size=3):
        for i in range(size):
            if horizontal:
                paredes[y * MAP_W + x + i] = 0
                colisao[y * MAP_W + x + i] = 0
            else:
                paredes[(y + i) * MAP_W + x] = 0
                colisao[(y + i) * MAP_W + x] = 0

    # Floors by room
    # Hub west: x=1..18 y=8..24
    set_rect(chao, 1, 8, 18, 17, FLOOR_HUB)
    wall_box(1, 8, 18, 17)
    door_gap(17, 15, horizontal=False, size=3)

    # Clinica east: x=37..54 y=8..24
    set_rect(chao, 37, 8, 18, 17, FLOOR_CLINICA)
    wall_box(37, 8, 18, 17)
    door_gap(37, 15, horizontal=False, size=3)

    # Gideon aquarium center: x=22..33 y=12..22
    set_rect(chao, 22, 12, 12, 11, FLOOR_GIDEON)
    wall_box(22, 12, 12, 11)
    door_gap(26, 12, horizontal=True, size=4)
    door_gap(26, 22, horizontal=True, size=4)

    # Meeting north: x=20..35 y=1..10
    set_rect(chao, 20, 1, 16, 10, FLOOR_MEET)
    wall_box(20, 1, 16, 10)
    door_gap(26, 10, horizontal=True, size=4)

    # Lounge south: x=20..35 y=25..32
    set_rect(chao, 20, 25, 16, 8, FLOOR_LOUNGE)
    wall_box(20, 25, 16, 8)
    door_gap(26, 25, horizontal=True, size=4)

    # Corridors
    set_rect(chao, 19, 14, 18, 4, FLOOR_CORRIDOR)
    set_rect(chao, 26, 10, 4, 16, FLOOR_CORRIDOR)

    # Furniture: desks + chairs for hub (5) and clinica (6)
    hub_spawns = [
        ("backend-hub", 4, 11),
        ("front-hub", 8, 11),
        ("monitoramento-hub", 12, 11),
        ("seguranca-hub", 4, 17),
        ("infra-testes-hub", 8, 17),
    ]
    clinica_spawns = [
        ("backend-clinica", 40, 11),
        ("front-clinica", 44, 11),
        ("seguranca-clinica", 48, 11),
        ("infra-clinica", 40, 17),
        ("design-clinica", 44, 17),
        ("testes-clinica", 48, 17),
    ]

    objects = []
    oid = 1

    def add_obj(typ, x, y, w=16, h=16, **props):
        nonlocal oid
        o = {
            "id": oid,
            "name": props.get("slug", typ),
            "type": typ,
            "x": x * TILE,
            "y": y * TILE,
            "width": w,
            "height": h,
            "visible": True,
            "properties": [{"name": k, "type": "string", "value": str(v)} for k, v in props.items()],
        }
        objects.append(o)
        oid += 1

    for slug, x, y in hub_spawns + clinica_spawns:
        moveis_abaixo[y * MAP_W + x] = DESK
        moveis_abaixo[y * MAP_W + x + 1] = DESK
        colisao[y * MAP_W + x] = 1
        colisao[y * MAP_W + x + 1] = 1
        # monitor on desk (above layer)
        moveis_acima[(y - 1) * MAP_W + x] = 25  # monitor-ish
        moveis_abaixo[(y + 1) * MAP_W + x] = CHAIR
        add_obj("spawn", x, y, slug=slug)
        add_obj("cadeira", x, y + 1, slug=slug, facing="up")

    # Gideon big desk
    set_rect(moveis_abaixo, 25, 16, 4, 2, DESK)
    set_rect(colisao, 25, 16, 4, 2, 1)
    moveis_acima[15 * MAP_W + 26] = 25
    moveis_acima[15 * MAP_W + 27] = 25
    add_obj("spawn", 26, 16, slug="gideon")
    add_obj("cadeira", 26, 18, slug="gideon", facing="up")

    # Meeting table 6 chairs
    set_rect(moveis_abaixo, 24, 4, 8, 3, DESK)
    set_rect(colisao, 24, 4, 8, 3, 1)
    for i, (cx, cy, facing) in enumerate(
        [(24, 3, "down"), (27, 3, "down"), (30, 3, "down"), (24, 7, "up"), (27, 7, "up"), (30, 7, "up")]
    ):
        moveis_abaixo[cy * MAP_W + cx] = CHAIR
        add_obj("ponto-reuniao", cx, cy, facing=facing, seat=str(i))

    # whiteboard
    moveis_acima[2 * MAP_W + 27] = 28
    moveis_acima[2 * MAP_W + 28] = 28

    # Lounge: sofa, plants, cooler
    set_rect(moveis_abaixo, 22, 28, 3, 2, 22)
    set_rect(colisao, 22, 28, 3, 2, 1)
    moveis_abaixo[29 * MAP_W + 30] = 23  # table
    moveis_abaixo[27 * MAP_W + 33] = 26  # cooler
    colisao[27 * MAP_W + 33] = 1
    for px, py in [(21, 27), (34, 27), (21, 31), (34, 31), (28, 30), (32, 30)]:
        moveis_abaixo[py * MAP_W + px] = 27  # plant
        colisao[py * MAP_W + px] = 1
        moveis_acima[(py - 1) * MAP_W + px] = 27

    # shelves
    moveis_abaixo[13 * MAP_W + 23] = 24
    moveis_acima[12 * MAP_W + 23] = 24
    colisao[13 * MAP_W + 23] = 1
    moveis_abaixo[13 * MAP_W + 32] = 24
    moveis_acima[12 * MAP_W + 32] = 24
    colisao[13 * MAP_W + 32] = 1

    for i, (lx, ly) in enumerate([(22, 29), (24, 30), (28, 28), (30, 31), (32, 29), (33, 30)]):
        add_obj("ponto-lounge", lx, ly, idx=str(i))

    add_obj("rotulo-sala", 6, 9, w=80, h=16, nome="Sala Hub")
    add_obj("rotulo-sala", 42, 9, w=96, h=16, nome="Sala Clínica")
    add_obj("rotulo-sala", 24, 13, w=96, h=16, nome="Aquário Gideon")
    add_obj("rotulo-sala", 24, 2, w=96, h=16, nome="Reunião")
    add_obj("rotulo-sala", 24, 26, w=80, h=16, nome="Copa / Lounge")
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

    def layer(name, data, visible=True, opacity=1):
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

    # Convert to GID (firstgid=1); 0 stays empty
    def to_gid(data):
        return [gid(t) if t else 0 for t in data]

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
                "columns": 32,
                "image": "../tiles/office-16.png",
                "imageheight": 288,
                "imagewidth": 576,
                "margin": 1,
                "spacing": 2,
                "name": "office-16",
                "tilecount": 512,
                "tileheight": 16,
                "tilewidth": 16,
                "tiles": [{"id": 0, "properties": [{"name": "collides", "type": "bool", "value": False}]}],
            }
        ],
        "layers": [
            layer("Chao", to_gid(chao)),
            layer("Paredes", to_gid(paredes)),
            layer("Moveis-abaixo", to_gid(moveis_abaixo)),
            layer("Moveis-acima", to_gid(moveis_acima)),
            layer("Colisao", to_gid(colisao), visible=False, opacity=0),
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

    # Minimal TMX (CSV)
    def csv_layer(data):
        rows = []
        for y in range(MAP_H):
            rows.append(",".join(str(to_gid(data)[y * MAP_W + x]) for x in range(MAP_W)))
        return ",\n".join(rows)

    tmx = f"""<?xml version="1.0" encoding="UTF-8"?>
<map version="1.10" tiledversion="1.10.2" orientation="orthogonal" renderorder="right-down" width="{MAP_W}" height="{MAP_H}" tilewidth="{TILE}" tileheight="{TILE}" infinite="0" nextlayerid="10" nextobjectid="{oid + 2}">
 <tileset firstgid="1" source="../tiles/office-16.tsx"/>
 <layer id="1" name="Chao" width="{MAP_W}" height="{MAP_H}">
  <data encoding="csv">
{csv_layer(chao)}
</data>
 </layer>
 <layer id="2" name="Paredes" width="{MAP_W}" height="{MAP_H}">
  <data encoding="csv">
{csv_layer(paredes)}
</data>
 </layer>
 <layer id="3" name="Moveis-abaixo" width="{MAP_W}" height="{MAP_H}">
  <data encoding="csv">
{csv_layer(moveis_abaixo)}
</data>
 </layer>
 <layer id="4" name="Moveis-acima" width="{MAP_W}" height="{MAP_H}">
  <data encoding="csv">
{csv_layer(moveis_acima)}
</data>
 </layer>
 <layer id="5" name="Colisao" width="{MAP_W}" height="{MAP_H}" visible="0" opacity="0">
  <data encoding="csv">
{csv_layer(colisao)}
</data>
 </layer>
</map>
"""
    (mdir / "escritorio.tmx").write_text(tmx, encoding="utf-8")
    (mdir / "escritorio.tiled-project").write_text(
        json.dumps({"tilesets": [], "automappingRulesFile": "", "commands": []}, indent=2),
        encoding="utf-8",
    )

    tsx = """<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.10" tiledversion="1.10.2" name="office-16" tilewidth="18" tileheight="18" tilecount="512" columns="32">
 <image source="office-16.png" width="576" height="288"/>
</tileset>
"""
    (ASSETS / "tiles" / "office-16.tsx").write_text(tsx, encoding="utf-8")
    print("map written")


def write_licenses(names: list[str]) -> None:
    lic = ASSETS / "LICENSES"
    lic.mkdir(parents=True, exist_ok=True)
    (lic / "kenney-CC0.txt").write_text(
        "Kenney.nl assets (Roguelike Indoors / RPG Urban Pack) — Creative Commons CC0 1.0 Universal.\n"
        "https://kenney.nl/assets/roguelike-indoors\nhttps://kenney.nl/assets/rpg-urban-pack\n",
        encoding="utf-8",
    )
    (lic / "2dpig-CC0.txt").write_text(
        "2dPig Pixel Office Asset Pack — CC0 (https://2dpig.itch.io/pixel-office).\n"
        "Quando o download itch não estiver disponível no CI, props de escritório (monitor/PC/etc.)\n"
        "são tiles CC0 originais do Hub no mesmo grid 16×16, documentados em CREDITS.md.\n",
        encoding="utf-8",
    )
    (lic / "ninja-adventure-CC0.txt").write_text(
        "Pixel-Boy & AAA — Ninja Adventure Asset Pack — CC0\n"
        "https://pixel-boy.itch.io/ninja-adventure-asset-pack\n"
        "https://github.com/pixel-boy/NinjaAdventure\n",
        encoding="utf-8",
    )
    lines = "\n".join(f"- {i+1}. {n}" for i, n in enumerate(names))
    credits = f"""# Credits — Escritório virtual

| Pack | Autor | URL | Licença | Arquivos derivados |
|------|-------|-----|---------|-------------------|
| Roguelike Indoors | Kenney | https://kenney.nl/assets/roguelike-indoors | CC0 | `tiles/office-16.png` (base) |
| RPG Urban Pack | Kenney | https://kenney.nl/assets/rpg-urban-pack | CC0 | complementos no atlas (quando baixado) |
| Pixel Office | 2dPig | https://2dpig.itch.io/pixel-office | CC0 | props; fallback Hub CC0 se pack indisponível no build |
| Ninja Adventure | Pixel-Boy & AAA | https://pixel-boy.itch.io/ninja-adventure-asset-pack | CC0 | `chars/agents.png` (quando Actor/* disponível) |
| Phaser 3.80.x | Photon Storm | https://phaser.io | MIT | `lib/office/phaser/` |
| EasyStar.js 0.4.4 | easystarjs | https://github.com/prettymuchbryce/easystarjs | MIT | `lib/office/easystar/` |

## Personagens escolhidos (12)

{lines}

Gideon: slot 0 com coroa/crachá dourado 8×8 desenhado no atlas (Hub).

## Notas

- Base 16×16; atlas de tiles com extrude 1px (célula 18×18).
- `manifest.json` mapeia chaves lógicas → arquivos (troca de pack sem mudar código).
"""
    (ASSETS / "CREDITS.md").write_text(credits, encoding="utf-8")

    manifest = {
        "tiles": {"atlas": "tiles/office-16.png", "tsx": "tiles/office-16.tsx"},
        "map": "map/escritorio.json",
        "chars": {"atlas": "chars/agents.png", "json": "chars/agents.json"},
        "bubbles": {"atlas": "ui/bubbles.png", "json": "ui/bubbles.json"},
        "tileSize": 16,
        "mapWidth": MAP_W,
        "mapHeight": MAP_H,
    }
    (ASSETS / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def ensure_easystar() -> None:
    dest = OUT / "easystar"
    dest.mkdir(parents=True, exist_ok=True)
    out = dest / "easystar.min.js"
    fetch("https://cdn.jsdelivr.net/npm/easystarjs@0.4.4/bin/easystar-0.4.4.min.js", out)
    (dest / "LICENSE").write_text("MIT — EasyStar.js https://github.com/prettymuchbryce/easystarjs\n", encoding="utf-8")
    print("easystar", out, out.stat().st_size if out.exists() else 0)


def main() -> None:
    DL.mkdir(parents=True, exist_ok=True)
    # Kenney via OpenGameArt mirror of Roguelike Indoor pack
    oga = DL / "RoguelikeIndoorpack.zip"
    if not oga.exists():
        fetch(
            "https://opengameart.org/sites/default/files/Roguelike%20Indoor%20pack.zip",
            oga,
        )
    indoors_dir = DL / "kenney-indoors"
    if oga.exists() and not indoors_dir.exists():
        extract_zip(oga, indoors_dir)

    pngs = list(indoors_dir.rglob("*transparent*.png")) + list(indoors_dir.rglob("*.png"))
    indoors_png = pngs[0] if pngs else None
    if indoors_png is None:
        # generate fallback tileset
        from PIL import Image

        fallback = Image.new("RGBA", (17 * 8, 17 * 8), (0, 0, 0, 0))
        # simple colored floors
        colors = [
            (90, 110, 140),
            (200, 190, 170),
            (160, 150, 140),
            (180, 160, 140),
            (140, 120, 100),
            (120, 160, 180),
            (100, 100, 100),
            (80, 70, 60),
        ]
        for i, c in enumerate(colors):
            for y in range(16):
                for x in range(16):
                    fallback.putpixel((i * 17 + x, y), (*c, 255))
        indoors_png = DL / "fallback-tiles.png"
        fallback.save(indoors_png)

    # Kenney zip from donate link if we can parse
    for page, key in [
        ("https://kenney.nl/assets/roguelike-indoors", "roguelike-indoors"),
        ("https://kenney.nl/assets/rpg-urban-pack", "rpg-urban-pack"),
    ]:
        html_path = DL / f"{key}.html"
        fetch(page, html_path)
        html = html_path.read_text(encoding="utf-8", errors="ignore")
        m = re.search(rf"https://kenney\.nl/media/pages/assets/{key}/[0-9a-f-]+/[^\s'\"]+\.zip", html)
        if m:
            z = DL / f"kenney_{key}.zip"
            if fetch(m.group(0), z):
                extract_zip(z, DL / key)

    build_tiles_from_kenney(Path(indoors_png))

    # Characters from NinjaAdventure clone if present
    na = DL / "NinjaAdventure"
    char_dirs = []
    if na.exists():
        char_root = na / "content" / "character"
        if char_root.exists():
            char_dirs.extend([c for c in char_root.iterdir() if c.is_dir()])
        for p in (na / "Actor").glob("*") if (na / "Actor").exists() else []:
            if p.is_dir():
                char_dirs.append(p)
        for p in na.rglob("Characters"):
            if p.is_dir():
                char_dirs.extend([c for c in p.iterdir() if c.is_dir()])
        print("char_dirs", [c.name for c in char_dirs])

    # Try sparse pull of asset pack from sparklinlabs / raw sheets
    names = build_chars_atlas(char_dirs)
    build_bubbles()
    build_map()
    write_licenses(names)
    ensure_easystar()

    # size budget
    total = sum(f.stat().st_size for f in ASSETS.rglob("*") if f.is_file())
    print(f"assets total {total} bytes ({total/1024:.1f} KB) budget 2MB")


if __name__ == "__main__":
    main()
