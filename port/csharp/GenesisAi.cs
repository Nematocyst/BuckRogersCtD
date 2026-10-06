// GenesisAi.cs -- the monster pathfinding / target-selection routine of the Genesis game (ROM 0x15D8A) and the occupant lookup it uses
// (ROM 0x1432E). Pure C#; verified against the real ROM routine in a 68000 emulator on random combat maps (port/tests/AiTests.cs).
//
// 0x15D8A is a breadth-first search over the 21 x 21 combat grid starting at the acting creature's cell. Each wave looks at the 8 neighbours
// in an order that depends on the wave counter (a spiral-like rotation), so ties between equally near goals are broken by that order.
// What counts as the goal depends on the mode byte [0xD505]:
//     0  the first cell whose live occupant is on the OTHER side: that creature becomes the target (written to the actor's slot byte +0x17).
//        This is how a monster picks its victim: the nearest enemy by path length through passable terrain.
//     1  the cell where the actor's current target (+0x17) lies fallen (slot flag bit 7 set): used to walk to a downed ally
//     2  a cell where one of the creatures listed at [0xCA22] (3-byte entries, [0xD506] of them) lies fallen
// Cells are passable when they hold no creature marker (bit 7 of the tile byte at 0xCACA + y*21 + x) and the terrain flag byte of the tile (table
// at [0xD810]) has bit 5 clear. Creatures of record type 2 (tall) also need the cell to the south free, type 3 (wide) the cell to the east.
// The result is the list of directions (0..7, indices into the direction table at 0x146E0) from the actor to the goal.

using System;
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    public sealed class CombatWorld
    {
        public const int Size = 21;
        public static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };   // direction table at ROM 0x146E0 (x offsets)
        public static readonly int[] DY = { -1, -1, 0, 1, 1, 1, 0, -1 };   // ... and 0x146EB (y offsets): N, NE, E, SE, S, SW, W, NW

        public byte[] Tiles = new byte[Size * Size];     // 0xCACA: bit 7 = a creature stands here, low 7 bits = terrain tile id
        public Func<int, int> TerrainFlags;              // byte at [0xD810] + index; index = (tile & 0x7F) - 2, or -1 for tiles 0 and 1
        public int SlotCount;
        public int[] Flags0, Flags1, X, Y, SizeType;     // slot bytes +0, +1, +0x12, +0x13 and the monster record byte +0x23 (2 tall, 3 wide)
        public int[] Target17;                           // slot byte +0x17 (the actor's chosen target; written by the search)
        public int Mode;                                 // [0xD505]
        public int Count506;                             // [0xD506]
        public List<List<int>> OccupantLog = new List<List<int>>();   // the occupant list each 0x1432E call wrote to [0xD5F8] (terminated by 0xFF), in call order
        public byte[] Ca22 = new byte[16];               // [0xCA22]: target list for mode 2 (only the first byte of each 3-byte entry is compared)
    }

    public static class GenesisAi
    {
        public struct Occupants { public int Live; public int Fallen; }   // 0xFF = none

        // ---------------------------------------------------------------------------------------------- occupant lookup 0x1432E
        /// Who stands on / lies on the cell: Live = first live creature whose footprint (own cell + one more for tall/wide) covers it; Fallen = a creature with
        /// bit 7 set exactly on the cell (a fallen side-0 creature counts only if nobody was found before; a side-1 creature with flags 0x83 always wins).
        public static Occupants Occupant(CombatWorld w, int x, int y)
        {
            var r = new Occupants { Live = 0xFF, Fallen = 0xFF };
            var calledList = new List<int>(); w.OccupantLog.Add(calledList);
            if ((w.Tiles[(y & 0xFFFF) * CombatWorld.Size + (x & 0xFFFF)] & 0x80) != 0)          // 0x1431A: only cells marked occupied are searched
            {
                var list = new List<int>();
                for (int s = 0; s < w.SlotCount; s++)
                {
                    int f = w.Flags0[s] & 0xFF;
                    if (f == 0 || (f & 0xC0) != 0 || (w.Flags1[s] & 4) != 0) continue;
                    int cx = w.X[s] & 0xFF, cy = w.Y[s] & 0xFF;
                    if (cx == x && cy == y) list.Add(s);
                    int t = w.SizeType[s] & 0xFF;
                    if (t == 3) { if (cx + 1 == x && cy == y) list.Add(s); }
                    else if (t == 2) { if (cx == x && cy + 1 == y) list.Add(s); }
                }
                calledList.AddRange(list);
                if (list.Count > 0) r.Live = list[0];
            }
            int d3 = 0xFF;
            for (int s = 0; s < w.SlotCount; s++)
            {
                if ((w.Flags1[s] & 4) != 0) continue;
                int f = w.Flags0[s] & 0xFF;
                if ((f & 0x80) == 0) continue;
                if ((w.X[s] & 0xFF) != x || (w.Y[s] & 0xFF) != y) continue;
                if ((w.Flags1[s] & 1) != 0 && f == 0x83) d3 = s;
                else if (d3 >= 0x80) d3 = s;
            }
            r.Fallen = d3;
            return r;
        }

        public struct PathResult
        {
            public bool Found;
            public int Target;                // the creature slot found (also stored to Target17[actor]); -1 if none
            public List<int> Path;            // directions from the actor to the goal (empty when not found)
            public byte[] Visited;            // the search's visited/parent map (441 bytes); the last byte lives in RAM at 0x6CAE
        }

        // ---------------------------------------------------------------------------------------------- pathfinding 0x15D8A
        /// waveInit: the ROM never initialises its wave counter (a stack local), so the value that happened to be on the stack is an input; it only changes
        /// the order in which neighbours are tried. lastVisitedByte: the byte just after the 440 cleared bytes of the visited map (cell 20,20); 0xFF = unvisited.
        public static PathResult FindPath(CombatWorld w, int actor, int waveInit, int lastVisitedByte)
        {
            var res = new PathResult { Target = -1, Path = new List<int>() };
            int curTarget = w.Target17[actor] & 0xFF, actorFlags = w.Flags1[actor] & 0xFF;
            var visited = new byte[CombatWorld.Size * CombatWorld.Size];
            for (int i = 0; i < 440; i++) visited[i] = 0xFF;
            visited[440] = (byte)lastVisitedByte; res.Visited = visited;
            int startX = w.X[actor] & 0xFF, startY = w.Y[actor] & 0xFF;
            int moveType = w.SizeType[actor] == 2 ? -1 : (w.SizeType[actor] == 3 ? 1 : 0);   // 0x15DD2: record +0x23
            var queue = new byte[3000]; int a4 = 0, a5 = 0;
            int d2 = startX, d3 = startY, wave = waveInit & 0xFFFF;
            bool first = true;
            while (true)
            {
                if (!first)
                {
                    if (a5 == a4) return res;                                          // queue empty: no goal reachable
                    if (a4 == 3000) a4 = 0;
                    d2 = queue[a4++]; d3 = queue[a4++];
                }
                first = false;
                wave = (wave + 1) & 0xFFFF;
                int d6 = d2, d7 = d3;
                for (int cnt = 7; cnt >= 0; cnt--)
                {
                    int d0 = (wave & 0xFF00) | (((sbyte)(wave & 0xFF)) >> 1) & 0xFF;     // asr.b #1,d0 on the low byte
                    int d4 = cnt;
                    if ((wave & 1) != 0) d4 = (-d4 - d0) & 7; else d4 = (d4 + d0) & 7;  // carry = bit shifted out
                    int nx = (d6 + CombatWorld.DX[d4]) & 0xFF; if (nx >= 21) continue;
                    int ny = (d7 + CombatWorld.DY[d4]) & 0xFF; if (ny >= 21) continue;
                    int d5 = ny * 21 + nx;
                    if ((visited[d5] & 0x80) == 0) continue;                             // already visited
                    int tile = w.Tiles[d5];
                    int found = -1;
                    bool checkPassable = false;
                    if (w.Mode == 0)
                    {
                        if ((tile & 0x80) == 0) checkPassable = true;
                        else
                        {
                            int live = Occupant(w, nx, ny).Live;
                            if ((live & 0x80) != 0) continue;
                            int f0 = w.Flags0[live] & 0xFF;
                            if (f0 == 0 || (f0 & 0xC0) != 0) continue;
                            if ((((w.Flags1[live] & 0xFF) + actorFlags) & 1) == 0) continue;     // same side
                            found = live;
                        }
                    }
                    else
                    {
                        int fallen = Occupant(w, nx, ny).Fallen;
                        if (w.Mode == 1) { if (fallen == curTarget) found = fallen; }
                        else
                        {
                            for (int e = 0; e < (w.Count506 & 0xFF); e++)       // the loop is entered through its dbra: [0xD506] entries
                                if (3 * e < w.Ca22.Length && w.Ca22[3 * e] == fallen) { found = fallen; break; }
                        }
                        if (found < 0) { if ((tile & 0x80) != 0) continue; checkPassable = true; }
                    }
                    if (found < 0)
                    {
                        if (checkPassable)
                        {
                            if ((w.TerrainFlags(TerrainIndex(tile)) & 0x20) != 0) continue;
                            if (moveType != 0)
                            {
                                int extIndex = d5 + (moveType < 0 ? 21 : 1);          // may run past the 441-byte grid on the last row: the ROM reads the RAM behind it (0 here)
                                int ext = extIndex < w.Tiles.Length ? w.Tiles[extIndex] : 0;
                                if ((ext & 0x80) != 0) continue;
                                if ((w.TerrainFlags(TerrainIndex(ext)) & 0x20) != 0) continue;
                            }
                            if (a5 == 3000) a5 = 0;
                            visited[d5] = (byte)((d4 + 4) & 7);
                            queue[a5++] = (byte)nx; queue[a5++] = (byte)ny;
                            if (a5 == a4) return res;                                      // queue overflow: gives up
                        }
                        continue;
                    }
                    // goal reached: record the target and rebuild the path from the parent directions
                    w.Target17[actor] = found;
                    res.Found = true; res.Target = found;
                    var rev = new List<int>();
                    int gd4 = (d4 + 4) & 7; rev.Add(gd4);
                    int px = nx, py = ny;
                    while (!(px == startX && py == startY))
                    {
                        px = (px + CombatWorld.DX[gd4]) & 0xFF; py = (py + CombatWorld.DY[gd4]) & 0xFF;
                        gd4 = visited[py * 21 + px]; rev.Add(gd4);        // the ROM also appends the start cell's own visited byte (usually 0xFF)
                    }
                    int n = 0;                                             // the ROM then counts entries up to the first one with bit 7 set
                    while (n < rev.Count && (rev[n] & 0x80) == 0) n++;
                    for (int i = n - 1; i >= 0; i--) res.Path.Add((rev[i] + 4) & 7);
                    return res;
                }
            }
        }

        static int TerrainIndex(int tile)
        {
            int d0 = ((tile & 0x7F) - 2) & 0xFF;                     // andi.w #$7F ; subq.b #2
            return d0 >= 0x80 ? -1 : d0;                             // bpl keep else moveq #-1
        }
    }
}
