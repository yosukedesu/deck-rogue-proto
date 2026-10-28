// MapScreen.cs — マップ画面 (M3・2026-09-07): 本家式の縦スクロール地図。
// 2026-10-28 品質スプリント 案1「夜の森の絵地図」(カンバス https://claude.ai/artifact/C8WiiTPfMSPD4jPFE2pi92 の Main・ユーザー裁定):
// 戦闘の森 (幕1 の箱庭) の地面・道・木・岩の絵で、見下ろしの絵地図を組む。地面は夜の草のタイル (1ドット=2px)、道は土の踏み跡、
// 節は空き地に立つ 32 ドットの印 (2倍)、両脇は針葉樹、上は霧に沈む。現在地は真鍮の輪 (地図にキャラは置かない。2026-10-28 ユーザー「マップにキャラいらない」)。
// 材料 (夜の色に焼いた複製・踏み跡の帯・蛍の動き) は MapForest.cs。
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class MapScreen
    {
        const float MapW = 1020f;      // 節の格子の幅 (列の間隔 150。カンバスの案1)
        const float RowH = 120f;       // 1行の高さ
        const float PadY = 220f;       // 上下の余白 (下は出発の道標と駒・下のボタンの帯、上はボスの名札)
        static float _ox = 0f;         // 節の格子の左端 (content の中の x)。地図は画面の幅いっぱいなので、格子を中央へ寄せる
        static float NodeSize { get { return UiKit.Phone ? 74f : 60f; } }   // 画面の切り欠きの寄せの目安 (2026-09-14)
        const int Cols = 7;

        /// <summary>
        /// 道の中央線の色 (2026-10-28 ユーザー「通った道と選択肢の道の中央線を見やすく。輪郭線と似た色だと分かりにくい」→ 候補 4 つ → 裁定 案1):
        /// 通った道の足跡＝真鍮・選択肢の破線＝青緑 (縁の淡い紙色・土のくすんだ紫から最も離れる)。
        /// 撮影の鍵 mapline=<n> で比べた候補を戻せる: 2 通った 青緑/選択肢 真鍮の明・3 朱/真鍮の明・4 真鍮の明/朱・9 旧 (真鍮の明/紙の白)
        /// </summary>
        public static int LinePalette = 0;
        /// <summary>撮影の計測 (2026-10-28 ユーザー「スクロールがカクつく」): mapscroll=1 で地図を 4 秒で1往復させ続ける (perf=<秒> と組むと PerfProbe が毎フレームの時間を記録する)</summary>
        public static bool AutoScroll = false;
        /// <summary>mapscroll=wheel: 往復の代わりにホイールの刻みを 3 回送り、content の位置を毎フレーム [MapWheel] に記録する (滑らかに寄るかの確認)</summary>
        public static bool AutoScrollWheel = false;
        /// <summary>計測の切り分け (mapdbg=nolife,noforest,nostage,canvas を「,」で並べる)。普段は空</summary>
        public static string DebugFlags = "";
        static bool Dbg(string k) { return !string.IsNullOrEmpty(DebugFlags) && ("," + DebugFlags + ",").Contains("," + k + ","); }
        static Color TakenLine { get { switch (LinePalette) { case 2: return PaperFx.Mana; case 3: return PaperFx.Ember; case 4: case 9: return PaperFx.BrassLight; default: return PaperFx.Brass; } } }
        static Color AvailLine { get { switch (LinePalette) { case 2: case 3: return PaperFx.BrassLight; case 4: return PaperFx.Ember; case 9: return PaperFx.Paper3; default: return PaperFx.Mana; } } }

        /// <summary>他の画面の上に重ねる読み取り専用の地図 (2026-09-12 ユーザー「マップは常に見れるようにして」)。進路は選べない・閉じるだけ</summary>
        public static void Overlay(GameRoot g, RectTransform root) { Build(g, root, true); }

        public static void Build(GameRoot g, RectTransform root) { Build(g, root, false); }

        public static string ActName(int act)
        {
            switch (act) { case 1: return "坑口の森"; case 2: return "先代の坑道"; case 3: return "埋もれた古代都市"; default: return "マップ"; }
        }

        static void Build(GameRoot g, RectTransform root, bool overlay)
        {
            var run = g.Rs;
            if (!overlay && !Dbg("nostage")) BattleScreen.BuildBackground(root, run.Act);
            if (overlay)
            {
                var dim = UiKit.Pan(root, new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.82f), "dim");   // 下の画面 (手札・吹き出し) を塞ぐ
                dim.raycastTarget = true;
                UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            }
            int recipeCount = 0;   // ⭐ いま手元で作れるレシピ対の数 (工房ノードに添える。2026-09-12)
            try { recipeCount = Fusion.RecipePairsInDeck(run.Deck).Count; } catch (Exception) { }

            var view = UiKit.NewRect("mapview", root);
            // 画面の切り欠き (パンチホール＝左端の縦の中央) に左の列のノードが掛かる狭い端末では、格子を右へ寄せる (2026-09-29 p10。S25 と PC は 0)
            var mcs = BattleScreen.CanvasSize(root);
            float mapShift = Mathf.Max(0f, UiKit.CutoutLeft(RunUi.TopH, mcs.y) - (mcs.x / 2f - MapW / 2f + 60f - NodeSize / 2f));
            mapShift = Mathf.Min(mapShift, Mathf.Max(0f, mcs.x / 2f - MapW / 2f));   // 右の端からはみ出さない
            _ox = Mathf.Max(0f, (mcs.x - MapW) / 2f) + mapShift;
            float viewTop = overlay ? RunUi.TopH + 56f : 0f, viewBottom = overlay ? 90f : 0f;   // 重ねる時は見出しと「閉じる」の分だけ狭める。地図の画面は全面 (上部バーは地図の上に重ねる)
            UiKit.Anchor(view, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, viewBottom), new Vector2(0f, -viewTop));
            float viewW = mcs.x, viewH = mcs.y - viewTop - viewBottom;
            var viewImg = view.gameObject.AddComponent<Image>();
            viewImg.color = PaperFx.Ground;
            view.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.NewRect("content", view);
            int rows = run.Map.Count;
            float contentH = Mathf.Max(viewH, PadY * 2f + (rows + 1) * RowH);   // +1 = 出発の行
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.sizeDelta = new Vector2(0f, contentH);
            content.anchoredPosition = Vector2.zero;
            var scroll = view.gameObject.AddComponent<MapScrollRect>();   // ホイールは滑らかに・位置は画素に丸める (2026-10-28)
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            if (Dbg("canvas"))
            {
                // 計測: content を入れ子の Canvas にする (スクロールで動くのは Canvas 1つ = 上の Canvas の束ね直しを起こさない)。当たり判定は Canvas ごとに要る
                content.gameObject.AddComponent<Canvas>();
                content.gameObject.AddComponent<GraphicRaycaster>();
            }
            var life = Dbg("nolife") ? null : content.gameObject.AddComponent<MapLife>();

            // 地面 (夜の草のタイル。8×8 枚の 512 ドットの塊を 1ドット=2px で繰り返す)
            {
                var gr = new GameObject("ground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                gr.transform.SetParent(content, false);
                gr.raycastTarget = false;
                UiKit.Stretch(gr.rectTransform, 0f, 0f, 0f, 0f);
                var gt = MapForest.Ground(run.Act);
                if (gt != null) { gr.texture = gt; gr.uvRect = new Rect(0f, 0f, viewW / (gt.width * 2f), contentH / (gt.height * 2f)); gr.color = Color.white; }
                else gr.color = PaperFx.Window;
            }

            var choices = DeckRogue.Engine.Run.NextChoices(run);
            var choiceSet = new HashSet<int>();
            for (int i = 0; i < choices.Count; i++) choiceSet.Add(choices[i]);
            // 翼の靴 (2026-09-12 本家 Wing Boots): 残回数があれば線の無い次の行のノードも選べる (線は引かない = ノードだけ押せる)
            var wings = DeckRogue.Engine.Run.WingChoices(run);
            for (int i = 0; i < wings.Count; i++) choiceSet.Add(wings[i]);
            int nextRow = run.Row + 1;

            // 通ってきた道 (現在地から親を逆に辿る。複数の親があれば列が近い方)
            var path = new HashSet<long>();
            if (run.Row >= 0)
            {
                int r = run.Row, c = run.Col;
                path.Add(Key(r, c));
                while (r > 0)
                {
                    int best = -1; int bestDist = 999;
                    var prev = run.Map[r - 1];
                    for (int i = 0; i < prev.Count; i++)
                    {
                        var n = prev[i];
                        bool links = false;
                        for (int k = 0; k < n.Next.Count; k++) if (n.Next[k] == c) links = true;
                        if (!links) continue;
                        int d = Math.Abs((n.Col ?? 0) - (run.Map[r][c].Col ?? 0));
                        if (d < bestDist) { bestDist = d; best = i; }
                    }
                    if (best < 0) break;
                    r--; c = best;
                    path.Add(Key(r, c));
                }
            }

            // 踏み跡 (カンバスの案1 と同じ: 暗い縁 38 の上に、土に夜の色を焼いた帯 26 を まだ先 → 通らなかった → 通った → 進める の順に重ね、
            // 通った道は真鍮の足跡、進める道は光る破線。帯の端は丸い = 節でなめらかにつながる。2026-10-28 ユーザー「道が醜すぎる」)
            var trailBase = Trail(content, "trail-base", null);
            var trailRim = Trail(content, "trail-rim", null);   // 道の輪郭 (月明かりの縁。2026-10-28 ユーザー「道の輪郭をもっと見やすく」)。全部の縁を先に描き、帯は後 = 節で縁が帯の上を横切らない
            var trailFuture = Trail(content, "trail-future", MapForest.DirtState(run.Act, 1), 64f);
            var trailPassed = Trail(content, "trail-passed", MapForest.DirtState(run.Act, 2), 64f);
            var trailTaken = Trail(content, "trail-taken", MapForest.DirtState(run.Act, 0), 64f);
            var trailTop = Trail(content, "trail-top", null);
            trailTop.RoundCaps = false;
            trailRim.RoundCaps = false;   // 縁の端は節の中心 (帯の丸い端と印の下に隠れる)。丸めると節に白い輪が出る
            var edges = new List<Vector2[]>();
            for (int r = 0; r < rows - 1; r++)
            {
                var row = run.Map[r];
                for (int i = 0; i < row.Count; i++)
                {
                    var n = row[i];
                    for (int k = 0; k < n.Next.Count; k++)
                    {
                        int j = n.Next[k];
                        if (j < 0 || j >= run.Map[r + 1].Count) continue;
                        bool fromCur = r == run.Row && i == run.Col;
                        bool avail = fromCur && choiceSet.Contains(j);
                        bool taken = path.Contains(Key(r, i)) && path.Contains(Key(r + 1, j));
                        bool passed = !taken && !avail && r <= run.Row;
                        Vector2 a = NodePos(n, r), b = NodePos(run.Map[r + 1][j], r + 1);
                        edges.Add(new[] { a, b });
                        var pts = Curve(a, b, r + (n.Col ?? 0));
                        trailBase.Add(pts, 42f, new Color(0.024f, 0.031f, 0.063f, MapForest.LinearAlpha(0.62f)));   // 外の影 (カンバスの rgba(6,8,16,.55) より少し濃く広く)
                        trailRim.Add(pts, 31f, new Color(PaperFx.PaperDim.r, PaperFx.PaperDim.g, PaperFx.PaperDim.b, taken || avail ? 0.72f : passed ? 0.26f : 0.46f));   // 縁 (帯 26 の外に 2.5px)
                        var band = taken || avail ? trailTaken : passed ? trailPassed : trailFuture;
                        band.Add(pts, 26f, band.Tex != null ? Color.white : new Color(0.27f, 0.22f, 0.25f, 1f));
                        if (taken) { var tc = TakenLine; trailTop.Add(pts, 6f, new Color(tc.r, tc.g, tc.b, 0.95f), 2f, 15f); }   // 足跡 (点)
                        if (avail)
                        {
                            var ac = AvailLine;
                            trailTop.Add(pts, 14f, new Color(ac.r, ac.g, ac.b, 0.22f));   // 破線のにじみ (カンバスの glow)
                            trailTop.Add(pts, 6f, new Color(ac.r, ac.g, ac.b, 0.97f), 12f, 12f);
                        }
                    }
                }
            }

            // 森 (両脇の木・格子の空いた所の茂みや切り株・出発の道標と灯)
            var rnd = new System.Random(977 * run.Act + rows * 31 + (run.Map.Count > 0 ? run.Map[0].Count : 0));
            if (!Dbg("noforest")) Forest(content, run, viewW, contentH, rnd, edges);
            Vector2 startPos = new Vector2(_ox + MapW / 2f, PadY - 10f);   // 出発 (駒の足元。行0 の空き地と重ならない)
            {
                var lamp = UiKit.Pan(content, new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.34f), "start-lamp");
                lamp.sprite = MapForest.Radial(); lamp.raycastTarget = false;
                Place(lamp.rectTransform, startPos + new Vector2(0f, 20f), new Vector2(170f, 170f), new Vector2(0.5f, 0.5f));
                var stone = MapForest.Graded(MapForest.Folder(1) + "/relief/act1_waystone", 0.7f, 10f);
                if (stone != null) { var st = Pic(content, "start-stone", stone, 0.62f); Place(st.rectTransform, startPos + new Vector2(0f, -40f), new Vector2(stone.rect.width, stone.rect.height), new Vector2(0.5f, 0f)); }
            }

            // ノード (名札は駒より上の層 labels に置く = 駒の頭に隠れない)
            var labels = UiKit.NewRect("labels", content);
            labels.anchorMin = Vector2.zero; labels.anchorMax = Vector2.one; labels.pivot = Vector2.zero;
            labels.offsetMin = Vector2.zero; labels.offsetMax = Vector2.zero;
            for (int r = 0; r < rows; r++)
            {
                var row = run.Map[r];
                for (int i = 0; i < row.Count; i++)
                {
                    var n = row[i];
                    bool cur = r == run.Row && i == run.Col;
                    bool avail = r == nextRow && choiceSet.Contains(i);
                    bool taken = path.Contains(Key(r, i));
                    bool passed = r <= run.Row;
                    Node(g, content, labels, n, r, i, cur, avail, taken, passed, !overlay, recipeCount, life);
                }
            }

            // 落書きの層 (ノードの上。左クリックは素通し = ICanvasRaycastFilter)
            var doodleRt = UiKit.NewRect("doodle", content);
            doodleRt.anchorMin = new Vector2(0f, 0f); doodleRt.anchorMax = new Vector2(1f, 1f); doodleRt.pivot = new Vector2(0f, 0f);
            doodleRt.offsetMin = Vector2.zero; doodleRt.offsetMax = Vector2.zero;
            doodleRt.gameObject.AddComponent<CanvasRenderer>();   // Graphic の前提 (エディタは自動で足さない)
            var doodle = doodleRt.gameObject.AddComponent<DoodleLayer>();
            doodle.Root = g; doodle.Strokes = g.DoodlesFor(run.Act); doodle.Editable = !overlay; doodle.raycastTarget = !overlay;

            labels.SetAsLastSibling();     // 名札は道と節の上
            doodleRt.SetAsLastSibling();   // 落書きはいちばん上 (右ドラッグ/ペンモードの時だけ入力を受ける)

            // 蛍 (森の上をゆっくり漂って明滅)
            for (int i = 0; i < 14; i++)
            {
                var f = UiKit.Pan(content, new Color(0.95f, 0.91f, 0.66f, 0.8f), "firefly");
                f.sprite = MapForest.Radial(); f.raycastTarget = false;
                float fx = _ox - 120f + (float)rnd.NextDouble() * (MapW + 240f), fy = 60f + (float)rnd.NextDouble() * (contentH - 120f);
                Place(f.rectTransform, new Vector2(fx, fy), new Vector2(14f, 14f), new Vector2(0.5f, 0.5f));
                if (life != null) life.Flies.Add(f.rectTransform);
            }

            // 窓に貼る空気 (地図と一緒には動かない): 夜の青・月明かり・月光の筋・上の霧・周辺減光・手前の暗い木・上下の暗がり
            Atmosphere(view, viewW, viewH, overlay);

            // 現在行が下から 38% に来るようにスクロール
            float curY = run.Row >= 0 ? NodePos(run.Map[run.Row][run.Col], run.Row).y : startPos.y;
            float target = curY - viewH * 0.38f;
            float maxScroll = Math.Max(0f, contentH - viewH);
            float norm = maxScroll > 0f ? Mathf.Clamp01(target / maxScroll) : 0f;
            scroll.verticalNormalizedPosition = norm;
            var fixer = view.gameObject.AddComponent<ScrollFix>();
            fixer.Scroll = scroll; fixer.Norm = norm;
            if (AutoScroll) { var asc = view.gameObject.AddComponent<MapAutoScroll>(); asc.Scroll = scroll; asc.Wheel = AutoScrollWheel; }

            if (overlay)
            {
                // 見出しと閉じるだけ (進路は選べない)。上部バーは下の画面のものが透けて見える
                var head = UiKit.Deco(root, "マップ — " + ActName(run.Act), 26, UiKit.ColText, TextAnchor.MiddleCenter);
                head.outlineWidth = 0.2f; head.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -RunUi.TopH - 44f), new Vector2(0f, -RunUi.TopH - 4f));
                RunUi.BottomButton(root, "閉じる", delegate { g.ViewMap = false; g.Rebuild(); }, 18, 220f, 50f, 0f, 28f);
                return;
            }
            RunUi.TopBar(g, root, ActName(run.Act));
            RunUi.Message(g, root);

            // 左下: 案内
            var hint = UiKit.Txt(root, choices.Count > 0 ? (UiKit.Phone ? "光る道へ進めます" : "光る道へ進めます ・ 節に触れると中身") : "進めるノードがありません", 15, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Anchor(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0.35f, 0f), new Vector2(26f, 70f), new Vector2(0f, 100f));
            if (!UiKit.Phone) Legend(root);   // スマホは凡例が左右の森に重なるので出さない (節は印 2 倍で読める)

            // セーブ (2026-09-15 本家形): 自動保存なので「セーブする」は無い。「セーブして終了」と「ランを放棄」(確認つき)
            var quit = UiKit.Btn(root, "セーブして終了", delegate { g.SaveAndQuit(); }, 14);
            var qle = quit.GetComponent<LayoutElement>();
            if (qle != null) UnityEngine.Object.Destroy(qle);
            UiKit.Anchor(quit.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(190f, 56f));
            Tooltip.Attach(quit.gameObject, delegate { return "タイトルへ戻る。ランは自動で保存されていて、「続きから」で再開できる"; });
            var abandon = UiKit.Btn(root, "ランを放棄", delegate { g.AskAbandonRun(); }, 14, true, PaperFx.DangerBtn);
            var ale = abandon.GetComponent<LayoutElement>();
            if (ale != null) UnityEngine.Object.Destroy(ale);
            UiKit.Anchor(abandon.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(200f, 20f), new Vector2(330f, 56f));

            // 右下: 落書きの道具 (2026-09-12 StS2 の移植。右ドラッグで描く／ペンを押すと指でも描ける)
            DoodleToolbar(g, root);

            // 下の真ん中: 幕の進み
            var prog = UiKit.Txt(root, "幕 " + run.Act + " / 3   勝利 " + run.BattlesWon + " 戦", 15, UiKit.ColDim, TextAnchor.MiddleCenter);
            UiKit.Anchor(prog.rectTransform, new Vector2(0.4f, 0f), new Vector2(0.6f, 0f), new Vector2(0f, 26f), new Vector2(0f, 52f));
        }

        /// <summary>ScrollRect は初期化後にレイアウトが走ると位置が戻るので、1フレーム後に指定位置へ戻す</summary>
        class ScrollFix : MonoBehaviour
        {
            public ScrollRect Scroll; public float Norm; int _n;
            void LateUpdate()
            {
                if (Scroll == null) { Destroy(this); return; }
                Scroll.verticalNormalizedPosition = Norm;
                if (++_n >= 2) Destroy(this);
            }
        }

        /// <summary>
        /// 地図の ScrollRect (2026-10-28 ユーザー「スクロールがカクつく」。計測ではスクロール中も 1 フレーム 4.2ms = 処理落ちではなく、ホイールの 1 刻みで 40px を即座に飛ぶ動きが原因)。
        /// ホイールは目標へ時定数 0.08 秒で滑らかに寄せる (1 刻み = WheelStep。刻みを重ねれば目標が先へ進む)。指・マウスのドラッグと慣性は ScrollRect のまま (ドラッグを始めたらホイールの目標は捨てる)。
        /// 位置は毎フレーム画面の画素に丸める (ドット絵の地面・木・印が小数の位置で太さを変えない)
        /// </summary>
        public class MapScrollRect : ScrollRect
        {
            public float WheelStep = 150f;
            float _target = float.NaN;   // ホイールの目標 (content.anchoredPosition.y)。NaN = ホイールで動いていない
            float _pos;                  // 寄せている途中の位置 (丸める前。丸めた値から寄せると残り 2px で止まる)
            Canvas _root;

            public override void OnScroll(PointerEventData data)
            {
                if (!IsActive() || content == null || viewport == null) return;
                float d = data.scrollDelta.y;
                if (Mathf.Abs(data.scrollDelta.x) > Mathf.Abs(d)) d = data.scrollDelta.x;   // ScrollRect と同じ (縦だけの時は横の刻みも縦に)
                if (float.IsNaN(_target)) _pos = content.anchoredPosition.y;
                float cur = float.IsNaN(_target) ? _pos : _target;
                float max = Mathf.Max(0f, content.rect.height - viewport.rect.height);   // content は下端を窓の下端に合わせて置く (pivot 0,0 側) = y は -max〜0
                _target = Mathf.Clamp(cur - d * WheelStep, -max, 0f);
                StopMovement();   // 慣性と混ぜない
            }

            public override void OnBeginDrag(PointerEventData eventData) { _target = float.NaN; base.OnBeginDrag(eventData); }

            protected override void LateUpdate()
            {
                base.LateUpdate();   // ドラッグ・慣性
                if (content == null) return;
                var p = content.anchoredPosition;
                if (!float.IsNaN(_target))
                {
                    _pos = Mathf.Lerp(_pos, _target, 1f - Mathf.Exp(-Time.unscaledDeltaTime / 0.08f));
                    if (Mathf.Abs(_pos - _target) < 0.5f) { _pos = _target; _target = float.NaN; }
                    p.y = _pos;
                }
                if (_root == null) { var cv = GetComponentInParent<Canvas>(); _root = cv != null ? cv.rootCanvas : null; }
                float s = _root != null && _root.scaleFactor > 0f ? _root.scaleFactor : 1f;   // キャンバスの1単位 = 画面の s px
                p.y = Mathf.Round(p.y * s) / s;
                if (p != content.anchoredPosition) SetContentAnchoredPosition(p);
            }
        }

        /// <summary>計測用 (mapscroll=1): 地図を上下に往復させ続ける (4 秒で1往復。実時間)。Wheel (mapscroll=wheel) はホイールの刻みを 10・40・70 フレーム目に送り、位置を毎フレーム記録する</summary>
        class MapAutoScroll : MonoBehaviour
        {
            public ScrollRect Scroll; public bool Wheel; float _t0 = -1f; int _f;
            void LateUpdate()
            {
                if (Scroll == null) return;
                if (Wheel)
                {
                    _f++;
                    if (_f == 10 || _f == 40 || _f == 70) Scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0f, -1f) });
                    if (_f >= 8 && _f <= 110 && Scroll.content != null) Debug.Log("[MapWheel] f=" + _f + " y=" + Scroll.content.anchoredPosition.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }
                if (_t0 < 0f) _t0 = Time.unscaledTime;
                float t = Time.unscaledTime - _t0;
                Scroll.verticalNormalizedPosition = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 0.5f);
            }
        }

        static long Key(int r, int c) { return ((long)r << 16) | (uint)c; }

        static Vector2 GridPos(int col, int row)
        {
            float colW = (MapW - 120f) / (Cols - 1);
            // 格子に揺らぎ (手で置いたように。行と列から決まる = 毎回同じ。カンバスの案1 と同じ式)
            float jx = (((row * 7 + col * 13) % 5) - 2) * 9f, jy = (((row * 11 + col * 5) % 5) - 2) * 7f;
            return new Vector2(_ox + 60f + col * colW + jx, PadY + RowH * (row + 1) + RowH * 0.5f + jy);
        }

        static Vector2 NodePos(MapNode n, int row) { return GridPos(n.Col ?? 0, row); }

        /// <summary>節と節の間のゆるい曲線 (二次ベジェを 14 分割。曲がりは行と列で決まる)</summary>
        static List<Vector2> Curve(Vector2 a, Vector2 b, int seed)
        {
            Vector2 m = (a + b) * 0.5f, d = b - a;
            float L = Mathf.Max(0.001f, d.magnitude);
            float k = ((seed % 3) - 1) * 22f;
            Vector2 c = m + new Vector2(-d.y / L, d.x / L) * k;
            var pts = new List<Vector2>(15);
            for (int i = 0; i <= 14; i++)
            {
                float t = i / 14f, u = 1f - t;
                pts.Add(u * u * a + 2f * u * t * c + t * t * b);
            }
            return pts;
        }

        static MapTrail Trail(RectTransform content, string name, Texture tex, float texPx = 128f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(content, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = Vector2.zero;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<MapTrail>();
            t.Tex = tex; t.TexPx = texPx * 2f; t.raycastTarget = false;
            return t;
        }

        static Image Pic(Transform parent, string name, Sprite sp, float bright)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.sprite = sp; img.raycastTarget = false;
            img.color = new Color(bright, bright, Mathf.Min(1f, bright * 1.05f), 1f);
            return img;
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
            return (a + ab * t - p).magnitude;
        }

        /// <summary>両脇の木 (1ドット=1px・下ほど手前) と、格子の空いた所の小物 (道から 46px 以上離す)</summary>
        static void Forest(RectTransform content, RunState run, float viewW, float contentH, System.Random rnd, List<Vector2[]> edges)
        {
            string relief = MapForest.Folder(1) + "/relief/";
            if (run.Act <= 1)
            {
                string[] kinds = { "conifer_dense_1", "act1_tree_pine", "conifer_w32_1", "act1_tree_oak", "act1_tree_pine", "conifer_dense_2", "act1_tree_giant", "conifer_w32_2", "act1_tree_pine", "act1_tree_oak2" };
                float xL = _ox + 60f - 90f, xR = _ox + MapW - 60f + 90f;
                var trees = new List<KeyValuePair<float, Action>>();
                int i = 0;
                for (float y = contentH + 40f; y > -60f; y -= 64f, i++)
                {
                    for (int side = 0; side < 2; side++)
                    {
                        var sp = MapForest.Graded(relief + kinds[(i + side * 3) % kinds.Length], 0.62f, 12f);
                        if (sp == null) continue;
                        float w = sp.rect.width, h = sp.rect.height;
                        int col = (i * 3 + side + rnd.Next(2)) % 4;
                        float x = side == 0 ? xL - w * 0.5f - col * 105f - rnd.Next(30) : xR + w * 0.5f + col * 105f + rnd.Next(30);
                        if (side == 0 && x + w * 0.5f < -20f) continue;
                        if (side == 1 && x - w * 0.5f > viewW + 20f) continue;
                        float br = 0.62f + (float)rnd.NextDouble() * 0.24f;
                        float yy = y + rnd.Next(24) - 12f;
                        var spc = sp;
                        trees.Add(new KeyValuePair<float, Action>(yy, delegate
                        {
                            var img = Pic(content, "tree", spc, br);
                            Place(img.rectTransform, new Vector2(x, yy), new Vector2(w, h), new Vector2(0.5f, 0f));
                        }));
                    }
                }
                trees.Sort(delegate (KeyValuePair<float, Action> a, KeyValuePair<float, Action> b) { return b.Key.CompareTo(a.Key); });   // 上 (奥) から置く = 下の木が手前
                foreach (var t in trees) t.Value();
            }

            // 格子の空いた所に小物 (幕1 は茂み・切り株・羊歯。幕2/3 は今は置かない)
            if (run.Act > 1) return;
            string[] small = { "bush_clump_m_1", "act1_stump", "act1_fern", "bush_clump_s_1", "act1_fern2", "bush_clump_m_2" };
            Vector2 bossAt = run.Map.Count > 0 && run.Map[run.Map.Count - 1].Count > 0 ? NodePos(run.Map[run.Map.Count - 1][0], run.Map.Count - 1) : new Vector2(-9999f, -9999f);
            for (int r = 0; r < run.Map.Count - 2; r++)
            {
                var used = new HashSet<int>();
                foreach (var n in run.Map[r]) used.Add(n.Col ?? 0);
                for (int c = 0; c < Cols; c++)
                {
                    if (used.Contains(c) || rnd.NextDouble() > 0.45) continue;
                    Vector2 p = GridPos(c, r) + new Vector2(rnd.Next(30) - 15f, -14f);
                    bool clear = (p - bossAt).magnitude > 200f;
                    foreach (var e in edges) if (SegDist(p + new Vector2(0f, 18f), e[0], e[1]) < 46f) { clear = false; break; }
                    if (!clear) continue;
                    var sp = MapForest.Graded(relief + small[rnd.Next(small.Length)], 0.7f, 10f);
                    if (sp == null) continue;
                    var img = Pic(content, "prop", sp, 0.5f + (float)rnd.NextDouble() * 0.1f);
                    Place(img.rectTransform, p, new Vector2(sp.rect.width, sp.rect.height), new Vector2(0.5f, 0f));
                }
            }
        }

        /// <summary>窓に貼る空気 (地図と一緒に動かない)。色はカンバスの案1 の値 (multiply/screen は α の重ねで近似)</summary>
        static void Atmosphere(RectTransform view, float viewW, float viewH, bool overlay)
        {
            Image Layer(string name, Sprite sp, Color c)
            {
                var img = UiKit.Pan(view, c, name);
                img.sprite = sp; img.raycastTarget = false;
                return img;
            }
            // 夜の青 (上ほど青く・下は暗く)
            var gTop = Layer("grade-top", MapForest.VFade(), new Color(0.16f, 0.20f, 0.38f, 0.08f));
            UiKit.Anchor(gTop.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var gBot = Layer("grade-bottom", MapForest.VFade(), new Color(0.04f, 0.05f, 0.12f, 0f));
            UiKit.Anchor(gBot.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.4f), Vector2.zero, Vector2.zero);
            gBot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            // 月明かり (左上の手前から)
            var moon = Layer("moon", MapForest.Radial(), new Color(0.69f, 0.78f, 0.93f, 0.22f));
            moon.rectTransform.anchorMin = moon.rectTransform.anchorMax = new Vector2(0.16f, 1f);
            moon.rectTransform.sizeDelta = new Vector2(2600f, 1700f);
            moon.rectTransform.anchoredPosition = new Vector2(0f, -60f);
            // 月光の筋 2 本
            for (int i = 0; i < 2; i++)
            {
                var sh = Layer("moon-shaft", MapForest.HBand(), new Color(0.77f, 0.84f, 0.96f, i == 0 ? 0.07f : 0.05f));
                sh.rectTransform.anchorMin = sh.rectTransform.anchorMax = new Vector2(0f, 1f);
                sh.rectTransform.sizeDelta = new Vector2(150f, 1600f);
                sh.rectTransform.anchoredPosition = new Vector2(viewW * (0.22f + 0.15f * i), -viewH * 0.45f);
                sh.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            }
            // 上の霧 (遠い行ほど霞む)
            var fog = Layer("fog", MapForest.VFade(), new Color(0.52f, 0.59f, 0.75f, 0.30f));
            fog.rectTransform.anchorMin = new Vector2(0f, 1f); fog.rectTransform.anchorMax = new Vector2(1f, 1f);
            fog.rectTransform.pivot = new Vector2(0.5f, 1f);
            fog.rectTransform.sizeDelta = new Vector2(0f, Mathf.Min(560f, viewH * (UiKit.Phone ? 0.36f : 0.55f)));
            if (UiKit.Phone) fog.color = new Color(fog.color.r, fog.color.g, fog.color.b, 0.3f);   // スマホは見える行が 3〜4 なので薄く短く
            fog.rectTransform.anchoredPosition = Vector2.zero;
            // 周辺減光
            var vig = Layer("vignette", MapForest.Vignette(), new Color(0.02f, 0.027f, 0.063f, 0.6f));
            UiKit.Stretch(vig.rectTransform, 0f, 0f, 0f, 0f);
            // 手前の暗い木 (画面の下の角。額縁)
            if (!overlay)
            {
                string relief = MapForest.Folder(1) + "/relief/";
                var l = MapForest.Graded(relief + "conifer_dense_1", 0.5f, 14f);
                if (l != null) { var img = Pic(view, "frame-left", l, 0.28f); img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero; img.rectTransform.pivot = new Vector2(0.5f, 0f); img.rectTransform.sizeDelta = new Vector2(l.rect.width * 2f, l.rect.height * 2f); img.rectTransform.anchoredPosition = new Vector2(40f, -150f); }
                var rr = MapForest.Graded(relief + "conifer_w32_1", 0.5f, 14f);
                if (rr != null) { var img = Pic(view, "frame-right", rr, 0.26f); img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(1f, 0f); img.rectTransform.pivot = new Vector2(0.5f, 0f); img.rectTransform.sizeDelta = new Vector2(rr.rect.width * 2f, rr.rect.height * 2f); img.rectTransform.anchoredPosition = new Vector2(-50f, -190f); }
                // 上部バーと下のボタンの下の暗がり (字が森に溶けない)
                var top = Layer("band-top", MapForest.VFade(), new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.88f));
                top.rectTransform.anchorMin = new Vector2(0f, 1f); top.rectTransform.anchorMax = new Vector2(1f, 1f); top.rectTransform.pivot = new Vector2(0.5f, 1f);
                top.rectTransform.sizeDelta = new Vector2(0f, RunUi.TopH + 60f); top.rectTransform.anchoredPosition = Vector2.zero;
                var bot = Layer("band-bottom", MapForest.VFade(), new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.88f));
                bot.rectTransform.anchorMin = new Vector2(0f, 0f); bot.rectTransform.anchorMax = new Vector2(1f, 0f); bot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                bot.rectTransform.sizeDelta = new Vector2(0f, 150f); bot.rectTransform.anchoredPosition = new Vector2(0f, 75f);
                bot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            }
        }

        static void Node(GameRoot g, RectTransform parent, RectTransform labels, MapNode n, int row, int index, bool cur, bool avail, bool taken, bool passed, bool interactive, int recipeCount, MapLife life)
        {
            bool boss = n.Type == MapNodeTypes.Boss;
            Vector2 at = NodePos(n, row);
            var cell = UiKit.NewRect("node-" + row + "-" + index, parent);
            Place(cell, at, new Vector2(boss ? 200f : 124f, boss ? 170f : 124f), new Vector2(0.5f, 0.5f));
            float asz = boss ? 96f : 64f;

            Image Soft(string name, Sprite sp, Color c, Vector2 pos, Vector2 size)
            {
                var img = UiKit.Pan(cell, c, name);
                img.sprite = sp; img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                img.rectTransform.sizeDelta = size; img.rectTransform.anchoredPosition = pos;
                return img;
            }

            // 光 (進める節は真鍮・ボスは薔薇)
            if (avail) Soft("halo", MapForest.Radial(), new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.30f), new Vector2(0f, 30f), new Vector2(230f, 230f));
            if (boss) Soft("boss-glow", MapForest.Radial(), new Color(PaperFx.Rose.r, PaperFx.Rose.g, PaperFx.Rose.b, 0.30f), new Vector2(0f, 40f), new Vector2(260f, 240f));
            // 空き地 (土の明るみ) と影
            Soft("clearing", MapForest.Radial(), new Color(0.41f, 0.35f, 0.31f, passed && !taken && !cur ? 0.32f : 0.62f), Vector2.zero, boss ? new Vector2(190f, 64f) : new Vector2(124f, 48f));
            Image ring = null;
            if (avail) ring = Soft("ring", MapForest.Ring(), new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.85f), new Vector2(0f, 0f), new Vector2(114f, 44f));
            if (cur)
            {
                // 現在地: 真鍮の輪を二重にして光らせる (キャラは置かない)
                Soft("cur-glow", MapForest.Radial(), new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.32f), new Vector2(0f, 24f), new Vector2(190f, 170f));
                Soft("ring-cur", MapForest.Ring(), PaperFx.Brass, Vector2.zero, new Vector2(126f, 48f));
                Soft("ring-cur2", MapForest.Ring(), new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.7f), Vector2.zero, new Vector2(110f, 40f));
            }
            else if (taken) Soft("ring-taken", MapForest.Ring(), new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.5f), Vector2.zero, new Vector2(118f, 44f));
            Soft("shadow", ThemeFx.Shadow(), Color.white, new Vector2(0f, -1f), new Vector2(boss ? 80f : 54f, boss ? 20f : 15f));

            // 印 (32 ドットを 2 倍。足元が空き地の中心の 4px 下。進める節は少し浮く)
            Image icon = null;
            {
                var nodeArt = Theme.Art("map", "node_" + n.Type);
                icon = UiKit.Icon(cell, NodeIcon(n.Type), boss ? 56f : 34f);
                if (nodeArt != null) { icon.sprite = nodeArt; icon.color = Color.white; }
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0f);
                icon.rectTransform.sizeDelta = new Vector2(asz, asz); UiKit.Le(icon, asz, asz, asz, asz);
                icon.rectTransform.anchoredPosition = new Vector2(0f, avail ? 8f : -4f);
                icon.color = avail || boss || cur ? Color.white : taken ? new Color(0.64f, 0.62f, 0.66f, 1f) : passed ? new Color(0.36f, 0.36f, 0.41f, 1f) : new Color(0.86f, 0.86f, 0.9f, 1f);
                if (avail && life != null) { life.Bobs.Add(icon.rectTransform); if (ring != null) life.Pulses.Add(ring); }
            }

            // ボスの名札 (カンバスの案1: 印の右に名前と「幕ボス ・ あと N 行」)
            if (boss)
            {
                string bname = "幕ボス";
                if (n.EncounterId != null) { try { bname = Content.EncounterName(n.EncounterId); } catch (Exception) { } }
                int left = Math.Max(0, row - g.Rs.Row);
                var bt = UiKit.Txt(labels, bname, 24, PaperFx.RoseLight, TextAnchor.MiddleLeft, true);
                bt.outlineWidth = 0.22f; bt.outlineColor = new Color(0f, 0f, 0f, 0.85f); bt.raycastTarget = false;
                Place(bt.rectTransform, at + new Vector2(62f, 66f), new Vector2(360f, 32f), new Vector2(0f, 0.5f));
                var bs = UiKit.Txt(labels, "幕ボス ・ あと " + left + " 行", 14, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                bs.outlineWidth = 0.2f; bs.outlineColor = new Color(0f, 0f, 0f, 0.85f); bs.raycastTarget = false;
                Place(bs.rectTransform, at + new Vector2(62f, 40f), new Vector2(360f, 22f), new Vector2(0f, 0.5f));
            }
            // 名札 (進める節)
            if (avail && !boss)
            {
                var tag = BattleScreen.Tag(labels, 30f, 0f);
                tag.anchorMin = tag.anchorMax = Vector2.zero;
                tag.pivot = new Vector2(0.5f, 1f);
                tag.sizeDelta = new Vector2(80f, 30f);
                tag.anchoredPosition = at + new Vector2(0f, -30f);
                var hg = tag.GetComponent<HorizontalLayoutGroup>(); if (hg != null) hg.childAlignment = TextAnchor.MiddleCenter;
                var t = UiKit.Txt(tag, NodeName(n.Type), 16, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Le(t, -1f, 26f, -1f, 26f);
            }

            string tip = NodeTip(n);
            // ⭐ 工房: いま手元でレシピが成立する対があれば印の右上に星 (2026-09-12「レシピは誰も踏まなかった」への提示)
            if (n.Type == MapNodeTypes.Workshop && recipeCount > 0 && !passed)
            {
                var star = UiKit.Icon(cell, "star", 32f);
                star.raycastTarget = false;
                star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                star.rectTransform.anchoredPosition = new Vector2(34f, 62f);
                int price = 0;
                try { price = DeckRogue.Engine.Run.WorkshopFusePrice(g.Rs); } catch (Exception) { }
                tip += "\n★ 今の手札でレシピ（手書きの一品）が " + recipeCount + " 組 作れる" + (g.Rs.Gold < price ? "（所持金が " + price + "G に足りない）" : "（" + price + "G）");
            }

            // 当たり (透明。印と空き地を覆う・指の的は 110 以上)
            var hit = UiKit.Pan(cell, new Color(0f, 0f, 0f, 0f), "hit");
            hit.raycastTarget = true;
            hit.rectTransform.anchorMin = hit.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hit.rectTransform.sizeDelta = boss ? new Vector2(170f, 150f) : new Vector2(112f, 112f);
            hit.rectTransform.anchoredPosition = new Vector2(0f, boss ? 30f : 22f);
            Tooltip.Attach(hit.gameObject, delegate { return tip; });

            if (avail && interactive)
            {
                var btn = hit.gameObject.AddComponent<Button>();
                btn.targetGraphic = icon != null ? (Graphic)icon : hit;
                var cols = btn.colors; cols.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cols.pressedColor = new Color(0.85f, 0.85f, 0.85f); btn.colors = cols;
                int idx = index;
                btn.onClick.AddListener(delegate
                {
                    Audio.Ui("node");
                    g.Do(new RunCommand_ChooseNode { Col = idx });
                });
            }
        }

        /// <summary>凡例 (右の夜色の札・真鍮の細い縁。印は 32 ドットの等倍)</summary>
        static void Legend(RectTransform root)
        {
            var panel = UiKit.Pan(root, new Color(PaperFx.Window.r, PaperFx.Window.g, PaperFx.Window.b, 0.84f), "legend");
            panel.raycastTarget = false;
            var ol = panel.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.6f); ol.effectDistance = new Vector2(1f, -1f);
            UiKit.Anchor(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-210f, -190f), new Vector2(-28f, 200f));
            var vg = UiKit.Vert(panel.rectTransform, 4, 0);
            vg.padding = new RectOffset(16, 12, 12, 10);
            vg.childForceExpandHeight = false;
            var h = UiKit.Txt(panel.rectTransform, "凡例", 15, PaperFx.BrassLight, TextAnchor.MiddleLeft, true);
            h.characterSpacing = 6f;
            UiKit.Le(h, -1f, 24f, -1f, 24f);
            foreach (var type in new[] { MapNodeTypes.Battle, MapNodeTypes.Elite, MapNodeTypes.Event, MapNodeTypes.Campfire, MapNodeTypes.Shop, MapNodeTypes.Workshop, MapNodeTypes.Treasure, MapNodeTypes.Boss })
                LegendRow(panel.rectTransform, type);
        }

        /// <summary>落書きの道具: 紙色ペン・朱ペン・消しゴム・全消し。押したペンが光る (=ペンモード。同じものを押すと解除)</summary>
        static void DoodleToolbar(GameRoot g, RectTransform root)
        {
            var bar = UiKit.NewRect("doodle-tools", root);
            UiKit.Anchor(bar, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f - 4f * 78f - 3f * 8f, 62f), new Vector2(-24f, UiKit.Phone ? 110f : 100f));   // スマホはボタンが 48 (SetSize の丸め。2026-09-29)
            var hg = UiKit.Horz(bar, 8, 0);
            hg.childAlignment = TextAnchor.MiddleRight; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            string[] labels = { "紙ペン", "朱ペン", "消しゴム", "全消し" };   // ✎ はフォントに無い
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                bool active = g.DoodleMode && g.DoodlePen == idx;
                Color? bg = active ? (Color?)(idx == 1 ? PaperFx.RoseLight : idx == 2 ? PaperFx.Paper2 : PaperFx.MossLight) : null;
                var b = UiKit.Btn(bar, labels[i], delegate
                {
                    Audio.Ui("click");
                    if (idx == 3) { g.DoodlesFor(g.Rs.Act).Clear(); g.Rebuild(); return; }
                    if (g.DoodleMode && g.DoodlePen == idx) g.DoodleMode = false;   // 同じ道具をもう一度 = ペンモード解除
                    else { g.DoodleMode = true; g.DoodlePen = idx; }
                    g.Rebuild();
                }, 14, true, bg);
                BattleScreen.SetSize(b, idx == 2 || idx == 3 ? 82f : 70f, 36f);
            }
            var hint = UiKit.Txt(root, g.DoodleMode ? (g.DoodlePen == 2 ? "消しゴム: 線に触れると消える（もう一度押すと解除）" : "ペン: ドラッグで地図に描ける（もう一度押すと解除）") : (UiKit.Phone ? "ペンを押すと指で地図に描ける" : "右ドラッグで地図に描ける（ペンを押すと指でも）"), 13, UiKit.ColDim, TextAnchor.MiddleRight);
            UiKit.Anchor(hint.rectTransform, new Vector2(0.55f, 0f), new Vector2(1f, 0f), new Vector2(0f, UiKit.Phone ? 114f : 104f), new Vector2(-24f, UiKit.Phone ? 136f : 126f));
        }

        public static string NodeIcon(string type)
        {
            switch (type)
            {
                case MapNodeTypes.Battle: return "attack";
                case MapNodeTypes.Elite: return "skull";
                case MapNodeTypes.Boss: return "crown";
                case MapNodeTypes.Campfire: return "burn";
                case MapNodeTypes.Shop: return "gold";
                case MapNodeTypes.Workshop: return "hammer";
                case MapNodeTypes.Event: return "question";
                case MapNodeTypes.Treasure: return "chest";
                default: return "attack";
            }
        }

        public static string NodeName(string type)
        {
            switch (type)
            {
                case MapNodeTypes.Battle: return "戦闘";
                case MapNodeTypes.Elite: return "強個体";
                case MapNodeTypes.Boss: return "幕ボス";
                case MapNodeTypes.Campfire: return "焚き火";
                case MapNodeTypes.Shop: return "ショップ";
                case MapNodeTypes.Workshop: return "工房";
                case MapNodeTypes.Event: return "？";
                case MapNodeTypes.Treasure: return "宝箱";
                default: return type;
            }
        }

        static string NodeTip(MapNode n)
        {
            string enc = "";
            if (n.EncounterId != null)
            {
                try { enc = "\n" + Content.EncounterName(n.EncounterId); }
                catch (Exception) { enc = "\n" + n.EncounterId; }
            }
            switch (n.Type)
            {
                case MapNodeTypes.Battle: return "<b>戦闘</b>" + enc + "\n勝てばカード報酬とゴールド";
                case MapNodeTypes.Elite: return "<b>強個体</b>" + enc + "\n勝てばレリック3択・レア確定の報酬・ゴールド";
                case MapNodeTypes.Boss: return "<b>幕ボス</b>" + enc + "\n倒すと全回復・レリック3択・次の幕へ";
                case MapNodeTypes.Campfire: return "<b>焚き火</b>\n休む (最大HPの割合で回復) か、カードを1枚鍛える";
                case MapNodeTypes.Workshop: return "<b>工房</b>\n同じ色のカード2枚を1枚に溶かす (" + DeckRogue.Engine.Run.WORKSHOP_FUSE_PRICE + "G)";
                case MapNodeTypes.Shop: return "<b>ショップ</b>\nカード・レリック・カード除去・鍛える";
                case MapNodeTypes.Event: return "<b>？</b>\n入るまで中身は分からない (イベント・戦闘・店・宝箱)";
                case MapNodeTypes.Treasure: return "<b>宝箱</b>\nレリック3択";
                default: return n.Type;
            }
        }

        static void LegendRow(Transform parent, string type)
        {
            var row = UiKit.NewRect("legend-" + type, parent);
            UiKit.Le(row, -1f, 36f, -1f, 36f);
            var hg = UiKit.Horz(row, 10, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
            var ic = UiKit.Icon(row, NodeIcon(type), 22f);
            var art = Theme.Art("map", "node_" + type);
            if (art != null) { ic.sprite = art; ic.color = Color.white; }
            UiKit.Le(ic, 32f, 32f, 32f, 32f);
            var t = UiKit.Txt(row, NodeName(type), 16, PaperFx.Paper, TextAnchor.MiddleLeft);
            UiKit.Le(t, 110f, 26f, -1f, 26f);
        }
    }
}
