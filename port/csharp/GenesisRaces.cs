// GenesisRaces.cs -- the race byte of a character / monster record (+0x17) and the two places where the game's rules depend on it, read from the ROM:
//   creation: racial ability modifiers, 5 signed bytes per race at 0x63B (applied to +0x10..+0x14 by 0x650 / 0x654; entries exist for races 1-3 only)
//   starship crew hazard (0x1956E): a d20 save against 14 - level / 3, desert runners -1, races >= 3 +3 (see ShipHazard)
// Other reads of +0x17: the combat info panel (0x6F26: race icon 0x33 + race, race 5 -> 0x45) and the death sound (0x11B40: race 4 -> sound 0x46).
using System;

namespace BuckRogersGenesis
{
    /// Record byte +0x17.
    public enum GenesisRace : byte
    {
        Other = 0,          // Terrines, Lunarian/Martian/Venusian "LL." fighters, E.C. gennies, animals ...
        Human = 1,          // creation menu entry 0x6A2
        DesertRunner = 2,   // creation menu entry 0x6BC (forces the warrior career, claws: +0x2A = 3, +0x2C = 1)
        Tinker = 3,         // creation menu entry 0x6D6 (forces the medic career)
        Robot = 4,          // monsters only
        Special = 5,        // Leander and Zane
    }

    public static class GenesisRaces
    {
        public const int AbilityModTable = 0x63B;

        /// Racial modifiers added to STR, DEX, CON, INT, WIS (record +0x10..+0x14) at creation; races 1-3 (the table has no sane entries for the others).
        public static readonly sbyte[][] AbilityMods =
        {
            null,
            new sbyte[] { 0, 0, 1, 0, 0 },          // human
            new sbyte[] { 2, 2, 1, -1, 0 },         // desert runner
            new sbyte[] { -2, 3, -2, 0, 3 },        // tinker
        };

        public static string Name(int race)
        {
            switch (race)
            {
                case 1: return "Human";
                case 2: return "Desert runner";
                case 3: return "Tinker";
                case 4: return "Robot";
                case 5: return "Special";
                default: return "Other";
            }
        }

        /// The race icon the info panel draws (0x6F26): 0x33 + race, except race 5 which uses 0x45.
        public static int InfoIcon(int race) { return race == 5 ? 0x45 : 0x33 + race; }

        /// The death sound (0x11B40): 0x46 for robots, else 0x44 for base armor 0x32 (record +0x25) unless the token key (+0x42) is 0x1F or 0x13 (space rat, hypersnake), else 0x45.
        public static int DeathSound(int race, int baseArmor, int tokenKey)
        {
            if (race == 4) return 0x46;
            if (baseArmor == 0x32 && tokenKey != 0x1F && tokenKey != 0x13) return 0x44;
            return 0x45;
        }
    }

    /// ROM 0x1956E: damage to one crew member when the party's starship is hit at the controls (1d4 to the pilot, caller 0x19508) or at a ship system (1d10 to a random
    /// crew member, caller 0x19534). Only the party's ship hurts the crew; the enemy ship's hits skip this.
    public static class ShipHazard
    {
        /// The save target, a byte as in the ROM's `cmp.b`: 14 - level / 3, minus 1 for a desert runner, plus 3 for races above 2 (the word wraps for levels above 41).
        public static int SaveTarget(int level, int race)
        {
            int t = 14 - (level & 0xFF) / 3;
            if (race == 2) t -= 1; else if ((race & 0xFF) > 2) t += 3;
            return t & 0xFF;
        }

        /// Draws 1d`sides` (the damage), then the d20 save; the damage is avoided when the d20 is at least the target. The victim is `slots[index]` (hit points at +0x0E,
        /// status at +0); damage to zero or below puts it at 0 hit points with status 0x84 (down) and records the excess in `overkill[index]`. Returns true when no slot
        /// has a status above zero any more (the whole party is down: the ship is lost). The avoided path returns false without touching the slots.
        public static bool Apply(GenesisRng rng, int sides, int level, int race, byte[][] slots, int index, byte[] overkill)
        {
            int dmg = rng.Roll(sides) & 0xFF;
            int d20 = rng.Roll(20) & 0xFF;
            if (d20 >= SaveTarget(level, race)) return false;
            var s = slots[index];
            int left = (s[0xE] - dmg) & 0xFF;
            if (left == 0) { s[0] = 0x84; s[0xE] = 0; }
            else if (left < 0x80) s[0xE] = (byte)left;
            else { overkill[index] = (byte)(-left); s[0] = 0x84; s[0xE] = 0; }
            for (int i = 0; i < 8; i++) if ((sbyte)slots[i][0] > 0) return false;
            return true;
        }
    }
}
