"""Attack-resolution vectors from the real ROM loop body 0x10804..0x108A0 (one attack: d20 vs to-hit, weapon dice, bonus,
multiplier, special full-damage roll).  usage: python gen_damage_vectors.py ROM OUT.json"""
import sys, json, random, struct, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(10804)

# boot RNG table (from the real boot loop) shared by all cases; only the index differs
m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4)
boot_table = m0.read_ram(0xD604, 512)

cases = []
for n in range(1500):
    m = Machine(rom)
    m.stub_rts(0x664E)
    A = rnd.choice([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]); T = rnd.choice([i for i in range(11) if i != A])
    d4 = rnd.randrange(2)
    slot = [rnd.randrange(256) for _ in range(26)]
    slot[2] = A if A < 8 else 8 + A % 3
    slot[8 + d4] = rnd.choice([0, 1, 1, 2, 3, rnd.randrange(8), rnd.randrange(20)])      # dice count
    slot[0xA + d4] = rnd.choice([0, 3, 4, 6, 8, 10, 12, rnd.randrange(256)])              # dice sides
    slot[0xC + d4] = rnd.choice([0, 1, 2, 3, 5, 0xFE, 0xFB, rnd.randrange(256)])          # bonus (signed byte)
    tgt = [rnd.randrange(256) for _ in range(26)]; tgt[2] = 8 + T % 3 if T >= 8 else T
    if tgt[2] == slot[2]: tgt[2] = (slot[2] + 1) % 6
    rec_flags = rnd.choice([0, 0, 2, 4, 6, rnd.randrange(256)])
    gear0 = rnd.choice([0, 1, 2, 3, 9, 10, 11, 10, 11, 12, 20, 25, 30, 38])
    m97ae = rnd.choice([0, 0, 1])
    mult = rnd.choice([1, 1, 1, 2, 3, 4, rnd.randrange(256)])
    tohit = rnd.randrange(1, 20)
    idx = rnd.randrange(256); d6 = rnd.randrange(0, 40)
    m.write_ram(0xD48E, bytes([0xEE] * 64))          # output list first: it covers 0xD496/0xD497, which are set below
    m.write_ram(0xD604, boot_table); m.write_ram(0xD804, bytes([idx]))
    m.write_ram(0xC470 + A * 26, bytes(slot)); m.write_ram(0xC470 + T * 26, bytes(tgt))
    for ridx, fields in ((slot[2], {0xAE: gear0}), (tgt[2], {0x2F: rec_flags})):
        base = 0xBA68 + ridx * 0xD6
        for off, val in fields.items(): m.write_ram(base + off, bytes([val]))
    m.write_ram(0xCA20, bytes([A])); m.write_ram(0xD513, bytes([T])); m.write_ram(0x97AE, bytes([m97ae]))
    m.write_ram(0xD496, bytes([mult])); m.write_ram(0xD511, bytes([tohit])); m.write_ram(0xD497, b'\xee'); m.write_ram(0xD4FC, b'\xee')
    stop = m.run_between(0x10804, {0x108A0}, d4=d4, d6=d6, d7=1)
    cases.append(dict(A=A, T=T, d4=d4, slot=slot, tgtslot=tgt, flags2f=rec_flags, gear0=gear0, m97ae=m97ae, mult=mult, tohit=tohit, idx=idx, d6=d6,
                      d497=m.ram_byte(0xD497), d4fc=m.ram_byte(0xD4FC), stored=m.ram_byte(0xD48E + d6), d6out=m.reg('d6') & 0xFFFF,
                      ridx=m.ram_byte(0xD804), rsum=sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF))
json.dump(dict(boot_table=list(struct.unpack('>256H', boot_table)), cases=cases), open(sys.argv[2], 'w'), separators=(',', ':'))
import collections
print(len(cases), collections.Counter(c['d497'] == 0xEE for c in cases), 'full-damage:', sum(c['d4fc'] == 0xFF for c in cases))
