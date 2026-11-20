#!/usr/bin/env python3
"""ECL disassembler for Buck Rogers: Countdown to Doomsday (Genesis).
  python ecl_disasm.py ROM.md OUTDIR
Writes OUTDIR/ecl_XX.asm for the 27 modules and OUTDIR/SUMMARY.txt. Needs decoder2.py + decoder.py beside it.

Format (verified 2026-10-04 on all 27 modules):
  * module tables: ECL 0x38CE2, text 0x42B0A, same 27 ids: [u32 -> offset table][ids .. FF][u32 offsets]
  * module loaded at 0x6AF6; first 20 bytes = 5 GOTO entry points
  * instruction = opcode byte + operands; operand = [type][lo]([hi]):
      00 imm byte | 01 memory word [addr] | 02 imm word #n | 03 address &addr
      80 text: u16 byte offset into the same-id text module | 81 string variable (u16)
  * variable-length: ONGOTO/ONGOSUB (selector, n, n targets), WHMENU/HMENU (dest, n, n texts),
    TREASURE (credits, n, n item ids)
  * path ends: EXIT, GOTO, RETURN, NEWECL, ENCEXIT (unless the previous instruction is an IF). PRINTRETURN (0x33)
    does NOT end a path in this build (code follows it everywhere). NEWECL ends it: module 0x5E 6B3D would
    otherwise fall into "YOU CANNOT LAND".
Checks on the whole ROM: 1,764/1,764 jump targets on instruction starts, 2,279/2,279 text operands on string
starts, no overlapping decodes, 58,442 of 61,173 bytes reached; the rest is data tables or unreferenced code."""
import struct, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from decoder2 import _decode_segment, decode_stream

BASE = 0x6AF6
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
IFS = range(0x16, 0x1C)


class Bad(Exception):
    pass


def load(rom):
    src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'decoder2.py')).read().replace(
        'raise ValueError(f"bad code {code:#x} (E={E:#x})")', 'return bytes(out), src.bit_pos, "BAD"')
    ns = {}; exec(compile(src, 'tolerant', 'exec'), ns)

    def table(t):
        base = struct.unpack_from('>I', rom, t)[0]; ids = []; i = t + 4
        while rom[i] != 0xFF: ids.append(rom[i]); i += 1
        return [(m, base + struct.unpack_from('>I', rom, base + 4 * k)[0]) for k, m in enumerate(ids)]

    def blob(a, nxt):
        bit = a * 8; out = b''
        while True:
            o, nb, last = ns['_decode_segment'](rom, bit, 0x2C00); out += o
            if last == 0x100: bit = nb; continue
            if last == 'BAD' and not (nxt and all(b == 0 for b in rom[(nb + 7) // 8:nxt])):
                raise SystemExit(f'bad stream at 0x{a:X}')
            return out

    def all_of(t):
        tb = table(t)
        return {m: blob(a, tb[k + 1][1] if k + 1 < len(tb) else None) for k, (m, a) in enumerate(tb)}, dict(tb)
    ecl, eaddr = all_of(0x38CE2); txt, _ = all_of(0x42B0A)
    t = 0x446E; names = []
    for k in range(94):
        o = struct.unpack_from('>H', rom, t + 2 * k)[0]; names.append(rom[t + o:rom.index(0, t + o)].decode())
    mon = b''.join(decode_stream(rom, 0x9E77C)[0]); mids = mon[2:56]
    mnames = {mids[i]: mon[56 + i * 214:72 + i * 214].split(b'\0')[0].decode('latin1') for i in range(54)}
    return ecl, eaddr, txt, names, mnames


def operand(d, p):
    if p + 1 >= len(d): raise Bad('eof')
    c, lo = d[p], d[p + 1]
    if c not in (0, 1, 2, 3, 0x80, 0x81): raise Bad(f'bad operand type {c:#x}')
    if c == 0: return (c, lo), p + 2
    if p + 2 >= len(d): raise Bad('eof')
    return (c, lo | d[p + 2] << 8), p + 3


def instr(d, p):
    op = d[p]
    if op in VAR:
        a, q = operand(d, p + 1); n, q = operand(d, q)
        if n[0] != 0: raise Bad('count operand not immediate')
        ops = [a, n]
        for _ in range(n[1]): o, q = operand(d, q); ops.append(o)
        return op, ops, q
    if op not in CNT: raise Bad(f'opcode {op:#x} has no known operand count')
    ops = []; q = p + 1
    for _ in range(CNT[op]): o, q = operand(d, q); ops.append(o)
    return op, ops, q


def jumps(op, ops):
    if op in (1, 2): return [ops[0][1]]
    if op in (0x25, 0x26): return [o[1] for o in ops[2:]]
    return []


def walk(d):
    todo = [0, 4, 8, 12, 16]; seen = {}; labels = set()
    while todo:
        p = todo.pop()
        while 0 <= p < len(d) and p not in seen:
            try: op, ops, q = instr(d, p)
            except Bad: break
            seen[p] = (op, ops, q)
            for t in jumps(op, ops):
                labels.add(t - BASE); todo.append(t - BASE)
            if op in TERM and not any(v[2] == p and v[0] in IFS for v in seen.values()): break
            p = q
    return seen, labels


def main():
    rom = open(sys.argv[1], 'rb').read(); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
    ecl, eaddr, txt, names, mnames = load(rom)
    summ = []
    for m, d in ecl.items():
        t = txt[m]
        def text(off):
            if not (0 <= off < len(t)) or (off and t[off - 1]): return '<not a string start>'
            s = t[off:t.index(0, off)].decode('latin1')
            return s if len(s) <= 70 else s[:67] + '...'
        def fmt(o):
            c, v = o
            return {0: f'{v}', 1: f'[{v:04X}]', 2: f'#{v}', 3: f'&{v:04X}', 0x80: f'T{v:04X}', 0x81: f'S{v:04X}'}[c]
        seen, labels = walk(d)
        lines = [f'; ECL module 0x{m:02X}  ROM 0x{eaddr[m]:05X}  {len(d)} bytes  load 0x{BASE:04X}',
                 '; entries: ' + ', '.join(f'{n}=L{(d[4*i+2] | d[4*i+3] << 8):04X}' for i, n in
                                           enumerate(['run', 'search', 'precamp', 'campint', 'init']))]
        p = 0; reached = sum(v[2] - k for k, v in seen.items())
        while p < len(d):
            if p in seen:
                op, ops, q = seen[p]
                if p in labels: lines.append(f'L{p + BASE:04X}:')
                args = ', '.join(('L' + f'{o[1]:04X}') if (op in (1, 2) or (op in (0x25, 0x26) and k >= 2)) else fmt(o)
                                 for k, o in enumerate(ops))
                cm = [f'"{text(o[1])}"' for o in ops if o[0] == 0x80]
                if op == 0x0B and ops[0][0] == 0: cm.append(mnames.get(ops[0][1], '?monster'))
                lines.append(f'  {p + BASE:04X}  {d[p:q].hex(" "):<30.30s} {names[op]:<13s} {args}'
                             + (f'   ; {" | ".join(cm)}' if cm else ''))
                p = q
            else:
                j = p
                while j < len(d) and j not in seen: j += 1
                lines.append(f'; --- {j - p} bytes not reached from the entry points (data table or unreferenced code)')
                for k in range(p, j, 16): lines.append(f'  {k + BASE:04X}  .db {d[k:min(k + 16, j)].hex(" ")}')
                p = j
        open(os.path.join(out, f'ecl_{m:02X}.asm'), 'w').write('\n'.join(lines) + '\n')
        summ.append(f'module 0x{m:02X}: {len(d):5d} bytes, {len(seen):4d} instructions, {reached / len(d):6.1%} reached, '
                    f'first text: "{t.split(bytes(1))[0][:50].decode("latin1")}"')
    open(os.path.join(out, 'SUMMARY.txt'), 'w').write('\n'.join(summ) + '\n')
    print('\n'.join(summ))


if __name__ == '__main__':
    main()
