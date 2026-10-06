# Fight lifecycle: setup and round loop

Code: `GenesisCombatSetup.cs` (start), `GenesisCombatLoop.cs` (rounds), `GenesisTurns.cs` (initiative). `RunCombat()` = ROM 0xE394.

## Setup (ROM 0x1503C)
1. **Mode and flags**: fight mode, per-fight flags reset, start cell from the facing, screen mode from the area kind.
2. **Battlefield generator (0x149BA)**: fill the 21x21 map with ground (a coin per cell), then stamp features from per-area lists: each feature rolls its chance, a dice count of stamps, and a random point within a spread (a quarter circle via an integer square root). A stamp only lands on plain ground.
   * *How it was cracked*: the first port placed stamps far off the map. The ROM's "random 0x81" is a divide by a *signed byte* (-127), so the remainder is almost the whole random word, of which only the low byte is kept; and the square root input can be negative, which sends the ROM's Newton iteration through 16-bit division overflow. The port copies the instruction semantics (`ISqrt`).
3. **Deployment (0x14738)**: party from the start cell, monsters from the opposite side or from each group direction; a breadth-first search for free, passable cells; large creatures need a second cell; after six cells occupied cells stop expanding the search. Creatures without room are marked out of the fight.
4. **Control assignment (0x14DF6)**: non-ally party members are player controlled; each of them may recruit allies with a skill check.
5. If a side is empty the fight is not on; otherwise markers, counters (round 0, peace counter 3), effects and patches reset.
6. *Bug found by the whole-start tests*: the port kept the old terrain tables; the ROM switches the terrain flag table and tile script when the area is chosen.

## Round loop (ROM 0xE3A8..0xE42C)
* **Round start**: each side's first able creature that passes a skill-2 check gives its side +1 attack modifier; then every creature rolls its round start (backstab mask, movement, attacks, initiative).
* **Turns**: count the living; if a side is gone, the fight ends; the creature with the highest (speed, tie-break) acts; repeat until nobody has time.
* **Round end**: round counter, patches tick, timed effects count down, the **peace counter** (reset to 3 by every attack) counts down and at 0 the monsters give up, dying party members count toward death (0x87 after 16 rounds).
* **Findings from whole-fight tests**: the AI turn reads the pad; the scroll routine puts the cursor on the acting creature, which matters for explosives; an uninitialised stack variable in the manual turn made one comparison unreproducible, so both sides now start it at 0.
* **Verification**: 200 whole fights in the repo; 37 whole fights from the very first call (`RunCombat`) matched in trial runs (vectors not stored, regenerate with `N_combat`).
