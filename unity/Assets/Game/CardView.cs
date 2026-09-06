// CardView.cs — カード1枚の描画 (2026-09-07 M2)。手札・確認ウィンドウ・ピッカーで共用する。
// 200×290 (5:7)。枠は Theme.CardFrame (9スライス) をタイプ色で染め、コスト玉・名前・タイプ帯・本文・注記を載せる。
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class CardView
    {
        public const float W = 200f;
        public const float H = 290f;

        /// <summary>カードを作る。root は W×H (pivot 中央・アンカー中央)。frame Image が raycast の的</summary>
        /// <summary>手札の予測に使う対象の敵 (BattleScreen が組み立て前に設定。-1 = 予測なし)</summary>
        public static int PreviewEnemy = -1;

        public static RectTransform Build(Transform parent, CardInstance c, GameState st, bool playable, bool interactable, string name = "card")
        {
            var root = UiKit.NewRect(name, parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(W, H);

            var typeCol = Theme.CardTypeColor(c.Def.Type);
            var frame = UiKit.Frame(root, Theme.CardFrame, playable ? typeCol : Color.Lerp(typeCol, Color.black, 0.5f), "frame", 3f);
            UiKit.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
            frame.raycastTarget = interactable;

            var inner = UiKit.Pan(root, playable ? UiKit.Hex("#1b2420") : UiKit.Hex("#141917"), "inner");
            UiKit.Stretch(inner.rectTransform, 12f, 12f, 12f, 12f);
            inner.raycastTarget = false;

            // 紋章の窓 (絵の代わり。Art/cards/<id>.png で差し替わる)
            var art = UiKit.NewRect("art", root);
            UiKit.Anchor(art, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -156f), new Vector2(-16f, -52f));
            var artImg = art.gameObject.AddComponent<Image>();
            artImg.sprite = ThemeFx.CardArt(c.Def.Id, typeCol);
            artImg.preserveAspect = false;
            artImg.raycastTarget = false;
            artImg.color = playable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            var artEdge = UiKit.Pan(root, Theme.PanelEdge, "artEdge");
            UiKit.Anchor(artEdge.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -159f), new Vector2(-16f, -156f));
            artEdge.raycastTarget = false;

            // 色の縁 (緑青赤白黒) は名前板の下線
            var edge = UiKit.Pan(root, Theme.ColorEdge(c.Def.Color ?? "green"), "edge");
            UiKit.Anchor(edge.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -52f), new Vector2(-12f, -49f));
            edge.raycastTarget = false;

            // コスト玉 (左上)
            int cost = c.Def.Cost;
            bool discounted = false;
            try { cost = Effects.EffectiveCost(st, c); discounted = c.Def.XCost != true && cost != c.Def.Cost; } catch (Exception) { }
            var orb = UiKit.NewRect("cost", root);
            UiKit.Anchor(orb, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-14f, -58f), new Vector2(50f, 6f));
            var orbImg = orb.gameObject.AddComponent<Image>();
            orbImg.sprite = ThemeFx.CostOrb();
            orbImg.preserveAspect = true;
            orbImg.raycastTarget = false;
            orbImg.color = playable ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            string costLabel = c.Def.XCost == true ? "X" : cost.ToString();
            var costT = UiKit.Txt(orb, costLabel, 28, discounted ? UiKit.ColAccent : Color.white, TextAnchor.MiddleCenter, true);
            costT.outlineWidth = 0.3f;
            costT.outlineColor = Color.black;
            UiKit.Stretch(costT.rectTransform, 0f, 0f, 0f, 0f);

            // レア度の宝石 (右上)
            var gem = UiKit.NewRect("gem", root);
            UiKit.Anchor(gem, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -44f), new Vector2(-16f, -20f));
            var gemImg = gem.gameObject.AddComponent<Image>();
            gemImg.sprite = ThemeFx.Gem(c.Def.Rarity ?? "common");
            gemImg.preserveAspect = true;
            gemImg.raycastTarget = false;

            // 名前
            var nameT = UiKit.Txt(root, c.Def.Name, c.Def.Name.Length > 7 ? 18 : 21, playable ? UiKit.ColText : UiKit.ColDim, TextAnchor.MiddleCenter, true);
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -48f), new Vector2(-40f, -12f));
            nameT.textWrappingMode = TextWrappingModes.NoWrap;
            nameT.overflowMode = TextOverflowModes.Ellipsis;

            // タイプ帯 (紋章の下)
            var band = UiKit.Txt(root, CardText.TypeJa(c.Def.Type) + "  ·  " + CardText.RarityLabel(c.Def.Rarity ?? "common"), 12, Color.Lerp(typeCol, Color.white, 0.55f), TextAnchor.MiddleCenter);
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -178f), new Vector2(-14f, -160f));

            // 本文
            var body = UiKit.Txt(root, CardText.Body(c.Def), 15, playable ? UiKit.ColText : UiKit.ColDim, TextAnchor.UpperCenter);
            UiKit.Anchor(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 36f), new Vector2(-18f, -182f));

            // ダメージ予測 (成長・勢い・弱体・急所・装甲・敵ブロックを実処理と同じ手順で)
            string preview = Preview(c, st);
            if (preview != null)
            {
                var pv = UiKit.Txt(root, preview, 14, UiKit.ColEnergy, TextAnchor.LowerLeft, true);
                UiKit.Anchor(pv.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 34f), new Vector2(-18f, 56f));
            }
            // 注記 (消滅・保持・追加コスト)
            var notes = CardText.Notes(c.Def);
            if (notes.Length > 0)
            {
                var nt = UiKit.Txt(root, notes, 12, UiKit.ColEnergy, TextAnchor.LowerCenter);
                UiKit.Anchor(nt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 14f), new Vector2(-16f, 40f));
            }
            return root;
        }

        /// <summary>対象が決まっている時、ダメージ効果の実値を見積もる。補正が無ければ null</summary>
        static string Preview(CardInstance c, GameState st)
        {
            if (st == null || PreviewEnemy < 0 || PreviewEnemy >= st.Enemies.Count) return null;
            var parts = new System.Collections.Generic.List<string>();
            bool changed = false;
            for (int i = 0; i < c.Def.Effects.Count; i++)
            {
                var e = c.Def.Effects[i];
                if (e.Trigger != "onPlay" || e.Effect != "dealDamage" || !e.Amount.HasValue) continue;
                int baseAmt = e.Amount.Value + (c.GrowBonus ?? 0);
                DamageBreakdown bd = null;
                try { bd = Effects.DamageBreakdownOf(st, PreviewEnemy, baseAmt, e.Pierce == true); } catch (Exception) { }
                if (bd == null || bd.Steps.Count == 0) continue;
                int final = bd.HpLoss;
                if (final != baseAmt) changed = true;
                parts.Add(final.ToString());
            }
            if (!changed || parts.Count == 0) return null;
            return "→ 実ダメ " + string.Join("+", parts.ToArray());
        }
    }
}
