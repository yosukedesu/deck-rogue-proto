// ReliefMesh.cs — 半立体 (2.5D) のメッシュ: スプライトの輪郭から高さを作り、表側だけ膨らませる (2026-09-30 HD-2D 見本 P04。計画 §2-2)。
// 樹冠の葉・茂み・羊歯・大岩の一部などの自然物に使う。裏面は持たない。カメラの方へは回さず、置いた向きのまま立てる。
// 膨らみの深さは幅の depthRatio 倍 (既定 0.12)。粒はキャラと同じ 25 テクセル/unit (1ドット 0.04 unit)。
// 形: 不透明のテクセル (alpha > cutoff) から輪郭までの距離 (面取りの距離変換) を測り、枕の形 sqrt(1−(1−d)²) で高さにする
// (d = 距離 ÷ ramp。ramp は膨らみの 1.6 倍以上。細い絵は膨らみを縮める)。
// 格子の1マス = cell テクセル。1つでも不透明のテクセルを含むマスだけ面を作る (抜けはシェーダのアルファで切る)。
// 表はローカル −z (カメラの方)。膨らみも −z へ。足元の中心 (pivot) が原点。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game
{
    public static class ReliefMesh
    {
        /// <summary>
        /// 半立体を作る。px = 元の絵の全テクセル (pw×ph。行 0 が下)、rect = 使う範囲 (テクセル)、uvRect = その範囲がアトラスで占める UV。
        /// depthRatio = 膨らみの深さ ÷ 幅、cell = 格子の1マスのテクセル (0 以下なら自動 = 長い辺を約 40 マス)、cutoff = 不透明とみなすアルファ (0〜1)。
        /// flipX = 左右を反転 (UV は元のまま・形と並びを鏡に)。pivot = 原点にする位置 (0〜1。既定 (0.5, 0) = 足元の中心)
        /// </summary>
        public static DioramaMeshBuilder Build(Color32[] px, int pw, int ph, RectInt rect, Rect uvRect, float depthRatio, int cell, float cutoff, bool flipX, Vector2 pivot)
        {
            var b = new DioramaMeshBuilder();
            int w = rect.width, h = rect.height;
            if (px == null || w <= 0 || h <= 0 || px.Length < pw * ph) return b;
            if (cell <= 0) cell = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(w, h) / 40f));
            byte cut = (byte)Mathf.Clamp(Mathf.RoundToInt(cutoff * 255f), 1, 254);

            // 不透明の印と、輪郭までの距離 (3-4 の面取り距離変換。単位はテクセル×3)
            var inside = new bool[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sx = rect.x + x, sy = rect.y + y;
                    inside[y * w + x] = sx >= 0 && sy >= 0 && sx < pw && sy < ph && px[sy * pw + sx].a > cut;
                }
            var dist = DistanceToOutside(inside, w, h);
            float dmax = 1f;
            for (int i = 0; i < dist.Length; i++) dmax = Mathf.Max(dmax, dist[i]);

            float unit = 1f / DioramaMesh.TexelsPerUnit;
            // 膨らみ = 幅 × depthRatio。ただし縁から頂までの距離 (ramp) を膨らみの 1.6 倍以上にとる = 斜面を 30° 程度に抑える。
            // 細い絵 (枝・蔓) は縁からの距離がすぐ尽きるので、膨らみの方を ramp ÷ 1.6 まで縮める (尖った畝で光が割れない)
            float depthTex = Mathf.Max(0f, depthRatio) * w;
            float ramp = Mathf.Max(2f, Mathf.Min(dmax * 0.9f, depthTex * 1.6f));
            depthTex = Mathf.Min(depthTex, ramp / 1.6f);
            float depth = depthTex * unit;
            float ox = pivot.x * w, oy = pivot.y * h;

            // 格子の頂点の高さ = まわり 2×2 テクセルの高さの平均 (外は 0)。輪郭へ向かってなだらかに 0 へ下りる
            Func<int, int, float> texelH = (x, y) =>
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return 0f;
                int i = y * w + x;
                if (!inside[i]) return 0f;
                float d = Mathf.Clamp01(dist[i] / ramp);
                float k = 1f - d;
                return depth * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k));
            };
            int nx = Mathf.CeilToInt(w / (float)cell), ny = Mathf.CeilToInt(h / (float)cell);
            var used = new bool[nx, ny];
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    bool any = false;
                    for (int y = j * cell; y < Mathf.Min(h, (j + 1) * cell) && !any; y++)
                        for (int x = i * cell; x < Mathf.Min(w, (i + 1) * cell); x++)
                            if (inside[y * w + x]) { any = true; break; }
                    used[i, j] = any;
                }
            var vIndex = new int[nx + 1, ny + 1];
            for (int j = 0; j <= ny; j++) for (int i = 0; i <= nx; i++) vIndex[i, j] = -1;
            Func<int, int, int> vertex = (i, j) =>
            {
                if (vIndex[i, j] >= 0) return vIndex[i, j];
                int tx = Mathf.Min(i * cell, w), ty = Mathf.Min(j * cell, h);
                float hh = 0.25f * (texelH(tx - 1, ty - 1) + texelH(tx, ty - 1) + texelH(tx - 1, ty) + texelH(tx, ty));
                float x = (tx - ox) * unit; if (flipX) x = -x;
                float y = (ty - oy) * unit;
                var p = new Vector3(x, y, -hh);
                var uv = new Vector2(uvRect.xMin + uvRect.width * tx / w, uvRect.yMin + uvRect.height * ty / h);
                float ao = depth > 1e-5f ? Mathf.Lerp(0.8f, 1f, hh / depth) : 1f;
                int id = b.Add(p, Vector3.back, uv, DioramaMesh.Ao(ao, 1f));
                vIndex[i, j] = id;
                return id;
            };
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    if (!used[i, j]) continue;
                    int a = vertex(i, j), c1 = vertex(i, j + 1), c2 = vertex(i + 1, j + 1), d = vertex(i + 1, j);
                    // −z から見て時計回り (左下→左上→右上→右下)。鏡にしたら並びも逆に
                    if (!flipX) b.Quad(a, c1, c2, d);
                    else b.Quad(a, d, c2, c1);
                }
            RecalcNormals(b);
            return b;
        }

        /// <summary>輪郭までの距離 (テクセル。3-4 の面取り距離変換を 3 で割った近似)。外のテクセルと枠の外は 0</summary>
        static float[] DistanceToOutside(bool[] inside, int w, int h)
        {
            const int Big = 1 << 20;
            var d = new int[w * h];
            for (int i = 0; i < d.Length; i++) d[i] = inside[i] ? Big : 0;
            Func<int, int, int> at = (x, y) => (x < 0 || y < 0 || x >= w || y >= h) ? 0 : d[y * w + x];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (d[i] == 0) continue;
                    int v = d[i];
                    v = Math.Min(v, at(x - 1, y) + 3); v = Math.Min(v, at(x, y - 1) + 3);
                    v = Math.Min(v, at(x - 1, y - 1) + 4); v = Math.Min(v, at(x + 1, y - 1) + 4);
                    d[i] = v;
                }
            for (int y = h - 1; y >= 0; y--)
                for (int x = w - 1; x >= 0; x--)
                {
                    int i = y * w + x;
                    if (d[i] == 0) continue;
                    int v = d[i];
                    v = Math.Min(v, at(x + 1, y) + 3); v = Math.Min(v, at(x, y + 1) + 3);
                    v = Math.Min(v, at(x + 1, y + 1) + 4); v = Math.Min(v, at(x - 1, y + 1) + 4);
                    d[i] = v;
                }
            var r = new float[w * h];
            for (int i = 0; i < r.Length; i++) r[i] = d[i] / 3f;
            return r;
        }

        /// <summary>頂点を共有した面の法線をならす (Mesh.RecalculateNormals と同じ考え。器の段階で済ませ、まとめた後も保つ)</summary>
        static void RecalcNormals(DioramaMeshBuilder b)
        {
            var acc = new Vector3[b.V.Count];
            for (int t = 0; t < b.T.Count; t += 3)
            {
                int i0 = b.T[t], i1 = b.T[t + 1], i2 = b.T[t + 2];
                var n = Vector3.Cross(b.V[i1] - b.V[i0], b.V[i2] - b.V[i0]);
                acc[i0] += n; acc[i1] += n; acc[i2] += n;
            }
            for (int i = 0; i < acc.Length; i++)
            {
                var n = acc[i];
                b.N[i] = n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.back;
            }
        }
    }
}
