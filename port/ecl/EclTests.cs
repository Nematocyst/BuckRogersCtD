// Acceptance tests of the Genesis ECL interpreter against data/scripts.json (the oracle, checked byte for byte against the ROM).
using System;
using System.Collections.Generic;
using System.IO;
using BuckRogersGenesis;
using BuckRogersGenesis.Ecl;

[Serializable] public class OScripts { public string source; public string[] entryNames; public OModule[] modules; }
[Serializable] public class OModule { public int id, romAddress, loadAddress, size; public int[] entryPoints; public string bytesHex; public OInstr[] instructions; public OString[] strings; }
[Serializable] public class OInstr { public int addr, opcode; public string name; public OOperand[] operands; }
[Serializable] public class OOperand { public int type; public string kind; public int value; public string text; }
[Serializable] public class OString { public int offset; public string text; }

sealed class StubHost : IEclHost
{
    public List<string> Events = new List<string>();
    public int MenuChoice = 0; public bool Yes = true; public bool MenuLast;
    public int NewEclModule = -1;
    public void Print(string t) { Events.Add("PRINT " + t); }
    public void PrintClear(string t) { Events.Add("PRINTCLEAR " + t); }
    public void PrintReturn() { Events.Add("PRINTRETURN"); }
    public void Continue() { Events.Add("CONTINUE"); }
    public void Delay() { Events.Add("DELAY"); }
    public void Picture(int n) { Events.Add("PICTURE " + n); }
    public void Sound(int n) { Events.Add("SOUND " + n); }
    public void LoadMonster(int id, int count, int pic) { Events.Add($"LOADMONSTER {id} {count} {pic}"); }
    public void ClearMonsters() { Events.Add("CLEARMONSTERS"); }
    public void SetupMonsters(int[] v) { Events.Add("SETUPMONSTERS " + string.Join(",", v)); }
    public void Combat() { Events.Add("COMBAT"); }
    public void Store(int n) { Events.Add("STORE " + n); }
    public void AddEp(int who, int xp) { Events.Add($"ADDEP {who} {xp}"); }
    public void Program(int n) { Events.Add("PROGRAM " + n); }
    public void NewEcl(int m) { NewEclModule = m; Events.Add("NEWECL " + m); }
    public void EncounterExit() { Events.Add("ENCEXIT"); }
    public void Treasure(int c, int[] items) { Events.Add($"TREASURE {c} x{items.Length}"); }
    public int Menu(bool wide, string[] texts) { return MenuLast ? texts.Length - 1 : MenuChoice; }
    public int InputNumber(int digits) { return 1; }
    public bool GetYesNo() { return Yes; }
    public bool FindItem(int item) { return Yes; }
    public void Other(EclInterpreter it, EclInstruction i) { Events.Add(i.Name); }
}

sealed class Asm
{
    public List<byte> Bytes = new List<byte>();
    public int Addr { get { return EclFormat.Base + Bytes.Count; } }
    public int Entry;
    public static byte[] B(int v) { return new byte[] { 0, (byte)v }; }
    public static byte[] M(int a) { return new byte[] { 1, (byte)a, (byte)(a >> 8) }; }
    public static byte[] A(int a) { return new byte[] { 3, (byte)a, (byte)(a >> 8) }; }
    public static byte[] W(int v) { return new byte[] { 2, (byte)v, (byte)(v >> 8) }; }
    public static byte[] L(int a) { return new byte[] { 1, (byte)a, (byte)(a >> 8) }; }
    public void Start() { for (int e = 0; e < 5; e++) { Bytes.Add(1); Bytes.AddRange(L(EclFormat.Base + 20)); } Entry = EclFormat.Base + 20; }
    public int Emit(int op, params byte[][] ops) { int at = Addr; Bytes.Add((byte)op); foreach (var o in ops) Bytes.AddRange(o); return at; }
    public byte[] Pad() { var b = new List<byte>(Bytes); b.AddRange(new byte[] { 0, 0, 0, 0 }); return b.ToArray(); }   // the last byte is never executed
}

static class EclTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 20) Console.WriteLine("FAIL: " + what); } }

    static byte[] Rom;
    static IEnumerable<string> ReadLines(string path)
    {
        using (var fs = File.OpenRead(path))
        using (var gz = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Decompress))
        using (var rd = new StreamReader(gz)) { string l; while ((l = rd.ReadLine()) != null) yield return l; }
    }
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }

    public static int Main(string[] args)
    {
        var json = File.ReadAllText(args[0]);
        var data = UnityEngine.JsonUtility.FromJson<OScripts>(json);
        Rom = args.Length > 1 ? File.ReadAllBytes(args[1]) : null;
        var mods = new Dictionary<int, EclModule>(); var oracle = new Dictionary<int, OModule>();
        foreach (var om in data.modules)
        {
            var strings = new Dictionary<int, string>(); foreach (var s in om.strings) strings[s.offset] = s.text;
            var m = EclModule.FromHex(om.id, om.bytesHex, off => { string t; return strings.TryGetValue(off, out t) ? t : null; });
            mods[om.id] = m; oracle[om.id] = om;
        }

        // The oracle decodes the one SKILLDAMAGE of the game (module 0x53, the script's last instruction) with 7 operands; the ROM's skip table (0x482E) and its handler (0x5B44) read 6, so the
        // module's final bytes are SKILLDAMAGE (6 operands), EXIT and the byte the game overwrites. Adjust the oracle accordingly (one more instruction).
        foreach (var om in data.modules)
        {
            var fixedList = new List<OInstr>();
            foreach (var oi in om.instructions)
            {
                if (oi.opcode == 0x49 && oi.operands.Length == 7)
                {
                    int addr = oi.addr; var six = new OOperand[6]; Array.Copy(oi.operands, six, 6); oi.operands = six; fixedList.Add(oi);
                    fixedList.Add(new OInstr { addr = addr + 13, opcode = 0, name = "EXIT", operands = new OOperand[0] });
                }
                else fixedList.Add(oi);
            }
            om.instructions = fixedList.ToArray();
        }

        // a. the decoder against the oracle
        int total = 0, texts = 0, textOk = 0;
        foreach (var om in data.modules)
        {
            var m = mods[om.id]; var walk = EclDecoder.Walk(m);
            Check(walk.Count == om.instructions.Length, $"module {om.id:X2}: {walk.Count} instructions vs oracle {om.instructions.Length}");
            Check(m.Bytes.Length == om.size && om.loadAddress == EclFormat.Base, $"module {om.id:X2}: size / load address");
            for (int e = 0; e < 5; e++) Check(m.Entries[e] == om.entryPoints[e], $"module {om.id:X2}: entry {e}");
            foreach (var oi in om.instructions)
            {
                total++;
                EclInstruction i; if (!walk.TryGetValue(oi.addr, out i)) { Check(false, $"module {om.id:X2}: instruction at {oi.addr:X} not found"); continue; }
                bool same = i.Opcode == oi.opcode && i.Ops.Length == oi.operands.Length && i.Name == oi.name;
                for (int k = 0; same && k < i.Ops.Length; k++) same = i.Ops[k].Type == oi.operands[k].type && i.Ops[k].Value == oi.operands[k].value;
                Check(same, $"module {om.id:X2} {oi.addr:X}: {oi.name} differs");
                for (int k = 0; k < i.Ops.Length; k++)
                    if (i.Ops[k].Type == 0x80) { texts++; if (m.Text(i.Ops[k].Value) == oi.operands[k].text && oi.operands[k].text != null) textOk++; }
            }
        }
        Check(total == 13937, "13,936 oracle instructions + the EXIT behind SKILLDAMAGE: " + total);
        Check(texts == 2261 && textOk == 2261, $"2,261 text operands resolve: {textOk} / {texts}");
        Console.WriteLine($"a. decoder: {total} instructions, {textOk}/{texts} text operands");

        // b. module 0x10: the first fight
        {
            var mem = new GenesisEclMemory(); var host = new StubHost();
            mem.Ram[0x9E6F] = 3;
            var it = new EclInterpreter(mods[0x10], mem, host, max => 0);
            var stop = it.RunFrom(0x6D3E);
            var want = new[] { "CLEARMONSTERS", "LOADMONSTER 41 2 32", "LOADMONSTER 32 1 32", "LOADMONSTER 39 3 39", "COMBAT" };
            Check(stop == EclStop.Exit && string.Join("|", host.Events) == string.Join("|", want), "module 10 fight events: " + string.Join("|", host.Events) + " stop " + stop);
            // with [9E6F] = 0 the third group is not loaded
            mem = new GenesisEclMemory(); host = new StubHost(); new EclInterpreter(mods[0x10], mem, host, max => 0).RunFrom(0x6D3E);
            Check(!host.Events.Exists(e => e.StartsWith("LOADMONSTER 39")), "module 10: no third group when [9E6F] = 0");
        }

        // c. module 0x5E: the path ending in NEWECL 96 stops there
        {
            var mem = new GenesisEclMemory(); var host = new StubHost(); mem.Ram[0x98B0] = 1;
            var stop = new EclInterpreter(mods[0x5E], mem, host, max => 0).RunFrom(0x6B32);
            Check(stop == EclStop.NewEcl && host.NewEclModule == 96 && host.Events.Count == 1, "module 5E NEWECL 96: " + stop + " " + string.Join("|", host.Events));
            var walk = EclDecoder.Walk(mods[0x5E]);
            Check(walk.ContainsKey(0x6B3D) && walk.ContainsKey(0x6B40), "module 5E: the text after NEWECL 96 is reached by another path only");
        }

        // d. RANDOM 255 returns 0 and does not advance the RNG
        {
            var w = new ushort[256]; var rnd = new Random(5); for (int i = 0; i < 256; i++) w[i] = (ushort)rnd.Next(65536);
            var rng = GenesisRng.FromSeedWords(w); var before = rng.TableCopy(); int idx = rng.Index;
            var asm = new Asm(); asm.Start(); asm.Emit(0x08, Asm.B(255), Asm.M(0x9E70)); asm.Emit(0x00); var full = asm.Pad();
            var mod = EclModule.FromBytes(0xFF, full, null);
            var mem = new GenesisEclMemory(); mem.Ram[0x9E70] = 77;
            var stop = new EclInterpreter(mod, mem, new StubHost(), rng.ScriptRandom).RunFrom(asm.Entry);
            var after = rng.TableCopy(); bool same = rng.Index == idx; for (int i = 0; i < 256; i++) same &= before[i] == after[i];
            Check(stop == EclStop.Exit && mem.Ram[0x9E70] == 0 && same, $"RANDOM 255: result {mem.Ram[0x9E70]}, rng unchanged {same}, stop {stop}");
            Check(rng.ScriptRandom(255) == 0, "ScriptRandom(255) == 0");
            var rng2 = GenesisRng.FromSeedWords(w); int r1 = rng2.ScriptRandom(9); var t2 = rng2.TableCopy(); bool moved = rng2.Index != idx; for (int i = 0; i < 256; i++) moved |= t2[i] != before[i];
            Check(r1 >= 0 && r1 <= 9 && moved, "RANDOM 9 is in range and advances the RNG");
        }

        // e. every entry point of every module with stub hosts: no unknown opcode, no last-byte read, no bad address
        {
            int runs = 0, limit = 0;
            foreach (var m in mods.Values)
                for (int policy = 0; policy < 4; policy++)
                    for (int e = 0; e < 5; e++)
                    {
                        var mem = new GenesisEclMemory(); if (policy >= 2) for (int a = 0x8000; a < 0x10000; a++) mem.Ram[a] = (byte)(a * 7 + policy);
                        var host = new StubHost { MenuLast = policy % 2 == 1, Yes = policy % 2 == 0 };
                        var it = new EclInterpreter(m, mem, host, max => max / 2) { MaxSteps = 20000 };
                        var stop = it.RunEntry(e); runs++; if (stop == EclStop.StepLimit) limit++;
                        Check(stop != EclStop.UnknownOpcode && stop != EclStop.LastByte && stop != EclStop.BadAddress, $"module {m.Id:X2} entry {e} policy {policy}: stop {stop} {string.Join(";", it.Log)}");
                    }
            Console.WriteLine($"e. {runs} entry runs, {limit} stopped by the step limit");
        }

        // g. control flow on small synthetic modules
        {
            Func<Asm, GenesisEclMemory> run = a => { var mem = new GenesisEclMemory(); new EclInterpreter(EclModule.FromBytes(0xFE, a.Pad(), null), mem, new StubHost(), max => 0).RunFrom(a.Entry); return mem; };
            var asm = new Asm(); asm.Start();                                                        // a false IF skips exactly one instruction
            asm.Emit(0x09, Asm.B(1), Asm.M(0x9E70)); asm.Emit(0x03, Asm.B(5), Asm.M(0x9E70)); asm.Emit(0x16); asm.Emit(0x09, Asm.B(9), Asm.M(0x9E70)); asm.Emit(0x09, Asm.B(7), Asm.M(0x9E71)); asm.Emit(0x00);
            var m1 = run(asm); Check(m1.Ram[0x9E70] == 1 && m1.Ram[0x9E71] == 7, "false IFEQ skips one instruction");
            asm = new Asm(); asm.Start();                                                            // a true IF before a terminator: the terminator runs
            asm.Emit(0x09, Asm.B(1), Asm.M(0x9E70)); asm.Emit(0x03, Asm.B(1), Asm.M(0x9E70)); asm.Emit(0x16); asm.Emit(0x00); asm.Emit(0x09, Asm.B(7), Asm.M(0x9E71)); asm.Emit(0x00);
            m1 = run(asm); Check(m1.Ram[0x9E71] == 0, "true IFEQ then EXIT exits");
            asm = new Asm(); asm.Start();                                                            // a false IF skips a 3-operand instruction whole
            asm.Emit(0x09, Asm.B(0), Asm.M(0x9E70)); asm.Emit(0x03, Asm.B(1), Asm.M(0x9E70)); asm.Emit(0x16);
            asm.Emit(0x04, Asm.B(1), Asm.W(2), Asm.M(0x9E72)); asm.Emit(0x09, Asm.B(7), Asm.M(0x9E71)); asm.Emit(0x00);
            m1 = run(asm); Check(m1.Ram[0x9E71] == 7 && m1.Ram[0x9E72] == 0, "false IF skips an ADD");
            asm = new Asm(); asm.Start();                                                            // operand type 03 = the word at [addr]; a type 03 destination stores a little-endian word
            asm.Emit(0x09, Asm.W(0x1234), Asm.A(0x9E70)); asm.Emit(0x09, Asm.A(0x9E70), Asm.M(0x9E74)); asm.Emit(0x09, Asm.M(0x9E71), Asm.M(0x9E75)); asm.Emit(0x00);
            m1 = run(asm); Check(m1.Ram[0x9E70] == 0x34 && m1.Ram[0x9E71] == 0x12 && m1.Ram[0x9E74] == 0x34 && m1.Ram[0x9E75] == 0x12, "word operands");
            asm = new Asm(); asm.Start();                                                            // execution must not run into the last byte
            asm.Emit(0x09, Asm.B(1), Asm.M(0x9E70)); var sm = new GenesisEclMemory(); var shost = new StubHost();
            var lastMod = EclModule.FromBytes(0xFD, asm.Pad(), null); var lastStop = new EclInterpreter(lastMod, sm, shost, null).RunFrom(asm.Entry);
            Check(lastStop == EclStop.LastByte || lastStop == EclStop.Exit, "falling off the end stops: " + lastStop);
            asm = new Asm(); asm.Start();                                                            // PRINTRETURN continues; GOSUB / RETURN
            int sub = 0; asm.Emit(0x33); int gs = asm.Emit(0x02, Asm.L(0)); asm.Emit(0x09, Asm.B(5), Asm.M(0x9E70)); asm.Emit(0x00); sub = asm.Addr; asm.Emit(0x09, Asm.B(6), Asm.M(0x9E71)); asm.Emit(0x13);
            var bb = asm.Bytes; int gp = gs - EclFormat.Base + 2; bb[gp] = (byte)sub; bb[gp + 1] = (byte)(sub >> 8);
            m1 = run(asm); Check(m1.Ram[0x9E70] == 5 && m1.Ram[0x9E71] == 6, "PRINTRETURN continues, GOSUB / RETURN");
            asm = new Asm(); asm.Start();                                                            // FOR 0,3 ... ENDFOR runs the body 4 times, [98EC] counts
            asm.Emit(0x46, Asm.B(0), Asm.B(3)); asm.Emit(0x04, Asm.B(1), Asm.M(0x9E71), Asm.M(0x9E71)); asm.Emit(0x47); asm.Emit(0x00);
            m1 = run(asm); Check(m1.Ram[0x9E71] == 4 && m1.Ram[0x98EC] == 4, "FOR 0,3: body 4 times " + m1.Ram[0x9E71]);
            asm = new Asm(); asm.Start();                                                            // ONGOTO selects the target by a 0-based selector; out of range falls through
            asm.Emit(0x09, Asm.B(1), Asm.M(0x9E70)); int og = asm.Emit(0x25, Asm.M(0x9E70), Asm.B(2), Asm.L(0), Asm.L(0)); asm.Emit(0x09, Asm.B(9), Asm.M(0x9E71)); asm.Emit(0x00);
            int t0 = asm.Addr; asm.Emit(0x09, Asm.B(1), Asm.M(0x9E72)); asm.Emit(0x00); int t1 = asm.Addr; asm.Emit(0x09, Asm.B(2), Asm.M(0x9E72)); asm.Emit(0x00);
            int pp = og - EclFormat.Base + 1 + 3 + 2; bb = asm.Bytes; bb[pp + 1] = (byte)t0; bb[pp + 2] = (byte)(t0 >> 8); bb[pp + 4] = (byte)t1; bb[pp + 5] = (byte)(t1 >> 8);
            m1 = run(asm); Check(m1.Ram[0x9E72] == 2 && m1.Ram[0x9E71] == 0, "ONGOTO selector 1 -> second target");
            // arithmetic: ADD, SUBTRACT (b - a), MULTIPLY, AND, OR
            asm = new Asm(); asm.Start(); asm.Emit(0x09, Asm.B(10), Asm.M(0x9E70)); asm.Emit(0x04, Asm.B(5), Asm.M(0x9E70), Asm.M(0x9E71)); asm.Emit(0x05, Asm.B(3), Asm.M(0x9E70), Asm.M(0x9E72));
            asm.Emit(0x07, Asm.B(4), Asm.M(0x9E70), Asm.M(0x9E73)); asm.Emit(0x2F, Asm.B(0x0C), Asm.M(0x9E70), Asm.M(0x9E74)); asm.Emit(0x30, Asm.B(0x05), Asm.M(0x9E70), Asm.M(0x9E75)); asm.Emit(0x00);
            m1 = run(asm); Check(m1.Ram[0x9E71] == 15 && m1.Ram[0x9E72] == 7 && m1.Ram[0x9E73] == 40 && m1.Ram[0x9E74] == 8 && m1.Ram[0x9E75] == 15, "arithmetic");
        }

        // h. differential: random scripts run by the ROM's own engine (tools/gen_ecl_vectors.py) vs this interpreter
        if (args.Length > 2 && File.Exists(args[2]))
        {
            var boot = new ushort[256]; var bh = File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[2]), "ecl_boot_table.txt")).Trim();
            for (int i = 0; i < 256; i++) boot[i] = (ushort)Convert.ToInt32(bh.Substring(4 * i, 4), 16);
            int[][] regions = { new[] { 0x9E30, 0x9E90 }, new[] { 0x98E0, 0x98F0 }, new[] { 0xB9F0, 0xB9F4 }, new[] { 0xBA68, 0xBA68 + 3 * 0xD6 }, new[] { 0xC470, 0xC470 + 3 * 0x1A }, new[] { 0x9DA7, 0x9DA8 } };
            int cases = 0, bad = 0;
            foreach (var line in ReadLines(args[2]))
            {
                if (line.Length == 0) continue; var f = line.Split(' '); cases++;
                var code = Hex(f[0]); var init = Hex(f[1]); var fin = Hex(f[2]); int flags = int.Parse(f[3]) & 0x3F, ridx = int.Parse(f[4]); long rsum = long.Parse(f[5]); int idx0 = int.Parse(f[6]);
                var mem = new GenesisEclMemory(); if (Rom != null) mem.Rom = a => a < Rom.Length ? Rom[a] : 0;
                var modMem = new byte[0];
                int pos = 0; foreach (var r in regions) for (int a = r[0]; a < r[1]; a++) mem.Ram[a] = init[pos++];
                for (int k = 0; k < 16; k++) { mem.Ram[0x9E70 + k] = init[pos + k]; mem.Ram[0x9E40 + k] = init[pos + 16 + k]; }
                var rng = GenesisRng.FromState(boot, (byte)idx0);
                var mod = EclModule.FromBytes(0xFC, code, null);
                var it = new EclInterpreter(mod, mem, new StubHost(), rng.ScriptRandom);
                var stop = it.RunFrom(EclFormat.Base + 20);
                string why = null;
                if (stop != EclStop.Exit) why = "stop " + stop + " " + string.Join(";", it.Log);
                pos = 0;
                foreach (var r in regions)
                    for (int a = r[0]; a < r[1]; a++, pos++)
                    {
                        if (why != null) break;
                        if (a >= 0xB9F0 && a < 0xB9F4) continue;                          // the flag byte is compared below; the others are the engine's own
                        if (mem.Ram[a] != fin[pos]) why = $"memory {a:X4}: {mem.Ram[a]:X2} vs ROM {fin[pos]:X2}";
                    }
                if (why == null && it.Flags != flags) why = $"flags {it.Flags:X} vs ROM {flags:X}";
                if (why == null && rng.Index != ridx) why = $"rng index {rng.Index} vs ROM {ridx}";
                if (why == null) { long sum = 0; foreach (var w in rng.TableCopy()) sum += w; if (sum != rsum) why = "rng table"; }
                if (why != null) { bad++; Check(false, $"ecl vector {cases}: {why}"); } else checks++;
            }
            Console.WriteLine($"h. differential: {cases} scripts against the ROM engine, {bad} differ");
        }

        Console.WriteLine($"ecl: {checks - fails}/{checks} checks passed");
        return fails == 0 ? 0 : 1;
    }
}
