using System;
using System.IO;
using BuckRogersGenesis;

[Serializable] public class RngTrial { public int[] table; public int index; public int[][] calls; public int[] final; }
[Serializable] public class OneToN { public int[] table; public int index; public int[][] calls; }
[Serializable] public class RngVectors { public int seed_words_rom_offset; public int[] seed_table; public int seed_index; public RngTrial[] trials; public OneToN one_to_n; }

static class RngTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 20) Console.WriteLine("FAIL: " + what); } }
    static ushort[] U(int[] a) { var r = new ushort[a.Length]; for (int i = 0; i < a.Length; i++) r[i] = (ushort)a[i]; return r; }

    static int Main(string[] args)
    {
        var v = UnityEngine.JsonUtility.FromJson<RngVectors>(File.ReadAllText(args[0]));
        var rom = args.Length > 1 && File.Exists(args[1]) ? File.ReadAllBytes(args[1]) : null;

        // boot seeding from the ROM seed words
        if (rom != null)
        {
            var r = GenesisRng.FromRom(rom);
            var tab = r.TableCopy(); bool same = true;
            for (int i = 0; i < 256; i++) if (tab[i] != v.seed_table[i]) same = false;
            Check(same && r.Index == v.seed_index, "boot seeding from ROM words matches the emulated boot loop");
        }
        else Console.WriteLine("(no ROM given: skipping the boot seeding check)");

        // 60 random states x 30 calls with every kind of argument
        foreach (var t in v.trials)
        {
            var r = GenesisRng.FromState(U(t.table), (byte)t.index);
            foreach (var c in t.calls)
            {
                int d0 = c[0]; int expect = c[1]; int expectIdx = c[2];
                int got = r.Next(d0);
                Check(got == expect, $"Next(0x{(uint)d0:X8}) = 0x{(uint)got:X8}, ROM says 0x{(uint)expect:X8}");
                Check(r.Index == expectIdx, $"index after Next(0x{(uint)d0:X8}): {r.Index} vs {expectIdx}");
            }
            var fin = r.TableCopy(); bool same = true;
            for (int i = 0; i < 256; i++) if (fin[i] != t.final[i]) same = false;
            Check(same, "final table identical to the ROM run");
        }

        // 1..N variant
        var o = v.one_to_n; var r2 = GenesisRng.FromState(U(o.table), (byte)o.index);
        foreach (var c in o.calls)
        {
            // 0x6C8C = Next(d0) + 1 on the full 32-bit result
            int got = r2.Roll(c[0]);
            Check(got == c[1], $"Roll({c[0]}) = {got}, ROM says {c[1]}");
        }

        // documented quirks
        var q = GenesisRng.FromState(new ushort[256], 5);
        Check(q.ScriptRandom(255) == 0 && q.Index == 5, "RANDOM 255 returns 0 and does not advance");
        var q2 = GenesisRng.FromState(new ushort[256], 0);
        int x = q2.ScriptRandom(6);
        Check(x >= 0 && x <= 6 && q2.Index == 1, "RANDOM 6 is 0..6 and advances once");

        Console.WriteLine($"{checks - fails}/{checks} checks passed");
        return fails == 0 ? 0 : 1;
    }
}
