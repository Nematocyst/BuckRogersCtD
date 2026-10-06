// BattleSequence.cs -- the attack animation between two BattleFrames (the board at the start of one creature's turn and at the start of the next): what the creature that acted
// did (moved, then if somebody lost hit points or went down: turned to the victim and showed its aim pose), how the victims reacted (hit pose, or the death sequence) and when
// everything is over. The poses and their order are the ROM's (docs/07-graphics-scoping.md: 0x1074A attack, 0x115E0 hit, 0x11B30 death); the timings are in the ROM's delay
// ticks (about 1/60 s). No UnityEngine here, so it is tested under mono. The ROM does not record the victims; they are found by comparing the two boards.
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    public sealed class BattleSequence
    {
        public const int MoveTicks = 12, AimTicks = 24, IdleTicks = 6;
        readonly BattleFrame a, b;
        readonly int moveEnd, aimEnd;
        readonly Dictionary<int, bool> victims = new Dictionary<int, bool>();   // creature -> goes down
        public float Duration { get; private set; }                              // in ticks
        public bool Attack { get { return victims.Count > 0; } }

        static bool Down(BattleFrame f, int i) { return (f.Status[i] & 0x80) != 0; }

        public BattleSequence(BattleFrame from, BattleFrame to)
        {
            a = from; b = to;
            int n = System.Math.Min(a.X.Length, b.X.Length);
            for (int i = 0; i < n; i++)
            {
                if (a.Status[i] == 0 || (a.Status[i] & 0x40) != 0) continue;
                bool wasDown = Down(a, i), isDown = Down(b, i);
                if (!wasDown && (b.Hp[i] < a.Hp[i] || isDown)) victims[i] = isDown;
            }
            bool moved = a.Actor >= 0 && a.Actor < n && (a.X[a.Actor] != b.X[a.Actor] || a.Y[a.Actor] != b.Y[a.Actor]);
            moveEnd = moved ? MoveTicks : 0;
            if (victims.Count == 0) { aimEnd = moveEnd; Duration = moved ? MoveTicks : IdleTicks; return; }
            aimEnd = moveEnd + AimTicks;
            float end = aimEnd;
            foreach (var kv in victims)
                end = System.Math.Max(end, aimEnd + (kv.Value ? Reaction(kv.Key, true).Length * TokenFrames.DeathTicks(b.Size[kv.Key]) : TokenFrames.HitTicks));
            Duration = end;
        }

        int[] Reaction(int i, bool dies) { return dies ? TokenFrames.Death(b.AnimSet[i]) : new[] { TokenFrames.HitPose }; }

        /// The creature's state `t` ticks into the sequence: position in cells (fractional while it slides), the sheet frame, whether it is mirrored, whether it is drawn at all,
        /// and the hit points to show.
        public void Sample(float t, int i, out float x, out float y, out int frame, out bool mirrored, out bool visible, out int hp)
        {
            bool done = t >= Duration;
            var f = (done || (i == a.Actor && t >= moveEnd) || t >= aimEnd) ? b : a;
            x = f.X[i]; y = f.Y[i]; hp = t >= aimEnd && victims.ContainsKey(i) || done ? b.Hp[i] : a.Hp[i];
            visible = true; mirrored = false;
            if (i == a.Actor && moveEnd > 0 && t < moveEnd)
            {
                float k = t / moveEnd; x = a.X[i] + (b.X[i] - a.X[i]) * k; y = a.Y[i] + (b.Y[i] - a.Y[i]) * k;
            }
            frame = TokenFrames.Idle((done || i == a.Actor ? b : a).Facing[i], Down(f, i), out mirrored);
            if (!done && Attack)
            {
                if (i == a.Actor && t >= moveEnd && t < aimEnd) frame = TokenFrames.Aim(b.Facing[i], out mirrored);
                bool dies;
                if (t >= aimEnd && victims.TryGetValue(i, out dies))
                {
                    float rt = t - aimEnd;
                    if (!dies) { if (rt < TokenFrames.HitTicks) frame = TokenFrames.HitPose; mirrored = false; }
                    else
                    {
                        var seq = Reaction(i, true); int step = (int)(rt / TokenFrames.DeathTicks(b.Size[i]));
                        if (step < seq.Length) { frame = seq[step]; mirrored = false; if (frame == TokenFrames.Vanish) visible = false; }
                    }
                }
            }
            if (visible && Down(b, i) && b.AnimSet[i] >= 1 && (done || !victims.ContainsKey(i)) && Down(a, i)) visible = false;   // an earlier casualty of a vanishing kind stays gone
            if (visible && done && Down(b, i) && b.AnimSet[i] >= 1) visible = false;                                           // so does one that has just finished its sequence
        }
    }
}
