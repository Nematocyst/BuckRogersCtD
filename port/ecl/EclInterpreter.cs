// EclInterpreter.cs -- the script ("ECL") engine of the Sega Genesis Buck Rogers: Countdown to Doomsday: decoder, interpreter and the host interface a front end implements.
// Format (all verified against the ROM, see GENESIS_ECL_FORMAT.md and data/scripts.json, the test oracle):
//   * a module is loaded at 0x6AF6; its first 20 bytes are 5 GOTOs (the entry points run, search, precamp, campint, init); the game overwrites the LAST byte of a loaded module;
//   * instruction = opcode byte + operands, operand = [type][lo]([hi]) (ROM operand reader 0x404A): 00 immediate byte, 01 the BYTE at [addr], 02 immediate word #n,
//     03 the little-endian WORD at [addr] (the listing tools call it "&addr", but the ROM reads memory), 80 text (u16 byte offset into the same-id text module),
//     81 string variable; a destination operand stores 1 byte (type 01) or a little-endian word (type 03); jump targets are absolute addresses (the 0x6AF6 space);
//   * ONGOTO / ONGOSUB / TREASURE / HMENU / WHMENU have a first operand, a count n (immediate) and n further operands;
//   * memory: every operand address is work RAM, 0xFF0000 + addr (the ROM sign-extends the 16-bit address); the loaded module itself sits in that RAM at 0x6AF6, so the
//     tables GETABLE reads (e.g. [7310]) are data inside the module; the windows 0x9AFC-0x9B4F and 0x9BF6-0x9C0F are the record / slot of the character [0x9DA7] (ROM 0x42E0);
//   * COMPARE sets six flag bits (EQ NE LT GT LE GE) comparing operand 0 with operand 1, unsigned; IFEQ..IFGE test one bit each (ROM 0x3452, 0x370C).
// This replaces the DOS (GoldBox) rules. Behaviour that is not verified against the ROM is marked "guess".
using System;
using System.Collections.Generic;
using System.Text;

namespace BuckRogersGenesis.Ecl
{
    public class EclFormatException : Exception { public EclFormatException(string m) : base(m) { } }

    public struct EclOperand
    {
        public int Type, Value;
        public const int Byte = 0, Memory = 1, Word = 2, Address = 3, Text = 0x80, StringVar = 0x81;
        public override string ToString() { return Type + ":" + Value; }
    }

    public sealed class EclInstruction
    {
        public int Addr, Opcode, Length;
        public EclOperand[] Ops;
        public string Name { get { return EclFormat.Name(Opcode); } }
        public int End { get { return Addr + Length; } }
    }

    /// One loaded module: its bytes (loaded at EclFormat.Base), the 5 entry points and the text of its text module.
    public sealed class EclModule
    {
        public int Id;
        public byte[] Bytes;
        public int[] Entries = new int[5];
        public Func<int, string> Text = o => null;     // text offset -> string (BuckRogersData.Text(Id, offset))

        public static EclModule FromHex(int id, string hex, Func<int, string> text)
        {
            var b = new byte[hex.Length / 2];
            for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(hex.Substring(2 * i, 2), 16);
            return FromBytes(id, b, text);
        }

        public static EclModule FromBytes(int id, byte[] b, Func<int, string> text)
        {
            var m = new EclModule { Id = id, Bytes = b };
            if (text != null) m.Text = text;
            for (int i = 0; i < 5; i++)
            {
                if (b.Length < 20 || b[4 * i] != 1 || b[4 * i + 1] != 1) throw new EclFormatException("module does not start with 5 GOTOs");
                m.Entries[i] = b[4 * i + 2] | b[4 * i + 3] << 8;
            }
            return m;
        }

        public int Size { get { return Bytes.Length; } }
        /// The last byte is overwritten by the game: it is never executed and never read.
        public int LastByteAddr { get { return EclFormat.Base + Bytes.Length - 1; } }
    }

    public static class EclFormat
    {
        public const int Base = 0x6AF6;
        public const int Variable = -1, Unknown = -2;
        public static readonly string[] EntryNames = { "run", "search", "precamp", "campint", "init" };

        // operand counts by opcode (-1 variable: first operand, count n, n operands; -2 never used in the game, count unknown)
        static readonly int[] counts = Build();
        static int[] Build()
        {
            var c = new int[0x5E]; for (int i = 0; i < c.Length; i++) c[i] = Unknown;
            int[,] t = {
                {0x00,0},{0x01,1},{0x02,1},{0x03,2},{0x04,3},{0x05,3},{0x06,3},{0x07,3},{0x08,2},{0x09,2},{0x0A,1},{0x0B,3},{0x0C,4},{0x0D,0},{0x0E,1},{0x0F,2},{0x10,2},{0x11,1},{0x12,1},{0x13,0},
                {0x14,4},{0x16,0},{0x17,0},{0x18,0},{0x19,0},{0x1A,0},{0x1B,0},{0x1C,0},{0x1F,4},{0x20,1},{0x21,3},{0x22,3},{0x23,3},{0x24,0},{0x29,0},{0x2A,3},{0x2C,0},{0x2D,0},{0x2E,5},{0x2F,3},
                {0x30,3},{0x32,1},{0x33,0},{0x35,3},{0x36,2},{0x37,1},{0x38,1},{0x39,1},{0x3A,0},{0x3D,0},{0x3E,0},{0x40,2},{0x41,2},{0x42,0},{0x43,1},{0x44,0},{0x45,2},{0x46,2},{0x47,0},{0x48,1},
                {0x49,6},{0x4A,0},{0x4B,1},{0x4C,2},{0x4E,0},{0x4F,0},{0x50,0},{0x51,1},{0x52,0},{0x53,4},{0x54,3},{0x55,4},{0x56,3},{0x57,1},{0x58,0},{0x59,1},{0x5A,0},{0x5B,0},{0x5C,6} };
            for (int i = 0; i < t.GetLength(0); i++) c[t[i, 0]] = t[i, 1];
            foreach (int v in new[] { 0x25, 0x26, 0x27, 0x2B, 0x31 }) c[v] = Variable;
            return c;
        }

        // ROM table 0x446E: the 94 opcode names
        static readonly string[] names = ("EXIT GOTO GOSUB COMPARE ADD SUBTRACT DIVIDE MULTIPLY RANDOM SAVE LOADCHARACTER LOADMONSTER SETUPMONSTERS APPROACH PICTURE INPUTNUMBER INPUTSTRING PRINT PRINTCLEAR RETURN COMPAREAND MENU " +
            "IFEQ IFNE IFLT IFGT IFLE IFGE CLEARMONSTERS SETTIMER CHECKPARTY SPACECOMBAT NEWECL LOADFILES SKILL PRINTSKILL COMBAT ONGOTO ONGOSUB TREASURE ROB CONTINUE GETABLE HMENU GETYN DRAWINDOW DAMAGE AND OR WHMENU FINDITEM " +
            "PRINTRETURN CLOCK SAVETABLE ADDNPC LOADPIECES PROGRAM WHO DELAY SPELLS PROTECT CLEARBOX DUMP JOURNAL DESTROY ADDEP ENCEXIT SOUND SAVECHARACTER HOWFAR FOR ENDFOR HIDEITEMS SKILLDAMAGE DUEL STORE VIEW ANIMATE STAIRCASE HALFSTEP " +
            "STEPFORWARD PALETTE UNLOCKDOOR ADDFIGURE ADDCORPSE ADDFIGURE2 ADDCORPSE2 UPDATEFRAME REMOVEFIGURE EXPLOSION STEPBACK HALFBACK NEWREGION ICONMENU").Split(' ');

        public static string Name(int op) { return op >= 0 && op < names.Length ? names[op] : "OP" + op.ToString("X2"); }
        public static int Count(int op) { return op >= 0 && op < counts.Length ? counts[op] : Unknown; }
        public static bool IsIf(int op) { return op >= 0x16 && op <= 0x1B; }
        public static bool IsTerminator(int op) { return op == 0x00 || op == 0x01 || op == 0x13 || op == 0x20 || op == 0x42; }   // EXIT GOTO RETURN NEWECL ENCEXIT
        public static bool IsVariable(int op) { return Count(op) == Variable; }
    }

    public static class EclDecoder
    {
        /// Decodes the instruction at `addr` (absolute, 0x6AF6 space). Throws EclFormatException for an unknown opcode / operand type or a run past the module.
        public static EclInstruction Decode(EclModule m, int addr)
        {
            int p = addr - EclFormat.Base; var d = m.Bytes;
            if (p < 0 || p >= d.Length) throw new EclFormatException("address outside the module: " + addr.ToString("X"));
            int op = d[p], cnt = EclFormat.Count(op);
            if (cnt == EclFormat.Unknown) throw new EclFormatException("opcode " + op.ToString("X2") + " has no known operand count");
            var ops = new List<EclOperand>(); int q = p + 1;
            if (cnt == EclFormat.Variable)
            {
                var a = Operand(d, ref q); var n = Operand(d, ref q);
                if (n.Type != EclOperand.Byte) throw new EclFormatException("count operand is not an immediate byte");
                ops.Add(a); ops.Add(n);
                for (int i = 0; i < n.Value; i++) ops.Add(Operand(d, ref q));
            }
            else for (int i = 0; i < cnt; i++) ops.Add(Operand(d, ref q));
            return new EclInstruction { Addr = addr, Opcode = op, Ops = ops.ToArray(), Length = q - p };
        }

        static EclOperand Operand(byte[] d, ref int p)
        {
            if (p + 1 >= d.Length) throw new EclFormatException("operand runs past the module");
            int type = d[p], lo = d[p + 1];
            if (type == 0) { p += 2; return new EclOperand { Type = 0, Value = lo }; }
            if (type != 1 && type != 2 && type != 3 && type != 0x80 && type != 0x81) throw new EclFormatException("bad operand type " + type.ToString("X2"));
            if (p + 2 >= d.Length) throw new EclFormatException("operand runs past the module");
            var o = new EclOperand { Type = type, Value = lo | d[p + 2] << 8 }; p += 3; return o;
        }

        /// The jump targets of an instruction (GOTO, GOSUB, ONGOTO, ONGOSUB).
        public static IEnumerable<int> Targets(EclInstruction i)
        {
            if (i.Opcode == 1 || i.Opcode == 2) yield return i.Ops[0].Value;
            else if (i.Opcode == 0x25 || i.Opcode == 0x26) for (int k = 2; k < i.Ops.Length; k++) yield return i.Ops[k].Value;
        }

        /// Every instruction reachable from the 5 entry points, found like the reference disassembler (ecl_disasm.walk): follow jumps; a path ends at EXIT / GOTO / RETURN / NEWECL /
        /// ENCEXIT unless the instruction directly follows an IF (a false IF skips exactly one instruction, whatever it is). Keyed by address.
        public static SortedDictionary<int, EclInstruction> Walk(EclModule m)
        {
            var seen = new SortedDictionary<int, EclInstruction>(); var ifEnds = new HashSet<int>(); var todo = new Stack<int>();
            for (int e = 0; e < 5; e++) todo.Push(EclFormat.Base + 4 * e);
            while (todo.Count > 0)
            {
                int p = todo.Pop();
                while (p - EclFormat.Base >= 0 && p - EclFormat.Base < m.Bytes.Length && !seen.ContainsKey(p))
                {
                    EclInstruction i;
                    try { i = Decode(m, p); } catch (EclFormatException) { break; }
                    seen[p] = i; if (EclFormat.IsIf(i.Opcode)) ifEnds.Add(i.End);
                    foreach (int t in Targets(i)) todo.Push(t);
                    if (EclFormat.IsTerminator(i.Opcode) && !ifEnds.Contains(p)) break;
                    p = i.End;
                }
            }
            return seen;
        }
    }

    /// Script memory: bytes addressed by the 16-bit operands.
    public interface IEclMemory
    {
        int ReadByte(int addr);
        void WriteByte(int addr, int value);
        /// GETABLE (ROM 0x39B4): the byte at (the pointer of the table address) + index, in the 24-bit address space of the 68000 (RAM and ROM, wrapping included).
        int ReadTable(int tableAddr, int index);
        /// Puts bytes into memory (the interpreter loads its module at 0x6AF6, where the scripts' own data tables are read).
        void Load(int addr, byte[] data);
    }

    /// Genesis work RAM (0xFF0000 + addr). The windows of ROM 0x42E0 map the script variables 0x9AFC-0x9B4F onto the record 0xBA68 + 0xD6 * [0x9DA7] and
    /// 0x9BF6-0x9C0F onto the slot 0xC470 + 0x1A * [0x9DA7]. `Rom` is only used when a GETABLE index runs the pointer out of RAM (the 68000's 24-bit wrap).
    public sealed class GenesisEclMemory : IEclMemory
    {
        public readonly byte[] Ram = new byte[0x10000];
        public Func<int, int> Rom = a => 0;
        public int Translate(int addr)
        {
            addr &= 0xFFFF;
            if (addr >= 0x9AFC && addr < 0x9B50) return (addr + 0x1F6C + 0xD6 * Ram[0x9DA7]) & 0xFFFF;
            if (addr >= 0x9BF6 && addr < 0x9C10) return (addr + 0x287A + 0x1A * Ram[0x9DA7]) & 0xFFFF;
            return addr;
        }
        public int ReadByte(int addr) { return Ram[Translate(addr)]; }
        public void WriteByte(int addr, int value) { Ram[Translate(addr)] = (byte)value; }
        public int ReadTable(int tableAddr, int index)
        {
            long a = (0xFF0000L | (uint)Translate(tableAddr)) + (uint)index; a &= 0xFFFFFF;
            return a >= 0xFF0000 ? Ram[a & 0xFFFF] : Rom((int)a) & 0xFF;
        }
        public void Load(int addr, byte[] data) { Array.Copy(data, 0, Ram, addr, Math.Min(data.Length, Ram.Length - addr)); }
    }

    /// What the front end does for the script's side effects. All values are already evaluated.
    public interface IEclHost
    {
        void Print(string text);
        void PrintClear(string text);
        void PrintReturn();
        void Continue();                                  // wait for a key
        void Delay();
        void Picture(int n);
        void Sound(int n);
        void LoadMonster(int id, int count, int picture);
        void ClearMonsters();
        void SetupMonsters(int[] values);
        void Combat();
        void Store(int n);
        void AddEp(int who, int xp);                      // who 1 = every party member
        void Program(int n);                              // 0 = the training screen
        void NewEcl(int module);                          // the script ends; load this module
        void EncounterExit();                             // ENCEXIT: the script ends
        void Treasure(int credits, int[] items);
        int Menu(bool wide, string[] texts);              // HMENU / WHMENU: the 0-based choice
        int InputNumber(int digits);
        bool GetYesNo();
        bool FindItem(int item);
        /// every other opcode (LOADCHARACTER, WHO, DAMAGE, ADDFIGURE ...): the host may evaluate operands / write results through the interpreter
        void Other(EclInterpreter it, EclInstruction i);
    }

    public enum EclStop { Running, Exit, Return, NewEcl, EncounterExit, Goto, UnknownOpcode, BadAddress, LastByte, StepLimit }

    public sealed class EclInterpreter
    {
        public readonly EclModule Module;
        public readonly IEclMemory Memory;
        public readonly IEclHost Host;
        readonly Func<int, int> random;                   // GenesisRng.ScriptRandom
        readonly Stack<int> calls = new Stack<int>();
        public int Pc;
        public int Flags;                                 // bit 0 EQ, 1 NE, 2 LT, 3 GT, 4 LE, 5 GE (ROM [0xB9F2])
        public const int FlagEq = 1, FlagNe = 2, FlagLt = 4, FlagGt = 8, FlagLe = 16, FlagGe = 32;
        public int Steps, MaxSteps = 200000;
        public readonly List<string> Log = new List<string>();
        public const int LoopVariable = 0x98EC, LoopLimit = 0x98ED, RemainderVariable = 0x9E35;
        int loopReturn;                                   // ROM [0xB9A8]: FOR does not nest

        // operand counts the ROM uses to skip the instruction after a false IF (table at ROM 0x482E; the variable-length opcodes ONGOTO / ONGOSUB / TREASURE / HMENU / WHMENU
        // are skipped with a count that is too small, so a script must not put them directly after an IF; none does)
        static readonly int[] skipCounts = { 0,1,1,2,3,3,3,3,2,2,1,3,4,0,1,2,2,1,1,0,4,0,0,0,0,0,0,0,0,2,6,2,1,3,3,3,0,0,2,0,3,0,3,0,0,0,5,3,3,0,1,0,1,3,1,1,1,1,0,3,1,0,0,2,2,2,0,1,0,2,2,0,1,6,0,1,2,0,0,0,0,1,0,4,3,4,3,1,0,1,0,0,0,0 };

        public EclInterpreter(EclModule module, IEclMemory memory, IEclHost host, Func<int, int> random)
        {
            Module = module; Memory = memory; Host = host; this.random = random ?? (max => 0);
            Memory.Load(EclFormat.Base, module.Bytes);
        }

        // ------------------------------------------------------------------ operands
        /// The value of an operand (ROM 0x404A): immediates as they are, type 01 reads the byte at its address, type 03 the little-endian word at its address;
        /// a text offset / string variable address is returned as it is.
        public int Value(EclOperand o)
        {
            switch (o.Type)
            {
                case EclOperand.Memory: return Memory.ReadByte(o.Value);
                case EclOperand.Address: return Memory.ReadByte(o.Value) | Memory.ReadByte(o.Value + 1) << 8;
                default: return o.Value;
            }
        }

        /// A destination operand (ROM 0x42AA): type 01 stores one byte at its address, type 03 a little-endian word.
        public void Store(EclOperand dest, int value)
        {
            if (dest.Type == EclOperand.Memory) Memory.WriteByte(dest.Value, value & 0xFF);
            else if (dest.Type == EclOperand.Address) { Memory.WriteByte(dest.Value, value & 0xFF); Memory.WriteByte(dest.Value + 1, (value >> 8) & 0xFF); }
            else Log.Add("store to operand " + dest + " at " + Pc.ToString("X") + " ignored");
        }

        public string Text(EclOperand o)
        {
            if (o.Type == EclOperand.Text) return Module.Text(o.Value);
            if (o.Type == EclOperand.StringVar) { var sb = new StringBuilder(); for (int a = o.Value; sb.Length < 255; a++) { int c = Memory.ReadByte(a); if (c == 0) break; sb.Append((char)c); } return sb.ToString(); }
            return Value(o).ToString();                        // PRINT [9E70]: a number
        }

        // ------------------------------------------------------------------ running
        /// Runs from entry point `entry` (0 run, 1 search, 2 precamp, 3 campint, 4 init).
        public EclStop RunEntry(int entry) { return RunFrom(Module.Entries[entry]); }

        public EclStop RunFrom(int addr)
        {
            Pc = addr; calls.Clear(); Steps = 0;
            bool skip = false;
            while (true)
            {
                if (++Steps > MaxSteps) return EclStop.StepLimit;
                int p = Pc - EclFormat.Base;
                if (p < 0 || p >= Module.Bytes.Length) { Log.Add("pc outside the module: " + Pc.ToString("X")); return EclStop.BadAddress; }
                if (Pc >= Module.LastByteAddr) { Log.Add("reached the last byte (overwritten by the game) at " + Pc.ToString("X")); return EclStop.LastByte; }
                EclInstruction i;
                if (skip)
                {
                    skip = false;                                   // a false IF skips the next instruction the way the ROM does (0x3748)
                    int q = p + 1, n = Module.Bytes[p] < skipCounts.Length ? skipCounts[Module.Bytes[p]] : 0;
                    for (int k = 0; k < n && q < Module.Bytes.Length; k++) q += Module.Bytes[q] == 0 ? 2 : 3;
                    Pc = EclFormat.Base + q; continue;
                }
                try { i = EclDecoder.Decode(Module, Pc); }
                catch (EclFormatException e) { Log.Add(e.Message + " at " + Pc.ToString("X")); return EclStop.UnknownOpcode; }
                if (i.End - 1 >= Module.LastByteAddr) { Log.Add("instruction at " + Pc.ToString("X") + " reads the last byte"); return EclStop.LastByte; }
                Pc = i.End;
                var stop = Execute(i, ref skip);
                if (stop != EclStop.Running) return stop;
            }
        }

        EclStop Execute(EclInstruction i, ref bool skip)
        {
            var o = i.Ops;
            switch (i.Opcode)
            {
                case 0x00: calls.Clear(); return EclStop.Exit;
                case 0x01: Pc = o[0].Value; return EclStop.Running;
                case 0x02: calls.Push(Pc); Pc = o[0].Value; return EclStop.Running;
                case 0x13: if (calls.Count == 0) return EclStop.Return; Pc = calls.Pop(); return EclStop.Running;
                case 0x20: Host.NewEcl(Value(o[0])); return EclStop.NewEcl;
                case 0x42: Host.EncounterExit(); return EclStop.EncounterExit;

                case 0x03: Compare(o[0], o[1]); break;
                case 0x14:                                                                // COMPAREAND: both comparisons equal -> EQ, else NE (only those two bits)
                    {
                        Compare(o[0], o[1]); bool e1 = (Flags & FlagEq) != 0;
                        Compare(o[2], o[3]); bool e2 = (Flags & FlagEq) != 0;
                        Flags = e1 && e2 ? FlagEq : FlagNe;
                        break;
                    }
                case 0x16: skip = (Flags & FlagEq) == 0; break;
                case 0x17: skip = (Flags & FlagNe) == 0; break;
                case 0x18: skip = (Flags & FlagLt) == 0; break;
                case 0x19: skip = (Flags & FlagGt) == 0; break;
                case 0x1A: skip = (Flags & FlagLe) == 0; break;
                case 0x1B: skip = (Flags & FlagGe) == 0; break;

                case 0x04: Store(o[2], Value(o[0]) + Value(o[1])); break;                 // ADD: op0 + op1
                case 0x05: Store(o[2], Value(o[1]) - Value(o[0])); break;                 // SUBTRACT: op1 - op0
                case 0x06:                                                                // DIVIDE: op0 / (op1 & 0xFFFF) by the ROM routine 0x13406 (zero divisor: quotient 0); the remainder goes to [0x9E35]
                    {
                        uint a = (uint)Value(o[0]); uint b = (uint)Value(o[1]) & 0xFFFF;
                        uint q = b == 0 ? 0 : a / b, r = b == 0 ? a : a % b;
                        Memory.WriteByte(RemainderVariable, (int)(r & 0xFF)); Store(o[2], (int)q); break;
                    }
                case 0x07: Store(o[2], (int)((uint)(Value(o[0]) & 0xFFFF) * (uint)(Value(o[1]) & 0xFFFF))); break;   // MULTIPLY: 16 x 16 bits (mulu.w)
                case 0x2F: { int r = Value(o[0]) & Value(o[1]); Flags = r == 0 ? FlagEq : FlagNe; Store(o[2], r); break; }   // AND / OR also set EQ (zero) / NE
                case 0x30: { int r = Value(o[0]) | Value(o[1]); Flags = r == 0 ? FlagEq : FlagNe; Store(o[2], r); break; }
                case 0x08: Store(o[1], random(Value(o[0]))); break;                       // RANDOM max, dest (GenesisRng.ScriptRandom; RANDOM 255 returns 0 without advancing the RNG: ROM 0x351E)
                case 0x09: Store(o[1], Value(o[0])); break;                               // SAVE value, dest
                case 0x2A: Store1(o[2], Memory.ReadTable(o[0].Value, Value(o[1]))); break;   // GETABLE table, index, dest: one byte from table + index (ROM 0x39B4)

                case 0x46: Memory.WriteByte(LoopVariable, Value(o[0]) & 0xFF); Memory.WriteByte(LoopLimit, Value(o[1]) & 0xFF); loopReturn = Pc; break;   // FOR start, end (ROM 0x3CE2: one loop at a time)
                case 0x47:                                                                // ENDFOR (ROM 0x3CF8): counter + 1 (byte); back to the FOR while counter <= limit
                    {
                        int c = (Memory.ReadByte(LoopVariable) + 1) & 0xFF; Memory.WriteByte(LoopVariable, c);
                        if (c <= Memory.ReadByte(LoopLimit)) Pc = loopReturn;
                        break;
                    }
                case 0x25: case 0x26:                                                     // ONGOTO / ONGOSUB selector, n, targets: 0-based; no jump when out of range (ROM 0x38EA)
                    {
                        int sel = Value(o[0]) & 0xFFFF, n = o[1].Value;
                        if (sel < n) { if (i.Opcode == 0x26) calls.Push(Pc); Pc = o[2 + sel].Value; }
                        break;
                    }

                case 0x11: Host.Print(Text(o[0])); break;
                case 0x12: Host.PrintClear(Text(o[0])); break;
                case 0x33: Host.PrintReturn(); break;                                      // prints a line break and CONTINUES
                case 0x29: Host.Continue(); break;
                case 0x3A: Host.Delay(); break;
                case 0x0E: Host.Picture(Value(o[0])); break;
                case 0x43: Host.Sound(Value(o[0])); break;
                case 0x0B: Host.LoadMonster(Value(o[0]), Value(o[1]), Value(o[2])); break;
                case 0x1C: Host.ClearMonsters(); break;
                case 0x0C: Host.SetupMonsters(Values(o)); break;
                case 0x24: Host.Combat(); break;
                case 0x4B: Host.Store(Value(o[0])); break;
                case 0x41: Host.AddEp(Value(o[0]), Value(o[1])); break;
                case 0x38: Host.Program(Value(o[0])); break;
                case 0x27: Host.Treasure(Value(o[0]), Values(o, 2)); break;
                case 0x2B: case 0x31:                                                      // HMENU / WHMENU dest, n, texts: the answer (0-based) is written to dest
                    {
                        var texts = new string[o[1].Value]; for (int k = 0; k < texts.Length; k++) texts[k] = Text(o[2 + k]);
                        Store(o[0], Host.Menu(i.Opcode == 0x31, texts));
                        break;
                    }
                case 0x0F: Store(o[1], Host.InputNumber(Value(o[0]))); break;              // INPUTNUMBER digits, dest
                case 0x2C: Flags = Host.GetYesNo() ? FlagEq : FlagNe; break;               // GETYN: yes = EQ (ROM 0x39D0)
                case 0x32: Flags = Host.FindItem(Value(o[0])) ? FlagEq : FlagNe; break;    // FINDITEM: found = EQ (ROM 0x3A8E)
                default: Host.Other(this, i); break;
            }
            return EclStop.Running;
        }

        void Store1(EclOperand dest, int value)
        {
            if (dest.Type == EclOperand.Memory || dest.Type == EclOperand.Address) Memory.WriteByte(dest.Value, value & 0xFF);
            else Log.Add("store to operand " + dest + " at " + Pc.ToString("X") + " ignored");
        }

        /// ROM 0x3452: unsigned compare of operand 0 with operand 1 (strings when operand 1 is text: equal -> EQ, else NE). Flags: EQ, NE, LT (op0 < op1), GT, LE, GE.
        void Compare(EclOperand a, EclOperand b)
        {
            if (b.Type == EclOperand.Text || b.Type == EclOperand.StringVar)
            {
                Flags = string.Equals(Text(a), Text(b), StringComparison.Ordinal) ? FlagEq : FlagNe; return;   // guess: ordinal equality (the ROM calls 0x1348C)
            }
            uint x = (uint)Value(a), y = (uint)Value(b);
            Flags = x > y ? FlagGt | FlagGe | FlagNe : x < y ? FlagLt | FlagLe | FlagNe : FlagEq | FlagLe | FlagGe;
        }

        int[] Values(EclOperand[] o, int from = 0)
        {
            var v = new int[o.Length - from]; for (int k = from; k < o.Length; k++) v[k - from] = Value(o[k]); return v;
        }
    }
}
