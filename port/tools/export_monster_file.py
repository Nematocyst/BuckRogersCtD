"""Decompress the monster file (LZW stream at ROM 0x9E77C) with the ROM's own decompressor in the emulator and write it as [count word][ids, even padded][count x 214-byte records].
usage: python export_monster_file.py ROM OUT.bytes"""
import sys, os, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine
rom = open(sys.argv[1], 'rb').read()
m = Machine(rom)
m.call(0x9E76, a0=0x9E77C)
D = 0xFFFFE000
def get(n):
    m.call(0x9ED8, d0=D, d1=n, max_insns=5_000_000)
    return m.read_ram(D, n)
count = struct.unpack('>H', get(2))[0]
ids = get((count + 1) & ~1)
recs = [get(214) for _ in range(count)]
out = struct.pack('>H', count) + ids + b''.join(recs)
open(sys.argv[2], 'wb').write(out)
print(count, 'monsters,', len(out), 'bytes; first ids', list(ids[:8]))
