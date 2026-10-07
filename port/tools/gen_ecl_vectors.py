"""ECL interpreter vectors: random synthetic scripts (control flow, arithmetic, compare / IF, SAVE, RANDOM, GETABLE, FOR, ONGOTO / ONGOSUB / GOSUB, word and memory operands, the
character windows of ROM 0x42E0) run by the ROM's own script engine (loop at 0x3300) in the emulator. Only opcodes without a screen side effect are used.
usage: python gen_ecl_vectors.py ROM OUT.txt.gz     one case per line: script hex | initial memory | final memory | flags | rng index | rng table sum   (see EclTests)"""
import sys, os, random, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE
rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0xEC1)
N = int(os.environ.get('N_ecl', 1500))
BASE = 0x6AF6
VARS = list(range(0x9E60, 0x9E80))
REGIONS = [(0x9E30, 0x9E90), (0x98E0, 0x98F0), (0xB9F0, 0xB9F4), (0xBA68, 0xBA68 + 3 * 0xD6), (0xC470, 0xC470 + 3 * 0x1A), (0x9DA7, 0x9DA8)]
ROMTABLES = [0x7310, 0x7318, 0x7A50, 0x7A57]   # tables inside the loaded module (work RAM at 0x6AF6.., the ROM engine reads them from RAM)
m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4); boot = m0.read_ram(0xD604, 512)

def B(v): return bytes([0, v & 255])
def Mm(a): return bytes([1, a & 255, a >> 8])
def Wi(v): return bytes([2, v & 255, (v >> 8) & 255])
def Aw(a): return bytes([3, a & 255, a >> 8])
def src():
    r = rnd.random()
    if r < 0.30: return B(rnd.choice([0, 1, 2, 3, 5, 7, 10, 127, 128, 200, 255, rnd.randrange(256)]))
    if r < 0.60: return Mm(rnd.choice(VARS))
    if r < 0.75: return Wi(rnd.choice([0, 1, 255, 256, 1000, 40000, 65535, rnd.randrange(65536)]))
    if r < 0.88: return Aw(rnd.choice(VARS[:-1]))
    if r < 0.94: return Mm(rnd.choice([0x9AFC + rnd.randrange(0x54), 0x9BF6 + rnd.randrange(0x1A)]))
    return Aw(rnd.choice([0x9AFC + rnd.randrange(0x53), 0x9BF6 + rnd.randrange(0x19)]))
def dst():
    r = rnd.random()
    if r < 0.60: return Mm(rnd.choice(VARS))
    if r < 0.85: return Aw(rnd.choice(VARS[:-1]))
    return Mm(rnd.choice([0x9AFC + rnd.randrange(0x54), 0x9BF6 + rnd.randrange(0x1A)]))

def simple():
    k = rnd.choice(['save', 'save', 'add', 'sub', 'mul', 'div', 'and', 'or', 'rand', 'get'])
    if k == 'save': return bytes([0x09]) + src() + dst()
    if k == 'rand': return bytes([0x08]) + src() + dst()
    if k == 'get':
        t = rnd.choice(ROMTABLES + [0x9E70, 0x9E40]); idx = rnd.choice([B(rnd.randrange(256)), Mm(rnd.choice(VARS))]); return bytes([0x2A]) + Mm(t) + idx + dst()   # byte indexes only: a word index can run the pointer into the engine's own variables (e.g. [0xD818])
    op = {'add': 4, 'sub': 5, 'div': 6, 'mul': 7, 'and': 0x2F, 'or': 0x30}[k]
    return bytes([op]) + src() + src() + dst()

def program():
    """returns the byte string (offset 0 = BASE+20 will be the entry)"""
    items = []          # (bytes or ('jump', op, label...)), labels as ints
    nlab = 0; labels = {}
    n = rnd.randrange(4, 22); subs = []
    i = 0
    while i < n:
        r = rnd.random()
        if r < 0.50: items.append(simple())
        elif r < 0.70:
            items.append(bytes([0x03]) + src() + src()); items.append(bytes([rnd.choice([0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B])])); items.append(simple()); i += 2
        elif r < 0.75:
            items.append(bytes([0x14]) + src() + src() + src() + src()); items.append(bytes([rnd.choice([0x16, 0x17, 0x18])])); items.append(simple()); i += 2
        elif r < 0.82 and not any(isinstance(x, tuple) and x[0] == 'for' for x in items[-6:]):
            items.append(bytes([0x46]) + B(rnd.randrange(0, 3)) + B(rnd.randrange(0, 4)))
            for _ in range(rnd.randrange(1, 3)): items.append(simple())
            items.append(bytes([0x47])); i += 3; items.append(('for',))
        elif r < 0.88:
            lab = nlab; nlab += 1; items.append(('goto', 0x01, [lab], None)); items.append(simple()); items.append(('label', lab)); i += 1
        elif r < 0.93:
            sub = len(subs); subs.append([simple() for _ in range(rnd.randrange(1, 4))]); items.append(('gosub', sub))
        elif r < 0.97:
            k = rnd.randrange(1, 4); labs = [nlab + j for j in range(k)]; nlab += k
            items.append(('ongoto', rnd.choice([0x25, 0x25, 0x26]), labs, src()))
            for j, lab in enumerate(labs): items.append(simple()); items.append(('label', lab))
            i += k
        else: items.append(bytes([0x1C]) if False else simple())
        i += 1
    items.append(bytes([0x00]))
    # subroutines after EXIT
    for si, body in enumerate(subs):
        items.append(('sublabel', si)); items.extend(body); items.append(bytes([0x13]))
    # layout
    def size(it):
        if isinstance(it, bytes): return len(it)
        if it[0] == 'goto': return 4
        if it[0] == 'gosub': return 4
        if it[0] == 'ongoto': return 1 + len(it[3]) + 2 + 3 * len(it[2])
        return 0
    pos = BASE + 20; lab = {}; sub = {}
    for it in items:
        if isinstance(it, tuple) and it[0] == 'label': lab[it[1]] = pos
        elif isinstance(it, tuple) and it[0] == 'sublabel': sub[it[1]] = pos
        pos += size(it)
    out = bytearray()
    for it in items:
        if isinstance(it, bytes): out += it
        elif it[0] == 'goto': out += bytes([0x01]) + Mm(lab[it[2][0]])
        elif it[0] == 'gosub': out += bytes([0x02]) + Mm(sub[it[1]])
        elif it[0] == 'ongoto': out += bytes([it[1]]) + it[3] + B(len(it[2])) + b''.join(Mm(lab[l]) for l in it[2])
    return bytes(out)

def region_bytes(m):
    return b''.join(m.read_ram(a, b - a) for a, b in REGIONS)

lines = []; skipped = 0
for k in range(N):
    prog = program(); code = b''.join(bytes([1]) + Mm(BASE + 20) for _ in range(5)) + prog + bytes(4)
    m = Machine(rom)
    for a, b in REGIONS: m.write_ram(a, bytes(rnd.randrange(256) if rnd.random() < 0.6 else rnd.choice([0, 1, 255]) for _ in range(b - a)))
    m.write_ram(0x9DA7, bytes([rnd.randrange(3)]))
    for t in (0x9E70, 0x9E40): m.write_ram(t, bytes(rnd.randrange(256) for _ in range(16)))
    m.write_ram(0xD604, boot); idx = rnd.randrange(256); m.write_ram(0xD804, bytes([idx]))
    m.write_ram(BASE, code)
    m.write_ram(0xB9AC, struct.pack('>I', 0xFFFFB9F0)); m.write_ram(0xBA59, b'\0'); m.write_ram(0xBA53, b'\0'); m.write_ram(0x9BB9, b'\0')
    m.write_ram(0xB9F2, b'\0')
    for a in (0x9E70, 0x9E40): pass
    init = region_bytes(m) + m.read_ram(0x9E70, 16) + m.read_ram(0x9E40, 16)
    try: m.call(0x3300, a2=RAM_BASE + BASE + 20, max_insns=400000)
    except Exception as e: skipped += 1; continue
    if m.reg('pc') != 0x00FFF000: skipped += 1; continue
    final = region_bytes(m)
    tsum = sum(struct.unpack('>256H', m.read_ram(0xD604, 512)))
    lines.append(' '.join([code.hex(), init.hex(), final.hex(), str(m.ram_byte(0xB9F2)), str(m.ram_byte(0xD804)), str(tsum), str(idx), ''.join('%02x' % 0 for _ in range(0))]))
import gzip
gzip.open(sys.argv[2], 'wt').write('\n'.join(lines) + '\n')

open(os.path.join(os.path.dirname(sys.argv[2]) or '.', 'ecl_boot_table.txt'), 'w').write(boot.hex())
print(len(lines), 'ecl cases,', skipped, 'skipped')
