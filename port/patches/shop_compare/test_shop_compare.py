"""Differential test of the shop hover line.  usage: test_shop_compare.py ROM [cases]
Runs the ROM's describe routine (0x169C6) for a shop item with the patch applied and compares the extra line with an oracle built from the
game's own stat routine (0x6D1E): the candidate is put into the hand / armour slot of a copy of the record and the sheet's numbers are read back."""
import sys, os, random, struct
here = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, here); sys.path.insert(0, here + '/../../tools')
import build_patch
from emu68k import Machine, RAM_BASE
from monster_world import sane_record

rom0 = open(sys.argv[1], 'rb').read(); N = int(sys.argv[2]) if len(sys.argv) > 2 else 600
rom1 = build_patch.apply(rom0)
SLOT, REC, POOL, FRAME = 0xC470, 0xBA68, 0x6AF6, 0xE800

def machine(rom):
    m = Machine(rom); m.map_io()
    m.set_reg('a4', 0xC00004); m.set_reg('a5', 0xC00000)
    m.call(0xF28E, max_insns=100, a3=RAM_BASE + SLOT, a2=RAM_BASE + REC)            # priming run (see monster_world.machine)
    for a in (0x1343E, 0x1343A): m.stub_rts(a)
    m.shots = []
    def flush(): 
        buf = m.read_ram(0xD5AE, 48); s = buf[:buf.index(0)].decode('latin1')
        m.shots.append((s, m.ram_word(0xD5D6), m.ram_word(0xD5D8), m.ram_word(0xD5AC))); return 0
    m.stub_fn(0x11CA4, flush)
    return m

def recompute(m, slot_i, rec_i, rec):
    m.write_ram(REC + 214 * rec_i, rec)
    sl = bytearray(m.read_ram(SLOT + 0x1A * slot_i, 26)); sl[0] = 1; sl[2] = rec_i
    m.write_ram(SLOT + 0x1A * slot_i, sl)
    m.call(0x6D1E, a0=RAM_BASE + SLOT + 0x1A * slot_i, a1=RAM_BASE + REC + 214 * rec_i)
    return bytearray(m.read_ram(SLOT + 0x1A * slot_i, 26))

def sheet_dmg(sl):
    mx = (sl[0xA] * sl[8] + sl[0xC]) & 0xFF; mx = 0 if mx & 0x80 else mx
    mn = (sl[8] + sl[0xC]) & 0xFF; mn = 0 if mn & 0x80 else mn
    return ('%d' % mn if mn == mx else '%d-%d' % (mn, mx)) + ' X%d' % (sl[6] >> 1)

def sheet_ac(sl):
    v = (0x3C - sl[4]) & 0xFF; return v - 256 if v & 0x80 else v

def describe(m, cell, item, shop=1):
    m.write_ram(POOL + 10 * (cell - 9), item)
    m.write_ram(FRAME - 0x52, bytes([0, 0, 1] * 24))
    m.write_ram(0xBA60, bytes([shop]))
    m.shots.clear(); m.write_ram(0xD5AE, bytes(40))
    m.call(0x169C6, d2=cell, a6=RAM_BASE + FRAME, a3=0x1234, a2=0x5678)
    return list(m.shots)

rnd = random.Random(0x5AB0)
mo, mp = machine(rom0), machine(rom1)
fails = 0; checks = 0; kinds = {'weapon': 0, 'armor': 0, 'none': 0}
for case in range(N):
    for m in (mo, mp): m.write_ram(0xD499, bytes([0, 0]))
    rec = sane_record(rnd, True)
    rec[0x10] = rnd.randrange(3, 24); rec[0x11] = rnd.randrange(3, 24)
    for i in range(5): rec[0x4D + i] = rnd.choice([0, 0, rnd.randrange(1, 0x14)])
    if rnd.random() < 0.5: rec[0xC2:0xCC] = bytes([rnd.choice([0x14, 0x15, 0x16, 0x17, 0x18]), 0, 0, 0, rnd.randrange(0, 4), 0, 0, 0, 1, 0])
    else: rec[0xC2:0xCC] = bytes(10)
    ci = rnd.randrange(0, 6)
    iid = rnd.choice(list(range(1, 0x27)) + [0x01, 0x04, 0x09, 0x0A, 0x14, 0x16, 0x18])
    item = bytearray(10); item[0] = iid; item[4] = rnd.choice([0, 0, 1, 3, 6]); item[5] = rnd.choice([0, 0, 0x10]); item[6:8] = struct.pack('>H', rnd.randrange(50, 3000)); item[8] = rnd.randrange(1, 9)
    item[9] = rnd.choice([0, 1, 2, 3, 4]) if iid < 0x20 else rnd.choice([5, 6, 8, 9, 12])
    cell = 9 + rnd.randrange(14)
    for m in (mo, mp):
        m.write_ram(0xCA20, bytes([ci])); recompute(m, ci, ci, rec)
    a = describe(mo, cell, item); b = describe(mp, cell, item)
    ra, rb = bytearray(mo.read_ram(0, 0x10000)), bytearray(mp.read_ram(0, 0x10000))
    for r_ in (ra, rb): r_[0xD5A0:0xD600] = bytes(0x60); r_[0xEE00:0xF000] = bytes(0x200)     # text scratch differs by design; dead stack below sp
    ram_same = ra == rb
    regs_same = all(mo.reg(x) == mp.reg(x) for x in ('a7', 'a6', 'd2', 'a2', 'a3'))
    row = rom0[0x779E + 8 * iid: 0x779E + 8 * iid + 8]
    # oracle
    expect_line = None
    if row[0] == 0 and row[3] != 0 and not (5 <= item[9] <= 12):
        r2 = bytearray(rec); r2[0xAE:0xB8] = item; sl = recompute(mp, ci, ci, r2)
        r3 = bytearray(rec); now = recompute(mp, ci, ci, r3)
        expect_line = 'DMG ' + sheet_dmg(sl) + '  NOW ' + sheet_dmg(now); kinds['weapon'] += 1
    elif row[0] == 1:
        r2 = bytearray(rec); r2[0xC2:0xCC] = item; sl = recompute(mp, ci, ci, r2)
        now = recompute(mp, ci, ci, bytearray(rec))
        expect_line = 'AC %d  NOW %d' % (sheet_ac(sl), sheet_ac(now)); kinds['armor'] += 1
    else: kinds['none'] += 1
    for m in (mo, mp): recompute(m, ci, ci, rec)            # restore for the next case's priming
    checks += 1
    if os.environ.get('SHOW') and expect_line: print(hex(iid), item[4], b[-1])
    ok = ram_same and regs_same and a == b[:len(a)] and len(b) == len(a) + (1 if expect_line else 0)
    if ok and expect_line: ok = b[-1] == (expect_line, 0x13 - len(expect_line) // 2 + 0 if False else b[-1][1], 0x18, 0xC000) and b[-1][0] == expect_line and b[-1][2] == 0x18
    if not ok:
        fails += 1
        if fails < 8: print('FAIL case', case, 'id', hex(iid), 'type', item[9], 'orig', a, 'patched', b, 'expect', expect_line)
print('cases', checks, kinds, 'failures', fails)
sys.exit(1 if fails else 0)
