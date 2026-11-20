# Combat and progression rules

How the Genesis game resolves fights, levels and loot, with the ROM address of each routine. The rules come from a C# reimplementation of the game logic, called "the port" below. Each routine was read from the ROM, ported, and checked against the ROM's own code run in a 68000 emulator on random inputs. A port that matches those vectors follows the ROM exactly, quirks included. Numbers of checks are given where they are known.

Fields of the records the rules read are in [RECORD-FIELDS.md](RECORD-FIELDS.md). Script variables such as `[0x9DC1]` are RAM addresses; see [SCRIPT-VARIABLES.md](SCRIPT-VARIABLES.md).

## Contents

1. [Random numbers](#random-numbers)
2. [Turn order and initiative](#turn-order-and-initiative)
3. [Skill checks](#skill-checks)
4. [To-hit](#to-hit)
5. [Armor by direction and backstab](#armor-by-direction-and-backstab)
6. [Damage](#damage)
7. [Special effects](#special-effects)
8. [Level-up, training and XP](#level-up-training-and-xp)
9. [Victory rewards and loot](#victory-rewards-and-loot)
10. [How the computer plays a turn](#how-the-computer-plays-a-turn)
11. [Explosives](#explosives)
12. [Start of a fight](#start-of-a-fight)
13. [Quirks and bugs of the original](#quirks-and-bugs-of-the-original)

## Random numbers

- The game's RNG (`0x6C94`, roll helper `0x6C8C`) is a 256-word lagged-Fibonacci table, seeded from 256 words of ROM data at `0x390E2` (seeding routine `0x12C8`). The script opcode `RANDOM` is at `0x351E`.
- The seed words are bytes inside the compressed script module 00. **Never edit module 00 in place**: changing it changes every random roll in the game.
- The only other writer is the vertical-blank handler, which adds `[0xCA21]` (0 in normal play) to the high byte of the first table word.
- `RANDOM 255` always returns 0 and does not advance the RNG (operand + 1 = 256 has a zero low byte). An argument whose low byte is 128 or more makes a huge unsigned divisor and returns the raw word.

## Turn order and initiative

- There is no fixed order. Every combat slot has a countdown word (+`0x14` speed, +`0x15` a d100 tie-break). The engine keeps picking the live creature with the highest word, lets it act (`[0xCA20]` = that slot), and when nobody has time left starts a new round (`[0xD50C]` + 1) and rolls again. The party acts in the order the player chooses, but only among creatures whose word is currently highest.
- **Initiative** = `INIT[DEX]` + d10, minus 8 for the surprised side, minimum 2. Table `0x7786`: DEX 1–5 = −6, −4, −3, −2, −1; DEX 6–15 = 0; 16 = +1; 17–18 = +2; 19–20 = +3; 21–22 = +4. Tie-break d100; remaining ties go to the lower slot.
- `[0x9DC1]` = 1 means the party is surprised and 2 means the enemies are surprised (the side is delayed).
- **Round start** (`0x100D6`): movement points (+`0x16`) = movement × 2 (quartered when the character fails skill 4 and `[0x97DC]` bit 4 is set); attacks-left bytes (+`0x18`, +`0x19`) are set from "attacks × 2", halved with the round parity; and the backstab mask `[0xD4FD]` gets a bit for each party member whose **skill 7** check reaches 2.
- **Round end** (`0xE434`): the round counter goes up, surprise is cleared, lingering patches tick, timed effects count down. The **peace counter** `[0xD50D]` (reset to 3 by every attack) counts down; at 0 the monsters give up (status `0x82`). Dying party members (status `0x83`) count up and are dead after 16 rounds.
- Round start also gives each side +1 attack modifier if its first creature (in slot order) is not under effect `0x0D`/`0x0E` and passes a skill-2 check (`0x158F2`).

## Skill checks

(`0x4F20`, `0x4F74`, `0x4FB0`)

- Value = points × 8, or (points − 2 × level) × 2 + 16 × level when points exceed 2 × level, plus the ability tied to the skill (table `0x4FFC`).
- Target = value × 4 >> shift. The shift is 2 everywhere it was found, so target = value.
- Roll d100. Success when roll ≤ target (critical when the margin is at least half the target); failure otherwise (critical failure when it misses by more than the target). No roll if the low byte of the value is 0.
- The check result is 0..3.

## To-hit

(`0x1056A`, flow at `0x10400`)

- Roll d20 (1–20). A hit needs roll ≤ value, where value = clamp(attack − armor + 20, 1, 19) after range penalties. The displayed percentage is value × 5, so the range is 5%–95%. A 20 always misses.
- Range penalty: a weapon with range above 1 loses 2 once the target is beyond half the range, and 5 beyond three quarters.
- Each point of armor is worth 5 percentage points; one armor step (+2 armor) is 10 points. The armor values by item step are in [ROM-FORMATS.md](ROM-FORMATS.md#weapon-and-armor-stat-table).
- A laser cannot fire through an aerosol cloud (message `0xBD`) and a rocket or grenade cannot fire through a chaff cloud (message `0xBE`). These are game rules, not bugs: chaff grenades jam rockets and aerosol blocks lasers. Line of fire (`0x15B5A`) is a Bresenham walk where each step costs 2/3 of a half step of range; terrain flags 501/502/503 block or stop.

## Armor by direction and backstab

(`0x10666`)

- Direction is the bearing from attacker to target minus the target's facing, mod 8: 0, 1, 7 = front (armor, +4 in the combat slot); 2, 6 = flank (armor − 2); 3, 4, 5 = rear (rear armor, +5).
- An idle enemy target (flags & 6 == 0) turns to face its attacker first, so only already-engaged targets can be flanked.
- **Backstab:** on a rear attack by a party member listed in `[0xD4FD]` who is unarmed or holds a melee weapon, the damage is multiplied by 2. For a **rogue** (career 4) the multiplier is (7 + level) >> 2 (levels 1–4: 2, 5–8: 3, 9: 4), and the rear armor used against the target is also 2 lower.
- Octant direction between two cells (`0x15D36`) uses tan 67.5° = `0x26A/256`.

## Damage

(`0x10804`, applied by `0x10E3E`, wound model `0x760A`)

- Each attack rolls to hit separately, and each hit rolls its own damage: the sum of `count` rolls of 1..sides plus a signed-byte bonus (negative becomes 0), times the multiplier, stored in a byte (wraps above 255).
- Damage is applied **last hit first**. The damage list is at `0xD48E`, one byte per attack. Entries are applied as a *signed* byte, so every entry of 0 or 128..255 is ignored for hit points.
- The racial tier (+1 Martian … +4 Lunarian) is a flat damage bonus on every hit. The damage die stays the same. A laser pistol is 1d8; Mercurian is 1d8 + 3.
- Damage greater than or equal to HP sets HP 0 and status `0x82` (down).
- A hit of 128 or more is also dropped. That is only reachable when the victim has 128+ HP, because the clamp effect limits damage to HP.
- Natural (unarmed) monster attacks sum their dice into one roll (see [RECORD-FIELDS.md](RECORD-FIELDS.md#natural-attacks)).

## Special effects

Effects live in record bytes +`0x43`..`0x4C` (permanent) or in the timed-effect list at `[0xD49C]` (3 bytes per entry: slot, effect, turns). At fixed points ("stages") the engine runs the handler of every effect on that stage's list that the creature carries. The stage runner is `0x664E`; stage lists are at `0x6670` (word offsets, each list ends with a byte ≥ `0x80`); the "has effect" test is `0x69DA`; the handler table is at `0x66F8` (word offsets from `0x66F8`, indexed by effect id).

| Stage | Where it runs | Effects |
| --- | --- | --- |
| 2 / 3 | each hit of the primary / secondary attack, before the damage dice (attacker) | `0x1C` slows the victim after a failed save (halves movement and attacks, effect `0x1D` for 5 turns). `0x1E` kills it (status `0x86`, HP 0) unless it saves |
| 5 | after the damage was rolled (victim) | `0x14` rockets do full damage; `0x17` immune to heat gun and plasma thrower; `0x18` immune to lasers; `0x19` / `0x1A` / `0x1B` half the hits do nothing against weapon types 1, 2, 5 / types 1–5 / melee; `0x03` damage never above the victim's HP |
| 6 | | `0x17`, `0x03` |
| 7, 15 | start of the creature's turn | `0x01` stunned (no time, reaction or movement); `0x0E` armor −2 and the turn is lost |
| 9 | a status effect is being applied | `0x15` / `0x16` immune to effect `0x0D` / `0x0E`; `0x03` immune to every effect |
| 10, 11, 12 | attack preparation (attacker / victim) and saving throws | `0x0D` armor −2, attack −4; `0x0E` armor −2 and turn lost (stage 11) |
| 14 | before a monster's weapon choice | `0x20` spits at its target: in line of fire within 12, d100 < 35 hits for 2d8 |
| 18 | start of the round | `0x1D` slowed: movement and attacks halved |

Only the secondary attack runs stage 3, so touch effects (slow, death) belong to a creature's second attack. Damage immunities of 50% are `d100 >= 50 → no damage`.

**Effect `0x03` is Buck Rogers' alone.** He cannot be killed outright (his damage is clamped to his HP, so he is knocked out at worst), cannot be given a status effect, and the blast code moves him out of explosions.

Effect ids on the creatures that carry them:

| Id | Meaning | Carried by |
| --- | --- | --- |
| `0x03` | Buck: never killed outright, immune to status effects, dodges blasts | Buck |
| `0x14` | rockets do full damage (weak to rockets) | robots, Hypersnake, Space Rat |
| `0x15` / `0x16` | immune to effect `0x0D` / `0x0E` | robots, Buck, Acidicium, … |
| `0x17` | immune to heat gun and plasma thrower | Desert Ape, Hexadillo, Sand Squid, Mer. H.S. Robot, Acid Frog, Buck |
| `0x18` | immune to lasers | P. Combat Robot, Assault and Combat Bots, Buck |
| `0x19` / `0x1A` / `0x1B` | half the hits do nothing against weapon types 1,2,5 / 1–5 / melee | Space Rat, Acidicium |
| `0x1C` | slowing touch (second attack) | Sm. E.C. Gennie, RAM G.D. Gennie, Swamp Hornet |
| `0x1E` | death touch (second attack, save) | Sand Squid, Lg. E.C. Gennie, Hyperscorp |
| `0x20` | spits at its target (within 12, 35%, 2d8) | Acid Frog |

### Effects that do nothing

Effect ids `0x0A`, `0x0B`, `0x10`, `0x12` and `0x13` have **empty handlers**. Three of them (`0x10`, `0x12`, `0x13`) are on a stage list, but their handler slot is a bare `RTS` (bytes `4E 75`). `0x0A` and `0x0B` are on no stage list at all, and their slot is a bare `RTS` too.

| Stage | List | Contains one of the five |
| --- | --- | --- |
| 5 | `14 19 12 17 18 1A 1B 03` | `0x12` |
| 9 | `16 15 13 10 03` | `0x13`, `0x10` |

Handler slots: effects `0x04`..`0x0C` (including `0x0A`, `0x0B`) all point at `0x6758`, and `0x0F`..`0x13` (including `0x10`, `0x12`, `0x13`) at `0x6778`; both hold only an `RTS`. Slots `0x00` and `0x02` are bare `RTS` too. The helper `0x69DA` has five call sites, and none asks for these ids. It was not searched whether other code reads record bytes +`0x43`..`0x4C` directly.

Carriers: `0x0A` only the RAM Assault Bot; `0x0B` only the RAM Combat Bot; `0x10` fifteen creatures (Desert Ape, the robots, Gennies, Ursadder, Acidicium, Stage 3 ECG, Buck Rogers); `0x12` nine (robots, Hypersnake, Buck Rogers); `0x13` eighteen (robots, Gennies, Space Rat, Swamp Hornet, Ursadder, Acid Frog, Acidicium, Buck Rogers). The six default party characters carry no effects.

Interpretation (not confirmed by the ROM): these look like designer tags on robots and acid creatures, and handlers were probably planned (immunities, resistances) and never written. They are hooks a mod could fill, for example `0x13` as a real resistance.

## Level-up, training and XP

(`0x0B1C`, `0x0B92`, `0x7418`, `0x3C0A`)

- **HP per level** = max(1, d(hit die) + CON bonus) + 4. Hit die 4 / 2 / 6 / 4 for rocket jock / medic / warrior / rogue. The CON bonus (table `0x0B7C`, −3..+6) is capped at +2 except for warriors. Maximum HP is capped at 104 (`0x68`), and the byte wraps before the cap check (HP 250 plus a gain behaves oddly).
- **Attack value** (record +34) by career and level comes from table `0x0BB0`. Warrior level 2 = 41, as in the pregenerated party.
- **XP thresholds:** table `0x0C16`, 8 u32 per career. Rocket jock 1250 / 2500 / 5000 / 10000 / 20000 / 40000 / 70000, medic 1500 … 96000, warrior 2000 … 125000, rogue as rocket jock. The eighth entry is `0xFFFFFFFF`, which caps every career at level 8. The level-9 variant replaces those entries; see [ROM-FORMATS.md](ROM-FORMATS.md#level-cap).
- **Training** scans the party: a ready character keeps XP up to the next-next threshold minus 1, so at most one level per training session. Warriors get a weapon-specialization screen at levels 4, 6 and 8. A maxed weapon refuses and the next one is tried.
- `ADDEP n` gives n XP to every present party member that is not down. It does not raise levels by itself.

## Victory rewards and loot

(`0x1631E`, `0x16268`)

- For every defeated enemy: its XP (record +`0x40`, unless record byte +`0x52` bit 0 is set) goes into a pool, divided by the number of living party members. The division is 16-bit: if the quotient does not fit, the low word of the pool remains.
- Its credits (record +`0x1A`, u32) are added to the party purse `[0xBA34]`, and its gear (13 slots from +`0x54`, built-in items excluded) goes to the loot pool. This is not applied when `[0xBA5B]` or `[0x97AE]` is set.
- The loot pool has 14 entries of 10 bytes at `0x6AF6`. The category table at `0xA829` decides per item id: negative = drops, 0 = never drops (knives), positive = stands for the base item at `0xF17CC + 10 × value`.
- Identical items (same id and byte +4) stack: counted items (quantity bit 7) get +1, others add the quantity and cap at 100; a new item without a quantity gets `0x81`.
- The scripted-XP table at `0x16402` was not decoded. The 6,250 credits shown after the first fight could not be reproduced from the record credits (they sum to 448), so the rest must be item value or a script bonus.

## How the computer plays a turn

(`0xEF64` and the routines it calls)

A creature's time word +`0x14` = 0 means done, 1 means "has waited once", otherwise a normal turn. The turn:

1. If it had waited, clear its target.
2. Pick a target (`0xE812`): keep the current one if alive, hostile and still in line of fire (range 100); otherwise a random one of the nearer half of the visible enemies; otherwise none.
3. Equip the best weapon (`ChooseWeapon`, `0xE89C`).
4. Work out its weapon range and the path to the nearest enemy.
5. Loop: with a ranged weapon (range above 1) attack as soon as the target is in line of fire within **half** the weapon range; with a melee weapon as soon as any enemy is within one cell. A successful attack ends the turn.
6. Otherwise take the next step of the path (the cost is in movement points, and each step can draw reactions).
7. When the path ends, a step fails, or there is no target: a creature that has not waited sets its time to 1 and stops; one that has waited attacks anything in range and ends its turn.

**Target selection and pathfinding** (`0x15D8A`) is a breadth-first search over the 21 × 21 combat grid. The first cell whose live occupant is on the other side becomes the target and the path comes back as direction codes (table `0x146E0`). Neighbours are tried in an order that rotates with a counter the ROM never initialises, so ties between equally near targets are broken by stack garbage. Passability: no creature marker (bit 7 of the tile byte), terrain flag bit 5 clear; tall creatures (record +`0x23` = 2) also need the cell south free and wide ones (3) the cell east. The search gives up if its 1,500-entry queue overflows.

**Weapon choice** (`0xE89C`, helpers `0xEAA6`, `0xEA90`) scores each weapon in the 13 slots by expected damage (dice × shots, ability bonus, item byte 4, warrior specializations × 3), then divides by a "this will not work" shift (decoded at `0xE912`–`0xE95F`):

- lasers: ÷ 8 if the line of fire crosses an aerosol cloud (battlefield tile 1, sets `[0xD502]`) or the target has effect `0x18`;
- rockets: ÷ 8 if the line crosses a chaff cloud (tile 0, sets `[0xD501]`); otherwise **÷ 4 if the target's record +`0x2F` has bits `0x06` or the target has effect `0x14`**.

It equips the best weapon if it beats the creature's unarmed value. The "÷ 4" is the value the v1.0 Quick Combat fix removes; see [PATCH-NOTES.md](PATCH-NOTES.md#quick-combat-weapon-choice-fix).

**Movement** (`0xF898`, helpers `0xF842`, `0x1443C`, `0x143FE`, `0x1432E`): one step costs terrain flags & `0x1F` against movement points left, flag `0x20` = impassable. Stepping off the map means fleeing (status `0x85`) unless a faster enemy sees the creature. After a step, every living enemy with its reaction ready (slot flag `0x10`) and a valid attack attacks the mover once (`0x11A44`).

**Healer rescue** (`0x10200`, `0x1021E`, `0xF28A`, `0xF22C`): a computer-controlled party creature with healing skill (record +`0x32` or +`0x3B` non-zero) looks for fallen friends: status `0x83` (dying), or `0x84` (unconscious) when the healer has points in +`0x32` and the friend is not yet in the revived mask `[0xD50A]`. It walks to the friend (path mode 2) and spends its whole turn there. Without +`0x32` points a first-aid check (skill 10) must reach 2. Then the friend is stabilised (status `0x84`, HP 0). Unless it was already revived this fight, a medicine check (skill 1: result − 2, times 4, plus the healer's +`0x32` points, at most the friend's maximum HP) puts it back on its feet on the nearest free passable cell, and its bit is set in `[0xD50A]`.

**Player turn** (`0xF2AE`): the command menu (`0x1391A`) offers attack, move, auto, wait (or rescue for a healer with somebody to help), end turn. *Auto* sets slot flag `0x80`, so later turns are played by the computer routine. An AI turn also reads the control pad once per decision; bit 7 (cancel) hands the party back to the player. A party creature that steps off the map is asked first (`0x136DA`): "yes" lets the escape rules proceed.

## Explosives

(`0x10FAA` and helpers)

- Item types 5, 6, 8, 9, 10, 11 and 12 (item byte +9) are explosives. A table at `0x10F69` gives per type the status effect, radius, whether it leaves a patch, and the damage mode.
- Damage = the weapon's dice plus bonus (types 5 and 12 do none but draw an effect duration of 2–5).
- The throw hits the aimed cell when d20 ≤ the to-hit value, otherwise it scatters up to four times to a neighbouring cell in sight. A blocked line stops at the last free cell. The throw does not run the to-hit preparation first.
- Types 8 and 9 leave a 3 × 3 patch (tile 0 / 1: smoke / gas) that replaces the terrain for 5 rounds (2 with the option bit) and blocks lines of fire. Types 6, 10 and 11 transform terrain around the blast and move a party creature that has effect 3 out of the blast to the nearest free cell it can see.
- Everybody in the radius with a line of fire gets a saving throw (`0x6990`: d20 + modifier against record +`0x15`), takes the damage and the type's effect. A save means half damage, or none for damage mode 2. The 16 lingering patches at `0x78CE` count down once per round.
- The thrower's action time is spent and the item loses a charge; a monster keeps it unless a random number below its record byte +`0x3F` is zero.
- The computer scores a throw with `0xEB50`: every cell within reach is scored by the creatures a blast there would hit, and the best cell with a line of fire wins (ties: 25% replace).

## Start of a fight

(`0xE394`, `0x1503C`, `0x149BA`, `0x14738`, `0x14DF6`)

- **Mode and flags:** `[0x9BBC]` becomes 2 (old value to `[0x9BBD]`). `[0xD4FE]` is the ambush flag (`[0x9DB6]`, forced to 1 when `[0xD8CC]` is set) and makes the monsters start one step further out. The screen mode `[0xB52A]` comes from `[0x97DC]` (`0xA2` → 4, `0xA8` → 5, else 6).
- **Battlefield** (`0x149BA`): outdoor types 0..10 start as a field of tiles 8 / 9 (a coin per cell), indoor types of `0x16` / `0x17`. Then the feature list of the type (tables `0x2FF6` and `0x300E`, rows of 11 bytes at `0x30D8`) is stamped. Screen mode 4 / 5 use terrain tables at `0x31F5` / `0x325A` or `0x31CB` / `0x3231`. Screen mode 6 (fixed dungeon maps, `0xB100`) turns the 16 × 16 map layers around the party into the 21 × 21 map; terrain table `0x3297`.
- **Deployment** (`0x14738`): the party is placed from the start cell in the facing direction and the monsters from the opposite side (or from each direction of the group mask). Creatures that find no room, and those not taking part in a solo fight (`[0xBA5B]`), get status bit 6.
- **Control** (`0x14DF6`): living party members that are not allies (flag bit 6) are player-controlled; everybody else is computer-controlled. Each player-controlled member may recruit each ally (computer-controlled, not large, no effect 3) with a skill-2 check.
- If either side has nobody left after deployment the fight is not on. Otherwise the round counter starts at 0 and the peace counter `[0xD50D]` at 3.

## Quirks and bugs of the original

- **The rocket special never damages** (fixed in v1.0). The damage list is pre-filled with `0xFF` meaning "no hit". When a rocket-type weapon (weapon-table type 2) hits a target whose record flag +`0x2F` has bit 2 (75%) or bit 1 (50%), or the target carries effect `0x14` ("weak to rockets", most robots; no dice roll), the hit's damage is replaced by `0xFF`. The applier reads that as a signed byte and ignores it. So **every rocket hit on a robot did nothing**. `0xFF` is only checked by the projectile animation (`0x10CBC`). Details: [PATCH-NOTES.md](PATCH-NOTES.md#rocket-special-fix).
- **Quick Combat avoided rocket pistols** (fixed in v1.0): the weapon-choice divide by 4 described above.
- **Character creation kept claws** (fixed in v1.0); see [RECORD-FIELDS.md](RECORD-FIELDS.md#character-creation-values-and-the-fixed-bug).
- The turn controller keeps "time was 1" in register d6, but the target enumeration leaves a counter in d6, so every turn that searched for a target takes the "has waited" branch at the end.
- The path can start with a bogus step: the search marks the start cell when a neighbour looks back at it, and the path reconstruction then emits an extra direction. A creature at the map edge can try to step off the map.
- The weapon choice tests the actor's tile at index x × 21 + y, where everything else uses y × 21 + x, and reads the actor's record flags where it looks at the target.
- The damage list overlaps the multiplier `[0xD496]`, the damage scratch `[0xD497]` and the monsters' attack modifier `[0xD499]` when an attack round hits 9 or more times.
- `0x11630` builds the weapon range word from the caller's register d2 (a byte move keeps bits 8–15), so the range is occasionally `0xFFxx`. The AI then reads it as "very long range".
- The BFS clears only 440 of its 441 visited bytes, so the last cell keeps old data.
- In the inventory screen: handing a single item to a member that already has a stack of it overwrites that stack (the whole 10 bytes are copied, count 1); selling truncates the proceeds to 16 bits per sale; shop sale price is half the item price (bytes +6/+7) times the amount.
