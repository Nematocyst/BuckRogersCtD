// GenesisCombatEnd.cs -- what happens when a fight is over (ROM 0x15FDA): statuses are settled (fled creatures return, the downed stay down), the stats are worked out again, the
// scripted treasure and the monsters' gear and credits go into the loot pool (0x1621E, 0x1631E: GenesisRewards.AddItem), the experience pool is divided among the living party members
// and credited, the credits go into the party's money. The medical aftermath of the dying (0x16B96), the loot sharing screen (0x165A0) and a few story hooks are the host's
// (CombatEndHooks); everything drawn is left out. Verified against the ROM in port/tests/MonsterTests.cs.
using System;
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public byte[] Pool = new byte[140];       // 0x6AF6: the loot pool, 14 items of 10 bytes
        public byte PoolCount;                    // [0xB9F3]: items in the pool (before the cleanup: the number of scripted treasure ids)
        public byte[] ScriptedLoot = new byte[16];   // [0xB9F4..]: scripted treasure (ids of base items, see GenesisRewards.BaseItemTable)
        public uint Credits;                      // [0xBA34]: credits collected from the defeated
        public byte FledMask;                     // [0xD8DA]: party members that fled (bit per slot)
        public byte Flag9DBD;                     // [0x9DBD]: 0x80 = the player's solo creature was knocked out, 0xFF = the party was wiped out
        public byte Scripted9858, Scripted9930, Scripted9927, Scripted9924;   // scripted-fight flags: bonus experience table index [0x9924]
        /// A solo fight (a single party member sent to fight alone, [0xBA5B]) that the member lost: the story script reads this, there is no game over because the others never took part.
        public bool SoloMemberLost { get { return Flag9DBD == 0x80; } }
        public byte SavedMode;                    // [0xBA5E]: the game mode to return to
        public Action GameOver, Aftermath, LootScreen, ScriptedFightEnd;   // 0x7588, 0x16B96, 0x165A0, 0x16EF0: the host's

        LootPool PoolView()
        {
            var p = new LootPool();
            for (int i = 0; i < PoolCount && i < LootPool.MaxEntries; i++) { var e = new byte[10]; Array.Copy(Pool, i * 10, e, 0, 10); p.Entries.Add(e); }
            return p;
        }
        void PoolStore(LootPool p)
        {
            for (int i = 0; i < p.Entries.Count; i++) Array.Copy(p.Entries[i], 0, Pool, i * 10, 10);
            PoolCount = (byte)p.Count;
        }
        void AddLoot(byte[] item)
        {
            var p = PoolView(); GenesisRewards.AddItem(Rom, p, item, ShopFlag != 0); PoolStore(p);
        }

        /// 0x1621E: the pool is emptied and the scripted treasure goes in (nothing happens while the party is player driven, [0x97AE]).
        public void ScriptedTreasure()
        {
            if (Mode97AE != 0) return;
            Array.Clear(Pool, 0, 140);
            int n = PoolCount; PoolCount = 0;
            for (int k = 0; k < n; k++)
            {
                var item = new byte[10]; int id = ScriptedLoot[k];
                for (int i = 0; i < 10; i++) item[i] = (byte)Rom.Byte(GenesisRewards.BaseItemTable + id * 10 + i);
                AddLoot(item);
            }
        }

        /// 0x1631E: the defeated enemies (slot 8 up, status bit 7) give experience (record +0x40 unless +0x52 bit 0) and, unless this is a solo or player-driven fight, credits (+0x1A) and
        /// their gear (not built-in items) for the pool; the living party members (not fled, not down) are counted in [0xD514]. At the end [0xD514] = the experience each of them gets.
        public void Tally()
        {
            uint d6 = 0; uint living = 0;
            Gs(0xD514, 0); Gs(0xD515, 0); Gs(0xD516, 0); Gs(0xD517, 0);
            if ((sbyte)Scripted9930 < 0 && Scripted9927 == 1) d6 = (uint)(Rom.Byte(0x16402 + 2 * Scripted9924) << 8 | Rom.Byte(0x16402 + 2 * Scripted9924 + 1));
            int n = Math.Max(S.SlotCount, 1);
            for (int d7 = 0; d7 < n; d7++)
            {
                if (d7 >= S.SlotCount) break;
                var sl = S.Slots[d7]; int st = sl[0];
                if (st == 0) continue;
                if (d7 < 8)
                {
                    if (((FledMask >> d7) & 1) != 0) continue;
                    if ((st & 0x80) != 0) continue;
                    living++; continue;
                }
                if ((st & 0x80) == 0) continue;
                var rec = S.Records[sl[2]];
                if ((rec[0x52] & 1) == 0) { d6 += (uint)(rec[0x40] << 8 | rec[0x41]); Gs(0xD50E, 0xFF); }
                if (SoloFlag != 0 || Mode97AE != 0) continue;
                Credits = unchecked(Credits + (uint)(rec[0x1A] << 24 | rec[0x1B] << 16 | rec[0x1C] << 8 | rec[0x1D]));
                for (int g = 0; g < 13; g++)
                {
                    int o = 0x54 + 10 * g;
                    if (rec[o] == 0 || (rec[o + 5] & 0x40) != 0) continue;
                    rec[o + 5] &= 0x7F;
                    var item = new byte[10]; Array.Copy(rec, o, item, 0, 10);
                    AddLoot(item);
                }
            }
            uint d0;
            if (SoloFlag != 0) d0 = d6;
            else if (living == 0) d0 = 0;
            else { uint q = d6 / (living & 0xFFFF); d0 = q <= 0xFFFF ? q : (d6 & 0xFFFF); }
            Gs(0xD514, (int)(d0 >> 24)); Gs(0xD515, (int)(d0 >> 16)); Gs(0xD516, (int)(d0 >> 8)); Gs(0xD517, (int)d0);
        }

        uint Xp() { return (uint)(Gb(0xD514) << 24 | Gb(0xD515) << 16 | Gb(0xD516) << 8 | Gb(0xD517)); }

        /// 0x1640C (state only): after a fight with something to report the experience is credited ([0x16560]: record +0x1E of every standing, not fled party member), the credits
        /// go into the money; a solo creature that was knocked out, or a fight lost with fled survivors, forfeits experience, credits and loot first.
        void Victory()
        {
            if (ShopFlag != 0) return;
            bool nothing = Gb(0xD50E) == 0 && SoloFlag == 0 && Xp() == 0 && Credits == 0 && ScriptedLoot[0] == 0;
            if (nothing) return;
            if (Gb(0xD50E) != 0 || SoloFlag != 0)
            {
                if (Flag9DBD == 0x80 || (Flag9DBD != 0x80 && Gb(0xD50B) != 0))
                { for (int i = 0xD514; i < 0xD518; i++) Gs(i, 0); PoolCount = 0; Credits = 0; }
            }
            uint xp = Xp();
            if (xp != 0)
                for (int i = 7; i >= 0; i--)
                {
                    if (i >= S.Records.Length) continue;
                    if (((i < S.SlotCount ? S.Slots[i][0] : 0) & 0xC0) != 0 || ((FledMask >> i) & 1) != 0) continue;     // an empty slot still counts: its record gets the experience
                    var r = S.Records[i]; uint cur = (uint)(r[0x1E] << 24 | r[0x1F] << 16 | r[0x20] << 8 | r[0x21]); cur += xp;
                    r[0x1E] = (byte)(cur >> 24); r[0x1F] = (byte)(cur >> 16); r[0x20] = (byte)(cur >> 8); r[0x21] = (byte)cur;
                }
            if (Credits != 0) Money += Credits;
            Gs(0xD593, 0);                                                                       // 0x14246
            if (ContinuePrompt != null) ContinuePrompt(); else if (Pad != null) ChoicePrompt(2);
        }

        /// 0x15FDA: the end of a fight (the caller has run CombatRounds). Returns true when the party is lost for good (the ROM jumps to the game-over screen and never returns: the
        /// rest of the clean-up is skipped). Note what does NOT count as lost: any creature with the party flag standing - allies that joined the fight included - and any party creature that fled.
        public bool CombatCleanup()
        {
            T("cleanup");
            FledMask = 0; GroupMask = 0;
            S.CombatMode = 3;
            if (Scripted9858 != 0) { Scripted9927 = 1; Scripted9930 = 0xFF; }
            bool anyParty = false, anyFled = false, anyUp = false;
            Gs(0xD50B, 0xFF); Flag9DBD = 0;
            for (int i = 0; i < S.Records.Length; i++)                                            // 0x16EC0: timed item bits (flags 0x30) are gone
                for (int k = 0; k < 13; k++) S.Records[i][0x54 + 10 * k + 5] &= 0xCF;
            if (DemoFlag == 0)
            {
                if (SoloFlag != 0 && SoloMember < S.SlotCount)
                {
                    var s = S.Slots[SoloMember];
                    if ((sbyte)s[0] < 0)
                    {
                        Flag9DBD = 0x80;
                        int d0 = s[0] & 0x3F;
                        if (d0 == 5) { FledMask |= (byte)(1 << SoloMember); d0 = 1; } else d0 = 0x84;
                        s[0] = (byte)d0;
                    }
                }
                for (int i = 0; i < 8 && i < S.SlotCount; i++)
                {
                    var s = S.Slots[i];
                    s[1] &= 0xFB; s[1] &= 0xEF;
                    int d0 = s[0];
                    if (d0 == 0) continue;
                    if ((sbyte)d0 >= 0 && (s[1] & 1) != 0) { anyParty = true; anyUp = true; Gs(0xD50B, 0); }
                    d0 &= 0xBF;
                    if ((d0 & 0xF) == 5) { FledMask |= (byte)(1 << i); anyFled = true; d0 = 1; anyParty = true; }
                    s[0] = (byte)d0;
                }
                for (int i = 7; i >= 0; i--)
                    if (i < S.SlotCount && S.Slots[i][0] != 0) GenesisStats.RecomputeSlot(Rom, S.Slots[i], S.Records[i], Gb(A49A), Gb(A499), Mode97AE != 0);
                if (!anyParty) { GameOver?.Invoke(); return true; }                                  // 0x160D8: nobody standing or fled: the game is over (the ROM jumps and never comes back)
                if (!anyUp)
                {
                    Flag9DBD = 0xFF;
                    for (int i = 0; i < 8 && i < S.SlotCount; i++) if ((S.Slots[i][0] & 0x80) != 0) S.Slots[i][0] = 0;
                }
            }
            if (Gb(0xD50E) != 0) { for (int i = 0xD57E; i < 0xD582; i++) Gs(i, 0); }
            if (DemoFlag == 0)
            {
                ScriptedTreasure();
                Tally();
                Victory();
                if ((sbyte)Scripted9930 < 0 || Gb(0xD50E) != 0) { MedicalAftermath(); Aftermath?.Invoke(); }
                if (PoolCount != 0) LootScreen?.Invoke();
                for (int i = 0xD57E; i < 0xD582; i++) Gs(i, 0);                                      // 0x16190
            }
            if ((sbyte)Scripted9930 < 0) { ScriptedFightEnd?.Invoke(); Scripted9930 = 0; }
            SavedMode = PrevMode;
            if (Scripted9858 != 0) Scripted9858 = 0;
            return false;
        }

        /// 0x76C4: the first party member's first backpack item with id `id` (record index order); null when nobody has one (or while the party is player driven, [0x97AE]).
        byte[] FindPartyItem(int id, out int at)
        {
            at = 0;
            if (Mode97AE != 0) return null;
            for (int j = 0; j < 8 && j < S.Records.Length; j++)
            {
                if ((j < S.SlotCount ? S.Slots[j][0] : 0) == 0) continue;
                var rec = S.Records[j];
                for (int k = 0; k < 13; k++) if (rec[0x54 + 10 * k] == id) { at = 0x54 + 10 * k; return rec; }
            }
            return null;
        }

        void PressC() { Gs(0xD593, 0); if (ContinuePrompt != null) ContinuePrompt(); else if (Pad != null) ChoicePrompt(2); }

        /// 0x16B96: what becomes of the party's wounded after a fight. A creature with effect 3 (Buck Rogers) is stood up with 45 HP. Then every party creature (flag bit 0, allies too) that
        /// is hurt, dying or down is looked at: a creature in status 6 needs a healer (a standing party member with first-aid points, record +0x32: d10 + 4 within the points) or is
        /// dead (0x87, HP 0, and listed); everybody else is healed by the best of the standing healers' rolls: d24 (d48 after a scripted fight) + 2 x (4 x) the points - 12, or d6 for a
        /// medicine skill roll (+0x3B, d10 within it); the dying (3, 4, 6) come back as 1 if healed or fall to 0x84 (unconscious) at HP 0. If anything changed the new statuses are written,
        /// the HP raised (never past the maximum) and each listed dead creature is brought back to 0x84 with a revive item (id 0x1F, one used each) while there are any.
        public void MedicalAftermath()
        {
            T("medical");
            for (int d7 = 7; d7 >= 0; d7--)                                                         // 0x16B9E: the first creature with effect 3 (highest slot) is back on its feet
            {
                if (d7 >= S.SlotCount || S.Slots[d7][0] == 0) continue;
                var rec = S.Records[S.Slots[d7][2]]; bool has = false;
                for (int k = 0; k < 10; k++) if (rec[0x43 + k] == 3) has = true;
                if (has) { S.Slots[d7][0] = 1; S.Slots[d7][0xE] = 0x2D; break; }
            }
            var newSt = new int[8]; var heal = new int[8]; var dead = new List<int>(); bool changed = false;
            for (int i = 0; i < 8; i++)
            {
                int d7 = 7 - i;
                if (i >= S.SlotCount || i >= S.Records.Length) continue;
                var a3 = S.Slots[i]; var a2 = S.Records[i];
                int st = a3[0]; newSt[d7] = st;
                if (st == 0 || (a3[1] & 1) == 0) continue;
                int low = st & 0x3F;
                if (low == 6)
                {
                    changed = true;
                    bool saved = false;
                    for (int j = 0; j < 8 && j < S.SlotCount && j < S.Records.Length; j++)
                    {
                        int sj = S.Slots[j][0];
                        if (sj == 0 || (sj & 0x80) != 0) continue;
                        int pts = S.Records[j][0x32];
                        if (pts == 0) continue;
                        if (((Rng.Roll(10) + 4) & 0xFF) <= pts) { saved = true; break; }
                    }
                    if (!saved) { dead.Add(a3[2]); newSt[d7] = 0x87; a3[0xE] = 0; continue; }
                }
                else
                {
                    if ((sbyte)st >= 0 && a3[0xE] == a2[0x2E]) continue;
                    if (low == 7 || low == 2) continue;
                }
                for (int j = 0; j < 8 && j < S.SlotCount && j < S.Records.Length; j++)             // 0x16C86: the healers' rolls
                {
                    int sj = S.Slots[j][0];
                    if (sj == 0 || (sj & 0x80) != 0) continue;
                    var hr = S.Records[j];
                    int pts = hr[0x32];
                    if (pts != 0)
                    {
                        pts = (pts << 1) & 0xFF; int sides = 0x18;
                        if ((sbyte)Scripted9930 < 0) { pts = (pts << 1) & 0xFF; sides = 0x30; }
                        int r = (Rng.Roll(sides) + pts) & 0xFF; r = (r - 0xC) & 0xFF;
                        if ((sbyte)r >= 0 && r > heal[d7]) heal[d7] = r;
                    }
                    int med = hr[0x3B];
                    if (med != 0 && Rng.Roll(10) <= med) { int r = Rng.Roll(6); if (r > heal[d7]) heal[d7] = r; }
                }
                int s2 = a3[0];
                if ((sbyte)s2 >= 0) newSt[d7] = 1;
                else
                {
                    int l2 = s2 & 0x3F;
                    if (l2 == 4 || l2 == 6 || l2 == 3) { a3[0xE] = 0; newSt[d7] = heal[d7] != 0 ? 1 : 0x84; }
                    else newSt[d7] = 1;
                }
                changed = true;
                int room = (a2[0x2E] - a3[0xE]) & 0xFF;
                if (room < heal[d7]) heal[d7] = room;
            }
            if (!changed) return;
            for (int i = 0; i < 8 && i < S.SlotCount; i++) { S.Slots[i][0] = (byte)newSt[7 - i]; S.Slots[i][0xE] = (byte)(S.Slots[i][0xE] + heal[7 - i]); }
            PressC();
            bool any = false;
            for (int k = dead.Count - 1; k >= 0; k--)                                                 // 0x16E2C: revive items
            {
                int at; var owner = FindPartyItem(0x1F, out at);
                if (owner == null) break;
                owner[at + 8]--; if (owner[at + 8] == 0) owner[at] = 0;
                int idx = dead[k]; if (idx < S.SlotCount) { S.Slots[idx][0] = 0x84; S.Slots[idx][0xE] = 0; }
                any = true;
            }
            if (any) PressC();
        }
    }
}
