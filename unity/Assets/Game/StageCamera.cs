// StageCamera.cs — Stage のカメラ (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3)。
// P02 (W1) で Stage.cs から移した部分: 画角と見下ろし・基準深度の画面の高さ・足元の線の割合・
// カメラと後処理の静的な値・準備 (Ensure)・LayoutCamera・ScaleFactor・ScreenToPlane・ProjectFeet・揺れと寄り (Shake・ZoomPunch・Dolly)・StageDriver。
// 口 (Camera・WorldRoot・Profile・Invalidate) は今の値を返す。
// P10 (W2) が書いた部分 (§2-3・§4 P10。旗を立てなければ今の見た目のまま = 画角 36・見下ろし 12・足元の線 PC 0.45/スマホ 0.56・r=1):
//  ・画角と見下ろしは旗 (cam=・pitch=) から。LayoutCamera のたびに fieldOfView・遠端 (220×r) を書く。r = 画角 36° の時の距離に対する今の距離の比
//    (同じ端末の比 = PC では _dist÷16.62。36° ならちょうど 1)。寄り (ZoomPunch・Dolly) の量に r を掛ける = どの画角でも同じ割合だけ寄る。
//    揺れ (Shake) は横に動かすので、焦点の面の px は画角に依らず同じ (r を掛けない)。
//  ・足元の線は旗 groundline= か、見本 (stage=diorama) の既定 PC 0.42・スマホ 0.52 (seatfit の表で帳面と自分の札が足元を隠さない、いちばん低い線)。
//  ・霧と影の距離は StageLook.ScaleByCameraDistance の1か所で書く (ここは _dist を渡すだけ)。座席の帯 (SeatDepthRange) をぼかしの帯へ、
//    影のカスケードの分割を「座席の帯の奥＋余白 ÷ 影の距離」から StageLook へ渡す (StageLook が当たっている時だけ。当たった後の最初のフレームでも渡す)。
//  ・LayoutCamera の最後で Diorama.OnCameraLayout (額縁の置き直し)。StageDriver が毎フレーム Diorama.Tick (滑車を基準の回転 × Z 回りで回す)。
//  ・待機の漂い (旗 drift=1): 描画用のカメラだけを場の中心 (座席の帯の重心) を軸にごく小さく回す。幅は座席 (足元と頭) の画面の動きが
//    合計 1.5px 以内になるよう LayoutCamera で計算する。撮影 (-det・-autopilot・-statesfile) では切る。
//  ・LayoutRotation (揺れと寄りと漂いを含まない回転)・DebugCameraInfo (dumplayout の stage.camera)・額縁の記録 (LayoutDumpers["frames"])。
using System;
using System.Collections.Generic;
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

        /// <summary>レイアウト用のカメラの回転 (揺れと寄りと漂いを含まない)。UI の置き場はこの回転と _camBase で写す。LayoutCamera の前は今の見下ろし</summary>
        public static Quaternion LayoutRotation => _laidOut ? _layoutRot : Quaternion.Euler(CurrentPitch, 0f, 0f);

        public const float Fov = 36f;                   // 広めの画角 = 手前が大きく奥が小さい (奥行きが読める)。今の舞台の画角 (旗 cam= の既定)
        public const float Pitch = 12f;                         // 見下ろし。上端の視線は水平より 6° 上 = 空の帯に月と坑口の櫓が入る (16° では帯が 2° で遠景が山に隠れた)。旗 pitch= の既定
        /// <summary>遠端の元の値 (画角 36° の時)。LayoutCamera が × r で書く</summary>
        const float FarClip = 220f;

        /// <summary>今の画角 (旗 cam=。既定・読めない値は Fov = 36)</summary>
        public static float CurrentFov { get { float f = HD2DFlags.CamFov; return f >= 5f && f <= 120f ? f : Fov; } }
        /// <summary>今の見下ろし (旗 pitch=。既定・読めない値は Pitch = 12)</summary>
        public static float CurrentPitch { get { float p = HD2DFlags.CamPitch; return p > -89f && p < 89f ? p : Pitch; } }
        /// <summary>見本のカメラと座席 (旗 stage=diorama): 足元の線の既定と、敵と人形の座席の表が見本の物になる。画角・見下ろしの旗はどちらの舞台でも効く</summary>
        static bool DioramaCamera => HD2DFlags.StageMode == HD2DStage.Diorama;
        /// <summary>
        /// r = 同じ端末の画角 36° の時の距離に対する今の距離の比 (= tan18° ÷ tan(画角/2)。PC では _dist ÷ 16.62・画角 36° ならちょうど 1)。
        /// 遠端・寄りの量に掛ける。霧と影の距離は StageLook が自分の r (_dist ÷ 16.62) で書く
        /// </summary>
        public static float DistanceRatio { get; private set; } = 1f;

        static Quaternion _layoutRot = Quaternion.Euler(Pitch, 0f, 0f);
        static bool _laidOut, _flagsHooked;
        static float _cascadeSplit = -1f;
        static StageLookData _lookSynced;
        /// <summary>影のカスケードの1段目の奥 = 座席の帯のいちばん奥 + これ (unit)。座席と、そのまわりに落ちる木と崖の影を1段目 (細かい影) に入れる</summary>
        const float CascadeMargin = 2.5f;
        // 待機の漂い (旗 drift=1): 場の中心を軸にごく小さく回す。幅 (度) は LayoutCamera が座席の画面の動きから決める
        static Vector3 _driftPivot;
        static float _driftYawAmp, _driftPitchAmp;
        const float DriftTestDeg = 0.1f;      // 幅を測るための試しの回転
        const float DriftYawPx = 0.9f;        // 横の回転で座席が動いてよい最大 (px)
        const float DriftPitchPx = 0.6f;      // 縦の回転で座席が動いてよい最大 (px)。合わせて 1.5px (計画の上限 2px の内側)
        const float DriftMaxDeg = 0.6f;
        const float DriftYawPeriod = 17f, DriftPitchPeriod = 23f, DriftFade = 1.5f;
        static int _apShots = -1;

        /// <summary>漂いを動かしてよい (旗 drift=1 で、撮影 = -det・-autopilot・-statesfile ではない)</summary>
        static bool DriftAllowed
        {
            get
            {
                if (!HD2DFlags.Drift || Autopilot.Det) return false;
                if (_apShots < 0) _apShots = (Autopilot.Arg("-autopilot") != null || Autopilot.Arg("-statesfile") != null) ? 1 : 0;
                return _apShots == 0;
            }
        }

        /// <summary>
        /// dumplayout 用のカメラの情報 (layout.json の stage.camera)。画角・見下ろし・足元の線・距離・r・焦点の 1unit の px・回転・遠端・
        /// 座席の帯の深さ・奥と手前の深さの比 (敵4体のいちばん奥 ÷ 主人公)・カスケードの分割・漂い・敵1〜4体の座席 (t・キャンバスの足元・間隔の差)・
        /// 人形の t・今の座席 (key ごとの世界の点・深さ・PNG の画素)。カメラがまだ無ければ null
        /// </summary>
        public static object DebugCameraInfo()
        {
            if (_cam == null || !_laidOut) return null;
            var o = new Dictionary<string, object>();
            float W = Screen.width, H = Screen.height;
            o["mode"] = DioramaCamera ? "diorama" : "old";
            o["fov"] = _cam.fieldOfView;
            o["pitch"] = CurrentPitch;
            o["groundLine"] = GroundLineRatio;
            o["planeUnitsPerScreen"] = PlaneUnitsPerScreen;
            o["dist"] = _dist;
            o["k"] = _k;
            o["distanceRatio"] = DistanceRatio;
            o["pxPerUnitAtFocus"] = _k > 0f ? 1f / _k : 0f;
            o["camBase"] = _camBase;
            o["layoutEuler"] = _layoutRot.eulerAngles;
            o["renderPos"] = _cam.transform.position;
            o["renderEuler"] = _cam.transform.rotation.eulerAngles;
            o["farClip"] = _cam.farClipPlane;
            o["screen"] = new float[] { W, H };
            o["canvasScale"] = ScaleFactor();
            float near, far;
            SeatDepthRange(out near, out far);
            o["seatNear"] = near;
            o["seatFar"] = far;
            float dLead = Vector3.Dot(LeaderSlot() - _camBase, _fwd);
            var e4 = EnemySeats(4);
            float dFar4 = Vector3.Dot(e4[3] - _camBase, _fwd);
            o["depthRatio"] = dLead > 0.01f ? dFar4 / dLead : 0f;
            o["cascadeSplit"] = _cascadeSplit;
            var urp = UniversalRenderPipeline.asset;
            o["shadowDistance"] = urp != null ? urp.shadowDistance : 0f;
            o["drift"] = new Dictionary<string, object> { { "flag", HD2DFlags.Drift }, { "allowed", DriftAllowed }, { "yawAmp", _driftYawAmp }, { "pitchAmp", _driftPitchAmp }, { "pivot", _driftPivot } };
            var enemies = new Dictionary<string, object>();
            for (int n = 1; n <= 4; n++)
            {
                var seats = EnemySeats(n);
                var feet = new List<object>();
                var xs = new float[n];
                for (int i = 0; i < n; i++) { var c = FeetCanvas(seats[i]); xs[i] = c.x; feet.Add(new float[] { c.x, c.y }); }
                float spread = 0f;
                if (n >= 2)
                {
                    float gmin = float.MaxValue, gmax = float.MinValue, gsum = 0f;
                    for (int i = 0; i + 1 < n; i++) { float gp = xs[i + 1] - xs[i]; gmin = Mathf.Min(gmin, gp); gmax = Mathf.Max(gmax, gp); gsum += gp; }
                    spread = gsum > 0f ? (gmax - gmin) / (gsum / (n - 1)) : 0f;
                }
                float[] tt = DioramaCamera ? DioramaEnemyT(n) : null;
                enemies[n.ToString()] = new Dictionary<string, object> { { "t", tt }, { "feetCanvas", feet }, { "gapSpread", spread } };
            }
            o["enemies"] = enemies;
            o["dollT"] = DioramaCamera ? DioramaDollT() : null;
            var seatsNow = new List<object>();
            foreach (var kv in _seatWorld)
            {
                float d = Vector3.Dot(kv.Value - _camBase, _fwd);
                var sp = LayoutScreen(kv.Value);
                seatsNow.Add(new Dictionary<string, object> { { "key", kv.Key }, { "world", kv.Value }, { "depth", d }, { "px", new float[] { sp.x, H - sp.y } } });
            }
            o["seats"] = seatsNow;
            return o;
        }

        /// <summary>世界の点をレイアウト用のカメラで画面 (px・左下原点) へ (ProjectFeet と同じ式・深さは記録しない)</summary>
        static Vector2 LayoutScreen(Vector3 world)
        {
            var rel = world - _camBase;
            float d = Mathf.Max(0.01f, Vector3.Dot(rel, _fwd));
            float k = _k * d / _dist;
            return new Vector2(Screen.width * 0.5f + Vector3.Dot(rel, _right) / k, Screen.height * 0.5f + Vector3.Dot(rel, _up) / k);
        }

        /// <summary>世界の点をキャンバスの単位 (左下原点) へ (ProjectFeet と同じ・深さは記録しない。dumplayout 用)</summary>
        static Vector2 FeetCanvas(Vector3 world)
        {
            var s = LayoutScreen(world);
            float sf = ScaleFactor();
            return new Vector2(s.x / sf, s.y / sf);
        }

        /// <summary>
        /// 額縁の画面の矩形 (layout.json の extra.frames。P08 の layout-check の L8 が読む): Diorama.Dynamic の frame-N の形 (メッシュの箱の8隅) を
        /// レイアウト用のカメラで写し、画面の中に切った [x, y, w, h] (PNG の画素・左上原点)。箱庭が無ければ null
        /// </summary>
        static object DumpFrames()
        {
            if (!Diorama.Active || _cam == null || !_laidOut) return null;
            float W = Screen.width, H = Screen.height;
            var list = new List<object>();
            var corner = new Vector3[8];
            foreach (var e in Diorama.Dynamic)
            {
                if (e.Transform == null || e.Name == null || !e.Name.StartsWith("frame", StringComparison.Ordinal)) continue;
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue, dsum = 0f;
                int cnt = 0;
                foreach (var mf in e.Transform.GetComponentsInChildren<MeshFilter>(false))
                {
                    if (mf == null || mf.sharedMesh == null) continue;
                    var b = mf.sharedMesh.bounds;
                    var m = mf.transform.localToWorldMatrix;
                    for (int c = 0; c < 8; c++)
                        corner[c] = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f)));
                    foreach (var w in corner)
                    {
                        var s = LayoutScreen(w);
                        x0 = Mathf.Min(x0, s.x); x1 = Mathf.Max(x1, s.x); y0 = Mathf.Min(y0, s.y); y1 = Mathf.Max(y1, s.y);
                        dsum += Vector3.Dot(w - _camBase, _fwd); cnt++;
                    }
                }
                if (cnt == 0) continue;
                x0 = Mathf.Max(0f, x0); y0 = Mathf.Max(0f, y0); x1 = Mathf.Min(W, x1); y1 = Mathf.Min(H, y1);
                if (x1 <= x0 || y1 <= y0) continue;
                list.Add(new Dictionary<string, object> { { "name", e.Name }, { "px", new float[] { x0, H - y1, x1 - x0, y1 - y0 } }, { "depth", dsum / cnt } });
            }
            return list;
        }
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
        // 旗 groundline= (0〜1) があればそれ。見本 (stage=diorama) の既定は PC 0.42・スマホ 0.52 (2026-09-30 P10: docs/design/hd2d-slice/seatfit.md の
        // 「今の UI のまま・手札を沈めずに帳面と自分の札が足元を 16px 以上隠さない、いちばん低い線」。計画の目安 PC 0.40 は自分の札が主人公の足元を 34〜38px 隠す)
        static float GroundLineRatio
        {
            get
            {
                float g = HD2DFlags.GroundLine;
                if (g >= 0f && g <= 1f) return g;
                if (DioramaCamera) return UiKit.Phone ? 0.52f : 0.42f;
                return UiKit.Phone ? 0.56f : 0.45f;   // スマホ 0.54→0.56 (2026-09-15 案C: 頭上の吹き出しが無くなり、足元の帳面の札 76 に足が掛からない高さへ)
            }
        }

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
            // 旗が変わったら (-statesfile の行ごと・StateJump の旗) カメラを置き直す (画角・見下ろし・足元の線・座席の表)。UI の組み直しは呼んだ側 (Rebuild) が行う
            if (!_flagsHooked) { _flagsHooked = true; HD2DFlags.Changed += OnHD2DFlagsChanged; }
            HD2DFlags.LayoutDumpers["frames"] = DumpFrames;   // 額縁の画面の矩形 (見本の時だけ中身がある)

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

        /// <summary>
        /// カメラの位置: 基準深度 _dist で 1 unit = 100px (1080p の PC)、world 原点が画面の下から GroundLineRatio に来る。
        /// 画角と見下ろしは旗 (cam=・pitch=)。毎回 fieldOfView と遠端 (220×r) を書き、最後に座席の帯・光・額縁・漂いを合わせる (AfterLayout)
        /// </summary>
        static void LayoutCamera()
        {
            float H = Screen.height;
            if (H < 1f) H = 1080f;
            float fov = CurrentFov, pitch = CurrentPitch;
            _k = PlaneUnitsPerScreen / H;
            _dist = (PlaneUnitsPerScreen * 0.5f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            DistanceRatio = fov == Fov ? 1f : Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            if (_dof != null) _dof.focusDistance.value = _dist + 1.0f;   // 焦点 = 場の中心 (原点より少し奥 = 敵①〜②の辺り)
            var rot = Quaternion.Euler(pitch, 0f, 0f);
            _layoutRot = rot;
            _fwd = rot * Vector3.forward; _up = rot * Vector3.up; _right = Vector3.right;
            float dy = (H * GroundLineRatio - H * 0.5f) * _k;
            Vector3 p0 = -_up * dy;
            _camBase = p0 - _fwd * _dist;
            _cam.transform.position = _camBase;
            _cam.transform.rotation = rot;
            _cam.fieldOfView = fov;
            _cam.farClipPlane = FarClip * DistanceRatio;
            _laidOut = true;
            if (_dof != null)
            {
                // 一番奥の座席 (敵4体時の t=11.2) の深度より少し奥からぼかし始める
                var far = Quaternion.Euler(0f, PathYaw, 0f) * new Vector3(11.2f, 0f, 0.7f);
                float dFar = Vector3.Dot(far - _camBase, _fwd);
                _dof.gaussianStart.value = dFar + 4f;
                _dof.gaussianEnd.value = dFar + 26f;
            }
            AfterLayout(fov);
        }

        /// <summary>LayoutCamera の後始末: 漂いの幅・光 (霧と影の距離・ぼかしの帯・カスケード)・額縁。今の舞台で旗が無ければ何も変えない</summary>
        static void AfterLayout(float fov)
        {
            ComputeDrift();
            PushLook();
            Diorama.OnCameraLayout(_camBase, _layoutRot, fov);   // 額縁をレイアウト用のカメラに合わせて置き直す (箱庭が無ければ値を覚えるだけ)
        }

        static void OnHD2DFlagsChanged()
        {
            if (_cam == null || _world == null) return;
            LayoutCamera();
        }

        /// <summary>
        /// 光 (StageLook) へカメラの距離と座席の帯を渡す (当たっている時だけ): 霧と影の距離 (× r は StageLook の1か所)・ぼかしの帯・
        /// 影のカスケードの分割 = (座席の帯のいちばん奥 + 余白) ÷ 影の距離 (座席が1段目に入る)
        /// </summary>
        static void PushLook()
        {
            if (!StageLook.Active) { _lookSynced = null; return; }
            _lookSynced = StageLook.Current;
            StageLook.ScaleByCameraDistance(_dist);
            float near, far;
            SeatDepthRange(out near, out far);
            if (!(far > near) || !(near > 0f)) return;
            StageLook.SetSeatBand(near, far);
            var urp = UniversalRenderPipeline.asset;
            float sd = urp != null ? urp.shadowDistance : 0f;
            if (sd > 0.01f)
            {
                _cascadeSplit = Mathf.Clamp((far + CascadeMargin) / sd, 0.05f, 0.95f);
                StageLook.SetShadowCascadeSplit(_cascadeSplit);
            }
        }

        /// <summary>
        /// 漂いの幅 (度) を決める: 場の中心 (座りうる席の重心) を軸に DriftTestDeg だけ回した時の、席の足元と頭 (+3 unit) の画面の動きの最大から、
        /// 横 0.9px・縦 0.6px に収まる幅にする (旗 drift=1 の時だけ計算する)
        /// </summary>
        static void ComputeDrift()
        {
            _driftYawAmp = 0f; _driftPitchAmp = 0f;
            if (!HD2DFlags.Drift || !_laidOut) return;
            var pts = SeatBandPoints();
            var c = Vector3.zero;
            foreach (var p in pts) c += p;
            _driftPivot = c / pts.Count;
            float sy = DriftSensitivity(pts, Vector3.up), sp = DriftSensitivity(pts, _right);
            _driftYawAmp = sy > 1e-4f ? Mathf.Min(DriftMaxDeg, DriftTestDeg * DriftYawPx / sy) : DriftMaxDeg;
            _driftPitchAmp = sp > 1e-4f ? Mathf.Min(DriftMaxDeg, DriftTestDeg * DriftPitchPx / sp) : DriftMaxDeg;
        }

        /// <summary>カメラを場の中心の軸 axis で DriftTestDeg だけ回した時の、席 (足元と +3 unit の頭) の画面の動き (px) の最大</summary>
        static float DriftSensitivity(List<Vector3> pts, Vector3 axis)
        {
            var q = Quaternion.AngleAxis(DriftTestDeg, axis);
            var pos = _driftPivot + q * (_camBase - _driftPivot);
            var rot = q * _layoutRot;
            float m = 0f;
            foreach (var p in pts)
                for (int h = 0; h < 2; h++)
                {
                    var w = p + Vector3.up * (h * 3f);
                    m = Mathf.Max(m, (ScreenOffset(w, _camBase, _layoutRot) - ScreenOffset(w, pos, rot)).magnitude);
                }
            return m;
        }

        /// <summary>世界の点の、画面の中心からの px (カメラ camPos・camRot。画角は今のレイアウトの画角)</summary>
        static Vector2 ScreenOffset(Vector3 world, Vector3 camPos, Quaternion camRot)
        {
            var l = Quaternion.Inverse(camRot) * (world - camPos);
            float z = Mathf.Max(0.01f, l.z);
            float pxPerUnit = _dist / (_k * z);
            return new Vector2(l.x * pxPerUnit, l.y * pxPerUnit);
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
            _seatWorld[key] = world;   // TryGetSeat (P10) が読む
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
        /// 基準の距離 ≈16.6 units なので 0.5 で約 3%。大技の着弾・とどめに。
        /// units は画角 36° の時の量: StageDriver が r (DistanceRatio) を掛けるので、どの画角でも同じ割合だけ寄る (P10)
        /// </summary>
        public static void ZoomPunch(float units, float dur = 0.32f)
        {
            if (_driver == null) return;
            _driver.PushAmp = Mathf.Max(_driver.PushAmp, units);
            _driver.PushT = Mathf.Max(0.05f, dur); _driver.PushDur = _driver.PushT;
        }

        /// <summary>ゆっくり寄って戻る (2026-09-17 ⑨⑧): ボスの登場・幕ボス撃破の余韻。inDur で units まで寄り、hold の後 outDur で戻る。units は画角 36° の時の量 (× r は StageDriver)</summary>
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
                push *= DistanceRatio;   // 寄りの量は画角 36° の時の量 = 今の距離の比を掛けて同じ割合だけ寄る (P10。36° なら ×1)
                var basePos = DriftBase();
                _cam.transform.position = basePos + off + _fwd * push;
                float t = Time.time;
                for (int i = 0; i < _world.childCount; i++)
                {
                    var c = _world.GetChild(i);
                    if (c.name == "rig-wheel") c.localRotation = Quaternion.Euler(0f, 0f, t * 18f);   // 回り続ける採掘の櫓
                    else if (c.name == "lantern-flame") c.localScale = new Vector3(0.36f, 0.36f, 1f) * (1f + 0.08f * Mathf.Sin(t * 9f) + 0.05f * Mathf.Sin(t * 23f));
                }
                if (_lantern != null) _lantern.intensity = _pal.LampIntensity * (1f + 0.06f * Mathf.Sin(t * 9f) + 0.04f * Mathf.Sin(t * 23f));
                // 箱庭 (P10): 動く物 (櫓の滑車など) を「基準の回転 × Z 回り」で回す (Diorama.Tick)
                if (Diorama.Active) Diorama.Tick(t);
                // 光が当たった (StageLook.Apply。P12 が Paint から呼ぶ) 後の最初のフレームで、カメラの距離と座席の帯とカスケードを渡す
                if (StageLook.Active) { if (!ReferenceEquals(_lookSynced, StageLook.Current)) PushLook(); }
                else _lookSynced = null;
            }

            float _driftBlend; bool _driftApplied;
            /// <summary>
            /// 待機の漂い (旗 drift=1・撮影では切る): 描画用のカメラを場の中心の軸で横 (17秒)・縦 (23秒) にごく小さく回し、その時のカメラの位置を返す。
            /// 入り切りは 1.5 秒でなじませる。漂っていなければ _camBase (回転は LayoutCamera のまま)
            /// </summary>
            Vector3 DriftBase()
            {
                float target = DriftAllowed ? 1f : 0f;
                if (_driftBlend != target) _driftBlend = Mathf.MoveTowards(_driftBlend, target, Time.deltaTime / DriftFade);
                if (_driftBlend > 0f && (_driftYawAmp > 0f || _driftPitchAmp > 0f))
                {
                    float tt = Time.time;
                    float yaw = _driftBlend * _driftYawAmp * Mathf.Sin(tt * (2f * Mathf.PI / DriftYawPeriod));
                    float pit = _driftBlend * _driftPitchAmp * Mathf.Sin(tt * (2f * Mathf.PI / DriftPitchPeriod) + 1.3f);
                    var q = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pit, _right);
                    _cam.transform.rotation = q * _layoutRot;
                    _driftApplied = true;
                    return _driftPivot + q * (_camBase - _driftPivot);
                }
                if (_driftApplied) { _cam.transform.rotation = _layoutRot; _driftApplied = false; }
                return _camBase;
            }
        }
    }
}
