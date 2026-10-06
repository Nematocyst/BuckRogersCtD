// GenesisMonsterAi.cs -- what a monster does on its turn (ROM 0xEF64 and the routines it calls), ported piece by piece and verified against the ROM
// (port/tests/MonsterTests.cs; vectors from the real routines run in a 68000 emulator, see port/tools/gen_monster_vectors.py).
//
// The small RAM areas the routines share are kept as byte images at their real addresses so the port can be compared byte for byte with the ROM:
//   G  = 0xD48E..0xD5FF  combat scratch globals: [0xD48E..0xD499] damage list of the attack in progress (12 entries; entry 8 shares [0xD496], 9 [0xD497], 11 [0xD499]), [0xD499/A] side attack modifiers, [0xD49C..] temporary effect list (3 bytes: slot, effect, ?),
//                        [0xD4FF] skip-blockers, [0xD500] side filter, [0xD501..3] line-of-fire flags, [0xD504] tile-0 blocks,
//                        [0xD505] mode, [0xD506] target-list count, [0xD511..] attack to-hit/status scratch, [0xD513] current victim
//   Ca = 0xCA20..0xCA7F  [0xCA20] current actor slot, [0xCA22..] target list: 3 bytes per entry (slot, distance, octant)

using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public CombatState S;
        public GenesisRng Rng;
        public Func<int, int> TerrainFlags;                   // terrain flag byte by terrain index (see GenesisLineOfFire.Run)
        public RomView Rom;                                   // ROM tables (weapon table, direction table, ...)
        public byte[] G = new byte[0x172];
        public byte[] Ca = new byte[0x60];

        public const int GBase = 0xD48E, CaBase = 0xCA20;
        public int Actor { get { return Ca[0]; } set { Ca[0] = (byte)value; } }
        public byte D4FF { get { return G[0xD4FF - GBase]; } set { G[0xD4FF - GBase] = value; } }
        public byte D500 { get { return G[0xD500 - GBase]; } set { G[0xD500 - GBase] = value; } }
        public byte D501 { get { return G[0xD501 - GBase]; } set { G[0xD501 - GBase] = value; } }
        public byte D502 { get { return G[0xD502 - GBase]; } set { G[0xD502 - GBase] = value; } }
        public byte D503 { get { return G[0xD503 - GBase]; } set { G[0xD503 - GBase] = value; } }
        public byte D504 { get { return G[0xD504 - GBase]; } set { G[0xD504 - GBase] = value; } }
        public byte D506 { get { return G[0xD506 - GBase]; } set { G[0xD506 - GBase] = value; } }
        public byte[] Nav = new byte[0x100];                  // 0x6CAE..0x6DAD: [0x6CAE] stale byte the search reads, [0x6CB0..] path (directions, 0xFF)
        public Func<int> WaveInit;                            // the uninitialised stack word the ROM's search starts its wave counter from (see GenesisAi.FindPath)
        public System.Collections.Generic.List<string> Trace0;   // optional event log used by the tests (names of the ROM routines entered)
        public bool Enumerated;                               // the ROM's enumeration (0x15C2C) leaves its candidate counter in register d6, which the turn controller reuses (see RunTurn)
        void T(string n) { if (Trace0 != null) Trace0.Add(n); }
        public byte Mode97AE;                                 // [0x97AE]: party members are driven by the player (no automatic weapon choice)
        public byte D97DC;                                    // [0x97DC] option bits (bit 4: no explosive weapon use)
        public byte[] Slot(int i) { return S.Slots[i]; }

        // ------------------------------------------------------------------------------------ 0x15B34 / 0x15B5A
        /// 0x15B34: line of fire from creature `from` (its reference cell) to the current actor's reference cell; the flags land in [0xD501..3].
        public GenesisLineOfFire.Trace LineOfFireTo(int from, int range)
        {
            var a = S.Slots[from]; var b = S.Slots[Actor];
            return Trace(a[0x12], a[0x13], b[0x12], b[0x13], range);
        }

        /// 0x15B5A with the globals: [0xD504] (cleared afterwards) / [0xD4FF] as inputs, [0xD501..3] as outputs.
        public GenesisLineOfFire.Trace Trace(int x0, int y0, int x1, int y1, int range)
        {
            var t = GenesisLineOfFire.Run(S.Tiles, TerrainFlags, x0, y0, x1, y1, range, D504 != 0, D4FF != 0);
            D504 = 0;                                          // one-shot: the ROM clears it at the end of every trace (0x15C20)
            D501 = (byte)(t.Flag501 ? 0xFF : 0); D502 = (byte)(t.Flag502 ? 0xFF : 0); D503 = (byte)(t.Flag503 ? 0xFF : 0);
            return t;
        }

        // ------------------------------------------------------------------------------------ 0x15C2C: targets in sight
        /// Builds the target list at [0xCA22] for creature `actor`: every living creature whose side matches [0xD500] (bit 7 set = any side, the actor included),
        /// that one of the actor's cells can see within `range` (line of fire). One entry per creature (slot, nearest distance, octant of the first cell pair that saw it),
        /// then sorted by distance (selection sort that also swaps equal distances). [0xD506] = number of entries; [0xD500] is set to 0xFF afterwards.
        public void EnumerateTargets(int actor, int range)
        {
            T("enum"); Enumerated = true;
            D506 = 0;
            foreach (var ac in Cells(actor))
            {
                for (int cand = 0; cand < S.SlotCount; cand++)
                {
                    int st = S.Slots[cand][0];
                    if (st == 0 || (st & 0xC0) != 0) continue;
                    if ((D500 & 0x80) == 0 && (((S.Slots[cand][1] ^ D500) & 1) != 0)) continue;
                    foreach (var cc in Cells(cand))
                    {
                        var t = Trace(ac.x, ac.y, cc.x, cc.y, range);
                        if (!t.Clear) continue;
                        int n = D506, k;
                        for (k = 0; k < n; k++)
                            if (Ca[2 + 3 * k] == cand) break;
                        if (k < n) { if (t.Distance < 0) { /* distance is a byte in the ROM */ } if ((byte)t.Distance < Ca[2 + 3 * k + 1]) Ca[2 + 3 * k + 1] = (byte)t.Distance; continue; }
                        Ca[2 + 3 * n] = (byte)cand; Ca[2 + 3 * n + 1] = (byte)t.Distance;
                        Ca[2 + 3 * n + 2] = (byte)GenesisCombat.Octant(cc.x, cc.y, ac.x, ac.y);
                        D506 = (byte)(n + 1);
                    }
                }
            }
            SortTargets();
            D500 = 0xFF;
        }

        /// 0x159C4
        void SortTargets()
        {
            int n = (D506 - 1) & 0xFF;
            if ((sbyte)n <= 0) return;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j <= n; j++)
                    if (Ca[2 + 3 * i + 1] >= Ca[2 + 3 * j + 1])
                        for (int b = 0; b < 3; b++) { byte tmp = Ca[2 + 3 * i + b]; Ca[2 + 3 * i + b] = Ca[2 + 3 * j + b]; Ca[2 + 3 * j + b] = tmp; }
        }

        /// 0x142B8: the cells a creature covers: its own, plus the east cell if wide (record type 3) or the south cell if tall (type 2).
        System.Collections.Generic.IEnumerable<(int x, int y)> Cells(int slot)
        {
            int x = S.Slots[slot][0x12], y = S.Slots[slot][0x13];
            yield return (x, y);
            int t = S.RecordSizeType[S.Slots[slot][2]];
            if (t == 3) yield return (x + 1, y);
            else if (t == 2) yield return (x, y + 1);
        }

        // ------------------------------------------------------------------------------------ 0xE812: choose a target
        /// Keeps the current target (+0x17) when it is a living creature of the other side that is still in line of fire (range 100); otherwise picks a new one:
        /// the nearest-first list of visible enemies, then one of the nearer half (index = random(count/2 + 1)); if nobody is visible even with blockers ignored
        /// ([0xD4FF]) the target becomes 0xFF (none).
        public void SelectTarget()
        {
            T("select");
            var me = S.Slots[Actor];
            int enemy = (me[1] ^ 1) & 1;
            int t = (sbyte)me[0x17];
            if (t >= 0)
            {
                var ts = S.Slots[t];
                if (ts[0] != 0 && (ts[0] & 0xC0) == 0 && (((ts[1] ^ enemy) & 1) == 0))
                    if (LineOfFireTo(t, 100).Clear) return;
            }
            D500 = (byte)enemy;
            EnumerateTargets(Actor, 100);
            if (D506 == 0)
            {
                D500 = (byte)enemy; D4FF = 0xFF;
                EnumerateTargets(Actor, 100);
                D4FF = 0;
                if (D506 == 0) { me[0x17] = 0xFF; return; }
            }
            int pick = Rng.Next(((D506 >> 1) + 1) & 0xFF);
            me[0x17] = Ca[2 + 3 * pick];
        }

        // ------------------------------------------------------------------------------------ 0x15D8A: find a way to the goal
        /// Breadth-first search from the current actor (see GenesisAi.FindPath; the goal depends on [0xD505]): the found creature becomes the actor's target
        /// (+0x17) and the directions are left at [0x6CB0..] ended by 0xFF (just 0xFF when nothing was found).
        public GenesisAi.PathResult Navigate()
        {
            T("nav");
            var w = new CombatWorld
            {
                Tiles = S.Tiles, TerrainFlags = TerrainFlags, SlotCount = S.SlotCount, Mode = Gb(0xD505), Count506 = D506,
                Flags0 = new int[S.SlotCount], Flags1 = new int[S.SlotCount], X = new int[S.SlotCount], Y = new int[S.SlotCount], SizeType = new int[S.SlotCount], Target17 = new int[S.SlotCount]
            };
            for (int i = 0; i < S.SlotCount; i++)
            {
                var sl = S.Slots[i];
                w.Flags0[i] = sl[0]; w.Flags1[i] = sl[1]; w.X[i] = sl[0x12]; w.Y[i] = sl[0x13]; w.SizeType[i] = S.RecordSizeType[sl[2]]; w.Target17[i] = sl[0x17];
            }
            w.Ca22 = new byte[Ca.Length - 2]; Array.Copy(Ca, 2, w.Ca22, 0, w.Ca22.Length);
            var r = GenesisAi.FindPath(w, Actor, WaveInit == null ? 0 : WaveInit(), Nav[0]);
            S.Slots[Actor][0x17] = (byte)w.Target17[Actor];
            Nav[0] = r.Visited[440];
            for (int i = 0; i < r.Path.Count; i++) Nav[2 + i] = (byte)r.Path[i];
            Nav[2 + r.Path.Count] = 0xFF;
            foreach (var list in w.OccupantLog)                           // every occupant lookup rewrote the list at [0xD5F8]: replay them
            {
                int n = 0; foreach (var o in list) G[A5F8 - GBase + n++] = (byte)o;
                G[A5F8 - GBase + n] = 0xFF;
            }
            return r;
        }
    }
}
