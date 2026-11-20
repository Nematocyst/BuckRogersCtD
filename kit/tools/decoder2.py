"""Corrected decoder for the Buck Rogers (Genesis) LZW variant.
Bit packing as in decoder.py, but standard LZW semantics: after every code `prev` becomes that
code (including the code == next-entry case). 0x100 = CLEAR (dictionary restarts, stream may
continue), 0x101 = END. Use decode_stream() for whole files, decode2() for a single segment."""
from decoder import BitReader


def _decode_segment(rom_bytes, bit, max_out=1 << 20):
    src = BitReader(rom_bytes, 0)
    src.bit_pos = bit
    size, rmin, rmax, E = 8, 2, 0x1FF, 0x102
    dic = {}
    out = bytearray()
    prev = None
    def S(c): return bytes([c]) if c < 256 else dic[c]
    while True:
        code = src.read_bits(size)
        if code <= rmin:
            code |= src.read_bits(1) << size
        if code in (0x100, 0x101):
            return bytes(out), src.bit_pos, code
        if prev is None:
            s = S(code)
        else:
            if code < 256 or code in dic:
                s = S(code)
            elif code == E:
                s = S(prev) + S(prev)[:1]
            else:
                raise ValueError(f"bad code {code:#x} (E={E:#x})")
            dic[E] = S(prev) + s[:1]
            rmin = (rmin + 1) & 0xFFFF
            E += 1
            if E == rmax:
                size += 1
                rmax = 2 ** (size + 1) - 1
                rmin = 0xFFFF
        out += s
        prev = code
        if len(out) > max_out:
            return bytes(out), src.bit_pos, None


def decode2(rom_bytes, start_offset=None, max_out=1 << 20, start_bit=None):
    """Decode ONE segment (up to the next 0x100/0x101). Returns (bytes, end_bit_position)."""
    bit = start_bit if start_bit is not None else (start_offset or 0) * 8
    out, endbit, _ = _decode_segment(rom_bytes, bit, max_out)
    return out, endbit


def decode_stream(rom_bytes, start_offset):
    """Decode a whole file: segments separated by 0x100, ended by 0x101.
    Returns (list_of_segments, end_bit_position)."""
    bit = start_offset * 8
    segs = []
    while True:
        out, bit, last = _decode_segment(rom_bytes, bit)
        segs.append(out)
        if last != 0x100:
            return segs, bit
