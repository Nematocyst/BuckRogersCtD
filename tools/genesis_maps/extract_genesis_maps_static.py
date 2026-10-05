"""Static extraction of every Genesis map straight from the ROM (no emulator).

The map loader (ROM 0x5766, called by the script opcode LOADFILES via 0x5734) opens ONE LZW stream at 0x8FA8D:
  [u8 0][u8 n] [n map ids] [n x 1024-byte map blocks]       (decoded size 2 + n + n*1024 = 18,452 for n = 18)
and decodes it sequentially 0x400 bytes at a time into work RAM 0xFFB5A4 until the id matches. The stream has
several 0x100 (CLEAR) segments, which is why a decode that stops at the first 0x100 only gives 1,697 bytes.

Block layout (DOS Gold Box GEO layout, 16 x 16 cells, index = y*16 + x, y grows south):
  plane 0  high nibble = wall type on the NORTH side, low nibble = EAST side
  plane 1  high nibble = SOUTH side,                  low nibble = WEST side
  plane 2  cell info: bit 7 = cell exists (0 = outside the map), low 7 bits = special/event code   [meaning open]
  plane 3  cell flags (only bits 0,2,4,6 and 7 are ever set)                                      [meaning open]
Wall types 0..14; 0 = open. Neighbouring planes agree (north of (y,x) == south of (y-1,x), east of (y,x) == west
of (y,x+1)) on 100% of edges in 16 of 18 maps, 99%/96% in maps 0x20/0x34.

usage: python extract_genesis_maps_static.py ROM.md OUTDIR
writes OUTDIR/genesis_maps.json and OUTDIR/previews/map_XX.png (needs numpy + Pillow for the PNGs)."""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from decoder2 import decode_stream

MAP_STREAM = 0x8FA8D
MAP_NAMES = {0x03: "Start / ship interior (module 03)", 0x10: "Chicagorg", 0x20: "Spy Ship", 0x23: "Asteroid Base",
             0x30: "Asteroid Base, Level 1", 0x31: "Asteroid Base, Level 2", 0x32: "Pirate Ship, Levels 1-5",
             0x34: "Pirate Ship, Levels 11-15", 0x41: "Desert Runner Village", 0x42: "Mars Base Gradiuvs Mons",
             0x43: "More Asteroid Bases", 0x51: "Lowlander Village, Venusian Space Elevator Ruins",
             0x52: "Venus RAM Base", 0x60: "Mercury Merchants Area", 0x61: "Mariposa Core",
             0x62: "Mercurian Finale, Weapons Control Level", 0x63: "Enemy Ships"}   # names: DOS ids, as in earlier notes


def load_maps(rom):
    segs, _ = decode_stream(rom, MAP_STREAM)
    d = b"".join(segs)
    n = d[1]
    ids = list(d[2:2 + n])
    assert len(d) == 2 + n + n * 1024, len(d)
    maps = []
    for i, mid in enumerate(ids):
        b = d[2 + n + i * 1024: 2 + n + (i + 1) * 1024]
        pl = [list(b[k * 256:(k + 1) * 256]) for k in range(4)]
        maps.append(dict(
            id=mid, name=MAP_NAMES.get(mid, ""), width=16, height=16,
            north=[v >> 4 for v in pl[0]], east=[v & 15 for v in pl[0]],
            south=[v >> 4 for v in pl[1]], west=[v & 15 for v in pl[1]],
            exists=[v >> 7 for v in pl[2]], special=[v & 0x7F for v in pl[2]], flags=pl[3],
            raw=b.hex()))
    return maps


def render(m, path, cell=28):
    from PIL import Image, ImageDraw
    img = Image.new("RGB", (16 * cell + 1, 16 * cell + 1), (235, 235, 235))
    g = ImageDraw.Draw(img)
    col = {0: None, 1: (0, 0, 0), 2: (40, 90, 220)}
    for y in range(16):
        for x in range(16):
            i = y * 16 + x
            x0, y0 = x * cell, y * cell
            if not m["exists"][i]:
                g.rectangle([x0, y0, x0 + cell, y0 + cell], fill=(190, 190, 190))
            elif m["special"][i]:
                g.text((x0 + 4, y0 + 8), f'{m["special"][i]:x}', fill=(160, 0, 0))
            for side, (a, b) in dict(north=((x0, y0), (x0 + cell, y0)), south=((x0, y0 + cell), (x0 + cell, y0 + cell)),
                                    west=((x0, y0), (x0, y0 + cell)), east=((x0 + cell, y0), (x0 + cell, y0 + cell))).items():
                t = m[side][i]
                if t:
                    g.line([a, b], fill=col.get(t, (230, 120, 0)), width=3 if t == 1 else 2)
    img.save(path)


if __name__ == "__main__":
    rom = open(sys.argv[1], "rb").read()
    out = sys.argv[2]
    os.makedirs(os.path.join(out, "previews"), exist_ok=True)
    maps = load_maps(rom)
    json.dump(dict(source="ROM stream 0x8FA8D", maps=maps), open(os.path.join(out, "genesis_maps.json"), "w"))
    for m in maps:
        try:
            render(m, os.path.join(out, "previews", f'map_{m["id"]:02X}.png'))
        except ImportError:
            break
    print(len(maps), "maps:", " ".join(f'{m["id"]:02X}' for m in maps))
