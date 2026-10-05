// BuckRogersMaps.cs -- the 18 Genesis maps of Buck Rogers: Countdown to Doomsday, decoded from the ROM, plus the
// cell-event tables of the event scripts. Companion to BuckRogersData.cs (same namespace, same Resources folder).
//
// SETUP: copy maps.json and map_events.json into Assets/Resources/BuckRogers/ and this file under Assets/.
//   var map = BuckRogersMaps.MapForModule(0x10);              // Chicagorg
//   int wall = map.Wall(5, 13, Facing.North);                 // wall type on that side (0 = open)
//   var ev = BuckRogersMaps.SearchEventAt(0x10, 6, 13);       // what the script does on that cell
//   Debug.Log(ev.summary);
//
// Data layout (all verified against the ROM loader, see genesis_maps/ in the repo):
//   * 16 x 16 cells, index = y * 16 + x, y grows SOUTH. Facing: 0 north, 1 east, 2 south, 3 west (the scripts' [9AFA]).
//   * Each cell has a wall type (0..14, 0 = open) on all four sides; neighbouring cells agree on shared edges.
//   * lock_* = 2-bit lock state per side. Movement is blocked while bit 0 is set (value 1); UNLOCKDOOR clears it on
//     both sides of the door. Values 2 and 3 are rare (see the notes in the repo).
//   * special = plane-2 byte without bit 7, exists = bit 7 (cell inside the explorable area; void cells can still
//     carry event codes). The event code is special & 0x3F.
//   * Wall types: Wall() returns the raw type 0..14; Kind()/KindOf() classify it (Open, Wall, Door, SecretDoor, SealedDoor,
//     Barrier). The classification is inferred, see genesis_maps/WALL_TYPES.md for the evidence and confidence per type.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuckRogersGenesis
{
    public enum Facing { North = 0, East = 1, South = 2, West = 3 }

    /// Behaviour class of a wall type (see genesis_maps/WALL_TYPES.md for the evidence; all but Open are inferred
    /// from map data and script texts, not read from the engine).
    public enum WallKind
    {
        Open,        // 0: nothing there
        Wall,        // 1: solid wall
        Door,        // 2,3,4,6,7,8,9,11: passable once unlocked (6 and 7 are usually locked at the start)
        SecretDoor,  // 10: looks like a wall until the script reveals it ("concealed latch")
        SealedDoor,  // 14: drawn as a door but the scripts keep it shut ("security doors are sealed", "fused shut")
        Barrier      // 5, 12, 13: bars / windows / decoration, never needed for connectivity
    }

    // ------------------------------------------------------------------ maps.json
    [Serializable] public class MapFile { public string source; public GenesisMap[] maps; }

    [Serializable] public class GenesisMap
    {
        public int id;                      // map id = decimal LOADFILES operand (0x10 = Chicagorg)
        public string name;
        public int width, height;           // 16 x 16
        public int[] north, east, south, west;           // wall type per side, 256 entries each
        public int[] exists;                // 1 = inside the explorable area
        public int[] special;               // plane 2 without bit 7 (event code in the low 6 bits)
        public int[] lock_north, lock_east, lock_south, lock_west;   // 0..3 per side

        public static readonly int[] DX = { 0, 1, 0, -1 };           // index = (int)Facing
        public static readonly int[] DY = { -1, 0, 1, 0 };

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
        public int Index(int x, int y) => y * width + x;

        public int Wall(int x, int y, Facing f)
        {
            if (!InBounds(x, y)) return 0;
            int i = Index(x, y);
            switch (f) { case Facing.North: return north[i]; case Facing.East: return east[i]; case Facing.South: return south[i]; default: return west[i]; }
        }

        /// Lock state of the side (0 none, 1 locked, 2/3 rare variants).
        public int Lock(int x, int y, Facing f)
        {
            if (!InBounds(x, y)) return 0;
            int i = Index(x, y);
            switch (f) { case Facing.North: return lock_north[i]; case Facing.East: return lock_east[i]; case Facing.South: return lock_south[i]; default: return lock_west[i]; }
        }

        /// Movement is blocked by the lock when bit 0 is set and the value is not the special 3.
        public bool IsLocked(int x, int y, Facing f) { int l = Lock(x, y, f); return (l & 1) != 0 && l != 3; }

        public bool HasWall(int x, int y, Facing f) => Wall(x, y, f) != 0;

        /// Behaviour class of a wall type number.
        public static WallKind KindOf(int wallType)
        {
            switch (wallType)
            {
                case 0: return WallKind.Open;
                case 1: return WallKind.Wall;
                case 10: return WallKind.SecretDoor;
                case 14: return WallKind.SealedDoor;
                case 5: case 12: case 13: return WallKind.Barrier;
                default: return (wallType >= 2 && wallType <= 11) ? WallKind.Door : WallKind.Wall;
            }
        }

        public WallKind Kind(int x, int y, Facing f) => KindOf(Wall(x, y, f));

        public bool InsideArea(int x, int y) => InBounds(x, y) && exists[Index(x, y)] != 0;

        /// The cell's plane-2 byte as the scripts see it in [9AF9] (bit 7 = inside the area).
        public int CellByte(int x, int y) { int i = Index(x, y); return (exists[i] << 7) | special[i]; }

        /// Event code of a cell: plane-2 byte & 0x3F (0 = none / default).
        public int EventCode(int x, int y) => InBounds(x, y) ? special[Index(x, y)] & 0x3F : 0;

        /// The cell reached by stepping from (x,y) in direction f (no wall check).
        public bool Neighbor(int x, int y, Facing f, out int nx, out int ny)
        {
            nx = x + DX[(int)f]; ny = y + DY[(int)f];
            return InBounds(nx, ny);
        }

        /// A step is allowed when the target is on the map, the side is Open or a Door (or `passableWall` accepts its
        /// type) and the side is not locked. Walls, barriers, secret doors and sealed doors block by default; the
        /// scripts decide when those open (e.g. UNLOCKDOOR, or an event that reveals a secret door).
        public bool TryStep(int x, int y, Facing f, out int nx, out int ny, Func<int, bool> passableWall = null)
        {
            if (!Neighbor(x, y, f, out nx, out ny)) return false;
            int w = Wall(x, y, f);
            WallKind k = KindOf(w);
            bool ok = k == WallKind.Open || k == WallKind.Door || (passableWall != null && passableWall(w));
            return ok && !IsLocked(x, y, f);
        }
    }

    // ------------------------------------------------------------------ map_events.json
    [Serializable] public class EventCell { public int x, y; }

    [Serializable] public class MapEvent
    {
        public int code;            // search events: event code (-1 for step events)
        public string test;         // step events: e.g. "[9AF9]EQ130" (full plane-2 byte) or "[9E6F]EQ17" (event code)
        public int facing;          // step events: required facing (-1 = any)
        public string handler;      // script label, e.g. "L6E0D"
        public string summary;      // readable description of what the handler does
        public string[] texts;      // every string the handler can print
        public EventCell[] cells;   // where it can trigger

        public bool Covers(int x, int y) { foreach (var c in cells) if (c.x == x && c.y == y) return true; return false; }
    }

    [Serializable] public class ModuleEvents { public int module; public int map; public MapEvent[] search; public MapEvent[] step; }
    [Serializable] public class EventFile { public string note; public ModuleEvents[] modules; }

    // ------------------------------------------------------------------ loader
    public static class BuckRogersMaps
    {
        static MapFile maps; static EventFile events;

        static T Load<T>(string name)
        {
            var asset = Resources.Load<TextAsset>("BuckRogers/" + name);
            if (asset == null) { Debug.LogError($"BuckRogersMaps: Assets/Resources/BuckRogers/{name}.json not found"); return default(T); }
            return JsonUtility.FromJson<T>(asset.text);
        }

        public static GenesisMap[] Maps => (maps != null ? maps : (maps = Load<MapFile>("maps"))).maps;
        public static ModuleEvents[] Modules => (events != null ? events : (events = Load<EventFile>("map_events"))).modules;

        public static GenesisMap MapById(int id) => Array.Find(Maps, m => m.id == id);
        public static ModuleEvents EventsForModule(int moduleId) => Array.Find(Modules, m => m.module == moduleId);

        /// The map a script module explores (module 0x10 -> map 0x10; module 0x53 shares map 0x51 with module 0x51).
        public static GenesisMap MapForModule(int moduleId)
        {
            var e = EventsForModule(moduleId);
            return e == null ? MapById(moduleId) : MapById(e.map);
        }

        /// The search-handler for the cell the party stands on (the script's AND [9AF9],63 -> ONGOTO table), or null.
        /// Cells whose code the table does not cover are answered by a step event instead (see StepEventsAt).
        public static MapEvent SearchEventAt(int moduleId, int x, int y)
        {
            var e = EventsForModule(moduleId); var map = MapForModule(moduleId);
            if (e == null || map == null) return null;
            int code = map.EventCode(x, y);
            return Array.Find(e.search, s => s.code == code && s.handler != "(falls through)" && s.Covers(x, y));
        }

        /// Step events (run entry) that can fire on a cell, optionally only for one facing.
        public static List<MapEvent> StepEventsAt(int moduleId, int x, int y, Facing? facing = null)
        {
            var list = new List<MapEvent>();
            var e = EventsForModule(moduleId); if (e == null) return list;
            foreach (var s in e.step)
                if (s.Covers(x, y) && (facing == null || s.facing < 0 || s.facing == (int)facing.Value)) list.Add(s);
            return list;
        }
    }
}
