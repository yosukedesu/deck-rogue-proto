// ShopScreen.cs — ショップ (M3・2026-09-07): 棚にカードを並べ値札を付ける。除去・鍛えるはデッキのグリッドから選ぶ
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game
{
    public static class ShopScreen
    {
        public static void Build(GameRoot g, RectTransform root)
        {
            var run = g.Rs;
            var shop = run.Shop;
            RewardScreen.Backdrop(g, root, "ショップ");

            if (shop == null)
            {
                RunUi.Heading(root, "ショップ", "在庫がありません");
                RunUi.BottomButton(root, "出る", delegate { g.Do(new RunCommand_ShopLeave()); }, 18, 220f, 50f);
                return;
            }

            int rmPrice = DeckRogue.Engine.Run.ShopRemovalPrice(run);
            int upPrice = DeckRogue.Engine.Run.ShopUpgradePrice(run);

            if (g.ShopMode == "gears") { BuildGears(g, root, run, shop); return; }   // ギアの棚 (2026-09-17 裁定2: サービス欄のボタン→専用画面)
            if (g.ShopMode != null)
            {
                bool removing = g.ShopMode == "remove";
                RunUi.Heading(root, removing ? "カード除去 (" + rmPrice + "G)" : "鍛える (" + upPrice + "G)",
                    removing ? "デッキから1枚を永久に取り除く" : UiKit.Phone ? "1枚選ぶ。「鍛えた後を見る」で全部の札が鍛えた後の姿に" : "1枚選ぶ。札に触れると元と鍛えた後が並ぶ（長押しで拡大）");
                RectTransform preview = removing || UiKit.Phone ? null : CampfireScreen.ForgePreviewArea(root);   // スマホは並びを出さず「鍛えた後を見る」のチェック (2026-09-16 案A)
                var area = UiKit.NewRect("svc", root);
                RunUi.PickArea(root, area, preview != null ? CampfireScreen.ForgePreviewH : 0f);
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return removing ? "除去" : (Upgrade.CanUpgradeCard(c) ? "鍛える" : null); },
                    delegate (int i, CardInstance c) { return removing ? run.Deck.Count > 5 : Upgrade.CanUpgradeCard(c); },
                    delegate (int i)
                    {
                        bool rm = g.ShopMode == "remove";
                        g.ShopMode = null;
                        Audio.Ui(rm ? "remove" : "upgrade");
                        if (rm) g.Do(new RunCommand_ShopRemove { Index = i });
                        else g.Do(new RunCommand_ShopUpgrade { Index = i });
                    },
                    removing ? 500f : 400f, pickKey: removing ? "shop-remove" : "shop-upgrade", confirmRoot: root);
                if (!removing) { CampfireScreen.AttachUpgradeTips(area, run.Deck); if (preview != null) CampfireScreen.AttachForgePreview(g, area, run.Deck, preview); }
                RunUi.BackButton(root, "戻る", delegate { g.ShopMode = null; g.Rebuild(); });
                return;
            }

            RunUi.Heading(root, "ショップ", (UiKit.Phone ? "カードをタップで購入。所持金 " : "カードをクリックで購入。所持金 ") + run.Gold + "G", RunUi.TopH + 24f, UiKit.Phone ? 440f : 0f);
            float shelfTop = RunUi.SceneWindow(root, "shop") ? RunUi.SceneBottom : RunUi.TopH + 110f;   // 情景の窓があれば棚をその下へ

            // 棚 (カード)
            // 棚は左端〜右パネルの手前 (画面幅から出す。スマホの 1800 幅では中央固定だと6枚目がパネルに隠れた。2026-09-09)
            var shelf = UiKit.NewRect("shelf", root);
            UiKit.Anchor(shelf, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -shelfTop - 410f), new Vector2(-480f, -shelfTop));
            float rootW = root.rect.width > 0f ? root.rect.width : 1920f;
            float availW = rootW - 40f - 480f;
            int n = shop.Cards.Count;
            float gap = 24f;
            float scale = Mathf.Min(0.95f, (availW - Math.Max(0, n - 1) * gap) / Math.Max(1, n) / CardView.W);
            float cardW = CardView.W * scale, cardH = CardView.H * scale;
            float totalW = n * cardW + Math.Max(0, n - 1) * gap;
            float x0 = -totalW / 2f + cardW / 2f;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var item = shop.Cards[i];
                CardDef def = null;
                try { def = Content.GetCardDef(item.Id); } catch (Exception) { }
                if (def == null) continue;
                bool sold = item.Sold == true;
                bool canBuy = !sold && run.Gold >= item.Price;
                var cell = UiKit.NewRect("shop" + i, shelf);
                cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0.5f);
                cell.sizeDelta = new Vector2(cardW, cardH + 60f);
                cell.anchoredPosition = new Vector2(x0 + i * (cardW + gap), 0f);
                var ci = new CardInstance { Uid = "shop" + i, Def = def };
                var cv = CardView.Build(cell, ci, null, canBuy, false, "shop-card");
                cv.localScale = Vector3.one * scale;
                cv.anchoredPosition = new Vector2(0f, 30f);
                if (sold)
                {
                    var cover = UiKit.Pan(cv, new Color(0f, 0f, 0f, 0.6f), "sold");
                    UiKit.Stretch(cover.rectTransform, 0f, 0f, 0f, 0f);
                    var st = UiKit.Txt(cover.transform, "売切", 40, UiKit.ColBad, TextAnchor.MiddleCenter, true);
                    UiKit.Stretch(st.rectTransform, 0f, 0f, 0f, 0f);
                }
                else RewardScreen.HoverRaise(cv, delegate { if (canBuy) { Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyCard { Index = idx }); } });
                CardPopup.Attach(g, cv, ci, null, true);
                PriceTag(cell, item.Price, sold ? "売切" : null, canBuy);
                if (i == n - 1 && n >= 6)
                {
                    var rare = UiKit.Txt(cell, "★ レア枠", 13, UiKit.ColGoldInk, TextAnchor.MiddleCenter, true);
                    UiKit.Anchor(rare.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-60f, 4f), new Vector2(60f, 26f));
                }
            }

            // レリック + サービス (右列)
            var side = UiKit.Frame(root, Theme.Panel, Color.white, "services", 3f);
            // スマホは上下いっぱいに使い、余白を詰める (高さ 675 では 250 のレリック札と 2 つのサービスが入らなかった。2026-09-14)
            bool ph = UiKit.Phone;
            UiKit.Anchor(side.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-420f, ph ? 100f : 120f), new Vector2(-40f, -(RunUi.TopH + (ph ? 12f : 100f))));
            var sv = UiKit.Vert(side.transform, ph ? 8 : 14, ph ? 12 : 20);
            sv.childForceExpandHeight = false;
            UiKit.Head(side.transform, "店主のサービス", 20);
            if (shop.RelicId != null)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(shop.RelicId); } catch (Exception) { }
                float rh = 250f;
                var rp = RewardScreen.RelicPanel(side.transform, rd, shop.RelicId, 340f, rh);
                UiKit.Le(rp, 340f, rh, 340f, rh);
                bool canRelic = run.Gold >= shop.RelicPrice;
                var rb = UiKit.Btn(rp, shop.RelicPrice + "G で買う", delegate { Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyRelic()); }, 16, canRelic, PaperFx.BrassLight);
                var rle = rb.GetComponent<LayoutElement>();
                if (rle != null) UnityEngine.Object.Destroy(rle);
                UiKit.Anchor(rb.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-110f, 12f), new Vector2(110f, 54f));
            }
            else
            {
                var none = UiKit.Txt(side.transform, "レリックは売切", 15, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(none, -1f, 40f, -1f, 40f);
            }
            ServiceBtn(side.transform, "カード除去  " + rmPrice + "G", run.Gold >= rmPrice && run.Deck.Count > 5, delegate { g.ShopMode = "remove"; g.Rebuild(); });
            ServiceBtn(side.transform, "鍛える  " + upPrice + "G", run.Gold >= upPrice, delegate { g.ShopMode = "upgrade"; g.Rebuild(); });
            int shelfN = shop.Gears != null ? shop.Gears.Count : 0;
            ServiceBtn(side.transform, "ギアの棚  " + shelfN + "枠 ／ 魔素 " + (shop.ManaPrice ?? DeckRogue.Engine.Run.SHOP_MANA_PRICE) + "G", true, delegate { g.ShopMode = "gears"; g.Rebuild(); });
            if (!ph)
            {
                var note = UiKit.Txt(side.transform, "除去・鍛えるは使うたび値上がり (ラン通算)", 12, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
                UiKit.Le(note, -1f, 24f, -1f, 24f);
            }

            RunUi.BottomButton(root, "店を出る", delegate { g.ShopMode = null; g.Do(new RunCommand_ShopLeave()); }, 18, 260f, 52f, ph ? -300f : -100f, ph ? 24f : 40f);
        }

        /// <summary>
        /// ギアの棚 (2026-09-17 ユーザー裁定2): 除去/鍛えると同じ下位モードの専用画面。3枠の棚＋魔素の購入＋持ち物の整理 (捨てる／満杯なら入れ替え)。PC/スマホ共通
        /// </summary>
        static void BuildGears(GameRoot g, RectTransform root, RunState run, ShopState shop)
        {
            bool ph = UiKit.Phone;
            var cs = BattleScreen.CanvasSize(root);
            var gears = DeckRogue.Engine.Run.GearsOf(run);
            int mana = DeckRogue.Engine.Run.ManaOf(run);
            RunUi.Heading(root, "ギアの棚", "自ターンに1個・魔素を払って組む。持ち物 " + gears.Count + "/" + Gears.GEAR_CARRY_MAX + "・魔素 " + Gears.ManaLabel(mana) + "・所持金 " + run.Gold + "G");
            var shelf = shop.Gears ?? new List<ShopStateGears>();
            int manaPrice = shop.ManaPrice ?? DeckRogue.Engine.Run.SHOP_MANA_PRICE;
            float scale = ph ? 0.8f : 1f;
            float cw = 200f * scale, chh = 272f * scale, gap = 40f, btnH = 70f;
            int n = shelf.Count + 1;   // 3枠 + 魔素の札
            float totalW = n * cw + (n - 1) * gap;
            float top = ph ? RunUi.TopH + 20f : RunUi.TopH + 120f;   // 棚の上端 (キャンバス上から)
            var shelfRt = UiKit.NewRect("gear-shelf", root);
            UiKit.Anchor(shelfRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-totalW / 2f, -top - chh - btnH), new Vector2(totalW / 2f, -top));
            for (int i = 0; i < shelf.Count; i++)
            {
                int idx = i;
                var item = shelf[i];
                var def = GearUi.DefOf(item.Id);
                bool sold = item.Sold == true;
                bool canBuy = !sold && run.Gold >= item.Price;
                var cell = UiKit.NewRect("gshop" + i, shelfRt);
                UiKit.Anchor(cell, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(i * (cw + gap), 0f), new Vector2(i * (cw + gap) + cw, 0f));
                var card = GearUi.Card(cell, def, item.Id, 200f, 272f, null, scale);
                card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f); card.pivot = new Vector2(0.5f, 1f); card.anchoredPosition = Vector2.zero;
                if (sold)
                {
                    var cover = UiKit.Pan(card, new Color(0f, 0f, 0f, 0.6f), "sold");
                    UiKit.Stretch(cover.rectTransform, 0f, 0f, 0f, 0f);
                    var st = UiKit.Txt(cover.transform, "売切", 40, UiKit.ColBad, TextAnchor.MiddleCenter, true);
                    UiKit.Stretch(st.rectTransform, 0f, 0f, 0f, 0f);
                }
                var tag = UiKit.NewRect("pricewrap", cell);
                UiKit.Anchor(tag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-cw / 2f, 0f), new Vector2(cw / 2f, btnH));
                PriceTag(tag, item.Price, sold ? "売切" : null, canBuy);
                var b = UiKit.Btn(cell, sold ? "売切" : "買う", delegate
                {
                    Audio.Ui("buy");
                    if (DeckRogue.Engine.Run.GearFull(g.Rs)) { g.GearSwap = "shop:" + idx; g.Rebuild(); }
                    else g.Do(new RunCommand_ShopBuyGear { Index = idx });
                }, 15, canBuy, PaperFx.BrassLight);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) UnityEngine.Object.Destroy(le);
                UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-cw / 2f + 10f, 40f), new Vector2(cw / 2f - 10f, btnH + 14f));
            }
            {   // 魔素の札: ギアを組む動力 (1個ぶん。上限50)
                int i = shelf.Count;
                var cell = UiKit.NewRect("gshop-mana", shelfRt);
                UiKit.Anchor(cell, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(i * (cw + gap), 0f), new Vector2(i * (cw + gap) + cw, 0f));
                // 魔素の札はギアの札と同じ器 (GearUi.Card) に仮の定義を流し込む = 大きさと位置が棚と揃う
                var manaDef = new GearDef { Id = "mana", Name = "魔素", Rarity = "common", Family = "interfere", Text = "ギアを組む動力。組むたび " + Gears.GEAR_MANA_COST + " 使う", Effects = new List<DeclarativeEffect>() };
                var card = GearUi.Card(cell, manaDef, "mana", 200f, 272f, null, scale, "1個ぶん（" + Gears.GEAR_MANA_COST + "）", "上限 " + Gears.MANA_MAX + "\nいま " + Gears.ManaLabel(mana), PaperFx.Mana);
                card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f); card.pivot = new Vector2(0.5f, 1f); card.anchoredPosition = Vector2.zero;
                bool full = mana >= Gears.MANA_MAX;
                bool canBuy = !full && run.Gold >= manaPrice;
                var tag = UiKit.NewRect("pricewrap", cell);
                UiKit.Anchor(tag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-cw / 2f, 0f), new Vector2(cw / 2f, btnH));
                PriceTag(tag, manaPrice, full ? "上限" : null, canBuy);
                var b = UiKit.Btn(cell, full ? "魔素は上限" : "買う", delegate { Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyMana()); }, 15, canBuy, PaperFx.BrassLight);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) UnityEngine.Object.Destroy(le);
                UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-cw / 2f + 10f, 40f), new Vector2(cw / 2f - 10f, btnH + 14f));
            }
            // 持ち物の整理: トークンの列＋「捨てる」
            float invTop = top + chh + btnH + 18f;
            float tw = 64f, th = 66f;
            float invW = Math.Max(420f, gears.Count * (tw + 14f));
            var inv = UiKit.NewRect("gear-inv", root);
            UiKit.Anchor(inv, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-invW / 2f, -invTop - 130f), new Vector2(invW / 2f, -invTop));
            var lbl = UiKit.Txt(inv, "持ち物 " + gears.Count + " / " + Gears.GEAR_CARRY_MAX + (gears.Count == 0 ? "（まだ持っていない）" : "　「捨てる」で枠を整理する（戻せない）"), 15, UiKit.ColText, TextAnchor.MiddleLeft);
            lbl.outlineWidth = 0.2f; lbl.outlineColor = new Color(0f, 0f, 0f, 0.7f);
            UiKit.Anchor(lbl.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -24f), new Vector2(0f, 0f));
            for (int i = 0; i < gears.Count; i++)
            {
                int idx = i;
                var cell = UiKit.NewRect("inv" + i, inv);
                UiKit.Anchor(cell, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * (tw + 14f), -30f - th - 32f), new Vector2(i * (tw + 14f) + tw, -30f));
                var tok = GearUi.Token(g, cell, run, null, -1, gears[i], tw, th, true, false);
                UiKit.Anchor(tok, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -th), new Vector2(tw, 0f));
                var b = UiKit.Btn(cell, "捨てる", delegate { Audio.Ui("remove"); g.Do(new RunCommand_DiscardGear { Index = idx }); }, 12);
                var le = b.GetComponent<LayoutElement>();
                if (le != null) UnityEngine.Object.Destroy(le);
                UiKit.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 30f));
            }
            RunUi.BackButton(root, "戻る", delegate { g.ShopMode = null; g.GearSwap = null; g.Rebuild(); });
            if (g.GearSwap != null && g.GearSwap.StartsWith("shop:"))
            {
                int si; if (int.TryParse(g.GearSwap.Substring(5), out si) && si >= 0 && si < shelf.Count)
                    GearUi.BuildSwapPicker(g, root, run, shelf[si].Id, delegate (int idx) { g.GearSwap = null; Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyGear { Index = si, DiscardIndex = idx }); });
                else g.GearSwap = null;
            }
        }

        static void ServiceBtn(Transform parent, string label, bool enabled, Action onClick)
        {
            var b = UiKit.Btn(parent, label, onClick, 17, enabled);
            UiKit.Le(b, -1f, 48f, -1f, 48f);
        }

        public static void PriceTag(RectTransform cell, int price, string over, bool affordable)
        {
            var tag = UiKit.NewRect("price", cell);
            UiKit.Anchor(tag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-70f, 4f), new Vector2(70f, 40f));
            var bg = tag.gameObject.AddComponent<Image>();
            bg.sprite = Theme.Tag; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1f;
            bg.color = over != null ? new Color(0.5f, 0.5f, 0.5f, 1f) : affordable ? Color.white : new Color(0.7f, 0.55f, 0.55f, 1f);
            var hg = UiKit.Horz(tag, 4, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
            if (over == null) UiKit.Icon(tag, "gold", 22f);
            var t = UiKit.Txt(tag, over ?? (price + " G"), 17, over != null ? UiKit.ColInkSoft : affordable ? UiKit.ColGoldInk : UiKit.ColBadInk, TextAnchor.MiddleCenter, true);
            UiKit.Le(t, 50f, 30f, -1f, 30f);
        }
    }
}
