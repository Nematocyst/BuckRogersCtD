"""
Encoder for the Buck Rogers (Genesis) LZW variant. It is the exact inverse of
decoder.py. Format (derived from the decoder + the DataCrystal ROM map):

  * Standard LZW with literals 0..255, 0x100 and 0x101 = stop codes, and new
    dictionary entries numbered from 0x102 upward. No clear/reset code.
  * Codes are packed MSB-first. A code is written as the low `size` bits,
    followed by ONE extra high bit only if (low <= range_min). The decoder
    reads that extra bit under exactly the same condition, so the encoder must
    emit it whenever the condition holds (even if the bit is 0).
  * State (identical to the decoder): size=8, range_min=2, range_max=0x1FF,
    next_entry=0x102. After every code except the first an entry is added:
    range_min += 1, next_entry += 1, and when next_entry == range_max the
    width grows (size += 1, range_max = 2**(size+1)-1, range_min = 0xFFFF).
"""

STOP = 0x100
END = 0x101


class BitWriter:
    def __init__(self):
        self.bits = []

    def write(self, value, n):
        for i in range(n - 1, -1, -1):
            self.bits.append((value >> i) & 1)

    def to_bytes(self):
        b = self.bits + [0] * ((-len(self.bits)) % 8)
        out = bytearray()
        for i in range(0, len(b), 8):
            v = 0
            for bit in b[i:i + 8]:
                v = (v << 1) | bit
            out.append(v)
        return bytes(out)


def _lzw(w, data, end_code, max_entries=None):
    """Write one LZW segment (fresh dictionary) followed by end_code into BitWriter w."""
    size, range_min, range_max, next_entry = 8, 2, 0x1FF, 0x102
    first = True

    def emit(code):
        nonlocal size, range_min, range_max, next_entry, first
        low = code & ((1 << size) - 1)
        w.write(low, size)
        if low <= range_min:
            w.write((code >> size) & 1, 1)
        else:
            assert code < (1 << size), f"code {code:#x} not representable at size {size}"
        if first:
            first = False
        else:
            range_min = (range_min + 1) & 0xFFFF
            next_entry += 1
            if next_entry == range_max:
                size += 1
                range_max = 2 ** (size + 1) - 1
                range_min = 0xFFFF

    table = {}
    next_code = 0x102
    if not data:
        emit(end_code)
        return
    cur = data[0]
    for c in data[1:]:
        key = (cur, c)
        if key in table:
            cur = table[key]
        else:
            emit(cur)
            table[key] = next_code
            next_code += 1
            if max_entries is not None and next_code - 0x102 > max_entries:
                raise ValueError("dictionary grew beyond max_entries")
            cur = c
    emit(cur)
    emit(end_code)


def encode(data, max_entries=None):
    """Single segment ending in 0x100 (kept for older scripts)."""
    w = BitWriter()
    _lzw(w, data, STOP, max_entries)
    return w.to_bytes()


def encode_stream(segments):
    """Segments separated by 0x100 (clear), whole stream ended by 0x101. Returns a BitWriter
    (use .bits for the exact bit sequence, .to_bytes() for byte-padded data)."""
    w = BitWriter()
    for i, seg in enumerate(segments):
        _lzw(w, seg, END if i == len(segments) - 1 else STOP)
    return w
