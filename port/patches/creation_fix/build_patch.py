"""Creation fix: picking Human or Tinker on the creation screen resets the natural-attack bytes the desert runner choice set.
usage: build_patch.py ROM [out.ips]      (ROM = original USA/Europe ROM; the check bytes below are verified first)

Original bug: the human handler (0x6A2) has `clr.b $0C(a2)` (name byte 12) where the tinker handler has `clr.b $2C(a2)` (claw damage bonus); neither resets the
dice sides +0x2A that the desert runner handler set to 3. After desert runner -> human / tinker the character kept claws +1 and 1d3 fists.
Fix: both handlers call a small routine in free ROM (0xF6000) that clears +0x2C and sets +0x2A = 2 (the values of a fresh record, ROM 0x5A0), then loads d0 as before.
The creation finish handler (0x810) still sets +0x2A = 3 for warriors and desert runners, as in the original."""
import sys, struct
HUMAN_AT, TINKER_AT, BLOB_AT = 0x6A8, 0x6DC, 0xF6000
HUMAN_OLD = bytes.fromhex('422a000c7001')        # clr.b $0C(a2) ; moveq #1,d0
TINKER_OLD = bytes.fromhex('422a002c7003')       # clr.b $2C(a2) ; moveq #3,d0
def routine(race):
    return bytes.fromhex('422a002c' '157c0002002a') + bytes([0x70, race, 0x4E, 0x75])   # clr.b $2C(a2) ; move.b #2,$2A(a2) ; moveq #race,d0 ; rts
BLOB = routine(1) + routine(3)                    # human at 0xF6000, tinker at 0xF600E
def jsr(addr): return bytes([0x4E, 0xB9]) + struct.pack('>I', addr)
def patches():
    return [(HUMAN_AT, jsr(BLOB_AT)), (TINKER_AT, jsr(BLOB_AT + 14)), (BLOB_AT, BLOB)]
def apply(rom):
    r = bytearray(rom)
    assert r[HUMAN_AT:HUMAN_AT + 6] == HUMAN_OLD, 'human hook site differs from the original ROM'
    assert r[TINKER_AT:TINKER_AT + 6] == TINKER_OLD, 'tinker hook site differs from the original ROM'
    assert set(r[BLOB_AT:BLOB_AT + len(BLOB)]) == {0xFF}, 'target area is not free'
    for off, data in patches(): r[off:off + len(data)] = data
    return bytes(r)
def ips(path):
    out = bytearray(b'PATCH')
    for off, data in patches(): out += struct.pack('>I', off)[1:] + struct.pack('>H', len(data)) + data
    out += b'EOF'; open(path, 'wb').write(out)
if __name__ == '__main__':
    for off, data in patches(): print('%06X: %s' % (off, data.hex(' ')))
    if len(sys.argv) > 2:
        apply(open(sys.argv[1], 'rb').read()); ips(sys.argv[2])
