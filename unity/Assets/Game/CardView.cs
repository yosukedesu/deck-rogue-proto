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

            // 色の縁 (緑青赤白黒)
            var edge = UiKit.Pan(root, Theme.ColorEdge(c.Def.Color ?? "green"), "edge");
            UiKit.Anchor(edge.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -50f), new Vector2(-12f, -46f));
            edge.raycastTarget = false;

            // コスト玉 (左上)
            int cost = c.Def.Cost;
            bool discounted = false;
            try { cost = Effects.EffectiveCost(st, c); discounted = c.Def.XCost != true && cost != c.Def.Cost; } catch (Exception) { }
            var orb = UiKit.NewRect("cost", root);
            UiKit.Anchor(orb, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-10f, -56f), new Vector2(46f, 0f));
            var orbImg = orb.gameObject.AddComponent<Image>();
            orbImg.sprite = Theme.Icon("energy");
            orbImg.preserveAspect = true;
            orbImg.raycastTarget = false;
            orbImg.color = st != null && playable ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            string costLabel = c.Def.XCost == true ? "X" : cost.ToString();
            var costT = UiKit.Txt(orb, costLabel, 26, discounted ? UiKit.ColAccent : Color.white, TextAnchor.MiddleCenter, true);
            costT.outlineWidth = 0.25f;
            costT.outlineColor = Color.black;
            UiKit.Stretch(costT.rectTransform, 0f, 0f, 0f, 0f);

            // 名前
            var nameT = UiKit.Txt(root, c.Def.Name, c.Def.Name.Length > 7 ? 18 : 21, playable ? UiKit.ColText : UiKit.ColDim, TextAnchor.MiddleCenter, true);
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -46f), new Vector2(-14f, -12f));
            nameT.textWrappingMode = TextWrappingModes.NoWrap;
            nameT.overflowMode = TextOverflowModes.Ellipsis;

            // タイプ帯
            var band = UiKit.Txt(root, CardText.TypeJa(c.Def.Type) + "  " + CardText.RarityLabel(c.Def.Rarity ?? "common"), 13, Color.Lerp(typeCol, Color.white, 0.55f), TextAnchor.MiddleCenter);
            UiKit.Anchor(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -74f), new Vector2(-14f, -52f));

            // 本文
            var body = UiKit.Txt(root, CardText.Body(c.Def), 15, playable ? UiKit.ColText : UiKit.ColDim, TextAnchor.UpperLeft);
            UiKit.Anchor(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 40f), new Vector2(-18f, -78f));

            // 注記 (消滅・保持・追加コスト)
            var notes = CardText.Notes(c.Def);
            if (notes.Length > 0)
            {
                var nt = UiKit.Txt(root, notes, 12, UiKit.ColEnergy, TextAnchor.LowerCenter);
                UiKit.Anchor(nt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 14f), new Vector2(-16f, 40f));
            }
            return root;
        }
    }
}
