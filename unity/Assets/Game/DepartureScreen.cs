// DepartureScreen.cs — 出立の店 (ラン開始 2026-09-24。docs/departure-proposal-2026-09-24.md):
// 主人公が坑に降りる前に支度を整えに寄った坑口の行商の店。札3 (カードの面)・遺物3 (右の列)・サービス4 (札の下) を値札で並べ、何個でも買える (買わなくてもよい。上限なし)。種類名は出さない (2026-09-24 夜)。
// 前借り (ツケ＋代償) は 2026-09-24 夜に撤去＝全部「N G で買う」。除去・鍛えは ?イベントと同じ「デッキから1枚選ぶ」画面 (EventScreen.CardPick)。下端の「店を出て坑へ」で地図へ。
// 中身 (どの遺物・どのギア・どの札か) は名指しで見せる。PC は1行、スマホは2段 (中身は小さな形)。
// 買わなかった「物」は行商が幕1の店に並べる仕組みは残るが、この画面では言わない (ユーザー裁定)。ShopScreen の「行商が預かった支度」の段が Art / Summary / Tip / UnavailableReason をここから使う
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class DepartureScreen
    {
        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            Backdrop(g, root);
            var offers = run.Departure != null ? run.Departure.Offers : null;
            if (offers == null || offers.Count == 0)
            {
                RunUi.Heading(root, Content.DEPARTURE_TITLE, "品物が見つかりません");
                LeaveButton(g, root, null);
                return;
            }

            // 除去・鍛え: 対象の札を選ぶ (?イベントと同じ画面。戻れば店へ)
            if (g.DepartureChoiceIndex >= 0 && g.DepartureChoiceIndex < offers.Count)
            {
                int ci = g.DepartureChoiceIndex;
                var of = offers[ci];
                string price = "（" + of.Price + "G）";
                EventScreen.CardPick(g, root, of.Choice, "「" + of.Name + "」" + price, "departure",
                    delegate (int i) { g.DepartureChoiceIndex = -1; Audio.Ui("buy"); g.Do(new RunCommand_BuyDeparture { Index = ci, CardIndex = i }); },
                    "店に戻る", delegate { g.DepartureChoiceIndex = -1; g.Rebuild(); });
                return;
            }

            bool ph = UiKit.Phone;
            var cs = BattleScreen.CanvasSize(root);
            // スマホは見出しが上部バーの札に畳まれる (幅 560)。地の文が長いと題まで切れるので短い版に
            RunUi.Heading(root, Content.DEPARTURE_TITLE, ph ? "坑に降りる前に、行商の店で支度を整える" : Content.DEPARTURE_LEAD);

            // 行商の一言 (出自の工房＝リーダーの最初の色の里)
            float lineTop = ph ? RunUi.TopH + 6f : RunUi.TopH + 116f;
            float lineH = ph ? 42f : 58f;
            MasterLine(root, Content.DepartureMasterLine(run.Colors), lineTop, lineH, cs.x);

            float top = lineTop + lineH + (ph ? 8f : 18f);
            var cards = new List<int>(); var relics = new List<int>(); var services = new List<int>();
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i].Kind == "card") cards.Add(i);
                else if (offers[i].Kind == "relic") relics.Add(i);
                else services.Add(i);
            }
            // 普通の店と同じ並べ方 (2026-09-24 夜 ユーザー「種類名はいらない。普通の商店のように」): 左に札 (カードの面)、右の列に遺物、札の下にサービス
            float colW = ph ? 440f : 480f, side = ph ? 20f : 40f, gap = ph ? 10f : 14f;
            float leftW = cs.x - side * 2f - colW - (ph ? 20f : 40f);
            float svcH = ph ? 126f : 140f;
            float bottomPad = ph ? 12f : 30f;
            float shelfH = cs.y - top - svcH - gap - bottomPad;
            // 札の棚
            int nc = cards.Count;
            if (nc > 0)
            {
                float cgap = ph ? 16f : 28f, priceH = 56f;
                float scale = Mathf.Min(ph ? 0.9f : 1.3f, Mathf.Min((leftW - cgap * (nc - 1)) / nc / CardView.W, (shelfH - priceH) / CardView.H));
                float cw = CardView.W * scale, ch = CardView.H * scale;
                float tw = nc * cw + (nc - 1) * cgap;
                for (int k = 0; k < nc; k++)
                {
                    var cell = CardCell(g, root, run, cards[k], scale);
                    cell.anchorMin = cell.anchorMax = new Vector2(0f, 1f);
                    cell.pivot = new Vector2(0.5f, 1f);
                    cell.anchoredPosition = new Vector2(side + leftW / 2f - tw / 2f + cw / 2f + k * (cw + cgap), -top);
                    cell.sizeDelta = new Vector2(cw, ch + priceH);
                }
            }
            // サービスの段 (札の下)
            int ns = services.Count;
            if (ns > 0)
            {
                float sw = (leftW - gap * (ns - 1)) / ns;
                float sy = cs.y - bottomPad - svcH;
                for (int k = 0; k < ns; k++)
                {
                    var tile = Tile(g, root, run, services[k], sw, svcH);
                    tile.anchorMin = tile.anchorMax = new Vector2(0f, 1f);
                    tile.pivot = new Vector2(0f, 1f);
                    tile.anchoredPosition = new Vector2(side + k * (sw + gap), -sy);
                }
            }
            // 遺物の列 (右)
            float leaveH = ph ? 56f : 60f;
            float colX = cs.x - side - colW;
            float colH = cs.y - top - bottomPad - leaveH - gap;
            int nr = relics.Count;
            if (nr > 0)
            {
                float rh = Mathf.Min(ph ? 140f : 170f, (colH - gap * (nr - 1)) / nr);
                for (int k = 0; k < nr; k++)
                {
                    var tile = Tile(g, root, run, relics[k], colW, rh);
                    tile.anchorMin = tile.anchorMax = new Vector2(0f, 1f);
                    tile.pivot = new Vector2(0f, 1f);
                    tile.anchoredPosition = new Vector2(colX, -top - k * (rh + gap));
                }
            }
            var slot = UiKit.NewRect("leave-slot", root);
            UiKit.Anchor(slot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(colX, bottomPad), new Vector2(colX + colW, bottomPad + leaveH));
            LeaveButton(g, root, slot);
        }

        /// <summary>札1枚 (普通の店と同じ: カードの面＋下に値札。押すと拡大・買うのは値札)</summary>
        static RectTransform CardCell(GameRoot g, RectTransform root, RunState run, int idx, float scale)
        {
            var offer = run.Departure.Offers[idx];
            bool bought = Run.DepartureOfferBought(run, offer);
            bool ok = !bought && Run.DepartureOfferAvailable(run, offer);
            var cell = UiKit.NewRect("departure-" + offer.Id, root);
            CardDef def = null;
            try { def = Content.GetCardDef(offer.Choice.AddCardIds[0]); } catch (Exception) { }
            if (def != null)
            {
                var ci = new CardInstance { Uid = "departure" + idx, Def = def };
                var cv = CardView.Build(cell, ci, null, ok || bought, false, "shop-card");
                cv.anchorMin = cv.anchorMax = new Vector2(0.5f, 1f);
                cv.pivot = new Vector2(0.5f, 1f);
                cv.localScale = Vector3.one * scale;
                cv.anchoredPosition = Vector2.zero;
                if (bought) BoughtStamp(cv, false);
                else RewardScreen.HoverRaise(cv, delegate { Audio.Ui("click"); CardPopup.Open(g, ci, null); });   // タップ＝拡大。買うのは値札
                CardPopup.Attach(g, cv, ci, null, true);
            }
            var wrap = UiKit.NewRect("pricewrap", cell);
            UiKit.Anchor(wrap, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-88f, 0f), new Vector2(88f, 48f));
            if (bought) ShopScreen.PriceTag(wrap, offer.Price, "買った", false);
            else ShopScreen.PriceTag(wrap, offer.Price, null, ok, delegate { if (ok) { Audio.Ui("buy"); g.Do(new RunCommand_BuyDeparture { Index = idx }); } });
            return cell;
        }

        /// <summary>遺物・サービスの紙の札: 左に絵、名前、説明 (遺物は本文・サービスは中身)、右下に値札。買えない時は理由を朱で</summary>
        static RectTransform Tile(GameRoot g, RectTransform root, RunState run, int idx, float w, float h)
        {
            var offer = run.Departure.Offers[idx];
            bool ph = UiKit.Phone;
            bool bought = Run.DepartureOfferBought(run, offer);
            bool ok = !bought && Run.DepartureOfferAvailable(run, offer);
            bool needsCard = Run.EventChoiceNeedsCard(offer.Choice);
            var tile = UiKit.NewRect("departure-" + offer.Id, root);
            tile.sizeDelta = new Vector2(w, h);
            var edge = PaperFx.Sheet(tile, PaperFx.Panel, "edge", KindEdge(offer.Kind));
            UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f);
            edge.raycastTarget = false;
            var paper = PaperFx.Sheet(tile, PaperFx.Panel, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            string tip = Tip(offer);
            Tooltip.Attach(paper.gameObject, delegate { return tip; });
            float pad = ph ? 10f : 12f, artS = ph ? 48f : 64f;
            var art = Art(tile, offer, artS);
            art.anchorMin = art.anchorMax = new Vector2(0f, 1f);
            art.anchoredPosition = new Vector2(pad + artS / 2f, -pad - artS / 2f);
            float textL = pad + artS + 10f;
            string title = offer.Name;
            if (offer.Choice.RelicId != null)
            {
                try { title += "  <size=" + (ph ? 12 : 14) + ">" + UiKit.ColorTag(PaperFx.BrassInk, CardText.RarityLabel(Content.GetRelicDef(offer.Choice.RelicId).Rarity)) + "</size>"; } catch (Exception) { }
            }
            var nm = UiKit.Deco(tile, title, ph ? 17 : 20, PaperFx.Ink, TextAnchor.MiddleLeft);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Anchor(nm.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textL, ph ? -32f : -40f), new Vector2(-pad, -6f));
            nm.raycastTarget = false;
            // G が足りないだけなら値札が沈むので本文は残す。対象の札が無い等の理由だけ朱で出す
            string why = bought || ok || run.Gold < offer.Price ? null : "買えない: " + UnavailableReason(run, offer);
            string body = why ?? TileText(offer);
            float priceW = 176f;
            // 本文は名前の下に全幅、値札は右下の段
            var sm = UiKit.Txt(tile, body, ph ? 13 : 15, why != null ? PaperFx.BadInk : PaperFx.InkSoft, TextAnchor.UpperLeft);
            sm.textWrappingMode = TextWrappingModes.Normal; sm.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Anchor(sm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(textL, pad + 46f), new Vector2(-pad, ph ? -30f : -44f));
            sm.raycastTarget = false;
            var wrap = UiKit.NewRect("pricewrap", tile);
            UiKit.Anchor(wrap, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-pad - priceW, pad - 4f), new Vector2(-pad, pad + 44f));
            Action buy = delegate
            {
                if (needsCard) { Audio.Ui("click"); g.DepartureChoiceIndex = idx; g.Rebuild(); }
                else { Audio.Ui("buy"); g.Do(new RunCommand_BuyDeparture { Index = idx }); }
            };
            if (bought) ShopScreen.PriceTag(wrap, offer.Price, "買った", false);
            else ShopScreen.PriceTag(wrap, offer.Price, null, ok, delegate { if (ok) buy(); });
            if (bought) { BoughtStamp(tile, true); wrap.SetAsLastSibling(); }
            return tile;
        }

        /// <summary>買った品: 暗く沈めて「買った」の判</summary>
        static void BoughtStamp(RectTransform cell, bool small)
        {
            var cover = UiKit.Pan(cell, new Color(0f, 0f, 0f, 0.5f), "bought");
            UiKit.Stretch(cover.rectTransform, 0f, 0f, 0f, 0f);
            cover.raycastTarget = false;
            var stamp = UiKit.NewRect("bought-stamp", cover.transform);
            float sw = small ? 130f : 170f, sh = small ? 46f : 60f;
            UiKit.Anchor(stamp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-sw / 2f, -sh / 2f), new Vector2(sw / 2f, sh / 2f));
            var sImg = stamp.gameObject.AddComponent<Image>();
            sImg.sprite = PaperFx.Tag; sImg.type = Image.Type.Sliced; sImg.pixelsPerUnitMultiplier = 1f; sImg.color = PaperFx.Paper3; sImg.raycastTarget = false;
            stamp.localRotation = Quaternion.Euler(0f, 0f, -6f);
            var st = UiKit.Deco(stamp, "買った", small ? 22 : 30, PaperFx.BadInk, TextAnchor.MiddleCenter);
            UiKit.Stretch(st.rectTransform, 0f, 0f, 0f, 0f);
            st.raycastTarget = false;
        }

        /// <summary>札の本文の代わりの一文: 遺物は本文、ギアは名前、それ以外は台帳の文</summary>
        public static string TileText(DepartureOffer offer)
        {
            var ch = offer.Choice;
            if (ch.RelicId != null)
            {
                try { return Content.GetRelicDef(ch.RelicId).Description; } catch (Exception) { return offer.Text ?? ""; }
            }
            if (ch.Gears != null && ch.Gears.Count > 0) return Summary(offer);
            return offer.Text ?? "";
        }

        /// <summary>「店を出て坑へ」(常に押せる)。slot があればその枠の中央、無ければ画面下の中央</summary>
        static void LeaveButton(GameRoot g, RectTransform root, RectTransform slot)
        {
            Action leave = delegate { Audio.Ui("click"); g.DepartureChoiceIndex = -1; g.Do(new RunCommand_LeaveDeparture()); };
            if (slot == null) { RunUi.BottomButton(root, "店を出て坑へ", leave, 20, 320f, 56f, 0f, 36f, PaperFx.BrassLight); return; }
            var b = UiKit.Btn(slot, "店を出て坑へ", leave, 20, true, PaperFx.BrassLight);
            var le = b.GetComponent<LayoutElement>();
            if (le != null) UnityEngine.Object.Destroy(le);
            UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-140f, -30f), new Vector2(140f, 30f));
            // 「買わなかった物は、行商が幕1の店にも並べる」の一文は出さない (2026-09-24 夜 ユーザー裁定。仕組みは残る)
        }

        /// <summary>背景: 幕1の舞台 (坑口の森と櫓) をそのまま情景にする。他の画面より暗がりを薄く (坑口が見える)</summary>
        static void Backdrop(GameRoot g, RectTransform root)
        {
            BattleScreen.BuildBackground(root, g.Rs.Act);
            var dim = UiKit.Pan(root, new Color(0f, 0f, 0f, 0.36f), "dim");
            dim.raycastTarget = false;
            UiKit.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
            RunUi.TopBar(g, root, "坑口");
            RunUi.Message(g, root);
        }

        /// <summary>行商の一言の紙の帯 (「坑口の行商」＋ 里ごとの一言)</summary>
        static void MasterLine(RectTransform root, string line, float top, float h, float canvasW)
        {
            bool ph = UiKit.Phone;
            float w = Mathf.Min(ph ? 1200f : 1300f, canvasW - 40f);
            var rt = UiKit.NewRect("master-line", root);
            UiKit.Anchor(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-w / 2f, -top - h), new Vector2(w / 2f, -top));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = PaperFx.Tag2; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f;
            img.raycastTarget = false;
            float whoW = ph ? 110f : 132f;
            var who = UiKit.Deco(rt, "坑口の行商", ph ? 15 : 17, PaperFx.BrassInk, TextAnchor.MiddleLeft);
            UiKit.Anchor(who.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(20f, 0f), new Vector2(20f + whoW, 0f));
            who.raycastTarget = false;
            var t = UiKit.Txt(rt, line, ph ? 16 : 19, PaperFx.Ink, TextAnchor.MiddleLeft);
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.Anchor(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f + whoW + 8f, 0f), new Vector2(-20f, 0f));
            t.raycastTarget = false;
        }

        // ---- 行商の段 (ShopScreen) と共有する部品 ----

        /// <summary>支度の小さな絵 (遺物の絵・ギアの絵・サービスの印)。行商の段の札の左端に置く</summary>
        public static RectTransform Art(Transform parent, DepartureOffer offer, float size)
        {
            var ch = offer.Choice;
            if (ch.RelicId != null) return RunUi.RelicArt(parent, ch.RelicId, size).rectTransform;
            if (ch.Gears != null && ch.Gears.Count > 0)
            {
                var gd = GearUi.DefOf(ch.Gears[0]);
                var img = new GameObject("gear-art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                img.transform.SetParent(parent, false);
                img.sprite = ThemeFx.GearGlyph(ch.Gears[0], gd != null ? gd.Family : "general");
                img.preserveAspect = true; img.raycastTarget = false;
                img.rectTransform.sizeDelta = new Vector2(size, size);
                return img.rectTransform;
            }
            var icon = UiKit.Icon(parent, ch.AddCardIds != null && ch.AddCardIds.Count > 0 ? "star" : BodyIcon(ch), size);
            return icon.rectTransform;
        }

        /// <summary>中身の一行 (行商の段): 古代の遺物「〜」／ギア「A」「B」＋魔素20／札「〜」／サービスの文</summary>
        public static string Summary(DepartureOffer offer)
        {
            var ch = offer.Choice;
            var parts = new List<string>();
            if (ch.RelicId != null)
            {
                string rn = ch.RelicId;
                try { rn = Content.GetRelicDef(ch.RelicId).Name; } catch (Exception) { }
                parts.Add("古代の遺物「" + rn + "」");
            }
            if (ch.Gears != null && ch.Gears.Count > 0)
            {
                var sb = new System.Text.StringBuilder("ギア");
                foreach (var id in ch.Gears) { var gd = GearUi.DefOf(id); sb.Append("「" + (gd != null ? gd.Name : id) + "」"); }
                parts.Add(sb.ToString());
            }
            if (ch.Mana.HasValue) parts.Add("魔素" + ch.Mana.Value);
            if (ch.AddCardIds != null) foreach (var id in ch.AddCardIds) parts.Add("札「" + CardText.CardName(id) + "」");
            if (parts.Count == 0) return offer.Text ?? "";
            return string.Join("＋", parts.ToArray());
        }

        /// <summary>説明文 (ホバー・スマホはタップで固定パネル): 名前・種類・文・中身の本文</summary>
        public static string Tip(DepartureOffer offer)
        {
            var ch = offer.Choice;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>" + offer.Name + "</b>  " + offer.Price + "G\n" + offer.Text);
            if (ch.RelicId != null)
            {
                try { var rd = Content.GetRelicDef(ch.RelicId); sb.Append("\n<b>" + rd.Name + "</b>: " + rd.Description); } catch (Exception) { }
            }
            if (ch.Gears != null) foreach (var id in ch.Gears) { var gd = GearUi.DefOf(id); if (gd != null) sb.Append("\n<b>" + gd.Name + "</b>: " + gd.Text); }
            if (ch.AddCardIds != null)
                foreach (var id in ch.AddCardIds)
                {
                    try { var cd = Content.GetCardDef(id); sb.Append("\n<b>" + cd.Name + "</b>: " + CardText.Body(cd)); } catch (Exception) { }
                }
            return sb.ToString();
        }

        /// <summary>出立の店で買えない理由 (買った・G・対象の札)</summary>
        public static string UnavailableReason(RunState run, DepartureOffer offer)
        {
            if (Run.DepartureOfferBought(run, offer)) return "買った";
            if (run.Gold < offer.Price) return "G が足りない";
            return UnavailableReason(run, offer.Choice);
        }

        /// <summary>選べない理由 (engine の EventChoiceAvailable が false の時)</summary>
        public static string UnavailableReason(RunState run, EventChoiceDef ch)
        {
            if (ch.RequireGold.HasValue && run.Gold < ch.RequireGold.Value) return "G が足りない";
            if (ch.RelicId != null && System.Linq.Enumerable.Contains(run.Relics, ch.RelicId)) return "その遺物はもう持っている";
            if (ch.RemoveCard == true && run.Deck.Count <= 5) return "デッキが5枚以下";
            if (ch.UpgradeCard == true) return "鍛えられる札がない";
            if (ch.TransformCard == true) return "変成できる札がない";
            return "対象の札がない";
        }

        // ---- 小道具 ----

        /// <summary>紙の外線: 墨 (真鍮の縁は「選択中」の色なので使わない = docs/color-theme.md)。前借りの朱は撤去 (2026-09-24 夜)</summary>
        public static Color KindEdge(string kind) { return PaperFx.Ink; }

        static string BodyIcon(EventChoiceDef ch)
        {
            if (ch.RemoveCard == true) return "exhaust";
            if (ch.UpgradeCard == true) return "hammer";
            if (ch.MaxHp.HasValue || ch.Hp.HasValue || ch.HpRatio.HasValue) return "heart";
            if (ch.Gold.HasValue) return "gold";
            return "star";
        }

    }
}
