"""Vectors for the character script opcodes SKILL (0x22, handler 0x38A2 -> 0x4E52) and PRINTSKILL (0x23, 0x38A8): the best-of-party / single-character skill check.
The screen helpers are stubbed; recorded are the two destination bytes, [0xB4C7] and the RNG state.   usage: python gen_scriptchar_vectors.py ROM OUT.txt
One case per line: op(22|23) skill who shift cur idx slotflags0..7 | records(8 x (level, 14 skill bytes, 6 abilities) hex) | who' result [B4C7] ridx rsum"""
import sys, os, random, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0x38A2)
m0 = Machine(rom); m0.run_range(0x12C8, 0x12E4)
boot_table = m0.read_ram(0xD604, 512)
STUBS = (0x11C8E, 0x11CA4, 0x1343E, 0x75F8)
lines = []
for n in range(int(os.environ.get('N_scriptchar', 1200))):
    m = Machine(rom)
    for a in STUBS: m.stub_rts(a)
    op = rnd.choice([0x22, 0x22, 0x23])
    skill = rnd.randrange(14); who = rnd.choice([0, 0, 1, 2, 0x100, 0x180]); shift = rnd.choice([2, 2, 2, 0, 1, 3])
    cur = rnd.randrange(8); idx = rnd.randrange(256)
    flags = [rnd.choice([1, 1, 1, 0x41, 0x81, 0, 0x40, 0x80]) for _ in range(8)]
    recs = []
    for i in range(8):
        r = bytearray(214)
        r[0x19] = rnd.choice([1, 2, 3, 5, 8, 9, 12, 40])
        for k in range(14): r[0x31 + k] = rnd.choice([0, 0, 1, 2, 3, 5, 8, 12, 20, 40, 200, rnd.randrange(256)])
        for k in range(6): r[0x10 + k] = rnd.randrange(3, 24)
        recs.append(r)
        m.write_ram(0xBA68 + 214 * i, bytes(r)); m.write_ram(0xC470 + 26 * i, bytes([flags[i]]) + bytes(25))
    m.write_ram(0xD604, boot_table); m.write_ram(0xD804, bytes([idx])); m.write_ram(0x9DA7, bytes([cur]))
    m.write_ram(0x9E70, bytes([who & 0xFF])); m.write_ram(0x9E71, bytes([shift]))
    m.write_ram(0xB4C7, b'\0')
    # SKILL skill, [9E70], [9E71]: operand 0 immediate byte, operands 1 and 2 are memory bytes that are also the destinations
    m.write_ram(0xE000, bytes([0, skill, 1, 0x70, 0x9E, 1, 0x71, 0x9E]))
    m.call(0x38A2 if op == 0x22 else 0x38A8, a2=0x00FFE000, a3=0x336E, a6=RAM_BASE + 0x8000, max_insns=400000)
    rec_s = ''.join('%02x' % r[0x19] + r[0x31:0x3F].hex() + r[0x10:0x16].hex() for r in recs)
    lines.append(' '.join(map(str, [op, skill, who & 0xFF, shift, cur, idx] + flags)) + ' | ' + rec_s + ' | ' +
                 ' '.join(map(str, [m.ram_byte(0x9E70), m.ram_byte(0x9E71), m.ram_byte(0xB4C7), m.ram_byte(0xD804), sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF])))
open(sys.argv[2], 'w').write('\n'.join(lines) + '\n')
import collections
print(len(lines), 'cases; index 0x90 (nobody eligible):', sum(l.split('|')[2].split()[0] == '144' for l in lines))
