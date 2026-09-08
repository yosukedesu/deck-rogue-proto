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
    public static class Stage
    {
        public const float Fov = 36f;                   // 広めの画角 = 手前が大きく奥が小さい (奥行きが読める)
        public const float Pitch = 12f;                         // 見下ろし。上端の視線は水平より 6° 上 = 空の帯に月と塔が入る (2026-09-08 世界観「月の塔」。16° では帯が 2° で塔が山に隠れた)
        const float PathYaw = -22f;                     // 道の向き (手前左 → 奥右)。隊列もこの線に沿う
        const float PlaneUnitsPerScreen = 10.8f;        // 基準深度で画面の高さ = 10.8 units
        const float GroundLineRatio = 0.45f;            // 画面の下から何割に world 原点を置くか
        const float Tile = 1.28f;                        // 32ドットのタイル1枚 = 1.28 units (基準深度で 4px/ドット)
        static readonly Color ShadowColor = new Color(0.02f, 0.02f, 0.08f, 0.92f);   // 接地影: 地面より暗く青寄り。幅0.8・高さ0.35・足元中心・地面とスプライトの間

        static Camera _cam;
        static Volume _volume;
        static ColorAdjustments _color;
        static DepthOfField _dof;
        static Transform _world, _fx, _units;
        static Light _sun, _lantern;
        static StageDriver _driver;
        static int _paintedAct = -1;
        static float _dist, _k;
        static Vector3 _fwd = Vector3.forward, _up = Vector3.up, _right = Vector3.right, _camBase;
        static Shader _unitShader;
        static Material _dioramaBase, _cutoutBase;
        static Mesh _quad, _cross, _quadCentered;
        static Texture2D _blobTex, _stripTex;
        static Pal _pal;
        static Vector3 _lampPos;
        static readonly Dictionary<string, StageUnit> _bound = new Dictionary<string, StageUnit>();
        static readonly Dictionary<string, float> _depths = new Dictionary<string, float>();
        static readonly Dictionary<string, float> _feetOffsets = new Dictionary<string, float>();

        public static Quaternion CameraRotation { get { return _cam != null ? _cam.transform.rotation : Quaternion.identity; } }

        // ---------------------------------------------------------------- 準備

        public static void Ensure()
        {
            if (_world != null) return;
            _cam = Camera.main;
            if (_cam == null)
            {
                var cgo = new GameObject("StageCamera");
                _cam = cgo.AddComponent<Camera>();
                cgo.tag = "MainCamera";
            }
            _cam.orthographic = false;
            _cam.fieldOfView = Fov;
            _cam.nearClipPlane = 0.3f;
            _cam.farClipPlane = 220f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = PaperFx.Night;
            _cam.allowHDR = true;
            try
            {
                var data = _cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None;
                data.renderShadows = true;
            }
            catch (Exception e) { Debug.LogWarning("[Stage] URP のカメラ設定に失敗: " + e.Message); }

            _unitShader = Shader.Find("DeckRogue/StageUnit");
            if (_unitShader == null || !_unitShader.isSupported) { Debug.LogWarning("[Stage] StageUnit シェーダが無い → Sprites/Default"); _unitShader = Shader.Find("Sprites/Default"); }
            _dioramaBase = Resources.Load<Material>("Materials/Diorama");
            if (_dioramaBase == null || _dioramaBase.shader == null || !_dioramaBase.shader.isSupported)
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Sprites/Default");
                _dioramaBase = new Material(lit);
            }
            _cutoutBase = Resources.Load<Material>("Materials/DioramaCutout");
            if (_cutoutBase == null || _cutoutBase.shader == null || !_cutoutBase.shader.isSupported)
            {
                _cutoutBase = new Material(_dioramaBase);
                _cutoutBase.SetFloat("_AlphaClip", 1f); _cutoutBase.EnableKeyword("_ALPHATEST_ON"); _cutoutBase.SetFloat("_Cull", 0f);
            }
            _quad = BuildQuad();
            _quadCentered = BuildQuadCentered();
            _cross = BuildCross();

            // ポスト処理: 遠景だけ滑らかにぼける (中距離でドットとボケを混ぜない)・ブルーム (月・街灯)・ビネット・夜の色補正
            try
            {
                var vgo = new GameObject("StageVolume");
                _volume = vgo.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 1f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _dof = profile.Add<DepthOfField>(true);
                _dof.mode.value = DepthOfFieldMode.Bokeh;      // 手前 (崖下の前景) も軽くぼける。中距離は 2px 未満
                _dof.focalLength.value = 300f;
                _dof.aperture.value = 9f;
                _dof.bladeCount.value = 6;
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 0.9f;
                bloom.intensity.value = 1.6f;
                bloom.scatter.value = 0.7f;
                bloom.tint.value = new Color(1f, 0.9f, 0.72f);
                var vig = profile.Add<Vignette>(true);
                vig.intensity.value = 0.34f;
                vig.smoothness.value = 0.6f;
                vig.color.value = new Color(0.02f, 0.02f, 0.06f);
                _color = profile.Add<ColorAdjustments>(true);
                _color.postExposure.value = -0.15f;
                _color.contrast.value = 16f;
                _color.saturation.value = -6f;
                _volume.sharedProfile = profile;
            }
            catch (Exception e) { Debug.LogWarning("[Stage] Volume の作成に失敗: " + e.Message); }

            _world = new GameObject("StageWorld").transform;
            _units = new GameObject("StageUnits").transform;
            _fx = new GameObject("StageFx").transform;
            _driver = _world.gameObject.AddComponent<StageDriver>();

            // ライト: 月光は右上から (影は左下へ落ちる)。街灯は暖色の点光源
            var sgo = new GameObject("Moonlight");
            _sun = sgo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.7f;
            _sun.shadowBias = 0.05f;
            _sun.shadowNormalBias = 0.5f;
            sgo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);    // 右上・手前から = 壁の正面と右面に光、左下が暗い
            var lgo = new GameObject("Lantern");
            _lantern = lgo.AddComponent<Light>();
            _lantern.type = LightType.Point;
            _lantern.range = 8.5f;
            _lantern.shadows = LightShadows.None;
            _lampPos = OnPath(-7.1f, 2.2f) + new Vector3(0f, 1.2f, 0f);   // 光の粒の群れの中心 (街灯は撤去。世界観「あかりは装置でなく月から零れた光の粒」2026-09-08)
            lgo.transform.position = _lampPos;

            LayoutCamera();
            Fireflies();
            Dust();
            Leaves();
            Motes();
        }

        /// <summary>カメラの位置: 基準深度 _dist で 1 unit = 100px、world 原点が画面の下から GroundLineRatio に来る</summary>
        static void LayoutCamera()
        {
            float H = Screen.height;
            if (H < 1f) H = 1080f;
            _k = PlaneUnitsPerScreen / H;
            _dist = (PlaneUnitsPerScreen * 0.5f) / Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            var rot = Quaternion.Euler(Pitch, 0f, 0f);
            _fwd = rot * Vector3.forward; _up = rot * Vector3.up; _right = Vector3.right;
            float dy = (H * GroundLineRatio - H * 0.5f) * _k;
            Vector3 p0 = -_up * dy;
            _camBase = p0 - _fwd * _dist;
            _cam.transform.position = _camBase;
            _cam.transform.rotation = rot;
            if (_dof != null) _dof.focusDistance.value = _dist + 0.8f;
        }

        static float ScaleFactor()
        {
            var g = GameRoot.I;
            var canvas = g != null && g.ScreenRoot != null ? g.ScreenRoot.GetComponentInParent<Canvas>() : null;
            return canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        }

        /// <summary>画面座標 (px・左下原点) を、深度 depth の面 (視線に垂直) 上の world 座標へ</summary>
        static Vector3 ScreenToPlane(float sx, float sy, float depth)
        {
            float W = Screen.width, H = Screen.height;
            float k = _k * depth / _dist;
            return _camBase + _fwd * depth + _right * ((sx - W * 0.5f) * k) + _up * ((sy - H * 0.5f) * k);
        }

        /// <summary>world の地面の点を画面 (UI キャンバスの px・左下原点) へ写し、その深度を key に覚える</summary>
        public static Vector2 ProjectFeet(string key, Vector3 world)
        {
            Ensure();
            var rel = world - _camBase;
            float d = Vector3.Dot(rel, _fwd);
            float x = Vector3.Dot(rel, _right), y = Vector3.Dot(rel, _up);
            float W = Screen.width, H = Screen.height;
            float k = _k * d / _dist;
            float sx = W * 0.5f + x / k, sy = H * 0.5f + y / k;
            _depths[key] = d;
            float sf = ScaleFactor();
            return new Vector2(sx / sf, sy / sf);
        }

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
        const float Cell = 0.5f, Step = 1.1f;
        const float TX0 = -56f, TZ0 = -24f;
        const int TNX = 224, TNZ = 200;
        static float[,] _H;
        static bool[,] _Dirt;
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
            float e = ((t - 2f) / 15f) * ((t - 2f) / 15f) + ((s - 0.5f) / 6f) * ((s - 0.5f) / 6f);
            return 1f - Mathf.Clamp01((e - 0.7f) / 0.5f);
        }
        static float RawHeight(float x, float z)
        {
            float t, s; PathLocal(x, z, out t, out s);
            float n = Fbm(x, z);
            float rise = Mathf.Max(0f, s - 4f) * 0.1f;                         // 奥へ緩く上がる
            float drop = s < -4f ? -(1.4f + (-4f - s) * 0.25f) : 0f;             // 手前は下がる
            float h = (n - 0.42f) * 3.2f + rise + drop;
            h = Mathf.Min(h, 3.3f);
            if (z > 34f) h = Mathf.Min(h, 2.2f - (z - 34f) * 0.02f);             // 遠景は低く = 山の稜線と空の帯が残る
            return Mathf.Lerp(h, 0f, ArenaMask(t, s));
        }
        static float Quantize(float h, float arena) { return arena > 0.99f ? 0f : Mathf.Round(h / Step) * Step; }
        static bool DirtAt(float x, float z, float h)
        {
            if (Mathf.Abs(h) > 0.01f) return false;
            float t, s; PathLocal(x, z, out t, out s);
            float e = ((t - 2f) / 11.5f) * ((t - 2f) / 11.5f) + ((s - 0.4f) / 3.4f) * ((s - 0.4f) / 3.4f)
                    + (Vnoise(x * 0.3f + 5f, z * 0.3f + 9f) - 0.5f) * 1.2f + (Vnoise(x * 0.9f + 2f, z * 0.9f + 4f) - 0.5f) * 0.35f;   // 縁を大きく・細かく揺らす
            return e < 1f;
        }
        static int CellI(float x) { return Mathf.Clamp(Mathf.FloorToInt((x - TX0) / Cell), 0, TNX - 1); }
        static int CellJ(float z) { return Mathf.Clamp(Mathf.FloorToInt((z - TZ0) / Cell), 0, TNZ - 1); }
        public static float GroundY(float x, float z) { return _H == null ? 0f : _H[CellI(x), CellJ(z)]; }
        static bool IsDirt(float x, float z) { return _Dirt != null && _Dirt[CellI(x), CellJ(z)]; }

        static void BuildTerrain(Material mGrass, Material mDirt, Material mCliff)
        {
            _tseed = 1000 + _paintedAct * 7;
            _H = new float[TNX, TNZ]; _Dirt = new bool[TNX, TNZ];
            for (int i = 0; i < TNX; i++)
                for (int j = 0; j < TNZ; j++)
                {
                    float x = TX0 + (i + 0.5f) * Cell, z = TZ0 + (j + 0.5f) * Cell;
                    float t, s; PathLocal(x, z, out t, out s);
                    _H[i, j] = Quantize(RawHeight(x, z), ArenaMask(t, s));
                    _Dirt[i, j] = DirtAt(x, z, _H[i, j]);
                }
            var top = new MB(); var dirtTop = new MB(); var walls = new MB(); var strips = new MB();
            var strong = new Color(1f, 1f, 1f, 1f); var clear = new Color(1f, 1f, 1f, 0f);
            for (int i = 0; i < TNX; i++)
                for (int j = 0; j < TNZ; j++)
                {
                    float h = _H[i, j];
                    float x0 = TX0 + i * Cell, x1 = x0 + Cell, z0 = TZ0 + j * Cell, z1 = z0 + Cell;
                    if (_Dirt[i, j]) dirtTop.Floor(x0, z0, x1, z1, h);
                    else top.FloorRot(x0, z0, x1, z1, h, (int)(Hash01(i * 3 + 11, j * 5 + 7) * 4f));
                    // 崖面: 隣が低ければその側に壁を張る (自分が高い側)。手前向きと横向きの壁の根元には影の帯
                    if (j > 0 && _H[i, j - 1] < h - 0.01f)
                    {
                        float hn = _H[i, j - 1];
                        walls.WallZ(x0, x1, hn, h, z0);
                        strips.QuadC(new Vector3(x0, hn + 0.03f, z0), new Vector3(x0, hn + 0.03f, z0 - 1.8f), new Vector3(x1, hn + 0.03f, z0 - 1.8f), new Vector3(x1, hn + 0.03f, z0), strong, clear, clear, strong);
                    }
                    if (j < TNZ - 1 && _H[i, j + 1] < h - 0.01f)
                    {
                        float hn = _H[i, j + 1];
                        walls.Quad(new Vector3(x1, hn, z1), new Vector3(x1, h, z1), new Vector3(x0, h, z1), new Vector3(x0, hn, z1),
                                   new Vector2(x1, hn) / Tile, new Vector2(x1, h) / Tile, new Vector2(x0, h) / Tile, new Vector2(x0, hn) / Tile);
                    }
                    if (i > 0 && _H[i - 1, j] < h - 0.01f)
                    {
                        float hn = _H[i - 1, j];
                        walls.Quad(new Vector3(x0, hn, z1), new Vector3(x0, h, z1), new Vector3(x0, h, z0), new Vector3(x0, hn, z0),
                                   new Vector2(z1, hn) / Tile, new Vector2(z1, h) / Tile, new Vector2(z0, h) / Tile, new Vector2(z0, hn) / Tile);
                        strips.QuadC(new Vector3(x0, hn + 0.03f, z0), new Vector3(x0 - 1.0f, hn + 0.03f, z0), new Vector3(x0 - 1.0f, hn + 0.03f, z1), new Vector3(x0, hn + 0.03f, z1), strong, clear, clear, strong);
                    }
                    if (i < TNX - 1 && _H[i + 1, j] < h - 0.01f)
                    {
                        float hn = _H[i + 1, j];
                        walls.Quad(new Vector3(x1, hn, z0), new Vector3(x1, h, z0), new Vector3(x1, h, z1), new Vector3(x1, hn, z1),
                                   new Vector2(z0, hn) / Tile, new Vector2(z0, h) / Tile, new Vector2(z1, h) / Tile, new Vector2(z1, hn) / Tile);
                        strips.QuadC(new Vector3(x1, hn + 0.03f, z1), new Vector3(x1 + 1.0f, hn + 0.03f, z1), new Vector3(x1 + 1.0f, hn + 0.03f, z0), new Vector3(x1, hn + 0.03f, z0), strong, clear, clear, strong);
                    }
                }
            Solid("terrain-grass", top, mGrass);
            Solid("terrain-dirt", dirtTop, mDirt);
            Solid("terrain-cliff", walls, mCliff);
            var sgo = new GameObject("terrain-shadow");
            sgo.transform.SetParent(_world, false);
            sgo.AddComponent<MeshFilter>().sharedMesh = strips.Build();
            var smr = sgo.AddComponent<MeshRenderer>();
            smr.sharedMaterial = GlowMaterial(Px.Solid(Color.white));
            smr.sharedMaterial.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, 1f);
            smr.shadowCastingMode = ShadowCastingMode.Off; smr.receiveShadows = false;
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

        public static Vector3 LeaderSlot() { return OnPath(-5.0f, 0.9f); }

        /// <summary>敵は道に沿って奥右へ (本家の3/4ジオラマの対角線の隊列)。横に少しずらして一直線を崩す</summary>
        public static Vector3[] EnemySlots(int n)
        {
            n = Math.Max(1, n);
            var r = new Vector3[n];
            float[] t = n == 1 ? new[] { 4.6f } : n == 2 ? new[] { 3.2f, 7.6f } : n == 3 ? new[] { 2.2f, 5.8f, 9.4f } : new[] { 1.6f, 4.8f, 8.0f, 11.2f };
            for (int i = 0; i < n; i++) r[i] = OnPath(t[i], (i % 2 == 0) ? -0.5f : 0.7f);
            return r;
        }

        /// <summary>UI の入れ物の下端から足元までの高さ (敵ごとに違う。名前札や HP バーは入れ物の下端基準で同じ線に揃う)</summary>
        public static void SetFeetOffset(string key, float y) { _feetOffsets[key] = y; }
        public static float FeetOffset(string key, float fallback) { float y; return _feetOffsets.TryGetValue(key, out y) ? y : fallback; }

        // ---------------------------------------------------------------- キャラ (UI の矩形に追従するビルボード)

        /// <summary>UI の矩形 (uGUI の sprite 枠) に追従するビルボードを、key の座席の深度の面に立てる。Image は非表示にして色 (生死・点滅) だけ読む</summary>
        public static void BindUnit(string key, RectTransform rect, Image img, Sprite sprite)
        {
            Ensure();
            if (rect == null || sprite == null || sprite.texture == null) return;
            if (img != null) img.enabled = false;
            float depth;
            if (!_depths.TryGetValue(key, out depth)) depth = _dist;

            var go = new GameObject("unit-" + key);
            go.transform.SetParent(_units, false);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = SpriteMat(sprite.texture, 0.4f, 0.35f);
            var tr = sprite.textureRect;
            float tw = sprite.texture.width, th = sprite.texture.height;
            mat.SetTextureScale("_BaseMap", new Vector2(tr.width / tw, tr.height / th));
            mat.SetTextureOffset("_BaseMap", new Vector2(tr.x / tw, tr.y / th));
            mat.SetFloat("_Rim", 0.5f);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;   // 影は接地影 (楕円) で
            mr.receiveShadows = false;
            var sh = Blob("shadow-" + key, _units, Vector3.zero, 1f);
            var u = go.AddComponent<StageUnit>();
            u.Rect = rect; u.Img = img; u.Mat = mat; u.Rend = mr; u.Depth = depth; u.Shadow = sh.transform;
            if (key == "player")
            {
                // 杖の先の光: 板の右上 (絵の uv≈0.64,0.93) に追従する淡い暖色のハロー
                var halo = new GameObject("staff-glow");
                halo.transform.SetParent(_units, false);
                halo.AddComponent<MeshFilter>().sharedMesh = _quadCentered;
                var hmr = halo.AddComponent<MeshRenderer>();
                hmr.sharedMaterial = GlowMaterial(Px.Radial(new Color(1f, 0.86f, 0.5f, 0.5f)));
                hmr.shadowCastingMode = ShadowCastingMode.Off; hmr.receiveShadows = false;
                u.Halo = halo.transform; u.HaloUv = new Vector2(0.64f, 0.93f);
            }
            u.LateUpdate();
            _bound[key] = u;
        }

        /// <summary>被弾の白い点滅</summary>
        public static void Flash(string key, float dur = 0.18f)
        {
            StageUnit u;
            if (_bound.TryGetValue(key, out u) && u != null) u.FlashT = dur;
        }

        /// <summary>画面揺れは舞台 (カメラ) が揺れる。紙の UI は揺れない</summary>
        public static void Shake(float px, float dur)
        {
            if (_driver == null) return;
            _driver.ShakeAmp = Mathf.Max(_driver.ShakeAmp, px);
            _driver.ShakeT = Mathf.Max(_driver.ShakeT, dur);
            _driver.ShakeDur = Mathf.Max(0.05f, dur);
        }

        class StageUnit : MonoBehaviour
        {
            public RectTransform Rect; public Image Img; public Material Mat; public MeshRenderer Rend; public float FlashT; public float Depth; public Transform Shadow;
            public Transform Halo; public Vector2 HaloUv;
            static readonly Vector3[] _c = new Vector3[4];
            void OnDestroy() { if (Shadow != null) Destroy(Shadow.gameObject); if (Halo != null) Destroy(Halo.gameObject); }
            public void LateUpdate()
            {
                if (Rect == null) { Destroy(gameObject); return; }
                Rect.GetWorldCorners(_c);
                float sx = Mathf.Round((_c[0].x + _c[3].x) * 0.5f), sy = Mathf.Round(_c[0].y);
                float w = Mathf.Round(_c[3].x - _c[0].x), h = Mathf.Round(_c[1].y - _c[0].y);
                float k = _k * Depth / _dist;
                var pos = ScreenToPlane(sx, sy, Depth);
                transform.position = pos;
                transform.rotation = CameraRotation;
                transform.localScale = new Vector3(Mathf.Max(0.01f, w * k), Mathf.Max(0.01f, h * k), 1f);
                var tint = Img != null ? Img.color : Color.white;
                Mat.SetColor("_BaseColor", tint);
                ApplyLight(Mat, 0.35f);
                if (FlashT > 0f) FlashT -= Time.deltaTime;
                Mat.SetFloat("_Flash", Mathf.Clamp01(FlashT / 0.18f) * 0.85f);
                if (Shadow != null)
                {
                    float ww = w * k;
                    PlaceBlob(Shadow, new Vector3(pos.x, 0f, pos.z), ww, tint.a);
                }
                if (Halo != null)
                {
                    float ww = w * k, hh = h * k;
                    Halo.position = pos + _right * ((HaloUv.x - 0.5f) * ww) + _up * (HaloUv.y * hh) - _fwd * 0.05f;
                    Halo.rotation = CameraRotation;
                    float sz = 0.9f * (1f + 0.06f * Mathf.Sin(Time.time * 5f));
                    Halo.localScale = new Vector3(sz, sz, 1f);
                }
            }
        }

        class StageDriver : MonoBehaviour
        {
            public float ShakeAmp, ShakeT, ShakeDur = 0.3f;
            void LateUpdate()
            {
                if (_cam == null) return;
                Vector3 off = Vector3.zero;
                if (ShakeT > 0f)
                {
                    ShakeT -= Time.deltaTime;
                    float a = ShakeAmp * Mathf.Clamp01(ShakeT / ShakeDur) * _k;
                    off = _right * (UnityEngine.Random.Range(-a, a)) + _up * (UnityEngine.Random.Range(-a, a));
                    if (ShakeT <= 0f) ShakeAmp = 0f;
                }
                _cam.transform.position = _camBase + off;
                float t = Time.time;
                for (int i = 0; i < _world.childCount; i++)
                {
                    var c = _world.GetChild(i);
                    if (c.name == "lantern-flame") c.localScale = new Vector3(0.36f, 0.36f, 1f) * (1f + 0.08f * Mathf.Sin(t * 9f) + 0.05f * Mathf.Sin(t * 23f));
                }
                if (_lantern != null) _lantern.intensity = _pal.LampIntensity * (1f + 0.06f * Mathf.Sin(t * 9f) + 0.04f * Mathf.Sin(t * 23f));
            }
        }

        // ---------------------------------------------------------------- 光と影の共通部品

        /// <summary>ドット絵の板の材質 (アンリット + 環境光 + 街灯 + 月光の勾配 + 接地影は別)</summary>
        static Material SpriteMat(Texture2D tex, float cutoff, float sunAmount)
        {
            var m = new Material(_unitShader);
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex;
            m.SetFloat("_Cutoff", cutoff);
            m.SetFloat("_Fog", 1f);
            ApplyLight(m, sunAmount);
            return m;
        }

        static void ApplyLight(Material m, float sunAmount)
        {
            bool painted = _pal.LampOnUnits > 0f;
            m.SetColor("_Ambient", painted ? _pal.UnitAmbient : Color.white);
            m.SetVector("_LampPos", _lampPos);
            m.SetColor("_LampColor", painted ? _pal.Lantern : Color.black);
            m.SetFloat("_LampStrength", painted ? _pal.LampOnUnits : 0f);
            m.SetFloat("_LampFalloff", 7f);
            m.SetVector("_SunDir2", new Vector4(0.7f, 0.7f, 0f, 0f));   // 右上が光源側
            m.SetFloat("_SunAmount", painted ? sunAmount : 0f);
        }

        static Texture2D BlobTex()
        {
            if (_blobTex != null) return _blobTex;
            _blobTex = Px.Glow(new Color(1f, 1f, 1f, 1f));
            return _blobTex;
        }

        /// <summary>接地影の楕円を作る (地面に寝かせた半透明の板)</summary>
        static GameObject Blob(string name, Transform parent, Vector3 basePos, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quadCentered;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GlowMaterial(BlobTex());
            mr.sharedMaterial.color = ShadowColor;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            PlaceBlob(go.transform, basePos, width, 1f);
            return go;
        }

        /// <summary>接地影: 横幅 0.8 倍・奥行き 0.4 倍。Euler(90,0,0) の板は原点から −z (手前) へ伸びるので、中心が足元に来るよう +d/2 に置く。
        /// 光源 (右上・手前) の反対 = 左奥へ少し寄せる</summary>
        static void PlaceBlob(Transform blob, Vector3 basePos, float width, float alpha)
        {
            float w = width * 0.8f, d = width * 0.35f;
            blob.position = new Vector3(basePos.x, basePos.y + 0.03f, basePos.z);   // 中心原点の板 = そのまま足元
            blob.rotation = Quaternion.Euler(90f, 0f, 0f);
            blob.localScale = new Vector3(w, d, 1f);
            var mr = blob.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, ShadowColor.a * alpha);
        }

        // ---------------------------------------------------------------- 箱庭

        struct Pal
        {
            public Color SkyTop, SkyBot, Fog, GrassA, GrassB, GrassC, GrassDry, DirtA, DirtB, StoneA, StoneB, CliffA, CliffB, LeafA, LeafB, LeafC, Trunk, Ambient, Sun, Lantern, Filter, UnitAmbient;
            public float LampOnUnits, LampIntensity, SunIntensity;
        }

        static Pal PalOf(int act)
        {
            var p = new Pal();
            if (act == 2)
            {
                p.SkyTop = UiKit.Hex("#0d1a22"); p.SkyBot = UiKit.Hex("#2a4a52"); p.Fog = UiKit.Hex("#6f8f99");
                p.GrassA = UiKit.Hex("#3f5c3a"); p.GrassB = UiKit.Hex("#324a30"); p.GrassC = UiKit.Hex("#5a7a4a"); p.GrassDry = UiKit.Hex("#6c7a4c");
                p.DirtA = UiKit.Hex("#55504a"); p.DirtB = UiKit.Hex("#43403a");
                p.StoneA = UiKit.Hex("#66717a"); p.StoneB = UiKit.Hex("#4c565c");
                p.CliffA = UiKit.Hex("#3f484c"); p.CliffB = UiKit.Hex("#2e3538");
                p.LeafA = UiKit.Hex("#25564c"); p.LeafB = UiKit.Hex("#1a3f38"); p.LeafC = UiKit.Hex("#3f7a66"); p.Trunk = UiKit.Hex("#3c3a38");
                p.Ambient = new Color(0.26f, 0.36f, 0.42f); p.Sun = new Color(0.55f, 0.85f, 0.92f); p.Lantern = new Color(0.55f, 0.95f, 1f);
                p.Filter = new Color(0.86f, 1.0f, 1.04f); p.UnitAmbient = new Color(0.62f, 0.8f, 0.86f);
            }
            else if (act == 3)
            {
                p.SkyTop = UiKit.Hex("#200c16"); p.SkyBot = UiKit.Hex("#5c2838"); p.Fog = UiKit.Hex("#8a5a66");
                p.GrassA = UiKit.Hex("#5a4a3a"); p.GrassB = UiKit.Hex("#463a2e"); p.GrassC = UiKit.Hex("#726048"); p.GrassDry = UiKit.Hex("#7a6a50");
                p.DirtA = UiKit.Hex("#5c4242"); p.DirtB = UiKit.Hex("#4a3333");
                p.StoneA = UiKit.Hex("#6c5b60"); p.StoneB = UiKit.Hex("#54464a");
                p.CliffA = UiKit.Hex("#4a3a3c"); p.CliffB = UiKit.Hex("#362a2c");
                p.LeafA = UiKit.Hex("#5e2632"); p.LeafB = UiKit.Hex("#3f1a22"); p.LeafC = UiKit.Hex("#8a4048"); p.Trunk = UiKit.Hex("#3a2a2a");
                p.Ambient = new Color(0.42f, 0.28f, 0.32f); p.Sun = new Color(1f, 0.72f, 0.62f); p.Lantern = new Color(1f, 0.6f, 0.35f);
                p.Filter = new Color(1.06f, 0.92f, 0.9f); p.UnitAmbient = new Color(0.86f, 0.7f, 0.72f);
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
                p.Ambient = new Color(0.25f, 0.29f, 0.5f); p.Sun = new Color(0.6f, 0.68f, 1f); p.Lantern = new Color(1f, 0.72f, 0.4f);
                p.Filter = new Color(0.84f, 0.9f, 1.12f); p.UnitAmbient = new Color(0.5f, 0.57f, 0.88f);   // 環境光は青く暗め = 街灯の暖色が読める
            }
            p.LampOnUnits = 1.25f; p.LampIntensity = 5.5f; p.SunIntensity = 0.8f;   // 補間の強さ (1 で街灯の色そのもの)
            return p;
        }

        /// <summary>幕の箱庭を組む (同じ幕なら組み直さない)</summary>
        public static void Paint(int act)
        {
            Ensure();
            if (_paintedAct == act) return;
            _paintedAct = act;
            for (int i = _world.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_world.GetChild(i).gameObject);
            var p = PalOf(act);
            _pal = p;
            var rng = new System.Random(1000 + act * 17);

            _cam.backgroundColor = p.SkyTop;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = p.Ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.Fog;                  // 空気遠近: 遠くほど白く薄く。最上段はここで薄れて終わる
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 62f;
            _sun.color = p.Sun; _sun.intensity = p.SunIntensity;
            _lantern.color = p.Lantern; _lantern.intensity = p.LampIntensity;
            if (_color != null) _color.colorFilter.value = p.Filter;

            // 地面と道
            var grass = Px.Grass(p, rng); var dirt = Px.Dirt(p, rng); var stone = Px.Stone(p, rng); var cliff = Px.Cliff(p, rng);
            var mGrass = Lit(Tex(act, "grass", grass)); var mDirt = Lit(Tex(act, "dirt", dirt)); var mStone = Lit(Tex(act, "stone", stone)); var mCliff = Lit(Tex(act, "cliff", cliff));
            // PixelLab のタイルは昼の色で描かれるので、取り込んだ時だけ幕の夜のパレットへ寄せる (仮のタイルは元から夜の色)
            if (HasTile(act, "grass")) mGrass.SetColor("_BaseColor", new Color(0.58f, 0.72f, 0.66f));
            if (HasTile(act, "dirt")) mDirt.SetColor("_BaseColor", new Color(0.66f, 0.6f, 0.56f));    // 土=灰茶 (月明かりの空き地。影が読める明るさ)。暖色は街灯の範囲だけ
            if (HasTile(act, "stone")) mStone.SetColor("_BaseColor", new Color(0.56f, 0.62f, 0.76f)); // 石=青灰
            if (HasTile(act, "cliff")) mCliff.SetColor("_BaseColor", new Color(0.6f, 0.52f, 0.48f));  // 崖=土色
            // ---- 段丘の地形 (本家の段々畑のような起伏): 高さ場を段に量子化し、段差に崖面を張る。戦闘の場だけ平らに均す
            BuildTerrain(mGrass, mDirt, mCliff);

            // 土の空き地の縁: 草に食われた縁と土のこぼれ (不定形の塊の境界に置く)
            var mBite = Cutout(Px.Patch(p.GrassA, p.GrassB, rng));
            var mSpill = Cutout(Px.Patch(p.DirtA, p.DirtB, rng));
            var edges = DirtEdgeCells();
            for (int i = 0; i < edges.Count; i++) { int j = rng.Next(i, edges.Count); var tmp = edges[i]; edges[i] = edges[j]; edges[j] = tmp; }
            for (int i = 0; i < Mathf.Min(44, edges.Count); i++)
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
            var tuft = Px.Tuft(p, rng); var pebble = Px.Pebble(p, rng); var flower = Px.Flower(p, rng);
            var mPebble = Cutout(pebble);
            for (int i = 0; i < 46; i++)
            {
                float t = -22f + (float)rng.NextDouble() * 46f;
                float sv = -5f + (float)rng.NextDouble() * 15f;
                var w = OnPath(t, sv);
                if (IsDirt(w.x, w.z)) { if (rng.NextDouble() < 0.5) Decal("pebble", mPebble, w.x, w.z, 0.36f + (float)rng.NextDouble() * 0.16f, 0.24f, (float)rng.NextDouble() * 360f); continue; }
                if (rng.NextDouble() < 0.15) Plane("flower", flower, w, 0.46f, 0.5f, false);
                else Plane("tuft", tuft, w, 0.44f + (float)rng.NextDouble() * 0.18f, 0.5f, false);
            }

            // 遺跡の柱 (石) — 段丘の上に (額縁)
            { var w = OnPath(-15f, 6.5f); Pillar(w.x, w.y, w.z, 1.2f, 4.0f, mStone); }
            { var w = OnPath(17f, 7.5f); Pillar(w.x, w.y, w.z, 1.3f, 3.2f, mStone); }

            // 木: 段丘の上に大小をばらして散らす (戦闘の場の外・奥ほど多い)。3種・大きさ 0.6〜1.5
            var trees = new[] { Px.Tree(p, rng, 0), Px.Tree(p, rng, 1), Px.Tree(p, rng, 2) };
            var bush = Px.Bush(p, rng); var rock = Px.Rock(p, rng);
            int placed = 0, tries = 0;
            while (placed < 64 && tries++ < 1400)
            {
                float x = -40f + (float)rng.NextDouble() * 80f, z = -6f + (float)rng.NextDouble() * 54f;
                float tt, ss; PathLocal(x, z, out tt, out ss);
                if (ss > -2f && ss < 6.5f && tt > -12f && tt < 16f) continue;      // 戦闘の場と背後は空ける
                if (ss > -2f && ss < 9.5f && tt > -9.5f && tt < 0f) continue;       // リーダーの頭上も空ける
                float h = GroundY(x, z);
                if (h < Step * 0.5f && ss < 8f) continue;                         // 近くの平地には置かない
                float sc = 2.6f * (0.6f + (float)rng.NextDouble() * 0.9f);
                Cross("tree", trees[rng.Next(3)], new Vector3(x, h, z), sc, 0.5f);
                placed++;
            }
            // 両脇の額縁の木 (近いので大きい)
            { var w = OnPath(-15.5f, 5.4f); Cross("tree", trees[1], w, 3.6f, 0.5f); }
            { var w = OnPath(-12f, 9.6f); Cross("tree", trees[0], w, 3.0f, 0.5f); }
            { var w = OnPath(15.5f, -4.4f); Cross("tree", trees[2], w, 3.2f, 0.5f); }
            // 前景 (手前の低い地面・画面の下の隅): 大きな岩と丈の高い草
            var tall = Px.TallGrass(p, rng);
            Plane("rock-front", rock, OnPath(-3.2f, -7.4f), 2.0f, 0.5f, true);
            Plane("rock-front", rock, OnPath(11.5f, -6.8f), 1.5f, 0.5f, true);
            for (int i = 0; i < 14; i++)
            {
                float t = -18f + (float)rng.NextDouble() * 36f;
                float sv = -5.0f - (float)rng.NextDouble() * 1.6f;
                var g = Plane("tallgrass", tall, OnPath(t, sv), 1.5f + (float)rng.NextDouble() * 0.6f, 0.5f, false);
                if (rng.NextDouble() < 0.5) g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1f);
            }
            // 茂み・岩: 戦闘ラインの外 (段丘の縁や手前)
            float[] bt = { -15f, -12f, 1f, 15f, 17f, 0.5f, 6f, -10f, 9f, -18f };
            float[] bs = { 5.6f, 7.4f, 9.6f, 5.2f, 7.8f, -6.4f, -7.2f, -6.0f, 8.8f, 7.2f };
            for (int i = 0; i < bt.Length; i++)
            {
                var b = Plane("bush", bush, OnPath(bt[i], bs[i]), 0.9f + (float)rng.NextDouble() * 0.6f, 0.5f, true);
                if (rng.NextDouble() < 0.5) b.transform.localScale = new Vector3(-b.transform.localScale.x, b.transform.localScale.y, 1f);
            }
            Plane("rock", rock, OnPath(-14.5f, 4.2f), 0.9f, 0.5f, true);
            Plane("rock", rock, OnPath(16.5f, 4.0f), 0.8f, 0.5f, true);
            for (int i = 0; i < 6; i++)
            {
                float x = -34f + (float)rng.NextDouble() * 68f, z = 4f + (float)rng.NextDouble() * 40f;
                float tt, ss; PathLocal(x, z, out tt, out ss);
                if (ss > -3f && ss < 7f && tt > -12f && tt < 16f) continue;
                Plane("rock", rock, new Vector3(x, GroundY(x, z), z), 0.6f + (float)rng.NextDouble() * 0.6f, 0.5f, true);
            }

            // 光の粒の足元: 群れの下に暖色の光溜まり (粒そのものは StageFx の Motes)。装置 (街灯) は置かない
            var lampBase = new Vector3(_lampPos.x, 0f, _lampPos.z);
            var pool = Glow("mote-pool", Px.Radial(new Color(1f, 0.78f, 0.46f, 0.5f)), lampBase, 1f, 1f);
            pool.GetComponent<MeshFilter>().sharedMesh = _quadCentered;
            pool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pool.transform.position = lampBase + new Vector3(0f, 0.035f, 0f);
            pool.transform.localScale = new Vector3(7.2f, 6.2f, 1f);

            // 空 (遠い板) と月、地平線の木立
            var skyTex = Theme.Art("bg", "act" + act);
            var sky = Prop("sky", skyTex != null ? skyTex.texture : Px.Gradient(p.SkyBot, p.SkyTop), new Vector3(0f, -30f, 90f), 130f, 0f, 260f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Ambient", Color.white);
            sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var moon = Prop("moon", Px.Disc(new Color(2.4f, 2.2f, 1.7f)), new Vector3(13f, 9.5f, 72f), 2.4f, 0.4f);   // 幕1は小さな月。塔の肩の脇 (カメラ y≈5.1・上端 +6° の帯の中)
            moon.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            moon.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_SunAmount", 0f);
            moon.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Ambient", Color.white);
            moon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var mts = Prop("mountains", Px.Mountains(p, rng, 0.55f), new Vector3(4f, 2.6f, 58f), 6.5f, 0.4f, 200f);
            mts.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var mts2 = Prop("mountains2", Px.Mountains(p, rng, 0.35f), new Vector3(-10f, 2.9f, 48f), 4.5f, 0.4f, 150f);
            mts2.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var skyline = Prop("skyline", Px.Skyline(p, rng), new Vector3(0f, 3.0f, 36f), 2.4f, 0.4f, 120f);
            skyline.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // 塔: 道の先 (奥右) の地平に立ち、画面の上へ消えるほど高い黒い塔。世界観「長い夜と月の塔」。霧は半分だけ受けて山より暗く残す
            var tower = Prop("tower", Px.Tower(p, rng), new Vector3(23f, -2f, 50f), 40f, 0.4f);   // 山 (z58) より手前に立てて上へ抜ける
            tower.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0.45f);
            tower.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static bool HasTile(int act, string kind) { return Theme.Art("tiles", "act" + act + "_" + kind) != null; }

        static Texture2D Tex(int act, string kind, Texture2D fallback)
        {
            // Art/tiles/act<N>_<kind>.png があればそれを使う (PixelLab のタイル。ArtImporter が Repeat にする)
            var s = Theme.Art("tiles", "act" + act + "_" + kind);
            return s != null ? s.texture : fallback;
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

            /// <summary>遠景の塔: 上へ細り、らせんの段が右上がりに走り、頂は空に溶ける。窓の灯はごく少数</summary>
            public static Texture2D Tower(Pal p, System.Random rng)
            {
                int w = 96, h = 384;
                var t = New(w, h, false);
                var px = new Color[w * h];
                var body = Mix(p.SkyTop, Color.black, 0.62f);
                var edge = Mix(body, p.SkyBot, 0.28f);
                var win = new Color(1f, 0.78f, 0.45f, 1f);
                for (int y = 0; y < h; y++)
                {
                    float k = y / (float)(h - 1);
                    float half = Mathf.Lerp(30f, 16f, k);
                    float cx = w * 0.5f + Mathf.Sin(k * 9f) * 1.5f;
                    float a = k > 0.8f ? Mathf.InverseLerp(1f, 0.8f, k) : 1f;
                    for (int x = 0; x < w; x++)
                    {
                        float d = x - cx;
                        if (Mathf.Abs(d) > half) continue;
                        var c = d > half - 4f ? edge : body;
                        bool ring = ((y + (int)(d * 0.35f)) % 28) < 2 && k < 0.78f;
                        if (ring) c = Mix(c, p.SkyBot, 0.3f);
                        c.a = a;
                        px[y * w + x] = c;
                    }
                }
                for (int i = 0; i < 9; i++)
                {
                    int y = rng.Next(20, (int)(h * 0.7f));
                    float k = y / (float)(h - 1); float half = Mathf.Lerp(30f, 16f, k);
                    int x = (int)(w * 0.5f + (rng.NextDouble() * 2 - 1) * (half - 6f));
                    px[y * w + x] = win;
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

        static void Fireflies()
        {
            var ps = NewSystem("fireflies", DotTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
            main.startColor = new Color(1.6f, 1.4f, 0.5f, 1f);
            main.maxParticles = 50;
            var em = ps.emission; em.rateOverTime = 3f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 3f, 12f); shape.position = new Vector3(0f, 1.6f, 4f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f); vel.y = new ParticleSystem.MinMaxCurve(-0.06f, 0.12f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.35f; noise.scrollSpeed = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
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
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f); vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
        }

        /// <summary>光の粒: 月から零れた光。道筋に沿って塔の方へゆっくり流れ、足元の一群が暖色の光源 (街灯の後継)</summary>
        static void Motes()
        {
            var dir = Quaternion.Euler(0f, PathYaw, 0f) * Vector3.right;   // 道の向き (+t = 塔の方)
            var ps = NewSystem("motes-path", DotTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = new Color(1.6f, 1.35f, 0.62f, 1f);
            main.maxParticles = 110;
            var em = ps.emission; em.rateOverTime = 12f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(34f, 1.4f, 3.2f);
            shape.rotation = new Vector3(0f, PathYaw, 0f); shape.position = new Vector3(0f, 0.9f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(dir.x * 0.1f, dir.x * 0.3f); vel.z = new ParticleSystem.MinMaxCurve(dir.z * 0.1f, dir.z * 0.3f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.08f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.4f; noise.scrollSpeed = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();

            var c = NewSystem("motes-cluster", DotTex());
            main = c.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startColor = new Color(1.5f, 1.25f, 0.62f, 1f);
            main.maxParticles = 24;
            em = c.emission; em.rateOverTime = 9f;
            shape = c.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1.1f; shape.position = _lampPos;
            noise = c.noise; noise.enabled = true; noise.strength = 0.5f; noise.frequency = 0.6f; noise.scrollSpeed = 0.35f;
            vel = c.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.1f);
            col = c.colorOverLifetime; col.enabled = true; col.color = g;
            c.Play();
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
            vel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.15f); vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.5f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.5f; noise.scrollSpeed = 0.3f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            ps.Play();
        }
    }
}
