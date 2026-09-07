// TitleScreen.cs — タイトル/セットアップ画面 (M3・2026-09-07): リーダー選択をカード風に並べ、右に詳細と開始ボタン
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class TitleScreen
    {
        const float PortraitW = 190f;
        const float PortraitH = 236f;

        public static void Build(GameRoot g, RectTransform root)
        {
            BattleScreen.BuildBackground(root, 1);
            var dim = UiKit.Pan(root, new Color(0f, 0f, 0f, 0.35f), "dim");
            dim.raycastTarget = false;
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);

            // 題
            var title = UiKit.Deco(root, "DECK ROGUE", 64, UiKit.ColText, TextAnchor.MiddleLeft);
            title.characterSpacing = 8f;
            title.outlineWidth = 0.25f; title.outlineColor = Color.black;
            UiKit.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(60f, -120f), new Vector2(0f, -30f));
            var sub = UiKit.Txt(root, "古の塔を登る — リアクション式デッキ構築ローグライク (プロトタイプ)", 18, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(64f, -150f), new Vector2(0f, -118f));

            RunUi.Message(g, root);

            // リーダー一覧 (横スクロール)
            var head = UiKit.Txt(root, "リーダーを選ぶ", 22, UiKit.ColText, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(64f, -200f), new Vector2(0f, -166f));

            var listRoot = UiKit.NewRect("leaders", root);
            UiKit.Anchor(listRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(48f, 60f), new Vector2(-480f, -210f));
            var content = UiKit.Scroll(listRoot, true, new Color(0f, 0f, 0f, 0.2f), 18, 18);
            UiKit.Stretch(UiKit.ScrollRoot(content), 0f, 0f, 0f, 0f);
            var vg = content.GetComponent<VerticalLayoutGroup>();
            if (vg != null) UnityEngine.Object.DestroyImmediate(vg);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(PortraitW, PortraitH);
            grid.spacing = new Vector2(18f, 18f);
            grid.padding = new RectOffset(18, 18, 18, 18);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            var leaders = Content.AllLeaders;
            LeaderDef selected = null;
            for (int i = 0; i < leaders.Count; i++)
            {
                var ld = leaders[i];
                if (ld.Id == g.LeaderId) selected = ld;
            }
            if (selected == null && leaders.Count > 0) { selected = leaders[0]; g.LeaderId = selected.Id; }
            for (int i = 0; i < leaders.Count; i++)
            {
                var ld = leaders[i];
                string id = ld.Id;
                bool sel = ld.Id == g.LeaderId;
                var cell = Portrait(content, ld, sel, delegate { g.LeaderId = id; Audio.Play("card_set", 0.6f); g.Rebuild(); });
                UiKit.Le(cell, PortraitW, PortraitH, PortraitW, PortraitH);
            }

            // 詳細パネル
            var side = UiKit.Frame(root, Theme.Panel, Color.white, "detail", 3f);
            UiKit.Anchor(side.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-460f, 60f), new Vector2(-40f, -40f));
            if (selected != null) Detail(g, side.rectTransform, selected);

            var ver = UiKit.Txt(root, "set-confirm / seed " + g.Seed + " / 難易度 " + g.Difficulty, 13, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Anchor(ver.rectTransform, new Vector2(0f, 0f), new Vector2(0.6f, 0f), new Vector2(64f, 20f), new Vector2(0f, 48f));
        }

        static RectTransform Portrait(Transform parent, LeaderDef ld, bool selected, Action onClick)
        {
            var cell = UiKit.NewRect("leader-" + ld.Id, parent);
            var frame = UiKit.Frame(cell, Theme.Panel, selected ? new Color(1f, 0.92f, 0.6f, 1f) : Color.white, "frame", 3f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            btn.onClick.AddListener(delegate { Audio.Play("click", 0.5f); onClick(); });
            var cols = btn.colors; cols.highlightedColor = new Color(1.08f, 1.08f, 1.08f); cols.pressedColor = new Color(0.9f, 0.9f, 0.9f); btn.colors = cols;

            // 色の縁
            Color edge = Theme.ColorEdge(ld.Colors.Count > 0 ? ld.Colors[0] : "green");
            var band = UiKit.Pan(cell, edge, "band");
            band.raycastTarget = false;
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -14f), new Vector2(-8f, -8f));
            if (ld.Colors.Count > 1)
            {
                var band2 = UiKit.Pan(cell, Theme.ColorEdge(ld.Colors[1]), "band2");
                band2.raycastTarget = false;
                UiKit.Anchor(band2.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, -14f), new Vector2(-8f, -8f));
            }

            // 立ち絵 (プレースホルダー: 生成スプライト)
            var art = new GameObject("art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            art.transform.SetParent(cell, false);
            art.raycastTarget = false;
            art.sprite = Creature.Get("leaders", ld.Id, true);
            art.preserveAspect = true;
            UiKit.Anchor(art.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-56f, -138f), new Vector2(56f, -22f));

            var name = UiKit.Deco(cell, ld.Name, 18, UiKit.ColInk, TextAnchor.MiddleCenter);
            name.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 54f), new Vector2(-6f, 96f));

            var stats = UiKit.Txt(cell, string.Join("/", RunUi.ColorsJa(ld.Colors)) + "  HP " + ld.MaxHp + "\nドロー " + ld.DrawPerTurn + "  エナジー " + ld.EnergyMax, 12, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
            UiKit.Anchor(stats.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 8f), new Vector2(-6f, 54f));

            if (selected)
            {
                var mark = UiKit.Txt(cell, "▼ 選択中", 13, Theme.Gold, TextAnchor.MiddleCenter, true);
                UiKit.Anchor(mark.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(0f, -14f));
                mark.raycastTarget = false;
            }
            return cell;
        }

        static void Detail(GameRoot g, RectTransform side, LeaderDef ld)
        {
            var name = UiKit.Deco(side, ld.Name, 30, UiKit.ColInk, TextAnchor.MiddleLeft);
            UiKit.Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -70f), new Vector2(-24f, -20f));
            var colors = UiKit.Txt(side, string.Join(" / ", RunUi.ColorsJa(ld.Colors)) + "   最大HP " + ld.MaxHp + "   ドロー " + ld.DrawPerTurn + "   エナジー " + ld.EnergyMax + "   報酬候補 " + ld.RewardChoices + "枚", 14, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            colors.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(colors.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -120f), new Vector2(-24f, -72f));

            var desc = UiKit.Txt(side, ld.Description, 16, UiKit.ColInk, TextAnchor.UpperLeft);
            desc.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(desc.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -330f), new Vector2(-24f, -126f));

            // 初期デッキ
            DeckDef deck = null;
            try { deck = Content.GetDeckDef(ld.RunDeckId); } catch (Exception) { }
            if (deck != null)
            {
                int total = 0;
                var parts = new List<string>();
                for (int i = 0; i < deck.Cards.Count; i++)
                {
                    var e = deck.Cards[i];
                    string nm = e.CardId;
                    try { nm = Content.GetCardDef(e.CardId).Name; } catch (Exception) { }
                    parts.Add(nm + (e.Count > 1 ? "×" + e.Count : ""));
                    total += e.Count;
                }
                var dk = UiKit.Txt(side, "初期デッキ " + total + "枚: " + string.Join("、", parts.ToArray()), 13, UiKit.ColInkSoft, TextAnchor.UpperLeft);
                dk.textWrappingMode = TextWrappingModes.Normal;
                UiKit.Anchor(dk.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -420f), new Vector2(-24f, -336f));
            }

            // シードと難易度 (2行)
            var seedRow = UiKit.NewRect("seedrow", side);
            UiKit.Anchor(seedRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 200f), new Vector2(-24f, 240f));
            var hg = UiKit.Horz(seedRow, 10, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandWidth = false;
            var seedLbl = UiKit.Txt(seedRow, "シード", 15, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(seedLbl, 64f, 36f, 64f, 36f);
            var field = RunScreens.MakeSeedField(g, seedRow);
            UiKit.Le(field, 180f, 36f, 180f, 36f);
            var diffRow = UiKit.NewRect("diffrow", side);
            UiKit.Anchor(diffRow, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 152f), new Vector2(-24f, 192f));
            var hg2 = UiKit.Horz(diffRow, 10, 0);
            hg2.childAlignment = TextAnchor.MiddleLeft;
            hg2.childForceExpandWidth = false;
            var diffLbl = UiKit.Txt(diffRow, "難易度", 15, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(diffLbl, 64f, 36f, 64f, 36f);
            var minus = UiKit.Btn(diffRow, "−", delegate { g.Difficulty = Math.Max(1, g.Difficulty - 1); g.Rebuild(); }, 18, g.Difficulty > 1);
            BattleScreen.SetSize(minus, 40f, 36f);
            var dv = UiKit.Txt(diffRow, g.Difficulty.ToString(), 20, g.Difficulty > DeckRogue.Engine.Run.DEFAULT_DIFFICULTY ? UiKit.ColBad : UiKit.ColInk, TextAnchor.MiddleCenter, true);
            UiKit.Le(dv, 44f, 36f, 44f, 36f);
            var plus = UiKit.Btn(diffRow, "+", delegate { g.Difficulty = Math.Min(10, g.Difficulty + 1); g.Rebuild(); }, 18, g.Difficulty < 10);
            BattleScreen.SetSize(plus, 40f, 36f);

            var note = UiKit.Txt(side, "難易度 3 が標準。上げると敵の打点とHPが増える (報酬は変わらない)", 12, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            note.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Anchor(note.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 108f), new Vector2(-24f, 146f));

            var start = UiKit.Btn(side, "ランを開始", delegate { Audio.Play("turn", 0.7f); g.StartRun(); }, 24, true, UiKit.Hex("#f0d58a"));
            var le = start.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(start.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 30f), new Vector2(-24f, 96f));
        }
    }
}
