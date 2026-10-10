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

        // 6. character opcodes: LOADCHARACTER, SKILL (best of the party and the current character), ADDNPC
        {
            for (int who = 0; who < 2; who++)
            {
                var mem = new GenesisEclMemory(); mem.Ram[0x9E70] = (byte)who; mem.Ram[0x9E71] = 2;
                var host = NewHost(rom, mf, dp, 11, mem);
                var asm = new HAsm(); asm.Start();
                asm.Emit(0x0A, HAsm.B(2)); asm.Emit(0x22, HAsm.B(5), HAsm.M(0x9E70), HAsm.M(0x9E71)); asm.Emit(0x44); asm.Emit(0x00);
                var recs = new byte[8][]; var slots = new byte[8][]; for (int k = 0; k < 8; k++) { recs[k] = (byte[])host.S.Records[k].Clone(); slots[k] = (byte[])host.S.Slots[k].Clone(); }
                var rng2 = GenesisRng.FromState(host.X.Rng.TableCopy(), host.X.Rng.Index); int expIndex;
                int expResult = GenesisScriptChar.Skill(rom, rng2, recs, slots, 2, 5, who, 2, out expIndex);
                var stop = new EclInterpreter(asm.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm.Entry);
                Check(stop == EclStop.Exit && mem.Ram[0x9DA7] == 2, "LOADCHARACTER sets [9DA7]");
                Check(mem.Ram[0x9E71] == expResult && mem.Ram[0x9E70] == expIndex && host.X.Rng.Index == rng2.Index, $"SKILL who={who}: result {mem.Ram[0x9E71]}/{expResult} index {mem.Ram[0x9E70]}/{expIndex}");
            }
            {
                var mem = new GenesisEclMemory(); var host = NewHost(rom, mf, dp, 12, mem);
                var asm = new HAsm(); asm.Start();
                asm.Emit(0x2E, HAsm.B(0xE0), HAsm.B(1), HAsm.B(4), HAsm.B(2), HAsm.B(0)); asm.Emit(0x00);                      // every member takes 1d4+2, no save
                int hp0 = host.S.Slots[0][0xE];
                var stop = new EclInterpreter(asm.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm.Entry);
                Check(stop == EclStop.Exit && host.S.Slots[0][0xE] < hp0 && host.S.Slots[0][0xE] >= hp0 - 6 && !host.PartyDown, "DAMAGE hurts the party through the host");
                var asm2 = new HAsm(); asm2.Start(); asm2.Emit(0x2E, HAsm.B(0xE0), HAsm.B(10), HAsm.B(100), HAsm.B(255), HAsm.B(0)); asm2.Emit(0x00);   // overwhelming: party down
                new EclInterpreter(asm2.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm2.Entry);
                Check(host.PartyDown && mem.Ram[0xBA53] == 1, "DAMAGE that leaves nobody standing reports the game over");
            }
            {
                var mem = new GenesisEclMemory(); var host = NewHost(rom, mf, dp, 13, mem); host.WhoHandler = () => 4;
                var asm = new HAsm(); asm.Start();
                asm.Emit(0x39, HAsm.B(0)); asm.Emit(0x32, HAsm.B(host.S.Records[0][0x54])); asm.Emit(0x00);                    // WHO -> member 4; FINDITEM of the first member's first item
                var it = new EclInterpreter(asm.Module(), mem, host, host.X.Rng.ScriptRandom);
                var stop = it.RunFrom(asm.Entry);
                Check(stop == EclStop.Exit && mem.Ram[0x9DA7] == 4 && (it.Flags & EclInterpreter.FlagEq) != 0, "WHO sets the current character; FINDITEM finds a carried item");
                int id = host.S.Records[0][0x54], q = host.S.Records[0][0x54 + 8];
                var asm2 = new HAsm(); asm2.Start(); asm2.Emit(0x40, HAsm.B(0), HAsm.B(id)); asm2.Emit(0x00);
                new EclInterpreter(asm2.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm2.Entry);
                Check(q > 1 ? host.S.Records[0][0x54 + 8] == q - 1 : host.S.Records[0][0x54] != id || host.S.Records[1][0x54] == id, "DESTROY removes one item");
            }
            {
                var mem = new GenesisEclMemory(); var host = NewHost(rom, mf, dp, 14, mem);
                mem.Ram[GenesisExplore.X] = 5; mem.Ram[GenesisExplore.Y] = 5; mem.Ram[GenesisExplore.Facing] = 1;             // open ground, facing east
                var asm = new HAsm(); asm.Start(); asm.Emit(0x50); asm.Emit(0x45, HAsm.M(0x9E70), HAsm.B(1)); asm.Emit(0x00);
                new EclInterpreter(asm.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm.Entry);
                Check(mem.Ram[GenesisExplore.X] == 6 && mem.Ram[GenesisExplore.Y] == 5 && mem.Ram[0x97E6] == 5 && mem.Ram[0x9E70] == 2, "STEPFORWARD moves east; HOWFAR sees open squares");
            }
            Console.WriteLine("6. character opcodes through the host");
        }

        // 7. the session driver: Boot, NEWECL between modules (variables cleared, the new module's init runs), Tick runs the "run" entry
        {
            Func<int, byte[], byte[], EclModule> Build = (id, init, run) =>
            {
                var b = new List<byte>(); int baseA = EclFormat.Base;
                int runAt = baseA + 21, initAt = runAt + run.Length;
                int[] targets = { runAt, baseA + 20, baseA + 20, baseA + 20, initAt };
                foreach (var t in targets) { b.Add(1); b.AddRange(HAsm.M(t)); }
                b.Add(0); b.AddRange(run); b.AddRange(init); b.AddRange(new byte[] { 0, 0, 0, 0 });
                return EclModule.FromBytes(id, b.ToArray(), null);
            };
            Func<int, int, byte[]> Save = (v, a) => { var l = new List<byte> { 9, 0, (byte)v }; l.AddRange(HAsm.M(a)); return l.ToArray(); };
            var initA = new List<byte>(); initA.AddRange(Save(5, 0x9E70)); initA.AddRange(Save(9, 0x97F6)); initA.AddRange(new byte[] { 0x20, 0, 2, 0 });         // module 1: set variables, NEWECL 2
            var initB = new List<byte>(); initB.AddRange(Save(7, 0x9E71)); initB.Add(0);                                                                     // module 2: [9E71] = 7
            var runB = new List<byte> { 4 }; runB.AddRange(HAsm.M(0x9E72)); runB.AddRange(HAsm.B(1)); runB.AddRange(HAsm.M(0x9E72)); runB.Add(0);                  // ADD [9E72], 1, [9E72]
            var m1 = Build(1, initA.ToArray(), new byte[] { 0 }); var m2 = Build(2, initB.ToArray(), runB.ToArray());
            var mem = new GenesisEclMemory(); var host = NewHost(rom, mf, dp, 15, mem);
            var sess = new EclSession(new[] { m1, m2 }, mem, host, host.X.Rng.ScriptRandom, () => host.NewEclModule, () => host.NewEclModule = -1);
            mem.Ram[0x97E8] = 1;
            int booted = sess.Boot();
            Check(booted == 1 && sess.Current.Id == 2 && mem.Ram[0xB9F0] == 2, "Boot starts module 1; its NEWECL 2 switches to module 2");
            Check(mem.Ram[0x9E70] == 0 && mem.Ram[0x97F6] == 0 && mem.Ram[0x9E71] == 7, "NEWECL cleared the scratch registers and module variables; module 2's init ran");
            Check(mem.Ram[0x97E8] == 2 && mem.Ram[0xB9F1] == 0, "after the init pass [97E8] = the current module and the changed flag is clear");
            sess.Tick(); sess.Tick();
            Check(mem.Ram[0x9E72] == 2, "each Tick runs the run entry once: " + mem.Ram[0x9E72]);
            Console.WriteLine("7. session driver");
        }

        // 7b. the shop: STORE 1 clears the monsters, loads the shop list and runs the loot screen as the buy screen; buying costs money and puts the item in the buyer's gear
        {
            var mem = new GenesisEclMemory(); var host = NewHost(rom, mf, dp, 17, mem);
            mem.Ram[0x9BD2] = 0x13; mem.Ram[0x9BD3] = 0x88; mem.Ram[0x9E63] = 16;                       // 5,000 credits, price factor 1
            var answers = new Queue<int>(new[] { 9, 0, 8, 8, 8, 8 });                                     // the first item of the shop, give it to member 0, leave
            host.X.InventoryMenu = () => answers.Count > 0 ? answers.Dequeue() : 8;
            host.X.AskQuantity = max => 1;
            int firstId = 3;                                                                                // list 1 starts with item 3
            int before = 0; for (int k = 0; k < 13; k++) if (host.S.Records[0][0x54 + 10 * k] == firstId) before++;
            var asm = new HAsm(); asm.Start(); asm.Emit(0x4B, HAsm.B(1)); asm.Emit(0x00);
            new EclInterpreter(asm.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm.Entry);
            uint money = (uint)(mem.Ram[0x9BD0] << 24 | mem.Ram[0x9BD1] << 16 | mem.Ram[0x9BD2] << 8 | mem.Ram[0x9BD3]);
            int after = 0; for (int k = 0; k < 13; k++) if (host.S.Records[0][0x54 + 10 * k] == firstId) after++;
            Check(host.StoreOpened == 1 && host.X.ShopFlag == 0 && mem.Ram[0xBA60] == 0, "STORE runs and clears the shop flag again");
            Check(money < 5000 && after == before + 1, $"buying item {firstId}: money 5000 -> {money}, copies {before} -> {after}");
            var asm3 = new HAsm(); asm3.Start(); asm3.Emit(0x38, HAsm.B(3)); asm3.Emit(0x00);
            new EclInterpreter(asm3.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm3.Entry);
            Check(mem.Ram[0xBA53] == 0xFF && host.ProgramCalled == 3, "PROGRAM 3 ends the game ([BA53] set)");
            Console.WriteLine("7b. shop through the host");
        }

        // 7c. PROGRAM 0: the training screen levels up a ready character (skill points, hit points, attack value), once, and the script goes on
        {
            var mem = new GenesisEclMemory(); var host = NewHost(rom, mf, dp, 18, mem);
            var r0 = host.S.Records[0]; int lvl0 = r0[0x19], skillsBefore = 0; for (int k = 0; k < 15; k++) skillsBefore += r0[0x31 + k];
            r0[0x1E] = 0; r0[0x1F] = 0x7F; r0[0x20] = 0xFF; r0[0x21] = 0x00;                                   // plenty of XP
            int asked = 0; int turn = 0;
            host.TrainChoose = scan => { asked++; return asked == 1 ? 0 : -1; };                                 // train the first present member once, then leave
            host.TrainMenu = entries => (turn++) % entries;
            mem.Ram[0x9D9E] = 127;
            var asm = new HAsm(); asm.Start(); asm.Emit(0x38, HAsm.B(0)); asm.Emit(0x09, HAsm.B(1), HAsm.M(0x9E70)); asm.Emit(0x00);
            var stop = new EclInterpreter(asm.Module(), mem, host, host.X.Rng.ScriptRandom).RunFrom(asm.Entry);
            int skillsAfter = 0; for (int k = 0; k < 15; k++) skillsAfter += r0[0x31 + k];
            Check(stop == EclStop.Exit && mem.Ram[0x9E70] == 1 && mem.Ram[0x9D9E] == 0, "the script goes on behind PROGRAM 0; the training flag is cleared");
            Check(r0[0x19] == lvl0 + 1 && skillsAfter > skillsBefore && asked == 2, $"training raised level {lvl0} -> {r0[0x19]} and the skills {skillsBefore} -> {skillsAfter}");
            Console.WriteLine("7c. training through the host");
        }

        // 8. the real game from its start: the session boots like the ROM does ([0xCA21] set: module 0x10, the first fight), runs for a while and stays consistent
        {
            var mem = new GenesisEclMemory(); mem.Ram[0xCA21] = 1; mem.Ram[0x97DC] = 0xA2; mem.Ram[0x97AD] = 2;
            var host = NewHost(rom, mf, dp, 16, mem); host.Maps = GenesisMaps.FromFile("ecl/data/map_layers.txt"); host.Maps.SetArea(a => rom.Byte(a), 0);
            var sess = new EclSession(mods.Values, mem, host, host.X.Rng.ScriptRandom, () => host.NewEclModule, () => host.NewEclModule = -1);
            int first = -1; int ticks = 0; bool ok = true;
            try { first = sess.Boot(); for (; ticks < 60 && sess.Tick(); ) ticks++; }
            catch (Exception ex) { ok = false; Console.WriteLine("   session exception: " + ex.GetType().Name + " " + ex.Message); }
            Check(ok && first == 0x10, $"the real game boots into module 0x10: {first:X}");
            int mapBytes = 0; for (int i = 0; i < 1024; i++) if (mem.Ram[0xB5A4 + i] != 0) mapBytes++;
            Check(mapBytes > 0 && mem.Ram[0x9BD4] == 0x10, $"LOADFILES put map 0x10 into memory: {mapBytes} non-zero layer bytes, [9BD4] = {mem.Ram[0x9BD4]:X}");
            Console.WriteLine($"8. real game: boot module {first:X2}, {ticks} ticks, {host.Fights} fights, modules {string.Join(",", sess.Log)}");
        }

        Console.WriteLine($"ecl host: {checks - fails}/{checks} checks passed");
        return fails == 0 ? 0 : 1;
    }

    static uint Xp(byte[] r) { return (uint)(r[0x1E] << 24 | r[0x1F] << 16 | r[0x20] << 8 | r[0x21]); }
    static bool Same(byte[] a, byte[] b) { if (a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }
}
