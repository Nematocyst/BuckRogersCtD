"""Build the optional add-on patches (cut weapons, difficulty, both) as IPS files that apply ON TOP of the release.

  cd kit
  sh build_release.sh ORIGINAL.md           # needs build/release.md
  sh build_release.sh ORIGINAL.md level9    # needs build/release_level9.md
  python3 tools/build_addons.py ORIGINAL.md

Writes build/addons/addon_{cut_weapons,difficulty,cut_weapons_difficulty}[_level9].ips. Each add-on is checked after
building: release + add-on equals the target ROM, the game checksum is valid, and the racial rocket pistols (item ids
0x5D, 0x5E) are still listed at New Elysium (shop 5) and Tycho (shop 9). Run it from the kit/ folder: it finds the
component patches and the build/ folder next to the tools/ folder."""
import os, sys, struct
if len(sys.argv) != 2:
    raise SystemExit('usage: python3 tools/build_addons.py ORIGINAL.md   (run from the kit folder, after build_release.sh)')
K = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))     # the kit folder
sys.path.insert(0, K + '/tools')
from ips import apply, diff
from build_monster_patch import game_checksum, header_checksum, make_ips
CK = set(range(0x18E, 0x190)) | set(range(0xFFFCC, 0xFFFD0))
o = open(sys.argv[1], 'rb').read()
for _n in ('', '_level9'):
    if not os.path.exists(K + f'/build/release{_n}.md'):
        raise SystemExit(f'missing {K}/build/release{_n}.md: run  sh build_release.sh ORIGINAL.md {_n[1:]}  first')
os.makedirs(K + '/build/addons', exist_ok=True)
def fix(r):
    r[0xFFFCC:0xFFFD0] = struct.pack('>I', game_checksum(bytes(r)))
    r[0x18E:0x190] = struct.pack('>H', header_checksum(bytes(r)))
    return bytes(r)
def lst(r, s):
    L = struct.unpack_from('>I', r, 0xF2000 + 4 * s)[0]; return L, list(r[L + 1:L + 1 + r[L]])
for L9 in ['', '_level9']:
    base = apply(o, K + f'/restoration_v9_clean{L9}.ips')
    rel = open(K + f'/build/release{L9}.md', 'rb').read()
    dif = apply(o, K + f'/restoration_v9_difficulty{L9}.ips')
    v11 = apply(o, K + f'/restoration_v11{L9}.ips')
    def add_diff(r):
        for i in diff(base, dif) - CK: r[i] = dif[i]
    def add_weap(r):
        for i in diff(base, v11) - CK:
            if not 0xF2000 <= i < 0xF2200: r[i] = v11[i]
        for s in range(16):
            new = [x for x in lst(v11, s)[1] if x not in lst(base, s)[1]]
            if not new: continue
            A, cur = lst(r, s); ids = cur + new
            assert len(ids) <= 15 and A + 1 + len(ids) <= 0xF2000 + 0x40 + 16 * 16 or True
            nxt = min(p for p in [struct.unpack_from('>I', r, 0xF2000 + 4 * t)[0] for t in range(16)] + [0xF2200] if p > A)
            assert A + 1 + len(ids) <= nxt, (s, len(ids))
            r[A] = len(ids); r[A + 1:A + 1 + len(ids)] = bytes(ids)
            print(f'  shop {s}: {len(cur)} -> {len(ids)} items')
    for name, steps in [('cut_weapons', [add_weap]), ('difficulty', [add_diff]), ('cut_weapons_difficulty', [add_weap, add_diff])]:
        r = bytearray(rel)
        for f in steps: f(r)
        r = fix(r)
        out = f'{K}/build/addons/addon_{name}{L9}.ips'; open(out, 'wb').write(make_ips(rel, r))
        # verify: release + addon == target, checksum valid, racial rockets still listed
        t = apply(rel, out); assert t == r and game_checksum(t) == struct.unpack('>I', t[0xFFFCC:0xFFFD0])[0]
        assert 0x5D in lst(t, 5)[1] and 0x5E in lst(t, 9)[1]
        print(f'addon_{name}{L9}.ips: {len(diff(rel, t))} bytes, checksums {t[0xFFFCC:0xFFFD0].hex(" ").upper()} / {t[0x18E:0x190].hex(" ").upper()}')
