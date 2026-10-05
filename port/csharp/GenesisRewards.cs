// GenesisRewards.cs -- combat-victory rewards of the Genesis game: the XP pool, credits and the loot pool. Ported from the ROM
// and verified against it (port/tests/ProgressTests.cs; vectors from the real routines run in a 68000 emulator).
//
//   Tally    ROM 0x1631E  walks the combat slots: counts the living party, and for every defeated enemy adds its XP reward
//                         (record +0x40, unless record byte +0x52 bit 0 is set), its credits (record +0x1A, u32, added to the party purse
//                         [0xBA34]) and its gear (13 slots of 10 bytes from record +0x54) to the loot pool; the XP pool is then divided
//                         by the number of living party members (a 16-bit division: if the quotient does not fit, the ROM keeps the low word of the pool)
//   AddItem  ROM 0x16268  puts one gear item into the loot pool (0x6AF6, 10-byte entries, at most 14): the category table at 0xA829
//                         says whether an id drops (negative), is skipped (0) or stands for a base item (positive: redirect to the record at
//                         0xF17CC + 10*value); identical items (same id and byte +4) stack: quantities with bit 7 set count up by one,
//                         others add and cap at 100; a new item without quantity gets 0x81 (= one)

using System;
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    public sealed class LootPool
    {
        public const int MaxEntries = 14;
        public readonly List<byte[]> Entries = new List<byte[]>();      // 10 bytes each
        public int Count { get { return Entries.Count; } }
    }

    /// The parts of a monster record copy (214 bytes in RAM at 0xBA68 + index * 0xD6) that the reward code reads.
    public sealed class RewardRecord
    {
        public int XpReward;            // +0x40 (u16)
        public int Flag52;              // +0x52: bit 0 set = no XP for this monster
        public uint Credits;            // +0x1A (u32)
        public byte[][] Gear;           // 13 slots x 10 bytes from +0x54; id 0 = empty, byte 5 bit 6 = built-in (never dropped), bit 7 = readied
    }

    public static class GenesisRewards
    {
        public const int DropCategoryTable = 0xA829, BaseItemTable = 0xF17CC;

        /// ROM 0x16268. `item` is a 10-byte gear slot. Returns true if the pool changed. noStack = the byte [0xBA60] (every item is appended, never merged,
        /// and quantity-less items do not get the default 0x81).
        public static bool AddItem(RomView rom, LootPool pool, byte[] item, bool noStack)
        {
            byte[] cur = item;
            int d0;
            while (true)
            {
                d0 = cur[0];
                if (d0 == 0) return false;
                int cat = rom.Byte(DropCategoryTable + d0);
                if (cat >= 0x80) break;                                   // negative: a droppable item
                if (cat == 0) return false;                               // not droppable
                cur = new byte[10];                                       // positive: stands for a base item record
                for (int i = 0; i < 10; i++) cur[i] = (byte)rom.Byte(BaseItemTable + cat * 10 + i);
            }
            int d4 = cur[4], d1 = cur[8];
            if (!noStack)
            {
                foreach (var e in pool.Entries)
                {
                    if (e[0] != d0 || e[4] != d4) continue;
                    int d3 = e[8];
                    if (d3 >= 0x80) d3 = d3 + 1;                          // counted items: one more
                    else { d3 += d1; if (d3 >= 100) d3 = 100; }           // charged/stacked items: add, cap 100
                    e[8] = (byte)d3;
                    return true;
                }
            }
            if (pool.Count >= LootPool.MaxEntries) return false;
            if (d1 == 0 && !noStack) d1 = 0x81;
            var n = new byte[10]; Array.Copy(cur, n, 10); n[8] = (byte)d1;
            pool.Entries.Add(n);
            return true;
        }

        public struct TallyResult
        {
            public uint XpPerMember;     // [0xD514] after the routine: the pool divided by the living party (or the whole pool in no-reward mode)
            public bool XpAwarded;       // [0xD50E] set: at least one defeated enemy gave XP
            public uint Credits;         // new value of the party purse [0xBA34]
            public int LivingParty;      // party members counted (before the division)
        }

        /// ROM 0x1631E. slotFlags/slotRecord: combat slot bytes 0 and 2 for slots 0..slotCount-1 (0-7 party, then enemies; bit 7 of an enemy = defeated;
        /// bit 7 of a party member = down). partyDownMask = byte [0xD8DA]. records indexed by the slot's record index. The gear flag byte (+5) of dropped
        /// items has its bit 7 cleared in the record, as in the ROM. noRewards = [0xBA5B] != 0 (no division, no credits/loot); mode97AE = [0x97AE] != 0
        /// (no credits/loot); scriptedXp >= 0 starts the pool with a scripted bonus (the ROM's table at 0x16402 when [0x9930] < 0 and [0x9927] == 1).
        public static TallyResult Tally(RomView rom, int slotCount, int[] slotFlags, int[] slotRecord, int partyDownMask,
            RewardRecord[] records, uint credits, LootPool pool, bool noRewards, bool mode97AE, bool noStack, int scriptedXp)
        {
            var r = new TallyResult();
            uint d6 = scriptedXp >= 0 ? (uint)scriptedXp : 0;
            uint living = 0;
            for (int d7 = 0; d7 < slotCount; d7++)
            {
                int d0 = slotFlags[d7] & 0xFF;
                if (d0 == 0) continue;
                if (d7 < 8)
                {
                    if (((partyDownMask >> d7) & 1) != 0) continue;
                    if ((d0 & 0x80) != 0) continue;
                    living++;
                    continue;
                }
                if ((d0 & 0x80) == 0) continue;                           // enemy still standing
                var rec = records[slotRecord[d7]];
                if ((rec.Flag52 & 1) == 0) { d6 += (uint)(rec.XpReward & 0xFFFF); r.XpAwarded = true; }
                if (noRewards || mode97AE) continue;
                credits = unchecked(credits + rec.Credits);
                for (int g = 0; g < 13; g++)
                {
                    var slot = rec.Gear[g];
                    if (slot[0] == 0) continue;
                    if ((slot[5] & 0x40) != 0) continue;                  // built-in item
                    slot[5] = (byte)(slot[5] & 0x7F);                     // bclr #7
                    AddItem(rom, pool, slot, noStack);
                }
            }
            r.Credits = credits; r.LivingParty = (int)living;
            if (noRewards) r.XpPerMember = d6;
            else if (living == 0) r.XpPerMember = 0;
            else
            {
                uint q = d6 / living;                                     // divu.w d1,d6: overflow leaves d6 unchanged
                r.XpPerMember = q <= 0xFFFF ? q : (d6 & 0xFFFF);
            }
            return r;
        }
    }
}
