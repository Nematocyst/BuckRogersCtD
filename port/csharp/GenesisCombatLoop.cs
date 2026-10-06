// GenesisCombatLoop.cs -- the round loop of a combat (ROM 0xE394 from 0xE3A8 on, with 0x158F2, 0x100D6, 0x754C, 0xE434, 0x6AA4): every round the sides may get a
// leadership bonus, every creature rolls its initiative, then the creature with the most time left acts until nobody has any; the round ends with the lingering
// patches ticking down, timed effects running out and dying party members bleeding. The fight ends when a side has nobody left standing.
// Verified against the ROM in port/tests/MonsterTests.cs (whole fights with scripted menu / pad input for the player-controlled creatures).
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public byte Surprise;                                  // [0x9DC1]: 1 = the party is surprised, 2 = the monsters are (-8 initiative for that side in the first round)
        public int MaxRounds = 5000;                           // safety net: the ROM has none (an attack, or a creature going down, always changes something)

        /// 0x754C: how many creatures of each side are standing: [0xD8CA] monsters, [0xD8CB] party.
        public void CountLiving()
        {
            S.LivingBySide[0] = 0; S.LivingBySide[1] = 0;
            for (int i = 0; i < S.SlotCount; i++)
            {
                int f = S.Slots[i][0];
                if (f == 0 || (f & 0xC0) != 0) continue;
                S.LivingBySide[S.Slots[i][1] & 1]++;
            }
        }

        /// 0x158F2: at the start of a round each side's first creature (in slot order) that is not under effect 0xD / 0xE and passes a skill-2 check gives its side
        /// a +1 modifier: [0xD499] monsters / [0xD49A] party (the attack value of every creature of the side, see GenesisStats.RecomputeSlot). The creature becomes the current actor.
        public void SideBonuses()
        {
            T("aura");
            Gs(A499, 0); Gs(A49A, 0);
            for (int i = 0; i < S.SlotCount; i++)
            {
                var sl = S.Slots[i]; int f = sl[0];
                if (f == 0 || (f & 0xC0) != 0) continue;
                int side = sl[1] & 1;
                if (Gb(A499 + side) != 0) continue;
                bool blocked = false;
                for (int k = 0; k < 32; k++)
                {
                    int e = Gb(0xD49C + 3 * k + 1);
                    if ((e == 0xE || e == 0xD) && Gb(0xD49C + 3 * k) == i) { blocked = true; break; }
                }
                if (blocked) continue;
                var rec = S.Records[sl[2]];
                if (GenesisSkills.SkillCheck(Rom, Rng, rec, sl[2] < S.SlotCount ? S.Slots[sl[2]][0] : 0, 2, 2) < 2) continue;
                Gs(A499 + side, 1);
                Actor = i;
            }
        }

        /// 0x6AA4: the timed effects list [0xD49C..] (slot, effect, turns left): one round passes; an effect whose turns run out is removed.
        void EffectTimers()
        {
            for (int k = 0; k < 32; k++)
            {
                int a = 0xD49C + 3 * k + 2;
                int b = Gb(a);
                if (b == 0) continue;
                Gs(a, b - 1);
                if (b - 1 == 0) Gs(a - 1, 0);
            }
        }

        /// 0xE434: the end of a round: round counter [0xD50C] + 1, the surprise is over, lingering patches tick down, timed effects run out; the peace counter [0xD50D]
        /// (set to 3 by every attack) counts down and at 0 the monsters give up (status 0x82, see DebugKillAll); a dying party member (status 0x83) gets one step closer
        /// to death: its HP byte counts up and past 15 it is dead (0x87).
        public void RoundEnd()
        {
            T("roundend");
            Gs(0xD50C, Gb(0xD50C) + 1);
            Surprise = 0;
            TickHazards();
            EffectTimers();
            Gs(0xD50D, Gb(0xD50D) - 1);
            if (Gb(0xD50D) == 0) DebugKillAll();
            for (int i = 0; i < 8 && i < S.SlotCount; i++)
            {
                var sl = S.Slots[i];
                if ((sl[1] & 1) == 0 || sl[0] != 0x83) continue;
                sl[0xE]++;
                if ((sbyte)sl[0xE] > 0xF) sl[0] = 0x87;
            }
        }

        /// 0xE3A8..0xE42C: the rounds of a fight, from the state the setup left ([0xD50E] set: a fight is on) until one side is gone.
        public void CombatRounds()
        {
            if (Gb(0xD50E) == 0) return;
            int rounds = 0;
            var flags0 = new int[Math.Max(S.SlotCount, 11)];
            while (true)
            {
                if (++rounds > MaxRounds) throw new InvalidOperationException("combat does not end");
                Gs(0xD4FD, 0);
                SideBonuses();
                if (S.SlotCount == 0) return;
                for (int i = 0; i < S.SlotCount; i++)
                {
                    T("begin");
                    for (int k = 0; k < flags0.Length; k++) flags0[k] = k < S.SlotCount ? S.Slots[k][0] : 0;
                    int slot = i;
                    int mask = GenesisTurns.BeginRound(Rom, Rng, S.Slots[i], S.Records[S.Slots[i][2]], S.Slots[i][2], flags0, Surprise, Gb(0xD50C), (D97DC & 0x10) != 0, Gb(0xD4FD),
                        () => Stage(0x12, slot));
                    Gs(0xD4FD, mask);
                }
                while (true)
                {
                    CountLiving();
                    if (S.LivingBySide[1] == 0 || S.LivingBySide[0] == 0) return;
                    var f = new int[S.SlotCount]; var sp = new int[S.SlotCount]; var tb = new int[S.SlotCount];
                    for (int i = 0; i < S.SlotCount; i++) { f[i] = S.Slots[i][0]; sp[i] = S.Slots[i][0x14]; tb[i] = S.Slots[i][0x15]; }
                    int next = GenesisTurns.PickNextActor(S.SlotCount, f, sp, tb);
                    if (next < 0) break;
                    Actor = next;
                    BeginTurn();
                }
                RoundEnd();
            }
        }
    }
}
