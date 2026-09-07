// Theme.cs — 見た目の一次資料 (2026-09-07 M1)。配色・9スライスの枠・カード枠・アイコンを **コードで生成** する。
// 生成物は PixelLab 素材のプレースホルダーで、同じ名前・同じ寸法の PNG を Resources/Art/<種別>/<id>.png に置くと差し替わる
// (Art() が先に Resources を引く)。ドット絵の実寸で描いて Point フィルタで拡大する＝差し替え後と同じ見え方。
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DeckRogue.Game
{
    public static class Theme
    {
        // ---- パレット (ドット絵向けに彩度を抑えた森の夜) ----
        public static readonly Color Bg = UiKit.Hex("#1a1c33");
        public static readonly Color PanelFill = UiKit.Hex("#f4ecd6");
        public static readonly Color PanelEdge = UiKit.Hex("#3b2f2f");
        public static readonly Color PanelLight = UiKit.Hex("#fbf6e8");
        public static readonly Color ButtonFill = UiKit.Hex("#f4ecd6");
        public static readonly Color ButtonLight = UiKit.Hex("#fbf6e8");
        public static readonly Color Gold = UiKit.Hex("#e0b25a");

        /// <summary>カードタイプの枠色 (物理=茶／呪文=紫／リアクション=青緑／置物=金)</summary>
        public static Color CardTypeColor(string type)
        {
            switch (type)
            {
                case "spell": return UiKit.Hex("#6c4f9c");
                case "reaction": return UiKit.Hex("#3f8c86");
                case "permanent": return UiKit.Hex("#b08a2e");
                default: return UiKit.Hex("#8a6a3c");
            }
        }

        /// <summary>色 (緑青赤白黒) の縁取り</summary>
        public static Color ColorEdge(string color)
        {
            switch (color)
            {
                case "blue": return UiKit.Hex("#4f8fd6");
                case "red": return UiKit.Hex("#d65a4f");
                case "white": return UiKit.Hex("#e8e2c8");
                case "black": return UiKit.Hex("#6b4f8a");
                default: return UiKit.Hex("#5fb85a");
            }
        }

        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>Resources/Art/<種別>/<name> があればそれ (PixelLab 差し替え)。無ければ null</summary>
        public static Sprite Art(string category, string name)
        {
            var key = "art:" + category + "/" + name;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Resources.Load<Sprite>("Art/" + category + "/" + name);
            _cache[key] = s;
            return s;
        }

        // ---- 9スライス ----

        // 「絵本」の肌: 紙の9スライス (PaperFx)。名前が paper で始まる絵は UiKit.Frame が1倍で貼る (線を細く保つ)
        public static Sprite Panel { get { return PaperFx.Panel; } }
        public static Sprite Button { get { return PaperFx.Button; } }
        public static Sprite CardFrame { get { return PaperFx.Card; } }
        public static Sprite Tag { get { return PaperFx.Tag; } }

        /// <summary>角の丸い枠 (size×size・border 幅の縁)。差し替えは同名 PNG (9スライスの border は Sprite 側の設定を使う)</summary>
        static Sprite Nine(string category, string name, int size, int border, Color fill, Color edge, Color light)
        {
            var key = "nine:" + name;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Art(category, name);
            if (s == null)
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int dx = Math.Min(x, size - 1 - x);
                        int dy = Math.Min(y, size - 1 - y);
                        int d = Math.Min(dx, dy);
                        Color c;
                        bool corner = dx < 2 && dy < 2 && (dx + dy) < 2; // 角を1ドット落として丸みを出す
                        if (corner) c = new Color(0f, 0f, 0f, 0f);
                        else if (d == 0) c = edge;
                        else if (d == 1) c = Color.Lerp(edge, fill, 0.35f);
                        else if (d == 2 && y > x) c = light; // 左上に光
                        else c = fill;
                        px[y * size + x] = c;
                    }
                }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
                s.name = name;
            }
            _cache[key] = s;
            return s;
        }

        // ---- アイコン (16×16 のドット絵。'.' 透明 / '#' 主色 / '+' 明 / '-' 暗 / 'w' 白) ----

        static readonly Dictionary<string, string[]> IconArt = new Dictionary<string, string[]>
        {
            { "sword", new[] {
                "..............+.", ".............+#.", "............+#..", "...........+#...", "..........+#....", ".........+#.....",
                "..-.....+#......", "..-#...+#.......", "...-#.+#........", "....-##.........", "....##-.........", "...#.-#-........",
                "..#...-#-.......", ".#.....-........", "................", "................" } },
            { "shield", new[] {
                "................", "...--------.....", "..-########-....", ".-#+++++++##-...", ".-#+++++++##-...", ".-#+++++++##-...", ".-##########-...",
                ".-##########-...", "..-########-....", "..-########-....", "...-######-.....", "....-####-......", ".....-##-.......", "......--........", "................", "................" } },
            { "heart", new[] {
                "................", "...--....--.....", "..-##-..-##-....", ".-#+##--##+#-...", ".-#########-....", ".-##########-...", ".-##########-...",
                "..-########-....", "...-######-.....", "....-####-......", ".....-##-.......", "......--........", "................", "................", "................", "................" } },
            { "energy", new[] {
                "................", ".......--.......", "......-##-......", ".....-#++#-.....", "....-#++++#-....", "....-#++++#-....", ".....-#++#-.....",
                "......-##-......", ".......--.......", "................", "................", "................", "................", "................", "................", "................" } },
            { "draw", new[] {
                "................", "...--------.....", "...-######-.....", "...-#w###w#-....", "...-######-.....", "...-#w###w#-....", "...-######-.....",
                "...-#w###w#-....", "...-######-.....", "...-######-.....", "...--------.....", "................", "................", "................", "................", "................" } },
            { "growth", new[] {
                "................", ".........--.....", "........-##-....", ".......-###-....", "......-####-....", ".....-####-.....", "....-####-......",
                "...-####-.......", "...-###-........", "....-#-.........", "....-#..........", "....#...........", "...#............", "................", "................", "................" } },
            { "momentum", new[] {
                "................", "................", "..-----.........", ".-#####-........", "......-#-.......", "..------#-......", ".-#######-......",
                "........-#-.....", "...-------......", "..-######-......", ".......-........", "................", "................", "................", "................", "................" } },
            { "burn", new[] {
                "................", ".......-........", "......-#-.......", ".....-##-.......", "....-###-.......", "....-###+-......", "...-##++#-......",
                "...-#++++#-.....", "...-#+ww+#-.....", "...-#+ww+#-.....", "....-#++#-......", ".....-##-.......", "......--........", "................", "................", "................" } },
            { "exposed", new[] {
                "................", ".....------.....", "....-######-....", "...-##----##-...", "...-#-....-#-...", "...-#-.##.-#-...", "...-#-.##.-#-...",
                "...-#-....-#-...", "...-##----##-...", "....-######-....", ".....------.....", "................", "................", "................", "................", "................" } },
            { "gold", new[] {
                "................", ".....------.....", "....-######-....", "...-##++++##-...", "...-#+#--#+#-...", "...-#+#..#+#-...", "...-#+#--#+#-...",
                "...-##++++##-...", "....-######-....", ".....------.....", "................", "................", "................", "................", "................", "................" } },
            { "exhaust", new[] {
                "................", "..-........-....", "...-......-.....", "....-....-......", ".....-..-.......", "......--........", "......--........",
                ".....-..-.......", "....-....-......", "...-......-.....", "..-........-....", "................", "................", "................", "................", "................" } },
            { "pierce", new[] {
                "................", "................", "..........-.....", "..........##....", "..........###...", ".---------####..", ".#############..",
                ".---------####..", "..........###...", "..........##....", "..........-.....", "................", "................", "................", "................", "................" } },
            { "skull", new[] {
                "................", ".....------.....", "....-######-....", "...-########-...", "...-##-##-##-...", "...-##-##-##-...", "...-########-...",
                "....-######-....", ".....-#-#-#.....", "......-#-#......", "......----......", "................", "................", "................", "................", "................" } },
            { "crown", new[] {
                "................", "................", "...-......-.....", "...#-.--.-#.....", "...##-##-##.....", "...########.....", "...#+#+#+#+.....",
                "...########.....", "...--------.....", "................", "................", "................", "................", "................", "................", "................" } },
            { "hammer", new[] {
                "................", ".......-----....", "......-#####-...", "......-#####-...", ".......--#--....", ".........#......", ".........#......",
                ".........#......", ".........#......", ".........#......", "........-#-.....", "................", "................", "................", "................", "................" } },
            { "question", new[] {
                "................", ".....-----......", "....-##--##-....", "....-#....#-....", ".........##.....", "........##......", ".......##.......",
                ".......##.......", ".......--.......", ".......##.......", ".......##.......", "................", "................", "................", "................", "................" } },
            { "chest", new[] {
                "................", "................", "...----------...", "..-##########-..", "..-#+#+#+#+#+-..", "..-##########-..", "..------------..",
                "..-######-####..", "..-#####.-####..", "..-######-####..", "..------------..", "................", "................", "................", "................", "................" } },
            { "flag", new[] {
                "................", "...#............", "...#######......", "...#++++++#.....", "...#+++++++#....", "...#++++++#.....", "...#######......",
                "...#............", "...#............", "...#............", "...#............", "...#............", "................", "................", "................", "................" } },
            { "map", new[] {
                "................", "..-----------...", "..-#########-...", "..-#--#--#--#...", "..-#########-...", "..-#--#--#--#...", "..-#########-...",
                "..-#--#--#--#...", "..-#########-...", "..-----------...", "................", "................", "................", "................", "................", "................" } },
            { "star", new[] {
                "................", ".......--.......", ".......##.......", "......-##-......", "..------##------", "..-############-", "...-##########-.",
                "....-########-..", ".....-######-...", "....-###--###-..", "...-##-....-##-.", "..-#-........-#-", "..--..........--", "................", "................", "................" } },
            { "counter", new[] {
                "................", "................", "....--..........", "...-#-..........", "..-#-----.......", ".-######-.......", "..-#-----#-.....",
                "...-#-...-#-....", "....--....-#-...", "...........#-...", "..........-#-...", ".....---.-#-....", ".....-###-......", "......---.......", "................", "................" } },
            { "crest_physical", new[] {
                "-..............-", "#-............-#", "-#-..........-#-", ".-#-........-#-.", "..-#-......-#-..", "...-#-....-#-...", "....-#-..-#-....",
                ".....-#--#-.....", ".....-#--#-.....", "....-#-..-#-....", "...-#-....-#-...", "..-#-......-#-..", ".-#-........-#-.", "-#-..........-#-", "#-............-#", "-..............-" } },
            { "crest_spell", new[] {
                ".......--.......", "......-##-......", "...-..-##-..-...", "..-#-.-##-.-#-..", "...-#--##--#-...", "....-######-....", ".....-####-.....",
                "-######++######-", "-######++######-", ".....-####-.....", "....-######-....", "...-#--##--#-...", "..-#-.-##-.-#-..", "...-..-##-..-...", "......-##-......", ".......--......." } },
            { "crest_reaction", new[] {
                "................", "................", "................", ".....------.....", "...--######--...", "..-###----###-..", ".-##--####--##-.",
                "-##-##-##-##-##-", "-##-##-++-##-##-", ".-##--####--##-.", "..-###----###-..", "...--######--...", ".....------.....", "................", "................", "................" } },
            { "crest_permanent", new[] {
                ".......--.......", "......-##-......", ".....-####-.....", "....-######-....", "...-########-...", "..-##########-..", ".-############-.",
                "-######++######-", "..-----##-----..", "......-##-......", "......-##-......", "......-##-......", ".....-####-.....", "....-######-....", "...-########-...", "...----------..." } },
            { "set", new[] {
                "................", "..-----------...", "..-#########-...", "..-#--#--#--#...", "..-#########-...", "..-#--#--#--#...", "..-#########-...",
                "..-#--#--#--#...", "..-#########-...", "..-----------...", "................", "................", "................", "................", "................", "................" } },
        };

        /// <summary>16px アイコン。差し替えは Resources/Art/icons/<name>.png</summary>
        public static Sprite Icon(string name)
        {
            var key = "icon:" + name;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Art("icons", name);
            if (s == null)
            {
                string[] rows;
                if (!IconArt.TryGetValue(name, out rows)) rows = IconArt["exposed"];
                s = FromBitmap(rows, IconColor(name), name);
            }
            _cache[key] = s;
            return s;
        }

        static Color IconColor(string name)
        {
            switch (name)
            {
                case "sword": return UiKit.Hex("#d9d2c0");
                case "shield": return UiKit.Hex("#6f9fd8");
                case "heart": return UiKit.Hex("#d64f4f");
                case "energy": return UiKit.Hex("#f0c33c");
                case "draw": return UiKit.Hex("#c8d0d8");
                case "growth": return UiKit.Hex("#6abf69");
                case "momentum": return UiKit.Hex("#9fd8d0");
                case "burn": return UiKit.Hex("#e8742f");
                case "gold": return UiKit.Hex("#e0b84a");
                case "pierce": return UiKit.Hex("#e8e2c8");
                case "star": return UiKit.Hex("#e0b25a");
                case "counter": return UiKit.Hex("#7ab8b0");
                case "crest_physical": case "crest_spell": case "crest_reaction": case "crest_permanent": return UiKit.Hex("#eadfc4");
                case "skull": return UiKit.Hex("#e2d6d0");
                case "crown": return UiKit.Hex("#f0c33c");
                case "hammer": return UiKit.Hex("#c9b08a");
                case "question": return UiKit.Hex("#9fd8d0");
                case "chest": return UiKit.Hex("#e0b84a");
                case "flag": return UiKit.Hex("#d64f4f");
                default: return UiKit.Hex("#c8c0b0");
            }
        }

        static Sprite FromBitmap(string[] rows, Color main, string name)
        {
            int h = rows.Length;
            int w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            var dark = Color.Lerp(main, Color.black, 0.45f);
            var light = Color.Lerp(main, Color.white, 0.45f);
            if (name != null && name.StartsWith("crest_")) { dark = PaperFx.Ink; light = PaperFx.Paper; } // 紋章は墨の線画 (塗りは紙色)
            for (int y = 0; y < h; y++)
            {
                var row = rows[y];
                for (int x = 0; x < w; x++)
                {
                    char ch = x < row.Length ? row[x] : '.';
                    Color c;
                    switch (ch)
                    {
                        case '#': c = main; break;
                        case '+': c = light; break;
                        case '-': c = dark; break;
                        case 'w': c = Color.white; break;
                        default: c = new Color(0f, 0f, 0f, 0f); break;
                    }
                    px[(h - 1 - y) * w + x] = c; // 文字列の1行目が上
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = name;
            return s;
        }

        // ---- TMP のインラインアイコン (<sprite name="sword">) ----

        static TMP_SpriteAsset _icons;
        static bool _iconsTried;

        /// <summary>カード文面などに埋め込むスプライトアセット。作れなければ null (文面は角括弧ラベルのまま)</summary>
        public static TMP_SpriteAsset Icons
        {
            get
            {
                if (_iconsTried) return _icons;
                _iconsTried = true;
                try { _icons = BuildSpriteAsset(); }
                catch (Exception e) { Debug.LogWarning("[Theme] インラインアイコンを作れなかった: " + e); _icons = null; }
                return _icons;
            }
        }

        static TMP_SpriteAsset BuildSpriteAsset()
        {
            var names = new List<string>(IconArt.Keys);
            const int cell = 16;
            int cols = 8;
            int rowsN = (names.Count + cols - 1) / cols;
            var atlas = new Texture2D(cols * cell, rowsN * cell, TextureFormat.RGBA32, false);
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color[atlas.width * atlas.height];
            atlas.SetPixels(clear);
            var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = "DeckRogueIcons";
            asset.spriteSheet = atlas;
            // 実行時に作った TMP_SpriteAsset は旧形式 (spriteInfoList) からの移行処理が走って NullReference になる
            // (UpdateLookupTables → UpgradeSpriteAsset)。版を最新にし、表を先に用意して移行を飛ばす
            var ty = typeof(TMP_SpriteAsset);
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
            ty.GetField("m_Version", F)?.SetValue(asset, "1.1.0");
            var glyphList = new List<TMP_SpriteGlyph>();
            var charList = new List<TMP_SpriteCharacter>();
            ty.GetField("m_SpriteGlyphTable", F)?.SetValue(asset, glyphList);
            ty.GetField("m_SpriteCharacterTable", F)?.SetValue(asset, charList);
            ty.GetField("spriteInfoList", BindingFlags.Public | BindingFlags.Instance)?.SetValue(asset, new List<TMP_Sprite>());
            var shader = Shader.Find("TextMeshPro/Sprite");
            asset.material = new Material(shader != null ? shader : Shader.Find("UI/Default"));
            asset.material.mainTexture = atlas;
            for (int i = 0; i < names.Count; i++)
            {
                var sp = Icon(names[i]);
                var tex = sp.texture;
                int cx = (i % cols) * cell;
                int cy = (rowsN - 1 - i / cols) * cell;
                atlas.SetPixels(cx, cy, cell, cell, tex.GetPixels());
                var glyph = new TMP_SpriteGlyph();
                glyph.index = (uint)i;
                glyph.metrics = new UnityEngine.TextCore.GlyphMetrics(cell, cell, 0f, cell * 0.85f, cell);
                glyph.glyphRect = new UnityEngine.TextCore.GlyphRect(cx, cy, cell, cell);
                glyph.scale = 1f;
                glyphList.Add(glyph);
                var ch = new TMP_SpriteCharacter((uint)(0xE000 + i), glyph);
                ch.name = names[i];
                ch.scale = 1f;
                charList.Add(ch);
            }
            atlas.Apply(false, false);
            var face = asset.faceInfo;
            face.pointSize = cell;
            face.scale = 1f;
            face.lineHeight = cell;
            face.ascentLine = cell * 0.85f;
            face.baseline = 0f;
            face.descentLine = -cell * 0.15f;
            asset.faceInfo = face;
            asset.UpdateLookupTables();
            return asset;
        }
    }
}

namespace DeckRogue.Game
{
    /// <summary>敵・リーダーのプレースホルダー (2026-09-07 M2): id のハッシュから左右対称のドット絵の生き物を生成する。
    /// Resources/Art/enemies/<id>.png (PixelLab) があればそれを使う</summary>
    public static class Creature
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>絵を取る。無ければ仮の絵を size ドット (通常 64・エリート 80・ボス 96 = どれも4倍表示) で作る</summary>
        public static Sprite Get(string category, string id, bool friendly = false, int size = 64)
        {
            var key = category + "/" + id + "@" + size;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Theme.Art(category, id);
            if (s == null) s = Generate(id, friendly, size);
            _cache[key] = s;
            return s;
        }

        static uint Hash(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 16777619u; }
            return h;
        }

        /// <summary>仮の生き物: 粗い型 (size/4 マス) を鏡映しで作り、4倍に伸ばして同じドットの粒に揃える。
        /// 輪郭・影・地・光の4段と目・口で「ドット絵の生き物」に見える最低限 (2026-09-07 粒の統一)</summary>
        static Sprite Generate(string id, bool friendly, int size)
        {
            int n = Mathf.Max(16, size);
            int cells = n / 4;                 // 粗い型 (体の形) はここで決め、ドットの描き込みは 1 ドット単位で行う
            uint h = Hash(id);
            var rng = new System.Random((int)(h & 0x7fffffff));
            float hue = friendly ? 0.33f + (h % 30) / 300f : (h % 360) / 360f;
            var main = Color.HSVToRGB(hue, friendly ? 0.5f : 0.55f, friendly ? 0.72f : 0.66f);
            var shade = Color.HSVToRGB(hue, 0.62f, 0.42f);
            var light = Color.HSVToRGB(hue, 0.36f, 0.9f);
            var hilite = Color.HSVToRGB(hue, 0.2f, 1f);
            var outline = Color.HSVToRGB(hue, 0.7f, 0.16f);
            var mask = new bool[cells, cells];
            for (int y = 2; y < cells - 1; y++)
                for (int x = 0; x < cells / 2; x++)
                {
                    float cx = (x + 0.5f) / (cells / 2f);
                    float cy = 1f - Mathf.Abs((y - cells / 2f) / (cells / 2f));
                    float p = 0.12f + 0.78f * cx * cy;
                    mask[x, y] = rng.NextDouble() < p;
                }
            int fx = cells / 4;
            mask[fx, 1] = true; mask[fx + 1, 1] = true; mask[fx, 2] = true; mask[fx + 1, 2] = true;
            if (rng.NextDouble() < 0.5) { mask[cells / 2 - 3, cells - 2] = true; mask[cells / 2 - 3, cells - 1] = true; }
            bool At(int x, int y)
            {
                if (x < 0 || y < 0 || y >= cells || x >= cells) return false;
                int mx = x < cells / 2 ? x : cells - 1 - x;
                return mask[mx, y];
            }
            for (int y = 0; y < cells; y++)
                for (int x = 0; x < cells / 2; x++)
                    if (mask[x, y] && !At(x - 1, y) && !At(x + 1, y) && !At(x, y - 1) && !At(x, y + 1)) mask[x, y] = false;
            // 1ドット単位の形: ブロックの外角を丸める (4x4 の階段でなく、なだらかな輪郭)
            bool Solid(int px_, int py_)
            {
                if (px_ < 0 || py_ < 0 || px_ >= n || py_ >= n) return false;
                int cx = px_ / 4, cy = py_ / 4;
                if (!At(cx, cy)) return false;
                int lx = px_ % 4, ly = py_ % 4;
                bool l = At(cx - 1, cy), r = At(cx + 1, cy), d = At(cx, cy - 1), u = At(cx, cy + 1);
                if (!l && !d && !At(cx - 1, cy - 1) && lx == 0 && ly == 0) return false;
                if (!r && !d && !At(cx + 1, cy - 1) && lx == 3 && ly == 0) return false;
                if (!l && !u && !At(cx - 1, cy + 1) && lx == 0 && ly == 3) return false;
                if (!r && !u && !At(cx + 1, cy + 1) && lx == 3 && ly == 3) return false;
                return true;
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color c = new Color(0f, 0f, 0f, 0f);
                    if (Solid(x, y))
                    {
                        bool edge = !Solid(x - 1, y) || !Solid(x + 1, y) || !Solid(x, y - 1) || !Solid(x, y + 1);
                        // 右上が光源: 右上の縁に近いほど明るく、左下の縁に近いほど暗く
                        int toLight = 0, toShade = 0;
                        for (int k = 1; k <= 5; k++) { if (Solid(x + k, y + k)) toLight = k; else break; }
                        for (int k = 1; k <= 5; k++) { if (Solid(x - k, y - k)) toShade = k; else break; }
                        bool dither = ((x + y) & 1) == 0;
                        if (edge) c = outline;
                        else if (toLight <= 1) c = hilite;
                        else if (toLight <= 3) c = dither ? light : main;
                        else if (toShade <= 2) c = shade;
                        else c = main;
                        // 体の模様: 斑を少し
                        if (!edge && ((x * 7 + y * 13) % 29) == 0) c = Color.Lerp(c, shade, 0.6f);
                    }
                    px[y * n + x] = c;
                }
            // 目 (白+黒の瞳+光)
            int ey = (int)(n * 0.62f);
            int ex = n / 2 - n / 6;
            for (int side = 0; side < 2; side++)
            {
                int bx = side == 0 ? ex - 2 : n - 1 - ex - 1;
                if (!Solid(bx + 1, ey)) continue;
                for (int dy = 0; dy < 4; dy++)
                    for (int dx = 0; dx < 4; dx++)
                        px[(ey + dy) * n + bx + dx] = Color.white;
                px[(ey + 1) * n + bx + 1] = Color.black; px[(ey + 1) * n + bx + 2] = Color.black;
                px[(ey + 2) * n + bx + 1] = Color.black; px[(ey + 2) * n + bx + 2] = Color.black;
                px[(ey + 2) * n + bx + 2] = new Color(0.85f, 0.9f, 1f);
                for (int dx = -1; dx <= 4; dx++) { if (Solid(bx + dx, ey - 1)) px[(ey - 1) * n + bx + dx] = outline; if (Solid(bx + dx, ey + 4)) px[(ey + 4) * n + bx + dx] = outline; }
            }
            int my = (int)(n * 0.5f);
            for (int dx = -2; dx <= 2; dx++) if (Solid(n / 2 + dx, my)) px[my * n + n / 2 + dx] = outline;
            tex.SetPixels(px);
            tex.Apply(false, false);
            var s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "creature:" + id;
            return s;
        }
    }
}

namespace DeckRogue.Game
{
    /// <summary>見た目の追加パーツ (2026-09-07 M2 見た目のパス): 背景のグラデーション・ビネット・足元の影・コスト玉・カードの紋章・レア度の宝石</summary>
    public static class ThemeFx
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>縦グラデーション (上 top → 下 bottom)。バイリニアで滑らかに</summary>
        public static Sprite Gradient(string key, Color top, Color bottom)
        {
            Sprite s;
            if (_cache.TryGetValue("grad:" + key, out s)) return s;
            const int h = 64;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[h];
            for (int y = 0; y < h; y++) px[y] = Color.Lerp(bottom, top, (float)y / (h - 1));
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _cache["grad:" + key] = s;
            return s;
        }

        /// <summary>ビネット (周辺が暗くなる)。alpha だけの黒</summary>
        public static Sprite Vignette()
        {
            Sprite s;
            if (_cache.TryGetValue("vignette", out s)) return s;
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.9f + dy * dy * 1.2f);
                    float a = Mathf.Clamp01((d - 0.55f) / 0.7f);
                    px[y * n + x] = new Color(0f, 0f, 0f, a * a * 0.85f);
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _cache["vignette"] = s;
            return s;
        }

        /// <summary>足元の影 (楕円)</summary>
        public static Sprite Shadow()
        {
            Sprite s;
            if (_cache.TryGetValue("shadow", out s)) return s;
            const int w = 64, h = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / w * 2f - 1f;
                    float dy = (y + 0.5f) / h * 2f - 1f;
                    float d = dx * dx + dy * dy;
                    float a = Mathf.Clamp01(1f - d) * 0.55f;
                    px[y * w + x] = new Color(0f, 0f, 0f, a);
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _cache["shadow"] = s;
            return s;
        }

        /// <summary>コスト玉 (16px のドット絵の球。差し替えは Art/ui/cost_orb.png)</summary>
        public static Sprite CostOrb()
        {
            Sprite s;
            if (_cache.TryGetValue("orb", out s)) return s;
            s = Theme.Art("ui", "cost_orb");
            if (s == null)
            {
                const int n = 16;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[n * n];
                var c0 = UiKit.Hex("#f0c33c"); var c1 = UiKit.Hex("#9a6d12"); var edge = UiKit.Hex("#2a1d05");
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f;
                        float dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        Color c = new Color(0f, 0f, 0f, 0f);
                        if (d < 1f)
                        {
                            c = d > 0.85f ? edge : Color.Lerp(c0, c1, Mathf.Clamp01((dx + dy) * 0.5f + 0.5f));
                            if (dx < -0.2f && dy > 0.25f && d < 0.7f) c = Color.Lerp(c, Color.white, 0.5f);
                        }
                        px[y * n + x] = c;
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache["orb"] = s;
            return s;
        }

        /// <summary>レア度の宝石 (8px)。C=灰 U=青 R=金</summary>
        public static Sprite Gem(string rarity)
        {
            string key = "gem:" + rarity;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Theme.Art("ui", "gem_" + rarity);
            if (s == null)
            {
                const int n = 8;
                var main = rarity == "rare" ? UiKit.Hex("#e0b84a") : rarity == "uncommon" ? UiKit.Hex("#5aa0e0") : UiKit.Hex("#9aa39c");
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        int m = Math.Abs(x - 3) + Math.Abs(y - 3) + (x > 3 ? 1 : 0) + (y > 3 ? 1 : 0);
                        px[y * n + x] = m <= 3 ? (m == 3 ? Color.Lerp(main, Color.black, 0.5f) : (x < 4 && y > 3 ? Color.Lerp(main, Color.white, 0.4f) : main)) : new Color(0, 0, 0, 0);
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache[key] = s;
            return s;
        }

        /// <summary>斬撃の筋 (48×12。中央が明るく両端へ消える)</summary>
        public static Sprite Slash()
        {
            Sprite s;
            if (_cache.TryGetValue("slash", out s)) return s;
            s = Theme.Art("fx", "slash");
            if (s == null)
            {
                const int w = 48, h = 12;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                        float dy = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                        float a = Mathf.Clamp01(1f - dx) * Mathf.Clamp01(1f - dy * 1.4f);
                        a = a > 0.55f ? 1f : a > 0.3f ? 0.6f : a > 0.15f ? 0.25f : 0f;
                        px[y * w + x] = new Color(1f, 1f, 1f, a);
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache["slash"] = s;
            return s;
        }

        /// <summary>カードの紋章 (絵の代わり): id のハッシュから 24×16 の左右対称の模様。差し替えは Art/cards/<id>.png</summary>
        /// <summary>レリックのプレースホルダー: id から生成する左右対称の紋章 (金の3階調+暗い縁・背景透過)</summary>
        public static Sprite RelicGlyph(string relicId)
        {
            string key = "relicglyph:" + relicId;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int n = 14;
            uint hh = 2166136261u;
            foreach (var ch in relicId) { hh ^= ch; hh *= 16777619u; }
            var rng = new System.Random((int)(hh & 0x7fffffff));
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            var mask = new bool[n / 2, n];
            for (int y = 1; y < n - 1; y++)
                for (int x = 0; x < n / 2; x++)
                {
                    float cx = (x + 0.5f) / (n / 2f);
                    float cy = 1f - Mathf.Abs((y - n / 2f) / (n / 2f));
                    mask[x, y] = rng.NextDouble() < 0.15f + 0.6f * cx * cy;
                }
            bool At(int x, int y) { if (x < 0 || y < 0 || y >= n || x >= n) return false; int mx = x < n / 2 ? x : n - 1 - x; return mask[mx, y]; }
            Color gold = UiKit.Hex("#e0b84a"), light = UiKit.Hex("#fff0a8"), dark = UiKit.Hex("#7a5a18"), edge = UiKit.Hex("#1a1208");
            int hueShift = rng.Next(0, 3);
            if (hueShift == 1) { gold = UiKit.Hex("#9fd8d0"); light = UiKit.Hex("#e0fff8"); dark = UiKit.Hex("#3a6a68"); }
            else if (hueShift == 2) { gold = UiKit.Hex("#d69a6a"); light = UiKit.Hex("#ffd8b8"); dark = UiKit.Hex("#6a3a20"); }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color c = new Color(0f, 0f, 0f, 0f);
                    if (At(x, y))
                    {
                        bool e = !At(x - 1, y) || !At(x + 1, y) || !At(x, y - 1) || !At(x, y + 1);
                        c = e ? dark : (y > n * 0.6f ? light : gold);
                    }
                    else if (At(x - 1, y) || At(x + 1, y) || At(x, y - 1) || At(x, y + 1)) c = edge;
                    px[y * n + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _cache[key] = s;
            return s;
        }

        public static Sprite CardArt(string cardId, Color tint)
        {
            string key = "cardart:" + cardId;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Theme.Art("cards", cardId);
            if (s == null)
            {
                const int w = 24, h = 16;
                uint hh = 2166136261u;
                foreach (var ch in cardId) { hh ^= ch; hh *= 16777619u; }
                var rng = new System.Random((int)(hh & 0x7fffffff));
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[w * h];
                var dark = Color.Lerp(tint, Color.black, 0.55f);
                var light = Color.Lerp(tint, Color.white, 0.35f);
                var mask = new bool[w / 2, h];
                for (int y = 1; y < h - 1; y++)
                    for (int x = 0; x < w / 2; x++)
                    {
                        float cx = (x + 0.5f) / (w / 2f);
                        float cy = 1f - Mathf.Abs((y - h / 2f) / (h / 2f));
                        mask[x, y] = rng.NextDouble() < 0.08f + 0.5f * cx * cy;
                    }
                bool At(int x, int y) { if (x < 0 || y < 0 || y >= h || x >= w) return false; int mx = x < w / 2 ? x : w - 1 - x; return mask[mx, y]; }
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        Color c = Color.Lerp(dark, Color.black, 0.6f);
                        if (At(x, y))
                        {
                            bool edge = !At(x - 1, y) || !At(x + 1, y) || !At(x, y - 1) || !At(x, y + 1);
                            c = edge ? dark : (y > h / 2 ? light : tint);
                        }
                        px[y * w + x] = c;
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache[key] = s;
            return s;
        }
    }
}
