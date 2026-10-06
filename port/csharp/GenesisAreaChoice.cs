// GenesisAreaChoice.cs -- where an explosive is thrown (ROM 0xEB50): every candidate cell around the thrower is scored by the creatures a blast there would hit.
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        /// 0xEB50. `score` mode (the weapon scorer, 0xEAA6): returns the expected value of the best cell for the item at `itemOff` (best score x the item's typical damage,
        /// 0 when no cell is worth it). Execute mode (the turn controller): throws the hand item at the best cell, or - when no cell scores - picks a normal weapon
        /// instead (ChooseWeapon(0)). Draws random numbers in both modes.
        /// A cell scores +4 / +1 per creature of the own / other side for chaff and aerosol grenades (ids 0x25 / 0x26: they help), otherwise -2 (-12 for party throwers) per
        /// creature of the own side and +1 per enemy; creatures already inside a patch (tile 0 / 1) count 0 for the gas grenades, creatures with effect 0xE for stun grenades.
        public int AreaEval(bool score, int itemOff)
        {
            T("eb50");
            var me0 = S.Slots[Actor]; var rec0 = S.Records[me0[2]];
            if (!score) itemOff = 0xAE;
            int id = rec0[itemOff];
            if (id == 0) return 0;
            int kind = Tbl(id, 1);
            int reach = kind == 7 ? 2 : kind == 9 ? 3 : 1;
            int center = Actor;
            int range = WeaponRangeOf(itemOff);
            int r = range + reach;
            if (r > 9)
            {
                r = 9;
                if (id != 0x25 && id != 0x26 && (sbyte)me0[0x17] >= 0) center = me0[0x17];
            }
            int inner = r - reach;
            var cs = S.Slots[center];
            int[] w = new int[2];                                           // weight by side bit of the creature
            bool gas = id == 0x25 || id == 0x26;
            bool partyThrower = (me0[1] & 1) != 0;
            if (gas) { if (partyThrower) { w[1] = 4; w[0] = 1; } else { w[1] = 1; w[0] = 4; } }
            else { if (partyThrower) { w[1] = 0xF4; w[0] = 1; } else { w[1] = 1; w[0] = 0xFE; } }
            int cx = cs[0x12], cy = cs[0x13];
            int xmin = cx - r, ymin = cy - r, xmax = cx + r, ymax = cy + r;
            int ixmin = cx - inner, iymin = cy - inner, ixmax = cx + inner, iymax = cy + inner;
            var grid = new sbyte[19 * 19 + 40];                                // score per cell relative to the centre (index 180 + dy*19 + dx)
            const int origin = 180;
            for (int s = 0; s < S.SlotCount; s++)
            {
                var sl = S.Slots[s];
                if (sl[0] == 0 || (sl[0] & 0xC0) != 0) continue;
                int px = sl[0x12], py = sl[0x13];
                if (px < xmin || px > xmax || py < ymin || py > ymax) continue;
                int d4 = w[sl[1] & 1];
                if (gas)
                {
                    int tile; CellInfo(px, py, out tile);
                    if (tile == 0 || tile == 1) d4 = 0;
                }
                else if (id == 0x23)
                {
                    if (HasEffectFor(s, S.Records[cs[2]], 0xE)) d4 = 0;
                }
                int dyMin = -reach, dyMax = reach;
                if (py - reach < iymin) dyMin = iymin - py;
                if (py + reach > iymax) dyMax = iymax - py;
                for (int dy = dyMin; dy <= dyMax; dy++)
                {
                    int ext = (sbyte)Rom.Byte(ABlastShape + reach + 3 * dy);
                    int xl = px - ext, xr = px + ext;
                    if (xl < ixmin) xl = ixmin;
                    if (xr > ixmax) xr = ixmax;
                    int idx = origin + (dy + py - cy) * 19 + xl - cx;
                    int xx = xl;
                    do                                                          // a do-while in the ROM: when the clipping leaves xl > xr the first cell still gets the weight
                    {
                        if (idx >= 0 && idx < grid.Length)
                        {
                            int sum = grid[idx] + (sbyte)d4;
                            if (sum >= -128 && sum <= 127) grid[idx] = (sbyte)sum;    // signed overflow: the ROM undoes the add
                        }
                        xx++; idx++;
                    } while (xx <= xr);
                }
            }
            if (TraceLof) { var sb = new System.Text.StringBuilder("grid c=" + center + " r=" + r + " in=" + inner + " reach=" + reach + " "); for (int q = 0; q < 361; q++) sb.Append(((byte)grid[q]).ToString("x2")); T(sb.ToString()); }
            var me = me0;
            int ax = me[0x12], ay = me[0x13];
            int best = 0, bestX = 0, bestY = 0;
            for (int dx = -inner; dx <= inner; dx++)
                for (int dy = -inner; dy <= inner; dy++)
                {
                    int sc = grid[origin + dy * 19 + dx];
                    if (sc < 0) continue;
                    if (sc < best) continue;
                    if (sc == best && Rng.Roll(100) > 0x19) continue;
                    int x = (cx + dx) & 0xFFFF, y = (cy + dy) & 0xFFFF;
                    if (x >= 21 || y >= 21) continue;
                    D504 = 0xFF;
                    var t = Trace(x, y, ax, ay, range);
                    if (t.Clear && D501 == 0) { best = sc; bestX = x; bestY = y; }
                }
            if (!score)
            {
                if (best == 0) { ChooseWeapon(0); return 0; }
                Gs(A50D, 3);
                CursorX = bestX * 24; CursorY = bestY * 24;
                AreaAttack();
                return 0;
            }
            if ((sbyte)best < 0) return 0;
            int m;
            switch (id)
            {
                case 0x25: m = Rng.Roll(6) + 1; break;
                case 0x26: m = Rng.Roll(6); break;
                case 0x21: m = 40; break;
                case 0x24: m = Rng.Roll(10) + 10; break;
                case 0x23: m = Rng.Roll(10) + 5; break;
                case 0x11: m = 25; break;
                case 0x12: m = 30; break;
                default: m = 0; break;
            }
            return (int)(((uint)(m & 0xFFFF) * (uint)best) & 0xFFFF);
        }
    }
}
