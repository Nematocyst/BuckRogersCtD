// GenesisParty.cs -- the game's default party: the compressed blob at ROM 0x6BAAD (loaded by 0x1F32) holds eight 214-byte party records (six characters, two empty) and eight 26-byte
// combat slots. The export tool (tools/export_default_party.py) writes the decompressed 1,920 bytes; this class reads them.
// Record fields (the ones used): +0x00 name (NUL padded, up to 15 chars), +0x10..0x15 the six abilities, +0x16 sex (0 male, 1 female), +0x17 race (GenesisRace), +0x18 career (1 rocket jock, 2 medic,
// 3 warrior, 4 rogue), +0x19 level, +0x1E experience (big-endian long), +0x2E hit points, +0x42 token key (0x80 | party sheet), +0x54 + 10 * k the 13 item slots (id first).
using System;
using System.Collections.Generic;
using System.Text;

namespace BuckRogersGenesis
{
    public sealed class DefaultParty
    {
        public const int RecordSize = 214, SlotSize = 26, Count = 8;
        public byte[][] Records = new byte[Count][];
        public byte[][] Slots = new byte[Count][];

        public static DefaultParty Parse(byte[] blob)
        {
            if (blob.Length < Count * (RecordSize + SlotSize)) throw new ArgumentException("default party blob too short");
            var p = new DefaultParty();
            for (int i = 0; i < Count; i++)
            {
                p.Records[i] = new byte[RecordSize]; Array.Copy(blob, i * RecordSize, p.Records[i], 0, RecordSize);
                p.Slots[i] = new byte[SlotSize]; Array.Copy(blob, Count * RecordSize + i * SlotSize, p.Slots[i], 0, SlotSize);
            }
            return p;
        }

        public static string Name(byte[] r) { int n = 0; while (n < 15 && r[n] != 0) n++; return Encoding.ASCII.GetString(r, 0, n); }
        public bool Present(int i) { return Records[i][0] != 0; }
        public string NameOf(int i) { return Name(Records[i]); }
        public int Sex(int i) { return Records[i][0x16]; }
        public int Race(int i) { return Records[i][0x17]; }
        public int Career(int i) { return Records[i][0x18]; }
        public int Level(int i) { return Records[i][0x19]; }
        public int HitPoints(int i) { return Records[i][0x2E]; }
        public int Experience(int i) { var r = Records[i]; return r[0x1E] << 24 | r[0x1F] << 16 | r[0x20] << 8 | r[0x21]; }
        public int Key(int i) { return Records[i][0x42]; }
        public int[] Abilities(int i) { var a = new int[6]; for (int k = 0; k < 6; k++) a[k] = Records[i][0x10 + k]; return a; }
        public List<int> ItemIds(int i) { var l = new List<int>(); for (int k = 0; k < 13; k++) if (Records[i][0x54 + 10 * k] != 0) l.Add(Records[i][0x54 + 10 * k]); return l; }
    }

    public sealed partial class TurnContext
    {
        /// Puts a party member (a record and its slot as the game stores them) into party index `index`: the record is copied, the slot is copied, and the slot count grows to cover it.
        public void LoadPartyMember(byte[] record, byte[] slot, int index)
        {
            if (index < 0 || index >= 8) throw new ArgumentException("party index 0..7");
            var r = S.Records[index] ?? (S.Records[index] = new byte[DefaultParty.RecordSize]);
            Array.Copy(record, r, DefaultParty.RecordSize);
            EnsureSlot(index);
            Array.Copy(slot, S.Slots[index], DefaultParty.SlotSize);
            S.RecordSizeType[index] = r[0x23];
            if (S.SlotCount <= index) S.SlotCount = index + 1;
        }
    }
}
