# Documentation for ROM hackers

| Page | Read it to |
| --- | --- |
| [BUILDING.md](BUILDING.md) | rebuild the v1.0 patches from the original ROM, verify the result, and make your own changes with `build_combined.py` |
| [ROM-FORMATS.md](ROM-FORMATS.md) | understand the data: checksums, the game's compression, item and shop tables, monster records, the script language, free space |
| [SHOPS.md](SHOPS.md) | find out which list is which shop, what the original game sold, which weapon kinds survive, how prices and capacity work |
| [RECORD-FIELDS.md](RECORD-FIELDS.md) | read the 214-byte character and monster record field by field, including natural attacks, race and combat slots |
| [COMBAT-RULES.md](COMBAT-RULES.md) | see how to-hit, damage, initiative, effects, XP, loot and the computer's turn work, with ROM addresses |
| [PATCH-NOTES.md](PATCH-NOTES.md) | see how each fix and add-on works, with the bytes it changes |
| [SCRIPT-VARIABLES.md](SCRIPT-VARIABLES.md) | find what the RAM variables used by scripts mean |
| [ENCOUNTERS.md](ENCOUNTERS.md) | look up all 135 scripted fights and their script addresses |
| [SPACE-COMBAT.md](SPACE-COMBAT.md) | see how space encounters start and what the ship types are (code-derived) |
| [MAPS.md](MAPS.md) | read the 16 × 16 map format, wall and door bits, and which areas are walkable |
| [DOS-DATA.md](DOS-DATA.md) | read the DOS `.DAX` container and how its monster and item files line up with Genesis |
| [EMULATOR-TESTING.md](EMULATOR-TESTING.md) | repeat the tests: RAM reading, teleporting between areas, verification habits |
| [GRAPHICS.md](GRAPHICS.md) | find where pictures, icons, creature tokens and terrain are stored and how they are drawn |
| [COMPONENTS.md](COMPONENTS.md) | see what each file in `kit/` is and how the add-ons are derived |
| [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) | see what is untested, unknown or not done |

Player-facing documentation of the release is in [`../BR_Restored_Improved_v1.0_README.md`](../BR_Restored_Improved_v1.0_README.md).

These pages were written from the build kit, the tool sources and the author's research notes. Statements about game behaviour that were confirmed in play are marked as such; everything else is what the code and the ROM data show.
