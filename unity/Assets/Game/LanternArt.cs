// LanternArt.cs — 灯籠の絵をコードで描く (2026-11-04 ユーザー「灯籠のイラストや UI も大手ゲームデベロッパー並みの UI にしてほしい」)。
// エナジーの宝珠 (EnergyOrb) と同じ描き方: なめらかな真鍮の金具 (左上から光・縁の陰・鋲)・硝子の奥の暖かい光・3 層の炎・硝子の照り返し。
// 旧は 32×48 ドットの灯籠 (ThemeFx.Lantern)。外形と硝子の窓の位置は同じ比率 (32:48・窓は x 7〜25・上から y 14〜36) なので LightUi の配置と粒の的は変わらない。
// 色は PaperFx の名前と ThemeFx の灯の光から (16 進は書かない)。
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game
{
    public static class LanternArt
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
        /// <summary>金具の絵の画素 (外形 32:48 の 5 倍)。硝子の窓は x 35〜125・y (下から) 60〜170</summary>
        public const int W = 160, H = 240;
        public const int GX0 = 35, GX1 = 125, GY0 = 60, GY1 = 170;

        static Sprite Done(string key, Texture2D tex, int w, int h)
        {
            tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = "lantern_" + key; _cache[key] = s; return s;
        }

        static float Step(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }

        /// <summary>真鍮の金具の色: u は部品の横の位置 (−1 左〜1 右)。左寄りが明るく、細い照りが入る。v は縦の明るさの足し (帯の上の縁など)</summary>
        static Color Brass(float u, float lift)
        {
            float b = Mathf.Clamp01(0.30f + 0.70f * (1f - Mathf.Abs(u + 0.30f) * 0.95f)) + lift;
            Color c = Color.Lerp(PaperFx.BrassInk, PaperFx.Brass, Mathf.Clamp01(b));
            float spec = Mathf.Exp(-Mathf.Pow((u + 0.48f) / 0.13f, 2f));
            c = Color.Lerp(c, PaperFx.BrassLight, Mathf.Clamp01(spec * 0.85f + Mathf.Max(0f, lift) * 0.5f));
            c.a = 1f;
            return c;
        }

        /// <summary>金具 (硝子の窓は透明)。底板・台座・下の帯・柱・上の帯・屋根 (反りのある笠)・宝珠の飾り・吊り輪。縁は 1 画素の陰で締める</summary>
        public static Sprite Frame()
        {
            Sprite s;
            if (_cache.TryGetValue("frame", out s) && s != null) return s;
            var px = new Color[W * H];
            const float cx = W / 2f;
            System.Action<int, int, Color> put = (x, y, c) => { if (x >= 0 && x < W && y >= 0 && y < H) px[y * W + x] = c; };
            // 横に伸びる部品: y0〜y1 で左右の半幅を halfW(t) で決める (t = 0 下〜1 上)。lift は上の縁を明るく・下の縁を暗く
            System.Action<int, int, System.Func<float, float>> band = (y0, y1, halfW) =>
            {
                for (int y = y0; y < y1; y++)
                {
                    float t = (y - y0 + 0.5f) / Mathf.Max(1, y1 - y0);
                    float hw = halfW(t);
                    for (int x = 0; x < W; x++)
                    {
                        float dx = x + 0.5f - cx;
                        if (Mathf.Abs(dx) > hw) continue;
                        float lift = (y == y1 - 1 ? 0.35f : 0f) + (y == y0 ? -0.35f : 0f) + 0.10f * (t - 0.5f);
                        put(x, y, Brass(dx / Mathf.Max(1f, hw), lift));
                    }
                }
            };
            // 底板 (足つき)・台座 (下が広い)・繰形の線・下の帯
            band(0, 10, t => 64f);
            band(10, 30, t => Mathf.Lerp(62f, 55f, t));
            band(30, 34, t => 58f);
            band(34, 48, t => Mathf.Lerp(55f, 53f, t));
            band(48, 60, t => 60f);
            // 柱 (左右。円柱の陰) — 硝子の窓の左右
            for (int y = 60; y < 170; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = x + 0.5f - cx;
                    float ax = Mathf.Abs(dx);
                    if (ax < (GX1 - cx) || ax > (GX1 - cx) + 13f) continue;
                    float uu = ((ax - (GX1 - cx)) / 13f) * 2f - 1f;   // 柱の中の横の位置
                    if (dx < 0f) uu = -uu;
                    put(x, y, Brass(uu, 0f));
                }
            // 窓の中の細い桟 (側面の硝子との境・正面の窓は数字の邪魔をしない)
            for (int y = 60; y < 170; y++)
                foreach (int bx in new[] { 52, 53, 106, 107 })
                    put(bx, y, Brass(bx < cx ? -0.2f : 0.3f, -0.1f));
            // 上の帯・軒 (少し張り出す)
            band(170, 182, t => 60f);
            band(182, 188, t => 66f);
            // 屋根: 反りのある笠 (下が広く上で細る)
            band(188, 216, t => 14f + 50f * Mathf.Pow(1f - t, 1.35f));
            // 頂の宝珠
            for (int y = 214; y < 228; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - 221f;
                    if (dx * dx + dy * dy > 7.5f * 7.5f) continue;
                    put(x, y, Brass(dx / 7.5f, dy > 2f ? 0.25f : 0f));
                }
            // 吊り輪 (上半分の輪)
            for (int y = 224; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - 226f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r < 9f || r > 13.5f || dy < 0f) continue;
                    put(x, y, Brass(dx / 13.5f, 0.1f));
                }
            // 鋲 (帯の上の明るい点): 下の帯と上の帯
            foreach (int ry in new[] { 54, 176 })
                for (int k = -3; k <= 3; k++)
                {
                    int rx = Mathf.RoundToInt(cx + k * 14f);
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        if (dx * dx + dy * dy <= 1) put(rx + dx, ry + dy, dx == -1 || dy == 1 ? PaperFx.BrassLight : PaperFx.Brass);
                }
            // 縁の陰 (不透明の画素で、隣が透明なら暗く) — 形を締める
            var outPx = (Color[])px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var c = px[y * W + x];
                    if (c.a <= 0f) continue;
                    bool edge = false;
                    for (int k = 0; k < 4 && !edge; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx < 0 || nx >= W || ny < 0 || ny >= H || px[ny * W + nx].a <= 0f) edge = true;
                    }
                    if (edge) { var d = Color.Lerp(c, PaperFx.Night, 0.55f); d.a = 1f; outPx[y * W + x] = d; }
                }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels(outPx);
            return Done("frame", tex, W, H);
        }

        /// <summary>硝子の奥 (窓の大きさ 90×110)。lit = 灯っている: 下の芯から明るい真鍮・熾・縁は夜へ沈む。消灯: 夜の硝子 (冷たい)</summary>
        public static Sprite GlassBack(bool lit)
        {
            string key = lit ? "glass_lit" : "glass_unlit";
            Sprite s;
            if (_cache.TryGetValue(key, out s) && s != null) return s;
            const int gw = GX1 - GX0, gh = GY1 - GY0;
            var px = new Color[gw * gh];
            for (int y = 0; y < gh; y++)
                for (int x = 0; x < gw; x++)
                {
                    float u = (x + 0.5f) / gw * 2f - 1f, v = (y + 0.5f) / gh;
                    Color c;
                    if (lit)
                    {
                        float dx = u, dy = (v - 0.28f) * 1.25f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        c = Color.Lerp(PaperFx.BrassLight, PaperFx.Brass, Step(0.05f, 0.55f, r));
                        c = Color.Lerp(c, PaperFx.Ember, Step(0.35f, 0.9f, r) * 0.55f);
                        c = Color.Lerp(c, PaperFx.BrassInk, Step(0.7f, 1.35f, r));
                    }
                    else
                    {
                        c = Color.Lerp(PaperFx.NightHi, PaperFx.Night, v);
                        c = Color.Lerp(c, PaperFx.BrassInk, (1f - v) * 0.25f);   // 底に冷えた真鍮の照り
                    }
                    float side = Mathf.Abs(u);   // 側面の硝子 (斜めに見える) は少し暗い
                    if (side > 0.62f) c = Color.Lerp(c, PaperFx.Night, 0.35f);
                    c.a = 1f;
                    px[y * gw + x] = c;
                }
            var tex = new Texture2D(gw, gh, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            return Done(key, tex, gw, gh);
        }

        /// <summary>炎の形 (しずく形)。layer 0 外 (熾→真鍮・柔らかい縁)・1 中 (明るい真鍮)・2 芯 (灯の白)</summary>
        public static Sprite Flame(int layer)
        {
            string key = "flame" + layer;
            Sprite s;
            if (_cache.TryGetValue(key, out s) && s != null) return s;
            const int fw = 64, fh = 96;
            var px = new Color[fw * fh];
            float soft = layer == 0 ? 0.32f : layer == 1 ? 0.22f : 0.16f;
            for (int y = 0; y < fh; y++)
                for (int x = 0; x < fw; x++)
                {
                    float u = (x + 0.5f) / fw * 2f - 1f, v = (y + 0.5f) / fh;
                    // 半幅: 下が丸く、上へ尖る
                    float hw = Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(v), 0.62f)) * (1f - 0.38f * v) * 0.92f;
                    if (v < 0.12f) hw *= Mathf.Sqrt(v / 0.12f);
                    float d = hw > 1e-4f ? Mathf.Abs(u) / hw : 2f;   // 0 芯〜1 縁
                    float a = 1f - Step(1f - soft, 1f, d);
                    if (a <= 0f) { px[y * fw + x] = new Color(0f, 0f, 0f, 0f); continue; }
                    Color c;
                    if (layer == 0) c = Color.Lerp(PaperFx.Brass, PaperFx.Ember, Step(0.2f, 1f, Mathf.Max(d, v)));
                    else if (layer == 1) c = Color.Lerp(ThemeFx.LampCore, PaperFx.BrassLight, Step(0.1f, 0.9f, Mathf.Max(d, v * 0.8f)));
                    else c = ThemeFx.LampCore;
                    c.a = a * (layer == 0 ? 0.92f : 1f) * (layer == 2 ? 1f - Step(0.55f, 1f, v) : 1f);
                    px[y * fw + x] = c;
                }
            var tex = new Texture2D(fw, fh, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            return Done(key, tex, fw, fh);
        }

        /// <summary>なめらかな光 (暈・足元の光溜まり・火の粉)。白で描き、色は Image で乗せる。旧 ThemeFx.Glow は段々の輪に見えた</summary>
        public static Sprite Glow()
        {
            Sprite s;
            if (_cache.TryGetValue("glow", out s) && s != null) return s;
            const int n = 128;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a * (3f - 2f * a);   // なめらかに消える
                    px[y * n + x] = new Color(1f, 1f, 1f, a * a);
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            return Done("glow", tex, n, n);
        }

        /// <summary>硝子の照り返し (窓の上に重ねる): 左上から斜めの白い筋 2 本と、上の縁の内側の陰</summary>
        public static Sprite Sheen()
        {
            Sprite s;
            if (_cache.TryGetValue("sheen", out s) && s != null) return s;
            const int gw = GX1 - GX0, gh = GY1 - GY0;
            var px = new Color[gw * gh];
            for (int y = 0; y < gh; y++)
                for (int x = 0; x < gw; x++)
                {
                    float u = (x + 0.5f) / gw, v = (y + 0.5f) / gh;
                    float diag = u + (1f - v) * 0.55f;   // 左上ほど小さい
                    float a = Mathf.Exp(-Mathf.Pow((diag - 0.30f) / 0.045f, 2f)) * 0.22f + Mathf.Exp(-Mathf.Pow((diag - 0.42f) / 0.02f, 2f)) * 0.14f;
                    a *= Step(0.25f, 0.65f, v);   // 下半分には照りを入れない (炎と数字を邪魔しない)
                    Color c = Color.white;
                    float top = 1f - Step(0.86f, 1f, v);   // 上の縁の内側の陰
                    if (top < 1f) { c = Color.Lerp(Color.white, PaperFx.Night, 1f - top); a = Mathf.Max(a, (1f - top) * 0.45f); }
                    c.a = a;
                    px[y * gw + x] = c;
                }
            var tex = new Texture2D(gw, gh, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            return Done("sheen", tex, gw, gh);
        }
    }
}
