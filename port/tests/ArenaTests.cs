using System;
using System.IO;
using BuckRogersGenesis;

static class ArenaTests
{
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }

    public static int Run(string vectors, byte[] romBytes)
    {
        var rom = RomView.FromRom(romBytes);
        int fails = 0, n = 0;
        foreach (var line in File.ReadAllLines(vectors))
        {
            if (line.Length == 0) continue;
            var f = line.Split(' ');
            var x = new TurnContext { S = new CombatState { Tiles = Hex(f[10]) }, Rom = rom };
            x.MapX = byte.Parse(f[0]); x.MapY = byte.Parse(f[1]); x.Facing = byte.Parse(f[2]); x.G[0xD4FE - TurnContext.GBase] = byte.Parse(f[3]); x.GroupMask = byte.Parse(f[4]);
            x.MapLayerA = Hex(f[5]); x.MapLayerB = Hex(f[6]); x.MapWalls = Hex(f[7]); x.TileClassA = Hex(f[8]); x.TileClassB = Hex(f[9]);
            x.BuildDungeonArena();
            var want = Hex(f[11]); n++;
            string d = ""; int bad = 0;
            for (int i = 0; i < 441; i++) if (x.S.Tiles[i] != want[i]) { if (bad++ < 6) d += $" ({i % 21},{i / 21}): {x.S.Tiles[i]:X2}/{want[i]:X2}"; }
            if (bad > 0) { fails++; if (fails < 6) Console.WriteLine($"FAIL: arena case {n}: {bad} cells differ [port/ROM]{d}"); }
            if (x.TerrainTable != 0x3297 || x.ScriptTable != 0) { fails++; Console.WriteLine("FAIL: arena tables"); }
        }
        Console.WriteLine($"arena: {n} cases, {fails} failing");
        return fails;
    }
}
