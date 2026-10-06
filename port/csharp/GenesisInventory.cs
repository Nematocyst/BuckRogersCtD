// GenesisInventory.cs -- the character sheet (ROM 0xFDC8 / 0x748C) and its inventory screen (0x78D6): which items may be moved where, and what moving, selling,
// dropping and handing items to another party member does to the records. Verified against the ROM in port/tests/MonsterTests.cs (the vectors drive the real screen with a
// scripted menu and quantity prompt). Everything drawn (stat page 0x7000, skills page 0x8034, the item icons, cursor, messages) is the host's job.
//
// The screen is a menu of 23 cells: 0..12 the 13 item slots of the current character (0..8 backpack, 9 = the hand, 10..12 = armour / shield / ammunition slots),
// 13 = drop (sell in a shop, [0xBA60] != 0), 14 = leave, 15..22 = the party members (switch to / hand an item to). The host's menu callback answers with a cell number
// (or a negative number = cancel); cells in the disabled list [0xD564..] (see CellDisabled) cannot be chosen. The first choice picks up an item, the second one
// puts it somewhere: another slot (the two items swap), the drop/sell cell, or a party member.
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public Func<int> InventoryMenu;                        // 0x1391A: the cell chosen (negative = cancel)
        public Func<int, int> AskQuantity;                     // 0x7DE6 / 0x134B0: "how many?" with the maximum; 0 = cancel
        public Func<int> SheetMenu;                            // 0x748C's page menu: <= 0 leave, 1 skills page, anything else the inventory
        public Action ShowSkills;                              // 0x8034: the skills page (display only)
        public uint Money;                                     // [0x9BD0]
        public byte ShopFlag;                                  // [0xBA60]: the inventory sells instead of dropping
        const int ListBase = 0xD564;

        /// is menu cell `cell` greyed out right now (in the disabled list [0xD564..])?
        public bool CellDisabled(int cell)
        {
            for (int a = ListBase; Gb(a) < 0x80; a++) if (Gb(a) == cell) return true;
            return false;
        }

        void ListReset() { Gs(ListBase, 0xFF); }                                   // 0x13DA2 / 0x13DD6
        void ListAdd(int cell)                                                      // 0x13E56
        {
            int a = ListBase; while (Gb(a) < 0x80) a++;
            Gs(a, cell); Gs(a + 1, 0xFF);
        }

        byte[] ActorRecord() { return S.Records[S.Slots[Actor][2]]; }
        static int ItemOff(int k) { return 0x54 + 10 * k; }
        int lastItem;                                                               // the item pointer register a2 of the ROM (index of the last item looked at by 0x7D7C)
        int lastClass = -1;                                                         // register a3 after 0x6E70: the weapon table row of that item

        /// 0x7D7C: is there an item in slot k of the current character's record (and may the player touch it: not in the "party is player-driven" mode)?
        bool ItemThere(int k)
        {
            lastItem = k;
            var rec = ActorRecord(); int id = rec[ItemOff(k)];
            if (id == 0 || Mode97AE != 0) return false;
            lastClass = WeaponTable + 8 * (sbyte)id;
            return true;
        }

        /// 0x7D2E: is cell `target` greyed out while item slot `sel` is picked up? Items go between backpack slots (0..8) freely; into slots 9..12 only items of the class
        /// that slot takes (weapon table class 0 hand, 3 / 1 / 7 for the other three).
        bool TargetDisabled(int sel, int target)
        {
            int d4 = sel, d3 = target;
            if (d4 == d3) return false;
            if (d4 < 9)
            {
                int t = d4; d4 = d3; d3 = t;
                if (d4 < 9) return false;
            }
            if (d3 >= 9) return true;
            if (!ItemThere(d3)) return false;
            int need = Rom.Byte(0x7D78 + (d4 - 9));
            return need != Rom.Byte(lastClass);
        }

        /// 0x7DB0: may item go to the party member of cell `cell`: alive (status set, not out of the fight), and not the current character.
        bool MemberReachable(int cell, out byte[] record)
        {
            int m = cell - 0xF; record = null;
            if (m < 0 || m >= S.SlotCount) return false;
            int st = S.Slots[m][0];
            if (st == 0 || (st & 0x40) != 0) return false;
            if (m >= S.Records.Length) return false;
            record = S.Records[m];
            return m != Actor;
        }

        /// 0x81E6: where would item (src, srcOff) go in record `dst`? A stack of the same item (same id and +4) with room (< 250); else the empty slot it belongs in
        /// (hand 0xAE class 0 / 2, 0xC2 class 1, 0xB8 class 3, 0xCC class 7), else the first empty backpack slot; -1 = nowhere.
        int FindDestination(byte[] src, int srcOff, byte[] dst)
        {
            if (src[srcOff + 8] != 0)
            {
                int id = src[srcOff], sub = src[srcOff + 4];
                for (int k = 0; k < 13; k++)
                {
                    int o = ItemOff(k);
                    if (dst[o] == id && dst[o + 4] == sub) return dst[o + 8] < 0xFA ? o : -1;
                }
            }
            int cls = Rom.Byte(WeaponTable + 8 * (sbyte)src[srcOff]);
            int eq;
            if (cls == 7) eq = 0xCC;
            else if (cls > 3) goto scan;
            else if (cls == 3) eq = 0xB8;
            else eq = cls == 1 ? 0xC2 : 0xAE;
            if (dst[eq] == 0) return eq;
        scan:
            for (int k = 0; k < 9; k++) if (dst[ItemOff(k)] == 0) return ItemOff(k);
            return -1;
        }

        /// 0x7C38: nothing picked up yet: grey out the drop cell, the party member cells (always while the sheet is open in mode 9, the current character and absent members
        /// otherwise) and the empty item slots.
        void DisableForNothingPicked()
        {
            ListAdd(0xD);
            for (int d3 = 0xF; d3 < 0x17; d3++)
            {
                int m = d3 - 0xF;
                bool absent = !(m < S.SlotCount && S.Slots[m][0] != 0 && (S.Slots[m][0] & 0x40) == 0);
                if (S.CombatMode == 9 || m == Actor || absent) ListAdd(d3);
            }
            for (int d3 = 0xC; d3 >= 0; d3--) if (!ItemThere(d3)) ListAdd(d3);
        }

        /// 0x7CA0: item slot `sel` picked up: grey out the slots it cannot go to and the members that cannot take it.
        void DisableForPicked(int sel)
        {
            for (int d3 = 0; d3 < 0xD; d3++) if (TargetDisabled(sel, d3)) ListAdd(d3);
            var me = S.Slots[Actor];
            bool all = S.CombatMode == 9 || ((me[1] & 0x40) != 0 && (me[0] & 0xF) == 1);
            if (all) { for (int d3 = 0xF; d3 < 0x17; d3++) ListAdd(d3); return; }
            var rec = ActorRecord();
            for (int d3 = 0xF; d3 < 0x17; d3++)
            {
                byte[] dst;
                if (!MemberReachable(d3, out dst) || FindDestination(rec, ItemOff(lastItem), dst) < 0) ListAdd(d3);
            }
        }

        void RecomputeSlotOf(int slot, byte[] rec)
        {
            GenesisStats.RecomputeSlot(Rom, S.Slots[slot], rec, Gb(A49A), Gb(A499), Mode97AE != 0);
        }

        /// 0x78D6: the inventory screen of the current character ([0xCA20]); returns when the player leaves it.
        public void InventoryScreen()
        {
            int sel;
            Gs(0xD593, 0);
            while (true)                                                                  // 0x78F4: (re)start for the current character
            {
                sel = 0xFF;
                while (true)                                                              // 0x795C
                {
                    ListReset();
                    if ((sbyte)sel < 0) DisableForNothingPicked(); else DisableForPicked(sel);
                menu: ;
                    int ans = InventoryMenu();
                    Gs(0xD593, 0); Gs(0xD595, 0); Gs(0xD592, 0); Gs(0xD597, 0); Gs(0xD596, 0);
                    if (ans < 0 || ans == 0xE)                                            // 0x7B76
                    {
                        if ((sbyte)sel >= 0) { sel = 0xFF; continue; }
                        ListReset(); Gs(0xD593, 0); Gs(0xD59A, 0);
                        for (int i = 0; i < 4; i++) Gs(0xD5A0 + i, 0);
                        return;
                    }
                    Gs(0xD593, ans);
                    if ((sbyte)sel < 0)
                    {
                        if (ans >= 0xF)                                                   // switch to that party member
                        {
                            ListReset(); Ca[0] = (byte)(ans - 0xF);
                            goto restart;
                        }
                        sel = ans; continue;                                              // picked up
                    }
                    if (ans == sel) { sel = 0xFF; continue; }
                    var me = S.Slots[Actor]; var rec = ActorRecord();
                    if (ans < 0xD)                                                        // swap two slots
                    {
                        int c6 = ItemThere(sel) ? Rom.Byte(lastClass + 6) : Rom.Byte(6);      // the register the ROM tests is the weapon table row of the picked item (ROM[6] when it is not there)
                        if (c6 != 0 && ans == 9) { me[0x14] = 0; me[0x15] = 0; }       // readying a slow weapon costs the turn
                        int a = ItemOff(sel), b = ItemOff(ans);
                        for (int i = 0; i < 10; i++) { byte t = rec[a + i]; rec[a + i] = rec[b + i]; rec[b + i] = t; }
                    }
                    else if (ans == 0xD)                                                  // drop / sell
                    {
                        int o = ItemOff(sel), d4 = sel;
                        int d2 = rec[o + 8];
                        int price = 0;
                        if (ShopFlag != 0) price = ((rec[o + 6] << 8 | rec[o + 7]) >> 1) & 0xFFFF;
                        if (d2 > 1)
                        {
                            d2 = AskQuantity(d2) & 0xFF;
                            if (d2 == 0) goto menu;
                            if (ShopFlag != 0) price = (int)(((uint)price * (uint)d2) & 0xFFFFFFFF);
                        }
                        if (ShopFlag != 0) Money += (uint)(price & 0xFFFF);
                        rec[o + 8] = (byte)(rec[o + 8] - d2);
                        if (rec[o + 8] == 0) rec[o] = 0;
                        if (ShopFlag == 0) ListReset();
                    }
                    else                                                                  // hand the item to a party member
                    {
                        byte[] dst;
                        MemberReachable(ans, out dst);
                        int so = ItemOff(sel), mem = ans - 0xF;
                        int dOff = FindDestination(rec, so, dst);
                        if (dOff < 0) { dst = new byte[16]; for (int i = 0; i < 16; i++) dst[i] = (byte)Rom.Byte(i); dOff = 0; }     // the menu let it through but there is no room: the ROM writes into its own ROM (ignored) and the item is lost
                        if (rec[so + 8] <= 1)
                        {
                            CopyItem(rec, so, dst, dOff, mem);
                            rec[so] = 0;
                        }
                        else
                        {
                            int d0 = rec[so + 8];
                            if (dst[dOff] != 0) { d0 = 0xFA - dst[dOff + 8]; if ((byte)d0 >= rec[so + 8]) d0 = rec[so + 8]; }
                            int d2 = AskQuantity(d0 & 0xFF) & 0xFF;
                            if (d2 == 0) goto menu;
                            if (dst[dOff + 8] == 0) { CopyItem(rec, so, dst, dOff, mem); dst[dOff + 8] = 0; }
                            dst[dOff + 8] = (byte)(dst[dOff + 8] + d2);
                            rec[so + 8] = (byte)(rec[so + 8] - d2);
                            if (rec[so + 8] == 0) rec[so] = 0;
                        }
                    }
                    RecomputeSlotOf(Actor, ActorRecord());                                // 0x7B50
                    Gs(0xD59A, 0); sel = 0xFF;
                }
            restart:;
            }
        }

        /// 0x7E42: copy an item into a slot of another member's record and work that member's stats out again.
        void CopyItem(byte[] src, int so, byte[] dst, int dOff, int member)
        {
            for (int i = 9; i >= 0; i--) dst[dOff + i] = src[so + i];
            RecomputeSlotOf(member, S.Records[member]);
        }

        /// 0xFDC8 / 0x748C: the character sheet of the current character: stats are worked out again, then the player pages through the stat page (the host),
        /// the skills page (ShowSkills) and the inventory screen. While it is open the game mode [0x9BBC] is 9 (no handing items over).
        public void CharacterSheetScreen()
        {
            int saved = S.CombatMode; S.CombatMode = 9;
            var me = S.Slots[Actor];
            GenesisStats.RecomputeSlot(Rom, me, S.Records[me[2]], Gb(A49A), Gb(A499), Mode97AE != 0);
            while (true)
            {
                int c = SheetMenu();
                Gs(0xD593, 0); Gs(0xD595, 0); Gs(0xD592, 0); Gs(0xD597, 0); Gs(0xD596, 0);
                if (c <= 0) break;
                Gs(0xD593, c);
                if (c == 1) ShowSkills?.Invoke(); else InventoryScreen();
            }
            S.CombatMode = saved;
        }
    }
}
