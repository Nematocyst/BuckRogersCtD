"""Runs the creation handlers in the 68000 emulator on the original and the patched ROM.  usage: test_creation_fix.py ROM"""
import sys, os
here = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, here); sys.path.insert(0, os.path.join(here, '..', '..', 'tools'))
from emu68k import Machine
import build_patch
orig = open(sys.argv[1], 'rb').read(); fixed = build_patch.apply(orig)
ENT = {'dr': 0x6BC, 'human': 0x6A2, 'tinker': 0x6D6}
A2, A3 = 0xFFFFC000, 0xFFFFC200

def run(rom, seq):
    m = Machine(rom); m.run_range(0x12C8, 0x12E4); m.stub_rts(0x13DF0)
    rec = bytearray(214); rec[0x17] = 1; rec[0x18] = 1; rec[0x24] = 8; rec[0x25] = 0x32; rec[0x26:0x2C] = bytes([2, 0, 1, 0, 2, 0]); rec[0x0C] = 0x4D
    rec[0x10:0x15] = bytes([10] * 5); m.write_ram(A2, bytes(rec))
    for s in seq:
        assert m.run_between(ENT[s], {0x762}, a2=A2, a3=A3, a1=0, a6=0) == 0x762
    return m.read_ram(A2, 214)
fails = 0
def check(ok, what):
    global fails
    if not ok: fails += 1; print('FAIL:', what)
for seq in (('dr', 'human'), ('dr', 'tinker'), ('dr', 'human', 'tinker'), ('human',), ('tinker',), ('human', 'dr', 'human'), ('dr', 'tinker', 'human')):
    a, b = run(orig, seq), run(fixed, seq)
    last = seq[-1]
    if last in ('human', 'tinker'):
        check(b[0x2A] == 2 and b[0x2C] == 0, f'{seq}: +0x2A={b[0x2A]} +0x2C={b[0x2C]}')
    else:
        check(b[0x2A] == 3 and b[0x2C] == 1, f'{seq}: desert runner keeps claws and 1d3')
    # everything else must match the original, except +0x0C (human no longer clears it) and the two bytes above
    diff = [i for i in range(214) if a[i] != b[i]]
    allowed = {0x0C, 0x2A, 0x2C}
    check(set(diff) <= allowed, f'{seq}: unexpected differences at {[hex(i) for i in diff if i not in allowed]}')
    print(f'{" -> ".join(seq):28} original +2A={a[0x2A]} +2C={a[0x2C]} +0C={a[0x0C]:02x} | fixed +2A={b[0x2A]} +2C={b[0x2C]} +0C={b[0x0C]:02x} | ability bytes same: {a[0x10:0x15] == b[0x10:0x15]}')
print('creation fix:', 'ok' if not fails else f'{fails} failing'); sys.exit(1 if fails else 0)
