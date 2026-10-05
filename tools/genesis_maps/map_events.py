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


def entries(path):
    """entry labels from the listing header: '; entries: run=L6BAE, search=L6C13, ...'"""
    for line in open(path):
        if line.startswith('; entries:'):
            return {k: v for k, v in re.findall(r'(\w+)=(L[0-9A-F]{4})', line)}
    return {}


def dispatch(ins, order, start_addr=0, lookahead=30):
    """first AND [9AF9],63,[VAR] at/after start_addr followed (within `lookahead` instructions) by an ONGOTO/ONGOSUB
    on VAR; return (var, n, [labels], opname)"""
    for i, a in enumerate(order):
        if a < start_addr: continue
        op, args = ins[a]
        if op == 'AND' and '[9AF9]' in args and '63' in args:
            var = args.split(',')[-1].strip()
            for b in order[i + 1:i + 1 + lookahead]:
                o2, a2 = ins[b]
                if o2 in ('ONGOTO', 'ONGOSUB') and a2.split(',')[0].strip() == var:
                    f = [x.strip() for x in a2.split(',')]
                    return var, int(f[1]), f[2:2 + int(f[1])], o2
    return None


def step_events(ins, order):
    """'run' (step-onto-cell) tests: COMPARE/COMPAREAND [9E6F|9AF9], K [, [9AFA], F] + IFEQ/IFNE + GOTO label.
    Returns [(value, facing or None, 'EQ'|'NE', label)]"""
    out = []
    for i, a in enumerate(order[:-2]):
        op, args = ins[a]
        if op in ('COMPARE', 'COMPAREAND'):
            f = [x.strip() for x in re.split(r',\s*(?![^\[]*\])', args.split(';')[0])]
            if f and f[0] in ('[9AF9]', '[9E6F]') and re.fullmatch(r'\d+', f[1] if len(f) > 1 else ''):
                nxt, nxt2 = ins[order[i + 1]], ins[order[i + 2]]
                if nxt[0] in ('IFEQ', 'IFNE') and nxt2[0] == 'GOTO':
                    fac = int(f[3]) if op == 'COMPAREAND' and len(f) > 3 and f[2] == '[9AFA]' and re.fullmatch(r'\d+', f[3]) else None
                    out.append((f[0], int(f[1]), fac, nxt[0][2:], nxt2[1].strip()))
    return out


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
    md, result = ['# Genesis map events (plane 2 -> ECL handlers)\n',
                  'Search events: plane-2 byte & 0x3F indexes (0-based) the ONGOTO table at the start of the module\'s search entry. '
                  'Step events: the module\'s run entry compares the plane-2 byte ([9AF9], full byte) or the code ([9E6F]) '
                  'and the facing ([9AFA]: 0 N, 1 E, 2 S, 3 W) and jumps to a handler. A few codes are tested with a raw byte '
                  '(e.g. module 11: [9AF9]==139 is code 0x0B with bit 7 set). Maps 51 and 53 share map 0x51: each module '
                  'only handles its own subset of the codes, so some codes appear unhandled in one of the two.\n'], {}
    for f in sorted(os.listdir(asmdir)):
        mm = re.match(r'ecl_([0-9A-F]{2})\.asm', f)
        if not mm: continue
        mod = int(mm.group(1), 16)
        path = os.path.join(asmdir, f)
        ins, labels, order = parse_module(path)
        lf = [int(re.findall(r'\d+', a)[0]) for o, a in ins.values() if o == 'LOADFILES' and int(re.findall(r'\d+', a)[0]) != 127]
        if not lf or lf[0] not in maps: continue
        mid = lf[0]; m = maps[mid]
        ent = entries(path)
        sa = labels.get(ent.get('search'), 0)
        d = dispatch(ins, order, sa)
        cells = collections.defaultdict(list)
        for i in range(256):
            if m['special'][i] & 0x3F:
                cells[m['special'][i] & 0x3F].append((i % 16, i // 16, m['exists'][i]))
        pos = lambda cs: ' '.join(f'({x},{y})' + ('' if ex else 'o') for x, y, ex in cs[:8]) + (' ...' if len(cs) > 8 else '')
        md.append(f'\n## Module {mod:02X} / map {mid:02X} {m["name"]}\n')
        rec = dict(map=mid, search={}, step=[])
        if d:
            var, n, targets, kind = d
            md.append(f'Search: `{kind} {var}`, {n} targets, code 0 and codes >= {n} fall through.\n')
            md.append('| code | cells (x,y); `o` = outside area (bit 7 clear) | handler | summary |\n|---|---|---|---|')
            for code in sorted(cells):
                h = targets[code] if code < n else '(falls through)'
                sm = summarize(ins, labels, order, h) if h in labels else ''
                md.append(f'| {code:02X} | {pos(cells[code])} | {h} | {sm} |')
                rec['search'][code] = dict(cells=[(x, y) for x, y, _ in cells[code]], handler=h, summary=sm)
            unref = sorted({targets[i] for i in range(1, n)} - {targets[c] for c in cells if c < n})
            md.append(f'\nHandlers no cell can reach: {", ".join(unref) or "none"}')
            beyond = sorted(c for c in cells if c >= n)
            if beyond: md.append(f'\nCodes beyond the table (fall through): {" ".join(f"{c:02X}" for c in beyond)}')
        else:
            md.append('No search dispatch (no `AND [9AF9],63` + `ONGOTO`): this map has no cell-event table. '
                      + ('Scripted cutscene walk (STEPFORWARD / SAVE facing), the map is only the backdrop.' if mod == 3 else ''))
        st = step_events(ins, order)
        if st:
            md.append('\nStep events (run entry and elsewhere):\n\n| tests | facing | handler | cells on the map | summary |\n|---|---|---|---|---|')
            for var, val, fac, cond, lab in st:
                cs = [(i % 16, i // 16, m['exists'][i]) for i in range(256)
                      if (m['special'][i] == val if var == '[9AF9]' else (m['special'][i] & 0x3F) == val)]
                sm = summarize(ins, labels, order, lab) if lab in labels else ''
                md.append(f'| {var}{"==" if cond == "EQ" else "!="}{val} | {"" if fac is None else "NESW"[fac]} | {lab} | {pos(cs) or "(none)"} | {sm} |')
                rec['step'].append(dict(test=f'{var}{cond}{val}', facing=fac, handler=lab, cells=[(x, y) for x, y, _ in cs], summary=sm))
        result[f'{mod:02X}'] = rec
    open(outmd, 'w').write('\n'.join(md) + '\n')
    if len(sys.argv) > 4: json.dump(result, open(sys.argv[4], 'w'), indent=1)
    print(len(result), 'modules with maps cross-referenced')
