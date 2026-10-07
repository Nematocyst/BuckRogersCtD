// Tests of EclCombatHost: scripts that load monsters and fight, run through the interpreter, against the same fight built directly with the combat API.
using System;
using System.Collections.Generic;
using System.IO;
using BuckRogersGenesis;
using BuckRogersGenesis.Ecl;

[Serializable] public class HScripts { public HModule[] modules; }
[Serializable] public class HModule { public int id; public string bytesHex; public HString[] strings; }
[Serializable] public class HString { public int offset; public string text; }

sealed class HAsm
{
    public List<byte> Bytes = new List<byte>(); public int Entry;
    public int Addr { get { return EclFormat.Base + Bytes.Count; } }
    public static byte[] B(int v) { return new byte[] { 0, (byte)v }; }
    public static byte[] M(int a) { return new byte[] { 1, (byte)a, (byte)(a >> 8) }; }
    public static byte[] W(int v) { return new byte[] { 2, (byte)v, (byte)(v >> 8) }; }
    public void Start() { for (int e = 0; e < 5; e++) { Bytes.Add(1); Bytes.AddRange(M(EclFormat.Base + 20)); } Entry = EclFormat.Base + 20; }
    public void Emit(int op, params byte[][] ops) { Bytes.Add((byte)op); foreach (var o in ops) Bytes.AddRange(o); }
    public EclModule Module() { var b = new List<byte>(Bytes); b.AddRange(new byte[] { 0, 0, 0, 0 }); return EclModule.FromBytes(0xFE, b.ToArray(), null); }
}

static class EclHostTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 20) Console.WriteLine("FAIL: " + what); } }

    static ushort[] Seed(int n) { var w = new ushort[256]; var r = new Random(n); for (int i = 0; i < 256; i++) w[i] = (ushort)r.Next(65536); return w; }

    static EclCombatHost NewHost(RomView rom, MonsterBinFile mf, DefaultParty dp, int seed, GenesisEclMemory mem)
    {
        return new EclCombatHost(rom, mf, dp, GenesisRng.FromSeedWords(Seed(seed)), mem);
    }

    public static int Main(string[] args)
    {
        // args: scripts.json  monster_file.bytes  default_party.bytes  ROM
        var rom = RomView.FromRom(File.ReadAllBytes(args[3]));
        var mf = MonsterBinFile.Parse(File.ReadAllBytes(args[1])); var dp = DefaultParty.Parse(File.ReadAllBytes(args[2]));
        var data = UnityEngine.JsonUtility.FromJson<HScripts>(File.ReadAllText(args[0]));
        var mods = new Dictionary<int, EclModule>();
        foreach (var om in data.modules)
        {
            var strings = new Dictionary<int, string>(); foreach (var s in om.strings) strings[s.offset] = s.text;
            mods[om.id] = EclModule.FromHex(om.id, om.bytesHex, off => { string t; return strings.TryGetValue(off, out t) ? t : null; });
        }

        // 1. module 0x10's first fight, run from its script, equals the same fight built directly
        for (int variant = 0; variant < 3; variant++)
        {
            int c3 = variant;                                   // the third group's count [9E6F]: 0 skips it
            var mem = new GenesisEclMemory(); mem.Ram[0x9E6F] = (byte)c3; mem.Ram[0x97DC] = 0xA2; mem.Ram[0x97AD] = 3; mem.Ram[0x9AFA] = 1;
            var host = NewHost(rom, mf, dp, 7 + variant, mem);
            var it = new EclInterpreter(mods[0x10], mem, host, host.X.Rng.ScriptRandom);
            var stop = it.RunFrom(0x6D3E);
            int c1 = mem.Ram[0x9E70];
            Check(stop == EclStop.Exit && host.Fights == 1, "module 10 first fight: stop " + stop + ", fights " + host.Fights);
            Check(host.Frames.Count > 2, "module 10: frames recorded " + host.Frames.Count);
            // the direct build: same party, same monsters, same configuration, same RNG
            var mem2 = new GenesisEclMemory(); mem2.Ram[0x97DC] = 0xA2; mem2.Ram[0x97AD] = 3; mem2.Ram[0x9AFA] = 1;
            var d = NewHost(rom, mf, dp, 7 + variant, mem2);
            d.X.Rng.ScriptRandom(2);                           // the script's RANDOM 2 drew one number before the monsters were loaded
            d.ClearMonsters(); d.LoadMonster(41, c1, 32); d.LoadMonster(32, 1, 32); if (c3 != 0) d.LoadMonster(39, c3, 39); d.Combat();
            bool same = d.Frames.Count == host.Frames.Count && d.LastWinner == host.LastWinner;
            for (int r = 0; same && r < 11; r++) same = Same(d.S.Records[r], host.S.Records[r]);
            for (int i = 0; same && i < d.S.SlotCount; i++) same = Same(d.S.Slots[i], host.S.Slots[i]);
            Check(same, $"module 10 fight (third group {c3}) differs from the direct build: frames {host.Frames.Count}/{d.Frames.Count}");
            Check(host.S.SlotCount == 8 + c1 + 1 + c3 || host.S.SlotCount <= 8 + c1 + 1 + c3, "module 10: slot count " + host.S.SlotCount);
            Console.WriteLine($"1. module 10 fight (groups {c1},1,{c3}): {host.Frames.Count} turns, {(host.LastWinner == 1 ? "party wins" : "monsters win")}");
        }

        // 2. TREASURE: credits and the filtered item list (ids 0, above 0x5D and the table 0x3978 are dropped)
        {
            var asm = new HAsm(); asm.Start(); asm.Emit(0x27, HAsm.W(1234), HAsm.B(5), HAsm.B(17), HAsm.B(0), HAsm.B(99), HAsm.B(1), HAsm.B(36)); asm.Emit(0x00);
            var mem = new GenesisEclMemory(); var host = NewHost(rom, mf, dp, 1, mem);
            new EclInterpreter(asm.Module(), mem, host, null).RunFrom(asm.Entry);
            Check(host.X.Credits == 1234 && host.X.PoolCount == 2 && host.X.ScriptedLoot[0] == 17 && host.X.ScriptedLoot[1] == 36, $"TREASURE: credits {host.X.Credits}, count {host.X.PoolCount}, items {host.X.ScriptedLoot[0]},{host.X.ScriptedLoot[1]}");
        }

        // 3. ADDEP: everybody present, or only the current character
        {
            var mem = new GenesisEclMemory(); var host = NewHost(rom, mf, dp, 1, mem);
            var asm = new HAsm(); asm.Start(); asm.Emit(0x41, HAsm.B(1), HAsm.W(500)); asm.Emit(0x00);
            uint before0 = Xp(host.S.Records[0]), before5 = Xp(host.S.Records[5]), before6 = Xp(host.S.Records[6]);
            new EclInterpreter(asm.Module(), mem, host, null).RunFrom(asm.Entry);
            Check(Xp(host.S.Records[0]) == before0 + 500 && Xp(host.S.Records[5]) == before5 + 500 && Xp(host.S.Records[6]) == before6, "ADDEP 1,500: all six present members, not the empty slots");
            mem.Ram[0x9DA7] = 2; asm = new HAsm(); asm.Start(); asm.Emit(0x41, HAsm.B(0), HAsm.W(70)); asm.Emit(0x00);
            uint a = Xp(host.S.Records[2]), b = Xp(host.S.Records[1]);
            new EclInterpreter(asm.Module(), mem, host, null).RunFrom(asm.Entry);
            Check(Xp(host.S.Records[2]) == a + 70 && Xp(host.S.Records[1]) == b, "ADDEP 0,70: only the current character");
        }

        // 4. a script that fights, then rewards: LOADMONSTER, TREASURE, COMBAT on a lone weak monster gives experience and credits
        {
            var mem = new GenesisEclMemory(); mem.Ram[0x97DC] = 0xA2; mem.Ram[0x97AD] = 1;
            var host = NewHost(rom, mf, dp, 3, mem);
            var asm = new HAsm(); asm.Start();
            asm.Emit(0x1C); asm.Emit(0x0B, HAsm.B(4), HAsm.B(1), HAsm.B(4)); asm.Emit(0x27, HAsm.W(300), HAsm.B(0)); asm.Emit(0x24); asm.Emit(0x00);
            uint xpBefore = Xp(host.S.Records[0]);
            var stop = new EclInterpreter(asm.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm.Entry);
            Check(stop == EclStop.Exit && host.Fights == 1 && host.Frames.Count > 1, "lone monster fight ran: " + stop + " fights " + host.Fights);
            if (host.LastWinner == 1) Check(Xp(host.S.Records[0]) > xpBefore, "victory gives experience");
            Console.WriteLine($"4. lone monster: {(host.LastWinner == 1 ? "party wins, xp " + xpBefore + " -> " + Xp(host.S.Records[0]) : "monsters win")}, {host.Frames.Count} turns");
        }

        // 5. every entry point of every module through the combat host (menus answer 0 / last, yes / no): no exception, no unknown opcode
        {
            int runs = 0, fights = 0; var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var m in mods.Values)
                for (int policy = 0; policy < 2; policy++)
                    for (int e = 0; e < 5; e++)
                    {
                        var mem = new GenesisEclMemory(); mem.Ram[0x97DC] = 0xA2; mem.Ram[0x97AD] = 2;
                        var host = NewHost(rom, mf, dp, 5 + e, mem);
                        host.MenuHandler = (w, t) => policy == 0 ? 0 : t.Length - 1; host.YesNoHandler = () => policy == 0;
                        var it = new EclInterpreter(m, mem, host, host.X.Rng.ScriptRandom) { MaxSteps = 5000 };
                        EclStop stop;
                        try { stop = it.RunEntry(e); }
                        catch (Exception ex) { Check(false, $"module {m.Id:X2} entry {e} policy {policy}: exception {ex.GetType().Name}: {ex.Message}"); continue; }
                        runs++; fights += host.Fights;
                        Check(stop != EclStop.UnknownOpcode && stop != EclStop.LastByte && stop != EclStop.BadAddress, $"module {m.Id:X2} entry {e} policy {policy}: stop {stop} {string.Join(";", it.Log)}");
                    }
            Console.WriteLine($"5. {runs} entry runs through the combat host, {fights} fights, {sw.ElapsedMilliseconds} ms");
        }

        Console.WriteLine($"ecl host: {checks - fails}/{checks} checks passed");
        return fails == 0 ? 0 : 1;
    }

    static uint Xp(byte[] r) { return (uint)(r[0x1E] << 24 | r[0x1F] << 16 | r[0x20] << 8 | r[0x21]); }
    static bool Same(byte[] a, byte[] b) { if (a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }
}
