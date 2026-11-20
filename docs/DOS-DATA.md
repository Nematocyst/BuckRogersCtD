# The DOS version's data files

The DOS (SSI Gold Box engine) release stores its data in `.DAX` containers. The Genesis game shares most of its data with it, which is how many Genesis fields were identified. This page lists the container format and what is known to match. DOS files are not part of this repository.

## Container format

- Header: a `u16` data offset, then `(data_offset − 2) / 9` entries of `{u8 id, u32 offset, u16 raw_size, u16 stored_size}`. DOS values are little-endian.
- **Blocks start at `data_offset + 2 + offset`.** With that start, every record of every indexed file decodes to exactly its `raw_size` (`MON0CHA` 53 of 53, `MON0ITM` 35 of 35, `MON0SPC` 24 of 24, `CHARS` 71 of 71). Starting at `data_offset + offset` gives blocks that begin with the previous block's last two bytes and produces phantom size mismatches.
- Compression is PackBits-style RLE. If `raw_size == stored_size` the block is stored raw. Otherwise read a signed control byte `c`: `c >= 0` copies `c + 1` bytes, `c < 0` repeats the next byte `−c` times.

## Files

| File | Content |
| --- | --- |
| `MON0CHA.DAX` | 53 monster records of a fixed 263 bytes. Field offsets are in [RECORD-FIELDS.md](RECORD-FIELDS.md#field-map) (DOS column) |
| `MON0ITM.DAX` | per-monster equipment: entries of 62 bytes. Each is 46 bytes of unknown data, then a Genesis-style 10-byte item record at +46, the quantity / ammo (`0xFA`) at +56 and the **price as a little-endian `u16` at +57** (a Terrine Warrior shows 335 + 500, laser pistol plus spacesuit). Entry counts match a community gear list for 10 of 13 monsters; the Pirate Leader, Mercurian Leader and RAM Martian CGennie are each off by one. Monsters with no gear listed have no block, while the robots and Talon have blocks (probably built-in, non-droppable weapons) |
| `MON0SPC.DAX` | arrays of 9-byte entries, probably per-monster effects |
| `GEO*.DAX` | the maps; same layout as in [MAPS.md](MAPS.md) |
| `ECL1`–`ECL6.DAX` | scripts. `ECL1` decodes to structured bytes but its opcodes are not decoded |

## MON0CHA against the Genesis record

A DOS record starts with a Pascal string (length byte, then the name). Abilities are at +16..22, career at +40 (DOS codes differ: 4 engineer, 5 rogue), level +41, XP total `u32` at +47, attack +51, movement +54, base armor +55, HP +69 (repeated at +227) and the XP reward `u16` at +178. Every field matches the Genesis record in 52–53 of 53 records. Real differences: the Desert Ape's armor (55 against 57 on Genesis) and a different DOS id for the Mercurian Warrior.

An earlier note put HP and ability offsets at "name + 0x45" and "name + 0x10"; those offsets were artifacts of the wrong block start described above.
