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
        const float PadY = 90f;        // 上下の余白
        const float NodeSize = 60f;
        const float BossSize = 92f;
        const int Cols = 7;

        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            BattleScreen.BuildBackground(root, run.Act);
            var dim = UiKit.Pan(root, new Color(0f, 0f, 0f, 0.42f), "dim");
            dim.raycastTarget = false;
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);

            // 地図本体 (縦スクロール)
            var view = UiKit.NewRect("mapview", root);
            UiKit.Anchor(view, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-MapW / 2f, 0f), new Vector2(MapW / 2f, -RunUi.TopH));
            var viewImg = view.gameObject.AddComponent<Image>();
            viewImg.color = new Color(0f, 0f, 0f, 0.18f);
            var mask = view.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.NewRect("content", view);
            int rows = run.Map.Count;
            float contentH = PadY * 2f + (rows + 1) * RowH;   // +1 = スタート地点の行
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.sizeDelta = new Vector2(0f, contentH);
            content.anchoredPosition = Vector2.zero;
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
            Vector2 startPos = new Vector2(MapW / 2f, PadY + RowH * 0.5f);
            if (rows > 0)
            {
                for (int i = 0; i < run.Map[0].Count; i++)
                {
                    bool avail = run.Row < 0 && choiceSet.Contains(i);
                    bool taken = run.Row >= 0 && path.Contains(Key(0, i));
                    Line(lines, startPos, NodePos(run.Map[0][i], 0), avail, taken, run.Row >= 0 && !taken);
                }
            }
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

            // スタート地点
            var startMark = UiKit.Icon(content, "flag", 40f);
            startMark.rectTransform.anchorMin = startMark.rectTransform.anchorMax = new Vector2(0f, 0f);
            startMark.rectTransform.anchoredPosition = startPos;
            startMark.raycastTarget = false;
            var startLbl = UiKit.Txt(content, "スタート", 13, UiKit.ColDim, TextAnchor.MiddleCenter);
            startLbl.rectTransform.anchorMin = startLbl.rectTransform.anchorMax = new Vector2(0f, 0f);
            startLbl.rectTransform.sizeDelta = new Vector2(120f, 20f);
            startLbl.rectTransform.anchoredPosition = startPos + new Vector2(0f, -34f);

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
                    Node(g, content, n, r, i, cur, avail, taken, passed);
                }
            }

            // 現在地マーカー (リーダーの小さな絵)
            {
                var mk = new GameObject("marker", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                mk.transform.SetParent(content, false);
                mk.raycastTarget = false;
                mk.sprite = Creature.Get("leaders", run.LeaderId, true);
                mk.preserveAspect = true;
                mk.rectTransform.anchorMin = mk.rectTransform.anchorMax = new Vector2(0f, 0f);
                mk.rectTransform.sizeDelta = new Vector2(56f, 56f);
                Vector2 at = run.Row >= 0 ? NodePos(run.Map[run.Row][run.Col], run.Row) : startPos;
                mk.rectTransform.anchoredPosition = at + new Vector2(-66f, 30f);
                Tween.Punch(mk.rectTransform, 0.12f, 0.6f);
            }

            // 現在行が下から1/3に来るようにスクロール
            float viewH = 1080f - RunUi.TopH;
            float curY = run.Row >= 0 ? NodePos(run.Map[run.Row][run.Col], run.Row).y : startPos.y;
            float target = curY - viewH * 0.38f;
            float maxScroll = Math.Max(0f, contentH - viewH);
            float norm = maxScroll > 0f ? Mathf.Clamp01(target / maxScroll) : 0f;
            scroll.verticalNormalizedPosition = norm;
            var fixer = view.gameObject.AddComponent<ScrollFix>();
            fixer.Scroll = scroll; fixer.Norm = norm;

            RunUi.TopBar(g, root, "マップ");
            RunUi.Message(g, root);

            // 左下: 案内 / ランを放棄
            var hint = UiKit.Txt(root, choices.Count > 0 ? "光っている道へ進めます。ノードにカーソルを重ねると中身の説明" : "進めるノードがありません", 15, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Anchor(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0.35f, 0f), new Vector2(24f, 70f), new Vector2(0f, 100f));
            var legend = UiKit.NewRect("legend", root);
            UiKit.Anchor(legend, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 110f), new Vector2(330f, 420f));
            var lg = UiKit.Vert(legend, 6, 0);
            lg.childForceExpandHeight = false;
            LegendRow(legend, MapNodeTypes.Battle); LegendRow(legend, MapNodeTypes.Elite); LegendRow(legend, MapNodeTypes.Event);
            LegendRow(legend, MapNodeTypes.Campfire); LegendRow(legend, MapNodeTypes.Shop); LegendRow(legend, MapNodeTypes.Workshop);
            LegendRow(legend, MapNodeTypes.Treasure); LegendRow(legend, MapNodeTypes.Boss);

            var abandon = UiKit.Btn(root, "ランを放棄", delegate { g.BackToSetup(); }, 14, true, UiKit.Hex("#8a5a5a"));
            var ale = abandon.GetComponent<LayoutElement>();
            if (ale != null) UnityEngine.Object.Destroy(ale);
            UiKit.Anchor(abandon.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(170f, 56f));

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
            float x = 60f + (n.Col ?? 0) * colW;
            float y = PadY + RowH * (row + 1) + RowH * 0.5f;
            return new Vector2(x, y);
        }

        static void Line(RectTransform parent, Vector2 a, Vector2 b, bool avail, bool taken, bool passed)
        {
            var img = UiKit.Pan(parent, Color.white, "edge");
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0.5f);
            Vector2 d = b - a;
            float len = d.magnitude;
            // ノードの縁から縁へ
            Vector2 dir = d / Math.Max(0.001f, len);
            float trim = NodeSize * 0.55f;
            rt.anchoredPosition = a + dir * trim;
            rt.sizeDelta = new Vector2(Math.Max(0f, len - trim * 2f), avail ? 6f : taken ? 5f : 3f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            img.color = avail ? new Color(0.55f, 0.95f, 0.55f, 0.95f)
                : taken ? new Color(0.95f, 0.8f, 0.35f, 0.9f)
                : passed ? new Color(1f, 1f, 1f, 0.12f)
                : new Color(1f, 1f, 1f, 0.32f);
        }

        static void Node(GameRoot g, RectTransform parent, MapNode n, int row, int index, bool cur, bool avail, bool taken, bool passed)
        {
            bool boss = n.Type == MapNodeTypes.Boss;
            float size = boss ? BossSize : NodeSize;
            var cell = UiKit.NewRect("node-" + row + "-" + index, parent);
            cell.anchorMin = cell.anchorMax = new Vector2(0f, 0f);
            cell.sizeDelta = new Vector2(size, size);
            cell.anchoredPosition = NodePos(n, row);

            if (avail)
            {
                var glow = UiKit.Pan(cell, new Color(0.55f, 0.95f, 0.55f, 0.35f), "glow");
                glow.raycastTarget = false;
                glow.sprite = ThemeFx.Shadow();
                UiKit.Anchor(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-size, -size), new Vector2(size, size));
            }

            var frame = UiKit.Frame(cell, Theme.Button, NodeTint(n.Type, avail, cur, taken, passed), "frame", 1f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            var icon = UiKit.Icon(cell, NodeIcon(n.Type), boss ? 56f : 34f);
            icon.raycastTarget = false;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = Vector2.zero;
            if (!avail && !cur) icon.color = new Color(1f, 1f, 1f, passed ? 0.45f : 0.8f);

            var lbl = UiKit.Txt(cell, NodeName(n.Type), 12, avail ? UiKit.ColText : UiKit.ColDim, TextAnchor.MiddleCenter);
            lbl.raycastTarget = false;
            UiKit.Anchor(lbl.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-60f, -22f), new Vector2(60f, -2f));
            if (passed && !cur) lbl.color = new Color(UiKit.ColDim.r, UiKit.ColDim.g, UiKit.ColDim.b, 0.5f);

            if (cur)
            {
                var ring = UiKit.Frame(cell, Theme.Panel, new Color(1f, 0.85f, 0.35f, 0.9f), "ring", 3f);
                ring.raycastTarget = false;
                UiKit.Anchor(ring.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(6f, 6f));
                ring.transform.SetAsFirstSibling();
            }

            string tip = NodeTip(n);
            Tooltip.Attach(cell.gameObject, delegate { return tip; });

            if (avail)
            {
                var btn = frame.gameObject.AddComponent<Button>();
                btn.targetGraphic = frame;
                var cols = btn.colors; cols.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cols.pressedColor = new Color(0.85f, 0.85f, 0.85f); btn.colors = cols;
                int idx = index;
                btn.onClick.AddListener(delegate
                {
                    Audio.Play("map", 0.8f);
                    g.Do(new RunCommand_ChooseNode { Col = idx });
                });
                Tween.Punch(cell, 0.1f, 0.8f);
            }
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
