using System;
using System.IO;
using BuckRogersGenesis;

/// SKILL / PRINTSKILL against vectors from the ROM handlers (tools/gen_scriptchar_vectors.py).
static class ScriptCharTests
{
    public static int Run(string vectors, byte[] romBytes)
    {
        var rom = RomView.FromRom(romBytes);
        int fails = 0, n = 0;
        foreach (var line in File.ReadAllLines(vectors))
        {
            if (line.Length == 0) continue;
            var p = line.Split('|'); var a = p[0].Trim().Split(' '); var hex = p[1].Trim(); var e = p[2].Trim().Split(' '); n++;
            int skill = int.Parse(a[1]), who = int.Parse(a[2]), shift = int.Parse(a[3]), cur = int.Parse(a[4]), idx = int.Parse(a[5]);
            var recs = new byte[8][]; var slots = new byte[8][];
            for (int i = 0; i < 8; i++)
            {
                recs[i] = new byte[214]; slots[i] = new byte[26]; slots[i][0] = (byte)int.Parse(a[6 + i]);
                var b = new byte[21]; for (int k = 0; k < 21; k++) b[k] = Convert.ToByte(hex.Substring(42 * i + 2 * k, 2), 16);
                recs[i][0x19] = b[0]; Array.Copy(b, 1, recs[i], 0x31, 14); Array.Copy(b, 15, recs[i], 0x10, 6);
            }
            var rng = GenesisRng.FromState(GenesisRng.FromRom(romBytes).TableCopy(), (byte)idx);
            int index; int result = GenesisScriptChar.Skill(rom, rng, recs, slots, cur, skill, who, shift, out index);
            long sum = 0; foreach (var w in rng.TableCopy()) sum += w;
            bool ok = index == int.Parse(e[0]) && result == int.Parse(e[1]) && rng.Index == int.Parse(e[3]) && sum == long.Parse(e[4]);
            if (!ok) { fails++; if (fails < 8) Console.WriteLine($"FAIL: case {n} op {a[0]} skill {skill} who {who} shift {shift} cur {cur}: index {index}/{e[0]} result {result}/{e[1]} rng {rng.Index}/{e[3]}"); }
        }
        Console.WriteLine($"script character opcodes: {n} cases, {fails} failing");
        return fails;
    }
    static int Main(string[] args) { return Run(args[0], File.ReadAllBytes(args[1])) == 0 ? 0 : 1; }
}
