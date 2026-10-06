# Status, gaps and how to run the tests

## Verified against the ROM (135,348 monster-turn checks in the repo vectors)
enum 200, select 200, weapon 200, attack 300, move 300, nav 150, whole turns 1,200, explosive scoring 200 / execution 300 / blast 500, patch ticks 60, effect stages 800, begin-turn 500, rescue 383, manual turns 700, inventory 1,200, retreat prompt 400, whole fights 200, battlefield 150, deployment 200, fight setup 200, fight clean-up 250, loot screen 250, ship repair 250. Earlier work (RNG, combat maths, progression, rewards, skills, initiative, pathfinding, actions) has its own vectors; see `port/README.md`.

## Dungeon-map arena (ROM 0xB100): ported
`BuildDungeonArena` is checked on 400 random maps (layers, wall densities, class tables, party positions including off-map ones) against the ROM, the whole 441-cell result compared; 7 mutations of the port are all caught. The host only has to supply the loaded map (`MapLayerA/B`, `MapWalls`, `TileClassA/B`, `MapX/MapY`).

## Combatant creation (ROM 0x3544, 0x488C, 0x48E8): ported
`AddMonsters(id, count)`, `AddAlly(id, op2)` and `LoadCombatant` (`GenesisCombatants.cs`): record copy from the monster file, hit-point-style fix (+0x24 x2/3), item remap / unworn-gear swap, the 0x38 slot cap, free party slot search, slot status / flags / stats. Checked on 600 random cases against the ROM (decompressor replaced by a plain stream, random monster files; whole records and all 56 slots compared); 10 mutations caught (one unreachable-value mutation is equivalent).

## Whole fights (ROM 0xE394)
`tests/combat_whole_vectors.json.gz` (84 KB, 34 fights: 37 generated, 3 dropped because the ROM run did not finish) holds complete fights run in the emulator with scripted menu / pad input: setup, every round, every turn, with event traces and RNG state compared. The port reproduces all 601 checks of them (combat=34).

## Not ported
* **Post-fight cleanup**: ported (see 04), medical aftermath, loot sharing screen and the starship repair at the end of scripted fights are all included; the host hooks `Aftermath`, `LootScreen`, `ScriptedFightEnd`, `GameOver` remain for drawing / story only.
* **Script engine**: the opcodes that call combat creation are not ported as a whole (operand reader 0x404A for variables / indirect values, the LZW decompressor 0x9ED8 for the monster file, the "monster not found" error box). The host passes parsed operands and the decompressed monster file.
* **Graphics, sound, animation, text boxes**: assumed to have no effect on game state (checked wherever a UI routine turned out to carry state).
* The "no room" hand-over path of the inventory and prompt messages other than 7 are implemented but untested.

## Running the tests
```
cd port
./run_tests.sh /path/to/rom.md          # C# tests against stored vectors (needs mono)
./run_tests.sh /path/to/rom.md --regen  # regenerate every vector set from the ROM (slow; needs python3 + unicorn)
```
A single section can be regenerated with environment counts, e.g. `N_fight=300` and zero for the others; `FIGHT_RANGE=a:b`, `COMBAT_RANGE=a:b` and `TRACE_LOF=1` help isolate a failing case.
Long generator runs are the expensive part: whole-fight sections take seconds each.
