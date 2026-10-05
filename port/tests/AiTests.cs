using System;
using System.IO;
using BuckRogersGenesis;

[Serializable] public class AiSlot { public int f0, f1, x, y, rec, t17; }
[Serializable] public class AiCase { public int n; public AiSlot[] slots; public int[] rectypes; public string tiles, flags, lst; public int mode, cnt, actor, wave0, last0, t17; public int[] path; }
[Serializable] public class AiVectors { public AiCase[] cases; }

static class AiTests
{
    static int fails, checks;
    static void Check(bool ok, string what) { checks++; if (!ok) { fails++; if (fails < 25) Console.WriteLine("FAIL: " + what); } }
    static byte[] Hex(string h) { var b = new byte[h.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(2 * i, 2), 16); return b; }

    public static int Run(string vectors, byte[] rom)
    {
        var v = UnityEngine.JsonUtility.FromJson<AiVectors>(File.ReadAllText(vectors));
        // the direction table in the ROM must be the one the port hard-codes
        bool dirOk = true;
        for (int d = 0; d < 8; d++) { if ((sbyte)rom[0x146E0 + d] != CombatWorld.DX[d] || (sbyte)rom[0x146EB + d] != CombatWorld.DY[d]) dirOk = false; }
        Check(dirOk, "direction table at 0x146E0 / 0x146EB equals CombatWorld.DX/DY");
        int found = 0;
        foreach (var c in v.cases)
        {
            var w = new CombatWorld { Tiles = Hex(c.tiles), SlotCount = c.n, Mode = c.mode, Count506 = c.cnt };
            var tf = Hex(c.flags);                                    // index -1 is tf[0], index k is tf[k + 1]
            w.TerrainFlags = i => tf[i + 1];
            var lst = Hex(c.lst); w.Ca22 = lst;
            w.Flags0 = new int[c.n]; w.Flags1 = new int[c.n]; w.X = new int[c.n]; w.Y = new int[c.n]; w.SizeType = new int[c.n]; w.Target17 = new int[c.n];
            for (int i = 0; i < c.n; i++) { var s = c.slots[i]; w.Flags0[i] = s.f0; w.Flags1[i] = s.f1; w.X[i] = s.x; w.Y[i] = s.y; w.SizeType[i] = c.rectypes[s.rec]; w.Target17[i] = s.t17; }
            var r = GenesisAi.FindPath(w, c.actor, c.wave0, c.last0);
            string ctx = $"actor={c.actor} mode={c.mode} slots={c.n}";
            bool same = r.Path.Count == c.path.Length;
            for (int i = 0; same && i < c.path.Length; i++) if (r.Path[i] != c.path[i]) same = false;
            Check(same, $"path [{string.Join(",", r.Path)}] vs ROM [{string.Join(",", c.path)}] ({ctx})");
            Check(w.Target17[c.actor] == c.t17, $"target {w.Target17[c.actor]} vs ROM {c.t17} ({ctx})");
            if (c.path.Length > 0) found++;
        }
        Console.WriteLine($"pathfinding: {checks - fails}/{checks} checks passed ({found} of {v.cases.Length} cases found a goal)");
        return fails == 0 ? 0 : 1;
    }
}
