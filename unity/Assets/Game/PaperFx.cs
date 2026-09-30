// PaperFx.cs — 「絵本」の肌 (2026-09-07 デザインカンバス第5版で決定) の生成部品。
// クリーム色の紙の9スライス (鉛筆の二重線)・水彩のにじみ・紙の粒・貼り絵の縁 (ドット絵の切り抜き)・タイプのしおり。マスキングテープは廃止 (2026-09-09 ユーザー「テープの書き方はやめて」)。
// 規約「絵はドット、紙と文字はなめらか」: ここで作るのは紙と線 (なめらか側)。ドット絵は Point フィルタで整数倍に置く。
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class PaperFx
    {
        // ---- カラーテーマ「黒鉄と真鍮」(2026-09-16 ユーザー裁定。一次資料 docs/color-theme.md)。UI の色はここが唯一の出典 ----
        // 肌 (クリーム色の紙・鉛筆の二重線・水彩) は不変。決めたのは「役割 → 色」: 真鍮＝価値と資源 (選択・決定・G・エナジー・レア・行動が変わる線)／
        // 脈の青緑＝からくりと光 (仕込み札のトークン・斬撃の縁・露頭)／墨だけ鉛筆の黒鉄／意味の色は4つ (薔薇 HP・鋼青 ブロック・苔 成長・藤 状態異常)。
        // エナジーは同日ユーザー「カードのエナジー表記は緑より黄色系がいい」で真鍮 (コスト玉・輪・数字)。
        // 文字は墨か各色の「墨」版だけ (紙の上で 7:1 前後)。淡い色は塗りにだけ使う。新しい色は足さない (足すなら役割を決めて表に書く)。
        static Color H(string hex) { Color c; return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta; }
        // 紙と墨
        public static readonly Color Paper = H("#f4ecd6");
        public static readonly Color Paper2 = H("#eadfc4");
        public static readonly Color Paper3 = H("#fbf6e8");
        /// <summary>夜の上に置く淡い紙色の文字 (9:1)</summary>
        public static readonly Color PaperDim = H("#c4beb2");
        /// <summary>墨＝鉛筆の黒鉄 (紙の上で 11.4:1)。旧 #3b2f2f (焦げ茶)</summary>
        public static readonly Color Ink = H("#2f2e35");
        /// <summary>中墨 (紙の上で 7.2:1)。透明度で薄めない</summary>
        public static readonly Color InkSoft = H("#4e4c55");
        /// <summary>出せない札・組めないギアの紙に掛ける乗算 (紙を中立の灰へ沈める＝承認済みのカード案の状態一覧 saturate(0.35) に合わせる。
        /// 2026-09-30 F26: 旧 (0.92,0.90,0.86) は青を多く削って黄ばんだ紙 (225,213,184) に見えた。明るさの比 1.25 と中墨 5.7:1 は保つ)</summary>
        public static readonly Color DimTint = new Color(0.88f, 0.90f, 0.96f, 1f);
        // 夜 (紙＝暖・夜＝寒 の対比がこの UI の芯)
        public static readonly Color Night = H("#1a1c33");
        /// <summary>札の挿絵の窓・からくりの窓</summary>
        public static readonly Color Window = H("#20233a");
        /// <summary>画面の外・最奥</summary>
        public static readonly Color Ground = H("#0f1120");
        /// <summary>確認の窓の暗幕 (地 α0.55。行動中の敵の周りだけ穴を開けて明るく残す。2026-09-29 p11 の値・p26 で名前にした)</summary>
        public static readonly Color Scrim = new Color(Ground.r, Ground.g, Ground.b, 0.55f);
        /// <summary>窓 (BattleScreen.Modal・札の拡大 CardPopup) の暗幕 (地 α0.7。旧 #141228 α0.68／0.72 の2値を地にそろえた。2026-09-29 p26)</summary>
        public static readonly Color ModalScrim = new Color(Ground.r, Ground.g, Ground.b, 0.7f);
        // 真鍮＝価値と資源 (選択中の札と狙っている敵の縁・決定のボタン・G・エナジー・R の外線・HP バーの「行動が変わる線」)
        public static readonly Color Brass = H("#c99a3a");
        /// <summary>決定のボタン・予告の札の地</summary>
        public static readonly Color BrassLight = H("#ead08a");
        /// <summary>真鍮の墨 (紙の上で 7.5:1): 予告の文字・G の数字・注意書き</summary>
        public static readonly Color BrassInk = H("#634410");
        // 脈の青緑＝からくりと光 (仕込み札のトークンの縁と帯・「仕込む」・斬撃の縁・露頭)
        public static readonly Color Mana = H("#3aa79b");
        /// <summary>「仕込む」の地・生きた罠の縁</summary>
        public static readonly Color ManaLight = H("#b5ddd6");
        /// <summary>青緑の墨 (紙の上で 7.2:1): からくりの文字・今ターン鳴る帯</summary>
        public static readonly Color ManaInk = H("#155650");
        /// <summary>からくりの帯 (紙の文字を乗せる濃い青緑 4.2:1)</summary>
        public static readonly Color ManaBand = H("#2a7d74");
        // 意味の色 (塗り。文字は「墨」版)
        /// <summary>薔薇＝HP バー・与ダメの札・敗北</summary>
        public static readonly Color Rose = H("#c9635a");
        public static readonly Color RoseLight = H("#fadbd6");
        /// <summary>薔薇の薄塗り (Rose と Paper を 0.55 で混ぜた値): 自分の HP バーの「削られる分」の帯。塗りの上に重ね、墨の斜線 (ThemeFx.Hatch) を敷く (2026-09-29 p08)</summary>
        public static readonly Color RoseLoss = H("#e1ae9e");
        /// <summary>鋼青＝ブロック</summary>
        public static readonly Color Sky = H("#6f95b8");
        public static readonly Color SkyLight = H("#d6e6fa");
        public static readonly Color SkyInk = H("#2f5a7a");
        /// <summary>苔＝成長・良い (割引の玉)</summary>
        public static readonly Color Moss = H("#7fa86c");
        public static readonly Color MossLight = H("#cfeacc");
        /// <summary>良いの墨: 鍛えた数字・割引のコスト・上がった数字</summary>
        public static readonly Color GoodInk = H("#276a34");
        /// <summary>危険の墨: 被ダメの数字・エラー・敗因</summary>
        public static readonly Color BadInk = H("#9c3a2a");
        /// <summary>下がった数字 (紙の上で 5.5:1)</summary>
        public static readonly Color BadDown = H("#a33a30");
        /// <summary>藤＝状態異常の印</summary>
        public static readonly Color Plum = H("#9d86bf");
        public static readonly Color PlumLight = H("#e9def3");
        /// <summary>状態異常の文字 (藤の紙の上で 6.8:1)</summary>
        public static readonly Color PlumInk = H("#5a3d78");
        /// <summary>延焼・炎 (赤の資源。絵の色に近い橙)</summary>
        public static readonly Color Ember = H("#e8742f");
        /// <summary>危険のボタン (「ランを放棄」・素材を外す×)</summary>
        public static readonly Color DangerBtn = H("#e8b8b0");
        // タイプの帯 (淡い色＋墨の文字。CardView のリボン)
        public static readonly Color Sand = H("#c9a982");
        /// <summary>呪文の帯: 藤を同じ色相のまま明るく (墨 6.1:1。旧 #a98cc4 は 4.6:1 で細い字が沈んだ。2026-09-29 p19)</summary>
        public static readonly Color PlumBand = H("#bca6d6");
        public static readonly Color Teal = H("#7ab8b0");
        /// <summary>置物の帯: 真鍮から離した鈍い黄 (選択の縁と混ざらない)。旧 蜂蜜→#a8a66b (墨 5.3:1)→#b3b67a (墨 6.3:1・砂との差が広がる。2026-09-29 p19)</summary>
        public static readonly Color Olive = H("#b3b67a");
        // 旧名の別名 (段階的に消す)
        public static readonly Color Honey = Brass;
        public static readonly Color GoldInk = BrassInk;

        // ---- 夜の札 (HD-2D 見本 ui=night。2026-09-30 P20・ユーザー裁定③「舞台の上に常に出る札は夜色の地に紙色の文字」) ----
        // 紙の役割をそのまま夜へ写す: 墨 → 紙色、各色の墨 → 同じ色の淡い版、淡い塗り → 同じ色相の暗い塗り (Nightify が写す)。
        // 文字は夜の地で 9:1 以上、色つきの暗い塗りの上で 7:1 以上 (WCAG の式で確かめた値)。対象は敵の帳面・意図の札・自分の札 (からくり・ギア・置物)・
        // 上部バー・人形の札だけ。紙のまま残すのは手札・確認の窓・メニューと窓 (Nightify を呼ばない所)。役割は増やさない＝紙の表の1対1の写し
        /// <summary>夜の札 (主) の地: 意図の攻撃の札・上部バーのボタン・レリックの円 (紙 Tag の役。紙色の文字と 12:1)</summary>
        public static readonly Color NightHi = H("#262943");
        /// <summary>夜の札の線 (紙 (濃) の札の墨の縁・区切り線の役。夜の上で 4.8:1)</summary>
        public static readonly Color NightEdge = H("#8a879c");
        /// <summary>夜の上の小さな札 (帳面の状態・rider) の地 (紙 (濃) の MiniPill の役。紙色 11:1・淡い紙色 7.2:1)</summary>
        public static readonly Color NightPill = H("#2a2d48");
        /// <summary>夜の札の HP バーの空き (紙の track の役)</summary>
        public static readonly Color NightTrack = H("#31344f");
        /// <summary>墨 → 紙色 (夜の上で 14:1)</summary>
        public static readonly Color NightInk = Paper;
        /// <summary>中墨 → 淡い紙色 (9:1)</summary>
        public static readonly Color NightInkSoft = PaperDim;
        /// <summary>真鍮の墨 → 淡い真鍮 (11:1)</summary>
        public static readonly Color NightBrassInk = BrassLight;
        /// <summary>青緑の墨 → 淡い青緑 (11:1)</summary>
        public static readonly Color NightManaInk = ManaLight;
        /// <summary>危険の墨・下がった数字 → 淡い薔薇 (8.2:1)</summary>
        public static readonly Color NightBadInk = H("#f2a193");
        /// <summary>良いの墨 → 淡い苔 (10:1)</summary>
        public static readonly Color NightGoodInk = H("#a6d99a");
        /// <summary>鋼青の墨 → 淡い鋼青 (9.7:1)</summary>
        public static readonly Color NightSkyInk = H("#a8c9ea");
        /// <summary>藤の墨 → 淡い藤 (9.1:1)</summary>
        public static readonly Color NightPlumInk = H("#cdb6ea");
        /// <summary>淡い真鍮の塗り (予告の札) → 暗い真鍮 (淡い真鍮の文字と 8.3:1)</summary>
        public static readonly Color NightBrass = H("#3d3214");
        /// <summary>淡い青緑の塗り (からくり) → 暗い青緑 (7.9:1)</summary>
        public static readonly Color NightMana = H("#173f3c");
        /// <summary>淡い薔薇の塗り (危険・先に壊す) → 暗い薔薇 (7.2:1)</summary>
        public static readonly Color NightRose = H("#3d2028");
        /// <summary>淡い鋼青の塗り (盾の札・ブロックの rider) → 暗い鋼青 (7.4:1)</summary>
        public static readonly Color NightSky = H("#1f344c");
        /// <summary>淡い苔の塗り → 暗い苔 (7.4:1)</summary>
        public static readonly Color NightMoss = H("#243c2a");
        /// <summary>藤の紙 (状態異常の札) → 暗い藤 (7.4:1)</summary>
        public static readonly Color NightPlum = H("#33284d");

        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>タイプの帯の色 (淡い色＋墨の文字)。物理=砂・呪文=藤・仕込み札=青緑・置物=鈍い黄 (2026-09-16 蜂蜜→オリーブ)</summary>
        public static Color TypeColor(string type)
        {
            switch (type)
            {
                case "spell": return PlumBand;
                case "reaction": return Teal;
                case "permanent": return Olive;
                default: return Sand;
            }
        }

        /// <summary>レア度の外線 (C 墨の半分・U 鋼青・R 真鍮)</summary>
        public static Color RarityEdge(string rarity)
        {
            string r = rarity ?? "common";
            return r == "rare" ? Brass : r == "uncommon" ? Sky : new Color(Ink.r, Ink.g, Ink.b, 0.5f);
        }

        /// <summary>役割の色 (しるし・にじみ)。dmg=薔薇・block=鋼青・counter=青緑・growth=苔・momentum=真鍮・expose=真鍮</summary>
        public static Color RoleColor(string role)
        {
            switch (role)
            {
                case "block": return Sky;
                case "counter": return Mana;
                case "growth": return Moss;
                case "momentum": return Brass;
                case "expose": return Brass;
                default: return Rose;
            }
        }

        // ---- 9スライス (角丸の紙。1テクセル=1px で線を細く保つ。UiKit.Frame は名前が paper で始まる絵を1倍で貼る) ----

        /// <summary>紙のパネル: 外から 淡い線1・紙3・墨2・紙。角丸 12</summary>
        public static Sprite Panel { get { return Nine("paper_panel", 48, 12, 14, PanelBands, false); } }
        /// <summary>紙の札 (小さな帯): 墨2・紙。角丸 8</summary>
        public static Sprite Tag { get { return Nine("paper_tag", 32, 8, 10, TagBands, false); } }
        /// <summary>紙 (濃) の札: 情報の段 (帳面・上部バーの札・からくりのトークン・付箋・山札の札)。手札と確認の窓 (紙) を一段前に出す (2026-09-16 戦闘画面の色の序列)</summary>
        public static Sprite Tag2 { get { return Nine("paper_tag2", 32, 8, 10, Tag2Bands, false); } }
        /// <summary>細い縁の札 (2026-09-29 p18 帳面の状態の札・頭上の rider): 中墨の縁1px・中は白 (Image.color で紙の色を掛ける＝塗りがその紙の色ちょうどになる)。
        /// 帳面の外枠と HP バーの枠 (墨2px) より弱い線にして、状態の札が帳面でいちばん重い線にならないように</summary>
        public static Sprite TagThin { get { return Nine("paper_tag_thin", 32, 8, 10, d => d < 1f ? InkSoft : Color.white, false); } }
        /// <summary>夜の札 (2026-09-29 手番の札の「敵の番」): 不透明の夜 #1a1c33 に紙 (濃) の縁2px。紙の文字を載せる。
        /// NightNote (夜 α0.84・縁は墨) は夜空との差が 1.6:1 で札の形が溶けるので、縁を紙 (濃) にして輪郭を残す (夜空と 7.5:1)</summary>
        public static Sprite NightTag { get { return Nine("night_tag", 32, 8, 10, NightTagBands, false); } }
        /// <summary>夜の札 (主。HD-2D 見本 ui=night・2026-09-30 P20): 紙 (濃) の縁2px・夜 (主) NightHi の地。紙の Tag の役 (意図の攻撃の札・上部バーの札)</summary>
        public static Sprite NightCard { get { return Nine("night_card", 32, 8, 10, d => d < 2f ? Paper2 : NightHi, false); } }
        /// <summary>夜の札 (情報): 夜の線 NightEdge 2px・夜の地。紙 (濃) の Tag2 の役 (帳面・自分の札・トークン・人形の札)</summary>
        public static Sprite NightCard2 { get { return Nine("night_card2", 32, 8, 10, d => d < 2f ? NightEdge : Night, false); } }
        /// <summary>夜の色つきの札 (Image.color で染める): 白の縁 1.5px・灰 NightTintFill の地＝縁が地より一段明るい同じ色相。淡い塗りの札 (状態・rider・盾) の役。
        /// 色は NightTintOf (塗りの色 ÷ NightTintFill) で渡す</summary>
        public static Sprite NightTint { get { return Nine("night_tint", 32, 8, 10, d => d < 1.5f ? Color.white : new Color(NightTintFill, NightTintFill, NightTintFill, 1f), false); } }
        /// <summary>NightTint の地の灰 (縁との明るさの比)</summary>
        public const float NightTintFill = 0.72f;
        /// <summary>夜のボタン: 紙 (濃) の縁2px・夜 (主) の地・下に厚み。紙の Button の役 (上部バーのマップ・メモ・≡)</summary>
        public static Sprite NightButton { get { return Nine("night_button", 40, 10, 12, d => d < 2f ? Paper2 : NightHi, true); } }
        /// <summary>紙のボタン: 墨2・紙、下に厚み (墨 50%) 4px。角丸 10</summary>
        public static Sprite Button { get { return Nine("paper_button", 40, 10, 12, TagBands, true); } }
        /// <summary>カードの面: パネルと同じ二重線。角丸 14</summary>
        public static Sprite Card { get { return Nine("paper_card", 52, 14, 16, PanelBands, false); } }
        /// <summary>カードの面・案B (2026-09-09): 外側の線の色がレア度 (C 墨50%・U 空・R 蜂蜜=2px)。差し替えは Art/ui/paper_card_<rarity>.png</summary>
        public static Sprite CardOf(string rarity)
        {
            string r = rarity ?? "common";
            Color line = RarityEdge(r);
            float lw = r == "common" ? 1f : 2f;
            return Nine("paper_card_" + r, 52, 14, 16, d => d < lw ? line : (d < 4f ? Paper : (d < 6f ? Ink : Paper)), false);
        }

        static Color PanelBands(float d)
        {
            if (d < 1f) return new Color(Ink.r, Ink.g, Ink.b, 0.5f);
            if (d < 4f) return Paper;
            if (d < 6f) return Ink;
            return Paper;
        }
        static Color TagBands(float d)
        {
            if (d < 2f) return Ink;
            return Paper;
        }
        static Color Tag2Bands(float d)
        {
            if (d < 2f) return Ink;
            return Paper2;
        }
        static Color NightTagBands(float d)
        {
            if (d < 2f) return Paper2;
            return Night;
        }

        static Sprite Nine(string name, int size, int radius, int border, Func<float, Color> bands, bool thickBottom)
        {
            Sprite s;
            if (_cache.TryGetValue(name, out s)) return s;
            s = Theme.Art("ui", name);
            if (s == null)
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[size * size];
                float half = size / 2f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        // 角丸矩形の内側距離 (ピクセル中心)。d<0 は外
                        float cx = x + 0.5f - half, cy = y + 0.5f - half;
                        float qx = Math.Abs(cx) - (half - radius), qy = Math.Abs(cy) - (half - radius);
                        float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                        float d = -outside; // 内側ほど大きい
                        Color c;
                        if (d < 0f) c = new Color(0f, 0f, 0f, 0f);
                        else
                        {
                            c = bands(d);
                            if (d < 1f) c.a *= Mathf.Clamp01(d + 0.5f); // 縁を半ドットだけ滑らかに
                            if (thickBottom && y < 4 && d >= 2f) c = Color.Lerp(c, Ink, 0.5f);
                        }
                        px[y * size + x] = c;
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
                s.name = name;
            }
            _cache[name] = s;
            return s;
        }

        // ---- 水彩のにじみ (役割の札・舞台の後ろ) ----

        public static Sprite Blob(Color color)
        {
            string key = "blob:" + ColorUtility.ToHtmlStringRGBA(color);
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int w = 96, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            var light = Color.Lerp(color, Color.white, 0.25f);
            var dark = Color.Lerp(color, Color.black, 0.12f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f, ny = (y + 0.5f) / h * 2f - 1f;
                    float ang = Mathf.Atan2(ny, nx);
                    float wobble = 1f + 0.08f * Mathf.Sin(ang * 3f + 0.7f) + 0.05f * Mathf.Cos(ang * 5f - 1.3f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny) / wobble;
                    float a = Mathf.Clamp01((0.98f - r) / 0.10f);           // 縁は 10% で落ちる
                    float t = Mathf.Clamp01((nx + 0.6f) * 0.5f + (ny + 0.6f) * 0.3f);
                    var c = Color.Lerp(light, dark, t);
                    c.a = a * (0.92f - 0.1f * r);
                    px[y * w + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key;
            _cache[key] = s;
            return s;
        }

        /// <summary>紙の粒 (乗算の代わりに、墨の低い不透明度の点を敷く)。Image.type = Tiled で使う</summary>
        public static Sprite Grain()
        {
            Sprite s;
            if (_cache.TryGetValue("grain", out s)) return s;
            const int n = 128;
            var rng = new System.Random(20260907);
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++)
            {
                double v = rng.NextDouble();
                float a = v < 0.55 ? 0f : (float)((v - 0.55) / 0.45) * 0.09f;
                px[i] = new Color(Ink.r, Ink.g, Ink.b, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "grain";
            _cache["grain"] = s;
            return s;
        }

        /// <summary>タイプのしおり (16×24 のドット。下端に切り込み)。2倍で貼る</summary>
        public static Sprite Bookmark(Color color)
        {
            string key = "bookmark:" + ColorUtility.ToHtmlStringRGB(color);
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int w = 16, h = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            var edge = Color.Lerp(color, Ink, 0.55f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // 下の切り込み: y が小さいほど (下) 中央が欠ける
                    int notch = 4 - y; // y=0 で 4, y=3 で 1
                    bool cut = notch > 0 && Math.Abs(x - (w - 1) / 2f) < notch;
                    bool inside = !cut;
                    if (!inside) { px[y * w + x] = new Color(0f, 0f, 0f, 0f); continue; }
                    bool border = x == 0 || x == w - 1 || y == h - 1 || (notch > 0 && Math.Abs(x - (w - 1) / 2f) < notch + 1) || y == 0;
                    px[y * w + x] = border ? edge : color;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key;
            _cache[key] = s;
            return s;
        }

        /// <summary>コスト玉 (52px): 色の玉 (左上が明るい) に墨の輪2・紙の輪2・淡い墨の外線1。案B のカードの左上</summary>
        public static Sprite Orb(Color color)
        {
            string key = "orb:" + ColorUtility.ToHtmlStringRGB(color);
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int n = 52;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            float c = n / 2f;
            var light = Color.Lerp(color, Color.white, 0.35f);
            var dark = Color.Lerp(color, Color.black, 0.18f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    Color col;
                    if (r < 20f) col = Color.Lerp(light, dark, Mathf.Clamp01((dx - dy) / 40f + 0.5f));   // 左上が明るい
                    else if (r < 22f) col = Ink;
                    else if (r < 24f) col = Paper;
                    else if (r < 25f) col = new Color(Ink.r, Ink.g, Ink.b, 0.5f);
                    else col = new Color(0f, 0f, 0f, 0f);
                    if (r >= 24f && r < 25.5f) col.a *= Mathf.Clamp01(25.5f - r);   // 外縁を滑らかに
                    px[y * n + x] = col;
                }
            tex.SetPixels(px); tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key; _cache[key] = s; return s;
        }

        /// <summary>タイプの帯 (150×26): 両端が尖った帯。塗りはタイプ色、内側 1.5px は墨寄りの線。案B の窓の下端に掛ける</summary>
        public static Sprite Ribbon(Color color)
        {
            string key = "ribbon:" + ColorUtility.ToHtmlStringRGB(color);
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int w = 150, h = 26;
            float hh = h / 2f;
            var edge = Color.Lerp(color, Ink, 0.55f);
            bool Inside(float x, float y, float m)
            {
                float yy = Mathf.Abs(y - hh);
                if (yy > hh - m) return false;
                if (x >= 8f + m && x <= w - 8f - m) return true;
                float tx = (x < w / 2f ? x : w - x) - m * 1.3f;   // 尖った端: 幅が中央へ向けて広がる
                if (tx < 0f) return false;
                return yy <= hh * (tx / 8f) - m * 0.5f;
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    Color col = !Inside(fx, fy, 0f) ? new Color(0f, 0f, 0f, 0f) : (!Inside(fx, fy, 1.5f) ? edge : color);
                    px[y * w + x] = col;
                }
            tex.SetPixels(px); tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key; _cache[key] = s; return s;
        }

        /// <summary>吹き出しの尾 (下向きの小さな三角。紙色に墨の線)</summary>
        public static Sprite BubbleTail() { return TailSprite("tail", Ink, Paper); }

        /// <summary>夜の札の尾 (BubbleTail の夜の版。2026-09-30 P20): 紙 (濃) の線・夜 (主) NightHi の地 = NightCard と同じ組</summary>
        public static Sprite NightBubbleTail() { return TailSprite("night_tail", Paper2, NightHi); }

        static Sprite TailSprite(string key, Color edgeCol, Color fillCol)
        {
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int w = 30, h = 22;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // 上辺が幅いっぱい、下の先端 (x≈8) へ細る
                    float t = 1f - (y + 0.5f) / h;           // 上=0, 下=1
                    float left = Mathf.Lerp(2f, 8f, t), right = Mathf.Lerp(w - 4f, 12f, t);
                    bool inside = x + 0.5f > left && x + 0.5f < right;
                    bool edge = inside && (x + 0.5f < left + 2f || x + 0.5f > right - 2f || y < 2);
                    px[y * w + x] = !inside ? new Color(0f, 0f, 0f, 0f) : (edge ? edgeCol : fillCol);
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key;
            _cache[key] = s;
            return s;
        }

        // ---- 貼り絵: ドット絵の切り抜きの縁 (紙色に膨らませた影絵) ----

        /// <summary>src の不透明部分を pad テクセルぶん膨らませた紙色の影絵。ドット絵の縁は崩さず、その外に紙の縁を足す</summary>
        public const int SilhouetteUp = 4;   // 影絵の解像度倍率 (縁の太さ = pad / この値 テクセル)

        public static Sprite Silhouette(Sprite src, int pad, Color color)
        {
            if (src == null || src.texture == null) return null;
            string key = "sil:" + src.name + ":" + pad + ":" + ColorUtility.ToHtmlStringRGBA(color);
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            Texture2D tex = src.texture;
            Color32[] srcPx;
            try { srcPx = tex.GetPixels32(); }
            catch (Exception) { return null; } // 読めないテクスチャ (Read/Write 無効) は諦める
            int w = tex.width, h = tex.height;
            // 取り込んだ絵は textureRect が透明部分を切り詰めた矩形になる (Tight メッシュ)。表示は rect 全体に合わせるので rect を使う
            var rect = src.rect;
            int rx = Mathf.RoundToInt(rect.x), ry = Mathf.RoundToInt(rect.y), rw = Mathf.RoundToInt(rect.width), rh = Mathf.RoundToInt(rect.height);
            if (rx < 0 || ry < 0 || rx + rw > w || ry + rh > h) return null;
            int up = SilhouetteUp;
            int ow = rw * up + pad * 2, oh = rh * up + pad * 2;
            var outPx = new Color[ow * oh];
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < oh; y++)
                for (int x = 0; x < ow; x++)
                {
                    bool hit = false;
                    for (int dy = -pad; dy <= pad && !hit; dy++)
                        for (int dx = -pad; dx <= pad; dx++)
                        {
                            int ux = x - pad + dx, uy = y - pad + dy;          // 4倍解像度の座標
                            if (ux < 0 || uy < 0 || ux >= rw * up || uy >= rh * up) continue;
                            int sx = ux / up, sy = uy / up;
                            if (srcPx[(ry + sy) * w + (rx + sx)].a > 40) { hit = true; break; }
                        }
                    outPx[y * ow + x] = hit ? color : clear;
                }
            var ot = new Texture2D(ow, oh, TextureFormat.RGBA32, false);
            ot.filterMode = FilterMode.Point;
            ot.wrapMode = TextureWrapMode.Clamp;
            ot.SetPixels(outPx);
            ot.Apply(false, false);
            s = Sprite.Create(ot, new Rect(0, 0, ow, oh), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key;
            _cache[key] = s;
            return s;
        }

        /// <summary>
        /// ドット絵を「紙の切り抜き」として置く: 紙色の縁 (pad テクセル) と、右下に落ちる墨の影。
        /// rt は絵の矩形 (preserveAspect の Image と同じ寸法で置く)。返り値は貼った縁の Image (無ければ null)
        /// </summary>
        public static Image Sticker(RectTransform host, Sprite art, RectTransform rt, int pad = 1)
        {
            var sil = Silhouette(art, pad, Paper);
            if (sil == null) return null;
            // 絵の表示倍率に合わせて縁の矩形を膨らませる (絵は整数倍で描かれる想定)
            float scale = rt.rect.width / art.rect.width;
            float grow = pad * scale / SilhouetteUp;
            var shadow = UiKit.NewRect("sticker-shadow", host);
            shadow.SetSiblingIndex(rt.GetSiblingIndex());
            CopyRect(rt, shadow, grow + 4f, -4f);
            var shImg = shadow.gameObject.AddComponent<Image>();
            shImg.sprite = sil; shImg.preserveAspect = true; shImg.raycastTarget = false;
            shImg.color = new Color(Ink.r, Ink.g, Ink.b, 0.55f);
            var edge = UiKit.NewRect("sticker", host);
            edge.SetSiblingIndex(rt.GetSiblingIndex());
            CopyRect(rt, edge, grow, 0f);
            var img = edge.gameObject.AddComponent<Image>();
            img.sprite = sil; img.preserveAspect = true; img.raycastTarget = false;
            return img;
        }

        static void CopyRect(RectTransform from, RectTransform to, float grow, float dy)
        {
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax; to.pivot = from.pivot;
            to.offsetMin = from.offsetMin + new Vector2(-grow, -grow + dy);
            to.offsetMax = from.offsetMax + new Vector2(grow, grow + dy);
        }

        /// <summary>舞台 (夜) の上に置く短い注記: 夜色の札に紙色の文字。文字の幅に合わせて札を作る (maxWidth を超えたら折り返す)。
        /// 縁取りだけの紙色の文字は草の上で読めなかった (2026-09-09)。呼び出し側で anchor/pivot/anchoredPosition を置く</summary>
        public static RectTransform NightNote(Transform parent, string text, int size, float maxWidth, bool bold = false, string name = "nightnote")
        {
            var rt = UiKit.NewRect(name, parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = Tag; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1f;
            bg.color = new Color(Night.r, Night.g, Night.b, 0.84f);
            bg.raycastTarget = false;
            var t = UiKit.Txt(rt, text, size, Paper, TextAnchor.MiddleCenter, bold);
            UiKit.Stretch(t.rectTransform, 9f, 9f, 3f, 3f);
            var pref = t.GetPreferredValues(text, Mathf.Max(40f, maxWidth - 18f), 0f);
            rt.sizeDelta = new Vector2(Mathf.Min(maxWidth, pref.x + 20f), pref.y + 8f);
            return rt;
        }

        /// <summary>紙の円盤 (墨の縁2px)。エナジーの太陽などに</summary>
        public static Sprite Disc()
        {
            Sprite s;
            if (_cache.TryGetValue("disc", out s)) return s;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[n * n];
            float c = n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = c - Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    Color col = d < 0f ? new Color(0f, 0f, 0f, 0f) : (d < 2f ? Ink : Paper);
                    if (d >= 0f && d < 1f) col.a *= Mathf.Clamp01(d + 0.5f);
                    px[y * n + x] = col;
                }
            tex.SetPixels(px); tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "disc"; _cache["disc"] = s; return s;
        }

        /// <summary>夜の円盤 (Disc の夜の版。2026-09-30 P20): 夜の線 NightEdge の縁2px・夜 (主) NightHi の地。レリックの円・角の数字の丸</summary>
        public static Sprite NightDisc()
        {
            Sprite s;
            if (_cache.TryGetValue("night_disc", out s)) return s;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[n * n];
            float c = n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = c - Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    Color col = d < 0f ? new Color(0f, 0f, 0f, 0f) : (d < 2f ? NightEdge : NightHi);
                    if (d >= 0f && d < 1f) col.a *= Mathf.Clamp01(d + 0.5f);
                    px[y * n + x] = col;
                }
            tex.SetPixels(px); tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "night_disc"; _cache["night_disc"] = s; return s;
        }

        /// <summary>白いリング (太さ thick px、外径 128)。色は Image で乗せ、fillMethod Radial360 で弧にする</summary>
        public static Sprite Ring(int thick = 8)
        {
            string key = "ring" + thick;
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[n * n];
            float c = n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    float outer = c - 1f, inner = c - 1f - thick;
                    float a = Mathf.Clamp01(outer - r + 0.5f) * Mathf.Clamp01(r - inner + 0.5f);
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px); tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key; _cache[key] = s; return s;
        }

        /// <summary>
        /// 輪の目盛り (2026-09-29 p20): Ring(thick) と同じ帯 (外径 128) に、上から右回りに 360/n 度ごとの切れ目 (幅 width px) だけを白で描く。
        /// 色は Image で乗せ、Ring の上に重ねて帯を n 等分する (エナジーの輪で「4つのうち3つ」を数える)。
        /// 切れ目は帯の内側だけ＝輪の外形は変わらない。縁は Ring と同じなめらかさ (UI の四角を回して置くとギザギザになるので絵に焼く)
        /// </summary>
        public static Sprite RingTicks(int thick, int n, float width = 3f)
        {
            n = Math.Max(1, n);
            string key = "ringticks" + thick + ":" + n + ":" + Mathf.RoundToInt(width * 10f);
            Sprite s;
            if (_cache.TryGetValue(key, out s)) return s;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            float c = size / 2f, step = Mathf.PI * 2f / n, half = width / 2f;
            float outer = c - 1f, inner = c - 1f - thick;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float band = Mathf.Clamp01(outer - r + 0.5f) * Mathf.Clamp01(r - inner + 0.5f);
                    float a = 0f;
                    if (band > 0f)
                    {
                        float th = Mathf.Atan2(dx, dy);   // 上が 0・右回り
                        if (th < 0f) th += Mathf.PI * 2f;
                        float m = th % step;
                        float da = Mathf.Min(m, step - m);   // いちばん近い切れ目までの角度
                        float dist = r * Mathf.Sin(Mathf.Min(da, Mathf.PI / 2f));
                        a = band * Mathf.Clamp01(half - dist + 0.5f);
                    }
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px); tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key; _cache[key] = s; return s;
        }

        /// <summary>ドット絵を整数倍で置く倍率 = round(目安の表示幅 / 実寸)。密度はオクトラ相当 (1ドット=4px): 通常 64→256・エリート 80→320・ボス 96→384 がどれも4倍。生成の代役 16 は 16倍</summary>
        public static int PixelScale(Sprite s, float target = 256f)
        {
            if (s == null) return 1;
            float w = s.rect.width;
            return Math.Max(1, Mathf.RoundToInt(target / w));
        }

        /// <summary>絵の倍率。PC は整数倍 (1ドット=4px)、スマホは半分の目安をそのまま (96 ドットの絵が 1倍に落ちて豆粒になるのを避ける。画面の px 自体が 1.31倍なので整数の意味は薄い。2026-09-14)</summary>
        public static float PixelScaleF(Sprite s, float target = 256f)
        {
            if (s == null) return 1f;
            if (UiKit.Phone) return Mathf.Max(0.5f, target / s.rect.width);
            return PixelScale(s, target);
        }

        /// <summary>rt を絵の整数倍の寸法にする (足元 bottom を保ち、中心 cx に置く)</summary>
        public static void FitPixel(RectTransform rt, Sprite s, float cx, float bottom, float target = 256f)
        {
            float k = PixelScaleF(s, target);
            float w = s.rect.width * k, h = s.rect.height * k;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(cx - w / 2f, bottom);
            rt.offsetMax = new Vector2(cx + w / 2f, bottom + h);
        }

        // ---- よく使う組み立て ----

        /// <summary>紙の面 (9スライス・1倍)。tint で紙の色味を変える (白=そのまま)</summary>
        public static Image Sheet(Transform parent, Sprite nine, string name = "paper", Color? tint = null)
        {
            var rt = UiKit.NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = nine;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = tint ?? Color.white;
            return img;
        }

        /// <summary>
        /// 点線のポケット (空き枠。2026-09-29 p17): 親 (w×h) いっぱいの角丸6の破線 (4 描いて 3 空ける・線は PC 1.5／スマホ 2) と、ごく薄い塗り。
        /// 決まった寸法で1枚の絵を作る (9スライスで伸ばすと辺の破線の間隔が崩れるので Simple)。テクスチャは画面の画素に合わせて作る (スマホは 1.31倍)。
        /// 色は line の RGB。線の濃さは line.a、塗りの濃さは fillAlpha (0 で塗りなし)。旧: Tag を灰色に染めた塗り＝押せないボタンに見えた
        /// </summary>
        public static Image DashedPocket(Transform parent, float w, float h, Color line, float fillAlpha, float lineW = -1f)
        {
            if (lineW <= 0f) lineW = UiKit.Phone ? 2f : 1.5f;
            var prt = parent as RectTransform;
            float k = 1f;
            if (prt != null && Screen.width > 0)
            {
                var cs = BattleScreen.CanvasSize(prt);
                if (cs.x > 0f) k = Mathf.Clamp(Screen.width / cs.x, 1f, 4f);
            }
            k = Mathf.Round(k * 20f) / 20f;
            int tw = Mathf.Max(8, Mathf.CeilToInt(w * k)), th = Mathf.Max(8, Mathf.CeilToInt(h * k));
            // 見える色 (夜の札の中なら NightSkin が写す先の色。ポケットはどれも夜の札の中＝自分の札のからくりの区画) と、
            // Linear の時の α の写し (P20 2周目): 絵に焼いた α は Linear では混ぜ方が変わり、スマホの空き枠の塗り (紙色 7%) が夜の上で
            // 輝度 12→73 まで持ち上がって「淡い灰の板」に見えていた (W3 の統合の所見)。UiKit.GammaAlpha で Gamma と同じ見え方の α へ (-uilinearfix 0 なら今のまま)
            Color shown = line;
            Color nightLine;
            if (HD2DFlags.UiNight && TryNightInk(line, out nightLine)) shown = nightLine;
            bool lin = UiKit.LinearFix;
            string key = "pocket:" + tw + "x" + th + ":" + Mathf.RoundToInt(lineW * k * 10f) + ":" + Mathf.RoundToInt(line.a * 100f) + ":" + Mathf.RoundToInt(fillAlpha * 100f)
                + (lin ? ":L" + ColorUtility.ToHtmlStringRGB(shown) : "");
            Sprite s;
            if (!_cache.TryGetValue(key, out s))
            {
                float lw = lineW * k, r = 6f * k, dash = 4f * k, gap = 3f * k;
                float hx = tw / 2f, hy = th / 2f;
                float rc = Mathf.Max(0.5f, r - lw / 2f);                       // 線の芯の角の半径
                float sx = Mathf.Max(0f, hx - r), sy = Mathf.Max(0f, hy - r);   // 直線の半分の長さ
                float wc = 2f * sx, hc = 2f * sy, arc = Mathf.PI / 2f * rc;
                float perim = 2f * wc + 2f * hc + 4f * arc;
                int nDash = Mathf.Max(4, Mathf.RoundToInt(perim / (dash + gap)));
                float period = perim / nDash, on = period * dash / (dash + gap);   // 周が破線の周期で割り切れるように少し伸ばす (継ぎ目で破線が欠けない)
                var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[tw * th];
                for (int y = 0; y < th; y++)
                    for (int x = 0; x < tw; x++)
                    {
                        float cx = x + 0.5f - hx, cy = y + 0.5f - hy;
                        float qx = Math.Abs(cx) - sx, qy = Math.Abs(cy) - sy;
                        float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                        float d = -outside;   // 内側ほど大きい
                        if (d < -0.5f) { px[y * tw + x] = new Color(1f, 1f, 1f, 0f); continue; }
                        float edgeA = Mathf.Clamp01(d + 0.5f);
                        float cov = edgeA * Mathf.Clamp01(lw - d + 0.5f);   // 線の太さぶん (縁は半画素なめらかに)
                        if (cov > 0f)
                        {
                            // 周に沿った位置 s (左上の角の終わりから時計回り)。角は弧の長さで数える
                            float spos;
                            if (qx > 0f && qy > 0f)
                            {
                                float ccx = cx > 0f ? sx : -sx, ccy = cy > 0f ? sy : -sy;
                                float phi = Mathf.Atan2(cy - ccy, cx - ccx);
                                if (cx > 0f && cy > 0f) spos = wc + (Mathf.PI / 2f - phi) * rc;                         // 右上
                                else if (cx > 0f) spos = wc + arc + hc + (-phi) * rc;                                    // 右下
                                else if (cy < 0f) { if (phi > 0f) phi -= 2f * Mathf.PI; spos = 2f * wc + 2f * arc + hc + (-Mathf.PI / 2f - phi) * rc; }   // 左下
                                else spos = 2f * wc + 3f * arc + 2f * hc + (Mathf.PI - phi) * rc;                        // 左上
                            }
                            else if (hy - Math.Abs(cy) < hx - Math.Abs(cx))
                                spos = cy > 0f ? cx + sx : 2f * arc + wc + hc + (sx - cx);                               // 上・下
                            else
                                spos = cx > 0f ? wc + arc + (sy - cy) : 2f * wc + 3f * arc + hc + (cy + sy);             // 右・左
                            float m = spos % period; if (m < 0f) m += period;
                            // 破線の端も半画素なめらかに
                            float dashA = Mathf.Clamp01(Mathf.Min(m + 0.5f, on - m + 0.5f));
                            cov *= dashA;
                        }
                        float la = cov * line.a;
                        float fa = d >= 0f ? fillAlpha * edgeA : 0f;
                        float pa = la + (1f - la) * fa;
                        if (lin) pa = UiKit.GammaAlpha(shown, pa);   // 下は色から既定 (明るい線＝暗い地・墨の線＝紙の地)
                        px[y * tw + x] = new Color(1f, 1f, 1f, pa);
                    }
                tex.SetPixels(px);
                tex.Apply(false, false);
                s = Sprite.Create(tex, new Rect(0, 0, tw, th), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                s.name = key;
                _cache[key] = s;
            }
            var rt = UiKit.NewRect("pocket", parent);
            UiKit.Stretch(rt, 0f, 0f, 0f, 0f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = new Color(shown.r, shown.g, shown.b, 1f);   // 夜の札の中では写した後の色 (NightSkin はもう写さない)
            img.raycastTarget = false;
            return img;
        }

        /// <summary>紙の粒を重ねる (親いっぱい)</summary>
        public static Image GrainOver(Transform parent, float alpha = 1f)
        {
            var rt = UiKit.NewRect("grain", parent);
            UiKit.Stretch(rt, 0f, 0f, 0f, 0f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Grain();
            img.type = Image.Type.Tiled;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = new Color(1f, 1f, 1f, alpha);
            img.raycastTarget = false;
            return img;
        }

        /// <summary>水彩のにじみ (Image)。w×h に伸ばす</summary>
        public static Image BlobImage(Transform parent, Color color, string name = "blob")
        {
            var rt = UiKit.NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Blob(color);
            img.preserveAspect = false;
            img.raycastTarget = false;
            return img;
        }

        // ---- 夜の札への写し (HD-2D 見本 ui=night。2026-09-30 P20) ----

        /// <summary>墨 → 夜の上の文字 (紙の表の1対1。文字の色・墨1色の絵・点線のポケット・輪・リッチテキストの色タグに使う)</summary>
        static readonly Color[,] NightInkPairs =
        {
            { Ink, NightInk }, { InkSoft, NightInkSoft }, { BrassInk, NightBrassInk }, { ManaInk, NightManaInk },
            { BadInk, NightBadInk }, { BadDown, NightBadInk }, { GoodInk, NightGoodInk }, { SkyInk, NightSkyInk }, { PlumInk, NightPlumInk },
        };
        /// <summary>淡い塗り → 夜の暗い塗り (紙の札を染めた色・平らな塗り)。紙そのもの (Paper・Paper2・Paper3) は小さな札なら NightPill・平らな塗りなら NightTrack</summary>
        static readonly Color[,] NightFillPairs =
        {
            { BrassLight, NightBrass }, { ManaLight, NightMana }, { RoseLight, NightRose }, { DangerBtn, NightRose },
            { SkyLight, NightSky }, { MossLight, NightMoss }, { PlumLight, NightPlum },
        };
        static Dictionary<string, string> _nightTags;

        /// <summary>夜の札の中でも写さない「絵」の枝の名前 (P20 2周目)。pile-glyph = 山札・捨て札の札の小さな札の絵 (表向きの紙の札・裏向きの夜＋真鍮の札。
        /// 写すと表向きの紙の札まで夜になり、裏向きと見分けられない)</summary>
        static readonly string[] KeepPaperNames = { "pile-glyph" };

        /// <summary>
        /// 舞台の上に常に出る札 (root の下) を夜の組へ写す。旗 ui=night の時だけ (立っていなければ何もしない＝今の画は1画素も変わらない)。組み立ての後に呼ぶ。
        /// 写し方 (紙の表の1対1): 紙の札 Tag→NightCard・Tag2→NightCard2・細い縁の札 TagThin と淡い色で染めた札→NightTint (同じ色相の暗い塗り)・
        /// 紙のボタン→NightButton・紙の円→NightDisc・吹き出しの尾→NightBubbleTail／墨の文字・墨1色の絵・点線のポケット・輪→紙色 (各色の墨はそれぞれの淡い版。リッチテキストの色タグも)／
        /// 紙の平らな塗り (HP バーの空き)→NightTrack・墨の細い線 (区切り・刻み)→NightEdge。
        /// 写さないもの: 名前が "edge" の縁 (狙い・行動中の真鍮の光・トークンのレア度の外線)・真鍮や薔薇などの濃い色で染めた札・墨の太い帯
        /// (ギアの名前の帯・挿絵の額＝上の紙色の文字ごと残す)・色つきの絵 (白で描く絵)・HP バーの数字 (紙の下敷きつき。UiKit.NumHaloNight が有ればそれに替えて紙色へ)。
        /// 後から足される子 (順送りの盾・書き換わる数字) は root に付けた NightSkin が毎フレーム写す (写した後の色は紙の表に無いので、2度写しても変わらない)
        /// </summary>
        public static void Nightify(Transform root)
        {
            if (root == null || !HD2DFlags.UiNight) return;
            var skin = root.GetComponent<NightSkin>();
            if (skin == null) skin = root.gameObject.AddComponent<NightSkin>();
            skin.Apply();
        }

        /// <summary>夜の札の皮 (Nightify が付ける)。LateUpdate で root の下を写す＝演出が後から足した札・書き換えた文字も同じフレームのうちに夜になる</summary>
        public class NightSkin : MonoBehaviour
        {
            static readonly List<Graphic> _buf = new List<Graphic>();
            readonly Dictionary<TMP_Text, string> _textSeen = new Dictionary<TMP_Text, string>();   // 文字の札 → 最後に見た文字列 (同じ参照なら色タグを見直さない)

            void LateUpdate() { if (HD2DFlags.UiNight) Apply(); }

            public void Apply()
            {
                _buf.Clear();
                GetComponentsInChildren(true, _buf);
                for (int i = 0; i < _buf.Count; i++)
                {
                    var gr = _buf[i];
                    if (gr == null) continue;
                    if (UnderKeepPaper(gr.transform)) continue;   // 札の中の「絵」(山札・捨て札の小さな札の絵) は紙のまま (P20 2周目)
                    var tmp = gr as TMP_Text;
                    if (tmp != null) { SkinText(tmp); continue; }
                    var img = gr as Image;
                    if (img != null) SkinImage(img);
                }
                _buf.Clear();
            }

            /// <summary>札の中に描いた「絵」の枝 (KeepPaperNames の名前の物の下) か。皮の根 (この部品の付いた物) までさかのぼる</summary>
            bool UnderKeepPaper(Transform t)
            {
                for (var p = t; p != null && p != transform; p = p.parent)
                    for (int k = 0; k < KeepPaperNames.Length; k++)
                        if (p.name == KeepPaperNames[k]) return true;
                return false;
            }

            void SkinText(TMP_Text t)
            {
                // HP バーの数字 (紙の下敷き NumHalo): 夜の下敷き (P21 の NumHaloNight) が有る時だけ紙色へ。無ければ墨＋紙の下敷きのまま (薔薇の塗りの上で読める)
                var halo = UiKit.NumHalo;
                if (halo != null && t.fontSharedMaterial == halo)
                {
                    var nm = UiKit.NumHaloNight(t.font);
                    if (nm == null) return;
                    t.fontSharedMaterial = nm;
                    t.color = WithA(NightInk, t.color.a);
                    return;
                }
                Color n;
                // Linear (W3 P21 の申し送り3): 墨で作った字を紙色へ塗り替えたら、字の素材も明るい字の版へ (墨の補正の素材のままだと縁1本 0.4 傾き幅ぶん太る)
                if (TryNightInk(t.color, out n)) { t.color = n; UiKit.FitTextToColor(t); }
                // リッチテキストの色タグ (UiKit.ColorTag が書く "<color=#RRGGBB>")。同じ文字列は2度見ない
                string s = t.text, seen;
                if (_textSeen.TryGetValue(t, out seen) && ReferenceEquals(seen, s)) return;
                if (!string.IsNullOrEmpty(s) && s.IndexOf("<color=#", StringComparison.Ordinal) >= 0)
                {
                    string r = s;
                    foreach (var kv in NightTagTable()) if (r.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0) r = ReplaceIgnoreCase(r, kv.Key, kv.Value);
                    if (r != s) { t.text = r; s = t.text; }
                }
                _textSeen[t] = s;
            }

            static void SkinImage(Image img)
            {
                if (img.gameObject.name == "edge") return;   // 狙い・行動中の真鍮の光とレア度の外線は紙の時の色のまま
                var sp = img.sprite;
                var c = img.color;
                Color f;
                if (sp == null)
                {   // 平らな塗り: 紙 → HP バーの空き・淡い塗り → 暗い塗り・墨の細い線 → 夜の線 (太い墨の帯は上の紙色の文字ごと残す)
                    if (TryNightFill(c, true, out f)) img.color = WithA(f, c.a);
                    else if ((Near(c, Ink) || Near(c, InkSoft)) && Thin(img.rectTransform)) img.color = WithA(NightEdge, c.a);
                    return;
                }
                bool tag = sp == Tag, tag2 = sp == Tag2, thin = sp == TagThin, btn = sp == Button;
                if (tag || tag2 || thin || btn)
                {
                    if (TryNightFill(c, false, out f)) { img.sprite = NightTint; img.color = WithA(NightTintOf(f), c.a); }
                    else if (Whiteish(c))
                    {
                        if (thin) { img.sprite = NightTint; var p = NightTintOf(NightPill); img.color = new Color(p.r * c.r, p.g * c.g, p.b * c.b, c.a); }
                        else img.sprite = tag ? NightCard : tag2 ? NightCard2 : NightButton;   // 灰の掛け算 (倒れた・組めない) はそのまま
                    }
                    return;
                }
                if (sp == Disc()) { if (Whiteish(c)) img.sprite = NightDisc(); return; }
                if (sp == BubbleTail())
                {   // 尾: 紙 (濃) の札の尾は Paper2/Paper の比で染めてある (IntentTag) → 夜 (情報) の札の尾は Night/NightHi の比
                    img.sprite = NightBubbleTail();
                    if (!(c.r > 0.995f && c.g > 0.995f && c.b > 0.995f)) img.color = new Color(Night.r / NightHi.r, Night.g / NightHi.g, Night.b / NightHi.b, c.a);
                    return;
                }
                Color n;
                if (TryNightInk(c, out n)) img.color = n;   // 墨1色の絵・点線のポケット・輪
            }
        }

        static bool Near(Color a, Color b) { return Mathf.Abs(a.r - b.r) < 0.006f && Mathf.Abs(a.g - b.g) < 0.006f && Mathf.Abs(a.b - b.b) < 0.006f; }
        static Color WithA(Color c, float a) { return new Color(c.r, c.g, c.b, a); }
        /// <summary>灰〜白 (掛け算の沈め: 倒れた帳面 0.8・組めないギア DimTint・紙 (濃) の比)。淡い色の塗りは先に NightFillPairs で拾う</summary>
        static bool Whiteish(Color c)
        {
            float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return mn >= 0.75f && mx - mn <= 0.1f;
        }
        /// <summary>細い線 (区切り・刻み)。大きさがまだ決まっていない (0×0) 時は写さない＝次のフレームに見直す</summary>
        static bool Thin(RectTransform rt)
        {
            if (rt == null) return false;
            var r = rt.rect;
            return r.width > 0f && r.height > 0f && Mathf.Min(r.width, r.height) <= 3.5f;
        }
        /// <summary>NightTint を c の塗りにする Image.color (地が NightTintFill の灰なので割り戻す。縁は1段明るい同じ色相)</summary>
        public static Color NightTintOf(Color fill)
        {
            return new Color(Mathf.Min(1f, fill.r / NightTintFill), Mathf.Min(1f, fill.g / NightTintFill), Mathf.Min(1f, fill.b / NightTintFill), 1f);
        }
        /// <summary>墨 (紙の表) → 夜の上の文字。透明度はそのまま。紙の表に無い色は false</summary>
        public static bool TryNightInk(Color c, out Color night)
        {
            for (int i = 0; i < NightInkPairs.GetLength(0); i++)
                if (Near(c, NightInkPairs[i, 0])) { night = WithA(NightInkPairs[i, 1], c.a); return true; }
            night = c; return false;
        }
        /// <summary>淡い塗り (紙の表) → 夜の暗い塗り。flat=平らな塗り (紙そのものは NightTrack)・それ以外は札の地 (紙そのものは NightPill)</summary>
        public static bool TryNightFill(Color c, bool flat, out Color night)
        {
            if (Near(c, Paper) || Near(c, Paper2) || Near(c, Paper3)) { night = flat ? NightTrack : NightPill; return true; }
            for (int i = 0; i < NightFillPairs.GetLength(0); i++)
                if (Near(c, NightFillPairs[i, 0])) { night = NightFillPairs[i, 1]; return true; }
            night = c; return false;
        }
        /// <summary>リッチテキストの色タグの写し ("&lt;color=#9C3A2A" → "&lt;color=#F2A193")。UiKit.ColorTag と同じ書式 (大文字の6桁)</summary>
        static Dictionary<string, string> NightTagTable()
        {
            if (_nightTags != null) return _nightTags;
            var d = new Dictionary<string, string>();
            for (int i = 0; i < NightInkPairs.GetLength(0); i++)
            {
                string k = "<color=#" + ColorUtility.ToHtmlStringRGB(NightInkPairs[i, 0]);
                if (!d.ContainsKey(k)) d[k] = "<color=#" + ColorUtility.ToHtmlStringRGB(NightInkPairs[i, 1]);
            }
            _nightTags = d;
            return d;
        }
        static string ReplaceIgnoreCase(string s, string from, string to)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            int i = 0;
            while (true)
            {
                int j = s.IndexOf(from, i, StringComparison.OrdinalIgnoreCase);
                if (j < 0) { sb.Append(s, i, s.Length - i); break; }
                sb.Append(s, i, j - i).Append(to);
                i = j + from.Length;
            }
            return sb.ToString();
        }
    }
}
