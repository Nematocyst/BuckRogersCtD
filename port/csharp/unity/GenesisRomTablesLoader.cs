// GenesisRomTablesLoader.cs -- Unity loader for the ROM tables the logic ports need (no ROM at run time).
// Copy rom_tables.json (made by port/tools/export_rom_tables.py) to Assets/Resources/BuckRogers/ and call GenesisRomTables.Load().
using System;
using UnityEngine;

namespace BuckRogersGenesis
{
    [Serializable] public class RomSegmentJson { public int start; public string hex; }
    [Serializable] public class RomTablesJson { public string note; public RomSegmentJson[] segments; }

    public static class GenesisRomTables
    {
        static RomView cached;
        public static RomView Load()
        {
            if (cached != null) return cached;
            var asset = Resources.Load<TextAsset>("BuckRogers/rom_tables");
            if (asset == null) { Debug.LogError("GenesisRomTables: Assets/Resources/BuckRogers/rom_tables.json not found"); return null; }
            var f = JsonUtility.FromJson<RomTablesJson>(asset.text);
            var starts = new int[f.segments.Length]; var hexes = new string[f.segments.Length];
            for (int i = 0; i < starts.Length; i++) { starts[i] = f.segments[i].start; hexes[i] = f.segments[i].hex; }
            return cached = RomView.FromSegments(starts, hexes);
        }
    }
}
