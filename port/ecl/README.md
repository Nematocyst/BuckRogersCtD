# Genesis ECL script interpreter

`EclInterpreter.cs` (namespace `BuckRogersGenesis.Ecl`) decodes and runs the script modules of the Sega Genesis Buck Rogers: Countdown to Doomsday. It replaces the DOS
(GoldBox) rules entirely; the file that was supposed to be updated was not in the repository, so it was written from the format spec (`GENESIS_ECL_FORMAT.md`), the
decoded oracle (`data/scripts.json`) and, where they disagreed or were silent, the ROM's own script engine (loop at 0x3300, operand reader 0x404A, handlers from 0x3438).

Note: `BuckRogersData.cs` (Unity loader of the decoded data) defines a class `MonsterFile`; the combat port's monster-file class is called `MonsterBinFile` so both can live in one Unity project. The interpreter does not depend on either.

## Use
```csharp
var module = EclModule.FromHex(id, scriptModule.bytesHex, offset => BuckRogersData.Text(id, offset));   // loaded at 0x6AF6
var mem = new GenesisEclMemory();                    // work RAM, the module is copied to 0x6AF6 by the interpreter
var it = new EclInterpreter(module, mem, host, rng.ScriptRandom);   // host: IEclHost, rng: GenesisRng
EclStop why = it.RunEntry(0);                        // 0 run, 1 search, 2 precamp, 3 campint, 4 init  (or RunFrom(address))
```
`IEclHost` carries the side effects (print, menus, monsters, combat, store, XP, NEWECL ...). Opcodes without a typed method go to `host.Other(interpreter, instruction)`, which can read operands
with `it.Value(op)` and write with `it.Store(op, v)`. Opcodes with an unknown operand count (INPUTSTRING, MENU, SETTIMER, CHECKPARTY, ROB, CLOCK, SPELLS, PROTECT, JOURNAL, ANIMATE,
ICONMENU) stop the script with `EclStop.UnknownOpcode` and a log line. The last byte of a module is never executed (`EclStop.LastByte`).

## What the ROM showed that the hand-off brief did not say (all covered by the differential test)
* **Operand types:** 00 immediate byte, 01 the **byte at [addr]**, 02 immediate word, 03 the **little-endian word at [addr]** (not "&addr"), 80 text offset, 81 string variable. A destination stores one byte (type 01) or a little-endian word (type 03).
* **Memory** is all work RAM (0xFF0000 + addr), including the loaded module at 0x6AF6: `GETABLE [7310]` reads a data table inside the module. The windows 0x9AFC-0x9B4F and 0x9BF6-0x9C0F are the record / slot of character [0x9DA7].
* **COMPARE** compares operand 0 with operand 1 (unsigned) and sets six bits (EQ NE LT GT LE GE); `IFLT` is true when operand 0 < operand 1. The brief's "A = o[1], B = o[0]" reading is the **opposite** for LT / GT / LE / GE. AND / OR also set EQ (zero) / NE; COMPAREAND sets only EQ (both pairs equal) or NE.
* **Arithmetic:** ADD = op0 + op1 (32 bit), SUBTRACT = op1 - op0, MULTIPLY = 16 x 16 bits, DIVIDE = op0 / (op1 & 0xFFFF) with the remainder in [0x9E35] (zero divisor: 0).
* **FOR / ENDFOR** do not nest: FOR start,end stores them in [0x98EC] / [0x98ED] and the return address; ENDFOR adds 1 (byte) and loops while counter <= end (afterwards the counter is end + 1).
* **A false IF** skips the next instruction with the ROM's own operand-count table (0x482E); for the variable-length opcodes (ONGOTO, ONGOSUB, TREASURE, HMENU, WHMENU) that table under-counts, so a script must not put one right after an IF (none does).
* GETYN yes = EQ, FINDITEM found = EQ; HMENU / WHMENU / ONGOTO selectors are 0-based (checked on the jumper menu of module 0).

## Not verified against the ROM (marked "guess" in the code)
String comparison (ordinal equality here, the ROM calls 0x1348C), INPUTNUMBER values above 255, and every opcode that only produces a host event (the host decides what it does).

## Tests (`EclTests.cs`, run by `../run_tests.sh`)
a. the decoder reproduces all 13,936 instructions of `data/scripts.json` (plus the EXIT behind the one SKILLDAMAGE: the oracle gives that instruction 7 operands, the ROM reads 6 -- 13,937 in all) and all 2,261 text operands; b. module 0x10's first fight emits CLEARMONSTERS, LOADMONSTER 41 (count [9E70]), 32 x1, 39 ([9E6F]), COMBAT; c. module 0x5E stops at NEWECL 96; d. RANDOM 255 returns 0 without advancing the RNG; e. all 5 entry points of all 27 modules with four stub-host policies: no unknown opcode, last-byte read or bad address; g. flow and arithmetic on small synthetic modules; h. 1,500 random scripts (`../tools/gen_ecl_vectors.py`) run by the ROM's engine in the emulator: final memory, compare flags and RNG state are identical.

## Host: scripts that fight (`../csharp/unity/EclCombatHost.cs`)
`EclCombatHost` implements `IEclHost` on top of the combat port: it holds a `TurnContext` with the game's default party (8 records / slots from `default_party.bytes`) and follows the ROM handlers: LOADMONSTER (0x3544 -> `AddMonsters`), CLEARMONSTERS (0x3766: slot count 8, monster counter 0, rewards cleared), SETUPMONSTERS (0x3566: group mask [D8CC]), COMBAT (0x38BA: setup, rounds, clean-up), TREASURE (0x392C: credits, item ids filtered by ROM table 0x3978; checked against the ROM handler), ADDEP (0x3BCE: every present, not-down member, or the current character [9DA7] when the first operand is 0). The combat start reads its configuration from the script's memory like the ROM reads RAM: facing [9AFA], ground type [97AD], screen kind [97DC], ambush [9DB6], surprise [9DC1], party cell [9AF7] / [9AF6] and the dungeon map layers / tile class tables for the arena builder. `Frames` collects the board of every creature turn (for the viewer), `Log` / `Output` every host call and printed text; `AutoPlay`, `MenuHandler`, `YesNoHandler`, `NumberHandler`, `BeforeCombat` / `AfterCombat` are the places a UI plugs in. `eclhosttests.exe` (built by `run_tests.sh`): module 0x10's first fight run from its script equals the same fight built directly (frames, all records and slots, winner; three variants), TREASURE and ADDEP, a lone-monster fight with reward, and all 27 modules x 5 entries x 2 answer policies through the host (270 runs, 10 fights, no exception).
Character opcodes LOADCHARACTER, SAVECHARACTER, SKILL, PRINTSKILL, ADDNPC, DAMAGE and SKILLDAMAGE are wired and checked against the ROM (`csharp/GenesisScriptChar.cs`). Not wired: shops (STORE just records the number), the training screen (PROGRAM records it), NEWECL (recorded: the caller loads the module), FINDITEM (always "not found"), WHO / DAMAGE and the remaining `Other` opcodes (logged only). The exploration commands (STEPFORWARD / STEPBACK / HALFSTEP / HALFBACK / UNLOCKDOOR / HOWFAR / NEWREGION) are wired through `GenesisExplore`. NEWREGION reads `n` rectangles of four operands (2 + 4n operands in all); the oracle decodes only the first (see HOST_OPCODES.md). `HOST_OPCODES.md` lists every opcode with its ROM handler, status and the order of work.

## Script variable names
`ECL_MEMORY_MAP.md` and `data/ecl_memory_map.csv` name the script variables (party position [9AF6]/[9AF7]/[9AFA], surprise [9DC1], scratch [9E6F]-[9E78], story flags ...), with the confidence of each label (verified live / debug label / inferred / context only). The interpreter does not depend on them; they are for host and tooling authors. The task-3 hand-off was checked against the code: the operand counts of the brief equal `data/opcodes.json` for all 94 opcodes, and `scripts.json`, `opcodes.json` and `memory_map.json` regenerate byte-identically from the ROM with `export_unity_data.py`.
