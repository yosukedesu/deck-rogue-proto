// StageCamera.cs — Stage のカメラ (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3)。
// P02 (W1) で Stage.cs から移した部分 (中身は1文字も変えていない): 画角と見下ろし・基準深度の画面の高さ・足元の線の割合・
// カメラと後処理の静的な値・準備 (Ensure)・LayoutCamera・ScaleFactor・ScreenToPlane・ProjectFeet・揺れと寄り (Shake・ZoomPunch・Dolly)・StageDriver。
// 口 (Camera・WorldRoot・Profile・Invalidate) は今の値を返すだけ (どこからもまだ呼ばれない)。見本のカメラ (LayoutRotation・DebugCameraInfo) は P10 が書く。
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        /// <summary>舞台を写すカメラ (描画用。揺れと寄りを含む)。Ensure の前は null</summary>
        public static Camera Camera => _cam;

        /// <summary>舞台の世界の根 (地形・小物・箱庭の親)。Ensure の前は null</summary>
        public static Transform WorldRoot => _world;

        /// <summary>舞台の後処理の Volume の設定 (sharedProfile)。Volume を作れなかった時と Ensure の前は null</summary>
        public static VolumeProfile Profile => _volume != null ? _volume.sharedProfile : null;

        /// <summary>次の組み直しで舞台を描き直させる (描いた幕を忘れる = 次の Paint が同じ幕でも組み直す)</summary>
        public static void Invalidate() { _paintedAct = -1; }

        /// <summary>レイアウト用のカメラの回転 (揺れと漂いを含まない)。UI の置き場はこの回転と _camBase で写す。骨組み: P10 が書く</summary>
        public static Quaternion LayoutRotation => Quaternion.identity;

        /// <summary>dumplayout 用のカメラの情報 (JSON にできる値。画角・距離・回転・座席の深さ)。骨組み: P10 が書く (無ければ null)</summary>
        public static object DebugCameraInfo() => null;

        public const float Fov = 36f;                   // 広めの画角 = 手前が大きく奥が小さい (奥行きが読める)
        public const float Pitch = 12f;                         // 見下ろし。上端の視線は水平より 6° 上 = 空の帯に月と坑口の櫓が入る (16° では帯が 2° で遠景が山に隠れた)
        // 基準深度で画面の高さ = 10.8 units。スマホ (2026-09-15) は横幅の余裕ぶんズームして座席の間隔を広げる (高さ 675 のキャンバスでは舞台が縮み、
        // 3〜4体の吹き出し・名前札・HPバーが重なっていた (敵の間隔 128〜165))。19.5:9 (S25) で 1.2倍 = 間隔 +20%、16:9 は等倍 (奥の座席が右端から出る)。
        // 絵は UI の枡なので大きさは変わらない
        static float PlaneUnitsPerScreen
        {
            get
            {
                if (!UiKit.Phone) return 10.8f;
                float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.78f;
                float zoom = Mathf.Clamp(aspect / 1.8f, 1f, 1.2f);
                return 10.8f / zoom;
            }
        }
        // 画面の下から何割に world 原点を置くか。スマホ (2026-09-14) は手札が画面の 43% を占めるので座席を上げる (絵は半分なので上端は余る)
        static float GroundLineRatio { get { return UiKit.Phone ? 0.56f : 0.45f; } }   // スマホ 0.54→0.56 (2026-09-15 案C: 頭上の吹き出しが無くなり、足元の帳面の札 76 に足が掛からない高さへ)

        static Camera _cam;
        static Volume _volume;
        static ColorAdjustments _color;
        static DepthOfField _dof;
        static StageDriver _driver;
        static float _dist, _k;
        static Vector3 _fwd = Vector3.forward, _up = Vector3.up, _right = Vector3.right, _camBase;
        static Vignette _vig;

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
                _dof.active = !Application.isMobilePlatform;   // スマホは被写界深度を切る (まず動くこと優先)
                // ティルトシフト (2026-09-21 HD-2D 裁定「手前もぼかす」): Bokeh で焦点を場の中心 (基準深度 = LayoutCamera の _dist) に置き、手前の草と遠景の両方をぼかす。
                // 焦点距離 90mm・F4.5 → 焦点の前後 ±3.4 units が鮮明 = リーダー (手前) から一番奥の敵までの座席は収まる (座席は t=-5〜11)。旧 Gaussian は遠景だけ
                _dof.mode.value = DepthOfFieldMode.Bokeh;
                _dof.focalLength.value = 90f;
                _dof.aperture.value = 4.5f;
                _dof.bladeCount.value = 5;
                _dof.gaussianMaxRadius.value = 1.2f;
                _dof.highQualitySampling.value = true;
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.value = 0.95f;
                bloom.intensity.value = 1.5f;
                bloom.scatter.value = 0.7f;
                bloom.tint.value = new Color(1f, 0.94f, 0.84f);   // 幻想寄り (2026-09-08「もっと幻想的に」): 少し強く・柔らかく・暖色に寄せすぎない
                var vig = profile.Add<Vignette>(true);
                vig.intensity.value = 0.34f;
                vig.smoothness.value = 0.6f;
                vig.color.value = new Color(0.02f, 0.02f, 0.06f);
                _vig = vig;   // 敵と人形の板が席ごとの減光を打ち消すのに値を読む (F06)
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.value = TonemappingMode.ACES;                       // 白飛びを滑らかに (オクトラの締まり)
                var lgg = profile.Add<LiftGammaGain>(true);
                lgg.lift.value = new Vector4(0.97f, 0.98f, 1.04f, 0f);       // 影はわずかに青く (ACES と重ねて沈めすぎない)
                lgg.gamma.value = new Vector4(1f, 1f, 1.02f, 0f);
                lgg.gain.value = new Vector4(1.04f, 1.02f, 0.98f, 0.03f);     // 光は少し暖かく
                var grain = profile.Add<FilmGrain>(true);
                grain.type.value = FilmGrainLookup.Thin1; grain.intensity.value = 0.12f; grain.response.value = 0.75f;
                var ca = profile.Add<ChromaticAberration>(true);
                ca.intensity.value = 0.03f;                                   // ドット絵の縁を崩さない程度
                _color = profile.Add<ColorAdjustments>(true);
                _color.postExposure.value = 0.12f;    // ACES が中間調を沈めるぶん戻す (2026-09-21 HD-2D: 舞台全体が暗く平坦だったので半段明るく・締まりを足す)
                _color.contrast.value = 20f;
                _color.saturation.value = -2f;
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
            _lantern.range = 7.0f;
            _lantern.shadows = LightShadows.None;
            _lampPos = OnPath(-7.1f, 2.2f) + new Vector3(0f, 1.2f, 0f);   // 光の粒の群れの中心 (街灯は撤去。世界観「あかりは装置でなく露頭から漏れるマナの光」2026-09-10)
            lgo.transform.position = _lampPos;

            LayoutCamera();
            Fireflies();
            Dust();
            Leaves();
            Motes();
            Mist();
            Moondust();
            WaterSparkle();
            VeinMotes(); Drips(); AshFall(); Wisps();   // 幕2/3 の粒 (SetFxForAct で幕ごとに出し分け)
            Embers();                                    // 幕2 の炉の火の粉 (2026-09-21)
        }

        /// <summary>カメラの位置: 基準深度 _dist で 1 unit = 100px、world 原点が画面の下から GroundLineRatio に来る</summary>
        static void LayoutCamera()
        {
            float H = Screen.height;
            if (H < 1f) H = 1080f;
            _k = PlaneUnitsPerScreen / H;
            _dist = (PlaneUnitsPerScreen * 0.5f) / Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad);
            if (_dof != null) _dof.focusDistance.value = _dist + 1.0f;   // 焦点 = 場の中心 (原点より少し奥 = 敵①〜②の辺り)
            var rot = Quaternion.Euler(Pitch, 0f, 0f);
            _fwd = rot * Vector3.forward; _up = rot * Vector3.up; _right = Vector3.right;
            float dy = (H * GroundLineRatio - H * 0.5f) * _k;
            Vector3 p0 = -_up * dy;
            _camBase = p0 - _fwd * _dist;
            _cam.transform.position = _camBase;
            _cam.transform.rotation = rot;
            if (_dof != null)
            {
                // 一番奥の座席 (敵4体時の t=11.2) の深度より少し奥からぼかし始める
                var far = Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(11.2f, 0f, 0.7f);
                float dFar = Vector3.Dot(far - _camBase, _fwd);
                _dof.gaussianStart.value = dFar + 4f;
                _dof.gaussianEnd.value = dFar + 26f;
            }
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

        /// <summary>画面揺れは舞台 (カメラ) が揺れる。紙の UI は揺れない</summary>
        public static void Shake(float px, float dur)
        {
            if (_driver == null) return;
            _driver.ShakeAmp = Mathf.Max(_driver.ShakeAmp, px);
            _driver.ShakeT = Mathf.Max(_driver.ShakeT, dur);
            _driver.ShakeDur = Mathf.Max(0.05f, dur);
        }

        /// <summary>
        /// ズームパンチ (2026-09-17 ⑫): カメラが units だけ前へ出て戻る (舞台と絵だけ寄る。紙の UI は動かない)。
        /// 基準の距離 ≈16.6 units なので 0.5 で約 3%。大技の着弾・とどめに
        /// </summary>
        public static void ZoomPunch(float units, float dur = 0.32f)
        {
            if (_driver == null) return;
            _driver.PushAmp = Mathf.Max(_driver.PushAmp, units);
            _driver.PushT = Mathf.Max(0.05f, dur); _driver.PushDur = _driver.PushT;
        }

        /// <summary>ゆっくり寄って戻る (2026-09-17 ⑨⑧): ボスの登場・幕ボス撃破の余韻。inDur で units まで寄り、hold の後 outDur で戻る</summary>
        public static void Dolly(float units, float inDur, float hold, float outDur)
        {
            if (_driver == null) return;
            _driver.DollyAmp = units; _driver.DollyIn = inDur; _driver.DollyHold = hold; _driver.DollyOut = outDur; _driver.DollyT = 0f; _driver.DollyOn = true;
        }

        class StageDriver : MonoBehaviour
        {
            public float ShakeAmp, ShakeT, ShakeDur = 0.3f;
            public float PushAmp, PushT, PushDur = 0.3f;                       // ズームパンチ (前へ出て戻る)
            public float DollyAmp, DollyIn, DollyHold, DollyOut, DollyT; public bool DollyOn;   // ゆっくり寄って戻る
            int _lastW, _lastH, _settle;
            int _edgeSig; bool _edgeSeen;
            void LateUpdate()
            {
                if (_cam == null) return;
                // 画面の切り欠き・safeArea が変わったら (折りたたみ端末・マルチウィンドウ)、2フレーム後に組み直す (左端の部品が UiKit.CutoutLeft で穴を避ける。2026-09-29 p10)。
                // Screen.cutouts は配列を作るので 30 フレームに1回だけ見る (向きは固定なので実質は起動時の1回)
                if (!_edgeSeen || Time.frameCount % 30 == 0)
                {
                    int sig = UiKit.CutoutSignature();
                    if (_edgeSeen && sig != _edgeSig) _settle = Math.Max(_settle, 3);
                    _edgeSig = sig; _edgeSeen = true;
                }
                // ウィンドウの大きさが変わったら、カメラの係数 (_k・_camBase = 描いた時の画面高さで固定していた) を即座に引き直し、
                // UI の枡 (ProjectFeet の座席→UI 座標) は大きさが2フレーム落ち着いてから作り直す (CanvasScaler の scaleFactor は次のフレームで更新されるので、
                // 同じフレームで Rebuild すると古い倍率で枡が置かれた)。放置すると UI 座標→舞台の面の写像が旧高さの比率でずれ、
                // キャラが地面から浮いたり埋まったりした (2026-09-12 ユーザー報告「ウィンドウの大きさを変更するとキャラが浮いたり埋まったり」)
                int sw = Screen.width, sh = Screen.height;
                if (sw > 0 && sh > 0 && (sw != _lastW || sh != _lastH))
                {
                    bool first = _lastW == 0;
                    _lastW = sw; _lastH = sh;
                    if (!first) { LayoutCamera(); _settle = 2; }
                }
                else if (_settle > 0 && --_settle == 0)
                {
                    LayoutCamera();
                    if (GameRoot.I != null) GameRoot.I.Rebuild();
                }
                if (_waterMat != null) _waterMat.mainTextureOffset = new Vector2(Time.time * 0.02f, Time.time * 0.045f);   // 小川の流れ
                UpdateActLights();   // 幕3 の主結晶の呼吸・核の脈動 (2026-09-21)
                Vector3 off = Vector3.zero;
                if (ShakeT > 0f)
                {
                    ShakeT -= Time.deltaTime;
                    float a = ShakeAmp * Mathf.Clamp01(ShakeT / ShakeDur) * _k;
                    off = _right * (UnityEngine.Random.Range(-a, a)) + _up * (UnityEngine.Random.Range(-a, a));
                    if (ShakeT <= 0f) ShakeAmp = 0f;
                }
                // 寄り (ズームパンチ・ドリー): 前へ出る = _fwd 方向。板は _camBase 基準の座席に立つので、寄るとそのぶん大きく見える
                float push = 0f;
                if (PushT > 0f)
                {
                    PushT -= Time.deltaTime;
                    float u = 1f - Mathf.Clamp01(PushT / PushDur);   // 0→1
                    float env = u < 0.25f ? u / 0.25f : 1f - (u - 0.25f) / 0.75f;   // 速く寄って、ゆっくり戻る
                    push += PushAmp * env;
                    if (PushT <= 0f) PushAmp = 0f;
                }
                if (DollyOn)
                {
                    DollyT += Time.deltaTime;
                    float total = DollyIn + DollyHold + DollyOut;
                    float e;
                    if (DollyT < DollyIn) { float u = DollyT / Mathf.Max(0.01f, DollyIn); e = 1f - (1f - u) * (1f - u); }
                    else if (DollyT < DollyIn + DollyHold) e = 1f;
                    else { float u = (DollyT - DollyIn - DollyHold) / Mathf.Max(0.01f, DollyOut); e = 1f - u * u * (3f - 2f * u); }
                    if (DollyT >= total) { DollyOn = false; e = 0f; }
                    push += DollyAmp * Mathf.Clamp01(e);
                }
                _cam.transform.position = _camBase + off + _fwd * push;
                float t = Time.time;
                for (int i = 0; i < _world.childCount; i++)
                {
                    var c = _world.GetChild(i);
                    if (c.name == "rig-wheel") c.localRotation = Quaternion.Euler(0f, 0f, t * 18f);   // 回り続ける採掘の櫓
                    else if (c.name == "lantern-flame") c.localScale = new Vector3(0.36f, 0.36f, 1f) * (1f + 0.08f * Mathf.Sin(t * 9f) + 0.05f * Mathf.Sin(t * 23f));
                }
                if (_lantern != null) _lantern.intensity = _pal.LampIntensity * (1f + 0.06f * Mathf.Sin(t * 9f) + 0.04f * Mathf.Sin(t * 23f));
            }
        }
    }
}
