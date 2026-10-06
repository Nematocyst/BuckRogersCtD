"""Monster-turn vectors (ROM 0xEF64 and the routines it calls) from the real ROM code in a 68000 emulator.
Every case stores the RAM the routines touch before and after (see monster_world.py). usage: python gen_monster_vectors.py ROM OUT.json"""
import sys, json, struct, collections, gzip
from monster_world import *

import os
cases = []
def N(name, default): return int(os.environ.get('N_' + name, default))

# ---- A. 0x15C2C target list: d0 = actor, d2 = range
for _ in range(N('enum', 500)):
    m = Machine(rom); w = build(m, rnd); load(m, w)
    rng_ = rnd.choice([3, 6, 10, 100]); a = w['actor']
    m.call(0x15C2C, d0=a, d2=rng_, max_insns=3000000)
    cases.append(dict(fn='enum', pre=pre(w), a=a, range=rng_, post=snap(m, w)))

# ---- B. 0xE812 choose target: a3 = actor slot
for _ in range(N('select', 700)):
    m = Machine(rom); w = build(m, rnd); load(m, w); a = w['actor']
    m.write_ram(0xCA20, bytes([a])); m.write_ram(0xBA64, struct.pack('>H', w['n']))
    m.call(0xE812, a3=RAM_BASE + SLOT + a * 26, max_insns=6000000)
    cases.append(dict(fn='select', pre=pre(w), a=a, post=snap(m, w)))

# ---- C. 0xE89C weapon choice: a3 = actor slot, a2 = actor record, d0 = mode
for _ in range(N('weapon', 500)):
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
for _ in range(N('attack', 500)):
    m = machine(rom); w = sane_world(rnd, effects=rnd.random() < 0.6); a = w['actor']; n = w['n']
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
for _ in range(N('move', 900)):
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

# ---- F. 0x15D8A: path to the nearest enemy (mode 0), actor = d0
for _ in range(N('nav', 500)):
    m = machine(rom); w = sane_world(rnd); a = w['actor']
    w['ca'][0] = a; w['g'][0xD505 - G0] = 0
    w['g'][0xD5F8 - G0: 0xD5F8 - G0 + 8] = bytes([0x55] * 8)
    load(m, w)
    m.call(0x15D8A, max_insns=20_000_000, d0=a, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + w['slots'][a][2] * 0xD6)
    cases.append(dict(fn='nav', pre=pre(w), a=a, range=0, post=snap(m, w)))

# ---- G. 0xEF64: a whole turn of a computer-controlled creature (monsters; party creatures without healing skill)
import os
for _ in range(N('turn', 1500)):
    m = machine(rom); w = sane_world(rnd, fx=rnd.random() < 0.5, effects=rnd.random() < 0.6); a = w['actor']; n = w['n']; sa = w['slots'][a]
    if rnd.random() < 0.15:                                    # a party creature under computer control
        pa = rnd.randrange(w['npar']); a = pa; w['actor'] = a; sa = w['slots'][a]
    sa[0] = 1; sa[0x14] = rnd.choice([2, 2, 2, 1, 3]); sa[1] &= ~0x04; sa[0x16] = rnd.randrange(0, 14)
    if rnd.random() < 0.7: sa[1] |= 0x80                       # computer controlled (every monster has it)
    if rnd.random() < 0.5: sa[1] &= ~0x10
    if rnd.random() < 0.5: w['recs'][sa[2]][0x32] = 0; w['recs'][sa[2]][0x3B] = 0
    else: w['recs'][sa[2]][0x32] = rnd.choice([0, 2, 6]); w['recs'][sa[2]][0x3B] = rnd.choice([4, 10, 16])
    if rnd.random() < 0.3: sa[0x17] = 0xFF
    w['ca'][0] = a; w['g'][0xD505 - G0] = 0; w['g'][0xD496 - G0] = 1
    load(m, w)
    m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xD8FC, b'\x00')
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0xEF64, max_insns=30_000_000, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + sa[2] * 0xD6, d5=0, d7=0, d3=0, d6=0)
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2))
    cases.append(dict(fn='turn', pre=pr, a=a, range=0, post=sn))

# ---- H. 0xEB50 scoring mode: the expected value of throwing the item in item slot k
def explosive_world(rnd, hand_only=False):
    for _ in range(50):
        w = sane_world(rnd, fx=True); a = w['actor']; rec = w['recs'][w['slots'][a][2]]
        w['slots'][a][1] = w['slots'][a][1] & ~0x04
        slots_ex = [k for k in range(13) if 5 <= rec[0x54 + 10 * k + 9] <= 12 and rec[0x54 + 10 * k]]
        if hand_only and not (5 <= rec[0xAE + 9] <= 12 and rec[0xAE]): continue
        if slots_ex or hand_only: return w, slots_ex
    return w, []


for _ in range(N('eb50s', 400)):
    m = machine(rom); w, ex = explosive_world(rnd)
    if not ex: continue
    a = w['actor']; rec = w['recs'][w['slots'][a][2]]; k = rnd.choice(ex); off = 0x54 + 10 * k
    w['ca'][0] = a; w['g'][0xD505 - G0] = 0; w['g'][0xD500 - G0] = rnd.choice([0, 1, 0xFF])
    load(m, w); m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02')
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0xEB50, max_insns=8_000_000, d0=1, a0=RAM_BASE + REC + w['slots'][a][2] * 0xD6 + off, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + w['slots'][a][2] * 0xD6)
    sn = snap(m, w); sn['ret'] = m.reg('d0') & 0xFFFF
    cases.append(dict(fn='eb50s', pre=pr, a=a, range=off, post=sn))

# ---- I. 0xEB50 execution mode: throw the hand item at the best cell (or choose another weapon)
for _ in range(N('eb50x', 400)):
    m = machine(rom); w, ex = explosive_world(rnd, hand_only=True)
    a = w['actor']; rec = w['recs'][w['slots'][a][2]]
    if not (5 <= rec[0xAE + 9] <= 12 and rec[0xAE]): continue
    w['ca'][0] = a; w['g'][0xD505 - G0] = 0; w['g'][0xD500 - G0] = rnd.choice([0, 1, 0xFF]); w['g'][0xD511 - G0] = rnd.randrange(0, 20)
    load(m, w); m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5]))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0xEB50, max_insns=12_000_000, d0=0, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + w['slots'][a][2] * 0xD6)
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2)); sn['mv'] = list(struct.unpack('>hh', m.read_ram(0xB3F0, 4)))
    cases.append(dict(fn='eb50x', pre=pr, a=a, range=0, post=sn))

# ---- J. 0x10FAA: the blast at the cursor cell
for _ in range(N('blast', 500)):
    m = machine(rom); w, ex = explosive_world(rnd, hand_only=True)
    a = w['actor']; rec = w['recs'][w['slots'][a][2]]
    if not (5 <= rec[0xAE + 9] <= 12 and rec[0xAE]): continue
    sa = w['slots'][a]
    tgt = w['slots'][rnd.randrange(w['n'])]
    cell = (rnd.randrange(21), rnd.randrange(21)) if rnd.random() < 0.25 else (max(0, min(20, tgt[0x12] + rnd.randrange(-1, 2))), max(0, min(20, tgt[0x13] + rnd.randrange(-1, 2)))) if rnd.random() < 0.7 else (max(0, min(20, sa[0x12] + rnd.randrange(-6, 7))), max(0, min(20, sa[0x13] + rnd.randrange(-6, 7))))
    cur = (cell[0] * 24 + rnd.randrange(24), cell[1] * 24 + rnd.randrange(24))
    w['ca'][0] = a; w['g'][0xD505 - G0] = 0; w['g'][0xD500 - G0] = rnd.choice([0, 1, 0xFF]); w['g'][0xD511 - G0] = rnd.randrange(0, 20)
    load(m, w); m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xB3F0, struct.pack('>HH', *cur))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0x10FAA, max_insns=12_000_000, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + sa[2] * 0xD6)
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2))
    cases.append(dict(fn='blast', pre=pr, a=a, range=0, mv=list(cur), post=sn))

# ---- K. 0x1158A: patches count down
for _ in range(N('tick', 100)):
    m = machine(rom); w = sane_world(rnd, fx=True)
    for o in range(0, 256, 16):
        if w['haz'][o + 0xE] == 0 and rnd.random() < 0.3: pass
    w['ca'][0] = w['actor']
    load(m, w); m.write_ram(0x9BBC, b'\x02')
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0x1158A, max_insns=2_000_000)
    cases.append(dict(fn='tick', pre=pr, a=w['actor'], range=0, post=snap(m, w)))

# ---- L. 0x664E: the effect hooks of one stage for one creature
for _ in range(N('stage', 1500)):
    m = machine(rom); w = sane_world(rnd, effects=True); a = w['actor']; n = w['n']
    ctx = rnd.randrange(n); stage = rnd.choice(list(range(0, 23)) + [2, 3, 5, 5, 5, 5, 5, 5, 7, 9, 12, 14, 14, 15, 18])
    w['slots'][ctx][0] = 1
    victim = rnd.randrange(n)
    w['ca'][0] = a; w['g'][0xD513 - G0] = victim; w['g'][0xD497 - G0] = rnd.choice([0, 1, 5, 20, 60, rnd.randrange(256)]); w['g'][0xD55E - G0] = rnd.choice([0, 0xD, 0xE, 0x1D, 0x1C])
    w['g'][0xD496 - G0] = rnd.choice([1, 2]); w['g'][0xD4FC - G0] = 0
    w['slots'][ctx][0x17] = rnd.choice([0xFF, rnd.randrange(n)])
    load(m, w); m.write_ram(0x97AE, bytes([rnd.choice([0, 0, 1])])); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5]))
    pr = pre(w); pr['m97'] = m.ram_byte(0x97AE); pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0x664E, max_insns=3_000_000, d0=stage, a3=RAM_BASE + SLOT + ctx * 26, a2=RAM_BASE + REC + w['slots'][ctx][2] * 0xD6)
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2))
    cases.append(dict(fn='stage', pre=pr, a=a, range=stage, mv=[ctx, 0], post=sn))

# ---- M. 0xE4F0: a whole turn of one creature (turn-start effects, the controller, item upkeep)
for _ in range(N('beginturn', 800)):
    m = machine(rom); w = sane_world(rnd, fx=rnd.random() < 0.4, effects=True); a = w['actor']; n = w['n']; sa = w['slots'][a]
    sa[0] = 1; sa[0x14] = rnd.choice([2, 2, 2, 1, 3]); sa[1] = (sa[1] & ~0x04) | 0x80; sa[0x16] = rnd.randrange(0, 14)
    if rnd.random() < 0.5: sa[1] &= ~0x10
    w['recs'][sa[2]][0x32] = 0; w['recs'][sa[2]][0x3B] = 0
    for k in range(13):
        if rnd.random() < 0.3: w['recs'][sa[2]][0x54 + 10 * k + 5] |= rnd.choice([0x10, 0x20, 0x30])
    if rnd.random() < 0.3: sa[0x17] = 0xFF
    w['ca'][0] = a; w['g'][0xD505 - G0] = 0; w['g'][0xD496 - G0] = 1
    load(m, w)
    m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xD8FC, b'\x00')
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0xE4F0, max_insns=40_000_000, d5=0, d7=0, d3=0, d6=0)
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2))
    cases.append(dict(fn='beginturn', pre=pr, a=a, range=0, post=sn))

# ---- N. 0x10200: a party creature with healing skill goes to a fallen friend and treats it
def healer_world(rnd):
    w = sane_world(rnd, nmin=5, nmax=11, effects=rnd.random() < 0.3); n = w['n']; npar = w['npar']
    if npar < 2: return None
    a = rnd.randrange(npar); w['actor'] = a; sa = w['slots'][a]; rec = w['recs'][sa[2]]
    sa[0] = 1; sa[1] = (sa[1] | 1) & ~0x04; sa[0x14] = 2
    rec[0x32] = rnd.choice([0, 0, 1, 3, 6, 12]); rec[0x3B] = rnd.choice([0, 3, 8, 14, 20]); rec[0x19] = rnd.randrange(1, 12)
    for i in range(0x31, 0x3F):
        if i not in (0x32, 0x3B) and rnd.random() < 0.3: rec[i] = rnd.randrange(0, 20)
    downed = [k for k in range(npar) if k != a]
    for k in downed:
        if rnd.random() < 0.7: w['slots'][k][0] = rnd.choice([0x83, 0x84, 0x83]); w['slots'][k][0xE] = 0
    for r in range(8): w['recs'][r][0x2E] = rnd.randrange(8, 60)
    w['g'][0xD50A - G0] = rnd.choice([0, 0, rnd.randrange(256)])
    return w


for _ in range(N('rescue', 600)):
    w = healer_world(rnd)
    if w is None: continue
    m = machine(rom); a = w['actor']; sa = w['slots'][a]
    # markers must follow the statuses (down creatures leave the map markers)
    for i in range(441): w['tiles'][i] &= 0x7F
    for sb in w['slots']:
        st = sb[0]
        if st == 0 or (st & 0xC0) or (sb[1] & 4): continue
        t = w['recs'][sb[2]][0x23]; x, y = sb[0x12], sb[0x13]
        for (cx, cy) in [(x, y)] + ([(x + 1, y)] if t == 3 else []) + ([(x, y + 1)] if t == 2 else []):
            if cx < 21 and cy < 21: w['tiles'][cy * 21 + cx] |= 0x80
    w['ca'][0] = a; w['g'][0xD505 - G0] = 0; w['g'][0xD496 - G0] = 1
    load(m, w); m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5]))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0x10200, max_insns=30_000_000, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + sa[2] * 0xD6, d7=0, d3=0)
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2))
    cases.append(dict(fn='rescue', pre=pr, a=a, range=0, post=sn))

# ---- O. 0xF2AE: the turn of a player-controlled creature, driven by scripted menu answers (0x1391A) and control pad readings (0xF1B66)
def remark(w):
    """map markers follow the creature statuses"""
    for i in range(441): w['tiles'][i] &= 0x7F
    for sb in w['slots']:
        st = sb[0]
        if st == 0 or (st & 0xC0) or (sb[1] & 4): continue
        t = w['recs'][sb[2]][0x23]; x, y = sb[0x12], sb[0x13]
        for (cx, cy) in [(x, y)] + ([(x + 1, y)] if t == 3 else []) + ([(x, y + 1)] if t == 2 else []):
            if cx < 21 and cy < 21: w['tiles'][cy * 21 + cx] |= 0x80


def manual_world(rnd):
    w = sane_world(rnd, nmin=5, nmax=11, fx=rnd.random() < 0.45, effects=rnd.random() < 0.3); n = w['n']; npar = w['npar']
    a = rnd.randrange(npar); w['actor'] = a; sa = w['slots'][a]; rec = w['recs'][sa[2]]
    sa[0] = 1; sa[1] = (sa[1] | 1) & ~0x84; sa[0x14] = rnd.choice([2, 2, 2, 1]); sa[0x16] = rnd.randrange(2, 14)
    if rnd.random() < 0.5:                                                   # a healer with fallen friends
        rec[0x32] = rnd.choice([0, 0, 1, 3, 6, 12]); rec[0x3B] = rnd.choice([0, 3, 8, 14, 20]); rec[0x19] = rnd.randrange(1, 12)
        for k in range(npar):
            if k != a and rnd.random() < 0.6: w['slots'][k][0] = rnd.choice([0x83, 0x84, 0x83]); w['slots'][k][0xE] = 0
    for r in range(8): w['recs'][r][0x2E] = rnd.randrange(8, 60)
    w['g'][0xD50A - G0] = rnd.choice([0, 0, rnd.randrange(256)])
    w['g'][0xD51D - G0 + a] = rnd.choice([0xFF, 0xFF, rnd.randrange(n), rnd.randrange(npar, n)])
    w['ca'][0] = a; w['ca'][1] = rnd.choice([1, 1, 1, 0]); w['g'][0xD505 - G0] = 0; w['g'][0xD496 - G0] = 1
    remark(w)
    return w


DIRS = [1, 2, 4, 8, 5, 6, 9, 10]


def manual_script(rnd):
    menu = [rnd.choice([0, 0, 0, 1, 1, 1, 3, 3, 4, -1, 2, 0xFFFF]) for _ in range(rnd.randrange(1, 9))]
    pad = []
    for _ in range(rnd.randrange(8, 40)):
        r = rnd.random()
        if r < 0.22: pad += [rnd.choice(DIRS[:4])] * 21               # a straight walk (the ROM reads 21 pad values per step)
        elif r < 0.30: pad += [rnd.choice(DIRS[4:])]                  # a diagonal step / one cursor cell
        elif r < 0.30 + 0.30: pad.append(rnd.choice(DIRS))
        elif r < 0.62: pad.append(0x10)
        elif r < 0.78: pad.append(0x20)
        elif r < 0.82: pad.append(0)
        elif r < 0.87: pad.append(0x80)
        elif r < 0.90: pad.append(0x40)
        else: pad.append(rnd.randrange(256))
    return menu, pad


for _ in range(N('manual', 700)):
    w = manual_world(rnd); a = w['actor']; sa = w['slots'][a]
    menu, pad = manual_script(rnd)
    m = machine(rom, unstub=(0xE5E6,), extra=(0xF81C, 0xF780, 0xF7BA, 0xF838, 0x116F8, 0x11746, 0x1172A, 0x6C54, 0xAD46, 0x96D8, 0x1344E, 0xFF7C, 0x11898, 0xFDC8))
    mq, pq, used = list(menu), list(pad), [0, 0]
    def next_menu():
        used[0] += 1
        return (mq.pop(0) & 0xFFFF) if mq else 4
    def next_pad():
        used[1] += 1
        return pq.pop(0) if pq else 0x80
    m.stub_fn(0x1391A, next_menu); m.stub_fn(0xF1B66, next_pad)
    load(m, w)
    m.write_ram(0xB400, struct.pack('>HH', 504, 504)); m.write_ram(0xEE00, bytes(0x200))
    m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5]))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0xF2AE, max_insns=60_000_000, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + sa[2] * 0xD6)
    if m.reg('pc') != 0x00FFF000: continue                                        # did not finish within the instruction budget
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2)); sn['polls'] = used
    cases.append(dict(fn='manual', pre=pr, a=a, range=0, menu=[x & 0xFFFF for x in menu], pad=pad, post=sn))

out['cases'] = cases
json.dump(out, gzip.open(sys.argv[2], 'wt'), separators=(',', ':'))
import collections; print(collections.Counter(c['fn'] for c in cases))
