// DioramaMesh.cs — 箱庭の3Dの形をコードで作る (2026-09-30 HD-2D 見本 P04。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-2)。
// Blender は使わない。作る形は4通り:
//  ① 面の平らな押し出し (段・崖の帯・段の角・柵・標・櫓の梁)。凹凸は輪郭 (平面の中の折れ線) にだけ入れ、法線方向のノイズは入れない
//  ② 低ポリの面取り多面体 (岩。側面・面取り・天面の3段で 8〜20面)。面ごとに平らな法線と平面の UV
//  ③ 円筒に展開した UV を持つ幹と根
//  ④ 平らな札 (草・小石・花) と、光る霧の面・光の筋
// 陰は頂点色で付ける (rgb = 凹みほど暗い AO)。頂点色の a は「天面の材質 (苔) を載せてよい割合」(1 = 載せる・0 = 載せない。空き地の土)。
// a を使うのは配列の材質 (段・岩・段の角) だけ。段の前の面・段の角の側面は 0 (苔は天面だけ)。メッシュの UV で貼る部品 (幹・根・梁・滑車・半立体・札・光) は a = 1。
// 三角形の向き: Unity は「外から見て時計回り」が表。cross(b−a, c−a) が外を向く順に並べる (FlatPoly が外向きの目安から自動でそろえる)。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.Game
{
    /// <summary>メッシュを組む器 (頂点・法線・UV・頂点色・三角形)</summary>
    public sealed class DioramaMeshBuilder
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Vector3> N = new List<Vector3>();
        public readonly List<Vector2> U = new List<Vector2>();
        public readonly List<Color32> C = new List<Color32>();
        public readonly List<int> T = new List<int>();

        public int VertexCount => V.Count;
        public int TriangleCount => T.Count / 3;

        public int Add(Vector3 p, Vector3 n, Vector2 uv, Color32 c)
        {
            V.Add(p); N.Add(n); U.Add(uv); C.Add(c);
            return V.Count - 1;
        }

        public void Tri(int a, int b, int c) { T.Add(a); T.Add(b); T.Add(c); }

        /// <summary>a→b→c→d の四角 (a,b,c と a,c,d の2枚)</summary>
        public void Quad(int a, int b, int c, int d) { Tri(a, b, c); Tri(a, c, d); }

        /// <summary>別の器を行列 m で写して足す (法線は回転だけ)</summary>
        public void Append(DioramaMeshBuilder o, Matrix4x4 m)
        {
            int baseIndex = V.Count;
            for (int i = 0; i < o.V.Count; i++)
            {
                V.Add(m.MultiplyPoint3x4(o.V[i]));
                N.Add(m.MultiplyVector(o.N[i]).normalized);
                U.Add(o.U[i]); C.Add(o.C[i]);
            }
            for (int i = 0; i < o.T.Count; i++) T.Add(o.T[i] + baseIndex);
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (V.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(V); m.SetNormals(N); m.SetUVs(0, U); m.SetColors(C);
            m.SetTriangles(T, 0, true);
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>形を作る関数の集まり。どれも決定的 (乱数は System.Random(seed)。UnityEngine.Random は使わない = det の撮影を乱さない)</summary>
    public static class DioramaMesh
    {
        /// <summary>1 unit あたりのテクセル (キャラと同じ粒 = 1ドット 0.04 unit)</summary>
        public const float TexelsPerUnit = 25f;
        /// <summary>タイル1枚のテクセル (既定 64 = 2.56 unit)。メッシュの UV は「タイル何枚ぶん」で持つ</summary>
        public static float TileTexels = 64f;

        static float UvScale => TexelsPerUnit / Mathf.Max(1f, TileTexels);

        /// <summary>Y 軸まわりに yawDeg 度まわす (Quaternion.Euler(0, yawDeg, 0) * v と同じ。Unity の外でも試せるよう三角関数で書く)</summary>
        public static Vector3 YawRotate(float yawDeg, Vector3 v)
        {
            float a = yawDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        public static Color32 Ao(float ao, float topMask = 1f)
        {
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(ao * 255f), 0, 255);
            return new Color32(g, g, g, (byte)Mathf.Clamp(Mathf.RoundToInt(topMask * 255f), 0, 255));
        }

        /// <summary>面の向きでいちばん近い軸を1つ選んで平面に投影した UV (タイル何枚ぶん)。座標はメッシュのローカル (道に沿わせて組んだ部品は道の座標)</summary>
        public static Vector2 PlanarUV(Vector3 p, Vector3 n)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            Vector2 uv;
            if (ay >= ax && ay >= az) uv = new Vector2(p.x, p.z);
            else if (ax >= az) uv = new Vector2(n.x >= 0f ? p.z : -p.z, p.y);
            else uv = new Vector2(n.z >= 0f ? -p.x : p.x, p.y);
            return uv * UvScale;
        }

        /// <summary>
        /// 平らな凸の多角形を1枚の面として足す (頂点は面ごとに持つ = 角が立つ)。outward = 外向きの目安 (これと逆なら並びを反転)。
        /// ao = 頂点ごとの頂点色 (null なら 1)
        /// </summary>
        public static void FlatPoly(DioramaMeshBuilder b, IList<Vector3> pts, Vector3 outward, Func<Vector3, Color32> ao = null)
        {
            int n = pts.Count;
            if (n < 3) return;
            Vector3 nrm = Vector3.zero;
            for (int i = 0; i < n; i++) nrm += Vector3.Cross(pts[i], pts[(i + 1) % n]);
            bool flip = Vector3.Dot(nrm, outward) < 0f;
            if (flip) nrm = -nrm;
            if (nrm.sqrMagnitude < 1e-12f) return;   // つぶれた面
            nrm.Normalize();
            int first = b.VertexCount;
            for (int k = 0; k < n; k++)
            {
                var p = pts[flip ? n - 1 - k : k];
                b.Add(p, nrm, PlanarUV(p, nrm), ao != null ? ao(p) : Ao(1f));
            }
            for (int k = 1; k < n - 1; k++) b.Tri(first, first + k, first + k + 1);
        }

        static void FlatTri(DioramaMeshBuilder b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 outward, Func<Vector3, Color32> ao)
        {
            FlatPoly(b, new[] { p0, p1, p2 }, outward, ao);
        }

        // ================================================================ ① 段 (面の平らな押し出し)

        /// <summary>
        /// 段 (崖の帯・台地) を世界の座標で作る。front = 手前の縁の折れ線 (道の座標 t,s。t の昇順)、sBack = 奥の縁 (s)。
        /// 天面は yTop の平ら (格子 grid で割って頂点色を持たせる)、前の面と両端の面は yBottom まで垂直。後ろと底は作らない (隠れる)。
        /// chamfer > 0 なら天面の手前の角を面取りする (前の面は yTop−chamfer まで、斜めの帯で天面へ)。
        /// heightAt(t,s) = 段の全体の高さ (天面の奥の段の根元を暗くする AO に使う)。topMask(t,s) = 天面の苔の割合
        /// </summary>
        public static DioramaMeshBuilder Slab(IList<Vector2> front, float sBack, float yTop, float yBottom, float grid, float chamfer,
            float pathYaw, Func<float, float, float> heightAt, Func<float, float, float> topMask)
        {
            var b = new DioramaMeshBuilder();
            int n = front.Count;
            if (n < 2) return b;
            Func<float, float, float, Vector3> W = (t, s, y) => YawRotate(pathYaw, new Vector3(t, y, s));
            float c = Mathf.Max(0f, Mathf.Min(chamfer, (yTop - yBottom) * 0.45f));
            grid = Mathf.Max(0.16f, grid);

            // 天面の格子: 行 = 縁の点 (t)、列 = 縁 (+面取り) から奥までを同じ数に割る (行どうしの四角がつながる)
            float minFront = float.MaxValue;
            for (int i = 0; i < n; i++) minFront = Mathf.Min(minFront, front[i].y);
            int cols = Mathf.Max(1, Mathf.CeilToInt((sBack - minFront - c) / grid));
            var idx = new int[n, cols + 1];
            for (int i = 0; i < n; i++)
            {
                float t = front[i].x, s0 = front[i].y + c;
                for (int j = 0; j <= cols; j++)
                {
                    float s = Mathf.Lerp(s0, sBack, j / (float)cols);
                    // 奥の段の根元ほど暗い (0.7 unit 奥の地面が自分より高い = 崖の足元)
                    float rise = heightAt != null ? heightAt(t, s + 0.7f) - yTop : 0f;
                    float ao = 1f - 0.32f * Mathf.Clamp01(rise / 1.0f);
                    if (j == 0) ao *= 0.94f;   // 縁 (面取りの上) はわずかに沈める
                    float mask = topMask != null ? topMask(t, s) : 1f;
                    var p = W(t, s, yTop);
                    idx[i, j] = b.Add(p, Vector3.up, new Vector2(t, s) * UvScale, Ao(ao, mask));
                }
            }
            for (int i = 0; i < n - 1; i++)
                for (int j = 0; j < cols; j++)
                {
                    int a0 = idx[i, j], a1 = idx[i, j + 1], b1 = idx[i + 1, j + 1], b0 = idx[i + 1, j];
                    // 上から見て時計回り = 法線が上
                    if (Vector3.Dot(Vector3.Cross(b.V[a1] - b.V[a0], b.V[b1] - b.V[a0]), Vector3.up) >= 0f) b.Quad(a0, a1, b1, b0);
                    else b.Quad(a0, b0, b1, a1);
                }

            // 前の面 (折れ線の1区間 = 1枚の平らな面) と面取りの帯
            float yFaceTop = yTop - c;
            for (int i = 0; i < n - 1; i++)
            {
                var f0 = front[i]; var f1 = front[i + 1];
                var seg = f1 - f0;
                if (seg.sqrMagnitude < 1e-8f) continue;
                var outwardTS = new Vector2(seg.y, -seg.x).normalized;   // s の小さい側 (手前)
                var outward = YawRotate(pathYaw, new Vector3(outwardTS.x, 0f, outwardTS.y));
                float faceH = Mathf.Max(0.01f, yFaceTop - yBottom);
                Func<Vector3, Color32> faceAo = p => Ao(0.56f + 0.44f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p.y - yBottom) / Mathf.Min(faceH, 1.4f))), 0f);
                FlatPoly(b, new[] { W(f0.x, f0.y, yBottom), W(f0.x, f0.y, yFaceTop), W(f1.x, f1.y, yFaceTop), W(f1.x, f1.y, yBottom) }, outward, faceAo);
                if (c > 0f)
                {
                    var tilt = (outward + Vector3.up).normalized;
                    FlatPoly(b, new[] { W(f0.x, f0.y, yFaceTop), W(f0.x, f0.y + c, yTop), W(f1.x, f1.y + c, yTop), W(f1.x, f1.y, yFaceTop) }, tilt, p => Ao(0.97f, 1f));
                }
            }

            // 両端の面 (t の始まりと終わり)
            for (int e = 0; e < 2; e++)
            {
                var f = e == 0 ? front[0] : front[n - 1];
                var outward = YawRotate(pathYaw, new Vector3(e == 0 ? -1f : 1f, 0f, 0f));
                var pts = new List<Vector3> { W(f.x, f.y, yBottom), W(f.x, f.y, yFaceTop) };
                if (c > 0f) pts.Add(W(f.x, f.y + c, yTop));
                pts.Add(W(f.x, sBack, yTop)); pts.Add(W(f.x, sBack, yBottom));
                FlatPoly(b, pts, outward, p => Ao(0.6f + 0.4f * Mathf.Clamp01((p.y - yBottom) / 1.2f), 0f));
            }
            return b;
        }

        // ================================================================ ① 段の角・岩棚 (押し出した多角形)

        /// <summary>
        /// 平面の凸の多角形 (ローカル x,z。どちら回りでもよい) を高さ h に押し出した塊。上の角を chamfer だけ面取りする。
        /// 置く時は道の向きに回す (部品の Transform)
        /// </summary>
        public static DioramaMeshBuilder Block(IList<Vector2> footprint, float h, float chamfer, float sink)
        {
            var b = new DioramaMeshBuilder();
            int n = footprint.Count;
            if (n < 3) return b;
            var ctr = Vector2.zero;
            foreach (var p in footprint) ctr += p;
            ctr /= n;
            float c = Mathf.Clamp(chamfer, 0f, h * 0.45f);
            float y0 = -sink, y1 = h - c, y2 = h;
            var top = new Vector3[n];
            var mid = new Vector3[n];
            var bot = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var p = footprint[i];
                var inset = p + (ctr - p).normalized * c;
                bot[i] = new Vector3(p.x, y0, p.y);
                mid[i] = new Vector3(p.x, y1, p.y);
                top[i] = new Vector3(inset.x, y2, inset.y);
            }
            Func<Vector3, Color32> sideAo = p => Ao(0.55f + 0.45f * Mathf.Clamp01((p.y - y0) / Mathf.Max(0.01f, y2 - y0)), 0f);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                var em = (mid[i] + mid[j]) * 0.5f;
                var outward = new Vector3(em.x - ctr.x, 0f, em.z - ctr.y);
                FlatPoly(b, new[] { bot[i], mid[i], mid[j], bot[j] }, outward, sideAo);
                if (c > 0f) FlatPoly(b, new[] { mid[i], top[i], top[j], mid[j] }, (outward.normalized + Vector3.up).normalized, p => Ao(0.96f, 1f));
            }
            FlatPoly(b, top, Vector3.up, p => Ao(1f, 1f));
            return b;
        }

        /// <summary>角を落とした長方形 (w×d。角の切り込みは seed で揺らす。格子 snap に合わせる)。段の角・岩棚の足場用</summary>
        public static List<Vector2> ChamferedRect(float w, float d, float cut, int seed, float snap)
        {
            var r = new System.Random(seed);
            float Cut() => Snap(cut * (0.5f + (float)r.NextDouble()), snap);
            float hw = w * 0.5f, hd = d * 0.5f;
            float c0 = Cut(), c1 = Cut(), c2 = Cut(), c3 = Cut();
            return new List<Vector2>
            {
                new Vector2(-hw + c0, -hd), new Vector2(hw - c1, -hd), new Vector2(hw, -hd + c1), new Vector2(hw, hd - c2),
                new Vector2(hw - c2, hd), new Vector2(-hw + c3, hd), new Vector2(-hw, hd - c3), new Vector2(-hw, -hd + c0),
            };
        }

        public static float Snap(float v, float step) { return step > 0f ? Mathf.Round(v / step) * step : v; }

        // ================================================================ ② 岩 (低ポリの面取り多面体)

        /// <summary>
        /// 岩: 根元の輪 (少し埋める)・中の輪・天面の輪 (内へ寄せた面取り) の3段。sides 本の輪 (5〜8)。面ごとに平らな法線。
        /// radius = 根元の半径、height = 高さ。頂点色は根元ほど暗く、天面は苔を載せる (a=1)
        /// </summary>
        public static DioramaMeshBuilder Rock(int seed, float radius, float height, int sides, float squash)
        {
            var r = new System.Random(seed);
            float Rnd(float a, float b) => a + (b - a) * (float)r.NextDouble();
            sides = Mathf.Clamp(sides, 4, 9);
            var b = new DioramaMeshBuilder();
            var bot = new Vector3[sides]; var mid = new Vector3[sides]; var top = new Vector3[sides];
            float a0 = Rnd(0f, Mathf.PI * 2f);
            float zs = Mathf.Clamp(squash, 0.4f, 1.6f);   // 奥行きの縮み (横長の岩)
            for (int i = 0; i < sides; i++)
            {
                float a = a0 + (i + Rnd(-0.28f, 0.28f)) * Mathf.PI * 2f / sides;
                float rb = radius * Rnd(0.82f, 1.12f);
                float rm = rb * Rnd(0.86f, 1.0f);
                float rt = rb * Rnd(0.42f, 0.62f);
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a) * zs);
                bot[i] = dir * rb + new Vector3(0f, -height * 0.12f, 0f);
                mid[i] = dir * rm + new Vector3(0f, height * Rnd(0.5f, 0.72f), 0f);
                top[i] = dir * rt + new Vector3(0f, height * Rnd(0.9f, 1.05f), 0f);
            }
            float y0 = -height * 0.12f;
            Func<Vector3, Color32> ao = p =>
            {
                float k = Mathf.Clamp01((p.y - y0) / Mathf.Max(0.01f, height * 1.05f - y0));
                return Ao(0.52f + 0.48f * Mathf.Sqrt(k), 1f);
            };
            var center = new Vector3(0f, height * 0.45f, 0f);
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                // 四角は平らとは限らないので三角2枚 (それぞれ平ら = 面取りの切子に見える)
                FlatTri(b, bot[i], mid[i], mid[j], ((bot[i] + mid[i] + mid[j]) / 3f - center), ao);
                FlatTri(b, bot[i], mid[j], bot[j], ((bot[i] + mid[j] + bot[j]) / 3f - center), ao);
                FlatTri(b, mid[i], top[i], top[j], ((mid[i] + top[i] + top[j]) / 3f - center), ao);
                FlatTri(b, mid[i], top[j], mid[j], ((mid[i] + top[j] + mid[j]) / 3f - center), ao);
            }
            // 天面: 中心を少し持ち上げた扇 (1枚の平らな面にはならないので切子になる)
            var apex = Vector3.zero;
            foreach (var p in top) apex += p;
            apex /= sides; apex.y += height * 0.04f;
            for (int i = 0; i < sides; i++) FlatTri(b, top[i], apex, top[(i + 1) % sides], Vector3.up, ao);
            return b;
        }

        // ================================================================ ③ 幹と根 (円筒の UV)

        /// <summary>
        /// 大樹の幹: 輪を 0.5 unit ごとに積んだ筒。根元は flare だけ膨らみ、上へ細る。lean で上ほど横へ曲がる (ローカル +x)。
        /// UV は円筒に展開 (u = 周の何タイルぶん・v = 高さ)。周のタイル数は整数に丸める (継ぎ目で模様が切れない)
        /// </summary>
        public static DioramaMeshBuilder Trunk(int seed, float height, float radius, float lean, float flare, int segs)
        {
            var b = new DioramaMeshBuilder();
            var r = new System.Random(seed);
            segs = Mathf.Clamp(segs, 6, 16);
            int rings = Mathf.Max(3, Mathf.CeilToInt(height / 0.5f));
            float circ = Mathf.PI * 2f * radius;
            float uMax = Mathf.Max(1f, Mathf.Round(circ * UvScale));
            float wob = (float)r.NextDouble() * 10f;
            var ringIdx = new int[rings + 1, segs + 1];
            for (int k = 0; k <= rings; k++)
            {
                float y = height * k / rings;
                float rr = RadiusAt(radius, flare, height, y) * (1f + 0.04f * Mathf.Sin(y * 1.7f + wob));
                float cx = lean * y * y / Mathf.Max(1f, height);
                float ao = 0.6f + 0.4f * Mathf.Clamp01(y / 1.6f);
                for (int i = 0; i <= segs; i++)
                {
                    float a = i * Mathf.PI * 2f / segs;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    var p = new Vector3(cx, y, 0f) + dir * rr;
                    ringIdx[k, i] = b.Add(p, dir, new Vector2(uMax * i / segs, y * UvScale), Ao(ao, 1f));
                }
            }
            for (int k = 0; k < rings; k++)
                for (int i = 0; i < segs; i++)
                {
                    int a0 = ringIdx[k, i], a1 = ringIdx[k + 1, i], b1 = ringIdx[k + 1, i + 1], b0 = ringIdx[k, i + 1];
                    OrientedQuad(b, a0, a1, b1, b0, b.N[a0]);
                }
            // 天の蓋 (画面の外へ出ることが多いが、低い幹で穴が見えないように)
            int capCenter = b.Add(new Vector3(lean * height, height, 0f), Vector3.up, new Vector2(0.5f, 0.5f), Ao(0.9f, 1f));
            for (int i = 0; i < segs; i++)
            {
                int a = ringIdx[rings, i], c2 = ringIdx[rings, i + 1];
                OrientedTri(b, capCenter, a, c2, Vector3.up);
            }
            return b;
        }

        static float RadiusAt(float radius, float flare, float height, float y)
        {
            return radius * (1f + flare * Mathf.Exp(-y / 0.45f)) * (1f - 0.3f * Mathf.Clamp01(y / Mathf.Max(1f, height)));
        }

        /// <summary>
        /// 根: 幹の根元 (半径 trunkRadius・角度 angleDeg) から外へ伸び、地面 (y=0) へ潜る管。2次のベジエに沿って細る。UV は円筒に展開
        /// </summary>
        public static void Root(DioramaMeshBuilder b, float trunkRadius, float angleDeg, float length, float radius, int seed)
        {
            var r = new System.Random(seed);
            float a = angleDeg * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var p0 = dir * (trunkRadius * 0.7f) + Vector3.up * (0.3f + 0.25f * (float)r.NextDouble());
            var p2 = dir * (trunkRadius + length) + Vector3.down * 0.08f;
            var p1 = dir * (trunkRadius + length * 0.35f) + Vector3.up * 0.28f;
            const int rings = 8, segs = 6;
            var idx = new int[rings + 1, segs + 1];
            float uMax = Mathf.Max(1f, Mathf.Round(Mathf.PI * 2f * radius * UvScale));
            float vAcc = 0f; Vector3 prev = p0;
            for (int k = 0; k <= rings; k++)
            {
                float t = k / (float)rings;
                var c = (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
                var tan = (2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1)).normalized;
                vAcc += (c - prev).magnitude; prev = c;
                var side = Vector3.Cross(Vector3.up, tan).normalized;
                if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
                var up2 = Vector3.Cross(tan, side).normalized;
                float rr = radius * Mathf.Lerp(1f, 0.25f, t);
                for (int i = 0; i <= segs; i++)
                {
                    float ang = i * Mathf.PI * 2f / segs;
                    var nrm = side * Mathf.Cos(ang) + up2 * Mathf.Sin(ang);
                    idx[k, i] = b.Add(c + nrm * rr, nrm, new Vector2(uMax * i / segs, vAcc * UvScale), Ao(0.62f + 0.3f * Mathf.Clamp01(c.y / 0.4f), 1f));
                }
            }
            for (int k = 0; k < rings; k++)
                for (int i = 0; i < segs; i++)
                    OrientedQuad(b, idx[k, i], idx[k + 1, i], idx[k + 1, i + 1], idx[k, i + 1], b.N[idx[k, i]]);
        }

        /// <summary>四角を「nrmHint を向く」並びで足す</summary>
        static void OrientedQuad(DioramaMeshBuilder b, int a, int c1, int c2, int d, Vector3 nrmHint)
        {
            var n = Vector3.Cross(b.V[c1] - b.V[a], b.V[c2] - b.V[a]);
            if (Vector3.Dot(n, nrmHint) >= 0f) b.Quad(a, c1, c2, d);
            else b.Quad(a, d, c2, c1);
        }

        static void OrientedTri(DioramaMeshBuilder b, int a, int c1, int c2, Vector3 nrmHint)
        {
            var n = Vector3.Cross(b.V[c1] - b.V[a], b.V[c2] - b.V[a]);
            if (Vector3.Dot(n, nrmHint) >= 0f) b.Tri(a, c1, c2);
            else b.Tri(a, c2, c1);
        }

        // ================================================================ ① 梁 (向きのある箱)・櫓・柵・標

        /// <summary>a から c へ伸びる梁 (断面 w×d の箱)。面ごとに平ら</summary>
        public static void Beam(DioramaMeshBuilder b, Vector3 a, Vector3 c, float w, float d, float ao)
        {
            var axis = c - a;
            float len = axis.magnitude;
            if (len < 1e-4f) return;
            var dir = axis / len;
            var side = Vector3.Cross(Vector3.up, dir);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(Vector3.forward, dir);
            side.Normalize();
            var up2 = Vector3.Cross(dir, side).normalized;
            var sw = side * (w * 0.5f); var ud = up2 * (d * 0.5f);
            Vector3 A(float x, float y) => a + sw * x + ud * y;
            Vector3 Cc(float x, float y) => c + sw * x + ud * y;
            float lo = Mathf.Min(a.y, c.y), hi = Mathf.Max(a.y, c.y) + 0.001f;
            Func<Vector3, Color32> col = p => Ao(ao * (0.72f + 0.28f * Mathf.Clamp01((p.y - lo) / Mathf.Max(0.3f, hi - lo))), 1f);
            FlatPoly(b, new[] { A(1, -1), A(1, 1), Cc(1, 1), Cc(1, -1) }, side, col);
            FlatPoly(b, new[] { A(-1, -1), A(-1, 1), Cc(-1, 1), Cc(-1, -1) }, -side, col);
            FlatPoly(b, new[] { A(-1, 1), A(1, 1), Cc(1, 1), Cc(-1, 1) }, up2, col);
            FlatPoly(b, new[] { A(-1, -1), A(1, -1), Cc(1, -1), Cc(-1, -1) }, -up2, col);
            FlatPoly(b, new[] { A(-1, -1), A(1, -1), A(1, 1), A(-1, 1) }, -dir, col);
            FlatPoly(b, new[] { Cc(-1, -1), Cc(1, -1), Cc(1, 1), Cc(-1, 1) }, dir, col);
        }

        /// <summary>
        /// 坑口の櫓: 4本の脚 (上へすぼまる)・2段の胴縁・前後の筋交い・天の台・滑車の支柱。ローカルは道に沿う (x = 道の向き・z = 奥)。
        /// 滑車 (輪) は別の器 wheel に作る (回すので静的なまとめに入れない)。wheelCenter = 輪の中心 (ローカル)
        /// </summary>
        public static DioramaMeshBuilder Rig(float width, float depth, float height, float wheelRadius, out DioramaMeshBuilder wheel, out Vector3 wheelCenter)
        {
            var b = new DioramaMeshBuilder();
            float hw = width * 0.5f, hd = depth * 0.5f, tw = width * 0.22f, td = depth * 0.28f;
            const float leg = 0.16f, brace = 0.1f, thin = 0.07f;
            var baseC = new[] { new Vector3(-hw, 0f, -hd), new Vector3(hw, 0f, -hd), new Vector3(hw, 0f, hd), new Vector3(-hw, 0f, hd) };
            var topC = new[] { new Vector3(-tw, height, -td), new Vector3(tw, height, -td), new Vector3(tw, height, td), new Vector3(-tw, height, td) };
            for (int i = 0; i < 4; i++) Beam(b, baseC[i] + Vector3.down * 0.1f, topC[i], leg, leg, 0.9f);
            Vector3 At(int i, float k) => Vector3.Lerp(baseC[i], topC[i], k);
            foreach (var k in new[] { 0.36f, 0.7f })
                for (int i = 0; i < 4; i++) Beam(b, At(i, k), At((i + 1) % 4, k), brace, brace, 0.92f);
            // 前後の筋交い (X)
            foreach (var face in new[] { new[] { 0, 1 }, new[] { 3, 2 } })
            {
                Beam(b, At(face[0], 0.02f), At(face[1], 0.36f), thin, thin, 0.85f);
                Beam(b, At(face[1], 0.02f), At(face[0], 0.36f), thin, thin, 0.85f);
                Beam(b, At(face[0], 0.36f), At(face[1], 0.7f), thin, thin, 0.88f);
                Beam(b, At(face[1], 0.36f), At(face[0], 0.7f), thin, thin, 0.88f);
            }
            // 天の台
            Beam(b, new Vector3(-tw - 0.18f, height + 0.07f, 0f), new Vector3(tw + 0.18f, height + 0.07f, 0f), td * 2f + 0.3f, 0.14f, 0.95f);
            // 滑車の支柱と軸受
            float postH = wheelRadius + 0.22f;
            foreach (var z in new[] { -0.2f, 0.2f })
                Beam(b, new Vector3(0f, height + 0.14f, z), new Vector3(0f, height + 0.14f + postH, z), 0.1f, 0.1f, 0.95f);
            wheelCenter = new Vector3(0f, height + 0.14f + postH, 0f);
            // 坑口の枠 (足元の井桁)
            Beam(b, new Vector3(-hw * 0.6f, 0.04f, -hd * 0.6f), new Vector3(hw * 0.6f, 0.04f, -hd * 0.6f), 0.14f, 0.1f, 0.7f);
            Beam(b, new Vector3(-hw * 0.6f, 0.04f, hd * 0.6f), new Vector3(hw * 0.6f, 0.04f, hd * 0.6f), 0.14f, 0.1f, 0.7f);
            Beam(b, new Vector3(-hw * 0.6f, 0.1f, -hd * 0.6f), new Vector3(-hw * 0.6f, 0.1f, hd * 0.6f), 0.14f, 0.1f, 0.7f);
            Beam(b, new Vector3(hw * 0.6f, 0.1f, -hd * 0.6f), new Vector3(hw * 0.6f, 0.1f, hd * 0.6f), 0.14f, 0.1f, 0.7f);
            // 綱 (滑車から坑口へ)
            Beam(b, wheelCenter + new Vector3(-wheelRadius * 0.9f, 0f, 0f), new Vector3(-0.05f, 0.1f, 0f), 0.035f, 0.035f, 0.6f);
            wheel = Wheel(wheelRadius, 0.12f, 6);
            return b;
        }

        /// <summary>滑車の輪: 中心が原点、軸はローカル z。縁 (多角形の輪)・輻・轂。UV は面ごとの平面 (回っても模様が一緒に回る)</summary>
        public static DioramaMeshBuilder Wheel(float radius, float thick, int spokes)
        {
            var b = new DioramaMeshBuilder();
            const int segs = 14;
            float ri = radius * 0.8f, hz = thick * 0.5f;
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * Mathf.PI * 2f / segs, a1 = (i + 1) * Mathf.PI * 2f / segs;
                var o0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f); var o1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                var mid = (o0 + o1).normalized;
                FlatPoly(b, new[] { o0 * radius + Vector3.back * hz, o1 * radius + Vector3.back * hz, o1 * radius + Vector3.forward * hz, o0 * radius + Vector3.forward * hz }, mid, p => Ao(0.85f, 1f));
                FlatPoly(b, new[] { o0 * ri + Vector3.back * hz, o1 * ri + Vector3.back * hz, o1 * radius + Vector3.back * hz, o0 * radius + Vector3.back * hz }, Vector3.back, p => Ao(0.95f, 1f));
                FlatPoly(b, new[] { o0 * ri + Vector3.forward * hz, o1 * ri + Vector3.forward * hz, o1 * radius + Vector3.forward * hz, o0 * radius + Vector3.forward * hz }, Vector3.forward, p => Ao(0.8f, 1f));
                FlatPoly(b, new[] { o0 * ri + Vector3.back * hz, o1 * ri + Vector3.back * hz, o1 * ri + Vector3.forward * hz, o0 * ri + Vector3.forward * hz }, -mid, p => Ao(0.7f, 1f));
            }
            for (int i = 0; i < spokes; i++)
            {
                float a = i * Mathf.PI * 2f / spokes;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Beam(b, d * 0.08f, d * (ri + 0.02f), 0.06f, 0.06f, 0.9f);
            }
            Beam(b, Vector3.back * (hz + 0.05f), Vector3.forward * (hz + 0.05f), 0.16f, 0.16f, 0.8f);
            return b;
        }

        /// <summary>柵: 長さ length (ローカル x) に posts 本の杭と2本の横木。杭は少し傾ける (seed)</summary>
        public static DioramaMeshBuilder Fence(float length, float height, int posts, int seed)
        {
            var b = new DioramaMeshBuilder();
            var r = new System.Random(seed);
            posts = Mathf.Max(2, posts);
            var tops = new List<Vector3>();
            for (int i = 0; i < posts; i++)
            {
                float x = -length * 0.5f + length * i / (posts - 1);
                float lean = ((float)r.NextDouble() - 0.5f) * 0.12f;
                float hh = height * (0.9f + 0.18f * (float)r.NextDouble());
                var top = new Vector3(x + lean, hh, lean * 0.5f);
                Beam(b, new Vector3(x, -0.12f, 0f), top, 0.12f, 0.12f, 0.9f);
                tops.Add(top);
            }
            foreach (var k in new[] { 0.45f, 0.82f })
                for (int i = 0; i < posts - 1; i++)
                    Beam(b, new Vector3(tops[i].x, tops[i].y * k, -0.07f), new Vector3(tops[i + 1].x, tops[i + 1].y * k + (float)(r.NextDouble() - 0.5) * 0.08f, -0.07f), 0.07f, 0.09f, 0.95f);
            return b;
        }

        /// <summary>標: 杭と、道の向きに打った板 (行き先の札)</summary>
        public static DioramaMeshBuilder Marker(float height, int seed)
        {
            var b = new DioramaMeshBuilder();
            var r = new System.Random(seed);
            float tilt = ((float)r.NextDouble() - 0.5f) * 0.1f;
            Beam(b, new Vector3(0f, -0.15f, 0f), new Vector3(tilt, height, 0f), 0.14f, 0.14f, 0.9f);
            Beam(b, new Vector3(-0.1f + tilt * 0.8f, height * 0.78f, -0.09f), new Vector3(0.62f + tilt * 0.8f, height * 0.74f, -0.09f), 0.05f, 0.24f, 1f);
            return b;
        }

        // ================================================================ ④ 平らな札・光の面

        /// <summary>
        /// 平らな札 (草・小石・花): 幅 w・高さ h、足元の中心が原点、表はローカル −z (カメラの方)。uv = アトラスの中の四角。
        /// 法線は少し上へ倒す (地面と同じように上からの光を受ける)。頂点色は足元ほど暗い
        /// </summary>
        public static DioramaMeshBuilder Card(float w, float h, Rect uv, bool flipX)
        {
            var b = new DioramaMeshBuilder();
            var n = new Vector3(0f, 0.55f, -1f).normalized;
            float u0 = flipX ? uv.xMax : uv.xMin, u1 = flipX ? uv.xMin : uv.xMax;
            int a = b.Add(new Vector3(-w * 0.5f, 0f, 0f), n, new Vector2(u0, uv.yMin), Ao(0.7f, 1f));
            int c1 = b.Add(new Vector3(-w * 0.5f, h, 0f), n, new Vector2(u0, uv.yMax), Ao(1f, 1f));
            int c2 = b.Add(new Vector3(w * 0.5f, h, 0f), n, new Vector2(u1, uv.yMax), Ao(1f, 1f));
            int d = b.Add(new Vector3(w * 0.5f, 0f, 0f), n, new Vector2(u1, uv.yMin), Ao(0.7f, 1f));
            b.Quad(a, c1, c2, d);   // −z から見て時計回り
            return b;
        }

        /// <summary>光る霧の面: 幅 w・高さ h の縦の板 (足元の中心が原点・表は −z)。uv = 光の絵の中の四角</summary>
        public static DioramaMeshBuilder GlowQuad(float w, float h, Rect uv)
        {
            var b = new DioramaMeshBuilder();
            var n = Vector3.back;
            int a = b.Add(new Vector3(-w * 0.5f, 0f, 0f), n, new Vector2(uv.xMin, uv.yMin), Ao(1f, 1f));
            int c1 = b.Add(new Vector3(-w * 0.5f, h, 0f), n, new Vector2(uv.xMin, uv.yMax), Ao(1f, 1f));
            int c2 = b.Add(new Vector3(w * 0.5f, h, 0f), n, new Vector2(uv.xMax, uv.yMax), Ao(1f, 1f));
            int d = b.Add(new Vector3(w * 0.5f, 0f, 0f), n, new Vector2(uv.xMax, uv.yMin), Ao(1f, 1f));
            b.Quad(a, c1, c2, d);
            return b;
        }

        /// <summary>
        /// 光の筋: 原点 (光の出口・上) からローカル −y へ length だけ伸びる4面の角柱 (上 topW・下 bottomW)。蓋は無い。
        /// 置く時に光の向きへ回す。uv の v は出口 = 上端 (yMax)・先 = 下端 (yMin)
        /// </summary>
        public static DioramaMeshBuilder Shaft(float topW, float bottomW, float length, Rect uv)
        {
            var b = new DioramaMeshBuilder();
            float ht = topW * 0.5f, hb = bottomW * 0.5f;
            var tp = new[] { new Vector3(-ht, 0f, -ht), new Vector3(ht, 0f, -ht), new Vector3(ht, 0f, ht), new Vector3(-ht, 0f, ht) };
            var bt = new[] { new Vector3(-hb, -length, -hb), new Vector3(hb, -length, -hb), new Vector3(hb, -length, hb), new Vector3(-hb, -length, hb) };
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                var mid = (tp[i] + tp[j] + bt[i] + bt[j]) * 0.25f;
                var outward = new Vector3(mid.x, 0f, mid.z).normalized;
                int a = b.Add(bt[i], outward, new Vector2(uv.xMin, uv.yMin), Ao(1f, 1f));
                int c1 = b.Add(tp[i], outward, new Vector2(uv.xMin, uv.yMax), Ao(1f, 1f));
                int c2 = b.Add(tp[j], outward, new Vector2(uv.xMax, uv.yMax), Ao(1f, 1f));
                int d = b.Add(bt[j], outward, new Vector2(uv.xMax, uv.yMin), Ao(1f, 1f));
                OrientedQuad(b, a, c1, c2, d, outward);
            }
            return b;
        }
    }
}
