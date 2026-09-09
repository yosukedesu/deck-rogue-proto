// RewardScreen.cs — カード報酬とレリック報酬 (M3・2026-09-07): 候補を大きなカードで並べ、クリックで取る
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
    public static class RewardScreen
    {
        public static void Reward(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            Backdrop(g, root, "報酬");
            RunUi.Heading(root, "カード報酬", "1枚選んでデッキに加える。見送ってもよい (デッキを薄く保つのも戦略)");

            var opts = run.RewardOptions;
            int n = opts != null ? opts.Count : 0;
            float scale = 1.25f;
            float cardW = CardView.W * scale, cardH = CardView.H * scale;
            float gap = 60f;
            float totalW = n * cardW + Math.Max(0, n - 1) * gap;
            float x0 = -totalW / 2f + cardW / 2f;
            if (n == 0)
            {
                var none = UiKit.Txt(root, "候補がありません", 22, UiKit.ColDim, TextAnchor.MiddleCenter);
                UiKit.Anchor(none.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -30f), new Vector2(0f, 30f));
            }
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                CardDef def = null;
                try { def = Content.GetCardDef(opts[i]); } catch (Exception) { }
                if (def == null) continue;
                var cell = UiKit.NewRect("reward" + i, root);
                cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0.5f);
                cell.sizeDelta = new Vector2(cardW, cardH + 70f);
                cell.anchoredPosition = new Vector2(x0 + i * (cardW + gap), 10f);
                var ci = new CardInstance { Uid = "reward" + i, Def = def };
                var cv = CardView.Build(cell, ci, null, true, false, "reward-card");
                cv.localScale = Vector3.one * scale;
                cv.anchoredPosition = new Vector2(0f, 35f);
                HoverRaise(cv, delegate { Audio.Play("card_play", 0.7f); g.Do(new RunCommand_PickReward { Index = idx }); });
                var b = UiKit.Btn(cell, "取る", delegate { Audio.Play("card_play", 0.7f); g.Do(new RunCommand_PickReward { Index = idx }); }, 18, true, UiKit.Hex("#cfeacc"));
                var le = b.GetComponent<LayoutElement>();
                if (le != null) UnityEngine.Object.Destroy(le);
                UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, 0f), new Vector2(90f, 48f));
                // 登場アニメ
                cell.localScale = Vector3.one * 0.85f;
                Tween.Scale(cell, Vector3.one, 0.35f + i * 0.08f);
            }

            RunUi.BottomButton(root, "見送る", delegate { g.Do(new RunCommand_SkipReward()); }, 18, 260f, 52f);
        }

        public static void Relic(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            Backdrop(g, root, "レリック");
            int left = run.RelicPicksLeft.HasValue ? run.RelicPicksLeft.Value : 1;
            RunUi.Heading(root, "レリック", left > 1 ? "あと " + left + " 個選べる" : "1個選んで持ち帰る。見送ってもよい");

            var opts = run.RelicOptions;
            int n = opts != null ? opts.Count : 0;
            float w = 360f, h = 300f, gap = 40f;
            float totalW = n * w + Math.Max(0, n - 1) * gap;
            float x0 = -totalW / 2f + w / 2f;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(opts[i]); } catch (Exception) { }
                var cell = RelicPanel(root, rd, opts[i], w, h);
                cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0.5f);
                cell.anchoredPosition = new Vector2(x0 + i * (w + gap), 20f);
                var b = UiKit.Btn(cell, "取る", delegate { Audio.Play("buff", 0.7f); g.Do(new RunCommand_PickRelic { Index = idx }); }, 18, true, UiKit.Hex("#f0d58a"));
                var le = b.GetComponent<LayoutElement>();
                if (le != null) UnityEngine.Object.Destroy(le);
                UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-100f, 16f), new Vector2(100f, 62f));
                cell.localScale = Vector3.one * 0.85f;
                Tween.Scale(cell, Vector3.one, 0.35f + i * 0.08f);
            }
            RunUi.BottomButton(root, "見送る", delegate { g.Do(new RunCommand_SkipRelic()); }, 18, 260f, 52f);
        }

        /// <summary>レリック1個のパネル (絵文字・名前・レア度・説明)</summary>
        public static RectTransform RelicPanel(Transform parent, RelicDef rd, string id, float w, float h)
        {
            var cell = UiKit.NewRect("relic-" + id, parent);
            cell.sizeDelta = new Vector2(w, h);
            var frame = UiKit.Frame(cell, Theme.Panel, Color.white, "frame", 3f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            var art = RunUi.RelicArt(cell, id, 84f);
            art.rectTransform.anchorMin = art.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            art.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            var name = UiKit.Deco(cell, rd != null ? rd.Name : id, 22, UiKit.ColInk, TextAnchor.MiddleCenter);
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -138f), new Vector2(-8f, -102f));
            string rar = rd != null ? CardText.RarityLabel(rd.Rarity) : "";
            var rt = UiKit.Txt(cell, rar, 13, UiKit.ColGoldInk, TextAnchor.MiddleCenter);
            UiKit.Anchor(rt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -160f), new Vector2(-8f, -138f));
            var desc = UiKit.Txt(cell, rd != null ? rd.Description : "", 15, UiKit.ColInk, TextAnchor.UpperCenter);
            desc.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(desc.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 70f), new Vector2(-16f, -166f));
            return cell;
        }

        public static void Backdrop(GameRoot g, RectTransform root, string title)
        {
            BattleScreen.BuildBackground(root, g.Rs.Act);
            var dim = UiKit.Pan(root, new Color(0f, 0f, 0f, 0.5f), "dim");
            dim.raycastTarget = false;
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            RunUi.TopBar(g, root, title);
            RunUi.Message(g, root);
        }

        /// <summary>ホバーで浮き上がり、クリックで onClick</summary>
        public static void HoverRaise(RectTransform rt, Action onClick)
        {
            var img = rt.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();
            if (img.sprite == null) img.color = new Color(0f, 0f, 0f, 0f);
            var trig = rt.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            Vector3 baseScale = rt.localScale;
            enter.callback.AddListener(delegate { Audio.Hover(); Tween.Scale(rt, baseScale * 1.06f, 0.15f, Ease.OutQuad); });
            trig.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(delegate { Tween.Scale(rt, baseScale, 0.15f, Ease.OutQuad); });
            trig.triggers.Add(exit);
            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(delegate { onClick(); });
            trig.triggers.Add(click);
        }
    }
}
