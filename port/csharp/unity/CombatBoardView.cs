// CombatBoardView.cs -- Unity front end for AutoBattle: add it to an empty GameObject; it runs one computer-played fight on Start and replays it turn by turn on the screen
// (IMGUI): the 21x21 ground as coloured cells and the creatures as their ROM sprites (Resources/BuckRogers/tokens/token_<monster id>.png, made by tools/export_tokens.py; the frame
// follows the facing / down rules of ROM 0xAD5A, see TokenFrames) with hit points and a side marker (blue = party, red = monsters). Without the PNGs it falls back to coloured boxes.
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
        [Range(0, 10)] public int AreaType = 1;      // outdoor ground type
        public float SecondsPerTurn = 0.4f;
        public int CellPixels = 24;

        List<BattleFrame> frames = new List<BattleFrame>();
        int shown;
        float timer;
        bool playing = true;
        string result = "";
        Texture2D white;
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
            frames = AutoBattle.Run(rom, file, Party, MonsterGroups, GroupSize, Seed, AreaType, out int winner);
            shown = 0; timer = 0; playing = true;
            result = frames.Count == 0 ? "no fight (a side had no room on the battlefield)" : (winner == 1 ? "the party wins" : "the monsters win");
        }

        void Update()
        {
            if (!playing || frames.Count == 0) return;
            timer += Time.deltaTime;
            if (timer >= SecondsPerTurn) { timer = 0; if (shown < frames.Count - 1) shown++; else playing = false; }
        }

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
            for (int y = 0; y < 21; y++)
                for (int x = 0; x < 21; x++)
                    Fill(new Rect(ox + x * px, oy + y * px, px - 1, px - 1), TileColour(f.Tiles[y * 21 + x]));
            float scale = px / 24f;
            for (int i = 0; i < f.X.Length; i++)
            {
                if (f.Status[i] == 0 || (f.Status[i] & 0x40) != 0 || f.X[i] >= 21) continue;
                bool down = (f.Status[i] & 0x80) != 0 || f.Hp[i] == 0;
                var side = f.Side[i] == 1 ? new Color(0.2f, 0.4f, 1f) : new Color(0.9f, 0.2f, 0.2f);
                float cx = ox + f.X[i] * px, cy = oy + f.Y[i] * px;
                var tex = Sheet(f.Id[i]);
                if (tex != null)
                {
                    bool mirrored;
                    int frame = TokenFrames.Idle(f.Facing[i], down, out mirrored);
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
                GUI.Label(new Rect(cx - 2, cy - 5, px + 8, 16), f.Hp[i].ToString());
            }
            int bx = ox + 21 * px + 20;
            if (GUI.Button(new Rect(bx, oy, 90, 26), playing ? "Pause" : "Play")) { playing = !playing; if (playing && shown >= frames.Count - 1) shown = 0; }
            if (GUI.Button(new Rect(bx, oy + 32, 90, 26), "Step")) { playing = false; if (shown < frames.Count - 1) shown++; }
            if (GUI.Button(new Rect(bx, oy + 64, 90, 26), "Back")) { playing = false; if (shown > 0) shown--; }
            if (GUI.Button(new Rect(bx, oy + 96, 90, 26), "New fight")) { Seed++; Start(); }
            shown = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(bx, oy + 136, 160, 16), shown, 0, frames.Count - 1));
        }
    }
}
