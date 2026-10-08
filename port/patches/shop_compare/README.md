# Shop hover comparison line (ROM patch)

One extra text line in the shop's buy screen. With the cursor on an item:

* weapon: `DMG 2-9 X3  NOW 1-8 X3` (damage range and attacks per round with this weapon, then what the selected character has now)
* armor:  `AC 0  NOW 4` (armor class the character would have, then the current one; lower is better, as on the character sheet)
* anything else, explosives included: no extra line

Files: `shop_compare.s` (source, GNU `as`, `m68k-linux-gnu-as -m68000 --register-prefix-optional`), `shop_compare.bin` (504 bytes),
`shop_compare.ips` (against the original USA/Europe ROM), `patch_bytes.txt` (address and bytes), `build_patch.py` (assembles, applies, writes the IPS),
`test_shop_compare.py` (differential test).

## Patch

| address | bytes | what |
|---|---|---|
| 0x016ABE (6 bytes) | `4E F9 00 0F 50 00` | `jmp 0xF5000` over `bsr 0x11C4C ; bra.b 0x16AC8` (the end of the shop branch of the item description, 0x169C6) |
| 0x0F5000 (504 bytes) | see `patch_bytes.txt` | the routine |

Free space used: 0xF5000-0xF51F7. It does not overlap the v8 IPS (0x18E, 0x3D46, 0xF19B8, 0xF1A2F, 0xF2000-0xF212F, 0xFFFCC); the test also passes on the v8-patched ROM.
The routine runs the instruction the hook replaced (`jsr 0x11C4C`, the price line) first, and ends with `jmp 0x16AC8` (the original destination), so everything else is untouched.
The hook is only reached in shop mode ([0xBA60] set): the loot screen's branch of the description never gets there, so the loot screen has no extra line (see below).

## How it works (ROM routines used)

* On entry a3 = the 10-byte item copy under the cursor (id, +4 tier, +9 type); the shop branch of 0x169C6 leaves it there.
* `0x6E70`: a2 = item, returns a3 = weapon-table row (0x779E + 8 x id), the same lookup the stat routine uses, so patched tables are followed.
* `0x6EEC`: returns a3 = slot, a2 = record of the character the screen shows ([0xCA20]).
* `0x6E80`: d0 = id, a1 = record, returns d0 = weapon-specialisation hits (+0x4D..+0x51). `0x6E9E`: a1 = record, returns d1 = strength damage bonus (melee only).
* `0x11C4C`: prints the string in the buffer 0xD5AE centred on the cursor [0xD5D6] (column) / [0xD5D8] (row), colour [0xD5AC]. Text is ASCII; both cases show as capitals.
* Numbers are formatted by the patch itself (`num`, `sgn`); it does not use the game's number printers (those append words such as "credits").

Weapon numbers follow the stat routine 0x6D1E exactly (byte arithmetic, negative shows 0, as the sheet does):
bonus = table +5 + (melee: strength damage bonus) + specialisations + item +4; min = dice + bonus; max = dice x sides + bonus; attacks = table +2 / 2.
"NOW" is read from the slot bytes the sheet prints (+6, +8, +0xA, +0xC), so it includes everything the sheet includes (unarmed, effects).
Armor: bonus = table +1 - 0x32 + item +4, as 0x6D1E adds it to slot +4; the sheet shows AC = 0x3C - slot +4.

## Fix after the first in-emulator run
The first version used a4 and a5 as scratch pointers and called the text flush (0x11C4C) with them still set; the flush writes through a4/a5 as the VDP control/data ports,
so the third line was computed (buffer and cursor were right) but its tiles went to RAM instead of the screen. The routine now takes the caller's a4/a5 back from its saved
registers before the flush, and the test asserts a4 = 0xC00004 and a5 = 0xC00000 at every flush and compares a4/a5 after the call. (The old version fails that test.)
A VDP-model run of the whole description now writes the third line to row 0x18, columns 8-29, with the same tile attribute (0xC0xx) as the price line.

## Screen position
The box is rows 0x16-0x19 (columns 2-0x25), all four interior (clearing it writes 36 cells on each of the four rows; checked by running 0x169A4 with the VDP model).
Name is row 0x16, price 0x17; the new line is row 0x18 (column 0x13, centred, colour 0xC000 like the other two). Row 0x19 stays free.

## Evidence
`python3 -I test_shop_compare.py ROM [cases]`: runs the real 0x169C6 with and without the patch for random characters, items, tiers and specialisations (800 cases on the
original ROM, 400 on the v8-patched ROM) and checks that
1. the name and price lines are identical, and all RAM outside the text scratch is identical (registers a6/a7/d2/a2/a3 too), so buying is not touched;
2. the extra line equals an oracle: the candidate is placed in the hand (or armor) slot, the game's own 0x6D1E recomputes the slot, and the sheet's formulas are applied to it.
Mutation checks (wrong tier term, wrong armor offset, missing attacks halving) are caught.
Real characters (default party): Flavius with laser pistol and spacesuit: `DMG 2-9 X3  NOW 2-9 X3`, rocket pistol `DMG 1-10 X4`, armor id 0x16 `AC 0  NOW 4`.

## Not done / unverified
* No screenshots: this environment has no renderer for the game screen. The row and clamping are derived from the code and the VDP model; please look at the screen once.
* The loot screen: its description never reaches the hook. It could be added by a second hook at 0x16AC4/0x16ACE, but that path is also taken by the "leave" cell where a3 is not an item.
* The test fixes the character as [0xCA20] and does not run the menu around the description (cursor movement is the unchanged ROM menu).
* Racial variants: the routine goes through 0x6E70 like the game, but I could not check items with ids >= 0x27 (the original table has no rows for them).
