# Genesis ECL format — what EclInterpreter.cs must change to run the Genesis scripts

EclInterpreter.cs was ported from the DOS (GoldBox Explorer) rules. Differences found in the Genesis ROM, all
verified on the 27 modules (scripts.json holds the decoded result):

1. **Text operands**: operand type 0x80 is a 16-bit little-endian **byte offset into the same-id text module**
   (scripts.json `strings`), not an inline 6-bit packed string. Type 0x81 = string variable (16-bit).
   Operand layout: `[type][lo]` for type 0, `[type][lo][hi]` for 1, 2, 3, 0x80, 0x81.
2. **Load address 0x6AF6**; the first 20 bytes are 5 GOTO entry points (run, search, precamp, campint, init).
   Script memory addresses are Genesis work-RAM addresses (byte values): [9AF6] party Y, [9AF7] X, [9AFA] facing,
   [9E08] CURDISK, [9DC1] surprise (1 party surprised, 2 enemies surprised), scratch [9E6F]-[9E78].
3. **Operand counts** differ from DOS for several opcodes (opcodes.json): SETUPMONSTERS 4 (DOS 3); TREASURE is
   variable (credits, n, n items; DOS 8); WHMENU and HMENU are variable (dest, n, n texts); VIEW 2; WHO 1;
   DUEL 0; PRINTSKILL 3; SAVETABLE 3. ONGOTO/ONGOSUB: selector, n, n targets.
4. **Control flow**: EXIT, GOTO, RETURN, ENCEXIT and **NEWECL end the script** (unless the previous instruction is an
   IF, which skips exactly one instruction). **PRINTRETURN (0x33) does not** end it (it prints a line break).
5. LOADMONSTER = (monster id, count, picture); ADDEP 1, n gives n XP to every member; training = `SAVE 0,[9807];
   SAVE 0,[9808]; SAVE 127,[9D9E]; PROGRAM 0`.
6. The game overwrites the **last byte** of a loaded module; never rely on it.
7. Unused opcodes (count unknown): CHECKPARTY, ROB, CLOCK, SPELLS, PROTECT, JOURNAL, ANIMATE, ICONMENU, MENU,
   SETTIMER (INPUTSTRING's count is the DOS value, unverified).
Suggested approach: drive the interpreter from `ScriptModule.bytesHex` and resolve text operands with
`BuckRogersData.Text(moduleId, offset)`; test it against `instructions` (same addresses, same operands).
