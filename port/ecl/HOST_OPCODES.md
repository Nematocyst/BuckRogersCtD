# Script opcodes: handler addresses and host-side status

ROM dispatch: loop at 0x3300, jump table at 0x336E (word offsets from 0x336E, one per opcode). Operand reader 0x404A (returns the value, `a0` = the operand's address for destinations).
Classes: **engine** (in `EclInterpreter`), **UI event** (a host callback; the host decides how to show it), **combat host** (`EclCombatHost`, ROM-checked), **A** character opcodes
(`GenesisScriptChar`, ROM-checked, vectors in `tests/scriptchar_vectors.txt`), **A2** next group, **B** exploration, **C** starship / duel, **D** graphics only.

| op | name | operands (-1 variable, -2 unknown) | uses in scripts | ROM handler | class |
|---|---|---|---|---|---|
| 00 | EXIT | 0 | 897 | 0342A | engine |
| 01 | GOTO | 1 | 1303 | 03438 | engine |
| 02 | GOSUB | 1 | 243 | 03440 | engine |
| 03 | COMPARE | 2 | 950 | 03452 | engine |
| 04 | ADD | 3 | 138 | 034EE | engine |
| 05 | SUBTRACT | 3 | 35 | 034F6 | engine |
| 06 | DIVIDE | 3 | 9 | 03500 | engine |
| 07 | MULTIPLY | 3 | 11 | 03516 | engine |
| 08 | RANDOM | 2 | 110 | 0351E | engine |
| 09 | SAVE | 2 | 1136 | 03530 | engine |
| 0A | LOADCHARACTER | 1 | 57 | 0353A | A: character (done) |
| 0B | LOADMONSTER | 3 | 240 | 03544 | combat host (done) |
| 0C | SETUPMONSTERS | 4 | 162 | 03566 | combat host (done) |
| 0D | APPROACH | 0 | 5 | 03652 | B: exploration (map) |
| 0E | PICTURE | 1 | 185 | 03662 | UI event (host) |
| 0F | INPUTNUMBER | 2 | 25 | 03670 | UI event (host) |
| 10 | INPUTSTRING | 2 | 0 | 0368C | unused |
| 11 | PRINT | 1 | 263 | 03690 | UI event (host) |
| 12 | PRINTCLEAR | 1 | 1635 | 036E6 | UI event (host) |
| 13 | RETURN | 0 | 207 | 036EE | engine |
| 14 | COMPAREAND | 4 | 109 | 034B4 | engine |
| 15 | MENU | -2 | 0 | 03708 | unused |
| 16 | IFEQ | 0 | 774 | 0370C | engine |
| 17 | IFNE | 0 | 730 | 03716 | engine |
| 18 | IFLT | 0 | 104 | 03720 | engine |
| 19 | IFGT | 0 | 106 | 0372A | engine |
| 1A | IFLE | 0 | 13 | 03734 | engine |
| 1B | IFGE | 0 | 21 | 0373E | engine |
| 1C | CLEARMONSTERS | 0 | 165 | 03766 | combat host (done) |
| 1D | SETTIMER | -2 | 0 | 0377A | unused |
| 1E | CHECKPARTY | -2 | 0 | 0377E | unused |
| 1F | SPACECOMBAT | 4 | 3 | 03782 | C: starship / duel |
| 20 | NEWECL | 1 | 70 | 0383E | engine |
| 21 | LOADFILES | 3 | 28 | 03886 | D: graphics (no game state) |
| 22 | SKILL | 3 | 44 | 038A2 | A: character (done) |
| 23 | PRINTSKILL | 3 | 1 | 038A8 | A: character (done) |
| 24 | COMBAT | 0 | 174 | 038BA | combat host (done) |
| 25 | ONGOTO | -1 | 182 | 038EA | engine |
| 26 | ONGOSUB | -1 | 11 | 038E2 | engine |
| 27 | TREASURE | -1 | 28 | 0392C | combat host (done) |
| 28 | ROB | -2 | 0 | 0399A | unused |
| 29 | CONTINUE | 0 | 1091 | 0399E | UI event (host) |
| 2A | GETABLE | 3 | 64 | 039B4 | engine |
| 2B | HMENU | -1 | 125 | 039CC | UI event (host) |
| 2C | GETYN | 0 | 111 | 039D0 | UI event (host) |
| 2D | DRAWINDOW | 0 | 4 | 03A0A | D: graphics (no game state) |
| 2E | DAMAGE | 5 | 42 | 03A0C | A2: character (done) |
| 2F | AND | 3 | 409 | 03A10 | engine |
| 30 | OR | 3 | 298 | 03A22 | engine |
| 31 | WHMENU | -1 | 16 | 03A34 | UI event (host) |
| 32 | FINDITEM | 1 | 5 | 03A8E | A2: character / item state (next) |
| 33 | PRINTRETURN | 0 | 79 | 03AAA | UI event (host) |
| 34 | CLOCK | -2 | 0 | 03ACC | unused |
| 35 | SAVETABLE | 3 | 1 | 03AD0 | A2: character / item state (next) |
| 36 | ADDNPC | 2 | 12 | 03AD4 | A: character (done) |
| 37 | LOADPIECES | 1 | 30 | 03AD8 | D: graphics (no game state) |
| 38 | PROGRAM | 1 | 9 | 03AEC | A2: character / item state (next) |
| 39 | WHO | 1 | 26 | 03B6A | A2: character / item state (next) |
| 3A | DELAY | 0 | 156 | 03B74 | UI event (host) |
| 3B | SPELLS | -2 | 0 | 03B78 | unused |
| 3C | PROTECT | -2 | 0 | 03B7C | unused |
| 3D | CLEARBOX | 0 | 2 | 03B80 | D: graphics (no game state) |
| 3E | DUMP | 0 | 11 | 03B86 | B: exploration (map) |
| 3F | JOURNAL | -2 | 0 | 03B90 | unused |
| 40 | DESTROY | 2 | 4 | 03A60 | A2: character / item state (next) |
| 41 | ADDEP | 2 | 51 | 03BCE | combat host (done) |
| 42 | ENCEXIT | 0 | 434 | 03C9E | engine |
| 43 | SOUND | 1 | 271 | 03CBA | UI event (host) |
| 44 | SAVECHARACTER | 0 | 13 | 03CC4 | A: character (done) |
| 45 | HOWFAR | 2 | 1 | 03CC6 | A2: character / item state (next) |
| 46 | FOR | 2 | 12 | 03CE2 | engine |
| 47 | ENDFOR | 0 | 12 | 03CF8 | engine |
| 48 | HIDEITEMS | 1 | 1 | 03D0C | A2: character / item state (next) |
| 49 | SKILLDAMAGE | 7 | 1 | 03D38 | A2: character (done) |
| 4A | DUEL | 0 | 2 | 038B4 | C: starship / duel |
| 4B | STORE | 1 | 11 | 03D3C | A2: character / item state (next) |
| 4C | VIEW | 2 | 46 | 03DD8 | B: exploration (map) |
| 4D | ANIMATE | -2 | 0 | 03E28 | unused |
| 4E | STAIRCASE | 0 | 12 | 03E2E | B: exploration (map) |
| 4F | HALFSTEP | 0 | 4 | 03FA0 | B: exploration (map) |
| 50 | STEPFORWARD | 0 | 53 | 03EB2 | B: exploration (map) |
| 51 | PALETTE | 1 | 2 | 03EB8 | D: graphics (no game state) |
| 52 | UNLOCKDOOR | 0 | 3 | 03EC4 | B: exploration (map) |
| 53 | ADDFIGURE | 4 | 79 | 03ED0 | D: graphics (no game state) |
| 54 | ADDCORPSE | 3 | 26 | 03F00 | D: graphics (no game state) |
| 55 | ADDFIGURE2 | 4 | 7 | 03F1A | D: graphics (no game state) |
| 56 | ADDCORPSE2 | 3 | 4 | 03F54 | D: graphics (no game state) |
| 57 | UPDATEFRAME | 1 | 10 | 03F70 | D: graphics (no game state) |
| 58 | REMOVEFIGURE | 0 | 76 | 03F7A | D: graphics (no game state) |
| 59 | EXPLOSION | 1 | 43 | 03F80 | D: graphics (no game state) |
| 5A | STEPBACK | 0 | 35 | 03EA4 | B: exploration (map) |
| 5B | HALFBACK | 0 | 1 | 03F88 | B: exploration (map) |
| 5C | NEWREGION | 6 | 57 | 03FB4 | B: exploration (map) |
| 5D | ICONMENU | -2 | 0 | 03FF4 | unused |

## Order of work
1. **A (done):** LOADCHARACTER, SAVECHARACTER, SKILL, PRINTSKILL, ADDNPC. SKILL's party search reproduces a ROM quirk: the skill-value routine 0x4FB0 overwrites the slot pointer, so after the first eligible member the following members' "slot flags" are ROM bytes at 0x4FFC + 0x1A k.
2. **A2 (in progress):** DAMAGE and SKILLDAMAGE are done (`GenesisScriptDamage.cs`, 1,500 and 1,200 ROM vectors; wired into `EclCombatHost`, which reports `PartyDown`). Notes: DAMAGE's "one random victim with a save" branch never tests the save (a missing TST in the ROM; the Z flag comes from the restored D2) -- `ScriptLeftoverD2`, unused by any script; the legacy shot mode (flags bit 7 clear) makes `flags` shots, not flags + 1 (the loop is entered at its DBRA). SKILLDAMAGE has **six** operands in the ROM (skip table 0x482E and the handler), not seven as in `scripts.json`: the seventh is the EXIT behind it in module 0x53, and the decoder test adjusts the oracle for that one instruction (13,937 instructions). Remaining: WHO (0x53A6, opens a member menu through 0x5308), SAVETABLE (0x4022), HOWFAR (0x3CC6), DESTROY (0x3A60, removes an item through 0x76C4), HIDEITEMS, FINDITEM (0x3A8E), STORE (0x3D3C), PROGRAM (0x3AEC).
3. **B:** NEWREGION (0x3FB4) and the step opcodes (0x3EA4-0x3FA0): the square/facing/area state that scripts and the map share ([9AF6], [9AF7], [9AFA], [9E08]); then the module driver (NEWECL -> load the next module).
4. **C / D:** SPACECOMBAT is the separate starship port; graphics opcodes change no game state and stay host callbacks.
