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
| 32 | FINDITEM | 1 | 5 | 03A8E | A2: character (done) |
| 33 | PRINTRETURN | 0 | 79 | 03AAA | UI event (host) |
| 34 | CLOCK | -2 | 0 | 03ACC | unused |
| 35 | SAVETABLE | 3 | 1 | 03AD0 | A2: character (done) |
| 36 | ADDNPC | 2 | 12 | 03AD4 | A: character (done) |
| 37 | LOADPIECES | 1 | 30 | 03AD8 | D: graphics (no game state) |
| 38 | PROGRAM | 1 | 9 | 03AEC | E: programs (screens) |
| 39 | WHO | 1 | 26 | 03B6A | A2: character (done) |
| 3A | DELAY | 0 | 156 | 03B74 | UI event (host) |
| 3B | SPELLS | -2 | 0 | 03B78 | unused |
| 3C | PROTECT | -2 | 0 | 03B7C | unused |
| 3D | CLEARBOX | 0 | 2 | 03B80 | D: graphics (no game state) |
| 3E | DUMP | 0 | 11 | 03B86 | B: exploration (map) |
| 3F | JOURNAL | -2 | 0 | 03B90 | unused |
| 40 | DESTROY | 2 | 4 | 03A60 | A2: character (done) |
| 41 | ADDEP | 2 | 51 | 03BCE | combat host (done) |
| 42 | ENCEXIT | 0 | 434 | 03C9E | engine |
| 43 | SOUND | 1 | 271 | 03CBA | UI event (host) |
| 44 | SAVECHARACTER | 0 | 13 | 03CC4 | A: character (done) |
| 45 | HOWFAR | 2 | 1 | 03CC6 | B: exploration (done) |
| 46 | FOR | 2 | 12 | 03CE2 | engine |
| 47 | ENDFOR | 0 | 12 | 03CF8 | engine |
| 48 | HIDEITEMS | 1 | 1 | 03D0C | A2: character (done) |
| 49 | SKILLDAMAGE | 6 (the oracle says 7) | 1 | 03D38 | A2: character (done) |
| 4A | DUEL | 0 | 2 | 038B4 | C: starship / duel |
| 4B | STORE | 1 | 11 | 03D3C | E: shops (screen mode of the combat engine) |
| 4C | VIEW | 2 | 46 | 03DD8 | B: exploration (map) |
| 4D | ANIMATE | -2 | 0 | 03E28 | unused |
| 4E | STAIRCASE | 0 | 12 | 03E2E | B: exploration (map) |
| 4F | HALFSTEP | 0 | 4 | 03FA0 | B: exploration (done) |
| 50 | STEPFORWARD | 0 | 53 | 03EB2 | B: exploration (done) |
| 51 | PALETTE | 1 | 2 | 03EB8 | D: graphics (no game state) |
| 52 | UNLOCKDOOR | 0 | 3 | 03EC4 | B: exploration (done) |
| 53 | ADDFIGURE | 4 | 79 | 03ED0 | D: graphics (no game state) |
| 54 | ADDCORPSE | 3 | 26 | 03F00 | D: graphics (no game state) |
| 55 | ADDFIGURE2 | 4 | 7 | 03F1A | D: graphics (no game state) |
| 56 | ADDCORPSE2 | 3 | 4 | 03F54 | D: graphics (no game state) |
| 57 | UPDATEFRAME | 1 | 10 | 03F70 | D: graphics (no game state) |
| 58 | REMOVEFIGURE | 0 | 76 | 03F7A | D: graphics (no game state) |
| 59 | EXPLOSION | 1 | 43 | 03F80 | D: graphics (no game state) |
| 5A | STEPBACK | 0 | 35 | 03EA4 | B: exploration (done) |
| 5B | HALFBACK | 0 | 1 | 03F88 | B: exploration (done) |
| 5C | NEWREGION | 6 | 57 | 03FB4 | B: exploration (done) |
| 5D | ICONMENU | -2 | 0 | 03FF4 | unused |

## Order of work
1. **A (done):** LOADCHARACTER, SAVECHARACTER, SKILL, PRINTSKILL, ADDNPC. SKILL's party search reproduces a ROM quirk: the skill-value routine 0x4FB0 overwrites the slot pointer, so after the first eligible member the following members' "slot flags" are ROM bytes at 0x4FFC + 0x1A k.
2. **A2 (in progress):** DAMAGE and SKILLDAMAGE are done (`GenesisScriptDamage.cs`, 1,500 and 1,200 ROM vectors; wired into `EclCombatHost`, which reports `PartyDown`). Notes: DAMAGE's "one random victim with a save" branch never tests the save (a missing TST in the ROM; the Z flag comes from the restored D2) -- `ScriptLeftoverD2`, unused by any script; the legacy shot mode (flags bit 7 clear) makes `flags` shots, not flags + 1 (the loop is entered at its DBRA). SKILLDAMAGE has **six** operands in the ROM (skip table 0x482E and the handler), not seven as in `scripts.json`: the seventh is the EXIT behind it in module 0x53, and the decoder test adjusts the oracle for that one instruction (13,937 instructions). FINDITEM, DESTROY and HIDEITEMS are done (`GenesisScriptItems.cs`, 600 ROM vectors incl. the [0x97AE] rules: the search finds nothing while it is set; id 0 matches an empty entry). WHO is a host callback (`WhoHandler`: the ROM opens a member menu and stores the pick in [0x9DA7]); SAVETABLE is a debug command that only prints "command not supported". Remaining: HOWFAR (needs the map: 0x4DAA / 0x14CCE, moves to group B).
3. **E: STORE and PROGRAM (read, not ported -- they open screens).**
   * STORE n (0x3D3C) is not a plain shop call: it clears the monsters (0x3766), takes list n from the table at 0x3D76 (one length byte, then that many bytes, copied to [0xB9F3] / [0xB9F4..]),
     sets [0xBA60] and runs the combat entry 0x38BA, i.e. the shop is a screen mode of the combat/loot engine; the purchases change gold and gear there. A faithful port needs that screen (0x38BA with [0xBA60]) first.
   * PROGRAM n (0x3AEC) jumps through the word table at 0x3B5E: 0 = the training screen (stores the script pointer in [0x9BCC], copies [0xB9F0] to [0x97E8], [0x9BBC] to [0x9BBD], then 0x569E);
     1 = `jmp 0xD77C`; 2 = [0xBA53] = [0xBA59] = 0xFF (end of the game; [0xBA59] also stops the script loop at 0x3300); 3 = 0x5C3C; 4 = 0x60E0; 5 = clear the save area ($FE000 or $200001 in steps) and call 0x225C three times.
     The host keeps recording the number (`ProgramCalled`).
4. **B (mostly done):** `GenesisExplore.cs` (1,500 ROM vectors): STEPFORWARD, STEPBACK, HALFSTEP, HALFBACK, UNLOCKDOOR, HOWFAR and the state part of NEWREGION (the host loads the map: `MapLoader`). Findings: a step against a wall (side state 1) or off the map turns the party round (+2 facing) instead of moving; a door (3) is passed; NEWREGION reads `n` rectangles of four operands -- the oracle and the ROM's skip table count 6 (n = 1) but 25 of its 57 uses have n = 2..8, whose operands the oracle showed as stray EXIT / data (the decoder now reads 2 + 4n operands; the test skips the 27 stray oracle decodes). Still open: STAIRCASE, VIEW, APPROACH, DUMP (drawing), the module driver is done (below). Original plan: NEWREGION (0x3FB4) and the step opcodes (0x3EA4-0x3FA0): the square/facing/area state that scripts and the map share ([9AF6], [9AF7], [9AFA], [9E08]); then the module driver (NEWECL -> load the next module).
5. **C / D:** SPACECOMBAT is the separate starship port; graphics opcodes change no game state and stay host callbacks.

## The module driver (`EclSession.cs`)
Read from the ROM main loop (0x4140-0x41FA), NEWECL (0x383E), the entry table (0x4284) and the re-init pass (0x424E): **Boot** picks the first module ([0x97E8] if set, else 3 when [0xBA5A], else 0 when [0xCA21] is 0, else 0x10)
and reads its five entry points into the table at 0xB59A; **NEWECL** keeps the module being left in [0x97E8], loads the new one, **clears the module variables 0x97F6..0x9815 and the scratch registers 0x9E6F..0x9E78**,
sets [0xBA59] (stop the running script) and [0xB9F1] (module changed); the **init pass** runs the new module's init entry (repeated while it switches again) and then copies the module id to [0x97E8];
a **tick** runs the "run" entry, the forced step 0x4D74 and the "search" entry. The player's own input (0x4A58), the camp and search commands and every screen are the host's. Tested with synthetic modules
(`EclHostTests` section 7) and as a smoke run of the real game from its start (section 8: boots into module 0x10, 60 ticks without an exception).

## Map loading (`GenesisMaps.cs`, 400 ROM vectors)
LOADFILES (map id below 0x7F; ROM 0x3886 -> 0x5734) and the end of NEWREGION (0x574E) decode the map into the four layers at 0xB5A4 and run the post-processing 0x57DA **twice**: the NEWREGION rectangles ([0x9BD5] of them at 0x9BD6)
decide which wall nibbles survive (cells inside keep everything, border cells keep the walls facing the rectangle, outside cells lose their walls and attribute), [0x9BC5] paints a wall type on the border, and each cell's door byte
is rebuilt through a 16-entry class table that depends on the area ([0x9BBE] / 3 -> table at ROM 0x51836; `SetArea`). The 18 maps (`ecl/data/map_layers.txt`) are the ROM's own LZW decode, exported by `tools/export_map_layers.py`.
The decoded `genesis_maps.json` of the other branch matches the ROM decode byte for byte (checked for all 18). Both entry points are wired in `EclCombatHost` (`Maps`); the area (class table) is chosen by the host: ROM 0x158BA does it when an area is entered.
