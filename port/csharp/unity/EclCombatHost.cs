// EclCombatHost.cs -- an IEclHost that connects the script interpreter (ecl/EclInterpreter.cs) to the combat port: a script that loads monsters and says COMBAT really runs the fight, with the
// game's default party, and ends it the way the game does (experience, scripted treasure, loot, medical aftermath hooks). The script's own variables (script memory) supply what the ROM's
// combat start reads from RAM: facing [9AFA], ground type [97AD], screen kind [97DC], ambush [9DB6], surprise [9DC1], the dungeon map layers and tile class tables (for the arena builder).
// ROM handlers followed: LOADMONSTER 0x3544, CLEARMONSTERS 0x3766, SETUPMONSTERS 0x3566 (the group mask [D8CC]), COMBAT 0x38BA, TREASURE 0x392C, ADDEP 0x3BCE.
using System;
using System.Collections.Generic;
using BuckRogersGenesis.Ecl;

namespace BuckRogersGenesis
{
    public sealed class EclCombatHost : IEclHost
    {
        public readonly TurnContext X;
        public readonly CombatState S;
        public readonly GenesisEclMemory Mem;
        public readonly List<BattleFrame> Frames = new List<BattleFrame>();   // every creature turn of every fight the script ran (cleared by ClearFrames)
        public readonly List<string> Output = new List<string>();              // PRINT / PRINTCLEAR text (PRINTRETURN adds "\n")
        public readonly List<string> Log = new List<string>();                 // every host call, one line each
        public bool AutoPlay = true;                                          // the computer plays every creature (the host can set Pad / MenuChoice on X and turn this off)
        public Func<bool, string[], int> MenuHandler = (wide, texts) => 0;
        public Func<bool> YesNoHandler = () => true;
        public Func<int, int> NumberHandler = digits => 1;
        public int Fights, LastWinner = -1, NewEclModule = -1, ProgramCalled = -1, StoreOpened = -1;
        public bool EncounterExited;
        public Action BeforeCombat, AfterCombat;                              // hooks for a UI

        public EclCombatHost(RomView rom, MonsterBinFile monsters, DefaultParty party, GenesisRng rng, GenesisEclMemory mem)
        {
            Mem = mem;
            S = new CombatState { SlotCount = 8, Slots = new byte[0][], Records = new byte[11][] };
            X = new TurnContext { S = S, Rom = rom, Monsters = monsters, Rng = rng };
            for (int i = 0; i < 8; i++) X.LoadPartyMember(party.Records[i], party.Slots[i], i);
            for (int r = 8; r < 11; r++) S.Records[r] = new byte[DefaultParty.RecordSize];
            S.SlotCount = 8;
            X.ContinuePrompt = () => 0; X.MapPixelsX = 504; X.MapPixelsY = 504; X.RetreatPrompt = () => 0;
            X.ActorVisible = () => { Frames.Add(AutoBattle.Snap(X)); return true; };
        }

        public void ClearFrames() { Frames.Clear(); }

        // ------------------------------------------------------------------ text and waiting
        public void Print(string t) { Output.Add(t ?? ""); Log.Add("PRINT"); }
        public void PrintClear(string t) { Output.Add(t ?? ""); Log.Add("PRINTCLEAR"); }
        public void PrintReturn() { Output.Add("\n"); Log.Add("PRINTRETURN"); }
        public void Continue() { Log.Add("CONTINUE"); }
        public void Delay() { Log.Add("DELAY"); }
        public void Picture(int n) { Log.Add("PICTURE " + n); }
        public void Sound(int n) { Log.Add("SOUND " + n); }

        // ------------------------------------------------------------------ monsters and fights
        public void LoadMonster(int id, int count, int picture)
        {
            Log.Add($"LOADMONSTER {id} {count}");
            if (Array.IndexOf(X.Monsters.Ids, (byte)id) < 0) { Log.Add("monster " + id + " is not in the monster file"); return; }
            if (X.MonsterTypeCount >= 3) { Log.Add("more than 3 monster types in one fight"); return; }
            X.AddMonsters(id, count);
        }

        public void ClearMonsters() { Log.Add("CLEARMONSTERS"); X.ClearMonsters(); }

        /// SETUPMONSTERS (0x3566): operands [B528] (word), [9DB7], [B525] and the group mask [D8CC]; the first three are stored in script memory like the ROM does, the mask goes to the combat context.
        public void SetupMonsters(int[] v)
        {
            Log.Add("SETUPMONSTERS " + string.Join(",", v));
            Mem.WriteByte(0xB528, v[0] & 0xFF); Mem.WriteByte(0xB529, (v[0] >> 8) & 0xFF); Mem.WriteByte(0x9DB7, v[1]); Mem.WriteByte(0xB525, v[2]);
            X.GroupMask = (byte)v[3];
        }

        /// COMBAT (0x38BA): the fight (setup, rounds) and the clean-up after it. Returns nothing; the frames, the party records and the script variables carry the result.
        public void Combat()
        {
            Log.Add("COMBAT"); Fights++;
            X.SoloFlag = 0;
            X.Facing = (byte)Mem.Ram[0x9AFA]; X.AreaType = Mem.Ram[0x97AD]; X.D97DC = Mem.Ram[0x97DC]; X.Ambush = Mem.Ram[0x9DB6]; X.Surprise = Mem.Ram[0x9DC1];
            X.MapX = Mem.Ram[0x9AF7]; X.MapY = Mem.Ram[0x9AF6];
            Array.Copy(Mem.Ram, 0xB5A4, X.MapLayerA, 0, 256); Array.Copy(Mem.Ram, 0xB6A4, X.MapLayerB, 0, 256); Array.Copy(Mem.Ram, 0xB7A4, X.MapWalls, 0, 256);
            ClassTable(0xB41A, X.TileClassA); ClassTable(0xB41E, X.TileClassB);
            BeforeCombat?.Invoke();
            if (X.CombatSetup())
            {
                if (AutoPlay) for (int i = 0; i < S.SlotCount; i++) S.Slots[i][1] |= 0x80;
                X.ForgetVictims();
                X.CombatRounds();
                X.CountLiving(); LastWinner = S.LivingBySide[1] > 0 ? 1 : 0;
            }
            else LastWinner = -1;
            X.CombatCleanup();
            Mem.WriteByte(0x9DBD, X.Flag9DBD);
            AfterCombat?.Invoke();
        }

        void ClassTable(int pointerAddr, byte[] dest)       // [B41A] / [B41E]: a long pointer to 16 bytes of work RAM
        {
            uint p = (uint)(Mem.Ram[pointerAddr] << 24 | Mem.Ram[pointerAddr + 1] << 16 | Mem.Ram[pointerAddr + 2] << 8 | Mem.Ram[pointerAddr + 3]);
            if ((p & 0xFF0000) != 0xFF0000) return;
            for (int k = 0; k < 16; k++) dest[k] = Mem.Ram[(int)((p + (uint)k) & 0xFFFF)];
        }

        // ------------------------------------------------------------------ rewards and progress
        public void Treasure(int credits, int[] items) { Log.Add($"TREASURE {credits} x{items.Length}"); X.SetScriptedTreasure((uint)credits, items); }

        /// ADDEP who, xp (0x3BCE): who != 0 gives every present, not-down party member the experience; 0 only the current character [9DA7].
        public void AddEp(int who, int xp)
        {
            Log.Add($"ADDEP {who} {xp}");
            for (int i = 0; i < 8; i++)
            {
                var sl = S.Slots[i]; if (sl[0] == 0 || (sl[0] & 0xC0) != 0) continue;
                if (who == 0 && i != Mem.Ram[0x9DA7]) continue;
                var r = S.Records[i]; uint cur = (uint)(r[0x1E] << 24 | r[0x1F] << 16 | r[0x20] << 8 | r[0x21]); cur += (uint)xp;
                r[0x1E] = (byte)(cur >> 24); r[0x1F] = (byte)(cur >> 16); r[0x20] = (byte)(cur >> 8); r[0x21] = (byte)cur;
            }
        }

        public void Store(int n) { StoreOpened = n; Log.Add("STORE " + n); }
        public void Program(int n) { ProgramCalled = n; Log.Add("PROGRAM " + n); }
        public void NewEcl(int module) { NewEclModule = module; Log.Add("NEWECL " + module); }
        public void EncounterExit() { EncounterExited = true; Log.Add("ENCEXIT"); }

        // ------------------------------------------------------------------ answers
        public int Menu(bool wide, string[] texts) { Log.Add("MENU " + texts.Length); return MenuHandler(wide, texts); }
        public int InputNumber(int digits) { Log.Add("INPUTNUMBER " + digits); return NumberHandler(digits); }
        public bool GetYesNo() { Log.Add("GETYN"); return YesNoHandler(); }
        public bool FindItem(int item) { Log.Add("FINDITEM " + item); return false; }
        public void Other(EclInterpreter it, EclInstruction i) { Log.Add(i.Name); }
    }
}
