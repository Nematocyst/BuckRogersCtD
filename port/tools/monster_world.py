"""Monster-turn world builders and ROM harness.

Every case stores the RAM the routines touch before and after: slots, records, tile map, terrain flags, scratch globals 0xD490..0xD5FF,
actor/target-list area 0xCA20..0xCA7F and the RNG state. Shared helpers for the monster-turn vector generators (ROM path: first command line argument or $ROM)"""
import sys, json, random, struct, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(os.environ.get('ROM') or sys.argv[1], 'rb').read()
rnd = random.Random(0xEF64)
m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4)
boot_table = m0.read_ram(0xD604, 512)
out = dict(boot_table=list(struct.unpack('>256H', boot_table)))
SLOT = 0xC470; REC = 0xBA68; G0, GN = 0xD48E, 0x172; CA0, CAN = 0xCA20, 0x60


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


UI_STUBS = (0x1B900, 0xE606, 0xCAEA, 0x75F8, 0x75FA, 0xCA7E, 0xAD5A, 0x9784, 0xDEE6, 0xDEC4, 0xDF08, 0xFA52, 0x98E4, 0xC3F0, 0xAD3E, 0x11C8E, 0x11C5A, 0x1343E, 0x1344A, 0x9240, 0x10FAA, 0x664E)


def machine(rom):
    """emulator with the VDP/IO ranges mapped, the VDP registers in a4/a5 and every graphics / sound / animation routine replaced by an empty one"""
    m = Machine(rom); m.map_io()
    for a in UI_STUBS: m.stub_rts(a)
    m.set_reg('a4', 0xC00004); m.set_reg('a5', 0xC00000)
    return m


def sane_record(rnd, party):
    b = bytearray(214)
    b[0x10] = rnd.randrange(3, 19); b[0x11] = rnd.randrange(3, 19); b[0x12] = rnd.randrange(3, 19)
    b[0x18] = rnd.randrange(0, 5); b[0x19] = rnd.randrange(1, 12)
    b[0x22] = rnd.randrange(6, 16); b[0x23] = rnd.choice([0, 0, 0, 1, 2, 3]); b[0x24] = rnd.randrange(3, 9); b[0x25] = rnd.randrange(0, 10)
    b[0x26] = rnd.choice([2, 2, 3, 4, 5]); b[0x27] = rnd.choice([0, 0, 0, 2, 3]); b[0x28] = rnd.randrange(1, 3); b[0x29] = rnd.randrange(0, 3)
    b[0x2A] = rnd.randrange(3, 9); b[0x2B] = rnd.randrange(2, 7); b[0x2C] = rnd.randrange(0, 3); b[0x2D] = rnd.randrange(0, 3); b[0x2E] = rnd.randrange(10, 60)
    b[0x2F] = rnd.choice([0, 0, 0, 2, 4, 6])
    for k in range(13):
        o = 0x54 + 10 * k
        if rnd.random() < 0.6: continue
        b[o] = rnd.choice([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 17, 18, 19, 20, 27, 30]); b[o + 4] = rnd.choice([0, 0, 1, 2]); b[o + 5] = 0; b[o + 9] = rnd.choice([0, 0, 1, 2, 3, 4])
    if rnd.random() < 0.8: b[0xAE] = rnd.choice([0, 1, 2, 4, 5, 6, 7, 9, 10, 11, 12, 13, 17, 18, 19])
    for i in range(10):
        if rnd.random() < 0.05: b[0x43 + i] = rnd.choice([0x14, 0x18, 3])
    return b


def sane_world(rnd, nmin=4, nmax=11):
    """a believable fight: 2-4 party members, monsters, consistent map markers, sensible stats"""
    n = rnd.randrange(nmin, nmax); npar = rnd.randrange(1, min(5, n))
    recs = [sane_record(rnd, r < 8) for r in range(11)]
    slots = []; taken = set()
    for k in range(n):
        sb = bytearray(26)
        party = k < npar
        sb[0] = rnd.choice([1, 1, 1, 1, 1, 1, 0x81, 0x83]) if k else 1
        sb[1] = (1 if party else 0) | rnd.choice([0, 0, 0x10, 0x10, 0x10, 0x02, 0x10, 0x20])
        sb[2] = k if party else 8 + rnd.randrange(3)
        while True:
            x, y = rnd.randrange(21), rnd.randrange(21)
            if (x, y) not in taken: break
        taken.add((x, y)); sb[0x12] = x; sb[0x13] = y
        sb[0xE] = rnd.randrange(3, 60); sb[0xF] = rnd.randrange(3, 9); sb[0x10] = rnd.randrange(8); sb[0x14] = rnd.choice([0, 2, 2, 2, 1]); sb[0x16] = rnd.randrange(0, 14)
        sb[0x17] = rnd.choice([0xFF, 0xFF, rnd.randrange(n)])
        sb[3] = rnd.randrange(6, 16); sb[4] = rnd.randrange(0, 10); sb[5] = sb[4]
        sb[6] = rnd.choice([2, 2, 3, 4]); sb[7] = rnd.choice([0, 0, 0, 2]); sb[8] = rnd.randrange(1, 3); sb[9] = rnd.randrange(0, 3); sb[10] = rnd.randrange(3, 9); sb[11] = rnd.randrange(2, 7)
        sb[12] = rnd.randrange(0, 3); sb[13] = 0
        slots.append(sb)
    tiles = bytearray(rnd.choice([2, 2, 2, 2, 3, 4, 5, 6, 7]) for _ in range(441))
    for sb in slots:                                           # markers of the living (same rule as 0x14254)
        st = sb[0]
        if st == 0 or (st & 0xC0) or (sb[1] & 4): continue
        t = recs[sb[2]][0x23]; x, y = sb[0x12], sb[0x13]
        for (cx, cy) in [(x, y)] + ([(x + 1, y)] if t == 3 else []) + ([(x, y + 1)] if t == 2 else []):
            if cx < 21 and cy < 21: tiles[cy * 21 + cx] |= 0x80
    ft = bytearray(129)
    for i in range(129): ft[i] = rnd.choice([0, 1, 1, 1, 2, 2, 3, 0x20, 0x40, 0x80, 0x01])
    g = bytearray(GN); ca = bytearray(CAN)
    g[0xD504 - G0] = 0; g[0xD500 - G0] = 0xFF; g[0xD4FD - G0] = rnd.randrange(256)
    g[0xD499 - G0] = rnd.randrange(0, 3); g[0xD49A - G0] = rnd.randrange(0, 3); g[0xD50C - G0] = rnd.randrange(2)
    g[0xD496 - G0] = 1
    actor = rnd.randrange(npar, n)                            # a monster
    slots[actor][0] = 1
    return dict(full=True, n=n, recs=recs, slots=slots, tiles=tiles, ft=ft, g=g, ca=ca, actor=actor, idx=rnd.randrange(256), npar=npar)


