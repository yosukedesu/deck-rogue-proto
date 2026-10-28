// MapForest.cs — 地図の「夜の森の絵地図」の材料 (2026-10-28 品質スプリント 案1。カンバス https://claude.ai/artifact/C8WiiTPfMSPD4jPFE2pi92 の Main)。
// 戦闘の森 (幕1 の箱庭) の地面・道・木・岩の絵 (Resources/Art/stage/act1・読み取り可) を、見下ろしの絵地図に組み直すための部品:
//   ・夜の色に焼いた Point の複製 (CSS の filter = brightness → saturate → hue-rotate と同じ式。カンバスの値をそのまま)
//   ・やわらかい丸・楕円の輪・縦横の帯・周辺減光の手続きの絵
//   ・踏み跡の帯を描く Graphic (MapTrail。土のタイルを画面の座標で貼る＝カンバスの pattern userSpaceOnUse と同じ)
//   ・蛍の明滅と進める節の上下 (MapLife)
// 絵が読めない (Resources に無い・読み取り不可) 時は null を返し、MapScreen は無地の夜で組む。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class MapForest
    {
        static readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> _spr = new Dictionary<string, Sprite>();

        /// <summary>幕ごとの絵の置き場 (Resources/Art/stage/act<N>)。幕2/3 は地面のタイルだけ (木は幕1 の森の物)</summary>
        public static string Folder(int act) { return "Art/stage/act" + Mathf.Clamp(act, 1, 3); }

        /// <summary>CSS の filter: brightness(b) saturate(s) hue-rotate(h) を sRGB の画素に。mul は最後に掛ける色 (夜の青の乗算)</summary>
        static Color32 Grade(Color32 c, float b, float s, float h, Vector3 mul)
        {
            float r = c.r / 255f * b, g = c.g / 255f * b, bl = c.b / 255f * b;
            float r2 = (0.213f + 0.787f * s) * r + (0.715f - 0.715f * s) * g + (0.072f - 0.072f * s) * bl;
            float g2 = (0.213f - 0.213f * s) * r + (0.715f + 0.285f * s) * g + (0.072f - 0.072f * s) * bl;
            float b2 = (0.213f - 0.213f * s) * r + (0.715f - 0.715f * s) * g + (0.072f + 0.928f * s) * bl;
            float a = h * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            float r3 = (0.213f + cs * 0.787f - sn * 0.213f) * r2 + (0.715f - cs * 0.715f - sn * 0.715f) * g2 + (0.072f - cs * 0.072f + sn * 0.928f) * b2;
            float g3 = (0.213f - cs * 0.213f + sn * 0.143f) * r2 + (0.715f + cs * 0.285f + sn * 0.140f) * g2 + (0.072f - cs * 0.072f - sn * 0.283f) * b2;
            float b3 = (0.213f - cs * 0.213f - sn * 0.787f) * r2 + (0.715f - cs * 0.715f + sn * 0.715f) * g2 + (0.072f + cs * 0.928f + sn * 0.072f) * b2;
            return new Color32(B(r3 * mul.x), B(g3 * mul.y), B(b3 * mul.z), c.a);
        }
        static byte B(float v) { return (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255); }

        static Texture2D Readable(string path)
        {
            var t = Resources.Load<Texture2D>(path);
            if (t == null || !t.isReadable) return null;
            return t;
        }

        static Texture2D NewTex(int w, int h, bool repeat, bool point)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = point ? FilterMode.Point : FilterMode.Bilinear,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
            };
        }

        // 夜の青の乗算 (カンバスの grade の真ん中 = rgba(26,34,72,.42) の multiply を焼いた値)
        static readonly Vector3 NightMul = new Vector3(0.623f, 0.636f, 0.699f);

        /// <summary>
        /// 地面: 草のタイル 8 種 (top_grass_a〜d・top_grass_alt_a〜d) を 8×8 枚の 512 ドットの塊に混ぜ、夜の色に焼く (Point・繰り返し)。
        /// 1ドット＝2px で敷く (=1024px ごとに同じ塊。並びは種で決まる)。幕2/3 は top_ で始まるタイル (無ければ幕1 の草)
        /// </summary>
        public static Texture2D Ground(int act)
        {
            string key = "ground" + act;
            Texture2D t;
            if (_tex.TryGetValue(key, out t)) return t;
            var tiles = new List<Texture2D>();
            string[] names = act <= 1
                ? new[] { "top_grass_a", "top_grass_b", "top_grass_c", "top_grass_d", "top_grass_alt_a", "top_grass_alt_b", "top_grass_alt_c", "top_grass_alt_d" }
                : new[] { "top_floor_a", "top_floor_b", "top_floor_c", "top_floor_d", "top_floor_seat_a", "top_floor_seat_b", "top_floor_seat_c", "top_floor_seat_d", "top_earth_a", "top_earth_b", "top_path_a", "top_path_b" };
            foreach (var n in names) { var tx = Readable(Folder(act) + "/tiles/" + n); if (tx != null && tx.width == 64 && tx.height == 64) tiles.Add(tx); }
            if (tiles.Count == 0 && act > 1) { _tex[key] = Ground(1); return _tex[key]; }
            if (tiles.Count == 0) { _tex[key] = null; return null; }
            const int N = 8, S = 64;
            t = NewTex(N * S, N * S, true, true);
            var px = new Color32[N * S * N * S];
            var rnd = new System.Random(20261028 + act);
            for (int ty = 0; ty < N; ty++)
                for (int tx = 0; tx < N; tx++)
                {
                    // 草 (a〜d) を 7 割・別の草 (alt) を 3 割。alt は塊で出る (隣が alt なら続きやすい)
                    int pick = rnd.Next(tiles.Count);
                    var src = tiles[pick].GetPixels32();
                    for (int y = 0; y < S; y++)
                        for (int x = 0; x < S; x++)
                            px[(ty * S + y) * N * S + tx * S + x] = Grade(src[y * S + x], 0.66f, 0.55f, 16f, NightMul);
                }
            t.SetPixels32(px); t.Apply(false, true);
            _tex[key] = t;
            return t;
        }

        /// <summary>踏み跡の土 (top_path_a〜d を 2×2 に並べた 128 ドット・焼かない＝上に夜の帯を重ねる)</summary>
        public static Texture2D Dirt(int act)
        {
            string key = "dirt" + act;
            Texture2D t;
            if (_tex.TryGetValue(key, out t)) return t;
            var tiles = new List<Texture2D>();
            foreach (var n in new[] { "top_path_a", "top_path_b", "top_path_c", "top_path_d" }) { var tx = Readable(Folder(act) + "/tiles/" + n) ?? Readable(Folder(1) + "/tiles/" + n); if (tx != null && tx.width == 64 && tx.height == 64) tiles.Add(tx); }
            if (tiles.Count == 0) { _tex[key] = null; return null; }
            const int S = 64;
            t = NewTex(S * 2, S * 2, true, true);
            var px = new Color32[S * 2 * S * 2];
            for (int q = 0; q < 4; q++)
            {
                var src = tiles[q % tiles.Count].GetPixels32();
                int ox = (q & 1) * S, oy = (q >> 1) * S;
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) px[(oy + y) * S * 2 + ox + x] = Grade(src[y * S + x], 0.62f, 0.72f, 6f, NightMul);
            }
            t.SetPixels32(px); t.Apply(false, true);
            _tex[key] = t;
            return t;
        }

        /// <summary>
        /// 踏み跡の土に夜の色を重ねて焼いた物 (2026-10-28 ユーザー「道が醜すぎる。デザイン案と全然違う」)。カンバスの案1 は土の模様 (top_path_a を 2 倍) の上に
        /// rgba(20,28,58,.52) などの帯を sRGB で重ねている。Unity (Linear の色空間) で α の帯を重ねると暗く濁るので、ブラウザと同じ sRGB の式で画素に焼き、不透明で描く。
        /// state: 0 進める・通った (.52) / 1 まだ先 (.62) / 2 通らなかった (.72)
        /// </summary>
        public static Texture2D DirtState(int act, int state)
        {
            string key = "dirt" + act + "s" + state;
            Texture2D t;
            if (_tex.TryGetValue(key, out t)) return t;
            var src = Readable(Folder(act) + "/tiles/top_path_a") ?? Readable(Folder(1) + "/tiles/top_path_a");
            if (src == null || src.width != 64 || src.height != 64) { _tex[key] = null; return null; }
            Color32 c; float a;
            switch (state)
            {
                case 1: c = new Color32(18, 24, 52, 255); a = 0.62f; break;
                case 2: c = PaperFx.Ground; a = 0.72f; break;   // 通らなかった道は地の色 (夜) で沈める
                default: c = new Color32(20, 28, 58, 255); a = 0.52f; break;
            }
            var px = src.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                px[i] = new Color32((byte)Mathf.RoundToInt(p.r * (1f - a) + c.r * a), (byte)Mathf.RoundToInt(p.g * (1f - a) + c.g * a), (byte)Mathf.RoundToInt(p.b * (1f - a) + c.b * a), 255);
            }
            t = NewTex(64, 64, true, true);
            t.SetPixels32(px); t.Apply(false, true);
            _tex[key] = t;
            return t;
        }

        /// <summary>sRGB の α (ブラウザの重ね) を Linear の重ねで同じ暗さにする α (黒に近い色の重ねだけに使う。.55 → .83)</summary>
        public static float LinearAlpha(float srgbAlpha) { return 1f - Mathf.Pow(1f - srgbAlpha, 2.2f); }

        /// <summary>木・岩・茂みの絵を夜の色に焼いた Point の複製 (path は Resources の中の道)。明るさは Image.color で掛ける</summary>
        public static Sprite Graded(string path, float sat, float hue)
        {
            string key = path + "@" + sat + "/" + hue;
            Sprite s;
            if (_spr.TryGetValue(key, out s)) return s;
            var src = Readable(path);
            if (src == null) { _spr[key] = null; return null; }
            var t = NewTex(src.width, src.height, false, true);
            var a = src.GetPixels32();
            for (int i = 0; i < a.Length; i++) a[i] = Grade(a[i], 1f, sat, hue, Vector3.one);
            t.SetPixels32(a); t.Apply(false, true);
            s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
            _spr[key] = s;
            return s;
        }

        static Sprite Proc(string key, int w, int h, Func<float, float, float> alpha)
        {
            Sprite s;
            if (_spr.TryGetValue(key, out s)) return s;
            var t = NewTex(w, h, false, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color32(255, 255, 255, B(alpha((x + 0.5f) / w, (y + 0.5f) / h)));
            t.SetPixels32(px); t.Apply(false, true);
            s = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _spr[key] = s;
            return s;
        }

        /// <summary>白いやわらかい丸 (closest-side の放射。中心 1・縁 0)。色は Image.color で</summary>
        public static Sprite Radial()
        {
            return Proc("radial", 128, 128, delegate (float u, float v)
            {
                float dx = u * 2f - 1f, dy = v * 2f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
                float k = Mathf.Clamp01(1f - r);
                return k * k * (3f - 2f * k);
            });
        }

        /// <summary>楕円の輪 (幅は箱の高さの 1/15。進める節の真鍮の輪)</summary>
        public static Sprite Ring()
        {
            return Proc("ring", 256, 96, delegate (float u, float v)
            {
                float dx = u * 2f - 1f, dy = v * 2f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
                float d = Mathf.Abs(r - 0.93f);
                return Mathf.Clamp01(1f - (d - 0.035f) / 0.03f);
            });
        }

        /// <summary>縦の帯: 上 1・下 0 (霧・上下の暗がり)</summary>
        public static Sprite VFade()
        {
            return Proc("vfade", 4, 128, delegate (float u, float v) { float k = v; return k * k * (3f - 2f * k); });
        }

        /// <summary>横の帯: 真ん中 1・両端 0 (月光の筋)</summary>
        public static Sprite HBand()
        {
            return Proc("hband", 64, 4, delegate (float u, float v) { float k = 1f - Mathf.Abs(u * 2f - 1f); return k * k * (3f - 2f * k); });
        }

        /// <summary>周辺減光: 中心 0・四隅 1 (楕円 72%×72% の外から)</summary>
        public static Sprite Vignette()
        {
            return Proc("vignette", 256, 256, delegate (float u, float v)
            {
                float dx = (u - 0.5f) / 0.78f, dy = (v - 0.46f) / 0.80f, r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float k = Mathf.Clamp01((r - 0.62f) / 0.5f);
                return k * k * (3f - 2f * k);
            });
        }

        /// <summary>絵の不透明な画素の左右の真ん中 (0〜1)。駒を節の真上に立てる (このは 0.51・ひばな 0.47)</summary>
        static readonly Dictionary<Sprite, float> _vcx = new Dictionary<Sprite, float>();
        public static float VisibleCenterX(Sprite sp)
        {
            if (sp == null) return 0.5f;
            float v;
            if (_vcx.TryGetValue(sp, out v)) return v;
            v = 0.5f;
            try
            {
                var t = sp.texture;
                var r = sp.textureRect;
                var px = t.GetPixels32();
                int x0 = Mathf.RoundToInt(r.x), y0 = Mathf.RoundToInt(r.y), w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
                int lo = w, hi = -1;
                for (int y = y0; y < y0 + h; y++)
                    for (int x = x0; x < x0 + w; x++)
                        if (px[y * t.width + x].a > 40) { if (x - x0 < lo) lo = x - x0; if (x - x0 > hi) hi = x - x0; }
                if (hi >= lo) v = (lo + hi + 1) * 0.5f / w;
            }
            catch (Exception) { v = 0.5f; }
            _vcx[sp] = v;
            return v;
        }
    }

    /// <summary>
    /// 踏み跡の帯 (2026-10-28 案1)。折れ線ごとに太さ・色・破線を持ち、テクスチャは画面の座標で貼る (u = x / TexPx・v = y / TexPx)。
    /// RectTransform は content いっぱい・pivot (0,0) に置く (頂点の座標 = 節の anchoredPosition と同じ)。破線の 2px 以下の点は正方形 (ドットの粒)
    /// </summary>
    public class MapTrail : MaskableGraphic
    {
        public Texture Tex;
        public float TexPx = 128f;
        public override Texture mainTexture { get { return Tex != null ? Tex : s_WhiteTexture; } }

        /// <summary>帯の両端を丸める (SVG の stroke-linecap: round。節で帯がなめらかにつながる)</summary>
        public bool RoundCaps = true;

        struct Stroke { public List<Vector2> P; public float W; public Color32 C; public float Dash, Gap; }
        readonly List<Stroke> _strokes = new List<Stroke>();

        public void Add(List<Vector2> pts, float width, Color color, float dash = 0f, float gap = 0f)
        {
            if (pts == null || pts.Count < 2) return;
            _strokes.Add(new Stroke { P = pts, W = width, C = color, Dash = dash, Gap = gap });
            SetVerticesDirty();
        }

        void Cap(VertexHelper vh, Vector2 c, Vector2 dir, float w, Color32 col)
        {
            // dir の向きへ半円の扇 (10 分割)
            int center = vh.currentVertCount;
            Vert(vh, c, col);
            float r = w * 0.5f, a0 = Mathf.Atan2(dir.y, dir.x) - Mathf.PI * 0.5f;
            const int N = 10;
            for (int i = 0; i <= N; i++)
            {
                float a = a0 + Mathf.PI * i / N;
                Vert(vh, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col);
            }
            for (int i = 0; i < N; i++) vh.AddTriangle(center, center + 1 + i, center + 2 + i);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < _strokes.Count; i++)
            {
                var s = _strokes[i];
                if (s.Dash <= 0f) Strip(vh, s.P, s.W, s.C);
                else Dashes(vh, s);
            }
        }

        void Vert(VertexHelper vh, Vector2 p, Color32 c) { vh.AddVert(new Vector3(p.x, p.y, 0f), c, new Vector4(p.x / TexPx, p.y / TexPx, 0f, 0f)); }

        void Strip(VertexHelper vh, List<Vector2> p, float w, Color32 c)
        {
            int n = p.Count;
            if (n < 2) return;
            int start = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                Vector2 t = (p[Mathf.Min(i + 1, n - 1)] - p[Mathf.Max(i - 1, 0)]);
                if (t.sqrMagnitude < 1e-6f) t = Vector2.up;
                t.Normalize();
                Vector2 nn = new Vector2(-t.y, t.x) * (w * 0.5f);
                Vert(vh, p[i] + nn, c);
                Vert(vh, p[i] - nn, c);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int k = start + i * 2;
                vh.AddTriangle(k, k + 1, k + 3);
                vh.AddTriangle(k, k + 3, k + 2);
            }
            if (RoundCaps && w > 6f)
            {
                Cap(vh, p[0], (p[0] - p[1]).normalized, w, c);
                Cap(vh, p[n - 1], (p[n - 1] - p[n - 2]).normalized, w, c);
            }
        }

        void Dot(VertexHelper vh, Vector2 c, float w, Color32 col)
        {
            int k = vh.currentVertCount;
            float h = w * 0.5f;
            Vert(vh, c + new Vector2(-h, -h), col); Vert(vh, c + new Vector2(-h, h), col);
            Vert(vh, c + new Vector2(h, h), col); Vert(vh, c + new Vector2(h, -h), col);
            vh.AddTriangle(k, k + 1, k + 2); vh.AddTriangle(k, k + 2, k + 3);
        }

        void Dashes(VertexHelper vh, Stroke s)
        {
            float period = s.Dash + s.Gap, along = 0f;
            var cur = new List<Vector2>();
            for (int i = 0; i < s.P.Count - 1; i++)
            {
                Vector2 a = s.P[i], b = s.P[i + 1];
                float len = (b - a).magnitude;
                float t = 0f;
                while (t < len)
                {
                    float phase = along % period;
                    bool on = phase < s.Dash;
                    float step = Mathf.Min(len - t, on ? s.Dash - phase : period - phase);
                    if (step <= 0.0001f) step = 0.0001f;
                    Vector2 p0 = Vector2.Lerp(a, b, t / len), p1 = Vector2.Lerp(a, b, (t + step) / len);
                    if (on)
                    {
                        if (cur.Count == 0) cur.Add(p0);
                        cur.Add(p1);
                        if (phase + step >= s.Dash - 0.0001f) { Flush(vh, cur, s); cur = new List<Vector2>(); }
                    }
                    t += step; along += step;
                }
            }
            Flush(vh, cur, s);
        }

        void Flush(VertexHelper vh, List<Vector2> pts, Stroke s)
        {
            if (pts.Count < 2) return;
            if (s.Dash <= 2.01f) Dot(vh, (pts[0] + pts[pts.Count - 1]) * 0.5f, s.W, s.C);
            else Strip(vh, pts, s.W, s.C);
        }
    }

    /// <summary>地図の小さな動き (2026-10-28 案1): 蛍の漂いと明滅・進める節の印の上下と輪の脈。時計は Time.unscaledTime</summary>
    public class MapLife : MonoBehaviour
    {
        public readonly List<RectTransform> Flies = new List<RectTransform>();
        readonly List<Vector2> _flyBase = new List<Vector2>();
        readonly List<float> _flyPhase = new List<float>();
        public readonly List<RectTransform> Bobs = new List<RectTransform>();
        readonly List<Vector2> _bobBase = new List<Vector2>();
        public readonly List<Image> Pulses = new List<Image>();
        readonly List<float> _pulseA = new List<float>();
        bool _init;

        void Init()
        {
            _init = true;
            for (int i = 0; i < Flies.Count; i++) { _flyBase.Add(Flies[i] != null ? Flies[i].anchoredPosition : Vector2.zero); _flyPhase.Add(i * 1.618f); }
            for (int i = 0; i < Bobs.Count; i++) _bobBase.Add(Bobs[i] != null ? Bobs[i].anchoredPosition : Vector2.zero);
            for (int i = 0; i < Pulses.Count; i++) _pulseA.Add(Pulses[i] != null ? Pulses[i].color.a : 1f);
        }

        void Update()
        {
            if (!_init) Init();
            float t = Time.unscaledTime;
            for (int i = 0; i < Flies.Count; i++)
            {
                var f = Flies[i];
                if (f == null) continue;
                float ph = _flyPhase[i];
                f.anchoredPosition = _flyBase[i] + new Vector2(Mathf.Sin(t * 0.37f + ph) * 22f, Mathf.Sin(t * 0.53f + ph * 1.3f) * 14f);
                var img = f.GetComponent<Image>();
                if (img != null) { var c = img.color; c.a = 0.35f + 0.55f * Mathf.Max(0f, Mathf.Sin(t * 1.1f + ph * 2.1f)); img.color = c; }
            }
            for (int i = 0; i < Bobs.Count; i++)
            {
                var b = Bobs[i];
                if (b == null) continue;
                b.anchoredPosition = _bobBase[i] + new Vector2(0f, Mathf.Round(Mathf.Sin(t * 2.4f + i) * 2f) * 2f);   // 2px の段で (ドットの粒)
            }
            for (int i = 0; i < Pulses.Count; i++)
            {
                var p = Pulses[i];
                if (p == null) continue;
                var c = p.color; c.a = _pulseA[i] * (0.72f + 0.28f * Mathf.Sin(t * 3.1f + i)); p.color = c;
            }
        }
    }
}
