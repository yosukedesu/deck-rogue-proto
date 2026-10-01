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
// 三周目 (2026-10-01・レーン S。計画 docs/design/hd2d-round3-plan-2026-10-01.md §2 S)。新しいキーが無ければ二周目と同じ:
//  ・surfaces.<名前>.normal (数 0〜1): アトラスの材質の法線アトラスの強さ (StageModule の _NormalAtlasOn/_NormalAtlasStrength)。どれかの atlas の面が > 0 の時だけ
//    DioramaTextures.R3S_BuildNormalAtlas で法線のアトラスを作る (各絵の "<パス>_n"。無い絵は平ら)。1枚も無ければ読まない (Missing に1行)。
//  ・surfaces.<名前>.sway: {"amp": unit, "freq": [Hz, Hz], "phase": rad/unit (既定 0.12)} = 揺れの振幅と周波数 (atlas と uv の材質だけ。_SwayAmp/_SwayFreq)。
//    部品の "sway": 0〜1 (relief・card・frame・tree の半立体) と "swayFrom": "bottom" (既定)|"top"|"left"|"right" (根元の辺) を ReliefMesh.R3S_SwayCopy が頂点色の a に書く
//    (a = 1 − 重み × 根元からの割合。揺らさない部品は a = 1 のまま)。
//  ・部品 "kind": "mist" = α合成のノイズ入り霧の板 (StageMist。材質は全部の霧の板で1つ = Materials["mist"]・板ごとの値は MaterialPropertyBlock)。
//    {"t","s","y","abs"?,"yaw"?,"w","h","v0"?,"alpha","noise":[横,縦 (回/unit)],"flow": unit/秒,"tint":[r,g,b]? (無ければ霧の色),"seed"?,"phone":{...}?}
//  ・設計図の頂の "gates": {"partsMin","partsMax","litterMax" (＋任意で "renderersMax","materialsMax","trianglesMax")} = 点検の門 (無ければ定数の三周目の既定)。
//  ・幕の光の設計図 look の "diorama": {"normalAtlas": false, "sway": false} で法線と揺れを切る (スマホの重さの口。既定 true)。
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

            // 設計図 look の diorama.dropKinds (スマホの段1〜3 = look_act1_phone1〜3。W4 P31 の申し送り・統合で足した): その種類の部品を組まない。
            // 段1 の「光の筋と霧の面を落とす」を見た目 (materials.glow.intensity 0) だけでなく描く重さごと消す。既定の設計図には無いキーなので既定の画は変わらない
            HashSet<string> drop = null;
            if (look != null && look.Raw != null && look.Raw["diorama"] is JObject dio && dio["dropKinds"] is JArray dk)
            {
                drop = new HashSet<string>();
                foreach (var k in dk) if (k.Type == JTokenType.String) drop.Add((string)k);
                if (drop.Count == 0) drop = null;
                else Debug.Log("[Diorama] 組まない部品の種類 (diorama.dropKinds): " + string.Join(",", drop));
            }

            foreach (var p in layout.Parts)
            {
                if (drop != null && drop.Contains(p.Kind)) continue;
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
            /// <summary>三周目 R7 (レーン S): 法線のアトラス (頼まれた時だけ。無ければ null) と、法線のあった絵の数</summary>
            public Texture2D NormalAtlas;
            public int NormalFound;
            /// <summary>三周目 R8 (レーン S): 霧の板の材質 (最初の mist 部品で作る。シェーダが無ければ null のまま = 霧の板は組まない)</summary>
            public Material Mist;
            public bool MistTried;
        }

        // ================================================================ 三周目 (レーン S) の記録 (dumplayout の extra.diorama)

        /// <summary>法線のアトラス: 頼まれたか・法線のあった絵の数・アトラスの絵の数・材質で読んでいるか</summary>
        static bool R3S_NormalRequested, R3S_NormalOn;
        static int R3S_NormalFound, R3S_NormalTotal;
        /// <summary>組んだ霧の板 (kind mist) と、揺れの重みを書いた部品の数・揺れを書いた材質の名前</summary>
        static int R3S_MistParts, R3S_SwayParts;
        static readonly List<string> R3S_SwayMaterials = new List<string>();

        /// <summary>幕の光の設計図 look の "diorama" の真偽 (key が無ければ def)。スマホの段で法線と揺れを切る口 ("normalAtlas"・"sway")</summary>
        static bool R3S_LookFlag(string key, bool def)
        {
            var dio = _look != null && _look.Raw != null ? _look.Raw["diorama"] as JObject : null;
            var t = dio != null ? dio[key] : null;
            if (t == null) return def;
            if (t.Type == JTokenType.Boolean) return (bool)t;
            if (t.Type == JTokenType.Integer || t.Type == JTokenType.Float) return t.Value<float>() > 0.5f;
            return def;
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
            // 異方性の段 (二周目 レーン D 段1): PC は設計図の anisoLevel (無ければ 8)・tier=phone は 1
            int aniso = HD2DFlags.Tier == HD2DTier.Phone ? DioramaTextures.AnisoPhone : L.AnisoPc;
            ctx.Arrays = DioramaTextures.BuildArrays(mats, L.Tile, L.AlbedoLinear, L.NormalStrength, aniso);
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

            // 三周目 R7 (レーン S): どれかのアトラスの材質が法線を頼んだ (surfaces.<名前>.normal > 0) 時だけ、同じ詰め方の法線のアトラスを作る。
            // look の diorama.normalAtlas = false (スマホの段) なら作らない。法線の絵が1枚も無ければ読ませない (平らと同じで、読む分だけ重い)
            foreach (var sd in L.Surfaces.Values) if (sd != null && sd.Shader == "atlas" && sd.NormalAtlas > 0f) R3S_NormalRequested = true;
            if (R3S_NormalRequested && R3S_LookFlag("normalAtlas", true))
            {
                ctx.NormalAtlas = DioramaTextures.R3S_BuildNormalAtlas(ctx.Atlas, out ctx.NormalFound, out R3S_NormalTotal, ctx.Missing);
                Own(ctx.NormalAtlas);
                R3S_NormalFound = ctx.NormalFound;
                if (ctx.NormalAtlas != null && ctx.NormalFound == 0) ctx.Missing.Add("normalAtlas: 半立体の絵に _n が1枚も無い (法線は読まない)");
            }

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
                    m.SetFloat("_LobeFloor", Mathf.Clamp01(sd.LobeFloor));   // W3 P22: 霧の光の芯から外れた面ほど薄く (中央の奥が光る)
                    m.renderQueue = 3000;
                    break;
                }
            }

            // 三周目 (レーン S): 法線のアトラス (R7) と揺れ (R14)。設計図にキーが無ければ 0 = 二周目と同じ
            if (sd.Shader != "shaft")
            {
                m.SetFloat("_NormalAtlasOn", 0f);
                m.SetFloat("_NormalAtlasStrength", 0f);
                m.SetFloat("_SwayAmp", 0f);
                if (sd.Shader == "atlas" && sd.NormalAtlas > 0f && ctx.NormalAtlas != null && ctx.NormalFound > 0)
                {
                    m.SetTexture("_NormalAtlas", ctx.NormalAtlas);
                    m.SetFloat("_NormalAtlasOn", 1f);
                    m.SetFloat("_NormalAtlasStrength", Mathf.Clamp(sd.NormalAtlas, 0f, 2f));
                    R3S_NormalOn = true;
                }
                // 揺れはメッシュの UV の材質だけ (配列の材質は頂点色の a が苔の割合 = シェーダも _UV_MESH の時しか揺らさない)
                if ((sd.Shader == "atlas" || sd.Shader == "uv") && sd.SwayAmp > 0f && R3S_LookFlag("sway", true))
                {
                    m.SetFloat("_SwayAmp", sd.SwayAmp);
                    m.SetVector("_SwayFreq", new Vector4(sd.SwayFreq.x, sd.SwayFreq.y, sd.SwayFreq.z, 0f));
                    R3S_SwayMaterials.Add(m.name);
                }
            }
        }

        /// <summary>三周目 R14 (レーン S): 部品の揺れの重み ("sway" 0〜1) と根元の辺 ("swayFrom")。重みが 0 なら false</summary>
        static bool R3S_PartSway(DioramaPart p, out float weight, out string from)
        {
            weight = Mathf.Clamp01(p.Num("sway", 0f));
            from = p.Raw != null && p.Raw["swayFrom"] != null && p.Raw["swayFrom"].Type == JTokenType.String ? (string)p.Raw["swayFrom"] : "bottom";
            if (from != "top" && from != "left" && from != "right") from = "bottom";
            return weight > 0f;
        }

        /// <summary>三周目 R14 (レーン S): その材質が揺らせる (メッシュの UV の材質 = atlas・uv)</summary>
        static bool R3S_SwayableSurface(BuildContext ctx, string surface)
        {
            return surface != null && ctx.Layout.Surfaces.TryGetValue(surface, out var sd) && sd != null && (sd.Shader == "atlas" || sd.Shader == "uv");
        }

        /// <summary>三周目 R8 (レーン S): 霧の板の材質 (全部の霧の板で1つ)。最初に呼ばれた時に作る。シェーダが無ければ null (霧の板は組まない = 白い箱を出さない)</summary>
        static Material R3S_MistMaterial(BuildContext ctx)
        {
            if (ctx.MistTried) return ctx.Mist;
            ctx.MistTried = true;
            var sh = Shader.Find("DeckRogue/StageMist");
            bool noDevice = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
            if (sh == null || (!sh.isSupported && !noDevice)) { ctx.Missing.Add("shader:StageMist"); return null; }
            var m = new Material(sh) { name = "diorama-mist" };
            Own(m);
            if (!Materials.ContainsKey("mist")) Materials["mist"] = m;
            ctx.Mist = m;
            return m;
        }

        /// <summary>三周目 R8 (レーン S): 霧の板の "tint" ([r, g, b]・0〜1 か 0〜255。見た目の明るさ = sRGB)。無い・読めなければ false (霧の色を使う)</summary>
        static bool R3S_MistTint(DioramaPart p, out Color c)
        {
            c = Color.white;
            var a = p.Raw != null ? p.Raw["tint"] as JArray : null;
            if (a == null || a.Count < 3) return false;
            float k = 1f;
            for (int i = 0; i < 3; i++) if (a[i].Value<float>() > 1.001f) { k = 1f / 255f; break; }
            c = new Color(Mathf.Clamp01(a[0].Value<float>() * k), Mathf.Clamp01(a[1].Value<float>() * k), Mathf.Clamp01(a[2].Value<float>() * k), 1f);
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) c = c.linear;
            return true;
        }

        /// <summary>三周目 R8 (レーン S): 霧の板を1枚組む (動かさない・まとめない・影なし)。板ごとの値は MaterialPropertyBlock</summary>
        static void R3S_BuildMist(BuildContext ctx, DioramaPart p, Vector3 pos, Quaternion rot)
        {
            var mat = R3S_MistMaterial(ctx);
            if (mat == null) return;
            var ph = PhoneOf(p);
            float w = Mathf.Max(0.1f, JNum(ph, "w", p.Num("w", 20f)));
            float h = Mathf.Max(0.1f, JNum(ph, "h", p.Num("h", 4f)));
            float v0 = Mathf.Clamp01(JNum(ph, "v0", p.Num("v0", 0f)));
            float alpha = Mathf.Clamp01(JNum(ph, "alpha", p.Num("alpha", 0.3f)));
            float flow = JNum(ph, "flow", p.Num("flow", 0.02f));
            var noise = p.Vec2("noise", new Vector2(0.3f, 0.8f));
            float seed01 = (Mathf.Abs(p.Seed) % 997) / 997f;
            var b = DioramaMesh.GlowQuad(w, h, new Rect(0f, v0, 1f, 1f - v0));
            var mesh = b.ToMesh("diorama-mist-" + p.Index);
            Own(mesh);
            var go = new GameObject("mist-" + p.Index);
            go.layer = HD2DLayers.StageSet;
            go.transform.SetParent(ctx.DynRoot, false);
            go.transform.localPosition = pos; go.transform.localRotation = rot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.renderingLayerMask = 1u | HD2DLayers.RenderingEnvironment;
            var mpb = new MaterialPropertyBlock();
            mpb.SetVector("_MistParams", new Vector4(alpha, flow, Mathf.Max(0.001f, noise.x), Mathf.Max(0.001f, noise.y)));
            mpb.SetVector("_MistSize", new Vector4(w, h, v0, seed01));
            mpb.SetVector("_MistTint", R3S_MistTint(p, out var tint) ? new Vector4(tint.r, tint.g, tint.b, 1f) : Vector4.zero);
            mr.SetPropertyBlock(mpb);
            AddBox(p, b, Matrix4x4.TRS(pos, rot, Vector3.one));
            R3S_MistParts++;
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
                case "card":
                case "litter": return Layout != null && Layout.Surfaces.ContainsKey("card") ? "card" : "relief";
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
            // スマホの上書き (二周目 レーン D 段2): 部品の "phone": {"hide": true} ならスマホでは組まない (額縁も)。t・s・y・scale はスマホの時だけその値で置く (PlaceOf)
            if (PhoneHidden(p) || R3I_FlagHidden(p)) return;
            PlaceOf(p, out float pt, out float ps, out float py, out float pscale);
            // 置き場 (根のローカル)
            float gy = HeightAtPath(pt, ps);
            var pos = OnPath(pt, ps, p.Abs ? py : gy + py);
            float yaw = PathAligned(p.Kind) ? L.PathYaw + p.Yaw : p.Yaw;
            var rot = Quaternion.Euler(0f, yaw, 0f);
            float scale = pscale;
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
                    // 地面の汚し (二周目 レーン D 段2): "mottle" があれば天面の頂点色に低い周波数の斑。縁の点の間が粗い段 (手前の段は2点) は
                    // 斑が t の向きに出ないので、"gridT" (無ければ mottle の時だけ grid) の間隔まで縁を割る。どちらも無ければ今どおり (割らない・斑なし)
                    var mottle = MottleFor(p, mask);
                    float tStep = p.Num("gridT", mottle != null ? p.Grid : 0f);
                    var b = DioramaMesh.Slab(front, p.Back, p.Top, p.Bottom, p.Grid, p.Chamfer, L.PathYaw, HeightAtPath, mask, mottle, tStep);
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
                case "litter":
                {
                    // 地面の小札 (W3b P22・ユーザー「小石・草の株・ひび・落ち葉の小さな2Dの札を不規則に控えめに散らす」): 札と同じ立った板 (大きさは絵のまま = 1ドット 4px)。
                    // 座席の帯にも置いてよい (点検の「座席の帯の部品」に数えない) ので、背丈 LitterMaxTexels (12 ドット = 0.48 unit) を超える絵は置かない。影は落とさない
                    if (!string.IsNullOrEmpty(p.Src) && ctx.Atlas.Entries.TryGetValue(p.Src, out var le) && le.H > LitterMaxTexels) { ctx.Missing.Add("litter:" + p.Src + " (背丈 " + le.H + " > " + LitterMaxTexels + ")"); return; }
                    var rb = CardFor(ctx, p.Src, p.Flip);
                    if (rb == null) return;
                    pieces.Add(new Piece { Mesh = rb, Surface = surface, Shadow = p.ShadowOr(false) });
                    scale = 1f;   // ドットの粒をそろえる (縮めない)
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
                    Color frameTint;
                    if (PartTint(p, out frameTint)) rb = TintedCopy(rb, frameTint);   // 額縁を暗い影絵に (W3 P22)
                    // 三周目 R14 (レーン S): 揺れの重み (部品の "sway"・材質が揺らせる時だけ。無ければ a = 1 のまま = 二周目)
                    if (R3S_PartSway(p, out var fsw, out var ffrom) && R3S_SwayableSurface(ctx, surface)) { rb = ReliefMesh.R3S_SwayCopy(rb, fsw, ffrom); R3S_SwayParts++; }
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
                    GlowVertexColor(b, p);
                    var go = MakeObject(ctx.DynRoot, "fog-" + p.Index, b.ToMesh("diorama-fog-" + p.Index), surface, false);
                    go.transform.localPosition = pos; go.transform.localRotation = rot;
                    AddBox(p, b, Matrix4x4.TRS(pos, rot, Vector3.one));
                    Dynamic.Add(new DynamicEntry { Name = "fog-" + p.Index, Transform = go.transform, BaseRotation = rot });
                    return;
                }
                case "shaft":
                {
                    var b = DioramaMesh.Shaft(p.Num("top", 0.8f), p.Num("bottom", 2.4f), p.Num("len", 12f), new Rect(0.5f, 0f, 0.5f, 1f));
                    GlowVertexColor(b, p);
                    var go = MakeObject(ctx.DynRoot, "shaft-" + p.Index, b.ToMesh("diorama-shaft-" + p.Index), surface, false);
                    var dir = p.Dir.sqrMagnitude > 1e-6f ? p.Dir.normalized : new Vector3(0.35f, -1f, 0.3f).normalized;
                    go.transform.localPosition = pos;
                    go.transform.localRotation = Quaternion.FromToRotation(Vector3.down, dir);
                    AddBox(p, b, Matrix4x4.TRS(pos, go.transform.localRotation, Vector3.one));
                    Dynamic.Add(new DynamicEntry { Name = "shaft-" + p.Index, Transform = go.transform, BaseRotation = go.transform.localRotation });
                    return;
                }
                case "mist":
                    // 三周目 R8 (レーン S): α合成のノイズ入り霧の板 (StageMist)。まとめない・動かさない
                    R3S_BuildMist(ctx, p, pos, rot);
                    return;
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
            // 映らない部品の数 (dumplayout の offscreenParts。二周目 レーン D 段2)。色を掛ける前の器で数える (半立体のキャッシュの箱を使い回す)
            if (p.Kind != "slab") foreach (var pc in pieces) AddBox(p, pc.Mesh, partM * pc.Local);

            // 部品ごとの色の倍率 (設計図の "tint": 灰の数 か [r, g, b])。頂点色 (AO) に掛ける = 材質は増やさない (W3 P22: 端の幹を暗い影絵に など)
            Color partTint;
            if (PartTint(p, out partTint))
                foreach (var pc in pieces) pc.Mesh = TintedCopy(pc.Mesh, partTint);

            // 三周目 R14 (レーン S): 揺れの重み (部品の "sway" 0〜1。メッシュの UV の材質 = 半立体・札・幹の半立体だけ。地面の小札は揺らさない)
            if (p.Kind != "litter" && R3S_PartSway(p, out var sw, out var swFrom))
            {
                bool any = false;
                foreach (var pc in pieces)
                    if (R3S_SwayableSurface(ctx, pc.Surface)) { pc.Mesh = ReliefMesh.R3S_SwayCopy(pc.Mesh, sw, swFrom); any = true; }
                if (any) R3S_SwayParts++;
            }

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

        /// <summary>
        /// 光の面 (霧の面・光の筋) の部品ごとの明るさと芯の効き (W3b P22)。StageShaft は頂点色を読む: rgb = 明るさの倍率 (設計図の "gain" 0〜1・既定 1)、
        /// a = 霧の光の芯 (_LobeFloor) の効き (設計図の "lobe" 0〜1・既定 1 = 今まで)。光の筋は lobe 0 = 芯から外れた画面の左右でも薄めない (本家の月光の筋は左上から差す)
        /// </summary>
        static void GlowVertexColor(DioramaMeshBuilder b, DioramaPart p)
        {
            float gain = Mathf.Clamp01(p.Num("gain", 1f)), lobe = Mathf.Clamp01(p.Num("lobe", 1f));
            if (gain >= 0.999f && lobe >= 0.999f) return;   // 既定 = 頂点色は白のまま (今まで)
            byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(gain * 255f), 0, 255), a = (byte)Mathf.Clamp(Mathf.RoundToInt(lobe * 255f), 0, 255);
            for (int i = 0; i < b.C.Count; i++) b.C[i] = new Color32(g, g, g, a);
        }

        /// <summary>設計図の部品の "tint" (灰の数 0〜1 か [r, g, b])。無い・読めない・白なら false (W3 P22)</summary>
        static bool PartTint(DioramaPart p, out Color c)
        {
            c = Color.white;
            var t = p.Raw != null ? p.Raw["tint"] : null;
            if (t == null) return false;
            if (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) { float g = Mathf.Clamp01(t.Value<float>()); c = new Color(g, g, g, 1f); }
            else if (t is JArray a && a.Count >= 3) c = new Color(Mathf.Clamp01(a[0].Value<float>()), Mathf.Clamp01(a[1].Value<float>()), Mathf.Clamp01(a[2].Value<float>()), 1f);
            else return false;
            if (!(c.r < 0.999f || c.g < 0.999f || c.b < 0.999f)) return false;
            // 値は見た目の明るさ (sRGB・surfaces の tint と同じ読み)。頂点色は色空間の変換を受けないので、Linear なら線形へ直して掛ける
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) c = c.linear;
            return true;
        }

        /// <summary>頂点色の rgb (AO) に色を掛けた写し (元の器はキャッシュで共有されるので書き換えない)。a (苔の割合) はそのまま</summary>
        static DioramaMeshBuilder TintedCopy(DioramaMeshBuilder src, Color tint)
        {
            var b = new DioramaMeshBuilder();
            b.Append(src, Matrix4x4.identity);
            for (int i = 0; i < b.C.Count; i++)
            {
                var k = b.C[i];
                b.C[i] = new Color32((byte)Mathf.RoundToInt(k.r * tint.r), (byte)Mathf.RoundToInt(k.g * tint.g), (byte)Mathf.RoundToInt(k.b * tint.b), k.a);
            }
            return b;
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

        /// <summary>空き地の縁の幅の既定 (楕円の値 v の InverseLerp の2点)。二周目 レーン D 段2 で段の "maskEdge" から読めるようにした (無ければ今どおり)</summary>
        static readonly Vector2 DefaultMaskEdge = new Vector2(0.72f, 1.08f), DefaultTrailEdge = new Vector2(0.7f, 1.05f);

        /// <summary>
        /// 空き地の苔の割合 (1 = 苔・0 = 土)。楕円 (中心 maskCenter・半径 maskRadius) を値ノイズで揺らし、左右へ延びる踏み跡を足す。
        /// 縁の幅 (二周目 レーン D 段2・stage-05 ①): 段の "maskEdge": [v0, v1] (無ければ [0.72, 1.08])。狭いほど苔と土の混ざる帯 (シェーダがドットごとの閾値で
        /// 切り替える＝ゴマ塩) が細る。踏み跡の縁は "trailEdge" (無ければ maskEdge があればそれ・どちらも無ければ [0.7, 1.05])。
        /// 頂点の間 (grid・縁の点の間隔) より細くはならない (頂点色を面の上で補間する) ので、もっと締めたい時は段の "grid" と "gridT" も小さく
        /// </summary>
        static float ClearingMask(DioramaPart p, float t, float s)
        {
            var c = p.Vec2("maskCenter", new Vector2(3f, 0.3f));
            var rr = p.Vec2("maskRadius", new Vector2(14f, 3.4f));
            var edge = EdgeOf(p.Vec2("maskEdge", DefaultMaskEdge));
            int seed = p.Seed;
            float e = Sq((t - c.x) / Mathf.Max(0.1f, rr.x)) + Sq((s - c.y) / Mathf.Max(0.1f, rr.y));
            float n = DioramaTextures.Fbm(t * 0.35f, s * 0.35f, seed);
            float v = e + (n - 0.5f) * 0.9f;
            float clearing = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge.x, edge.y, v));
            // 踏み跡 (空き地から左右へ細く続く土)
            float trail = p.Num("trail", 1.1f);
            if (trail > 0f)
            {
                var tedge = EdgeOf(p.Vec2("trailEdge", p.Has("maskEdge") ? edge : DefaultTrailEdge));
                float center = c.y + 0.5f * Mathf.Sin(t * 0.17f + seed * 0.01f);
                float d = Mathf.Abs(s - center) / trail + (DioramaTextures.Noise(t * 0.5f, s * 0.5f, seed + 7) - 0.5f) * 0.8f;
                clearing = Mathf.Max(clearing, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(tedge.x, tedge.y, d)));
            }
            return 1f - clearing;
        }

        /// <summary>縁の2点を小さい順に・幅 0.01 以上に (逆や同じ値を書いても割り算にならない)</summary>
        static Vector2 EdgeOf(Vector2 e)
        {
            float a = Mathf.Min(e.x, e.y), b = Mathf.Max(e.x, e.y);
            if (b - a < 0.01f) { float m = (a + b) * 0.5f; a = m - 0.005f; b = m + 0.005f; }
            return new Vector2(a, b);
        }

        // ---------------------------------------------------------------- 地面の汚し (二周目 レーン D 段2・stage-05 ④)

        /// <summary>
        /// 段の "mottle": {"scale": [小, 大] (unit・斑の大きさ), "amount": 暗くする量 (0〜0.6), "seed"} → 天面の頂点色 (AO) に掛ける倍率 (1 = そのまま)。
        /// 2つの大きさの値ノイズの平均が中ほどより上の所だけを暗くする (斑)。空き地の土 (mask "clearing" の 0 の所) と座席の帯
        /// (SeatT0〜SeatT1・SeatS0〜SeatS1 とその外 1 unit) には入れない。キーが無い・amount 0 なら null (今どおり)
        /// </summary>
        static Func<float, float, float> MottleFor(DioramaPart p, Func<float, float, float> mask)
        {
            var mo = p.Raw != null ? p.Raw["mottle"] as JObject : null;
            if (mo == null) return null;
            float amount = Mathf.Clamp(JNum(mo, "amount", 0.1f), 0f, 0.6f);
            if (amount <= 0f) return null;
            Vector2 sc = new Vector2(2f, 5f);
            if (mo["scale"] is JArray sa && sa.Count >= 2) sc = new Vector2(sa[0].Value<float>(), sa[1].Value<float>());
            else if (mo["scale"] != null && (mo["scale"].Type == JTokenType.Float || mo["scale"].Type == JTokenType.Integer)) sc = Vector2.one * mo["scale"].Value<float>();
            float small = Mathf.Max(0.25f, Mathf.Min(sc.x, sc.y)), big = Mathf.Max(small, Mathf.Max(sc.x, sc.y));
            int seed = (int)JNum(mo, "seed", p.Seed + 523);
            return (t, s) =>
            {
                float n = 0.5f * DioramaTextures.Noise(t / small, s / small, seed) + 0.5f * DioramaTextures.Noise(t / big + 3.7f, s / big - 1.9f, seed + 71);
                float spot = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(MottleLo, MottleHi, n));
                float w = mask != null ? Mathf.Clamp01(mask(t, s)) : 1f;
                return 1f - amount * spot * w * SeatFade(t, s);
            };
        }

        /// <summary>斑の閾値 (2つの値ノイズの平均 = 平均 0.52・標準偏差 0.15。0.52 より上から暗くなり始め 0.78 で amount いっぱい。
        /// Python の写しで: 斑のある所 (0.1 以上) が面の 33〜36%・amount いっぱいは 3〜5%・amount 0.1 の平均の暗さ 1.8〜2.1%。scratchpad lane-D/s2/edge_sim.py)</summary>
        const float MottleLo = 0.52f, MottleHi = 0.78f;

        /// <summary>座席の帯の中は 0・外へ 1 unit で 1 に (座席の帯に地面の汚しを入れない)</summary>
        static float SeatFade(float t, float s)
        {
            float dt = Mathf.Max(0f, Mathf.Max(SeatT0 - t, t - SeatT1)), ds = Mathf.Max(0f, Mathf.Max(SeatS0 - s, s - SeatS1));
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Sqrt(dt * dt + ds * ds)));
        }

        static float JNum(JObject o, string key, float def)
        {
            var t = o != null ? o[key] : null;
            return t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? t.Value<float>() : def;
        }

        // ---------------------------------------------------------------- スマホの上書き (二周目 レーン D 段2)

        /// <summary>部品の "phone" (スマホの配置 = UiKit.Phone の時だけ。段 slab は持たない = 地面の高さが PC と変わらない)</summary>
        static JObject PhoneOf(DioramaPart p)
        {
            if (!UiKit.Phone || p == null || p.Raw == null || p.Kind == "slab") return null;
            return p.Raw["phone"] as JObject;
        }

        /// <summary>三周目 統合 (2026-10-02): 旗が無いと組まない部品 ("onlyWith": "uitrial" = 旗 uitrial=1 の時だけ。手札を沈めた試し撮りでだけ見える真ん中の手前の草など)。
        /// 普段の UI では手札の後ろに隠れる部品を、物差し (UI なしの撮影) に混ぜないため。部品の数には数える (PhoneHidden と同じ扱い)</summary>
        public static bool R3I_FlagHidden(DioramaPart p)
        {
            var w = p != null && p.Raw != null ? p.Raw["onlyWith"] : null;
            if (w == null || w.Type != JTokenType.String) return false;
            switch ((string)w)
            {
                case "uitrial": return !HD2DFlags.UiTrial;
                default: return false;
            }
        }

        /// <summary>スマホでは組まない部品 ("phone": {"hide": true})。額縁も (スマホで片側だけの手前の草など)</summary>
        public static bool PhoneHidden(DioramaPart p)
        {
            var h = PhoneOf(p)?["hide"];
            if (h == null) return false;
            if (h.Type == JTokenType.Boolean) return (bool)h;
            return (h.Type == JTokenType.Integer || h.Type == JTokenType.Float) && h.Value<float>() > 0.5f;
        }

        /// <summary>
        /// 部品を置く道の座標・高さ・大きさ。スマホなら "phone" の t・s・y・scale (書いた物だけ) で上書きする。
        /// 額縁は画面の割合で置く (OnCameraLayout が "phone" の vx・vy・depth・scale・roll を読む) ので、ここでは上書きしない
        /// </summary>
        static void PlaceOf(DioramaPart p, out float t, out float s, out float y, out float scale)
        {
            var ph = p.Kind == "frame" ? null : PhoneOf(p);
            t = JNum(ph, "t", p.T);
            s = JNum(ph, "s", p.S);
            y = JNum(ph, "y", p.Y);
            scale = JNum(ph, "scale", p.Scale);
            if (scale <= 0f) scale = 1f;
        }

        // ---------------------------------------------------------------- 映らない部品の数 (二周目 レーン D 段2・任意)

        struct PartBox { public int Index; public string Name; public Bounds Box; }
        static readonly List<PartBox> _partBoxes = new List<PartBox>();

        /// <summary>部品の形 (器 b を m で写した物) の外接の箱 (根のローカル) を部品ごとに足す。組んだ後に DebugInfo が今のカメラへ写して数える</summary>
        static void AddBox(DioramaPart p, DioramaMeshBuilder b, Matrix4x4 m)
        {
            if (b == null || b.V.Count == 0) return;
            var lb = LocalBounds(b);
            var mn = lb.min; var mx = lb.max;
            var box = new Bounds(m.MultiplyPoint3x4(mn), Vector3.zero);
            for (int k = 1; k < 8; k++)
                box.Encapsulate(m.MultiplyPoint3x4(new Vector3((k & 1) != 0 ? mx.x : mn.x, (k & 2) != 0 ? mx.y : mn.y, (k & 4) != 0 ? mx.z : mn.z)));
            for (int i = _partBoxes.Count - 1; i >= 0 && i >= _partBoxes.Count - 4; i--)
            {
                if (_partBoxes[i].Index != p.Index) continue;
                var pb = _partBoxes[i]; pb.Box.Encapsulate(box); _partBoxes[i] = pb;   // 同じ部品の2つ目の形 (幹と根など)
                return;
            }
            _partBoxes.Add(new PartBox { Index = p.Index, Name = PartObjectName(p), Box = box });
        }

        static readonly Dictionary<DioramaMeshBuilder, Bounds> _localBounds = new Dictionary<DioramaMeshBuilder, Bounds>();

        /// <summary>器の頂点の外接の箱 (同じ器 = 半立体のキャッシュは1回だけ数える)</summary>
        static Bounds LocalBounds(DioramaMeshBuilder b)
        {
            if (_localBounds.TryGetValue(b, out var bb)) return bb;
            bb = new Bounds(b.V[0], Vector3.zero);
            for (int i = 1; i < b.V.Count; i++) bb.Encapsulate(b.V[i]);
            _localBounds[b] = bb;
            return bb;
        }

        /// <summary>
        /// 今のカメラ (OnCameraLayout が受けたレイアウトのカメラ = 揺れ・寄り・漂いなし) に1画素も映らない部品を数える。
        /// widen = 画面の左右へ広げる割合 (NDC。0.3333 = 1920 幅の画面の ±320px = 21:9 の余白)。names に部品の名前 (先頭 max 個)
        /// </summary>
        static int CountOffscreen(float widen, List<string> names, int max)
        {
            if (Root == null || !_hasCam) return -1;
            float tanV = Mathf.Tan(Mathf.Clamp(_camFov, 1f, 170f) * 0.5f * Mathf.Deg2Rad);
            float aspect = Screen.width > 0 && Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9f;
            var toWorld = Root.localToWorldMatrix;
            var inv = Quaternion.Inverse(_camRot);
            int count = 0;
            foreach (var pb in _partBoxes)
            {
                var mn = pb.Box.min; var mx = pb.Box.max;
                bool anyFront = false, anyBehind = false;
                float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
                for (int k = 0; k < 8; k++)
                {
                    var w = toWorld.MultiplyPoint3x4(new Vector3((k & 1) != 0 ? mx.x : mn.x, (k & 2) != 0 ? mx.y : mn.y, (k & 4) != 0 ? mx.z : mn.z));
                    var v = inv * (w - _camPos);
                    if (v.z <= 0.05f) { anyBehind = true; continue; }
                    anyFront = true;
                    float nx = v.x / (v.z * tanV * aspect), ny = v.y / (v.z * tanV);
                    x0 = Mathf.Min(x0, nx); x1 = Mathf.Max(x1, nx); y0 = Mathf.Min(y0, ny); y1 = Mathf.Max(y1, ny);
                }
                bool visible = anyFront && (anyBehind || (x1 >= -1f - widen && x0 <= 1f + widen && y1 >= -1f && y0 <= 1f));
                if (visible) continue;
                count++;
                if (names != null && names.Count < max) names.Add(pb.Name);
            }
            return count;
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
            // スマホの配置 (UI 1.6倍) では札の置き場が違うので、額縁の "phone": {vx, vy, depth, scale, roll} があればそちら (W3 P22: 額縁と UI の重なり L8)
            bool phone = UiKit.Phone;
            foreach (var f in _frames)
            {
                if (f.Tr == null) continue;
                var p = f.Part;
                var ph = phone && p.Raw != null ? p.Raw["phone"] as JObject : null;
                float depth = FrameNum(p, ph, "depth", 8f);
                float x = (FrameNum(p, ph, "vx", 0f) - 0.5f) * 2f * tanV * aspect * depth;
                float y = (FrameNum(p, ph, "vy", 0.5f) - 0.5f) * 2f * tanV * depth;
                f.Tr.position = camPos + camRot * new Vector3(x, y, depth);
                f.Tr.rotation = camRot * Quaternion.Euler(0f, 0f, FrameNum(p, ph, "roll", 0f));
                float sc = FrameNum(p, ph, "scale", p.Scale > 0f ? p.Scale : 1f);
                f.Tr.localScale = Vector3.one * (sc > 0f ? sc : 1f);
                if (f.DynIndex >= 0 && f.DynIndex < Dynamic.Count)
                {
                    var e = Dynamic[f.DynIndex];
                    e.BaseRotation = f.Tr.localRotation;
                    Dynamic[f.DynIndex] = e;
                }
            }
        }

        /// <summary>額縁の値: スマホの上書き (ph) にあればそれ、無ければ部品の値</summary>
        static float FrameNum(DioramaPart p, JObject ph, string key, float def)
        {
            var t = ph != null ? ph[key] : null;
            if (t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer)) return t.Value<float>();
            return p.Num(key, def);
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
            o["layout"] = Layout != null ? (!string.IsNullOrEmpty(Layout.Name) ? Layout.Name : LayoutResource(Layout.Act)) : null;
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
            // 地面の配列の異方性 (二周目 レーン D 段1): 組んだ時の段と、品質設定の異方性の方式 (Disable / Enable = テクスチャごと / ForceEnable = 強制)
            o["arrayAniso"] = DioramaTextures.LastArrayAniso;
            o["anisoMode"] = QualitySettings.anisotropicFiltering.ToString();
            // 映らない部品 (二周目 レーン D 段2): 今のレイアウトのカメラに1画素も映らない部品の数 (offscreenParts) と、
            // 左右へ ±320px (1920 幅。21:9 の余白) 広げても映らない数 (offscreenPartsWide = 消してよい候補)。名前は広げた方の先頭 60 個
            var offNames = new List<string>();
            o["offscreenParts"] = CountOffscreen(0f, null, 0);
            o["offscreenPartsWide"] = CountOffscreen(1f / 3f, offNames, 60);
            o["offscreenNames"] = offNames;
            o["phoneHidden"] = st != null ? st.PhoneHidden : 0;
            // 三周目 (レーン S): 点検の門の値 (設計図の "gates" か定数)・法線のアトラス (R7)・霧の板 (R8)・揺れ (R14)
            o["gates"] = st != null ? st.GatesInfo() : new DioramaStats().GatesInfo();
            o["normalAtlas"] = new Dictionary<string, object>
            {
                ["requested"] = R3S_NormalRequested, ["on"] = R3S_NormalOn, ["found"] = R3S_NormalFound, ["total"] = R3S_NormalTotal,
                ["lookAllows"] = R3S_LookFlag("normalAtlas", true),
            };
            o["mistParts"] = R3S_MistParts;
            o["sway"] = new Dictionary<string, object>
            {
                ["parts"] = R3S_SwayParts, ["materials"] = new List<string>(R3S_SwayMaterials), ["lookAllows"] = R3S_LookFlag("sway", true),
            };
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
            _partBoxes.Clear(); _localBounds.Clear();
            Root = null; Layout = null; Active = false; _look = null;
            _pathYaw = DefaultPathYaw;
            // 三周目 (レーン S) の記録
            R3S_NormalRequested = false; R3S_NormalOn = false; R3S_NormalFound = 0; R3S_NormalTotal = 0;
            R3S_MistParts = 0; R3S_SwayParts = 0; R3S_SwayMaterials.Clear();
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

        /// <summary>見本の門 (計画 P04 の確かめ方): 部品の数・Renderer・材質・三角形の上限・座席の帯の高さ |y| &lt; 0.01・座席の帯に部品が無い。
        /// 三周目 (2026-10-01・レーン S・計画 §0 の 7): 部品 200〜450・材質 7 (霧の板の材質 +1)・小札 250・三角形 25 万のまま・Renderer 120 のまま。
        /// 設計図の頂の "gates" ({"partsMin","partsMax","litterMax"} ＋任意で "renderersMax","materialsMax","trianglesMax") があればそちらが勝つ (DioramaStats の G*)。
        /// 二周目までの値: 部品 250〜350・材質 6・小札 80</summary>
        public const int GatePartsMin = 200, GatePartsMax = 450, GateRenderers = 120, GateMaterials = 7, GateTriangles = 250000;
        public const float GateSeatAbsY = 0.01f;
        /// <summary>地面の小札 (kind "litter") の数の上限 (三周目 250。二周目 80) と、1枚の絵の背丈の上限 (ドット。12 = 0.48 unit = 座席で 48px)。控えめに散らす (W3b P22)</summary>
        public const int GateLitter = 250, LitterMaxTexels = 12;

        /// <summary>三周目 (レーン S): 設計図の "gates" を点検の門へ (書いた物だけ。無ければ定数のまま)</summary>
        static void R3S_ApplyGates(DioramaStats st, DioramaLayout layout)
        {
            var g = layout != null && layout.Raw != null ? layout.Raw["gates"] as JObject : null;
            if (g == null) return;
            st.GateSource = "layout";
            st.GPartsMin = (int)JNum(g, "partsMin", st.GPartsMin);
            st.GPartsMax = (int)JNum(g, "partsMax", st.GPartsMax);
            st.GLitter = (int)JNum(g, "litterMax", st.GLitter);
            st.GRenderers = (int)JNum(g, "renderersMax", st.GRenderers);
            st.GMaterials = (int)JNum(g, "materialsMax", st.GMaterials);
            st.GTriangles = (int)JNum(g, "trianglesMax", st.GTriangles);
        }

        /// <summary>座席の帯 (道の座標)。敵4体の奥の席 t=11.2・ひなたの人形の後列 s≈2.05・からくりの匣 (−6.3, 1.7)/(−5.3, −1.9) を含む</summary>
        public const float SeatT0 = -8.5f, SeatT1 = 13f, SeatS0 = -2.6f, SeatS1 = 2.8f;

        /// <summary>いま組んである箱庭を数える (Build の最後にも呼ぶ)</summary>
        public static DioramaStats Check()
        {
            var st = new DioramaStats();
            R3S_ApplyGates(st, Layout);   // 三周目 (レーン S): 設計図の "gates" (無ければ定数)
            if (Layout != null)
            {
                foreach (var p in Layout.Parts)
                {
                    st.Parts++;
                    st.ByKind.TryGetValue(p.Kind, out var c); st.ByKind[p.Kind] = c + 1;
                    if (PhoneHidden(p)) { st.PhoneHidden++; continue; }
                    if (R3I_FlagHidden(p)) continue;   // 三周目 統合: 旗の時だけの部品 ("onlyWith")   // スマホで組まない部品 (二周目 レーン D 段2。部品の数には数える = 設計図の数)
                    if (p.Kind == "litter") { st.Litter++; continue; }   // 地面の小札 (背丈 0.48 unit 以下) は座席の帯にも置く (W3b P22)
                    if (p.Kind == "slab" || p.Kind == "fog" || p.Kind == "shaft" || p.Kind == "frame" || p.Kind == "mist") continue;   // 霧の板 (三周目) も光の面と同じく数えない
                    float reach = p.Kind == "rock" ? p.Num("r", 0.6f) : p.Kind == "block" ? Mathf.Max(p.Num("w", 1.6f), p.Num("d", 1.2f)) * 0.5f
                        : p.Kind == "tree" ? p.Num("r", 0.55f) + p.Num("rootLen", 1.6f) : p.Kind == "fence" ? p.Num("len", 3f) * 0.5f : p.Kind == "rig" ? 1.5f : 0.3f;
                    PlaceOf(p, out float pt, out float ps, out _, out _);   // スマホなら "phone" の t・s
                    if (pt + reach > SeatT0 && pt - reach < SeatT1 && ps + reach > SeatS0 && ps - reach < SeatS1 && !p.Abs)
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
            try { var l = DioramaLayout.Parse(ta.text); l.Name = LayoutResource(act); return l; }
            catch (Exception e) { Debug.LogWarning("[Diorama] 設計図が読めない: " + e.Message); return null; }
        }

        /// <summary>
        /// 名前で設計図を読む (W3b P22。比べる用の別の設計図 = 幕の光の設計図 look の "layout" キー。例 "act1_layout_w3" → Resources/Stage/act1_layout_w3)。
        /// 名前が空・見つからない・読めなければ null (呼び手は既定の設計図のまま)
        /// </summary>
        public static DioramaLayout LoadLayoutNamed(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string path = name.StartsWith("Stage/", StringComparison.Ordinal) ? name : "Stage/" + name;
            var ta = Resources.Load<TextAsset>(path);
            if (ta == null) { Debug.LogWarning("[Diorama] 設計図 " + path + " が無い (既定の設計図で組む)"); return null; }
            try { var l = DioramaLayout.Parse(ta.text); l.Name = path; return l; }
            catch (Exception e) { Debug.LogWarning("[Diorama] 設計図 " + path + " が読めない: " + e.Message); return null; }
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
        public int Parts, Triangles, Renderers, Materials, Dynamic, SeatIntrusions, Litter;
        /// <summary>スマホの配置で組まなかった部品 ("phone": {"hide": true})。二周目 レーン D 段2</summary>
        public int PhoneHidden;
        public float SeatMaxAbsY;
        public readonly Dictionary<string, int> ByKind = new Dictionary<string, int>();
        public readonly List<string> IntrusionNames = new List<string>();
        /// <summary>見つからなかった物 (絵・シェーダ・材質)。W1 では PixelLab の新しい絵が無いので、落ちた候補がここに並ぶ</summary>
        public readonly List<string> Missing = new List<string>();
        public readonly List<string> Failures = new List<string>();
        public bool Ok => Failures.Count == 0;

        /// <summary>三周目 (レーン S): この点検で使う門 (既定は Diorama の定数。設計図の "gates" で上書き)。GateSource = "default" | "layout"</summary>
        public int GPartsMin = Diorama.GatePartsMin, GPartsMax = Diorama.GatePartsMax, GRenderers = Diorama.GateRenderers,
            GMaterials = Diorama.GateMaterials, GTriangles = Diorama.GateTriangles, GLitter = Diorama.GateLitter;
        public string GateSource = "default";

        public void Evaluate()
        {
            Failures.Clear();
            if (Parts < GPartsMin || Parts > GPartsMax) Failures.Add("部品 " + Parts + " (" + GPartsMin + "〜" + GPartsMax + ")");
            if (Renderers > GRenderers) Failures.Add("Renderer " + Renderers + " (" + GRenderers + " 以下)");
            if (Materials > GMaterials) Failures.Add("材質 " + Materials + " (" + GMaterials + " 以下)");
            if (Triangles > GTriangles) Failures.Add("三角形 " + Triangles + " (" + GTriangles + " 以下)");
            if (float.IsNaN(SeatMaxAbsY) || SeatMaxAbsY >= Diorama.GateSeatAbsY) Failures.Add("座席の帯の高さ " + SeatMaxAbsY.ToString("0.000", CultureInfo.InvariantCulture));
            if (SeatIntrusions > 0) Failures.Add("座席の帯の部品 " + SeatIntrusions);
            if (Litter > GLitter) Failures.Add("地面の小札 " + Litter + " (" + GLitter + " 以下)");
        }

        /// <summary>門の値 (dumplayout の extra.diorama.gates・ToJson の gates)</summary>
        public Dictionary<string, object> GatesInfo()
        {
            return new Dictionary<string, object>
            {
                ["source"] = GateSource, ["partsMin"] = GPartsMin, ["partsMax"] = GPartsMax, ["renderersMax"] = GRenderers,
                ["materialsMax"] = GMaterials, ["trianglesMax"] = GTriangles, ["litterMax"] = GLitter, ["seatAbsY"] = Diorama.GateSeatAbsY,
            };
        }

        public string Summary()
        {
            var sb = new StringBuilder();
            sb.Append("check parts=").Append(Parts).Append(" tris=").Append(Triangles).Append(" renderers=").Append(Renderers)
              .Append(" materials=").Append(Materials).Append(" dynamic=").Append(Dynamic)
              .Append(" seatY=").Append(SeatMaxAbsY.ToString("0.0000", CultureInfo.InvariantCulture))
              .Append(" seatParts=").Append(SeatIntrusions)
              .Append(" litter=").Append(Litter)
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
                ["seatIntrusions"] = SeatIntrusions, ["litter"] = Litter, ["phoneHidden"] = PhoneHidden, ["ok"] = Ok,
                ["failures"] = new JArray(Failures.ToArray()), ["missing"] = new JArray(Missing.ToArray()),
                ["intrusions"] = new JArray(IntrusionNames.ToArray()),
                ["gates"] = new JObject   // 三周目 (レーン S)。IL2CPP で安全なよう手で組む (FromObject は使わない)
                {
                    ["source"] = GateSource, ["partsMin"] = GPartsMin, ["partsMax"] = GPartsMax, ["renderersMax"] = GRenderers,
                    ["materialsMax"] = GMaterials, ["trianglesMax"] = GTriangles, ["litterMax"] = GLitter,
                },
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
        /// <summary>光の面: 霧の光の芯から外れた所の明るさの倍率 (StageShaft の _LobeFloor。1 = 芯を見ない) (W3 P22)</summary>
        public float LobeFloor = 1f;
        /// <summary>配列の材質で法線の配列 _Normal を読む (_NormalArrayOn)</summary>
        public bool NormalArray = true;
        public Color Tint = Color.white;
        /// <summary>三周目 R7 (レーン S): "normal" = アトラスの材質の法線アトラスの強さ (0 = 読まない = 二周目)</summary>
        public float NormalAtlas;
        /// <summary>三周目 R14 (レーン S): "sway" = {"amp": 振幅 unit (0 = 揺れない), "freq": [Hz, Hz], "phase": rad/unit}。SwayFreq = (Hz1, Hz2, phase)</summary>
        public float SwayAmp;
        public Vector3 SwayFreq = new Vector3(0.23f, 0.61f, 0.12f);
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
        /// <summary>読んだ Resources のパス (dumplayout の extra.diorama.layout。W3b P22)</summary>
        public string Name;
        public float PathYaw = Diorama.DefaultPathYaw;
        public int Tile = 64;
        /// <summary>色の配列を linear:true で作り sRGB の色をそのまま入れる (シェーダが Linear の時だけ戻す = _AlbedoDecode 1)。計画 P04 の手順2。false = sRGB の配列 (_AlbedoDecode 0)</summary>
        public bool AlbedoLinear = true;
        public bool StaticBatch = true;
        public float NormalStrength = 0.6f;
        /// <summary>PC の地面の配列の異方性の段 (頂の "anisoLevel"・0〜16。無ければ DioramaTextures.AnisoPc = 8。tier=phone は設計図に依らず 1)。二周目 レーン D 段1</summary>
        public int AnisoPc = DioramaTextures.AnisoPc;
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
            L.AnisoPc = Mathf.Clamp((int)F(root, "anisoLevel", DioramaTextures.AnisoPc), 0, 16);

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
                    var sd = new DioramaSurface
                    {
                        Shader = S(o, "shader") ?? "array", Side = S(o, "side"), Top = S(o, "top"), Tile = S(o, "tile"),
                        Receive = F(o, "receive", 0.8f), ShadowStrength = F(o, "shadowStrength", 1f), VColorAO = F(o, "vcolorAO", 1f),
                        TopThreshold = F(o, "topThreshold", 0.65f), TopBlend = F(o, "topBlend", 0.15f), Cutoff = F(o, "cutoff", 0.4f),
                        Intensity = F(o, "intensity", 1f), SoftDepth = F(o, "softDepth", 1.5f), NearFade = F(o, "nearFade", 2f), Fog = F(o, "fog", 0.3f),
                        EdgeFade = F(o, "edgeFade", 0f), NormalArray = B(o, "normalArray", true), LobeFloor = F(o, "lobeFloor", 1f),
                        Tint = Col(o, "tint", Color.white),
                        NormalAtlas = Mathf.Max(0f, F(o, "normal", 0f)),   // 三周目 R7 (レーン S)
                    };
                    // 三周目 R14 (レーン S): 揺れ {"amp", "freq": [f1, f2], "phase"}。無ければ揺れない
                    if (o["sway"] is JObject so)
                    {
                        sd.SwayAmp = Mathf.Max(0f, F(so, "amp", 0f));
                        var fq = so["freq"] as JArray;
                        float f1 = fq != null && fq.Count >= 1 ? fq[0].Value<float>() : sd.SwayFreq.x;
                        float f2 = fq != null && fq.Count >= 2 ? fq[1].Value<float>() : (fq != null && fq.Count == 1 ? f1 * 2.63f : sd.SwayFreq.y);
                        sd.SwayFreq = new Vector3(f1, f2, F(so, "phase", sd.SwayFreq.z));
                    }
                    L.Surfaces[kv.Key] = sd;
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
