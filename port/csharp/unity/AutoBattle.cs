// AutoBattle.cs -- the thin adapter between the ported combat logic and a front end: sets up a fight from monster-file ids, lets the computer play every creature (the player-input
// hooks Pad / MenuChoice are the real UI's job) and records a BattleFrame at every creature's turn so a viewer can replay it. No UnityEngine here, so it also runs under mono (tests).
using System;
using System.Collections.Generic;

namespace BuckRogersGenesis
{
    /// The board at one moment: the 21x21 tile map (bit 7 = occupied), and per creature its place, hit points and state.
    public sealed class BattleFrame
    {
        public int Round, Actor;
        public bool Indoor;                          // the ground set of the fight (terrain art mode 5 instead of 4)
        public byte[] Tiles;
        public int[] X, Y, Hp, Side, Status, Id, Facing, Size, AnimSet;   // AnimSet = the token table's animation set (0 / 1 / 2)   // Facing = slot byte +0x10 (0..7), Size = record size type (1 normal, 2 tall, 3 wide)
    }

    public static class AutoBattle
    {
        /// Runs a fight: `party` ids fight `monsterIds` (one group per entry, `count` creatures each) on outdoor ground of `areaType` (0..10). Party members are monster-file records too (there are
        /// no stored player characters in the data). Returns the frames; `winner` is 0 = the monsters, 1 = the party.
        public static List<BattleFrame> Run(RomView rom, MonsterFile file, int[] party, int[] monsterIds, int count, int seed, int areaType, out int winner)
        {
            return Run(rom, file, party, monsterIds, count, seed, areaType, false, out winner);
        }

        /// As above; `indoor` = the indoor ground (screen mode 5, area types 0..12) instead of the outdoor ground (mode 4, types 0..10).
        public static List<BattleFrame> Run(RomView rom, MonsterFile file, int[] party, int[] monsterIds, int count, int seed, int areaType, bool indoor, out int winner)
        {
            var s = new CombatState { SlotCount = party.Length, Slots = new byte[0][], Records = new byte[11][] };
            var words = new ushort[256]; var rnd = new Random(seed); for (int i = 0; i < 256; i++) words[i] = (ushort)rnd.Next(65536);
            var x = new TurnContext { S = s, Rom = rom, Monsters = file, Rng = GenesisRng.FromSeedWords(words) };
            for (int i = 0; i < party.Length; i++)
            {
                x.LoadCombatant(party[i], 1, party[i], i);
                s.Records[i][0x52] |= 1; s.Slots[i][1] |= 1;                       // party side
            }
            foreach (int id in monsterIds) x.AddMonsters(id, count);
            for (int r = 0; r < 11; r++) if (s.Records[r] == null) s.Records[r] = new byte[214];
            x.D97DC = (byte)(indoor ? 0xA8 : 0xA2); x.AreaType = (byte)(areaType % (indoor ? 13 : 11)); x.Facing = (byte)rnd.Next(4);
            x.ContinuePrompt = () => 0; x.MapPixelsX = 504; x.MapPixelsY = 504; x.RetreatPrompt = () => 0;
            var frames = new List<BattleFrame>();
            x.ActorVisible = () => { frames.Add(Snap(x)); return true; };
            winner = 0;
            if (!x.CombatSetup()) return frames;
            for (int i = 0; i < s.SlotCount; i++) s.Slots[i][1] |= 0x80;           // the computer plays everybody
            x.ForgetVictims();
            x.CombatRounds();
            frames.Add(Snap(x));
            x.CountLiving();
            winner = s.LivingBySide[1] > 0 ? 1 : 0;
            return frames;
        }

        static BattleFrame Snap(TurnContext x)
        {
            var s = x.S; int n = s.SlotCount;
            var f = new BattleFrame { Indoor = x.D97DC == 0xA8, Round = x.Round, Actor = x.Actor, Tiles = (byte[])s.Tiles.Clone(), X = new int[n], Y = new int[n], Hp = new int[n], Side = new int[n], Status = new int[n], Id = new int[n], Facing = new int[n], Size = new int[n], AnimSet = new int[n] };
            for (int i = 0; i < n; i++)
            {
                var sl = s.Slots[i]; f.X[i] = sl[0x12]; f.Y[i] = sl[0x13]; f.Hp[i] = sl[0xE]; f.Side[i] = sl[1] & 1; f.Status[i] = sl[0]; f.Id[i] = s.Records[sl[2]][0x42]; f.Facing[i] = sl[0x10] & 7; f.Size[i] = s.Records[sl[2]][0x23]; f.AnimSet[i] = TokenFrames.AnimationSet(x.Rom, f.Id[i]);
            }
            return f;
        }
    }
}
