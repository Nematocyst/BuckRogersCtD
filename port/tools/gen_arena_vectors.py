"""Dungeon-arena vectors (ROM 0xB100): random map layers / tile tables / party position -> the 21x21 tile map at 0xCACA. usage: python gen_arena_vectors.py ROM OUT.txt
One case per line: mapx mapy facing d4fe d8cc layerA layerB walls classA classB tiles_before tiles_after (hex strings)"""
import sys, os, random
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine
import struct
rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0xB100)
N = int(os.environ.get('N_arena', 400))
lines = []
for k in range(N):
    m = Machine(rom)
    la = bytes(rnd.randrange(256) for _ in range(256)); lb = bytes(rnd.randrange(256) for _ in range(256))
    dens = rnd.choice([0.1, 0.3, 0.5, 0.8])
    wl = bytes((0x80 if rnd.random() < dens else 0) | rnd.randrange(128) for _ in range(256))
    ca = bytes((0x80 | rnd.randrange(128)) if rnd.random() < rnd.choice([0.2, 0.5, 0.0]) else rnd.randrange(128) for _ in range(16))
    cb = bytes(rnd.randrange(8) if rnd.random() < 0.8 else rnd.randrange(256) for _ in range(16))
    mx = rnd.choice([rnd.randrange(16)] * 3 + [rnd.randrange(-3, 20), rnd.randrange(256)]); my = rnd.choice([rnd.randrange(16)] * 3 + [rnd.randrange(-3, 20), rnd.randrange(256)])
    mx &= 255; my &= 255
    face = rnd.randrange(4); d4fe = rnd.choice([0, 1, 1]); d8cc = rnd.choice([0, 0, 0, 3])
    before = bytes(rnd.randrange(256) for _ in range(441))
    m.write_ram(0xB5A4, la); m.write_ram(0xB6A4, lb); m.write_ram(0xB7A4, wl)
    m.write_ram(0xE000, ca); m.write_ram(0xE020, cb); m.write_ram(0xB41A, struct.pack('>II', 0x00FFE000, 0x00FFE020))
    m.write_ram(0x9AF7, bytes([mx])); m.write_ram(0x9AF6, bytes([my])); m.write_ram(0x9AFA, bytes([face]))
    m.write_ram(0xD4FE, bytes([d4fe])); m.write_ram(0xD8CC, bytes([d8cc])); m.write_ram(0xCACA, before)
    m.call(0xB100, max_insns=2000000)
    after = m.read_ram(0xCACA, 441)
    assert m.ram_word(0xD810) == 0 and m.read_ram(0xD810, 4) == bytes([0, 0, 0x32, 0x97])
    lines.append(' '.join([str(mx), str(my), str(face), str(d4fe), str(d8cc), la.hex(), lb.hex(), wl.hex(), ca.hex(), cb.hex(), before.hex(), after.hex()]))
open(sys.argv[2], 'w').write('\n'.join(lines) + '\n')
print(len(lines), 'arena cases')
