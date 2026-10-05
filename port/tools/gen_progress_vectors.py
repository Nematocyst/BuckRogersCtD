"""Progression / reward vectors from the real ROM code:
   0x0B1C  HP gained on level-up        0x0B92  attack value by career+level      0x7418  training eligibility + XP cap
   0x3C0A..0x3C2E  party XP award (ADDEP)       0x1631E  combat-victory tally (XP pool, credits, loot pool)
usage: python gen_progress_vectors.py ROM OUT.json"""
import sys, json, random, struct, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(7418)
S32 = lambda v: ((v + 2**31) % 2**32) - 2**31

m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4)
boot_table = m0.read_ram(0xD604, 512)
out = dict(boot_table=list(struct.unpack('>256H', boot_table)))

def with_rng(m, idx):
    m.write_ram(0xD604, boot_table); m.write_ram(0xD804, bytes([idx]))
def rng_state(m): return [m.ram_byte(0xD804), sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF]

# ---- A. HP gain 0xB1C: a2 = record (0xBA68), record +0x18 career, +0x12 CON, +0x2E HP
hp = []
for n in range(2500):
    m = Machine(rom)
    career = rnd.choice([0, 1, 2, 3, 4, 4, 3]); con = rnd.choice([rnd.randrange(0, 26), rnd.randrange(3, 19)]); hp0 = rnd.randrange(0, 110)
    idx = rnd.randrange(256); with_rng(m, idx)
    base = 0xBA68
    m.write_ram(base + 0x18, bytes([career])); m.write_ram(base + 0x12, bytes([con])); m.write_ram(base + 0x2E, bytes([hp0]))
    m.call(0xB1C, a2=0xFFFF0000 + base)
    hp.append([career, con, hp0, idx, m.ram_byte(base + 0x2E)] + rng_state(m))
out['hp'] = hp

# ---- B. attack value 0xB92: +0x18 career, +0x19 level -> +0x22
av = []
for career in range(0, 6):
    for level in range(0, 14):
        m = Machine(rom)
        base = 0xBA68
        m.write_ram(base + 0x18, bytes([career])); m.write_ram(base + 0x19, bytes([level])); m.write_ram(base + 0x22, b'\xee')
        m.call(0xB92, a2=0xFFFF0000 + base)
        av.append([career, level, m.ram_byte(base + 0x22)])
out['attack'] = av

# ---- C. training scan 0x7418 (a0 = output list): 8 slots (flag byte 0 = 1 present) x records
tr = []
TABLE = [struct.unpack('>I', rom[0xC16 + 4 * k:0xC16 + 4 * k + 4])[0] for k in range(0, 44)]
for n in range(800):
    m = Machine(rom)
    slots, recs = [], []
    for s in range(8):
        flag = rnd.choice([0, 1, 1, 1, 1, 2, 0x81])
        career = rnd.choice([1, 2, 3, 4]); level = rnd.randrange(1, 9)
        thr = TABLE[career * 8 + level - 9]; thr2 = TABLE[career * 8 + level - 8]
        xp = rnd.choice([0, rnd.randrange(0, 200000), max(0, thr - 1) if thr != 0xFFFFFFFF else 5, thr if thr != 0xFFFFFFFF else 7,
                         min(thr2, 0xFFFFFFFE) + rnd.randrange(0, 5), 2 * thr2 if thr2 < 2**31 else 1, rnd.randrange(0, 2**32)])
        slots.append(flag); recs.append((career, level, S32(xp & 0xFFFFFFFF)))
        m.write_ram(0xC470 + s * 26, bytes([flag]))
        base = 0xBA68 + s * 0xD6
        m.write_ram(base + 0x18, bytes([career])); m.write_ram(base + 0x19, bytes([level])); m.write_ram(base + 0x1E, struct.pack('>I', xp & 0xFFFFFFFF))
    m.write_ram(0xD000, bytes([0xAA] * 16))
    m.call(0x7418, a0=0xFFFF0000 + 0xD000)
    lst = []
    for b in m.read_ram(0xD000, 16):
        lst.append(b)
        if b == 0xFF: break
    newxp = [struct.unpack('>I', m.read_ram(0xBA68 + s * 0xD6 + 0x1E, 4))[0] for s in range(8)]
    tr.append(dict(slots=slots, recs=[list(r) for r in recs], ready=S32(m.reg('d0')), list=lst, xp=[S32(x) for x in newxp]))
out['train'] = tr

# ---- D. party XP award (ADDEP loop 0x3C0A..0x3C2E): every slot with flag != 0 and (flag & 0xC0) == 0 gets d3 added to record +0x1E
ad = []
for n in range(500):
    m = Machine(rom)
    flags = [rnd.choice([0, 1, 1, 0x40, 0x80, 0xC0, 0x41, 0x05]) for _ in range(8)]
    xps = [rnd.randrange(0, 2**32) if rnd.random() < 0.3 else rnd.randrange(0, 100000) for _ in range(8)]
    amount = rnd.choice([0, 1, 100, 5000, rnd.randrange(0, 2**32)])
    for s in range(8):
        m.write_ram(0xC470 + s * 26, bytes([flags[s]])); m.write_ram(0xBA68 + s * 0xD6 + 0x1E, struct.pack('>I', xps[s]))
    m.run_range(0x3C0A, 0x3C2E, d3=amount)
    ad.append(dict(flags=flags, xp=[S32(x) for x in xps], amount=S32(amount),
                   out=[S32(struct.unpack('>I', m.read_ram(0xBA68 + s * 0xD6 + 0x1E, 4))[0]) for s in range(8)]))
out['award'] = ad
# ---- E. combat-victory tally 0x1631E (calls 0x16268 loot add and 0x16592 for each dropped item)
ITEM_IDS = [0, 1, 2, 3, 4, 5, 7, 9, 10, 11, 12, 13, 15, 19, 20, 28, 29, 30, 32, 34, 8, 14, 16, 17]    # mixture of droppable (table -1), redirecting (>0) and zero entries
tally = []
for n in range(1200):
    m = Machine(rom)
    nslots = rnd.randrange(8, 20)
    pflags = [rnd.choice([0, 1, 1, 1, 0x80, 0x41, 0x81, 0x05]) for _ in range(8)]
    slots = []
    for sidx in range(nslots):
        if sidx < 8: fl = pflags[sidx]; rec = sidx if sidx < 6 else 0
        else: fl = rnd.choice([0, 0x80, 0x81, 0x83, 0x01, 0x41, 0xC0, 0x80, 0x80]); rec = 8 + rnd.randrange(3)
        slots.append([fl, rec])
        sb = bytearray(26); sb[0] = fl; sb[2] = rec
        m.write_ram(0xC470 + sidx * 26, bytes(sb))
    m.write_ram(0xBA64, struct.pack('>H', nslots))
    recs = {}
    for ridx in (8, 9, 10):
        xp = rnd.choice([0, 25, 75, 125, 1150, rnd.randrange(0, 65536)]); f52 = rnd.choice([0, 0, 0, 1, 0xFF, rnd.randrange(256)])
        cr = rnd.choice([0, 33, 40, 57, rnd.randrange(0, 5000), rnd.randrange(0, 2**32)])
        gear = []
        for g in range(13):
            idb = rnd.choice(ITEM_IDS) if rnd.random() < 0.55 else 0
            if idb == 0: gear.append([0] * 10)
            else: gear.append([idb, rnd.randrange(256), rnd.randrange(256), rnd.randrange(256), rnd.choice([0, 1, 2, 5, 9, 0x10]), rnd.choice([0x80, 0x40, 0xC0, 0, 0x81, 0x02]), rnd.randrange(256), rnd.randrange(256), rnd.choice([0, 1, 2, 8, 20, 90, 250, 0x81, 0xFA]), rnd.randrange(256)])
        recs[ridx] = (xp, f52, cr, gear)
        base = 0xBA68 + ridx * 0xD6
        m.write_ram(base + 0x40, struct.pack('>H', xp)); m.write_ram(base + 0x52, bytes([f52])); m.write_ram(base + 0x1A, struct.pack('>I', cr))
        for g in range(13): m.write_ram(base + 0x54 + g * 10, bytes(gear[g]))
    pre = rnd.randrange(0, 15)
    pool0 = []
    for k in range(pre):
        e = [rnd.choice(ITEM_IDS[:20]) or 3, rnd.randrange(256), rnd.randrange(256), rnd.randrange(256), rnd.choice([0, 1, 2, 5, 9, 0x10]), rnd.randrange(256), rnd.randrange(256), rnd.randrange(256), rnd.choice([0x81, 0x82, 5, 20, 0xFF, 99]), rnd.randrange(256)]
        pool0.append(e); m.write_ram(0x6AF6 + k * 10, bytes(e))
    m.write_ram(0xB9F3, bytes([pre]))
    mask = rnd.choice([0, 0, rnd.randrange(256)]); ba5b = rnd.choice([0, 0, 0, 1]); m97 = rnd.choice([0, 0, 0, 1]); ba60 = rnd.choice([0, 0, 1])
    credits0 = rnd.randrange(0, 2**32)
    scripted = rnd.choice([None, None, None, None, 300, 3000])
    m.write_ram(0xD8DA, bytes([mask])); m.write_ram(0xBA5B, bytes([ba5b])); m.write_ram(0x97AE, bytes([m97])); m.write_ram(0xBA60, bytes([ba60]))
    m.write_ram(0xBA34, struct.pack('>I', credits0)); m.write_ram(0xD50E, b'\x00'); m.write_ram(0xD514, struct.pack('>I', 0x12345678))
    if scripted is None: m.write_ram(0x9930, b'\x00')
    else: m.write_ram(0x9930, b'\x80'); m.write_ram(0x9927, b'\x01'); m.write_ram(0x9924, bytes([0 if scripted == 300 else 1]))
    m.call(0x1631E)
    cnt = m.ram_byte(0xB9F3)
    tally.append(dict(slots=slots, mask=mask, ba5b=ba5b, m97=m97, ba60=ba60, credits0=S32(credits0), scripted=-1 if scripted is None else scripted,
                      recs={str(k): dict(xp=v[0], f52=v[1], cr=S32(v[2]), gear=v[3]) for k, v in recs.items()}, pool0=pool0, pre=pre,
                      xpOut=S32(struct.unpack('>I', m.read_ram(0xD514, 4))[0]), flag=m.ram_byte(0xD50E),
                      credits=S32(struct.unpack('>I', m.read_ram(0xBA34, 4))[0]), count=cnt,
                      pool=[list(m.read_ram(0x6AF6 + k * 10, 10)) for k in range(min(cnt, 15))],
                      flags5=[[m.ram_byte(0xBA68 + r * 0xD6 + 0x54 + g * 10 + 5) for g in range(13)] for r in (8, 9, 10)]))
out['tally'] = tally
# ---- F. round start for one slot 0x100D6 (skill checks, flags, attacks left, initiative) and G. skill check 0x4F20
def fill_record(m, base, rnd):
    for off in range(0, 0xD6):
        if rnd.random() < 0.2: m.write_ram(base + off, bytes([rnd.randrange(256)]))
    for k in range(0x10, 0x15): m.write_ram(base + k, bytes([rnd.choice([rnd.randrange(3, 19), rnd.randrange(256)])]))
    m.write_ram(base + 0x19, bytes([rnd.randrange(1, 10)]))
    for k in range(0x31, 0x31 + 14): m.write_ram(base + k, bytes([rnd.choice([0, rnd.randrange(1, 12), rnd.randrange(1, 40), rnd.randrange(256)])]))

ini = []
for n in range(3000):
    m = Machine(rom)
    m.stub_rts(0x664E)
    flags0 = []
    for k in range(21):
        f0 = rnd.choice([0x00, 0x01, 0x01, 0x01, 0x11, 0x05, 0x02, 0x81, 0x41]); flags0.append(f0)
        sb = bytearray(rnd.randrange(256) for _ in range(26)); sb[0] = f0; sb[2] = k if k < 8 else 8 + rnd.randrange(3)
        m.write_ram(0xC470 + k * 26, bytes(sb))
    sidx = rnd.choice([i for i in range(21) if flags0[i] in (0x01, 0x11, 0x05, 0x02)] or [0])
    sb = bytearray(m.read_ram(0xC470 + sidx * 26, 26))
    sb[1] = rnd.choice([0x00, 0x01, 0x11, 0x10, 0x03, 0x12, 0x20, 0x21, 0x23, 0xFF])
    m.write_ram(0xC470 + sidx * 26, bytes(sb))
    for r in range(11): fill_record(m, 0xBA68 + r * 0xD6, rnd)
    sur = rnd.choice([0, 0, 1, 2]); par = rnd.randrange(256); d97dc = rnd.randrange(256); mask0 = rnd.randrange(256)
    m.write_ram(0x9DC1, bytes([sur])); m.write_ram(0xD50C, bytes([par])); m.write_ram(0x97DC, bytes([d97dc])); m.write_ram(0xD4FD, bytes([mask0]))
    recidx = sb[2]
    recbase = 0xBA68 + recidx * 0xD6
    rec = list(m.read_ram(recbase, 0xD6))
    idx = rnd.randrange(256); with_rng(m, idx)
    m.call(0x100D6, a3=0xFFFF0000 + 0xC470 + sidx * 26)
    after = list(m.read_ram(0xC470 + sidx * 26, 26))
    ini.append(dict(flags0=flags0, slot=list(sb), rec=rec, sur=sur, par=par, d97dc=d97dc, mask0=mask0, idx=idx, after=after, mask=m.ram_byte(0xD4FD),
                    rng=rng_state(m)))
out['round'] = ini

sk = []
for n in range(3000):
    m = Machine(rom)
    ridx = rnd.randrange(8)
    f0 = rnd.choice([0x01, 0x01, 0x01, 0x11, 0x00, 0x81, 0x41, 0x02])
    sb = bytearray(26); sb[0] = f0
    m.write_ram(0xC470 + ridx * 26, bytes(sb))
    fill_record(m, 0xBA68 + ridx * 0xD6, rnd)
    skill = rnd.choice([rnd.randrange(14), rnd.randrange(14), 4, 7]); shift = rnd.choice([0, 1, 2, 2, 2, 3, 4])
    rec = list(m.read_ram(0xBA68 + ridx * 0xD6, 0xD6))
    idx = rnd.randrange(256); with_rng(m, idx)
    m.call(0x4F20, d0=skill, d1=shift, d2=0, a0=0xFFFF0000 + 0xBA68 + ridx * 0xD6)
    sk.append(dict(f0=f0, rec=rec, skill=skill, shift=shift, idx=idx, out=m.reg('d0') & 0xFFFF, rng=rng_state(m)))
out['skill'] = sk

pick = []
for n in range(1500):
    m = Machine(rom)
    nslots = rnd.randrange(1, 21)
    rows = []
    for sidx in range(nslots):
        sb = bytearray(26)
        sb[0] = rnd.choice([0x00, 0x01, 0x01, 0x01, 0x81, 0x41, 0xC1])
        sb[0x14] = rnd.choice([0, 2, 3, 5, 5, 8, 12, 20, rnd.randrange(256)]); sb[0x15] = rnd.choice([1, 50, 100, rnd.randrange(256)])
        m.write_ram(0xC470 + sidx * 26, bytes(sb)); rows.append([sb[0], sb[0x14], sb[0x15]])
    m.write_ram(0xBA64, struct.pack('>H', nslots))
    m.run_between(0xE3E6, {0xE418}, a7=0)
    d5 = m.reg('d5') & 0xFFFF; d6 = m.reg('d6') & 0xFFFF
    pick.append(dict(rows=rows, d5=d5, d6=d6))
out['pick'] = pick
json.dump(out, open(sys.argv[2], 'w'), separators=(',', ':'))
print({k: len(v) for k, v in out.items() if isinstance(v, list)})
