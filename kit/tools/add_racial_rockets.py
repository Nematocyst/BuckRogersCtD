#!/usr/bin/env python3
"""Add a Venusian rocket pistol (id 0x5D, sold at New Elysium = shop 5) and a Lunarian rocket pistol (id 0x5E, Tycho =
shop 9) to a build that already has the relocated item table at 0xF2200 (v9 / v10 / v11 and their combinations).
  python add_racial_rockets.py ORIGINAL.md OUT.md OUT.ips --ips BASE.ips [--ips MORE.ips ...]
Records copy the rocket pistol with the race word and racial tier (Venusian 0x14 / tier 2, Lunarian 0x16 / tier 4).
Prices follow the existing rocket pistols (plain 400, Martian 1000 = x2.5, Mercurian 4000 = x10): Venusian 2000 (x5),
Lunarian 8000 (x20)."""
import sys, os, struct
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_combined import read_ips, CHECKSUM_BYTES
from build_monster_patch import game_checksum, header_checksum, make_ips
args = sys.argv[1:]; src, out_rom, out_ips = args[:3]
ipses = [args[i + 1] for i, a in enumerate(args) if a == '--ips']
orig = open(src, 'rb').read(); rom = bytearray(orig)
for p in ipses:
    for off, data in read_ips(p):
        for k, b in enumerate(data):
            if off + k not in CHECKSUM_BYTES: rom[off + k] = b
TB = struct.unpack_from('>I', rom, 0xBF0)[0]; assert TB == 0xF2200, hex(TB)
for op in (0xBF0, 0x16254, 0x1628C): assert struct.unpack_from('>I', rom, op)[0] == TB
assert rom[TB + 10 * 0x5C] == 0x06 and rom[TB + 10 * 0x5C + 3] == 0x16, 'expected the Lunarian needle gun at 0x5C'
NEW = {0x5D: (bytes.fromhex('0a0a0b14020007d00000'), 5), 0x5E: (bytes.fromhex('0a0a0b1604001f400000'), 9)}
for iid, (rec, shop) in NEW.items():
    a = TB + 10 * iid
    assert all(b == 0xFF for b in rom[a:a + 10]), f'slot for {iid:#x} not free'
    rom[a:a + 10] = rec
    L = struct.unpack_from('>I', rom, 0xF2000 + 4 * shop)[0]; n = rom[L]; assert n < 14 and iid not in rom[L + 1:L + 1 + n]
    rom[L + 1 + n] = iid; rom[L] = n + 1
    print(f'id {iid:#x} {rec.hex(" ")} -> shop {shop} ({n + 1} items)')
rom[0xFFFCC:0xFFFD0] = struct.pack('>I', game_checksum(bytes(rom)))
rom[0x18E:0x190] = struct.pack('>H', header_checksum(bytes(rom)))
open(out_rom, 'wb').write(rom); open(out_ips, 'wb').write(make_ips(orig, bytes(rom)))
print('checksums', rom[0xFFFCC:0xFFFD0].hex(' ').upper(), '/', rom[0x18E:0x190].hex(' ').upper())
