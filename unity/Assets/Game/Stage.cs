// Stage.cs — HD-2D の舞台 (2026-09-07 ユーザー「やっぱ3D表現がほしい」→「オクトラ風なら」)。
// オクトラの構造そのもの: 舞台だけ本物の3D (ドット絵テクスチャの箱庭・透視カメラ・被写界深度・ライトと影・霧・粒子)、
// キャラは2Dドットのビルボード。紙の UI は Overlay キャンバスのままで、舞台には掛からない。
//
// 座標: 地面 y=0、キャラの立つ線 z=0、1 unit = 焦点面で画面 100px (1080p)。カメラは Pitch 度の見下ろしで、
// 焦点面 (視線に垂直・距離 _dist) 上では画面のピクセルと world が1:1に対応するので、UI の矩形 (uGUI) をそのまま
// 焦点面に写せば「1ドット=画面4px」を保ったままキャラが箱庭に立つ。名前札・HPバー・吹き出しは UI 側の既存レイアウトのまま。
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
        public const float Fov = 30f;
        public const float Pitch = 12f;                 // 見下ろし角
        const float PlaneUnitsPerScreen = 10.8f;        // 焦点面で画面の高さ = 10.8 units
        const float Tile = 1.28f;                        // 32ドットのタイル1枚 = 1.28 units (焦点面で 4px/ドット)

        static Camera _cam;
        static Volume _volume;
        static ColorAdjustments _color;
        static DepthOfField _dof;
        static Transform _world, _fx, _units;
        static Light _sun, _lantern;
        static StageDriver _driver;
        static int _paintedAct = -1;
        static float _groundLine = -1f;                  // 画面px (下から) のキャラの足元の線
        static float _dist, _k;
        static Vector3 _fwd = Vector3.forward, _up = Vector3.up, _right = Vector3.right, _camBase;
        static Shader _unitShader;
        static Material _dioramaBase;
        static Mesh _quad;
        static readonly Dictionary<string, StageUnit> _bound = new Dictionary<string, StageUnit>();

        public static float K { get { return _k; } }
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
            _cam.farClipPlane = 200f;
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
            _quad = BuildQuad();

            // ポスト処理: 被写界深度 (ティルトシフト)・ブルーム・ビネット・幕ごとの色調整
            try
            {
                var vgo = new GameObject("StageVolume");
                _volume = vgo.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 1f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _dof = profile.Add<DepthOfField>(true);
                _dof.mode.value = DepthOfFieldMode.Bokeh;
                _dof.focalLength.value = 300f;
                _dof.aperture.value = 2.2f;
                _dof.bladeCount.value = 6;
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 0.9f;
                bloom.intensity.value = 0.9f;
                bloom.scatter.value = 0.7f;
                bloom.tint.value = new Color(1f, 0.93f, 0.8f);
                var vig = profile.Add<Vignette>(true);
                vig.intensity.value = 0.28f;
                vig.smoothness.value = 0.6f;
                vig.color.value = new Color(0.03f, 0.02f, 0.06f);
                _color = profile.Add<ColorAdjustments>(true);
                _color.postExposure.value = 0.1f;
                _color.contrast.value = 10f;
                _color.saturation.value = 0f;
                _volume.sharedProfile = profile;
            }
            catch (Exception e) { Debug.LogWarning("[Stage] Volume の作成に失敗: " + e.Message); }

            _world = new GameObject("StageWorld").transform;
            _units = new GameObject("StageUnits").transform;
            _fx = new GameObject("StageFx").transform;
            _driver = _world.gameObject.AddComponent<StageDriver>();

            // ライト: 月光 (影を落とす) + 手前のランタン (暖色の点光源)
            var sgo = new GameObject("Moonlight");
            _sun = sgo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.65f;
            _sun.shadowBias = 0.05f;
            _sun.shadowNormalBias = 0.4f;
            sgo.transform.rotation = Quaternion.Euler(50f, 150f, 0f);   // 斜め後ろの上から = 影がキャラの手前に落ちる
            var lgo = new GameObject("Lantern");
            _lantern = lgo.AddComponent<Light>();
            _lantern.type = LightType.Point;
            _lantern.range = 9f;
            _lantern.shadows = LightShadows.None;
            lgo.transform.position = new Vector3(-8.9f, 2.7f, 1.4f);

            LayoutCamera();
            Fireflies();
            Dust();
            Leaves();
        }

        /// <summary>カメラの位置を決める。焦点面上の地面の線 (world 原点) が画面の _groundLine に来るように置く</summary>
        static void LayoutCamera()
        {
            float H = Screen.height;
            if (H < 1f) H = 1080f;
            _k = PlaneUnitsPerScreen / H;
            _dist = (PlaneUnitsPerScreen * 0.5f) / Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            var rot = Quaternion.Euler(Pitch, 0f, 0f);
            _fwd = rot * Vector3.forward; _up = rot * Vector3.up; _right = Vector3.right;
            float gl = _groundLine >= 0f ? _groundLine : H * 0.435f;
            float dy = (gl - H * 0.5f) * _k;
            Vector3 p0 = -_up * dy;
            _camBase = p0 - _fwd * _dist;
            _cam.transform.position = _camBase;
            _cam.transform.rotation = rot;
            if (_dof != null) _dof.focusDistance.value = _dist;
        }

        /// <summary>画面座標 (px・左下原点) を焦点面上の world 座標へ</summary>
        public static Vector3 ScreenToPlane(float sx, float sy)
        {
            float W = Screen.width, H = Screen.height;
            return _camBase + _fwd * _dist + _right * ((sx - W * 0.5f) * _k) + _up * ((sy - H * 0.5f) * _k);
        }

        // ---------------------------------------------------------------- キャラ (UI の矩形に追従するビルボード)

        /// <summary>UI の矩形 (uGUI の sprite 枠) に追従するビルボードを舞台に立てる。Image は非表示にして色 (生死・点滅) だけ読む</summary>
        public static void BindUnit(string key, RectTransform rect, Image img, Sprite sprite)
        {
            Ensure();
            if (rect == null || sprite == null || sprite.texture == null) return;
            if (img != null) img.enabled = false;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float bottom = corners[0].y;
            if (_groundLine < 0f || Mathf.Abs(bottom - _groundLine) > 0.5f) { _groundLine = bottom; LayoutCamera(); }

            var go = new GameObject("unit-" + key);
            go.transform.SetParent(_units, false);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(_unitShader);
            mat.SetTexture("_BaseMap", sprite.texture);
            mat.mainTexture = sprite.texture;
            var tr = sprite.textureRect;
            float tw = sprite.texture.width, th = sprite.texture.height;
            mat.SetTextureScale("_BaseMap", new Vector2(tr.width / tw, tr.height / th));
            mat.SetTextureOffset("_BaseMap", new Vector2(tr.x / tw, tr.y / th));
            mat.SetFloat("_Cutoff", 0.4f);
            mat.SetFloat("_Fog", 1f);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = false;
            var u = go.AddComponent<StageUnit>();
            u.Rect = rect; u.Img = img; u.Mat = mat; u.Rend = mr;
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
            public RectTransform Rect; public Image Img; public Material Mat; public MeshRenderer Rend; public float FlashT;
            static readonly Vector3[] _c = new Vector3[4];
            public void LateUpdate()
            {
                if (Rect == null) { Destroy(gameObject); return; }
                Rect.GetWorldCorners(_c);
                float sx = Mathf.Round((_c[0].x + _c[3].x) * 0.5f), sy = Mathf.Round(_c[0].y);
                float w = Mathf.Round(_c[3].x - _c[0].x), h = Mathf.Round(_c[1].y - _c[0].y);
                transform.position = ScreenToPlane(sx, sy);
                transform.rotation = CameraRotation;
                transform.localScale = new Vector3(Mathf.Max(0.01f, w * _k), Mathf.Max(0.01f, h * _k), 1f);
                var tint = Img != null ? Img.color : Color.white;
                Mat.SetColor("_BaseColor", tint);
                if (FlashT > 0f) FlashT -= Time.deltaTime;
                Mat.SetFloat("_Flash", Mathf.Clamp01(FlashT / 0.18f) * 0.85f);
                Rend.shadowCastingMode = tint.a > 0.9f ? ShadowCastingMode.On : ShadowCastingMode.Off;
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
                // 霧と月の暈をゆっくり揺らす
                float t = Time.time;
                for (int i = 0; i < _world.childCount; i++)
                {
                    var c = _world.GetChild(i);
                    if (c.name == "moon-halo") c.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 0.7f));
                    else if (c.name.StartsWith("mist")) c.position = c.position + new Vector3(Mathf.Sin(t * 0.3f + c.GetSiblingIndex()) * 0.004f, 0f, 0f);
                }
            }
        }

        // ---------------------------------------------------------------- 箱庭

        struct Pal
        {
            public Color SkyTop, SkyBot, GrassA, GrassB, DirtA, DirtB, StoneA, StoneB, CliffA, CliffB, LeafA, LeafB, Trunk, Ambient, Sun, Lantern, Filter;
        }

        static Pal PalOf(int act)
        {
            var p = new Pal();
            if (act == 2)
            {
                p.SkyTop = UiKit.Hex("#0f1f26"); p.SkyBot = UiKit.Hex("#25454c");
                p.GrassA = UiKit.Hex("#2f5246"); p.GrassB = UiKit.Hex("#264238");
                p.DirtA = UiKit.Hex("#5a5248"); p.DirtB = UiKit.Hex("#49433a");
                p.StoneA = UiKit.Hex("#66717a"); p.StoneB = UiKit.Hex("#4c565c");
                p.CliffA = UiKit.Hex("#3f484c"); p.CliffB = UiKit.Hex("#2e3538");
                p.LeafA = UiKit.Hex("#2f6a58"); p.LeafB = UiKit.Hex("#245247"); p.Trunk = UiKit.Hex("#3c3a38");
                p.Ambient = new Color(0.28f, 0.38f, 0.41f); p.Sun = new Color(0.55f, 0.85f, 0.92f); p.Lantern = new Color(0.55f, 0.95f, 1f);
                p.Filter = new Color(0.92f, 1.02f, 1.02f);
            }
            else if (act == 3)
            {
                p.SkyTop = UiKit.Hex("#25101a"); p.SkyBot = UiKit.Hex("#5c2838");
                p.GrassA = UiKit.Hex("#4b3a3a"); p.GrassB = UiKit.Hex("#3a2c2c");
                p.DirtA = UiKit.Hex("#5c4242"); p.DirtB = UiKit.Hex("#4a3333");
                p.StoneA = UiKit.Hex("#6c5b60"); p.StoneB = UiKit.Hex("#54464a");
                p.CliffA = UiKit.Hex("#4a3a3c"); p.CliffB = UiKit.Hex("#362a2c");
                p.LeafA = UiKit.Hex("#6e2c36"); p.LeafB = UiKit.Hex("#4c1d27"); p.Trunk = UiKit.Hex("#3a2a2a");
                p.Ambient = new Color(0.46f, 0.32f, 0.36f); p.Sun = new Color(1f, 0.72f, 0.62f); p.Lantern = new Color(1f, 0.6f, 0.35f);
                p.Filter = new Color(1.06f, 0.94f, 0.92f);
            }
            else
            {
                p.SkyTop = UiKit.Hex("#1a1e40"); p.SkyBot = UiKit.Hex("#3d4270");
                p.GrassA = UiKit.Hex("#3e5c3a"); p.GrassB = UiKit.Hex("#2f4830");
                p.DirtA = UiKit.Hex("#6b5a3f"); p.DirtB = UiKit.Hex("#584933");
                p.StoneA = UiKit.Hex("#72727e"); p.StoneB = UiKit.Hex("#5a5a68");
                p.CliffA = UiKit.Hex("#4c4740"); p.CliffB = UiKit.Hex("#3a3630");
                p.LeafA = UiKit.Hex("#3a6a3e"); p.LeafB = UiKit.Hex("#2b4f31"); p.Trunk = UiKit.Hex("#4a3a2a");
                p.Ambient = new Color(0.36f, 0.39f, 0.56f); p.Sun = new Color(0.66f, 0.74f, 1f); p.Lantern = new Color(1f, 0.74f, 0.42f);
                p.Filter = new Color(0.97f, 0.98f, 1.05f);
            }
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
            var rng = new System.Random(1000 + act * 17);

            _cam.backgroundColor = p.SkyTop;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = p.Ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.SkyBot;
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 78f;
            _sun.color = p.Sun; _sun.intensity = 1.0f;
            _lantern.color = p.Lantern; _lantern.intensity = 3.2f;
            if (_color != null) _color.colorFilter.value = p.Filter;

            // 地面と道
            var grass = Px.Grass(p, rng); var dirt = Px.Dirt(p, rng); var stone = Px.Stone(p, rng); var cliff = Px.Cliff(p, rng);
            var mGrass = Lit(Tex(act, "grass", grass)); var mDirt = Lit(Tex(act, "dirt", dirt)); var mStone = Lit(Tex(act, "stone", stone)); var mCliff = Lit(Tex(act, "cliff", cliff));
            var ground = new MB();
            ground.Floor(-60f, -30f, 60f, 60f, 0f);
            Solid("ground", ground, mGrass);
            var path = new MB();
            path.Floor(-60f, -1.9f, 60f, 1.9f, 0.012f);
            Solid("path", path, mDirt);

            // 段々の台地 (奥へ上がる) と崖の面
            Terrace(-60f, 7.5f, 60f, 17f, 2.1f, mGrass, mCliff);
            Terrace(-60f, 17f, 60f, 48f, 4.8f, mGrass, mCliff);

            // 遺跡の柱 (石) — 左右で額縁を作る
            Pillar(-10.4f, 0f, 3.4f, 1.2f, 4.4f, mStone, rng);
            Pillar(10.6f, 0f, 4.2f, 1.1f, 3.1f, mStone, rng);
            Pillar(13.5f, 2.1f, 10.5f, 1.4f, 5.2f, mStone, rng);
            Pillar(-14f, 2.1f, 11.5f, 1.3f, 3.6f, mStone, rng);

            // 木・茂み・岩 (板のスプライト)
            var tree = Px.Tree(p, rng); var bush = Px.Bush(p, rng); var rock = Px.Rock(p, rng);
            for (int i = 0; i < 14; i++)
            {
                float x = -22f + (float)rng.NextDouble() * 44f;
                float z = 9f + (float)rng.NextDouble() * 8f;
                float y = z >= 17f ? 4.8f : z >= 7.5f ? 2.1f : 0f;
                float sc = 3.2f + (float)rng.NextDouble() * 1.6f;
                Prop("tree", tree, new Vector3(x, y, z), sc, 0.4f);
            }
            for (int i = 0; i < 10; i++)
            {
                float x = -24f + (float)rng.NextDouble() * 48f;
                float z = 18f + (float)rng.NextDouble() * 14f;
                Prop("tree-far", tree, new Vector3(x, 4.8f, z), 4.5f + (float)rng.NextDouble() * 2f, 0.4f);
            }
            float[] bx = { -13.2f, -5.6f, 3.6f, 12.4f, -9f, 8.4f };
            float[] bz = { 2.8f, 4.6f, 5.2f, 2.6f, 6.0f, 6.4f };
            for (int i = 0; i < bx.Length; i++) Prop("bush", bush, new Vector3(bx[i], 0f, bz[i]), 1.1f + (float)rng.NextDouble() * 0.5f, 0.4f);
            Prop("rock", rock, new Vector3(-3.4f, 0f, 3.6f), 1.0f, 0.4f);
            Prop("rock", rock, new Vector3(7.2f, 0f, 2.9f), 0.8f, 0.4f);
            // 手前 (焦点の外・ぼける) の茂み
            Prop("bush-near", bush, new Vector3(-9.4f, 0f, -3.6f), 1.6f, 0.4f);
            Prop("bush-near", bush, new Vector3(9.8f, 0f, -4.0f), 1.5f, 0.4f);
            Prop("bush-near", bush, new Vector3(-4.3f, 0f, -4.4f), 1.1f, 0.4f);

            // ランタン (リーダーの傍。点光源の出どころ)
            var pole = new MB();
            pole.Box(-9.0f, 0f, 1.2f, 0.16f, 2.4f, 0.16f);
            Solid("lantern-pole", pole, mStone);
            var lamp = Px.Solid(new Color(1.6f, 1.2f, 0.6f));
            var lampGo = Prop("lantern", lamp, new Vector3(-9.0f, 2.35f, 1.15f), 0.34f, 0f);
            lampGo.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            lampGo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // 空 (遠い板) と月、地平線の木立
            var skyTex = Theme.Art("bg", "act" + act);
            var sky = Prop("sky", skyTex != null ? skyTex.texture : Px.Gradient(p.SkyBot, p.SkyTop), new Vector3(0f, -30f, 90f), 130f, 0f, 260f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var moon = Prop("moon", Px.Disc(new Color(1.7f, 1.6f, 1.3f)), new Vector3(10f, 3.6f, 80f), 4.2f, 0.4f);
            moon.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            moon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Glow("moon-halo", Px.Glow(new Color(1f, 0.95f, 0.8f, 0.5f)), new Vector3(10f, -1.6f, 81f), 15f, 15f);
            var skyline = Prop("skyline", Px.Skyline(p, rng), new Vector3(0f, 1.2f, 60f), 6f, 0.4f, 220f);
            skyline.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // 低い霧の帯 (半透明・地面の上をゆっくり流れる)
            for (int i = 0; i < 3; i++)
                Glow("mist" + i, Px.Glow(new Color(0.8f, 0.85f, 0.95f, 0.22f)), new Vector3(-12f + i * 12f, -1.4f, 5f + i * 3f), 4f, 22f);
            PlaceParticles();
        }

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

        static void Terrace(float x0, float z0, float x1, float z1, float h, Material top, Material side)
        {
            var t = new MB(); t.Floor(x0, z0, x1, z1, h); Solid("terrace-top", t, top);
            var s = new MB(); s.WallZ(x0, x1, 0f, h, z0); Solid("terrace-side", s, side);
        }

        static void Pillar(float x, float y, float z, float w, float h, Material mat, System.Random rng)
        {
            var mb = new MB();
            mb.Box(x, y, z, w, h, w);
            mb.Box(x, y + h, z, w * 1.3f, 0.3f, w * 1.3f);      // 笠石
            Solid("pillar", mb, mat);
        }

        /// <summary>板のスプライトを立てる (高さ height units・幅は絵の比率)。ぼかされる前提なので整数倍は要らない</summary>
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
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>半透明の板 (暈・霧)。影も深度も書かない</summary>
        static GameObject Glow(string name, Texture2D tex, Vector3 pos, float height, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(width, height, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            var baseMat = Resources.Load<Material>("Materials/ParticleSprite");
            var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Sprites/Default"));
            m.mainTexture = tex;
            m.renderQueue = 3000;
            mr.sharedMaterial = m;
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

        /// <summary>箱庭のメッシュ (面を足して1つに)。UV は world 座標をタイル幅で割る = 継ぎ目なく敷ける</summary>
        class MB
        {
            readonly List<Vector3> _v = new List<Vector3>();
            readonly List<Vector2> _uv = new List<Vector2>();
            readonly List<Vector3> _n = new List<Vector3>();
            readonly List<int> _t = new List<int>();

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = _v.Count;
                var n = Vector3.Cross(b - a, c - a).normalized;
                _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
                _uv.Add(ua); _uv.Add(ub); _uv.Add(uc); _uv.Add(ud);
                _n.Add(n); _n.Add(n); _n.Add(n); _n.Add(n);
                _t.Add(i); _t.Add(i + 1); _t.Add(i + 2); _t.Add(i); _t.Add(i + 2); _t.Add(i + 3);
            }

            /// <summary>上向きの床 (x0..x1, z0..z1 at y)</summary>
            public void Floor(float x0, float z0, float x1, float z1, float y)
            {
                Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0),
                     new Vector2(x0, z0) / Tile, new Vector2(x0, z1) / Tile, new Vector2(x1, z1) / Tile, new Vector2(x1, z0) / Tile);
            }

            /// <summary>手前 (−z) を向いた壁 (x0..x1, y0..y1 at z)</summary>
            public void WallZ(float x0, float x1, float y0, float y1, float z)
            {
                Quad(new Vector3(x0, y0, z), new Vector3(x0, y1, z), new Vector3(x1, y1, z), new Vector3(x1, y0, z),
                     new Vector2(x0, y0) / Tile, new Vector2(x0, y1) / Tile, new Vector2(x1, y1) / Tile, new Vector2(x1, y0) / Tile);
            }

            /// <summary>箱 (底の中心 x,y,z・幅 w・高さ h・奥行 d)。上面と4側面</summary>
            public void Box(float x, float y, float z, float w, float h, float d)
            {
                float x0 = x - w / 2f, x1 = x + w / 2f, z0 = z - d / 2f, z1 = z + d / 2f, y1 = y + h;
                Floor(x0, z0, x1, z1, y1);
                WallZ(x0, x1, y, y1, z0);
                // 奥の面 (+z 向き)
                Quad(new Vector3(x1, y, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), new Vector3(x0, y, z1),
                     new Vector2(x1, y) / Tile, new Vector2(x1, y1) / Tile, new Vector2(x0, y1) / Tile, new Vector2(x0, y) / Tile);
                // 左の面 (−x 向き)
                Quad(new Vector3(x0, y, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y, z0),
                     new Vector2(z1, y) / Tile, new Vector2(z1, y1) / Tile, new Vector2(z0, y1) / Tile, new Vector2(z0, y) / Tile);
                // 右の面 (+x 向き)
                Quad(new Vector3(x1, y, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y, z1),
                     new Vector2(z0, y) / Tile, new Vector2(z0, y1) / Tile, new Vector2(z1, y1) / Tile, new Vector2(z1, y) / Tile);
            }

            public Mesh Build()
            {
                var m = new Mesh();
                m.SetVertices(_v); m.SetUVs(0, _uv); m.SetNormals(_n); m.SetTriangles(_t, 0);
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

            public static Texture2D Solid(Color c)
            {
                var t = New(4, 4, false);
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = c;
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Grass(Pal p, System.Random rng)
            {
                var t = New(32, 32, true);
                var px = new Color[32 * 32];
                for (int i = 0; i < px.Length; i++) px[i] = rng.NextDouble() < 0.18 ? p.GrassB : p.GrassA;
                for (int k = 0; k < 14; k++)
                {
                    int x = rng.Next(32), y = rng.Next(31);
                    var c = Mix(p.GrassA, Color.white, 0.12f);
                    px[y * 32 + x] = c; px[(y + 1) * 32 + x] = c;
                }
                for (int k = 0; k < 6; k++) px[rng.Next(32) * 32 + rng.Next(32)] = Mix(p.GrassB, Color.black, 0.25f);
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Dirt(Pal p, System.Random rng)
            {
                var t = New(32, 32, true);
                var px = new Color[32 * 32];
                for (int i = 0; i < px.Length; i++) px[i] = rng.NextDouble() < 0.22 ? p.DirtB : p.DirtA;
                for (int k = 0; k < 9; k++)
                {
                    int x = rng.Next(31), y = rng.Next(32);
                    var c = Mix(p.DirtA, Color.white, 0.14f);
                    px[y * 32 + x] = c; px[y * 32 + x + 1] = Mix(p.DirtB, Color.black, 0.2f);
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

            public static Texture2D Tree(Pal p, System.Random rng)
            {
                int w = 32, h = 48;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                for (int y = 0; y < 18; y++) for (int x = 14; x < 18; x++) px[y * w + x] = x == 14 ? Mix(p.Trunk, Color.black, 0.3f) : p.Trunk;
                Disc(px, w, h, 16f, 30f, 12.5f, p.LeafB, rng, 1.6f);
                Disc(px, w, h, 11f, 27f, 8f, p.LeafA, rng, 1.2f);
                Disc(px, w, h, 20f, 33f, 8.5f, p.LeafA, rng, 1.2f);
                Disc(px, w, h, 15f, 38f, 7f, Mix(p.LeafA, Color.white, 0.12f), rng, 1.2f);
                // 輪郭 (選択的な線): 葉の外周を暗く
                var outline = Mix(p.LeafB, Color.black, 0.45f);
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                    {
                        if (px[y * w + x].a <= 0f) continue;
                        bool edge = px[(y - 1) * w + x].a <= 0f || px[(y + 1) * w + x].a <= 0f || px[y * w + x - 1].a <= 0f || px[y * w + x + 1].a <= 0f;
                        if (edge && y > 18) px[y * w + x] = outline;
                    }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Bush(Pal p, System.Random rng)
            {
                int w = 24, h = 16;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                Disc(px, w, h, 8f, 6f, 6.5f, p.LeafB, rng, 1.2f);
                Disc(px, w, h, 16f, 6f, 6.5f, p.LeafB, rng, 1.2f);
                Disc(px, w, h, 12f, 9f, 6f, p.LeafA, rng, 1.2f);
                Disc(px, w, h, 10f, 11f, 3f, Mix(p.LeafA, Color.white, 0.12f), rng, 1f);
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Rock(Pal p, System.Random rng)
            {
                int w = 24, h = 14;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                Disc(px, w, h, 12f, 4f, 10f, p.StoneB, rng, 1.5f);
                Disc(px, w, h, 10f, 6f, 6f, p.StoneA, rng, 1.2f);
                for (int x = 0; x < w; x++) for (int y = 0; y < 2; y++) if (px[y * w + x].a > 0f) px[y * w + x] = Mix(p.StoneB, Color.black, 0.3f);
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
            var baseMat = Resources.Load<Material>("Materials/ParticleSprite");
            var sh = baseMat != null ? baseMat.shader : Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("UI/Default");
            var mat = baseMat != null ? new Material(baseMat) : new Material(sh);
            mat.mainTexture = tex;
            r.material = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = 5;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;
            return ps;
        }

        static void PlaceParticles()
        {
            // 粒子は箱庭の体積に置く (キャラの手前にも奥にも)
        }

        static void Fireflies()
        {
            var ps = NewSystem("fireflies", DotTex());
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startColor = new Color(1f, 0.86f, 0.45f, 1f);
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 4f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 3.5f, 10f); shape.position = new Vector3(0f, 1.8f, 3f);
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
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
            main.startColor = new Color(1f, 0.97f, 0.9f, 0.35f);
            main.maxParticles = 120;
            var em = ps.emission; em.rateOverTime = 8f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(22f, 8f, 12f); shape.position = new Vector3(0f, 3.5f, 2f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f); vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
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
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.56f, 0.68f, 0.42f, 0.9f), new Color(0.72f, 0.55f, 0.3f, 0.9f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.maxParticles = 20;
            var em = ps.emission; em.rateOverTime = 0.7f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(22f, 0.5f, 10f); shape.position = new Vector3(0f, 8f, 3f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.15f); vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.5f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.5f; noise.scrollSpeed = 0.3f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            ps.Play();
        }
    }
}
