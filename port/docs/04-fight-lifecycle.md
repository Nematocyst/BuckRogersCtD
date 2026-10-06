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

## Clean-up after the fight (ROM 0x15FDA) - `GenesisCombatEnd.cs`
`CombatCleanup()` settles everything once `CombatRounds` returns.
* **Statuses**: mode becomes 3; the temporary item bits (flag 0x30) of every record are cleared; the moving / reaction flags of the 8 party slots are cleared; fled creatures (status low nibble 5) come back (status 1, noted in the fled mask [0xD8DA]); everyone else loses status bit 6; a knocked-out solo creature becomes 0x84 (or returns if it had fled).
  Then the stats of all 8 slots are worked out again. **The ROM uses the record with the slot's *index* here, not the slot's own record number** (harmless while slots 0..7 are the party, wrong for a monster in them).
* **Nobody left**: no party member standing or fled = game over (host hook `GameOver`, ROM 0x7588 never returns). If nobody stands but some fled, the downed are removed and the party is flagged wiped ([0x9DBD] = 0xFF).
* **Treasure** (`ScriptedTreasure`, 0x1621E): the pool (14 items at 0x6AF6, count [0xB9F3]) is emptied and the scripted treasure ids go through `GenesisRewards.AddItem`.
* **Rewards** (`Tally`, 0x1631E, reusing the already verified `AddItem`): defeated monsters give experience (unless record +0x52 bit 0), credits and gear (unless solo / player-driven fights); the pool of experience is divided among the standing party members.
* **Victory** (state part of 0x1640C): a solo knockout or a lost-with-survivors fight forfeits experience, credits and loot; otherwise every standing, not fled slot **including empty ones** gets the experience (record +0x1E) and the credits join the money [0x9BD0] (they are *not* cleared from [0xBA34] here).
* **Medical aftermath** (`MedicalAftermath`, 0x16B96, run after a scripted fight or any fight with XP): a creature with effect 3 (Buck Rogers) is stood up at 45 HP. Every party creature (allies too) that is hurt, dying or down is looked at: status 6 needs a healer (standing party member with first-aid points, d10 + 4 within them) or is dead (0x87, listed); the rest are healed by the best healer roll (d24, d48 after a scripted fight, + 2x / 4x the points - 12; or a d6 from the medicine skill); the dying return as 1 if healed, else unconscious 0x84. HP rises (never past the maximum), and each listed dead creature is brought back to 0x84 with one revive item (id 0x1F) while the party has any. Nothing is written if nobody needed anything.
* **Host hooks, not ported**: `Aftermath` (called after the above, for the screens), `LootScreen` (0x165A0, sharing the pool out), `ScriptedFightEnd` (0x16EF0), `GameOver`.
* Verified with 250 stored cases including the aftermath (300 in trial runs; every flag combination: demo, solo, shop, scripted fights, player-driven party, dying characters, healers, revive items).
