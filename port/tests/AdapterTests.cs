using System;
using System.IO;
using BuckRogersGenesis;

static class AdapterTests
{
    public static int Run(string monsterFile, byte[] romBytes)
    {
        var rom = RomView.FromRom(romBytes);
        var file = MonsterFile.Parse(File.ReadAllBytes(monsterFile));
        int fails = 0, fights = 0, frames = 0, party = 0, totalAttacks = 0;
        var setups = new[] { new[] { 0, 1, 2 }, new[] { 4, 5 }, new[] { 1 } };
        for (int seed = 1; seed <= 12; seed++)
        {
            var pids = setups[seed % 3]; var mids = new[] { 4 + seed % 5, 10 + seed % 3 };
            var a = AutoBattle.Run(rom, file, pids, mids, 1 + seed % 3, seed, seed % 11, out int w1);
            var b = AutoBattle.Run(rom, file, pids, mids, 1 + seed % 3, seed, seed % 11, out int w2);
            fights++; frames += a.Count; if (w1 == 1) party++;
            int attacks = 0;
            for (int k = 0; k + 1 < a.Count; k++)
            {
                var seq = new BattleSequence(a[k], a[k + 1]);
                if (seq.Duration <= 0) { fails++; Console.WriteLine("FAIL: sequence duration"); }
                if (seq.Attack) attacks++;
                for (int i = 0; i < a[k].X.Length && i < a[k + 1].X.Length; i++)
                {
                    seq.Sample(seq.Duration, i, out float ex, out float ey, out int ef, out bool em, out bool ev, out int eh);
                    var nb = a[k + 1];
                    if (ex != nb.X[i] || ey != nb.Y[i] || eh != nb.Hp[i] || ef != TokenFrames.Idle(nb.Facing[i], (nb.Status[i] & 0x80) != 0, out bool m2) && ev) { fails++; Console.WriteLine($"FAIL: sequence end state creature {i} (fight seed {seed}, turn {k})"); break; }
                    seq.Sample(0, i, out ex, out ey, out ef, out em, out ev, out eh);
                    if (ex != a[k].X[i] || ey != a[k].Y[i]) { fails++; Console.WriteLine("FAIL: sequence start position"); break; }
                }
            }
            totalAttacks += attacks;
            var last = a[a.Count - 1];
            for (int i = 0; i < last.Id.Length; i++) if (last.Size[i] < 1 || last.Size[i] > 3 || last.Facing[i] > 7) { fails++; Console.WriteLine("FAIL: adapter size/facing"); }
            bool ok = a.Count > 1 && a.Count == b.Count && w1 == w2 && last.Tiles.Length == 441;
            int alive0 = 0, alive1 = 0;
            for (int i = 0; i < last.Hp.Length; i++) if (last.Status[i] != 0 && (last.Status[i] & 0xC0) == 0) { if (last.Side[i] == 1) alive1++; else alive0++; }
            ok = ok && (alive0 == 0 || alive1 == 0) && (alive1 > 0) == (w1 == 1);
            if (!ok) { fails++; Console.WriteLine($"FAIL: adapter fight seed {seed}: frames {a.Count}/{b.Count} winner {w1}/{w2} alive {alive0}/{alive1}"); }
        }
        {   // the attack-animation tables, as measured in the ROM
            int[] aim = { 3, 4, 4, 4, 5, 4, 4, 4 };
            for (int f = 0; f < 8; f++) if (TokenFrames.Aim(f, out bool m) != aim[f] || m != (f >= 5)) { fails++; Console.WriteLine("FAIL: aim frame facing " + f); }
            if (TokenFrames.Death(2).Length != 4 || TokenFrames.Death(0).Length != 2 || TokenFrames.DeathTicks(1) != 20 || TokenFrames.DeathTicks(3) != 35) { fails++; Console.WriteLine("FAIL: death table"); }
        }
        {   // frame pixel sizes
            TokenFrames.FramePixels(1, out int w1, out int h1); TokenFrames.FramePixels(2, out int w2, out int h2); TokenFrames.FramePixels(3, out int w3, out int h3);
            if (w1 != 24 || h1 != 24 || w2 != 24 || h2 != 48 || w3 != 48 || h3 != 24) { fails++; Console.WriteLine("FAIL: frame pixels"); }
        }
        {   // the idle frame table, as measured in the ROM (0xAD5A) for facings 0..7
            int[] want = { 0, 6, 6, 6, 12, 6, 6, 6 };
            for (int f = 0; f < 8; f++)
            {
                int fr = TokenFrames.Idle(f, false, out bool m);
                if (fr != want[f] || m != (f >= 5) || TokenFrames.Idle(f, true, out m) != 16) { fails++; Console.WriteLine("FAIL: token frame facing " + f); }
            }
        }
        if (totalAttacks == 0) { fails++; Console.WriteLine("FAIL: no attack found in any fight"); }
        Console.WriteLine($"adapter: {fights} fights, {frames} frames ({totalAttacks} attack sequences), party won {party}, {fails} failing");
        return fails;
    }
}
