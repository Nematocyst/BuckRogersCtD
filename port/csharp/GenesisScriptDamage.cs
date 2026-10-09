// GenesisScriptDamage.cs -- the script command DAMAGE (opcode 0x2E, ROM 0x500A) and the helpers it uses; the screen output of the ROM handler is left to the host.
//   DAMAGE flags, count, sides, bonus, target
//   damage = count d sides + bonus (a word; applied as its low byte by the wound model 0x760A = ApplyDamage), only to living members (status non-zero, bits 6 and 7 clear).
//   flags bit 7 set: one blast, the target says who:
//       flags bit 6: every living member;  else target bit 7: the current character [0x9DA7];  else one random living member (0x51D2: list of living members, index = Next(n); nobody alive: member 0)
//       flags bit 5: no saving throw;  else a saving throw (0x6990, modifier = flags & 0x1F) is made for each victim; a failed save means damage, and bit 4 means damage even when the save succeeds (one random victim: the ROM never tests the save, see ScriptLeftoverD2)
//       target bits 0-2 = 0 with the current character: no save at all
//   flags bit 7 clear: `flags` shots (the loop is entered at its dbra), each with its own damage roll, a random living member and an attack test (0x6946): d20 (1 misses, 20 becomes 100), stage-0x10 hooks, then
//       hit when (signed byte)(roll + target) > the victim's armor (slot +4).
// After the damage: no living member left -> [0xBA53] = 1 and the game-over routine 0x7588 (reported as partyDown); any damage -> a screen refresh (changed).
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        const int A498 = 0xD498;
        /// The value of register D2 when the ROM handler reaches the untested save (one random victim, with a save): it decides, not the save. 0 (the value in the vectors) = always damage.
        /// No script of the game uses that combination (flags 0x80 / 0x90 with a random target and no 0x20 bit).
        public int ScriptLeftoverD2;

        bool PartyAlive(int i) { int st = S.Slots[i][0]; return st != 0 && (st & 0xC0) == 0; }

        /// 0x6CE4: `count` times Roll(sides), summed as a word.
        int RollDice(int count, int sides)
        {
            int sum = 0;
            for (int k = 0; k < (count & 0xFF); k++) sum += Rng.Roll(sides & 0xFF) & 0xFFFF;
            return sum & 0xFFFF;
        }

        /// 0x51D2: a random living party member (nobody alive: 0, the register that was just compared).
        int RandomLivingMember()
        {
            var list = new int[8]; int n = 0;
            for (int i = 0; i < 8; i++) if (PartyAlive(i)) list[n++] = i;
            return n == 0 ? 0 : list[Rng.Next(n) & 0xFF];
        }

        /// 0x6946: the attack test of a scripted shot against party member `victim` with modifier `tgt`.
        bool ScriptShotHits(int victim, int tgt)
        {
            int d = Rng.Roll(20) & 0xFF;
            if (d == 1) return false;
            if (d == 0x14) d = 0x64;
            Gs(A498, d); Stage(0x10, victim);
            int v = Gb(A498);
            if ((sbyte)v < 0) return false;
            v = (v + tgt) & 0xFF;
            return (sbyte)v > (sbyte)S.Slots[victim][4];
        }

        void HurtMember(int i, int amount, ref bool changed)
        {
            changed = true;
            if (PartyAlive(i)) S.ApplyDamage(i, amount & 0xFF);              // 0x5216 tests the status first, 0x760A subtracts
        }

        /// 0x500A. `current` = [0x9DA7]. Returns true when anything was hit (the ROM then refreshes the screen); partyDown = nobody is left standing.
        public bool ScriptDamage(int flags, int count, int sides, int bonus, int tgt, int current, out bool partyDown)
        {
            flags &= 0xFF; tgt &= 0xFF; bool changed = false;
            bool keep = (flags & 0x10) != 0;
            if ((flags & 0x80) != 0)
            {
                int type = flags & 0x1F;
                int dmg = (RollDice(count, sides) + (bonus & 0xFF)) & 0xFFFF;
                if ((flags & 0x40) != 0)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (!PartyAlive(i)) continue;
                        if ((flags & 0x20) != 0) HurtMember(i, dmg, ref changed);
                        else if (!SavingThrow(i, type)) HurtMember(i, dmg, ref changed);
                        else if (keep) HurtMember(i, dmg, ref changed);
                    }
                }
                else if ((tgt & 0x80) != 0)
                {
                    int c = current & 0xFF;
                    bool hit = true;
                    if ((tgt & 7) != 0 && SavingThrow(c, type) && !keep) hit = false;
                    if (hit) HurtMember(c, dmg, ref changed);
                }
                else
                {
                    int m = RandomLivingMember();
                    SavingThrow(m, type);                                           // ROM bug: the result is never tested (no TST after the call; the Z flag comes from the restored D2)
                    if (ScriptLeftoverD2 == 0 || keep) HurtMember(m, dmg, ref changed);
                }
            }
            else
            {
                for (int shot = 0; shot < flags; shot++)
                {
                    int dmg = (RollDice(count, sides) + (bonus & 0xFF)) & 0xFFFF;
                    int m = RandomLivingMember();
                    if (ScriptShotHits(m, tgt)) HurtMember(m, dmg, ref changed);
                }
            }
            int alive = 0; for (int i = 0; i < 8; i++) if (PartyAlive(i)) alive++;
            partyDown = alive == 0;
            return changed;
        }
    }
}

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        /// SKILLDAMAGE skill, who, shift, count, sides, bonus (opcode 0x49, ROM 0x5B44 -- six operands; the oracle's seventh is the EXIT behind it). Every member (who != 0) or the current
        /// character makes a skill check (GenesisSkills.SkillCheck, `shift` as for SKILL); a result below 2 costs count d sides + bonus hit points (the low byte, to a living member only).
        /// The damage dice are rolled for every failed check, whether or not the member can be hurt. Returns true when the ROM would have refreshed the screen first (any failed check).
        public bool ScriptSkillDamage(int skill, int who, int shift, int count, int sides, int bonus, int current)
        {
            bool failedOnce = false;
            int first = (who & 0xFFFF) != 0 ? 0 : (current & 0xFF), n = (who & 0xFFFF) != 0 ? 8 : 1;
            for (int k = 0; k < n; k++)
            {
                int m = first + k;
                int r = GenesisSkills.SkillCheck(Rom, Rng, S.Records[m], S.Slots[m][0], skill & 0xFF, shift);
                if (r >= 2) continue;
                failedOnce = true;
                int dmg = (RollDice(count, sides) + bonus) & 0xFFFF;
                if (PartyAlive(m)) S.ApplyDamage(m, dmg & 0xFF);
            }
            return failedOnce;
        }
    }
}
