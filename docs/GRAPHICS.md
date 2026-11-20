# Graphics in the ROM

Where the pictures, icons, creature tokens and terrain are stored and how the fight screen draws them. Everything here was checked by decoding the data and looking at the result, or by running the ROM's own draw routines in an emulator with a small model of the video chip. No game art is included in this repository; the formats are described so that you can extract it from your own ROM.

## Contents

1. [Piece format](#piece-format)
2. [Encounter pictures](#encounter-pictures)
3. [Icons](#icons)
4. [Creature tokens](#creature-tokens)
5. [Party tokens](#party-tokens)
6. [Frames and fight animation](#frames-and-fight-animation)
7. [Terrain](#terrain)
8. [Party figure keys](#party-figure-keys)
9. [Not found](#not-found)

## Piece format

Pictures are "pieces": an LZW stream (the same compressor as the monster file; decoder at `0x9ED8`, init `0x9E76`; see [ROM-FORMATS.md](ROM-FORMATS.md)) that holds

1. a header (tile count, tilemap bytes, palette mask),
2. the tilemap (tile index plus flip bits),
3. up to four 32-byte palettes,
4. 4-bit 8 × 8 tiles.

The loader at `0x9DD4` (55 call sites) writes the tiles straight into video memory. Besides the tables below, about 40 distinct streams are loaded from fixed addresses (title font, interface parts, terrain blobs).

## Encounter pictures

Table `0x51360`, 57 entries. Each is 11 × 11 tiles (88 × 88 px) with one palette and 1 to 12 frames (tilemaps of 121 × n words): portraits and scenes (warriors, aliens, officials, ships, planets, landscapes) shown when an encounter starts. All 57 decode. How a monster record points to its picture is not known.

## Icons

Table `0xF14F2`, 185 entries: 3 × 3-tile items, weapons, menu icons and projectile frames. The pieces carry no palette; it comes from a shared palette set elsewhere. 120 are 3 × 3 or 6 × 6. The item icon id in an item record indexes this set; see [ROM-FORMATS.md](ROM-FORMATS.md#item-records).

## Creature tokens

The fight screen draws a creature (routine around `0xCB00`–`0xCC30`, size / position helper `0xCC66`, tile blitter `0x961C`) by looking up the creature's record byte +`0x42` in an 8-byte-entry table at `0x9A14` (52 entries, ended by a first byte `0xFF`). The lookup is `0x99BC`; the loader that cuts frames out of the piece is `0x9BB6`.

| Entry bytes | Content |
| --- | --- |
| 0–3 | 32-bit pointer to a piece |
| 4 | monster id |
| 5 | tilemap bytes per frame |
| 6 | animation set (0, 1 or 2) |
| 7 | extra |

- Each piece is one creature's sprite sheet of **18 frames**; several ids share a sheet (ids 0 and 43, 20–22, …).
- Byte 5 is 18 (9 words = 3 × 3 tiles, 24 × 24 px) for normal creatures and 36 (18 words) for record size types 2 (tall, 3 wide × 6 high tiles) and 3 (wide, 6 wide × 3 high). This holds for all 52 entries.
- The blitter reads width × height words per frame, row by row. Facings 5 and up are drawn mirrored (the frame is read backwards with the horizontal-flip bit toggled), so a sheet holds only half of the facings.
- **Palette:** the tilemap words use palette line 0. The fight screen builder (`0x14D60`) writes the same colour contents for every area type: line 0 is the 16-word system palette at `0x9710` and line 1 follows at `0x9730`. Lines 2 and 3 come from piece palettes. With that palette the creatures have natural colours.

## Party tokens

Table `0x998C`, 12 sheets with the same 18-frame layout and palette. A record byte +`0x42` with bit 7 set selects one: `0x99BC` clears the bit and indexes the table with the rest. Monsters have bit 7 clear and use the `0x9A14` table above.

## Frames and fight animation

Frames within a sheet (monster id 0 and the party sheets):

| Frames | Content |
| --- | --- |
| 0–2 | standing, facing the viewer |
| 3–5 | aiming and firing (5 has the muzzle flash) |
| 6–8 | side view / walking |
| 9–11 | blank filler |
| 12–14 | a second set of poses |
| 15 | hurt / kneeling |
| 16 | lying (dead) |
| 17 | ready |

**Standing creatures** (`0xAD5A`, one creature per call, onto plane A): cell (x, y) of the 21 × 21 map is the 3 × 3 (or 3 × 6, 6 × 3) tile block at plane column x × 3, row y × 3. Creatures outside the scrolled window, and slots with flag bit 2, are skipped. The frame comes from the sprite-buffer chunk of the creature's type (slot +`0x11` × 4; four frames [0, 6, 12, 16] loaded at fight start by `0x982A`):

- alive: facing 0 = frame 0 (front); 1–3 = frame 6 (side); 4 = frame 12 (back); 5–7 = frame 6 mirrored;
- down (status bit 7): frame 16.

**Attack animation** (module `0xCAEA`, run only during an attack; state 0 = frame 17, 1 = frame 15, 2 = frame 16, 3 = frame 11, 5 = aim pose):

1. *Attacker* (`0x1074A`): turns towards the target (`0x116AC`), draws the aim pose (state 5: facing 0 → frame 3, 1–3 → frame 4, 4 → frame 5, 5–7 → frame 4 mirrored) with the attack sound and delay (`0xE606`, `0x1B900`), loads hand-held frames (`0xCA7E`), flies the projectile for a ranged weapon (`0x108AA`, icon frames from the `0xF14F2` table, not traced in detail) and redraws the standing frame.
2. *Victim that survives* (`0x115E0`–`0x11626`): hit pose (frame 17) for 15 ticks, then idle.
3. *Victim that goes down* (`0x11B30`): states 1, 2, 3, 4 in turn, each held 20 ticks (35 for size types 2 and 3). A death sound is played first (message `0x44` / `0x45`). Then, by animation set (entry byte 6): set 0 leaves the body (drawn later as frame 16); set 1 makes the creature vanish; set 2 shows frame 11 first, then vanishes. Set 0 draws states 0, 1, 2, 5; set 1 draws 3 specially; set 2 draws 3 and treats 4 specially (table `0xCC36`, 6 bytes per set).
4. *Area attacks* (`0x112A0`): the same hit and death routines per victim, with a delay between.

One tick (`0x75FA`) is assumed to be about 1/60 s on NTSC; this was not measured.

The map-object renderer (`0xC060`–`0xC3F0`) is a hardware-sprite manager for the dungeon and overland screens (object list at `0xB018`, sprite table `0xEC00`) and is not part of the fight view.

## Terrain

The battlefield ground is painted by `0xFEF8` (called through `0xFEA8` when the view scrolls or starts). For each cell of the 21 × 21 tile map at `0xCACA`, the byte `& 0x7F` selects a 3 × 3-tile block (block `id × 9` of the tilemap of the screen mode's tile piece, loaded through `0x14D60` and `0x8360`), written to plane A at column x × 3, row y × 3. Bit 7 of the map byte is the creature marker and is ignored.

The art depends only on the screen mode, not on the ground type; the generator just puts different ids on the map. Screen mode follows `[0x97DC]`:

| `[0x97DC]` | Mode | Look |
| --- | --- | --- |
| `0xA2` | 4 | outdoor: alien jungle (plants, mushrooms, trees) |
| `0xA8` | 5 | rocky / desert set |
| any other | 6 | dungeon / ship interior: red metal wall blocks of many shapes, grey floors, a starfield tile; 110 ids in use (the arena builder at `0xB100` writes ids 2 and up) |

The screen builder produced identical memory for all 13 ground types of a mode, and the dungeon art did not change when varying `9BBE`, `BA5F`, `BA60`, `B525`, `9AFB`, `97AE`, `97AD` or `9BB6`. Which block goes where comes from the map's wall bits and class tables.

## Party figure keys

A party member's record byte +`0x42` is `0x80` + the sheet number of the table at `0x998C`.

**The default party** (8 records at `0x6BAAD`, loaded by `0x1F32`) carries its keys in the data:

| Name | Race | Sex | Career | Key | Sheet |
| --- | --- | --- | --- | --- | --- |
| Flavius | 2 | M | warrior | `0x83` | 3 |
| Celeste | 2 | F | warrior | `0x85` | 5 |
| Pierre | 1 | M | rocket jock | `0x8A` | 10 |
| Nichole | 1 | F | rogue | `0x87` | 7 |
| Roarke | 3 | M | medic | `0x81` | 1 |
| Janelle | 3 | F | medic | `0x80` | 0 |

No formula in the code maps race, sex and career to a sheet; the sheet is an arbitrary data choice per character.

**Created characters:** the creation screen's figure grid (2 × 6) writes the key at `0x99E` as `0x80 | (row × 6 + column)`, so any sheet can be chosen. It is a memory-to-memory move (`move.b -$20(a6),$42(a1)`), which is why a search for store instructions missed it at first and an earlier note wrongly concluded that created characters keep key 0. See [RECORD-FIELDS.md](RECORD-FIELDS.md#created-character-figure-key).

**Allies** (`AddAlly`, table `0x48DA`, NPC id → key): `0x6A` → `0x8A`, `0x6B` → `0x83`, `0x6C` → `0x87` (the sheets of Pierre, Flavius and Nichole). The story NPCs `0x3B`–`0x3E` use their own monster-token entries (ids 59–62).

## Not found

- The projectile flight (`0x108AA`): icon frames and path.
- Frames 1–2, 4–5 (partly), 7–10 and 13–14 of the sheets are used only by the aim and fire poses or not at all.
- The link from a monster record to its encounter picture.
- The shared palettes of the icons and interface.
- Area-dependent colour variants (none seen), animated tiles, and whether other dungeon maps load their own wall art at exploration time (loader `0x40D0`, tables `0x38CE2` / `0x42B0A`).
