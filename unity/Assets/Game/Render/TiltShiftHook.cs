// TiltShiftHook.cs — ティルトシフトのパスを舞台のカメラにだけ積む (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-6・P07)。
//
// URP のレンダラー (Assets/Settings/URP-Renderer.asset) に機能 (ScriptableRendererFeature) を足さず、
// RenderPipelineManager.beginCameraRendering で ScriptableRenderer.EnqueuePass する (設定の資産を触らない・旗で出し入れできる)。
//   - 積むのは TiltShiftSettings.Enabled かつ !UseUrpBokeh の時だけ。既定 (Enabled = false) では何もしない = 今の見た目 (W1)
//   - 積むのは舞台のカメラ (Stage.Camera) だけ。水面の反射のカメラ・シーンビュー・プレビューには積まない
//   - スマホの段 (HD2DFlags.Tier = phone) なら上限は MaxPxPhone
// 場に GameObject は作らない (撮影の粒の並べ直し・dumplayout の走査に何も増やさない)。登録は起動時に1回、終了 (再生の終わり) で外す。
// P24 (W3): 積む直前に TiltShiftLook.Sync (設計図の tiltShiftPass = ピントの帯の形・点の数などの詰めの値) を呼び、
//           dumplayout の extra.tiltShiftPass に読んだ値と実際に使ったピントの帯の形を出す。
//
// 逃げ道 (dof=urp): UseUrpBokeh = true の間は自作のパスを積まず、URP の被写界深度 (Bokeh) を使う。
// Volume の DepthOfField を組む側 (StageLook・P09 / P24) が ConfigureUrpBokeh(dof) を呼ぶと、帯の値から焦点と絞りを入れて有効にする。
// 自作のパスを使う時は、同じ呼び出しが URP の被写界深度を切る (二重にぼかさない)。
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    public static class TiltShiftHook
    {
        static TiltShiftPass s_Pass;
        static bool s_Installed;

        /// <summary>いま積める状態か (有効・自作のパス・帯が設定済み)。調べ物と dumplayout 用</summary>
        public static bool WouldRun =>
            TiltShiftSettings.Enabled && !TiltShiftSettings.UseUrpBokeh && TiltShiftSettings.BandFar > 0f;

        // 再生のたびに静的な状態を初めに戻す (エディタで domain reload を切っていても二重に登録しない)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad()
        {
            Uninstall();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (s_Installed) return;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            Application.quitting += Uninstall;
            HD2DFlags.LayoutDumpers["tiltShiftPass"] = TiltShiftLook.DebugInfo;   // layout.json の extra.tiltShiftPass (P24)
            s_Installed = true;
        }

        static void Uninstall()
        {
            if (s_Installed)
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
                Application.quitting -= Uninstall;
                HD2DFlags.LayoutDumpers.Remove("tiltShiftPass");
                s_Installed = false;
            }
            TiltShiftLook.Invalidate();
            if (s_Pass != null)
            {
                s_Pass.Dispose();
                s_Pass = null;
            }
        }

        static void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (!TiltShiftSettings.Enabled || TiltShiftSettings.UseUrpBokeh) return;
            if (cam == null || cam.cameraType != CameraType.Game) return;
            Camera stageCam = Stage.Camera;
            if (stageCam == null || cam != stageCam) return;
            if (!cam.TryGetComponent<UniversalAdditionalCameraData>(out var camData)) return;
            ScriptableRenderer renderer = camData.scriptableRenderer;
            if (renderer == null) return;
            if (s_Pass == null) s_Pass = new TiltShiftPass();
            if (!s_Pass.EnsureMaterial()) return;
            TiltShiftLook.Sync();   // 詰めの値 (設計図の tiltShiftPass。替わった時だけ読み直す。P24)
            s_Pass.Phone = HD2DFlags.Tier == HD2DTier.Phone;
            renderer.EnqueuePass(s_Pass);
        }

        /// <summary>
        /// 逃げ道 (dof=urp) の URP の被写界深度を帯の値から組む。返り値 = 有効にしたか。
        /// Enabled かつ UseUrpBokeh かつ帯が設定済みの時だけ dof を有効にし、それ以外 (自作のパスを使う時・ぼかし無し) は dof を切る。
        /// 焦点 = 帯の手前と奥の調和平均 (帯の両端のぼけが同じ大きさ)。帯の奥＋奥の傾斜で錯乱円が上限に届くよう絞りを解く (焦点距離 300mm・絞り 1〜32)。
        /// URP の Bokeh の半径の上限は 1080 で約14px (URP の内部の値) なので、自作 (PC 24px) より控えめになる。帯の中も少しぼける (Bokeh に平らな帯は無い)
        /// </summary>
        public static bool ConfigureUrpBokeh(DepthOfField dof)
        {
            if (dof == null) return false;
            bool on = TiltShiftSettings.Enabled && TiltShiftSettings.UseUrpBokeh && TiltShiftSettings.BandFar > 0f;
            dof.active = on;
            if (!on) return false;
            float n = Mathf.Max(0.3f, TiltShiftSettings.BandNear);
            float f = Mathf.Max(n + 0.01f, TiltShiftSettings.BandFar);
            float focus = 2f * n * f / (n + f);
            float z1 = f + Mathf.Max(0.5f, TiltShiftSettings.RampFar);
            // URP: coc = (1 − P/z) × maxCoC、maxCoC = (焦点距離/絞り) × F / (P − F)。z1 で coc = 1 になる maxCoC から絞りを解く
            float maxCoC = 1f / Mathf.Max(1e-3f, 1f - focus / z1);
            const float focalMm = 300f;
            float F = focalMm / 1000f;
            float a = maxCoC * Mathf.Max(focus - F, 1e-3f) / F;
            float aperture = Mathf.Clamp(focalMm / Mathf.Max(a, 1e-3f), 1f, 32f);
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(focus);
            dof.focalLength.Override(focalMm);
            dof.aperture.Override(aperture);
            return true;
        }
    }
}
