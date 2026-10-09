# ECL memory map — Buck Rogers: Countdown to Doomsday (Genesis)  (2026-10-04)

**ECL addresses are Genesis work-RAM addresses**: ECL `[9E70]` = 68000 `0xFF9E70` (byte values; stable-retro's
`get_ram()` is word-swapped, so read index `addr ^ 1`). Modules are loaded at `0xFF6AF6`. Verified live: one C press
before the first fight `[9E70]` changes 8 -> 4 (= `RANDOM 2` + 2 in module 0x10) and the fight then loads 4 Terrine
Warriors; `[9E6F]` = 7 = the NEO Warrior count.

Full table: `ecl_memory_map.csv` (261 addresses: 209 RAM variables + 52 tables inside script modules). Columns:
category, label, confidence, writes/reads, modules, ops, constants compared/set, evidence.

## Engine and party state
| ECL addr | Meaning | Confidence | Evidence |
|---|---|---|---|
| 9AF7 | party X | debug label | "X POSITION:"; teleports in 21 modules |
| 9AF6 | party Y | **verified live** | +1 per square walked south (6..11) |
| 9AFA | facing 0-3 | **verified live** | 3 -> 2 when turning south |
| 9AF9 | current-square attribute byte | live (partly) | 0x1A on square (5,7), 0 elsewhere; scripts test bit masks |
| 9E08 | CURDISK | debug label | "WHAT DO YOU WANT TO SET CURDISK TO?" |
| 96F6 | base of party/game-state block | debug label | "PARTY PLUS WHAT OFFSET?" + SAVETABLE |
| 97AD | combat land type | debug label | "INPUT LANDTYPE BYTE." |
| 9E78 | combat option 0-15 (0 = normal) | debug label | SETUPMONSTERS 4th operand |
| 98EC | selected character index | inferred | read-only; LOADCHARACTER/HOWFAR/GETABLE |
| 9DBD | combat result/state | inferred | read-only; compared with 128 after fights |
| **9DC1** | **surprise: 1 = party surprised, 2 = enemies surprised** | **verified live** | poke before fight 1: =1 enemy free round (party hit at f625), =2 enemies skip their first round |
| 97DC | area/combat setting | live, 1 run | =16 changed the first auto-target; starting slots identical |
| 9909-9920 | ship systems (hull/ammo-like values) | inferred | set in "NEO MECHANICS REPAIR YOUR SHIP" scenes |

**Character records**: work RAM `0xFFBA68`, 6 x 214 bytes, **same layout as the monster records** (PROJECT_STATE
3.1.1): name +0, abilities +16..21, career +24, level +25, XP +30..33, attack +34, movement +36, armor +37, HP +46,
gear +84... Verified on the pregenerated party (HP 25/23/17/15/11/11 = combat slots; XP 2000/2000/1250/1250/1500/1500;
Flavius abilities = the pregen file).

## Script variables
- **Scratch registers 9E6F-9E78**: menu choices, loop counters, monster counts; reused by every module. Values
  only mean something at the point of use.
- **Module-local temps 97F7-9800**: menu destinations (HMENU/WHMENU) and per-module state.
- **Story event flags** (65): set once (SAVE 1) and tested; label = the scene text next to them, e.g. 98A1
  "YOU FIND A SECURITY CARD IN A DRAWER.", 9876 tested before landing on the Mariposa.
- **Story bitfields 9827-9867** (30): OR/AND only, several events per byte.
- **Other / open**: poke test before fight 1 (2026-10-04): 9DBC, 9DBF, 97A1, 9BCB changed nothing in the fight
  (caveat: poked after module start, so bytes read only at module start or while walking cannot show here).
  9DBF = 255 is written after "DOORS SEALED"/"FUSED SHUT" + EXIT (push-back candidate). `poke_test.py` reruns it.

## How to test a label
Poke the byte in the emulator before a scene and watch the script branch, or read it after an event. The ECL
listings (`ecl_disassembly.zip`) show every reader and writer of an address.
