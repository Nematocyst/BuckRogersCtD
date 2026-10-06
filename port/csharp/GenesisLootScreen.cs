// GenesisLootScreen.cs -- the loot sharing screen (ROM 0x165A0): after a fight the pool (up to 14 items, see GenesisCombatEnd) is handed out to the party members; the same
// screen is the shop's "buy" screen when [0xBA60] is set (items cost money, the pool never runs out). Like the inventory screen it is a menu of cells driven by the host's
// InventoryMenu / AskQuantity callbacks, with the cells the ROM greys out in the disabled list (CellDisabled). Verified against the ROM in port/tests/MonsterTests.cs.
//   cells 0..7 party members (switch to / give to), 8 leave, 9..22 the 14 pool items
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public byte PriceFactor;                 // [0x9E63]: shop price modifier, price = word at item +6 * factor / 16
        public Func<int, int> PromptHook;        // 0x136DA for the loot screen's "leave items behind?" box (message 8); default: ChoicePrompt on the pad, or "no"

        int PoolPrice(byte[] item, int off) { return (((item[off + 6] << 8) | item[off + 7]) * PriceFactor) >> 4; }                 // 0x168EA

        /// 0xA7BC: the icon of an item (0xFF = none, shown as a blank cell 0x4E).
        int ItemIcon(byte[] item, int off)
        {
            int d0 = Rom.Byte(0xA802 + item[off]);
            if (d0 == 0x13 || d0 == 0x14) { if ((item[off + 5] & 0x30) != 0) d0 = (d0 + 0x59) & 0xFF; }
            else if ((sbyte)d0 < 0 && d0 != 0xFF) d0 = (d0 + item[off + 4] * 0x12) & 0xFF;
            return d0;
        }

        /// 0x165A0: share the loot pool out. Returns when the player leaves (after the "leave items behind?" box if anything is still there).
        public void ShareLoot()
        {
            T("loot");
            int saved = S.CombatMode; S.CombatMode = 0xC;
            int cur = 0, sel = 0xFF;
            var icon = new int[23];
            for (int c = 0; c < 23; c++) icon[c] = Rom.Byte(0x16AE2 + 3 * c + 2);
            bool shop = ShopFlag != 0;
            for (int i = 0; i < 4; i++) Gs(0xD5A0 + i, i == 2 ? 0x69 : i == 3 ? 0xC6 : 0);   // 0x169C6: the message routine the menu calls
            while (true)
            {
                sel = 0xFF;                                                                      // 0x165D8
                for (int k = 0; k < Math.Max((int)PoolCount, 1) && k < 14; k++) { int ic = ItemIcon(Pool, k * 10); icon[9 + k] = ic == 0xFF ? 0x4E : ic; }   // 0x168FC
                while (true)
                {
                    ListReset();                                                                // 0x165E0
                    if (sel >= 0xFF || (sbyte)sel < 0) DisableLootNothing(shop); else DisableLootPicked(sel);
                menu: ;
                    int ans = InventoryMenu();
                    Gs(0xD593, 0); Gs(0xD595, 0); Gs(0xD592, 0); Gs(0xD597, 0); Gs(0xD596, 0);
                    bool leave = false;
                    if (ans == -1 || ans == 0xFFFF) leave = true;
                    else
                    {
                        cur = ans & 0xFF;
                        if (cur < 8 && Gb(0xD59A) == 0)                                           // switch to that member
                        {
                            Ca[0] = (byte)cur;
                            if (cur >= S.SlotCount || S.Slots[cur][0] == 0) goto menu;
                            ListReset();
                            break;
                        }
                        if (ans == 8) leave = true;
                        else if ((sbyte)sel < 0)                                                   // pick up
                        {
                            sel = ans; Gs(0xD59A, icon[ans]);
                            if (!shop) icon[ans] = 0x4E;
                            continue;
                        }
                        else
                        {
                            // hand the picked item to member `ans`
                            var tmp = new byte[10]; Array.Copy(Pool, (sel - 9) * 10, tmp, 0, 10);
                            if ((sbyte)tmp[8] < 0) tmp[8] = 0;
                            var rec = S.Records[ans];
                            int dOff = FindDestination(tmp, 0, rec);
                            int qty = (sbyte)tmp[8], d2 = 0;
                            int destOff = dOff;
                            if (qty > 0)
                            {
                                int d1;
                                if (shop) { int price = PoolPrice(tmp, 0); uint q = Money / (uint)(price & 0xFFFF); d1 = q > 0xFFFF ? (int)(Money & 0xFFFF) : (int)q; if ((d1 & 0xFFFF) > 0xFA) d1 = 0xFA; }
                                else d1 = qty;
                                if (destOff >= 0 && rec[destOff] != 0) { int room = 0xFA - rec[destOff + 8]; if ((d1 & 0xFF) > (room & 0xFF)) d1 = room; }
                                d2 = d1 & 0xFF;
                                if (shop || d2 != 1) { d2 = AskQuantity(d2) & 0xFF; if (d2 == 0) goto menu; }
                            }
                            if (qty > 0 && destOff >= 0 && rec[destOff] != 0) rec[destOff + 8] = (byte)(rec[destOff + 8] + d2);
                            else
                            {
                                if (destOff < 0) { destOff = 0; rec = new byte[214]; }                 // no room (the menu should have refused): the ROM writes through a null pointer
                                for (int i = 0; i < 10; i++) rec[destOff + i] = Pool[(sel - 9) * 10 + i];
                                rec[destOff + 8] = (byte)d2;
                            }
                            if (d2 == 0) d2 = 1;
                            RecomputeSlotOf(cur, S.Records[cur]);
                            if (shop)
                            {
                                var dest = S.Records[ans]; int p2 = PoolPrice(dest, destOff);
                                Money = (uint)(Money - (uint)((p2 & 0xFFFF) * d2));
                                if ((uint)PoolPrice(dest, destOff) > Money) { Gs(0xD59A, 0); break; }
                                continue;
                            }
                            int po = (sel - 9) * 10;
                            Pool[po + 8] = (byte)(Pool[po + 8] - d2);
                            if ((Pool[po + 8] & 0x7F) != 0) continue;
                            Pool[po] = 0; Gs(0xD59A, 0); break;
                        }
                    }
                    if (leave)
                    {
                        if ((sbyte)sel >= 0) { Gs(0xD59A, 0); break; }
                        bool ok;
                        if (shop) ok = true;
                        else
                        {
                            bool any = false;
                            for (int d1 = 0xD; d1 >= 0; d1--) if (icon[d1 + 9] != 0x4E) { any = true; break; }
                            if (!any) ok = true;
                            else
                            {
                                int r = PromptHook != null ? PromptHook(8) : Pad != null ? ChoicePrompt(8) : 1;
                                if ((sbyte)r < 0) r = 0;
                                ok = (r & 0xFF) != 0;
                            }
                        }
                        if (!ok) { Gs(0xD59A, 0); break; }
                        ListReset();
                        for (int i = 0; i < 4; i++) Gs(0xD5A0 + i, 0);
                        Gs(0xD59A, 0);
                        S.CombatMode = saved;
                        return;
                    }
                }
            }
        }

        void DisableLootNothing(bool shop)                                                        // 0x168B2
        {
            for (int c = 9; c < 0x17; c++)
            {
                int o = (c - 9) * 10;
                bool dis = Pool[o] == 0 || (shop && PoolPrice(Pool, o) > Money);
                if (dis) ListAdd(c);
            }
        }

        void DisableLootPicked(int sel)                                                           // 0x16868
        {
            for (int c = 9; c < 0x17; c++) ListAdd(c);
            var tmp = new byte[10]; Array.Copy(Pool, (sel - 9) * 10, tmp, 0, 10);
            if ((sbyte)tmp[8] < 0) tmp[8] = 0;
            for (int d3 = 7; d3 >= 0; d3--)
            {
                bool there = d3 < S.SlotCount && S.Slots[d3][0] != 0;
                if (!there || d3 >= S.Records.Length || FindDestination(tmp, 0, S.Records[d3]) < 0) ListAdd(d3);
            }
        }
    }
}
