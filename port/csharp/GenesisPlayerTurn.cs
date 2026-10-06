// GenesisPlayerTurn.cs -- the turn of a creature the player controls (ROM 0xF2AE): the command menu, the walking mode with its undo, the attack mode with
// the target cursor and the rescue mode of a healer. The player's input arrives through two callbacks that stand for the ROM's input routines:
//   MenuChoice  (0x1391A)  the command menu: 0 attack, 1 move, 2 auto (the computer plays this creature from now on), 3 wait (healer: rescue), 4 end the turn;
//                          a negative answer (cancel) opens the character sheet (CharacterSheet, 0xFDC8).
//   Pad         (0xF1B66)  one reading of the control pad, [0xD8FC]: bit 0 up, 1 down, 2 left, 3 right, 4 next target, 5 confirm, 6 character sheet, 7 cancel.
// Everything else (cursor sprites, scrolling, text boxes, sounds) is not part of the port; the game state changes are verified against the ROM in
// port/tests/MonsterTests.cs (the vectors answer the same scripted menu / pad inputs).
using System;

namespace BuckRogersGenesis
{
    public sealed partial class TurnContext
    {
        public Func<int> MenuChoice;
        public Func<int> Pad;
        public Action CharacterSheet;
        public int MapPixelsX = 504, MapPixelsY = 504;      // [0xB400], [0xB402]: the cursor stays inside the map
        public int Polls;                                   // safety net: pad readings of the current turn

        /// 0xF1EA: the creatures the attack cursor can cycle through: every living creature of the monster side (flag bit 0 clear).
        void BuildTargetList()
        {
            int n = 0;
            for (int i = 0; i < S.SlotCount; i++)
            {
                var sl = S.Slots[i];
                if (sl[0] == 0 || (sl[0] & 0xC0) != 0 || (sl[1] & 1) != 0) continue;
                Ca[2 + 3 * n++] = (byte)i;
            }
            D506 = (byte)n;
        }

        /// 0xF22C: the fallen party members the healer can go to (the same list 0xF28A builds; the healing points come from the record of slot number Actor).
        void BuildRescueList()
        {
            var rec = S.Records[Actor];
            FillRescueList(rec[0x32]);
        }

        /// 0x1432E with the second pass: who stands on cell (x,y). Returns the first living occupant (0xFF none); `fallen` = a creature that is down on the
        /// cell (status bit 7, reference cell only): the last party member with status 0x83, else the first one; -1 none.
        int OccupantsFull(int x, int y, out int fallen)
        {
            int first = Occupants(x, y);
            fallen = -1;
            for (int i = 0; i < S.SlotCount; i++)
            {
                var sl = S.Slots[i];
                if ((sl[1] & 4) != 0 || (sl[0] & 0x80) == 0) continue;
                if (sl[0x12] != (byte)x || sl[0x13] != (byte)y) continue;
                if (((sl[1] & 1) != 0 && sl[0] == 0x83) || fallen < 0) fallen = i;
            }
            return first;
        }

        /// 0x10006: the cursor moves by (MoveDx, MoveDy) pixels and stays inside the map.
        void MoveCursor()
        {
            CursorX = (short)(CursorX + MoveDx); CursorY = (short)(CursorY + MoveDy);
            CursorX = Math.Min(Math.Max(CursorX, 12), (short)(MapPixelsX - 12));
            CursorY = Math.Min(Math.Max(CursorY, 12), (short)(MapPixelsY - 12));
        }

        /// 0xF6DC: the cursor jumps to the next creature of the target list (the list index wraps).
        void CycleTarget(ref int index)
        {
            if (D506 == 0) return;
            var t = S.Slots[Ca[2 + 3 * index]];
            int tx = t[0x12] * 24 + 12, ty = t[0x13] * 24 + 12, sx = (short)CursorX, sy = (short)CursorY;
            for (int d1 = 1; d1 <= 32; d1++)                    // the cursor glides over in 32 steps; the last step vector stays behind in [0xB3F4/6]
            {
                MoveDx = (short)((((tx - sx) * d1) >> 5) + sx - (short)CursorX);
                MoveDy = (short)((((ty - sy) * d1) >> 5) + sy - (short)CursorY);
                MoveCursor();
            }
            index = (index + 1) & 0xFF;
            if (index >= D506) index = 0;
        }

        /// 0x116AC (mode 0): the actor turns towards the cursor cell.
        void FaceCursor()
        {
            var me = S.Slots[Actor];
            me[0x10] = (byte)GenesisCombat.Octant((CursorX & 0xFFFF) / 24, (CursorY & 0xFFFF) / 24, me[0x12], me[0x13]);
        }

        /// 0x103CE: the player confirmed the attack: nothing happens when the preparation found no way to attack ([0xD511] = 0).
        void ConfirmAttack()
        {
            if (Gb(A511) == 0) return;
            if (HoldsExplosive()) { AreaAttack(); return; }
            ExecuteAttack();
            Gs(0xD51D + Actor, Gb(A513));                       // the creature remembers its victim: the next turn's cursor starts there
        }

        /// 0x101A4: the cursor is on a fallen party member: a healer in rescue mode goes to it and treats it.
        void RescueSelect()
        {
            var me = S.Slots[Actor]; var rec = S.Records[me[2]];
            int fallen; OccupantsFull((CursorX & 0xFFFF) / 24, (CursorY & 0xFFFF) / 24, out fallen);
            if (fallen < 0 || fallen >= 8) return;
            me[0x17] = (byte)fallen;
            var v = S.Slots[fallen];
            if (v[0] != 0x83)
            {
                if (v[0] != 0x84 || rec[0x32] == 0) return;
                if (((Gb(0xD50A) >> (v[2] & 7)) & 1) != 0) return;
            }
            Gs(0xD505, 1); Rescue(); Gs(0xD505, 0);
        }

        /// 0xF1C0: debug key (walking mode, [0xCA21] = 0): every living monster is knocked out.
        void DebugKillAll()
        {
            for (int i = 0; i < S.SlotCount; i++)
            {
                var sl = S.Slots[i];
                if (sl[0] != 0 && (sl[0] & 0xC0) == 0 && (sl[1] & 1) == 0) sl[0] = 0x82;
            }
        }

        /// 0xF2AE: one player-controlled creature's turn, from the command menu until its time is spent (or it left the turn some other way:
        /// "auto", "end turn", "wait", a death during a step, ...). The current actor is [0xCA20].
        public void PlayerTurn()
        {
            T("manual");
            var me = S.Slots[Actor];
            moving = false;
            bool healer = AllyRescueCheck();
            int sel = 0xFF, mode = -1, tIdx = 0;                 // -a(a6), d7, -16(a6)
            bool explosive = false;                              // -14(a6) (stack garbage in the ROM until the attack mode sets it)
            int saveXY = 0, saveMp = 0;                          // -10(a6), -12(a6): the position / movement points of the walking mode's undo
            const int Menu = 0, Refresh = 1, Aim = 2, Poll = 3;
            int pc = Menu;
            while (true)
            {
                if (++Polls > 100000) throw new InvalidOperationException("manual turn does not end");
                switch (pc)
                {
                    case Menu: mode = -1; sel = 0xFF; pc = Refresh; break;                                   // 0xF2E0
                    case Refresh:                                                                            // 0xF2EA
                        MoveDx = 0; MoveDy = 0; CursorOnActor();
                        sel = Actor; pc = Aim; break;
                    case Aim:                                                                                // 0xF2F4
                        if (Gb(0xD51C) != 0) { CycleTarget(ref tIdx); Gs(0xD51C, 0); }
                        if (mode >= 1) PrepareAttack(sel);
                        pc = Poll; break;
                    case Poll:                                                                               // 0xF350
                        if (me[0x14] == 0) return;
                        if (mode == -1)
                        {
                            int c = MenuChoice();
                            if (c < 0)                                                                       // 0xF400: character sheet
                            {
                                if (Actor >= 8) break;
                                CharacterSheet?.Invoke();
                                if (me[0x14] == 0) return;
                                pc = Menu; break;
                            }
                            switch (c)
                            {
                                case 0:                                                                      // attack
                                    mode = 1; explosive = HoldsExplosive();
                                    BuildTargetList(); tIdx = 0;
                                    {
                                        int memo = Gb(0xD51D + Actor);
                                        if ((sbyte)memo >= 0)
                                            for (int k = D506 - 1; k >= 0; k--)
                                                if (Ca[2 + 3 * k] == memo) { tIdx = k; Gs(0xD51C, 0xFF); break; }
                                    }
                                    pc = Refresh; break;
                                case 1:                                                                      // move
                                    mode = 0; saveXY = me[0x12] << 8 | me[0x13]; saveMp = me[0x16];
                                    StartMotion(); pc = Refresh; break;
                                case 2: me[1] |= 0x80; return;                                               // auto
                                case 3:
                                    if (!healer) { if (me[0x14] == 1) me[0x14] = 0; else me[0x14] = 1; return; }
                                    mode = 2; BuildRescueList(); tIdx = 0; pc = Refresh; break;
                                case 4: EndTurn(); return;
                                default: throw new InvalidOperationException("menu answer " + c);
                            }
                            break;
                        }
                        // the cursor / walking modes
                        if (mode == 1) FaceCursor();
                        {
                            int accX = 0, accY = 0, d4 = 20;
                            int dx = 0, dy = 0, pad;
                            while (true)
                            {
                                if (Pad == null) throw new InvalidOperationException("no pad input");
                                if (++Polls > 100000) throw new InvalidOperationException("manual turn does not end");
                                pad = Pad() & 0xFF;
                                if ((pad & 0x80) != 0)                                                       // cancel
                                {
                                    if (mode == 0)
                                    {
                                        me[1] &= 0xFB; S.SetMarkers(Actor); moving = false;                  // 0xFA22, 0xF996
                                        S.ClearMarkers(Actor);
                                        me[0x12] = (byte)(saveXY >> 8); me[0x13] = (byte)saveXY; me[0x16] = (byte)saveMp;
                                        S.SetMarkers(Actor);                                                 // 0xF986
                                    }
                                    else explosive = false;
                                    mode = -1; pc = Refresh; break;
                                }
                                if ((pad & 0x40) != 0)                                                       // character sheet
                                {
                                    if (mode == 0) { me[1] &= 0xFB; S.SetMarkers(Actor); moving = false; mode = -1; }
                                    if (Actor >= 8) { pc = Poll; break; }
                                    CharacterSheet?.Invoke();
                                    if (me[0x14] == 0) return;
                                    pc = Menu; break;
                                }
                                if ((pad & 0x10) != 0)                                                       // next target
                                {
                                    if (mode != 0) { CycleTarget(ref tIdx); pc = Aim; break; }
                                    if (Ca[1] != 0) { pc = Poll; break; }
                                    DebugKillAll(); return;
                                }
                                if ((pad & 0x20) != 0)                                                       // confirm
                                {
                                    if (mode == 0) { me[1] &= 0xFB; S.SetMarkers(Actor); moving = false; mode = -1; pc = Refresh; break; }
                                    if (mode == 1) { ConfirmAttack(); pc = Poll; break; }
                                    mode = -1; RescueSelect(); pc = Refresh; break;
                                }
                                dy = (pad & 1) != 0 ? -1 : (pad & 2) != 0 ? 1 : 0;
                                dx = (pad & 4) != 0 ? -1 : (pad & 8) != 0 ? 1 : 0;
                                MoveDx = dx; MoveDy = dy;
                                if (mode == 0)
                                {
                                    accX |= dx & 0xFFFF; accY |= dy & 0xFFFF;
                                    if ((accX & accY) != 0 || --d4 < 0)
                                    {
                                        MoveDx = (short)accX; MoveDy = (short)accY;
                                        int r = MoveStep();
                                        if (r < 0) return;
                                        pc = Refresh; break;
                                    }
                                    continue;
                                }
                                // cursor movement: one cell (24 pixels) in the pressed direction
                                if ((MoveDx | MoveDy) != 0) for (int k = 0; k < 24; k++) MoveCursor();
                                MoveDx = 0; MoveDy = 0;
                                int fallen; int d0 = OccupantsFull((CursorX & 0xFFFF) / 24, (CursorY & 0xFFFF) / 24, out fallen);
                                if ((sbyte)d0 < 0 || mode == 2) { if (fallen >= 0) d0 = fallen; }
                                if (d0 != sel) { sel = d0; pc = Aim; break; }
                                if (!explosive) { pc = Poll; break; }
                                int d2 = Gb(A511), d3 = Gb(A518) << 8 | Gb(A518 + 1);
                                PrepareAttack(sel);
                                pc = (Gb(A511) != d2 || (Gb(A518) << 8 | Gb(A518 + 1)) != d3) ? Aim : Poll;
                                break;
                            }
                        }
                        break;
                }
            }
        }
    }
}
