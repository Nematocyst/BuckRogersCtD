// GenesisScriptChar.cs -- the character opcodes of the script engine whose effect is on the game state (the screen output of the ROM handlers is left to the host).
//   LOADCHARACTER n (0x0A, ROM 0x353A): [0x9DA7] = n (the "current character" the other opcodes use)
//   SAVECHARACTER   (0x44, ROM 0x3CC4): does nothing (a bare RTS)
//   SKILL skill, who, shift (0x22, ROM 0x38A2 -> 0x4E52 -> 0x4F20): operand 1 and 2 are values AND destinations. who = 0: the current character [0x9DA7] alone, otherwise the best of the
//       party's eight members. The result level 0..3 (GenesisSkills.SkillCheck, `shift` is its second argument, 2 in the game) is written to operand 2 and the index of the character who got it to operand 1.
//   PRINTSKILL (0x23, ROM 0x38A8): the same, with [0xB4C7] = 3 first and the result printed (host side).
// Party search (0x4F20): members are tried in order 0..7; one whose skill value is zero (dead, absent, no skill points) is skipped; otherwise it replaces the best so far when its result is
// greater OR EQUAL (a later member wins ties), so with everyone at 0 the last eligible member is named. Nobody eligible: the ROM stores a leftover register (0x336E, the dispatch table) through
// the record-index routine 0x6D0A, which gives 0x90 -- reproduced as NobodyIndex.
using System;

namespace BuckRogersGenesis
{
    public static class GenesisScriptChar
    {
        public const int CurrentCharacter = 0x9DA7, PrintSkillFlag = 0xB4C7, NobodyIndex = 0x90;

        /// records / slots = the party's 8 records and combat slots (slot i belongs to record i); current = [0x9DA7].
        public static int Skill(RomView rom, GenesisRng rng, byte[][] records, byte[][] slots, int current, int skill, int who, int shift, out int index)
        {
            if ((who & 0xFF) == 0)
            {
                int c = current & 0xFF;
                index = c;
                return GenesisSkills.SkillCheck(rom, rng, records[c], slots[c][0], skill, shift);
            }
            // The ROM's skill-value routine 0x4FB0 overwrites a0 (the slot pointer) with 0x4FFC once a member passes the flag test and has skill points, and the loop keeps adding 0x1A to it:
            // from then on the "slot flags" of the following members are ROM bytes at 0x4FFC + 0x1A * k. Reproduced (rpos = that address, -1 while a0 is still the real slot).
            int best = 0; index = NobodyIndex; int rpos = -1;
            for (int i = 0; i < 8; i++)
            {
                int flags = rpos < 0 ? slots[i][0] : rom.Byte(rpos);
                int value = GenesisSkills.SkillValue(rom, records[i], flags, skill);
                bool flagsOk = (flags & 0xC0) == 0 && flags != 0;
                if (flagsOk && records[i][0x31 + (skill & 0xFF)] != 0) rpos = 0x4FFC;           // lea $4ffc,a0 was executed
                if ((value & 0xFF) != 0)
                {
                    int r = GenesisSkills.SkillCheck(rom, rng, records[i], flags, skill, shift);
                    if (r >= best) { best = r; index = i; }
                }
                if (rpos >= 0) rpos += 0x1A;                                                    // adda.w #$1a,a0
            }
            return best;
        }
    }
}
