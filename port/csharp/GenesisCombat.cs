// GenesisCombat.cs -- exact ports of three combat routines of the Genesis game. Pure C# (no UnityEngine); each function is
// verified against the real ROM code run in a 68000 emulator (port/tests/CombatTests.cs, port/tools/gen_combat_vectors.py).
//
//   Octant        ROM 0x15D36  direction (0..7) from the attacker to the target
//   ArmorAgainst  ROM 0x10666  which armor value protects the target: front, flank (-2), rear (rear armor, rogue -2) and the
//                              rear-attack damage multiplier ("backstab") the damage code reads from [0xD496]
//   ToHit         ROM 0x1056A  range penalties and the clamp to 1..19 (x5 = displayed percent)
//   ResolveAttack ROM 0x10804  one attack: d20 vs to-hit, weapon dice, signed-byte bonus, backstab multiplier and the
//                              rocket-weakness "full damage" roll; consumes the RNG exactly like the ROM
//   AttacksThisRound ROM 0x107E8  attacks per slot per round from the "attacks x2" value and the round parity
//
// Conventions of the combat engine (26-byte slots from 0xC470 party / 0xC540 enemies, real byte offsets):
//   +1 flags (bit 0 = side), +3 attack value, +4 armor, +5 rear armor, +0x10 facing 0..7, +0x12 x, +0x13 y
//   character/monster record (214 bytes): +0x18 career (4 = rogue), +0x19 level, +0xAE first gear slot (item id byte)
// Directions: 0..7 with 0 = the axis direction used by the ROM for "dy>=0, |dx| small" etc.; only differences matter for
// armor ((bearing - facing) & 7).

using System;

namespace BuckRogersGenesis
{
    public static class GenesisCombat
    {
        // ------------------------------------------------------------------------------------------ octant 0x15D36
        /// Direction from the attacker (ax,ay) to the target (tx,ty) as the ROM computes it: 0..7.
        public static int Octant(int attackerX, int attackerY, int targetX, int targetY)
        {
            short dx = (short)(attackerX - targetX);                // sub.w d0,d2
            short dy = (short)(attackerY - targetY);                // sub.w d1,d3
            ushort ax = (ushort)(dx >= 0 ? dx : -dx);               // neg.w (stays 0x8000 for -32768)
            ushort ay = (ushort)(dy >= 0 ? dy : -dy);
            int d4;
            if (ax < ay)
            {
                uint m = ((uint)ax * 0x26Au) >> 8;                  // mulu.w #$26a ; asr.l #8   (tan 67.5 deg = 2.414 = 0x26A/256)
                d4 = ((ushort)m > ay) ? 1 : 0;
            }
            else
            {
                uint m = ((uint)ay * 0x26Au) >> 8;
                d4 = ((ushort)m > ax) ? 1 : 2;
            }
            if (dy >= 0) d4 = 4 - d4;                               // tst.w d3 ; bmi skip ; neg.w d4 ; addq #4
            if (dx < 0) d4 = -d4;                                   // tst.w d2 ; bpl skip ; neg.w d4
            return d4 & 7;
        }

        // ------------------------------------------------------------------------------ armor selection 0x10666
        public struct ArmorResult
        {
            public int Armor;               // armor value the to-hit roll uses (byte)
            public int DamageMultiplier;    // [0xD496]: 1 normally; backstab multiplier on a rear attack by an eligible attacker
            public int Message;             // 0x128 flank, 0x129 rear, 0x12A backstab applied, -1 none (message ids of the game's text table)
            public bool TargetTurned;       // the target turned to face the attacker (then the attack counts as frontal)
            public int TargetFacing;        // target facing after the call
        }

        /// weaponTypeOf(itemId) = byte b1 of the weapon table at ROM 0x779E + 8*id (0 = melee/unarmed class).
        public static ArmorResult ArmorAgainst(int bearing, int attackerFlags, int targetFlags, int targetFacing,
            int targetArmor, int targetRearArmor, int attackerCareer, int attackerLevel, int attackerIndex,
            int backstabMask, int attackerWeaponItemId, Func<int, int> weaponTypeOf)
        {
            var r = new ArmorResult { DamageMultiplier = 1, Message = -1, TargetFacing = targetFacing & 0xFF };
            // an idle target (flags & 6 == 0) of the other side turns toward its attacker
            if ((targetFlags & 6) == 0 && (((attackerFlags ^ targetFlags) & 1) != 0))
            {
                r.TargetFacing = bearing & 0xFF; r.TargetTurned = true;
            }
            int rel = (bearing - r.TargetFacing) & 7;               // sub.b ; andi.w #7
            if (rel < 2 || rel == 7) { r.Armor = targetArmor & 0xFF; return r; }   // front (0,1,7)
            if (rel == 2 || rel == 6) { r.Message = 0x128; r.Armor = (targetArmor - 2) & 0xFF; return r; }  // flank
            // rear (3,4,5)
            r.Message = 0x129;
            int armor = targetRearArmor & 0xFF; int d6 = 2;
            if ((attackerCareer & 0xFF) == 4)                       // rogue
            {
                armor = (armor - 2) & 0xFF;
                d6 = ((sbyte)(byte)(7 + attackerLevel)) >> 2;       // asr.b #2
            }
            if ((attackerIndex & 0xFF) < 8 && ((backstabMask >> (attackerIndex & 7)) & 1) != 0)
            {
                if (attackerWeaponItemId == 0 || weaponTypeOf(attackerWeaponItemId) == 0)
                {
                    r.DamageMultiplier = d6 & 0xFF; r.Message = 0x12A;
                }
            }
            r.Armor = armor;
            return r;
        }

        // ---------------------------------------------------------------------------------------- to-hit 0x1056A
        public struct ToHitResult
        {
            public bool Computed;   // false: the attack was refused (the ROM leaves the old result and sets Message)
            public int Value;       // 1..19 (the ROM's [0xD511]); displayed percent = Value * 5
            public int Percent => Value * 5;
            public int Message;     // -1 none, 0x0C (flag503 set), 0xBD / 0xBE (attack refused)
        }

        /// attack/armor: byte values; distance/range: in cells (range 0 or 1 = no range penalty); weaponType: weapon table b1;
        /// itemMod: the weapon slot's modifier byte (+9); flag501/502/503: engine state bytes at 0xD501..0xD503 (meaning not decoded).
        public static ToHitResult ToHit(int attack, int armor, int distance, int range, int weaponType, int itemMod,
            int flag501, int flag502, int flag503)
        {
            var res = new ToHitResult { Message = -1 };
            int att = attack & 0xFF;
            if ((flag503 & 0xFF) != 0) { res.Message = 0x0C; att = (att - 2) & 0xFF; }
            int rng = range & 0xFFFF, dist = distance & 0xFFFF;
            if (rng > 1)        // range 0/1 weapons jump straight to the final calculation: no penalties, no refusal checks
            {
                int half = ((short)rng) >> 1;                       // asr.w #1
                if (dist > half)
                {
                    int d6 = ((short)((rng * 3) & 0xFFFF)) >> 2;    // mulu.w #3 ; asr.w #2
                    att = (att - ((dist > (d6 & 0xFFFF)) ? 5 : 2)) & 0xFF;
                }
                int wt = weaponType & 0xFF, mod = itemMod & 0xFF;
                if (wt == 3)
                {
                    if ((flag502 & 0xFF) != 0) { res.Message = 0xBD; return res; }
                }
                else if (wt == 2 || (mod >= 5 && mod <= 0x0C))
                {
                    if ((flag501 & 0xFF) != 0) { res.Message = 0xBE; return res; }
                }
            }
            int v = (sbyte)(byte)(att - (armor & 0xFF) + 20);       // sub.b ; addi.b #$14
            if (v <= 0) v = 1;
            if (v > 19) v = 19;
            res.Computed = true; res.Value = v;
            return res;
        }

        // ------------------------------------------------------------------------- attacks per round 0x107E8
        /// Attacks a combat slot makes in this round: the slot stores "attacks x 2" (+6 primary, +7 secondary, slot index d4 = 0/1);
        /// the round parity at [0xD50C] makes odd values alternate (5 -> 2,3,2,3 ...; the second slot starts on the other phase).
        public static int AttacksThisRound(int attacksX2, int slotIndex, int roundParity)
        {
            int d7 = ((roundParity & 1) ^ (slotIndex & 0xFF)) & 0xFF;
            d7 = (d7 + attacksX2) & 0xFF;
            return d7 >> 1;
        }

        // ------------------------------------------------------------------------ attack resolution 0x10804
        public struct AttackResult
        {
            public bool Hit;          // false: the d20 roll was above the to-hit value
            public int Roll;          // the d20 (1..20); 20 always misses because to-hit tops out at 19
            public int Damage;        // the byte the ROM stores in [0xD497] (valid when Hit)
            public bool FullDamage;   // the special roll fired: damage forced to 0xFF
        }

        /// ROM 0x1062C: the full-damage special applies only to attackers carrying a rocket-class weapon (weapon table b1 == 2) in their first
        /// gear slot, and not for party members while the mode byte [0x97AE] is non-zero.
        public static bool SpecialEligible(int mode97AE, int attackerIndex, int attackerGear0, Func<int, int> weaponTypeOf)
        {
            if (mode97AE != 0 && (attackerIndex & 0xFF) < 8) return false;
            return attackerGear0 != 0 && weaponTypeOf(attackerGear0) == 2;
        }

        /// One attack. toHit = ToHit(...).Value; dice/bonus come from the combat slot (+8/+9 dice count, +0xA/+0xB sides, +0xC/+0xD bonus for the
        /// primary/secondary attack); multiplier = ArmorAgainst(...).DamageMultiplier ([0xD496]); targetRecordFlags2F = target record byte +0x2F
        /// (bit 2: 75% full-damage chance, else bit 1: 50%) is read only when specialEligible.
        public static AttackResult ResolveAttack(GenesisRng rng, int toHit, int diceCount, int diceSides, int bonus, int multiplier,
            bool specialEligible, int targetRecordFlags2F)
        {
            var r = new AttackResult();
            r.Roll = rng.Roll(20) & 0xFF;                            // moveq #$14,d0 ; jsr 6C8C
            if (r.Roll > (toHit & 0xFF)) return r;                   // cmp.b d511,d0 ; bhi
            r.Hit = true;
            int sum = 0;                                             // 0x6CE4: sum of diceCount rolls of 1..sides (word)
            for (int i = 0; i < (diceCount & 0xFF); i++) sum = (sum + (rng.Roll(diceSides & 0xFF) & 0xFFFF)) & 0xFFFF;
            int b = (sbyte)(byte)(sum + bonus);                      // add.b bonus ; bpl else 0
            int baseDamage = b < 0 ? 0 : b;
            r.Damage = (baseDamage * (multiplier & 0xFF)) & 0xFF;    // mulu.w ; move.b -> [0xD497]
            if (specialEligible)
            {
                bool full = false;
                if ((targetRecordFlags2F & 4) != 0) full = (rng.Roll(100) & 0xFF) <= 0x4B;
                else if ((targetRecordFlags2F & 2) != 0) full = (rng.Roll(100) & 0xFF) <= 0x32;
                if (full) { r.FullDamage = true; r.Damage = 0xFF; }
            }
            return r;
        }
    }
}
