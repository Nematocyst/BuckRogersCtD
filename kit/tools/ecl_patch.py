#!/usr/bin/env python3
"""Script (ECL) edits for Buck Rogers: Countdown to Doomsday (Genesis), written back into the ROM.

Edit strings (module id in hex, ECL address in hex as in the ecl_disasm.py listings):
  "10:6D4E+=SAVE 1,[9DC1]"                  insert instruction(s) before the instruction at 6D4E (';' separates)
  "10:6D4E+=PRINTCLEAR T\"AMBUSH!\"; SAVE 1,[9DC1]"   T"..." appends a new string to text module 10 and uses its offset
  "10:6D3F=00 05"                            overwrite bytes in place (same length)
Operand syntax = the disassembler's: 5 (byte) | #500 (word) | [9DC1] (memory) | &9E71 (address) | T001A (text
offset) | S0001 (string var) | L6B40 (jump target, for GOTO/GOSUB/ONGOTO).

The game overwrites the last byte of a module after loading it (verified live), so new code is always followed by
2 padding bytes. How an insertion works (no addresses move): whole instructions starting at the site, at least 4 bytes, are
replaced by "GOTO <new code>" (+ EXIT padding, never executed); the new code is appended at the module's end and is
followed by the displaced instructions and "GOTO <site + displaced length>". Refused when: the site follows an IF
(the IF would skip only the GOTO), an IF is among the displaced instructions, or a jump target falls inside the
displaced bytes after the site.

Placement: a module that still fits its original slot is written in place, otherwise it moves to free space
(default from 0xFB000) and only its entry in the module table (ECL 0x38CE2 / text 0x42B0A) changes.
Verified: all 27 ECL modules and 26 text modules (not 0x61, which has no END code) re-encode bit for bit.
"""
import os, re, struct, sys
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from decoder2 import _decode_segment
from encoder import encode_stream

BASE = 0x6AF6
ECL_TABLE, TEXT_TABLE = 0x38CE2, 0x42B0A
LOAD_LIMIT = 0x96F6 - BASE                       # decoded module must fit the load buffer
FREE_DEFAULT = 0xFB000
NAMES = []                                       # filled from the ROM's opcode-name table (0x446E)
def init_names(rom):
    if NAMES: return
    for k in range(94):
        o = struct.unpack_from('>H', rom, 0x446E + 2 * k)[0]; e = rom.index(0, 0x446E + o)
        NAMES.append(bytes(rom[0x446E + o:e]).decode())
# operand counts (ecl_disasm.py); variable-length opcodes: first operand, count n, n operands
CNT = {0x00: 0, 0x01: 1, 0x02: 1, 0x03: 2, 0x04: 3, 0x05: 3, 0x06: 3, 0x07: 3, 0x08: 2, 0x09: 2, 0x0A: 1,
       0x0B: 3, 0x0C: 4, 0x0D: 0, 0x0E: 1, 0x0F: 2, 0x10: 2, 0x11: 1, 0x12: 1, 0x13: 0, 0x14: 4, 0x16: 0,
       0x17: 0, 0x18: 0, 0x19: 0, 0x1A: 0, 0x1B: 0, 0x1C: 0, 0x1F: 4, 0x20: 1, 0x21: 3, 0x22: 3, 0x23: 3,
       0x24: 0, 0x29: 0, 0x2A: 3, 0x2C: 0, 0x2D: 0, 0x2E: 5, 0x2F: 3, 0x30: 3, 0x32: 1, 0x33: 0, 0x35: 3,
       0x36: 2, 0x37: 1, 0x38: 1, 0x39: 1, 0x3A: 0, 0x3D: 0, 0x3E: 0, 0x40: 2, 0x41: 2, 0x42: 0, 0x43: 1,
       0x44: 0, 0x45: 2, 0x46: 2, 0x47: 0, 0x48: 1, 0x49: 7, 0x4A: 0, 0x4B: 1, 0x4C: 2, 0x4E: 0, 0x4F: 0,
       0x50: 0, 0x51: 1, 0x52: 0, 0x53: 4, 0x54: 3, 0x55: 4, 0x56: 3, 0x57: 1, 0x58: 0, 0x59: 1, 0x5A: 0,
       0x5B: 0, 0x5C: 6}
VAR = {0x25, 0x26, 0x27, 0x2B, 0x31}
TERM = {0x00, 0x01, 0x13, 0x20, 0x42}
IFS = set(range(0x16, 0x1C))


class EclError(Exception):
    pass


# ---------------------------------------------------------------- tables and streams
def read_table(rom, t):
    base = struct.unpack_from('>I', rom, t)[0]; ids = []; i = t + 4
    while rom[i] != 0xFF: ids.append(rom[i]); i += 1
    return base, ids, [base + struct.unpack_from('>I', rom, base + 4 * k)[0] for k in range(len(ids))]


def decode_module(rom, addr):
    bit = addr * 8; segs = []
    while True:
        out, nb, last = _decode_segment(rom, bit, max_out=0x2C00)
        segs.append(out); bit = nb
        if last == 0x100: continue
        if last != 0x101: raise EclError(f'stream at 0x{addr:X} does not end with END')
        return segs, (bit - addr * 8 + 7) // 8


# ---------------------------------------------------------------- disassembly (for safety checks)
def operand(d, p):
    c = d[p]
    if c not in (0, 1, 2, 3, 0x80, 0x81): raise EclError(f'bad operand type {c:#x} at +{p:#x}')
    return ((c, d[p + 1]), p + 2) if c == 0 else ((c, d[p + 1] | d[p + 2] << 8), p + 3)


def instr(d, p):
    op = d[p]
    if op in VAR:
        a, q = operand(d, p + 1); n, q = operand(d, q); ops = [a, n]
        for _ in range(n[1]): o, q = operand(d, q); ops.append(o)
        return op, ops, q
    if op not in CNT: raise EclError(f'opcode {op:#x} without known operand count at {BASE + p:04X}')
    ops = []; q = p + 1
    for _ in range(CNT[op]): o, q = operand(d, q); ops.append(o)
    return op, ops, q


def walk(d):
    todo = [0, 4, 8, 12, 16]; seen = {}; labels = set()
    while todo:
        p = todo.pop()
        while 0 <= p < len(d) and p not in seen:
            try: op, ops, q = instr(d, p)
            except EclError: break
            seen[p] = (op, ops, q)
            tg = [ops[0][1]] if op in (1, 2) else ([o[1] for o in ops[2:]] if op in (0x25, 0x26) else [])
            for t in tg: labels.add(t - BASE); todo.append(t - BASE)
            if op in TERM and not any(v[2] == p and v[0] in IFS for v in seen.values()): break
            p = q
    return seen, labels


# ---------------------------------------------------------------- assembler
def asm_operand(tok, text_add):
    tok = tok.strip()
    m = re.fullmatch(r'T"(.*)"', tok)
    if m: off = text_add(m.group(1)); return bytes([0x80, off & 0xFF, off >> 8])
    for pre, code in (('[', 1), ('&', 3), ('#', 2), ('T', 0x80), ('S', 0x81), ('L', 1)):
        if tok.startswith(pre):
            body = tok[1:-1] if pre == '[' else tok[1:]
            v = int(body, 10) if pre == '#' else int(body, 16)
            if not 0 <= v <= 0xFFFF: raise EclError(f'operand out of range: {tok}')
            return bytes([code, v & 0xFF, v >> 8])
    v = int(tok, 0)
    if not 0 <= v <= 255: raise EclError(f'byte operand out of range (use #n for words): {tok}')
    return bytes([0, v])


def split_operands(s):
    out, cur, q = [], '', False
    for ch in s:
        if ch == '"': q = not q
        if ch == ',' and not q: out.append(cur); cur = ''
        else: cur += ch
    if cur.strip(): out.append(cur)
    return out


def assemble(line, text_add):
    line = line.strip()
    m = re.match(r'([A-Z][A-Z0-9]*)\s*(.*)$', line)
    if not m or m.group(1) not in NAMES: raise EclError(f'unknown instruction: {line!r}')
    op = NAMES.index(m.group(1)); args = split_operands(m.group(2))
    if op in VAR:
        if len(args) < 2 or int(args[1], 0) != len(args) - 2:
            raise EclError(f'{m.group(1)} needs: first operand, count n, then n operands')
    elif op not in CNT: raise EclError(f'{m.group(1)}: operand count unknown (opcode never used in the game)')
    elif len(args) != CNT[op]: raise EclError(f'{m.group(1)} takes {CNT[op]} operand(s), got {len(args)}')
    return bytes([op]) + b''.join(asm_operand(a, text_add) for a in args)


def split_lines(s):
    out, cur, q = [], '', False
    for ch in s:
        if ch == '"': q = not q
        if ch == ';' and not q: out.append(cur); cur = ''
        else: cur += ch
    if cur.strip(): out.append(cur)
    return [x for x in out if x.strip()]


# ---------------------------------------------------------------- edits
def goto(addr): return bytes([0x01, 0x01, addr & 0xFF, addr >> 8])


def apply_ecl_edits(rom, edits, log, free_start=FREE_DEFAULT):
    """Apply ECL edit strings to bytearray rom in place. Returns the next free address used for relocation."""
    init_names(bytes(rom))
    ebase, eids, eaddr = read_table(rom, ECL_TABLE); tbase, tids, taddr = read_table(rom, TEXT_TABLE)
    mods, texts, new_text = {}, {}, {}
    def ecl(m):
        if m not in eids: raise EclError(f'no ECL module {m:#04x}')
        if m not in mods:
            segs, clen = decode_module(rom, eaddr[eids.index(m)])
            if len(segs) != 1: raise EclError(f'ECL module {m:#04x} has {len(segs)} segments')
            mods[m] = bytearray(segs[0])
        return mods[m]
    def text_adder(m):
        def add(s):
            if m == 0x61: raise EclError('text module 0x61 has no END code; not editable')
            if m not in texts:
                segs, _ = decode_module(rom, taddr[tids.index(m)])
                if len(segs) != 1: raise EclError(f'text module {m:#04x} has {len(segs)} segments')
                texts[m] = bytearray(segs[0])
            t = texts[m]; off = len(t)
            if not all(32 <= ord(c) < 127 for c in s): raise EclError(f'text must be printable ASCII: {s!r}')
            t += s.upper().encode() + b'\0'; new_text.setdefault(m, []).append((off, s.upper()))
            return off
        return add
    for e in edits:
        m = re.fullmatch(r'\s*([0-9A-Fa-f]{2}):([0-9A-Fa-f]{4})(\+=|=)(.*)', e, re.S)
        if not m: raise EclError(f'bad ECL edit {e!r} (want "MM:AAAA+=INSTR; INSTR" or "MM:AAAA=hex bytes")')
        mod, addr, kind, body = int(m.group(1), 16), int(m.group(2), 16), m.group(3), m.group(4)
        d = ecl(mod); p = addr - BASE
        if not 0 <= p < len(d): raise EclError(f'{e}: address outside module {mod:#04x}')
        if kind == '=':
            new = bytes.fromhex(body.replace(' ', ''))
            if p + len(new) > len(d): raise EclError(f'{e}: runs past the module end')
            d[p:p + len(new)] = new; log.append(f'ecl {mod:02X}:{addr:04X} overwrite {len(new)} byte(s)'); continue
        seen, labels = walk(bytes(d))
        if p not in seen: raise EclError(f'{e}: {addr:04X} is not a decoded instruction start in module {mod:02X}')
        if any(v[2] == p and v[0] in IFS for v in seen.values()):
            raise EclError(f'{e}: the instruction before {addr:04X} is an IF; insert one instruction earlier')
        q = p; moved = []
        while q - p < 4:
            if q not in seen: raise EclError(f'{e}: cannot displace 4 bytes from {addr:04X} (undecoded bytes)')
            op = seen[q][0]
            if op in IFS: raise EclError(f'{e}: an IF ({BASE + q:04X}) would be displaced; choose another site')
            if q != p and q in labels: raise EclError(f'{e}: jump target {BASE + q:04X} inside the displaced bytes')
            moved.append(bytes(d[q:seen[q][2]])); q = seen[q][2]
        code = b''.join(assemble(l, text_adder(mod)) for l in split_lines(body))
        new_at = len(d)
        tail = code + b''.join(moved) + goto(BASE + q)
        d += tail + bytes(2)        # padding: the game overwrites a loaded module's LAST byte (seen live)
        d[p:q] = goto(BASE + new_at) + bytes(q - p - 4)
        log.append(f'ecl {mod:02X}:{addr:04X} insert {len(code)} byte(s) of new code; displaced {q - p} byte(s) '
                   f'({", ".join(NAMES[x[0]] for x in moved)}) -> code at {BASE + new_at:04X}')
        if len(d) > LOAD_LIMIT: raise EclError(f'module {mod:02X} would exceed the load buffer ({len(d)} > {LOAD_LIMIT})')
    # sanity: every edited module still disassembles with all jump targets on instructions
    for mod, d in mods.items():
        seen, labels = walk(bytes(d))
        bad = [l for l in labels if l not in seen]
        if bad: raise EclError(f'module {mod:02X}: jump targets not on instructions after editing: {[hex(BASE + b) for b in bad]}')
    free = free_start
    for (tabbase, ids, addrs, store, kind) in ((ebase, eids, eaddr, mods, 'ECL'), (tbase, tids, taddr, texts, 'text')):
        for mod, data in store.items():
            k = ids.index(mod); start = addrs[k]
            later = [a for a in addrs if a > start]
            nxt = min(later) if later else start + decode_module(rom, start)[1]   # last module: its own length
            stream = encode_stream([bytes(data)]).to_bytes()
            segs, _ = decode_module(bytes(stream) + b'\0' * 4, 0)
            if segs != [bytes(data)]: raise EclError(f'{kind} module {mod:02X}: round trip failed')
            if len(stream) <= nxt - start:
                rom[start:start + len(stream)] = stream; where = f'in place at 0x{start:X}'
            else:
                free = (free + 1) & ~1
                if not all(b == 0xFF for b in rom[free:free + len(stream)]):
                    raise EclError(f'free space at 0x{free:X} is not empty')
                rom[free:free + len(stream)] = stream
                struct.pack_into('>I', rom, tabbase + 4 * k, free - tabbase)
                where = f'relocated to 0x{free:X} (table entry updated)'; free += len(stream)
            log.append(f'  {kind} module {mod:02X}: {len(data)} bytes decoded, {len(stream)} compressed, {where}')
    for m, lst in new_text.items():
        for off, s in lst: log.append(f'  new text in module {m:02X}: T{off:04X} = "{s}"')
    return free
