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
        public static readonly Color Bg = UiKit.Hex("#131917");
        public static readonly Color PanelFill = UiKit.Hex("#1e2622");
        public static readonly Color PanelEdge = UiKit.Hex("#0b0f0d");
        public static readonly Color PanelLight = UiKit.Hex("#3a4a44");
        public static readonly Color ButtonFill = UiKit.Hex("#2c3a35");
        public static readonly Color ButtonLight = UiKit.Hex("#5a7a6c");
        public static readonly Color Gold = UiKit.Hex("#e0b84a");

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

        public static Sprite Panel { get { return Nine("ui", "panel", 24, 6, PanelFill, PanelEdge, PanelLight); } }
        public static Sprite Button { get { return Nine("ui", "btn_normal", 24, 6, ButtonFill, PanelEdge, ButtonLight); } }
        public static Sprite CardFrame { get { return Nine("ui", "card_frame", 32, 8, UiKit.Hex("#e9e2cf"), PanelEdge, Color.white); } }

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

        public static Sprite Get(string category, string id, bool friendly = false)
        {
            var key = category + "/" + id;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Theme.Art(category, id);
            if (s == null) s = Generate(id, friendly);
            _cache[key] = s;
            return s;
        }

        static uint Hash(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 16777619u; }
            return h;
        }

        /// <summary>16×16 の半分をランダムに埋めて鏡映しにする (宇宙船ジェネレータの古典)。色は id の色相・目は白</summary>
        static Sprite Generate(string id, bool friendly)
        {
            const int n = 16;
            uint h = Hash(id);
            var rng = new System.Random((int)(h & 0x7fffffff));
            float hue = friendly ? 0.33f + (h % 30) / 300f : (h % 360) / 360f;
            var main = Color.HSVToRGB(hue, friendly ? 0.55f : 0.6f, friendly ? 0.75f : 0.7f);
            var dark = Color.HSVToRGB(hue, 0.7f, 0.3f);
            var light = Color.HSVToRGB(hue, 0.35f, 0.95f);
            var mask = new bool[n, n];
            // 体: 中央寄りほど埋まりやすい。上下の端は空ける
            for (int y = 2; y < n - 1; y++)
            {
                for (int x = 0; x < n / 2; x++)
                {
                    float cx = (x + 0.5f) / (n / 2f);            // 0(外)〜1(中央)
                    float cy = 1f - Mathf.Abs((y - n / 2f) / (n / 2f));
                    float p = 0.15f + 0.75f * cx * cy;
                    mask[x, y] = rng.NextDouble() < p;
                }
            }
            // 足: 下段に2本
            mask[3, 1] = true; mask[4, 1] = true; mask[3, 2] = true; mask[4, 2] = true;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            bool At(int x, int y) { if (x < 0 || y < 0 || y >= n || x >= n) return false; int mx = x < n / 2 ? x : n - 1 - x; return mask[mx, y]; }
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Color c = new Color(0f, 0f, 0f, 0f);
                    if (At(x, y))
                    {
                        bool edge = !At(x - 1, y) || !At(x + 1, y) || !At(x, y - 1) || !At(x, y + 1);
                        c = edge ? dark : (y > n / 2 + 1 ? light : main);
                    }
                    else if (At(x - 1, y) || At(x + 1, y) || At(x, y - 1) || At(x, y + 1))
                    {
                        c = new Color(0f, 0f, 0f, 0.9f); // 輪郭
                    }
                    px[y * n + x] = c;
                }
            }
            // 目 (白+黒) を上から5行目あたりに
            int ey = n - 6;
            for (int x = 0; x < n; x++)
            {
                if (At(x, ey) && (x == 5 || x == n - 6)) { px[ey * n + x] = Color.white; px[(ey - 1) * n + x] = Color.black; }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            var s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "creature:" + id;
            return s;
        }
    }
}
