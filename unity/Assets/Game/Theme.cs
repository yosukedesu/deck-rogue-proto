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
        // 色は PaperFx が唯一の出典 (2026-09-16 カラーテーマ「黒鉄と真鍮」)。ここは旧名の別名
        public static readonly Color Bg = PaperFx.Night;
        public static readonly Color PanelFill = PaperFx.Paper;
        public static readonly Color PanelEdge = PaperFx.Ink;
        public static readonly Color PanelLight = PaperFx.Paper3;
        public static readonly Color ButtonFill = PaperFx.Paper;
        public static readonly Color ButtonLight = PaperFx.Paper3;
        public static readonly Color Gold = PaperFx.Brass;

        /// <summary>カードタイプの枠色 (物理=茶／呪文=紫／リアクション=青緑／置物=金)</summary>
        public static Color CardTypeColor(string type)
        {
            switch (type)
            {
                case "spell": return UiKit.Hex("#6c4f9c");
                case "reaction": return PaperFx.ManaBand;
                case "permanent": return UiKit.Hex("#9c7a24");
                default: return UiKit.Hex("#7d6146");
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
            // 墨一色で読む剣 (人形の足元の札。2026-09-30 F43: sword は刃と鍔を「-」で描くので IconMono で消え、1〜2ドットの斜線と小さな×＝「✓」「メ」に見えた)。
            // 「#」だけで描く: 刃は1行3ドットの斜め・鍔は刃と直角・柄頭 2×2
            { "sword_mono", new[] {
                ".............###", "............####", "...........###..", "..........###...", ".........###....", "........###.....",
                ".......###......", "..#...###.......", "..##.###........", "...####.........", "....###.........", "...#####........",
                "..##..##........", ".##.............", "##..............", "................" } },
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
            // ---- 状態の札の記号 (2026-09-29 p18: 1つの概念に1つの記号。墨1色で読めるように線は1〜2ドット・区切りは透明で抜く＝'-' を使わない) ----
            // 筋力の上下 = 強化の意図 (intent_buff の真鍮の矢印) と同じ形を 16 ドットに。向きが符号 (旧: 剣＝攻撃の意図と同じ絵で、14px に縮むと「✓」に見えた)
            { "strength_up", new[] {
                "................", ".......##.......", "......####......", ".....######.....", "....########....", "...##########...", "..############..",
                "......####......", "......####......", "......####......", "......####......", "......####......", "......####......", "......####......", "................", "................" } },
            { "strength_down", new[] {
                "................", "......####......", "......####......", "......####......", "......####......", "......####......", "......####......",
                "......####......", "..############..", "...##########...", "....########....", ".....######.....", "......####......", ".......##.......", "................", "................" } },
            // 威圧 (敵)・弱体 (自分) = 与ダメ -25%: 下向きの二重の山形 (筋力の▼と見分ける)
            { "weak", new[] {
                "................", "................", "..##........##..", "...##......##...", "....##....##....", ".....##..##.....", "......####......",
                ".......##.......", "..##........##..", "...##......##...", "....##....##....", ".....##..##.....", "......####......", ".......##.......", "................", "................" } },
            // 装甲・ターン装甲 = 胴鎧 (肩と草摺の段)。盾 (ブロック) と形を分ける
            { "armor", new[] {
                "................", "..###......###..", "..####....####..", "..############..", "...##########...", "...##########...", "...##########...",
                "................", "...##########...", "...##########...", "................", "...##########...", "....########....", "................", "................", "................" } },
            // ターン = 砂時計 (旧: 地図と同じ格子)
            { "turn", new[] {
                "................", "..############..", "..############..", "...##......##...", "....##....##....", ".....##..##.....", "......####......",
                ".......##.......", "......####......", ".....######.....", "....########....", "...##########...", "..############..", "..############..", "................", "................" } },
            // 混乱 = 渦 (旧: 急所と同じ的)
            { "confuse", new[] {
                "................", "....#######.....", "...##.....##....", "..##.......##...", "..#...####..#...", "..#..##..##.##..", "..#..#....#..#..",
                "..#..#..#.#..#..", "..#..#..###..#..", "..#..##.....##..", "..##..######.#..", "...##.......##..", "....#########...", "................", "................", "................" } },
            // 虚弱 = ひびの入った盾 (得るブロック -25%)
            { "frail", new[] {
                "................", "..############..", "..######.#####..", "..#####.######..", "..######.#####..", "..######.#####..", "..#####.######..",
                "..######.#####..", "..#######.####..", "...#####.####...", "....####.###....", ".....###.##.....", "......##.#......", ".......#........", "................", "................" } },
        };

        /// <summary>アイコン。差し替えは Resources/Art/icons/<name>.png (PixelLab は 32 ドット。2026-09-11)。
        /// size を渡すと 32px 未満の置き場では従来の 16px ビットマップを返す (縮小してつぶれたドットを出さない)</summary>
        public static Sprite Icon(string name, float size = 0f)
        {
            if (size > 0f && size < 32f) return IconBitmap(name);
            var key = "icon:" + name;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Art("icons", name);
            if (s == null) s = IconBitmap(name);
            _cache[key] = s;
            return s;
        }

        /// <summary>PixelLab の絵があるか (呼び手が整数倍の寸法を選ぶため)</summary>
        public static bool HasIconArt(string name) { return Art("icons", name) != null; }

        /// <summary>コード描画の 16px アイコン (絵が無い時と、小さく置く時)</summary>
        public static Sprite IconBitmap(string name)
        {
            var key = "iconbmp:" + name;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
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
                case "burn": return PaperFx.Ember;   // 延焼の印 (color-theme の表の Ember と同じ値)
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
                // 2026-09-29 p18 (色を掛けずに置く時の色。札の中は IconMono＋墨で塗る)。役割の色そのもの＝PaperFx の名前 (p26)
                case "strength_up": return PaperFx.Brass;
                case "strength_down": case "weak": return PaperFx.Sky;
                case "confuse": case "frail": return PaperFx.Plum;
                default: return UiKit.Hex("#c8c0b0");
            }
        }

        /// <summary>
        /// 墨1色で塗る 16px のアイコン (2026-09-29 p18)。'#' と '+' は白・'-' と 'w' は透明 (呼び手が墨の色を掛ける)。
        /// '#' を持たない絵 (消滅の ✕) だけ '-' を白に。IconBitmap に墨を掛けると '#' (主色) と '-' (主色の45%暗) の差が 1.5:1 に潰れ、
        /// 格子や的が黒い四角になっていた (帳面の状態の札・ターンの札・盾の札)
        /// </summary>
        public static Sprite IconMono(string name)
        {
            var key = "iconmono:" + name;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            string[] rows;
            if (!IconArt.TryGetValue(name, out rows)) rows = IconArt["exposed"];
            bool hasFill = false;
            foreach (var r in rows) if (r.IndexOf('#') >= 0 || r.IndexOf('+') >= 0) { hasFill = true; break; }
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            var clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < h; y++)
            {
                var row = rows[y];
                for (int x = 0; x < w; x++)
                {
                    char ch = x < row.Length ? row[x] : '.';
                    bool on = ch == '#' || ch == '+' || (!hasFill && ch == '-');
                    px[(h - 1 - y) * w + x] = on ? Color.white : clear;
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = name + "-mono";
            _cache[key] = s;
            return s;
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
            if (s == null) s = category == "dolls" ? Doll(id, size) : Generate(id, friendly, size);
            _cache[key] = s;
            return s;
        }

        /// <summary>人形 (白の従者) の仮の絵 (2026-09-19 人形の盤面表示): 白鉄のフードの小さな騎士 = 暗い顔の空洞に琥珀の目2つ・真鍮の帯。
        /// 持ち物は id から (剣・盾・弩・鐘・蝋燭・旗・犬)。本番は PixelLab の Art/dolls/<id>.png (docs/pixellab/dolls-stage.json) が差し替える</summary>
        static Sprite Doll(string id, int size)
        {
            int n = Mathf.Max(16, size);
            float u = n / 32f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            Color white = UiKit.Hex("#e9e6df"), whiteShade = UiKit.Hex("#b9b6b0"), whiteDark = UiKit.Hex("#8a8884"), ink = UiKit.Hex("#2f2e35");
            Color brass = UiKit.Hex("#c99a3a"), brassDark = UiKit.Hex("#8a6520"), hollow = UiKit.Hex("#1a1620"), amber = UiKit.Hex("#ffc45a");
            bool hound = id.Contains("hound");
            void Put(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < n && y < n) px[y * n + x] = c; }
            int X(float v) { return Mathf.RoundToInt(v * u); }
            if (!hound)
            {
                // 胴 (下すぼまりの板) と脚
                for (int y = X(3); y <= X(15); y++)
                {
                    float t = (y - X(3)) / (float)Mathf.Max(1, X(15) - X(3));
                    int half = Mathf.RoundToInt(Mathf.Lerp(4f, 6f, t) * u);
                    for (int x = X(15) - half; x <= X(16) + half; x++) Put(x, y, x < X(15) ? whiteShade : white);
                }
                for (int y = X(1); y < X(4); y++) { for (int x = X(11); x <= X(13); x++) Put(x, y, whiteDark); for (int x = X(18); x <= X(20); x++) Put(x, y, whiteDark); }
                // 真鍮の帯 (腰) と鋲
                for (int x = X(10); x <= X(21); x++) Put(x, X(8), brass);
                Put(X(12), X(12), brass); Put(X(19), X(12), brass); Put(X(15), X(5), brassDark);
                // フード (丸い頭・とがった先端) と暗い顔の空洞・琥珀の目
                for (int y = X(15); y <= X(27); y++)
                {
                    float t = (y - X(15)) / (float)Mathf.Max(1, X(27) - X(15));
                    int half = Mathf.RoundToInt((t < 0.75f ? 6.5f : Mathf.Lerp(6.5f, 1.5f, (t - 0.75f) / 0.25f)) * u);
                    for (int x = X(15) - half; x <= X(16) + half; x++) Put(x, y, x < X(14) ? whiteShade : white);
                }
                Put(X(16), X(28), white); Put(X(17), X(29), whiteShade);
                for (int y = X(18); y <= X(23); y++) for (int x = X(13); x <= X(20); x++) Put(x, y, hollow);
                Put(X(14), X(21), amber); Put(X(15), X(21), amber); Put(X(18), X(21), amber); Put(X(19), X(21), amber);
                // 背中のランタン (左肩の後ろに小さな真鍮)
                Put(X(9), X(13), brassDark); Put(X(9), X(14), brass); Put(X(9), X(12), brassDark);
                // 持ち物 (id から)
                if (id.Contains("squire") || id.Contains("page")) { for (int y = X(9); y <= X(24); y++) Put(X(24), y, y > X(11) ? brass : brassDark); Put(X(23), X(11), brassDark); Put(X(25), X(11), brassDark); }
                else if (id.Contains("shield")) { for (int y = X(6); y <= X(16); y++) for (int x = X(5); x <= X(11); x++) { float dx = x - X(8), dy = y - X(11); if (dx * dx / (9f * u * u) + dy * dy / (25f * u * u) <= 1f) Put(x, y, (Mathf.Abs(dx) > 2.2f * u || Mathf.Abs(dy) > 4f * u) ? brass : white); } }
                else if (id.Contains("archer")) { for (int x = X(20); x <= X(27); x++) Put(x, X(12), brassDark); for (int y = X(9); y <= X(15); y++) Put(X(25), y, brass); }
                else if (id.Contains("band")) { for (int y = X(12); y <= X(17); y++) for (int x = X(23); x <= X(27); x++) Put(x, y, y == X(12) ? brassDark : brass); Put(X(25), X(18), brassDark); }
                else if (id.Contains("candle")) { for (int y = X(10); y <= X(18); y++) Put(X(24), y, white); Put(X(24), X(19), amber); Put(X(24), X(20), amber); Put(X(24), X(9), brass); }
                else if (id.Contains("banneret")) { for (int y = X(4); y <= X(30); y++) Put(X(25), y, brassDark); for (int y = X(20); y <= X(29); y++) for (int x = X(26); x <= X(31); x++) Put(x, y, (x + y) % 3 == 0 ? whiteShade : white); }
                else if (id.Contains("choir") || id.Contains("monk")) { for (int x = X(21); x <= X(26); x++) Put(x, X(13), brass); for (int x = X(22); x <= X(25); x++) { Put(x, X(14), amber); Put(x, X(12), brassDark); } }
            }
            else
            {
                // 四つ足の白鉄の犬: 胴は横長・頭は右 (敵の方)
                for (int y = X(6); y <= X(14); y++) for (int x = X(6); x <= X(24); x++) Put(x, y, y < X(9) ? whiteShade : white);
                for (int x = X(8); x <= X(22); x += X(4)) for (int y = X(1); y < X(6); y++) { Put(x, y, whiteDark); Put(x + 1, y, whiteDark); }
                for (int y = X(10); y <= X(19); y++) for (int x = X(21); x <= X(29); x++) Put(x, y, white);
                for (int y = X(12); y <= X(16); y++) for (int x = X(23); x <= X(28); x++) Put(x, y, hollow);
                Put(X(25), X(14), amber); Put(X(27), X(14), amber);
                for (int x = X(8); x <= X(22); x++) Put(x, X(10), brass);
                Put(X(5), X(13), whiteShade); Put(X(4), X(14), whiteShade); Put(X(3), X(15), whiteDark);
                Put(X(14), X(15), brassDark); Put(X(14), X(16), brass);
            }
            // 輪郭 (選択的アウトライン): 絵の外側に接する画素を墨に
            var src = (Color[])px.Clone();
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    if (src[y * n + x].a > 0f) continue;
                    bool near = (x > 0 && src[y * n + x - 1].a > 0f) || (x + 1 < n && src[y * n + x + 1].a > 0f) || (y > 0 && src[(y - 1) * n + x].a > 0f) || (y + 1 < n && src[(y + 1) * n + x].a > 0f);
                    if (near) px[y * n + x] = ink;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            var s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "doll:" + id;
            return s;
        }

        static readonly Dictionary<Sprite, int> _topMargin = new Dictionary<Sprite, int>();
        /// <summary>絵の上端の透明な行数 (ドット)。吹き出しを頭のすぐ上に置くため (2026-09-15 スマホ)。読めなければ 0</summary>
        public static int TopMargin(Sprite s)
        {
            if (s == null) return 0;
            int m;
            if (_topMargin.TryGetValue(s, out m)) return m;
            m = 0;
            try
            {
                var tex = s.texture; var r = s.rect;   // rect はテクスチャ内の矩形 (左下原点)
                int x0 = Mathf.RoundToInt(r.x), y0 = Mathf.RoundToInt(r.y), w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
                var px = tex.GetPixels32();
                int tw = tex.width;
                for (int row = h - 1; row >= 0; row--)   // 上の行から
                {
                    bool any = false;
                    int baseIdx = (y0 + row) * tw + x0;
                    for (int x = 0; x < w; x++) if (baseIdx + x < px.Length && px[baseIdx + x].a >= 16) { any = true; break; }
                    if (any) break;
                    m++;
                }
                if (m >= h) m = 0;   // 全部透明 (仮の絵など) なら余白なし扱い
            }
            catch (Exception) { m = 0; }
            _topMargin[s] = m;
            return m;
        }

        static readonly Dictionary<Sprite, Vector2Int> _sideMargin = new Dictionary<Sprite, Vector2Int>();
        /// <summary>絵の左右の透明な列数 (ドット。x=左・y=右)。確認の窓が敵の体に掛からないかを測るため (2026-09-29 p11)。読めなければ 0</summary>
        public static Vector2Int SideMargins(Sprite s)
        {
            if (s == null) return Vector2Int.zero;
            Vector2Int m;
            if (_sideMargin.TryGetValue(s, out m)) return m;
            m = Vector2Int.zero;
            try
            {
                var tex = s.texture; var r = s.rect;
                int x0 = Mathf.RoundToInt(r.x), y0 = Mathf.RoundToInt(r.y), w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
                var px = tex.GetPixels32();
                int tw = tex.width;
                int lo = w, hi = -1;
                for (int row = 0; row < h; row++)
                {
                    int baseIdx = (y0 + row) * tw + x0;
                    for (int x = 0; x < w; x++)
                    {
                        int k = baseIdx + x;
                        if (k < px.Length && px[k].a >= 16) { if (x < lo) lo = x; if (x > hi) hi = x; }
                    }
                }
                if (hi >= lo) m = new Vector2Int(lo, w - 1 - hi);
            }
            catch (Exception) { m = Vector2Int.zero; }
            _sideMargin[s] = m;
            return m;
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
            float u = n / 64f;                                   // 64 ドット基準の倍率
            uint h = Hash(id);
            var rng = new System.Random((int)(h & 0x7fffffff));
            float hue = friendly ? 0.33f + (h % 30) / 300f : (h % 360) / 360f;
            var main = Color.HSVToRGB(hue, friendly ? 0.5f : 0.55f, friendly ? 0.72f : 0.64f);
            var shade = Color.HSVToRGB(hue, 0.64f, 0.4f);
            var light = Color.HSVToRGB(hue, 0.38f, 0.88f);
            var hilite = Color.HSVToRGB(hue, 0.2f, 1f);
            var outline = Color.HSVToRGB(hue, 0.7f, 0.14f);
            // 形: 楕円の胴・円の頭・脚・角/耳
            float cx = n * 0.5f;
            float bodyRx = n * (0.22f + (float)rng.NextDouble() * 0.12f), bodyRy = n * (0.16f + (float)rng.NextDouble() * 0.08f);
            float bodyCy = n * (0.30f + (float)rng.NextDouble() * 0.06f);
            float headR = n * (0.14f + (float)rng.NextDouble() * 0.08f);
            float headCx = cx + (rng.NextDouble() < 0.5 ? -1f : 1f) * n * (float)rng.NextDouble() * 0.05f;
            float headCy = bodyCy + bodyRy * 0.55f + headR * 0.6f;
            int legs = rng.NextDouble() < 0.55 ? 2 : 4;
            float legW = n * 0.06f, legTop = bodyCy, legSpread = bodyRx * (legs == 2 ? 0.45f : 0.75f);
            bool horns = rng.NextDouble() < 0.5, ears = !horns && rng.NextDouble() < 0.6, cyclops = rng.NextDouble() < 0.18;
            bool Solid(int x, int y)
            {
                if (x < 0 || y < 0 || x >= n || y >= n) return false;
                float fx = x + 0.5f, fy = y + 0.5f;
                float dx = (fx - cx) / bodyRx, dy = (fy - bodyCy) / bodyRy;
                if (dx * dx + dy * dy <= 1f) return true;
                float hx = fx - headCx, hy = fy - headCy;
                if (hx * hx + hy * hy <= headR * headR) return true;
                for (int l = 0; l < legs; l++)
                {
                    float lx = legs == 2 ? cx + (l == 0 ? -legSpread : legSpread) : cx + (l - 1.5f) * legSpread * 0.66f;
                    if (Mathf.Abs(fx - lx) <= legW * 0.5f && fy >= n * 0.04f && fy <= legTop) return true;
                    if (fy < n * 0.04f + legW * 0.6f && fy >= n * 0.04f && Mathf.Abs(fx - lx) <= legW * 0.8f) return true;   // 足先
                }
                if (horns)
                {
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        float bx = headCx + sgn * headR * 0.6f, by = headCy + headR * 0.7f;
                        float t = (fy - by) / (headR * 0.9f);
                        if (t >= 0f && t <= 1f && Mathf.Abs(fx - (bx + sgn * t * headR * 0.35f)) <= (1f - t) * legW * 0.6f + 0.6f) return true;
                    }
                }
                if (ears)
                {
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        float ex = headCx + sgn * headR * 0.8f, ey = headCy + headR * 0.65f;
                        float r = headR * 0.42f;
                        if ((fx - ex) * (fx - ex) + (fy - ey) * (fy - ey) <= r * r) return true;
                    }
                }
                return false;
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
                        int toLight = 0, toShade = 0;
                        for (int k = 1; k <= 6; k++) { if (Solid(x + k, y + k)) toLight = k; else break; }
                        for (int k = 1; k <= 6; k++) { if (Solid(x - k, y - k)) toShade = k; else break; }
                        bool dither = ((x + y) & 1) == 0;
                        int L = Mathf.Max(1, Mathf.RoundToInt(u));
                        if (edge) c = outline;
                        else if (toLight <= L) c = hilite;
                        else if (toLight <= 3 * L) c = dither ? light : main;
                        else if (toShade <= 2 * L) c = shade;
                        else if (toShade <= 4 * L) c = dither ? shade : main;
                        else c = main;
                        // 胴と頭の境に暗い線 (重なりを読ませる)
                        float hx = x + 0.5f - headCx, hy = y + 0.5f - headCy;
                        float hd = Mathf.Sqrt(hx * hx + hy * hy);
                        if (!edge && hd > headR - 1.2f * u && hd <= headR && (y + 0.5f) < headCy) c = shade;
                        if (!edge && ((x * 7 + y * 13) % 31) == 0 && (y + 0.5f) < bodyCy + bodyRy * 0.6f) c = Color.Lerp(c, shade, 0.6f);
                    }
                    px[y * n + x] = c;
                }
            // 目 (白+黒の瞳+光) と口
            int eyeW = Mathf.Max(3, Mathf.RoundToInt(4 * u));
            int ey = Mathf.RoundToInt(headCy - eyeW * 0.5f + headR * 0.05f);
            var eyeXs = cyclops ? new[] { Mathf.RoundToInt(headCx - eyeW * 0.5f) } : new[] { Mathf.RoundToInt(headCx - headR * 0.5f - eyeW * 0.5f), Mathf.RoundToInt(headCx + headR * 0.5f - eyeW * 0.5f) };
            foreach (var bx in eyeXs)
            {
                for (int dy = 0; dy < eyeW; dy++)
                    for (int dx = 0; dx < eyeW; dx++)
                        if (Solid(bx + dx, ey + dy)) px[(ey + dy) * n + bx + dx] = Color.white;
                int pw = Mathf.Max(1, eyeW / 2);
                for (int dy = 0; dy < pw; dy++)
                    for (int dx = 0; dx < pw; dx++)
                        if (bx + dx + 1 < n && ey + dy + 1 < n) px[(ey + dy + 1) * n + bx + dx + 1] = Color.black;
                if (bx + pw + 1 < n && ey + pw + 1 < n) px[(ey + pw + 1) * n + bx + pw + 1 - 1] = new Color(0.85f, 0.9f, 1f);
                for (int dx = -1; dx <= eyeW; dx++) { if (Solid(bx + dx, ey - 1)) px[(ey - 1) * n + bx + dx] = outline; if (Solid(bx + dx, ey + eyeW)) px[(ey + eyeW) * n + bx + dx] = outline; }
            }
            int my = Mathf.RoundToInt(headCy - headR * 0.45f);
            int mw = Mathf.RoundToInt(headR * 0.5f);
            for (int dx = -mw; dx <= mw; dx++) { int mx = Mathf.RoundToInt(headCx) + dx; if (Solid(mx, my)) px[my * n + mx] = outline; }
            if (rng.NextDouble() < 0.5) for (int dx = -mw; dx <= mw; dx += Mathf.Max(1, mw)) { int mx = Mathf.RoundToInt(headCx) + dx; if (Solid(mx, my - 1)) px[(my - 1) * n + mx] = Color.white; }
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

        // ---- 演出の光の色 (絵の側＝color-theme の「絵の光」。PaperFx の役割の表には載せない)。
        // 同じ光を複数のファイルが直書きしていたので名前を置く (2026-09-29 戦闘画面の見直し p26。値は不変) ----
        /// <summary>灯の炎の芯 (淡い暖白 #fff6d2): 炎・光の筋の絵の芯、灯の火の粉 (LightUi)・灯を払う粒 (Presenter)</summary>
        public static readonly Color LampCore = UiKit.Hex("#fff6d2");
        /// <summary>灯の光 (暖色・α0.95): 灯の札の当たりの筋 (Presenter.HitColor)・ランタンの暈 (LightUi は透明度だけ変える)</summary>
        public static readonly Color LampGlow = new Color(1f, 0.9f, 0.62f, 0.95f);
        /// <summary>斬撃の白い芯 (紙色の光・α0.95): 札の当たりの既定の筋 (Presenter.HitColor)・火種の芯 (Tween)。
        /// 紙 (明) PaperFx.Paper3 に近いが光の色＝紙の色とは別に動かす</summary>
        public static readonly Color SlashCore = new Color(1f, 0.98f, 0.9f, 0.95f);

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

        /// <summary>斜線の地紋 (8×8・Point・Repeat): 45° の墨の線 2px (α0.35)。Image.type=Tiled で敷く (1 ドット = キャンバス 1 単位)。
        /// 自分の HP バーの「削られる分」の帯 (2026-09-29 p08。薔薇の薄塗りだけだと「すでに減った分」とも読めるので、見込みの斜線を重ねる)</summary>
        public static Sprite Hatch()
        {
            Sprite s;
            if (_cache.TryGetValue("hatch", out s)) return s;
            const int n = 8;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            var ink = PaperFx.Ink;
            var line = new Color(ink.r, ink.g, ink.b, 0.35f);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = (x + y) % n < 2 ? line : new Color(ink.r, ink.g, ink.b, 0f);
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _cache["hatch"] = s;
            return s;
        }

        /// <summary>暗幕の穴の縁のぼかし (9スライス。2026-09-29 戦闘画面のレビュー p11): 左・右・上の辺は c (暗幕の色) で、内側へ b ドットかけて透明になる。
        /// 下の辺はぼかさない (帳面と手札の暗幕の境は硬いまま)。穴の矩形いっぱいに Sliced で敷き、中は透明。raycastTarget は呼ぶ側で false に</summary>
        public static Sprite HoleFeather(Color c, int b) { return HoleFeather(c, b, b); }

        /// <summary>左右のぼかし幅 side と上のぼかし幅 top を分けた版 (敵が4体で左右に余白が無くても、上の縁は柔らかく)</summary>
        public static Sprite HoleFeather(Color c, int side, int top)
        {
            side = Mathf.Max(1, side); top = Mathf.Max(1, top);
            string key = "hole:" + side + ":" + top + ":" + ColorUtility.ToHtmlStringRGBA(c);
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            int w = 2 * side + 2, h = top + 2;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // 左・右・上の辺からの距離をそれぞれのぼかし幅で割った小さい方 (下の辺は数えない)
                    float k = Mathf.Clamp01(Mathf.Min(Mathf.Min(x, w - 1 - x) / (float)side, (h - 1 - y) / (float)top));
                    float a = c.a * (1f - k * k * (3f - 2f * k));   // smoothstep
                    px[y * w + x] = new Color(c.r, c.g, c.b, a);
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(side, 0f, side, top));
            _cache[key] = s;
            return s;
        }

        /// <summary>ビネット (周辺が暗くなる)。alpha だけの黒</summary>
        public static Sprite Vignette() { return Vignette(Color.black, "vignette"); }

        /// <summary>下から上へ消える縦のグラデーション (2026-09-29 I44 手札の後ろの手前の地面を沈める)。RGB は色そのもの、
        /// アルファは下の 40% で <paramref name="alpha"/> のまま一定、そこから上端まで smoothstep で 0。上端に線は付けない (案B の作業台にはしない)</summary>
        public static Sprite FadeUp(Color rgb, float alpha, string key)
        {
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int w = 4, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float k = y / (float)(h - 1);                       // 0 = 下端・1 = 上端
                float u = Mathf.Clamp01((k - 0.4f) / 0.6f);
                float a = alpha * (1f - u * u * (3f - 2f * u));     // 下 40% は一定、その上を smoothstep で 0 へ
                for (int x = 0; x < w; x++) px[y * w + x] = new Color(rgb.r, rgb.g, rgb.b, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _cache[key] = s;
            return s;
        }

        /// <summary>色つきの縁 (HP 危険域の薔薇など)。RGB は色そのもの、アルファだけ縁へ向かって濃くなる (黒の縁を Image で染めても黒のまま = 別の絵が要る)</summary>
        public static Sprite Vignette(Color rgb, string key)
        {
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
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
                    px[y * n + x] = new Color(rgb.r, rgb.g, rgb.b, a * a * 0.85f);
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _cache[key] = s;
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

        /// <summary>斬撃の太い筋 (96×24。白い芯・青緑の縁・薄い光。両端が尖る)。差し替えは Art/fx/slash_streak.png (2026-09-16 斬撃の豪華化)</summary>
        public static Sprite SlashStreak()
        {
            Sprite s;
            if (_cache.TryGetValue("slash_streak", out s)) return s;
            s = Theme.Art("fx", "slash_streak");
            if (s == null)
            {
                const int w = 96, h = 24;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[w * h];
                var core = Color.white; var edge = UiKit.Hex("#9fe0d6"); var glow = new Color(0.48f, 0.72f, 0.69f, 0.4f);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float t = (x + 0.5f) / w;                          // 0..1 (両端で細い。中央よりやや先端寄りが太い)
                        float thick = 1f + 8.5f * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.7f);
                        float dy = Mathf.Abs(y + 0.5f - h / 2f);
                        Color c;
                        if (dy < thick * 0.35f) c = core;
                        else if (dy < thick * 0.75f) c = edge;
                        else if (dy < thick * 1.05f) c = glow;
                        else continue;
                        px[y * w + x] = c;
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache["slash_streak"] = s;
            return s;
        }

        /// <summary>灯の筋 (2026-09-20 灯籠の放出): 斬撃の筋と同じ形で、縁が真鍮の紙・光が真鍮＝暖色の光線。差し替えは Art/fx/light_streak.png</summary>
        public static Sprite LightStreak()
        {
            Sprite s;
            if (_cache.TryGetValue("light_streak", out s)) return s;
            s = Theme.Art("fx", "light_streak");
            if (s == null)
            {
                const int w = 96, h = 24;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[w * h];
                var core = LampCore; var edge = UiKit.Hex("#ead08a"); var glow = new Color(0.79f, 0.6f, 0.23f, 0.45f);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float t = (x + 0.5f) / w;
                        float thick = 1f + 8.5f * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.7f);
                        float dy = Mathf.Abs(y + 0.5f - h / 2f);
                        Color c;
                        if (dy < thick * 0.35f) c = core;
                        else if (dy < thick * 0.75f) c = edge;
                        else if (dy < thick * 1.05f) c = glow;
                        else continue;
                        px[y * w + x] = c;
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache["light_streak"] = s;
            return s;
        }

        /// <summary>光の玉 (64×64。中心が白く縁へ薄れる円)。差し替えは Art/fx/glow.png</summary>
        public static Sprite Glow()
        {
            Sprite s;
            if (_cache.TryGetValue("glow", out s)) return s;
            s = Theme.Art("fx", "glow");
            if (s == null)
            {
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                        if (d >= 1f) continue;
                        float a = d < 0.35f ? 1f : (d < 0.6f ? 0.6f : (d < 0.85f ? 0.3f : 0.12f));   // 4段の階調 (ドット絵の光)
                        px[y * n + x] = new Color(1f, 1f, 1f, a);
                    }
                tex.SetPixels(px); tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache["glow"] = s;
            return s;
        }

        /// <summary>衝撃の輪 (64×64 の細い円環)。差し替えは Art/fx/ring.png</summary>
        public static Sprite Ring()
        {
            Sprite s;
            if (_cache.TryGetValue("ring", out s)) return s;
            s = Theme.Art("fx", "ring");
            if (s == null)
            {
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                        if (d > 31f || d < 26f) continue;
                        px[y * n + x] = (d > 27.5f && d < 29.5f) ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                    }
                tex.SetPixels(px); tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache["ring"] = s;
            return s;
        }

        /// <summary>火花 (8×8 の菱形)。差し替えは Art/fx/spark.png</summary>
        public static Sprite Spark()
        {
            Sprite s;
            if (_cache.TryGetValue("spark", out s)) return s;
            s = Theme.Art("fx", "spark");
            if (s == null)
            {
                const int n = 8;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float m = Mathf.Abs(x + 0.5f - 4f) + Mathf.Abs(y + 0.5f - 4f);
                        if (m > 4f) continue;
                        px[y * n + x] = m < 2f ? Color.white : new Color(1f, 0.95f, 0.75f, 1f);
                    }
                tex.SetPixels(px); tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache["spark"] = s;
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

        // ---- 合成札の絵 (2026-09-11 ユーザー裁定「素材2枚の絵をその場で溶かし合わせる」) ----

        static readonly System.Text.RegularExpressions.Regex FusedId = new System.Text.RegularExpressions.Regex("^fused_(.+)__(.+)$");
        static readonly Color ManaTeal = new Color(0.42f, 0.95f, 0.86f, 1f);

        /// <summary>計算合成 (fused_&lt;A&gt;__&lt;B&gt;) の絵: 素材2枚の 80×48 を斜めの継ぎ目で溶かし合わせる (左=A・右=B・継ぎ目にマナの線)。
        /// 同名2枚 (真・) は元の絵に青緑の内枠と光。素材の絵が片方でも無ければ null (呼び手は紋章へ)。engine の id 規則 (fusion.ts resolveFusedDef) と同じ貪欲一致で入れ子 (工房産を素材にした札) も辿る。決定的・キャッシュ</summary>
        public static Sprite FusedArt(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || !cardId.StartsWith("fused_")) return null;
            string key = "fusedart:" + cardId;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            var m = FusedId.Match(cardId);
            if (!m.Success) { _cache[key] = null; return null; }
            string ida = m.Groups[1].Value, idb = m.Groups[2].Value;
            var a = Theme.Art("cards", ida) ?? FusedArt(ida);
            var b = Theme.Art("cards", idb) ?? FusedArt(idb);
            if (a == null || b == null) { _cache[key] = null; return null; }
            try { s = ida == idb ? TrueForm(a) : Melt(a, b); }
            catch (Exception e) { Debug.LogWarning("[ThemeFx] 合成札の絵を作れなかった " + cardId + ": " + e.Message); s = null; }
            _cache[key] = s;
            return s;
        }

        static Color[] PixelsOf(Sprite sp, int w, int h)
        {
            // スプライトの矩形を w×h に (寸法が違えば最近傍で詰める)
            var tex = sp.texture; var r = sp.rect;
            int sw = (int)r.width, sh = (int)r.height;
            var src = tex.GetPixels((int)r.x, (int)r.y, sw, sh);
            if (sw == w && sh == h) return src;
            var outp = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    outp[y * w + x] = src[Math.Min(sh - 1, y * sh / h) * sw + Math.Min(sw - 1, x * sw / w)];
            return outp;
        }

        static Sprite MakeSprite(Color[] px, int w, int h, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(px); tex.Apply(false, false);
            var sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = name;
            return sp;
        }

        /// <summary>左=A・右=B。継ぎ目は下 36 → 上 44 の斜線 (1ドットの青緑＋両脇にほのかな光)。「溶け合う」を継ぎ目の1本で言う</summary>
        static Sprite Melt(Sprite a, Sprite b)
        {
            const int w = 80, h = 48;
            var pa = PixelsOf(a, w, h); var pb = PixelsOf(b, w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float xs = 36f + 8f * y / (h - 1f);   // texture 座標 (y=0 が下)
                for (int x = 0; x < w; x++)
                {
                    float d = (x + 0.5f) - xs;
                    Color c = d < 0f ? pa[y * w + x] : pb[y * w + x];
                    float ad = Math.Abs(d);
                    if (ad < 0.75f) c = Color.Lerp(c, ManaTeal, 0.95f);
                    else if (ad < 1.75f) c = Color.Lerp(c, ManaTeal, 0.45f);
                    else if (ad < 2.75f) c = Color.Lerp(c, ManaTeal, 0.15f);
                    px[y * w + x] = c;
                }
            }
            return MakeSprite(px, w, h, "fused:" + a.name + "+" + b.name);
        }

        /// <summary>真・化 (同名2枚): 元の絵に青緑の内枠 (1ドット) と内側へ薄れる光</summary>
        static Sprite TrueForm(Sprite a)
        {
            int w = (int)a.rect.width, h = (int)a.rect.height;
            var px = PixelsOf(a, w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int inset = Math.Min(Math.Min(x, w - 1 - x), Math.Min(y, h - 1 - y));
                    float k = inset == 1 ? 0.85f : inset == 2 ? 0.35f : inset == 3 ? 0.12f : 0f;
                    if (k > 0f) px[y * w + x] = Color.Lerp(px[y * w + x], ManaTeal, k);
                }
            return MakeSprite(px, w, h, "true:" + a.name);
        }

        // ---- ギアの絵 (2026-09-17 消耗品): PixelLab の挿絵 (Art/gears/<id>.png) が来るまでのコード生成の歯車 ----

        /// <summary>
        /// ギアの絵。`Assets/Resources/Art/gears/&lt;id&gt;.png` (32×32) があればそれ、無ければ id から決まる歯車 (歯数 6〜9・軸穴の大きさが id で変わる)。
        /// 干渉系 (family=interfere) だけ青緑 (からくりと同じ脈の色)、他は墨と真鍮。決定的・キャッシュ
        /// </summary>
        public static Sprite GearGlyph(string gearId, string family)
        {
            string key = "gearglyph:" + gearId;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Theme.Art("gears", gearId);
            if (s == null)
            {
                const int n = 20;
                uint hh = 2166136261u;
                foreach (var ch in gearId) { hh ^= ch; hh *= 16777619u; }
                var rng = new System.Random((int)(hh & 0x7fffffff));
                int teeth = 6 + rng.Next(0, 4);           // 6〜9 枚
                float hub = 0.16f + 0.10f * (float)rng.NextDouble();   // 軸穴の半径 (中心から)
                float phase = (float)rng.NextDouble() * Mathf.PI * 2f;
                bool teal = family == "interfere";
                Color body = teal ? UiKit.Hex("#3aa79b") : UiKit.Hex("#c99a3a");     // 脈の青緑 ／ 真鍮
                Color light = teal ? UiKit.Hex("#b5ddd6") : UiKit.Hex("#ead08a");
                Color dark = teal ? UiKit.Hex("#155650") : UiKit.Hex("#634410");
                Color edge = UiKit.Hex("#2f2e35");                                   // 墨の外線
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[n * n];
                float c = (n - 1) / 2f;
                bool In(int x, int y)
                {
                    if (x < 0 || y < 0 || x >= n || y >= n) return false;
                    float dx = x - c, dy = y - c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / (n / 2f);
                    if (r < hub) return false;                       // 軸穴
                    float a = Mathf.Atan2(dy, dx) + phase;
                    float tooth = 0.5f + 0.5f * Mathf.Cos(a * teeth); // 歯: 角度で外周が波打つ
                    float outer = 0.72f + 0.22f * (tooth > 0.55f ? 1f : 0f);
                    return r <= outer;
                }
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        Color col = new Color(0f, 0f, 0f, 0f);
                        if (In(x, y))
                        {
                            bool e = !In(x - 1, y) || !In(x + 1, y) || !In(x, y - 1) || !In(x, y + 1);
                            float dx = x - c, dy = y - c;
                            col = e ? dark : ((dx - dy) > 2f ? light : body);   // 右上が明るい (舞台の月光と同じ向き)
                        }
                        else if (In(x - 1, y) || In(x + 1, y) || In(x, y - 1) || In(x, y + 1)) col = edge;
                        px[y * n + x] = col;
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache[key] = s;
            return s;
        }

        /// <summary>
        /// 組めないギアの絵 (2026-09-29 p14): GearGlyph の画素を輝度へ 85% 寄せて彩度を落とし、沈んだ紙の色 (PaperFx.PaperDim) へ 35% 寄せた写し＝紙に溶けかけた灰色の影。
        /// 旧・0.72 の乗算は絵を暗く濃くし、組めないトークンのほうが重く見えた (使えない印として逆に働いていた)。×0.85 で暗くする案も墨の外線が残って「鋼の版」に見えたので、暗くせず紙へ寄せる。
        /// 絵の寸法・ピボット・PPU は元と同じ (preserveAspect の置き方を変えない)。読めない絵 (Read/Write 無効) は null＝呼び出し側で元の絵に α0.6
        /// </summary>
        public static Sprite GearGlyphMuted(string gearId, string family)
        {
            return MutedCopy(GearGlyph(gearId, family), "gearglyph-muted:" + gearId, 0.85f, 0.35f);
        }

        /// <summary>出せない札のコスト玉 (2026-09-30 F26: 乗算 0.82 では暗い金のままで「灰の玉」にならなかった)。PixelLab の cost_orb を輝度の灰へ 70%・淡い紙色へ 15% 寄せた写し。読めなければ null</summary>
        public static Sprite CostOrbMuted()
        {
            return MutedCopy(Theme.Art("ui", "cost_orb"), "cost_orb-muted", 0.7f, 0.15f);
        }

        /// <summary>ドット絵の灰の写し (輝度へ toLuma・PaperFx.PaperDim へ toPaper 寄せる。rect と同じ寸法・Point・元のピボットと PPU)。読めなかった時も null を覚える</summary>
        static Sprite MutedCopy(Sprite src, string key, float toLuma, float toPaper)
        {
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = null;
            if (src != null && src.texture != null)
            {
                Color32[] px = null;
                try { px = src.texture.GetPixels32(); } catch (Exception) { px = null; }
                int tw = src.texture.width, th = src.texture.height;
                var rect = src.rect;
                int rx = Mathf.RoundToInt(rect.x), ry = Mathf.RoundToInt(rect.y), rw = Mathf.RoundToInt(rect.width), rh = Mathf.RoundToInt(rect.height);
                if (px != null && rw > 0 && rh > 0 && rx >= 0 && ry >= 0 && rx + rw <= tw && ry + rh <= th && px.Length >= tw * th)
                {
                    Color pd = PaperFx.PaperDim;
                    var outPx = new Color32[rw * rh];
                    for (int y = 0; y < rh; y++)
                        for (int x = 0; x < rw; x++)
                        {
                            Color32 c = px[(ry + y) * tw + (rx + x)];
                            float r = c.r / 255f, gg = c.g / 255f, b = c.b / 255f;
                            float l = 0.30f * r + 0.59f * gg + 0.11f * b;
                            r = Mathf.Lerp(Mathf.Lerp(r, l, toLuma), pd.r, toPaper);
                            gg = Mathf.Lerp(Mathf.Lerp(gg, l, toLuma), pd.g, toPaper);
                            b = Mathf.Lerp(Mathf.Lerp(b, l, toLuma), pd.b, toPaper);
                            outPx[y * rw + x] = new Color32((byte)Mathf.RoundToInt(r * 255f), (byte)Mathf.RoundToInt(gg * 255f), (byte)Mathf.RoundToInt(b * 255f), c.a);
                        }
                    var tex = new Texture2D(rw, rh, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.SetPixels32(outPx);
                    tex.Apply(false, false);
                    s = Sprite.Create(tex, new Rect(0, 0, rw, rh), new Vector2(src.pivot.x / rw, src.pivot.y / rh), src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    s.name = key;
                }
            }
            _cache[key] = s;   // 読めなかった時も null を覚える (作り直しのたびに例外を起こさない)
            return s;
        }

        /// <summary>
        /// 真鍮のランタン (2026-09-20 灯の表示・案B): 32×48 ドットの正面図＝吊り輪・笠・黒鉄の枠・硝子の窓 (x 7〜24・y 14〜35＝暗いまま。炎と数字は LightUi が上に描く)・台座。
        /// 差し替えは Art/ui/lantern.png (同じ寸法・硝子は暗く空けておく)。lit=false は消灯の色 (黒鉄も真鍮も沈む)
        /// </summary>
        public static Sprite Lantern(bool lit)
        {
            string key = lit ? "lantern" : "lantern-dark";
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Theme.Art("ui", lit ? "lantern" : "lantern_dark");
            if (s == null && !lit) s = Theme.Art("ui", "lantern");   // 消灯の絵が無ければ点灯の絵を暗く (LightUi が色を落とす)
            if (s == null)
            {
                const int W = 32, H = 48;
                Color iron = lit ? UiKit.Hex("#2f2e35") : UiKit.Hex("#26252c"), ironHi = lit ? UiKit.Hex("#4e4c55") : UiKit.Hex("#3a393f");
                Color brass = lit ? UiKit.Hex("#c99a3a") : UiKit.Hex("#8a6d33"), brassHi = lit ? UiKit.Hex("#ead08a") : UiKit.Hex("#a58a52"), brassLo = lit ? UiKit.Hex("#634410") : UiKit.Hex("#4a3410");
                Color glass = UiKit.Hex("#20233a");
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[W * H];
                // 上が y=0 の設計図をそのまま書く (Texture2D は下が 0 なので反転)
                void R(int x0, int y0, int w0, int h0, Color c)
                {
                    for (int y = y0; y < y0 + h0; y++) for (int x = x0; x < x0 + w0; x++)
                        if (x >= 0 && x < W && y >= 0 && y < H) px[(H - 1 - y) * W + x] = c;
                }
                R(14, 0, 4, 1, brassLo); R(13, 1, 1, 3, brass); R(18, 1, 1, 3, brass); R(14, 3, 4, 1, brassHi);        // 吊り輪
                int[] capX = { 13, 11, 9, 7, 6 }, capW = { 6, 10, 14, 18, 20 };
                for (int i = 0; i < 5; i++) R(capX[i], 4 + i, capW[i], 1, i < 2 ? brassHi : brass);                    // 笠
                R(6, 9, 20, 1, brassLo);
                R(4, 10, 24, 2, brass); R(4, 10, 24, 1, brassHi);                                                       // 上の板
                R(4, 12, 24, 26, iron); R(5, 12, 1, 26, ironHi); R(4, 12, 24, 1, ironHi);                              // 胴の枠
                R(7, 14, 18, 22, glass);                                                                                // 硝子 (暗い)
                R(15, 14, 2, 22, new Color(0.184f, 0.18f, 0.208f, 1f));                                                 // 桟
                R(5, 13, 1, 1, brassHi); R(26, 13, 1, 1, brassHi); R(5, 36, 1, 1, brassHi); R(26, 36, 1, 1, brassHi);    // 鋲
                R(4, 38, 24, 2, brass); R(4, 39, 24, 1, brassLo);                                                       // 下の板
                R(13, 40, 6, 3, iron); R(13, 40, 1, 3, ironHi);                                                         // 台座
                R(10, 43, 12, 2, brass); R(8, 45, 16, 2, brass); R(8, 45, 16, 1, brassHi); R(7, 47, 18, 1, brassLo);
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache[key] = s;
            return s;
        }

        /// <summary>炎 (12×16 ドット・真鍮の系: 白い芯 → 真鍮の紙 → 真鍮)。差し替えは Art/fx/flame.png。原点は根元の中央</summary>
        public static Sprite Flame()
        {
            Sprite s;
            if (_cache.TryGetValue("flame", out s)) return s;
            s = Theme.Art("fx", "flame");
            if (s == null)
            {
                const int W = 12, H = 16;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[W * H];
                Color core = LampCore, mid = UiKit.Hex("#ead08a"), edge = UiKit.Hex("#c99a3a");
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        // 涙の形: 下が丸く上が尖る。t=0 根元・1 先端
                        float t = (y + 0.5f) / H;
                        float half = t < 0.35f ? Mathf.Lerp(3.2f, 5.6f, t / 0.35f) : Mathf.Lerp(5.6f, 0.6f, (t - 0.35f) / 0.65f);
                        float dx = Mathf.Abs(x + 0.5f - W / 2f);
                        if (dx > half) continue;
                        float inner = dx / Mathf.Max(0.5f, half);
                        Color c = t < 0.55f && inner < 0.45f ? core : (inner < 0.75f ? mid : edge);
                        px[y * W + x] = c;
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            }
            _cache["flame"] = s;
            return s;
        }

        public static Sprite CardArt(string cardId, Color tint)
        {
            string key = "cardart:" + cardId;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            s = Theme.Art("cards", cardId);
            if (s == null) s = FusedArt(cardId);
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

        /// <summary>
        /// 挿絵の中央を w×h ドットで切り出した絵 (2026-09-29 p25。置物の付箋のサムネイル 48×29)。縮めずに1ドット＝1単位で置くため
        /// (80×48 を 0.6 倍に縮めるとドットが間引かれていた)。絵がその寸法以下の辺は切らない。画素を写した別のテクスチャ
        /// (＝テクスチャ丸ごとのスプライト。UiKit.PixelArt が当たる)。読めない絵は同じテクスチャの部分矩形 (最近傍のまま)。決定的・キャッシュ
        /// </summary>
        public static Sprite CardArtCrop(string cardId, Color tint, int w, int h)
        {
            string key = "cardcrop:" + cardId + ":" + w + "x" + h;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            var src = CardArt(cardId, tint);
            s = src;
            if (src != null && src.texture != null)
            {
                var r = src.rect;
                int sw = Mathf.RoundToInt(r.width), sh = Mathf.RoundToInt(r.height);
                int cw = Math.Min(w, sw), ch = Math.Min(h, sh);
                if (cw < sw || ch < sh)
                {
                    int x0 = Mathf.RoundToInt(r.x) + (sw - cw) / 2, y0 = Mathf.RoundToInt(r.y) + (sh - ch) / 2;
                    try
                    {
                        var px = src.texture.GetPixels(x0, y0, cw, ch);
                        var tex = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
                        tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
                        tex.SetPixels(px); tex.Apply(false, false);
                        s = Sprite.Create(tex, new Rect(0, 0, cw, ch), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    }
                    catch (Exception)
                    {
                        s = Sprite.Create(src.texture, new Rect(x0, y0, cw, ch), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    }
                    s.name = key;
                }
            }
            _cache[key] = s;
            return s;
        }
    }
}
