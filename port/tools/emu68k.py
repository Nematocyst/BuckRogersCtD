"""Run routines of the Genesis ROM in a 68000 emulator (Unicorn) for differential testing of C# ports.

    m = Machine(rom_bytes)            # ROM at 0, work RAM mapped at 0xFFFF0000..0xFFFFFFFF (abs.w addresses sign-extend there)
    m.call(0x6C94, d0=5)              # JSR-style call, returns when the routine executes its RTS
    m.reg('d0'), m.ram_word(0xD604), m.write_ram(...)
Everything the routine touches that is not mapped raises an error, so a port is only trusted for routines that run
in this closed environment (no VDP / IO)."""
import struct
from unicorn import Uc, UC_ARCH_M68K, UC_MODE_BIG_ENDIAN, UcError
from unicorn.m68k_const import *

RET = 0x00FFF000        # return sentinel address (inside ROM space, unused)
RAM_BASE = 0xFFFF0000


class Machine:
    def __init__(self, rom):
        self.uc = Uc(UC_ARCH_M68K, UC_MODE_BIG_ENDIAN)
        try:
            self.uc.ctl_set_cpu_model(UC_CPU_M68K_M68000)
        except Exception:
            pass
        self.uc.mem_map(0, 0x100000, 5)                       # ROM r-x
        self.uc.mem_write(0, bytes(rom[:0x100000]))
        self.uc.mem_map(0x00FF0000, 0x10000)                  # RAM as seen with 24-bit addresses
        self.uc.mem_map(RAM_BASE, 0x10000)                    # RAM as seen through sign-extended abs.w addresses

    def map_io(self):
        """map the VDP / Z80 / IO register ranges as plain RAM so UI code that pokes them runs (writes are simply kept)"""
        for base, size in ((0xA00000, 0x20000), (0xC00000, 0x10000)):
            self.uc.mem_map(base, size)

    # --- memory (work RAM is mirrored: 0xFFFFxxxx and 0x00FFxxxx are the same bytes) -------------------------------
    def _w(self, a, data):
        self.uc.mem_write(RAM_BASE + (a & 0xFFFF), data); self.uc.mem_write(0x00FF0000 + (a & 0xFFFF), data)

    def write_ram(self, addr, data): self._w(addr, bytes(data))
    def read_ram(self, addr, n): return bytes(self.uc.mem_read(RAM_BASE + (addr & 0xFFFF), n))
    def ram_word(self, addr): return struct.unpack('>H', self.read_ram(addr, 2))[0]
    def ram_byte(self, addr): return self.read_ram(addr, 1)[0]
    def sync_mirror(self):
        self.uc.mem_write(0x00FF0000, bytes(self.uc.mem_read(RAM_BASE, 0x10000)))

    def reg(self, name): return self.uc.reg_read(globals()['UC_M68K_REG_' + name.upper()])
    def set_reg(self, name, v): self.uc.reg_write(globals()['UC_M68K_REG_' + name.upper()], v & 0xFFFFFFFF)

    def call(self, addr, max_insns=200000, **regs):
        """call addr like a subroutine; stops at its RTS. regs: d0..d7, a0..a6"""
        for k, v in regs.items(): self.set_reg(k, v)
        sp = RAM_BASE + 0xF000
        self.set_reg('a7', sp - 4)
        self.uc.mem_write(sp - 4, struct.pack('>I', RET))
        self.uc.emu_start(addr, RET, count=max_insns)

    def stub_rts(self, addr):
        """make the routine at addr a no-op that returns immediately (for graphics / IO helpers); returns the call counter list"""
        from unicorn import UC_HOOK_CODE
        calls = []
        def hook(uc, address, size, user):
            if address == addr:
                calls.append(1)
                sp = uc.reg_read(UC_M68K_REG_A7)
                ret = struct.unpack('>I', bytes(uc.mem_read(sp, 4)))[0]
                uc.reg_write(UC_M68K_REG_A7, sp + 4)
                uc.reg_write(UC_M68K_REG_PC, ret)
        self.uc.hook_add(UC_HOOK_CODE, hook, begin=addr, end=addr)
        return calls

    def run_between(self, start, stops, max_insns=500000, **regs):
        """run from start until the PC reaches any address in stops; returns the stop address"""
        from unicorn import UC_HOOK_CODE
        hit = []
        def hook(uc, address, size, user):
            if address in stops:
                hit.append(address); uc.emu_stop()
        h = self.uc.hook_add(UC_HOOK_CODE, hook)
        for k, v in regs.items(): self.set_reg(k, v)
        self.set_reg('a7', RAM_BASE + 0xF000)
        try:
            self.uc.emu_start(start, 0xFFFFFE, count=max_insns)
        finally:
            self.uc.hook_del(h)
        return hit[0] if hit else None

    def run_range(self, start, stop, max_insns=500000, **regs):
        for k, v in regs.items(): self.set_reg(k, v)
        self.set_reg('a7', RAM_BASE + 0xF000)
        self.uc.emu_start(start, stop, count=max_insns)
