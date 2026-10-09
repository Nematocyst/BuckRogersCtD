"""Vectors for the script item commands FINDITEM (0x3A8E), DESTROY (0x3A60) and HIDEITEMS (0x3D0C) with random party gear.   usage: python gen_scriptitems_vectors.py ROM OUT.txt.gz
One case per line: op arg [97AE] d499 d49a | 8 x (slot hex, record hex) | after: [B9F2] [97AE] | 8 x (slot hex, record hex)"""
import sys, os, random, gzip
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0x3A8E)
lines = []
for n in range(int(os.environ.get('N_scriptitems', 600))):
    m = Machine(rom)
    op = rnd.choice('FFDDDH'); mode = rnd.choice([0, 0, 0, 0xFF]); d499 = rnd.randrange(-2, 3) & 0xFF; d49a = rnd.randrange(-2, 3) & 0xFF
    arg = rnd.choice([0, 1, 5, 9, 10, 11, 12, 20, 25, 30, 38, 0x89, 0xA0]) if op != 'H' else rnd.choice([0, 1, 2])
    pre = []
    for i in range(8):
        rec = bytearray(214)
        for off in (0x10, 0x11): rec[off] = rnd.randrange(3, 22)
        rec[0x22] = rnd.randrange(30, 60); rec[0x24] = rnd.randrange(4, 12); rec[0x25] = 0x32
        rec[0x26:0x2E] = bytes([rnd.choice([2, 4]), 0, 1, 0, rnd.choice([2, 3]), 0, 0, 0])
        for k in range(13):
            o = 0x54 + 10 * k
            if rnd.random() < 0.55: continue
            rec[o] = rnd.choice([1, 5, 9, 10, 11, 12, 20, 25, 30, 38, rnd.randrange(1, 39)]); rec[o + 8] = rnd.choice([0, 1, 1, 2, 5])
        rec[0xAE] = rnd.choice([0, 0, 9, 10, 11]); rec[0xAE + 4] = rnd.randrange(0, 3)
        rec[0xC2] = rnd.choice([0, 0, 24, 20]); rec[0xC2 + 4] = rnd.randrange(0, 3)
        slot = bytearray(26); slot[0] = rnd.choice([1, 1, 1, 0, 0x81, 4]); slot[1] = rnd.choice([1, 0, 0x21, 0x20]); slot[2] = i
        pre.append((bytes(slot), bytes(rec)))
        m.write_ram(0xC470 + 26 * i, bytes(slot)); m.write_ram(0xBA68 + 214 * i, bytes(rec))
    m.write_ram(0x97AE, bytes([mode])); m.write_ram(0xD499, bytes([d499])); m.write_ram(0xD49A, bytes([d49a])); m.write_ram(0xB9F2, b'\0')
    script = {'F': [0, arg], 'D': [0, 0, 0, arg], 'H': [0, arg]}[op]
    m.write_ram(0xE000, bytes(script))
    m.call({'F': 0x3A8E, 'D': 0x3A60, 'H': 0x3D0C}[op], a2=0x00FFE000, a3=0x336E, a6=RAM_BASE + 0x8000, max_insns=600000)
    post = [(m.read_ram(0xC470 + 26 * i, 26), m.read_ram(0xBA68 + 214 * i, 214)) for i in range(8)]
    lines.append(' '.join(map(str, [op, arg, mode, d499, d49a])) + ' | ' + ' '.join(s.hex() + ' ' + r.hex() for s, r in pre) + ' | ' + ' '.join(map(str, [m.ram_byte(0xB9F2), m.ram_byte(0x97AE)])) + ' ' + ' '.join(s.hex() + ' ' + r.hex() for s, r in post))
with gzip.open(sys.argv[2], 'wt') as f: f.write('\n'.join(lines) + '\n')
print(len(lines), 'cases')
