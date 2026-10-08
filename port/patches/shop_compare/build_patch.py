"""Assemble shop_compare.s and print/emit the patch (usage: build_patch.py ROM [out.ips])."""
import subprocess, sys, struct, os
here = os.path.dirname(os.path.abspath(__file__))
BLOB_AT, HOOK_AT = 0xF5000, 0x16ABE
def assemble():
    subprocess.check_call(['m68k-linux-gnu-as', '-m68000', '--register-prefix-optional', '-o', here + '/shop_compare.o', here + '/shop_compare.s'])
    subprocess.check_call(['m68k-linux-gnu-objcopy', '-O', 'binary', '-j', '.text', here + '/shop_compare.o', here + '/shop_compare.bin'])
    return open(here + '/shop_compare.bin', 'rb').read()
HOOK = bytes([0x4E, 0xF9]) + struct.pack('>I', BLOB_AT)         # jmp 0xF5000 over  bsr 0x11C4C ; bra.b 0x16AC8
def patches():
    return [(HOOK_AT, HOOK), (BLOB_AT, assemble())]
def apply(rom):
    r = bytearray(rom)
    expect = bytes.fromhex('6100b18c6004')
    assert r[HOOK_AT:HOOK_AT + 6] == expect, 'hook site differs from the original ROM (%s)' % r[HOOK_AT:HOOK_AT+6].hex()
    for off, data in patches(): r[off:off + len(data)] = data
    return bytes(r)
def ips(path):
    out = bytearray(b'PATCH')
    for off, data in patches(): out += struct.pack('>I', off)[1:] + struct.pack('>H', len(data)) + data
    out += b'EOF'; open(path, 'wb').write(out)
if __name__ == '__main__':
    for off, data in patches(): print('%06X: %s' % (off, data.hex(' ')))
    if len(sys.argv) > 1: ips(sys.argv[1])
