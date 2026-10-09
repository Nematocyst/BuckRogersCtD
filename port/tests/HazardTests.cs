using System;
using System.IO;
using BuckRogersGenesis;

/// Race table bytes, the crew hazard (ROM 0x1956E) against vectors from the real routine, and the disarmed-fighter example of the record fields (ROM 0x6D1E).
static class HazardTests
{
    public static int Run(string vectors, byte[] romBytes, byte[] party)
    {
        int fails = 0, checks = 0;
        Action<bool, string> Check = (ok, what) => { checks++; if (!ok) { fails++; if (fails < 10) Console.WriteLine("FAIL: " + what); } };

        // the table is 5 bytes per race from 0x63B; race r (1..3) is at 0x63B + 5 * r  (race 1 = 0x640)
        for (int r = 1; r <= 3; r++)
            for (int k = 0; k < 5; k++)
                Check((sbyte)romBytes[GenesisRaces.AbilityModTable + 5 * r + k] == GenesisRaces.AbilityMods[r][k], $"ability modifier race {r} ability {k}");

        int n = 0;
        foreach (var line in File.ReadAllLines(vectors))
        {
            if (line.Length == 0) continue;
            var halves = line.Split('|'); var a = halves[0].Trim().Split(' '); var e = halves[1].Trim().Split(' '); n++;
            int slot = int.Parse(a[0]), level = int.Parse(a[1]), race = int.Parse(a[2]), sides = int.Parse(a[3]), hp = int.Parse(a[4]), idx = int.Parse(a[5]);
            var slots = new byte[8][];
            for (int i = 0; i < 8; i++) { slots[i] = new byte[26]; slots[i][0] = (byte)int.Parse(a[6 + i]); }
            slots[slot][0xE] = (byte)hp;
            var rng = GenesisRng.FromRom(romBytes);                  // same boot table as the generator; only the index differs
            rng = GenesisRng.FromState(rng.TableCopy(), (byte)idx);
            var over = new byte[8];
            bool wiped = ShipHazard.Apply(rng, sides, level, race, slots, slot, over);
            Check(wiped == (e[0] == "1"), $"case {n}: wiped {wiped} vs {e[0]}");
            Check(slots[slot][0xE] == int.Parse(e[1]), $"case {n}: hp {slots[slot][0xE]} vs {e[1]}");
            Check(slots[slot][0] == int.Parse(e[2]), $"case {n}: status {slots[slot][0]:X} vs {int.Parse(e[2]):X}");
            Check(over[slot] == int.Parse(e[3]), $"case {n}: overkill {over[slot]} vs {e[3]}");
            Check(rng.Index == int.Parse(e[4]), $"case {n}: rng index {rng.Index} vs {e[4]}");
            long sum = 0; foreach (var w in rng.TableCopy()) sum += w;
            Check(sum == long.Parse(e[5]), $"case {n}: rng table sum {sum} vs {e[5]}");
        }

        // a disarmed Flavius (desert runner, STR 17): 2 attacks (attacks x2 = 4) of 1d3 + 1 (claws) + 1 (STR 17) = +2, attack value 0x2A (one less than armed's 0x2B)
        var rom = RomView.FromRom(romBytes, new[] { new[] { 0x779E - 8 * 128, 0x779E + 8 * 128 }, new[] { 0x76C0, 0x7800 } });
        var rec = new byte[214]; Array.Copy(party, 0, rec, 0, 214);
        var s = new byte[26]; Array.Copy(party, 1712, s, 0, 26);
        Array.Clear(rec, 0xAE, 10);
        GenesisStats.RecomputeSlot(rom, s, rec, 0, 0, false);
        Check(s[6] == 4 && s[8] == 1 && s[10] == 3 && s[12] == 2 && s[3] == 0x2A, $"disarmed Flavius slot: att x2 {s[6]}, dice {s[8]}d{s[10]}+{s[12]}, attack value 0x{s[3]:X2}");
        Check(GenesisCombat.AttacksThisRound(4, 0, 0) == 2 && GenesisCombat.AttacksThisRound(4, 0, 1) == 2, "attacks x2 = 4 gives 2 attacks every round");
        Check(GenesisCombat.AttacksThisRound(1, 0, 0) == 0 && GenesisCombat.AttacksThisRound(1, 0, 1) == 1, "attacks x2 = 1 (Carlos Rioja) gives one attack every second round");
        Check(GenesisRaces.DeathSound(4, 0x3A, 0) == 0x46 && GenesisRaces.DeathSound(1, 0x32, 0) == 0x44 && GenesisRaces.DeathSound(0, 0x32, 0x1F) == 0x45 && GenesisRaces.DeathSound(0, 0x36, 0) == 0x45, "death sound");

        Console.WriteLine($"hazard / races: {checks} checks, {n} hazard cases, {fails} failing");
        return fails;
    }
    static int Main(string[] args) { return Run(args[0], File.ReadAllBytes(args[1]), File.ReadAllBytes(args[2])) == 0 ? 0 : 1; }
}
