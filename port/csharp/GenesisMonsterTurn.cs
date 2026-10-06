// GenesisMonsterTurn.cs -- attacks, movement and the turn controller of a computer-controlled creature (ROM 0x10400/0x1074A, 0xF898, 0x11A44, 0xEF64 ...).
// Verified against the ROM in port/tests/MonsterTests.cs. Graphics, sound and animation routines are not part of the port: in the ROM test they are replaced by
// empty routines, and the code below assumes they have no effect on the game state.
using System;
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        // addresses inside G
        const int A496 = 0xD496, A497 = 0xD497, A499 = 0xD499, A49A = 0xD49A, A4FC = 0xD4FC, A4FD = 0xD4FD, A50C = 0xD50C, A50D = 0xD50D, A510 = 0xD510,
                  A511 = 0xD511, A512 = 0xD512, A513 = 0xD513, A518 = 0xD518, A5D6 = 0xD5D6, A5F8 = 0xD5F8;
        byte Gb(int a) { return G[a - GBase]; }
        void Gs(int a, int v) { G[a - GBase] = (byte)v; }

        public int MoveDx, MoveDy;          // [0xB3F4], [0xB3F6]: the step being taken (words)
        public int CursorX, CursorY;        // [0xB3F0], [0xB3F2]: pixel position used by area attacks
        /// ROM 0x136DA(7): the "leave the battlefield?" prompt of a step off the map for a creature without the auto flag; nonzero = cancel. Default: cancel.
        public Func<int> RetreatPrompt;

        static readonly int[] FacingByStep = { 7, 0, 1, 8, 6, 8, 2, 8, 5, 4, 3, 8 };    // 0xF97A.. indexed by 4*dy + dx + 5; 8 = no step

        // ------------------------------------------------------------------------------------ small helpers
        /// 0x144EA: terrain flag byte of a tile id (tiles 0 and 1 read the byte before the table).
        int TerrainByte(int tile) { int d = ((tile & 0x7F) - 2) & 0xFF; return TerrainFlags(d >= 0x80 ? -1 : d); }

        /// 0xE588: pixel centre of the current actor's cell.
        void CursorOnActor() { var me = S.Slots[Actor]; CursorX = (me[0x12] * 24 + 12) & 0xFFFF; CursorY = (me[0x13] * 24 + 12) & 0xFFFF; }

        /// 0x1432E: who stands on cell (x,y): the occupant list is written to [0xD5F8..] (terminated by 0xFF); returns the first entry (0xFF none).
        int Occupants(int x, int y)
        {
            int n = 0;
            if ((S.Tiles[y * 21 + x] & 0x80) != 0)
                for (int s = 0; s < S.SlotCount; s++)
                {
                    int f = S.Slots[s][0];
                    if (f == 0 || (f & 0xC0) != 0 || (S.Slots[s][1] & 4) != 0) continue;
                    foreach (var c in Cells(s)) if (c.x == x && c.y == y) G[A5F8 - GBase + n++] = (byte)s;
                }
            G[A5F8 - GBase + n] = 0xFF;
            return G[A5F8 - GBase];
        }

        /// 0x143FE: occupant list + tile id of a cell; cells outside the map read as "nobody", tile 0.
        int CellInfo(int x, int y, out int tile)
        {
            if ((x & 0xFF) >= 21 || (y & 0xFF) >= 21) { Gs(A5F8, 0xFF); tile = 0; return 0xFF; }
            int o = Occupants(x & 0xFF, y & 0xFF);
            tile = S.Tiles[(y & 0xFF) * 21 + (x & 0xFF)] & 0x7F;
            return o;
        }

        /// 0x1443C: what a step of creature `slot` in direction `dir` would run into: the last other occupant of the cells it would cover (0xFF = free) and the
        /// tile with the highest terrain cost among them (cost = terrain flags & 0x3F, at least 1).
        void ProbeStep(int slot, int dir, out int occupant, out int tile)
        {
            var s = S.Slots[slot]; int ox = s[0x12], oy = s[0x13];
            s[0x12] = (byte)(ox + CombatWorld.DX[dir]); s[0x13] = (byte)(oy + CombatWorld.DY[dir]);
            occupant = 0xFF; int best = 1; tile = 0;
            foreach (var c in Cells(slot))
            {
                int t; CellInfo(c.x, c.y, out t);
                for (int k = 0; ; k++)
                {
                    int e = G[A5F8 - GBase + k]; if (e >= 0x80) break;
                    if (e != slot) occupant = e;
                }
                int cost = TerrainByte(t) & 0x3F;
                if (cost >= best) { best = cost; tile = t; }
            }
            s[0x12] = (byte)ox; s[0x13] = (byte)oy;
        }

        /// 0xF842: may the current actor leave the map: false when a faster enemy (more movement) has a clear line (range 0x28) to it.
        bool CanEscape()
        {
            var me = S.Slots[Actor]; int mySide = me[1] & 1;
            for (int s = S.SlotCount - 1; s >= 0; s--)
            {
                var o = S.Slots[s];
                if (o[0] == 0 || (o[0] & 0xC0) != 0) continue;
                if (((o[1] & 1) ^ mySide) == 0) continue;
                if (me[0xF] >= o[0xF]) continue;
                if (LineOfFireTo(s, 0x28).Clear) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------------------------ 0x10400 with the globals
        /// Everything the ROM works out before an attack by the current actor on `target` (see GenesisAttackPlanner), leaving the result in the globals:
        /// [0xD511] to-hit (0 = cannot attack), [0xD518] message, [0xD512] distance, [0xD513] target, [0xD496] damage multiplier, [0xD501..3] line-of-fire flags.
        public void PrepareAttack(int target)
        {
            T("prep");
            Gs(A513, target); Gs(A511, 0); Gs(A518, 0); Gs(A518 + 1, 0);
            var env = new AttackEnv
            {
                State = S, Rom = Rom, TerrainFlags = TerrainFlags, Mode97AE = Mode97AE, SkipBlockers = D4FF, SideModMonster = Gb(A499), SideModParty = Gb(A49A),
                BackstabMask = Gb(A4FD), CursorX = CursorX, CursorY = CursorY, DamageMultiplier = Gb(A496), RangeGarbage = NextRangeGarbage, Hook = (st, sl) => Stage(st, sl)
            };
            var plan = GenesisAttackPlanner.Prepare(env, Actor, target);
            Gs(A496, env.DamageMultiplier);
            Gs(A513, plan.Target); Gs(A511, plan.ToHit); Gs(A518, plan.Message >> 8); Gs(A518 + 1, plan.Message);
            if (plan.TailReached) Gs(A512, plan.Distance);
            if (plan.LofRan) { D501 = (byte)(plan.Flag501 ? 0xFF : 0); D502 = (byte)(plan.Flag502 ? 0xFF : 0); D503 = (byte)(plan.Flag503 ? 0xFF : 0); D504 = 0; }
        }

        /// 0x105FC: does the current actor hold an explosive (type 5..12) in its hand?
        public bool HoldsExplosive()
        {
            var me = S.Slots[Actor];
            if (Mode97AE != 0 && me[2] < 8) return false;
            var rec = S.Records[me[2]];
            if (rec[0xAE] == 0) return false;
            int t = rec[0xAE + 9];
            return t >= 5 && t <= 0xC;
        }

        // ------------------------------------------------------------------------------------ 0x1074A: carry out an attack on [0xD513]
        /// The current actor attacks the creature in [0xD513] using the to-hit prepared by PrepareAttack: every attack of its primary and then secondary
        /// natural/weapon attack is rolled (to-hit, damage dice, rocket special), the damaging hits are collected in order and then applied to the victim, LAST HIT
        /// FIRST (the animation lets the projectiles arrive in reverse order). Afterwards the actor faces the victim and its action time is spent.
        /// A victim that is not alive aborts the whole thing. Returns the damage list.
        public List<byte> ExecuteAttack()
        {
            T("exec");
            var list = new List<byte>();
            int t = (sbyte)Gb(A513);
            if (t < 0) return list;
            var v = S.Slots[t];
            if (v[0] == 0 || (v[0] & 0xC0) != 0) return list;
            v[1] |= 2;
            Gs(A50D, 3);
            var me = S.Slots[Actor]; var rec = S.Records[me[2]]; var vrec = S.Records[v[2]];
            for (int d4 = 0; d4 < 2; d4++)
            {
                int n = GenesisCombat.AttacksThisRound(me[6 + d4], d4, Gb(A50C));
                for (int i = 0; i < n; i++)
                {
                    Gs(A4FC, 0);
                    if (Rng.Roll(20) > Gb(A511)) continue;                                  // the d20 is above the to-hit value: a miss
                    Stage(2 + d4, Actor);
                    int sum = 0; for (int q = 0; q < me[8 + d4]; q++) sum = (sum + (Rng.Roll(me[0xA + d4]) & 0xFFFF)) & 0xFFFF;
                    int b = (sbyte)(byte)(sum + me[0xC + d4]);
                    Gs(A497, ((b < 0 ? 0 : b) * Gb(A496)) & 0xFF);
                    Stage(4, Actor);
                    Stage(5, t);                                                             // the victim's defences may change [0xD497]
                    if (GenesisCombat.SpecialEligible(Mode97AE, Actor, rec[0xAE], id => Rom.Byte(WeaponTable + 8 * (sbyte)id + 1)))
                    {
                        int f = vrec[0x2F]; bool full = false;
                        if ((f & 4) != 0) full = (Rng.Roll(100) & 0xFF) <= 0x4B;
                        else if ((f & 2) != 0) full = (Rng.Roll(100) & 0xFF) <= 0x32;
                        if (full) Gs(A4FC, 0xFF);
                    }
                    if (Gb(A4FC) != 0) Gs(A497, 0xFF);
                    if (TraceLof) T("st " + Gb(A497));
                    Gs(0xD48E + list.Count, Gb(A497));                                       // the list overlaps [0xD496..0xD499]
                    list.Add(Gb(A497));
                }
            }
            if (list.Count > 12) throw new InvalidOperationException("Attack Missile error!");          // 0x108CE: the ROM prints this and halts
            // 0x116AC: the actor turns towards the victim
            me[0x10] = (byte)GenesisCombat.Octant(v[0x12], v[0x13], me[0x12], me[0x13]);
            var arr = new byte[list.Count]; for (int i = 0; i < arr.Length; i++) arr[i] = Gb(0xD48E + i);     // read back: entries 8..11 share bytes with scratch
            bool melee = (WeaponRangeOfActor() & 0xFF) <= 1;
            if (!melee) AnimationProbe();
            for (int k = list.Count - 1; k >= 0; k--)
            {
                bool cue, applied;
                S.ApplyDamageEntry(t, arr, k, list.Count, melee, out cue, out applied);
            }
            if ((v[0] & 0x80) != 0) S.ClearMarkers(t);                                        // 0x10DC4: a victim that went down leaves the map markers
            me[0x14] = 0;                                                                      // 0x107D2: the attack spends the action time
            return list;
        }

        /// 0x11638 for the current actor.
        int WeaponRangeOfActor()
        {
            var env = new AttackEnv { State = S, Rom = Rom, Mode97AE = Mode97AE, RangeGarbage = NextRangeGarbage };
            return GenesisAttackPlanner.WeaponRange(env, Actor, S.Records[S.Slots[Actor][2]]);
        }

        // ------------------------------------------------------------------------------------ 0x11A44: reactions to a step
        /// After creature Actor (the mover) took a step, every living creature of the other side that still has its reaction ready (slot flag +1 bit 4) and
        /// can attack it does so, once. Returns 1, or -1 when the mover did not survive. The mover's map markers are restored while the reactions run.
        public int Reactions()
        {
            T("react");
            int mover = Actor; Gs(A513, mover);
            int moverSide = S.Slots[mover][1] & 1; bool first = false;
            Ca[0] = 0;
            do
            {
                var a3 = S.Slots[Actor];
                int st = a3[0];
                if (st != 0 && (st & 0xC0) == 0 && (a3[1] & 0x10) != 0 && (((a3[1] & 1) ^ moverSide) != 0))
                {
                    PrepareAttack(mover);
                    if (Gb(A511) != 0)
                    {
                        a3[1] &= 0xEF;
                        if (!first)
                        {
                            S.Slots[mover][1] &= 0xFB; S.SetMarkers(mover);                       // 0xFA22: the mover is a normal map occupant again
                            first = true;
                        }
                        Gs(A513, mover);
                        CursorOnActor();
                        ExecuteAttack();
                        if ((sbyte)S.Slots[mover][0] <= 0) { Ca[0] = (byte)mover; return -1; }
                    }
                }
                Ca[0] = (byte)(Ca[0] + 1);
            } while (Ca[0] < S.SlotCount);
            Ca[0] = (byte)mover;
            if (first) { S.Slots[mover][1] |= 4; S.ClearMarkers(mover); Gs(A510, 0xFF); }        // 0xF9A6: the mover is "in motion" again
            return 1;
        }

        // ------------------------------------------------------------------------------------ 0xF898: one step
        /// The current actor tries to step by ([0xB3F4],[0xB3F6]). Returns 1 (stepped, nobody killed it), -1 (it died to a reaction), 0 (no step).
        /// A step costs the terrain's movement points (flags & 0x1F); it fails when the cell is occupied, impassable (flag 0x20) or too expensive.
        /// Stepping off the map is "fleeing": allowed unless a faster enemy sees the creature; the creature gets status 0x85 and 6 movement points.
        public int MoveStep()
        {
            T("step");
            var me = S.Slots[Actor];
            int nx = (me[0x12] + (MoveDx & 0xFF)) & 0xFF, ny = (me[0x13] + (MoveDy & 0xFF)) & 0xFF;
            bool cancel = false;
            if (nx >= 21 || ny >= 21)
            {
                if ((me[1] & 0x80) == 0 && (RetreatPrompt == null || RetreatPrompt() != 0)) cancel = true;
                else if (!CanEscape()) { G[A5D6 - GBase] = 0; G[A5D6 - GBase + 1] = 4; G[A5D6 - GBase + 2] = 0; G[A5D6 - GBase + 3] = 0x17; cancel = true; }
                else { me[0] = 0x85; me[0x16] = 6; me[0x14] = 0; }
            }
            if (!cancel)
            {
                int idx = (short)(MoveDy * 4 + MoveDx);
                int dir = (idx + 5 >= 0 && idx + 5 < FacingByStep.Length) ? FacingByStep[idx + 5] : 8;
                if (dir == 8) cancel = true;
                else
                {
                    me[0x10] = (byte)dir;
                    int occ, tile; ProbeStep(Actor, dir, out occ, out tile);
                    if (occ < 0x80) cancel = true;
                    else
                    {
                        int fl = TerrainByte(tile);
                        if ((fl & 0x20) != 0) cancel = true;
                        else { int cost = fl & 0x1F; if (cost > me[0x16]) cancel = true; else me[0x16] = (byte)(me[0x16] - cost); }
                    }
                }
            }
            if (cancel) { MoveDx = 0; MoveDy = 0; }
            me[0x12] = (byte)(me[0x12] + (MoveDx & 0xFF)); me[0x13] = (byte)(me[0x13] + (MoveDy & 0xFF));
            if (((MoveDx | MoveDy) & 0xFF) == 0) return 0;
            return Reactions();
        }

        // ------------------------------------------------------------------------------------ the turn controller
        public byte D8FC;                   // [0xD8FC]: bit 7 = the player asked to take over (party members only)
        public System.Collections.Generic.Queue<int> RangeD2;      // the bits 8..15 of register d2 at each call of 0x11630 / 0x11638 (see AttackEnv.RangeGarbage); empty = 0
        int NextRangeGarbage() { return RangeD2 != null && RangeD2.Count > 0 ? RangeD2.Dequeue() : 0; }
        /// 0x10EB4, run by every projectile animation (0x108AA) that is not melee: with an item above id 0x12 in the actor's hand it asks 0x11638 for the range - one more
        /// draw from the garbage queue (see RangeD2).
        void AnimationProbe()
        {
            var rec = S.Records[S.Slots[Actor][2]]; int id = rec[0xAE];
            if (id != 0 && id > 0x12) NextRangeGarbage();
        }
        public int Ticks;                   // safety net for the controller loop (the ROM has none; a path is finite)

        /// 0xF156: bit 7 of [0xD8FC] hands the party over to the player: every party creature with flag bit 7 loses it; true when that was the current actor.
        bool TakeoverRequested()
        {
            bool mine = false;
            if ((D8FC & 0x80) == 0) return false;
            for (int i = 0; i < 8 && i < S.SlotCount; i++)
            {
                var sl = S.Slots[i];
                if ((sl[1] & 1) == 0 || (sl[1] & 0x40) != 0 || (sl[1] & 0x80) == 0) continue;
                sl[1] &= 0x7F;
                if (i == Actor) mine = true;
            }
            return mine;
        }

        /// 0xF1A2: the creature is done for the round (a creature that cannot throw explosives keeps its reaction ready).
        void EndTurn()
        {
            T("end");
            var me = S.Slots[Actor]; me[0x14] = 0;
            if (!HoldsExplosive()) me[1] |= 0x10;
        }

        /// 0xF132: first visible action of the turn: the creature's stats are recomputed once.
        bool turnShown;
        void ShowTurn()
        {
            if (turnShown) return;
            turnShown = true;
            MoveDx = 0; MoveDy = 0;                                                       // 0xE5A8
            var me = S.Slots[Actor];
            GenesisStats.RecomputeSlot(Rom, me, S.Records[me[2]], Gb(A49A), Gb(A499), Mode97AE != 0);
            Gs(0xD508, 0xFF);
        }

        /// 0xF0E2: attack the creature chosen as target (+0x17) if the preparation allows it. Explosive weapons (0xEB50) are not ported.
        void AttackTarget()
        {
            T("attack");
            var me = S.Slots[Actor];
            if (me[0x14] == 0) return;
            if (moving) { EndMotion(); }
            if (HoldsExplosive()) { ShowTurn(); AreaEval(false, 0xAE); return; }                 // 0xF0E2: an explosive is thrown by the area logic
            if ((sbyte)me[0x17] < 0) return;
            CursorOnActor();
            PrepareAttack(me[0x17]);
            if (Gb(A511) == 0) return;
            ShowTurn();
            ExecuteAttack();
        }

        bool moving;                        // the ROM's d7: the creature has started walking (flag 4 set, map markers cleared)
        void StartMotion() { var me = S.Slots[Actor]; S.ClearMarkers(Actor); me[1] |= 4; Gs(A510, 0xFF); moving = true; }          // 0xF9A6
        void EndMotion() { if (!moving) return; var me = S.Slots[Actor]; me[1] &= 0xFB; S.SetMarkers(Actor); moving = false; }      // 0xFA22 via 0xF0D6

        /// One decision of the monster's turn: ROM 0xEF64 (with the current actor in [0xCA20], its time in +0x14). The creature picks (or keeps) a target, equips its
        /// best weapon, then walks along the shortest path to the nearest enemy, attacking as soon as the target is in line of fire within (half) weapon range
        /// - or, for a weapon of range 1, as soon as an enemy stands next to it. Steps cost movement points; every step may draw reactions from adjacent enemies.
        /// When it cannot get anywhere it waits (time 1); a creature that already waited attacks anything in reach, else ends its turn.
        public void RunTurn()
        {
            var me = S.Slots[Actor];
            moving = false; turnShown = false; Enumerated = false;
            TakeoverRequested();
            if ((me[1] & 1) != 0) { /* 0x10200: a party creature may first go to a fallen friend */ AllyRescue(); }
            if (me[0x14] == 0) { Finish(); return; }
            bool waited = me[0x14] == 1;
            if (waited) me[0x17] = 0xFF;
            SelectTarget();
            bool giveUp = (sbyte)me[0x17] < 0;
            int range = 0, pi = 0;
            if (!giveUp)
            {
                Stage(0xE, Actor);
                ChooseWeapon(1);
                if (me[0x14] == 0 && me[0x15] == 0) { Finish(); return; }
                range = WeaponRangeOfActor();
                Navigate();
                pi = 2;
                if ((sbyte)Nav[pi] < 0) giveUp = true;
            }
            if (!giveUp)
            {
                while (true)
                {
                    if (++Ticks > 100000) throw new InvalidOperationException("turn does not end");
                    if (TakeoverRequested()) { Finish(); return; }
                    if (me[0x14] == 0) { Finish(); return; }
                    bool attack;
                    if (range > 1)
                        attack = LineOfFireTo(me[0x17], ((short)range >> 1) & 0xFFFF).Clear;
                    else
                    {
                        D500 = (byte)((me[1] ^ 1) & 1);
                        EnumerateTargets(Actor, 1);
                        attack = D506 != 0;
                        if (attack) me[0x17] = Ca[2 + 3 * Rng.Next(D506)];
                    }
                    if (attack)
                    {
                        AttackTarget();
                        if (me[0x14] == 0) { Finish(); return; }
                    }
                    int dir = (sbyte)Nav[pi++];
                    if (dir < 0) break;
                    if (!moving) { ShowTurn(); StartMotion(); }
                    MoveDx = CombatWorld.DX[dir]; MoveDy = CombatWorld.DY[dir];
                    int r = MoveStep();
                    if (r < 0) return;                                                     // the creature died on the way: nothing more to tidy up
                    if (r == 0) break;
                }
            }
            // 0xF07A: the way is blocked or finished. The ROM tests register d6 here, which holds "time was 1" until an enumeration (0x15C2C) overwrites it with the
            // number of creatures: any turn that searched for a target therefore takes the "waited" branch.
            bool d6 = waited || Enumerated;
            if (!d6) { me[0x14] = 1; Finish(); return; }
            if (range != 0)
            {
                D500 = (byte)((me[1] ^ 1) & 1);
                EnumerateTargets(Actor, range);
                if (D506 != 0)
                {
                    me[0x17] = Ca[2 + 3 * Rng.Next(D506)];
                    AttackTarget();
                    if (me[0x14] == 0) { Finish(); return; }
                }
            }
            EndTurn();
            Finish();
        }

        void Finish() { EndMotion(); }

        /// 0xF28A / 0xF22C: does a party creature with healing skill (record +0x32 or +0x3B) have a fallen friend to help? The candidates (party creatures with status 0x83
        /// (dying), or 0x84 when the healer has skill points in +0x32 and the friend is not yet in the mask [0xD50A]) are left at [0xCA22] / [0xD506].
        bool AllyRescueCheck()
        {
            var me = S.Slots[Actor]; var rec = S.Records[me[2]];
            if ((me[1] & 1) == 0 || (rec[0x32] == 0 && rec[0x3B] == 0)) return false;
            FillRescueList(rec[0x32]);
            return D506 != 0;
        }

        /// The list loop of 0xF28A / 0xF22C: `d4` = the healer's skill points (+0x32).
        void FillRescueList(int d4)
        {
            int n = 0;
            for (int i = 0; i < 8 && i < S.SlotCount; i++)
            {
                var sl = S.Slots[i]; int st = sl[0];
                if (st != 0x83)
                {
                    if (d4 == 0 || st != 0x84) continue;
                    if (((Gb(0xD50A) >> (sl[2] & 7)) & 1) != 0) continue;
                }
                if ((sl[1] & 1) == 0) continue;
                Ca[2 + 3 * n++] = (byte)i;
            }
            D506 = (byte)n;
        }

        /// 0x10200: the first fallen friend becomes the target; the creature walks to it along the shortest path (mode 2 of the search) and treats it.
        public void AllyRescue()
        {
            if (!AllyRescueCheck()) return;
            S.Slots[Actor][0x17] = Ca[2];
            Gs(0xD505, 2); Rescue(); Gs(0xD505, 0);
        }

        /// 0x1039E: is the target (+0x17) within one cell of the current actor?
        bool TargetAdjacent()
        {
            var me = S.Slots[Actor]; int t = (sbyte)me[0x17];
            if (t < 0 || t >= S.SlotCount) return false;
            var v = S.Slots[t];
            return (byte)(v[0x12] - me[0x12] + 1) <= 2 && (byte)(v[0x13] - me[0x13] + 1) <= 2;
        }

        static readonly int[] Dx9 = { 0, 1, 1, 1, 0, -1, -1, -1, 0 }, Dy9 = { -1, -1, 0, 1, 1, 1, 0, -1, 0 };    // 0x146E0 / 0x146EB incl. entry 8 (the cell itself)

        /// 0x1021E: walk to the fallen friend; once next to it the creature spends its turn: without healing skill points (+0x32) a first-aid check (skill 10) must reach 2;
        /// then the friend is stabilised (status 0x84, HP 0) and - unless it was already revived in this fight ([0xD50A] bit of its record) - a medicine check (skill 1, result - 2,
        /// times 4, plus the healer's +0x32 points; at most the friend's maximum HP) brings it back on its feet on the nearest free cell around it.
        void Rescue()
        {
            T("rescue");
            var me = S.Slots[Actor]; var rec = S.Records[me[2]];
            bool moved = false, shown = false;
            Navigate(); int pi = 2;
            if ((sbyte)Nav[pi] >= 0)
                while (true)
                {
                    if (TargetAdjacent()) break;
                    int dir = (sbyte)Nav[pi++];
                    if (dir < 0) break;
                    if (!moved)
                    {
                        if (!shown) { shown = true; MoveDx = 0; MoveDy = 0; GenesisStats.RecomputeSlot(Rom, me, rec, Gb(A49A), Gb(A499), Mode97AE != 0); Gs(0xD508, 0xFF); }
                        S.ClearMarkers(Actor); me[1] |= 4; Gs(A510, 0xFF); moved = true;                  // 0xF9A6
                    }
                    MoveDx = Dx9[dir]; MoveDy = Dy9[dir];
                    int r = MoveStep();
                    if (r < 0) return;
                    if (r == 0) break;
                }
            me[1] &= 0xFB; S.SetMarkers(Actor); moving = false;                                          // 0xFA22 (also when it never moved)
            if ((sbyte)me[0] < 0) return;
            if (!TargetAdjacent()) return;
            me[0x14] = 0;
            int t = me[0x17]; var v = S.Slots[t];
            int skillFlags = S.Slots[me[2]][0];
            if (rec[0x32] == 0 && GenesisSkills.SkillCheck(Rom, Rng, rec, skillFlags, 0xA, 2) < 2) return;
            v[0xE] = 0; v[0] = 0x84;
            if (((Gb(0xD50A) >> (v[2] & 7)) & 1) != 0) return;
            int d0 = (GenesisSkills.SkillCheck(Rom, Rng, rec, skillFlags, 1, 2) - 2) & 0xFF;
            if ((sbyte)d0 < 0) return;
            d0 = (d0 << 2) & 0xFF; d0 = (d0 + rec[0x32]) & 0xFF;
            v[0xE] = (byte)d0;
            var vrec = S.Records[v[2]];
            if (vrec[0x2E] < v[0xE]) v[0xE] = vrec[0x2E];
            for (int d4 = 8; d4 >= 0; d4--)
            {
                int x = (v[0x12] + Dx9[d4]) & 0xFF, y = (v[0x13] + Dy9[d4]) & 0xFF;
                int tile; int occ = CellInfo(x, y, out tile);
                if (occ < 0x80) continue;
                if ((TerrainByte(tile) & 0x20) != 0) continue;
                v[0] = 1; v[0x12] = (byte)x; v[0x13] = (byte)y;
                S.RebuildMarkers();
                G[0xD50A - GBase] |= (byte)(1 << (v[2] & 7));
                return;
            }
        }
    }
}
