// CardView.cs — カードの面 (200×290)。「絵本」の肌 (2026-09-07 デザインカンバス第5版):
// クリーム色の紙に鉛筆の二重線、左上のしおり=タイプ (コストを乗せる)、右上の星=レア度、
// 紙の台紙に貼った挿絵 (Art/cards/<id>.png 80×48 を2倍。無ければタイプの紋章 16×16 を3倍)、
// 本文は墨、左下の剣のにじみ=与ダメ・右下の盾のにじみ=ブロック (効果から導出。データにカテゴリは増やさない)。
using System;
using System.Collections.Generic;
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

        /// <summary>予測行 (対象が決まっている時の実値) の対象。-1 で出さない</summary>
        public static int PreviewEnemy = -1;

        public static RectTransform Build(Transform parent, CardInstance c, GameState st, bool playable, bool interactable, string name = "card")
        {
            var root = UiKit.NewRect(name, parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(W, H);

            var def = c.Def;
            var typeCol = PaperFx.TypeColor(def.Type);
            var ink = playable ? PaperFx.Ink : new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.7f);

            // 紙
            var paper = PaperFx.Sheet(root, PaperFx.Card, "paper", playable ? Color.white : new Color(0.82f, 0.8f, 0.76f, 1f));
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            paper.raycastTarget = interactable;
            var grain = PaperFx.GrainOver(root, 0.7f);
            grain.raycastTarget = false;

            // 挿絵の台紙 (168×104) と絵
            var mat = UiKit.NewRect("art", root);
            UiKit.Anchor(mat, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -158f), new Vector2(184f, -54f));
            var matEdge = mat.gameObject.AddComponent<Image>();
            matEdge.color = new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.8f);
            matEdge.raycastTarget = false;
            var matIn = UiKit.Pan(mat, PaperFx.Paper2, "mat");
            UiKit.Stretch(matIn.rectTransform, 2f, 2f, 2f, 2f);
            matIn.raycastTarget = false;
            var art = Theme.Art("cards", def.Id);
            if (art != null)
            {
                // 80×48 を2倍 (整数倍)。それ以外の寸法でも縦横比を保って収める
                var ai = UiKit.NewRect("pic", mat);
                ai.anchorMin = ai.anchorMax = new Vector2(0.5f, 0.5f);
                ai.sizeDelta = new Vector2(Mathf.Min(160f, art.rect.width * 2f), Mathf.Min(96f, art.rect.height * 2f));
                var aimg = ai.gameObject.AddComponent<Image>();
                aimg.sprite = art; aimg.preserveAspect = true; aimg.raycastTarget = false;
                aimg.color = playable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            }
            else
            {
                var wash = UiKit.Pan(mat, new Color(typeCol.r, typeCol.g, typeCol.b, 0.35f), "wash");
                UiKit.Stretch(wash.rectTransform, 2f, 2f, 2f, 2f);
                wash.raycastTarget = false;
                var crestRt = UiKit.NewRect("crest", mat);
                crestRt.anchorMin = crestRt.anchorMax = new Vector2(0.5f, 0.5f);
                crestRt.sizeDelta = new Vector2(48f, 48f);
                crestRt.anchoredPosition = Vector2.zero;
                var crest = crestRt.gameObject.AddComponent<Image>();
                crest.sprite = Theme.Icon("crest_" + def.Type);
                crest.preserveAspect = true;
                crest.raycastTarget = false;
                crest.color = playable ? Color.white : new Color(1f, 1f, 1f, 0.7f);
            }

            // しおり (タイプ) とコスト
            var bm = UiKit.NewRect("bookmark", root);
            UiKit.Anchor(bm, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -44f), new Vector2(44f, 4f));
            var bmImg = bm.gameObject.AddComponent<Image>();
            bmImg.sprite = PaperFx.Bookmark(playable ? typeCol : Color.Lerp(typeCol, Color.gray, 0.5f));
            bmImg.raycastTarget = false;
            int cost = def.Cost;
            bool discounted = false;
            try { if (st != null) { cost = Effects.EffectiveCost(st, c); discounted = def.XCost != true && cost != def.Cost; } } catch (Exception) { }
            string costLabel = def.XCost == true ? "X" : cost.ToString();
            var costT = UiKit.Txt(bm, costLabel, 19, discounted ? UiKit.Hex("#2f6e40") : PaperFx.Ink, TextAnchor.MiddleCenter, true);
            UiKit.Anchor(costT.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 10f), new Vector2(0f, -4f));

            // 星 (レア度)
            int stars = def.Rarity == "rare" ? 3 : def.Rarity == "uncommon" ? 2 : 1;
            Color starCol = def.Rarity == "rare" ? PaperFx.Honey : def.Rarity == "uncommon" ? PaperFx.Sky : PaperFx.Paper;
            for (int i = 0; i < stars; i++)
            {
                var star = UiKit.Icon(root, "star", 16f, starCol);
                UiKit.Anchor(star.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f - 16f * (i + 1), -28f), new Vector2(-12f - 16f * i, -12f));
                star.raycastTarget = false;
            }

            // 名前 (装飾明朝) と鉛筆の下線
            int nameSize = def.Name.Length > 5 ? 16 : (def.Name.Length > 4 ? 18 : 20);
            var nameT = UiKit.Deco(root, def.Name, nameSize, ink, TextAnchor.MiddleCenter);
            UiKit.Anchor(nameT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(50f, -46f), new Vector2(-34f, -10f));
            nameT.textWrappingMode = TextWrappingModes.NoWrap;
            nameT.overflowMode = TextOverflowModes.Overflow;
            var underline = UiKit.Pan(root, new Color(PaperFx.Ink.r, PaperFx.Ink.g, PaperFx.Ink.b, 0.55f), "underline");
            UiKit.Anchor(underline.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -48f), new Vector2(-46f, -46f));
            underline.raycastTarget = false;

            // タイプ・レア度の小さな文字
            var typeT = UiKit.Txt(root, CardText.TypeJa(def.Type) + " ・ " + CardText.RarityLabel(def.Rarity ?? "common"), 10, PaperFx.InkSoft, TextAnchor.MiddleCenter);
            UiKit.Anchor(typeT.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -176f), new Vector2(-14f, -160f));
            typeT.characterSpacing = 3f;

            // 本文 (墨)
            string bodyText = CardText.Body(def);
            var body = UiKit.Txt(root, bodyText, 14, ink, TextAnchor.UpperCenter, true);
            UiKit.Anchor(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 56f), new Vector2(-16f, -180f));
            body.lineSpacing = -4f;

            // 予測行 (対象が決まっている時、実処理と同じ手順の実値)
            string preview = Preview(c, st);
            if (preview != null)
            {
                var pv = UiKit.Txt(root, preview, 13, UiKit.Hex("#8a5a1a"), TextAnchor.MiddleCenter, true);
                UiKit.Anchor(pv.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 54f), new Vector2(-18f, 78f));
            }

            // 役割の札 (左下=与ダメ・右下=ブロック・返しは左下に戻り矢印)
            string dmg, blk, counter; bool modeBoth;
            RoleLabels(def, out dmg, out blk, out counter, out modeBoth);
            if (dmg != null) Badge(root, "dmg", "sword", dmg, false, ink);
            else if (counter != null) Badge(root, "counter", "counter", counter, false, ink);
            if (blk != null) Badge(root, "block", "shield", blk, true, ink);
            string notes = CardText.Notes(def);
            if (modeBoth)
            {
                var either = UiKit.Txt(root, "どちらか", 10, PaperFx.InkSoft, TextAnchor.MiddleCenter);
                UiKit.Anchor(either.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 14f), new Vector2(0f, 30f));
            }
            else if (notes.Length > 0)
            {
                var tape = UiKit.NewRect("tape", root);
                UiKit.Anchor(tape, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-44f, 12f), new Vector2(44f, 34f));
                tape.localRotation = Quaternion.Euler(0f, 0f, 3f);
                var tImg = tape.gameObject.AddComponent<Image>();
                tImg.sprite = PaperFx.Tape(); tImg.raycastTarget = false;
                var tt = UiKit.Txt(tape, notes, 10, PaperFx.Ink, TextAnchor.MiddleCenter, true);
                UiKit.Stretch(tt.rectTransform, 2f, 2f, 0f, 0f);
                tt.textWrappingMode = TextWrappingModes.NoWrap;
            }
            return root;
        }

        /// <summary>にじみの札: 水彩の丸に、ドットのしるしと数字 (墨)</summary>
        static void Badge(RectTransform root, string role, string icon, string value, bool right, Color ink)
        {
            var cell = UiKit.NewRect("badge-" + role, root);
            if (right) UiKit.Anchor(cell, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-70f, 8f), new Vector2(-10f, 48f));
            else UiKit.Anchor(cell, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 8f), new Vector2(70f, 48f));
            cell.localRotation = Quaternion.Euler(0f, 0f, right ? 4f : -4f);
            var blob = PaperFx.BlobImage(cell, PaperFx.RoleColor(role));
            UiKit.Stretch(blob.rectTransform, 0f, 0f, 0f, 0f);
            var row = UiKit.NewRect("row", cell);
            UiKit.Stretch(row, 0f, 0f, 0f, 0f);
            var hg = UiKit.Horz(row, 3, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
            var ic = UiKit.Icon(row, icon, 16f, ink);
            UiKit.Le(ic, 16f, 16f, 16f, 16f);
            var t = UiKit.Txt(row, value, value.Length > 2 ? 15 : 18, ink, TextAnchor.MiddleCenter, true);
            UiKit.Le(t, 14f, 24f, -1f, 24f);
            row.localRotation = Quaternion.Euler(0f, 0f, right ? -4f : 4f); // 文字は水平に戻す
        }

        /// <summary>効果から役割の値を導く。ダメージ=onPlay の dealDamage の合計 (同値の多段は a×n)、ブロック=gainBlock/gainIceBlock、返し=counter</summary>
        public static void RoleLabels(CardDef def, out string dmg, out string blk, out string counter, out bool modeBoth)
        {
            dmg = null; blk = null; counter = null; modeBoth = false;
            if (def.Modes != null && def.Modes.Count > 0)
            {
                string d = null, b = null;
                for (int m = 0; m < def.Modes.Count; m++)
                {
                    string md, mb, mc;
                    Summarize(def.Modes[m].Effects, out md, out mb, out mc);
                    if (md != null) d = md;
                    if (mb != null) b = mb;
                }
                dmg = d; blk = b; modeBoth = d != null && b != null;
                return;
            }
            Summarize(def.Effects, out dmg, out blk, out counter);
        }

        static void Summarize(IReadOnlyList<DeclarativeEffect> effects, out string dmg, out string blk, out string counter)
        {
            dmg = null; blk = null; counter = null;
            if (effects == null) return;
            var dmgs = new List<int>();
            int block = 0, ctr = 0;
            for (int i = 0; i < effects.Count; i++)
            {
                var e = effects[i];
                if (e.Trigger != null && e.Trigger != "onPlay" && e.Trigger != "onAttacked" && e.Trigger != "onAttackedPre") continue;
                if (e.Effect == "dealDamage" && e.Amount.HasValue) dmgs.Add(e.Amount.Value);
                else if ((e.Effect == "gainBlock" || e.Effect == "gainIceBlock") && e.Amount.HasValue) block += e.Amount.Value;
                else if (e.Effect == "counter" && e.Amount.HasValue) ctr += e.Amount.Value;
            }
            if (dmgs.Count > 0)
            {
                bool same = true;
                for (int i = 1; i < dmgs.Count; i++) if (dmgs[i] != dmgs[0]) same = false;
                int sum = 0; for (int i = 0; i < dmgs.Count; i++) sum += dmgs[i];
                dmg = dmgs.Count > 1 && same ? dmgs[0] + "×" + dmgs.Count : sum.ToString();
            }
            if (block > 0) blk = block.ToString();
            if (ctr > 0) counter = ctr.ToString();
        }

        /// <summary>対象が決まっている時、ダメージ効果の実値を見積もる (実処理と同じ手順)。補正が無ければ null</summary>
        static string Preview(CardInstance c, GameState st)
        {
            if (st == null || PreviewEnemy < 0 || PreviewEnemy >= st.Enemies.Count) return null;
            var parts = new List<string>();
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
