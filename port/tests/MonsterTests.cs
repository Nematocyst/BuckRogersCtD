using System;
using System.IO;
using BuckRogersGenesis;

[Serializable] public class MonPre { public string haz, script, nav; public int n, idx, m97, d97dc, d513, mode, shop; public long money; public int[] d8ca; public string[] recs, slots; public string tiles, ft, g, ca; }
[Serializable] public class MonPost { public int[] polls, cur, mvo; public long money; public int[] d2s; public string haz; public string[] trace; public int[] waves; public string nav; public string[] slots; public string tiles, g, ca; public int[] d8ca, mv; public int ret, ridx; public long rsum, recsum; }
[Serializable] public class MonCase { public string fn; public int a, range; public int[] mv, menu, pad, qmax; public MonPre pre; public MonPost post; }
[Serializable] public class MonVectors { public int[] boot_table; public MonCase[] cases; }

static class MonsterTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }
    static string Diff(byte[] a, byte[] b, int baseAddr)
    {
        string r = ""; int n = 0;
        for (int i = 0; i < a.Length && i < b.Length; i++) if (a[i] != b[i]) { if (n++ < 8) r += $" {(baseAddr + i):X}: {a[i]:X2}/{b[i]:X2}"; }
        return r + (n > 8 ? $" (+{n - 8} more)" : "");
    }
    static void StartTrace(TurnContext x) { x.Trace0 = new System.Collections.Generic.List<string>(); x.TraceLof = Environment.GetEnvironmentVariable("TRACE_LOF") != null; if (x.TraceLof) x.Rng.Log = d => x.Trace0.Add("rng " + d); }
    static string NoLof(string t) { var tk = t.Split(' '); var o = new System.Collections.Generic.List<string>(); for (int i = 0; i < tk.Length; i++) { if (tk[i] == "lof") { i += 5; continue; } if (tk[i] == "grid") { i += 5; continue; } o.Add(tk[i]); } return string.Join(" ", o); }
    static bool Same(byte[] a, byte[] b) { if (a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }

    public static TurnContext Build(MonCase c, ushort[] boot, RomView rom)
    {
        var p = c.pre; var ft = Hex(p.ft);
        var s = new CombatState { SlotCount = p.n, Slots = new byte[p.n][], Records = new byte[11][], Tiles = Hex(p.tiles) };
        for (int i = 0; i < p.n; i++) s.Slots[i] = Hex(p.slots[i]);
        for (int r = 0; r < 11; r++) { var rec = new byte[214]; var h = Hex(p.recs[r]); Array.Copy(h, rec, h.Length); s.Records[r] = rec; s.RecordSizeType[r] = rec[0x23]; }
        var ctx = new TurnContext { S = s, Rng = GenesisRng.FromState(boot, (byte)p.idx), TerrainFlags = i => ft[i + 1], Rom = rom };
        if (!string.IsNullOrEmpty(p.haz)) Array.Copy(Hex(p.haz), ctx.Haz, 256);
        if (!string.IsNullOrEmpty(p.script)) { var sc = Hex(p.script); ctx.TileScript = i => i < sc.Length ? sc[i] : 0; }
        ctx.Mode97AE = (byte)p.m97; ctx.D97DC = (byte)p.d97dc; Array.Copy(Hex(p.g), ctx.G, ctx.G.Length); Array.Copy(Hex(p.ca), ctx.Ca, ctx.Ca.Length); Array.Copy(Hex(p.nav), ctx.Nav, ctx.Nav.Length);
        if (c.post != null && c.post.d2s != null) ctx.RangeD2 = new System.Collections.Generic.Queue<int>(c.post.d2s);
        { var q = c.post; int wi = 0; if (q != null && q.waves != null) ctx.WaveInit = () => q.waves[wi < q.waves.Length ? wi++ : q.waves.Length - 1]; }
        return ctx;
    }

    public static void Compare(TurnContext x, MonCase c, string ctx)
    {
        var q = c.post;
        for (int i = 0; i < c.pre.n; i++) Check(Same(x.S.Slots[i], Hex(q.slots[i])), $"slot {i} differs [offset: port/ROM]: {Diff(x.S.Slots[i], Hex(q.slots[i]), 0)} ({ctx})");
        Check(Same(x.S.Tiles, Hex(q.tiles)), $"tile map ({ctx})");
        {   // [0xD51A..B] is the projectile animation's sprite scratch (written when a creature is moved by a blast): not game state
            var mine = (byte[])x.G.Clone(); var theirs = Hex(q.g);
            mine[0xD51A - TurnContext.GBase] = theirs[0xD51A - TurnContext.GBase] = 0; mine[0xD51B - TurnContext.GBase] = theirs[0xD51B - TurnContext.GBase] = 0;
            mine[0xD5AC - TurnContext.GBase] = theirs[0xD5AC - TurnContext.GBase] = 0;       // [0xD5AC]: text colour of the rescue messages
            foreach (int a in new[] { 0xD594, 0xD59C, 0xD59D, 0xD59E, 0xD59F, 0xD582, 0xD583, 0xD584, 0xD585, 0xD586, 0xD587, 0xD588, 0xD589, 0xD592, 0xD595 })      // the command menu's window layout
                mine[a - TurnContext.GBase] = theirs[a - TurnContext.GBase] = 0;
            Check(Same(mine, theirs), $"globals differ [address: port/ROM]: {Diff(mine, theirs, TurnContext.GBase)} ({ctx})");
        }
        { var nv = Hex(q.nav); var mine = new byte[0x34]; var theirs = new byte[0x34]; Array.Copy(x.Nav, mine, 0x34); Array.Copy(nv, theirs, 0x34); Check(Same(mine, theirs), $"path buffer differs [address: port/ROM]: {Diff(mine, theirs, 0x6CAE)} ({ctx})"); }
        if (!string.IsNullOrEmpty(q.haz)) Check(Same(x.Haz, Hex(q.haz)), $"lingering patches differ [offset: port/ROM]: {Diff(x.Haz, Hex(q.haz), 0)} ({ctx})");
        Check(Same(x.Ca, Hex(q.ca)), $"actor/target list differs [address: port/ROM]: {Diff(x.Ca, Hex(q.ca), TurnContext.CaBase)} ({ctx})");
        if (q.d8ca != null) Check(x.S.LivingBySide[0] == q.d8ca[0] && x.S.LivingBySide[1] == q.d8ca[1], $"living counters ({ctx})");
        long sum = 0; foreach (var w in x.Rng.TableCopy()) sum += w;
        Check(x.Rng.Index == q.ridx && sum == q.rsum, $"RNG state: index {x.Rng.Index} vs {q.ridx}, sum {sum} vs {q.rsum} ({ctx})");
        long rs = 0, pos = 0; foreach (var r in x.S.Records) foreach (var b in r) { pos++; rs += pos * b; }
        Check(rs == q.recsum, $"record bytes changed: sum {rs} vs ROM {q.recsum} ({ctx})");
    }

    public static int Run(string vectors, byte[] romBytes)
    {
        string json;
        using (var fs = File.OpenRead(vectors)) using (var gz = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Decompress)) using (var rd = new StreamReader(gz)) json = rd.ReadToEnd();
        var v = UnityEngine.JsonUtility.FromJson<MonVectors>(json);
        var boot = new ushort[256]; for (int i = 0; i < 256; i++) boot[i] = (ushort)v.boot_table[i];
        var rom = RomView.FromRom(romBytes);
        var counts = new System.Collections.Generic.Dictionary<string, int>();
        int ordinal = -1;
        foreach (var c in v.cases)
        {
            ordinal++;
            var x = Build(c, boot, rom); x.Actor = c.a;
            string ctx = c.fn + " actor " + c.a;
            switch (c.fn)
            {
                case "enum": x.EnumerateTargets(c.a, c.range); break;
                case "select": x.SelectTarget(); break;
                case "weapon": x.ChooseWeapon(c.range); break;
                case "move":
                    x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1];
                    x.MoveDx = c.mv[0]; x.MoveDy = c.mv[1];
                    int r = x.MoveStep();
                    Check(r == c.post.ret, $"step result {r} vs ROM {c.post.ret} ({ctx})");
                    Check(x.MoveDx == c.post.mv[0] && x.MoveDy == c.post.mv[1], $"step after {x.MoveDx},{x.MoveDy} vs ROM {c.post.mv[0]},{c.post.mv[1]} ({ctx})");
                    break;
                case "nav": x.Navigate(); break;
                case "rescue": x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; StartTrace(x); x.AllyRescue(); break;
                case "stage": x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; x.Stage(c.range, c.mv[0]); break;
                case "beginturn": x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; StartTrace(x); x.BeginTurn();
                    { var mine = NoLof(string.Join(" ", x.Trace0)); var rom0 = NoLof(string.Join(" ", c.post.trace)); Check(mine == rom0, $"event sequence differs: port [{mine}] vs ROM [{rom0}] ({ctx})"); if (mine != rom0 && x.TraceLof) File.WriteAllText("/tmp/port_trace.txt", c.fn + " ordinal " + ordinal + "\n" + string.Join(" ", x.Trace0)); }
                    break;
                case "eb50s": { x.Trace0 = new System.Collections.Generic.List<string>(); x.TraceLof = Environment.GetEnvironmentVariable("TRACE_LOF") != null; int rv = x.AreaEval(true, c.range); Check(rv == c.post.ret, $"area score {rv} vs ROM {c.post.ret} ({ctx})"); if (rv != c.post.ret && x.TraceLof) Console.WriteLine("port: " + string.Join(" | ", x.Trace0) + "\nROM : " + string.Join(" | ", c.post.trace)); break; }
                case "eb50x": x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; x.Trace0 = new System.Collections.Generic.List<string>(); x.AreaEval(false, 0xAE); break;
                case "blast": x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; x.CursorX = c.mv[0]; x.CursorY = c.mv[1]; x.AreaAttack(); break;
                case "tick": x.S.CombatMode = c.pre.mode; x.TickHazards(); break;
                case "turn": x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; StartTrace(x); x.RunTurn();
                    { var mine = NoLof(string.Join(" ", x.Trace0)); var rom0 = NoLof(string.Join(" ", c.post.trace)); Check(mine == rom0, $"event sequence differs: port [{mine}] vs ROM [{rom0}] ({ctx})"); if (mine != rom0 && x.TraceLof) File.WriteAllText("/tmp/port_trace.txt", c.fn + " ordinal " + ordinal + "\n" + string.Join(" ", x.Trace0)); }
                    break;
                case "manual":
                    {
                        x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; StartTrace(x);
                        int mi = 0, pi = 0, menuCalls = 0, padCalls = 0;
                        x.MenuChoice = () => { menuCalls++; return mi < c.menu.Length ? (short)c.menu[mi++] : 4; };
                        x.Pad = () => { padCalls++; return pi < c.pad.Length ? c.pad[pi++] : 0x80; };
                        x.MapPixelsX = 504; x.MapPixelsY = 504;
                        x.PlayerTurn();
                        Check(menuCalls == c.post.polls[0] && padCalls == c.post.polls[1], $"input readings: menu {menuCalls} pad {padCalls} vs ROM {c.post.polls[0]} {c.post.polls[1]} ({ctx})");
                        Check(x.CursorX == c.post.cur[0] && x.CursorY == c.post.cur[1], $"cursor {x.CursorX},{x.CursorY} vs ROM {c.post.cur[0]},{c.post.cur[1]} ({ctx})");
                        Check(x.MoveDx == c.post.mvo[0] && x.MoveDy == c.post.mvo[1], $"step vector {x.MoveDx},{x.MoveDy} vs ROM {c.post.mvo[0]},{c.post.mvo[1]} ({ctx})");
                        var mine = NoLof(string.Join(" ", x.Trace0)); var rom0 = NoLof(string.Join(" ", c.post.trace));
                        Check(mine == rom0, $"event sequence differs: port [{mine}] vs ROM [{rom0}] ({ctx})");
                        if (mine != rom0 && x.TraceLof) File.WriteAllText("/tmp/port_trace.txt", c.fn + " ordinal " + ordinal + "\n" + string.Join(" ", x.Trace0));
                    }
                    break;
                case "inventory":
                    {
                        x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; x.ShopFlag = (byte)c.pre.shop; x.Money = (uint)c.pre.money;
                        int mi = 0, qi = 0;
                        x.InventoryMenu = () => { if (mi < c.menu.Length) return (short)c.menu[mi++]; mi++; return 0xE; };
                        x.AskQuantity = mx => { if (qi < c.pad.Length) { Check(mx == c.qmax[qi], $"quantity prompt maximum {mx} vs ROM {c.qmax[qi]} ({ctx})"); return c.pad[qi++]; } qi++; return 0; };
                        int si = 0; x.SheetMenu = () => si < (c.mv?.Length ?? 0) ? (short)c.mv[si++] : 0; x.ShowSkills = () => { };
                        if (c.range == 1) x.CharacterSheetScreen(); else x.InventoryScreen();
                        Check(mi == c.post.polls[0] && qi == c.post.polls[1], $"menu / quantity prompts: {mi} {qi} vs ROM {c.post.polls[0]} {c.post.polls[1]} ({ctx})");
                        Check(x.Money == (uint)c.post.money, $"money {x.Money} vs ROM {c.post.money} ({ctx})");
                    }
                    break;
                case "attack": x.S.CombatMode = c.pre.mode; x.S.LivingBySide[0] = c.pre.d8ca[0]; x.S.LivingBySide[1] = c.pre.d8ca[1]; x.ExecuteAttack(); break;
                default: Check(false, "unknown case " + c.fn); continue;
            }
            Compare(x, c, ctx);
            int k; counts.TryGetValue(c.fn, out k); counts[c.fn] = k + 1;
        }
        string sm = ""; foreach (var kv in counts) sm += $" {kv.Key}={kv.Value}";
        Console.WriteLine($"monster turn: {checks - fails}/{checks} checks passed ({sm.Trim()})");
        return fails == 0 ? 0 : 1;
    }
}
