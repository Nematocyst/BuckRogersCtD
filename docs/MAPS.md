# Dungeon and area maps

How the game stores the walkable maps (the first-person areas such as Chicagorg, the spy ship and the asteroid bases). The layout is the same as the Gold Box `GEO*.DAX` map blocks of the DOS version.

Most of this was read from work RAM while the game was running. Where the text says "measured", it was tested by walking the party in an emulator.

## Where the map lives

The map loader (function `0x40D0`, call sites `0x410A` and `0x412A`) reads two pointer tables, the script table at `0x38CE2` and the text table at `0x42B0A`, and decodes one entry of each into a `0x2C00`-byte buffer (see [ROM-FORMATS.md](ROM-FORMATS.md#module-tables-scripts-and-text)). The map itself is kept in work RAM at `0xFFB5A2`. The area's script chooses its map with `LOADFILES`.

The current position is in script variables: X `[9AF7]`, Y `[9AF6]`, facing `[9AFA]` (0–3); see [SCRIPT-VARIABLES.md](SCRIPT-VARIABLES.md).

## Layout

A 2-byte header followed by four 256-byte planes. Every map is a fixed 16 × 16 grid, cell index = `y × 16 + x`.

| Plane | Content |
| --- | --- |
| 0 | high nibble = north wall type, low nibble = east wall type |
| 1 | high nibble = south wall type, low nibble = west wall type |
| 2 | event id of the cell (the low 7 bits are meaningful) |
| 3 | door bits, 2 bits per side: bits 0–1 north, 2–3 east, 4–5 south, 6–7 west |

- Wall type 0 means no wall. Any other value is a wall and selects the wall graphic (0–15).
- Shared walls agree from both sides: a cell's north wall equals the cell above's south wall, and its east wall equals the right cell's west wall. This held for every captured map. All 64 boundary edges of a map carry a wall.
- Events: Chicagorg has 117 cells with an event; the starting square (5,7) has event `0x1A`. Scripts test the current square's event through `[9AF9]`.

## Door bits in the Genesis data

The DOS meaning of the door bits (0 none, 1 door, 2 locked, 3 wizard-locked) does **not** apply. In the Genesis data every wall edge has door bits 1 or 3, never 0. Measured by walking the party into 1,078 edges in 15 areas:

| Edge | Result |
| --- | --- |
| no wall (bits 0) | open: the party moved through 441 times; the few blocks were people, objects or event squares |
| wall with bits 1 | solid: 337 attempts, never passed |
| wall with bits 3 | doorway: passed 51 times; 6 attempts were blocked |

The six blocked doorways are doors that never open by design: scripts print that the rest of the base or of Salvation is restricted. Locking is done by scripts (`UNLOCKDOOR`), not by the map data.

Over the 20 captured areas: 639 doorways, 3,516 solid wall edges and 16,325 open edges.

## Areas

The walkable modules (script module ids) are 01, 03, 10, 11, 20, 23, 30, 31, 41, 51, 52, 53, 60, 61, 62 and 63. Module 00 (the debug jumper menu) and 22 (ship hub) are menus; 32 (space), 40 and 50 (planet arrival, wilderness) did not respond to the d-pad. Modules 21 → 20, 5E → 22 ("YOU CANNOT LAND": a ship-code flag is not set) and 5F → 63 continue by themselves.

## Open

- Which doors are locked cannot be read from the map; it lives in the scripts.
- The wall graphics (what each of the 0–15 values looks like) and how other dungeon maps load their art at exploration time were not examined; see [GRAPHICS.md](GRAPHICS.md#not-found).
