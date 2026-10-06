// GenesisActions.cs -- creature actions of the Genesis combat engine, ported from the ROM and verified against it
// (port/tests/ActionTests.cs; vectors from the real routines run in a 68000 emulator, see port/tools/gen_action_vectors.py).
//
// Combat state mirrors the RAM layout: 26-byte slots (party 0-7, monsters after), a 21x21 tile map whose bit 7 marks cells occupied by a creature,
// and the record type byte (+0x23: 2 = tall, 3 = wide) of each monster/character record.
//   Slot bytes used here: +0 status, +1 flags (bit 0 party), +2 record index, +0x0E HP, +0x0F movement, +0x10 facing, +0x12 x, +0x13 y, +0x14 initiative,
//   +0x16 movement points left, +0x17 current target.
//   Status (+0): 0 empty, 1 active, 2 dead, 3 dying, 4 unconscious, 7 badly wounded; bit 7 set = down/out of the fight.

using System;

namespace BuckRogersGenesis
{
    public sealed class CombatState
    {
        public const int MapSize = 21;
        public byte[][] Slots;                       // 26 bytes each
        public byte[][] Records;                     // 214-byte character/monster records by record index (party 0-7, monster type copies 8..10)
        public int SlotCount;
        public byte[] Tiles = new byte[MapSize * MapSize];   // 0xCACA
        public int[] RecordSizeType = new int[11];   // record +0x23 per record index
        public int[] LivingBySide = new int[2];      // [0xD8CA], [0xD8CB]: living creatures per side (indexed by slot flags bit 0)
        public int CombatMode;                       // [0x9BBC]: 2 = a normal fight (living counts are maintained and markers cleared on death)

        int SizeOf(int slot) { return RecordSizeType[Slots[slot][2]]; }

        // the ROM does not bounds-check: a wide/tall creature on the last column/row touches the byte after the map (unrelated RAM); ignored here
        void Mark(int index, bool on) { if (index >= 0 && index < Tiles.Length) Tiles[index] = (byte)(on ? (Tiles[index] | 0x80) : (Tiles[index] & 0x7F)); }

        // ------------------------------------------------------------------------------------ markers 0x14254 / 0x142A8
        /// 0x142A8: clears the occupied bit of the cells a creature covers (its own, plus the east cell if wide, the south cell if tall).
        public void ClearMarkers(int slot)
        {
            int x = Slots[slot][0x12], y = Slots[slot][0x13];
            Mark(y * MapSize + x, false);
            int t = SizeOf(slot);
            if (t == 3) Mark(y * MapSize + x + 1, false);
            else if (t == 2) Mark((y + 1) * MapSize + x, false);
        }

        /// 0x14254: clears every marker, then marks the cells of all living creatures (status != 0, not down, flag bit 2 clear).
        public void RebuildMarkers()
        {
            for (int i = 0; i < Tiles.Length; i++) Tiles[i] &= 0x7F;
            for (int s = 0; s < SlotCount; s++)
            {
                int f = Slots[s][0];
                if (f == 0 || (f & 0xC0) != 0 || (Slots[s][1] & 4) != 0) continue;
                int x = Slots[s][0x12], y = Slots[s][0x13];
                Mark(y * MapSize + x, true);
                int t = SizeOf(s);
                if (t == 3) Mark(y * MapSize + x + 1, true);
                else if (t == 2) Mark((y + 1) * MapSize + x, true);
            }
        }

        // ------------------------------------------------------------------------------------ wound model 0x760A
        /// Applies `damage` (a byte) to a creature. Damage below its HP just subtracts. Otherwise HP becomes 0 and the overflow decides the status:
        ///   exactly 0 -> unconscious (4);  1..9 -> dying (3) with the overflow kept in HP;  10..24 -> badly wounded (7);  25+ -> dead (2);
        /// the down bit 0x80 is set in every case. In a normal fight (mode 2) the side's living counter drops and the creature's map markers are cleared.
        /// A creature that is already down takes nothing.
        public void ApplyDamage(int victim, int damage)
        {
            var s = Slots[victim];
            if ((s[0] & 0x80) != 0) return;
            int d0 = damage & 0xFF, hp = s[0xE];
            if (d0 < hp) { s[0xE] = (byte)(hp - d0); return; }
            s[0xE] = 0;
            d0 -= hp;
            if (d0 == 0) s[0] = 4;
            else if (d0 < 10) { s[0xE] = (byte)d0; s[0] = 3; }
            else if (d0 < 0x19) s[0] = 7;
            else s[0] = 2;
            s[0] |= 0x80;
            if (CombatMode == 2)
            {
                int side = s[1] & 1;
                LivingBySide[side] = (LivingBySide[side] - 1) & 0xFF;
                ClearMarkers(victim);
            }
        }

        /// <summary>
        /// ROM 0x10E3E: applies one entry of the attack's damage list (0xD48E[idx]) to the victim while the attack animation plays.
        /// The entry byte is tested as a SIGNED byte (move.b + ble): 0 and anything 128..255 do nothing to HP. That also covers the 0xFF "full damage"
        /// marker the rocket weakness special stores: it only changes the projectile animation. Only a living victim (status byte non-zero, bits 6/7 clear) is hit.
        /// <paramref name="cue"/> reports that the hit sound (0x1B900 with 1) was requested; <paramref name="applied"/> that HP was processed.
        /// </summary>
        public void ApplyDamageEntry(int victim, byte[] list, int idx, int count, bool melee, out bool cue, out bool applied)
        {
            cue = false; applied = false;
            if (count == 0) return;
            if (!melee) cue = true;
            int e = (sbyte)list[idx];
            if (e <= 0 || victim < 0) return;
            int st = Slots[victim][0];
            if (st == 0 || (st & 0xC0) != 0) return;
            ApplyDamage(victim, e);
            applied = true;
        }
    }

    public static class GenesisLineOfFire
    {
        public struct Trace
        {
            public bool Clear;          // d0: the target was reached within range with nothing blocking
            public int Distance;        // d1: (half-steps travelled) / 2
            public int LastX, LastY;    // d2, d3: the last cell reached
            public bool Flag501;        // [0xD501]: the path crossed a tile with id 0
            public bool Flag502;        // [0xD502]: the path crossed a tile with id 1
            public bool Flag503;        // [0xD503]: after the first two ordinary tiles, a tile whose terrain flags have bit 7 set was crossed (to-hit penalty 2)
        }

        /// ROM 0x15B5A. Walks a Bresenham line from (x0,y0) to (x1,y1) over the 21x21 tile map. Distance is counted in half-steps: 2 per straight step, 3 per diagonal
        /// (so range r allows 2r+1). terrainFlags(tile id) = the flag byte of the terrain table ([0xD810] + (id - 2), -1 for ids 0/1). `noTileZeroStop` = [0xD504] (when set a
        /// tile with id 0 ends the trace as blocked); `skipBlockers` = [0xD4FF] (blocking terrain, flag 0x40, is ignored when set).
        public static Trace Run(byte[] tiles, Func<int, int> terrainFlags, int x0, int y0, int x1, int y1, int range, bool tileZeroBlocks, bool skipBlockers)
        {
            var r = new Trace();
            int d7 = 2, d5 = (range * 2 + 1) & 0xFFFF;
            int curX = x0, curY = y0;
            int adx = Math.Abs(x1 - x0), ady = Math.Abs(y1 - y0), sx = Math.Sign(x1 - x0), sy = Math.Sign(y1 - y0);
            int err = 0, steps = 0;
            bool ok;
            while (true)
            {
                int ti = curY * 21 + curX;                                  // a trace towards row 21 reads the RAM behind the map: 0 here
                int tile = (ti >= 0 && ti < tiles.Length ? tiles[ti] : 0) & 0x7F;
                if (tile == 0) { r.Flag501 = true; if (tileZeroBlocks) { ok = false; break; } }
                else if (tile == 1) r.Flag502 = true;
                else if (d7 != 0) d7--;
                else if ((terrainFlags(TerrainIndex(tile)) & 0x80) != 0) r.Flag503 = true;
                if ((d5 & 0xFF) < (steps & 0xFF)) { ok = false; break; }                      // cmp.b $16(a2),d5 ; bcs
                if (!skipBlockers && (terrainFlags(TerrainIndex(tile)) & 0x40) != 0 && !(x0 == x1 && y0 == y1)) { ok = false; break; }
                // one Bresenham step (0x15A4E)
                if (adx >= ady)
                {
                    if (curX == x1) { ok = true; break; }
                    curX += sx; err += 2 * ady; steps += 2;
                    if (err >= adx) { err -= 2 * adx; curY += sy; steps += 1; }
                }
                else
                {
                    if (curY == y1) { ok = true; break; }
                    curY += sy; err += 2 * adx; steps += 2;
                    if (err >= ady) { err -= 2 * ady; curX += sx; steps += 1; }
                }
            }
            r.Clear = ok; r.Distance = (sbyte)(steps & 0xFF) >> 1; r.LastX = curX; r.LastY = curY;
            return r;
        }

        static int TerrainIndex(int tile) { int d0 = ((tile & 0x7F) - 2) & 0xFF; return d0 >= 0x80 ? -1 : d0; }
    }
}

namespace BuckRogersGenesis
{
    /// Derivation of a combat slot's stats from the character/monster record and its gear (ROM 0x6D1E and helpers 0x6E70-0x6EBC).
    public static class GenesisStats
    {
        public const int WeaponTable = 0x779E, StrDexTable = 0x7741, DexTable = 0x776F;

        /// Recomputes `slot` (26 bytes) from `record` (214 bytes), exactly like the ROM:
        ///   slot +6..+9 / +10..+13 start as the record's natural attacks (+0x26..+0x2D), +3 = record +0x22 (attack value), record +0x2F is rebuilt:
        ///       bit 0 = carries a class-3 item, bit 1 = carries a class-7 item (gear scan), bits 1+2 = wears battle armor (item 0x18)
        ///   armed (record +0xAE): +6 attacks x2, +8 dice count, +10 dice sides, +12 bonus from the weapon table; melee weapons (type 0) add the STR tables (0x7741) to attack
        ///       value (+3) and damage bonus (+12), missile weapons add the DEX table (0x776F) to +3; then weapon specialisation (how many of the five ids at record +0x4D..+0x51
        ///       equal the weapon id) + the weapon's modifier byte (+4) are added to both +3 and +12
        ///   unarmed: STR tables add to +3 and +12
        ///   side modifier [0xD49A] (party, slot flag bit 0) or [0xD499] (monsters) is added to +3
        ///   +4 armor = record +0x25 + (worn armor type - 50 + its modifier); +5 = +4 - 2 (taken before the DEX bonus), then +4 += DEX armor bonus
        ///   +0x0F movement = record +0x24, minus (items - 6) when carrying more than six, at least 3
        ///   slot flag bit 5 (set by the defensive-posture check) costs 2 attack value and 2 armor
        /// mode97AE = [0x97AE] != 0 skips the gear scan and armor for party members (record index < 8).
        public static void RecomputeSlot(RomView rom, byte[] slot, byte[] record, int sideModParty, int sideModMonster, bool mode97AE)
        {
            int d5 = 0;                                            // item count
            slot[6] = record[0x26]; slot[7] = record[0x27]; slot[8] = record[0x28]; slot[9] = record[0x29];
            slot[10] = record[0x2A]; slot[11] = record[0x2B]; slot[12] = record[0x2C]; slot[13] = record[0x2D];
            slot[3] = record[0x22];
            record[0x2F] = 0;
            int d6 = slot[2];
            bool scan = !mode97AE || d6 >= 8;
            if (scan)
            {
                for (int d4 = 0; d4 < 13; d4++)
                {
                    int id = record[0x54 + 10 * d4];
                    if (id == 0) continue;
                    d5++;
                    int cls = rom.Byte(WeaponTable + 8 * (sbyte)id);        // ext.w: ids >= 0x80 index backwards
                    if (cls == 3) record[0x2F] |= 1;
                    else if (cls == 7) record[0x2F] |= 2;
                }
            }
            int weapon = record[0xAE];
            if (!scan || weapon == 0)
            {
                // unarmed: STR bonuses
                Str(rom, record, out int a, out int b);
                slot[3] = (byte)(slot[3] + a); slot[12] = (byte)(slot[12] + b);
            }
            else
            {
                int row = WeaponTable + 8 * (sbyte)weapon;
                slot[6] = (byte)rom.Byte(row + 2); slot[8] = (byte)rom.Byte(row + 3); slot[10] = (byte)rom.Byte(row + 4); slot[12] = (byte)rom.Byte(row + 5);
                if (rom.Byte(row + 1) == 0)
                {
                    Str(rom, record, out int a, out int b);
                    slot[3] = (byte)(slot[3] + a); slot[12] = (byte)(slot[12] + b);
                }
                else
                {
                    Dex(rom, record, out int a, out int b);
                    slot[3] = (byte)(slot[3] + a);
                }
                int spec = 0;
                for (int k = 0; k < 5; k++) if (record[0x4D + k] == weapon) spec++;
                int bonus = (spec + record[0xAE + 4]) & 0xFF;
                slot[12] = (byte)(slot[12] + bonus); slot[3] = (byte)(slot[3] + bonus);
            }
            slot[3] = (byte)(slot[3] + (((slot[1] & 1) != 0) ? sideModParty : sideModMonster));
            slot[4] = record[0x25];
            if (scan && record[0xC2] != 0)
            {
                int armor = rom.Byte(WeaponTable + 8 * (sbyte)record[0xC2] + 1);
                int d0 = (armor - 0x32 + record[0xC2 + 4]) & 0xFF;
                slot[4] = (byte)(slot[4] + d0);
                if (record[0xC2] == 0x18) record[0x2F] |= 6;
            }
            int move = record[0x24];
            int over = d5 - 6;
            if (over >= 0)
            {
                move = (move - over) & 0xFF;
                if ((sbyte)move < 3) move = 3;
            }
            slot[0xF] = (byte)move;
            if ((slot[1] & 0x20) != 0) { slot[3] = (byte)(slot[3] - 2); slot[4] = (byte)(slot[4] - 2); }
            slot[5] = (byte)(slot[4] - 2);
            Dex(rom, record, out int _, out int dexArmor);
            slot[4] = (byte)(slot[4] + dexArmor);
        }

        // 0x6E9E: STR tables: d1 = damage bonus, d0 = attack bonus (indices by the signed STR byte, record +0x10)
        static void Str(RomView rom, byte[] record, out int attack, out int damage)
        {
            int i = (sbyte)record[0x10];
            damage = (sbyte)rom.Byte(StrDexTable + i); attack = (sbyte)rom.Byte(StrDexTable + 0x17 + i);
        }

        // 0x6EBC: DEX tables: d1 = armor bonus, d0 = attack bonus (record +0x11)
        static void Dex(RomView rom, byte[] record, out int attack, out int armor)
        {
            int i = (sbyte)record[0x11];
            armor = (sbyte)rom.Byte(DexTable + i); attack = (sbyte)rom.Byte(DexTable + 0x17 + i);
        }
    }
}

namespace BuckRogersGenesis
{
    /// Everything the attack-preparation routine reads from RAM besides the combat state.
    public sealed class AttackEnv
    {
        public CombatState State;
        public RomView Rom;
        public Func<int, int> TerrainFlags;          // terrain flag byte by (tile id - 2), -1 for ids 0/1
        public int Mode97AE;                         // [0x97AE]
        public int SkipBlockers;                     // [0xD4FF]
        public int SideModMonster, SideModParty;     // [0xD499], [0xD49A]
        public int BackstabMask;                     // [0xD4FD]
        public int CursorX, CursorY;                 // [0xB3F0], [0xB3F2]: pixel position of the targeting cursor (area attacks)
        public int DamageMultiplier;                 // [0xD496]: in/out
    }

    public struct AttackPlan
    {
        public int ToHit;           // [0xD511]: 1..19 (x5 = percent), 0 = the attack cannot be made
        public int Message;         // [0xD518]: 0 none, 0x128 flank, 0x129 rear, 0x12A backstab, 0x0C cover penalty, 0xBA line blocked, 0xBB out of range,
                                    //           0xBC same side, 0xBD / 0xBE weapon refuses (flags), 0x12B item cannot be used
        public int Distance;        // [0xD512] (valid when TailReached)
        public bool TailReached;
        public int Target;          // [0xD513] after the call (0xFF for an area attack)
    }

    /// ROM 0x10400: everything the engine works out before an attack: who is attacked, the armor by facing, both creatures' stats from their gear,
    /// range, line of fire (a second try towards the other cell of a tall/wide target) and the final to-hit.  No random numbers.
    public static class GenesisAttackPlanner
    {
        public static AttackPlan Prepare(AttackEnv env, int attacker, int target)
        {
            var plan = new AttackPlan { Target = target & 0xFF };
            var st = env.State; var rom = env.Rom;
            var att = st.Slots[attacker]; var attRec = st.Records[att[2]];
            Func<int, int> weaponTypeOf = id => rom.Byte(GenesisStats.WeaponTable + 8 * (sbyte)id + 1);

            // 0x105FC: an attacker holding a grenade-class item (modifier 5..12) makes an area attack at the cursor
            bool grenade = false;
            if (!(env.Mode97AE != 0 && att[2] < 8) && attRec[0xAE] != 0) { int mod = attRec[0xAE + 9]; grenade = mod >= 5 && mod <= 12; }

            int tx, ty, armor, tflags;
            int tIndex = target & 0xFF;
            if (grenade)
            {
                if ((attRec[0xAE + 5] & 0x30) != 0) { plan.Message = 0x12B; return plan; }
                armor = 0x32; tflags = 0; plan.Target = 0xFF; tIndex = 0xFF;
                tx = (env.CursorX & 0xFFFF) / 24; ty = (env.CursorY & 0xFFFF) / 24;
            }
            else
            {
                if (tIndex >= 0x80) return plan;
                var tSlot = st.Slots[tIndex]; var tRec = st.Records[tSlot[2]];
                tx = tSlot[0x12]; ty = tSlot[0x13];
                GenesisStats.RecomputeSlot(rom, tSlot, tRec, env.SideModParty, env.SideModMonster, env.Mode97AE != 0);
                int bearing = GenesisCombat.Octant(att[0x12], att[0x13], tx, ty);
                var ar = GenesisCombat.ArmorAgainst(bearing, att[1], tSlot[1], tSlot[0x10], tSlot[4], tSlot[5], attRec[0x18], attRec[0x19], attacker,
                    env.BackstabMask, attRec[0xAE], weaponTypeOf);
                if (ar.TargetTurned) tSlot[0x10] = (byte)ar.TargetFacing;
                env.DamageMultiplier = ar.DamageMultiplier;
                if (ar.Message != -1) plan.Message = ar.Message;
                armor = ar.Armor; tflags = tSlot[1];
            }
            GenesisStats.RecomputeSlot(rom, att, attRec, env.SideModParty, env.SideModMonster, env.Mode97AE != 0);
            int attack = att[3];
            if ((((tflags ^ att[1]) & 1)) == 0) { plan.Message = 0xBC; return plan; }

            int range = WeaponRange(env, attacker, attRec);
            int d5 = 0, d7 = 0;
            if (!(env.Mode97AE != 0 && attacker < 8) && attRec[0xAE] != 0) { d7 = attRec[0xAE + 9]; d5 = weaponTypeOf(attRec[0xAE]); }

            var lof = GenesisLineOfFire.Run(st.Tiles, env.TerrainFlags, att[0x12], att[0x13], tx, ty, range, false, env.SkipBlockers != 0);
            if (!lof.Clear)
            {
                var last = lof;
                bool second = false; int x2 = 0, y2 = 0;
                if (tIndex < 0x80)
                {
                    int tType = st.Records[st.Slots[tIndex][2]][0x23];
                    if (tType >= 2) { x2 = tx; y2 = ty; if (tType == 2) y2++; else x2++; second = true; }
                }
                if (second)
                {
                    var lof2 = GenesisLineOfFire.Run(st.Tiles, env.TerrainFlags, att[0x12], att[0x13], x2, y2, range, false, env.SkipBlockers != 0);
                    if (lof2.Clear) { lof = lof2; goto tail; }
                    last = lof2;
                }
                if ((range & 0xFF) == 1 || (last.Distance & 0xFF) >= (range & 0xFF)) plan.Message = 0xBB; else plan.Message = 0xBA;
                return plan;
            }
        tail:
            var th = GenesisCombat.ToHit(attack, armor, lof.Distance, range, d5, d7, lof.Flag501 ? 1 : 0, lof.Flag502 ? 1 : 0, lof.Flag503 ? 1 : 0);
            plan.TailReached = true; plan.Distance = lof.Distance & 0xFF;
            if (th.Message != -1) plan.Message = th.Message;
            if (th.Computed) plan.ToHit = th.Value;
            return plan;
        }

        /// ROM 0x11638: range in cells of the attacker's primary weapon (weapon table +7; 1 for no weapon / unlimited markers); throwing-class weapons
        /// (modifier 5..12 except 10, 11) reach 12 cells when the attacker carries item 0x10 (grenade launcher).
        public static int WeaponRange(AttackEnv env, int attacker, byte[] attRec)
        {
            if (env.Mode97AE != 0 && attacker < 8) return 1;
            int id = attRec[0xAE];
            if (id == 0) return 1;
            int d2 = env.Rom.Byte(GenesisStats.WeaponTable + 8 * (sbyte)id + 7);
            if (d2 == 0 || d2 == 0xFF) return 1;
            int mod = attRec[0xAE + 9];
            if (mod >= 5 && mod <= 12 && mod != 10 && mod != 11)
            {
                bool partyRecord = env.State.Slots[attacker][2] < 8;      // 0x76FA: with [0x97AE] set only monster records are searched
                if (!(env.Mode97AE != 0 && partyRecord))
                    for (int k = 0; k < 13; k++) if (attRec[0x54 + 10 * k] == 0x10) { d2 = 12; break; }
            }
            return d2;
        }
    }
}
