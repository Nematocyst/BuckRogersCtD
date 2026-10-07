# Project state: Buck Rogers - Countdown to Doomsday (Genesis) -> Unity combat port

Last updated at the end of the session that built the adapter, graphics export and party-sprite trace. Branch: `claude/eloquent-newton-1nwtph` of `nematocyst/buckrogersctd` (everything is committed and pushed; no PR was opened, none was asked for).

## 0. Start here
* **Goal:** port the Genesis game's ground-combat engine to C# for Unity, with every routine verified against the real ROM (1 MB, US/Europe) by differential testing in a 68000 emulator (Unicorn).
* **The ROM is not in the repo.** In the cloud session it was an upload named `Buck_Rogers_-_Countdown_to_Doomsday_USA_Europe.md` (1,048,576 bytes; the `.md` is just the file name). Every tool takes the ROM path as its first argument.
* **Run the tests:** `cd port && ./run_tests.sh /path/to/rom` (needs `mono`/`mcs`). Add `--regen` to regenerate all vectors from the ROM (needs `pip install unicorn pillow capstone`; slow, minutes). Costs matter: do not run `--regen` casually. The combat-vector generator alone is the expensive part.
* **Read first:** `port/README.md` (API and per-feature notes), `port/docs/README.md` (method, per-topic pages, quirks, gaps). `docs/07-graphics-scoping.md` holds all graphics findings.
* The earlier ROM-hacking notes (`PROJECT_STATE_.md`, uploaded by the user, not in the repo) describe record layouts, LZW format, scripts and the patch work; this port builds on them.

## 1. What exists (all under `port/`)
| Area | Files | Status |
|---|---|---|
| RNG, hit/damage, progression, skills, turn order, stats, rewards | `csharp/GenesisRng/Combat/Progression/Rewards/Skills/Turns/Actions.cs` | verified |
| Computer-controlled turn (targets, weapons, movement, attacks, reactions, explosives, healer rescue, special effects) | `GenesisMonsterAi/MonsterWeapons/MonsterTurn/Effects/Area*.cs`, `GenesisAi.cs` (pathfinding) | verified |
| Player turn, character sheet, inventory, retreat prompt | `GenesisPlayerTurn.cs`, `GenesisInventory.cs` | verified (callbacks for UI) |
| Fight start, round loop | `GenesisCombatSetup.cs`, `GenesisCombatLoop.cs` | verified |
| Fight end: tally, XP, loot sharing, medical aftermath, scripted-fight end (starship repair) | `GenesisCombatEnd.cs`, `GenesisLootScreen.cs`, `GenesisShipRepair.cs` | verified |
| Dungeon-map arena (ROM 0xB100) | `GenesisDungeonArena.cs` | verified (400 random maps) |
| Combatant creation (ROM 0x3544 add monsters, 0x488C add NPC, 0x48E8 loader) | `GenesisCombatants.cs` | verified (600 cases) |
| Default party | `csharp/GenesisParty.cs`, `csharp/unity/Resources/BuckRogers/default_party.bytes` (from ROM 0x6BAAD, `tools/export_default_party.py`) | records, slots and tokens agree with the engine (RecomputeSlot reproduces the stored slot stats) |
| Unity adapter | `csharp/unity/AutoBattle.cs`, `BattleSequence.cs`, `TokenFrames.cs`, `CombatBoardView.cs`, `GenesisRomTablesLoader.cs` | logic tested under mono; the `MonoBehaviour` view is **not compiled or run in Unity** |
| Script (ECL) interpreter | `ecl/EclInterpreter.cs`, `ecl/EclTests.cs`, `ecl/data/` (oracle + vectors), `ecl/README.md` | verified: decoder vs 13,936 oracle instructions, 1,500 random scripts vs the ROM's own engine |
| Tools | `tools/*.py` (vector generators, `emu68k.py`, `vdp_model.py`, exporters) | working |

Test status at the end of this session (all passing): rng, 20,189 combat checks, 4,533 attack, 17,089 progression, 62,911 victory tally, 16,500 skills/turn order, 1,201 pathfinding, 15,922 actions, 148,623 monster-turn checks, 400 arena cases, 600 combatant cases, 34 whole fights (601 checks), adapter (12 fights, 184 frames, 127 attack sequences, indoor + dungeon + party-key checks). Mutation checks were run on every new family (a few deliberate breakages each, all caught except the equivalent ones noted in the docs).

## 2. How verification works (details in `docs/01-method.md`)
Run the ROM routine in Unicorn on random worlds (`tools/monster_world.py`, `sane_world` / `fight_world` ...), store pre/post RAM, RNG state and event traces as gzip JSON; the C# test rebuilds the world, runs the port and compares everything. Graphics / UI routines are stubbed (`UI_STUBS`); UI callbacks (`Pad`, `MenuChoice`, `InventoryMenu`, `AskQuantity`, `CharacterSheet`, `RetreatPrompt`, `ContinuePrompt`, `PromptHook`, `ActorVisible`, `GameOver`, `Aftermath`, `LootScreen`, `ScriptedFightEnd`) are the host's. Quirks of the original are reproduced on purpose (`docs/05-quirks.md`).

## 3. Graphics (all found by running the ROM's own code; details in `docs/07-graphics-scoping.md`)
* Pictures are LZW "pieces" (decoder 0x9ED8, init 0x9E76): header (tile count, tilemap bytes, palette mask), tilemap, palettes, 4bpp 8x8 tiles. Art is **not committed** (game copyright); `.gitignore` lists `csharp/unity/Resources/BuckRogers/{pictures,tokens,terrain}`. Regenerate with `tools/export_pictures.py`, `export_tokens.py`, `export_terrain.py`.
* Encounter pictures: table 0x51360 (57, 11x11 tiles). Icons: table 0xF14F2 (185).
* Creature tokens: table 0x9A14 (52 entries by monster id), 18 frames each, 3x3 tiles (tall 3x6 / wide 6x3 for size types 2 / 3), palette = system palette at ROM 0x9710. Standing frame by facing and death state: ROM 0xAD5A (`TokenFrames.Idle`). Attack animation order: aim 3/4/5, hit 17, death 15 -> 16 -> (11) -> vanish by animation set (`TokenFrames`, `BattleSequence`).
* Party tokens: table 0x998C (12 sheets), chosen by record byte +0x42 (0x80 | sheet). Default party keys live in the compressed blob at ROM 0x6BAAD (8 records; offset `record*214 + 0x42`). No formula from race/sex/career; created characters keep key 0 (derived from the code, not seen in the game).
* Terrain: ROM painter 0xFEF8, 3x3-tile block per tile id, atlases for screen modes 4 (outdoor), 5 (indoor), 6 (dungeon / ship interior).

## 4. Known gaps / open items
* **Unity:** `CombatBoardView` never ran in Unity (compiled only against a hand-written stub of the used UnityEngine calls). No player input UI (everything is computer-played), no sound, no projectile flight, no text/menus, no sprites for the ROM UI.
* **Script engine:** the interpreter is in `ecl/` (see its README); the host side (menus, store, shops, combat start glue, screens) is not written, and `AddMonsters` / `AddAlly` are not yet called from `IEclHost.LoadMonster` / `SetupMonsters`. Still not ported: the LZW decompressor (replaced by the exported `monster_file.bytes`), the "monster not found" error box, starship combat (0x17816 engine, separate subsystem), overland/dungeon exploration, character creation screen, save/load.
* **Combat details not traced:** projectile flight (0x108AA), what party sheets 2, 4, 6, 8, 9, 11 are for, the tick = 1/60 s assumption of the animation timings, sound.
* **Demo limits:** party members in `AutoBattle` are monster-file records with party keys (the viewer can now use the six default characters from `default_party.bytes` instead: `DefaultParty`, `AutoBattle.RunDefaultParty`); dungeon arenas use a generated map (no real map layers in the builder's format); outdoor types 0-10, indoor 0-12.
* **Never run in a real emulator session by me:** claims marked "derived from the code" in the docs (created characters drawing as monster id 0; the animation tick length).

## 5. Conventions and constraints (keep following)
* Work only on branch `claude/eloquent-newton-1nwtph`; commit and push there; **never open a PR unless asked**.
* Every commit message ends with the two attribution lines (`Co-Authored-By: ...` and `Claude-Session: ...`) as given by the session; no model identifiers inside repo files.
* The user has limited credit: stop at clean states, avoid long generator runs, report plainly (including failures and unverified claims).
* A stop hook requires all changes to be committed and pushed.
* Environment notes: scratch files go in the session scratchpad; scripts that read ROM data should run with `python3 -I` when they read untrusted files; the Unicorn harness needs the "priming run" of `0xF28E` (see `monster_world.machine`); hooks added after code has run need `uc.ctl_flush_tb()`; the decompressor keeps its dictionary at RAM 0xFFFF0000, so output buffers must go elsewhere (the exporters use 0xFFFF9000).

## 6. Suggested next steps (in order of value / cost)
1. Open the viewer in Unity: copy `rom_tables.json`, `monster_file.bytes`, the three exported art folders and the `csharp/` files; fix whatever the editor reports.
2. (done) The six default characters are decoded (`csharp/GenesisParty.cs`, `Resources/BuckRogers/default_party.bytes`) and the viewer fights with them.
3. Real player input UI (map picking, command menu) on the existing callbacks.
4. Projectile flight and sounds; the unused party sheets; the script engine / encounter data path (`AddMonsters` already takes parsed operands).
5. Starship combat as a separate port (large).
