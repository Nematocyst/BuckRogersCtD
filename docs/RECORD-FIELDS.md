# Character and monster record (214 bytes)

One record layout is used for the monsters in the ROM monster file (`0x9E77C`, 54 records), for the party in work RAM (`0xFFBA68`, 6 records) and for the pregenerated characters (8 records at `0x6BAAD`, loaded by the code at `0x1F32`). Multi-byte values are big-endian.

The **DOS** column gives the matching offset in the DOS version's `MON0CHA.DAX`, where records are a fixed 263 bytes. DOS values are little-endian.

**Status** tells you how a field was established:

| Tag | Meaning |
| --- | --- |
| live | changed in an emulator and the effect observed |
| port | taken from a separate C# reimplementation of the game logic that was checked against the ROM's own code in an emulator (millions of random test cases) |
| static | the same pattern in all 54 monster records or all party records |
| DOS | agrees with the DOS data in 52–53 of 53 records |
| code | read from the 68000 code (addresses given) |

## Field map

| Offset | Field | DOS | Status |
| --- | --- | --- | --- |
| +0..15 | name, NUL-padded | +1.. (Pascal string) | verified |
| +16..20 | STR, DEX, CON, CHA, TECH | +16, 17, 18, 21, 22 | verified (on-screen order; DEX live) |
| +21 (`0x15`) | saving-throw target: d20 + modifier is compared with it. 10 for almost everyone, 9 for Talon and Buck Rogers, 14 for the party | +29 | port |
| +22 | gender: 0 male, 1 female | +38 | static (party) |
| +23 (`0x17`) | race: 0 other (Terrines, Lowlanders, CGennies, all animals), 1 human, 2 desert runner, 3 tinker, 4 robot, 5 Leander/Zane. See [Race uses](#race-uses) | +39 | code |
| +24 | career: 0 engineer, 1 rocket jock, 2 medic, 3 warrior, 4 rogue | +40 (DOS codes differ: 4 engineer, 5 rogue) | verified |
| +25 | level | +41 | verified |
| +26..29 (`0x1A`) | credits carried (u32). Added to the party purse when the monster is defeated | ~+42..45 | port |
| +30..33 | XP total (u32) | +47 (little-endian) | verified |
| +34 (`0x22`) | attack value, set by career and level from the table at `0x0BB0`. Copied to combat slot +3 | +51 | live, port, code |
| +35 (`0x23`) | size type: 1 normal, 2 tall (1×2 cells), 3 wide (2×1) | +52 | port |
| +36 | movement. Character creation: human 8, desert runner 10, tinker 6 | +54 | live, code |
| +37 | base armor | +55 | live |
| +38..45 (`0x26`..`0x2D`) | natural attacks, two interleaved. See [Natural attacks](#natural-attacks) | +59..64 | code, static |
| +46 | hit points. In a fight the live value is combat slot +`0x0E` | +69 | verified |
| +47 (`0x2F`) | flags: bit 2 = rockets are special against this creature 75% of the time, bit 1 = 50% | none | port |
| +48..52 | career skills: +48 + career code (48 jury-rig, 49 pilot rocket, 50 treat wounds, 51 tactics/leadership, 52 bypass security) | none | verified (training), port (+50 = medicine) |
| +53..62 | general skills, in skill-screen order: zero-G, perception, demolition, stealth, climbing, fast talk, first aid (+59), repair rocket, library search, programming | none | likely (order from ROM `0x1243E`; +59 confirmed by the port) |
| +63 (`0x3F`) | ammo saving: a monster keeps a thrown charge unless a random number below this is 0 | none | port |
| +64..65 | XP reward (u16) | +178 (little-endian) | verified |
| +66 (`0x42`) | party figure key: `0x80` + sheet number (table `0x998C`). The creation screen's 2×6 figure grid writes `0x80 + row*6 + column`. Monsters use bit 7 clear and a different table (see [GRAPHICS.md](GRAPHICS.md)) | none | verified |
| +67..76 (`0x43`..`0x4C`) | special-effect list, 10 bytes. See [Special effects](COMBAT-RULES.md#special-effects) | none | port |
| +77..81 | warrior weapon specializations: one weapon id per pick (levels 2, 4, 6, 8…). The same id up to 3 times gives +3 (the manual's cap) | +186.. | verified on a trained party (09 / 09 09 / 09 09 09, then `0E` once the laser pistol was maxed) |
| +82 (`0x52`) | bit 0 = gives no XP when defeated (NEO allies, duplicates, named NPCs) | +195 | port |
| +84.. | 13 gear slots × 10 bytes (in RAM copies the item area moves). +`0xAE` = readied weapon slot | `MON0ITM.DAX` | verified |

Gear slots use the item record layout from [ROM-FORMATS.md](ROM-FORMATS.md#item-records): id, three word indexes, racial tier, price, and so on.

## Natural attacks

Offsets +`0x26`..`0x2D` hold two attacks, interleaved (first attack / second attack):

| Offset | Content |
| --- | --- |
| +`0x26` / `0x27` | attacks × 2 (2 = one attack per round, 1 = one every second round) |
| +`0x28` / `0x29` | number of damage dice |
| +`0x2A` / `0x2B` | die sides |
| +`0x2C` / `0x2D` | flat damage bonus |

The fight-setup routine at `0x6D1E` copies this block as-is into combat slot +6..+`0x0D` (two `move.l` at `0x6D24` and `0x6D2A`), then checks the readied-weapon slot (+`0xAE`):

- **No weapon** (`0x6D86`): the record's values stay. A strength modifier is added to slot +3 (to-hit) and slot +`0x0C` (damage bonus). Tables `0x7741` (STR) and `0x776F` (DEX): damage −4…+10 and attack −5…+5 over STR 1..22.
- **Weapon** (`0x6D98`): the weapon's stat-table values (attacks × 2, dice, sides, bonus) overwrite the first attack (slot +6, +8, +`0xA`, +`0xC`). The second attack (slot +7, +9, +`0xB`, +`0xD`) keeps the record's values.

So **+`0x2C` is the flat damage bonus of the first natural attack, used only when the fighter has no readied weapon**. +`0x2D` is the same for the second attack and is 0 on every record, so it has no effect in the shipped game.

Observed values:

- Desert runners have +`0x2C` = 1 (the manual's "claws, +1 damage bare-handed"): D.R. Warrior, Tuskon, Beowulf, Flavius, Celeste.
- Terrines, the RAM CGennies (Martian, Mercurian, Venusian) and Talon have 2.
- Everyone else has 0. All the ones with a bonus carry weapons, so the bonus only shows when they are disarmed.
- Animals use the dice fields instead, with no bonus: Sand Squid 2 × 1d4 plus 1 × 1d2, Acidicium 2 × 2d8 plus 1 × 1d2.
- Their dice are summed into one damage roll, unlike weapon attacks, which roll each hit separately.
- Example: a disarmed Flavius (desert runner, STR 17) makes 2 attacks of 1d3 + 2 per round.
- A record with +`0x26` = 1 (Carlos Rioja) attacks every second round. This is the "attacks × 2" rule, not a missing attack.

Attacks per round (`0x107E8`) = `((round parity & 1) ^ attack number) + attacks×2) >> 1`, where the attack number is 0 or 1. An "attacks × 2" of 5 gives 2 and 3 attacks on alternate rounds.

### Character creation values and the fixed bug

Creation (`0x590`..`0x81E`) starts from 1 attack of 1d2 (fists). Warriors get 2 attacks (+`0x26` = 4). A desert runner sets +`0x2A` = 3 and +`0x2C` = 1. A tinker clears +`0x2C`. Warriors and desert runners finish with 1d3.

**Original bug, confirmed live:** the human handler (`0x6A8`) clears +`0x0C` (a name byte) instead of +`0x2C`, and neither the human nor the tinker handler resets +`0x2A`. So switching from desert runner to human keeps the claw bonus and 1d3; to tinker keeps 1d3. The v1.0 patch fixes it; see [PATCH-NOTES.md](PATCH-NOTES.md#character-creation-fix).

## Race uses

| Where | What |
| --- | --- |
| `0x650` (creation) | racial ability modifiers: table at `0x63B`, 5 signed bytes per race, races 1–3 only |
| `0x6F26` (combat info panel, controlled fighters only) | icons through `0xA85A`: sex `0x32 + sex`, race `0x33 + race` (race 5 has its own icon `0x45`), career `0x1F + career` |
| `0x11B40` | death sound: `0x46` robots, `0x44` people (base armor `0x32`), `0x45` animals and others. Keys `0x1F` and `0x13` (space rat, hypersnake) count as animals |
| `0x1956E` | starship combat crew hazard: when the party's ship is hit, the pilot takes 1d4 or a random crew member at a ship system takes 1d10. Avoided if d20 ≥ 14 − level/3. Desert runner −1 (easier), races 3/4/5 +3 (harder) |
| `0x810` | creation finish handler |

Race 0 is a generic "other", treated like human. Races 4 (robots) and 5 (Leander, Zane) have real effects.

## Created-character figure key

The key at +`0x42` is written at `0x99E` (`move.b -$20(a6),$42(a1)`) by the figure screen (`0x8F0`): `0x80 | (row*6 + column)`. It is a memory-to-memory move, which is why a scan for stores missed it at first. Created characters therefore do not keep key 0, and sheets 2, 4, 6, 8, 9 and 11 are creation choices. The pregenerated party carries its keys in the data; see [GRAPHICS.md](GRAPHICS.md#party-figure-keys).

## Combat slots

During a fight each creature has a 26-byte combat slot, built from its record by `0x6D1E`. In work RAM, party slots start at `0xFFC470` and enemy slots at `0xFFC540`. Older emulator-based notes read RAM through a word-swapped view and list some of these fields one byte off (for example HP at +15). The numbering below follows the ROM code.

| Slot offset | Content |
| --- | --- |
| +0 | status. A creature is alive when the byte is non-zero and bits 6 and 7 are clear. `0x82` down, `0x83` dying, `0x84` unconscious, `0x85` fleeing off the map, `0x86` killed outright (death touch), `0x87` dead. Bit 6 is set on creatures that found no room at deployment |
| +1 | flags: bit 0 = party, bit 2 = hidden (creature removed from the board), bit 4 = reaction ready (`0x10`), bit 6 = ally, bit 7 = computer-controlled (`0x80`, set by "auto" / Quick Combat) |
| +3 | attack value |
| +4 / +5 | armor from the front / armor from the rear |
| +6..+`0x0D` | the two attacks: attacks × 2, dice, sides, flat bonus for attack one (+6, +8, +`0xA`, +`0xC`) and attack two (+7, +9, +`0xB`, +`0xD`) |
| +`0x0E` | hit points |
| +`0x0F` | movement |
| +`0x10` | facing, 0..7 |
| +`0x11` | sprite chunk index for the creature's type |
| +`0x14` | speed countdown word (0 = done, 1 = has waited once) |
| +`0x15` | d100 tie-break |
| +`0x16` | movement points for the round (= movement × 2 at round start) |
| +`0x17` | current target |
| +`0x18` / +`0x19` | attacks left |

Dying party members (status `0x83`) count up in their HP byte and are dead (`0x87`) after 16 rounds.

## Open fields

Fields not listed above have not been identified. The gear-slot bytes +5 and +8 are preserved by the monster tool but their meaning is unknown. See [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md).
