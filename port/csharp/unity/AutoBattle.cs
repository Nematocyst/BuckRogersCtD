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
        public int Mode = 4;                         // the ground set of the fight: screen mode 4 outdoor, 5 indoor, 6 dungeon (selects the terrain art atlas)
        public byte[] Tiles;
        public int[] X, Y, Hp, Side, Status, Id, Facing, Size, AnimSet;   // AnimSet = the token table's animation set (0 / 1 / 2)   // Facing = slot byte +0x10 (0..7), Size = record size type (1 normal, 2 tall, 3 wide)
    }

    public static class AutoBattle
    {
        /// Runs a fight: `party` ids fight `monsterIds` (one group per entry, `count` creatures each) on outdoor ground of `areaType` (0..10). Party members are monster-file records too (there are
        /// no stored player characters in the data). Returns the frames; `winner` is 0 = the monsters, 1 = the party.
        public static List<BattleFrame> Run(RomView rom, MonsterBinFile file, int[] party, int[] monsterIds, int count, int seed, int areaType, out int winner)
        {
            return Run(rom, file, party, monsterIds, count, seed, areaType, 4, out winner);
        }

        /// As above; `mode` = 4 outdoor ground (types 0..10), 5 indoor ground (types 0..12), 6 a dungeon arena built by the ported 0xB100 from a generated map (walls, rooms).
        public static List<BattleFrame> Run(RomView rom, MonsterBinFile file, int[] party, int[] monsterIds, int count, int seed, int areaType, int mode, out int winner)
        {
            return Run(rom, file, party, null, monsterIds, count, seed, areaType, mode, out winner);
        }

        /// As above with the party members' token keys (record byte +0x42): 0x80 | sheet for a party sheet (see TokenFrames.PregenKeys), or null = the monster id of the member.
        public static List<BattleFrame> Run(RomView rom, MonsterBinFile file, int[] party, int[] partyKeys, int[] monsterIds, int count, int seed, int areaType, int mode, out int winner)
        {
            bool indoor = mode == 5;
            var s = new CombatState { SlotCount = party.Length, Slots = new byte[0][], Records = new byte[11][] };
            var words = new ushort[256]; var rnd = new Random(seed); for (int i = 0; i < 256; i++) words[i] = (ushort)rnd.Next(65536);
            var x = new TurnContext { S = s, Rom = rom, Monsters = file, Rng = GenesisRng.FromSeedWords(words) };
            for (int i = 0; i < party.Length; i++)
            {
                x.LoadCombatant(party[i], 1, partyKeys != null ? partyKeys[i] : party[i], i);
                s.Records[i][0x52] |= 1; s.Slots[i][1] |= 1;                       // party side
            }
            foreach (int id in monsterIds) x.AddMonsters(id, count);
            for (int r = 0; r < 11; r++) if (s.Records[r] == null) s.Records[r] = new byte[214];
            x.D97DC = (byte)(mode == 6 ? 0xA0 : indoor ? 0xA8 : 0xA2); if (mode == 6) DungeonMap(x, rnd); x.AreaType = (byte)(areaType % (indoor ? 13 : 11)); x.Facing = (byte)rnd.Next(4);
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

        /// A made-up dungeon map for the arena builder: walled room with a few blocks; random tile classes.
        static void DungeonMap(TurnContext x, Random rnd)
        {
            for (int i = 0; i < 256; i++)
            {
                int px = i % 16, py = i / 16;
                bool wall = px == 0 || py == 0 || px == 15 || py == 15 || (rnd.Next(100) < 12 && px > 2 && px < 13 && py > 2 && py < 13);
                x.MapWalls[i] = (byte)(wall ? 0x80 : 0);
                x.MapLayerA[i] = (byte)(rnd.Next(4) | (rnd.Next(4) << 4)); x.MapLayerB[i] = (byte)(rnd.Next(4) | (rnd.Next(4) << 4));
            }
            for (int k = 0; k < 16; k++) { x.TileClassA[k] = (byte)(rnd.Next(4) == 0 ? 0x80 : rnd.Next(3)); x.TileClassB[k] = (byte)rnd.Next(5); }
            x.MapX = 8; x.MapY = 8;
        }

        static BattleFrame Snap(TurnContext x)
        {
            var s = x.S; int n = s.SlotCount;
            var f = new BattleFrame { Mode = x.ScreenModeOfFight, Round = x.Round, Actor = x.Actor, Tiles = (byte[])s.Tiles.Clone(), X = new int[n], Y = new int[n], Hp = new int[n], Side = new int[n], Status = new int[n], Id = new int[n], Facing = new int[n], Size = new int[n], AnimSet = new int[n] };
            for (int i = 0; i < n; i++)
            {
                var sl = s.Slots[i]; f.X[i] = sl[0x12]; f.Y[i] = sl[0x13]; f.Hp[i] = sl[0xE]; f.Side[i] = sl[1] & 1; f.Status[i] = sl[0]; f.Id[i] = s.Records[sl[2]][0x42]; f.Facing[i] = sl[0x10] & 7; f.Size[i] = s.Records[sl[2]][0x23]; f.AnimSet[i] = TokenFrames.AnimationSet(x.Rom, f.Id[i]);
            }
            return f;
        }
    }
}
