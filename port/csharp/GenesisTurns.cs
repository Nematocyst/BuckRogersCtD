// GenesisTurns.cs -- initiative and turn order of the Genesis combat engine, ported from the ROM (0x100D6 roll, 0xE3E6 pick) and verified
// against it (port/tests/ProgressTests.cs, vectors from the real routines run in a 68000 emulator).
//
// There is no fixed turn order and no "player phase / monster phase": the engine keeps a countdown word in every combat slot
//     slot +0x14 = speed (initiative points left)       slot +0x15 = a d100 tie-break rolled at the start of the round
// and repeatedly lets the live creature with the HIGHEST word (+0x14 high byte, +0x15 low byte) act (RAM [0xCA20] = that slot), until nobody has
// time left; then a new round starts: round counter [0xD50C] + 1, every slot rolls again. Actions reduce +0x14 elsewhere (movement and attacks).
//
//   RollInitiative  ROM 0x100D6 (0x6EBC): speed = INIT[DEX] + d10, -8 for the side that is surprised, minimum 2; tie-break = d100
//   BeginRound      ROM 0x100D6: everything a slot does at the start of a round (skill checks for the backstab mask and the defensive flag, attacks left,
//                   then the initiative roll)
//   PickNextActor   ROM 0xE3E6..0xE418: highest (speed, tie-break) among live slots with speed != 0; ties -> lowest slot index
//
// The side bit is slot byte +1 bit 0: SET = party, CLEAR = monsters. Surprise [0x9DC1]: 1 = party surprised (-8 for party slots),
// 2 = monsters surprised (-8 for monster slots).

using System;

namespace BuckRogersGenesis
{
    public static class GenesisTurns
    {
        public const int InitiativeTable = 0x776F + 0x17;   // second half of the table at 0x776F: initiative bonus by DEX (first half: DEX armor bonus)

        public struct Initiative { public int Speed; public int Tiebreak; }

        /// slotFlags1 = combat slot byte +1 (bit 0 = party side); dex = record byte +0x11 (a signed index, like the ROM); surprise = [0x9DC1].
        /// Consumes two RNG rolls (d10 then d100).
        public static Initiative RollInitiative(RomView rom, GenesisRng rng, int dex, int slotFlags1, int surprise)
        {
            int d2 = (short)(sbyte)rom.Byte(InitiativeTable + (sbyte)(byte)dex);         // ext.w of the table byte
            int roll = rng.Roll(10) & 0xFF;                                               // jsr 6C8C with d0 = 10
            d2 = (d2 & 0xFF00) | ((d2 + roll) & 0xFF);                                    // add.b
            bool party = (slotFlags1 & 1) != 0;
            if (party ? surprise == 1 : surprise == 2) d2 = (d2 & 0xFF00) | ((d2 - 8) & 0xFF);   // subq.b #8
            int speed = d2 & 0xFF;
            if ((sbyte)speed <= 1) speed = 2;                                             // cmp.b #1 ; bgt keep ; moveq #2
            return new Initiative { Speed = speed, Tiebreak = rng.Roll(100) & 0xFF };     // jsr 6D06 (d100)
        }

        /// Start-of-round processing of one combat slot (ROM 0x100D6; the UI call 0x664E is skipped). `slot` (26 bytes) and its monster/character `record`
        /// (214 bytes) are modified in place; `slotFlags0ByIndex[i]` = byte 0 of combat slot i (the skill check looks the slot up by the record's index, which for a
        /// monster is the slot of its type). Returns the new backstab mask bit state (the ROM sets bit <record index> of [0xD4FD] for party members whose
        /// skill 7 check reaches 2). Consumes 0-2 skill rolls, then the d10 and d100 of the initiative.
        public static int BeginRound(RomView rom, GenesisRng rng, byte[] slot, byte[] record, int recordIndex, int[] slotFlags0ByIndex,
            int surprise, int roundParity, bool flag97DCBit4, int backstabMask)
        {
            int f0 = slot[0];
            if (f0 == 0 || (f0 & 0xC0) != 0) return backstabMask;
            if (recordIndex < 8 && GenesisSkills.SkillCheck(rom, rng, record, slotFlags0ByIndex[recordIndex], 7, 2) >= 2)
                backstabMask |= 1 << recordIndex;
            slot[1] = (byte)(slot[1] & ~2);                                                  // bclr #1
            slot[0x16] = (byte)(slot[0xF] << 1);                                             // HP * 2
            slot[1] = (byte)(slot[1] & ~0x20);                                               // 0xE7DC: bclr #5
            if (flag97DCBit4 && GenesisSkills.SkillCheck(rom, rng, record, slotFlags0ByIndex[recordIndex], 4, 2) < 2)
            {
                slot[1] = (byte)(slot[1] | 0x20);
                slot[0x16] = (byte)((sbyte)slot[0x16] >> 2);
            }
            slot[0x18] = slot[6]; slot[0x19] = slot[7];                                      // attacks x2 -> attacks left (word copy)
            int par = roundParity & 1;
            slot[0x18] = (byte)((sbyte)(byte)(slot[0x18] + par) >> 1);
            slot[0x19] = (byte)((sbyte)(byte)(slot[0x19] + par) >> 1);
            var ini = RollInitiative(rom, rng, record[0x11], slot[1], surprise);
            slot[0x14] = (byte)ini.Speed; slot[0x15] = (byte)ini.Tiebreak;
            return backstabMask;
        }

        /// The next creature to act, or -1 when nobody has time left (end of round). Live = flags0 != 0 and (flags0 & 0xC0) == 0.
        /// Uses the ROM's signed 16-bit comparison, so a speed of 0x80 or more never wins.
        public static int PickNextActor(int slotCount, int[] flags0, int[] speed, int[] tiebreak)
        {
            int best = -1, d5 = -1;
            for (int i = 0; i < slotCount; i++)
            {
                int f = flags0[i] & 0xFF;
                if (f == 0 || (f & 0xC0) != 0) continue;
                if ((speed[i] & 0xFF) == 0) continue;
                short w = (short)(((speed[i] & 0xFF) << 8) | (tiebreak[i] & 0xFF));
                if (w <= d5) continue;                                                    // cmp.w d5,d0 ; ble
                d5 = w; best = i;
            }
            return best;
        }
    }
}
