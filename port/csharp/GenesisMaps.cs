// GenesisMaps.cs -- the map preparation of the exploration engine (ROM 0x5734 / 0x574E, post-processing 0x57DA with the rectangle test 0x5948), ported from the ROM.
// A map is 1,024 bytes (ecl/data/map_layers.txt, exported by tools/export_map_layers.py from the ROM's LZW stream at 0x8FA8D): wall layer A (0xB5A4), wall layer B (0xB6A4), square attributes
// (0xB7A4) and door states (0xB8A4), 256 cells each, one nibble of walls per side (see GenesisExplore). Loading a map (LOADFILES -> 0x5734, NEWREGION -> 0x574E) decodes it into those four
// layers and runs the post-processing TWICE:
//   for each cell (visited from the last to the first) the NEWREGION rectangles ([0x9BD5] of them, at 0x9BD6 as the words x1-1, y1-1, x2+1, y2+1) decide what stays: cells strictly inside keep everything,
//   cells on a rectangle's border keep the wall nibbles that face it, cells outside lose their walls (and their attribute byte); with no rectangle everything stays. [0x9BC5] != 0 paints that wall type
//   onto the border walls. The door byte of the cell (layer 4) is then rebuilt from the wall nibbles through a 16-entry class table (nibble -> table byte + 1, where 2 counts as 1), two bits per side.
// The class table is bytes 0x10..0x1F of the area's table at 0x51836 + the word at 0x51836 + 2 * (area / 3) (ROM 0x158BA, which stores it in [0xB41E]).
using System;
using System.Collections.Generic;
using System.IO;
using BuckRogersGenesis.Ecl;

namespace BuckRogersGenesis
{
    public sealed class GenesisMaps
    {
        public const int Layers = 0xB5A4, RegionColour = 0x9BC5, RegionCount = 0x9BD5, RegionList = 0x9BD6, MapId = 0x9BD4;
        public readonly Dictionary<int, byte[]> Raw = new Dictionary<int, byte[]>();
        public byte[] ClassTable = new byte[16];                      // [0xB41E]

        /// Reads ecl/data/map_layers.txt (one line per map: id, then 2,048 hex digits).
        public static GenesisMaps FromFile(string path)
        {
            var g = new GenesisMaps();
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.Length == 0) continue;
                var p = line.Split(' '); var b = new byte[p[1].Length / 2];
                for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(p[1].Substring(2 * i, 2), 16);
                g.Raw[int.Parse(p[0])] = b;
            }
            return g;
        }

        /// ROM 0x158BA: the area's class table (area = [0x9BBE]; the word table at 0x51836 is indexed by area / 3).
        public void SetArea(Func<int, int> romByte, int area)
        {
            int w = 0x51836 + 2 * ((area & 0xFF) / 3);
            int off = (short)(romByte(w) << 8 | romByte(w + 1));
            for (int i = 0; i < 16; i++) ClassTable[i] = (byte)romByte(0x51836 + off + 0x10 + i);
        }

        /// 0x5766: the four layers of map `id` into memory (an unknown id leaves the layers alone, as the ROM only prints a message).
        public bool Decode(IEclMemory m, int id)
        {
            byte[] raw; if (!Raw.TryGetValue(id & 0xFF, out raw)) return false;
            for (int i = 0; i < 1024; i++) m.WriteByte(Layers + i, raw[i]);
            return true;
        }

        /// 0x5734 (LOADFILES with a map id below 0x7F): [0x9BD4] = id, decode, post-process twice.
        public void Load(IEclMemory m, int id) { m.WriteByte(MapId, id); Decode(m, id); PostProcess(m); PostProcess(m); }

        /// 0x574E (the end of NEWREGION): decode the current map [0x9BD4] again and post-process twice.
        public void Reload(IEclMemory m) { Decode(m, m.ReadByte(MapId)); PostProcess(m); PostProcess(m); }

        /// 0x57DA.
        public void PostProcess(IEclMemory m)
        {
            int colour = m.ReadByte(RegionColour), n = m.ReadByte(RegionCount);
            var rect = new int[Math.Max(n, 0), 4];
            for (int r = 0; r < n; r++) for (int j = 0; j < 4; j++) { int a = RegionList + 8 * r + 2 * j; rect[r, j] = (short)(m.ReadByte(a) << 8 | m.ReadByte(a + 1)); }
            for (int y = 15; y >= 0; y--)
                for (int x = 15; x >= 0; x--)
                {
                    int cell = y * 16 + x;                                        // a2 walks from layer A's last byte downwards: row 15 first
                    int a = Layers + cell, b = a + 0x100, attr = a + 0x200, door = a + 0x300;
                    int f1 = 0xFF, f2 = 0;
                    if (n > 0)
                    {
                        f1 = 0;
                        for (int r = 0; r < n; r++) Rect(x, y, rect[r, 0], rect[r, 1], rect[r, 2], rect[r, 3], ref f1, ref f2);
                    }
                    int d1 = colour;
                    if (d1 != 0)
                    {
                        int d2 = f2 & 0xFF, d0 = ((f2 << 2) ^ 0x0C) & 0xFF; d2 &= d0;
                        if ((d2 & 8) != 0)
                        {
                            m.WriteByte(a, (m.ReadByte(a) & 0xF0) | d1);
                            m.WriteByte(b + 1, (m.ReadByte(b + 1) & 0xF0) | d1);               // 0x101(a2): layer B, the next cell
                        }
                        if ((d2 & 4) != 0)
                        {
                            d1 = (d1 << 4) & 0xFF;
                            m.WriteByte(a, (m.ReadByte(a) & 0x0F) | d1);
                            m.WriteByte(a + 0xF0, (m.ReadByte(a + 0xF0) & 0x0F) | d1);         // 0xF0(a2): layer A, 15 cells on
                        }
                    }
                    int f = f1;
                    if ((f & 1) == 0) { if ((f & 2) == 0) m.WriteByte(a, 0); else m.WriteByte(a, m.ReadByte(a) & 0x0F); }
                    else if ((f & 2) == 0) m.WriteByte(a, m.ReadByte(a) & 0xF0);
                    if ((f & 4) == 0) { if ((f & 8) == 0) m.WriteByte(b, 0); else m.WriteByte(b, m.ReadByte(b) & 0x0F); }
                    else if ((f & 8) == 0) m.WriteByte(b, m.ReadByte(b) & 0xF0);
                    if ((f & 0x10) == 0) m.WriteByte(attr, 0);
                    int v = Cls(m.ReadByte(a) >> 4) | Cls(m.ReadByte(a) & 0xF) << 2 | Cls(m.ReadByte(b) >> 4) << 4 | Cls(m.ReadByte(b) & 0xF) << 6;
                    m.WriteByte(door, v & 0xFF);
                }
        }

        int Cls(int nibble) { int d = (ClassTable[nibble & 0xF] + 1) & 0xFF; return d == 2 ? 1 : d; }

        /// 0x5948: one rectangle against cell (x = d6, y = d7); the compares are signed bytes.
        static void Rect(int x, int y, int x1, int y1, int x2, int y2, ref int f1, ref int f2)
        {
            sbyte X = (sbyte)x, Y = (sbyte)y, d2 = (sbyte)x1, d3 = (sbyte)y1, d4 = (sbyte)x2, d5 = (sbyte)y2;
            if (X < d2) return;
            if (X == d2) { if (Y <= d3) return; if (Y >= d5) return; f2 |= 8; f1 |= 2; return; }
            if (Y < d3) return;
            if (Y == d3) { if (X <= d2) return; if (X >= d4) return; f1 |= 4; return; }
            if (X > d4) return;
            if (X == d4) { if (Y <= d3) return; if (Y >= d5) return; f1 |= 8; return; }
            if (Y > d5) return;
            if (Y == d5) { if (X <= d2) return; if (X >= d4) return; f1 |= 1; f2 |= 4; return; }
            f1 = 0xFF; f2 |= 3;
        }
    }
}
