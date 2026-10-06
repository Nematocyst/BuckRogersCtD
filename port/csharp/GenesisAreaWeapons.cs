// GenesisAreaWeapons.cs -- explosive / area weapons: choosing the blast cell (ROM 0xEB50), the blast itself (0x10FAA), lingering gas / fire patches on the map
// (0x1150E, 0x1158A, 0x145DE, 0x14670), saving throws and status effects (0x6990, 0x6A34), area damage (0x115CE).
// Verified against the ROM in port/tests/MonsterTests.cs (the special-effect hooks of 0x664E stay disabled, graphics / sound are not part of the port).
using System;
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public byte[] Haz = new byte[256];           // 0x78CE: 16 lingering patches of 16 bytes: x.w, y.w, 9 saved tiles (reversed), +0xE turns left, +0xF patch tile id
        public Func<int, int> TileScript;            // the table [0xD814] points at (terrain transformation chains for fire / acid blasts), null when the pointer is 0
        const int A55D = 0xD55D, A55E = 0xD55E, ATypeTable = 0x10F69, ABlastShape = 0xEF56;

        int HazX(int o) { return (short)((Haz[o] << 8) | Haz[o + 1]); }
        int HazY(int o) { return (short)((Haz[o + 2] << 8) | Haz[o + 3]); }
        void SetHazXY(int o, int x, int y) { Haz[o] = (byte)(x >> 8); Haz[o + 1] = (byte)x; Haz[o + 2] = (byte)(y >> 8); Haz[o + 3] = (byte)y; }
        int TileIndex(int x, int y) { return y * 21 + x; }
        int TileAtIndex(int i) { return i >= 0 && i < S.Tiles.Length ? S.Tiles[i] : 0; }

        // ------------------------------------------------------------------------------------ 0x145DE / 0x14670: tiles under patches
        /// The terrain id of a cell as it is without a lingering patch on it (a patch covers the cell with tile id 0 or 1 and keeps the original in its record).
        public int OriginalTile(int x, int y)
        {
            x = (sbyte)x; y = (sbyte)y;
            int t = TileAtIndex(TileIndex(x, y)) & 0x7F;
            if (t != 0 && t != 1) return t;
            for (int k = 0; k < 16; k++)
            {
                int o = k * 16; if (Haz[o + 0xE] == 0) continue;
                int dx = (ushort)(x - HazX(o)), dy = (ushort)(y - HazY(o));
                if (dx > 2 || dy > 2) continue;
                int b = (sbyte)Haz[o + 0xC - (dy * 3 + dx)];
                if (b < 0) continue;
                return b;
            }
            return t;                                                    // the ROM prints "failed Original Tile" and returns the patch id
        }

        /// Replaces the terrain id of a cell, or - when a patch covers it - the saved original inside the patch record (the ROM indexes the record in the opposite
        /// direction from OriginalTile here).
        public void SetTile(int x, int y, int id)
        {
            x = (sbyte)x; y = (sbyte)y;
            int idx = TileIndex(x, y); int t = TileAtIndex(idx) & 0x7F;
            if (t != 0 && t != 1) { if (idx >= 0 && idx < S.Tiles.Length) S.Tiles[idx] = (byte)id; return; }
            for (int k = 0; k < 16; k++)
            {
                int o = k * 16; if (Haz[o + 0xE] == 0) continue;
                int dx = (ushort)(x - HazX(o)), dy = (ushort)(y - HazY(o));
                if (dx > 2 || dy > 2) continue;
                int i = o + 4 + dy * 3 + dx;
                if ((sbyte)Haz[i] < 0) continue;
                Haz[i] = (byte)id;
            }
        }

        /// 0x144FE: follows the terrain transformation chain of [0xD814] from `tile`: 0x80 n = remember n, 0x81 n = jump to n or skip (coin flip), other negative = jump.
        /// Returns the final tile id; `mark` is the remembered byte (0xFF none).
        public int TileChain(int tile, out int mark)
        {
            int d3 = 0xFF; mark = d3;
            if (TileScript == null) return tile;
            int d2 = tile & 0x7F;
            while (true)
            {
                int d0 = (sbyte)TileScript(d2 & 0xFFFF);
                if (d0 >= 0) { mark = d3; return d0; }
                if (d0 == -128) { d3 = TileScript((d2 + 1) & 0xFFFF); d2 = (d2 + 2) & 0xFFFF; }
                else if (d0 == -127)
                {
                    int r = Rng.Next(100) + 1;
                    if ((r & 1) != 0) d2 = (d2 & ~0xFF) | TileScript((d2 + 1) & 0xFFFF);
                    else d2 = (d2 & ~0xFF) | ((d2 + 2) & 0xFF);
                }
                else d2 = (d2 & ~0xFF) | (d0 & 0xFF);
                mark = d3;
            }
        }

        // ------------------------------------------------------------------------------------ 0x1150E / 0x1158A: patches
        /// Removes patch k: its saved terrain goes back on the map, then the other patches repaint their own tile over the cells they still cover.
        public void RemoveHazard(int k)
        {
            int o = k * 16; Haz[o + 0xE] = 0;
            int x = HazX(o), y = HazY(o), n = 0;
            for (int d7 = 2; d7 >= 0; d7--)
                for (int d6 = 2; d6 >= 0; d6--)
                {
                    int b = (sbyte)Haz[o + 4 + n++];
                    if (b >= 0) { int i = (y + d7) * 21 + x + d6; if (i >= 0 && i < S.Tiles.Length) S.Tiles[i] = (byte)b; }
                }
            for (int j = 0; j < 16; j++)
            {
                int p = j * 16; if (Haz[p + 0xE] == 0) continue;
                int px = HazX(p), py = HazY(p); n = 0;
                for (int d7 = 2; d7 >= 0; d7--)
                    for (int d6 = 2; d6 >= 0; d6--)
                    {
                        int b = (sbyte)Haz[p + 4 + n++];
                        if (b >= 0) { int i = (py + d7) * 21 + px + d6; if (i >= 0 && i < S.Tiles.Length) S.Tiles[i] = Haz[p + 0xF]; }
                    }
            }
        }

        /// 0x1158A (once per round): patches count down; one with a single turn left is removed and the map markers are rebuilt.
        public void TickHazards()
        {
            bool any = false;
            for (int k = 0; k < 16; k++)
            {
                int o = k * 16; int d = Haz[o + 0xE];
                if (d == 0) continue;
                if (d == 1) { RemoveHazard(k); any = true; } else Haz[o + 0xE] = (byte)(d - 1);
            }
            if (any) S.RebuildMarkers();
        }

        // ------------------------------------------------------------------------------------ 0x6990 / 0x6A34 / 0x115CE
        /// 0x6990: saving throw of creature `victim`: d20 (1 always fails, 20 always succeeds), plus `mod`, against record byte +0x15 (signed compare).
        public bool SavingThrow(int victim, int mod)
        {
            var vrec = S.Records[S.Slots[victim][2]];
            int d = Rng.Roll(20) & 0xFF; Gs(A55D, d);
            if (d == 1) return false;
            if (d == 20) return true;
            Gs(A55D, Gb(A55D) + mod); Stage(0xC, victim);
            return (sbyte)Gb(A55D) >= (sbyte)vrec[0x15];
        }

        /// 0x6A34: puts status effect `effect` with duration `param` (0 = permanent) on the creature in [0xD513] (a3/a2 = that creature). A saved throw against a
        /// mode-2 effect cancels it. A running temporary effect keeps the longer duration; otherwise a new entry goes into the list at [0xD49C] (32 x 3 bytes).
        public void ApplyEffect(int victim, int effect, int param, bool saved, int mode)
        {
            Gs(A55E, effect); Stage(9, victim);
            if (Gb(A55E) == 0) return;
            if (mode == 2 && saved) return;
            int e = Gb(A55E);
            var rec = S.Records[S.Slots[victim][2]];
            bool inRecord = false;
            for (int i = 0; i < 10; i++) if (rec[0x43 + i] == e) inRecord = true;
            int found = -1;
            if (!inRecord)
                for (int k = 0; k < 32; k++) { int o = 0xD49C - GBase + 3 * k; if (G[o + 1] == e && G[o] == victim) { found = o; break; } }
            if (found >= 0)
            {
                if (param == 0 || param >= G[found + 2]) G[found + 2] = (byte)param;
                return;
            }
            for (int k = 0; k < 32; k++)
            {
                int o = 0xD49C - GBase + 3 * k;
                if (G[o + 1] != 0) continue;
                G[o] = Gb(A513); G[o + 1] = Gb(A55E); G[o + 2] = (byte)param;
                return;
            }
        }

        /// 0x115CE: blast damage on one creature: a saved throw halves it (mode 1..) or cancels it (mode 2); mode 0 ignores the save. Same wound model as a hit.
        public void AreaDamage(int victim, int dmg, bool saved, int mode)
        {
            if (saved)
            {
                if (mode == 2) return;
                if (mode != 0) dmg = ((sbyte)dmg >> 1) & 0xFF;
            }
            S.ApplyDamage(victim, dmg);
        }
    }
}
