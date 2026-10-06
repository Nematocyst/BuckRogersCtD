"""Export the battlefield creature tokens (ROM table 0x9A14: 8-byte entries [32-bit pointer to an LZW piece][monster id][frames][anim set][extra], ended by a first byte of 0xFF).
Each piece holds 18 animation frames of the creature. Entry +5 is the tilemap BYTES per frame (the loader 0x9BB6 reads the tilemap in chunks of that size): 18 = 9 words =
3x3 tiles (24x24 px); 36 = 18 words for the creatures of record size type 2 (tall: 3 wide x 6 high tiles) and 3 (wide: 6 wide x 3 high); the draw routine 0xCC66 uses
exactly these sizes, and "36 <=> size type 2/3" holds for all 52 entries. Frames are row-major in the tilemap. The tile words carry palette line 0; the fight screen's line 0 is the
16-word system palette at ROM 0x9710 (written to colour memory by the fight screen builder 0x14D60 for every area type), which these sheets use.
party tokens: table 0x998C (12 sheets, same 18-frame 3x3 layout), written as party_<n>.png.
Transparent PNGs (colour index 0 = transparent) for the Unity viewer: put them in Assets/Resources/BuckRogers/tokens/.
usage: python export_tokens.py ROM OUTDIR [MONSTER_FILE.bytes]  -> token_<monster id>.png (18 frames in a row; the monster file gives each id's size type, default wide)"""
import sys, os, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine
from PIL import Image
rom = open(sys.argv[1], 'rb').read(); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
D = 0xFFFF9000
def colour(w): return (((w >> 1) & 7) * 36, ((w >> 5) & 7) * 36, ((w >> 9) & 7) * 36)
PAL = [colour(struct.unpack('>H', rom[0x9710 + 2 * i:0x9710 + 2 * i + 2])[0]) for i in range(16)]
size = {}
if len(sys.argv) > 3:
    mf = open(sys.argv[3], 'rb').read(); cnt = int.from_bytes(mf[:2], 'big'); r0 = 2 + cnt + (cnt & 1)
    size = {mf[2 + i]: mf[r0 + 214 * i + 0x23] for i in range(cnt)}

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
    fw = e[5] // 2                                   # words per frame
    cols, rows = (3, fw // 3) if fw == 9 or size.get(e[4]) == 2 else (6, 3)
    frames = len(words) // fw
    pw, ph = cols * 8, rows * 8
    im = Image.new('RGBA', (pw * frames, ph), (0, 0, 0, 0))
    for f in range(frames):
        for k in range(fw):
            wd = words[f * fw + k]; t, hf, vf = wd & 0x7FF, wd >> 11 & 1, wd >> 12 & 1
            for y in range(8):
                for x in range(8):
                    yy = 7 - y if vf else y; xx = 7 - x if hf else x
                    b = tiles[t * 32 + yy * 4 + xx // 2] if t < nt else 0
                    v = (b >> 4) if xx % 2 == 0 else b & 15
                    if v: im.putpixel((f * pw + (k % cols) * 8 + x, (k // cols) * 8 + y), PAL[v] + (255,))
    im.save(f'{out}/token_{e[4]:02d}.png')
print(n, 'creature token sheets ->', out)
# party tokens: ROM table 0x998C, 12 pieces (record byte +0x42 with bit 7 set: index = byte & 0x7F), always 3x3 frames
for k in range(12):
    nt, words, tiles = piece(struct.unpack('>I', rom[0x998C + 4 * k:0x998C + 4 * k + 4])[0])
    frames = len(words) // 9
    im = Image.new('RGBA', (24 * frames, 24), (0, 0, 0, 0))
    for f in range(frames):
        for kk in range(9):
            wd = words[f * 9 + kk]; t, hf, vf = wd & 0x7FF, wd >> 11 & 1, wd >> 12 & 1
            for y in range(8):
                for x in range(8):
                    yy = 7 - y if vf else y; xx = 7 - x if hf else x
                    b = tiles[t * 32 + yy * 4 + xx // 2] if t < nt else 0
                    v = (b >> 4) if xx % 2 == 0 else b & 15
                    if v: im.putpixel((f * 24 + (kk % 3) * 8 + x, (kk // 3) * 8 + y), PAL[v] + (255,))
    im.save(f'{out}/party_{k:02d}.png')
print('12 party token sheets ->', out)
