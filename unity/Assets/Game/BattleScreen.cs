// BattleScreen.cs — 戦闘画面 (2026-09-07 M2: モダンな戦闘UIの文法へ作り直し)。
// 1920×1080 のキャンバス直下に組む。上=状況バー／中=戦場 (左にリーダーと伏せ場・右に敵)／下=扇状の手札と山札・捨て札・ターン終了。
// ログは引き出し (既定は閉)。確認ウィンドウ・対象選択・追加コストのピッカーはモーダル。
// ここは「状態を読んでコマンドを投げるだけ」。演出は Presenter (イベントログ差分) が担う。
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class BattleScreen
    {
        /// <summary>上部バーの高さ。スマホは 56 (2026-09-15: 頭上の吹き出しの高さを稼ぐ。札は 40〜44 なので収まる)</summary>
        public static float TopH { get { return UiKit.Phone ? 56f : 72f; } }
        /// <summary>手札の下端 (キャンバス下からの距離)。スマホは詰める。HD-2D 見本の箱庭では HandSink だけ沈める</summary>
        public static float HandY { get { return (UiKit.Phone ? 14f : 30f) - HandSink; } }
        /// <summary>HD-2D 見本の箱庭 (stage=diorama) の画面の配置か (2026-09-30 P20)。足元の線・手札の沈め・帳面の置き方 (ledger=feet)・暗幕の丈がこの時だけ変わる。
        /// 紙か夜か (ui=night|paper) とは別 = ui=paper でも同じ配置で撮れる。今の舞台 (stage=old) は1画素も変えない</summary>
        public static bool Hd2dLayout { get { return HD2DFlags.StageMode == HD2DStage.Diorama; } }
        /// <summary>
        /// 手札を沈める量 (キャンバス単位。HD-2D 見本の箱庭だけ。2026-09-30 P20)。上限は手札の本文の数字が画面の下端から 36px (layout-check L5):
        /// W2 の撮影で両端の札の数字の下端が PC 57.8px・スマホ 60.4px (5枚の扇) なので、PC 19 (→38.8px)・スマホ 17 (×1.31＝22.3px → 38.1px)。
        /// 計画の目安「PC 30→−12」(42 沈める) は数字が 16px まで下がるので採らない (lane-P20.md)。触れた札の持ち上げにはこの量を足す (HookHandCard)
        /// </summary>
        public static float HandSink { get { return Hd2dLayout ? (R3 ? (UiKit.Phone ? R3U_HandSinkPhone : R3U_HandSinkPc) : (UiKit.Phone ? 17f : 19f)) : 0f; } }

        // ---- 三周目 R3 UI の作り直し (2026-10-02 ユーザー裁定「本番に入れる」・仕様 docs/design/hd2d-slice/r3-ui-spec.md) ----
        // 箱庭 (Hd2dLayout) の既定 = 旗 uilayout=r3 (uilayout=r2 で二周目の割り付け。uitrial=1 は r3 の別名)。PC とスマホの両方:
        //   足元の線 PC 0.36・スマホ 0.45 (Stage.GroundLineRatio)／手札を PC 168・スマホ 176 沈め、上の帯と「要の数字の札」(CardView keynum) だけ見せ、触れた札だけ上げる
        //   (本文は触れて読む)／敵の帳面は足元の下 (PC 10・スマホ 8) に名前＋HP の 60 (予告の行があれば全員 88。スマホ 53/79)／
        //   PC の自分の欄 = 主人公の足元の下の「足元の帳」(HP・見込み・状態) と左下の「匣」(からくり・ギア・置物を輪の上へ下から積む)／
        //   スマホ = 足元の帳 (HP・見込み・からくり) と今の上の帯 (状態・ギア・置物)、輪・灯籠・山札は下へ／確認の窓・ギアの窓・持ち物の一覧は R3PcWindowRect・R3PhoneWindowRect。
        //   今の舞台 (stage=old) は Hd2dLayout が偽なので1画素も変わらない。段1 の試し撮り (R3A_) を本番の形にしたもの (新しいメンバーは R3U_)

        /// <summary>箱庭で三周目の割り付けか (PC とスマホ共通。r3 の分岐は全部この値を読む)</summary>
        public static bool R3 { get { return Hd2dLayout && HD2DFlags.UiLayout == HD2DUiLayout.R3; } }
        /// <summary>手札の沈め (r3)。PC 168 (=手札の上端 行 951。試し撮りの 183 だと10枚の端の札の要の数字の札が画面の外に出る)・スマホ 176 (手札の上端 上から 547)</summary>
        const float R3U_HandSinkPc = 168f, R3U_HandSinkPhone = 176f;
        /// <summary>触れた (押した) 札の持ち上げ。沈めた量を足す＝持ち上げた札は二周目と同じ高さ (PC)。スマホ r3 は +130 (札の全体が親指 = 画面の下 0〜110 より上に出る)</summary>
        public static float HandLift { get { return R3 && UiKit.Phone ? 130f : 70f; } }
        /// <summary>
        /// 手札を「場に出す」高さ (画面の px・下から): 札を離した点がこれより上なら出す。二周目は画面の 36% (1080 で 389＝二周目の手札の上端 278 の 111 上)。
        /// r3 の PC は「触れて上がった札の上端」の 15.6 下 (=二周目と同じ 行 691・キャンバス 388.8)。PC は上がった札のどこでもつかめるので、
        /// 沈めた手札の上端＋111 (行 840) だと上がった札の上半分が線より上にあり、つかんでその場で離すだけで出てしまった (2026-10-02 読み合わせの指摘)。
        /// r3 のスマホは沈めた手札の上端＋111 (下から 239。押した札をつかむ前に上げない狙いの矢)。BattleView の狙いの矢 (スマホ) もこの線で隠す
        /// </summary>
        public static float DropLineScreen(RectTransform any)
        {
            if (!R3) return Screen.height * 0.36f;
            float ch = CanvasSize(any).y;
            float canvasY = UiKit.Phone
                ? HandY + CardView.H * CardScale + R3A_DropMargin
                : HandY + HandSink + HandLift + CardView.H * (CardScale + R3U_HoverScale) / 2f - R3U_DropBelowLiftTop;   // 上がった札の上端 (中心の札。回転0) − 15.6
            return ch > 0f ? canvasY * Screen.height / ch : Screen.height * 0.36f;
        }
        /// <summary>触れた札の倍率 (HookHandCard の PointerEnter と同じ値)・PC の「場に出す」線を上がった札の上端から下げる量 (二周目の 404.4−388.8)</summary>
        const float R3U_HoverScale = 1.18f, R3U_DropBelowLiftTop = 15.6f;
        /// <summary>二周目の手札の上端 (30−19+290×0.92＝277.8) と「場に出す」線 (1080×0.36＝388.8) の差</summary>
        const float R3A_DropMargin = 111f;
        /// <summary>r3 の PC の敵の帳面の高さ (名前の行＋HP バー＝上 4・名前 30・間 2・バー 20・下 4) と予告の行 (28)</summary>
        const float R3A_LedgerThinH = 60f, R3U_ForecastRowH = 28f;
        /// <summary>足元の帳 (r3) の右端 (キャンバス x)。下の窓・名前の帯・人形の札の障害物が読む。組むたびに書く。r3 でない時は -1</summary>
        public static float R3U_FootRight = -1f;
        /// <summary>r3 のスマホの足元の帳の HP の区画の右端 (キャンバス x)。窓の左端 (仕様 §8-2) が読む。組むたびに書く</summary>
        public static float R3U_FootHpRight = -1f;
        /// <summary>r3 のスマホの足元の帳 (hpwrap) の矩形 (キャンバス・左下基準)。窓の下端がこれを縁で切らない (仕様 §8-2・直しの輪1)。組むたびに書く。無ければ幅 0</summary>
        public static Rect R3U_PhoneFootRect = default(Rect);
        /// <summary>r3 の自分の欄の矩形 (キャンバス・左下基準)。足元の帳・匣 (PC)／足元の帳・上の帯 (スマホ)。人形の足元の札の障害物 (ArrangeDollTags)。組むたびに書く</summary>
        public static readonly List<Rect> R3U_SelfRects = new List<Rect>();
        /// <summary>手札の札の倍率。スマホは等倍 (2026-09-14「字が小さい」= 札の本文が最も読まれる文字)</summary>
        public static float CardScale { get { return UiKit.Phone ? 1.0f : 0.92f; } }
        /// <summary>戦闘の絵の目安の幅 (通常 256・エリート 320・ボス 384 = 1ドット4px)。スマホは半分 (1ドット2px) = 吹き出しが画面に収まる。
        /// 旗 artscale= (HD-2D 見本の変種。0.625 = S25 の実機で1ドット4px) が有ればその値</summary>
        public static float ArtScale { get { float f = HD2DFlags.ArtScale; return f > 0f ? f : (UiKit.Phone ? 0.6f : 1f); } }   // スマホは 0.5→0.6 (2026-09-15 吹き出しの小型化と対で「敵が小さすぎる」を戻す)
        /// <summary>PC の自分の札の上端 (キャンバス y・下から) = 足元の線＋StripH (札は上端を固定して中身の 120/140 を下で吸収する。2026-09-30 F19)。
        /// ギアの窓・人形の札の床・確認の窓が読む (2026-09-30 P20: 箱庭で足元の線が下がった時に1か所で追う)</summary>
        public static float SelfCardTop { get { return BattleView.StatusLineY + StripH; } }
        /// <summary>キャンバスの実寸 (スマホ 1.6倍なら 1200〜1462×675)。組み立て中に画面の上端 (上部バーの下) を知るため</summary>
        public static Vector2 CanvasSize(RectTransform any)
        {
            // 最初のフレーム (Awake の組み立て) はキャンバスの矩形がまだ px のままなので、スケーラーの式で毎回求める (CanvasScaler.ScaleWithScreenSize と同じ計算)
            var canvas = any != null ? any.GetComponentInParent<Canvas>() : null;
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler == null || Screen.width <= 0 || Screen.height <= 0) return new Vector2(1920f, 1080f);
            float logW = Mathf.Log(Screen.width / scaler.referenceResolution.x, 2f);
            float logH = Mathf.Log(Screen.height / scaler.referenceResolution.y, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(logW, logH, scaler.matchWidthOrHeight));
            return new Vector2(Screen.width / scale, Screen.height / scale);
        }

        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            var st = run.Combat;
            var v = BattleView.Ensure(g, root);
            v.SyncField(g, st);
            v.ClearUi();
            var ui = v.UiLayer;
            BuildPiles(g, ui, st);
            BuildEndTurn(g, ui, st);
            BuildTopBar(g, ui, run, st);
            if (g.ShowLog) BuildLogDrawer(g, ui, st);
            v.SyncHand(g, st, true);

            if (st.Phase == CombatPhases.AwaitingReaction) BuildReactionWindow(g, ui, st);
            else if (st.PendingScry != null) BuildScryChooser(g, ui, st);   // 占術 (青 2026-09-25): 選ぶまで他の操作はできない
            else if (g.Pending != null)
            {
                var need = g.Pending.NextNeed();
                if (need == "target") BuildTargetBanner(g, ui);
                else if (need != null) BuildPicker(g, ui, st, need);
            }
            else if (g.ModeChoiceUid != null) BuildModeChooser(g, ui, st);
            else if (g.GearPending != null) GearUi.BuildPending(g, ui, run, st);   // ギア (2026-09-17): 窓／札を選ぶ／対象の帯
            else if (g.GearMore) GearUi.BuildMore(g, ui, run, st);   // ギアの「+N」= 持ち物の一覧 (2026-09-18)
            else if (g.RetainChoice != null) BuildRetainChooser(g, ui, st);   // 満ち潮の書庫: 残す手札 (青 2026-09-25)
            else if (g.HearthChoice) BuildHearthChooser(g, ui, st);   // 灯の火床: 火種にする枚数 (2026-09-20 夜)
        }

        // ---- 背景 ----

        /// <summary>戦闘の背景 (BattleView の FieldLayer)。戦闘以外の画面の暗がり (MenuShade) は足さない</summary>
        public static void BuildBackground(RectTransform root, RunState run) { BuildBackground(root, run.Act, false); }

        /// <summary>戦闘以外の画面 (タイトル・地図・報酬・店・焚き火・工房・?・出立の店・終わり) の背景</summary>
        public static void BuildBackground(RectTransform root, int act) { BuildBackground(root, act, true); }

        public static void BuildBackground(RectTransform root, int act, bool menu)
        {
            // 背景は舞台 (別カメラ・ポスト処理と粒子つき) に描く。UI 側 (root) には何も置かない (箱庭の上の戦闘以外の画面の暗がりだけ例外 = 下の MenuShade)。
            // 舞台が組めなくても UI は組む (2026-09-14 実機: 舞台の例外で画面ごと消えていた疑い。原因は ErrorOverlay に出る)
            try { Stage.Paint(act); }
            catch (Exception e) { Debug.LogException(e); }
            if (menu && Stage.ShowingDiorama) MenuShade(root);
        }

        /// <summary>
        /// 箱庭の上の戦闘以外の画面の暗がり (2026-10-01 APK の既定で幕1 の全画面が箱庭になった時の直し)。
        /// 箱庭は画面の上 1/3 に明るい霧の帯があり (縦 120〜420 の平均 126)、今の舞台 (暗い夜の森) を前提にした各画面の暗幕 (0.35〜0.65) だけでは、
        /// 舞台の上に直に置いた淡い字 (見出しの下の説明・地図のノードの名前・タイトルの副題・終わりの画面の要約) の対比が今の舞台の半分に落ちた。
        /// 各画面の暗幕の下に、地の色 (PaperFx.Ground) を上 α0.68・下 α0.2 で重ねる = 霧の帯を今の舞台の暗さまで沈め、手前の地面は少しだけ沈める。
        /// 戦闘の画面・今の舞台 (幕2/3・stage=old) には足さない (今の舞台は1画素も変えない)。霧の帯を座席のすぐ上へ下げる作業 (W5 の判定) の後に α を見直す
        /// </summary>
        static void MenuShade(RectTransform root)
        {
            var shade = UiKit.NewRect("dio-shade", root);
            UiKit.Stretch(shade, 0f, 0f, 0f, 0f);
            var img = shade.gameObject.AddComponent<Image>();
            img.sprite = UiKit.LinearSprite(ThemeFx.FadeDown(PaperFx.Ground, MenuShadeTop, MenuShadeBottom, "fade-down-ground-menu"), 0.25f);   // Linear: 絵に焼いた α を Gamma と同じ濃さへ (desk-shade と同じ)
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
        }
        /// <summary>MenuShade の上端と下端の濃さ (Gamma の見た目の α)。
        /// 二周目 (2026-10-01 統合): 霧の帯がキャラのすぐ上へ下がり、画面の中ほど (地図の下の段のノードの名前) の後ろが明るくなった
        /// (スマホ相当の地図の「戦闘」の行で淡い字の対比 p95 9.2 → 6.7)。下端 0.2 → 0.4 (画面の縦 51% の α 0.46 → 0.55)</summary>
        const float MenuShadeTop = 0.68f, MenuShadeBottom = 0.4f;

        static void BuildTopBar(GameRoot g, RectTransform root, RunState run, GameState st)
        {
            var bar = UiKit.NewRect("topbar", root);
            UiKit.Anchor(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -TopH), new Vector2(0f, 0f));
            var hg = UiKit.Horz(bar, 12, 0);
            hg.padding = new RectOffset((int)UiKit.Edge, (int)UiKit.Edge, 0, 0);   // 四辺の余白 (2026-09-29 p12: 旧 28)
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;

            string enc = "";
            try
            {
                var node = DeckRogue.Engine.Run.CurrentNode(run);
                if (node != null && node.EncounterId != null) enc = Content.EncounterName(node.EncounterId);
            }
            catch (Exception) { }
            var title = Tag(bar, 40f, -0.6f);
            var tl = UiKit.Txt(title, "幕" + run.Act + "・行" + (run.Row + 1), 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);   // 全角の中黒 (F29)
            tl.characterSpacing = 2f;
            UiKit.Le(tl, -1f, 30f, -1f, 30f);
            var te = UiKit.Deco(title, enc, 19, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(te, -1f, 30f, -1f, 30f);
            var turn = Tag(bar, 32f, 1f);
            // ターン＝砂時計 (2026-09-29 p18: 旧は地図・特性と同じ格子で、墨を掛けると黒い四角だった)。墨1色の版。
            // 札の傾き (+1°) を絵だけ打ち消す (ドットが1°回ると縦に 1px の段差が出る)
            var turnIc = UiKit.Icon(turn, "turn", 16f, PaperFx.InkSoft, true);
            turnIc.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1f);
            var tt = UiKit.Txt(turn, "ターン " + st.Turn, 13, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(tt, -1f, 26f, -1f, 26f);

            var spacer = UiKit.NewRect("spacer", bar);
            UiKit.Le(spacer, 10f, 10f, -1f, -1f, 1f, -1f);
            // 手番の札 (2026-09-15 案C): 「あなたの番」「敵の番 ③ / 3」を上部バーの中央に (レイアウトの外・絶対配置)。
            // 2026-09-29: 演出の的 "phase" に登録し、順送りの間も Presenter が SetPhase で書き換える (敵の番＝夜の札)。組み直しでは弾ませない
            var pt = UiKit.NewRect("phase", root);
            g.RegisterAnchor("phase", pt);
            {
                int pkind = PhaseKindOf(st), pacting = -1;
                if (st.Phase == CombatPhases.AwaitingReaction && st.PendingWindow != null) pacting = st.PendingWindow.EnemyIndex;
                StylePhaseTag(pt, pkind, pacting, st.Enemies.Count, st.Phase == CombatPhases.Lost ? "敗北" : "勝利");
                g.PhaseShownKind = pkind;
            }

            var gold = Tag(bar, 34f, 0f);
            g.RegisterAnchor("gold", gold);   // 盗みの演出の的 (2026-09-17)
            UiKit.Icon(gold, "gold", 16f);
            var gt = UiKit.Deco(gold, run.Gold.ToString(), 18, PaperFx.BrassInk, TextAnchor.MiddleLeft);   // G の数字は真鍮の墨 (color-theme の表。2026-09-29)
            UiKit.Le(gt, -1f, 28f, -1f, 28f);
            var gl = UiKit.Txt(gold, "G", 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(gl, -1f, 28f, -1f, 28f);
            var mana = GearUi.ManaTag(g, bar, run, UiKit.Phone);   // 魔素 (ギアの動力。2026-09-17)。演出の的 "mana"

            // レリック (2026-09-29 上部バーの整理): PC はバーの下の左の列 (RelicRow)。スマホはバーの中に、手番の札の右 24 から G の左までに入る数だけ (溢れは「+N」)。
            // 旧: バーの右の群れに最大8個 = レリックが4個を超えると G・魔素が中央の手番の札の下に潜り、9個目からは黙って消えていた
            if (UiKit.Phone)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gold);
                LayoutRebuilder.ForceRebuildLayoutImmediate(mana);
                float cw = CanvasSize(root).x;
                float fixedW = LayoutUtility.GetPreferredWidth(gold) + 12f + LayoutUtility.GetPreferredWidth(mana) + 12f + PhoneMenuW + UiKit.Edge;   // 右の余白 (p12)
                float room = cw - fixedW - (cw / 2f + PhaseReserveHalf(pt, st.Enemies.Count) + 24f);
                PhoneRelicsInBar(g, bar, run, room);
                RunUi.MenuButton(g, bar);   // スマホは「≡」に畳む (マップ・ログ・メモ・レポート。2026-09-14)
                KeepGoldClearOfPhase(g, bar, root, gold, pt, run);
            }
            else
            {
                RelicRow(g, root, run);
                // PC の右は「マップ・メモ・≡」だけ (2026-09-29): ログとレポートは ≡ (RunUi.Menu) に同じ項目がある。メモはデータ収集の入口なので残す
                var mapBtn = UiKit.Btn(bar, "マップ", delegate { g.ViewMap = !g.ViewMap; g.Rebuild(); }, 13);   // 戦闘中も地図を確かめられる (2026-09-12)
                SetSize(mapBtn, 84f, 34f);
                FeedbackUi.TopBarButtons(g, bar, false, 34f);   // メモ (2026-09-14)
                RunUi.MenuButton(g, bar);   // PC も「≡」: セーブして終了・ランを放棄・戦闘ログ・レポート (2026-09-15)
                KeepGoldClearOfPhase(g, bar, root, gold, pt, run);
            }
            // 夜の札 (ui=night・2026-09-30 P20): 上部バーの札・ボタン・レリックの円 (PC はバーの下の列も)。手番の札は今のまま
            // (自分の番＝紙・敵の番＝夜の札で「明度の反転で読める」＝2026-09-29 C07。夜の上部バーの中では自分の番の紙が1枚だけ明るく立つ)。通知の札は紙のまま
            PaperFx.Nightify(bar);
            PaperFx.Nightify(root.Find("relic-row"));

            // エラー・通知は上部バーの下に (紙の札)
            string msg = g.Error != null ? "! " + g.Error : (g.Notice != null ? g.Notice : null);
            if (msg != null)
            {
                var note = UiKit.NewRect("message", root);
                UiKit.Anchor(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-460f, -TopH - 48f), new Vector2(460f, -TopH - 8f));
                // 通知 (エラーでない) は紙 (濃)＝情報の段の紙で、幅は中身＋48 (2026-09-30 F07: 幅920 の紙 (明) の帯は6割が空きで、舞台でいちばん広い明るい面が視線を奪った)。
                // エラーは今までどおり 920・警告の紙 (旧 生の (1,0.85,0.8)。2026-09-29 p26)
                var nImg = PaperFx.Sheet(note, g.Error != null ? PaperFx.Tag : PaperFx.Tag2, "paper", g.Error != null ? PaperFx.RoseLight : Color.white);
                UiKit.Stretch(nImg.rectTransform, 0f, 0f, 0f, 0f);
                var m = UiKit.Txt(note, msg, 15, g.Error != null ? UiKit.ColBadInk : PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Stretch(m.rectTransform, 12f, 12f, 0f, 0f);
                if (g.Error == null)
                {
                    float nw = Mathf.Min(920f, m.GetPreferredValues(msg, 2000f, 40f).x + 48f);
                    UiKit.Anchor(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-nw / 2f, -TopH - 48f), new Vector2(nw / 2f, -TopH - 8f));
                }
            }
        }

        // ---- 上部バーの手番の札とレリック (2026-09-29 上部バーの整理) ----

        /// <summary>スマホの「≡」の幅 (RunUi.MenuButton と同じ)</summary>
        const float PhoneMenuW = 64f;

        /// <summary>手番の札の種類: 0=自分の番・1=敵の番 (確認の窓・順送りを含む)・2=勝敗</summary>
        static int PhaseKindOf(GameState st)
        {
            if (st.Phase == CombatPhases.PlayerTurn) return 0;
            if (st.Phase == CombatPhases.Won || st.Phase == CombatPhases.Lost) return 2;
            return 1;
        }

        /// <summary>手番の札の文字。敵の番で行動中の敵が分かる時 (2体以上) は丸数字を 20px で「敵の番 ② / 3」</summary>
        static string PhaseText(int kind, int acting, int count, string endLabel)
        {
            if (kind == 0) return "あなたの番";
            if (kind == 2) return endLabel ?? "勝利";
            if (count > 1 && acting >= 0 && acting < Circled.Length) return "敵の番 <size=20>" + Circled[acting] + "</size> / " + count;
            return "敵の番";
        }

        /// <summary>
        /// 手番の札の見た目 (2026-09-29 戦闘画面のレビュー C07): 自分の番・勝敗＝紙 (明) Paper3 に墨の Deco15 (color-theme の表どおり。真鍮は使わない＝
        /// 自分の番の真鍮はターン終了のボタンが担う)。敵の番＝夜の札 (PaperFx.NightTag＝不透明の夜＋紙 (濃) の縁2px) に紙色の文字。明度が反転するので文字を読まなくても分かる。
        /// 札の中の文字 ("label") と Image を差し替えるだけ (Presenter が順送りの間に呼ぶ)
        /// </summary>
        public static void StylePhaseTag(RectTransform pt, int kind, int acting, int count, string endLabel)
        {
            if (pt == null) return;
            var img = pt.GetComponent<Image>();
            if (img == null)
            {
                img = pt.gameObject.AddComponent<Image>();
                img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.raycastTarget = false;
            }
            bool enemy = kind == 1;
            img.sprite = enemy ? PaperFx.NightTag : PaperFx.Tag;
            img.color = enemy ? Color.white : PaperFx.Paper3;
            var lt = pt.Find("label");
            TMP_Text ptx = lt != null ? lt.GetComponent<TMP_Text>() : null;
            if (ptx == null)
            {
                ptx = UiKit.Deco(pt, "", 15, PaperFx.Ink, TextAnchor.MiddleCenter);
                ptx.gameObject.name = "label";
                ptx.characterSpacing = 3f;
                UiKit.Stretch(ptx.rectTransform, 4f, 4f, 0f, 0f);
                ptx.textWrappingMode = TextWrappingModes.NoWrap;
            }
            string text = kind == 2 && endLabel == null ? ptx.text : PhaseText(kind, acting, count, endLabel);
            ptx.text = text;
            ptx.color = enemy ? PaperFx.Paper : PaperFx.Ink;
            float pw = Mathf.Max(150f, ptx.GetPreferredValues(text, 0f, 0f).x + 28f), phh = 32f;
            UiKit.Anchor(pt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-pw / 2f, -TopH / 2f - phh / 2f), new Vector2(pw / 2f, -TopH / 2f + phh / 2f));
        }

        /// <summary>
        /// 順送りの間に手番の札を書き換える (Presenter の TurnEnded・EnemyActionExecuting・TurnStarted。2026-09-29)。
        /// 旧: 札は組み直しの時にしか作られず、敵が行動している数秒間ずっと「あなたの番」のまま (確認の窓の後は「① / 2」のまま②が行動) だった。
        /// 弾み (Tween.Punch 0.1・0.2秒) は種類が変わった時だけ
        /// </summary>
        public static void SetPhase(GameRoot g, int kind, int acting, int count)
        {
            var pt = g != null ? g.Anchor("phase") : null;
            if (pt == null) return;
            bool changed = g.PhaseShownKind != kind;
            StylePhaseTag(pt, kind, acting, count, null);
            g.PhaseShownKind = kind;
            if (changed) Tween.Punch(pt, 0.1f, 0.2f);
        }

        /// <summary>手番の札が取りうる最大の半幅 (敵の番「② / 3」の形。組み直しの後に順送りで書き換わっても、G・レリックの予算が足りるように)</summary>
        static float PhaseReserveHalf(RectTransform pt, int count)
        {
            var lt = pt != null ? pt.Find("label") : null;
            var ptx = lt != null ? lt.GetComponent<TMP_Text>() : null;
            if (ptx == null) return 75f;
            float w = ptx.GetPreferredValues(PhaseText(1, Mathf.Max(0, Mathf.Min(count, Circled.Length) - 1), count, null), 0f, 0f).x + 28f;
            return Mathf.Max(75f, w / 2f);
        }

        /// <summary>レリックの紙の円 (直径 d・絵 art)。残り回数のあるもの (脈打つ欠片) は円の右下に紙 (濃) の小札で回数</summary>
        static RectTransform RelicDisc(Transform parent, RunState run, string id, float d, float art)
        {
            RelicDef rd = null;
            try { rd = Content.GetRelicDef(id); } catch (Exception) { }
            var cell = UiKit.NewRect("relic", parent);
            UiKit.Le(cell, d, d, d, d);
            var disc = cell.gameObject.AddComponent<Image>();
            disc.sprite = PaperFx.Disc(); disc.preserveAspect = true;
            RunUi.RelicArt(cell, id, art);
            int? left = null; if (rd != null) left = Run.RelicChargesLeft(run, id);   // 脈打つ欠片の残り回数 (2026-09-18)
            if (left != null)
            {
                float bs = d >= 40f ? 22f : 20f;
                var badge = UiKit.NewRect("charges", cell);
                // PC (直径 < 40) は円の右下の内側に寄せる (F37: 旧は右へ 8 出て隣の円に触れた)。スマホは今のまま
                if (d < 40f) UiKit.Anchor(badge, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-bs + 2f, -4f), new Vector2(2f, bs - 4f));
                else UiKit.Anchor(badge, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-bs * 0.6f, -5f), new Vector2(bs * 0.4f, bs - 5f));
                var bImg = PaperFx.Sheet(badge, PaperFx.Tag2, "paper");
                UiKit.Stretch(bImg.rectTransform, 0f, 0f, 0f, 0f);
                bImg.raycastTarget = false;
                var bt = UiKit.Txt(badge, left.Value.ToString(), 13, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Stretch(bt.rectTransform, 0f, 0f, 0f, 1f);
            }
            var tip = rd != null ? "<b>" + rd.Name + "</b>" + (left != null ? "  残り" + left + "回" : "") + "\n" + rd.Description : id;
            Tooltip.Attach(cell.gameObject, delegate { return tip; });
            return cell;
        }

        /// <summary>入りきらないレリックの「+N」(紙 (濃) の札)。押すとレリック一覧 (RunUi.RelicViewer。≡ の「レリック一覧」と同じ)</summary>
        static RectTransform RelicMore(GameRoot g, Transform parent, int hidden, int total, float w, float h, int size)
        {
            var chip = UiKit.NewRect("relic-more", parent);
            UiKit.Le(chip, w, h, w, h);
            var img = PaperFx.Sheet(chip, PaperFx.Tag2, "paper");
            UiKit.Stretch(img.rectTransform, 0f, 0f, 0f, 0f);
            img.raycastTarget = true;
            var t = UiKit.Txt(chip, "+" + hidden, size, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            t.gameObject.name = "count";
            UiKit.Stretch(t.rectTransform, 2f, 2f, 0f, 0f);
            var b = chip.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(delegate { Audio.Ui("click"); g.ViewRelics = true; g.ViewDeck = false; g.ViewMap = false; g.MenuOpen = false; g.Rebuild(); });
            if (!UiKit.Phone) Tooltip.Attach(chip.gameObject, delegate { return "<b>レリック一覧（" + total + "個）</b>\n押すと全部を並べて見る"; });
            return chip;
        }

        /// <summary>
        /// PC のレリックの列 (2026-09-29): 上部バーのすぐ下の左 (x Edge〜Edge+460 = 32〜492・y TopH+4〜TopH+40) に直径36の紙の円 (絵32 を等倍・間隔6) で全部並べる
        /// (2026-09-30 F37: 旧・直径28 に絵17 を渡していたが RelicArt が 32 に書き換え、絵が円から出て12個が1本の帯に見えた)。
        /// 入りきらない分 (12個以上) は最後の枡を「+N」にする。通知の札 (x 500〜) と右の戦闘ログ・≡ の窓には掛からない
        /// </summary>
        static void RelicRow(GameRoot g, RectTransform root, RunState run)
        {
            int n = run.Relics.Count;
            if (n == 0) return;
            const float D = 36f, Gap = 6f, W = 460f;
            float X0 = UiKit.Edge;   // 上部バーの左の余白と同じ (2026-09-29 p12: 旧 28)
            var row = UiKit.NewRect("relic-row", root);
            UiKit.Anchor(row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(X0, -TopH - 4f - D), new Vector2(X0 + W, -TopH - 4f));
            var hg = UiKit.Horz(row, (int)Gap, 0);
            hg.childAlignment = TextAnchor.MiddleLeft; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            int fit = Mathf.FloorToInt((W + Gap) / (D + Gap));
            int shown = n <= fit ? n : fit - 1;
            for (int i = 0; i < shown; i++) RelicDisc(row, run, run.Relics[i], D, 32f);
            if (shown < n) RelicMore(g, row, n - shown, n, 40f, D, 13);
        }

        /// <summary>上部バーの帯の下端 (キャンバスの上端から)。PC でレリックの列が出ている時はその下まで (確認の窓を上へ逃がす時の天井)</summary>
        public static float TopBandBottom(GameRoot g)
        {
            bool row = !UiKit.Phone && g != null && g.Rs != null && g.Rs.Relics.Count > 0;
            return row ? TopH + 44f : TopH;   // 列の下端 TopH+40 ＋余白 4 (F37)
        }

        /// <summary>スマホ: バーの中に直径44の円 (絵26) を room に入る数だけ。溢れは「+N」(52×48)</summary>
        static void PhoneRelicsInBar(GameRoot g, Transform bar, RunState run, float room)
        {
            int n = run.Relics.Count;
            if (n == 0) return;
            const float D = 44f, Step = 56f, MoreW = 52f;
            int all = Mathf.FloorToInt((room + 0.01f) / Step);
            int shown = n <= all ? n : Mathf.Max(0, Mathf.FloorToInt((room - MoreW - 12f + 0.01f) / Step));
            for (int i = 0; i < shown; i++) RelicDisc(bar, run, run.Relics[i], D, 26f);
            if (shown < n) RelicMore(g, bar, n - shown, n, MoreW, 48f, 16);
        }

        /// <summary>
        /// 保険 (2026-09-29): バーを組み直した後で、G の札の左端が手番の札の右端＋24 より左に来ていたら、スマホはレリックの円を末尾から「+N」へ畳む。
        /// 予算の計算が合っていれば通らない枝なので、通ったら Debug.LogWarning を出す (撮影で気づけるように)
        /// </summary>
        static void KeepGoldClearOfPhase(GameRoot g, RectTransform bar, RectTransform root, RectTransform gold, RectTransform pt, RunState run)
        {
            float unit = root.lossyScale.x > 0f ? root.lossyScale.x : 1f;
            var c = new Vector3[4];
            for (int guard = 0; guard < 16; guard++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(bar);
                gold.GetWorldCorners(c); float goldLeft = c[0].x;
                pt.GetWorldCorners(c); float phaseRight = c[2].x;
                float gap = (goldLeft - phaseRight) / unit;
                if (gap >= 24f - 0.5f) return;
                if (guard == 0) Debug.LogWarning("[BattleScreen] 上部バー: G の札が手番の札に近すぎる (間 " + gap.ToString("0") + ")");
                if (!UiKit.Phone) return;
                // 末尾の円を1つ外して「+N」を1つ増やす
                RectTransform last = null; RectTransform more = null; int discs = 0;
                for (int i = 0; i < bar.childCount; i++)
                {
                    var ch = bar.GetChild(i) as RectTransform;
                    if (ch == null) continue;
                    if (ch.name == "relic") { last = ch; discs++; }
                    else if (ch.name == "relic-more") more = ch;
                }
                if (last == null) return;
                int sib = last.GetSiblingIndex();
                UnityEngine.Object.DestroyImmediate(last.gameObject);
                int hidden = run.Relics.Count - (discs - 1);
                if (more != null) UnityEngine.Object.DestroyImmediate(more.gameObject);
                var nm = RelicMore(g, bar, hidden, run.Relics.Count, 52f, 48f, 16);
                nm.SetSiblingIndex(sib);
            }
        }

        /// <summary>紙の札 (横並びの入れ物)。少し傾けて手で置いた感じに</summary>
        public static RectTransform Tag(Transform parent, float height, float rot, Color? tint = null)
        {
            var rt = UiKit.NewRect("tag", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = tint.HasValue ? PaperFx.Tag : PaperFx.Tag2; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f;   // 情報の札は紙 (濃)。2026-09-16
            img.color = tint ?? Color.white;
            img.raycastTarget = false;
            var hg = UiKit.Horz(rt, 6, 0);
            hg.padding = new RectOffset(10, 10, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;
            var le = UiKit.Le(rt, -1f, height, -1f, height);
            le.flexibleWidth = 0f;
            var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (rot != 0f) rt.localRotation = Quaternion.Euler(0f, 0f, rot);
            return rt;
        }

        static void Chip(Transform parent, string icon, string text, Color color, int size)
        {
            var tag = Tag(parent, size + 12f, 0f);
            UiKit.Icon(tag, icon, 16f);
            var t = UiKit.Txt(tag, text, size, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 30f, size + 8f, -1f, size + 8f);
        }

        /// <summary>縦レイアウトの中に、横いっぱいに伸びない中央寄せのボタンを置く</summary>
        public static Button CenteredButton(Transform parent, string label, Action onClick, int size, float w, float h, Color? bg = null)
        {
            if (UiKit.Phone) h = Mathf.Max(h, 48f);   // ボタンは SetSize で 48 以上に丸まるので、行も同じ高さに
            var row = UiKit.NewRect("btnrow", parent);
            UiKit.Le(row, -1f, h, -1f, h);
            var hg = UiKit.Horz(row, 0, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
            var b = UiKit.Btn(row, label, onClick, size, true, bg);
            SetSize(b, w, h);
            return b;
        }

        /// <summary>大きさを固定する (min と preferred)。スマホで押せる部品 (Button) は高さ 48 単位以上に丸める
        /// (2026-09-29: UiKit.Btn が保証した 48 をここで上書きしていた＝≡ 56×44・デッキ 110×40 など。CLAUDE.md「指で押せる大きさ＝ボタンは最低 48 単位」)</summary>
        public static void SetSize(Component c, float w, float h)
        {
            if (UiKit.Phone && c != null && c.GetComponent<Button>() != null) h = Mathf.Max(h, 48f);
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.minWidth = w; le.preferredWidth = w; le.minHeight = h; le.preferredHeight = h; le.flexibleWidth = 0f; le.flexibleHeight = 0f;
        }

        // ---- 敵: 帳面の一行 (2026-09-15 ユーザー裁定「案C」。設計は docs/design/battle-v2) ----
        // 敵の名前・HP・意図・状態は頭上の吹き出しでなく、足元の線 (入れ物の下端 = StatusLineY・手札のすぐ上) の紙の札に。頭上には何も置かない。
        //   スマホ 176×76 = 番号＋名前＋ブロック・状態／HP／意図 の3段、PC 210×140 = さらに特性・分岐の一文。幅は隣との間隔で絞る (StripW)。
        //   PC は自分の札と上端をそろえ、下へ伸ばす (2026-09-29 p23 ユーザー裁定「上端でそろえて下へ」＝足元に近づき手札から離れる。PcLedgerTop。自分の札の上端は StripH に固定＝2026-09-30 F19)。スマホは下端が足元の線のまま
        //   狙っている敵は蜂蜜の縁＋頭上の▼ (PC は足元の輪も)、対象の候補は薄い蜂蜜の縁、確認の窓で行動中の敵は明るい縁が脈打つ。
        //   札もタップの的 (絵と同じ = 狙う)。分岐・特性の全文は札のツールチップ (スマホはタップの説明パネル)。
        // 旧: 頭上の吹き出し (奥の敵ほど高い) と名前札・HP バー・状態の札が縦に散っていた。

        public static float StripH { get { return UiKit.Phone ? 76f : 140f; } }
        /// <summary>ledger=feet (HD-2D 見本の変種): 帳面の上端と足元の間 (キャンバス単位。L2 の「16px 以上隠さない」の内側)</summary>
        const float LedgerFeetGap = 10f;
        /// <summary>r3 のスマホの帳面と足元の間 (仕様 §5)</summary>
        const float R3U_LedgerFeetGapPhone = 8f;
        /// <summary>帳面の一行の幅: 隣との間隔に収める (スマホ 間隔−4 を 96〜176・PC 間隔−12 を 116〜210。150 未満は LedgerStrip の narrow = 状態の札は絵だけ)。
        /// 座席を画面上で等間隔にしたので (2026-09-29 p02) 4体でも PC 約204・スマホ約150〜157。1体だけ (ボス) は広く (特性の札も並ぶ)</summary>
        public static float StripW(float neighborGap, bool solo = false)
        {
            // 1体の幕ボスは 420/440 (2026-09-30 F03: 300/320 だと門番の「筋力+2・装甲35・ターン装甲90・激昂」が入らず、絵＋数字に縮んで装甲とターン装甲が読み分けられなかった)
            if (solo) return UiKit.Phone ? 420f : 440f;
            // PC の下限 150 → 116 (2026-09-29 p02 保険): 下限に張り付くと間隔 126 の隣と重なり、左隣の帳面が上に描かれて「④」の番号が隠れていた
            // PC の上限 210→220 (2026-09-30: 状態の札を全員「絵＋数字」にそろえ〔F12〕右の余白を 6 にした〔F15〕ぶん、2〜3体戦で「②泥投げの妖術師」の末尾が欠けた)。4体は間隔で決まる
            return UiKit.Phone ? Mathf.Clamp(neighborGap - 4f, 96f, 176f) : Mathf.Clamp(neighborGap - 12f, 116f, 220f);
        }
        static readonly string Circled = "①②③④⑤⑥⑦⑧";

        /// <summary>敵の入れ物の中身 (絵・狙いの印・帳面の一行)。入れ物 pan は BattleView が持ち越す</summary>
        public static void FillEnemyPanel(GameRoot g, RectTransform pan, GameState st, int index, int shownHp, float neighborGap = float.MaxValue, bool dying = false, bool uniformForecast = false, bool uniformStack = false)
        {
            var e = st.Enemies[index];
            bool alive = e.Hp > 0;
            // 倒した (逃げた) 敵は消える (2026-09-17 ユーザー「倒した敵は消えるようにしたほうが良くない？」): 倒れた瞬間 (dying) だけ絵と帳面を描いて
            // BattleView が崩して消す。以後の組み直しでは何も描かない (的の枡だけ残す = 座席は詰めない)
            if (!alive && !dying) { g.RegisterAnchor("enemy" + index, pan); return; }
            // 狙い: 庇われている敵を狙っても単体の札は護衛に向かう (engine と同じ規則) ので、縁は護衛に付ける (2026-09-29 p01。ギアの狙いは PreferredTarget のまま)
            bool aimed = (g.PreferredTarget >= 0 && GuardRedirect(st, g.PreferredTarget) == index) || (g.Pending != null && g.Pending.TargetIndex.HasValue && g.Pending.TargetIndex.Value == index);
            bool targeting = g.Pending != null && g.Pending.NextNeed() == "target";
            bool acting = st.Phase == CombatPhases.AwaitingReaction && st.PendingWindow != null && st.PendingWindow.EnemyIndex == index;
            int guardian = GuardianOf(st, index);
            bool candidate = targeting && alive && !aimed && (guardian < 0 || guardian == index);   // 庇われている敵は対象の候補にしない (Web の「選べない」と同じ)
            g.RegisterAnchor("enemy" + index, pan);

            EnemyDef def = null;
            try { def = Content.GetEnemyDef(e.EnemyId); } catch (Exception) { }
            string nm = def != null ? def.Name : e.EnemyId;
            // 密度はオクトラ相当 (1ドット=画面4px)。目安の幅は絵のドット数×4 (2026-09-29 p24 ユーザー裁定「1体で戦う幕ボスは128ドットで描き直す」:
            // オーガ・大亀・門番・朧の大鹿・熾を喰う古炉・合成獣の一の相は 128→512px＝このはの約1.7倍。旧は節の種類で固定 (通常 256・エリート 320・ボス 384) で、
            // 128 の絵は round(384/128)=3＝1ドット3px に落ちる)。節の目安より小さい絵は節の値のまま＝仮の絵 (Creature が節の大きさで作る) と
            // ボスの節の 64 ドット。合成獣の二の相・三の相も 2026-09-30 F48 で 128 に描き直した (背丈 103→109→111 ドット＝相ごとに膨らむ。スマホは <id>_96)
            string nodeType = null;
            try { var node = DeckRogue.Engine.Run.CurrentNode(g.Rs); nodeType = node != null ? node.Type : null; } catch (Exception) { }
            float nodeDots = nodeType == MapNodeTypes.Boss ? 384f : nodeType == MapNodeTypes.Elite ? 320f : 256f;
            var artSprite = Creature.Get("enemies", e.EnemyId, false, (int)(nodeDots / 4f));
            // スマホの 128 の幕ボスは 96 の絵 (<id>_96) を 1ドット 2.4 単位＝このはと同じ粒で描く (2026-09-30 F04: 128 を背丈 184 で頭打ちにすると
            // 大きさは前と同じまま 1ドットだけ約 2.35px に細かくなり、このはの 3.15px と粒がそろわなかった)。96 の絵が無い時は今までどおり 128 を頭打ちで
            bool phone96 = false;
            if (UiKit.Phone && artSprite.rect.width >= 128f)
            {
                var s96 = Theme.Art("enemies", e.EnemyId + "_96");
                if (s96 != null) { artSprite = s96; phone96 = true; }
            }
            float artDots = Mathf.Max(nodeDots, artSprite.rect.width * 4f);
            // 64ドット未満の小さな絵 (2026-09-27 技封じの絡繰 48×48) は節の箱へ引き伸ばさず 1ドット=4px のまま (ボス戦の取り巻きがボスと同じ大きさにならない)
            if (artSprite.rect.height < 64f) artDots = artSprite.rect.height * 4f;
            float artTarget = artDots * ArtScale;   // スマホは 0.6 (2026-09-15)
            // スマホは中身の背丈 184 単位で頭打ち (2026-09-29 p24): 足元から頭 (上の透明な余白を除く) まで。今のオーガ (78 ドット×2.4＝187) と同じ背丈で、
            // 128 の幕ボスも大きくしない (上部バーまでの余白は約 12px しか無く、頭上の意図の札が頭に重なる)。スマホは整数倍の規約を外している (PixelScaleF＝目安÷幅) ので縮めてよい。
            // 96 の背の高いボス (巻き上げ機の番人・血族) とボスの節の 64 の仮の絵も同じ背丈にそろう (合成獣の二・三の相は 2026-09-30 F48 から 128＋<id>_96 = phone96 の道)
            // 96 の幕ボス (phone96) は頭打ちにしない＝背の高い門番・大鹿・古炉は意図の札が頭に重なる (IntentTag の逃がし。本家も重なる。2026-09-16 案A)
            if (UiKit.Phone && !phone96)
            {
                float body = artSprite.rect.height - Creature.TopMargin(artSprite);
                if (body > 0f) artTarget = Mathf.Min(artTarget, 184f * artSprite.rect.width / body);
            }
            float feetY = Stage.FeetOffset("enemy" + index, 130f);
            float spriteTop = feetY + artSprite.rect.height * PaperFx.PixelScaleF(artSprite, artTarget);
            float headTop = spriteTop - Creature.TopMargin(artSprite) * PaperFx.PixelScaleF(artSprite, artTarget);   // 絵の上端の透明な余白を除いた頭の上
            bool ph = UiKit.Phone;

            // 対象の候補: 足元に薄い輪 (PC。スマホは足元が札に近いので札の縁だけ)
            if (candidate && !ph)
            {
                var cand = UiKit.NewRect("cand", pan);
                UiKit.Anchor(cand, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-100f, feetY - 14f), new Vector2(100f, feetY + 14f));
                var cImg = cand.gameObject.AddComponent<Image>();
                cImg.sprite = PaperFx.Ring(5); cImg.color = new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.55f); cImg.raycastTarget = false;
                cImg.preserveAspect = false;
            }
            // 絵は舞台 (HD-2D) のビルボードが描く。UI 側の矩形は位置・大きさ・色 (生死/点滅) の基準として残す
            var spr = UiKit.NewRect("sprite", pan);
            PaperFx.FitPixel(spr, artSprite, 0f, feetY, artTarget);
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = artSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;   // 倒れた瞬間も素の色 (白く光ってから崩れる)
            Stage.BindUnit("enemy" + index, spr, img, artSprite);
            bool ledgerAtFeet = Hd2dLayout && (HD2DFlags.Ledger == HD2DLedger.Feet || R3);   // 帳面を足元ごとに浮かせる (HD-2D 見本 ledger=feet。2026-09-30 P20。三周目 r3 の既定 = PC とスマホ)
            if ((aimed || acting) && alive && !ph && !ledgerAtFeet && feetY > StripH + 16f)
            {   // 足元の輪 (PC。スマホは札の上端が足元なので出さない)。頭上の▼は 2026-09-16 に廃止 = 狙いは意図の札と帳面の縁 (真鍮)。
                // 輪の下端が帳面に掛からない時だけ: PC の帳面の上端は自分の札の上端＝StripH (2026-09-30 F19 で固定)
                var honey = acting ? PaperFx.BrassLight : PaperFx.Brass;
                var ring = UiKit.NewRect("ring", pan);
                UiKit.Anchor(ring, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-116f, feetY - 16f), new Vector2(116f, feetY + 14f));
                var rImg = ring.gameObject.AddComponent<Image>();
                rImg.sprite = PaperFx.Ring(6); rImg.color = honey; rImg.raycastTarget = false;
                rImg.preserveAspect = false;
            }

            // 帳面 (入れ物の下端に。幅は隣との間隔で絞る): 名前＋状態／HP (盾は左端)／予告がある時だけ3段目。意図は頭上の札へ (2026-09-16 案A)
            float w = StripW(neighborGap, st.Enemies.Count == 1);
            string forecast = alive ? ForecastLine(st, index, def, e, w) : null;   // r3 も予告の行を出す (2026-10-02 仕様 §5: 割り込みの予告は足元から PC 98px 以内に必ず。段1 の試し撮りは出していなかった)
            // 1体でも3段目がある画面は全員の帳面を同じ高さに (名前の行と HP バーが一直線に並ぶ。3段目の無い帳面は空いた紙のまま。2026-09-29 p01)
            // r3 の PC は 60 (名前＋HP。下の余白 4)・予告ありは全員 88 (スマホは今の 53/79 のまま)
            float h = R3 && !ph ? R3A_LedgerThinH + (forecast != null || uniformForecast ? R3U_ForecastRowH : 0f) : EnemyStripH(forecast != null || uniformForecast);
            var strip = UiKit.NewRect("strip", pan);
            // PC は自分の札と上端をそろえ、下へ伸ばす (2026-09-29 p23 ユーザー裁定「上端でそろえて下へ」): 旧は下端を足元の線 (手札のすぐ上) にそろえていたので、
            // 足元から 120〜230px 離れて手札の 12px 上に乗り「手札の一部」に見えた。敵全員と自分の札の名前の行が1本の線にそろう。スマホは今のまま (下端が線・足元まで約40px)
            float sb = ph ? 0f : PcLedgerTop(h) - h;
            // ledger=feet (変種): 帳面の上端を足元の LedgerFeetGap 下に (足元の線より下へは出さない)。帳面の高さがそろう (uniformForecast) ので名前の行は足元の高さ順に並ぶ
            if (ledgerAtFeet) sb = Mathf.Max(0f, feetY - (R3 && ph ? R3U_LedgerFeetGapPhone : LedgerFeetGap) - h);
            UiKit.Anchor(strip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-w / 2f, sb), new Vector2(w / 2f, sb + h));
            if (EntranceHidesStrips) { var scg = strip.gameObject.AddComponent<CanvasGroup>(); scg.alpha = 0f; scg.blocksRaycasts = false; }   // 見本: 登場の演出の間は隠れて生まれる (直しの輪2・R09)
            LedgerStrip(g, strip, st, index, def, nm, shownHp, w, h, aimed, candidate, acting, forecast, dying);
            if (alive) IntentTag(g, pan, st, index, def, headTop, aimed, candidate, acting, neighborGap, uniformStack);
            // 夜の札 (ui=night): 帳面と意図の札を夜の組へ (旗が無ければ何もしない)
            PaperFx.Nightify(strip);
            PaperFx.Nightify(pan.Find("intent-tag"));
            // 前の表示 (shownHp) から今の HP へ滑らせる (案C への書き換えで落ちていた＝バーが1手遅れて減っていた。2026-09-16 ユーザー報告)
            if (shownHp != e.Hp) TweenHpBar(pan, e.Hp);
        }

        /// <summary>帳面の一行の中身: 番号＋名前 (左) とブロック・状態の札 (右)／HP バー／意図 (絵・実値・ライダー)／(PC) 特性・分岐の一文</summary>
        static void LedgerStrip(GameRoot g, RectTransform strip, GameState st, int index, EnemyDef def, string nm, int shownHp, float w, float h, bool aimed, bool candidate, bool acting, string forecast, bool dying = false)
        {
            var e = st.Enemies[index];
            bool alive = e.Hp > 0;
            if (dying) alive = true;   // 倒れた瞬間の帳面は生前の姿のまま薄れて消える (「（撃破）」は着弾の前に出てしまうので出さない)
            bool ph = UiKit.Phone;
            // 縁: 狙っている=蜂蜜／候補=薄い蜂蜜／行動中 (確認の窓) = 明るい蜂蜜が脈打つ
            if ((aimed || candidate || acting) && alive)
            {
                var edge = PaperFx.Sheet(strip, PaperFx.Tag, "edge", acting ? PaperFx.BrassLight : candidate ? new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.55f) : PaperFx.Brass);
                float o = acting ? -5f : -3f;
                UiKit.Stretch(edge.rectTransform, o, o, o, o);
                edge.raycastTarget = false;
                if (acting)
                {
                    var gimg = edge; float t0 = UnityEngine.Random.value;
                    Tween.Run(1.2f, k => { if (gimg != null) { var c = gimg.color; c.a = 0.75f + 0.25f * Mathf.Sin((k + t0) * Mathf.PI * 2f); gimg.color = c; } }, Ease.Linear, null);
                }
            }
            var paper = PaperFx.Sheet(strip, PaperFx.Tag2, "paper", alive ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f));   // 帳面は紙 (濃)
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = true;
            // 札もタップの的 (絵と同じ = 狙う)。説明は絵と同じツールチップ
            var btn = paper.gameObject.AddComponent<Button>();
            btn.targetGraphic = paper; btn.transition = Selectable.Transition.None; btn.interactable = alive;
            int captured = index;
            btn.onClick.AddListener(delegate { g.OnEnemyClicked(captured); });
            Tooltip.Attach(paper.gameObject, delegate { return EnemyTip(g, captured); });

            float pad = 8f;
            float top = ph ? 2f : 4f, nameH = ph ? 25f : 30f;   // Ellipsis は行の高さが文字の行 (15px≒23・19px≒29) より低いと行ごと落とす
            string num = st.Enemies.Count > 1 && index < Circled.Length ? Circled[index].ToString() : "";
            int nameSize = ph ? 15 : 19;
            bool narrow = w < 150f;   // 4体 (幅 96〜130): 名前の行が狭い
            // 右詰めの札: 状態 (筋力・延焼・急所…) と、1体だけ (ボス) の時は特性 (装甲・とげ・再生…)。ブロックは HP バーの左端の盾 (本家形。2026-09-16 案A)
            // 先に全部集めて幅を見積もり、名前は札の手前で「…」にする (2026-09-29 p01: 旧は名前が帳面の全幅を取り、札の下に潜って末尾が欠けていた)
            // 記号と色 (2026-09-29 p18: 1つの概念に1つの記号。塗りは紙(濃)、役割は墨で分ける＝浮き文字と color-theme の実装5 に揃える):
            // 筋力+＝上の矢印・真鍮の墨／筋力−＝下の矢印・鋼青の墨／威圧＝二重の山形・鋼青の墨／急所＝的・真鍮の墨／混乱＝渦・藤の墨と藤の紙。
            // 1対1の記号が無い札 (アーティファクト・潜伏・朧・封じ) は絵を貸さずに文字だけ (旧: 盾＝ブロックや格子＝ターン/地図の絵を使い回していた)
            var pills = new List<LedgerPillSpec>();
            if (alive)
            {
                if (e.Strength != 0) pills.Add(new LedgerPillSpec(e.Strength > 0 ? "strength_up" : "strength_down", "筋力" + (e.Strength > 0 ? "+" : "") + e.Strength, e.Strength > 0 ? PaperFx.BrassInk : PaperFx.SkyInk));
                if (e.Burn > 0) pills.Add(new LedgerPillSpec("burn", "延焼" + e.Burn, PaperFx.Ink));
                if (e.Exposed > 0) pills.Add(new LedgerPillSpec("exposed", "急所" + e.Exposed, PaperFx.BrassInk));
                if (e.Confusion > 0) pills.Add(new LedgerPillSpec("confuse", "混乱" + e.Confusion, PaperFx.PlumInk, PaperFx.PlumLight));
                if ((e.Weak ?? 0) > 0) pills.Add(new LedgerPillSpec("weak", "威圧" + e.Weak.Value, PaperFx.SkyInk));
                if ((e.Artifact ?? 0) > 0) pills.Add(new LedgerPillSpec(null, "アーティファクト" + e.Artifact.Value, PaperFx.Ink));   // 旧「AF1」は略語で読めなかった (2026-09-20)
                if (e.BurrowActive == true) pills.Add(new LedgerPillSpec(null, "潜伏", PaperFx.Ink));
                if ((e.Slippery ?? 0) > 0) pills.Add(new LedgerPillSpec(null, "朧 残り" + e.Slippery.Value + "回", PaperFx.Ink));   // 2026-09-27 朧の大鹿: HPに届く当たりの残り回数
                if (e.Sealed != null && e.Sealed.Count > 0) pills.Add(new LedgerPillSpec(null, "封じ: " + string.Join("・", System.Linq.Enumerable.Select(e.Sealed, c => c.Def.Name)), PaperFx.Ink));   // 2026-09-27 技封じの絡繰: 倒せば手札に戻る
                // 1体だけ (ボス): 特性の上位3つも名前の行に (2026-09-29 p01: 定義の欄から作る。旧は連結文字列を分割し7字を超える項目〔再生・激昂…〕を捨てていた)
                // 2体以上の特性は3段目 (ForecastLine) に文字で出す
                if (st.Enemies.Count == 1 && def != null)
                {
                    var badges = CardText.EnemyTraitBadges(def);
                    for (int i = 0; i < badges.Count && i < 3; i++)
                        pills.Add(new LedgerPillSpec(badges[i].Icon, badges[i].Label, PaperFx.InkSoft) { Tip = TraitTip(badges[i]) });
                }
            }
            // 幅は TMP で実測する (見積りだと札の幅を 8px ほど多めに取り、名前が1字余計に欠けた)
            string nameText = num + nm + (alive ? "" : (e.Fled == true ? "（逃走）" : "（撃破）"));
            var nameT = UiKit.Deco(strip, nameText, nameSize, alive ? PaperFx.Ink : PaperFx.InkSoft, TextAnchor.MiddleLeft);
            nameT.textWrappingMode = TextWrappingModes.NoWrap; nameT.overflowMode = TextOverflowModes.Ellipsis;
            nameT.characterSpacing = 0f;   // 帳面の名前だけ字間を詰める (Deco 共通の設定は触らない)
            float nameFull = nameT.GetPreferredValues(nameText).x;
            float nameMin = nameT.GetPreferredValues(num + (nm.Length > 2 ? nm.Substring(0, 2) : nm) + "…").x;   // 名前に最低限残す幅 = 番号＋先頭2字＋「…」
            float fullW = 0f, shortW = 0f;
            var iconOnlyW = new float[pills.Count];   // 絵だけの形 (form 2) の1枚の幅 (絵なしの札は文字の形のまま)
            if (pills.Count > 0)
            {
                var meas = UiKit.Txt(strip, "", 13, PaperFx.Ink, TextAnchor.MiddleLeft, true);   // MiniPill の文字と同じ書体・大きさ (UiKit.Txt が MinFontSize に切り上げる)
                meas.textWrappingMode = TextWrappingModes.NoWrap;
                for (int i = 0; i < pills.Count; i++)
                {
                    // MiniPill の実寸: 文字つき = 余白4+絵16+間2+文字+余白4／狭い形 (tight) = 余白2+絵16+間1+文字+余白2。最小 24 (2026-09-29 p18: 絵 14→16)。
                    // 絵なしの札 (1対1の記号が無い札) は 余白4+文字+余白4／狭い形 余白2+文字+余白2 で、狭い形でも言葉のまま (Short＝Text)
                    bool hasIcon = pills[i].Icon != null;
                    float wf = Mathf.Max(24f, (hasIcon ? 26f : 8f) + meas.GetPreferredValues(pills[i].Text).x) + 1f;
                    float ws = string.IsNullOrEmpty(pills[i].Short) ? 24f : Mathf.Max(24f, (hasIcon ? 25f : 8f) + meas.GetPreferredValues(pills[i].Short).x) + 1f;   // 狭い形の右の余白 6 (F15)
                    iconOnlyW[i] = hasIcon ? 24f : ws;
                    float gapW = i > 0 ? 4f : 0f;
                    fullW += gapW + wf; shortW += gapW + ws;
                }
                meas.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(meas.gameObject);
            }
            float inner = w - 2f * pad;
            float cap = inner - 4f - nameMin;   // 札に使ってよい最大の幅
            int form = 0;   // 0 = 文字つき・1 = 絵＋数字 (「-3」)・2 = 絵だけ (全文はツールチップ)
            float reserve = 0f;
            var shown = new List<int>();   // 描く札 (form 0/1 は全部。form 2 は入る札だけ)
            if (pills.Count > 0)
            {
                // 文字つきの形 (form 0) は1体だけ (ボス) の帳面に限る (2026-09-30 F12: 旧は敵ごとに名前の長さで形を選び、同じ画面に「筋力-3」と「-3」が混ざった。
                // スマホは名前を3字まで削って文字を残していた)。2体以上はいつも「絵＋数字」＝全員が同じ形・名前を先に守る。絵の無い札は言葉のまま (Short＝Text)
                bool allowFull = !narrow && st.Enemies.Count == 1;
                if (allowFull && nameFull + 4f + fullW <= inner) { form = 0; reserve = fullW; }
                else if (nameFull + 4f + shortW <= inner) { form = 1; reserve = shortW; }   // 「絵＋数字」にすれば名前が全部入る
                else if (shortW <= cap) { form = 1; reserve = shortW; }   // 状態の札を先に縮めてから名前を切る
                else if (allowFull && fullW <= cap) { form = 0; reserve = fullW; }
                else
                {   // 絵だけ。入る札を順に (入らない札は飛ばす＝絵なしの長い札で後ろの絵が消えない)。少なくとも1枚 (絵のある最初の札)。残りは説明へ
                    form = 2; reserve = 0f;
                    for (int i = 0; i < pills.Count; i++)
                    {
                        float add = (shown.Count > 0 ? 4f : 0f) + iconOnlyW[i];
                        if (reserve + add > cap) continue;
                        reserve += add; shown.Add(i);
                    }
                    if (shown.Count == 0)
                    {
                        int first = pills.FindIndex(p => p.Icon != null);
                        if (first < 0) first = 0;
                        shown.Add(first); reserve = iconOnlyW[first];
                    }
                }
                if (form != 2) for (int i = 0; i < pills.Count; i++) shown.Add(i);
            }
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pad, -top - nameH), new Vector2(-(pad + (reserve > 0f ? reserve + 4f : 0f)), -top));
            if (pills.Count > 0 && reserve > 0f)
            {
                var rightMask = UiKit.NewRect("statusmask", strip);
                UiKit.Anchor(rightMask, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(w - (pad - 2f) - reserve, -top - nameH), new Vector2(-pad + 2f, -top));
                rightMask.gameObject.AddComponent<RectMask2D>();
                var right = UiKit.NewRect("status", rightMask);
                UiKit.Anchor(right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-600f, 0f), new Vector2(0f, 0f));   // 右詰め: 右端を合わせて左へ伸びる (溢れた分は左で切れる)
                var rg = UiKit.Horz(right, 4, 0);
                rg.childAlignment = TextAnchor.MiddleRight; rg.childForceExpandWidth = false; rg.childForceExpandHeight = false;
                foreach (int i in shown)
                {
                    var pl = pills[i];
                    string text = form == 0 ? pl.Text : (form == 1 || pl.Icon == null) ? pl.Short : "";
                    MiniPill(right, pl.Icon, text, pl.Ink, pl.Paper, pl.Tip ?? ChipTip(pl.Text), 13, form != 0);
                }
            }
            // HP バー (2段目)。ブロックがあれば左端に盾 (本家形): 盾の札の右端から 4 手前でバーを始める (盾がバーに 4 重なる)。右端は盾の有無によらず w−pad。
            // 2026-09-29 p04: 旧は半幅を shieldW/2 縮めたうえで両端を +shieldW ずらしていて、右端が帳面の紙の外へ PC 5px・スマホ 3px はみ出していた。
            // スマホの帯も 20 (旧 16: 塗りが 13px しかなく 15 の数字の下が墨の縁に掛かった。帳面は下の余白 6→4 で +2 だけ)
            float barTop = top + nameH + 2f, barH = 20f;
            HpBar(strip, new Vector2(0f, 0f), new Vector2(1f, 0f), h - barTop - barH, h - barTop, shownHp, e.MaxHp, w / 2f - pad, ph ? 13 : 16, alive ? InterruptMarkRatio(def, e) : -1f);
            var bar = strip.Find("hpbar") as RectTransform;
            // 盾の置き場 (順送りの途中でブロックが増減した時に SetEnemyBlockBadge が同じ場所へ出し入れする。2026-09-17)
            var slot = strip.gameObject.AddComponent<BlockSlot>();
            // スマホは盾の下端をバーの下端にそろえ、上へだけ 4 はみ出す (2026-09-30 F18: 上下に 4 ずつだと盾の下の縁が帳面の墨の縁と同じ行に重なり、紙の中に収まらなかった)
            slot.X = pad - 2f; slot.Y = h - barTop - barH - (ph ? 0f : 4f); slot.Size = barH + (ph ? 4f : 8f); slot.Ph = ph; slot.Alive = alive;
            slot.BarLeft0 = -(w / 2f - pad); slot.BarLeftShifted = -w / 2f + ShieldRight(slot) - 4f;
            if (alive && e.Block > 0) { BlockShield(strip, slot.X, slot.Y, slot.Size, e.Block, ph); ShiftBarForShield(bar, slot, true); }
            if (!alive) return;
            // 3段目 (頭上に無い情報がある時だけ): 庇われている／特性 (2体以上)／分岐／割り込みの予告「HP90以下で 攻撃8〜10×2」= 目盛りと同じ札に (2026-09-16 案A・2026-09-29 p01)。
            // 色は部分ごとに ForecastLine が付ける (特性・庇う=墨・分岐=青緑・予告=真鍮の墨)。全文はツールチップ
            if (forecast != null)
            {
                float rowTop = barTop + barH + 2f, rowH = ph ? 26f : 28f;
                var ft = UiKit.Txt(strip, forecast, 13, PaperFx.BrassInk, TextAnchor.MiddleLeft);
                UiKit.Anchor(ft.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pad, -rowTop - rowH), new Vector2(-pad, -rowTop));
                ft.textWrappingMode = TextWrappingModes.NoWrap; ft.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        /// <summary>帳面の名前の行の札1枚 (状態・1体の時の特性)。Short は狭い時の「絵＋数字」の数字 (末尾の符号つきの数。数の無い2字はそのまま・それ以外は絵だけ)。
        /// Icon=null (1対1の記号が無い札) は狭い形でも言葉のまま (Short＝Text)。Ink/Paper は墨と紙 (2026-09-29 p18: 塗りは紙(濃)・役割は墨。混乱だけ藤の紙)</summary>
        class LedgerPillSpec
        {
            public string Icon, Text, Short, Tip; public Color Ink, Paper;
            public LedgerPillSpec(string icon, string text, Color ink, Color? paper = null)
            {
                Icon = icon; Text = text; Ink = ink; Paper = paper ?? PaperFx.Paper2;
                if (icon == null) { Short = text; return; }
                int k = text.Length;
                while (k > 0 && char.IsDigit(text[k - 1])) k--;
                if (k < text.Length && k > 0 && (text[k - 1] == '+' || text[k - 1] == '-')) k--;
                Short = k < text.Length ? text.Substring(k) : (text.Length <= 2 ? text : "");   // 数の無い2字は言葉のまま
            }
        }

        /// <summary>特性の札のツールチップ: 全文 (「再生5 (1ターンに30ダメージ受けると止まる)」) と用語解説</summary>
        static string TraitTip(CardText.TraitBadge b)
        {
            string help;
            if (b.Term != null && KeywordHelp.Terms.TryGetValue(b.Term, out help)) return "<b>" + b.Full + "</b>\n" + help;
            return b.Full;
        }

        /// <summary>庇う (engine の Combat.PlayCard と同じ規則): 単体の札がこの敵に向かうなら -1 以外。自分が護衛なら自分、生きている護衛がいればその番号、いなければ -1</summary>
        public static int GuardianOf(GameState st, int idx)
        {
            if (st == null || idx < 0 || idx >= st.Enemies.Count) return -1;
            if (IsGuardian(st.Enemies[idx])) return idx;
            for (int i = 0; i < st.Enemies.Count; i++)
                if (st.Enemies[i].Hp > 0 && IsGuardian(st.Enemies[i])) return i;
            return -1;
        }

        static bool IsGuardian(EnemyState e)
        {
            try { return Content.GetEnemyDef(e.EnemyId).Guardian == true; } catch (Exception) { return false; }
        }

        /// <summary>単体の札の向かう先: 生きている敵 idx を狙うと、庇われていれば護衛へ (表示と手札の予告を engine の結果に合わせる)</summary>
        public static int GuardRedirect(GameState st, int idx)
        {
            if (st == null || idx < 0 || idx >= st.Enemies.Count || st.Enemies[idx].Hp <= 0) return idx;
            int gi = GuardianOf(st, idx);
            return gi >= 0 ? gi : idx;
        }

        /// <summary>庇われている敵を押した時の一言 (「①盾持ちの従士が庇っている（単体の札は①へ向かう）」)。庇われていなければ null</summary>
        public static string GuardNotice(GameState st, int idx)
        {
            int gi = GuardianOf(st, idx);
            if (gi < 0 || gi == idx) return null;
            string c = gi < Circled.Length ? Circled[gi].ToString() : "";
            string name;
            try { name = Content.GetEnemyDef(st.Enemies[gi].EnemyId).Name; } catch (Exception) { name = st.Enemies[gi].EnemyId; }
            return c + name + "が庇っている（単体の札は" + UiKit.ColorTag(PaperFx.BrassInk, c) + "へ向かう）";
        }

        /// <summary>この敵の帳面に3段目 (特性・庇われている・分岐・割り込みの予告) が出るか。BattleView が全員の帳面の高さをそろえるのに使う (2026-09-29 p01)</summary>
        public static bool HasForecast(GameState st, int index, float neighborGap)
        {
            var e = st.Enemies[index];
            if (e.Hp <= 0) return false;
            EnemyDef def = null;
            try { def = Content.GetEnemyDef(e.EnemyId); } catch (Exception) { }
            return ForecastLine(st, index, def, e, StripW(neighborGap, st.Enemies.Count == 1)) != null;
        }

        /// <summary>帳面の高さ: 名前の行＋HP バー (＋3段目)。スマホ 53/79・PC 64/92 (2026-09-16 案A。旧 76/140 は意図の行と PC の4段目を含んでいた)。
        /// 置き場はスマホ＝下端が足元の線 (StatusLineY)、PC＝上端が自分の札の上端 (PcLedgerTop) で下へ伸ばす (2026-09-29 p23)</summary>
        public static float EnemyStripH(bool forecast)
        {
            bool ph = UiKit.Phone;
            float top = ph ? 2f : 4f, nameH = ph ? 25f : 30f, barH = 20f;   // スマホの帯も 20 (2026-09-29 p04。下の余白 6→4 で帳面は +2 だけ)
            return top + nameH + 2f + barH + (ph ? 4f : 8f) + (forecast ? (ph ? 26f : 28f) : 0f);   // 予告の行は 15px の文字が Ellipsis で落ちない高さ (≥23)
        }

        /// <summary>PC の敵の帳面の上端 (入れ物の下端＝足元の線からの高さ) = 自分の札の上端＝StripH (2026-09-30 F19: 札は上端固定で中身の 120/140 を下で吸収)。帳面の高さ h より下にはしない (2026-09-29 p23)</summary>
        static float PcLedgerTop(float h) { return Mathf.Max(h, StripH); }   // 自分の札の上端は StripH (140) に固定 (2026-09-30 F19)

        /// <summary>
        /// 帳面の3段目 (2026-09-29 p01。PC とスマホで同じ): 頭上に無い情報だけを左から ①庇われている「①が庇う（単体は①へ）」②2体以上なら特性の上位2つ「庇う・とげ2」
        /// ③分岐 (まだ立っていない時)「からくりがあると 先に壊す」④未発火の割り込み (HP半分・被弾覚醒・仲間) の「いつ→何」。無ければ null。
        /// 旧: PC だけ意図の全文 (頭上と同じ内容) を先に積み、ここでしか読めない分岐の文が省略で消えていた
        /// </summary>
        static string ForecastLine(GameState st, int index, EnemyDef def, EnemyState e, float w)
        {
            // 部品ごとに長い順の形を持ち、測って入るまで優先の低い部品から短くする (2026-09-30 F02: 旧は幅を測らず1行につなぎ、
            // 13px の Ellipsis で右端の予告から欠けた＝「庇う　仲間が全滅すると 攻撃…」で数字が消えた)。並びは ①②③④ のまま。
            // 短くする順は ②特性 → ①庇われている → ③分岐 → ④予告 (予告の数字は最後まで残す)。全文はツールチップ
            float avail = w - 16f;   // 帳面の左右の余白 8 ずつ
            var forms = new List<string[]>();
            var inks = new List<Color?>();
            var prio = new List<int>();   // 小さいほど先に短くする
            // ① 庇われている (単体の札は護衛に向かう。敵ギミック第1波の裁定「『庇われている』表示＝窓が嘘をつかない」)
            int gi = GuardianOf(st, index);
            if (gi >= 0 && gi != index)
            {
                string c = gi < Circled.Length ? Circled[gi].ToString() : "";
                forms.Add(new[] { c + "が庇う（単体は" + c + "へ）", c + "が庇う" }); inks.Add(PaperFx.InkSoft); prio.Add(1);
            }
            // ② 特性 (1体だけの時は名前の行の札に出る)。護衛が仲間と一緒にいる時の自分の「庇う」は省く
            // (庇われている側の行に「①が庇う」が出て、狙いの縁も護衛へ移る＝同じことを2か所に書かない。F02)
            if (st.Enemies.Count > 1 && def != null)
            {
                var badges = CardText.EnemyTraitBadges(def);
                var labels = new List<string>();
                for (int k = 0; k < badges.Count; k++)
                {
                    if (badges[k].Term == "庇う" && HasOtherAliveEnemy(st, index)) continue;
                    labels.Add(badges[k].Label);
                }
                if (labels.Count >= 2) { forms.Add(new[] { labels[0] + "・" + labels[1], labels[0] }); inks.Add(PaperFx.InkSoft); prio.Add(0); }
                else if (labels.Count == 1) { forms.Add(new[] { labels[0] }); inks.Add(PaperFx.InkSoft); prio.Add(0); }
            }
            // ③ 分岐 (からくり／人形に反応する。立っている時は頭上に「先に壊す」の札)
            var br = CardText.BranchForms(st, index);
            if (br != null) { forms.Add(br); inks.Add(PaperFx.ManaInk); prio.Add(2); }
            // ④ 割り込みの予告。鎮めの錘 (ギア) で割り込みを止めた敵は豹変しない = 予告を出さない (2026-09-24 Opus ひなた E5)
            if (def != null && def.Interrupts != null && e.InterruptBlocked != true)
            {
                int strength = e.Strength;
                for (int k = 0; k < def.Interrupts.Count; k++)
                {
                    var itr = def.Interrupts[k];
                    if (IndexIn(e.FiredInterrupts, k)) continue;
                    var first = EnemyGraph.FirstMoveOf(def, itr.Goto);
                    string move = first != null ? CardText.MoveShort(first, strength) : null;
                    string when, whenShort;
                    if (itr.On == EnemyInterruptTriggers.HpBelowHalf) { when = "HP" + (e.MaxHp / 2) + "以下で"; whenShort = "HP半分で"; }
                    else if (itr.On == EnemyInterruptTriggers.DamageTaken) { when = "あと" + Math.Max(0, (itr.Amount ?? 0) - (e.DamageTakenTotal ?? 0)) + "ダメージで"; whenShort = when; }
                    else if (itr.On == EnemyInterruptTriggers.Alone) { if (!HasOtherAliveEnemy(st, index)) continue; when = "仲間が全滅すると"; whenShort = "ひとりになると"; }
                    else if (itr.On == EnemyInterruptTriggers.AllyDied) { if (!HasOtherAliveEnemy(st, index)) continue; when = "仲間が倒れると"; whenShort = when; }
                    else continue;
                    var fl = new List<string>();
                    if (move != null) { fl.Add(when + " " + move); if (whenShort != when) fl.Add(whenShort + " " + move); }
                    fl.Add(whenShort + "行動が変わる");
                    fl.Add(whenShort + "変わる");   // スマホの細い帳面 (幅 176) で「ひとりになると行動…」と欠けた分 (2026-09-30 最終の答え合わせ)
                    forms.Add(fl.ToArray()); inks.Add(null); prio.Add(3);
                    break;
                }
            }
            if (forms.Count == 0) return null;
            var lv = new int[forms.Count];
            float sep = EstTextW("　", 13);
            Func<float> total = () =>
            {
                float t = sep * (forms.Count - 1);
                for (int i = 0; i < forms.Count; i++) t += EstTextW(forms[i][lv[i]], 13);
                return t;
            };
            for (int guard = 0; guard < 16 && total() > avail; guard++)
            {
                int pick = -1;
                for (int p = 0; p <= 3 && pick < 0; p++)
                    for (int i = 0; i < forms.Count; i++)
                        if (prio[i] == p && lv[i] + 1 < forms[i].Length) { pick = i; break; }
                if (pick < 0) break;
                lv[pick]++;
            }
            var parts = new List<string>();
            for (int i = 0; i < forms.Count; i++)
            {
                string t = forms[i][lv[i]];
                if (prio[i] == 3) t = "<nobr>" + t + "</nobr>";
                parts.Add(inks[i].HasValue ? UiKit.ColorTag(inks[i].Value, t) : t);
            }
            return string.Join("　", parts.ToArray());
        }

        /// <summary>HP バーの左端の盾 (本家形): 空色の紙の円に盾の絵と数字。ブロックは「盾を差し引いた被ダメ」を HP と同じ場所で読ませる</summary>
        static float BlockShield(RectTransform strip, float x, float y, float size, int block, bool ph)
        {
            var disc = UiKit.NewRect("block", strip);
            UiKit.Anchor(disc, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, y), new Vector2(x + size + 16f, y + size));
            var img = disc.gameObject.AddComponent<Image>();
            img.sprite = PaperFx.Tag; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; img.color = PaperFx.SkyLight; img.raycastTarget = true;
            var hg = UiKit.Horz(disc, 1, 3);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            // 盾は 16px の等倍・墨1色の版 (2026-09-29 p18: 旧 12px は 16 ドットを間引いて雫形の塊だった)。札の幅も +4
            var ic = UiKit.Icon(disc, "shield", 16f, PaperFx.SkyInk, true);
            UiKit.Le(ic, 16f, 16f, 16f, 16f);
            var t = UiKit.Deco(disc, block.ToString(), ph ? 14 : 16, PaperFx.SkyInk, TextAnchor.MiddleCenter);
            UiKit.Le(t, 10f, size - 4f, -1f, size - 4f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            Tooltip.Attach(disc.gameObject, delegate { return ChipTip("ブロック " + block); });
            return x + size + 16f;   // 盾の札の右端 (幅 size+16 は縮めない: 2桁のブロックが盾の絵に食い込む)
        }

        /// <summary>
        /// 盾の置き場 (敵の帳面は LedgerStrip・自分の札は PlayerShieldSlot が載せる)。順送りの途中でブロックが増減した時に同じ場所へ盾を出し入れする。
        /// BarLeft0 / BarLeftShifted = HP バーの offsetMin.x (盾なし／盾あり＝盾の右端−4)。右端 (offsetMax) は動かさない。
        /// Stretched = バーが入れ物いっぱいに引き伸ばし (自分の札)。敵は中央から ±半幅 (2026-09-29 p04: 自分と敵で盾を1つの形に)
        /// </summary>
        public class BlockSlot : MonoBehaviour { public float X, Y, Size, BarLeft0, BarLeftShifted; public bool Ph, Shifted, Alive, Stretched; }

        /// <summary>盾の札の右端 (BlockShield と同じ式。盾がまだ無い時にバーの左端を決める)</summary>
        static float ShieldRight(BlockSlot slot) { return slot.X + slot.Size + 16f; }

        /// <summary>盾の有無で HP バーの左端だけを切り替える (組み立てと順送りの途中で同じ式)。数字の置き場も帯の幅に合わせて直す</summary>
        static void ShiftBarForShield(RectTransform bar, BlockSlot slot, bool on)
        {
            if (slot == null) return;
            slot.Shifted = on;
            if (bar == null) return;
            bar.offsetMin = new Vector2(on ? slot.BarLeftShifted : slot.BarLeft0, bar.offsetMin.y);
            FitHpLabel(bar.GetComponent<HpBarInfo>());
        }

        /// <summary>盾を今の値に出し直す (0 なら消し、HP バーの左端も戻す)。holder = 盾とバーの親 (敵の帳面 strip／自分の札 hp)</summary>
        static void ApplyBlockSlot(RectTransform holder, BlockSlot slot, int block)
        {
            var old = holder.Find("block");
            if (old != null) { old.SetParent(null, false); UnityEngine.Object.Destroy(old.gameObject); }
            var bar = holder.Find("hpbar") as RectTransform;
            if (block > 0)
            {
                if (!slot.Shifted) ShiftBarForShield(bar, slot, true);
                BlockShield(holder, slot.X, slot.Y, slot.Size, block, slot.Ph);
                var disc = holder.Find("block") as RectTransform;
                if (disc != null) Tween.Punch(disc, 0.22f);
            }
            else if (slot.Shifted) ShiftBarForShield(bar, slot, false);
        }

        /// <summary>
        /// 自分の札の盾の置き場 (2026-09-29 p04): 敵と同じ空色の紙の札を HP バーの左端に (盾の右端−4 からバー)。
        /// 旧は水彩のにじみの札 (PlayerBlockBadge) のために HP バーの左を 50〜66 空けていて、ブロック 0 の序盤は被ダメの行と左端がそろわなかった
        /// </summary>
        static void PlayerShieldSlot(RectTransform hpRt, int block, float barH)
        {
            var slot = hpRt.gameObject.AddComponent<BlockSlot>();
            slot.X = -2f; slot.Y = -4f; slot.Size = barH + 8f; slot.Ph = UiKit.Phone; slot.Alive = true; slot.Stretched = true;
            slot.BarLeft0 = 0f; slot.BarLeftShifted = ShieldRight(slot) - 4f;
            if (block > 0) { BlockShield(hpRt, slot.X, slot.Y, slot.Size, block, slot.Ph); ShiftBarForShield(hpRt.Find("hpbar") as RectTransform, slot, true); }
        }

        /// <summary>敵の帳面の盾を今の値に (順送りの敵フェーズ: 防御で得た／攻撃で削れた／敵フェーズの始まりで失効。2026-09-17)。0 なら消し、HP バーの幅も戻す</summary>
        public static void SetEnemyBlockBadge(RectTransform pan, int block)
        {
            if (pan == null) return;
            var strip = pan.Find("strip") as RectTransform;
            var slot = strip != null ? strip.GetComponent<BlockSlot>() : null;
            if (slot == null || !slot.Alive) return;
            ApplyBlockSlot(strip, slot, block);
        }

        /// <summary>自分の札の盾 (HP バーの左の空色の札) を今の値に。0 なら消す (2026-09-17)</summary>
        public static void SetPlayerBlockBadge(RectTransform area, int block)
        {
            if (area == null) return;
            var hp = area.Find("hpwrap/hp") as RectTransform;
            var slot = hp != null ? hp.GetComponent<BlockSlot>() : null;
            if (slot == null) return;
            ApplyBlockSlot(hp, slot, block);
        }

        /// <summary>自分の札の氷壁の文字を今の値に (順送りの途中。無ければ作らない＝組み直しで出る)</summary>
        public static void SetPlayerIceText(RectTransform area, int ice)
        {
            if (area == null) return;
            var wrap = area.Find("hpwrap");
            var t = wrap != null ? wrap.Find("ice") : null;
            var txt = t != null ? t.GetComponent<TMP_Text>() : null;
            if (txt == null) return;
            if (ice <= 0) { t.SetParent(null, false); UnityEngine.Object.Destroy(t.gameObject); return; }
            txt.text = "氷壁 " + ice;
            Tween.Punch(txt.rectTransform, 0.2f);
        }

        /// <summary>
        /// 頭上の意図の札 (2026-09-16 案A・ユーザー「敵の行動は敵の上に表示したほうがわかりやすい」): [意図の絵][数字][rider] を頭のすぐ上の紙 (明るい) の札に。
        /// 狙っている敵は真鍮の縁・行動中 (確認の窓) は明るい真鍮が脈打つ。隣が近い (PC 240・スマホ 170 未満) 時は一段小さく。
        /// 序列 (2026-09-29 p02): このターンにあなたへ働く行動 (攻撃・呪い・盗み・壊し…) は明るい紙で大きく、敵自身に働く行動
        /// (防御・筋力上げ・応援・回復・隙) は紙 (濃) で一段小さく。rider は1行が隣との間隔に収まらない時だけ下の段に。
        /// 上部バーに掛かる時は頭に重ねる (本家も重なる)。分岐の注記・全文はタップ (PC はホバー) の説明に
        /// </summary>
        /// <summary>頭上の意図の札の rider 1枚 (絵・文字・墨・紙)</summary>
        class RiderSpec { public string Icon, Text; public Color Ink, Paper; public bool IconW = true; }

        /// <summary>意図の札の寸法と rider (IntentTag と IntentNeedsStack が同じ式を読む＝見積りと描画をずらさない。2026-09-30 F13)</summary>
        static void IntentMetrics(GameState st, int index, EnemyIntent it, float neighborGap, out bool small, out bool minor, out float isz, out int num, out float h,
            out string shortText, out List<RiderSpec> riders, out float wTop, out float wR, out float gap)
        {
            bool ph = UiKit.Phone;
            bool hidden = st.HideIntents == true;
            // 隣が近い時は一段小さく (2026-09-29 p02: PC にも。旧はスマホの 150 未満だけ = PC の4体で「筋力+2」の札が 50px 重なっていた)
            small = neighborGap < (ph ? 170f : 240f);
            // 敵自身に働く行動 = 一段小さく紙 (濃)。攻撃と、呪い・盗み・壊し・技封じ・山札喰い・逃走・孵化・召喚 (あなたの資源か倒す順の締切に働く) は大きいまま
            minor = !hidden && (it.Kind == "defend" || it.Kind == "buff" || it.Kind == "rally" || it.Kind == "heal" || it.Kind == "rest");
            if (minor) { isz = small ? 22f : (ph ? 26f : 32f); num = small ? 16 : (ph ? 18 : 22); h = small ? 30f : (ph ? 34f : 44f); }
            else if (small) { isz = ph ? 28f : 36f; num = ph ? 22 : 26; h = ph ? 38f : 48f; }
            else { isz = ph ? 32f : 44f; num = ph ? 24 : 32; h = ph ? 42f : 56f; }
            // 夜の札 (HD-2D 見本 ui=night・2026-09-30 P20): 意図の数字は画面で 32px 以上 (門⑨)。スマホは 25 単位 × 1.31 = 32.8px。
            // 一段小さい札 (隣が近い・敵自身の行動) も数字だけは下げない (序列は地の明るさ NightCard/NightCard2 と絵の大きさで残す)。行の高さは数字が入る 48/38 まで
            if (HD2DFlags.UiNight)
            {
                int numMin = ph ? 25 : 32;
                if (num < numMin) num = numMin;
                float hMin = ph ? 38f : 48f;
                if (h < hMin) h = hMin;
            }
            shortText = hidden ? "？" : IntentShort(st, index, it);
            if (it.Kind == "defend" && !hidden) shortText = it.Actual.ToString();
            // rider (状態異常・同時に筋力・同時にブロック・先に壊す)
            riders = new List<RiderSpec>();
            if (!hidden)
            {
                // rider の文字は PC も 15 (2026-09-29 p18: 13 は数字 32 の 0.4 倍で読めなかった。スマホは最小 15 のまま)。
                // 言い方は「この行動で起きること」と帳面の「今の値」を取り違えないように「同時に筋力+1」「同時にブロック5」「あなたに弱体3」(2026-09-14 の文に戻す)。
                // 隣が近い (small＝4体) 札だけ前置きを外す (区別は記号、全文はタップの説明)
                var inflict = Effects.DisplayedInflict(st, it.Inflict);
                if (inflict != null) { string sic = StatusIcon(inflict.Status); riders.Add(new RiderSpec { Icon = sic, Text = (small ? "" : "あなたに") + CardText.StatusName(inflict.Status) + inflict.Amount, Ink = PaperFx.PlumInk, Paper = PaperFx.PlumLight, IconW = sic != null }); }
                if (it.AlsoBuff.HasValue) riders.Add(new RiderSpec { Icon = "strength_up", Text = (small ? "" : "同時に") + "筋力+" + it.AlsoBuff.Value, Ink = PaperFx.BrassInk, Paper = PaperFx.Paper2 });
                if (it.AlsoDefend.HasValue) riders.Add(new RiderSpec { Icon = "shield", Text = (small ? "" : "同時に") + "ブロック" + it.AlsoDefend.Value, Ink = PaperFx.SkyInk, Paper = PaperFx.SkyLight });
                if (it.StrengthPerMilled.HasValue) riders.Add(new RiderSpec { Icon = "strength_up", Text = "食べた分 筋力+" + it.StrengthPerMilled.Value + "/枚", Ink = PaperFx.BrassInk, Paper = PaperFx.Paper2 });
                if (it.AlsoDestroySet == true) riders.Add(new RiderSpec { Icon = "exhaust", Text = "先に壊す", Ink = PaperFx.BadInk, Paper = PaperFx.RoseLight });
            }
            int rsz = Mathf.Max(UiKit.MinFontSize, 15);
            gap = ph ? 6f : 10f;
            wTop = isz + gap + (shortText.Length > 0 ? EstTextW(shortText, num, true) : 0f) + 24f;   // 数字は Deco (字間つき)
            wR = 0f; for (int i = 0; i < riders.Count; i++) wR += MiniPillW(riders[i].Text, rsz, riders[i].IconW) + 6f;
        }

        /// <summary>この敵の意図の札で rider が下の段に回るか (BattleView が全員の段数をそろえる。2026-09-30 F13: 旧は札ごとの幅で決まり、
        /// 3体戦で「2 ×3／同時に筋力+1」の2段と「5 あなたに弱体3」の1段が隣り合った)</summary>
        public static bool IntentNeedsStack(GameState st, int index, float neighborGap)
        {
            if (st == null || index < 0 || index >= st.Enemies.Count) return false;
            var e = st.Enemies[index];
            if (e.Hp <= 0 || e.Intent == null || st.HideIntents == true) return false;
            var it = Effects.EffectiveIntent(st, index) ?? e.Intent;
            if (it == null) return false;
            bool small, minor; float isz, h, wTop, wR, gap; int num; string shortText; List<RiderSpec> riders;
            IntentMetrics(st, index, it, neighborGap, out small, out minor, out isz, out num, out h, out shortText, out riders, out wTop, out wR, out gap);
            return riders.Count > 0 && (small || (UiKit.Phone && minor) || wTop + wR > neighborGap - 8f);
        }

        // ---- 登場の演出の間は意図の札を隠す (2026-09-30 F47 ユーザー裁定「帯を下げ、札は後から」) ----
        // 強個体・幕ボスの名前の帯と舞台の寄り (Stage.Dolly) が終わるまで、意図の札を CanvasGroup の alpha 0 にする。
        // 旧: 寄りの間に 128 ドットの幕ボスの鬣が札の尾に食い込み、名乗りと予告が同時に出ていた。帯の間は入力を塞いでいる (Presenter.ShowEntrance) ので読めなくても困らない。
        // 帯が消える時に Presenter が RevealIntentsAfterEntrance を呼び、0.2 秒で出す (名乗り → 予告の順)。隠している間に組み直しが来ても、新しい札は隠れて生まれる。
        // 保険: 出す合図が来なくても期限 (_entranceHideUntil) を過ぎたら新しい札は隠さない (札が消えたままにならない)
        static bool _entranceHide;
        static float _entranceHideUntil;
        static bool EntranceHidesIntents { get { return _entranceHide && Time.time < _entranceHideUntil; } }
        /// <summary>二周目 直しの輪2 (2026-10-01・R09): 見本 (箱庭) では敵の帳面 (strip) も意図の札と同じく登場の演出の間は隠す。
        /// 新しいカメラで足元が 18px 下がり、足元と帳面の間 (約29px) に高さ 62 の名前の帯が入らず、帯の文字が帳面の名前の行に重なっていた。
        /// 帳面は帯が消えてから意図の札と一緒に出す (F47「札は後から」と同じ作法)。今の舞台 (stage=old) は今までどおり隠さない</summary>
        static bool EntranceHidesStrips { get { return EntranceHidesIntents && Hd2dLayout; } }

        /// <summary>登場の演出の間、いま出ている意図の札 (見本では帳面も) を隠し、以後に組み直した札も隠れて生まれるようにする。maxSeconds は保険の期限</summary>
        public static void HideIntentsForEntrance(GameRoot g, float maxSeconds)
        {
            _entranceHide = true;
            _entranceHideUntil = Time.time + Mathf.Max(0.5f, maxSeconds);
            ForEachIntentTag(g, cg => { cg.alpha = 0f; cg.blocksRaycasts = false; });
            if (Hd2dLayout) ForEachStrip(g, cg => { cg.alpha = 0f; cg.blocksRaycasts = false; });
        }

        /// <summary>登場の演出の終わり: 意図の札 (見本では帳面も) を dur 秒で出す (いま隠れている札だけ。途中で組み直した札は隠さずに生まれる)</summary>
        public static void RevealIntentsAfterEntrance(GameRoot g, float dur)
        {
            _entranceHide = false;
            var groups = new List<CanvasGroup>();
            ForEachIntentTag(g, cg => { if (cg.alpha < 1f) groups.Add(cg); });
            if (Hd2dLayout) ForEachStrip(g, cg => { if (cg.alpha < 1f) groups.Add(cg); });
            if (groups.Count == 0) return;
            var from = new float[groups.Count];
            for (int i = 0; i < groups.Count; i++) from[i] = groups[i].alpha;
            Tween.Run(Mathf.Max(0.01f, dur), k =>
            {
                for (int i = 0; i < groups.Count; i++) if (groups[i] != null) groups[i].alpha = Mathf.Lerp(from[i], 1f, k);
            }, Ease.OutQuad, () =>
            {
                for (int i = 0; i < groups.Count; i++) if (groups[i] != null) { groups[i].alpha = 1f; groups[i].blocksRaycasts = true; }
            });
        }

        /// <summary>画面の敵全員の意図の札 (入れ物の子 "intent-tag") の CanvasGroup (無ければ足す) に act を掛ける</summary>
        static void ForEachIntentTag(GameRoot g, Action<CanvasGroup> act) { ForEachEnemyChild(g, "intent-tag", act); }

        /// <summary>画面の敵全員の帳面 (入れ物の子 "strip") の CanvasGroup (無ければ足す) に act を掛ける (直しの輪2・見本だけ)</summary>
        static void ForEachStrip(GameRoot g, Action<CanvasGroup> act) { ForEachEnemyChild(g, "strip", act); }

        static void ForEachEnemyChild(GameRoot g, string child, Action<CanvasGroup> act)
        {
            var st = g != null && g.Rs != null ? g.Rs.Combat : null;
            if (st == null) return;
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                var pan = g.Anchor("enemy" + i);
                var tag = pan != null ? pan.Find(child) as RectTransform : null;
                if (tag == null) continue;
                var cg = tag.GetComponent<CanvasGroup>();
                if (cg == null) cg = tag.gameObject.AddComponent<CanvasGroup>();
                act(cg);
            }
        }

        static void IntentTag(GameRoot g, RectTransform pan, GameState st, int index, EnemyDef def, float headTop, bool aimed, bool candidate, bool acting, float neighborGap, bool uniformStack = false)
        {
            var e = st.Enemies[index];
            var it = e.Intent != null ? (Effects.EffectiveIntent(st, index) ?? e.Intent) : null;
            if (it == null) return;
            bool ph = UiKit.Phone;
            bool hidden = st.HideIntents == true;
            bool small, minor; float isz, h, wTop, wR, gap; int num; string shortText; List<RiderSpec> specs;
            IntentMetrics(st, index, it, neighborGap, out small, out minor, out isz, out num, out h, out shortText, out specs, out wTop, out wR, out gap);
            bool solo = st.Enemies.Count == 1;
            int rsz = Mathf.Max(UiKit.MinFontSize, 15);
            var riders = new List<Action<Transform>>();
            foreach (var sp in specs) { var c = sp; riders.Add(t => MiniPill(t, c.Icon, c.Text, c.Ink, c.Paper, null, rsz, true)); }
            // rider は1行が隣との間隔に収まらない時だけ下の段へ (旧: スマホは間隔 220 未満なら必ず・PC は決して下ろさなかった)。
            // 画面の誰か1体が下ろすなら全員下ろす (uniformStack。2026-09-30 F13)。スマホの一段小さい札 (防御・筋力上げ) は必ず下ろす
            // (2026-09-30 F16: 数字 18 と rider の文字 15 が1行に並ぶと rider の方が大きく見え、序列が逆になった)
            bool stack = riders.Count > 0 && (small || (ph && minor) || uniformStack || wTop + wR > neighborGap - 8f);
            // 筋力上げは、札が隣との間隔に収まらない時だけ「+N」に縮める (上向きの矢印の絵で筋力と読める。帳面の「筋力-3」と語をそろえるので普段は縮めない)
            float cap = solo ? float.MaxValue : neighborGap - 10f;
            if (it.Kind == "buff" && !hidden && (stack ? Mathf.Max(wTop, wR + 12f) : wTop + wR) > cap)
            {
                shortText = "+" + it.Actual;
                wTop = isz + gap + EstTextW(shortText, num, true) + 24f;
            }
            float w = stack ? Mathf.Max(72f, Mathf.Max(wTop, wR + 12f)) : Mathf.Max(72f, wTop + wR);
            // 1体以外は隣との間隔で頭打ち (隣の札と重ねない)。ただし絵と数字の行は切らない (原則: 意図の絵と数字は切らない)
            if (!solo) w = Mathf.Min(w, Mathf.Max(wTop, cap));
            // 下の段 = rider の札 (MiniPill の高さ スマホ 22・PC 24) ＋上下の余白。PC は札が 24 なので段を 28 に (2026-09-29 p18: 旧 24 の段に 24 の札で下端が吹き出しの縁に乗っていた)
            // 段は 札＋吹き出しの墨の縁 2＋紙の余白 4 (2026-09-30 F14: 旧 24/28 で札の下端が吹き出しの縁に乗り、二重線に見えた)
            float stackH = ph ? 30f : 32f, pillH = ph ? 22f : 24f;
            if (stack) h += stackH;
            // 位置: 頭のすぐ上。上部バー (TopH) に掛かるなら頭に重ねる
            float bottom = headTop + 8f;
            float canvasTopFromLine = CanvasSize(pan).y - BattleView.StatusLineY - TopH - 6f;
            if (bottom + h > canvasTopFromLine) bottom = Mathf.Max(headTop - h * 0.6f, canvasTopFromLine - h);
            var tag = UiKit.NewRect("intent-tag", pan);
            UiKit.Anchor(tag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-w / 2f, bottom), new Vector2(w / 2f, bottom + h));
            if (EntranceHidesIntents) { var hcg = tag.gameObject.AddComponent<CanvasGroup>(); hcg.alpha = 0f; hcg.blocksRaycasts = false; }   // 登場の演出の間は隠れて生まれる (F47)
            if (aimed || candidate || acting)
            {
                var edge = PaperFx.Sheet(tag, PaperFx.Tag, "edge", acting ? PaperFx.BrassLight : candidate ? new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.55f) : PaperFx.Brass);
                float o = acting ? -5f : -3f;
                UiKit.Stretch(edge.rectTransform, o, o, o, o);
                edge.raycastTarget = false;
                if (acting)
                {
                    var gimg = edge; float t0 = UnityEngine.Random.value;
                    Tween.Run(1.2f, k => { if (gimg != null) { var c = gimg.color; c.a = 0.75f + 0.25f * Mathf.Sin((k + t0) * Mathf.PI * 2f); gimg.color = c; } }, Ease.Linear, null);
                }
            }
            var paper = PaperFx.Sheet(tag, minor ? PaperFx.Tag2 : PaperFx.Tag, "paper");   // 敵自身の行動は紙 (濃) = 攻撃の札より一段沈む
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = true;
            var btn = paper.gameObject.AddComponent<Button>();
            btn.targetGraphic = paper; btn.transition = Selectable.Transition.None;
            int captured = index;
            btn.onClick.AddListener(delegate { g.OnEnemyClicked(captured); });
            Tooltip.Attach(paper.gameObject, delegate { return EnemyTip(g, captured); });
            // 尾 (頭へ)
            var tail = UiKit.NewRect("tail", tag);
            float ts = 14f;
            UiKit.Anchor(tail, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-ts / 2f, -ts + 3f), new Vector2(ts / 2f, 3f));
            var tImg = tail.gameObject.AddComponent<Image>();
            // 尾の絵は紙色で焼いてあるので、紙 (濃) の札では Paper2/Paper の比で染めて本体と色を合わせる (墨の縁もわずかに沈むだけ)
            tImg.sprite = PaperFx.BubbleTail(); tImg.raycastTarget = false; tImg.preserveAspect = true;
            tImg.color = minor ? new Color(PaperFx.Paper2.r / PaperFx.Paper.r, PaperFx.Paper2.g / PaperFx.Paper.g, PaperFx.Paper2.b / PaperFx.Paper.b, 1f) : Color.white;
            // 中身: 絵・数字 (・rider)
            float rowH = stack ? h - stackH : h;
            var row = UiKit.NewRect("row", tag);
            UiKit.Anchor(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -rowH), new Vector2(0f, 0f));
            var rg = UiKit.Horz(row, (int)gap, 0);
            rg.childAlignment = TextAnchor.MiddleCenter; rg.childForceExpandHeight = false; rg.childForceExpandWidth = false;
            var intentArt = Theme.Art("icons", "intent_" + it.Kind);
            var ic = UiKit.Icon(row, IntentIcon(it.Kind), isz, intentArt != null ? Color.white : IntentColor(it.Kind));
            if (intentArt != null) { ic.sprite = intentArt; UiKit.PixelArt(ic); }   // 32 ドットを 44/36/28 等で置く＝非整数倍 (2026-09-29 p25)
            ic.rectTransform.sizeDelta = new Vector2(isz, isz); UiKit.Le(ic, isz, isz, isz, isz);
            if (shortText.Length > 0)
            {
                // 筋力上げ・応援の数字は真鍮の墨 (color-theme: 筋力の rider と同じ「紙 (濃)＋真鍮の墨」)
                var itT = UiKit.Deco(row, shortText, num, (!hidden && (it.Kind == "buff" || it.Kind == "rally")) ? PaperFx.BrassInk : PaperFx.Ink, TextAnchor.MiddleLeft);
                if (HD2DFlags.UiNight) { var hm = UiKit.NumHaloNight(itT.font); if (hm != null) itT.fontSharedMaterial = hm; }   // 夜の札の数字の素材 (P21。無ければ素の素材のまま)
                UiKit.Le(itT, 14f, rowH, -1f, rowH);
                itT.textWrappingMode = TextWrappingModes.NoWrap; itT.overflowMode = TextOverflowModes.Overflow;   // 数字は省略記号で切らない
            }
            if (!stack) for (int i = 0; i < riders.Count; i++) riders[i](row);
            else
            {
                var row2 = UiKit.NewRect("riders", tag);
                const float bubbleInk = 2f, riderMargin = 4f;
                float r2 = bubbleInk + riderMargin;   // 吹き出しの墨 2＋余白 4 (F14)
                UiKit.Anchor(row2, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, r2), new Vector2(0f, r2 + pillH));
                var rg2 = UiKit.Horz(row2, 4, 0);
                rg2.childAlignment = TextAnchor.MiddleCenter; rg2.childForceExpandHeight = false; rg2.childForceExpandWidth = false;
                for (int i = 0; i < riders.Count; i++) riders[i](row2);
            }
        }

        /// <summary>帳面の一行の小さな札 (ブロック・状態・ライダー)。高さ 22 (スマホ) / 24 (PC)。tip があればツールチップ。
        /// スマホは文字が 15 に切り上がるので 20→22 (2026-09-29 p16。24 だと頭上の rider の段 22 に収まらない)。
        /// 2026-09-29 p18: 地は細い縁の札 (中墨1px・塗りは paper ちょうど。旧は墨2px の Tag に色を掛けて塗りが紙より暗い #e0cea4 になり、帳面でいちばん重い線だった)、
        /// 絵は 16px の等倍 (旧 14 は 16 ドットを間引いて剣が「✓」に見えた)・墨1色の版。icon=null は絵なし (1対1の記号が無い札)</summary>
        static void MiniPill(Transform row, string icon, string text, Color ink, Color paper, string tip, int size = 13, bool tight = false)   // size は UiKit.Fs で最小に切り上がる
        {
            bool ph = UiKit.Phone;
            var pill = UiKit.NewRect("pill", row);
            var bg = pill.gameObject.AddComponent<Image>();
            bg.sprite = PaperFx.TagThin; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1f; bg.color = paper; bg.raycastTarget = tip != null;
            float h = ph ? 22f : 24f;
            UiKit.Le(pill, 24f, h, -1f, h);
            var hg = UiKit.Horz(pill, tight ? 1 : 2, tight ? 2 : 4);
            // 文字のある狭い札は右の余白を 6 に (2026-09-30 F15: 左右2では最後の字が右の縁に接した。左は絵の透明な縁で広く見える)
            if (tight && !string.IsNullOrEmpty(text)) hg.padding = new RectOffset(2, 6, 2, 2);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            if (icon != null)
            {
                var ic = UiKit.Icon(pill, icon, 16f, ink, true);
                UiKit.Le(ic, 16f, 16f, 16f, 16f);
            }
            if (!string.IsNullOrEmpty(text))
            {   // 絵だけの札 (狭い帳面) は文字の枠を作らない (旧: 空の枠 10 が絵の右に残っていた。2026-09-29 p01)
                var t = UiKit.Txt(pill, text, size, ink, TextAnchor.MiddleCenter, true);
                UiKit.Le(t, 10f, h - 2f, -1f, h - 2f);
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
            var fit = pill.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (tip != null) Tooltip.Attach(pill.gameObject, delegate { return tip; });
        }

        /// <summary>帳面の小さな札の幅の見積り (MiniPill: 余白 2+4 + 絵 16 + 間 2 + 文字 + 余白 4)。icon=false は絵なしの札 (余白 2+4 + 文字 + 余白 4)</summary>
        static float MiniPillW(string text, int size = 13, bool icon = true)
        {
            if (string.IsNullOrEmpty(text)) return 24f;   // 絵だけ (最小幅 24)
            return 4f + (icon ? 16f + 2f : 0f) + EstTextW(text, size) + 6f;   // 文字は UiKit.Txt が MinFontSize (スマホ 15) まで切り上げて描く＝EstTextW も同じ大きさで見積もる
        }

        /// <summary>HP バーに引く「行動が変わる線」の位置 (0〜1)。HP半分の割り込み＝0.5、被弾覚醒＝いまのHPから残りの累計を引いた所。無ければ -1</summary>
        static float InterruptMarkRatio(EnemyDef def, EnemyState e)
        {
            if (def == null || def.Interrupts == null || e.MaxHp <= 0) return -1f;
            if (e.InterruptBlocked == true) return -1f;   // 鎮めの錘で止めた敵は行動が変わらない = 線も引かない (2026-09-24 E5)
            for (int k = 0; k < def.Interrupts.Count; k++)
            {
                var it = def.Interrupts[k];
                if (IndexIn(e.FiredInterrupts, k)) continue;
                if (it.On == EnemyInterruptTriggers.HpBelowHalf) return 0.5f;
                if (it.On == EnemyInterruptTriggers.DamageTaken)
                {
                    int remain = Math.Max(0, (it.Amount ?? 0) - (e.DamageTakenTotal ?? 0));
                    float at = (float)(e.Hp - remain) / e.MaxHp;
                    if (at > 0f && at < 1f) return at;
                }
            }
            return -1f;
        }

        static bool IndexIn(IReadOnlyList<int> list, int k)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++) if (list[i] == k) return true;
            return false;
        }

        static bool HasOtherAliveEnemy(GameState st, int index)
        {
            for (int j = 0; j < st.Enemies.Count; j++) if (j != index && st.Enemies[j].Hp > 0) return true;
            return false;
        }

        /// <summary>文字幅の見積り (レイアウト前に吹き出しの幅を決めるため)。全角 1em・数字と記号 0.6em。
        /// 大きさは実際に描かれる大きさ (UiKit.Fs＝最小 13/15 に切り上げ) で見積もる。deco=Deco の字間 (characterSpacing 2＝0.02em／字) も足す (2026-09-29 p16)</summary>
        static float EstTextW(string s, float size, bool deco = false)
        {
            if (string.IsNullOrEmpty(s)) return 0f;
            size = Mathf.Max(size, UiKit.MinFontSize);
            float w = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                w += (c < 0x80 || c == '×' || c == '＋') ? (char.IsDigit(c) ? 0.58f : 0.62f) : 1.0f;
            }
            if (deco) w += 0.02f * s.Length;
            return w * size;
        }

        /// <summary>小さなライダーの札の幅 (BubblePill compact: 余白 12 + 絵 16 + 3 + 文字 15px)</summary>
        static float CompactPillW(string text) { return 12f + 16f + 3f + EstTextW(text, 15f) + 6f; }

        /// <summary>敵の吹き出し (名前・HP・意図・特性)。用語解説は Tooltip 側が足す</summary>
        public static string EnemyTip(GameRoot g, int index)
        {
            var cur = g.Rs != null ? g.Rs.Combat : null;
            if (cur == null || index >= cur.Enemies.Count) return null;
            var ce = cur.Enemies[index];
            EnemyDef def = null;
            try { def = Content.GetEnemyDef(ce.EnemyId); } catch (Exception) { }
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(def != null ? def.Name : ce.EnemyId).Append("</b>  HP ").Append(ce.Hp).Append(" / ").Append(ce.MaxHp);
            if (ce.Block > 0) sb.Append("  ブロック").Append(ce.Block);
            if (ce.Hp > 0) sb.Append("\n意図: ").Append(CardText.IntentText(cur, index));
            string traits = CardText.EnemyTraits(def, cur, index);
            if (traits.Length > 0) sb.Append("\n特性: ").Append(traits);
            string guard = ce.Hp > 0 ? GuardNotice(cur, index) : null;   // 庇われている (2026-09-29 p01)
            if (guard != null) sb.Append("\n").Append(guard);
            return sb.ToString();
        }

        /// <summary>吹き出しの中の小さな札 (デバフ・筋力・盾の予告)</summary>
        static void BubblePill(Transform row, string icon, string text, Color ink, Color paper, bool compact = false)
        {
            // compact = スマホの小型の吹き出し (2026-09-15): 高さ 28・文字 15
            var pill = UiKit.NewRect("pill", row);
            var bg = pill.gameObject.AddComponent<Image>();
            bg.sprite = PaperFx.Tag; bg.type = Image.Type.Sliced; bg.color = paper; bg.raycastTarget = false;
            float h = compact ? 28f : 34f;
            UiKit.Le(pill, compact ? 44f : 60f, h, -1f, h);
            var hg = UiKit.Horz(pill, 3, 6);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            float isz = compact ? 16f : 18f;
            var ic = UiKit.Icon(pill, icon, isz, ink);
            UiKit.Le(ic, isz, isz, isz, isz);
            var t = UiKit.Txt(pill, text, compact ? 15 : 17, ink, TextAnchor.MiddleCenter, true);
            UiKit.Le(t, 24f, compact ? 22f : 26f, -1f, compact ? 22f : 26f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var fit = pill.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void SmallChip(Transform parent, string icon, string text, Color color, float h = 28f)
        {
            var tag = Tag(parent, h, 0f);
            var img = tag.GetComponent<Image>();
            img.raycastTarget = true;
            bool debuff = text.StartsWith("弱") || text.StartsWith("脆") || text.StartsWith("虚") || text.StartsWith("重") || text.StartsWith("拘") || text.StartsWith("霞");
            // 地は細い縁の札 (中墨1px) に塗りを1回だけ＝頭上の「あなたに弱体3」・帳面の状態の札と同じ作り (2026-09-30 F32: 旧は墨2px の紙(濃) に藤を掛けて
            // 灰桃 #d6c2bb になり、左の色の四角と印が2つ並んだ)。状態異常＝藤の紙・それ以外＝紙(濃)。役割は墨で分ける (浮き文字と同じ対応)
            img.sprite = PaperFx.TagThin; img.pixelsPerUnitMultiplier = 1f;
            img.color = debuff ? PaperFx.PlumLight : PaperFx.Paper2;
            Tooltip.Attach(tag.gameObject, delegate { return ChipTip(text); });
            Color ink = debuff ? PaperFx.PlumInk : text.StartsWith("勢い") ? PaperFx.BrassInk : text.StartsWith("成長") ? PaperFx.GoodInk : PaperFx.Ink;
            if (icon != null) UiKit.Icon(tag, icon, 16f, ink, true);   // 墨1色の版 (旧: 16 ドットの主色×墨で縁と塗りが潰れ黒い塊だった)。icon=null は絵なし (首輪・拘束)
            var t = UiKit.Txt(tag, text, 14, ink, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 20f, h - 2f, -1f, h - 2f);
        }

        /// <summary>札のツールチップ: 用語解説 (KEYWORD_HELP) と残りの数の意味</summary>
        static string ChipTip(string text)
        {
            int sp = text.IndexOf(' ');
            string key = sp > 0 ? text.Substring(0, sp) : text;
            for (int i = key.Length; i > 0; i--)
            {
                string help;
                if (KeywordHelp.Terms.TryGetValue(key.Substring(0, i), out help))
                {
                    string rest = sp > 0 ? text.Substring(sp + 1) : "";
                    return "<b>" + key.Substring(0, i) + "</b>" + (rest.Length > 0 ? " " + rest.Replace("T", "ターン") : "") + "\n" + help;
                }
            }
            return text;
        }

        /// <summary>からくりの見出し (「からくり 1 / 2」) の説明 (2026-09-29 p15): 置き場の説明 (からくり) と、準備と期限の説明 (仕込む) を並べる。
        /// 旧は見出しに説明が付いておらず、Unity の画面から仕込みの仕組みの説明へ届く入口が無かった</summary>
        public static string SetLabelTip(GameState st)
        {
            if (st == null) return null;
            var p = st.Player;
            string tip = ChipTip("からくり " + p.SetCards.Count + " / " + p.SetSlots);
            string help;
            if (KeywordHelp.Terms.TryGetValue("仕込む", out help)) tip += "\n<b>仕込む</b>\n" + help;
            return tip;
        }

        /// <summary>からくりの見出しに説明を付ける (PC はホバー・スマホはタップで上部の固定パネル)。的は見出しの文字の行 (PC)・夜の札 (スマホ)</summary>
        static void AttachSetLabelTip(GameRoot g, GameObject target, Graphic hit)
        {
            if (target == null) return;
            if (hit != null) hit.raycastTarget = true;
            var rt = target.transform as RectTransform;
            if (rt != null) g.RegisterAnchor("setlabel", rt);
            Tooltip.Attach(target, delegate { return SetLabelTip(g.Rs != null ? g.Rs.Combat : null); }, false);
        }

        /// <summary>HP バーの値 (演出で滑らせるために持つ)。Mark = 行動が変わる線の位置 (無ければ -1)・HpOnly = 線を避けて今の HP だけを出している (2026-09-29 p04)。
        /// Loss/LossTo = 自分の HP バーの「削られる分」の帯と、敵の番の後に残る HP の見込み (2026-09-29 p08。敵の HP バーには無い＝Loss が null)。
        /// 帯は LossTo から今の塗りの端 (Now＝滑っている途中の HP) まで＝攻撃が届いて塗りが縮んでも、見込みの左端は動かない</summary>
        public class HpBarInfo : MonoBehaviour { public int Max; public int Value; public float Now; public RectTransform Fill; public TMP_Text Label; public float Mark = -1f; public bool HpOnly; public RectTransform Loss; public int LossTo; }

        /// <summary>入れ物の中の HP バーを target まで滑らせる (無ければ何もしない)</summary>
        public static void TweenHpBar(RectTransform container, int target)
        {
            if (container == null) return;
            var info = container.GetComponentInChildren<HpBarInfo>();
            if (info == null || info.Fill == null) return;
            int from = info.Value;
            info.Value = target;
            float r0 = info.Max > 0 ? Mathf.Clamp01((float)from / info.Max) : 0f;
            float r1 = info.Max > 0 ? Mathf.Clamp01((float)target / info.Max) : 0f;
            var fill = info.Fill; var label = info.Label; int max = info.Max; var hi = info;
            Tween.Run(0.35f, k =>
            {
                if (fill == null) return;
                fill.anchorMax = new Vector2(Mathf.Lerp(r0, r1, k), 1f);
                if (label != null) label.text = Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(from, target, k))) + (hi != null && hi.HpOnly ? "" : " / " + max);
                if (hi != null) { hi.Now = Mathf.Lerp(from, target, k); if (hi.Loss != null) SetLossBand(hi); }   // 削られる分の帯の右端も塗りの端についていく (p08)
            }, Ease.OutCubic);
        }

        /// <summary>
        /// 削られる分の帯 (2026-09-29 p08): 見込みの残り HP (LossTo) から今の塗りの端 (Now) まで。帯は inner の子＝塗りと同じ座標。
        /// 致死 (LossTo ≤ 0) なら塗りの全部で、左端の刻みは出さない。削られる分が無い (Now ≤ LossTo) 時は消す
        /// </summary>
        static void SetLossBand(HpBarInfo info)
        {
            if (info == null || info.Loss == null) return;
            bool on = info.Max > 0 && info.Now > 0f && info.Now > info.LossTo;
            if (info.Loss.gameObject.activeSelf != on) info.Loss.gameObject.SetActive(on);
            if (!on) return;
            float hiR = Mathf.Clamp01(info.Now / info.Max);
            float loR = Mathf.Clamp01((float)info.LossTo / info.Max);
            info.Loss.anchorMin = new Vector2(loR, 0f);
            info.Loss.anchorMax = new Vector2(hiR, 1f);
            info.Loss.offsetMin = Vector2.zero; info.Loss.offsetMax = Vector2.zero;
            var tick = info.Loss.Find("tick");
            if (tick != null) tick.gameObject.SetActive(loR > 0f);
        }

        /// <summary>
        /// HP バー: 墨の縁 2px＋紙 1px の内側 (inner) に薔薇色の塗りと「行動が変わる線」。数字は紙 (明) の下敷きつきの墨 (UiKit.NumHalo)。
        /// 2026-09-29 p04: 塗りを inner の子にして満タンでも右の縁が残る・HP 0 で負の幅の塗りが出ない・線の中心と「ちょうどその HP の塗りの端」がそろう。
        /// 旧 outlineWidth 0.16 の紙の縁取りは Mobile SDF で OUTLINE_ON が入らず一度も描かれていなかった
        /// </summary>
        static void HpBar(RectTransform parent, Vector2 aMin, Vector2 aMax, float yMin, float yMax, int hp, int max, float xHalf = 0f, int textSize = 15, float markRatio = -1f, int predictLoss = 0, int predictFrom = -1)
        {
            var bar = UiKit.NewRect("hpbar", parent);
            // xHalf > 0 なら中央から ±xHalf の固定幅 (スマホの敵は隣との間隔に収める。2026-09-15)
            if (xHalf > 0f) UiKit.Anchor(bar, new Vector2(0.5f, aMin.y), new Vector2(0.5f, aMax.y), new Vector2(-xHalf, yMin), new Vector2(xHalf, yMax));
            else UiKit.Anchor(bar, aMin, aMax, new Vector2(0f, yMin), new Vector2(0f, yMax));
            // 紙の帯 (墨の縁) に薔薇色の水彩
            var edge = bar.gameObject.AddComponent<Image>();
            edge.color = PaperFx.Ink;
            edge.raycastTarget = false;
            var track = UiKit.Pan(bar, PaperFx.Paper, "track");
            UiKit.Stretch(track.rectTransform, 2f, 2f, 2f, 2f);
            track.raycastTarget = false;
            var inner = UiKit.NewRect("inner", bar);
            UiKit.Stretch(inner, 3f, 3f, 3f, 3f);
            float r = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
            var fill = UiKit.NewRect("fill", inner);
            var fimg = fill.gameObject.AddComponent<Image>();
            fimg.sprite = ThemeFx.Gradient("hpfill", Color.Lerp(PaperFx.Rose, Color.white, 0.15f), Color.Lerp(PaperFx.Rose, Color.black, 0.08f));
            fimg.raycastTarget = false;
            UiKit.Anchor(fill, new Vector2(0f, 0f), new Vector2(r, 1f), Vector2.zero, Vector2.zero);
            RectTransform loss = null;
            if (predictLoss > 0)
            {   // 削られる分 (2026-09-29 p08。自分の札だけ): 塗りの端から左へ見込みの HP 損失ぶん、薔薇の薄塗り＋墨の斜線。左端に墨の刻み (上下へ 3 はみ出す)。
                // 数字 (後で作る) はこの上＝紙の下敷きで読める。札を出してブロックが増えるたびに縮む (組み直し)
                loss = UiKit.NewRect("loss", inner);
                var li = loss.gameObject.AddComponent<Image>(); li.color = PaperFx.RoseLoss; li.raycastTarget = false;
                var hatch = UiKit.NewRect("hatch", loss);
                UiKit.Stretch(hatch, 0f, 0f, 0f, 0f);
                var hi = hatch.gameObject.AddComponent<Image>();
                hi.sprite = ThemeFx.Hatch(); hi.type = Image.Type.Tiled; hi.pixelsPerUnitMultiplier = 1f; hi.raycastTarget = false;
                var tick = UiKit.NewRect("tick", loss);
                UiKit.Anchor(tick, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-1f, -3f), new Vector2(1f, 3f));
                var ti = tick.gameObject.AddComponent<Image>(); ti.color = PaperFx.Ink; ti.raycastTarget = false;
            }
            if (markRatio > 0f && markRatio < 1f)
            {   // 行動が変わる線 (HP半分・被弾覚醒): 真鍮の目盛りを帯の上下にはみ出させる (2026-09-16)。狭い札でも「次の一撃で割るか」が読める。
                // inner の子 = 塗りと同じ座標 (HP がちょうど線の値の時に塗りの端と線の中心が一致する)。数字 (後で作る) はこの上に描かれる
                var mark = UiKit.NewRect("mark", inner);
                UiKit.Anchor(mark, new Vector2(markRatio, 0f), new Vector2(markRatio, 1f), new Vector2(-3f, -6f), new Vector2(3f, 6f));
                var mi = mark.gameObject.AddComponent<Image>(); mi.color = PaperFx.Ink; mi.raycastTarget = false;
                var core = UiKit.NewRect("core", mark);
                UiKit.Stretch(core, 1.5f, 1.5f, 1f, 1f);
                var ci = core.gameObject.AddComponent<Image>(); ci.color = PaperFx.Brass; ci.raycastTarget = false;
            }
            var t = UiKit.Txt(bar, Mathf.Max(0, hp) + " / " + max, textSize, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            if (UiKit.NumHalo != null) t.fontSharedMaterial = UiKit.NumHalo;   // 字のすぐ外が紙 (明) になる下敷き (塗りの色は変えない)
            t.alignment = TextAlignmentOptions.Midline;   // Klee One は行の高さがディセンダ込みで、MiddleCenter だと数字が下に寄る
            t.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
            var info = bar.gameObject.AddComponent<HpBarInfo>();
            info.Max = max; info.Value = hp; info.Fill = fill; info.Label = t;
            info.Mark = markRatio > 0f && markRatio < 1f ? markRatio : -1f;
            info.Now = hp;
            // predictFrom = 見込みを数える元の HP (組み直しで塗りが shownHp から今の HP へ滑る時は今の HP。省略は hp)
            info.Loss = loss; info.LossTo = (predictFrom >= 0 ? predictFrom : hp) - Math.Max(0, predictLoss);
            SetLossBand(info);
            FitHpLabel(info);
        }

        /// <summary>HP バーの幅 (中央から ±xHalf の固定幅なら offset から。引き伸ばしなら rect)</summary>
        static float BarWidthOf(RectTransform bar)
        {
            if (bar == null) return 0f;
            if (Mathf.Approximately(bar.anchorMin.x, bar.anchorMax.x)) return bar.offsetMax.x - bar.offsetMin.x;
            return bar.rect.width;
        }

        /// <summary>
        /// 数字を「行動が変わる線」から逃がす (2026-09-29 p04。ボスの「165 / 165」の斜線が線と重なり「165 | 165」と割れて読めた)。
        /// 線が中央の数字 (最大値の桁で測る) から 8 未満に掛かる時だけ、線の右の区間 (入れば。左端には盾が来る) か左の区間の真ん中へ移す。
        /// どちらにも入らなければ今の HP だけを右寄せで出す (HpOnly。最大値は3段目の予告と帯の長さが補う)。帯の幅が変わったら (盾の出し入れ) 呼び直す
        /// </summary>
        static void FitHpLabel(HpBarInfo info)
        {
            if (info == null || info.Label == null) return;
            var t = info.Label;
            var rt = t.rectTransform;
            info.HpOnly = false;
            t.text = Mathf.Max(0, info.Value) + " / " + info.Max;
            t.alignment = TextAlignmentOptions.Midline;
            UiKit.Stretch(rt, 0f, 0f, 0f, 0f);
            if (info.Mark <= 0f || info.Mark >= 1f) return;
            float W = BarWidthOf(info.transform as RectTransform);
            if (W <= 0f) return;
            float mx = 3f + info.Mark * (W - 6f);                                   // 線の中心 (inner の座標を帯の座標へ)
            float tw = t.GetPreferredValues(info.Max + " / " + info.Max).x;          // 桁が減っても位置が動かないよう最大値の桁で測る
            const float gap = 3f + 8f;                                               // 線の半幅＋数字との間 8
            if (Mathf.Abs(mx - W / 2f) >= tw / 2f + gap) return;                    // 中央のままで線に掛からない
            float lA = 10f, lB = mx - gap, rA = mx + gap, rB = W - 8f;             // 左の区間は盾 (帯に 4 重なる) の分を空ける。右は紙の縁まで 5
            Action<float, float, TextAlignmentOptions> place = (a, b, al) =>
            {
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
                rt.offsetMin = new Vector2(a, 0f); rt.offsetMax = new Vector2(b, 0f);
                t.alignment = al;
            };
            if (rB - rA >= tw) { place(rA, rB, TextAlignmentOptions.Midline); return; }
            if (lB - lA >= tw) { place(lA, lB, TextAlignmentOptions.Midline); return; }
            // どちらにも入らない (スマホの狭い札で盾がある時など): 今の HP だけ
            info.HpOnly = true;
            t.text = Mathf.Max(0, info.Value).ToString();
            float hw = t.GetPreferredValues(info.Max.ToString()).x;
            if (rB - rA >= hw || rB - rA >= lB - lA) place(rA, rB, TextAlignmentOptions.MidlineRight);
            else place(lA, lB, TextAlignmentOptions.MidlineRight);
        }

        static string IntentIcon(string kind)
        {
            switch (kind)
            {
                case "attack": return "sword";
                case "defend": return "shield";
                case "buff": case "rally": return "strength_up";   // 2026-09-29 p18: 旧 growth＝あなたの「成長」の記号 (筋力と成長は分ける＝2026-09-02 の用語の裁定)。絵 intent_buff/intent_rally があればそちら
                case "heal": return "heart";
                case "hex": return "burn";
                case "destroy-set": case "destroy-token": return "exhaust";
                case "steal-gold": return "gold";
                case "flee": return "momentum";
                case "mill": return "draw";
                case "seal": return "exhaust";
                case "summon": return "growth";
                case "rest": return "set";
                default: return "exposed";
            }
        }

        /// <summary>状態異常の記号 (2026-09-29 p18: 1つの概念に1つの記号)。弱体＝威圧と同じ「与ダメ -25%」の二重の山形・虚弱＝ひびの盾・
        /// 脆弱と重り＝的 (受ける量が増える＝急所と同じ系)。それ以外 (拘束・負傷・火傷・がらくた) は1対1の記号が無いので null＝文字だけ</summary>
        static string StatusIcon(string status)
        {
            switch (status)
            {
                case "weak": return "weak";
                case "frail": return "frail";
                case "vulnerable": case "slow": return "exposed";
                case "mist": return "draw";   // 霞み＝ドローが減る (自分の札の「霞み NT」と同じ絵)
                default: return null;
            }
        }

        static Color IntentColor(string kind)
        {
            switch (kind)
            {
                case "attack": return PaperFx.Rose;
                case "defend": return UiKit.ColBlock;
                case "buff": case "rally": return PaperFx.Brass;
                case "heal": return UiKit.ColAccent;
                case "rest": return UiKit.ColDim;
                default: return PaperFx.Plum;
            }
        }

        /// <summary>頭上の短い意図: 「12」「12 ×2」「防御 8」など (詳細は IntentText)。攻撃は補正込みのライブ値 (実値公開 2026-09-14)</summary>
        static string IntentShort(GameState st, int index, EnemyIntent it)
        {
            switch (it.Kind)
            {
                case "attack":
                {
                    string s = Effects.DisplayedIntentValue(st, index, it.Kind, it.Actual).ToString();
                    if ((it.Hits ?? 1) > 1) s += " ×" + it.Hits.Value;
                    if (it.MirrorHits == true) s += " ×手数";
                    return s;
                }
                case "defend": return "防御 " + it.Actual;   // 防御の量も頭上に (2026-09-14)
                case "buff": return "筋力+" + it.Actual;
                case "rally": return "応援+" + it.Actual;   // 量も頭上に (3段目の意図の全文を外したので。2026-09-29 p01)
                case "heal": return "回復" + it.Actual;
                case "hex": return "呪い";
                case "destroy-set": return "からくり壊し";
                case "destroy-token": return "人形狩り";   // 「従者」→「人形」(2026-09-24 T3)
                case "steal-gold": return "盗み" + it.Actual + "G";   // 量も頭上に (2026-09-29 p02。CardText.IntentLine と同じ量)
                case "flee": return "逃走";
                case "mill": return "山札喰い" + it.Actual + "枚";
                case "seal": return "技封じ";
                case "summon": return "召喚";
                case "rest": return "隙";
                case "hatch": return "孵化";
                default: return it.Kind;
            }
        }

        // ---- リーダー・伏せ場・置物 ----

        /// <summary>リーダー欄の中身 (入れ物 area は BattleView が持ち越す)</summary>
        // ---- 人形 (白の従者) の舞台の入れ物 (2026-09-19 人形の盤面表示・案A「灯りの列」) ----

        /// <summary>輝き増し・灯り増しの合計 (従者の量つき効果に乗る。Effects.RunPermanentTriggers と同じ式)</summary>
        public static int RetainerBless(GameState st)
        {
            int n = 0;
            foreach (var p in st.Player.Permanents) foreach (var e in p.Def.Effects) if (e.Effect == "blessRetainers") n += e.Amount ?? 0;
            return n;
        }

        /// <summary>足元の札の中身: 最初の量つき効果の絵と数字 (「何が出るか」だけ。いつ出るかはタップの説明)。boosted=輝き増しが乗っている</summary>
        public static bool DollTag(GameState st, CardInstance d, out string icon, out string text, out bool boosted)
        {
            icon = "crest_permanent"; text = ""; boosted = false;
            int bless = RetainerBless(st);
            foreach (var e in d.Def.Effects)
            {
                if (e.Amount == null) continue;
                switch (e.Effect)
                {
                    case "dealDamage": case "dealDamageCleave": case "dealDamageRandom": icon = "sword"; break;
                    case "dealDamagePerLight":
                    {
                        // 灯篭の人形 (2026-09-20 灯と人形の結び): 灯2につきN。いまの灯で読んだ実値 (Effects.cs と同じ式)。灯が足りなければ 0
                        icon = "sword";
                        int lit = (int)System.Math.Floor((st.Player.Light ?? 0) / 2.0) * e.Amount.Value;
                        int litAmount = lit > 0 ? lit + bless : 0;
                        boosted = lit > 0 && bless > 0;
                        text = litAmount + (e.Target == "all" ? "全" : "");
                        return true;
                    }
                    case "gainBlock": case "gainIceBlock": icon = "shield"; break;
                    case "gainHp": icon = "heart"; break;
                    case "drawCards": icon = "draw"; break;
                    case "exposeEnemy": icon = "exposed"; break;
                    case "weakenEnemy": icon = "weak"; break;   // 威圧 = 帳面の威圧と同じ二重の山形 (2026-09-29 p18)
                    default: icon = "crest_permanent"; break;
                }
                // いまの量 = 素の量＋アンセム＋育ち (2026-09-21 人形の灯り。DollUi.EffectAmount = 実処理と同じ式)。育ちかアンセムが乗れば真鍮の数字
                int amount = DollUi.EffectAmount(st, d, e, bless) ?? (e.Amount.Value + bless);
                boosted = amount != e.Amount.Value;
                text = amount + (e.Target == "all" ? "全" : "");
                return true;
            }
            return false;
        }

        /// <summary>人形の入れ物の中身: 絵 (舞台のビルボード) と足元の札。overflow=上限を超えて立てなかった数 (最後の人形の札に「+N」)</summary>
        public static void FillDollPanel(GameRoot g, RectTransform pan, GameState st, CardInstance d, int overflow)
        {
            string key = "doll:" + d.Uid;
            var art = Creature.Get("dolls", d.Def.Id, true, 32);
            // 32 ドット×4px (スマホは 0.6) = ひなたの半分の背丈。大きい人形 (竜・獅子 2026-09-26) は 48 ドットの絵を同じ 4px で＝1.5 倍の背丈
            float artTarget = Mathf.Max(32f, Mathf.Max(art.rect.width, art.rect.height)) * 4f * ArtScale;
            float feetY = Stage.FeetOffset(key, 130f);
            var spr = UiKit.NewRect("sprite", pan);
            PaperFx.FitPixel(spr, art, 0f, feetY, artTarget);
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = art; img.preserveAspect = true; img.raycastTarget = false; img.color = Color.white;
            Stage.BindUnit(key, spr, img, art);
            bool ph = UiKit.Phone;
            // 「人形を1体選ぶ」札の候補・選択中は足元に輪 (敵の対象と同じ作法)
            bool choosing = g.Pending != null && g.Pending.NextNeed() == "permanent" && (g.Pending.Card == null || DollUi.Eligible(st, g.Pending.Card.Def, d));   // 期限なしの人形に継ぎ火は選べない (2026-09-21)
            bool chosen = g.Pending != null && g.Pending.PermanentUid == d.Uid;
            if (choosing || chosen)
            {
                var ring = UiKit.NewRect("ring", pan);
                float rw = ph ? 40f : 64f;
                UiKit.Anchor(ring, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-rw, feetY - (ph ? 8f : 12f)), new Vector2(rw, feetY + (ph ? 8f : 12f)));
                var rImg = ring.gameObject.AddComponent<Image>();
                rImg.sprite = PaperFx.Ring(chosen ? 6 : 5); rImg.color = chosen ? PaperFx.BrassLight : new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.6f); rImg.raycastTarget = false; rImg.preserveAspect = false;
            }
            FillDollTag(pan, st, d, overflow);
        }

        /// <summary>足元の札 (紙(濃)): 絵＋数字＋「・あとN」。輝き増しが乗っていれば真鍮の数字。上限を超えた分 (overflow) は「+N」。
        /// 幅は中身から決める (2026-09-29 p16: 旧は指定 12・10 の文字で固定幅 68 を計算していたが、スマホでは 15 に切り上がって描かれ「あと4」の「4」が札の外の夜に出ていた)。
        /// 高さはスマホ・PC とも 24、絵は 16、数字は 15、「・あとN」は最小の大きさ (スマホ 15・PC 13)。
        /// compact=人形が混んで2段でも並ばない時の短い形: 「・あとN」の文字の代わりに、札の右端に丸い数字 (付箋・からくり・ギアの角の数字と同じ形。残り1は朱・期限なしは ∞)。
        /// 隣の札との重なりと自分の札への潜りは、全部の札を組んだ後に ArrangeDollTags がほどく</summary>
        public static void FillDollTag(RectTransform pan, GameState st, CardInstance d, int overflow, bool compact = false)
        {
            string key = "doll:" + d.Uid;
            float feetY = Stage.FeetOffset(key, 130f);
            string icon, text; bool boosted;
            bool has = DollTag(st, d, out icon, out text, out boosted);
            if (overflow > 0) text = (has ? text + " " : "") + "+" + overflow;
            // 残りの期限 (2026-09-21 人形の灯り): 「あとN」を右に。期限なしは ∞。残り1は朱
            var lifeLeft = DollUi.LifeLeft(st, d);
            string lifeText = (has ? "・" : "") + (lifeLeft == null ? "∞" : "あと" + lifeLeft.Value);
            bool lastTurn = lifeLeft != null && lifeLeft.Value <= 1;
            if (!has && overflow <= 0) { text = ""; }
            float th = DollTagH;
            var tag = UiKit.NewRect("tag", pan);
            tag.anchorMin = tag.anchorMax = new Vector2(0.5f, 0f);
            tag.pivot = new Vector2(0.5f, 0f);
            tag.sizeDelta = new Vector2(40f, th);
            tag.anchoredPosition = new Vector2(0f, feetY - th - 2f);
            var tImg = tag.gameObject.AddComponent<Image>();
            tImg.sprite = PaperFx.Tag2; tImg.type = Image.Type.Sliced; tImg.pixelsPerUnitMultiplier = 1f; tImg.color = Color.white; tImg.raycastTarget = false;
            var hg = UiKit.Horz(tag, 2, 0);
            hg.padding = new RectOffset(4, 4, 0, 0);
            hg.childAlignment = TextAnchor.MiddleCenter; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            const float isz = 16f;   // 12 では剣の絵が 1px の斜線になった
            // 絵は墨1色の版に役割の墨を掛ける (2026-09-29 p18: 旧は色を掛けず、剣 #d9d2c0 と紙(濃) の差が 1.14:1 で縁しか見えなかった)。紋章 (効果の量を持たない人形) は元の絵
            // 心は HP の記号なので薔薇 (2026-09-30 F43: 苔の墨の心は毒・成長とも読め、他の心 (HP・回復の意図・再生) と色が割れた)。数字は回復の浮き文字と同じ良いの墨のまま
            Color iconInk = icon == "shield" ? PaperFx.SkyInk : icon == "heart" ? PaperFx.Rose : icon == "weak" ? PaperFx.SkyInk : PaperFx.Ink;
            bool crest = icon.StartsWith("crest_");
            string drawIcon = icon == "sword" ? "sword_mono" : icon;   // 墨一色で読める剣 (F43)
            var ic = crest ? UiKit.Icon(tag, icon, isz) : UiKit.Icon(tag, drawIcon, isz, iconInk, true);
            UiKit.Le(ic, isz, isz, isz, isz);
            UiKit.PixelArt(ic);   // スマホは 16 ドットを 1.31 倍＝21px (PC は等倍＝最近傍のまま。p25)
            var numColor = boosted ? PaperFx.BrassInk : icon == "shield" ? PaperFx.SkyInk : icon == "heart" ? PaperFx.GoodInk : PaperFx.Ink;
            if (text.Length > 0)
            {
                var t = UiKit.Deco(tag, text, 15, numColor, TextAnchor.MiddleCenter);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                UiKit.Le(t, 10f, th - 4f, -1f, th - 4f);
            }
            else { ic.gameObject.SetActive(false); }
            if (!compact)
            {   // 残りの期限: 最小の大きさ。残り1 (このターンの敵フェーズが終わると消える) は朱
                var lt = UiKit.Deco(tag, lifeText, UiKit.MinFontSize, lastTurn ? PaperFx.BadDown : PaperFx.InkSoft, TextAnchor.MiddleCenter);
                lt.textWrappingMode = TextWrappingModes.NoWrap;
                UiKit.Le(lt, 8f, th - 4f, -1f, th - 4f);
            }
            else
            {   // 短い形: 札の右端に丸い数字 (札の中に収める＝重なりの判定は札の矩形だけで足りる)。
                // 丸は 18 (輪も中に収める)・前の数字との間に 2 の空き・右の余白 4 (2026-09-30 F44: 旧は輪が 23 で高さ24 の札の縁の線に乗り、「2③」が「23」に読めた)
                var gapR = UiKit.NewRect("gap", tag);
                UiKit.Le(gapR, 2f, 2f, 2f, 2f);
                var disc = UiKit.NewRect("life", tag);
                UiKit.Le(disc, 18f, 18f, 18f, 18f);
                LifeDisc(disc, lifeLeft);
            }
            var fit = tag.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(tag);   // 幅をこの場で確定する (ArrangeDollTags と確認の窓の DollTagsTop が読む)
            PaperFx.Nightify(tag);   // 夜の札 (ui=night・2026-09-30 P20。舞台の上に常に出る札＝人形の足元の札も)
        }

        /// <summary>人形の残りの期限の丸い数字 (付箋の挿絵の角・足元の札の短い形)。紙 (濃) の丸＋墨の輪＋数字 15。残り1は朱・期限なしは ∞ (2026-09-29 p16)</summary>
        static void LifeDisc(RectTransform badge, int? left)
        {
            bool last = left != null && left.Value <= 1;
            var bImg = badge.gameObject.AddComponent<Image>();
            bImg.sprite = PaperFx.Disc(); bImg.preserveAspect = true; bImg.raycastTarget = false;
            // Disc は紙色で焼いてあるので Paper2/Paper の比で染める (意図の札の尾と同じ)
            bImg.color = new Color(PaperFx.Paper2.r / PaperFx.Paper.r, PaperFx.Paper2.g / PaperFx.Paper.g, PaperFx.Paper2.b / PaperFx.Paper.b, 1f);
            // 輪は丸の内側に収める＝渡された矩形が見た目の大きさ。太さ 8 (約1px の実線。旧 4 は 20px に縮めると 0.6px で途切れて点線に見えた)。F44
            var bRing = UiKit.NewRect("ring", badge);
            UiKit.Stretch(bRing, 0f, 0f, 0f, 0f);
            var rImg = bRing.gameObject.AddComponent<Image>();
            rImg.sprite = PaperFx.Ring(8); rImg.color = last ? PaperFx.BadInk : PaperFx.Ink; rImg.raycastTarget = false; rImg.preserveAspect = true;
            var btx = UiKit.Deco(badge, left == null ? "∞" : left.Value.ToString(), 15, last ? PaperFx.BadDown : left == null ? PaperFx.InkSoft : PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Stretch(btx.rectTransform, -2f, -2f, 0f, 0f);
            btx.characterSpacing = 0f; btx.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>人形の足元の札の高さ (スマホ・PC とも。2026-09-29 p16)</summary>
        public const float DollTagH = 24f;

        /// <summary>
        /// 人形の足元の札を並べ直す (2026-09-29 p16。BattleView が人形の札を全部組んだ後に呼ぶ)。座席の順に見て:
        /// ①PC は札の下端を自分の札 (PcSelfStrip) の上端＋4 で止める (横に掛かる札だけ。旧は手前の座席の札が自分の札の上端に約5px 潜っていた)。
        /// ②先に置いた札と横にも縦にも重なれば、置ける高さのうち元の位置に近い所へ動かす。下 (足元の下は手札の線まで空いている) を先に、
        /// 上 (人形の足に掛かる) は1段 (札の高さ＋4) までで、上へ動かすのは下の 1.6 倍の遠さと見る。
        /// どこにも置けない札が1枚でもあれば false (呼ぶ側が短い形に組み直してもう一度並べる)
        /// </summary>
        public static bool ArrangeDollTags(RectTransform area, bool lastResort = false)
        {
            if (area == null) return true;
            bool ph = UiKit.Phone;
            var tags = new List<RectTransform>(); var pans = new List<RectTransform>(); var order = new List<int>();
            for (int i = 0; i < area.childCount; i++)
            {
                var pan = area.GetChild(i) as RectTransform;
                if (pan == null || !pan.gameObject.activeInHierarchy) continue;
                if (pan.GetComponent<Button>() is Button b && !b.interactable) continue;   // 崩れかけの人形 (KillDoll が札を捨てた)
                var tag = pan.Find("tag") as RectTransform;
                if (tag == null || !tag.gameObject.activeInHierarchy) continue;
                var info = pan.GetComponent<BattleView.DollInfo>();
                int o = info != null ? info.Order : 99, k = 0;
                while (k < order.Count && order[k] <= o) k++;
                tags.Insert(k, tag); pans.Insert(k, pan); order.Insert(k, o);
            }
            bool ok = true;
            var placed = new List<Rect>();
            // r3 (2026-10-02 仕様 §7): 足元の帳・匣 (スマホは足元の帳・上の帯) を障害物として先に置く (保険。足元の帳の右端の上限で普通は掛からない)。
            // 障害物は札としては数えない (placed に入れるだけ)
            if (R3) foreach (var sr in R3U_SelfRects) placed.Add(sr);
            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i]; var pan = pans[i];
                float pcx = (pan.offsetMin.x + pan.offsetMax.x) / 2f, py = pan.offsetMin.y;
                float w = tag.rect.width, h = tag.rect.height;
                var r = new Rect(pcx + tag.anchoredPosition.x - w / 2f, py + tag.anchoredPosition.y, w, h);
                // 床: 手札の線の上 2。PC で自分の札に横が掛かる札は、自分の札の上端＋4
                float floor = BattleView.StatusLineY + 2f;
                if (!ph && !R3)
                {   // r3 は床を足元の線＋2 のまま (足元の帳は人形の座席の左にあるので床にならない＝障害物として上で避ける。仕様 §7)
                    float selfRight = SelfStripRight > 0f ? SelfStripRight : float.MaxValue;
                    if (r.xMin < selfRight && r.xMax > UiKit.Edge) floor = BattleView.StatusLineY + StripH + 4f;   // 札の上端は StripH に固定 (2026-09-30 F19)
                }
                if (r.yMin < floor) r.y = floor;
                if (DollTagHits(r, placed))
                {
                    float y0 = r.y, best = y0, bestCost = float.MaxValue;
                    var cands = new List<float>();
                    for (int j = 0; j < placed.Count; j++)
                        if (r.xMin < placed[j].xMax + 2f && r.xMax > placed[j].xMin - 2f) { cands.Add(placed[j].yMin - h - 2f); cands.Add(placed[j].yMax + 2f); }
                    for (int c = 0; c < cands.Count; c++)
                    {
                        float y = cands[c];
                        if (y < floor || y > y0 + h + 4f) continue;
                        var test = new Rect(r.x, y, w, h);
                        if (DollTagHits(test, placed)) continue;
                        float cost = y <= y0 ? y0 - y : (y - y0) * 1.6f;
                        if (cost < bestCost) { bestCost = cost; best = y; }
                    }
                    // 見本 (stage=diorama) の最後の手 (直しの輪1 2026-10-01。短い形で並べ直す2回目 lastResort だけ): 1段上でも置けない札は、2段上まで・半札ぶん横にずらした置き場も探す。
                    // 低いカメラ (22°・7°) では人形が横に詰まり、1段では3組が重なった (PH 人形9体の L6)。今の舞台は通らない (1画素も変えない)
                    if (bestCost == float.MaxValue && lastResort && HD2DFlags.StageMode == HD2DStage.Diorama)
                    {
                        float x0 = r.x, bestDx = 0f;
                        float[] dxs = { 0f, w * 0.5f, -w * 0.5f };
                        foreach (var dx in dxs)
                        {
                            var ys = new List<float> { y0 };
                            for (int j = 0; j < placed.Count; j++)
                                if (x0 + dx < placed[j].xMax + 2f && x0 + dx + w > placed[j].xMin - 2f) { ys.Add(placed[j].yMin - h - 2f); ys.Add(placed[j].yMax + 2f); }
                            for (int c = 0; c < ys.Count; c++)
                            {
                                float y = ys[c];
                                if (y < floor || y > y0 + 2f * (h + 4f)) continue;
                                if (DollTagHits(new Rect(x0 + dx, y, w, h), placed)) continue;
                                float cost = (y <= y0 ? y0 - y : (y - y0) * 1.6f) + Mathf.Abs(dx) * 2f;
                                if (cost < bestCost) { bestCost = cost; best = y; bestDx = dx; }
                            }
                            if (bestCost < float.MaxValue) break;
                        }
                        if (bestCost < float.MaxValue && bestDx != 0f)
                        {
                            r.x = x0 + bestDx;
                            tag.anchoredPosition = new Vector2(tag.anchoredPosition.x + bestDx, tag.anchoredPosition.y);
                        }
                    }
                    if (bestCost == float.MaxValue) ok = false;
                    r.y = best;
                }
                placed.Add(r);
                tag.anchoredPosition = new Vector2(tag.anchoredPosition.x, r.y - py);
            }
            return ok;
        }

        static bool DollTagHits(Rect r, List<Rect> placed)
        {
            for (int j = 0; j < placed.Count; j++)
                if (r.xMin < placed[j].xMax + 2f && r.xMax > placed[j].xMin - 2f && r.yMin < placed[j].yMax + 1f && r.yMax > placed[j].yMin - 1f) return true;
            return false;
        }

        /// <summary>人形の説明 (タップ／ホバー): 名前・本文・いまの値 (輝き増し込み)・壊れる条件</summary>
        public static string DollTip(GameRoot g, string uid)
        {
            var cur = g.Rs != null ? g.Rs.Combat : null;
            if (cur == null) return null;
            CardInstance d = null;
            foreach (var p in cur.Player.Permanents) if (p.Uid == uid) { d = p; break; }
            if (d == null) return null;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(d.Def.Name).Append("</b>  <size=80%>人形 (置物)</size>\n").Append(CardText.Body(d.Def));
            int bless = RetainerBless(cur);
            if (bless > 0) sb.Append("\n").Append(UiKit.ColorTag(PaperFx.BrassInk, "輝き増しで +" + bless));
            // 灯り (2026-09-21): 残りと育ち
            var left = DollUi.LifeLeft(cur, d);
            int grow = DollUi.Growth(cur, d);
            // 語彙 (2026-09-22 友人ラン): 寿命は「期限」。資源の「灯」と同じ字を使わない＝「灯が減ると人形が消える」と読ませない
            sb.Append("\n").Append(UiKit.ColorTag(PaperFx.BrassInk, "出した瞬間に1回動く（点灯）。" + (left == null ? "期限なし（消えない）" : "あと" + left.Value + "ターンで消える（出したターンを含む）")));
            // 火勢はダメージ・ブロックを持つ人形だけ (2026-09-24 Opus ひなた E8: 灯篭の人形の「灯2につき1」・手当て・灯芯には乗らない)
            sb.Append("\n").Append(UiKit.ColorTag(PaperFx.InkSoft, (DollUi.HasGrowth(d.Def) ? "出してから1ターンごとにダメージとブロック+1（いま火勢+" + grow + "）。" : "") + "写し灯などで写すと残りの期限も写す。灯（資源）とは別＝灯が減っても消えない"));
            sb.Append("\n").Append(UiKit.ColorTag(PaperFx.InkSoft, "敵の「人形狩り」で壊れる。灯の捧げの対価に選べる"));   // 用語の見出し「人形狩り」にそろえる (2026-09-24 T3)
            if (g.Pending != null && g.Pending.NextNeed() == "permanent")
            {
                bool ok = g.Pending.Card == null || DollUi.Eligible(cur, g.Pending.Card.Def, d);
                sb.Append(ok ? "\n<b>押すとこの人形を選ぶ</b>" : "\n" + UiKit.ColorTag(PaperFx.BadInk, "期限なしの人形には使えない"));
            }
            return sb.ToString();
        }

        /// <param name="selfArea">自分の札を置く入れ物 (HD-2D 見本の箱庭では area と同じ矩形の兄弟＝被弾の押し縮みが札に掛からない。BattleView.SyncPlayerSelf)。null なら area</param>
        public static void FillPlayerPanel(GameRoot g, RectTransform area, GameState st, int shownHp, RectTransform selfArea = null)
        {
            var p = st.Player;
            var self = selfArea != null ? selfArea : area;

            string leaderId = g.Rs.LeaderId;

            var spr = UiKit.NewRect("sprite", area);
            var leaderArt = Creature.Get("leaders", leaderId, true);
            float pFeet = Stage.FeetOffset("player", 130f);
            float pArt = leaderArt.rect.width * 4f * ArtScale;   // 1ドット=4px を絵の幅に寄らず保つ (2026-09-16 このは v2 は 88×64=斧ぶん横に広い)。スマホは 0.6
            float pTop = pFeet + leaderArt.rect.height * PaperFx.PixelScaleF(leaderArt, pArt);
            PaperFx.FitPixel(spr, leaderArt, 0f, pFeet, pArt);
            spr.anchorMin = spr.anchorMax = new Vector2(0f, 0f);
            spr.offsetMin += new Vector2(130f, 0f); spr.offsetMax += new Vector2(130f, 0f);
            var img = spr.gameObject.AddComponent<Image>();
            img.sprite = leaderArt;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Stage.BindUnit("player", spr, img, leaderArt);

            // 自キャラの名前札は出さない (2026-09-11 ユーザー「スマホ表示だとキャラと名前が被ってキャラがよく見えなくなる。
            // 自キャラ名表示は不要なのでは？」)。誰を操作しているかはセットアップとラン画面で分かるので、戦場では絵を優先する。
            // 敵の名前札は「どれを狙うか」の識別に要るので据え置き
            // 自分の札 (帳面の一行の左端。2026-09-15 案C): HP・被ダメ予測・資源を、敵の札と同じ足元の線に置く
            R3U_SelfRects.Clear();
            R3U_FootRight = -1f;
            R3U_FootHpRight = -1f;
            R3U_PhoneFootRect = default(Rect);
            if (UiKit.Phone) PhoneSelfColumn(g, self, st, shownHp);   // r3 の枝は中 (足元の帳＝HP・見込み・からくり／上の帯＝状態・ギア・置物)
            else if (R3) { R3U_PcFootLedger(g, self, st, shownHp); R3U_PcBox(g, self, st); }   // 三周目 r3 (2026-10-02 仕様 §6-1・§6-2): 足元の帳と左下の匣
            else PcSelfStrip(g, self, st, shownHp);
            // 夜の札 (ui=night・2026-09-30 P20): 自分の札 (PC は hpwrap の中にからくり・ギア・置物。スマホは左下の札と上の帯の3区画。r3 の PC は足元の帳と匣)
            PaperFx.Nightify(self.Find("box"));
            PaperFx.Nightify(self.Find("chips"));
            PaperFx.Nightify(self.Find("hpwrap"));
            PaperFx.Nightify(self.Find("setzone"));
            PaperFx.Nightify(self.Find("gearzone"));
            PaperFx.Nightify(self.Find("perms"));
        }

        /// <summary>
        /// 受けるダメージの見込み (2026-09-29 p08。旧「被ダメ 17 − 盾 5 ＝ HP −12 → 59」の1行は、判断に使う結論がいちばん小さく、スマホは「＝」の後で折れていた):
        /// 結論を先に大きく (Kaisei Decol)「HP 80 → 69（−11）」、内訳を後に小さく (Klee One)「受けるダメージ 11 − ブロック 5」。
        /// 1つの TMP の中でフォントは変えられないので2つに分ける (head = "incoming"・detail = "incoming-detail")。
        /// ブロックは氷壁も足す (Web の App.tsx と同じ式)。HP バーの削られる分の帯 (PredictedLoss) と同じ数を言う
        /// </summary>
        static IncomingInfo IncomingBlock(RectTransform parent, GameState st, bool phone)
        {
            var p = st.Player;
            int incoming = 0;
            try { incoming = Effects.IncomingTotal(st); } catch (Exception) { }
            var head = UiKit.Deco(parent, "", phone ? 18 : 20, PaperFx.Ink, TextAnchor.MiddleLeft);
            head.name = "incoming";
            head.textWrappingMode = TextWrappingModes.NoWrap;
            head.overflowMode = TextOverflowModes.Overflow;
            head.alignment = TextAlignmentOptions.MidlineLeft;
            var detail = UiKit.Txt(parent, "", phone ? 15 : 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            detail.name = "incoming-detail";
            detail.textWrappingMode = TextWrappingModes.NoWrap;
            detail.overflowMode = TextOverflowModes.Overflow;
            detail.alignment = TextAlignmentOptions.MidlineLeft;
            Func<string> tip = delegate { return "<b>受けるダメージの見込み</b>\n全ての敵の攻撃 (1発の数字×ヒット数) の合計から今のブロック (氷壁も) を引いた、この敵の番に失う HP の見込み。威圧・脆弱・重りも込み。HP バーの斜線の帯が削られる分"; };
            Tooltip.Attach(head.gameObject, tip);
            Tooltip.Attach(detail.gameObject, tip);
            head.raycastTarget = true; detail.raycastTarget = true;
            // 順送りの途中で盾の数字・HP・届いた攻撃と一緒に同じ式で引き直せるよう、見積りの元を持つ (2026-09-17)
            var info = head.gameObject.AddComponent<IncomingInfo>();
            info.Incoming = incoming; info.Phone = phone; info.Hidden = st.HideIntents == true; info.Detail = detail;
            info.SkipFirst = st.NullifyNextAttack == true;   // 身代わりの符: 最初の攻撃は見込みに入っていない (IncomingTotal と同じ)
            info.Lethal = LethalEdge(parent);
            ApplyIncomingText(info, p.Block + p.IceBlock, p.Hp);
            return info;
        }

        /// <summary>受けるダメージの見込みの元 (順送りの途中で盾の数字・HP・届いた攻撃と一緒に引き直す)。Landed = この敵の番の攻撃がもう届き始めた・Lethal = 自分の札の薔薇の縁</summary>
        public class IncomingInfo : MonoBehaviour { public int Incoming; public bool Phone, Hidden, Landed, SkipFirst; public TMP_Text Detail; public GameObject Lethal; }

        /// <summary>HP バーの削られる分 (2026-09-29 p08): 受けるダメージ − (ブロック＋氷壁)。自分の番だけ (敵の番・確認の窓・ルーンの円蓋・受けるダメージ 0 は 0＝帯を出さない)</summary>
        static int PredictedLoss(GameState st)
        {
            if (st == null || st.HideIntents == true || st.EnemyPhase == true || st.Phase != CombatPhases.PlayerTurn) return 0;
            int incoming = 0;
            try { incoming = Effects.IncomingTotal(st); } catch (Exception) { }
            if (incoming <= 0) return 0;
            return Math.Max(0, incoming - (st.Player.Block + st.Player.IceBlock));
        }

        /// <summary>見込みの2行の文 (head は結論・detail は内訳)。defense はブロック＋氷壁</summary>
        static void IncomingTexts(int incoming, int defense, int hp, bool phone, bool hidden, bool landed, out string head, out string detail, out bool lethal)
        {
            lethal = false;
            // 小さく添える部分: PC 20×75%=15・スマホ 18×85%≈15 (スマホの最小 15 を割らない)
            string small = phone ? "<size=85%>" : "<size=75%>";
            if (hidden) { head = small + "受けるダメージ ？</size>"; detail = "ルーンの円蓋で見えない"; return; }
            if (incoming <= 0)
            {
                head = small + (landed ? "この番の攻撃は終わった" : "この番は攻撃されない") + "</size>";
                detail = "";
                return;
            }
            int left = Math.Max(0, incoming - defense);
            lethal = hp - left <= 0;
            if (lethal) head = "HP " + Math.Max(0, hp) + " → 0 致死";   // 行ごと危険の墨 (色は ApplyIncomingText)
            else
            {
                // 失う HP: 1以上は危険の墨・0 (受けきる) は良いの墨
                // スマホも括弧つき (2026-09-30 F30: 括弧の無い「HP 80 → 69 -11」は「69 − 11」と続けて読めた。括弧つきでも 3桁の HP で約180/208 に収まる。HD-2D 見本の箱庭は札が 16 細く内側 192 = PhoneStripWHd2d)
                string lossTxt = "（−" + left + "）";
                head = "HP " + hp + " → <b>" + (hp - left) + "</b>" + small + UiKit.ColorTag(left > 0 ? PaperFx.BadInk : PaperFx.GoodInk, lossTxt) + "</size>";
            }
            // 内訳: ブロックが 0 なら「− ブロック」ごと省く (盾の札はバーの左端にもある)。スマホは幅 208 に入るよう「受ける」
            // (F30: 旧「ダメージ」は手札の本文の「ダメージ6」＝与える量と同じ語で、受ける量と区別がつかなかった)
            detail = (phone ? "受ける " : "受けるダメージ ") + UiKit.ColorTag(PaperFx.BadInk, "<b>" + incoming + "</b>")
                   + (defense > 0 ? " − ブロック " + UiKit.ColorTag(PaperFx.SkyInk, "<b>" + defense + "</b>") : "");
        }

        /// <summary>見込みの2行と致死の縁を、見えているブロック (氷壁込み) と HP で書き直す</summary>
        static void ApplyIncomingText(IncomingInfo info, int defense, int hp)
        {
            if (info == null) return;
            var head = info.GetComponent<TMP_Text>();
            string h, d; bool lethal;
            IncomingTexts(info.Incoming, defense, hp, info.Phone, info.Hidden, info.Landed, out h, out d, out lethal);
            if (head != null) { head.text = h; head.color = lethal ? PaperFx.BadInk : PaperFx.Ink; }
            if (info.Detail != null) info.Detail.text = d;
            if (info.Lethal != null && info.Lethal.activeSelf != lethal) info.Lethal.SetActive(lethal);
        }

        /// <summary>致死の見込みの時、自分の札の紙の外側に薔薇の 3px の縁 (塗りは付けない。舞台の縁の脈動とは別に、紙の側で知らせる。2026-09-29 p08)</summary>
        static GameObject LethalEdge(RectTransform strip)
        {
            var edge = PaperFx.Sheet(strip, PaperFx.Tag, "lethal", PaperFx.Rose);
            UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f);
            edge.raycastTarget = false;
            edge.transform.SetAsFirstSibling();   // 紙の後ろ＝外に 3px だけ見える
            edge.gameObject.SetActive(false);
            return edge.gameObject;
        }

        /// <summary>自分の札の見込みを、今見えている盾 (通常＋氷壁) と HP で引き直す (順送りの途中。無ければ何もしない)。HP バーの削られる分の帯も同じ数で</summary>
        public static void RefreshIncomingLine(RectTransform area, int block, int ice, int hp)
        {
            if (area == null) return;
            var wrap = area.Find("hpwrap");
            var t = wrap != null ? wrap.Find("incoming") : null;
            var info = t != null ? t.GetComponent<IncomingInfo>() : null;
            if (info == null) return;
            ApplyIncomingText(info, block + ice, hp);
            var hpRt = wrap.Find("hp");
            var bar = hpRt != null ? hpRt.GetComponentInChildren<HpBarInfo>() : null;
            if (bar != null && bar.Loss != null)
            {   // 帯の左端 = 見えている HP (滑っている途中なら行き先) − 残りの見込み。右端は塗りの端 (Now) のまま
                bar.LossTo = info.Hidden ? hp : hp - Math.Max(0, info.Incoming - (block + ice));
                SetLossBand(bar);
            }
        }

        /// <summary>順送りで敵の攻撃が届いた: 見込みから届いた分 (ブロック前の量) を引く＝残りの敵の攻撃だけを言う (旧: 届いた攻撃を HP と見込みで二重に引いていた)。
        /// 身代わりの符で消えた最初の攻撃は見込みに入っていないので引かない (2026-09-29 p08)</summary>
        public static void ConsumeIncoming(RectTransform area, int amount)
        {
            var wrap = area != null ? area.Find("hpwrap") : null;
            var t = wrap != null ? wrap.Find("incoming") : null;
            var info = t != null ? t.GetComponent<IncomingInfo>() : null;
            if (info == null) return;
            info.Landed = true;
            if (info.SkipFirst) { info.SkipFirst = false; return; }
            info.Incoming = Math.Max(0, info.Incoming - Math.Max(0, amount));
        }

        /// <summary>PC の自分の札の右端 (キャンバス x)。確認の窓が札に掛かるかを決める。スマホ・未作成は -1 (2026-09-29 p11)</summary>
        public static float SelfStripRight = -1f;

        /// <summary>スマホの自分の札の右端 (キャンバス x)。ギアの窓・持ち物の一覧の左端がこの 12 右から (2026-09-30 F42)。PC は -1</summary>
        public static float PhoneStripRight = -1f;

        /// <summary>スマホの上の帯 (からくり・ギア・置物) の下端 (キャンバスの上から)。ギアの窓の上端をこれより下に留める
        /// (2026-09-30 最終の答え合わせ: 窓が持ち物のトークンの名前を隠した)。PC は -1</summary>
        public static float PhoneBandBottom = -1f;

        /// <summary>PC の自分の札の区画の境 (2026-09-29 p11): 確認の窓を札の上へ上げられない時は、窓の手前の境で札を打ち切る
        /// (パネルを別のパネルの途中で切らない)。ギア・置物は敵の番には押せず暗幕の下なので、窓を閉じた時 (BattleView.CloseReactionWindow) に戻す</summary>
        public class SelfStripCuts : MonoBehaviour
        {
            public float[] Cuts;               // 区画の右端 (札の中の x): A・A+B・A+B+G・全幅
            public GameObject[][] Sections;    // [0]=A (常に残す)・[1]=からくり・[2]=ギア・[3]=置物
            public float FullW;
            public bool Trimmed;
        }

        /// <summary>自分の札を、右端が maxRight (キャンバス x) 以下になる区画の境で打ち切る。どの境も入らなければ A (HP・被ダメ) だけ残す</summary>
        static void TrimSelfStrip(RectTransform playerArea, float maxRight)
        {
            var wrap = playerArea != null ? playerArea.Find("hpwrap") as RectTransform : null;
            var cuts = wrap != null ? wrap.GetComponent<SelfStripCuts>() : null;
            if (cuts == null || cuts.Cuts == null || cuts.Sections == null) return;
            float left = playerArea.offsetMin.x + wrap.offsetMin.x;   // 札の左端 (キャンバス x)
            int keep = 0;
            for (int i = 0; i < cuts.Cuts.Length; i++) if (left + cuts.Cuts[i] <= maxRight) keep = i;
            if (keep >= cuts.Cuts.Length - 1) return;
            wrap.offsetMax = new Vector2(wrap.offsetMin.x + cuts.Cuts[keep], wrap.offsetMax.y);
            for (int s = keep + 1; s < cuts.Sections.Length; s++)
                foreach (var go in cuts.Sections[s]) if (go != null) go.SetActive(false);
            cuts.Trimmed = true;
        }

        /// <summary>打ち切った自分の札を元の幅に戻す (確認の窓を畳んだ時。戻した区画は暗幕と同じ 0.16 秒で現れる)</summary>
        public static void RestoreSelfStrip(RectTransform playerArea)
        {
            var wrap = playerArea != null ? playerArea.Find("hpwrap") as RectTransform : null;
            var cuts = wrap != null ? wrap.GetComponent<SelfStripCuts>() : null;
            if (cuts == null || !cuts.Trimmed) return;
            cuts.Trimmed = false;
            wrap.offsetMax = new Vector2(wrap.offsetMin.x + cuts.FullW, wrap.offsetMax.y);
            for (int s = 1; s < cuts.Sections.Length; s++)
                foreach (var go in cuts.Sections[s])
                {
                    if (go == null || go.activeSelf) continue;
                    go.SetActive(true);
                    var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    var c0 = cg;
                    Tween.Run(0.16f, k => { if (c0 != null) c0.alpha = k; }, Ease.Linear, () => { if (c0 != null) c0.alpha = 1f; });
                }
        }

        /// <summary>PC の自分の札の実際の高さ (2026-09-29 p17): 資源の札が2行・ギアが2段・置物が2行の時だけ 140、それ以外は 120。
        /// 札は上端を StripH に固定して下で吸収する (2026-09-30 F19) ので、位置の計算 (ギアの窓・人形の札の床・確認の窓・敵の帳面) は StripH を読む。これは札そのものの高さの参考。スマホは StripH</summary>
        public static float SelfStripH = 140f;

        /// <summary>
        /// PC の自分の札 (帳面の左端): HP＋ブロック／被ダメ／資源｜からくり (トークン 68×74)｜ギア｜置物 (付箋 200×40 を2行×最大3列・超えたら +N)。
        /// 中身の無い区画は描かない (2026-09-29 p17。スマホと同じ規則): 置物0なら置物の区画、ギア0ならギアの区画を見出しごと出さない＝序盤は A＋B だけの 480。
        /// 置物の列は、ギアを並べた後の空き (敵の帳面の手前) と枚数の小さい方。ギアは1段に名前つきで入れば名前の帯つき 68×74、入らなければ 62×62、それも入らなければ 48 の2段
        /// </summary>
        static void PcSelfStrip(GameRoot g, RectTransform area, GameState st, int shownHp)
        {
            var p = st.Player;
            float ax = area.offsetMin.x;   // area の左端 (キャンバス x)。札はキャンバス x=Edge (32) から (2026-09-29 p12: 旧 40)
            var gearList = DeckRogue.Engine.Run.GearsOf(g.Rs);
            // 置物の一覧と資源の札を先に作る (区画の幅と札の高さを中身から決める。2026-09-29 p17)
            var perms = new List<CardInstance>();
            for (int i = 0; i < p.Permanents.Count; i++) if (p.Permanents[i].Innate != true) perms.Add(p.Permanents[i]);
            var res = ResourceChips(p, st);
            const float PermSecMin = 236f;   // 置物の区画の1列ぶん (14＋付箋200＋22)。ギアの予算はこれを引いて出す＝置物を置いてもギアの並びが変わらない
            float secA = 300f, secB = p.SetSlots * (PhoneTokenW + 10f) + 24f;
            // C: ギア (2026-09-17 案A「匣の帯」): トークンを実際の個数ぶんだけ (空きは詰める。0 個なら区画ごと出さない＝2026-09-29 p17)。
            // 札はいちばん左の敵の帳面より左で止める (2026-09-18 ユーザー「ギアが集まると枠が左の敵のステータス表示と重なり何も見えない」):
            // 大きさは3段 (2026-09-29 p17): ①名前の帯つき 68×74 (からくりのトークンと同じ・間隔76) の1段 ②名前なし 62×62 の1段 ③48 の2段 (10個で 5列)。それでも溢れれば最後の枡を「+N」(押すと一覧から選べる)
            float zoneRight = BattleView.SelfZoneRight > 0f ? BattleView.SelfZoneRight - 8f : CanvasSize(area).x - 420f;
            // 置物の1列ぶんを先に取っておくのは、置物を出す見込みがある時だけ (2026-09-30 F39: 置物の札が1枚も無いデッキでも 236 を引き、
            // 4体戦の PC でギアが名前なしの2段に落ちてスマホより情報が少なかった。見込みがあれば今までどおり＝置物を置いてもギアの並びは変わらない)
            bool mayPerm = perms.Count > 0 || MayPlacePermanent(p);
            // 置物の区画が無い時は、ギアの区画の終わりに 12 の余白 (F40: 最後のトークンと札の右の縁の間が約5px しかなかった)。見込みがある時は取っておいた 236 の内側
            float gearEndPad = perms.Count == 0 ? 12f : 0f;
            float budget = zoneRight - UiKit.Edge - (secA + secB + (mayPerm ? PermSecMin : gearEndPad));   // C 区画に使える幅 (左右の余白 24 込み)
            int gearN = gearList.Count;
            float tokW = GearUi.TokenW, tokH = GearUi.TokenH, pitch = GearUi.TokenW + 8f; int gearCols = 0, gearRows = 1, gearShown = gearN;
            bool gearNamed = false;
            if (gearN > 0)
            {
                if (gearN * (GearUi.NamedW + 8f) + 16f <= budget)
                { tokW = GearUi.NamedW; tokH = GearUi.NamedH; pitch = GearUi.NamedW + 8f; gearNamed = true; gearCols = gearN; }
                else if (gearN * (GearUi.TokenW + 8f) + 16f <= budget) gearCols = gearN;
                else
                {
                    tokW = tokH = 46f; pitch = 54f; gearRows = 2;   // 46 の2段: 34+46+8+46 = 134、縁の輪を込めて 137 < 札の下の線 138 (F40: 48 だと下の段が札の下端から出た)
                    int fitCols = Math.Max(1, (int)((budget - 16f + 8f) / pitch));
                    gearCols = Math.Min(fitCols, (gearN + 1) / 2);
                    int cells = gearCols * 2;
                    gearShown = gearN <= cells ? gearN : cells - 1;
                }
            }
            float secG = gearN > 0 ? gearCols * pitch + 16f : 0f;
            // 見出し (2026-09-29 p14): 組めない理由 (魔素不足・今ターン済・占術中＝GearUi.BlockShort) は全部のギアに共通なのでトークンごとでなく見出しに1回＝「ギア 3 / 10  魔素不足」(理由は危険の墨)。
            // 先に文字を作って実寸を測り、入る幅を区画に確保する (敵の帳面の手前の予算の内側で。入らなければ理由だけ)。1個の時の区画 92 には入らないので広げる
            string gearHead = "ギア " + gearList.Count + " / " + Gears.GEAR_CARRY_MAX;
            TMP_Text gearLabel = null;
            if (gearN > 0)
            {
                gearLabel = UiKit.Txt(area, gearHead, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                gearLabel.textWrappingMode = TextWrappingModes.NoWrap;
                string gearWhy = GearUi.BlockShort(g.Rs, st);
                if (gearWhy != null)
                {
                    string why = UiKit.ColorTag(PaperFx.BadInk, gearWhy);
                    float room = Mathf.Max(secG, budget);
                    float needFull = gearLabel.GetPreferredValues(gearHead + "  " + why).x + 14f + 8f;   // 左の余白 14・区画の右の境まで 8
                    if (needFull <= room) { gearLabel.text = gearHead + "  " + why; secG = Mathf.Max(secG, needFull); }
                    else { gearLabel.text = why; secG = Mathf.Max(secG, Mathf.Min(room, gearLabel.GetPreferredValues(why).x + 14f + 8f)); }
                }
                secG += gearEndPad;
            }
            // D: 置物の列 (2026-09-29 p17 I23): ギアを並べた後の空き (敵の帳面の手前) に付箋の列 (200＋間隔8) が入るだけ、ただし枚数ぶん (ceil(n/2)) まで・最大3列。
            // 0枚なら区画ごと出さない (見出しも区切り線も)。1列なら今までどおり 236
            int permCols = 1;
            float secC = 0f;
            if (perms.Count > 0)
            {
                int permFit = (int)((zoneRight - (UiKit.Edge + secA + secB + secG) - 28f) / 208f);
                permCols = Mathf.Clamp(Math.Min(permFit, (perms.Count + 1) / 2), 1, 3);
                secC = permCols * 208f + 28f;
            }
            // 資源の札は中身の幅で行に詰める (2026-09-30 F31: 旧は「1行に3つ」の決め打ちで、首輪・成長・勢いの3枚が行の枠 278 を越えて重なり、
            // 先頭の札が紙の左の外へ出た)。2行に入らない分は2行目の最後を「+N」に (押すと一覧)
            var chipRowsList = PackChips(area, res, secA - 22f);
            int chipRows = chipRowsList.Count;
            // 高さ (2026-09-29 p17): 中身が1段なら 120 (見出し34＋トークン74＋余白12)、資源の札が2行・ギアが2段・置物が2行なら 140。
            // 上端を帳面の線 (StripH＝140) に固定し、120 の時は下で吸収する (2026-09-30 F19: 旧は下端を固定して上へ伸ばしたので、勢いや弱体で札が 120↔140 を
            // 行き来するたび、上端にそろえた敵全員の帳面が 20px 上下した)
            float w = secA + secB + secG + secC;
            float h = (chipRows > 1 || gearRows > 1 || perms.Count > permCols) ? 140f : 120f;
            SelfStripH = h;
            SelfStripRight = UiKit.Edge + w;   // 確認の窓が札に掛かるなら上げる/札を区画の境で打ち切る (2026-09-29 p11)
            PhoneStripRight = -1f;
            PhoneBandBottom = -1f;
            var strip = UiKit.NewRect("hpwrap", area);
            UiKit.Anchor(strip, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(UiKit.Edge - ax, StripH - h), new Vector2(UiKit.Edge - ax + w, StripH));
            var cuts = strip.gameObject.AddComponent<SelfStripCuts>();
            cuts.Cuts = new[] { secA, secA + secB, secA + secB + secG, w };
            cuts.FullW = w;
            var paper = PaperFx.Sheet(strip, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = false;
            // A: HP・被ダメ・資源
            // HP バーは被ダメの行と同じ左端 16 (2026-09-29 p04: 旧 66 = 水彩のにじみの盾の予約席。ブロックは敵と同じ盾の札をバーの左端に)
            var hpRt = UiKit.NewRect("hp", strip);
            UiKit.Anchor(hpRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -36f), new Vector2(secA - 12f, -14f));
            // 削られる分の帯 (2026-09-29 p08): 見込みは今の HP (p.Hp) から数える＝塗りが shownHp から滑る間も帯の左端は動かない
            HpBar(hpRt, Vector2.zero, Vector2.one, 0f, 0f, shownHp, p.MaxHp, 0f, 17, -1f, PredictedLoss(st), p.Hp);
            PlayerShieldSlot(hpRt, p.Block, 22f);
            if (shownHp != p.Hp) TweenHpBar(area, p.Hp);
            // 受けるダメージの見込み (2026-09-29 p08): 結論「HP 80 → 69（−11）」(Deco 20) を先に、内訳「受けるダメージ 11 − ブロック 5」(13) を後に
            var inc = IncomingBlock(strip, st, false);
            UiKit.Anchor(inc.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -64f), new Vector2(secA - 8f, -40f));
            UiKit.Anchor(inc.Detail.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -80f), new Vector2(secA - 8f, -64f));
            if (p.IceBlock > 0)
            {   // 氷壁: HP バーの行の右端に置き、バーをその分だけ縮める (2026-09-29 p04: 旧はバーの右端に重ねて描いていた。
                // 被ダメの行へ下ろす案は、行の幅 276 を被ダメの文 (約220) がほぼ使い切り「氷壁 N」が重なるので採らない)
                string iceText = "氷壁 " + p.IceBlock;
                var ice = UiKit.Txt(strip, iceText, 14, PaperFx.SkyInk, TextAnchor.MiddleRight, true);
                ice.name = "ice";
                ice.textWrappingMode = TextWrappingModes.NoWrap;
                ice.alignment = TextAlignmentOptions.MidlineRight;
                float iw = ice.GetPreferredValues(iceText + "0").x;   // 順送りで1桁増えても収まる幅
                UiKit.Anchor(ice.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(secA - 12f - iw, -36f), new Vector2(secA - 12f, -14f));
                hpRt.offsetMax = new Vector2(secA - 12f - iw - 8f, hpRt.offsetMax.y);
            }
            if (res.Count > 0)
            {   // 資源・状態の札 (PackChips の行割り)。内訳の行 (−80) から 4 あけて −84 から。2行の時は札 24・送り 27 で下端 −135 (札の下の線 138 の内側)
                bool twoRows = chipRows > 1;
                float chipH = twoRows ? 24f : 28f, chipPitch = twoRows ? 27f : 32f, chipTop = -84f;
                for (int r = 0; r < chipRows; r++)
                {
                    var col = UiKit.NewRect(r == 0 ? "chips" : "chips2", strip);
                    UiKit.Anchor(col, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, chipTop - r * chipPitch - chipH), new Vector2(secA - 8f, chipTop - r * chipPitch));
                    var vg = UiKit.Horz(col, 6, 0);
                    vg.childAlignment = TextAnchor.MiddleLeft; vg.childForceExpandWidth = false; vg.childForceExpandHeight = false;
                    foreach (int i in chipRowsList[r])
                    {
                        if (i >= 0) { SmallChip(col, res[i].Key, res[i].Value, PaperFx.Ink, chipH); continue; }
                        // 「+N」: 入らなかった札の一覧 (ツールチップ)
                        int hidden = -i;
                        var sb = new System.Text.StringBuilder();
                        for (int k = res.Count - hidden; k < res.Count; k++) { if (sb.Length > 0) sb.Append("\n"); sb.Append(ChipTip(res[k].Value)); }
                        string mtip = sb.ToString();
                        var more = Tag(col, chipH, 0f);
                        more.GetComponent<Image>().raycastTarget = true;
                        var mt = UiKit.Txt(more, "+" + hidden, 14, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                        UiKit.Le(mt, 16f, chipH - 2f, -1f, chipH - 2f);
                        Tooltip.Attach(more.gameObject, delegate { return mtip; });
                    }
                }
            }
            // B: からくり = 仕込み札のトークン
            var divA = UiKit.Pan(strip, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.35f), "div");
            UiKit.Anchor(divA.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(secA, 12f), new Vector2(secA + 1f, -12f));
            divA.raycastTarget = false;
            var setArea = UiKit.NewRect("setzone", strip);
            UiKit.Anchor(setArea, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(secA + 14f, 0f), new Vector2(secA + secB, 0f));
            var setLabel = UiKit.Txt(setArea, "からくり " + p.SetCards.Count + " / " + p.SetSlots, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            // 見出しはトークンの上端 -34 から離す (2026-09-29 p14: 旧 -30〜-10 は角の数字の輪 -23.5 に文字の下端が接していた。トークンは動かさない＝2段のギアの 138≤140 を保つ)
            UiKit.Anchor(setLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(0f, -6f));
            AttachSetLabelTip(g, setLabel.gameObject, setLabel);
            for (int i = 0; i < p.SetSlots; i++)
            {
                var slot = UiKit.NewRect("slot" + i, setArea);
                UiKit.Anchor(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (PhoneTokenW + 10f), -34f - PhoneTokenH), new Vector2(i * (PhoneTokenW + 10f) + PhoneTokenW, -34f));
                g.RegisterAnchor("setslot" + i, slot);
                if (i < p.SetCards.Count) PhoneSetToken(g, slot, st, p.SetCards[i]);
                else
                {   // 空きの枠: 点線のポケット (2026-09-29 p17: 旧は Tag を灰色に染めた塗り＝押せないボタンに見えた)。墨の中墨 45%・塗りなし。
                    // 青緑は使わない (「あとN回」の生きた罠の縁・帯の色なので、空の枠が罠に見える)
                    PaperFx.DashedPocket(slot, PhoneTokenW, PhoneTokenH, new Color(PaperFx.InkSoft.r, PaperFx.InkSoft.g, PaperFx.InkSoft.b, 0.45f), 0f);
                }
            }
            // C: ギア = 持ち物のトークン (押すと窓。敵の番・札の選択中は押せない)。0 個なら区画ごと出さない (2026-09-29 p17)。
            // 演出の予備の的 "gearzone" (Presenter) は、からくりの右端に幅0の矩形として残す
            float gx0 = secA + secB;
            GameObject[] gearSection;
            if (gearN == 0)
            {
                var gz = UiKit.NewRect("gearzone", strip);
                UiKit.Anchor(gz, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(gx0, 0f), new Vector2(gx0, 0f));
                g.RegisterAnchor("gearzone", gz);
                gearSection = new[] { gz.gameObject };
            }
            else
            {
                var divG = UiKit.Pan(strip, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.35f), "div");
                UiKit.Anchor(divG.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(gx0, 12f), new Vector2(gx0 + 1f, -12f));
                divG.raycastTarget = false;
                var gearArea = UiKit.NewRect("gearzone", strip);
                UiKit.Anchor(gearArea, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(gx0 + 14f, 0f), new Vector2(gx0 + secG, 0f));
                g.RegisterAnchor("gearzone", gearArea);
                gearLabel.rectTransform.SetParent(gearArea, false);   // 幅を測るために先に作った見出し (上の secG)
                UiKit.Anchor(gearLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(0f, -6f));
                bool canUseGear = st.Phase == CombatPhases.PlayerTurn && st.EnemyPhase != true && g.Pending == null;
                float rowPitch = gearRows > 1 ? tokH + 8f : 0f;   // 2段は 34+48+8+48 = 138 ≤ 140。名前つき1段は 34+74 = 108 (からくりのトークンと下端がそろう)
                for (int i = 0; i < gearShown; i++)
                {
                    // 名前の帯 (2026-09-29 p17 I48): 1段に 68×74 で入る時だけ (スマホと同じ帯。旧 PC は絵だけで名前はツールチップ＝歯車と過負荷の歯車が見分けにくい)
                    var tok = GearUi.Token(g, gearArea, g.Rs, st, i, gearList[i], tokW, tokH, gearNamed, canUseGear);
                    int cx = i % gearCols, cy = i / gearCols;
                    UiKit.Anchor(tok, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx * pitch, -34f - cy * rowPitch - tokH), new Vector2(cx * pitch + tokW, -34f - cy * rowPitch));
                }
                if (gearShown < gearN)
                {   // 溢れた分は最後の枡に「+N」(押すと持ち物の一覧。窓を開いている札が隠れていれば真鍮の縁)
                    var rest = new List<GearInstance>();
                    for (int i = gearShown; i < gearN; i++) rest.Add(gearList[i]);
                    bool openHidden = g.GearPending != null && g.GearPending.Index >= gearShown;
                    var chip = GearUi.MoreChip(g, gearArea, rest, tokW, tokH, openHidden);
                    int cx = gearShown % gearCols, cy = gearShown / gearCols;
                    UiKit.Anchor(chip, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx * pitch, -34f - cy * rowPitch - tokH), new Vector2(cx * pitch + tokW, -34f - cy * rowPitch));
                }
                gearSection = new[] { divG.gameObject, gearArea.gameObject };
            }
            // D: 置物 = 付箋 (挿絵 + 名前) を2行×permCols 列。入らない分は「+N …」。0 枚なら区画ごと出さない (2026-09-29 p17)
            float cx0 = secA + secB + secG;
            GameObject[] permSection = new GameObject[0];
            if (perms.Count > 0)
            {
                var divB = UiKit.Pan(strip, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.35f), "div");
                UiKit.Anchor(divB.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(cx0, 12f), new Vector2(cx0 + 1f, -12f));
                divB.raycastTarget = false;
                var permRow = UiKit.NewRect("perms", strip);
                UiKit.Anchor(permRow, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(cx0 + 14f, 0f), new Vector2(cx0 + secC - 8f, 0f));
                var permLabel = UiKit.Txt(permRow, "置物 " + perms.Count, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                UiKit.Anchor(permLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(0f, -6f));
                PermChips(permRow, perms, permCols, 200f, 34f, st);
                permSection = new[] { divB.gameObject, permRow.gameObject };
            }
            // 区画ごとの中身 (確認の窓が上げられない時に窓の手前の境で打ち切る。A は常に残す)
            cuts.Sections = new[] { new GameObject[0], new[] { divA.gameObject, setArea.gameObject }, gearSection, permSection };
        }

        // ---- 三周目 r3 の PC の自分の欄 (2026-10-02 仕様 docs/design/hd2d-slice/r3-ui-spec.md §6-1・§6-2) ----

        /// <summary>r3 の PC の足元の帳の左端 (匣の右 184 ＋20)・既定の右端 (仕様 §2 の x 204〜500)</summary>
        const float R3U_FootLeftPc = 204f, R3U_FootRightPc = 500f;
        /// <summary>足元の帳の高さ: 状態の札が無い時 74・ある時 96 (4行目 72〜94＋下の余白 2)</summary>
        const float R3U_FootHPc = 74f, R3U_FootHStatusPc = 96f;
        /// <summary>匣の幅 (x Edge〜Edge+152)・区画の間・見出しの行の高さ・上端の上限 (画面の上から 480＝主人公の頭 455 より下)</summary>
        const float R3U_BoxW = 152f, R3U_BoxGap = 8f, R3U_BoxHead = 22f, R3U_BoxTopFromTop = 480f;
        /// <summary>匣の下端 (キャンバス y・下から) = エナジーの輪の上端 (116+128) ＋24 (画面の上から 812)</summary>
        const float R3U_BoxBottomPc = 268f;
        /// <summary>匣のギアのトークン (3個以上): 46×46 を3列 (送り 50)・2段 (送り 53)</summary>
        const float R3U_GearSmall = 46f, R3U_GearPitchX = 50f, R3U_GearPitchY = 53f;
        /// <summary>匣の置物の付箋の高さ・段の送り</summary>
        const float R3U_PermChipH = 32f, R3U_PermPitchY = 38f;

        /// <summary>白の色を持つリーダーの時、足元の帳の右端を人形の座席のいちばん左の x の 38 手前で止める (人形の足元の札に掛けない。仕様 §6-1)。
        /// 座席は人形がいなくても先に見る (戦闘の途中で帳の幅が変わらない)。舞台が組めていなければ既定の右端</summary>
        static float R3U_FootRightFor(GameRoot g, float dflt)
        {
            if (g == null || !LightUi.LeaderHasWhite(g.Rs)) return dflt;
            try
            {
                var seats = Stage.DollSlots(BattleView.DollCap);
                float minX = float.MaxValue;
                foreach (var w in seats) minX = Mathf.Min(minX, Stage.R3U_ProjectPoint(w).x);
                if (minX < float.MaxValue) return Mathf.Min(dflt, minX - 38f);
            }
            catch (Exception e) { Debug.LogException(e); }
            return dflt;
        }

        /// <summary>
        /// r3 の PC の足元の帳 (名前 hpwrap・夜色の札1枚。仕様 §6-1): 主人公の足元の 10 下・x 204〜500 (白は人形の座席の 38 手前まで)。
        /// 上から HP バー (6〜28・盾は左端・氷壁は右端)／結論「HP 80 → 67（−13）」(30〜54)／内訳「受けるダメージ 17 − ブロック 5」(54〜70)／状態の札1行 (72〜94。入らない分は「+N」)。
        /// 状態が1つも無ければ4行目ごと無く高さ 74。子の名前は二周目と同じ (hp・incoming・incoming-detail・ice・chips) = 順送りの書き直し (RefreshIncomingLine・SetPlayerBlockBadge・
        /// TweenHpBar・ConsumeIncoming) がそのまま効く。確認の窓・ギアの窓は足元の帳に掛からない (§8 の x0 ≥ 右端＋12) ので TrimSelfStrip は使わない (区画は1つ)
        /// </summary>
        static void R3U_PcFootLedger(GameRoot g, RectTransform area, GameState st, int shownHp)
        {
            var p = st.Player;
            float ax = area.offsetMin.x;   // area の左端 (キャンバス x)。area の下端は足元の線 (StatusLineY)
            var res = ResourceChips(p, st);
            float h = res.Count > 0 ? R3U_FootHStatusPc : R3U_FootHPc;
            float x0 = R3U_FootLeftPc, x1 = Mathf.Max(x0 + 240f, R3U_FootRightFor(g, R3U_FootRightPc));
            float w = x1 - x0;
            float feet = Stage.FeetOffset("player", 130f);
            float top = feet - LedgerFeetGap;   // area の中 (下端＝足元の線) の高さ
            SelfStripH = h;
            SelfStripRight = -1f;   // r3 は確認の窓が自分の札を打ち切らない (下の窓 R3PcWindowRect)。人形の札の床も自分の札を見ない
            PhoneStripRight = -1f;
            PhoneBandBottom = -1f;
            R3U_FootRight = x1;
            float canvasTop = BattleView.StatusLineY + top;
            R3U_SelfRects.Add(new Rect(x0, canvasTop - h, w, h));
            var strip = UiKit.NewRect("hpwrap", area);
            UiKit.Anchor(strip, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x0 - ax, top - h), new Vector2(x0 - ax + w, top));
            var cuts = strip.gameObject.AddComponent<SelfStripCuts>();
            cuts.Cuts = new[] { w, w, w, w };
            cuts.FullW = w;
            cuts.Sections = new[] { new GameObject[0], new GameObject[0], new GameObject[0], new GameObject[0] };
            var paper = PaperFx.Sheet(strip, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = false;
            // 1段目: HP バー (二周目の自分の札と同じ部品。行 6〜28)
            var hpRt = UiKit.NewRect("hp", strip);
            UiKit.Anchor(hpRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -28f), new Vector2(w - 12f, -6f));
            HpBar(hpRt, Vector2.zero, Vector2.one, 0f, 0f, shownHp, p.MaxHp, 0f, 17, -1f, PredictedLoss(st), p.Hp);
            PlayerShieldSlot(hpRt, p.Block, 22f);
            if (shownHp != p.Hp) TweenHpBar(area, p.Hp);
            // 2・3段目: 受けるダメージの結論 (Deco 20) と内訳 (13)
            var inc = IncomingBlock(strip, st, false);
            UiKit.Anchor(inc.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -54f), new Vector2(w - 8f, -30f));
            UiKit.Anchor(inc.Detail.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -70f), new Vector2(w - 8f, -54f));
            if (p.IceBlock > 0)
            {   // 氷壁: HP バーの行の右端 (二周目と同じ作法。バーをその分だけ縮める)
                string iceText = "氷壁 " + p.IceBlock;
                var ice = UiKit.Txt(strip, iceText, 14, PaperFx.SkyInk, TextAnchor.MiddleRight, true);
                ice.name = "ice";
                ice.textWrappingMode = TextWrappingModes.NoWrap;
                ice.alignment = TextAlignmentOptions.MidlineRight;
                float iw = ice.GetPreferredValues(iceText + "0").x;
                UiKit.Anchor(ice.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(w - 12f - iw, -28f), new Vector2(w - 12f, -6f));
                hpRt.offsetMax = new Vector2(w - 12f - iw - 8f, hpRt.offsetMax.y);
            }
            // 4段目: 状態の札1行 (成長・勢い・弱体・虚弱・拘束…)。入らない分は「+N」(一覧はツールチップ)
            if (res.Count > 0)
            {
                var rows = PackChips(strip, res, w - 24f, 1);
                if (rows.Count > 0) R3U_ChipRow(strip, "chips", res, rows[0], 12f, -72f, w - 8f, 22f);
            }
        }

        /// <summary>状態の札の1行 (PackChips の行割りの1行。負の値 −N は「+N」＝入らない札の一覧のツールチップ)。parent の左上から (left, top) に高さ chipH</summary>
        static RectTransform R3U_ChipRow(RectTransform parent, string name, List<KeyValuePair<string, string>> res, List<int> row, float left, float top, float right, float chipH)
        {
            var col = UiKit.NewRect(name, parent);
            UiKit.Anchor(col, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, top - chipH), new Vector2(right, top));
            var hg = UiKit.Horz(col, 6, 0);
            hg.childAlignment = TextAnchor.MiddleLeft; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            foreach (int i in row)
            {
                if (i >= 0) { SmallChip(col, res[i].Key, res[i].Value, PaperFx.Ink, chipH); continue; }
                int hidden = -i;
                var sb = new System.Text.StringBuilder();
                for (int k = res.Count - hidden; k < res.Count; k++) { if (sb.Length > 0) sb.Append("\n"); sb.Append(ChipTip(res[k].Value)); }
                string mtip = sb.ToString();
                var more = Tag(col, chipH, 0f);
                more.GetComponent<Image>().raycastTarget = true;
                var mt = UiKit.Txt(more, "+" + hidden, 14, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Le(mt, 16f, chipH - 2f, -1f, chipH - 2f);
                Tooltip.Attach(more.gameObject, delegate { return mtip; });
            }
            return col;
        }

        /// <summary>
        /// r3 の PC の左下の匣 (名前 box・x Edge〜Edge+152。仕様 §6-2): エナジーの輪の真上 (下端 画面の上から 812) から上へ、からくり → ギア → 置物 の区画を積む。
        /// 区画ごとに夜色の札・間は 8・見出しは 13px。上端は画面の上から 480 より上へ出さない: 超える時は 置物を1段 → ギアを1段 の順に畳む (からくりは畳まない＝判断に要る)。
        /// 人形は付箋に並べず見出しの「人形 m」だけ (舞台の足元の札で数と期限が読める。仕様 §15 Q1)。演出の的 (setslotN・setlabel・gearzone・gear:uid) は匣の中
        /// </summary>
        static void R3U_PcBox(GameRoot g, RectTransform area, GameState st)
        {
            var p = st.Player;
            var cs = CanvasSize(area);
            float ax = area.offsetMin.x, ay = BattleView.StatusLineY;
            float left = UiKit.Edge;
            var gearList = DeckRogue.Engine.Run.GearsOf(g.Rs);
            int gearN = gearList.Count;
            var perms = new List<CardInstance>();
            int dolls = 0;
            for (int i = 0; i < p.Permanents.Count; i++)
            {
                var q = p.Permanents[i];
                if (q.Innate == true) continue;
                if (DollUi.IsDoll(q)) dolls++; else perms.Add(q);
            }
            // 区画の高さ (見出し 22＋中身＋下の余白 4)
            int setRows = Math.Max(1, (p.SetSlots + 1) / 2);
            float hSet = R3U_BoxHead + setRows * PhoneTokenH + (setRows - 1) * 8f + 4f;
            int gearRows = gearN <= 0 ? 0 : gearN <= 2 ? 1 : (gearN <= 3 ? 1 : 2);
            bool gearNamed = gearN > 0 && gearN <= 2;
            Func<int, float> gearH = rows => gearN <= 0 ? 0f : R3U_BoxHead + (gearNamed ? PhoneTokenH : rows * R3U_GearSmall + (rows - 1) * (R3U_GearPitchY - R3U_GearSmall)) + 4f;
            bool hasPermSec = perms.Count > 0 || dolls > 0;
            int permRows = perms.Count >= 2 ? 2 : perms.Count;
            Func<int, float> permH = rows => !hasPermSec ? 0f : R3U_BoxHead + (rows > 0 ? R3U_PermChipH + (rows - 1) * R3U_PermPitchY : 0f) + 4f;
            Func<float> total = () =>
            {
                float t = hSet;
                if (gearN > 0) t += R3U_BoxGap + gearH(gearRows);
                if (hasPermSec) t += R3U_BoxGap + permH(permRows);
                return t;
            };
            float topLimit = cs.y - R3U_BoxTopFromTop;
            float bottom = R3U_BoxBottomPc;
            if (bottom + total() > topLimit && permRows > 1) permRows = 1;          // ①置物を1段 (付箋1枚＋「+N」)
            if (bottom + total() > topLimit && gearRows > 1 && !gearNamed) gearRows = 1;   // ②ギアを1段 (3枡目が「+N」)
            // ③名前つきのギア (1〜2個・高さ 100) を 46×46 の1段へ (2026-10-02 読み合わせの指摘: かすみの3枠＋ギア2＋置物で上端が 行 456＝主人公の頭の高さに届いた)。
            // それでも超えるのは仕込み枠5つ (かすみ＋二重の符＋罠師の茂み) だけ: からくりのトークンは 68×74 より小さくすると帯の字が 13px を割るので畳まず、上限を越えて積む (仕様 §6-2 の注記)
            if (bottom + total() > topLimit && gearNamed) { gearNamed = false; gearRows = 1; }
            float boxH = total();
            var box = UiKit.NewRect("box", area);
            UiKit.Anchor(box, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(left - ax, bottom - ay), new Vector2(left - ax + R3U_BoxW, bottom - ay + boxH));
            R3U_SelfRects.Add(new Rect(left, bottom, R3U_BoxW, boxH));
            float y = 0f;   // box の中の下から

            // からくり (いちばん下。トークン 68×74 を2枚で1段。3枠は2段目＝下の段の左。点線のポケット・帯の一言・角の数字は二周目の部品)
            var setArea = R3U_BoxSection(box, "setzone", y, hSet);
            var setLabel = UiKit.Txt(setArea, "からくり " + p.SetCards.Count + " / " + p.SetSlots, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Anchor(setLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -R3U_BoxHead), new Vector2(-6f, -2f));
            setLabel.textWrappingMode = TextWrappingModes.NoWrap;
            AttachSetLabelTip(g, setLabel.gameObject, setLabel);
            for (int i = 0; i < p.SetSlots; i++)
            {
                int r = i / 2, c = i % 2;
                var slot = UiKit.NewRect("slot" + i, setArea);
                float tx = 4f + c * 76f, ttop = -R3U_BoxHead - r * (PhoneTokenH + 8f);
                UiKit.Anchor(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(tx, ttop - PhoneTokenH), new Vector2(tx + PhoneTokenW, ttop));
                g.RegisterAnchor("setslot" + i, slot);
                if (i < p.SetCards.Count) PhoneSetToken(g, slot, st, p.SetCards[i]);
                else PaperFx.DashedPocket(slot, PhoneTokenW, PhoneTokenH, new Color(PaperFx.InkSoft.r, PaperFx.InkSoft.g, PaperFx.InkSoft.b, 0.45f), 0f);
            }
            y += hSet;

            // ギア (からくりの上)。0個なら区画を出さず、演出の予備の的 gearzone はからくりの見出しの右端に幅 0 で
            if (gearN == 0)
            {
                var gz = UiKit.NewRect("gearzone", setArea);
                UiKit.Anchor(gz, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -R3U_BoxHead), new Vector2(0f, 0f));
                g.RegisterAnchor("gearzone", gz);
            }
            else
            {
                y += R3U_BoxGap;
                float hg = gearH(gearRows);
                var gearArea = R3U_BoxSection(box, "gearzone", y, hg);
                g.RegisterAnchor("gearzone", gearArea);
                // 見出し「ギア n / 10」＋組めない理由 (夜の札の上なので危険の墨は Nightify が夜の淡い朱へ写す)。入らなければ理由だけ
                string head = "ギア " + gearN + " / " + Gears.GEAR_CARRY_MAX;
                var gearLabel = UiKit.Txt(gearArea, head, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                gearLabel.textWrappingMode = TextWrappingModes.NoWrap;
                UiKit.Anchor(gearLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -R3U_BoxHead), new Vector2(-4f, -2f));
                string why = GearUi.BlockShort(g.Rs, st);
                if (why != null)
                {
                    string whyTag = UiKit.ColorTag(PaperFx.BadInk, why);
                    gearLabel.text = gearLabel.GetPreferredValues(head + " " + why).x <= R3U_BoxW - 12f ? head + " " + whyTag : whyTag;
                }
                bool canUseGear = st.Phase == CombatPhases.PlayerTurn && st.EnemyPhase != true && g.Pending == null;
                int cells = gearNamed ? gearN : gearRows * 3;
                int shown = gearN <= cells ? gearN : cells - 1;
                for (int i = 0; i < shown + (shown < gearN ? 1 : 0); i++)
                {
                    float tw, th, tx, ttop;
                    if (gearNamed) { tw = PhoneTokenW; th = PhoneTokenH; tx = 4f + i * 76f; ttop = -R3U_BoxHead; }
                    else { tw = th = R3U_GearSmall; tx = 3f + (i % 3) * R3U_GearPitchX; ttop = -R3U_BoxHead - (i / 3) * R3U_GearPitchY; }
                    RectTransform tok;
                    if (i < shown) tok = GearUi.Token(g, gearArea, g.Rs, st, i, gearList[i], tw, th, gearNamed, canUseGear);
                    else
                    {   // 溢れた分は最後の枡に「+N」(押すと持ち物の一覧。窓を開いている札が隠れていれば真鍮の縁)
                        var rest = new List<GearInstance>();
                        for (int k = shown; k < gearN; k++) rest.Add(gearList[k]);
                        tok = GearUi.MoreChip(g, gearArea, rest, tw, th, g.GearPending != null && g.GearPending.Index >= shown);
                    }
                    UiKit.Anchor(tok, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(tx, ttop - th), new Vector2(tx + tw, ttop));
                }
                y += hg;
            }

            // 置物 (いちばん上)。見出し「置物 n・人形 m」。付箋 (挿絵＋名前) を2段、3枚目以降は2段目が「+N …」。人形は数だけ
            if (hasPermSec)
            {
                y += R3U_BoxGap;
                float hp = permH(permRows);
                var permRow = R3U_BoxSection(box, "perms", y, hp);
                string head = (perms.Count > 0 ? "置物 " + perms.Count : "") + (perms.Count > 0 && dolls > 0 ? "・" : "") + (dolls > 0 ? "人形 " + dolls : "");
                var permLabel = UiKit.Txt(permRow, head, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                permLabel.textWrappingMode = TextWrappingModes.NoWrap;
                UiKit.Anchor(permLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -R3U_BoxHead), new Vector2(-4f, -2f));
                if (dolls > 0) Tooltip.Attach(permLabel.gameObject, delegate { return "<b>人形 " + dolls + "</b>\n人形は舞台の足元の札で、何をするかと残りの期限を読む"; });
                if (dolls > 0) permLabel.raycastTarget = true;
                int fit = permRows * 1;   // 付箋の枚数 (段ごとに1枚)
                int showN = perms.Count <= fit ? perms.Count : Math.Max(0, fit - 1);
                bool oneRowMore = permRows == 1 && perms.Count > 1;   // 1段に畳んだ時: 付箋1枚＋「+N」を横に
                if (oneRowMore) showN = 1;
                for (int i = 0; i < showN; i++)
                {
                    var chip = UiKit.NewRect("perm", permRow);
                    float cw = oneRowMore ? R3U_BoxW - 4f - 48f - 6f : R3U_BoxW - 8f;
                    float ctop = -R3U_BoxHead - i * R3U_PermPitchY;
                    UiKit.Anchor(chip, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, ctop - R3U_PermChipH), new Vector2(4f + cw, ctop));
                    PhonePermChip(chip, perms[i], st);
                }
                if (showN < perms.Count)
                {
                    var more = UiKit.NewRect("more", permRow);
                    float mx = oneRowMore ? R3U_BoxW - 4f - 48f : 4f, mw = oneRowMore ? 48f : 72f;
                    float mtop = -R3U_BoxHead - (oneRowMore ? 0 : showN) * R3U_PermPitchY;
                    UiKit.Anchor(more, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(mx, mtop - R3U_PermChipH), new Vector2(mx + mw, mtop));
                    var mImg = PaperFx.Sheet(more, PaperFx.Tag2, "paper");
                    UiKit.Stretch(mImg.rectTransform, 0f, 0f, 0f, 0f);
                    mImg.raycastTarget = true;
                    var mt = UiKit.Txt(more, "+" + (perms.Count - showN) + (oneRowMore ? "" : " …"), 15, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                    UiKit.Stretch(mt.rectTransform, 4f, 4f, 0f, 0f);
                    var sb = new System.Text.StringBuilder();
                    for (int i = showN; i < perms.Count; i++) { if (sb.Length > 0) sb.Append("\n"); sb.Append("<b>" + perms[i].Def.Name + "</b> " + CardText.Body(perms[i].Def)); }
                    string mtip = sb.ToString();
                    Tooltip.Attach(more.gameObject, delegate { return mtip; });
                }
            }
        }

        /// <summary>匣の区画 (夜色の札1枚)。box の中の下から y に高さ h・全幅</summary>
        static RectTransform R3U_BoxSection(RectTransform box, string name, float y, float h)
        {
            var sec = UiKit.NewRect(name, box);
            UiKit.Anchor(sec, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, y), new Vector2(0f, y + h));
            var paper = PaperFx.Sheet(sec, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = false;
            return sec;
        }

        /// <summary>置物の付箋を並べる (cols 列・2行)。行に収まらない分は「+N …」(タップで名前の一覧)。上端 top から下へ</summary>
        static void PermChips(RectTransform permRow, List<CardInstance> perms, int cols, float chipW, float top, GameState st = null)
        {
            int cells = cols * 2;
            int show = perms.Count <= cells ? perms.Count : cells - 1;
            for (int i = 0; i < show; i++)
            {
                var chip = UiKit.NewRect("perm", permRow);
                int cx = i % cols, cy = i / cols;
                UiKit.Anchor(chip, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx * (chipW + 8f), -top - cy * (PhoneChipH + 6f) - PhoneChipH), new Vector2(cx * (chipW + 8f) + chipW, -top - cy * (PhoneChipH + 6f)));
                PhonePermChip(chip, perms[i], st);
            }
            if (show < perms.Count)
            {   // 残りは「+N …」(タップで名前の一覧)
                var more = UiKit.NewRect("more", permRow);
                int cx = show % cols, cy = show / cols;
                UiKit.Anchor(more, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(cx * (chipW + 8f), -top - cy * (PhoneChipH + 6f) - PhoneChipH), new Vector2(cx * (chipW + 8f) + 72f, -top - cy * (PhoneChipH + 6f)));
                var mImg = PaperFx.Sheet(more, PaperFx.Tag2, "paper");
                UiKit.Stretch(mImg.rectTransform, 0f, 0f, 0f, 0f);
                mImg.raycastTarget = true;
                var mt = UiKit.Txt(more, "+" + (perms.Count - show) + " …", 15, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Stretch(mt.rectTransform, 4f, 4f, 0f, 0f);
                var sb = new System.Text.StringBuilder();
                for (int i = show; i < perms.Count; i++) { if (sb.Length > 0) sb.Append("\n"); sb.Append("<b>" + perms[i].Def.Name + "</b> " + CardText.Body(perms[i].Def)); }
                string mtip = sb.ToString();
                Tooltip.Attach(more.gameObject, delegate { return mtip; });
            }
        }

        /// <summary>資源の札を行に詰める (F31)。幅は SmallChip と同じ部品 (左右の余白 10+10・絵 16＋間 6・14px の文字) で測る。最大2行。
        /// 入らない分は2行目の最後を「+N」(負の値 −N) にする</summary>
        static List<List<int>> PackChips(RectTransform any, List<KeyValuePair<string, string>> res, float rowW, int maxRows = 2)
        {
            var rows = new List<List<int>>();
            if (res.Count == 0) return rows;
            var meas = UiKit.Txt(any, "", 14, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            meas.textWrappingMode = TextWrappingModes.NoWrap;
            var cw = new float[res.Count];
            for (int i = 0; i < res.Count; i++) cw[i] = 20f + (res[i].Key != null ? 22f : 0f) + meas.GetPreferredValues(res[i].Value).x + 2f;
            float moreW = 20f + meas.GetPreferredValues("+" + res.Count).x + 2f;
            meas.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(meas.gameObject);
            var cur = new List<int>(); float x = 0f;
            int k = 0;
            for (; k < res.Count; k++)
            {
                float add = (cur.Count > 0 ? 6f : 0f) + cw[k];
                if (cur.Count > 0 && x + add > rowW)
                {
                    rows.Add(cur);
                    if (rows.Count == maxRows) break;
                    cur = new List<int>(); x = 0f; add = cw[k];
                }
                cur.Add(k); x += add;
            }
            if (rows.Count < maxRows && cur.Count > 0) { rows.Add(cur); cur = null; }
            if (k < res.Count)
            {   // 2行目の最後を「+N」に: 入るまで2行目の後ろから外す
                var last = rows[rows.Count - 1];
                int hidden = res.Count - k;
                float lx = 0f; for (int j = 0; j < last.Count; j++) lx += (j > 0 ? 6f : 0f) + cw[last[j]];
                while (last.Count > 1 && lx + 6f + moreW > rowW) { lx -= cw[last[last.Count - 1]] + 6f; last.RemoveAt(last.Count - 1); hidden++; }
                last.Add(-hidden);
            }
            return rows;
        }

        /// <summary>置物を場に出す見込み (手札・山札・捨て札に置物か召喚の札がある)。無いデッキでは PC の自分の札に置物の区画を取っておかない (F39)</summary>
        static bool MayPlacePermanent(PlayerState p)
        {
            Func<IReadOnlyList<CardInstance>, bool> any = list =>
            {
                if (list == null) return false;
                foreach (var c in list)
                {
                    var d = c.Def;
                    if (d == null) continue;
                    if (d.Type == "permanent") return true;
                    if (d.Effects != null) foreach (var e in d.Effects) if (e.Effect == "summonPermanent") return true;
                    if (d.Modes != null) foreach (var m in d.Modes) if (m.Effects != null) foreach (var e in m.Effects) if (e.Effect == "summonPermanent") return true;
                }
                return false;
            };
            return any(p.Hand) || any(p.DrawPile) || any(p.DiscardPile);
        }

        /// <summary>資源・状態の札の一覧 (アイコン名, 文言)</summary>
        static List<KeyValuePair<string, string>> ResourceChips(PlayerState p, GameState st = null)
        {
            var res = new List<KeyValuePair<string, string>>();
            // 天鵞絨の首輪 (2026-09-18 Opus 白C): 残り枚数を事前に出す (7枚目で初めてエラー、は読めない)
            if (st != null && st.PlayCap != null) res.Add(new KeyValuePair<string, string>(null, "首輪 あと" + Math.Max(0, st.PlayCap.Value - (p.PlaysThisTurn ?? 0)) + "枚"));
            if (p.Growth > 0) res.Add(new KeyValuePair<string, string>("growth", "成長 " + p.Growth));
            if (p.Momentum > 0) res.Add(new KeyValuePair<string, string>("momentum", "勢い " + p.Momentum));
            if (p.Aether > 0) res.Add(new KeyValuePair<string, string>("energy", "霊気 " + p.Aether));
            // 灯は資源の札に出さない (2026-09-20 灯の表示: エナジーの輪の隣の灯籠 LightUi が担う。同じ物を2か所に描かない)
            if ((p.SparksPlayedThisCombat ?? 0) > 0) res.Add(new KeyValuePair<string, string>("draw", "火種 " + p.SparksPlayedThisCombat.Value + "枚")); // 撃った火種 (2026-09-20 夜。火種の嵐の参照値)
            if (p.NextCardDiscount > 0) res.Add(new KeyValuePair<string, string>("energy", "次のカード -" + p.NextCardDiscount));
            if (p.SpellEchoes > 0) res.Add(new KeyValuePair<string, string>("draw", "反復 " + p.SpellEchoes));
            // 状態異常は「名前 残りNT」で、数字がターンだと一目で読めるように (2026-09-09「デバフ表示が分かりにくすぎる」)。
            // 記号は StatusIcon と同じ (2026-09-29 p18: 弱体＝威圧と同じ二重の山形・虚弱＝ひびの盾。首輪・拘束は1対1の記号が無いので絵なし＝旧は地図の格子)
            if (p.Weak > 0) res.Add(new KeyValuePair<string, string>("weak", "弱体 " + p.Weak + "T"));
            if (p.Vulnerable > 0) res.Add(new KeyValuePair<string, string>("exposed", "脆弱 " + p.Vulnerable + "T"));
            if (p.Frail > 0) res.Add(new KeyValuePair<string, string>("frail", "虚弱 " + p.Frail + "T"));
            if (p.Restrain > 0) res.Add(new KeyValuePair<string, string>(null, "拘束 " + p.Restrain + "T"));
            if ((p.Mist ?? 0) > 0) res.Add(new KeyValuePair<string, string>("draw", "霞み " + p.Mist.Value + "T"));
            if ((p.Slow ?? 0) > 0) res.Add(new KeyValuePair<string, string>("exposed", "重り " + p.Slow.Value + "T"));
            return res;
        }

        // ---- スマホの「自分の欄」(2026-09-14 ユーザー裁定「案B 左に3段」→ 2026-09-15 案C: HP・被ダメ・資源を足元の線の札に) ----
        // 画面の左の一角に上から からくり (仕込み札のトークン 68×74)／置物 (付箋 168×40)／自分の札 (HP＋ブロック・被ダメ予測・資源) を積む。
        // 自分の札の下端は敵の札と同じ線 (StatusLineY = 手札のすぐ上)。資源の札が3つ以上なら札が2行ぶん高くなり、置物は1行に詰める。
        // 座標はキャンバスの左上から測った値 (S25 相当 1462×675) を area (左下が feet.x-130, StatusLineY) の座標へ写す。
        public const float PhoneTokenW = 68f, PhoneTokenH = 74f, PhoneChipW = 168f, PhoneChipH = 40f;

        /// <summary>
        /// スマホの自分の札 (左下の hpwrap) の幅。HD-2D 見本の箱庭だけ (2026-10-01 P33)。今の舞台は 224 のまま。
        /// 箱庭では自分の札が演出で動かない (P20 2周目: リーダーの入れ物の兄弟) ので、被弾ののけぞり (絵の Lunge −36 と、入れ物の押し縮みの拡大・2.5° の傾きで
        /// 足元が横へ最大 約60 ずれる) の間、リーダーの足元の検査の幅 (絵の幅の ±25%) の左端が 232.7 まで来て、札の右端 248 (=24+224) に入っていた
        /// (W3b の regress の PH-R06-guard-9・PH-R07-hurt-5 = layout-check L2「足元を self:hpwrap が 60px 隠す」)。16 細くして右端を 232 にする (のけぞりの最も深いコマより左)。
        /// 見込みの2行は最長 185 (3桁の被ダメと3桁のブロック「受ける 110 − ブロック 120」を Klee One 15 で測った幅) で、内側 192 に収まる
        /// </summary>
        public const float PhoneStripWHd2d = 208f;

        static void PhoneSelfColumn(GameRoot g, RectTransform area, GameState st, int shownHp)
        {
            if (R3) { R3U_PhoneSelf(g, area, st, shownHp); return; }   // 三周目 r3 (2026-10-02 仕様 §6-3): 足元の帳 (HP・見込み・からくり) と上の帯 (状態・ギア・置物)
            SelfStripRight = -1f;   // スマホの確認の窓は自分の欄 (左の列) に掛からないので上げない (2026-09-29 p11)
            SelfStripH = StripH;    // PC 用 (スマホでは読まない)
            var p = st.Player;
            float ax = area.offsetMin.x;                  // area の左端 (キャンバス x)
            float ay = BattleView.StatusLineY;            // area の下端 (キャンバス y・下から)
            var cs = CanvasSize(area);
            // キャンバス左上基準の (x, top, w, h) を area の Anchor (左下基準) に置く
            Action<RectTransform, float, float, float, float> place = (rt, x, top, w, h) =>
                UiKit.Anchor(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x - ax, cs.y - top - h - ay), new Vector2(x - ax + w, cs.y - top - ay));
            float left = UiKit.Edge; const float bandTop = 62f;   // 左の余白は四辺の余白と同じ (2026-09-29 p12。値は 24 のまま＝出典だけ一本に)
            PhoneBandBottom = bandTop + 22f + PhoneTokenH;   // ギア・置物の区画を置いたら伸ばす

            // 上の帯 (リーダーの頭より上): からくり = 仕込み札のトークン (挿絵・状態の一言・角に残り回数)
            var setArea = UiKit.NewRect("setzone", area);
            float setW = p.SetSlots * (PhoneTokenW + 10f);
            place(setArea, left, bandTop, setW, 22f + PhoneTokenH);   // 見出しの行 22 + トークン (角の数字は上に 4 はみ出す＝輪を含めて 5.5)
            var setLabel = PaperFx.NightNote(setArea, "からくり " + p.SetCards.Count + " / " + p.SetSlots, 14, 200f);
            setLabel.anchorMin = setLabel.anchorMax = new Vector2(0f, 1f); setLabel.pivot = new Vector2(0f, 1f);
            // 見出しの札は区画の上へ 9 (2026-09-29 p14: 旧 2 は札の下半分に角の数字が食い込んだ。上部バーの札の下端 47 から 6 空く)
            setLabel.anchoredPosition = new Vector2(-4f, 9f);
            AttachSetLabelTip(g, setLabel.gameObject, setLabel.GetComponent<Image>());
            for (int i = 0; i < p.SetSlots; i++)
            {
                var slot = UiKit.NewRect("slot" + i, setArea);
                UiKit.Anchor(slot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(i * (PhoneTokenW + 10f), 0f), new Vector2(i * (PhoneTokenW + 10f) + PhoneTokenW, PhoneTokenH));
                g.RegisterAnchor("setslot" + i, slot);
                if (i < p.SetCards.Count) PhoneSetToken(g, slot, st, p.SetCards[i]);
                else
                {   // 空きの枠: 点線のポケット (文字は置かない)。夜の上なので紙色の破線 55%＋塗り 7% (2026-09-29 p17: 旧は白35%の塗り＝押せないボタンに見えた)。
                    // 大きさは 68×74 のまま (仕込んだ札がこの枠の大きさへ降りる)。青緑は使わない (生きた罠の縁の色)
                    PaperFx.DashedPocket(slot, PhoneTokenW, PhoneTokenH, new Color(PaperFx.Paper.r, PaperFx.Paper.g, PaperFx.Paper.b, 0.55f), 0.07f);
                }
            }

            // 同じ帯のからくりの右: ギア (2026-09-17 案A「匣の帯」) = 64×66 のトークン。溢れは「+N」(押すと持ち物の一覧から選べる)。
            // 帯はいちばん左の敵の意図の札より左で止める (2026-09-18 ユーザー「ギアが集まると枠が左の敵のステータス表示と重なり何も見えない」):
            // 置物に最低1列を残した幅にトークンが収まるだけ並べ、収まらなければ最後を「+N」に。旧・固定の上限 6 (幅 1300 未満は 4) は敵の位置を見ていなかった
            var gearList = DeckRogue.Engine.Run.GearsOf(g.Rs);
            var perms = new List<CardInstance>();
            for (int i = 0; i < p.Permanents.Count; i++) if (p.Permanents[i].Innate != true) perms.Add(p.Permanents[i]);
            float zoneRight = BattleView.SelfZoneRight > 0f ? BattleView.SelfZoneRight - 8f : cs.x - 300f;
            float gearW = 0f;
            if (gearList.Count > 0)
            {
                float gearPitch = GearUi.PhoneTokenW + 8f, chipW = 44f;
                float avail = zoneRight - (left + setW + 14f) - (perms.Count > 0 ? PhoneChipW + 8f + 14f : 0f);
                int fitAll = (int)((avail + 8f) / gearPitch);
                int gearShown = gearList.Count <= fitAll ? gearList.Count : Math.Max(1, (int)((avail - chipW - 8f + 8f) / gearPitch));
                bool more = gearShown < gearList.Count;
                gearW = gearShown * gearPitch + (more ? chipW + 8f : 0f);
                var gearArea = UiKit.NewRect("gearzone", area);
                place(gearArea, left + setW + 14f, bandTop, gearW, 22f + GearUi.PhoneTokenH);
                g.RegisterAnchor("gearzone", gearArea);
                PhoneBandBottom = Mathf.Max(PhoneBandBottom, bandTop + 22f + GearUi.PhoneTokenH);
                // 組めない理由 (2026-09-29 p14) は見出しに1回＝「ギア 3 / 10・魔素不足」。夜の札の上なので理由も紙色の文字のまま (夜に危険の墨は読めない)
                string gearWhy = GearUi.BlockShort(g.Rs, st);
                var gearLabel = PaperFx.NightNote(gearArea, "ギア " + gearList.Count + " / " + Gears.GEAR_CARRY_MAX + (gearWhy != null ? "・" + gearWhy : ""), 14, gearWhy != null ? Mathf.Max(200f, gearW) : 160f);
                gearLabel.anchorMin = gearLabel.anchorMax = new Vector2(0f, 1f); gearLabel.pivot = new Vector2(0f, 1f);
                gearLabel.anchoredPosition = new Vector2(-4f, 9f);
                // 見出しの札が区画より広ければ区画を広げる (右の置物の見出しと重ならないように。トークンは左下に留めてあるので動かない)
                float labelRight = gearLabel.sizeDelta.x - 4f;
                if (labelRight > gearW) { gearW = labelRight; place(gearArea, left + setW + 14f, bandTop, gearW, 22f + GearUi.PhoneTokenH); }
                bool canUseGear = st.Phase == CombatPhases.PlayerTurn && st.EnemyPhase != true && g.Pending == null;
                for (int i = 0; i < gearShown; i++)
                {
                    var tok = GearUi.Token(g, gearArea, g.Rs, st, i, gearList[i], GearUi.PhoneTokenW, GearUi.PhoneTokenH, true, canUseGear);
                    UiKit.Anchor(tok, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(i * (GearUi.PhoneTokenW + 8f), 0f), new Vector2(i * (GearUi.PhoneTokenW + 8f) + GearUi.PhoneTokenW, GearUi.PhoneTokenH));
                }
                if (more)
                {
                    var rest = new List<GearInstance>();
                    for (int i = gearShown; i < gearList.Count; i++) rest.Add(gearList[i]);
                    bool openHidden = g.GearPending != null && g.GearPending.Index >= gearShown;
                    var chip = GearUi.MoreChip(g, gearArea, rest, chipW, GearUi.PhoneTokenH, openHidden);
                    UiKit.Anchor(chip, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(gearShown * gearPitch, 0f), new Vector2(gearShown * gearPitch + chipW, GearUi.PhoneTokenH));
                }
                gearW += 14f;
            }
            // その右: 置物 = 付箋 (挿絵 + 名前) を2列×2行 (狭いキャンバスと、2列が敵の表示に掛かる時は1列)。超えたら「+N …」。リーダーの頭 (y≈200) より上なので絵と重ならない
            if (perms.Count > 0)
            {
                float permX = left + setW + 14f + gearW;
                int cols = cs.x >= 1400f && permX + 2f * (PhoneChipW + 8f) - 8f <= zoneRight ? 2 : 1;
                var permRow = UiKit.NewRect("perms", area);
                place(permRow, permX, bandTop, cols * (PhoneChipW + 8f), 22f + 2f * (PhoneChipH + 6f));
                PhoneBandBottom = Mathf.Max(PhoneBandBottom, bandTop + 22f + (perms.Count > cols ? 2f : 1f) * (PhoneChipH + 6f));
                var permLabel = PaperFx.NightNote(permRow, "置物 " + perms.Count, 14, 120f);
                permLabel.anchorMin = permLabel.anchorMax = new Vector2(0f, 1f); permLabel.pivot = new Vector2(0f, 1f);
                permLabel.anchoredPosition = new Vector2(-4f, 9f);   // からくり・ギアの見出しと同じ行 (p14)
                PermChips(permRow, perms, cols, PhoneChipW, 22f, st);
            }

            // 自分の札 (下端は帳面の線): HP＋ブロック／被ダメ予測 (2行)／資源 (1行に2つ・3つ目からは2行目)
            var res = ResourceChips(p, st);
            int resRows = res.Count > 2 ? 2 : (res.Count > 0 ? 1 : 0);
            float stripW = Hd2dLayout ? PhoneStripWHd2d : 224f, stripH = 80f + resRows * 30f;   // HP の行 18→22 (2026-09-29 p04) の分 +4・見込みの結論を Deco 18 にした分 +6 (p08)
            float stripTop = cs.y - ay - stripH;
            var strip = UiKit.NewRect("hpwrap", area);
            // 画面の切り欠き (S25 のパンチホール＝横持ちで左端の縦の中央) がこの札の高さに掛かれば、その右へ逃がす (2026-09-29 p10: 「被ダメ」の頭が穴に隠れていた)。
            // からくり・ギア・置物の帯、エナジーの輪、山札、上部バーは穴の高さの外なので動かさない
            float stripX = UiKit.SafeLeft(left, stripTop, stripTop + stripH);
            PhoneStripRight = stripX + stripW;   // ギアの窓を札の右に置く (切り欠きで札が右へずれた時も重ねない。2026-09-30 F42)
            place(strip, stripX, stripTop, stripW, stripH);
            var paper = PaperFx.Sheet(strip, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = false;
            // HP バーは被ダメの行と同じ左端 8 (2026-09-29 p04: 旧 54 = にじみの盾の予約席)。帯 18→22 (15 の数字が塗りの内側に収まる)
            var hpRt = UiKit.NewRect("hp", strip);
            UiKit.Anchor(hpRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -30f), new Vector2(-8f, -8f));
            HpBar(hpRt, Vector2.zero, Vector2.one, 0f, 0f, shownHp, p.MaxHp, 0f, 13, -1f, PredictedLoss(st), p.Hp);
            PlayerShieldSlot(hpRt, p.Block, 22f);
            if (shownHp != p.Hp) TweenHpBar(area, p.Hp);
            if (p.IceBlock > 0)
            {   // 氷壁: PC と同じく HP バーの行の右端に置き、バーをその分縮める (2026-09-29 p08: 旧は見込みの2行の右端＝内訳「受ける 11 − ブロック 5」と重なる)
                string iceText = "氷壁 " + p.IceBlock;
                var ice = UiKit.Txt(strip, iceText, 13, PaperFx.SkyInk, TextAnchor.MiddleRight, true);
                ice.name = "ice";
                ice.textWrappingMode = TextWrappingModes.NoWrap;
                ice.alignment = TextAlignmentOptions.MidlineRight;
                float iw = ice.GetPreferredValues(iceText + "0").x;   // 順送りで1桁増えても収まる幅
                UiKit.Anchor(ice.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f - iw, -30f), new Vector2(-8f, -8f));
                hpRt.offsetMax = new Vector2(-8f - iw - 8f, hpRt.offsetMax.y);
            }
            // 受けるダメージの見込み (2026-09-29 p08): 結論「HP 80 → 69（−11）」(Deco 18) と内訳「受ける 11 − ブロック 5」(15。2026-09-30 F30) の2行。旧は「＝」の後で折れていた
            var inc = IncomingBlock(strip, st, true);
            UiKit.Anchor(inc.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -54f), new Vector2(-8f, -32f));
            UiKit.Anchor(inc.Detail.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -76f), new Vector2(-8f, -54f));
            for (int r = 0; r < resRows; r++)
            {
                int from = r * 2;
                var col = UiKit.NewRect(r == 0 ? "chips" : "chips2", strip);
                UiKit.Anchor(col, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -80f - r * 30f - 28f), new Vector2(-4f, -80f - r * 30f));
                var vg = UiKit.Horz(col, 6, 0);
                vg.childAlignment = TextAnchor.MiddleLeft; vg.childForceExpandWidth = false; vg.childForceExpandHeight = false;
                col.gameObject.AddComponent<RectMask2D>();
                for (int i = from; i < res.Count && i < from + 2; i++) SmallChip(col, res[i].Key, res[i].Value, PaperFx.Ink);
            }
        }

        // ---- 三周目 r3 のスマホの自分の欄 (2026-10-02 仕様 docs/design/hd2d-slice/r3-ui-spec.md §3・§6-3) ----

        /// <summary>r3 のスマホの足元の帳: HP の区画の幅・高さ・足元からの間</summary>
        const float R3U_PhoneHpW = 200f, R3U_PhoneFootH = 80f, R3U_PhoneFootGap = 8f;
        /// <summary>白で仕込み枠が3つ以上の時のトークン (足元の帳の右端を人形の札の手前に収める)</summary>
        const float R3U_PhoneTokSmallW = 56f, R3U_PhoneTokSmallH = 62f;

        /// <summary>
        /// r3 のスマホの自分の欄: 足元の帳 (名前 hpwrap・x 24〜384・主人公の足元の 8 下・高さ 80) = HP の区画 (24〜224: HP バー／結論／内訳) ＋ からくりの区画 (setzone・トークン 68×74・見出しなし)。
        /// 上の帯 (今の位置 上から 62〜) = 状態の札 (2列×最大2行・「+N」) → ギア (68×66 名前つき・溢れは「+N」) → 置物 (付箋 168×40 を2行)。
        /// 確認の窓・ギアの窓が開いている間はからくりの区画を畳む (TrimSelfStrip。区画の境 = HP の区画の右)
        /// </summary>
        static void R3U_PhoneSelf(GameRoot g, RectTransform area, GameState st, int shownHp)
        {
            SelfStripRight = -1f;
            SelfStripH = StripH;
            var p = st.Player;
            float ax = area.offsetMin.x, ay = BattleView.StatusLineY;
            var cs = CanvasSize(area);
            Action<RectTransform, float, float, float, float> place = (rt, x, top, w, h) =>
                UiKit.Anchor(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x - ax, cs.y - top - h - ay), new Vector2(x - ax + w, cs.y - top - ay));
            float left = UiKit.Edge; const float bandTop = 62f;
            PhoneBandBottom = -1f;
            float bandRight = left;

            // 上の帯 ①状態の札 (2列×最大2行。5つ目からは2行目の右が「+N」)
            var res = ResourceChips(p, st);
            float statusW = 0f;
            if (res.Count > 0)
            {
                var meas = UiKit.Txt(area, "", 14, PaperFx.Ink, TextAnchor.MiddleLeft, true);
                meas.textWrappingMode = TextWrappingModes.NoWrap;
                Func<int, float> cw = i => 20f + (res[i].Key != null ? 22f : 0f) + meas.GetPreferredValues(res[i].Value).x + 2f;
                var rows = new List<List<int>> { new List<int>() };
                for (int i = 0; i < res.Count && i < 2; i++) rows[0].Add(i);
                if (res.Count > 2)
                {
                    rows.Add(new List<int>());
                    if (res.Count <= 4) for (int i = 2; i < res.Count; i++) rows[1].Add(i);
                    else { rows[1].Add(2); rows[1].Add(-(res.Count - 3)); }
                }
                float moreW = 20f + meas.GetPreferredValues("+" + res.Count).x + 2f;
                foreach (var r in rows)
                {
                    float rw = 0f;
                    for (int j = 0; j < r.Count; j++) rw += (j > 0 ? 6f : 0f) + (r[j] >= 0 ? cw(r[j]) : moreW);
                    statusW = Mathf.Max(statusW, rw);
                }
                meas.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(meas.gameObject);
                statusW = Mathf.Ceil(statusW) + 4f;
                var chips = UiKit.NewRect("chips", area);
                place(chips, left, bandTop + 22f, statusW, 28f + 30f * (rows.Count - 1));
                for (int r = 0; r < rows.Count; r++) R3U_ChipRow(chips, r == 0 ? "row0" : "row1", res, rows[r], 0f, -r * 30f, statusW, 28f);
                PhoneBandBottom = bandTop + 22f + 28f + 30f * (rows.Count - 1);
                bandRight = left + statusW;
                statusW += 14f;
            }

            // ②ギア・③置物 (二周目の上の帯と同じ部品。からくりが抜けた分、状態の札の右から)
            var gearList = DeckRogue.Engine.Run.GearsOf(g.Rs);
            var perms = new List<CardInstance>();
            for (int i = 0; i < p.Permanents.Count; i++) if (p.Permanents[i].Innate != true) perms.Add(p.Permanents[i]);
            float zoneRight = BattleView.SelfZoneRight > 0f ? BattleView.SelfZoneRight - 8f : cs.x - 300f;
            float gx = left + statusW;
            float gearW = 0f;
            if (gearList.Count > 0)
            {
                float gearPitch = GearUi.PhoneTokenW + 8f, chipW = 44f;
                float avail = zoneRight - gx - (perms.Count > 0 ? PhoneChipW + 8f + 14f : 0f);
                int fitAll = (int)((avail + 8f) / gearPitch);
                int gearShown = gearList.Count <= fitAll ? gearList.Count : Math.Max(1, (int)((avail - chipW - 8f + 8f) / gearPitch));
                bool more = gearShown < gearList.Count;
                gearW = gearShown * gearPitch + (more ? chipW + 8f : 0f);
                var gearArea = UiKit.NewRect("gearzone", area);
                place(gearArea, gx, bandTop, gearW, 22f + GearUi.PhoneTokenH);
                g.RegisterAnchor("gearzone", gearArea);
                PhoneBandBottom = Mathf.Max(PhoneBandBottom, bandTop + 22f + GearUi.PhoneTokenH);
                string gearWhy = GearUi.BlockShort(g.Rs, st);
                var gearLabel = PaperFx.NightNote(gearArea, "ギア " + gearList.Count + " / " + Gears.GEAR_CARRY_MAX + (gearWhy != null ? "・" + gearWhy : ""), 14, gearWhy != null ? Mathf.Max(200f, gearW) : 160f);
                gearLabel.anchorMin = gearLabel.anchorMax = new Vector2(0f, 1f); gearLabel.pivot = new Vector2(0f, 1f);
                gearLabel.anchoredPosition = new Vector2(-4f, 9f);
                float labelRight = gearLabel.sizeDelta.x - 4f;
                if (labelRight > gearW) { gearW = labelRight; place(gearArea, gx, bandTop, gearW, 22f + GearUi.PhoneTokenH); }
                bool canUseGear = st.Phase == CombatPhases.PlayerTurn && st.EnemyPhase != true && g.Pending == null;
                for (int i = 0; i < gearShown; i++)
                {
                    var tok = GearUi.Token(g, gearArea, g.Rs, st, i, gearList[i], GearUi.PhoneTokenW, GearUi.PhoneTokenH, true, canUseGear);
                    UiKit.Anchor(tok, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(i * gearPitch, 0f), new Vector2(i * gearPitch + GearUi.PhoneTokenW, GearUi.PhoneTokenH));
                }
                if (more)
                {
                    var rest = new List<GearInstance>();
                    for (int i = gearShown; i < gearList.Count; i++) rest.Add(gearList[i]);
                    bool openHidden = g.GearPending != null && g.GearPending.Index >= gearShown;
                    var chip = GearUi.MoreChip(g, gearArea, rest, chipW, GearUi.PhoneTokenH, openHidden);
                    UiKit.Anchor(chip, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(gearShown * gearPitch, 0f), new Vector2(gearShown * gearPitch + chipW, GearUi.PhoneTokenH));
                }
                bandRight = gx + gearW;
                gearW += 14f;
            }
            else
            {   // 演出の予備の的 gearzone (Presenter) は帯の左端に幅 0 で
                var gz = UiKit.NewRect("gearzone", area);
                place(gz, gx, bandTop, 0f, 22f + GearUi.PhoneTokenH);
                g.RegisterAnchor("gearzone", gz);
            }
            if (perms.Count > 0)
            {
                float permX = gx + gearW;
                int cols = cs.x >= 1400f && permX + 2f * (PhoneChipW + 8f) - 8f <= zoneRight ? 2 : 1;
                var permRow = UiKit.NewRect("perms", area);
                place(permRow, permX, bandTop, cols * (PhoneChipW + 8f), 22f + 2f * (PhoneChipH + 6f));
                PhoneBandBottom = Mathf.Max(PhoneBandBottom, bandTop + 22f + (perms.Count > cols ? 2f : 1f) * (PhoneChipH + 6f));
                var permLabel = PaperFx.NightNote(permRow, "置物 " + perms.Count, 14, 120f);
                permLabel.anchorMin = permLabel.anchorMax = new Vector2(0f, 1f); permLabel.pivot = new Vector2(0f, 1f);
                permLabel.anchoredPosition = new Vector2(-4f, 9f);
                PermChips(permRow, perms, cols, PhoneChipW, 22f, st);
                bandRight = permX + cols * (PhoneChipW + 8f);
            }
            if (PhoneBandBottom > 0f) R3U_SelfRects.Add(new Rect(left, cs.y - PhoneBandBottom, bandRight - left, PhoneBandBottom - bandTop));

            // 足元の帳 (主人公の足元の 8 下・高さ 80): HP の区画 ＋ からくりの区画
            float feet = Stage.FeetOffset("player", 130f);
            float top = cs.y - (ay + feet) + R3U_PhoneFootGap;   // キャンバスの上から
            float h = R3U_PhoneFootH;
            bool smallTok = p.SetSlots >= 3 && LightUi.LeaderHasWhite(g.Rs);
            float tw = smallTok ? R3U_PhoneTokSmallW : PhoneTokenW, th = smallTok ? R3U_PhoneTokSmallH : PhoneTokenH;
            float setW = 8f + p.SetSlots * (tw + 8f);
            float stripW = R3U_PhoneHpW + setW;
            float stripX = UiKit.SafeLeft(left, top, top + h);   // 画面の切り欠き (仕様 §3: 足元の帳は穴の高さの外に来るが、端末が変わっても守る)
            PhoneStripRight = stripX + stripW;
            R3U_FootRight = stripX + stripW;
            R3U_FootHpRight = stripX + R3U_PhoneHpW;
            R3U_PhoneFootRect = new Rect(stripX, cs.y - top - h, stripW, h);
            R3U_SelfRects.Add(R3U_PhoneFootRect);
            var strip = UiKit.NewRect("hpwrap", area);
            place(strip, stripX, top, stripW, h);
            var cuts = strip.gameObject.AddComponent<SelfStripCuts>();
            cuts.Cuts = new[] { R3U_PhoneHpW, stripW, stripW, stripW };
            cuts.FullW = stripW;
            var paper = PaperFx.Sheet(strip, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = false;
            var hpRt = UiKit.NewRect("hp", strip);
            UiKit.Anchor(hpRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -26f), new Vector2(R3U_PhoneHpW - 8f, -4f));
            HpBar(hpRt, Vector2.zero, Vector2.one, 0f, 0f, shownHp, p.MaxHp, 0f, 13, -1f, PredictedLoss(st), p.Hp);
            PlayerShieldSlot(hpRt, p.Block, 22f);
            if (shownHp != p.Hp) TweenHpBar(area, p.Hp);
            if (p.IceBlock > 0)
            {   // 氷壁: HP バーの行の右端 (二周目と同じ作法)
                string iceText = "氷壁 " + p.IceBlock;
                var ice = UiKit.Txt(strip, iceText, 13, PaperFx.SkyInk, TextAnchor.MiddleRight, true);
                ice.name = "ice";
                ice.textWrappingMode = TextWrappingModes.NoWrap;
                ice.alignment = TextAlignmentOptions.MidlineRight;
                float iw = ice.GetPreferredValues(iceText + "0").x;
                UiKit.Anchor(ice.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(R3U_PhoneHpW - 8f - iw, -26f), new Vector2(R3U_PhoneHpW - 8f, -4f));
                hpRt.offsetMax = new Vector2(R3U_PhoneHpW - 8f - iw - 8f, hpRt.offsetMax.y);
            }
            var inc = IncomingBlock(strip, st, true);
            UiKit.Anchor(inc.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -50f), new Vector2(R3U_PhoneHpW - 6f, -28f));
            UiKit.Anchor(inc.Detail.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -72f), new Vector2(R3U_PhoneHpW - 6f, -50f));
            // からくりの区画 (見出しは置かない。空きのポケットとトークンの無い所を押すと からくり と 仕込む の説明＝的 setlabel はこの区画の矩形)
            var div = UiKit.Pan(strip, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.35f), "div");
            UiKit.Anchor(div.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(R3U_PhoneHpW, 8f), new Vector2(R3U_PhoneHpW + 1f, -8f));
            div.raycastTarget = false;
            var setArea = UiKit.NewRect("setzone", strip);
            UiKit.Anchor(setArea, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(R3U_PhoneHpW, 0f), new Vector2(stripW, 0f));
            var setHit = setArea.gameObject.AddComponent<Image>();
            setHit.color = Color.clear;
            AttachSetLabelTip(g, setArea.gameObject, setHit);
            for (int i = 0; i < p.SetSlots; i++)
            {
                // トークンの部品 (PhoneSetToken) は 68×74 の寸法で組むので、小さいトークンは枠ごと縮める (演出の的 setslotN は縮めた矩形の中心へ飛ぶ)
                var slot = UiKit.NewRect("slot" + i, setArea);
                float sx = 8f + i * (tw + 8f) + tw / 2f;
                UiKit.Anchor(slot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(sx - PhoneTokenW / 2f, -PhoneTokenH / 2f), new Vector2(sx + PhoneTokenW / 2f, PhoneTokenH / 2f));
                if (smallTok) slot.localScale = Vector3.one * (tw / PhoneTokenW);
                g.RegisterAnchor("setslot" + i, slot);
                if (i < p.SetCards.Count) PhoneSetToken(g, slot, st, p.SetCards[i]);
                else PaperFx.DashedPocket(slot, PhoneTokenW, PhoneTokenH, new Color(PaperFx.InkSoft.r, PaperFx.InkSoft.g, PaperFx.InkSoft.b, 0.45f), 0f);   // 札の上なので中墨の破線 (PC の匣と同じ)
            }
            cuts.Sections = new[] { new GameObject[0], new[] { div.gameObject, setArea.gameObject }, new GameObject[0], new GameObject[0] };
        }

        /// <summary>仕込み札のトークン (68×74): 上に挿絵 64×38、下に状態の帯 (準備中／あとN回／鳴る／期限なし)。生きている札は蜂蜜の縁、今ターン鳴る札は縁が脈打ち角に残り回数</summary>
        /// <summary>からくりの枠を今の盤面で描き直す (2026-09-30 F55: 準備が明けた瞬間の浮き文字「準備完了」と、帯の「準備中」が順送りの間食い違った)。
        /// 期限切れで番号がずれても同じ札が2枚見えないよう全部の枠を描き直す。空きの枠は PC＝中墨の破線・スマホ＝紙色の破線 (組み立てと同じ)</summary>
        public static void RedrawSetTokens(GameRoot g, GameState st)
        {
            if (g == null || st == null) return;
            for (int i = 0; i < st.Player.SetSlots; i++)
            {
                var slot = g.Anchor("setslot" + i);
                if (slot == null) continue;
                for (int c = slot.childCount - 1; c >= 0; c--) { var ch = slot.GetChild(c); ch.SetParent(null, false); UnityEngine.Object.Destroy(ch.gameObject); }
                if (i < st.Player.SetCards.Count) PhoneSetToken(g, slot, st, st.Player.SetCards[i]);
                else if (UiKit.Phone && !R3) PaperFx.DashedPocket(slot, PhoneTokenW, PhoneTokenH, new Color(PaperFx.Paper.r, PaperFx.Paper.g, PaperFx.Paper.b, 0.55f), 0.07f);   // r3 のスマホは足元の帳 (札) の中 = PC と同じ中墨
                else PaperFx.DashedPocket(slot, PhoneTokenW, PhoneTokenH, new Color(PaperFx.InkSoft.r, PaperFx.InkSoft.g, PaperFx.InkSoft.b, 0.45f), 0f);
            }
        }

        static void PhoneSetToken(GameRoot g, RectTransform slot, GameState st, CardInstance sc, bool badge = true)
        {
            // badge=false: 角の数字を出さない (確認の窓の中＝残り回数は行の右の「あとN回」1か所だけ。2026-09-29 p03)
            bool live = Effects.IsTrapLive(st, sc);
            bool canFireNow = live && Effects.TrapCanFireThisPhase(st, sc);
            int? left = Effects.TrapWindowsLeft(st, sc);
            // 縁 (状態の色) → 紙 → 挿絵 → 帯 → 角の数字
            var edge = PaperFx.Sheet(slot, PaperFx.Tag, "edge", !live ? PaperFx.InkSoft : canFireNow ? PaperFx.Mana : PaperFx.ManaLight);   // からくり＝青緑 (2026-09-16 カラーテーマ)
            UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f);
            edge.raycastTarget = false;
            if (canFireNow)
            {
                var gimg = edge; float t0 = UnityEngine.Random.value;
                Tween.Run(1.2f, k => { if (gimg != null) { var c = gimg.color; c.a = 0.75f + 0.25f * Mathf.Sin((k + t0) * Mathf.PI * 2f); gimg.color = c; } }, Ease.Linear, null);
            }
            var paper = PaperFx.Sheet(slot, PaperFx.Tag2, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = true;
            var frame = UiKit.Pan(slot, PaperFx.Ink, "frame");
            UiKit.Anchor(frame.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, -41f), new Vector2(67f, -1f));
            frame.raycastTarget = false;
            var pic = UiKit.NewRect("pic", slot);
            UiKit.Anchor(pic, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, -40f), new Vector2(66f, -2f));
            var pimg = pic.gameObject.AddComponent<Image>();
            pimg.sprite = ThemeFx.CardArt(sc.Def.Id, Theme.CardTypeColor(sc.Def.Type)); pimg.preserveAspect = true; pimg.raycastTarget = false;
            UiKit.PixelArt(pimg);   // 80×48 を 64×38 に (PC 0.79倍＝バイリニア・スマホ 1.04倍。p25)
            if (!live) pimg.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            // 今ターン鳴る札の帯は「鳴る」(2026-09-30 F29: 旧「今ターン」は隣の「準備中」(今ターンに仕込んだ札) と取り違えやすく、右の「あと2回」と同じ状態を2語で言っていた)
            string band = !live ? "準備中" : canFireNow ? "鳴る" : left.HasValue ? "あと" + left.Value + "回" : "期限なし";
            var bandImg = UiKit.Pan(slot, !live ? PaperFx.InkSoft : canFireNow ? PaperFx.ManaInk : PaperFx.ManaBand, "band");
            UiKit.Anchor(bandImg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 29f));
            bandImg.raycastTarget = false;
            var bt = UiKit.Deco(slot, band, 15, PaperFx.Paper, TextAnchor.MiddleCenter);
            UiKit.Anchor(bt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(0f, 29f));
            bt.textWrappingMode = TextWrappingModes.NoWrap;
            if (canFireNow && badge)
            {   // 角の数字 = 残りの窓 (期限なしは ∞)。外へのはみ出しは 4 (輪を含めて 5.5。2026-09-29 p14: 旧 9＝上の見出し「からくり 1 / 2」の文字に乗っていた)
                var badgeRt = UiKit.NewRect("badge", slot);
                UiKit.Anchor(badgeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(4f, 4f));
                var bImg = badgeRt.gameObject.AddComponent<Image>();
                bImg.sprite = PaperFx.Disc(); bImg.preserveAspect = true; bImg.raycastTarget = false;
                var bRing = UiKit.NewRect("ring", badgeRt);
                UiKit.Stretch(bRing, -1.5f, -1.5f, -1.5f, -1.5f);
                var rImg = bRing.gameObject.AddComponent<Image>();
                rImg.sprite = PaperFx.Ring(4); rImg.color = PaperFx.Ink; rImg.raycastTarget = false; rImg.preserveAspect = true;
                var btx = UiKit.Deco(badgeRt, left.HasValue ? left.Value.ToString() : "∞", 13, PaperFx.Ink, TextAnchor.MiddleCenter);
                UiKit.Stretch(btx.rectTransform, 0f, 0f, 0f, 0f);
            }
            string trapLife = Effects.TrapStatusTextKarakuri(st, sc);
            string tip = "<b>" + sc.Def.Name + "</b>\n" + CardText.Body(sc.Def) + "\n" + UiKit.ColorTag(PaperFx.BrassInk, trapLife);   // 注意書き＝真鍮の墨 (旧・金の墨 #7a4e12。2026-09-29 p26)
            string liveTip = null;
            try { liveTip = Effects.SetCardLiveDamage(st, sc.Def); } catch (Exception) { }
            if (liveTip != null) tip += "\n" + UiKit.ColorTag(PaperFx.BrassInk, liveTip);
            Tooltip.Attach(paper.gameObject, delegate { return tip; });
        }

        /// <summary>置物の付箋 (168×40): 挿絵 48×29 + 名前 (長い名前は…)。タップで本文</summary>
        static void PhonePermChip(RectTransform chip, CardInstance q, GameState st = null)
        {
            var img = PaperFx.Sheet(chip, PaperFx.Tag2, "paper");
            UiKit.Stretch(img.rectTransform, 0f, 0f, 0f, 0f);
            img.raycastTarget = true;
            var frame = UiKit.Pan(chip, PaperFx.Ink, "frame");
            UiKit.Anchor(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, -15.5f), new Vector2(54f, 15.5f));
            frame.raycastTarget = false;
            var pic = UiKit.NewRect("pic", chip);
            UiKit.Anchor(pic, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, -14.5f), new Vector2(53f, 14.5f));
            var pimg = pic.gameObject.AddComponent<Image>();
            // 挿絵 80×48 を 0.6 倍に縮めるとドットが間引かれる → 中央の 48×29 を切り出して1ドット＝1単位で置く (2026-09-29 p25)
            pimg.sprite = ThemeFx.CardArtCrop(q.Def.Id, Theme.CardTypeColor(q.Def.Type), 48, 29); pimg.preserveAspect = true; pimg.raycastTarget = false;
            UiKit.PixelArt(pimg);
            var nt = UiKit.Deco(chip, q.Def.Name, 15, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Anchor(nt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(60f, 0f), new Vector2(-8f, 0f));
            nt.textWrappingMode = TextWrappingModes.NoWrap;
            nt.overflowMode = TextOverflowModes.Ellipsis;
            string ptip = "<b>" + q.Def.Name + "</b>\n" + CardText.Body(q.Def);
            // 人形の付箋には残りの期限 (2026-09-21): 挿絵の右上の角に丸い数字 (からくり・ギアの角の数字＝残り回数と同じ形。残り1は朱・期限なしは ∞)。
            // 2026-09-29 p16: 旧は右端に「あとN」の文字 (指定 11 がスマホで 15 に切り上がり、「癒しの人形」の名前に重なって「癒しの人形と5」と読めた)。
            // 丸は挿絵の側に置くので名前の枠 (60〜−8) は削らない
            if (DollUi.IsDoll(q) && st != null)
            {
                var left = DollUi.LifeLeft(st, q);
                var badge = UiKit.NewRect("life", chip);
                UiKit.Anchor(badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(35f, -4f), new Vector2(55f, 16f));   // 挿絵 (5〜53×±14.5) の右上。付箋の上の縁から 4 離す (F44: 旧は縁の線に重なった)。名前 (60〜) から 5 離す
                LifeDisc(badge, left);
                ptip += "\n" + (left == null ? "期限なし" : "あと" + left.Value + "ターンで消える") + (DollUi.HasGrowth(q.Def) ? "・火勢+" + DollUi.Growth(st, q) : "");   // 火勢はダメージ・ブロックを持つ人形だけ (2026-09-24 E8)
            }
            Tooltip.Attach(chip.gameObject, delegate { return ptip; });
        }

        // ---- 手札 (扇) ----

        /// <summary>出せない札を押した (2026-09-17 ⑦): 札が首を振り、理由 (エナジー不足・仕込む札・従者がいない・拘束) を浮かせる。エナジー不足なら輪も朱に光る</summary>
        static void CannotPlay(GameRoot g, BattleView.HandCard hc, CardInstance c)
        {
            var st = g.Rs != null ? g.Rs.Combat : null;
            var fx = g.FxLayer;
            if (st == null || st.Phase != CombatPhases.PlayerTurn || g.Pending != null || hc.Rt == null) return;
            int cost = c.Def.Cost; try { cost = Effects.EffectiveCost(st, c); } catch (Exception) { }
            string why = null; bool energy = false, light = false;
            var cap = Combat.PlayCapOf(st);
            if (!Effects.IsPlayableFromHand(c, st)) why = "仕込む札 (プレイできない)";
            else if (cap != null && (st.Player.PlaysThisTurn ?? 0) >= cap.Value) why = (st.Player.Restrain > 0 && cap.Value == Combat.RESTRAIN_PLAY_CAP ? "拘束中は" : "首輪で") + "1ターン" + cap.Value + "枚まで";   // 2026-09-29 p12: 扇で沈むようになったので理由も
            else if (cost > st.Player.Energy) { why = "エナジー不足"; energy = true; }
            else if ((c.Def.LightCost ?? 0) > (st.Player.Light ?? 0)) { why = "灯が足りない (あと" + (c.Def.LightCost.Value - (st.Player.Light ?? 0)) + ")"; light = true; } // 号令 (白 2026-09-20)
            else if (!Effects.RetainerRequirementMet(st, c)) why = "場に人形がいない";   // 「従者」→「人形」(2026-09-24 T3)
            else why = "いまは出せない";
            Tween.Shake(hc.Rt, 7f, 0.25f);
            Audio.Ui("click", 0.5f);
            if (fx != null) Tween.Float(fx, Tween.CenterIn(hc.Rt, fx) + new Vector2(0f, 150f), why, UiKit.ColBad, 24, 30f, 1.0f);
            if (energy)
            {
                var orb = g.Anchor("energy");
                if (orb != null)
                {
                    Tween.Shake(orb, 6f, 0.25f);
                    var oi = orb.GetComponentInChildren<Image>();
                    if (oi != null) Tween.Flash(oi, PaperFx.Rose, 0.4f);
                }
            }
            if (light) LightUi.Insufficient();   // 灯籠も首を振って硝子が朱に光る (エナジー不足の輪と対。2026-09-20)
        }

        /// <summary>カードの吹き出し: 本文は見えているので用語解説だけ (無ければ出さない)</summary>
        /// <summary>札の用語の説明。仕込み札は「仕込み札」を先頭に必ず出す (2026-09-30 F56: 札の本文に「仕込み札」の字は無いので、手札のホバー・長押しの拡大から
        /// 「手札から直接は出せない・準備・期限切れ」の説明に届かなかった。タイプの帯の文字は CardView が別に描く)</summary>
        public static string KeywordsOnly(CardDef def)
        {
            if (def == null) return null;
            return KeywordsOnly(CardText.Body(def) + " " + CardText.Notes(def), def.Type == "reaction" ? "仕込み札" : null);
        }

        public static string KeywordsOnly(string text, string lead = null)
        {
            var terms = KeywordHelp.FindIn(text);
            if (lead != null && KeywordHelp.Terms.ContainsKey(lead)) { terms.Remove(lead); terms.Insert(0, lead); }   // 4語で打ち切る前に先頭へ
            if (terms.Count == 0) return null;
            var lines = new List<string>();
            for (int i = 0; i < terms.Count && i < 4; i++) lines.Add(UiKit.ColorTag(PaperFx.GoodInk, "<b>" + terms[i] + "</b>") + " " + KeywordHelp.Terms[terms[i]]);
            return string.Join("\n", lines.ToArray());
        }

        static bool _dragging;
        // スマホで敵を狙う札 (選択式でない単体の札) をドラッグ中: 札は手札に留まり、指まで狙いの矢が伸びる (2026-09-29 p09。BattleView.UpdateAimArrow)
        static bool _dragAim;

        /// <summary>
        /// 三周目 r3 (段1 の試し撮り 2026-10-01 レーン A の部品。2026-10-02 から r3 の PC とスマホで常に) の、触れて上がった札の下の透明な的。札を沈めると見えるのは上の約 129 だけで、
        /// 触れると札は HandLift＋沈め 上がる (PC は二周目と同じ高さ) ので、指 (ポインタ) の下から札が抜けて PointerExit → 下りる → また Enter の揺れになる
        /// (上がった札の下端はキャンバスの y 76。見えていた帯 0〜114 のうち 0〜76 で起きる)。札の下端から沈めた量＋70 だけ下へ伸ばした透明な Image を札の子に置き、
        /// 上がった札が元の場所の上も覆うようにする (休んでいる時は画面の下の外)。子なので Enter/Exit・クリック・ドラッグは札の EventTrigger に届く。
        /// 旗が無い時は作らない (今の 19 の沈めでも札の下 65 で同じ揺れがありうるが、二周目の画と操作は変えない)
        /// </summary>
        public static void R3A_HoverCatch(RectTransform card)
        {
            var catcher = UiKit.NewRect("r3a-hover-catch", card);
            UiKit.Anchor(catcher, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -(HandLift + HandSink)), new Vector2(0f, 0f));
            var img = catcher.gameObject.AddComponent<Image>();
            img.color = Color.clear;   // 見えない。当たり判定だけ (敵の入れ物の当たりと同じ作法)
            img.raycastTarget = true;
            catcher.SetAsFirstSibling();
        }

        public static void HookHandCard(GameRoot g, BattleView.HandCard hc, CardInstance c)
        {
            var rt = hc.Rt;
            var et = rt.gameObject.AddComponent<EventTrigger>();
            if (R3) R3A_HoverCatch(rt);   // r3 (PC とスマホ): 触れて上がった札の下に、沈めていた時の札の場所を受ける透明な的 (上がった札が指から離れて下りる揺れを止める)
            // 長押し 0.5 秒で拡大表示 (本家の SingleCardViewPopup。右クリックは伏せるに使っているので手札は長押しだけ)
            CardPopup.Attach(g, rt, c, delegate { return g.Rs != null ? g.Rs.Combat : null; }, false);
            // ドラッグ: カードを持ち上げて敵に落とすと対象指定して即プレイ、戦場に落とすとプレイ、手札に戻すと取り消し
            var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDrag.callback.AddListener(delegate
            {
                if (!hc.Playable || CardPopup.IsOpen) return;
                _dragging = true;
                // スマホで敵を狙う札は指に付けず、手札の元の位置・等倍に留める (数字と狙った敵の帳面が親指に隠れない。p09)。PC とそれ以外の札は今までどおり 0.8倍で指に付く
                _dragAim = UiKit.Phone && (c.Def.Modes == null || c.Def.Modes.Count == 0) && Effects.CardNeedsTarget(c);
                if (g.Battle != null) g.Battle.ClearAimArrow();   // 前のドラッグの残り (EndDrag が来ないまま組み直された時) を消す
                rt.SetAsLastSibling();
                rt.localRotation = Quaternion.identity;
                if (_dragAim) { rt.anchoredPosition = hc.BasePos; rt.localScale = Vector3.one * CardScale; CardView.SetKeyNumVisible(rt, true); }   // 押した時の持ち上げ (+70・1.18倍) を戻す。以後は矢の部品が押さえる
                else rt.localScale = Vector3.one * 0.8f;
                try { if (g.Rs != null) LightUi.PreviewFor(g.Rs.Combat, c); } catch (Exception) { }   // 灯籠に「−2 → 4」などの予告 (2026-09-20)
            });
            et.triggers.Add(beginDrag);
            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener(delegate (BaseEventData d)
            {
                if (!_dragging) return;
                var pd = d as PointerEventData;
                var parent = rt.parent as RectTransform;
                Vector2 local;
                if (!_dragAim && pd != null && parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pd.position, null, out local)) rt.anchoredPosition = local;
                // 敵の上に来たら、その敵に対する実値で札を描き直す (急所・装甲が数字に乗る)
                if (pd != null && g.Battle != null && g.Rs != null && g.Rs.Combat != null)
                {
                    int over = EnemyUnderPointer(pd);
                    try { g.Battle.RefreshHandCard(g, g.Rs.Combat, hc, over); } catch (Exception) { }
                    // 狙いの矢: 札の上端から指まで。指の下の敵には真鍮の縁 (p09)
                    if (_dragAim) { try { g.Battle.UpdateAimArrow(g, g.Rs.Combat, hc, pd.position, over); } catch (Exception e) { Debug.LogException(e); } }
                }
            });
            et.triggers.Add(drag);
            var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDrag.callback.AddListener(delegate (BaseEventData d)
            {
                if (!_dragging) return;
                _dragging = false;
                if (_dragAim && g.Battle != null) g.Battle.ClearAimArrow();   // 矢と縁を片付け、札の押さえを外す (p09)
                _dragAim = false;
                LightUi.HidePreview();
                var pd = d as PointerEventData;
                int enemyIdx = pd != null ? EnemyUnderPointer(pd) : -1;
                bool overField = pd != null && pd.position.y > DropLineScreen(rt);   // r3 の PC は上がった札の上端の 15.6 下 (=二周目と同じ)・スマホは沈めた手札の上端＋111 (二周目は画面の 36%)
                if (c.Def.Modes != null && c.Def.Modes.Count > 0)
                {
                    if (overField) { g.PreferredTarget = enemyIdx; g.ModeChoiceUid = c.Uid; g.Rebuild(); }
                    else { Tween.Move(rt, hc.BasePos, 0.15f); Tween.Scale(rt, Vector3.one * CardScale, 0.15f); rt.localRotation = Quaternion.Euler(0f, 0f, hc.BaseRot); CardView.SetKeyNumVisible(rt, true); RestoreOrder(rt); }
                    return;
                }
                if (enemyIdx >= 0 || overField)
                {
                    if (enemyIdx >= 0) g.PreferredTarget = enemyIdx;
                    PlayCard(g, c, null);
                    return;
                }
                Tween.Move(rt, hc.BasePos, 0.15f);
                Tween.Scale(rt, Vector3.one * CardScale, 0.15f);
                rt.localRotation = Quaternion.Euler(0f, 0f, hc.BaseRot);
                CardView.SetKeyNumVisible(rt, true);
                RestoreOrder(rt);
            });
            et.triggers.Add(endDrag);
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(delegate
            {
                if (_dragging) return;
                Audio.Hover();
                rt.SetAsLastSibling();
                Tween.Scale(rt, Vector3.one * R3U_HoverScale, 0.12f, Ease.OutQuad);
                Tween.Move(rt, hc.BasePos + new Vector2(0f, HandLift + HandSink), 0.12f, Ease.OutQuad);   // 沈めた分を足す = 持ち上げた札は今と同じ高さ (HD-2D 見本 2026-09-30 P20)。r3 のスマホは +130 (親指より上)
                CardView.SetKeyNumVisible(rt, false);   // r3: 上がっている間は要の数字の札を隠す (本文に同じ数字がある。仕様 §4)
                rt.localRotation = Quaternion.identity;
                try { if (g.Rs != null) LightUi.PreviewFor(g.Rs.Combat, c); } catch (Exception) { }   // 灯籠に予告 (PC のホバー。2026-09-20)
            });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(delegate
            {
                if (_dragging) return;
                LightUi.HidePreview();
                CardView.SetKeyNumVisible(rt, true);
                Tween.Scale(rt, Vector3.one * CardScale, 0.12f, Ease.OutQuad);
                Tween.Move(rt, hc.BasePos, 0.12f, Ease.OutQuad);
                rt.localRotation = Quaternion.Euler(0f, 0f, hc.BaseRot);
                RestoreOrder(rt);
            });
            et.triggers.Add(exit);
            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(delegate (BaseEventData d)
            {
                var pd = d as PointerEventData;
                if (CardPopup.ClickSuppressed || CardPopup.IsOpen) return;   // 長押しで拡大表示を開いた直後の離しはプレイしない
                if (pd != null && pd.button == PointerEventData.InputButton.Right)
                {
                    if (hc.Settable) g.DoCombat(new Command_SetCard { CardUid = c.Uid });
                    return;
                }
                if (pd != null && pd.dragging) return;
                if (c.Def.Modes != null && c.Def.Modes.Count > 0) { g.ModeChoiceUid = c.Uid; g.Rebuild(); return; }
                if (hc.Playable) PlayCard(g, c, null);
                else if (hc.Settable) { g.ModeChoiceUid = c.Uid; g.Rebuild(); }
                else CannotPlay(g, hc, c);
            });
            et.triggers.Add(click);

            // 伏せられる札には「伏せる」ボタン (ホバー中だけ・カードの足元)
            if (hc.Settable)
            {
                var sb = UiKit.Btn(rt, "仕込む", delegate { g.DoCombat(new Command_SetCard { CardUid = c.Uid }); }, 15, true, PaperFx.ManaLight);
                var sle = sb.GetComponent<LayoutElement>();
                if (sle != null) UnityEngine.Object.Destroy(sle);
                var srt = sb.GetComponent<RectTransform>();
                UiKit.Anchor(srt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-60f, -22f), new Vector2(60f, 22f));
                sb.gameObject.SetActive(false);
                var showSet = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                showSet.callback.AddListener(delegate { if (sb != null) sb.gameObject.SetActive(true); });
                et.triggers.Add(showSet);
                var hideSet = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                hideSet.callback.AddListener(delegate { if (sb != null) sb.gameObject.SetActive(false); });
                et.triggers.Add(hideSet);
            }
        }

        /// <summary>ポインタの下にある敵パネルの添字 (無ければ -1)</summary>
        static int EnemyUnderPointer(PointerEventData pd)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pd, results);
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                for (var t = go.transform; t != null; t = t.parent)
                {
                    int idx;
                    if (t.name.StartsWith("enemy") && int.TryParse(t.name.Substring(5), out idx)) return idx;
                }
            }
            return -1;
        }

        /// <summary>カードをプレイする。行き先の演出 (敵へ飛ぶ→捨て札) は GameRoot.SubmitIfReady が LastPlayed を記録し BattleView.SyncHand が行う</summary>
        public static void PlayCard(GameRoot g, CardInstance c, int? modeIndex)
        {
            g.BeginPlay(c, modeIndex);
        }

        static void RestoreOrder(RectTransform rt)
        {
            // 名前 handN の N で元の並びに戻す
            var name = rt.name;
            int idx;
            if (name.StartsWith("hand") && int.TryParse(name.Substring(4), out idx)) rt.SetSiblingIndex(Mathf.Min(idx, rt.parent.childCount - 1));
        }

        // ---- 山札・捨て札・消滅・ターン終了 ----

        /// <summary>山札の札 (左下)・捨て札の札 (右下)・消滅の札 (捨て札の真上。1枚以上ある時だけ)。2026-09-29 p12:
        /// 四辺の余白 (UiKit.Edge) にそろえ、捨て札と消滅を1枚に並べた「0 捨て札 | 0 消滅」(区切りの棒が「1」に読めた) を2枚に分けた。
        /// 絵は小さな札 (山札＝裏・捨て札＝表・消滅＝PixelLab の exhaust)。スマホは指で押せる高さ 52 (消滅は 36＝ターン終了との間を 24 以上空ける)</summary>
        static void BuildPiles(GameRoot g, RectTransform root, GameState st)
        {
            var p = st.Player;
            bool ph = UiKit.Phone;
            float e = UiKit.Edge, pw = ph ? 150f : 130f, phh = ph ? 52f : 40f;
            // r3 のスマホ (2026-10-02 仕様 §3): 山札・捨て札は 10 下 (下から 14)、消滅はそのすぐ上 (間 4)。輪・灯籠を 48 下げた分の場所
            float py = ph && R3 ? R3U_PilesYPhone : 24f, exGap = ph && R3 ? 4f : 8f;
            Pile(g, root, new Vector2(0f, 0f), new Vector2(e, py), new Vector2(pw, phh), "draw", "山札", p.DrawPile.Count, delegate { g.ViewPile = "draw"; g.Rebuild(); }, "pile-draw");
            Pile(g, root, new Vector2(1f, 0f), new Vector2(-(pw + e), py), new Vector2(pw, phh), "discard", "捨て札", p.DiscardPile.Count, delegate { g.ViewPile = "discard"; g.Rebuild(); }, "pile-discard");
            if (p.ExhaustPile.Count > 0)   // 本家と同じく、消滅は1枚以上ある時だけ (1ターン目の右下の角を静かに保つ)
                Pile(g, root, new Vector2(1f, 0f), new Vector2(-(pw + e), py + phh + exGap), new Vector2(pw, 36f), "exhaust", "消滅", p.ExhaustPile.Count, delegate { g.ViewPile = "exhaust"; g.Rebuild(); }, "pile-exhaust");
            if (g.ViewPile != null) BuildPileViewer(g, root, st);
        }

        static void Pile(GameRoot g, RectTransform root, Vector2 anchor, Vector2 offset, Vector2 size, string kind, string label, int count, Action onClick = null, string anchorName = null)
        {
            bool ph = UiKit.Phone;
            var rt = UiKit.NewRect("pile-" + kind, root);
            if (anchorName != null) g.RegisterAnchor(anchorName, rt);
            UiKit.Anchor(rt, anchor, anchor, offset, offset + size);
            // 手置きの傾き: 左の山札 −1°・右の捨て札 +1°・その上の消滅 −1° (交互に)
            rt.localRotation = Quaternion.Euler(0f, 0f, kind == "discard" ? 1f : -1f);
            var frame = PaperFx.Sheet(rt, PaperFx.Tag2, "paper");
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            frame.raycastTarget = onClick != null;
            if (onClick != null)
            {
                var pb = rt.gameObject.AddComponent<Button>();
                pb.targetGraphic = frame;
                pb.transition = Selectable.Transition.None;
                pb.onClick.AddListener(delegate { onClick(); });
            }
            var row = UiKit.NewRect("row", rt);
            UiKit.Stretch(row, 0f, 0f, 0f, 0f);
            var hg = UiKit.Horz(row, 6, 0);
            hg.padding = new RectOffset(12, 12, 0, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            if (kind == "exhaust")
            {   // 消滅: PixelLab の絵 (煙の立つ札。32 ドット＝等倍)
                var ic = UiKit.Icon(row, "exhaust", 32f);
                UiKit.Le(ic, 32f, 32f, 32f, 32f);
            }
            else MiniCardIcon(row, kind == "draw", count);
            float lineH = Mathf.Min(size.y - 4f, ph ? 38f : 30f);
            var cnt = UiKit.Deco(row, count.ToString(), ph ? 19 : 17, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(cnt, -1f, lineH, -1f, lineH);
            var lb = UiKit.Txt(row, label, ph ? 15 : 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(lb, -1f, lineH, -1f, lineH);
            // 夜の札 (ui=night・2026-09-30 P20 2周目「本家っぽく」②「画面の端と上下が沈む」): 山札・捨て札・消滅の札も舞台の上に常に出る札
            // (裁定③「紙のまま残すのは手札・確認の窓・メニューだけ」)。画面の下の両隅で紙の札だけが明るく立っていた。小さな札の絵 (pile-glyph) は絵なので紙のまま
            PaperFx.Nightify(rt);
        }

        /// <summary>山札・捨て札の札の絵 (2026-09-29 p12): 墨一色の 16 ドットの「draw」は ■ に見え、捨て札には消滅の ✕ が付いていた。
        /// 山札＝裏向きの小さな札 (夜＋真鍮の枠。手札が山札から飛ぶ時の裏 BattleView.CardBack と同じ物)、捨て札＝表向きの札 (紙＋墨の枠＋夜の窓)。
        /// 2枚以上なら2枚を重ね (山札は右下へずらし、捨て札は −8°/+6° に傾ける)、1枚なら1枚、0枚なら枠だけ (まもなく切り直し／空)</summary>
        static void MiniCardIcon(Transform row, bool faceDown, int count)
        {
            bool ph = UiKit.Phone;
            float cw = ph ? 24f : 20f, ch = ph ? 34f : 28f, dx = ph ? 5f : 4f, dy = ph ? 4f : 3f;
            var box = UiKit.NewRect("pile-glyph", row);
            UiKit.Le(box, cw + dx + 2f, ch + dy + 2f, cw + dx + 2f, ch + dy + 2f);
            int n = count >= 2 ? 2 : 1;
            bool empty = count <= 0;
            for (int i = 0; i < n; i++)
            {
                bool front = i == n - 1;
                float x, y, rot;
                if (faceDown) { x = n == 1 ? dx / 2f + 1f : (i == 0 ? 1f : 1f + dx); y = n == 1 ? dy / 2f + 1f : (i == 0 ? 1f + dy : 1f); rot = 0f; }
                else { x = n == 1 ? dx / 2f + 1f : (i == 0 ? 0f : dx + 1f); y = dy / 2f + 1f; rot = n == 1 ? 6f : (i == 0 ? -8f : 6f); }   // 表: 左の札 −8°・右の札 +6° (扇に開いた2枚)
                var c = UiKit.NewRect("mini" + i, box);
                UiKit.Anchor(c, Vector2.zero, Vector2.zero, new Vector2(x, y), new Vector2(x + cw, y + ch));
                c.localRotation = Quaternion.Euler(0f, 0f, rot);
                if (faceDown)
                {
                    if (!empty) MiniFill(c, PaperFx.Night, 0f, 0f, 0f, 0f);
                    MiniFrame(c, PaperFx.Brass, empty ? 0f : 2f);
                    if (!empty && front) { var dot = MiniFill(c, PaperFx.Brass, 0f, 0f, 0f, 0f); var drt = dot.rectTransform; drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f); drt.sizeDelta = new Vector2(ph ? 5f : 4f, ph ? 5f : 4f); drt.anchoredPosition = Vector2.zero; }
                }
                else
                {
                    if (!empty) MiniFill(c, PaperFx.Paper3, 0f, 0f, 0f, 0f);
                    MiniFrame(c, empty ? PaperFx.InkSoft : PaperFx.Ink, 0f);
                    if (!empty && front) MiniFill(c, PaperFx.Window, 3f, 3f, ch * 0.18f, ch * 0.42f);   // 挿絵の窓 (夜) = 表向きの札に読める
                }
            }
        }

        static Image MiniFill(RectTransform parent, Color col, float l, float r, float top, float bottom)
        {
            var img = UiKit.Pan(parent, col, "fill");
            img.raycastTarget = false;
            UiKit.Stretch(img.rectTransform, l, r, top, bottom);
            return img;
        }

        /// <summary>1 単位の枠を4本の線で (inset＝外周からの距離)</summary>
        static void MiniFrame(RectTransform parent, Color col, float inset)
        {
            for (int side = 0; side < 4; side++)
            {
                var ln = UiKit.Pan(parent, col, "frame");
                ln.raycastTarget = false;
                var lr = ln.rectTransform;
                if (side < 2) UiKit.Anchor(lr, new Vector2(0f, side), new Vector2(1f, side), new Vector2(inset, side == 0 ? inset : -inset - 1f), new Vector2(-inset, side == 0 ? inset + 1f : -inset));
                else UiKit.Anchor(lr, new Vector2(side - 2, 0f), new Vector2(side - 2, 1f), new Vector2(side == 2 ? inset : -inset - 1f, inset), new Vector2(side == 2 ? inset + 1f : -inset, -inset));
            }
        }

        /// <summary>山札 (名前順=引き順は伏せたまま) / 捨て札 / 消滅置き場の一覧モーダル</summary>
        static void BuildPileViewer(GameRoot g, RectTransform root, GameState st)
        {
            var inner = Modal(root, 1500f, 800f, "pileViewer");
            var tabs = UiKit.NewRect("tabs", inner);
            var tle = UiKit.Le(tabs, -1f, 48f, -1f, 48f);
            tle.flexibleHeight = 0f;
            var tg = UiKit.Horz(tabs, 10, 0);
            tg.childAlignment = TextAnchor.MiddleLeft;
            tg.childForceExpandWidth = false;
            tg.childForceExpandHeight = false;
            string[] kinds = { "draw", "discard", "exhaust" };
            string[] labels = { "山札 " + st.Player.DrawPile.Count, "捨て札 " + st.Player.DiscardPile.Count, "消滅 " + st.Player.ExhaustPile.Count };
            for (int i = 0; i < kinds.Length; i++)
            {
                string k = kinds[i];
                var b = UiKit.Btn(tabs, labels[i], delegate { g.ViewPile = k; g.Rebuild(); }, 18, true, g.ViewPile == k ? PaperFx.BrassLight : Color.white);
                SetSize(b, 200f, 44f);
            }
            IReadOnlyList<CardInstance> pile = g.ViewPile == "discard" ? st.Player.DiscardPile : g.ViewPile == "exhaust" ? st.Player.ExhaustPile : st.Player.DrawPile;
            var list = new List<CardInstance>(pile);
            if (g.ViewPile == "draw") list.Sort((a, b) => string.CompareOrdinal(a.Def.Name, b.Def.Name)); // 引き順は伏せたまま
            var content = UiKit.Scroll(inner, true, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 12, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 300f, -1f, 300f, -1f, 1f);
            var vg = content.GetComponent<VerticalLayoutGroup>();
            if (vg != null) UnityEngine.Object.DestroyImmediate(vg);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CardView.W * 0.8f, CardView.H * 0.8f);
            grid.spacing = new Vector2(14f, 14f);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.childAlignment = TextAnchor.UpperLeft;
            if (list.Count == 0)
            {
                var none = UiKit.Txt(inner, "（空）", 18, PaperFx.InkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(none, -1f, 40f, -1f, 40f);
            }
            for (int i = 0; i < list.Count; i++)
            {
                var cell = UiKit.NewRect("cell", content);
                var cv = CardView.Build(cell, list[i], st, true, true, "pile-card");
                cv.localScale = Vector3.one * 0.8f;
                CardPopup.Attach(g, cv, list[i], delegate { return g.Rs != null ? g.Rs.Combat : null; }, true);
            }
            CenteredButton(inner, "閉じる", delegate { g.ViewPile = null; g.Rebuild(); }, 18, 260f, 50f);
        }

        /// <summary>手札に「今出せる札」か「仕込める札」が1枚でもあるか (ターン終了の合図の判定。ギアは数えない)。出せるかは BattleView.CanActNow＝扇の沈みと同じ関数</summary>
        static bool AnyMoveLeft(GameRoot g, GameState st)
        {
            foreach (var c in st.Player.Hand)
            {
                if (BattleView.CanActNow(g, st, c)) return true;
                try { if (SetBase.CanSetCard(st, c.Uid)) return true; } catch (Exception) { }
            }
            return false;
        }

        /// <summary>
        /// 打てる手が尽きた時のターン終了の脈 (2026-09-29 p12。本家 StS1 の EndTurnButton が hand.canUseAnyCard() で光るのと同じ合図)。
        /// 1.2Hz で縁の不透明度 0.45↔1.0・ボタンの大きさ 1.00↔1.03。最初の 0.4 秒は止める (最後の札が飛び終わる前には光らせない)。
        /// Time.unscaledDeltaTime で進む (ヒットストップの timeScale に引きずられない)。ボタンを押した後の順送り (古い盤面の上の敵フェーズ) では、
        /// 組んだ時の GameState が g.Rs.Combat と同じものでなくなるので止める
        /// </summary>
        public class EndTurnPulse : MonoBehaviour
        {
            public Image Edge; public GameObject Note; public GameRoot G; public GameState Built;
            float _t;
            void Update()
            {
                if (G == null || G.Rs == null || !ReferenceEquals(G.Rs.Combat, Built))
                {
                    transform.localScale = Vector3.one;
                    if (Edge != null) { var c0 = Edge.color; Edge.color = new Color(c0.r, c0.g, c0.b, 0f); }
                    if (Note != null) Note.SetActive(false);   // 「打てる札なし」も順送りの間は出さない
                    enabled = false;
                    return;
                }
                _t += Tween.UnscaledDt;   // det の撮影では 1/60 秒 (2026-09-30 HD-2D 見本 P00)
                float live = _t - 0.4f;
                if (live <= 0f) return;
                float s = 0.5f - 0.5f * Mathf.Cos(live * 1.2f * Mathf.PI * 2f);
                transform.localScale = Vector3.one * (1f + 0.03f * s);
                if (Edge != null)
                {
                    float a = (0.45f + 0.55f * s) * Mathf.Clamp01(live / 0.2f);   // 現れる時だけ 0.2 秒で薄く入る
                    var c = Edge.color; Edge.color = new Color(c.r, c.g, c.b, a);
                }
            }
        }

        /// <summary>r3 のスマホの左下の列 (仕様 §3): 山札の下端 (下から)・エナジーの輪と灯籠の台座の下端 (下から。二周目の 116 より 48 下)・ターン終了の上端の下限 (上から)・高さ</summary>
        const float R3U_PilesYPhone = 14f, R3U_OrbYPhone = 68f, R3U_EndTopPhone = 465f, R3U_EndHPhone = 64f;

        /// <summary>r3 のスマホのターン終了の上端 (キャンバスの上から): 敵の帳の最下端＋6 (4体の帳と重ならない)。下端は手札の上端より上 (仕様 §3)</summary>
        static float R3U_EndTurnTopPhone(GameRoot g, Vector2 cs)
        {
            float top = R3U_EndTopPhone;
            var bv = g.Battle;
            var st = g.Rs != null ? g.Rs.Combat : null;
            if (bv != null && st != null)
                for (int i = 0; i < st.Enemies.Count; i++)
                {
                    Rect r;
                    if (PanelChildRect(bv.EnemyPanel(i), "strip", 0f, out r)) top = Mathf.Max(top, cs.y - r.yMin + 6f);
                }
            float handTop = cs.y - (HandY + CardView.H * CardScale);
            return Mathf.Min(top, handTop - R3U_EndHPhone - 4f);
        }

        static void BuildEndTurn(GameRoot g, RectTransform root, GameState st)
        {
            bool myTurn = st.Phase == CombatPhases.PlayerTurn && g.Pending == null;
            float e = UiKit.Edge;
            bool r3ph = R3 && UiKit.Phone;
            var csE = CanvasSize(root);
            // 打てる手が尽きた (2026-09-29 p12): 自分の番で、手札に出せる札も仕込める札も無い (ギアは数えない＝本家もポーションは数えない)。
            // 占術・満ち潮の書庫・灯の火床・ギアの窓が開いている間は合図を出さない
            bool idle = myTurn && st.PendingScry == null && g.RetainChoice == null && !g.HearthChoice && g.GearPending == null && g.ModeChoiceUid == null && !AnyMoveLeft(g, st);
            Image nudgeEdge = null;
            Vector2 bMin = new Vector2(-(230f + e), 146f), bMax = new Vector2(-e, 210f);   // 右端は四辺の余白 (PC 32・スマホ 24)
            if (r3ph)
            {   // r3 のスマホ: 上端 = max(465, 敵の帳の最下端＋6)・高さ 64 (仕様 §3)
                float top = R3U_EndTurnTopPhone(g, csE);
                bMax = new Vector2(-e, csE.y - top); bMin = new Vector2(-(230f + e), csE.y - top - R3U_EndHPhone);
            }
            if (idle)
            {   // ボタンの 6 外に真鍮の縁 (行動中の敵の帳面の縁と同じ作法＝紙の縁が脈打つ。光の玉は使わない＝紙の UI は光らない)。ボタンより先に置く＝後ろ
                nudgeEdge = PaperFx.Sheet(root, PaperFx.Tag, "endturn-edge", new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0f));
                nudgeEdge.raycastTarget = false;
                UiKit.Anchor(nudgeEdge.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), bMin - new Vector2(6f, 6f), bMax + new Vector2(6f, 6f));
                nudgeEdge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1f);
            }
            // 灯の火床 (2026-09-20 夜「枚数を選ぶ」): 火床が場にあり灯3以上なら、ターン終了の前に何枚火種にするかの窓を挟む
            var b = UiKit.Btn(root, "ターン終了", delegate
            {
                StartEndTurn(g);
            }, 21, myTurn, myTurn ? PaperFx.BrassLight : Color.white);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            var brt = b.GetComponent<RectTransform>();
            UiKit.Anchor(brt, new Vector2(1f, 0f), new Vector2(1f, 0f), bMin, bMax);
            brt.localRotation = Quaternion.Euler(0f, 0f, -1f);
            var bt = b.GetComponentInChildren<TMP_Text>();
            if (bt != null && UiKit.FontDeco != null) { bt.font = UiKit.FontDeco; bt.characterSpacing = 4f; }
            // 旧・注記「手札 N · からくり a/b」は撤去 (2026-09-29 p12): 手札の数は扇で見え、からくりの数は自分の札の見出しと同じ値の繰り返しだった
            if (idle)
            {
                var note = PaperFx.NightNote(root, "打てる札なし", 13, 240f, false, "endturn-note");
                note.anchorMin = note.anchorMax = new Vector2(1f, 0f); note.pivot = new Vector2(1f, 0f);
                note.anchoredPosition = new Vector2(-e, 222f);
                if (r3ph) { note.pivot = new Vector2(1f, 0.5f); note.anchoredPosition = new Vector2(-(230f + e + 12f), (bMin.y + bMax.y) / 2f); }   // r3 のスマホ: ターン終了の左 (真上は4体の帳に重なる)
                var pulse = b.gameObject.AddComponent<EndTurnPulse>();
                pulse.Edge = nudgeEdge; pulse.Note = note.gameObject; pulse.G = g; pulse.Built = st;
            }
            // 火種を全部撃つ (2026-09-21 【G】人間ラン#15「火種のクリック89回」): 手札に火種が2枚以上ある時だけ。自動プレイはしない (灯火の炉などのために持つ選択を残す)。
            // PC はターン終了の左 (間 16)、スマホはターン終了の真上 (手札の扇の上に重ねない。2026-09-29 p12)
            int sparks = 0;
            foreach (var hc in st.Player.Hand) if (hc.Def.SparkToken == true) sparks++;
            if (sparks >= 2)
            {
                var sb = UiKit.Btn(root, "火種を全部撃つ (" + sparks + ")", delegate { g.PlayAllSparks(); }, 17, myTurn, myTurn ? PaperFx.BrassLight : Color.white);
                var sle = sb.GetComponent<LayoutElement>();
                if (sle != null) UnityEngine.Object.Destroy(sle);
                var srt = sb.GetComponent<RectTransform>();
                if (r3ph) UiKit.Anchor(srt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-(230f + e + 12f + 216f), bMax.y - 48f), new Vector2(-(230f + e + 12f), bMax.y));   // r3 のスマホ: ターン終了の左 −12・幅 216・上端をそろえる
                else if (UiKit.Phone) UiKit.Anchor(srt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-(230f + e), 252f), new Vector2(-e, 300f));
                else UiKit.Anchor(srt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-(460f + e), 152f), new Vector2(-(246f + e), 204f));
                srt.localRotation = Quaternion.Euler(0f, 0f, -1f);
            }

            // 灯の器 (2026-09-20 灯の表示・案B「真鍮のランタン」): 白の色を持つリーダーは常に、他は灯1以上で。
            // エナジーの輪は常に左の余白 (UiKit.Edge＝山札の真上。2026-09-29 p12: 旧はスマホで灯籠を出す時だけ 16 へ寄せ、他は 96＝リーダーで約110px 動いた)。
            // 灯籠は輪の右 12 に置く (PC 3px/ドット＝96×144 で中心 220、スマホ 2.5px/ドット＝80×120 で中心 204)。どちらも台座の下端を輪の下端 (116) に揃える
            bool lantern = LightUi.ShouldShow(g.Rs, st);
            float orbX = e;
            // 分母＝このターンに補充した上限 (2026-09-29 p20): 上限+1 (このはのパッシブ・古根の杯などボスレリック・ターン途中の芽吹き) は次のターンの補充から効くので、
            // 旧・EnergyMax を分母にすると1ターン目が「3 / 4」・弧 3/4 で「もう1使った」ように見えた。EnergyMaxAtTurnStart には大樹の心の上乗せ (上限参照札が読む値) が
            // 足してあるので引く (引かないと大樹の心を持つ間ずっと「3 / 4」に戻る)。弧・「/ N」・目盛りの数はすべてこの値を読む
            int turnMax = st.Player.EnergyMaxAtTurnStart > 0 ? st.Player.EnergyMaxAtTurnStart - (st.EnergyMaxRefBonus ?? 0) : st.Player.EnergyMax;   // 0 は旧セーブの欠落
            if (turnMax < 1) turnMax = 1;
            // エナジーの輪 (紙の円盤に真鍮の弧)
            float orbY = r3ph ? R3U_OrbYPhone : 116f;   // r3 のスマホは 48 下 (足元の帳の下。仕様 §3)
            var sun = UiKit.NewRect("energyOrb", root);
            UiKit.Anchor(sun, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(orbX, orbY), new Vector2(orbX + 128f, orbY + 128f));
            sun.localRotation = Quaternion.Euler(0f, 0f, -3f);
            var disc = UiKit.NewRect("disc", sun);   // 最初の子のまま (CannotPlay の Flash が GetComponentInChildren<Image> で円盤を朱に光らせる)
            UiKit.Stretch(disc, 6f, 6f, 6f, 6f);
            var dImg = disc.gameObject.AddComponent<Image>();
            dImg.sprite = PaperFx.Disc(); dImg.preserveAspect = true; dImg.raycastTarget = false;
            dImg.color = st.Player.Energy > 0 ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            // 空きの溝 (2026-09-29 p20): 使った分は暗い真鍮の溝として残る＝「使った分＝くすんだ真鍮／残り＝明るい真鍮」。旧は弧だけで、空いた所に夜の舞台が透けて輪が欠けた「C」に見えた
            var track = UiKit.NewRect("track", sun);
            UiKit.Stretch(track, 0f, 0f, 0f, 0f);
            var tImg = track.gameObject.AddComponent<Image>();
            tImg.sprite = PaperFx.Ring(7); tImg.color = PaperFx.BrassInk; tImg.raycastTarget = false;
            var arc = UiKit.NewRect("arc", sun);
            UiKit.Stretch(arc, 0f, 0f, 0f, 0f);
            var aImg = arc.gameObject.AddComponent<Image>();
            // エナジーの輪＝真鍮 (2026-09-16 ユーザー「エナジー表記は黄色系がいい」)。raycastTarget は旧はコメントの中に入って効いていなかった (2026-09-29 p20)
            aImg.sprite = PaperFx.Ring(7); aImg.color = PaperFx.Brass; aImg.raycastTarget = false;
            aImg.type = Image.Type.Filled; aImg.fillMethod = Image.FillMethod.Radial360; aImg.fillOrigin = 2; aImg.fillClockwise = true;
            aImg.fillAmount = Mathf.Clamp01((float)st.Player.Energy / turnMax);   // 一時マナで分母を超えても弧は満タンのまま (超えた分は外の小玉)
            // 目盛り (2026-09-29 p20): 分母が 2〜12 の時、上から右回りに 360/分母 度ごとの紙色の切れ目＝帯を分母の数に割る (真鍮の上でも溝の上でも見える)
            if (turnMax >= 2 && turnMax <= 12)
            {
                var ticks = UiKit.NewRect("ticks", sun);
                UiKit.Stretch(ticks, 0f, 0f, 0f, 0f);
                var tkImg = ticks.gameObject.AddComponent<Image>();
                tkImg.sprite = PaperFx.RingTicks(7, turnMax); tkImg.color = PaperFx.Paper; tkImg.raycastTarget = false;
            }
            // 上限を超えた分 (一時マナ・溶けない氷菓の持ち越し。2026-09-29 p20): 輪の外の右上に真鍮の小玉 (コスト玉と同じ絵) を超えた数だけ (5つまで。数は中央の数字が正)
            int over = st.Player.Energy - turnMax;
            for (int i = 0; i < over && i < 5; i++)
            {
                float a = (20f + 16f * i) * Mathf.Deg2Rad;   // 上から右回り
                var o = UiKit.NewRect("over" + i, sun);
                o.anchorMin = o.anchorMax = o.pivot = new Vector2(0.5f, 0.5f);
                o.sizeDelta = new Vector2(14f, 14f);
                o.anchoredPosition = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 74f;
                var oi = o.gameObject.AddComponent<Image>();
                oi.sprite = PaperFx.Orb(PaperFx.Brass); oi.preserveAspect = true; oi.raycastTarget = false;
            }
            var et = UiKit.Deco(sun, st.Player.Energy.ToString(), 40, PaperFx.Ink, TextAnchor.MiddleCenter);
            UiKit.Anchor(et.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-8f, 4f), new Vector2(-8f, 6f));
            var em = UiKit.Txt(sun, "/ " + turnMax, 15, PaperFx.InkSoft, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(em.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, -14f), new Vector2(60f, 8f));
            // 名札「エナジー」は灯籠の名札「灯」と対で太字 (SemiBold)・中墨 (2026-09-29 p20)
            var el = UiKit.Txt(sun, "エナジー", 13, PaperFx.InkSoft, TextAnchor.MiddleCenter, true);
            el.characterSpacing = 2f;
            UiKit.Anchor(el.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(0f, 38f));
            g.RegisterAnchor("energy", sun);
            float ls = UiKit.Phone ? 2.5f : 3f;
            if (st.Player.EnergyMax > turnMax)
            {   // r3 のスマホは輪の右 (白は灯籠の右) の上端にそろえる (輪の下は山札との間が無い。仕様 §3)
                if (r3ph) EnergyNextTag(root, orbX, st.Player.EnergyMax, turnMax, orbX + 128f + 8f + (lantern ? 12f + LightUi.DotsW * ls : 0f), orbY + 128f - 28f);
                else EnergyNextTag(root, orbX, st.Player.EnergyMax, turnMax);
            }
            if (lantern) LightUi.Build(g, root, st, orbX + 128f + 12f + LightUi.DotsW * ls / 2f, orbY, ls);
        }

        /// <summary>
        /// 予告の札「次のターン 上限 N」(2026-09-29 p20): 上限が今ターンの分母より大きい時 (1ターン目のこのは・ボスレリックの上限+1・ターン途中の芽吹きなど) だけ、
        /// 輪の下 (山札の札の上端と輪の下端の間) に紙 (濃)＋真鍮の墨 (color-theme「予告の札」の形)。左端は四辺の余白 (山札と同じ x)。
        /// 文言は「次のターンから上限 N」だとスマホで灯籠の名札「灯」に掛かる (15px で約148) ので、同じ意味の短い形にした (「+1」だけだとエナジーが1増えるとも読める)
        /// </summary>
        static void EnergyNextTag(RectTransform root, float orbX, int nextMax, int turnMax, float atX = -1f, float atY = -1f)
        {
            string text = "次のターン 上限 " + nextMax;
            var rt = UiKit.NewRect("energyNext", root);
            var img = PaperFx.Sheet(rt, PaperFx.Tag2, "paper");
            UiKit.Stretch(img.rectTransform, 0f, 0f, 0f, 0f);
            img.raycastTarget = true;   // 説明の的 (山札の札とは重ならない)
            var t = UiKit.Txt(rt, text, 13, PaperFx.BrassInk, TextAnchor.MiddleCenter);
            UiKit.Stretch(t.rectTransform, 8f, 8f, 2f, 2f);
            t.raycastTarget = false;
            float w = Mathf.Ceil(t.GetPreferredValues(text).x) + 18f;
            // 山札の札の上端 (PC 64・スマホ 76) と輪の下端 116 の間の真ん中
            float y0 = UiKit.Phone ? 82f : 76f, h = 28f;
            float x0 = orbX;
            if (atX >= 0f) { x0 = atX; y0 = atY; }   // r3 のスマホ: 輪 (灯籠) の右・輪の上端にそろえる
            UiKit.Anchor(rt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x0, y0), new Vector2(x0 + w, y0 + h));
            rt.localRotation = Quaternion.Euler(0f, 0f, -2f);
            Tooltip.Attach(rt.gameObject, delegate
            {
                return "<b>エナジーの上限 " + nextMax + "</b>\n上限が増えた分は、次のターンの補充から効く（このターンは " + turnMax + " まで）";
            }, false);
            PaperFx.Nightify(rt);   // 夜の札 (ui=night・P20 2周目): 予告の札も舞台の上に常に出る札 (紙 (濃)＋真鍮の墨 → 夜＋淡い真鍮)。エナジーの輪 (資源＝真鍮) は今のまま
        }

        // ---- ログの引き出し ----

        static void BuildLogDrawer(GameRoot g, RectTransform root, GameState st)
        {
            var pan = UiKit.NewRect("logDrawer", root);
            UiKit.Anchor(pan, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-520f, 260f), new Vector2(-12f, -TopH - 8f));
            var sheet = PaperFx.Sheet(pan, PaperFx.Panel, "paper");
            UiKit.Stretch(sheet.rectTransform, 0f, 0f, 0f, 0f);
            var inner = UiKit.NewRect("inner", pan);
            UiKit.Stretch(inner, 16f, 16f, 14f, 14f);
            UiKit.Vert(inner, 4, 0);
            var head = UiKit.Head(inner, "戦闘ログ", 20);
            if (UiKit.Phone) UiKit.Le(head, -1f, 48f, -1f, 48f);   // 閉じるボタン (48) の高さぶん見出しの行を取る
            // 閉じる (2026-09-29): PC の上部バーの「ログ」は ≡ に畳んだので、窓の右上で閉じられるように
            {
                bool ph = UiKit.Phone;
                var cb = UiKit.Btn(pan, "閉じる", delegate { g.ShowLog = false; g.Rebuild(); }, ph ? 16 : 14);
                float bw = ph ? 104f : 92f, bh = ph ? 48f : 34f;
                UiKit.Anchor((RectTransform)cb.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f - bw, -12f - bh), new Vector2(-14f, -12f));
            }
            var content = UiKit.Scroll(inner, true, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 2, 8);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 100f, -1f, 100f, -1f, 1f);
            var lines = new List<string>();
            for (int i = 0; i < st.EventLog.Count; i++)
            {
                // 灯の火床の直後の「火種N枚を山札に混ぜた」は同じ出来事 = 「灯9を払って火種3を山札へ」の1行にまとめる (2026-09-24 T15。CLI と同じ)
                if (st.EventLog[i] is GameEvent_CardsAddedToDraw && i > 0 && st.EventLog[i - 1] is GameEvent_LightDischarged hearth && (hearth.Sparks ?? 0) > 0) continue;
                var s = CardText.LogLine(st.EventLog[i]);
                if (s != null) lines.Add(s);
            }
            int from = Math.Max(0, lines.Count - 60);
            for (int i = from; i < lines.Count; i++)
            {
                var t = UiKit.Txt(content, lines[i], 14, PaperFx.Ink);
                UiKit.Le(t, -1f, 20f, -1f, -1f);
            }
        }

        // ---- モーダル: 確認ウィンドウ (set-confirm) ----

        /// <summary>
        /// ターン終了の入口 (2026-09-25): 満ち潮の書庫があれば残す手札の窓 → 灯の火床があれば枚数の窓 → EndTurn。
        /// 窓で選んだものは FinishEndTurn がまとめて1つの EndTurn にする
        /// </summary>
        public static void StartEndTurn(GameRoot g)
        {
            var st = g.Rs != null ? g.Rs.Combat : null;
            if (st == null) return;
            if (g.RetainChoice == null && Combat.RetainHandMax(st) > 0) { g.RetainChoice = new List<string>(); g.Rebuild(); return; }
            if (Effects.HearthSparkMax(st) > 0) { g.HearthChoice = true; g.Rebuild(); return; }
            FinishEndTurn(g, null);
        }

        static void FinishEndTurn(GameRoot g, int? hearth)
        {
            var keep = g.RetainChoice != null && g.RetainChoice.Count > 0 ? new List<string>(g.RetainChoice) : null;
            g.RetainChoice = null;
            g.HearthChoice = false;
            g.DoCombat(new Command_EndTurn { HearthSparks = hearth, RetainUids = keep });
        }

        /// <summary>満ち潮の書庫の窓 (青 2026-09-25): ターン終了時に残す手札を上限まで選ぶ (敵ターンの後の全捨てで捨てない)</summary>
        static void BuildRetainChooser(GameRoot g, RectTransform root, GameState st)
        {
            int max = Combat.RetainHandMax(st);
            var pool = new List<CardInstance>();
            for (int i = 0; i < st.Player.Hand.Count; i++) if (!st.Player.Hand[i].Def.Id.StartsWith("status_", StringComparison.Ordinal)) pool.Add(st.Player.Hand[i]);
            var inner = Modal(root, 1400f, 720f, "retain");
            UiKit.Head(inner, "満ち潮の書庫 — 残す手札を選ぶ（" + g.RetainChoice.Count + " / " + max + "枚まで）", 24);
            UiKit.Txt(inner, "選んだ札は敵ターンの後も手札に残ります。選ばなかった札はいつもどおり捨て札へ", 15, PaperFx.InkSoft);
            CardRow(g, inner, st, pool, g.RetainChoice, "残す", "残す（選択中）", uid =>
            {
                if (g.RetainChoice.Contains(uid)) g.RetainChoice.Remove(uid);
                else if (g.RetainChoice.Count < max) g.RetainChoice.Add(uid);
                g.Rebuild();
            });
            CenteredButton(inner, g.RetainChoice.Count > 0 ? "これで終える" : "残さずに終える", delegate
            {
                if (g.Rs != null && g.Rs.Combat != null && Effects.HearthSparkMax(g.Rs.Combat) > 0) { g.HearthChoice = true; g.Rebuild(); return; }
                FinishEndTurn(g, null);
            }, 18, 300f, 50f, PaperFx.BrassLight);
            CenteredButton(inner, "戻る", delegate { g.RetainChoice = null; g.Rebuild(); }, 15, 200f, 44f);
        }

        /// <summary>占術の窓 (青 2026-09-25): 山札の上 (左が次に引く札) から捨てる札を選ぶ。残した札は並びのまま山札に戻る。決めるまで他の操作はできない</summary>
        static void BuildScryChooser(GameRoot g, RectTransform root, GameState st)
        {
            int n = Math.Min(st.PendingScry.Count, st.Player.DrawPile.Count);
            var look = new List<CardInstance>();
            for (int i = 0; i < n; i++) look.Add(st.Player.DrawPile[i]);
            g.ScryDiscard.RemoveAll(u => !look.Exists(c => c.Uid == u));
            var inner = Modal(root, 1400f, 720f, "scry");
            UiKit.Head(inner, "占術" + st.PendingScry.Count + " — 山札の上から捨てる札を選ぶ（左が次に引く札）", 24);
            UiKit.Txt(inner, "選んだ札は捨て札へ。残した札は並びのまま山札に戻ります", 15, PaperFx.InkSoft);
            CardRow(g, inner, st, look, g.ScryDiscard, "捨てる", "捨てる（選択中）", uid =>
            {
                if (g.ScryDiscard.Contains(uid)) g.ScryDiscard.Remove(uid); else g.ScryDiscard.Add(uid);
                g.Rebuild();
            });
            CenteredButton(inner, g.ScryDiscard.Count > 0 ? g.ScryDiscard.Count + "枚を捨てて決める" : "全部残して決める", delegate
            {
                var sel = new List<string>(g.ScryDiscard);
                g.ScryDiscard.Clear();
                g.DoCombat(new Command_ResolveScry { DiscardUids = sel });
            }, 18, 320f, 50f, PaperFx.BrassLight);
        }

        /// <summary>札を横に並べて1枚ずつ切り替えるボタンをつける (書庫・占術の窓が共用)</summary>
        static void CardRow(GameRoot g, RectTransform inner, GameState st, IReadOnlyList<CardInstance> pool, List<string> selected, string offLabel, string onLabel, Action<string> toggle)
        {
            var content = UiKit.Scroll(inner, false, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 16, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 360f, -1f, 360f, -1f, 1f);
            var lg = content.GetComponent<HorizontalLayoutGroup>();
            if (lg != null) { lg.childForceExpandWidth = false; lg.childForceExpandHeight = false; lg.childAlignment = TextAnchor.MiddleLeft; }
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                string uid = c.Uid;
                bool isSel = selected.Contains(uid);
                var wrap = UiKit.NewRect("cand", content);
                UiKit.Le(wrap, 220f, 330f, 220f, 330f);
                var cv = CardView.Build(wrap, c, st, !isSel, true, "cand-card");
                CardPopup.Attach(g, cv, c, delegate { return g.Rs != null ? g.Rs.Combat : null; }, true);
                cv.anchoredPosition = new Vector2(0f, 30f);
                cv.localScale = Vector3.one * 0.86f;
                var pick = UiKit.Btn(wrap, isSel ? onLabel : offLabel, delegate { toggle(uid); }, 16, true, isSel ? PaperFx.BrassLight : Color.white);
                var ple = pick.GetComponent<LayoutElement>();
                if (ple != null) UnityEngine.Object.Destroy(ple);
                UiKit.Anchor(pick.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-80f, 0f), new Vector2(80f, 44f));
            }
        }

        /// <summary>灯の火床の窓 (2026-09-20 夜): ターン終了時に灯3につき火種1を山札へ。0〜最大枚数のボタンで選んでターンを終える</summary>
        static void BuildHearthChooser(GameRoot g, RectTransform root, GameState st)
        {
            int max = Effects.HearthSparkMax(st);
            var inner = Modal(root, 560f, 120f + 62f * (max + 1), "hearth");
            UiKit.Txt(inner, "灯の火床: 灯を火種に変える枚数 (灯3につき火種1。払った灯だけ失う。いま灯 " + (st.Player.Light ?? 0) + ")", 16, PaperFx.Ink);
            for (int n = 0; n <= max; n++)
            {
                int nn = n;
                CenteredButton(inner, nn == 0 ? "変えない (灯を残す)" : nn + "枚 (灯" + (nn * 3) + "を火種に)", delegate
                {
                    FinishEndTurn(g, nn > 0 ? nn : (int?)null);   // 満ち潮の書庫で選んだ残す手札も一緒に (2026-09-25)
                }, 17, 360f, 50f, nn == 0 ? (Color?)null : PaperFx.BrassLight);
            }
            CenteredButton(inner, "戻る", delegate { g.HearthChoice = false; g.Rebuild(); }, 15, 200f, 44f);
        }

        public static RectTransform Modal(RectTransform root, float w, float h, string name)
        {
            // キャンバスに収める (スマホ 1.6倍は 1200×675 しかない。2026-09-14)
            var cs = CanvasSize(root);
            h = Mathf.Min(h, cs.y - 16f);
            // 画面の切り欠き (パンチホール) が窓の高さに掛かれば、左の余白をその右まで広げ、窓を右へ寄せる (2026-09-29 p10。PC は左右 12 のまま中央)
            float padL = UiKit.SafeLeft(12f, (cs.y - h) / 2f, (cs.y + h) / 2f);
            w = Mathf.Min(w, cs.x - padL - 12f);
            float shiftX = Mathf.Max(0f, padL - (cs.x - w) / 2f);   // 中央に置くと穴に掛かる分だけ右へ
            var backdrop = UiKit.Pan(root, PaperFx.ModalScrim, name + "-backdrop");
            UiKit.Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);
            var win = UiKit.Frame(backdrop.transform, Theme.Panel, Color.white, name, 3f);
            UiKit.Anchor(win.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-w / 2f + shiftX, -h / 2f), new Vector2(w / 2f + shiftX, h / 2f));
            win.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0.4f);
            PaperFx.GrainOver(win.transform, 0.6f);
            var inner = UiKit.NewRect("inner", win.transform);
            UiKit.Stretch(inner, 28f, 28f, 22f, 22f);
            UiKit.Vert(inner, 10, 0);
            return inner;
        }

        // ---- 確認ウィンドウ (set-confirm): 帳面から立ち上がる窓 (2026-09-15 案C。旧: 中央のモーダルが舞台と敵を隠していた) ----
        // 行動する敵の札の隣 (左に余裕が無ければ右) に紙の窓が立ち上がり、その敵の列だけ明るく、他の敵・手札・上部バーは暗く沈む。
        // 中身: 「③ 探り屋 の攻撃の前」／実値×ヒット・「温存すると HP −3（80 → 77）」／候補ごとにトークン＋名前＋効果の全文＋発動 (右の列)＋「発動すると HP −0 …」／
        // 準備中・エナジー不足は灰色で理由／温存 (発動と同じ右の列・同じ幅の2番手) (2026-09-29 p03)。

        static void BuildReactionWindow(GameRoot g, RectTransform root, GameState st)
        {
            var win = Effects.WindowFromPending(st);
            var pending = st.PendingWindow;
            if (win == null || pending == null)
            {
                var inner0 = Modal(root, 600f, 240f, "reaction");
                UiKit.Txt(inner0, "窓の情報を復元できません", 16, UiKit.ColBadInk);
                UiKit.Btn(inner0, "温存して続ける", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, 18);
                return;
            }
            bool ph = UiKit.Phone;
            var cs = CanvasSize(root);
            int ei = pending.EnemyIndex;
            string ename = "?";
            try { if (ei >= 0 && ei < st.Enemies.Count) ename = Content.GetEnemyDef(st.Enemies[ei].EnemyId).Name; } catch (Exception) { }
            string num = st.Enemies.Count > 1 && ei >= 0 && ei < Circled.Length ? Circled[ei].ToString() : "";
            // 行動する敵の列 = 明るく残す穴 (2026-09-29 p11): 穴は行動中の敵の意図の札の少し上で止め (旧: 上部バーまで縦に抜け、空と木に硬い光の柱が2本立ち、
            // G と魔素の札が途中で明暗に割れていた)、左・右・上の縁は内側へぼかす。上部バーは全幅で一様に沈め、手番の札だけ明るく残す
            var epan = g.Battle != null ? g.Battle.EnemyPanel(ei) : null;
            float ecx = epan != null ? (epan.offsetMin.x + epan.offsetMax.x) / 2f : cs.x * 0.7f;
            float gapE = epan != null ? g.Battle.EnemyGap(ei) : float.MaxValue;
            float stripHalf = StripW(gapE, st.Enemies.Count == 1) / 2f;
            float feather = ph ? 28f : 40f;
            // 穴の半幅: 1体 (ボス) は帳面＋ぼかし幅まで広げる (ぼかしが帳面の金の縁に掛からない)。隣の敵の帳面には掛けない (4体でも隣の札を明るくしない)
            float half = st.Enemies.Count == 1 ? Mathf.Max(stripHalf + feather + 14f, ph ? 100f : 150f) : Mathf.Max(stripHalf, ph ? 100f : 150f) + 14f;
            if (gapE < float.MaxValue) half = Mathf.Max(stripHalf + 6f, Mathf.Min(half, gapE - stripHalf - 2f));
            float fi = Mathf.Clamp(half - stripHalf - 6f, 0f, feather);   // 左右: 帳面との余白が足りなければ ぼかしを 余白−6 まで狭める (4体は硬い縁に戻る)
            float holeL = ecx - half, holeR = ecx + half;
            float sy = BattleView.StatusLineY;
            // 上の縁: 意図の札の上端 +5 (行動中の札の金の縁) +4 の上にぼかし幅ぶん。上部バーの 4 下で頭打ち (その時はぼかしを余白ぶんに狭める)
            float holeTop = cs.y - TopH - 4f, ft = feather;
            var itag = epan != null ? epan.Find("intent-tag") as RectTransform : null;
            if (itag != null)
            {
                float tagTop = epan.offsetMin.y + itag.offsetMax.y + 5f + 4f;
                if (tagTop + feather < holeTop) holeTop = tagTop + feather;
                else ft = Mathf.Clamp(holeTop - tagTop, 0f, feather);
            }
            var dimCol = PaperFx.Scrim;
            Dim(root, 0f, 0f, cs.x, sy - 6f, dimCol);          // 手札
            Dim(root, 0f, sy - 6f, holeL, cs.y, dimCol);        // 左 (自分の札・他の敵・上部バー)
            Dim(root, holeR, sy - 6f, cs.x, cs.y, dimCol);      // 右
            Dim(root, holeL, holeTop, holeR, cs.y, dimCol);     // 穴の上 (空・上部バー)
            if (fi >= 1f || ft >= 1f)
            {   // 縁のぼかし (名前は "dim" = 窓を畳む時に暗幕と一緒に薄れて消える)
                var fe = UiKit.Pan(root, Color.white, "dim");
                fe.sprite = ThemeFx.HoleFeather(dimCol, Mathf.RoundToInt(fi), Mathf.RoundToInt(ft));
                fe.type = Image.Type.Sliced; fe.pixelsPerUnitMultiplier = 1f; fe.raycastTarget = false;
                UiKit.Anchor(fe.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(holeL, sy - 6f), new Vector2(holeR, holeTop));
            }
            // 手番の札「敵の番 ① / 2」は暗幕の上に (この場面でいちばん大事な上部バーの情報。窓の紙はこの後に作る)
            var phaseT = root.Find("phase");
            if (phaseT != null) phaseT.SetAsLastSibling();

            // 候補: 使える札・エナジー不足の札・この窓では動かない札 (準備中・別の窓の札)
            var usable = Effects.UsableSetCards(st, win);
            var un = Effects.UnaffordableSetCards(st, win);
            var others = new List<CardInstance>();
            for (int i = 0; i < st.Player.SetCards.Count; i++)
            {
                var sc = st.Player.SetCards[i];
                bool listed = false;
                for (int j = 0; j < usable.Count; j++) if (usable[j].Uid == sc.Uid) listed = true;
                for (int j = 0; j < un.Count; j++) if (un[j].Uid == sc.Uid) listed = true;
                if (!listed) others.Add(sc);
            }
            var risks = Effects.SetBranchFlipRisks(st);
            int rows = usable.Count + un.Count + others.Count;
            // 意図は見出し・実値の行・見込みの試算が読むので先に取る (2026-09-29 p03)
            var it = Effects.EffectiveIntent(st, ei) ?? (ei >= 0 && ei < st.Enemies.Count ? st.Enemies[ei].Intent : null);
            bool preAttack = win.Stage == "pre" && it != null && it.Kind == "attack";
            bool hasSub = it != null && (win.Stage == "post" || it.Kind == "attack");
            // 候補ごとの説明と「発動すると」の行 (2026-09-29 p03): 見込みはエンジンの純関数で ConfirmReaction を試して出す (実処理と同じ数)。
            // 返し・ダメージの実値 (SetCardLiveDamage) は「発動すると」の行が無い窓 (被攻撃後など) ではその行に出す＝説明の3行を食わない
            var descs = new string[usable.Count];
            var outcomes = new string[usable.Count];
            string enemyLabel = (num.Length > 0 ? num + " " : "") + ename;
            for (int i = 0; i < usable.Count; i++)
            {
                var c = usable[i];
                string live = null;
                try { live = Effects.SetCardLiveDamage(st, c.Def, ei); } catch (Exception) { }
                // 敵に与える見込み (2026-09-30 F22: 旧は「実際の値: 」を「発動すると 」に置き換えただけで、被攻撃前の窓の「HP」＝自分と同じ書き出しで敵の HP を指していた)。
                // 主語 (敵の名前) を先に置き、自分の HP の行と見分ける。試せなければ今までの文 (「実際の値」のまま)
                string foe = FiredEnemyOutcome(st, ei, c.Uid, enemyLabel);
                string fired = preAttack ? FiredOutcome(st, ei, c.Uid, foe) : null;
                outcomes[i] = fired ?? (foe != null ? "発動すると " + foe : live);
                descs[i] = ReactionDesc(c.Def, win);
            }
            // 窓の大きさ: 要素の高さの合計。候補の行は説明の実寸で決める (2026-09-30 F23: 旧は 104 固定で、説明が1行の札の「発動すると」が
            // 自分の説明から離れて次の札の名前に近く見えた)。上部バーに掛かるなら下端を手札側へ下げる (敵の番のあいだ手札は触れない)
            float sp = ph ? 4f : 6f;
            float rowH2 = ph ? 84f : 86f;            // 動かせない行 (説明は1〜2行。トークン74＋余白)
            float outH = ph ? 24f : 26f;             // 「発動すると」の行 (Ellipsis の矩形は字の大きさ×1.6以上)
            float W = ph ? 490f : 560f;              // スマホは 410→490 (説明の列 194→274。窓は帳面の左に張り付いたまま左へ伸びる)
            float btnWc = ph ? 100f : 140f, bhc = ph ? 30f : 28f;
            var descH = new float[usable.Count];
            var candH = new float[usable.Count];
            var hs = new List<float>();
            // 窓の高さを幅 w で測る (説明の行数は幅で決まる)。descH・candH・hs を書き直す
            Func<float, float> measureH = mw =>
            {
                float descW = mw - 2f * (ph ? 14f : 20f) - (PhoneTokenW + 12f) - (btnWc + 8f);
                var meas = UiKit.Txt(root, "", 15, PaperFx.Ink, TextAnchor.UpperLeft, true);
                meas.lineSpacing = 0f;
                float three = meas.GetPreferredValues("あ\nあ\nあ", descW, 0f).y;
                for (int i = 0; i < usable.Count; i++)
                {
                    descH[i] = Mathf.Min(three, meas.GetPreferredValues(descs[i], descW, 0f).y) + 2f;
                    float outTop = Mathf.Max(32f + descH[i] + 4f, 2f + bhc * 2f + 4f);
                    candH[i] = outcomes[i] != null ? Mathf.Max(PhoneTokenH + 4f, outTop + outH + 4f) : Mathf.Max(PhoneTokenH + 4f, 32f + descH[i] + 6f);
                }
                meas.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(meas.gameObject);
                hs.Clear();
                hs.Add(ph ? 26f : 30f);                  // 見出し
                hs.Add(ph ? 34f : 40f);                  // 実値の行
                if (hasSub) hs.Add(ph ? 22f : 26f);      // 温存すると／受けた HP
                if (risks.Count > 0) hs.Add(40f);
                hs.Add(1f);                              // 区切り
                for (int i = 0; i < usable.Count; i++) { if (i > 0) hs.Add(1f); hs.Add(candH[i]); }   // 候補の間に細い線
                for (int i = 0; i < un.Count + others.Count; i++) hs.Add(rowH2);
                if (rows == 0) hs.Add(44f);
                hs.Add(usable.Count > 0 ? 48f : (ph ? 48f : 52f));   // 温存の行 (発動と同じ列) ／続ける
                float mh = (ph ? 20f : 28f) + sp * (hs.Count - 1);
                for (int i = 0; i < hs.Count; i++) mh += hs[i];
                float mMax = cs.y - TopH - 12f - 12f;
                return mh > mMax ? mMax : mh;
            };
            float W0r = W;
            float H = measureH(W);
            // r3 (2026-10-02 読み合わせの指摘): 置き場の幅は H で変わる (H が大きいと主人公を避けて左端が右へ寄る＝狭まる)。
            // 最後の幅で中身を測り直す (2回まで)。旧は H=300 の見積りの幅で中身を組み、狭まった窓から説明とボタンがはみ出した
            if (R3)
                for (int it2 = 0; it2 < 3; it2++)
                {
                    Rect pr;
                    if (!R3WindowRect(g, st, cs, W0r, H, ei, out pr, false) || Mathf.Abs(pr.width - W) < 1f) break;
                    W = pr.width; H = measureH(W);
                }
            // 置き場 (2026-09-29 p11): PC で自分の札に掛かる時は札の上 (空いた道) へ上げる。上げられなければ下端は帳面の線のまま、
            // 自分の札を窓の手前の区画の境で打ち切る (ギア・置物は敵の番には押せず暗幕の下。窓を畳むと戻す)
            bool lifted;
            Rect wr;
            if (R3 && R3WindowRect(g, st, cs, W0r, H, ei, out wr)) { lifted = true; W = wr.width; H = wr.height; }   // r3 (2026-10-02 仕様 §8): 下の窓 (PC)／主人公を切らない窓 (スマホ)。幅は上で測った W と同じ (同じ H なので同じ置き場)
            else { if (R3) { W = W0r; H = measureH(W); } wr = ReactionWindowRect(g, st, cs, ei, ecx, stripHalf, W, H, out lifted); }
            float x = wr.x, y0 = wr.y;
            if (!ph && !lifted && SelfStripRight > 0f && x < SelfStripRight + 2f) TrimSelfStrip(g.Battle != null && g.Battle.SelfArea != null ? g.Battle.SelfArea : g.Anchor("player"), x - 12f);   // 札の入れ物 (箱庭では兄弟。P20 2周目)
            var panel = PaperFx.Sheet(root, PaperFx.Panel, "reaction");
            UiKit.Anchor(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, y0), new Vector2(x + W, y0 + H));
            panel.raycastTarget = true;
            PaperFx.GrainOver(panel.transform, 0.6f);
            var inner = UiKit.NewRect("inner", panel.transform);
            UiKit.Stretch(inner, ph ? 14f : 20f, ph ? 14f : 20f, ph ? 10f : 14f, ph ? 10f : 14f);
            var vg = UiKit.Vert(inner, (int)sp, 0);

            // 見出し: 「① 探り屋 の攻撃の前」「② コボルト の応援の前」「① 探り屋 の攻撃の後」(旧「の行動の前（実行前）」)
            string kindJa = CardText.KindJa(!string.IsNullOrEmpty(win.Kind) ? win.Kind : (it != null ? it.Kind : null));
            if (string.IsNullOrEmpty(kindJa)) kindJa = "行動";
            var head = UiKit.Deco(inner, (num.Length > 0 ? num + " " : "") + ename + " の" + kindJa + "の" + (win.Stage == "pre" ? "前" : "後"), ph ? 16 : 19, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(head, -1f, ph ? 26f : 30f, -1f, ph ? 26f : 30f);
            // 実値の行: 絵・実値・×ヒット (2以上の時だけ)。下の1行は温存した時の HP (行動の後は実際の HP損失)
            var line = UiKit.NewRect("actual", inner);
            UiKit.Le(line, -1f, ph ? 34f : 40f, -1f, ph ? 34f : 40f);
            var lg = UiKit.Horz(line, 8, 0);
            lg.childAlignment = TextAnchor.MiddleLeft; lg.childForceExpandWidth = false; lg.childForceExpandHeight = false;
            if (it != null)
            {
                var intentArt = Theme.Art("icons", "intent_" + it.Kind);
                var ic = UiKit.Icon(line, IntentIcon(it.Kind), 32f, intentArt != null ? Color.white : IntentColor(it.Kind));
                if (intentArt != null) { ic.sprite = intentArt; UiKit.PixelArt(ic); }   // スマホでは 1.31 倍 (p25)
                ic.rectTransform.sizeDelta = new Vector2(32f, 32f); UiKit.Le(ic, 32f, 32f, 32f, 32f);
                string val = it.Kind == "attack" ? Effects.DisplayedIntentValue(st, ei, it.Kind, it.Actual).ToString() : IntentShort(st, ei, it);
                var vt = UiKit.Deco(line, val, ph ? 24 : 28, PaperFx.Ink, TextAnchor.MiddleLeft);
                UiKit.Le(vt, -1f, 34f, -1f, 34f);
                string tail, sub = null;
                if (win.Stage == "post")
                {   // 受けた HP を温存の行と同じ書式で (2026-09-30 F22: 旧「この HP損失: 3」は下の「発動すると … HP-10」(敵) と同じ語で主語が逆だった)
                    tail = "";
                    int hl = Math.Max(0, win.HpLoss), hpNow = st.Player.Hp;
                    sub = "受けた " + UiKit.ColorTag(hl > 0 ? PaperFx.BadInk : PaperFx.GoodInk, "<b>HP −" + hl + "</b>") + "（" + (hpNow + hl) + " → " + hpNow + "）";
                }
                else if (it.Kind == "attack")
                {
                    int hits = Effects.IntentHits(st, it.MirrorHits, it.Hits);
                    tail = hits >= 2 ? "×" + hits : "";
                    int holdBlocked, holdBefore; bool holdCancelled;
                    int? holdLoss = PreviewHpLoss(st, ei, new Command_ConfirmReaction { Fire = false }, out holdBlocked, out holdBefore, out holdCancelled);
                    if (holdLoss.HasValue && !holdCancelled)
                        sub = "温存すると " + LossOutcome(st, holdLoss.Value, BlockLeft(st, holdBefore, holdBlocked));
                    else
                    {   // 試算できない時は近似 (通常ブロック→氷壁を引く)
                        int total = Effects.DisplayedIntentValue(st, ei, it.Kind, it.Actual) * hits;
                        int loss = Math.Max(0, total - st.Player.Block - st.Player.IceBlock);
                        sub = "温存すると " + LossOutcome(st, loss, 0);
                    }
                }
                else tail = CardText.IntentText(st, ei);
                if (!string.IsNullOrEmpty(tail))
                {
                    var tt = UiKit.Txt(line, tail, ph ? 13 : 15, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                    UiKit.Le(tt, -1f, 34f, -1f, 34f);
                    tt.textWrappingMode = TextWrappingModes.NoWrap; tt.overflowMode = TextOverflowModes.Ellipsis;
                }
                if (sub != null)
                {
                    var st2 = UiKit.Txt(inner, sub, ph ? 14 : 16, PaperFx.Ink, TextAnchor.MiddleLeft);
                    UiKit.Le(st2, -1f, ph ? 22f : 26f, -1f, ph ? 22f : 26f);
                    st2.textWrappingMode = TextWrappingModes.NoWrap; st2.overflowMode = TextOverflowModes.Ellipsis;
                    // 判断の本体の行は 16px でも小さい字の素材 (2026-09-30 F21: PC は 16 で作るので UiKit.Txt の判定から漏れ、下の 13px の注記より薄く見えた)
                    var sm2 = UiKit.SmallMat(st2.font); if (sm2 != null) st2.fontSharedMaterial = sm2;
                }
            }
            if (risks.Count > 0)
            {
                var buf = new List<string>();
                for (int i = 0; i < risks.Count; i++) buf.Add((risks[i] + 1).ToString());
                var wt = UiKit.Txt(inner, "⚠ 発動するとからくりが空き、敵 " + string.Join("・", buf.ToArray()) + " が「からくりなし」の分岐に変わる", ph ? 13 : 15, PaperFx.GoldInk);
                UiKit.Le(wt, -1f, 40f, -1f, 40f);
            }
            var sep = UiKit.Pan(inner, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.35f), "sep");
            UiKit.Le(sep, -1f, 1f, -1f, 1f);
            // 候補の行: トークン＋名前＋効果の全文＋発動 (右の列)。下に「発動すると」の1行 (全幅)
            for (int i = 0; i < usable.Count; i++)
            {
                var c = usable[i]; string uid = c.Uid;
                if (i > 0)
                {   // 候補どうしの間の細い線 (見出しの下の線より弱い＝階層を分ける。F23)
                    var csep = UiKit.Pan(inner, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.2f), "candSep");
                    UiKit.Le(csep, -1f, 1f, -1f, 1f);
                }
                int? winLeft = Effects.TrapWindowsLeft(st, c);
                // 「発動すると」の行は候補の行の中 (説明のすぐ下・発動のボタンより下) に置く＝どの札の見込みかが近さで読める (F23)
                ReactionRow(g, inner, st, c, candH[i], true, descs[i], winLeft.HasValue ? "あと" + winLeft.Value + "回" : "期限なし",
                    delegate { g.DoCombat(new Command_ConfirmReaction { Fire = true, CardUid = uid }); }, outcomes[i], descH[i]);
            }
            for (int i = 0; i < un.Count; i++) ReactionRow(g, inner, st, un[i], rowH2, false, "エナジー不足で発動できない（コスト " + un[i].Def.Cost + "・残り " + st.Player.Energy + "）", null, null);
            for (int i = 0; i < others.Count; i++)
            {
                bool liveTrap = Effects.IsTrapLive(st, others[i]);
                ReactionRow(g, inner, st, others[i], rowH2, false, liveTrap ? "この窓では発動しない（別の窓で鳴る札）" : "準備中（次のターンから）", null, null);
            }
            if (rows == 0)
            {
                var none = UiKit.Txt(inner, "発動できる仕込み札はありません", 16, PaperFx.InkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(none, -1f, 44f, -1f, 44f);
            }
            if (usable.Count > 0)
            {   // 温存: 発動と同じ右の列・同じ幅の2番手 (地は既定の紙)。左に「温存するとどうなるか」(2026-09-29 p03。旧: 窓いっぱいの幅で発動の約5倍の面積)
                var holdRow = UiKit.NewRect("holdRow", inner);
                UiKit.Le(holdRow, -1f, 48f, -1f, 48f);
                float hbW = ph ? 100f : 140f;
                var hold = UiKit.Btn(holdRow, "温存する", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, ph ? 16 : 17);
                var hle = hold.GetComponent<LayoutElement>(); if (hle != null) UnityEngine.Object.Destroy(hle);
                UiKit.Anchor(hold.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-hbW, -24f), new Vector2(0f, 24f));
                // 注記は数を出さない (「あとN回」は敵の番の数で窓の数ではない＝行の右の1か所だけにする)。使える候補の中でいちばん残りが少ない札で書き分ける
                int? minLeft = null;
                for (int i = 0; i < usable.Count; i++)
                {
                    int? l = Effects.TrapWindowsLeft(st, usable[i]);
                    if (l.HasValue && (!minLeft.HasValue || l.Value < minLeft.Value)) minLeft = l;
                }
                // この後に行動する敵のうち、候補の札の窓が実際に開く敵の番号 (2026-09-30 F24: 旧「次の敵の番」は上部バーの「敵の番 ①」と同じ語で
                // 「次の敵 (②) の番」とも読め、残り1回の「この敵の番が終わると期限切れ」は①の行動で切れると読めた。語は札の文と同じ「敵フェーズ」)
                var later = new System.Text.StringBuilder();
                for (int j = ei + 1; j < st.Enemies.Count && j < Circled.Length; j++)
                {
                    if (st.Enemies[j].Hp <= 0) continue;
                    var itj = Effects.EffectiveIntent(st, j);
                    if (itj == null) continue;
                    int vj = Effects.ReactionActionValue(st, j);
                    bool can = false;
                    for (int k = 0; k < usable.Count && !can; k++)
                        can = Effects.ReactionMatches(st, usable[k], Effects.PreWindowFor(st, j))
                           || Effects.ReactionMatches(st, usable[k], new ReactionWindow { Stage = "post", Kind = itj.Kind, Actual = vj, HpLoss = vj });
                    if (can) later.Append(Circled[j]);
                }
                string L = later.ToString();
                string noteText = !minLeft.HasValue ? "温存すると窓は閉じる。からくりは期限なしで残る"
                    : minLeft.Value >= 2 ? (L.Length > 0 ? "温存すると窓は閉じる。" + L + "の行動と次の敵フェーズにも発動できる" : "温存すると窓は閉じる。次の敵フェーズにも発動できる")
                    : (L.Length > 0 ? "温存: " + L + "の行動にはまだ発動できる。この敵フェーズで期限切れ" : "温存すると窓は閉じ、この敵フェーズの終わりに期限切れ");
                var note = UiKit.Txt(holdRow, noteText, 13, PaperFx.InkSoft, TextAnchor.MiddleLeft);
                UiKit.Anchor(note.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-hbW - 8f, 0f));
                note.lineSpacing = -4f; note.maxVisibleLines = 2; note.overflowMode = TextOverflowModes.Ellipsis;
            }
            else
            {   // 動かせる候補が無い: 全幅の「続ける」(今のまま)
                var hold = UiKit.Btn(inner, "続ける", delegate { g.DoCombat(new Command_ConfirmReaction { Fire = false }); }, ph ? 16 : 18);
                var hle = hold.GetComponent<LayoutElement>(); if (hle != null) { hle.minHeight = ph ? 48f : 52f; hle.preferredHeight = ph ? 48f : 52f; }
            }
        }

        /// <summary>確認の窓の「温存すると／発動すると」の見込み (2026-09-29 p03): エンジンの純関数 State.ApplyCommand で ConfirmReaction を1回だけ試し、
        /// 行動している敵 ei の攻撃で失う HP とブロック (通常＋氷壁) が吸った量を読む。氷壁・身代わりの符・被ダメの上限のレリック・打ち消し・返しで倒す、まで実処理どおりの数になる。
        /// 打ち消された／攻撃の前に倒れた → cancelled。blockBefore は攻撃の直前に持っていたブロック (今のブロック＋氷壁＋この試算で得た分)。試算が例外なら null＝呼び側は近似の式に戻す。
        /// 状態は不変・乱数も状態の中なので、試しても盤面は変わらない</summary>
        static int? PreviewHpLoss(GameState st, int ei, Command_ConfirmReaction cmd, out int blocked, out int blockBefore, out bool cancelled)
        {
            blocked = 0; cancelled = false;
            blockBefore = st.Player.Block + st.Player.IceBlock;
            GameState next;
            try { next = State.ApplyCommand(st, cmd); }
            catch (Exception) { return null; }
            if (next == null || next.EventLog == null || st.EventLog == null) return null;
            int loss = 0; bool hit = false;
            for (int i = st.EventLog.Count; i < next.EventLog.Count && !hit; i++)
            {
                var ev = next.EventLog[i];
                var bg = ev as GameEvent_BlockGained;
                if (bg != null && bg.Target == "player") { blockBefore += bg.Amount; continue; }
                var ig = ev as GameEvent_IceBlockGained;
                if (ig != null) { blockBefore += ig.Amount; continue; }
                var dd = ev as GameEvent_DamageDealt;
                if (dd != null && dd.Source == "enemy" && dd.EnemyIndex == ei) { loss = dd.HpLoss; blocked = dd.Blocked ?? 0; hit = true; continue; }
                var an = ev as GameEvent_ActionNegated;
                if (an != null && an.EnemyIndex == ei) cancelled = true;
                var died = ev as GameEvent_EnemyDied;
                if (died != null && died.EnemyIndex == ei) cancelled = true;
            }
            if (hit) cancelled = false;
            return hit ? loss : 0;
        }

        /// <summary>攻撃を受けた後に余るブロック (身代わりの符はブロックを使わない)</summary>
        static int BlockLeft(GameState st, int blockBefore, int blocked)
        {
            if (st.NullifyNextAttack == true) return blockBefore;
            return Math.Max(0, blockBefore - blocked);
        }

        /// <summary>「HP −3（80 → 77）・ブロック 9 が余る」: HP −0 は良いの墨・1以上は危険の墨・余りは鋼青の墨 (墨版だけ・塗りは足さない)。
        /// 通常のブロックはターンの始めに消えるので「残る」とは書かず「余る」と書く</summary>
        static string LossOutcome(GameState st, int loss, int left)
        {
            int hp = st.Player.Hp;
            // 減らない時は「HP は減らない」(2026-09-30 F21: 「HP −0（80 → 80）」は符号つきの0で数式に見え、括弧の後の「・」で間延びした)
            string s = loss <= 0 ? UiKit.ColorTag(PaperFx.GoodInk, "<b>HP は減らない</b>")
                : UiKit.ColorTag(PaperFx.BadInk, "<b>HP −" + loss + "</b>") + "（" + hp + " → " + Math.Max(0, hp - loss) + "）";
            if (left > 0) s += "・" + UiKit.ColorTag(PaperFx.SkyInk, "ブロック " + left + " が余る");
            return s;
        }

        /// <summary>発動した時の見込みの1行 (被攻撃前の窓で意図が攻撃の時だけ)。foe＝敵に与える見込み (FiredEnemyOutcome) があれば先に書く。試算できなければ null</summary>
        static string FiredOutcome(GameState st, int ei, string uid, string foe = null)
        {
            int blocked, before; bool cancelled;
            int? loss = PreviewHpLoss(st, ei, new Command_ConfirmReaction { Fire = true, CardUid = uid }, out blocked, out before, out cancelled);
            if (!loss.HasValue) return null;
            if (cancelled) return "発動すると " + (foe != null ? foe + "・" : "") + UiKit.ColorTag(PaperFx.GoodInk, "<b>この攻撃は来ない</b>");
            if (foe != null) return "発動すると " + foe + "・" + LossOutcome(st, loss.Value, 0);
            return "発動すると " + LossOutcome(st, loss.Value, BlockLeft(st, before, blocked));
        }

        /// <summary>発動した時に行動中の敵 ei へ与える見込み (2026-09-30 F22): ConfirmReaction をエンジンで1回だけ試し、札の解決が終わるまで
        /// (次の敵の行動・ターンの切り替わり・敵の攻撃が来るまで) の DamageDealt (あなた→ei) の HP 損失を足す。倒せば「① 探り屋を倒す」、
        /// 与えなければ null。数字は墨 (自分の HP の損得の色＝朱・苔は使わない)</summary>
        static string FiredEnemyOutcome(GameState st, int ei, string uid, string enemyLabel)
        {
            GameState next;
            try { next = State.ApplyCommand(st, new Command_ConfirmReaction { Fire = true, CardUid = uid }); }
            catch (Exception) { return null; }
            if (next == null || next.EventLog == null || st.EventLog == null) return null;
            int dmg = 0; bool killed = false, started = false;
            for (int i = st.EventLog.Count; i < next.EventLog.Count; i++)
            {
                var ev = next.EventLog[i];
                if (ev is GameEvent_ReactionTriggered) { started = true; continue; }
                if (!started) continue;
                var dd = ev as GameEvent_DamageDealt;
                if (dd != null && dd.Source == "enemy") break;
                if (ev is GameEvent_EnemyActionExecuting || ev is GameEvent_TurnStarted || ev is GameEvent_TurnEnded) break;
                if (dd != null && dd.Source == "player" && dd.EnemyIndex == ei) dmg += dd.HpLoss;
                var died = ev as GameEvent_EnemyDied;
                if (died != null && died.EnemyIndex == ei) killed = true;
            }
            if (killed) return UiKit.ColorTag(PaperFx.GoodInk, "<b>" + enemyLabel + "を倒す</b>");
            if (dmg <= 0) return null;
            int before = ei >= 0 && ei < st.Enemies.Count ? st.Enemies[ei].Hp : 0;
            return enemyLabel + "に <b>" + dmg + "</b>（" + before + " → " + Math.Max(0, before - dmg) + "）";
        }

        /// <summary>確認の窓に出す札の効果 (2026-09-29 p03): この窓の誘発の見出し (「被攻撃前: 」等) は窓が言うので省き、効果を「・」で並べる。
        /// 条件は ConditionLabel が文「Xなら、」で返す (2026-09-29 p06 でカードの面も同じ形になったので、ここでの角括弧の書き換えは撤去)</summary>
        static string ReactionDesc(CardDef def, ReactionWindow win)
        {
            var hide = win != null && win.Stage == "post" ? PostWindowTriggers : PreWindowTriggers;
            string body = CardText.Body(def, true, hide) ?? "";
            body = body.Replace(" / ", "・").Replace("\n", "・");
            return CardText.NoBreak(body);   // 語の途中で改行しない (2026-09-30 F20)
        }

        static readonly string[] PreWindowTriggers = { "onEnemyAction", "onAttackIncoming" };
        static readonly string[] PostWindowTriggers = { "onAttacked", "onEnemyBuffed", "onEnemyDefended" };

        /// <summary>確認の窓の1行 (2026-09-29 p03): トークン (68×74。角の数字は出さない)＋名前 (高さ28)＋右上に「あとN回」(青緑の墨・残りはここ1か所)＋効果の全文 (墨の太字15・3行)、
        /// 発動できるなら右の列に発動 (PC 140×56・スマホ 100×60)</summary>
        static void ReactionRow(GameRoot g, RectTransform inner, GameState st, CardInstance c, float rowH, bool usable, string desc, string left, Action onFire, string outcome = null, float descH = -1f)
        {
            bool ph = UiKit.Phone;
            bool topAlign = descH > 0f;   // 使える候補 (F23): トークン・ボタンを上にそろえ、説明の実寸の下に「発動すると」の行
            var row = UiKit.NewRect("cand", inner);
            UiKit.Le(row, -1f, rowH, -1f, rowH);
            var tok = UiKit.NewRect("token", row);
            if (topAlign) UiKit.Anchor(tok, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, -2f - PhoneTokenH), new Vector2(2f + PhoneTokenW, -2f));
            else UiKit.Anchor(tok, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, -PhoneTokenH / 2f), new Vector2(2f + PhoneTokenW, PhoneTokenH / 2f));
            PhoneSetToken(g, tok, st, c, false);
            CardPopup.Attach(g, tok, c, delegate { return g.Rs != null ? g.Rs.Combat : null; }, true);   // 長押しで札の実物
            if (!usable) { var cg = tok.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0.55f; }
            float btnW = usable ? (ph ? 100f : 140f) : 0f;
            float leftW = left != null ? 84f : 0f;
            // 名前: 高さ28 (Deco 17/15px の行は 26/23 要る。旧は高さ20で Ellipsis が行ごと消していた)
            var nameT = UiKit.Deco(row, c.Def.Name, ph ? 15 : 17, usable ? PaperFx.Ink : PaperFx.InkSoft, TextAnchor.MiddleLeft);
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(PhoneTokenW + 12f, -30f), new Vector2(-btnW - 8f - leftW, -2f));
            nameT.textWrappingMode = TextWrappingModes.NoWrap; nameT.overflowMode = TextOverflowModes.Ellipsis;
            if (left != null)
            {
                var lt = UiKit.Txt(row, left, 13, PaperFx.ManaInk, TextAnchor.MiddleRight);
                UiKit.Anchor(lt.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-btnW - 8f - 80f, -30f), new Vector2(-btnW - 8f, -2f));
                lt.textWrappingMode = TextWrappingModes.NoWrap;
            }
            // 効果: 墨の太字15・3行まで (PC は 13〜15 に縮む。スマホは最小15で固定)。数字の強調は掛けない (行が高くなりすぎる)
            var dt = UiKit.Txt(row, desc, 15, usable ? PaperFx.Ink : PaperFx.InkSoft, TextAnchor.UpperLeft, true);
            if (topAlign) UiKit.Anchor(dt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(PhoneTokenW + 12f, -32f - descH), new Vector2(-btnW - 8f, -32f));
            else UiKit.Anchor(dt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(PhoneTokenW + 12f, 2f), new Vector2(-btnW - 8f, -32f));
            dt.lineSpacing = 0f; dt.maxVisibleLines = 3; dt.overflowMode = TextOverflowModes.Ellipsis;
            if (!ph) { dt.enableAutoSizing = true; dt.fontSizeMin = 13; dt.fontSizeMax = 15; }
            float bh = ph ? 30f : 28f;
            if (usable && onFire != null)
            {
                var fb = UiKit.Btn(row, "発動", delegate { onFire(); }, ph ? 18 : 20, true, PaperFx.BrassLight);
                var fle = fb.GetComponent<LayoutElement>(); if (fle != null) UnityEngine.Object.Destroy(fle);
                if (topAlign) UiKit.Anchor(fb.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-btnW, -2f - bh * 2f), new Vector2(0f, -2f));
                else UiKit.Anchor(fb.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-btnW, -bh), new Vector2(0f, bh));
                var bt = fb.GetComponentInChildren<TMP_Text>();
                if (bt != null && UiKit.FontDeco != null) { bt.font = UiKit.FontDeco; bt.characterSpacing = 3f; }
            }
            if (topAlign && outcome != null)
            {   // 「発動すると」の行: 説明 (かボタン) のすぐ下。札の文の列から窓の右端まで (発動のボタンの下も使う)
                float outH = ph ? 24f : 26f;
                float outTop = Mathf.Max(32f + descH + 4f, 2f + bh * 2f + 4f);
                var ot = UiKit.Txt(row, outcome, ph ? 14 : 16, PaperFx.Ink, TextAnchor.MiddleLeft);
                UiKit.Anchor(ot.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(PhoneTokenW + 12f, -outTop - outH), new Vector2(0f, -outTop));
                ot.textWrappingMode = TextWrappingModes.NoWrap; ot.overflowMode = TextOverflowModes.Ellipsis;
                if (!ph) { ot.enableAutoSizing = true; ot.fontSizeMin = 13; ot.fontSizeMax = 16; }   // 長い時は PC だけ縮める (スマホは最小15)
                var smo = UiKit.SmallMat(ot.font); if (smo != null) ot.fontSharedMaterial = smo;   // 判断の本体の行は 16px でも小さい字の素材 (F21)
            }
        }

        /// <summary>確認の窓の置き場 (キャンバス座標・左下基準。2026-09-29 p11 に BuildReactionWindow から切り出し)。
        /// 基本は行動する敵の帳面の左 (余裕が無ければ右) で、下端は帳面の線。PC で自分の札に掛かる時は、自分の札の 12 上 (空いた道) へ上げる。
        /// 上げるのは、上部バーの下に収まり、行動していない生きた敵の意図の札と体に掛からない時だけ。x は行動中の敵の意図の札・体の左端の 12 手前で止める。
        /// リーダーの武器に掛かるのは許す (暗幕の下。行動中の敵を優先する)</summary>
        static Rect ReactionWindowRect(GameRoot g, GameState st, Vector2 cs, int ei, float ecx, float stripHalf, float W, float H, out bool lifted)
        {
            lifted = false;
            float sy = BattleView.StatusLineY;
            float y0 = Mathf.Min(sy, cs.y - TopH - 12f - H);
            float minX = UiKit.SafeLeft(12f, cs.y - y0 - H, cs.y - y0);   // 画面の切り欠き (パンチホール) に窓の左端を掛けない (2026-09-29 p10。PC は 12 のまま)
            float x = ecx - stripHalf - 10f - W;
            if (x < minX) x = ecx + stripHalf + 10f;
            if (x + W > cs.x - 12f) x = Mathf.Max(minX, cs.x - 12f - W);
            var basic = new Rect(x, y0, W, H);
            if (UiKit.Phone || SelfStripRight <= 0f || x >= SelfStripRight + 12f || g.Battle == null) return basic;
            float ly = sy + StripH + 12f;   // 自分の札の上端 (StripH に固定。2026-09-30 F19) の 12 上
            float lx = x;
            Rect r;
            var epan = g.Battle.EnemyPanel(ei);
            bool leftOfActor = epan != null && x + W <= ecx;
            // 窓が行動中の敵の左にある時: 意図の札 (幅の広い札は帳面より左へ出る) の左端の 12 手前で止める
            if (leftOfActor && PanelChildRect(epan, "intent-tag", 5f, out r)) lx = Mathf.Min(lx, r.xMin - 12f - W);
            // 行動中の敵の体の左端の 12 手前で止める (大きい絵のボス・強個体)。その後で、白の人形の足元の札 (「3・あと4」) を窓の下端で切らないよう、
            // 窓の幅に掛かる札の上端の 8 上まで上げる (2回: 体で左へずれた分の札も見る)
            Rect lr;
            bool hasLeader = LeaderBodyRect(g, out lr);
            for (int pass = 0; pass < 2; pass++)
            {
                if (leftOfActor && EnemyBodyRect(epan, out r) && r.yMin < ly + H && r.yMax > ly) lx = Mathf.Min(lx, r.xMin - 12f - W);
                // リーダーの体 (上半分＝顔) を窓の左端で縦に切らない: 切るなら左へ寄せて丸ごと覆う (2026-09-30 F25: 4体戦の①の窓の左端がこのはの目のすぐ右を通り、
                // 顔の前半分が隠れて描き損じに見えた。武器まで・暗幕の下で覆うのは p11 の許容の内)
                if (hasLeader && ly < lr.yMax && ly + H > lr.yMin + lr.height * 0.5f && lx > lr.xMin - 12f && lx < lr.xMax + 12f) lx = lr.xMin - 12f;
                ly = Mathf.Max(ly, DollTagsTop(g, lx, lx + W) + 8f);
            }
            if (ly + H > cs.y - TopH - 12f) return basic;
            if (lx < 12f) return basic;
            // PC のレリックの列 (上部バーの下の左 x 28〜488。2026-09-29) に掛かる高さまで上げない
            if (lx < UiKit.Edge + 460f + 8f && ly + H > cs.y - TopBandBottom(g) - 8f) return basic;
            var win = new Rect(lx, ly, W, H);
            for (int j = 0; j < st.Enemies.Count; j++)
            {
                if (j == ei || st.Enemies[j].Hp <= 0) continue;
                var pj = g.Battle.EnemyPanel(j);
                if (pj == null) continue;
                if (PanelChildRect(pj, "intent-tag", 5f, out r) && r.Overlaps(win)) return basic;
                if (EnemyBodyRect(pj, out r) && r.Overlaps(win)) return basic;
            }
            lifted = true;
            return win;
        }

        // ---- 三周目 r3 の窓の置き場 (2026-10-02 仕様 docs/design/hd2d-slice/r3-ui-spec.md §8) ----
        // 確認の窓・ギアの窓・持ち物の一覧が同じ枠を使う。PC は「下の窓」(足元の帳の右から敵の帳の左まで・下端は画面の下 −24)、
        // スマホは「主人公を窓の縁で切らない窓」(覆うなら丸ごと)。窓と重なる人形の足元の札は窓が開いている間だけ畳む

        /// <summary>r3 の PC の下の窓の幅の下限・下端 (キャンバス y・下から＝画面の上から 1056)</summary>
        const float R3U_WinMinWPc = 440f, R3U_WinBottomPc = 24f;
        /// <summary>r3 のスマホの窓の幅の下限</summary>
        const float R3U_WinMinWPhone = 400f;
        /// <summary>窓と重なって畳んだ人形の足元の札 (窓を閉じた時に戻す。組み直しは札ごと作り直すので戻さなくてよい)</summary>
        static readonly List<GameObject> _r3uHiddenDollTags = new List<GameObject>();

        /// <summary>生きている敵の帳面の左端のうちいちばん左 (キャンバス x)。無ければ +∞</summary>
        static float R3U_LeftmostLedger(GameRoot g, GameState st)
        {
            float x = float.MaxValue;
            if (g == null || g.Battle == null || st == null) return x;
            for (int i = 0; i < st.Enemies.Count; i++)
            {
                if (st.Enemies[i].Hp <= 0) continue;
                Rect r;
                if (PanelChildRect(g.Battle.EnemyPanel(i), "strip", 0f, out r)) x = Mathf.Min(x, r.xMin);
            }
            return x;
        }

        /// <summary>
        /// 窓の縁が人形の体を切らないように窓を直す (2026-10-02 読み合わせの指摘: 上端だけを見ていて、左右の縁がスマホの人形5体目・PC の人形2体目と7体目を縦に切った)。
        /// 人形の体は絵の箱 (透明な余白込み＝layout-check L13 の箱)。窓の縦の範囲に掛かる人形について: 左の縁が体の中なら体の左 −8 まで広げる (minX より左へは出さない。出せなければ体の右 +8 まで狭める・幅 minW を割るなら触らない)。
        /// 右の縁も同じ (maxX まで広げる。出せなければ体の左 −8 まで狭める)。上の縁が体の中ならその人形の頭の 8 上まで上げる (topLimit まで)。2回まわす
        /// (広げた・上げた分で新しく掛かる人形も見る)。人形の札を畳むのは R3U_HideDollTags (apply の時だけ)
        /// </summary>
        static Rect R3U_FitDolls(GameRoot g, Rect win, float topLimit, float minX, float maxX, float minW)
        {
            float x0 = win.xMin, x1 = win.xMax, y0 = win.yMin, top = win.yMax;
            var area = g != null && g.Battle != null && g.Battle.FieldLayer != null ? g.Battle.FieldLayer.Find("dolls") : null;
            if (area == null) return win;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < area.childCount; i++)
                {
                    var dp = area.GetChild(i) as RectTransform;
                    if (dp == null || !dp.gameObject.activeInHierarchy) continue;
                    Rect r;
                    if (!PanelChildRect(dp, "sprite", 0f, out r)) continue;   // 絵の箱 (透明な余白も含む): layout-check L13 と同じ箱で測る (余白の内側で切ると L13 が「縁で切る」と数える)
                    if (r.yMax <= y0 || r.yMin >= top) continue;   // 窓の縦の範囲に掛からない
                    if (r.xMin < x0 && r.xMax > x0)
                    {
                        if (r.xMin - 8f >= minX) x0 = r.xMin - 8f;
                        else if (x1 - (r.xMax + 8f) >= minW) x0 = r.xMax + 8f;
                    }
                    if (r.xMin < x1 && r.xMax > x1)
                    {
                        if (r.xMax + 8f <= maxX) x1 = r.xMax + 8f;
                        else if ((r.xMin - 8f) - x0 >= minW) x1 = r.xMin - 8f;
                    }
                    if (r.xMax > x0 && r.xMin < x1 && top > r.yMin && top < r.yMax && r.yMax + 8f <= topLimit) top = r.yMax + 8f;
                }
            return Rect.MinMaxRect(x0, y0, x1, top);
        }

        /// <summary>窓 full と重なる人形の足元の札を、窓が開いている間だけ畳む (閉じると R3U_RestoreDollTags が戻す)</summary>
        static void R3U_HideDollTags(GameRoot g, Rect full)
        {
            _r3uHiddenDollTags.RemoveAll(x => x == null);   // 組み直しで捨てた札
            var area = g != null && g.Battle != null && g.Battle.FieldLayer != null ? g.Battle.FieldLayer.Find("dolls") : null;
            if (area == null) return;
            for (int i = 0; i < area.childCount; i++)
            {
                var dp = area.GetChild(i) as RectTransform;
                if (dp == null) continue;
                Rect r;
                if (!PanelChildRect(dp, "tag", 0f, out r) || !r.Overlaps(full)) continue;
                var tag = dp.Find("tag");
                tag.gameObject.SetActive(false);
                _r3uHiddenDollTags.Add(tag.gameObject);
            }
        }

        /// <summary>窓を閉じた時に、畳んだ人形の足元の札を戻す (BattleView.CloseReactionWindow)</summary>
        public static void R3U_RestoreDollTags()
        {
            foreach (var go in _r3uHiddenDollTags) if (go != null) go.SetActive(true);
            _r3uHiddenDollTags.Clear();
        }

        /// <summary>
        /// r3 の PC の「下の窓」(仕様 §8-1)。右端 = いちばん左の生きている敵の帳の左 −12 (行動中の敵の帳も含む)・左端の限界 = 足元の帳の右 +12・
        /// 幅 = min(W0, 右端−左端の限界) で下限 440・下端 = 画面の下 −24。窓の上端が主人公の足元より上に出て主人公の体と x で重なるなら、左端を体の右 +12 へ寄せる (下限 440)。
        /// 寄せると下限を割る時 (4体の時の右端 1012 など) は、主人公を足元の帳ごと丸ごと覆う: 左端 = min(体の左 −12, 足元の帳の左 −8) (匣の右 +12 より左へは出さない)・上端 = 頭の上 +8
        /// (窓の中に「温存すると HP 80 → 67」があるので足元の帳を覆っても数字は消えない。スマホ §8-2 と同じ考え。2026-10-02 読み合わせの指摘＝旧は二周目の置き方へ戻り、
        /// r3 では自分の欄を畳めないので主人公を縦に切った)。窓の縁が人形の体を切るなら直し (R3U_FitDolls)、高くなった窓の右端は行動していない敵の意図の札の手前で止める。
        /// false を返すのは上部バーに当たる時だけ (呼ぶ側は二周目の置き方へ戻す)。apply の時だけ窓と重なる人形の足元の札を畳む
        /// </summary>
        public static bool R3PcWindowRect(GameRoot g, GameState st, Vector2 cs, float W0, float H, out Rect rect, bool apply = true)
        {
            rect = default(Rect);
            float lm = R3U_LeftmostLedger(g, st);
            float x1 = (lm < float.MaxValue ? lm : cs.x) - 12f;
            float xL = (R3U_FootRight > 0f ? R3U_FootRight : 500f) + 12f;
            float W = Mathf.Max(R3U_WinMinWPc, Mathf.Min(W0, x1 - xL));
            float x0 = x1 - W;
            float y0 = R3U_WinBottomPc, top = y0 + H;
            float topLimit = cs.y - TopH - 12f;
            if (top > topLimit) return false;
            float minX = xL;
            Rect lr;
            if (LeaderBodyRect(g, out lr) && top > lr.yMin && x0 < lr.xMax + 12f && x0 + W > lr.xMin)
            {
                float xs = lr.xMax + 12f, ws = Mathf.Min(W0, x1 - xs);
                if (ws >= R3U_WinMinWPc) { x0 = xs; W = ws; minX = xs; }
                else
                {   // 主人公を丸ごと覆う (足元の帳ごと。窓の縁で帳を切らない。匣の右 +12 より左へは出さない)
                    x0 = Mathf.Max(Mathf.Min(lr.xMin - 12f, R3U_FootLeftPc - 8f), UiKit.Edge + R3U_BoxW + 12f);
                    W = Mathf.Min(Mathf.Max(W0, lr.xMax + 12f - x0), x1 - x0);
                    top = Mathf.Max(top, lr.yMax + 8f);
                    if (top > topLimit) return false;
                    minX = x0;
                }
            }
            var win = Rect.MinMaxRect(x0, y0, x0 + W, top);
            win = R3U_FitDolls(g, win, topLimit, minX, x1, R3U_WinMinWPc);
            // 主人公の足元より上へ伸びた窓: 行動していない敵の意図の札に右端を掛けない (掛かるなら手前で止める。幅の下限を割るなら止めない)
            if (win.yMax > (lr.height > 0f ? lr.yMin : y0 + 350f) && g != null && g.Battle != null && st != null)
                for (int j = 0; j < st.Enemies.Count; j++)
                {
                    if (st.Enemies[j].Hp <= 0) continue;
                    Rect r;
                    if (PanelChildRect(g.Battle.EnemyPanel(j), "intent-tag", 4f, out r) && r.Overlaps(win) && r.xMin - 12f - win.xMin >= R3U_WinMinWPc)
                        win = Rect.MinMaxRect(win.xMin, win.yMin, r.xMin - 12f, win.yMax);
                }
            if (apply) R3U_HideDollTags(g, win);
            rect = win;
            return true;
        }

        /// <summary>
        /// r3 のスマホの窓 (仕様 §8-2)。主人公を窓の縁で切らない (覆うなら丸ごと)。
        /// 2026-10-02 三周目 直しの輪1 (反証のまとめ (d)「窓の左端 x 304〜367 が主人公の絵の箱を縁で切る・『次のターン 上限 4』を途中で切る・下半分が空く」):
        /// ・主人公は絵の箱 (透明な余白込み＝layout-check L13 の箱。LeaderSpriteBox) で測る。旧は透明な余白を除いた体で測ったので、
        ///   ひなた (箱の左 207) は x0 232 の縁で箱を切った。
        /// ・窓が主人公の足元より下に収まらない (＝主人公を覆う) 時は、HP の区画の右 +8 が箱の左 −8 より右なら 足元の帳ごと覆う: 左端は画面の左の余白から
        ///   (切り欠きの右・UiKit.SafeLeft)。右端は覆わない時と同じ (HP の区画の右 +8 +W0。主人公の右 +8 までは必ず) で、左へ伸ばすだけ。
        /// ・上端は主人公の頭の 8 上・下端は足元の 4 下まで (箱を丸ごと中に)。高さは max(中身の高さ, 主人公の丈)＝中身に合わせる (旧は下端が足元の線に貼り付き、
        ///   小さいギアの窓は上の半分が中身・下の半分が空いた)。
        /// ・下端は、横に重なるエナジーの輪・灯籠・足元の帳を縁で切らない (切るなら丸ごと含むか丸ごと外す。候補のうち窓がいちばん低く、同じなら上端が低い置き方)。
        /// ・上の帯の下 +6 に収まらなければ、上部バーの下まで上げてよい (上の帯の札を覆う。旧は二周目の置き方へ戻り、主人公を縦に切った)。
        ///   それでも入らない背の高い窓 (候補が2つ以上の確認の窓) は下端を足元の線まで下げ、横に重なるエナジーの輪・灯籠を窓の間だけ畳む
        ///   (縁で輪の数字を切らない。足元の帳は切らない)。それでも入らない時だけ false。
        /// ・「次のターン 上限 N」の札は、窓に重なるなら窓が開いている間だけ畳む (R3U_HideOverlap。apply の時)。
        /// 右端は 行動中の敵の帳の左 −12 (ギアの窓は いちばん左の敵の帳の左 −12) で頭打ち (下限 400。割る時は他の敵の体と帳に掛けてよい)。
        /// 窓の縁が人形の体を切るなら直す (R3U_FitDolls)。行動していない敵の意図の札に掛かる時は false (二周目の置き方へ)
        /// </summary>
        public static bool R3PhoneWindowRect(GameRoot g, GameState st, Vector2 cs, float W0, float H, int actingEnemy, out Rect rect, out bool coverLedger, bool apply = true)
        {
            rect = default(Rect); coverLedger = false;
            float line = BattleView.StatusLineY;
            float hpRight = R3U_FootHpRight > 0f ? R3U_FootHpRight : UiKit.Edge + R3U_PhoneHpW;
            float xDef = hpRight + 8f;
            float capX;
            if (actingEnemy >= 0 && g != null && g.Battle != null)
            {
                Rect sr;
                capX = PanelChildRect(g.Battle.EnemyPanel(actingEnemy), "strip", 0f, out sr) ? sr.xMin - 12f : cs.x - 12f;
            }
            else { float lm = R3U_LeftmostLedger(g, st); capX = (lm < float.MaxValue ? lm : cs.x) - 12f; }
            float maxX = Mathf.Min(capX, cs.x - 12f);
            float topLimit = cs.y - Mathf.Max(PhoneBandBottom > 0f ? PhoneBandBottom : 0f, 170f) - 6f;   // 上の帯の下 +6
            float topHard = cs.y - TopH - 6f;                                                               // 上部バーの下 (覆う窓が上の帯に入らない時だけ)
            Rect box;
            bool hasLeader = LeaderSpriteBox(g, out box);
            float x0, xr, bottom, top, lim = topLimit;
            bool hideLeft = false;   // 背の高い窓が下端でエナジーの輪・灯籠に掛かる時だけ、窓の間それらを畳む
            bool below = !hasLeader || line + H <= box.yMin - 4f;   // 窓が主人公の足元より下に収まる (小さい窓・主人公がいない)
            if (below)
            {   // 今の置き方: HP の区画の右から・下端は足元の線
                x0 = xDef;
                xr = Mathf.Min(maxX, x0 + W0);
                if (xr - x0 < R3U_WinMinWPhone) xr = x0 + R3U_WinMinWPhone;
                if (xr > cs.x - 12f) return false;
                bottom = line; top = line + H;
                if (top > topLimit) return false;
            }
            else
            {
                coverLedger = xDef > box.xMin - 8f;
                x0 = coverLedger ? UiKit.Edge : xDef;
                xr = Mathf.Min(maxX, Mathf.Max(xDef + W0, box.xMax + 8f));
                if (xr - x0 < R3U_WinMinWPhone) xr = x0 + R3U_WinMinWPhone;
                if (xr > cs.x - 12f) return false;
                float headTop = box.yMax + 8f, feetMax = Mathf.Max(line, box.yMin - 4f);
                float needH = Mathf.Max(H, headTop - feetMax);
                // 下端が縁で切ってはいけない物 (横に重なる物だけ)。足元の帳は、覆わない時は HP の区画だけ (からくりの区画は TrimSelfStrip が畳む)
                var foot = new List<Rect>();
                var left = new List<Rect>();
                Rect r;
                if (R3U_PhoneFootRect.width > 0f) foot.Add(coverLedger ? R3U_PhoneFootRect : Rect.MinMaxRect(R3U_PhoneFootRect.xMin, R3U_PhoneFootRect.yMin, hpRight, R3U_PhoneFootRect.yMax));
                if (g != null && R3U_CanvasRect(g, g.Anchor("energy"), out r)) left.Add(r);
                if (g != null && R3U_CanvasRect(g, g.Anchor("light"), out r)) left.Add(r);
                float ox0 = x0, oxr = xr;
                foot.RemoveAll(o => o.xMax <= ox0 + 1f || o.xMin >= oxr - 1f);
                left.RemoveAll(o => o.xMax <= ox0 + 1f || o.xMin >= oxr - 1f);
                float bestB = float.NaN, bestTop = 0f, bestH = float.MaxValue;
                // 1) 上の帯の下に収める 2) 上部バーの下まで上げる 3) それでも入らない背の高い窓 (スマホで候補が2つ以上) は、下端を足元の線まで下げ、
                //    横に重なるエナジーの輪・灯籠は窓が開いている間だけ畳む (縁で数字を切らない。足元の帳は切らない)
                for (int pass = 0; pass < 3 && float.IsNaN(bestB); pass++)
                {
                    lim = pass == 0 ? topLimit : topHard;
                    var obs = new List<Rect>(foot);
                    if (pass < 2) obs.AddRange(left);
                    var cands = new List<float> { headTop - needH, line };
                    foreach (var o in obs) { cands.Add(o.yMin - 2f); cands.Add(o.yMax + 2f); }
                    foreach (var c0 in cands)
                    {
                        float b = Mathf.Clamp(c0, line, feetMax);
                        bool cut = false;
                        foreach (var o in obs) if (b > o.yMin + 1f && b < o.yMax - 1f) { cut = true; break; }
                        if (cut) continue;
                        float t = Mathf.Max(b + needH, headTop);
                        if (t > lim) continue;
                        float hh = t - b;
                        if (hh < bestH - 0.5f || (Mathf.Abs(hh - bestH) <= 0.5f && t < bestTop)) { bestB = b; bestTop = t; bestH = hh; }
                    }
                    if (!float.IsNaN(bestB) && pass == 2) hideLeft = true;
                }
                if (float.IsNaN(bestB)) return false;
                bottom = bestB; top = bestTop;
                if (coverLedger) x0 = UiKit.SafeLeft(UiKit.Edge, cs.y - top, cs.y - bottom);   // 窓の縦の範囲 (上から) に掛かる切り欠きの右から。右端は保つ
            }
            var win = Rect.MinMaxRect(x0, bottom, xr, top);
            win = R3U_FitDolls(g, win, lim, x0, maxX, R3U_WinMinWPhone);
            if (g != null && g.Battle != null && st != null)
                for (int j = 0; j < st.Enemies.Count; j++)
                {
                    if (j == actingEnemy || st.Enemies[j].Hp <= 0) continue;
                    Rect r;
                    if (PanelChildRect(g.Battle.EnemyPanel(j), "intent-tag", 4f, out r) && r.Overlaps(win)) return false;
                }
            if (apply)
            {
                R3U_HideDollTags(g, win);
                if (g != null && g.Battle != null && g.Battle.UiLayer != null) R3U_HideOverlap(g, g.Battle.UiLayer.Find("energyNext") as RectTransform, win);   // 「次のターン 上限 N」を窓の縁で切らない
                if (hideLeft && g != null) { R3U_HideOverlap(g, g.Anchor("energy"), win); R3U_HideOverlap(g, g.Anchor("light"), win); }
            }
            rect = win;
            return true;
        }

        /// <summary>rt のキャンバスの矩形 (窓の層 UiLayer の左下が原点＝窓の置き場と同じ座標)。見えていない・無い時は false</summary>
        static bool R3U_CanvasRect(GameRoot g, RectTransform rt, out Rect r)
        {
            r = default(Rect);
            var basis = g != null && g.Battle != null ? g.Battle.UiLayer : null;
            if (rt == null || basis == null || !rt.gameObject.activeInHierarchy) return false;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var p = basis.InverseTransformPoint(c[i]);
                x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
            }
            var b = basis.rect;
            r = Rect.MinMaxRect(x0 - b.xMin, y0 - b.yMin, x1 - b.xMin, y1 - b.yMin);
            return r.width > 0f && r.height > 0f;
        }

        /// <summary>rt が窓 full と重なるなら、窓が開いている間だけ畳む (閉じると R3U_RestoreDollTags が人形の札と一緒に戻す。組み直しは作り直すので戻さなくてよい)</summary>
        static void R3U_HideOverlap(GameRoot g, RectTransform rt, Rect full)
        {
            Rect r;
            if (!R3U_CanvasRect(g, rt, out r) || !r.Overlaps(full)) return;
            rt.gameObject.SetActive(false);
            _r3uHiddenDollTags.Add(rt.gameObject);
        }

        /// <summary>r3 の窓の置き場 (確認の窓・ギアの窓・持ち物の一覧の共通の入口)。apply = 人形の札を畳み・スマホで足元の帳のからくりの区画に掛かるなら畳む
        /// (false = 幅の見積りだけ。窓の中身の幅を先に決める時)。入らなければ false</summary>
        public static bool R3WindowRect(GameRoot g, GameState st, Vector2 cs, float W0, float H, int actingEnemy, out Rect rect, bool apply = true)
        {
            if (!UiKit.Phone) return R3PcWindowRect(g, st, cs, W0, H, out rect, apply);
            bool cover;
            if (!R3PhoneWindowRect(g, st, cs, W0, H, actingEnemy, out rect, out cover, apply)) return false;
            if (apply && !cover && g != null && g.Battle != null) TrimSelfStrip(g.Battle.SelfArea, rect.xMin - 8f);
            return true;
        }

        /// <summary>x0〜x1 に掛かる白の人形の足元の札の上端 (キャンバス y)。無ければ -∞</summary>
        static float DollTagsTop(GameRoot g, float x0, float x1)
        {
            float top = float.NegativeInfinity;
            var area = g.Battle != null && g.Battle.FieldLayer != null ? g.Battle.FieldLayer.Find("dolls") : null;
            if (area == null) return top;
            Rect r;
            for (int i = 0; i < area.childCount; i++)
            {
                var dp = area.GetChild(i) as RectTransform;
                if (dp != null && dp.gameObject.activeInHierarchy && PanelChildRect(dp, "tag", 0f, out r) && r.xMax > x0 && r.xMin < x1) top = Mathf.Max(top, r.yMax);
            }
            return top;
        }

        /// <summary>敵の入れ物 (左下基準) の子 (アンカー (0.5, 0)) のキャンバス上の矩形。pad だけ外へ広げる</summary>
        static bool PanelChildRect(RectTransform pan, string child, float pad, out Rect r)
        {
            r = default(Rect);
            var c = pan != null ? pan.Find(child) as RectTransform : null;
            if (c == null || !c.gameObject.activeInHierarchy) return false;
            float pcx = (pan.offsetMin.x + pan.offsetMax.x) / 2f, py = pan.offsetMin.y;
            r = Rect.MinMaxRect(pcx + c.offsetMin.x - pad, py + c.offsetMin.y - pad, pcx + c.offsetMax.x + pad, py + c.offsetMax.y + pad);
            return true;
        }

        /// <summary>リーダーの絵の、透明な余白を除いた体の矩形 (キャンバス座標。武器も含む)。F25</summary>
        static bool LeaderBodyRect(GameRoot g, out Rect r)
        {
            r = default(Rect);
            var area = g != null ? g.Anchor("player") : null;
            var spr = area != null ? area.Find("sprite") as RectTransform : null;
            if (spr == null || !spr.gameObject.activeInHierarchy) return false;
            var box = Rect.MinMaxRect(area.offsetMin.x + spr.offsetMin.x, area.offsetMin.y + spr.offsetMin.y, area.offsetMin.x + spr.offsetMax.x, area.offsetMin.y + spr.offsetMax.y);
            var img = spr.GetComponent<Image>();
            var s = img != null ? img.sprite : null;
            if (s == null || s.rect.width <= 0f) { r = box; return true; }
            float k = box.width / s.rect.width;
            var side = Creature.SideMargins(s);
            float top = Creature.TopMargin(s) * k;
            r = Rect.MinMaxRect(box.xMin + side.x * k, box.yMin, box.xMax - side.y * k, box.yMax - top);
            if (r.width <= 0f || r.height <= 0f) r = box;
            return true;
        }

        /// <summary>リーダーの絵の箱 (透明な余白を含む・キャンバス座標)。layout-check L13 が「窓の縁で切らない」を測る箱と同じ (スマホの窓。2026-10-02 三周目 直しの輪1)</summary>
        static bool LeaderSpriteBox(GameRoot g, out Rect r)
        {
            r = default(Rect);
            var area = g != null ? g.Anchor("player") : null;
            var spr = area != null ? area.Find("sprite") as RectTransform : null;
            if (spr == null || !spr.gameObject.activeInHierarchy) return false;
            r = Rect.MinMaxRect(area.offsetMin.x + spr.offsetMin.x, area.offsetMin.y + spr.offsetMin.y, area.offsetMin.x + spr.offsetMax.x, area.offsetMin.y + spr.offsetMax.y);
            return r.width > 0f && r.height > 0f;
        }

        /// <summary>敵の絵の、透明な余白を除いた体の矩形 (キャンバス座標)</summary>
        static bool EnemyBodyRect(RectTransform pan, out Rect r)
        {
            r = default(Rect);
            Rect box;
            if (!PanelChildRect(pan, "sprite", 0f, out box)) return false;
            var img = pan.Find("sprite").GetComponent<Image>();
            var s = img != null ? img.sprite : null;
            if (s == null || s.rect.width <= 0f) { r = box; return true; }
            float k = box.width / s.rect.width;
            var side = Creature.SideMargins(s);
            float top = Creature.TopMargin(s) * k;
            r = Rect.MinMaxRect(box.xMin + side.x * k, box.yMin, box.xMax - side.y * k, box.yMax - top);
            if (r.width <= 0f || r.height <= 0f) r = box;
            return true;
        }

        /// <summary>暗転の板 (キャンバス座標・左下基準)。押しても何も起きない = 手札を触れなくする</summary>
        static void Dim(RectTransform root, float x0, float y0, float x1, float y1, Color color)
        {
            if (x1 <= x0 || y1 <= y0) return;
            var pan = UiKit.Pan(root, color, "dim");
            UiKit.Anchor(pan.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x0, y0), new Vector2(x1, y1));
            pan.raycastTarget = true;
        }

        // ---- 対象選択・モード選択 ----

        static void BuildTargetBanner(GameRoot g, RectTransform root)
        {
            var pan = PaperFx.Sheet(root, PaperFx.Tag, "targetBanner", PaperFx.BrassLight);
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -TopH - 66f), new Vector2(420f, -TopH - 14f));
            var t = UiKit.Txt(pan.transform, "「" + g.Pending.Card.Def.Name + (UiKit.Phone ? "」の対象を選ぶ — 敵をタップ" : "」の対象を選ぶ — 敵をクリック（またはカードを敵へドラッグ）"), 18, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 0f), new Vector2(-150f, 0f));
            var b = UiKit.Btn(pan.transform, "取り消し", delegate { g.CancelPending(); }, 16);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-136f, -20f), new Vector2(-16f, 20f));
        }

        static void BuildModeChooser(GameRoot g, RectTransform root, GameState st)
        {
            CardInstance card = null;
            for (int i = 0; i < st.Player.Hand.Count; i++) if (st.Player.Hand[i].Uid == g.ModeChoiceUid) card = st.Player.Hand[i];
            if (card == null) { g.ModeChoiceUid = null; return; }
            var pan = UiKit.Frame(root, Theme.Panel, Color.white, "modeChooser", 3f);
            if (R3 && UiKit.Phone)
            {   // r3 のスマホ (2026-10-02 仕様 §8-3): 足元の帳の右 ＋12 から・下端は手札の上端 ＋8 (手札の上 60 だと主人公の足元に掛かる)
                float mx = (R3U_FootRight > 0f ? R3U_FootRight : 384f) + 12f, my = HandY + CardView.H * CardScale + 8f;
                UiKit.Anchor(pan.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(mx, my), new Vector2(mx + 720f, my + 90f));
            }
            else UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-360f, HandY + CardView.H * CardScale + 60f), new Vector2(360f, HandY + CardView.H * CardScale + 150f));
            var inner = UiKit.NewRect("inner", pan.transform);
            UiKit.Stretch(inner, 16f, 16f, 12f, 12f);
            var hg = UiKit.Horz(inner, 10, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = true;
            var title = UiKit.Deco(inner, card.Def.Name, 18, PaperFx.Ink, TextAnchor.MiddleLeft);
            UiKit.Le(title, 100f, -1f, 160f, -1f);
            bool playable = BattleView.CanActNow(g, st, card);   // 扇の沈みと同じ判定 (2026-09-29 p12)
            if (card.Def.Modes != null)
            {
                for (int m = 0; m < card.Def.Modes.Count; m++)
                {
                    int mi = m;
                    string label = (m + 1) + ": " + CardText.Short(ModeText(card.Def.Modes[m]), 18);
                    UiKit.Btn(inner, label, delegate { g.ModeChoiceUid = null; PlayCard(g, card, mi); }, 16, playable);
                }
            }
            else if (Effects.IsPlayableFromHand(card, st))
            {
                UiKit.Btn(inner, "プレイ", delegate { g.ModeChoiceUid = null; PlayCard(g, card, null); }, 16, playable);
            }
            // 仕込み札 (手札からはプレイできない札) には押せない「プレイ」を出さない＝「仕込む／やめる」の確認だけ。
            // 取り出しが廃止されて仕込むと戻せないので、窓は押し間違いの確認として残す (2026-09-29 p05)
            // 「仕込む」は青緑の紙 (色の規約 規則2・ホバーの「仕込む」と同じ。2026-09-30 F27: 窓の中だけ既定の紙で「やめる」と見分けられなかった)
            if (SetBase.CanSetCard(st, card.Uid)) UiKit.Btn(inner, "仕込む", delegate { g.ModeChoiceUid = null; g.DoCombat(new Command_SetCard { CardUid = card.Uid }); }, 16, true, PaperFx.ManaLight);
            UiKit.Btn(inner, "やめる", delegate { g.ModeChoiceUid = null; g.Rebuild(); }, 16);
        }

        static string ModeText(CardMode m)
        {
            var parts = new List<string>();
            for (int i = 0; i < m.Effects.Count; i++) parts.Add(CardText.EffectLine(m.Effects[i], null));
            return string.Join(" / ", parts.ToArray());
        }

        // ---- 追加コスト・選択のピッカー (モーダル) ----

        public static IReadOnlyList<CardInstance> DeckChoosePool(GameState st, string kind, CardDef def = null) { return CombatScreen.DeckChoosePool(st, kind, def); }
        public static List<CardInstance> UpgradablePool(GameState st, CardInstance self) { return CombatScreen.UpgradablePool(st, self); }

        static void BuildPicker(GameRoot g, RectTransform root, GameState st, string need)
        {
            var p = g.Pending;
            IReadOnlyList<CardInstance> pool;
            List<string> selected;
            int want;
            string title;
            if (need == "discard") { pool = ExceptSelf(st.Player.Hand, p.Card.Uid); selected = p.Discard; want = p.DiscardNeed; title = "追加コスト: 手札を" + want + "枚捨てる"; }
            else if (need == "exhaust") { pool = ExceptSelf(st.Player.Hand, p.Card.Uid); selected = p.Exhaust; want = p.ExhaustNeed; title = "追加コスト: 手札を" + want + "枚消滅させる"; }
            else if (need == "retrieve") { pool = st.Player.ExhaustPile; selected = new List<string>(); want = 1; title = "消滅置き場から1枚選ぶ"; }
            else if (need == "deck") { pool = DeckChoosePool(st, p.DeckKind, p.Card.Def); selected = p.DeckSel; want = p.DeckNeed; bool onlyTraps = false; foreach (var fe in p.Card.Def.Effects) if (fe.Effect == "searchDeck" && fe.CardType == "reaction") onlyTraps = true; title = (p.DeckKind == "searchDeck" ? (onlyTraps ? "山札の仕込み札" : "山札") : p.DeckKind == "retrieveFromDiscard" ? "捨て札" : "山札か捨て札") + "から" + want + "枚選ぶ"; }
            else if (need == "hand") { pool = UpgradablePool(st, p.Card); selected = p.HandSel; want = p.HandNeed; title = "手札から" + want + "枚を鍛える"; }
            else if (need == "permanent")
            {
                var retainers = new List<CardInstance>();
                for (int i = 0; i < st.Player.Permanents.Count; i++) { var q = st.Player.Permanents[i]; if (q.Def.Retainer == true && q.Innate != true) retainers.Add(q); }
                pool = retainers; selected = new List<string>(); want = 1; title = p.NeedSacrifice ? "捧げる人形を選ぶ" : "人形を1体選ぶ";   // 「従者」→「人形」(2026-09-24 T3)。写し灯・継ぎ火・永遠の灯も同じ窓
            }
            else { UiKit.Txt(root, "未対応の選択: " + need, 20, UiKit.ColBadInk); return; }

            var inner = Modal(root, 1400f, 720f, "picker");
            UiKit.Head(inner, p.Card.Def.Name + " — " + title + "（選択中 " + selected.Count + " / " + want + "）", 24);
            var content = UiKit.Scroll(inner, false, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.06f), 16, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, 360f, -1f, 360f, -1f, 1f);
            var lg = content.GetComponent<HorizontalLayoutGroup>();
            if (lg != null) { lg.childForceExpandWidth = false; lg.childForceExpandHeight = false; lg.childAlignment = TextAnchor.MiddleLeft; }
            if (pool.Count == 0)
            {
                var none = UiKit.Txt(content, "（候補がありません）", 16, PaperFx.InkSoft);
                UiKit.Le(none, 300f, 40f, 300f, 40f);
            }
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                string uid = c.Uid;
                bool isSel = selected.Contains(uid);
                var wrap = UiKit.NewRect("cand", content);
                UiKit.Le(wrap, 220f, 330f, 220f, 330f);
                var cv = CardView.Build(wrap, c, st, !isSel, true, "cand-card");
                CardPopup.Attach(g, cv, c, delegate { return g.Rs != null ? g.Rs.Combat : null; }, true);
                cv.anchoredPosition = new Vector2(0f, 30f);
                cv.localScale = Vector3.one * 0.86f;
                string cap = need;
                var pick = UiKit.Btn(wrap, isSel ? "選択中" : "選ぶ", delegate { OnPick(g, cap, uid); }, 16, true, isSel ? PaperFx.BrassLight : Color.white);
                var ple = pick.GetComponent<LayoutElement>();
                if (ple != null) UnityEngine.Object.Destroy(ple);
                UiKit.Anchor(pick.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 0f), new Vector2(70f, 44f));
            }
            CenteredButton(inner, "取り消し", delegate { g.CancelPending(); }, 18, 260f, 50f);
        }

        static void OnPick(GameRoot g, string need, string uid)
        {
            var p = g.Pending;
            if (p == null) return;
            if (need == "retrieve") { p.RetrieveUid = uid; g.SubmitIfReady(); return; }
            if (need == "permanent") { p.PermanentUid = uid; g.SubmitIfReady(); return; }
            List<string> list = need == "discard" ? p.Discard : need == "exhaust" ? p.Exhaust : need == "deck" ? p.DeckSel : p.HandSel;
            if (list.Contains(uid)) list.Remove(uid); else list.Add(uid);
            g.SubmitIfReady();
        }

        static IReadOnlyList<CardInstance> ExceptSelf(IReadOnlyList<CardInstance> hand, string selfUid)
        {
            var list = new List<CardInstance>();
            for (int i = 0; i < hand.Count; i++) if (hand[i].Uid != selfUid) list.Add(hand[i]);
            return list;
        }
    }
}
