"""Vectors for SKILLDAMAGE (opcode 0x49, handler 0x3D38 -> 0x5B44; six operands) with the screen helpers stubbed.   usage: python gen_scriptskilldmg_vectors.py ROM OUT.txt
One case per line: skill who shift count sides bonus cur idx | 8 x (status hp level skills(14) abilities(6) hex) | 8 x (status hp) refreshcalls ridx rsum"""
import sys, os, random, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0x5B44)
m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4)
boot_table = m0.read_ram(0xD604, 512)
lines = []
for n in range(int(os.environ.get('N_scriptskilldmg', 1200))):
    m = Machine(rom)
    for a in (0x136A0, 0x11CA4, 0x11C8E, 0x13328, 0x1368C, 0x11CA0, 0x142A8): m.stub_rts(a)
    refresh = m.stub_rts(0x1343E)
    skill = rnd.randrange(14); who = rnd.choice([0, 1, 1]); shift = rnd.choice([2, 2, 2, 0, 1, 3]); count = rnd.choice([0, 1, 1, 2, 3]); sides = rnd.choice([2, 4, 6, 8, 10])
    bonus = rnd.choice([0, 1, 2, 5, 10, 100]); cur = rnd.randrange(8); idx = rnd.randrange(256)
    mem = []
    for i in range(8):
        status = rnd.choice([1, 1, 1, 1, 0x81, 0x84, 0x41, 0, 4]); hp = rnd.choice([1, 3, 5, 10, 20, 60])
        r = bytearray(214); r[0x19] = rnd.choice([1, 2, 3, 5, 9])
        for k in range(14): r[0x31 + k] = rnd.choice([0, 0, 1, 2, 4, 8, 12, 30, rnd.randrange(256)])
        for k in range(6): r[0x10 + k] = rnd.randrange(3, 24)
        slot = bytearray(26); slot[0] = status; slot[2] = i; slot[0xE] = hp
        m.write_ram(0xC470 + 26 * i, bytes(slot)); m.write_ram(0xBA68 + 214 * i, bytes(r))
        mem.append('%d %d %02x%s%s' % (status, hp, r[0x19], r[0x31:0x3F].hex(), r[0x10:0x16].hex()))
    m.write_ram(0xD604, boot_table); m.write_ram(0xD804, bytes([idx])); m.write_ram(0x9DA7, bytes([cur]))
    ops = [skill, who, shift, count, sides, bonus]
    m.write_ram(0xE000, bytes(b for v in ops for b in (0, v)))
    m.call(0x3D38, a2=0x00FFE000, a3=0x336E, a6=RAM_BASE + 0x8000, max_insns=600000)
    out = []
    for i in range(8): out += [m.ram_byte(0xC470 + 26 * i), m.ram_byte(0xC470 + 26 * i + 0xE)]
    lines.append(' '.join(map(str, ops + [cur, idx])) + ' | ' + ' | '.join(mem) + ' | ' + ' '.join(map(str, out + [len(refresh), m.ram_byte(0xD804), sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF])))
open(sys.argv[2], 'w').write('\n'.join(lines) + '\n')
print(len(lines), 'cases; refreshed:', sum(l.split('|')[-1].split()[16] != '0' for l in lines))
