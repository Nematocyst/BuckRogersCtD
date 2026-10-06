# Graphics scoping: what is in the ROM and what it would take

## What was found (all checked by decoding and looking at the pictures)
* **Format.** Pictures are "pieces": an LZW stream (the same compressor as the monster file, decoder at ROM 0x9ED8, init 0x9E76) holding a header (tile count, tilemap bytes, palette mask), the tilemap (tile index + flip bits), up to four 32-byte palettes and 4-bit 8x8 tiles. The loader is 0x9DD4 (55 call sites), which writes the tiles straight to video memory.
* **Encounter pictures: table 0x51360, 57 entries.** 11x11 tiles (88x88 px), one palette each, 1 to 12 frames (tilemaps of 121 x n words). They are portraits and scenes (warriors, aliens, officials, ships, planets, landscapes), i.e. the big picture shown when an encounter starts. All 57 decode (`tools/export_pictures.py`).
* **Icon set: table 0xF14F2, 185 entries.** 3x3-tile items, weapons, menu icons, projectile frames (no palette inside the piece: it comes from a shared palette set elsewhere). 120 are 3x3 or 6x6 and are exported in grey.
* Other pieces are loaded from fixed addresses (title font, interface parts, terrain blobs, about 40 distinct streams in the 55 call sites).

## What was NOT found
* The **small battlefield sprites** of the creatures (the tokens on the 21x21 map). They are not in the two tables above. Candidates not yet checked: the 12-entry table at 0x998C, the 6-entry one at 0x17BD4, pieces loaded from fixed addresses during the fight screen, or sprites built from tiles by the drawing code. Finding them needs a trace of which pieces load while a fight screen is drawn (the emulator harness stubs the UI, so this needs a short run of the real fight-screen code with the video routines mapped).
* The link from a monster record to its encounter picture (probably a byte of the record or an index in the monster file; not checked).
* The shared palettes of the icons and the interface.

## Cost estimate
* **Extract the encounter pictures into Unity**: done as a tool (PNG per picture, frames as a strip). About a unit to wire them to the monster ids (find the record field) and show them in the viewer. Art is not committed to the repo (it is the game's copyright); run the tool on your own ROM.
* **Find and extract the battlefield sprites**: 1 to 2 units of emulator tracing; unknown until the sprite source is found.
* **Palettes / interface art / animation**: further units; the game uses tile animation and palette cycling, and a faithful port needs the VDP's colour handling.
