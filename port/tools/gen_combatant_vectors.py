"""Combatant-creation vectors (ROM 0x3544 add monsters, 0x488C add party NPC, 0x48E8 record loader). The LZW decompressor 0x9ED8 is replaced by a plain stream of a random
monster file. usage: python gen_combatant_vectors.py ROM OUT.txt
One case per line: kind(m|a) id arg n d49b d499 d49a m97 file records(11) slots(56) | n d49b records slots (hex strings)"""
import sys, os, random, struct
sys.path.insert(0, os.path.dirname(__file__))
from emu68k import Machine, RAM_BASE
from unicorn import UC_HOOK_CODE
from unicorn.m68k_const import *
rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(0x48E8)
N = int(os.environ.get('N_combatant', 600))
SLOT, REC = 0xC470, 0xBA68
NPC = [0x3b, 0x3c, 0x3d, 0x3e, 0x6a, 0x6b, 0x6c]

def item_record():
    b = bytearray(rnd.randrange(256) for _ in range(214))
    b[0x23] = rnd.choice([0, 0, 1, 2, 3]); b[0x24] = rnd.randrange(256)
    for k in range(13):
        o = 0x54 + 10 * k
        if rnd.random() < 0.4: b[o:o + 10] = bytes(10); continue
        b[o] = rnd.choice(list(range(1, 39)) + [rnd.randrange(1, 255)] * 0); b[o + 8] = rnd.choice([0, 0, 1, 5, 10, 11, 200])
    return b

def run(kind, ident, arg, n, d49b, mdfile, recs, slots, m97):
    m = Machine(rom)
    stream = bytearray(); cur = [0]
    ids = [x[0] for x in mdfile]; stream += struct.pack('>H', len(ids)) + bytes(ids) + (b'\0' if len(ids) & 1 else b'')
    for x in mdfile: stream += x[1]
    def init(uc, address, size, user):
        if address == 0x9E76: cur[0] = 0; ret()
    def ret():
        sp = m.uc.reg_read(UC_M68K_REG_A7); r = struct.unpack('>I', bytes(m.uc.mem_read(sp, 4)))[0]
        m.uc.reg_write(UC_M68K_REG_A7, sp + 4); m.uc.reg_write(UC_M68K_REG_PC, r)
    def copy(uc, address, size, user):
        if address == 0x9ED8:
            dest = uc.reg_read(UC_M68K_REG_D0); cnt = uc.reg_read(UC_M68K_REG_D1) & 0xFFFF
            m._w(dest & 0xFFFF, bytes(stream[cur[0]:cur[0] + cnt])); cur[0] += cnt; ret()
    m.uc.hook_add(UC_HOOK_CODE, init, begin=0x9E76, end=0x9E76); m.uc.hook_add(UC_HOOK_CODE, copy, begin=0x9ED8, end=0x9ED8)
    for i, r in enumerate(recs): m.write_ram(REC + 214 * i, bytes(r))
    for i, s in enumerate(slots): m.write_ram(SLOT + 26 * i, bytes(s))
    m.write_ram(0xBA64, struct.pack('>H', n)); m.write_ram(0xD49B, bytes([d49b])); m.write_ram(0x97AE, bytes([m97]))
    script = bytes([0, ident, 0, arg, 0, 0]); m.write_ram(0xE000, script)
    m.call(0x3544 if kind == 'm' else 0x488C, a2=0x00FFE000, max_insns=3000000)
    post_recs = [m.read_ram(REC + 214 * i, 214) for i in range(11)]
    post_slots = [m.read_ram(SLOT + 26 * i, 26) for i in range(56)]
    return struct.unpack('>H', m.read_ram(0xBA64, 2))[0], m.ram_byte(0xD49B), post_recs, post_slots

lines = []
for k in range(N):
    nf = rnd.randrange(3, 40); ids = rnd.sample(range(1, 200), nf)
    mdfile = [(i, bytes(item_record())) for i in ids]
    recs = [bytes(item_record()) for _ in range(11)]
    n = rnd.choice([rnd.randrange(1, 12)] * 3 + [rnd.randrange(48, 57)])
    slots = []
    for i in range(56):
        s = bytearray(rnd.randrange(256) for _ in range(26))
        if i < n and rnd.random() < 0.8 or i < 8 and rnd.random() < 0.4: s[0] = rnd.choice([1, 1, 0x81, 0x83])
        elif i < 8: s[0] = 0
        slots.append(bytes(s))
    d49b = rnd.choice([0, 0, 1, 2]); m97 = rnd.choice([0, 0, 1])
    if rnd.random() < 0.7:
        kind = 'm'; ident = rnd.choice(ids); arg = rnd.choice([0, 1, 1, 2, 3, 6, 10, 30])
    else:
        kind = 'a'; ident = rnd.choice(NPC); arg = rnd.randrange(256)
        mdfile[rnd.randrange(len(mdfile))] = (ident, bytes(item_record())) if ident not in ids else mdfile[rnd.randrange(len(mdfile))]
        if ident not in [x[0] for x in mdfile]: mdfile[0] = (ident, mdfile[0][1])
    try: res = run(kind, ident, arg, n, d49b, mdfile, recs, slots, m97)
    except Exception as e: print('skip', e); continue
    f = lambda xs: b''.join(bytes(x) for x in xs).hex()
    lines.append(' '.join([kind, str(ident), str(arg), str(n), str(d49b), '0', '0', str(m97), b''.join(struct.pack('B', x[0]) + x[1] for x in mdfile).hex() + '/' + ','.join(str(x[0]) for x in mdfile) , f(recs), f(slots), str(res[0]), str(res[1]), f(res[2]), f(res[3])]))
open(sys.argv[2], 'w').write('\n'.join(lines) + '\n'); print(len(lines), 'combatant cases')
