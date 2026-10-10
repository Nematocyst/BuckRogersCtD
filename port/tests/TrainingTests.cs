using System;
using System.IO;
using System.IO.Compression;
using BuckRogersGenesis;

/// The training screen's level-up (ROM 0xCD0 with the skill and speciality picks) against vectors from the ROM routine (tools/gen_training_vectors.py).
static class TrainingTests
{
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }
    static int[] List(string s) { if (s == "-") return new int[0]; var p = s.Split(','); var r = new int[p.Length]; for (int i = 0; i < p.Length; i++) r[i] = int.Parse(p[i]); return r; }

    static int Main(string[] args)
    {
        var romBytes = File.ReadAllBytes(args[1]); var rom = RomView.FromRom(romBytes);
        string text; using (var f = new GZipStream(File.OpenRead(args[0]), CompressionMode.Decompress)) text = new StreamReader(f).ReadToEnd();
        int fails = 0, n = 0;
        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0) continue;
            var p = line.Split('|'); var h = p[0].Trim().Split(' '); n++;
            int member = int.Parse(h[0]), idx = int.Parse(h[1]), d499 = int.Parse(h[2]), d49a = int.Parse(h[3]);
            var skh = List(p[1].Trim()); var sph = List(p[2].Trim());
            var s = new CombatState { SlotCount = 8, Slots = new byte[56][], Records = new byte[11][] };
            for (int i = 0; i < 56; i++) s.Slots[i] = new byte[26];
            for (int i = 0; i < 11; i++) s.Records[i] = new byte[214];
            s.Records[member] = Hex(p[3].Trim()); s.Slots[member] = Hex(p[4].Trim());
            var rng = GenesisRng.FromState(GenesisRng.FromRom(romBytes).TableCopy(), (byte)idx);
            var x = new TurnContext { S = s, Rom = rom, Rng = rng };
            x.G[0xD499 - TurnContext.GBase] = (byte)d499; x.G[0xD49A - TurnContext.GBase] = (byte)d49a;
            int a = 0, b = 0, calls = 0;
            x.TrainMenu = entries => { calls++; if (entries == 6) return b < sph.Length ? sph[b++] : (b++ - sph.Length) % 6; return a < skh.Length ? skh[a++] : (a++ - skh.Length) % 11; };
            x.TrainLevelUp(member);
            var e = p[5].Trim().Split(' ');
            var wr = Hex(e[0]); var ws = Hex(e[1]); string d = "";
            for (int k = 0; k < 214; k++) if (s.Records[member][k] != wr[k]) { d += $" rec+{k:X}:{s.Records[member][k]:X2}/{wr[k]:X2}"; break; }
            for (int k = 0; k < 26; k++) if (s.Slots[member][k] != ws[k]) { d += $" slot+{k:X}:{s.Slots[member][k]:X2}/{ws[k]:X2}"; break; }
            long sum = 0; foreach (var w in rng.TableCopy()) sum += w;
            if (calls != int.Parse(e[2])) d += $" menu calls {calls}/{e[2]}";
            if (rng.Index != int.Parse(e[3]) || sum != long.Parse(e[4])) d += " rng";
            if (d.Length > 0) { fails++; if (fails < 8) Console.WriteLine($"FAIL: training case {n} member {member}:{d}"); }
        }
        Console.WriteLine($"training level-up: {n} cases, {fails} failing");
        return fails == 0 ? 0 : 1;
    }
}
