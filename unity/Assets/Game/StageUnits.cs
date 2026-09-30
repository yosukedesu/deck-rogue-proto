// StageUnits.cs — Stage のキャラの板 (2026-09-30 HD-2D 見本。計画 docs/design/hd2d-slice-plan-2026-09-30.md §3・§2-3)。
// P02 (W1) で Stage.cs から移した部分 (中身は1文字も変えていない): アニメの状態・板の明暗と主役の照明の定数・板の登録簿 (_bound・_depths)・
// BindUnit・FeetPad・PlayAnim・DebugAnim・Flash・Dissolve・StageUnit・光と影の共通部品 (SpriteMat・VignetteLift・ApplyLight・BlobTex・Blob・PlaceBlob)。
// P11 (W2) が足した部分 (計画 §2-3・§2-5・P11):
//   アダプタ   … stage=diorama の時、板の位置を「座席の世界の点 (TryGetSeat) ＋ 休んでいる時の矩形との差 (カメラの横と上 × px × 座席の k)」で決める。
//               板は回さず、向きはレイアウト用のカメラの回転 (揺れ・寄り・漂いを含まない)。式は今の ScreenToPlane と数学的に同じ (単体の検査 = adapter の範囲)。
//               stage=old は今のコードをそのまま通す (1画素も変えない)
//   光を受ける板 … litunits=1 の時、材質を StageUnitLit へ差し替える (旗が変われば LateUpdate が差し替え直す)。コマごとに法線 _n・発光 _e を差し替え、
//               _KeyFlip は art-lint の表 (Art/stage/act1/keyflip)、受光・主役の持ち上げ・環境光の倍率は StageLook (旗 receive=・herolift= が勝つ)
//   影         … stage=diorama か charshadow=1 の時、板はレイヤー8 (HD2DLayers.StageUnit)・Rendering Layer は Characters。charshadow=1 で影を落とし、接地影の楕円は濃さ 0.35 倍・幅 0.6 倍
//   48 の見本  … herodots=48 の絵 (Theme.cs の Creature.Get が leader_green_48 を返す) も主役の照明と杖の先の光の表で同じに扱う
//   材質の漏れ … 板が消える時に材質 (と接地影・杖の先の光の材質) を捨てる
//   口         … TryGetUnitBox (板の足元と高さ。StageFx.UnitPoint・技の光が読む)・DebugUnitBoxes (矩形と板のずれ。dumplayout の stage.unitBoxes)
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static partial class Stage
    {
        struct AnimState { public string Anim; public int Frame; public float FrameT; public float At; }
        static readonly Dictionary<string, AnimState> _animStates = new Dictionary<string, AnimState>();
        // キャラの板の明暗 (2026-09-16 ユーザー「このはも反射と影がすごくない？」): 月光の勾配は板の中で右上 1+0.7×A・左下 1−0.7×A、リムは右上の縁を空色へ寄せる割合。
        // 旧 0.5／0.5 は太い墨線のこのは v2 で「左半分が影・右の縁が光る」と読めた → 0.25／0.2 に緩めた (敵も同じ板なので同時に緩む)
        const float UnitSunAmount = 0.25f, UnitRim = 0.2f;
        // 敵と人形の板のリム (2026-09-29 I24 で 0.3 に上げた): 環境光を白寄りにした分、暗い墨線の敵 (オーガ・コボルト) の輪郭を足元の地面から離すため。
        // 2026-09-30 F05 (ユーザー裁定「主人公を持ち上げる」): 敵と人形の縁がリーダー (0.2) より強く光り「主役の照明」の序列が逆になった → 主役とそろえる
        const float CharRim = UnitRim;
        // リーダーの板の環境光 (主役の照明)。旧 1.15 = ACES と色補正が沈める中間調を戻す値 (2026-09-16)。
        // 2026-09-30 F05 裁定で 1.35: 敵を白寄りの環境光にした後 (I24)、このはの体 (線形 Y 0.064) が狼 0.21・妖術師 0.23 より暗く画面でいちばん暗いキャラになっていた。
        // 縁の光ではなく体の中間調を持ち上げる (09-16「反射と影がすごい」の裁定＝勾配とリムは触らない)
        const float HeroAmbient = 1.35f;
        // 主役の照明はリーダーの絵ごとに変えられる (2026-09-30 F05 の続き)。1.35 のままでは、このはの体が幕1で線形 Y 0.077〜0.083 にとどまり、目標 0.09 に届かなかった。
        // 原因は光より絵の側: 元の絵の明るさ (線形 Y) は このは 0.086 (黒鉄の衣と墨線)・妖術師 0.315・狼 0.340 で、このはだけ最初から敵の約4分の1。
        // ひなたは白い衣が幕3ですでに白に近いので 1.35 のまま (共用の値を上げると衣が飛ぶ)。このはだけ 1.5 = 予測 Y 0.090〜0.096・白飛び 1% のまま。
        // 敵÷このは ≤2.2 には 1.6 が要り、その時は白飛び 5% になるので 1.5 で止める (クリーム色・白い敵の場面の比は次の裁定)
        static float HeroAmbientFor(string art)
        {
            // 名前の頭で見る (2026-09-30 P11): herodots=48 の縮めた見本 leader_green_48 も同じ絵なので同じ 1.5
            return art != null && art.StartsWith("leader_green", StringComparison.Ordinal) ? 1.5f : HeroAmbient;
        }
        // 杖の先の光の位置 (板の中の UV。x 0=左・1=右、y 0=足元・1=上端) は絵ごとの表 (2026-09-30 P11)。表に無い絵は今の値 (0.64, 0.93) = 板の右上。
        // P05 の art-lint: 48 は 62 を足元の中央を基準に同じ倍率で縮めたので、板の中の UV はほぼ同じ (斧の宝石 62 (0.686,0.721)／48 (0.689,0.717)、
        // 柄の真鍮の先 62 (0.215,0.411)／48 (0.213,0.405))。今の (0.64, 0.93) は v2 では斧の頭の上。どこに置くかは P23 の詰め (見た目は今のまま)
        static readonly Vector2 DefaultHaloUv = new Vector2(0.64f, 0.93f);
        static readonly Dictionary<string, Vector2> HaloUvByArt = new Dictionary<string, Vector2>
        {
            { "leader_green", new Vector2(0.64f, 0.93f) },
            { "leader_green_48", new Vector2(0.64f, 0.93f) },
        };
        static Vector2 HaloUvFor(string art)
        {
            Vector2 uv;
            return art != null && HaloUvByArt.TryGetValue(art, out uv) ? uv : DefaultHaloUv;
        }
        // ランタンの暖色は、リーダーの環境光が 1.15 の時にシェーダの lamp×1.15 と R がそろう値。環境光だけ上げるとランタン側 (左) の方が暗くなる
        // (lerp(1.35, lamp×1.15, lf) の G・B が下がる = 光源の側が暗い逆の陰影。このはの体は lf 0.36〜0.80 でランタンに近く、1.35 の半分しか効かない) →
        // リーダーだけランタンの色も (主役の照明)/1.15 倍にして、暖色の差 (近い側が暖かい) はそのままに体全体を 1.35/1.15 倍 (このは 1.5/1.15 倍) にする
        const float HeroLampRef = 1.15f;
        // 人形の板の環境光 = 小物・木の UnitAmbient と敵の CharAmbient の間 (2026-09-30 F05 裁定 Lerp 0.6)。
        // 人形は味方の板なので敵ほど白く立てず、ひなた (主役) を頭一つ上に残す (I24 の後は盾の人形・聖歌の人形がひなたの 85% まで来ていた)
        const float DollAmbientMix = 0.6f;
        static readonly Dictionary<string, StageUnit> _bound = new Dictionary<string, StageUnit>();
        static readonly Dictionary<string, float> _depths = new Dictionary<string, float>();

        /// <summary>
        /// キャラの板 (key = "player"・"enemy0"…・"doll:&lt;uid&gt;") の足元の中心の世界の点と、板の世界の高さ。演出で動いている時はその位置。
        /// 無ければ false。
        /// feet = 絵の足元 (板の下端から絵の下の余白 FeetPad ぶん上 = 地面に着いている点。呼吸・踏み込み・縮みなどの演出の動きを含む)。
        /// heightWorld = 足元から板の上端までの世界の長さ (広い枠のコマはそのぶん高い)。板はレイアウト用のカメラの回転を向く (StageFx.UnitPoint がこれで板の上の点を作る)。
        /// 値は最後の LateUpdate の時のもの (演出の途中で聞けば1フレーム前)。stage=old でも答える
        /// </summary>
        public static bool TryGetUnitBox(string key, out Vector3 feet, out float heightWorld)
        {
            feet = default; heightWorld = 0f;
            StageUnit u;
            if (string.IsNullOrEmpty(key) || !_bound.TryGetValue(key, out u) || u == null || !u.HasBox) return false;
            feet = u.FeetWorld; heightWorld = u.HeightWorld;
            return heightWorld > 0f;
        }

        /// <summary>
        /// 「UI の矩形の画面の箱」と「板をレイアウト用のカメラで写した箱」を並べた記録 (JSON にできる値。dumplayout の stage.unitBoxes)。
        /// 1体1つの辞書の並び: key・rectPx (矩形)・boardPx (板を写した箱から、決めてあるずらし = 絵の下の余白・呼吸・広い枠のコマの拡大 を外した箱 = 矩形と同じ物差し)・
        /// boardRawPx (板を写した箱そのもの)・dev (rectPx と boardPx の角のずれの最大・px)・moving (演出中)・tol (静止 1・演出中 2)・over (dev が tol を超えた)・
        /// pxPerDot (板の幅 ÷ いまのコマの絵の幅 = 座席での1ドットの大きさ)・ほか (mode・lit・depth・座席の k の突き合わせ・板の向きとレイアウトの回転の差)。
        /// 箱は [x, y, w, h] = PNG の画素 (左上が原点・y は下向き。layout.json の px と同じ)
        /// </summary>
        public static object DebugUnitBoxes()
        {
            var list = new List<object>();
            if (_cam == null) return list;
            var cam = CurrentLayoutCam();
            var layoutRot = LayoutRot();
            float H = Screen.height;
            foreach (var kv in _bound)
            {
                var u = kv.Value;
                if (u == null || !u.HasBox || u.Rect == null) continue;
                var e = new Dictionary<string, object>();
                e["key"] = kv.Key;
                e["mode"] = u.InDiorama ? "diorama" : "old";
                e["lit"] = u.IsLit;
                e["art"] = u.ArtName;
                e["anim"] = u.Anim + "#" + u.Frame;
                // 矩形 (LateUpdate が読んだ角) と、板の四隅をレイアウト用のカメラで写した箱
                var rect = new Vector4(u.RectMin.x, u.RectMin.y, u.RectMax.x, u.RectMax.y);
                var t = u.transform;
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                bool ok = true;
                foreach (var v in _quadCorners)
                {
                    float d;
                    var p = cam.Project(t.TransformPoint(v), out d);
                    if (!(d > 0.01f)) { ok = false; break; }
                    x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y);
                }
                e["rectPx"] = PngBox(rect, H);
                if (ok)
                {
                    var raw = new Vector4(x0, y0, x1, y1);
                    var board = NormalizeBoardBox(raw, u.FeetPadPx, u.BreathePx, u.FrameScaleX, u.FrameScaleY);
                    float dev = BoxDev(rect, board);
                    float tol = u.Moving ? 2f : 1f;
                    e["boardRawPx"] = PngBox(raw, H);
                    e["boardPx"] = PngBox(board, H);
                    e["dev"] = Mathf.Round(dev * 100f) / 100f;
                    e["tol"] = tol;
                    e["over"] = dev > tol;
                    float texW = u.CurrentTexelWidth();
                    e["pxPerDot"] = texW > 0f ? Mathf.Round((x1 - x0) / texW * 1000f) / 1000f : 0f;
                }
                else e["error"] = "板がカメラの後ろ";
                e["moving"] = u.Moving;
                e["feetPadPx"] = u.FeetPadPx;
                e["breathePx"] = Mathf.Round(u.BreathePx * 100f) / 100f;
                e["frameScale"] = new Vector2(u.FrameScaleX, u.FrameScaleY);
                e["depth"] = u.UsedDepth;
                e["k"] = u.UsedK;
                e["seat"] = u.SeatFound;
                if (u.SeatFound) { e["seatWorld"] = u.SeatWorld; e["seatK"] = u.SeatK; }
                e["feetWorld"] = u.FeetWorld;
                e["heightWorld"] = u.HeightWorld;
                // 板の向き: 見本はレイアウト用のカメラの回転 (_fwd・_up から作る)。Stage.LayoutRotation (P10) との差も並べる (約束のずれの見張り)
                e["rotVsLayoutDeg"] = Mathf.Round(Quaternion.Angle(t.rotation, layoutRot) * 100f) / 100f;
                e["rotVsLayoutRotationDeg"] = Mathf.Round(Quaternion.Angle(t.rotation, LayoutRotation) * 100f) / 100f;
                e["layer"] = u.gameObject.layer;
                e["renderingLayerMask"] = u.Rend != null ? u.Rend.renderingLayerMask : 0u;
                e["castShadows"] = u.Rend != null && u.Rend.shadowCastingMode != ShadowCastingMode.Off;
                if (u.IsLit) { e["keyFlip"] = u.KeyFlipArt; e["hasNormal"] = u.CurNormal != null; e["hasEmission"] = u.CurEmission != null; }
                list.Add(e);
            }
            return list;
        }

        static readonly Vector3[] _quadCorners = { new Vector3(-0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f), new Vector3(0.5f, 0f, 0f) };

        /// <summary>画面の箱 (x0, y0, x1, y1。左下が原点) → PNG の箱 [x, y, w, h] (左上が原点・0.1px に丸め)</summary>
        static float[] PngBox(Vector4 b, float H)
        {
            Func<float, float> r = v => Mathf.Round(v * 10f) / 10f;
            return new[] { r(b.x), r(H - b.w), r(b.z - b.x), r(b.w - b.y) };
        }

        /// <summary>今のレイアウト用のカメラ (ProjectFeet・ScreenToPlane と同じ静的な値)</summary>
        static LayoutCam CurrentLayoutCam()
        {
            return new LayoutCam { Base = _camBase, Fwd = _fwd, Right = _right, Up = _up, K = _k, Dist = _dist, W = Screen.width, H = Screen.height };
        }

        /// <summary>
        /// 見本の板の向き = レイアウト用のカメラの回転。ProjectFeet・ScreenToPlane が使う _fwd・_up から作る (= LayoutCamera の回転。揺れ・寄り・漂いを含まない)。
        /// Stage.LayoutRotation (P10) と同じ物のはずで、DebugUnitBoxes が差を並べる
        /// </summary>
        static Quaternion LayoutRot()
        {
            if (_fwd.sqrMagnitude < 1e-8f || _up.sqrMagnitude < 1e-8f) return CameraRotation;
            return Quaternion.LookRotation(_fwd, _up);
        }

        // ==== adapter-begin (単体の検査 scratchpad/hd2d/check-P11 がこの範囲を取り出して .NET で回す。Unity の API は Vector2〜4 の算術と Mathf だけ)
        /// <summary>
        /// レイアウト用のカメラ (揺れ・寄り・漂いを含まない): 位置 Base・向き Fwd/Right/Up・基準深度 Dist で1px あたり K unit・画面 W×H (px・左下が原点)。
        /// Project と Unproject は今の ProjectFeet・ScreenToPlane と同じ式
        /// </summary>
        internal struct LayoutCam
        {
            public Vector3 Base, Fwd, Right, Up;
            public float K, Dist, W, H;
            /// <summary>深さ depth での k (画面 1px あたりの unit)</summary>
            public float KAt(float depth) { return K * depth / Dist; }
            /// <summary>画面の点 (px) → 深さ depth の面 (視線に垂直) の世界の点 (= ScreenToPlane)</summary>
            public Vector3 Unproject(float sx, float sy, float depth)
            {
                float k = KAt(depth);
                return Base + Fwd * depth + Right * ((sx - W * 0.5f) * k) + Up * ((sy - H * 0.5f) * k);
            }
            /// <summary>世界の点 → 画面の点 (px・左下が原点) と深さ (= ProjectFeet の画面の px。キャンバスの倍率で割る前)</summary>
            public Vector2 Project(Vector3 world, out float depth)
            {
                var rel = world - Base;
                depth = Vector3.Dot(rel, Fwd);
                float k = KAt(depth);
                return new Vector2(W * 0.5f + Vector3.Dot(rel, Right) / k, H * 0.5f + Vector3.Dot(rel, Up) / k);
            }
        }

        /// <summary>
        /// アダプタ (計画 §2-3): 板の足元 (矩形の下端の中心 sx, sy・px) を、座席の世界の点 seat からのずれに変える。
        /// 休んでいる時の矩形の点 = seat をレイアウト用のカメラで写した点 (rest)。いまの矩形との差 (sx−rest.x, sy−rest.y) を、
        /// 座席の深さの面の横 (Right) と上 (Up) × 座席の k で世界へ戻して seat に足す。
        /// seat = Base + Fwd·d + Right·x + Up·y、rest = (W/2 + x/k, H/2 + y/k) なので、結果は Unproject(sx, sy, d) (= 今の ScreenToPlane) と同じ式になる
        /// (休んでいる時は seat そのもの。演出で矩形が動けば、そのぶんだけ座席の面で動く)
        /// </summary>
        internal static Vector3 AdapterFeet(LayoutCam c, Vector3 seat, float sx, float sy, out float k, out float depth)
        {
            var rest = c.Project(seat, out depth);
            k = c.KAt(depth);
            return seat + c.Right * ((sx - rest.x) * k) + c.Up * ((sy - rest.y) * k);
        }

        /// <summary>
        /// 板を写した箱 raw (x0, y0, x1, y1。px・左下が原点) から、決めてあるずらしを外して矩形と同じ物差しにする:
        /// 絵の下の余白 (板を feetPadPx 下げて足を地面に着けた)・呼吸 (breathePx 上下)・広い枠のコマの拡大 (幅 fsx 倍は足元の中央を軸・高さ fsy 倍は下端を軸)
        /// </summary>
        internal static Vector4 NormalizeBoardBox(Vector4 raw, float feetPadPx, float breathePx, float fsx, float fsy)
        {
            float cx = (raw.x + raw.z) * 0.5f;
            float w = (raw.z - raw.x) / Mathf.Max(0.0001f, fsx);
            float h = (raw.w - raw.y) / Mathf.Max(0.0001f, fsy);
            float y0 = raw.y + feetPadPx - breathePx;
            return new Vector4(cx - w * 0.5f, y0, cx + w * 0.5f, y0 + h);
        }

        /// <summary>2つの箱 (x0, y0, x1, y1) の角のずれの最大 (px)</summary>
        internal static float BoxDev(Vector4 a, Vector4 b)
        {
            return Mathf.Max(Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)), Mathf.Max(Mathf.Abs(a.z - b.z), Mathf.Abs(a.w - b.w)));
        }
        // ==== adapter-end

        // ---- 光を受ける板 (litunits=1) の共通の値 ----
        static Shader _litShader; static bool _litShaderTried;
        /// <summary>StageUnitLit (無い・使えない時は null = 今の StageUnit のまま。警告は1回)</summary>
        static Shader LitShader()
        {
            if (_litShaderTried) return _litShader;
            _litShaderTried = true;
            var s = Shader.Find("DeckRogue/StageUnitLit");
            if (s == null || !s.isSupported) { Debug.LogWarning("[Stage] StageUnitLit が無いか使えない → litunits=1 でも今の StageUnit のまま"); s = null; }
            _litShader = s;
            return _litShader;
        }

        static HashSet<string> _keyFlipArts;
        /// <summary>画像ファイルを左右反転した絵 (P05 の art-lint が書く Resources/Art/stage/act1/keyflip の "keyflip")。_KeyFlip=1 にする絵の名前</summary>
        static bool IsKeyFlipArt(string art)
        {
            if (_keyFlipArts == null)
            {
                _keyFlipArts = new HashSet<string>(StringComparer.Ordinal);
                try
                {
                    var ta = Resources.Load<TextAsset>("Art/stage/act1/keyflip");
                    if (ta != null)
                    {
                        var arr = JObject.Parse(ta.text)["keyflip"] as JArray;
                        if (arr != null) foreach (var x in arr) { var n = (string)x; if (!string.IsNullOrEmpty(n)) _keyFlipArts.Add(n); }
                    }
                    else Debug.LogWarning("[Stage] Art/stage/act1/keyflip が無い → _KeyFlip は全部 0");
                }
                catch (Exception ex) { Debug.LogWarning("[Stage] keyflip を読めない: " + ex.Message); }
            }
            return art != null && _keyFlipArts.Contains(art);
        }

        /// <summary>
        /// 設計図のキャラの塊 (look_act1.char.json の "char") の、StageLook の型に無い材質の値 (P23 が詰める口)。書いていなければシェーダの既定のまま。
        /// localLights (技の光・逆光を受ける割合。既定 0)・whiteCap・emissionIntensity・outlineFloor [r,g,b]・receiveShadows・cookieOnKey
        /// </summary>
        sealed class CharMatExtras
        {
            public float LocalLights = -1f, WhiteCap = -1f, EmissionIntensity = -1f, ReceiveShadows = -1f, CookieOnKey = -1f;
            public Color OutlineFloor; public bool HasOutlineFloor;
        }
        static StageLookData _extrasFor; static CharMatExtras _extras;
        static CharMatExtras CharExtras()
        {
            var cur = StageLook.Current;
            if (_extras != null && ReferenceEquals(_extrasFor, cur)) return _extras;
            var x = new CharMatExtras();
            try
            {
                var c = cur != null && cur.Raw != null ? cur.Raw["char"] as JObject : null;
                if (c != null)
                {
                    Func<string, float> num = name => { var t = c[name]; return t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? (float)t : -1f; };
                    x.LocalLights = num("localLights"); x.WhiteCap = num("whiteCap"); x.EmissionIntensity = num("emissionIntensity");
                    x.ReceiveShadows = num("receiveShadows"); x.CookieOnKey = num("cookieOnKey");
                    var of = c["outlineFloor"] as JArray;
                    if (of != null && of.Count >= 3) { x.OutlineFloor = new Color((float)of[0], (float)of[1], (float)of[2], 1f); x.HasOutlineFloor = true; }
                }
            }
            catch (Exception ex) { Debug.LogWarning("[Stage] look の char を読めない: " + ex.Message); }
            _extrasFor = cur; _extras = x;
            return x;
        }

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
            mat.SetFloat("_Rim", UnitRim);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;   // 影は接地影 (楕円) で
            mr.receiveShadows = false;
            var sh = Blob("shadow-" + key, _units, Vector3.zero, 1f);
            var u = go.AddComponent<StageUnit>();
            u.Rect = rect; u.Img = img; u.Mat = mat; u.Rend = mr; u.Depth = depth; u.Shadow = sh.transform;
            u.FeetPad = FeetPad(sprite);   // 絵の下端の透明行 (足元の余白) の割合。板をそのぶん下げて足を地面に着ける (2026-09-08「キャラが地面から浮いてる」)
            u.BaseTex = sprite.texture;
            u.BaseUvScale = new Vector2(tr.width / tw, tr.height / th); u.BaseUvOffset = new Vector2(tr.x / tw, tr.y / th);
            u.BreathePhase = (key.GetHashCode() & 0xff) * 0.05f;   // 位相をずらして全員が同期しない
            // コマ: Art/<種別>/anim/<id>_<動き>_<n>.png (64×64。PixelLab の animate-with-text)。無ければ一枚絵のまま
            string cat = key == "player" ? "leaders" : "enemies";
            foreach (var anim in new[] { "idle", "attack", "hurt", "block" })
            {
                var list = new List<Texture2D>();
                for (int i = 0; i < 16; i++)
                {
                    var f = Theme.Art(cat + "/anim", sprite.name + "_" + anim + "_" + i);
                    if (f == null) break;
                    list.Add(f.texture);
                }
                if (list.Count > 0) u.Anims[anim] = list;
            }
            u.Breathe = !u.Anims.ContainsKey("idle");   // 待機のコマ (PixelLab の呼吸) があるならコードの上下はしない (2026-09-16)
            // このは v2 (2026-09-16): PixelLab の17コマから 頭上→溜め(刃が光る)→着弾→食い込み の4コマ。着弾は 0.13 秒後 (Presenter の踏み込み 0.11 に合わせる)
            u.FrameDur["attack"] = new[] { 0.05f, 0.08f, 0.04f, 0.18f };
            u.FrameDur["hurt"] = new[] { 0.06f, 0.16f, 0.10f };
            u.FrameDur["block"] = new[] { 0.06f, 0.08f, 0.10f, 0.20f };   // 9コマの 2,4,6,8 = 斧を前に回して構える
            // 盤面の作り直し (Rebuild) で板が作り直されても、再生中のコマ送りは引き継ぐ (攻撃コマが Rebuild で消えていた)
            u.Key = key;
            // 光を受ける板の法線・発光と _KeyFlip の引き先 (2026-09-30 P11)。絵の名前 = Resources の名前 (leader_green・enemy_wolf・人形の id)
            u.ArtName = sprite.name;
            u.BaseFolder = key == "player" ? "leaders" : key.StartsWith("doll:", StringComparison.Ordinal) ? "dolls" : "enemies";
            u.AnimFolder = cat + "/anim";
            u.KeyFlipArt = IsKeyFlipArt(sprite.name);
            u.InitShadowState();
            AnimState st0;
            if (_animStates.TryGetValue(key, out st0) && st0.Anim != "idle" && u.Anims.ContainsKey(st0.Anim) && Time.time - st0.At < 2f)
            {
                u.Anim = st0.Anim; u.Frame = st0.Frame; u.FrameT = st0.FrameT; u.Apply();
            }
            else if (u.Anims.ContainsKey("idle")) u.Play("idle");
            if (key == "player")
            {
                u.HeroLight = HeroAmbientFor(sprite.name);   // 主役の照明はリーダーの絵ごと (F05 の続き)
                // 杖の先の光: 板の右上 (絵の uv≈0.64,0.93) に追従する淡い暖色のハロー
                var halo = new GameObject("staff-glow");
                halo.transform.SetParent(_units, false);
                halo.AddComponent<MeshFilter>().sharedMesh = _quadCentered;
                var hmr = halo.AddComponent<MeshRenderer>();
                hmr.sharedMaterial = GlowMaterial(Px.Radial(new Color(1f, 0.86f, 0.5f, 0.5f)));
                hmr.shadowCastingMode = ShadowCastingMode.Off; hmr.receiveShadows = false;
                u.Halo = halo.transform; u.HaloUv = HaloUvFor(sprite.name);   // 絵ごとの表 (表に無ければ今の 0.64, 0.93)
            }
            u.LateUpdate();
            _bound[key] = u;
        }

        static readonly Dictionary<Texture2D, float> _feetPad = new Dictionary<Texture2D, float>();
        /// <summary>絵の矩形の中で、いちばん下の不透明ドットより下にある透明行の割合 (0〜1)。PixelLab の絵は下に数ドットの余白がある</summary>
        static float FeetPad(Sprite sprite)
        {
            var t = sprite.texture;
            float pad;
            if (_feetPad.TryGetValue(t, out pad)) return pad;
            pad = 0f;
            try
            {
                var r = sprite.textureRect;
                var px = t.GetPixels32();
                int w = t.width, x0 = Mathf.RoundToInt(r.x), y0 = Mathf.RoundToInt(r.y), x1 = Mathf.RoundToInt(r.x + r.width), y1 = Mathf.RoundToInt(r.y + r.height);
                for (int y = y0; y < y1 && pad == 0f; y++)
                    for (int x = x0; x < x1; x++)
                        if (px[y * w + x].a > 40) { pad = (y - y0) / Mathf.Max(1f, r.height); break; }
                if (pad >= 0.5f) pad = 0f;   // 半分以上が透明なら絵ではなく別物 (安全弁)
            }
            catch (System.Exception) { pad = 0f; }
            _feetPad[t] = pad;
            return pad;
        }

        /// <summary>コマ送りの再生 (attack/hurt/block は1回、終わると待機へ)。コマが無ければ何もしない</summary>
        public static void PlayAnim(string key, string anim)
        {
            StageUnit u;
            bool ok = _bound.TryGetValue(key, out u) && u != null;
            Debug.Log("[Stage] PlayAnim " + key + " " + anim + " t=" + Time.realtimeSinceStartup.ToString("F2") + " dt=" + Time.deltaTime.ToString("F3") + " bound=" + ok + (ok ? " has=" + u.Anims.ContainsKey(anim) : ""));
            if (ok) u.Play(anim);
        }

        /// <summary>デバッグ: いま表示中のコマ</summary>
        public static string DebugAnim(string key)
        {
            StageUnit u;
            if (!_bound.TryGetValue(key, out u) || u == null) return "(unbound)";
            return u.Anim + "#" + u.Frame + " t=" + Time.realtimeSinceStartup.ToString("F2") + " tex=" + (u.Mat != null && u.Mat.mainTexture != null ? u.Mat.mainTexture.name : "-");
        }

        /// <summary>被弾の白い点滅</summary>
        public static void Flash(string key, float dur = 0.18f)
        {
            StageUnit u;
            if (_bound.TryGetValue(key, out u) && u != null) u.FlashT = dur;
        }

        /// <summary>撃破の崩れ (2026-09-17 ユーザー「倒した敵は消えるようにしたほうが良くない？」): 0→1 でドットが頭から消えていき、接地影も薄くなる</summary>
        public static void Dissolve(string key, float k)
        {
            StageUnit u;
            if (_bound.TryGetValue(key, out u) && u != null) u.DissolveK = Mathf.Clamp01(k);
        }

        class StageUnit : MonoBehaviour
        {
            public RectTransform Rect; public Image Img; public Material Mat; public MeshRenderer Rend; public float FlashT; public float DissolveK; public float Depth; public Transform Shadow;
            public Transform Halo; public Vector2 HaloUv; public float FeetPad;
            public float HeroLight = HeroAmbient;   // リーダーの板の主役の照明 (絵ごと。HeroAmbientFor)。敵と人形では使わない
            // コマ送り (2026-09-09 このはの戦闘アニメ): 待機はループ、攻撃/被弾/防御は1回流して待機へ戻る。ドットは拡大・回転せず絵を差し替えるだけ
            public Texture2D BaseTex;
            public Dictionary<string, List<Texture2D>> Anims = new Dictionary<string, List<Texture2D>>();
            public Dictionary<string, float[]> FrameDur = new Dictionary<string, float[]>();   // コマごとの秒 (緩急)。無ければ fps で均等
            public string Anim = "idle"; public int Frame; public float FrameT; public float Fps = 8f; public bool Breathe = true; public float BreathePhase; public string Key;
            static readonly Vector3[] _c = new Vector3[4];

            // ---- 2026-09-30 P11 (HD-2D 見本) ----
            // 絵の名前と引き先 (法線 <名前>_n・発光 <名前>_e・コマ <名前>_<動き>_<n>_n)
            public string ArtName, BaseFolder, AnimFolder;
            public bool KeyFlipArt;                         // art-lint の表で左右反転した絵
            public bool IsLit;                              // 材質が StageUnitLit (litunits=1)
            bool _mapsLoaded;
            Texture2D _baseN, _baseE;
            readonly Dictionary<string, List<Texture2D>> _animN = new Dictionary<string, List<Texture2D>>(), _animE = new Dictionary<string, List<Texture2D>>();
            public Texture2D CurNormal, CurEmission;       // いまのコマの法線・発光 (無ければ null)
            // 影の置き方 (レイヤー・Rendering Layer・影を落とすか)。作った時の値を覚え、旗が変わった時だけ書く (撮影の unitsonly がその場でレイヤーを動かすのを毎フレーム戻さない)
            int _layer0; uint _mask0; int _shadowSig = int.MinValue;
            public bool CharShadowOn;
            // 最後の LateUpdate の値 (TryGetUnitBox・DebugUnitBoxes が読む)
            public bool HasBox, InDiorama, SeatFound, Moving;
            public Vector3 FeetWorld, SeatWorld; public float HeightWorld, SeatK, UsedDepth, UsedK;
            public Vector2 RectMin, RectMax;                // 矩形の角 (px・左下が原点。丸める前)
            public float FeetPadPx, BreathePx;
            Vector4 _lastRect; int _movedFrame = -100;
            public void Play(string anim)
            {
                if (!Anims.ContainsKey(anim) || Anims[anim].Count == 0) return;
                Anim = anim; Frame = 0; FrameT = 0f;
                if (Key != null) _animStates[Key] = new AnimState { Anim = Anim, Frame = Frame, FrameT = FrameT, At = Time.time };
                Apply();
            }
            public float FrameScaleX = 1f, FrameScaleY = 1f;   // 枠が元絵より大きいコマ (攻撃の 96px) の拡大率
            public void Apply()
            {
                List<Texture2D> frames;
                Texture2D tex = Anims.TryGetValue(Anim, out frames) && frames.Count > 0 ? frames[Mathf.Clamp(Frame, 0, frames.Count - 1)] : BaseTex;
                if (tex == null || Mat == null) return;
                Mat.SetTexture("_BaseMap", tex); Mat.mainTexture = tex;
                if (BaseTex != null && tex != BaseTex)
                {
                    FrameScaleX = tex.width / (float)BaseTex.width; FrameScaleY = tex.height / (float)BaseTex.height;
                    Mat.SetTextureScale("_BaseMap", Vector2.one); Mat.SetTextureOffset("_BaseMap", Vector2.zero);
                }
                else
                {
                    FrameScaleX = FrameScaleY = 1f;
                    Mat.SetTextureScale("_BaseMap", BaseUvScale); Mat.SetTextureOffset("_BaseMap", BaseUvOffset);
                }
                if (IsLit) ApplyMaps(tex);
            }
            public Vector2 BaseUvScale = Vector2.one, BaseUvOffset = Vector2.zero;

            /// <summary>いまのコマの絵の幅 (テクセル。一枚絵は絵の矩形の幅)。pxPerDot の分母</summary>
            public float CurrentTexelWidth()
            {
                var tex = Mat != null ? Mat.mainTexture : null;
                if (tex == null) return 0f;
                if (tex == BaseTex) return BaseUvScale.x * tex.width;
                return tex.width;
            }

            /// <summary>光を受ける板: いまのコマの法線 _NormalMap と発光 _EmissionMap を差し替える (無いコマは _HasNormal・_HasEmission を 0)</summary>
            void ApplyMaps(Texture tex)
            {
                EnsureMaps();
                Texture2D n = null, e = null;
                if (tex == BaseTex) { n = _baseN; e = _baseE; }
                else
                {
                    List<Texture2D> l;
                    if (_animN.TryGetValue(Anim, out l) && Frame >= 0 && Frame < l.Count) n = l[Frame];
                    if (_animE.TryGetValue(Anim, out l) && Frame >= 0 && Frame < l.Count) e = l[Frame];
                }
                CurNormal = n; CurEmission = e;
                Mat.SetTexture("_NormalMap", n); Mat.SetFloat("_HasNormal", n != null ? 1f : 0f);
                Mat.SetTexture("_EmissionMap", e); Mat.SetFloat("_HasEmission", e != null ? 1f : 0f);
            }

            /// <summary>法線と発光の絵を1回だけ引く (P05 の sprite-normals.py の出力。一枚絵 = Art/&lt;種別&gt;/&lt;名前&gt;_n、コマ = Art/&lt;種別&gt;/anim/&lt;名前&gt;_&lt;動き&gt;_&lt;n&gt;_n)</summary>
            void EnsureMaps()
            {
                if (_mapsLoaded) return;
                _mapsLoaded = true;
                if (string.IsNullOrEmpty(ArtName)) return;
                _baseN = MapTex(BaseFolder, ArtName + "_n");
                _baseE = MapTex(BaseFolder, ArtName + "_e");
                foreach (var kv in Anims)
                {
                    var ln = new List<Texture2D>(); var le = new List<Texture2D>();
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        string f = ArtName + "_" + kv.Key + "_" + i;
                        ln.Add(MapTex(AnimFolder, f + "_n"));
                        le.Add(MapTex(AnimFolder, f + "_e"));
                    }
                    _animN[kv.Key] = ln; _animE[kv.Key] = le;
                }
            }

            static Texture2D MapTex(string folder, string name)
            {
                if (string.IsNullOrEmpty(folder)) return null;
                var s = Theme.Art(folder, name);
                return s != null ? s.texture : null;
            }

            /// <summary>
            /// 材質を旗に合わせる: litunits=1 なら StageUnitLit、そうでなければ今の StageUnit。合っていれば何もしない (stage=old・旗なしでは毎フレーム即戻る)。
            /// 差し替えたら古い材質は捨てる
            /// </summary>
            void EnsureMaterial()
            {
                bool want = HD2DFlags.LitUnits && LitShader() != null;
                if (want == IsLit || Mat == null || BaseTex == null) return;
                Material m;
                if (want)
                {
                    m = new Material(LitShader());
                    m.SetTexture("_BaseMap", BaseTex); m.mainTexture = BaseTex;
                    m.SetFloat("_Cutoff", 0.4f);
                    m.SetFloat("_Fog", 1f);
                }
                else m = SpriteMat(BaseTex, 0.4f, 0.35f);
                var old = Mat;
                Mat = m; IsLit = want;
                if (Rend != null) Rend.sharedMaterial = m;
                Apply();   // いまのコマの絵と UV (光を受ける板なら法線・発光も)
                if (!IsLit) { CurNormal = null; CurEmission = null; }
                Destroy(old);
            }

            /// <summary>光を受ける板の材質の値 (毎フレーム。旗 receive=・herolift=・keyflip= と設計図 look の char が変わっても追う)</summary>
            void ApplyLitProps(bool hero)
            {
                Mat.SetFloat("_Receive", StageLook.CharReceive(hero));
                Mat.SetFloat("_HeroLift", hero ? StageLook.HeroLift : 1f);
                Mat.SetFloat("_AmbientScale", StageLook.CharAmbientScale);
                Mat.SetFloat("_KeyFlip", KeyFlipArt && HD2DFlags.KeyFlip == HD2DKeyFlip.Auto ? 1f : 0f);
                var x = CharExtras();
                Mat.SetFloat("_LocalLights", x.LocalLights >= 0f ? x.LocalLights : 0f);
                if (x.WhiteCap >= 0f) Mat.SetFloat("_WhiteCap", x.WhiteCap);
                if (x.EmissionIntensity >= 0f) Mat.SetFloat("_EmissionIntensity", x.EmissionIntensity);
                if (x.ReceiveShadows >= 0f) Mat.SetFloat("_ReceiveShadows", x.ReceiveShadows);
                if (x.CookieOnKey >= 0f) Mat.SetFloat("_CookieOnKey", x.CookieOnKey);
                if (x.HasOutlineFloor) Mat.SetColor("_OutlineFloor", x.OutlineFloor);
            }

            /// <summary>作った時のレイヤー・Rendering Layer を覚える (旗なしの時はこの値のまま一度も書かない)</summary>
            public void InitShadowState()
            {
                _layer0 = gameObject.layer;
                _mask0 = Rend != null ? Rend.renderingLayerMask : 1u;
                _shadowSig = ShadowSig(_layer0, _mask0, false);
            }

            static int ShadowSig(int layer, uint mask, bool cast) { return (layer & 0xff) | ((int)(mask & 0xffff) << 8) | (cast ? 1 << 24 : 0); }

            /// <summary>
            /// 影の置き方 (計画 P11 手順3): stage=diorama か charshadow=1 なら板はレイヤー8 (StageUnit)・Rendering Layer は Default＋Characters (bit0・bit1。Environment は無し)
            /// (舞台の灯 = スポットの影のレイヤーだけに載り、月の影 (Environment) には載らない = キャラの影は1方向に1本)。charshadow=1 で影を落とす。
            /// どれも立っていなければ作った時の値。旗が変わった時だけ書く
            /// </summary>
            void SyncShadowMode(bool dio)
            {
                bool cs = HD2DFlags.CharShadow;
                bool staged = dio || cs;
                int layer = staged ? HD2DLayers.StageUnit : _layer0;
                // Default (bit0) は残す (Diorama の部品が 1|Environment にしているのと同じ形)。月の影 (Environment だけ) には載らず、舞台の灯 (Characters) には載る
                uint mask = staged ? (1u | HD2DLayers.RenderingCharacters) : _mask0;
                int sig = ShadowSig(layer, mask, cs);
                CharShadowOn = cs;
                if (sig == _shadowSig) return;
                _shadowSig = sig;
                gameObject.layer = layer;
                if (Rend != null)
                {
                    Rend.renderingLayerMask = mask;
                    Rend.shadowCastingMode = cs ? ShadowCastingMode.On : ShadowCastingMode.Off;
                }
            }
            void Advance()
            {
                List<Texture2D> frames;
                if (Key != null) _animStates[Key] = new AnimState { Anim = Anim, Frame = Frame, FrameT = FrameT, At = Time.time };
                if (!Anims.TryGetValue(Anim, out frames) || frames.Count == 0) return;
                float fps = Anim == "idle" ? 4f : Fps;
                float dur = 1f / fps;
                float[] durs;
                if (FrameDur.TryGetValue(Anim, out durs) && Frame < durs.Length) dur = durs[Frame];
                FrameT += Mathf.Min(Time.deltaTime, 0.04f);   // 長いフレーム (Rebuild の直後・端末のもたつき) が1回でコマを何枚も飛ばさないよう上限 (2026-09-16: 攻撃4コマが1フレームで消えていた)
                if (FrameT < dur) return;
                FrameT -= dur;
                Frame++;
                if (Frame >= frames.Count)
                {
                    if (Anim == "idle") Frame = 0;
                    else { Anim = "idle"; Frame = 0; }
                }
                Apply();
            }
            // 板が消える時に、この板だけの材質を捨てる (2026-09-30 P11: Rebuild のたびに板の材質・接地影の材質・杖の先の光の材質と絵が漏れていた)。
            // 接地影の絵 (BlobTex) は全員で共有なので捨てない。杖の先の光の絵 (Px.Radial) は板ごとに作っているので捨てる
            void OnDestroy()
            {
                if (Shadow != null)
                {
                    var smr = Shadow.GetComponent<MeshRenderer>();
                    if (smr != null && smr.sharedMaterial != null) Destroy(smr.sharedMaterial);
                    Destroy(Shadow.gameObject);
                }
                if (Halo != null)
                {
                    var hmr = Halo.GetComponent<MeshRenderer>();
                    if (hmr != null && hmr.sharedMaterial != null)
                    {
                        var htex = hmr.sharedMaterial.mainTexture;
                        Destroy(hmr.sharedMaterial);
                        if (htex != null) Destroy(htex);
                    }
                    Destroy(Halo.gameObject);
                }
                if (Mat != null) Destroy(Mat);
            }
            public void LateUpdate()
            {
                if (Rect == null) { Destroy(gameObject); return; }
                Advance();
                Rect.GetWorldCorners(_c);
                float sx = Mathf.Round((_c[0].x + _c[3].x) * 0.5f), sy = Mathf.Round(_c[0].y);
                float w = Mathf.Round(_c[3].x - _c[0].x), h = Mathf.Round(_c[1].y - _c[0].y);
                // 見本 (stage=diorama) はアダプタ (計画 §2-3): 座席の世界の点＋休んでいる時の矩形との差。座席が無い (TryGetSeat が false) 時は今の式。
                // どちらも同じ画面の箱になる (adapter の範囲の注記)。stage=old は今の式をそのまま (1画素も変えない)
                bool dio = HD2DFlags.StageMode == HD2DStage.Diorama;
                float k; Vector3 pos;
                Vector3 seat; float seatK;
                SeatFound = false;
                if (dio && Key != null && TryGetSeat(Key, out seat, out seatK) && TryAdapter(seat, sx, sy, out pos, out k))
                {
                    SeatFound = true; SeatWorld = seat; SeatK = seatK;
                }
                else
                {
                    k = _k * Depth / _dist;
                    pos = ScreenToPlane(sx, sy, Depth);
                    UsedDepth = Depth;
                }
                UsedK = k;
                var ground = pos;
                pos -= _up * (FeetPad * h * k);   // 絵の余白ぶん下げる = 足が地面の点に着く (影は地面の点のまま)
                float breathe = 0f;
                if (Anim == "idle" && Breathe)
                {
                    breathe = Mathf.Sin(Time.time * (2.4f + BreathePhase * 0.08f) + BreathePhase) * (Key == "player" ? 2f : 3f);
                    pos += _up * (breathe * k);   // 呼吸: ±2〜3px の上下 (拡大・回転はしない)。周期も個体ごとに少しずらす (⑩ 2026-09-17)
                }
                var rot = dio ? LayoutRot() : CameraRotation;   // 見本の板はレイアウト用のカメラの回転 (漂い・揺れで回さない)
                transform.position = pos;
                transform.rotation = rot;
                transform.localScale = new Vector3(Mathf.Max(0.01f, w * k * FrameScaleX), Mathf.Max(0.01f, h * k * FrameScaleY), 1f);   // 広い枠のコマは同じドット密度で板を広げる (足元中央は固定)
                RecordBox(dio, pos, k, w, h, breathe);
                EnsureMaterial();   // litunits の旗に材質を合わせる (旗なしなら何もしない)
                var tint = Img != null ? Img.color : Color.white;
                Mat.SetColor("_BaseColor", tint);
                // この経路を通るのは BindUnit で置いたキャラの板だけ (player・enemyN・人形)。リーダー = 主役の照明、それ以外 = キャラの環境光 (2026-09-29 I24)
                bool hero = Key == "player";
                bool doll = !hero && Key != null && Key.StartsWith("doll:", StringComparison.Ordinal);   // 人形 (BattleScreen.FillDollPanel の key) = 敵と主役の間の環境光 (F05)
                if (IsLit) ApplyLitProps(hero);   // 光を受ける板: 固定のキー＋上下の環境光 (StageLook の全体値)。見本は PalOf・主役の照明・周辺減光の打ち消しを使わない (計画 §2-4)
                else
                {
                    // 敵と人形は絵の真ん中の画面の位置で周辺減光を打ち消す (F06: 同じ噛みつく巻物が ①→④ で明るさ半分・青く曇った)
                    Color lift = hero || Screen.width <= 0 || Screen.height <= 0 ? Color.white : VignetteLift(sx / Screen.width, (sy + h * 0.5f * (1f - FeetPad)) / Screen.height);
                    ApplyLight(Mat, UnitSunAmount, hero, !hero, lift, doll, HeroLight);
                }
                Mat.SetFloat("_Rim", hero ? UnitRim : CharRim);
                if (FlashT > 0f) FlashT -= Time.deltaTime;
                Mat.SetFloat("_Flash", Mathf.Clamp01(FlashT / 0.18f) * 0.85f);
                Mat.SetFloat("_Dissolve", DissolveK);
                SyncShadowMode(dio);
                if (Shadow != null)
                {
                    float ww = w * k;
                    float sa = tint.a * (1f - DissolveK);
                    if (CharShadowOn) { ww *= 0.6f; sa *= 0.35f; }   // 本物の影 (舞台の灯) が落ちる時は、足元の接地影の楕円を濃さ 0.35 倍・幅 0.6 倍に (計画 P11 手順3)
                    // 見本は座席の地面の高さ (帯は平ら)。old は今どおり 0
                    PlaceBlob(Shadow, new Vector3(ground.x, SeatFound ? SeatWorld.y : 0f, ground.z), ww, sa);
                }
                if (Halo != null)
                {
                    float ww = w * k, hh = h * k;
                    Halo.position = pos + _right * ((HaloUv.x - 0.5f) * ww) + _up * (HaloUv.y * hh) - _fwd * 0.05f;
                    Halo.rotation = rot;
                    float sz = 0.9f * (1f + 0.06f * Mathf.Sin(Time.time * 5f));
                    Halo.localScale = new Vector3(sz, sz, 1f);
                }
            }

            /// <summary>アダプタ: レイアウト用のカメラで座席 seat を写し、矩形の足元 (sx, sy) との差を座席の深さの面で足す。深さが正でなければ false (今の式へ)</summary>
            bool TryAdapter(Vector3 seat, float sx, float sy, out Vector3 pos, out float k)
            {
                float d;
                pos = AdapterFeet(CurrentLayoutCam(), seat, sx, sy, out k, out d);
                if (!(d > 0.01f) || !(k > 0f) || float.IsNaN(pos.x) || float.IsInfinity(pos.x)) { pos = default; k = 0f; return false; }
                UsedDepth = d;
                return true;
            }

            /// <summary>TryGetUnitBox・DebugUnitBoxes の値を記録する (矩形の角・絵の足元・高さ・決めてあるずらし・演出中か)</summary>
            void RecordBox(bool dio, Vector3 pos, float k, float w, float h, float breathe)
            {
                InDiorama = dio;
                FeetWorld = pos + _up * (FeetPad * h * k);   // 絵の足元 (地面の点。呼吸と演出の動きを含む)
                HeightWorld = Mathf.Max(0f, h * k * FrameScaleY - FeetPad * h * k);
                RectMin = new Vector2(_c[0].x, _c[0].y); RectMax = new Vector2(_c[2].x, _c[2].y);
                FeetPadPx = FeetPad * h; BreathePx = breathe;
                var r = new Vector4(_c[0].x, _c[0].y, _c[2].x, _c[2].y);
                if ((r - _lastRect).sqrMagnitude > 1e-4f) _movedFrame = Time.frameCount;
                _lastRect = r;
                // 演出中 = 矩形がこの2フレームで動いた・待機でないコマ・点滅・崩れ・呼吸のない絵の揺れ (許すずれは 2px)
                Moving = Time.frameCount - _movedFrame <= 2 || Anim != "idle" || FlashT > 0f || DissolveK > 0f;
                HasBox = true;
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

        /// <summary>周辺減光の打ち消し (2026-09-30 F06): 敵と人形の板の環境光に掛ける係数。URP の Vignette と同じ式 (d=|uv−center|×強さ×3、f=(1−d·d)^(滑らかさ×5)、
        /// 出力＝入力×lerp(色,1,f)) を画面の座標 (0〜1) で求め、手前の席 (f≈0.93) を 1 として奥の席ほど持ち上げる。チャンネルごと＝青の曇りも戻す。
        /// 上限 1.35 (これ以上はブルームの足切りを奥の席だけ越え、④だけに光のにじみが出る)。額縁の暗さ (リーダー・小物・木・空) はそのまま</summary>
        static Color VignetteLift(float ux, float uy)
        {
            if (_vig == null || !_vig.active) return Color.white;
            float sc = _vig.intensity.value * 3f;
            var c = _vig.center.value;
            float dx = Mathf.Abs(ux - c.x) * sc, dy = Mathf.Abs(uy - c.y) * sc;
            float f = Mathf.Pow(Mathf.Clamp01(1f - (dx * dx + dy * dy)), _vig.smoothness.value * 5f);
            const float fRef = 0.93f;   // 手前の敵の席の減光 (pc06 ①・pc01 の狼の胴で約 0.93〜0.95)
            var vc = _vig.color.value;
            Func<float, float> ch = col =>
            {
                float v = Mathf.Lerp(col, 1f, f), vr = Mathf.Lerp(col, 1f, fRef);
                return Mathf.Clamp(vr / Mathf.Max(0.5f, v), 1f, 1.35f);
            };
            return new Color(ch(vc.r), ch(vc.g), ch(vc.b), 1f);
        }

        static void ApplyLight(Material m, float sunAmount, bool hero = false, bool character = false) { ApplyLight(m, sunAmount, hero, character, Color.white); }

        static void ApplyLight(Material m, float sunAmount, bool hero, bool character, Color lift, bool doll = false, float heroLight = HeroAmbient)
        {
            bool painted = _pal.LampOnUnits > 0f;
            // hero = リーダーの板: 夜の環境光 (0.8〜0.96) を掛けず源の色で立つ = 主役の照明 (2026-09-16 ユーザー裁定。2026-09-30 F05 で 1.15→1.35、このはだけ 1.5 = heroLight)
            // character = 敵と人形の板: ほぼ白で少しだけ冷たい CharAmbient (2026-09-29 ユーザー裁定「敵の環境光を白寄りに」)。
            //   旧は小物と同じ UnitAmbient (幕1 0.58/0.65/0.94 = 線形で明るさ 0.4 倍の青いフィルタ) で、クリーム・砂の絵が青灰になり地面に溶けていた。
            //   doll = 人形は UnitAmbient と CharAmbient の間 (F05)。小物・木の板 (SpriteMat の既定) は UnitAmbient のまま = 森の暗さと夜の青は変えない
            Color amb = hero ? new Color(heroLight, heroLight, heroLight)   // hero は ACES と色補正が中間調を沈めるぶん戻し、白寄りにした敵 (I24) に対して主役を持ち上げる (F05)
                : painted ? (character ? (doll ? Color.Lerp(_pal.UnitAmbient, _pal.CharAmbient, DollAmbientMix) : _pal.CharAmbient) * lift : _pal.UnitAmbient)
                : Color.white;
            amb.a = 1f;
            m.SetColor("_Ambient", amb);
            m.SetVector("_LampPos", _lampPos);
            // リーダーのランタンの暖色は環境光と同じ倍率 (HeroLampRef の注記)。プロジェクトはガンマ色空間なので SetColor の値がそのままシェーダに届く
            Color lamp = painted ? (hero ? _pal.Lantern * (heroLight / HeroLampRef) : _pal.Lantern) : Color.black;
            lamp.a = 1f;
            m.SetColor("_LampColor", lamp);
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
    }
}
