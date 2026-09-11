// WorkshopScreen.cs — 工房 (M3・2026-09-07): 左にデッキのグリッド、右に素材2枠と合成結果を常時表示 (ブラウザ版の追従パネルと同じ形)
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class WorkshopScreen
    {
        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            RewardScreen.Backdrop(g, root, "工房");
            int price = DeckRogue.Engine.Run.WorkshopFusePrice(run);
            RunUi.Heading(root, "工房", "同じ色のカード2枚を1枚に溶かす (" + price + "G)。素材は消え、合成札が入る");

            var marked = new List<int>();
            if (g.WorkshopA >= 0) marked.Add(g.WorkshopA);
            if (g.WorkshopB >= 0) marked.Add(g.WorkshopB);

            float deckTop = RunUi.SceneWindow(root, "workshop") ? RunUi.SceneBottom : RunUi.TopH + 110f;   // 情景の窓があればデッキをその下へ
            var area = UiKit.NewRect("deck", root);
            UiKit.Anchor(area, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 40f), new Vector2(-520f, -deckTop));
            UiKit.Vert(area, 0, 0);
            RunUi.CardGrid(g, area, run.Deck,
                delegate (int i, CardInstance c) { return marked.Contains(i) ? "外す" : "選ぶ"; },
                null,
                delegate (int i)
                {
                    if (g.WorkshopA == i) g.WorkshopA = -1;
                    else if (g.WorkshopB == i) g.WorkshopB = -1;
                    else if (g.WorkshopA < 0) g.WorkshopA = i;
                    else if (g.WorkshopB < 0) g.WorkshopB = i;
                    else { g.WorkshopA = g.WorkshopB; g.WorkshopB = i; }  // 3枚目は古い方と入れ替える
                    Audio.Play("card_set", 0.6f);
                    g.Rebuild();
                },
                500f, marked);

            // 右の追従パネル
            var side = UiKit.Frame(root, Theme.Panel, Color.white, "fuse", 3f);
            UiKit.Anchor(side.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-500f, 40f), new Vector2(-40f, -(RunUi.TopH + 20f)));
            var srt = side.rectTransform;

            CardInstance a = (g.WorkshopA >= 0 && g.WorkshopA < run.Deck.Count) ? run.Deck[g.WorkshopA] : null;
            CardInstance b = (g.WorkshopB >= 0 && g.WorkshopB < run.Deck.Count) ? run.Deck[g.WorkshopB] : null;

            var ht = UiKit.Txt(srt, "素材", 18, UiKit.ColInk, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(ht.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -46f), new Vector2(-20f, -16f));
            Slot(g, srt, a, "A", new Vector2(-105f, -170f), delegate { g.WorkshopA = -1; g.Rebuild(); });
            Slot(g, srt, b, "B", new Vector2(105f, -170f), delegate { g.WorkshopB = -1; g.Rebuild(); });
            var plus = UiKit.Txt(srt, "+", 40, UiKit.ColInkSoft, TextAnchor.MiddleCenter, true);
            UiKit.Anchor(plus.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-20f, -195f), new Vector2(20f, -145f));

            string blocked = null;
            CardDef fused = null;
            var notes = new List<string>();
            if (a != null && b != null)
            {
                try
                {
                    blocked = Fusion.FuseBlockReason(a, b);
                    if (blocked == null)
                    {
                        fused = Fusion.FuseCards(a, b);
                        var fn = Fusion.FusionNotes(a, b);
                        for (int i = 0; i < fn.Count; i++) notes.Add(fn[i]);
                    }
                }
                catch (Exception ex) { blocked = ex.Message; }
            }

            var arrow = UiKit.Txt(srt, "▼ 合成結果", 16, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
            UiKit.Anchor(arrow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -330f), new Vector2(0f, -300f));

            if (fused != null)
            {
                // 結果の札はパネルの高さに合わせて縮める (スマホの 831 高では 1.1倍だと合成ボタンに隠れた。2026-09-09)
                float rootH = root.rect.height > 0f ? root.rect.height : 1080f;
                float panelH = rootH - 40f - (RunUi.TopH + 20f);
                float rs = Mathf.Clamp((panelH - 340f - 156f) / CardView.H, 0.7f, 1.1f);
                var cell = UiKit.NewRect("result", srt);
                cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
                cell.sizeDelta = new Vector2(CardView.W * rs, CardView.H * rs);
                cell.anchoredPosition = new Vector2(0f, -340f - CardView.H * rs * 0.5f);
                var ci = new CardInstance { Uid = "fused", Def = fused };
                var cv = CardView.Build(cell, ci, null, true, true, "fused-card");
                cv.localScale = Vector3.one * rs;
                CardPopup.Attach(g, cv, ci, null, true);
                Tween.Punch(cell, 0.08f, 0.5f);
                if (notes.Count > 0)
                {
                    var nt = UiKit.Txt(srt, "注記: " + string.Join(" / ", notes.ToArray()), 12, UiKit.ColInkSoft, TextAnchor.UpperLeft);
                    nt.textWrappingMode = TextWrappingModes.Normal;
                    UiKit.Anchor(nt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -340f - CardView.H * rs - 70f), new Vector2(-20f, -340f - CardView.H * rs - 6f));
                }
            }
            else
            {
                var msg = UiKit.Txt(srt, blocked != null ? "合成できない: " + blocked : "デッキから2枚選ぶ", 16, blocked != null ? UiKit.ColBadInk : UiKit.ColInkSoft, TextAnchor.MiddleCenter);
                msg.textWrappingMode = TextWrappingModes.Normal;
                UiKit.Anchor(msg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -440f), new Vector2(-20f, -340f));
            }

            int ia = g.WorkshopA, ib = g.WorkshopB;
            bool can = a != null && b != null && blocked == null && run.Gold >= price;
            var fuse = UiKit.Btn(srt, "合成する  " + price + "G" + (run.Gold < price ? " (不足)" : ""), delegate { Audio.Play("buff", 0.9f); g.Do(new RunCommand_WorkshopFuse { IndexA = ia, IndexB = ib }); }, 20, can, UiKit.Hex("#f0d58a"));
            var fle = fuse.GetComponent<LayoutElement>();
            if (fle != null) UnityEngine.Object.Destroy(fle);
            UiKit.Anchor(fuse.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 80f), new Vector2(-20f, 136f));
            var skip = UiKit.Btn(srt, "見送る", delegate { g.Do(new RunCommand_WorkshopSkip()); }, 16);
            var sle = skip.GetComponent<LayoutElement>();
            if (sle != null) UnityEngine.Object.Destroy(sle);
            UiKit.Anchor(skip.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 24f), new Vector2(-20f, 68f));
        }

        static void Slot(GameRoot g, RectTransform parent, CardInstance c, string label, Vector2 pos, Action onClear)
        {
            float w = CardView.W * 0.62f, h = CardView.H * 0.62f;
            var cell = UiKit.NewRect("slot" + label, parent);
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
            cell.sizeDelta = new Vector2(w, h);
            cell.anchoredPosition = pos;
            if (c == null)
            {
                var frame = UiKit.Frame(cell, Theme.Panel, new Color(1f, 1f, 1f, 0.5f), "empty", 3f);
                UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
                var t = UiKit.Txt(cell, label + "\n(空)", 16, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
                UiKit.Stretch(t.rectTransform, 0f, 0f, 0f, 0f);
                return;
            }
            var cv = CardView.Build(cell, c, null, true, true, "slot-card");
            CardPopup.Attach(g, cv, c, null, true);
            cv.localScale = Vector3.one * 0.62f;
            var x = UiKit.Btn(cell, "×", delegate { Audio.Play("click", 0.5f); onClear(); }, 14, true, UiKit.Hex("#8a5a5a"));
            var le = x.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(x.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(2f, 2f));
        }
    }
}
