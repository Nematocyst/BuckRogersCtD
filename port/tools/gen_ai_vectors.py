"""Monster pathfinding / target-selection vectors from the real ROM routine 0x15D8A (breadth-first search over the 21x21 combat grid).
usage: python gen_ai_vectors.py ROM OUT.json"""
import sys, json, random, struct, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(15884)
S32 = lambda v: ((v + 2**31) % 2**32) - 2**31
DX = [0, 1, 1, 1, 0, -1, -1, -1]; DY = [-1 + 0, 0, 0, 0, 0, 0, 0, 0]       # replaced below from the ROM table

cases = []
for n in range(600):
    m = Machine(rom)
    nslots = rnd.randrange(3, 21)
    slots = []
    for sidx in range(nslots):
        if sidx < 8:
            fl0 = rnd.choice([0, 1, 1, 1, 1, 0x80, 0x83, 0x41]); fl1 = rnd.choice([0x00, 0x00, 0x10, 0x20, 0x04])
        else:
            fl0 = rnd.choice([0, 1, 1, 1, 0x80, 0x83, 0x84, 0x41, 0x81]); fl1 = rnd.choice([0x01, 0x01, 0x11, 0x21, 0x05, 0x00])
        x = rnd.randrange(21); y = rnd.randrange(21)
        rec = rnd.randrange(11)
        slots.append(dict(f0=fl0, f1=fl1, x=x, y=y, rec=rec, t17=rnd.randrange(256)))
    rectypes = [rnd.choice([0, 0, 0, 1, 2, 3, 3, 2, 4]) for _ in range(11)]
    for sidx, s in enumerate(slots):
        sb = bytearray(26); sb[0] = s['f0']; sb[1] = s['f1']; sb[2] = s['rec']; sb[0x12] = s['x']; sb[0x13] = s['y']; sb[0x17] = s['t17']
        m.write_ram(0xC470 + sidx * 26, bytes(sb))
    for r in range(11): m.write_ram(0xBA68 + r * 0xD6 + 0x23, bytes([rectypes[r]]))
    m.write_ram(0xBA64, struct.pack('>H', nslots))
    # combat map: random tiles, markers at live slot cells (and the extra cell of wide/tall monsters), plus some stray markers
    tiles = [rnd.choice([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, rnd.randrange(128)]) for _ in range(441)]
    for s in slots:
        if s['f0'] != 0 and (s['f0'] & 0xC0) == 0 and (s['f1'] & 4) == 0 or rnd.random() < 0.3:
            tiles[s['y'] * 21 + s['x']] |= 0x80
            t = rectypes[s['rec']]
            if t == 3 and s['x'] + 1 < 21: tiles[s['y'] * 21 + s['x'] + 1] |= 0x80
            if t == 2 and s['y'] + 1 < 21: tiles[s['y'] * 21 + s['x'] + 21] |= 0x80
    for _ in range(rnd.choice([0, 5, 20])): tiles[rnd.randrange(441)] |= 0x80
    m.write_ram(0xCACA, bytes(tiles))
    openness = rnd.choice([0.97, 0.9, 0.8])
    flags_table = [0 if rnd.random() < openness else rnd.choice([0x20, 0x20, 0x20, rnd.randrange(256)]) for _ in range(129)]
    m.write_ram(0xCFFF, bytes(flags_table))             # index -1 is the byte before the table
    m.write_ram(0xD810, struct.pack('>I', 0xFFFF0000 + 0xD000))
    mode = rnd.choice([0, 0, 0, 1, 2]); cnt506 = rnd.randrange(0, 4)
    lst = [rnd.randrange(256) for _ in range(12)]
    m.write_ram(0xD505, bytes([mode])); m.write_ram(0xD506, bytes([cnt506])); m.write_ram(0xCA22, bytes(lst))
    # actor: a live slot if possible
    live = [i for i, s in enumerate(slots) if s['f0'] != 0 and (s['f0'] & 0xC0) == 0]
    actor = rnd.choice(live) if live and rnd.random() < 0.9 else rnd.randrange(nslots)
    if mode in (1, 2) and rnd.random() < 0.8:
        cands = [i for i in range(nslots) if i != actor]
        fi = rnd.choice(cands)
        sb = bytearray(m.read_ram(0xC470 + fi * 26, 26)); sb[0] = rnd.choice([0x80, 0x83, 0x84]); sb[1] &= ~4 & 0xFF
        m.write_ram(0xC470 + fi * 26, bytes(sb))
        slots[fi].update(f0=sb[0], f1=sb[1])
        if mode == 1:
            sb2 = bytearray(m.read_ram(0xC470 + actor * 26, 26)); sb2[0x17] = fi; m.write_ram(0xC470 + actor * 26, bytes(sb2)); slots[actor]['t17'] = fi
        else:
            lst[0] = fi; cnt506 = max(cnt506, 1); m.write_ram(0xD506, bytes([cnt506])); m.write_ram(0xCA22, bytes(lst))
    # uninitialised stack garbage the ROM reads: -0xC(a6) -> RAM 0xEFEC (a6 = 0xEFF8 after the link)
    garbage = bytes(rnd.randrange(256) for _ in range(0x60))
    m.write_ram(0xEFA0, garbage)
    wave0 = struct.unpack('>H', m.read_ram(0xEFEC, 2))[0]
    last0 = rnd.randrange(256); m.write_ram(0x6CAE, bytes([last0]))
    m.write_ram(0xD5F8, bytes([0x55] * 16))
    m.call(0x15D8A, max_insns=20_000_000, d0=actor)
    path = []
    for b in m.read_ram(0x6CB0, 64):
        if b == 0xFF: break
        path.append(b)
    cases.append(dict(n=nslots, slots=slots, rectypes=rectypes, tiles=bytes(tiles).hex(), flags=bytes(flags_table).hex(), mode=mode, cnt=cnt506, lst=bytes(lst).hex(), actor=actor,
                      wave0=wave0, last0=last0, path=path, t17=m.read_ram(0xC470 + actor * 26 + 0x17, 1)[0]))
json.dump(dict(cases=cases), open(sys.argv[2], 'w'), separators=(',', ':'))
import collections
print(len(cases), 'path lengths', sorted(collections.Counter(len(c['path']) for c in cases).items())[:12], 'target changed', sum(c['t17'] != c['slots'][c['actor']]['t17'] for c in cases))
