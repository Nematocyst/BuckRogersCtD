"""Monster-turn vectors (ROM 0xEF64 and the routines it calls) from the real ROM code in a 68000 emulator.
Every case stores the RAM the routines touch before and after: slots, records, tile map, terrain flags, scratch globals 0xD4E0..0xD51F,
actor/target-list area 0xCA20..0xCA7F and the RNG state. usage: python gen_monster_vectors.py ROM OUT.json"""
import sys, json, random, struct, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0xEF64)
m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4)
boot_table = m0.read_ram(0xD604, 512)
out = dict(boot_table=list(struct.unpack('>256H', boot_table)))
SLOT = 0xC470; REC = 0xBA68; G0, GN = 0xD4E0, 0x40; CA0, CAN = 0xCA20, 0x60


def make_record(rnd, idx, party, full=True):
    b = bytearray(rnd.randrange(256) for _ in range(214)) if full else bytearray(214)
    b[0x23] = rnd.choice([0, 0, 0, 1, 2, 3])
    return b


def build(m, rnd, nmin=3, nmax=12, near=True, full=False):
    """random combat world; returns the pre-state dict"""
    n = rnd.randrange(nmin, nmax)
    recs = [make_record(rnd, r, r < 8, full) for r in range(11)]
    slots = []
    for k in range(n):
        sb = bytearray(rnd.randrange(256) for _ in range(26))
        sb[0] = rnd.choice([1, 1, 1, 1, 1, 0x81, 0x11, 0x41, 0x00])
        sb[1] = rnd.choice([0x00, 0x01, 0x01, 0x10, 0x11, 0x04, 0x05, 0x20, 0x21])
        sb[2] = k if k < 8 else 8 + rnd.randrange(3)
        sb[0x12] = rnd.randrange(21); sb[0x13] = rnd.randrange(21)
        sb[0x17] = rnd.choice([0xFF, 0xFF, rnd.randrange(n)])
        sb[0xE] = rnd.randrange(1, 60)
        slots.append(sb)
    for k in range(n):                                   # monsters near the party so lines of fire exist
        if near and rnd.random() < 0.7:
            o = slots[rnd.randrange(n)]
            slots[k][0x12] = max(0, min(20, o[0x12] + rnd.randrange(-6, 7))); slots[k][0x13] = max(0, min(20, o[0x13] + rnd.randrange(-6, 7)))
    tiles = bytearray(rnd.choice([2, 2, 2, 2, 2, 3, 4, 5, 6, 0, 1, rnd.randrange(128)]) for _ in range(441))
    ft = bytearray(rnd.choice([0, 0, 0, 0, 0, 0, 0x80, 0x40, rnd.randrange(256)]) for _ in range(129))
    actor = rnd.randrange(n)
    g = bytearray(GN); ca = bytearray(rnd.randrange(256) for _ in range(CAN)); ca[0] = actor
    g[0xD504 - G0] = rnd.choice([0, 0, 1])
    g[0xD500 - G0] = rnd.choice([0, 1, 0xFF])
    g[0xD506 - G0] = rnd.choice([0, 3])
    for k in range(n):
        if rnd.random() < 0.7: slots[k][0x1] = (slots[k][1] & ~1) | (k < 4)   # a rough party / monster split
    return dict(n=n, recs=recs, slots=slots, tiles=tiles, ft=ft, g=g, ca=ca, actor=actor, idx=rnd.randrange(256))


def load(m, w):
    for r, b in enumerate(w['recs']): m.write_ram(REC + r * 0xD6, bytes(b))
    for k, sb in enumerate(w['slots']): m.write_ram(SLOT + k * 26, bytes(sb))
    m.write_ram(0xBA64, struct.pack('>H', w['n'])); m.write_ram(0xCACA, bytes(w['tiles']))
    m.write_ram(0xCFFF, bytes(w['ft'])); m.write_ram(0xD810, struct.pack('>I', 0xFFFF0000 + 0xD000))
    m.write_ram(G0, bytes(w['g'])); m.write_ram(CA0, bytes(w['ca']))
    m.write_ram(0xD604, boot_table); m.write_ram(0xD804, bytes([w['idx']]))


def snap(m, w):
    return dict(slots=[m.read_ram(SLOT + k * 26, 26).hex() for k in range(w['n'])], recsum=sum(m.read_ram(REC, 11 * 0xD6)) & 0xFFFFFFFF,
                tiles=m.read_ram(0xCACA, 441).hex(), g=m.read_ram(G0, GN).hex(), ca=m.read_ram(CA0, CAN).hex(),
                ridx=m.ram_byte(0xD804), rsum=sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF)


def pre(w):
    return dict(n=w['n'], recs=[bytes(r[:w.get('rlen', 0x24)]).hex() for r in w['recs']], slots=[bytes(s).hex() for s in w['slots']], tiles=bytes(w['tiles']).hex(),
                ft=bytes(w['ft']).hex(), g=bytes(w['g']).hex(), ca=bytes(w['ca']).hex(), idx=w['idx'])


cases = []

# ---- A. 0x15C2C target list: d0 = actor, d2 = range
for _ in range(500):
    m = Machine(rom); w = build(m, rnd); load(m, w)
    rng_ = rnd.choice([3, 6, 10, 100]); a = w['actor']
    m.call(0x15C2C, d0=a, d2=rng_, max_insns=3000000)
    cases.append(dict(fn='enum', pre=pre(w), a=a, range=rng_, post=snap(m, w)))

# ---- B. 0xE812 choose target: a3 = actor slot
for _ in range(700):
    m = Machine(rom); w = build(m, rnd); load(m, w); a = w['actor']
    m.write_ram(0xCA20, bytes([a])); m.write_ram(0xBA64, struct.pack('>H', w['n']))
    m.call(0xE812, a3=RAM_BASE + SLOT + a * 26, max_insns=6000000)
    cases.append(dict(fn='select', pre=pre(w), a=a, post=snap(m, w)))

out['cases'] = cases
json.dump(out, open(sys.argv[2], 'w'), separators=(',', ':'))
import collections; print(collections.Counter(c['fn'] for c in cases))
