"""Action-logic vectors from the real ROM routines: wound model 0x760A (damage applied to a creature),
weapon choice 0xE89C, movement step 0xF898 and the monster turn 0xEF64 (added piece by piece).
usage: python gen_action_vectors.py ROM OUT.json"""
import sys, json, random, struct, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(760)
S32 = lambda v: ((v + 2**31) % 2**32) - 2**31
out = {}

# ---- A. wound model 0x760A: a3 = victim slot, d0 = damage byte
wound = []
for n in range(300):
    m = Machine(rom)
    nslots = rnd.randrange(3, 20)
    slots = []
    for k in range(nslots):
        sb = bytearray(rnd.randrange(256) for _ in range(26))
        sb[0] = rnd.choice([0x01, 0x01, 0x01, 0x81, 0x11, 0x05, 0x00])
        sb[1] = rnd.choice([0x00, 0x01, 0x10, 0x11, 0x04])
        sb[2] = k if k < 8 else 8 + rnd.randrange(3)
        sb[0x12] = rnd.randrange(21); sb[0x13] = rnd.randrange(21)
        sb[0xE] = rnd.choice([0, 1, 5, 12, 30, rnd.randrange(256)])
        m.write_ram(0xC470 + k * 26, bytes(sb)); slots.append(list(sb))
    rectypes = [rnd.choice([0, 0, 1, 2, 3, 4]) for _ in range(11)]
    for r in range(11): m.write_ram(0xBA68 + r * 0xD6 + 0x23, bytes([rectypes[r]]))
    tiles = [rnd.randrange(256) for _ in range(441)]
    m.write_ram(0xCACA, bytes(tiles))
    v = rnd.randrange(nslots)
    dmg = rnd.choice([0, 1, 3, 10, 20, 40, rnd.randrange(256), slots[v][0xE], slots[v][0xE] + 1, slots[v][0xE] + 9, slots[v][0xE] + 10, slots[v][0xE] + 24, slots[v][0xE] + 25]) & 0xFF
    mode = rnd.choice([0, 2, 2, 5]); d8ca = [rnd.randrange(256), rnd.randrange(256)]
    m.write_ram(0x9BBC, bytes([mode])); m.write_ram(0xD8CA, bytes(d8ca)); m.write_ram(0xBA64, struct.pack('>H', nslots))
    m.call(0x760A, a3=0xFFFF0000 + 0xC470 + v * 26, d0=dmg)
    wound.append(dict(slots=[bytes(x).hex() for x in slots], rectypes=rectypes, tiles=bytes(tiles).hex(), v=v, dmg=dmg, mode=mode, d8ca=d8ca, n=nslots,
                      after=m.read_ram(0xC470 + v * 26, 26).hex(), d8ca_after=list(m.read_ram(0xD8CA, 2)), tiles_after=bytes(m.read_ram(0xCACA, 441)).hex()))
out['wound'] = wound

# ---- B. line of fire 0x15B5A: d0,d1 = x0,y0 ; d2,d3 = x1,y1 ; d4 = range
lof = []
for n in range(500):
    m = Machine(rom)
    tile_mode = rnd.choice(['mixed', 'open', 'sparse'])
    if tile_mode == 'mixed': tiles = [rnd.choice([0, 1, 2, 3, 4, 5, 6, rnd.randrange(128)]) | (0x80 if rnd.random() < 0.1 else 0) for _ in range(441)]
    elif tile_mode == 'open': tiles = [rnd.choice([2, 3, 4, 5]) for _ in range(441)]
    else: tiles = [rnd.choice([2, 2, 2, 2, 3, 0, 1, rnd.randrange(128)]) for _ in range(441)]
    m.write_ram(0xCACA, bytes(tiles))
    ft = [rnd.choice([0, 0, 0, 0x80, 0x40, 0xC0, rnd.randrange(256)]) for _ in range(129)]
    m.write_ram(0xCFFF, bytes(ft)); m.write_ram(0xD810, struct.pack('>I', 0xFFFF0000 + 0xD000))
    x0, y0, x1, y1 = rnd.randrange(21), rnd.randrange(21), rnd.randrange(21), rnd.randrange(21)
    if rnd.random() < 0.05: x1, y1 = x0, y0
    rng_ = rnd.choice([0, 1, 2, 3, 5, 8, 12, 20, rnd.randrange(0, 60), rnd.randrange(256)])
    d504 = rnd.choice([0, 0, 1]); d4ff = rnd.choice([0, 0, 1])
    m.write_ram(0xD504, bytes([d504])); m.write_ram(0xD4FF, bytes([d4ff])); m.write_ram(0xD501, b'\xee\xee\xee')
    m.call(0x15B5A, d0=x0, d1=y0, d2=x1, d3=y1, d4=rng_)
    lof.append(dict(tiles=bytes(tiles).hex(), ft=bytes(ft).hex(), x0=x0, y0=y0, x1=x1, y1=y1, rng=rng_, d504=d504, d4ff=d4ff,
                    out=[m.reg('d0') & 0xFFFF, m.reg('d1') & 0xFFFF, m.reg('d2') & 0xFFFF, m.reg('d3') & 0xFFFF], f=list(m.read_ram(0xD501, 3)), d504_after=m.ram_byte(0xD504)))
out['lof'] = lof

# ---- C. equipment -> combat stats 0x6D1E: a0 = slot, a1 = record
ITEMS = [0] * 4 + list(range(1, 39)) + [0x18, 0x18, 0x20, 0x30, 0x7F, 0xFF]
stats = []
for n in range(400):
    m = Machine(rom)
    rec = bytearray(rnd.randrange(256) for _ in range(0xD6))
    for k in range(13): rec[0x54 + 10 * k] = rnd.choice(ITEMS) if rnd.random() < 0.5 else 0         # gear list
    rec[0xAE] = rnd.choice(ITEMS)                                                                   # primary weapon
    rec[0xC2] = rnd.choice([0, 0, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, rnd.choice(ITEMS)])  # worn armor
    for off in (0xAE + 4, 0xC2 + 4): rec[off] = rnd.choice([0, 0, 1, 2, 0xFF, rnd.randrange(256)])
    rec[0x10] = rnd.choice([rnd.randrange(3, 19), rnd.randrange(256)]); rec[0x11] = rnd.choice([rnd.randrange(3, 19), rnd.randrange(256)])
    for k in range(5): rec[0x4D + k] = rnd.choice([0, rec[0xAE], rnd.choice(ITEMS)])
    rec[0x24] = rnd.choice([rnd.randrange(3, 20), rnd.randrange(256)])
    slot = bytearray(rnd.randrange(256) for _ in range(26))
    ridx = rnd.randrange(11); slot[2] = ridx
    slot[1] = rnd.choice([0x00, 0x01, 0x20, 0x21, rnd.randrange(256)])
    d499, d49a = rnd.choice([0, 0, 1, 0xFF, rnd.randrange(256)]), rnd.choice([0, 0, 1, 0xFF, rnd.randrange(256)])
    m97 = rnd.choice([0, 0, 1])
    m.write_ram(0xBA68 + ridx * 0xD6, bytes(rec)); m.write_ram(0xC470, bytes(slot))
    m.write_ram(0xD499, bytes([d499, d49a])); m.write_ram(0x97AE, bytes([m97]))
    m.call(0x6D1E, a0=0xFFFF0000 + 0xC470, a1=0xFFFF0000 + 0xBA68 + ridx * 0xD6)
    stats.append(dict(slot=bytes(slot).hex(), rec=bytes(rec).hex(), ridx=ridx, d499=d499, d49a=d49a, m97=m97,
                      slot_after=m.read_ram(0xC470, 26).hex(), flags2f=m.ram_byte(0xBA68 + ridx * 0xD6 + 0x2F)))
out['stats'] = stats

# ---- D. attack preparation 0x10400 (target, armor by facing, stats recompute, range, line of fire, to-hit); UI calls 0x664E / 0xAD5A stubbed
def make_record(rnd, ridx, party):
    rec = bytearray(rnd.randrange(256) for _ in range(0xD6))
    for k in range(13): rec[0x54 + 10 * k] = rnd.choice(ITEMS) if rnd.random() < 0.4 else 0
    rec[0xAE] = rnd.choice([0, 0, 1, 2, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15, 17, 18, 20, 30, 32, 33, 34, 36, 38])
    rec[0xAE + 4] = rnd.choice([0, 1, 2, 3]); rec[0xAE + 9] = rnd.choice([0, 1, 5, 6, 7, 8, 9, 10, 11, 12, 13, rnd.randrange(256)]); rec[0xAE + 5] = rnd.choice([0, 0, 0, 0x10, 0x20, 0x80, 0xC0, rnd.randrange(256)])
    rec[0xC2] = rnd.choice([0, 0, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B]); rec[0xC2 + 4] = rnd.choice([0, 0, 1, 2])
    rec[0x10] = rnd.randrange(3, 19); rec[0x11] = rnd.randrange(3, 19); rec[0x12] = rnd.randrange(3, 19)
    rec[0x18] = rnd.choice([0, 1, 2, 3, 4]); rec[0x19] = rnd.randrange(1, 10)
    rec[0x24] = rnd.randrange(8, 20); rec[0x25] = rnd.choice([50, 52, 54, 56]); rec[0x22] = rnd.randrange(38, 50)
    rec[0x23] = rnd.choice([0, 0, 0, 1, 2, 3])
    for k in range(5): rec[0x4D + k] = rnd.choice([0, rec[0xAE]])
    return rec

prep = []
for n in range(400):
    m = Machine(rom)
    m.stub_rts(0x664E); m.stub_rts(0xAD5A)
    nslots = rnd.randrange(2, 14)
    slots = []; recs = [make_record(rnd, r, r < 8) for r in range(11)]
    live = []
    for k in range(nslots):
        sb = bytearray(rnd.randrange(256) for _ in range(26))
        sb[0] = rnd.choice([0x01, 0x01, 0x01, 0x01, 0x81, 0x11, 0x00])
        sb[1] = rnd.choice([0x00, 0x01, 0x20, 0x21, 0x04, 0x05, 0x10, 0x11])
        sb[2] = k if k < 8 else 8 + rnd.randrange(3)
        sb[0x12] = rnd.randrange(21); sb[0x13] = rnd.randrange(21); sb[0x10] = rnd.randrange(8)
        sb[0xE] = rnd.randrange(1, 60)
        slots.append(bytearray(sb))
    for r in range(11): m.write_ram(0xBA68 + r * 0xD6, bytes(recs[r]))
    for k in range(nslots): m.write_ram(0xC470 + k * 26, bytes(slots[k]))
    m.write_ram(0xBA64, struct.pack('>H', nslots))
    att = rnd.randrange(nslots); tgt = rnd.choice([i for i in range(nslots) if i != att] + [0xFF])
    if rnd.random() < 0.7:                                  # make the pair opposite sides most of the time
        slots[att][1] = (slots[att][1] & ~1) | 1; m.write_ram(0xC470 + att * 26 + 1, bytes([slots[att][1]]))
        if tgt != 0xFF: slots[tgt][1] &= ~1; m.write_ram(0xC470 + tgt * 26 + 1, bytes([slots[tgt][1]]))
    if rnd.random() < 0.7:
        for k in (att, tgt):
            if k != 0xFF: slots[k][0] = 0x01; m.write_ram(0xC470 + k * 26, bytes(slots[k]))
    if tgt != 0xFF and rnd.random() < 0.8:                       # target near the attacker so ranges are met
        ax, ay = slots[att][0x12], slots[att][0x13]
        slots[tgt][0x12] = max(0, min(20, ax + rnd.randrange(-4, 5))); slots[tgt][0x13] = max(0, min(20, ay + rnd.randrange(-4, 5)))
        m.write_ram(0xC470 + tgt * 26, bytes(slots[tgt]))
    tiles = [rnd.choice([2, 2, 2, 2, 2, 3, 4, 5, 6, 0, 1, rnd.randrange(128)]) | (0x80 if rnd.random() < 0.05 else 0) for _ in range(441)]
    m.write_ram(0xCACA, bytes(tiles))
    ft = [rnd.choice([0, 0, 0, 0, 0, 0, 0x80, 0x40, rnd.randrange(256)]) for _ in range(129)]
    m.write_ram(0xCFFF, bytes(ft)); m.write_ram(0xD810, struct.pack('>I', 0xFFFF0000 + 0xD000))
    m97 = rnd.choice([0, 0, 0, 1]); d4ff = rnd.choice([0, 0, 1]); d499 = rnd.choice([0, 0, 1, 255]); d49a = rnd.choice([0, 0, 1, 2])
    if rnd.random() < 0.5: cx, cy = (slots[att][0x12] + rnd.randrange(-3, 4)) * 24 + 5, (slots[att][0x13] + rnd.randrange(-3, 4)) * 24 + 7
    else: cx, cy = rnd.randrange(0, 21 * 24), rnd.randrange(0, 21 * 24)
    cx, cy = max(0, min(cx, 20 * 24 + 23)), max(0, min(cy, 20 * 24 + 23))
    d496 = rnd.randrange(256)
    m.write_ram(0x97AE, bytes([m97])); m.write_ram(0xD4FF, bytes([d4ff])); m.write_ram(0xD499, bytes([d499, d49a]))
    m.write_ram(0xB3F0, struct.pack('>HH', cx, cy)); m.write_ram(0xCA20, bytes([att])); m.write_ram(0xD496, bytes([d496])); m.write_ram(0xD4FD, bytes([rnd.randrange(256)]))
    d4fd = m.ram_byte(0xD4FD)
    m.write_ram(0xD511, b'\xee'); m.write_ram(0xD518, b'\xff\xff'); m.write_ram(0xD504, b'\x00')
    m.call(0x10400, a3=0xFFFF0000 + 0xC470 + att * 26, a2=0xFFFF0000 + 0xBA68 + slots[att][2] * 0xD6, d0=tgt, d2=rnd.randrange(21))
    after_slots = [m.read_ram(0xC470 + k * 26, 26).hex() for k in range(nslots)]
    prep.append(dict(n=nslots, slots=[bytes(x).hex() for x in slots], recs=[bytes(x).hex() for x in recs], att=att, tgt=tgt, tiles=bytes(tiles).hex(), ft=bytes(ft).hex(),
                     m97=m97, d4ff=d4ff, d499=d499, d49a=d49a, cx=cx, cy=cy, d496=d496, d4fd=d4fd,
                     out=dict(d511=m.ram_byte(0xD511), d518=struct.unpack('>H', m.read_ram(0xD518, 2))[0], d496=m.ram_byte(0xD496), d512=m.ram_byte(0xD512), d513=m.ram_byte(0xD513)),
                     after_slots=after_slots, flags2f=[m.ram_byte(0xBA68 + r * 0xD6 + 0x2F) for r in range(11)]))
out['prep'] = prep

json.dump(out, open(sys.argv[2], 'w'), separators=(',', ':'))
print({k: len(v) for k, v in out.items()})
