# Open questions

What is unknown, untested or not yet done, so that nobody has to rediscover it.

## Untested

- **Difficulty add-on.** Nobody has fought a battle with it. The release README says so. It changes 25 enemy records (HP floor, armor, attack).
- **Sonic Stunner.** The cut weapons add-on sells it, but its stun effect has not been checked.
- **Saves from an unpatched ROM** have not been tested with the patch. Start a new game.
- **Weapon stat fields** other than the die size (`sides`) are read from the table layout and the character screen, not tested by changing them in play. See [ROM-FORMATS.md](ROM-FORMATS.md#weapon-and-armor-stat-table).

## Unknown data

- **Record fields `+23`, `+44` and `+45`.** Field `+23` was seen with the values 0, 4 and 5. They were parked with no description of what they are. They are most likely in the 214-byte monster record (HP is at `+46`), but that is a guess. If so, the monster tool can set any record byte with `"NAME@OFFSET=VALUE"`, which is the way to test them.
- **Monster item slot bytes** `+5` and `+8`. A gear edit preserves them from the replaced slot; their meaning is not known.
- **Item record bytes 5, 8 and 9.** They are 0 in the records seen.
- **Item effects** `0x0A`, `0x0B`, `0x10`, `0x12` and `0x13` have empty handlers in the original code and do nothing.
- **Racial gaps in the original data.** Some race and item combinations never existed in the ROM (for example no Lunarian Needle Gun and no Venusian or Lunarian Rocket Pistol; v1.0 adds all three as new records). The v1.0 README lists what the item system supports.

## Not done

Ideas that came up and were set aside:

- A regional check for enemy ship spawns. The scope has not been investigated.
- A combat log showing numeric damage dealt. It needs new 68000 code, like the shop comparison line.
- A level-10 cap. The level-9 variant has no level-10 entry, and a level-9 character who somehow earned enough XP for a tenth level would hit undefined training behaviour.
- Icons for the cut weapons. The game has no art for them; shops show a name next to an empty icon box.
- Parked without notes in this repository: DOS avatars, encounter edits, Cargobay.

## Documentation gaps

- Details of how the game computes damage and to-hit are described only where the shop comparison line needed them (see the v1.0 README).
- The script language has a disassembler and an assembler for insertions, but no opcode-by-opcode reference. The operand counts are in `ecl_disasm.py`; the opcode names are in the ROM at `0x446E`.
- Older notes in [`archive/`](../archive/) contradict the current ones in places (for example which lists are real shops). The current ones are in [SHOPS.md](SHOPS.md) and the v1.0 README.
