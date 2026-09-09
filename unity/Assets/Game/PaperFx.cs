// PaperFx.cs — 「絵本」の肌 (2026-09-07 デザインカンバス第5版で決定) の生成部品。
// クリーム色の紙の9スライス (鉛筆の二重線)・水彩のにじみ・紙の粒・貼り絵の縁 (ドット絵の切り抜き)・タイプのしおり・マスキングテープ。
// 規約「絵はドット、紙と文字はなめらか」: ここで作るのは紙と線 (なめらか側)。ドット絵は Point フィルタで整数倍に置く。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class PaperFx
    {
        public static readonly Color Paper = UiKit.Hex("#f4ecd6");
        public static readonly Color Paper2 = UiKit.Hex("#eadfc4");
        public static readonly Color Ink = UiKit.Hex("#3b2f2f");
        public static readonly Color InkSoft = UiKit.Hex("#574b48");   // 中墨 (紙の上で 7:1)。透明度で薄めない
        /// <summary>状態異常の文字 (藤色の紙の上で 6.7:1。Plum そのものは 1.9:1 で読めない)</summary>
        public static readonly Color PlumInk = UiKit.Hex("#5a3d78");
        /// <summary>紙の上の金の文字 (予測行・注意書き)</summary>
        public static readonly Color GoldInk = UiKit.Hex("#7a4e12");
        public static readonly Color Honey = UiKit.Hex("#e0b25a");
        public static readonly Color Rose = UiKit.Hex("#d97b7b");
        public static readonly Color Sky = UiKit.Hex("#7fa7c9");
        public static readonly Color Moss = UiKit.Hex("#8fae7b");
        public static readonly Color Plum = UiKit.Hex("#a98cc4");
        public static readonly Color Teal = UiKit.Hex("#7ab8b0");
        public static readonly Color Sand = UiKit.Hex("#c9a982");
        public static readonly Color Night = UiKit.Hex("#1a1c33");

        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>タイプの色 (しおり)。物理=砂・呪文=藤・リアクション=青緑・置物=蜂蜜</summary>
        public static Color TypeColor(string type)
        {
            switch (type)
            {
                case "spell": return Plum;
                case "reaction": return Teal;
                case "permanent": return Honey;
                default: return Sand;
            }
        }

        /// <summary>役割の色 (しるし・にじみ)。dmg=薔薇・block=空・counter=青緑・growth=苔・momentum=蜂蜜</summary>
        public static Color RoleColor(string role)
        {
            switch (role)
            {
                case "block": return Sky;
                case "counter": return Teal;
                case "growth": return Moss;
                case "momentum": return Honey;
                case "expose": return UiKit.Hex("#e0a04a");
                default: return Rose;
            }
        }

        // ---- 9スライス (角丸の紙。1テクセル=1px で線を細く保つ。UiKit.Frame は名前が paper で始まる絵を1倍で貼る) ----

        /// <summary>紙のパネル: 外から 淡い線1・紙3・墨2・紙。角丸 12</summary>
        public static Sprite Panel { get { return Nine("paper_panel", 48, 12, 14, PanelBands, false); } }
        /// <summary>紙の札 (小さな帯): 墨2・紙。角丸 8</summary>
        public static Sprite Tag { get { return Nine("paper_tag", 32, 8, 10, TagBands, false); } }
        /// <summary>紙のボタン: 墨2・紙、下に厚み (墨 50%) 4px。角丸 10</summary>
        public static Sprite Button { get { return Nine("paper_button", 40, 10, 12, TagBands, true); } }
        /// <summary>カードの面: パネルと同じ二重線。角丸 14</summary>
        public static Sprite Card { get { return Nine("paper_card", 52, 14, 16, PanelBands, false); } }
        /// <summary>カードの面・案B (2026-09-09): 外側の線の色がレア度 (C 墨50%・U 空・R 蜂蜜=2px)。差し替えは Art/ui/paper_card_<rarity>.png</summary>
        public static Sprite CardOf(string rarity)
        {
            string r = rarity ?? "common";
            Color line = r == "rare" ? Honey : r == "uncommon" ? Sky : new Color(Ink.r, Ink.g, Ink.b, 0.5f);
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

        /// <summary>マスキングテープ (半透明の蜂蜜色。左右は破いた縁)</summary>
        public static Sprite Tape()
        {
            Sprite s;
            if (_cache.TryGetValue("tape", out s)) return s;
            const int w = 96, h = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[w * h];
            var rng = new System.Random(7);
            var col = new Color(1f, 0.886f, 0.55f, 0.74f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool tornL = x < 3 && rng.NextDouble() < 0.45, tornR = x > w - 4 && rng.NextDouble() < 0.45;
                    px[y * w + x] = (tornL || tornR) ? new Color(0f, 0f, 0f, 0f) : col;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "tape";
            _cache["tape"] = s;
            return s;
        }

        /// <summary>吹き出しの尾 (下向きの小さな三角。紙色に墨の線)</summary>
        public static Sprite BubbleTail()
        {
            Sprite s;
            if (_cache.TryGetValue("tail", out s)) return s;
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
                    px[y * w + x] = !inside ? new Color(0f, 0f, 0f, 0f) : (edge ? Ink : Paper);
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "tail";
            _cache["tail"] = s;
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

        /// <summary>ドット絵を整数倍で置く倍率 = round(目安の表示幅 / 実寸)。密度はオクトラ相当 (1ドット=4px): 通常 64→256・エリート 80→320・ボス 96→384 がどれも4倍。生成の代役 16 は 16倍</summary>
        public static int PixelScale(Sprite s, float target = 256f)
        {
            if (s == null) return 1;
            float w = s.rect.width;
            return Math.Max(1, Mathf.RoundToInt(target / w));
        }

        /// <summary>rt を絵の整数倍の寸法にする (足元 bottom を保ち、中心 cx に置く)</summary>
        public static void FitPixel(RectTransform rt, Sprite s, float cx, float bottom, float target = 256f)
        {
            int k = PixelScale(s, target);
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
    }
}
