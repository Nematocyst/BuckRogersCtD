#!/usr/bin/env python3
"""Combined patch builder for Buck Rogers: Countdown to Doomsday (Genesis).

Applies any mix of changes to a FRESH original ROM and computes the checksums ONCE, so patches that each carry
their own checksum fix (and break when stacked) can be combined:

  python build_combined.py ORIGINAL.md OUT.md OUT.ips [components...]
    --ips FILE              apply an IPS patch (its writes to the checksum bytes are ignored)
    --level9                level-9 cap (XP sentinels 0xC32/0xC52/0xC72/0xC92 -> 110000/192000/250000/110000;
                            alone it reproduces level9_cap_patch.ips's published checksums 2A C1 31 17 / 0B B0)
    --monster "EDIT"        monster edit, same forms as build_monster_patch.py ("TERRINE WARRIOR=32",
                            "TERRINE WARRIOR:gear1=heavy body armor", "TERRINE WARRIOR@37=60")
    --weapon "ITEM:F=V"     weapon/armor table edit, same as build_weapon_patch.py ("laser pistol:sides=10")
    --byte ADDR=VAL         raw byte (hex or decimal)
    --ecl "EDIT"            script edit (see ecl_patch.py): "10:6D4E+=SAVE 1,[9DC1]" inserts before 6D4E in
                            module 0x10; T"TEXT" adds a new string; "10:6D3F=00 05" overwrites bytes
    --allow-override        let a later component overwrite bytes an earlier one changed (default: refuse)

Components are applied in command-line order. After each one the builder records which bytes it changed; if a
later component changes such a byte to a different value, the build stops and names both components.
Checksum bytes (0xFFFCC-0xFFFCF, 0x18E-0x18F) are owned by nobody and recomputed at the end.
Needs decoder2.py, decoder.py, encoder.py, build_monster_patch.py, build_weapon_patch.py beside it.
"""
import argparse, os, re, struct, sys
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from decoder2 import decode_stream
from encoder import encode_stream
from build_monster_patch import (game_checksum, header_checksum, make_ips, resolve_item, ITEM_NAMES,
                                 BLOB_ADDR, BODY, REC, HP_OFF, SLOT0, SLOT_LEN, LEA_OPERAND, FREE_START)
from build_weapon_patch import FIELD, item_id, rec_addr, NAMES as WNAMES
from ecl_patch import apply_ecl_edits, EclError

CHECKSUM_BYTES = set(range(0xFFFCC, 0xFFFD0)) | {0x18E, 0x18F}


def read_ips(path):
    d = open(path, 'rb').read()
    assert d[:5] == b'PATCH', f'{path}: not an IPS file'
    p = 5; recs = []
    while True:
        if d[p:p + 3] == b'EOF': break
        off = int.from_bytes(d[p:p + 3], 'big'); size = int.from_bytes(d[p + 3:p + 5], 'big'); p += 5
        if size == 0:                                   # RLE record
            n = int.from_bytes(d[p:p + 2], 'big'); val = d[p + 2]; p += 3
            recs.append((off, bytes([val]) * n))
        else:
            recs.append((off, d[p:p + size])); p += size
    return recs


def apply_ips(rom, path, log):
    skipped = 0
    for off, data in read_ips(path):
        for k, b in enumerate(data):
            a = off + k
            if a in CHECKSUM_BYTES: skipped += 1; continue
            if a >= len(rom): raise SystemExit(f'{path}: writes past the end of the ROM (0x{a:X})')
            rom[a] = b
    log.append(f'IPS {os.path.basename(path)} ({skipped} checksum bytes ignored)')


def apply_level9(rom, log):
    for a, v in ((0xC32, 110000), (0xC52, 192000), (0xC72, 250000), (0xC92, 110000)):
        assert rom[a:a + 4] == b'\xff\xff\xff\xff', f'level-9: sentinel at 0x{a:X} already changed'
        rom[a:a + 4] = struct.pack('>I', v)
    log.append('level-9 cap (rocket jock 110000, medic 192000, warrior 250000, rogue 110000)')


def apply_monsters(rom, edits, log):
    segs, endbit = decode_stream(bytes(rom), BLOB_ADDR)
    nbits = endbit - BLOB_ADDR * 8; clen = (nbits + 7) // 8
    seg_lens = [len(x) for x in segs]; blob = bytearray(b''.join(segs))
    assert len(blob) == BODY + 54 * REC, 'monster file has unexpected size (stream already modified?)'
    recs = {}
    for i in range(54):
        o = BODY + i * REC; e = o
        while blob[e]: e += 1
        recs.setdefault(bytes(blob[o:e]).decode('latin1'), []).append(o)
    for e in edits:
        g = re.match(r'(.+?):gear(\d+)=(.+)$', e); b = re.match(r'(.+?)@(\w+)=(\w+)$', e)
        name = (g or b or re.match(r'(.+?)[=*]', e)).group(1)
        if name not in recs: raise SystemExit(f'unknown monster {name!r}')
        for o in recs[name]:
            if g:
                k = int(g.group(2)); iid, rec = resolve_item(rom, g.group(3)); a = o + SLOT0 + k * SLOT_LEN
                new = bytearray(rec); new[5] = blob[a + 5]; new[8] = blob[a + 8]; blob[a:a + SLOT_LEN] = new
            elif b:
                off, val = int(b.group(2), 0), int(b.group(3), 0); assert 0 <= off < REC and 0 <= val <= 255
                blob[o + off] = val
            else:
                m = re.match(r'(.+?)(=|\*)([\d.]+)$', e); val = float(m.group(3)); cur = blob[o + HP_OFF]
                blob[o + HP_OFF] = int(val) if m.group(2) == '=' else min(255, max(1, round(cur * val)))
        log.append(f'monster: {e}')
    cuts = [sum(seg_lens[:k]) for k in range(len(seg_lens) + 1)]
    new_segs = [bytes(blob[cuts[k]:cuts[k + 1]]) for k in range(len(seg_lens))]
    stream = encode_stream(new_segs).to_bytes()
    assert decode_stream(stream, 0)[0] == new_segs, 'monster stream round-trip failed'
    if len(stream) <= clen:
        rom[BLOB_ADDR:BLOB_ADDR + len(stream)] = stream; where = f'in place at 0x{BLOB_ADDR:X}'
    else:
        if not all(x == 0xFF for x in rom[FREE_START:FREE_START + len(stream)]):
            raise SystemExit('monster stream grew and the free space at 0xF8000 is not empty (used by another component?)')
        rom[FREE_START:FREE_START + len(stream)] = stream
        rom[LEA_OPERAND:LEA_OPERAND + 4] = struct.pack('>I', FREE_START); where = f'relocated to 0x{FREE_START:X}'
    log.append(f'  monster stream re-encoded: {clen} -> {len(stream)} bytes, {where}')


def apply_weapon(rom, e, log):
    name, kv = e.split(':', 1); f, v = kv.split('='); i = item_id(name); o = rec_addr(i) + FIELD[f.strip()]
    v = int(v, 0); assert 0 <= v <= 255; rom[o] = v
    log.append(f'weapon: {WNAMES[i - 1]} {f.strip()} = {v} (0x{o:05X})')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('original'); ap.add_argument('out_rom'); ap.add_argument('out_ips')
    ap.add_argument('--allow-override', action='store_true')
    comps = []
    class Add(argparse.Action):
        def __call__(self, p, ns, values, opt=None): comps.append((self.dest, values))
    ap.add_argument('--ips', action=Add); ap.add_argument('--monster', action=Add); ap.add_argument('--weapon', action=Add)
    ap.add_argument('--byte', action=Add); ap.add_argument('--level9', action=Add, nargs=0)
    ap.add_argument('--ecl', action=Add)
    a = ap.parse_args()
    orig = open(a.original, 'rb').read()
    if game_checksum(orig) != struct.unpack('>I', orig[0xFFFCC:0xFFFD0])[0] or len(orig) != 0x100000:
        raise SystemExit('input is not the original ROM (checksum/size mismatch); always start from a fresh copy')
    rom = bytearray(orig); owner = {}; log = []
    # monster edits are applied together (one re-encode) at the position of the first --monster
    mons = [v for k, v in comps if k == 'monster']; done_mon = False
    ecls = [v for k, v in comps if k == 'ecl']; done_ecl = False
    for kind, val in comps:
        if kind == 'monster' and done_mon: continue
        if kind == 'ecl' and done_ecl: continue
        before = bytes(rom); label = f'{kind} {val}' if kind != 'level9' else 'level9'
        if kind == 'ips': apply_ips(rom, val, log)
        elif kind == 'level9': apply_level9(rom, log)
        elif kind == 'monster': apply_monsters(rom, mons, log); done_mon = True; label = 'monster edits'
        elif kind == 'weapon': apply_weapon(rom, val, log)
        elif kind == 'ecl':
            try: apply_ecl_edits(rom, ecls, log)
            except EclError as ex: raise SystemExit(f'script edit refused: {ex}')
            done_ecl = True; label = 'script edits'
        elif kind == 'byte':
            ad, v = val.split('='); ad, v = int(ad, 0), int(v, 0); rom[ad] = v; log.append(f'byte 0x{ad:X} = {v}')
        for i in (i for i in range(len(rom)) if rom[i] != before[i]):
            if i in CHECKSUM_BYTES: continue
            if i in owner and owner[i] != label and not a.allow_override:
                raise SystemExit(f'CONFLICT at 0x{i:X}: "{label}" overwrites a byte changed by "{owner[i]}" '
                                 f'({before[i]:#04x} -> {rom[i]:#04x}). Use --allow-override to accept.')
            owner[i] = label
    rom[0xFFFCC:0xFFFD0] = struct.pack('>I', game_checksum(bytes(rom)))
    rom[0x18E:0x190] = struct.pack('>H', header_checksum(bytes(rom)))
    assert game_checksum(bytes(rom)) == struct.unpack('>I', rom[0xFFFCC:0xFFFD0])[0]
    open(a.out_rom, 'wb').write(rom); open(a.out_ips, 'wb').write(make_ips(orig, bytes(rom)))
    print('\n'.join(log))
    print(f'{sum(1 for x, y in zip(orig, rom) if x != y)} bytes differ from the original; '
          f'boot constant {rom[0xFFFCC:0xFFFD0].hex(" ").upper()}, header {rom[0x18E:0x190].hex(" ").upper()}')
    print(f'wrote {a.out_rom} and {a.out_ips}')


if __name__ == '__main__':
    main()
