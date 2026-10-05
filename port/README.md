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

Run: `./run_tests.sh ROM.md` (needs `mono-mcs`); `./run_tests.sh ROM.md --regen` re-generates the vectors (needs `pip install unicorn`).
Vectors are random-state runs, so a C# port that matches them follows the ROM exactly, quirks included.

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
  damage is multiplied by 2; for a **rogue (career 4) the multiplier is (7 + level) >> 2** (levels 1-8: 2..3, 9: 4) and the rogue also
  halves nothing but lowers the rear armor by 2.
* **Damage**: sum of `count` rolls of 1..sides + signed-byte bonus (negative -> 0), x multiplier, stored in a byte (wraps above 255).
  **Rocket-class weapons (type 2) in the first gear slot**: if the TARGET's record flag +0x2F has bit 2 there is a 75% chance (else bit 1: 50%) of
  forced full damage 0xFF.
* **Attacks per round**: ((round parity & 1) ^ slotIndex) + attacksX2) >> 1, so an "attacks x2" of 5 gives 2 and 3 on alternate rounds.

## Not ported yet (found, not verified)
Initiative / turn order, movement points, monster AI target selection (0x15D8A onward), XP award and level-up (PROGRAM / training), loot, the
message ids 0x128/0x129/0x12A/0xBD/0xBE text, engine flags [0xD501..0xD503] and [0x97AE].
