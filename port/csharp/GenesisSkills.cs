// GenesisSkills.cs -- the character skill check of the Genesis game (ROM 0x4F20 -> 0x4F74, skill value 0x4FB0), ported and verified against the ROM
// (port/tests/ProgressTests.cs). A check turns the character's skill points, level and the matching ability score into a skill value, rolls a d100
// against it and reports a result level 0..3. Used by the SKILL script command, the backstab test and the defensive-posture test at the start of a round.
//
//   value  : points = record[0x31 + skill]; limit = level*2 (record +0x19); if points > limit: (points-limit)*2 + limit*8 else points*8;
//            then + ability score record[0x10 + ABILITY[skill]] (byte add); 0 when the character is not alive/available or has no points
//   result : target = (value*4) >> shift; d = target - d100;  d >= 0: success, critical (3) when d >= target/2 else 2;  d < 0: failure (1) when -d <= target else critical failure (0)
//   a result is only rolled when the low byte of the value is non-zero (a zero value uses no RNG)

using System;

namespace BuckRogersGenesis
{
    public static class GenesisSkills
    {
        public const int AbilityTable = 0x4FFC;

        /// record = the 214-byte character record; slotFlags0 = byte 0 of the combat slot whose index equals the record index (the ROM looks the slot up by index);
        /// skill = 0..13, shift = the second argument of the ROM routine (2 in all callers found: target = value).
        public static int SkillCheck(RomView rom, GenesisRng rng, byte[] record, int slotFlags0, int skill, int shift)
        {
            int value = SkillValue(rom, record, slotFlags0, skill);
            if ((value & 0xFF) == 0) return 0;                                  // tst.b d0 ; beq: no roll
            short d4 = (short)(value << 2);                                     // asl.w #2
            d4 = (short)(d4 >> (shift & 63));                                   // asr.w d3,d4
            int d0 = rng.Roll(100) & 0xFFFF;                                    // jsr 6D06
            int d1 = (short)(d4 - d0);
            if (d1 >= 0)
            {
                d4 = (short)(d4 >> 1);
                return d1 >= d4 ? 3 : 2;
            }
            d1 = (short)(-d1);
            return d1 <= d4 ? 1 : 0;
        }

        /// ROM 0x4FB0.
        public static int SkillValue(RomView rom, byte[] record, int slotFlags0, int skill)
        {
            int f = slotFlags0 & 0xFF;
            if ((f & 0x80) != 0 || f == 0 || (f & 0xC0) != 0) return 0;         // bmi ; beq ; andi.b #$C0
            int d4 = record[0x31 + (skill & 0xFF)];
            if (d4 == 0) return 0;
            int d0 = (record[0x19] << 1) & 0xFF;                                // asl.b #1
            if (d4 > d0) { d4 = (d4 - d0) & 0xFF; d0 = (d0 << 3) & 0xFF; d4 = ((d4 << 1) & 0xFF); d4 = (d4 + d0) & 0xFF; }   // sub.b ; asl.b #3 ; asl.b #1 ; add.b
            else d4 = d4 << 3;                                                  // asl.w #3
            int abil = record[0x10 + rom.Byte(AbilityTable + (skill & 0xFF))];
            d4 = (d4 & ~0xFF) | ((d4 + abil) & 0xFF);                           // add.b $10(a2,d0.w),d4
            return d4;
        }
    }
}
