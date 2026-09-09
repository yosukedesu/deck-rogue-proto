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

            if (g.ShopMode != null)
            {
                bool removing = g.ShopMode == "remove";
                RunUi.Heading(root, removing ? "カード除去 (" + rmPrice + "G)" : "鍛える (" + upPrice + "G)",
                    removing ? "デッキから1枚を永久に取り除く" : "1枚選ぶ。札に触れると元と鍛えた後が並ぶ（長押しで拡大）");
                RectTransform preview = removing ? null : CampfireScreen.ForgePreviewArea(root);
                var area = UiKit.NewRect("svc", root);
                UiKit.Anchor(area, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-760f, 110f), new Vector2(760f, -(RunUi.TopH + 110f + (removing ? 0f : CampfireScreen.ForgePreviewH))));
                UiKit.Vert(area, 0, 0);
                RunUi.CardGrid(g, area, run.Deck,
                    delegate (int i, CardInstance c) { return removing ? "除去" : (Upgrade.CanUpgradeCard(c) ? "鍛える" : null); },
                    delegate (int i, CardInstance c) { return removing ? run.Deck.Count > 5 : Upgrade.CanUpgradeCard(c); },
                    delegate (int i)
                    {
                        bool rm = g.ShopMode == "remove";
                        g.ShopMode = null;
                        Audio.Play(rm ? "card_set" : "buff", 0.8f);
                        if (rm) g.Do(new RunCommand_ShopRemove { Index = i });
                        else g.Do(new RunCommand_ShopUpgrade { Index = i });
                    },
                    removing ? 500f : 400f);
                if (!removing) { CampfireScreen.AttachUpgradeTips(area, run.Deck); CampfireScreen.AttachForgePreview(g, area, run.Deck, preview); }
                RunUi.BottomButton(root, "戻る", delegate { g.ShopMode = null; g.Rebuild(); }, 18, 220f, 50f);
                return;
            }

            RunUi.Heading(root, "ショップ", "カードをクリックで購入。所持金 " + run.Gold + "G");

            // 棚 (カード)
            // 棚は左端〜右パネルの手前 (画面幅から出す。スマホの 1800 幅では中央固定だと6枚目がパネルに隠れた。2026-09-09)
            var shelf = UiKit.NewRect("shelf", root);
            UiKit.Anchor(shelf, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -RunUi.TopH - 520f), new Vector2(-480f, -RunUi.TopH - 110f));
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
                else RewardScreen.HoverRaise(cv, delegate { if (canBuy) { Audio.Play("energy", 0.7f); g.Do(new RunCommand_ShopBuyCard { Index = idx }); } });
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
            UiKit.Anchor(side.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-420f, 120f), new Vector2(-40f, -(RunUi.TopH + 100f)));
            var sv = UiKit.Vert(side.transform, 14, 20);
            sv.childForceExpandHeight = false;
            UiKit.Head(side.transform, "店主のサービス", 20);
            if (shop.RelicId != null)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(shop.RelicId); } catch (Exception) { }
                var rp = RewardScreen.RelicPanel(side.transform, rd, shop.RelicId, 340f, 250f);
                UiKit.Le(rp, 340f, 250f, 340f, 250f);
                bool canRelic = run.Gold >= shop.RelicPrice;
                var rb = UiKit.Btn(rp, shop.RelicPrice + "G で買う", delegate { Audio.Play("buff", 0.7f); g.Do(new RunCommand_ShopBuyRelic()); }, 16, canRelic, UiKit.Hex("#f0d58a"));
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
            var note = UiKit.Txt(side.transform, "除去・鍛えるは使うたび値上がり (ラン通算)", 12, UiKit.ColInkSoft, TextAnchor.MiddleCenter);
            UiKit.Le(note, -1f, 24f, -1f, 24f);

            RunUi.BottomButton(root, "店を出る", delegate { g.ShopMode = null; g.Do(new RunCommand_ShopLeave()); }, 18, 260f, 52f, -100f);
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
