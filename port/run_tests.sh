#!/bin/sh
# Differential tests: C# ports vs the real ROM code.  usage: ./run_tests.sh ROM.md [--regen]
#   --regen  re-generate the vectors by running the ROM routines in Unicorn (pip install unicorn)
set -e
ROM="$1"; cd "$(dirname "$0")"
if [ "$2" = "--regen" ]; then
  python3 tools/gen_rng_vectors.py "$ROM" tests/rng_vectors.json
  python3 tools/gen_combat_vectors.py "$ROM" tests/combat_vectors.json
  python3 tools/gen_damage_vectors.py "$ROM" tests/damage_vectors.json
  python3 tools/gen_progress_vectors.py "$ROM" tests/progress_vectors.json
  python3 tools/gen_ai_vectors.py "$ROM" tests/ai_vectors.json
  python3 tools/gen_action_vectors.py "$ROM" tests/action_vectors.json
  python3 tools/gen_monster_vectors.py "$ROM" tests/monster_vectors.json.gz
  python3 tools/gen_arena_vectors.py "$ROM" tests/arena_vectors.txt
  python3 tools/gen_combatant_vectors.py "$ROM" tests/combatant_vectors.txt
  python3 tools/gen_hazard_vectors.py "$ROM" tests/hazard_vectors.txt
  python3 tools/export_monster_file.py "$ROM" csharp/unity/Resources/BuckRogers/monster_file.bytes
  # whole fights only (the other families are in monster_vectors.json.gz): about 30 s
  env $(for n in $(grep -o "N('[a-z0-9_]*'" tools/gen_monster_vectors.py | sed "s/N('//;s/'//" | sort -u); do [ $n != combat ] && echo N_$n=0; done) N_combat=37 python3 tools/gen_monster_vectors.py "$ROM" tests/combat_whole_vectors.json.gz csharp/unity/Resources/BuckRogers/monster_file.bytes
  python3 tools/gen_ecl_vectors.py "$ROM" ecl/data/ecl_vectors.txt.gz
  python3 tools/export_rom_tables.py "$ROM" tests/rom_tables.json
fi
STUB=../genesis_maps/unity/tests/UnityStub.cs
mcs -out:tests/rngtests.exe csharp/GenesisRng.cs $STUB tests/RngTests.cs
mcs -out:tests/combattests.exe csharp/GenesisRng.cs csharp/GenesisCombat.cs csharp/GenesisProgression.cs csharp/GenesisRewards.cs csharp/GenesisAi.cs csharp/GenesisSkills.cs csharp/GenesisTurns.cs csharp/GenesisActions.cs csharp/GenesisMonsterAi.cs csharp/GenesisMonsterWeapons.cs csharp/GenesisMonsterTurn.cs csharp/GenesisPlayerTurn.cs csharp/GenesisInventory.cs csharp/GenesisCombatLoop.cs csharp/GenesisCombatSetup.cs csharp/GenesisCombatEnd.cs csharp/GenesisLootScreen.cs csharp/GenesisShipRepair.cs csharp/GenesisAreaWeapons.cs csharp/GenesisAreaAttack.cs csharp/GenesisAreaChoice.cs csharp/GenesisEffects.cs csharp/GenesisDungeonArena.cs csharp/GenesisCombatants.cs csharp/GenesisParty.cs $STUB tests/CombatTests.cs tests/ProgressTests.cs tests/AiTests.cs tests/ActionTests.cs tests/MonsterTests.cs tests/ArenaTests.cs tests/CombatantTests.cs tests/AdapterTests.cs csharp/unity/AutoBattle.cs csharp/unity/TokenFrames.cs csharp/unity/BattleSequence.cs tests/RomTablesShapes.cs
mono tests/rngtests.exe tests/rng_vectors.json "$ROM"
mono tests/combattests.exe tests/combat_vectors.json "$ROM" tests/damage_vectors.json tests/progress_vectors.json tests/ai_vectors.json tests/action_vectors.json tests/monster_vectors.json.gz tests/arena_vectors.txt tests/combatant_vectors.txt tests/combat_whole_vectors.json.gz csharp/unity/Resources/BuckRogers/monster_file.bytes
# the script (ECL) interpreter: decoder against the 13,936 decoded instructions of data/scripts.json, flow tests, and 1,500 random scripts run by the ROM's own engine
mcs -out:ecl/ecltests.exe csharp/GenesisRng.cs ecl/EclInterpreter.cs $STUB ecl/EclTests.cs
mono ecl/ecltests.exe ecl/data/scripts.json "$ROM" ecl/data/ecl_vectors.txt.gz
# scripts that fight: the interpreter + the combat host (EclCombatHost) on module 0x10, TREASURE / ADDEP, every entry point of every module
CORE=$(grep '^mcs -out:tests/combattests.exe' "$0" | sed 's/^mcs -out:tests\/combattests.exe //; s/\$STUB.*//')
mcs -out:ecl/eclhosttests.exe $CORE csharp/unity/AutoBattle.cs csharp/unity/TokenFrames.cs csharp/unity/EclCombatHost.cs ecl/EclInterpreter.cs $STUB ecl/EclHostTests.cs
mono ecl/eclhosttests.exe ecl/data/scripts.json csharp/unity/Resources/BuckRogers/monster_file.bytes csharp/unity/Resources/BuckRogers/default_party.bytes "$ROM"
# race table, starship crew hazard (0x1956E), disarmed-fighter example (0x6D1E)
mcs -out:tests/hazardtests.exe csharp/GenesisRng.cs csharp/GenesisRaces.cs csharp/GenesisCombat.cs csharp/GenesisProgression.cs csharp/GenesisActions.cs $STUB tests/HazardTests.cs
mono tests/hazardtests.exe tests/hazard_vectors.txt "$ROM" csharp/unity/Resources/BuckRogers/default_party.bytes
