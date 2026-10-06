# Graphics scoping: what is in the ROM and what it would take

## What was found (all checked by decoding and looking at the pictures)
* **Format.** Pictures are "pieces": an LZW stream (the same compressor as the monster file, decoder at ROM 0x9ED8, init 0x9E76) holding a header (tile count, tilemap bytes, palette mask), the tilemap (tile index + flip bits), up to four 32-byte palettes and 4-bit 8x8 tiles. The loader is 0x9DD4 (55 call sites), which writes the tiles straight to video memory.
* **Encounter pictures: table 0x51360, 57 entries.** 11x11 tiles (88x88 px), one palette each, 1 to 12 frames (tilemaps of 121 x n words). They are portraits and scenes (warriors, aliens, officials, ships, planets, landscapes), i.e. the big picture shown when an encounter starts. All 57 decode (`tools/export_pictures.py`).
* **Icon set: table 0xF14F2, 185 entries.** 3x3-tile items, weapons, menu icons, projectile frames (no palette inside the piece: it comes from a shared palette set elsewhere). 120 are 3x3 or 6x6 and are exported in grey.
* Other pieces are loaded from fixed addresses (title font, interface parts, terrain blobs, about 40 distinct streams in the 55 call sites).

## Battlefield creature tokens: found (ROM table 0x9A14)
* The creature drawing routine (around 0xCB00-0xCC30, size / position helper 0xCC66) looks the monster up by record byte +0x42 (which `LoadCombatant` sets from the monster id) in an 8-byte-entry table at ROM 0x9A14 (52 entries, ended by a first byte 0xFF): `[32-bit pointer to a piece][monster id][frames][animation set][extra]`.
* Each piece is one creature's sprite sheet: 3x3-tile (24x24 px) frames, 9 tilemap words per frame, 18 frames (162 words) or 36 (324 words, `entry +5` = 18 / 36): walking, attacking, hurt and fallen poses per facing. Shared sheets exist (several ids point to the same piece, e.g. ids 0 and 43). Creatures of size class 2 / 3 (tall / wide) use bigger frames (the draw code sets 3x6 or 6x3 tiles, `0xCC66`); this is why the exporter slices them wrongly into 3x3 pieces.
* The pieces contain no palette (palette mask 0): the fight screen supplies the colours (not yet traced; the table's last two bytes, 0 / 1 / 2 and 0 / 2 / 3 / 8 / 0x26 / 0x34 / 0x35, are the first candidates).
* `tools/export_tokens.py` writes one grey sheet per monster id (checked by eye: warrior, ape, spider, scarab and other poses are recognisable).

## What was NOT found
* The **palette** of the tokens, the exact frame layout for size-class 2 / 3 creatures, the party members' tokens (player characters are drawn from other sheets, probably chosen by race / career, not found) and the animation sequencing (which frame is used when).
* The link from a monster record to its encounter picture (probably a byte of the record or an index in the monster file; not checked).
* The shared palettes of the icons and the interface.

## Cost estimate
* **Extract the encounter pictures into Unity**: done as a tool (PNG per picture, frames as a strip). About a unit to wire them to the monster ids (find the record field) and show them in the viewer. Art is not committed to the repo (it is the game's copyright); run the tool on your own ROM.
* **Battlefield tokens**: located and exportable (grey). To use them in the viewer: palette (about a unit of tracing), frame layout for big creatures, the animation order (about a unit), and the party's tokens.
* **Palettes / interface art / animation**: further units; the game uses tile animation and palette cycling, and a faithful port needs the VDP's colour handling.
