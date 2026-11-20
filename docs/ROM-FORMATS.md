# ROM data formats

What the tools in [`kit/tools/`](../kit/tools/) know about the Genesis ROM of *Buck Rogers: Countdown to Doomsday* (USA, Europe; 1,048,576 bytes). All addresses are ROM file offsets. Integers are big-endian.

Everything here is taken from the tool sources and the release notes. Where a field is a guess or was never tested in play, the text says so. Unknown fields are listed in [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md).

## Contents

1. [ROM identification](#rom-identification)
2. [Checksums](#checksums)
3. [Compressed data (LZW variant)](#compressed-data-lzw-variant)
4. [Module tables: scripts and text](#module-tables-scripts-and-text)
5. [Monster file](#monster-file)
6. [Weapon and armor stat table](#weapon-and-armor-stat-table)
7. [Item records](#item-records)
8. [Shop lists](#shop-lists)
9. [Level cap](#level-cap)
10. [Script language (ECL)](#script-language-ecl)
11. [Free space](#free-space)

## ROM identification

Standard Sega header at `0x100`:

| Field | Value |
| --- | --- |
| Console name | `SEGA GENESIS` |
| Copyright / date | `(C)T-50 1991.DEC` |
| Domestic and overseas title | `BUCK RODGERS COUNTDOWN TO DOOMSD` (the misspelling "RODGERS" is in the ROM) |
| Serial | `GM T-50286 -00` |
| Region | `U` |
| Size | 1,048,576 bytes |
| Original checksums | game `0x10D1310C` at `0xFFFCC`, header `0xD7B6` at `0x18E` |

The first routine the game runs (at `0xFFFB0`) sums the ROM and compares it with the game checksum below. On a mismatch the game hangs on a blank screen with no message.

## Checksums

Two checksums must be correct for the ROM to boot and to match known dumps. Both are recomputed by every build tool (`game_checksum` and `header_checksum` in `build_monster_patch.py`).

| Checksum | Stored at | How it is computed |
| --- | --- | --- |
| Game checksum | `0xFFFCC` (4 bytes) | Sum of the 32-bit words from `0x00000` up to (not including) `0xFFFB0`, skipping the word at `0x18C`, modulo 2³² |
| Header checksum | `0x18E` (2 bytes) | Sum of the 16-bit words from `0x200` to the end of the ROM, modulo 2¹⁶ |

Write the game checksum first. The header checksum sum covers the end of the ROM, including the new game checksum.

## Compressed data (LZW variant)

The game stores monsters, scripts and script text as LZW streams. The variant (see `decoder2.py` and `encoder.py`):

- Codes are packed most-significant bit first. A stream is a sequence of **segments**. Code `0x100` ends a segment and clears the dictionary (the bit stream continues without byte alignment); code `0x101` ends the whole stream.
- Literals are `0x00`–`0xFF`. New dictionary entries are numbered from `0x102`.
- Decoder state at the start of a segment: code width `size = 8`, `range_min = 2`, `range_max = 0x1FF`, next entry `E = 0x102`.
- To read a code: read `size` bits. If the value is `<= range_min`, read **one more bit** and place it above the top (`code |= bit << size`). The extra bit exists only under that condition, and an encoder must write it even when it is 0.
- After every code except the first, an entry is added: `range_min += 1`, `E += 1`. When `E == range_max`, the width grows: `size += 1`, `range_max = 2^(size+1) - 1`, `range_min = 0xFFFF`.
- The `code == E` case (a code not yet in the dictionary) is handled as in standard LZW.

`decoder2.decode_stream(rom, offset)` returns the list of segments and the end bit position. `encoder.encode_stream(segments)` is its inverse. Decoding and re-encoding reproduces the original bits for the monster file, all 27 script modules and 26 of the 27 text modules (all but `0x61`).

## Module tables: scripts and text

Scripts (ECL) and their text live in two parallel tables, one entry per module, 27 modules with the same ids in both:

| Table | At |
| --- | --- |
| Script (ECL) modules | `0x38CE2` |
| Text modules | `0x42B0A` |

Table layout: `[u32 base][module ids ... 0xFF][u32 offsets]`. The offsets array starts at `base`; module *k* starts at `base + offset[k]`. Each module is an LZW stream (see above).

- Text module `0x61` has no END code and cannot be edited by the tools.
- To make a module longer, `ecl_patch.py` writes the new stream to free space and changes only that module's table entry.

## Monster file

One LZW stream at `0x9E77C`, two segments. Decoded layout:

| Offset | Size | Content |
| --- | --- | --- |
| 0 | 2 | `00 36` (54 monsters) |
| 2 | 54 | monster ids |
| 56 | 54 × 214 | monster records |

Record layout (known fields):

| Offset | Size | Content |
| --- | --- | --- |
| +0 | 16 | name, NUL-terminated |
| +46 | 1 | HP |
| +84 + 10·k | 10 | item slot *k* (up to 13 slots; an item id of 0 ends the list) |

A gear edit copies the 10-byte base-item record into the slot and keeps the old slot's bytes +5 and +8 (their meaning is not known). The base item table used for this is the original one at `0xF17CC` (item id 1 is at `0xF17D6`); race variants are not supported by the monster tool.

If the edited stream is longer than the original, it is written to `0xF8000` and the 32-bit address in the instruction operand at `0x48F2` is changed to point there. Only its low three bytes differ, so patches show a write at `0x48F3`. The difficulty add-on uses this.

## Weapon and armor stat table

38 records of 8 bytes at `0x77A6`. The record for base item id *i* (`0x01`–`0x26`) is at `0x779E + 8·i`. The table covers base items only; racial variants have no entry of their own and use their base item's record. Item ids are listed in the [item ids table](#base-item-ids).

| Byte | Field name in the tool | Meaning | Status |
| --- | --- | --- | --- |
| 0 | `class` | item class: 0 weapon or grenade, 1 armor, 2–8 other (the grenade launcher is class 6) | read from the table |
| 1 | `type` | weapon type: 0 melee, 1 needle / bolt / crossbow, 2 rocket pistol and rifle, 3 laser, 4 cover gun, 5 heat gun, 6 sonic stunner, 7 rocket launcher, 8 grenade, 9 plasma thrower | read from the table |
| 1 | `armor` | for class 1: armor value (spacesuit 54 up to battle armor w/fields 62) | read from the table |
| 2 | `rof2` | attacks per round × 2 (laser pistol 6 = 3 attacks, as the character screen shows) | matches the character screen |
| 3 | `dice` | number of damage dice | not tested live |
| 4 | `sides` | damage die size | **verified in play** (laser pistol d8 → d20 gave hits of 15, 16, 11) |
| 5 | `bonus` | stun grenade has 2 | meaning not tested |
| 6 | `flag` | 1 on grenade launcher, plasma thrower, rocket launcher | meaning not tested |
| 7 | `range` | range (grenades 3 … laser rifle 120) | not tested live |

Range values: grenades 3, heat gun 6, needle gun 12, laser pistol 32, rocket rifle 80, laser rifle 120. Live tests on the original: attacks `6 → 2` cut the first fight's volleys from 1–3 hits to mostly 1 (the attacker can still add attacks on top of the weapon's), and `sides 8 → 20` made a hit of 15 appear.

**Armor value** of an item: `stat byte 1 − 0x32 + item record byte 4` (the racial tier), as the stat routine at `0x6D1E` adds it. One point of armor value is one point of AC.

## Item records

The item table has one 10-byte record per item id. Its address is stored in three places in the code: `0xBF0`, `0x16254` and `0x1628C`.

- Original game: table at `0xF17CC` (record for id *i* at `0xF17CC + 10·i`).
- Release patch: table moved to `0xF2200` and the three pointers updated, to make room for new records (ids up to `0x5E`).

| Byte | Content |
| --- | --- |
| 0 | base item id (indexes the stat table; for plain items it equals the record's own id) |
| 1, 2, 3 | indexes into the word table; the displayed name is word[3] + word[2] + word[1] |
| 4 | racial tier added to the item (Martian 1, Venusian 2, Mercurian 3, Lunarian 4) |
| 5 | 0 in the records seen; meaning unknown |
| 6–7 | base price in credits, 16-bit |
| 8–9 | 0 in the records seen; meaning unknown |

The word table is at `0x11DC4`: a list of 16-bit offsets from the table start to NUL-terminated strings (index 0 is empty). Names are built from three words, which is why race variants share words with their base items.

Fields of the same record seen in other places: byte 4 is also added to a weapon's damage (the racial tier is a flat damage bonus, see [COMBAT-RULES.md](COMBAT-RULES.md#damage)) and bytes 6–7 are the price that [Prices](#prices) turns into what a shop shows.

Gear slots in character and monster records use this same 10-byte layout; see [RECORD-FIELDS.md](RECORD-FIELDS.md).

Examples from `add_racial_rockets.py`:

```
Venusian Rocket Pistol (id 0x5D):  0A 0A 0B 14 02 00 07 D0 00 00   base 0x0A, race word 0x14, tier 2, price 2000
Lunarian Rocket Pistol (id 0x5E):  0A 0A 0B 16 04 00 1F 40 00 00   base 0x0A, race word 0x16, tier 4, price 8000
```

Id `0x5C` is the Lunarian Needle Gun (added by the v9 base patch).

### Base item ids

These are the ids `0x01`–`0x26` of the stat table, with the names the tools use:

| Id | Name | Id | Name | Id | Name |
| --- | --- | --- | --- | --- | --- |
| 01 | knife | 0D | laser rifle | 19 | demo charge |
| 02 | mono knife | 0E | heat gun | 1A | ecm package |
| 03 | cutlass | 0F | sonic stunner | 1B | protective goggles |
| 04 | sword | 10 | grenade launcher | 1C | "lost." |
| 05 | d.r. x-bow | 11 | plasma thrower | 1D | rope |
| 06 | needle gun | 12 | rocket launcher | 1E | belt |
| 07 | bolt gun | 13 | polearm | 1F | poison antidote |
| 08 | mono sword | 14 | spacesuit | 20 | mini explosive |
| 09 | laser pistol | 15 | smartsuit | 21 | explosive grenade |
| 0A | rocket pistol | 16 | heavy body armor | 22 | treating wounds grenade |
| 0B | rocket rifle | 17 | battle armor | 23 | stun grenade |
| 0C | cover gun | 18 | battle armor w/fields | 24 | dazzle grenade |
| | | | | 25 | chaff grenade |
| | | | | 26 | aerosol mist grenade |

The "lost." entry is exactly as it appears in the tool's name list.

### Availability and icon tables

- **`0xA829`, availability / redirect table**, one byte per **base item** (ids `0x01`–`0x25`; racial variants are not covered, as `0x41` and `0x48` sold correctly with it untouched). `0xFF` or any value `>= 0x80`: sold as it is (and a loot drop is allowed); `0x00`: never shown in a shop (knives and the sonic stunner in the original, and never dropped); a small value `n`: the item is replaced by base item `n` (sword → mono sword, bolt gun → needle gun, rocket rifle and cover gun → rocket pistol, laser rifle → laser pistol, polearm → cutlass). The cut weapons add-on changes these bytes; see [PATCH-NOTES.md](PATCH-NOTES.md#cut-weapons-add-on) and [SHOPS.md](SHOPS.md#which-weapon-kinds-survive-into-a-shop).
- **`0xA802`, icon table**, read by the routine at `0xA7BC`. It returns `0xFF` for kinds without art. Both are used by the shop and inventory list builders (`0x7C1A`, `0x1690A`). Icon ids index the set in [GRAPHICS.md](GRAPHICS.md#icons).

### Prices

A price shown in a shop is the item record's price (bytes 6–7) × `[0x9E63]` / 16, where `[0x9E63]` is the port's markup in sixteenths (32 for × 2, 64 for × 4). Selling gives half of the price (bytes 6–7), times the amount. A test that opens a shop through an injected `STORE` script command leaves `[0x9E63]` at 0, so every price reads 0 there.

## Shop lists

The game has 16 shop lists, numbered 0–15. See [SHOPS.md](SHOPS.md) for which list is which shop.

In the original ROM the lists are packed tightly behind an offset table at `0x3D76`, so no list can grow. The release patch rewrites the lookup code at `0x3D46`–`0x3D53` to read a pointer table instead:

| What | At |
| --- | --- |
| Pointer table | `0xF2000`: 16 entries, each a 32-bit ROM offset of a list |
| List | `[count][item id × count]`, in a 16-byte slot (count plus up to 15 items) |

The original loader (`0x3D3C`) refuses to add a 15th item to the working copy, so **14 items is the effective maximum per shop** even though a 16-byte slot has room for 15. Whether the on-screen grid could show more than 14 is untested.

Item ids in a list refer to the [item records](#item-records). `build_addons.py` checks that a list never grows into the next one.

## Level cap

In the original ROM each career's level-9 XP threshold is the sentinel `FF FF FF FF`, which keeps every career at level 8. The level-9 variant replaces the four sentinels with real thresholds (32-bit values; the build stops if a sentinel is not found):

| Address | New XP threshold | Career |
| --- | --- | --- |
| `0xC32` | 110,000 | Rocketjock |
| `0xC52` | 192,000 | Medic |
| `0xC72` | 250,000 | Warrior |
| `0xC92` | 110,000 | Rogue |

## Script language (ECL)

The game's events, shops, encounters and quest logic are written in a bytecode, which the tools call ECL. `ecl_disasm.py` produces readable listings; `ecl_patch.py` edits and reassembles modules. Facts below were verified on all 27 modules.

**Opcode table.** The 94 opcode names at `0x446E` are plain text in the ROM, not compressed. The 27 modules use 83 of the 94 opcodes; the other 11 never appear (INPUTSTRING, MENU, SETTIMER, CHECKPARTY, ROB, CLOCK, SPELLS, PROTECT, JOURNAL, ANIMATE and ICONMENU, whose operand counts are unknown). The scripts hold 135 fight entry points; see [ENCOUNTERS.md](ENCOUNTERS.md). `SPACECOMBAT` is opcode `0x1F` (see [SPACE-COMBAT.md](SPACE-COMBAT.md)). The RAM variables scripts use are in [SCRIPT-VARIABLES.md](SCRIPT-VARIABLES.md).

**Text region.** The 27 text modules occupy `0x42B96`–`0x510FE`, indexed by the table at `0x42B0A`. The first one holds the developer debug menu ("START FROM SCRATCH OR USE THE JUMPER?" with ARENA, TREASURE, STORE, SOUND, SPRITE, ARTTOOL, TESTS and DEMO tools).

**Modules.** Loaded at `0x6AF6`. The decoded module must fit the load buffer, which ends at `0x96F6` (`0x2C00` bytes). The first 20 bytes are five `GOTO` entry points, in this order: run, search, precamp, campint, init. The game overwrites the last byte of a module after loading it, so edited modules end with 2 padding bytes.

**Instructions.** One opcode byte followed by operands. An operand is `[type][lo]([hi])`:

| Type | Meaning | Syntax in listings |
| --- | --- | --- |
| `00` | immediate byte (1 byte follows) | `5` |
| `01` | memory word at an address | `[9DC1]` |
| `02` | immediate word | `#500` |
| `03` | address | `&9E71` |
| `80` | text: 16-bit byte offset into the text module with the same id | `T001A` |
| `81` | string variable | `S0001` |

Jump targets appear as `L6B40`. Opcode names come from a table in the ROM at `0x446E`: 94 16-bit offsets, relative to `0x446E`, to NUL-terminated names. The operand count of each opcode is in the `CNT` table in `ecl_disasm.py` and `ecl_patch.py`. Five opcodes take a variable number of operands (first operand, a count *n*, then *n* operands): `ONGOTO`, `ONGOSUB`, `WHMENU`, `HMENU` and `TREASURE` (credits, *n*, then *n* item ids).

**Control flow.** A path ends at `EXIT`, `GOTO`, `RETURN`, `NEWECL` or `ENCEXIT`, unless the previous instruction is an `IF`. `PRINTRETURN` does not end a path in this build. `NEWECL` does: in module `0x5E`, address `6B3D` would otherwise fall into the "YOU CANNOT LAND" text.

**Coverage of the disassembler:** 1,764 of 1,764 jump targets land on instruction starts, 2,279 of 2,279 text operands land on string starts, no decodes overlap, and 58,442 of 61,173 bytes are reached. The remainder is data tables or unreferenced code.

**Edits (`ecl_patch.py`).** Insertions move no addresses. Whole instructions at the insertion site (at least 4 bytes) are replaced by a `GOTO` to new code appended at the end of the module; the new code is followed by the displaced instructions and a `GOTO` back. An insertion is refused if the site follows an `IF`, if an `IF` would be displaced, or if a jump target falls inside the displaced bytes. `T"text"` operands add a new string to the module's text.

A module that still fits its original slot is written in place. Otherwise it moves to free space (starting at `0xFB000`) and only its table entry changes. The courier reward in the release is such an edit; the exact command is in `kit/build_release.sh`.

## Other data worth knowing

- **Pregenerated characters:** 8 records of 214 bytes at `0x6BAAD`, loaded by the code at `0x1F32`; the first six are the default party (Flavius, Celeste, Pierre, Nichole, Roarke, Janelle). Record layout in [RECORD-FIELDS.md](RECORD-FIELDS.md).
- **Random numbers:** the RNG table is seeded from 256 words at `0x390E2`, inside script module 00. Do not edit that module in place; see [COMBAT-RULES.md](COMBAT-RULES.md#random-numbers).
- **Graphics tables:** encounter pictures `0x51360`, icons `0xF14F2`, creature tokens `0x9A14`, party tokens `0x998C`, system palette `0x9710`. See [GRAPHICS.md](GRAPHICS.md).
- **Combat tables:** XP thresholds `0x0C16`, attack values `0x0BB0`, initiative `0x7786`, strength / dexterity modifiers `0x7741` / `0x776F`, effect stage lists `0x6670` and handlers `0x66F8`. See [COMBAT-RULES.md](COMBAT-RULES.md).

## Free space

Free space in the ROM is filled with `0xFF`; the tools check that a region is all `0xFF` before using it. Areas the release uses:

| Range | Used for |
| --- | --- |
| `0xF2000`–`0xF21FF` | shop list pointers and lists |
| `0xF2200`– | item records, 10 bytes each (ids `0x00`–`0x5E`) |
| `0xF5000`–`0xF51F7` | shop comparison code |
| `0xF6000`– | character creation fix |
| `0xF8000`– | difficulty add-on (relocated monster file) |
| `0xFB000`– | relocated script modules and text |

Every patch of the release, with its addresses, is listed in the *Technical notes* section of the [v1.0 README](../BR_Restored_Improved_v1.0_README.md).
