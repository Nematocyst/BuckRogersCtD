# Record fields +0x17 / +0x26..+0x2D / +0x42: findings (2026-10-09)

Answers to `RECORD_FIELDS_BRIEF.md`. Everything below was read from the original ROM (disassembled with Capstone) and, where stated, run in `tools/emu68k.py`.
Addresses are ROM offsets of the unpatched ROM. The port changes are at the end; all checks are in `./run_tests.sh` (`tests/HazardTests.cs`).

## Answers

**1. What triggers 0x1956E?** Starship combat (module 0x17xxx..0x19xxx): the party's ship is hit and the hit lands on a location that has a crew effect. The hit routine is
0x19360 (location picked by 0x19670; message ids 0xF6..0xFF are "N pts of damage to <ship>'s hull / controls", "<system> destroyed on <ship>", "Engines Explodes!", "X takes N pts of damage ... and falls unconcious").
`d7` says whose ship is hit (0 = the party's ship at 0x98F6, else the enemy's at -0xBA(a6)); **enemy-ship hits skip the crew**.
* location 2 = **controls** (caller 0x19508): the crew member at the controls (`-0xD4(a6)`, skipped when negative) takes **1d4**, on every hit at the controls (not only when it is destroyed).
* location 4 = a **ship system** (caller 0x19534): a random crew member (rand 0..7; the slot must be alive, not negative, bit 6 clear) takes **1d10**.
* The save is as described: damage = 1d`sides` is rolled first, then a d20; **damage is avoided when d20 >= target**, target = `14 - level/3`, desert runner (race 2) `-1`, race > 2 `+3`
  (word arithmetic, compared as a byte). Level is record +0x19. A failed save subtracts the damage from slot +0x0E; reaching zero or below sets hit points 0, status 0x84 (down) and, if it went below zero,
  stores the excess at `-0x5A(a6, index)`. The routine returns nonzero when no slot has a status above zero any more (the caller then loses the ship). Port: `ShipHazard` in `GenesisRaces.cs`, 1,500 ROM vectors.
* Terrain, gas, vacuum or falling have nothing to do with it.

**2. 0x11B40 is the death sequence 0x11B30 (sound + animation).** `d0` is a **sound effect id** passed to 0x1B900 (valid ids 0..0x4B): **0x46 robots** (race 4), **0x44** when base armor (+0x25) is 0x32 (people) unless the token key (+0x42) is 0x1F or 0x13,
**0x45** otherwise (animals and armoured creatures). +0x42 = 0x1F / 0x13 are monster ids 31 (SPACE RAT) and 19 (HYPERSNAKE): both have armor 0x32 but are animals, so they get the creature sound. After the sound it runs the four death
draw states (0xCAEA with d0 = 1..4) with a delay of 0x14 ticks (record +0x23 < 2) or 0x23 ticks (tall / wide creatures) per state. Created characters (key 0x80..0x8B) never equal 0x1F / 0x13.
`GenesisRaces.DeathSound` encodes it.

**3. The creation bug at 0x6A8 is real; the one-byte fix works.** The race menu is a jump table at 0x606 (word offsets from 0x606): human 0x6A2, desert runner 0x6BC, tinker 0x6D6; careers 0x714 / 0x728 / 0x73C / 0x750; sex 0x690 / 0x698.
The desert runner and tinker handlers end by jumping into a career handler (0x73C = warrior, 0x728 = medic), the human handler does not. Run in emulator (record with +0x26..+0x2B = 02 00 01 00 02 00, name bytes 0..12 marked):

| sequence | +0x26..+0x2D | +0x0C |
|---|---|---|
| desert runner | 04 00 01 00 **03** 00 **01** 00 | untouched |
| desert runner, then human (unpatched) | 04 00 01 00 **03** 00 **01** 00 (claws +1 and 1d3 stay) | cleared |
| desert runner, then human (0x6AB = 0x2C) | 04 00 01 00 03 00 **00** 00 | untouched |
| desert runner, human, tinker | 02 00 01 00 03 00 00 00 (tinker clears +0x2C, keeps +0x2A = 3) | |

* **Patch: ROM 0x6AB: 0x0C -> 0x2C** (`42 2A 00 0C` becomes `42 2A 00 2C`, i.e. `clr.b $2C(a2)`). Nothing relies on +0x0C being cleared: the finish handler (0x7C8) copies only **11 name bytes** (+0x00..+0x0A) from the name buffer
  into the already-cleared record, and +0x0C is not read by any creation code.
* The patch fixes the claws bonus only. **+0x2A (dice sides) stays 3** after desert runner -> human (and -> tinker), because no handler resets it to 2; the finish handler (0x810..0x81E) only ever sets it to 3 (`career == 3 or race == 2`).
  A non-warrior human made by switching away from desert runner therefore keeps 1d3 fists. The full fix is built: `patches/creation_fix/` (both handlers call a 14-byte routine at 0xF6000 that clears +0x2C and sets +0x2A = 2; tested in the emulator on seven race-switch sequences).
* Also visible in the run: switching race first subtracts the old race's ability modifiers (0x650 with d1 = 1), then adds the new ones. The table at 0x63B holds 5 signed bytes per race for races 1..3 only
  (human 00 00 01 00 00, desert runner 02 02 01 FF 00, tinker FE 03 FE 00 03); the entries for races 0 and 4 overlap code and are never used (a new record starts as race 1).

**4. Unarmed damage: confirmed.** 0x6D1E: `move.l $26(a1),$6(a0)` and `move.l $2A(a1),$A(a0)` copy the block into slot +6..+0xD; with nothing in +0xAE (0x6D86) `bsr 0x6E9E; add.b d0,3(a0); add.b d1,$C(a0)`,
so **slot +0x0C = record +0x2C + STR damage modifier** and slot +3 gets the STR attack modifier; the attack roll then adds slot +0x0C (`add.b $c(a3,d4.w),d0` at 0x1082E, clamped at 0, then times the multiplier).
Example, run in the emulator on the default party record with +0xAE..+0xB7 cleared (routine 0x6D1E):
* **Flavius** (desert runner, STR 17): slot +6 = 4 (**2 attacks every round**), 1d3 (+8 = 1, +0xA = 3), +0xC = 1 (claws) + 1 (STR 17) = **2**, attack value 0x2A (with his default weapon, item 9, he has slot +6 = 6, 1d8 +1 and attack value 0x2B).
* **Celeste** (STR 16): 2 attacks of 1d3 + (1 + 1) = +2, attack value 0x29. **Pierre** (human, STR 14): 1 attack of 1d2 + 0, attack value 0x28. Nichole, Roarke, Janelle: 1 attack of 1d2 + 0.
* The second attack (+7, +9, +0xB, +0xD) is zero on all of them; only a few monsters use it (sand squid, gennies, hornet: +0x27 = 2).
Note that the armed case replaces only the **first** attack's slot +6, +8, +0xA, +0xC from item bytes 2..5; the second attack keeps the record's values.

**5. 0x6D86 / 0x6E9E.** `0x6E9E` indexes by the **signed STR byte (record +0x10)**: `d1 = table[0x7741 + STR]` is the damage modifier, `d0 = table[0x7741 + 0x17 + STR]` the attack modifier (a 0x17-byte offset between the two arrays; 23 entries each):

| STR | 0 | 1 | 2 | 3 | 4 | 5 | 6-7 | 8-15 | 16 | 17 | 18 | 19 | 20 | 21 | 22 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| damage (d1) | 0 | -4 | -3 | -2 | -1 | -1 | 0 | 0 | +1 | +1 | +2 | +4 | +6 | +8 | +10 |
| attack (d0) | 0 | -5 | -4 | -3 | -2 | -2 | -1 | 0 | 0 | +1 | +2 | +3 | +3 | +4 | +5 |

The ranged twin is `0x6EBC` with **DEX (+0x11)**: table 0x776F, d1 = armor bonus (added to slot +4), d0 = attack modifier; it is used instead of the STR routine when the weapon's type byte (item byte 1) is nonzero.
`GenesisStats.RecomputeSlot` already does all of this; a test now pins the disarmed-Flavius numbers.

## Findings to confirm (section A-C of the brief)

* **A. Natural attacks: confirmed.** Layout +0x26/27 attacks x2, +0x28/29 dice count, +0x2A/2B dice sides, +0x2C/2D flat bonus (attack 0 / attack 1), copied to slot +6..+0xD at 0x6D24/0x6D2A.
  Values over the 54 monster records: +0x2C = 1 for desert runners (ids 0, 43, 59, 106), 2 for Terrines, CGennies and Talon (ids 20-22, 32, 41, 58), 0 otherwise; **+0x2D is 0 everywhere**.
  Attacks per round (0x107E8): `((round parity ^ d4) + slot[6 + d4]) >> 1`, where **d4 is the attack number (0 or 1), not the creature's slot index** (the C# parameter was misnamed `slotIndex`, now `attackNo`).
  It reads slot +6 (+7 for the second attack), which is the record's +0x26 when unarmed: Carlos Rioja (+0x26 = 1) attacks every second round. Dice, sides and flat bonus are read at +8/+0xA/+0xC (+d4) in 0x10822..0x1082E.
* **B. Race +0x17: confirmed**, with these reads (0..5 as in the brief; the enum is `GenesisRace`): 0x650 creation modifiers, 0x6F26 info panel (sex icon 0x32 + sex, race icon 0x33 + race, race 5 -> 0x45, career icon 0x1F + career),
  0x11B40 death sound (race 4 -> 0x46), 0x1956E crew hazard save, and the creation finish handler (0x810..0x81E: race 2 or career 3 sets +0x2A = 3). Not traced: 0x76A, which after each race handler calls 0x13E56 with 3, 6, 5 (tinker) or 6, 4 (desert runner), probably a menu / highlight update.
* **C. Creation figure key: confirmed, and the store is found.** The figure screen is 0x8F0..0x9A4: a loop at 0x90E lays out the 2 x 6 grid, 0x950..0x96E reads the cursor (`[0xD594]` via menu routine 0x13C9C) into `-0x20(a6)`, `bset #7` at 0x970,
  and **0x99E: `move.b -$20(a6),$42(a1)`** stores it into the character being made (record pointer from 0x6F14 with index [0xCA20]). It is a plain `move.b d16(a6),d16(a1)`, a memory-to-memory move, which a scan for register stores does not show.
  `docs/07-graphics-scoping.md`, `TokenFrames.cs` and `PROJECT_STATE.md` are corrected; `TokenFrames.CreationKey(row, column)` is new.

## Port changes

* `csharp/GenesisRaces.cs` (new): `GenesisRace` enum (0 other, 1 human, 2 desert runner, 3 tinker, 4 robot, 5 special), `GenesisRaces` (ability-modifier table, names, info icon, death sound), `ShipHazard` (0x1956E).
* `tools/gen_hazard_vectors.py` + `tests/hazard_vectors.txt` (1,500 cases from the real routine with its screen helpers stubbed) and `tests/HazardTests.cs` (also: the ROM race table bytes, the disarmed-Flavius slot, attacks per round); wired into `run_tests.sh`.
* `GenesisCombat.AttacksThisRound`: parameter renamed `attackNo`. `TokenFrames`: corrected creation-key note, `CreationKey`.
* Not changed: `GenesisStats.RecomputeSlot` (it already used the +0x26..+0x2D layout and the armed / unarmed split as described).
