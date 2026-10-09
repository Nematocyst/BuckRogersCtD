"""Vectors for the exploration commands STEPFORWARD (0x3EB2), STEPBACK (0x3EA4), HALFSTEP (0x3FA0), HALFBACK (0x3F88), UNLOCKDOOR (0x3EC4), HOWFAR (0x3CC6) with their screen helpers stubbed.
The four 256-byte map layers (0xB5A4..0xB8A3) are made from a seed by xorshift32 (the C# test uses the same generator), so only the seed and the results are stored.
usage: python gen_explore_vectors.py ROM OUT.txt
One case per line: op arg seed x y f b4c1 b4c7 kind | x y f 9af9 9af8 97e6 97e7 ba5c b4c1 b4c7 mapsum result"""
import sys, os, random
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0x53B6)
def xs(seed, n):
    out = []; s = seed or 1
    for _ in range(n):
        s ^= (s << 13) & 0xFFFFFFFF; s ^= s >> 17; s ^= (s << 5) & 0xFFFFFFFF; out.append(s & 0xFF)
    return out
UI = (0x574E, 0x81AC, 0x5AC2, 0x5566, 0xFF7C, 0xC134, 0x4E00, 0x5A8A, 0x1343E)
ENTRY = {'R': 0x3FB4, 'S': 0x3EB2, 'B': 0x3EA4, 'H': 0x3FA0, 'K': 0x3F88, 'U': 0x3EC4, 'W': 0x3CC6}
lines = []
for n in range(int(os.environ.get('N_explore', 1500))):
    m = Machine(rom)
    for a in UI: m.stub_rts(a)
    op = rnd.choice('SSSSBBHKUWWRR'); arg = rnd.randrange(4); seed = rnd.randrange(1, 2**32)
    x = rnd.choice(list(range(16)) * 3 + [0, 15, 16, 255]); y = rnd.choice(list(range(16)) * 3 + [0, 15, 16, 255]); f = rnd.randrange(4)
    b4c1 = rnd.choice([0, 0, 0xFF]); b4c7 = rnd.choice([0, 0, 1, 2, 5]); kind = rnd.choice([0, 0, 0, 0xA2, 0xA8, 0x40])
    maps = xs(seed, 1024)
    # make walls and doors likely enough to matter: thin the layers out
    walls = [v if (v & 0x30) != 0x30 else 0 for v in maps[:512]]
    maps = walls + maps[512:]
    m.write_ram(0xB5A4, bytes(maps)); m.write_ram(0x9AF7, bytes([x])); m.write_ram(0x9AF6, bytes([y])); m.write_ram(0x9AFA, bytes([f]))
    m.write_ram(0xB4C1, bytes([b4c1])); m.write_ram(0xB4C7, bytes([b4c7])); m.write_ram(0x97DC, bytes([kind]))
    m.write_ram(0xE000, bytes([0, 0, 0, arg]) if op == 'W' else b'\0\0\0\0')
    # HOWFAR: first operand is a memory destination 0x9E70 (type 1), second the direction (immediate)
    if op == 'W': m.write_ram(0xE000, bytes([1, 0x70, 0x9E, 0, arg]))
    if op == 'R':
        stream = xs(seed ^ 0x5A5A, 1 + 4 * arg); script = [0, stream[0], 0, arg]
        for v in stream[1:]: script += [0, v]
        m.write_ram(0xE000, bytes(script))
    m.call(ENTRY[op], a2=0x00FFE000, a3=0x336E, a6=RAM_BASE + 0x8000, max_insns=400000)
    mp = m.read_ram(0xB5A4, 1024)
    reg = m.read_ram(0x9BC5, 0x31)
    out = [m.ram_byte(a) for a in (0x9AF7, 0x9AF6, 0x9AFA, 0x9AF9, 0x9AF8, 0x97E6, 0x97E7, 0xBA5C, 0xB4C1, 0xB4C7)] + [sum((i + 1) * b for i, b in enumerate(mp)) & 0xFFFFFFFF, (m.ram_byte(0x9E70) if op == 'W' else 0), sum((i + 1) * b for i, b in enumerate(reg)) & 0xFFFFFFFF]
    lines.append(' '.join(map(str, [op, arg, seed, x, y, f, b4c1, b4c7, kind])) + ' | ' + ' '.join(map(str, out)))
open(sys.argv[2], 'w').write('\n'.join(lines) + '\n')
import collections
print(len(lines), 'cases', collections.Counter(l.split()[0] for l in lines))
