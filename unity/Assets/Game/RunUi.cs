// RunUi.cs — 戦闘以外の画面で共有する部品 (2026-09-07 M3): 上部バー・デッキ一覧・レリック帯・見出し
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class RunUi
    {
        public const float TopH = 72f;

        /// <summary>ランの上部バー: 幕/行・HP・ゴールド・デッキ (押すと一覧)・レリック (ホバーで説明)</summary>
        public static void TopBar(GameRoot g, RectTransform root, string title)
        {
            var run = g.Rs;
            var bar = UiKit.Pan(root, new Color(0f, 0f, 0f, 0.55f), "topbar");
            UiKit.Anchor(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -TopH), new Vector2(0f, 0f));
            bar.raycastTarget = false;
            var left = UiKit.Txt(bar.transform, title + "   幕" + run.Act + "  行 " + (run.Row + 1) + " / " + run.Map.Count, 22, UiKit.ColText, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(left.rectTransform, new Vector2(0f, 0f), new Vector2(0.4f, 1f), new Vector2(24f, 0f), new Vector2(0f, 0f));

            var mid = UiKit.NewRect("res", bar.transform);
            UiKit.Anchor(mid, new Vector2(0.38f, 0f), new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero);
            var hg = UiKit.Horz(mid, 18, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandHeight = false;
            Chip(mid, "heart", run.Hp + " / " + run.MaxHp, UiKit.ColHp, 24);
            Chip(mid, "gold", run.Gold + " G", Theme.Gold, 22);
            var deckBtn = UiKit.Btn(mid, "デッキ " + run.Deck.Count, delegate { g.ViewDeck = !g.ViewDeck; g.Rebuild(); }, 16);
            BattleScreen.SetSize(deckBtn, 150f, 44f);

            var right = UiKit.NewRect("relics", bar.transform);
            UiKit.Anchor(right, new Vector2(0.7f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-16f, 0f));
            var rg = UiKit.Horz(right, 6, 0);
            rg.childAlignment = TextAnchor.MiddleRight;
            rg.childForceExpandHeight = false;
            rg.childForceExpandWidth = false;
            if (run.Relics.Count == 0)
            {
                var none = UiKit.Txt(right, "レリックなし", 15, UiKit.ColDim, TextAnchor.MiddleRight);
                UiKit.Le(none, 120f, 30f, -1f, 30f);
            }
            for (int i = 0; i < run.Relics.Count && i < 12; i++)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(run.Relics[i]); } catch (Exception) { }
                var cell = UiKit.NewRect("relic", right);
                UiKit.Le(cell, 48f, 48f, 48f, 48f);
                var bg = cell.gameObject.AddComponent<Image>();
                bg.sprite = Theme.Panel;
                bg.type = Image.Type.Sliced;
                bg.pixelsPerUnitMultiplier = 1f / 3f;
                bg.color = Color.white;
                RelicArt(cell, run.Relics[i], 36f);
                var tip = rd != null ? "<b>" + rd.Name + "</b>\n" + rd.Description : run.Relics[i];
                Tooltip.Attach(cell.gameObject, delegate { return tip; });
            }
        }

        /// <summary>エンジンの拒否理由・通知を画面上部に1行 (無ければ何も置かない)</summary>
        public static void Message(GameRoot g, RectTransform root)
        {
            string msg = g.Error != null ? "! " + g.Error : g.Notice;
            if (string.IsNullOrEmpty(msg)) return;
            var pan = UiKit.Frame(root, Theme.Panel, g.Error != null ? new Color(1f, 0.7f, 0.7f, 1f) : Color.white, "message", 3f);
            UiKit.Anchor(pan.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-460f, -TopH - 56f), new Vector2(460f, -TopH - 8f));
            pan.raycastTarget = false;
            var t = UiKit.Txt(pan.transform, msg, 16, g.Error != null ? UiKit.ColBad : UiKit.ColText, TextAnchor.MiddleCenter);
            UiKit.Stretch(t.rectTransform, 12f, 12f, 0f, 0f);
            t.raycastTarget = false;
        }

        /// <summary>レリックの絵 (プレースホルダー: idから生成した紋様。PixelLab の絵が Resources/Art/relics/&lt;id&gt; にあればそれ)</summary>
        public static Image RelicArt(Transform parent, string relicId, float size)
        {
            var img = new GameObject("relic-art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.raycastTarget = false;
            var art = Theme.Art("relics", relicId);
            img.sprite = art != null ? art : ThemeFx.RelicGlyph(relicId);
            img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }

        /// <summary>画面下部の固定位置ボタン (レイアウトに属さない)。x は中心からのずれ</summary>
        public static Button BottomButton(RectTransform root, string label, Action onClick, int size, float w, float h, float x = 0f, float bottom = 40f, Color? bg = null, bool enabled = true)
        {
            var b = UiKit.Btn(root, label, onClick, size, enabled, bg);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x - w / 2f, bottom), new Vector2(x + w / 2f, bottom + h));
            return b;
        }

        public static void Chip(Transform parent, string icon, string text, Color color, int size)
        {
            var row = UiKit.NewRect("chip-" + icon, parent);
            var hg = UiKit.Horz(row, 6, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            UiKit.Icon(row, icon, 32);
            var t = UiKit.Txt(row, text, size, color, TextAnchor.MiddleLeft, true);
            UiKit.Le(t, 40f, size + 8f, -1f, size + 8f);
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>デッキ一覧 (カードのグリッド)。onPick があればカードの下にボタン</summary>
        public static void DeckViewer(GameRoot g, RectTransform root)
        {
            var inner = BattleScreen.Modal(root, 1500f, 820f, "deckViewer");
            UiKit.Head(inner, "デッキ " + g.Rs.Deck.Count + "枚", 24);
            CardGrid(g, inner, g.Rs.Deck, null, null, null, 360f);
            BattleScreen.CenteredButton(inner, "閉じる", delegate { g.ViewDeck = false; g.Rebuild(); }, 18, 260f, 50f);
        }

        /// <summary>カードのグリッド (スクロール)。btnLabel が null を返す札はボタンなし。marked は強調</summary>
        public static void CardGrid(GameRoot g, Transform parent, IReadOnlyList<CardInstance> cards,
            Func<int, CardInstance, string> btnLabel, Func<int, CardInstance, bool> btnEnabled, Action<int> onPick, float minH, List<int> marked = null)
        {
            var content = UiKit.Scroll(parent, true, new Color(0f, 0f, 0f, 0.25f), 12, 12);
            UiKit.Le(UiKit.ScrollRoot(content), -1f, minH, -1f, minH, -1f, 1f);
            var vg = content.GetComponent<VerticalLayoutGroup>();
            if (vg != null) UnityEngine.Object.DestroyImmediate(vg);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            bool withBtn = btnLabel != null;
            grid.cellSize = new Vector2(CardView.W * 0.8f, CardView.H * 0.8f + (withBtn ? 48f : 0f));
            grid.spacing = new Vector2(14f, 14f);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.childAlignment = TextAnchor.UpperLeft;
            if (cards.Count == 0)
            {
                var none = UiKit.Txt(parent, "（空）", 18, UiKit.ColDim, TextAnchor.MiddleCenter);
                UiKit.Le(none, -1f, 40f, -1f, 40f);
            }
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                int idx = i;
                bool mark = marked != null && marked.Contains(i);
                var cell = UiKit.NewRect("cell", content);
                var cv = CardView.Build(cell, c, g.Rs.Combat, true, false, "deck-card");
                cv.localScale = Vector3.one * 0.8f;
                if (withBtn) cv.anchoredPosition = new Vector2(0f, 24f);
                if (mark)
                {
                    var ring = UiKit.Frame(cell, Theme.Panel, new Color(1f, 0.85f, 0.3f, 0.6f), "mark", 3f);
                    UiKit.Anchor(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-CardView.W * 0.4f - 8f, -CardView.H * 0.4f - 8f + (withBtn ? 24f : 0f)), new Vector2(CardView.W * 0.4f + 8f, CardView.H * 0.4f + 8f + (withBtn ? 24f : 0f)));
                    ring.raycastTarget = false;
                    ring.transform.SetAsFirstSibling();
                }
                if (withBtn)
                {
                    string label = btnLabel(i, c);
                    if (label != null)
                    {
                        bool en = btnEnabled == null || btnEnabled(i, c);
                        var b = UiKit.Btn(cell, label, delegate { if (onPick != null) onPick(idx); }, 15, en, mark ? UiKit.Hex("#cfeacc") : Color.white);
                        var le = b.GetComponent<LayoutElement>();
                        if (le != null) UnityEngine.Object.Destroy(le);
                        UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 0f), new Vector2(70f, 42f));
                    }
                }
            }
        }

        /// <summary>画面の見出し (大きな題と小さな説明)</summary>
        public static void Heading(RectTransform root, string title, string sub, float y = TopH + 24f)
        {
            var t = UiKit.Txt(root, title, 36, UiKit.ColText, TextAnchor.MiddleCenter, true);
            t.outlineWidth = 0.2f; t.outlineColor = Color.black;
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -y - 50f), new Vector2(0f, -y));
            if (!string.IsNullOrEmpty(sub))
            {
                var s = UiKit.Txt(root, sub, 17, UiKit.ColDim, TextAnchor.MiddleCenter);
                UiKit.Anchor(s.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -y - 80f), new Vector2(0f, -y - 50f));
            }
        }

        public static string[] ColorsJa(IReadOnlyList<string> colors)
        {
            var outp = new string[colors.Count];
            for (int i = 0; i < colors.Count; i++)
            {
                string c = colors[i];
                outp[i] = c == "green" ? "緑" : c == "blue" ? "青" : c == "red" ? "赤" : c == "white" ? "白" : c == "black" ? "黒" : c;
            }
            return outp;
        }
    }
}
