using System;
using System.IO;
using BuckRogersGenesis;

[Serializable] public class WoundCase { public string[] slots; public int[] rectypes; public string tiles, tiles_after, after; public int v, dmg, mode, n; public int[] d8ca, d8ca_after; }
[Serializable] public class EntryCase { public string[] slots, after; public int[] rectypes, lst, d8ca, d8ca_after; public string tiles, tiles_after; public int v, idx, count, flag48, vnull, mode, n, f40, b900, e606, cae; }
[Serializable] public class LofCase { public string tiles, ft; public int x0, y0, x1, y1, rng, d504, d4ff, d504_after; public int[] @out, f; }
[Serializable] public class StatCase { public string slot, rec, slot_after; public int ridx, d499, d49a, m97, flags2f; }
[Serializable] public class PrepOut { public int d511, d518, d496, d512, d513; }
[Serializable] public class PrepCase
{
    public int n, att, tgt, m97, d4ff, d499, d49a, cx, cy, d496, d4fd; public string[] slots, recs, after_slots; public string tiles, ft; public PrepOut @out; public int[] flags2f;
}
[Serializable] public class ActionVectors { public WoundCase[] wound; public LofCase[] lof; public StatCase[] stats; public PrepCase[] prep; public EntryCase[] entry; }

static class ActionTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }
    static byte[] B(int[] a) { var b = new byte[a.Length]; for (int i = 0; i < a.Length; i++) b[i] = (byte)a[i]; return b; }

    public static int Run(string vectors, byte[] rom)
    {
        var v = UnityEngine.JsonUtility.FromJson<ActionVectors>(File.ReadAllText(vectors));
        foreach (var c in v.wound)
        {
            var st = new CombatState { SlotCount = c.n, Slots = new byte[c.n][], Tiles = Hex(c.tiles), CombatMode = c.mode };
            for (int i = 0; i < c.n; i++) st.Slots[i] = Hex(c.slots[i]);
            for (int i = 0; i < 11; i++) st.RecordSizeType[i] = c.rectypes[i];
            st.LivingBySide[0] = c.d8ca[0]; st.LivingBySide[1] = c.d8ca[1];
            st.ApplyDamage(c.v, c.dmg);
            string ctx = $"victim {c.v} dmg={c.dmg} mode={c.mode}";
            bool same = true; var a = Hex(c.after); for (int i = 0; i < 26; i++) if (st.Slots[c.v][i] != a[i]) same = false;
            Check(same, $"slot {BitConverter.ToString(st.Slots[c.v])} vs ROM {BitConverter.ToString(a)} ({ctx})");
            Check(st.LivingBySide[0] == c.d8ca_after[0] && st.LivingBySide[1] == c.d8ca_after[1], $"living counters ({ctx})");
            var ta = Hex(c.tiles_after); bool tsame = true; for (int i = 0; i < ta.Length; i++) if (st.Tiles[i] != ta[i]) tsame = false;
            Check(tsame, $"map markers ({ctx})");
        }
        foreach (var c in v.entry)
        {
            var st = new CombatState { SlotCount = c.n, Slots = new byte[c.n][], Tiles = Hex(c.tiles), CombatMode = c.mode };
            for (int i = 0; i < c.n; i++) st.Slots[i] = Hex(c.slots[i]);
            for (int i = 0; i < 11; i++) st.RecordSizeType[i] = c.rectypes[i];
            st.LivingBySide[0] = c.d8ca[0]; st.LivingBySide[1] = c.d8ca[1];
            st.ApplyDamageEntry(c.vnull != 0 ? -1 : c.v, B(c.lst), c.idx, c.count, c.flag48 != 0, out bool cue, out bool applied);
            string ctx = $"entry {c.lst[c.idx]} count={c.count} melee={c.flag48} victim {c.v} null={c.vnull}";
            bool same = true;
            for (int k = 0; k < c.n; k++) { var a = Hex(c.after[k]); for (int i = 0; i < 26; i++) if (st.Slots[k][i] != a[i]) same = false; }
            Check(same, $"slots differ from ROM ({ctx})");
            Check(st.LivingBySide[0] == c.d8ca_after[0] && st.LivingBySide[1] == c.d8ca_after[1], $"living counters ({ctx})");
            var ta = Hex(c.tiles_after); bool tsame = true; for (int i = 0; i < ta.Length; i++) if (st.Tiles[i] != ta[i]) tsame = false;
            Check(tsame, $"map markers ({ctx})");
            Check(cue == (c.b900 != 0), $"hit cue {cue} vs ROM {c.b900} ({ctx})");
            Check(applied == (c.e606 != 0), $"applied {applied} vs ROM {c.e606} ({ctx})");
        }
        foreach (var c in v.lof)
        {
            var tiles = Hex(c.tiles); var ft = Hex(c.ft);
            var r = GenesisLineOfFire.Run(tiles, i => ft[i + 1], c.x0, c.y0, c.x1, c.y1, c.rng, c.d504 != 0, c.d4ff != 0);
            string ctx = $"({c.x0},{c.y0})->({c.x1},{c.y1}) range={c.rng} d504={c.d504} d4ff={c.d4ff}";
            Check(r.Clear == (c.@out[0] != 0), $"line of fire clear {r.Clear} vs ROM {c.@out[0]} ({ctx})");
            Check(r.Distance == (short)c.@out[1], $"distance {r.Distance} vs ROM {(short)c.@out[1]} ({ctx})");
            Check(r.LastX == c.@out[2] && r.LastY == c.@out[3], $"last cell ({r.LastX},{r.LastY}) vs ROM ({c.@out[2]},{c.@out[3]}) ({ctx})");
            Check(r.Flag501 == (c.f[0] != 0) && r.Flag502 == (c.f[1] != 0) && r.Flag503 == (c.f[2] != 0), $"flags {r.Flag501}/{r.Flag502}/{r.Flag503} vs ROM {c.f[0]:X}/{c.f[1]:X}/{c.f[2]:X} ({ctx})");
        }
        var view = RomView.FromRom(rom);
        foreach (var c in v.stats)
        {
            var slot = Hex(c.slot); var rec = Hex(c.rec);
            GenesisStats.RecomputeSlot(view, slot, rec, c.d49a, c.d499, c.m97 != 0);
            var exp = Hex(c.slot_after);
            bool same = true; for (int i = 0; i < 26; i++) if (slot[i] != exp[i]) same = false;
            string ctx = $"rec={c.ridx} weapon={rec[0xAE]:X} armor={rec[0xC2]:X} 97ae={c.m97}";
            Check(same, $"slot {BitConverter.ToString(slot)} vs ROM {BitConverter.ToString(exp)} ({ctx})");
            Check(rec[0x2F] == c.flags2f, $"record flags +0x2F {rec[0x2F]:X} vs ROM {c.flags2f:X} ({ctx})");
        }
        int tails = 0, hits = 0;
        foreach (var c in v.prep)
        {
            var st = new CombatState { SlotCount = c.n, Slots = new byte[c.n][], Records = new byte[c.recs.Length][] };
            for (int i = 0; i < c.n; i++) st.Slots[i] = Hex(c.slots[i]);
            for (int i = 0; i < c.recs.Length; i++) st.Records[i] = Hex(c.recs[i]);
            st.Tiles = Hex(c.tiles);
            var ft = Hex(c.ft);
            var env = new AttackEnv { State = st, Rom = view, TerrainFlags = i => ft[i + 1], Mode97AE = c.m97, SkipBlockers = c.d4ff, SideModMonster = c.d499, SideModParty = c.d49a,
                BackstabMask = c.d4fd, CursorX = c.cx, CursorY = c.cy, DamageMultiplier = c.d496 };
            var plan = GenesisAttackPlanner.Prepare(env, c.att, c.tgt);
            string ctx = $"att={c.att} tgt={c.tgt:X} 97ae={c.m97} slots={c.n}";
            Check(plan.ToHit == (c.@out.d511 == 0xEE ? 0 : c.@out.d511), $"to-hit {plan.ToHit} vs ROM {c.@out.d511} ({ctx})");
            int romMsg = c.@out.d518 == 0xFFFF ? 0 : c.@out.d518;
            Check(plan.Message == romMsg, $"message {plan.Message:X} vs ROM {romMsg:X} ({ctx})");
            Check(env.DamageMultiplier == c.@out.d496, $"damage multiplier {env.DamageMultiplier} vs ROM {c.@out.d496} ({ctx})");
            Check(plan.Target == c.@out.d513 || !true, $"target {plan.Target:X} vs ROM {c.@out.d513:X} ({ctx})");
            if (plan.TailReached) Check(plan.Distance == c.@out.d512, $"distance {plan.Distance} vs ROM {c.@out.d512} ({ctx})");
            for (int i = 0; i < c.n; i++)
            {
                var e = Hex(c.after_slots[i]); bool same = true; for (int b = 0; b < 26; b++) if (st.Slots[i][b] != e[b]) same = false;
                Check(same, $"slot {i} after: {BitConverter.ToString(st.Slots[i])} vs ROM {BitConverter.ToString(e)} ({ctx})");
            }
            for (int r = 0; r < 11; r++) Check(st.Records[r][0x2F] == c.flags2f[r], $"record {r} flags +0x2F ({ctx})");
            if (plan.TailReached) tails++; if (plan.ToHit > 0) hits++;
        }
        Console.WriteLine($"attack preparation: {tails} reached the to-hit stage, {hits} computed a to-hit");
        Console.WriteLine($"actions: {checks - fails}/{checks} checks passed");
        return fails == 0 ? 0 : 1;
    }
}
