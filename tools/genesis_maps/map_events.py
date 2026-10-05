"""Cross-reference map plane-2 event codes with the ECL 'search' handler of each module.

The 'search' entry point of a map module (entry 2, e.g. L6C13 in module 0x10) starts with
    AND [9AF9], 63, [9E6F]      ; [9AF9] = plane-2 byte of the cell the party stands on
    ONGOTO [9E6F], N, L1 .. LN
so the event code is the plane-2 byte & 0x3F, and the ONGOTO table maps it to a handler label. The ROM's ONGOTO
handler (0x38EA) is 0-based: target[code]; code 0 / out-of-range falls through. Confirmed on map 0x10: code 1 sits on
the void cells around the burning area and target[1] is "THE HEAT FROM THE EXPLOSIONS DRIVES YOU BACK" (STEPBACK).
Cells outside the explorable area (plane-2 bit 7 clear) can carry codes too, so no cell is filtered out.
This script lists, per map, every event code with its cells, its handler label and a short summary of the
handler (first text printed, monsters loaded, module jumps, treasure).

usage: python map_events.py ECL_ASM_DIR genesis_maps.json OUT.md [OUT.json]"""
import sys, re, json, os, collections

LABEL = re.compile(r'^(L[0-9A-F]{4}):')
INSTR = re.compile(r'^\s+([0-9A-F]{4})\s+((?:[0-9a-f]{2} )*[0-9a-f]{2})\s+([A-Z0-9]+)\s*(.*)$')


def parse_module(path):
    """-> (instrs by addr, labels {name: addr}, ordered list of addrs)"""
    ins, labels, order, pend = {}, {}, [], []
    for line in open(path):
        m = LABEL.match(line)
        if m:
            pend.append(m.group(1)); continue
        m = INSTR.match(line)
        if m:
            a = int(m.group(1), 16)
            ins[a] = (m.group(3), m.group(4)); order.append(a)
            for p in pend: labels[p] = a
            pend = []
    return ins, labels, order


def dispatch(ins, order):
    """find AND [9AF9],63,[VAR] followed by ONGOTO/ONGOSUB [VAR]; return (var, [labels])"""
    for i, a in enumerate(order):
        op, args = ins[a]
        if op == 'AND' and '[9AF9]' in args and '63' in args:
            var = args.split(',')[-1].strip()
            for b in order[i + 1:i + 6]:
                o2, a2 = ins[b]
                if o2 in ('ONGOTO', 'ONGOSUB') and a2.split(',')[0].strip() == var:
                    f = [x.strip() for x in a2.split(',')]
                    return var, int(f[1]), f[2:2 + int(f[1])], o2
    return None


def summarize(ins, labels, order, label, limit=14):
    a = labels[label]; i = order.index(a)
    out, seen = [], 0
    for b in order[i:i + 40]:
        op, args = ins[b]
        if op.startswith('PRINT') and '"' in args and len(out) < 2:
            t = args.split('; ', 1)[-1].strip().strip('"')
            out.append('"' + t + '"')
        elif op in ('LOADMONSTER',):
            out.append(args.split(';')[-1].strip().title() or args)
        elif op == 'NEWECL': out.append('-> module ' + args)
        elif op in ('TREASURE', 'COMBAT', 'ENCEXIT', 'SPACECOMBAT', 'DUEL'): out.append(op + (' ' + args if op == 'TREASURE' else ''))
        if op in ('EXIT', 'RETURN', 'NEWECL', 'ENCEXIT') or seen > limit: break
        seen += 1
    return ' | '.join(out)


if __name__ == '__main__':
    asmdir, mapjson, outmd = sys.argv[1:4]
    maps = {m['id']: m for m in json.load(open(mapjson))['maps']}
    # module -> map id (LOADFILES first operand, decimal)
    md, result = ['# Genesis map events (plane 2 low 6 bits -> ECL handler)\n'], {}
    for f in sorted(os.listdir(asmdir)):
        mm = re.match(r'ecl_([0-9A-F]{2})\.asm', f)
        if not mm: continue
        mod = int(mm.group(1), 16)
        ins, labels, order = parse_module(os.path.join(asmdir, f))
        lf = [int(re.findall(r'\d+', a)[0]) for o, a in ins.values() if o == 'LOADFILES' and int(re.findall(r'\d+', a)[0]) != 127]
        d = dispatch(ins, order)
        if not lf or not d: continue
        mid = lf[0]
        if mid not in maps: continue
        var, n, targets, kind = d
        m = maps[mid]
        cells = collections.defaultdict(list)
        for i in range(256):
            if m['special'][i] & 0x3F:
                cells[m['special'][i] & 0x3F].append((i % 16, i // 16, m['exists'][i]))
        md.append(f'\n## Module {mod:02X} / map {mid:02X} {m["name"]}\n`{kind} {var}`, {n} targets. Code 0 (and any code beyond {n}) falls through the table.\n')
        md.append('| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |\n|---|---|---|---|')
        ev = {}
        for code in sorted(cells):
            h = targets[code] if code < n else '(falls through)'
            s = summarize(ins, labels, order, h) if h in labels else ''
            pos = ' '.join(f'({x},{y})' + ('' if ex else 'o') for x, y, ex in cells[code][:8]) + (' ...' if len(cells[code]) > 8 else '')
            md.append(f'| {code:02X} | {pos} | {h} | {s} |')
            ev[code] = dict(cells=[(x, y) for x, y, _ in cells[code]], handler=h, summary=s)
        unref = sorted({targets[i] for i in range(1, n)} - {targets[c] for c in cells if c < n})
        md.append(f'\nHandlers no cell can reach: {", ".join(unref) or "none"}')
        result[f'{mid:02X}'] = ev
    open(outmd, 'w').write('\n'.join(md) + '\n')
    if len(sys.argv) > 4: json.dump(result, open(sys.argv[4], 'w'), indent=1)
    print(len(result), 'maps cross-referenced')
