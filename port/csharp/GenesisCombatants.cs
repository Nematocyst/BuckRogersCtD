// GenesisCombatants.cs -- how the creatures of a fight come into being (the script opcodes "add monsters" ROM 0x3544 and "add a party NPC" 0x488C, both using the record loader
// 0x48E8): a monster type is looked up in the monster file (a list of ids and the 214-byte records, an LZW stream at ROM 0x9E77C that the host decompresses), its record is copied into
// the next free record index, its gear is tidied up (item ids remapped through the table at 0xA829, a stray quantity cleared, an unworn weapon / armour moved into the empty hand / body
// slot) and one combat slot per creature is made (status 1, flags from the record | 0x40, the record index, hit points, derived stats = 0x6D1E).
// Verified against the ROM in port/tests/ArenaTests.cs (ids, counts and the free-slot search with random monster files; the decompressor is replaced by the file).
using System;

namespace BuckRogersGenesis
{
    /// The decompressed monster file: [count word][count ids, padded to even][count records of 214 bytes].
    public sealed class MonsterBinFile
    {
        public byte[] Ids;
        public byte[][] Records;

        public static MonsterBinFile Parse(byte[] b)
        {
            int n = (b[0] << 8) | b[1];
            var f = new MonsterBinFile { Ids = new byte[n], Records = new byte[n][] };
            Array.Copy(b, 2, f.Ids, 0, n);
            int p = 2 + ((n + 1) & ~1);
            for (int i = 0; i < n; i++) { f.Records[i] = new byte[214]; Array.Copy(b, p + 214 * i, f.Records[i], 0, 214); }
            return f;
        }
    }

    public sealed partial class TurnContext
    {
        public MonsterBinFile Monsters;                      // the monster file (see MonsterBinFile)

        /// 0x3544: the script adds `count` monsters of type `id` to the fight: they use the next monster record index (8 + [0xD49B], at most 3 types per fight).
        public void AddMonsters(int id, int count)
        {
            int k = Gb(0xD49B);
            Gs(0xD49B, k + 1);
            LoadCombatant(id, count, id, 8 + k);
        }

        /// 0x488C: the script adds a party NPC: the type goes in the first free slot of the party's eight (nothing happens when all are taken); `op2` becomes record byte +0x30.
        public void AddAlly(int id, int op2)
        {
            int d5 = ((op2 & 0xFFFF) >> 1 | 0x80) & 0xFF;
            int free = -1;
            for (int i = 0; i < 8; i++) { EnsureSlot(i); if (S.Slots[i][0] == 0) { free = i; break; } }
            if (free < 0) return;
            int p = 0x48DA;
            while (Rom.Byte(p) != (id & 0xFF)) { p += 2; if (p > 0x48E8) throw new InvalidOperationException("unknown party NPC " + id); }
            LoadCombatant(id, 1, Rom.Byte(p + 1), free);
            S.Records[free][0x30] = (byte)d5;
        }

        void EnsureSlot(int i)
        {
            if (S.Slots.Length <= i) Array.Resize(ref S.Slots, i + 1);
            if (S.Slots[i] == null) S.Slots[i] = new byte[26];
        }

        /// 0x48E8: record `rec` becomes monster `id` (d3 = count: byte +0x3F; d4: byte +0x42) and `count` slots are made for it: from slot `rec` on for a party record (< 8),
        /// else at the end of the slot list (at most 0x38 slots in all).
        public void LoadCombatant(int id, int count, int d4, int rec)
        {
            int idx = Array.IndexOf(Monsters.Ids, (byte)id);
            if (idx < 0) throw new InvalidOperationException("monster " + id + " is not in the monster file");
            if (rec >= S.Records.Length) throw new InvalidOperationException("no record index " + rec);
            var r = S.Records[rec] ?? (S.Records[rec] = new byte[214]);
            Array.Copy(Monsters.Records[idx], r, 214);
            r[0x3F] = (byte)count;
            r[0x24] = (byte)(((r[0x24] * 2) & 0xFF) / 3);
            for (int k = 0; k < 13; k++)
            {
                int a = 0x54 + 10 * k;
                if (r[a] != 0) { int t = Rom.Byte(0xA829 + r[a]); if (t < 0x80) r[a] = (byte)t; }
                if (r[a + 8] > 0xA) r[a + 8] = 0;
                if (r[a] == 0) continue;
                int kind = Rom.Byte(0x779E + r[a] * 8), eq;
                if (kind == 0) eq = 0xAE; else if (kind == 1) eq = 0xC2; else continue;
                if (r[eq] != 0) continue;
                for (int j = 0; j < 10; j++) { byte tmp = r[eq + j]; r[eq + j] = r[a + j]; r[a + j] = tmp; }
            }
            int first = rec, n = count & 0xFFFF;
            if (rec >= 8)
            {
                first = S.SlotCount;
                int room = 0x38 - first;
                if (n > room) n = room;
                S.SlotCount += n;
            }
            r[0x42] = (byte)d4;
            for (int i = 0; i < n; i++)
            {
                EnsureSlot(first + i);
                var sl = S.Slots[first + i];
                sl[0] = 1; sl[1] = (byte)(r[0x52] | 0x40); sl[2] = (byte)rec; sl[0xE] = r[0x2E];
                GenesisStats.RecomputeSlot(Rom, sl, r, Gb(A49A), Gb(A499), Mode97AE != 0);
            }
            S.RecordSizeType[rec] = r[0x23];
        }
    }
}
