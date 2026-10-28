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
        const float RowH = 150f;       // 1行の高さ (磨き 案C 2026-10-29: 120 → 150 = 見える行 8 → 7・節の間に余白)
        const float PadTop = 255f;     // 上の余白 (丘の闇に坑口の門 384×256。門の下端の 12px 下が幕ボスの中心 = 門の前のレールの上)
        const float PadBottom = 200f;  // 下の余白 (出発点 = 坑匠の荷車と道標と灯。行 0 の 150px 下 = 行 0 が 7 列に広がっても道が寝ない。下の帯 100 の上)
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
            float contentH = Mathf.Max(viewH, PadBottom + (rows + 1) * RowH + PadTop);   // +1 = 出発の行
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
            // 月明かりの溜まり (左右に交互) と両脇の木陰 (磨き 案B 2026-10-28: 地面に中ほどの明暗)
            for (int k = 0; 320f + k * 520f < contentH - 200f; k++)
            {
                var pool = UiKit.Pan(content, new Color(0.74f, 0.80f, 0.95f, 0.20f), "pool");
                pool.sprite = MapForest.Radial(); pool.raycastTarget = false;
                UiKit.LinearShade(pool, 0.12f);   // 下は暗い地面
                Place(pool.rectTransform, new Vector2(_ox + (k % 2 == 0 ? MapW * 0.3f : MapW * 0.72f), 320f + k * 520f), new Vector2(600f, 380f), new Vector2(0.5f, 0.5f));
            }
            for (int side = 0; side < 2; side++)
            {
                var shade = UiKit.Pan(content, new Color(0.016f, 0.024f, 0.055f, 0.46f), "shade");
                shade.sprite = MapForest.Radial(); shade.raycastTarget = false;
                Place(shade.rectTransform, new Vector2(side == 0 ? _ox - 120f : _ox + MapW + 120f, contentH / 2f), new Vector2(560f, contentH + 200f), new Vector2(0.5f, 0.5f));
            }

            // 頂の丘の闇 (磨き 案C 2026-10-29: 上の余白に丘。坑口の門は森と小物の後に貼る = 道の端が門の足元に隠れる)
            {
                var hc = Color.Lerp(PaperFx.Ground, Color.black, 0.55f); hc.a = 0.86f;
                var hill = UiKit.Pan(content, hc, "hill");
                hill.sprite = MapForest.Hill(); hill.raycastTarget = false;
                hill.rectTransform.anchorMin = new Vector2(0f, 1f); hill.rectTransform.anchorMax = new Vector2(1f, 1f); hill.rectTransform.pivot = new Vector2(0.5f, 1f);
                hill.rectTransform.sizeDelta = new Vector2(0f, 400f); hill.rectTransform.anchoredPosition = Vector2.zero;
                var gd = UiKit.Pan(content, new Color(0f, 0f, 0f, 0.55f), "gate-dark");
                gd.sprite = MapForest.Radial(); gd.raycastTarget = false;
                Place(gd.rectTransform, new Vector2(_ox + MapW / 2f, contentH - 180f), new Vector2(720f, 360f), new Vector2(0.5f, 0.5f));
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
            Vector2 startPos = new Vector2(_ox + MapW / 2f, PadBottom + 75f);   // 出発 (荷車の足元。行 0 の 150px 下)
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
                        var pts = Curve(a, b, r * 5 + (n.Col ?? 0) * 3 + (run.Map[r + 1][j].Col ?? 0) * 7);
                        Road(trailBase, trailRim, trailTop, taken || avail ? trailTaken : passed ? trailPassed : trailFuture, pts, taken, avail, passed);
                    }
                }
            }
            // 出発点から行 0 への道 (磨き 案C 2026-10-29: 麓の荷車から森へ入る。まだ節を選んでいない時は進める道)
            for (int i = 0; i < run.Map[0].Count; i++)
            {
                Vector2 b = NodePos(run.Map[0][i], 0);
                bool taken = path.Contains(Key(0, i)), avail = run.Row < 0 && choiceSet.Contains(i);
                var pts = Curve(startPos, b, 3 + i * 7);
                edges.Add(new[] { startPos, b });
                Road(trailBase, trailRim, trailTop, taken || avail ? trailTaken : trailFuture, pts, taken, avail, false);   // 通らなかった出発の道は「まだ先」の薄さ (5〜6 本が扇に広がるので沈める)
            }

            // 森 (両脇の木・格子の空いた所の茂みや切り株・出発の道標と灯)
            var rnd = new System.Random(977 * run.Act + rows * 31 + (run.Map.Count > 0 ? run.Map[0].Count : 0));
            if (!Dbg("noforest")) Forest(content, run, viewW, contentH, rnd, edges);
            // 坑口の門 (磨き 案C 2026-10-29: PixelLab 192×128 を 2 倍・縁は丘に溶かしてある。灯の溜まりと足元の霧。森と小物の上・節の下)
            {
                var gsp = Theme.Art("map", "map_mine_gate");
                if (gsp != null)
                {
                    Vector2 gc = new Vector2(_ox + MapW / 2f, contentH - 190f);
                    var gate = Pic(content, "gate", gsp, 0.78f);
                    Place(gate.rectTransform, gc, new Vector2(gsp.rect.width * 2f, gsp.rect.height * 2f), new Vector2(0.5f, 0.5f));
                    var gl = UiKit.Pan(content, new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.18f), "gate-glow");
                    gl.sprite = MapForest.Radial(); gl.raycastTarget = false;
                    Place(gl.rectTransform, gc + new Vector2(0f, -10f), new Vector2(320f, 220f), new Vector2(0.5f, 0.5f));
                    var mist = UiKit.Pan(content, new Color(PaperFx.PaperDim.r, PaperFx.PaperDim.g, PaperFx.PaperDim.b, 0.22f), "gate-mist");
                    mist.sprite = MapForest.Radial(); mist.raycastTarget = false;
                    UiKit.LinearShade(mist, 0.12f);
                    Place(mist.rectTransform, gc + new Vector2(0f, -120f), new Vector2(800f, 170f), new Vector2(0.5f, 0.5f));
                }
            }
            // 出発点 (磨き 案C 2026-10-29: 坑匠の荷車 PixelLab 62×56 を 2 倍・道標・灯の溜まり・「森の入口」の札)
            {
                var lamp = UiKit.Pan(content, new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.34f), "start-lamp");
                lamp.sprite = MapForest.Radial(); lamp.raycastTarget = false;
                Place(lamp.rectTransform, startPos + new Vector2(10f, -20f), new Vector2(180f, 180f), new Vector2(0.5f, 0.5f));
                var cart = Theme.Art("map", "map_cart");
                if (cart != null) { var ci = Pic(content, "start-cart", cart, 0.82f); Place(ci.rectTransform, startPos + new Vector2(-52f, -12f), new Vector2(cart.rect.width * 2f, cart.rect.height * 2f), new Vector2(0.5f, 0.5f)); }
                var stone = MapForest.Graded(MapForest.Folder(1) + "/relief/act1_waystone", 0.7f, 10f);
                if (stone != null) { var st = Pic(content, "start-stone", stone, 0.62f); Place(st.rectTransform, startPos + new Vector2(70f, -17f), new Vector2(stone.rect.width * 1.5f, stone.rect.height * 1.5f), new Vector2(0.5f, 0f)); }
                var sl = UiKit.Txt(content, "森の入口", 18, PaperFx.Paper, TextAnchor.MiddleLeft, true);
                sl.characterSpacing = 3f; sl.outlineWidth = 0.22f; sl.outlineColor = new Color(0f, 0f, 0f, 0.85f); sl.raycastTarget = false;
                Place(sl.rectTransform, startPos + new Vector2(118f, -2f), new Vector2(260f, 26f), new Vector2(0f, 0.5f));
                var ss = UiKit.Txt(content, "出発点 ・ 坑匠の荷車", 13, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                ss.outlineWidth = 0.2f; ss.outlineColor = new Color(0f, 0f, 0f, 0.85f); ss.raycastTarget = false;
                Place(ss.rectTransform, startPos + new Vector2(118f, -24f), new Vector2(260f, 20f), new Vector2(0f, 0.5f));
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

            {   // 現在地のまわりに 3 匹 (磨き 案B: 灯に寄る)
                Vector2 curAt = run.Row >= 0 ? NodePos(run.Map[run.Row][run.Col], run.Row) : startPos;
                for (int i = 0; i < 3; i++)
                {
                    var f = UiKit.Pan(content, new Color(0.95f, 0.91f, 0.66f, 0.8f), "firefly");
                    f.sprite = MapForest.Radial(); f.raycastTarget = false;
                    Place(f.rectTransform, curAt + new Vector2(rnd.Next(160) - 80f, 20f + rnd.Next(110)), new Vector2(12f, 12f), new Vector2(0.5f, 0.5f));
                    if (life != null) life.Flies.Add(f.rectTransform);
                }
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
            RunUi.TopBar(g, root, UiKit.Phone ? ActName(run.Act) : null);   // PC は下の Header が見出しを描く (磨き 案B 2026-10-28)
            RunUi.Message(g, root);
            if (!UiKit.Phone) Header(root, run);

            // 左下: 案内 (磨き 案B 2026-10-28: セーブして終了・ランを放棄は ≡ の中だけに。幕の進みは見出しへ。下の帯は案内と道具だけ)
            var hint = UiKit.Txt(root, choices.Count > 0 ? (UiKit.Phone ? "光る節を押して進む" : "光る節を押して進む ・ 節に触れると中身") : "進める節がありません", 14, PaperFx.PaperDim, TextAnchor.MiddleLeft);
            hint.characterSpacing = 1f; hint.raycastTarget = false;
            UiKit.Anchor(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 0f), new Vector2(30f, 18f), new Vector2(0f, 40f));
            if (!UiKit.Phone) Legend(root);   // スマホは凡例が左右の森に重なるので出さない (節は印 2 倍で読める)
            DoodleToolbar(g, root);   // 右下の丸い帯 (2026-09-12 StS2 の移植)
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

        static Vector2 GridPos(int col, int row, bool jitter = true)
        {
            float colW = (MapW - 120f) / (Cols - 1);
            // 格子に揺らぎ (手で置いたように。行と列から決まる = 毎回同じ。磨き 案C 2026-10-29: x ±28・y ±20 = 格子に見せない。旧 ±18/±14)
            float jx = jitter ? (((row * 7 + col * 13) % 5) - 2) * 14f : 0f, jy = jitter ? (((row * 11 + col * 5) % 5) - 2) * 10f : 0f;
            return new Vector2(_ox + 60f + col * colW + jx, PadBottom + RowH * (row + 1) + RowH * 0.5f + jy);
        }

        static Vector2 NodePos(MapNode n, int row) { return GridPos(n.Col ?? 0, row, n.Type != MapNodeTypes.Boss); }   // 幕ボスは門の正面 (揺らさない)

        /// <summary>道 1 本 (磨き 案C 2026-10-29: 外の影 22〔まだ先は 18・薄く〕・月明かりの縁 17・帯 14・足跡 4・破線 4)</summary>
        static void Road(MapTrail trailBase, MapTrail trailRim, MapTrail trailTop, MapTrail band, List<Vector2> pts, bool taken, bool avail, bool passed)
        {
            if (taken || avail || passed) trailBase.Add(pts, 22f, new Color(0.024f, 0.031f, 0.063f, MapForest.LinearAlpha(0.42f)));   // 外の影 (カンバスの rgba(6,8,16,.42))
            else trailBase.Add(pts, 18f, new Color(0.024f, 0.031f, 0.063f, MapForest.LinearAlpha(0.20f)));   // まだ先の道は影を半分に (網に見えない)
            float rimA = taken || avail ? 0.22f : passed ? 0.06f : 0.10f;   // 縁 (帯の外に 1.5px)。カンバスの .26/.07/.12 を実機と並べて合わせた値
            trailRim.Add(pts, 17f, new Color(PaperFx.PaperDim.r, PaperFx.PaperDim.g, PaperFx.PaperDim.b, rimA));
            band.Add(pts, 14f, band.Tex != null ? Color.white : new Color(0.27f, 0.22f, 0.25f, 1f));
            if (taken) { var tc = TakenLine; trailTop.Add(pts, 4f, new Color(tc.r, tc.g, tc.b, 0.95f), 2f, 13f); }   // 足跡 (点)
            if (avail)
            {
                var ac = AvailLine;
                trailTop.Add(pts, 10f, new Color(ac.r, ac.g, ac.b, 0.22f));   // 破線のにじみ (カンバスの glow)
                trailTop.Add(pts, 4f, new Color(ac.r, ac.g, ac.b, 0.97f), 9f, 9f);
            }
        }

        /// <summary>節と節の間のゆるい曲線 (二次ベジェを 14 分割。曲がりは行と列で決まる)</summary>
        static List<Vector2> Curve(Vector2 a, Vector2 b, int seed)
        {
            Vector2 m = (a + b) * 0.5f, d = b - a;
            float L = Mathf.Max(0.001f, d.magnitude);
            float k = ((seed % 5) - 2) * 15f;   // 道ごとに ±30 まで散らす (磨き 案C 2026-10-29。旧 ±22 の 3 段)
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
                for (float y = contentH + 40f; y > -60f; y -= 56f, i++)
                {
                    for (int side = 0; side < 2; side++)
                    {
                        var sp = MapForest.Graded(relief + kinds[(i + side * 3) % kinds.Length], 0.62f, 12f);
                        if (sp == null) continue;
                        float w = sp.rect.width, h = sp.rect.height;
                        int col = (i * 3 + side + rnd.Next(2)) % 4;
                        float x = side == 0 ? xL - w * 0.5f - col * 110f - rnd.Next(30) : xR + w * 0.5f + col * 110f + rnd.Next(30);
                        if (side == 0 && x + w * 0.5f < -20f) continue;
                        if (side == 1 && x - w * 0.5f > viewW + 20f) continue;
                        float br = (0.62f + (float)rnd.NextDouble() * 0.22f) * (1f - 0.30f * Mathf.Clamp01(y / contentH));   // 奥 (上) ほど暗く (カンバスの depth)
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

            // 格子の空いた所に道端の群れ (磨き 案B 2026-10-28: 石＋草・切り株＋羊歯・茂み＋葦 のように 1〜2 個を寄せて置く。旧は羊歯などを 1 つずつ)。幕2/3 は今は置かない
            if (run.Act > 1) return;
            var recipes = new[] {
                new Cluster("act1_rock_big2", "tuft_stand_1", 54f, -4f), new Cluster("act1_tallgrass1", "act1_leafclump1", -60f, -8f), new Cluster("act1_stump", "act1_fern", 36f, -14f),
                new Cluster("act1_bush3", "act1_reed", 42f, -4f), new Cluster("act1_bush2", null, 0f, 0f), new Cluster("act1_root", "tuft_stand_1", 40f, -6f),
                new Cluster("bush_clump_s_1", null, 0f, 0f), new Cluster("bush_clump_l_1", null, 0f, 0f), new Cluster("act1_rock_big1", "tuft_stand_1", -48f, -12f),
                new Cluster("bush_clump_m_1", null, 0f, 0f), new Cluster("act1_leafclump2", null, 0f, 0f), new Cluster("bush_clump_m_2", "act1_reed", 36f, -4f), new Cluster("act1_tallgrass1", null, 0f, 0f) };
            Vector2 bossAt = run.Map.Count > 0 && run.Map[run.Map.Count - 1].Count > 0 ? NodePos(run.Map[run.Map.Count - 1][0], run.Map.Count - 1) : new Vector2(-9999f, -9999f);
            for (int r = 0; r < run.Map.Count - 2; r++)
            {
                var used = new HashSet<int>();
                foreach (var n in run.Map[r]) used.Add(n.Col ?? 0);
                for (int c = 0; c < Cols; c++)
                {
                    if (used.Contains(c) || rnd.NextDouble() > 0.55) continue;
                    Vector2 p = GridPos(c, r) + new Vector2(rnd.Next(30) - 15f, -10f);
                    var rc = recipes[rnd.Next(recipes.Length)];
                    float br = 0.58f - 0.22f * Mathf.Clamp01(p.y / contentH);   // 奥ほど暗く (カンバスの .56 → .36)
                    for (int k = 0; k < 2; k++)
                    {
                        string name = k == 0 ? rc.A : rc.B;
                        if (name == null) continue;
                        Vector2 q = k == 0 ? p : p + new Vector2(rc.Dx, rc.Dy);
                        bool clear = (q - bossAt).magnitude > 200f;
                        foreach (var e in edges) if (SegDist(q + new Vector2(0f, 14f), e[0], e[1]) < 50f) { clear = false; break; }
                        if (!clear) continue;
                        var sp = MapForest.Graded(relief + name, 0.7f, 10f);
                        if (sp == null) continue;
                        float sc = name.StartsWith("tuft") ? 2f : 1f;   // 小さな草の束だけ 2 倍
                        var img = Pic(content, "prop", sp, br);
                        Place(img.rectTransform, q, new Vector2(sp.rect.width * sc, sp.rect.height * sc), new Vector2(0.5f, 0f));
                    }
                }
            }
        }

        /// <summary>道端の群れの型: 絵 A と、寄せて置く絵 B (無ければ null) とそのずれ</summary>
        struct Cluster
        {
            public string A, B; public float Dx, Dy;
            public Cluster(string a, string b, float dx, float dy) { A = a; B = b; Dx = dx; Dy = dy; }
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
                // 頭上の枝と下の角の枝 (磨き 案B 2026-10-28: 画面の角に 2 倍・明るさ .30 の影。右は左右を裏返す)
                void Corner(string name, string file, Vector2 anchor, Vector2 pos, float scale, bool flip, float alpha)
                {
                    var sp = MapForest.Graded(relief + file, 0.5f, 18f);
                    if (sp == null) return;
                    var img = Pic(view, name, sp, 0.30f);
                    img.rectTransform.anchorMin = img.rectTransform.anchorMax = anchor;
                    img.rectTransform.pivot = new Vector2(0f, anchor.y);
                    img.rectTransform.sizeDelta = new Vector2(sp.rect.width * scale, sp.rect.height * scale);
                    img.rectTransform.anchoredPosition = pos;
                    if (flip) img.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                    if (alpha < 1f) { var c = img.color; c.a = alpha; img.color = c; }
                }
                Corner("bough-left", "bough_hang_1", new Vector2(0f, 1f), new Vector2(-30f, 14f), 2.25f, false, 1f);
                Corner("bough-right", "bough_hang_2", new Vector2(1f, 1f), new Vector2(30f, 10f), 2.25f, true, 1f);
                Corner("bough-mid", "bough_hang_3", new Vector2(0f, 1f), new Vector2(300f, 40f), 2f, false, 0.8f);
                Corner("frame-bl", "frame_1", new Vector2(0f, 0f), new Vector2(-20f, -14f), 2.2f, false, 1f);
                Corner("frame-br", "frame_2", new Vector2(1f, 0f), new Vector2(20f, -22f), 2.15f, true, 1f);
                // 上部バーと下のボタンの下の暗がり (字が森に溶けない)
                var top = Layer("band-top", MapForest.VFade(), new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.88f));
                top.rectTransform.anchorMin = new Vector2(0f, 1f); top.rectTransform.anchorMax = new Vector2(1f, 1f); top.rectTransform.pivot = new Vector2(0.5f, 1f);
                top.rectTransform.sizeDelta = new Vector2(0f, RunUi.TopH + 60f); top.rectTransform.anchoredPosition = Vector2.zero;
                var bot = Layer("band-bottom", MapForest.VFade(), new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.88f));
                bot.rectTransform.anchorMin = new Vector2(0f, 0f); bot.rectTransform.anchorMax = new Vector2(1f, 0f); bot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                bot.rectTransform.sizeDelta = new Vector2(0f, 100f); bot.rectTransform.anchoredPosition = new Vector2(0f, 50f);
                bot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            }
        }

        static void Node(GameRoot g, RectTransform parent, RectTransform labels, MapNode n, int row, int index, bool cur, bool avail, bool taken, bool passed, bool interactive, int recipeCount, MapLife life)
        {
            bool boss = n.Type == MapNodeTypes.Boss;
            Vector2 at = NodePos(n, row);
            var cell = UiKit.NewRect("node-" + row + "-" + index, parent);
            Place(cell, at, new Vector2(boss ? 200f : 124f, boss ? 170f : 124f), new Vector2(0.5f, 0.5f));
            float asz = boss ? 84f : 52f;   // 印 (磨き 案C 2026-10-29: 64 → 52・ボス 96 → 84)

            Image Soft(string name, Sprite sp, Color c, Vector2 pos, Vector2 size)
            {
                var img = UiKit.Pan(cell, c, name);
                img.sprite = sp; img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                img.rectTransform.sizeDelta = size; img.rectTransform.anchoredPosition = pos;
                return img;
            }

            // 印の円盤 (磨き 案B・印 1「石の円盤」2026-10-28): 青灰の石の円盤 72 (ボス 108) を節の中心の 6px 上に。
            // 進める節だけ真鍮の縁と淡い光、現在地は真鍮の二重の縁に灯の溜まり (坑匠のランタン)、通った節は沈める、幕ボスは薔薇の縁。旧「土の空き地の楕円と大きな光」は廃止
            const float cy = 6f;
            float dsz = boss ? 96f : 62f;   // 円盤 (磨き 案C 2026-10-29: 72 → 62・ボス 108 → 96)
            Image Ring(string name, float d, float w, Color c)
            {
                float rs = MapForest.RingSize(d, w);
                var im = Soft(name, MapForest.RingCircle(d, w), c, new Vector2(0f, cy), new Vector2(rs, rs));
                if (c.a < 1f) UiKit.LinearShade(im, 0.12f);   // 下は暗い地面
                return im;
            }
            if (cur) Soft("ember", MapForest.Radial(), new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.40f), new Vector2(0f, cy), new Vector2(240f, 240f));
            if (avail) Soft("halo", MapForest.Radial(), new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.22f), new Vector2(0f, cy), new Vector2(140f, 140f));
            if (boss) Soft("boss-glow", MapForest.Radial(), new Color(PaperFx.Rose.r, PaperFx.Rose.g, PaperFx.Rose.b, 0.26f), new Vector2(0f, cy + 6f), new Vector2(260f, 240f));
            Soft("shadow", MapForest.Radial(), new Color(0f, 0f, 0f, 0.6f), new Vector2(0f, cy - 7f), new Vector2(dsz + 36f, dsz + 36f));   // カンバスの 0 7px 14px の影
            if (avail) Soft("rim-glow", MapForest.Radial(), new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.5f), new Vector2(0f, cy), new Vector2(dsz + 44f, dsz + 44f));
            else if (cur) Soft("rim-glow", MapForest.Radial(), new Color(PaperFx.Ember.r, PaperFx.Ember.g, PaperFx.Ember.b, 0.6f), new Vector2(0f, cy), new Vector2(dsz + 52f, dsz + 52f));
            float tsz = MapForest.TokenSize(dsz);
            Soft("disc", MapForest.Token(boss), passed && !taken && !cur ? new Color(1f, 1f, 1f, 0.5f) : Color.white, new Vector2(0f, cy), new Vector2(tsz, tsz));
            Image ring = null;
            if (boss) { Ring("ring", dsz + 3f, 3f, new Color(PaperFx.Rose.r, PaperFx.Rose.g, PaperFx.Rose.b, 0.7f)); Ring("ring2", dsz + 11f, 5f, new Color(PaperFx.Rose.r, PaperFx.Rose.g, PaperFx.Rose.b, 0.18f)); }
            else if (cur) { Ring("ring", dsz + 3f, 3f, PaperFx.Brass); Ring("ring2", dsz + 10f, 4f, new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.30f)); }
            else if (avail) ring = Ring("ring", dsz + 3f, 3f, PaperFx.BrassLight);
            else if (taken) Ring("ring", dsz + 2f, 2f, new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.45f));
            else Ring("ring", dsz + 2f, 2f, new Color(PaperFx.PaperDim.r, PaperFx.PaperDim.g, PaperFx.PaperDim.b, passed ? 0.07f : 0.14f));   // 細い縁 (通らなかった節は半分)

            // 印 (32 ドットを 2 倍。円盤の真ん中。進める節は少し浮く)
            Image icon = null;
            {
                var nodeArt = Theme.Art("map", "node_" + n.Type);
                icon = UiKit.Icon(cell, NodeIcon(n.Type), boss ? 56f : 34f);
                if (nodeArt != null) { icon.sprite = nodeArt; icon.color = Color.white; }
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.sizeDelta = new Vector2(asz, asz); UiKit.Le(icon, asz, asz, asz, asz);
                icon.rectTransform.anchoredPosition = new Vector2(0f, cy + (avail ? 8f : 0f));
                icon.color = avail || boss || cur ? Color.white : taken ? new Color(0.72f, 0.72f, 0.74f, 1f) : passed ? new Color(0.36f, 0.36f, 0.41f, 1f) : new Color(0.9f, 0.9f, 0.92f, 1f);
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
                Place(bt.rectTransform, at + new Vector2(70f, 34f), new Vector2(360f, 32f), new Vector2(0f, 0.5f));
                var bs = UiKit.Txt(labels, "幕ボス ・ あと " + left + " 行", 14, PaperFx.PaperDim, TextAnchor.MiddleLeft);
                bs.outlineWidth = 0.2f; bs.outlineColor = new Color(0f, 0f, 0f, 0.85f); bs.raycastTarget = false;
                Place(bs.rectTransform, at + new Vector2(70f, 8f), new Vector2(360f, 22f), new Vector2(0f, 0.5f));
            }
            // 名札 (進める節)
            if (avail && !boss)
            {
                var tag = BattleScreen.Tag(labels, 30f, 0f);
                tag.anchorMin = tag.anchorMax = Vector2.zero;
                tag.pivot = new Vector2(0.5f, 1f);
                tag.sizeDelta = new Vector2(80f, 30f);
                tag.anchoredPosition = at + new Vector2(0f, -36f);
                var hg = tag.GetComponent<HorizontalLayoutGroup>(); if (hg != null) hg.childAlignment = TextAnchor.MiddleCenter;
                var t = UiKit.Txt(tag, NodeName(n.Type), 16, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Le(t, -1f, 26f, -1f, 26f);
            }

            string tip = NodeTip(n, row, g.Rs);
            // ⭐ 工房: いま手元でレシピが成立する対があれば印の右上に星 (2026-09-12「レシピは誰も踏まなかった」への提示)
            if (n.Type == MapNodeTypes.Workshop && recipeCount > 0 && !passed)
            {
                var star = UiKit.Icon(cell, "star", 32f);
                star.raycastTarget = false;
                star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                star.rectTransform.anchoredPosition = new Vector2(28f, 52f);   // 円盤 62 の右上 (磨き 案C)
                int price = 0;
                try { price = DeckRogue.Engine.Run.WorkshopFusePrice(g.Rs); } catch (Exception) { }
                tip += "\n★ 今の手札でレシピ（手書きの一品）が " + recipeCount + " 組 作れる" + (g.Rs.Gold < price ? "（所持金が " + price + "G に足りない）" : "（" + price + "G）");
            }

            // 当たり (透明。印と空き地を覆う・指の的は 110 以上)
            var hit = UiKit.Pan(cell, new Color(0f, 0f, 0f, 0f), "hit");
            hit.raycastTarget = true;
            hit.rectTransform.anchorMin = hit.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hit.rectTransform.sizeDelta = boss ? new Vector2(140f, 140f) : new Vector2(112f, 112f);
            hit.rectTransform.anchoredPosition = new Vector2(0f, cy);
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
        /// <summary>見出し (磨き 案B 2026-10-28・PC): 顔の右に「幕 N」の真鍮の札・幕の名前 34・行／勝利／次の焚き火までの小さな行。上部バーの札の代わり</summary>
        static void Header(RectTransform root, RunState run)
        {
            // 紙の帯 (磨き 案C 2026-10-29: 平らな UI の板をやめ、紙の札に墨の字。幕の札は真鍮)
            var band = UiKit.Pan(root, Color.white, "act-band");
            band.sprite = PaperFx.Tag2; band.type = Image.Type.Sliced; band.pixelsPerUnitMultiplier = 1f; band.raycastTarget = false;
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(112f, -96f), new Vector2(452f, -16f));
            var chip = UiKit.Pan(band.rectTransform, PaperFx.BrassLight, "act-chip");
            chip.raycastTarget = false;
            UiKit.Anchor(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -42f), new Vector2(68f, -18f));
            var ct = UiKit.Txt(chip.rectTransform, "幕 " + run.Act, 14, PaperFx.BrassInk, TextAnchor.MiddleCenter, true);
            ct.characterSpacing = 2f; ct.raycastTarget = false;
            UiKit.Stretch(ct.rectTransform, 0f, 0f, 0f, 0f);
            var name = UiKit.Txt(band.rectTransform, ActName(run.Act), 32, PaperFx.Ink, TextAnchor.MiddleLeft, true);
            name.characterSpacing = 6f; name.raycastTarget = false;
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(80f, -54f), new Vector2(-10f, -6f));
            int toFire = BattlesToCampfire(run);
            string sub = "行 " + (run.Row + 1) + " / " + run.Map.Count + "  ・  勝利 " + run.BattlesWon + " 戦" + (toFire >= 0 ? "  ・  次の焚き火まで " + toFire + " 戦" : "");
            var st = UiKit.Txt(band.rectTransform, sub, 14, PaperFx.InkSoft, TextAnchor.MiddleLeft);
            st.characterSpacing = 1f; st.raycastTarget = false;
            UiKit.Anchor(st.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -78f), new Vector2(-10f, -54f));
        }

        /// <summary>今の節から焚き火に着くまでに踏む戦闘 (通常・強個体) の最少数。届く焚き火が無ければ -1</summary>
        static int BattlesToCampfire(RunState run)
        {
            int result = -1;
            if (run.Map.Count == 0) return result;
            var choices = DeckRogue.Engine.Run.NextChoices(run);
            var cur = new Dictionary<int, int>();   // 今見ている行の 節の番号 → ここまでの戦闘数
            for (int i = 0; i < choices.Count; i++) cur[choices[i]] = 0;
            int row = run.Row + 1;
            while (row < run.Map.Count && cur.Count > 0)
            {
                var next = new Dictionary<int, int>();
                foreach (var kv in cur)
                {
                    if (kv.Key < 0 || kv.Key >= run.Map[row].Count) continue;
                    var n = run.Map[row][kv.Key];
                    if (n.Type == MapNodeTypes.Campfire) { if (result < 0 || kv.Value < result) result = kv.Value; continue; }
                    int v = kv.Value + (n.Type == MapNodeTypes.Battle || n.Type == MapNodeTypes.Elite ? 1 : 0);
                    if (result >= 0 && v >= result) continue;
                    for (int k = 0; k < n.Next.Count; k++) { int j = n.Next[k]; int old; if (!next.TryGetValue(j, out old) || v < old) next[j] = v; }
                }
                cur = next; row++;
            }
            return result;
        }

        /// <summary>凡例 (磨き 案B 2026-10-28): 右下の小さな板に 2 列 4 行 (絵 24・字 14)。旧は右の中ほどに幅 176 の 8 行</summary>
        static void Legend(RectTransform root)
        {
            float w = 318f, h = 166f;
            var panel = UiKit.Pan(root, Color.white, "legend");
            panel.sprite = PaperFx.Tag2; panel.type = Image.Type.Sliced; panel.pixelsPerUnitMultiplier = 1f; panel.raycastTarget = false;   // 紙の札 (磨き 案C 2026-10-29。旧は夜色の板)
            UiKit.Anchor(panel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f - w, 88f), new Vector2(-28f, 88f + h));
            var hd = UiKit.Txt(panel.rectTransform, "凡例", 13, PaperFx.BrassInk, TextAnchor.MiddleLeft, true);
            hd.characterSpacing = 8f; hd.raycastTarget = false;
            UiKit.Anchor(hd.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -32f), new Vector2(-14f, -10f));
            var rule = UiKit.Pan(panel.rectTransform, PaperFx.Brass, "rule");
            rule.raycastTarget = false;
            UiKit.Anchor(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -35f), new Vector2(-14f, -33.5f));
            var types = new[] { MapNodeTypes.Battle, MapNodeTypes.Elite, MapNodeTypes.Event, MapNodeTypes.Campfire, MapNodeTypes.Shop, MapNodeTypes.Workshop, MapNodeTypes.Treasure, MapNodeTypes.Boss };
            for (int i = 0; i < types.Length; i++)
            {
                float x = 14f + (i % 2) * 152f, y = -36f - (i / 2) * 30f;
                var row = UiKit.NewRect("legend-" + types[i], panel.rectTransform);
                UiKit.Anchor(row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y - 30f), new Vector2(x + 146f, y));
                var ic = UiKit.Icon(row, NodeIcon(types[i]), 22f);
                var art = Theme.Art("map", "node_" + types[i]);
                if (art != null) { ic.sprite = art; ic.color = Color.white; }
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0f, 0.5f); ic.rectTransform.pivot = new Vector2(0f, 0.5f);
                ic.rectTransform.sizeDelta = new Vector2(24f, 24f); ic.rectTransform.anchoredPosition = Vector2.zero;
                var t = UiKit.Txt(row, NodeName(types[i]), 14, PaperFx.Ink, TextAnchor.MiddleLeft, true);
                t.raycastTarget = false;
                UiKit.Anchor(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(32f, 0f), new Vector2(0f, 0f));
            }
        }

        /// <summary>落書きの道具 (磨き 案B 2026-10-28: 右下の丸い帯に絵のボタン 4 つ＝紙ペン・朱ペン・消しゴム・全消し。旧は字のボタン 4 つと案内の行)。押したペンが光る (=ペンモード。同じものを押すと解除)</summary>
        static void DoodleToolbar(GameRoot g, RectTransform root)
        {
            float bh = UiKit.Phone ? 56f : 44f;
            var bar = UiKit.Pan(root, Color.white, "doodle-tools");
            bar.sprite = PaperFx.MapPill; bar.type = Image.Type.Sliced; bar.pixelsPerUnitMultiplier = 1f; bar.raycastTarget = false;
            UiKit.Anchor(bar.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f - 300f, 24f), new Vector2(-28f, 24f + bh));
            bar.rectTransform.pivot = new Vector2(1f, 0f);
            var hg = UiKit.Horz(bar.rectTransform, 2, 0);
            hg.padding = new RectOffset(16, 8, 0, 0);
            hg.childAlignment = TextAnchor.MiddleRight; hg.childForceExpandWidth = false; hg.childForceExpandHeight = false;
            var fit = bar.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            string tipText = g.DoodleMode ? (g.DoodlePen == 2 ? "消しゴム: 線に触れると消える" : "ペン: ドラッグで描ける") : (UiKit.Phone ? "ペンを押すと指で描ける" : "右ドラッグで描ける");
            var tip = UiKit.Txt(bar.rectTransform, tipText, 12, PaperFx.PaperDim, TextAnchor.MiddleRight);
            tip.characterSpacing = 1f; tip.raycastTarget = false;
            UiKit.Le(tip, -1f, bh, -1f, bh);
            var tfit = tip.gameObject.AddComponent<ContentSizeFitter>(); tfit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var spacer = UiKit.NewRect("sp", bar.rectTransform); UiKit.Le(spacer, 8f, bh, 8f, bh);
            string[] kinds = { "pen", "pen", "eraser", "trash" };
            Color[] cols = { PaperFx.Paper, DoodleLayer.PenColor(1), PaperFx.PaperDim, PaperFx.PaperDim };
            string[] tips = { "紙ペン（ドラッグで地図に描く。もう一度押すと解除）", "朱ペン（同じ）", "消しゴム（触れた線ごと消す）", "全消し" };
            for (int i = 0; i < kinds.Length; i++)
            {
                int idx = i;
                if (i == 2) { var dv = UiKit.Pan(bar.rectTransform, new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.3f), "divider"); dv.raycastTarget = false; UiKit.LinearShade(dv, 0.12f); UiKit.Le(dv, 1f, 22f, 1f, 22f); }
                bool active = g.DoodleMode && g.DoodlePen == idx;
                var rt = UiKit.NewRect("tool-" + kinds[i], bar.rectTransform);
                var bg = rt.gameObject.AddComponent<Image>();
                bg.color = active ? new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.18f) : new Color(0f, 0f, 0f, 0f);
                if (active) UiKit.LinearShade(bg, 0.12f);
                var btn = rt.gameObject.AddComponent<Button>();
                var ic = new GameObject("icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ic.transform.SetParent(rt, false); ic.sprite = MapForest.ToolIcon(kinds[i]); ic.color = cols[i]; ic.raycastTarget = false;
                ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); ic.rectTransform.sizeDelta = new Vector2(24f, 24f);
                btn.targetGraphic = ic;
                var cb = btn.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(1.2f, 1.2f, 1.1f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); cb.selectedColor = Color.white; cb.fadeDuration = 0.05f; btn.colors = cb;
                btn.onClick.AddListener(delegate
                {
                    Audio.Ui("click");
                    if (idx == 3) { g.DoodlesFor(g.Rs.Act).Clear(); g.Rebuild(); return; }
                    if (g.DoodleMode && g.DoodlePen == idx) g.DoodleMode = false;   // 同じ道具をもう一度 = ペンモード解除
                    else { g.DoodleMode = true; g.DoodlePen = idx; }
                    g.Rebuild();
                });
                BattleScreen.SetSize(rt, UiKit.Phone ? 48f : 38f, bh - 8f);
                Tooltip.Attach(rt.gameObject, delegate { return tips[idx]; });
            }
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

        /// <summary>節のホバーの札 (磨き 案C 2026-10-29: 本家の形 = 行・種類と中身・いまの HP と G・勝てば/入ると何が起きるか)</summary>
        static string NodeTip(MapNode n, int row, RunState run)
        {
            string enc = "";
            if (n.EncounterId != null)
            {
                try { enc = "\n" + Content.EncounterName(n.EncounterId); }
                catch (Exception) { enc = "\n" + n.EncounterId; }
            }
            string head = "<size=13>行 " + (row + 1) + " / " + run.Map.Count + "</size>\n";
            string now = "\nいま　HP " + run.Hp + " / " + run.MaxHp + " ・ " + run.Gold + " G";
            switch (n.Type)
            {
                case MapNodeTypes.Battle: return head + "<b>戦闘</b>" + enc + now + "\n勝てば　札 3 枚から 1 枚 ・ 12〜18 G ・ 魔素 +5";
                case MapNodeTypes.Elite: return head + "<b>強個体</b>" + enc + now + "\n勝てば　レリック 3 択 ・ レア確定の札 ・ 42〜58 G ・ 魔素 +10";
                case MapNodeTypes.Boss: return head + "<b>幕ボス</b>" + enc + now + "\n倒すと　回復 ・ レリック 3 択 ・ 次の幕へ";
                case MapNodeTypes.Campfire: return head + "<b>焚き火</b>" + now + "\n休む（最大 HP の割合で回復）か、札を 1 枚鍛える";
                case MapNodeTypes.Workshop: return head + "<b>工房</b>" + now + "\n同じ色の札 2 枚を 1 枚に溶かす（" + DeckRogue.Engine.Run.WORKSHOP_FUSE_PRICE + " G）";
                case MapNodeTypes.Shop: return head + "<b>ショップ</b>" + now + "\n札 ・ レリック ・ 札の除去 ・ 鍛える";
                case MapNodeTypes.Event: return head + "<b>？</b>" + now + "\n入るまで中身は分からない（イベント ・ 戦闘 ・ 店 ・ 宝箱）";
                case MapNodeTypes.Treasure: return head + "<b>宝箱</b>" + now + "\nレリック 3 択";
                default: return n.Type;
            }
        }

    }
}
