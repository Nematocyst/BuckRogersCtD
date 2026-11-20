#!/usr/bin/env python3
"""Remove racial armors that duplicate another armor's armor value at a higher price (keep the cheapest per value;
never remove armors the original game's shops sold: spacesuit, smartsuit, heavy body armor, battle armor w/fields).
  python dedupe_armor.py ORIGINAL.md OUT.md OUT.ips --ips BASE.ips [...]
Armor value = stat-table byte 1 - 0x32 + item byte 4 (racial tier), as the stat routine 0x6D1E adds it.
Only the shop lists (0xF2000 pointers) change; item records stay (loot drops are unaffected)."""
import sys, os, struct
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_combined import read_ips, CHECKSUM_BYTES
from build_monster_patch import game_checksum, header_checksum, make_ips
args = sys.argv[1:]; src, out_rom, out_ips = args[:3]
orig = open(src, 'rb').read(); rom = bytearray(orig)
for p in [args[i + 1] for i, a in enumerate(args) if a == '--ips']:
    for off, data in read_ips(p):
        for k, b in enumerate(data):
            if off + k not in CHECKSUM_BYTES: rom[off + k] = b
TB = struct.unpack_from('>I', rom, 0xBF0)[0]; T = 0x11DC4
def word(i):
    if i == 0: return ''
    o = struct.unpack_from('>H', rom, T + 2 * i)[0]; return rom[T + o:rom.index(0, T + o)].decode('latin1')
def name(i): x = rom[TB + 10 * i:TB + 10 * i + 10]; return ' '.join(w for w in (word(x[3]), word(x[2]), word(x[1])) if w).title()
KEEP_ALWAYS = {0x14, 0x15, 0x16, 0x18}                      # sold by the original game's shops
sold = set()
for s in range(16):
    L = struct.unpack_from('>I', rom, 0xF2000 + 4 * s)[0]; sold |= set(rom[L + 1:L + 1 + rom[L]])
armor = {}
for i in sold:
    x = rom[TB + 10 * i:TB + 10 * i + 10]; st = rom[0x779E + 8 * x[0]:0x779E + 8 * x[0] + 8]
    if st[0] == 1: armor[i] = (st[1] - 0x32 + x[4], struct.unpack('>H', x[6:8])[0])
remove = set()
for v in sorted({a[0] for a in armor.values()}):
    group = sorted([i for i in armor if armor[i][0] == v], key=lambda i: (i not in KEEP_ALWAYS, armor[i][1]))
    keep = group[0]
    for i in group[1:]:
        if i not in KEEP_ALWAYS: remove.add(i)
    print(f'armor +{v:2d}: keep {name(keep)} ({armor[keep][1]})' + ''.join(f'; drop {name(i)} ({armor[i][1]})' for i in group[1:] if i in remove))
for s in range(16):
    L = struct.unpack_from('>I', rom, 0xF2000 + 4 * s)[0]; n = rom[L]; ids = list(rom[L + 1:L + 1 + n])
    new = [i for i in ids if i not in remove]
    if new != ids:
        rom[L] = len(new); rom[L + 1:L + 1 + n] = bytes(new + [0] * (n - len(new)))
        print(f'shop {s}: {n} -> {len(new)} items')
rom[0xFFFCC:0xFFFD0] = struct.pack('>I', game_checksum(bytes(rom)))
rom[0x18E:0x190] = struct.pack('>H', header_checksum(bytes(rom)))
open(out_rom, 'wb').write(rom); open(out_ips, 'wb').write(make_ips(orig, bytes(rom)))
print('checksums', rom[0xFFFCC:0xFFFD0].hex(' ').upper(), '/', rom[0x18E:0x190].hex(' ').upper())
