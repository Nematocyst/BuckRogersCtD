# Open questions

What is unknown, untested or not yet done, so that nobody has to rediscover it.

## Untested

- **Difficulty add-on.** Nobody has fought a battle with it. The release README says so. It changes 25 enemy records (HP floor, armor, attack).
- **Sonic Stunner.** The cut weapons add-on sells it, but its stun effect has not been checked.
- **Saves from an unpatched ROM** have not been tested with the patch. Start a new game.
- **Weapon stat fields** other than the die size (`sides`) are read from the table layout and the character screen, not tested by changing them in play. See [ROM-FORMATS.md](ROM-FORMATS.md#weapon-and-armor-stat-table).

## Unknown data

- **Monster item slot bytes** `+5` and `+8`. A gear edit preserves them from the replaced slot; their meaning is not known.
- **Item record bytes 5, 8 and 9.** They are 0 in the records seen.
- **The 6,250 credits after the first fight.** The credits of the defeated enemies' records sum to 448 in the port, so the rest must come from item value or a script bonus. Not traced.
- **The scripted-XP table at `0x16402`** was not decoded.
- **Whether other code reads record bytes `+0x43`..`+0x4C` directly.** Effect ids `0x0A`, `0x0B`, `0x10`, `0x12` and `0x13` have empty handlers and no stage-list reader asks for them, but nobody searched for direct reads. See [COMBAT-RULES.md](COMBAT-RULES.md#effects-that-do-nothing).
- **Record fields not in [RECORD-FIELDS.md](RECORD-FIELDS.md)**, and the meaning of record byte `+0x52` bits other than bit 0.
- **Graphics gaps:** the link from a monster record to its encounter picture, the projectile flight, the shared icon and interface palettes, and the sheets 2, 4, 6, 8, 9 and 11 beyond being creation choices. See [GRAPHICS.md](GRAPHICS.md#not-found).
- **Script variables** that a poke test did not move (`9DBC`, `9DBF`, `97A1`, `9BCB`); see [SCRIPT-VARIABLES.md](SCRIPT-VARIABLES.md#still-open).
- **Space combat** is code-derived only: the tick length of an encounter, the effect of a failed hail, and the faction (no field found in the ship record). See [SPACE-COMBAT.md](SPACE-COMBAT.md).
- **Racial gaps in the original data.** Some race and item combinations never existed in the ROM (for example no Lunarian Needle Gun and no Venusian or Lunarian Rocket Pistol; v1.0 adds all three as new records). The v1.0 README lists what the item system supports.
- **Why the original cut weapon kinds were removed.** The shop rule is in game code and looks deliberate; see [SHOPS.md](SHOPS.md#which-weapon-kinds-survive-into-a-shop).

Resolved since earlier notes: record fields `+23` (race), `+44` and `+45` (the flat damage bonus of the natural attacks, see [RECORD-FIELDS.md](RECORD-FIELDS.md#natural-attacks)) and the empty effect handlers.

## Not done

Ideas that came up and were set aside:

- A regional check for enemy ship spawns. The scope has not been investigated.
- A combat log showing numeric damage dealt. It needs new 68000 code, like the shop comparison line.
- A level-10 cap. The level-9 variant has no level-10 entry, and a level-9 character who somehow earned enough XP for a tenth level would hit undefined training behaviour.
- Icons for the cut weapons. The game has no art for them; shops show a name next to an empty icon box.
- Parked without notes in this repository: DOS avatars, encounter edits, Cargobay.

## Documentation gaps

- The script language has a disassembler and an assembler for insertions, but no opcode-by-opcode reference. The operand counts are in `ecl_disasm.py`; the opcode names are in the ROM at `0x446E`. Eleven opcodes are never used and their operand counts are unknown.
- Combat is documented in [COMBAT-RULES.md](COMBAT-RULES.md); the oldest quirks in it (the unseeded neighbour rotation in pathfinding, the stale-register range word) have no in-game test.
- Older notes in [`archive/`](../archive/) contradict the current ones in places (for example which lists are real shops). The current ones are in [SHOPS.md](SHOPS.md) and the v1.0 README.
