using System;
using System.IO;
using BuckRogersGenesis;

static class AdapterTests
{
    public static int Run(string monsterFile, byte[] romBytes)
    {
        var rom = RomView.FromRom(romBytes);
        var file = MonsterFile.Parse(File.ReadAllBytes(monsterFile));
        int fails = 0, fights = 0, frames = 0, party = 0;
        var setups = new[] { new[] { 0, 1, 2 }, new[] { 4, 5 }, new[] { 1 } };
        for (int seed = 1; seed <= 12; seed++)
        {
            var pids = setups[seed % 3]; var mids = new[] { 4 + seed % 5, 10 + seed % 3 };
            var a = AutoBattle.Run(rom, file, pids, mids, 1 + seed % 3, seed, seed % 11, out int w1);
            var b = AutoBattle.Run(rom, file, pids, mids, 1 + seed % 3, seed, seed % 11, out int w2);
            fights++; frames += a.Count; if (w1 == 1) party++;
            var last = a[a.Count - 1];
            bool ok = a.Count > 1 && a.Count == b.Count && w1 == w2 && last.Tiles.Length == 441;
            int alive0 = 0, alive1 = 0;
            for (int i = 0; i < last.Hp.Length; i++) if (last.Status[i] != 0 && (last.Status[i] & 0xC0) == 0) { if (last.Side[i] == 1) alive1++; else alive0++; }
            ok = ok && (alive0 == 0 || alive1 == 0) && (alive1 > 0) == (w1 == 1);
            if (!ok) { fails++; Console.WriteLine($"FAIL: adapter fight seed {seed}: frames {a.Count}/{b.Count} winner {w1}/{w2} alive {alive0}/{alive1}"); }
        }
        Console.WriteLine($"adapter: {fights} fights, {frames} frames, party won {party}, {fails} failing");
        return fails;
    }
}
