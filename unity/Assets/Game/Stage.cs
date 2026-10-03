// Stage.cs — HD-2D の舞台 (2026-09-07)。オクトラの構造そのもの: 舞台だけ本物の3D (ドット絵テクスチャの箱庭・
// 俯瞰の透視カメラ・遠景だけのボケ・月光と接地影・霧=空気遠近・粒子)、キャラは2Dドットのビルボード。紙の UI は Overlay のまま。
//
// 外部レビュー (2026-09-07・3回) の処方:
//  第1回 粒の統一 / 見下ろし / 光 / 段階的なボケ / 地面の描き込み
//  第2回 「カメラだけ」= 俯瞰 28°・道と崖を斜めに・隊列を対角線に・名前札は同じ線
//  第3回 接地影 (全オブジェクト) → 月光の方向と街灯のコントラスト → 粒 (仮の敵絵を64ドットの描き込みに・小物のテクスチャを2倍密度に)
//
// 座標: 地面 y=0、道は PathYaw で手前左→奥右、1 unit = 基準深度で画面 100px (1080p)。キャラの板は「その座席の深度の面 (視線に垂直)」に
// UI の矩形を写すので、画面上の大きさは UI の矩形どおり (1ドット=4px) のまま、足元はその座席の地面に着く。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        const float PathYaw = -22f;                     // 道の向き (手前左 → 奥右)。隊列もこの線に沿う
        const float Tile = 1.28f;                        // タイル1枚 = 1.28 units。幕1は 64 ドット (基準深度で 2px/ドット = 地面だけ細かい。2026-09-21 HD-2D 裁定)、幕2/3 は 32 ドット (4px/ドット)
        const float Dot = 0.04f;                         // 立て板 (木・茂み) の 1 ドット = 基準深度で 4px = 0.04 units (キャラと同じ粒)
        const float GroundDot = 0.02f;                   // 地面の敷物の 1 ドット = 2px (地面のタイルと同じ粒)
        static readonly Color ShadowColor = new Color(0.02f, 0.02f, 0.08f, 0.92f);   // 接地影: 地面より暗く青寄り。幅0.8・高さ0.35・足元中心・地面とスプライトの間

        static Transform _world, _fx, _units;
        static Light _sun, _lantern;
        static int _paintedAct = -1;
        static Shader _unitShader;
        static Material _dioramaBase, _cutoutBase;
        static Mesh _quad, _cross, _quadCentered;
        static Texture2D _blobTex, _stripTex;
        static Pal _pal;
        static Vector3 _lampPos;
        static Material _waterMat;
        // 箱庭 (HD-2D 見本 P12・2026-09-30): 幕1 × stage=diorama の時だけ、今の舞台 (Paint の old の道筋) の代わりに Diorama (P04) と StageLook (P09) で組む
        static bool _diorama;                   // いま舞台が箱庭 (GroundY は Diorama.GroundY を読む・SetFxForAct は水の粒を止める)
        /// <summary>いま舞台が箱庭か (Paint の後に読む。組むのに失敗して今の舞台で描いた時は false)。戦闘以外の画面が暗がりを足すかを決める (BattleScreen.MenuShade)</summary>
        public static bool ShowingDiorama { get { return _diorama; } }
        static string _paintedSig = "";         // 描いた舞台の組み方 ("old" か "d|幕|幹|設計図|段")。同じ幕でも組み方が変われば描き直す (old と diorama を交互に撮っても混ざらない)
        static bool _dioramaFlagsHooked;        // HD2DFlags.Changed を1回だけ購読した (aa の切り替えで半立体の alpha-to-coverage を当て直す)
        static float _dioramaBuildMs;           // 最後に箱庭を組んだ時間 (ミリ秒。dumplayout と記録)

        // ---------------------------------------------------------------- 座席 (舞台が配置を決める)

        /// <summary>道の上の点 (t = 道に沿った距離・s = 道と直角の横ずれ。s>0 は奥側) を world へ。高さは段丘の高さ場に合わせる</summary>
        static Vector3 OnPath(float t, float s)
        {
            var p = Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(t, 0f, s);
            p.y = GroundY(p.x, p.z);
            return p;
        }

        static void PathLocal(float x, float z, out float t, out float s)
        {
            var l = Quaternion.Euler(0f, -PathYaw, 0f) * new Vector3(x, 0f, z);
            t = l.x; s = l.z;
        }

        // ---------------------------------------------------------------- 段丘の高さ場 (2026-09-08 ユーザー「地形が単調。本家を見習って」)
        // 本家の戦場は起伏を段に切った段丘 (草の天面と土の崖面・縁は不定形)。ノイズの起伏を Step 刻みに量子化して段にし、
        // 隣のセルとの高低差に崖面を張る。戦闘の場 (道の座標 t≈2,s≈0.5 の楕円) は平らに均し、土の空き地は不定形の塊にする。
        const float Cell = 0.64f, Step = 1.1f;          // セル = タイルの半分 (2026-09-21: 材質の 4 種を 2×2 のアトラスに束ね、タイル単位で種と向きを選ぶのでセルはタイルに揃える)
        const float TX0 = -56f, TZ0 = -24f;
        const int TNX = 175, TNZ = 157;
        static float[,] _H;
        static bool _flat;                      // 幕2/3 = 坑の中の平らな床 (起伏・川・土の道なし)
        static bool[,] _Dirt, _Bed;
        static int _tseed = 1;

        static float Hash01(int x, int z)
        {
            uint h = (uint)(x * 374761393 + z * 668265263 + _tseed * 1013904223);
            h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 0xffffff) / 16777216f;
        }
        static float Vnoise(float x, float z)
        {
            int xi = Mathf.FloorToInt(x), zi = Mathf.FloorToInt(z);
            float fx = x - xi, fz = z - zi;
            fx = fx * fx * (3f - 2f * fx); fz = fz * fz * (3f - 2f * fz);
            float a = Hash01(xi, zi), b = Hash01(xi + 1, zi), c = Hash01(xi, zi + 1), d = Hash01(xi + 1, zi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
        }
        static float Fbm(float x, float z)
        {
            return 0.6f * Vnoise(x * 0.045f, z * 0.045f) + 0.28f * Vnoise(x * 0.1f + 31f, z * 0.1f + 17f) + 0.12f * Vnoise(x * 0.2f + 7f, z * 0.2f + 3f);
        }
        /// <summary>戦闘の場 (平らに均す領域) の重み 0..1</summary>
        static float ArenaMask(float t, float s)
        {
            float e = ((t - 2f) / 14f) * ((t - 2f) / 14f) + ((s - 0.2f) / 4.2f) * ((s - 0.2f) / 4.2f);
            return 1f - Mathf.Clamp01((e - 0.7f) / 0.5f);
        }
        /// <summary>森の床 (2026-09-08 ユーザー「根本的にマップ地形を見直して」): 段丘と石垣をやめ、なだらかな起伏。奥 (塔の方) へ緩く登り、手前は少し下がる。
        /// 小川の溝は空き地の奥を横切る。戦闘の場は平ら</summary>
        // ---- 幕2/3 の帯 (2026-09-21 HD-2D): 高さは道の座標 s の帯で決める = 道と平行の段 (水平の帯を作らない)。境界は t のノイズで揺らして崩れた縁に
        static float BandWobble(float t, float k) { return (Vnoise(t * 0.35f + k * 13f, k * 7f) - 0.5f) * 0.45f; }
        /// <summary>幕2 (宿場跡)「縦に積む坑道」(2026-09-21 ユーザー「地形が平らすぎる」→ 裁定): 道は張り出しの棚 (0)。手前は下の坑道 −1.4 (軌道と水 −1.75)、
        /// 棚の縁 s=−3.4 は揺らさない (支保工の板張りが直線で走る)。後ろは一段目 +0.9 (敷石の宿場)・二段目 +1.8 (軌道)・壁 (s≥11.4)</summary>
        const float LedgeS = -3.4f, GalleryY = -1.4f, ChannelS = -6.6f, ChannelY = -1.75f;
        static float H2(float t, float s)
        {
            if (s < LedgeS) return s < ChannelS + BandWobble(t, 3f) * 0.5f ? ChannelY : GalleryY;
            s += BandWobble(t, 1f);
            if (s < 5.0f) return 0f;
            if (s < 8.2f) return 0.9f;
            return 1.8f;
        }
        /// <summary>幕3 (古代都市): 手前の池 −0.95 (t∈[−12,8]) ／ 場と近岸 0 ／ 水路 −0.95 ／ 遠岸 0 ／ T1 +1.1 ／ T2 +2.2</summary>
        static float H3(float t, float s)
        {
            s += BandWobble(t, 2f);
            if (s < -3.6f) return (t > -12f && t < 8f) ? -0.95f : 0f;
            if (s < 5.8f) return 0f;
            if (s < 8.6f) return -0.95f;
            if (s < 10.8f) return 0f;
            if (s < 15.6f) return 1.1f;
            return 2.2f;
        }
        /// <summary>幕2/3 の材質: 0=床 (幕2 敷石・幕3 大石板)・1=土 (幕2 道と二段目・幕3 遠岸と段の上と手前の縁)・2=水底</summary>
        static int BandMat(int act, float t, float s)
        {
            if (act == 2)
            {
                if (s < LedgeS) return s < ChannelS + BandWobble(t, 3f) * 0.5f ? 2 : 1;   // 下の坑道 = 土・水路 = 水底
                s += BandWobble(t, 1f);
                if (s < 5.0f) return 1; if (s < 8.2f) return 0; return 1;
            }
            s += BandWobble(t, 2f);
            if (s < -3.6f) return (t > -12f && t < 8f) ? 2 : 1;
            if (s < 5.8f) return 0; if (s < 8.6f) return 2; return 1;
        }
        static bool Stepped { get { return _paintedAct != 1; } }
        static float WaterY { get { return _paintedAct == 2 ? ChannelY + 0.25f : -0.55f; } }

        static float RawHeight(float x, float z)
        {
            if (_flat)
            {
                float t0, s0; PathLocal(x, z, out t0, out s0);
                return _paintedAct == 2 ? H2(t0, s0) : _paintedAct == 3 ? H3(t0, s0) : 0f;
            }
            float t, s; PathLocal(x, z, out t, out s);
            float n = Fbm(x, z);
            float rise = Mathf.Max(0f, s - 5f) * 0.06f + Mathf.Max(0f, t - 12f) * 0.05f;   // 奥と道の先へ緩く登る (塔へ向かう道)
            float drop = s < -5f ? -(0.5f + (-5f - s) * 0.12f) : 0f;                        // 手前は少し下がる
            float h = (n - 0.45f) * 2.2f + rise + drop;
            h = Mathf.Min(h, 3.0f);
            if (z > 34f) h = Mathf.Min(h, 2.0f - (z - 34f) * 0.02f);                          // 遠景は低く = 山の稜線と空の帯が残る
            h = Mathf.Lerp(h, 0f, ArenaMask(t, s));
            return h - StreamDepth(t, s);                                                     // 溝は場の均しの後に掘る (川は場の外)
        }
        /// <summary>小川の中心線 (s 方向の位置) と、その点での溝の深さ</summary>
        static float StreamCenter(float t)
        {
            float bulge = Mathf.Exp(-((t - 13f) * (t - 13f)) / 40f);            // 道の先 (t≈13) で手前へ膨らみ、空き地の奥から見える
            return 6.0f - bulge * 1.3f + Mathf.Sin(t * 0.21f) * 1.0f + Mathf.Sin(t * 0.07f + 2f) * 0.5f;
        }
        const float StreamHalf = 0.95f, StreamBed = 0.5f, WaterLevel = -0.22f;
        static float StreamDepth(float t, float s)
        {
            if (_flat) return 0f;
            if (t < -26f || t > 30f) return 0f;                                       // 川は舞台の幅だけ (遠くまで延ばさない)
            float d = Mathf.Abs(s - StreamCenter(t)) / StreamHalf;
            if (d >= 1f) return 0f;
            float k = 1f - d * d;                                                   // 中心で最深
            return StreamBed * k * k;
        }
        static bool InStream(float t, float s) { return !_flat && t >= -26f && t <= 30f && Mathf.Abs(s - StreamCenter(t)) < StreamHalf * 0.92f; }
        static float Quantize(float h, float arena) { return arena > 0.99f ? 0f : Mathf.Round(h / Step) * Step; }
        static bool DirtAt(float x, float z, float h)
        {
            if (_flat) return false;
            float t, s; PathLocal(x, z, out t, out s);
            if (InStream(t, s)) return false;
            float e = ((t - 2f) / 9.5f) * ((t - 2f) / 9.5f) + ((s - 0.4f) / 2.7f) * ((s - 0.4f) / 2.7f)
                    + (Vnoise(x * 0.3f + 5f, z * 0.3f + 9f) - 0.5f) * 1.0f + (Vnoise(x * 0.9f + 2f, z * 0.9f + 4f) - 0.5f) * 0.35f;   // 縁を大きく・細かく揺らす
            if (e < 1f) return true;
            // 森の中の獣道: 空き地の前後へ曲がりながら細く続く (2026-09-08「森じゃなくて道路じゃん」= 広い道をやめる)
            float wob = Mathf.Sin(t * 0.32f) * 1.1f + Mathf.Sin(t * 0.11f + 1.7f) * 0.6f;
            float half = 0.75f + (Vnoise(x * 0.5f + 3f, z * 0.5f + 1f) - 0.5f) * 0.5f;
            return Mathf.Abs(s - 0.4f - wob) < half && t > -34f && t < 44f;
        }
        static int CellI(float x) { return Mathf.Clamp(Mathf.FloorToInt((x - TX0) / Cell), 0, TNX - 1); }
        static int CellJ(float z) { return Mathf.Clamp(Mathf.FloorToInt((z - TZ0) / Cell), 0, TNZ - 1); }
        /// <summary>舞台の地面の高さ (世界の x・z)。箱庭 (幕1 × stage=diorama) の間は Diorama.GroundY (設計図の段の天面。座席の帯は 0)、それ以外は段丘の高さ場</summary>
        public static float GroundY(float x, float z)
        {
            if (_diorama) return Diorama.GroundY(x, z);
            return _H == null ? 0f : _H[CellI(x), CellJ(z)];
        }
        static bool IsDirt(float x, float z) { return _Dirt != null && _Dirt[CellI(x), CellJ(z)]; }

        static float CornerH(int i, int j)
        {
            // 格子の角 (i,j) の高さ: 周囲 4 セルの平均 (角を共有するので面が割れない)
            float sum = 0f; int n = 0;
            for (int di = -1; di <= 0; di++) for (int dj = -1; dj <= 0; dj++)
            {
                int ii = i + di, jj = j + dj;
                if (ii < 0 || jj < 0 || ii >= TNX || jj >= TNZ) continue;
                sum += _H[ii, jj]; n++;
            }
            return n > 0 ? sum / n : 0f;
        }

        /// <summary>アトラス (2×2) の象限 v に、タイル内の座標 (lx,lz ∈ 0..1) を向き rot (90° 単位) で回して写す</summary>
        static Vector2 AtlasUv(int v, int rot, float lx, float lz)
        {
            float rx, rz;
            switch (rot & 3) { case 1: rx = 1f - lz; rz = lx; break; case 2: rx = 1f - lx; rz = 1f - lz; break; case 3: rx = lz; rz = 1f - lx; break; default: rx = lx; rz = lz; break; }
            const float inset = 0.5f / 64f;   // 半ドット (64 ドットのタイル)
            rx = Mathf.Lerp(inset, 1f - inset, rx); rz = Mathf.Lerp(inset, 1f - inset, rz);
            return new Vector2((v % 2) * 0.5f + rx * 0.5f, (v / 2) * 0.5f + rz * 0.5f);
        }

        static int grassVariants, dirtVariants, cliffVariants, bedVariants;
        static List<Texture2D> _rootDecals;

        /// <summary>段の垂直の面 (2026-09-21 幕2/3): 隣のセルが低ければ、共有する辺に面を張る。面は壁の材質のアトラス (cliff a〜d) をタイルの上端から切る
        /// (高さは 1 タイル 1.28 以下 = 帯の差は最大 1.1)。象限は辺のタイル列で選び回さない (層は横)。カメラに背を向ける面は描画側で裏面として消える</summary>
        static void AddStepFaces(MB faces, int i, int j, float x0, float x1, float z0, float z1)
        {
            float h = _H[i, j];
            int[] di = { -1, 1, 0, 0 }, dj = { 0, 0, -1, 1 };
            for (int k = 0; k < 4; k++)
            {
                int ni = i + di[k], nj = j + dj[k];
                if (ni < 0 || nj < 0 || ni >= TNX || nj >= TNZ) continue;
                float hl = _H[ni, nj];
                if (hl >= h - 0.01f) continue;
                float dh = Mathf.Min(h - hl, Tile);
                Vector3 a, b, c, d; float along0, along1; int col;
                if (k == 0) { a = new Vector3(x0, hl, z0); d = new Vector3(x0, hl, z1); along0 = z0; along1 = z1; col = j; }
                else if (k == 1) { a = new Vector3(x1, hl, z1); d = new Vector3(x1, hl, z0); along0 = z1; along1 = z0; col = j; }
                else if (k == 2) { a = new Vector3(x1, hl, z0); d = new Vector3(x0, hl, z0); along0 = x1; along1 = x0; col = i; }
                else { a = new Vector3(x0, hl, z1); d = new Vector3(x1, hl, z1); along0 = x0; along1 = x1; col = i; }
                b = a + new Vector3(0f, h - hl, 0f); c = d + new Vector3(0f, h - hl, 0f);
                Vector2 ua, ub, uc, ud;
                if (cliffVariants > 0)
                {
                    int tcol = col >> 1; int q = (int)(Hash01(tcol * 5 + k * 17 + 3, (k < 2 ? i : j) * 3 + 1) * 4f) % 4;
                    float l0 = (col & 1) * 0.5f, l1 = l0 + 0.5f; float lv = dh / Tile;
                    ua = AtlasUv(q, 0, l0, 1f - lv); ub = AtlasUv(q, 0, l0, 1f); uc = AtlasUv(q, 0, l1, 1f); ud = AtlasUv(q, 0, l1, 1f - lv);
                }
                else { ua = new Vector2(along0 / Tile, hl / Tile); ub = new Vector2(along0 / Tile, h / Tile); uc = new Vector2(along1 / Tile, h / Tile); ud = new Vector2(along1 / Tile, hl / Tile); }
                // 外向き (低い隣へ) の法線になる順に
                var outward = new Vector3(di[k], 0f, dj[k]);
                var n = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(n, outward) >= 0f) faces.Quad(a, b, c, d, ua, ub, uc, ud); else faces.Quad(d, c, b, a, ud, uc, ub, ua);
            }
        }
        // ---- 道の帯の明るさ (2026-09-30 F50 ユーザー裁定「地面を3か所明るく」) ----
        // 道の座標 s=−2〜8 (リーダー・人形・敵の座席から、幕1 では奥の小川 (中心 s≈6) の両岸まで) の地面タイルの色を 1.15 倍にする = 画面の中間の群 (60〜169) を舞台の地面で作る。
        // 縁は外へ PathBandFeather の幅で 3 段 (1.05/1.10/1.15) に下げ、境目を t のノイズで揺らす (s 一定の直線の帯を作らない)。
        // 川床・水面 (幕1 の小川・幕2/3 の水路) と段の垂直の面は対象外 (川床は「座席の高さで最も明るい帯にしない」の裁定どおり暗いまま)
        const float PathBandS0 = -2f, PathBandS1 = 8f, PathBandGain = 0.15f, PathBandFeather = 1.3f;
        const int PathBandTiers = 3;
        static int PathBandTier(float t, float s)
        {
            float w = (Vnoise(t * 0.3f + 57f, 11f) - 0.5f) * 1.0f;
            float e = Mathf.Min(s + w - PathBandS0, PathBandS1 - (s + w));   // 帯の内側への距離 (負 = 外)
            float k = Mathf.Clamp01((e + PathBandFeather) / PathBandFeather);
            return Mathf.Clamp(Mathf.RoundToInt(k * PathBandTiers), 0, PathBandTiers);
        }
        static float PathBandGainAt(float x, float z)
        {
            if (_H == null) return 1f;
            float t, s; PathLocal(x, z, out t, out s);
            return 1f + PathBandGain * PathBandTier(t, s) / PathBandTiers;
        }
        static readonly Dictionary<MB, MB[]> _bandMb = new Dictionary<MB, MB[]>();
        static MB PathBandMb(MB baseMb, int tier)
        {
            if (tier <= 0) return baseMb;
            MB[] arr;
            if (!_bandMb.TryGetValue(baseMb, out arr)) { arr = new MB[PathBandTiers + 1]; _bandMb[baseMb] = arr; }
            if (arr[tier] == null) arr[tier] = new MB();
            return arr[tier];
        }
        /// <summary>地面の面を置き、道の帯の段 (PathBandMb で振り分けた分) を _BaseColor を上げた複製の材質で重ねずに置く</summary>
        static void SolidBand(string name, MB baseMb, Material mat)
        {
            Solid(name, baseMb, mat);
            MB[] arr;
            if (!_bandMb.TryGetValue(baseMb, out arr)) return;
            var c = mat.GetColor("_BaseColor");
            for (int k = 1; k <= PathBandTiers; k++)
            {
                if (arr[k] == null) continue;
                var m = new Material(mat);
                float g = 1f + PathBandGain * k / PathBandTiers;
                m.SetColor("_BaseColor", new Color(c.r * g, c.g * g, c.b * g, c.a));
                Solid(name + "-band" + k, arr[k], m);
            }
        }

        static void BuildTerrain(Material mGrass, Material mDirt, Material mCliff)
        {
            _tseed = 1000 + _paintedAct * 7;
            _bandMb.Clear();
            _H = new float[TNX, TNZ]; _Dirt = new bool[TNX, TNZ]; _Bed = new bool[TNX, TNZ];
            for (int i = 0; i < TNX; i++)
                for (int j = 0; j < TNZ; j++)
                {
                    float x = TX0 + (i + 0.5f) * Cell, z = TZ0 + (j + 0.5f) * Cell;
                    _H[i, j] = RawHeight(x, z);                       // 量子化なし = なだらかな森の床 (幕2/3 は帯の段)
                    if (Stepped) { float t, sv; PathLocal(x, z, out t, out sv); int m = BandMat(_paintedAct, t, sv); _Dirt[i, j] = m == 1; _Bed[i, j] = m == 2; }
                    else _Dirt[i, j] = DirtAt(x, z, _H[i, j]);
                }
            var top = new MB(); var dirtTop = new MB(); var litter = new MB(); var bed = new MB(); var faces = new MB();
            bool grassAtlas = grassVariants > 0, dirtAtlas = dirtVariants > 0;
            for (int i = 0; i < TNX; i++)
                for (int j = 0; j < TNZ; j++)
                {
                    float x0 = TX0 + i * Cell, x1 = x0 + Cell, z0 = TZ0 + j * Cell, z1 = z0 + Cell;
                    float h00, h01, h11, h10;
                    if (Stepped) { h00 = h01 = h11 = h10 = _H[i, j]; AddStepFaces(faces, i, j, x0, x1, z0, z1); }   // 段は平らな天面 + 垂直の面 (2026-09-21 幕2/3)
                    else { h00 = CornerH(i, j); h01 = CornerH(i, j + 1); h11 = CornerH(i + 1, j + 1); h10 = CornerH(i + 1, j); }
                    float t, sv; PathLocal((x0 + x1) * 0.5f, (z0 + z1) * 0.5f, out t, out sv);
                    MB target; bool atlas;
                    // 落ち葉の溜まりを斑に (幕1)。手前 (sv<-4.2) ほど溜まりのしきい値を上げ、sv<-5.2 では使わない (2026-09-29 I44: カメラの近くでタイル 1枚が約230px に写り、
                    // 落ち葉↔株の柄の境目が手札の左に斜めの直線として出ていた)。sv で一律に切ると溜まりが s 一定の直線で断ち切られるので、しきい値を上げて縁をノイズの形のまま縮める
                    bool litterHere = !Stepped && Vnoise(x0 * 0.13f + 40f, z0 * 0.13f + 9f) > 0.6f + Mathf.Max(0f, -4.2f - sv) * 0.4f;
                    if (Stepped ? _Bed[i, j] : InStream(t, sv)) { target = bed; atlas = Stepped && bedVariants > 0; }
                    else if (_Dirt[i, j]) { target = dirtTop; atlas = dirtAtlas; }
                    else if (grassAtlas) { target = top; atlas = true; }
                    else { target = litterHere ? litter : top; atlas = false; }
                    var dst = target == bed ? bed : PathBandMb(target, PathBandTier(t, sv));   // 道の帯は _BaseColor を上げた複製へ (F50。種と向きの選び方は target のまま)
                    Vector2[] uv;
                    if (atlas)
                    {
                        // タイル (2×2 セル) 単位で種 (アトラスの象限) と向きを選ぶ。セルの角はタイルの 0/0.5/1 に当たるので向きを変えても中身は繋がる。
                        // 象限の縁は半ドット内側に寄せる (点サンプルが隣の象限を拾わない)
                        int ti = i >> 1, tj = j >> 1;
                        float hv = Hash01(ti * 7 + 3, tj * 11 + 5);
                        int v;
                        if (Stepped) v = hv < 0.6f ? 0 : hv < 0.8f ? 1 : hv < 0.95f ? 2 : 3;                    // 幕2/3: a 60%・b 20%・c 15%・d 5% (種の違いが目立たないよう a を主に)
                        else v = target == top ? (litterHere ? 2 : hv < 0.74f ? 0 : 3)                       // 草: 素の草 74%・株 26%。落ち葉は溜まりだけ。クローバー (b) は粒が均等に並んで気持ち悪いので使わない (2026-09-21)
                                               : (hv < 0.9f ? (int)(hv * 3.33f) % 3 : 3);                    // 土: 踏み固めた土 3 相 90%・ひび割れ 10%
                        int rot = Stepped ? 0 : (int)(Hash01(ti * 3 + 11, tj * 5 + 7) * 4f);   // 幕2/3 は回さない (煉瓦・敷石の目地が揃う)
                        if (!Stepped && target == top && v == 0) rot &= 2;   // 向きのある草 (grass_a = 横から描いた葉の列) は 0°/180° だけ (2026-09-29 I44: 90° で葉の列が直交し、手前の約230px 角のタイルの格子が見えていた)
                        float lx0 = (i & 1) * 0.5f, lz0 = (j & 1) * 0.5f, lx1 = lx0 + 0.5f, lz1 = lz0 + 0.5f;
                        uv = new[] { AtlasUv(v, rot, lx0, lz0), AtlasUv(v, rot, lx0, lz1), AtlasUv(v, rot, lx1, lz1), AtlasUv(v, rot, lx1, lz0) };
                        dst.Quad(new Vector3(x0, h00, z0), new Vector3(x0, h01, z1), new Vector3(x1, h11, z1), new Vector3(x1, h10, z0), uv[0], uv[1], uv[2], uv[3]);
                    }
                    else
                    {
                        int rot = Stepped ? 0 : (int)(Hash01(i * 3 + 11, j * 5 + 7) * 4f);
                        uv = new[] { new Vector2(x0, z0) / Tile, new Vector2(x0, z1) / Tile, new Vector2(x1, z1) / Tile, new Vector2(x1, z0) / Tile };
                        dst.Quad(new Vector3(x0, h00, z0), new Vector3(x0, h01, z1), new Vector3(x1, h11, z1), new Vector3(x1, h10, z0),
                                    uv[rot], uv[(rot + 1) % 4], uv[(rot + 2) % 4], uv[(rot + 3) % 4]);
                    }
                }
            SolidBand("terrain-grass", top, mGrass);
            var mLitter = _flat ? mGrass : HasTile(_paintedAct, "grass2") ? Lit(Tex(_paintedAct, "grass2", null)) : Lit(Tex(_paintedAct, "leaves", Px.Dirt(_pal, new System.Random(3))));
            if (!_flat) mLitter.SetColor("_BaseColor", HasTile(_paintedAct, "grass2") ? new Color(0.4f, 0.5f, 0.5f) : HasTile(_paintedAct, "leaves") ? new Color(0.62f, 0.62f, 0.66f) : new Color(0.5f, 0.46f, 0.44f));
            SolidBand("terrain-litter", litter, mLitter);
            SolidBand("terrain-dirt", dirtTop, mDirt);
            var mBed = HasTile(_paintedAct, "bed") ? Lit(Tex(_paintedAct, "bed", null)) : Lit(Tex(_paintedAct, "dirt", Px.Dirt(_pal, new System.Random(5))));
            mBed.SetColor("_BaseColor", HasTile(_paintedAct, "bed") ? (Stepped ? new Color(0.3f, 0.36f, 0.4f) : new Color(0.36f, 0.42f, 0.48f)) : new Color(0.36f, 0.34f, 0.32f));   // 川床 = 暗い湿った土 (座席の高さで最も明るい帯にしない = レビュー)
            Solid("terrain-bed", bed, mBed);
            if (Stepped)
            {
                // 段の垂直の面 (壁の材質のアトラス) と、段の根元の影の帯・水面 (2026-09-21 HD-2D)
                Solid("terrain-faces", faces, mCliff);
                var water2 = new MB(); float wy = WaterY;
                for (int i = 0; i < TNX; i++) for (int j = 0; j < TNZ; j++) if (_Bed[i, j]) water2.Floor(TX0 + i * Cell, TZ0 + j * Cell, TX0 + (i + 1) * Cell, TZ0 + (j + 1) * Cell, wy);
                var wgo2 = new GameObject("water"); wgo2.transform.SetParent(_world, false); wgo2.layer = 4;
                wgo2.AddComponent<MeshFilter>().sharedMesh = water2.Build();
                var wmr2 = wgo2.AddComponent<MeshRenderer>();
                wmr2.sharedMaterial = WaterMaterial(_paintedAct == 2 ? new Color(0.12f, 0.14f, 0.2f, 0.75f) : new Color(0.05f, 0.09f, 0.12f, 0.8f), 0.85f, _paintedAct == 2 ? 0.4f : 0.3f);
                wmr2.shadowCastingMode = ShadowCastingMode.Off; wmr2.receiveShadows = false;
                EnsureReflection(wy);
                return;
            }
            // 水面: 溝の上に平らな半透明の板 (月と粒の光を淡く映す)。StageDriver が UV を流す
            var water = new MB();
            for (int i = 0; i < TNX; i++)
                for (int j = 0; j < TNZ; j++)
                {
                    float x0 = TX0 + i * Cell, x1 = x0 + Cell, z0 = TZ0 + j * Cell, z1 = z0 + Cell;
                    float t, sv; PathLocal((x0 + x1) * 0.5f, (z0 + z1) * 0.5f, out t, out sv);
                    float depth = StreamDepth(t, sv);
                    if (!InStream(t, sv) || depth < 0.07f) continue;
                    float wy = _H[i, j] + depth - 0.05f;                                 // 溝を掘る前の地面の少し下 = 岸は乾き、中ほどに水
                    water.Floor(x0, z0, x1, z1, wy);
                }
            var wgo = new GameObject("water");
            wgo.transform.SetParent(_world, false);
            wgo.AddComponent<MeshFilter>().sharedMesh = water.Build();
            var wmr = wgo.AddComponent<MeshRenderer>();
            _waterMat = GlowMaterial(Px.Water(_pal));
            _waterMat.mainTextureScale = new Vector2(1f, 1f);
            wmr.sharedMaterial = _waterMat;
            wmr.shadowCastingMode = ShadowCastingMode.Off; wmr.receiveShadows = false;
        }

        /// <summary>土の空き地の境界のセル中心 (縁の食い込みを置く候補)</summary>
        static List<Vector3> DirtEdgeCells()
        {
            var list = new List<Vector3>();
            if (_Dirt == null) return list;
            for (int i = 1; i < TNX - 1; i++)
                for (int j = 1; j < TNZ - 1; j++)
                    if (_Dirt[i, j] && (!_Dirt[i - 1, j] || !_Dirt[i + 1, j] || !_Dirt[i, j - 1] || !_Dirt[i, j + 1]))
                        list.Add(new Vector3(TX0 + (i + 0.5f) * Cell, _H[i, j], TZ0 + (j + 0.5f) * Cell));
            return list;
        }

        // ---------------------------------------------------------------- 水面の反射 (2026-09-21 HD-2D 裁定「幕3の水面（反射）」)

        /// <summary>鏡像カメラ: 主カメラを水面 (y=WaterY) で折り返した位置から舞台を描き、_ReflectionTex に入れる。
        /// 水面より下は斜めのニアクリップで切る (水の下の地面が映らない)。主カメラより先に描く (depth が小さい)。UI とキャラ以外の舞台を映す。
        /// スマホでは作らない (1 フレームに舞台を2回描く)</summary>
        class Reflection : MonoBehaviour
        {
            public Camera Main; public Camera Cam; public RenderTexture Rt; public float WaterY;
            static readonly int ReflTex = Shader.PropertyToID("_ReflectionTex");
            void OnEnable() { RenderPipelineManager.beginCameraRendering += Begin; RenderPipelineManager.endCameraRendering += End; }
            void OnDisable() { RenderPipelineManager.beginCameraRendering -= Begin; RenderPipelineManager.endCameraRendering -= End; Shader.SetGlobalFloat("_HasReflection", 0f); }
            void Begin(ScriptableRenderContext ctx, Camera c) { if (c == Cam) GL.invertCulling = true; }
            void End(ScriptableRenderContext ctx, Camera c) { if (c == Cam) GL.invertCulling = false; }
            void LateUpdate()
            {
                if (Main == null || Cam == null) return;
                int w = Mathf.Max(64, Screen.width / 2), h = Mathf.Max(64, Screen.height / 2);
                if (Rt == null || Rt.width != w || Rt.height != h)
                {
                    if (Rt != null) Rt.Release();
                    Rt = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR); Rt.name = "stage-reflection"; Rt.filterMode = FilterMode.Bilinear;
                    Cam.targetTexture = Rt;
                }
                // 折り返し: 位置は y を WaterY で鏡映、向きは前と上の y を反転
                var mp = Main.transform.position; var mf = Main.transform.forward; var mu = Main.transform.up;
                Cam.transform.position = new Vector3(mp.x, 2f * WaterY - mp.y, mp.z);
                Cam.transform.rotation = Quaternion.LookRotation(new Vector3(mf.x, -mf.y, mf.z), new Vector3(mu.x, -mu.y, mu.z));
                Cam.fieldOfView = Main.fieldOfView; Cam.nearClipPlane = Main.nearClipPlane; Cam.farClipPlane = Main.farClipPlane;
                Cam.ResetProjectionMatrix();
                // 水面で切る斜めのクリップ面 (カメラ空間)
                var plane = new Vector4(0f, 1f, 0f, -WaterY + 0.02f);
                var m = Cam.worldToCameraMatrix;
                var cp = Matrix4x4.Transpose(Matrix4x4.Inverse(m)) * plane;
                Cam.projectionMatrix = Cam.CalculateObliqueMatrix(cp);
                Shader.SetGlobalTexture(ReflTex, Rt);
                Shader.SetGlobalFloat("_HasReflection", 1f);
            }
            void OnDestroy() { if (Rt != null) Rt.Release(); Shader.SetGlobalFloat("_HasReflection", 0f); }
        }
        static Reflection _reflection;

        /// <summary>水面の反射を用意する (幕ごとに1つ。y = 水面の高さ)。無い時は水面シェーダが _HasReflection=0 で水の色だけを出す</summary>
        static void EnsureReflection(float waterY)
        {
            if (Application.isMobilePlatform) return;
            if (_reflection == null)
            {
                var go = new GameObject("StageReflection");
                go.transform.SetParent(_world.parent, false);
                var cam = go.AddComponent<Camera>();
                cam.CopyFrom(_cam);
                cam.depth = _cam.depth - 2f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = _cam.backgroundColor;
                cam.cullingMask = _cam.cullingMask & ~(1 << 5) & ~(1 << 4);   // UI (5) と水面そのもの (Water=4) は映さない
                cam.allowHDR = true; cam.allowMSAA = false;
                try
                {
                    var data = cam.GetUniversalAdditionalCameraData();
                    data.renderPostProcessing = false; data.renderShadows = true; data.requiresColorOption = CameraOverrideOption.Off; data.requiresDepthOption = CameraOverrideOption.Off;
                }
                catch (Exception e) { Debug.LogWarning("[Stage] 鏡像カメラの URP 設定に失敗: " + e.Message); }
                _reflection = go.AddComponent<Reflection>();
                _reflection.Main = _cam; _reflection.Cam = cam;
            }
            _reflection.WaterY = waterY;
            _reflection.gameObject.SetActive(true);
        }
        static void DisableReflection() { if (_reflection != null) _reflection.gameObject.SetActive(false); Shader.SetGlobalFloat("_HasReflection", 0f); }

        static Shader _waterShader;
        /// <summary>水面の材質 (反射つき)。tint.a が水の濃さ (地面がどれだけ透けるか)</summary>
        static Material WaterMaterial(Color tint, float reflect, float sparkle)
        {
            if (_waterShader == null) _waterShader = Shader.Find("DeckRogue/StageWater");
            if (_waterShader == null || !_waterShader.isSupported) { var g = GlowMaterial(Px.Water(_pal)); g.color = tint; return g; }
            var m = new Material(_waterShader);
            m.SetTexture("_BaseMap", Px.RippleNoise(new System.Random(77)));
            m.SetTextureScale("_BaseMap", new Vector2(0.35f, 0.35f));
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_ReflectAmount", reflect);
            m.SetFloat("_Sparkle", sparkle);
            m.renderQueue = 3000;
            return m;
        }

        // ---------------------------------------------------------------- 箱庭

        struct Pal
        {
            public Color SkyTop, SkyBot, Fog, GrassA, GrassB, GrassC, GrassDry, DirtA, DirtB, StoneA, StoneB, CliffA, CliffB, LeafA, LeafB, LeafC, Trunk, Ambient, Sun, Lantern, Filter, UnitAmbient;
            public Color CharAmbient;   // 敵と人形の板の夜の環境光 (ほぼ白・少しだけ冷たい。2026-09-29 I24)。UnitAmbient は小物と木の板
            public float LampOnUnits, LampIntensity, SunIntensity;
        }

        static Pal PalOf(int act)
        {
            var p = new Pal();
            if (act == 3)
            {
                // 幕3 坑底の古代都市: 冷たい銀と藍。濃い脈の光
                p.SkyTop = UiKit.Hex("#0d1a22"); p.SkyBot = UiKit.Hex("#2a4a52"); p.Fog = UiKit.Hex("#6f8f99");
                p.GrassA = UiKit.Hex("#3f5c3a"); p.GrassB = UiKit.Hex("#324a30"); p.GrassC = UiKit.Hex("#5a7a4a"); p.GrassDry = UiKit.Hex("#6c7a4c");
                p.DirtA = UiKit.Hex("#55504a"); p.DirtB = UiKit.Hex("#43403a");
                p.StoneA = UiKit.Hex("#66717a"); p.StoneB = UiKit.Hex("#4c565c");
                p.CliffA = UiKit.Hex("#3f484c"); p.CliffB = UiKit.Hex("#2e3538");
                p.LeafA = UiKit.Hex("#25564c"); p.LeafB = UiKit.Hex("#1a3f38"); p.LeafC = UiKit.Hex("#3f7a66"); p.Trunk = UiKit.Hex("#3c3a38");
                p.Ambient = new Color(0.2f, 0.25f, 0.28f); p.Sun = new Color(0.7f, 0.84f, 0.84f); p.Lantern = new Color(1f, 0.72f, 0.4f);   // 2026-09-21 HD-2D: 環境光を落として光溜まりと AO が読める余地。リーダーのランタンは暖色 (青緑だとキャラが環境光と同じ色に沈む = レビュー)
                p.Fog = new Color(0.26f, 0.46f, 0.48f);   // 霧は背景より明るい青緑 = 遠くほど光に溶ける (「空気が光って見える」)
                p.Filter = new Color(0.92f, 0.97f, 1.06f); p.UnitAmbient = new Color(0.8f, 0.84f, 0.96f);   // 銀の月光 (キャラを緑に染めない)
                p.CharAmbient = new Color(0.94f, 0.95f, 1.0f);
            }
            else if (act == 2)
            {
                // 幕2 提灯の夜市: 暖かい提灯の橙と古い木と石
                p.SkyTop = UiKit.Hex("#200c16"); p.SkyBot = UiKit.Hex("#5c2838"); p.Fog = UiKit.Hex("#8a5a66");
                p.GrassA = UiKit.Hex("#5a4a3a"); p.GrassB = UiKit.Hex("#463a2e"); p.GrassC = UiKit.Hex("#726048"); p.GrassDry = UiKit.Hex("#7a6a50");
                p.DirtA = UiKit.Hex("#5c4242"); p.DirtB = UiKit.Hex("#4a3333");
                p.StoneA = UiKit.Hex("#6c5b60"); p.StoneB = UiKit.Hex("#54464a");
                p.CliffA = UiKit.Hex("#4a3a3c"); p.CliffB = UiKit.Hex("#362a2c");
                p.LeafA = UiKit.Hex("#5e2632"); p.LeafB = UiKit.Hex("#3f1a22"); p.LeafC = UiKit.Hex("#8a4048"); p.Trunk = UiKit.Hex("#3a2a2a");
                p.Ambient = new Color(0.3f, 0.32f, 0.4f); p.Sun = new Color(0.6f, 0.66f, 0.8f); p.Lantern = new Color(1f, 0.6f, 0.35f);   // 2026-09-21 HD-2D: 地は青灰、暖色は炉と提灯の範囲だけ (旧は全体が暖色に染まっていた。設計の 0.16 は真っ暗だった)
                p.Fog = new Color(0.12f, 0.13f, 0.18f);
                p.Filter = new Color(1.0f, 0.96f, 0.98f); p.UnitAmbient = new Color(0.7f, 0.7f, 0.82f);
                p.CharAmbient = new Color(0.9f, 0.88f, 0.94f);
            }
            else
            {
                // 幕1: 地面は黄緑寄り・木は青緑寄り (溶け合わない)
                p.SkyTop = UiKit.Hex("#141a3c"); p.SkyBot = UiKit.Hex("#3a4478"); p.Fog = UiKit.Hex("#5a6a9c");
                p.GrassA = UiKit.Hex("#436230"); p.GrassB = UiKit.Hex("#365228"); p.GrassC = UiKit.Hex("#5a7a3c"); p.GrassDry = UiKit.Hex("#6c6c42");
                p.DirtA = UiKit.Hex("#6c5b40"); p.DirtB = UiKit.Hex("#564834");
                p.StoneA = UiKit.Hex("#7a7674"); p.StoneB = UiKit.Hex("#5c5856");
                p.CliffA = UiKit.Hex("#524a42"); p.CliffB = UiKit.Hex("#3c3630");
                p.LeafA = UiKit.Hex("#2c5a48"); p.LeafB = UiKit.Hex("#1e4236"); p.LeafC = UiKit.Hex("#4a8a64"); p.Trunk = UiKit.Hex("#4a3a2a");
                p.Ambient = new Color(0.28f, 0.33f, 0.56f); p.Sun = new Color(0.72f, 0.8f, 1f); p.Lantern = new Color(1f, 0.72f, 0.4f);
                p.Filter = new Color(0.86f, 0.92f, 1.12f); p.UnitAmbient = new Color(0.58f, 0.65f, 0.94f);   // 環境光は青く暗め = 街灯の暖色が読める (小物と木の板)
                p.CharAmbient = new Color(0.92f, 0.93f, 1.0f);   // 敵と人形は絵本の元の色 (クリーム・砂・淡い銀) で地面より一段明るく立つ。夜の青はカラーフィルタとリフトと空・樹冠が持つ
            }
            p.LampOnUnits = 1.0f; p.LampIntensity = 3.6f; p.SunIntensity = act == 1 ? 2.1f : act == 2 ? 0.9f : 1.45f;   // 幕1: 月光を強く (木の影と地面の陰影)。幕2: 坑内 = 風穴からの淡い光。幕3: 脈の照り返し (2026-09-21 HD-2D)   // 月明かりは強め (2026-09-08「月明かりももっと強くして」。旧 0.8)   // 補間の強さ (1 で街灯の色そのもの)
            return p;
        }

        /// <summary>幕の箱庭を組む (同じ幕なら組み直さない)</summary>
        public static void Paint(int act)
        {
            Ensure();
            // 検証用: 起動引数 -stageact N で舞台の幕だけ差し替える (スクショの自動操縦で幕2/3の舞台を撮る。ゲームの進行には触れない)
            var cargs = Environment.GetCommandLineArgs();
            for (int i = 0; i < cargs.Length - 1; i++) if (cargs[i] == "-stageact") { int a; if (int.TryParse(cargs[i + 1], out a)) act = Mathf.Clamp(a, 1, 3); }
            // 幕ごとの既定 (2026-10-01): 旗 stage=・hd2d= を書かない普通の起動では、ここで幕の束を当てる = 幕1 は見本 (hd2d=slice)・幕2/3 は今の舞台 (stage=old・画角36・紙の札・主人公62)。
            // 箱庭は幕1にしか無いので、幕2/3 に見本のカメラ・座席・キャラの光・夜色の札を残さない。変われば Changed でカメラが置き直り、下の名札 (sig) が組み直しを決める。
            // 撮影の STATE・-hd2d に stage= か hd2d= を書いた時は何もしない (今までどおり書いた旗が全部の幕で勝つ)
            // 段2 の口 (2026-10-03): 今組む幕を先に書き (カメラ・札・キャラの光の「箱庭か」= HD2DFlags.DioramaHere が読む)、
            // その幕の箱庭の設計図と光の設計図 (look_act<N>) がそろっているかを書いてから束を当てる (無い幕・組めなかった幕は旗で入れても今の舞台に落とす)
            HD2DFlags.StageAct = act;
            // 箱庭にする幕の時だけ設計図と光の設計図の有無を見る (今の舞台の幕で TextAsset を読まない。反証「壊していないか」)
            HD2DFlags.SetDioramaFallback(act, HD2DFlags.DioramaOn(act) && (!DioramaAssetsReady(act) || _dioramaFailed.Contains(act)));
            HD2DFlags.ApplyActDefault(act);
            bool wantDio = WantDiorama(act);
            string sig = wantDio ? DioramaSignature(act) : "old";
            if (_paintedAct == act && _paintedSig == sig) return;
            // 箱庭 (HD-2D 見本 P12): 箱庭にする幕 × stage=diorama なら Diorama と StageLook で組んで終わり。設計図が無い・組むのに失敗したら下の今の舞台 (old) で描く
            if (wantDio && PaintDiorama(act, sig)) return;
            if (wantDio)
            {
                // 組むのに失敗した: この幕は以後も今の舞台に落とし (毎回の組み直しで失敗を繰り返さない)、カメラ・札・キャラの光も今の舞台の値へ (預け替え)
                _dioramaFailed.Add(act);
                HD2DFlags.SetDioramaFallback(act, true);
                HD2DFlags.ApplyActDefault(act);
                sig = "old";
            }
            LeaveDiorama();   // old で描く前に: 箱庭の光 (StageLook) を控えへ戻し (Paint が下で書く環境光・霧・色補正を上書きしないよう先に)・箱庭を捨て・今の月とランタンを点け直す
            _paintedAct = act;
            _paintedSig = sig;   // 箱庭に失敗した時もこの組み方のうちは組み直さない (Rebuild のたびに失敗と old の描き直しを繰り返さない)
            _flat = act != 1;
            for (int i = _world.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_world.GetChild(i).gameObject);
            _seatPools.Clear(); _seatPoolN = -1;   // 座席の光溜まりも今消した子の中にある (Destroy はフレームの終わりなので null 判定に頼らず次の EnemySlots で置き直す)
            DisableReflection();
            SetFxForAct(act);
            var p = PalOf(act);
            _pal = p;
            var rng = new System.Random(1000 + act * 17);

            _cam.backgroundColor = p.SkyTop;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = p.Ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.Fog;                  // 空気遠近: 遠くほど白く薄く。最上段はここで薄れて終わる
            RenderSettings.fogStartDistance = act == 1 ? 12f : 20f;
            RenderSettings.fogEndDistance = act == 1 ? 46f : 62f;   // 幕1: 遠い芝を霧に沈める (窓を開けた奥が明るい芝生に見えない)
            _sun.color = p.Sun; _sun.intensity = p.SunIntensity;
            _lantern.color = p.Lantern; _lantern.intensity = p.LampIntensity;
            if (_color != null) { _color.colorFilter.value = p.Filter; _color.postExposure.value = 0.12f; }

            // 地面と道
            var grass = Px.Grass(p, rng); var dirt = Px.Dirt(p, rng); var stone = Px.Stone(p, rng); var cliff = Px.Cliff(p, rng);
            var mGrass = Ground(act, "grass", grass, out grassVariants); var mDirt = Ground(act, "dirt", dirt, out dirtVariants); var mStone = Lit(Tex(act, "stone", stone)); var mCliff = Ground(act, "cliff", cliff, out cliffVariants);
            bedVariants = 0;
            if (cliffVariants > 0) mCliff.SetColor("_BaseColor", act == 2 ? new Color(0.62f, 0.62f, 0.7f) : new Color(0.36f, 0.4f, 0.48f));
            // PixelLab のタイルは昼の色で描かれるので、取り込んだ時だけ幕の夜のパレットへ寄せる (仮のタイルは元から夜の色)。4 種のアトラス (夜の色で発注) は薄く
            if (grassVariants > 0) mGrass.SetColor("_BaseColor", new Color(0.44f, 0.52f, 0.55f));   // 月夜の草地 = 青緑に沈める (昼の緑のまま出すと芝生になる)
            else if (HasTile(act, "grass")) mGrass.SetColor("_BaseColor", new Color(0.46f, 0.6f, 0.58f));   // 森の床は暗め
            if (dirtVariants > 0) mDirt.SetColor("_BaseColor", act == 1 ? new Color(0.58f, 0.55f, 0.54f) : act == 2 ? new Color(0.78f, 0.78f, 0.86f) : new Color(0.56f, 0.58f, 0.64f));
            else if (HasTile(act, "dirt")) mDirt.SetColor("_BaseColor", act == 2 ? new Color(0.8f, 0.8f, 0.84f) : new Color(0.66f, 0.6f, 0.56f));    // 土=灰茶 (月明かりの空き地。影が読める明るさ)。暖色は街灯の範囲だけ
            if (HasTile(act, "stone")) mStone.SetColor("_BaseColor", new Color(0.56f, 0.62f, 0.76f)); // 石=青灰
            if (HasTile(act, "cliff")) mCliff.SetColor("_BaseColor", new Color(0.6f, 0.52f, 0.48f));  // 崖=土色
            if (act != 1)
            {
                // 幕2/3: 坑の中。床は石 (幕2=暖かい灰茶・幕3=冷たい黒石)
                var mFloor = Ground(act, "stone", stone, out grassVariants);   // 幕2/3 の床 = 石 (4 種のアトラスがあればそれ。BuildTerrain は grassVariants を床の種の数として読む)
                mFloor.SetColor("_BaseColor", grassVariants > 0 ? (act == 2 ? new Color(0.72f, 0.74f, 0.84f) : new Color(0.58f, 0.6f, 0.66f)) : act == 2 ? new Color(0.5f, 0.42f, 0.38f) : new Color(0.3f, 0.33f, 0.44f));   // 幕3 は青一色にしない (灰の石)
                if (_color != null) _color.postExposure.value = act == 2 ? 0.32f : 0.22f;   // 坑の中は光源が点なので半段明るく (幕1 は 0.12)
                BuildTerrain(mFloor, mDirt, mCliff);
                if (act == 2) PaintMarket2(p, rng, mFloor, mCliff); else PaintCorridor2(p, rng, mFloor, mCliff);
                SetFxForAct(act);   // 炉の位置 (_emberPos) が決まった後にもう一度 (火の粉を炉に置く)
                return;
            }
            // ---- 森の床 (幕1): なだらかな起伏・小川・獣道
            BuildTerrain(mGrass, mDirt, mCliff);

            // 土の空き地の縁: 草に食われた縁と土のこぼれ (不定形の塊の境界に置く)
            var mBite = Cutout(Px.Patch(p.GrassA, p.GrassB, rng));
            var mSpill = Cutout(Px.Patch(p.DirtA, p.DirtB, rng));
            var edges = DirtEdgeCells();
            var edgeDecals = PatchSet(act, "grass_a", true, 40, 26, 32, 22, 48, 28);   // grass_b (クローバー) は等間隔の粒なので舌にも使わない
            _decalTint = mGrass.GetColor("_BaseColor");
            if (edgeDecals.Count > 0)
            {
                // 境界のセルごとに、草側の隣へ向けて「草の舌」を寝かせる = セル単位の段が消え、道の縁が不定形になる (2026-09-21)
                for (int e = 0; e < edges.Count; e++)
                {
                    var c = edges[e]; int ci = CellI(c.x), cj = CellJ(c.z);
                    int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
                    for (int k = 0; k < 4; k++)
                    {
                        int ni = ci + dx[k], nj = cj + dz[k];
                        if (ni < 0 || nj < 0 || ni >= TNX || nj >= TNZ || _Dirt[ni, nj]) continue;
                        float ex = c.x + dx[k] * Cell * 0.5f, ez = c.z + dz[k] * Cell * 0.5f;
                        float yaw = Mathf.Atan2(dx[k], dz[k]) * Mathf.Rad2Deg + ((float)rng.NextDouble() - 0.5f) * 30f;
                        GroundDecal("edge", Pick(edgeDecals, rng), ex + ((float)rng.NextDouble() - 0.5f) * 0.15f, ez + ((float)rng.NextDouble() - 0.5f) * 0.15f,
                                    0.95f + (float)rng.NextDouble() * 0.5f, yaw, 0.012f, rng.NextDouble() < 0.5);
                    }
                }
            }
            for (int i = 0; i < edges.Count; i++) { int j = rng.Next(i, edges.Count); var tmp = edges[i]; edges[i] = edges[j]; edges[j] = tmp; }
            for (int i = 0; i < (edgeDecals.Count > 0 ? 0 : Mathf.Min(44, edges.Count)); i++)
            {
                var c = edges[i];
                float sz = 0.7f + (float)rng.NextDouble() * 1.4f;
                if (i % 3 != 2) Decal("bite", mBite, c.x + ((float)rng.NextDouble() - 0.5f) * 0.6f, c.z + ((float)rng.NextDouble() - 0.5f) * 0.6f, sz, sz * 0.6f, (float)rng.NextDouble() * 360f);
                else Decal("spill", mSpill, c.x + ((float)rng.NextDouble() - 0.5f) * 0.8f, c.z + ((float)rng.NextDouble() - 0.5f) * 0.8f, sz * 0.8f, sz * 0.45f, (float)rng.NextDouble() * 360f);
            }
            // 草地のムラ: 明るい草地と枯れ地の「色の島」(少なく・大小・不定形)
            var mIslandLight = Cutout(Px.Patch(p.GrassC, Color.Lerp(p.GrassA, p.GrassC, 0.5f), rng));
            var mIslandDry = Cutout(Px.Patch(p.GrassDry, Color.Lerp(p.GrassA, p.GrassDry, 0.5f), rng));
            {   // 塊 2 つ (向きを変える) と細い流れ 1 つ
                var w1 = OnPath(-15f, 6.2f); Decal("island", mIslandLight, w1.x, w1.z, 4.2f, 2.6f, 25f);
                var w2 = OnPath(12f, -4.6f); Decal("island", mIslandDry, w2.x, w2.z, 2.4f, 1.9f, 140f);
                var w3 = OnPath(3f, 7.6f); Decal("island-streak", mIslandDry, w3.x, w3.z, 5.0f, 0.9f, 80f);
            }
            // 草の株・花は草地に、小石は土の上に
            var tuft = PropTex(act, "tuft", Px.Tuft(p, rng)); var pebble = Px.Pebble(p, rng); var flower = Px.Flower(p, rng);
            var mPebble = Cutout(pebble);
            var pebbleDecals = PatchSet(act, "m_gravel", false, 24, 16, 40, 28, 32, 20);
            for (int i = 0; i < 70; i++)
            {
                float t = -22f + (float)rng.NextDouble() * 46f;
                float sv = -5f + (float)rng.NextDouble() * 15f;
                var w = OnPath(t, sv);
                if (IsDirt(w.x, w.z))
                {
                    _decalTint = mDirt.GetColor("_BaseColor");
                    if (pebbleDecals.Count > 0) { if (rng.NextDouble() < 0.7) GroundDecal("pebbles", Pick(pebbleDecals, rng), w.x, w.z, 0.8f + (float)rng.NextDouble() * 0.5f, (float)rng.NextDouble() * 360f, 0.016f, rng.NextDouble() < 0.5); }
                    else if (rng.NextDouble() < 0.5) Decal("pebble", mPebble, w.x, w.z, 0.36f + (float)rng.NextDouble() * 0.16f, 0.24f, (float)rng.NextDouble() * 360f);
                    _decalTint = mGrass.GetColor("_BaseColor");
                    continue;
                }
                if (rng.NextDouble() < 0.15) Plane("flower", flower, w, 0.46f, 0.5f, false);
                else SpriteH("tuft", tuft, w, 0.3f + (float)rng.NextDouble() * 0.18f, 0f, rng.NextDouble() < 0.5);
            }
            // 地面の敷物 (2026-09-21 HD-2D): 落ち葉・苔・クローバー・花・小枝・根・ひび・泥・水たまりを、材質に合わせて散らす (床の空白を埋める = オクトラの床)
            {
                var leaves = PatchSet(act, "m_leaves", false, 48, 32, 64, 44, 80, 56, 36, 24); var moss = PatchSet(act, "m_moss", false, 40, 28, 56, 40, 72, 48); var clover = PatchSet(act, "m_clover", false, 40, 28, 56, 36);
                var flowers = PatchSet(act, "m_flowers", false, 32, 24, 48, 32); var twigs = PatchSet(act, "m_twigs", false, 40, 24, 56, 32); var roots = PropSet(act, "d_roots1", "root");
                var cracks = PatchSet(act, "m_crack", false, 48, 32, 64, 44); var mud = PatchSet(act, "m_mud", false, 56, 40, 80, 56);
                var puddles = new List<Texture2D> { Px.Puddle(rng, 72, 40), Px.Puddle(rng, 56, 32), Px.Puddle(rng, 88, 44) };
                System.Func<float, float, Vector3?> spot = (tMin, tMax) =>
                {
                    float t = tMin + (float)rng.NextDouble() * (tMax - tMin), sv = -8f + (float)rng.NextDouble() * 22f;
                    var w = OnPath(t, sv);
                    if (InStream(t, sv)) return null;
                    return w;
                };
                // 草地: 落ち葉 (奥と縁に多い)・苔・クローバー・花・小枝
                for (int i = 0; i < 40 && leaves.Count > 0; i++) { var w = spot(-26f, 32f); if (w == null || IsDirt(w.Value.x, w.Value.z)) continue; GroundDecal("leaves", Pick(leaves, rng), w.Value.x, w.Value.z, 0.8f + (float)rng.NextDouble() * 0.6f, (float)rng.NextDouble() * 360f, 0.018f, rng.NextDouble() < 0.5); }
                for (int i = 0; i < 30 && moss.Count > 0; i++) { var w = spot(-26f, 32f); if (w == null || IsDirt(w.Value.x, w.Value.z)) continue; GroundDecal("moss", Pick(moss, rng), w.Value.x, w.Value.z, 0.8f + (float)rng.NextDouble() * 0.7f, (float)rng.NextDouble() * 360f, 0.014f, rng.NextDouble() < 0.5); }
                for (int i = 0; i < 10 && clover.Count > 0; i++) { var w = spot(-24f, 30f); if (w == null || IsDirt(w.Value.x, w.Value.z)) continue; GroundDecal("clover", Pick(clover, rng), w.Value.x, w.Value.z, 0.8f + (float)rng.NextDouble() * 0.5f, (float)rng.NextDouble() * 360f, 0.02f, rng.NextDouble() < 0.5); }
                for (int i = 0; i < 8 && flowers.Count > 0; i++) { var w = spot(-24f, 30f); if (w == null || IsDirt(w.Value.x, w.Value.z)) continue; GroundDecal("flowers", Pick(flowers, rng), w.Value.x, w.Value.z, 0.8f + (float)rng.NextDouble() * 0.4f, (float)rng.NextDouble() * 360f, 0.024f, rng.NextDouble() < 0.5); }
                for (int i = 0; i < 18 && twigs.Count > 0; i++) { var w = spot(-26f, 32f); if (w == null) continue; GroundDecal("twigs", Pick(twigs, rng), w.Value.x, w.Value.z, 0.8f + (float)rng.NextDouble() * 0.5f, (float)rng.NextDouble() * 360f, 0.026f, rng.NextDouble() < 0.5); }
                // 土: 小石は上で撒いた。ひび・泥・水たまり
                _decalTint = mDirt.GetColor("_BaseColor");
                for (int i = 0; i < 5 && cracks.Count > 0; i++) { var w = spot(-22f, 28f); if (w == null || !IsDirt(w.Value.x, w.Value.z)) continue; GroundDecal("crack", Pick(cracks, rng), w.Value.x, w.Value.z, 0.9f + (float)rng.NextDouble() * 0.5f, (float)rng.NextDouble() * 360f, 0.013f, rng.NextDouble() < 0.5); }
                for (int i = 0; i < 5 && mud.Count > 0; i++) { var w = spot(-22f, 28f); if (w == null || !IsDirt(w.Value.x, w.Value.z)) continue; GroundDecal("mud", Pick(mud, rng), w.Value.x, w.Value.z, 0.9f + (float)rng.NextDouble() * 0.6f, (float)rng.NextDouble() * 360f, 0.015f, rng.NextDouble() < 0.5); }
                {
                    // 水たまり: 半透明の板 (Glow = 影も深度も書かない) を地面のすぐ上に寝かせる。道の脇と空き地の縁
                    float[] pt = { -2f, 9f, 5.5f, -14f, 20f }; float[] pss = { -2.2f, 2.4f, -2.6f, 1.2f, 3.5f };
                    for (int i = 0; i < pt.Length; i++)
                    {
                        var w = OnPath(pt[i], pss[i]); var tex = Pick(puddles, rng);
                        float sw = tex.width * GroundDot * (0.9f + (float)rng.NextDouble() * 0.5f);
                        var g = Glow("puddle", tex, new Vector3(w.x, GroundY(w.x, w.z) + 0.035f, w.z), 1f, 1f);
                        g.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                        g.transform.rotation = Quaternion.Euler(90f, (float)rng.NextDouble() * 360f, 0f);
                        g.transform.localScale = new Vector3(sw, sw * tex.height / tex.width, 1f);
                    }
                }
                _rootDecals = roots;   // 根は木の足元に (木を置く時に)
                _decalTint = mGrass.GetColor("_BaseColor");
            }
            // 夜の花と水たまり (道の脇・空き地の縁)
            var flower2 = PropTex(act, "flower", null); var puddle = PropTex(act, "puddle", null);
            for (int i = 0; i < 22; i++)
            {
                float t = -20f + (float)rng.NextDouble() * 42f;
                float sv = rng.NextDouble() < 0.5 ? -4.6f + (float)rng.NextDouble() * 1.8f : 3.9f + (float)rng.NextDouble() * 1.4f;
                var w = OnPath(t, sv);
                if (IsDirt(w.x, w.z) || InStream(t, sv)) continue;
                if (flower2 != null && rng.NextDouble() < 0.75) Plane("nightflower", flower2, w, 0.42f + (float)rng.NextDouble() * 0.12f, 0.5f, false);
            }
            if (puddle != null && false)   // 旧 PixelLab の水たまり (48×24) は使わない (コード生成の Px.Puddle へ。2026-09-21)
            {
                float[] pt = { -2f, 9f, 5.5f }; float[] pss = { -2.2f, 2.4f, -2.6f };
                for (int i = 0; i < pt.Length; i++)
                {
                    var w = OnPath(pt[i], pss[i]);
                    var g = Plane("puddle", puddle, new Vector3(w.x, w.y + 0.015f, w.z), 0.5f, 0.5f, false);
                    g.transform.rotation = Quaternion.Euler(90f, (float)rng.NextDouble() * 360f, 0f); g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.localScale = new Vector3(1.4f, 0.7f, 1f);
                    g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            // 露頭 (2026-09-10 世界観改稿): 脈が地表に顔を出した場所。地面の細い割れ目が青緑に光り、光の粒はここから湧く。
            // 群れの中心 (_lampPos) に太いのを1本、その周りに細いのを数本。幕2/3の脈の光と同じ色で繋ぐ
            {
                var seam = Px.Radial(new Color(0.45f, 0.95f, 0.86f, 0.75f));
                var lb = new Vector3(_lampPos.x, 0f, _lampPos.z);
                for (int i = 0; i < 7; i++)
                {
                    float ang = (i == 0) ? PathYaw + 18f : (float)rng.NextDouble() * 180f;
                    float len = (i == 0) ? 3.6f : 1.2f + (float)rng.NextDouble() * 1.6f;
                    float wid = (i == 0) ? 0.34f : 0.14f + (float)rng.NextDouble() * 0.1f;
                    var off = (i == 0) ? Vector3.zero : new Vector3(-2.2f + (float)rng.NextDouble() * 4.4f, 0f, -1.6f + (float)rng.NextDouble() * 3.2f);
                    var w = lb + off; w.y = GroundY(w.x, w.z) + 0.03f;
                    var g = Glow("outcrop-seam", seam, w, 1f, 1f);
                    g.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                    g.transform.rotation = Quaternion.Euler(90f, ang, 0f);
                    g.transform.localScale = new Vector3(len, wid, 1f);
                }
                var ogo = new GameObject("outcrop-light"); ogo.transform.SetParent(_world, false); ogo.transform.position = lb + new Vector3(0f, 0.4f, 0f);
                var ol = ogo.AddComponent<Light>(); ol.type = LightType.Point; ol.range = 3.2f; ol.intensity = 0.9f; ol.color = new Color(0.45f, 0.95f, 0.88f); ol.shadows = LightShadows.None;
            }
            // 光る茸: 脈のマナを浴びて育つ森の灯 (世界観)。自発光の板 + 足元の青い暈 + いくつかは点光源。戦闘の場は避ける
            var shroom = PropTex(act, "shroom", Px.GlowShroom(p, rng));
            var shroom2 = PropTex(act, "shroom2", null);
            int lit = 0;
            for (int i = 0; i < 40; i++)
            {
                float t = -24f + (float)rng.NextDouble() * 50f;
                float sv = -6f + (float)rng.NextDouble() * 17f;
                if (sv > -2.5f && sv < 6.5f && t > -11f && t < 15f) continue;   // 戦闘の場と道の中は避ける
                var w = OnPath(t, sv);
                if (IsDirt(w.x, w.z)) continue;
                bool big = shroom2 != null && rng.NextDouble() < 0.4;
                var g = Prop("glowshroom", big ? shroom2 : shroom, w + new Vector3(0f, 0.02f, 0f), big ? 0.5f + (float)rng.NextDouble() * 0.2f : 0.42f + (float)rng.NextDouble() * 0.16f, 0.5f);
                g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
                var halo = Glow("shroom-halo", Px.Radial(new Color(0.55f, 0.9f, 1f, 0.42f)), w + new Vector3(0f, 0.03f, 0f), 1f, 1f);
                halo.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                halo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                halo.transform.localScale = new Vector3(1.5f, 1.3f, 1f);
                if (lit < 8 && sv > -4f && sv < 9f)
                {
                    var lgo2 = new GameObject("shroom-light"); lgo2.transform.SetParent(_world, false); lgo2.transform.position = w + new Vector3(0f, 0.35f, 0f);
                    var l = lgo2.AddComponent<Light>(); l.type = LightType.Point; l.range = 1.7f; l.intensity = 0.9f; l.color = new Color(0.55f, 0.85f, 1f); l.shadows = LightShadows.None;
                    lit++;
                }
            }
            // 月光の筋: 右上 (月光の向き) から差す淡い光の帯を木立の間に (中景〜遠景)
            var beam = Px.Beam();
            float[] bx = { -5.5f, 0.5f, 4.5f, 8.5f, 13f, -18f, 25f }; float[] bz = { 10f, 11f, 13f, 10f, 14f, 12f, 7f }; float[] bw = { 2.8f, 3.4f, 2.6f, 3.2f, 2.4f, 4.2f, 5.0f };   // 短い筋 = 映る帯の α が濃い (高さ 22 では α 0.13 で読めなかった = レビュー)
            var moonPool = PropTexRaw(act, "d_moonlight", null);
            for (int i = 0; i < bx.Length; i++)
            {
                var b = Glow("moonbeam", beam, new Vector3(bx[i], 0.3f, bz[i]), i < 5 ? 9f : 22f, bw[i]);
                b.transform.rotation = Quaternion.Euler(0f, 0f, -14f);
                b.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(1f, 1f, 1f, i < 4 ? 1f : 0.7f);
                // 筋の足元: 木漏れ日の光溜まり (2026-09-21 HD-2D)。筋は -14° に傾いているので足元は少し右 (+x) へ
                float fx = bx[i] + 22f * Mathf.Sin(14f * Mathf.Deg2Rad) * 0.5f, fz = bz[i];
                if (moonPool != null)
                {
                    for (int q = 0; q < 2; q++)
                    {
                        float px2 = fx + ((float)rng.NextDouble() - 0.5f) * 2.5f, pz2 = fz + ((float)rng.NextDouble() - 0.5f) * 2.5f;
                        var g = Glow("moon-pool", moonPool, new Vector3(px2, GroundY(px2, pz2) + 0.06f, pz2), 1f, 1f);
                        g.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                        g.transform.rotation = Quaternion.Euler(90f, (float)rng.NextDouble() * 360f, 0f);
                        float sw = moonPool.width * GroundDot * (1.2f + (float)rng.NextDouble() * 0.8f);
                        g.transform.localScale = new Vector3(sw, sw * moonPool.height / moonPool.width, 1f);
                        g.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(1f, 1f, 1f, 0.55f);
                    }
                }
                else
                {
                    var g = Glow("moon-pool", Px.Radial(new Color(0.8f, 0.88f, 1f, 0.22f)), new Vector3(fx, GroundY(fx, fz) + 0.05f, fz), 1f, 1f);
                    g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.rotation = Quaternion.Euler(90f, 0f, 0f); g.transform.localScale = new Vector3(bw[i] * 1.3f, bw[i] * 0.8f, 1f);
                }
            }
            // 主役の背後の月明かり (2026-09-29 I25): 黒鉄の斧の刃が背後の暗い幹と同じ暗さ (0.99:1) で黒い円盤に、柄の先だけが火の粉に見えていた。
            // リーダーの板のリム・勾配・環境光は触らない (2026-09-16 の裁定)。上と同じ月光の筋を1本、リーダー (z≈-1) と背後の木 (z≈9) の間に通し、
            // 刃と柄の後ろの面だけを持ち上げる (リーダーの板は AlphaTest＋ZWrite なので筋は本人に隠れ、本人の上には色が乗らない)。寒色＝「暖色は街灯の範囲だけ」の外
            {
                // 筋の絵は上ほど濃い (α = v^1.3)。根元を地面の下 (y=-3。手前の地面に隠れる) へ下げて長さ 11 にし、濃い中ほどを刃と頭の高さに当てる
                // (根元 y=0.3・長さ 8 だと刃の高さは v≈0.2 で α≈0.04 = 効かなかった)。傾き −14° で上ほど右へ寄るぶん根元を左へ (刃の高さで中心が画面 x≈500)
                var hbPos = OnPath(-4.6f, 4.0f); hbPos = new Vector3(hbPos.x - 0.44f, -3.0f, hbPos.z);
                // 上は薄れさせる (F53: 刃と頭の高さ v≈0.3〜0.45 の α は今と同じ・梢 v≥0.58 では 0)
                var hb = Glow("moonbeam-hero", Px.Beam(new Color(0.7f, 0.8f, 1f), 0.42f, 0.58f), hbPos, 11f, 3.0f);
                hb.transform.rotation = Quaternion.Euler(0f, 0f, -14f);
                // 足元の光溜まり: 足の少し奥 (画面では脚の背後の道)。地面に寝かせる (Glow の既定は下端が支点の立て板)
                var hp = Glow("moon-pool-hero", Px.Radial(new Color(0.8f, 0.88f, 1f, 0.28f)), Vector3.zero, 1f, 1f);
                hp.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                hp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                hp.transform.position = LeaderSlot() + new Vector3(0.2f, 0.05f, 0.8f);
                hp.transform.localScale = new Vector3(3.0f, 1.8f, 1f);
                // 斧の柄の背後の光溜まり (2026-09-30 F51: 柄の背後に見えているのは川の向こう岸の草で、紺黒の柄が草の暗い筋に溶け、石突の橙だけが浮いた)。
                // 向こう岸 (s≈8.2・PC とスマホの両方の柄の背後) に寝かせる。手前の暖色の mote-pool (z≦2.0) とは重ならない。α は控えめ (スポットライトに見せない)
                var hf = OnPath(-5.7f, 8.2f);
                var hh = Glow("moon-pool-haft", Px.Radial(new Color(0.8f, 0.88f, 1f, 0.18f)), Vector3.zero, 1f, 1f);
                hh.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                hh.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                hh.transform.position = new Vector3(hf.x, GroundY(hf.x, hf.z) + 0.05f, hf.z);
                hh.transform.localScale = new Vector3(3.0f, 2.6f, 1f);
            }

            // 遺跡の柱 (石) — 段丘の上に (額縁)
            // 道標: 坑口へ続く古い道の名残 (苔むした折れた石柱)。青い煉瓦の柱は森に浮くので廃止
            var waystone = PropTex(act, "waystone", null);
            if (waystone != null)
            {
                Plane("waystone", waystone, OnPath(15.5f, 2.6f), 1.7f, 0.5f, true);
                Plane("waystone", waystone, OnPath(-17f, -2.2f), 1.4f, 0.5f, true);
            }

            // 木: 段丘の上に大小をばらして散らす (戦闘の場の外・奥ほど多い)。3種・大きさ 0.6〜1.5
            var trees = new[] { Px.Tree(p, rng, 0), Px.Tree(p, rng, 1), Px.Tree(p, rng, 2) };
            var bush = Px.Bush(p, rng); var rock = PropTex(act, "rock", Px.Rock(p, rng));
            var bushes = PropSet(act, "bush1", "bush2", "bush3"); var bigRocks = PropSet(act, "rock_big1", "rock_big2"); var smallRock = PropTex(act, "rock_small", null);   // bush_flower (等間隔の白い花) は 2026-09-21 ユーザー「集合体恐怖症を煽る」で外した var bigRocks = PropSet(act, "rock_big1", "rock_big2"); var smallRock = PropTex(act, "rock_small", null);
            var stump = PropTex(act, "stump", null); var log2 = PropTex(act, "log2", null); var tallGrass = PropSet(act, "tallgrass1", "tallgrass2"); var fern2 = PropTex(act, "fern2", null); var vines = PropTex(act, "vines", null);
            var clumpList = new List<Texture2D> { PropTex(act, "leafclump1", Px.LeafClump(p, rng, 0)), PropTex(act, "leafclump2", Px.LeafClump(p, rng, 1)), PropTex(act, "leafclump3", Px.LeafClump(p, rng, 2)) };
            var c4 = PropTex(act, "leafclump4", null); if (c4 != null) clumpList.Add(c4);
            var c5 = PropTex(act, "leafclump5", null); if (c5 != null) clumpList.Add(c5);
            var clumps = clumpList.ToArray();
            var barkMat = Lit(Tex(act, "bark", Px.Bark(p, rng)));
            if (HasTile(act, "bark")) barkMat.SetColor("_BaseColor", new Color(0.5f, 0.42f, 0.38f)); else barkMat.SetColor("_BaseColor", new Color(0.8f, 0.72f, 0.7f));
            // 一枚絵の木 (2026-09-21 HD-2D 裁定「大物は一枚絵のドットに描き直す」): Art/props/act1_tree_*.png を板で立てる (影を落とす・幹の幅の接地影・根の敷物)。無ければ旧・立体の木
            var species = PropSet(act, "tree_oak", "tree_oak2", "tree_pine", "tree_pine2", "tree_birch", "tree_willow");
            var deadTree = PropTex(act, "tree_dead", null);
            var giantTree = PropTex(act, "tree_giant", null);
            var treeSpots = new List<Vector3>();
            System.Action<Vector3, float, int> tree = (pos, h, kind) =>
            {
                if (species.Count == 0) { Tree3D(pos, h, kind, rng, clumps, barkMat); return; }
                var tex = (deadTree != null && rng.NextDouble() < 0.1) ? deadTree : species[kind % species.Count];
                if (rng.NextDouble() < 0.5) tex = Pick(species, rng);
                float scale = h / (tex.height * Dot);                                   // 旧の高さの指定 (units) を絵のドット数に合わせた倍率へ
                Sprite("tree", tex, pos, scale, 0.32f, rng.NextDouble() < 0.5);
                treeSpots.Add(pos);
                if (_rootDecals != null && _rootDecals.Count > 0 && rng.NextDouble() < 0.35)
                {
                    var rp = new Vector3(pos.x + ((float)rng.NextDouble() - 0.5f) * 0.8f, 0f, pos.z - 0.25f); rp.y = GroundY(rp.x, rp.z) + 0.01f;
                    SpriteH("roots", Pick(_rootDecals, rng), rp, 0.5f + (float)rng.NextDouble() * 0.25f, 0f, rng.NextDouble() < 0.5);
                }
            };
            // 森 (2026-09-08「森じゃなくて道路じゃん」): 平地にも木を生やし、戦闘の場と手前・リーダーの頭上・塔の見える切れ目だけ空ける
            int placed = 0, tries = 0;
            while (placed < 120 && tries++ < 3200)
            {
                float x = -44f + (float)rng.NextDouble() * 88f, z = -8f + (float)rng.NextDouble() * 58f;
                float tt, ss; PathLocal(x, z, out tt, out ss);
                if (ss > -2.6f && ss < 4.6f && tt > -13f && tt < 17f) continue;      // 戦闘の場
                if (ss > -2.6f && ss < 6.0f && tt > -9.5f && tt < -1f) continue;     // リーダーの頭上 (吹き出し)
                if (ss < -2.6f && ss > -9f && tt > -12f && tt < 18f) continue;       // 場の手前 (キャラを隠さない)
                if (IsDirt(x, z)) continue;                                          // 道の上には生えない
                if (tt > 13f && tt < 26f && ss > 3f && ss < 13f && rng.NextDouble() < 0.7) continue;   // 道の先 = 塔が見える切れ目
                if (x > -1f && x < 30f && z > 13f && (z < 30f || rng.NextDouble() < 0.7)) continue;     // 月と坑口が見える切れ目 (右奥。上辺が梢で塞がっていた = レビュー 2026-09-21)。遠い影絵 (z>30) は 3 割残す
                float h = GroundY(x, z);
                float sc = 4.6f * (0.7f + (float)rng.NextDouble() * 0.9f) * (species.Count > 0 ? 1.35f : 1f);
                if (ss < -2.6f) sc *= 1.25f;                                         // 手前の木は大きい
                if (InStream(tt, ss)) continue;
                // 黒鉄の斧の刃の真後ろ (画面 x≈450〜610) に幹を立てない (2026-09-29 I25): t≈-3.3・s≈6.8 の小さな木の幹が刃の真後ろに立ち、刃が黒い円盤に見えていた。
                // 帯は刃の後ろだけに絞る (頭と胴の後ろの幹まで動かすと左の木立が抜けて森に囲まれた感じが消えた)。
                // continue で飛ばすと rng の消費が変わって森全体が組み変わるので、位置だけ左奥 (t-6・s+1 = 画面左端の大樹の木立) へずらす (tree() の rng の消費は位置に依らない)
                if (ss >= 6.5f && ss < 11f && tt > -4.2f && tt < -2.0f)
                {
                    var moved = Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(tt - 6f, 0f, ss + 1f);
                    x = moved.x; z = moved.z; h = GroundY(x, z);
                }
                tree(new Vector3(x, h, z), sc, rng.Next(3));
                placed++;
            }
            // 額縁と場の背後の大木 (近いので大きい = 森に囲まれている)
            tree(OnPath(-15.5f, 4.6f), 8.5f, 1); tree(OnPath(-12f, 10.4f), 7.0f, 0); tree(OnPath(15.5f, -4.6f), 7.6f, 2);
            tree(OnPath(-13.5f, -5.4f), 9.5f, 0); tree(OnPath(19.5f, 3.6f), 8.2f, 1); tree(OnPath(1f, 10.2f), 7.8f, 0);   // リーダーの真後ろ (t=-4) から右へ (幹がリーダーに重なっていた = レビュー)。t=-1 では根の張り出し (画面 x≈540〜625) が黒鉄の斧の刃のすぐ右に残り、刃と背後の差が +14 止まり → t=1 で奥の大木 (1.5,11.4) と一叢に (2026-09-29 I25)
            tree(OnPath(19f, 5f), 7.5f, 1); tree(OnPath(1.5f, 11.4f), 8.8f, 1);   // (7,10.2)・(12.5,4.6) は窓を塞いでいたので削除
            // 大樹 (2026-09-21): 場の背後の左に一本、根が場まで張る (画面の左上を埋める額縁)
            if (giantTree != null) { var gp = OnPath(-9f, 8.6f); Sprite("tree-giant", giantTree, gp, 1.15f, 0.34f); treeSpots.Add(gp); }
            // 月の映り込み: 川筋に沿って淡い銀の光の帯を寝かせる (面の反射はしない。遠目に「水が月を映している」だけ伝わればよい)
            for (float tt2 = -24f; tt2 <= 28f; tt2 += 2.6f)
            {
                var c = OnPath(tt2, StreamCenter(tt2));
                var g = Glow("moon-on-water", Px.Radial(new Color(0.75f, 0.85f, 1f, 0.2f)), new Vector3(c.x, c.y + 0.06f, c.z), 1f, 1f);   // 川の光の帯を弱く (座席の胴の高さで最も鮮鋭だった = レビュー)
                g.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
                g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                g.transform.localScale = new Vector3(3.6f, 1.9f, 1f);
            }
            // 小川の踏み石 (道が川を渡る所) と岸の葦
            var stepTex = PropTex(act, "stone", Px.Rock(p, rng)); var reed = PropTex(act, "reed", Px.TallGrass(p, rng));
            {
                float tCross = 13f;
                for (int k = 0; k < 4; k++)
                {
                    float sv = StreamCenter(tCross) - 1.05f + k * 0.7f;
                    var w = OnPath(tCross + (k % 2 == 0 ? 0.15f : -0.15f), sv);
                    var st = Plane("stepstone", stepTex, new Vector3(w.x, WaterLevel + 0.02f, w.z), 0.42f, 0.5f, false);
                    st.transform.rotation = Quaternion.Euler(90f, 0f, 0f); st.GetComponent<MeshFilter>().sharedMesh = _quadCentered; st.transform.localScale = new Vector3(0.7f, 0.5f, 1f);
                }
                for (int k = 0; k < 18; k++)
                {
                    float t = -16f + (float)rng.NextDouble() * 36f;
                    float sv = StreamCenter(t) + (rng.NextDouble() < 0.5 ? -1f : 1f) * (StreamHalf + 0.1f + (float)rng.NextDouble() * 0.5f);
                    var w = OnPath(t, sv);
                    var g = Plane("reed", reed, w, 0.9f + (float)rng.NextDouble() * 0.5f, 0.5f, false);
                    if (rng.NextDouble() < 0.5) g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f);
                }
                // 倒木と根 (空き地の縁)
                var log = PropTex(act, "log", null); var root = PropTex(act, "root", null);
                if (log2 != null) { SpriteH("log", log2, OnPath(-9.5f, -4.2f), 0.9f, 0.9f); SpriteH("log", log2, OnPath(16.5f, 5.0f), 0.8f, 0.9f, true); SpriteH("log", log2, OnPath(-20f, 6.5f), 0.75f, 0.9f); }
                else if (log != null) { var w = OnPath(-9.5f, -4.2f); Plane("log", log, w, 1.0f, 0.5f, true); var w2 = OnPath(16.5f, 5.0f); var g = Plane("log", log, w2, 0.9f, 0.5f, true); g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f); }
                if (stump != null) { SpriteH("stump", stump, OnPath(-7f, 5.2f), 0.8f, 0.8f); SpriteH("stump", stump, OnPath(13f, -5.6f), 0.75f, 0.8f, true); SpriteH("stump", stump, OnPath(22f, 6.0f), 0.8f, 0.8f); }
                if (root != null) { var w = OnPath(-13f, 3.6f); Plane("root", root, w, 0.7f, 0.5f, false); }
                if (root != null) { var w = OnPath(18.5f, -3.8f); Plane("root", root, w, 0.75f, 0.5f, false); }
            }
            // 下草: 羊歯を空き地の縁と段丘に (道の上は避ける)
            var fern = PropTex(act, "fern", Px.Fern(p, rng));
            for (int i = 0; i < (fern2 != null ? 84 : 56); i++)
            {
                float t = -18f + (float)rng.NextDouble() * 40f;
                float sv = rng.NextDouble() < 0.75 ? -5.6f + (float)rng.NextDouble() * 1.8f : 4.0f + (float)rng.NextDouble() * 0.5f;   // 奥は川を隠さないよう少なめ。手前は座席から離す (2026-09-21: 高い羊歯が敵を隠した)
                var w = OnPath(t, sv);
                if (IsDirt(w.x, w.z) || InStream(t, sv)) continue;
                if (fern2 != null && rng.NextDouble() < 0.5) { SpriteH("fern", fern2, w, 0.5f + (float)rng.NextDouble() * 0.3f, 0f, rng.NextDouble() < 0.5); continue; }
                var f = Plane("fern", fern, w, 0.7f + (float)rng.NextDouble() * 0.4f, 0.5f, false);
                if (rng.NextDouble() < 0.5) f.transform.localScale = new Vector3(-f.transform.localScale.x, f.transform.localScale.y, 1f);
            }
            // 木々の足元の下草 (2026-09-21): 木の根元に羊歯と茂みを寄せる = 幹が地面に刺さって見えない
            if (fern2 != null || bushes.Count > 0)
                foreach (var tp in treeSpots)
                {
                    float tt2, ss2; PathLocal(tp.x, tp.z, out tt2, out ss2);
                    if (ss2 > -3f && ss2 < 5f && tt2 > -14f && tt2 < 18f) continue;
                    if (rng.NextDouble() < 0.5) continue;
                    float ox = ((float)rng.NextDouble() - 0.5f) * 1.6f, oz = -0.25f - (float)rng.NextDouble() * 0.4f;
                    var w = new Vector3(tp.x + ox, GroundY(tp.x + ox, tp.z + oz), tp.z + oz);
                    if (fern2 != null && rng.NextDouble() < 0.6) SpriteH("fern", fern2, w, 0.5f + (float)rng.NextDouble() * 0.3f, 0f, rng.NextDouble() < 0.5);
                    else if (bushes.Count > 0) SpriteH("bush", Pick(bushes, rng), w, 0.8f + (float)rng.NextDouble() * 0.4f, 0.7f, rng.NextDouble() < 0.5);
                }
            // 頭上の枝葉 (額縁の上辺。中央は月と坑口のために空ける)
            var canopyArt = PropTexRaw(act, "canopy", null);
            if (canopyArt != null)
            {
                // 一枚絵の枝葉 (200×100 の垂れ下がる葉の塊)。上辺の両隅に大小を並べて垂らす = 額縁 (中央は月と坑口のために空ける)。手前 (z<0) なので座席より大きく写る
                float[] cx = { -13.5f, -8.0f, -3.4f, 9.5f, 14.5f }; float[] cy = { 2.4f, 2.9f, 3.6f, 3.1f, 2.5f }; float[] cs = { 1.15f, 1.0f, 0.8f, 0.95f, 1.1f };
                for (int i = 0; i < cx.Length; i++)
                {
                    var c = Plane("canopy", canopyArt, new Vector3(cx[i], cy[i], -1.5f + i * 0.08f), canopyArt.height * Dot * cs[i], 0.5f, false); c.transform.rotation = Quaternion.identity;
                    if (i % 2 == 1) c.transform.localScale = new Vector3(-c.transform.localScale.x, c.transform.localScale.y, 1f);
                    c.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", new Color(0.6f, 0.66f, 0.8f));   // 逆光の枝葉 = 暗く青く
                }
                if (vines != null)
                {
                    var v1 = Plane("vines", vines, new Vector3(-6.2f, 2.9f, -1.3f), vines.height * Dot * 0.8f, 0.5f, false); v1.transform.rotation = Quaternion.identity;
                    var v2 = Plane("vines", vines, new Vector3(10.6f, 3.4f, -1.1f), vines.height * Dot * 0.7f, 0.5f, false); v2.transform.rotation = Quaternion.identity; v2.transform.localScale = new Vector3(-v2.transform.localScale.x, v2.transform.localScale.y, 1f);
                }
            }
            else
            {
                var canopy = Px.Canopy(p, rng);
                { var c = Plane("canopy", canopy, new Vector3(-11f, 5.3f, 0.5f), 3.0f, 0.5f, false); c.transform.rotation = Quaternion.identity; }   // 上辺の両隅にだけ垂れる (画面を覆わない)
                { var c = Plane("canopy", canopy, new Vector3(11.5f, 5.5f, 0.2f), 2.8f, 0.5f, false); c.transform.rotation = Quaternion.identity; c.transform.localScale = new Vector3(-c.transform.localScale.x, c.transform.localScale.y, 1f); }
            }
            // 前景 (手前の低い地面・画面の下の隅): 大きな岩と丈の高い草
            var tall = Px.TallGrass(p, rng);
            if (bigRocks.Count > 0) { SpriteH("rock-front", bigRocks[0], OnPath(-3.2f, -7.6f), 1.5f, 0.9f); SpriteH("rock-front", bigRocks[bigRocks.Count - 1], OnPath(11.5f, -7.0f), 1.2f, 0.9f, true); }
            else { Plane("rock-front", rock, OnPath(-3.2f, -7.4f), 2.0f, 0.5f, true); Plane("rock-front", rock, OnPath(11.5f, -6.8f), 1.5f, 0.5f, true); }
            for (int i = 0; i < (tallGrass.Count > 0 ? 44 : 26); i++)
            {
                float t = -20f + (float)rng.NextDouble() * 42f;
                float sv = i % 2 == 0 ? -5.4f - (float)rng.NextDouble() * 2.2f : 4.6f + (float)rng.NextDouble() * 1.6f;
                if (tallGrass.Count > 0) { var w0 = OnPath(t, sv); if (InStream(t, sv)) continue; SpriteH("tallgrass", Pick(tallGrass, rng), w0, 0.65f + (float)rng.NextDouble() * 0.35f, 0f, rng.NextDouble() < 0.5); continue; }
                var g = Plane("tallgrass", tall, OnPath(t, sv), 1.5f + (float)rng.NextDouble() * 0.6f, 0.5f, false);
                if (rng.NextDouble() < 0.5) g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f);
            }
            // 茂み・岩: 戦闘ラインの外 (段丘の縁や手前)
            float[] bt = { -15f, -12f, 1f, 15f, 17f, 0.5f, 6f, -10f, 9f, -18f, -7f, 4f, 10f, 14f, -16f, 20f, -2f, 8f };
            float[] bs = { 5.6f, 7.4f, 9.6f, 5.2f, 7.8f, -6.4f, -7.2f, -6.0f, 8.8f, 7.2f, 5.0f, 5.4f, 6.6f, -5.4f, -4.6f, 6.2f, -4.8f, -5.6f };
            for (int i = 0; i < bt.Length; i++)
            {
                if (InStream(bt[i], bs[i])) continue;
                if (bushes.Count > 0) { SpriteH("bush", Pick(bushes, rng), OnPath(bt[i], bs[i]), 0.9f + (float)rng.NextDouble() * 0.5f, 0.75f, rng.NextDouble() < 0.5); continue; }
                var b = Plane("bush", bush, OnPath(bt[i], bs[i]), 0.9f + (float)rng.NextDouble() * 0.6f, 0.5f, true);
                if (rng.NextDouble() < 0.5) b.transform.localScale = new Vector3(-b.transform.localScale.x, b.transform.localScale.y, 1f);
            }
            if (bigRocks.Count > 0) { SpriteH("rock", Pick(bigRocks, rng), OnPath(-14.5f, 4.2f), 1.0f, 0.9f); SpriteH("rock", Pick(bigRocks, rng), OnPath(16.5f, 4.0f), 0.9f, 0.9f, true); }
            else { Plane("rock", rock, OnPath(-14.5f, 4.2f), 0.9f, 0.5f, true); Plane("rock", rock, OnPath(16.5f, 4.0f), 0.8f, 0.5f, true); }
            for (int i = 0; i < (smallRock != null ? 14 : 3); i++)
            {
                float x = -34f + (float)rng.NextDouble() * 68f, z = -6f + (float)rng.NextDouble() * 50f;
                float tt, ss; PathLocal(x, z, out tt, out ss);
                if (ss > -3f && ss < 7f && tt > -12f && tt < 16f) continue;
                if (InStream(tt, ss)) continue;
                if (smallRock != null && rng.NextDouble() < 0.7) SpriteH("rock", smallRock, new Vector3(x, GroundY(x, z), z), 0.3f + (float)rng.NextDouble() * 0.2f, 0.8f, rng.NextDouble() < 0.5);
                else if (bigRocks.Count > 0) SpriteH("rock", Pick(bigRocks, rng), new Vector3(x, GroundY(x, z), z), 0.7f + (float)rng.NextDouble() * 0.5f, 0.9f, rng.NextDouble() < 0.5);
                else Plane("rock", rock, new Vector3(x, GroundY(x, z), z), 0.6f + (float)rng.NextDouble() * 0.6f, 0.5f, true);
            }

            // 光の粒の足元: 群れの下に暖色の光溜まり (粒そのものは StageFx の Motes)。装置 (街灯) は置かない
            var lampBase = new Vector3(_lampPos.x, 0f, _lampPos.z);
            var pool = Glow("mote-pool", Px.Radial(new Color(1f, 0.78f, 0.46f, 0.18f)), lampBase, 1f, 1f);
            pool.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
            pool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pool.transform.position = lampBase + new Vector3(0f, 0.035f, 0f);
            pool.transform.localScale = new Vector3(6.0f, 5.2f, 1f);

            // 空 (遠い板) と月、地平線の木立
            var skyTex = Theme.Art("bg", "act" + act);
            var sky = Prop("sky", skyTex != null ? skyTex.texture : Px.Gradient(p.SkyBot, p.SkyTop), new Vector3(0f, -30f, 90f), 130f, 0f, 260f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Ambient", Color.white);
            sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var stars = Prop("stars", Px.Stars(rng), new Vector3(0f, 6f, 88f), 14f, 0.3f, 150f);
            stars.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            stars.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var moon = Prop("moon", Px.Disc(new Color(2.4f, 2.2f, 1.7f)), new Vector3(36f, 10.5f, 87.5f), 3.0f, 0.4f);   // 上端から 25px 下 = 円盤が全部見える (レビュー)。x36 = 場の背後の大木 (1.5,11.4) の梢 (画面 1120〜1500) の右・櫓の真上   // 幕1は小さな月。遠く高く (仰角 5°) = どの梢よりも上に出る。空の板 (z90) の手前
            moon.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            moon.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
            moon.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Ambient", Color.white);
            moon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Glow("moon-halo", Px.Glow(new Color(0.8f, 0.86f, 1f, 0.5f)), new Vector3(36f, 6.5f, 88.5f), 11f, 11f);   // 月の暈 = 月明かりが強い夜 (中心 = 月の中心)
            var mts = Prop("mountains", Px.Mountains(p, rng, 0.55f), new Vector3(4f, 2.6f, 58f), 6.5f, 0.4f, 200f);
            mts.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var mts2 = Prop("mountains2", Px.Mountains(p, rng, 0.35f), new Vector3(-10f, 2.9f, 48f), 4.5f, 0.4f, 150f);
            mts2.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // 街の輪郭 (平らな段) は梢の絵が無い時だけの代わり (2026-09-29 I26: 梢の後ろで y≈164〜219 の横一直線と右の平らな帯になり、梢の端と合わせて上中央に四角い濃紺の影を作っていた)。
            // 絵は梢があっても作って捨てる = rng の消費を変えない (後ろの坑口の櫓の形を変えない)
            var skylineTex = Px.Skyline(p, rng);
            var treeline = PropTexRaw(act, "treeline", null);
            if (treeline == null)
            {
                var skyline = Prop("skyline", skylineTex, new Vector3(0f, 3.0f, 36f), 2.4f, 0.4f, 120f);
                skyline.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            else UnityEngine.Object.Destroy(skylineTex);
            if (treeline != null)
            {
                // 遠い森の梢 (2026-09-21): 木々の後ろ・山の手前に霧の中の稜線を2枚 (ずらして重ねる = 一本の帯に見えない)。霧を半分受ける
                float tw = treeline.width * Dot * 2.6f, th = treeline.height * Dot * 2.6f;   // 遠い森の梢は近く大きく (窓を開けた芝の空白を梢で閉じる)
                var tl1 = Prop("treeline", treeline, new Vector3(-2f, 1.0f, 33f), th, 0.4f, tw); tl1.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.7f); tl1.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                // tl1 の右の続き (2026-09-29 I26): 絵の端は縦の直線で切れていて (列0・列199 が不透明)、tl1 の右端が画面の上中央 (PC x≈1248) に縦の切り口として出ていた。
                // 同じ深さ・同じ大きさで右へ2枚つなぎ、端を画面の外 (world x=50 = PC x≈2667・スマホ x≈2445) へ追い出す。
                // 1枚目は鏡像 (列199 と列199 が接する)・2枚目は正像 (列0 と列0 が接する) = 継ぎ目が出ない。絵のアルファは 0/255 の切り抜きなので端をぼかしても硬い線が内側へ動くだけ
                var tl1b = Prop("treeline", treeline, new Vector3(-2f + tw, 1.0f, 33f), th, 0.4f, tw); tl1b.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.7f); tl1b.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                tl1b.transform.localScale = new Vector3(-tw, th, 1f);
                var tl1c = Prop("treeline", treeline, new Vector3(-2f + tw * 2f, 1.0f, 33f), th, 0.4f, tw); tl1c.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.7f); tl1c.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                var tl2 = Prop("treeline", treeline, new Vector3(34f, 0.6f, 37f), th * 0.9f, 0.4f, tw * 0.9f); tl2.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.8f); tl2.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                tl2.transform.localScale = new Vector3(-tl2.transform.localScale.x, tl2.transform.localScale.y, 1f);
                var tl3 = Prop("treeline", treeline, new Vector3(-40f, 1.2f, 36f), th * 0.95f, 0.4f, tw * 0.95f); tl3.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.75f); tl3.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                foreach (var tl in new[] { tl1, tl1b, tl1c, tl2, tl3 }) tl.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", new Color(0.22f, 0.3f, 0.36f));   // 影絵 = 暗く青く (絵は昼の青緑)
            }
            // 坑口: 道の先 (奥右) に立つ木組みの櫓と、その足元の竪坑。世界観「マナ脈の坑を降りる」(2026-09-10 改稿。旧・古の塔を置換)。
            // 霧は半分だけ受けて山より暗く残す = 「これから降りる場所」が遠景の主役になる
            var pithead = Prop("pithead", Px.Headframe(p, rng), new Vector3(22f, 0.2f, 46f), 9.5f, 0.4f);   // 13 では頂上が上端の外だった (レビュー)
            pithead.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.45f);
            pithead.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            {   // 滑車は別の板にして回す (StageDriver が "rig-wheel" を回す)。櫓の絵の輪の位置 (128×176 の 64,163) に重ねる
                var wheel = Prop("rig-wheel", Px.Wheel(Color.Lerp(p.SkyTop, Color.black, 0.55f), 32), new Vector3(22f, 0.2f + 9.5f * 163f / 176f, 45.9f), 1.55f, 0.4f);
                wheel.GetComponent<MeshFilter>().sharedMesh = _quadCentered; wheel.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.45f); wheel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // ---------------------------------------------------------------- 箱庭をつなぐ (HD-2D 見本 P12・2026-09-30。計画 docs/design/hd2d-slice-plan-2026-09-30.md の P12)
        // 幕1 × stage=diorama の時だけ、Paint は今の舞台 (下の old の道筋) の代わりに Diorama (P04 の組み立て器) と StageLook (P09 の光の一式) で組む。
        // 幕2・3 と stage=old は今の舞台のまま (比べる元と戻り先)。old に戻る時は LeaveDiorama が光を控えへ戻してから今の舞台を描く

        /// <summary>この幕を箱庭で描くか (旗 stage=diorama・箱庭にする幕 dioramaacts・設計図の有無。段2 の口 2026-10-03。それまでは幕1 固定)</summary>
        static bool WantDiorama(int act) { return HD2DFlags.DioramaFor(act); }

        static readonly Dictionary<int, bool> _dioramaAssets = new Dictionary<int, bool>();
        static readonly HashSet<int> _dioramaFailed = new HashSet<int>();
        /// <summary>組めなかった幕の記録を忘れる (HD2DFlags.Reset = 撮影の一覧の行ごと。1 行の失敗で後ろの行の幕が今の舞台に落ちたままにならないように)</summary>
        public static void ForgetDioramaFailures() { _dioramaFailed.Clear(); }
        /// <summary>幕 act の箱庭の設計図 (Resources/Stage/act&lt;N&gt;_layout) と光の設計図 (look_act&lt;N&gt;) が両方あるか (幕ごとに1回だけ調べる)。
        /// 光の無い箱庭で判断を誤らないよう、片方だけの幕は今の舞台に落とす (移行の設計 §6 の 5)</summary>
        static bool DioramaAssetsReady(int act)
        {
            bool ok;
            if (_dioramaAssets.TryGetValue(act, out ok)) return ok;
            bool layout = Resources.Load<TextAsset>(Diorama.LayoutResource(act)) != null;
            bool look = Resources.Load<TextAsset>(StageLook.ResourceDir + StageLook.DefaultName(act)) != null;
            ok = layout && look;
            _dioramaAssets[act] = ok;
            if (!ok && act != 1) Debug.Log("[Stage] 幕" + act + " の箱庭は" + (!layout ? " 設計図 (Resources/" + Diorama.LayoutResource(act) + ")" : "") + (!look ? " 光の設計図 (" + StageLook.DefaultName(act) + ")" : "") + " が無い → 旗で入れても今の舞台 (old)");
            return ok;
        }

        /// <summary>箱庭の組み方の名札。組むのに使う旗 (幹 trunk=・光の設計図 look=・段 tier=) が変われば別の名札 = Paint が組み直す</summary>
        static string DioramaSignature(int act)
        {
            return "d|" + act + "|" + HD2DFlags.Trunk + "|" + (HD2DFlags.Look ?? "") + "|" + HD2DFlags.Tier;
        }

        /// <summary>
        /// 幕 act を箱庭で描く (計画 P12 の手順1): world を空にし、今の月とランタンを止め、_pal = PalOf(act) → StageLook.Apply → Diorama.Build →
        /// StageLook.ApplyMaterials (Build が材質を作り直すので後でもう一度) → 半立体の alpha-to-coverage → Diorama.OnCameraLayout → SetFxForAct の順。
        /// 設計図が無い・組むのに失敗したら false (呼び手の Paint が LeaveDiorama で片付けて今の舞台で描く)
        /// </summary>
        static bool PaintDiorama(int act, string sig)
        {
            var layout = Diorama.LoadLayout(act);
            if (layout == null) { Debug.LogError("[Stage] 箱庭の設計図が無い (Resources/" + Diorama.LayoutResource(act) + ") → 今の舞台 (old) で描く"); return false; }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Diorama.Clear();   // 前の箱庭を先に捨てる (下で world を空にする時に同じ物を2回捨てない)
            _paintedAct = act;
            _paintedSig = sig;
            _diorama = true;   // ここから GroundY は Diorama.GroundY (座席の帯は 0)
            _flat = false;
            for (int i = _world.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_world.GetChild(i).gameObject);   // 前の舞台の地形・小物・点光源・前の光の一式 (HD2D-LookRig)
            _seatPools.Clear(); _seatPoolN = -1;   // 座席の光溜まりも今消した子の中 (次の EnemySlots が置き直す)
            DisableReflection();
            if (_sun != null) _sun.enabled = false;          // 今の月 (Moonlight) とランタン (Lantern) を止める = 光は StageLook の一式 (月・舞台の灯・逆光) だけ
            if (_lantern != null) _lantern.enabled = false;
            _pal = PalOf(act);   // 見本でも StageUnits (litunits=0 の板)・StageDriver が _pal を読む (古い幕の値を残さない。審査1)
            try
            {
                StageLook.Apply(act, _world, _cam, Profile);   // 光の一式 (HD2D-LookRig) は world の下。old に戻る時は LeaveDiorama が Restore してから world を空にする
                var look = StageLook.Current ?? StageLook.Load(act);
                // 比べる用の別の設計図 (W3b P22): 光の設計図 (旗 look= で重ねた変種を含む) に "layout": "act1_layout_w3" などがあれば、その設計図で組む。無ければ既定
                var named = look != null && look.Raw != null && look.Raw["layout"] != null && look.Raw["layout"].Type == Newtonsoft.Json.Linq.JTokenType.String
                    ? Diorama.LoadLayoutNamed((string)look.Raw["layout"]) : null;
                if (named != null) layout = named;
                Diorama.Build(act, _world, look, new DioramaBuildOptions { Layout = layout, Log = false });
                if (!Diorama.Active || Diorama.Root == null) throw new Exception("Diorama.Build が箱庭を組めなかった");
                StageLook.ApplyMaterials();                     // Build が材質を作り直したので、設計図の受光と影の強さをもう一度
                Diorama.SetAlphaToCoverage(DioramaMsaaOn());    // MSAA の時だけ半立体と札の縁を alpha-to-coverage に (P04 の申し送り)
                LayoutCamera();   // 光が当たった後のカメラの同期 (P10 の LayoutCamera の後始末 = 霧と影の距離 × r・ぼかしの帯・影のカスケードの分割・額縁の置き直し)
                Diorama.OnCameraLayout(_camBase, LayoutRotation, _cam.fieldOfView);   // 額縁をレイアウトのカメラ (揺れ・寄り・漂いなし) の決まった位置へ (計画 P12 の手順1)
            }
            catch (Exception e)
            {
                Debug.LogError("[Stage] 箱庭を組めない → 今の舞台 (old) で描く: " + e);
                return false;
            }
            SetFxForAct(act);
            sw.Stop();
            _dioramaBuildMs = (float)sw.Elapsed.TotalMilliseconds;
            HD2DFlags.LayoutDumpers["diorama"] = DioramaDebugInfo;   // dumplayout=1 の layout.json の extra.diorama (部品数・座席の帯の高さ・見つからない物・組んだ時間)。額縁の矩形 extra.frames は P10 (StageCamera の DumpFrames)
            if (!_dioramaFlagsHooked) { _dioramaFlagsHooked = true; HD2DFlags.Changed += OnDioramaFlagsChanged; }
            LogDiorama(act);
            return true;
        }

        /// <summary>
        /// 今の舞台 (old) で描く前に: 箱庭だったなら、光の一式を控えへ戻し (RenderSettings.sun・環境光・霧・Volume・カメラ・URP のアセット・ぼかし)・
        /// 箱庭を捨て (メッシュ・テクスチャ・材質)・記録の口を外す。どちらの時も今の月とランタンを点ける (箱庭で止めた物)
        /// </summary>
        static void LeaveDiorama()
        {
            bool was = _diorama || Diorama.Active || StageLook.Active;
            _diorama = false;   // ここから GroundY は段丘の高さ場 (下の BuildTerrain が作り直す)
            if (was)
            {
                StageLook.Restore();
                Diorama.Clear();
                HD2DFlags.LayoutDumpers.Remove("diorama");
            }
            if (_sun != null) _sun.enabled = true;
            if (_lantern != null) _lantern.enabled = true;
        }

        /// <summary>箱庭に MSAA が掛かるか (StageLook.ApplyMsaa と同じ決め方: aa=msaa の時、標本数 = 旗の msaa と設計図の msaaMax の小さい方が 2 以上)。
        /// 旗から決める = HD2DFlags.Changed の購読の順に依らない</summary>
        static bool DioramaMsaaOn()
        {
            if (HD2DFlags.Aa != HD2DAa.Msaa) return false;
            int max = StageLook.Current != null ? StageLook.Current.Cam.MsaaMax : 8;
            return Mathf.Min(HD2DFlags.MsaaSamples, Mathf.Max(1, max)) > 1;
        }

        /// <summary>旗が変わった時 (aa=・msaa=): 箱庭なら半立体の alpha-to-coverage を当て直す (光の MSAA は StageLook が当て直す)。
        /// 舞台の組み方の旗 (stage=・trunk=・look=・tier=) は次の Paint が名札で見て組み直す</summary>
        static void OnDioramaFlagsChanged()
        {
            if (_diorama) Diorama.SetAlphaToCoverage(DioramaMsaaOn());
        }

        static object DioramaDebugInfo()
        {
            var o = Diorama.DebugInfo();
            o["sig"] = _paintedSig;
            o["buildMs"] = _dioramaBuildMs;
            return o;
        }

        /// <summary>組んだ箱庭の記録を1行 (部品数・三角形・Renderer・材質・座席の帯の高さ・種類ごとの数・見つからない物・光の設計図)。門を外れたら警告</summary>
        static void LogDiorama(int act)
        {
            var st = Diorama.LastStats;
            var sb = new System.Text.StringBuilder();
            sb.Append("[Stage] 箱庭 幕").Append(act).Append(" を組んだ ").Append(Mathf.RoundToInt(_dioramaBuildMs)).Append("ms (").Append(_paintedSig).Append(") ");
            sb.Append(st != null ? st.Summary() : "(点検なし)");
            if (st != null)
            {
                sb.Append(" | 種類");
                var keys = new List<string>(st.ByKind.Keys); keys.Sort(string.CompareOrdinal);
                foreach (var k in keys) sb.Append(' ').Append(k).Append('=').Append(st.ByKind[k]);
                if (st.Missing.Count > 0)
                {
                    int n = Math.Min(16, st.Missing.Count);
                    sb.Append(" | 見つからない物 ").Append(string.Join(", ", st.Missing.GetRange(0, n).ToArray()));
                    if (st.Missing.Count > n) sb.Append(" ほか").Append(st.Missing.Count - n);
                }
                if (st.IntrusionNames.Count > 0) sb.Append(" | 座席の帯の部品 ").Append(string.Join(", ", st.IntrusionNames.ToArray()));
            }
            var look = StageLook.Current;
            if (look != null) sb.Append(" | 光 ").Append(look.Name).Append(" [").Append(string.Join("+", look.Sources.ToArray())).Append(']');
            if (st == null || !st.Ok) Debug.LogWarning(sb.ToString()); else Debug.Log(sb.ToString());
        }

        /// <summary>粒子の幕別トグル: 蛍・水のきらめき・落ち葉・月の塵は森 (幕1) のもの。幕3 は月の塵だけ戻す。
        /// 箱庭 (幕1の見本) は小川が無いので水のきらめきを止め、粒の光のひな型 (mote-light-template) も点けない。
        /// 二周目 段2 (R2B): 箱庭で光の設計図 (look) の moondust.slice が true なら、今の moondust の代わりに箱庭だけの月の塵 moondust-slice を点ける</summary>
        static void SetFxForAct(int act)
        {
            if (_fx == null) return;
            bool dio = _diorama;
            S2B_RestoreFxHome();   // 段2 (S2B): 前の箱庭が fx の置き場 (pos・at) で動かした粒の系を元の置き場へ戻す (動かした物が無ければ何もしない = 今の舞台だけの起動は今まで)
            bool slice = dio && R2B_MoondustSliceLook() != null;   // 二周目 段2 (R2B): 箱庭だけの月の塵
            if (slice) R2B_MoondustSlice();                        // 無ければ作る (StageFx のいちばん後ろ)・値を設計図に合わせる
            // 三周目 直しの輪1 (2026-10-02・反証のまとめ (d)「舞う葉の緑のにじみ」): 箱庭では光の設計図 (look) の fx.leaves が false なら舞う葉を点けない。
            // 既定の look_act1.json は false・二周目と W5 の写し (look_act1_r2・look_act1_w5) は true = 写しの画は今まで。今の舞台 (stage=old)・幕2/3 は読まない = 今まで
            // 段2 の口 (2026-10-03): 箱庭では光の設計図の fx の表が勝つ (fx.<名前> が true/false ならそれ・書いていない名前は下の今の規則)。
            // 幕1 の look_act1.json の fx は leaves:false だけ = 今までの「fx.leaves が false なら舞う葉を点けない」と同じ値。今の舞台 (!dio) は読まない
            Newtonsoft.Json.Linq.JObject fxLook = null;
            if (dio)
            {
                var lookRaw = StageLook.Current != null ? StageLook.Current.Raw : null;
                fxLook = lookRaw != null ? lookRaw["fx"] as Newtonsoft.Json.Linq.JObject : null;
            }
            for (int i = 0; i < _fx.childCount; i++)
            {
                var c = _fx.GetChild(i);
                bool on = true;
                switch (c.name)
                {
                    case "fireflies": on = act == 1; break;
                    case "leaves": on = act == 1; break;
                    case "water-sparkle": on = act == 1 && !dio; break;
                    case "mote-light-template": on = !dio; break;   // 粒ごとの点光源のひな型。今の舞台は今までどおり (既定の on で点いている = 見た目を変えない)。箱庭では点けない (世界の原点に弱い暖色の点光源が1つ立っていた)
                    case "motes-cluster": on = !dio; break;   // 今の舞台のランタンの足元の暖色の粒の一群 (t −7.1 = 画面の左端)。箱庭ではランタンが無く、左端の地面だけが暖色に光って③ (中央÷端) を下げていた (W3 P22)
                    case "moondust": on = act != 2 && !slice; break;
                    case "moondust-slice": on = slice; break;   // 今の舞台では作られない (作られていても消す)
                    case "mist-far": on = act != 2; break;
                    case "vein-motes": on = act != 1; break;
                    case "drips": on = act != 1; break;
                    case "ashfall": case "wisps": on = act == 3; break;
                    case "embers": on = act == 2 && !dio; if (on) c.position = _emberPos; break;   // 箱庭では炉の位置 (_emberPos) を今の舞台が決めないので、光の設計図の fx が点けた時だけ
                }
                if (fxLook != null)
                {
                    var fv = fxLook[c.name];
                    if (fv != null && fv.Type == Newtonsoft.Json.Linq.JTokenType.Boolean) on = (bool)fv;
                    else if (fv is Newtonsoft.Json.Linq.JObject fo) on = S2B_FxObject(c, fo);   // 段2 (S2B・約束 §C2-4): 物の形 {"on", "pos": [t, s, y], "at"?}
                    // 段2 (S2B): 位置のある粒 (火の粉) は、箱庭では置き場が決まった時だけ点ける (bool の true だけでは、今の舞台の炉の位置か原点に出る)
                    if (on && S2B_NeedsPlace(c.name) && !_s2bFxPlaced.Contains(c))
                    {
                        on = false;
                        Debug.LogWarning("[Stage] 光の設計図の fx." + c.name + " は箱庭では置き場 (pos か at) が要る → 点けない");
                    }
                }
                c.gameObject.SetActive(on);
            }
        }

        // ---- 段2 (S2B・約束 §C2-4): 光の設計図の fx の物の形 (箱庭の枝だけ) ----
        static readonly Dictionary<Transform, Vector3> _s2bFxHome = new Dictionary<Transform, Vector3>();   // 動かした粒の系の元の置き場 (_fx の中の位置)
        static readonly HashSet<Transform> _s2bFxPlaced = new HashSet<Transform>();                      // この SetFxForAct で置き場を決めた粒の系

        /// <summary>位置のある粒 (出どころが1点の系)。箱庭では fx の置き場が決まった時だけ点く。箱の粒 (塵・霧・しずく) は書かなくても今の箱のまま</summary>
        static bool S2B_NeedsPlace(string name) { return name == "embers"; }

        /// <summary>
        /// fx.&lt;名前&gt; の物の形を読む: on (無ければ true) と、置き場 (pos [t, s, y]・y は絶対。at があれば設計図の部品に付いていく = StageLook.S2B_FxPlace)。
        /// 置き場を書いていれば粒の系をそこへ動かし (元の置き場を控える)、点けるかを返す。置き場が決まらない時、位置のある粒は点けない
        /// </summary>
        static bool S2B_FxObject(Transform c, Newtonsoft.Json.Linq.JObject fo)
        {
            var ot = fo["on"];
            bool on = ot == null || ot.Type == Newtonsoft.Json.Linq.JTokenType.Null
                || (ot.Type == Newtonsoft.Json.Linq.JTokenType.Boolean ? (bool)ot
                : (ot.Type == Newtonsoft.Json.Linq.JTokenType.Integer || ot.Type == Newtonsoft.Json.Linq.JTokenType.Float) && (float)ot != 0f);
            if (!on) return false;
            if (fo["pos"] == null && fo["at"] == null) return true;   // 置き場を書いていなければ点け消しだけ
            Vector3 w; string how;
            if (!StageLook.S2B_FxPlace(fo, out w, out how))
            {
                Debug.LogWarning("[Stage] 光の設計図の fx." + c.name + " の置き場が決まらない (at の部品が無く pos も無い)");
                return !S2B_NeedsPlace(c.name);
            }
            if (!_s2bFxHome.ContainsKey(c)) _s2bFxHome[c] = c.localPosition;
            c.position = w;
            _s2bFxPlaced.Add(c);
            return true;
        }

        /// <summary>前の SetFxForAct が動かした粒の系を元の置き場へ戻す (今の舞台へ戻る時・箱庭の組み直しの時)。動かした物が無ければ何もしない</summary>
        static void S2B_RestoreFxHome()
        {
            _s2bFxPlaced.Clear();
            if (_s2bFxHome.Count == 0) return;
            foreach (var kv in _s2bFxHome) if (kv.Key != null) kv.Key.localPosition = kv.Value;
            _s2bFxHome.Clear();
        }

        /// <summary>幕2 先代の坑道 (2026-09-10 改稿。旧「提灯の夜市」): 坑道の宿場跡。石の床・奥の石壁と暗い門・提灯の柱と吊り提灯 (暖色はここだけ)・屋台と樽と歯車。空は暗い天井</summary>
        static void PaintMarket(Pal p, System.Random rng, Material mFloor)
        {
            // 奥の壁 (石) と暗い門
            var wall = new MB();
            var wz = OnPath(0f, 9.5f).z; // 場の奥
            for (int seg = 0; seg < 3; seg++)
            {
                float z = 12f + seg * 9f; float h = 6f + seg * 1.5f;
                wall.WallZ(-46f, 60f, 0f, h, z);
                wall.Floor(-46f, z, 60f, z + 0.8f, h);
            }
            var mWall = Lit(Tex(_paintedAct, "stone", Px.Stone(p, rng))); mWall.SetColor("_BaseColor", new Color(0.42f, 0.36f, 0.34f));
            Solid("wall", wall, mWall);
            var dark = Px.Solid(new Color(0.03f, 0.02f, 0.04f));
            float[] gx = { -18f, -4f, 10f, 26f };
            for (int i = 0; i < gx.Length; i++)
            {
                var g = Prop("gate", dark, new Vector3(gx[i], 0f, 11.9f), 4.2f, 0.1f, 2.6f);
                g.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.6f);
            }
            // 支保工 (2026-09-10): 先代の坑匠が組んだ木の門型が道に沿って奥へ連なる = 坑道の背骨。
            // MB.Box は軸に平行なので、道の向きに寝かせる梁は小さな箱を並べて作る
            var mWood = Lit(Px.Solid(UiKit.Hex("#4a3a2c")));
            // 道の向き (PathYaw) に回した入れ物の中で組む = 梁が 1 本の箱で済む (2026-09-11 第2版。旧「小さな箱の連なり」は梁が凸凹に見えた)。
            // 手前の柱は戦闘の場 (t=-12〜14) の外だけに立てる = キャラが柱に隠れない (ユーザー「キャラと柱が被って見えなくなってる」)
            var timber = new MB();
            const float sNear = -5.2f, sFar = 6.6f;
            for (int i = 0; i < 9; i++)
            {
                float t = -22f + i * 5.6f;
                bool nearPost = t < -12f || t > 14f;
                timber.Box(t, 0f, sFar, 0.5f, 4.3f, 0.5f);                                         // 奥の柱 (全部)
                if (nearPost)
                {
                    timber.Box(t, 0f, sNear, 0.5f, 4.3f, 0.5f);                                    // 手前の柱
                    timber.Box(t, 4.3f, (sNear + sFar) * 0.5f, 0.42f, 0.42f, sFar - sNear);         // 道を跨ぐ梁
                    timber.Box(t, 3.4f, sNear + 0.9f, 0.3f, 0.3f, 1.8f);                            // 方杖 (角の補強)
                }
                else timber.Box(t, 4.3f, sFar - 2.2f, 0.42f, 0.42f, 4.4f);                          // 場の上には短い持ち送りだけ
                timber.Box(t, 3.4f, sFar - 0.9f, 0.3f, 0.3f, 1.8f);
            }
            Solid("timber", timber, mWood).transform.rotation = Quaternion.Euler(0f, PathYaw, 0f);

            // トロッコの軌道 (2026-09-10): 場の奥を道に沿って走る。枕木 + 二本のレール
            var mRail = Lit(Px.Solid(UiKit.Hex("#3a3a42")));
            var rails = new MB();
            for (float t = -26f; t <= 26f; t += 0.55f)
            {
                var r1 = OnPath(t, 8.1f); var r2 = OnPath(t, 9.3f);
                rails.Box(r1.x, 0.16f, r1.z, 0.34f, 0.12f, 0.34f);
                rails.Box(r2.x, 0.16f, r2.z, 0.34f, 0.12f, 0.34f);
            }
            for (float t = -26f; t <= 26f; t += 1.7f)   // 枕木
                for (int k = 0; k <= 6; k++)
                {
                    var w = OnPath(t, Mathf.Lerp(7.9f, 9.5f, k / 6f));
                    rails.Box(w.x, 0f, w.z, 0.4f, 0.16f, 0.4f);
                }
            Solid("rails", rails, mRail);

            // ---- ハイディテール (2026-09-11 ユーザー「2.3ステージをもっとハイディテールに幻想的に」) ----
            var veinC = new Color(0.42f, 0.95f, 0.86f);
            GameObject Halo(string nm, Vector3 center, float size, Color c) { var g = Glow(nm, Px.Glow(c), center, size, size); g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; return g; }
            // 岩の天井 (裏返しの床 = 下向きの面) と鍾乳石と垂れ根。カメラの上端 (水平+6°) には奥の天井だけが入る
            {
                var ceil = new MB(); ceil.Floor(64f, 4f, -50f, 40f, 6.2f);
                var mCeil = Lit(Tex(_paintedAct, "cliff", Px.Cliff(p, rng))); mCeil.SetColor("_BaseColor", new Color(0.3f, 0.26f, 0.26f));
                Solid("ceiling", ceil, mCeil);
                var stal = PropTexRaw(_paintedAct, "stalactite", Px.Stalactites(p, rng));
                for (int i = 0; i < 9; i++)
                {
                    var w = OnPath(-22f + i * 5.5f + ((float)rng.NextDouble() - 0.5f) * 3f, 4f + (float)rng.NextDouble() * 6f);
                    var g = Plane("stalactite", stal, new Vector3(w.x, 6.2f - 3.3f, w.z), 3.3f, 0.5f, false);
                    g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                var roots = PropTexRaw(_paintedAct, "roots", Px.HangingRoots(p, rng));
                for (int i = 0; i < 6; i++)
                {
                    var w = OnPath(-18f + i * 7.5f, 3f + (float)rng.NextDouble() * 5f);
                    var g = Plane("roots", roots, new Vector3(w.x, 6.2f - 2.8f, w.z), 2.8f, 0.5f, false);
                    g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            // マナの結晶: 壁の根元に群れ、奥の壁を割って大結晶 (幻想の主役)。点光源と暈を添える
            for (int i = 0; i < 7; i++)
            {
                float t = -21f + i * 7f + ((float)rng.NextDouble() - 0.5f) * 3f;
                float sv = (i % 2 == 0) ? 7.6f + (float)rng.NextDouble() : -6.6f - (float)rng.NextDouble() * 0.6f;
                if (sv < 0f && t > -12f && t < 14f) sv = 7.8f;   // 手前の真ん中はカメラに近いので奥へ
                var w = OnPath(t, sv);
                float hgt = 0.9f + (float)rng.NextDouble() * 0.9f;
                var cr = Prop("crystal", PropTexRaw(_paintedAct, "crystal", Px.Crystal(veinC, rng)), w, hgt, 0.5f);
                cr.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
                Halo("crystal-halo", w + new Vector3(0f, hgt * 0.45f, -0.2f), hgt * 2.2f, new Color(veinC.r, veinC.g, veinC.b, 0.5f));
                var lgo = new GameObject("crystal-light"); lgo.transform.SetParent(_world, false); lgo.transform.position = w + new Vector3(0f, hgt * 0.5f, 0f);
                var li = lgo.AddComponent<Light>(); li.type = LightType.Point; li.range = 4.5f + hgt * 2f; li.intensity = 0.9f + hgt * 0.5f; li.color = veinC; li.shadows = LightShadows.None;
            }
            {
                var w = OnPath(15f, 9.4f);
                var big = Prop("crystal-big", PropTexRaw(_paintedAct, "crystal", Px.Crystal(veinC, rng)), w, 4.2f, 0.5f);
                big.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
                Halo("crystal-halo", w + new Vector3(0f, 1.9f, -0.3f), 9f, new Color(veinC.r, veinC.g, veinC.b, 0.55f));
                var lgo = new GameObject("crystal-light"); lgo.transform.SetParent(_world, false); lgo.transform.position = w + new Vector3(0f, 2f, -0.5f);
                var li = lgo.AddComponent<Light>(); li.type = LightType.Point; li.range = 12f; li.intensity = 2.2f; li.color = veinC; li.shadows = LightShadows.None;
            }
            // 壁を走る脈: 奥の壁の面に細い光の筋
            {
                var seamTex = Px.Radial(new Color(veinC.r, veinC.g, veinC.b, 0.7f));
                for (int i = 0; i < 10; i++)
                {
                    float x = -30f + (float)rng.NextDouble() * 70f, y = 0.6f + (float)rng.NextDouble() * 4.5f;
                    var g = Glow("wall-vein", seamTex, new Vector3(x, y, 11.92f), 0.18f + (float)rng.NextDouble() * 0.12f, 1.6f + (float)rng.NextDouble() * 2.4f);
                    g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.rotation = Quaternion.Euler(0f, 0f, -35f + (float)rng.NextDouble() * 70f);
                }
            }
            // トロッコ: 軌道の上に 1 台 (鉱が光る)、遠くにもう 1 台
            {
                var cartTex = PropTexRaw(_paintedAct, "minecart", Px.MineCart(veinC, rng));
                var w1 = OnPath(4f, 8.7f); Plane("minecart", cartTex, w1 + new Vector3(0f, 0.12f, 0f), 1.15f, 0.5f, true);
                Halo("ore-glow", w1 + new Vector3(0f, 1.0f, -0.2f), 2.2f, new Color(veinC.r, veinC.g, veinC.b, 0.35f));
                var w2 = OnPath(-17f, 8.7f); var cart2 = Plane("minecart", cartTex, w2 + new Vector3(0f, 0.12f, 0f), 1.05f, 0.5f, true);
                cart2.transform.localScale = new Vector3(-cart2.transform.localScale.x, cart2.transform.localScale.y, 1f);
                for (int i = 0; i < 16; i++)   // こぼれた鉱のかけら
                {
                    var w = OnPath(-24f + (float)rng.NextDouble() * 48f, 7.4f + (float)rng.NextDouble() * 2.6f);
                    float sz = 0.12f + (float)rng.NextDouble() * 0.12f;
                    Glow("ore-bit", Px.Glow(new Color(veinC.r, veinC.g, veinC.b, 0.9f)), w + new Vector3(0f, 0.05f, 0f), sz, sz);
                }
            }
            // 上の桟橋: 奥の壁に沿う木の歩廊 (支柱・床板・手すり)
            {
                var gal = new MB();
                for (float t = -24f; t <= 24f; t += 0.5f) { var w = OnPath(t, 9.9f); gal.Box(w.x, 3.6f, w.z, 0.55f, 0.14f, 1.1f); }
                for (float t = -24f; t <= 24f; t += 4f) { var w = OnPath(t, 9.9f); gal.Box(w.x, 0f, w.z, 0.28f, 3.6f, 0.28f); gal.Box(w.x, 3.74f, w.z, 0.18f, 0.9f, 0.18f); }
                for (float t = -24f; t <= 24f; t += 0.5f) { var w = OnPath(t, 9.45f); gal.Box(w.x, 4.5f, w.z, 0.5f, 0.1f, 0.1f); }
                Solid("gallery", gal, mWood);
            }
            // 崩れた岩: 壁の根元の瓦礫
            {
                var rock = Px.Rock(p, rng);
                for (int i = 0; i < 10; i++)
                {
                    float sv = (i % 2 == 0) ? 8.4f + (float)rng.NextDouble() * 1.5f : -6.4f - (float)rng.NextDouble();
                    float t = -24f + (float)rng.NextDouble() * 50f;
                    if (sv < 0f && t > -12f && t < 14f) continue;
                    Prop("rubble", rock, OnPath(t, sv), 0.35f + (float)rng.NextDouble() * 0.4f, 0.5f);
                }
            }

            // 提灯の柱: 道の両脇に。暖色の点光源と足元の光溜まり
            var lantern = PropTex(_paintedAct, "lantern", null);
            var mIron = Lit(Px.Solid(UiKit.Hex("#2c2a30")));
            for (int i = 0; i < 6; i++)
            {
                float t = -15f + i * 6.4f; float sv = (t < -11f || t > 13f) ? -5.4f : 5.2f;   // 手前の柱は両端だけ (真ん中の手前はカメラに近くて画面を貫く)
                var w = OnPath(t, sv);
                var pole = new MB(); pole.Box(w.x, 0f, w.z, 0.14f, 3.2f, 0.14f); pole.Box(w.x, 0f, w.z, 0.5f, 0.12f, 0.5f); pole.Box(w.x + 0.25f, 3.1f, w.z, 0.6f, 0.08f, 0.08f);
                Solid("lantern-pole", pole, mIron);
                if (lantern != null) { var l = Plane("lantern", lantern, new Vector3(w.x + 0.5f, 2.35f, w.z - 0.02f), 0.9f, 0.5f, false); l.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); }
                var lgo = new GameObject("lantern-light"); lgo.transform.SetParent(_world, false); lgo.transform.position = new Vector3(w.x + 0.5f, 2.4f, w.z);
                var li = lgo.AddComponent<Light>(); li.type = LightType.Point; li.range = 5.5f; li.intensity = 1.6f; li.color = new Color(1f, 0.62f, 0.32f); li.shadows = LightShadows.None;
                var pool = Glow("lantern-pool", Px.Radial(new Color(1f, 0.7f, 0.4f, 0.3f)), new Vector3(w.x + 0.5f, 0.035f, w.z), 1f, 1f);
                pool.GetComponent<MeshFilter>().sharedMesh = _quadCentered; pool.transform.rotation = Quaternion.Euler(90f, 0f, 0f); pool.transform.localScale = new Vector3(4.6f, 4f, 1f);
                Glow("lantern-glow", Px.Glow(new Color(1f, 0.7f, 0.4f, 0.5f)), new Vector3(w.x + 0.5f, 1.9f, w.z - 0.3f), 2.2f, 2.2f);
            }
            // 吊り提灯の列: 道を横切る紐に 5 個ずつ (奥ほど高く小さく)
            if (lantern != null)
                for (int row = 0; row < 3; row++)
                {
                    float sv0 = 1.2f + row * 3.2f;
                    for (int k = 0; k < 6; k++)
                    {
                        float t = -12f + k * 5f + (row % 2) * 2.5f;
                        var w = OnPath(t, sv0);
                        var l = Plane("lantern-string", lantern, new Vector3(w.x, 3.6f + row * 0.3f, w.z), 0.55f, 0.5f, false);
                        l.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
                        Glow("lantern-glow", Px.Glow(new Color(1f, 0.66f, 0.36f, 0.35f)), new Vector3(w.x, 3.75f + row * 0.3f, w.z - 0.2f), 1.2f, 1.2f);
                    }
                }
            // 屋台・樽・木箱・歯車 (場の外)
            var stall = PropTex(_paintedAct, "stall", null); var barrel = PropTex(_paintedAct, "barrel", null); var crate = PropTex(_paintedAct, "crate", null); var gear = PropTex(_paintedAct, "gear", null);
            if (stall != null) { Plane("stall", stall, OnPath(-9f, 7.2f), 2.4f, 0.5f, true); Plane("stall", stall, OnPath(9f, 7.6f), 2.2f, 0.5f, true); var st3 = Plane("stall", stall, OnPath(20f, 6.4f), 2.0f, 0.5f, true); st3.transform.localScale = new Vector3(-st3.transform.localScale.x, st3.transform.localScale.y, 1f); }
            float[] bt = { -16f, -14.5f, -4f, 4f, 15f, 17f, 19f, -11f, 12f, -6f };
            float[] bs = { -5.2f, 5.6f, 6.4f, -5.4f, 5.4f, -5.0f, 6.8f, -6.2f, 6.9f, 6.1f };
            for (int i = 0; i < bt.Length; i++)
            {
                var w = OnPath(bt[i], bs[i]);
                var tex = (i % 3 == 0) ? crate : (i % 3 == 1) ? barrel : gear;
                if (tex == null) continue;
                var g = Plane(i % 3 == 0 ? "crate" : i % 3 == 1 ? "barrel" : "gear", tex, w, 0.8f + (float)rng.NextDouble() * 0.3f, 0.5f, true);
                if (rng.NextDouble() < 0.5) g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f);
            }
            // 光の粒の足元 (幕1と同じ群れの位置)
            var lampBase = new Vector3(_lampPos.x, 0f, _lampPos.z);
            var mp = Glow("mote-pool", Px.Radial(new Color(1f, 0.78f, 0.46f, 0.25f)), lampBase, 1f, 1f);
            mp.GetComponent<MeshFilter>().sharedMesh = _quadCentered; mp.transform.rotation = Quaternion.Euler(90f, 0f, 0f); mp.transform.position = lampBase + new Vector3(0f, 0.035f, 0f); mp.transform.localScale = new Vector3(5f, 4.4f, 1f);
            // 天井: 暗い岩天井の板 (空の代わり)
            var sky = Prop("sky", BgTex(_paintedAct, Px.Gradient(UiKit.Hex("#1a1014"), UiKit.Hex("#050305"))), new Vector3(0f, -30f, 90f), 130f, 0f, 260f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f); sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // 露頭 (2026-09-10。旧・高い窓からの月光を置換): 岩の割れ目から漏れるマナの光。
            // 光のルールどおり色で分ける = 暖色は提灯の範囲だけ、脈は青緑
            for (int i = 0; i < 5; i++)
            {
                var w = OnPath(-19f + i * 9.2f, 8.8f + (float)rng.NextDouble() * 1.4f);
                float hy = 1.1f + (float)rng.NextDouble() * 1.6f;
                Glow("vein-glow", Px.Glow(new Color(0.42f, 0.95f, 0.86f, 0.45f)), w + new Vector3(0f, hy, -0.25f), 2.4f, 2.4f);
                var vgo = new GameObject("vein-light"); vgo.transform.SetParent(_world, false); vgo.transform.position = w + new Vector3(0f, hy, 0f);
                var vl = vgo.AddComponent<Light>(); vl.type = LightType.Point; vl.range = 6f; vl.intensity = 0.8f; vl.color = new Color(0.4f, 0.95f, 0.9f); vl.shadows = LightShadows.None;
            }
            RenderSettings.fogStartDistance = 10f; RenderSettings.fogEndDistance = 40f;   // 坑道は近くから霞む (奥行き)
        }

        /// <summary>幕3 坑底の古代都市 (2026-09-10 改稿。旧「月の回廊」): 底の黒い石の街路。両脇の柱と鎖・跪く石像・古代の冷たい灯。奥に街の輪郭と今も回っている採掘機械。空は無く、脈の光が床の割れ目から立ち上る</summary>
        static void PaintCorridor(Pal p, System.Random rng, Material mFloor)
        {
            var pillar = PropTex(_paintedAct, "pillar", null); var chain = PropTex(_paintedAct, "chain", null); var statue = PropTex(_paintedAct, "statue", null); var brazier = PropTex(_paintedAct, "brazier", null);
            var mDark = Lit(Tex(_paintedAct, "stone", Px.Stone(p, rng))); mDark.SetColor("_BaseColor", new Color(0.2f, 0.22f, 0.3f));
            // 両脇の柱の列 (奥ほど霧に溶ける)。柱の間に鎖
            var pts = new List<Vector2>();
            for (int i = 0; i < 7; i++) if (i != 3 && i != 4) pts.Add(new Vector2(-16f + i * 5.4f, 5.8f));   // 奥の列 (場の後ろ)。3・4 本目は抜いて大門を見せる (2026-09-11 第2版)
            pts.Add(new Vector2(-20f, -4.8f)); pts.Add(new Vector2(22f, -4.6f));            // 両端の手前 (額縁。場の手前には立てない = キャラを隠さない)
            for (int i = 0; i < pts.Count; i++)
            {
                var w = OnPath(pts[i].x, pts[i].y);
                if (pillar != null) Plane("pillar", pillar, w, 5.6f, 0.5f, true);
                else { var mb = new MB(); mb.Box(w.x, 0f, w.z, 1.1f, 5.6f, 1.1f); Solid("pillar", mb, mDark); }
                if (chain != null && i % 2 == 1) Plane("chain", chain, new Vector3(w.x + 1.5f, 2.6f, w.z + 0.2f), 4.4f, 0.5f, false);
            }
            // 低い縁石 (回廊の縁) と奥の段
            var edge = new MB();
            foreach (float sv in new[] { -6.2f, 6.6f }) { var a = OnPath(-30f, sv); var b = OnPath(40f, sv); edge.Box((a.x + b.x) * 0.5f, 0f, (a.z + b.z) * 0.5f, 70f, 0.5f, 0.6f); }
            var step = OnPath(0f, 10f); edge.Box(step.x, 0f, step.z, 90f, 1.2f, 3f); edge.Box(step.x, 0f, step.z + 3f, 90f, 2.4f, 3f);
            Solid("corridor-edge", edge, mDark);
            // 石像と古代の灯 (冷たい青の火。暖色は無い = 人のいない場所)
            if (statue != null) { Plane("statue", statue, OnPath(-20f, 3.2f), 3.2f, 0.5f, true); var s2 = Plane("statue", statue, OnPath(22f, 3.4f), 3.2f, 0.5f, true); s2.transform.localScale = new Vector3(-s2.transform.localScale.x, s2.transform.localScale.y, 1f); }
            // 倒れた柱 (PixelLab の絵がある時だけ。場の外の縁石の向こう。2026-09-11)
            var fallen = PropTexRaw(_paintedAct, "pillar_fallen", null);
            if (fallen != null) { Plane("pillar-fallen", fallen, OnPath(-13f, 7.4f), 1.3f, 0.5f, true); var f2 = Plane("pillar-fallen", fallen, OnPath(11f, -7.2f), 1.2f, 0.5f, true); f2.transform.localScale = new Vector3(-f2.transform.localScale.x, f2.transform.localScale.y, 1f); }
            float[] bt = { -10f, 8f, 16f, -2f };
            for (int i = 0; i < bt.Length; i++)
            {
                var w = OnPath(bt[i], i % 2 == 0 ? -3.6f : 4.0f);
                if (brazier != null) { var b = Plane("brazier", brazier, w, 1.5f, 0.5f, true); b.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); }
                var lgo = new GameObject("brazier-light"); lgo.transform.SetParent(_world, false); lgo.transform.position = w + new Vector3(0f, 1.2f, 0f);
                var li = lgo.AddComponent<Light>(); li.type = LightType.Point; li.range = 5f; li.intensity = 1.1f; li.color = new Color(0.55f, 0.75f, 1f); li.shadows = LightShadows.None;
                Glow("brazier-glow", Px.Glow(new Color(0.6f, 0.8f, 1f, 0.45f)), w + new Vector3(0f, 1.2f, -0.3f), 2.4f, 2.4f);
            }
            // ---- ハイディテール (2026-09-11 ユーザー「2.3ステージをもっとハイディテールに幻想的に」) ----
            var veinC = new Color(0.42f, 0.95f, 0.86f);
            GameObject Halo(string nm, Vector3 center, float size, Color c) { var g = Glow(nm, Px.Glow(c), center, size, size); g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; return g; }
            // 天井を走る脈: 洞窟の天井に沿う光の帯が 2 本 (オーロラのように)
            {
                var a1 = Glow("aurora", Px.Aurora(veinC, rng), new Vector3(-4f, 10.5f, 40f), 5.5f, 70f);
                a1.GetComponent<MeshFilter>().sharedMesh = _quadCentered; a1.transform.rotation = Quaternion.Euler(-25f, 0f, -6f);
                var a2 = Glow("aurora", Px.Aurora(new Color(0.6f, 1f, 0.95f), rng), new Vector3(6f, 8.8f, 34f), 3.2f, 48f);
                a2.GetComponent<MeshFilter>().sharedMesh = _quadCentered; a2.transform.rotation = Quaternion.Euler(-25f, 0f, 34f);   // 1本目と交差させる
            }
            // 大結晶の尖塔: 街の左右に 3 本 (幻想の主役)。強い点光源
            {
                float[] ct = { -26f, 26f, -13f }; float[] cs = { 7.5f, 8.5f, 11.5f }; float[] ch = { 6.5f, 8.5f, 5f };
                for (int i = 0; i < ct.Length; i++)
                {
                    var w = OnPath(ct[i], cs[i]);
                    var cr = Prop("crystal-spire", PropTexRaw(_paintedAct, "spire", Px.Crystal(veinC, rng)), w, ch[i], 0.5f);
                    cr.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); cr.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.3f);
                    Halo("crystal-halo", w + new Vector3(0f, ch[i] * 0.5f, -0.4f), ch[i] * 2.4f, new Color(veinC.r, veinC.g, veinC.b, 0.4f));
                    var lgo = new GameObject("crystal-light"); lgo.transform.SetParent(_world, false); lgo.transform.position = w + new Vector3(0f, ch[i] * 0.45f, -1f);
                    var li = lgo.AddComponent<Light>(); li.type = LightType.Point; li.range = 10f + ch[i]; li.intensity = 1.8f; li.color = veinC; li.shadows = LightShadows.None;
                }
                for (int i = 0; i < 8; i++)   // 小さな結晶: 縁石の外
                {
                    var w = OnPath(-22f + i * 6.3f + ((float)rng.NextDouble() - 0.5f) * 2f, (i % 2 == 0) ? 6.9f : -6.6f);
                    float hgt = 0.6f + (float)rng.NextDouble() * 0.7f;
                    var cr = Prop("crystal", PropTexRaw(2, "crystal", Px.Crystal(veinC, rng)), w, hgt, 0.5f); cr.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);   // 小結晶は幕2の絵を共用
                    Halo("crystal-halo", w + new Vector3(0f, hgt * 0.45f, -0.2f), hgt * 2f, new Color(veinC.r, veinC.g, veinC.b, 0.45f));
                }
            }
            // 大門と水道橋: 奥の段の上に古代の門 (紋が光る)、その両脇に半円アーチの列
            {
                var gw = OnPath(3f, 19f);   // 第2版: 近すぎると半円がカメラの上端から切れる (z≈20 で y 7.3 まで入る)
                var arch = Prop("great-arch", PropTexRaw(_paintedAct, "gate", Px.Arch(p, veinC)), new Vector3(gw.x, 0.4f, gw.z), 6.6f, 0.4f);
                arch.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f); arch.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.35f);
                Halo("arch-glow", new Vector3(gw.x, 3.6f, gw.z - 0.3f), 5.5f, new Color(veinC.r, veinC.g, veinC.b, 0.22f));
                var arc = PropTexRaw(_paintedAct, "aqueduct", Px.Arcade(p, 9));
                var al = Prop("arcade", arc, new Vector3(gw.x - 20f, 0.6f, gw.z + 7f), 4.6f, 0.4f); al.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.5f);
                var ar = Prop("arcade", arc, new Vector3(gw.x + 22f, 0.6f, gw.z + 9f), 5.0f, 0.4f); ar.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.5f);
                var st = new MB(); var b = OnPath(3f, 16f);   // 門へ上る幅広の階段
                for (int k = 0; k < 5; k++) st.Box(b.x, k * 0.24f, b.z - k * 0.5f, 16f - k * 1.2f, 0.24f, 0.55f);
                Solid("great-stairs", st, mDark);
            }
            // 床の紋: 場の縁を囲む淡い光の線 (古代の刻印)
            {
                var rune = Px.Radial(new Color(veinC.r, veinC.g, veinC.b, 0.5f));
                for (int i = 0; i < 12; i++)
                {
                    float t = -14f + i * 2.6f;
                    foreach (float sv in new[] { 5.4f, -5.6f })
                    {
                        var w = OnPath(t + (sv < 0f ? 1.3f : 0f), sv);
                        var g = Glow("floor-rune", rune, new Vector3(w.x, 0.045f, w.z), 1f, 1f);
                        g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.rotation = Quaternion.Euler(90f, PathYaw, 0f); g.transform.localScale = new Vector3(2.2f, 0.12f, 1f);
                    }
                }
            }
            // 倒れた柱と傾いた石像 (打ち捨てられた街)
            if (pillar != null) { var f1 = Plane("pillar-fallen", pillar, OnPath(-9f, 8.6f) + new Vector3(2.4f, 0.3f, 0f), 5f, 0.5f, false); f1.transform.rotation = Quaternion.Euler(0f, 0f, 82f); }
            if (statue != null) { var s3 = Plane("statue", statue, OnPath(-4f, 12.5f), 2.6f, 0.5f, true); s3.transform.rotation = Quaternion.Euler(0f, 0f, -12f); }
            // 水没した街路: 左奥の低い所に黒い水面 (脈の光と窓明かりを映す)
            {
                var wp = Glow("flood", Px.Water(p), OnPath(-15f, -6.5f) + new Vector3(0f, 0.03f, 0f), 1f, 1f);   // 左手前の低い所 (第2版: 左奥では見えなかった)
                wp.GetComponent<MeshFilter>().sharedMesh = _quadCentered; wp.transform.rotation = Quaternion.Euler(90f, PathYaw, 0f); wp.transform.localScale = new Vector3(16f, 7f, 1f);
                var wm = wp.GetComponent<MeshRenderer>().sharedMaterial; wm.color = new Color(0.5f, 0.8f, 0.85f, 0.35f); _waterMat = wm;
                for (int i = 0; i < 6; i++)
                {
                    var h = Halo("flood-reflect", OnPath(-21f + i * 2.4f, -8.5f + (float)rng.NextDouble() * 3.5f) + new Vector3(0f, 0.06f, 0f), 1.6f, new Color(veinC.r, veinC.g, veinC.b, 0.22f));
                    h.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }
            // 鎖に吊られた古代の灯: 天井から (冷たい青の火)
            if (chain != null)
                for (int i = 0; i < 5; i++)
                {
                    var w = OnPath(-16f + i * 8f, 3f + (float)rng.NextDouble() * 4f);
                    var c = Plane("chain-hang", chain, new Vector3(w.x, 4.8f, w.z), 4.2f, 0.5f, false); c.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    Halo("hang-flame", new Vector3(w.x, 4.6f, w.z - 0.2f), 1.1f, new Color(0.6f, 0.85f, 1f, 0.6f));
                    var lgo = new GameObject("hang-light"); lgo.transform.SetParent(_world, false); lgo.transform.position = new Vector3(w.x, 4.6f, w.z);
                    var li = lgo.AddComponent<Light>(); li.type = LightType.Point; li.range = 4f; li.intensity = 0.8f; li.color = new Color(0.55f, 0.8f, 1f); li.shadows = LightShadows.None;
                }

            // 光の粒の足元
            var lampBase = new Vector3(_lampPos.x, 0f, _lampPos.z);
            var mp = Glow("mote-pool", Px.Radial(new Color(1f, 0.78f, 0.46f, 0.22f)), lampBase, 1f, 1f);
            mp.GetComponent<MeshFilter>().sharedMesh = _quadCentered; mp.transform.rotation = Quaternion.Euler(90f, 0f, 0f); mp.transform.position = lampBase + new Vector3(0f, 0.035f, 0f); mp.transform.localScale = new Vector3(5f, 4.4f, 1f);
            // 岩天井と、脈の光にぼんやり浮かぶ古代都市 (2026-09-10。旧・空を埋める月と月光の帯を置換。ここは坑の底なので空は無い)
            var sky = Prop("sky", BgTex(_paintedAct, Px.Gradient(UiKit.Hex("#08181c"), UiKit.Hex("#02070a"))), new Vector3(0f, -30f, 90f), 130f, 0f, 260f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f); sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // 地平の脈の光: 街の輪郭が黒く抜けて読めるように、街の後ろに広い青緑の光の帯を敷く (旧世界の月の代わりの明るい背景)
            Glow("vein-horizon", Px.Radial(new Color(0.35f, 0.9f, 0.82f, 0.7f)), new Vector3(4f, 2.5f, 66f), 22f, 170f);
            Glow("vein-horizon2", Px.Radial(new Color(0.45f, 1f, 0.9f, 0.45f)), new Vector3(-18f, 2f, 64f), 14f, 80f);
            var city = Prop("city-far", Px.Skyline(p, rng), new Vector3(2f, 1.4f, 58f), 13f, 0.3f, 170f);       // 街の輪郭 (奥。手前の段と柱の上に頭が出る高さ)
            city.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.55f); city.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var city2 = Prop("city-near", Px.Skyline(p, rng), new Vector3(-14f, 1.0f, 46f), 9f, 0.3f, 120f);    // 街の輪郭 (中景)
            city2.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.4f); city2.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // 止まらない採掘機械: 街の向こうで今も回っている櫓 (幕1の坑口と同じ形 = 「同じものが底にもある」)
            var rig = Prop("rig", Px.Headframe(p, rng), new Vector3(18f, 0.6f, 50f), 14f, 0.4f);
            rig.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.5f); rig.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            {   // 滑車を回す (幕1の坑口と同じ仕掛け。誰も止めなかった機械が今も回っている)
                var wheel = Prop("rig-wheel", Px.Wheel(Color.Lerp(p.SkyTop, Color.black, 0.5f), 32), new Vector3(18f, 0.6f + 14f * 163f / 176f, 49.9f), 2.3f, 0.4f);
                wheel.GetComponent<MeshFilter>().sharedMesh = _quadCentered; wheel.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.5f); wheel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // 街の窓明かり (脈の色。誰もいないのに灯っている)
            for (int i = 0; i < 14; i++)
            {
                float wx = -34f + (float)rng.NextDouble() * 70f, wy = 0.8f + (float)rng.NextDouble() * 6f, wz = 44f + (float)rng.NextDouble() * 16f;
                Glow("city-window", Px.Glow(new Color(0.4f, 0.9f, 0.85f, 0.3f)), new Vector3(wx, wy, wz), 2.2f, 2.2f);
            }
            // 脈の光: 床の割れ目から立ち上る (旧・月光の柱の置換。ここが世界でいちばん脈が濃い)
            var beam = Px.Beam(new Color(0.5f, 1f, 0.92f));
            float[] bx = { -8f, 4f, 16f }; float[] bz = { 10f, 12f, 9f };
            for (int i = 0; i < bx.Length; i++) { var b = Glow("vein-beam", beam, new Vector3(bx[i], 0.3f, bz[i]), 22f, 6f); b.transform.rotation = Quaternion.Euler(0f, 0f, -10f); }
            for (float tt2 = -20f; tt2 <= 24f; tt2 += 3f)
            {
                var c = OnPath(tt2, 0.4f);
                var g = Glow("vein-on-floor", Px.Radial(new Color(0.45f, 0.95f, 0.88f, 0.16f)), new Vector3(c.x, 0.04f, c.z), 1f, 1f);
                g.GetComponent<MeshFilter>().sharedMesh = _quadCentered; g.transform.rotation = Quaternion.Euler(90f, 0f, 0f); g.transform.localScale = new Vector3(4.5f, 3f, 1f);
            }
            RenderSettings.fogStartDistance = 14f; RenderSettings.fogEndDistance = 58f;
        }

        static bool HasTile(int act, string kind) { return Theme.Art("tiles", "act" + act + "_" + kind) != null; }
        /// <summary>Art/props/act&lt;N&gt;_&lt;name&gt;.png があればそれ、無ければ fallback (透明率の検査なし。結晶・大門など塗りの多い絵用。2026-09-11)</summary>
        static Texture2D PropTexRaw(int act, string name, Texture2D fallback)
        {
            var sp = Theme.Art("props", "act" + act + "_" + name);
            return sp != null ? sp.texture : fallback;
        }
        /// <summary>幕の背景の板 (Art/bg/act&lt;N&gt;.png・384×216)。無ければ fallback のグラデーション</summary>
        static Texture2D BgTex(int act, Texture2D fallback)
        {
            var sp = Theme.Art("bg", "act" + act);
            return sp != null ? sp.texture : fallback;
        }
        /// <summary>Art/props/act<N>_<name>.png があればそれ、無ければ仮の絵 (null なら置かない)</summary>
        static readonly Dictionary<string, bool> _propOk = new Dictionary<string, bool>();
        static Texture2D PropTex(int act, string name, Texture2D fallback)
        {
            var sp = Theme.Art("props", "act" + act + "_" + name);
            if (sp == null) return fallback;
            string key = "act" + act + "_" + name;
            bool ok;
            if (!_propOk.TryGetValue(key, out ok))
            {
                ok = false;
                try
                {
                    var px = sp.texture.GetPixels32(); int tr = 0;
                    for (int i = 0; i < px.Length; i++) if (px[i].a < 16) tr++;
                    ok = tr >= px.Length * 0.45f;                                   // 透明が 3 割未満 = 風景ごと描かれた絵 → 使わない
                }
                catch (System.Exception) { ok = false; }
                _propOk[key] = ok;
            }
            return ok ? sp.texture : fallback;
        }

        static Texture2D Tex(int act, string kind, Texture2D fallback)
        {
            // Art/tiles/act<N>_<kind>.png があればそれを使う (PixelLab のタイル。ArtImporter が Repeat にする)
            var s = Theme.Art("tiles", "act" + act + "_" + kind);
            return s != null ? s.texture : fallback;
        }

        /// <summary>地面の材質 (2026-09-21 HD-2D): Art/tiles/act&lt;N&gt;_&lt;kind&gt;_{a,b,c,d}.png の 4 種を 2×2 のアトラスに束ね、タイルごとに種と向きを変えて繰り返しを消す。
        /// 明度を高さと読んだノーマルマップで月光の陰影 (草の房・石の角) を出す。4 種が無ければ act&lt;N&gt;_&lt;kind&gt;.png 1 種 (Repeat) のまま。
        /// variants = アトラスの種の数 (0 = 1 種の敷き詰め)</summary>
        static Material Ground(int act, string kind, Texture2D fallback, out int variants) { return Ground(act, kind, fallback, out variants, 0.9f); }
        static Material Ground(int act, string kind, Texture2D fallback, out int variants, float normalStrength)
        {
            var list = new List<Texture2D>();
            foreach (var sfx in new[] { "a", "b", "c", "d" }) { var t = Tex(act, kind + "_" + sfx, null); if (t != null) list.Add(t); }
            if (list.Count < 2) { variants = 0; var m0 = Lit(list.Count == 1 ? list[0] : Tex(act, kind, fallback)); return m0; }   // 1 種 (act<N>_<kind>_a.png) は Repeat で敷く (継ぎ目が出ない)
            while (list.Count < 4) list.Add(list[list.Count % Mathf.Max(1, list.Count)]);
            int w = list[0].width, h = list[0].height;
            var atlas = new Texture2D(w * 2, h * 2, TextureFormat.RGBA32, true);
            atlas.name = "ground-atlas-" + kind; atlas.filterMode = FilterMode.Bilinear; atlas.wrapMode = TextureWrapMode.Clamp;   // 2px/ドットなのでバイリニアでも粒は残り、遠くのちらつき (点サンプル×ミップ) が消える
            Vector3 mean0 = Vector3.zero;
            for (int v = 0; v < 4; v++)
            {
                var src = list[v];
                if (src.width != w || src.height != h) { src = list[0]; }
                var px = src.GetPixels32();
                // 種ごとの平均色を 1 種目に揃える (種の違いがタイルの格子に見えない。2026-09-21)
                Vector3 mean = Vector3.zero; for (int i = 0; i < px.Length; i++) mean += new Vector3(px[i].r, px[i].g, px[i].b); mean /= Mathf.Max(1, px.Length);
                if (v == 0) mean0 = mean;
                else
                {
                    var k = new Vector3(mean0.x / Mathf.Max(1f, mean.x), mean0.y / Mathf.Max(1f, mean.y), mean0.z / Mathf.Max(1f, mean.z));
                    k = Vector3.Lerp(Vector3.one, k, 0.8f);
                    for (int i = 0; i < px.Length; i++) px[i] = new Color32((byte)Mathf.Clamp(px[i].r * k.x, 0f, 255f), (byte)Mathf.Clamp(px[i].g * k.y, 0f, 255f), (byte)Mathf.Clamp(px[i].b * k.z, 0f, 255f), 255);
                }
                for (int i = 0; i < px.Length; i++) { float g = px[i].r * 0.3f + px[i].g * 0.59f + px[i].b * 0.11f; px[i] = new Color32((byte)(g + (px[i].r - g) * 0.78f), (byte)(g + (px[i].g - g) * 0.78f), (byte)(g + (px[i].b - g) * 0.78f), 255); }   // 彩度 -22% (夜)
                atlas.SetPixels32((v % 2) * w, (v / 2) * h, w, h, px);
            }
            atlas.Apply(true, false);
            variants = 4;
            var m = Lit(atlas);
            var nrm = NormalFromLuma(atlas, normalStrength);
            m.SetTexture("_BumpMap", nrm); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP");
            return m;
        }

        /// <summary>明度から高さを読んでノーマルマップを作る (3×3 でならしてから Sobel)。RGB に法線・alpha 1 = URP の UnpackNormal (RG or AG) がそのまま読める</summary>
        static Texture2D NormalFromLuma(Texture2D src, float strength)
        {
            int w = src.width, h = src.height;
            var px = src.GetPixels32();
            var lum = new float[w * h];
            for (int i = 0; i < px.Length; i++) lum[i] = (px[i].r * 0.3f + px[i].g * 0.59f + px[i].b * 0.11f) / 255f;
            var sm = new float[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float a = 0f;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) a += lum[((y + dy + h) % h) * w + (x + dx + w) % w];
                sm[y * w + x] = a / 9f;
            }
            var outPx = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float l = sm[y * w + (x - 1 + w) % w], r = sm[y * w + (x + 1) % w], d = sm[((y - 1 + h) % h) * w + x], u = sm[((y + 1) % h) * w + x];
                var n = new Vector3(-(r - l) * strength, -(u - d) * strength, 1f).normalized;
                outPx[y * w + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
            }
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true, true);
            t.name = src.name + "-normal"; t.filterMode = FilterMode.Bilinear; t.wrapMode = src.wrapMode;
            t.SetPixels32(outPx); t.Apply(true, false);
            return t;
        }

        static Material Lit(Texture2D tex)
        {
            var m = new Material(_dioramaBase);
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            m.SetFloat("_Smoothness", 0f);
            m.SetFloat("_Metallic", 0f);
            m.SetColor("_BaseColor", Color.white);
            return m;
        }

        /// <summary>光を受け影を落とす抜き板の材質 (地面の斑・小石)</summary>
        static Material Cutout(Texture2D tex)
        {
            var m = new Material(_cutoutBase);
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            m.SetFloat("_Smoothness", 0f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Cutoff", 0.5f);
            m.SetFloat("_Cull", 0f);
            return m;
        }

        static Material GlowMaterial(Texture2D tex)
        {
            var baseMat = Resources.Load<Material>("Materials/ParticleSprite");
            var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Sprites/Default"));
            m.mainTexture = tex;
            m.renderQueue = 3000;
            return m;
        }

        static GameObject Solid(string name, MB mb, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.AddComponent<MeshFilter>().sharedMesh = mb.Build();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        static Texture2D StripTex()
        {
            if (_stripTex != null) return _stripTex;
            _stripTex = new Texture2D(4, 32, TextureFormat.RGBA32, false);
            _stripTex.filterMode = FilterMode.Bilinear; _stripTex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[128];
            for (int y = 0; y < 32; y++) { float a = 1f - Mathf.Abs(y - 15.5f) / 15.5f; for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(1f, 1f, 1f, a * a); }
            _stripTex.SetPixels(px); _stripTex.Apply();
            return _stripTex;
        }

        static void Terrace(float x0, float z0, float x1, float z1, float h, Material top, Material side, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var t = new MB(); t.Floor(x0, z0, x1, z1, h); Solid("terrace-top", t, top).transform.rotation = rot;
            var s = new MB(); s.WallZ(x0, x1, 0f, h, z0); Solid("terrace-side", s, side).transform.rotation = rot;
            // 崖の根元の接地の影 (壁から手前へ薄れる帯)
            var strip = new GameObject("terrace-shadow");
            strip.transform.SetParent(_world, false);
            strip.AddComponent<MeshFilter>().sharedMesh = _quadCentered;
            var mr = strip.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GlowMaterial(StripTex());
            mr.sharedMaterial.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, 0.95f);
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            strip.transform.rotation = rot * Quaternion.Euler(90f, 0f, 0f);
            strip.transform.position = rot * new Vector3((x0 + x1) * 0.5f, 0.03f, z0);   // 中心を根元の線に
            strip.transform.localScale = new Vector3(x1 - x0, 6.0f, 1f);
        }

        static void Pillar(float x, float y, float z, float w, float h, Material mat)
        {
            var mb = new MB();
            mb.Box(x, y, z, w, h, w);
            mb.Box(x, y + h, z, w * 1.3f, 0.3f, w * 1.3f);      // 笠石
            Solid("pillar", mb, mat);
            Blob("shadow-pillar", _world, new Vector3(x, y, z), w * 1.6f);
        }

        static readonly Dictionary<string, Material> _cutoutCache = new Dictionary<string, Material>();
        static Color _decalTint = Color.white;   // 敷物の色 = 下の地面の _BaseColor に結ぶ (敷物が地面の 2 倍明るい白斑にならない = レビュー 2026-09-21)
        static Material CutoutCached(Texture2D tex)
        {
            Material m; string key = tex.name + "#" + tex.GetHashCode() + ":" + _decalTint;
            if (_cutoutCache.TryGetValue(key, out m) && m != null) return m;
            m = Cutout(tex); m.SetColor("_BaseColor", _decalTint); _cutoutCache[key] = m; return m;
        }

        /// <summary>地面の敷物 (2026-09-21 HD-2D): PixelLab の抜き板 (落ち葉・苔・根・小石・花・水たまり) を地面の粒 (1ドット=2px) で寝かせる。
        /// lift = 重なる敷物の前後 (z-fighting を避ける段)。flip で左右を返す</summary>
        static GameObject GroundDecal(string name, Texture2D tex, float x, float z, float scale, float rotDeg, float lift, bool flip = false)
        {
            float w = tex.width * GroundDot * scale, d = tex.height * GroundDot * scale;
            // 道の帯の上の敷物は、下の地面と同じだけ明るくする (F50。敷物の色は下の地面の _BaseColor に結ぶ = _decalTint の約束を帯の中でも守る)
            var tint0 = _decalTint; float bandGain = PathBandGainAt(x, z);
            if (bandGain != 1f) _decalTint = new Color(tint0.r * bandGain, tint0.g * bandGain, tint0.b * bandGain, tint0.a);
            var g = Decal(name, CutoutCached(tex), x, z, w, d, rotDeg);
            _decalTint = tint0;
            g.transform.position += new Vector3(0f, lift, 0f);
            if (flip) g.transform.localScale = new Vector3(-w, d, 1f);
            return g;
        }

        /// <summary>立て板の一枚絵 (木・茂み・岩・切り株)。大きさは絵のドット数 × 0.04 (キャラと同じ粒) × scale。接地影の幅は絵の幅 × blobK (木の幹は細い)</summary>
        static GameObject Sprite(string name, Texture2D tex, Vector3 pos, float scale, float blobK, bool flip = false)
        {
            float h = tex.height * Dot * scale;
            var g = Plane(name, tex, pos, h, 0.5f, false);
            float w = h * tex.width / (float)tex.height;
            if (flip) g.transform.localScale = new Vector3(-w, h, 1f);
            if (blobK > 0f) Blob("shadow-" + name, _world, pos, w * blobK);
            return g;
        }

        /// <summary>材質タイルをくり抜いた敷物 (2026-09-21 HD-2D): PixelLab は「地面に寝かせた抜き板」を頼むと浮島や台座を描くので、
        /// 材質 (落ち葉・苔・砂利・ひび・泥・花・小枝) だけをタイルで描かせ、ここで不定形の輪郭にくり抜く。輪郭は楕円＋ノイズで、縁は1ドットのディザ (ドット絵の縁)。
        /// edge=true は道の縁の帯: 上の 45% は塗り、下へ向かってノイズでちぎれて消える (草が土へ食い込む舌)。同じタイルから切るので地面と色が揃う</summary>
        static Texture2D MaterialPatch(Texture2D tile, int w, int h, int seed, bool edge)
        {
            var src = tile.GetPixels32(); int tw = tile.width, th = tile.height;
            var px = new Color32[w * h];
            var rng = new System.Random(seed);
            int ox = rng.Next(tw), oy = rng.Next(th);
            float n1 = (float)rng.NextDouble() * 100f, n2 = (float)rng.NextDouble() * 100f;
            float rot = (float)rng.NextDouble() * Mathf.PI;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = src[((y + oy) % th) * tw + (x + ox) % tw];
                    {   // 夜の色に寄せる: 彩度 -30%・明るさ -12% (PixelLab の材質は昼の色で来る)。幕2/3 の床は暗いので敷物はさらに沈める (床の 2 倍明るい白斑にしない = レビュー)
                        float g = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f; float k = 0.9f;   // 明るさは材質の _BaseColor (= 地面の色 _decalTint) が担う
                        c = new Color32((byte)((g + (c.r - g) * 0.7f) * k), (byte)((g + (c.g - g) * 0.7f) * k), (byte)((g + (c.b - g) * 0.7f) * (k + 0.02f)), c.a);
                    }
                    float fx = (x + 0.5f) / w * 2f - 1f, fy = (y + 0.5f) / h * 2f - 1f;
                    float m;
                    if (edge)
                    {
                        // v=1 (上) が塗り・v=0 (下) が消える。境目をノイズで揺らす (幅の 55% あたり)
                        float v = (y + 0.5f) / h;
                        float wob = (Mathf.PerlinNoise(x * 0.11f + n1, n2) - 0.5f) * 0.55f + (Mathf.PerlinNoise(x * 0.31f + n2, n1) - 0.5f) * 0.25f;
                        m = (v - (0.45f + wob)) * 3.2f;
                    }
                    else
                    {
                        float rx = fx * Mathf.Cos(rot) - fy * Mathf.Sin(rot), ry = fx * Mathf.Sin(rot) + fy * Mathf.Cos(rot);
                        float r = Mathf.Sqrt(rx * rx * 1.15f + ry * ry * 0.85f);
                        float ang = Mathf.Atan2(ry, rx);
                        float wob = (Mathf.PerlinNoise(Mathf.Cos(ang) * 1.6f + n1 + 5f, Mathf.Sin(ang) * 1.6f + n2 + 5f) - 0.5f) * 0.7f
                                  + (Mathf.PerlinNoise(x * 0.18f + n2, y * 0.18f + n1) - 0.5f) * 0.45f;
                        m = (0.92f + wob - r) * 3.5f;
                    }
                    // 縁のディザ: m が 0 付近の帯だけ市松で抜く (滑らかな alpha でなく 1 ドットのぎざぎざ)
                    bool solid = m > 0.18f || (m > -0.12f && ((x + y) & 1) == 0) ;
                    px[y * w + x] = solid ? c : new Color32(0, 0, 0, 0);
                }
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.name = tile.name + "-patch" + seed; t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            t.SetPixels32(px); t.Apply(false, false);
            return t;
        }

        static readonly Dictionary<string, List<Texture2D>> _patchCache = new Dictionary<string, List<Texture2D>>();
        /// <summary>材質 kind (Art/tiles/act&lt;N&gt;_&lt;kind&gt;.png) の敷物を数種 (大きさ違い・向き違い) 作る。タイルが無ければ空</summary>
        static List<Texture2D> PatchSet(int act, string kind, bool edge, params int[] sizes)
        {
            string key = act + ":" + kind + ":" + (edge ? "e" : "p") + ":" + sizes.Length;
            List<Texture2D> l;
            if (_patchCache.TryGetValue(key, out l)) return l;
            l = new List<Texture2D>();
            var tile = Tex(act, kind, null);
            if (tile != null)
                for (int i = 0; i + 1 < sizes.Length; i += 2)
                    for (int v = 0; v < 3; v++) l.Add(MaterialPatch(tile, sizes[i], sizes[i + 1], act * 1000 + kind.GetHashCode() % 997 + i * 7 + v * 31, edge));
            _patchCache[key] = l;
            return l;
        }

        /// <summary>立て板の一枚絵を「世界での高さ h (units)」で置く (PixelLab の絵はドット数と実物の大きさが揃わないので、岩・茂み・草は高さで指定する)</summary>
        static GameObject SpriteH(string name, Texture2D tex, Vector3 pos, float h, float blobK, bool flip = false)
        {
            return Sprite(name, tex, pos, h / (tex.height * Dot), blobK, flip);
        }

        /// <summary>幕の小物のうち存在するものだけを集める (無い名前は飛ばす)</summary>
        static List<Texture2D> PropSet(int act, params string[] names)
        {
            var l = new List<Texture2D>();
            foreach (var n in names) { var t = PropTex(act, n, null); if (t != null) l.Add(t); }
            return l;
        }
        static T Pick<T>(List<T> l, System.Random rng) { return l[rng.Next(l.Count)]; }

        /// <summary>地面に寝かせた抜き板 (斑・小石)</summary>
        static GameObject Decal(string name, Material mat, float x, float z, float w, float d, float rotDeg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = new Vector3(x, GroundY(x, z) + 0.02f, z);
            go.transform.rotation = Quaternion.Euler(90f, rotDeg, 0f);
            go.transform.localScale = new Vector3(w, d, 1f);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            go.transform.position += go.transform.rotation * new Vector3(0f, -d * 0.5f, 0f);
            return go;
        }

        /// <summary>一枚の立て板 (茂み・草株・花・岩)。月光の勾配と街灯を受け、影を落とし、接地影を持てる</summary>
        static GameObject Plane(string name, Texture2D tex, Vector3 pos, float height, float cutoff, bool contactShadow)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            float w = height * tex.width / (float)tex.height;
            go.transform.localScale = new Vector3(w, height, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = SpriteMat(tex, cutoff, 1.0f);
            mr.sharedMaterial.SetFloat("_Rim", 0.3f);                 // 右上の縁に 1 ドットの光 (切り絵に見えない)
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = false;
            if (contactShadow) Blob("shadow-" + name, _world, pos, w);
            return go;
        }

        /// <summary>葉の塊のビルボード (カメラ正面。月光の勾配を受け、影を落とす)</summary>
        static GameObject LeafBillboard(Texture2D tex, Vector3 pos, float height, float tint)
        {
            var go = new GameObject("leaves");
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            go.transform.rotation = CameraRotation;
            float w = height * tex.width / (float)tex.height;
            go.transform.localScale = new Vector3(w, height, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quadCentered;
            var mr = go.AddComponent<MeshRenderer>();
            var m = SpriteMat(tex, 0.5f, 1.0f);
            m.SetFloat("_Rim", 0.35f);
            m.SetColor("_BaseColor", new Color(tint, tint, tint, 1f));
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>立体の木 (2026-09-08「森の木々は立体的に」): 根張り付きの多角柱の幹 (樹皮テクスチャ・影を落とす) と、高さと向きをずらした葉の塊 4〜6 枚。
        /// 月光で塊ごとに明暗が回り、カメラ揺れで奥行きが出る (オクトラの木の作り)</summary>
        static void Tree3D(Vector3 pos, float height, int kind, System.Random rng, Texture2D[] clumps, Material bark)
        {
            float trunkH = height * (kind == 1 ? 0.5f : kind == 2 ? 0.38f : 0.44f);
            float r0 = height * 0.075f, r1 = r0 * 0.62f;
            var mb = new MB();
            int seg = 7;
            for (int k = 0; k < seg; k++)
            {
                float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg;
                var b0 = pos + new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0); var b1 = pos + new Vector3(Mathf.Cos(a1) * r0, 0f, Mathf.Sin(a1) * r0);
                var t0 = pos + new Vector3(Mathf.Cos(a0) * r1, trunkH, Mathf.Sin(a0) * r1); var t1 = pos + new Vector3(Mathf.Cos(a1) * r1, trunkH, Mathf.Sin(a1) * r1);
                float u0 = k / (float)seg * 2f, u1 = (k + 1) / (float)seg * 2f;
                mb.Quad(b0, t0, t1, b1, new Vector2(u0, 0f), new Vector2(u0, trunkH / 1.2f), new Vector2(u1, trunkH / 1.2f), new Vector2(u1, 0f));
                // 根張り: 下 1/6 で外へ広がる楔
                if (k % 2 == 0)
                {
                    float am = (a0 + a1) * 0.5f;
                    var rt = pos + new Vector3(Mathf.Cos(am) * r0 * 2.6f, 0f, Mathf.Sin(am) * r0 * 2.6f);
                    var rl = pos + new Vector3(Mathf.Cos(a0) * r0 * 0.9f, 0f, Mathf.Sin(a0) * r0 * 0.9f);
                    var rr = pos + new Vector3(Mathf.Cos(a1) * r0 * 0.9f, 0f, Mathf.Sin(a1) * r0 * 0.9f);
                    var up = pos + new Vector3(Mathf.Cos(am) * r0 * 0.95f, height * 0.09f, Mathf.Sin(am) * r0 * 0.95f);
                    mb.Quad(rl, up, rt, rt + (rl - rr) * 0.01f, new Vector2(0f, 0f), new Vector2(0f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0f));
                    mb.Quad(rt, up, rr, rr + (rr - rl) * 0.01f, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.3f), new Vector2(1f, 0.3f), new Vector2(1f, 0f));
                }
            }
            var trunk = Solid("trunk", mb, bark);
            Blob("shadow-tree", _world, pos, r0 * 5f);
            // 葉の塊: 幹の上部を中心に、高さ・横ずれ・大きさを変えて重ねる。奥の塊ほど暗い
            int n = kind == 2 ? 8 : 7;
            float crownR = height * (kind == 2 ? 0.36f : 0.3f);
            for (int q = 0; q < n; q++)
            {
                float ang = (q / (float)n) * Mathf.PI * 2f + (float)rng.NextDouble() * 0.8f;
                float rad = crownR * (0.25f + (float)rng.NextDouble() * 0.75f);
                float hy = trunkH + height * (kind == 1 ? 0.12f : 0.06f) + (float)rng.NextDouble() * height * 0.28f;
                var off = new Vector3(Mathf.Cos(ang) * rad, hy, Mathf.Sin(ang) * rad * 0.7f);
                float sz = height * (0.34f + (float)rng.NextDouble() * 0.2f);
                float depthK = Mathf.InverseLerp(-crownR, crownR, off.z);          // 奥 (+z) ほど暗く小さく
                var tex = clumps[rng.Next(clumps.Length)];
                LeafBillboard(tex, pos + off - _fwd * (0.15f * (1f - depthK)), sz * (1f - 0.15f * depthK), 1f - 0.28f * depthK);
            }
            // 頂の塊 (一番明るい)
            LeafBillboard(clumps[0], pos + new Vector3(0f, trunkH + height * 0.32f, -0.05f), height * 0.46f, 1.04f);
        }

        /// <summary>十字の板 (木)。月光の勾配と街灯を受け、影を落とし、接地影を持つ</summary>
        static GameObject Cross(string name, Texture2D tex, Vector3 pos, float height, float cutoff)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            float w = height * tex.width / (float)tex.height;
            go.transform.localScale = new Vector3(w, height, w);
            go.AddComponent<MeshFilter>().sharedMesh = _cross;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = SpriteMat(tex, cutoff, 1.0f);
            mr.sharedMaterial.SetFloat("_Rim", 0.3f);
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = false;
            Blob("shadow-" + name, _world, pos, w * 0.6f);
            return go;
        }

        /// <summary>光を受けない板 (空・月・炎)</summary>
        static GameObject Prop(string name, Texture2D tex, Vector3 pos, float height, float cutoff, float width = -1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            float w = width > 0f ? width : height * tex.width / (float)tex.height;
            go.transform.localScale = new Vector3(w, height, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            var m = new Material(_unitShader);
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex;
            m.SetFloat("_Cutoff", cutoff);
            m.SetColor("_Ambient", Color.white);
            m.SetFloat("_SunAmount", 0f);
            m.SetColor("_LampColor", Color.black);
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>半透明の板 (暈・光溜まり)。影も深度も書かない</summary>
        static GameObject Glow(string name, Texture2D tex, Vector3 pos, float height, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(width, height, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GlowMaterial(tex);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        static Mesh BuildQuad()
        {
            var m = new Mesh();
            m.name = "stage-quad";
            m.vertices = new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f), new Vector3(0.5f, 0f, 0f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>中心が原点の板 (接地影・根元の帯)。回転で伸びる向きを考えなくてよい</summary>
        static Mesh BuildQuadCentered()
        {
            var m = new Mesh();
            m.name = "stage-quad-centered";
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds();
            return m;
        }

        static Mesh BuildCross()
        {
            var m = new Mesh();
            m.name = "stage-cross";
            var n = new Vector3(0f, 0.8f, -0.6f).normalized;
            var n2 = new Vector3(-0.6f, 0.8f, 0f).normalized;
            m.vertices = new[] {
                new Vector3(-0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f), new Vector3(0.5f, 0f, 0f),
                new Vector3(0f, 0f, -0.5f), new Vector3(0f, 1f, -0.5f), new Vector3(0f, 1f, 0.5f), new Vector3(0f, 0f, 0.5f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            m.normals = new[] { n, n, n, n, n2, n2, n2, n2 };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>箱庭のメッシュ (面を足して1つに)。UV は world 座標をタイル幅で割る = 継ぎ目なく敷ける</summary>
        class MB
        {
            readonly List<Vector3> _v = new List<Vector3>();
            readonly List<Vector2> _uv = new List<Vector2>();
            readonly List<Vector3> _n = new List<Vector3>();
            readonly List<int> _t = new List<int>();

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = _v.Count;
                var n = Vector3.Cross(b - a, c - a).normalized;
                _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
                _uv.Add(ua); _uv.Add(ub); _uv.Add(uc); _uv.Add(ud);
                _n.Add(n); _n.Add(n); _n.Add(n); _n.Add(n);
                _t.Add(i); _t.Add(i + 1); _t.Add(i + 2); _t.Add(i); _t.Add(i + 2); _t.Add(i + 3);
            }

            public void Floor(float x0, float z0, float x1, float z1, float y)
            {
                Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0),
                     new Vector2(x0, z0) / Tile, new Vector2(x0, z1) / Tile, new Vector2(x1, z1) / Tile, new Vector2(x1, z0) / Tile);
            }

            /// <summary>床。UV を 90° 単位で回す (草の目が一方向に流れない)</summary>
            public void FloorRot(float x0, float z0, float x1, float z1, float y, int rot)
            {
                var uv = new[] { new Vector2(x0, z0) / Tile, new Vector2(x0, z1) / Tile, new Vector2(x1, z1) / Tile, new Vector2(x1, z0) / Tile };
                rot = ((rot % 4) + 4) % 4;
                Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0),
                     uv[rot], uv[(rot + 1) % 4], uv[(rot + 2) % 4], uv[(rot + 3) % 4]);
            }

            public void WallZ(float x0, float x1, float y0, float y1, float z)
            {
                Quad(new Vector3(x0, y0, z), new Vector3(x0, y1, z), new Vector3(x1, y1, z), new Vector3(x1, y0, z),
                     new Vector2(x0, y0) / Tile, new Vector2(x0, y1) / Tile, new Vector2(x1, y1) / Tile, new Vector2(x1, y0) / Tile);
            }

            public void Box(float x, float y, float z, float w, float h, float d)
            {
                float x0 = x - w / 2f, x1 = x + w / 2f, z0 = z - d / 2f, z1 = z + d / 2f, y1 = y + h;
                Floor(x0, z0, x1, z1, y1);
                WallZ(x0, x1, y, y1, z0);
                Quad(new Vector3(x1, y, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), new Vector3(x0, y, z1),
                     new Vector2(x1, y) / Tile, new Vector2(x1, y1) / Tile, new Vector2(x0, y1) / Tile, new Vector2(x0, y) / Tile);
                Quad(new Vector3(x0, y, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y, z0),
                     new Vector2(z1, y) / Tile, new Vector2(z1, y1) / Tile, new Vector2(z0, y1) / Tile, new Vector2(z0, y) / Tile);
                Quad(new Vector3(x1, y, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y, z1),
                     new Vector2(z0, y) / Tile, new Vector2(z0, y1) / Tile, new Vector2(z1, y1) / Tile, new Vector2(z1, y) / Tile);
            }

            readonly List<Color> _c = new List<Color>();

            /// <summary>頂点色つきの面 (影の帯: 壁側 1 → 外 0)</summary>
            public void QuadC(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd)
            {
                while (_c.Count < _v.Count) _c.Add(Color.white);
                Quad(a, b, c, d, Vector2.zero, Vector2.one, Vector2.one, Vector2.zero);
                _c.Add(ca); _c.Add(cb); _c.Add(cc); _c.Add(cd);
            }

            public Mesh Build()
            {
                var m = new Mesh();
                m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(_v); m.SetUVs(0, _uv); m.SetNormals(_n); m.SetTriangles(_t, 0);
                if (_c.Count > 0) { while (_c.Count < _v.Count) _c.Add(Color.white); m.SetColors(_c); }
                m.RecalculateBounds();
                m.RecalculateTangents();   // 地面のノーマルマップ (2026-09-21)
                return m;
            }
        }

        // ---------------------------------------------------------------- ドット絵の生成 (PixelLab が来るまでの仮)

        static class Px
        {
            static Texture2D New(int w, int h, bool repeat)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.filterMode = FilterMode.Point;
                t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                return t;
            }

            static Color Mix(Color a, Color b, float k) { return Color.Lerp(a, b, k); }

            static void Put(Color[] px, int w, int h, int x, int y, Color c)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                px[y * w + x] = c;
            }

            public static Texture2D Solid(Color c)
            {
                var t = New(4, 4, false);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = c;
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>草地: 柔らかい2色の地に、疎らな草の房 (4階調)。黒点は置かない (砂嵐に見える)</summary>
            public static Texture2D Grass(Pal p, System.Random rng)
            {
                int n = 32;
                var t = New(n, n, true);
                var px = new Color[n * n];
                var mid = Mix(p.GrassA, p.GrassB, 0.5f);
                // なめらかなムラ (継ぎ目なく繰り返す値ノイズ) を3階調に。境界はディザで馴染ませる
                var g = new float[4, 4];
                for (int gy = 0; gy < 4; gy++) for (int gx = 0; gx < 4; gx++) g[gx, gy] = (float)rng.NextDouble();
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float fx = x / 8f, fy = y / 8f;
                        int x0 = (int)fx, y0 = (int)fy; float tx = fx - x0, ty = fy - y0;
                        tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
                        float v = Mathf.Lerp(Mathf.Lerp(g[x0 % 4, y0 % 4], g[(x0 + 1) % 4, y0 % 4], tx), Mathf.Lerp(g[x0 % 4, (y0 + 1) % 4], g[(x0 + 1) % 4, (y0 + 1) % 4], tx), ty);
                        bool dither = ((x + y) & 1) == 0;
                        var c = v < 0.38f ? (dither ? mid : p.GrassB) : v < 0.62f ? mid : (dither ? p.GrassA : mid);
                        px[y * n + x] = c;
                    }
                for (int k = 0; k < 4; k++)
                {
                    int x = rng.Next(3, n - 3), y = rng.Next(1, n - 4);
                    var lo = p.GrassB; var hi = p.GrassC; var top = Mix(p.GrassC, Color.white, 0.18f);
                    Put(px, n, n, x - 1, y, lo); Put(px, n, n, x + 1, y, lo); Put(px, n, n, x, y, lo);
                    Put(px, n, n, x - 2, y + 1, lo); Put(px, n, n, x - 1, y + 1, hi); Put(px, n, n, x + 1, y + 1, hi); Put(px, n, n, x + 2, y + 1, lo);
                    Put(px, n, n, x - 2, y + 2, hi); Put(px, n, n, x, y + 2, top); Put(px, n, n, x + 2, y + 2, hi);
                    Put(px, n, n, x + 1, y + 3, top);
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>土: ムラのある地に小石 (明+影) とひび</summary>
            public static Texture2D Dirt(Pal p, System.Random rng)
            {
                int n = 32;
                var t = New(n, n, true);
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        int bx = x / 8, by = y / 8;
                        float k = ((bx * 5 + by * 3) % 3) / 3f;
                        var c = Mix(p.DirtA, p.DirtB, k * 0.6f);
                        if (rng.NextDouble() < 0.18) c = Mix(c, p.DirtB, 0.5f);
                        px[y * n + x] = c;
                    }
                for (int k = 0; k < 8; k++)
                {
                    int x = rng.Next(1, n - 2), y = rng.Next(1, n - 1);
                    var hi = Mix(p.DirtA, Color.white, 0.2f); var sh = Mix(p.DirtB, Color.black, 0.3f);
                    Put(px, n, n, x, y, hi); Put(px, n, n, x + 1, y, Mix(hi, p.DirtA, 0.5f));
                    Put(px, n, n, x, y - 1, sh); Put(px, n, n, x + 1, y - 1, sh);
                }
                for (int k = 0; k < 3; k++)
                {
                    int x = rng.Next(n), y = rng.Next(n);
                    var d = Mix(p.DirtB, Color.black, 0.3f);
                    for (int i = 0; i < 6; i++) { Put(px, n, n, (x + i) % n, y, d); if (rng.NextDouble() < 0.4) y = (y + 1) % n; }
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Stone(Pal p, System.Random rng)
            {
                var t = New(32, 32, true);
                var px = new Color[32 * 32];
                var mortar = Mix(p.StoneB, Color.black, 0.35f);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        int row = y / 8;
                        int xx = (x + (row % 2) * 8) % 32;
                        bool m = (y % 8 == 0) || (xx % 16 == 0);
                        var c = m ? mortar : ((x * 7 + y * 13 + row * 5) % 11 < 3 ? p.StoneB : p.StoneA);
                        if (!m && y % 8 == 7) c = Mix(c, Color.white, 0.08f);
                        if (!m && rng.NextDouble() < 0.05) c = Mix(c, Color.black, 0.15f);
                        px[y * 32 + x] = c;
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Cliff(Pal p, System.Random rng)
            {
                var t = New(32, 32, true);
                var px = new Color[32 * 32];
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        bool strata = (y % 6 == 0 && ((x + y * 3) % 9) < 6);
                        var c = strata ? Mix(p.CliffB, Color.black, 0.3f) : (rng.NextDouble() < 0.2 ? p.CliffB : p.CliffA);
                        if (y % 6 == 5 && !strata) c = Mix(c, Color.white, 0.06f);
                        px[y * 32 + x] = c;
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            static void Disc(Color[] px, int w, int h, float cx, float cy, float r, Color c, System.Random rng, float noise)
            {
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        if (d <= r - (float)rng.NextDouble() * noise) px[y * w + x] = c;
                    }
            }

            /// <summary>葉の塊に4階調 (影・地・光・ハイライト) の陰影と外周の暗い線。右上が光源</summary>
            static void Foliage(Color[] px, int w, int h, Pal p, System.Random rng, int yMin, int q)
            {
                var outline = Mix(p.LeafB, Color.black, 0.45f);
                var hiLite = Mix(p.LeafC, Color.white, 0.25f);
                for (int y = yMin; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x;
                        if (px[i].a <= 0f) continue;
                        bool lit = false, shade = false, top = false;
                        for (int k = 1; k <= 3 * q && !lit; k++) if (y + k < h && px[(y + k) * w + x].a <= 0f) lit = true;
                        for (int k = 1; k <= 2 * q && !top; k++) if (y + k < h && x + k < w && px[(y + k) * w + x + k].a <= 0f) top = true;
                        for (int k = 1; k <= 3 * q && !shade; k++) if (y - k >= 0 && px[(y - k) * w + x].a <= 0f) shade = true;
                        for (int k = 1; k <= 2 * q && !shade; k++) if (x - k >= 0 && px[y * w + x - k].a <= 0f) shade = true;
                        bool topEdge = false;
                        for (int k = 1; k <= q && !topEdge; k++) if (y + k < h && px[(y + k) * w + x].a <= 0f) topEdge = true;
                        if (topEdge || (top && ((x + y) & 1) == 0)) px[i] = hiLite;
                        else if (lit || top) px[i] = p.LeafC;
                        else if (shade) px[i] = p.LeafB;
                        if (rng.NextDouble() < 0.04) px[i] = Mix(px[i], p.LeafC, 0.6f);
                    }
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                    {
                        if (px[y * w + x].a <= 0f) continue;
                        bool edge = px[(y - 1) * w + x].a <= 0f || px[(y + 1) * w + x].a <= 0f || px[y * w + x - 1].a <= 0f || px[y * w + x + 1].a <= 0f;
                        if (edge && y >= yMin) px[y * w + x] = outline;
                    }
            }

            /// <summary>木 3種: 0=丸い広葉樹・1=細長い針葉樹・2=横に広い低木。2倍密度 (q=2) で描く</summary>
            public static Texture2D Tree(Pal p, System.Random rng, int kind)
            {
                const int q = 2;
                int w = (kind == 1 ? 32 : kind == 2 ? 52 : 40) * q, h = (kind == 1 ? 72 : kind == 2 ? 44 : 60) * q;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                var bark = Mix(p.Trunk, Color.black, 0.35f);
                int trunkH = (kind == 1 ? 14 : kind == 2 ? 10 : 24) * q;
                int tx0 = w / 2 - 3 * q, tx1 = w / 2 + 3 * q;
                for (int y = 0; y < trunkH; y++)
                    for (int x = tx0; x < tx1; x++)
                    {
                        var c = x < tx0 + q || x >= tx1 - q ? bark : (((x / q + (y / q) * 3) % 5 == 0) ? Mix(p.Trunk, Color.black, 0.15f) : p.Trunk);
                        if (x >= tx1 - 2 * q && x < tx1 - q) c = Mix(p.Trunk, Color.white, 0.12f);   // 右側 (光源側) にハイライト
                        px[y * w + x] = c;
                    }
                for (int y = 0; y < 4 * q; y++) { Put(px, w, h, tx0 - 2 * q + (4 * q - 1 - y) / 1, y, bark); Put(px, w, h, tx1 + 2 * q - (4 * q - 1 - y), y, bark); }
                if (kind == 1)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        int baseY = (12 + s * 17) * q, topY = baseY + 26 * q;
                        for (int y = baseY; y < Mathf.Min(h, topY); y++)
                        {
                            float k = (y - baseY) / (float)(topY - baseY);
                            int half = Mathf.RoundToInt((1f - k) * (14f - s * 2f) * q) + 1;
                            for (int x = w / 2 - half; x < w / 2 + half; x++) Put(px, w, h, x, y, p.LeafA);
                        }
                    }
                    Foliage(px, w, h, p, rng, 12 * q, q);
                }
                else if (kind == 2)
                {
                    Disc(px, w, h, 16f * q, 24f * q, 13f * q, p.LeafA, rng, 1.6f * q);
                    Disc(px, w, h, 36f * q, 22f * q, 13f * q, p.LeafA, rng, 1.6f * q);
                    Disc(px, w, h, 26f * q, 30f * q, 12f * q, p.LeafA, rng, 1.4f * q);
                    Foliage(px, w, h, p, rng, 10 * q, q);
                }
                else
                {
                    Disc(px, w, h, 20f * q, 36f * q, 15.5f * q, p.LeafA, rng, 1.8f * q);
                    Disc(px, w, h, 12f * q, 32f * q, 10f * q, p.LeafA, rng, 1.4f * q);
                    Disc(px, w, h, 28f * q, 33f * q, 10.5f * q, p.LeafA, rng, 1.4f * q);
                    Disc(px, w, h, 19f * q, 46f * q, 10f * q, p.LeafA, rng, 1.4f * q);
                    Disc(px, w, h, 25f * q, 44f * q, 8f * q, p.LeafA, rng, 1.2f * q);
                    Foliage(px, w, h, p, rng, 22 * q, q);
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>羊歯: 根元から弧を描いて広がる葉 5〜6 本</summary>
            public static Texture2D Fern(Pal p, System.Random rng)
            {
                const int q = 2;
                int w = 18 * q, h = 12 * q;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                int n = 5 + rng.Next(2);
                for (int k = 0; k < n; k++)
                {
                    float ang = Mathf.Lerp(0.25f, Mathf.PI - 0.25f, (k + 0.5f) / n) + ((float)rng.NextDouble() - 0.5f) * 0.25f;
                    float len = (8f + (float)rng.NextDouble() * 3f) * q;
                    var c = k % 2 == 0 ? p.LeafB : Mix(p.LeafA, p.LeafC, 0.5f);
                    for (float d = 0; d < len; d += 0.5f)
                    {
                        float k2 = d / len;
                        float x = w / 2f + Mathf.Cos(ang) * d, y = Mathf.Sin(ang) * d * (1f - 0.35f * k2 * k2);
                        Put(px, w, h, Mathf.RoundToInt(x), Mathf.RoundToInt(y), c);
                        if (((int)(d / q)) % 2 == 0) { Put(px, w, h, Mathf.RoundToInt(x) + (Mathf.Cos(ang) > 0 ? 1 : -1), Mathf.RoundToInt(y) + 1, Mix(c, Color.black, 0.25f)); }
                    }
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>頭上の枝葉: 上辺から垂れる葉の塊 (下 4 割は透明)。額縁の上辺に置く</summary>
            public static Texture2D Canopy(Pal p, System.Random rng)
            {
                const int q = 2;
                int w = 160 * q, h = 48 * q;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                var dark = Mix(p.LeafA, Color.black, 0.45f);
                for (int i = 0; i < 26; i++)
                {
                    float cx = (float)rng.NextDouble() * w, cy = h - (6f + (float)rng.NextDouble() * 16f) * q, r = (7f + (float)rng.NextDouble() * 9f) * q;
                    Disc(px, w, h, cx, cy, r, i % 3 == 0 ? p.LeafA : dark, rng, 1.2f * q);
                }
                for (int x = 0; x < w; x++) for (int y = h - 8 * q; y < h; y++) if (px[y * w + x].a > 0.5f) px[y * w + x] = dark;   // 最上段は影
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Bush(Pal p, System.Random rng)
            {
                const int q = 2;
                int w = 28 * q, h = 18 * q;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                Disc(px, w, h, 9f * q, 6f * q, 7.5f * q, p.LeafA, rng, 1.2f * q);
                Disc(px, w, h, 19f * q, 6f * q, 7.5f * q, p.LeafA, rng, 1.2f * q);
                Disc(px, w, h, 14f * q, 10f * q, 7f * q, p.LeafA, rng, 1.2f * q);
                Foliage(px, w, h, p, rng, 0, q);
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>岩: 灰〜茶 (茂みの緑と別物に見える)。右上に光、左下に影</summary>
            public static Texture2D Rock(Pal p, System.Random rng)
            {
                const int q = 2;
                int w = 24 * q, h = 14 * q;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                var ra = Mix(p.StoneA, p.DirtA, 0.45f); var rb = Mix(p.StoneB, p.DirtB, 0.45f);
                Disc(px, w, h, 12f * q, 4f * q, 10f * q, rb, rng, 1.5f * q);
                Disc(px, w, h, 13f * q, 6.5f * q, 6.5f * q, ra, rng, 1.2f * q);
                Disc(px, w, h, 15f * q, 8.5f * q, 3f * q, Mix(ra, Color.white, 0.22f), rng, 1f * q);
                var outline = Mix(rb, Color.black, 0.4f);
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                    {
                        if (px[y * w + x].a <= 0f) continue;
                        bool edge = px[(y - 1) * w + x].a <= 0f || px[(y + 1) * w + x].a <= 0f || px[y * w + x - 1].a <= 0f || px[y * w + x + 1].a <= 0f;
                        if (edge) px[y * w + x] = outline;
                    }
                for (int x = 0; x < w; x++) for (int y = 0; y < 2; y++) if (px[y * w + x].a > 0f) px[y * w + x] = outline;
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Tuft(Pal p, System.Random rng)
            {
                int w = 12, h = 10;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                int[] tops = { 4, 7, 9, 6, 8, 5 };
                int[] xs = { 1, 3, 5, 7, 9, 10 };
                for (int b = 0; b < xs.Length; b++)
                {
                    int x = xs[b];
                    for (int y = 0; y < tops[b]; y++)
                    {
                        int xx = x + (y > tops[b] / 2 ? (b % 2 == 0 ? 1 : -1) : 0);
                        Put(px, w, h, xx, y, y >= tops[b] - 2 ? p.GrassC : (y < 2 ? p.GrassB : p.GrassA));
                    }
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Flower(Pal p, System.Random rng)
            {
                int w = 8, h = 10;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                for (int y = 0; y < 6; y++) Put(px, w, h, 4, y, p.GrassA);
                Put(px, w, h, 2, 2, p.GrassC); Put(px, w, h, 3, 3, p.GrassA); Put(px, w, h, 5, 1, p.GrassC);
                var petal = rng.NextDouble() < 0.5 ? new Color(0.95f, 0.9f, 0.7f) : new Color(0.9f, 0.7f, 0.85f);
                for (int y = 6; y < 9; y++) for (int x = 3; x < 6; x++) Put(px, w, h, x, y, petal);
                Put(px, w, h, 4, 7, new Color(1f, 0.85f, 0.35f));
                Put(px, w, h, 2, 7, petal); Put(px, w, h, 6, 7, petal); Put(px, w, h, 4, 9, petal); Put(px, w, h, 4, 5, Mix(petal, p.GrassA, 0.5f));
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Pebble(Pal p, System.Random rng)
            {
                int w = 12, h = 8;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                var pa = Mix(p.DirtA, p.StoneA, 0.4f); var pb = Mix(p.DirtB, p.StoneB, 0.4f);
                Disc(px, w, h, 6f, 4f, 4.5f, pb, rng, 1.2f);
                Disc(px, w, h, 6.5f, 5f, 2.5f, pa, rng, 0.8f);
                for (int x = 0; x < w; x++) if (px[x].a > 0f) px[x] = Mix(pb, Color.black, 0.3f);
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>地面の斑 (不規則な塊。ドット絵らしい硬い縁)。小さな円を多く重ねて輪郭を崩す</summary>
            public static Texture2D Patch(Color a, Color b, System.Random rng)
            {
                int n = 48;
                var t = New(n, n, false);
                var px = new Color[n * n];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                float cx = n / 2f, cy = n / 2f;
                for (int k = 0; k < 18; k++)
                {
                    float ang = (float)rng.NextDouble() * 6.283f, r = (float)rng.NextDouble() * n * 0.28f;
                    Disc(px, n, n, cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r * 0.7f, 3f + (float)rng.NextDouble() * 9f, ((k & 1) == 0) ? a : Mix(a, b, 0.5f), rng, 2.5f);
                }
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) if (px[y * n + x].a > 0f && ((x + y) & 1) == 0 && rng.NextDouble() < 0.35) px[y * n + x] = Mix(px[y * n + x], b, 0.5f);
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>放射状の光溜まり: 中心から外へ滑らかに減衰 (輪郭を作らない)</summary>
            public static Texture2D Radial(Color c)
            {
                int n = 128;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * (3f - 2f * a);
                        px[y * n + x] = new Color(c.r, c.g, c.b, c.a * a * a);
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>丈の高い草 (前景用・大きめ)。3階調の穂</summary>
            public static Texture2D TallGrass(Pal p, System.Random rng)
            {
                int w = 28, h = 44;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                for (int b = 0; b < 9; b++)
                {
                    int x0 = 2 + b * 3, top = 22 + rng.Next(20);
                    int lean = rng.Next(3) - 1;
                    for (int y = 0; y < top; y++)
                    {
                        int x = x0 + (y * lean) / 12;
                        var c = y > top - 6 ? p.GrassC : (y < 8 ? p.GrassB : p.GrassA);
                        Put(px, w, h, x, y, c);
                        if (y > top - 3) Put(px, w, h, x + 1, y, Mix(p.GrassC, Color.white, 0.15f));
                    }
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>遠くの山の稜線 (霧で薄れる前提の一色のシルエット)。fade で空の色に寄せる</summary>
            public static Texture2D Mountains(Pal p, System.Random rng, float fade)
            {
                int w = 512, h = 96;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var c = Mix(Mix(p.SkyBot, Color.black, 0.35f), p.Fog, fade);
                float hh = 30f;
                for (int x = 0; x < w; x++)
                {
                    if (x % 3 == 0) hh = Mathf.Clamp(hh + (float)(rng.NextDouble() * 6.0 - 3.0), 18f, 80f);
                    int top = (int)hh + ((x / 40) % 2 == 0 ? 6 : 0);
                    for (int y = 0; y < top; y++) px[y * w + x] = c;
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Skyline(Pal p, System.Random rng)
            {
                int w = 256, h = 64;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var c = Mix(p.SkyBot, Color.black, 0.45f);
                int hh = 20;
                for (int x = 0; x < w; x++)
                {
                    if (x % 5 == 0) hh = Mathf.Clamp(hh + rng.Next(-6, 7), 8, 44);
                    int top = hh + ((x % 5 == 2) ? 6 : 0);
                    for (int y = 0; y < top; y++) px[y * w + x] = c;
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>(旧) 遠景の塔: 上へ細り、らせんの段が右上がりに走り、頂は空に溶ける。窓の灯はごく少数</summary>
            /// <summary>坑口の櫓 (2026-09-10 世界観改稿。旧 Tower を置換): 木組みの八の字の脚・筋交い・
            /// 天辺の滑車と巻上げ機・足元に黒い竪坑の口。遠景のシルエットとして読ませる</summary>
            public static Texture2D Headframe(Pal p, System.Random rng)
            {
                int w = 128, h = 176;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var body = Mix(p.SkyTop, Color.black, 0.66f);          // 木組み (空より暗い影)
                var edge = Mix(body, p.SkyBot, 0.3f);                  // 縁の一段明るい木
                var hole = Mix(Color.black, p.SkyBot, 0.08f);          // 竪坑の口 = ほぼ黒
                var lamp = new Color(1f, 0.78f, 0.45f, 1f);            // 櫓の作業灯

                void Bar(int x0, int y0, int x1, int y1, int th, Color c)
                {
                    int n = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) + 1;
                    for (int i = 0; i < n; i++)
                    {
                        float u = n == 1 ? 0f : i / (float)(n - 1);
                        int cx = Mathf.RoundToInt(Mathf.Lerp(x0, x1, u));
                        int cy = Mathf.RoundToInt(Mathf.Lerp(y0, y1, u));
                        for (int dy = -th / 2; dy <= th / 2; dy++)
                            for (int dx = -th / 2; dx <= th / 2; dx++)
                            {
                                int x = cx + dx, y = cy + dy;
                                if (x < 0 || x >= w || y < 0 || y >= h) continue;
                                var col = c; col.a = 1f; px[y * w + x] = col;
                            }
                    }
                }

                // 脚: 下広がりの八の字 (地面 y=14 から天辺 y=150 へ)
                int footL = 16, footR = w - 1 - footL, topL = 48, topR = w - 1 - topL;
                int gy = 14, ty = 150;
                Bar(footL, gy, topL, ty, 7, body);
                Bar(footR, gy, topR, ty, 7, body);
                Bar(footL + 9, gy, topL + 7, ty, 4, edge);             // 内側のもう一本 (二重の柱)
                Bar(footR - 9, gy, topR - 7, ty, 4, edge);

                // 横木と筋交い (4段。上へいくほど幅が狭い)
                for (int i = 0; i < 4; i++)
                {
                    float k0 = i / 4f, k1 = (i + 1) / 4f;
                    int y0 = (int)Mathf.Lerp(gy, ty, k0), y1 = (int)Mathf.Lerp(gy, ty, k1);
                    int l0 = (int)Mathf.Lerp(footL, topL, k0), r0 = w - 1 - l0;
                    int l1 = (int)Mathf.Lerp(footL, topL, k1), r1 = w - 1 - l1;
                    Bar(l1, y1, r1, y1, 4, body);                      // 横木
                    Bar(l0, y0, r1, y1, 3, edge);                      // 筋交い (右上がり)
                    Bar(r0, y0, l1, y1, 3, edge);                      // 筋交い (左上がり)
                }

                // 天辺: 台と滑車 (輪。中心を抜く)
                Bar(topL - 6, ty, topR + 6, ty, 6, body);
                int pcx = w / 2, pcy = ty + 13, pr = 13;
                for (int y = pcy - pr; y <= pcy + pr; y++)
                    for (int x = pcx - pr; x <= pcx + pr; x++)
                    {
                        if (x < 0 || x >= w || y < 0 || y >= h) continue;
                        float d = Mathf.Sqrt((x - pcx) * (x - pcx) + (y - pcy) * (y - pcy));
                        if (d > pr || d < pr - 4.5f) continue;
                        var c = body; c.a = 1f; px[y * w + x] = c;
                    }
                Bar(pcx, ty, pcx, pcy, 4, body);                        // 滑車の支え
                Bar(pcx - 1, gy + 6, pcx - 1, pcy - pr, 2, edge);        // 巻上げの綱 (坑へ垂れる)

                // 足元: 竪坑の口 (地面に開いた黒い穴。櫓より広い)
                for (int y = 0; y <= gy + 2; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float ex = (x - w * 0.5f) / (w * 0.46f);
                        float ey = (y - gy) / 13f;
                        if (ex * ex + ey * ey > 1f) continue;
                        var c = hole; c.a = 1f; px[y * w + x] = c;
                    }

                // 作業灯 2つ (暖色は灯の範囲だけ、の規約どおり点で置く)
                px[(ty - 4) * w + topL + 2] = lamp;
                px[(gy + 20) * w + footR - 12] = lamp;

                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>からくりの匣 (2026-09-10): 小さな木の匣。open なら蓋が開いて中に仕込み札の裏 (夜色) と歯車が見える</summary>
            public static Texture2D KarakuriBox(bool open)
            {
                int w = 24, h = 22;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var wood = UiKit.Hex("#6b4b33"); var woodL = UiKit.Hex("#8a6647"); var woodD = UiKit.Hex("#4a3222");
                var iron = UiKit.Hex("#3a3a42"); var card = UiKit.Hex("#2b2d4d"); var brass = UiKit.Hex("#c9a04a"); var ink = UiKit.Hex("#221a16");
                void Rect(int x0, int y0, int x1, int y1, Color c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) if (x >= 0 && x < w && y >= 0 && y < h) { var cc = c; cc.a = 1f; px[y * w + x] = cc; } }
                // 本体 (下 12 行): 板目と鉄の帯
                Rect(2, 0, 21, 11, wood);
                Rect(2, 0, 21, 0, woodD); Rect(2, 11, 21, 11, woodL);
                Rect(2, 0, 2, 11, woodD); Rect(21, 0, 21, 11, woodD);
                Rect(4, 2, 4, 9, iron); Rect(19, 2, 19, 9, iron);          // 鉄の帯
                Rect(11, 4, 12, 6, brass);                                  // 錠前
                for (int y = 3; y <= 9; y += 3) Rect(6, y, 17, y, woodD);   // 板目
                if (!open)
                {
                    // 閉じた蓋 (上 6 行): 少し張り出す
                    Rect(1, 12, 22, 16, woodL); Rect(1, 12, 22, 12, woodD); Rect(1, 16, 22, 16, wood);
                    Rect(4, 13, 4, 15, iron); Rect(19, 13, 19, 15, iron);
                }
                else
                {
                    // 開いた蓋 (後ろへ立つ) と、中の仕込み札の裏と歯車
                    Rect(3, 12, 20, 13, ink);                               // 口の影
                    Rect(5, 13, 18, 16, card); Rect(5, 16, 18, 16, UiKit.Hex("#4a4d80"));   // 札の裏 (夜色)
                    Rect(1, 15, 22, 21, woodL); Rect(1, 15, 22, 15, woodD); Rect(1, 21, 22, 21, woodD);   // 立った蓋
                    Rect(4, 16, 4, 20, iron); Rect(19, 16, 19, 20, iron);
                    Rect(10, 17, 13, 19, brass); Rect(11, 16, 12, 20, brass); Rect(9, 18, 14, 18, brass);   // 歯車
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            // ---------------- 幕2/3 のハイディテール (2026-09-11 ユーザー「2.3ステージをもっとハイディテールに幻想的に」) ----------------

            /// <summary>マナの結晶: 3〜5 本の柱状の結晶が根元から扇に伸びる。左面が暗く右面が明るい (右上の光源)。自発光の板として置く</summary>
            public static Texture2D Crystal(Color glow, System.Random rng)
            {
                int w = 40, h = 48;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var dark = Mix(glow, Color.black, 0.55f); var mid = Mix(glow, Color.white, 0.1f); var bright = Mix(glow, Color.white, 0.55f);
                int n = 3 + rng.Next(3);
                for (int k = 0; k < n; k++)
                {
                    float ang = -50f + (100f / (n - 1)) * k + ((float)rng.NextDouble() - 0.5f) * 14f;
                    float len = 18f + (float)rng.NextDouble() * 22f;
                    float wid = 3.5f + (float)rng.NextDouble() * 3f;
                    float bx = w * 0.5f + ((float)rng.NextDouble() - 0.5f) * 10f, by = 4f;
                    float dx = Mathf.Sin(ang * Mathf.Deg2Rad), dy = Mathf.Cos(ang * Mathf.Deg2Rad);
                    for (float u = 0f; u <= len; u += 0.5f)
                    {
                        float hw = wid * (1f - Mathf.Pow(u / len, 2.2f)) + 0.6f;
                        for (float v = -hw; v <= hw; v += 0.5f)
                        {
                            int x = Mathf.RoundToInt(bx + dx * u - dy * v), y = Mathf.RoundToInt(by + dy * u + dx * v);
                            if (x < 0 || x >= w || y < 0 || y >= h) continue;
                            var c = v < -hw * 0.35f ? dark : v > hw * 0.45f ? bright : mid;
                            if (u > len - 3f) c = bright;
                            c.a = 1f; px[y * w + x] = c;
                        }
                    }
                }
                for (int y = 0; y < 6; y++)
                    for (int x = 6; x < w - 6; x++)
                    {
                        float e = (x - w * 0.5f) / (w * 0.4f), f = (y - 3f) / 3.5f;
                        if (e * e + f * f > 1f) continue;
                        px[y * w + x] = new Color(0.22f, 0.24f, 0.28f, 1f);
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>鍾乳石: 天井から垂れる石の牙 2〜3 本 (板の上端が天井)。先は濡れて青い</summary>
            public static Texture2D Stalactites(Pal p, System.Random rng)
            {
                int w = 48, h = 40;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var a = Mix(p.CliffA, Color.black, 0.2f); var b = Mix(p.CliffB, Color.black, 0.35f); var wet = Mix(p.CliffA, new Color(0.5f, 0.9f, 0.9f), 0.35f);
                int n = 2 + rng.Next(2);
                for (int k = 0; k < n; k++)
                {
                    float cx = 8f + (w - 16f) * (k + 0.5f) / n + ((float)rng.NextDouble() - 0.5f) * 6f;
                    float len = 16f + (float)rng.NextDouble() * 20f; float top = 6f + (float)rng.NextDouble() * 5f;
                    for (int y = 0; y < h; y++)
                    {
                        float d = h - 1 - y;
                        if (d > len) continue;
                        float hw = top * (1f - d / len);
                        for (int x = 0; x < w; x++)
                        {
                            float e = x - cx; if (Mathf.Abs(e) > hw) continue;
                            var c = e < -hw * 0.3f ? b : a; if (d > len - 2.5f) c = wet;
                            c.a = 1f; px[y * w + x] = c;
                        }
                    }
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>垂れ根: 天井の岩の隙間から下がる細い根 (板の上端が天井)</summary>
            public static Texture2D HangingRoots(Pal p, System.Random rng)
            {
                int w = 40, h = 44;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var c1 = Mix(p.Trunk, Color.black, 0.2f); var c2 = Mix(p.Trunk, p.LeafC, 0.25f);
                int n = 4 + rng.Next(3);
                for (int k = 0; k < n; k++)
                {
                    float x0 = 4f + (float)rng.NextDouble() * (w - 8f); float len = 14f + (float)rng.NextDouble() * 28f;
                    float sway = ((float)rng.NextDouble() - 0.5f) * 10f; float ph = (float)rng.NextDouble() * 6f;
                    for (int y = h - 1; y >= 0 && (h - 1 - y) <= len; y--)
                    {
                        float d = h - 1 - y; float x = x0 + sway * (d / len) + Mathf.Sin(d * 0.35f + ph) * 1.6f;
                        int xi = Mathf.RoundToInt(x); int th = d < len * 0.5f ? 2 : 1;
                        for (int dx = 0; dx < th; dx++) { int xx = xi + dx; if (xx < 0 || xx >= w) continue; var c = dx == 0 ? c1 : c2; c.a = 1f; px[y * w + xx] = c; }
                    }
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>トロッコ: 木の箱に鉄の帯、山盛りの光る鉱</summary>
            public static Texture2D MineCart(Color ore, System.Random rng)
            {
                int w = 34, h = 26;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var wood = UiKit.Hex("#5e4432"); var woodL = UiKit.Hex("#7a5a40"); var iron = UiKit.Hex("#3a3a42"); var ironL = UiKit.Hex("#5a5a66");
                void R(int x0, int y0, int x1, int y1, Color c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) if (x >= 0 && x < w && y >= 0 && y < h) { var cc = c; cc.a = 1f; px[y * w + x] = cc; } }
                R(6, 0, 10, 4, iron); R(7, 1, 9, 3, ironL); R(23, 0, 27, 4, iron); R(24, 1, 26, 3, ironL);
                for (int y = 5; y <= 18; y++) { int inset = (18 - y) / 3; R(3 + inset, y, w - 4 - inset, y, (y % 4 == 0) ? woodL : wood); }
                R(3, 5, w - 4, 5, iron); R(2, 17, w - 3, 18, iron); R(9, 6, 9, 16, iron); R(w - 10, 6, w - 10, 16, iron);
                var oreD = Mix(ore, Color.black, 0.4f); var oreL = Mix(ore, Color.white, 0.4f);
                for (int i = 0; i < 26; i++)
                {
                    int x = 6 + rng.Next(w - 12), y = 15 + rng.Next(9);
                    float e = (x - w * 0.5f) / (w * 0.36f), f = (y - 15f) / 9f; if (e * e + f * f > 1f) continue;
                    R(x, y, x + 1, y + 1, rng.NextDouble() < 0.3 ? oreL : rng.NextDouble() < 0.5 ? ore : oreD);
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>古代の大門: 二本の柱と半円のアーチ。縁の紋が脈の色に光る</summary>
            public static Texture2D Arch(Pal p, Color rune)
            {
                int w = 96, h = 124;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var stone = Mix(p.StoneB, Color.black, 0.45f); var edge = Mix(p.StoneA, Color.black, 0.3f);
                int pw = 18, top = 76; float R = w * 0.5f, cx = w * 0.5f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        bool inPillar = (x < pw || x >= w - pw) && y < top;
                        float dx = x - cx, dy = y - top; float rr = Mathf.Sqrt(dx * dx + dy * dy);
                        bool inArch = y >= top && rr <= R && rr >= R - pw;
                        if (!(inPillar || inArch)) continue;
                        var c = ((x % 9 == 0) || (y % 11 == 0)) ? edge : stone;
                        c.a = 1f; px[y * w + x] = c;
                    }
                var rc = rune; rc.a = 1f;
                for (int a = 0; a <= 180; a += 2)
                {
                    float ang = a * Mathf.Deg2Rad; float rad = R - pw * 0.5f;
                    int x = Mathf.RoundToInt(cx + Mathf.Cos(ang) * rad), y = Mathf.RoundToInt(top + Mathf.Sin(ang) * rad);
                    if (x >= 0 && x < w && y >= 0 && y < h) { px[y * w + x] = rc; if (y + 1 < h) px[(y + 1) * w + x] = rc; }
                }
                for (int i = 0; i < 7; i++)
                {
                    int y0 = 8 + i * 10;
                    for (int y = y0; y < y0 + 4 && y < top; y++) { px[y * w + pw / 2] = rc; px[y * w + pw / 2 + 1] = rc; px[y * w + (w - pw / 2 - 1)] = rc; px[y * w + (w - pw / 2 - 2)] = rc; }
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>水道橋の列: 半円アーチの連なり (遠景のシルエット)</summary>
            public static Texture2D Arcade(Pal p, int arches)
            {
                int aw = 28, w = aw * arches, h = 44;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var stone = Mix(p.StoneB, Color.black, 0.5f); var edge = Mix(p.StoneA, Color.black, 0.35f);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int lx = x % aw; float dx = lx - aw * 0.5f;
                        bool open = false;
                        if (y < 24) open = Mathf.Abs(dx) < 8f;
                        else if (y < 32) { float dy = y - 24; open = dx * dx + dy * dy < 64f; }
                        if (open) continue;
                        var c = (y >= h - 3 || y == 33 || lx == 0) ? edge : stone; c.a = 1f; px[y * w + x] = c;
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>天井を走る脈: 洞窟の天井に沿う光の帯 (両端で消える。滑らか)</summary>
            public static Texture2D Aurora(Color c0, System.Random rng)
            {
                int w = 256, h = 48;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[w * h];
                float[] ph = new float[4]; for (int i = 0; i < 4; i++) ph[i] = (float)rng.NextDouble() * 10f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w, v = y / (float)h;
                        float center = 0.5f + 0.18f * Mathf.Sin(u * 6.2f + ph[0]) + 0.08f * Mathf.Sin(u * 17f + ph[1]);
                        float d = Mathf.Abs(v - center);
                        float a = Mathf.Exp(-d * d * 40f) * (0.55f + 0.45f * Mathf.Sin(u * 31f + ph[2]) * Mathf.Sin(u * 7f + ph[3]));
                        a *= Mathf.Sin(u * Mathf.PI);
                        px[y * w + x] = new Color(c0.r, c0.g, c0.b, Mathf.Clamp01(a) * 0.8f);
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>滑車の輪: 縁と 6 本のスポーク (中心の板に貼って回す)</summary>
            public static Texture2D Wheel(Color c, int size)
            {
                int w = size, h = size;
                var t = New(w, h, false);
                var px = new Color[w * h];
                float cx = w * 0.5f - 0.5f, cy = h * 0.5f - 0.5f, R = w * 0.5f - 1f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float dx = x - cx, dy = y - cy; float r = Mathf.Sqrt(dx * dx + dy * dy); if (r > R) continue;
                        bool rim = r > R - 3f, hub = r < 3f, spoke = false;
                        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        for (int k = 0; k < 6; k++) if (Mathf.Abs(Mathf.DeltaAngle(ang, k * 60f)) < 5f) spoke = true;
                        if (!(rim || hub || spoke)) continue;
                        var cc = c; cc.a = 1f; px[y * w + x] = cc;
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>光る茸: 淡い青緑の傘 2〜3 本 (自発光の板として置く)</summary>
            public static Texture2D GlowShroom(Pal p, System.Random rng)
            {
                int w = 14, h = 12;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                var cap = new Color(0.62f, 0.98f, 1.15f); var capD = new Color(0.4f, 0.72f, 0.9f); var stem = new Color(0.78f, 0.86f, 0.8f); var stemD = new Color(0.55f, 0.62f, 0.62f);
                int[] cx = { 3, 8, 11 }; int[] ch = { 5, 8, 4 }; int[] cw = { 2, 3, 2 };
                for (int k = 0; k < 3; k++)
                {
                    for (int y = 0; y < ch[k] - 2; y++) { Put(px, w, h, cx[k], y, stem); Put(px, w, h, cx[k] - 1, y, stemD); }
                    for (int dx = -cw[k]; dx <= cw[k]; dx++)
                    {
                        Put(px, w, h, cx[k] + dx, ch[k] - 2, capD);
                        Put(px, w, h, cx[k] + dx, ch[k] - 1, cap);
                        if (Mathf.Abs(dx) < cw[k]) Put(px, w, h, cx[k] + dx, ch[k], cap);
                    }
                    Put(px, w, h, cx[k], ch[k] - 1, Color.white);
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>月光の筋: 上が濃く下へ消える縦の帯、左右は柔らかく</summary>
            public static Texture2D Beam() { return Beam(new Color(0.7f, 0.8f, 1f)); }

            /// <summary>光の柱。色を渡せる (2026-09-10: 幕3 の脈の光は青緑)</summary>
            public static Texture2D Beam(Color tint) { return Beam(tint, 2f, 2f); }

            /// <summary>上を薄れさせる光の柱 (v が fadeFrom→fadeTo で α が 0 へ)。2026-09-30 F53: 主役の背後の筋は上端 (v=1) が最も濃く、
            /// 梢の中に理由の分からない青い柱が立って視線を引いた。刃と頭の高さの α は変えずに梢へは届かせない</summary>
            public static Texture2D Beam(Color tint, float fadeFrom, float fadeTo)
            {
                int w = 32, h = 128;
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                        float side = Mathf.Sin(u * Mathf.PI); side *= side;
                        float a = side * Mathf.Pow(v, 1.3f) * 0.6f;
                        if (v > fadeFrom) a *= 1f - Mathf.SmoothStep(0f, 1f, (v - fadeFrom) / Mathf.Max(0.001f, fadeTo - fadeFrom));
                        px[y * w + x] = new Color(tint.r, tint.g, tint.b, a);
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>星空: まばらな点。いくつかは少し大きく明るい</summary>
            public static Texture2D Stars(System.Random rng)
            {
                int w = 512, h = 64;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                for (int i = 0; i < 170; i++)
                {
                    int x = rng.Next(0, w), y = rng.Next(0, h);
                    float b = 0.5f + (float)rng.NextDouble() * 0.5f;
                    var c = new Color(0.85f * b + 0.4f, 0.9f * b + 0.4f, 1.3f * b + 0.3f, 1f);
                    px[y * w + x] = c;
                    if (rng.NextDouble() < 0.18) { Put(px, w, h, x + 1, y, c * 0.7f); Put(px, w, h, x, y + 1, c * 0.7f); }
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>樹皮 (仮): 縦の筋と苔の斑</summary>
            public static Texture2D Bark(Pal p, System.Random rng)
            {
                int w = 32, h = 32;
                var t = New(w, h, true);
                var px = new Color[w * h];
                var a = Mix(p.Trunk, Color.black, 0.3f); var b = p.Trunk; var c = Mix(p.Trunk, Color.white, 0.1f); var moss = Mix(p.LeafA, p.Trunk, 0.5f);
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    float n = Vnoise(x * 0.5f + 3f, y * 0.12f + 1f);
                    var col = n < 0.35f ? a : n < 0.7f ? b : c;
                    if (Vnoise(x * 0.25f + 9f, y * 0.25f + 5f) > 0.72f) col = moss;
                    px[y * w + x] = col;
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>葉の塊 (仮): 丸い塊に葉のドットと右上の光</summary>
            public static Texture2D LeafClump(Pal p, System.Random rng, int kind)
            {
                const int q = 2;
                int w = (kind == 2 ? 24 : 32) * q, h = (kind == 2 ? 16 : 20) * q;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                Disc(px, w, h, w * 0.5f, h * 0.45f, h * 0.5f, p.LeafA, rng, 1.6f * q);
                Disc(px, w, h, w * 0.3f, h * 0.4f, h * 0.36f, Mix(p.LeafA, Color.black, 0.2f), rng, 1.2f * q);
                Disc(px, w, h, w * 0.68f, h * 0.55f, h * 0.36f, p.LeafB, rng, 1.2f * q);
                Foliage(px, w, h, p, rng, 6 * q, q);
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>水面 (仮): 暗い青に淡いさざ波の筋。Repeat</summary>
            /// <summary>水たまり (2026-09-21): 地面が透ける暗い水面。楕円をノイズで崩し、縁は 1 ドットのディザ。左上に月の映り込み (淡い光) と数点のきらめき。
            /// PixelLab に頼むと縁のある皿 (マンホール) になったのでコードで描く。半透明なので下の土や草の色がそのまま透ける</summary>
            public static Texture2D Puddle(System.Random rng, int w, int h)
            {
                var t = New(w, h, false);
                var px = new Color[w * h];
                float n1 = (float)rng.NextDouble() * 50f, n2 = (float)rng.NextDouble() * 50f;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float fx = (x + 0.5f) / w * 2f - 1f, fy = (y + 0.5f) / h * 2f - 1f;
                        float ang = Mathf.Atan2(fy, fx);
                        float wob = (Mathf.PerlinNoise(Mathf.Cos(ang) * 1.3f + n1, Mathf.Sin(ang) * 1.3f + n2) - 0.5f) * 0.5f;
                        float r = Mathf.Sqrt(fx * fx + fy * fy);
                        float m = (0.9f + wob - r) * 4f;
                        bool inside = m > 0.2f || (m > -0.15f && ((x + y) & 1) == 0);
                        if (!inside) { px[y * w + x] = new Color(0f, 0f, 0f, 0f); continue; }
                        // 水面: 暗い青灰 (alpha 0.72)。縁 (m 小) はもう少し濃く = 湿った土の帯
                        float edge = Mathf.Clamp01(m / 1.2f);
                        var c = new Color(0.16f, 0.2f, 0.3f, Mathf.Lerp(0.85f, 0.7f, edge));
                        // 月の映り込み: 左上寄りの柔らかい楕円 + 細い横の光の筋
                        float hx = (fx + 0.35f) / 0.55f, hy = (fy - 0.25f) / 0.42f;
                        float hl = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy));
                        hl = hl * hl * 0.55f;
                        if (((y * 7 + x * 3) % 11) == 0 && r < 0.75f && rng.NextDouble() < 0.06) hl += 0.45f;    // きらめき
                        c = Color.Lerp(c, new Color(0.78f, 0.86f, 1f, 0.9f), hl);
                        px[y * w + x] = c;
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>さざ波のノイズ (64×64・Repeat・バイリニア)。水面シェーダが2枚重ねて流す</summary>
            public static Texture2D RippleNoise(System.Random rng)
            {
                int w = 64; var t = new Texture2D(w, w, TextureFormat.RGBA32, false); t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Repeat;
                var px = new Color[w * w]; float o1 = (float)rng.NextDouble() * 30f, o2 = (float)rng.NextDouble() * 30f;
                for (int y = 0; y < w; y++) for (int x = 0; x < w; x++)
                {
                    // 周期的にするため 2 方向の位相を合わせた正弦の重ね合わせ + パーリン
                    float u = x / (float)w, v = y / (float)w;
                    float n = 0.5f + 0.25f * Mathf.Sin((u * 3f + v * 1f) * Mathf.PI * 2f + o1) + 0.25f * Mathf.Sin((u * -1f + v * 4f) * Mathf.PI * 2f + o2);
                    n = Mathf.Lerp(n, Mathf.PerlinNoise(u * 4f + o1, v * 4f + o2), 0.2f);   // パーリンは周期的でないので薄く (継ぎ目)
                    px[y * w + x] = new Color(n, n, n, 1f);
                }
                t.SetPixels(px); t.Apply(); return t;
            }

            public static Texture2D Water(Pal p)
            {
                int w = 32, h = 32;
                var t = New(w, h, true);
                var px = new Color[w * h];
                var deep = new Color(0.14f, 0.24f, 0.42f, 0.78f); var light = new Color(0.7f, 0.85f, 1.05f, 0.75f);
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    float n = Vnoise(x * 0.35f + 11f, y * 0.35f + 4f);
                    bool ripple = ((x + y * 2 + (int)(n * 6)) % 9) < 2 && n > 0.3f;
                    px[y * w + x] = ripple ? light : deep;
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Gradient(Color bottom, Color top)
            {
                var t = new Texture2D(4, 64, TextureFormat.RGBA32, false);
                t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[4 * 64];
                for (int y = 0; y < 64; y++)
                {
                    float k = y / 63f;
                    var c = Mix(bottom, top, Mathf.Pow(k, 0.8f));
                    for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Disc(Color c)
            {
                int n = 32;
                var t = New(n, n, false);
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                        px[y * n + x] = d <= n / 2f - 0.5f ? c : Color.clear;
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Glow(Color c)
            {
                int n = 64;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                        float a = Mathf.Clamp01(1f - d);
                        px[y * n + x] = new Color(c.r, c.g, c.b, c.a * a * a);
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }
        }

        // ---------------------------------------------------------------- 粒子

        static Texture2D _dot;
        static Texture2D _glowDot;
        /// <summary>光の粒の絵: 明るい芯と柔らかい暈 (ブルームに乗る)</summary>
        static Texture2D GlowDotTex()
        {
            if (_glowDot != null) return _glowDot;
            _glowDot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            _glowDot.filterMode = FilterMode.Bilinear;
            var px = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
                    float core = Mathf.Clamp01(1f - d * 3.2f);                 // 芯 (半径 ≈ 1/3)
                    float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f) * 0.55f;   // 暈
                    px[y * 32 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + halo));
                }
            _glowDot.SetPixels(px); _glowDot.Apply();
            return _glowDot;
        }

        static Light _moteLightTpl;
        /// <summary>粒ごとの点光源のひな型 (ParticleSystem の Lights モジュールが複製する)。URP は Forward+ なので数の上限は気にしない</summary>
        static Light MoteLightTemplate()
        {
            if (_moteLightTpl != null) return _moteLightTpl;
            var go = new GameObject("mote-light-template");
            go.transform.SetParent(_fx, false);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.range = 1.1f; l.intensity = 0.55f; l.color = new Color(1f, 0.82f, 0.5f); l.shadows = LightShadows.None;
            go.SetActive(false);
            _moteLightTpl = l;
            return l;
        }

        static void MoteLights(ParticleSystem ps, int max, float ratio)
        {
            if (Application.isMobilePlatform) { max = Mathf.Max(2, max / 3); ratio *= 0.5f; }   // スマホ: 粒ごとの点光源を 1/3 に
            var lights = ps.lights;
            lights.enabled = true;
            lights.light = MoteLightTemplate();
            lights.ratio = ratio;                   // 照明になる粒の割合 (全部だと足元が光溜まりだらけになる)
            lights.maxLights = max;
            lights.useParticleColor = false;
            lights.sizeAffectsRange = false;
            lights.alphaAffectsIntensity = true;
            lights.rangeMultiplier = 1f;
            lights.intensityMultiplier = 1f;
        }

        static Texture2D DotTex()
        {
            if (_dot != null) return _dot;
            _dot = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            _dot.filterMode = FilterMode.Bilinear;
            var px = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f)) / 8f;
                    px[y * 16 + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d * d));
                }
            _dot.SetPixels(px); _dot.Apply();
            return _dot;
        }

        static Texture2D _leaf;
        static Texture2D LeafTex()
        {
            if (_leaf != null) return _leaf;
            string[] rows = { "........", "......##", ".....###", "....####", "...####.", "..###...", ".##.....", "#......." };
            _leaf = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            _leaf.filterMode = FilterMode.Point;
            var px = new Color[64];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    px[(7 - y) * 8 + x] = rows[y][x] == '#' ? Color.white : new Color(0f, 0f, 0f, 0f);
            _leaf.SetPixels(px); _leaf.Apply();
            return _leaf;
        }

        static ParticleSystem NewSystem(string name, Texture2D tex)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_fx, false);
            var ps = go.AddComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = GlowMaterial(tex);
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = 5;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;
            return ps;
        }

        /// <summary>蛍 (2026-09-08 ユーザー案「ホタルのような動く緑の発光する粒子」): 黄緑に瞬く小さな光。ふわりと漂い、ときどき止まる。
        /// 川辺と森の縁に多い。道を照らす光の粒 (金・流れる) とは色と動きで区別する</summary>
        static void Fireflies()
        {
            var ps = NewSystem("fireflies", GlowDotTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.2f, 2.2f, 0.6f, 1f), new Color(1.6f, 2.4f, 0.9f, 1f));
            main.maxParticles = 70;
            var em = ps.emission; em.rateOverTime = 7f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(40f, 2.2f, 22f);
            shape.rotation = new Vector3(0f, PathYaw, 0f);
            var c = Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(2f, 0f, 3.5f);
            shape.position = new Vector3(c.x, 1.1f, c.z);                                     // 空き地の奥〜川辺〜森の縁
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); vel.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.14f); vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.55f; noise.frequency = 0.5f; noise.scrollSpeed = 0.35f;
            // 瞬き: 寿命の中で 3 回ふわっと灯る (蛍の呼吸)。Gradient のアルファキーは 8 個まで (9 個目は Android で
            // 「Max number of alpha keys is 8」のエラーになり、ErrorOverlay を塞いでいた。2026-09-14)
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.05f, 0.26f), new GradientAlphaKey(1f, 0.42f),
                              new GradientAlphaKey(0.05f, 0.58f), new GradientAlphaKey(1f, 0.74f), new GradientAlphaKey(0.05f, 0.9f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            var curve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.08f, 1f), new Keyframe(0.2f, 0.5f), new Keyframe(0.34f, 1f), new Keyframe(0.48f, 0.5f), new Keyframe(0.62f, 1f), new Keyframe(0.76f, 0.5f), new Keyframe(0.88f, 1f), new Keyframe(1f, 0.6f));
            sz.size = new ParticleSystem.MinMaxCurve(1f, curve);
            ps.Play();
        }

        static void Dust()
        {
            var ps = NewSystem("dust", DotTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.035f);
            main.startColor = new Color(1f, 0.97f, 0.9f, 0.22f);
            main.maxParticles = 80;
            var em = ps.emission; em.rateOverTime = 5f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(22f, 8f, 14f); shape.position = new Vector3(0f, 3.5f, 3f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f); vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        /// <summary>光の粒: 露頭から漏れるマナの光 (2026-09-10 改稿。旧「月から零れた光」)。道筋に沿って坑口の方へゆっくり流れ、足元の一群が暖色の光源 (街灯の後継)</summary>
        static void Motes()
        {
            var dir = Quaternion.Euler(0f, PathYaw, 0f) * Vector3.right;   // 道の向き (+t = 坑口の方)
            var ps = NewSystem("motes-path", GlowDotTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
            main.startColor = new Color(1.6f, 1.3f, 0.65f, 1f);
            main.maxParticles = 50;
            var em = ps.emission; em.rateOverTime = 6f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(34f, 1.4f, 3.2f);
            shape.rotation = new Vector3(0f, PathYaw, 0f); shape.position = new Vector3(0f, 0.15f, 0f);   // 地面すれすれから湧く (露頭から漏れる光)
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(dir.x * 0.1f, dir.x * 0.3f); vel.z = new ParticleSystem.MinMaxCurve(dir.z * 0.1f, dir.z * 0.3f);
            vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);   // 上へ昇る (2026-09-10 改稿: 下から湧く)。3軸とも同じモード = 混ぜると Android で毎フレーム E ログ
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.4f; noise.scrollSpeed = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });   // 途中で消えない = 全部の粒が光っている
            col.color = g;
            MoteLights(ps, 14, 0.3f);
            ps.Play();

            var c = NewSystem("motes-cluster", GlowDotTex());
            main = c.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
            main.startColor = new Color(1.8f, 1.45f, 0.7f, 1f);
            main.maxParticles = 18;
            em = c.emission; em.rateOverTime = 9f;
            shape = c.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1.1f; shape.position = _lampPos;
            noise = c.noise; noise.enabled = true; noise.strength = 0.5f; noise.frequency = 0.6f; noise.scrollSpeed = 0.35f;
            vel = c.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f); vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.1f);
            col = c.colorOverLifetime; col.enabled = true; col.color = g;
            MoteLights(c, 4, 0.35f);
            c.Play();
        }

        /// <summary>地面の霧: 大きく淡い板がゆっくり流れる (中景〜遠景。戦闘の場は薄く)。遠景の帯は濃いめ = 段丘の奥行き</summary>
        static void Mist()
        {
            var dir = Quaternion.Euler(0f, PathYaw, 0f) * Vector3.right;
            var near = NewSystem("mist", GlowDotTex());
            var main = near.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(14f, 22f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
            main.startColor = new Color(0.62f, 0.74f, 1f, 0.05f);
            main.maxParticles = 16;
            var em = near.emission; em.rateOverTime = 0.9f;
            var shape = near.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(44f, 0.4f, 22f); shape.position = new Vector3(0f, 0.45f, 15f);
            var vel = near.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(dir.x * 0.05f, dir.x * 0.14f); vel.z = new ParticleSystem.MinMaxCurve(dir.z * 0.05f, dir.z * 0.14f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = near.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            near.Play();

            var far = NewSystem("mist-far", GlowDotTex());
            main = far.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(16f, 26f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startColor = new Color(0.6f, 0.72f, 1f, 0.09f);
            main.maxParticles = 14;
            em = far.emission; em.rateOverTime = 0.65f;
            shape = far.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(70f, 0.6f, 16f); shape.position = new Vector3(4f, 1.2f, 38f);
            vel = far.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            col = far.colorOverLifetime; col.enabled = true; col.color = g;
            far.Play();
        }

        /// <summary>月の塵: 銀色の小さな粒が舞台全体でゆっくり昇る (暖色ではないので光のルールに触れない)。
        /// 箱庭の月の塵 (moondust-slice・二周目 段2) はここでは作らない = R2B_MoondustSlice (SetFxForAct が箱庭で初めて要る時に作る。粒の系の順番を変えないため)</summary>
        static void Moondust()
        {
            var ps = NewSystem("moondust", GlowDotTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 15f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new Color(0.85f, 0.95f, 1.3f, 0.8f);
            main.maxParticles = 90;
            var em = ps.emission; em.rateOverTime = 7f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(40f, 6f, 30f); shape.position = new Vector3(0f, 2.5f, 12f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f); vel.y = new ParticleSystem.MinMaxCurve(0.04f, 0.14f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.2f; noise.frequency = 0.3f; noise.scrollSpeed = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        /// <summary>
        /// 二周目 段2 (R2B・計画 hd2d-round2-plan §2 レーン B の 11): 箱庭だけの月の塵の値 (光の設計図 look の "moondust")。slice が true でなければ null (= 今までの moondust)。
        /// 今の舞台 (stage=old) では StageLook を当てないので読まれない
        /// </summary>
        static Newtonsoft.Json.Linq.JObject R2B_MoondustSliceLook()
        {
            var look = StageLook.Current;
            var md = look != null && look.Raw != null ? look.Raw["moondust"] as Newtonsoft.Json.Linq.JObject : null;
            var sl = md != null ? md["slice"] : null;
            return sl != null && sl.Type == Newtonsoft.Json.Linq.JTokenType.Boolean && (bool)sl ? md : null;
        }

        static string R2B_moondustSig;

        /// <summary>
        /// 二周目 段2 (R2B): 箱庭だけの月の塵 moondust-slice。道の座標の箱 (中心 t・s・y と大きさ t・高さ・s) の中に銀の小さな粒が昇る。始めから満ちた状態 (prewarm・1周 12 秒)。
        /// 今の moondust (Moondust) とは別の粒の系だが、Moondust の中では作らず、箱庭で初めて要る時にここで作って StageFx のいちばん後ろへ置く:
        /// det の撮影 (Autopilot.DetSettleStage) は粒の系を階層の順に並べて順番で種を配るので、途中に足すと後ろの系 (幕2/3 の脈の粒・しずく・灰・火の粉) の種がずれ、今の舞台の画が W5 と変わる。
        /// いちばん後ろなら他の系の順番は変わらない。今の舞台だけの撮影では作られもしない。値が変わった時だけ作り直して頭から流す
        /// </summary>
        static void R2B_MoondustSlice()
        {
            var md = R2B_MoondustSliceLook();
            if (md == null || _fx == null) return;
            Vector3 c = R2B_V3(md["center"], new Vector3(3f, 3.5f, 3.5f));     // 道の座標 (t, s, y)
            Vector3 z = R2B_V3(md["size"], new Vector3(36f, 5f, 10f));         // 大きさ (t, 高さ, s)
            float rate = R2B_F(md["rate"], 10f), max = R2B_F(md["max"], 120f);
            float s0 = R2B_F(md["sizeMin"], 0.05f), s1 = R2B_F(md["sizeMax"], 0.11f);
            var ca = md["color"] as Newtonsoft.Json.Linq.JArray;
            var col = ca != null && ca.Count >= 4 ? new Color(R2B_F(ca[0], 1.3f), R2B_F(ca[1], 1.45f), R2B_F(ca[2], 1.9f), R2B_F(ca[3], 0.9f)) : new Color(1.3f, 1.45f, 1.9f, 0.9f);
            // 直しの輪2 (2026-10-01): sharp が true なら粒の芯が深さを書く材質 (StageDust) = ぼかしが芯を帯の中と読み、くっきりした小さな光の点になる。
            // 頂点の色は 0〜1 に丸められるので、色を最大の成分で割り、その成分を材質の _Intensity に (HDR の明るさを保つ)
            var shTok = md["sharp"];
            bool sharp = shTok != null && shTok.Type == Newtonsoft.Json.Linq.JTokenType.Boolean && (bool)shTok;
            float coreCut = Mathf.Clamp(R2B_F(md["coreCutoff"], 0.5f), 0.05f, 0.95f);   // 芯 (深さを書く所) の α のしきい。小さいほど芯が大きい
            float yaw = StageLook.Current != null ? StageLook.Current.PathYaw : PathYaw;
            var sigSb = new System.Text.StringBuilder();
            foreach (var v in new[] { c.x, c.y, c.z, z.x, z.y, z.z, rate, max, s0, s1, col.r, col.g, col.b, col.a, yaw, sharp ? 1f : 0f, coreCut }) sigSb.Append(v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            string sig = sigSb.ToString();

            var tr = _fx.Find("moondust-slice");
            ParticleSystem ps = tr != null ? tr.GetComponent<ParticleSystem>() : null;
            bool fresh = ps == null;
            if (fresh) ps = NewSystem("moondust-slice", GlowDotTex());
            ps.transform.SetAsLastSibling();   // 粒の系の順番で他の系の種を動かさない (上の説明)
            if (!fresh && sig == R2B_moondustSig && ps.gameObject.activeSelf) return;
            R2B_moondustSig = sig;
            ps.gameObject.SetActive(true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // duration は止めてからでないと書けない
            var pr = ps.GetComponent<ParticleSystemRenderer>();
            Material dm = sharp ? R2B_DustMaterial() : null;
            float hdr = Mathf.Max(1f, Mathf.Max(col.r, Mathf.Max(col.g, col.b)));
            if (dm != null)
            {
                dm.SetFloat("_Intensity", hdr);
                dm.SetFloat("_Cutoff", coreCut);
                pr.sharedMaterial = dm;
                pr.shadowCastingMode = ShadowCastingMode.Off; pr.receiveShadows = false;   // 深さを書いても影は落とさない
                col = new Color(col.r / hdr, col.g / hdr, col.b / hdr, col.a);
            }
            else if (pr.sharedMaterial == _r2bDustMat) pr.sharedMaterial = GlowMaterial(GlowDotTex());   // sharp を外した変種 = 今までの材質へ戻す
            var main = ps.main;
            main.duration = 12f;
            main.prewarm = true;                                               // 始めから満ちた状態 (det の撮影も 90 フレームで満ちる)
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 15f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Max(0.001f, s0), Mathf.Max(s0, s1));
            main.startColor = col;
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(max));
            var em = ps.emission; em.rateOverTime = Mathf.Max(0f, rate);
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box;
            shape.rotation = new Vector3(0f, yaw, 0f);
            shape.scale = new Vector3(z.x, z.y, z.z);                          // 箱の x = 道の t・y = 高さ・z = 道の s (shape.rotation で道の向きへ)
            var cw = Quaternion.Euler(0f, yaw, 0f) * new Vector3(c.x, 0f, c.y);
            shape.position = new Vector3(cw.x, c.z, cw.z);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f); vel.y = new ParticleSystem.MinMaxCurve(0.04f, 0.14f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.2f; noise.frequency = 0.3f; noise.scrollSpeed = 0.15f;
            var colL = ps.colorOverLifetime; colL.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colL.color = g;
            ps.Play();
        }

        static Material _r2bDustMat;
        /// <summary>直しの輪2: 月の塵の芯が深さを書く材質 (Resources/Shaders/StageDust)。不透明の列 2450。シェーダが無ければ null (= 今までの材質)</summary>
        static Material R2B_DustMaterial()
        {
            if (_r2bDustMat != null) return _r2bDustMat;
            var sh = Resources.Load<Shader>("Shaders/StageDust");
            if (sh == null) sh = Shader.Find("DeckRogue/StageDust");
            if (sh == null || !sh.isSupported) return null;
            _r2bDustMat = new Material(sh) { name = "moondust-slice-dust" };
            _r2bDustMat.mainTexture = GlowDotTex();
            _r2bDustMat.SetFloat("_Cutoff", 0.5f);
            _r2bDustMat.renderQueue = 2450;
            return _r2bDustMat;
        }

        static float R2B_F(Newtonsoft.Json.Linq.JToken t, float def)
        {
            return t != null && (t.Type == Newtonsoft.Json.Linq.JTokenType.Float || t.Type == Newtonsoft.Json.Linq.JTokenType.Integer) ? (float)t : def;
        }

        static Vector3 R2B_V3(Newtonsoft.Json.Linq.JToken t, Vector3 def)
        {
            var a = t as Newtonsoft.Json.Linq.JArray;
            return a != null && a.Count >= 3 ? new Vector3(R2B_F(a[0], def.x), R2B_F(a[1], def.y), R2B_F(a[2], def.z)) : def;
        }

        /// <summary>脈の粒: 結晶と露頭から青緑の粒がゆっくり昇る (幕2/3)</summary>
        static void VeinMotes()
        {
            var ps = NewSystem("vein-motes", GlowDotTex());
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f); main.startColor = new Color(0.5f, 1.4f, 1.3f, 1f); main.maxParticles = 70;
            var em = ps.emission; em.rateOverTime = 7f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(46f, 0.6f, 26f); shape.position = new Vector3(2f, 0.2f, 10f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f); vel.y = new ParticleSystem.MinMaxCurve(0.08f, 0.2f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.4f; noise.scrollSpeed = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g; ps.Play();
        }

        /// <summary>しずく: 天井から落ちる短い銀の筋 (幕2)</summary>
        static void Drips()
        {
            var ps = NewSystem("drips", GlowDotTex());
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.08f); main.startColor = new Color(0.8f, 0.95f, 1f, 0.9f); main.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 3f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(40f, 0.2f, 20f); shape.position = new Vector3(0f, 5.6f, 12f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.y = new ParticleSystem.MinMaxCurve(-6f, -5f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var ren = ps.GetComponent<ParticleSystemRenderer>(); ren.renderMode = ParticleSystemRenderMode.Stretch; ren.velocityScale = 0.06f; ren.lengthScale = 1f;
            ps.Play();
        }

        /// <summary>炉の火の粉 (幕2・2026-09-21): 炉の口から上へ舞い、橙から消える。位置は SetFxForAct が _emberPos (PaintMarket2 の炉) に置く</summary>
        static void Embers()
        {
            var ps = NewSystem("embers", GlowDotTex());
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f); main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.2f, 1.1f, 0.4f, 1f), new Color(2.4f, 0.7f, 0.25f, 1f)); main.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 8f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(0.6f, 0.3f, 0.4f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f); vel.y = new ParticleSystem.MinMaxCurve(0.3f, 0.6f); vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 1.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0.3f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        /// <summary>灰: 天井から細かい灰がゆっくり落ちる (幕3)</summary>
        static void AshFall()
        {
            var ps = NewSystem("ashfall", DotTex());
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f); main.startColor = new Color(0.75f, 0.85f, 0.9f, 0.5f); main.maxParticles = 120;
            var em = ps.emission; em.rateOverTime = 9f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(50f, 0.5f, 30f); shape.position = new Vector3(0f, 9f, 14f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f); vel.y = new ParticleSystem.MinMaxCurve(-0.5f, -0.3f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.5f; noise.scrollSpeed = 0.3f;
            ps.Play();
        }

        /// <summary>人魂: 淡い大きな光がゆっくり漂う (幕3。帰らなかった者の名残)</summary>
        static void Wisps()
        {
            var ps = NewSystem("wisps", GlowDotTex());
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 20f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f); main.startColor = new Color(0.6f, 1.2f, 1.2f, 0.35f); main.maxParticles = 6;
            var em = ps.emission; em.rateOverTime = 0.35f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(44f, 3f, 20f); shape.position = new Vector3(0f, 2.5f, 16f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f); vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.08f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.8f; noise.frequency = 0.25f; noise.scrollSpeed = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g; ps.Play();
        }

        /// <summary>水のきらめき: 川筋 (s≈6.6 の帯) に限定した小さな銀の粒。短命で瞬く</summary>
        static void WaterSparkle()
        {
            var ps = NewSystem("water-sparkle", GlowDotTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new Color(1.4f, 1.6f, 2.0f, 1f);
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 26f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(48f, 0.05f, 1.6f);
            shape.rotation = new Vector3(0f, PathYaw, 0f);
            var c = Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(2f, 0f, 5.7f);
            shape.position = new Vector3(c.x, -0.02f, c.z);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        static void Leaves()
        {
            var ps = NewSystem("leaves", LeafTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.66f, 0.42f, 0.9f), new Color(0.7f, 0.56f, 0.3f, 0.9f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.maxParticles = 16;
            var em = ps.emission; em.rateOverTime = 0.6f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(22f, 0.5f, 12f); shape.position = new Vector3(0f, 8f, 4f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.15f); vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.5f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.5f; noise.scrollSpeed = 0.3f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            ps.Play();
        }
    }
}
