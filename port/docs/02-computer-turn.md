# The computer-controlled turn

Everything a monster (or an AI-controlled party member) does on its turn, ROM 0xEF64 and what it calls. Code: `GenesisMonsterAi.cs`, `GenesisMonsterWeapons.cs`, `GenesisMonsterTurn.cs`, `GenesisAreaWeapons.cs`, `GenesisAreaAttack.cs`, `GenesisAreaChoice.cs`, `GenesisEffects.cs`.

## Decision order (RunTurn)
1. A creature that already waited forgets its target.
2. It keeps its target if still alive, hostile and in line of fire; otherwise it picks one of the nearer half of the visible enemies (`SelectTarget`, using the sorted list from `EnumerateTargets`).
3. It equips its best weapon (`ChooseWeapon`: scores every item slot, shifts ranged weapons down for cover, keeps ammo/armour in place).
4. Breadth-first path to the target (`Navigate`), then a loop: attack if in range (half range for ranged weapons, adjacent for melee), else take the next step (cost in movement points, reactions from enemies), until blocked or done.
5. A creature that has not waited sets time 1 ("wait"); one that has waited attacks anything in reach and ends its turn.

## How each piece was approached
* **Target list / selection / path search**: pure functions of the map, so the easiest to prove. The search's weird start-cell behaviour (a bogus first step) was found by the tests and kept.
* **Attack execution** (`PrepareAttack`/`ExecuteAttack`): the ROM collects all hits first and applies them *last hit first*; the damage list overlaps other globals when 9+ hits occur. Both were reproduced after the comparison showed shifted globals.
* **Movement and reactions** (`MoveStep`, `Reactions`): stepping off the map is "fleeing" (status 0x85) unless a faster enemy sees the creature; every step can draw reactions from enemies with the reaction flag.
* **Whole-turn controller**: the ROM keeps "time was 1" in register d6 but the target enumeration reuses d6 as a counter, so any turn that searched for a target takes the "has waited" branch. Found when whole-turn tests failed while every piece passed; reproduced with a flag.
* **Explosive weapons** (types 5..12): scoring mode (the expected value of throwing at each cell) and execution mode (the blast: scatter, line of fire, patches left on the map, status effects, saving throws). A question about Buck Rogers rolling away from grenades led to checking the ignored bytes: the effect "3" relocates him out of the blast (the damage clamp and immunity are in the effect hooks).
* **Special-effect hooks** (ROM 0x664E, `Stage`): at fixed stages every effect the creature carries runs its handler (stun, armour penalties, damage immunities, slowing, instant death, spitting, Buck Rogers' damage clamp). Verified per stage and per effect (800 stage runs) and inside whole turns. A comparison-direction bug in three handlers (>= 0x32 zeroes damage) was found by the stage tests.
* **Healer rescue** (0x10200 / 0x1021E): a party creature with healing skill walks to a fallen friend, stabilises it and may revive it (skill checks, nearest free cell, revived mask). Verified with 383 cases.

## What the original does that you might not expect
See [05-quirks.md](05-quirks.md): rocket "full damage" never hurts, a normal hit of 128+ is dropped, the weapon choice reads the actor's tile at x*21+y, explosives scatter without running the to-hit preparation first.
