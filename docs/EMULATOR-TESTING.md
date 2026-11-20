# Testing in an emulator

How the findings in these docs were checked, so that you can repeat them. The tools are not part of this repository; this is the method.

## Setup

- The Python library `stable-retro` runs the ROM headlessly. A Genesis Plus GX core was used for the checks in these docs. A first attempt with BizHawk under Mono and a virtual X display never got through the boot sequence.
- RAM is read as `0xFF0000 + address`. Some readers return RAM word-swapped: read index `address ^ 1`. This is why older notes list some combat-slot fields one byte off; see [RECORD-FIELDS.md](RECORD-FIELDS.md#combat-slots).
- The game is turn-based and reads one press at a time, so scripted input must be slowed to roughly one press per second. The opening is a scripted scene that needs a specific press pattern.

## Teleporting

A jump is a few script instructions written into the running module in RAM at the next instruction: `SAVE disk,[9E08]`, optionally `SAVE x,[9AF7]`, `SAVE y,[9AF6]`, `SAVE f,[9AFA]`, then `SAVE 0,[9822]`, `SAVE 0,[9823]` and `NEWECL module`. This is the debug jumper's own code (module 00, label `L6C00`). It runs on the next press of C and needs no ROM patch.

- The disk is the tens digit of the module id for all story teleports.
- Most modules set their own entry position on load, overriding X and Y (module `0x20` always enters at (15,5)).
- With a patched ROM, scripts already in RAM are the old ones: jump again after loading a state to pick up the patched scripts.
- During a fight the script area at `0xFF6AF6` is reused, so module detection reports nothing.

Save states of every module, taken after loading, make later tests repeatable. The party in each is the level-2 pregenerated party, so late fights are not balanced tests until the party records (`0xFFBA68`, 214 bytes each) are raised. Story flags are those of the game's start.

## Habits that paid off

- Check a combat-affecting change with numbers, not screenshots: read the damage list, the record bytes or the flag in RAM.
- Poke a script variable before a scene and watch which branch runs (see [SCRIPT-VARIABLES.md](SCRIPT-VARIABLES.md#how-to-test-a-label)).
- Run the ROM's own routine in a 68000 emulator against a re-implementation on random inputs. The rules in [COMBAT-RULES.md](COMBAT-RULES.md) were verified that way.
- Never ship a combat change without seeing it work in a real fight. The first rocket fix passed boot tests and killed the party in round one; see [PATCH-NOTES.md](PATCH-NOTES.md#rocket-special-fix).
