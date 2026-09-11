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
            // レリック限定の第3選択肢 (2026-09-12 本家形): 発掘 (鶴嘴) / 鍛錬 (重石) / 取り除く (煙管)。融合の鎚は鍛えられない
            var opt = DeckRogue.Engine.Run.CampfireOptions(run);
            bool fresh = run.CampfireUpgradesUsed == 0;
            if (!opt.Forge) remain = 0;

            if (g.SubMode == "remove")
            {
                RunUi.Heading(root, "取り除く", "安らぎの煙管: デッキの1枚を永久に取り除く (休む/鍛えるとは排他)");
                var area = UiKit.NewRect("remove", root);
                UiKit.Anchor(area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-760f, 110f), new Vector2(760f, -(RunUi.TopH + 110f)));
                UiKit.Vert(area, 0, 0);
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return "取り除く"; },
                    delegate (int i, CardInstance c) { return run.Deck.Count > 5; },
                    delegate (int i) { Audio.Play("card_play", 0.7f); g.Do(new RunCommand_CampfireRemove { Index = i }); },
                    400f);
                RunUi.BottomButton(root, "戻る", delegate { g.SubMode = null; g.Rebuild(); }, 18, 220f, 50f);
                return;
            }

            if (g.SubMode == "forge")
            {
                RunUi.Heading(root, "鍛える", "1枚選ぶ。札に触れると元と鍛えた後が並ぶ（長押しで拡大）");
                var preview = ForgePreviewArea(root);
                var area = UiKit.NewRect("forge", root);
                UiKit.Anchor(area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-760f, 110f), new Vector2(760f, -(RunUi.TopH + 110f + ForgePreviewH)));
                UiKit.Vert(area, 0, 0);
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return Upgrade.CanUpgradeCard(c) ? "鍛える" : null; },
                    delegate (int i, CardInstance c) { return Upgrade.CanUpgradeCard(c); },
                    delegate (int i) { Audio.Play("buff", 0.8f); g.Do(new RunCommand_CampfireUpgrade { Index = i }); },
                    400f);
                AttachUpgradeTips(area, run.Deck);
                AttachForgePreview(g, area, run.Deck, preview);
                RunUi.BottomButton(root, "戻る", delegate { g.SubMode = null; g.Rebuild(); }, 18, 220f, 50f);
                return;
            }

            int extra = (opt.Dig && fresh ? 1 : 0) + (opt.TrainLeft > 0 && fresh ? 1 : 0) + (opt.Remove && fresh ? 1 : 0);
            RunUi.Heading(root, "焚き火", extra > 0 ? "どれか1つ。鍛えた後は回復なしで立ち去る" : "どちらか1つ。鍛えた後は回復なしで立ち去る");
            float optY = RunUi.SceneWindow(root, "campfire") ? -40f : 0f;   // 情景の窓 (左上) と札が触れないよう少し下げる
            // 選択肢が3つ以上なら幅を詰めて横に並べる (レリックの第3選択肢 2026-09-12)
            int count = 2 + extra;
            float w = count <= 2 ? 440f : count == 3 ? 400f : 320f;
            float gap = count <= 2 ? 80f : 24f;
            float x0 = -((count - 1) * (w + gap)) / 2f;
            int slot = 0;
            Vector2 Pos() { return new Vector2(x0 + (slot++) * (w + gap), optY); }

            // 休む
            var rest = Option(root, "burn", "休む", restHeals ? "HP +" + heal + " (最大HPの " + (int)Math.Round(run.CampfireRatio * 100) + "%)" : noRest ? "レリックの効果で回復できない" : "すでに鍛えたので回復なし",
                restHeals ? "今のHP " + run.Hp + " → " + Math.Min(run.MaxHp, run.Hp + heal) : "立ち去る",
                Pos(), delegate { Audio.Play("heal", 0.8f); g.Do(new RunCommand_CampfireRest()); }, true, w);
            // 鍛える
            Option(root, "hammer", "鍛える", !opt.Forge ? "融合の鎚: 焚き火では鍛えられない" : remain > 0 ? "デッキの1枚を強化 (残り " + remain + " 回)" : "この焚き火ではもう鍛えられない",
                remain > 0 ? "鍛えると数値が伸びる・コストが下がる" : "", Pos(),
                delegate { g.SubMode = "forge"; g.Rebuild(); }, remain > 0, w);
            if (opt.Dig && fresh)
                Option(root, "chest", "発掘", "発掘の鶴嘴: レリックを1個掘る", "休む・鍛えるとは排他", Pos(),
                    delegate { Audio.Play("buff", 0.8f); g.Do(new RunCommand_CampfireDig()); }, true, w);
            if (opt.TrainLeft > 0 && fresh)
                Option(root, "growth", "鍛錬", "重石: 以後の戦闘開始時の成長+1", "現在 +" + DeckRogue.Engine.Run.RelicStateOf(run, "train") + "・あと " + opt.TrainLeft + " 回", Pos(),
                    delegate { Audio.Play("buff", 0.8f); g.Do(new RunCommand_CampfireTrain()); }, true, w);
            if (opt.Remove && fresh)
                Option(root, "skull", "取り除く", "安らぎの煙管: デッキの1枚を永久に除去", "休む・鍛えるとは排他", Pos(),
                    delegate { g.SubMode = "remove"; g.Rebuild(); }, run.Deck.Count > 5, w);
        }

        static RectTransform Option(RectTransform root, string icon, string title, string line1, string line2, Vector2 pos, Action onClick, bool enabled, float w = 440f)
        {
            float h = 360f;
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
                var hintB = UiKit.Txt(cell, "クリックで選ぶ", 13, UiKit.ColGoldInk, TextAnchor.MiddleCenter);
                hintB.raycastTarget = false;
                UiKit.Anchor(hintB.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(0f, 44f));
            }
            return cell;
        }

        /// <summary>本家 Smith の「元 → 鍛えた後」の並び (2026-09-09): 見出しの下に横 800×高 ForgePreviewH の場所を取り、札に触れると2枚を並べる</summary>
        public const float ForgePreviewH = 262f;
        public static RectTransform LastPreviewArea;

        public static RectTransform ForgePreviewArea(RectTransform root)
        {
            var area = UiKit.NewRect("forgePreview", root);
            UiKit.Anchor(area, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -(RunUi.TopH + 100f + ForgePreviewH)), new Vector2(420f, -(RunUi.TopH + 100f)));
            var hint = UiKit.Txt(area, "札に触れると、元の札と鍛えた後の札がここに並ぶ", 15, UiKit.ColDim, TextAnchor.MiddleCenter);
            hint.outlineWidth = 0.3f; hint.outlineColor = new Color(0.05f, 0.03f, 0.06f, 0.95f);
            UiKit.Stretch(hint.rectTransform, 0f, 0f, 0f, 0f);
            LastPreviewArea = area;
            return area;
        }

        public static void ShowForgePair(GameRoot g, RectTransform area, CardInstance c)
        {
            if (area == null || c == null) return;
            for (int i = area.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(area.GetChild(i).gameObject);
            const float sc = 0.85f;
            float w = CardView.W * sc, h = CardView.H * sc;
            var left = UiKit.NewRect("orig", area);
            left.anchorMin = left.anchorMax = new Vector2(0.5f, 0.5f);
            left.sizeDelta = new Vector2(w, h);
            left.anchoredPosition = new Vector2(-w / 2f - 56f, 0f);
            var cvA = CardView.Build(left, c, null, true, false, "orig-card");
            cvA.localScale = Vector3.one * sc;
            var arrow = UiKit.Deco(area, "→", 44, UiKit.ColText, TextAnchor.MiddleCenter);
            arrow.outlineWidth = 0.25f; arrow.outlineColor = new Color(0.05f, 0.03f, 0.06f, 0.95f);
            UiKit.Anchor(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40f, -30f), new Vector2(40f, 30f));
            CardInstance up = null;
            try { if (Upgrade.CanUpgradeCard(c)) up = Upgrade.UpgradeCard(c); } catch (Exception) { }
            if (up != null)
            {
                var right = UiKit.NewRect("upgraded", area);
                right.anchorMin = right.anchorMax = new Vector2(0.5f, 0.5f);
                right.sizeDelta = new Vector2(w, h);
                right.anchoredPosition = new Vector2(w / 2f + 56f, 0f);
                var cvB = CardView.Build(right, up, null, true, false, "upgraded-card");
                cvB.localScale = Vector3.one * sc;
                Tween.Punch(right, 0.06f, 0.4f);
            }
            else
            {
                bool upg = false;
                try { upg = Upgrade.IsUpgraded(c); } catch (Exception) { }
                var note = UiKit.Txt(area, upg ? "鍛え済み" : "この札は鍛えられない", 17, UiKit.ColDim, TextAnchor.MiddleCenter);
                note.outlineWidth = 0.3f; note.outlineColor = new Color(0.05f, 0.03f, 0.06f, 0.95f);
                UiKit.Anchor(note.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f, -30f), new Vector2(56f + w, 30f));
            }
        }

        /// <summary>グリッドの各 cell に、触れる (PC はホバー・スマホはタップ) と ShowForgePair する挙動を付ける</summary>
        public static void AttachForgePreview(GameRoot g, RectTransform gridArea, IReadOnlyList<CardInstance> deck, RectTransform previewArea)
        {
            var cells = gridArea.GetComponentsInChildren<RectTransform>(true);
            int k = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].name != "cell") continue;
                if (k >= deck.Count) break;
                var c = deck[k++];
                var et = cells[i].gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enter.callback.AddListener(delegate { ShowForgePair(g, previewArea, c); });
                et.triggers.Add(enter);
                var click = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick };
                click.callback.AddListener(delegate { if (!CardPopup.ClickSuppressed) ShowForgePair(g, previewArea, c); });
                et.triggers.Add(click);
            }
        }

        /// <summary>自動操縦のスクショ用: 最初に鍛えられる札の並びを出す</summary>
        public static void PreviewFirst(GameRoot g)
        {
            if (LastPreviewArea == null || g.Rs == null) return;
            for (int i = 0; i < g.Rs.Deck.Count; i++)
            {
                bool ok = false;
                try { ok = Upgrade.CanUpgradeCard(g.Rs.Deck[i]); } catch (Exception) { }
                if (ok) { ShowForgePair(g, LastPreviewArea, g.Rs.Deck[i]); return; }
            }
        }

        /// <summary>ツールチップ用の「鍛えると→」の1文 (コストが下がる札はコストだけ、他は鍛えた後の効果行)</summary>
        public static string DescribeUpgrade(CardInstance c)
        {
            if (!Upgrade.CanUpgradeCard(c)) return Upgrade.IsUpgraded(c) ? "鍛え済み" : "鍛えられない";
            try
            {
                var up = Upgrade.UpgradeCard(c);
                if (up.Def.Cost != c.Def.Cost) return "鍛えると→ コスト " + c.Def.Cost + "E → " + up.Def.Cost + "E（効果は据え置き）";
                return "鍛えると→ " + CardText.Body(up.Def).Replace("\n", " / ");
            }
            catch (Exception ex) { return "鍛えられない: " + ex.Message; }
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
