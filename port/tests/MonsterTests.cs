using System;
using System.IO;
using BuckRogersGenesis;

[Serializable] public class MonPre { public int n, idx, m97, d97dc; public string[] recs, slots; public string tiles, ft, g, ca; }
[Serializable] public class MonPost { public string[] slots; public string tiles, g, ca; public int ridx; public long rsum, recsum; }
[Serializable] public class MonCase { public string fn; public int a, range; public MonPre pre; public MonPost post; }
[Serializable] public class MonVectors { public int[] boot_table; public MonCase[] cases; }

static class MonsterTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }
    static bool Same(byte[] a, byte[] b) { if (a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }

    public static TurnContext Build(MonCase c, ushort[] boot, RomView rom)
    {
        var p = c.pre; var ft = Hex(p.ft);
        var s = new CombatState { SlotCount = p.n, Slots = new byte[p.n][], Records = new byte[11][], Tiles = Hex(p.tiles) };
        for (int i = 0; i < p.n; i++) s.Slots[i] = Hex(p.slots[i]);
        for (int r = 0; r < 11; r++) { var rec = new byte[214]; var h = Hex(p.recs[r]); Array.Copy(h, rec, h.Length); s.Records[r] = rec; s.RecordSizeType[r] = rec[0x23]; }
        var ctx = new TurnContext { S = s, Rng = GenesisRng.FromState(boot, (byte)p.idx), TerrainFlags = i => ft[i + 1], Rom = rom };
        ctx.Mode97AE = (byte)p.m97; ctx.D97DC = (byte)p.d97dc; Array.Copy(Hex(p.g), ctx.G, ctx.G.Length); Array.Copy(Hex(p.ca), ctx.Ca, ctx.Ca.Length);
        return ctx;
    }

    public static void Compare(TurnContext x, MonCase c, string ctx)
    {
        var q = c.post;
        for (int i = 0; i < c.pre.n; i++) Check(Same(x.S.Slots[i], Hex(q.slots[i])), $"slot {i}: {BitConverter.ToString(x.S.Slots[i])} vs ROM {BitConverter.ToString(Hex(q.slots[i]))} ({ctx})");
        Check(Same(x.S.Tiles, Hex(q.tiles)), $"tile map ({ctx})");
        Check(Same(x.G, Hex(q.g)), $"globals {BitConverter.ToString(x.G)} vs ROM {BitConverter.ToString(Hex(q.g))} ({ctx})");
        Check(Same(x.Ca, Hex(q.ca)), $"actor/target list {BitConverter.ToString(x.Ca)} vs ROM {BitConverter.ToString(Hex(q.ca))} ({ctx})");
        long sum = 0; foreach (var w in x.Rng.TableCopy()) sum += w;
        Check(x.Rng.Index == q.ridx && sum == q.rsum, $"RNG state: index {x.Rng.Index} vs {q.ridx}, sum {sum} vs {q.rsum} ({ctx})");
        long rs = 0, pos = 0; foreach (var r in x.S.Records) foreach (var b in r) { pos++; rs += pos * b; }
        Check(rs == q.recsum, $"record bytes changed: sum {rs} vs ROM {q.recsum} ({ctx})");
    }

    public static int Run(string vectors, byte[] romBytes)
    {
        var v = UnityEngine.JsonUtility.FromJson<MonVectors>(File.ReadAllText(vectors));
        var boot = new ushort[256]; for (int i = 0; i < 256; i++) boot[i] = (ushort)v.boot_table[i];
        var rom = RomView.FromRom(romBytes);
        var counts = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var c in v.cases)
        {
            var x = Build(c, boot, rom); x.Actor = c.a;
            string ctx = c.fn + " actor " + c.a;
            switch (c.fn)
            {
                case "enum": x.EnumerateTargets(c.a, c.range); break;
                case "select": x.SelectTarget(); break;
                case "weapon": x.ChooseWeapon(c.range); break;
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
