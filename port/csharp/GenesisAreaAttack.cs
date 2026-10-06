// GenesisAreaAttack.cs -- the explosive attack (ROM 0x10FAA with 0x1137E scatter and 0x113EC terrain / reflection search) and the choice of the blast cell (0xEB50).
using System;
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        /// 0x15C54: like EnumerateTargets, but around a map cell instead of a creature: every living creature (side filter [0xD500]) with a line of fire from the cell within `range`.
        public void EnumerateAround(int cx, int cy, int range)
        {
            T("enumAround");
            D506 = 0;
            for (int cand = 0; cand < S.SlotCount; cand++)
            {
                int st = S.Slots[cand][0];
                if (st == 0 || (st & 0xC0) != 0) continue;
                if ((D500 & 0x80) == 0 && (((S.Slots[cand][1] ^ D500) & 1) != 0)) continue;
                foreach (var cc in Cells(cand))
                {
                    var t = Trace(cx, cy, cc.x, cc.y, range);
                    if (!t.Clear) continue;
                    int n = D506, k;
                    for (k = 0; k < n; k++) if (Ca[2 + 3 * k] == cand) break;
                    if (k < n) { if ((byte)t.Distance < Ca[2 + 3 * k + 1]) Ca[2 + 3 * k + 1] = (byte)t.Distance; continue; }
                    Ca[2 + 3 * n] = (byte)cand; Ca[2 + 3 * n + 1] = (byte)t.Distance;
                    Ca[2 + 3 * n + 2] = (byte)GenesisCombat.Octant(cc.x, cc.y, cx, cy);
                    D506 = (byte)(n + 1);
                }
            }
            SortTargets();
            D500 = 0xFF;
        }

        /// 0x11630: range of the item at record offset `itemOff` in the current actor's hands (see WeaponRange).
        public int WeaponRangeOf(int itemOff)
        {
            var me = S.Slots[Actor];
            var env = new AttackEnv { State = S, Rom = Rom, Mode97AE = Mode97AE, RangeGarbage = NextRangeGarbage };
            return GenesisAttackPlanner.WeaponRange(env, Actor, S.Records[me[2]], itemOff);
        }

        // ------------------------------------------------------------------------------------ 0x1137E: a missed throw scatters
        void Scatter(int ax, int ay, ref int tx, ref int ty)
        {
            for (int tries = 0; tries < 4; tries++)
            {
                int nx = (tx + (((Rng.Roll(4)) >> 1) - 1)) & 0xFFFF;
                int ny = (ty + (((Rng.Roll(4)) >> 1) - 1)) & 0xFFFF;
                if (nx >= 21 || ny >= 21) continue;
                if (Trace(ax, ay, nx, ny, 4).Clear) { tx = nx; ty = ny; return; }
            }
        }

        // ------------------------------------------------------------------------------------ 0x113EC: terrain transformation and relocation spot
        /// For the 7x7 cells around the blast: where the target cell sees the cell (within the blast radius) its terrain is transformed along the chain of [0xD814];
        /// where it does not, and a creature is being moved out of the blast (`special`), free passable cells it can see within 5 are candidates: the nearest becomes
        /// the new spot (relocX/relocY). Finally the map markers are rebuilt.
        void ReflectSearch(int tx, int ty, int radius, bool special, int sx, int sy, ref int relocX, ref int relocY)
        {
            int best = 0xFF;
            for (int d6 = 6; d6 >= 0; d6--)
                for (int d7 = 6; d7 >= 0; d7--)
                {
                    int cx = (tx + d6 - 3) & 0xFFFF, cy = (ty + d7 - 3) & 0xFFFF;
                    if (cx >= 21 || cy >= 21) continue;
                    var t = Trace(tx, ty, cx, cy, radius);
                    if (t.Clear)
                    {
                        int tile = OriginalTile(cx, cy) & 0xFF;
                        int mark; int d2 = TileChain(tile, out mark);
                        if ((d2 & 0xFF) != tile) SetTile(cx, cy, d2);
                        // the ROM then tests the remembered byte as a WORD, which is always negative (0xFFxx): the follow-up SetTile below it is dead code
                    }
                    else if (special)
                    {
                        int occ = CellInfo(cx, cy, out int tile1);
                        if (occ < 0x80) continue;
                        if ((TerrainByte(tile1) & 0x20) != 0) continue;
                        var t2 = Trace(sx, sy, cx, cy, 5);
                        if (!t2.Clear) continue;
                        if ((byte)t2.Distance >= best) continue;
                        best = (byte)t2.Distance; relocX = t2.LastX & 0xFF; relocY = t2.LastY & 0xFF;
                    }
                }
            S.RebuildMarkers();
        }

        // ------------------------------------------------------------------------------------ 0x10FAA: the blast
        /// The current actor throws / fires its explosive item at the cell under the cursor ([0xB3F0],[0xB3F2] pixels / 24).
        /// Damage = the weapon's dice + bonus (types 5 and 12 do no damage but draw a 2..5 turn effect duration); a throw that misses (d20 above [0xD511])
        /// scatters to a neighbouring cell that is in sight; a blocked line stops at the last free cell. Types 6, 10, 11 first look for a party creature with effect 3,
        /// which gets moved out of the blast and cause terrain to transform. A type whose table entry has a patch flag (8, 9) leaves a 3x3 gas / fire patch on the map.
        /// Everyone in the blast radius with a line of fire takes the damage (saved: halved / none) and the status effect of the type; the item loses a charge.
        public void AreaAttack()
        {
            T("10faa");
            var me = S.Slots[Actor]; var rec = S.Records[me[2]];
            const int hand = 0xAE;
            int type = rec[hand + 9];
            int id = rec[hand];
            int te = ATypeTable + type * 5;
            int t3 = Tbl(id, 3), t4 = Tbl(id, 4), t5 = Tbl(id, 5);
            int sum = 0; for (int i = 0; i < t3; i++) sum = (sum + (Rng.Roll(t4) & 0xFFFF)) & 0xFFFF;
            int dmg = (sum + ((t5 + rec[hand + 4]) & 0xFF)) & 0xFF;
            int effParam = 0xFF;
            int tFx = Rom.Byte(te), tRadius = (sbyte)Rom.Byte(te + 1), tPatch = Rom.Byte(te + 2), tMode = Rom.Byte(te + 3);
            if (type == 12) { dmg = 0; effParam = (Rng.Roll(4) + 1) & 0xFF; }
            else if (type == 5) effParam = (Rng.Roll(4) + 1) & 0xFF;
            int tb = Tbl(id, 1);
            if (tb == 7 || tb == 9) rec[hand + 5] |= 0x20;
            int ax = me[0x12], ay = me[0x13];
            int tx = (CursorX & 0xFFFF) / 24, ty = (CursorY & 0xFFFF) / 24;
            if (Rng.Roll(20) > Gb(A511)) Scatter(ax, ay, ref tx, ref ty);
            D504 = 0xFF;
            var lof = Trace(ax, ay, tx, ty, 100);
            if (!lof.Clear) { tx = lof.LastX & 0xFFFF; ty = lof.LastY & 0xFFFF; }
            AnimationProbe();                                                                     // the projectile animation (0x108AA)
            int special = -1, specialSlot = 0, sx = 0xFFFF, sy = 0xFFFF, relocX = 0xFF, relocY = 0xFF;
            if (type == 6 || type == 10 || type == 11)
            {
                for (int d7 = 7; d7 >= 0; d7--)
                {
                    if (d7 >= S.SlotCount) continue;
                    var v = S.Slots[d7];
                    if (v[0] == 0 || (v[0] & 0xC0) != 0) continue;
                    if (!HasEffect(d7, 3)) continue;
                    special = d7; specialSlot = d7; sx = v[0x12]; sy = v[0x13];
                    break;
                }
                ReflectSearch(tx, ty, tRadius, special >= 0, sx, sy, ref relocX, ref relocY);
            }
            EnumerateAround(tx, ty, tRadius);
            if (special >= 0)
            {
                int n = D506;
                for (int i = 0; i < n; i++)
                {
                    if (Ca[2 + 3 * i] != specialSlot) continue;
                    Ca[2 + 3 * i] = 0xFF;
                    if ((sbyte)relocX < 0) break;
                    var sp = S.Slots[special];
                    sp[1] |= 4; S.ClearMarkers(special); Gs(A510, 0xFF);                        // 0xF9A6
                    AnimationProbe();                                                           // the relocation animation
                    sp[0x12] = (byte)relocX; sp[0x13] = (byte)relocY;
                    sp[1] &= 0xFB; S.SetMarkers(special);                                       // 0xFA22
                    break;
                }
            }
            if (tPatch != 0)
            {
                int o = 0;
                for (int k = 1; k < 16; k++) if (Haz[k * 16 + 0xE] < Haz[o + 0xE]) o = k * 16;
                if (Haz[o + 0xE] != 0) RemoveHazard(o / 16);
                SetHazXY(o, tx - 1, ty - 1);
                Haz[o + 0xF] = (byte)(type + 0xF8);
                int a3 = o + 4;
                for (int d7 = 2; d7 >= 0; d7--)
                    for (int d6 = 2; d6 >= 0; d6--)
                    {
                        int x = (tx - 1 + d6) & 0xFFFF, y = (ty - 1 + d7) & 0xFFFF;
                        if (x < 21 && y < 21)
                        {
                            int t = OriginalTile(x, y) & 0xFF;
                            if ((TerrainByte(t) & 0x20) != 0) Haz[a3++] = 0xFF;
                            else { Haz[a3++] = (byte)t; S.Tiles[y * 21 + x] = Haz[o + 0xF]; }
                        }
                        else Haz[a3++] = 0xFF;
                    }
                Haz[o + 0xE] = (byte)((D97DC & 0x10) != 0 ? 2 : 5);
                S.RebuildMarkers();
            }
            int count = D506;
            for (int i = 0; i < count; i++)
            {
                int v = Ca[2 + 3 * i];
                if ((sbyte)v < 0) continue;
                Gs(A513, v);
                var vs = S.Slots[v]; var vrec = S.Records[vs[2]];
                bool saved = (type == 5 && (vrec[0x2F] & 1) != 0) ? true : SavingThrow(v, 0);
                if (dmg != 0) AreaDamage(v, dmg, saved, tMode);
                if (vs[0] != 0 && (vs[0] & 0xC0) == 0 && tFx != 0) ApplyEffect(v, tFx, effParam, saved, tMode);
            }
            me[0x14] = 0;                                                                        // the throw spends the action time
            int uses = rec[hand + 8];
            if (uses != 0)
            {
                bool consume = Actor < 8 || Rng.Next(rec[0x3F]) == 0;
                if (consume)
                {
                    uses--;
                    if (uses == 0) rec[hand] = 0;
                    rec[hand + 8] = (byte)uses;
                }
            }
        }
    }
}
