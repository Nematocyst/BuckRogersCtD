"""Export the battlefield creature tokens (ROM table 0x9A14: 8-byte entries [32-bit pointer to an LZW piece][monster id][frames][anim set][extra], ended by a first byte of 0xFF).
Each piece holds the creature's 3x3-tile frames (tilemap = 9 words per frame; entry +5 = 18 or 36 frames: walking, attacking, falling per facing; creatures of size
class 2/3 use bigger frames, which this tool slices as 3x3 anyway). The pieces carry no palette (the fight screen supplies it), so frames are written in grey.
usage: python export_tokens.py ROM OUTDIR  -> token_<monster id>.png (all frames in a row of 18)"""
import sys, os, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine
from PIL import Image
rom = open(sys.argv[1], 'rb').read(); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
D = 0xFFFF9000
GREY = [(0, 0, 0)] + [(17 * i + 10,) * 3 for i in range(1, 16)]

def piece(ptr):
    m = Machine(rom); m.call(0x9E76, a0=ptr)
    def get(n):
        if n == 0: return b''
        m.call(0x9ED8, d0=D, d1=n, max_insns=20_000_000); return m.read_ram(D, n)
    nt, tm, pm = struct.unpack('>HHH', get(6))
    tmap = get(tm)
    for k in range(4):
        if pm >> k & 1: get(0x20)
    return nt, [struct.unpack('>H', tmap[2 * i:2 * i + 2])[0] for i in range(tm // 2)], get(nt * 32)

n = 0
while rom[0x9A14 + 8 * n] != 0xFF:
    e = rom[0x9A14 + 8 * n:0x9A14 + 8 * n + 8]; n += 1
    nt, words, tiles = piece(struct.unpack('>I', e[:4])[0])
    frames = len(words) // 9
    im = Image.new('RGB', (24 * min(frames, 18), 24 * ((frames + 17) // 18)), (255, 0, 255))
    for f in range(frames):
        for k in range(9):
            wd = words[f * 9 + k]; t, hf, vf = wd & 0x7FF, wd >> 11 & 1, wd >> 12 & 1
            for y in range(8):
                for x in range(8):
                    yy = 7 - y if vf else y; xx = 7 - x if hf else x
                    b = tiles[t * 32 + yy * 4 + xx // 2] if t < nt else 0
                    v = (b >> 4) if xx % 2 == 0 else b & 15
                    if v: im.putpixel(((f % 18) * 24 + (k % 3) * 8 + x, (f // 18) * 24 + (k // 3) * 8 + y), GREY[v])
    im.save(f'{out}/token_{e[4]:02d}.png')
print(n, 'creature token sheets ->', out)
