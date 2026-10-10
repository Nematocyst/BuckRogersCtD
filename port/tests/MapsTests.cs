using System;
using System.IO;
using BuckRogersGenesis;
using BuckRogersGenesis.Ecl;

/// Map loading (LOADFILES' 0x5734, NEWREGION's 0x574E) against the ROM: decode + post-processing with random region rectangles (tools/gen_maps_vectors.py).
static class MapsTests
{
    static int Main(string[] args)
    {
        var rom = File.ReadAllBytes(args[2]);
        var maps = GenesisMaps.FromFile(args[1]);
        int fails = 0, n = 0;
        foreach (var line in File.ReadAllLines(args[0]))
        {
            if (line.Length == 0) continue;
            var p = line.Split('|'); var a = p[0].Trim().Split(' '); n++;
            char op = a[0][0]; int id = int.Parse(a[1]), area = int.Parse(a[2]), colour = int.Parse(a[3]), cnt = int.Parse(a[4]);
            var m = new GenesisEclMemory();
            maps.SetArea(x => rom[x], area);
            m.Ram[GenesisMaps.RegionColour] = (byte)colour; m.Ram[GenesisMaps.RegionCount] = (byte)cnt;
            for (int i = 0; i < 4 * cnt; i++) { int v = int.Parse(a[5 + i]) & 0xFFFF; m.Ram[GenesisMaps.RegionList + 2 * i] = (byte)(v >> 8); m.Ram[GenesisMaps.RegionList + 2 * i + 1] = (byte)v; }
            if (op == 'L') maps.Load(m, id); else { m.Ram[GenesisMaps.MapId] = (byte)id; maps.Reload(m); }
            long sum = 0; for (int i = 0; i < 1024; i++) sum += (long)(i + 1) * m.Ram[0xB5A4 + i];
            if ((sum & 0xFFFFFFFFL) != long.Parse(p[1].Trim())) { fails++; if (fails < 8) Console.WriteLine($"FAIL: map case {n}: {p[0].Trim()}"); }
        }
        Console.WriteLine($"map loading: {n} cases, {fails} failing");
        return fails == 0 ? 0 : 1;
    }
}
