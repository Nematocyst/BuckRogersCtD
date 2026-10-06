"""Monster-turn vectors (ROM 0xEF64 and the routines it calls) from the real ROM code in a 68000 emulator.
Every case stores the RAM the routines touch before and after (see monster_world.py). usage: python gen_monster_vectors.py ROM OUT.json"""
import sys, json, struct, collections
from monster_world import *

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

# ---- D. 0x1074A: carry out an attack by the actor on [0xD513] (sane worlds, graphics routines stubbed)
for _ in range(500):
    m = machine(rom); w = sane_world(rnd); a = w['actor']; n = w['n']
    foes = [k for k in range(n) if (w['slots'][k][1] & 1) != (w['slots'][a][1] & 1) and w['slots'][k][0] in (1, 0x81)]
    t = rnd.choice(foes) if foes and rnd.random() < 0.9 else rnd.randrange(n)
    w['ca'][0] = a; w['g'][0xD513 - G0] = t; w['g'][0xD511 - G0] = rnd.randrange(1, 20); w['g'][0xD496 - G0] = rnd.choice([1, 1, 1, 2, 3])
    load(m, w)
    mode = rnd.choice([2, 2, 2, 5]); d8ca = [rnd.randrange(1, 9), rnd.randrange(1, 9)]
    m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, bytes([mode])); m.write_ram(0xD8CA, bytes(d8ca))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = mode; pr['d8ca'] = d8ca
    m.call(0x1074A, max_insns=4000000)
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2))
    cases.append(dict(fn='attack', pre=pr, a=a, range=t, post=sn))

# ---- E. 0xF898: one step of the actor by ([0xB3F4],[0xB3F6]) including the reactions of the enemies (0x11A44)
for _ in range(900):
    m = machine(rom); w = sane_world(rnd); a = w['actor']; n = w['n']; sa = w['slots'][a]
    off = rnd.random() < 0.12
    if off: sa[1] |= 0x80; sa[0x12] = rnd.choice([0, 20, rnd.randrange(21)]); sa[0x13] = rnd.choice([0, 20, rnd.randrange(21)])
    dx, dy = rnd.choice([-1, 0, 1]), rnd.choice([-1, 0, 1])
    if not off:
        if not (0 <= sa[0x12] + dx < 21): dx = 0
        if not (0 <= sa[0x13] + dy < 21): dy = 0
    if rnd.random() < 0.6:                                  # the actor is "in motion": flag 4 set, its markers cleared (what 0xF9A6 does)
        sa[1] |= 4
        t = w['recs'][sa[2]][0x23]; x, y = sa[0x12], sa[0x13]
        for (cx, cy) in [(x, y)] + ([(x + 1, y)] if t == 3 else []) + ([(x, y + 1)] if t == 2 else []):
            if cx < 21 and cy < 21: w['tiles'][cy * 21 + cx] &= 0x7F
    w['ca'][0] = a; w['g'][0xD496 - G0] = 1
    load(m, w)
    m.write_ram(0xB3F4, struct.pack('>hh', dx, dy)); m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5]))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0xF898, max_insns=6000000, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + sa[2] * 0xD6)
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2)); sn['ret'] = struct.unpack('b', bytes([m.reg('d0') & 0xFF]))[0]
    sn['mv'] = list(struct.unpack('>hh', m.read_ram(0xB3F4, 4)))
    cases.append(dict(fn='move', pre=pr, a=a, range=0, mv=[dx, dy], post=sn))

out['cases'] = cases
json.dump(out, open(sys.argv[2], 'w'), separators=(',', ':'))
import collections; print(collections.Counter(c['fn'] for c in cases))
