// GenesisProgression.cs -- level-up, training and party XP, ported from the Genesis ROM and verified against it
// (port/tests/ProgressTests.cs, vectors from port/tools/gen_progress_vectors.py run the real ROM routines in a 68000 emulator).
//
//   HpAfterLevelUp      ROM 0x0B1C  HP gained on level-up: max(1, d(hit die) + CON bonus) + 4, +2 CON-bonus cap unless warrior, max 104
//   AttackValue         ROM 0x0B92  attack value (record +0x22) by career and level (table 0x0BB0)
//   Threshold           ROM 0x7418  XP needed to leave a level: table at 0x0C16, 8 u32 per career (last one = level-9 sentinel)
//   ScanTraining        ROM 0x7418  who can train, one level at a time: XP is capped to (next threshold - 1)
//   AwardXp             ROM 0x3C0A  ADDEP: every present, not-down party member gets the amount
//
// Tables are read through RomView, a sparse view of the ROM bytes the ports need, so even out-of-range arguments read the same
// bytes as the ROM does. RomView.FromRom(rom) uses a real ROM image; RomView.FromSegments loads the small export (rom_tables.json, ~5 KB).

using System;
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    /// Sparse read-only view of a Genesis ROM: only the listed ranges are present.
    public sealed class RomView
    {
        public static readonly int[][] DefaultRanges = {
            new[] { 0x0B70, 0x0C00 },     // hit dice, CON bonus, attack-value tables (+ the bytes beyond them that out-of-range indices read)
            new[] { 0x0C10, 0x0CB0 },     // XP thresholds per career
            new[] { 0x4FFC, 0x50D0 },                // ability used by each skill (skill check) + the bytes the party-wide SKILL search reads as slot flags (see GenesisScriptChar)
            new[] { 0x76C0, 0x7800 },                // STR / DEX / initiative tables at 0x7741 and 0x776F (a signed index reaches 0x76C1..0x77FE)
            new[] { 0x779E - 8 * 128, 0x779E + 8 * 128 },   // weapon table; item ids are sign-extended, so ids 0x80..0xFF index backwards
            new[] { 0xA829, 0xA829 + 0x100 },        // item drop category table (+ the bytes after it that ids >= 0x28 would read)
            new[] { 0xF17CC, 0xF17CC + 10 * 128 },   // base item records
            new[] { 0x6670, 0x66D2 },                // effect lists of the hook stages
            new[] { 0xEF4E, 0xEF64 },                // blast shape table of the area weapons (circle extents by reach)
            new[] { 0x2FF6, 0x3340 },                // battlefield generator: feature lists (0x2FF6, 0x300E), feature rows (0x30D8) - and the terrain / tile script tables of the two area kinds (0x31CB, 0x31F5, 0x3231, 0x325A)
            new[] { 0x146E0, 0x14738 },              // deployment: neighbour offsets, start positions, formations
            new[] { 0x16402, 0x16412 },              // scripted bonus experience by scripted fight (8 words)
            new[] { 0xA802, 0xA902 },                // item icons by id (loot screen)
            new[] { 0x16AE2, 0x16B28 },              // the loot screen's menu cell table
            new[] { 0xB332, 0xB396 },
            new[] { 0x48DA, 0x48E8 },
            new[] { 0x3978, 0x399A },                // TREASURE: the item ids a script may not hand out
            new[] { 0x9A14, 0x9BC0 },                // the creature token table (id -> sheet, animation set)                // the party NPC table (npc id -> record id)                // the dungeon arena: view offsets and the cell pattern table
            new[] { 0x0000, 0x0010 },                // the vector table: the inventory's weapon-readying test reads ROM[6] when the picked item is not there
            new[] { 0x7D78, 0x7D7C },                // item class each equipment slot of the inventory accepts (hand, armour, shield, ammunition)
            new[] { 0x10F69, 0x10F69 + 13 * 5 },     // explosive item types 0..12: effect, radius, hazard tile?, damage mode, animation
        };

        readonly SortedList<int, byte[]> seg = new SortedList<int, byte[]>();
        public void Add(int start, byte[] bytes) { seg[start] = bytes; }

        /// Build a view from exported segments (start address + hex string), e.g. parsed from rom_tables.json (see port/tools/export_rom_tables.py).
        public static RomView FromSegments(int[] starts, string[] hexes)
        {
            var v = new RomView();
            for (int i = 0; i < starts.Length; i++)
            {
                var b = new byte[hexes[i].Length / 2];
                for (int k = 0; k < b.Length; k++) b[k] = Convert.ToByte(hexes[i].Substring(2 * k, 2), 16);
                v.Add(starts[i], b);
            }
            return v;
        }

        public static RomView FromRom(byte[] rom, int[][] ranges = null)
        {
            var v = new RomView();
            foreach (var r in ranges ?? DefaultRanges) { var b = new byte[r[1] - r[0]]; Array.Copy(rom, r[0], b, 0, b.Length); v.Add(r[0], b); }
            return v;
        }

        public int Byte(int addr)
        {
            foreach (var kv in seg)
                if (addr >= kv.Key && addr < kv.Key + kv.Value.Length) return kv.Value[addr - kv.Key];
            throw new ArgumentOutOfRangeException(nameof(addr), $"ROM address 0x{addr:X} is not in this RomView");
        }
        public uint U32(int addr) { return ((uint)Byte(addr) << 24) | ((uint)Byte(addr + 1) << 16) | ((uint)Byte(addr + 2) << 8) | (uint)Byte(addr + 3); }
    }

    public static class GenesisProgression
    {
        public const int ThresholdTable = 0x0C16, HitDieTable = 0x0B78, ConBonusTable = 0x0B7C, AttackTable = 0x0BB0;

        // ---------------------------------------------------------------------------------------------- level-up 0x0B1C
        /// New maximum HP (record +0x2E, a byte) after one level-up. career = record +0x18 (1 rocket jock, 2 medic, 3 warrior, 4 rogue),
        /// con = record +0x12, hp = current record +0x2E. Consumes one RNG roll.
        public static int HpAfterLevelUp(RomView rom, GenesisRng rng, int career, int con, int hp)
        {
            int dieIndex = (short)((career & 0xFF) - 1);                       // subq.l #1,d0 ; (a0,d0.w): career 0 reads the byte before the table
            int die = rom.Byte(HitDieTable + dieIndex);
            int d0 = rng.Roll(die) & 0xFF;                                      // jsr 6C8C: 1..die
            int d1 = (sbyte)rom.Byte(ConBonusTable + (con & 0xFF));             // CON bonus (signed byte)
            if (d1 > 2 && (career & 0xFF) != 3) d1 = 2;                         // only warriors keep a CON bonus above +2
            d0 = (sbyte)(byte)(d0 + d1);                                        // add.b
            if (d0 <= 0) d0 = 1;                                                // cmp.b #0 ; bgt
            d0 += 4;                                                            // addq.l #4
            int h = (hp + d0) & 0xFF;                                           // add.b d0,$2E(a2)  (wraps)
            if (h >= 0x68) h = 0x68;                                            // cmpi.b #$68 ; bcs
            return h;
        }

        // ---------------------------------------------------------------------------------------------- attack value 0x0B92
        /// Attack value stored in record +0x22 for a career and level (the table covers careers 1-4, levels 2-9; other values read neighbouring bytes like the ROM).
        public static int AttackValue(RomView rom, int career, int level)
        {
            int d0 = (career & 0xFF) << 3;                                      // asl.w #3
            d0 = (d0 & 0xFF00) | ((d0 + level) & 0xFF);                         // add.b level
            d0 = (d0 & 0xFF00) | ((d0 - 10) & 0xFF);                            // subi.b #10
            return rom.Byte(AttackTable + (short)d0);
        }

        // ---------------------------------------------------------------------------------------------- thresholds 0x7418
        /// XP needed to reach level+1 from `level` (table index career*8 + level - 9; 0xFFFFFFFF for level 8, the cap).
        public static uint Threshold(RomView rom, int career, int level, int plus = 0)
        {
            int d1 = (career & 0xFF) << 3;                                      // asl.w #3
            d1 = (d1 & 0xFF00) | ((d1 + level) & 0xFF);                         // add.b level
            d1 = (d1 - 9) & 0xFFFF;                                             // subi.w #9
            d1 = (d1 << 2) & 0xFFFF;                                            // asl.w #2
            return rom.U32(ThresholdTable + (short)d1 + plus);
        }

        public struct TrainingScan
        {
            public bool AnyReady;            // at least one character has enough XP for the next level (ROM returns it in d0)
            public List<int> NotReady;       // ordinals (counting present slots only) of characters that cannot train now, as the ROM lists them
            public uint[] Xp;                // XP after the scan: ready characters with more than one level of XP are capped at next threshold - 1
        }

        /// slotFlags[8] = combat slot byte 0 (0 = empty, 1 = active, anything else = unavailable); career/level/xp per record.
        public static TrainingScan ScanTraining(RomView rom, int[] slotFlags, int[] career, int[] level, uint[] xp)
        {
            var r = new TrainingScan { NotReady = new List<int>(), Xp = (uint[])xp.Clone() };
            int d3 = 0;
            for (int s = 0; s < 8; s++)
            {
                int flag = slotFlags[s] & 0xFF;
                if (flag == 0) continue;                                        // empty slot: not counted at all
                if (flag == 1)
                {
                    uint need = Threshold(rom, career[s], level[s]);
                    if (r.Xp[s] >= need)
                    {
                        uint next = Threshold(rom, career[s], level[s], 4);
                        if (r.Xp[s] >= next) r.Xp[s] = next - 1;                // at most one level per training session
                        r.AnyReady = true;
                        d3++; continue;
                    }
                }
                r.NotReady.Add(d3 & 0xFF);
                d3++;
            }
            return r;
        }

        // ---------------------------------------------------------------------------------------------- party XP 0x3C0A
        /// ADDEP with a non-zero first operand: every present party member that is not down (slot flags & 0xC0 == 0) receives `amount` (u32, wraps).
        public static uint[] AwardXp(int[] slotFlags, uint[] xp, uint amount)
        {
            var r = (uint[])xp.Clone();
            for (int s = 0; s < 8; s++)
            {
                int f = slotFlags[s] & 0xFF;
                if (f != 0 && (f & 0xC0) == 0) r[s] = unchecked(r[s] + amount);
            }
            return r;
        }
    }
}
