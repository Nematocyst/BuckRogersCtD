// TokenFrames.cs -- which of the 18 frames of a creature's token sheet the fight screen shows (ROM 0xAD5A draws the standing creatures; verified by running the real routine in an
// emulator and matching its output against every sheet frame, for small, tall and wide creatures, see docs/07-graphics-scoping.md).
namespace BuckRogersGenesis
{
    public static class TokenFrames
    {
        public const int Front = 0, Side = 6, Back = 12, Lying = 16, Ready = 17, Hurt = 15, Frames = 18;

        /// Size in pixels of one frame in the exported strips (tools/export_tokens.py): 24x24 normally, 24x48 for tall creatures (size type 2), 48x24 for wide ones (type 3); the cell size of the map is 24.
        public static void FramePixels(int sizeType, out int w, out int h) { w = sizeType == 3 ? 48 : 24; h = sizeType == 2 ? 48 : 24; }

        /// The frame of a creature standing / lying on the battlefield. `facing` is the slot byte +0x10 (0..7; the deployment uses even values, direction * 2), `down` = status bit 7 (dying, dead).
        /// facing 0 = front, 1-3 = side, 4 = back, 5-7 = side mirrored (the ROM sets the tile h-flip bit and reads the frame backwards).
        public static int Idle(int facing, bool down, out bool mirrored)
        {
            mirrored = false;
            if (down) return Lying;
            int k = ((facing & 7) == 0) ? 0 : ((facing & 7) <= 3) ? 1 : ((facing & 7) == 4) ? 2 : 1;
            mirrored = (facing & 7) >= 5;
            return k == 0 ? Front : k == 1 ? Side : Back;
        }

        /// The animation set (0 / 1 / 2) of a creature: byte +6 of its entry in the token table at ROM 0x9A14 (key = record byte +0x42); party keys (bit 7) have set 0, unknown ids too.
        public static int AnimationSet(RomView rom, int id)
        {
            if ((id & 0x80) != 0) return 0;
            for (int p = 0x9A14; p < 0x9BC0; p += 8)
            {
                if (rom.Byte(p) == 0xFF) break;
                if (rom.Byte(p + 4) == id) return rom.Byte(p + 6) & 3;
            }
            return 0;
        }

        /// The default party of the game (ROM blob 0x6BAAD, loaded by 0x1F32) and the party-sheet key (record byte +0x42 = 0x80 | sheet) each member carries:
        /// name, record race (+0x17), sex (+0x16: 0 male, 1 female), career (+0x18: 1 rocket jock, 2 medic, 3 warrior, 4 rogue), key. The three NPC allies that reuse these sprites are
        /// the entries 0x6A -> 0x8A, 0x6B -> 0x83, 0x6C -> 0x87 of the ally table (ROM 0x48DA). No ROM code writes +0x42 for a character made on the creation screen (its record is
        /// cleared first), so such a character keeps key 0 and is drawn with the token of monster id 0.
        public static readonly object[][] PregenKeys =
        {
            new object[] { "FLAVIUS", 2, 0, 3, 0x83 }, new object[] { "CELESTE", 2, 1, 3, 0x85 }, new object[] { "PIERRE", 1, 0, 1, 0x8A },
            new object[] { "NICHOLE", 1, 1, 4, 0x87 }, new object[] { "ROARKE", 3, 0, 2, 0x81 }, new object[] { "JANELLE", 3, 1, 2, 0x80 },
        };

        public const int Vanish = -1;                       // "remove the creature": the block is blanked and the slot is marked hidden (slot+1 bit 2)

        /// The pose of the attacker while it aims / fires (draw state 5, ROM 0xCAEA with the b586 frames): facing 0 = frame 3, 1-3 = frame 4, 4 = frame 5, 5-7 = frame 4 mirrored.
        /// The actor first turns towards its target (0x116AC, slot+0x10 = the octant to the target).
        public static int Aim(int facing, out bool mirrored)
        {
            int f = facing & 7; mirrored = f >= 5;
            return f == 0 ? 3 : f == 4 ? 5 : 4;
        }

        /// The pose of a creature that was hit and survives (state 0): frame 17 for 15 ticks, then back to the idle pose (0x115E0..0x11626).
        public const int HitPose = 17, HitTicks = 15;

        /// The death sequence (0x11B30: draw states 1, 2, 3, 4 in turn, each held for `ticks`): animation set 0 (byte +6 of the 0x9A14 entry): frames 15, 16 (it stays lying);
        /// set 1: 15, 16, then it vanishes; set 2: 15, 16, 11, then it vanishes (table 0xCC36 says per state whether it is drawn, skipped or the vanish). Verified by running the ROM routine.
        public static int[] Death(int animationSet)
        {
            return animationSet == 0 ? new[] { 15, 16 } : animationSet == 1 ? new[] { 15, 16, Vanish } : new[] { 15, 16, 11, Vanish };
        }

        /// Ticks (the ROM's 0x75FA delay unit) each death state is held: 20 for normal-size creatures, 35 (0x23) for tall / wide ones (record size type 2, 3).
        public static int DeathTicks(int sizeType) { return sizeType >= 2 ? 35 : 20; }
    }
}
