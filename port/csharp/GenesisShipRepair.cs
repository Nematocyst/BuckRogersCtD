// GenesisShipRepair.cs -- the end of a scripted (starship) fight (ROM 0x16EF0): the crew repairs the ship's damage between battles. The ship's state is a small table at 0x98F6:
//   three components as words: maximum at +0, +2, +6 and current at +0x24, +0x26, +0x2A, and four 5-byte systems from +0x0C (byte +1 maximum, byte +2 current).
// Every crew member (slot 0..7, record skill 0x0B) rolls for each component and system; the repaired amounts are then put back piece by piece. Verified against the ROM
// in port/tests/MonsterTests.cs.
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public byte[] Ship = new byte[0x30];          // 0x98F6..0x9925

        int ShipWord(int off) { return Ship[off] << 8 | Ship[off + 1]; }
        void ShipSetWord(int off, int v) { Ship[off] = (byte)(v >> 8); Ship[off + 1] = (byte)v; }

        /// 0x16EF0
        public void ShipRepair()
        {
            T("ship");
            var f = new bool[3]; var pool = new int[8]; var def = new int[8];                    // per component offset (0, 2, 6)
            var poolB = new int[4]; var defB = new int[4];
            bool damage = false;
            int[] offs = { 0, 2, 6 };
            for (int d6 = 2; d6 >= 0; d6--)
            {
                int off = offs[d6];
                int d1 = (ShipWord(off) - ShipWord(off + 0x24)) & 0xFFFF;
                if (d1 != 0) damage = true;
                pool[off] = 0; def[off] = d1;
            }
            for (int d6 = 3; d6 >= 0; d6--)
            {
                int a = 0x0C + 5 * d6;
                int d1 = (Ship[a + 1] - Ship[a + 2]) & 0xFF;
                if (d1 != 0) damage = true;
                poolB[d6] = 0; defB[d6] = d1;
            }
            if (!damage) return;
            for (int d7 = 7; d7 >= 0; d7--)
            {
                if (d7 >= S.Records.Length) continue;
                int d5 = GenesisSkills.SkillValue(Rom, S.Records[d7], d7 < S.SlotCount ? S.Slots[d7][0] : 0, 0xB) & 0xFF;
                if (d5 == 0) continue;
                for (int d6 = 2; d6 >= 0; d6--)
                {
                    int r = Rng.Roll(100), d4 = d5 >> 1;
                    if (r < 5 || (r <= 0x5F && r <= d5)) f[d6] = true; else d4 >>= 1;
                    int off = offs[d6], d1 = def[off];
                    if (d1 != 0)
                    {
                        uint prod = (uint)((d1 & 0xFFFF) * (d4 & 0xFFFF));
                        uint quo = prod / 100, rem = prod % 100;
                        d1 = (int)(quo & 0xFFFF);
                        if (rem >= 0x32) d1 = (d1 + 1) & 0xFFFF;
                        if (d1 >= def[off]) d1 = def[off];
                        def[off] -= d1; pool[off] += d1;
                    }
                }
                for (int d6 = 3; d6 >= 0; d6--)
                {
                    int n = defB[d6];
                    for (int k = 0; k < n; k++)
                    {
                        int r = Rng.Roll(100);
                        if (r < 5 || (r <= 0x5F && r <= d5)) { poolB[d6]++; defB[d6]--; }
                    }
                }
            }
            for (int d6 = 2; d6 >= 0; d6--)
            {
                if (!f[d6]) continue;
                int off = offs[d6], d1 = pool[off];
                if (d1 >= 0x50) continue;
                pool[off] = 0; def[off] += d1;
                d1 = def[off]; if (d1 >= 0x50) d1 = 0x50;
                pool[off] = d1; def[off] -= d1;
            }
            S.CombatMode = 5;
            while (true)                                                                         // the repairs arrive 0x28 at a time
            {
                bool any = false;
                for (int d6 = 2; d6 >= 0; d6--)
                {
                    int off = offs[d6], d1 = pool[off];
                    if (d1 == 0) continue;
                    any = true;
                    if (d1 > 0x28) d1 = 0x28;
                    ShipSetWord(off + 0x24, (ShipWord(off + 0x24) + d1) & 0xFFFF); pool[off] -= d1;
                    if (ShipWord(off) < ShipWord(off + 0x24)) { ShipSetWord(off + 0x24, ShipWord(off)); pool[off] = 0; }
                }
                for (int d6 = 3; d6 >= 0; d6--)
                {
                    if (poolB[d6] == 0) continue;
                    any = true; poolB[d6]--;
                    int a = 0x0C + 5 * d6;
                    Ship[a + 2]++;
                    if (Ship[a + 1] < Ship[a + 2]) { Ship[a + 2] = Ship[a + 1]; poolB[d6] = 0; }
                }
                if (!any) break;
            }
            PressC();
        }
    }
}
