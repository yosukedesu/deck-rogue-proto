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
                // スマホは幅いっぱい (2026-09-15 ユーザー「カード一覧の左が切れてマナコストが見えない」: PC 用の幅 1520 がキャンバス 1462 からはみ出していた)
                if (UiKit.Phone) UiKit.Anchor(area, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 90f), new Vector2(-24f, -(RunUi.TopH + 100f)));
                else UiKit.Anchor(area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-760f, 110f), new Vector2(760f, -(RunUi.TopH + 110f)));
                UiKit.Vert(area, 0, 0);
                int ci = g.EventChoiceIndex;
                // 鍛えられない札・5枚以下のデッキの除去は選べない (engine が拒む手を押せないようにする 2026-09-14)
                bool isUpgrade = ch.UpgradeCard == true;
                bool canRemove = ch.RemoveCard != true || run.Deck.Count > 5;
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return isUpgrade && !DeckRogue.Engine.Upgrade.CanUpgradeCard(c) ? "鍛えられない" : "これ"; },
                    delegate (int i, CardInstance c) { return canRemove && (!isUpgrade || DeckRogue.Engine.Upgrade.CanUpgradeCard(c)); },
                    delegate (int i) { g.EventChoiceIndex = -1; g.Do(new RunCommand_EventChoice { Index = ci, CardIndex = i }); }, 500f);
                RunUi.BottomButton(root, "選び直す", delegate { g.EventChoiceIndex = -1; g.Rebuild(); }, 18, 220f, 50f, 0f, UiKit.Phone ? 24f : 40f);
                return;
            }

            bool ph = UiKit.Phone;
            int rowsH = 0;
            for (int i = 0; i < def.Choices.Count; i++) rowsH += (ChoiceHint(def.Choices[i]).Length > 0 ? 70 : 52) + 12;
            float panelH = 350f + rowsH + 40f;
            var panel = UiKit.Frame(root, Theme.Panel, Color.white, "event", 3f);
            var cs = BattleScreen.CanvasSize(root);
            // 情景の絵 (Art/scenes/event.png 240×135 を 2倍) があれば挿絵つきの頁に: 左に絵、右に名前と本文 (2026-09-11)
            var sceneArt = Theme.Art("scenes", "event");
            float pad = ph ? 28f : 40f;
            float textX = pad;
            // スマホ (2026-09-15 ユーザー「イベントのダイアログで上部ステータスが見えない」): パネルを上部バーの下に全幅で置く。
            // 絵は左、名前・本文・選択肢は右の列で、高さは中身に合わせる = HP/G を見たまま選べる (旧: 高さ 636 の中央パネルが高さ 675 の画面でバーを覆っていた)
            float phColX = 0f, phFlavorH = 0f;
            if (ph)
            {
                phColX = sceneArt != null ? pad + 480f + 24f : 110f;
                float colW = cs.x - 48f - phColX - 28f;
                int cpl = Mathf.Max(10, (int)((colW - 8f) / 17f));
                int lines = Mathf.Max(1, Mathf.CeilToInt((def.Flavor ?? "").Length / (float)cpl));
                phFlavorH = lines * 25f + 6f;
                float choicesH = 0f;
                for (int i = 0; i < def.Choices.Count; i++) choicesH += (ChoiceHint(def.Choices[i]).Length > 0 ? 62f : 50f) + (i > 0 ? 8f : 0f);
                float colH = 22f + 44f + 6f + phFlavorH + 10f + choicesH + 24f;
                float artH = sceneArt != null ? pad + 270f + pad : 0f;
                float avail = cs.y - RunUi.TopH - 10f - 16f;
                float h = Mathf.Min(avail, Mathf.Max(colH, artH));
                float top = RunUi.TopH + 10f + Mathf.Max(0f, (avail - h) / 2f);   // 余った高さの中央に
                UiKit.Anchor(panel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -top - h), new Vector2(-24f, -top));
            }
            else UiKit.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -panelH / 2f), new Vector2(520f, panelH / 2f + 20f));
            var prt = panel.rectTransform;
            if (sceneArt != null)
            {
                var sc = new GameObject("scene", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                sc.transform.SetParent(prt, false);
                sc.sprite = sceneArt; sc.preserveAspect = true; sc.raycastTarget = false;
                UiKit.Anchor(sc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(pad, -pad - 270f), new Vector2(pad + 480f, -pad));
                textX = pad + 480f + (ph ? 24f : 30f);
            }
            else
            {
                var icon = UiKit.Icon(prt, "question", 64f);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(60f, -60f);
            }
            var name = UiKit.Deco(prt, def.Name, ph ? 30 : 34, UiKit.ColInk, TextAnchor.MiddleLeft);
            var flavor = UiKit.Txt(prt, def.Flavor, ph ? 17 : 18, UiKit.ColInk, TextAnchor.UpperLeft);
            flavor.textWrappingMode = TextWrappingModes.Normal;
            flavor.lineSpacing = 8f;
            var list = UiKit.NewRect("choices", prt);
            if (ph)
            {
                float colX = phColX, flavorH = phFlavorH;
                UiKit.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(colX, -66f), new Vector2(-28f, -22f));
                UiKit.Anchor(flavor.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(colX, -72f - flavorH), new Vector2(-28f, -72f));
                UiKit.Anchor(list, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(colX, 24f), new Vector2(-28f, -72f - flavorH - 10f));
            }
            else
            {
                UiKit.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(sceneArt != null ? textX : 110f, -100f), new Vector2(-30f, -24f));
                UiKit.Anchor(flavor.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textX, -330f), new Vector2(-40f, -120f));
                UiKit.Anchor(list, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 30f), new Vector2(-40f, -350f));
            }
            var vg = UiKit.Vert(list, ph ? 8 : 12, 0);
            vg.childForceExpandHeight = false;
            for (int i = 0; i < def.Choices.Count; i++)
            {
                int idx = i;
                var ch = def.Choices[i];
                bool needsCard = DeckRogue.Engine.Run.EventChoiceNeedsCard(ch);
                // 無料の「立ち去る」(取引型だけに残る。2026-09-14) は ▶ も色も付けない。それ以外は全部が本物の選択肢
                bool leave = IsFreeChoice(ch);
                // 所持金が足りない・対象カードが無い選択肢は押せない (engine と同じ判定 EventChoiceAvailable)
                bool available = DeckRogue.Engine.Run.EventChoiceAvailable(run, ch);
                string why = available ? "" : (ch.RequireGold.HasValue && run.Gold < ch.RequireGold.Value ? "  (G不足)" : "  (対象がない)");
                string hint = ChoiceHint(ch);
                var b = UiKit.Btn(list, (leave ? "" : "▶ ") + ch.Label + (needsCard ? "  (デッキから1枚選ぶ)" : "") + why + (hint.Length > 0 ? "\n<size=14><color=#7a4e12>" + hint + "</color></size>" : ""),
                    delegate
                    {
                        if (needsCard) { g.EventChoiceIndex = idx; g.Rebuild(); }
                        else { Audio.Ui("event_choice"); g.Do(new RunCommand_EventChoice { Index = idx }); }
                    }, 18, available, leave ? null : (Color?)UiKit.Hex("#dfe8dc"));
                float bh = hint.Length > 0 ? (ph ? 62f : 70f) : (ph ? 50f : 52f);
                UiKit.Le(b, -1f, bh, -1f, bh);
                var tx = b.GetComponentInChildren<TMP_Text>();
                if (tx != null) { tx.alignment = TextAlignmentOptions.Left; tx.margin = new Vector4(18f, 0f, 12f, 0f); }
            }
        }

        /// <summary>効果を何も持たない選択肢 (無料の「立ち去る」)。取引型のイベントだけが最後に持つ</summary>
        static bool IsFreeChoice(EventChoiceDef ch)
        {
            return !ch.Gold.HasValue && !ch.RequireGold.HasValue && !ch.Hp.HasValue && !ch.MaxHp.HasValue && !ch.HpRatio.HasValue
                && !ch.Wounds.HasValue && !ch.Brands.HasValue && !ch.TimedCurses.HasValue && !ch.AddRandomCards.HasValue
                && ch.Relic != true && ch.RemoveCard != true && ch.UpgradeCard != true && ch.TransformCard != true
                && ch.DuplicateCard != true && ch.RemoveAllWounds != true && !ch.UpgradeRandomCards.HasValue && ch.Gamble == null;
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
