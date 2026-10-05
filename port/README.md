# 68000 -> C# ports of Buck Rogers: Countdown to Doomsday (Genesis) game logic

Each routine below was read from the ROM, ported to dependency-free C# (`csharp/`) and **verified bit-for-bit against the real ROM
code**, which is executed in a 68000 emulator (Unicorn) to produce test vectors (`tests/*.json`). The C# tests replay those vectors.

| C# | ROM | What it does | Checks |
|---|---|---|---|
| `GenesisRng.Next / Roll / ScriptRandom / FromRom` | 0x6C94, 0x6C8C, seeding 0x12C8, opcode RANDOM 0x351E | the game's RNG: 256-word lagged-Fibonacci table, seeded from ROM words at 0x390E2 | 3,863 |
| `GenesisCombat.Octant` | 0x15D36 | direction 0..7 between two cells (uses tan 67.5 deg = 0x26A/256) | 4,912 vectors |
| `GenesisCombat.ArmorAgainst` | 0x10666 | armor by attack direction: front / flank (-2) / rear (rear armor), turn-to-face, rogue -2 and the rear-attack damage multiplier | 1,200 vectors x 5 fields |
| `GenesisCombat.ToHit` | 0x1056A | range penalties (-2 / -5), weapon-class refusals, clamp to 1..19 (x5 = displayed %) | 3,000 vectors |
| `GenesisCombat.AttacksThisRound` | 0x107E8 | attacks per slot per round from "attacks x2" and round parity | 600 vectors |
| `GenesisCombat.ResolveAttack` / `SpecialEligible` | 0x10804, 0x1062C | d20 vs to-hit, weapon dice, signed-byte bonus, multiplier, full-damage special; consumes the RNG exactly like the ROM | 1,500 attacks, incl. final RNG state |

| `GenesisProgression.HpAfterLevelUp / AttackValue / Threshold / ScanTraining / AwardXp` | 0x0B1C, 0x0B92, 0x7418, 0x3C0A | level-up HP, attack value table, XP thresholds, who can train (one level per session), party XP award (ADDEP) | 17,089 |
| `GenesisRewards.Tally / AddItem` | 0x1631E, 0x16268 | victory tally: XP pool divided by living party, credits, gear drops into the 14-entry loot pool with stacking | 62,911 |
| `GenesisSkills.SkillCheck` | 0x4F20 / 0x4F74 / 0x4FB0 | skill value and d100 check, result 0..3 | 3,000 vectors |
| `GenesisTurns.BeginRound / RollInitiative / PickNextActor` | 0x100D6, 0xE3E6 | start-of-round processing of a slot (two skill checks, attacks left, initiative) and the next-actor pick | 3,000 + 1,500 vectors |
| `GenesisAi.FindPath / Occupant` | 0x15D8A, 0x1432E | monster target selection: breadth-first search over the 21x21 combat grid (3 modes) | 600 maps, 373 with a goal |

Run: `./run_tests.sh ROM.md` (needs `mono-mcs`); `./run_tests.sh ROM.md --regen` re-generates the vectors (needs `pip install unicorn`).
Tables the ports read (hit dice, XP thresholds, item categories, initiative bonus ...) come through `RomView`: `RomView.FromRom(rom)` in tests, or
`RomView.FromSegments` from `rom_tables.json` (2.4 KB, made by `tools/export_rom_tables.py`; Unity loader `csharp/unity/GenesisRomTablesLoader.cs`).
Vectors are random-state runs, so a C# port that matches them follows the ROM exactly, quirks included.

## Turn order, initiative, skills (ROM 0x100D6, 0xE3E6, 0x4F20)
* **There is no fixed order.** Every combat slot has a countdown word (+0x14 speed, +0x15 a d100 tie-break). The engine keeps picking the live
  creature with the highest word, lets it act (RAM [0xCA20] = that slot), and when nobody has time left starts a new round ([0xD50C] + 1) and re-rolls.
  The party acts in the order the player chooses from the menu, but only among creatures whose word is currently highest.
* **Initiative** = `INIT[DEX]` (table 0x7786, DEX 14 -> +? see table) + d10, -8 for the surprised side, minimum 2; tie-break d100; ties go to the lower slot.
  **Slot byte +1 bit 0 SET = party.** `[0x9DC1]` = 1 delays the party, 2 delays the monsters (the earlier poke test results now make sense).
* **Round start also**: sets slot +0x16 = HP*2 (quartered when the character fails skill 4 and [0x97DC] bit 4 is set), copies "attacks x2" to the
  attacks-left bytes (+0x18/+0x19, halved with the round parity), and sets bit <slot> in the backstab mask [0xD4FD] for party members whose
  **skill 7 check reaches 2** (that is what makes backstabs possible; the rogue multiplier is in `ArmorAgainst`).
* **Skill check**: value = points*8 (or (points - 2*level)*2 + 16*level when points exceed 2*level) + the ability tied to the skill (table 0x4FFC);
  target = value*4 >> shift (shift 2 everywhere found, so target = value); roll d100: success when roll <= target (critical when the margin is at least
  half the target), failure otherwise (critical failure when it misses by more than the target). No roll if the value's low byte is 0.

## Level-up and training (ROM 0x0B1C, 0x0B92, 0x7418)
* HP per level = max(1, d(hit die) + CON bonus) + 4, hit die 4/2/6/4 for rocket jock / medic / warrior / rogue; the CON bonus (table 0x0B7C,
  -3..+6) is capped at +2 except for warriors; max HP is capped at 104 (0x68) and the byte wraps before the cap check (HP 250 + gain behaves oddly).
* Attack value (+0x22) by career and level from table 0x0BB0; warrior level 2 = 41 as in the pregenerated party.
* XP thresholds: table 0x0C16, 8 u32 per career (1250/2500/5000/10000/20000/40000/70000 for rocket jock, medic 1500..96000, warrior 2000..125000,
  rogue as rocket jock), the eighth is the 0xFFFFFFFF cap (level 8). Training scans the party: ready characters keep XP up to the next-next threshold - 1,
  i.e. at most one level per training session. `ADDEP n` gives the amount to every present party member that is not down.

## Victory rewards and loot (ROM 0x1631E, 0x16268)
* For every defeated enemy: its XP (+0x40, unless record byte +0x52 bit 0) goes into a pool; the pool is divided by the number of living party
  members (16-bit division: if the quotient does not fit, the low word of the pool remains). Its credits (+0x1A, u32) are added to the party purse
  [0xBA34], and its gear (13 slots from +0x54, built-in items excluded) goes to the loot pool. Not applied when [0xBA5B] or [0x97AE] is set.
* Loot pool = 14 entries of 10 bytes at 0x6AF6. The category table at 0xA829 decides per item id: negative = drops, 0 = never drops (knives!), positive =
  stands for the base item at 0xF17CC + 10*value. Identical items (same id and byte +4) stack: counted items (quantity bit 7) +1, others add the quantity and cap
  at 100; a new item without quantity gets 0x81. The displayed 6,250 credits of the first fight were not reproduced (record credits sum to 448, so the rest must be
  item value or a script bonus: not investigated).

## Monster target selection (ROM 0x15D8A)
* It is a **breadth-first search** over the 21 x 21 combat grid from the acting creature. Mode 0 ([0xD505] = 0): the first cell whose live occupant is on the other side
  becomes the target (stored in slot byte +0x17) and the path is returned as direction codes (0..7 over the table at 0x146E0); modes 1 and 2 walk to a fallen ally
  (the creature in +0x17, or those listed at [0xCA22]) -- the healer logic. Neighbours are tried in an order that rotates with a wave counter that the ROM never initialises
  (an uninitialised stack value), so ties between equally near targets are broken by whatever was on the stack: the port takes it as an input.
* Passability: no creature marker (bit 7 of the tile byte) and terrain flag bit 5 clear; tall creatures (record +0x23 = 2) also need the cell south free, wide ones (3) the cell east.
* The BFS gives up if its 1,500-entry queue overflows. The ROM clears only 440 of the 441 visited bytes (the last cell keeps old data).

## Findings that matter for the port / a difficulty patch
* **RNG is deterministic**: the table starts from 256 words of ROM data at 0x390E2 (bytes inside an event-script blob, so editing that
  script changes every random roll in the game). The only other writer is the vblank handler, which adds [0xCA21] (0 in normal play) to
  the high byte of T[0].
* **Quirks kept on purpose**: `RANDOM 255` always returns 0 and does not advance the RNG (operand+1 = 256 has a zero low byte); an argument
  whose low byte is >= 128 makes a huge unsigned divisor and returns the raw word (negative after sign extension if >= 0x8000).
* **To-hit**: roll d20 (1..20); hit when roll <= value, value = clamp(attack - armor + 20, 1, 19) after range penalties: weapons with range > 1
  lose 2 once the target is beyond half the range, 5 beyond three quarters; 20 always misses (95% cap). Weapon type 3 and type 2 / modifier 5..12
  weapons can refuse to fire (messages 0xBD / 0xBE) depending on engine flags [0xD502] / [0xD501] (meaning not decoded).
* **Armor by direction**: bearing minus target facing (mod 8): 0,1,7 front = armor (+4); 2,6 flank = armor-2; 3,4,5 rear = rear armor (+5).
  An idle enemy target (flags & 6 == 0) turns to face its attacker first, so only already-engaged targets can be flanked.
* **Backstab**: on a rear attack by a party member listed in the mask [0xD4FD] who is unarmed or holds a melee weapon (weapon type 0) the
  damage is multiplied by 2; for a **rogue (career 4) the multiplier is (7 + level) >> 2** (levels 1-4: 2, 5-8: 3, 9: 4) and the rear armor
  value used against the target is also 2 lower.
* **Damage**: sum of `count` rolls of 1..sides + signed-byte bonus (negative -> 0), x multiplier, stored in a byte (wraps above 255).
  **Rocket-class weapons (type 2) in the first gear slot**: if the TARGET's record flag +0x2F has bit 2 there is a 75% chance (else bit 1: 50%) of
  forced full damage 0xFF.
* **Attacks per round**: ((round parity & 1) ^ slotIndex) + attacksX2) >> 1, so an "attacks x2" of 5 gives 2 and 3 on alternate rounds.

## Not ported yet (found, not verified)
The action cost that counts +0x14 down (movement and attack handlers around 0xE4F0, 0xF2AE, 0xEF64), what a creature does once it has a target (attack vs
move choice, spells and abilities), the `ad5a`-style animations, the remaining uses of [0xD501..0xD503] and [0x97AE], the text behind message ids
0x128/0x129/0x12A/0xBD/0xBE, and the scripted-XP table at 0x16402 (the tally takes the scripted bonus as a parameter).
