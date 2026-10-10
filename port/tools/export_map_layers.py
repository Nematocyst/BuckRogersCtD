"""Export the 18 dungeon / area maps as the game holds them in RAM after the LZW decode of ROM 0x5766 (1,024 bytes at 0xB5A4: wall layer A, wall layer B, square attributes, door states).
usage: python export_map_layers.py ROM OUT.txt       -> one line per map: id hex(1024 bytes)   (ids in the stream at ROM 0x8FA8D)"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine
rom = open(sys.argv[1], 'rb').read()
ids = [3, 16, 17, 32, 35, 48, 49, 50, 52, 65, 66, 67, 81, 82, 96, 97, 98, 99]
out = []
for i in ids:
    m = Machine(rom); m.map_io(); m.call(0x5766, d2=i, max_insns=60_000_000)
    out.append('%d %s' % (i, m.read_ram(0xB5A4, 1024).hex()))
open(sys.argv[2], 'w').write('\n'.join(out) + '\n')
print(len(out), 'maps')
