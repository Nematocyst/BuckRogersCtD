"""Vectors for the map preparation: LOADFILES' map load (0x5734) and NEWREGION's reload (0x574E), i.e. the LZW decode of a map plus the post-processing 0x57DA twice, with random region rectangles.
usage: python gen_maps_vectors.py ROM OUT.txt
One case per line: op id area colour n rect words(4n signed) | weighted sum of the 1,024 layer bytes"""
import sys, os, random, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0x57DA)
IDS = [3, 16, 17, 32, 35, 48, 49, 50, 52, 65, 66, 67, 81, 82, 96, 97, 98, 99]
lines = []
for n in range(int(os.environ.get('N_maps', 400))):
    m = Machine(rom); m.map_io()
    op = rnd.choice('LN'); mid = rnd.choice(IDS); area = rnd.randrange(0, 30); colour = rnd.choice([0, 0, 1, 2, 5, 9, 0xF])
    cnt = rnd.choice([0, 1, 1, 2, 3, 4])
    words = []
    for r in range(cnt):
        x1 = rnd.randrange(0, 14); y1 = rnd.randrange(0, 14); x2 = rnd.randrange(x1, 16); y2 = rnd.randrange(y1, 16)
        words += [x1 - 1, y1 - 1, x2 + 1, y2 + 1]
    # class table pointer: what 0x158BA stores in [0xB41E]
    w = struct.unpack('>h', rom[0x51836 + 2 * (area // 3):0x51836 + 2 * (area // 3) + 2])[0]
    m.write_ram(0xB41E, struct.pack('>I', 0x51836 + w + 0x10))
    m.write_ram(0x9BC5, bytes([colour])); m.write_ram(0x9BD5, bytes([cnt]))
    m.write_ram(0x9BD6, b''.join(struct.pack('>h', v) for v in words))
    if op == 'L': m.call(0x5734, d2=mid, max_insns=80_000_000)
    else:
        m.write_ram(0x9BD4, bytes([mid])); m.call(0x574E, d2=0, max_insns=80_000_000)
    mp = m.read_ram(0xB5A4, 1024)
    lines.append(' '.join(map(str, [op, mid, area, colour, cnt] + words)) + ' | ' + str(sum((i + 1) * b for i, b in enumerate(mp)) & 0xFFFFFFFF))
open(sys.argv[2], 'w').write('\n'.join(lines) + '\n')
print(len(lines), 'cases')
