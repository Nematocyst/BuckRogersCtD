# Status, gaps and how to run the tests

## Verified against the ROM (135,348 monster-turn checks in the repo vectors)
enum 200, select 200, weapon 200, attack 300, move 300, nav 150, whole turns 1,200, explosive scoring 200 / execution 300 / blast 500, patch ticks 60, effect stages 800, begin-turn 500, rescue 383, manual turns 700, inventory 1,200, retreat prompt 400, whole fights 200, battlefield 150, deployment 200, fight setup 200, fight clean-up 250, loot screen 250. Earlier work (RNG, combat maths, progression, rewards, skills, initiative, pathfinding, actions) has its own vectors; see `port/README.md`.

## Not ported
* **Dungeon-map arena** (screen mode 6, ROM 0xB100): the map around the party becomes the battlefield. `BuildDungeonArena` is a host hook.
* **Post-fight cleanup**: ported (see 04), medical aftermath and loot sharing screen included; only the scripted-fight end (0x16EF0) is a host hook.
* **Combat slot creation** from encounter data (the script engine).
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
