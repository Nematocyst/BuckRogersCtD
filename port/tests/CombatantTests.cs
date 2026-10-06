using System;
using System.IO;
using BuckRogersGenesis;

static class CombatantTests
{
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }
    static byte[] Part(byte[] b, int o, int n) { var r = new byte[n]; Array.Copy(b, o, r, 0, n); return r; }

    public static int Run(string vectors, byte[] romBytes)
    {
        var rom = RomView.FromRom(romBytes, new[] { new[] { 0xA802, 0xA902 }, new[] { 0x779E - 8 * 128, 0x779E + 8 * 128 }, new[] { 0x48DA, 0x48E8 }, new[] { 0x0B70, 0x0C00 }, new[] { 0x0000, 0x0010 }, new[] { 0x76C0, 0x7800 },
            new[] { 0x6670, 0x66D2 }, new[] { 0x4FFC, 0x500A } });
        int fails = 0, n = 0;
        foreach (var line in File.ReadAllLines(vectors))
        {
            if (line.Length == 0) continue;
            var f = line.Split(' '); n++;
            string kind = f[0]; int id = int.Parse(f[1]), arg = int.Parse(f[2]);
            var fileBytes = Hex(f[8].Split('/')[0]); int nf = f[8].Split('/')[1].Split(',').Length;
            var mf = new MonsterFile { Ids = new byte[nf], Records = new byte[nf][] };
            for (int i = 0; i < nf; i++) { mf.Ids[i] = fileBytes[215 * i]; mf.Records[i] = Part(fileBytes, 215 * i + 1, 214); }
            var recsB = Hex(f[9]); var slotsB = Hex(f[10]);
            var s = new CombatState { SlotCount = int.Parse(f[3]), Slots = new byte[56][], Records = new byte[11][] };
            for (int i = 0; i < 56; i++) s.Slots[i] = Part(slotsB, 26 * i, 26);
            for (int i = 0; i < 11; i++) { s.Records[i] = Part(recsB, 214 * i, 214); s.RecordSizeType[i] = s.Records[i][0x23]; }
            var x = new TurnContext { S = s, Rom = rom, Monsters = mf, Mode97AE = (byte)int.Parse(f[7]) };
            x.G[0xD49B - TurnContext.GBase] = (byte)int.Parse(f[4]);
            if (kind == "m") x.AddMonsters(id, arg); else x.AddAlly(id, arg);
            var wantRecs = Hex(f[13]); var wantSlots = Hex(f[14]);
            string d = ""; int bad = 0;
            for (int i = 0; i < 11 * 214; i++) if (s.Records[i / 214][i % 214] != wantRecs[i]) { if (bad++ < 4) d += $" rec{i / 214}+{i % 214:X}:{s.Records[i / 214][i % 214]:X2}/{wantRecs[i]:X2}"; }
            for (int i = 0; i < 56 * 26; i++) if (s.Slots[i / 26][i % 26] != wantSlots[i]) { if (bad++ < 4) d += $" slot{i / 26}+{i % 26:X}:{s.Slots[i / 26][i % 26]:X2}/{wantSlots[i]:X2}"; }
            if (s.SlotCount != int.Parse(f[11]) || x.G[0xD49B - TurnContext.GBase] != int.Parse(f[12])) { bad++; d += $" count {s.SlotCount}/{f[11]} d49b {x.G[0xD49B - TurnContext.GBase]}/{f[12]}"; }
            if (bad > 0) { fails++; if (fails < 6) Console.WriteLine($"FAIL: combatant case {n} ({kind} id {id} arg {arg} n {f[3]}): {bad} differences [port/ROM]{d}"); }
        }
        Console.WriteLine($"combatants: {n} cases, {fails} failing");
        return fails;
    }
}
