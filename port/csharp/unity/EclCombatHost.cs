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
        public bool FindItem(int item) { Log.Add("FINDITEM " + item); return X.ScriptFindItem(item); }
        public void Other(EclInterpreter it, EclInstruction i)
        {
            Log.Add(i.Name);
            var o = i.Ops;
            switch (i.Opcode)
            {
                case 0x0A: Mem.WriteByte(GenesisScriptChar.CurrentCharacter, it.Value(o[0]) & 0xFF); break;        // LOADCHARACTER (ROM 0x353A)
                case 0x44: break;                                                                                   // SAVECHARACTER: a bare RTS (ROM 0x3CC4)
                case 0x36: X.AddAlly(it.Value(o[0]), it.Value(o[1])); break;                                        // ADDNPC (ROM 0x3AD4 -> 0x488C)
                case 0x5C:                                                                                          // NEWREGION id, n, 4n rectangle operands (ROM 0x3FB4), then the map load (0x574E)
                    {
                        var vals = new int[o.Length - 2]; for (int k = 2; k < o.Length; k++) vals[k - 2] = it.Value(o[k]);
                        GenesisExplore.NewRegion(Mem, it.Value(o[0]), it.Value(o[1]), vals);
                        if (Maps != null) Maps.Reload(Mem);                                                            // 0x574E: decode map [0x9BD4] again and cut it to the rectangles
                        if (MapLoader != null) MapLoader(Mem.ReadByte(0x9BD4));
                        break;
                    }
                case 0x21: { int mapId = it.Value(o[0]); if (mapId < 0x7F && Maps != null) Maps.Load(Mem, mapId); break; }   // LOADFILES map, ., . (ROM 0x3886 -> 0x5734): the map's four layers
                case 0x50: GenesisExplore.StepForward(Mem); break;                                                   // STEPFORWARD (ROM 0x53B6)
                case 0x5A: GenesisExplore.StepBack(Mem); break;                                                      // STEPBACK (ROM 0x3EA4)
                case 0x4F: GenesisExplore.HalfStep(Mem); break;                                                      // HALFSTEP
                case 0x5B: GenesisExplore.HalfBack(Mem); break;                                                      // HALFBACK
                case 0x52: GenesisExplore.UnlockDoor(Mem); break;                                                    // UNLOCKDOOR
                case 0x45: it.Store(o[0], GenesisExplore.HowFar(Mem, it.Value(o[1]))); break;                        // HOWFAR dest, direction
                case 0x40: X.ScriptDestroyItem(it.Value(o[1])); break;                                               // DESTROY x, item (ROM 0x3A60): removes one; the first operand is ignored
                case 0x48: X.ScriptHideItems(it.Value(o[0])); break;                                                 // HIDEITEMS n (ROM 0x3D0C)
                case 0x39: Mem.WriteByte(GenesisScriptChar.CurrentCharacter, WhoHandler() & 0xFF); break;           // WHO (ROM 0x53A6): the member menu picks the current character
                case 0x35: break;                                                                                   // SAVETABLE: a debug command; the ROM only prints "command not supported"
                case 0x2E:                                                                                          // DAMAGE flags, count, sides, bonus, target (ROM 0x500A)
                    {
                        bool down; X.ScriptDamage(it.Value(o[0]), it.Value(o[1]), it.Value(o[2]), it.Value(o[3]), it.Value(o[4]), Mem.ReadByte(GenesisScriptChar.CurrentCharacter), out down);
                        if (down) { PartyDown = true; Mem.WriteByte(0xBA53, 1); Log.Add("GAMEOVER"); }                    // the ROM sets [0xBA53] and restarts through 0x7588
                        break;
                    }
                case 0x49:                                                                                          // SKILLDAMAGE skill, who, shift, count, sides, bonus (ROM 0x5B44)
                    X.ScriptSkillDamage(it.Value(o[0]), it.Value(o[1]), it.Value(o[2]), it.Value(o[3]), it.Value(o[4]), it.Value(o[5]), Mem.ReadByte(GenesisScriptChar.CurrentCharacter));
                    break;
                case 0x22: case 0x23:                                                                               // SKILL / PRINTSKILL skill, who, shift (ROM 0x38A2 / 0x38A8)
                    {
                        if (i.Opcode == 0x23) Mem.WriteByte(GenesisScriptChar.PrintSkillFlag, 3);
                        var recs = new byte[8][]; var slots = new byte[8][];
                        for (int k = 0; k < 8; k++) { recs[k] = S.Records[k]; slots[k] = S.Slots[k]; }
                        int index;
                        int result = GenesisScriptChar.Skill(X.Rom, X.Rng, recs, slots, Mem.ReadByte(GenesisScriptChar.CurrentCharacter), it.Value(o[0]), it.Value(o[1]), it.Value(o[2]), out index);
                        it.Store(o[2], result); it.Store(o[1], index);                                                // operands 1 and 2 are also the destinations (stored in this order)
                        LastSkillResult = result; LastSkillIndex = index;
                        break;
                    }
            }
        }
        public int LastSkillResult = -1, LastSkillIndex = -1;
        public Func<int> WhoHandler = () => 0;                                                                 // WHO: the party member the player picks (0-7); a UI plugs in here
        public Action<int> MapLoader;                                                                          // NEWREGION: load the map of this region id into the four layers at 0xB5A4
        public GenesisMaps Maps;                                                                               // the game's maps (ecl/data/map_layers.txt); null = the host loads maps itself (MapLoader)
        public bool PartyDown;                                                                                 // DAMAGE left nobody standing (the game restarts)
    }
}
