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
            var sub = UiKit.Txt(root, "マナ脈の坑を降りる — からくり式デッキ構築ローグライク (プロトタイプ)", 18, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(64f, -150f), new Vector2(0f, -118f));

            RunUi.Message(g, root);

            // 続きから (2026-09-15 本家形): 進行中のセーブがあれば、リーダー一覧の上に帯で出す (本家の Continue が最初に来るのと同じ)
            float top = -210f;
            string saveSummary = SaveGame.PeekSummary();
            if (saveSummary != null)
            {
                ContinueBand(g, root, saveSummary);
                top = -346f;
            }

            // リーダー一覧 (横スクロール)
            var head = UiKit.Txt(root, "リーダーを選ぶ", 22, UiKit.ColText, TextAnchor.MiddleLeft, true);
            UiKit.Anchor(head.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(64f, top + 10f), new Vector2(0f, top + 44f));

            var listRoot = UiKit.NewRect("leaders", root);
            UiKit.Anchor(listRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(48f, 60f), new Vector2(-480f, top));
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
            // 列数は幅から (スマホ 1.6倍のキャンバスでは 4列。2026-09-14。PC は 5列)
            float listW = BattleScreen.CanvasSize(root).x - 480f - 48f;
            grid.constraintCount = Mathf.Max(2, Mathf.FloorToInt((listW - 36f + 18f) / (PortraitW + 18f)));

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
                var cell = Portrait(content, ld, sel, delegate { g.LeaderId = id; Audio.Ui("click"); g.Rebuild(); });
                UiKit.Le(cell, PortraitW, PortraitH, PortraitW, PortraitH);
            }

            // 詳細パネル
            var side = UiKit.Frame(root, Theme.Panel, Color.white, "detail", 3f);
            UiKit.Anchor(side.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-460f, 60f), new Vector2(-40f, -40f));
            if (selected != null) Detail(g, side.rectTransform, selected);

            var ver = UiKit.Txt(root, "set-confirm / seed " + g.Seed + " / 難易度 " + g.Difficulty, 13, UiKit.ColDim, TextAnchor.MiddleLeft);
            UiKit.Anchor(ver.rectTransform, new Vector2(0f, 0f), new Vector2(0.6f, 0f), new Vector2(64f, 20f), new Vector2(0f, 48f));
            // 前回のランのデータ回収 (2026-09-14): 落ちた/閉じたランも autosave から書き出せる
            if (Feedback.HasAutosave)
            {
                var b = UiKit.Btn(root, "前回のランのレポートを書き出す", delegate
                {
                    try { var md = Feedback.ExportAutosave(); g.Error = null; g.Notice = md != null ? "書き出した: " + md : "前回のランの記録がない"; }
                    catch (Exception e) { g.Error = "書き出しに失敗: " + e.Message; }
                    g.Rebuild();
                }, 14);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) UnityEngine.Object.Destroy(le);
                UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(360f, 14f), new Vector2(700f, 54f));
                Tooltip.Attach(b.gameObject, delegate { return "置き場: " + Feedback.ReportsDir; });
            }
        }

        /// <summary>進行中のランの帯: 要約＋「続きから」＋「放棄」。y は -296〜-206 (副題の下)</summary>
        static void ContinueBand(GameRoot g, RectTransform root, string summary)
        {
            var band = UiKit.Frame(root, Theme.Panel, new Color(1f, 0.92f, 0.6f, 1f), "continue", 3f);
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(48f, -302f), new Vector2(-480f, -206f));
            var inner = UiKit.NewRect("inner", band.transform);
            UiKit.Stretch(inner, 18f, 14f, 10f, 10f);
            var hg = UiKit.Horz(inner, 14, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandHeight = false;
            hg.childForceExpandWidth = false;

            string leaderId = null;
            try { var sf = SaveGame.Peek(); leaderId = sf != null && sf.Run != null ? sf.Run.LeaderId : null; } catch (Exception) { }
            var icon = leaderId != null ? Theme.Art("leaders", leaderId + "_icon") : null;
            if (icon != null)
            {
                var li = new GameObject("leader-icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                li.transform.SetParent(inner, false);
                li.sprite = icon; li.preserveAspect = true; li.raycastTarget = false;
                UiKit.Le(li.rectTransform, 64f, 64f, 64f, 64f);
            }
            var col = UiKit.NewRect("text", inner);
            UiKit.Le(col, 200f, 74f, -1f, 74f, 1f, -1f);
            var vg = UiKit.Vert(col, 2, 0);
            vg.childAlignment = TextAnchor.MiddleLeft;
            vg.childForceExpandHeight = false;
            var t1 = UiKit.Txt(col, "進行中のラン", 13, UiKit.ColInkSoft, TextAnchor.MiddleLeft, true);
            t1.characterSpacing = 2f;
            UiKit.Le(t1, -1f, 20f, -1f, 20f);
            var t2 = UiKit.Deco(col, summary, UiKit.Phone ? 17 : 19, UiKit.ColInk, TextAnchor.MiddleLeft);
            t2.textWrappingMode = TextWrappingModes.NoWrap;
            t2.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Le(t2, -1f, 28f, -1f, 28f);
            var at = SaveGame.SavedAt();
            string detail = null;
            try { detail = SaveGame.Detail(SaveGame.Peek()); } catch (Exception) { }
            // 1行に収める (スマホは文字の最小が 15 なので短く: 「戦闘中 ターン3 の途中　最終保存 9/15 01:39　自動保存」)
            var t3 = UiKit.Txt(col, (detail != null ? detail + "　" : "") + (at.HasValue ? "最終保存 " + at.Value.ToString("M/d HH:mm") + "　" : "") + "自動保存", 12, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            t3.textWrappingMode = TextWrappingModes.NoWrap;
            t3.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Le(t3, -1f, 22f, -1f, 22f);

            var resume = UiKit.Btn(inner, "▶ 続きから", delegate { Audio.Ui("start_run"); g.ResumeSave(); }, 22, true, UiKit.Hex("#f0d58a"));
            BattleScreen.SetSize(resume, UiKit.Phone ? 220f : 240f, 60f);
            var abandon = UiKit.Btn(inner, "放棄", delegate { g.AskAbandonSave(); }, 15, true, UiKit.Hex("#e8b8b0"));
            BattleScreen.SetSize(abandon, 92f, 44f);
            Tooltip.Attach(abandon.gameObject, delegate { return "このセーブを消す（確認あり）"; });
        }

        static RectTransform Portrait(Transform parent, LeaderDef ld, bool selected, Action onClick)
        {
            var cell = UiKit.NewRect("leader-" + ld.Id, parent);
            var frame = UiKit.Frame(cell, Theme.Panel, selected ? new Color(1f, 0.92f, 0.6f, 1f) : Color.white, "frame", 3f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            var btn = frame.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            btn.onClick.AddListener(delegate { Audio.Ui("click"); onClick(); });
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
                var mark = UiKit.Txt(cell, "▼ 選択中", 13, UiKit.ColGoldInk, TextAnchor.MiddleCenter, true);
                UiKit.Anchor(mark.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(0f, -14f));
                mark.raycastTarget = false;
            }
            return cell;
        }

        static void Detail(GameRoot g, RectTransform side, LeaderDef ld)
        {
            // 縦のレイアウトで流す (2026-09-14: 絶対座標だとスマホの低いキャンバスで初期デッキとシードが重なった)
            var col = UiKit.NewRect("col", side);
            UiKit.Stretch(col, 24f, 24f, 20f, 24f);
            var vg = UiKit.Vert(col, 8, 0);
            vg.childForceExpandHeight = false;
            var name = UiKit.Deco(col, ld.Name, 30, UiKit.ColInk, TextAnchor.MiddleLeft);
            UiKit.Le(name, -1f, 44f, -1f, 44f);
            var colors = UiKit.Txt(col, string.Join(" / ", RunUi.ColorsJa(ld.Colors)) + "   最大HP " + ld.MaxHp + "   ドロー " + ld.DrawPerTurn + "   エナジー " + ld.EnergyMax + "   報酬候補 " + ld.RewardChoices + "枚", 14, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            colors.textWrappingMode = TextWrappingModes.Normal;
            var desc = UiKit.Txt(col, ld.Description, 16, UiKit.ColInk, TextAnchor.UpperLeft);
            desc.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Le(desc, -1f, 60f, -1f, -1f, -1f, 1f);   // 余った高さは説明が受ける

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
                var dk = UiKit.Txt(col, "初期デッキ " + total + "枚: " + string.Join("、", parts.ToArray()), 13, UiKit.ColInkSoft, TextAnchor.UpperLeft);
                dk.textWrappingMode = TextWrappingModes.Normal;
            }

            // シードと難易度 (2行)
            var seedRow = UiKit.NewRect("seedrow", col);
            UiKit.Le(seedRow, -1f, 40f, -1f, 40f);
            var hg = UiKit.Horz(seedRow, 10, 0);
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.childForceExpandWidth = false;
            var seedLbl = UiKit.Txt(seedRow, "シード", 15, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(seedLbl, 64f, 36f, 64f, 36f);
            var field = RunScreens.MakeSeedField(g, seedRow);
            UiKit.Le(field, 180f, 36f, 180f, 36f);
            var diffRow = UiKit.NewRect("diffrow", col);
            UiKit.Le(diffRow, -1f, 40f, -1f, 40f);
            var hg2 = UiKit.Horz(diffRow, 10, 0);
            hg2.childAlignment = TextAnchor.MiddleLeft;
            hg2.childForceExpandWidth = false;
            var diffLbl = UiKit.Txt(diffRow, "難易度", 15, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            UiKit.Le(diffLbl, 64f, 36f, 64f, 36f);
            var minus = UiKit.Btn(diffRow, "−", delegate { g.Difficulty = Math.Max(1, g.Difficulty - 1); g.Rebuild(); }, 18, g.Difficulty > 1);
            BattleScreen.SetSize(minus, 44f, 40f);
            var dv = UiKit.Txt(diffRow, g.Difficulty.ToString(), 20, g.Difficulty > DeckRogue.Engine.Run.DEFAULT_DIFFICULTY ? UiKit.ColBadInk : UiKit.ColInk, TextAnchor.MiddleCenter, true);
            UiKit.Le(dv, 44f, 36f, 44f, 36f);
            var plus = UiKit.Btn(diffRow, "+", delegate { g.Difficulty = Math.Min(10, g.Difficulty + 1); g.Rebuild(); }, 18, g.Difficulty < 10);
            BattleScreen.SetSize(plus, 44f, 40f);

            var note = UiKit.Txt(col, "難易度 3 が標準。上げると敵の打点とHPが増える (報酬は変わらない)", 12, UiKit.ColInkSoft, TextAnchor.MiddleLeft);
            note.textWrappingMode = TextWrappingModes.Normal;

            // 進行中のセーブがあれば「新しいランを開始」= 捨てて始める確認を挟む (2026-09-15 本家形: 進行中のランは1本)
            bool hasSave = SaveGame.Exists;
            var start = UiKit.Btn(col, hasSave ? "新しいランを開始" : "ランを開始", delegate { Audio.Ui("start_run"); g.AskStartRun(); }, 24, true, hasSave ? (Color?)null : UiKit.Hex("#f0d58a"));
            var le = start.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 64f; le.preferredHeight = 64f; }
        }
    }
}
