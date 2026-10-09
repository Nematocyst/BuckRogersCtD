// EclSession.cs -- the script driver: module switching and the main loop around the interpreter, from the ROM (main loop 0x4140-0x41FA, NEWECL handler 0x383E, entry table 0x4284, re-init 0x424E).
// What it does (the player's own input -- 0x4A58 -- and every screen are the host's):
//   Boot       0x4140: the module to start is [0x97E8] if set (return from a side trip), else 3 when [0xBA5A], else 0 when [0xCA21] is 0, else 0x10; it is loaded and its five entry points
//              (run, search, precamp, campint, init) are read from the GOTOs at its start into the table at 0xB59A.
//   NEWECL     0x383E: [0x97E8] = [0xB9F0] (the module we leave), [0xB9F0] = the new module, the new module is loaded, the module-local variables 0x97F6..0x9815 (32 bytes) and the scratch registers
//              0x9E6F..0x9E78 (10 bytes) are cleared, and the flags [0xBA59] (stop the running script) and [0xB9F1] (module changed) are set.
//   InitPass   0x424E: while the module changed: clear [0xB9F1], [0x9DCB]; refresh [0x9AF9] / [0x9AF8] for the party's square; then run the init entry (or resume at a saved address,
//              ResumeAddress = the pointer PROGRAM 0 keeps in [0x9BCC]); afterwards [0x97E8] = [0xB9F0].
//   Tick       0x4198-0x41FA: run entry 0 ("run"); then, if [0x9DBF] is 0 and [0xBA5D] is set, a forced step (0x4D74); clear [0x9DBF]; run entry 1 ("search"). After each script a module change
//              starts an InitPass; [0xBA53] (game over) ends the session.
using System;
using System.Collections.Generic;

namespace BuckRogersGenesis.Ecl
{
    public sealed class EclSession
    {
        public const int ModuleNow = 0xB9F0, ModulePrev = 0x97E8, Changed = 0xB9F1, StopScript = 0xBA59, GameOver = 0xBA53, EntryTable = 0xB59A, LocalVars = 0x97F6, Scratch = 0x9E6F;

        readonly Dictionary<int, EclModule> modules;
        public readonly GenesisEclMemory Mem;
        readonly IEclHost host; readonly Func<int, int> random;
        readonly Func<int> newEclRequest; readonly Action clearNewEcl;
        public EclModule Current;
        public int ResumeAddress;                         // [0x9BCC]: where a script that called PROGRAM 0 continues
        public readonly List<string> Log = new List<string>();
        public int ScriptRuns;

        public EclSession(IEnumerable<EclModule> all, GenesisEclMemory mem, IEclHost host, Func<int, int> random, Func<int> newEclRequest, Action clearNewEcl)
        {
            modules = new Dictionary<int, EclModule>(); foreach (var m in all) modules[m.Id] = m;
            Mem = mem; this.host = host; this.random = random; this.newEclRequest = newEclRequest; this.clearNewEcl = clearNewEcl;
        }

        public bool IsGameOver { get { return Mem.Ram[GameOver] != 0; } }

        void Load(int id)
        {
            EclModule m;
            if (!modules.TryGetValue(id, out m)) throw new EclFormatException("unknown module " + id.ToString("X2"));
            Current = m;
            Mem.Ram[ModuleNow] = (byte)id;
            Mem.Load(EclFormat.Base, m.Bytes);
            Mem.Ram[StopScript] = 0; Mem.Ram[0x9DC8] = 0; Mem.Ram[0x9DC9] = 0; Mem.Ram[0x97DB] = 0;    // 0x4284
            for (int k = 0; k < 5; k++) { int e = m.Entries[k]; Mem.Ram[EntryTable + 2 * k] = (byte)(e >> 8); Mem.Ram[EntryTable + 2 * k + 1] = (byte)e; }
        }

        /// 0x4140.
        public int Boot()
        {
            int id;
            if (Mem.Ram[ModulePrev] != 0) id = Mem.Ram[ModulePrev];
            else if (Mem.Ram[0xBA5A] != 0) id = 3;
            else id = Mem.Ram[0xCA21] == 0 ? 0 : 0x10;
            Load(id);
            InitPass();
            return id;
        }

        /// NEWECL (0x383E).
        void Enter(int id)
        {
            Mem.Ram[ModulePrev] = Mem.Ram[ModuleNow];
            Load(id);
            for (int i = 0; i < 32; i++) Mem.Ram[LocalVars + i] = 0;
            for (int i = 0; i < 10; i++) Mem.Ram[Scratch + i] = 0;
            Mem.Ram[StopScript] = 0xFF; Mem.Ram[Changed] = 0xFF; Mem.Ram[0x9BD5] = 0;
            Log.Add("module " + id.ToString("X2"));
        }

        EclStop Run(Func<EclInterpreter, EclStop> go)
        {
            var it = new EclInterpreter(Current, Mem, host, random);
            ScriptRuns++;
            var stop = go(it);
            Mem.Ram[StopScript] = 0;
            if (stop == EclStop.NewEcl) { int m = newEclRequest(); clearNewEcl(); Enter(m); }
            return stop;
        }

        /// 0x424E.
        public void InitPass()
        {
            int guard = 0;
            do
            {
                Mem.Ram[Changed] = 0; Mem.Ram[0x9DCB] = 0;
                int x = Mem.Ram[GenesisExplore.X], y = Mem.Ram[GenesisExplore.Y], f = Mem.Ram[GenesisExplore.Facing];
                Mem.Ram[GenesisExplore.Attr] = Mem.Ram[GenesisExplore.Attrs + ((y << 4) + x & 0xFF)];
                Mem.Ram[GenesisExplore.WallAhead] = (byte)GenesisExplore.WallNibble(Mem, x, y, (f << 1) & 0xFF);
                if (ResumeAddress != 0) { int a = ResumeAddress; ResumeAddress = 0; Run(it => it.RunFrom(a)); }
                else Run(it => it.RunEntry(4));
            } while (Mem.Ram[Changed] != 0 && ++guard < 64 && !IsGameOver);
            Mem.Ram[ModulePrev] = Mem.Ram[ModuleNow];
        }

        /// One pass of the main loop (ROM 0x4198-0x41FA); false when the game is over. A module change after a script starts an InitPass and a new pass, as the ROM's loop does.
        public bool Tick()
        {
            if (IsGameOver) return false;
            Run(it => it.RunEntry(0));
            if (IsGameOver) return false;
            if (Mem.Ram[Changed] != 0) { InitPass(); return !IsGameOver; }
            if (Mem.Ram[0x9DBF] == 0 && Mem.Ram[0xBA5D] != 0) GenesisExplore.StepForward(Mem);              // 0x4D74: a forced step
            Mem.Ram[0x9DBF] = 0;
            Run(it => it.RunEntry(1));
            if (IsGameOver) return false;
            if (Mem.Ram[Changed] != 0) InitPass();
            return !IsGameOver;
        }
    }
}
