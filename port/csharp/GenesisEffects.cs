// GenesisEffects.cs -- the special-effect hooks of the combat engine (ROM 0x664E and its handlers 0x6742-0x68D4).
// Creatures carry status / ability ids (record +0x43..+0x4C permanently, the temporary list at [0xD49C] for timed ones). At fixed points ("stages") the engine runs the
// handler of every effect on the stage's list that the creature in a3 has. The handlers change the damage in [0xD497], the creature's stats, status or action time,
// or start an attack of their own. Verified against the ROM in port/tests/MonsterTests.cs.
//
//   stage  where it runs                              effects (id: meaning, as far as the handlers show)
//     2/3  every hit of the primary / secondary attack,    28 (0x1C): slows the victim after a failed save (halves movement and attacks, effect 0x1D for 5 turns)
//          before the damage dice (attacker)               30 (0x1E): the victim dies (status 0x86, HP 0) unless it saves
//     5    after the damage was rolled (victim)            20 (0x14): rocket weapons always do full damage   25 (0x19): half the hits do nothing against types 1, 2, 5   18 (0x12): none
//                                                          23 (0x17): immune to heat gun / plasma thrower   24 (0x18): immune to lasers (type 3)
//                                                          26 (0x1A): half the hits do nothing against types 1..5              27 (0x1B): half the hits do nothing against type 0 (melee)
//                                                          3: damage never above the victim's HP (knocked out, never killed)
//     7/15 start of the creature's turn                    1: stunned (no time, reaction or movement)   14 (0x0E): armor -2, and the turn is lost
//     9    a status effect is being applied (victim)        21 / 22 (0x15 / 0x16): immune to effect 0x0D / 0x0E     3: immune to every effect
//     10/11 attack preparation (attacker / victim)         13 (0x0D): armor -2, attack -4;  14 as above (stage 11)
//     12   saving throw                                     13
//     14   before the turn's weapon choice (monsters)      32 (0x20): spits at its target (35% to hit, 2d8)
//     18   start of the round                              29 (0x1D): slowed: movement and attacks halved
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public bool HooksEnabled = true;
        const int ATable = 0x6670;

        /// 0x664E: run the effect handlers of `stage` for creature `slot` (the ROM's a3).
        public void Stage(int stage, int slot)
        {
            if (!HooksEnabled || stage >= 0x17) return;
            int p = ATable + (Rom.Byte(ATable + 2 * stage) << 8 | Rom.Byte(ATable + 2 * stage + 1));
            int e;
            while ((e = Rom.Byte(p++)) < 0x80)
                if (HasEffect(slot, e)) RunEffect(e, slot);
        }

        void Halve(byte[] c)
        {
            c[0xF] = (byte)((sbyte)c[0xF] >> 1); c[0x16] = (byte)((sbyte)c[0x16] >> 1);
            c[6] = (byte)((sbyte)c[6] >> 1); c[7] = (byte)((sbyte)c[7] >> 1);
        }

        void EndTime(byte[] c) { c[0x14] = 0; c[1] &= 0xEF; c[0x16] = 0; }                 // 0x6A24

        /// 0x6ACC: the actor's hand weapon type (weapon table +1), or -1 when it has none (or the party is player controlled).
        int ActorHandType()
        {
            var me = S.Slots[Actor];
            if (Mode97AE != 0 && Actor < 8) return -1;
            var rec = S.Records[me[2]];
            if (rec[0xAE] == 0) return -1;
            return Tbl(rec[0xAE], 1);
        }

        void RunEffect(int e, int slot)
        {
            var c = S.Slots[slot];
            switch (e)
            {
                case 1: EndTime(c); break;
                case 3: Gs(A55E, 0); if (Gb(A497) >= c[0xE]) Gs(A497, c[0xE]); break;
                case 13: c[5] = (byte)(c[5] - 2); c[4] = c[5]; c[3] = (byte)(c[3] - 4); break;
                case 14: c[5] = (byte)(c[5] - 2); c[4] = c[5]; EndTime(c); break;
                case 20:
                    {
                        var me = S.Slots[Actor]; var rec = S.Records[me[2]];
                        if (GenesisCombat.SpecialEligible(Mode97AE, Actor, rec[0xAE], id => Rom.Byte(WeaponTable + 8 * (sbyte)id + 1))) Gs(A4FC, 0xFF);
                        break;
                    }
                case 21: if (Gb(A55E) == 0xD) Gs(A55E, 0); break;
                case 22: if (Gb(A55E) == 0xE) Gs(A55E, 0); break;
                case 23: { if (ActorHandType() >= 0) { int id = S.Records[S.Slots[Actor][2]][0xAE]; if (id == 0xE || id == 0x11) Gs(A497, 0); } break; }
                case 24: if (ActorHandType() == 3) Gs(A497, 0); break;
                case 25: { int t = ActorHandType(); if ((t == 1 || t == 2 || t == 5) && Rng.Roll(100) >= 0x32) Gs(A497, 0); break; }
                case 26: { int t = ActorHandType(); if (t >= 1 && t <= 5 && Rng.Roll(100) >= 0x32) Gs(A497, 0); break; }
                case 27: if (ActorHandType() == 0 && Rng.Roll(100) >= 0x32) Gs(A497, 0); break;
                case 28:
                    {
                        int v = Gb(A513); var vs = S.Slots[v];
                        bool saved = SavingThrow(v, 0);
                        if (!saved) { Halve(vs); ApplyEffect(v, 0x1D, 5, saved, 2); }
                        break;
                    }
                case 29: Halve(c); break;
                case 30:
                    {
                        int v = Gb(A513); var vs = S.Slots[v];
                        if (!SavingThrow(v, 0)) { vs[0] = 0x86; vs[0xE] = 0; }
                        break;
                    }
                case 32: Spit(); break;
            }
        }

        /// 0x68D4: an acid-spitting creature, before choosing its weapon: if its target is in line of fire within 12 cells, a d100 below 35 hits it for 2d8.
        void Spit()
        {
            var me = S.Slots[Actor];
            int ax = me[0x12], ay = me[0x13];
            for (int i = 0; i < 8; i++) Gs(0xD48E + i, 0);
            int sum = 0; for (int i = 0; i < 2; i++) sum += Rng.Roll(8) & 0xFFFF;
            Gs(0xD48E, sum);
            if ((sbyte)me[0x17] < 0) return;
            int t = me[0x17]; Gs(A513, t);
            var v = S.Slots[t];
            if (!Trace(v[0x12], v[0x13], ax, ay, 12).Clear) return;
            int count = Rng.Roll(100) < 0x23 ? 1 : 0;
            AnimationProbe();
            var list = new byte[] { Gb(0xD48E) };
            S.ApplyDamageEntry(t, list, 0, count, false, out bool cue, out bool applied);
            if ((v[0] & 0x80) != 0) S.ClearMarkers(t);
        }

        // ------------------------------------------------------------------------------------ turn start / end (0xE4F0)
        /// 0xE4F0: the whole of one creature's turn: the reaction flag is consumed, the start-of-turn effects run (a creature left without time loses its turn), then the
        /// computer controlled creature (flag bit 7 set: every monster) takes its turn, and finally the end-of-turn item upkeep. Manually played turns (flag 7 clear: party members under the player's control) are not ported.
        public void BeginTurn()
        {
            T("turn");
            var me = S.Slots[Actor];
            me[1] &= 0xEF;
            Stage(7, Actor);
            if (me[0x14] != 0)
            {
                Stage(0xF, Actor);
                if (me[0x14] != 0)
                {
                    if ((me[1] & 0x80) == 0) throw new NotSupportedException("manually played turn (ROM 0xF2AE)");
                    RunTurn();
                }
            }
            EndOfTurn();
        }

        /// 0xE54A: a creature that is out of time and not under effect 0x0E counts down its timed items (item flag bits 0x30: one step of 0x10).
        public void EndOfTurn()
        {
            var me = S.Slots[Actor]; var rec = S.Records[me[2]];
            if (HasEffectFor(Actor, rec, 0xE) || me[0x14] != 0) return;
            for (int k = 0; k < 13; k++) { int o = 0x54 + 10 * k + 5; if ((rec[o] & 0x30) != 0) rec[o] = (byte)(rec[o] - 0x10); }
        }
    }
}
