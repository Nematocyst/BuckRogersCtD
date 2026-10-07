"""Export the battlefield terrain art: the ROM's own map painter (0xFEF8) is run for every tile id with a model of the VDP. The fight screen builder 0x14D60 loads the tile piece of
the screen mode; the painter draws each cell (tile id & 0x7F) as the 3x3-tile block id*9 of that piece's tilemap (24x24 px), with palette lines 0-2 from colour memory.
Mode 4 = outdoor ground ([0x97DC] = 0xA2), mode 5 = indoor ([0x97DC] = 0xA8); the art is the same for every ground type of a mode (the screen builder loads it the same way for 13
types). Writes terrain_mode4.png, terrain_mode5.png and terrain_mode6.png: 16 x 8 ids, 24 px cells, id = row * 16 + column.  usage: python export_terrain.py ROM OUTDIR"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine
from vdp_model import VDP
from PIL import Image
rom = open(sys.argv[1], 'rb').read(); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
def rgb(w): return (((w >> 1) & 7) * 36, ((w >> 5) & 7) * 36, ((w >> 9) & 7) * 36)
for mode, d97 in ((4, 0xA2), (5, 0xA8), (6, 0x00)):
    m = Machine(rom); v = VDP(m)
    m.write_ram(0x97DC, bytes([d97])); m.write_ram(0x97AD, bytes([1])); m.write_ram(0x9BBC, bytes([2]))
    kw = dict(max_insns=8_000_000, a4=0xC00004, a5=0xC00000)
    m.call(0x14D60, **kw)
    m.write_ram(0xCACA, bytes(list(range(128)) + [0] * (441 - 128)))
    for i in range(0xC000, 0xE000): v.vram[i] = 0
    m.call(0xFEF8, **kw)
    atlas = Image.new('RGB', (16 * 24, 8 * 24))
    for idn in range(128):
        row, col = divmod(idn, 21)
        for r in range(3):
            for c in range(3):
                a = 0xC000 + ((row * 3 + r) * 64 + col * 3 + c) * 2; wd = (v.vram[a] << 8) | v.vram[a + 1]
                t, hf, vf, pal = wd & 0x7FF, wd >> 11 & 1, wd >> 12 & 1, wd >> 13 & 3
                for y in range(8):
                    for x in range(8):
                        yy = 7 - y if vf else y; xx = 7 - x if hf else x
                        b = v.vram[t * 32 + yy * 4 + xx // 2]; n = (b >> 4) if xx % 2 == 0 else b & 15
                        atlas.putpixel(((idn % 16) * 24 + c * 8 + x, (idn // 16) * 24 + r * 8 + y), rgb(v.cram[pal * 16 + n]))
    atlas.save(f'{out}/terrain_mode{mode}.png')
print('terrain atlases ->', out)
