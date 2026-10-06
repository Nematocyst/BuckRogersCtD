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
SLOT = 0xC470; REC = 0xBA68; G0, GN = 0xD490, 0x90; CA0, CAN = 0xCA20, 0x60


def make_record(rnd, idx, party, full=True):
    b = bytearray(rnd.randrange(256) for _ in range(214)) if full else bytearray(214)
    b[0x23] = rnd.choice([0, 0, 0, 1, 2, 3])
    if full: make_items(rnd, b)
    return b


def make_items(rnd, b, area=False):
    ids = list(range(1, 39))
    for k in range(13):
        o = 0x54 + 10 * k
        if rnd.random() < 0.35: b[o:o + 10] = bytes(10); continue
        b[o] = rnd.choice(ids); b[o + 4] = rnd.choice([0, 0, 1, 2, 5]); b[o + 5] = rnd.choice([0, 0, 0, 0, 0x10, 0x20, 0x30])
        b[o + 9] = rnd.choice([0, 0, 0, 1, 2, 3, 4, 13] if not area else [0, 1, 5, 6, 7, 8, 9, 10, 11, 12])
    for i in range(5): b[0x4D + i] = rnd.choice([0, rnd.choice(ids), rnd.choice(ids)])
    b[0x10] = rnd.randrange(0, 26); b[0x11] = rnd.randrange(0, 26); b[0x2F] = rnd.choice([0, 0, 2, 4, 6, rnd.randrange(256)])


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
    return dict(full=full, n=n, recs=recs, slots=slots, tiles=tiles, ft=ft, g=g, ca=ca, actor=actor, idx=rnd.randrange(256))


def load(m, w):
    for r, b in enumerate(w['recs']): m.write_ram(REC + r * 0xD6, bytes(b))
    for k, sb in enumerate(w['slots']): m.write_ram(SLOT + k * 26, bytes(sb))
    m.write_ram(0xBA64, struct.pack('>H', w['n'])); m.write_ram(0xCACA, bytes(w['tiles']))
    m.write_ram(0xCFFF, bytes(w['ft'])); m.write_ram(0xD810, struct.pack('>I', 0xFFFF0000 + 0xD000))
    m.write_ram(G0, bytes(w['g'])); m.write_ram(CA0, bytes(w['ca']))
    m.write_ram(0xD604, boot_table); m.write_ram(0xD804, bytes([w['idx']]))


def snap(m, w):
    return dict(slots=[m.read_ram(SLOT + k * 26, 26).hex() for k in range(w['n'])], recsum=sum((i + 1) * b for i, b in enumerate(m.read_ram(REC, 11 * 0xD6))) & 0xFFFFFFFF, m97=m.ram_byte(0x97AE),
                tiles=m.read_ram(0xCACA, 441).hex(), g=m.read_ram(G0, GN).hex(), ca=m.read_ram(CA0, CAN).hex(),
                ridx=m.ram_byte(0xD804), rsum=sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF)


def pre(w):
    return dict(n=w['n'], recs=[bytes(r[:(214 if w.get('full') else 0x24)]).hex() for r in w['recs']], slots=[bytes(s).hex() for s in w['slots']], tiles=bytes(w['tiles']).hex(),
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

# ---- C. 0xE89C weapon choice: a3 = actor slot, a2 = actor record, d0 = mode
for _ in range(500):
    m = Machine(rom); w = build(m, rnd, full=True)
    w['g'][0xD499 - G0] = rnd.randrange(0, 4); w['g'][0xD49A - G0] = rnd.randrange(0, 4)
    for k in range(32):                                  # temporary effect list [0xD49C]: slot, effect, duration
        if rnd.random() < 0.15: w['g'][0xD49C - G0 + 3 * k: 0xD49C - G0 + 3 * k + 3] = bytes([rnd.randrange(w['n']), rnd.choice([0x14, 0x18, 0x03]), 5])
    load(m, w); a = w['actor']; d97 = rnd.choice([0, 0x10]); m97 = rnd.choice([0, 0, 1])
    m.write_ram(0x97AE, bytes([m97])); m.write_ram(0x97DC, bytes([d97])); m.write_ram(0xCA20, bytes([a]))
    if rnd.random() < 0.3:                                # make the actor stand on tile 0 sometimes
        x, y = w['slots'][a][0x12], w['slots'][a][0x13]; tl = bytearray(w['tiles']); tl[x * 21 + y] = 0; m.write_ram(0xCACA, bytes(tl)); w['tiles'] = tl
    mode = rnd.choice([0, 1, 1])
    m.call(0xE89C, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + w['slots'][a][2] * 0xD6, a0=RAM_BASE + SLOT, d0=mode, max_insns=3000000)
    pr = pre(w); pr['m97'] = m97; pr['d97dc'] = d97
    cases.append(dict(fn='weapon', pre=pr, a=a, range=mode, post=snap(m, w)))

out['cases'] = cases
json.dump(out, open(sys.argv[2], 'w'), separators=(',', ':'))
import collections; print(collections.Counter(c['fn'] for c in cases))
