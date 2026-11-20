#!/usr/bin/env python3
"""
Edit monsters in the Buck Rogers (Genesis) ROM: HP and equipment.

Monster file = LZW stream at 0x9E77C (2 segments: 0x100 = dictionary reset, 0x101 = end):
  [00 36][54 ids] then 54 records of 214 bytes (all 54 monsters incl. Talon).
  record +0..15  name, +46 HP, +84 + 10*k  = 10-byte item slot k (id 0 ends the list)

Usage:
  build_monster_patch.py ORIGINAL.md OUT.md OUT.ips EDIT [EDIT ...]
  EDIT forms:
    "TERRINE WARRIOR=32"              set HP
    "TERRINE WARRIOR*1.5"             scale HP (max 255)
    "TERRINE WARRIOR:gear1=heavy body armor"   replace item slot 1 (0-based)
    "TERRINE WARRIOR:gear0=0x0A"                 item by hex id
    "TERRINE WARRIOR@37=60"           set any record byte (offset and value decimal or 0x..)
  A gear edit copies the 10-byte record of the BASE item (table at 0xF17D6)
  into the slot, keeping the replaced slot's byte +5 and byte +8 flags.
  Race-variant items (Martian/Venusian/...) are not supported yet.
"""
import os, struct, sys, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))   # decoder2.py and encoder.py sit beside this file
from decoder2 import decode_stream
from encoder import encode_stream

BLOB_ADDR = 0x9E77C
BODY, REC, HP_OFF, SLOT0, SLOT_LEN, NSLOTS = 56, 214, 46, 84, 10, 13
LEA_OPERAND = 0x48F2
FREE_START = 0xF8000
ITEM_TABLE, ITEM_COUNT = 0xF17D6, 38
ITEM_NAMES = {1: 'knife', 2: 'mono knife', 3: 'cutlass', 4: 'sword', 5: 'd.r. x-bow', 6: 'needle gun',
              7: 'bolt gun', 8: 'mono sword', 9: 'laser pistol', 10: 'rocket pistol', 11: 'rocket rifle',
              14: 'heat gun', 16: 'grenade launcher', 17: 'plasma thrower', 20: 'spacesuit', 21: 'smartsuit',
              22: 'heavy body armor', 27: 'protective goggles', 32: 'mini explosive', 33: 'explosive grenade',
              35: 'stun grenade', 36: 'dazzle grenade', 37: 'chaff grenade', 38: 'aerosol mist grenade'}


def game_checksum(rom):
    d0 = 0; a0 = 0
    while a0 < 0xFFFB0:
        if a0 != 0x18C:
            d0 = (d0 + struct.unpack_from('>I', rom, a0)[0]) & 0xFFFFFFFF
        a0 += 4
    return d0


def header_checksum(rom):
    return sum(struct.unpack_from('>H', rom, a)[0] for a in range(0x200, len(rom), 2)) & 0xFFFF


def make_ips(orig, new):
    recs = []; i = 0; n = len(orig)
    while i < n:
        if orig[i] == new[i]:
            i += 1; continue
        j = i
        while j < n and (orig[j] != new[j] or (j + 1 < n and orig[j + 1] != new[j + 1] and j - i < 0xFFF0)):
            j += 1
        j = min(j, i + 0xFFFF)
        recs.append((i, new[i:j])); i = j
    out = bytearray(b'PATCH')
    for off, data in recs:
        if off == 0x454F46: off -= 1
        out += off.to_bytes(3, 'big') + len(data).to_bytes(2, 'big') + data
    return bytes(out + b'EOF')


def resolve_item(rom, text):
    text = text.strip().lower()
    if re.fullmatch(r'0x[0-9a-f]+|\d+', text):
        iid = int(text, 0)
    else:
        by_name = {v: k for k, v in ITEM_NAMES.items()}
        if text not in by_name:
            raise SystemExit(f'unknown item name {text!r}; known: {sorted(by_name)} (or use a hex id)')
        iid = by_name[text]
    rec = bytes(rom[ITEM_TABLE + (iid - 1) * 10: ITEM_TABLE + iid * 10])
    assert rec[0] == iid, f'item table record for id {iid:#x} does not start with its id: {rec.hex(" ")}'
    return iid, rec


def main():
    orig_path, out_rom, out_ips, *edits = sys.argv[1:]
    rom = bytearray(open(orig_path, 'rb').read())
    segs, endbit = decode_stream(bytes(rom), BLOB_ADDR)
    nbits = endbit - BLOB_ADDR * 8
    clen = (nbits + 7) // 8
    orig_bits = [(rom[(BLOB_ADDR * 8 + i) // 8] >> (7 - ((BLOB_ADDR * 8 + i) % 8))) & 1 for i in range(nbits)]
    assert encode_stream(segs).bits == orig_bits, "encoder does not reproduce the stream of this ROM"
    seg_lens = [len(x) for x in segs]
    blob = bytearray(b''.join(segs))
    assert len(blob) == BODY + 54 * REC, f"unexpected monster file size {len(blob)}"
    def name_at(o):
        e = o
        while blob[e] != 0: e += 1
        return bytes(blob[o:e]).decode('latin1')
    recs = {}
    for i in range(54):
        o = BODY + i * REC
        recs.setdefault(name_at(o), []).append(o)

    for e in edits:
        g = re.match(r'(.+?):gear(\d+)=(.+)$', e)
        if g:
            name, k, item = g.group(1), int(g.group(2)), g.group(3)
            iid, rec = resolve_item(rom, item)
            for o in recs[name]:
                a = o + SLOT0 + k * SLOT_LEN
                old = bytes(blob[a:a + SLOT_LEN])
                new = bytearray(rec); new[5] = old[5]; new[8] = old[8]
                blob[a:a + SLOT_LEN] = new
                print(f'  {name}: gear slot {k}  {old.hex(" ")}  ->  {bytes(new).hex(" ")}  ({ITEM_NAMES.get(iid, hex(iid))})')
            continue
        b = re.match(r'(.+?)@(\w+)=(\w+)$', e)
        if b:
            name, off, val = b.group(1), int(b.group(2), 0), int(b.group(3), 0)
            assert 0 <= off < REC and 0 <= val <= 255, e
            for o in recs[name]:
                print(f'  {name}: +{off} {blob[o + off]} -> {val}')
                blob[o + off] = val
            continue
        m = re.match(r'(.+?)(=|\*)([\d.]+)$', e)
        name, op, val = m.group(1), m.group(2), float(m.group(3))
        for o in recs[name]:
            cur = blob[o + HP_OFF]
            new = int(val) if op == '=' else min(255, max(1, round(cur * val)))
            print(f'  {name}: HP {cur} -> {new}')
            blob[o + HP_OFF] = new

    # re-split at the original segment boundaries, encode with fresh dictionaries
    cuts = [sum(seg_lens[:k]) for k in range(len(seg_lens) + 1)]
    new_segs = [bytes(blob[cuts[k]:cuts[k + 1]]) for k in range(len(seg_lens))]
    stream = encode_stream(new_segs).to_bytes()
    check, _ = decode_stream(stream, 0)
    assert check == new_segs, "round-trip failed"
    print(f'compressed: original {clen} bytes -> new {len(stream)} bytes (segments {seg_lens})')
    if len(stream) <= clen:
        rom[BLOB_ADDR:BLOB_ADDR + len(stream)] = stream
        print('placed in place at 0x%X' % BLOB_ADDR)
    else:
        assert all(b == 0xFF for b in rom[FREE_START:FREE_START + len(stream)]), "free space at 0xF8000 is not empty"
        rom[FREE_START:FREE_START + len(stream)] = stream
        rom[LEA_OPERAND:LEA_OPERAND + 4] = struct.pack('>I', FREE_START)
        print('relocated to 0x%X; LEA operand at 0x%X repointed' % (FREE_START, LEA_OPERAND))
    rom[0xFFFCC:0xFFFD0] = struct.pack('>I', game_checksum(bytes(rom)))
    rom[0x18E:0x190] = struct.pack('>H', header_checksum(bytes(rom)))
    open(out_rom, 'wb').write(rom)
    orig = open(orig_path, 'rb').read()
    open(out_ips, 'wb').write(make_ips(orig, bytes(rom)))
    print(f'wrote {out_rom} ({sum(1 for a, b in zip(orig, rom) if a != b)} bytes differ) and {out_ips}')


if __name__ == '__main__':
    main()
