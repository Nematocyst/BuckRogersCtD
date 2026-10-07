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
a. the decoder reproduces all 13,936 instructions of `data/scripts.json` and all 2,261 text operands; b. module 0x10's first fight emits CLEARMONSTERS, LOADMONSTER 41 (count [9E70]), 32 x1, 39 ([9E6F]), COMBAT; c. module 0x5E stops at NEWECL 96; d. RANDOM 255 returns 0 without advancing the RNG; e. all 5 entry points of all 27 modules with four stub-host policies: no unknown opcode, last-byte read or bad address; g. flow and arithmetic on small synthetic modules; h. 1,500 random scripts (`../tools/gen_ecl_vectors.py`) run by the ROM's engine in the emulator: final memory, compare flags and RNG state are identical.
