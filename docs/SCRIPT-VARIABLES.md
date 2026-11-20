# Script variables (ECL memory map)

ECL scripts address the Genesis **work RAM**: ECL `[9E70]` is 68000 address `0xFF9E70`. Values are bytes. Script modules are loaded at `0x6AF6` (see [ROM-FORMATS.md](ROM-FORMATS.md)). When reading RAM through an emulator that exposes it word-swapped (stable-retro's `get_ram()` does), read index `addr ^ 1`.

Confirmation levels: **live** = poked or watched in an emulator, **label** = the game's own debug text names it, **inferred** = from how scripts read and write it.

Check: one C press before the first fight changes `[9E70]` from 8 to 4 (`RANDOM 2` + 2 in module 0x10) and the fight then loads 4 Terrine Warriors. `[9E6F]` = 7 is the NEO Warrior count.

## Engine and party state

| ECL address | Meaning | Confidence |
| --- | --- | --- |
| `9AF7` | party X ("X POSITION:"; teleports in 21 modules) | label |
| `9AF6` | party Y (+1 per square walked south) | live |
| `9AFA` | facing 0–3 (3 → 2 when turning south) | live |
| `9AF9` | attribute byte of the current square (`0x1A` on square (5,7), 0 elsewhere); scripts test bit masks | live (partly) |
| `9E08` | CURDISK ("WHAT DO YOU WANT TO SET CURDISK TO?") | label |
| `96F6` | base of the party / game-state block ("PARTY PLUS WHAT OFFSET?" with SAVETABLE) | label |
| `97AD` | combat land type ("INPUT LANDTYPE BYTE.") | label |
| `9E78` | combat option 0–15, 0 = normal (4th operand of `SETUPMONSTERS`) | label |
| `98EC` | selected character index (read by LOADCHARACTER / HOWFAR / GETABLE) | inferred |
| `9DBD` | combat result / state, compared with 128 after fights | inferred |
| **`9DC1`** | **surprise: 1 = party surprised, 2 = enemies surprised** | live |
| `97DC` | area / combat setting (16 changed the first auto-target) | live, one run |
| `9909`–`9920` | ship systems (hull / ammo-like values), set in the "NEO mechanics repair your ship" scenes | inferred |

Setting `9DC1` before the first fight: with 1 the enemies get a free round (the party is hit at once); with 2 the enemies skip their first round. The fight code uses it as described in [COMBAT-RULES.md](COMBAT-RULES.md#turn-order-and-initiative).

The party's character records are in work RAM at `0xFFBA68` (6 × 214 bytes), with the same layout as the monster records; see [RECORD-FIELDS.md](RECORD-FIELDS.md).

## Other variables

- **Scratch registers `9E6F`–`9E78`:** menu choices, loop counters and monster counts, reused by every module. A value only means something at the point where it is used.
- **Module-local temporaries `97F7`–`9800`:** menu destinations (`HMENU` / `WHMENU`) and per-module state.
- **Story event flags** (65 of them): set once (`SAVE 1`) and tested later. The scene text next to them labels them, for example `98A1` "YOU FIND A SECURITY CARD IN A DRAWER." and `9876`, which is tested before landing on the Mariposa.
- **Story bitfields `9827`–`9867`** (30 bytes): changed only by OR and AND, with several events per byte.
- **Space and encounter variables:** see [SPACE-COMBAT.md](SPACE-COMBAT.md).

## Still open

A poke test before fight 1 on `9DBC`, `9DBF`, `97A1` and `9BCB` changed nothing in the fight. The test was run after the module had started, so bytes that are only read when a module starts, or while walking, cannot show up. `9DBF` = 255 is written after "DOORS SEALED" / "FUSED SHUT" and EXIT, which makes it a candidate for a push-back flag. See [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md).

## How to test a label

Poke the byte in the emulator before a scene and see whether the script branches, or read it after an event. A disassembly of the script modules shows every reader and writer of an address.
