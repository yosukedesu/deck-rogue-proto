// MapScreen.cs — マップ画面 (M3・2026-09-07): 本家式の縦スクロール地図。ノードは格子列に置き、線で接続。進める道だけ光る
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class MapScreen
    {
        const float MapW = 1100f;      // 地図の描画幅
        const float RowH = 112f;       // 1行の高さ
        static float PadY = 90f;       // 上下の余白 (描いた地図 Art/map/act<N>_map があればその高さに合わせて Build が決める。2026-10-28 品質スプリント 案 B)
        static float _ox = 0f;         // 節の格子の左端 (content の中の x)。地図は画面の幅いっぱいなので、格子 (幅 MapW) を中央へ寄せる
        static RectTransform _marker;  // 現在地の駒 (選んだ節へ歩いてから命令を出す)
        static bool _walking;
        const float MarkerSize = 84f;  // 駒の箱の予備 (絵が無い時)
        const float MarkerDot = 1.5f;  // 駒は 1ドット＝1.5px (戦闘の 3px の半分)
        static float _markerFeet;      // 駒の枠の下の透明行 (px)。歩く先も同じだけ下げる
        static readonly Vector2 MarkerOffset = new Vector2(0f, -10f);
        static float NodeSize { get { return UiKit.Phone ? 74f : 60f; } }   // スマホは指で押せる大きさ (2026-09-14)
        static float BossSize { get { return UiKit.Phone ? 104f : 92f; } }
        const int Cols = 7;

        /// <summary>他の画面の上に重ねる読み取り専用の地図 (2026-09-12 ユーザー「マップは常に見れるようにして」)。進路は選べない・閉じるだけ</summary>
        public static void Overlay(GameRoot g, RectTransform root) { Build(g, root, true); }

        public static void Build(GameRoot g, RectTransform root) { Build(g, root, false); }

        static void Build(GameRoot g, RectTransform root, bool overlay)
        {
            var run = g.Rs;
            if (!overlay) BattleScreen.BuildBackground(root, run.Act);
            var dim = UiKit.Pan(root, new Color(0f, 0f, 0f, overlay ? 0.82f : 0.42f), "dim");   // 重ねる時は下の画面 (手札・吹き出し) が地図の線と混ざらない濃さ
            dim.raycastTarget = overlay;   // 重ねる時は下の画面のボタンを塞ぐ
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            int recipeCount = 0;   // ⭐ いま手元で作れるレシピ対の数 (工房ノードに添える。2026-09-12)
            try { recipeCount = Fusion.RecipePairsInDeck(run.Deck).Count; } catch (Exception) { }

            // 地図本体 (縦スクロール)。2026-10-28 品質スプリント 案 B「描いた地図」: 画面の幅いっぱいの窓に、描いた地図 (Art/map/act<N>_map・1ドット=2px) を
            // content の下敷きとして敷き、節の格子 (幅 MapW) はその中央に置く。地図が無ければ今までどおり舞台の暗がりの上に格子だけ
            _walking = false; _marker = null;
            var view = UiKit.NewRect("mapview", root);
            // 画面の切り欠き (パンチホール＝左端の縦の中央) に左の列のノードが掛かる狭い端末では、格子を右へ寄せる (2026-09-29 p10。S25 と PC は 0)
            var mcs = BattleScreen.CanvasSize(root);
            float mapShift = Mathf.Max(0f, UiKit.CutoutLeft(RunUi.TopH, mcs.y) - (mcs.x / 2f - MapW / 2f + 60f - NodeSize / 2f));
            mapShift = Mathf.Min(mapShift, Mathf.Max(0f, mcs.x / 2f - MapW / 2f));   // 右の端からはみ出さない
            _ox = Mathf.Max(0f, (mcs.x - MapW) / 2f) + mapShift;
            UiKit.Anchor(view, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, overlay ? 90f : 0f), new Vector2(0f, -RunUi.TopH - (overlay ? 56f : 0f)));   // 重ねる時は見出しと「閉じる」の分だけ狭める
            var mapArt = Theme.Art("map", "act" + run.Act + "_map");
            var viewImg = view.gameObject.AddComponent<Image>();
            viewImg.color = mapArt != null ? new Color(0f, 0f, 0f, overlay ? 0.35f : 0f) : new Color(0f, 0f, 0f, overlay ? 0.5f : 0.18f);
            var mask = view.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.NewRect("content", view);
            int rows = run.Map.Count;
            float needH = 90f * 2f + (rows + 1) * RowH;   // +1 = スタート地点の行
            float artW = mapArt != null ? mapArt.texture.width * 2f : 0f, artH = mapArt != null ? mapArt.texture.height * 2f : 0f;
            float contentH = mapArt != null ? Mathf.Max(artH, needH) : needH;
            PadY = (contentH - (rows + 1) * RowH) / 2f;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.sizeDelta = new Vector2(0f, contentH);
            content.anchoredPosition = Vector2.zero;
            if (mapArt != null)
            {
                // 描いた地図 (下敷き)。幅は 1ドット=2px のまま中央に置き、狭い画面では左右が窓で切れる (格子 1100 は常に入る)
                var bg = new GameObject("map-art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                bg.transform.SetParent(content, false);
                bg.sprite = mapArt; bg.raycastTarget = false; bg.color = Color.white;
                bg.rectTransform.anchorMin = bg.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                bg.rectTransform.pivot = new Vector2(0.5f, 0f);
                bg.rectTransform.sizeDelta = new Vector2(artW, artH);
                bg.rectTransform.anchoredPosition = Vector2.zero;
                // 窓の上下の縁は夜色に溶かす (上部バーの下に地図が突き当たらない)
                var fadeTop = UiKit.Pan(view, PaperFx.Ground, "fade-top"); fadeTop.raycastTarget = false; fadeTop.sprite = FadeSprite();
                UiKit.Anchor(fadeTop.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -140f), new Vector2(0f, 0f));
                var fadeBot = UiKit.Pan(view, PaperFx.Ground, "fade-bottom"); fadeBot.raycastTarget = false; fadeBot.sprite = FadeSprite();
                UiKit.Anchor(fadeBot.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 110f));
                fadeBot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                fadeTop.transform.SetAsLastSibling(); fadeBot.transform.SetAsLastSibling();
            }
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

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

            // 線 (ノードより下に描く)
            var lines = UiKit.NewRect("lines", content);
            UiKit.Stretch(lines, 0f, 0f, 0f, 0f);
            // スタート地点 → 行0
            Vector2 startPos = new Vector2(_ox + MapW / 2f, PadY + RowH * 0.5f);
            // 行0 へ入る踏み跡は引かない (本家: 最初の行はどこからでも入れる。進める節は輪と光で分かる。2026-10-28 案 B)
            for (int r = 0; r < rows; r++)
            {
                var row = run.Map[r];
                for (int i = 0; i < row.Count; i++)
                {
                    var n = row[i];
                    if (r + 1 >= rows) continue;
                    for (int k = 0; k < n.Next.Count; k++)
                    {
                        int j = n.Next[k];
                        if (j < 0 || j >= run.Map[r + 1].Count) continue;
                        bool fromCur = r == run.Row && i == run.Col;
                        bool avail = fromCur && choiceSet.Contains(j);
                        bool taken = path.Contains(Key(r, i)) && path.Contains(Key(r + 1, j));
                        bool passed = r < run.Row;
                        Line(lines, NodePos(n, r), NodePos(run.Map[r + 1][j], r + 1), avail, taken, passed && !taken);
                    }
                }
            }

            // 出発点の旗と「スタート」の字は廃止 (駒が出発点に立つ。2026-10-28 案 B)

            // 落書きの層 (線の下・ノードの上に見せるため、ノードの後に作って最前面へ。左クリックは素通し = ICanvasRaycastFilter)
            var doodleRt = UiKit.NewRect("doodle", content);
            doodleRt.anchorMin = new Vector2(0f, 0f); doodleRt.anchorMax = new Vector2(1f, 1f); doodleRt.pivot = new Vector2(0f, 0f);
            doodleRt.offsetMin = Vector2.zero; doodleRt.offsetMax = Vector2.zero;
            doodleRt.gameObject.AddComponent<CanvasRenderer>();   // Graphic の前提 (エディタは自動で足さない)
            var doodle = doodleRt.gameObject.AddComponent<DoodleLayer>();
            doodle.Root = g; doodle.Strokes = g.DoodlesFor(run.Act); doodle.Editable = !overlay; doodle.raycastTarget = !overlay;

            // ノード
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
                    Node(g, content, n, r, i, cur, avail, taken, passed, !overlay, recipeCount);
                }
            }

            doodleRt.SetAsLastSibling();   // 落書きはノードの上に (右ドラッグ/ペンモードの時だけ入力を受ける)

            // 現在地の駒 (リーダーの絵が節の台の上に立つ。選ぶと踏み跡を歩く。2026-10-28 案 B)
            {
                var mk = new GameObject("marker", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                mk.transform.SetParent(content, false);
                mk.raycastTarget = false;
                var spr = Creature.Get("leaders", run.LeaderId, true);
                mk.sprite = spr;
                mk.preserveAspect = true;
                mk.rectTransform.anchorMin = mk.rectTransform.anchorMax = new Vector2(0f, 0f);
                mk.rectTransform.pivot = new Vector2(0.5f, 0f);
                // 1ドット＝1.5px で絵の枠ごと置く (このは 88×64 → 132×96・ひばな 128×84 → 192×126 = どちらも背丈 64 ドットが 96px)。足元は枠の下の透明行を引いて台の中心の 10px 下
                float mw = spr != null ? spr.rect.width * MarkerDot : MarkerSize, mh = spr != null ? spr.rect.height * MarkerDot : MarkerSize;
                float feet = spr != null ? Stage.FeetPad(spr) * mh : 0f;
                mk.rectTransform.sizeDelta = new Vector2(mw, mh);
                Vector2 at = run.Row >= 0 ? NodePos(run.Map[run.Row][run.Col], run.Row) : startPos;
                mk.rectTransform.anchoredPosition = at + MarkerOffset - new Vector2(0f, feet);
                _markerFeet = feet;
                mk.transform.SetSiblingIndex(doodleRt.GetSiblingIndex());   // 落書きのすぐ下 = 節の上
                _marker = mk.rectTransform;
                Tween.Punch(mk.rectTransform, 0.12f, 0.6f, true);
            }

            // 現在行が下から1/3に来るようにスクロール
            float viewH = 1080f - RunUi.TopH - (overlay ? 146f : 0f);
            float curY = run.Row >= 0 ? NodePos(run.Map[run.Row][run.Col], run.Row).y : startPos.y;
            float target = curY - viewH * 0.38f;
            float maxScroll = Math.Max(0f, contentH - viewH);
            float norm = maxScroll > 0f ? Mathf.Clamp01(target / maxScroll) : 0f;
            scroll.verticalNormalizedPosition = norm;
            var fixer = view.gameObject.AddComponent<ScrollFix>();
            fixer.Scroll = scroll; fixer.Norm = norm;

            if (overlay)
            {
                // 見出しと閉じるだけ (進路は選べない)。上部バーは下の画面のものが透けて見える
                var head = UiKit.Deco(root, "マップ — 現在地と道筋", 26, UiKit.ColText, TextAnchor.MiddleCenter);
                head.outlineWidth = 0.2f; head.outlineColor = new Color(0f, 0f, 0f, 0.7f);
                UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -RunUi.TopH - 44f), new Vector2(0f, -RunUi.TopH - 4f));
                RunUi.BottomButton(root, "閉じる", delegate { g.ViewMap = false; g.Rebuild(); }, 18, 220f, 50f, 0f, 28f);
                return;
            }
            RunUi.TopBar(g, root, "マップ");
            RunUi.Message(g, root);

            // 左下: 案内 / ランを放棄
            var hint = UiKit.Txt(root, choices.Count > 0 ? (UiKit.Phone ? "光っている道へ進めます" : "光っている道へ進めます。ノードにカーソルを重ねると中身の説明") : "進めるノードがありません", 15, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Anchor(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0.35f, 0f), new Vector2(24f, 70f), new Vector2(0f, 100f));
            if (!UiKit.Phone)   // スマホは地図が画面の幅いっぱいで凡例が左の列に重なるので出さない (節は印 2 倍で読める。2026-10-28 案 B)
            {
                var legend = UiKit.NewRect("legend", root);
                float legendL = UiKit.SafeLeft(24f, mcs.y - 420f, mcs.y - 110f);   // 凡例の行は縦の中央に掛かるので画面の切り欠き (パンチホール) の右から (2026-09-29 p10)
                UiKit.Anchor(legend, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(legendL, 110f), new Vector2(legendL + 306f, 420f));
                var lg = UiKit.Vert(legend, 6, 0);
                lg.childForceExpandHeight = false;
                LegendRow(legend, MapNodeTypes.Battle); LegendRow(legend, MapNodeTypes.Elite); LegendRow(legend, MapNodeTypes.Event);
                LegendRow(legend, MapNodeTypes.Campfire); LegendRow(legend, MapNodeTypes.Shop); LegendRow(legend, MapNodeTypes.Workshop);
                LegendRow(legend, MapNodeTypes.Treasure); LegendRow(legend, MapNodeTypes.Boss);
            }

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

            // 右下: 幕の進み
            var prog = UiKit.Txt(root, "幕 " + run.Act + " / 3   勝利 " + run.BattlesWon + " 戦", 15, UiKit.ColDim, TextAnchor.MiddleRight);
            UiKit.Anchor(prog.rectTransform, new Vector2(0.7f, 0f), new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(-24f, 50f));
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

        static long Key(int r, int c) { return ((long)r << 16) | (uint)c; }

        static Vector2 NodePos(MapNode n, int row)
        {
            float colW = (MapW - 120f) / (Cols - 1);
            float x = _ox + 60f + (n.Col ?? 0) * colW;
            float y = PadY + RowH * (row + 1) + RowH * 0.5f;
            return new Vector2(x, y);
        }

        static Sprite _dot, _fade;
        /// <summary>丸い点 (踏み跡・節の台)。16×16・縁は 1px でなだらか</summary>
        static Sprite DotSprite()
        {
            if (_dot != null) return _dot;
            const int S = 32;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / (S / 2f);
                float a = Mathf.Clamp01((1f - r) * (S / 2f) / 1.5f);   // 縁 1.5px で落ちる
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            _dot = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            return _dot;
        }
        /// <summary>縦の帯: 上が不透明・下が透明 (窓の上下の縁を夜色に溶かす)</summary>
        static Sprite FadeSprite()
        {
            if (_fade != null) return _fade;
            const int H = 64;
            var tex = new Texture2D(4, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[4 * H];
            for (int y = 0; y < H; y++) { float k = (float)y / (H - 1); float a = k * k; for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, (byte)(a * 255f)); }
            tex.SetPixels32(px); tex.Apply(false, true);
            _fade = Sprite.Create(tex, new Rect(0, 0, 4, H), new Vector2(0.5f, 0.5f), 100f);
            return _fade;
        }

        /// <summary>節と節を結ぶ踏み跡 (点の列。2026-10-28 案 B)。進める道は紙の白、通った道は真鍮、通らなかった道は薄く</summary>
        static void Line(RectTransform parent, Vector2 a, Vector2 b, bool avail, bool taken, bool passed)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            Vector2 dir = d / Math.Max(0.001f, len);
            float trim = NodeSize * 0.62f;   // ノードの台の縁から縁へ
            float usable = len - trim * 2f;
            if (usable <= 0f) return;
            int n = Mathf.Max(1, Mathf.RoundToInt(usable / 15f));
            float size = avail ? 9f : taken ? 8f : 6f;
            Color c = avail ? PaperFx.Paper3 : taken ? PaperFx.BrassLight : PaperFx.Paper;
            c.a = avail ? 0.95f : taken ? 0.9f : passed ? 0.16f : 0.4f;
            Vector2 side = new Vector2(-dir.y, dir.x);
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            for (int i = 0; i <= n; i++)
            {
                var img = UiKit.Pan(parent, c, "step");
                img.raycastTarget = false; img.sprite = DotSprite();
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size * 0.78f);
                rt.anchoredPosition = a + dir * (trim + usable * i / n) + side * ((i & 1) == 0 ? 2.5f : -2.5f);   // 左右に交互 = 足跡
                rt.localRotation = Quaternion.Euler(0f, 0f, ang);
            }
        }

        static void Node(GameRoot g, RectTransform parent, MapNode n, int row, int index, bool cur, bool avail, bool taken, bool passed, bool interactive = true, int recipeCount = 0)
        {
            bool boss = n.Type == MapNodeTypes.Boss;
            float size = boss ? BossSize : NodeSize;
            var cell = UiKit.NewRect("node-" + row + "-" + index, parent);
            cell.anchorMin = cell.anchorMax = new Vector2(0f, 0f);
            cell.sizeDelta = new Vector2(size, size);
            cell.anchoredPosition = NodePos(n, row);

            // 節の目印 (2026-10-28 案 B): 32 ドットの印を整数倍 (64px・ボス 96px) で地図の上に直に置き、足元に丸い影。進める節は真鍮の輪と光
            float asz = boss ? 96f : 64f;
            if (avail)
            {
                var glow = UiKit.Pan(cell, new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.35f), "glow");
                glow.raycastTarget = false;
                glow.sprite = ThemeFx.Shadow();
                UiKit.Anchor(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-asz, -asz), new Vector2(asz, asz));
            }
            var frame = UiKit.Pan(cell, new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, passed && !cur ? 0.3f : 0.55f), "shade");
            frame.sprite = ThemeFx.Shadow();
            frame.raycastTarget = avail && interactive;
            UiKit.Anchor(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-asz * 0.72f, -asz * 0.72f), new Vector2(asz * 0.72f, asz * 0.72f));
            if (avail)
            {
                float rs = asz + 14f;
                var ring = UiKit.Pan(cell, PaperFx.BrassLight, "ring"); ring.sprite = DotSprite(); ring.raycastTarget = false;
                UiKit.Anchor(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-rs / 2f, -rs / 2f), new Vector2(rs / 2f, rs / 2f));
                var hole = UiKit.Pan(cell, new Color(PaperFx.Window.r, PaperFx.Window.g, PaperFx.Window.b, 0.9f), "hole"); hole.sprite = DotSprite(); hole.raycastTarget = false;
                UiKit.Anchor(hole.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-rs / 2f + 3f, -rs / 2f + 3f), new Vector2(rs / 2f - 3f, rs / 2f - 3f));
            }
            var icon = UiKit.Icon(cell, NodeIcon(n.Type), boss ? 56f : 34f);
            var nodeArt = Theme.Art("map", "node_" + n.Type);
            if (nodeArt != null)
            {
                icon.sprite = nodeArt; icon.color = Color.white;
                icon.rectTransform.sizeDelta = new Vector2(asz, asz); UiKit.Le(icon, asz, asz, asz, asz);
            }
            icon.raycastTarget = false;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = Vector2.zero;
            if (!avail && !cur) icon.color = new Color(1f, 1f, 1f, passed ? 0.45f : 0.85f);

            // 名札は進める節とボスだけ (印が 2 倍になったので読める。凡例は残す)
            if (avail || boss)
            {
                var lbl = UiKit.Txt(cell, NodeName(n.Type), 13, avail ? UiKit.ColText : UiKit.ColDim, TextAnchor.MiddleCenter, true);
                lbl.raycastTarget = false;
                lbl.outlineWidth = 0.25f; lbl.outlineColor = new Color(PaperFx.Ground.r, PaperFx.Ground.g, PaperFx.Ground.b, 0.85f);
                UiKit.Anchor(lbl.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, -asz / 2f - 6f), new Vector2(70f, -asz / 2f + 14f));
            }

            // 現在地は駒 (リーダーの絵) が台の上に立つので、輪は描かない (2026-10-28 案 B)

            string tip = NodeTip(n);
            // ⭐ 工房: いま手元でレシピが成立する対があれば駒の右上に星 (2026-09-12「レシピは誰も踏まなかった」への提示)
            if (n.Type == MapNodeTypes.Workshop && recipeCount > 0 && !passed)
            {
                var star = UiKit.Icon(cell, "star", 32f);
                star.raycastTarget = false;
                star.rectTransform.anchorMin = star.rectTransform.anchorMax = new Vector2(1f, 1f);
                star.rectTransform.anchoredPosition = new Vector2(6f, 6f);
                int price = 0;
                try { price = DeckRogue.Engine.Run.WorkshopFusePrice(g.Rs); } catch (Exception) { }
                tip += "\n★ 今の手札でレシピ（手書きの一品）が " + recipeCount + " 組 作れる" + (g.Rs.Gold < price ? "（所持金が " + price + "G に足りない）" : "（" + price + "G）");
            }
            Tooltip.Attach(cell.gameObject, delegate { return tip; });

            if (avail && interactive)
            {
                var btn = frame.gameObject.AddComponent<Button>();
                btn.targetGraphic = frame;
                var cols = btn.colors; cols.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cols.pressedColor = new Color(0.85f, 0.85f, 0.85f); btn.colors = cols;
                int idx = index;
                Vector2 dest = NodePos(n, row);
                btn.onClick.AddListener(delegate
                {
                    if (_walking) return;
                    Audio.Ui("node");
                    var mk = _marker;
                    if (mk == null) { g.Do(new RunCommand_ChooseNode { Col = idx }); return; }
                    // 駒が踏み跡を歩いてから進む (2026-10-28 案 B。0.55 秒・小さく弾む)
                    _walking = true;
                    Vector2 from = mk.anchoredPosition, to = dest + MarkerOffset - new Vector2(0f, _markerFeet);
                    bool done = false;
                    Action finish = delegate { if (done) return; done = true; _walking = false; g.Do(new RunCommand_ChooseNode { Col = idx }); };
                    Tween.Run(0.55f, delegate (float k)
                    {
                        if (mk == null) return;
                        float s = k * k * (3f - 2f * k);
                        mk.anchoredPosition = Vector2.Lerp(from, to, s) + new Vector2(0f, Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3f)) * 5f);
                    }, Ease.Linear, delegate { finish(); }, delegate { _walking = false; });
                });
                Tween.Punch(cell, 0.1f, 0.8f);
            }
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
                Color? bg = active ? (Color?)(idx == 1 ? UiKit.Hex("#e9b8ae") : idx == 2 ? UiKit.Hex("#d8d2c4") : UiKit.Hex("#dfe8dc")) : null;
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

        static Color NodeTint(string type, bool avail, bool cur, bool taken, bool passed)
        {
            Color baseCol;
            switch (type)
            {
                case MapNodeTypes.Elite: baseCol = UiKit.Hex("#e0b0b0"); break;
                case MapNodeTypes.Boss: baseCol = UiKit.Hex("#f0c8a0"); break;
                case MapNodeTypes.Campfire: baseCol = UiKit.Hex("#f0c8a0"); break;
                case MapNodeTypes.Shop: baseCol = UiKit.Hex("#e8dca0"); break;
                case MapNodeTypes.Workshop: baseCol = UiKit.Hex("#d0c8e8"); break;
                case MapNodeTypes.Treasure: baseCol = UiKit.Hex("#f0e0a0"); break;
                case MapNodeTypes.Event: baseCol = UiKit.Hex("#b0e0e0"); break;
                default: baseCol = Color.white; break;
            }
            if (avail || cur) return baseCol;
            if (taken) return baseCol * new Color(0.8f, 0.8f, 0.8f, 1f);
            if (passed) return baseCol * new Color(0.45f, 0.45f, 0.45f, 1f);
            return baseCol * new Color(0.7f, 0.7f, 0.7f, 1f);
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
            UiKit.Le(row, -1f, 26f, -1f, 26f);
            var hg = UiKit.Horz(row, 8, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
            var ic = UiKit.Icon(row, NodeIcon(type), 22f);
            UiKit.Le(ic, 22f, 22f, 22f, 22f);
            var t = UiKit.Txt(row, NodeName(type), 14, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Le(t, 100f, 24f, -1f, 24f);
        }
    }
}
