# Findings: effect ids 0x0A, 0x0B, 0x10, 0x12 and 0x13 (record +0x43..+0x4C)

**Question.** Which stage handlers (ROM 0x664E) use the effect ids 0x0A, 0x0B, 0x10, 0x12 and 0x13 from a creature's record bytes +0x43..+0x4C, and what do they do?

**Answer in one line.** None of them does anything. Three of the five (0x10, 0x12, 0x13) are on a stage list, but their handler slot is a bare `RTS`; the other two (0x0A, 0x0B) are on no stage list at all, and their slot is a bare `RTS` too.

## How the stage mechanism works (ROM)
| Step | Address | What it does |
|---|---|---|
| Stage runner | `0x664E` | takes a stage number (valid below 0x17), reads that stage's list of effect ids from the table at `0x6670` (word offsets, each list ends with a byte >= 0x80), and calls `0x66D2` for every id on it |
| Per-effect step | `0x66D2` | asks `0x69DA` whether the creature has the effect; if it does, jumps through the word table at `0x66F8`, indexed by the effect id |
| "Has effect" test | `0x69DA` | looks for the id in record bytes `+0x43..+0x4C` (10 bytes, permanent), then in the timed-effect list at `[0xD49C]` (3 bytes per entry: slot, effect, turns) |

The port reproduces this in `csharp/GenesisEffects.cs` (`Stage`, `HasEffect`, `RunEffect`); the effects that have handlers are verified by the 800 stage runs in `MonsterTests`.

## Where the five ids appear
Stage lists decoded from ROM 0x6670 (ids in hex; stages not listed are empty):

| Stage | Where it runs | List | Contains one of the five? |
|---|---|---|---|
| 03 | secondary attack, before the damage dice | 1C 1E | no |
| 05 | after the damage roll (victim) | 14 19 **12** 17 18 1A 1B 03 | **0x12** |
| 06 | | 17 03 | no |
| 07 / 0F | start of the creature's turn | 01 02 0E / 0E 02 | no |
| 09 | a status effect is being applied (victim) | 16 15 **13** **10** 03 | **0x13, 0x10** |
| 0A / 0B / 0C | attack preparation, saving throw | 0D / 0D 0E / 0D | no |
| 0E | before the weapon choice | 20 | no |
| 12 / 16 | start of the round | 1D | no |

**0x0A and 0x0B are on no stage list.**

## What the handlers are
Dispatch table at ROM 0x66F8 (word offsets from 0x66F8), the relevant slots:

| Effect id | Handler address | Code |
|---|---|---|
| 0x04 .. 0x0C (includes **0x0A, 0x0B**) | `0x6758` | `RTS` |
| 0x0F .. 0x13 (includes **0x10, 0x12, 0x13**) | `0x6778` | `RTS` |

For comparison, the slots that do something: 0x01 (ends the creature's time), 0x03 (damage never above hit points), 0x0D, 0x0E, 0x14-0x1E and 0x20 (see the table in the header of `GenesisEffects.cs`). Slots 0x00 and 0x02 are bare `RTS` as well.

## Nothing else reads them
The helper `0x69DA` has five call sites: the stage runner (`0x66D8`), `0x6A5C`, and three direct tests that ask for effect **0x0E** (`0xE554`, `0xED3E`) and **0x03** (`0x1110C`). No call site asks for 0x0A, 0x0B, 0x10, 0x12 or 0x13. (I did not search for code that reads bytes `+0x43..+0x4C` directly without the helper; that would be unusual.)

## Who carries them
From the monster file records (`csharp/unity/Resources/BuckRogers/monster_file.bytes`, bytes `+0x43..+0x4C`); the six default party characters carry no effects at all.

| Effect | Carried by |
|---|---|
| 0x0A | RAM Assault Bot (only) |
| 0x0B | RAM Combat Bot (only) |
| 0x10 | Desert Ape, Mer. H.S. Robot, Sm. E.C. Gennie, Lg. E.C. Gennie, P. Combat Robot, RAM Assault Bot, RAM Combat Bot, RAM H.D. Robot, RAM L.S. Robot, RAM H.S. Robot, Ursadder, Acidicium, Ven. H.S. Robot, Stage 3 ECG, Buck Rogers |
| 0x12 | Mer. H.S. Robot, P. Combat Robot, Hypersnake, RAM Assault Bot, RAM Combat Bot, RAM L.S. Robot, RAM H.S. Robot, Ven. H.S. Robot, Buck Rogers |
| 0x13 | Mer. H.S. Robot, Sm. / Lg. E.C. Gennie, P. Combat Robot, RAM Assault / Combat / H.D. / L.S. / H.S. Bots, Space Rat, Swamp Hornet, Ursadder, Acid Frog (two records), Acidicium, Ven. H.S. Robot, Stage 3 ECG, Buck Rogers |

## Interpretation (not confirmed by the ROM)
The ids look like tags the designers put on robots and acid creatures. Their stage slots sit next to real handlers (0x13 and 0x10 in stage 9 beside the "immune to effect 0x0D / 0x0E" handlers 0x15 and 0x16; 0x12 in stage 5 beside the damage-reduction handlers). The most likely story is that handlers were planned (immunities, resistances) and never written, or were removed. The code does not say which; for the port this changes nothing, because an empty handler is already what `RunEffect` does for these ids.

## What this means for the port
* No code change is needed: `RunEffect` has no case for these ids, which is exactly the ROM behaviour.
* If a creature ever carries one of them with an effect that matters elsewhere (for example a future mod that fills in the handlers), the stage lists above are the places to hook.
* Verification: the stage lists and the dispatch table were decoded from the ROM bytes (`0x6670`, `0x66F8`) and the handler slots disassembled; the carrier lists come from the exported monster file.

## Reproduce
```python
# stage lists: for stage 0..0x16, p = 0x6670 + word at 0x6670 + 2*stage; read bytes until one >= 0x80
# dispatch:    handler(e) = 0x66F8 + word at 0x66F8 + 2*e  (0x6758 and 0x6778 hold the bytes 4E 75 = RTS)
```
