"""Export the small ROM tables the C# ports read (RomView.DefaultRanges) so a game does not need the ROM at run time.
usage: python export_rom_tables.py ROM OUT.json   -> {"segments":[{"start":N,"hex":"..."}, ...]}"""
import sys, json
RANGES = [(0x0B70, 0x0C00), (0x0C10, 0x0CB0), (0x4FFC, 0x500A), (0x76C0, 0x7800), (0x779E - 8 * 128, 0x779E + 8 * 128), (0xA829, 0xA829 + 0x100), (0xF17CC, 0xF17CC + 10 * 128), (0x6670, 0x66D2), (0xEF4E, 0xEF64), (0x10F69, 0x10F69 + 13 * 5), (0x0000, 0x0010), (0xA802, 0xA902), (0x16AE2, 0x16B28), (0x16402, 0x16412), (0x2FF6, 0x3340), (0x146E0, 0x14738), (0x7D78, 0x7D7C)]
rom = open(sys.argv[1], 'rb').read()
json.dump(dict(note='ROM table bytes for GenesisProgression/GenesisRewards/GenesisTurns/GenesisSkills (RomView.FromSegments)',
               segments=[dict(start=a, hex=rom[a:b].hex()) for a, b in RANGES]), open(sys.argv[2], 'w'), separators=(',', ':'))
print(sum(b - a for a, b in RANGES), 'bytes')
