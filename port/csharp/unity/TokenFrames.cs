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
    }
}
