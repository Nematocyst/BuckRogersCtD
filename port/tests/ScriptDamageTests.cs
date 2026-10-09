using System;
using System.IO;
using BuckRogersGenesis;

/// DAMAGE (script opcode 0x2E) against vectors from the ROM handler (tools/gen_scriptdamage_vectors.py).
static class ScriptDamageTests
{
    public static int Run(string vectors, byte[] romBytes)
    {
        var rom = RomView.FromRom(romBytes);
        int fails = 0, n = 0;
        foreach (var line in File.ReadAllLines(vectors))
        {
            if (line.Length == 0) continue;
            var p = line.Split('|'); var a = p[0].Trim().Split(' '); var e = p[1].Trim().Split(' '); n++;
            int flags = int.Parse(a[0]), count = int.Parse(a[1]), sides = int.Parse(a[2]), bonus = int.Parse(a[3]), tgt = int.Parse(a[4]), cur = int.Parse(a[5]), idx = int.Parse(a[6]);
            var s = new CombatState { SlotCount = 8, Slots = new byte[56][], Records = new byte[11][] };
            for (int i = 0; i < 56; i++) s.Slots[i] = new byte[26];
            for (int i = 0; i < 11; i++) s.Records[i] = new byte[214];
            for (int i = 0; i < 8; i++)
            {
                s.Slots[i][0] = (byte)int.Parse(a[7 + 4 * i]); s.Slots[i][0xE] = (byte)int.Parse(a[8 + 4 * i]); s.Slots[i][4] = (byte)int.Parse(a[9 + 4 * i]); s.Slots[i][2] = (byte)i;
                s.Records[i][0x15] = (byte)int.Parse(a[10 + 4 * i]);
            }
            var rng = GenesisRng.FromState(GenesisRng.FromRom(romBytes).TableCopy(), (byte)idx);
            var x = new TurnContext { S = s, Rom = rom, Rng = rng };
            bool down; bool changed = x.ScriptDamage(flags, count, sides, bonus, tgt, cur, out down);
            string d = ""; bool ok = true;
            for (int i = 0; i < 8; i++)
                if (s.Slots[i][0] != int.Parse(e[2 * i]) || s.Slots[i][0xE] != int.Parse(e[2 * i + 1])) { ok = false; d += $" m{i}:{s.Slots[i][0]:X}/{s.Slots[i][0xE]} vs {int.Parse(e[2 * i]):X}/{e[2 * i + 1]}"; }
            long sum = 0; foreach (var w in rng.TableCopy()) sum += w;
            if (changed != (int.Parse(e[16]) != 0)) { ok = false; d += $" refresh {changed}/{e[16]}"; }
            if (down != (int.Parse(e[17]) != 0)) { ok = false; d += $" gameover {down}/{e[17]}"; }
            if (rng.Index != int.Parse(e[19]) || sum != long.Parse(e[20])) { ok = false; d += $" rng {rng.Index}/{e[19]}"; }
            if (!ok) { fails++; if (fails < 8) Console.WriteLine($"FAIL: case {n} flags {flags:X2} count {count} sides {sides} bonus {bonus} tgt {tgt:X2} cur {cur}:{d}"); }
        }
        Console.WriteLine($"script DAMAGE: {n} cases, {fails} failing");
        return fails;
    }

    /// SKILLDAMAGE (opcode 0x49) against vectors from the ROM handler (tools/gen_scriptskilldmg_vectors.py).
    public static int RunSkill(string vectors, byte[] romBytes)
    {
        var rom = RomView.FromRom(romBytes);
        int fails = 0, n = 0;
        foreach (var line in File.ReadAllLines(vectors))
        {
            if (line.Length == 0) continue;
            var p = line.Split('|'); var h = p[0].Trim().Split(' '); var e = p[9].Trim().Split(' '); n++;
            int skill = int.Parse(h[0]), who = int.Parse(h[1]), shift = int.Parse(h[2]), count = int.Parse(h[3]), sides = int.Parse(h[4]), bonus = int.Parse(h[5]), cur = int.Parse(h[6]), idx = int.Parse(h[7]);
            var s = new CombatState { SlotCount = 8, Slots = new byte[56][], Records = new byte[11][] };
            for (int i = 0; i < 56; i++) s.Slots[i] = new byte[26];
            for (int i = 0; i < 11; i++) s.Records[i] = new byte[214];
            for (int i = 0; i < 8; i++)
            {
                var f = p[1 + i].Trim().Split(' '); string hex = f[2];
                s.Slots[i][0] = (byte)int.Parse(f[0]); s.Slots[i][0xE] = (byte)int.Parse(f[1]); s.Slots[i][2] = (byte)i;
                var b = new byte[21]; for (int k = 0; k < 21; k++) b[k] = Convert.ToByte(hex.Substring(2 * k, 2), 16);
                s.Records[i][0x19] = b[0]; Array.Copy(b, 1, s.Records[i], 0x31, 14); Array.Copy(b, 15, s.Records[i], 0x10, 6);
            }
            var rng = GenesisRng.FromState(GenesisRng.FromRom(romBytes).TableCopy(), (byte)idx);
            var x = new TurnContext { S = s, Rom = rom, Rng = rng };
            bool refreshed = x.ScriptSkillDamage(skill, who, shift, count, sides, bonus, cur);
            string d = ""; bool ok = true;
            for (int i = 0; i < 8; i++)
                if (s.Slots[i][0] != int.Parse(e[2 * i]) || s.Slots[i][0xE] != int.Parse(e[2 * i + 1])) { ok = false; d += $" m{i}:{s.Slots[i][0]:X}/{s.Slots[i][0xE]} vs {int.Parse(e[2 * i]):X}/{e[2 * i + 1]}"; }
            long sum = 0; foreach (var w in rng.TableCopy()) sum += w;
            if (refreshed != (int.Parse(e[16]) != 0)) { ok = false; d += $" refresh {refreshed}/{e[16]}"; }
            if (rng.Index != int.Parse(e[17]) || sum != long.Parse(e[18])) { ok = false; d += $" rng {rng.Index}/{e[17]}"; }
            if (!ok) { fails++; if (fails < 8) Console.WriteLine($"FAIL: skilldamage case {n} skill {skill} who {who} shift {shift} cur {cur}:{d}"); }
        }
        Console.WriteLine($"script SKILLDAMAGE: {n} cases, {fails} failing");
        return fails;
    }
}
