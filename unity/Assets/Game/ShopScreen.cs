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
            if (g.ShopMode != null && g.ShopMode.StartsWith("depart-pick:"))
            {   // 行商が預かった支度 (2026-09-24) の除去・鍛え: ?イベントと同じ「デッキから1枚選ぶ」画面
                int di;
                var slots = shop.Departures;
                var offer = int.TryParse(g.ShopMode.Substring("depart-pick:".Length), out di) && slots != null && di >= 0 && di < slots.Count ? LeftoverOffer(run, slots[di].Id) : null;
                if (offer != null)
                {
                    int price = slots[di].Price;
                    EventScreen.CardPick(g, root, offer.Choice, "「" + offer.Name + "」 (" + price + "G)", "shop-departure",
                        delegate (int i) { g.ShopMode = null; Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyDeparture { Index = di, CardIndex = i }); },
                        "戻る", delegate { g.ShopMode = null; g.Rebuild(); });
                    return;
                }
                g.ShopMode = null;
            }
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

            RunUi.Heading(root, "ショップ", (UiKit.Phone ? "札はタップで拡大。買うのは値札のボタン。所持金 " : "札はクリックで拡大。買うのは値札のボタン。所持金 ") + run.Gold + "G", RunUi.TopH + 24f, UiKit.Phone ? 440f : 0f);
            bool scene = RunUi.SceneWindow(root, "shop");
            float shelfTop = scene ? RunUi.SceneBottom : RunUi.TopH + 110f;   // 情景の窓があれば棚をその下へ
            // 行商が預かった支度 (出立の店の売れ残り 2026-09-24。幕1の店だけ): 見出しと棚のあいだの段。PC は情景の窓の右、スマホは上部バーのすぐ下
            // 坑口で買わなかった札は普通の札と同じ棚に並べ、遺物・薬草・道具箱だけを段に置く (2026-09-24 夜)
            var leftCards = new List<int>(); var leftOthers = new List<int>();
            if (shop.Departures != null)
                for (int d = 0; d < shop.Departures.Count; d++)
                {
                    var lo = LeftoverOffer(run, shop.Departures[d].Id);
                    if (lo != null && shop.Departures[d].Sold != true && lo.Kind == "card" && lo.Choice.AddCardIds != null && lo.Choice.AddCardIds.Count > 0) leftCards.Add(d);
                    else if (lo != null && shop.Departures[d].Sold != true) leftOthers.Add(d);
                }
            if (leftOthers.Count > 0)
            {
                float used = DepartureBand(g, root, run, shop, scene, leftOthers);
                if (!UiKit.Phone && !scene) shelfTop += used;   // 情景の窓が無い PC は棚を段の下へ
            }

            // 棚 (カード)
            // 棚は左端〜右パネルの手前 (画面幅から出す。スマホの 1800 幅では中央固定だと6枚目がパネルに隠れた。2026-09-09)
            var shelf = UiKit.NewRect("shelf", root);
            UiKit.Anchor(shelf, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -shelfTop - 410f), new Vector2(-480f, -shelfTop));
            float rootW = root.rect.width > 0f ? root.rect.width : 1920f;
            float availW = rootW - 40f - 480f;
            int nShop = shop.Cards.Count;
            int n = nShop + leftCards.Count;
            float gap = 24f;
            float scale = Mathf.Min(0.95f, (availW - Math.Max(0, n - 1) * gap) / Math.Max(1, n) / CardView.W);
            // スマホで札が多い (坑口の札が残った幕1の店＝9枚) 時は2段に折る (2026-09-30 F11: 1段だと 0.42 倍まで縮み、値札のボタンが隣と約90px 重なって
            // 「で買う」が隠れ、左端のボタンは画面の外へ出た)。棚は「店を出る」の上まで下へ伸ばし、段の中は間を広げて値札を並べる
            int rows = 1, perRow = n;
            if (UiKit.Phone && n >= 5 && scale < 0.5f)
            {
                rows = 2; perRow = (n + 1) / 2;
                float canvasH = BattleScreen.CanvasSize(root).y;
                float shelfH = canvasH - shelfTop - 84f;
                UiKit.Anchor(shelf, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -shelfTop - shelfH), new Vector2(-480f, -shelfTop));
                scale = Mathf.Min(0.95f, Mathf.Min((availW - (perRow - 1) * gap) / perRow / CardView.W, (shelfH - 2f * 60f - 8f) / 2f / CardView.H));
                float cw0 = CardView.W * scale;
                gap = perRow > 1 ? Mathf.Min(88f, (availW - perRow * cw0) / (perRow - 1)) : gap;
            }
            float cardW = CardView.W * scale, cardH = CardView.H * scale;
            float totalW = perRow * cardW + Math.Max(0, perRow - 1) * gap;
            float x0 = -totalW / 2f + cardW / 2f;
            float tagW = Mathf.Min(176f, cardW + gap - 8f);   // 値札は段の間隔に収める (隣と重ねない)
            Func<int, Vector2> cellPos = i =>
            {
                int col = i % perRow, row = i / perRow;
                float y = rows == 2 ? (row == 0 ? 1f : -1f) * (cardH + 68f) / 2f : 0f;
                return new Vector2(x0 + col * (cardW + gap), y);
            };
            for (int i = nShop; i < n; i++)
            {   // 坑口で買わなかった札 (普通の札と同じ見た目・値段は棚の値段)
                int di = leftCards[i - nShop];
                var dslot = shop.Departures[di];
                var dof = LeftoverOffer(run, dslot.Id);
                CardDef ddef = null;
                try { ddef = Content.GetCardDef(dof.Choice.AddCardIds[0]); } catch (Exception) { }
                if (ddef == null) continue;
                bool dOk = run.Gold >= dslot.Price;
                var dcell = UiKit.NewRect("shop-dep" + i, shelf);
                dcell.anchorMin = dcell.anchorMax = new Vector2(0.5f, 0.5f);
                dcell.sizeDelta = new Vector2(cardW, cardH + 60f);
                dcell.anchoredPosition = cellPos(i);
                var dci = new CardInstance { Uid = "shop-dep" + i, Def = ddef };
                var dcv = CardView.Build(dcell, dci, null, dOk, false, "shop-card");
                dcv.localScale = Vector3.one * scale;
                dcv.anchoredPosition = new Vector2(0f, 30f);
                RewardScreen.HoverRaise(dcv, delegate { Audio.Ui("click"); CardPopup.Open(g, dci, null); });
                CardPopup.Attach(g, dcv, dci, null, true);
                PriceTag(dcell, dslot.Price, null, dOk, delegate { if (dOk) { Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyDeparture { Index = di }); } }, tagW);
            }
            for (int i = 0; i < nShop; i++)
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
                cell.anchoredPosition = cellPos(i);
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
                else RewardScreen.HoverRaise(cv, delegate { Audio.Ui("click"); CardPopup.Open(g, ci, null); });   // タップ＝拡大 (説明)。買うのは値札のボタン (2026-09-22 報酬と同じ作法)
                CardPopup.Attach(g, cv, ci, null, true);
                bool rareSlot = i == nShop - 1 && nShop >= 6;
                PriceTag(cell, item.Price, sold ? "売切" : null, canBuy, sold ? null : (Action)delegate { if (canBuy) { Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyCard { Index = idx }); } }, tagW, rareSlot && rows == 2);
                if (rareSlot && rows == 1)
                {   // 夜の上の注記は夜色の札に (2026-09-30 F11: 旧は真鍮の墨を夜空に直に置いて 2.1:1)。2段の時は段の間が狭いので値札の頭に「★」
                    var rare = PaperFx.NightNote(cell, "★ レア枠", 13, 140f, true, "rare");
                    rare.anchorMin = rare.anchorMax = new Vector2(0.5f, 1f); rare.pivot = new Vector2(0.5f, 0f);
                    rare.anchoredPosition = new Vector2(0f, 2f);
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
            RunUi.Heading(root, "ギアの棚", "自ターンに1個・魔素を払って組む。持ち物 " + gears.Count + "/" + Gears.GEAR_CARRY_MAX + "・魔素 " + GearUi.ManaText(mana) + "・所持金 " + run.Gold + "G");
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
                var card = GearUi.Card(cell, manaDef, "mana", 200f, 272f, null, scale, "1個ぶん（" + Gears.GEAR_MANA_COST + "）", "上限 " + Gears.MANA_MAX + "\nいま " + GearUi.ManaText(mana), PaperFx.Mana);
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
            float tw = GearUi.PhoneTokenW, th = GearUi.PhoneTokenH;   // ギアのトークンの幅にそろえる (F41)
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

        /// <summary>
        /// 「行商が預かった支度」の段 (2026-09-24 出立の支度。docs/departure-proposal-2026-09-24.md §1-3): 出立の店で買わなかった「物」を行商が担いで降り、幕1の店に並べる。
        /// 札ごとに 絵・名前・中身・値段 (「N G で買う」/売切)。札に触れると中身の説明 (スマホはタップで固定パネル)。除去・鍛えは値札を押すと札を選ぶ画面へ。
        /// 返り値は段が使った高さ (PC で情景の窓が無い時に棚を下げる量)
        /// </summary>
        static float DepartureBand(GameRoot g, RectTransform root, RunState run, ShopState shop, bool scene, List<int> which)
        {
            bool ph = UiKit.Phone;
            var cs = BattleScreen.CanvasSize(root);
            var slots = shop.Departures;
            // 置き場: PC は情景の窓 (左 40〜540) の右〜右の列 (右端 -480) の手前、見出しの説明の下。スマホは上部バーの下〜棚の上 (見出しは上部バーに畳まれている)
            float left = ph ? 20f : (scene ? 580f : 40f);
            float right = cs.x - (ph ? 440f : 500f);
            float top = ph ? RunUi.TopH + 6f : RunUi.TopH + 116f;
            float labelH = ph ? 20f : 26f;
            float tileH = ph ? 80f : 112f;
            var lbl = UiKit.Txt(root, "坑口の品", ph ? 13 : 15, UiKit.ColText, TextAnchor.MiddleLeft, true);
            lbl.outlineWidth = 0.2f; lbl.outlineColor = new Color(0f, 0f, 0f, 0.7f);
            UiKit.Anchor(lbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left + 4f, -top - labelH), new Vector2(right, -top));
            lbl.raycastTarget = false;
            int n = which.Count;
            float gap = ph ? 12f : 20f;
            float w = Mathf.Min(ph ? 480f : 420f, (right - left - gap * (n - 1)) / n);
            float y = top + labelH + 4f;
            for (int i = 0; i < n; i++)
            {
                int idx = which[i];
                var slot = slots[idx];
                var offer = LeftoverOffer(run, slot.Id);
                bool sold = slot.Sold == true || offer == null;
                bool available = !sold && Run.EventChoiceAvailable(run, offer.Choice);   // 店の値段は slot.Price (物価の倍率込み)。出立の店の値段・購入済みは見ない
                bool canBuy = available && run.Gold >= slot.Price;
                var tile = UiKit.NewRect("departure-" + slot.Id, root);
                UiKit.Anchor(tile, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left + i * (w + gap), -y - tileH), new Vector2(left + i * (w + gap) + w, -y));
                var edge = PaperFx.Sheet(tile, PaperFx.Panel, "edge", DepartureScreen.KindEdge(offer != null ? offer.Kind : "service"));
                UiKit.Stretch(edge.rectTransform, -3f, -3f, -3f, -3f);
                edge.raycastTarget = false;
                var paper = PaperFx.Sheet(tile, PaperFx.Panel, "paper");
                UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
                // 名前と中身 (売れた札は行商の手を離れているので台帳の名前と文だけ)
                string nameS, sumS, tip;
                if (offer != null)
                {
                    nameS = offer.Name; sumS = DepartureScreen.Summary(offer); tip = DepartureScreen.Tip(offer);
                    if (!sold && !available) sumS = "選べない: " + DepartureScreen.UnavailableReason(run, offer.Choice);
                }
                else
                {
                    DepartureTemplate t = null;
                    foreach (var d in Content.AllDepartures) if (d.Id == slot.Id) { t = d; break; }
                    nameS = t != null ? t.Name : slot.Id; sumS = t != null ? t.Text : ""; tip = "<b>" + nameS + "</b>\n" + sumS;
                }
                Tooltip.Attach(paper.gameObject, delegate { return tip; });
                float priceW = 176f, pad = 12f;
                var wrap = UiKit.NewRect("pricewrap", tile);
                // 札が狭い (4品並ぶ) 時は縦に積む: 名前 (PC は中身の一行も)・下に値札。中身の全文は札に触れると出る
                bool narrow = w < 360f;
                if (narrow)
                {
                    var nmN = UiKit.Deco(tile, nameS, ph ? 16 : 18, PaperFx.Ink, TextAnchor.MiddleCenter);
                    nmN.textWrappingMode = TextWrappingModes.NoWrap; nmN.overflowMode = TextOverflowModes.Ellipsis;
                    UiKit.Anchor(nmN.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pad, ph ? -28f : -32f), new Vector2(-pad, -4f));
                    nmN.raycastTarget = false;
                    if (!ph)
                    {
                        var smN = UiKit.Txt(tile, sumS, 13, !sold && !available ? PaperFx.BadInk : PaperFx.InkSoft, TextAnchor.MiddleCenter);
                        smN.textWrappingMode = TextWrappingModes.NoWrap; smN.overflowMode = TextOverflowModes.Ellipsis;
                        UiKit.Anchor(smN.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pad, -54f), new Vector2(-pad, -32f));
                        smN.raycastTarget = false;
                    }
                    UiKit.Anchor(wrap, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-priceW / 2f, 4f), new Vector2(priceW / 2f, 52f));
                }
                else
                {
                    float artS = ph ? 48f : 64f;
                    bool withArt = offer != null && w - pad * 2f - priceW - 10f - artS - 10f >= 120f;   // 狭い札 (幅 1200 のスマホ) は絵を省いて文を読ませる
                    float textL = pad;
                    if (withArt)
                    {
                        var art = DepartureScreen.Art(tile, offer, artS);
                        art.anchorMin = art.anchorMax = new Vector2(0f, 0.5f);
                        art.anchoredPosition = new Vector2(pad + artS / 2f, 0f);
                        textL = pad + artS + 10f;
                    }
                    float textR = priceW + pad + 8f;
                    var nm = UiKit.Deco(tile, nameS, ph ? 17 : 20, PaperFx.Ink, TextAnchor.MiddleLeft);
                    nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
                    UiKit.Anchor(nm.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textL, ph ? -34f : -44f), new Vector2(-textR, ph ? -8f : -12f));
                    nm.raycastTarget = false;
                    var sm = UiKit.Txt(tile, sumS, ph ? 13 : 15, !sold && !available ? PaperFx.BadInk : PaperFx.InkSoft, TextAnchor.UpperLeft);
                    sm.textWrappingMode = TextWrappingModes.Normal; sm.overflowMode = TextOverflowModes.Ellipsis;
                    UiKit.Anchor(sm.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(textL, 8f), new Vector2(-textR, ph ? -36f : -48f));
                    sm.raycastTarget = false;
                    float pb = (tileH - 48f) / 2f;
                    UiKit.Anchor(wrap, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-pad - priceW, pb), new Vector2(-pad, pb + 48f));
                }
                PriceTag(wrap, slot.Price, sold ? "売切" : null, canBuy, sold ? null : (Action)delegate
                {
                    if (!canBuy) return;
                    if (Run.EventChoiceNeedsCard(offer.Choice)) { Audio.Ui("click"); g.ShopMode = "depart-pick:" + idx; g.Rebuild(); }
                    else { Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyDeparture { Index = idx }); }
                });
                if (sold)
                {
                    var cover = UiKit.Pan(tile, new Color(0f, 0f, 0f, 0.35f), "sold");
                    UiKit.Stretch(cover.rectTransform, 0f, 0f, 0f, 0f);
                    cover.raycastTarget = false;
                }
            }
            return labelH + 4f + tileH + 12f;
        }

        /// <summary>行商がまだ持っている支度 (run.departure.leftovers)。売れたら null</summary>
        static DepartureOffer LeftoverOffer(RunState run, string id)
        {
            if (run.Departure == null || run.Departure.Leftovers == null) return null;
            foreach (var o in run.Departure.Leftovers) if (o.Id == id) return o;
            return null;
        }

        static void ServiceBtn(Transform parent, string label, bool enabled, Action onClick)
        {
            var b = UiKit.Btn(parent, label, onClick, 17, enabled);
            UiKit.Le(b, -1f, 48f, -1f, 48f);
        }

        /// <summary>値札。onBuy があればボタン (「N G で買う」。2026-09-22 札のタップは拡大になったので、買うのはここだけ)。指で押せる 48 の高さ</summary>
        public static void PriceTag(RectTransform cell, int price, string over, bool affordable, Action onBuy = null, float width = 176f, bool star = false)
        {
            var tag = UiKit.NewRect("price", cell);
            bool asBtn = onBuy != null;
            float hw = Mathf.Min(88f, width / 2f);   // 棚の段の間隔に収める (F11)
            UiKit.Anchor(tag, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), asBtn ? new Vector2(-hw, 0f) : new Vector2(-Mathf.Min(70f, hw), 4f), asBtn ? new Vector2(hw, 48f) : new Vector2(Mathf.Min(70f, hw), 40f));
            var bg = tag.gameObject.AddComponent<Image>();
            bg.sprite = asBtn ? Theme.Button : Theme.Tag; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 1f;
            bg.color = over != null ? new Color(0.5f, 0.5f, 0.5f, 1f) : affordable ? (asBtn ? PaperFx.BrassLight : Color.white) : new Color(0.7f, 0.55f, 0.55f, 1f);
            if (asBtn)
            {
                var btn = tag.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg; btn.interactable = affordable;
                btn.onClick.AddListener(delegate { onBuy(); });
            }
            var hg = UiKit.Horz(tag, 4, 0);
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;
            if (over == null) UiKit.Icon(tag, "gold", 22f);
            // 幅が 150 未満の値札は「で買う」を省く (ボタンの形は残す)。レア枠は頭に「★」
            string label = over ?? ((star ? "★ " : "") + price + " G" + (asBtn && affordable && width >= 150f ? " で買う" : ""));
            var t = UiKit.Txt(tag, label, 17, over != null ? UiKit.ColInkSoft : affordable ? UiKit.ColGoldInk : UiKit.ColBadInk, TextAnchor.MiddleCenter, true);
            t.raycastTarget = false;
            UiKit.Le(t, 50f, 30f, -1f, 30f);
        }
    }
}
