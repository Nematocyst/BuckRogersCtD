// GenesisRng.cs -- exact port of the Genesis game's random number generator (ROM 0x6C94, seeding at 0x12C8).
// Pure C#, no UnityEngine dependency. Verified against the real ROM code run in a 68000 emulator (port/tests).
//
// The generator is a 256-word lagged-Fibonacci style table (work RAM 0xFFD604) with a byte index (0xFFD804):
//     i  = index; index++ (byte)
//     v  = T[(i - 0x67) & 255] ^ T[(i - 0xCD) & 255] ^ T[i];     T[i] = v;
//     result = v mod divisor
// where the divisor is the LOW BYTE of the argument, sign-extended to a word and then used as an unsigned 16-bit
// divisor. Faithful quirks (kept on purpose, scripts rely on the exact stream):
//   * argument whose low byte is 0 returns 0 and does NOT advance the generator (so the script command
//     RANDOM 255 always gives 0: its operand+1 = 256 has a zero low byte).
//   * argument low byte >= 128 becomes a huge unsigned divisor (0xFF80..0xFFFF), so the result is the raw word, and as the
//     routine sign-extends the 16-bit remainder, values >= 0x8000 come back negative.
//   * the table is seeded at boot from 256 words of ROM data at 0x390E2 (these bytes are part of an event-script blob, so
//     editing that script changes the whole random sequence).
//   * the vertical-blank handler adds the byte [0xCA21] (SRAM flag, 0 in normal play) to the high byte of T[0]; call
//     VBlank(flag) once per frame if you want that bit-exact, otherwise ignore it.

using System;

namespace BuckRogersGenesis
{
    public sealed class GenesisRng
    {
        public const int SeedRomOffset = 0x390E2;
        public const int SeedWordCount = 256;

        readonly ushort[] t = new ushort[256];
        byte idx;

        GenesisRng() { }

        /// Seed exactly like the boot code (0x12D2-0x12E0): d1 = 0; for each ROM word w: d1 = (d1 & 0xFFFF) * 0x3C4D; d1.w += w; T[k] = d1.w
        public static GenesisRng FromSeedWords(ushort[] words)
        {
            if (words == null || words.Length < SeedWordCount) throw new ArgumentException("need 256 seed words");
            var r = new GenesisRng();
            uint d1 = 0;
            for (int k = 0; k < 256; k++)
            {
                d1 = (d1 & 0xFFFF) * 0x3C4Du;
                d1 = (d1 & 0xFFFF0000u) | ((d1 + words[k]) & 0xFFFFu);
                r.t[k] = (ushort)d1;
            }
            r.idx = 0;
            return r;
        }

        /// Read the 256 seed words from a Genesis ROM image (big endian) and seed like the boot code.
        public static GenesisRng FromRom(byte[] rom)
        {
            var w = new ushort[SeedWordCount];
            for (int k = 0; k < SeedWordCount; k++) w[k] = (ushort)((rom[SeedRomOffset + 2 * k] << 8) | rom[SeedRomOffset + 2 * k + 1]);
            return FromSeedWords(w);
        }

        /// Restore a generator from a captured RAM state (table = 256 words from 0xFFD604, index = byte at 0xFFD804).
        public static GenesisRng FromState(ushort[] table, byte index)
        {
            var r = new GenesisRng();
            Array.Copy(table, r.t, 256); r.idx = index;
            return r;
        }

        public ushort[] TableCopy() { var c = new ushort[256]; Array.Copy(t, c, 256); return c; }
        public byte Index => idx;

        /// The ROM routine at 0x6C94 with d0 = `d0`: returns the new d0 (a signed 32-bit value as the CPU would hold it).
        public Action<int> Log;                                      // debugging aid: called with the divisor of every draw
        public int Next(int d0)
        {
            Log?.Invoke(d0 & 0xFF);
            int divisor = (ushort)(short)(sbyte)(d0 & 0xFF);          // ext.w d0
            if (divisor == 0) return (int)((uint)d0 & 0xFFFF0000u);   // returns before touching the table
            int i = idx;
            idx++;                                                   // addq.b #1, $d804
            ushort v = (ushort)(t[(i - 0x67) & 255] ^ t[(i - 0xCD) & 255] ^ t[i]);
            t[i] = v;
            int rem = v % divisor;                                   // divu.w d0,d1 ; swap
            return (short)rem;                                       // move.w d1,d0 ; ext.l d0
        }

        /// ROM 0x6C8C: 1..n (n in 1..127): Next(n) + 1.
        public int Roll(int n) { return Next(n) + 1; }

        /// Script command RANDOM n: the interpreter passes n+1 and keeps the low byte, so the result is 0..n (but always 0
        /// for n = 255 because of the zero low byte).
        public int ScriptRandom(int operand) { return Next(operand + 1) & 0xFF; }

        /// Vertical-blank side effect: byte add of [0xCA21] to the high byte of T[0] (0 in normal play).
        public void VBlank(byte ca21 = 0) { t[0] = (ushort)(t[0] + (ca21 << 8)); }
    }
}
