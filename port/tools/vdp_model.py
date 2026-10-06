"""A small model of the Genesis VDP for the emulator (emu68k.Machine): captures what ROM routines write to video memory (VRAM), colour memory (CRAM), vertical scroll memory and
runs 68000->VRAM DMA. Enough to run the ROM's own drawing code and look at the result (see export_terrain.py).   m = Machine(rom); v = VDP(m); m.call(..., a4=0xC00004, a5=0xC00000)"""
import struct
from unicorn import UC_HOOK_MEM_WRITE

class VDP:
    def __init__(s, m):
        s.m = m; s.vram = bytearray(0x10000); s.cram = [0] * 64; s.vsram = [0] * 40
        s.pend = None; s.code = 0; s.addr = 0; s.inc = 2; s.regs = [0] * 24
        m.map_io(); m.uc.hook_add(UC_HOOK_MEM_WRITE, s._write, begin=0xC00000, end=0xC0000F)

    def _write(s, uc, access, address, size, value, user):
        port = address & 0x1F
        if port < 4: s._data(value, size)
        elif port < 8:
            if size == 4: s._cmd((value >> 16) & 0xFFFF); s._cmd(value & 0xFFFF)
            else: s._cmd(value & 0xFFFF)

    def _cmd(s, w):
        if s.pend is not None:
            hi = s.pend; s.pend = None
            s.code = ((hi >> 14) & 3) | ((w >> 2) & 0x3C); s.addr = (hi & 0x3FFF) | ((w & 3) << 14)
            if s.code & 0x20 and s.regs[1] & 0x10: s._dma()
        elif (w >> 13) == 4:
            r = (w >> 8) & 0x1F; s.regs[r] = w & 0xFF
            if r == 15: s.inc = w & 0xFF
        else: s.pend = w

    def _put(s, val):
        c = s.code & 0xF
        if c == 1: s.vram[s.addr & 0xFFFF] = val >> 8; s.vram[(s.addr + 1) & 0xFFFF] = val & 0xFF
        elif c == 3: s.cram[(s.addr >> 1) & 63] = val
        elif c == 5: s.vsram[(s.addr >> 1) % 40] = val
        s.addr = (s.addr + s.inc) & 0xFFFF

    def _data(s, v, size):
        if size == 4: s._put((v >> 16) & 0xFFFF); s._put(v & 0xFFFF)
        else: s._put(v & 0xFFFF)

    def _dma(s):
        r = s.regs; n = ((r[20] << 8) | r[19]) or 0x10000; src = (((r[23] & 0x7F) << 16) | (r[22] << 8) | r[21]) << 1
        if (r[23] & 0xC0) == 0x80: return
        data = bytes(s.m.uc.mem_read(src if src < 0x100000 else 0xFF0000 | (src & 0xFFFF), 2 * n))
        for i in range(n): s._put((data[2 * i] << 8) | data[2 * i + 1])
