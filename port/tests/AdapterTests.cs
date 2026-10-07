using System;
using System.IO;
using BuckRogersGenesis;

static class AdapterTests
{
    public static int Run(string monsterFile, byte[] romBytes)
    {
        string partyFile = Path.Combine(Path.GetDirectoryName(monsterFile), "default_party.bytes");
        var rom = RomView.FromRom(romBytes);
        var file = MonsterBinFile.Parse(File.ReadAllBytes(monsterFile));
        int fails = 0, fights = 0, frames = 0, party = 0, totalAttacks = 0, dungeons = 0;
        var setups = new[] { new[] { 0, 1, 2 }, new[] { 4, 5 }, new[] { 1 } };
        for (int seed = 1; seed <= 12; seed++)
        {
            var pids = setups[seed % 3]; var mids = new[] { 4 + seed % 5, 10 + seed % 3 };
            var a = AutoBattle.Run(rom, file, pids, mids, 1 + seed % 3, seed, seed % 11, out int w1);
            var b = AutoBattle.Run(rom, file, pids, mids, 1 + seed % 3, seed, seed % 11, out int w2);
            if (seed <= 4) { var ind = AutoBattle.Run(rom, file, pids, mids, 1 + seed % 3, seed, seed, 5, out int w3); if (ind.Count < 2 || ind[0].Mode != 5 || a[0].Mode != 4) { fails++; Console.WriteLine("FAIL: indoor fight seed " + seed); } }
            if (seed <= 6) { var dg = AutoBattle.Run(rom, file, pids, mids, 1 + seed % 3, seed, seed, 6, out int w4); if (dg.Count > 0 && dg[0].Mode != 6) { fails++; Console.WriteLine("FAIL: dungeon fight mode"); } dungeons += dg.Count > 1 ? 1 : 0; }
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
        {   // party sheet keys: the default party fights with its own sprites
            var pk = AutoBattle.Run(rom, file, new[] { 0, 1, 2 }, new[] { 0x83, 0x85, 0x8A }, new[] { 5, 11 }, 1, 3, 1, 4, out int wp);
            if (pk.Count < 2 || pk[0].Id[0] != 0x83 || pk[0].Id[2] != 0x8A || pk[0].AnimSet[0] != 0) { fails++; Console.WriteLine("FAIL: party keys"); }
            if (TokenFrames.PregenKeys.Length != 6) { fails++; Console.WriteLine("FAIL: pregen table"); }
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
        if (dungeons == 0) { fails++; Console.WriteLine("FAIL: no dungeon fight ran"); }
        if (totalAttacks == 0) { fails++; Console.WriteLine("FAIL: no attack found in any fight"); }
        {   // the default party: real records, gear, keys; the slots stored in the blob agree with the derived stats
            var dp = DefaultParty.Parse(File.ReadAllBytes(partyFile));
            string[] names = { "FLAVIUS", "CELESTE", "PIERRE", "NICHOLE", "ROARKE", "JANELLE" };
            for (int i = 0; i < 8; i++)
            {
                if (i >= 6) { if (dp.Present(i)) { fails++; Console.WriteLine("FAIL: default party record " + i + " should be empty"); } continue; }
                var row = TokenFrames.PregenKeys[i];
                if (dp.NameOf(i) != names[i] || dp.NameOf(i) != (string)row[0] || dp.Race(i) != (int)row[1] || dp.Sex(i) != (int)row[2] || dp.Career(i) != (int)row[3] || dp.Key(i) != (int)row[4] || dp.Level(i) != 2
                    || dp.ItemIds(i).Count != 2) { fails++; Console.WriteLine("FAIL: default party member " + i + " " + dp.NameOf(i)); }
            }
            if (dp.Experience(0) != 2000 || dp.HitPoints(0) != 25 || dp.HitPoints(5) != 11) { fails++; Console.WriteLine("FAIL: default party xp / hp"); }
            // RecomputeSlot (ROM 0x6D1E) on each stored record reproduces the combat stats stored in the blob's slot (bytes 3..13)
            var s0 = new CombatState { SlotCount = 6, Slots = new byte[0][], Records = new byte[11][] };
            var x0 = new TurnContext { S = s0, Rom = rom };
            for (int i = 0; i < 6; i++)
            {
                x0.LoadPartyMember(dp.Records[i], dp.Slots[i], i);
                var calc = (byte[])dp.Slots[i].Clone(); var rec = (byte[])dp.Records[i].Clone();
                GenesisStats.RecomputeSlot(rom, calc, rec, 0, 0, false);
                for (int b = 3; b < 14; b++) if (calc[b] != dp.Slots[i][b]) { fails++; Console.WriteLine($"FAIL: {dp.NameOf(i)} slot byte {b}: recomputed {calc[b]} vs stored {dp.Slots[i][b]}"); }
            }
            int won = 0, ran = 0;
            for (int seed = 1; seed <= 6; seed++)
            {
                var fr = AutoBattle.RunDefaultParty(rom, file, dp, 3 + seed % 4, new[] { 4 + seed % 5, 10 + seed % 3 }, 1 + seed % 2, seed, seed % 11, seed % 2 == 0 ? 4 : 5, out int wd);
                if (fr.Count < 2) { fails++; Console.WriteLine("FAIL: default party fight seed " + seed); continue; }
                ran++; if (wd == 1) won++;
                bool keys = fr[0].Id[0] == 0x83 && fr[0].Side[0] == 1 && fr[0].AnimSet[0] == 0;
                if (!keys) { fails++; Console.WriteLine("FAIL: default party tokens in the frames"); }
            }
            Console.WriteLine($"adapter: default party fights {ran}, party won {won}");
        }
        Console.WriteLine($"adapter: {fights} fights, {frames} frames ({totalAttacks} attack sequences), party won {party}, {fails} failing");
        return fails;
    }
}
