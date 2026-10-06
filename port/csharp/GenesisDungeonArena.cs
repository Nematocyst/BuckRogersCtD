// GenesisDungeonArena.cs -- the battlefield of a fight inside a dungeon map (screen mode 6, ROM 0xB100 with 0xB396 / 0xB3D4 / 0xB3FA): the 16x16 map layers around the party are
// turned into the 21x21 tile map at 0xCACA. The map is scanned in 6x5 blocks along the diagonals of the arena (a rotated view); every arena cell gets a tile id from the wall
// bits around the map cell and from the two tile tables the map defines.
// Verified against the ROM in port/tests/MonsterTests.cs (random layers and tables, the whole 441-byte result compared).
namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public byte[] MapLayerA = new byte[256];      // [0xB5A4]: 16x16 map layer; low nibble = tile class of the cell above, high nibble = class of the cell on the other side
        public byte[] MapLayerB = new byte[256];      // [0xB6A4]: the second layer (the cell itself; the cell left of the map; the row below the map)
        public byte[] MapWalls = new byte[256];       // [0xB7A4]: bit 7 = a wall stands in the cell
        public byte[] TileClassA = new byte[16];      // [[0xB41A]]: tile class -> shape (bit 7 set = no drawing)
        public byte[] TileClassB = new byte[16];      // [[0xB41E]]: tile class -> variant
        public byte MapX, MapY;                       // [0x9AF7], [0x9AF6]: the party's cell on the map

        /// 0xB3D4: is there a wall in cell (x, y)? (outside the 16x16 map: no)
        int WallAt(int x, int y)
        {
            if ((x & 0xFF) >= 0x10 || (y & 0xFF) >= 0x10) return 0;
            return (MapWalls[((y << 4) + x) & 0xFF] & 0x80) != 0 ? 1 : 0;
        }

        /// 0xB3FA: the variants of the three tile classes around the cell (xx, yy): a = the cell itself (-0x42), c = the one above (-0x43), b = the one on the other side (-0x44);
        /// 0xFF where outside the map or where the class has no drawing (shape bit 7). (The ROM also computes the drawing offsets, which the arena builder never reads.)
        void CellVariants(int xx, int yy, out int a, out int c, out int b)
        {
            xx &= 0xFFFF; yy &= 0xFFFF;
            a = c = b = 0xFF;
            if (yy < 0x10 && xx <= 0x10)
            {
                int cls = xx == 0x10 ? MapLayerA[((yy << 4) + 0xF) & 0xFF] & 0xF : MapLayerB[((yy << 4) + xx) & 0xFF] & 0xF;
                a = (sbyte)TileClassA[cls] < 0 ? 0xFF : TileClassB[cls];
            }
            int y1 = (yy - 1) & 0xFFFF;
            if (y1 < 0x10 && (xx == 0xFFFF || xx < 0x10))
            {
                int cls = xx == 0xFFFF ? MapLayerB[(y1 << 4) & 0xFF] & 0xF : MapLayerA[((y1 << 4) + xx) & 0xFF] & 0xF;
                c = (sbyte)TileClassA[cls] < 0 ? 0xFF : TileClassB[cls];
            }
            if (xx < 0x10 && yy <= 0x10)
            {
                int cls = yy == 0x10 ? (MapLayerB[(0xF0 + xx) & 0xFF] >> 4) & 0xF : (MapLayerA[((yy << 4) + xx) & 0xFF] >> 4) & 0xF;
                b = (sbyte)TileClassA[cls] < 0 ? 0xFF : TileClassB[cls];
            }
        }

        /// 0xB100: the dungeon arena. The party's cell (shifted by the direction it faces when it surprises the monsters from one side) is the centre of the view.
        public void BuildDungeonArena()
        {
            TerrainTable = 0x3297; ScriptTable = 0;
            int dir = (Gb(0xD4FE) != 0 && GroupMask == 0) ? Facing : 4;
            int x0 = (sbyte)(MapX + Rom.Byte(0xB332 + dir)), y0 = (sbyte)(MapY + Rom.Byte(0xB337 + dir));
            int bx = -3, by = -2;                     // -0x36 / -0x30: where the block starts in the arena
            var l = new int[4];                       // -0x22..-0x1c: wall flags (0 / 0x12): [0] the cell, [1] the previous block's [0], [2] the diagonal cell, [3] the previous [2]
            do
            {
                int xx = x0, ax = bx;
                Walls(xx - 1, y0, l);
                do
                {
                    int bs = (short)(by * 21 + ax);
                    l[1] = l[0]; l[3] = l[2];
                    Walls(xx, y0, l);
                    CellVariants(xx, y0, out int a, out int c, out int b);
                    int t = 0xB33C;
                    for (int row = 0; row < 5; row++)
                        for (int col = 0; col < 6; col++)
                        {
                            int d0 = (sbyte)Rom.Byte(t++), d1 = (sbyte)Rom.Byte(t++), d2 = (sbyte)Rom.Byte(t++);
                            if (((ax + col) & 0xFFFF) < 0x15 && ((by + row) & 0xFFFF) < 0x15)
                                S.Tiles[bs + row * 21 + col] = (byte)CellTile(d0, d1, d2, a, b, c, l);
                        }
                    xx++; ax += 6;
                } while (ax < 0x15);
                y0++; x0--; by += 5; bx--;
            } while ((by & 0xFFFF) < 0x15);
        }

        void Walls(int xx, int yy, int[] l)           // 0xB396
        {
            l[0] = WallAt(xx, yy) != 0 ? 0x12 : 0;
            l[2] = WallAt(xx + 1, yy - 1) != 0 ? 0x12 : 0;
        }

        /// 0xB1BA..0xB2E2: the tile of one arena cell from its row of the pattern table (d0 = kind, d1 = part, d2 = side) and the classes around the map cell.
        static int CellTile(int d0, int d1, int d2, int a, int b, int c, int[] l)
        {
            int r;
            if (d1 >= 0 && (sbyte)b >= 0)
            {
                if (d0 == 0 && (sbyte)a >= 0) r = 0x64 + (b == 1 ? 2 : 0) + (a == 1 ? 1 : 0);
                else r = (sbyte)b * 6 + d1 + (l[3] != 0 ? 0x12 : 0) + 4;
                return r + 2 & 0xFF;
            }
            if (d0 >= 0)
            {
                int v = (sbyte)(d0 == 9 ? c : a);
                if (v >= 0)
                {
                    if (v == 2 && d2 == 4)
                    {
                        if (d0 == 5) goto edge;
                        if (l[0] != l[1])
                        {
                            if (d0 == 3) return (l[0] == 0 ? 0x69 : 0x68) + 2;
                            if (d0 == 7) return (l[0] == 0 ? 0x6B : 0x6A) + 2;
                        }
                        d2 = 4;
                    }
                    int e = d2 == 4 ? 1 : d2 == 5 ? 3 : d2;
                    return (d0 + v * 10 + (l[e] != 0 ? 0x1E : 0) + 0x28 + 2) & 0xFF;
                }
            }
        edge:
            if (d2 == 4 || d2 == 5)
            {
                int w0 = d2 == 4 ? l[0] : l[2], w1 = d2 == 4 ? l[1] : l[3];
                r = w0 == 0 ? (w1 != 0 ? 3 : 0) : (w1 != 0 ? 1 : 2);
            }
            else r = l[d2] != 0 ? 1 : 0;
            return r + 2 & 0xFF;
        }
    }
}
