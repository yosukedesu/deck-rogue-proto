// EndScreen.cs — ランの決着 (M3・2026-09-07): 走破/敗北の見出し・戦績・最終デッキ
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class EndScreen
    {
        public static void Build(GameRoot g, RectTransform root, bool won)
        {
            var run = g.Rs;
            BattleScreen.BuildBackground(root, run.Act);
            var dim = UiKit.Pan(root, won ? new Color(0.1f, 0.08f, 0f, 0.55f) : new Color(0.1f, 0f, 0f, 0.65f), "dim");
            dim.raycastTarget = false;
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);

            var title = UiKit.Txt(root, won ? "走破！" : "敗北", 72, won ? Theme.Gold : UiKit.ColBad, TextAnchor.MiddleCenter, true);
            title.outlineWidth = 0.25f; title.outlineColor = Color.black;
            UiKit.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(0f, -40f));
            Tween.Punch(title.rectTransform, 0.15f, 0.7f);
            string leader = run.LeaderId;
            try { leader = Content.GetLeaderDef(run.LeaderId).Name; } catch (Exception) { }
            var sub = UiKit.Txt(root, leader + "  /  シード " + run.Seed + "  /  難易度 " + run.Difficulty
                + "\n幕" + run.Act + " 行" + (run.Row + 1) + "  ·  勝利 " + run.BattlesWon + " 戦  ·  HP " + run.Hp + " / " + run.MaxHp
                + "  ·  " + run.Gold + "G  ·  デッキ " + run.Deck.Count + "枚  ·  レリック " + run.Relics.Count + "個", 18, UiKit.ColText, TextAnchor.MiddleCenter);
            UiKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -220f), new Vector2(0f, -150f));

            // レリック
            var relics = UiKit.NewRect("relics", root);
            UiKit.Anchor(relics, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-600f, -280f), new Vector2(600f, -228f));
            var hg = UiKit.Horz(relics, 8, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
            for (int i = 0; i < run.Relics.Count; i++)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(run.Relics[i]); } catch (Exception) { }
                var t = UiKit.Txt(relics, (rd != null ? rd.Name : run.Relics[i]), 15, UiKit.ColText, TextAnchor.MiddleCenter);
                UiKit.Le(t, 40f, 30f, -1f, 30f);
                var tip = rd != null ? rd.Description : "";
                Tooltip.Attach(t.gameObject, delegate { return tip; });
            }

            var area = UiKit.NewRect("deck", root);
            UiKit.Anchor(area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-760f, 110f), new Vector2(760f, -290f));
            UiKit.Vert(area, 0, 0);
            RunUi.CardGrid(g, area, run.Deck, null, null, null, 400f);

            RunUi.BottomButton(root, "タイトルへ", delegate { g.BackToSetup(); }, 20, 300f, 56f, 0f, 36f, UiKit.Hex("#f0d58a"));
        }
    }
}
