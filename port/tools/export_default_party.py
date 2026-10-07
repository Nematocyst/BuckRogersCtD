"""Decompress the game's default party (LZW blob at ROM 0x6BAAD, loaded by 0x1F32 into the party records 0xBA68 and the slots 0xC470) with the ROM's own decoder:
8 records of 214 bytes (6 characters: FLAVIUS, CELESTE, PIERRE, NICHOLE, ROARKE, JANELLE; 2 empty) followed by 8 slots of 26 bytes = 1,920 bytes.   usage: python export_default_party.py ROM OUT.bytes"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine
rom = open(sys.argv[1], 'rb').read()
m = Machine(rom); m.call(0x9E76, a0=0x6BAAD)
D = 0xFFFF9000
m.call(0x9ED8, d0=D, d1=1920, max_insns=30_000_000)
blob = m.read_ram(D, 1920)
open(sys.argv[2], 'wb').write(blob)
print('default party:', [blob[214 * i:214 * i + 8].split(b'\0')[0].decode() or '-' for i in range(8)], len(blob), 'bytes')
