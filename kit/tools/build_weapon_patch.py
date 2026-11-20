#!/usr/bin/env python3
"""Edit the weapon/armor stat table of Buck Rogers (Genesis) and write a patched ROM + IPS (checksums fixed).
  python build_weapon_patch.py ROM.md OUT.md OUT.ips "laser pistol:sides=10" "rocket pistol:dice=2" ...
Table: ROM 0x77A6, 38 records x 8 bytes, record for item id i at 0x779E + 8*i (ids 0x01-0x26, base items only;
racial variants 0x27+ are not in it). Fields:
  class  b0  item class (0 weapon/grenade, 1 armor, 2-8 other)          [read]
  type   b1  weapon type (0 melee, 1 needle/bolt/x-bow, 2 rocket, 3 laser, 8 grenade ...)
  armor  b1  armor value for class-1 items (spacesuit 54 .. b.armor w/fields 62; combat armor = 50 + value - 52)
  rof2   b2  attacks per round x2 (laser pistol 6 = 3 attacks, as the character screen shows)
  dice   b3  number of damage dice
  sides  b4  damage die size            [verified live: laser pistol d8 -> d20 gave hits of 15, 16, 11]
  bonus  b5  (stun grenade 2; meaning not tested)
  flag   b6  (1 on grenade launcher, plasma thrower, rocket launcher)
  range  b7  range (grenades 3 ... laser rifle 120)
Fields other than 'sides' are read from the table layout and the character screen, not yet live-tested."""
import sys, struct
from build_monster_patch import game_checksum, header_checksum, make_ips
NAMES = ['knife','mono knife','cutlass','sword','d.r. x-bow','needle gun','bolt gun','mono sword','laser pistol','rocket pistol',
 'rocket rifle','cover gun','laser rifle','heat gun','sonic stunner','grenade launcher','plasma thrower','rocket launcher','polearm',
 'spacesuit','smartsuit','heavy body armor','battle armor','battle armor w/fields','demo charge','ecm package','protective goggles',
 'lost.','rope','belt','poison antidote','mini explosive','explosive grenade','treating wounds grenade','stun grenade',
 'dazzle grenade','chaff grenade','aerosol mist grenade']
FIELD = {'class':0,'type':1,'armor':1,'rof2':2,'dice':3,'sides':4,'bonus':5,'flag':6,'range':7}
def rec_addr(i): return 0x779E + 8 * i
def item_id(text):
    t = text.strip().lower()
    if t.startswith('0x'): return int(t, 16)
    if t in NAMES: return NAMES.index(t) + 1
    raise SystemExit(f'unknown item "{text}"; use a name from the list or 0xNN')
def main():
    src, out, ips, *edits = sys.argv[1:]
    orig = open(src, 'rb').read(); rom = bytearray(orig)
    assert game_checksum(orig) == struct.unpack('>I', orig[0xFFFCC:0xFFFD0])[0], 'input ROM checksum mismatch (not original?)'
    for e in edits:
        name, kv = e.split(':', 1); f, v = kv.split('='); i = item_id(name); o = rec_addr(i) + FIELD[f.strip()]
        v = int(v, 0); assert 0 <= v <= 255
        print(f'  {NAMES[i-1]} (0x{i:02X}) {f}: {rom[o]} -> {v}   [ROM 0x{o:05X}]'); rom[o] = v
    rom[0xFFFCC:0xFFFD0] = struct.pack('>I', game_checksum(bytes(rom)))
    rom[0x18E:0x190] = struct.pack('>H', header_checksum(bytes(rom)))
    open(out, 'wb').write(rom); open(ips, 'wb').write(make_ips(orig, bytes(rom)))
    print(f'wrote {out} and {ips}; boot constant {rom[0xFFFCC:0xFFFD0].hex()} header {rom[0x18E:0x190].hex()}')
if __name__ == '__main__':
    main()
