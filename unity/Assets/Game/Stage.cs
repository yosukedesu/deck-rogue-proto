// Stage.cs — HD-2D の舞台 (2026-09-07)。オクトラの構造そのもの: 舞台だけ本物の3D (ドット絵テクスチャの箱庭・
// 見下ろしの透視カメラ・被写界深度・ライトと接地影・霧=空気遠近・粒子)、キャラは2Dドットのビルボード。紙の UI は Overlay のまま。
//
// 2026-09-07 フィードバック5点への作り直し: ①粒の統一 (仮の敵も 64/80/96 ドットの4倍) ②見下ろし20°で敵を奥行きにずらした斜めの列
// (配置は舞台が決め、UI の札がそこへ追従) ③接地影・ランタンと月のブルーム・夜の色補正・キャラの環境光 ④段階的なボケ＋空気遠近
// ⑤草の房と土のムラを描いたタイル＋散らばる草株と小石。
//
// 座標: 地面 y=0、キャラの立つ線 z≈0、1 unit = 基準深度で画面 100px (1080p)。キャラの板は「その座席の深度の面 (視線に垂直)」に
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
        public const float Fov = 30f;
        public const float Pitch = 28f;                 // 見下ろし角 (俯瞰。上端も地面 = 舞台は地面と台地と木立で埋まる)
        const float PathYaw = -22f;                     // 道の向き (手前左 → 奥右)。隊列もこの線に沿う
        const float PlaneUnitsPerScreen = 10.8f;        // 基準深度で画面の高さ = 10.8 units
        const float GroundLineRatio = 0.45f;            // 画面の下から何割にキャラの立つ線 (world 原点) を置くか
        const float Tile = 1.28f;                        // 32ドットのタイル1枚 = 1.28 units (基準深度で 4px/ドット)

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
        static Mesh _quad, _cross;
        static Texture2D _blobTex;
        static Pal _pal;
        static Vector3 _lampPos = new Vector3(-9f, 2.3f, 1.2f);
        static readonly Dictionary<string, StageUnit> _bound = new Dictionary<string, StageUnit>();
        static readonly Dictionary<string, float> _depths = new Dictionary<string, float>();

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
            _cross = BuildCross();

            // ポスト処理: 被写界深度 (近景シャープ→遠景ほど段階的に)・ブルーム (月・ランタン)・ビネット・夜の色補正
            try
            {
                var vgo = new GameObject("StageVolume");
                _volume = vgo.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 1f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _dof = profile.Add<DepthOfField>(true);
                _dof.mode.value = DepthOfFieldMode.Gaussian;
                _dof.gaussianMaxRadius.value = 1.1f;
                _dof.highQualitySampling.value = true;
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 0.85f;
                bloom.intensity.value = 1.4f;
                bloom.scatter.value = 0.68f;
                bloom.tint.value = new Color(1f, 0.93f, 0.8f);
                var vig = profile.Add<Vignette>(true);
                vig.intensity.value = 0.32f;
                vig.smoothness.value = 0.6f;
                vig.color.value = new Color(0.02f, 0.02f, 0.06f);
                _color = profile.Add<ColorAdjustments>(true);
                _color.postExposure.value = -0.1f;
                _color.contrast.value = 14f;
                _color.saturation.value = -8f;
                _volume.sharedProfile = profile;
            }
            catch (Exception e) { Debug.LogWarning("[Stage] Volume の作成に失敗: " + e.Message); }

            _world = new GameObject("StageWorld").transform;
            _units = new GameObject("StageUnits").transform;
            _fx = new GameObject("StageFx").transform;
            _driver = _world.gameObject.AddComponent<StageDriver>();

            // ライト: 月光 (斜め前の上から。木や柱が影を落とす) + ランタン (暖色の点光源)
            var sgo = new GameObject("Moonlight");
            _sun = sgo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.6f;
            _sun.shadowBias = 0.05f;
            _sun.shadowNormalBias = 0.5f;
            sgo.transform.rotation = Quaternion.Euler(55f, -32f, 0f);
            var lgo = new GameObject("Lantern");
            _lantern = lgo.AddComponent<Light>();
            _lantern.type = LightType.Point;
            _lantern.range = 11f;
            _lantern.shadows = LightShadows.None;
            lgo.transform.position = _lampPos + new Vector3(0.2f, 0.3f, 0f);

            LayoutCamera();
            Fireflies();
            Dust();
            Leaves();
        }

        /// <summary>カメラの位置: 基準深度 _dist で 1 unit = 100px、world 原点 (キャラの立つ線) が画面の下から GroundLineRatio に来る</summary>
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
            if (_dof != null) { _dof.gaussianStart.value = _dist + 8f; _dof.gaussianEnd.value = _dist + 48f; }
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

        /// <summary>道の上の点 (t = 道に沿った距離・s = 道と直角の横ずれ) を world へ</summary>
        static Vector3 OnPath(float t, float s)
        {
            return Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(t, 0f, s);
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

        static readonly Dictionary<string, float> _feetOffsets = new Dictionary<string, float>();
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
            mr.shadowCastingMode = ShadowCastingMode.Off;   // 影は接地影 (楕円) で
            mr.receiveShadows = false;
            // 接地影 (地面に寝かせた半透明の楕円)
            var sh = new GameObject("shadow-" + key);
            sh.transform.SetParent(_units, false);
            sh.AddComponent<MeshFilter>().sharedMesh = _quad;
            var smr = sh.AddComponent<MeshRenderer>();
            smr.sharedMaterial = GlowMaterial(BlobTex());
            smr.sharedMaterial.color = new Color(0.05f, 0.05f, 0.14f, 0.5f);
            smr.shadowCastingMode = ShadowCastingMode.Off; smr.receiveShadows = false;
            var u = go.AddComponent<StageUnit>();
            u.Rect = rect; u.Img = img; u.Mat = mat; u.Rend = mr; u.Depth = depth; u.Shadow = sh.transform;
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
            static readonly Vector3[] _c = new Vector3[4];
            void OnDestroy() { if (Shadow != null) Destroy(Shadow.gameObject); }
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
                Mat.SetColor("_Ambient", _pal.LampOnUnits > 0f ? _pal.UnitAmbient : Color.white);
                Mat.SetVector("_LampPos", _lampPos);
                Mat.SetColor("_LampColor", _pal.Lantern * _pal.LampOnUnits);
                Mat.SetFloat("_LampFalloff", 10f);
                if (FlashT > 0f) FlashT -= Time.deltaTime;
                Mat.SetFloat("_Flash", Mathf.Clamp01(FlashT / 0.18f) * 0.85f);
                if (Shadow != null)
                {
                    // 足元の楕円: 板の下端を地面 (y=0) に落とし、板の幅に合わせる
                    float ww = w * k;
                    Shadow.position = new Vector3(pos.x, 0.03f, pos.z - ww * 0.11f);
                    Shadow.rotation = Quaternion.Euler(90f, 0f, 0f);
                    Shadow.localScale = new Vector3(ww * 0.62f, ww * 0.34f, 1f);
                    var sr = Shadow.GetComponent<MeshRenderer>();
                    if (sr != null) sr.sharedMaterial.color = new Color(0.05f, 0.05f, 0.14f, 0.5f * tint.a);
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
                    if (c.name == "moon-halo") c.localScale = new Vector3(8f, 8f, 1f) * (1f + 0.03f * Mathf.Sin(t * 0.7f));
                    else if (c.name.StartsWith("mist")) c.position = c.position + new Vector3(Mathf.Sin(t * 0.3f + c.GetSiblingIndex()) * 0.004f, 0f, 0f);
                    else if (c.name == "lantern-flame") c.localScale = new Vector3(0.3f, 0.3f, 1f) * (1f + 0.08f * Mathf.Sin(t * 9f) + 0.05f * Mathf.Sin(t * 23f));
                }
                if (_lantern != null) _lantern.intensity = _pal.LampIntensity * (1f + 0.06f * Mathf.Sin(t * 9f) + 0.04f * Mathf.Sin(t * 23f));
            }
        }

        // ---------------------------------------------------------------- 箱庭

        struct Pal
        {
            public Color SkyTop, SkyBot, Fog, GrassA, GrassB, GrassC, DirtA, DirtB, StoneA, StoneB, CliffA, CliffB, LeafA, LeafB, LeafC, Trunk, Ambient, Sun, Lantern, Filter, UnitAmbient;
            public float LampOnUnits, LampIntensity, SunIntensity;
        }

        static Pal PalOf(int act)
        {
            var p = new Pal();
            if (act == 2)
            {
                p.SkyTop = UiKit.Hex("#0d1a22"); p.SkyBot = UiKit.Hex("#2a4a52"); p.Fog = UiKit.Hex("#6f8f99");
                p.GrassA = UiKit.Hex("#2c4c42"); p.GrassB = UiKit.Hex("#233c34"); p.GrassC = UiKit.Hex("#3d6152");
                p.DirtA = UiKit.Hex("#55504a"); p.DirtB = UiKit.Hex("#43403a");
                p.StoneA = UiKit.Hex("#66717a"); p.StoneB = UiKit.Hex("#4c565c");
                p.CliffA = UiKit.Hex("#3f484c"); p.CliffB = UiKit.Hex("#2e3538");
                p.LeafA = UiKit.Hex("#2f6a58"); p.LeafB = UiKit.Hex("#245247"); p.LeafC = UiKit.Hex("#4a8a72"); p.Trunk = UiKit.Hex("#3c3a38");
                p.Ambient = new Color(0.30f, 0.40f, 0.44f); p.Sun = new Color(0.55f, 0.85f, 0.92f); p.Lantern = new Color(0.55f, 0.95f, 1f);
                p.Filter = new Color(0.86f, 1.0f, 1.04f); p.UnitAmbient = new Color(0.72f, 0.86f, 0.9f);
            }
            else if (act == 3)
            {
                p.SkyTop = UiKit.Hex("#200c16"); p.SkyBot = UiKit.Hex("#5c2838"); p.Fog = UiKit.Hex("#8a5a66");
                p.GrassA = UiKit.Hex("#4b3a3a"); p.GrassB = UiKit.Hex("#3a2c2c"); p.GrassC = UiKit.Hex("#5e4a48");
                p.DirtA = UiKit.Hex("#5c4242"); p.DirtB = UiKit.Hex("#4a3333");
                p.StoneA = UiKit.Hex("#6c5b60"); p.StoneB = UiKit.Hex("#54464a");
                p.CliffA = UiKit.Hex("#4a3a3c"); p.CliffB = UiKit.Hex("#362a2c");
                p.LeafA = UiKit.Hex("#6e2c36"); p.LeafB = UiKit.Hex("#4c1d27"); p.LeafC = UiKit.Hex("#8c4048"); p.Trunk = UiKit.Hex("#3a2a2a");
                p.Ambient = new Color(0.46f, 0.32f, 0.36f); p.Sun = new Color(1f, 0.72f, 0.62f); p.Lantern = new Color(1f, 0.6f, 0.35f);
                p.Filter = new Color(1.06f, 0.92f, 0.9f); p.UnitAmbient = new Color(0.9f, 0.78f, 0.78f);
            }
            else
            {
                p.SkyTop = UiKit.Hex("#141a3c"); p.SkyBot = UiKit.Hex("#3a4478"); p.Fog = UiKit.Hex("#7d8bb8");
                p.GrassA = UiKit.Hex("#3b5836"); p.GrassB = UiKit.Hex("#2d452c"); p.GrassC = UiKit.Hex("#4f7044");
                p.DirtA = UiKit.Hex("#6a5a40"); p.DirtB = UiKit.Hex("#564834");
                p.StoneA = UiKit.Hex("#72727e"); p.StoneB = UiKit.Hex("#5a5a68");
                p.CliffA = UiKit.Hex("#4c4740"); p.CliffB = UiKit.Hex("#3a3630");
                p.LeafA = UiKit.Hex("#3a6a3e"); p.LeafB = UiKit.Hex("#2b4f31"); p.LeafC = UiKit.Hex("#5a8a4c"); p.Trunk = UiKit.Hex("#4a3a2a");
                p.Ambient = new Color(0.34f, 0.38f, 0.58f); p.Sun = new Color(0.62f, 0.7f, 1f); p.Lantern = new Color(1f, 0.74f, 0.42f);
                p.Filter = new Color(0.84f, 0.9f, 1.12f); p.UnitAmbient = new Color(0.74f, 0.8f, 0.98f);
            }
            p.LampOnUnits = 0.75f; p.LampIntensity = 7f; p.SunIntensity = 0.9f;
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
            RenderSettings.fogColor = p.Fog;                  // 空気遠近: 遠くほど白く薄く
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 85f;
            _sun.color = p.Sun; _sun.intensity = p.SunIntensity;
            _lantern.color = p.Lantern; _lantern.intensity = p.LampIntensity;
            if (_color != null) _color.colorFilter.value = p.Filter;

            // 地面と道
            var grass = Px.Grass(p, rng); var dirt = Px.Dirt(p, rng); var stone = Px.Stone(p, rng); var cliff = Px.Cliff(p, rng);
            var mGrass = Lit(Tex(act, "grass", grass)); var mDirt = Lit(Tex(act, "dirt", dirt)); var mStone = Lit(Tex(act, "stone", stone)); var mCliff = Lit(Tex(act, "cliff", cliff));
            var ground = new MB();
            ground.Floor(-60f, -30f, 60f, 60f, 0f);
            Solid("ground", ground, mGrass);
            // 道: 手前左から奥右へ斜めに抜ける (水平の帯にしない)
            var path = new MB();
            path.Floor(-70f, -2.4f, 70f, 2.2f, 0.012f);
            Solid("path", path, mDirt).transform.rotation = Quaternion.Euler(0f, PathYaw, 0f);
            // 草地のムラ: 濃い草の斑 (寝かせた抜き板・控えめ)。土の斑は道の上だけ
            var mPatchDirt = Cutout(Px.Patch(p.DirtB, p.DirtA, rng)); var mPatchDark = Cutout(Px.Patch(Color.Lerp(p.GrassA, p.GrassB, 0.6f), p.GrassA, rng));
            for (int i = 0; i < 16; i++)
            {
                float t = -24f + (float)rng.NextDouble() * 48f;
                float s = -9f + (float)rng.NextDouble() * 18f;
                float sz = 1.4f + (float)rng.NextDouble() * 2.2f;
                bool onPath = Mathf.Abs(s) < 1.8f;
                var w = OnPath(t, s);
                Decal("patch", onPath ? mPatchDirt : mPatchDark, w.x, w.z, sz, sz * (0.55f + (float)rng.NextDouble() * 0.3f), (float)rng.NextDouble() * 360f);
            }
            // 草の株・花は道の外に、小石は道の上に (暖色の灰。青く見えない)
            var tuft = Px.Tuft(p, rng); var pebble = Px.Pebble(p, rng); var flower = Px.Flower(p, rng);
            var mPebble = Cutout(pebble);
            for (int i = 0; i < 60; i++)
            {
                float t = -26f + (float)rng.NextDouble() * 52f;
                float s = -10f + (float)rng.NextDouble() * 22f;
                var w = OnPath(t, s);
                if (Mathf.Abs(s) < 1.9f) { if (rng.NextDouble() < 0.35) Decal("pebble", mPebble, w.x, w.z, 0.22f + (float)rng.NextDouble() * 0.12f, 0.14f, (float)rng.NextDouble() * 360f); continue; }
                if (Mathf.Abs(s) < 2.5f) continue;
                if (rng.NextDouble() < 0.12) Plane("flower", flower, w, 0.3f, 0.5f);
                else Plane("tuft", tuft, w, 0.24f + (float)rng.NextDouble() * 0.14f, 0.5f);
            }

            // 段々の台地 (奥へ上がる) と崖の面。縁は斜め (手前左が近く、奥右へ抜ける)
            const float yaw1 = -14f, yaw2 = -8f;
            Terrace(-70f, 9f, 70f, 24f, 1.4f, mGrass, mCliff, yaw1);
            Terrace(-70f, 24f, 70f, 70f, 3.0f, mGrass, mCliff, yaw2);

            // 遺跡の柱 (石) — 左右で額縁を作る (奥行きをばらす)
            Pillar(-11.6f, 0f, 2.2f, 1.2f, 4.2f, mStone);
            Pillar(12.4f, 0f, 6.0f, 1.1f, 3.0f, mStone);
            var pt = Quaternion.Euler(0f, yaw1, 0f) * new Vector3(15f, 0f, 14f);
            Pillar(pt.x, 1.4f, pt.z, 1.4f, 3.4f, mStone);

            // 木: 3種の形・大きさ 0.7〜1.3・群生と隙間。奥の木を大きくしない (遠近で自然に小さく)
            var trees = new[] { Px.Tree(p, rng, 0), Px.Tree(p, rng, 1), Px.Tree(p, rng, 2) };
            var bush = Px.Bush(p, rng); var rock = Px.Rock(p, rng);
            var rot1 = Quaternion.Euler(0f, yaw1, 0f);
            for (int c = 0; c < 4; c++)
            {
                float cx = -20f + c * 12f + (float)rng.NextDouble() * 6f, cz = 12.5f + (float)rng.NextDouble() * 5f;
                int cnt = 3 + rng.Next(3);
                for (int i = 0; i < cnt; i++)
                {
                    var local = new Vector3(cx + (float)rng.NextDouble() * 5f - 2.5f, 1.4f, cz + (float)rng.NextDouble() * 4f - 2f);
                    Cross("tree", trees[rng.Next(3)], rot1 * local, 3.0f * (0.7f + (float)rng.NextDouble() * 0.6f), 0.5f);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                var local = new Vector3(-26f + (float)rng.NextDouble() * 52f, 1.4f, 15f + (float)rng.NextDouble() * 7f);
                Cross("tree", trees[rng.Next(3)], rot1 * local, 3.0f * (0.7f + (float)rng.NextDouble() * 0.6f), 0.5f);
            }
            // 地面の高さの木 (両脇の額縁・近いので大きく見える)
            Cross("tree", trees[1], new Vector3(-13.5f, 0f, 5.5f), 3.4f, 0.5f);
            Cross("tree", trees[0], new Vector3(-9.8f, 0f, 8.2f), 2.8f, 0.5f);
            Cross("tree", trees[2], new Vector3(13.2f, 0f, 9.5f), 3.1f, 0.5f);
            // 奥の台地の木 (小さめ)
            var rot2 = Quaternion.Euler(0f, yaw2, 0f);
            for (int i = 0; i < 12; i++)
            {
                var local = new Vector3(-30f + (float)rng.NextDouble() * 60f, 3.0f, 26f + (float)rng.NextDouble() * 10f);
                Cross("tree-far", trees[rng.Next(3)], rot2 * local, 3.0f * (0.6f + (float)rng.NextDouble() * 0.5f), 0.5f);
            }
            float[] bx = { -14.6f, -5.4f, 3.8f, 11.0f, -9.0f, 8.2f, -1.6f, 6.4f };
            float[] bz = { 4.0f, 6.6f, 7.4f, 4.4f, 9.2f, 10.0f, 10.4f, 12.6f };
            for (int i = 0; i < bx.Length; i++)
            {
                var b = Plane("bush", bush, new Vector3(bx[i], 0f, bz[i]), 0.9f + (float)rng.NextDouble() * 0.6f, 0.5f);
                if (rng.NextDouble() < 0.5) b.transform.localScale = new Vector3(-b.transform.localScale.x, b.transform.localScale.y, 1f);
            }
            Plane("rock", rock, new Vector3(-3.0f, 0f, 5.6f), 0.9f, 0.5f);
            Plane("rock", rock, new Vector3(8.6f, 0f, 3.0f), 0.7f, 0.5f);

            // ランタン (リーダーの傍。点光源の出どころ。炎はゆらぐ)
            var pole = new MB();
            pole.Box(_lampPos.x, 0f, _lampPos.z, 0.16f, 2.1f, 0.16f);
            pole.Box(_lampPos.x, 2.1f, _lampPos.z, 0.44f, 0.5f, 0.44f);
            Solid("lantern-pole", pole, mStone);
            var flame = Prop("lantern-flame", Px.Disc(new Color(3.4f, 2.4f, 1.1f)), _lampPos, 0.3f, 0.4f);
            flame.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            flame.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Glow("lantern-glow", Px.Glow(new Color(1f, 0.8f, 0.5f, 0.55f)), _lampPos + new Vector3(0f, -1.2f, -0.3f), 2.8f, 2.8f);

            // 空 (遠い板) と月、地平線の木立、低い霧
            var skyTex = Theme.Art("bg", "act" + act);
            var sky = Prop("sky", skyTex != null ? skyTex.texture : Px.Gradient(p.SkyBot, p.SkyTop), new Vector3(0f, -30f, 90f), 130f, 0f, 260f);
            sky.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var moon = Prop("moon", Px.Disc(new Color(2.4f, 2.2f, 1.7f)), new Vector3(7f, 4.4f, 44f), 2.2f, 0.4f);
            moon.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Fog", 0f);
            moon.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Glow("moon-halo", Px.Glow(new Color(1f, 0.95f, 0.8f, 0.55f)), new Vector3(7f, 1.5f, 45f), 8f, 8f);
            var skyline = Prop("skyline", Px.Skyline(p, rng), new Vector3(0f, 3.0f, 32f), 2.6f, 0.4f, 120f);
            skyline.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var skyline2 = Prop("skyline2", Px.Skyline(p, rng), new Vector3(6f, 3.0f, 40f), 3.4f, 0.4f, 160f);
            skyline2.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
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

        /// <summary>光を受け影を落とす抜き板の材質 (木・茂み・草株・地面の斑)</summary>
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

        static Texture2D BlobTex()
        {
            if (_blobTex != null) return _blobTex;
            _blobTex = Px.Glow(new Color(1f, 1f, 1f, 1f));
            return _blobTex;
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

        static void Terrace(float x0, float z0, float x1, float z1, float h, Material top, Material side, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var t = new MB(); t.Floor(x0, z0, x1, z1, h); Solid("terrace-top", t, top).transform.rotation = rot;
            var s = new MB(); s.WallZ(x0, x1, 0f, h, z0); Solid("terrace-side", s, side).transform.rotation = rot;
        }

        static void Pillar(float x, float y, float z, float w, float h, Material mat)
        {
            var mb = new MB();
            mb.Box(x, y, z, w, h, w);
            mb.Box(x, y + h, z, w * 1.3f, 0.3f, w * 1.3f);      // 笠石
            Solid("pillar", mb, mat);
        }

        /// <summary>地面に寝かせた抜き板 (斑・小石)</summary>
        static GameObject Decal(string name, Material mat, float x, float z, float w, float d, float rotDeg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = new Vector3(x, 0.02f, z);
            go.transform.rotation = Quaternion.Euler(90f, rotDeg, 0f);
            go.transform.localScale = new Vector3(w, d, 1f);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            // _quad は下端が原点なので中心へ寄せる
            go.transform.position += go.transform.rotation * new Vector3(0f, -d * 0.5f, 0f);
            return go;
        }

        /// <summary>一枚の立て板 (茂み・草株・花・岩)。光を受け、影を落とす</summary>
        static GameObject Plane(string name, Texture2D tex, Vector3 pos, float height, float cutoff)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            float w = height * tex.width / (float)tex.height;
            go.transform.localScale = new Vector3(w, height, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            var m = Cutout(tex);
            m.SetFloat("_Cutoff", cutoff);
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        /// <summary>十字の板 (木)。光を受け、影を落とす</summary>
        static GameObject Cross(string name, Texture2D tex, Vector3 pos, float height, float cutoff)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.position = pos;
            float w = height * tex.width / (float)tex.height;
            go.transform.localScale = new Vector3(w, height, w);
            go.AddComponent<MeshFilter>().sharedMesh = _cross;
            var mr = go.AddComponent<MeshRenderer>();
            var m = Cutout(tex);
            m.SetFloat("_Cutoff", cutoff);
            mr.sharedMaterial = m;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        /// <summary>光を受けない板 (空・月・炎)。ぼかされる前提なので整数倍は要らない</summary>
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
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>半透明の板 (暈・霧・炎の光)。影も深度も書かない</summary>
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

        /// <summary>十字に交差した2枚の板 (下端が原点)。法線は上向き寄りにして、光が均一に当たるようにする</summary>
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

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
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

            /// <summary>草地: 2色のディザ地に草の房 (V字+ハイライト) と暗い窪み</summary>
            public static Texture2D Grass(Pal p, System.Random rng)
            {
                int n = 32;
                var t = New(n, n, true);
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        bool dither = ((x + y) & 1) == 0;
                        var c = dither ? p.GrassA : Mix(p.GrassA, p.GrassB, 0.5f);
                        if (rng.NextDouble() < 0.06) c = p.GrassB;
                        px[y * n + x] = c;
                    }
                for (int k = 0; k < 9; k++)
                {
                    int x = rng.Next(2, n - 2), y = rng.Next(1, n - 3);
                    var lo = p.GrassB; var hi = p.GrassC;
                    Put(px, n, n, x - 1, y, lo); Put(px, n, n, x + 1, y, lo);
                    Put(px, n, n, x - 1, y + 1, hi); Put(px, n, n, x + 1, y + 1, Mix(hi, p.GrassA, 0.4f));
                    Put(px, n, n, x, y + 1, lo); Put(px, n, n, x, y + 2, hi);
                    Put(px, n, n, x - 2, y + 2, Mix(hi, p.GrassA, 0.5f));
                }
                for (int k = 0; k < 5; k++)
                {
                    int x = rng.Next(n), y = rng.Next(n);
                    var d = Mix(p.GrassB, Color.black, 0.3f);
                    Put(px, n, n, x, y, d); Put(px, n, n, x + 1, y, d);
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
                        float k = (float)rng.NextDouble();
                        var c = k < 0.55 ? p.DirtA : k < 0.85 ? Mix(p.DirtA, p.DirtB, 0.5f) : p.DirtB;
                        px[y * n + x] = c;
                    }
                for (int k = 0; k < 10; k++)
                {
                    int x = rng.Next(1, n - 2), y = rng.Next(1, n - 1);
                    var hi = Mix(p.DirtA, Color.white, 0.22f); var sh = Mix(p.DirtB, Color.black, 0.3f);
                    Put(px, n, n, x, y, hi); Put(px, n, n, x + 1, y, Mix(hi, p.DirtA, 0.5f));
                    Put(px, n, n, x, y - 1, sh); Put(px, n, n, x + 1, y - 1, sh);
                }
                for (int k = 0; k < 3; k++)
                {
                    int x = rng.Next(n), y = rng.Next(n);
                    var d = Mix(p.DirtB, Color.black, 0.35f);
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

            /// <summary>葉の塊に3段の陰影 (下=影・中=地・左上=光) を付け、外周に暗い線</summary>
            static void Foliage(Color[] px, int w, int h, Pal p, System.Random rng, int yMin)
            {
                var outline = Mix(p.LeafB, Color.black, 0.45f);
                for (int y = yMin; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * w + x;
                        if (px[i].a <= 0f) continue;
                        // 上端からの深さ・左右で陰影
                        bool lit = false, shade = false;
                        for (int k = 1; k <= 3 && !lit; k++) if (y + k < h && px[(y + k) * w + x].a <= 0f) lit = true;
                        for (int k = 1; k <= 3 && !shade; k++) if (y - k >= 0 && px[(y - k) * w + x].a <= 0f) shade = true;
                        if (x >= 2 && px[y * w + x - 2].a <= 0f) lit = lit || ((x + y) & 1) == 0;
                        if (lit) px[i] = p.LeafC; else if (shade) px[i] = p.LeafB;
                        if (rng.NextDouble() < 0.05) px[i] = Mix(px[i], p.LeafC, 0.6f);
                    }
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                    {
                        if (px[y * w + x].a <= 0f) continue;
                        bool edge = px[(y - 1) * w + x].a <= 0f || px[(y + 1) * w + x].a <= 0f || px[y * w + x - 1].a <= 0f || px[y * w + x + 1].a <= 0f;
                        if (edge && y >= yMin) px[y * w + x] = outline;
                    }
            }

            /// <summary>木 3種: 0=丸い広葉樹・1=細長い針葉樹・2=横に広い低木</summary>
            public static Texture2D Tree(Pal p, System.Random rng, int kind)
            {
                int w = kind == 1 ? 32 : kind == 2 ? 52 : 40, h = kind == 1 ? 72 : kind == 2 ? 44 : 60;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                var bark = Mix(p.Trunk, Color.black, 0.35f);
                int trunkH = kind == 1 ? 14 : kind == 2 ? 10 : 24;
                int tx0 = w / 2 - 3, tx1 = w / 2 + 3;
                for (int y = 0; y < trunkH; y++)
                    for (int x = tx0; x < tx1; x++)
                    {
                        var c = x == tx0 || x == tx1 - 1 ? bark : (((x + y * 3) % 5 == 0) ? Mix(p.Trunk, Color.black, 0.15f) : p.Trunk);
                        if (x == tx0 + 2 && y % 4 == 1) c = Mix(p.Trunk, Color.white, 0.12f);
                        px[y * w + x] = c;
                    }
                for (int y = 0; y < 4; y++) { Put(px, w, h, tx0 - 2 + (3 - y), y, bark); Put(px, w, h, tx1 + 1 - (3 - y), y, bark); }
                if (kind == 1)
                {
                    // 針葉樹: 三角を3段
                    for (int s = 0; s < 3; s++)
                    {
                        int baseY = 12 + s * 17, topY = baseY + 26;
                        for (int y = baseY; y < Mathf.Min(h, topY); y++)
                        {
                            float k = (y - baseY) / (float)(topY - baseY);
                            int half = Mathf.RoundToInt((1f - k) * (14f - s * 2f)) + 1;
                            for (int x = w / 2 - half; x < w / 2 + half; x++) Put(px, w, h, x, y, p.LeafA);
                        }
                    }
                    Foliage(px, w, h, p, rng, 12);
                }
                else if (kind == 2)
                {
                    Disc(px, w, h, 16f, 24f, 13f, p.LeafA, rng, 1.6f);
                    Disc(px, w, h, 36f, 22f, 13f, p.LeafA, rng, 1.6f);
                    Disc(px, w, h, 26f, 30f, 12f, p.LeafA, rng, 1.4f);
                    Foliage(px, w, h, p, rng, 10);
                }
                else
                {
                    Disc(px, w, h, 20f, 36f, 15.5f, p.LeafA, rng, 1.8f);
                    Disc(px, w, h, 12f, 32f, 10f, p.LeafA, rng, 1.4f);
                    Disc(px, w, h, 28f, 33f, 10.5f, p.LeafA, rng, 1.4f);
                    Disc(px, w, h, 19f, 46f, 10f, p.LeafA, rng, 1.4f);
                    Disc(px, w, h, 25f, 44f, 8f, p.LeafA, rng, 1.2f);
                    Foliage(px, w, h, p, rng, 22);
                }
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Bush(Pal p, System.Random rng)
            {
                int w = 28, h = 18;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                Disc(px, w, h, 9f, 6f, 7.5f, p.LeafA, rng, 1.2f);
                Disc(px, w, h, 19f, 6f, 7.5f, p.LeafA, rng, 1.2f);
                Disc(px, w, h, 14f, 10f, 7f, p.LeafA, rng, 1.2f);
                Foliage(px, w, h, p, rng, 0);
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Rock(Pal p, System.Random rng)
            {
                int w = 24, h = 14;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                var ra = Mix(p.StoneA, p.DirtA, 0.5f); var rb = Mix(p.StoneB, p.DirtB, 0.5f);
                Disc(px, w, h, 12f, 4f, 10f, rb, rng, 1.5f);
                Disc(px, w, h, 10f, 6f, 6f, ra, rng, 1.2f);
                Disc(px, w, h, 8f, 8f, 3f, Mix(ra, Color.white, 0.15f), rng, 1f);
                for (int x = 0; x < w; x++) for (int y = 0; y < 2; y++) if (px[y * w + x].a > 0f) px[y * w + x] = Mix(rb, Color.black, 0.3f);
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>草の株 (十字の板用・小)</summary>
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
                for (int y = 0; y < 6; y++) Put(px, w, h, 4, y, p.LeafA);
                Put(px, w, h, 2, 2, p.LeafC); Put(px, w, h, 3, 3, p.LeafA); Put(px, w, h, 5, 1, p.LeafC);
                var petal = rng.NextDouble() < 0.5 ? new Color(0.95f, 0.9f, 0.7f) : new Color(0.9f, 0.7f, 0.85f);
                for (int y = 6; y < 9; y++) for (int x = 3; x < 6; x++) Put(px, w, h, x, y, petal);
                Put(px, w, h, 4, 7, new Color(1f, 0.85f, 0.35f));
                Put(px, w, h, 2, 7, petal); Put(px, w, h, 6, 7, petal); Put(px, w, h, 4, 9, petal); Put(px, w, h, 4, 5, Mix(petal, p.LeafA, 0.5f));
                t.SetPixels(px); t.Apply();
                return t;
            }

            public static Texture2D Pebble(Pal p, System.Random rng)
            {
                int w = 12, h = 8;
                var t = New(w, h, false);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                var pa = Mix(p.DirtA, p.StoneA, 0.5f); var pb = Mix(p.DirtB, p.StoneB, 0.5f);
                Disc(px, w, h, 6f, 4f, 4.5f, pb, rng, 1.2f);
                Disc(px, w, h, 5f, 5f, 2.5f, pa, rng, 0.8f);
                for (int x = 0; x < w; x++) if (px[x].a > 0f) px[x] = Mix(pb, Color.black, 0.3f);
                t.SetPixels(px); t.Apply();
                return t;
            }

            /// <summary>地面の斑 (不規則な塊。ドット絵らしい硬い縁)</summary>
            public static Texture2D Patch(Color a, Color b, System.Random rng)
            {
                int n = 32;
                var t = New(n, n, false);
                var px = new Color[n * n];
                for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
                for (int k = 0; k < 7; k++)
                    Disc(px, n, n, 8f + (float)rng.NextDouble() * 16f, 8f + (float)rng.NextDouble() * 16f, 4f + (float)rng.NextDouble() * 7f, ((k & 1) == 0) ? a : Mix(a, b, 0.5f), rng, 1.5f);
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) if (px[y * n + x].a > 0f && ((x + y) & 1) == 0 && rng.NextDouble() < 0.35) px[y * n + x] = Mix(px[y * n + x], b, 0.5f);
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
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startColor = new Color(1.4f, 1.1f, 0.5f, 1f);
            main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 4f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 3.5f, 12f); shape.position = new Vector3(0f, 1.8f, 4f);
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
            main.startColor = new Color(1f, 0.97f, 0.9f, 0.3f);
            main.maxParticles = 120;
            var em = ps.emission; em.rateOverTime = 8f;
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
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(22f, 0.5f, 12f); shape.position = new Vector3(0f, 8f, 4f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.15f); vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.5f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.5f; noise.scrollSpeed = 0.3f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            ps.Play();
        }
    }
}
