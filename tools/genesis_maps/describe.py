"""Readable one-paragraph descriptions of ECL event handlers (Genesis Buck Rogers).

Walks the handler's control flow in scripts.json (from export_unity_data.py) and reports what it does: text shown,
fights, treasure, module jumps, shops, XP, teleports, once-only flags. Control flow follows the ECL conventions:
  IFxx           the NEXT instruction runs only if the condition holds (otherwise it is skipped)
  GOTO/EXIT/RETURN/NEWECL/ENCEXIT end a path unless they directly follow an IFxx
  ONGOTO         every table target is explored; GOSUB/ONGOSUB subroutines are explored too
"""
import json, os

IFS = {'IFEQ', 'IFNE', 'IFLT', 'IFGT', 'IFLE', 'IFGE'}
ENDS = {'EXIT', 'RETURN', 'NEWECL', 'ENCEXIT'}
POS = {0x9AF7: 'x', 0x9AF6: 'y', 0x9AFA: 'facing'}


class Describer:
    def __init__(self, data_dir):
        sc = json.load(open(os.path.join(data_dir, 'scripts.json')))
        self.mods = {}
        for m in sc['modules']:
            ins = {i['addr']: i for i in m['instructions']}
            self.mods[m['id']] = (ins, sorted(ins))
        self.mon = {m['id']: m['name'].title() for m in json.load(open(os.path.join(data_dir, 'monsters.json')))['monsters']}
        self.items = {i['id']: i['name'] for i in json.load(open(os.path.join(data_dir, 'items.json')))['items']}

    # --- control flow -------------------------------------------------------------------------------------------
    def walk(self, mod, start, cap=500):
        ins, order = self.mods[mod]
        nxt = {a: order[i + 1] if i + 1 < len(order) else None for i, a in enumerate(order)}
        seen, out, stack = set(), [], [start]
        while stack and len(out) < cap:
            a = stack.pop()
            prev_if = False
            while a is not None and a in ins and a not in seen and len(out) < cap:
                seen.add(a); i = ins[a]; n = i['name']; out.append(i)
                cond = prev_if; prev_if = n in IFS
                if n == 'GOTO':
                    stack.append(i['operands'][0]['value'])
                    if not cond: break
                elif n in ('ONGOTO', 'ONGOSUB'):
                    stack += [o['value'] for o in i['operands'] if o['kind'] == 'label']
                    if n == 'ONGOTO': break
                elif n == 'GOSUB':
                    stack.append(i['operands'][0]['value'])
                elif n in ENDS and not cond:
                    break
                a = nxt[a]
        return sorted(out, key=lambda i: i['addr'])

    # --- description --------------------------------------------------------------------------------------------
    def describe(self, mod, start):
        ops = self.walk(mod, start)
        if not ops: return dict(text='', texts=[], parts=[])
        parts, texts, flags_set, pos = [], [], [], {}
        mon, fights, treas = [[]], False, []
        once = None
        names = [i['name'] for i in ops[:6]]
        # once-only guard: AND [flag], mask, [tmp] ; IFNE ; EXIT  at the top of the handler
        for k, i in enumerate(ops[:5]):
            if i['name'] == 'AND' and len(i['operands']) == 3 and i['operands'][0]['kind'] == 'memory' and i['operands'][1]['kind'] == 'byte' \
                    and k + 2 < len(ops) and ops[k + 1]['name'] == 'IFNE' and ops[k + 2]['name'] == 'EXIT':
                once = (i['operands'][0]['value'], i['operands'][1]['value'])
        for i in ops:
            n, o = i['name'], i['operands']
            if n in ('PRINT', 'PRINTCLEAR') and o and o[0]['kind'] == 'text' and o[0]['text'] and o[0]['text'] not in texts:
                texts.append(o[0]['text'])
            elif n in ('HMENU', 'WHMENU'):
                opts = [x['text'] for x in o if x['kind'] == 'text'][-int(o[1]['value']) if len(o) > 1 and o[1]['kind'] == 'byte' else 0:]
                if opts: parts.append('menu: ' + ' / '.join(t.title() for t in opts))
            elif n in ('SETUPMONSTERS', 'CLEARMONSTERS'):
                if mon[-1]: mon.append([])
            elif n == 'LOADMONSTER':
                nm = self.mon.get(o[0]['value'], f'monster {o[0]["value"]}')
                cnt = o[1]['value'] if o[1]['kind'] == 'byte' else None
                mon[-1].append(f'{cnt}x {nm}' if cnt and cnt > 1 else nm)
            elif n == 'COMBAT': fights = True
            elif n == 'SPACECOMBAT': parts.append('space combat')
            elif n == 'DUEL': parts.append('duel')
            elif n == 'TREASURE':
                cr = o[0]['value'] if o and o[0]['kind'] in ('word', 'byte') else None
                its = [self.items.get(x['value'], f'item {x["value"]}') for x in o[2:] if x['kind'] == 'byte']
                treas.append((f'{cr} cr' if cr else 'credits') + (', ' + ', '.join(its) if its else ''))
            elif n == 'NEWECL': parts.append(f'-> module {o[0]["value"]:02X}' if o[0]['kind'] == 'byte' else '-> module (variable)')
            elif n == 'STORE': parts.append(f'store {o[0]["value"]}')
            elif n == 'ADDEP': parts.append(f'+{o[1]["value"]} XP' if o[1]['kind'] in ('byte', 'word') else 'XP award')
            elif n == 'PROGRAM': parts.append(f'program {o[0]["value"]}')
            elif n == 'LOADCHARACTER': parts.append('NPC joins/appears')
            elif n == 'FINDITEM': parts.append('item search')
            elif n == 'SKILL': parts.append('skill check')
            elif n == 'DAMAGE': parts.append('damages party')
            elif n == 'STEPBACK': parts.append('pushes party back')
            elif n == 'EXPLOSION': parts.append('explosion effect')
            elif n == 'ROB': parts.append('robbery')
            elif n == 'DESTROY': parts.append('destroys item')
            elif n == 'CHECKPARTY': parts.append('party check')
            elif n == 'STAIRCASE': parts.append('staircase')
            elif n == 'UNLOCKDOOR': parts.append('unlocks door')
            elif n == 'GETYN': parts.append('yes/no prompt')
            elif n == 'RANDOM': parts.append('random roll')
            elif n == 'SAVE' and len(o) == 2 and o[0]['kind'] == 'byte' and o[1]['kind'] == 'memory':
                if o[1]['value'] in POS: pos[POS[o[1]['value']]] = o[0]['value']
                elif o[0]['value'] not in (0,) : flags_set.append(f'[{o[1]["value"]:04X}]={o[0]["value"]}')
            elif n == 'OR' and len(o) == 3 and o[0]['kind'] == 'memory' and o[1]['kind'] == 'byte' and o[2]['kind'] == 'memory' and o[0]['value'] == o[2]['value']:
                flags_set.append(f'flag {o[0]["value"]:04X}|={o[1]["value"]}')
        groups = [' + '.join(g) for g in mon if g]
        if groups: parts.insert(0, ('fight: ' if fights else 'monsters: ') + ' | or | '.join(groups))
        elif fights: parts.insert(0, 'fight')
        if treas: parts.append('treasure: ' + '; '.join(treas))
        if 'x' in pos or 'y' in pos:
            parts.append('moves party to (%s,%s)%s' % (pos.get('x', '?'), pos.get('y', '?'), ' facing ' + 'NESW'[pos['facing'] & 3] if 'facing' in pos else ''))
        if flags_set: parts.append('sets ' + ', '.join(dict.fromkeys(flags_set)))
        head = [f'once only (flag {once[0]:04X} bit mask {once[1]})'] if once else []
        t = ''
        if texts:
            t = '"%s"' % texts[0][:110].strip() + ('...' if len(texts[0]) > 110 else '') + (f' (+{len(texts) - 1} more texts)' if len(texts) > 1 else '')
        desc = '; '.join(head + ([t] if t else []) + parts)
        if not desc and [i['name'] for i in ops if i['name'] != 'EXIT'] == ['ENCEXIT']:
            desc = 'no event here (just the random-encounter check)'
        elif not desc and all(i['name'] in IFS | {'EXIT', 'RETURN', 'GOTO'} for i in ops):
            desc = 'no event here (cell is a plain marker)'
        if not desc:
            # pure logic handler: describe by its first operations
            desc = 'logic only (' + ', '.join(dict.fromkeys(i['name'] for i in ops if i['name'] not in IFS | {'EXIT', 'GOTO', 'COMPARE', 'COMPAREAND', 'AND'})) + ')' \
                if any(i['name'] not in IFS | {'EXIT', 'GOTO', 'COMPARE', 'COMPAREAND', 'AND'} for i in ops) else 'no effect (returns)'
        return dict(text=desc, texts=texts, parts=parts, once=once, walked=len(ops))
