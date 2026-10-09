using System;
using System.IO;
using System.IO.Compression;
using BuckRogersGenesis;

/// FINDITEM / DESTROY / HIDEITEMS against vectors from the ROM handlers (tools/gen_scriptitems_vectors.py).
static class ScriptItemsTests
{
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }

    public static int Run(string vectors, byte[] romBytes)
    {
        var rom = RomView.FromRom(romBytes);
        int fails = 0, n = 0;
        string text; using (var f = new GZipStream(File.OpenRead(vectors), CompressionMode.Decompress)) text = new StreamReader(f).ReadToEnd();
        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0) continue;
            var p = line.Split('|'); var a = p[0].Trim().Split(' '); var pre = p[1].Trim().Split(' '); var post = p[2].Trim().Split(' '); n++;
            char op = a[0][0]; int arg = int.Parse(a[1]), mode = int.Parse(a[2]), d499 = int.Parse(a[3]), d49a = int.Parse(a[4]);
            var s = new CombatState { SlotCount = 8, Slots = new byte[56][], Records = new byte[11][] };
            for (int i = 0; i < 56; i++) s.Slots[i] = new byte[26];
            for (int i = 0; i < 11; i++) s.Records[i] = new byte[214];
            for (int i = 0; i < 8; i++) { s.Slots[i] = Hex(pre[2 * i]); s.Records[i] = Hex(pre[2 * i + 1]); }
            var x = new TurnContext { S = s, Rom = rom, Mode97AE = (byte)mode };
            x.G[0xD499 - TurnContext.GBase] = (byte)d499; x.G[0xD49A - TurnContext.GBase] = (byte)d49a;
            int flag = 0;
            if (op == 'F') flag = x.ScriptFindItem(arg) ? 1 : 2;
            else if (op == 'D') x.ScriptDestroyItem(arg);
            else x.ScriptHideItems(arg);
            bool ok = flag == int.Parse(post[0]) && x.Mode97AE == int.Parse(post[1]); string d = "";
            if (!ok) d += $" flag {flag}/{post[0]} mode {x.Mode97AE}/{post[1]}";
            for (int i = 0; i < 8; i++)
            {
                var ws = Hex(post[2 + 2 * i]); var wr = Hex(post[3 + 2 * i]);
                for (int k = 0; k < 26; k++) if (s.Slots[i][k] != ws[k]) { ok = false; d += $" slot{i}+{k:X}:{s.Slots[i][k]:X2}/{ws[k]:X2}"; break; }
                for (int k = 0; k < 214; k++) if (s.Records[i][k] != wr[k]) { ok = false; d += $" rec{i}+{k:X}:{s.Records[i][k]:X2}/{wr[k]:X2}"; break; }
            }
            if (!ok) { fails++; if (fails < 8) Console.WriteLine($"FAIL: item case {n} op {op} arg {arg} mode {mode}:{d}"); }
        }
        Console.WriteLine($"script items (FINDITEM / DESTROY / HIDEITEMS): {n} cases, {fails} failing");
        return fails;
    }
}
