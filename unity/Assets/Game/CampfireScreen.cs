// CampfireScreen.cs — 焚き火 (M3・2026-09-07): 「休む」と「鍛える」の二択を大きな札で。鍛えるはデッキのグリッドから選ぶ
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class CampfireScreen
    {
        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            RewardScreen.Backdrop(g, root, "焚き火");

            int allowed = DeckRogue.Engine.Run.CampfireForgeAllowed(run);
            int remain = Math.Max(0, allowed - run.CampfireUpgradesUsed);
            int heal = (int)Math.Floor(run.MaxHp * run.CampfireRatio);
            bool noRest = false;
            for (int i = 0; i < run.Relics.Count; i++)
            {
                try { var rd = Content.GetRelicDef(run.Relics[i]); if (rd.Bonus != null && rd.Bonus.NoRest == true) noRest = true; }
                catch (Exception) { }
            }
            bool restHeals = !noRest && run.CampfireUpgradesUsed == 0;

            if (g.SubMode == "forge")
            {
                RunUi.Heading(root, "鍛える", "1枚選ぶ。カードにカーソルを重ねると鍛えた後の姿");
                var area = UiKit.NewRect("forge", root);
                UiKit.Anchor(area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-760f, 110f), new Vector2(760f, -(RunUi.TopH + 110f)));
                UiKit.Vert(area, 0, 0);
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return Upgrade.CanUpgradeCard(c) ? "鍛える" : null; },
                    delegate (int i, CardInstance c) { return Upgrade.CanUpgradeCard(c); },
                    delegate (int i) { Audio.Play("buff", 0.8f); g.Do(new RunCommand_CampfireUpgrade { Index = i }); },
                    500f);
                AttachUpgradeTips(area, run.Deck);
                RunUi.BottomButton(root, "戻る", delegate { g.SubMode = null; g.Rebuild(); }, 18, 220f, 50f);
                return;
            }

            RunUi.Heading(root, "焚き火", "どちらか1つ。鍛えた後は回復なしで立ち去る");

            // 休む
            var rest = Option(root, "burn", "休む", restHeals ? "HP +" + heal + " (最大HPの " + (int)Math.Round(run.CampfireRatio * 100) + "%)" : noRest ? "レリックの効果で回復できない" : "すでに鍛えたので回復なし",
                restHeals ? "今のHP " + run.Hp + " → " + Math.Min(run.MaxHp, run.Hp + heal) : "立ち去る",
                new Vector2(-260f, 0f), delegate { Audio.Play("heal", 0.8f); g.Do(new RunCommand_CampfireRest()); }, true);
            // 鍛える
            Option(root, "hammer", "鍛える", remain > 0 ? "デッキの1枚を強化 (残り " + remain + " 回)" : "この焚き火ではもう鍛えられない",
                remain > 0 ? "鍛えると数値が伸びる・コストが下がる" : "", new Vector2(260f, 0f),
                delegate { g.SubMode = "forge"; g.Rebuild(); }, remain > 0);
        }

        static RectTransform Option(RectTransform root, string icon, string title, string line1, string line2, Vector2 pos, Action onClick, bool enabled)
        {
            float w = 440f, h = 360f;
            var cell = UiKit.NewRect("opt-" + title, root);
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0.5f);
            cell.sizeDelta = new Vector2(w, h);
            cell.anchoredPosition = pos;
            var frame = UiKit.Frame(cell, Theme.Panel, enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f), "frame", 3f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            var ic = UiKit.Icon(cell, icon, 96f);
            ic.raycastTarget = false;
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            ic.rectTransform.anchoredPosition = new Vector2(0f, -80f);
            var t = UiKit.Deco(cell, title, 30, UiKit.ColInk, TextAnchor.MiddleCenter);
            t.raycastTarget = false;
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -190f), new Vector2(0f, -140f));
            var l1 = UiKit.Txt(cell, line1, 17, UiKit.ColInk, TextAnchor.MiddleCenter, true);
            l1.raycastTarget = false;
            l1.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(l1.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -240f), new Vector2(-16f, -192f));
            var l2 = UiKit.Txt(cell, line2, 14, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
            l2.raycastTarget = false;
            UiKit.Anchor(l2.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -280f), new Vector2(-16f, -242f));
            if (enabled)
            {
                var btn = frame.gameObject.AddComponent<Button>();
                btn.targetGraphic = frame;
                var cols = btn.colors; cols.highlightedColor = new Color(1.1f, 1.1f, 1.1f); cols.pressedColor = new Color(0.85f, 0.85f, 0.85f); btn.colors = cols;
                btn.onClick.AddListener(delegate { Audio.Play("click", 0.5f); onClick(); });
                var hintB = UiKit.Txt(cell, "クリックで選ぶ", 13, Theme.Gold, TextAnchor.MiddleCenter);
                hintB.raycastTarget = false;
                UiKit.Anchor(hintB.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(0f, 44f));
            }
            return cell;
        }

        /// <summary>グリッドの各カードに「鍛えると→」のツールチップ</summary>
        public static void AttachUpgradeTips(RectTransform area, IReadOnlyList<CardInstance> deck)
        {
            var cards = area.GetComponentsInChildren<RectTransform>(true);
            int k = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].name != "cell") continue;
                if (k >= deck.Count) break;
                var c = deck[k++];
                string tip;
                if (Upgrade.CanUpgradeCard(c))
                {
                    try
                    {
                        var up = Upgrade.UpgradeCard(c);
                        tip = "<b>鍛えると →</b> [" + CardText.CostLabel(up.Def) + "] " + up.Def.Name + "\n" + CardText.Body(up.Def);
                    }
                    catch (Exception ex) { tip = "鍛えられない: " + ex.Message; }
                }
                else tip = Upgrade.IsUpgraded(c) ? "鍛え済み" : "この札は鍛えられない";
                Tooltip.Attach(cards[i].gameObject, delegate { return tip; });
            }
        }
    }
}
