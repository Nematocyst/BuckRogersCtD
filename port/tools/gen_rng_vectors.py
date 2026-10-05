"""Generate RNG test vectors by running the real ROM code (0x6C94 RNG, 0x6C8C 1..N variant, boot seeding 0x12C8-0x12E4).
usage: python gen_rng_vectors.py ROM OUT.json"""
import sys, json, random, struct
sys.path.insert(0, __import__('os').path.dirname(__file__))
from emu68k import Machine

rom = open(sys.argv[1], 'rb').read()
rnd = random.Random(1991)
S32 = lambda v: ((v + 2**31) % 2**32) - 2**31      # signed 32-bit, as C# int
out = {}

# 1. boot seeding: table built from 256 ROM words at 0x390E2 by the loop at 0x12D2..0x12E0
m = Machine(rom)
m.run_range(0x12C8, 0x12E4)
out['seed_words_rom_offset'] = 0x390E2
out['seed_table'] = list(struct.unpack('>256H', m.read_ram(0xD604, 512)))
out['seed_index'] = m.ram_byte(0xD804)

# 2. random states, call sequences of 6C94 with assorted d0 (incl. byte >= 128, 0, 255, 256, garbage upper bits)
trials = []
for t in range(60):
    m = Machine(rom)
    table = [rnd.randrange(65536) for _ in range(256)]
    idx = rnd.randrange(256)
    m.write_ram(0xD604, struct.pack('>256H', *table)); m.write_ram(0xD804, bytes([idx]))
    calls = []
    for _ in range(30):
        d0 = rnd.choice([rnd.randrange(0, 20), rnd.randrange(0, 256), 255, 256, 128, 127, 0, 1, rnd.getrandbits(32), rnd.randrange(100, 101)])
        m.call(0x6C94, d0=d0, d1=0x11111111, d2=0x22222222, d3=0x33333333)
        calls.append([S32(d0), S32(m.reg('d0')), m.ram_byte(0xD804)])
    trials.append(dict(table=table, index=idx, calls=calls, final=list(struct.unpack('>256H', m.read_ram(0xD604, 512)))))
out['trials'] = trials

# 3. 6C8C (1..N)
t3 = []
m = Machine(rom)
table = [rnd.randrange(65536) for _ in range(256)]
m.write_ram(0xD604, struct.pack('>256H', *table)); m.write_ram(0xD804, bytes([77]))
for _ in range(200):
    d0 = rnd.choice([rnd.randrange(1, 30), rnd.randrange(0, 256)])
    m.call(0x6C8C, d0=d0)
    t3.append([S32(d0), S32(m.reg('d0'))])
out['one_to_n'] = dict(table=table, index=77, calls=t3)
json.dump(out, open(sys.argv[2], 'w'), separators=(',', ':'))
print('trials', len(trials), 'calls', sum(len(t['calls']) for t in trials))
