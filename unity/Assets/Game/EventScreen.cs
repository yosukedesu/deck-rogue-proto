// EventScreen.cs — ?マスのイベント (M3・2026-09-07): 中央の羊皮紙風パネルに物語と選択肢
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class EventScreen
    {
        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            RewardScreen.Backdrop(g, root, "？");
            EventDef def = null;
            try { if (run.EventId != null) def = Content.GetEventDef(run.EventId); } catch (Exception) { }

            if (def == null)
            {
                RunUi.Heading(root, "イベント", "定義が見つかりません: " + run.EventId);
                return;
            }

            if (g.EventChoiceIndex >= 0 && g.EventChoiceIndex < def.Choices.Count)
            {
                var ch = def.Choices[g.EventChoiceIndex];
                RunUi.Heading(root, "「" + ch.Label + "」", "対象のカードを1枚選ぶ");
                var area = UiKit.NewRect("pick", root);
                UiKit.Anchor(area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-760f, 110f), new Vector2(760f, -(RunUi.TopH + 110f)));
                UiKit.Vert(area, 0, 0);
                int ci = g.EventChoiceIndex;
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return "これ"; }, null,
                    delegate (int i) { g.EventChoiceIndex = -1; g.Do(new RunCommand_EventChoice { Index = ci, CardIndex = i }); }, 500f);
                RunUi.BottomButton(root, "選び直す", delegate { g.EventChoiceIndex = -1; g.Rebuild(); }, 18, 220f, 50f);
                return;
            }

            int rowsH = 0;
            for (int i = 0; i < def.Choices.Count; i++) rowsH += (ChoiceHint(def.Choices[i]).Length > 0 ? 70 : 52) + 12;
            float panelH = 350f + rowsH + 40f;
            var panel = UiKit.Frame(root, Theme.Panel, Color.white, "event", 3f);
            UiKit.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -panelH / 2f), new Vector2(520f, panelH / 2f + 20f));
            var prt = panel.rectTransform;
            var icon = UiKit.Icon(prt, "question", 64f);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 1f);
            icon.rectTransform.anchoredPosition = new Vector2(60f, -60f);
            var name = UiKit.Txt(prt, def.Name, 34, UiKit.ColText, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(110f, -100f), new Vector2(-30f, -24f));
            var flavor = UiKit.Txt(prt, def.Flavor, 18, UiKit.ColText, TextAnchor.UpperLeft);
            flavor.textWrappingMode = TextWrappingModes.Normal;
            flavor.lineSpacing = 8f;
            UiKit.Anchor(flavor.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -330f), new Vector2(-40f, -120f));

            var list = UiKit.NewRect("choices", prt);
            UiKit.Anchor(list, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 30f), new Vector2(-40f, -350f));
            var vg = UiKit.Vert(list, 12, 0);
            vg.childForceExpandHeight = false;
            for (int i = 0; i < def.Choices.Count; i++)
            {
                int idx = i;
                var ch = def.Choices[i];
                bool needsCard = DeckRogue.Engine.Run.EventChoiceNeedsCard(ch);
                bool last = i == def.Choices.Count - 1;
                string hint = ChoiceHint(ch);
                var b = UiKit.Btn(list, (last ? "" : "▶ ") + ch.Label + (needsCard ? "  (デッキから1枚選ぶ)" : "") + (hint.Length > 0 ? "\n<size=14><color=#c8b878>" + hint + "</color></size>" : ""),
                    delegate
                    {
                        if (needsCard) { g.EventChoiceIndex = idx; g.Rebuild(); }
                        else { Audio.Play("card_play", 0.6f); g.Do(new RunCommand_EventChoice { Index = idx }); }
                    }, 18, true, last ? null : (Color?)UiKit.Hex("#dfe8dc"));
                UiKit.Le(b, -1f, hint.Length > 0 ? 70f : 52f, -1f, hint.Length > 0 ? 70f : 52f);
                var tx = b.GetComponentInChildren<TMP_Text>();
                if (tx != null) { tx.alignment = TextAlignmentOptions.Left; tx.margin = new Vector4(18f, 0f, 12f, 0f); }
            }
        }

        static string ChoiceHint(EventChoiceDef ch)
        {
            var parts = new List<string>();
            if (ch.Gold.HasValue) parts.Add((ch.Gold.Value >= 0 ? "+" : "") + ch.Gold.Value + "G");
            if (ch.RequireGold.HasValue) parts.Add("要" + ch.RequireGold.Value + "G");
            if (ch.Hp.HasValue) parts.Add("HP" + (ch.Hp.Value >= 0 ? "+" : "") + ch.Hp.Value);
            if (ch.MaxHp.HasValue) parts.Add("最大HP" + (ch.MaxHp.Value >= 0 ? "+" : "") + ch.MaxHp.Value);
            if (ch.HpRatio.HasValue) parts.Add("最大HP比" + ch.HpRatio.Value);
            if (ch.Wounds.HasValue) parts.Add("負傷" + ch.Wounds.Value);
            if (ch.Brands.HasValue) parts.Add("烙印" + ch.Brands.Value);
            if (ch.TimedCurses.HasValue) parts.Add("仮初の烙印" + ch.TimedCurses.Value);
            if (ch.AddRandomCards.HasValue) parts.Add("ランダムカード" + ch.AddRandomCards.Value + "枚");
            if (ch.Relic == true) parts.Add("レリック");
            if (ch.RemoveAllWounds == true) parts.Add("負傷を全て除去");
            if (ch.UpgradeRandomCards.HasValue) parts.Add("ランダムに" + ch.UpgradeRandomCards.Value + "枚鍛える");
            if (ch.Gamble != null) parts.Add("賭け");
            return parts.Count > 0 ? string.Join(" / ", parts.ToArray()) : "";
        }
    }
}
