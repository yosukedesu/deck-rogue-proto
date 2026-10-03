// StageMotion.cs — 舞台の動き (段2 2026-10-03 レーン M。計画 docs/design/hd2d-stage2-plan-2026-10-02.md §2 M・約束 docs/design/hd2d-stage2/contracts.md §C5・
// 分析書 docs/design/hd2d-stage2-analysis-2026-10-02.md §8)。
// 本家の「技で 3D 背景が輝く」は3つの重なり (①寄り＝1フレームで近い画へ ②その間だけ技の点光源で壁と床を照らす ③技の前後は舞台を暗くしておく) だった。
// その3つと、敵の大技の板のフラッシュ・火花・ピントの連動をここで持つ。Presenter が2Dの当たりと同じ所で口を呼ぶ。
//
// 門: StageFx.Live (有効な箱庭で光が当たっている) かつ 光の設計図の motion.on が true (On)。どちらかが偽なら何もしない。
//     Presenter は門が偽なら計画 (Presenter.PlanMotion) も口の呼び出しもしない = 今の演出 (2Dの当たり・StageFx の光・ZoomPunch・Dolly・HitStop) は
//     1行も通り方が変わらない。幕1 の光の設計図には motion を書かない = 幕1 の見本と今の舞台 (stage=old) は1画素も変わらない。
// 時間: Time.deltaTime の積算 (StageDriver の LateUpdate が Tick を呼ぶ。det の撮影では 1/60 秒ずつ = 何コマ目に何が起きるかが決まる。
//       ヒットストップの間はゆっくり進む)。乱数は使わない (火花の向きは黄金角と添字の小数部)。待機中にカメラは動かさない (本家 0.00px)。OT2 の回転は入れない。
//
// 寄り (zoom): 対象 (当たった敵) の体の点 (板の高さの pivot) を軸に、レイアウト用のカメラを下へ lookUp 度回し (見上げ)、その点へ scale 倍に寄る
//   = 対象は画面の同じ所に残り (2Dの当たりの筋・数字がそのまま合う)、対象の深さで scale 倍に大きく、後ろの壁が画面に入る。1フレームで切り替え・戻しも切り替え
//   (StageDriver が描画用のカメラに当てる)。寄りの間は座席に追従する紙の UI を退避 (BattleView.MotionHideUi)・ピントの帯を対象へ (focus)・
//   額縁 (カメラに付く手前の草と枝) を寄りのカメラに付け直す (frames)。寄りの間に同じ敵へ次の当たりが来たら延ばす (姿勢は変えない)。
//   寄る当たりが別の敵に来たら、重さによらずその敵へ切り替える (寄りの軸は1体だけ。2Dの当たり・数字はレイアウトの座標で描くので、寄りのカメラで位置が合うのは軸の敵だけ)。
//   寄りの外の者に演出が出る時 (別の敵への寄らない当たり・敵の当たり・敵の行動・手番の区切り・次の札 = Presenter が Interrupt を呼ぶ)・
//   カメラの置き直し (画面の大きさの変更・旗 = StageCamera が LayoutChanged を呼ぶ) は寄りを戻す。
// 技の光 (light): 寄りの間の当たりは StageFx.MotionLight (技の光の印つき・強さ intensity・届く距離 range・dur 秒・hold は dur に対する割合) を対象の体の前に1つ、
//   壁の光 (wall) を1つ。次の当たりは同じ2つを灯し直す (多段で光の数が増えない)。寄らない当たりは今の光 (StageFx.PlayerHit) のまま。
//   壁の光の置き場 (直し 2026-10-03・反証「技の光が画面で対象の 326px 左に落ちる」): mode behind (既定) = 寄りのカメラから軸 (対象の体の点) へ延ばした線が
//   最初に当たる面 (名前が parts で始まる壁の部品の足跡・s を書けば道の座標 s のその面・steps なら段の立ち上がり) の pull 手前 = 画面で対象の真後ろ。
//   高さは当たった所 + rise・地面から minHeight 以上。線が maxDist までどこにも当たらない・軸 (対象の体の点) がもう面の中 (奥の席で s を書いた・
//   壁の部品の足跡の下に立っている) なら path の置き方 (記録の how に理由)。壁の部品の箱は組む時と同じ置き場と大きさ (Diorama.PartPlace = スマホの phone の t・s・y・scale)。
//   mode path = 今までの置き方 (足元から道の奥へ back か s)。
//   寄りの間だけ距離の霧と霧の板を薄める (zoom.fogScale = StageLook.SetFogScale。霧の end を伸ばし、2段目の強さと霧の板の α を下げる。寄りを戻すと正確に戻す。
//   別の敵への切り替えでは戻さずに掛けたまま = 空気の基を取り直さない)。
// 暗転 (dim): 寄る札 (既定は X 札・全体の最後の1発・とどめ = minKind boost) を出した瞬間 (Presenter の CardPlayed) に StageLook.SetDim を k へ in 秒で落とし、
//   寄りが終わって after 秒後に out 秒で 1 へ戻す
//   (寄りが wait 秒来なければ戻す・長くても max 秒)。キャラの固定のキーは SetDim が触らない (レーン B)。
//   SetDim は光 (月・舞台の灯・逆光・lights) に加えて空気も落とす (直し 2026-10-03・反証「暗転が見えない」): 霧の色・芯・高さの霧・2段目・霧の板の色は lerp(1, k, air)、
//   環境光 (Trilight と SH) は lerp(1, k, ambient)、暈と光の面 (StageShaft) は lerp(1, k, glow)。効きは StageLook.SetDimMix で渡す (落とす前に毎回)。
// 敵の大技 (enemyBig): 予告つき大技 (moves に名指し = 既定は溜め・予告つきの大技 17 個 DefaultBigMoves。auto を真にすると、その敵の攻撃の技でいちばん大きい技・
//   実値×発数が minTotal 以上・攻撃の技が2つ以上も) を
//   実行する瞬間に、赤い合図 (cue 秒・敵の足元の赤い点光源 StageFx.MotionCue と、敵の絵を cueTint で染める) → 暖のフラッシュ (warm 秒) → 白 (white 秒) → 技の色の帯 (band 秒)。
//   mode light (既定・直し 2026-10-03・反証「色の板をかぶせただけで舞台の形が消える」): 暖と白は舞台だけの光 = 技の光の印つきの大きな点光源 2 つ
//   (技の向かう先 = 自分の足元の上 flashHeight と、自分と敵の間 (画面の中心寄り) の上 centerHeight) と、舞台の後処理の露出の山
//   (StageLook.SetExposureBoost = ColorAdjustments.postExposure に暖 expWarm 段・白 expWhite 段 (白の間に 0 へ)。紙の UI は Overlay なので染まらない)。
//   暗部は暗いまま照らされた形が残る。板は技の色の帯 (band 秒・bandAlpha) だけ。mode plate = 今までの置き方 (暖・白も舞台のカメラの前の板の α)。
//   light の光は段の頭で灯し直す (暖の 1 つ目 = 合図の光・白 = 暖の 2 つ = 光は 2 つ (合図を入れて) まで)。灯し直される合図と暖は段の長さ + 0.1 秒灯す
//   (FlashReuseMargin。段の境目で先に消えて光の無い 1 フレームが出ない。plate の合図は今のまま)。
//   帯は舞台のカメラの前の板 (描画用のカメラの子・舞台のカメラが描く時だけ見える) = 紙の UI は染めない。
//   合図から帯の終わりまで (どちらの mode も) は Presenter の被弾の画面の赤い点滅 (Tween.ScreenFlash) の α を hurtFlashAlpha までにする (0 = 出さない)。
// 火花 (sparks): 大きい当たり・とどめ・X 札と全体の最後の1発で、寄っている対象の体の前から count 粒を放射 (画面に沿って広がる)・life 秒。
//   粒は速さで伸ばす (直し 2026-10-03・反証「火花が点のまま」: ParticleSystemRenderer の Stretch = 長さ size × lengthScale + 速さ × velocityScale。stretch false で丸い点)。
//
// 光の設計図の "motion" のキー (書かなければ下の既定。tier=phone の時は motion.phone の中のキーを上に重ねる):
//   on false
//   zoom:    { on true, big 0.45, boost 0.9, finish 0.9, bossFinish 1.8, bigMin 15, boostMin 15, scale 1.28, lookUp 6, yaw 0, pivot 0.45, minCamY 0.35,
//              chainHold 0.5, hideUi true, hideDesk true, frames "follow" | "keep", fogScale 0.6 }
//            big = 大きい当たり (与ダメ bigMin 以上か急所) を保つ秒・boost = X 札の最後の1発／全体の最後の1発 (札の与ダメの合計 boostMin 以上か大きい当たりを含む)・
//            finish = とどめ・bossFinish = 幕ボスのとどめ。scale = 対象の深さでの拡大・lookUp = 見上げ (度。足元が近すぎる時は minCamY まで弱める)・
//            yaw = 対象を軸に横へ回す (度。+ = カメラが左へ回り込み右を向く)・pivot = 軸の高さ (板の高さの割合)・minCamY = カメラの足元からの最低の高さ (unit)・
//            chainHold = 同じ札で同じ敵にまだ寄る当たりが来る時 (Shot.Chain)、寄りの時間が切れてから次の当たりを待つ上限の秒 (待つ間も寄ったまま)・
//            fogScale = 寄りの間の霧の量の倍率 (0.05〜1。1 = 触らない)
//   light:   { on true, intensity 12, range 10, dur 1.0, hold 0.35, height 0.45, towardCamera 0.6, shadow true, finishMul 1.25,
//              colors { physical [1,0.62,0.3], spell [0.35,1,0.85], light [1,0.74,0.4], spark [1,0.5,0.2], <形の名前> [r,g,b] },
//              wall { on true, mode "behind" | "path", intensity 8, range 9,
//                     behind の時: s (無し = 壁の面は部品から), parts ["wall"], steps "auto" | true | false, pull 1.0, rise 0.4, minHeight 0.5, maxLift 2.2, maxDist 40,
//                     path の時: back 4, s (無し), height 1.6 } }
//            intensity = hitLight.refDist の距離での明るさ (StageFx と同じ物差し)。hold = dur に対する割合 (0.35 × 1.0 秒 = 0.35 秒は最大のまま)
//            wall = 壁を照らす2つ目の光。behind: 寄りのカメラ→対象の体の点の線の先で最初に当たる面の pull (unit) 手前・高さ + rise・地面から minHeight 以上。
//            面 = 名前が parts のどれかで始まる block・arch・pillar の足跡の中 (上端より下。床に立つ壁の下の地面も壁に数える)・s を書けばその s の面 (どれか最初の物)。
//            steps: true = 段の立ち上がり (地面より下に入る所) も面に数える・false = 数えない・"auto" (既定) = 壁で探して、当たらないか
//            地面へ持ち上げる高さ (線の高さからの差) が maxLift を超えた時だけ (床が線より高い幕 = 幕3 の段) 段の立ち上がりも数えて探し直す。
//            path: 対象の足元から道の奥 (s の + 向き) へ back の所、足元から height の高さ。s を書くと back の代わりに道の座標 s のその位置
//            (座席の奥行きによらず壁のすぐ手前に置ける。壁の部品の中に入れると壁の表は照らさない = 壁の面より手前の s にする)
//   dim:     { on true, k 0.4, in 0.2, out 0.35, after 0.1, wait 1.5, max 4, minKind "boost", air 0.7, ambient 0.9, glow 0.9 }
//            (minKind = この重さ以上の寄りを含む札だけ暗くする。既定 boost = X 札・全体の最後の1発・とどめだけ
//            = 分析書 §8-3「ブーストの溜め」。本家は WEAK の 0.42 秒の寄りでは暗くしない。big にすると大きい当たり (15 以上・急所) の札も全部暗くする)。
//            air・ambient・glow = 空気の効き (0 = 落とさない・1 = 光と同じ k まで落とす): 霧の色・芯・高さの霧・2段目・霧の板 / 環境光 / 暈と光の面
//   sparks:  { on true, count 120, life 0.6, speed [5, 11], size [0.07, 0.15], gravity 0.6, drag 3, glow 3.0, height 0.5, towardCamera 0.8,
//              stretch true, velocityScale 0.06, lengthScale 1.0 }   (stretch = 速さで伸ばす筋・false = 丸い点 (今まで))
//   focus:   { on true, near 2, far 12, tiltTop 0 }   (道の s で対象の足元の near 手前〜far 奥をピントの帯に。深さの帯は寄りのカメラから同じ幅。tiltTop = 上端の追加のぼけ・負なら触らない)
//   enemyBig:{ on true, auto false, minTotal 20, moves [既定の名指し DefaultBigMoves], cue 0.17, warm 0.15, white 0.3, band 1.2, mode "light" | "plate",
//              cueColor [1,0.25,0.18], cueIntensity 6, cueRange 5, cueHeight 0.35, cueTint [1,0.62,0.55], warmColor [1,0.55,0.22], warmAlpha 0.45, whiteColor [1,1,1], whiteAlpha 0.5,
//              bandColor [r,g,b] (無ければ技の色: 飛び道具・光線はその色・近接は bandMelee [1,0.5,0.28]), bandAlpha 0.16,
//              light の時: flashWarm 8, flashWhite 14, flashRange 16, flashHeight 1.2, centerHeight 2.0, centerMul 0.8, flashToward 0.8, expWarm 0.5, expWhite 1.0,
//              hurtFlashAlpha 0.1 }
//            cueTint = 赤い合図の間だけ敵の絵 (板は Image の色を掛ける) に掛ける色 ([1,1,1] で染めない)。戻すのは合図の終わり (誰かが色を書き換えていたら戻さない)
//            moves = 「敵の id:技の id」か「技の id」の名指し (書けば既定の名指しを置き換える。[] で名指しなし)。既定は溜め・予告つきの大技だけ (DefaultBigMoves)。
//            auto = 名指しの外でも、その敵の攻撃の技でいちばん大きい技 (攻撃の技2つ以上・実値×発数 ≥ minTotal) を大技とみなす (既定 false。
//            true にすると狼の裂き・道化の大振りのような溜めの無い技まで鳴る = 2026-10-03 反証: 89 体中 26 体)
//            light: flashWarm・flashWhite = 暖・白の点光源の強さ (StageFx と同じ物差し・2 つ目は × centerMul・0 で 2 つ目なし)・flashRange = 届く距離・
//            flashHeight = 1 つ目 (技の向かう先 = 自分) の足元からの高さ・centerHeight = 2 つ目 (自分と敵の足元の真ん中) の高さ・flashToward = カメラの方へ寄せる unit・
//            expWarm・expWhite = 露出の足し (段。白は白の間に 0 へ下がる)。warmAlpha・whiteAlpha は plate の時だけ。
//            hurtFlashAlpha = 合図から帯の終わりまでの被弾の赤い点滅の α の上限 (0 = 出さない・どちらの mode も)
//   phone:   { 上と同じ形 (スマホの段だけ重なる) }
// 記録: dumplayout の extra.motion (DebugInfo。門が真の時だけ登録)・ログ "[StageMotion]" (det・dumplayout の時だけ。frame と time つき)。
using System;
using System.Collections.Generic;
using System.Globalization;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DeckRogue.Game
{
    public static class StageMotion
    {
        /// <summary>寄りの種類と保つ秒 (Presenter.PlanMotion が当たりごとに作る)。Kind = big・boost・finish・bossFinish</summary>
        public sealed class Shot
        {
            public string Kind;
            public float Dur;
            /// <summary>同じ札で同じ敵に、この後も寄る当たりが来る (寄りの時間が切れても zoom.chainHold 秒までは次の当たりを待つ = 寄り直しの1フレームの切り返しを出さない)</summary>
            public bool Chain;
        }

        // ================================================================ 門

        /// <summary>動きを出してよい (有効な箱庭で光が当たっていて、光の設計図の motion.on が true)。偽なら下の口は全部なにもしない</summary>
        public static bool On
        {
            get
            {
                bool on = StageFx.Live && Tune.On;
                if (on != _registered)
                {
                    if (on) HD2DFlags.LayoutDumpers["motion"] = DebugInfo;   // dumplayout の extra.motion (門が真の時だけ)
                    else HD2DFlags.LayoutDumpers.Remove("motion");
                    _registered = on;
                }
                return on;
            }
        }
        static bool _registered;

        /// <summary>寄りの種類から寄りを作る (保つ秒は設計図の zoom.<種類>)。寄りを切っている・秒が 0 なら null</summary>
        public static Shot ShotFor(string kind)
        {
            var z = Tune.Zoom;
            if (!z.On || string.IsNullOrEmpty(kind)) return null;
            float dur;
            switch (kind)
            {
                case "big": dur = z.Big; break;
                case "boost": dur = z.Boost; break;
                case "finish": dur = z.Finish; break;
                case "bossFinish": dur = z.BossFinish; break;
                default: return null;
            }
            return dur > 0f ? new Shot { Kind = kind, Dur = dur } : null;
        }

        /// <summary>大きい当たりのしきい (与ダメ。既定 15 = 2Dの大きい当たりと同じ)</summary>
        public static int BigMin => Mathf.Max(1, Mathf.RoundToInt(Tune.Zoom.BigMin));
        /// <summary>X 札・全体攻撃の最後の1発で寄る、札の与ダメの合計のしきい (大きい当たりを含む札はしきいによらず寄る。既定 15 = X=1 の打ち据え級では寄らない)</summary>
        public static int BoostMin => Mathf.Max(0, Mathf.RoundToInt(Tune.Zoom.BoostMin));

        /// <summary>寄りの種類 kind の当たりを含む札で舞台を暗くするか (設計図の dim.minKind 以上の重さ。既定 boost = X 札・全体の最後の1発・とどめの札だけ)</summary>
        public static bool DimFor(string kind)
        {
            var dm = Tune.Dim;
            return dm.On && Rank(kind) > 0 && Rank(kind) >= Rank(dm.MinKind);
        }

        /// <summary>いま敵 enemyIndex を寄っている (門が偽なら false)</summary>
        public static bool CloseUpOn(int enemyIndex) => _cu && _cuEnemy == enemyIndex && On;

        /// <summary>いま寄っている (誰かを)。門を読まない (寄りが走っていなければ false = 門が偽なら一度も true にならない)</summary>
        public static bool CloseUpActive => _cu;

        /// <summary>いま敵 enemyIndex 以外を寄っている (門が偽なら false)</summary>
        public static bool CloseUpOnOther(int enemyIndex) => _cu && _cuEnemy != enemyIndex && On;

        /// <summary>
        /// 寄りを戻す (Presenter: 寄りの外の者に演出が出る時。2Dの演出はレイアウトの座標で描く = 寄りのカメラでは軸の敵の絵しか位置が合わない)。
        /// 暗転は自分の時計で戻る (寄りが終わって after 秒後)。寄っていなければ何もしない
        /// </summary>
        public static void Interrupt(string why)
        {
            if (_cu) EndCloseUp(why);
        }

        /// <summary>レイアウト用のカメラを置き直した (StageCamera の LayoutCamera)。寄りの姿勢は前のカメラから作った物なので戻す。寄っていなければ何もしない</summary>
        internal static void LayoutChanged()
        {
            if (_cu) EndCloseUp("カメラの置き直し");
        }

        // ================================================================ 演出からの口 (Presenter)

        /// <summary>
        /// 自分の当たり (Presenter の2Dの当たりと同じ所。StageFx.PlayerHit・Finish の代わりに呼ぶ)。shot = 寄るなら寄りの種類 (null = 寄らない。寄りの間の次の当たり)。
        /// 寄れた (または寄っている敵への当たり) なら寄りの技の光と火花、寄れなければ今の光 (StageFx) に任せる
        /// </summary>
        public static void PlayerHit(int enemyIndex, string style, Shot shot, bool big, bool finishing, Color? fxColor = null)
        {
            string key = "enemy" + enemyIndex;
            if (!On) { Fallback(key, style, big, finishing); return; }
            var t = Tune;
            // カメラを動かさない時は全部の当たりで技の光を出す (寄らない当たりは光の長さぶんの「見えない寄り」)
            if (shot == null && t.Zoom.On && !t.Zoom.Move) shot = new Shot { Kind = "hit", Dur = Mathf.Max(0.2f, t.Light.Dur * 0.6f) };
            if (shot != null && shot.Dur > 0f && t.Zoom.On)
            {
                if (!_cu) StartCloseUp(enemyIndex, shot);
                else if (_cuEnemy != enemyIndex)
                {   // 寄る当たりが別の敵に来た: 重さによらずその敵へ切り替える (軸の敵しか画面の位置が保たれない = 延ばすと別の敵の絵と2Dの当たりがずれる。2026-10-03 反証)。
                    // 紙の UI は隠したまま (同じフレームで戻して隠し直すと、捨てた CanvasGroup が描く前に消えて1フレーム見える)。
                    // 寄りの霧の倍率もそのまま (戻してすぐ掛け直すと、空気の基の取り直しが切り替えのフレームに入る。直し 2026-10-03 反証)
                    EndCloseUp("切り替え", true);
                    if (!StartCloseUp(enemyIndex, shot)) { HideUi(false); RestoreFog(); }
                }
                else Extend(shot);
            }
            if (!_cu || _cuEnemy != enemyIndex) { Fallback(key, style, big, finishing); return; }   // 寄れなかった (板も座席も無い)・別の敵を寄っている = 今の光
            _fxColor = fxColor;   // 2026-10-03 ユーザー「攻撃エフェクトの主色に光の色を合わせて」
            Lights(key, style, finishing);
            bool heavy = big || finishing || (shot != null && Rank(shot.Kind) >= 2);   // 大きい当たり・とどめ・X 札と全体の最後の1発 (boost 以上)
            if (heavy && t.Sparks.On) Burst(key, fxColor ?? t.Light.ColorFor(style));
        }

        /// <summary>寄る札を出した瞬間 (Presenter の CardPlayed): 舞台を暗くし始める。寄りが終わって after 秒後に戻す</summary>
        public static void BeginDim()
        {
            if (!On) return;
            var dm = Tune.Dim;
            if (!dm.On) return;
            _dimPhase = 1; _dimT = 0f; _dimSawCu = false; _dimAfter = 0f;
            _dims++;
            Log("暗転 k " + F(dm.K) + " (" + F(dm.In) + "秒で落とす)");
            Remember("dim", null, 0f);
        }

        /// <summary>
        /// 敵の大技 (予告つき大技) を実行する瞬間 (Presenter の EnemyActionExecuting): 赤い合図 → 暖のフラッシュ → 白 → 技の色の帯。band = 技の色 (null = 設計図か既定)
        /// </summary>
        public static void EnemyBig(int enemyIndex, Color? band)
        {
            if (!On) return;
            var eb = Tune.EnemyBig;
            if (!eb.On) return;
            string key = "enemy" + enemyIndex;
            Vector3 feet; float h;
            _cueSeq = 0;
            if (eb.Cue > 0f && Stage.TryGetUnitBox(key, out feet, out h))
            {
                var p = feet + Vector3.up * (h * eb.CueHeight);
                var cam = Stage.Camera;
                if (cam != null) { var to = cam.transform.position - p; if (to.sqrMagnitude > 1e-6f) p += to.normalized * 0.8f; }
                // light の時は合図の光を暖の段が灯し直す = 段の境目まで消えないよう少し長く灯す (FlashReuseMargin。plate は今のまま)
                _cueSeq = StageFx.MotionCue(p, eb.CueColor, eb.CueIntensity, eb.CueRange, eb.Cue + (eb.LightMode ? FlashReuseMargin : 0f), key);
            }
            if (eb.Cue > 0f) CueTint(enemyIndex, eb.CueTint);
            _plateT = 0f; _plateStartFrame = Time.frameCount;
            _plateBand = band ?? eb.BandColor ?? eb.BandMelee;
            _plateKey = key; _flashPhase = 0; _flashSeqA = 0; _flashSeqB = 0;
            if (eb.LightMode) StageLook.SetExposureBoost(0f);   // 前の大技の露出の山が残っていれば戻す (合図の間は 0)
            _enemyBigs++;
            EnsurePlate();
            Log("敵の大技 " + key + " (" + (eb.LightMode ? "光" : "板") + ") 赤 " + F(eb.Cue) + "→暖 " + F(eb.Warm) + "→白 " + F(eb.White) + "→帯 " + F(eb.Band) + "秒");
            Remember("enemyBig", key, eb.Cue + eb.Warm + eb.White + eb.Band);
        }

        /// <summary>
        /// 予告つき大技か: 設計図の enemyBig.moves (既定 DefaultBigMoves) に「敵の id:技の id」か「技の id」で名指し、
        /// または auto が真の時 (既定 false) その敵の攻撃の技でいちばん大きい技 (最大×発数。HPで痛む一撃はいちばん大きい) で、
        /// 攻撃の技が2つ以上あり、今の実値×発数 total が minTotal 以上。門が偽なら false
        /// </summary>
        public static bool IsEnemyBig(EnemyDef def, string moveId, int total)
        {
            if (!On) return false;
            var eb = Tune.EnemyBig;
            if (!eb.On || string.IsNullOrEmpty(moveId)) return false;
            if (eb.Moves != null && (eb.Moves.Contains(moveId) || (def != null && eb.Moves.Contains(def.Id + ":" + moveId)))) return true;
            if (!eb.Auto || def == null || def.Moves == null) return false;
            if (total < eb.MinTotal) return false;
            int attacks = 0; float best = -1f, mine = -1f;
            foreach (var m in def.Moves)
            {
                if (m == null || m.Kind != "attack") continue;
                attacks++;
                float v = m.DamageFromPlayerHp != null ? 1e6f : (m.Max ?? m.Min ?? 0) * Mathf.Max(1, m.Hits ?? 1);
                if (v > best) best = v;
                if (m.Id == moveId) mine = v;
            }
            return attacks >= 2 && mine >= 0f && mine >= best;
        }

        /// <summary>敵の大技の合図から帯の終わりまで (門が偽なら false = Presenter の被弾の点滅は今のまま)</summary>
        /// <summary>カメラを動かさない時 (zoom.move false) は全部の当たりで技の光を出す (Presenter が寄らない当たりもここへ回す)</summary>
        public static bool LightsEveryHit => On && Tune.Zoom.On && !Tune.Zoom.Move;

        public static bool EnemyBigFlashing => _plateT >= 0f && On;

        /// <summary>敵の大技の間の被弾の画面の赤い点滅の α の上限 (光の設計図の enemyBig.hurtFlashAlpha・既定 0.1。0 = 出さない)</summary>
        public static float EnemyBigHurtFlashAlpha => Mathf.Clamp01(Tune.EnemyBig.HurtFlash);

        // ================================================================ カメラ (StageDriver)

        /// <summary>寄りの姿勢 (StageDriver が描画用のカメラに当てる。揺れは StageDriver が足す)。寄っていなければ false</summary>
        internal static bool TryCloseUpPose(out Vector3 pos, out Quaternion rot)
        {
            pos = _cuPos; rot = _cuRot;
            return _cu && _cuMove;
        }

        /// <summary>毎フレームの時計 (StageDriver の LateUpdate の頭)。何も走っていなければ何もしない (門が偽なら一度も走らない)</summary>
        internal static void Tick(float dt)
        {
            if (!_cu && _dimPhase == 0 && _plateT < 0f) return;
            if (!On) { StopAll("門が閉じた"); return; }
            // 戦闘の画面が無くなった (決着の余韻を飛ばして報酬の画面へ組み直した・タイトルへ戻った): 寄りの姿勢のまま次の画面を写さない
            if (GameRoot.I == null || GameRoot.I.Battle == null) { StopAll("戦闘の画面が無い"); return; }
            if (_cu)
            {
                if (Time.frameCount != _cuStartFrame) _cuLeft -= dt;   // 始めたフレームは数えない (始めたフレームから dur 秒ちょうど寄る)
                if (_cuLeft <= 0f)
                {
                    // 同じ札の次の当たりを待つ (多段の途中に急所が乗った・X 札の途中が大きい当たりだった。待てるのは chainHold 秒まで = 次が来なければ戻す)
                    if (_cuChain && _cuChainT < Tune.Zoom.ChainHold) { if (Time.frameCount != _cuStartFrame) _cuChainT += dt; }
                    else EndCloseUp(_cuChain ? "次の当たりが来ない" : "時間");
                }
                if (_cu && _cuMove && Tune.Zoom.HideUi) HideUi(true);   // 寄りの間 (次の当たりを待つ間も) に組み直しが来ても隠し直す
                if (_cu) KeepFogScale();                     // 寄りの間の霧の倍率 (光の組み直しで 1 に戻っていたら掛け直す)
            }
            TickDim(dt);
            TickPlate(dt);
        }

        // ================================================================ 寄り

        static bool _cu;
        static int _cuEnemy = -1, _cuStartFrame;
        static string _cuKey, _cuKind;
        static float _cuLeft, _cuTotal, _cuChainT;
        static bool _cuChain;
        static Vector3 _cuPos, _cuPivot, _cuFeet;
        static bool _cuMove = true;
        static Quaternion _cuRot;
        static bool _cuFramesMoved;
        static BattleView _hidView;
        static long _lightSeq, _wallSeq;
        static bool _cuWallSet;          // この寄りの壁の光の置き場を解いた (寄りごとに1回。多段の灯し直しも同じ所)
        static Vector3 _cuWall;
        static string _cuWallHow;        // 置き場の出どころ (記録: "behind:ground"・"behind:part:wall-main-2"・"behind:s"・"path"・"path (behind: 当たらない)"・"path (behind: 軸が面の中 (s))")
        static float _cuFog = 1f;        // この寄りの霧の倍率 (zoom.fogScale)

        static int Rank(string kind)
        {
            switch (kind) { case "bossFinish": return 4; case "finish": return 3; case "boost": return 2; case "big": return 1; default: return 0; }
        }

        static bool StartCloseUp(int enemyIndex, Shot shot)
        {
            var t = Tune; var z = t.Zoom;
            string key = "enemy" + enemyIndex;
            Vector3 pivot, feet;
            if (!Pivot(key, z.Pivot, out pivot, out feet)) { Log("寄れない (板も座席も無い) " + key); return false; }
            Vector3 pos; Quaternion rot; float look = 0f;
            if (!z.Move)
            {   // カメラは今のまま (引きの画)。壁の光の置き場と火花の向きは今のカメラから測る
                var cam = Stage.Camera;
                if (cam == null) { Log("寄れない (カメラが無い) " + key); return false; }
                pos = cam.transform.position; rot = cam.transform.rotation;
            }
            else if (!Pose(pivot, feet, z, out pos, out rot, out look)) { Log("寄れない (姿勢が作れない) " + key); return false; }
            _cuMove = z.Move;
            _cu = true; _cuEnemy = enemyIndex; _cuKey = key; _cuKind = shot.Kind; _cuLeft = shot.Dur; _cuTotal = shot.Dur; _cuStartFrame = Time.frameCount;
            _cuChain = shot.Chain; _cuChainT = 0f;
            _cuPos = pos; _cuRot = rot; _cuPivot = pivot; _cuFeet = feet;
            _lightSeq = 0; _wallSeq = 0;
            _cuWallSet = false; _cuWallHow = null;
            _cuFog = z.Move ? z.FogScale : 1f;
            KeepFogScale();   // 寄りの間だけ距離の霧と霧の板を薄める (fogScale 1 なら触らない)
            if (z.Move && z.HideUi) HideUi(true);
            if (z.Move && t.Focus.On) FocusOn(pivot, feet, pos, rot, t.Focus);
            if (z.Move && z.Frames) { Diorama.OnCameraLayout(pos, rot, Stage.CurrentFov); _cuFramesMoved = true; }
            if (_dimPhase == 1) { _dimSawCu = true; _dimAfter = 0f; }   // 戻した寄りの後にまた寄った (Interrupt の後の寄り直し): 戻すまでの after を数え直す
            _closeUps++;
            Log("寄り " + shot.Kind + " " + key + " " + F(shot.Dur) + "秒 倍率 " + F(z.Scale) + " 見上げ " + F(look) + "° カメラ " + V(pos));
            Remember("closeUp:" + shot.Kind, key, shot.Dur);
            return true;
        }

        /// <summary>寄っている敵への次の寄る当たり: 残りを延ばす (姿勢は変えない)。別の敵への寄る当たりは切り替え (PlayerHit) = ここには来ない</summary>
        static void Extend(Shot shot)
        {
            if (!_cu) return;
            if (shot.Dur > _cuLeft) { _cuTotal += shot.Dur - _cuLeft; _cuLeft = shot.Dur; }
            if (Rank(shot.Kind) > Rank(_cuKind)) _cuKind = shot.Kind;
            _cuChain = shot.Chain; _cuTotal += _cuChainT; _cuChainT = 0f;   // 待っていた次の当たりが来た (この当たりの後も来るかは、この当たりの印)
            _extended++;
            Log("寄りを延ばした " + shot.Kind + " " + _cuKey + " 残り " + F(_cuLeft) + "秒");
        }

        /// <summary>寄りを戻す。switching = 別の敵へ切り替える途中 (紙の UI と寄りの霧の倍率はそのまま。続く StartCloseUp が失敗したら呼び手が HideUi(false)・RestoreFog)</summary>
        static void EndCloseUp(string why, bool switching = false)
        {
            if (!_cu) return;
            _cu = false;
            if (!switching) { HideUi(false); RestoreFog(); }   // 寄りの霧の倍率を正確に戻す (暗転が残っていれば空気は暗転の分だけ落ちたまま)
            else _cuFog = 1f;                                   // 書いた倍率は残す (次の寄りが同じ値なら KeepFogScale は書かない)
            FocusOff();
            if (_cuFramesMoved) { _cuFramesMoved = false; Diorama.OnCameraLayout(Stage.MotionLayoutPos, Stage.LayoutRotation, Stage.CurrentFov); }
            Log("寄りを戻した (" + why + ") " + _cuKey);
            Remember("closeUpEnd:" + why, _cuKey, 0f);
            _cuEnemy = -1; _cuKey = null; _cuKind = null; _cuLeft = 0f; _cuChain = false; _cuChainT = 0f;
            _cuWallSet = false; _cuWallHow = null;
        }

        /// <summary>寄りの霧の倍率を 1 へ戻す (StageLook が基へ正確に戻す。書いていなければ何もしない)</summary>
        static void RestoreFog()
        {
            _cuFog = 1f;
            if (StageLook.FogScale < 1f) StageLook.SetFogScale(1f);
        }

        /// <summary>寄りの間の霧の倍率を StageLook に当てる (光の組み直しで 1 に戻っていたら掛け直す。fogScale 1 以上なら触らない)</summary>
        static void KeepFogScale()
        {
            if (!(_cuFog < 1f)) return;
            float want = Mathf.Clamp(_cuFog, 0.05f, 1f);
            if (Mathf.Abs(StageLook.FogScale - want) > 1e-4f) StageLook.SetFogScale(want);
        }

        /// <summary>寄りの軸 (対象の体の点 = 板の高さの frac) と足元。板が無ければ座席から (高さ 2.4 unit の板とみなす)</summary>
        static bool Pivot(string key, float frac, out Vector3 pivot, out Vector3 feet)
        {
            pivot = feet = default;
            float h;
            if (Stage.TryGetUnitBox(key, out feet, out h))
            {
                var p = StageFx.UnitPoint(key, new Vector2(0.5f, frac));
                pivot = p.HasValue ? p.Value : feet + Vector3.up * (frac * h);
                return true;
            }
            float k;
            if (Stage.TryGetSeat(key, out feet, out k)) { pivot = feet + Vector3.up * (frac * 2.4f); return true; }
            return false;
        }

        /// <summary>
        /// 寄りの姿勢: レイアウト用のカメラ (MotionLayoutPos・LayoutRotation) を軸 pivot の回りに横 yaw・下へ lookUp 度回し (対象の画面の位置は変わらない)、
        /// 軸へ向かって距離を 1/scale に縮める (対象の深さで scale 倍)。カメラが足元から minCamY より下になるなら見上げを弱める (4段)
        /// </summary>
        static bool Pose(Vector3 pivot, Vector3 feet, ZoomTune z, out Vector3 pos, out Quaternion rot, out float look)
        {
            pos = default; rot = Quaternion.identity; look = 0f;
            if (!Stage.Camera) return false;
            Vector3 c0 = Stage.MotionLayoutPos;
            Quaternion r0 = Stage.LayoutRotation;
            float s = Mathf.Clamp(z.Scale, 1f, 3f);
            Vector3 right = r0 * Vector3.right;
            for (int i = 0; i <= 4; i++)
            {
                look = z.LookUp * (1f - i / 4f);
                var q = Quaternion.AngleAxis(z.Yaw, Vector3.up) * Quaternion.AngleAxis(-look, right);
                var p = pivot + (q * (c0 - pivot)) / s;
                if (i < 4 && p.y < feet.y + z.MinCamY) continue;
                var r = q * r0;
                if (!(Vector3.Dot(pivot - p, r * Vector3.forward) > 0.5f)) return false;
                pos = p; rot = r;
                return true;
            }
            return false;
        }

        /// <summary>紙の UI を隠す (寄りの間・毎フレーム) / 戻す</summary>
        static void HideUi(bool hide)
        {
            if (!hide)
            {
                if (_hidView != null) _hidView.MotionHideUi(false, false);
                _hidView = null;
                return;
            }
            var g = GameRoot.I;
            var v = g != null ? g.Battle : null;
            if (v == null) return;
            if (_hidView != null && !ReferenceEquals(_hidView, v)) _hidView.MotionHideUi(false, false);
            _hidView = v;
            v.MotionHideUi(true, Tune.Zoom.HideDesk);
        }

        // ---------------------------------------------------------------- ピント (TiltShiftSettings・TiltShiftPass の静的な値を寄りの間だけ書き換えて戻す)

        static bool _focusSet;
        static float _svPathNear, _svPathFar, _svBandNear, _svBandFar, _svTiltTop;    // 書き換える前の値
        static float _stPathNear, _stPathFar, _stBandNear, _stBandFar, _stTiltTop;    // 書いた値 (戻す時、誰も書き換えていない値だけ戻す)

        static void FocusOn(Vector3 pivot, Vector3 feet, Vector3 camPos, Quaternion camRot, FocusTune f)
        {
            if (_focusSet) FocusOff();
            _svPathNear = TiltShiftSettings.PathNear; _svPathFar = TiltShiftSettings.PathFar;
            _svBandNear = TiltShiftSettings.BandNear; _svBandFar = TiltShiftSettings.BandFar;
            _svTiltTop = TiltShiftPass.TiltTop;
            float sT;
            if (PathS(feet, out sT)) { TiltShiftSettings.PathNear = sT - Mathf.Max(0f, f.Near); TiltShiftSettings.PathFar = sT + Mathf.Max(0.01f, f.Far); }
            float d = Vector3.Dot(pivot - camPos, camRot * Vector3.forward);
            if (d > 0.1f && TiltShiftSettings.BandFar > 0f)   // 帯が未設定 (0) の時は触らない (触ると帯の外が全部ぼける)
            {
                TiltShiftSettings.BandNear = Mathf.Max(0.01f, d - Mathf.Max(0f, f.Near));
                TiltShiftSettings.BandFar = d + Mathf.Max(0.01f, f.Far);
            }
            if (f.TiltTop >= 0f) TiltShiftPass.TiltTop = f.TiltTop;
            _stPathNear = TiltShiftSettings.PathNear; _stPathFar = TiltShiftSettings.PathFar;
            _stBandNear = TiltShiftSettings.BandNear; _stBandFar = TiltShiftSettings.BandFar;
            _stTiltTop = TiltShiftPass.TiltTop;
            _focusSet = true;
        }

        static void FocusOff()
        {
            if (!_focusSet) return;
            _focusSet = false;
            if (TiltShiftSettings.PathNear == _stPathNear) TiltShiftSettings.PathNear = _svPathNear;
            if (TiltShiftSettings.PathFar == _stPathFar) TiltShiftSettings.PathFar = _svPathFar;
            if (TiltShiftSettings.BandNear == _stBandNear) TiltShiftSettings.BandNear = _svBandNear;
            if (TiltShiftSettings.BandFar == _stBandFar) TiltShiftSettings.BandFar = _svBandFar;
            if (TiltShiftPass.TiltTop == _stTiltTop) TiltShiftPass.TiltTop = _svTiltTop;
        }

        /// <summary>世界の点の道の座標 s (TiltShiftPass の道に沿った帯と同じ式: 箱庭の道の s 軸・根の原点から)。箱庭が無ければ false</summary>
        static bool PathS(Vector3 world, out float s)
        {
            s = 0f;
            Vector3 n, origin;
            if (!PathAxis(out n, out origin)) return false;
            s = Vector3.Dot(n, world - origin);
            return true;
        }

        /// <summary>道の s 軸 (道と直角・奥が +) の世界の向き (水平・長さ1) と原点</summary>
        static bool PathAxis(out Vector3 n, out Vector3 origin)
        {
            n = Vector3.forward; origin = Vector3.zero;
            if (!Diorama.Active) return false;
            n = Diorama.OnPath(0f, 1f, 0f) - Diorama.OnPath(0f, 0f, 0f);
            var root = Stage.WorldRoot;
            if (root != null) { n = root.TransformDirection(n); origin = root.position; }
            if (!(n.sqrMagnitude > 1e-8f)) return false;
            n.Normalize();
            return true;
        }

        // ---------------------------------------------------------------- 技の光

        static Color? _fxColor;
        /// <summary>2Dの当たりの主色を光の色にする (不透明に・明るさを 1 にそろえる = 光の強さは intensity で決める)</summary>
        static Color FxLightColor(Color c)
        {
            float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            return m > 1e-3f ? new Color(c.r / m, c.g / m, c.b / m, 1f) : Color.white;
        }

        static void Lights(string key, string style, bool finishing)
        {
            var L = Tune.Light;
            if (!L.On) return;
            Color c = _fxColor.HasValue ? FxLightColor(_fxColor.Value) : L.ColorFor(style);
            float mul = finishing ? L.FinishMul : 1f;
            var body = StageFx.UnitPoint(key, new Vector2(0.5f, L.Height));
            if (!body.HasValue) return;
            var p = body.Value;
            if (L.TowardCamera > 0f) { var to = _cuPos - p; if (to.sqrMagnitude > 1e-6f) p += to.normalized * L.TowardCamera; }
            _lightSeq = StageFx.MotionLight(p, c, L.Intensity * mul, L.Range, L.Dur, L.Hold, L.Shadow, "motion:" + (style ?? "slash"), key, _lightSeq);
            var w = L.Wall;
            if (w.On)
            {
                if (!_cuWallSet)
                {
                    // 置き場は寄りごとに1回 (多段の次の当たりも同じ所を灯し直す)
                    _cuWallSet = true;
                    string how = null;
                    if (w.Behind && WallBehind(w, out _cuWall, out how)) _cuWallHow = "behind:" + how;
                    else { _cuWall = WallOnPath(w); _cuWallHow = w.Behind ? "path (behind: " + (how ?? "当たらない") + ")" : "path"; }
                    Log("壁の光 " + _cuWallHow + " " + V(_cuWall) + " " + _cuKey);
                    Remember("wall:" + _cuWallHow, _cuKey, 0f);
                }
                _wallSeq = StageFx.MotionLight(_cuWall, c, w.Intensity * mul, w.Range, L.Dur, L.Hold, false, "motion-wall:" + (style ?? "slash"), key, _wallSeq);
            }
        }

        /// <summary>壁の光の今までの置き場 (mode path): 対象の足元から道の奥 (s の + 向き) へ back (s を書けばその s)、足元から height</summary>
        static Vector3 WallOnPath(WallTune w)
        {
            Vector3 n, origin;
            if (!PathAxis(out n, out origin)) n = Vector3.ProjectOnPlane(Stage.LayoutRotation * Vector3.forward, Vector3.up).normalized;
            float back = w.Back, sFeet;
            if (w.S.HasValue && PathS(_cuFeet, out sFeet)) back = w.S.Value - sFeet;   // 道の座標 s の位置 (足元の s からの差)
            return _cuFeet + n * back + Vector3.up * w.Height;
        }

        /// <summary>
        /// 壁の光の置き場 (mode behind): 寄りのカメラ (_cuPos) から軸 (_cuPivot = 対象の体の点) へ延ばした線を、軸から 0.5 unit 先から 0.2 unit ずつ maxDist まで進め、
        /// 最初に「面の中」に入った所を二分で詰め、そこから pull 手前 (軸からの距離の半分より手前には来ない) に置く = 画面で対象の真後ろ。
        /// 面 = 名前が parts で始まる壁の部品の足跡の中 (高さは上端より下 = 床に立つ壁の下の地面の中も壁に数える)・s を書いていればその s より奥・
        /// steps の時は段の立ち上がり (地面より下に入る所) も。高さは線の高さ + rise、その所の地面から minHeight 以上。
        /// steps "auto" (既定): まず壁と s だけで探し、当たらないか、地面へ持ち上げる高さが maxLift を超えたら (床が線より高い幕 = 幕3 の段) 段の立ち上がりも数えて探し直す。
        /// 箱庭が無い・線が奥へ進まない・どこにも当たらなければ false (呼び手は path の置き方)。how = 当たった物 (記録)
        /// </summary>
        static bool WallBehind(WallTune w, out Vector3 wp, out string how)
        {
            wp = default; how = null;
            if (!Diorama.Active || !Stage.Camera) { how = "箱庭が無い"; return false; }
            Vector3 dir = _cuPivot - _cuPos;
            float len = dir.magnitude;
            if (!(len > 1e-3f)) { how = "線が作れない"; return false; }
            dir /= len;
            Vector3 n, origin;
            if (!PathAxis(out n, out origin) || Vector3.Dot(n, dir) < 0.05f) { how = "線が奥へ進まない"; return false; }
            var root = Diorama.Root != null ? Diorama.Root : Stage.WorldRoot;
            var boxes = WallBoxes(w);
            Vector3 p1, p2; string h1, h2; float lift1, lift2;
            bool hit1 = March(w, dir, root, boxes, w.Steps == WallSteps.On, out p1, out h1, out lift1);
            if (w.Steps != WallSteps.Auto || (hit1 && lift1 <= w.MaxLift))
            {
                if (!hit1) { how = h1; return false; }   // h1 = 外れた理由 (軸が面の中) か null (どこにも当たらない)
                wp = p1; how = h1 + " lift " + F(lift1);
                return true;
            }
            bool hit2 = March(w, dir, root, boxes, true, out p2, out h2, out lift2);
            if (hit2) { wp = p2; how = h2 + " lift " + F(lift2) + (hit1 ? " (壁は持ち上げ " + F(lift1) + " > maxLift)" : " (壁に当たらない)"); return true; }
            if (hit1) { wp = p1; how = h1 + " lift " + F(lift1); return true; }
            how = h1 ?? h2;
            return false;
        }

        /// <summary>
        /// WallBehind の1回の探し (steps = 段の立ち上がりも面に数える)。当たれば置き場 (pull 手前・持ち上げ済み)・当たった物・持ち上げた高さ (線からの差)。
        /// 軸 (対象の体の点) がもう面の中なら探さずに false・how に理由 (奥の席で s を書いた・壁の部品の足跡の下に立っている = 線の先は全部「中」か、
        /// 外へ出た先は別の面の裏。直し 2026-10-03 反証: 1回目の点がもう中だと二分の幅が 0 になり、光が体の 0.25 後ろ = 体の中に置かれていた)。
        /// 軸は外で1回目の点 (start) が中なら、軸〜start を二分で詰める (面が体のすぐ後ろ = 光は体と面の間)
        /// </summary>
        static bool March(WallTune w, Vector3 dir, Transform root, List<WallBox> boxes, bool steps, out Vector3 wp, out string how, out float lift)
        {
            wp = default; how = null; lift = 0f;
            const float start = 0.5f, step = 0.2f;
            float maxD = Mathf.Clamp(w.MaxDist, 1f, 200f);
            string atAxis = SolidAt(root, _cuPivot, w, boxes, steps);
            if (atAxis != null) { how = "軸が面の中 (" + atAxis + ")"; return false; }
            float prev = 0f;   // いちばん近い外の点 (軸は外 = 0)
            for (float d = start; d <= maxD; d += step)
            {
                string hit = SolidAt(root, _cuPivot + dir * d, w, boxes, steps);
                if (hit == null) { prev = d; continue; }
                // 二分で面の所を詰める (prev は外・d は中。1回目の点で当たれば 0〜start)
                float lo = prev, hi = d;
                for (int i = 0; i < 6; i++)
                {
                    float mid = 0.5f * (lo + hi);
                    string hm = SolidAt(root, _cuPivot + dir * mid, w, boxes, steps);
                    if (hm != null) { hi = mid; hit = hm; } else lo = mid;
                }
                float dHit = hi;
                float dAt = Mathf.Max(0.5f * dHit, dHit - Mathf.Max(0f, w.Pull));
                var q = _cuPivot + dir * dAt;
                var ql = root != null ? root.InverseTransformPoint(q) : q;
                float t, s;
                Diorama.ToPath(ql.x, ql.z, out t, out s);
                float y = Mathf.Max(ql.y + w.Rise, Diorama.HeightAtPath(t, s) + w.MinHeight);
                lift = y - ql.y;
                ql.y = y;
                wp = root != null ? root.TransformPoint(ql) : ql;
                how = hit + " d " + F(dHit) + " s " + F(s);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 点 p (世界) が面の中か: steps なら地面より下 ("ground")・s を書いていればその s より奥 ("s")・壁の部品の足跡の中で上端より下 ("part:名前")。
        /// 壁の部品は下端を見ない (床に立つ壁の下は地面 = 線が床の下を通っても、その奥の壁の面で止める)。どれでもなければ null
        /// </summary>
        static string SolidAt(Transform root, Vector3 p, WallTune w, List<WallBox> boxes, bool steps)
        {
            var lp = root != null ? root.InverseTransformPoint(p) : p;
            float t, s;
            Diorama.ToPath(lp.x, lp.z, out t, out s);
            if (steps && lp.y < Diorama.HeightAtPath(t, s)) return "ground";
            if (w.S.HasValue && s >= w.S.Value) return "s";
            foreach (var b in boxes)
            {
                if (lp.y > b.Y1) continue;
                float dt = t - b.T, ds = s - b.S;
                float lx = dt * b.Cos - ds * b.Sin, lz = dt * b.Sin + ds * b.Cos;   // 部品の向き (道に沿う + yaw) の箱の中の座標
                if (Mathf.Abs(lx) <= b.HalfW && Mathf.Abs(lz) <= b.HalfD) return "part:" + b.Name;
            }
            return null;
        }

        /// <summary>壁の部品の箱 (道の座標の中心・半幅・向き・高さの範囲)。名前が parts のどれかで始まる block・arch・pillar (スマホで組まない物・旗が無いと組まない物は除く)</summary>
        sealed class WallBox { public string Name; public float T, S, HalfW, HalfD, Cos, Sin, Y0, Y1; }
        static readonly List<WallBox> _wallBoxes = new List<WallBox>();
        static DioramaLayout _wallBoxesOf;
        static string _wallBoxesSig;

        static List<WallBox> WallBoxes(WallTune w)
        {
            var L = Diorama.Layout;
            string sig = (UiKit.Phone ? "ph:" : "pc:") + string.Join(",", w.Parts ?? new string[0]);
            if (ReferenceEquals(L, _wallBoxesOf) && sig == _wallBoxesSig) return _wallBoxes;
            _wallBoxesOf = L; _wallBoxesSig = sig;
            _wallBoxes.Clear();
            if (L == null || w.Parts == null || w.Parts.Length == 0) return _wallBoxes;
            foreach (var p in L.Parts)
            {
                if (p == null || string.IsNullOrEmpty(p.Name) || (p.Kind != "block" && p.Kind != "arch" && p.Kind != "pillar")) continue;
                bool named = false;
                foreach (var pre in w.Parts) if (!string.IsNullOrEmpty(pre) && p.Name.StartsWith(pre, StringComparison.Ordinal)) { named = true; break; }
                if (!named || Diorama.PhoneHidden(p) || Diorama.R3I_FlagHidden(p)) continue;
                // 置き場と大きさは組む時と同じ規則 (Diorama.PartPlace = PlaceOf: スマホなら phone の t・s・y・scale。直し 2026-10-03 反証: scale を見ていなかった)。
                // 形は原点の回りに大きさ scale で写る (Diorama の partM = TRS(pos, rot, scale)) = 足跡・高さ・沈みの全部に掛ける。w・d・h・sink は組む時も phone を読まない
                float pt, ps, py, sc;
                Diorama.PartPlace(p, out pt, out ps, out py, out sc);
                bool arch = p.Kind == "arch", pillar = p.Kind == "pillar";
                float bw = p.Num("w", arch ? 4f : pillar ? 0.9f : 1.6f) * sc, bd = p.Num("d", arch ? 1f : pillar ? 0.9f : 1.2f) * sc, bh = p.Num("h", arch ? 4.5f : pillar ? 5f : 1f) * sc;
                float sink = p.Num("sink", arch || pillar ? 0.15f : 0.2f) * sc;
                float y0 = (p.Abs ? py : Diorama.HeightAtPath(pt, ps) + py) - sink;
                float a = (Diorama.IsPathAligned(p.Kind) ? p.Yaw : 0f) * Mathf.Deg2Rad;
                _wallBoxes.Add(new WallBox { Name = p.Name, T = pt, S = ps, HalfW = 0.5f * bw, HalfD = 0.5f * bd, Cos = Mathf.Cos(a), Sin = Mathf.Sin(a), Y0 = y0, Y1 = y0 + sink + bh });
            }
            return _wallBoxes;
        }

        /// <summary>寄れない時の光 (今の口と同じ = Presenter が門の外で呼ぶ物)</summary>
        static void Fallback(string key, string style, bool big, bool finishing)
        {
            if (finishing) StageFx.Finish(key); else StageFx.PlayerHit(key, style, big);
        }

        // ---------------------------------------------------------------- 火花

        static ParticleSystem _sparks;
        static int _bursts;

        static void Burst(string key, Color color)
        {
            var sp = Tune.Sparks;
            int n = Mathf.Clamp(sp.Count, 0, 300);
            if (n <= 0) return;
            var body = StageFx.UnitPoint(key, new Vector2(0.5f, sp.Height));
            if (!body.HasValue) return;
            var ps = Sparks();
            if (ps == null) return;
            var at = body.Value;
            var camPos = _cu ? _cuPos : (Stage.Camera != null ? Stage.Camera.transform.position : at);
            var camRot = _cu ? _cuRot : Stage.LayoutRotation;
            if (sp.TowardCamera > 0f) { var to = camPos - at; if (to.sqrMagnitude > 1e-6f) at += to.normalized * sp.TowardCamera; }
            SparkRender(ps, sp);
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(color.r * sp.Glow, color.g * sp.Glow, color.b * sp.Glow, 1f));
            main.gravityModifier = sp.Gravity;
            var lim = ps.limitVelocityOverLifetime;
            lim.drag = sp.Drag;
            Vector3 right = camRot * Vector3.right, up = camRot * Vector3.up, fwd = camRot * Vector3.forward;
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                // 向き: 画面に沿った円を黄金角で埋め、奥行きに少しばらす (乱数を使わない = 撮るたびに同じ)
                float a = i * 2.3999632f + _bursts * 0.917f;
                float f1 = Frac(i * 0.6180340f + _bursts * 0.3141f);
                float f2 = Frac(i * 0.7548777f + _bursts * 0.5698f);
                float f3 = Frac(i * 0.5698403f + _bursts * 0.1234f);
                var dir = (Mathf.Cos(a) * right + Mathf.Sin(a) * up + (f2 - 0.5f) * 0.8f * fwd).normalized;
                ep.position = at;
                ep.velocity = dir * Mathf.Lerp(sp.SpeedMin, sp.SpeedMax, f1);
                ep.startLifetime = sp.Life * (0.6f + 0.4f * f3);
                ep.startSize = Mathf.Lerp(sp.SizeMin, sp.SizeMax, f2);
                ps.Emit(ep, 1);
            }
            _bursts++;
            Log("火花 " + n + "粒" + (sp.Stretch ? " (筋)" : "") + " " + key);
        }

        static float Frac(float x) => x - Mathf.Floor(x);

        /// <summary>粒の描き方 (stretch = 速さで伸ばす筋: 長さ = 大きさ × lengthScale + 速さ × velocityScale・カメラの速さは足さない (寄りの切り替えで伸びない)。false = 丸い点)</summary>
        static void SparkRender(ParticleSystem ps, SparkTune sp)
        {
            var r = ps != null ? ps.GetComponent<ParticleSystemRenderer>() : null;
            if (r == null) return;
            if (sp.Stretch)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = sp.VelocityScale;
                r.lengthScale = sp.LengthScale;
                r.cameraVelocityScale = 0f;
            }
            else r.renderMode = ParticleSystemRenderMode.Billboard;
        }

        /// <summary>火花の粒の系 (無ければ作る)。粒の光の材質 (Stage.MotionGlowMaterial)。乱数の種は固定・自分では出さない (Emit だけ)</summary>
        static ParticleSystem Sparks()
        {
            if (_sparks != null) return _sparks;
            var mat = Stage.MotionGlowMaterial(false);
            if (mat == null) return null;
            var go = new GameObject("HD2D-MotionSparks");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.randomSeed = 20261003u;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = 6;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = false;
            main.startSpeed = 0f;
            main.startLifetime = 0.5f;
            main.maxParticles = 600;
            var em = ps.emission; em.rateOverTime = 0f;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.7f, 0.45f), 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.25f));
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 100f; lim.drag = 3f;
            ps.Play();
            _sparks = ps;
            return ps;
        }

        // ---------------------------------------------------------------- 暗転

        static int _dimPhase;          // 0 = していない・1 = 落として保つ・2 = 戻している
        static float _dimK = 1f, _dimT, _dimAfter;
        static bool _dimSawCu;

        static void TickDim(float dt)
        {
            if (_dimPhase == 0) return;
            var dm = Tune.Dim;
            float k = Mathf.Clamp01(dm.K);
            _dimT += dt;
            if (_dimPhase == 1)
            {
                _dimK = Mathf.MoveTowards(_dimK, k, dt * (1f - k) / Mathf.Max(0.01f, dm.In));
                if (_cu) _dimSawCu = true;
                bool release = _dimT >= dm.Max || (!_dimSawCu && _dimT >= dm.Wait);
                if (_dimSawCu && !_cu) { _dimAfter += dt; if (_dimAfter >= dm.After) release = true; }
                if (release) { _dimPhase = 2; Log("暗転を戻す"); }
            }
            else
            {
                _dimK = Mathf.MoveTowards(_dimK, 1f, dt * Mathf.Max(0.01f, 1f - k) / Mathf.Max(0.01f, dm.Out));
                if (_dimK >= 1f) { _dimK = 1f; _dimPhase = 0; }
            }
            StageLook.SetDimMix(dm.Air, dm.Ambient, dm.Glow);   // 空気の効き (霧・環境光・暈。k=1 では何も書かない)
            StageLook.SetDim(_dimK);
        }

        // ---------------------------------------------------------------- 敵の大技の板

        static GameObject _plate;
        static MeshRenderer _plateR;
        static Material _plateMat;
        static float _plateT = -1f;
        static int _plateStartFrame;
        static Color _plateBand;
        static bool _plateWanted, _plateHooked;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        static void EnsurePlate()
        {
            var cam = Stage.Camera;
            if (cam == null) return;
            if (_plate == null)
            {
                _plateMat = Stage.MotionGlowMaterial(true);
                if (_plateMat == null) return;
                _plate = new GameObject("HD2D-MotionPlate");
                var mf = _plate.AddComponent<MeshFilter>();
                mf.sharedMesh = PlateMesh();
                _plateR = _plate.AddComponent<MeshRenderer>();
                _plateR.sharedMaterial = _plateMat;
                _plateR.shadowCastingMode = ShadowCastingMode.Off;
                _plateR.receiveShadows = false;
                _plateR.enabled = false;
            }
            if (_plate.transform.parent != cam.transform) _plate.transform.SetParent(cam.transform, false);
            if (!_plateHooked) { _plateHooked = true; RenderPipelineManager.beginCameraRendering += OnBeginCamera; }
        }

        /// <summary>板は舞台のカメラが描く時だけ見える (水面の反射のカメラ・シーンビューには写さない)</summary>
        static void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (_plateR == null) return;
            _plateR.enabled = _plateWanted && cam != null && cam == Stage.Camera;
        }

        static Mesh PlateMesh()
        {
            var m = new Mesh { name = "motion-plate" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);   // 板は視野いっぱい = 切り捨てられないように
            return m;
        }

        static void TickPlate(float dt)
        {
            if (_plateT < 0f) return;
            if (Time.frameCount != _plateStartFrame) _plateT += dt;
            var eb = Tune.EnemyBig;
            float t0 = eb.Cue, t1 = t0 + eb.Warm, t2 = t1 + eb.White, t3 = t2 + eb.Band;
            Color c = Color.white; float a = 0f;
            if (_plateT >= t0) CueTintOff();   // 赤い合図の終わり
            if (eb.LightMode) { TickFlash(eb, t0, t1, t2, t3); return; }   // 光で出す (既定。板は帯だけ)
            if (_plateT < t0) a = 0f;
            else if (_plateT < t1) { c = eb.WarmColor; a = eb.WarmAlpha; }
            else if (_plateT < t2) { float u = (_plateT - t1) / Mathf.Max(0.01f, eb.White); c = eb.WhiteColor; a = eb.WhiteAlpha * (1f - 0.5f * u); }
            else if (_plateT < t3) { float u = (_plateT - t2) / Mathf.Max(0.01f, eb.Band); c = _plateBand; a = eb.BandAlpha * (u < 0.8f ? 1f : (1f - u) / 0.2f); }
            else { _plateT = -1f; HidePlate(); Log("敵の大技の板を消した"); return; }
            SetPlate(c, a);
        }

        // 敵の大技を光で出す (mode light): 暖と白は技の光の印つきの大きな点光源 2 つ (A = 技の向かう先 = 自分の足元の上・B = 自分と敵の真ん中の上) と
        // 舞台の後処理の露出の山。板は技の色の帯だけ。光は段の頭で1回ずつ灯す (暖は合図の光を灯し直す = 光の数を増やさない・白は暖の2つを灯し直す)
        static int _flashPhase;          // 0 = まだ (合図)・1 = 暖を灯した・2 = 白を灯した
        static long _cueSeq, _flashSeqA, _flashSeqB;
        // 次の段が灯し直す光 (合図・暖) を段の長さより長く灯す秒 (直し 2026-10-03 反証: StageFx は灯したフレームから数え、こちらは始めたフレームを数えない・
        // LateUpdate の順も決まっていない = 光が段の境目の 1 フレーム前に消えて灯し直しに失敗し、光の無い 1 フレームが出ていた。60fps で 6 フレーム・30fps で 3 フレームの余り。
        // 余りの分は次の段の頭で灯し直すので消えていく所は画に出ない。消え方は hold が dur の割合なので、合図は段の終わりまで少し明るいまま = 合図→暖が途切れない)
        const float FlashReuseMargin = 0.1f;
        static string _plateKey;

        static void TickFlash(EnemyBigTune eb, float t0, float t1, float t2, float t3)
        {
            if (_plateT >= t3)
            {
                _plateT = -1f; HidePlate(); StageLook.SetExposureBoost(0f);
                Log("敵の大技の光と帯を消した");
                return;
            }
            float exp = 0f;
            if (_plateT >= t0 && _plateT < t1)
            {
                if (_flashPhase < 1) FlashLights(eb, 1);
                exp = eb.ExpWarm;
            }
            else if (_plateT >= t1 && _plateT < t2)
            {
                if (_flashPhase < 2) FlashLights(eb, 2);
                float u = Mathf.Clamp01((_plateT - t1) / Mathf.Max(0.01f, eb.White));
                exp = eb.ExpWhite * (1f - u * u);   // 白の頭がいちばん明るく、白の終わりで 0 へ
            }
            StageLook.SetExposureBoost(exp);   // 合図と帯の間は 0 (= 基へ正確に戻す)
            if (_plateT >= t2)
            {
                float u = (_plateT - t2) / Mathf.Max(0.01f, eb.Band);
                SetPlate(_plateBand, eb.BandAlpha * (u < 0.8f ? 1f : (1f - u) / 0.2f));
            }
            else SetPlate(Color.white, 0f);
        }

        /// <summary>暖 (phase 1) か白 (phase 2) の点光源 2 つを灯す (影なし・技の光の印つき = _HitReceive の材質が強く受ける)</summary>
        static void FlashLights(EnemyBigTune eb, int phase)
        {
            _flashPhase = phase;
            bool warm = phase == 1;
            Color col = warm ? eb.WarmColor : eb.WhiteColor;
            float I = warm ? eb.FlashWarm : eb.FlashWhite;
            // 暖は白の段が灯し直すので、段の長さに FlashReuseMargin を足して段の境目まで消さない (白は最後の光 = 段の長さのまま消える)
            float dur = Mathf.Max(0.02f, warm ? eb.Warm + FlashReuseMargin : eb.White);
            float hold = warm ? 0.9f : 0.35f;   // 暖は段の間ずっと最大・白は 0.35 から消えていく (露出の山と同じ形)
            if (!(I > 0f)) return;
            Vector3 a, b;
            bool hasB;
            if (!FlashPoints(eb, out a, out b, out hasB)) { Log("敵の大技の光を置けない (板も座席も無い) " + _plateKey); return; }
            // A の灯し直し: 暖は合図の光・白は暖の A (暖を灯さなかった = warm 0・強さ 0 なら合図の光)
            long reuseA = warm ? _cueSeq : (_flashSeqA != 0 ? _flashSeqA : _cueSeq);
            _flashSeqA = StageFx.MotionLight(a, col, I, eb.FlashRange, dur, hold, false, "enemyBig:" + (warm ? "warm" : "white"), "player", reuseA);
            if (hasB && eb.CenterMul > 0f)
                _flashSeqB = StageFx.MotionLight(b, col, I * eb.CenterMul, eb.FlashRange, dur, hold, false, "enemyBig-center:" + (warm ? "warm" : "white"), _plateKey, _flashSeqB);
            Log("敵の大技の" + (warm ? "暖" : "白") + "の光 強さ " + F(I) + " 距離 " + F(eb.FlashRange) + " A " + V(a) + (hasB ? " B " + V(b) : "") + " 露出 +" + F(warm ? eb.ExpWarm : eb.ExpWhite));
        }

        /// <summary>
        /// 暖と白の光の置き場: A = 技の向かう先 (自分の板の足元) の上 flashHeight、B = 自分と敵の足元の真ん中の上 centerHeight。どちらもカメラの方へ flashToward 寄せる。
        /// 自分の板が無ければ A は敵の足元の上 (B なし)。どちらも無ければ false
        /// </summary>
        static bool FlashPoints(EnemyBigTune eb, out Vector3 a, out Vector3 b, out bool hasB)
        {
            a = b = default; hasB = false;
            Vector3 pf, ef = default; float ph, eh;
            bool hasP = Stage.TryGetUnitBox("player", out pf, out ph);
            bool hasE = !string.IsNullOrEmpty(_plateKey) && Stage.TryGetUnitBox(_plateKey, out ef, out eh);
            if (!hasP && !hasE) return false;
            a = (hasP ? pf : ef) + Vector3.up * eb.FlashHeight;
            if (hasP && hasE) { b = 0.5f * (pf + ef) + Vector3.up * eb.CenterHeight; hasB = true; }
            var cam = Stage.Camera;
            if (cam != null && eb.FlashToward > 0f)
            {
                var ta = cam.transform.position - a; if (ta.sqrMagnitude > 1e-6f) a += ta.normalized * eb.FlashToward;
                if (hasB) { var tb = cam.transform.position - b; if (tb.sqrMagnitude > 1e-6f) b += tb.normalized * eb.FlashToward; }
            }
            return true;
        }

        static void SetPlate(Color c, float a)
        {
            EnsurePlate();
            if (_plate == null || _plateMat == null) return;
            var cam = Stage.Camera;
            if (cam == null) return;
            float z = cam.nearClipPlane + 0.05f;
            float h = 2f * z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.3f;
            float w = h * Mathf.Max(0.1f, cam.aspect) * 1.3f;
            var tr = _plate.transform;
            tr.localPosition = new Vector3(0f, 0f, z);
            tr.localRotation = Quaternion.identity;
            tr.localScale = new Vector3(w, h, 1f);
            _plateMat.SetColor(ColorId, new Color(c.r, c.g, c.b, Mathf.Clamp01(a)));
            _plateWanted = a > 0.001f;
        }

        // 赤い合図の間だけ敵の絵を染める (本家 guardian 22.48s「敵の絵が赤く染まる」)。キャラの板は Image の色を _BaseColor に掛けて描く (StageUnits)。
        // 戻す時は、自分が書いた色のままの時だけ元の色へ (その間に誰かが色を書き換えていたら、その値を残す = 古い色で上書きしない)
        static Image _cueImg;
        static Color _cueOrig, _cueSet;
        static bool _cueOn;

        static void CueTint(int enemyIndex, Color tint)
        {
            CueTintOff();
            if (Mathf.Approximately(tint.r, 1f) && Mathf.Approximately(tint.g, 1f) && Mathf.Approximately(tint.b, 1f)) return;
            var g = GameRoot.I;
            var rt = g != null && g.Battle != null ? g.Battle.EnemySprite(enemyIndex) : null;
            var img = rt != null ? rt.GetComponent<Image>() : null;
            if (img == null) return;
            _cueImg = img; _cueOrig = img.color;
            _cueSet = new Color(_cueOrig.r * tint.r, _cueOrig.g * tint.g, _cueOrig.b * tint.b, _cueOrig.a);
            img.color = _cueSet;
            _cueOn = true;
        }

        static void CueTintOff()
        {
            if (!_cueOn) return;
            _cueOn = false;
            if (_cueImg != null && _cueImg.color == _cueSet) _cueImg.color = _cueOrig;
            _cueImg = null;
        }

        static void HidePlate()
        {
            _plateWanted = false;
            if (_plateR != null) _plateR.enabled = false;
        }

        // ---------------------------------------------------------------- 止める

        /// <summary>全部止めて元へ戻す (門が閉じた時・戦闘の画面が無くなった時)。寄り・ピント・額縁・紙の UI・暗転・板・寄りの技の光 (印も外す)</summary>
        static void StopAll(string why)
        {
            if (_cu) EndCloseUp(why);
            StageFx.MotionStop();   // 寄りの技の光を消して印を外す (前のランの印を次の画面・次の幕へ持ち越さない。2026-10-03 反証)
            if (_dimPhase != 0 || _dimK < 1f) { _dimPhase = 0; _dimK = 1f; StageLook.SetDim(1f); }
            if (_plateT >= 0f) { _plateT = -1f; HidePlate(); }
            if (StageLook.FogScale < 1f) StageLook.SetFogScale(1f);           // 寄りの霧の倍率 (EndCloseUp が戻すが念のため)
            if (StageLook.ExposureBoost != 0f) StageLook.SetExposureBoost(0f); // 敵の大技の露出の山
            _flashPhase = 0; _cueSeq = _flashSeqA = _flashSeqB = 0;
            CueTintOff();
            Log("止めた (" + why + ")");
        }

        /// <summary>プレイの始まり (ドメインの読み直しなしでも) に時計・印・板の呼び出しを捨てる (StageHitReceive.ResetStatics と同じ考え)</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (_plateHooked) RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            _plateHooked = false; _plateWanted = false; _plate = null; _plateR = null; _plateMat = null; _plateT = -1f;
            _cu = false; _cuEnemy = -1; _cuKey = null; _cuKind = null; _cuLeft = 0f; _cuChain = false; _cuChainT = 0f; _cuFramesMoved = false; _hidView = null;
            _focusSet = false; _dimPhase = 0; _dimK = 1f; _sparks = null; _cueOn = false; _cueImg = null;
            _cuWallSet = false; _cuWallHow = null; _cuFog = 1f; _wallBoxes.Clear(); _wallBoxesOf = null; _wallBoxesSig = null;
            _flashPhase = 0; _cueSeq = _flashSeqA = _flashSeqB = 0; _plateKey = null;
            _tune = null; _tuneOf = null; _registered = false;
            _closeUps = _extended = _dims = _enemyBigs = _bursts = 0;
            _recent.Clear();
        }

        // ================================================================ 値 (光の設計図の motion)

        sealed class ZoomTune
        {
            public bool On = true;
            public bool Move = true;   // false = カメラは動かさない (寄りの時間・技の光・壁の光・火花・暗転だけ。2026-10-03 ユーザー「寄ると酔う」)
            public float Big = 0.45f, Boost = 0.9f, Finish = 0.9f, BossFinish = 1.8f, BigMin = 15f, BoostMin = 15f;
            public float Scale = 1.28f, LookUp = 6f, Yaw = 0f, Pivot = 0.45f, MinCamY = 0.35f;
            public float ChainHold = 0.5f;   // 同じ札の次の当たりを待つ上限 (秒。Play の当たりは 0.12 秒・順送りは 0.4 秒おき)
            public bool HideUi = true, HideDesk = true, Frames = true;
            public float FogScale = 0.6f;    // 寄りの間の霧の量の倍率 (StageLook.SetFogScale。1 = 触らない。直し 2026-10-03)
        }

        sealed class WallTune
        {
            public bool On = true;
            public float Back = 4f, Height = 1.6f, Intensity = 8f, Range = 9f;
            public float? S;   // 道の座標 s (path: Back の代わり・behind: その s の面)
            // mode behind (既定・直し 2026-10-03): 寄りのカメラ→対象の体の点の線の先で最初に当たる面の手前。path = 今までの置き方
            public bool Behind = true;
            public float Pull = 1.0f, Rise = 0.4f, MinHeight = 0.5f, MaxDist = 40f, MaxLift = 2.2f;
            public string[] Parts = { "wall" };   // 壁とみなす部品の名前の頭 (block・arch・pillar)
            public WallSteps Steps = WallSteps.Auto;   // 段の立ち上がりを面に数えるか (auto = 壁で持ち上げが maxLift を超える時だけ)
        }

        enum WallSteps { Off, On, Auto }

        sealed class LightTune
        {
            public bool On = true, Shadow = true;
            public float Intensity = 12f, Range = 10f, Dur = 1f, Hold = 0.35f, Height = 0.45f, TowardCamera = 0.6f, FinishMul = 1.25f;
            // 色 (光の色なので明るさ1に寄せた値。UI の役割の色ではない = check:colors の対象外の Stage*.cs に置く)。本家の洞窟の技の光は暖色の割合 0.91
            public Color Physical = new Color(1f, 0.62f, 0.3f);    // 斬撃・牙・角・蔦・踏みつけ = 暖色
            public Color Spell = new Color(0.35f, 1f, 0.85f);       // 呪文 = 脈の青緑
            public Color Lamp = new Color(1f, 0.74f, 0.4f);         // 灯 (白の呪文) = 暖色
            public Color Spark = new Color(1f, 0.5f, 0.2f);         // 火種 = 橙
            public readonly Dictionary<string, Color> Styles = new Dictionary<string, Color>();
            public WallTune Wall = new WallTune();

            public Color ColorFor(string style)
            {
                Color c;
                if (!string.IsNullOrEmpty(style) && Styles.TryGetValue(style, out c)) return c;
                switch (style)
                {
                    case "spell": return Spell;
                    case "light": return Lamp;
                    case "spark": return Spark;
                    default: return Physical;
                }
            }
        }

        sealed class DimTune
        {
            public bool On = true;
            public float K = 0.4f, In = 0.2f, Out = 0.35f, After = 0.1f, Wait = 1.5f, Max = 4f;   // wait 1.5 = X 札の多段 (最後の1発で寄る) が 0.13+0.12×(X−1) 秒後でも間に合う
            public string MinKind = "boost";   // この重さ以上の寄りを含む札だけ暗くする (big < boost < finish < bossFinish)。既定 boost = 分析書 §8-3「ブーストの溜め」 (2026-10-03 反証で big から)
            public float Air = 0.7f, Ambient = 0.9f, Glow = 0.9f;   // 空気の効き (StageLook.SetDimMix。霧・霧の板 / 環境光 / 暈と光の面。直し 2026-10-03)
        }

        sealed class SparkTune
        {
            public bool On = true;
            // 直し 2026-10-03 (反証「火花が点のまま」): 数・速さ・大きさ・光り方を上げ、速さで伸ばす筋に (前の既定 70・0.5・[3.5,8]・[0.05,0.11]・2.2・点)
            public int Count = 120;
            public float Life = 0.6f, SpeedMin = 5f, SpeedMax = 11f, SizeMin = 0.07f, SizeMax = 0.15f, Gravity = 0.6f, Drag = 3f, Glow = 3f, Height = 0.5f, TowardCamera = 0.8f;
            public bool Stretch = true;
            public float VelocityScale = 0.06f, LengthScale = 1f;
        }

        sealed class FocusTune
        {
            public bool On = true;
            public float Near = 2f, Far = 12f, TiltTop = 0f;
        }

        /// <summary>
        /// 敵の大技の既定の名指し (「敵の id:技の id」。2026-10-03 反証: 約束 §C5「予告つき大技だけ」= 溜め・数えられる拍・一度きりの大技)。
        /// 大亀の大薙ぎ (殻→殻→大薙ぎ)・火薬樽の大爆発 (3拍子)・斧鬼の大振り (振りかぶり→)・砥石の巨像の断頭 (構え→研ぎ→)・巨面の圧潰 (睨み→)・
        /// 梟の急降下 (羽撃ち→構え→)・大鴉の急降下 (舞い上がる→)・刺突の書の四連 (2→3→4)・汚泥の圧殺 (4拍目)・蛙の騎士の突進 (一度きり)・
        /// 巨蟹の撃ち腕の光線 (照準→)・巻き上げ機の番人の大光線 (巻き上げ×2→)・古炉の熾喰いと業火・朧の大鹿の角の突進・因縁の亡霊の大鎌 (実体の拍)・合成獣三の相の跳びかかり。
        /// 技の id は src/data/enemies.json と同じ (無い組は何も起きない)。設計図の enemyBig.moves を書けば置き換わる
        /// </summary>
        static readonly string[] DefaultBigMoves =
        {
            "enemy_turtle:crush", "enemy_bomber:big_boom", "enemy_axe_ogre:great_swing", "enemy_whetstone_colossus:guillotine",
            "enemy_elite_giant_face:crush", "enemy_elite_owl:talon_dive", "enemy_elite_gold_raven:dive", "enemy_elite_stab_book:stab4",
            "enemy_sludge_berserker:smother", "enemy_frog_knight:beetle_charge", "enemy_crab_cannon:laser", "enemy_winch_warden:hyper_beam",
            "enemy_ember_furnace:devour_ember", "enemy_ember_furnace:inferno", "enemy_haze_stag:antler_charge", "enemy_nemesis_wraith:scythe",
            "enemy_chimera_3:pounce",
        };

        sealed class EnemyBigTune
        {
            public bool On = true, Auto = false;   // auto = 名指しの外の推定 (既定 false。2026-10-03 反証で true から)
            public float MinTotal = 20f;
            public HashSet<string> Moves = new HashSet<string>(DefaultBigMoves, StringComparer.Ordinal);   // 「敵の id:技の id」か「技の id」
            public float Cue = 0.17f, Warm = 0.15f, White = 0.3f, Band = 1.2f;
            public Color CueColor = new Color(1f, 0.25f, 0.18f);
            public float CueIntensity = 6f, CueRange = 5f, CueHeight = 0.35f;
            public Color CueTint = new Color(1f, 0.62f, 0.55f);   // 赤い合図の間の敵の絵 (掛ける色)
            public Color WarmColor = new Color(1f, 0.55f, 0.22f);
            public float WarmAlpha = 0.45f;
            public Color WhiteColor = new Color(1f, 1f, 1f);
            public float WhiteAlpha = 0.5f;
            public Color? BandColor;
            public Color BandMelee = new Color(1f, 0.5f, 0.28f);
            public float BandAlpha = 0.16f;
            // mode light (既定・直し 2026-10-03): 暖と白を舞台だけの光 (点光源 2 つ＋露出の山) で出す。false = plate (今までの板)
            public bool LightMode = true;
            public float FlashWarm = 8f, FlashWhite = 14f, FlashRange = 16f, FlashHeight = 1.2f, CenterHeight = 2f, CenterMul = 0.8f, FlashToward = 0.8f;
            public float ExpWarm = 0.5f, ExpWhite = 1f;   // 露出の足し (段)
            public float HurtFlash = 0.1f;                // 合図から帯の終わりまでの被弾の赤い点滅の α の上限 (0 = 出さない)
        }

        sealed class MotionTune
        {
            public bool On;
            public ZoomTune Zoom = new ZoomTune();
            public LightTune Light = new LightTune();
            public DimTune Dim = new DimTune();
            public SparkTune Sparks = new SparkTune();
            public FocusTune Focus = new FocusTune();
            public EnemyBigTune EnemyBig = new EnemyBigTune();
        }

        static MotionTune _tune;
        static StageLookData _tuneOf;
        static HD2DTier _tuneTier;
        static readonly MotionTune Off = new MotionTune();

        static MotionTune Tune
        {
            get
            {
                var cur = StageLook.Current;
                if (cur == null) return Off;
                var tier = HD2DFlags.Tier;
                if (_tune != null && ReferenceEquals(_tuneOf, cur) && tier == _tuneTier) return _tune;
                _tuneOf = cur; _tuneTier = tier;
                _tune = ReadTune(cur, tier == HD2DTier.Phone);
                return _tune;
            }
        }

        static MotionTune ReadTune(StageLookData d, bool phone)
        {
            var t = new MotionTune();
            JObject m = null;
            try { m = d != null && d.Raw != null ? d.Raw["motion"] as JObject : null; } catch (Exception) { m = null; }
            if (m == null) return t;
            try
            {
                if (phone && m["phone"] is JObject ph)
                {
                    m = (JObject)m.DeepClone();
                    m.Merge(ph, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Ignore });
                }
                t.On = Bool(m, "on", false);
                if (m["zoom"] is JObject z)
                {
                    var Z = t.Zoom;
                    Z.On = Bool(z, "on", Z.On); Z.Move = Bool(z, "move", Z.Move);
                    Z.Big = Num(z, "big", Z.Big); Z.Boost = Num(z, "boost", Z.Boost); Z.Finish = Num(z, "finish", Z.Finish); Z.BossFinish = Num(z, "bossFinish", Z.BossFinish);
                    Z.BigMin = Num(z, "bigMin", Z.BigMin); Z.BoostMin = Num(z, "boostMin", Z.BoostMin);
                    Z.Scale = Num(z, "scale", Z.Scale); Z.LookUp = Signed(z, "lookUp", Z.LookUp); Z.Yaw = Signed(z, "yaw", Z.Yaw);
                    Z.Pivot = Num(z, "pivot", Z.Pivot); Z.MinCamY = Signed(z, "minCamY", Z.MinCamY); Z.ChainHold = Num(z, "chainHold", Z.ChainHold);
                    Z.HideUi = Bool(z, "hideUi", Z.HideUi); Z.HideDesk = Bool(z, "hideDesk", Z.HideDesk);
                    Z.FogScale = Num(z, "fogScale", Z.FogScale);
                    var fr = z["frames"];
                    if (fr != null && fr.Type == JTokenType.String) Z.Frames = !string.Equals((string)fr, "keep", StringComparison.OrdinalIgnoreCase);
                    else if (fr != null && fr.Type == JTokenType.Boolean) Z.Frames = (bool)fr;
                }
                if (m["light"] is JObject l)
                {
                    var L = t.Light;
                    L.On = Bool(l, "on", L.On); L.Shadow = Bool(l, "shadow", L.Shadow);
                    L.Intensity = Num(l, "intensity", L.Intensity); L.Range = Num(l, "range", L.Range); L.Dur = Num(l, "dur", L.Dur); L.Hold = Num(l, "hold", L.Hold);
                    L.Height = Num(l, "height", L.Height); L.TowardCamera = Num(l, "towardCamera", L.TowardCamera); L.FinishMul = Num(l, "finishMul", L.FinishMul);
                    if (l["colors"] is JObject cs)
                        foreach (var p in cs.Properties())
                        {
                            Color c;
                            if (!Col(p.Value, out c)) continue;
                            switch (p.Name)
                            {
                                case "physical": L.Physical = c; break;
                                case "spell": L.Spell = c; break;
                                case "light": L.Lamp = c; break;
                                case "spark": L.Spark = c; break;
                                default: L.Styles[p.Name] = c; break;
                            }
                        }
                    if (l["wall"] is JObject w)
                    {
                        var W = L.Wall;
                        W.On = Bool(w, "on", W.On); W.Back = Signed(w, "back", W.Back); W.Height = Signed(w, "height", W.Height);
                        W.Intensity = Num(w, "intensity", W.Intensity); W.Range = Num(w, "range", W.Range);
                        if (w["s"] != null && (w["s"].Type == JTokenType.Float || w["s"].Type == JTokenType.Integer)) { float sv = w["s"].Value<float>(); if (!float.IsNaN(sv) && !float.IsInfinity(sv)) W.S = sv; }
                        var wm = w["mode"];
                        if (wm != null && wm.Type == JTokenType.String) W.Behind = !string.Equals((string)wm, "path", StringComparison.OrdinalIgnoreCase);
                        W.Pull = Num(w, "pull", W.Pull); W.Rise = Signed(w, "rise", W.Rise); W.MinHeight = Signed(w, "minHeight", W.MinHeight); W.MaxDist = Num(w, "maxDist", W.MaxDist);
                        W.MaxLift = Num(w, "maxLift", W.MaxLift);
                        var ws = w["steps"];
                        if (ws != null && ws.Type == JTokenType.Boolean) W.Steps = (bool)ws ? WallSteps.On : WallSteps.Off;
                        else if (ws != null && ws.Type == JTokenType.String) W.Steps = string.Equals((string)ws, "auto", StringComparison.OrdinalIgnoreCase) ? WallSteps.Auto : (string.Equals((string)ws, "on", StringComparison.OrdinalIgnoreCase) ? WallSteps.On : WallSteps.Off);
                        if (w["parts"] is JArray wp)
                        {
                            var names = new List<string>();
                            foreach (var x in wp) if (x.Type == JTokenType.String && ((string)x).Length > 0) names.Add((string)x);
                            W.Parts = names.ToArray();
                        }
                    }
                }
                if (m["dim"] is JObject dm)
                {
                    var D = t.Dim;
                    D.On = Bool(dm, "on", D.On); D.K = Num(dm, "k", D.K); D.In = Num(dm, "in", D.In); D.Out = Num(dm, "out", D.Out);
                    D.After = Num(dm, "after", D.After); D.Wait = Num(dm, "wait", D.Wait); D.Max = Num(dm, "max", D.Max);
                    if (dm["minKind"] != null && dm["minKind"].Type == JTokenType.String && Rank((string)dm["minKind"]) > 0) D.MinKind = (string)dm["minKind"];
                    D.Air = Num(dm, "air", D.Air); D.Ambient = Num(dm, "ambient", D.Ambient); D.Glow = Num(dm, "glow", D.Glow);
                }
                if (m["sparks"] is JObject sp)
                {
                    var S = t.Sparks;
                    S.On = Bool(sp, "on", S.On); S.Count = Mathf.RoundToInt(Num(sp, "count", S.Count)); S.Life = Num(sp, "life", S.Life);
                    Range2(sp, "speed", ref S.SpeedMin, ref S.SpeedMax); Range2(sp, "size", ref S.SizeMin, ref S.SizeMax);
                    S.Gravity = Signed(sp, "gravity", S.Gravity); S.Drag = Num(sp, "drag", S.Drag); S.Glow = Num(sp, "glow", S.Glow);
                    S.Height = Num(sp, "height", S.Height); S.TowardCamera = Num(sp, "towardCamera", S.TowardCamera);
                    S.Stretch = Bool(sp, "stretch", S.Stretch); S.VelocityScale = Num(sp, "velocityScale", S.VelocityScale); S.LengthScale = Num(sp, "lengthScale", S.LengthScale);
                }
                if (m["focus"] is JObject fo)
                {
                    var Fo = t.Focus;
                    Fo.On = Bool(fo, "on", Fo.On); Fo.Near = Num(fo, "near", Fo.Near); Fo.Far = Num(fo, "far", Fo.Far); Fo.TiltTop = Signed(fo, "tiltTop", Fo.TiltTop);
                }
                if (m["enemyBig"] is JObject eb)
                {
                    var E = t.EnemyBig;
                    E.On = Bool(eb, "on", E.On); E.Auto = Bool(eb, "auto", E.Auto); E.MinTotal = Num(eb, "minTotal", E.MinTotal);
                    if (eb["moves"] is JArray mv)
                    {
                        E.Moves = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var x in mv) if (x.Type == JTokenType.String) E.Moves.Add((string)x);
                    }
                    E.Cue = Num(eb, "cue", E.Cue); E.Warm = Num(eb, "warm", E.Warm); E.White = Num(eb, "white", E.White); E.Band = Num(eb, "band", E.Band);
                    Color c;
                    if (eb["cueColor"] != null && Col(eb["cueColor"], out c)) E.CueColor = c;
                    E.CueIntensity = Num(eb, "cueIntensity", E.CueIntensity); E.CueRange = Num(eb, "cueRange", E.CueRange); E.CueHeight = Num(eb, "cueHeight", E.CueHeight);
                    if (eb["cueTint"] != null && Col(eb["cueTint"], out c)) E.CueTint = c;
                    if (eb["warmColor"] != null && Col(eb["warmColor"], out c)) E.WarmColor = c;
                    E.WarmAlpha = Num(eb, "warmAlpha", E.WarmAlpha);
                    if (eb["whiteColor"] != null && Col(eb["whiteColor"], out c)) E.WhiteColor = c;
                    E.WhiteAlpha = Num(eb, "whiteAlpha", E.WhiteAlpha);
                    if (eb["bandColor"] != null && Col(eb["bandColor"], out c)) E.BandColor = c;
                    if (eb["bandMelee"] != null && Col(eb["bandMelee"], out c)) E.BandMelee = c;
                    E.BandAlpha = Num(eb, "bandAlpha", E.BandAlpha);
                    var em = eb["mode"];
                    if (em != null && em.Type == JTokenType.String) E.LightMode = !string.Equals((string)em, "plate", StringComparison.OrdinalIgnoreCase);
                    E.FlashWarm = Num(eb, "flashWarm", E.FlashWarm); E.FlashWhite = Num(eb, "flashWhite", E.FlashWhite); E.FlashRange = Num(eb, "flashRange", E.FlashRange);
                    E.FlashHeight = Signed(eb, "flashHeight", E.FlashHeight); E.CenterHeight = Signed(eb, "centerHeight", E.CenterHeight);
                    E.CenterMul = Num(eb, "centerMul", E.CenterMul); E.FlashToward = Num(eb, "flashToward", E.FlashToward);
                    E.ExpWarm = Signed(eb, "expWarm", E.ExpWarm); E.ExpWhite = Signed(eb, "expWhite", E.ExpWhite);
                    E.HurtFlash = Num(eb, "hurtFlashAlpha", E.HurtFlash);
                }
            }
            catch (Exception e) { Debug.LogWarning("[StageMotion] 光の設計図の motion を読めない (読めた所まで使う): " + e.Message); }
            if (HD2DFlags.Det || HD2DFlags.DumpLayout)
                Debug.Log("[StageMotion] motion を読んだ: " + (t.On ? "on" : "off") + (phone ? " (phone を重ねた)" : "") + " 寄り " + F(t.Zoom.Big) + "/" + F(t.Zoom.Boost) + "/" + F(t.Zoom.Finish) + "/" + F(t.Zoom.BossFinish)
                    + "秒 倍率 " + F(t.Zoom.Scale) + " 光 " + F(t.Light.Intensity) + " 距離 " + F(t.Light.Range) + " 暗転 " + F(t.Dim.K)
                    + " 壁 " + (t.Light.Wall.Behind ? "behind" : "path") + " 霧 " + F(t.Zoom.FogScale) + " 敵の大技 " + (t.EnemyBig.LightMode ? "light" : "plate") + " 火花 " + t.Sparks.Count + (t.Sparks.Stretch ? " 筋" : ""));
            return t;
        }

        static bool Bool(JObject o, string name, bool def)
        {
            var v = o[name];
            return v != null && v.Type == JTokenType.Boolean ? (bool)v : def;
        }

        /// <summary>0 以上の数 (負・NaN・読めない値は def)</summary>
        static float Num(JObject o, string name, float def)
        {
            float f = Signed(o, name, def);
            return f < 0f ? def : f;
        }

        /// <summary>符号つきの数 (NaN・無限・読めない値は def)</summary>
        static float Signed(JObject o, string name, float def)
        {
            var v = o[name];
            if (v == null || (v.Type != JTokenType.Float && v.Type != JTokenType.Integer)) return def;
            float f = v.Value<float>();
            return float.IsNaN(f) || float.IsInfinity(f) ? def : f;
        }

        /// <summary>[min, max] の2つの数 (どちらも 0 以上・min ≦ max の時だけ)</summary>
        static void Range2(JObject o, string name, ref float min, ref float max)
        {
            var a = o[name] as JArray;
            if (a == null || a.Count < 2) return;
            if ((a[0].Type != JTokenType.Float && a[0].Type != JTokenType.Integer) || (a[1].Type != JTokenType.Float && a[1].Type != JTokenType.Integer)) return;
            float x = a[0].Value<float>(), y = a[1].Value<float>();
            if (x >= 0f && y >= x) { min = x; max = y; }
        }

        /// <summary>[r,g,b] (0〜1 か 0〜255。どれかが 1 を超えたら 255 の物差し)</summary>
        static bool Col(JToken v, out Color c)
        {
            c = default;
            var a = v as JArray;
            if (a == null || a.Count < 3) return false;
            for (int i = 0; i < 3; i++) if (a[i].Type != JTokenType.Float && a[i].Type != JTokenType.Integer) return false;
            float r = a[0].Value<float>(), g = a[1].Value<float>(), b = a[2].Value<float>();
            if (r > 1f || g > 1f || b > 1f) { r /= 255f; g /= 255f; b /= 255f; }
            c = new Color(r, g, b, 1f);
            return true;
        }

        // ================================================================ 記録

        static int _closeUps, _extended, _dims, _enemyBigs;
        static readonly List<Dictionary<string, object>> _recent = new List<Dictionary<string, object>>();
        const int RecentMax = 24;

        static void Remember(string kind, string key, float dur)
        {
            var r = new Dictionary<string, object> { { "frame", Time.frameCount }, { "time", Time.time }, { "kind", kind }, { "key", key }, { "dur", dur } };
            if (kind != null && kind.StartsWith("closeUp:", StringComparison.Ordinal))
            {
                r["camPos"] = _cuPos; r["camEuler"] = _cuRot.eulerAngles; r["pivot"] = _cuPivot; r["feet"] = _cuFeet;
                r["pathBand"] = new[] { TiltShiftSettings.PathNear, TiltShiftSettings.PathFar };
                r["depthBand"] = new[] { TiltShiftSettings.BandNear, TiltShiftSettings.BandFar };
            }
            if (kind != null && kind.StartsWith("wall:", StringComparison.Ordinal)) r["pos"] = _cuWall;   // 壁の光の置き場 (直し 2026-10-03)
            _recent.Insert(0, r);
            if (_recent.Count > RecentMax) _recent.RemoveRange(RecentMax, _recent.Count - RecentMax);
        }

        /// <summary>dumplayout の extra.motion: 門・読んだ値・今の寄り (対象・残り・姿勢)・暗転の k・板・数・最近の記録</summary>
        public static object DebugInfo()
        {
            var t = Tune;
            var o = new Dictionary<string, object>();
            o["on"] = StageFx.Live && t.On;
            o["tune"] = new Dictionary<string, object>
            {
                { "zoom", new[] { t.Zoom.Big, t.Zoom.Boost, t.Zoom.Finish, t.Zoom.BossFinish } }, { "scale", t.Zoom.Scale }, { "lookUp", t.Zoom.LookUp }, { "yaw", t.Zoom.Yaw },
                { "light", new[] { t.Light.Intensity, t.Light.Range, t.Light.Dur, t.Light.Hold } }, { "wall", new[] { t.Light.Wall.Back, t.Light.Wall.Height, t.Light.Wall.Intensity, t.Light.Wall.Range } }, { "wallS", t.Light.Wall.S },
                { "dimK", t.Dim.K }, { "sparks", t.Sparks.Count }, { "focus", new[] { t.Focus.Near, t.Focus.Far, t.Focus.TiltTop } }, { "enemyBigMin", t.EnemyBig.MinTotal }, { "boostMin", t.Zoom.BoostMin },
                { "dimMinKind", t.Dim.MinKind }, { "enemyBigAuto", t.EnemyBig.Auto }, { "enemyBigMoves", t.EnemyBig.Moves != null ? t.EnemyBig.Moves.Count : 0 },
                // 直し 2026-10-03: 壁の光の置き方・寄りの霧・暗転の空気の効き・敵の大技の出し方・火花の筋
                { "wallMode", t.Light.Wall.Behind ? "behind" : "path" }, { "wallBehind", new[] { t.Light.Wall.Pull, t.Light.Wall.Rise, t.Light.Wall.MinHeight, t.Light.Wall.MaxDist, t.Light.Wall.MaxLift } }, { "wallSteps", t.Light.Wall.Steps.ToString() },
                { "fogScale", t.Zoom.FogScale }, { "dimMix", new[] { t.Dim.Air, t.Dim.Ambient, t.Dim.Glow } },
                { "enemyBigMode", t.EnemyBig.LightMode ? "light" : "plate" },
                { "enemyBigFlash", new[] { t.EnemyBig.FlashWarm, t.EnemyBig.FlashWhite, t.EnemyBig.FlashRange, t.EnemyBig.ExpWarm, t.EnemyBig.ExpWhite, t.EnemyBig.HurtFlash } },
                { "sparkStretch", t.Sparks.Stretch ? new[] { t.Sparks.VelocityScale, t.Sparks.LengthScale } : null },
            };
            o["closeUp"] = _cu ? new Dictionary<string, object>
            {
                { "key", _cuKey }, { "kind", _cuKind }, { "left", _cuLeft }, { "total", _cuTotal }, { "chain", _cuChain }, { "chainWait", _cuChainT }, { "camPos", _cuPos }, { "camEuler", _cuRot.eulerAngles }, { "pivot", _cuPivot },
                // 額縁を寄りのカメラに付け直している (Diorama.OnCameraLayout に寄りの姿勢を渡した) = この間の Diorama の記録 (offscreenParts など) は寄りのカメラから数えた値
                { "framesOnCloseUp", _cuFramesMoved },
                { "wall", _cuWallSet ? (object)_cuWall : null }, { "wallHow", _cuWallHow }, { "fogScale", StageLook.FogScale },
            } : null;
            o["dimK"] = _dimK;
            o["dimPhase"] = _dimPhase;
            o["plateT"] = _plateT;
            o["flashPhase"] = _flashPhase;
            o["exposureBoost"] = StageLook.ExposureBoost;
            o["counts"] = new Dictionary<string, object> { { "closeUps", _closeUps }, { "extended", _extended }, { "dims", _dims }, { "enemyBigs", _enemyBigs }, { "sparkBursts", _bursts } };
            o["recent"] = new List<Dictionary<string, object>>(_recent);
            return o;
        }

        static void Log(string msg)
        {
            if (HD2DFlags.Det || HD2DFlags.DumpLayout)
                Debug.Log("[StageMotion] " + msg + " frame " + Time.frameCount + " t " + F(Time.time));
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string V(Vector3 v) => "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
    }
}
