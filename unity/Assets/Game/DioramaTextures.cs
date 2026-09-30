// DioramaTextures.cs — 箱庭のテクスチャ (2026-09-30 HD-2D 見本 P04。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-2)。
//  ・材質ごとの4種のタイルを1つの Texture2DArray に積む (_Albedo・_Normal)。CPU の SetPixels32 で埋めてミップを付ける
//    (CopyTexture は GPU の無いバッチで落ちるので使わない)。法線 (データ) の配列は linear:true。色の配列も既定は linear:true で
//    sRGB の色をそのまま入れ、Linear の色空間の時だけシェーダが戻す (StageModule の _AlbedoDecode=1。設計図 albedoLinear=false なら sRGB の配列・_AlbedoDecode=0)。
//    メッシュの UV で貼る _BaseMap (幹のタイル・アトラス) は普通の sRGB のテクスチャ。
//  ・タイルは既定 64×64 (2.56 unit)。既存の 32×32 は縮めずに 2×2 に敷いて 64 にする (粒は 25 テクセル/unit のまま)。
//  ・半立体と札の絵は1枚のアトラスに詰める (材質を増やさない。縁を外へ伸ばしてにじみを防ぐ)。
//  ・読むのはどれも Resources の読み取り可の絵 (ArtImporter が Resources/Art を読み取り可にする)。読めなければ次の候補、無ければ単色で埋める = Build は落ちない。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game
{
    /// <summary>タイルの候補1つ (Resources のパスと、読み込んだ後の整え方)</summary>
    public sealed class DioramaTileCandidate
    {
        public string Art;
        /// <summary>明暗の幅 (1 = そのまま・0.35 = 平均のまわりに 35% へ縮める)。PixelLab の新しいタイル (tile-calm 済み) は 1</summary>
        public float Calm = 1f;
        /// <summary>彩度の倍率 (1 = そのまま)</summary>
        public float Sat = 1f;
        /// <summary>白い粒を平均の近くへ寄せる</summary>
        public bool ClampWhite;
    }

    /// <summary>材質1つ (moss・dirt・rock・plank…) のタイルの並び。Variants[i] = i 種目の候補 (先に見つかった物を使う)</summary>
    public sealed class DioramaTileMaterial
    {
        public string Name;
        public readonly List<List<DioramaTileCandidate>> Variants = new List<List<DioramaTileCandidate>>();
        /// <summary>タイルの回し方: 0 = none・1 = flipX (左右の反転だけ)・2 = rot4 (90° ずつ4通り。向きのない土・岩の天面だけ)</summary>
        public int Rot;
        /// <summary>何も読めなかった時の単色</summary>
        public Color32 Fallback = new Color32(96, 104, 88, 255);
    }

    public static class DioramaTextures
    {
        /// <summary>配列の中の材質の場所 (First から Count 枚)</summary>
        public sealed class Slices
        {
            public string Name;
            public int First, Count, Rot;
            /// <summary>使った絵のパス (種ごと。無ければ "(単色)")</summary>
            public readonly List<string> Used = new List<string>();
        }

        public sealed class ArraySet
        {
            public Texture2DArray Albedo, Normal;
            public int TileSize;
            public readonly Dictionary<string, Slices> Materials = new Dictionary<string, Slices>();
            /// <summary>見つからなかった絵 (候補を全部試して単色に落ちた種)</summary>
            public readonly List<string> Missing = new List<string>();
        }

        /// <summary>
        /// 材質のタイルを1つの配列に積む。tile = 1枚のテクセル (64)。normalStrength = 明るさから作る法線の強さ。
        /// 各タイルは「同じ材質の1種目の平均色」へ寄せる (種の違いが格子に見えない)。法線は同じ名前に _n があればそれ、無ければ明るさから作る
        /// </summary>
        public static ArraySet BuildArrays(IList<DioramaTileMaterial> mats, int tile, bool albedoLinear, float normalStrength)
        {
            var set = new ArraySet { TileSize = tile };
            var colors = new List<Color32[]>();
            var normals = new List<Color32[]>();
            foreach (var m in mats)
            {
                var sl = new Slices { Name = m.Name, First = colors.Count, Rot = m.Rot };
                Vector3 mean0 = Vector3.zero;
                int vcount = Mathf.Max(1, m.Variants.Count);
                for (int v = 0; v < vcount; v++)
                {
                    Color32[] px = null; Color32[] npx = null; string used = null;
                    var cands = v < m.Variants.Count ? m.Variants[v] : null;
                    if (cands != null)
                        foreach (var c in cands)
                        {
                            var tex = LoadTex(c.Art);
                            var raw = tex != null ? ReadPixels(tex) : null;
                            if (raw == null) continue;
                            px = ExpandToTile(raw, tex.width, tex.height, tile);
                            if (px == null) continue;
                            Process(px, c.Calm, c.Sat, c.ClampWhite);
                            var ntex = LoadTex(c.Art + "_n");
                            var nraw = ntex != null ? ReadPixels(ntex) : null;
                            if (nraw != null) npx = ExpandToTile(nraw, ntex.width, ntex.height, tile);
                            used = c.Art;
                            break;
                        }
                    if (px == null)
                    {
                        px = new Color32[tile * tile];
                        for (int i = 0; i < px.Length; i++) px[i] = m.Fallback;
                        used = "(単色)";
                        set.Missing.Add(m.Name + "[" + v + "]");
                    }
                    var mean = Mean(px);
                    if (v == 0) mean0 = mean;
                    else ShiftMean(px, mean, mean0, 0.8f);
                    for (int i = 0; i < px.Length; i++) px[i].a = 255;
                    colors.Add(px);
                    normals.Add(npx ?? NormalFromLuma(px, tile, normalStrength));
                    sl.Used.Add(used);
                }
                sl.Count = vcount;
                set.Materials[m.Name] = sl;
            }
            int depth = Mathf.Max(1, colors.Count);
            if (!SystemInfo.supports2DArrayTextures && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                // 配列の無い端末 (今の対象には無い)。材質は配列なしで作られ、StageModule は _BaseColor だけになる
                Debug.LogWarning("[Diorama] この端末は Texture2DArray を使えない");
                set.Missing.Add("Texture2DArray");
                return set;
            }
            set.Albedo = new Texture2DArray(tile, tile, depth, TextureFormat.RGBA32, true, albedoLinear) { name = "diorama-albedo", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat, anisoLevel = 1 };
            set.Normal = new Texture2DArray(tile, tile, depth, TextureFormat.RGBA32, true, true) { name = "diorama-normal", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat, anisoLevel = 1 };
            try
            {
                for (int i = 0; i < colors.Count; i++)
                {
                    set.Albedo.SetPixels32(colors[i], i, 0);
                    set.Normal.SetPixels32(normals[i], i, 0);
                }
                set.Albedo.Apply(true, false);
                set.Normal.Apply(true, false);
            }
            catch (Exception e)
            {
                // 描く装置の無いバッチなどで埋められなかった (数える点検は続けられる)
                Debug.LogWarning("[Diorama] タイルの配列を埋められない: " + e.Message);
                set.Missing.Add("Texture2DArray の埋め込み");
            }
            return set;
        }

        /// <summary>1枚のタイルを敷き詰め用の Texture2D にする (幹の円筒の UV・滑車など、メッシュの UV で貼る材質用)。Repeat・Bilinear・ミップあり</summary>
        public static Texture2D TileTexture(DioramaTileMaterial m, int tile, bool linear, out string used)
        {
            used = null;
            Color32[] px = null;
            if (m.Variants.Count > 0)
                foreach (var c in m.Variants[0])
                {
                    var tex = LoadTex(c.Art);
                    var raw = tex != null ? ReadPixels(tex) : null;
                    if (raw == null) continue;
                    px = ExpandToTile(raw, tex.width, tex.height, tile);
                    if (px == null) continue;
                    Process(px, c.Calm, c.Sat, c.ClampWhite);
                    used = c.Art;
                    break;
                }
            if (px == null)
            {
                px = new Color32[tile * tile];
                for (int i = 0; i < px.Length; i++) px[i] = m.Fallback;
                used = "(単色)";
            }
            for (int i = 0; i < px.Length; i++) px[i].a = 255;
            var t = new Texture2D(tile, tile, TextureFormat.RGBA32, true, linear) { name = "diorama-" + m.Name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            t.SetPixels32(px);
            t.Apply(true, false);
            return t;
        }

        // ---------------------------------------------------------------- アトラス (半立体と札の絵)

        public sealed class AtlasEntry
        {
            public string Id;
            /// <summary>アトラスの中の場所 (テクセル)</summary>
            public RectInt Px;
            /// <summary>アトラスの中の UV</summary>
            public Rect Uv;
            /// <summary>元の絵の全テクセル (半立体の形を作るのに使う)。行 0 が下</summary>
            public Color32[] Src;
            public int W, H;
        }

        public sealed class Atlas
        {
            public Texture2D Tex;
            public readonly Dictionary<string, AtlasEntry> Entries = new Dictionary<string, AtlasEntry>();
            public readonly List<string> Missing = new List<string>();
        }

        /// <summary>
        /// 絵をアトラスに詰める (棚詰め・背の高い順)。pad = まわりの余白 (縁の色を外へ伸ばす = ミップとバイリニアのにじみ止め)。
        /// sources: id → 候補のパス (先に読めた物)。何も読めなかった id は Missing に入り、Entries には入らない
        /// </summary>
        public static Atlas BuildAtlas(IList<KeyValuePair<string, IList<string>>> sources, int pad, int maxSize)
        {
            var atlas = new Atlas();
            var items = new List<AtlasEntry>();
            foreach (var kv in sources)
            {
                Color32[] px = null; int w = 0, h = 0;
                foreach (var path in kv.Value)
                {
                    var tex = LoadTex(path);
                    var raw = tex != null ? ReadPixels(tex) : null;
                    if (raw == null) continue;
                    px = raw; w = tex.width; h = tex.height;
                    break;
                }
                if (px == null) { atlas.Missing.Add(kv.Key); continue; }
                px = CropToAlpha(px, ref w, ref h, 16);   // 透明な余白を切る (足元の中心 = 見えている絵の下端になる)
                items.Add(new AtlasEntry { Id = kv.Key, Src = px, W = w, H = h });
            }
            items.Sort((a, b) => b.H != a.H ? b.H.CompareTo(a.H) : string.CompareOrdinal(a.Id, b.Id));
            int size = 256;
            while (size <= maxSize && !Pack(items, size, pad)) size *= 2;
            if (size > maxSize) { size = maxSize; Pack(items, size, pad); }
            var outPx = new Color32[size * size];
            foreach (var e in items)
            {
                if (e.Px.width <= 0) { atlas.Missing.Add(e.Id + "(入りきらない)"); continue; }
                Blit(outPx, size, e, pad);
                e.Uv = new Rect(e.Px.x / (float)size, e.Px.y / (float)size, e.Px.width / (float)size, e.Px.height / (float)size);
                atlas.Entries[e.Id] = e;
            }
            atlas.Tex = new Texture2D(size, size, TextureFormat.RGBA32, 4, false) { name = "diorama-atlas", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            atlas.Tex.SetPixels32(outPx);
            atlas.Tex.Apply(true, false);
            return atlas;
        }

        /// <summary>アルファが minA を超えるテクセルの外接の四角へ切る (全部透明ならそのまま)</summary>
        public static Color32[] CropToAlpha(Color32[] px, ref int w, ref int h, byte minA)
        {
            int x0 = w, y0 = h, x1 = -1, y1 = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > minA) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
            if (x1 < 0 || (x0 == 0 && y0 == 0 && x1 == w - 1 && y1 == h - 1)) return px;
            int nw = x1 - x0 + 1, nh = y1 - y0 + 1;
            var o = new Color32[nw * nh];
            for (int y = 0; y < nh; y++)
                for (int x = 0; x < nw; x++) o[y * nw + x] = px[(y0 + y) * w + x0 + x];
            w = nw; h = nh;
            return o;
        }

        static bool Pack(List<AtlasEntry> items, int size, int pad)
        {
            foreach (var e in items) e.Px = new RectInt(0, 0, 0, 0);   // 入らなかった物は幅 0 のまま (前の試しの位置を残さない)
            int x = pad, y = pad, rowH = 0;
            foreach (var e in items)
            {
                int w = e.W + pad * 2, h = e.H + pad * 2;
                if (w > size || h > size) { e.Px = new RectInt(0, 0, 0, 0); return false; }
                if (x + w > size) { x = pad; y += rowH; rowH = 0; }
                if (y + h > size) return false;
                e.Px = new RectInt(x + pad, y + pad, e.W, e.H);
                x += w; rowH = Mathf.Max(rowH, h);
            }
            return true;
        }

        /// <summary>絵を写し、透明な縁の色を不透明の隣から pad テクセルまで伸ばす (アルファは 0 のまま)</summary>
        static void Blit(Color32[] dst, int size, AtlasEntry e, int pad)
        {
            for (int y = 0; y < e.H; y++)
                for (int x = 0; x < e.W; x++)
                    dst[(e.Px.y + y) * size + e.Px.x + x] = e.Src[y * e.W + x];
            // 縁の色を外へ (透明なテクセルの RGB を近くの不透明の色に)
            int x0 = e.Px.x - pad, y0 = e.Px.y - pad, w = e.W + pad * 2, h = e.H + pad * 2;
            var filled = new bool[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int ax = x0 + x, ay = y0 + y;
                    if (ax < 0 || ay < 0 || ax >= size || ay >= size) continue;
                    filled[y * w + x] = dst[ay * size + ax].a > 8;
                }
            for (int pass = 0; pass < pad + 2; pass++)
            {
                var next = (bool[])filled.Clone();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (filled[y * w + x]) continue;
                        int ax = x0 + x, ay = y0 + y;
                        if (ax < 0 || ay < 0 || ax >= size || ay >= size) continue;
                        int r = 0, g = 0, bl = 0, n = 0;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h || !filled[ny * w + nx]) continue;
                                var c = dst[(y0 + ny) * size + x0 + nx];
                                r += c.r; g += c.g; bl += c.b; n++;
                            }
                        if (n == 0) continue;
                        dst[ay * size + ax] = new Color32((byte)(r / n), (byte)(g / n), (byte)(bl / n), 0);
                        next[y * w + x] = true;
                    }
                filled = next;
            }
        }

        // ---------------------------------------------------------------- 光の絵 (霧の面と光の筋)

        /// <summary>
        /// 光の面の絵 (128×64・linear)。左半分 = 霧の面 (下が明るく上へ消える・左右の端は柔らかく)、右半分 = 光の筋 (上 = 出口が明るく下へ消える・弱め)。
        /// UV の四角: 霧 = (0,0,0.5,1)・筋 = (0.5,0,0.5,1)
        /// </summary>
        public static Texture2D GlowTexture()
        {
            const int W = 128, H = 64;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x % 64 + 0.5f) / 64f, v = (y + 0.5f) / H;
                    float edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(u, 1f - u) / 0.28f));
                    float a;
                    if (x < 64) a = edge * Mathf.Pow(1f - v, 1.6f) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v / 0.08f));   // 霧: 足元はわずかに抜く (地面との交わりは深度で消す)
                    else a = 0.45f * edge * Mathf.Pow(v, 1.3f);                                                             // 筋: 出口が明るい
                    byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255);
                    px[y * W + x] = new Color32(g, g, g, g);
                }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false, true) { name = "diorama-glow", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply(false, false);
            return t;
        }

        // ---------------------------------------------------------------- 読み込みと整え

        /// <summary>Resources のテクスチャ (スプライトとして取り込まれた絵の本体も読める)。無ければ null</summary>
        public static Texture2D LoadTex(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var t = Resources.Load<Texture2D>(path);
            if (t == null)
            {
                var sp = Resources.Load<Sprite>(path);
                if (sp != null) t = sp.texture;
            }
            return t;
        }

        /// <summary>全テクセル (読めなければ null。読み取り不可の絵・圧縮形式は例外になるので捕まえる)</summary>
        public static Color32[] ReadPixels(Texture2D t)
        {
            if (t == null) return null;
            try { return t.GetPixels32(); }
            catch (Exception) { return null; }
        }

        /// <summary>タイルを tile×tile にする: 同じ大きさはそのまま、約数なら敷き詰め (粒を保つ)、それ以外は最近傍で写す</summary>
        public static Color32[] ExpandToTile(Color32[] src, int w, int h, int tile)
        {
            if (src == null || w <= 0 || h <= 0 || src.Length < w * h) return null;
            var dst = new Color32[tile * tile];
            bool repeat = tile % w == 0 && tile % h == 0;
            for (int y = 0; y < tile; y++)
                for (int x = 0; x < tile; x++)
                {
                    int sx = repeat ? x % w : Mathf.Min(w - 1, x * w / tile);
                    int sy = repeat ? y % h : Mathf.Min(h - 1, y * h / tile);
                    dst[y * tile + x] = src[sy * w + sx];
                }
            return dst;
        }

        static float Luma(Color32 c) { return c.r * 0.3f + c.g * 0.59f + c.b * 0.11f; }

        /// <summary>明暗の幅を縮め (calm)、彩度を掛け (sat)、白い粒を寄せる (clampWhite)</summary>
        public static void Process(Color32[] px, float calm, float sat, bool clampWhite)
        {
            if (Mathf.Approximately(calm, 1f) && Mathf.Approximately(sat, 1f) && !clampWhite) return;
            double sum = 0, sum2 = 0;
            for (int i = 0; i < px.Length; i++) { float l = Luma(px[i]); sum += l; sum2 += l * l; }
            float mean = (float)(sum / Mathf.Max(1, px.Length));
            float sd = Mathf.Sqrt(Mathf.Max(0f, (float)(sum2 / Mathf.Max(1, px.Length)) - mean * mean));
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                float l = Luma(c);
                float target = mean + (l - mean) * calm;
                if (clampWhite && target > mean + 1.5f * sd) target = mean + 1.5f * sd;
                float k = l > 1f ? target / l : 1f;
                float r = c.r * k, g = c.g * k, b = c.b * k;
                float gg = target;
                r = gg + (r - gg) * sat; g = gg + (g - gg) * sat; b = gg + (b - gg) * sat;
                px[i] = new Color32((byte)Mathf.Clamp(r, 0f, 255f), (byte)Mathf.Clamp(g, 0f, 255f), (byte)Mathf.Clamp(b, 0f, 255f), c.a);
            }
        }

        static Vector3 Mean(Color32[] px)
        {
            var m = Vector3.zero;
            for (int i = 0; i < px.Length; i++) m += new Vector3(px[i].r, px[i].g, px[i].b);
            return m / Mathf.Max(1, px.Length);
        }

        static void ShiftMean(Color32[] px, Vector3 mean, Vector3 target, float amount)
        {
            var k = new Vector3(target.x / Mathf.Max(1f, mean.x), target.y / Mathf.Max(1f, mean.y), target.z / Mathf.Max(1f, mean.z));
            k = Vector3.Lerp(Vector3.one, k, amount);
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32((byte)Mathf.Clamp(px[i].r * k.x, 0f, 255f), (byte)Mathf.Clamp(px[i].g * k.y, 0f, 255f), (byte)Mathf.Clamp(px[i].b * k.z, 0f, 255f), px[i].a);
        }

        /// <summary>明るさを高さと読んで法線を作る (3×3 でならしてから差分。巻き戻しあり)。RGB = 法線×0.5+0.5 (シェーダが自分で ×2−1 でほどく)</summary>
        public static Color32[] NormalFromLuma(Color32[] px, int size, float strength)
        {
            var lum = new float[size * size];
            for (int i = 0; i < lum.Length; i++) lum[i] = Luma(px[i]) / 255f;
            var sm = new float[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 0f;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) a += lum[((y + dy + size) % size) * size + (x + dx + size) % size];
                    sm[y * size + x] = a / 9f;
                }
            var o = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float l = sm[y * size + (x - 1 + size) % size], r = sm[y * size + (x + 1) % size];
                    float d = sm[((y - 1 + size) % size) * size + x], u = sm[((y + 1) % size) * size + x];
                    var n = new Vector3(-(r - l) * strength * 4f, -(u - d) * strength * 4f, 1f).normalized;
                    o[y * size + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
                }
            return o;
        }

        // ---------------------------------------------------------------- なめらかなノイズ (空き地の縁・揺らぎ)

        /// <summary>値ノイズ (0〜1)。seed ごとに決定的。UnityEngine.Random・Mathf.PerlinNoise の内部の乱数は使わない</summary>
        public static float Noise(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float sx = fx * fx * (3f - 2f * fx), sy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed), c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        /// <summary>2段の値ノイズ (0〜1)</summary>
        public static float Fbm(float x, float y, int seed)
        {
            return Noise(x, y, seed) * 0.65f + Noise(x * 2.03f + 17.1f, y * 2.03f - 5.3f, seed + 101) * 0.35f;
        }

        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144269504);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
