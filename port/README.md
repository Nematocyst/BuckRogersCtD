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
* **Initiative** = `INIT[DEX]` (table 0x7786: DEX 1-5 = -6,-4,-3,-2,-1; DEX 6-15 = 0; 16 = +1; 17-18 = +2; 19-20 = +3; 21-22 = +4) + d10, -8 for the surprised side, minimum 2; tie-break d100; ties go to the lower slot.
  **Slot byte +1 bit 0 SET = party.** `[0x9DC1]` = 1 delays the party, 2 delays the monsters (the earlier poke test results now make sense).
* **Round start also**: sets slot +0x16 (movement points) = slot[+0x0F] (movement)*2 (quartered when the character fails skill 4 and [0x97DC] bit 4 is set), copies "attacks x2" to the
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
Spells and abilities, the `ad5a`-style animations, the remaining uses of [0xD501..0xD503] and [0x97AE], the text behind message ids
0x128/0x129/0x12A/0xBD/0xBE, and the scripted-XP table at 0x16402 (the tally takes the scripted bonus as a parameter).

## Attack preparation, line of fire, wounds (ROM 0x10400, 0x15B5A, 0x6D1E, 0x760A)

`GenesisActions.cs` ports the pure-logic half of "attacking": everything up to (but not including) the RNG rolls.

| C# | ROM | Notes |
|---|---|---|
| `GenesisAttackPlanner.Prepare` | 0x10400 | grenade mode (item modifier 5..12), armor vs. octant, recompute both slots, side check (msg 0xBC), weapon range (0x11638), line of fire with a second try at the other cell of a tall/wide target (msgs 0xBA/0xBB), then the to-hit tail. No RNG. |
| `GenesisLineOfFire.Run` | 0x15B5A | Bresenham; each step costs 2/3 of a "half step" of range; terrain flags 501/502/503 block or stop; reads outside the map return 0. |
| `GenesisStats.RecomputeSlot` | 0x6D1E | derives a combat slot from the character record: item ids are sign-extended, record+0x2F flags, encumbrance, STR/DEX tables. |
| `CombatState.ApplyDamage` | 0x760A | wound model: damage >= HP sets HP 0 and status 0x82 (down). |

Slot field corrections: slot +0x0E = HP, +0x0F = movement, **+0x16 = movement points for the round** (= movement*2 at round start), not HP*2.
**Damage list, resolved (ROM 0x107E8 -> 0x10804 -> 0x10E3E).** Each attack stores one byte per attack into the list at 0xD48E (damage after the stage-5 effect hooks, which end with the clamp-to-HP effect 0x6742; the rocket-weakness special then overwrites it with 0xFF). `0x10E3E` applies an entry with `move.b (a1,d2.w),d2 / ble`, i.e. as a *signed* byte, so every entry of 0 or 128..255 is ignored for HP. Consequences, all verified against the ROM (`CombatState.ApplyDamageEntry`, 600 cases):
* The 0xFF "full damage" result of the rocket special never kills or hurts anyone in the original; 0xFF is only checked by the projectile animation (0x10CBC, the missile homes in on its target). `ResolveAttack` still reports it because the RNG draws happen.
* A normal hit of 128+ is also dropped (reachable only when the victim has 128+ HP, since the clamp hook limits damage to HP).
* The victim must be alive (status byte non-zero, bits 6/7 clear); the hit sound (0x1B900) is requested for ranged attacks only.
This is almost certainly a bug in the original game, not a design. The port reproduces it so recorded fights stay identical; if you want the intended behaviour in Unity, treat 0xFF as "damage = current HP" and entries as unsigned.

## The monster turn (ROM 0xEF64 and everything it calls) - `GenesisMonster*.cs`

`TurnContext.RunTurn()` is the port of the turn controller, built from verified pieces (every piece and the whole turn are compared with the real ROM routines
in `tests/MonsterTests.cs`; vectors by `tools/gen_monster_vectors.py`, 56,000 checks: 300 target lists, 300 target choices, 300 weapon choices, 300 attacks, 500 steps,
200 searches and **2,000 whole turns**, byte-exact on slots, records, map markers, the scratch globals 0xD48E..0xD5FF, the target list and the RNG).

| C# | ROM | What it does |
|---|---|---|
| `EnumerateTargets` | 0x15C2C (+0x15C88/0x15CD8/0x159C4) | creatures of a side that one of the actor's cells can see within a range, with distance and octant, sorted by distance (selection sort that also swaps equal distances) |
| `SelectTarget` | 0xE812 | keep the current target if it is alive, hostile and still in line of fire (range 100); else pick a random one of the nearer half of the visible enemies (blockers ignored when nobody is visible); else none |
| `ChooseWeapon` | 0xE89C (+0xEAA6, 0xEA90) | scores every weapon in the 13 item slots (dice + modifiers + STR/DEX bonus, ranged weapons shifted down for cover), equips the best one if it beats the creature's unarmed value, keeps ammo/shield/armour items in their slots, recomputes the stats; some items end the turn when equipped |
| `Navigate` | 0x15D8A | the breadth-first search (`GenesisAi.FindPath`); the path lands at [0x6CB0..] |
| `PrepareAttack` / `ExecuteAttack` | 0x10400 / 0x1074A | to-hit and armor (see above), then every natural/weapon attack is rolled, the damaging hits collected and applied **last hit first**; the attack spends the action time |
| `MoveStep` | 0xF898 (+0xF842, 0x1443C, 0x143FE, 0x1432E) | one step: facing, occupant and terrain check (cost = terrain flags & 0x1F against movement points left, flag 0x20 = impassable), stepping off the map = fleeing (status 0x85) unless a faster enemy sees the creature |
| `Reactions` | 0x11A44 | after a step every living enemy with its reaction ready (slot flag 0x10) and a valid attack attacks the mover once; the mover can die |
| `RunTurn` | 0xEF64 | see below |

**What a creature does with its turn.** Time +0x14 = 0 means done, 1 means "has waited once", otherwise a normal turn. It (1) clears its target if it had waited, (2) picks a target,
(3) equips its best weapon, (4) works out its weapon range and the path to the nearest enemy, and then loops: (5) with a ranged weapon (range > 1) it attacks as soon as the target is in line
of fire within **half** the weapon range, with a melee weapon as soon as any enemy stands within one cell (a random one of them becomes the target); a successful attack ends the turn;
(6) otherwise it takes the next step of the path (cost in movement points, every step can draw reactions); (7) when the path ends, a step fails or it has no target: a creature that has not
waited sets its time to 1 ("wait") and stops; one that has waited attacks anything in range (random pick) and then ends its turn (time 0, reaction ready unless it carries an explosive).

Quirks of the original that the port reproduces (found by the differential tests):
* the turn controller keeps "time was 1" in register d6, but the target enumeration leaves a counter in d6: every turn that searched for a target takes the "has waited" branch at the end;
* the path can start with a bogus step: the search marks the start cell when a neighbour looks back at it, and the path reconstruction then emits that extra direction - a creature at the map edge can try to step off the map;
* the weapon choice tests the actor's tile at index x*21+y (everything else uses y*21+x) and reads the *actor's* record flags where it looks at the target;
* the damage list overlaps the multiplier [0xD496], the damage scratch [0xD497] and the monsters' attack modifier [0xD499] when an attack round hits 9 or more times.

Not ported (the ROM test replaces them by empty routines, so these are the known gaps): the screen, sound and animation routines (assumed to have no effect on the game state, except the projectile animation's sprite scratch [0xD51A/B], which the test ignores); 

### The "leave the battlefield?" prompt - `ChoicePrompt` (ROM 0x136DA)

A party creature that is not on auto and steps off the map is asked first (text box 7: "yes / no"); "yes" (choice 0) lets `MoveStep` go on to the escape rules, anything else (no, cancel) keeps it where it is.
`ChoicePrompt(message)` is the whole box on the host's `Pad` callback (the same pad readings as the player turn; `RetreatPrompt` still overrides it with a plain answer): the cursor starts on the choice stored in [0xD593]
(0 = yes, the menus leave it 0), left / right (and the next-target button) move it, confirm takes it, cancel answers -1 unless [0xD592] forbids that; pad readings of 0 are skipped; in demonstration mode ([0xBA5A],
`DemoFlag`) any button press takes the preset choice. The same routine asks the other yes / no questions (messages 1, 6, 8), shows "abort" / "press c to continue" style boxes with a single choice (any of the buttons 4..7 answers)
and the 8-choice debug picker (message 9); the box clears [0xD592], [0xD593], [0xD595] when it closes, and the help button leaves [0xD596] set.
Verified with 400 off-map steps of player-controlled creatures answered by random pad readings (7,190 checks: choice, step result, pad readings consumed, globals; the text box's cursor bytes [0xD5D6..0xD5E1] are ignored).

### The start of a fight - `GenesisCombatSetup.cs` (ROM 0xE394, 0x1503C, 0x149BA, 0x14738, 0x147EE, 0x14DF6)

`RunCombat()` is the whole of ROM 0xE394: `CombatSetup()` (0x1503C), the per-creature memory of the last victim [0xD51D..] forgotten, then `CombatRounds()`. The caller (the script engine) has filled the combat slots with the party and the monsters; what
the original does after the fight (0x15FDA: experience, treasure, leftover statuses, the victory screens) is the host's.
* **Mode and flags**: [0x9BBC] becomes 2 (the old value goes to [0x9BBD], `PrevMode`), [0xD509], [0xD50E], [0xD50A] are cleared, [0xD4FE] (`Ambush`, [0x9DB6]; forced to 1 when `GroupMask` [0xD8CC] is set) makes the monsters start one step further out.
  The party's start cell [0xD8D0/1] comes from a table by `Facing` [0x9AFA] (when [0xD4FE] is set and no groups) or is fixed. The screen mode [0xB52A] is worked out from [0x97DC] (0xA2 → 4, 0xA8 → 5, else 6).
* **Battlefield** (`GenerateBattlefield`, 0x149BA): outdoor types 0..10 start as a field of tiles 8 / 9 (a coin per cell), indoor types (screen mode 5: [0x97AD] + 11) of 0x16 / 0x17; then the feature list of the type (tables 0x2FF6 / 0x300E, rows of 11 bytes at 0x30D8) is stamped: each (kind, repeats) pair rolls
  its chance, then how many stamps (dice), then for each stamp a random point within the row's spread of a random spot (a quarter-circle by a Newton square root, `ISqrt`, reproduced including its 16-bit overflow behaviour) - a stamp goes only on cells that are still plain ground.
  Screen mode 4 / 5 select the terrain flag table and tile script of the area ([0xD810] / [0xD814] = ROM 0x31F5 / 0x325A or 0x31CB / 0x3231 - the port switches `TerrainFlags` and `TileScript` to them). **Screen mode 6** (fixed dungeon maps, 0xB100) is `BuildDungeonArena` (`GenesisDungeonArena.cs`): the 16x16 map layers around the party (wall bits, two layers of tile classes, two class tables) are turned into the 21x21 map by scanning the map along a diagonal in 6x5 blocks; terrain table 0x3297. The host fills `MapLayerA/B`, `MapWalls`, `TileClassA/B`, `MapX/MapY` from the loaded dungeon map.
* **Deployment** (`DeployAll`, 0x14738): every creature loses its place, then the party is placed from the start cell in the facing direction and the monsters from the opposite side (one group) or from each direction of the group mask (the monsters are shared out round-robin); `DeploySide`
  starts at the start cell (shifted back by the spread) and does a breadth-first search over the neighbour pattern of the direction (a wider one in wide formation) for free cells: unoccupied, passable, terrain cost < 4, and for large creatures (record type 2 tall / 3 wide) the second cell too. After the first six cells have been looked at, occupied cells no longer
  open up their neighbours. Creatures that find no room (and those not taking part in a solo fight, [0xBA5B]) get status bit 6.
* **Control** (`AssignControl`, 0x14DF6): living party members that are not allies (flag bit 6) are player controlled (flag 7 clear), everybody else computer controlled; then every player-controlled party member in turn may recruit each ally (computer-controlled, not large, no effect 3) with a skill-2 check; "press C" waits if anyone did.
* If either side has nobody left after the deployment the fight is not on ([0xD50E] stays 0). Otherwise the map markers are built, round counter 0, peace counter [0xD50D] = 3, the timed effects [0xD49C..] and lingering patches are cleared and the cursor is at 0,0.
Verified against the ROM: 300 battlefields (every area type), 400 deployments, 400 whole starts; 37 whole fights from the first call (`RunCombat`: setup + rounds) matched in trial runs, but their vectors are not in the repo yet (`N_combat` in the generator).

### The round loop of a fight - `GenesisCombatLoop.cs` (ROM 0xE3A8..0xE42C, 0x158F2, 0x754C, 0xE434, 0x6AA4)

`CombatRounds()` runs a fight from the state the setup leaves ([0xD50E] set) until one side has nobody standing:
1. **round start** (0x158F2, `SideBonuses`): [0xD499] / [0xD49A] (monsters / party attack modifier, see `RecomputeSlot`) are cleared; then in slot order the first creature of each side that is not under effect 0xD / 0xE and passes a skill-2 check gives its side +1 (and
   becomes the current actor [0xCA20] - a leftover the next turn overwrites); then every slot rolls its round start (`GenesisTurns.BeginRound`, 0x100D6: backstab mask, movement points, attacks, initiative);
2. **turns**: `CountLiving` (0x754C: [0xD8CA] monsters / [0xD8CB] party) - the fight is over when either is 0 - then `PickNextActor` (highest speed/tie-break word among living creatures with time) acts through `BeginTurn` (0xE4F0); repeat until nobody has time;
3. **round end** (`RoundEnd`, 0xE434): round counter [0xD50C]++, surprise [0x9DC1] cleared, lingering patches tick, timed effects ([0xD49C..]) count down, the **peace counter [0xD50D]** (reset to 3 by every attack) counts down - at 0 the monsters give up (status 0x82) -
   and every dying party member (status 0x83) counts up its HP byte and is dead (0x87) after 16 rounds.
`ChoicePrompt`, `PlayerTurn` and the AI turn all read the same `Pad` callback; note that **an AI turn also reads the pad** (0xF156, once per decision): bit 7 (cancel) hands the party over to the player (`TakeoverRequested`).
`ActorVisible` stands for the camera: a creature that acts off screen makes the view scroll (0xE5A8 / 0xE5E6) and the cursor [0xB3F0/2] lands on it - matters for the cursor-based explosive preparation of reactions.
Verified with 200 whole fights (random worlds, AI and player-controlled creatures with scripted menu / pad input, order of turns, RNG draws and event sequence compared with the ROM; 5,198 checks on 296 fights).
In the manual turn the "explosive in hand" flag and the target index are uninitialised stack in the ROM; the test starts them at 0 (the port does too).

### The player's turn - `TurnContext.PlayerTurn` (ROM 0xF2AE) - `GenesisPlayerTurn.cs`

A creature without the computer-control flag (slot +1 bit 7 clear: the party members unless the player chose "auto") plays through `PlayerTurn`; `BeginTurn` (0xE4F0) picks it. The ROM's two input
routines are the host's callbacks: `MenuChoice` (0x1391A, the command menu: 0 attack, 1 move, 2 auto, 3 wait - or rescue when the creature is a healer with somebody to help - 4 end the turn,
negative = cancel = `CharacterSheet`, 0xFDC8) and `Pad` (0xF1B66, one reading of the control pad: bit 0 up, 1 down, 2 left, 3 right, 4 next target, 5 confirm, 6 character sheet, 7 cancel). The
turn is a small state machine over the modes *menu*, *walk* (0), *attack* (1) and *rescue* (2) that ends when the creature's time (+0x14) is 0 or the menu command leaves the turn.

* **Walk.** The menu starts the walk (`StartMotion`, 0xF9A6: markers removed, flag 4) after saving position and movement points. One step needs **21 pad readings** (20 more after the first): the directions seen in them are or-ed,
  a diagonal ends the reading at once; then `MoveStep` runs (terrain cost, reactions, fleeing off the map). *Cancel* undoes the whole walk (position, movement points, markers back) - note the reactions
  that were drawn stay drawn; *confirm* ends the walk. The debug key (next target) knocks out every living monster when [0xCA21] is 0.
* **Attack.** The target list is every living monster (0xF1EA, [0xCA22..]); the cursor (pixel position, one cell = 24 steps, kept inside the map) starts on the actor, or - when the creature's previous victim ([0xD51D + slot]) is
  still in the list - on that one. Each cursor stop looks up who stands there (`OccupantsFull` = 0x1432E with its second pass for fallen creatures) and, when it is a different creature, runs `PrepareAttack` on it - which has
  game state effects (stats recomputed, target turned) just by aiming. The actor always faces the cursor. With an explosive in hand the preparation is re-run at every stop and the display refreshed when the to-hit or message changed.
  *Confirm* attacks (`ExecuteAttack`, or `AreaAttack` at the cursor for explosives; nothing when the preparation found no way to attack) and remembers the victim in [0xD51D + slot] (not after a blast).
* **Rescue** (healer menu entry): the cursor lists the fallen friends (0xF22C); confirming on one runs the healer routine (`Rescue`, with path mode 1 instead of the computer's 2).
* **Wait** toggles the time between 0 and 1 (a second "wait" ends the turn); **end turn** is 0xF1A2; **auto** sets flag 0x80 so the next turns are played by `RunTurn`.

Quirks of the original reproduced: the "explosive in hand" flag lives in an uninitialised stack slot until the attack mode sets it and survives a visit to the character sheet (the port starts it at false); the target-list index likewise (starts 0).

Verified with 700 scripted turns (13,600 checks) (random menu answers and pad readings, run in the ROM with the input routines replaced and compared with the port: the number of readings consumed, final cursor, slots, records, markers, scratch globals, RNG and the event sequence match; the menu window layout bytes [0xD582..9, 0xD592, 0xD595] are ignored).

### The character sheet and inventory - `GenesisInventory.cs` (ROM 0xFDC8, 0x748C, 0x78D6)

`CharacterSheetScreen()` is 0xFDC8/0x748C (the player turn opens it from the sheet key: `OpenSheet`, which calls the host's `CharacterSheet` callback or, when the host gave a `SheetMenu`, this
screen): the character's stats are worked out again, the game mode [0x9BBC] is 9 while the sheet is open, and the page menu (`SheetMenu`: <= 0 leave, 1 skills page = `ShowSkills`, anything else the
inventory) loops. The stat page (0x7000) and skills page (0x8034) only draw - the host reads the same record fields. **`InventoryScreen()` (0x78D6) holds all the rules.**

The screen is a menu of 23 cells: 0..12 the character's 13 item slots (0..8 backpack, 9 hand, 10..12 armour / shield / ammunition), 13 drop (sell when `ShopFlag` = [0xBA60] is set), 14 leave, 15..22 the party members.
The host's `InventoryMenu` callback answers with a cell (negative = cancel); **cells in the disabled list [0xD564..] (`CellDisabled`) cannot be chosen** - the ROM greys them out and ignores them.
First an item is picked up, then put somewhere:
* nothing picked: only non-empty item slots can be picked (no item can be touched at all when [0x97AE] is set); the party member cells are always grey while the sheet is open (mode 9), in other modes every living member but the current one switches the screen to that character;
* picked item -> another slot: the two items swap (empty slots too). Slots 9..12 take only items of one weapon-table class each (0 hand, 3, 1, 7: table at 0x7D78), the backpack takes anything. **Moving something into the hand
  costs the action time (+0x14/+0x15 are cleared) when the picked item's weapon-table row has a non-zero byte +6** (a slow weapon to ready); afterwards the character's stats are worked out again;
* picked item -> drop cell: a stack asks "how many" (`AskQuantity(max)`, 0 cancels); the amount leaves the stack (the item disappears at 0). In a shop the item is sold instead: money [0x9BD0] += half the price (item bytes +6/+7) times the amount, **truncated to 16 bits per sale**;
* picked item -> a party member (not in mode 9): reachable when alive, not out of the fight, not the current character and the item has somewhere to go (`FindDestination`, 0x81E6: a stack of the same item - same id and +4 - with fewer than 250, else the empty slot of its class -
  hand 0xAE for classes 0/2, 0xC2 class 1, 0xB8 class 3, 0xCC class 7 - else the first free backpack slot). One item (stack count 0 or 1) moves whole; a stack asks for an amount up to what fits (250 - the destination's count) and is split (a new stack starts at the amount). The receiving member's stats are worked out again.

Quirks reproduced: the member cells are greyed with the item pointer register left over from the slot checks, so while an item of slots 9..12 is picked the check uses *backpack slot 8's* item instead; handing over a single item to a member that already has a stack of it
overwrites that stack (the whole 10 bytes are copied, count 1) - the item count of the destination is lost; and if the greyed-out check lets a member through that has no room, the ROM writes into its own ROM (ignored by the hardware) and the item is lost - `InventoryScreen` models that,
but the tests cannot (the emulator faults on the write; 2 of 2,500 generated screens).

Verified with 1,200 scripted screens (random choices among the cells the ROM leaves enabled, random quantities with the prompt maximum compared too; a third of them go through the sheet's page menu): records of every member, slots, scratch globals (including the disabled list),
money and the order of prompts all match; the menu window scratch bytes [0xD594], [0xD59C..F] are ignored.

### Healer rescue - `TurnContext.AllyRescue` (ROM 0x10200, 0x1021E, 0xF28A, 0xF22C)

A computer-controlled party creature with healing skill (record +0x32 or +0x3B non-zero) looks, at the start of its turn, for fallen friends: party creatures with status 0x83 (dying), or 0x84
(unconscious) when the healer has skill points in +0x32 and the friend is not yet in the revived mask [0xD50A]. The first becomes its target and it walks to it (the path search in mode 2), every step costing
movement and drawing reactions. Next to the friend the creature spends its whole turn: without +0x32 points a first-aid check (skill 10) must reach 2; then the friend is stabilised (status 0x84, HP 0) and,
unless it was already revived in this fight, a medicine check (skill 1: result - 2, times 4, plus the healer's +0x32 points, at most the friend's maximum HP) puts it back on its feet on the nearest free passable cell
around it, and its bit is set in [0xD50A]. Revival does not touch the living counters. If the way is blocked or the friend is not reached the creature carries on with a normal turn with the fallen friend as stale target.
383 cases verified (225 + the party turns of the whole-turn tests); the text colour [0xD5AC] of the messages is ignored by the test.

### Special-effect hooks - `GenesisEffects.cs`

`TurnContext.Stage(stage, creature)` is ROM 0x664E: at fixed points of the engine the handler of every effect on the stage's list that the creature carries is run (effects live in record +0x43..+0x4C or, timed, in the list at [0xD49C]).
Verified with 800 stage runs over every stage, effect and creature combination, 500 whole turns through `BeginTurn` (= ROM 0xE4F0) and inside the attack, turn and explosive tests (75,000 checks for the monster turn in all).

| stage | where | effects (id = hex; meaning from what the handler does) |
|---|---|---|
| 2 / 3 | each hit of the primary / **secondary** attack, before the damage dice (attacker) | 0x1C slows the victim after a failed save (halves movement and attacks, effect 0x1D for 5 turns); 0x1E kills it (status 0x86, HP 0) unless it saves |
| 5 | after the damage was rolled (victim) | 0x14 rockets do full damage; 0x17 immune to heat gun / plasma thrower; 0x18 immune to lasers; 0x19 / 0x1A / 0x1B: half the hits do nothing against weapon types 1,2,5 / 1..5 / 0 (melee); **3: damage never above the victim's HP** |
| 7, 15 | start of the creature's turn (`BeginTurn`) | 1 stunned (no time, reaction or movement); 0x0E armor -2 and the turn is lost |
| 9 | a status effect is being applied | 0x15 / 0x16 immune to effect 0x0D / 0x0E; **3: immune to every effect** |
| 10, 11, 12 | attack preparation (attacker / victim) and saving throws | 0x0D armor -2, attack -4; 0x0E armor -2 + turn lost (stage 11) |
| 14 | before a monster's weapon choice (`RunTurn`) | 0x20 spits at its target: in line of fire within 12, d100 < 35 hits for 2d8 |
| 18 | start of the round (`GenesisTurns.BeginRound` takes an optional callback) | 0x1D slowed: movement and attacks halved |

Effect 3 is Buck Rogers' (nobody else has it): he cannot be killed outright (his damage is clamped to his HP, so he is knocked out at worst), cannot be given a status effect, and the blast code moves him out of explosions - the dodge.
Only the secondary attack runs stage 3, so touch effects (slow, death) belong to a creature's second attack. Note the order of the handlers' damage immunities: 50% chances are `d100 >= 50 -> no damage`.

An uninitialised-register quirk the differential tests found: `0x11630` builds the weapon range word from the *caller's* register d2 (a byte move keeps bits 8..15), so the range is occasionally 0xFFxx; the AI then reads it as "very long range" (explosive blast radius clamps to 9, ranged branch of the turn, to-hit distance rules). `TurnContext.RangeD2` replays the captured bits in the tests; in a game leave it empty (0) to get the clean behaviour.

### Explosive weapons - `GenesisArea*.cs`

Verified like the rest (300 scorings, 500 executions, 800 blasts, 100 patch ticks and the 2,000 whole turns, half of them in worlds with explosives, gas patches and a terrain-transformation table).

| C# | ROM | What it does |
|---|---|---|
| `AreaEval(score:true)` | 0xEB50 (+0xEAA6) | the weapon scorer's value for an explosive: the best cell's score times a typical damage by item id (draws random numbers) |
| `AreaEval(score:false)` | 0xEB50 | the turn controller's throw: every cell within reach of the thrower (or of its target when range + reach > 9) is scored by the creatures a blast there would hit, the best cell with a line of fire wins (ties: 25% replace); no cell worth it -> `ChooseWeapon(0)` picks something else |
| `AreaAttack` | 0x10FAA (+0x1137E, 0x113EC, 0x15C54) | the blast |
| `OriginalTile`, `SetTile`, `TileChain` | 0x145DE, 0x14670, 0x144FE | terrain under a lingering patch, replacement, and the terrain transformation chain of [0xD814] |
| `RemoveHazard`, `TickHazards` | 0x1150E, 0x1158A | the 16 lingering patches at 0x78CE count down once per round |
| `SavingThrow`, `ApplyEffect`, `AreaDamage` | 0x6990, 0x6A34, 0x115CE | d20 + modifier against record +0x15; status effects in the temporary list at [0xD49C]; save = half damage (or none for damage mode 2) |

How a blast works. The item types (item byte +9) 5, 6, 8, 9, 10, 11, 12 are explosives; the table at 0x10F69 gives per type the status effect, radius, whether it leaves a patch and the damage mode.
Damage = the weapon's dice + bonus (types 5 and 12 do none but draw an effect duration 2..5). The throw hits the aimed cell when d20 <= the to-hit value, otherwise it scatters up to four times to a
neighbouring cell in sight; a blocked line stops at the last free cell. Types 8 and 9 leave a 3x3 patch (tile 0 / 1: smoke / gas) that replaces the terrain for 5 rounds (2 with the option bit) and blocks lines of fire. Types 6, 10, 11
transform terrain around the blast along the [0xD814] chain and move a party creature that has effect 3 (a force field?) out of the blast to the nearest free cell it can see. Everybody in the
blast radius with a line of fire gets a saving throw, takes the damage and the type's effect; the thrower's action time is spent and the item loses a charge (a monster keeps it unless a random number below its record byte +0x3F is zero, i.e. it saves ammunition).

Quirks reproduced: the blast-shape loop is a do-while, so a creature at the edge of the scoring area still gives its weight to one cell when clipping leaves nothing; `SetTile` indexes a patch's saved tiles in the opposite order from `OriginalTile`;
the follow-up in the terrain search tests a byte as a word and is dead code; the throw does not run the to-hit preparation first, so [0xD511] is whatever the last attack left there (usually a lot of scatter).

## Unity front end (thin adapter)
* `csharp/unity/AutoBattle.cs`: sets up a fight from monster-file ids (`LoadCombatant` / `AddMonsters`), lets the computer play every creature and returns a `BattleFrame` per creature turn (tile map, positions, hit points, sides). Plain C#; tested under mono (`tests/AdapterTests.cs`: 12 fights, deterministic for a seed, always ends with one side standing). Party members are monster-file records (the data has no stored player characters); outdoor ground types 0..10.
* `csharp/unity/CombatBoardView.cs`: a `MonoBehaviour` that runs one fight on Start and replays it with IMGUI (coloured ground cells, creature boxes with hit points, Play / Pause / Step / Back / New fight). Put it on an empty GameObject with `rom_tables.json` and `monster_file.bytes` (both in `csharp/unity/Resources/BuckRogers/`) under `Assets/Resources/BuckRogers/` plus the `csharp/*.cs` logic files. It was not compiled in Unity here; the logic it calls is the tested part.
* Not in the adapter: player input (`Pad`, `MenuChoice`, the inventory / sheet / loot hooks), sprites, text, sound, the script engine and everything around a fight.
