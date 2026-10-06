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

# ---- P. 0x78D6: the inventory screen, driven by a scripted menu (0x1391A answers cells that are not greyed out) and quantity prompt (0x7DE6)
def inv_item(rnd, pool=None):
    if rnd.random() < 0.4: return bytes(10)
    return bytes([rnd.choice(pool) if pool else rnd.randrange(1, 39), 0, 0, 0, rnd.choice([0, 0, 1, 2] if not pool else [0, 0, 0, 1]), rnd.choice([0, 0, 0, 0x10, 0x30]), rnd.randrange(0, 40), rnd.randrange(0, 256),
                  rnd.choice([0, 0, 1, 2, 5, 30, 200, 240, 245, 248, 249, 250, 255]) if not pool else rnd.choice([0, 1, 2, 3, 5, 30, 100, 150, 200, 240, 245, 248]), rnd.choice([0, 0, 1, 2, 3, 4, 13])])


def inv_world(rnd):
    w = sane_world(rnd, nmin=4, nmax=10); npar = w['npar']; n = w['n']
    pool = rnd.sample(range(1, 39), 4) if rnd.random() < 0.6 else None           # members carrying the same things, so stacks merge
    for r in range(8):
        rec = w['recs'][r]
        for k in range(13): rec[0x54 + 10 * k: 0x54 + 10 * k + 10] = inv_item(rnd, pool)
        for o in (0xAE, 0xB8, 0xC2, 0xCC): rec[o:o + 10] = inv_item(rnd, pool) if rnd.random() < 0.6 else bytes(10)
    for k in range(npar):
        w['slots'][k][0] = rnd.choice([1, 1, 1, 0x81, 0x41, 0x83, 0x82, 0x01]); w['slots'][k][1] |= 1
        w['slots'][k][0x14] = rnd.choice([0, 2, 2]); w['slots'][k][0x15] = rnd.randrange(3)
    a = rnd.randrange(npar); w['actor'] = a; w['slots'][a][0] = rnd.choice([1, 1, 1, 0x81])
    w['slots'][a][1] = (w['slots'][a][1] & ~0x40) | rnd.choice([0, 0, 0x40])
    w['ca'][0] = a
    return w


for _ in range(N('inv', 600)):
    w = inv_world(rnd); a = w['actor']; n = w['n']
    mode = rnd.choice([9, 9, 2, 5, 1]); shop = rnd.choice([0, 0, 1]); money = rnd.choice([0, 100, 65000, 70000, rnd.randrange(1 << 24)]); m97 = rnd.choice([0, 0, 0, 0, 1])
    m = machine(rom, extra=(0x8A44, 0x95BE, 0x9DD4, 0x978C, 0x7BA6, 0x7FC8, 0xC8FC, 0x7000, 0xA102, 0x138AC, 0x138FC, 0x8034))
    sheet = rnd.random() < 0.3; smenus = []
    if sheet: mode = 9
    from unicorn import UC_HOOK_CODE
    from unicorn.m68k_const import UC_M68K_REG_A7, UC_M68K_REG_PC, UC_M68K_REG_D2, UC_M68K_REG_SR
    menus, qtys, qmaxs = [], [], []
    limit = rnd.randrange(2, 16)
    def menu_fn():
        if sheet and m.reg('a2') == 0x7508:                                  # the sheet's page menu
            ans = rnd.choice([0, 1, 2, 2, 2, 2, 3, 0xFFFF]) if len(smenus) < 3 else 0
            smenus.append(ans)
            for ad in (0xD593, 0xD595, 0xD592, 0xD597, 0xD596): m.write_ram(ad, b'\x00')
            return ans
        lst = []; ad = 0xD564
        while m.ram_byte(ad) < 0x80: lst.append(m.ram_byte(ad)); ad += 1
        def exists(c):
            if c < 0xF: return True
            st = m.ram_byte(SLOT + (c - 0xF) * 26) if c - 0xF < n else 0
            return st != 0 and not (st & 0x40)
        cands = [c for c in range(0x17) if c != 0xE and c not in lst and exists(c)]
        if len(menus) >= limit or not cands or rnd.random() < 0.1: ans = 0xE
        elif rnd.random() < 0.03: ans = 0xFFFF
        else: ans = rnd.choice(cands)
        menus.append(ans)
        for ad in (0xD593, 0xD595, 0xD592, 0xD597, 0xD596): m.write_ram(ad, b'\x00')
        return ans
    m.stub_fn(0x1391A, menu_fn)
    m.stub_fn(0x13DA2, lambda: (m.write_ram(0xD564, b'\xFF'), 0)[1])
    def add_fn():
        c = m.reg('d0') & 0xFF; ad = 0xD564
        while m.ram_byte(ad) < 0x80: ad += 1
        m.write_ram(ad, bytes([c, 0xFF])); return c
    m.stub_fn(0x13E56, add_fn)
    def qty_hook(uc, address, size, user):
        if address != 0x7DE6: return
        mx = uc.reg_read(UC_M68K_REG_D2) & 0xFF
        q = rnd.choice([0, 1, mx, rnd.randrange(0, mx + 1)]) if rnd.random() > 0.1 else 0
        qtys.append(q); qmaxs.append(mx)
        sp = uc.reg_read(UC_M68K_REG_A7); ret = struct.unpack('>I', bytes(uc.mem_read(sp, 4)))[0]
        uc.reg_write(UC_M68K_REG_A7, sp + 4); uc.reg_write(UC_M68K_REG_D2, q)
        uc.reg_write(UC_M68K_REG_SR, (uc.reg_read(UC_M68K_REG_SR) & ~0xF) | (0x4 if q == 0 else 0)); uc.reg_write(UC_M68K_REG_PC, ret)
    m.uc.hook_add(UC_HOOK_CODE, qty_hook, begin=0x7DE6, end=0x7DE6)
    load(m, w)
    m.write_ram(0x97AE, bytes([m97])); m.write_ram(0x9BBC, bytes([mode])); m.write_ram(0xBA60, bytes([shop])); m.write_ram(0x9BD0, struct.pack('>I', money)); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xEE00, bytes(0x200))
    pr = pre(w); pr['m97'] = m97; pr['mode'] = mode; pr['d8ca'] = [5, 5]; pr['shop'] = shop; pr['money'] = money
    try: m.call(0x748C if sheet else 0x78D6, max_insns=20_000_000)
    except Exception as e:
        continue                                                            # an item handed to a member with no room makes the ROM write into itself
    if m.reg('pc') != 0x00FFF000: continue
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2)); sn['money'] = struct.unpack('>I', m.read_ram(0x9BD0, 4))[0]; sn['polls'] = [len(menus), len(qtys)]
    cases.append(dict(fn='inventory', pre=pr, a=a, range=1 if sheet else 0, mv=[x & 0xFFFF for x in smenus], menu=menus, pad=qtys, qmax=qmaxs, post=sn))

# ---- Q. 0xF898 with the real "leave the battlefield?" prompt (0x136DA) answered by scripted pad readings
for _ in range(N('retreat', 400)):
    m = machine(rom, retreat=None, extra=(0x1388E, 0x1382C, 0x138FC, 0x6C54, 0x6C3A, 0x5308)); w = sane_world(rnd); a = w['actor']; n = w['n']; sa = w['slots'][a]
    sa[1] &= ~0x80
    sa[0x12] = rnd.choice([0, 20, rnd.randrange(21)]); sa[0x13] = rnd.choice([0, 20, rnd.randrange(21)])
    dx, dy = rnd.choice([-1, 0, 1]), rnd.choice([-1, 0, 1])
    if sa[0x12] == 0: dx = rnd.choice([-1, -1, 0, 1])
    if sa[0x12] == 20: dx = rnd.choice([1, 1, 0, -1])
    if sa[0x13] == 0: dy = rnd.choice([-1, -1, 0, 1])
    if sa[0x13] == 20: dy = rnd.choice([1, 1, 0, -1])
    if not (0 <= sa[0x12] + dx < 21 and 0 <= sa[0x13] + dy < 21) is False and rnd.random() < 0.5: pass
    sa[1] |= 0x04
    t = w['recs'][sa[2]][0x23]; x, y = sa[0x12], sa[0x13]
    for (cx, cy) in [(x, y)] + ([(x + 1, y)] if t == 3 else []) + ([(x, y + 1)] if t == 2 else []):
        if cx < 21 and cy < 21: w['tiles'][cy * 21 + cx] &= 0x7F
    w['ca'][0] = a; w['g'][0xD496 - G0] = 1
    w['g'][0xD593 - G0] = rnd.choice([0, 0, 0, 1, 3]); w['g'][0xD592 - G0] = rnd.choice([0, 0, 1]); w['g'][0xD595 - G0] = rnd.choice([0, 0, 0x40])
    demo = rnd.choice([0, 0, 0, 1])
    load(m, w)
    m.write_ram(0xB3F4, struct.pack('>hh', dx, dy)); m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xBA5A, bytes([demo])); m.write_ram(0xEE00, bytes(0x200))
    script = [rnd.choice([0, 1, 2, 4, 8, 0x10, 0x20, 0x40, 0x80, 0x80, 0x20, 0x24, rnd.randrange(256)]) for _ in range(rnd.randrange(1, 10))]
    sq, used = list(script), [0]
    def next_pad():
        used[0] += 1
        return sq.pop(0) if sq else 0x20
    m.stub_fn(0xF1B66, next_pad)
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]; pr['shop'] = demo
    m.call(0xF898, max_insns=6000000, a3=RAM_BASE + SLOT + a * 26, a2=RAM_BASE + REC + sa[2] * 0xD6)
    if m.reg('pc') != 0x00FFF000: continue
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2)); sn['ret'] = struct.unpack('b', bytes([m.reg('d0') & 0xFF]))[0]
    sn['mv'] = list(struct.unpack('>hh', m.read_ram(0xB3F4, 4))); sn['polls'] = [0, used[0]]
    cases.append(dict(fn='retreat', pre=pr, a=a, range=0, mv=[dx, dy], pad=script, post=sn))

# ---- R. 0xE3A8: the rounds of a whole fight (the setup before and the clean-up after are not part of it)
def fight_world(rnd):
    w = manual_world(rnd); n = w['n']; npar = w['npar']
    for k in range(n):
        sa = w['slots'][k]
        sa[0] = rnd.choice([1, 1, 1, 1, 1, 1, 0x81]) if k else 1
        sa[1] = (sa[1] | (0x80 if (k >= npar or rnd.random() < 0.7) else 0)) & ~0x04
        sa[0x16] = rnd.randrange(0, 14); sa[0xE] = rnd.randrange(4, 40)
        if k >= npar: w['recs'][sa[2]][0x32] = 0; w['recs'][sa[2]][0x3B] = 0
    for k in range(npar):
        if k != w['actor'] and rnd.random() < 0.15: w['slots'][k][0] = 0x83; w['slots'][k][0xE] = rnd.randrange(0, 14)
    for r in range(8):
        if rnd.random() < 0.5: w['recs'][r][0x32] = 0; w['recs'][r][0x3B] = 0
    w['g'][0xD50E - G0] = 0xFF; w['g'][0xD50C - G0] = rnd.randrange(0, 4); w['g'][0xD50D - G0] = rnd.randrange(1, 5); w['g'][0xD505 - G0] = 0
    w['g'][0xD51D - G0:0xD51D - G0 + 0x40] = bytes([0xFF] * 0x40); w['g'][0xD51C - G0] = 0
    if rnd.random() < 0.3: w['d97dc'] = 0x10
    remark(w)
    return w


for _fi in range(N('fight', 300)):
    w = fight_world(rnd); n = w['n']
    if os.environ.get('FIGHT_RANGE'):
        lo, hi = map(int, os.environ['FIGHT_RANGE'].split(':'))
        if not lo <= _fi < hi:
            manual_script(rnd); rnd.choice([0, 0, 1, 2]); continue
    m = machine(rom, unstub=(0xE5E6,), extra=(0x15FDA, 0xF81C, 0xF780, 0xF7BA, 0xF838, 0x116F8, 0x11746, 0x1172A, 0x6C54, 0xAD46, 0x96D8, 0x1344E, 0xFF7C, 0x11898, 0xFDC8))
    m.actor_trace = True
    from unicorn import UC_HOOK_CODE as _HC
    from unicorn.m68k_const import UC_M68K_REG_A7 as _A7
    def clean_frame(uc, address, size, user):                       # the manual turn's frame variables (explosive flag, target index) are uninitialised stack in the ROM: start them at 0
        sp = uc.reg_read(_A7); uc.mem_write(sp - 0x100, bytes(0x100))
    m.uc.hook_add(_HC, clean_frame, begin=0xF2AE, end=0xF2AE)
    menu, pad = manual_script(rnd)
    mq, pq, used = [x for x in menu if x != 2], list(pad), [0, 0]
    def next_menu():
        used[0] += 1
        return (mq.pop(0) & 0xFFFF) if mq else 4
    def next_pad():
        used[1] += 1
        return pq.pop(0) if pq else 0x80
    m.stub_fn(0x1391A, next_menu); m.stub_fn(0xF1B66, next_pad)
    load(m, w)
    sur = rnd.choice([0, 0, 1, 2]); m.write_ram(0x9DC1, bytes([sur]))
    m.write_ram(0xB400, struct.pack('>HH', 504, 504)); m.write_ram(0xEE00, bytes(0x200)); m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xD8FC, b'\x00')
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]; pr['shop'] = sur
    sp = RAM_BASE + 0xF000 - 24
    m.uc.mem_write(sp + 20, struct.pack('>I', 0x00FFF000)); m.set_reg('a7', sp)
    m.set_reg('a3', RAM_BASE + SLOT)
    try: m.uc.emu_start(0xE3A8, 0x00FFF000, count=150_000_000)
    except Exception as e: continue
    if m.reg('pc') != 0x00FFF000: continue
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2)); sn['polls'] = used
    cases.append(dict(fn='fight', pre=pr, a=w['actor'], range=0, menu=[x & 0xFFFF for x in menu if x != 2], pad=pad, post=sn))

# ---- S. 0x149BA: the battlefield generator
for _ in range(N('terrain', 300)):
    m = machine(rom); w = sane_world(rnd); typ = rnd.randrange(0, 24)
    load(m, w); m.write_ram(0xD8CE, b'\x00\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5]))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]
    m.call(0x149BA, max_insns=20_000_000, d2=typ)
    sn = snap(m, w); sn['d8ca'] = [5, 5]
    cases.append(dict(fn='terrain', pre=pr, a=0, range=typ, post=sn))

# ---- T. 0x14738: placing both sides on the battlefield
for _ in range(N('deploy', 400)):
    m = machine(rom); w = sane_world(rnd, nmin=3, nmax=14); n = w['n']
    for i in range(441): w['tiles'][i] = rnd.choice([2, 2, 2, 3, 4, 5, 6, 7]) if rnd.random() < 0.9 else rnd.randrange(128)
    for sb in w['slots']:
        if rnd.random() < 0.1: sb[0] = rnd.choice([0, 0x41, 0x81, 0x82])
        if rnd.random() < 0.3: sb[0x12] = rnd.randrange(256)
    for r in w['recs']: r[0x23] = rnd.choice([0, 0, 0, 1, 2, 3])
    w['g'][0xD4FE - G0] = rnd.choice([0, 0, 1]); w['ca'][0] = 0
    load(m, w)
    face = rnd.randrange(4); mask = rnd.choice([0, 0, 0, 1, 2, 3, 4, 5, 8, 15, 6, 9, 0x13]); wide = rnd.choice([0, 0, 1])
    m.write_ram(0x9AFA, bytes([face])); m.write_ram(0xD8CC, bytes([mask])); m.write_ram(0xBA5D, bytes([wide])); m.write_ram(0xD8D0, bytes([rnd.randrange(3, 18), rnd.randrange(3, 18)]))
    m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5]))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]; pr['shop'] = face | (wide << 4) | (mask << 8)
    pr['money'] = m.ram_byte(0xD8D0) | (m.ram_byte(0xD8D1) << 8)
    m.call(0x14738, max_insns=20_000_000)
    sn = snap(m, w); sn['d8ca'] = [5, 5]
    cases.append(dict(fn='deploy', pre=pr, a=0, range=0, post=sn))

# ---- U. 0x1503C: the whole start of a fight (battlefield, deployment, control of the allies)
for _ in range(N('setup', 300)):
    m = machine(rom, extra=(0xB100, 0x132A6, 0x14D60, 0x85D6, 0x8360, 0x982A, 0xA768, 0xFF7C, 0x860E, 0x14FEA, 0xB834, 0x14F12, 0x14F60, 0x14FBC, 0xA71E, 0x8A76, 0x8A90, 0xB0DA, 0x862A, 0xAB00, 0xA85A, 0x8A44, 0xC8FC), retreat=0)
    w = sane_world(rnd, nmin=3, nmax=14, effects=rnd.random() < 0.3); n = w['n']; npar = w['npar']
    for i in range(441): w['tiles'][i] = rnd.randrange(128)
    for k, sb in enumerate(w['slots']):
        if rnd.random() < 0.1: sb[0] = rnd.choice([0, 0x41, 0x81, 0x82])
        sb[0x12] = rnd.randrange(256)
        if (k < npar and rnd.random() < 0.4) or rnd.random() < 0.1: sb[1] |= 0x40
        if rnd.random() < 0.4: sb[1] |= 0x80
        else: sb[1] &= 0x7F
    for r in w['recs']:
        r[0x23] = rnd.choice([0, 0, 0, 1, 2, 3])
        for i in range(5): r[0x31 + i] = rnd.choice([0, 3, 8, 15, 30])
        r[0x19] = rnd.randrange(1, 12)
    w['g'][0xD50E - G0] = rnd.choice([0, 0xFF]); w['g'][0xD50C - G0] = rnd.randrange(256); w['g'][0xD50D - G0] = rnd.randrange(256); w['ca'][0] = 0
    for i in range(0x60): w['g'][0xD49C - G0 + i] = rnd.randrange(256) if rnd.random() < 0.2 else 0
    if rnd.random() < 0.3: w['haz'] = bytearray(rnd.randrange(256) for _ in range(256))
    w['d97dc'] = rnd.choice([0xA2, 0xA8])
    load(m, w)
    face = rnd.randrange(4); mask = rnd.choice([0, 0, 0, 1, 2, 3, 4, 5, 8, 15, 6, 9]); wide = rnd.choice([0, 0, 1]); amb = rnd.choice([0, 0, 1]); area = rnd.randrange(0, 13)
    solo = rnd.choice([0, 0, 0, 1]); solom = rnd.randrange(8); mode0 = rnd.choice([1, 3, 2, 9])
    m.write_ram(0x9AFA, bytes([face])); m.write_ram(0xD8CC, bytes([mask])); m.write_ram(0xBA5D, bytes([wide])); m.write_ram(0x9DB6, bytes([amb])); m.write_ram(0x97AD, bytes([area]))
    m.write_ram(0xBA5B, bytes([solo])); m.write_ram(0x9DA7, bytes([solom])); m.write_ram(0x9BBC, bytes([mode0])); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xD57E, bytes(4)); m.write_ram(0x97DC, bytes([w['d97dc']]))
    m.write_ram(0xEE00, bytes(0x200))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = mode0; pr['d8ca'] = [5, 5]; pr['shop'] = face | (wide << 4) | (solo << 5) | (amb << 6); pr['money'] = mask | (area << 8) | (solom << 16)
    m.call(0x1503C, max_insns=40_000_000)
    if m.reg('pc') != 0x00FFF000: continue
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2)); sn['ret'] = m.ram_byte(0xD50E)
    cases.append(dict(fn='setup', pre=pr, a=0, range=0, post=sn))

# ---- V. 0xE394: a whole fight from its start (setup, rounds; the clean-up 0x15FDA is replaced by an empty routine)
for _fi in range(N('combat', 150)):
    w = fight_world(rnd); n = w['n']; npar = w['npar']
    _skip = bool(os.environ.get('COMBAT_RANGE')) and not (lambda lo, hi: lo <= _fi < hi)(*map(int, os.environ['COMBAT_RANGE'].split(':')))
    for i in range(441): w['tiles'][i] = rnd.randrange(128)
    for k, sb in enumerate(w['slots']):
        sb[0x12] = rnd.randrange(256); sb[0x17] = rnd.choice([0xFF, 0xFF, rnd.randrange(n)])
        if rnd.random() < 0.1: sb[1] |= 0x40
    w['g'][0xD50E - G0] = 0; w['d97dc'] = rnd.choice([0xA2, 0xA8])
    m = machine(rom, unstub=(0xE5E6,), extra=(0x15FDA, 0xF81C, 0xF780, 0xF7BA, 0xF838, 0x116F8, 0x11746, 0x1172A, 0x6C54, 0xAD46, 0x96D8, 0x1344E, 0xFF7C, 0x11898, 0xFDC8,
                                                0xB100, 0x132A6, 0x14D60, 0x85D6, 0x8360, 0x982A, 0xA768, 0x860E, 0x14FEA, 0xB834, 0x14F12, 0x14F60, 0x14FBC, 0xA71E, 0x8A76, 0x8A90, 0xB0DA, 0x862A, 0xAB00, 0xA85A, 0x8A44, 0xC8FC), retreat=1)
    m.actor_trace = True
    from unicorn import UC_HOOK_CODE as _HC
    from unicorn.m68k_const import UC_M68K_REG_A7 as _A7
    def clean_frame2(uc, address, size, user):
        sp = uc.reg_read(_A7); uc.mem_write(sp - 0x100, bytes(0x100))
    m.uc.hook_add(_HC, clean_frame2, begin=0xF2AE, end=0xF2AE)
    menu, pad = manual_script(rnd)
    mq, pq, used = [x for x in menu if x != 2], list(pad), [0, 0]
    def next_menu():
        used[0] += 1
        return (mq.pop(0) & 0xFFFF) if mq else 4
    def next_pad():
        used[1] += 1
        return pq.pop(0) if pq else 0x80
    m.stub_fn(0x1391A, next_menu); m.stub_fn(0xF1B66, next_pad)
    load(m, w)
    face = rnd.randrange(4); mask = rnd.choice([0, 0, 0, 1, 2, 3, 4, 5, 8, 15]); wide = rnd.choice([0, 0, 1]); amb = rnd.choice([0, 0, 1]); area = rnd.randrange(0, 13)
    sur = rnd.choice([0, 0, 1, 2])
    if _skip: continue
    m.write_ram(0x9AFA, bytes([face])); m.write_ram(0xD8CC, bytes([mask])); m.write_ram(0xBA5D, bytes([wide])); m.write_ram(0x9DB6, bytes([amb])); m.write_ram(0x97AD, bytes([area]))
    m.write_ram(0xBA5B, b'\x00'); m.write_ram(0x9BBC, bytes([rnd.choice([1, 3])])); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xD57E, bytes(4)); m.write_ram(0x97DC, bytes([w['d97dc']]))
    m.write_ram(0x9DC1, bytes([sur])); m.write_ram(0xB400, struct.pack('>HH', 504, 504)); m.write_ram(0xEE00, bytes(0x200)); m.write_ram(0x97AE, b'\x00'); m.write_ram(0xD8FC, b'\x00')
    pr = pre(w); pr['m97'] = 0; pr['mode'] = m.ram_byte(0x9BBC); pr['d8ca'] = [5, 5]; pr['shop'] = face | (wide << 4) | (amb << 6) | (sur << 8); pr['money'] = mask | (area << 8)
    try: m.call(0xE394, max_insns=250_000_000)
    except Exception as e: continue
    if m.reg('pc') != 0x00FFF000: continue
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2)); sn['polls'] = used
    cases.append(dict(fn='combat', pre=pr, a=0, range=0, menu=[x & 0xFFFF for x in menu if x != 2], pad=pad, post=sn))

# ---- W. 0x15FDA: the end of a fight (the medical aftermath 0x16B96 and the loot screen 0x165A0 are replaced by empty routines)
for _ in range(N('cleanup', 300)):
    m = machine(rom, extra=(0xCAC0, 0x85D6, 0x8360, 0x982A, 0xA768, 0x165A0, 0xC8FC, 0xAB5A, 0x16EF0, 0x40D0, 0xBEF4, 0x82EC, 0xAF00, 0x862A, 0xA71E, 0x8A76, 0x8A60, 0xB0DA, 0x8A90, 0x13376, 0x1368C, 0x11CA0, 0x13268, 0x6C54), retreat=0)
    w = sane_world(rnd, nmin=4, nmax=14, effects=rnd.random() < 0.3); n = w['n']; npar = w['npar']
    for k, sb in enumerate(w['slots']):
        sb[1] |= rnd.choice([0, 0, 0x04, 0x10, 0x14])
        sb[0] = rnd.choice([1, 1, 0x81, 0x82, 0x83, 0x84, 0x85, 0x45, 0x01, 0xC1, 0x86, 0x05, 0x83, 0x84, 0x86, 0x87, 0x03, 0x04, 0x06]) if k < npar else rnd.choice([1, 0x81, 0x82, 0x86, 0x81, 0x81, 0x41])
    for r in w['recs']:
        r[0x40] = rnd.randrange(256); r[0x41] = rnd.randrange(256); r[0x52] = rnd.choice([0, 0, 1]); r[0x1A:0x1E] = bytes([0, 0, rnd.randrange(4), rnd.randrange(256)]); r[0x1E:0x22] = bytes([0, 0, rnd.randrange(256), rnd.randrange(256)])
        r[0x32] = rnd.choice([0, 0, 2, 5, 9]); r[0x3B] = rnd.choice([0, 0, 3, 8]); r[0x2E] = rnd.randrange(10, 60)
        if rnd.random() < 0.1: r[0x43 + rnd.randrange(10)] = 3
        for g in range(13):
            o = 0x54 + 10 * g
            if rnd.random() < 0.08: r[o:o + 10] = bytes([0x1F, 0, 0, 0, 0, 0, 0, 0, rnd.choice([1, 1, 2]), 0])
            elif rnd.random() < 0.4: r[o:o + 10] = bytes(10)
            else: r[o:o + 10] = bytes([rnd.randrange(1, 40), 0, 0, 0, rnd.choice([0, 1]), rnd.choice([0, 0x40, 0x80, 0xC0, 0x30, 0xF0]), 0, 0, rnd.choice([0, 0, 1, 5, 0x81]), rnd.choice([0, 1, 2])])
    for sb in w['slots']: sb[0xE] = rnd.randrange(0, 60)
    w['g'][0xD50E - G0] = rnd.choice([0, 0xFF]); w['g'][0xD50B - G0] = rnd.choice([0, 0xFF]); w['ca'][0] = 0
    w['g'][0xD514 - G0:0xD518 - G0] = bytes(4); w['g'][0xD57E - G0:0xD582 - G0] = bytes([1, 2, 3, 4])
    load(m, w)
    shop = rnd.choice([0, 0, 0, 1]); demo = rnd.choice([0, 0, 0, 0, 1]); solo = rnd.choice([0, 0, 0, 1]); solom = rnd.randrange(npar)
    m97 = rnd.choice([0, 0, 0, 1]); cnt = rnd.randrange(0, 6); loot = bytes(rnd.randrange(1, 40) for _ in range(16)); s58 = rnd.choice([0, 0, 1]); s30 = rnd.choice([0, 0, 0xFF]); s27 = rnd.choice([0, 1]); s24 = rnd.randrange(0, 8)
    m.write_ram(0xBA60, bytes([shop])); m.write_ram(0xBA5A, bytes([demo])); m.write_ram(0xBA5B, bytes([solo])); m.write_ram(0x9DA7, bytes([solom])); m.write_ram(0x97AE, bytes([m97]))
    m.write_ram(0xB9F3, bytes([cnt]) + loot); m.write_ram(0x6AF6, bytes(rnd.randrange(256) for _ in range(140))); m.write_ram(0xBA34, struct.pack('>I', rnd.choice([0, 0, rnd.randrange(1000)])))
    m.write_ram(0x9BD0, struct.pack('>I', rnd.randrange(100000))); m.write_ram(0x9858, bytes([s58])); m.write_ram(0x9930, bytes([s30])); m.write_ram(0x9927, bytes([s27])); m.write_ram(0x9924, bytes([s24]))
    m.write_ram(0xD8CC, bytes([rnd.randrange(16)])); m.write_ram(0xD8DA, bytes([rnd.randrange(256)])); m.write_ram(0x9BBD, bytes([rnd.randrange(8)])); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xD57E, bytes([1, 2, 3, 4]))
    m.write_ram(0xEE00, bytes(0x200))
    pr = pre(w); pr['m97'] = m97; pr['mode'] = 2; pr['d8ca'] = [5, 5]; pr['shop'] = shop | (demo << 1) | (solo << 2) | (solom << 4) | (s58 << 8) | ((1 if s30 else 0) << 9) | (s27 << 10) | (s24 << 11)
    pr['money'] = cnt; pr['pool'] = loot.hex() + m.read_ram(0x6AF6, 140).hex() + m.read_ram(0xBA34, 4).hex() + m.read_ram(0x9BD0, 4).hex() + m.read_ram(0xD8CC, 1).hex() + m.read_ram(0xD8DA, 1).hex() + m.read_ram(0x9BBD, 1).hex()
    if os.environ.get('DBG_WR'):
        from unicorn import UC_HOOK_MEM_WRITE
        m.uc.hook_add(UC_HOOK_MEM_WRITE, lambda uc, t, a, sz, v, u: print('write 992x at', hex(a), 'pc', hex(uc.reg_read(__import__('unicorn.m68k_const', fromlist=['x']).UC_M68K_REG_PC)), v), begin=0xFFFF9920, end=0xFFFF992F)
    from unicorn import UC_HOOK_CODE as _HC2
    from unicorn.m68k_const import UC_M68K_REG_PC as _PC2
    m.uc.hook_add(_HC2, lambda uc, a, sz, u: uc.reg_write(_PC2, 0x00FFF000), begin=0x7588, end=0x7588)          # 0x7588 (game over) never returns: end the run there
    lastpc = []
    m.uc.hook_add(_HC2, lambda uc, a, sz, u: (lastpc.append(a), lastpc.__delitem__(0) if len(lastpc) > 12 else None), begin=0x15000, end=0x17000)
    if os.environ.get('DBG_WR'): print('pre9927', m.ram_byte(0x9927), 's27', s27, 's30', m.ram_byte(0x9930), 'demo', demo)
    try: m.call(0x15FDA, max_insns=20_000_000)
    except Exception as e: print('cleanup error', e, hex(m.reg('pc')), [hex(x) for x in lastpc[-8:]]); continue
    if m.reg('pc') != 0x00FFF000: continue
    sn = snap(m, w); sn['d8ca'] = list(m.read_ram(0xD8CA, 2))
    sn['misc'] = [m.ram_byte(0xB9F3), struct.unpack('>I', m.read_ram(0xBA34, 4))[0], struct.unpack('>I', m.read_ram(0x9BD0, 4))[0], m.ram_byte(0xD8DA), m.ram_byte(0xD8CC), m.ram_byte(0x9DBD), m.ram_byte(0x9BBC), m.ram_byte(0xBA5E), m.ram_byte(0x9858), m.ram_byte(0x9930), m.ram_byte(0x9927)]
    sn['pool'] = m.read_ram(0x6AF6, 140).hex()
    cases.append(dict(fn='cleanup', pre=pr, a=0, range=0, post=sn))

# ---- X. 0x165A0: the loot sharing screen (menu 0x1391A, quantity box 0x16834 and the "leave items behind?" box 0x136DA scripted)
for _ in range(N('loot', 400)):
    w = inv_world(rnd); a = w['actor']; n = w['n']
    shop = rnd.choice([0, 0, 1]); money = rnd.choice([0, 100, 5000, 65000, rnd.randrange(1 << 20)]); m97 = 0; fac = rnd.randrange(16, 64)
    cnt = rnd.randrange(1, 15); pool = bytearray(140)
    for k in range(14):
        if k < cnt or rnd.random() < 0.2:
            pool[10 * k:10 * k + 10] = bytes([rnd.randrange(1, 40), 0, 0, 0, rnd.choice([0, 0, 1]), rnd.choice([0, 0, 0x10]), 0, 0, 0, rnd.choice([0, 1, 2, 3])]) if rnd.random() < 0.9 else bytes(10)
            pr_ = rnd.randrange(16, 3000); pool[10 * k + 6] = pr_ >> 8; pool[10 * k + 7] = pr_ & 255
            pool[10 * k + 8] = rnd.choice([0, 0, 1, 5, 30, 200, 0x81, 0x81, 0x82, 0x85, 250])
    m = machine(rom, extra=(0xC940, 0x16954, 0x1692A, 0x16B28, 0x169A4, 0x85D6, 0x6C54), retreat=None)
    stay = rnd.choice([0, 0, 1, 2]); prompts = []
    def prompt_fn():
        a_ = 0 if len(prompts) < stay else 1
        prompts.append(a_); return a_
    m.stub_fn(0x136DA, prompt_fn)
    from unicorn import UC_HOOK_CODE
    from unicorn.m68k_const import UC_M68K_REG_A7, UC_M68K_REG_PC, UC_M68K_REG_D2, UC_M68K_REG_SR
    menus, qtys, qmaxs = [], [], []
    limit = rnd.randrange(3, 22)
    def menu_fn():
        lst = []; ad = 0xD564
        while m.ram_byte(ad) < 0x80: lst.append(m.ram_byte(ad)); ad += 1
        cands = [c for c in range(23) if c not in lst and c != 8]
        if len(menus) >= limit or not cands or rnd.random() < 0.08: ans = 8
        elif rnd.random() < 0.03: ans = 0xFFFF
        else: ans = rnd.choice(cands)
        menus.append(ans)
        for ad in (0xD593, 0xD595, 0xD592, 0xD597, 0xD596): m.write_ram(ad, b'\x00')
        return ans
    m.stub_fn(0x1391A, menu_fn)
    m.stub_fn(0x13DA2, lambda: (m.write_ram(0xD564, b'\xFF'), 0)[1])
    def add_fn():
        c = m.reg('d0') & 0xFF; ad = 0xD564
        while m.ram_byte(ad) < 0x80: ad += 1
        m.write_ram(ad, bytes([c, 0xFF])); return c
    m.stub_fn(0x13E56, add_fn)
    def qty_hook(uc, address, size, user):
        if address != 0x16834: return
        mx = uc.reg_read(UC_M68K_REG_D2) & 0xFF
        q = rnd.choice([0, 1, mx, rnd.randrange(0, mx + 1)]) if rnd.random() > 0.1 else 0
        qtys.append(q); qmaxs.append(mx)
        sp = uc.reg_read(UC_M68K_REG_A7); ret = struct.unpack('>I', bytes(uc.mem_read(sp, 4)))[0]
        uc.reg_write(UC_M68K_REG_A7, sp + 4); uc.reg_write(UC_M68K_REG_D2, q)
        uc.reg_write(UC_M68K_REG_SR, (uc.reg_read(UC_M68K_REG_SR) & ~0xF) | (0x4 if q == 0 else 0)); uc.reg_write(UC_M68K_REG_PC, ret)
    m.uc.hook_add(UC_HOOK_CODE, qty_hook, begin=0x16834, end=0x16834)
    load(m, w)
    m.write_ram(0x97AE, b'\x00'); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0xBA60, bytes([shop])); m.write_ram(0x9BD0, struct.pack('>I', money)); m.write_ram(0x9E63, bytes([fac]))
    m.write_ram(0xB9F3, bytes([cnt])); m.write_ram(0x6AF6, bytes(pool)); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xEE00, bytes(0x200))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]; pr['shop'] = shop | (fac << 8); pr['money'] = money
    pr['pool'] = bytes(pool).hex() + '%02x' % cnt
    try: m.call(0x165A0, max_insns=20_000_000)
    except Exception as e: print('loot error', e, hex(m.reg('pc'))); continue
    if m.reg('pc') != 0x00FFF000: continue
    sn = snap(m, w); sn['d8ca'] = [5, 5]; sn['money'] = struct.unpack('>I', m.read_ram(0x9BD0, 4))[0]; sn['polls'] = [len(menus), len(qtys)]; sn['pool'] = m.read_ram(0x6AF6, 140).hex()
    cases.append(dict(fn='loot', pre=pr, a=a, range=0, mv=prompts, menu=menus, pad=qtys, qmax=qmaxs, post=sn))

# ---- Y. 0x16EF0: the end of a scripted (starship) fight: the crew repairs the ship
for _ in range(N('ship', 300)):
    w = sane_world(rnd, nmin=4, nmax=10); n = w['n']
    for r in w['recs']:
        for i in range(14): r[0x31 + i] = rnd.choice([0, 0, 3, 8, 15, 30])
        r[0x19] = rnd.randrange(1, 12)
    for sb in w['slots']: sb[0] = rnd.choice([1, 1, 1, 0x81, 0x41, 1])
    m = machine(rom, extra=(0x175E0, 0x862A, 0x8360, 0x171E0, 0x17270, 0x17368, 0x175BE, 0x17546, 0x176DC, 0x860E, 0x85D6), retreat=0)
    ship = bytearray(0x30)
    for off in (0, 2, 6):
        mx = rnd.randrange(100, 700); cur = max(0, mx - rnd.choice([0, 0, rnd.randrange(0, 400)]))
        ship[off] = mx >> 8; ship[off + 1] = mx & 255; ship[off + 0x24] = cur >> 8; ship[off + 0x25] = cur & 255
    for k in range(4):
        mx = rnd.randrange(0, 20); cur = max(0, mx - rnd.choice([0, 0, rnd.randrange(0, 12)])); a = 0x0C + 5 * k
        ship[a + 1] = mx; ship[a + 2] = cur
    load(m, w); w['ca'][0] = 0
    m.write_ram(0x98F6, bytes(ship)); m.write_ram(0x9BBC, b'\x02'); m.write_ram(0x97AE, b'\x00'); m.write_ram(0xD8CA, bytes([5, 5])); m.write_ram(0xEE00, bytes(0x200))
    pr = pre(w); pr['m97'] = 0; pr['mode'] = 2; pr['d8ca'] = [5, 5]; pr['pool'] = bytes(ship).hex()
    try: m.call(0x16EF0, max_insns=20_000_000)
    except Exception as e: print('ship error', e, hex(m.reg('pc'))); continue
    if m.reg('pc') != 0x00FFF000: continue
    sn = snap(m, w); sn['d8ca'] = [5, 5]; sn['pool'] = m.read_ram(0x98F6, 0x30).hex(); sn['misc'] = [m.ram_byte(0x9BBC)]
    cases.append(dict(fn='ship', pre=pr, a=0, range=0, post=sn))

out['cases'] = cases
json.dump(out, gzip.open(sys.argv[2], 'wt'), separators=(',', ':'))
import collections; print(collections.Counter(c['fn'] for c in cases))
