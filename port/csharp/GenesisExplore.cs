// GenesisExplore.cs -- the exploration state the movement script commands change, ported from the ROM (screen output, door animations and the redraw are the host's business).
// State (work RAM, the same bytes the scripts read): [0x9AF7] x, [0x9AF6] y, [0x9AFA] facing (0 north, 1 east, 2 south, 3 west), [0x9AF9] attribute of the square, [0x9AF8] wall nibble ahead,
// [0x97E6] / [0x97E7] the previous x / y, [0xBA5C] the side state ahead, [0xB4C1] half-step flag, [0xB4C7] a countdown the step command decrements.
// The 16 x 16 map layers sit at 0xB5A4 (walls of layer A), 0xB6A4 (layer B), 0xB7A4 (square attributes) and 0xB8A4 (door states, two bits per side); a square's walls are one nibble per side:
//   facing 0 = high nibble of layer A, 1 = low nibble of A, 2 = high nibble of B, 3 = low nibble of B (ROM 0x14CCE); its door state is (byte >> (2 * facing)) & 3 (ROM 0x14C9A).
// Commands: STEPFORWARD 0x53B6 (a side state of 1 -- a wall -- or a target outside the map turns the party round instead; 3 = a door: passes), STEPBACK 0x3EA4 (turn round, then step),
// HALFSTEP 0x3FA0 / HALFBACK 0x3F88 (the half-step flag; HALFBACK turns round when it is set), UNLOCKDOOR 0x3EC4 / 0x5BEC (clears the door bits on both sides of the door ahead),
// HOWFAR 0x3CC6 / 0x4DAA (how many of the next two squares in a direction have no wall; outdoor screens [0x97DC] = 0xA2 / 0xA8 always 2).
using System;
using BuckRogersGenesis.Ecl;

namespace BuckRogersGenesis
{
    public static class GenesisExplore
    {
        public const int X = 0x9AF7, Y = 0x9AF6, Facing = 0x9AFA, Attr = 0x9AF9, WallAhead = 0x9AF8, PrevX = 0x97E6, PrevY = 0x97E7, SideAhead = 0xBA5C, HalfStepFlag = 0xB4C1, Countdown = 0xB4C7;
        public const int WallsA = 0xB5A4, WallsB = 0xB6A4, Attrs = 0xB7A4, Doors = 0xB8A4, ScreenKind = 0x97DC;
        public static readonly int[] Dx = { 0, 1, 0, -1 }, Dy = { -1, 0, 1, 0 };            // ROM 0x146E0 / 0x146EB, entries 0, 2, 4, 6

        static bool InMap(int x, int y) { return (sbyte)x >= 0 && (sbyte)x < 16 && (sbyte)y >= 0 && (sbyte)y < 16; }

        /// 0x14CCE: the wall nibble of square (x, y) on side d5 = 2 * facing (0 outside the map).
        public static int WallNibble(IEclMemory m, int x, int y, int d5)
        {
            if (!InMap(x, y)) return 0;
            int v = m.ReadByte(((d5 & 4) != 0 ? WallsB : WallsA) + (y & 0xFF) * 16 + (x & 0xFF));
            int sh = ((d5 << 1) & 7) ^ 4;
            return (v >> sh) & 0xF;
        }

        /// 0x14C9A / 0x4D8C: the door state ahead of the party (0 when there is no wall or the party is off the map).
        public static int SideState(IEclMemory m)
        {
            int x = m.ReadByte(X), y = m.ReadByte(Y), d5 = (m.ReadByte(Facing) & 0xFF) << 1;
            if (!InMap(x, y) || WallNibble(m, x, y, d5) == 0) return 0;
            return (m.ReadByte(Doors + y * 16 + x) >> (d5 & 7)) & 3;
        }

        /// STEPFORWARD.
        public static void StepForward(IEclMemory m)
        {
            int x = m.ReadByte(X), y = m.ReadByte(Y), f = m.ReadByte(Facing) & 3;
            m.WriteByte(PrevX, x); m.WriteByte(PrevY, y);
            int tx = (x + Dx[f]) & 0xFF, ty = (y + Dy[f]) & 0xFF;
            int side = SideState(m); m.WriteByte(SideAhead, side);
            bool passes = tx < 16 && ty < 16 && ((side & 1) == 0 || side == 3);
            if (passes)
            {
                m.WriteByte(X, tx); m.WriteByte(Y, ty);
                m.WriteByte(Attr, m.ReadByte(Attrs + ty * 16 + tx));                         // 0x4210
                m.WriteByte(WallAhead, WallNibble(m, tx, ty, f << 1));                       // 0x4228
            }
            else m.WriteByte(Facing, (f + 2) & 3);                                           // bump: the party is turned round
            int c = m.ReadByte(Countdown);
            if (c != 0) m.WriteByte(Countdown, c - 1);
        }

        public static void StepBack(IEclMemory m) { m.WriteByte(Facing, (m.ReadByte(Facing) + 2) & 3); StepForward(m); }

        public static void HalfStep(IEclMemory m) { m.WriteByte(HalfStepFlag, 0xFF); }

        public static void HalfBack(IEclMemory m) { if (m.ReadByte(HalfStepFlag) != 0) m.WriteByte(Facing, (m.ReadByte(Facing) + 2) & 3); }

        /// 0x5BEC (called with 0, then 1): clears the two door bits of the party's side of the square, then of the facing side of the square ahead.
        public static void UnlockDoor(IEclMemory m)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                int x = m.ReadByte(X), y = m.ReadByte(Y), f = m.ReadByte(Facing) & 3, d1 = (f << 1) & 0xFF;
                if (pass == 1) { x = (x + Dx[f]) & 0xFF; y = (y + Dy[f]) & 0xFF; d1 = (d1 + 4) & 7; }
                int idx = (((y << 4) & 0xFF) + x) & 0xFF;                                    // asl.b #4, add.b: a byte index
                m.WriteByte(Doors + idx, m.ReadByte(Doors + idx) & ~(3 << (d1 & 7)));
            }
        }

        /// NEWREGION id, n, then n rectangles of four operands (ROM 0x3FB4): [0x9BC5] = id, [0x9BD5] = n, and at 0x9BD6 four big-endian words per rectangle: x1 - 1, y1 - 1, x2 + 1, y2 + 1.
        /// The ROM then loads the map of region [0x9BD4] into the four layers (LZW from the table at 0x8FA8D, ROM 0x574E): that is the host's job.
        public static void NewRegion(IEclMemory m, int id, int n, int[] values)
        {
            m.WriteByte(0x9BC5, id); m.WriteByte(0x9BD5, n);
            int a = 0x9BD6;
            for (int r = 0, k = 0; r < (n & 0xFF); r++)                  // the loop is entered at its DBRA: n rectangles
                for (int j = 0; j < 4; j++, k++)
                {
                    int v = (values[k] + (j < 2 ? -1 : 1)) & 0xFFFF;
                    m.WriteByte(a++, v >> 8); m.WriteByte(a++, v & 0xFF);
                }
        }

        /// HOWFAR: the number (0..2) of squares ahead in direction `dir` (0..3) without a wall.
        public static int HowFar(IEclMemory m, int dir)
        {
            int d5 = (dir << 1) & 0xFF, x = m.ReadByte(X), y = m.ReadByte(Y), n = 0;
            int kind = m.ReadByte(ScreenKind);
            if (kind == 0xA2 || kind == 0xA8) return 2;
            while (n < 2)
            {
                if (WallNibble(m, x, y, d5) != 0) break;
                n++;
                x = (x + Dx[(d5 >> 1) & 3]) & 0xFF; y = (y + Dy[(d5 >> 1) & 3]) & 0xFF;
            }
            return n;
        }
    }
}
