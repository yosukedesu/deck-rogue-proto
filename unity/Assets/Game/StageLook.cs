// StageLook.cs — 幕ごとの光の設計図 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-4・§2-6・§2-7)。
// 骨組み (P00) の口に、P09 (W1) が中身を書いた:
//   Load    … 設計図 Resources/Stage/look_act<N>.json (舞台・P22) → .char.json (キャラ・P23) → .dof.json (ぼかし・P24) → .phone.json (スマホ・P31。tier=phone の時だけ)
//             を順に重ねて読む (オブジェクトはキーごとに混ぜる・配列と数は置き換える・「_」で始まるキーは説明)。書いていないキーは下の既定の値
//   Apply   … ライトの一式 (月＝平行光・舞台の灯＝スポット・逆光＝点光源) と木漏れ日のクッキー、Trilight の環境光、霧 (元の値×r)、URP の影の値、
//             後処理 (Volume)、カメラ (深度テクスチャ・MSAA)、キャラの全体値 (_CharKey*)、ぼかし (TiltShiftSettings)、箱庭の材質の受光を当てる。
//             当てる前の値は控えに取り (最初の Apply の時だけ)、Restore で丸ごと戻す
//   Restore … 控えに戻す (old に戻る時・終了の時)。技の光 (StageFx) も消す
// 見本の舞台 (stage=diorama) でだけ使う。stage=old では Apply は何もしない (W1 はまだ誰も呼ばない = 今の見た目のまま)。
//
// 約束 (§3) の外に足した口 (他のレーンが呼ぶ):
//   ApplyMaterials()         … 箱庭の材質 (Diorama.Materials) に受光 _Receive・影の強さ _ShadowStrength を書く。Apply の中でも呼ぶが、P12 は Diorama.Build の後にもう一度呼ぶ
//   ScaleByCameraDistance(d) … 霧の start・end と URP の影の距離を「元の値 × d ÷ baseCameraDistance」で書く (距離の値を書くのはここ1か所)。P10 が LayoutCamera のたびに _dist を渡す
//   SetSeatBand(near, far)   … 座席の帯の深さ (Stage.SeatDepthRange) にぼかしの余白を足して TiltShiftSettings へ。P10 が変わるたびに呼ぶ
//   SetShadowCascadeSplit(s) … 月の影のカスケードの分割を、控えを取った URP のアセットにだけ書く (P10 が座席の深さから計算して渡す)
//   CharReceive(hero)・HeroLift・CharAmbientScale … キャラの板の材質の値 (旗 receive=・herolift= が勝つ)。P11 が読む
//   Active・Current・Moon・Lamp・Backlight・DistanceScale・DebugInfo() … 詰めと dumplayout (layout.json の extra.look) 用
//
// W3 P22 (舞台の詰め) で足したもの:
//   旗 look= … 幕の設計図 (look_act1 + .char + .dof) の上に重ねる変種 (look=look_act1_w2light・「+」で複数)。今まで look= は丸ごとの差し替えで、使う物は無かった
//   灯の形 (lamp.mask・lamp.pool・lamp.fit) … 舞台の灯のクッキーに「座席の帯 ∪ 中央の光の池」を焼き、外を暗く。帯の t は今の座席 (Stage.TryGetUnitBox) に合わせて
//             LookDriver (光の一式の根に付く) が焼き直す。灯の位置と向き (= キャラのキー KeyDir) は設計図の帯から決めたまま変えない
//   霧の光の芯 (fog.lobe) … 全体値 _HD2DFogLobePos・_HD2DFogLobeColor。視線が坑口の奥の脈を向くほど霧が明るく、外れるほど暗い (StageModule の霧・StageShaft の光の面が読む)
//   高さの霧の深さ (fog.height.depthStart・depthFull) … 全体値 _HD2DHeightFogDepth (× r)。座席の帯より奥にだけ掛ける (StageModule が読む)
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    // ==== data-begin (scratchpad の検査がここから data-end までを取り出して .NET で読む。UnityEngine の型は Color・Vector2〜4・LightShadows だけ)
    /// <summary>
    /// 幕の設計図 (look_act&lt;N&gt;*.json を順に重ねたもの)。値を足すのは P09 (作った)・P22 (舞台)・P23 (キャラ)・P24 (ぼかし)・P31 (スマホ) の各ファイル。
    /// 既定の値 = 計画 §2-4 の初期値 (ファイルが無くても同じ光になる)。新しいキーは Raw (重ねた後の JSON) からも読める
    /// </summary>
    public sealed class StageLookData
    {
        public int Act;
        /// <summary>読んだ設計図の名前 (look_act1。旗 look= の時はその名前)</summary>
        public string Name;
        /// <summary>実際に読めたファイル (重ねた順。Resources/Stage/ の下の名前)</summary>
        public readonly List<string> Sources = new List<string>();
        /// <summary>重ねた後の生の JSON (このクラスに無いキーの読み先)</summary>
        public JObject Raw;
        /// <summary>道の向き (度)。道の座標 (t, s) → 世界 = Euler(0, PathYaw, 0) × (t, y, s)</summary>
        public float PathYaw = -22f;
        /// <summary>距離の値 (霧・影の距離) を書いた時のカメラの距離 (画角36° の _dist)。r = 今の距離 ÷ これ</summary>
        public float BaseCameraDistance = 16.62f;

        public readonly MoonLook Moon = new MoonLook();
        public readonly LampLook Lamp = new LampLook();
        public readonly BacklightLook Backlight = new BacklightLook();
        public readonly CookieLook Cookie = new CookieLook();
        public readonly AmbientLook Ambient = new AmbientLook();
        public readonly FogLook Fog = new FogLook();
        public readonly ShadowLook Shadow = new ShadowLook();
        public readonly PostLook Post = new PostLook();
        public readonly CameraLook Cam = new CameraLook();
        public readonly CharLook Char = new CharLook();
        public readonly DofLook Dof = new DofLook();
        public readonly HitLook Hit = new HitLook();
        /// <summary>箱庭の材質 (Diorama.Materials の名前) ごとの受光と影の強さ</summary>
        public readonly Dictionary<string, MaterialLook> Materials = new Dictionary<string, MaterialLook>();

        /// <summary>光の共通の値</summary>
        public class LightLook
        {
            public Color Color = Color.white;
            public LightShadows Shadows = LightShadows.Soft;
            public float ShadowStrength = 0.8f, ShadowBias = 0.05f, ShadowNormalBias = 0.4f;
            /// <summary>この光が照らす Rendering Layers (bit0 Default・bit1 Characters・bit2 Environment)</summary>
            public uint LightLayers = 0xFFFFFFFFu;
            /// <summary>この光の影を落とす Rendering Layers</summary>
            public uint ShadowLayers = 0xFFFFFFFFu;
            /// <summary>追加ライトの影の解像度の段 (-1 独自・0 低・1 中・2 高)</summary>
            public int ShadowTier = 1;
        }

        /// <summary>月 (平行光)。左上の手前から。影は地形と大物だけ</summary>
        public sealed class MoonLook : LightLook
        {
            public Vector3 Euler = new Vector3(48f, 38f, 0f);
            public float Intensity = 0.6f;
            public bool Cookie;
            public Vector2 CookieSize = new Vector2(24f, 24f);
            public MoonLook() { Color = new Color(0.66f, 0.76f, 1f); Shadows = LightShadows.Soft; ShadowStrength = 0.75f; ShadowBias = 0.05f; ShadowNormalBias = 0.4f; ShadowLayers = 1u << 2; }
        }

        /// <summary>舞台の灯 (スポット)。カメラ側の上から座席の帯へ。影はキャラの板だけ。キャラの固定のキーはこの灯の向き</summary>
        public sealed class LampLook : LightLook
        {
            /// <summary>帯でいちばん暗い所 (flatten=false なら狙いの点) の明るさ</summary>
            public float Illum = 1.65f;
            /// <summary>帯の真ん中 (狙いの点) から灯の位置へのずれ (世界の軸)</summary>
            public Vector3 Offset = new Vector3(-8f, 17f, -14f);
            /// <summary>座席の帯 (道の座標)</summary>
            public float TMin = -6.5f, TMax = 12.5f, SMin = -1.4f, SMax = 1.8f;
            /// <summary>狙いの点の高さ</summary>
            public float AimY = 0f;
            /// <summary>帯の四隅がクッキー (円錐) の半径のどこに来るか (0〜1)。小さいほど円錐が広い</summary>
            public float BandFill = 0.6f;
            /// <summary>内側の角度 ÷ 外側の角度</summary>
            public float InnerRatio = 0.78f;
            /// <summary>届く距離 = 四隅までのいちばん遠い距離 × これ</summary>
            public float RangeScale = 3f;
            /// <summary>クッキーで距離と入射角の差を打ち消し、帯の地面の明るさをそろえる</summary>
            public bool Flatten = true;
            /// <summary>
            /// 打ち消しで削る下限 (これより暗くはしない)。帯は「左手前 → 右奥」に伸び、影を右奥へ落とすには灯を主人公の側に置くので、
            /// 打ち消す前の帯のそろいは約 0.25 (近い端が4倍明るい)。0.2 なら全部そろう (P09 の検査 check-P09)
            /// </summary>
            public float FlattenFloor = 0.2f;
            public bool Cookie = true;

            // ---- 灯の形 (W3 P22。クッキーに焼く。灯の位置と向き = キャラのキーは帯から決めたまま変えない)
            /// <summary>座席の外を暗くする (クッキーに「座席の帯 ∪ 中央の光の池」の形を焼く。外は MaskOutside 倍)</summary>
            public bool MaskOn = true;
            /// <summary>帯 (t = 座席に合わせた範囲・s = SMin〜SMax) の外側の余白 (t, s)。縁は MaskSoft (半径に対する割合) でなじむ。MaskPower = 角の丸さ (大きいほど四角)</summary>
            public Vector2 MaskMargin = new Vector2(1.6f, 4f);
            public float MaskSoft = 0.25f, MaskPower = 6f, MaskOutside = 0.1f;
            /// <summary>手前 (s が小さい側) の余白 (負 = MaskMargin.y と同じ)。奥は座席の後ろの崖の根元まで照らし (③のむら)、手前は段の縁で止める</summary>
            public float MaskMarginFront = 3.2f;
            /// <summary>中央の光の池 (奥の段の真ん中へ伸ばす楕円。画面の中央の列を明るく保つ = ③)。Shear = s が1増えるごとに中心の t がずれる量 (画面の中央の線に沿わせる)</summary>
            public bool PoolOn = true;
            public float PoolT = 3.8f, PoolS = 4.8f, PoolRT = 5f, PoolRS = 9f, PoolShear = 0.4f, PoolLevel = 0.8f, PoolSoft = 0.35f;
            /// <summary>帯の t を今の座席 (主人公と敵の板の足元) に合わせる (敵1体なら右の地面は暗い)。外れる時は設計図の帯</summary>
            public bool FitSeats = true;
            public float FitPadLeft = 2f, FitPadRight = 2.2f, FitMinSpan = 8f;
            public LampLook() { Color = new Color(0.92f, 0.96f, 1f); Shadows = LightShadows.Soft; ShadowStrength = 0.8f; ShadowBias = 0.04f; ShadowNormalBias = 0.3f; ShadowTier = 2; ShadowLayers = 1u << 1; }
        }

        /// <summary>逆光: 坑口の奥の脈の青緑 (点光源)。影は PC だけ</summary>
        public sealed class BacklightLook : LightLook
        {
            public bool On = true;
            public float T = 9.5f, S = 23f, Y = 5.2f;
            /// <summary>IllumDist の距離での明るさ (Light.intensity = Illum × IllumDist²)</summary>
            public float Illum = 3.2f, IllumDist = 5f, Range = 16f;
            /// <summary>影を落とすか (スマホの段は false)</summary>
            public bool Shadow = true;
            public BacklightLook() { Color = new Color(0.40f, 0.95f, 0.88f); Shadows = LightShadows.Soft; ShadowStrength = 0.6f; ShadowTier = 0; ShadowLayers = 1u << 2; }
        }

        /// <summary>木漏れ日のクッキー (舞台の灯に付ける)</summary>
        public sealed class CookieLook
        {
            public int Size = 256, Seed = 20260930, Blobs = 4;
            /// <summary>中央の半径 (円錐の縁 = 1)。この内はコントラスト CenterContrast 以下</summary>
            public float CenterRadius = 0.64f;
            public float CenterContrast = 0.18f, EdgeContrast = 0.62f;
            /// <summary>明暗の縁のやわらかさ (形の半径に対する割合)</summary>
            public float Softness = 0.16f;
        }

        public sealed class AmbientLook
        {
            /// <summary>trilight・flat</summary>
            public string Mode = "trilight";
            public Color Sky = new Color(0.23f, 0.27f, 0.45f), Equator = new Color(0.16f, 0.19f, 0.30f), Ground = new Color(0.08f, 0.09f, 0.12f);
        }

        public sealed class FogLook
        {
            public bool On = true;
            public Color Color = new Color(0.18f, 0.25f, 0.34f);
            /// <summary>画角36° の時の開始と終了 (× r で書く)</summary>
            public float Start = 14f, End = 60f;
            public bool HeightOn = true;
            public Color HeightColor = new Color(0.34f, 0.59f, 0.64f);
            public float HeightBase = -0.6f, HeightTop = 4f, HeightDensity = 0.55f;
            /// <summary>高さの霧がかかり始める深さと満ちる深さ (画角36° の時の値 × r。座席の帯より奥だけに掛ける) (W3 P22)</summary>
            public float HeightDepthStart = 20f, HeightDepthFull = 32f;
            // ---- 霧の光の芯 (W3 P22): 坑口の奥の脈の方を向く霧ほど明るく、外れるほど暗い (中央が光り左右の端が沈む夜の霧)
            public bool LobeOn = true;
            /// <summary>芯の置き場 (道の座標)</summary>
            public float LobeT = 11f, LobeS = 30f, LobeY = 3f;
            /// <summary>絞り (視線と芯の向きの cos の乗数。画面の端 ≈ 24° で 16 なら 0.23 倍)</summary>
            public float LobePower = 16f;
            /// <summary>芯から外れた所の霧の色の倍率 (0〜1)</summary>
            public float LobeEdge = 0.5f;
            /// <summary>芯で霧に足す色 (× LobeStrength)</summary>
            public Color LobeColor = new Color(0.36f, 0.78f, 0.74f);
            public float LobeStrength = 1.7f;
        }

        public sealed class ShadowLook
        {
            /// <summary>画角36° の時の影の距離 (× r で書く)</summary>
            public float Distance = 42f;
            public int Cascades = 2;
            public float Cascade2Split = 0.33f;
            public int MainResolution = 2048, AdditionalResolution = 2048;
            /// <summary>影を落とす光の数の上限 (PC 3・スマホ 2)。技の光が影を借りる時もこの中で</summary>
            public int MaxShadowedLights = 3;
        }

        public sealed class PostLook
        {
            /// <summary>aces・neutral・none</summary>
            public string Tonemap = "aces";
            public float Exposure = 0.3f, Contrast = 10f, Saturation = 0f;
            public Color ColorFilter = new Color(0.95f, 0.98f, 1.04f);
            public Vector4 Lift = new Vector4(1f, 1f, 1f, 0f), Gamma = new Vector4(1f, 1f, 1f, 0f), Gain = new Vector4(1f, 1f, 1f, 0f);
            public float BloomThreshold = 1f, BloomIntensity = 0.8f, BloomScatter = 0.65f;
            public Color BloomTint = new Color(0.92f, 0.98f, 1f);
            public float VignetteIntensity = 0.25f, VignetteSmoothness = 0.45f;
            public Vector2 VignetteCenter = new Vector2(0.5f, 0.52f);
            public bool VignetteRounded;
            public Color VignetteColor = new Color(0.02f, 0.02f, 0.06f);
            public bool FilmGrain, ChromaticAberration;
        }

        public sealed class CameraLook
        {
            public bool DepthTexture = true;
            /// <summary>MSAA の標本数の上限 (スマホは 1 = 切る)</summary>
            public int MsaaMax = 8;
            public bool HasBackground = true;
            public Color Background = new Color(0.08f, 0.10f, 0.21f);
        }

        public sealed class CharLook
        {
            public Color Key = new Color(0.92f, 0.96f, 1f), KeyWarm = new Color(1f, 0.92f, 0.8f);
            public float KeyIntensity = 1f;
            public Color AmbTop = new Color(0.62f, 0.66f, 0.80f), AmbBottom = new Color(0.38f, 0.40f, 0.50f);
            public float AmbientScale = 1f, Receive = 0.6f;
            /// <summary>主役の受光 (負 = Receive と同じ)</summary>
            public float HeroReceive = -1f;
            public float HeroLift = 1.2f;
        }

        public sealed class DofLook
        {
            public float BandMargin = 0.8f, RampNear = 2.5f, RampFar = 9f, MaxPxPC = 24f, MaxPxPhone = 12f;
            public bool HalfRes;
            /// <summary>座席の帯がまだ無い時 (P10 の前) の帯 = カメラの距離 + この (手前, 奥)</summary>
            public Vector2 FallbackBandRel = new Vector2(-4f, 8f);
            public float FocalLength = 90f, Aperture = 4.5f;
            public int BladeCount = 5;
        }

        public sealed class HitLook
        {
            public int MaxLights = 3;
            public float Range = 4f;
            /// <summary>HitLight の intensity はこの距離での明るさ (Light.intensity = intensity × RefDist²)</summary>
            public float RefDist = 1.5f;
            public int ShadowTier = 0;
        }

        public sealed class MaterialLook
        {
            /// <summary>負 = 書かない (設計図の値のまま)</summary>
            public float Receive = -1f, ShadowStrength = -1f;
            /// <summary>光の面 (StageShaft) の色と強さの上書き (W3 の統合・本家の色彩)。null・負 = 書かない (設計図 act1_layout.json の値のまま)</summary>
            public Color? Tint;
            public float Intensity = -1f;
        }
    }
    // ==== data-end

    public static class StageLook
    {
        // ================================================================ 約束の口 (§3)

        /// <summary>
        /// 幕 act の設計図を読む。look_act&lt;N&gt; → .char → .dof → (旗 look= の上書きの設計図) → (tier=phone なら) .phone の順に重ねる。
        /// 旗 look= は「幕の設計図の上に重ねる変種」(W3 P22 で丸ごとの差し替えから変えた。今まで look= を使う物は無かった):
        /// look=look_act1_w2 なら look_act1 一式の上に look_act1_w2.json を重ねる。「+」でつなぐと順に重ねる (look=look_act1_w2+look_act1_recv20)。
        /// 見つからない変種は警告して飛ばす。1つも読めなければ既定の値 (計画 §2-4 の初期値) だけの設計図。
        /// 舞台には何もしない (エディタの検査 = HD2DSetup の checks からも呼ばれる)
        /// </summary>
        public static StageLookData Load(int act)
        {
            string custom = HD2DFlags.Look;
            var overlays = new List<string>();
            if (!string.IsNullOrEmpty(custom))
                foreach (var raw in custom.Split('+', ' '))
                {
                    string n = raw.Trim();
                    if (n.Length == 0 || n == DefaultName(act) || overlays.Contains(n)) continue;
                    overlays.Add(n);
                }
            var d = LoadNamed(act, DefaultName(act), overlays);
            foreach (var n in overlays)
                if (!d.Sources.Contains(n)) Debug.LogWarning("[StageLook] 変種の設計図 " + ResourceDir + n + " が無い (飛ばす)");
            return d;
        }

        /// <summary>
        /// 設計図を舞台に当てる。ライトの一式 (月・舞台の灯・逆光) を rigParent の下に作り、環境光・霧・URP の影の値・カメラ・Volume・キャラの全体値・
        /// ぼかし・箱庭の材質を書き換える。書き換える前の値は最初の Apply の時に控えに取る (2回目以降は控えを取り直さない = Restore は最初の状態へ戻る)。
        /// cam が null なら Stage.Camera (無ければ Camera.main)。profile が null なら後処理は触らない。stage=old では何もしない
        /// </summary>
        public static void Apply(int act, Transform rigParent, Camera cam, VolumeProfile profile)
        {
            if (HD2DFlags.StageMode != HD2DStage.Diorama)
            {
                if (!_warnedOld) { _warnedOld = true; Debug.LogWarning("[StageLook] stage=old では光を当てない (Apply は何もしない)"); }
                return;
            }
            EnsureIds();
            if (cam == null) cam = Stage.Camera != null ? Stage.Camera : Camera.main;
            var d = Load(act);
            StageFx.StopAll();   // 前の Apply の技の光を消し、借りていた影を返す
            TakeBackups(cam, profile);
            Current = d;
            Active = true;
            if (!_hooked) { _hooked = true; Application.quitting += OnQuit; HD2DFlags.Changed += OnFlagsChanged; }

            BuildRig(rigParent, d);
            ApplyAmbient(d);
            ApplyFogColor(d);
            ApplyShadowSettings(d);
            ScaleByCameraDistance(CameraDistanceOf(cam));
            ApplyCamera(cam, d);
            ApplyTiltShift(d);
            ApplyVolume(profile, d);
            ApplyCharGlobals(d);
            ApplyHeightFog(d);
            ApplyEnvGrade(d);
            ApplyMaterials();
            StageFx.Prepare(d.Hit);
            HD2DFlags.LayoutDumpers["look"] = DebugInfo;
            Debug.Log("[StageLook] " + Summary());
        }

        /// <summary>Apply で書き換えた物を控えに戻す (old に戻る時・終了の時)。ライトの一式と技の光を消す。当てていなければ何もしない</summary>
        public static void Restore()
        {
            StageFx.StopAll();
            if (!Active) return;
            EnsureIds();
            DestroyRig();
            if (_bakVol != null) { LastRestoreVolumeOk = _bakVol.Put(); _bakVol = null; }
            if (_bakCam != null) { _bakCam.Put(); _bakCam = null; }
            if (_bakUrp != null) { _bakUrp.Put(); _bakUrp = null; }
            if (_bakRender != null) { _bakRender.Put(); _bakRender = null; }
            if (_bakTs != null) { _bakTs.Put(); _bakTs = null; }
            ClearGlobals();
            KeyDir = Vector3.down;
            KeyColor = DefaultKeyColor;
            Active = false;
            Current = null;
            DistanceScale = 1f;
            _seatBandSet = false;
            _borrowed = 0; _backlightLent = false;
            _lampBasis = null; _maskFitted = false; _maskBakes = 0; _fitSeats.Clear();
            HD2DFlags.LayoutDumpers.Remove("look");
            Debug.Log("[StageLook] Restore (Volume は控えと" + (LastRestoreVolumeOk ? "一致" : "不一致") + ")");
        }

        /// <summary>キャラの固定のキーライトの向き (光が進む向き・世界)。舞台の灯 (スポット) の向きと同じ。当てていない時は真下</summary>
        public static Vector3 KeyDir { get; private set; } = Vector3.down;

        /// <summary>キャラの固定のキーライトの色 (既定は中立からわずかに寒色。旗 keycolor=warm で設計図の keyWarm)</summary>
        public static Color KeyColor { get; private set; } = new Color(0.92f, 0.96f, 1f, 1f);   // = DefaultKeyColor (静的な初期化は書いた順なので、後ろの定数は読まない)

        // ================================================================ 足した口

        /// <summary>光を当てている (Apply から Restore まで)</summary>
        public static bool Active { get; private set; }
        /// <summary>当てている設計図 (当てていなければ null)</summary>
        public static StageLookData Current { get; private set; }
        /// <summary>月・舞台の灯・逆光 (無ければ null)</summary>
        public static Light Moon => _moon;
        public static Light Lamp => _lamp;
        public static Light Backlight => _backlight;
        /// <summary>今の r (カメラの距離 ÷ baseCameraDistance)。距離の値はこれを掛けて書いてある</summary>
        public static float DistanceScale { get; private set; } = 1f;
        /// <summary>最後の Restore で、Volume が控えと一致したか (計画 P09 の W2 の確かめ方)</summary>
        public static bool LastRestoreVolumeOk { get; private set; } = true;

        /// <summary>キャラの板の受光率 (旗 receive= が勝つ)。hero = 主役 (設計図の heroReceive。無ければ receive)</summary>
        public static float CharReceive(bool hero)
        {
            if (HD2DFlags.Receive >= 0f) return HD2DFlags.Receive;
            var c = Current != null ? Current.Char : DefaultChar;
            return hero && c.HeroReceive >= 0f ? c.HeroReceive : c.Receive;
        }

        /// <summary>主役の持ち上げ (旗 herolift= が勝つ)</summary>
        public static float HeroLift => HD2DFlags.HeroLift >= 0f ? HD2DFlags.HeroLift : (Current != null ? Current.Char.HeroLift : DefaultChar.HeroLift);

        /// <summary>キャラの板の環境光の倍率 (StageUnitLit の _AmbientScale)</summary>
        public static float CharAmbientScale => Current != null ? Current.Char.AmbientScale : DefaultChar.AmbientScale;

        /// <summary>
        /// 箱庭の材質 (Diorama.Materials の名前) に、設計図の受光 _Receive と影の強さ _ShadowStrength を書く (設計図に書いた名前と値だけ)。
        /// Apply の中でも呼ぶ。P12 は Diorama.Build の後にもう一度呼ぶ (Build が材質を作り直すため)
        /// </summary>
        public static void ApplyMaterials()
        {
            var d = Current;
            if (!Active || d == null) return;
            EnsureIds();
            foreach (var kv in d.Materials)
            {
                Material m;
                if (!Diorama.Materials.TryGetValue(kv.Key, out m) || m == null) continue;
                if (kv.Value.Receive >= 0f && m.HasProperty(_idReceive)) m.SetFloat(_idReceive, kv.Value.Receive);
                if (kv.Value.ShadowStrength >= 0f && m.HasProperty(_idShadowStrength)) m.SetFloat(_idShadowStrength, kv.Value.ShadowStrength);
            }
            // 光の面の色と強さの上書き (W3 の統合・本家の色彩)。上書きの無い材質は設計図の値へ戻す
            foreach (var mkv in Diorama.Materials)
            {
                var m = mkv.Value;
                if (m == null || !m.HasProperty(_idTint) || !m.HasProperty(_idIntensity)) continue;
                if (!_matOrig.ContainsKey(m)) _matOrig[m] = new KeyValuePair<Color, float>(m.GetColor(_idTint), m.GetFloat(_idIntensity));
                var orig = _matOrig[m];
                StageLookData.MaterialLook ml;
                d.Materials.TryGetValue(mkv.Key, out ml);
                m.SetColor(_idTint, ml != null && ml.Tint.HasValue ? ml.Tint.Value : orig.Key);
                m.SetFloat(_idIntensity, ml != null && ml.Intensity >= 0f ? ml.Intensity : orig.Value);
            }
        }

        /// <summary>
        /// 距離の値を書く (1か所): 霧の start・end と URP の影の距離 = 設計図の値 (画角36° の時) × r (r = dist ÷ baseCameraDistance)。
        /// dist = カメラの距離 (Stage の _dist)。P10 が LayoutCamera のたびに呼ぶ。当てていなければ何もしない
        /// </summary>
        public static void ScaleByCameraDistance(float dist)
        {
            var d = Current;
            if (!Active || d == null) return;
            if (!(dist > 0.01f)) dist = d.BaseCameraDistance;
            _camDist = dist;
            float r = dist / Mathf.Max(0.01f, d.BaseCameraDistance);
            DistanceScale = r;
            RenderSettings.fogStartDistance = d.Fog.Start * r;
            RenderSettings.fogEndDistance = d.Fog.End * r;
            EnsureIds();
            Shader.SetGlobalVector(_idHFogDepth, new Vector4(d.Fog.HeightDepthStart * r, Mathf.Max(d.Fog.HeightDepthStart + 0.01f, d.Fog.HeightDepthFull) * r, 0f, 0f));   // 高さの霧の深さ (W3 P22)
            var urp = BackedUrp();
            if (urp != null) urp.shadowDistance = d.Shadow.Distance * r;
            if (!_seatBandSet) ApplyFallbackBand();
        }

        /// <summary>
        /// 座席の帯の深さ (カメラからの距離。Stage.SeatDepthRange) を渡す。設計図の bandMargin を足して TiltShiftSettings.BandNear・BandFar に書き、
        /// dof=urp の時は URP の焦点を帯の真ん中に置く。P10 が変わるたびに呼ぶ。当てていなければ何もしない
        /// </summary>
        public static void SetSeatBand(float near, float far)
        {
            var d = Current;
            if (!Active || d == null || !(far > near) || !(near > 0f)) return;
            float m = Mathf.Max(0f, d.Dof.BandMargin);
            TiltShiftSettings.BandNear = Mathf.Max(0.01f, near - m);
            TiltShiftSettings.BandFar = far + m;
            _seatBandSet = true;
            SyncUrpFocus();
        }

        /// <summary>
        /// 月の影の2段のカスケードの分割 (影の距離に対する割合 0〜1) を書く。P10 が座席の奥の深さから計算して渡す (座席が1段目に入るように)。
        /// 控えを取った URP のアセットにだけ書く (Restore で元に戻る)。当てていなければ何もしない = 今の舞台のアセットは触らない
        /// </summary>
        public static void SetShadowCascadeSplit(float split01)
        {
            if (!Active) return;
            var urp = BackedUrp();
            if (urp != null) urp.cascade2Split = Mathf.Clamp(split01, 0.01f, 0.99f);
        }

        /// <summary>dumplayout (layout.json の extra.look) と詰めの確認用: ライトの一覧・影の数・距離・霧・環境光・URP の値・ぼかし</summary>
        public static object DebugInfo()
        {
            var o = new Dictionary<string, object>();
            o["active"] = Active;
            var d = Current;
            o["name"] = d != null ? d.Name : null;
            o["sources"] = d != null ? new List<string>(d.Sources) : new List<string>();
            o["r"] = DistanceScale;
            o["cameraDistance"] = _camDist;
            o["keyDir"] = KeyDir;
            o["keyColor"] = KeyColor;
            var lights = new List<object>();
            foreach (var l in new[] { _moon, _lamp, _backlight }) if (l != null) lights.Add(LightInfo(l));
            foreach (var l in StageFx.PoolLights()) if (l != null) lights.Add(LightInfo(l));
            o["lights"] = lights;
            o["shadowed"] = RigShadowCount() + _borrowed;
            o["shadowCap"] = d != null ? d.Shadow.MaxShadowedLights : 0;
            o["shadowBorrowed"] = _borrowed;
            o["backlightLent"] = _backlightLent;
            if (_lampStats != null) o["lamp"] = _lampStats;
            o["ambient"] = new Dictionary<string, object>
            {
                { "mode", RenderSettings.ambientMode.ToString() }, { "sky", RenderSettings.ambientSkyColor },
                { "equator", RenderSettings.ambientEquatorColor }, { "ground", RenderSettings.ambientGroundColor },
            };
            o["fog"] = new Dictionary<string, object>
            {
                { "on", RenderSettings.fog }, { "mode", RenderSettings.fogMode.ToString() }, { "color", RenderSettings.fogColor },
                { "start", RenderSettings.fogStartDistance }, { "end", RenderSettings.fogEndDistance },
                // W3 P22: 霧の光の芯・高さの霧 (シェーダの全体値の今の値)
                { "lobePos", _idsReady ? Shader.GetGlobalVector(_idLobePos) : Vector4.zero }, { "lobeColor", _idsReady ? Shader.GetGlobalVector(_idLobeColor) : Vector4.zero },
                { "heightColor", _idsReady ? Shader.GetGlobalVector(_idHFogColor) : Vector4.zero }, { "heightRange", _idsReady ? Shader.GetGlobalVector(_idHFogRange) : Vector4.zero },
                { "heightDepth", _idsReady ? Shader.GetGlobalVector(_idHFogDepth) : Vector4.zero },
            };
            o["envGrade"] = _idsReady ? Shader.GetGlobalVector(_idEnvGrade) : Vector4.zero;   // 舞台の色の寄せ (W3 の統合)
            var urp = BackedUrp();
            if (urp != null)
                o["urp"] = new Dictionary<string, object>
                {
                    { "asset", urp.name }, { "shadowDistance", urp.shadowDistance }, { "cascades", urp.shadowCascadeCount },
                    { "cascade2Split", urp.cascade2Split }, { "mainShadowRes", urp.mainLightShadowmapResolution },
                    { "additionalShadowRes", urp.additionalLightsShadowmapResolution }, { "msaa", urp.msaaSampleCount },
                    { "additionalLightShadows", urp.supportsAdditionalLightShadows }, { "renderingLayers", urp.useRenderingLayers },
                };
            o["tiltShift"] = new Dictionary<string, object>
            {
                { "enabled", TiltShiftSettings.Enabled }, { "urp", TiltShiftSettings.UseUrpBokeh }, { "bandNear", TiltShiftSettings.BandNear },
                { "bandFar", TiltShiftSettings.BandFar }, { "rampNear", TiltShiftSettings.RampNear }, { "rampFar", TiltShiftSettings.RampFar },
                { "maxPxPC", TiltShiftSettings.MaxPxPC }, { "maxPxPhone", TiltShiftSettings.MaxPxPhone }, { "halfRes", TiltShiftSettings.HalfRes },
                { "seatBandFromStage", _seatBandSet },
            };
            o["volumeRestoreOk"] = LastRestoreVolumeOk;
            return o;
        }

        // ================================================================ 技の光 (StageFx) との影の貸し借り

        /// <summary>
        /// 技の光が影を1つ借りる。影を落とす光の数 (リグ＋借りている分) が上限 (shadow.maxShadowedLights。PC 3・スマホ 2) 未満なら貸す。
        /// 上限に届いていれば、逆光の影を一時的に止めて貸す (逆光は奥にあり、当たりの間は目立たない)。どちらもできなければ false
        /// </summary>
        internal static bool TryBorrowShadowSlot()
        {
            var d = Current;
            if (!Active || d == null) return false;
            int cap = Mathf.Max(0, d.Shadow.MaxShadowedLights);
            if (RigShadowCount() + _borrowed < cap) { _borrowed++; return true; }
            if (_backlight != null && _backlight.shadows != LightShadows.None && !_backlightLent)
            {
                _backlightShadow = _backlight.shadows;
                _backlight.shadows = LightShadows.None;
                _backlightLent = true;
                _borrowed++;
                return true;
            }
            return false;
        }

        /// <summary>借りた影を返す。借りている物が無くなったら逆光の影を戻す</summary>
        internal static void ReturnShadowSlot()
        {
            if (_borrowed > 0) _borrowed--;
            if (_borrowed == 0 && _backlightLent)
            {
                if (_backlight != null) _backlight.shadows = _backlightShadow;
                _backlightLent = false;
            }
        }

        // ================================================================ 中身

        const string ResourceDir = "Stage/";
        static readonly Color DefaultKeyColor = new Color(0.92f, 0.96f, 1f, 1f);
        static readonly StageLookData.CharLook DefaultChar = new StageLookData.CharLook();

        static bool _warnedOld, _hooked, _idsReady, _seatBandSet, _backlightLent, _warnedUrpSwap;
        static int _borrowed;
        static LightShadows _backlightShadow = LightShadows.Soft;
        static float _camDist;
        static Transform _rig;
        static Light _moon, _lamp, _backlight;
        static Texture2D _lampCookie, _moonCookie;
        static Dictionary<string, object> _lampStats;
        static RenderBak _bakRender;
        static UrpBak _bakUrp;
        static CamBak _bakCam;
        static VolBak _bakVol;
        static TsBak _bakTs;
        static int _idKeyDir, _idKeyColor, _idAmbTop, _idAmbBottom, _idHFogColor, _idHFogRange, _idReceive, _idShadowStrength;
        static int _idHFogDepth, _idLobePos, _idLobeColor;   // 高さの霧の深さ・霧の光の芯 (W3 P22。StageModule・StageShaft が読む)
        static int _idEnvGrade, _idTint, _idIntensity;        // 舞台の色の寄せ (W3 の統合・本家の色彩。StageModule が読む)・光の面の色と強さ

        static string DefaultName(int act) { return "look_act" + act; }

        /// <summary>全体値の番号 (初めて当てる時に1回。静的な初期化で Unity を呼ばない)</summary>
        static void EnsureIds()
        {
            if (_idsReady) return;
            _idsReady = true;
            _idKeyDir = Shader.PropertyToID("_CharKeyDir");            // HD2DCharLight.hlsl (P03)
            _idKeyColor = Shader.PropertyToID("_CharKeyColor");
            _idAmbTop = Shader.PropertyToID("_CharAmbTop");
            _idAmbBottom = Shader.PropertyToID("_CharAmbBottom");
            _idHFogColor = Shader.PropertyToID("_HD2DHeightFogColor"); // 高さの霧: rgb = 色 (線形)・a = 濃さ (読むシェーダは P22/P03)
            _idHFogRange = Shader.PropertyToID("_HD2DHeightFogRange"); // x = 下の高さ・y = 上の高さ・z = 1 なら有効
            _idReceive = Shader.PropertyToID("_Receive");               // StageModule (P03)
            _idShadowStrength = Shader.PropertyToID("_ShadowStrength");
            _idHFogDepth = Shader.PropertyToID("_HD2DHeightFogDepth");  // x = かかり始める深さ・y = 満ちる深さ (W3 P22)
            _idLobePos = Shader.PropertyToID("_HD2DFogLobePos");        // xyz = 霧の光の芯 (世界)・w = 絞り (0 = 使わない)
            _idLobeColor = Shader.PropertyToID("_HD2DFogLobeColor");    // rgb = 芯で足す色 (線形)・a = 外れた所の霧の色の倍率
            _idEnvGrade = Shader.PropertyToID("_HD2DEnvGrade");         // xyz = 色の倍率 − 1・w = 彩度を落とす量 (W3 の統合。0 = そのまま)
            _idTint = Shader.PropertyToID("_Tint");                     // StageShaft の光の面の色
            _idIntensity = Shader.PropertyToID("_Intensity");
        }

        static StageLookData LoadNamed(int act, string baseName, IList<string> overlays)
        {
            var names = new List<string> { baseName, baseName + ".char", baseName + ".dof" };
            if (overlays != null) names.AddRange(overlays);   // 旗 look= の変種 (W3 P22)。スマホの段より先 = スマホの段の軽くする値が最後に勝つ
            if (HD2DFlags.Tier == HD2DTier.Phone) names.Add(baseName + ".phone");
            var texts = new List<KeyValuePair<string, string>>();
            TextAsset[] dir = null;
            foreach (var n in names)
            {
                TextAsset ta = null;
                try { ta = Resources.Load<TextAsset>(ResourceDir + n); }
                catch (Exception e) { Debug.LogWarning("[StageLook] " + ResourceDir + n + " を読めない: " + e.Message); }
                // 名前に点がある設計図 (look_act1.char など): Resources.Load が点の後ろを拡張子として扱って見つけないことがある (W1 で未確認 = P09 の申し送り)。
                // その時はフォルダの TextAsset を全部読み、アセットの名前 (= ファイル名から最後の拡張子だけを除いた物) で探す (W2 P12 の直し)
                if (ta == null && n.IndexOf('.') >= 0)
                {
                    try { if (dir == null) dir = Resources.LoadAll<TextAsset>(ResourceDir.TrimEnd('/')); }
                    catch (Exception e) { Debug.LogWarning("[StageLook] " + ResourceDir + " を読めない: " + e.Message); dir = new TextAsset[0]; }
                    foreach (var t in dir) if (t != null && t.name == n) { ta = t; break; }
                }
                if (ta != null) texts.Add(new KeyValuePair<string, string>(n, ta.text));
            }
            var d = FromLayers(act, texts);
            d.Name = overlays != null && overlays.Count > 0 ? baseName + "+" + string.Join("+", overlays) : baseName;
            return d;
        }

        static void OnQuit() { Restore(); }   // エディタの再生の終わりに URP のアセットの値を戻す (実行時に書いた値がアセットに残らないように)

        /// <summary>旗が変わった時 (keycolor・dof・aa): 当てている間だけ、旗から決まる値を当て直す。stage の切り替えは P12 が Apply／Restore で行う</summary>
        static void OnFlagsChanged()
        {
            if (!Active || Current == null || HD2DFlags.StageMode != HD2DStage.Diorama) return;
            ApplyCharGlobals(Current);
            ApplyTiltShift(Current);
            if (_bakCam != null && _bakCam.Cam != null) ApplyMsaa(_bakCam.Cam, Current);
        }

        // ---------------------------------------------------------------- 控え

        static void TakeBackups(Camera cam, VolumeProfile profile)
        {
            if (_bakRender == null) _bakRender = RenderBak.Take();
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (_bakUrp != null && _bakUrp.Asset != urp) { _bakUrp.Put(); _bakUrp = null; }   // 品質の段 (tier) が変わった = 前のアセットを戻して今のアセットの控えを取る
            if (_bakUrp == null && urp != null) _bakUrp = UrpBak.Take(urp);
            if (_bakCam != null && _bakCam.Cam != cam) { _bakCam.Put(); _bakCam = null; }
            if (_bakCam == null && cam != null) _bakCam = CamBak.Take(cam);
            if (_bakVol != null && _bakVol.Profile != profile) { LastRestoreVolumeOk = _bakVol.Put(); _bakVol = null; }
            if (_bakVol == null && profile != null) _bakVol = VolBak.Take(profile);
            if (_bakTs == null) _bakTs = TsBak.Take();
        }

        /// <summary>控えを取ったアセット (実行時に書くのはこれだけ。今のアセットが違えば書かない)</summary>
        static UniversalRenderPipelineAsset BackedUrp()
        {
            if (_bakUrp == null || _bakUrp.Asset == null) return null;
            if (!ReferenceEquals(GraphicsSettings.currentRenderPipeline, _bakUrp.Asset))
            {
                if (!_warnedUrpSwap) { _warnedUrpSwap = true; Debug.LogWarning("[StageLook] URP のアセットが Apply の後で替わった (tier の切り替え?)。次の Apply まで影の値は書かない"); }
                return null;
            }
            return _bakUrp.Asset;
        }

        sealed class RenderBak
        {
            AmbientMode _mode; Color _sky, _equator, _ground; float _ambIntensity;
            bool _fog; FogMode _fogMode; Color _fogColor; float _fogStart, _fogEnd, _fogDensity;
            Light _sun;
            public static RenderBak Take()
            {
                return new RenderBak
                {
                    _mode = RenderSettings.ambientMode, _sky = RenderSettings.ambientSkyColor, _equator = RenderSettings.ambientEquatorColor,
                    _ground = RenderSettings.ambientGroundColor, _ambIntensity = RenderSettings.ambientIntensity,
                    _fog = RenderSettings.fog, _fogMode = RenderSettings.fogMode, _fogColor = RenderSettings.fogColor,
                    _fogStart = RenderSettings.fogStartDistance, _fogEnd = RenderSettings.fogEndDistance, _fogDensity = RenderSettings.fogDensity,
                    _sun = RenderSettings.sun,
                };
            }
            public void Put()
            {
                RenderSettings.ambientMode = _mode;
                RenderSettings.ambientSkyColor = _sky;   // = ambientLight (Flat の色)
                RenderSettings.ambientEquatorColor = _equator;
                RenderSettings.ambientGroundColor = _ground;
                RenderSettings.ambientIntensity = _ambIntensity;
                RenderSettings.fog = _fog;
                RenderSettings.fogMode = _fogMode;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogStartDistance = _fogStart;
                RenderSettings.fogEndDistance = _fogEnd;
                RenderSettings.fogDensity = _fogDensity;
                RenderSettings.sun = _sun;
                UpdateEnvironment();
            }
        }

        sealed class UrpBak
        {
            public UniversalRenderPipelineAsset Asset;
            float _shadowDistance, _split; int _cascades, _mainRes, _addRes, _msaa;
            public static UrpBak Take(UniversalRenderPipelineAsset a)
            {
                return new UrpBak
                {
                    Asset = a, _shadowDistance = a.shadowDistance, _cascades = a.shadowCascadeCount, _split = a.cascade2Split,
                    _mainRes = a.mainLightShadowmapResolution, _addRes = a.additionalLightsShadowmapResolution, _msaa = a.msaaSampleCount,
                };
            }
            public void Put()
            {
                if (Asset == null) return;
                Asset.shadowDistance = _shadowDistance;
                try { Asset.shadowCascadeCount = _cascades; } catch (Exception e) { Debug.LogWarning("[StageLook] カスケードの数を戻せない: " + e.Message); }
                Asset.cascade2Split = _split;
                Asset.mainLightShadowmapResolution = _mainRes;
                Asset.additionalLightsShadowmapResolution = _addRes;
                Asset.msaaSampleCount = _msaa;
            }
        }

        sealed class CamBak
        {
            public Camera Cam;
            Color _bg; bool _allowMsaa, _hasData; CameraOverrideOption _depth;
            public static CamBak Take(Camera c)
            {
                var b = new CamBak { Cam = c, _bg = c.backgroundColor, _allowMsaa = c.allowMSAA };
                try { var cd = c.GetUniversalAdditionalCameraData(); b._depth = cd.requiresDepthOption; b._hasData = true; }
                catch (Exception) { b._hasData = false; }
                return b;
            }
            public void Put()
            {
                if (Cam == null) return;
                Cam.backgroundColor = _bg;
                Cam.allowMSAA = _allowMsaa;
                if (_hasData) { try { Cam.GetUniversalAdditionalCameraData().requiresDepthOption = _depth; } catch (Exception) { } }
            }
        }

        /// <summary>Volume の控え: 触る型の部品だけ、部品の有効と、各値 (overrideState と値の写し)。Apply で足した部品は Put で外す</summary>
        sealed class VolBak
        {
            public VolumeProfile Profile;
            readonly List<KeyValuePair<VolumeComponent, bool>> _actives = new List<KeyValuePair<VolumeComponent, bool>>();
            readonly List<ParamBak> _params = new List<ParamBak>();
            public readonly List<Type> Added = new List<Type>();
            struct ParamBak { public VolumeParameter P; public VolumeParameter Copy; public bool Override; }

            public static VolBak Take(VolumeProfile p)
            {
                var b = new VolBak { Profile = p };
                foreach (var t in TouchedTypes)
                {
                    VolumeComponent c;
                    if (!p.TryGet(t, out c) || c == null) continue;
                    b._actives.Add(new KeyValuePair<VolumeComponent, bool>(c, c.active));
                    foreach (var prm in c.parameters)
                    {
                        if (prm == null) continue;
                        VolumeParameter copy = null;
                        try { copy = prm.Clone() as VolumeParameter; } catch (Exception) { }
                        if (copy != null) b._params.Add(new ParamBak { P = prm, Copy = copy, Override = prm.overrideState });
                    }
                }
                return b;
            }

            /// <summary>控えに戻す。戻した後に1つずつ比べ、全部一致なら true</summary>
            public bool Put()
            {
                if (Profile == null) return true;
                foreach (var t in Added) { try { Profile.Remove(t); } catch (Exception e) { Debug.LogWarning("[StageLook] Volume の部品を外せない: " + t.Name + " " + e.Message); } }
                Added.Clear();
                foreach (var kv in _actives) if (kv.Key != null) kv.Key.active = kv.Value;
                foreach (var pb in _params)
                {
                    try { pb.P.SetValue(pb.Copy); } catch (Exception e) { Debug.LogWarning("[StageLook] Volume の値を戻せない: " + e.Message); }
                    pb.P.overrideState = pb.Override;
                }
                int bad = 0;
                foreach (var kv in _actives) if (kv.Key != null && kv.Key.active != kv.Value) bad++;
                foreach (var pb in _params) if (pb.P.overrideState != pb.Override || !pb.P.Equals(pb.Copy)) bad++;
                foreach (var t in TouchedTypes)
                {
                    bool had = false;
                    foreach (var kv in _actives) if (kv.Key != null && kv.Key.GetType() == t) had = true;
                    VolumeComponent c;
                    if (!had && Profile.TryGet(t, out c) && c != null) bad++;   // 足した部品が残っている
                }
                if (bad > 0) Debug.LogWarning("[StageLook] Volume が控えと " + bad + " か所違う");
                return bad == 0;
            }
        }

        sealed class TsBak
        {
            bool _enabled, _urp, _half; float _bn, _bf, _rn, _rf, _mpc, _mph;
            public static TsBak Take()
            {
                return new TsBak
                {
                    _enabled = TiltShiftSettings.Enabled, _urp = TiltShiftSettings.UseUrpBokeh, _half = TiltShiftSettings.HalfRes,
                    _bn = TiltShiftSettings.BandNear, _bf = TiltShiftSettings.BandFar, _rn = TiltShiftSettings.RampNear, _rf = TiltShiftSettings.RampFar,
                    _mpc = TiltShiftSettings.MaxPxPC, _mph = TiltShiftSettings.MaxPxPhone,
                };
            }
            public void Put()
            {
                TiltShiftSettings.Enabled = _enabled; TiltShiftSettings.UseUrpBokeh = _urp; TiltShiftSettings.HalfRes = _half;
                TiltShiftSettings.BandNear = _bn; TiltShiftSettings.BandFar = _bf; TiltShiftSettings.RampNear = _rn; TiltShiftSettings.RampFar = _rf;
                TiltShiftSettings.MaxPxPC = _mpc; TiltShiftSettings.MaxPxPhone = _mph;
            }
        }

        /// <summary>Apply が書き換える Volume の部品の型 (控えを取るのもこれだけ)</summary>
        static readonly Type[] TouchedTypes =
        {
            typeof(Tonemapping), typeof(Bloom), typeof(Vignette), typeof(ColorAdjustments), typeof(LiftGammaGain),
            typeof(FilmGrain), typeof(ChromaticAberration), typeof(DepthOfField),
        };

        // ---------------------------------------------------------------- ライトの一式

        static void BuildRig(Transform parent, StageLookData d)
        {
            DestroyRig();
            var root = new GameObject("HD2D-LookRig");
            if (parent != null) root.transform.SetParent(parent, false);
            _rig = root.transform;
            if (Application.isPlaying) root.AddComponent<LookDriver>();   // 灯の帯を座席に合わせる (W3 P22)

            // 月 (平行光): 左上の手前から。影は地形と大物だけ
            var mgo = new GameObject("HD2D-Moon");
            mgo.transform.SetParent(_rig, false);
            mgo.transform.rotation = Quaternion.Euler(d.Moon.Euler);
            _moon = mgo.AddComponent<Light>();
            _moon.type = LightType.Directional;
            _moon.color = d.Moon.Color;
            _moon.intensity = Mathf.Max(0f, d.Moon.Intensity);
            SetupShadows(_moon, d.Moon, d.Moon.Shadows);
            if (d.Moon.Cookie)
            {
                _moonCookie = MakeCookieTexture("HD2D-MoonCookie", ClampSize(d.Cookie.Size), CookiePattern(ClampSize(d.Cookie.Size), d.Cookie.Seed + 1, d.Cookie.Blobs,
                    d.Cookie.CenterRadius, d.Cookie.CenterContrast, d.Cookie.EdgeContrast, d.Cookie.Softness), null, TextureWrapMode.Repeat);
                _moon.cookie = _moonCookie;
                try { var ad = _moon.GetUniversalAdditionalLightData(); ad.lightCookieSize = d.Moon.CookieSize; ad.lightCookieOffset = Vector2.zero; } catch (Exception) { }
            }
            RenderSettings.sun = _moon;   // URP の主光 = この月 (P12 が今の月を止めた後も、残っていても、こちらが主光)

            BuildLamp(d);

            // 逆光 (点光源): 坑口の奥の脈の青緑。影は PC だけ
            var B = d.Backlight;
            if (B.On)
            {
                var bgo = new GameObject("HD2D-Backlight");
                bgo.transform.SetParent(_rig, false);
                bgo.transform.position = PathToWorld(d.PathYaw, B.T, B.S, B.Y);
                _backlight = bgo.AddComponent<Light>();
                _backlight.type = LightType.Point;
                _backlight.range = Mathf.Max(0.1f, B.Range);
                _backlight.color = B.Color;
                _backlight.intensity = Mathf.Max(0f, B.Illum * B.IllumDist * B.IllumDist);
                SetupShadows(_backlight, B, B.Shadow ? B.Shadows : LightShadows.None);
            }
            EnforceShadowCap(d);
        }

        /// <summary>
        /// 舞台の灯 (スポット): 座席の帯の真ん中を狙い、帯の四隅がクッキー (円錐) の半径 bandFill に来る角度にする。
        /// flatten なら、クッキーに「帯の地面でいちばん暗い所 ÷ その点」を焼いて明るさをそろえ、強さはいちばん暗い所が illum になる値。
        /// 向き = キャラのキー (KeyDir)
        /// </summary>
        static void BuildLamp(StageLookData d)
        {
            var L = d.Lamp;
            float yaw = d.PathYaw;
            Vector3 aim = PathToWorld(yaw, (L.TMin + L.TMax) * 0.5f, (L.SMin + L.SMax) * 0.5f, L.AimY);
            Vector3 pos = aim + L.Offset;
            Vector3 fwd = aim - pos;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.down;
            fwd.Normalize();
            var go = new GameObject("HD2D-StageLamp");
            go.transform.SetParent(_rig, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(fwd, Mathf.Abs(fwd.y) > 0.999f ? Vector3.forward : Vector3.up);
            Vector3 R = go.transform.right, U = go.transform.up;

            float maxTan = 0f, maxDist = 0f;
            var ts = new[] { L.TMin, L.TMax };
            var ss = new[] { L.SMin, L.SMax };
            foreach (var t in ts)
                foreach (var s in ss)
                {
                    Vector3 v = PathToWorld(yaw, t, s, 0f) - pos;
                    float z = Vector3.Dot(v, fwd);
                    if (z <= 1e-3f) continue;
                    float x = Vector3.Dot(v, R) / z, y = Vector3.Dot(v, U) / z;
                    maxTan = Mathf.Max(maxTan, Mathf.Sqrt(x * x + y * y));
                    maxDist = Mathf.Max(maxDist, v.magnitude);
                }
            float fill = Mathf.Clamp(L.BandFill, 0.05f, 1f);
            float spot = Mathf.Clamp(2f * Mathf.Atan(Mathf.Max(0.02f, maxTan / fill)) * Mathf.Rad2Deg, 1f, 179f);
            float tanHalf = Mathf.Tan(spot * 0.5f * Mathf.Deg2Rad);
            float range = Mathf.Max(1f, Mathf.Max(maxDist, (aim - pos).magnitude) * Mathf.Max(1f, L.RangeScale));

            _lamp = go.AddComponent<Light>();
            _lamp.type = LightType.Spot;
            _lamp.spotAngle = spot;
            _lamp.innerSpotAngle = spot * Mathf.Clamp01(L.InnerRatio);
            _lamp.range = range;
            _lamp.color = L.Color;
            SetupShadows(_lamp, L, L.Shadows);

            int size = ClampSize(d.Cookie.Size);
            double eMin, eMax; int bandN;
            var band = new double[] { L.TMin, L.TMax, L.SMin, L.SMax };
            // 座席の帯の地面は高さ0の平ら (計画 §2-2)。狙いの高さ aimY とは別
            var E = LampGroundLight(size, V(pos), V(fwd), V(R), V(U), tanHalf, 0.0, range, yaw, band, out eMin, out eMax, out bandN);
            double eAim = PointGroundLight(V(pos), new double[] { aim.x, 0.0, aim.z }, range);
            bool flatten = L.Flatten && L.Cookie && bandN > 0 && eMin > 0.0;
            double eRef = flatten ? eMin : eAim;
            _lamp.intensity = (float)(Math.Max(0.0, L.Illum) / Math.Max(1e-9, eRef));
            _lampBasis = null;
            if (L.Cookie)
            {
                var pat = CookiePattern(size, d.Cookie.Seed, d.Cookie.Blobs, d.Cookie.CenterRadius, d.Cookie.CenterContrast, d.Cookie.EdgeContrast, d.Cookie.Softness);
                var gain = flatten ? FlattenGain(E, eMin, L.FlattenFloor) : null;
                // 灯の形 (W3 P22): 座席の帯 ∪ 中央の光の池 の外を暗く。帯の t は今の座席に合わせて LookDriver が焼き直す (灯の位置と向きは変えない = キャラのキーは同じ)
                if (L.MaskOn)
                {
                    var bas = new double[size * size];
                    for (int i = 0; i < bas.Length; i++) bas[i] = pat[i] * (gain != null ? gain[i] : 1.0);
                    _lampBasis = new LampBasis { Size = size, Pos = V(pos), Fwd = V(fwd), Right = V(R), Up = V(U), TanHalf = tanHalf, Yaw = yaw, Base = bas };
                    if (!_maskFitted) { _maskTMin = L.TMin; _maskTMax = L.TMax; }
                    _lampCookie = MakeCookieTexture("HD2D-LampCookie", size, bas, LampMaskFor(d, _maskTMin, _maskTMax), TextureWrapMode.Clamp);
                }
                else _lampCookie = MakeCookieTexture("HD2D-LampCookie", size, pat, gain, TextureWrapMode.Clamp);
                _lamp.cookie = _lampCookie;
            }
            KeyDir = fwd;
            double evenBefore = eMax > 0.0 ? eMin / eMax : 0.0;
            double evenAfter = !flatten ? evenBefore : eMin / Math.Max(eMin, Math.Max(0.0, Math.Min(1.0, L.FlattenFloor)) * eMax);
            _lampStats = new Dictionary<string, object>
            {
                { "aim", aim }, { "position", pos }, { "spotAngle", spot }, { "range", range }, { "intensity", _lamp.intensity },
                { "bandTexels", bandN }, { "bandLightMin", eMin }, { "bandLightMax", eMax },
                { "bandEvenness", evenAfter }, { "bandEvennessBeforeFlatten", evenBefore }, { "flatten", flatten },
            };
            UpdateMaskStats(d);
        }

        // ---------------------------------------------------------------- 灯の形 (W3 P22)

        /// <summary>クッキーを焼き直すための灯の基底 (位置・向き・円錐・木漏れ日×打ち消しの倍率)。BuildLamp が作る</summary>
        sealed class LampBasis
        {
            public int Size;
            public double[] Pos, Fwd, Right, Up, Base;
            public double TanHalf;
            public float Yaw;
        }

        static LampBasis _lampBasis;
        static readonly Dictionary<string, float> _fitNow = new Dictionary<string, float>();
        static float _maskTMin = -6.5f, _maskTMax = 12.5f;
        static bool _maskFitted;
        static int _maskBakes;

        /// <summary>帯 (t = tMin〜tMax・s = 設計図の SMin〜SMax) と中央の光の池からクッキーの形 (0〜1) を作る</summary>
        static double[] LampMaskFor(StageLookData d, float tMin, float tMax)
        {
            var L = d.Lamp; var b = _lampBasis;
            if (b == null) return null;
            return LampGroundMask(b.Size, b.Pos, b.Fwd, b.Right, b.Up, b.TanHalf, 0.0, b.Yaw,
                new double[] { tMin, tMax, L.SMin, L.SMax }, new double[] { L.MaskMargin.x, L.MaskMargin.y, L.MaskMarginFront >= 0f ? L.MaskMarginFront : L.MaskMargin.y }, L.MaskSoft, L.MaskPower, L.MaskOutside,
                L.PoolOn ? new double[] { L.PoolT, L.PoolS, L.PoolRT, L.PoolRS, L.PoolShear, L.PoolLevel, L.PoolSoft } : null);
        }

        static void UpdateMaskStats(StageLookData d)
        {
            if (_lampStats == null || d == null) return;
            var L = d.Lamp;
            _lampStats["mask"] = new Dictionary<string, object>
            {
                { "on", L.MaskOn && _lampBasis != null }, { "tMin", _maskTMin }, { "tMax", _maskTMax }, { "fitted", _maskFitted },
                { "sMin", L.SMin }, { "sMax", L.SMax }, { "margin", L.MaskMargin }, { "outside", L.MaskOutside }, { "pool", L.PoolOn }, { "bakes", _maskBakes },
            };
        }

        static readonly Dictionary<string, float> _fitSeats = new Dictionary<string, float>();

        /// <summary>
        /// 帯の t を今の座席に合わせる (LookDriver が毎フレーム呼ぶ。変わった時だけクッキーを焼き直す = 新しい戦闘・召喚の時)。
        /// 座席 = 板が居る (Stage.TryGetUnitBox) 主人公と敵 (enemy0〜7) の座席の足元 (Stage.TryGetSeat = 組み直しの置き場。演出の踏み込みでは動かない)。
        /// 合わせ直すのは「居る座席の置き場が変わった・新しい座席が増えた」時だけ。倒れて居なくなっただけなら縮めない (撃破の最中に灯が動かない)
        /// </summary>
        internal static void RefitLamp()
        {
            var d = Current;
            if (!Active || d == null || _lamp == null || _lampBasis == null || _lampCookie == null) return;
            var L = d.Lamp;
            if (!L.MaskOn || !L.FitSeats) return;
            float lo = float.MaxValue, hi = float.MinValue;
            int n = 0;
            bool changed = !_maskFitted;
            float yaw = d.PathYaw * Mathf.Deg2Rad, c = Mathf.Cos(yaw), s = Mathf.Sin(yaw);
            _fitNow.Clear();
            for (int i = -1; i < 8; i++)
            {
                string key = i < 0 ? "player" : "enemy" + i;
                Vector3 feet, seat; float h, k;
                if (!Stage.TryGetUnitBox(key, out feet, out h)) continue;
                if (Stage.TryGetSeat(key, out seat, out k)) feet = seat;
                float t = c * feet.x - s * feet.z;   // 世界 → 道の t (LampGroundLight と同じ式)
                _fitNow[key] = t;
                float old;
                if (!_fitSeats.TryGetValue(key, out old) || Mathf.Abs(old - t) > 0.1f) changed = true;
                if (t < lo) lo = t;
                if (t > hi) hi = t;
                n++;
            }
            if (n == 0 || !changed) return;
            _fitSeats.Clear();
            foreach (var kv in _fitNow) _fitSeats[kv.Key] = kv.Value;
            float tMin = lo - Mathf.Max(0f, L.FitPadLeft), tMax = hi + Mathf.Max(0f, L.FitPadRight);
            float span = Mathf.Max(1f, L.FitMinSpan);
            if (tMax - tMin < span) { float m = 0.5f * (tMin + tMax); tMin = m - 0.5f * span; tMax = m + 0.5f * span; }
            tMin = Mathf.Clamp(tMin, L.TMin - 4f, L.TMax + 4f);
            tMax = Mathf.Clamp(tMax, tMin + 1f, L.TMax + 4f);
            if (_maskFitted && Mathf.Abs(tMin - _maskTMin) < 0.25f && Mathf.Abs(tMax - _maskTMax) < 0.25f) return;
            _maskTMin = tMin; _maskTMax = tMax; _maskFitted = true;
            var mask = LampMaskFor(d, tMin, tMax);
            if (mask == null) return;
            var bas = _lampBasis.Base;
            int size = _lampBasis.Size;
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                double v = bas[i] * mask[i];
                byte b = (byte)Math.Round(255.0 * Math.Max(0.0, Math.Min(1.0, v)));
                px[i] = new Color32(b, b, b, 255);
            }
            _lampCookie.SetPixels32(px);
            _lampCookie.Apply(true, false);
            _maskBakes++;
            UpdateMaskStats(d);
            Debug.Log("[StageLook] 灯の形を座席に合わせた t " + tMin.ToString("0.0", CultureInfo.InvariantCulture) + "〜" + tMax.ToString("0.0", CultureInfo.InvariantCulture) + " (座席 " + n + ")");
        }

        /// <summary>光の一式の根に付く見張り: 帯の t を座席に合わせる (StageLook は静的なので毎フレームの口をここに持つ)</summary>
        sealed class LookDriver : MonoBehaviour
        {
            void LateUpdate() { RefitLamp(); }
        }

        static void SetupShadows(Light l, StageLookData.LightLook s, LightShadows shadows)
        {
            l.shadows = shadows;
            l.shadowStrength = Mathf.Clamp01(s.ShadowStrength);
            l.shadowBias = s.ShadowBias;
            l.shadowNormalBias = s.ShadowNormalBias;
            try
            {
                var ad = l.GetUniversalAdditionalLightData();
                ad.usePipelineSettings = false;   // 光ごとの影のずらし (bias) を使う
                ad.customShadowLayers = true;     // 照らす層と影を落とす層を分ける (月 = 地形と大物だけ・舞台の灯 = キャラだけ)
                ad.renderingLayers = s.LightLayers;
                ad.shadowRenderingLayers = s.ShadowLayers;
                if (Application.isPlaying && l.type != LightType.Directional) ad.additionalLightsShadowResolutionTier = s.ShadowTier;
            }
            catch (Exception e) { Debug.LogWarning("[StageLook] " + l.name + " の URP の光の設定に失敗: " + e.Message); }
        }

        /// <summary>影を落とす光を上限 (shadow.maxShadowedLights) までにする (多ければ 逆光 → 舞台の灯 → 月 の順に影を切る)</summary>
        static void EnforceShadowCap(StageLookData d)
        {
            int cap = Mathf.Max(0, d.Shadow.MaxShadowedLights);
            var order = new[] { _backlight, _lamp, _moon };
            int n = RigShadowCount();
            foreach (var l in order)
            {
                if (n <= cap) break;
                if (l == null || l.shadows == LightShadows.None) continue;
                l.shadows = LightShadows.None; n--;
                Debug.LogWarning("[StageLook] 影を落とす光が上限 " + cap + " を超えるので " + l.name + " の影を切った");
            }
        }

        static int RigShadowCount()
        {
            int n = 0;
            if (_moon != null && _moon.enabled && _moon.shadows != LightShadows.None) n++;
            if (_lamp != null && _lamp.enabled && _lamp.shadows != LightShadows.None) n++;
            if (_backlight != null && _backlight.enabled && _backlight.shadows != LightShadows.None) n++;
            return n;
        }

        static void DestroyRig()
        {
            // 主光 (RenderSettings.sun) は、作り直しなら BuildRig が新しい月を、Restore なら控えが戻す
            foreach (var l in new[] { _moon, _lamp, _backlight }) if (l != null) l.enabled = false;   // Destroy はフレームの終わり = 同じフレームに光が二重にならないよう先に消す
            if (_rig != null) Kill(_rig.gameObject);
            _rig = null; _moon = null; _lamp = null; _backlight = null;
            if (_lampCookie != null) Kill(_lampCookie);
            if (_moonCookie != null) Kill(_moonCookie);
            _lampCookie = null; _moonCookie = null;
            _lampStats = null;
            _borrowed = 0; _backlightLent = false;
        }

        static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o);
        }

        static Texture2D MakeCookieTexture(string name, int size, double[] pattern, double[] gain, TextureWrapMode wrap)
        {
            // linear:true (値は明るさの倍率。Linear でも sRGB として読まれない)。CPU の SetPixels32 で埋める (CopyTexture は使わない)
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { name = name, wrapMode = wrap, filterMode = FilterMode.Bilinear, anisoLevel = 1 };
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                double v = pattern[i] * (gain != null ? gain[i] : 1.0);
                byte b = (byte)Math.Round(255.0 * Math.Max(0.0, Math.Min(1.0, v)));
                px[i] = new Color32(b, b, b, 255);
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            return tex;
        }

        static int ClampSize(int s) { return Mathf.Clamp(s, 16, 1024); }

        static double[] V(Vector3 v) { return new double[] { v.x, v.y, v.z }; }

        /// <summary>道の座標 (t, s) と高さ y を世界へ (Diorama.OnPath と同じ式。Build の前にも使えるよう道の向きは設計図から)</summary>
        static Vector3 PathToWorld(float yaw, float t, float s, float y) { return Quaternion.Euler(0f, yaw, 0f) * new Vector3(t, y, s); }

        /// <summary>カメラの距離 (Stage の _dist): 世界の原点は視線に垂直な基準の面の上にあるので、原点までの視線方向の距離 = _dist</summary>
        static float CameraDistanceOf(Camera cam)
        {
            float def = Current != null ? Current.BaseCameraDistance : 16.62f;
            if (cam == null) return def;
            float d = Vector3.Dot(-cam.transform.position, cam.transform.forward);
            return d > 0.5f ? d : def;
        }

        // ---------------------------------------------------------------- 環境光・霧・影・カメラ

        static void ApplyAmbient(StageLookData d)
        {
            var a = d.Ambient;
            if (string.Equals(a.Mode, "flat", StringComparison.OrdinalIgnoreCase))
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = a.Sky;
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = a.Sky;
                RenderSettings.ambientEquatorColor = a.Equator;
                RenderSettings.ambientGroundColor = a.Ground;
            }
            UpdateEnvironment();
        }

        static void UpdateEnvironment()
        {
            try { DynamicGI.UpdateEnvironment(); } catch (Exception) { }   // 環境光の SH を今の色で作り直す (Trilight はふつう自動。念のため)
        }

        static void ApplyFogColor(StageLookData d)
        {
            RenderSettings.fog = d.Fog.On;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = d.Fog.Color;
        }

        static void ApplyShadowSettings(StageLookData d)
        {
            var urp = BackedUrp();
            if (urp == null) return;
            var s = d.Shadow;
            try { urp.shadowCascadeCount = Mathf.Clamp(s.Cascades, 1, 4); } catch (Exception e) { Debug.LogWarning("[StageLook] カスケードの数を書けない: " + e.Message); }
            urp.cascade2Split = Mathf.Clamp(s.Cascade2Split, 0.01f, 0.99f);   // 座席の深さからの分割は P10 が上書きする
            urp.mainLightShadowmapResolution = s.MainResolution;
            urp.additionalLightsShadowmapResolution = s.AdditionalResolution;
            if (!urp.supportsAdditionalLightShadows) Debug.LogWarning("[StageLook] URP のアセット " + urp.name + " は追加ライトの影が無効 = 舞台の灯と逆光の影が出ない (prep の前)");
            if (!urp.useRenderingLayers) Debug.LogWarning("[StageLook] URP のアセット " + urp.name + " は Rendering Layers が無効 = 光ごとの影のレイヤーが効かない (prep の前)");
        }

        static void ApplyCamera(Camera cam, StageLookData d)
        {
            if (cam == null) return;
            if (d.Cam.HasBackground) cam.backgroundColor = d.Cam.Background;
            ApplyMsaa(cam, d);
            if (d.Cam.DepthTexture)
            {
                try { cam.GetUniversalAdditionalCameraData().requiresDepthTexture = true; }   // ぼかし (TiltShift) が深度を読む
                catch (Exception e) { Debug.LogWarning("[StageLook] 深度テクスチャを要求できない: " + e.Message); }
            }
        }

        /// <summary>MSAA: 旗 aa=msaa の時だけ。標本数 = HD2DFlags.MsaaSamples と設計図の msaaMax の小さい方 (1・2・4・8)。それ以外は 1 (切る)</summary>
        static void ApplyMsaa(Camera cam, StageLookData d)
        {
            int n = 1;
            if (HD2DFlags.Aa == HD2DAa.Msaa) n = Mathf.Min(HD2DFlags.MsaaSamples, Mathf.Max(1, d.Cam.MsaaMax));
            n = n >= 8 ? 8 : n >= 4 ? 4 : n >= 2 ? 2 : 1;
            var urp = BackedUrp();
            if (urp != null) urp.msaaSampleCount = n;
            if (cam != null) cam.allowMSAA = n > 1;
        }

        static void ApplyTiltShift(StageLookData d)
        {
            var t = d.Dof;
            // uionly・unitsonly の撮影の間は切ったまま (Autopilot が撮る前に Enabled を控えて false にしている。組み直しや旗の通知で書き直すと撮影の間に戻ってしまう = P07 の申し送り。W2 P12 の直し)
            TiltShiftSettings.Enabled = HD2DFlags.TiltShift == HD2DTiltShift.On && !HD2DFlags.UiOnly && !HD2DFlags.UnitsOnly;
            TiltShiftSettings.UseUrpBokeh = HD2DFlags.TiltShift == HD2DTiltShift.Urp;
            TiltShiftSettings.RampNear = Mathf.Max(0.01f, t.RampNear);
            TiltShiftSettings.RampFar = Mathf.Max(0.01f, t.RampFar);
            TiltShiftSettings.MaxPxPC = Mathf.Max(0f, t.MaxPxPC);
            TiltShiftSettings.MaxPxPhone = Mathf.Max(0f, t.MaxPxPhone);
            TiltShiftSettings.HalfRes = t.HalfRes;
            float near, far;
            Stage.SeatDepthRange(out near, out far);
            if (far > near && near > 0f) SetSeatBand(near, far);
            else { _seatBandSet = false; ApplyFallbackBand(); }
        }

        /// <summary>座席の帯がまだ無い時 (P10 の前): 帯 = カメラの距離 + fallbackBandRel</summary>
        static void ApplyFallbackBand()
        {
            var d = Current;
            if (d == null) return;
            float c = _camDist > 0.01f ? _camDist : d.BaseCameraDistance;
            TiltShiftSettings.BandNear = Mathf.Max(0.01f, c + d.Dof.FallbackBandRel.x);
            TiltShiftSettings.BandFar = Mathf.Max(TiltShiftSettings.BandNear + 0.01f, c + d.Dof.FallbackBandRel.y);
            SyncUrpFocus();
        }

        /// <summary>dof=urp (逃げ道) の時: URP の Bokeh の焦点を座席の帯の真ん中に</summary>
        static void SyncUrpFocus()
        {
            if (_bakVol == null || _bakVol.Profile == null || !TiltShiftSettings.UseUrpBokeh) return;
            DepthOfField dof;
            if (_bakVol.Profile.TryGet(out dof) && dof != null) dof.focusDistance.Override(0.5f * (TiltShiftSettings.BandNear + TiltShiftSettings.BandFar));
        }

        // ---------------------------------------------------------------- 後処理 (Volume)

        static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
        {
            T c;
            if (p.TryGet(out c) && c != null) return c;
            c = p.Add<T>(false);
            if (_bakVol != null && !_bakVol.Added.Contains(typeof(T))) _bakVol.Added.Add(typeof(T));
            return c;
        }

        static void ApplyVolume(VolumeProfile profile, StageLookData d)
        {
            if (profile == null || _bakVol == null || _bakVol.Profile != profile) return;
            var P = d.Post;
            string tm = (P.Tonemap ?? "aces").Trim().ToLowerInvariant();
            var tone = GetOrAdd<Tonemapping>(profile);
            tone.active = true;
            tone.mode.Override(tm == "neutral" ? TonemappingMode.Neutral : tm == "none" ? TonemappingMode.None : TonemappingMode.ACES);

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.Override(P.BloomThreshold);
            bloom.intensity.Override(P.BloomIntensity);
            bloom.scatter.Override(P.BloomScatter);
            bloom.tint.Override(P.BloomTint);

            var vig = GetOrAdd<Vignette>(profile);
            vig.active = true;
            vig.intensity.Override(P.VignetteIntensity);
            vig.smoothness.Override(P.VignetteSmoothness);
            vig.center.Override(P.VignetteCenter);
            vig.rounded.Override(P.VignetteRounded);
            vig.color.Override(P.VignetteColor);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.active = true;
            color.postExposure.Override(P.Exposure);
            color.contrast.Override(P.Contrast);
            color.saturation.Override(P.Saturation);
            color.colorFilter.Override(P.ColorFilter);

            var lgg = GetOrAdd<LiftGammaGain>(profile);
            lgg.active = true;
            lgg.lift.Override(P.Lift);
            lgg.gamma.Override(P.Gamma);
            lgg.gain.Override(P.Gain);

            FilmGrain grain;
            if (profile.TryGet(out grain) && grain != null) grain.active = P.FilmGrain;
            else if (P.FilmGrain) GetOrAdd<FilmGrain>(profile).active = true;
            ChromaticAberration ca;
            if (profile.TryGet(out ca) && ca != null) ca.active = P.ChromaticAberration;
            else if (P.ChromaticAberration) GetOrAdd<ChromaticAberration>(profile).active = true;

            // 被写界深度: 自作のティルトシフト (dof=1) の時と dof=0 の時は URP の Bokeh を切る。dof=urp の時だけ使う (焦点は座席の帯の真ん中)
            DepthOfField dof;
            if (HD2DFlags.TiltShift == HD2DTiltShift.Urp)
            {
                dof = GetOrAdd<DepthOfField>(profile);
                dof.active = true;
                dof.mode.Override(DepthOfFieldMode.Bokeh);
                dof.focalLength.Override(d.Dof.FocalLength);
                dof.aperture.Override(d.Dof.Aperture);
                dof.bladeCount.Override(d.Dof.BladeCount);
                SyncUrpFocus();
            }
            else if (profile.TryGet(out dof) && dof != null) dof.active = false;
        }

        // ---------------------------------------------------------------- キャラの全体値・高さの霧

        static void ApplyCharGlobals(StageLookData d)
        {
            EnsureIds();
            var key = HD2DFlags.KeyColor == HD2DKeyColor.Warm ? d.Char.KeyWarm : d.Char.Key;
            key.a = 1f;
            KeyColor = key;
            Vector3 k = KeyDir;
            Shader.SetGlobalVector(_idKeyDir, new Vector4(k.x, k.y, k.z, 0f));
            // キーは明るさ×色 (HDR 可) なので、線形へ直してから強さを掛けて SetGlobalVector で書く (SetGlobalColor は 1 を超える値を線形へ直すと強さが歪む)
            Color kc = QualitySettings.activeColorSpace == ColorSpace.Linear ? key.linear : key;
            float ki = Mathf.Max(0f, d.Char.KeyIntensity);
            Shader.SetGlobalVector(_idKeyColor, new Vector4(kc.r * ki, kc.g * ki, kc.b * ki, 1f));
            Shader.SetGlobalColor(_idAmbTop, d.Char.AmbTop);        // 0〜1 の色 = SetGlobalColor (Linear なら自動で線形へ)
            Shader.SetGlobalColor(_idAmbBottom, d.Char.AmbBottom);
        }

        static void ApplyHeightFog(StageLookData d)
        {
            var f = d.Fog;
            Color c = QualitySettings.activeColorSpace == ColorSpace.Linear ? f.HeightColor.linear : f.HeightColor;
            Shader.SetGlobalVector(_idHFogColor, new Vector4(c.r, c.g, c.b, Mathf.Max(0f, f.HeightDensity)));
            Shader.SetGlobalVector(_idHFogRange, new Vector4(f.HeightBase, Mathf.Max(f.HeightBase + 0.01f, f.HeightTop), f.HeightOn ? 1f : 0f, 0f));
            float r = DistanceScale > 0.01f ? DistanceScale : 1f;
            Shader.SetGlobalVector(_idHFogDepth, new Vector4(f.HeightDepthStart * r, Mathf.Max(f.HeightDepthStart + 0.01f, f.HeightDepthFull) * r, 0f, 0f));
            // 霧の光の芯 (W3 P22): 坑口の奥の脈の方を向く霧ほど明るく、外れるほど暗い。StageModule の霧の色と StageShaft の光の面 (_LobeFloor) が読む
            if (f.LobeOn && f.LobePower > 0f)
            {
                Vector3 lp = PathToWorld(d.PathYaw, f.LobeT, f.LobeS, f.LobeY);
                Color lc = QualitySettings.activeColorSpace == ColorSpace.Linear ? f.LobeColor.linear : f.LobeColor;
                float ls = Mathf.Max(0f, f.LobeStrength);
                Shader.SetGlobalVector(_idLobePos, new Vector4(lp.x, lp.y, lp.z, f.LobePower));
                Shader.SetGlobalVector(_idLobeColor, new Vector4(lc.r * ls, lc.g * ls, lc.b * ls, Mathf.Clamp01(f.LobeEdge)));
            }
            else
            {
                Shader.SetGlobalVector(_idLobePos, Vector4.zero);
                Shader.SetGlobalVector(_idLobeColor, Vector4.zero);
            }
        }

        /// <summary>全体値を「未設定」(0) に戻す = シェーダは場の主光と環境光 (SH) に戻る</summary>
        static void ClearGlobals()
        {
            Shader.SetGlobalVector(_idKeyDir, Vector4.zero);
            Shader.SetGlobalVector(_idKeyColor, Vector4.zero);
            Shader.SetGlobalVector(_idAmbTop, Vector4.zero);
            Shader.SetGlobalVector(_idAmbBottom, Vector4.zero);
            Shader.SetGlobalVector(_idHFogColor, Vector4.zero);
            Shader.SetGlobalVector(_idHFogRange, Vector4.zero);
            Shader.SetGlobalVector(_idHFogDepth, Vector4.zero);
            Shader.SetGlobalVector(_idLobePos, Vector4.zero);
            Shader.SetGlobalVector(_idLobeColor, Vector4.zero);
            Shader.SetGlobalVector(_idEnvGrade, Vector4.zero);
        }

        /// <summary>
        /// 舞台の色の寄せ (2026-09-30 W3 の統合。ユーザー「本家の色彩も参考にしてほしい」→ docs/design/hd2d-slice/honke-color.md)。
        /// look の "envGrade": { "desaturate": 0〜1 (舞台の絵の彩度を落とす量), "tint": [r, g, b] (舞台の絵の色に掛ける倍率。線形) } を
        /// 全体値 _HD2DEnvGrade に書く。StageModule (地形・3Dの部品・半立体・札) の絵の色にだけ掛かり、キャラの板 (StageUnitLit) には掛からない
        /// = 本家の「舞台は1つの色相に沈め、キャラは舞台の色の外にいる」。書いていなければ 0 (= そのまま)
        /// </summary>
        static void ApplyEnvGrade(StageLookData d)
        {
            var eg = d.Raw != null ? d.Raw["envGrade"] as JObject : null;
            _envGrade = Vector4.zero;
            if (eg != null && B(eg, "on", true))
            {
                Vector3 t = V3(eg, "tint", Vector3.one);
                _envGrade = new Vector4(t.x - 1f, t.y - 1f, t.z - 1f, Mathf.Clamp01(F(eg, "desaturate", 0f)));
            }
            Shader.SetGlobalVector(_idEnvGrade, _envGrade);
        }
        static Vector4 _envGrade;
        /// <summary>光の面の設計図の色と強さ (上書きの前。上書きの無い look に替わった時に戻す)</summary>
        static readonly Dictionary<Material, KeyValuePair<Color, float>> _matOrig = new Dictionary<Material, KeyValuePair<Color, float>>();

        // ---------------------------------------------------------------- 記録

        static Dictionary<string, object> LightInfo(Light l)
        {
            var o = new Dictionary<string, object>
            {
                { "name", l.name }, { "type", l.type.ToString() }, { "enabled", l.enabled && l.gameObject.activeInHierarchy },
                { "color", l.color }, { "intensity", l.intensity }, { "shadows", l.shadows.ToString() },
                { "position", l.transform.position }, { "forward", l.transform.forward },
            };
            if (l.type != LightType.Directional) o["range"] = l.range;
            if (l.type == LightType.Spot) { o["spotAngle"] = l.spotAngle; o["innerSpotAngle"] = l.innerSpotAngle; }
            if (l.cookie != null) o["cookie"] = l.cookie.name;
            try
            {
                var ad = l.GetUniversalAdditionalLightData();
                uint lightMask = ad.renderingLayers, shadowMask = ad.shadowRenderingLayers;
                o["renderingLayers"] = lightMask;
                o["shadowRenderingLayers"] = shadowMask;
            }
            catch (Exception) { }
            return o;
        }

        static string Summary()
        {
            var d = Current;
            if (d == null) return "(設計図なし)";
            var sb = new System.Text.StringBuilder();
            sb.Append("Apply ").Append(d.Name).Append(" [").Append(string.Join("+", d.Sources.ToArray())).Append("]");
            sb.Append(" r=").Append(DistanceScale.ToString("0.00", CultureInfo.InvariantCulture));
            foreach (var l in new[] { _moon, _lamp, _backlight })
            {
                if (l == null) continue;
                sb.Append(" | ").Append(l.name).Append(' ').Append(l.type.ToString()).Append(" i=").Append(l.intensity.ToString("0.###", CultureInfo.InvariantCulture));
                if (l.type == LightType.Spot) sb.Append(" 角").Append(l.spotAngle.ToString("0.#", CultureInfo.InvariantCulture)).Append('°');
                sb.Append(" 影=").Append(l.shadows.ToString());
            }
            sb.Append(" | 影 ").Append(RigShadowCount()).Append('/').Append(d.Shadow.MaxShadowedLights);
            if (_lampStats != null) sb.Append(" | 帯の明るさのそろい ").Append(Convert.ToDouble(_lampStats["bandEvenness"], CultureInfo.InvariantCulture).ToString("0.00", CultureInfo.InvariantCulture));
            if (_lampBasis != null) sb.Append(" | 灯の形 t ").Append(_maskTMin.ToString("0.0", CultureInfo.InvariantCulture)).Append('〜').Append(_maskTMax.ToString("0.0", CultureInfo.InvariantCulture)).Append(d.Lamp.PoolOn ? "+池" : "");
            if (d.Fog.LobeOn) sb.Append(" | 霧の芯 ").Append(d.Fog.LobePower.ToString("0", CultureInfo.InvariantCulture)).Append("乗");
            if (_envGrade != Vector4.zero) sb.Append(" | 舞台の色 彩度−").Append(_envGrade.w.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(" 倍率 ").Append((1f + _envGrade.x).ToString("0.00", CultureInfo.InvariantCulture)).Append(',').Append((1f + _envGrade.y).ToString("0.00", CultureInfo.InvariantCulture)).Append(',').Append((1f + _envGrade.z).ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(" | ぼかし ").Append(TiltShiftSettings.Enabled ? "自作" : TiltShiftSettings.UseUrpBokeh ? "URP" : "なし");
            sb.Append(" 帯 ").Append(TiltShiftSettings.BandNear.ToString("0.0", CultureInfo.InvariantCulture)).Append('〜').Append(TiltShiftSettings.BandFar.ToString("0.0", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        // ==== json-begin (scratchpad の検査がここから json-end までを取り出して .NET で読む。Unity の API は Debug.LogWarning と ColorUtility だけ)

        /// <summary>設計図のファイル (名前, JSON の文字列) を順に重ねて読む。オブジェクトはキーごとに混ぜる・配列と数は置き換える・null は飛ばす。読めない JSON は警告して飛ばす</summary>
        internal static StageLookData FromLayers(int act, IList<KeyValuePair<string, string>> layers)
        {
            var merged = new JObject();
            var used = new List<string>();
            var ms = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Ignore };
            if (layers != null)
                foreach (var kv in layers)
                {
                    JObject o;
                    try { o = JObject.Parse(kv.Value ?? ""); }
                    catch (Exception e) { Debug.LogWarning("[StageLook] " + kv.Key + " の JSON が読めない (飛ばす): " + e.Message); continue; }
                    merged.Merge(o, ms);
                    used.Add(kv.Key);
                }
            var d = FromJson(merged, act);
            d.Sources.AddRange(used);
            return d;
        }

        /// <summary>重ねた後の JSON から設計図を作る (書いていないキーは既定の値のまま)</summary>
        internal static StageLookData FromJson(JObject o, int act)
        {
            var d = new StageLookData { Act = act, Raw = o };
            if (o == null) return d;
            d.PathYaw = F(o, "pathYaw", d.PathYaw);
            d.BaseCameraDistance = Math.Max(0.01f, F(o, "baseCameraDistance", d.BaseCameraDistance));

            var m = Obj(o, "moon");
            if (m != null)
            {
                ReadLight(m, d.Moon);
                d.Moon.Euler = V3(m, "euler", d.Moon.Euler);
                d.Moon.Intensity = F(m, "intensity", d.Moon.Intensity);
                d.Moon.Cookie = B(m, "cookie", d.Moon.Cookie);
                d.Moon.CookieSize = V2(m, "cookieSize", d.Moon.CookieSize);
            }

            var l = Obj(o, "lamp");
            if (l != null)
            {
                var L = d.Lamp;
                ReadLight(l, L);
                L.Illum = F(l, "illum", L.Illum);
                L.Offset = V3(l, "offset", L.Offset);
                var band = Obj(l, "band");
                if (band != null)
                {
                    L.TMin = F(band, "tMin", L.TMin); L.TMax = F(band, "tMax", L.TMax);
                    L.SMin = F(band, "sMin", L.SMin); L.SMax = F(band, "sMax", L.SMax);
                }
                L.AimY = F(l, "aimY", L.AimY);
                L.BandFill = F(l, "bandFill", L.BandFill);
                L.InnerRatio = F(l, "innerRatio", L.InnerRatio);
                L.RangeScale = F(l, "rangeScale", L.RangeScale);
                L.Flatten = B(l, "flatten", L.Flatten);
                L.FlattenFloor = F(l, "flattenFloor", L.FlattenFloor);
                L.Cookie = B(l, "cookie", L.Cookie);
                var mk = Obj(l, "mask");
                if (mk != null)
                {
                    L.MaskOn = B(mk, "on", L.MaskOn);
                    L.MaskMargin = V2(mk, "margin", L.MaskMargin);
                    L.MaskMarginFront = F(mk, "marginFront", L.MaskMarginFront);
                    L.MaskSoft = F(mk, "soft", L.MaskSoft);
                    L.MaskPower = F(mk, "power", L.MaskPower);
                    L.MaskOutside = F(mk, "outside", L.MaskOutside);
                }
                var pl = Obj(l, "pool");
                if (pl != null)
                {
                    L.PoolOn = B(pl, "on", L.PoolOn);
                    L.PoolT = F(pl, "t", L.PoolT); L.PoolS = F(pl, "s", L.PoolS);
                    L.PoolRT = F(pl, "rt", L.PoolRT); L.PoolRS = F(pl, "rs", L.PoolRS);
                    L.PoolShear = F(pl, "shear", L.PoolShear);
                    L.PoolLevel = F(pl, "level", L.PoolLevel);
                    L.PoolSoft = F(pl, "soft", L.PoolSoft);
                }
                var ft = Obj(l, "fit");
                if (ft != null)
                {
                    L.FitSeats = B(ft, "on", L.FitSeats);
                    L.FitPadLeft = F(ft, "padLeft", L.FitPadLeft);
                    L.FitPadRight = F(ft, "padRight", L.FitPadRight);
                    L.FitMinSpan = F(ft, "minSpan", L.FitMinSpan);
                }
            }

            var b = Obj(o, "backlight");
            if (b != null)
            {
                var BL = d.Backlight;
                ReadLight(b, BL);
                BL.On = B(b, "on", BL.On);
                BL.T = F(b, "t", BL.T); BL.S = F(b, "s", BL.S); BL.Y = F(b, "y", BL.Y);
                BL.Illum = F(b, "illum", BL.Illum);
                BL.IllumDist = F(b, "illumDist", BL.IllumDist);
                BL.Range = F(b, "range", BL.Range);
                BL.Shadow = B(b, "shadow", BL.Shadow);
            }

            var c = Obj(o, "cookie");
            if (c != null)
            {
                var C = d.Cookie;
                C.Size = I(c, "size", C.Size); C.Seed = I(c, "seed", C.Seed); C.Blobs = I(c, "blobs", C.Blobs);
                C.CenterRadius = F(c, "centerRadius", C.CenterRadius);
                C.CenterContrast = F(c, "centerContrast", C.CenterContrast);
                C.EdgeContrast = F(c, "edgeContrast", C.EdgeContrast);
                C.Softness = F(c, "softness", C.Softness);
            }

            var a = Obj(o, "ambient");
            if (a != null)
            {
                d.Ambient.Mode = S(a, "mode", d.Ambient.Mode);
                d.Ambient.Sky = Col(a, "sky", d.Ambient.Sky);
                d.Ambient.Equator = Col(a, "equator", d.Ambient.Equator);
                d.Ambient.Ground = Col(a, "ground", d.Ambient.Ground);
            }

            var f = Obj(o, "fog");
            if (f != null)
            {
                var FG = d.Fog;
                FG.On = B(f, "on", FG.On);
                FG.Color = Col(f, "color", FG.Color);
                FG.Start = F(f, "start", FG.Start);
                FG.End = F(f, "end", FG.End);
                var h = Obj(f, "height");
                if (h != null)
                {
                    FG.HeightOn = B(h, "on", FG.HeightOn);
                    FG.HeightColor = Col(h, "color", FG.HeightColor);
                    FG.HeightBase = F(h, "base", FG.HeightBase);
                    FG.HeightTop = F(h, "top", FG.HeightTop);
                    FG.HeightDensity = F(h, "density", FG.HeightDensity);
                    FG.HeightDepthStart = F(h, "depthStart", FG.HeightDepthStart);
                    FG.HeightDepthFull = F(h, "depthFull", FG.HeightDepthFull);
                }
                var lb = Obj(f, "lobe");
                if (lb != null)
                {
                    FG.LobeOn = B(lb, "on", FG.LobeOn);
                    FG.LobeT = F(lb, "t", FG.LobeT); FG.LobeS = F(lb, "s", FG.LobeS); FG.LobeY = F(lb, "y", FG.LobeY);
                    FG.LobePower = F(lb, "power", FG.LobePower);
                    FG.LobeEdge = F(lb, "edge", FG.LobeEdge);
                    FG.LobeColor = Col(lb, "color", FG.LobeColor);
                    FG.LobeStrength = F(lb, "strength", FG.LobeStrength);
                }
            }

            var sh = Obj(o, "shadow");
            if (sh != null)
            {
                var SH = d.Shadow;
                SH.Distance = F(sh, "distance", SH.Distance);
                SH.Cascades = I(sh, "cascades", SH.Cascades);
                SH.Cascade2Split = F(sh, "cascade2Split", SH.Cascade2Split);
                SH.MainResolution = I(sh, "mainResolution", SH.MainResolution);
                SH.AdditionalResolution = I(sh, "additionalResolution", SH.AdditionalResolution);
                SH.MaxShadowedLights = I(sh, "maxShadowedLights", SH.MaxShadowedLights);
            }

            var p = Obj(o, "post");
            if (p != null)
            {
                var P = d.Post;
                P.Tonemap = S(p, "tonemap", P.Tonemap);
                P.Exposure = F(p, "exposure", P.Exposure);
                P.Contrast = F(p, "contrast", P.Contrast);
                P.Saturation = F(p, "saturation", P.Saturation);
                P.ColorFilter = Col(p, "colorFilter", P.ColorFilter);
                var g = Obj(p, "liftGammaGain");
                if (g != null) { P.Lift = V4(g, "lift", P.Lift); P.Gamma = V4(g, "gamma", P.Gamma); P.Gain = V4(g, "gain", P.Gain); }
                var bl = Obj(p, "bloom");
                if (bl != null)
                {
                    P.BloomThreshold = F(bl, "threshold", P.BloomThreshold);
                    P.BloomIntensity = F(bl, "intensity", P.BloomIntensity);
                    P.BloomScatter = F(bl, "scatter", P.BloomScatter);
                    P.BloomTint = Col(bl, "tint", P.BloomTint);
                }
                var v = Obj(p, "vignette");
                if (v != null)
                {
                    P.VignetteIntensity = F(v, "intensity", P.VignetteIntensity);
                    P.VignetteSmoothness = F(v, "smoothness", P.VignetteSmoothness);
                    P.VignetteCenter = V2(v, "center", P.VignetteCenter);
                    P.VignetteRounded = B(v, "rounded", P.VignetteRounded);
                    P.VignetteColor = Col(v, "color", P.VignetteColor);
                }
                P.FilmGrain = B(p, "filmGrain", P.FilmGrain);
                P.ChromaticAberration = B(p, "chromaticAberration", P.ChromaticAberration);
            }

            var cam = Obj(o, "camera");
            if (cam != null)
            {
                d.Cam.DepthTexture = B(cam, "depthTexture", d.Cam.DepthTexture);
                d.Cam.MsaaMax = I(cam, "msaaMax", d.Cam.MsaaMax);
                var bg = cam["background"];
                if (bg != null && bg.Type == JTokenType.Boolean) d.Cam.HasBackground = (bool)bg;
                else if (bg != null && bg.Type != JTokenType.Null) { d.Cam.Background = Col(cam, "background", d.Cam.Background); d.Cam.HasBackground = true; }
            }

            var ch = Obj(o, "char");
            if (ch != null)
            {
                var CH = d.Char;
                CH.Key = Col(ch, "key", CH.Key);
                CH.KeyWarm = Col(ch, "keyWarm", CH.KeyWarm);
                CH.KeyIntensity = F(ch, "keyIntensity", CH.KeyIntensity);
                CH.AmbTop = Col(ch, "ambTop", CH.AmbTop);
                CH.AmbBottom = Col(ch, "ambBottom", CH.AmbBottom);
                CH.AmbientScale = F(ch, "ambientScale", CH.AmbientScale);
                CH.Receive = F(ch, "receive", CH.Receive);
                CH.HeroReceive = F(ch, "heroReceive", CH.HeroReceive);
                CH.HeroLift = F(ch, "heroLift", CH.HeroLift);
            }

            var tsh = Obj(o, "tiltShift");
            if (tsh != null)
            {
                var DF = d.Dof;
                DF.BandMargin = F(tsh, "bandMargin", DF.BandMargin);
                DF.RampNear = F(tsh, "rampNear", DF.RampNear);
                DF.RampFar = F(tsh, "rampFar", DF.RampFar);
                DF.MaxPxPC = F(tsh, "maxPxPC", DF.MaxPxPC);
                DF.MaxPxPhone = F(tsh, "maxPxPhone", DF.MaxPxPhone);
                DF.HalfRes = B(tsh, "halfRes", DF.HalfRes);
                DF.FallbackBandRel = V2(tsh, "fallbackBandRel", DF.FallbackBandRel);
            }
            var bk = Obj(o, "urpBokeh");
            if (bk != null)
            {
                d.Dof.FocalLength = F(bk, "focalLength", d.Dof.FocalLength);
                d.Dof.Aperture = F(bk, "aperture", d.Dof.Aperture);
                d.Dof.BladeCount = I(bk, "bladeCount", d.Dof.BladeCount);
            }

            var hl = Obj(o, "hitLight");
            if (hl != null)
            {
                d.Hit.MaxLights = I(hl, "maxLights", d.Hit.MaxLights);
                d.Hit.Range = F(hl, "range", d.Hit.Range);
                d.Hit.RefDist = F(hl, "refDist", d.Hit.RefDist);
                d.Hit.ShadowTier = ParseTier(hl["shadowTier"], d.Hit.ShadowTier);
            }

            var mats = Obj(o, "materials");
            if (mats != null)
                foreach (var prop in mats.Properties())
                {
                    if (prop.Name.StartsWith("_", StringComparison.Ordinal)) continue;
                    var mo = prop.Value as JObject;
                    if (mo == null) continue;
                    var ml = new StageLookData.MaterialLook { Receive = F(mo, "receive", -1f), ShadowStrength = F(mo, "shadowStrength", -1f), Intensity = F(mo, "intensity", -1f) };
                    if (mo["tint"] != null) ml.Tint = Col(mo, "tint", Color.white);
                    d.Materials[prop.Name] = ml;
                }
            return d;
        }

        static void ReadLight(JObject o, StageLookData.LightLook l)
        {
            l.Color = Col(o, "color", l.Color);
            l.Shadows = ParseShadows(o["shadows"], l.Shadows);
            l.ShadowStrength = F(o, "shadowStrength", l.ShadowStrength);
            l.ShadowBias = F(o, "shadowBias", l.ShadowBias);
            l.ShadowNormalBias = F(o, "shadowNormalBias", l.ShadowNormalBias);
            l.LightLayers = ParseLayers(o["lightLayers"], l.LightLayers);
            l.ShadowLayers = ParseLayers(o["shadowLayers"], l.ShadowLayers);
            l.ShadowTier = ParseTier(o["shadowTier"], l.ShadowTier);
        }

        static JObject Obj(JObject o, string k) { return o != null ? o[k] as JObject : null; }

        static bool IsNum(JToken t) { return t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer); }

        static float F(JObject o, string k, float def)
        {
            var t = o != null ? o[k] : null;
            if (IsNum(t)) return (float)t;
            float f;
            if (t != null && t.Type == JTokenType.String && float.TryParse((string)t, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
            if (t != null && t.Type != JTokenType.Null) Debug.LogWarning("[StageLook] 数でない: " + k + "=" + t.ToString());
            return def;
        }

        static int I(JObject o, string k, int def)
        {
            var t = o != null ? o[k] : null;
            if (IsNum(t)) return (int)Math.Round((double)t);
            return def;
        }

        static bool B(JObject o, string k, bool def)
        {
            var t = o != null ? o[k] : null;
            if (t == null) return def;
            if (t.Type == JTokenType.Boolean) return (bool)t;
            if (IsNum(t)) return (double)t != 0.0;
            if (t.Type == JTokenType.String)
            {
                string s = ((string)t).Trim().ToLowerInvariant();
                if (s == "1" || s == "true" || s == "on" || s == "yes") return true;
                if (s == "0" || s == "false" || s == "off" || s == "no") return false;
            }
            return def;
        }

        static string S(JObject o, string k, string def)
        {
            var t = o != null ? o[k] : null;
            return t != null && t.Type == JTokenType.String ? (string)t : def;
        }

        static float[] Nums(JToken t)
        {
            var arr = t as JArray;
            if (arr == null) return null;
            var r = new float[arr.Count];
            for (int i = 0; i < arr.Count; i++) { if (!IsNum(arr[i])) return null; r[i] = (float)arr[i]; }
            return r;
        }

        /// <summary>色: [r, g, b] か [r, g, b, a] (0〜1) か "#rrggbb"</summary>
        static Color Col(JObject o, string k, Color def)
        {
            var t = o != null ? o[k] : null;
            if (t == null || t.Type == JTokenType.Null) return def;
            if (t.Type == JTokenType.String)
            {
                Color c;
                if (ColorUtility.TryParseHtmlString((string)t, out c)) return c;
                Debug.LogWarning("[StageLook] 色が読めない: " + k + "=" + (string)t);
                return def;
            }
            var n = Nums(t);
            if (n != null && n.Length >= 3) return new Color(n[0], n[1], n[2], n.Length >= 4 ? n[3] : 1f);
            Debug.LogWarning("[StageLook] 色が読めない: " + k + "=" + t.ToString());
            return def;
        }

        static Vector2 V2(JObject o, string k, Vector2 def) { var n = Nums(o != null ? o[k] : null); return n != null && n.Length >= 2 ? new Vector2(n[0], n[1]) : def; }
        static Vector3 V3(JObject o, string k, Vector3 def) { var n = Nums(o != null ? o[k] : null); return n != null && n.Length >= 3 ? new Vector3(n[0], n[1], n[2]) : def; }
        static Vector4 V4(JObject o, string k, Vector4 def) { var n = Nums(o != null ? o[k] : null); return n != null && n.Length >= 4 ? new Vector4(n[0], n[1], n[2], n[3]) : def; }

        /// <summary>
        /// Rendering Layers: "all"・"none"・"default" (bit0)・"characters" (bit1)・"environment" (bit2)・"all-but-characters"、
        /// それらを「+」「,」「|」でつないだ物、名前の配列、数 (そのまま)
        /// </summary>
        internal static uint ParseLayers(JToken t, uint def)
        {
            if (t == null || t.Type == JTokenType.Null) return def;
            if (t.Type == JTokenType.Integer) return unchecked((uint)(long)t);
            if (t.Type == JTokenType.Array) { uint acc = 0; foreach (var x in t) acc |= ParseLayers(x, 0u); return acc; }
            if (t.Type != JTokenType.String) return def;
            uint mask = 0; bool any = false;
            foreach (var raw in ((string)t).Split('+', ',', '|', ' '))
            {
                string w = raw.Trim().ToLowerInvariant();
                if (w.Length == 0) continue;
                switch (w)
                {
                    case "all": case "everything": mask |= 0xFFFFFFFFu; any = true; break;
                    case "none": case "nothing": any = true; break;
                    case "default": mask |= 1u; any = true; break;
                    case "characters": case "character": case "chars": mask |= 1u << 1; any = true; break;   // HD2DLayers.RenderingCharacters
                    case "environment": case "env": mask |= 1u << 2; any = true; break;                     // HD2DLayers.RenderingEnvironment
                    case "all-but-characters": mask |= ~(1u << 1); any = true; break;
                    default: Debug.LogWarning("[StageLook] 知らない Rendering Layer: " + w); break;
                }
            }
            return any ? mask : def;
        }

        internal static LightShadows ParseShadows(JToken t, LightShadows def)
        {
            if (t == null || t.Type == JTokenType.Null) return def;
            if (t.Type == JTokenType.Boolean) return (bool)t ? LightShadows.Soft : LightShadows.None;
            if (t.Type != JTokenType.String) return def;
            switch (((string)t).Trim().ToLowerInvariant())
            {
                case "none": case "off": case "false": return LightShadows.None;
                case "hard": return LightShadows.Hard;
                case "soft": case "on": case "true": return LightShadows.Soft;
                default: return def;
            }
        }

        /// <summary>追加ライトの影の解像度の段: "low" 0・"medium" 1・"high" 2・"custom" -1、か数</summary>
        internal static int ParseTier(JToken t, int def)
        {
            if (t == null || t.Type == JTokenType.Null) return def;
            if (t.Type == JTokenType.Integer) return Math.Max(-1, Math.Min(2, (int)t));
            if (t.Type != JTokenType.String) return def;
            switch (((string)t).Trim().ToLowerInvariant())
            {
                case "low": return 0;
                case "medium": return 1;
                case "high": return 2;
                case "custom": return -1;
                default: return def;
            }
        }
        // ==== json-end

        // ==== pure-begin (scratchpad の検査がここから pure-end までを取り出して .NET で回す。System の型だけ)

        /// <summary>
        /// 木漏れ日の模様 (size×size・行 j=0 が下・値 0〜1)。円 (半径1 = 円錐の縁) の中央 (半径 centerRadius の内) はコントラスト centerContrast 以下、
        /// 外は edgeContrast。暗い形は blobs 個の不定形 (楕円の縁を3倍と5倍の波で崩す)。中央の外の環 (centerRadius〜centerRadius+0.3) に等分に置く。
        /// 同じ引数なら同じ値 (System.Random の種)
        /// </summary>
        internal static double[] CookiePattern(int size, int seed, int blobs, double centerRadius, double centerContrast, double edgeContrast, double softness)
        {
            var rnd = new System.Random(seed);
            int k = Math.Max(0, Math.Min(12, blobs));
            var cx = new double[k]; var cy = new double[k]; var ra = new double[k]; var rb = new double[k];
            var cs = new double[k]; var sn = new double[k]; var h1 = new double[k]; var p1 = new double[k]; var h2 = new double[k]; var p2 = new double[k];
            double start = rnd.NextDouble() * Math.PI * 2.0;
            for (int b = 0; b < k; b++)
            {
                double ang = start + Math.PI * 2.0 * b / k + (rnd.NextDouble() - 0.5) * (Math.PI / k);
                double rho = centerRadius + 0.30 * rnd.NextDouble();
                cx[b] = Math.Cos(ang) * rho; cy[b] = Math.Sin(ang) * rho;
                ra[b] = 0.22 + 0.16 * rnd.NextDouble();
                rb[b] = ra[b] * (0.55 + 0.35 * rnd.NextDouble());
                double phi = rnd.NextDouble() * Math.PI;
                cs[b] = Math.Cos(phi); sn[b] = Math.Sin(phi);
                h1[b] = 0.12 + 0.10 * rnd.NextDouble(); p1[b] = rnd.NextDouble() * Math.PI * 2.0;
                h2[b] = 0.05 + 0.07 * rnd.NextDouble(); p2[b] = rnd.NextDouble() * Math.PI * 2.0;
            }
            double soft = Math.Max(0.01, Math.Min(0.9, softness));
            double cc = Math.Max(0.0, Math.Min(1.0, centerContrast)), ec = Math.Max(0.0, Math.Min(1.0, edgeContrast));
            var v = new double[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    double x = (i + 0.5) / size * 2.0 - 1.0, y = (j + 0.5) / size * 2.0 - 1.0;
                    double r = Math.Sqrt(x * x + y * y);
                    double dark = 0.0;
                    for (int b = 0; b < k; b++)
                    {
                        double dx = x - cx[b], dy = y - cy[b];
                        double lx = dx * cs[b] + dy * sn[b], ly = -dx * sn[b] + dy * cs[b];
                        double q = Math.Sqrt((lx / ra[b]) * (lx / ra[b]) + (ly / rb[b]) * (ly / rb[b]));
                        double th = Math.Atan2(ly, lx);
                        double edge = 1.0 + h1[b] * Math.Sin(3.0 * th + p1[b]) + h2[b] * Math.Sin(5.0 * th + p2[b]);
                        double dk = 1.0 - SmoothStep(1.0 - soft, 1.0, q / edge);
                        if (dk > dark) dark = dk;
                    }
                    double c = cc + (ec - cc) * SmoothStep(centerRadius, centerRadius + 0.14, r);
                    v[j * size + i] = 1.0 - c * dark;
                }
            return v;
        }

        /// <summary>
        /// 舞台の灯 (スポット) が地面 (高さ groundY) を照らす明るさを、クッキーの画素ごとに (行 j=0 が下)。URP の点光源・スポットと同じ
        /// 「入射角 × 距離の縁のなじみ (1 − (d²/range²)²)² ÷ 距離²」。円錐の外・地面に当たらない画素は 0。
        /// 画素 (u, v) の向き = fwd + right×(2u−1)×tanHalf + up×(2v−1)×tanHalf (URP のスポットのクッキーの写し方)。
        /// 帯 (道の座標 band = tMin, tMax, sMin, sMax。道の向き yawDeg) に当たる画素の最小・最大と数も返す
        /// </summary>
        internal static double[] LampGroundLight(int size, double[] pos, double[] fwd, double[] right, double[] up, double tanHalf,
            double groundY, double range, double yawDeg, double[] band, out double eMin, out double eMax, out int bandTexels)
        {
            var e = new double[size * size];
            eMin = double.MaxValue; eMax = 0.0; bandTexels = 0;
            double yaw = yawDeg * Math.PI / 180.0, cyw = Math.Cos(yaw), syw = Math.Sin(yaw);
            double invR2 = range > 0.0 ? 1.0 / (range * range) : 0.0;
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    double x = (i + 0.5) / size * 2.0 - 1.0, y = (j + 0.5) / size * 2.0 - 1.0;
                    if (x * x + y * y > 1.0) continue;   // 円錐の外
                    double dx = fwd[0] + (right[0] * x + up[0] * y) * tanHalf;
                    double dy = fwd[1] + (right[1] * x + up[1] * y) * tanHalf;
                    double dz = fwd[2] + (right[2] * x + up[2] * y) * tanHalf;
                    double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    dx /= len; dy /= len; dz /= len;
                    if (dy > -1e-4) continue;             // 地面に向かわない
                    double t = (groundY - pos[1]) / dy;
                    if (t <= 0.0) continue;
                    double s2 = t * t * invR2;
                    double sm = 1.0 - s2 * s2;
                    if (sm <= 0.0) continue;              // 届かない
                    sm *= sm;
                    double lit = (-dy) * sm / (t * t);
                    e[j * size + i] = lit;
                    double hx = pos[0] + dx * t, hz = pos[2] + dz * t;
                    double pt = cyw * hx - syw * hz, ps = syw * hx + cyw * hz;   // 世界 → 道の座標 (−yaw だけ回す)
                    if (pt >= band[0] && pt <= band[1] && ps >= band[2] && ps <= band[3])
                    {
                        bandTexels++;
                        if (lit < eMin) eMin = lit;
                        if (lit > eMax) eMax = lit;
                    }
                }
            if (bandTexels == 0) eMin = 0.0;
            return e;
        }

        /// <summary>点 target (地面) を pos の光が照らす明るさ (上と同じ式。target の面は上向き)</summary>
        internal static double PointGroundLight(double[] pos, double[] target, double range)
        {
            double dx = target[0] - pos[0], dy = target[1] - pos[1], dz = target[2] - pos[2];
            double d2 = dx * dx + dy * dy + dz * dz;
            if (d2 <= 1e-9) return 0.0;
            double d = Math.Sqrt(d2);
            double cosI = Math.Max(0.0, -dy / d);
            double s2 = range > 0.0 ? d2 / (range * range) : 0.0;
            double sm = Math.Max(0.0, 1.0 - s2 * s2); sm *= sm;
            return cosI * sm / d2;
        }

        /// <summary>明るさをそろえる倍率: 明るい所ほど削って eMin に合わせる (floor より下げない)。当たらない画素は 1</summary>
        internal static double[] FlattenGain(double[] e, double eMin, double floor)
        {
            var g = new double[e.Length];
            double fl = Math.Max(0.0, Math.Min(1.0, floor));
            for (int i = 0; i < e.Length; i++) g[i] = e[i] > 0.0 ? Math.Max(fl, Math.Min(1.0, eMin / e[i])) : 1.0;
            return g;
        }

        /// <summary>
        /// 灯の形 (W3 P22。クッキーの画素ごとの倍率 0〜1・行 j=0 が下)。画素の向きの光線が地面 (高さ groundY) に当たる点を道の座標 (t, s) にし、
        /// 帯 band = (tMin, tMax, sMin, sMax) を余白 margin = (t, s の奥[, s の手前]) だけ広げた「角の丸い四角」(|dt|^p + |ds|^p ≦ 1。p = power) の中なら 1、
        /// 縁の外 soft (半径に対する割合) でなめらかに outside へ落とす。pool = (t, s, rt, rs, shear, level, soft) があれば中央の光の池 (楕円・中心の t は s とともに shear ずれる) を
        /// level まで足す (大きい方)。円錐の外・地面に向かわない画素は outside。壁に当たる光線も地面の点で決まる (壁の奥の地面 = 帯の外なら暗い)
        /// </summary>
        internal static double[] LampGroundMask(int size, double[] pos, double[] fwd, double[] right, double[] up, double tanHalf,
            double groundY, double yawDeg, double[] band, double[] margin, double soft, double power, double outside, double[] pool)
        {
            var m = new double[size * size];
            double yaw = yawDeg * Math.PI / 180.0, cyw = Math.Cos(yaw), syw = Math.Sin(yaw);
            double tc = 0.5 * (band[0] + band[1]), sc = 0.5 * (band[2] + band[3]);
            double rt = Math.Max(0.1, 0.5 * (band[1] - band[0]) + Math.Max(0.0, margin[0]));
            double rs = Math.Max(0.1, 0.5 * (band[3] - band[2]) + Math.Max(0.0, margin[1]));   // 奥 (s ≧ 帯の真ん中)
            double rsF = margin.Length >= 3 ? Math.Max(0.1, 0.5 * (band[3] - band[2]) + Math.Max(0.0, margin[2])) : rs;   // 手前
            double pw = Math.Max(1.0, power), sf = Math.Max(0.01, soft);
            double outV = Math.Max(0.0, Math.Min(1.0, outside));
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    int k = j * size + i;
                    m[k] = outV;
                    double x = (i + 0.5) / size * 2.0 - 1.0, y = (j + 0.5) / size * 2.0 - 1.0;
                    if (x * x + y * y > 1.0) continue;
                    double dx = fwd[0] + (right[0] * x + up[0] * y) * tanHalf;
                    double dy = fwd[1] + (right[1] * x + up[1] * y) * tanHalf;
                    double dz = fwd[2] + (right[2] * x + up[2] * y) * tanHalf;
                    double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    dx /= len; dy /= len; dz /= len;
                    if (dy > -1e-4) continue;
                    double t = (groundY - pos[1]) / dy;
                    if (t <= 0.0) continue;
                    double hx = pos[0] + dx * t, hz = pos[2] + dz * t;
                    double pt = cyw * hx - syw * hz, ps = syw * hx + cyw * hz;
                    double q = Math.Pow(Math.Pow(Math.Abs(pt - tc) / rt, pw) + Math.Pow(Math.Abs(ps - sc) / (ps >= sc ? rs : rsF), pw), 1.0 / pw);
                    double inside = 1.0 - SmoothStep(1.0 - 0.5 * sf, 1.0 + 0.5 * sf, q);
                    if (pool != null && pool.Length >= 7)
                    {
                        double ct = pool[0] + pool[4] * (ps - pool[1]);
                        double e = Math.Sqrt(Sq((pt - ct) / Math.Max(0.1, pool[2])) + Sq((ps - pool[1]) / Math.Max(0.1, pool[3])));
                        double pv = (1.0 - SmoothStep(1.0 - 0.5 * pool[6], 1.0 + 0.5 * pool[6], e)) * Math.Max(0.0, Math.Min(1.0, pool[5]));
                        if (pv > inside) inside = pv;
                    }
                    m[k] = outV + (1.0 - outV) * inside;
                }
            return m;
        }

        static double Sq(double v) { return v * v; }

        internal static double SmoothStep(double a, double b, double x)
        {
            double t = b > a ? (x - a) / (b - a) : (x >= b ? 1.0 : 0.0);
            t = t < 0.0 ? 0.0 : t > 1.0 ? 1.0 : t;
            return t * t * (3.0 - 2.0 * t);
        }
        // ==== pure-end
    }
}
