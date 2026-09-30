// Diorama.cs — 幕1の3Dの箱庭 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-2)。
// 組み立て器 (P04): 置き場の設計図 Resources/Stage/act<N>_layout.json を読み、実行時に組む。
//  ・形は DioramaMesh (段・岩・幹・櫓・柵・札)・ReliefMesh (半立体)、テクスチャは DioramaTextures (タイルの配列・アトラス)。
//  ・静的な部品は「材質 × 影の有無 × 区画」ごとに1つのメッシュへ溶かし (Renderer を減らす)、実行中なら StaticBatchingUtility.Combine でまとめる。
//    回る滑車・カメラに合わせて置き直す額縁・霧の面と光の筋は、まとめから外して Dynamic に登録する (Tick が回す)。
//  ・座標は道の座標: t = 道に沿った距離・s = 道と直角 (+ が奥)。道の向き PathYaw = −22° (Stage と同じ)。
//  ・Stage の Paint からつなぐのは P12 (W2)。W1 ではどこからも呼ばない = 見た目は1画素も変わらない。
//  ・エディタでは DioramaLayoutTool (メニュー「DeckRogue/箱庭/幕1を組む」「書き戻す」「点検」) が部品を1つずつの GameObject で組む。
// 材質の約束 (StageModule。P03 のシェーダの頭の注記と同じ。合わない所は ApplySurface の1か所で直す):
//  配列の材質: _Albedo/_Normal = Texture2DArray (全部の材質のタイルを1つに積む。どちらも linear:true。色は sRGB のまま入れ _AlbedoDecode=1)、
//   _MatSlices = (側の最初の枚・種の数・側の回し方・天の回し方)、_TopSlice = 天の最初の枚 (種の数は側と同じ。負 = 天も側と同じ = 苔なし)、
//   _NormalArrayOn = 1、_PathYaw = 道の向き (度)、_TexelsPerUnit = 25、_TileTexels = 64。回し方 0 = none・1 = flipX・2 = rot4。
//   頂点色 rgb = AO (_VColorAO で効かせる)、a = 天の材質 (苔) を載せてよい割合 (空き地の土は 0)。
//  メッシュの UV の材質 (_UV_MESH・_MeshArraySlice = −1): _BaseMap (sRGB)。アトラスの半立体と札は _ALPHATEST_ON・_Cutoff も。
//  光の面 (StageShaft): _BaseMap = 光の絵 (霧 = 左半分・筋 = 右半分)、_Tint・_Intensity・_SoftDepth・_NearFade・_EdgeFade・_Fog。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeckRogue.Game
{
    public static class Diorama
    {
        /// <summary>動く物・カメラに合わせて置き直す物の登録 (静的なまとめ StaticBatchingUtility.Combine から外す)。
        /// 櫓の輪 (rig-wheel)・額縁 (frame-N)・霧の面 (fog-N)・光の筋 (shaft-N)。
        /// 回すのは Tick (StageDriver が毎フレーム Diorama.Tick(時刻) を呼ぶ): Spin ≠ 0 の物だけ「基準の回転 × Z 回り (Spin 度/秒)」。
        /// 額縁は OnCameraLayout が置き直すたびに BaseRotation を書き換える (回さない)</summary>
        public struct DynamicEntry
        {
            /// <summary>部品の名前 (例 "rig-wheel"・"frame-0"・"fog-1"・"shaft-2")</summary>
            public string Name;
            public Transform Transform;
            /// <summary>組んだ時の回転 (localRotation。回す時の基準)</summary>
            public Quaternion BaseRotation;
            /// <summary>Z 回り (ローカル) に回す速さ (度/秒)。0 = 回さない</summary>
            public float Spin;
            /// <summary>揺らす幅 (度・Z 回り)。0 = 揺らさない</summary>
            public float Sway;
            /// <summary>揺らす周期 (秒)</summary>
            public float SwayPeriod;
        }

        /// <summary>箱庭が組まれていて、舞台として使われている</summary>
        public static bool Active { get; private set; }

        /// <summary>動く物の登録簿 (Build が積み、Clear が空にする)</summary>
        public static readonly List<DynamicEntry> Dynamic = new List<DynamicEntry>();

        /// <summary>組んだ箱庭の根 (無ければ null)</summary>
        public static Transform Root { get; private set; }

        /// <summary>読んだ設計図 (無ければ null)</summary>
        public static DioramaLayout Layout { get; private set; }

        /// <summary>材質 (設計図の surfaces の名前 → Material)。StageLook (P09/P12) が受光 _Receive などを後から書き換えてよい</summary>
        public static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        /// <summary>最後に組んだ時の点検 (部品数・三角形・Renderer・材質・座席の帯の高さ)</summary>
        public static DioramaStats LastStats { get; private set; }

        /// <summary>Build に渡された幕の設計図 (光の値の読み先。いまの組み立ては使わない)</summary>
        public static StageLookData Look => _look;

        /// <summary>道の向き (度)。Stage.PathYaw と同じ値 (Stage 側は private なので写す)。設計図の pathYaw で上書きされる</summary>
        public const float DefaultPathYaw = -22f;

        static float _pathYaw = DefaultPathYaw;
        static readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        static readonly List<SlabShape> _slabs = new List<SlabShape>();
        static readonly List<FrameSlot> _frames = new List<FrameSlot>();
        static StageLookData _look;
        static bool _hasCam;
        static Vector3 _camPos;
        static Quaternion _camRot = Quaternion.identity;
        static float _camFov = 28f;

        sealed class SlabShape
        {
            public string Name;
            public float[] T, S;
            public float Back, Top;
            public float FrontAt(float t)
            {
                int n = T.Length;
                if (t <= T[0]) return S[0];
                if (t >= T[n - 1]) return S[n - 1];
                int lo = 0, hi = n - 1;
                while (hi - lo > 1) { int mid = (lo + hi) >> 1; if (T[mid] <= t) lo = mid; else hi = mid; }
                float k = (t - T[lo]) / Mathf.Max(1e-5f, T[hi] - T[lo]);
                return Mathf.Lerp(S[lo], S[hi], k);
            }
            public bool Contains(float t, float s) { return t >= T[0] && t <= T[T.Length - 1] && s >= FrontAt(t) - 1e-4f && s <= Back; }
        }

        sealed class FrameSlot
        {
            public Transform Tr;
            public DioramaPart Part;
            public int DynIndex;
        }

        // ================================================================ 道の座標

        /// <summary>道の座標 (t, s) と高さ y を、箱庭の根のローカル (= 世界。根は原点に置く) へ</summary>
        public static Vector3 OnPath(float t, float s, float y)
        {
            return DioramaMesh.YawRotate(_pathYaw, new Vector3(t, y, s));   // = Quaternion.Euler(0, PathYaw, 0) * (t, y, s) (Stage.OnPath と同じ)
        }

        /// <summary>世界の x・z を道の座標へ</summary>
        public static void ToPath(float x, float z, out float t, out float s)
        {
            var l = DioramaMesh.YawRotate(-_pathYaw, new Vector3(x, 0f, z));
            t = l.x; s = l.z;
        }

        // ================================================================ 地面の高さ

        /// <summary>箱庭の地面の高さ (世界の x・z)。座席の帯は 0。設計図の段 (slab) のうち、その点を含むいちばん高い天面。
        /// どの段にも入らなければいちばん低い段の天面。設計図を読んでいなければ 0</summary>
        public static float GroundY(float x, float z)
        {
            if (_slabs.Count == 0) return 0f;
            ToPath(x, z, out var t, out var s);
            return HeightAtPath(t, s);
        }

        /// <summary>道の座標での地面の高さ (GroundY と同じ)</summary>
        public static float HeightAtPath(float t, float s)
        {
            if (_slabs.Count == 0) return 0f;
            float best = float.NegativeInfinity, lowest = float.PositiveInfinity;
            for (int i = 0; i < _slabs.Count; i++)
            {
                var sl = _slabs[i];
                lowest = Mathf.Min(lowest, sl.Top);
                if (sl.Top > best && sl.Contains(t, s)) best = sl.Top;
            }
            return float.IsNegativeInfinity(best) ? lowest : best;
        }

        // ================================================================ 組む

        /// <summary>幕 act の箱庭を parent の下に組む (look = 幕の設計図。いまは使わない = 受光などは StageLook が Materials を書き換える)</summary>
        public static void Build(int act, Transform parent, StageLookData look)
        {
            Build(act, parent, look, null);
        }

        /// <summary>組み方を選べる版 (エディタの道具・点検が使う)。opt = null なら実行時の既定 (溶かしてまとめる)</summary>
        public static void Build(int act, Transform parent, StageLookData look, DioramaBuildOptions opt)
        {
            Clear();
            opt = opt ?? new DioramaBuildOptions();
            _look = look;
            var layout = opt.Layout ?? LoadLayout(act);
            if (layout == null) { Debug.LogWarning("[Diorama] 設計図が無い: Resources/" + LayoutResource(act)); return; }
            PrepareGround(layout);

            var rootGo = new GameObject("diorama-act" + layout.Act);
            rootGo.layer = HD2DLayers.StageSet;
            if (parent != null) rootGo.transform.SetParent(parent, false);
            Root = rootGo.transform;
            var staticRoot = new GameObject("static").transform; staticRoot.SetParent(Root, false); staticRoot.gameObject.layer = HD2DLayers.StageSet;
            var dynRoot = new GameObject("dynamic").transform; dynRoot.SetParent(Root, false); dynRoot.gameObject.layer = HD2DLayers.StageSet;

            var missing = new List<string>();
            var ctx = new BuildContext { Layout = layout, Opt = opt, StaticRoot = staticRoot, DynRoot = dynRoot, Missing = missing };
            MakeTexturesAndMaterials(ctx);

            foreach (var p in layout.Parts)
            {
                try { BuildPart(ctx, p); }
                catch (Exception e) { missing.Add("p" + p.Index + ":" + p.Kind + " 例外 " + e.Message); Debug.LogWarning("[Diorama] 部品 " + p.Index + " (" + p.Kind + ") で例外: " + e); }
            }
            if (opt.Merge) FlushGroups(ctx);

            if (opt.Merge && opt.StaticBatch && Application.isPlaying && layout.StaticBatch)
            {
                try
                {
                    StaticBatchingUtility.Combine(staticRoot.gameObject);
                    // 結合メッシュ (Unity が作る) も自分の物として Clear で捨てる (old ↔ diorama の往復で溜めない)
                    foreach (var mf in staticRoot.GetComponentsInChildren<MeshFilter>(true))
                        if (mf.sharedMesh != null && !_owned.Contains(mf.sharedMesh)) _owned.Add(mf.sharedMesh);
                }
                catch (Exception e) { Debug.LogWarning("[Diorama] StaticBatchingUtility.Combine に失敗: " + e.Message); }
            }
            if (!Application.isPlaying) MarkDontSave(rootGo);

            // 額縁は既定のカメラ (見本の cam=28) で一度置く。実行中は Stage が OnCameraLayout を呼んで置き直す
            if (_hasCam) OnCameraLayout(_camPos, _camRot, _camFov);
            else OnCameraLayout(DefaultCamPos, Quaternion.Euler(12f, 0f, 0f), 28f);

            Active = true;
            LastStats = Check();
            LastStats.Missing.AddRange(missing);
            if (opt.Log) Debug.Log("[Diorama] " + LastStats.Summary());
        }

        /// <summary>設計図の段だけを読み、GroundY・HeightAtPath を使えるようにする (組まない。Build も最初にこれを通る)。
        /// 座席の置き場の検査 (seatfit) や、組む前に地面の高さが要る時に使う。Clear で忘れる</summary>
        public static void PrepareGround(DioramaLayout layout)
        {
            _slabs.Clear();
            Layout = layout;
            if (layout == null) return;
            _pathYaw = layout.PathYaw;
            DioramaMesh.TileTexels = layout.Tile;
            foreach (var p in layout.Parts) if (p.Kind == "slab") _slabs.Add(ToShape(p));
        }

        /// <summary>見本のカメラの既定の位置 (cam=28・見下ろし 12°・足元の線 0.40・1080p。Stage.LayoutCamera と同じ式で計算した値)</summary>
        public static readonly Vector3 DefaultCamPos = new Vector3(0f, 5.56f, -20.96f);

        sealed class Piece
        {
            public DioramaMeshBuilder Mesh;
            public Matrix4x4 Local = Matrix4x4.identity;   // 部品の中での置き場
            public string Surface;
            public bool Shadow;
        }

        sealed class BuildContext
        {
            public DioramaLayout Layout;
            public DioramaBuildOptions Opt;
            public Transform StaticRoot, DynRoot;
            public List<string> Missing;
            public DioramaTextures.ArraySet Arrays;
            public DioramaTextures.Atlas Atlas;
            public readonly Dictionary<string, DioramaMeshBuilder> MergeGroups = new Dictionary<string, DioramaMeshBuilder>();
            public readonly Dictionary<string, string> GroupSurface = new Dictionary<string, string>();
            public readonly Dictionary<string, bool> GroupShadow = new Dictionary<string, bool>();
            public readonly Dictionary<string, DioramaMeshBuilder> ReliefCache = new Dictionary<string, DioramaMeshBuilder>();
            public readonly Dictionary<string, DioramaMeshBuilder> ModelCache = new Dictionary<string, DioramaMeshBuilder>();
        }

        /// <summary>差し替えの形 (Resources/Stage/Act&lt;N&gt;/Models/ の Mesh)。名前は部品の model → name → kind の順に探す。無ければ null</summary>
        static DioramaMeshBuilder ModelFor(BuildContext ctx, DioramaPart p)
        {
            string model = p.Raw != null && p.Raw["model"] != null && p.Raw["model"].Type == JTokenType.String ? (string)p.Raw["model"] : null;
            foreach (var name in new[] { model, p.Name, p.Kind })
            {
                if (string.IsNullOrEmpty(name)) continue;
                string path = "Stage/Act" + ctx.Layout.Act + "/Models/" + name;
                if (!ctx.ModelCache.TryGetValue(path, out var b))
                {
                    b = null;
                    var mesh = Resources.Load<Mesh>(path);
                    if (mesh != null)
                    {
                        if (!mesh.isReadable) ctx.Missing.Add("model:" + name + " (取り込みの Read/Write を有効に)");
                        else b = FromMesh(mesh);
                    }
                    ctx.ModelCache[path] = b;
                }
                if (b != null) return b;
            }
            return null;
        }

        static DioramaMeshBuilder FromMesh(Mesh m)
        {
            var b = new DioramaMeshBuilder();
            var v = m.vertices; var n = m.normals; var uv = m.uv; var c = m.colors32;
            for (int i = 0; i < v.Length; i++)
                b.Add(v[i], n.Length == v.Length ? n[i] : Vector3.up, uv.Length == v.Length ? uv[i] : Vector2.zero, c.Length == v.Length ? c[i] : new Color32(255, 255, 255, 255));
            b.T.AddRange(m.triangles);
            return b;
        }

        static void MakeTexturesAndMaterials(BuildContext ctx)
        {
            var L = ctx.Layout;
            // 配列に積む材質 = 配列の surface が使う side・top (登場順)
            var names = new List<string>();
            foreach (var sd in L.Surfaces.Values)
            {
                if (sd.Shader != "array") continue;
                if (!string.IsNullOrEmpty(sd.Side) && !names.Contains(sd.Side)) names.Add(sd.Side);
                if (!string.IsNullOrEmpty(sd.Top) && !names.Contains(sd.Top)) names.Add(sd.Top);
            }
            var mats = new List<DioramaTileMaterial>();
            foreach (var n in names)
            {
                if (L.Tiles.TryGetValue(n, out var tm)) mats.Add(tm);
                else { ctx.Missing.Add("tiles:" + n); mats.Add(new DioramaTileMaterial { Name = n }); }
            }
            ctx.Arrays = DioramaTextures.BuildArrays(mats, L.Tile, L.AlbedoLinear, L.NormalStrength);
            Own(ctx.Arrays.Albedo); Own(ctx.Arrays.Normal);
            ctx.Missing.AddRange(ctx.Arrays.Missing);

            // アトラス = 部品が使う半立体と札の絵
            var used = new List<string>();
            foreach (var p in L.Parts)
            {
                if (!string.IsNullOrEmpty(p.Src) && !used.Contains(p.Src)) used.Add(p.Src);
                if (p.Kind == "tree" && !string.IsNullOrEmpty(p.ReliefSrc) && !used.Contains(p.ReliefSrc)) used.Add(p.ReliefSrc);
            }
            var srcList = new List<KeyValuePair<string, IList<string>>>();
            foreach (var id in used)
            {
                if (L.Sources.TryGetValue(id, out var sd)) srcList.Add(new KeyValuePair<string, IList<string>>(id, sd.Art));
                else ctx.Missing.Add("sources:" + id);
            }
            ctx.Atlas = DioramaTextures.BuildAtlas(srcList, 6, 2048);
            Own(ctx.Atlas.Tex);
            foreach (var m in ctx.Atlas.Missing) ctx.Missing.Add("atlas:" + m);

            foreach (var kv in L.Surfaces)
            {
                var m = MakeMaterial(ctx, kv.Key, kv.Value);
                if (m != null) { Materials[kv.Key] = m; Own(m); }
            }
        }

        static Material MakeMaterial(BuildContext ctx, string key, DioramaSurface sd)
        {
            var L = ctx.Layout;
            bool shaft = sd.Shader == "shaft";
            var sh = Shader.Find(shaft ? "DeckRogue/StageShaft" : "DeckRogue/StageModule");
            // -nographics のバッチ (点検) では描く装置が無く isSupported が当てにならないので、名前が見つかれば使う
            bool noDevice = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            if (sh == null || (!sh.isSupported && !noDevice))
            {
                ctx.Missing.Add("shader:" + (shaft ? "StageShaft" : "StageModule"));
                sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (sh == null) return null;
            }
            var m = new Material(sh) { name = "diorama-" + key };
            ApplySurface(ctx, m, sd);
            return m;
        }

        /// <summary>材質に値を書く (シェーダとの約束はここ1か所)</summary>
        static void ApplySurface(BuildContext ctx, Material m, DioramaSurface sd)
        {
            var L = ctx.Layout;
            m.SetFloat("_TexelsPerUnit", DioramaMesh.TexelsPerUnit);
            m.SetFloat("_TileTexels", L.Tile);
            m.SetFloat("_PathYaw", L.PathYaw);
            m.SetFloat("_Receive", sd.Receive);
            m.SetFloat("_ShadowStrength", sd.ShadowStrength);
            m.SetFloat("_VColorAO", sd.VColorAO);
            m.SetFloat("_NormalStrength", L.NormalStrength);
            m.SetColor("_BaseColor", sd.Tint);
            switch (sd.Shader)
            {
                case "array":
                {
                    m.DisableKeyword("_UV_MESH"); m.DisableKeyword("_ALPHATEST_ON");
                    m.SetTexture("_Albedo", ctx.Arrays.Albedo);
                    m.SetTexture("_Normal", ctx.Arrays.Normal);
                    ctx.Arrays.Materials.TryGetValue(sd.Side ?? "", out var side);
                    DioramaTextures.Slices top = null;
                    if (!string.IsNullOrEmpty(sd.Top)) ctx.Arrays.Materials.TryGetValue(sd.Top, out top);
                    m.SetVector("_MatSlices", new Vector4(side != null ? side.First : 0, side != null ? side.Count : 1, side != null ? side.Rot : 0, top != null ? top.Rot : 0));
                    m.SetFloat("_TopSlice", top != null ? top.First : -1);   // 負 = 天面も側と同じ (苔なし)
                    m.SetFloat("_TopThreshold", sd.TopThreshold);
                    m.SetFloat("_TopBlend", sd.TopBlend);
                    // 配列を linear:true で作った (sRGB の色を入れた) 時だけ、Linear の色空間でシェーダが sRGB→線形に戻す
                    m.SetFloat("_AlbedoDecode", L.AlbedoLinear ? 1f : 0f);
                    m.SetFloat("_NormalArrayOn", sd.NormalArray ? 1f : 0f);
                    m.SetFloat("_MeshArraySlice", -1f);
                    // P03 のシェーダは天面の種の数を側の数 (_MatSlices.y) で読む
                    if (top != null && side != null && top.Count != side.Count) ctx.Missing.Add("surface:" + sd.Side + "/" + sd.Top + " 天と側の種の数が違う (" + top.Count + "/" + side.Count + ")");
                    break;
                }
                case "atlas":
                    m.EnableKeyword("_UV_MESH"); m.EnableKeyword("_ALPHATEST_ON");
                    m.SetTexture("_BaseMap", ctx.Atlas.Tex); m.mainTexture = ctx.Atlas.Tex;
                    m.SetFloat("_Cutoff", sd.Cutoff);
                    m.SetFloat("_AlphaClip", 1f);   // シェーダが無くて URP の Unlit に落ちた時の抜き
                    m.SetFloat("_MeshArraySlice", -1f);
                    break;
                case "uv":
                {
                    m.EnableKeyword("_UV_MESH"); m.DisableKeyword("_ALPHATEST_ON");
                    DioramaTileMaterial tm = null;
                    if (!string.IsNullOrEmpty(sd.Tile)) L.Tiles.TryGetValue(sd.Tile, out tm);
                    // _BaseMap は普通の sRGB のテクスチャ (シェーダは戻さない)
                    var tex = DioramaTextures.TileTexture(tm ?? new DioramaTileMaterial { Name = sd.Tile ?? "uv" }, L.Tile, false, out var usedArt);
                    Own(tex);
                    if (usedArt == "(単色)") ctx.Missing.Add("tiles:" + sd.Tile);
                    m.SetTexture("_BaseMap", tex); m.mainTexture = tex;
                    m.SetFloat("_MeshArraySlice", -1f);
                    break;
                }
                case "shaft":
                {
                    var glow = DioramaTextures.GlowTexture();
                    Own(glow);
                    m.SetTexture("_BaseMap", glow); m.mainTexture = glow;
                    m.SetColor("_Tint", sd.Tint);
                    m.SetFloat("_Intensity", sd.Intensity);
                    m.SetFloat("_SoftDepth", sd.SoftDepth);
                    m.SetFloat("_NearFade", sd.NearFade);
                    m.SetFloat("_Fog", sd.Fog);
                    m.SetFloat("_EdgeFade", sd.EdgeFade);
                    m.renderQueue = 3000;
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- 部品

        static bool PathAligned(string kind) { return kind == "block" || kind == "rig" || kind == "fence" || kind == "marker"; }

        /// <summary>部品の既定の材質 (設計図の surfaces の名前。StageLook の look_act1.json の materials と同じ名前)。
        /// 札は "card" の材質が設計図にあればそれ (受光 0.25)、無ければ半立体と同じ "relief" (材質を 6 以下に保つ既定)</summary>
        static string DefaultSurface(string kind)
        {
            switch (kind)
            {
                case "slab": return "terrain";
                case "card": return Layout != null && Layout.Surfaces.ContainsKey("card") ? "card" : "relief";
                case "block": case "rock": return "rock";
                case "tree": return "bark";
                case "rig": case "fence": case "marker": return "wood";
                case "fog": case "shaft": return "glow";
                default: return "relief";
            }
        }

        static void BuildPart(BuildContext ctx, DioramaPart p)
        {
            var L = ctx.Layout;
            string surface = !string.IsNullOrEmpty(p.Surface) ? p.Surface : DefaultSurface(p.Kind);
            // 置き場 (根のローカル)
            float gy = HeightAtPath(p.T, p.S);
            var pos = OnPath(p.T, p.S, p.Abs ? p.Y : gy + p.Y);
            float yaw = PathAligned(p.Kind) ? L.PathYaw + p.Yaw : p.Yaw;
            var rot = Quaternion.Euler(0f, yaw, 0f);
            float scale = p.Scale > 0f ? p.Scale : 1f;
            var pieces = new List<Piece>();
            var r = new System.Random(p.Seed);

            switch (p.Kind)
            {
                case "slab":
                {
                    var front = new List<Vector2>();
                    for (int i = 0; i < p.Front.Count; i++) front.Add(p.Front[i]);
                    Func<float, float, float> mask = null;
                    if (p.Mask == "clearing") mask = (t, s) => ClearingMask(p, t, s);
                    var b = DioramaMesh.Slab(front, p.Back, p.Top, p.Bottom, p.Grid, p.Chamfer, L.PathYaw, HeightAtPath, mask);
                    pieces.Add(new Piece { Mesh = b, Surface = surface, Shadow = p.ShadowOr(true) });
                    pos = Vector3.zero; rot = Quaternion.identity; scale = 1f;   // 世界の座標で作ってある
                    break;
                }
                case "block":
                {
                    var fp = DioramaMesh.ChamferedRect(p.Num("w", 1.6f), p.Num("d", 1.2f), p.Num("cut", 0.32f), p.Seed, 0.08f);
                    pieces.Add(new Piece { Mesh = DioramaMesh.Block(fp, p.Num("h", 1f), p.Num("chamfer", 0.12f), p.Num("sink", 0.2f)), Surface = surface, Shadow = p.ShadowOr(true) });
                    break;
                }
                case "rock":
                    pieces.Add(new Piece { Mesh = DioramaMesh.Rock(p.Seed, p.Num("r", 0.6f), p.Num("h", 0.7f), (int)p.Num("sides", 6f), p.Num("squash", 0.8f)), Surface = surface, Shadow = p.ShadowOr(true) });
                    break;
                case "tree":
                {
                    if (HD2DFlags.Trunk == HD2DTrunk.Relief && !string.IsNullOrEmpty(p.ReliefSrc))
                    {
                        var rb = ReliefFor(ctx, p.ReliefSrc, p.Flip, p);
                        if (rb != null) pieces.Add(new Piece { Mesh = rb, Surface = "relief", Shadow = p.ShadowOr(true), Local = Matrix4x4.Scale(Vector3.one * p.Num("reliefScale", 1f)) });
                        break;
                    }
                    float h = p.Num("h", 12f), rad = p.Num("r", 0.55f);
                    var b = DioramaMesh.Trunk(p.Seed, h, rad, p.Num("lean", 0.4f), p.Num("flare", 0.5f), (int)p.Num("segs", 10f));
                    int roots = (int)p.Num("roots", 4f);
                    float a0 = (float)r.NextDouble() * 360f;
                    for (int i = 0; i < roots; i++)
                    {
                        float ang = a0 + i * 360f / Mathf.Max(1, roots) + ((float)r.NextDouble() - 0.5f) * 40f;
                        DioramaMesh.Root(b, rad, ang, p.Num("rootLen", 1.6f) * (0.7f + 0.6f * (float)r.NextDouble()), rad * 0.42f, p.Seed * 31 + i);
                    }
                    pieces.Add(new Piece { Mesh = b, Surface = surface, Shadow = p.ShadowOr(true) });
                    break;
                }
                case "relief":
                case "card":
                {
                    var rb = p.Kind == "card" ? CardFor(ctx, p.Src, p.Flip) : ReliefFor(ctx, p.Src, p.Flip, p);
                    if (rb == null) return;
                    float hWorld = HeightOf(ctx, p.Src) * scale;
                    bool shadowDefault = p.Kind == "relief" && hWorld >= 1.6f;
                    pieces.Add(new Piece { Mesh = rb, Surface = surface, Shadow = p.ShadowOr(shadowDefault) });
                    break;
                }
                case "rig":
                {
                    var frame = DioramaMesh.Rig(p.Num("w", 2.0f), p.Num("d", 1.6f), p.Num("h", 1.9f), p.Num("wheel", 0.42f), out var wheel, out var wheelCenter);
                    pieces.Add(new Piece { Mesh = frame, Surface = surface, Shadow = p.ShadowOr(true) });
                    // 滑車は別の GameObject (回す)。まとめない
                    var m = Matrix4x4.TRS(pos, rot, Vector3.one * scale);
                    var wgo = MakeObject(ctx.DynRoot, "rig-wheel", wheel.ToMesh("diorama-rig-wheel"), "bark", p.ShadowOr(true));
                    wgo.transform.localPosition = m.MultiplyPoint3x4(wheelCenter);
                    wgo.transform.localRotation = rot;
                    wgo.transform.localScale = Vector3.one * scale;
                    Dynamic.Add(new DynamicEntry { Name = "rig-wheel", Transform = wgo.transform, BaseRotation = wgo.transform.localRotation, Spin = p.Num("spin", 18f) });
                    break;
                }
                case "fence":
                    pieces.Add(new Piece { Mesh = DioramaMesh.Fence(p.Num("len", 3f), p.Num("h", 0.9f), (int)p.Num("posts", 4f), p.Seed), Surface = surface, Shadow = p.ShadowOr(true) });
                    break;
                case "marker":
                    pieces.Add(new Piece { Mesh = DioramaMesh.Marker(p.Num("h", 1.3f), p.Seed), Surface = surface, Shadow = p.ShadowOr(true) });
                    break;
                case "frame":
                {
                    var rb = ReliefFor(ctx, p.Src, p.Flip, p, new Vector2(0.5f, 0.5f));
                    if (rb == null) return;
                    int idx = Dynamic.Count;
                    var go = MakeObject(ctx.DynRoot, "frame-" + _frames.Count, rb.ToMesh("diorama-frame-" + p.Index), surface, p.ShadowOr(false));
                    go.transform.localScale = Vector3.one * scale;
                    Dynamic.Add(new DynamicEntry { Name = go.name, Transform = go.transform, BaseRotation = go.transform.localRotation });
                    _frames.Add(new FrameSlot { Tr = go.transform, Part = p, DynIndex = idx });
                    return;
                }
                case "fog":
                {
                    float v0 = Mathf.Clamp01(p.Num("v0", 0f));
                    var b = DioramaMesh.GlowQuad(p.Num("w", 80f), p.Num("h", 12f), new Rect(0f, v0, 0.5f, 1f - v0));
                    var go = MakeObject(ctx.DynRoot, "fog-" + p.Index, b.ToMesh("diorama-fog-" + p.Index), surface, false);
                    go.transform.localPosition = pos; go.transform.localRotation = rot;
                    Dynamic.Add(new DynamicEntry { Name = "fog-" + p.Index, Transform = go.transform, BaseRotation = rot });
                    return;
                }
                case "shaft":
                {
                    var b = DioramaMesh.Shaft(p.Num("top", 0.8f), p.Num("bottom", 2.4f), p.Num("len", 12f), new Rect(0.5f, 0f, 0.5f, 1f));
                    var go = MakeObject(ctx.DynRoot, "shaft-" + p.Index, b.ToMesh("diorama-shaft-" + p.Index), surface, false);
                    var dir = p.Dir.sqrMagnitude > 1e-6f ? p.Dir.normalized : new Vector3(0.35f, -1f, 0.3f).normalized;
                    go.transform.localPosition = pos;
                    go.transform.localRotation = Quaternion.FromToRotation(Vector3.down, dir);
                    Dynamic.Add(new DynamicEntry { Name = "shaft-" + p.Index, Transform = go.transform, BaseRotation = go.transform.localRotation });
                    return;
                }
                default:
                    ctx.Missing.Add("kind:" + p.Kind);
                    return;
            }

            // 後で FBX に差し替える口: Resources/Stage/Act<N>/Models/<model|name|kind> に Mesh (FBX の中の形・Read/Write 有効) があれば、コードの形の代わりに使う
            if (pieces.Count > 0 && (p.Kind == "block" || p.Kind == "rock" || p.Kind == "rig" || p.Kind == "fence" || p.Kind == "marker" || (p.Kind == "tree" && pieces[0].Surface != "relief")))
            {
                var model = ModelFor(ctx, p);
                if (model != null) { pieces[0].Mesh = model; pieces[0].Local = Matrix4x4.identity; }
            }

            var partM = Matrix4x4.TRS(pos, rot, Vector3.one * scale);
            if (ctx.Opt.Merge)
            {
                string chunk = ChunkKey(p);
                foreach (var pc in pieces)
                {
                    string key = pc.Surface + "|" + (pc.Shadow ? "s" : "n") + "|" + chunk;
                    if (!ctx.MergeGroups.TryGetValue(key, out var g))
                    {
                        g = new DioramaMeshBuilder();
                        ctx.MergeGroups[key] = g; ctx.GroupSurface[key] = pc.Surface; ctx.GroupShadow[key] = pc.Shadow;
                    }
                    g.Append(pc.Mesh, partM * pc.Local);
                }
            }
            else
            {
                // エディタ: 部品ごとに1つの GameObject (名前 = "p012:rock:名前" で書き戻す)
                var go = new GameObject(PartObjectName(p));
                go.layer = HD2DLayers.StageSet;
                go.transform.SetParent(ctx.StaticRoot, false);
                go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = Vector3.one * scale;
                for (int i = 0; i < pieces.Count; i++)
                {
                    var pc = pieces[i];
                    var child = MakeObject(go.transform, "mesh" + i, pc.Mesh.ToMesh("diorama-p" + p.Index + "-" + i), pc.Surface, pc.Shadow);
                    child.transform.localPosition = pc.Local.GetColumn(3);
                    child.transform.localRotation = pc.Local.rotation;
                    child.transform.localScale = pc.Local.lossyScale;
                }
            }
        }

        /// <summary>エディタで組んだ部品の GameObject の名前 ("p012:rock:名前")。DioramaLayoutTool がこれで設計図の行を探す</summary>
        public static string PartObjectName(DioramaPart p)
        {
            return "p" + p.Index.ToString("000", CultureInfo.InvariantCulture) + ":" + p.Kind + (string.IsNullOrEmpty(p.Name) ? "" : ":" + p.Name);
        }

        /// <summary>名前 "p012:…" から設計図の行の番号 (違えば −1)</summary>
        public static int PartIndexFromName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 4 || name[0] != 'p') return -1;
            int colon = name.IndexOf(':');
            if (colon < 2) return -1;
            return int.TryParse(name.Substring(1, colon - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : -1;
        }

        /// <summary>区画 (まとめる単位)。24 unit 四方 = 画面の中で 2〜3 区画</summary>
        static string ChunkKey(DioramaPart p)
        {
            if (p.Kind == "slab") return "slab";
            int ct = Mathf.FloorToInt((p.T + 48f) / 24f), cs = Mathf.FloorToInt((p.S + 24f) / 24f);
            return ct + "," + cs;
        }

        static void FlushGroups(BuildContext ctx)
        {
            var keys = new List<string>(ctx.MergeGroups.Keys);
            keys.Sort(string.CompareOrdinal);   // 決定的な順 (描く順が変わらない)
            foreach (var key in keys)
            {
                var g = ctx.MergeGroups[key];
                if (g.VertexCount == 0) continue;
                MakeObject(ctx.StaticRoot, "merged:" + key, g.ToMesh("diorama-" + key), ctx.GroupSurface[key], ctx.GroupShadow[key]);
            }
            ctx.MergeGroups.Clear();
        }

        static GameObject MakeObject(Transform parent, string name, Mesh mesh, string surface, bool shadow)
        {
            Own(mesh);
            var go = new GameObject(name);
            go.layer = HD2DLayers.StageSet;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            Materials.TryGetValue(surface, out var mat);
            mr.sharedMaterial = mat;
            bool glow = Layout != null && Layout.Surfaces.TryGetValue(surface, out var sd) && sd.Shader == "shaft";
            mr.shadowCastingMode = shadow && !glow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = !glow;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            // 既定の bit0 (全部のライトが照らす) + Environment (月の影は地形と大物だけ = 月のライトの影のレイヤーは Environment)
            mr.renderingLayerMask = 1u | HD2DLayers.RenderingEnvironment;
            return go;
        }

        // ---------------------------------------------------------------- 半立体と札

        static float HeightOf(BuildContext ctx, string src)
        {
            if (src != null && ctx.Atlas.Entries.TryGetValue(src, out var e)) return e.H / DioramaMesh.TexelsPerUnit;
            return 0f;
        }

        static DioramaMeshBuilder ReliefFor(BuildContext ctx, string src, bool flip, DioramaPart p)
        {
            return ReliefFor(ctx, src, flip, p, new Vector2(0.5f, 0f));
        }

        static DioramaMeshBuilder ReliefFor(BuildContext ctx, string src, bool flip, DioramaPart p, Vector2 pivot)
        {
            if (string.IsNullOrEmpty(src) || !ctx.Atlas.Entries.TryGetValue(src, out var e)) { ctx.Missing.Add("relief:" + src); return null; }
            ctx.Layout.Sources.TryGetValue(src, out var sd);
            float depth = sd != null ? sd.Depth : 0.12f;
            if (p != null && p.Has("depth")) depth = p.Num("depth", depth);
            string key = src + "|" + flip + "|" + pivot.x.ToString("0.00", CultureInfo.InvariantCulture) + pivot.y.ToString("0.00", CultureInfo.InvariantCulture) + "|" + depth.ToString("0.000", CultureInfo.InvariantCulture);
            if (ctx.ReliefCache.TryGetValue(key, out var cached)) return cached;
            DioramaMeshBuilder b;
            if (sd != null && sd.Flat) b = CardFor(ctx, src, flip);
            else
            {
                int cells = sd != null ? sd.Cells : 40;
                int cell = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(e.W, e.H) / (float)Mathf.Max(4, cells)));
                b = ReliefMesh.Build(e.Src, e.W, e.H, new RectInt(0, 0, e.W, e.H), e.Uv, depth, cell, sd != null ? sd.Cutoff : 0.4f, flip, pivot);
            }
            ctx.ReliefCache[key] = b;
            return b;
        }

        static DioramaMeshBuilder CardFor(BuildContext ctx, string src, bool flip)
        {
            if (string.IsNullOrEmpty(src) || !ctx.Atlas.Entries.TryGetValue(src, out var e)) { ctx.Missing.Add("card:" + src); return null; }
            string key = "card|" + src + "|" + flip;
            if (ctx.ReliefCache.TryGetValue(key, out var cached)) return cached;
            var b = DioramaMesh.Card(e.W / DioramaMesh.TexelsPerUnit, e.H / DioramaMesh.TexelsPerUnit, e.Uv, flip);
            ctx.ReliefCache[key] = b;
            return b;
        }

        // ---------------------------------------------------------------- 空き地 (天面の苔を抜く)

        /// <summary>空き地の苔の割合 (1 = 苔・0 = 土)。楕円 (中心 maskCenter・半径 maskRadius) を値ノイズで揺らし、左右へ延びる踏み跡を足す</summary>
        static float ClearingMask(DioramaPart p, float t, float s)
        {
            var c = p.Vec2("maskCenter", new Vector2(3f, 0.3f));
            var rr = p.Vec2("maskRadius", new Vector2(14f, 3.4f));
            int seed = p.Seed;
            float e = Sq((t - c.x) / Mathf.Max(0.1f, rr.x)) + Sq((s - c.y) / Mathf.Max(0.1f, rr.y));
            float n = DioramaTextures.Fbm(t * 0.35f, s * 0.35f, seed);
            float v = e + (n - 0.5f) * 0.9f;
            float clearing = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1.08f, v));
            // 踏み跡 (空き地から左右へ細く続く土)
            float trail = p.Num("trail", 1.1f);
            if (trail > 0f)
            {
                float center = c.y + 0.5f * Mathf.Sin(t * 0.17f + seed * 0.01f);
                float d = Mathf.Abs(s - center) / trail + (DioramaTextures.Noise(t * 0.5f, s * 0.5f, seed + 7) - 0.5f) * 0.8f;
                clearing = Mathf.Max(clearing, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1.05f, d)));
            }
            return 1f - clearing;
        }

        static float Sq(float v) { return v * v; }

        static SlabShape ToShape(DioramaPart p)
        {
            int n = p.Front.Count;
            var sh = new SlabShape { Name = p.Name, T = new float[n], S = new float[n], Back = p.Back, Top = p.Top };
            for (int i = 0; i < n; i++) { sh.T[i] = p.Front[i].x; sh.S[i] = p.Front[i].y; }
            return sh;
        }

        // ================================================================ カメラと動き

        /// <summary>カメラを置き直した後に呼ぶ: 額縁をカメラからの深さ (6〜10) の画面の決まった位置へ置き直す。
        /// 位置は画面の割合 (vx 0=左・1=右、vy 0=下・1=上) で、カメラの方を向ける。画面の縦横比は Screen から (無ければ 16:9)</summary>
        public static void OnCameraLayout(Vector3 camPos, Quaternion camRot, float fov)
        {
            _hasCam = true; _camPos = camPos; _camRot = camRot; _camFov = fov;
            if (_frames.Count == 0) return;
            float tanV = Mathf.Tan(Mathf.Clamp(fov, 1f, 170f) * 0.5f * Mathf.Deg2Rad);
            float aspect = Screen.width > 0 && Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9f;
            foreach (var f in _frames)
            {
                if (f.Tr == null) continue;
                var p = f.Part;
                float depth = p.Num("depth", 8f);
                float x = (p.Num("vx", 0f) - 0.5f) * 2f * tanV * aspect * depth;
                float y = (p.Num("vy", 0.5f) - 0.5f) * 2f * tanV * depth;
                f.Tr.position = camPos + camRot * new Vector3(x, y, depth);
                f.Tr.rotation = camRot * Quaternion.Euler(0f, 0f, p.Num("roll", 0f));
                if (f.DynIndex >= 0 && f.DynIndex < Dynamic.Count)
                {
                    var e = Dynamic[f.DynIndex];
                    e.BaseRotation = f.Tr.localRotation;
                    Dynamic[f.DynIndex] = e;
                }
            }
        }

        /// <summary>動く物を時刻 time (秒) の姿にする (StageDriver が毎フレーム呼ぶ。det の撮影では Time.time が決定的)</summary>
        public static void Tick(float time)
        {
            for (int i = 0; i < Dynamic.Count; i++)
            {
                var e = Dynamic[i];
                if (e.Transform == null) continue;
                if (e.Spin != 0f) e.Transform.localRotation = e.BaseRotation * Quaternion.Euler(0f, 0f, time * e.Spin);
                else if (e.Sway != 0f && e.SwayPeriod > 0f) e.Transform.localRotation = e.BaseRotation * Quaternion.Euler(0f, 0f, e.Sway * Mathf.Sin(time * Mathf.PI * 2f / e.SwayPeriod));
            }
        }

        /// <summary>
        /// アトラスの材質 (設計図の surfaces で shader = "atlas" の物 = 半立体・札・額縁) の alpha-to-coverage (StageModule の _AlphaToMask) を切り替える。
        /// MSAA の時だけ on にする (MSAA の無い時に on だと、URP の AlphaClip が切った後の値を1標本の被覆へ回して縁の切れ方が変わる)。Stage (P12) が組んだ後と旗 aa= が変わった時に呼ぶ
        /// </summary>
        public static void SetAlphaToCoverage(bool on)
        {
            if (Layout == null) return;
            foreach (var kv in Layout.Surfaces)
            {
                if (kv.Value == null || kv.Value.Shader != "atlas") continue;
                if (Materials.TryGetValue(kv.Key, out var m) && m != null && m.HasProperty("_AlphaToMask")) m.SetFloat("_AlphaToMask", on ? 1f : 0f);
            }
        }

        /// <summary>
        /// dumplayout 用 (layout.json の extra.diorama): 最後に組んだ時の点検 (部品・三角形・Renderer・材質・動く物・座席の帯の高さと部品・種類ごとの数・見つからない物)と、
        /// 設計図の名前・額縁の数・動く物の名前。Stage が組んだ時間と組み方の名札を足す (額縁の画面の矩形 extra.frames は P10 の StageCamera.DumpFrames)
        /// </summary>
        public static Dictionary<string, object> DebugInfo()
        {
            var o = new Dictionary<string, object>();
            o["active"] = Active;
            o["layout"] = Layout != null ? LayoutResource(Layout.Act) : null;
            o["pathYaw"] = _pathYaw;
            var st = LastStats;
            if (st != null)
            {
                o["parts"] = st.Parts; o["triangles"] = st.Triangles; o["renderers"] = st.Renderers; o["materials"] = st.Materials; o["dynamic"] = st.Dynamic;
                o["seatMaxAbsY"] = float.IsNaN(st.SeatMaxAbsY) ? (object)null : st.SeatMaxAbsY;
                o["seatIntrusions"] = st.SeatIntrusions;
                o["ok"] = st.Ok;
                o["failures"] = new List<string>(st.Failures);
                o["missing"] = new List<string>(st.Missing);
                var kinds = new Dictionary<string, object>();
                var keys = new List<string>(st.ByKind.Keys); keys.Sort(string.CompareOrdinal);
                foreach (var k in keys) kinds[k] = st.ByKind[k];
                o["byKind"] = kinds;
            }
            o["frames"] = _frames.Count;
            var dyn = new List<string>();
            foreach (var e in Dynamic) dyn.Add(e.Name);
            o["dynamicNames"] = dyn;
            return o;
        }

        // ================================================================ 捨てる

        /// <summary>箱庭を捨てる (old に戻る時)。作ったメッシュ・テクスチャ・材質も捨てる</summary>
        public static void Clear()
        {
            if (Root != null) Kill(Root.gameObject);
            foreach (var o in _owned) if (o != null) Kill(o);
            _owned.Clear();
            Dynamic.Clear();
            Materials.Clear();
            _frames.Clear();
            _slabs.Clear();
            Root = null; Layout = null; Active = false; _look = null;
            _pathYaw = DefaultPathYaw;
        }

        static void Own(UnityEngine.Object o) { if (o != null) _owned.Add(o); }

        static void Kill(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        static void MarkDontSave(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            foreach (var o in _owned) if (o != null) o.hideFlags = HideFlags.DontSave;
        }

        // ================================================================ 点検

        /// <summary>見本の門 (計画 P04 の確かめ方): 部品 250〜350・Renderer 120 以下・材質 6 以下・三角形 25 万以下・座席の帯の高さ |y| &lt; 0.01・座席の帯に部品が無い</summary>
        public const int GatePartsMin = 250, GatePartsMax = 350, GateRenderers = 120, GateMaterials = 6, GateTriangles = 250000;
        public const float GateSeatAbsY = 0.01f;

        /// <summary>座席の帯 (道の座標)。敵4体の奥の席 t=11.2・ひなたの人形の後列 s≈2.05・からくりの匣 (−6.3, 1.7)/(−5.3, −1.9) を含む</summary>
        public const float SeatT0 = -8.5f, SeatT1 = 13f, SeatS0 = -2.6f, SeatS1 = 2.8f;

        /// <summary>いま組んである箱庭を数える (Build の最後にも呼ぶ)</summary>
        public static DioramaStats Check()
        {
            var st = new DioramaStats();
            if (Layout != null)
            {
                foreach (var p in Layout.Parts)
                {
                    st.Parts++;
                    st.ByKind.TryGetValue(p.Kind, out var c); st.ByKind[p.Kind] = c + 1;
                    if (p.Kind == "slab" || p.Kind == "fog" || p.Kind == "shaft" || p.Kind == "frame") continue;
                    float reach = p.Kind == "rock" ? p.Num("r", 0.6f) : p.Kind == "block" ? Mathf.Max(p.Num("w", 1.6f), p.Num("d", 1.2f)) * 0.5f
                        : p.Kind == "tree" ? p.Num("r", 0.55f) + p.Num("rootLen", 1.6f) : p.Kind == "fence" ? p.Num("len", 3f) * 0.5f : p.Kind == "rig" ? 1.5f : 0.3f;
                    if (p.T + reach > SeatT0 && p.T - reach < SeatT1 && p.S + reach > SeatS0 && p.S - reach < SeatS1 && !p.Abs)
                    { st.SeatIntrusions++; st.IntrusionNames.Add(PartObjectName(p)); }
                }
            }
            float maxY = 0f;
            if (_slabs.Count > 0)
            {
                for (float t = SeatT0; t <= SeatT1 + 1e-3f; t += 0.25f)
                    for (float s = SeatS0; s <= SeatS1 + 1e-3f; s += 0.2f)
                        maxY = Mathf.Max(maxY, Mathf.Abs(HeightAtPath(t, s)));
            }
            else maxY = float.NaN;
            st.SeatMaxAbsY = maxY;
            var mats = new HashSet<Material>();
            if (Root != null)
            {
                foreach (var mr in Root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    st.Renderers++;
                    foreach (var m in mr.sharedMaterials) if (m != null) mats.Add(m);
                }
                // 同じメッシュは1回だけ数える (StaticBatchingUtility.Combine の後は、まとめた全部の Renderer が1つの結合メッシュを指す)
                var seen = new HashSet<Mesh>();
                foreach (var mf in Root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = mf.sharedMesh;
                    if (mesh == null || !seen.Add(mesh)) continue;
                    for (int sm = 0; sm < mesh.subMeshCount; sm++) st.Triangles += (int)(mesh.GetIndexCount(sm) / 3);
                }
            }
            st.Materials = mats.Count;
            st.Dynamic = Dynamic.Count;
            st.Evaluate();
            return st;
        }

        // ================================================================ 設計図

        /// <summary>設計図の Resources のパス (拡張子なし)</summary>
        public static string LayoutResource(int act) { return "Stage/act" + act + "_layout"; }

        /// <summary>Resources の設計図を読む (無ければ null)</summary>
        public static DioramaLayout LoadLayout(int act)
        {
            var ta = Resources.Load<TextAsset>(LayoutResource(act));
            if (ta == null) return null;
            try { return DioramaLayout.Parse(ta.text); }
            catch (Exception e) { Debug.LogWarning("[Diorama] 設計図が読めない: " + e.Message); return null; }
        }
    }

    /// <summary>Build の選び方</summary>
    public sealed class DioramaBuildOptions
    {
        /// <summary>静的な部品を材質ごとに溶かす (実行時の既定)。false = 部品ごとの GameObject (エディタで目で詰める)</summary>
        public bool Merge = true;
        /// <summary>実行中なら StaticBatchingUtility.Combine も掛ける (設計図の staticBatch も true の時)</summary>
        public bool StaticBatch = true;
        /// <summary>最後に点検の1行をログへ出す</summary>
        public bool Log = true;
        /// <summary>読んだ設計図を使う (null なら Resources から読む)</summary>
        public DioramaLayout Layout;
    }

    /// <summary>点検の結果</summary>
    public sealed class DioramaStats
    {
        public int Parts, Triangles, Renderers, Materials, Dynamic, SeatIntrusions;
        public float SeatMaxAbsY;
        public readonly Dictionary<string, int> ByKind = new Dictionary<string, int>();
        public readonly List<string> IntrusionNames = new List<string>();
        /// <summary>見つからなかった物 (絵・シェーダ・材質)。W1 では PixelLab の新しい絵が無いので、落ちた候補がここに並ぶ</summary>
        public readonly List<string> Missing = new List<string>();
        public readonly List<string> Failures = new List<string>();
        public bool Ok => Failures.Count == 0;

        public void Evaluate()
        {
            Failures.Clear();
            if (Parts < Diorama.GatePartsMin || Parts > Diorama.GatePartsMax) Failures.Add("部品 " + Parts + " (" + Diorama.GatePartsMin + "〜" + Diorama.GatePartsMax + ")");
            if (Renderers > Diorama.GateRenderers) Failures.Add("Renderer " + Renderers + " (" + Diorama.GateRenderers + " 以下)");
            if (Materials > Diorama.GateMaterials) Failures.Add("材質 " + Materials + " (" + Diorama.GateMaterials + " 以下)");
            if (Triangles > Diorama.GateTriangles) Failures.Add("三角形 " + Triangles + " (" + Diorama.GateTriangles + " 以下)");
            if (float.IsNaN(SeatMaxAbsY) || SeatMaxAbsY >= Diorama.GateSeatAbsY) Failures.Add("座席の帯の高さ " + SeatMaxAbsY.ToString("0.000", CultureInfo.InvariantCulture));
            if (SeatIntrusions > 0) Failures.Add("座席の帯の部品 " + SeatIntrusions);
        }

        public string Summary()
        {
            var sb = new StringBuilder();
            sb.Append("check parts=").Append(Parts).Append(" tris=").Append(Triangles).Append(" renderers=").Append(Renderers)
              .Append(" materials=").Append(Materials).Append(" dynamic=").Append(Dynamic)
              .Append(" seatY=").Append(SeatMaxAbsY.ToString("0.0000", CultureInfo.InvariantCulture))
              .Append(" seatParts=").Append(SeatIntrusions)
              .Append(" result=").Append(Ok ? "OK" : "NG");
            if (!Ok) sb.Append(" (").Append(string.Join(" / ", Failures)).Append(")");
            if (Missing.Count > 0) sb.Append(" missing=").Append(Missing.Count);
            return sb.ToString();
        }

        public string ToJson()
        {
            var o = new JObject
            {
                ["parts"] = Parts, ["triangles"] = Triangles, ["renderers"] = Renderers, ["materials"] = Materials, ["dynamic"] = Dynamic,
                ["seatMaxAbsY"] = float.IsNaN(SeatMaxAbsY) ? (JToken)JValue.CreateNull() : SeatMaxAbsY,
                ["seatIntrusions"] = SeatIntrusions, ["ok"] = Ok,
                ["failures"] = new JArray(Failures.ToArray()), ["missing"] = new JArray(Missing.ToArray()),
                ["intrusions"] = new JArray(IntrusionNames.ToArray()),
            };
            var kinds = new JObject();
            var keys = new List<string>(ByKind.Keys); keys.Sort(string.CompareOrdinal);
            foreach (var k in keys) kinds[k] = ByKind[k];
            o["byKind"] = kinds;
            return o.ToString(Formatting.Indented);
        }
    }

    /// <summary>材質の面 (設計図の surfaces の1つ)</summary>
    public sealed class DioramaSurface
    {
        /// <summary>"array" (タイルの配列を道の座標で投影)・"atlas" (アトラスの半立体と札)・"uv" (メッシュの UV でタイルを貼る)・"shaft" (光の面)</summary>
        public string Shader = "array";
        public string Side, Top, Tile;
        public float Receive = 0.8f, ShadowStrength = 1f, VColorAO = 1f, TopThreshold = 0.65f, TopBlend = 0.15f, Cutoff = 0.4f;
        public float Intensity = 1f, SoftDepth = 1.5f, NearFade = 2f, Fog = 0.3f, EdgeFade;
        /// <summary>配列の材質で法線の配列 _Normal を読む (_NormalArrayOn)</summary>
        public bool NormalArray = true;
        public Color Tint = Color.white;
    }

    /// <summary>半立体・札の元の絵 (設計図の sources の1つ)</summary>
    public sealed class DioramaSource
    {
        public readonly List<string> Art = new List<string>();
        public float Depth = 0.12f, Cutoff = 0.4f;
        public int Cells = 40;
        /// <summary>膨らませない (平らな札)</summary>
        public bool Flat;
    }

    /// <summary>設計図の部品1行。Raw は元の JSON (書き戻しで値だけ差し替える)</summary>
    public sealed class DioramaPart
    {
        public int Index;
        public string Kind, Name, Src, Surface, ReliefSrc, Mask;
        public float T, S, Y, Yaw, Scale = 1f;
        public bool Abs, Flip;
        public int Seed;
        public int Shadow = -1;
        // 段
        public readonly List<Vector2> Front = new List<Vector2>();
        public float Back, Top, Bottom, Grid = 0.64f, Chamfer;
        // 光の筋
        public Vector3 Dir;
        public JObject Raw;

        public bool ShadowOr(bool def) { return Shadow < 0 ? def : Shadow > 0; }
        public bool Has(string key) { return Raw != null && Raw[key] != null; }
        public float Num(string key, float def)
        {
            var t = Raw != null ? Raw[key] : null;
            if (t == null || (t.Type != JTokenType.Float && t.Type != JTokenType.Integer)) return def;
            return t.Value<float>();
        }
        public Vector2 Vec2(string key, Vector2 def)
        {
            var a = Raw != null ? Raw[key] as JArray : null;
            if (a == null || a.Count < 2) return def;
            return new Vector2(a[0].Value<float>(), a[1].Value<float>());
        }
    }

    /// <summary>箱庭の設計図 (Resources/Stage/act&lt;N&gt;_layout.json)。IL2CPP で安全なように JObject を手で読む (リフレクションの逆直列化はしない)</summary>
    public sealed class DioramaLayout
    {
        public int Act = 1;
        public float PathYaw = Diorama.DefaultPathYaw;
        public int Tile = 64;
        /// <summary>色の配列を linear:true で作り sRGB の色をそのまま入れる (シェーダが Linear の時だけ戻す = _AlbedoDecode 1)。計画 P04 の手順2。false = sRGB の配列 (_AlbedoDecode 0)</summary>
        public bool AlbedoLinear = true;
        public bool StaticBatch = true;
        public float NormalStrength = 0.6f;
        public readonly Dictionary<string, DioramaTileMaterial> Tiles = new Dictionary<string, DioramaTileMaterial>();
        public readonly Dictionary<string, DioramaSurface> Surfaces = new Dictionary<string, DioramaSurface>();
        public readonly Dictionary<string, DioramaSource> Sources = new Dictionary<string, DioramaSource>();
        public readonly List<DioramaPart> Parts = new List<DioramaPart>();
        public JObject Raw;

        static float F(JToken o, string key, float def)
        {
            var t = o != null ? o[key] : null;
            return t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? t.Value<float>() : def;
        }
        static string S(JToken o, string key) { var t = o != null ? o[key] : null; return t != null && t.Type == JTokenType.String ? (string)t : null; }
        static bool B(JToken o, string key, bool def) { var t = o != null ? o[key] : null; return t != null && t.Type == JTokenType.Boolean ? (bool)t : def; }
        static Color Col(JToken o, string key, Color def)
        {
            var a = o != null ? o[key] as JArray : null;
            if (a == null || a.Count < 3) return def;
            float k = 1f;
            foreach (var v in a) if (v.Value<float>() > 1.001f) { k = 1f / 255f; break; }
            return new Color(a[0].Value<float>() * k, a[1].Value<float>() * k, a[2].Value<float>() * k, a.Count > 3 ? a[3].Value<float>() * k : 1f);
        }

        static int RotOf(string s)
        {
            switch (s) { case "flipX": return 1; case "rot4": return 2; default: return 0; }
        }

        public static DioramaLayout Parse(string json)
        {
            var root = JObject.Parse(json);
            var L = new DioramaLayout { Raw = root };
            L.Act = (int)F(root, "act", 1);
            L.PathYaw = F(root, "pathYaw", Diorama.DefaultPathYaw);
            L.Tile = Mathf.Clamp((int)F(root, "tile", 64), 8, 512);
            L.AlbedoLinear = B(root, "albedoLinear", true);
            L.StaticBatch = B(root, "staticBatch", true);
            L.NormalStrength = F(root, "normalStrength", 0.6f);

            if (root["tiles"] is JObject tiles)
                foreach (var kv in tiles)
                {
                    var o = kv.Value as JObject; if (o == null) continue;
                    var tm = new DioramaTileMaterial { Name = kv.Key, Rot = RotOf(S(o, "rot")) };
                    var fb = Col(o, "fallback", new Color(0.38f, 0.41f, 0.35f));
                    tm.Fallback = (Color32)fb;
                    if (o["variants"] is JArray vars)
                        foreach (var v in vars)
                        {
                            var list = new List<DioramaTileCandidate>();
                            if (v is JArray cands) foreach (var c in cands) AddCandidate(list, c);
                            else AddCandidate(list, v);
                            tm.Variants.Add(list);
                        }
                    L.Tiles[kv.Key] = tm;
                }

            if (root["surfaces"] is JObject surfs)
                foreach (var kv in surfs)
                {
                    var o = kv.Value as JObject; if (o == null) continue;
                    L.Surfaces[kv.Key] = new DioramaSurface
                    {
                        Shader = S(o, "shader") ?? "array", Side = S(o, "side"), Top = S(o, "top"), Tile = S(o, "tile"),
                        Receive = F(o, "receive", 0.8f), ShadowStrength = F(o, "shadowStrength", 1f), VColorAO = F(o, "vcolorAO", 1f),
                        TopThreshold = F(o, "topThreshold", 0.65f), TopBlend = F(o, "topBlend", 0.15f), Cutoff = F(o, "cutoff", 0.4f),
                        Intensity = F(o, "intensity", 1f), SoftDepth = F(o, "softDepth", 1.5f), NearFade = F(o, "nearFade", 2f), Fog = F(o, "fog", 0.3f),
                        EdgeFade = F(o, "edgeFade", 0f), NormalArray = B(o, "normalArray", true),
                        Tint = Col(o, "tint", Color.white),
                    };
                }

            if (root["sources"] is JObject srcs)
                foreach (var kv in srcs)
                {
                    var o = kv.Value as JObject; if (o == null) continue;
                    var sd = new DioramaSource { Depth = F(o, "depth", 0.12f), Cutoff = F(o, "cutoff", 0.4f), Cells = (int)F(o, "cells", 40), Flat = B(o, "flat", false) };
                    if (o["art"] is JArray arts) foreach (var a in arts) { if (a.Type == JTokenType.String) sd.Art.Add((string)a); }
                    else if (S(o, "art") != null) sd.Art.Add(S(o, "art"));
                    L.Sources[kv.Key] = sd;
                }

            if (root["parts"] is JArray parts)
                for (int i = 0; i < parts.Count; i++)
                {
                    var o = parts[i] as JObject; if (o == null) continue;
                    var p = new DioramaPart
                    {
                        Index = i, Raw = o, Kind = S(o, "kind") ?? "?", Name = S(o, "name"), Src = S(o, "src"), Surface = S(o, "surface"),
                        ReliefSrc = S(o, "relief"), Mask = S(o, "mask"),
                        T = F(o, "t", 0f), S = F(o, "s", 0f), Y = F(o, "y", 0f), Yaw = F(o, "yaw", 0f), Scale = F(o, "scale", 1f),
                        Abs = B(o, "abs", false), Flip = B(o, "flip", false), Seed = (int)F(o, "seed", i * 7919 + 13),
                        Shadow = o["shadow"] == null ? -1 : o["shadow"].Type == JTokenType.Boolean ? ((bool)o["shadow"] ? 1 : 0) : (F(o, "shadow", 1f) > 0.5f ? 1 : 0),
                        Back = F(o, "back", 10f), Top = F(o, "top", 0f), Bottom = F(o, "bottom", -1f), Grid = F(o, "grid", 0.64f), Chamfer = F(o, "chamfer", 0f),
                    };
                    if (o["front"] is JArray fr)
                        foreach (var q in fr) if (q is JArray qa && qa.Count >= 2) p.Front.Add(new Vector2(qa[0].Value<float>(), qa[1].Value<float>()));
                    if (o["dir"] is JArray da && da.Count >= 3) p.Dir = new Vector3(da[0].Value<float>(), da[1].Value<float>(), da[2].Value<float>());
                    if (p.Kind == "slab")
                    {
                        p.Front.Sort((a, b) => a.x.CompareTo(b.x));
                        if (p.Front.Count < 2) continue;   // 縁の無い段は読まない
                    }
                    L.Parts.Add(p);
                }
            return L;
        }

        static void AddCandidate(List<DioramaTileCandidate> list, JToken c)
        {
            if (c == null) return;
            if (c.Type == JTokenType.String) { list.Add(new DioramaTileCandidate { Art = (string)c }); return; }
            if (c is JObject o && S(o, "art") != null)
                list.Add(new DioramaTileCandidate { Art = S(o, "art"), Calm = F(o, "calm", 1f), Sat = F(o, "sat", 1f), ClampWhite = B(o, "clampWhite", false) });
        }
    }
}
