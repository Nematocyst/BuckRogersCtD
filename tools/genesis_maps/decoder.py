"""Minimal BitReader (MSB-first) for decoder2.py; the old decoder.py's LZW logic is NOT included."""
class BitReader:
    def __init__(self, data, start_offset=0):
        self.data = data; self.bit_pos = start_offset * 8
    def read_bits(self, n):
        v = 0
        for _ in range(n):
            byte = self.data[self.bit_pos >> 3]
            v = (v << 1) | ((byte >> (7 - (self.bit_pos & 7))) & 1)
            self.bit_pos += 1
        return v
