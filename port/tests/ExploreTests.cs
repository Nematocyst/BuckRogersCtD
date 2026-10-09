using System;
using System.IO;
using BuckRogersGenesis;
using BuckRogersGenesis.Ecl;

/// STEPFORWARD / STEPBACK / HALFSTEP / HALFBACK / UNLOCKDOOR / HOWFAR against vectors from the ROM handlers (tools/gen_explore_vectors.py).
static class ExploreTests
{
    static int Main(string[] args)
    {
        int fails = 0, n = 0;
        foreach (var line in File.ReadAllLines(args[0]))
        {
            if (line.Length == 0) continue;
            var p = line.Split('|'); var a = p[0].Trim().Split(' '); var e = p[1].Trim().Split(' '); n++;
            char op = a[0][0]; int arg = int.Parse(a[1]); uint seed = uint.Parse(a[2]);
            var m = new GenesisEclMemory();
            uint s = seed == 0 ? 1 : seed;
            for (int i = 0; i < 1024; i++) { s ^= s << 13; s ^= s >> 17; s ^= s << 5; int v = (int)(s & 0xFF); if (i < 512 && (v & 0x30) == 0x30) v = 0; m.Ram[0xB5A4 + i] = (byte)v; }
            m.Ram[GenesisExplore.X] = (byte)int.Parse(a[3]); m.Ram[GenesisExplore.Y] = (byte)int.Parse(a[4]); m.Ram[GenesisExplore.Facing] = (byte)int.Parse(a[5]);
            m.Ram[GenesisExplore.HalfStepFlag] = (byte)int.Parse(a[6]); m.Ram[GenesisExplore.Countdown] = (byte)int.Parse(a[7]); m.Ram[GenesisExplore.ScreenKind] = (byte)int.Parse(a[8]);
            int result = 0;
            switch (op)
            {
                case 'S': GenesisExplore.StepForward(m); break;
                case 'B': GenesisExplore.StepBack(m); break;
                case 'H': GenesisExplore.HalfStep(m); break;
                case 'K': GenesisExplore.HalfBack(m); break;
                case 'U': GenesisExplore.UnlockDoor(m); break;
                case 'R':
                    {
                        uint t = seed ^ 0x5A5A; t = t == 0 ? 1 : t; var all = new int[1 + 4 * arg];
                        for (int i = 0; i < all.Length; i++) { t ^= t << 13; t ^= t >> 17; t ^= t << 5; all[i] = (int)(t & 0xFF); }
                        var vals = new int[all.Length - 1]; Array.Copy(all, 1, vals, 0, vals.Length);
                        GenesisExplore.NewRegion(m, all[0], arg, vals); break;
                    }
                case 'W': result = GenesisExplore.HowFar(m, arg); break;
            }
            int[] addr = { 0x9AF7, 0x9AF6, 0x9AFA, 0x9AF9, 0x9AF8, 0x97E6, 0x97E7, 0xBA5C, 0xB4C1, 0xB4C7 };
            string d = ""; bool ok = true;
            for (int k = 0; k < addr.Length; k++) if (m.Ram[addr[k]] != int.Parse(e[k])) { ok = false; d += $" [{addr[k]:X}]={m.Ram[addr[k]]}/{e[k]}"; }
            long sum = 0; for (int i = 0; i < 1024; i++) sum += (long)(i + 1) * m.Ram[0xB5A4 + i];
            if ((sum & 0xFFFFFFFFL) != long.Parse(e[10])) { ok = false; d += " maps"; }
            long rs = 0; for (int i = 0; i < 0x31; i++) rs += (long)(i + 1) * m.Ram[0x9BC5 + i];
            if ((rs & 0xFFFFFFFFL) != long.Parse(e[12])) { ok = false; d += " region"; }
            if (op == 'W' && result != int.Parse(e[11])) { ok = false; d += $" result {result}/{e[11]}"; }
            if (!ok) { fails++; if (fails < 10) Console.WriteLine($"FAIL: explore case {n} {line.Substring(0, line.IndexOf('|'))}:{d}"); }
        }
        Console.WriteLine($"exploration commands: {n} cases, {fails} failing");
        return fails == 0 ? 0 : 1;
    }
}
