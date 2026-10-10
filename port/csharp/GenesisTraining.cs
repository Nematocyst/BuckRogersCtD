// GenesisTraining.cs -- what the training screen does to a character (ROM 0xC96 -> 0xCD0, the skill pick screen 0xE8C and the speciality pick screen 0xDC4, both drawn by 0xF5A), ported from the ROM.
// The screens themselves (menus, text) are the host's: TrainMenu answers the ROM's menu 0x1391A with the chosen cell, a negative number being "undo the last pick".
//
// Who can train: GenesisProgression.ScanTraining (ROM 0x7418). For the chosen member, CD0 does, in order:
//   level +1 (record +0x19);
//   4 skill points (0xE8C with 4): the menu lists 12 skills -- the career's own (index career - 1) and general skills 4..14; a pick adds 1 to a skill if the skill is below 12 and has been picked fewer than 2 times
//     in this session; an invalid pick is ignored; a negative answer takes the last pick back; the screen ends when all 4 points are placed. Only the first 11 entries are written back to the record (0xF00);
//   if the new level is EVEN: record +0x15 (the saving-throw number, 14 at creation) goes down by 1, and a WARRIOR (career 3) also picks one weapon speciality (0xDC4 with 1): the menu lists the six weapon
//     classes (items 3, 8, 6, 14, 9, 10), a class may be taken at most 3 times in all (counted from the speciality bytes record +0x4D..+0x51), and the class id is appended to that list;
//   hit points (0xB1C: GenesisProgression.HpAfterLevelUp) and the current hit points set to the new maximum; attack value (0xB92); the combat slot is recomputed (0x6D1E).
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public Func<int, int> TrainMenu;                         // 0x1391A on the training screens, given the number of cells (11 skills / 6 speciality classes): the cell chosen (negative = take the last pick back)
        static readonly int[] SpecialityClasses = { 0x03, 0x08, 0x06, 0x0E, 0x09, 0x0A };           // ROM 0xE60

        /// The training screen (ROM 0xC96): while somebody is present, the scan 0x7418 lists who is ready (XP capped to one level per session), `choose` (the member menu 0x71D6, given the scan; the
        /// ordinal counts present slots only) names the member or leaves (negative), and the member trains (0xCD0). A member the scan lists as not ready cannot be chosen: the menu greys them out.
        public void TrainingScreen(Func<GenesisProgression.TrainingScan, int> choose)
        {
            for (int guard = 0; guard < 1000; guard++)
            {
                var flags = new int[8]; var career = new int[8]; var level = new int[8]; var xp = new uint[8]; int present = 0;
                for (int i = 0; i < 8; i++)
                {
                    var sl = S.Slots[i]; var r = S.Records[i];
                    flags[i] = sl[0]; career[i] = r[0x18]; level[i] = r[0x19]; xp[i] = (uint)(r[0x1E] << 24 | r[0x1F] << 16 | r[0x20] << 8 | r[0x21]);
                    if (sl[0] != 0) present++;
                }
                if (present == 0) return;
                var scan = GenesisProgression.ScanTraining(Rom, flags, career, level, xp);
                for (int i = 0; i < 8; i++)
                {
                    var r = S.Records[i]; uint v = scan.Xp[i];
                    r[0x1E] = (byte)(v >> 24); r[0x1F] = (byte)(v >> 16); r[0x20] = (byte)(v >> 8); r[0x21] = (byte)v;
                }
                int ord = choose(scan);
                if (ord < 0) return;
                if (scan.NotReady.Contains(ord)) continue;
                int member = -1, seen = 0;
                for (int i = 0; i < 8; i++) if (S.Slots[i][0] != 0) { if (seen++ == ord) { member = i; break; } }
                if (member < 0) continue;
                TrainLevelUp(member);
            }
        }

        /// 0xCD0 (the state part; the sheet that follows is the host's).
        public void TrainLevelUp(int member)
        {
            var rec = S.Records[member]; var slot = S.Slots[member];
            rec[0x19]++;
            int career = rec[0x18];
            // 0xE8C: the skills. pair p = (index, value): pair 0 = the career's own skill, pairs 1..11 = skills 4..14
            var idx = new int[12]; var val = new int[12];
            idx[0] = (career - 1) & 0xFF; val[0] = rec[0x31 + ((career - 1) & 0xFF)];
            for (int p = 1; p < 12; p++) { idx[p] = 3 + p; val[p] = rec[0x31 + idx[p]]; }
            PickLoop(4, 2, 12, val, 11);
            for (int p = 0; p < 11; p++) rec[0x31 + idx[p]] = (byte)val[p];
            if ((rec[0x19] & 1) == 0)
            {
                rec[0x15]--;
                if (career == 3)
                {
                    // 0xDC4: one speciality
                    var cnt = new int[6];
                    for (int k = 0; k < 5; k++) for (int c = 0; c < 6; c++) if (rec[0x4D + k] == SpecialityClasses[c]) { cnt[c]++; break; }
                    int picked = PickLoop(1, 1, 3, cnt, 6);
                    int at = 0x4D; while (at < rec.Length && rec[at] != 0) at++;
                    if (picked >= 0 && at < rec.Length) rec[at] = (byte)SpecialityClasses[picked];
                }
            }
            rec[0x2E] = (byte)GenesisProgression.HpAfterLevelUp(Rom, Rng, rec[0x18], rec[0x12], rec[0x2E]);
            slot[0xE] = rec[0x2E];
            rec[0x22] = (byte)GenesisProgression.AttackValue(Rom, rec[0x18], rec[0x19]);
            GenesisStats.RecomputeSlot(Rom, slot, rec, Gb(A49A), Gb(A499), Mode97AE != 0);
        }

        /// 0xF5A's pick loop: `points` picks, each entry at most `perEntry` times in this session and only while its value is below `cap`; values are raised in place. Returns the last entry picked.
        int PickLoop(int points, int perEntry, int cap, int[] values, int entries)
        {
            var picks = new int[points + 1]; int n = 0, last = -1;
            for (int guard = 0; n < points && guard < 10000; guard++)
            {
                int a = TrainMenu(entries);
                if (a < 0)
                {
                    if (n > 0) { n--; values[picks[n]]--; }
                    continue;
                }
                if (a >= entries) continue;
                int times = 0; for (int k = 0; k < n; k++) if (picks[k] == a) times++;
                if (times >= perEntry || values[a] >= cap) continue;
                picks[n++] = a; values[a]++; last = a;
            }
            return n == 0 ? -1 : picks[0];
        }
    }
}
