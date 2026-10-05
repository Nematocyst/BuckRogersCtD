using System;
// test-only copy of the JSON shapes from csharp/unity/GenesisRomTablesLoader.cs (that file needs UnityEngine's Resources)
[Serializable] public class RomSegmentJson { public int start; public string hex; }
[Serializable] public class RomTablesJson { public string note; public RomSegmentJson[] segments; }
