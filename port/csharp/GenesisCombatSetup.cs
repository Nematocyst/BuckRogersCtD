// GenesisCombatSetup.cs -- what happens when a fight starts, before the first round (ROM 0xE394 -> 0x1503C): the battlefield is generated (0x149BA, tile map at 0xCACA), both sides
// are placed on it (0x14738 / 0x147EE), the party's recruits take command of the allies (0x14DF6) and the per-fight state is reset. The combat slots themselves (who fights) are
// filled by the caller (the script engine), the clean-up after the fight (0x15FDA: experience, treasure, status changes) is not part of this file.
// Verified against the ROM in port/tests/MonsterTests.cs.
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        // the game variables the setup reads
        public byte Facing;                // [0x9AFA]: the direction (0..3) the party faces / enters the battlefield from
        public byte Ambush;                // [0x9DB6]: monsters come from all sides (not only the one opposite the party)? (see Deploy) - stored in [0xD4FE]
        public byte GroupMask;             // [0xD8CC]: bit k set = a group of monsters enters from direction k+2 (0 = one group opposite the party)
        public byte AreaType;              // [0x97AD]: the kind of ground (0..10 outdoors, 11.. indoors) - selects the terrain generator and its feature list
        public int ScreenMode;             // [0xB52A]: 6 = a fixed map (skips the generator), 4 = outdoors tables, anything else indoor tables
        public byte SoloFlag;              // [0xBA5B]: only the creature [0x9DA7] fights for the party
        public byte SoloMember;            // [0x9DA7]
        public byte WideFormation;         // [0xBA5D]: a wider placement pattern
        public int TerrainTable, ScriptTable;   // [0xD810], [0xD814]: ROM addresses of the terrain flag table and the tile script the area selects
        public int ForcedSpread = -1;      // [0xD8CE]: a spread radius the generator forces on every feature (word, -1 = none)
        public byte PrevMode;              // [0x9BBD]: the game mode before the fight ([0x9BBC] is 2 during it)
        public int OriginX, OriginY;       // [0xD8D0], [0xD8D1]: where the party is placed from
        public Func<int> ContinuePrompt;   // 0x14246: "press C to continue" after a recruit took command (default: nothing to wait for)

        // ------------------------------------------------------------------------------------ 0x149BA: the battlefield
        /// 0x149BA: fills the 21x21 map with grass-like ground (tile 8 or 9 outdoors / 0x16 or 0x17 indoors, a coin per cell) and then stamps the area's features on it. The feature
        /// list (0x300E + table 0x2FF6 [area type]) is a sequence of (kind, repeats) pairs: kind 0x7F = force a random spread radius (3..6) on the next features, 0x7E = stop forcing, 0xFF.. ends.
        /// Each kind is a row of the table at 0x30D8 (11 bytes: chance %, dice count, dice sides, 3x3 flag, base tile, extra random tile, flags, tile A, tile B, random A, random B).
        public void GenerateBattlefield(int type)
        {
            T("terrain");
            type &= 0xFF;
            ForcedSpread = -1;
            int baseTile = type < 0xB ? 8 : 0x16;
            for (int i = 0; i < 441; i++) S.Tiles[i] = (byte)(baseTile + (Rng.Roll(2) & 1));
            int p = 0x300E + Rom.Byte(0x2FF6 + type);
            while (true)
            {
                int d0 = (sbyte)Rom.Byte(p++);
                if (d0 < 0) break;
                if (d0 == 0x7F) { ForcedSpread = Rng.Roll(4) + 2; continue; }
                if (d0 == 0x7E) { ForcedSpread = -1; continue; }
                int count = Rom.Byte(p++);
                FeatureGroup(type, d0, count);
            }
        }

        /// 0x14A30: `count` attempts to place feature `kind`: each succeeds when a d100 is within the row's chance.
        void FeatureGroup(int type, int kind, int count)
        {
            int row = 0x30D8 + (kind - 1) * 11;
            int n = count == 0 ? 0x10000 : count;
            for (int i = 0; i < n; i++)
                if (Rng.Roll(100) <= Rom.Byte(row)) PlaceFeature(type, row);
        }

        /// 0x14A5E: one feature row: the dice give how many times it is stamped; the spot is random unless the row continues from the previous one (flag 0x10).
        void PlaceFeature(int type, int row)
        {
            int repeats = 0, dice = Rom.Byte(row + 1), sides = Rom.Byte(row + 2);
            for (int i = 0; i < dice; i++) repeats = (repeats + Rng.Roll(sides)) & 0xFFFF;           // 0x6CE4
            Gs(0xD5F8, 1);
            int spread = Rom.Byte(row + 4);                                                         // d6: how far from the spot the stamps may land
            Gs(0xD5F9, Rom.Byte(row + 3));
            int flags = Rom.Byte(row + 6);
            if ((flags & 0x10) == 0) { Gs(0xD5FA, Rng.Roll(0x15)); Gs(0xD5FB, Rng.Roll(0x15)); }
            int extra = Rom.Byte(row + 5);
            if (extra != 0) spread = (spread + Rng.Roll(extra)) & 0xFF;
            if (ForcedSpread >= 0) spread = ForcedSpread;
            for (int i = 0; i < repeats; i++) Stamp(type, row, spread);
        }

        /// 0x14AC4: stamp the feature once (one cell, or a cell and its neighbour, or 3x3) at the spot (or at a random point within `spread` cells of it).
        void Stamp(int type, int row, int spread)
        {
            int flags = Rom.Byte(row + 6);
            int inc = 0;
            if (Gb(0xD5F8) != 0 || (flags & 2) == 0)                                                  // the tiles are drawn once per feature when flag 2 is set
            {
                Gs(0xD5FC, Rom.Byte(row + 7));
                int r9 = Rom.Byte(row + 9);
                if (r9 != 0)
                {
                    inc = Rng.Next(r9) & 0xFF;
                    if ((flags & 8) != 0) inc = (inc << 1) & 0xFF;
                    Gs(0xD5FC, Gb(0xD5FC) + inc);
                }
                Gs(0xD5FD, Rom.Byte(row + 8));
                if (Gb(0xD5FD) != 0)
                {
                    if ((flags & 4) == 0)
                    {
                        int r10 = Rom.Byte(row + 10);
                        if (r10 != 0)
                        {
                            inc = Rng.Next(r10) & 0xFF;
                            if ((flags & 8) != 0) inc = (inc << 1) & 0xFF;
                            Gs(0xD5FD, Gb(0xD5FD) + inc);
                        }
                    }
                    else Gs(0xD5FD, Gb(0xD5FD) + inc);                                                // flag 4: both tiles get the same random bonus
                }
                Gs(0xD5F8, 0);
            }
            int d4, d5;
            if ((spread & 0xFF) == 0) { d4 = (sbyte)Gb(0xD5FA); d5 = (sbyte)Gb(0xD5FB); }
            else
            {
                int d7 = spread & 0xFF;
                if ((flags & 0x20) == 0) d7 = Rng.Next(d7 + 1) & 0xFF;
                int r = Rng.Next(0x81) & 0xFF;                                                          // the divisor is the signed byte 0x81 = -127: the remainder is nearly the whole random word, and only its low byte is kept
                int q = ISqrt((0x3FFF - r * r) & 0xFFFF) & 0xFFFF;                                    // a random point in a quarter circle
                int w = (short)(sbyte)d7 & 0xFFFF;                                                    // ext.w d7: radii of 128+ are negative
                int a = (short)((r * w) & 0xFFFF) >> 7, b = (short)((q * w) & 0xFFFF) >> 7;
                int dir = Rng.Next(4);
                if ((dir & 1) != 0) a = -a;
                if ((dir & 2) != 0) b = -b;
                d4 = (sbyte)((a + Gb(0xD5FA)) & 0xFF); d5 = (sbyte)((b + Gb(0xD5FB)) & 0xFF);
            }
            if (!CanStamp(type, row, d4, d5)) return;
            int idx = d5 * 21 + d4;
            S.Tiles[idx] = Gb(0xD5FC);
            if (Gb(0xD5FD) != 0)
            {
                idx += (flags & 1) != 0 ? 21 : 1;
                S.Tiles[idx] = Gb(0xD5FD);
            }
        }

        /// 0x6BD6: integer square root by Newton's iteration, instruction by instruction as the ROM does it (a negative argument - the generator can produce one - wanders
        /// through the 16-bit division, including its overflow case, where the destination stays as it was).
        static int ISqrt(int wordValue)
        {
            int d0 = (short)wordValue, d1 = d0;
            d0 >>= 1;
            for (int guard = 0; guard < 100000; guard++)
            {
                int w0 = d0 & 0xFFFF;
                if (w0 == 0) return d0;
                uint dividend = (uint)((long)w0 * w0) + (uint)d1 + (uint)d0;
                uint quo = dividend / (uint)w0;
                int d2 = quo > 0xFFFF ? (int)dividend : (int)(((dividend % (uint)w0) << 16) | quo);
                int low = (short)(d2 & 0xFFFF) >> 1;                                           // asr.w #1
                if ((low & 0xFFFF) == w0) return d0;
                d0 = (short)low;
            }
            throw new InvalidOperationException("square root does not converge");
        }

        /// 0x14C2C: may a feature go on cell (x,y): the cell must still be plain ground (tile 8 / 9 outdoors, 0x18 / 0x16 / 0x17 indoors).
        bool CellFree(int type, int x, int y)
        {
            if (x < 0 || x >= 21 || y < 0 || y >= 21) return false;
            int t = S.Tiles[y * 21 + x];
            if (type < 0xB) return t == 8 || t == 9;
            return t == 0x18 || t == 0x16 || t == 0x17;
        }

        /// 0x14BD4: can the feature be stamped at (x,y): all the cells it covers must be free.
        bool CanStamp(int type, int row, int x, int y)
        {
            bool ok = true;
            if (Gb(0xD5F9) != 0)
            {
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) ok &= CellFree(type, x + dx, y + dy);
                return ok;
            }
            ok &= CellFree(type, x, y);
            if (Gb(0xD5FD) != 0)
            {
                if ((Rom.Byte(row + 6) & 1) != 0) y++; else x++;
                ok &= CellFree(type, x, y);
            }
            return ok;
        }

        // ------------------------------------------------------------------------------------ 0x14738: placing the sides
        /// 0x14738: every creature is unplaced (x = 0xFF, no target); the party is placed around its start cell facing [0x9AFA]; the monsters come from the opposite side (or from
        /// each direction whose bit is set in [0xD8CC], sharing the monsters between them); creatures that found no room are marked as not taking part (status bit 6).
        public void DeployAll()
        {
            T("deploy");
            for (int i = 0; i < S.SlotCount; i++) { S.Slots[i][0x12] = 0xFF; S.Slots[i][0x17] = 0xFF; }
            DeploySide(0, Facing & 3, 1, 1);
            int groups = 0;
            if (GroupMask == 0)
                DeploySide(Gb(0xD4FE), (Facing + 2) & 3, 0, 1);
            else
            {
                for (int k = 0; k < 4; k++) if (((GroupMask >> k) & 1) != 0) groups++;
                for (int k = 0; k < 4; k++)
                {
                    if (((GroupMask >> k) & 1) == 0) continue;
                    int dir = (k + 2) & 3, spread = Gb(0xD4FE);
                    if (spread == 0 && dir != ((Facing + 2) & 3)) spread = 1;
                    DeploySide(spread, dir, 0, groups);
                    groups--;
                }
            }
            for (int i = 0; i < S.SlotCount; i++)
                if ((sbyte)S.Slots[i][0x12] < 0 && S.Slots[i][0] != 0) S.Slots[i][0] |= 0x40;
        }

        /// 0x147EE: place the creatures of side `side` (1 party): the first goes on the start cell for direction `dir`, shifted back by `spread` steps, the next ones on the free cells
        /// found by a breadth-first search from there (the neighbour pattern depends on the direction and the formation). With `groups` > 1 only every groups-th unplaced creature is taken.
        void DeploySide(int spread, int dir, int side, int groups)
        {
            int rowOff = 0x14700 + dir;
            int pat = 0x14710 + dir * 5 + (WideFormation != 0 ? 0x14 : 0);
            int d2 = Rom.Byte(rowOff + 8), d3 = Rom.Byte(rowOff + 0xC);
            int x0 = (OriginX + Rom.Byte(rowOff)) & 0xFF, y0 = (OriginY + Rom.Byte(rowOff + 4)) & 0xFF;
            if (WideFormation != 0) d2 = (d2 + d3) & 0xFF;
            int mx = (short)(sbyte)d2 * (short)spread, my = (short)(sbyte)d3 * (short)spread;
            x0 = (x0 - mx) & 0xFF; y0 = (y0 - my) & 0xFF;
            var queue = new byte[512]; int head = 0, tail = 0;
            queue[tail++] = (byte)x0; queue[tail++] = (byte)y0;
            int counter = 6, count = 0, stride = groups & 0xFFFF;
            int left = groups & 0xFFFF;
            while (true)
            {
                int slotIndex = -1;                                                                       // 0x14874: the next creature to place
                while (true)
                {
                    count++;
                    if (count > S.SlotCount) break;
                    var sl = S.Slots[count - 1];
                    if (sl[0] == 0 || (sl[0] & 0x40) != 0 || (sbyte)sl[0x12] >= 0 || (sl[1] & 1) != side) continue;
                    left = (left - 1) & 0xFFFF;
                    if (left != 0) continue;
                    left = stride; slotIndex = count - 1; break;
                }
                if (slotIndex < 0) return;
                var cr = S.Slots[slotIndex];
                int type = S.Records[cr[2]][0x23];
                // 0x148B8: find it a cell
                int facing = (dir * 2) & 0xFF;
                bool placed = false;
                while (true)
                {
                    if (counter != 0) counter--;
                    if (head == tail) return;
                    int x = (sbyte)queue[head], y = (sbyte)queue[head + 1];
                    head = (head + 2) & 511;
                    int idx = (short)((y * 21 + x) & 0xFFFF);
                    bool expand = true, here = false;
                    int tile = idx >= 0 && idx < 441 ? S.Tiles[idx] : 0;
                    if ((tile & 0x80) != 0) { if (counter == 0) continue; }
                    else
                    {
                        int fl = TerrainByte(tile);
                        if ((fl & 0x20) != 0) { if (counter == 0) continue; }
                        else if ((fl & 0x3F) < 4)
                        {
                            bool ok = true;
                            if (type == 2 || type == 3)
                            {
                                int j = idx + (type == 2 ? 21 : 1);
                                int t2 = j >= 0 && j < 441 ? S.Tiles[j] : 0;
                                if ((sbyte)t2 < 0 || (TerrainByte(t2) & 0x3F) >= 4) ok = false;
                            }
                            if (ok)
                            {
                                cr[0x10] = (byte)facing; cr[0x12] = (byte)x; cr[0x13] = (byte)y;
                                S.SetMarkers(slotIndex);
                                here = true;
                            }
                        }
                    }
                    if (expand)
                        for (int k = 0; k < 5; k++)
                        {
                            int n = Rom.Byte(pat + k);
                            int nx = (Rom.Byte(0x146E0 + n) + x) & 0xFF, ny = (Rom.Byte(0x146E0 + 0xB + n) + y) & 0xFF;
                            if (nx >= 21 || ny >= 21) continue;
                            queue[tail] = (byte)nx; queue[tail + 1] = (byte)ny; tail = (tail + 2) & 511;
                        }
                    if (here) { placed = true; break; }
                }
                if (!placed) return;
            }
        }

        // ------------------------------------------------------------------------------------ 0x14DF6: who commands whom
        /// 0x14DF6: every living creature gets its control flag (slot +1 bit 7: set = the computer plays it): party members that are not allies (bit 6) are the player's, the rest the computer's.
        /// Then each of the player's party members in turn may take command of every ally (party, bit 6, still computer controlled, not a large creature and without effect 3) by passing a
        /// skill-2 check; the first one to pass does (the ally's bit 7 is cleared). If anybody did, "press C to continue" waits.
        public void AssignControl()
        {
            T("assign");
            bool any = false;
            for (int i = 0; i < S.SlotCount; i++)
            {
                var sl = S.Slots[i];
                if (sl[0] == 0 || (sl[0] & 0xC0) != 0) continue;
                if ((sl[1] & 1) != 0 && (sl[1] & 0x40) == 0) sl[1] &= 0x7F; else sl[1] |= 0x80;
            }
            for (int d6 = 0; d6 < S.SlotCount; d6++)
            {
                var lead = S.Slots[d6];
                if (lead[0] == 0 || (lead[0] & 0xC0) != 0 || (lead[1] & 1) == 0 || (lead[1] & 0x40) != 0) continue;
                var rec = S.Records[d6];
                for (int d5 = 0; d5 < S.SlotCount; d5++)
                {
                    var al = S.Slots[d5];
                    if (al[0] == 0) continue;
                    if (S.Records[al[2]][0x23] >= 2) continue;
                    if ((al[1] & 0xC1) != 0xC1) continue;
                    if (HasEffect(d5, 3)) continue;
                    if (GenesisSkills.SkillCheck(Rom, Rng, rec, lead[0], 2, 2) < 2) continue;
                    al[1] &= 0x7F; any = true;
                }
            }
            if (any)
            {
                Gs(0xD593, 0);                                                                         // 0x14246: the "press C to continue" box
                if (ContinuePrompt != null) ContinuePrompt(); else if (Pad != null) ChoicePrompt(2);
            }
        }

        // ------------------------------------------------------------------------------------ 0x1503C: the start of a fight
        
        /// 0x1503C: the game mode becomes 2 (fight), the per-fight flags are reset, the battlefield is made (ground generated for the area type, or the host's arena for screen mode 6),
        /// the sides are deployed and counted; if both sides have creatures the fight is on: map markers are built, the round counter is 0, the peace counter 3, the recruits take command
        /// of the allies and the timed effects and lingering patches are cleared. Returns whether a fight is on ([0xD50E] set).
        public bool CombatSetup()
        {
            T("setup");
            PrevMode = (byte)S.CombatMode; S.CombatMode = 2;
            Gs(0xD509, 0); Gs(0xD50E, 0); Gs(0xD50A, 0);
            Gs(0xD4FE, Ambush);
            if (GroupMask != 0) Gs(0xD4FE, 1);
            if (Gb(0xD4FE) != 0 && GroupMask == 0) { OriginX = Rom.Byte(0x146F6 + (Facing & 0xFF)); OriginY = Rom.Byte(0x146F6 + (Facing & 0xFF) + 5); }
            else { OriginX = Rom.Byte(0x146FA); OriginY = Rom.Byte(0x146FF); }
            int kind = D97DC == 0xA2 ? 1 : D97DC == 0xA8 ? 2 : 3;                                   // 0x82AC: the kind of area (the screen mode)
            ScreenMode = kind + 3;
            if (ScreenMode == 6) { TerrainTable = 0x3297; ScriptTable = 0; BuildDungeonArena(); }
            else if (ScreenMode == 4) { GenerateBattlefield(AreaType); TerrainTable = 0x31F5; ScriptTable = 0x325A; }
            else { GenerateBattlefield(AreaType + 0xB); TerrainTable = 0x31CB; ScriptTable = 0x3231; }
            int table = TerrainTable;
            TerrainFlags = i => Rom.Byte(table + i);                                                  // [0xD810]: the terrain flags of the area (tile 0 / 1 read the byte before the table)
            int script = ScriptTable;
            TileScript = script == 0 ? (Func<int, int>)null : i => Rom.Byte(script + i);              // [0xD814]: the tile transformation chains of fire / acid blasts
            if (SoloFlag != 0)
                for (int i = 0; i < 8 && i < S.SlotCount; i++) if (i != SoloMember) S.Slots[i][0] |= 0x40;
            DeployAll();
            CountLiving();
            if (S.LivingBySide[1] == 0 || S.LivingBySide[0] == 0) return false;
            S.RebuildMarkers();
            Gs(0xD509, 0xFF); Gs(0xD50E, 0xFF); Gs(0xD50C, 0); Gs(0xD50D, 3);
            AssignControl();
            for (int i = 0xD49C; i < 0xD49C + 0x60; i++) Gs(i, 0);
            Array.Clear(Haz, 0, 256);
            CursorX = 0; CursorY = 0;                                                                // 0x15222
            ScreenMode = 0;                                                                          // 0x1519E
            return true;
        }

        /// 0xE394: a whole fight from the start: setup, the memory of each creature's last victim ([0xD51D..]) forgotten, then the rounds (CombatRounds). The clean-up after the
        /// fight (0x15FDA: experience, treasure, leftover statuses) is the host's.
        public void RunCombat()
        {
            T("combat");
            CombatSetup();
            for (int i = 0; i < 0x40; i++) Gs(0xD51D + i, 0xFF);
            CombatRounds();
        }
    }
}
