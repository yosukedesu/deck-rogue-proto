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
            // 案 B「二段の帳場」(§11・2026-10-29 ユーザー裁定): 背景は屋台の一枚絵 (PixelLab 400×224 を 5 倍)、上の段＝札 1 列、下の段＝遺物・坑口の品・サービスを同じ大きさの札で 1 列。
            // 一枚絵が無ければ今までの箱庭＋小物。棚 (除去・鍛える・ギア) を開いている間は暗幕 0.5
            BattleScreen.BuildBackground(root, run.Act);
            bool paintedS = CampfireScreen.Backdrop(root, "bd_shop", g.ShopMode != null ? 0.55f : 0.42f);
            if (!paintedS)
            {
                var dimS = UiKit.Pan(root, new Color(0f, 0f, 0f, g.ShopMode != null ? 0.5f : 0.18f), "dim");
                dimS.raycastTarget = false;
                UiKit.Stretch(dimS.rectTransform, 0f, 0f, 0f, 0f);
                if (g.ShopMode == null && shop != null) Scene(root);
            }
            RunUi.TopBar(g, root, "ショップ");
            RunUi.Message(g, root);

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

            // 案 B「二段の帳場」(§11・2026-10-29 ユーザー裁定「配置など見にくい」→ 読みやすさ優先): 上の段＝札を 1 列、下の段＝遺物・坑口の品・サービスを同じ大きさの札 (絵つき) で 1 列。
            // 段は夜色の板に真鍮の見出し札。要素は「2 段と進むボタン」だけ。場面 (屋台の一枚絵) は後ろで暗め
            bool ph = UiKit.Phone;
            var csS = BattleScreen.CanvasSize(root);
            RunUi.Heading(root, "坑口の行商", "買わなくてもよい ・ 所持金 " + run.Gold + " G ・ " + (ph ? "札はタップで拡大" : "札は押すと拡大"));
            // 行商が預かった支度 (出立の店の売れ残り 2026-09-24。幕1の店だけ): 札は上の段に普通の札と並べ、遺物・薬草・道具箱は下の段へ
            var leftCards = new List<int>(); var leftOthers = new List<int>();
            if (shop.Departures != null)
                for (int d = 0; d < shop.Departures.Count; d++)
                {
                    var lo = LeftoverOffer(run, shop.Departures[d].Id);
                    if (lo != null && shop.Departures[d].Sold != true && lo.Kind == "card" && lo.Choice.AddCardIds != null && lo.Choice.AddCardIds.Count > 0) leftCards.Add(d);
                    else if (lo != null && shop.Departures[d].Sold != true) leftOthers.Add(d);
                }

            // 段の寸法。PC は盤の値 (上の段 180〜600・下の段 636〜846)。スマホは上部バーの下から「店を出る」の上までを 2 段に割る
            float side = ph ? 20f : 60f;
            float btnH = ph ? 48f : 52f, btnBottom = ph ? 12f : 40f;
            float tier2H = ph ? 136f : 210f;
            float tier1Top = ph ? RunUi.TopH + 12f : 180f;
            float tierGap = ph ? 12f : 36f;
            float tier1H = ph ? csS.y - tier1Top - (btnBottom + btnH + 10f) - tier2H - tierGap : 420f;
            float tier2Top = tier1Top + tier1H + tierGap;
            var tier1 = Tier(root, "札　" + (shop.Cards.Count + leftCards.Count) + " 枚", side, tier1Top, tier1H);
            var tier2 = Tier(root, "遺物 ・ 坑口の品 ・ サービス", side, tier2Top, tier2H);

            // 上の段: 札を 1 列 (全部入る倍率。PC は 0.85・スマホは 0.6 まで)
            int nShop = shop.Cards.Count;
            int n = nShop + leftCards.Count;
            float innerW = csS.x - side * 2f - 80f;
            float gap = ph ? 12f : 24f;
            float scale = Mathf.Min(ph ? 0.6f : 0.95f, (innerW - Math.Max(0, n - 1) * gap) / Math.Max(1, n) / CardView.W);
            scale = Mathf.Min(scale, (tier1H - 24f - 60f) / CardView.H);   // 段の高さにも収める (値札 60 ぶんを足す)
            float cardW = CardView.W * scale, cardH = CardView.H * scale;
            float totalW = n * cardW + Math.Max(0, n - 1) * gap;
            float x0 = -totalW / 2f + cardW / 2f;
            float tagW = Mathf.Min(176f, cardW + gap - 8f);   // 値札は隣と重ねない
            float cellY = 4f;   // 段の中心 (札は 30 上・値札は下)
            Func<int, Vector2> cellPos = i => new Vector2(x0 + i * (cardW + gap), cellY);
            for (int i = nShop; i < n; i++)
            {   // 坑口で買わなかった札 (普通の札と同じ見た目・値段は棚の値段)
                int di = leftCards[i - nShop];
                var dslot = shop.Departures[di];
                var dof = LeftoverOffer(run, dslot.Id);
                CardDef ddef = null;
                try { ddef = Content.GetCardDef(dof.Choice.AddCardIds[0]); } catch (Exception) { }
                if (ddef == null) continue;
                bool dOk = run.Gold >= dslot.Price;
                var dcell = UiKit.NewRect("shop-dep" + i, tier1);
                dcell.anchorMin = dcell.anchorMax = new Vector2(0.5f, 0.5f);
                dcell.sizeDelta = new Vector2(cardW, cardH + 60f);
                dcell.anchoredPosition = cellPos(i);
                var dci = new CardInstance { Uid = "shop-dep" + i, Def = ddef };
                var dcv = CardView.Build(dcell, dci, null, dOk, false, "shop-card");
                dcv.localScale = Vector3.one * scale;
                dcv.anchoredPosition = new Vector2(0f, 30f);
                RewardScreen.HoverRaise(dcv, delegate { Audio.Ui("click"); CardPopup.Open(g, dci, null); });
                CardPopup.Attach(g, dcv, dci, null, true);
                PriceTag(dcell, dslot.Price, null, dOk, delegate { if (dOk) { Audio.Ui("buy"); ScreenFx.FlyTo(g, dcv, "deck", () => g.Do(new RunCommand_ShopBuyDeparture { Index = di })); } }, tagW);
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
                var cell = UiKit.NewRect("shop" + i, tier1);
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
                bool rareSlot = i == nShop - 1 && nShop >= 6;   // レア枠は値札の頭に「★」(夜の段の上に注記の札を重ねない)
                PriceTag(cell, item.Price, sold ? "売切" : null, canBuy, sold ? null : (Action)delegate { if (canBuy) { Audio.Ui("buy"); ScreenFx.FlyTo(g, cv, "deck", () => g.Do(new RunCommand_ShopBuyCard { Index = idx })); } }, tagW, rareSlot);
            }

            // 下の段: 遺物 → 坑口の品 → 取り除く → 鍛える → ギアの棚。同じ大きさの紙の札 (左に絵・名前・一行・値札のボタン)
            var tiles = new List<Action<RectTransform, float, float>>();
            if (shop.RelicId != null)
            {
                RelicDef rd = null;
                try { rd = Content.GetRelicDef(shop.RelicId); } catch (Exception) { }
                string rName = rd != null ? rd.Name : shop.RelicId;
                string rSub = (rd != null ? CardText.RarityLabel(rd.Rarity) + "の遺物 ・ " + rd.Description : "");
                string rTip = "<b>" + rName + "</b>  " + shop.RelicPrice + "G" + (rd != null ? "\n" + CardText.RarityLabel(rd.Rarity) + "\n" + rd.Description : "");
                bool canRelic = run.Gold >= shop.RelicPrice;
                string relicId = shop.RelicId;
                tiles.Add((tile, w, h) => Tile(g, tile, w, h, p => RunUi.RelicArt(p, relicId, 64f).rectTransform, rName, rSub, rTip, shop.RelicPrice, null, canRelic,
                    delegate { Audio.Ui("buy"); var art = tile.Find("art"); ScreenFx.FlyTo(g, art != null ? art as RectTransform : tile, "relics", () => g.Do(new RunCommand_ShopBuyRelic())); }));
            }
            else tiles.Add((tile, w, h) => Tile(g, tile, w, h, p => UiKit.Icon(p, "star", 48f).rectTransform, "遺物", "売切", "<b>遺物</b>\n売切", shop.RelicPrice, "売切", false, null));
            foreach (var idx0 in leftOthers)
            {
                int idx = idx0;
                var slot = shop.Departures[idx];
                var offer = LeftoverOffer(run, slot.Id);
                bool available = Run.EventChoiceAvailable(run, offer.Choice);   // 店の値段は slot.Price (物価の倍率込み)。出立の店の値段・購入済みは見ない
                bool canBuy = available && run.Gold >= slot.Price;
                string sub = available ? DepartureScreen.Summary(offer) : "選べない: " + DepartureScreen.UnavailableReason(run, offer.Choice);
                tiles.Add((tile, w, h) => Tile(g, tile, w, h, p => DepartureScreen.Art(p, offer, 64f), offer.Name, sub, DepartureScreen.Tip(offer), slot.Price, null, canBuy, delegate
                {
                    if (Run.EventChoiceNeedsCard(offer.Choice)) { Audio.Ui("click"); g.ShopMode = "depart-pick:" + idx; g.Rebuild(); }
                    else { Audio.Ui("buy"); g.Do(new RunCommand_ShopBuyDeparture { Index = idx }); }
                }));
            }
            tiles.Add((tile, w, h) => Tile(g, tile, w, h, p => UiKit.Icon(p, "exhaust", 56f).rectTransform, "取り除く", "デッキの 1 枚を永久に外す ・ 使うたび +25 G",
                "<b>取り除く</b>  " + rmPrice + "G\nデッキの 1 枚を永久に外す。使うたび 25 G 値上がり (ラン通算)", rmPrice, null, run.Gold >= rmPrice && run.Deck.Count > 5, delegate { g.ShopMode = "remove"; g.Rebuild(); }));
            tiles.Add((tile, w, h) => Tile(g, tile, w, h, p => UiKit.Icon(p, "hammer", 56f).rectTransform, "鍛える", "デッキの 1 枚を鍛える ・ 使うたび +50 G",
                "<b>鍛える</b>  " + upPrice + "G\nデッキの 1 枚を鍛える。使うたび 50 G 値上がり (ラン通算)", upPrice, null, run.Gold >= upPrice, delegate { g.ShopMode = "upgrade"; g.Rebuild(); }));
            int shelfN = shop.Gears != null ? shop.Gears.Count : 0;
            int manaPrice = shop.ManaPrice ?? DeckRogue.Engine.Run.SHOP_MANA_PRICE;
            var gearsHeld = DeckRogue.Engine.Run.GearsOf(run);
            tiles.Add((tile, w, h) => Tile(g, tile, w, h, p => UiKit.Icon(p, "toolbox", 56f).rectTransform, "ギアの棚", shelfN + " 枠 ・ 魔素 10 ごと " + manaPrice + " G ・ 持ち物 " + gearsHeld.Count + "/" + Gears.GEAR_CARRY_MAX,
                "<b>ギアの棚</b>\nギア " + shelfN + " 枠と魔素 (10 ごと " + manaPrice + " G)。持ち物 " + gearsHeld.Count + "/" + Gears.GEAR_CARRY_MAX, manaPrice, null, true, delegate { g.ShopMode = "gears"; g.Rebuild(); }, "棚を開く"));
            {
                int tn = tiles.Count;
                float tgap = ph ? 10f : 20f;
                float tw = Mathf.Min(ph ? 262f : 300f, (csS.x - side * 2f - 80f - tgap * (tn - 1)) / tn);
                float th = ph ? 104f : 136f;
                float tx0 = -((tn - 1) * (tw + tgap)) / 2f;
                for (int i = 0; i < tn; i++)
                {
                    var tile = UiKit.NewRect("tile" + i, tier2);
                    tile.anchorMin = tile.anchorMax = new Vector2(0.5f, 0.5f);
                    tile.sizeDelta = new Vector2(tw, th);
                    tile.anchoredPosition = new Vector2(tx0 + i * (tw + tgap), ph ? -4f : -6f);
                    tiles[i](tile, tw, th);
                }
            }

            // 進む＝真鍮の決定のボタン (右下)
            RunUi.BottomButton(root, "店を出て坑へ", delegate { g.ShopMode = null; g.Do(new RunCommand_ShopLeave()); }, 18, 260f, btnH, csS.x / 2f - side - 130f, btnBottom, PaperFx.BrassLight);
        }

        /// <summary>段 (案 B): 夜色の板 (α 0.72) と左上の真鍮の見出し札。返り値は板の RectTransform (中身は中央基準で置く)</summary>
        static RectTransform Tier(RectTransform root, string label, float side, float top, float h)
        {
            // 盤の rgba(15,17,32,.72)。Linear の合成では屋台の明るい暖色が 7% 透けるだけで茶色 (53,39,27) に出たので、ほぼ不透明 (0.98) の夜色にする
            var board = PaperFx.Sheet(root, PaperFx.Tag, "tier", new Color(PaperFx.Night.r, PaperFx.Night.g, PaperFx.Night.b, 0.98f));
            board.raycastTarget = false;
            UiKit.Anchor(board.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(side, -top - h), new Vector2(-side, -top));
            var rim = board.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(PaperFx.Brass.r, PaperFx.Brass.g, PaperFx.Brass.b, 0.35f); rim.effectDistance = new Vector2(1f, -1f); rim.useGraphicAlpha = false;
            float tabW = label.Length * (UiKit.Phone ? 14f : 15f) + 28f;
            var tab = PaperFx.Sheet(board.transform, PaperFx.Tag, "tab", PaperFx.Brass);
            tab.raycastTarget = false;
            UiKit.Anchor(tab.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(24f + tabW, 14f));
            var tt = UiKit.Txt(tab.transform, label, UiKit.Phone ? 13 : 15, PaperFx.Ink, TextAnchor.MiddleCenter, true);
            tt.raycastTarget = false; tt.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Stretch(tt.rectTransform, 0f, 0f, 0f, 0f);
            return board.rectTransform;
        }

        /// <summary>
        /// 下の段の札 (案 B): 紙の札に 左の絵 (遺物の絵・ギアの絵・道具の印)・名前・一行・値札のボタン。狭い札 (スマホ・幅 220 未満) は一行を省き、絵を小さく。
        /// over が null でなければ売切 (灰)。priceLabel は値札の字を差し替える (「棚を開く」)
        /// </summary>
        static void Tile(GameRoot g, RectTransform tile, float w, float h, Func<Transform, RectTransform> artOf, string name, string sub, string tip, int price, string over, bool canBuy, Action onBuy, string priceLabel = null)
        {
            bool ph = UiKit.Phone;
            var paper = PaperFx.Sheet(tile, PaperFx.Panel, "paper");
            UiKit.Stretch(paper.rectTransform, 0f, 0f, 0f, 0f);
            Tooltip.Attach(paper.gameObject, delegate { return tip; });
            bool narrow = w < 220f;
            float pad = ph ? 8f : 12f;
            float artS = narrow ? 36f : (ph ? 48f : 64f);
            float upper = h - (ph ? 48f : 60f);   // 上の帯 (絵・名前・一行) の高さ。下は値札のボタン
            var art = artOf(tile);
            art.name = "art";
            art.anchorMin = art.anchorMax = new Vector2(0f, 1f);
            art.anchoredPosition = new Vector2(pad + artS / 2f, -upper / 2f);
            if (art.sizeDelta.x > artS || art.sizeDelta.x <= 0f) art.sizeDelta = new Vector2(artS, artS);   // 遺物の絵は 32/64 の整数倍のまま
            float textL = pad + artS + (narrow ? 6f : 10f);
            var nm = UiKit.Deco(tile, name, narrow ? 14 : (ph ? 16 : 18), PaperFx.Ink, TextAnchor.MiddleLeft);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis; nm.raycastTarget = false;
            if (narrow) UiKit.Anchor(nm.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textL, -upper), new Vector2(-pad, 0f));
            else
            {
                UiKit.Anchor(nm.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textL, ph ? -30f : -36f), new Vector2(-pad, ph ? -6f : -8f));
                var sm = UiKit.Txt(tile, sub, ph ? 12 : 13, PaperFx.InkSoft, TextAnchor.UpperLeft);
                sm.textWrappingMode = TextWrappingModes.Normal; sm.overflowMode = TextOverflowModes.Ellipsis; sm.raycastTarget = false;
                UiKit.Anchor(sm.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textL, -upper), new Vector2(-pad, ph ? -30f : -36f));
            }
            float priceW = Mathf.Min(176f, w - pad * 2f);
            var wrap = UiKit.NewRect("pricewrap", tile);
            float btnH = ph ? 40f : 48f;
            UiKit.Anchor(wrap, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(pad, ph ? 4f : 6f), new Vector2(pad + priceW, (ph ? 4f : 6f) + btnH));
            PriceTag(wrap, price, over, canBuy, over != null || onBuy == null ? null : (Action)delegate { if (canBuy) onBuy(); }, priceW, false, priceLabel);
            if (over != null)
            {
                var cover = UiKit.Pan(tile, new Color(0f, 0f, 0f, 0.35f), "sold");
                UiKit.Stretch(cover.rectTransform, 0f, 0f, 0f, 0f);
                cover.raycastTarget = false;
            }
        }
        /// <summary>場面 (§10-3「店主は出さない」): 道の左奥に屋台と提灯の柱、手前に木箱・袋・樽、右に荷車。提灯に光と暈。小物は幕2の既存の絵 (act2_*) と地図の荷車</summary>
        static void Scene(RectTransform root)
        {
            float cw = BattleScreen.CanvasSize(root).x;
            float dot = 4.5f * BattleScreen.ArtScale;   // 焚き火と同じ (座席の板は矩形より小さく映る)
            void Put(string key, string art, float t, float s, float dots)
            {
                var sp = Theme.Art("props", art) ?? Theme.Art("map", art);
                if (sp == null) return;
                var rt = UiKit.NewRect(key, root);
                var feet = Stage.SceneSeat(key, t, s);
                PaperFx.FitPixel(rt, sp, feet.x - cw / 2f, feet.y, dots * dot);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = sp; img.preserveAspect = true; img.raycastTarget = false;
                Stage.BindUnit(key, rt, img, sp);
            }
            // 奥 (s が大きい) に置く = 画面の上に上がり、下の敷物 (UI) に隠れない
            Put("scene-lantern", "act2_lantern_post", -8.8f, 2.0f, 40f);
            Put("scene-stall", "act2_stall_a", -6.8f, 2.8f, 96f);
            Put("scene-crates", "act2_crate_stack", -4.4f, 2.6f, 48f);
            Put("scene-sacks", "act2_sacks", -3.6f, 2.0f, 48f);
            Put("scene-barrel", "act2_barrel", -2.4f, 2.4f, 32f);
            Put("scene-cart", "map_cart", 6.4f, 2.4f, 62f);
            Stage.AttachLight("scene-lantern", PaperFx.BrassLight, 3.2f, 7f, 0.85f, 0.06f);
            Stage.AttachGlow("scene-lantern", new Color(PaperFx.BrassLight.r, PaperFx.BrassLight.g, PaperFx.BrassLight.b, 0.28f), 3.2f, 0.85f);
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
        public static void PriceTag(RectTransform cell, int price, string over, bool affordable, Action onBuy = null, float width = 176f, bool star = false, string text = null)
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
            if (over == null && text == null) UiKit.Icon(tag, "gold", 22f);
            // 幅が 150 未満の値札は「で買う」を省く (ボタンの形は残す)。レア枠は頭に「★」
            string label = over ?? text ?? ((star ? "★ " : "") + price + " G" + (asBtn && affordable && width >= 150f ? " で買う" : ""));
            var t = UiKit.Txt(tag, label, 17, over != null ? UiKit.ColInkSoft : affordable ? UiKit.ColGoldInk : UiKit.ColBadInk, TextAnchor.MiddleCenter, true);
            t.raycastTarget = false;
            UiKit.Le(t, 50f, 30f, -1f, 30f);
        }
    }
}
