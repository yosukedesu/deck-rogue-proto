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
// P23 (W3) が足した部分 (計画 P23。光を受ける板だけ = litunits=1。stage=old と litunits=0 は1画素も変えない):
//   輪郭の持ち上げ … look の char の blackLift を、トーンマップと露出で割り戻して _BlackLift に書く (BlackLiftVector)
//   _KeyFlip       … 描き込まれた光の向きを測った表 (art-lint の lightFromRight) で選べるようにした (ResolveKeyFlip・look の char の keyFlipFrom)
//   絵ごとの上書き … look の char の art (このはだけ主役の持ち上げ 1.6・ひなたの輪郭の持ち上げは半分)。露出の割り戻し … look の char の exposureRef (CharExposureScale)
// P23 (W3b・2周目「本家っぽく」) が足した部分 (同じく光を受ける板だけ):
//   輪郭の目標     … look の char の outlineTarget (画面の輝度) から、今の後処理を灰色で逆にたどって _BlackLift を絵ごとに解く
//                    (BlackLiftForTarget・PostGrayDisplay/PostGrayInverse・ViewFor。舞台 P22 が露出・コントラスト・LGG・周辺減光を変えても輪郭の暗さを保つ)
//   キャラの色     … _CharTint = look の char の tint × cancelColorFilter (後処理の colorFilter をキャラだけ打ち消す = 舞台の青がキャラの白に乗らない。CharTintFor)
//   暗い色の持ち上げ … _ShadeLift (look の char の art の shadeLift。このはの黒鉄の衣が ACES の足で潰れるのを戻す。ShadeLiftFor)
//   発光の強さ     … look の char の art の emission (狼の白い毛の発光 1.6 が白飛び → ブルームで青く冷えていた)
// 三周目 レーン E (2026-10-01・計画 docs/design/hd2d-round3-plan-2026-10-01.md §2 E。箱庭の時だけ・look の char のキーが無ければ二周目と同じ):
//   足元ほど暗い勾配 … char.bodyShade → _BodyShade (編成の頭数 3 以上の敵と人形だけ。R3E_ApplyLitExtras)
//   輪郭の1画素     … char.edgeSoft → _EdgeSoft・_EdgeA2C (MSAA が効いている時だけ。裁定 Q2 = 規約の例外。R3E_MsaaOn)
//   座席の環境光    … char.seatAmbient (敵の座席の番号)・dollBackAmbient (人形の後列) を _AmbientScale に掛ける (R3E_SeatAmbientMul)
//   待機の位相      … char.idlePhaseSpread で座席の番号ごとに散らす (R3E_IdleOffset・R3E_BreathePhase)
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
        // 二周目 レーン E (2026-10-01・char C11): 箱庭で look の char.heroGem がある時だけ、杖の先の光を「斧の宝石」(このは)・「ランタンの火」(ひなた) へ置き、
        // そこに点光源 (宝石の灯) を作る。今の表 HaloUvByArt は触らない (今の舞台の杖の光は動かさない)。
        // 値は P05 の art-lint (このは 62 = (0.686, 0.721)・48 = (0.689, 0.717)) と、ひなたの一枚絵の橙の火の画素の重心 (レーン E が測った (0.766, 0.574))
        static readonly Dictionary<string, Vector2> R2E_HaloUvByArtDiorama = new Dictionary<string, Vector2>
        {
            { "leader_green", new Vector2(0.686f, 0.721f) },
            { "leader_green_48", new Vector2(0.689f, 0.717f) },
            { "leader_white", new Vector2(0.766f, 0.574f) },
        };
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
                if (u.IsLit)
                {
                    e["keyFlip"] = u.KeyFlipArt; e["hasNormal"] = u.CurNormal != null; e["hasEmission"] = u.CurEmission != null;
                    // P23: 材質に書いた光の値 (主役の持ち上げ・輪郭の持ち上げ〔線形〕・受光)
                    if (u.Mat != null)
                    {
                        var bl = u.Mat.GetVector("_BlackLift");
                        e["heroLift"] = Mathf.Round(u.Mat.GetFloat("_HeroLift") * 1000f) / 1000f;
                        e["receive"] = Mathf.Round(u.Mat.GetFloat("_Receive") * 1000f) / 1000f;
                        e["blackLift"] = new[] { Mathf.Round(bl.x * 10000f) / 10000f, Mathf.Round(bl.y * 10000f) / 10000f, Mathf.Round(bl.z * 10000f) / 10000f };
                        e["edgeLin"] = Mathf.Round(u.EdgeLin() * 100000f) / 100000f;   // 絵の輪郭の暗さ (線形。-1 = 測れない)
                        // P23 2周目: キャラの色の掛け算・暗い色の持ち上げ・発光の強さ・輪郭の目標を解いた時の周辺減光と霧
                        Func<float, float> r4 = v => Mathf.Round(v * 10000f) / 10000f;
                        e["charTint"] = new[] { r4(u.LastTint.x), r4(u.LastTint.y), r4(u.LastTint.z) };
                        e["shadeLift"] = new[] { r4(u.LastShade.x), r4(u.LastShade.y) };
                        e["emission"] = r4(u.LastEmission);
                        e["outlineVignette"] = r4(u.LastVignette);
                        e["outlineFog"] = r4(u.LastFog);
                        // 二周目 レーン E: 環境光の倍率・霧を受ける割合・鮮やかさ・リム・点光源を受ける割合 (look の char が効いたかの確かめ)
                        e["ambientScale"] = r4(u.Mat.GetFloat("_AmbientScale"));
                        e["fogOnUnit"] = r4(u.Mat.GetFloat("_Fog"));
                        e["charSat"] = r4(u.Mat.GetFloat("_CharSat"));
                        e["rim"] = r4(u.Mat.GetFloat("_Rim"));
                        e["localLights"] = r4(u.Mat.GetFloat("_LocalLights"));
                        // 三周目 レーン E: 足元ほど暗い勾配・輪郭の1画素・座席の環境光の倍率 (look の char が効いたかの確かめ)
                        var bsv = u.Mat.GetVector("_BodyShade");
                        e["bodyShade"] = new[] { r4(bsv.x), r4(bsv.y), r4(bsv.z), r4(bsv.w) };
                        e["edgeSoft"] = r4(u.Mat.GetFloat("_EdgeSoft"));
                        e["edgeA2C"] = u.Mat.GetFloat("_EdgeA2C") > 0.5f;
                        e["seatAmbient"] = r4(u.R3E_LastSeatAmb);
                    }
                }
                e["r3Slot"] = u.R3E_LastSlot;               // 三周目 レーン E: 座席の番号 (敵 = enemyN の N・人形 = DollSlots の番号。-1 = 主人公・分からない)
                e["r3Group"] = u.R3E_LastGroup;             // 敵の編成の頭数の見当 (座席の表から。0 = 敵でない)
                e["r3IdleOffset"] = Mathf.Round(u.R3E_LastIdleOffset * 1000f) / 1000f;   // 待機の位相 (秒。二周目の式 = 位相 × 0.37)
                e["r2idle"] = u.R2E_IdleInfo;   // 二周目 レーン E: 待機のコマ (off = 使っていない・on = r2idle のコマ・missing = 欲しいが絵が無い)
                e["contactCore"] = u.R2E_ContactInfo;
                e["contactDepth"] = u.R2E_ContactDepthInfo;   // 段2: 接地影の奥行きの倍率 (1 = W5)
                if (u.R2E_GemInfo != null) e["gemLight"] = u.R2E_GemInfo;
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

        static HashSet<string> _keyFlipArts, _lightFromRightArts;
        /// <summary>
        /// P05 の art-lint が書く Resources/Art/stage/act1/keyflip の2つの表を1回だけ読む:
        /// "keyflip" = 画像ファイルを左右反転した絵・"lightFromRight" = 描き込まれた光が右から来る絵 (測った向き。lightDx ≥ 0.10・大きさ ≥ 0.12)
        /// </summary>
        static void LoadKeyFlipTables()
        {
            if (_keyFlipArts != null) return;
            _keyFlipArts = new HashSet<string>(StringComparer.Ordinal);
            _lightFromRightArts = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                var ta = Resources.Load<TextAsset>("Art/stage/act1/keyflip");
                if (ta != null)
                {
                    var o = JObject.Parse(ta.text);
                    Action<string, HashSet<string>> read = (name, set) =>
                    {
                        var arr = o[name] as JArray;
                        if (arr != null) foreach (var x in arr) { var n = (string)x; if (!string.IsNullOrEmpty(n)) set.Add(n); }
                    };
                    read("keyflip", _keyFlipArts);
                    read("lightFromRight", _lightFromRightArts);
                }
                else Debug.LogWarning("[Stage] Art/stage/act1/keyflip が無い → _KeyFlip は全部 0");
            }
            catch (Exception ex) { Debug.LogWarning("[Stage] keyflip を読めない: " + ex.Message); }
        }

        /// <summary>画像ファイルを左右反転した絵 (P05 の art-lint の "keyflip")。P11 の選び方 (look の char に keyFlipFrom が無い時) の _KeyFlip=1 の絵</summary>
        static bool IsKeyFlipArt(string art)
        {
            LoadKeyFlipTables();
            return art != null && _keyFlipArts.Contains(art);
        }

        /// <summary>描き込まれた光が右から来る絵 (P05 の art-lint の "lightFromRight")</summary>
        static bool IsLightFromRightArt(string art)
        {
            LoadKeyFlipTables();
            return art != null && _lightFromRightArts.Contains(art);
        }

        /// <summary>
        /// _KeyFlip=1 にする絵か (2026-09-30 P23)。look の char の keyFlipFrom で選び方を決める:
        ///   "measured" = 描き込まれた光が右から来る絵 (lightFromRight)。反転した絵でも光が左から描かれていれば 0 (art-lint の表では反転した16枚のうち9枚が左からの光だった)
        ///   "mirrored" = 画像ファイルを左右反転した絵 (P11 の選び方・keyFlipFrom が無い時の既定)
        ///   "both"     = どちらか
        /// そのうえで keyFlipAdd の絵は 1、keyFlipRemove の絵は 0 (表の境目の絵を手で直す口)
        /// </summary>
        static bool ResolveKeyFlip(string art, CharMatExtras x)
        {
            if (string.IsNullOrEmpty(art)) return false;
            if (x != null && x.KeyFlipRemove.Contains(art)) return false;
            if (x != null && x.KeyFlipAdd.Contains(art)) return true;
            string from = x != null && !string.IsNullOrEmpty(x.KeyFlipFrom) ? x.KeyFlipFrom : "mirrored";
            switch (from)
            {
                case "measured": return IsLightFromRightArt(art);
                case "both": return IsLightFromRightArt(art) || IsKeyFlipArt(art);
                default: return IsKeyFlipArt(art);
            }
        }

        /// <summary>
        /// 設計図のキャラの塊 (look_act1.char.json の "char") の、StageLook の型に無い材質の値 (P23 が詰める口)。書いていなければシェーダの既定のまま。
        /// localLights (技の光・逆光を受ける割合。既定 0)・whiteCap・emissionIntensity・outlineFloor [r,g,b]・receiveShadows・cookieOnKey。
        /// P23 (W3) が足した口:
        ///   blackLift [r,g,b] / heroBlackLift [r,g,b] … 輪郭の持ち上げ (StageUnitLit の _BlackLift)。sRGB の色 = 真っ黒がなる色 (トーンマップ blackLiftTonemap の
        ///     「1」の段・露出 blackLiftExposureRef の時の値)。今のトーンマップの倍率 × 2^(基準の露出 − 今の露出) を線形の値に掛けて書く = 画面の上の輪郭の暗さを
        ///     舞台の露出 (P22) が変わっても保つ。heroBlackLift が無ければ主役も blackLift
        ///   blackLiftExposureRef … 上の値を決めた時の露出 (post.exposure)。無ければ割り戻さない
        ///   blackLiftTonemap {aces, neutral, none} … トーンマップごとの倍率 (ACES の足は暗部を強く沈めるので、Neutral・無しでは少なくてよい)。無い段は 1
        ///   keyFlipFrom "measured"|"mirrored"|"both"・keyFlipAdd [絵の名前]・keyFlipRemove [絵の名前] … _KeyFlip の選び方 (ResolveKeyFlip)
        ///   art { 絵の名前の頭: { heroLift, blackLift } } … 絵ごとの上書き (名前の頭がいちばん長く一致した物。leader_green は leader_green_48 にも当たる)。
        ///     heroLift = 主役の持ち上げ (主役の時だけ・旗 herolift= が勝つ)・blackLift = 輪郭の持ち上げの倍率 (0 = 持ち上げない)
        /// P23 (2周目・本家っぽく) が足した口:
        ///   outlineTarget … 輪郭の暗さの目標 (画面の輝度 sRGB 0〜255。書けば _BlackLift を「画面でこの暗さ」になるよう今の後処理を逆にたどって絵ごとに求める = PostGrayInverse)。
        ///     blackLift は色味 (輝度で割った比) だけに使う。無ければ W3 の読み方 (blackLift を露出とトーンマップで割り戻す)
        ///   outlineLitRef … 絵の輪郭の画素にかかる光のおおよその輝度 (W3 の撮影から逆算した 0.8)。無ければ blackLiftAuto.litRef
        ///   tint [r,g,b] … キャラの色の掛け算 (sRGB の色。_CharTint)・cancelColorFilter … 後処理の colorFilter をキャラだけ打ち消す (輝度は保つ)
        ///   shadeLift [gain, knee] … 暗い色の持ち上げ (_ShadeLift。全員)。art の shadeLift が勝つ
        ///   art { 絵の名前の頭: { receive, emission, shadeLift } } … 受光 (旗 receive= が勝つ)・発光の強さ (狼の白い毛の発光 1.6 が白飛びしてブルームで青く冷えていた)・暗い色の持ち上げ
        /// </summary>
        sealed class ArtLook
        {
            public float HeroLift = -1f, BlackLiftScale = -1f;
            /// <summary>受光 (負 = 設計図の receive / heroReceive。旗 receive= が勝つ)</summary>
            public float Receive = -1f;
            /// <summary>発光の強さ (負 = 設計図の emissionIntensity)</summary>
            public float Emission = -1f;
            /// <summary>暗い色の持ち上げ (HasShade の時だけ。gain 0 = 持ち上げない)</summary>
            public Vector2 ShadeLift; public bool HasShade;
            // 二周目 レーン E (2026-10-01)。負 = 書いていない (W5 と同じ = 全体の値)
            /// <summary>環境光の倍率 (負 = look の char の ambientScale)。材質には これ × 露出の割り戻し を書く (暗めの敵を本家どおり暗い体に。char C3・N14)</summary>
            public float AmbientScale = -1f;
            /// <summary>鮮やかさ (負 = look の char の saturation。無ければ 1)。StageUnitLit の _CharSat</summary>
            public float Saturation = -1f;
            /// <summary>近くの点光源を受ける割合 (負 = look の char の localLights。無ければ 0)。このはの斧の宝石の灯を体に受ける</summary>
            public float LocalLights = -1f;
            /// <summary>三周目 レーン E: 足元ほど暗い勾配の強さの上書き (負 = look の char の bodyShade の enemy / doll。0 = この絵には掛けない = 白い狼)</summary>
            public float BodyShade = -1f;
        }
        sealed class CharMatExtras
        {
            public readonly Dictionary<string, ArtLook> Art = new Dictionary<string, ArtLook>(StringComparer.Ordinal);
            /// <summary>絵の名前の頭がいちばん長く一致した上書き (無ければ null)</summary>
            public ArtLook ArtFor(string art)
            {
                if (string.IsNullOrEmpty(art) || Art.Count == 0) return null;
                ArtLook best = null; int bestLen = -1;
                foreach (var kv in Art)
                    if (kv.Key.Length > bestLen && art.StartsWith(kv.Key, StringComparison.Ordinal)) { best = kv.Value; bestLen = kv.Key.Length; }
                return best;
            }
            public float LocalLights = -1f, WhiteCap = -1f, EmissionIntensity = -1f, ReceiveShadows = -1f, CookieOnKey = -1f;
            public Color OutlineFloor; public bool HasOutlineFloor;
            public Color BlackLift, HeroBlackLift; public bool HasBlackLift, HasHeroBlackLift;
            public float BlackLiftExposureRef = float.NaN;
            /// <summary>
            /// 輪郭の持ち上げを絵ごとに割り引く時の、キャラの光のおおよその倍率 (look の char の blackLiftAuto.litRef。NaN = 割り引かない)。
            /// 絵の輪郭がもともと明るい (ひなた・白の人形) ほど持ち上げを減らす: 倍率 = 1 − 絵の輪郭の暗さ (線形) × litRef × 主役の持ち上げ ÷ 持ち上げの明るさ
            /// </summary>
            public float BlackLiftAutoLit = float.NaN;
            /// <summary>キャラの光を決めた時の露出 (look の char の exposureRef)。あれば受光・環境光・発光を 2^(これ − 今の post.exposure) 倍 = 舞台の露出が変わってもキャラの明るさを保つ</summary>
            public float ExposureRef = float.NaN;
            public readonly Dictionary<string, float> BlackLiftTonemap = new Dictionary<string, float>(StringComparer.Ordinal);
            public string KeyFlipFrom;
            public readonly HashSet<string> KeyFlipAdd = new HashSet<string>(StringComparer.Ordinal), KeyFlipRemove = new HashSet<string>(StringComparer.Ordinal);
            // P23 (2周目)
            /// <summary>輪郭の暗さの目標 (画面の輝度 sRGB 0〜255。NaN = 目標を使わない = W3 の読み方)</summary>
            public float OutlineTarget = float.NaN;
            /// <summary>輪郭の画素にかかる光の輝度 (NaN = blackLiftAuto.litRef)</summary>
            public float OutlineLitRef = float.NaN;
            public Color Tint = Color.white; public bool HasTint;
            public bool CancelColorFilter;
            public Vector2 ShadeLift; public bool HasShade;
            // ---- 二周目 レーン E (2026-10-01。計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン E)。どれも書いていなければ W5 と同じ ----
            /// <summary>キャラの板が霧を受ける割合 (char.fog。StageUnitLit の _Fog。負 = W5 の 1)</summary>
            public float Fog = -1f;
            /// <summary>光を受ける板のリム (char.rim = 敵・char.dollRim = 人形・char.heroRim = 主役。負 = W5 の UnitRim / CharRim)</summary>
            public float Rim = -1f, DollRim = -1f, HeroRim = -1f;
            /// <summary>鮮やかさ (char.saturation。負 = 1)</summary>
            public float Saturation = -1f;
            /// <summary>
            /// 接地影 (charshadow=1 の時だけ): ContactCore = 芯の平らな半径 (0〜1。負 = W5 の柔らかい楕円 Px.Glow と 幅 0.6・濃さ 0.35)、
            /// ContactWidth・ContactAlpha = 芯の幅と濃さの倍率 (負 = W5 の 0.6・0.35)。HaloWidth・HaloAlpha = 同じ位置に重ねる広く薄い暈
            /// (幅は芯の何倍・濃さは元の接地影の何倍。char.contactHalo。負 = 暈なし)
            /// </summary>
            public float ContactCore = -1f, ContactWidth = -1f, ContactAlpha = -1f, HaloWidth = -1f, HaloAlpha = -1f;
            /// <summary>接地影の楕円の奥行きの倍率 (char.contactDepth。段2: 低いカメラ 22°・5° では寝かせた楕円の画面の縦が W5 (28°・12°) の約 0.49 倍に潰れるので、
            /// 奥行きを伸ばして W5 と同じ画面の形に戻す。芯の影と暈の両方に掛ける。負 = 1 = 今のまま。charshadow=1 の時だけ)</summary>
            public float ContactDepth = -1f;
            /// <summary>敵と人形の待機のコマ (char.r2idle.on。箱庭の時だけ Art/enemies/anim/&lt;名前&gt;_r2idle_&lt;n&gt; を読む)。IdleDur = 1コマの秒・IdleSlowDur = 幕ボスの1コマの秒 (IdleSlow の名前の頭に当たる絵)</summary>
            public bool Idle; public float IdleDur = 0.25f, IdleSlowDur = 0.4f;
            public readonly List<string> IdleSlow = new List<string>();
            /// <summary>主役の斧の宝石の灯 (char.heroGem。光を受ける板の時だけ・tier=phone では作らない)。Range・Intensity・Moving (待機以外のコマの強さの倍率)・Colors (リーダーの絵の頭 → 色)・Push (板から手前へ出す距離)</summary>
            public bool Gem; public float GemRange = 2.75f, GemIntensity = 1f, GemMoving = 0.3f, GemPush = 0.35f;
            public readonly Dictionary<string, Color> GemColors = new Dictionary<string, Color>(StringComparer.Ordinal);
            // ---- 三周目 レーン E (2026-10-01。計画 docs/design/hd2d-round3-plan-2026-10-01.md §2 E・分析 R5・R11)。どれも書いていなければ二周目と同じ ----
            /// <summary>足元ほど暗い勾配 (char.bodyShade): 足元の暗さ (敵・人形。0 = 掛けない)・勾配が 1 に戻る高さ (足元からの絵の高さの割合)・曲がり・敵に掛ける編成の頭数の下限</summary>
            public float R3E_BodyShadeEnemy, R3E_BodyShadeDoll, R3E_BodyShadeTop = 0.9f, R3E_BodyShadePower = 1f;
            public int R3E_BodyShadeMinGroup = 3;
            /// <summary>輪郭の1画素の中間色の強さ (char.edgeSoft 0〜1。0 = 切)。MSAA が効いている時だけ材質に書く</summary>
            public float R3E_EdgeSoft;
            /// <summary>敵の環境光の倍率を座席の番号で (char.seatAmbient。enemy0, enemy1… の順・最後の値で止める。null = 掛けない)</summary>
            public float[] R3E_SeatAmbient;
            /// <summary>人形の後列の環境光の倍率 (char.dollBackAmbient。負 = 掛けない)</summary>
            public float R3E_DollBackAmbient = -1f;
            /// <summary>待機の位相を座席の番号で散らす割合 (char.idlePhaseSpread 0〜1。0 = 二周目の式 = 板の位相 × 0.37 秒)</summary>
            public float R3E_IdlePhaseSpread;
        }

        /// <summary>JSON の [a, b] を Vector2 に (無い・短い時は false)</summary>
        static bool ReadVec2(JToken t, out Vector2 v)
        {
            v = Vector2.zero;
            var a = t as JArray;
            if (a == null || a.Count < 2) return false;
            if (!(a[0].Type == JTokenType.Float || a[0].Type == JTokenType.Integer) || !(a[1].Type == JTokenType.Float || a[1].Type == JTokenType.Integer)) return false;
            v = new Vector2((float)a[0], (float)a[1]);
            return true;
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
                    // P23: 輪郭の持ち上げ・_KeyFlip の選び方
                    var bl = c["blackLift"] as JArray;
                    if (bl != null && bl.Count >= 3) { x.BlackLift = new Color((float)bl[0], (float)bl[1], (float)bl[2], 1f); x.HasBlackLift = true; }
                    var hbl = c["heroBlackLift"] as JArray;
                    if (hbl != null && hbl.Count >= 3) { x.HeroBlackLift = new Color((float)hbl[0], (float)hbl[1], (float)hbl[2], 1f); x.HasHeroBlackLift = true; }
                    var ert = c["blackLiftExposureRef"];
                    if (ert != null && (ert.Type == JTokenType.Float || ert.Type == JTokenType.Integer)) x.BlackLiftExposureRef = (float)ert;
                    var bla = c["blackLiftAuto"] as JObject;
                    if (bla != null)
                    {
                        var lr = bla["litRef"];
                        if (lr != null && (lr.Type == JTokenType.Float || lr.Type == JTokenType.Integer)) x.BlackLiftAutoLit = Mathf.Max(0f, (float)lr);
                    }
                    var cer = c["exposureRef"];
                    if (cer != null && (cer.Type == JTokenType.Float || cer.Type == JTokenType.Integer)) x.ExposureRef = (float)cer;
                    var tm = c["blackLiftTonemap"] as JObject;
                    if (tm != null)
                        foreach (var p in tm.Properties())
                            if (!p.Name.StartsWith("_", StringComparison.Ordinal) && (p.Value.Type == JTokenType.Float || p.Value.Type == JTokenType.Integer))
                                x.BlackLiftTonemap[p.Name.Trim().ToLowerInvariant()] = Mathf.Max(0f, (float)p.Value);
                    var kf = c["keyFlipFrom"];
                    if (kf != null && kf.Type == JTokenType.String) x.KeyFlipFrom = ((string)kf).Trim().ToLowerInvariant();
                    Action<string, HashSet<string>> names = (name, set) =>
                    {
                        var arr = c[name] as JArray;
                        if (arr != null) foreach (var t in arr) if (t.Type == JTokenType.String && !string.IsNullOrEmpty((string)t)) set.Add((string)t);
                    };
                    names("keyFlipAdd", x.KeyFlipAdd);
                    names("keyFlipRemove", x.KeyFlipRemove);
                    // P23 (2周目): 輪郭の暗さの目標・キャラの色の掛け算・暗い色の持ち上げ
                    var ot = c["outlineTarget"];
                    if (ot != null && (ot.Type == JTokenType.Float || ot.Type == JTokenType.Integer)) x.OutlineTarget = Mathf.Clamp((float)ot, 0f, 255f);
                    var olr = c["outlineLitRef"];
                    if (olr != null && (olr.Type == JTokenType.Float || olr.Type == JTokenType.Integer)) x.OutlineLitRef = Mathf.Max(0f, (float)olr);
                    var tn = c["tint"] as JArray;
                    if (tn != null && tn.Count >= 3) { x.Tint = new Color((float)tn[0], (float)tn[1], (float)tn[2], 1f); x.HasTint = true; }
                    var ccf = c["cancelColorFilter"];
                    if (ccf != null && ccf.Type == JTokenType.Boolean) x.CancelColorFilter = (bool)ccf;
                    Vector2 sl;
                    if (ReadVec2(c["shadeLift"], out sl)) { x.ShadeLift = sl; x.HasShade = true; }
                    var arts = c["art"] as JObject;
                    if (arts != null)
                        foreach (var p in arts.Properties())
                        {
                            var ao = p.Value as JObject;
                            if (ao == null || p.Name.StartsWith("_", StringComparison.Ordinal)) continue;
                            Func<string, float> anum = name => { var t = ao[name]; return t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? (float)t : -1f; };
                            var al = new ArtLook { HeroLift = anum("heroLift"), BlackLiftScale = anum("blackLift"), Emission = anum("emission"), Receive = anum("receive"),
                                AmbientScale = anum("ambientScale"), Saturation = anum("saturation"), LocalLights = anum("localLights"),   // 後ろ3つ = 二周目 レーン E
                                BodyShade = anum("bodyShade") };   // 三周目 レーン E
                            Vector2 asl;
                            if (ReadVec2(ao["shadeLift"], out asl)) { al.ShadeLift = asl; al.HasShade = true; }
                            x.Art[p.Name] = al;
                        }
                    R2E_ReadExtras(c, x, num);
                    R3E_ReadExtras(c, x, num);
                }
            }
            catch (Exception ex) { Debug.LogWarning("[Stage] look の char を読めない: " + ex.Message); }
            _extrasFor = cur; _extras = x;
            return x;
        }

        /// <summary>
        /// 二周目 レーン E (2026-10-01) の look の char のキーを読む。どれも無ければ W5 と同じ (CharMatExtras の既定 = 負・off)。
        ///   fog … キャラの板が霧を受ける割合 (_Fog)・rim / dollRim / heroRim … 光を受ける板のリム (敵・人形・主役)・saturation … 鮮やかさ (_CharSat)
        ///   contactCore / contactWidth / contactAlpha … 芯のある接地影 (charshadow=1 の時だけ)・contactHalo {width, alpha} | true | false … 広く薄い暈
        ///   r2idle {on, dur, slowDur, slow [絵の名前の頭]} | true | false … 敵と人形の待機のコマ (箱庭の時だけ)
        ///   heroGem {on, range, intensity, moving, push, color {リーダーの絵の頭: [r,g,b]}} | true | false … 主役の斧の宝石の灯 (光を受ける板の時だけ・スマホは作らない)
        /// 塊は {"on": false} か false で消える (重ね読みは null を飛ばすので、W5 の写し look_act1_w5char はこの形で消す)
        /// </summary>
        static void R2E_ReadExtras(JObject c, CharMatExtras x, Func<string, float> num)
        {
            x.Fog = num("fog");
            x.Rim = num("rim"); x.DollRim = num("dollRim"); x.HeroRim = num("heroRim");
            x.Saturation = num("saturation");
            x.ContactCore = num("contactCore"); x.ContactWidth = num("contactWidth"); x.ContactAlpha = num("contactAlpha");
            x.ContactDepth = num("contactDepth");
            Func<JToken, bool> on = t =>
            {
                if (t == null || t.Type == JTokenType.Null) return false;
                if (t.Type == JTokenType.Boolean) return (bool)t;
                var o = t as JObject;
                if (o == null) return false;
                var f = o["on"];
                return f == null || f.Type != JTokenType.Boolean || (bool)f;
            };
            Func<JObject, string, float, float> onum = (o, name, def) =>
            {
                var t = o != null ? o[name] : null;
                return t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? (float)t : def;
            };
            var halo = c["contactHalo"];
            if (on(halo))
            {
                var ho = halo as JObject;
                x.HaloWidth = Mathf.Max(0f, onum(ho, "width", 1.8f)); x.HaloAlpha = Mathf.Max(0f, onum(ho, "alpha", 0.3f));
            }
            var idle = c["r2idle"];
            x.Idle = on(idle);
            if (x.Idle)
            {
                var io = idle as JObject;
                x.IdleDur = Mathf.Max(0.02f, onum(io, "dur", x.IdleDur)); x.IdleSlowDur = Mathf.Max(0.02f, onum(io, "slowDur", x.IdleSlowDur));
                var sl = io != null ? io["slow"] as JArray : null;
                if (sl != null) foreach (var t in sl) if (t.Type == JTokenType.String && !string.IsNullOrEmpty((string)t)) x.IdleSlow.Add((string)t);
            }
            var gem = c["heroGem"];
            x.Gem = on(gem);
            if (x.Gem)
            {
                var go = gem as JObject;
                x.GemRange = Mathf.Max(0.1f, onum(go, "range", x.GemRange)); x.GemIntensity = Mathf.Max(0f, onum(go, "intensity", x.GemIntensity));
                x.GemMoving = Mathf.Clamp01(onum(go, "moving", x.GemMoving)); x.GemPush = onum(go, "push", x.GemPush);
                var cols = go != null ? go["color"] as JObject : null;
                if (cols != null)
                    foreach (var p in cols.Properties())
                    {
                        var a = p.Value as JArray;
                        if (a != null && a.Count >= 3 && !p.Name.StartsWith("_", StringComparison.Ordinal)) x.GemColors[p.Name] = new Color((float)a[0], (float)a[1], (float)a[2], 1f);
                    }
            }
        }

        /// <summary>
        /// 三周目 レーン E (2026-10-01) の look の char のキーを読む。どれも無ければ二周目と同じ (CharMatExtras の既定 = 0・null・負)。
        ///   bodyShade {on, enemy, doll, top, power, minGroup} | 数 | false … 足元ほど暗い縦の勾配 (R5 (b))。enemy = 編成の頭数が minGroup 以上の敵の足元の暗さ・
        ///     doll = 人形の足元の暗さ (0〜1)・top = 勾配が 1 に戻る高さ (足元からの絵の高さの割合)・power = 曲がり。数なら enemy と doll の両方。
        ///     art の bodyShade (数) が絵ごとに勝つ (0 = その絵には掛けない)
        ///   edgeSoft 0〜1 … 輪郭の1画素の中間色 (R5 (c)・裁定 Q2)。MSAA が効いている時だけ (StageUnitLit の _EdgeSoft と _EdgeA2C)
        ///   seatAmbient [enemy0, enemy1, …] … 敵の環境光の倍率を座席の番号で (R11。最後の値で止める)・dollBackAmbient … 人形の後列の環境光の倍率
        ///   idlePhaseSpread 0〜1 … 待機の位相を座席の番号 (2進の逆順 0・½・¼・¾…) で散らす割合 (R11。0 = 二周目の板ごとの乱数の位相)
        /// off の書き方 (写し look_act1_r2char): bodyShade {"on": false}・edgeSoft 0・seatAmbient [1]・dollBackAmbient 1・idlePhaseSpread 0・art の bodyShade -1
        /// </summary>
        static void R3E_ReadExtras(JObject c, CharMatExtras x, Func<string, float> num)
        {
            Func<JObject, string, float, float> onum = (o, name, def) =>
            {
                var t = o != null ? o[name] : null;
                return t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? (float)t : def;
            };
            var bs = c["bodyShade"];
            if (bs != null && (bs.Type == JTokenType.Float || bs.Type == JTokenType.Integer))
            {
                x.R3E_BodyShadeEnemy = x.R3E_BodyShadeDoll = Mathf.Clamp01((float)bs);
            }
            else if (bs is JObject bo)
            {
                var f = bo["on"];
                bool on = f == null || f.Type != JTokenType.Boolean || (bool)f;
                if (on)
                {
                    x.R3E_BodyShadeEnemy = Mathf.Clamp01(onum(bo, "enemy", 0f));
                    x.R3E_BodyShadeDoll = Mathf.Clamp01(onum(bo, "doll", 0f));
                    x.R3E_BodyShadeTop = Mathf.Clamp(onum(bo, "top", x.R3E_BodyShadeTop), 0.05f, 1f);
                    x.R3E_BodyShadePower = Mathf.Clamp(onum(bo, "power", x.R3E_BodyShadePower), 0.05f, 8f);
                    x.R3E_BodyShadeMinGroup = Mathf.Max(1, Mathf.RoundToInt(onum(bo, "minGroup", x.R3E_BodyShadeMinGroup)));
                }
            }
            x.R3E_EdgeSoft = Mathf.Clamp01(Mathf.Max(0f, num("edgeSoft")));
            var sa = c["seatAmbient"] as JArray;
            if (sa != null && sa.Count > 0)
            {
                var arr = new List<float>();
                foreach (var t in sa) if (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) arr.Add(Mathf.Max(0f, (float)t));
                bool any = false;
                foreach (var v in arr) if (Mathf.Abs(v - 1f) > 1e-4f) { any = true; break; }
                x.R3E_SeatAmbient = any ? arr.ToArray() : null;
            }
            float db = num("dollBackAmbient");
            x.R3E_DollBackAmbient = db >= 0f && Mathf.Abs(db - 1f) > 1e-4f ? db : -1f;
            x.R3E_IdlePhaseSpread = Mathf.Clamp01(Mathf.Max(0f, num("idlePhaseSpread")));
        }

        /// <summary>座席の番号を 0〜1 に散らす (2進の逆順: 0・½・¼・¾・⅛・⅝…)。隣り合う座席ほど離れた位相になる (三周目 レーン E・R11)</summary>
        static float R3E_VanDerCorput(int n)
        {
            float v = 0f, b = 0.5f;
            for (n = Mathf.Max(0, n); n > 0; n >>= 1, b *= 0.5f) if ((n & 1) != 0) v += b;
            return v;
        }

        /// <summary>
        /// MSAA が効いているか (半立体と札の alpha-to-coverage と同じ判定 = Stage.DioramaMsaaOn: 旗 aa=msaa・標本数 = HD2DFlags.MsaaSamples と設計図の msaaMax の小さい方 &gt; 1)。
        /// 輪郭の1画素 (Alpha to Coverage) はこの時だけ材質に書く (MSAA が無いと被覆が 0/1 にしかならず、外側の1画素がそのまま太る)。スマホの段は msaaMax 1 = 切
        /// </summary>
        static bool R3E_MsaaOn()
        {
            return HD2DFlags.Tier != HD2DTier.Phone && DioramaMsaaOn();
        }

        /// <summary>名前の頭がいちばん長く一致した値 (無ければ false)。二周目 レーン E の表 (宝石の灯の色・待機のコマの幕ボス) が使う</summary>
        static bool R2E_PrefixMatch<T>(Dictionary<string, T> table, string art, out T value)
        {
            value = default(T);
            if (string.IsNullOrEmpty(art) || table == null) return false;
            int best = -1;
            foreach (var kv in table)
                if (kv.Key.Length > best && art.StartsWith(kv.Key, StringComparison.Ordinal)) { value = kv.Value; best = kv.Key.Length; }
            return best >= 0;
        }

        /// <summary>
        /// 輪郭の持ち上げの実際の値 (StageUnitLit の _BlackLift。線形・後処理の前。2026-09-30 P23)。
        /// look の char の blackLift (主役は heroBlackLift があればそれ) を線形へ直し、今のトーンマップの倍率 (blackLiftTonemap) と
        /// 2^(blackLiftExposureRef − 今の post.exposure) を掛ける = 後処理が変わっても画面の上の輪郭の暗さ (目標 20〜30) を保つ。書いていなければ 0 (何もしない)。
        /// edgeLin = 絵の輪郭の暗さ (EdgeDarkLinear。負 = 分からない)・litMul = その板の光の倍率 (露出の割り戻し × 主役の持ち上げ)。
        /// 絵ごとの倍率: look の char の art の blackLift があればそれ、無ければ blackLiftAuto (絵の輪郭がもともと明るいほど減らす)、どちらも無ければ 1
        /// </summary>
        static Vector4 BlackLiftVector(CharMatExtras x, bool hero, ArtLook art, float edgeLin = -1f, float litMul = 1f)
        {
            return BlackLiftVector(x, hero, art, edgeLin, litMul, UnitView.Neutral);
        }

        /// <summary>
        /// 2周目 (P23・本家っぽく): look の char に outlineTarget があれば、「輪郭の画素が画面でその暗さになる」_BlackLift を解く (BlackLiftForTarget)。
        /// 無ければ W3 の読み方 (blackLift を露出とトーンマップで割り戻す)。view = その板の画面の事情 (周辺減光・霧・キャラの色の掛け算)
        /// </summary>
        static Vector4 BlackLiftVector(CharMatExtras x, bool hero, ArtLook art, float edgeLin, float litMul, UnitView view)
        {
            if (x == null) return Vector4.zero;
            if (!float.IsNaN(x.OutlineTarget) && StageLook.Current != null) return BlackLiftForTarget(x, hero, art, edgeLin, litMul, view);
            bool heroOwn = hero && x.HasHeroBlackLift;
            if (!heroOwn && !x.HasBlackLift) return Vector4.zero;
            Color c = heroOwn ? x.HeroBlackLift : x.BlackLift;
            Color lin = QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
            float s = 1f;
            var cur = StageLook.Current;
            if (cur != null)
            {
                string tm = (cur.Post.Tonemap ?? "aces").Trim().ToLowerInvariant();
                float f;
                if (x.BlackLiftTonemap.TryGetValue(tm, out f)) s *= f;
                if (!float.IsNaN(x.BlackLiftExposureRef)) s *= Mathf.Pow(2f, Mathf.Clamp(x.BlackLiftExposureRef - cur.Post.Exposure, -3f, 3f));
            }
            float artScale = 1f;
            if (art != null && art.BlackLiftScale >= 0f) artScale = art.BlackLiftScale;   // 絵ごとの倍率 (look の char の art)
            else if (!float.IsNaN(x.BlackLiftAutoLit) && edgeLin >= 0f)
            {
                // 輪郭の画素 ≈ 絵の輪郭 × 光。持ち上げはその足りない分だけ (もともと目標より明るい輪郭は持ち上げない)
                float liftY = (0.2126f * lin.r + 0.7152f * lin.g + 0.0722f * lin.b) * s;
                artScale = liftY > 1e-6f ? Mathf.Clamp01(1f - edgeLin * x.BlackLiftAutoLit * Mathf.Max(0f, litMul) / liftY) : 0f;
            }
            s *= artScale;
            return new Vector4(Mathf.Clamp01(lin.r * s), Mathf.Clamp01(lin.g * s), Mathf.Clamp01(lin.b * s), 0f);
        }

        // ---------------------------------------------------------------- 輪郭の暗さを画面の値で決める (2026-09-30 P23 2周目)

        /// <summary>
        /// 輪郭の目標を解く時の、その板の画面の事情。Vignette = 周辺減光の倍率 (輝度・1 = 掛からない)・FogAmount = 霧の割合 (0 = 無し)・
        /// FogLum = 霧の色の輝度 (線形)・CharCf = キャラの色 (後処理の colorFilter × _CharTint) の輝度・StageCf = 後処理の colorFilter の輝度 (霧にかかる)
        /// </summary>
        struct UnitView
        {
            public float Vignette, FogAmount, FogLum, CharCf, StageCf;
            /// <summary>何も掛からない (周辺減光 1・霧 0・colorFilter は後処理の値そのまま = -1)</summary>
            public static UnitView Neutral => new UnitView { Vignette = 1f, FogAmount = 0f, FogLum = 0f, CharCf = -1f, StageCf = -1f };
        }

        /// <summary>
        /// look の char の outlineTarget (画面の輝度 sRGB 0〜255) から _BlackLift (線形・後処理の前) を解く。
        ///   ① 今の後処理を灰色について逆にたどり (PostGrayInverse。周辺減光・霧を含む)、輪郭の画素が後処理の前にいくつなら画面で目標の暗さかを求める
        ///   ② 絵の輪郭がもともと持つ明るさ (edgeLin × 光 × 暗い色の持ち上げ) を差し引く (もともと目標より明るい輪郭の絵 = ひなた・白の人形は持ち上げない)
        ///   ③ col = L + col×(1−L) を L について解き、blackLift の色味 (輝度で割った比。無ければ灰色) を掛ける。絵ごとの倍率 (art の blackLift) も掛ける
        /// </summary>
        static Vector4 BlackLiftForTarget(CharMatExtras x, bool hero, ArtLook art, float edgeLin, float litMul, UnitView view)
        {
            var p = StageLook.Current.Post;
            float stageCf = view.StageCf > 0f ? view.StageCf : LumLin(p.ColorFilter);
            float charCf = view.CharCf > 0f ? view.CharCf : stageCf;
            // ① 画面で目標の暗さになる、後処理の前の値 (colorFilter は舞台の値で、キャラの分は charCf/stageCf で割り戻す)
            float xin = PostGrayInverse(x.OutlineTarget, p, view.Vignette, stageCf);
            float phi = Mathf.Clamp01(view.FogAmount);
            float z = (xin - phi * view.FogLum) / Mathf.Max(1e-4f, (1f - phi) * charCf / Mathf.Max(1e-4f, stageCf));
            // ② 絵の輪郭の明るさ (読めない絵 = -1 は 0 として扱う = 割り引かない)
            float litRef = !float.IsNaN(x.OutlineLitRef) ? x.OutlineLitRef : (!float.IsNaN(x.BlackLiftAutoLit) ? x.BlackLiftAutoLit : 0.8f);
            float c = edgeLin > 0f ? edgeLin * litRef * Mathf.Max(0f, litMul) : 0f;
            Vector2 shade = ShadeLiftFor(x, art);
            if (shade.x > 0f) { float k = Mathf.Max(1e-3f, shade.y), q = k / (k + c); c *= 1f + shade.x * q * q; }
            // ③
            float l = c < 0.999f ? Mathf.Max(0f, (z - c) / (1f - c)) : 0f;
            if (art != null && art.BlackLiftScale >= 0f) l *= art.BlackLiftScale;
            Vector3 hue = Vector3.one;
            bool heroOwn = hero && x.HasHeroBlackLift;
            if (heroOwn || x.HasBlackLift)
            {
                Color h = heroOwn ? x.HeroBlackLift : x.BlackLift;
                Color hl = QualitySettings.activeColorSpace == ColorSpace.Linear ? h.linear : h;
                float hy = 0.2126f * hl.r + 0.7152f * hl.g + 0.0722f * hl.b;
                if (hy > 1e-6f) hue = new Vector3(hl.r / hy, hl.g / hy, hl.b / hy);
            }
            return new Vector4(Mathf.Clamp01(l * hue.x), Mathf.Clamp01(l * hue.y), Mathf.Clamp01(l * hue.z), 0f);
        }

        /// <summary>暗い色の持ち上げ (gain, knee): art の shadeLift ＞ look の char の shadeLift ＞ 無し (0, 0)</summary>
        static Vector2 ShadeLiftFor(CharMatExtras x, ArtLook art)
        {
            if (art != null && art.HasShade) return new Vector2(Mathf.Max(0f, art.ShadeLift.x), Mathf.Max(0f, art.ShadeLift.y));
            if (x != null && x.HasShade) return new Vector2(Mathf.Max(0f, x.ShadeLift.x), Mathf.Max(0f, x.ShadeLift.y));
            return Vector2.zero;
        }

        /// <summary>sRGB の色 (0〜1) の輝度を線形で (色空間が Gamma なら値のまま)</summary>
        static float LumLin(Color c)
        {
            Color l = QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
            return 0.2126f * l.r + 0.7152f * l.g + 0.0722f * l.b;
        }

        /// <summary>
        /// 後処理 (URP 17.6 の HDR の色の段) を灰色の入力 x (線形・後処理の前) について近似し、画面の輝度 (sRGB 0〜255 の 0.2126R+0.7152G+0.0722B) を返す。
        /// 写した物: 周辺減光 (UberPost の ApplyVignette = 輝度の倍率 vig で受ける) → 露出 2^exposure → コントラスト (ACES は ACEScc で 0.18 を軸・それ以外は LogC) →
        /// colorFilter → Lift/Gamma/Gain (ColorUtils.PrepareLiftGammaGain) → トーンマップ (ACES は Color.hlsl の MJP の近似と dim surround のγ 0.9811・Neutral・無し) → sRGB。
        /// 灰色では効かない ACES の色の回転・glow・赤の補正・彩度の補正は省く。W3 の撮影 (unitsonly と hideui) で暗部 (線形 0.02〜0.08) は ±2 レベル
        /// (ブルームのにじみの分だけ実際が少し明るい)。cfScale = colorFilter の代わりに掛ける灰色の倍率 (負 = 設計図の colorFilter をチャンネルごとに)
        /// </summary>
        internal static float PostGrayDisplay(float x, StageLookData.PostLook p, float vig, float cfScale)
        {
            string tm = (p.Tonemap ?? "aces").Trim().ToLowerInvariant();
            float v = Mathf.Max(0f, x) * Mathf.Max(0f, vig) * Mathf.Pow(2f, p.Exposure);
            float con = p.Contrast / 100f + 1f;
            if (tm == "aces") v = 0.18f * Mathf.Pow(Mathf.Max(v, 1.52587890625e-5f) / 0.18f, con);
            else v = Mathf.Max(0f, LogCToLinearApprox((LinearToLogCApprox(v) - 0.4135884f) * con + 0.4135884f));
            Vector3 cf;
            if (cfScale >= 0f) cf = new Vector3(cfScale, cfScale, cfScale);
            else
            {
                Color cl = QualitySettings.activeColorSpace == ColorSpace.Linear ? p.ColorFilter.linear : p.ColorFilter;
                cf = new Vector3(cl.r, cl.g, cl.b);
            }
            Vector3 lift, gamma, gain;
            PrepareLiftGammaGain(p.Lift, p.Gamma, p.Gain, out lift, out gamma, out gain);
            float y = 0f;
            for (int i = 0; i < 3; i++)
            {
                float ch = Mathf.Max(0f, v * cf[i]);
                ch = ch * gain[i] + lift[i];
                ch = Mathf.Sign(ch) * Mathf.Pow(Mathf.Abs(ch), gamma[i]);
                if (tm == "aces") ch = Mathf.Pow(AcesFitCurve(ch), 0.9811f);
                else if (tm == "neutral") ch = NeutralTonemapCurve(ch);
                else ch = Mathf.Max(0f, ch);
                float w = i == 0 ? 0.2126f : i == 1 ? 0.7152f : 0.0722f;
                y += w * SrgbEncode01(ch) * 255f;
            }
            return y;
        }

        /// <summary>PostGrayDisplay の逆 (画面の輝度 target になる後処理の前の灰色の値)。対数の二分探索 40 回。届かなければ端の値</summary>
        internal static float PostGrayInverse(float target, StageLookData.PostLook p, float vig, float cfScale)
        {
            float lo = -18f, hi = 4f;   // log2 の範囲 (約 3.8e-6 〜 16)
            if (PostGrayDisplay(Mathf.Pow(2f, lo), p, vig, cfScale) >= target) return 0f;
            if (PostGrayDisplay(Mathf.Pow(2f, hi), p, vig, cfScale) <= target) return Mathf.Pow(2f, hi);
            for (int i = 0; i < 40; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (PostGrayDisplay(Mathf.Pow(2f, mid), p, vig, cfScale) < target) lo = mid; else hi = mid;
            }
            return Mathf.Pow(2f, (lo + hi) * 0.5f);
        }

        /// <summary>URP の ColorUtils.PrepareLiftGammaGain と同じ (lift = 色×0.15 の輝度を引いて w を足す・gamma = 1/(色×0.8 − 輝度 + 1 + w)・gain = 色×0.8 − 輝度 + 1 + w)</summary>
        static void PrepareLiftGammaGain(Vector4 inLift, Vector4 inGamma, Vector4 inGain, out Vector3 lift, out Vector3 gamma, out Vector3 gain)
        {
            Func<Vector4, float, Vector3> lin = (v, s) => new Vector3(Mathf.GammaToLinearSpace(v.x) * s, Mathf.GammaToLinearSpace(v.y) * s, Mathf.GammaToLinearSpace(v.z) * s);
            Func<Vector3, float> lum = v => 0.2126f * v.x + 0.7152f * v.y + 0.0722f * v.z;
            var l = lin(inLift, 0.15f); float ll = lum(l);
            lift = new Vector3(l.x - ll + inLift.w, l.y - ll + inLift.w, l.z - ll + inLift.w);
            var g = lin(inGamma, 0.8f); float lg = lum(g); float gw = inGamma.w + 1f;
            gamma = new Vector3(1f / Mathf.Max(g.x - lg + gw, 1e-3f), 1f / Mathf.Max(g.y - lg + gw, 1e-3f), 1f / Mathf.Max(g.z - lg + gw, 1e-3f));
            var n = lin(inGain, 0.8f); float ln = lum(n); float nw = inGain.w + 1f;
            gain = new Vector3(n.x - ln + nw, n.y - ln + nw, n.z - ln + nw);
        }

        /// <summary>URP の AcesTonemap の有理式 (MJP の BakingLab の近似。Color.hlsl)</summary>
        static float AcesFitCurve(float x)
        {
            const float a = 0.0245786f, b = 0.000090537f, c = 0.983729f, d = 0.4329510f, e = 0.238081f;
            x = Mathf.Max(0f, x);
            return Mathf.Max(0f, (x * (x + a) - b) / (x * (c * x + d) + e));
        }

        /// <summary>光を受ける板のリム (二周目 レーン E の 2・char C6): look の char の heroRim (主役)・dollRim (人形)・rim (敵)。書いていなければ fallback (今の UnitRim / CharRim)</summary>
        static float R2E_LitRim(bool hero, bool doll, float fallback)
        {
            var x = CharExtras();
            float v = hero ? x.HeroRim : doll ? x.DollRim : x.Rim;
            return v >= 0f ? v : fallback;
        }

        /// <summary>URP の NeutralTonemap (Color.hlsl) の1チャンネル</summary>
        static float NeutralTonemapCurve(float x)
        {
            Func<float, float> curve = t => ((t * (0.2f * t + 0.24f * 0.29f) + 0.272f * 0.02f) / (t * (0.2f * t + 0.29f) + 0.272f * 0.3f)) - 0.02f / 0.3f;
            float ws = 1f / curve(5.3f);
            return curve(Mathf.Max(0f, x) * ws) * ws;
        }

        /// <summary>URP の LinearToLogC (Alexa LogC El 1000・USE_PRECISE_LOGC 0 の式)</summary>
        static float LinearToLogCApprox(float x) { return 0.244161f * Mathf.Log10(Mathf.Max(5.555556f * x + 0.047996f, 1e-6f)) + 0.386036f; }
        /// <summary>URP の LogCToLinear (同上)</summary>
        static float LogCToLinearApprox(float x) { return (Mathf.Pow(10f, (x - 0.386036f) / 0.244161f) - 0.047996f) / 5.555556f; }

        /// <summary>線形 (0〜1) → sRGB (0〜1)</summary>
        static float SrgbEncode01(float v)
        {
            v = Mathf.Clamp01(v);
            return v <= 0.0031308f ? v * 12.92f : 1.055f * Mathf.Pow(v, 1f / 2.4f) - 0.055f;
        }

        /// <summary>
        /// その板の画面の事情 (輪郭の目標を解く時に使う)。周辺減光は URP の ApplyVignette (uv は画面の左下が原点・強さ×3・なめらかさ×5・rounded なら横に縦横比)、
        /// 霧は RenderSettings (Linear・Exp・Exp2)。uvX/uvY = 板の真ん中の画面の割合・depth = 板の深さ・fogOnUnit = 材質の _Fog・charTint = _CharTint (線形)
        /// </summary>
        static UnitView ViewFor(float uvX, float uvY, float depth, float fogOnUnit, Vector3 charTint)
        {
            var v = UnitView.Neutral;
            var cur = StageLook.Current;
            if (cur == null) return v;
            var p = cur.Post;
            if (p.VignetteIntensity > 0f)
            {
                float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
                float dx = Mathf.Abs(uvX - p.VignetteCenter.x) * p.VignetteIntensity * 3f, dy = Mathf.Abs(uvY - p.VignetteCenter.y) * p.VignetteIntensity * 3f;
                if (p.VignetteRounded) dx *= aspect;
                float vf = Mathf.Pow(Mathf.Clamp01(1f - (dx * dx + dy * dy)), p.VignetteSmoothness * 5f);
                v.Vignette = Mathf.Lerp(LumLin(p.VignetteColor), 1f, vf);
            }
            if (RenderSettings.fog && fogOnUnit > 0f && depth > 0f)
            {
                float amt;
                switch (RenderSettings.fogMode)
                {
                    case FogMode.Linear:
                        float s = RenderSettings.fogStartDistance, e = RenderSettings.fogEndDistance;
                        amt = e > s ? Mathf.Clamp01((depth - s) / (e - s)) : 0f; break;
                    case FogMode.Exponential: amt = 1f - Mathf.Exp(-RenderSettings.fogDensity * depth); break;
                    default: { float dd = RenderSettings.fogDensity * depth; amt = 1f - Mathf.Exp(-dd * dd); break; }
                }
                v.FogAmount = Mathf.Clamp01(amt * fogOnUnit);
                v.FogLum = LumLin(RenderSettings.fogColor);
            }
            Color cfl = QualitySettings.activeColorSpace == ColorSpace.Linear ? p.ColorFilter.linear : p.ColorFilter;
            v.StageCf = 0.2126f * cfl.r + 0.7152f * cfl.g + 0.0722f * cfl.b;
            v.CharCf = 0.2126f * cfl.r * charTint.x + 0.7152f * cfl.g * charTint.y + 0.0722f * cfl.b * charTint.z;
            return v;
        }

        /// <summary>
        /// キャラの色の掛け算 _CharTint (線形)。look の char の tint (sRGB の色) × cancelColorFilter なら後処理の colorFilter の打ち消し
        /// (チャンネルごとに colorFilter の輝度 ÷ colorFilter = 画面の上でキャラの色だけ colorFilter が掛からない。明るさは保つ)。無ければ (1,1,1)
        /// </summary>
        static Vector3 CharTintFor(CharMatExtras x)
        {
            var t = Vector3.one;
            if (x == null) return t;
            if (x.HasTint)
            {
                Color tl = QualitySettings.activeColorSpace == ColorSpace.Linear ? x.Tint.linear : x.Tint;
                t = new Vector3(tl.r, tl.g, tl.b);
            }
            var cur = StageLook.Current;
            if (x.CancelColorFilter && cur != null)
            {
                Color cfl = QualitySettings.activeColorSpace == ColorSpace.Linear ? cur.Post.ColorFilter.linear : cur.Post.ColorFilter;
                float lum = 0.2126f * cfl.r + 0.7152f * cfl.g + 0.0722f * cfl.b;
                if (cfl.r > 1e-4f && cfl.g > 1e-4f && cfl.b > 1e-4f)
                    t = new Vector3(t.x * lum / cfl.r, t.y * lum / cfl.g, t.z * lum / cfl.b);
            }
            return t;
        }

        /// <summary>
        /// 絵の輪郭の暗さ (線形の輝度): 矩形 [x0,x1)×[y0,y1) の中で、不透明 (a&gt;127) で上下左右のどれかが透明か矩形の外の画素の、
        /// 輝度 (sRGB 0〜255 の 0.2126R+0.7152G+0.0722B) の 10 パーセンタイルを線形へ直した値。輪郭の画素が無ければ 0 (真っ黒として扱う)
        /// </summary>
        internal static float EdgeDarkLinearFrom(Color32[] px, int w, int x0, int y0, int x1, int y1)
        {
            var list = new List<float>();
            if (px == null || w <= 0) return 0f;
            int h = px.Length / w;
            x0 = Math.Max(0, x0); y0 = Math.Max(0, y0); x1 = Math.Min(w, x1); y1 = Math.Min(h, y1);
            Func<int, int, bool> opaque = (xx, yy) => xx >= x0 && xx < x1 && yy >= y0 && yy < y1 && px[yy * w + xx].a > 127;
            for (int y = y0; y < y1; y++)
                for (int xx = x0; xx < x1; xx++)
                {
                    if (!opaque(xx, y)) continue;
                    if (opaque(xx - 1, y) && opaque(xx + 1, y) && opaque(xx, y - 1) && opaque(xx, y + 1)) continue;
                    var p = px[y * w + xx];
                    list.Add(0.2126f * p.r + 0.7152f * p.g + 0.0722f * p.b);
                }
            if (list.Count == 0) return 0f;
            list.Sort();
            // numpy の percentile (線形補間) と同じ 10 パーセンタイル
            float pos = 0.1f * (list.Count - 1);
            int i0 = (int)Math.Floor(pos); int i1 = Math.Min(list.Count - 1, i0 + 1);
            float v = (list[i0] + (list[i1] - list[i0]) * (pos - i0)) / 255f;
            return v <= 0.04045f ? v / 12.92f : (float)Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        /// <summary>StageUnitLit の _EmissionIntensity の既定 (シェーダの Properties と同じ値)</summary>
        const float DefaultEmissionIntensity = 1.6f;

        /// <summary>
        /// キャラの光の露出の割り戻し (2026-09-30 P23): look の char に exposureRef があれば 2^(exposureRef − 今の post.exposure) (0.25〜4 倍)。無ければ 1。
        /// 受光・環境光・発光の強さに掛ける = 舞台の露出 (P22) を下げて夜にしても、キャラの画面の明るさは保つ (主役の照明と同じ考え)
        /// </summary>
        static float CharExposureScale(CharMatExtras x)
        {
            var cur = StageLook.Current;
            if (x == null || cur == null || float.IsNaN(x.ExposureRef)) return 1f;
            return Mathf.Pow(2f, Mathf.Clamp(x.ExposureRef - cur.Post.Exposure, -2f, 2f));
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
        static readonly Dictionary<Texture2D, float> _edgeLinCache = new Dictionary<Texture2D, float>();   // 絵の輪郭の暗さ (P23。StageUnit.EdgeLin)
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
                    string stem;
                    if (!AnimStem.TryGetValue(kv.Key, out stem)) stem = kv.Key;   // 二周目 レーン E: 箱庭の待機は r2idle の絵
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        string f = ArtName + "_" + stem + "_" + i;
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

            float _edgeLin = -2f;   // 絵の輪郭の暗さ (線形。-2 = まだ測っていない・-1 = 測れない)
            /// <summary>この板の一枚絵の輪郭の暗さ (EdgeDarkLinearFrom。絵ごとに1回だけ測って覚える。読めない絵は -1 = 輪郭の持ち上げを割り引かない)</summary>
            public float EdgeLin()
            {
                if (_edgeLin > -1.5f) return _edgeLin;
                _edgeLin = -1f;
                if (BaseTex == null) return _edgeLin;
                float cached;
                if (_edgeLinCache.TryGetValue(BaseTex, out cached)) return _edgeLin = cached;
                try
                {
                    int tw = BaseTex.width, th = BaseTex.height;
                    int x0 = Mathf.RoundToInt(BaseUvOffset.x * tw), y0 = Mathf.RoundToInt(BaseUvOffset.y * th);
                    int x1 = x0 + Mathf.RoundToInt(BaseUvScale.x * tw), y1 = y0 + Mathf.RoundToInt(BaseUvScale.y * th);
                    _edgeLin = EdgeDarkLinearFrom(BaseTex.GetPixels32(), tw, x0, y0, x1, y1);
                }
                catch (Exception) { _edgeLin = -1f; }
                _edgeLinCache[BaseTex] = _edgeLin;
                return _edgeLin;
            }

            /// <summary>2周目 (P23) の dumplayout 用: 最後に解いた板の画面の事情 (周辺減光・霧) と _CharTint・_ShadeLift・発光の強さ</summary>
            public float LastVignette = 1f, LastFog, LastEmission = -1f;
            public Vector3 LastTint = Vector3.one; public Vector2 LastShade;

            /// <summary>光を受ける板の材質の値 (毎フレーム。旗 receive=・herolift=・keyflip= と設計図 look の char が変わっても追う)。
            /// sx, sy = 足元の画面の点 (px・左下が原点)・h = 絵の高さ (px)。輪郭の目標 (outlineTarget) の周辺減光を板の真ん中で読む</summary>
            void ApplyLitProps(bool hero, float sx, float sy, float h)
            {
                var x = CharExtras();
                var art = x.ArtFor(ArtName);
                float es = CharExposureScale(x);   // 露出の割り戻し (look の char の exposureRef。無ければ 1)
                // 受光: 旗 receive= ＞ 絵ごとの上書き (look の char の art の receive。P23 2周目: このはだけ舞台の灯を強く受ける) ＞ 設計図の receive / heroReceive
                float receive = HD2DFlags.Receive < 0f && art != null && art.Receive >= 0f ? art.Receive : StageLook.CharReceive(hero);
                Mat.SetFloat("_Receive", receive * es);
                // 主役の持ち上げ: 旗 herolift= > 絵ごとの上書き (look の char の art。P23: このはだけ上げ、白い衣のひなたは上げない) > look の heroLift
                float heroLift = StageLook.HeroLift;
                if (hero && HD2DFlags.HeroLift < 0f && art != null && art.HeroLift >= 0f) heroLift = art.HeroLift;
                Mat.SetFloat("_HeroLift", hero ? heroLift : 1f);
                // 環境光の倍率: 絵ごとの上書き (二周目 レーン E: 暗めの敵 = art の "enemy_" の ambientScale) ＞ look の ambientScale。どちらも露出の割り戻しを掛ける
                float ambScale = art != null && art.AmbientScale >= 0f ? art.AmbientScale : StageLook.CharAmbientScale;
                ambScale *= R3E_SeatAmbientMul(x);   // 三周目 レーン E (R11): 群れの奥の座席・人形の後列ほど暗く (look の char.seatAmbient・dollBackAmbient。無ければ 1)
                Mat.SetFloat("_AmbientScale", ambScale * es);
                R3E_ApplyLitExtras(x, art, hero);   // 三周目 レーン E (R5 (b)(c)): 足元ほど暗い勾配・輪郭の1画素 (無ければ 0 = 二周目)
                // 霧を受ける割合 (二周目 レーン E: look の char.fog。無ければ W5 の 1)。輪郭の目標 (ViewFor) がこの値を読むので先に書く
                Mat.SetFloat("_Fog", x.Fog >= 0f ? Mathf.Clamp01(x.Fog) : 1f);
                // 鮮やかさ (二周目 レーン E: 絵ごと ＞ look の char.saturation ＞ 1 = W5)
                float sat = art != null && art.Saturation >= 0f ? art.Saturation : (x.Saturation >= 0f ? x.Saturation : 1f);
                Mat.SetFloat("_CharSat", sat);
                // _KeyFlip の絵は look の char の keyFlipFrom で選ぶ (P23: 既定は描き込まれた光の向きを測った表。旗 keyflip=off なら全部 0)。dumplayout の keyFlip もこの値
                KeyFlipArt = ResolveKeyFlip(ArtName, x);
                Mat.SetFloat("_KeyFlip", KeyFlipArt && HD2DFlags.KeyFlip == HD2DKeyFlip.Auto ? 1f : 0f);
                // キャラの色の掛け算 (P23 2周目): 後処理の colorFilter の打ち消し × 暖かさ。暗い色の持ち上げ (このはだけ = art の shadeLift)
                var tint = CharTintFor(x);
                Mat.SetVector("_CharTint", new Vector4(tint.x, tint.y, tint.z, 0f));
                var shade = ShadeLiftFor(x, art);
                Mat.SetVector("_ShadeLift", new Vector4(shade.x, shade.y, 0f, 0f));
                LastTint = tint; LastShade = shade;
                // 輪郭の持ち上げ (P23)。書いていなければ 0。2周目: outlineTarget があれば画面の暗さから解く (周辺減光は板の真ん中・霧は板の深さ)。
                // 絵の輪郭がもともと明るい絵 (ひなた・白の人形) は持ち上げを減らす
                UnitView view = UnitView.Neutral;
                if (!float.IsNaN(x.OutlineTarget) && Screen.width > 0 && Screen.height > 0)
                {
                    view = ViewFor(sx / Screen.width, (sy + h * 0.5f * (1f - FeetPad)) / Screen.height, UsedDepth, Mat.GetFloat("_Fog"), tint);
                    LastVignette = view.Vignette; LastFog = view.FogAmount;
                }
                Mat.SetVector("_BlackLift", BlackLiftVector(x, hero, art, EdgeLin(), es * (hero ? heroLift : 1f), view));
                // 近くの点光源を受ける割合: 絵ごと (二周目 レーン E: このは = 斧の宝石の灯) ＞ look の localLights ＞ 0
                Mat.SetFloat("_LocalLights", art != null && art.LocalLights >= 0f ? art.LocalLights : (x.LocalLights >= 0f ? x.LocalLights : 0f));
                if (x.WhiteCap >= 0f) Mat.SetFloat("_WhiteCap", x.WhiteCap);
                // 発光の強さ: 絵ごと (art の emission。狼の白い毛) ＞ 設計図の emissionIntensity ＞ シェーダの既定 1.6。露出の割り戻しが戻った時も書き直す
                float em = art != null && art.Emission >= 0f ? art.Emission : (x.EmissionIntensity >= 0f ? x.EmissionIntensity : DefaultEmissionIntensity);
                LastEmission = em;
                Mat.SetFloat("_EmissionIntensity", em * es);
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
                // 二周目 レーン E: 接地影の暈 (材質は板ごと・絵は共有) と宝石の灯
                if (_r2eHalo != null)
                {
                    var hmr2 = _r2eHalo.GetComponent<MeshRenderer>();
                    if (hmr2 != null && hmr2.sharedMaterial != null) Destroy(hmr2.sharedMaterial);
                    Destroy(_r2eHalo.gameObject);
                }
                if (_r2eGem != null) Destroy(_r2eGem.gameObject);
                // 光の板の色の差し替え (R2E_SyncHaloTint): 上で捨てたのは今貼っている絵なので、もう片方 (元の暖色か宝石の色) を捨てる
                if (_r2eHaloTinted) { if (_r2eHaloTexOrig != null) Destroy(_r2eHaloTexOrig); }
                else if (_r2eHaloGemTex != null) Destroy(_r2eHaloGemTex);
                if (Mat != null) Destroy(Mat);
            }
            public void LateUpdate()
            {
                if (Rect == null) { Destroy(gameObject); return; }
                R2E_SyncIdle(HD2DFlags.StageMode == HD2DStage.Diorama);   // 二周目 レーン E: 箱庭の待機のコマ (look の char.r2idle が無ければ何もしない)
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
                    breathe = Mathf.Sin(Time.time * (2.4f + BreathePhase * 0.08f) + R3E_BreathePhase()) * (Key == "player" ? 2f : 3f);   // 三周目 レーン E (R11): 位相は座席の番号で散らす (無ければ BreathePhase = 二周目)
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
                if (IsLit) ApplyLitProps(hero, sx, sy, h);   // 光を受ける板: 固定のキー＋上下の環境光 (StageLook の全体値)。見本は PalOf・主役の照明・周辺減光の打ち消しを使わない (計画 §2-4)
                else
                {
                    // 敵と人形は絵の真ん中の画面の位置で周辺減光を打ち消す (F06: 同じ噛みつく巻物が ①→④ で明るさ半分・青く曇った)
                    Color lift = hero || Screen.width <= 0 || Screen.height <= 0 ? Color.white : VignetteLift(sx / Screen.width, (sy + h * 0.5f * (1f - FeetPad)) / Screen.height);
                    ApplyLight(Mat, UnitSunAmount, hero, !hero, lift, doll, HeroLight);
                }
                float rim = hero ? UnitRim : CharRim;
                if (IsLit) rim = R2E_LitRim(hero, doll, rim);   // 二周目 レーン E: 光を受ける板のリムは look の char (敵 0・人形 0.1・主役 0.25)。無ければ今のまま
                Mat.SetFloat("_Rim", rim);
                if (FlashT > 0f) FlashT -= Time.deltaTime;
                Mat.SetFloat("_Flash", Mathf.Clamp01(FlashT / 0.18f) * 0.85f);
                Mat.SetFloat("_Dissolve", DissolveK);
                SyncShadowMode(dio);
                if (Shadow != null)
                {
                    float ww = w * k;
                    float sa = tint.a * (1f - DissolveK);
                    bool core = false; float haloW = -1f, haloA = 0f, coreFlat = 0f, depthMul = 1f;
                    if (CharShadowOn)
                    {
                        // 本物の影 (舞台の灯) が落ちる時は、足元の接地影の楕円を濃さ 0.35 倍・幅 0.6 倍に (計画 P11 手順3)。
                        // 二周目 レーン E の 3 (char C5・N15): look の char の contactCore があれば芯のある影 (幅 contactWidth・濃さ contactAlpha 倍) と、
                        // contactHalo があれば同じ位置に広く薄い暈 (芯の幅の width 倍・元の濃さの alpha 倍・今の柔らかい楕円)。無ければ W5 のまま
                        var cx = CharExtras();
                        float baseSa = sa;
                        core = cx.ContactCore >= 0f; coreFlat = cx.ContactCore;
                        ww *= cx.ContactWidth >= 0f ? cx.ContactWidth : 0.6f;
                        sa *= cx.ContactAlpha >= 0f ? cx.ContactAlpha : 0.35f;
                        if (cx.HaloWidth > 0f && cx.HaloAlpha > 0f) { haloW = ww * cx.HaloWidth; haloA = baseSa * cx.HaloAlpha; }
                        if (cx.ContactDepth > 0f) depthMul = cx.ContactDepth;   // 段2: 低いカメラで潰れる楕円の奥行きを戻す (無ければ 1 = W5)
                    }
                    // 見本は座席の地面の高さ (帯は平ら)。old は今どおり 0
                    var gp = new Vector3(ground.x, SeatFound ? SeatWorld.y : 0f, ground.z);
                    R2E_SyncShadowTex(core, coreFlat);   // 芯の有無が変わった時だけ絵を貼り替える (旗なし・W5 では一度も触らない)
                    PlaceBlob(Shadow, gp, ww, sa, depthMul);
                    R2E_SyncHalo(gp, haloW, haloA, depthMul);
                    _r2eDepthMul = depthMul;
                }
                if (Halo != null)
                {
                    float ww = w * k, hh = h * k;
                    var huv = R2E_HaloUv(dio);   // 二周目 レーン E: 箱庭で look の char.heroGem がある時は斧の宝石 (無ければ今の HaloUv)
                    Halo.position = pos + _right * ((huv.x - 0.5f) * ww) + _up * (huv.y * hh) - _fwd * 0.05f;
                    Halo.rotation = rot;
                    float sz = 0.9f * (1f + 0.06f * Mathf.Sin(Time.time * 5f));
                    Halo.localScale = new Vector3(sz, sz, 1f);
                    R2E_SyncGem(dio, Halo.position);   // 宝石の灯 (点光源。heroGem が無い・スマホ・光を受けない板では消えている)
                    R2E_SyncHaloTint(dio);   // 宝石の位置へ動いた光の板の色を宝石の灯の色へ (箱庭で heroGem がある時だけ。今の舞台・W5 では触らない)
                    // 直しの輪1 (2026-10-01): 光の板の位置は待機の絵の宝石 (R2E_HaloUvByArtDiorama) なので、振り・構えのコマでは斧が動いて宙に残った
                    // (R01 の1〜3コマ目、主人公の頭の右上に青緑の玉)。箱庭で heroGem の表がある絵は、待機以外のコマで板を隠す。今の舞台・表の無い絵は今まで
                    bool r2HideHalo = dio && Anim != "idle" && ArtName != null && CharExtras().Gem && R2E_HaloUvByArtDiorama.ContainsKey(ArtName);
                    if (Halo.gameObject.activeSelf == r2HideHalo) Halo.gameObject.SetActive(!r2HideHalo);
                }
            }

            // ---------------------------------------------------------------- 二周目 レーン E (2026-10-01)。どれも look の char のキーが無ければ何もしない (W5 と同じ)

            bool _r2eCoreOn; float _r2eCoreFlat = -1f;
            Transform _r2eHalo;
            /// <summary>dumplayout (DebugUnitBoxes) 用: 待機のコマ・接地影の芯・宝石の灯の今の状態</summary>
            public string R2E_IdleInfo => _r2eIdleState == 1 ? "on" : _r2eIdleState == 2 ? "missing" : "off";
            public bool R2E_ContactInfo => _r2eCoreOn;
            float _r2eDepthMul = 1f;
            /// <summary>dumplayout 用: 接地影の楕円の奥行きの倍率 (char.contactDepth。1 = 今のまま)</summary>
            public float R2E_ContactDepthInfo => _r2eDepthMul;
            public object R2E_GemInfo => _r2eGem != null && _r2eGem.enabled
                ? (object)new Dictionary<string, object> { { "intensity", Mathf.Round(_r2eGem.intensity * 1000f) / 1000f }, { "range", _r2eGem.range }, { "color", new[] { _r2eGem.color.r, _r2eGem.color.g, _r2eGem.color.b } }, { "pos", _r2eGem.transform.position } }
                : null;
            /// <summary>接地影の絵: 芯のある影 (R2E_BlobCoreTex) か今の柔らかい楕円 (BlobTex)。変わった時だけ貼り替える (旗なし・W5 では一度も触らない)</summary>
            void R2E_SyncShadowTex(bool core, float flat)
            {
                if (core == _r2eCoreOn && (!core || Mathf.Abs(flat - _r2eCoreFlat) < 1e-4f)) return;
                _r2eCoreOn = core; _r2eCoreFlat = flat;
                var smr = Shadow != null ? Shadow.GetComponent<MeshRenderer>() : null;
                if (smr == null || smr.sharedMaterial == null) return;
                smr.sharedMaterial.mainTexture = core ? R2E_BlobCoreTex(flat) : BlobTex();
            }

            /// <summary>接地影に重ねる広く薄い暈 (width ≤ 0 なら隠す)。今の柔らかい楕円 (BlobTex) を芯より先に描く (renderQueue 2999)。初めて要る時に作る</summary>
            void R2E_SyncHalo(Vector3 ground, float width, float alpha, float depthMul = 1f)
            {
                if (width <= 0f || alpha <= 0f)
                {
                    if (_r2eHalo != null && _r2eHalo.gameObject.activeSelf) _r2eHalo.gameObject.SetActive(false);
                    return;
                }
                if (_r2eHalo == null)
                {
                    var go = Blob("shadow-halo-" + (Key ?? "unit"), _units, ground, width);
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null) mr.sharedMaterial.renderQueue = 2999;
                    _r2eHalo = go.transform;
                }
                if (!_r2eHalo.gameObject.activeSelf) _r2eHalo.gameObject.SetActive(true);
                PlaceBlob(_r2eHalo, new Vector3(ground.x, ground.y - 0.005f, ground.z), width, alpha, depthMul);
            }

            // ---- 待機のコマ (レーン E の 7・char C8): 箱庭 (stage=diorama) かつ look の char.r2idle の時だけ、Art/enemies/anim/<名前>_r2idle_<n> を待機にする ----
            int _r2eIdleState;   // 0 = 使っていない・1 = r2idle を待機にしている・2 = 欲しいが絵が無い (コードの上下のまま)
            List<Texture2D> _r2eSavedIdle; float[] _r2eSavedDur; bool _r2eSavedBreathe;
            /// <summary>待機のコマの絵の名前の頭 (動きの名前 → ファイルの名前の動きの部分。無ければ動きの名前のまま)。EnsureMaps・R2E_SyncIdle が読む</summary>
            public readonly Dictionary<string, string> AnimStem = new Dictionary<string, string>(StringComparer.Ordinal);

            void R2E_SyncIdle(bool dio)
            {
                bool want = dio && Key != null && Key != "player" && StageLook.Current != null && CharExtras().Idle;
                if (want && _r2eIdleState != 0) return;
                if (!want && _r2eIdleState == 0) return;
                if (!want)
                {
                    if (_r2eIdleState == 1)
                    {
                        if (_r2eSavedIdle != null) Anims["idle"] = _r2eSavedIdle; else Anims.Remove("idle");
                        if (_r2eSavedDur != null) FrameDur["idle"] = _r2eSavedDur; else FrameDur.Remove("idle");
                        AnimStem.Remove("idle");
                        Breathe = _r2eSavedBreathe;
                        R2E_ReloadIdleMaps();
                        if (Anim == "idle") { Frame = 0; FrameT = 0f; Apply(); }
                    }
                    _r2eIdleState = 0;
                    return;
                }
                var frames = new List<Texture2D>();
                for (int i = 0; i < 16; i++)
                {
                    var f = Theme.Art(AnimFolder, ArtName + "_r2idle_" + i);
                    if (f == null) break;
                    frames.Add(f.texture);
                }
                if (frames.Count == 0 || BaseTex == null || frames[0].width != BaseTex.width || frames[0].height != BaseTex.height) { _r2eIdleState = 2; return; }
                List<Texture2D> old; float[] oldDur;
                _r2eSavedIdle = Anims.TryGetValue("idle", out old) ? old : null;
                _r2eSavedDur = FrameDur.TryGetValue("idle", out oldDur) ? oldDur : null;
                _r2eSavedBreathe = Breathe;
                var x = CharExtras();
                bool slow = false;
                foreach (var p in x.IdleSlow) if (ArtName != null && ArtName.StartsWith(p, StringComparison.Ordinal)) { slow = true; break; }
                float dur = slow ? x.IdleSlowDur : x.IdleDur;
                var durs = new float[frames.Count];
                for (int i = 0; i < durs.Length; i++) durs[i] = dur;
                Anims["idle"] = frames; FrameDur["idle"] = durs; AnimStem["idle"] = "r2idle";
                Breathe = false;   // コマで息をするので、コードの上下 (足元ごと動く) は止める
                R2E_ReloadIdleMaps();
                _r2eIdleState = 1;
                if (Anim == "idle")
                {
                    // 全員が同じ拍で息をしないよう、時刻と板ごとの位相から途中のコマで始める (盤面の作り直しでも拍が続く)
                    float cyc = dur * frames.Count;
                    float tt = Mathf.Repeat(Time.time + R3E_IdleOffset(cyc), cyc);   // 三周目 レーン E (R11): 座席の番号で散らす (look の char.idlePhaseSpread。無ければ BreathePhase × 0.37 = 二周目)
                    Frame = Mathf.Clamp((int)(tt / dur), 0, frames.Count - 1); FrameT = tt - Frame * dur;
                    Apply();
                }
            }

            /// <summary>待機の法線・発光を引き直す (EnsureMaps が済んでいる時だけ。まだなら EnsureMaps が AnimStem で引く)</summary>
            void R2E_ReloadIdleMaps()
            {
                if (!_mapsLoaded) return;
                List<Texture2D> frames;
                if (!Anims.TryGetValue("idle", out frames)) { _animN.Remove("idle"); _animE.Remove("idle"); return; }
                string stem;
                if (!AnimStem.TryGetValue("idle", out stem)) stem = "idle";
                var ln = new List<Texture2D>(); var le = new List<Texture2D>();
                for (int i = 0; i < frames.Count; i++)
                {
                    string f = ArtName + "_" + stem + "_" + i;
                    ln.Add(MapTex(AnimFolder, f + "_n"));
                    le.Add(MapTex(AnimFolder, f + "_e"));
                }
                _animN["idle"] = ln; _animE["idle"] = le;
            }

            // ---------------------------------------------------------------- 三周目 レーン E (2026-10-01・計画 §2 E・分析 R5 (b)(c)・R11)
            // どれも箱庭 (stage=diorama) の時だけ・look の char のキーが無ければ二周目と同じ (今の舞台 = 幕2/3 は1画素も変えない)

            /// <summary>dumplayout 用: 最後に掛けた座席の環境光の倍率・座席の番号・敵の編成の頭数の見当・待機の位相 (秒)</summary>
            public float R3E_LastSeatAmb = 1f, R3E_LastIdleOffset;
            public int R3E_LastSlot = -1, R3E_LastGroup;
            Vector3 _r3eSlotFor = new Vector3(float.NaN, 0f, 0f);
            int _r3eSlot = -1, _r3eGroup; bool _r3eDollBack;

            bool R3E_IsDoll => Key != null && Key.StartsWith("doll:", StringComparison.Ordinal);

            /// <summary>敵の番号 (key "enemyN" の N。敵でなければ −1)</summary>
            int R3E_EnemyIndex()
            {
                if (Key == null || Key.Length <= 5 || !Key.StartsWith("enemy", StringComparison.Ordinal)) return -1;
                int n;
                return int.TryParse(Key.Substring(5), out n) && n >= 0 ? n : -1;
            }

            /// <summary>
            /// 座席の番号を座席の世界の点から引く (座席が変わった時だけ計算し直す。板は組み直しのたびに作り直されるので、ほぼ1回)。
            /// 敵 = enemyN の N と、編成の頭数 (座席の表 EnemySeats(n) のどの n の N 番目と同じ点か = BattleView が Stage.EnemySlots(敵の数) を ProjectFeet に渡す)。
            /// 人形 = いちばん近い DollSlots(9) の番号 (5 以上 = 後列。大きい人形は同じ列の2席の間なので列は同じ)。主人公 = −1
            /// </summary>
            void R3E_ResolveSlot()
            {
                Vector3 w = Vector3.zero; float k;
                bool has = Key != null && TryGetSeat(Key, out w, out k);
                if (has && w == _r3eSlotFor) return;
                _r3eSlotFor = has ? w : new Vector3(float.NaN, 0f, 0f);
                int ei = R3E_EnemyIndex();
                _r3eSlot = -1; _r3eGroup = 0; _r3eDollBack = false;
                if (ei >= 0)
                {
                    _r3eSlot = ei;
                    _r3eGroup = has ? R3E_GroupFromSeat(ei, w) : ei + 1;
                }
                else if (R3E_IsDoll && has)
                {
                    var slots = DollSlots(9);
                    float bd = float.MaxValue;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        float d = (slots[i] - w).sqrMagnitude;
                        if (d < bd) { bd = d; _r3eSlot = i; }
                    }
                    _r3eDollBack = _r3eSlot >= 5;
                }
                R3E_LastSlot = _r3eSlot; R3E_LastGroup = _r3eGroup;
            }

            /// <summary>敵の編成の頭数の見当: 自分の座席が EnemySeats(n) の idx 番目と同じ点になる n (1〜4)。どれとも合わなければ 5 体以上の戦闘 (= 3 体以上)</summary>
            static int R3E_GroupFromSeat(int idx, Vector3 w)
            {
                int best = -1; float bd = float.MaxValue;
                for (int n = Mathf.Max(1, idx + 1); n <= 4; n++)
                {
                    var s = EnemySeats(n);
                    if (idx >= s.Length) continue;
                    float d = (s[idx] - w).sqrMagnitude;
                    if (d < bd) { bd = d; best = n; }
                }
                return best < 0 || bd > 0.04f ? Mathf.Max(idx + 1, 5) : best;
            }

            /// <summary>R11: 環境光の倍率 = 敵は座席の番号の seatAmbient (最後の値で止める)・人形の後列は dollBackAmbient。主人公・今の舞台・キーが無い時は 1</summary>
            float R3E_SeatAmbientMul(CharMatExtras x)
            {
                R3E_LastSeatAmb = 1f;
                if (HD2DFlags.StageMode != HD2DStage.Diorama || Key == null || Key == "player") return 1f;
                if (x.R3E_SeatAmbient == null && x.R3E_DollBackAmbient < 0f) return 1f;
                R3E_ResolveSlot();
                float m = 1f;
                var sa = x.R3E_SeatAmbient;
                if (R3E_EnemyIndex() >= 0 && sa != null && sa.Length > 0 && _r3eSlot >= 0) m = sa[Mathf.Min(_r3eSlot, sa.Length - 1)];
                else if (R3E_IsDoll && _r3eDollBack && x.R3E_DollBackAmbient >= 0f) m = x.R3E_DollBackAmbient;
                R3E_LastSeatAmb = m;
                return m;
            }

            /// <summary>
            /// R5 (b)(c) の材質の値 (毎フレーム): _BodyShade = 編成の頭数が minGroup 以上の敵と人形にだけ (主人公・1〜2体の敵・幕ボスは 0)。
            /// 足元の uv = 絵の下の余白 FeetPad (広い枠のコマは FrameScaleY で割る)・勾配が 1 に戻る uv = 足元 + top × 絵の高さ。
            /// _EdgeSoft・_EdgeA2C = 輪郭の1画素 (全員。MSAA が効いている時だけ。R3E_MsaaOn)
            /// </summary>
            void R3E_ApplyLitExtras(CharMatExtras x, ArtLook art, bool hero)
            {
                bool dio = HD2DFlags.StageMode == HD2DStage.Diorama;
                float bs = 0f;
                if (dio && !hero && Key != null)
                {
                    R3E_ResolveSlot();
                    bool doll = R3E_IsDoll;
                    bool enemyInGroup = R3E_EnemyIndex() >= 0 && _r3eGroup >= x.R3E_BodyShadeMinGroup;
                    if (doll || enemyInGroup)
                        bs = art != null && art.BodyShade >= 0f ? Mathf.Clamp01(art.BodyShade) : (doll ? x.R3E_BodyShadeDoll : x.R3E_BodyShadeEnemy);
                }
                if (bs > 0f)
                {
                    float fsy = Mathf.Max(1f, FrameScaleY);
                    float feet = Mathf.Clamp01(FeetPad / fsy);
                    float top = Mathf.Clamp(feet + x.R3E_BodyShadeTop * (1f - FeetPad) / fsy, feet + 0.01f, 1f);
                    Mat.SetVector("_BodyShade", new Vector4(bs, feet, top, x.R3E_BodyShadePower));
                }
                else Mat.SetVector("_BodyShade", new Vector4(0f, 0f, 1f, 1f));
                float es = dio && x.R3E_EdgeSoft > 0f && R3E_MsaaOn() ? x.R3E_EdgeSoft : 0f;
                Mat.SetFloat("_EdgeSoft", es);
                Mat.SetFloat("_EdgeA2C", es > 0f ? 1f : 0f);
            }

            /// <summary>座席の番号で散らす待機の位相 (s = look の char.idlePhaseSpread・f = 座席の番号の 2進の逆順)。散らさない時は false</summary>
            bool R3E_SpreadPhase(out float s, out float f)
            {
                s = 0f; f = 0f;
                if (HD2DFlags.StageMode != HD2DStage.Diorama || Key == null || Key == "player" || StageLook.Current == null) return false;
                var x = CharExtras();
                if (!(x.R3E_IdlePhaseSpread > 0f)) return false;
                R3E_ResolveSlot();
                if (_r3eSlot < 0) return false;
                s = x.R3E_IdlePhaseSpread; f = R3E_VanDerCorput(_r3eSlot);
                return true;
            }

            /// <summary>
            /// 待機のコマ (r2idle) の始まりのずれ (秒)。二周目 = BreathePhase × 0.37 (板ごとの乱数 = 同じ絵の4体のうち2体が同じコマになることがあった)。
            /// 散らす時は位相 (周期の割合) を二周目の乱数と座席の番号 (0・½・¼・¾ = 隣の座席ほど離れる) の間で s の割合に寄せる。周期は全員同じなので、ずれはそのまま続く
            /// </summary>
            float R3E_IdleOffset(float cyc)
            {
                float r2 = BreathePhase * 0.37f;
                R3E_LastIdleOffset = r2;
                float s, f;
                if (!(cyc > 1e-4f) || !R3E_SpreadPhase(out s, out f)) return r2;
                float h = Mathf.Repeat(r2 / cyc, 1f);
                float off = Mathf.Repeat(h * (1f - s) + f * s, 1f) * cyc;
                R3E_LastIdleOffset = off;
                return off;
            }

            /// <summary>コードの上下 (待機のコマが無い絵) の位相 (ラジアン)。散らさない時は BreathePhase (二周目)。周期は二周目のまま (板ごとに少しずつ違う)</summary>
            float R3E_BreathePhase()
            {
                float s, f;
                if (!R3E_SpreadPhase(out s, out f)) return BreathePhase;
                const float Tau = Mathf.PI * 2f;
                float h = Mathf.Repeat(BreathePhase / Tau, 1f);
                return Mathf.Repeat(h * (1f - s) + f * s, 1f) * Tau;
            }

            // ---- 斧の宝石の灯 (レーン E の 5・char C11): 主役・光を受ける板・箱庭・tier=pc・look の char.heroGem の時だけ。点光源 (影なし) ----
            Light _r2eGem;
            /// <summary>杖の先の光の板の中の位置: 箱庭で heroGem がある時は R2E_HaloUvByArtDiorama (宝石・ランタンの火)、無ければ今の HaloUv</summary>
            Vector2 R2E_HaloUv(bool dio)
            {
                Vector2 uv;
                if (dio && ArtName != null && CharExtras().Gem && R2E_HaloUvByArtDiorama.TryGetValue(ArtName, out uv)) return uv;
                return HaloUv;
            }

            void R2E_SyncGem(bool dio, Vector3 at)
            {
                var x = CharExtras();
                Color col = Color.white;
                bool want = Key == "player" && dio && IsLit && HD2DFlags.LitUnits && HD2DFlags.Tier != HD2DTier.Phone && x.Gem
                    && R2E_PrefixMatch(x.GemColors, ArtName, out col) && R2E_HaloUvByArtDiorama.ContainsKey(ArtName ?? "");
                if (!want)
                {
                    if (_r2eGem != null && _r2eGem.enabled) _r2eGem.enabled = false;
                    return;
                }
                if (_r2eGem == null)
                {
                    var go = new GameObject("gem-light");
                    go.transform.SetParent(_units, false);
                    _r2eGem = go.AddComponent<Light>();
                    _r2eGem.type = LightType.Point;
                    _r2eGem.shadows = LightShadows.None;
                }
                if (!_r2eGem.enabled) _r2eGem.enabled = true;
                _r2eGem.transform.position = at - _fwd * x.GemPush;   // 板より少し手前 (板の画素の法線が灯の方を向く)
                _r2eGem.range = x.GemRange;
                _r2eGem.color = col;
                // 待機以外のコマ (振り・構え) では斧が動いて宝石が灯から離れる = 宙に残らないよう弱める
                _r2eGem.intensity = x.GemIntensity * (Anim == "idle" ? 1f : x.GemMoving);
            }

            // 杖の先の光の板 (staff-glow) の色 (段2 の仕上げ): 板は R2E_HaloUv で宝石 (このは = 青緑)・ランタンの火 (ひなた = 暖色) へ動くが、
            // 絵は今の淡い暖色 (1, 0.86, 0.5) のまま = このはの青緑の宝石のまわりに黄色いにじみが出ていた。
            // 箱庭で look の char.heroGem がある時だけ、板の絵を宝石の灯の色 (heroGem.color。濃さ 0.5 は今と同じ) の放射に差し替える。
            // スマホ (点光源を作らない) でも板は宝石の位置へ動くので、色はそろえる。heroGem が無い・今の舞台では一度も触らない (元の絵のまま)。
            // 絵は板ごと (OnDestroy が今貼っている絵を捨て、もう片方はこの下の値で捨てる)
            Texture _r2eHaloTexOrig; Texture2D _r2eHaloGemTex; bool _r2eHaloTinted; Color _r2eHaloGemCol;
            void R2E_SyncHaloTint(bool dio)
            {
                var hmr = Halo != null ? Halo.GetComponent<MeshRenderer>() : null;
                if (hmr == null || hmr.sharedMaterial == null) return;
                var x = CharExtras();
                Color col = Color.white;
                bool want = dio && x.Gem && ArtName != null && R2E_HaloUvByArtDiorama.ContainsKey(ArtName) && R2E_PrefixMatch(x.GemColors, ArtName, out col);
                var m = hmr.sharedMaterial;
                if (want)
                {
                    if (!_r2eHaloTinted) { _r2eHaloTexOrig = m.mainTexture; _r2eHaloTinted = true; }
                    if (_r2eHaloGemTex == null || col != _r2eHaloGemCol)
                    {
                        if (_r2eHaloGemTex != null) Destroy(_r2eHaloGemTex);
                        _r2eHaloGemTex = Px.Radial(new Color(col.r, col.g, col.b, 0.5f));
                        _r2eHaloGemCol = col;
                    }
                    if (m.mainTexture != _r2eHaloGemTex) m.mainTexture = _r2eHaloGemTex;
                }
                else if (_r2eHaloTinted)
                {
                    m.mainTexture = _r2eHaloTexOrig;
                    _r2eHaloTinted = false;
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

        static Texture2D _R2E_blobCoreTex; static float _R2E_blobCoreFlat = -1f;
        /// <summary>
        /// 芯のある接地影の絵 (二周目 レーン E の 3・char C5・N15): 半径の flat (既定 0.55) まで平らに濃く、そこから smoothstep で縁の 0 へ。
        /// 64×64・全員で共有 (板ごとの材質の mainTexture に貼る。捨てない)。今の BlobTex (Px.Glow = 中心から (1−d)² で消える円錐) は芯が無く、足の真下が暗くならなかった
        /// </summary>
        static Texture2D R2E_BlobCoreTex(float flat)
        {
            flat = Mathf.Clamp(flat, 0f, 0.95f);
            if (_R2E_blobCoreTex != null && Mathf.Abs(_R2E_blobCoreFlat - flat) < 1e-4f) return _R2E_blobCoreTex;
            if (_R2E_blobCoreTex != null) UnityEngine.Object.Destroy(_R2E_blobCoreTex);
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = d <= flat ? 1f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - flat) / Mathf.Max(1e-3f, 1f - flat)));
                    px[y * n + x] = new Color(1f, 1f, 1f, d >= 1f ? 0f : a);
                }
            t.SetPixels(px); t.Apply();
            _R2E_blobCoreTex = t; _R2E_blobCoreFlat = flat;
            return t;
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
        static void PlaceBlob(Transform blob, Vector3 basePos, float width, float alpha, float depthMul = 1f)
        {
            float w = width * 0.8f, d = width * 0.35f * depthMul;   // depthMul: 二周目 段2 の char.contactDepth (既定 1 = 今のまま)
            blob.position = new Vector3(basePos.x, basePos.y + 0.03f, basePos.z);   // 中心原点の板 = そのまま足元
            blob.rotation = Quaternion.Euler(90f, 0f, 0f);
            blob.localScale = new Vector3(w, d, 1f);
            var mr = blob.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial.color = new Color(ShadowColor.r, ShadowColor.g, ShadowColor.b, ShadowColor.a * alpha);
        }
    }
}
