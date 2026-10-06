"""Export the ROM's pictures with the ROM's own LZW decoder (emulator). A "piece" is an LZW stream: header [tiles u16][tilemap bytes u16][palette mask u16], the tilemap
(u16 words: tile index bits 0-10, h-flip bit 11, v-flip bit 12), 0x20-byte palettes (one per set mask bit), then 4bpp 8x8 tiles (32 bytes each).
 * table 0x51360: 57 encounter pictures, 11x11 tiles (88x88 px); tilemaps longer than 121 words hold further 121-word frames (written as <n>_frames.png)
 * table 0xF14F2: 185 icon pieces (mostly 3x3 tiles, no palette in the piece: written in grey)
usage: python export_pictures.py ROM OUTDIR"""
import sys, os, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine
from PIL import Image
rom = open(sys.argv[1], 'rb').read(); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
D = 0xFFFF9000          # output buffer: the decoder keeps its dictionary at 0xFFFF0000.., so the buffer must not overlap it

def piece(ptr):
    m = Machine(rom); m.call(0x9E76, a0=ptr)
    def get(n):
        if n == 0: return b''
        m.call(0x9ED8, d0=D, d1=n, max_insns=20_000_000); return m.read_ram(D, n)
    nt, tm, pm = struct.unpack('>HHH', get(6))
    tmap = get(tm); pals = [get(0x20) if pm >> k & 1 else None for k in range(4)]
    return nt, [struct.unpack('>H', tmap[2 * i:2 * i + 2])[0] for i in range(tm // 2)], pals, get(nt * 32)

def colour(w): return (((w >> 1) & 7) * 36, ((w >> 5) & 7) * 36, ((w >> 9) & 7) * 36)
GREY = [(0, 0, 0)] + [(17 * i + 10,) * 3 for i in range(1, 16)]

def draw(nt, words, cols, tiles, w):
    im = Image.new('RGB', (w * 8, len(words) // w * 8))
    for i, wd in enumerate(words):
        t, hf, vf = wd & 0x7FF, wd >> 11 & 1, wd >> 12 & 1
        for y in range(8):
            for x in range(8):
                yy = 7 - y if vf else y; xx = 7 - x if hf else x
                b = tiles[t * 32 + yy * 4 + xx // 2] if t < nt else 0
                v = (b >> 4) if xx % 2 == 0 else b & 15
                im.putpixel(((i % w) * 8 + x, (i // w) * 8 + y), cols[v] if v else (255, 0, 255))
    return im

n = 0
for k in range(57):
    ptr = struct.unpack('>I', rom[0x51360 + 4 * k:0x51360 + 4 * k + 4])[0]
    nt, words, pals, tiles = piece(ptr)
    cols = [colour(struct.unpack('>H', next(p for p in pals if p)[2 * i:2 * i + 2])[0]) for i in range(16)]
    frames = [words[f * 121:(f + 1) * 121] for f in range(len(words) // 121)]
    draw(nt, frames[0], cols, tiles, 11).save(f'{out}/picture_{k:02d}.png')
    if len(frames) > 1:
        strip = Image.new('RGB', (88 * len(frames), 88))
        for f, fw in enumerate(frames): strip.paste(draw(nt, fw, cols, tiles, 11), (88 * f, 0))
        strip.save(f'{out}/picture_{k:02d}_frames.png')
    n += 1
icons = 0
for k in range(185):
    ptr = struct.unpack('>I', rom[0xF14F2 + 4 * k:0xF14F2 + 4 * k + 4])[0]
    nt, words, pals, tiles = piece(ptr)
    if len(words) == 0: continue
    p = next((p for p in pals if p), None)
    cols = [colour(struct.unpack('>H', p[2 * i:2 * i + 2])[0]) for i in range(16)] if p else GREY
    w = next((c for c in (3, 9, 6, 4, 2, 1) if len(words) % c == 0 and (c * c == len(words) or c == 3)), 1) if len(words) == 9 else (6 if len(words) == 18 else 1)
    if len(words) in (9, 18): draw(nt, words, cols, tiles, 3 if len(words) == 9 else 6).save(f'{out}/icon_{k:03d}.png'); icons += 1
print(n, 'pictures,', icons, 'icons ->', out)
