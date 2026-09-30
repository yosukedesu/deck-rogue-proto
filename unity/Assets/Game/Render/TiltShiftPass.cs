// TiltShiftPass.cs — ティルトシフト (自作のぼかし) の Render Graph のパス (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-6・P07)。
//
// 何をするか: 座席の帯 (TiltShiftSettings.BandNear〜BandFar・カメラからの距離 unit) ではぼかし 0、帯の外で急に強める。
// 手前は「にじみ出し」(タイルの最大値で半径を広げる) と前景の層の合成を行う。上限は PC 24px・スマホ 12px (1080 基準・画面の高さに比例)。
// シェーダは Resources/Shaders/TiltShift.shader (パスの番号 = ShaderPass)。積むのは TiltShiftHook (舞台のカメラだけ)。
//
// Render Graph の流れ (どれもラスターのパス。テクスチャは全部カメラの色の記述子から作る = XR の次元・スライスを引き継ぐ)
//   0 Prefilter  : 色 (全) ＋ 深さ (全) → work (作業の解像度・RGBA16F・a = 符号つきの錯乱円)
//   1 TileMax    : work → tile (NearDilateTile px 四方の手前の錯乱円の最大)
//   2 TileDilate : tile → tile2 (3×3 の最大)
//   3 Gather     : work ＋ tile2 → far・near (MRT・前乗算)
//   4 Composite  : 色 (全) ＋ 深さ ＋ far ＋ near → dest (カメラの色と同じ記述子) → resourceData.cameraColor を dest に差し替える
// 事象は BeforeRenderingPostProcessing (ブルーム・トーンマップの前 = ぼけた光もブルームで光る)。
// requiresIntermediateTexture = true (バックバッファを直接読めない)・ConfigureInput(Depth) (深さのテクスチャを必ず作らせる)。
// 材質の値は MaterialPropertyBlock でパスごとに渡す (材質の状態をパスの間で共有しない = 記録の後で値が書き換わる事故が無い)。
//
// 既定 (TiltShiftSettings.Enabled = false) では TiltShiftHook が何も積まない = 今の見た目のまま (W1)。
// 帯が未設定 (BandFar ≤ 0) の間は、有効でも何もしない (画面全体が「奥」になって全部ぼけるのを防ぐ)。
//
// P24 (W3) の詰め
//   - ピントの帯を「道に沿った帯」にできる (TiltShiftSettings.Focus = Path。既定)。画素の世界の点の道の座標 s が PathNear〜PathFar ならぼかし 0。
//     座席は道に沿って斜めに並ぶ (左手前の主人公〜右奥の敵) ので、深さの帯 (Depth) だと「左の奥のひな壇は帯の中でくっきり・右の敵のすぐ後ろの地面はぼける」
//     という左右の食い違いが出ていた (W2 のスマホ相当の⑥ 0.23〜0.29 の主因)。道に沿った帯は、焦点面を座席の列に沿って傾けたティルトシフトのレンズと同じ。
//     シェーダには、道の s 軸をカメラの右・上・前へ写した値 (_TS_Plane) と投影の値 (_TS_Proj) を渡し、画素ごとに s = n·C + 深さ × (n·視線) で求める。
//     箱庭が無い・カメラが道とほぼ平行・正射影の時は深さの帯へ戻る (LastFocus に理由)。
//   - 手前の層の輪郭: 自分が手前の画素は、自分の錯乱円の円盤 (NearOwnTaps 点) で「手前の物が占める割合」と「後ろの背景」を取り、
//     α = 占める割合・下地 = 背景の平均にする (前は自分の画素を α=1 にしていたので、細い蔦や羊歯の輪郭がくっきり残っていた = W2 の額縁 frame-4・frame-1)。
//     背景は奥の層のテクスチャ (far) の空いている所 (自分が手前の画素) に α=1 で書き、合成で「元の色」の代わりに使う。
//
// P24 (W3b・2周目「本家っぽく」)
//   - 帯の外のぼけの伸び方をレンズの形にできる (TiltShiftSettings.Curve = Lens。既定)。錯乱円 = 上限 × saturate(強さ × 帯の縁からの深さの差 ÷ 深さ)。
//     本家 (オクトラ1 の夜の森 ot_921570_16・洞窟 _7・村 _11) を同じ物差しで測ると、奥は帯のすぐ後ろがほぼくっきり・奥の木で 2〜3px・いちばん奥は霧で沈む、
//     手前の草で 4〜5px (1080 基準) と弱い。W3 の smoothstep (帯の外 5 unit で上限 24px) は奥が一面の塗りつぶし・手前の額縁が黒い雲になっていた。
//     値 (上限・強さ・点の数) は look_act1.dof.json。W3 の形へ戻すのは look=look_act1_dofw3。シェーダへは _TS_Lens (x = 奥の強さ・y = 手前の強さ。0 = smoothstep)
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DeckRogue.Game
{
    public sealed class TiltShiftPass : ScriptableRenderPass, IDisposable
    {
        // ---------------------------------------------------------------- 詰めの値 (P24 が詰める。値の置き場を TiltShiftSettings へ移してもよい)

        /// <summary>ぼかしの点の数 (ゴールデンアングルの渦。4〜64)。多いほど滑らかで重い (P24: 22 → 32。PC の全解像度 24px で 22 点は粒が見えた。
        /// 2周目: 上限が PC 10〜12px になったので 24。値は TiltShiftLook が設計図から入れる)</summary>
        public static int Taps = 24;
        /// <summary>自分が手前の画素だけ、自分の錯乱円の円盤を取る点の数 (4〜64。手前の輪郭の α と後ろの背景。P24。2周目 24 → 16)</summary>
        public static int NearOwnTaps = 16;
        /// <summary>手前のにじみ出しのタイルの大きさ (作業の解像度の px。4〜64)</summary>
        public static int NearDilateTile = 16;
        /// <summary>点の渦を画素ごとに回す (帯状の縞を細かい粒に変える。粒は画素の位置だけで決まる = 撮影で毎回同じ)</summary>
        public static bool Jitter = true;
        /// <summary>奥の層が拾わない「手前の点」のしきい (中心の錯乱円に対する割合。0 = 何でも拾う・1 = 同じ深さより奥だけ)</summary>
        public static float FarExclude = 0.5f;
        /// <summary>画面の上端の追加のぼけ (上限に対する割合。0 = 無し)・始まる高さ (画面の高さの割合。1 が上)</summary>
        public static float TiltTop = 0f, TiltTopFrom = 0.75f;
        /// <summary>画面の下端の追加のぼけ (手前の側。0 = 無し)・始まる高さ (画面の高さの割合。0 が下)</summary>
        public static float TiltBottom = 0f, TiltBottomFrom = 0.25f;
        /// <summary>調べの表示: 0 = 普通・1 = 錯乱円 (赤 = 手前・青 = 奥)・2 = 奥の層・3 = 手前の層</summary>
        public static int DebugView = 0;

        /// <summary>最後に記録したフレームのピントの帯 ("path" = 道に沿った帯・"depth" = 深さの帯・"depth (理由)" = 道に沿った帯にできなかった)。dumplayout 用</summary>
        public static string LastFocus = "";
        /// <summary>最後に記録したフレームの道の帯の値 (x = n·カメラの右・y = n·カメラの上・z = n·カメラの前・w = カメラの位置の s)。dumplayout 用</summary>
        public static Vector4 LastPlane;
        /// <summary>最後に記録したフレームの上限 (全解像度の px・手前は NearScale を掛ける前)</summary>
        public static float LastMaxPx;
        /// <summary>最後に記録したフレームの帯の外の伸び方 ("lens" / "smooth")。dumplayout 用 (P24 2周目)</summary>
        public static string LastCurve = "";

        /// <summary>シェーダ DeckRogue/TiltShift のパスの番号</summary>
        public static class ShaderPass
        {
            public const int Prefilter = 0, TileMax = 1, TileDilate = 2, Gather = 3, Composite = 4, Copy = 5;
        }

        /// <summary>スマホの段 (上限は MaxPxPhone)。積む前に TiltShiftHook が入れる</summary>
        public bool Phone { get; set; }

        static class Ids
        {
            public static readonly int BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int Source = Shader.PropertyToID("_TS_Source");
            public static readonly int Depth = Shader.PropertyToID("_TS_Depth");
            public static readonly int Work = Shader.PropertyToID("_TS_Work");
            public static readonly int Tile = Shader.PropertyToID("_TS_Tile");
            public static readonly int Far = Shader.PropertyToID("_TS_Far");
            public static readonly int Near = Shader.PropertyToID("_TS_Near");
            public static readonly int FullSize = Shader.PropertyToID("_TS_FullSize");
            public static readonly int WorkSize = Shader.PropertyToID("_TS_WorkSize");
            public static readonly int TileSize = Shader.PropertyToID("_TS_TileSize");
            public static readonly int DepthScale = Shader.PropertyToID("_TS_DepthScale");
            public static readonly int Band = Shader.PropertyToID("_TS_Band");
            public static readonly int Params = Shader.PropertyToID("_TS_Params");
            public static readonly int Tilt = Shader.PropertyToID("_TS_Tilt");
            public static readonly int Misc = Shader.PropertyToID("_TS_Misc");
            public static readonly int Focus = Shader.PropertyToID("_TS_Focus");
            public static readonly int Plane = Shader.PropertyToID("_TS_Plane");
            public static readonly int Proj = Shader.PropertyToID("_TS_Proj");
            public static readonly int Lens = Shader.PropertyToID("_TS_Lens");
        }

        static readonly ProfilingSampler s_Prefilter = new ProfilingSampler("TiltShift Prefilter");
        static readonly ProfilingSampler s_TileMax = new ProfilingSampler("TiltShift TileMax");
        static readonly ProfilingSampler s_TileDilate = new ProfilingSampler("TiltShift TileDilate");
        static readonly ProfilingSampler s_Gather = new ProfilingSampler("TiltShift Gather");
        static readonly ProfilingSampler s_Composite = new ProfilingSampler("TiltShift Composite");
        static readonly Vector4 s_NoScaleBias = new Vector4(1f, 1f, 0f, 0f);

        // 一度だけ出す警告 (毎フレームのログで埋めない)
        static bool s_WarnedShader, s_WarnedBackBuffer, s_WarnedInputs, s_WarnedFormat;

        Material _mat;
        // パスごとの値の入れ物 (記録の時に中身が写される。パスどうしで共有しない)
        readonly MaterialPropertyBlock _mpbPrefilter = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock _mpbTileMax = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock _mpbTileDilate = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock _mpbGather = new MaterialPropertyBlock();
        readonly MaterialPropertyBlock _mpbComposite = new MaterialPropertyBlock();

        public TiltShiftPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);
            profilingSampler = new ProfilingSampler("TiltShift");
        }

        /// <summary>材質を用意する (シェーダが無い・使えない時は false。一度だけ警告)</summary>
        public bool EnsureMaterial()
        {
            if (_mat != null) return true;
            var shader = Shader.Find("DeckRogue/TiltShift");
            if (shader == null || !shader.isSupported)
            {
                if (!s_WarnedShader) { s_WarnedShader = true; Debug.LogWarning("[TiltShift] シェーダ DeckRogue/TiltShift が無いか、この端末で使えない → ぼかしを掛けない"); }
                return false;
            }
            _mat = CoreUtils.CreateEngineMaterial(shader);
            return _mat != null;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(_mat);
            _mat = null;
        }

        // ---------------------------------------------------------------- 1フレームの定数

        struct Consts
        {
            public Vector4 fullSize, workSize, tileSize, band, param, tilt, misc, focus, plane, proj, lens;
        }

        static void SetConsts(MaterialPropertyBlock mpb, in Consts c)
        {
            mpb.SetVector(Ids.BlitScaleBias, s_NoScaleBias);   // Blit.hlsl の Vert の uv = 0〜1 そのまま
            mpb.SetVector(Ids.FullSize, c.fullSize);
            mpb.SetVector(Ids.WorkSize, c.workSize);
            mpb.SetVector(Ids.TileSize, c.tileSize);
            mpb.SetVector(Ids.Band, c.band);
            mpb.SetVector(Ids.Params, c.param);
            mpb.SetVector(Ids.Tilt, c.tilt);
            mpb.SetVector(Ids.Misc, c.misc);
            mpb.SetVector(Ids.Focus, c.focus);
            mpb.SetVector(Ids.Plane, c.plane);
            mpb.SetVector(Ids.Proj, c.proj);
            mpb.SetVector(Ids.Lens, c.lens);
        }

        /// <summary>
        /// 道に沿った帯の値 (P24)。道の s 軸 n (箱庭の根のローカル = 世界。Diorama.OnPath) を、描画のカメラ (揺れ・寄りを含む実際のカメラ) の
        /// 右・上・前へ写す: plane = (n·右, n·上, n·前, n·(カメラの位置 − 根の原点))。画素の視線 r = ((ndc.x + m02)/m00, (ndc.y + m12)/m11, 1) (カメラの空間・+z が前) で
        /// s = plane.w + 深さ × (plane.x r.x + plane.y r.y + plane.z)。proj = (1/m00, 1/m11, m02, m12) (投影は GL の形 = 上下の反転を含まない)。
        /// 返り値 false = 深さの帯へ戻す (why に理由)
        /// </summary>
        static bool TryPathPlane(Camera cam, out Vector4 plane, out Vector4 proj, out string why)
        {
            plane = proj = default;
            why = null;
            if (TiltShiftSettings.Focus != TiltShiftFocus.Path) { why = "depth"; return false; }
            if (!Diorama.Active) { why = "depth (箱庭が無い)"; return false; }
            if (cam == null || cam.orthographic) { why = "depth (カメラが無いか正射影)"; return false; }
            Vector3 n = Diorama.OnPath(0f, 1f, 0f) - Diorama.OnPath(0f, 0f, 0f);
            Vector3 origin = Vector3.zero;
            Transform root = Stage.WorldRoot;
            if (root != null) { n = root.TransformDirection(n); origin = root.position; }   // 箱庭は根のローカル (根は原点に置く約束。念のため写す)
            if (!(n.sqrMagnitude > 1e-8f)) { why = "depth (道の向きが無い)"; return false; }
            n.Normalize();
            Matrix4x4 c2w = cam.cameraToWorldMatrix;   // カメラの空間は −z が前 (GL の形)
            Vector3 right = ((Vector3)c2w.GetColumn(0)).normalized;
            Vector3 up = ((Vector3)c2w.GetColumn(1)).normalized;
            Vector3 fwd = -((Vector3)c2w.GetColumn(2)).normalized;
            Vector3 pos = c2w.GetColumn(3);
            Matrix4x4 p = cam.projectionMatrix;
            if (!(Mathf.Abs(p.m00) > 1e-6f) || !(Mathf.Abs(p.m11) > 1e-6f)) { why = "depth (投影が読めない)"; return false; }
            float nf = Vector3.Dot(n, fwd);
            if (!(nf > 0.2f)) { why = "depth (カメラが道とほぼ平行)"; return false; }
            plane = new Vector4(Vector3.Dot(n, right), Vector3.Dot(n, up), nf, Vector3.Dot(n, pos - origin));
            proj = new Vector4(1f / p.m00, 1f / p.m11, p.m02, p.m12);
            return true;
        }

        static Vector4 SizeVec(int w, int h) { return new Vector4(w, h, 1f / w, 1f / h); }

        // 深さのテクスチャの uv の倍率 (RTHandle が拡縮を使う時だけ 1 未満。実行の時にしか分からない)
        static Vector4 DepthScaleOf(TextureHandle depth)
        {
            RTHandle h = depth;
            if (h != null && h.useScaling)
            {
                Vector4 s = h.rtHandleProperties.rtHandleScale;
                return new Vector4(s.x, s.y, 0f, 0f);
            }
            return s_NoScaleBias;
        }

        bool TryMakeConsts(Camera cam, int fullW, int fullH, out Consts c, out int workW, out int workH, out int tileW, out int tileH)
        {
            c = default; workW = workH = tileW = tileH = 0;
            float bandNear = TiltShiftSettings.BandNear, bandFar = TiltShiftSettings.BandFar;
            if (!(bandFar > 0f) || bandFar < bandNear) return false;                     // 帯が未設定 = 何もしない
            float maxPx = Mathf.Max(0f, Phone ? TiltShiftSettings.MaxPxPhone : TiltShiftSettings.MaxPxPC) * (fullH / 1080f);
            LastMaxPx = maxPx;
            if (maxPx < 0.5f) return false;                                              // 上限が 0 = 何もしない
            // ピントの帯の形 (P24): 道に沿った帯にできなければ深さの帯
            bool path = TryPathPlane(cam, out var plane, out var proj, out var why);
            LastFocus = path ? "path" : why;
            LastPlane = plane;
            float pathNear = TiltShiftSettings.PathNear, pathFar = Mathf.Max(TiltShiftSettings.PathNear + 0.01f, TiltShiftSettings.PathFar);
            c.focus = new Vector4(pathNear, pathFar, Mathf.Clamp(TiltShiftSettings.NearScale, 0f, 2f), path ? 1f : 0f);
            c.plane = plane;
            c.proj = proj;
            // 帯の外のぼけの伸び方 (P24 2周目): レンズなら強さ (>0)、smoothstep なら 0 (シェーダが _TS_Band.zw の傾斜を使う)
            bool lens = TiltShiftSettings.Curve == TiltShiftCurve.Lens;
            c.lens = new Vector4(lens ? Mathf.Max(0.01f, TiltShiftSettings.LensFar) : 0f, lens ? Mathf.Max(0.01f, TiltShiftSettings.LensNear) : 0f, 0f, 0f);
            LastCurve = lens ? "lens" : "smooth";
            int scale = TiltShiftSettings.HalfRes ? 2 : 1;
            workW = (fullW + scale - 1) / scale;
            workH = (fullH + scale - 1) / scale;
            int tile = Mathf.Clamp(NearDilateTile, 4, 64);
            tileW = (workW + tile - 1) / tile;
            tileH = (workH + tile - 1) / tile;
            int taps = Mathf.Clamp(Taps, 4, 64);

            c.fullSize = SizeVec(fullW, fullH);
            c.workSize = SizeVec(workW, workH);
            c.tileSize = SizeVec(tileW, tileH);
            c.band = new Vector4(bandNear, bandFar,
                1f / Mathf.Max(TiltShiftSettings.RampNear, 1e-3f),
                1f / Mathf.Max(TiltShiftSettings.RampFar, 1e-3f));
            c.param = new Vector4(maxPx, scale, taps, Jitter ? 1f : 0f);
            c.tilt = new Vector4(Mathf.Clamp01(TiltTop), Mathf.Clamp(TiltTopFrom, 0f, 0.999f),
                                 Mathf.Clamp01(TiltBottom), Mathf.Clamp(TiltBottomFrom, 0.001f, 1f));
            c.misc = new Vector4(tile, Mathf.Clamp01(FarExclude), Mathf.Clamp(DebugView, 0, 3), Mathf.Clamp(NearOwnTaps, 4, 64));
            return true;
        }

        // ---------------------------------------------------------------- テクスチャの記述子

        static readonly GraphicsFormat s_LayerFormat = GraphicsFormat.R16G16B16A16_SFloat;
        static GraphicsFormat s_TileFormat = GraphicsFormat.None;

        static bool FormatsSupported()
        {
            if (s_TileFormat != GraphicsFormat.None) return true;
            if (!SystemInfo.IsFormatSupported(s_LayerFormat, GraphicsFormatUsage.Render)) return false;
            s_TileFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.Render) ? GraphicsFormat.R16_SFloat : s_LayerFormat;
            return true;
        }

        // カメラの色の記述子から、大きさと形式だけを変えた作業用のテクスチャを作る (MSAA・ミップ・動的な拡縮は外す)
        static TextureDesc Derive(in TextureDesc src, int w, int h, GraphicsFormat format, string name, FilterMode filter)
        {
            TextureDesc d = src;
            d.name = name;
            d.sizeMode = TextureSizeMode.Explicit;
            d.width = w;
            d.height = h;
            d.format = format;
            d.msaaSamples = MSAASamples.None;
            d.bindTextureMS = false;
            d.useMipMap = false;
            d.autoGenerateMips = false;
            d.anisoLevel = 0;
            d.enableRandomWrite = false;
            d.useDynamicScale = false;
            d.useDynamicScaleExplicit = false;
            d.isShadowMap = false;
            d.memoryless = RenderTextureMemoryless.None;
            d.filterMode = filter;
            d.wrapMode = TextureWrapMode.Clamp;
            d.clearBuffer = false;
            d.discardBuffer = false;
            return d;
        }

        // ---------------------------------------------------------------- パスの入れ物 (記録のたびに全部の欄を入れ直す)

        class PrefilterData
        {
            public Material material;
            public MaterialPropertyBlock mpb;
            public Consts consts;
            public TextureHandle source;
            public TextureHandle depth;
        }

        class TileMaxData
        {
            public Material material;
            public MaterialPropertyBlock mpb;
            public Consts consts;
            public TextureHandle work;
        }

        class TileDilateData
        {
            public Material material;
            public MaterialPropertyBlock mpb;
            public Consts consts;
            public TextureHandle tile;
        }

        class GatherData
        {
            public Material material;
            public MaterialPropertyBlock mpb;
            public Consts consts;
            public TextureHandle work;
            public TextureHandle tile;
        }

        class CompositeData
        {
            public Material material;
            public MaterialPropertyBlock mpb;
            public Consts consts;
            public TextureHandle source;
            public TextureHandle depth;
            public TextureHandle far;
            public TextureHandle near;
        }

        // ---------------------------------------------------------------- 記録

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_mat == null) return;
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.isSceneViewCamera || cameraData.isPreviewCamera) return;
            if (resourceData.isActiveTargetBackBuffer)
            {
                if (!s_WarnedBackBuffer) { s_WarnedBackBuffer = true; Debug.LogWarning("[TiltShift] 色の書き先がバックバッファ (中間テクスチャが無い) → ぼかしを掛けない"); }
                return;
            }
            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle depth = resourceData.cameraDepthTexture;
            if (!source.IsValid() || !depth.IsValid())
            {
                if (!s_WarnedInputs) { s_WarnedInputs = true; Debug.LogWarning("[TiltShift] カメラの色か深さのテクスチャが無い → ぼかしを掛けない"); }
                return;
            }
            if (!FormatsSupported())
            {
                if (!s_WarnedFormat) { s_WarnedFormat = true; Debug.LogWarning("[TiltShift] RGBA16F に描けない端末 → ぼかしを掛けない"); }
                return;
            }

            TextureDesc srcDesc = source.GetDescriptor(renderGraph);
            int fullW = srcDesc.width, fullH = srcDesc.height;
            if (srcDesc.sizeMode != TextureSizeMode.Explicit || fullW <= 0 || fullH <= 0)
            {
                fullW = cameraData.cameraTargetDescriptor.width;
                fullH = cameraData.cameraTargetDescriptor.height;
            }
            if (fullW < 4 || fullH < 4) return;
            if (!TryMakeConsts(cameraData.camera, fullW, fullH, out var consts, out int workW, out int workH, out int tileW, out int tileH)) return;

            TextureHandle work = renderGraph.CreateTexture(Derive(srcDesc, workW, workH, s_LayerFormat, "TiltShift_Work", FilterMode.Point));
            TextureHandle tile = renderGraph.CreateTexture(Derive(srcDesc, tileW, tileH, s_TileFormat, "TiltShift_Tile", FilterMode.Point));
            TextureHandle tile2 = renderGraph.CreateTexture(Derive(srcDesc, tileW, tileH, s_TileFormat, "TiltShift_TileDilated", FilterMode.Point));
            TextureHandle far = renderGraph.CreateTexture(Derive(srcDesc, workW, workH, s_LayerFormat, "TiltShift_Far", FilterMode.Bilinear));
            TextureHandle near = renderGraph.CreateTexture(Derive(srcDesc, workW, workH, s_LayerFormat, "TiltShift_Near", FilterMode.Bilinear));
            // 書き先はカメラの色と同じ記述子 (MSAA・形式・大きさを保つ = 後ろのパスの深さと組み合わせても食い違わない)
            TextureDesc dstDesc = srcDesc;
            dstDesc.name = "TiltShift_Color";
            dstDesc.clearBuffer = false;
            dstDesc.discardBuffer = false;
            TextureHandle dest = renderGraph.CreateTexture(dstDesc);

            // 0 Prefilter
            using (var builder = renderGraph.AddRasterRenderPass<PrefilterData>("TiltShift Prefilter", out var d, s_Prefilter))
            {
                d.material = _mat;
                d.mpb = _mpbPrefilter;
                d.consts = consts;
                d.source = source;
                d.depth = depth;
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(depth, AccessFlags.Read);
                builder.SetRenderAttachment(work, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PrefilterData data, RasterGraphContext ctx) =>
                {
                    var mpb = data.mpb;
                    mpb.Clear();
                    SetConsts(mpb, data.consts);
                    mpb.SetTexture(Ids.Source, data.source);
                    mpb.SetTexture(Ids.Depth, data.depth);
                    mpb.SetVector(Ids.DepthScale, DepthScaleOf(data.depth));
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, ShaderPass.Prefilter, MeshTopology.Triangles, 3, 1, mpb);
                });
            }

            // 1 TileMax
            using (var builder = renderGraph.AddRasterRenderPass<TileMaxData>("TiltShift TileMax", out var d, s_TileMax))
            {
                d.material = _mat;
                d.mpb = _mpbTileMax;
                d.consts = consts;
                d.work = work;
                builder.UseTexture(work, AccessFlags.Read);
                builder.SetRenderAttachment(tile, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (TileMaxData data, RasterGraphContext ctx) =>
                {
                    var mpb = data.mpb;
                    mpb.Clear();
                    SetConsts(mpb, data.consts);
                    mpb.SetTexture(Ids.Work, data.work);
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, ShaderPass.TileMax, MeshTopology.Triangles, 3, 1, mpb);
                });
            }

            // 2 TileDilate
            using (var builder = renderGraph.AddRasterRenderPass<TileDilateData>("TiltShift TileDilate", out var d, s_TileDilate))
            {
                d.material = _mat;
                d.mpb = _mpbTileDilate;
                d.consts = consts;
                d.tile = tile;
                builder.UseTexture(tile, AccessFlags.Read);
                builder.SetRenderAttachment(tile2, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (TileDilateData data, RasterGraphContext ctx) =>
                {
                    var mpb = data.mpb;
                    mpb.Clear();
                    SetConsts(mpb, data.consts);
                    mpb.SetTexture(Ids.Tile, data.tile);
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, ShaderPass.TileDilate, MeshTopology.Triangles, 3, 1, mpb);
                });
            }

            // 3 Gather (MRT: 0 = 奥・1 = 手前)
            using (var builder = renderGraph.AddRasterRenderPass<GatherData>("TiltShift Gather", out var d, s_Gather))
            {
                d.material = _mat;
                d.mpb = _mpbGather;
                d.consts = consts;
                d.work = work;
                d.tile = tile2;
                builder.UseTexture(work, AccessFlags.Read);
                builder.UseTexture(tile2, AccessFlags.Read);
                builder.SetRenderAttachment(far, 0, AccessFlags.Write);
                builder.SetRenderAttachment(near, 1, AccessFlags.Write);
                builder.SetRenderFunc(static (GatherData data, RasterGraphContext ctx) =>
                {
                    var mpb = data.mpb;
                    mpb.Clear();
                    SetConsts(mpb, data.consts);
                    mpb.SetTexture(Ids.Work, data.work);
                    mpb.SetTexture(Ids.Tile, data.tile);
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, ShaderPass.Gather, MeshTopology.Triangles, 3, 1, mpb);
                });
            }

            // 4 Composite
            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("TiltShift Composite", out var d, s_Composite))
            {
                d.material = _mat;
                d.mpb = _mpbComposite;
                d.consts = consts;
                d.source = source;
                d.depth = depth;
                d.far = far;
                d.near = near;
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(depth, AccessFlags.Read);
                builder.UseTexture(far, AccessFlags.Read);
                builder.UseTexture(near, AccessFlags.Read);
                builder.SetRenderAttachment(dest, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (CompositeData data, RasterGraphContext ctx) =>
                {
                    var mpb = data.mpb;
                    mpb.Clear();
                    SetConsts(mpb, data.consts);
                    mpb.SetTexture(Ids.Source, data.source);
                    mpb.SetTexture(Ids.Depth, data.depth);
                    mpb.SetVector(Ids.DepthScale, DepthScaleOf(data.depth));
                    mpb.SetTexture(Ids.Far, data.far);
                    mpb.SetTexture(Ids.Near, data.near);
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, ShaderPass.Composite, MeshTopology.Triangles, 3, 1, mpb);
                });
            }

            // 以後のパス (URP の後処理) はぼかした色を読む
            resourceData.cameraColor = dest;
        }
    }
}
