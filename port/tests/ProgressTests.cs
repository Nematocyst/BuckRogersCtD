using System;
using System.IO;
using BuckRogersGenesis;

[Serializable] public class TrainCase { public int[] slots; public int[][] recs; public int ready; public int[] list; public int[] xp; }
[Serializable] public class AwardCase { public int[] flags; public int[] xp; public int amount; public int[] @out; }
[Serializable] public class ProgressVectors { public int[] boot_table; public int[][] hp; public int[][] attack; public TrainCase[] train; public AwardCase[] award; }

static class ProgressTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }
    static uint U(int v) { return unchecked((uint)v); }

    public static int Run(string vectors, byte[] rom)
    {
        var v = UnityEngine.JsonUtility.FromJson<ProgressVectors>(File.ReadAllText(vectors));
        var view = RomView.FromRom(rom);
        var boot = new ushort[256]; for (int i = 0; i < 256; i++) boot[i] = (ushort)v.boot_table[i];

        foreach (var c in v.hp)      // career, con, hp0, idx, hpAfter, rngIndex, rngSum
        {
            var rng = GenesisRng.FromState(boot, (byte)c[3]);
            int h = GenesisProgression.HpAfterLevelUp(view, rng, c[0], c[1], c[2]);
            long sum = 0; foreach (var w in rng.TableCopy()) sum += w;
            string ctx = $"career={c[0]} con={c[1]} hp={c[2]} idx={c[3]}";
            Check(h == c[4], $"HP after level-up {h} vs ROM {c[4]} ({ctx})");
            Check(rng.Index == c[5] && sum == c[6], $"RNG state after HP roll ({ctx})");
        }
        foreach (var c in v.attack)
            Check(GenesisProgression.AttackValue(view, c[0], c[1]) == c[2], $"attack value career={c[0]} level={c[1]}: {GenesisProgression.AttackValue(view, c[0], c[1])} vs ROM {c[2]}");

        foreach (var t in v.train)
        {
            var career = new int[8]; var level = new int[8]; var xp = new uint[8];
            for (int s = 0; s < 8; s++) { career[s] = t.recs[s][0]; level[s] = t.recs[s][1]; xp[s] = U(t.recs[s][2]); }
            var r = GenesisProgression.ScanTraining(view, t.slots, career, level, xp);
            Check(r.AnyReady == (t.ready != 0), $"training ready {r.AnyReady} vs ROM {t.ready}");
            bool same = r.NotReady.Count == t.list.Length - 1;      // the ROM list ends with 0xFF
            for (int i = 0; same && i < r.NotReady.Count; i++) if (r.NotReady[i] != t.list[i]) same = false;
            Check(same, $"training not-ready list [{string.Join(",", r.NotReady)}] vs ROM [{string.Join(",", t.list)}]");
            for (int s = 0; s < 8; s++) Check(r.Xp[s] == U(t.xp[s]), $"XP after scan slot {s}: {r.Xp[s]} vs {U(t.xp[s])}");
        }

        foreach (var a in v.award)
        {
            var xp = new uint[8]; for (int s = 0; s < 8; s++) xp[s] = U(a.xp[s]);
            var o = GenesisProgression.AwardXp(a.flags, xp, U(a.amount));
            for (int s = 0; s < 8; s++) Check(o[s] == U(a.@out[s]), $"award slot {s} flags={a.flags[s]:X}: {o[s]} vs {U(a.@out[s])}");
        }

        // the exported JSON tables give the same bytes as the ROM-backed view (checked over every exported range)
        var tj = UnityEngine.JsonUtility.FromJson<RomTablesJson>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(vectors), "rom_tables.json")));
        var starts = new int[tj.segments.Length]; var hexes = new string[tj.segments.Length];
        for (int i = 0; i < starts.Length; i++) { starts[i] = tj.segments[i].start; hexes[i] = tj.segments[i].hex; }
        var fromJson = RomView.FromSegments(starts, hexes);
        bool sameTables = true;
        foreach (var rg in RomView.DefaultRanges) for (int a = rg[0]; a < rg[1]; a++) if (fromJson.Byte(a) != view.Byte(a)) sameTables = false;
        Check(sameTables, "rom_tables.json == RomView.FromRom over all exported ranges");

        // documented examples
        Check(GenesisProgression.Threshold(view, 3, 2) == 4000 && GenesisProgression.Threshold(view, 3, 3) == 8000, "warrior: 4,000 XP to reach L3, 8,000 to reach L4");
        Check(GenesisProgression.Threshold(view, 1, 3) == 5000 && GenesisProgression.Threshold(view, 2, 3) == 6000, "rocket jock needs 5,000 to reach L4, medic 6,000");
        Check(GenesisProgression.Threshold(view, 3, 8) == 0xFFFFFFFFu, "level 8 is the cap (sentinel)");
        Check(GenesisProgression.AttackValue(view, 3, 2) == 41, "warrior level 2 attack value 41 (as in the pregenerated party)");

        Console.WriteLine($"progression: {checks - fails}/{checks} checks passed");
        return fails == 0 ? 0 : 1;
    }
}

[Serializable] public class TallyRec { public int xp, f52, cr; public int[][] gear; }
[Serializable] public class TallyRecs { public TallyRec _8, _9, _10; }
[Serializable] public class TallyCase
{
    public int[][] slots; public int mask, ba5b, m97, ba60, credits0, scripted; public TallyRecJson recs; public int[][] pool0; public int pre;
    public int xpOut, flag, credits, count; public int[][] pool; public int[][] flags5;
}
[Serializable] public class TallyRecJson { public TallyRec r8, r9, r10; }
[Serializable] public class TallyVectors { public TallyCase[] tally; }

static class TallyTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }
    static uint U(int v) { return unchecked((uint)v); }

    public static int Run(string vectors, byte[] rom)
    {
        var v = UnityEngine.JsonUtility.FromJson<TallyVectors>(File.ReadAllText(vectors).Replace("\"8\":", "\"r8\":").Replace("\"9\":", "\"r9\":").Replace("\"10\":", "\"r10\":"));
        var view = RomView.FromRom(rom);
        int hasPool = 0;
        foreach (var c in v.tally)
        {
            var recs = new RewardRecord[11];
            var src = new[] { c.recs.r8, c.recs.r9, c.recs.r10 };
            for (int i = 0; i < 3; i++)
            {
                recs[8 + i] = new RewardRecord { XpReward = src[i].xp, Flag52 = src[i].f52, Credits = U(src[i].cr), Gear = new byte[13][] };
                for (int g = 0; g < 13; g++) { recs[8 + i].Gear[g] = new byte[10]; for (int b = 0; b < 10; b++) recs[8 + i].Gear[g][b] = (byte)src[i].gear[g][b]; }
            }
            var pool = new LootPool();
            for (int k = 0; k < c.pre; k++) { var e = new byte[10]; for (int b = 0; b < 10; b++) e[b] = (byte)c.pool0[k][b]; pool.Entries.Add(e); }
            int n = c.slots.Length; var flags = new int[n]; var rid = new int[n];
            for (int i = 0; i < n; i++) { flags[i] = c.slots[i][0]; rid[i] = c.slots[i][1]; }
            // party slots may reference records 0..5 (never read for party slots); give them empty records
            for (int i = 0; i < 8; i++) if (recs[rid[i]] == null) recs[rid[i]] = new RewardRecord { Gear = new byte[13][] };
            var r = GenesisRewards.Tally(view, n, flags, rid, c.mask, recs, U(c.credits0), pool, c.ba5b != 0, c.m97 != 0, c.ba60 != 0, c.scripted);
            string ctx = $"slots={n} mask={c.mask:X} ba5b={c.ba5b} 97ae={c.m97} ba60={c.ba60}";
            Check(r.XpPerMember == U(c.xpOut), $"XP per member {r.XpPerMember} vs ROM {U(c.xpOut)} ({ctx})");
            Check(r.XpAwarded == (c.flag != 0), $"XP flag {r.XpAwarded} vs ROM {c.flag} ({ctx})");
            Check(r.Credits == U(c.credits), $"credits {r.Credits} vs ROM {U(c.credits)} ({ctx})");
            Check(pool.Count == c.count, $"loot count {pool.Count} vs ROM {c.count} ({ctx})");
            for (int k = 0; k < Math.Min(pool.Count, c.pool.Length); k++)
            {
                bool same = true; for (int b = 0; b < 10; b++) if (pool.Entries[k][b] != c.pool[k][b]) same = false;
                Check(same, $"loot entry {k}: {BitConverter.ToString(pool.Entries[k])} vs ROM {string.Join("-", c.pool[k])} ({ctx})");
            }
            for (int i = 0; i < 3; i++) for (int g = 0; g < 13; g++) Check(recs[8 + i].Gear[g][5] == c.flags5[i][g], $"gear flag byte rec {8 + i} slot {g}");
            hasPool += pool.Count;
        }
        Console.WriteLine($"victory tally: {checks - fails}/{checks} checks passed (average pool {hasPool / (double)v.tally.Length:F1} entries)");
        return fails == 0 ? 0 : 1;
    }
}

[Serializable] public class PickCase { public int[][] rows; public int d5, d6; }
[Serializable] public class RoundCase { public int[] flags0, slot, rec, after, rng; public int sur, par, d97dc, mask0, idx, mask; }
[Serializable] public class SkillCase { public int f0; public int[] rec; public int skill, shift, idx, @out; public int[] rng; }
[Serializable] public class TurnVectors { public int[] boot_table; public RoundCase[] round; public SkillCase[] skill; public PickCase[] pick; }

static class TurnTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }
    static byte[] B(int[] a) { var b = new byte[a.Length]; for (int i = 0; i < a.Length; i++) b[i] = (byte)a[i]; return b; }
    static long Sum(GenesisRng r) { long s = 0; foreach (var w in r.TableCopy()) s += w; return s; }

    public static int Run(string vectors, byte[] rom)
    {
        var v = UnityEngine.JsonUtility.FromJson<TurnVectors>(File.ReadAllText(vectors));
        var view = RomView.FromRom(rom);
        var boot = new ushort[256]; for (int i = 0; i < 256; i++) boot[i] = (ushort)v.boot_table[i];

        foreach (var c in v.skill)
        {
            var rng = GenesisRng.FromState(boot, (byte)c.idx);
            int got = GenesisSkills.SkillCheck(view, rng, B(c.rec), c.f0, c.skill, c.shift);
            string ctx = $"skill={c.skill} shift={c.shift} f0={c.f0:X} idx={c.idx}";
            Check(got == c.@out, $"skill check {got} vs ROM {c.@out} ({ctx})");
            Check(rng.Index == c.rng[0] && Sum(rng) == c.rng[1], $"RNG state after skill check ({ctx})");
        }

        foreach (var c in v.round)
        {
            var rng = GenesisRng.FromState(boot, (byte)c.idx);
            var slot = B(c.slot); var rec = B(c.rec);
            int recIndex = c.slot[2];
            int mask = GenesisTurns.BeginRound(view, rng, slot, rec, recIndex, c.flags0, c.sur, c.par, (c.d97dc & 0x10) != 0, c.mask0);
            string ctx = $"slot flags0={c.slot[0]:X} flags1={c.slot[1]:X} rec={recIndex} surprise={c.sur} idx={c.idx}";
            bool same = true; for (int i = 0; i < 26; i++) if (slot[i] != c.after[i]) same = false;
            Check(same, $"slot bytes {BitConverter.ToString(slot)} vs ROM {BitConverter.ToString(B(c.after))} ({ctx})");
            Check(mask == c.mask, $"backstab mask {mask:X} vs ROM {c.mask:X} ({ctx})");
            Check(rng.Index == c.rng[0] && Sum(rng) == c.rng[1], $"RNG state after round start ({ctx})");
        }

        foreach (var p in v.pick)
        {
            int n = p.rows.Length; var f = new int[n]; var sp = new int[n]; var tb = new int[n];
            for (int i = 0; i < n; i++) { f[i] = p.rows[i][0]; sp[i] = p.rows[i][1]; tb[i] = p.rows[i][2]; }
            int got = GenesisTurns.PickNextActor(n, f, sp, tb);
            bool romNone = p.d5 == 0xFFFF;                                   // ROM: d5 stays -1 when nobody qualifies
            Check(romNone ? got == -1 : got == p.d6, $"pick {got} vs ROM {(romNone ? -1 : p.d6)} rows={n}");
        }
        Console.WriteLine($"skills / initiative / turn order: {checks - fails}/{checks} checks passed");
        return fails == 0 ? 0 : 1;
    }
}
