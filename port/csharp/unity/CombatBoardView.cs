// CombatBoardView.cs -- Unity front end for AutoBattle: add it to an empty GameObject; it runs one computer-played fight on Start and replays it turn by turn on the screen
// (IMGUI): the 21x21 ground as the ROM's terrain art (Resources/BuckRogers/terrain/terrain_mode4 / 5 / 6.png, made by tools/export_terrain.py; coloured cells without it) and the creatures as their ROM sprites (Resources/BuckRogers/tokens/token_<monster id>.png, made by tools/export_tokens.py; the frame
// follows the facing / down rules of ROM 0xAD5A, see TokenFrames; attacks play as in the ROM: the attacker aims, victims show the hit pose or the death sequence, see BattleSequence) with hit points and a side marker (blue = party, red = monsters). Without the PNGs it falls back to coloured boxes.
// Needs Assets/Resources/BuckRogers/rom_tables.json and monster_file.bytes (see port/README.md). Not covered by the ROM tests (the logic it shows is; this view is checked by looking).
using System.Collections.Generic;
using UnityEngine;

namespace BuckRogersGenesis
{
    public sealed class CombatBoardView : MonoBehaviour
    {
        public int[] Party = { 0, 1, 2 };            // monster-file ids that fight for the party
        public int[] MonsterGroups = { 5, 11 };      // monster-file ids, one group each
        public int GroupSize = 2;
        public int Seed = 1;
        [Range(0, 10)] public int AreaType = 1;      // ground type (outdoor 0..10, indoor 0..12)
        [Range(4, 6)] public int GroundMode = 4;     // 4 outdoor, 5 indoor, 6 dungeon arena (terrain art atlas and ground generator)
        public float SecondsPerTurn = 0.4f;
        public int CellPixels = 24;

        List<BattleFrame> frames = new List<BattleFrame>();
        int shown;
        float elapsed;                   // seconds into the transition shown -> shown + 1
        BattleSequence seq;
        public float TickSeconds = 1f / 60f;   // the ROM's delay tick; raise to slow the animations down
        bool playing = true;
        string result = "";
        Texture2D white;
        Texture2D[] terrain = new Texture2D[3];
        bool terrainLoaded;

        /// The terrain atlas of a ground set (16 x 8 cells of 24 px, id = row * 16 + column, made by tools/export_terrain.py), or null.
        Texture2D Terrain(int mode)
        {
            if (!terrainLoaded)
            {
                terrainLoaded = true;
                terrain[0] = Resources.Load<Texture2D>("BuckRogers/terrain/terrain_mode4");
                terrain[1] = Resources.Load<Texture2D>("BuckRogers/terrain/terrain_mode5");
                terrain[2] = Resources.Load<Texture2D>("BuckRogers/terrain/terrain_mode6");
                foreach (var t in terrain) if (t != null) t.filterMode = FilterMode.Point;
            }
            return terrain[Mathf.Clamp(mode - 4, 0, 2)];
        }
        readonly Dictionary<int, Texture2D> sheets = new Dictionary<int, Texture2D>();

        /// The token strip of a monster id (18 frames in a row), or null when the PNG is not in Resources.
        Texture2D Sheet(int id)
        {
            Texture2D t;
            if (sheets.TryGetValue(id, out t)) return t;
            t = Resources.Load<Texture2D>("BuckRogers/tokens/token_" + id.ToString("00"));
            if (t != null) t.filterMode = FilterMode.Point;
            sheets[id] = t;
            return t;
        }

        void Start()
        {
            white = Texture2D.whiteTexture;
            var rom = GenesisRomTables.Load();
            var asset = Resources.Load<TextAsset>("BuckRogers/monster_file");
            if (rom == null || asset == null) { result = "missing Resources/BuckRogers/rom_tables.json or monster_file.bytes"; return; }
            Run(rom, MonsterFile.Parse(asset.bytes));
        }

        void Run(RomView rom, MonsterFile file)
        {
            frames = AutoBattle.Run(rom, file, Party, MonsterGroups, GroupSize, Seed, AreaType, GroundMode, out int winner);
            Show(0); playing = true;
            result = frames.Count == 0 ? "no fight (a side had no room on the battlefield)" : (winner == 1 ? "the party wins" : "the monsters win");
        }

        void Show(int index)
        {
            shown = Mathf.Clamp(index, 0, frames.Count - 1); elapsed = 0;
            seq = shown < frames.Count - 1 ? new BattleSequence(frames[shown], frames[shown + 1]) : null;
        }

        void Update()
        {
            if (!playing || frames.Count == 0) return;
            if (seq == null) { playing = false; return; }
            elapsed += Time.deltaTime;
            if (elapsed >= seq.Duration * TickSeconds * SpeedFactor) { Show(shown + 1); if (seq == null) playing = false; }
        }

        public float SpeedFactor = 1f;

        static Color TileColour(int tile)
        {
            int t = tile & 0x7F;                       // a stable colour per ground tile id
            float h = (t * 0.137f) % 1f;
            return Color.HSVToRGB(h, 0.35f, 0.55f);
        }

        void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old; }

        void OnGUI()
        {
            if (frames.Count == 0) { GUI.Label(new Rect(10, 10, 700, 24), result); return; }
            var f = frames[shown];
            int px = CellPixels, ox = 10, oy = 40;
            GUI.Label(new Rect(10, 8, 900, 24), $"round {f.Round}   turn {shown + 1}/{frames.Count}   {(shown == frames.Count - 1 ? result : "")}");
            var atlas = Terrain(f.Mode);
            for (int y = 0; y < 21; y++)
                for (int x = 0; x < 21; x++)
                {
                    int id = f.Tiles[y * 21 + x] & 0x7F;
                    if (atlas != null)
                    {
                        float aw = atlas.width, ah = atlas.height;
                        var uv = new Rect((id % 16) * 24f / aw, 1f - ((id / 16) + 1) * 24f / ah, 24f / aw, 24f / ah);
                        GUI.DrawTextureWithTexCoords(new Rect(ox + x * px, oy + y * px, px, px), atlas, uv);
                    }
                    else Fill(new Rect(ox + x * px, oy + y * px, px - 1, px - 1), TileColour(f.Tiles[y * 21 + x]));
                }
            float scale = px / 24f;
            float t = seq == null ? 0 : elapsed / (TickSeconds * SpeedFactor);
            for (int i = 0; i < f.X.Length; i++)
            {
                if (f.Status[i] == 0 || (f.Status[i] & 0x40) != 0) continue;
                float gx = f.X[i], gy = f.Y[i]; int frame, hp; bool mirrored, visible;
                if (seq != null) seq.Sample(t, i, out gx, out gy, out frame, out mirrored, out visible, out hp);
                else { frame = TokenFrames.Idle(f.Facing[i], (f.Status[i] & 0x80) != 0, out mirrored); visible = true; hp = f.Hp[i]; }
                if (!visible || gx >= 21) continue;
                bool down = (f.Status[i] & 0x80) != 0 || hp == 0;
                var side = f.Side[i] == 1 ? new Color(0.2f, 0.4f, 1f) : new Color(0.9f, 0.2f, 0.2f);
                float cx = ox + gx * px, cy = oy + gy * px;
                var tex = Sheet(f.Id[i]);
                if (tex != null && frame >= 0)
                {
                    int fw, fh; TokenFrames.FramePixels(f.Size[i], out fw, out fh);
                    float u = (float)(frame * fw) / tex.width, uw = (float)fw / tex.width;
                    var uv = mirrored ? new Rect(u + uw, 0, -uw, 1) : new Rect(u, 0, uw, 1);
                    var r = new Rect(cx, cy, fw * scale, fh * scale);
                    if (i == f.Actor) Fill(new Rect(r.x, r.y, r.width, r.height), new Color(1f, 1f, 0f, 0.35f));
                    GUI.DrawTextureWithTexCoords(r, tex, uv);
                    Fill(new Rect(cx, cy + fh * scale - 3, fw * scale, 3), side);
                }
                else
                {
                    var c = down ? Color.grey : side;
                    var r = new Rect(cx + 2, cy + 2, px - 5, px - 5);
                    Fill(r, i == f.Actor ? Color.yellow : c);
                    if (i == f.Actor) Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), c);
                }
                GUI.Label(new Rect(cx - 2, cy - 5, px + 8, 16), hp.ToString());
            }
            int bx = ox + 21 * px + 20;
            if (GUI.Button(new Rect(bx, oy, 90, 26), playing ? "Pause" : "Play")) { playing = !playing; if (playing && shown >= frames.Count - 1) Show(0); }
            if (GUI.Button(new Rect(bx, oy + 32, 90, 26), "Step")) { playing = false; Show(shown + 1); }
            if (GUI.Button(new Rect(bx, oy + 64, 90, 26), "Back")) { playing = false; Show(shown - 1); }
            if (GUI.Button(new Rect(bx, oy + 96, 90, 26), "New fight")) { Seed++; Start(); }
            int slid = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(bx, oy + 136, 160, 16), shown, 0, frames.Count - 1));
            if (slid != shown) { playing = false; Show(slid); }
            GUI.Label(new Rect(bx, oy + 160, 160, 20), "speed");
            SpeedFactor = GUI.HorizontalSlider(new Rect(bx, oy + 180, 160, 16), SpeedFactor, 0.25f, 4f);
        }
    }
}
