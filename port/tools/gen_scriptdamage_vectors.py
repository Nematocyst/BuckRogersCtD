"""Vectors for the script command DAMAGE (opcode 0x2E, handler 0x3A0C -> 0x500A) with its screen helpers stubbed (no status-effect data: the stage hooks run but find nothing).
usage: python gen_scriptdamage_vectors.py ROM OUT.txt
One case per line: flags count sides bonus tgt cur idx | 8 x (status hp armor wis) | 8 x (status hp) refresh gameover ba53 ridx rsum"""
import sys, os, random, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0x500A)
m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4)
boot_table = m0.read_ram(0xD604, 512)
STUBS = (0x136A0, 0x11CA4, 0x11C8E, 0x13328, 0x1368C, 0x11CA0, 0x142A8)
lines = []
for n in range(int(os.environ.get('N_scriptdamage', 1500))):
    m = Machine(rom)
    for a in STUBS: m.stub_rts(a)
    refresh = m.stub_rts(0x14246); over = m.stub_rts(0x7588)
    mode = rnd.choice(['blast', 'blast', 'shots'])
    if mode == 'blast':
        flags = 0x80 | rnd.choice([0, 0x10, 0x20, 0x40, 0x50, 0x60, 0x40, 0x00]) | rnd.choice([0, 0, 1, 3, 8, 0x1F])
        tgt = rnd.choice([0, 0, 0x80, 0x81, 0x83, 0x05, 0x7F, 0xFF])
    else:
        flags = rnd.randrange(0, 9); tgt = rnd.choice([0, 20, 45, 100, 0xFE, 5])
    count = rnd.choice([0, 1, 1, 2, 3, 5, 10]); sides = rnd.choice([2, 4, 6, 8, 10, 12, 20, 100, 127])
    bonus = rnd.choice([0, 0, 1, 5, 10, 50, 100, 200]); cur = rnd.randrange(8); idx = rnd.randrange(256)
    mem = []
    for i in range(8):
        status = rnd.choice([1, 1, 1, 1, 0x81, 0x84, 0x41, 0, 0x04, 0x07])
        hp = rnd.choice([1, 3, 5, 8, 10, 15, 20, 30, 60, rnd.randrange(1, 90)])
        armor = rnd.choice([0, 10, 30, 45, 50, 60, 100, 0xF0, rnd.randrange(256)]); wis = rnd.randrange(3, 22)
        mem.append((status, hp, armor, wis))
        slot = bytearray(26); slot[0] = status; slot[2] = i; slot[4] = armor; slot[0xE] = hp
        m.write_ram(0xC470 + 26 * i, bytes(slot))
        r = bytearray(214); r[0x15] = wis; m.write_ram(0xBA68 + 214 * i, bytes(r))
    m.write_ram(0xD604, boot_table); m.write_ram(0xD804, bytes([idx])); m.write_ram(0x9DA7, bytes([cur])); m.write_ram(0xBA53, b'\0')
    m.write_ram(0xE000, bytes([0, flags, 0, count, 0, sides, 0, bonus, 0, tgt]))
    m.call(0x3A0C, a2=0x00FFE000, a3=0x336E, a6=RAM_BASE + 0x8000, max_insns=600000)
    out = []
    for i in range(8): out += [m.ram_byte(0xC470 + 26 * i), m.ram_byte(0xC470 + 26 * i + 0xE)]
    lines.append(' '.join(map(str, [flags, count, sides, bonus, tgt, cur, idx] + [x for t in mem for x in t])) + ' | ' + ' '.join(map(str, out + [len(refresh), len(over), m.ram_byte(0xBA53), m.ram_byte(0xD804), sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF])))
open(sys.argv[2], 'w').write('\n'.join(lines) + '\n')
print(len(lines), 'cases; refreshes:', sum(l.split('|')[1].split()[16] != '0' for l in lines), 'game overs:', sum(l.split('|')[1].split()[17] != '0' for l in lines))
