using System;
using System.IO;
using BuckRogersGenesis;

[Serializable] public class ArmorCase { public int A, T; public int[] att, tgt; public int career, level, gear0, mask, armor, d496, msg, facing, turned; }
[Serializable] public class CombatVectors { public int[][] octant; public ArmorCase[] armor; public int[][] tohit; public int[][] attacks; }

static class CombatTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }

    static int Main(string[] args)
    {
        var v = UnityEngine.JsonUtility.FromJson<CombatVectors>(File.ReadAllText(args[0]));
        var rom = File.ReadAllBytes(args[1]);
        Func<int, int> weaponType = id => rom[0x779E + 8 * id + 1];

        foreach (var c in v.octant)
            Check(GenesisCombat.Octant(c[2], c[3], c[0], c[1]) == c[4], $"Octant att=({c[2]},{c[3]}) tgt=({c[0]},{c[1]}) -> {GenesisCombat.Octant(c[2], c[3], c[0], c[1])}, ROM {c[4]}");

        foreach (var c in v.armor)
        {
            // bearing exactly as the ROM calls it: d0,d1 = target x,y (+0x12,+0x13); d2,d3 = attacker x,y
            int bearing = GenesisCombat.Octant(c.att[0x12], c.att[0x13], c.tgt[0x12], c.tgt[0x13]);
            var r = GenesisCombat.ArmorAgainst(bearing, c.att[1], c.tgt[1], c.tgt[0x10], c.tgt[4], c.tgt[5], c.career, c.level, c.A, c.mask, c.gear0, weaponType);
            string ctx = $"A={c.A} T={c.T} bearing={bearing}";
            Check(r.Armor == c.armor, $"armor {r.Armor} vs ROM {c.armor} ({ctx})");
            Check(r.DamageMultiplier == c.d496, $"multiplier {r.DamageMultiplier} vs ROM {c.d496} ({ctx})");
            Check((r.Message == -1 ? 0xFFFF : r.Message) == c.msg, $"message {r.Message:X} vs ROM {c.msg:X} ({ctx})");
            Check(r.TargetFacing == c.facing, $"facing {r.TargetFacing} vs ROM {c.facing} ({ctx})");
            Check((r.TargetTurned ? 1 : 0) == c.turned, $"turned {r.TargetTurned} vs ROM {c.turned} ({ctx})");
        }

        foreach (var c in v.tohit)
        {
            var r = GenesisCombat.ToHit(c[0], c[1], c[2], c[3], c[4], c[5], c[6], c[7], c[8]);
            int expectD511 = c[9], expectMsg = c[10];
            string ctx = $"att={c[0]} arm={c[1]} dist={c[2]} rng={c[3]} wt={c[4]} mod={c[5]} f={c[6]},{c[7]},{c[8]}";
            Check(r.Computed == (expectD511 != 0xEE), $"computed={r.Computed} ROM d511={expectD511:X} ({ctx})");
            if (r.Computed) Check(r.Value == expectD511, $"value {r.Value} vs ROM {expectD511} ({ctx})");
            Check((r.Message == -1 ? 0xFFFF : r.Message) == expectMsg, $"to-hit message {r.Message:X} vs ROM {expectMsg:X} ({ctx})");
        }

        foreach (var c in v.attacks)
            Check(GenesisCombat.AttacksThisRound(c[0], c[1], c[2]) == c[3], $"attacks x2={c[0]} slot={c[1]} parity={c[2]} -> {GenesisCombat.AttacksThisRound(c[0], c[1], c[2])}, ROM {c[3]}");

        // spot checks of documented behaviour
        Check(GenesisCombat.ToHit(41, 52, 0, 0, 0, 0, 0, 0, 0).Percent == 45, "Terrine warrior (41) vs spacesuit (52): 45% as shown in the game");
        Check(GenesisCombat.ToHit(41, 62, 0, 0, 0, 0, 0, 0, 0).Percent == 5 || GenesisCombat.ToHit(41, 62, 0, 0, 0, 0, 0, 0, 0).Value == 1, "very high armor clamps to the minimum");
        Check(GenesisCombat.ToHit(60, 52, 0, 0, 0, 0, 0, 0, 0).Value == 19, "high attack clamps to 19 (95%)");

        Console.WriteLine($"{checks - fails}/{checks} checks passed");
        int dmg = args.Length > 2 ? DamageTests.Run(args[2], rom) : 0;
        int prog = args.Length > 3 ? ProgressTests.Run(args[3], rom) : 0;
        int tally = args.Length > 3 ? TallyTests.Run(args[3], rom) : 0;
        int turns = args.Length > 3 ? TurnTests.Run(args[3], rom) : 0;
        int ai = args.Length > 4 ? AiTests.Run(args[4], rom) : 0;
        int act = args.Length > 5 ? ActionTests.Run(args[5], rom) : 0;
        int mon = args.Length > 6 ? MonsterTests.Run(args[6], rom) : 0;
        int arena = args.Length > 7 ? ArenaTests.Run(args[7], rom) : 0;
        int comb = args.Length > 8 ? CombatantTests.Run(args[8], rom) : 0;
        return (fails == 0 ? 0 : 1) | dmg | prog | tally | turns | ai | act | mon | arena | comb;
    }
}

[Serializable] public class DamageCase { public int A, T, d4; public int[] slot, tgtslot; public int flags2f, gear0, m97ae, mult, tohit, idx, d6, d497, d4fc, stored, d6out, ridx; public long rsum; }
[Serializable] public class DamageVectors { public int[] boot_table; public DamageCase[] cases; }

static class DamageTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }

    public static int Run(string vectors, byte[] rom)
    {
        var v = UnityEngine.JsonUtility.FromJson<DamageVectors>(File.ReadAllText(vectors));
        Func<int, int> weaponType = id => rom[0x779E + 8 * id + 1];
        var boot = new ushort[256]; for (int i = 0; i < 256; i++) boot[i] = (ushort)v.boot_table[i];
        // the vectors start from the table the real boot loop produces: confirm our seeding gives the same one
        var seeded = GenesisRng.FromRom(rom).TableCopy(); bool same = true; for (int i = 0; i < 256; i++) if (seeded[i] != boot[i]) same = false;
        Check(same, "boot table used by the vectors equals GenesisRng.FromRom");

        int hits = 0, full = 0;
        foreach (var c in v.cases)
        {
            var rng = GenesisRng.FromState(boot, (byte)c.idx);
            bool elig = GenesisCombat.SpecialEligible(c.m97ae, c.A, c.gear0, weaponType);
            var r = GenesisCombat.ResolveAttack(rng, c.tohit, c.slot[8 + c.d4], c.slot[0xA + c.d4], c.slot[0xC + c.d4], c.mult, elig, c.flags2f);
            bool romHit = c.d6out == c.d6 + 1;
            string ctx = $"A={c.A} d4={c.d4} tohit={c.tohit} idx={c.idx}";
            Check(r.Hit == romHit, $"hit {r.Hit} vs ROM {romHit} ({ctx})");
            if (romHit) { hits++; Check(r.Damage == c.d497 && r.Damage == c.stored, $"damage {r.Damage} vs ROM {c.d497}/{c.stored} ({ctx})"); Check(r.FullDamage == (c.d4fc == 0xFF), $"full-damage {r.FullDamage} vs ROM d4fc={c.d4fc:X} ({ctx})"); if (r.FullDamage) full++; }
            long sum = 0; foreach (var w in rng.TableCopy()) sum += w;
            Check(rng.Index == c.ridx && sum == c.rsum, $"RNG state after the attack: index {rng.Index} vs {c.ridx}, table sum {sum} vs {c.rsum} ({ctx})");
        }
        Console.WriteLine($"attack resolution: {checks - fails}/{checks} checks passed ({hits} hits, {full} full-damage)");
        return fails == 0 ? 0 : 1;
    }
}
