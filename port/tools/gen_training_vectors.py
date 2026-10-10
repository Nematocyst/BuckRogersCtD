"""Vectors for the training screen's level-up of one character (ROM 0xCD0 with the skill pick 0xE8C and the speciality pick 0xDC4; menu 0x1391A scripted, screens stubbed).
usage: python gen_training_vectors.py ROM OUT.txt.gz
One case per line: member idx d499 d49a | skill answers | speciality answers | record hex | slot hex | after: record hex slot hex menucalls ridx rsum"""
import sys, os, random, struct, gzip
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE
from unicorn.m68k_const import UC_M68K_REG_A6

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0xCD0)
UI = (0x1344E, 0x11C8E, 0x11BF2, 0x11BDC, 0x11CA0, 0x13322, 0x136DA, 0xC8FC, 0x748C, 0x9DD4, 0x8A44, 0xAC6C, 0x95BE, 0xA85A, 0xA9EA, 0x14246, 0x978C)
lines = []
for n in range(int(os.environ.get('N_training', 400))):
    m = Machine(rom); m.map_io()
    for a in UI: m.stub_rts(a)
    member = rnd.randrange(8); idx = rnd.randrange(256); d499 = rnd.randrange(-2, 3) & 0xFF; d49a = rnd.randrange(-2, 3) & 0xFF
    career = rnd.choice([1, 2, 3, 3, 4]); level = rnd.randrange(1, 9)
    rec = bytearray(214); rec[0x18] = career; rec[0x19] = level; rec[0x15] = rnd.randrange(3, 15); rec[0x12] = rnd.randrange(3, 20)
    for k in range(6): rec[0x10 + k] = rnd.randrange(3, 20)
    rec[0x22] = rnd.randrange(30, 60); rec[0x24] = rnd.randrange(4, 12); rec[0x25] = 0x32; rec[0x2E] = rnd.randrange(5, 100)
    for k in range(15): rec[0x31 + k] = rnd.choice([0, 0, 1, 2, 3, 5, 8, 10, 11, 12, rnd.randrange(14)])
    for k in range(rnd.choice([0, 1, 2, 3, 4, 5])): rec[0x4D + k] = rnd.choice([3, 8, 6, 14, 9, 10])
    rec[0xAE] = rnd.choice([0, 0, 9, 10]); rec[0xAE + 4] = rnd.randrange(0, 3)
    slot = bytearray(26); slot[0] = 1; slot[1] = rnd.choice([1, 0x21]); slot[2] = member
    skh = [rnd.choice([-1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 3, 3]) for _ in range(rnd.randrange(0, 14))]; ska = skh + [i % 11 for i in range(200)]   # then 0,1,2.. cycling
    sph = [rnd.choice([-1, 0, 1, 2, 3, 4, 5, 5]) for _ in range(rnd.randrange(0, 6))]; spa = sph + [i % 6 for i in range(100)]
    st = {'sk': 0, 'sp': 0, 'calls': 0}
    def menu():
        a6 = m.uc.reg_read(UC_M68K_REG_A6)
        pts = struct.unpack('>H', bytes(m.uc.mem_read(a6 - 6, 2)))[0]
        st['calls'] += 1
        if pts == 1: v = spa[st['sp']]; st['sp'] += 1
        else: v = ska[st['sk']]; st['sk'] += 1
        return v & 0xFFFFFFFF
    m.stub_fn(0x1391A, menu)
    m.write_ram(0xBA68 + 214 * member, bytes(rec)); m.write_ram(0xC470 + 26 * member, bytes(slot))
    m.write_ram(0xCA20, bytes([member])); m.write_ram(0xD499, bytes([d499])); m.write_ram(0xD49A, bytes([d49a]))
    boot = Machine(rom); boot.run_range(0x12C8, 0x12E4); m.write_ram(0xD604, boot.read_ram(0xD604, 512)); m.write_ram(0xD804, bytes([idx]))
    m.call(0xCD0, a6=RAM_BASE + 0x8000, max_insns=3_000_000)
    r2 = m.read_ram(0xBA68 + 214 * member, 214); s2 = m.read_ram(0xC470 + 26 * member, 26)
    lines.append(' '.join(map(str, [member, idx, d499, d49a])) + ' | ' + (','.join(map(str, skh)) or '-') + ' | ' + (','.join(map(str, sph)) or '-') + ' | ' + bytes(rec).hex() + ' | ' + bytes(slot).hex() +
                 ' | ' + r2.hex() + ' ' + s2.hex() + ' ' + str(st['calls']) + ' ' + str(m.ram_byte(0xD804)) + ' ' + str(sum(struct.unpack('>256H', m.read_ram(0xD604, 512))) & 0xFFFFFFFF))
with gzip.open(sys.argv[2], 'wt') as f: f.write('\n'.join(lines) + '\n')
print(len(lines), 'cases')
