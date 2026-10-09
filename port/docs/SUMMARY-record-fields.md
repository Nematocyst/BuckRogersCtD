# Summary: record fields +0x17, +0x26..+0x2D, +0x42, and the creation fix

Full detail: [findings-record-fields.md](findings-record-fields.md). All addresses are ROM offsets of the original USA/Europe ROM, read from the disassembly and run in the 68000 emulator.

## What was confirmed
* **Natural attacks (+0x26..+0x2D)**: two interleaved attacks (attacks x2, dice count, dice sides, flat bonus; first / second), copied to combat slot +6..+0xD by 0x6D1E. Attacks per round (0x107E8) = `((parity ^ attack number) + attacks x2) >> 1`; the "slot" in the formula is the attack number 0/1. Armed: item bytes 2..5 replace the first attack only. Unarmed: slot +0xC = record +0x2C + STR damage modifier, slot +3 gets the STR attack modifier (tables 0x7741; DEX twin 0x776F).
  Example: a disarmed Flavius (desert runner, STR 17) makes 2 attacks per round of 1d3 +2. Carlos Rioja (+0x26 = 1) attacks every second round. +0x2D is 0 on every record.
* **Race (+0x17)**: 0 other, 1 human, 2 desert runner, 3 tinker, 4 robot, 5 Leander/Zane. Reads: creation modifiers (0x650, table 0x63B, races 1-3 only), info panel icons (0x6F26), death sound (0x11B40), starship crew hazard (0x1956E), creation finish handler (0x810).
* **Created-character figure key (+0x42)**: written at **0x99E** (`move.b -$20(a6),$42(a1)`) by the figure screen (0x8F0): `0x80 | (row*6 + column)`. Created characters do not keep key 0, and sheets 2, 4, 6, 8, 9, 11 are creation choices. The earlier scan missed it because it is a memory-to-memory move.

## Answers to the open questions
1. **0x1956E = starship combat crew hazard.** Only when the party's ship is hit: at the controls the pilot takes 1d4, at a ship system a random crew member takes 1d10. Save: d20 >= 14 - level/3 (desert runner -1, race > 2 +3) avoids it.
2. **0x11B40 = death sound.** 0x46 robots, 0x44 people (base armor 0x32), 0x45 animals/others; keys 0x1F / 0x13 (space rat, hypersnake) count as animals. Then four death animation frames.
3. **Creation bug at 0x6A8: real.** Human clears name byte 12 instead of +0x2C, and neither human nor tinker resets +0x2A. The one-byte fix (0x6AB = 0x2C) only fixes the claws bonus; the **full fix** (`port/patches/creation_fix/creation_fix.ips`) resets both for Human and Tinker. Nothing relied on +0x0C being cleared.
4. **Unarmed damage confirmed** (see example above).
5. **STR tables**: damage -4..+10 and attack -5..+5 over STR 1..22, indexed by the signed STR byte.

## What changed in the repository (branch claude/festive-heisenberg-6setnq)
* `port/csharp/GenesisRaces.cs`: race enum, ability table, death sound, `ShipHazard` (verified against 1,500 vectors from the ROM routine); `tests/HazardTests.cs`, `tools/gen_hazard_vectors.py`; wired into `run_tests.sh` (all tests pass).
* Docs corrected for the figure key (07-graphics-scoping.md, TokenFrames.cs, PROJECT_STATE.md); `TokenFrames.CreationKey`.
* `port/patches/creation_fix/`: the human/tinker fix (IPS, builder, emulator test).

## Open
* Not play-tested in a full emulator session. Not traced: the 0x76A calls to 0x13E56 after the race handlers.
