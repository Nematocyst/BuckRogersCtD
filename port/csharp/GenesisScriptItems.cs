// GenesisScriptItems.cs -- the item commands of the script engine that change or read the party's gear, ported from the ROM handlers:
//   FINDITEM id  (opcode 0x32, ROM 0x3A8E -> search 0x76C4): EQ when a party member (combat slot status non-zero) carries the item in one of its 13 gear entries (record +0x54, 10 bytes each,
//       id first), NE otherwise. While [0x97AE] is set the search finds nothing for party records. Note that id 0 matches an empty entry.
//   DESTROY x, id (opcode 0x40, ROM 0x3A60): removes ONE of item `id & 0x7F` from the first member who has it (quantity at entry +8 above 1: minus one, otherwise the id byte is cleared); the first operand is read and ignored.
//   HIDEITEMS n  (opcode 0x48, ROM 0x3D0C): [0x97AE] = 0xFF when n is 0, else 0, then every present member's combat slot is recomputed (0x6D1E), which ignores gear while the flag is set.
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        /// 0x76C4: (record, entry) of the first match, or (-1, -1).
        bool FindGear(int id, out int member, out int entry)
        {
            member = -1; entry = -1;
            for (int i = 0; i < 8; i++)
            {
                if (S.Slots[i][0] == 0) continue;
                if (Mode97AE != 0) continue;                                           // 0x76FA: a party record while [0x97AE] is set: never found
                for (int k = 0; k < 13; k++)
                    if (S.Records[i][0x54 + 10 * k] == (id & 0xFF)) { member = i; entry = k; return true; }
            }
            return false;
        }

        public bool ScriptFindItem(int id) { int m, k; return FindGear(id, out m, out k); }

        public void ScriptDestroyItem(int id)
        {
            int m, k;
            if (!FindGear(id & 0x7F, out m, out k)) return;
            var r = S.Records[m]; int o = 0x54 + 10 * k;
            if (r[o + 8] > 1) r[o + 8]--; else r[o] = 0;
        }

        public void ScriptHideItems(int n)
        {
            Mode97AE = (byte)((n & 0xFF) == 0 ? 0xFF : 0);
            for (int i = 7; i >= 0; i--)
                if (S.Slots[i][0] != 0) GenesisStats.RecomputeSlot(Rom, S.Slots[i], S.Records[i], Gb(A49A), Gb(A499), Mode97AE != 0);
        }
    }
}
