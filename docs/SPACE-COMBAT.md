# Space encounters

Derived from the ROM code, **not play-tested**. Readings marked "inferred" come from how the code uses a value, not from observed behaviour. Script module 0x22 is the solar-system module; addresses are Genesis ROM offsets.

## How an encounter starts

The solar-map travel loop is at `0xBBA4`.

1. If the cooldown word `[0xBA62]` is non-zero, it counts down and no encounter happens.
2. Otherwise a d100 (`0x6D06`, 0..99) must be exactly 1, and a second d100 must be 10 or less. That is 1% × 11% = **0.11% per travel tick**. On success `[0xBA62]` = 300 and `[0x979B]` = 0. The real-time length of a tick was not measured.
3. Module 0x22, entry 2 (`0x6C31`), runs `ONGOTO [979B]`: 0 is the random encounter (`0x71C7`), 1–13 are scripted arrivals.

## Choosing the enemy (module 0x22 at `0x71C7`)

- Type = `RANDOM 7` (0..7). A result above 4 has 5 subtracted, so type 0: 25%, 1: 25%, 2: 25%, 3: 12.5%, 4: 12.5%.
- If any of the story flags `[9876]` (asteroid belt / message from Milo), `[9855]` (Gradivus Mons, Mars) or `[98B0]` (retinal lockpick, Venus RAM base) is set, the type is `RANDOM 1`, so 0 or 1 at 50% each.
- Crew: `RANDOM 4` indexes tables `0x7502` / `0x7507` and gives SPACECOMBAT operands 3 / 4 (`[9926]` / `[9925]`): (30,20), (40,18), (50,16), (60,14), (70,12), 20% each. `[9926]` is compared with a d100 before the enemy repairs a damaged system (`0x182EC`). `[9925]` feeds the enemy's to-hit (`0x18260`); lower is better for the enemy. These readings are inferred.
- Menu: HAIL (bluff = skill 9, intimidate = skill 2; intimidation always fails against type 3), ATTACK, FLEE (70% escape, otherwise a forced fight). A failed hail sets SPACECOMBAT operand 2 = 1; the effect was not traced.
- `SPACECOMBAT` is ECL opcode `0x1F` with four operands (type, unknown, crew, gunnery). Its handler at `0x3782` stores the type in `[9924]`.
- Salvage: `GETABLE [750C]` by type = 3, 10, 20, 12, 18 (the multiplier in "YOU SALVAGE n00 CREDITS").

## Faction

The faction is printed by the module-0x22 subroutine at `0x7382`, after "SENSOR ALERT!". It is fixed per ship type and exists only in the script text: 0 RAM scout, 1 RAM medium, 2 RAM heavy, 3 pirate medium, 4 Mercurian medium. No faction field was found in the ship record.

## Ship types

Records are at `0x17722`, 34 bytes each, built by `0x19C60`. Each has 4 weapon mounts of 5 bytes (id, count, count, ammo, unknown) from +`0x0C`.

| Type | Class (+`0x0A`) | Hull (+0) | Weapons | Names (`0xC8` + type × 5 + counter) |
| --- | --- | --- | --- | --- |
| 0 | Scout cruiser | 200 | 2 beam lasers, 2 missiles (8) | Van Allen, Hubble, Copernicus, Kepler, Goddard |
| 1 | Medium cruiser | 600 | 1 heavy accelerator gun (10), 1 K-cannon (6), 3 gyrocannons (150), 5 beam lasers | Adder, Viper, Anaconda, Sidewinder, Boomslang |
| 2 | Heavy cruiser | 2000 | 5 K-cannons (30), 5 gyrocannons (250), 5 pumped lasers, 5 heavy missiles (20) | Mohave, Sahara, Gobi, OutBack, Kalihari |
| 3 | Medium cruiser | 400 | 4 light accelerator guns (60), 5 pumped lasers, 4 heavy missiles (16) | Flare, Corona, Hermes, Osiris, Apollo |
| 4 | Medium cruiser | 800 | 4 beam lasers, 3 "missiles" with 150 ammo (looks like a gyrocannon entry) | Liberator, Eclipse, Daisy, Gambit, Dervish |
| 5 | Medium cruiser | 600 | 2 K-cannons (12), 2 missiles (8), 5 beam lasers | Maelstrom Rider (the party's ship, created at `0x1196`) |

Names cycle through the group with a per-type counter at `[0x9987 + type]`. After the fifth name, the number at record +`0x23` rises. The class reading of +`0x0A` is inferred: 0, 1, 2 match hull size and the scripted "scout" being type 0.

## Scripted space fights (module 0x22)

- Mars landing intercept, "by a scout": `SPACECOMBAT 0, 1, 50, 18` (type 0, crew 50 / 18).
- Pirate chase, "the next ship arrives in firing range": `SPACECOMBAT 1, 1, 75, 8` (type 1, the best crew; the RAM ships chasing the lone ship to Ceres).

## Crew hazard

When the party's ship is hit, a pilot takes 1d4 or a random crew member at a ship system takes 1d10, unless a d20 roll reaches 14 − level / 3. Races change this; see [RECORD-FIELDS.md](RECORD-FIELDS.md#race-uses).
