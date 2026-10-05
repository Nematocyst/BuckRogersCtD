"""Combat test vectors from the real ROM code: octant bearing (0x15D36), armor selection incl. flank/rear/backstab (0x10666),
to-hit arithmetic tail (0x1056A-0x105F4).   usage: python gen_combat_vectors.py ROM OUT.json"""
import sys, json, random, struct, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(68000)
S32 = lambda v: ((v + 2**31) % 2**32) - 2**31
out = {}

# ---- 1. octant (0x15D36): d0 = target x, d1 = target y, d2 = attacker x, d3 = attacker y -> d0.w
m = Machine(rom)
oct_cases = []
pairs = [(dx, dy) for dx in range(-24, 25) for dy in range(-24, 25)]
for _ in range(2500):
    pairs.append((rnd.randrange(-300, 300), rnd.randrange(-300, 300)))
pairs += [(0, 0), (1, 0), (0, 1), (-1, 0), (0, -1), (2, 5), (5, 2), (100, 41), (100, 42), (41, 100), (42, 100)]
for dx, dy in pairs:
    tx, ty = 1000, 1000
    ax, ay = tx + dx, ty + dy
    m.call(0x15D36, d0=tx, d1=ty, d2=ax & 0xFFFF, d3=ay & 0xFFFF)
    oct_cases.append([tx, ty, ax, ay, m.reg('d0') & 0xFFFF])
out['octant'] = oct_cases

# ---- 2. armor selection (0x10666): whole routine, ad5a (graphics) stubbed
def build(m, A, T, att_slot, tgt_slot, att_rec_fields, tgt_rec_fields, mask):
    slots = 0xC470
    m.write_ram(slots + A * 26, bytes(att_slot)); m.write_ram(slots + T * 26, bytes(tgt_slot))
    for idx, fields in ((att_slot[2], att_rec_fields), (tgt_slot[2], tgt_rec_fields)):
        base = 0xBA68 + idx * 0xD6
        for off, val in fields.items(): m.write_ram(base + off, bytes([val]))
    m.write_ram(0xCA20, bytes([A])); m.write_ram(0xD513, bytes([T])); m.write_ram(0xD4FD, bytes([mask]))
    m.write_ram(0xD496, bytes([0x55])); m.write_ram(0xD518, b'\xff\xff'); m.write_ram(0x97AE, b'\x00')

armor_cases = []
for n in range(1200):
    m = Machine(rom)
    calls = m.stub_rts(0xAD5A)
    A = rnd.choice([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]); T = rnd.choice([i for i in range(11) if i != A])
    def slot(idx):
        s = [rnd.randrange(256) for _ in range(26)]
        s[2] = idx if idx < 8 else 8 + (idx % 3)                 # record index
        s[0x10] = rnd.randrange(8); s[0x12] = rnd.randrange(16); s[0x13] = rnd.randrange(16)
        s[1] = rnd.choice([rnd.randrange(256), rnd.randrange(256) & ~6, rnd.randrange(256) | 6])
        return s
    att, tgt = slot(A), slot(T)
    if att[2] == tgt[2]: tgt[2] = (tgt[2] + 1) % 11 if (tgt[2] + 1) % 11 < 8 or (tgt[2] + 1) % 11 >= 8 else 0
    arec = {0x18: rnd.choice([0, 1, 2, 3, 4, 4, 4]), 0x19: rnd.randrange(1, 12), 0xAE: rnd.choice([0, 0, 1, 2, 9, 12, 20, 30, 38])}
    trec = {}
    mask = rnd.randrange(256)
    build(m, A, T, att, tgt, arec, trec, mask)
    m.call(0x10666, d0=0, d1=0, d2=0x1234, d3=0x5678)
    armor_cases.append(dict(A=A, T=T, att=att, tgt=tgt, career=arec[0x18], level=arec[0x19], gear0=arec[0xAE], mask=mask,
                            armor=m.reg('d0') & 0xFF, d496=m.ram_byte(0xD496), msg=struct.unpack('>H', m.read_ram(0xD518, 2))[0],
                            facing=m.read_ram(0xC470 + T * 26 + 0x10, 1)[0], turned=len(calls)))
out['armor'] = armor_cases

# ---- 3. to-hit arithmetic tail (0x1056A..0x105F4)
th = []
for n in range(3000):
    m = Machine(rom)
    attack, armor = rnd.choice([rnd.randrange(256), rnd.randrange(30, 80)]), rnd.choice([rnd.randrange(256), rnd.randrange(40, 70)])
    dist = rnd.choice([rnd.randrange(0, 12), rnd.randrange(0, 40)]); rng = rnd.choice([rnd.randrange(0, 12), rnd.randrange(0, 40), 1, 0])
    wt = rnd.choice([0, 1, 2, 3, 4, 8]); item_mod = rnd.choice([rnd.randrange(0, 20), rnd.randrange(256)])
    f1, f2, f3 = [rnd.choice([0, 0, 1, rnd.randrange(256)]) for _ in range(3)]
    a6 = RAM_BASE + 0xE000
    m.write_ram(0xE000 - 2, bytes([attack, armor]))
    m.write_ram(0xD511, b'\xee'); m.write_ram(0xD518, b'\xff\xff'); m.write_ram(0xD512, b'\xee')
    m.write_ram(0xD501, bytes([f1])); m.write_ram(0xD502, bytes([f2])); m.write_ram(0xD503, bytes([f3]))
    m.run_range(0x1056A, 0x105F4, d1=dist, d5=wt, d6=rng, d7=item_mod, a6=a6, a7=RAM_BASE + 0xD000)
    th.append([attack, armor, dist, rng, wt, item_mod, f1, f2, f3, m.ram_byte(0xD511), struct.unpack('>H', m.read_ram(0xD518, 2))[0], m.ram_byte(0xD512)])
out['tohit'] = th
# ---- 4. attacks per slot per round (0x107E8 .. 0x10800): d7 = ((parity & 1) ^ d4) + slot[6 + d4]) >> 1
ar = []
for n in range(600):
    m = Machine(rom)
    A = rnd.randrange(11)
    slot = [rnd.randrange(256) for _ in range(26)]; slot[2] = A if A < 8 else 8 + A % 3
    slot[6] = rnd.choice([0, 1, 2, 3, 4, 5, 6, 9, rnd.randrange(256)]); slot[7] = rnd.choice([0, 1, 2, 3, 4, 5, 6, 9, rnd.randrange(256)])
    d4 = rnd.randrange(2); par = rnd.randrange(256)
    m.write_ram(0xC470 + A * 26, bytes(slot)); m.write_ram(0xCA20, bytes([A])); m.write_ram(0xD50C, bytes([par]))
    m.run_between(0x107E8, {0x10800}, d4=d4, d7=0x77777777)
    ar.append([slot[6 + d4], d4, par, m.reg('d7') & 0xFFFF])
out['attacks'] = ar
json.dump(out, open(sys.argv[2], 'w'), separators=(',', ':'))
print({k: len(v) for k, v in out.items()})
