"""Vectors for the starship crew hazard (ROM 0x1956E): a crew member takes 1d`sides` damage unless a d20 save (target 14 - level/3, desert runner -1, race >= 3 +3) succeeds.
The routine is run with its screen / message helpers stubbed; what is recorded is the return value (nonzero = the whole party is down), the victim's hit points and status,
the 'overkill' byte at -0x5A(a6, d3), and the RNG state.   usage: python gen_hazard_vectors.py ROM OUT.txt
One case per line: slot level race sides hp idx status0..7 | ret hp status overkill idx rsum"""
import sys, os, random, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0x1956E)
m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4)
boot_table = m0.read_ram(0xD604, 512)
STUBS = (0x1B900, 0x191FC, 0x11CA4, 0x11C8E, 0x13376, 0x1368C, 0x11CA0, 0x19976)
A6 = RAM_BASE + 0x8000
out = []
for n in range(int(os.environ.get('N_hazard', 1500))):
    m = Machine(rom)
    for a in STUBS: m.stub_rts(a)
    slot = rnd.randrange(8)
    level = rnd.choice([1, 2, 3, 5, 8, 9, 12, 15, 20, 30, 44, 45, 46, 60, 100, 200, 255, rnd.randrange(256)])
    race = rnd.choice([0, 1, 2, 3, 4, 5, 6, 9, 255])
    sides = rnd.choice([4, 10, 4, 10, 6, 20, rnd.randrange(1, 100)])
    hp = rnd.choice([0, 1, 2, 3, 4, 5, 8, 10, 11, 20, 100, 127, 128, 200, rnd.randrange(256)])
    idx = rnd.randrange(256)
    status = [rnd.choice([0, 1, 1, 1, 0x81, 0x84, 0x40, 0x41, 0x80]) for _ in range(8)]
    status[slot] = rnd.choice([1, 1, 1, 0x81, 1])
    for i in range(8):
        m.write_ram(0xC470 + 26 * i, bytes([status[i]]) + bytes(25))
    m.write_ram(0xC470 + 26 * slot + 0xE, bytes([hp]))
    rec = bytearray(214); rec[0x19] = level; rec[0x17] = race
    m.write_ram(0xBA68 + 214 * slot, bytes(rec))
    m.write_ram(0xD604, boot_table); m.write_ram(0xD804, bytes([idx]))
    m.write_ram(A6 & 0xFFFF, bytes(0x100))
    # the caller: d0 = crew index, d1 = sides, a0 = its slot, a1 = its record (0x6F02 / 0x6F14)
    m.call(0x1956E, d0=slot, d1=sides, a0=RAM_BASE + 0xC470 + 26 * slot, a1=RAM_BASE + 0xBA68 + 214 * slot, a6=A6, d7=0, max_insns=200000)
    ret = 1 if (m.reg('d0') & 0xFF) else 0
    st = m.ram_byte(0xC470 + 26 * slot); hp2 = m.ram_byte(0xC470 + 26 * slot + 0xE)
    over = m.ram_byte((A6 - 0x5A + slot) & 0xFFFF)
    rsum = sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF
    out.append(' '.join(map(str, [slot, level, race, sides, hp, idx] + status + ['|', ret, hp2, st, over, m.ram_byte(0xD804), rsum])))
open(sys.argv[2], 'w').write('\n'.join(out) + '\n')
import collections
print(len(out), 'cases; wiped:', sum(l.split('|')[1].split()[0] == '1' for l in out), '; damaged:', sum(l.split('|')[1].split()[1] != l.split()[4] for l in out))
