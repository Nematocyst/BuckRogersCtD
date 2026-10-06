// GenesisMonsterWeapons.cs -- weapon choice of a creature on its turn (ROM 0xE89C and helpers 0xEAA6, 0xEA90, 0x69C8/69DA, 0x6E80/6E9E/6EBC).
// Verified against the ROM in port/tests/MonsterTests.cs.
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        const int WeaponTable = 0x779E;
        int Tbl(int id, int k) { return Rom.Byte(WeaponTable + 8 * (sbyte)id + k); }

        // ------------------------------------------------------------------------------------ 0x69C8 / 0x69DA
        /// Does creature `slot` have effect `effect`? Looks in the ten effect ids at record +0x43..+0x4C, then in the temporary effect list at [0xD49C]
        /// (32 entries of 3 bytes: slot, effect id, duration).
        public bool HasEffect(int slot, int effect) { return HasEffectFor(slot, S.Records[S.Slots[slot][2]], effect); }

        /// 0x69DA with explicit pieces: the temporary list is searched for `slot`, the permanent ids in `rec` (the ROM passes whichever record register a2 holds).
        public bool HasEffectFor(int slot, byte[] rec, int effect)
        {
            for (int i = 0; i < 10; i++) if (rec[0x43 + i] == effect) return true;
            for (int k = 0; k < 32; k++)
            {
                int o = 0xD49C - GBase + 3 * k;
                if (G[o + 1] == effect && G[o] == slot) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------------------------ 0x6E80 / 0x6E9E / 0x6EBC
        /// 0x6E80: how many of the five weapon-specialisation ids at record +0x4D..+0x51 equal `weaponId`.
        static int SpecialisationCount(byte[] rec, int weaponId)
        {
            int n = 0; for (int i = 0; i < 5; i++) if (rec[0x4D + i] == weaponId) n++;
            return n;
        }

        // ------------------------------------------------------------------------------------ 0xEAA6: weapon score
        /// Expected-damage score of the item at `rec[itemOff..]` (10 bytes: id, .., .., .., modifier, flags, .., .., .., type) for the creature in `slot`.
        /// dice (table +3 x +4) + 2*(table +5) + 3*(specialisation + item modifier) + the STR (melee) or DEX (missile) bonus, times the table's +2 (shots).
        /// Explosive items (type 5..12) score 2 * the area evaluation, or 0 when `mode` is 0 or the creature may not use them.
        public int WeaponScore(byte[] slot, byte[] rec, int itemOff, int mode)
        {
            int id = rec[itemOff];
            int t3 = Tbl(id, 3), t4 = Tbl(id, 4), t5 = Tbl(id, 5);
            int d0 = (t3 * t4) & 0xFFFF;
            d0 = (d0 & 0xFF00) | ((d0 + 2 * t5) & 0xFF);
            int d2 = d0;
            int type = rec[itemOff + 9];
            if (type >= 5 && type <= 0xC)
            {
                if (mode == 0) return 0;
                bool grenade = id == 0x25 || id == 0x26;
                if (((slot[1] & 1) != 0 && grenade) || ((D97DC & 0x10) != 0 && grenade)) d2 = 0;
                else return (AreaScore(itemOff) << 1) & 0xFFFF;
            }
            int spec = SpecialisationCount(rec, id);
            d0 = (spec + rec[itemOff + 4]) & 0xFF;
            d0 = (d0 * 3) & 0xFFFF;
            d2 = (d2 + d0) & 0xFFFF;
            if (Tbl(id, 1) == 0)
            {
                int str = (sbyte)rec[0x10];
                d0 = (sbyte)Rom.Byte(0x7741 + 0x17 + str) + (sbyte)Rom.Byte(0x7741 + str);
            }
            else d0 = (sbyte)Rom.Byte(0x776F + 0x17 + (sbyte)rec[0x11]);
            d2 = (d2 + d0) & 0xFFFF;
            return (int)(((uint)d2 * (uint)Tbl(id, 2)) & 0xFFFF);
        }

        int AreaScore(int itemOff) { return AreaEval(true, itemOff); }

        // ------------------------------------------------------------------------------------ 0xEA90
        static void SwapItems(byte[] rec, int a, int b) { for (int i = 0; i < 10; i++) { byte t = rec[a + i]; rec[a + i] = rec[b + i]; rec[b + i] = t; } }

        // ------------------------------------------------------------------------------------ 0xE89C: choose the weapon
        /// The creature (current actor) looks through its 13 item slots and equips the best one (record +0xAE is the hand slot):
        ///  * the score of every weapon-class item (table +0 == 0) is compared with the creature's current unarmed value (dice x sides + bonus);
        ///    ranged items are discounted by the cover seen on the way to the target (shift by 3 / 2 when the line crosses water/cover tiles or the target has effect 0x18/0x14 ...)
        ///  * ammunition-like items (class 1) are kept in slot +0xC2, class 3 in +0xB8, class 7 in +0xCC when those are free (or the new one is better)
        ///  * equipping an item whose flags (+5) have bits 0x30 set, or whose type (+9) is 10/11, ends the turn (initiative word +0x14 cleared)
        ///  * with nothing better the hand item is put away into the last free slot before it, and the stats are recomputed (0x6D1E).
        /// `mode` is the ROM's argument (1 from the turn controller): 0 is forced when the actor stands on tile id 0.
        public void ChooseWeapon(int mode)
        {
            T("weapon");
            var me = S.Slots[Actor]; var rec = S.Records[me[2]];
            G[0xD513 - GBase] = me[0x17];
            int tileIdx = me[0x12] * 21 + me[0x13];                          // sic: x * 21 + y (the line of fire uses y * 21 + x)
            if (((tileIdx >= 0 && tileIdx < S.Tiles.Length ? S.Tiles[tileIdx] : 0) & 0x7F) == 0) mode = 0;
            int best = -1, empty = -1;
            int bestScore = ((me[0xA] * me[8]) & 0xFFFF) + me[0xC] & 0xFFFF;
            int shiftB = 0, shiftC = 0;
            int t = (sbyte)me[0x17];
            if (t >= 0)
            {
                LineOfFireTo(t, 100);
                if (D502 != 0 || HasEffect(t, 0x18)) shiftB = 3;
                if (D501 != 0) shiftC = 3;
                else
                {
                    if ((rec[0x2F] & 6) != 0 || HasEffect(t, 0x14)) shiftC = 2;
                }
            }
            if (!(Mode97AE != 0 && me[2] < 8))
            {
                for (int k = 0; k < 13; k++)
                {
                    int off = 0x54 + 10 * k;
                    int id = rec[off];
                    if (id == 0) { if (off < 0xAE) empty = off; continue; }
                    int cls = Tbl(id, 0);
                    if (cls == 0)
                    {
                        int d2 = WeaponScore(me, rec, off, mode);
                        int ty = Tbl(id, 1);
                        if (ty == 2) d2 = (ushort)((short)d2 >> shiftC); else if (ty == 3) d2 = (ushort)((short)d2 >> shiftB);
                        if (d2 > bestScore) { bestScore = d2; best = off; }
                    }
                    else if (cls == 1)
                    {
                        const int a0 = 0xC2;
                        if (rec[a0] == 0) SwapItems(rec, a0, off);
                        else if (off != a0)
                        {
                            int d3 = (rec[a0 + 4] + Tbl(rec[a0], 1)) & 0xFF;
                            int d4 = (rec[off + 4] + Tbl(id, 1)) & 0xFF;
                            if (d4 > d3) SwapItems(rec, a0, off);
                        }
                    }
                    else if (cls == 3) { if (off != 0xB8 && rec[0xB8] == 0) SwapItems(rec, 0xB8, off); }
                    else if (cls == 7) { if (off != 0xCC && rec[0xCC] == 0) SwapItems(rec, 0xCC, off); }
                }
                if (best >= 0)
                {
                    if ((rec[best + 5] & 0x30) != 0) { me[0x14] = 0; me[0x15] = 0; }
                    if (best != 0xAE)
                    {
                        if (rec[best + 9] == 0xA || rec[best + 9] == 0xB) { me[0x14] = 0; me[0x15] = 0; }
                        SwapItems(rec, 0xAE, best);
                    }
                    goto done;
                }
            }
            if (rec[0xAE] != 0 && empty >= 0) SwapItems(rec, 0xAE, empty);
        done:
            GenesisStats.RecomputeSlot(Rom, me, rec, G[0xD49A - GBase], G[0xD499 - GBase], Mode97AE != 0);
        }
    }
}
