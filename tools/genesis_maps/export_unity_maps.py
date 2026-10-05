"""Unity-friendly JSON for the Genesis maps (JsonUtility cannot read dictionaries or tuples).

usage: python export_unity_maps.py genesis_maps/genesis_maps.json genesis_maps/map_events.json OUTDIR
writes OUTDIR/maps.json and OUTDIR/map_events.json (copy both into Assets/Resources/BuckRogers/)."""
import sys, json, os

maps = json.load(open(sys.argv[1]))
ev = json.load(open(sys.argv[2]))
out = sys.argv[3]
os.makedirs(out, exist_ok=True)
keep = ('id', 'name', 'width', 'height', 'north', 'east', 'south', 'west', 'exists', 'special',
        'lock_north', 'lock_east', 'lock_south', 'lock_west')
json.dump(dict(source=maps['source'], maps=[{k: m[k] for k in keep} for m in maps['maps']]),
          open(os.path.join(out, 'maps.json'), 'w'), separators=(',', ':'))

cells = lambda cs: [dict(x=x, y=y) for x, y in cs]
mods = []
for mod, r in sorted(ev.items()):
    mods.append(dict(
        module=int(mod, 16), map=r['map'],
        search=[dict(code=int(c), test='', facing=-1, handler=v['handler'], summary=v['summary'], texts=v['texts'], cells=cells(v['cells']))
                for c, v in sorted(r['search'].items(), key=lambda kv: int(kv[0]))],
        step=[dict(code=-1, test=s['test'], facing=-1 if s['facing'] is None else s['facing'], handler=s['handler'],
                   summary=s['summary'], texts=s['texts'], cells=cells(s['cells'])) for s in r['step']]))
json.dump(dict(note='search: plane-2 byte & 0x3F indexes the ONGOTO table; step: run-entry tests on [9AF9] (full plane-2 byte) or [9E6F] (code) with facing 0 N 1 E 2 S 3 W (-1 = any)',
               modules=mods), open(os.path.join(out, 'map_events.json'), 'w'), separators=(',', ':'))
print(len(maps['maps']), 'maps,', len(mods), 'modules ->', out)
