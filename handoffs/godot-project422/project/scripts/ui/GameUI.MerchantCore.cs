// Source-backed merchant composition. Pinned Unity reference: 7a84644.
// All state transitions remain engine commands. Enlarged text uses native scroll
// regions instead of shrinking prices or letting a service panel cover a card.
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private readonly Dictionary<string, double> _merchantScroll = new();
    private string _merchantVisit = "";

    private void BuildDeparture(RunState run)
    {
        Heading("出立の店", PhoneLayout ? "坑に降りる前に、行商の店で支度を整える" : "坑口の行商の店。坑に降りる前に、支度を整えに寄った。");
        BeginMerchantVisit(run);
        var quote = MerchantPaper(_body, "SourceDepartureMasterLine");
        var quoteRow = Row(quote);
        var who = MerchantLabel(quoteRow, "坑口の行商", 17, SourcePalette.BrassInk);
        who.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; who.AutowrapMode = TextServer.AutowrapMode.Off;
        MerchantLabel(quoteRow, Content.DepartureMasterLine(run.Colors), 18);

        var decision = new SourceUiDecision(_session?.Revision ?? 0);
        void Commit(Action action) { if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked)) action(); }
        var offers = run.Departure?.Offers ?? Array.Empty<DepartureOffer>();
        if (offers.Count == 0)
        {
            Text(_body, "品物が見つかりません", 21, Muted);
            MerchantButton(_body, "店を出て坑へ", () => Commit(() => Do(new RunCommand_LeaveDeparture())), true, true);
            return;
        }

        var layout = MerchantColumns("SourceDepartureLayout", "departure");
        var cardOffers = offers.Select((o, i) => (Offer: o, Index: i))
            .Where(x => x.Offer.Kind == "card" && x.Offer.Choice.AddCardIds?.Count > 0).ToArray();
        var shelfSizing = SourceMerchantUi.CardShelf(layout.LeftWidth, cardOffers.Length, _textScale, true);
        shelfSizing = shelfSizing with { CardScale = Math.Min(PhoneLayout ? 1.30f : 1.35f, Math.Max(1f, shelfSizing.CellWidth - 16) / SourceCardFace.W) };
        var shelf = MerchantGrid(layout.Left, "SourceDepartureCardShelf", shelfSizing.Columns);
        foreach (var item in cardOffers)
        {
            int index = item.Index;
            var offer = item.Offer;
            bool bought = Run.DepartureOfferBought(run, offer);
            bool available = Run.DepartureOfferAvailable(run, offer);
            var card = new CardInstance { Uid = "departure-" + index, Def = Content.GetCardDef(offer.Choice.AddCardIds[0]) };
            MerchantCard(shelf, card, shelfSizing, offer.Price, available, bought ? "購入済み" : null,
                () => ResolveChoice(run, offer.Choice, offer.Name + $"（{offer.Price} G）", ci => Commit(() => Do(new RunCommand_BuyDeparture { Index = index, CardIndex = ci }))),
                "SourceDepartureBuyCard" + index);
        }

        var services = offers.Select((o, i) => (Offer: o, Index: i)).Where(x => x.Offer.Kind != "relic" && !cardOffers.Any(c => c.Index == x.Index)).ToArray();
        var serviceGrid = MerchantGrid(layout.Left, "SourceDepartureServiceBand", Math.Min(Math.Max(1, services.Length), Math.Max(1, (int)(layout.LeftWidth / (245 * _textScale)))));
        foreach (var item in services) AddOffer(serviceGrid, item.Offer, item.Index);
        foreach (var item in offers.Select((o, i) => (Offer: o, Index: i)).Where(x => x.Offer.Kind == "relic"))
            AddOffer(layout.Right, item.Offer, item.Index);

        var resources = MerchantLabel(layout.Footer, $"支度: 札 {run.Deck.Count}枚 · 遺物 {run.Relics.Count}個 · ギア {run.Gears?.Count ?? 0}/{Gears.GEAR_CARRY_MAX}", 16, Muted);
        resources.Name = "SourceDepartureLoadout";
        MerchantButton(layout.Footer, "店を出て坑へ", () => Commit(() => Do(new RunCommand_LeaveDeparture())), true, false).Name = "SourceDepartureLeave";

        void AddOffer(Node parent, DepartureOffer offer, int index)
        {
            bool bought = Run.DepartureOfferBought(run, offer);
            bool available = Run.DepartureOfferAvailable(run, offer);
            var tile = DepartureTile(parent, offer);
            if (!bought && !available && run.Gold >= offer.Price)
                MerchantLabel(tile, SourceMerchantUi.UnavailableReason(run, offer.Choice), 15, SourcePalette.BadInk);
            MerchantPrice(tile, offer.Price, available, bought ? "購入済み" : null, () =>
                ResolveChoice(run, offer.Choice, offer.Name + $"（{offer.Price} G）", ci => Commit(() => Do(new RunCommand_BuyDeparture { Index = index, CardIndex = ci }))),
                "SourceDepartureBuy" + index);
            if (bought) AddMerchantStamp(tile.GetParent<Control>(), "買った", small: true);
        }
    }

    private VBoxContainer DepartureTile(Node parent, DepartureOffer offer)
    {
        var choice = offer.Choice;
        string group = choice.RelicId != null ? "relics" : choice.Gears?.Count > 0 ? "gears" : "icons";
        string image = choice.RelicId ?? choice.Gears?.FirstOrDefault() ??
            (choice.RemoveCard == true ? "exhaust" : choice.UpgradeCard == true ? "hammer" : choice.MaxHp.HasValue || choice.Hp.HasValue || choice.HpRatio.HasValue ? "heart" : "star");
        string rarity = choice.RelicId == null ? "" : RewardRarityName(Content.GetRelicDef(choice.RelicId).Rarity);
        var tile = MerchantOffer(parent, "SourceDepartureOffer" + offer.Id, offer.Name, SourceMerchantUi.Summary(offer), group, image,
            () => Open(offer.Name, box => Text(ScrollColumn(box), SourceMerchantUi.Details(offer), 22)), rarity);
        return tile;
    }

    private void BuildShop(RunState run)
    {
        if (run.Shop == null)
        {
            Heading("ショップ", "在庫がありません");
            MerchantButton(_body, "店を出る", () => Do(new RunCommand_ShopLeave()), true, true);
            return;
        }
        BeginMerchantVisit(run);
        var shop = run.Shop;
        if (_shopGearsScreen) { BuildShopGearShelf(run, shop); return; }
        Heading("ショップ", (PhoneLayout ? "札はタップで拡大。" : "札はクリックで拡大。") + "買うのは値札のボタン。");
        var decision = new SourceUiDecision(_session?.Revision ?? 0);
        void Commit(Action action) { if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked)) action(); }
        var layout = MerchantColumns("SourceShopLayout", "shop");
        var departures = shop.Departures ?? Array.Empty<ShopStateDepartures>();
        var leftoverCards = new List<(int Index, ShopStateDepartures Slot, DepartureOffer Offer)>();
        var leftoverOther = new List<(int Index, ShopStateDepartures Slot, DepartureOffer Offer)>();
        for (int i = 0; i < departures.Count; i++)
        {
            var slot = departures[i];
            var offer = run.Departure?.Leftovers?.FirstOrDefault(o => o.Id == slot.Id);
            if (offer == null || slot.Sold == true) continue;
            var item = (i, slot, offer);
            if (offer.Kind == "card" && offer.Choice.AddCardIds?.Count > 0) leftoverCards.Add(item);
            else leftoverOther.Add(item);
        }

        var sceneRow = Row(layout.Left);
        sceneRow.Name = "SourceShopSceneAndStock";
        var scene = Art("scenes", "shop");
        if (scene != null && !PhoneLayout && leftoverOther.Count <= 3)
        {
            var frame = MerchantPaper(sceneRow, "SourceShopSceneWindow", night: true);
            frame.CustomMinimumSize = new Vector2(Math.Min(320, layout.LeftWidth * .29f), PhoneLayout ? 104 : 132);
            frame.SizeFlagsHorizontal = SizeFlags.Fill;
            var image = new TextureRect { Texture = scene, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore };
            frame.AddChild(image);
        }
        if (leftoverOther.Count > 0)
        {
            var band = Column(sceneRow);
            Text(band, "坑口の品", 17, Gold);
            var grid = MerchantGrid(band, "SourceShopDepartureBand", Math.Min(leftoverOther.Count, Math.Max(1, (int)(layout.LeftWidth / 190))));
            foreach (var item in leftoverOther)
            {
                var offer = item.Offer; int index = item.Index; var slot = item.Slot;
                var col = MerchantCarriedOffer(grid, offer);
                bool available = Run.EventChoiceAvailable(run, offer.Choice);
                if (!available && run.Gold >= slot.Price) MerchantLabel(col, SourceMerchantUi.UnavailableReason(run, offer.Choice), 15, SourcePalette.BadInk);
                MerchantPrice(col, slot.Price, available && run.Gold >= slot.Price, null, () => ResolveChoice(run, offer.Choice,
                    offer.Name + $"（{slot.Price} G）", ci => Commit(() => Do(new RunCommand_ShopBuyDeparture { Index = index, CardIndex = ci }))), "SourceShopDepartureBuy" + index);
            }
        }
        else
        {
            var sign = Column(sceneRow);
            sign.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            Text(sign, "札と道具を、次の坑道へ。", 23, Gold);
            if (!PhoneLayout) Text(sign, "旅の道具を、ここで。", 18, Muted);
        }

        var shelfSizing = SourceMerchantUi.CardShelf(layout.LeftWidth, shop.Cards.Count + leftoverCards.Count, _textScale, false);
        var shelf = MerchantGrid(layout.Left, "SourceShopCardShelf", shelfSizing.Columns);
        for (int i = 0; i < shop.Cards.Count; i++)
        {
            int index = i; var offer = shop.Cards[i];
            var card = new CardInstance { Uid = "shop-" + i, Def = Content.GetCardDef(offer.Id) };
            MerchantCard(shelf, card, shelfSizing, offer.Price, offer.Sold != true && run.Gold >= offer.Price, offer.Sold == true ? "売切" : null,
                () => Commit(() => Do(new RunCommand_ShopBuyCard { Index = index })), "SourceShopBuyCard" + i,
                SourceRunScreenPolicy.RareSlot(i, shop.Cards.Count));
        }
        foreach (var item in leftoverCards)
        {
            int index = item.Index; var slot = item.Slot; var offer = item.Offer;
            var card = new CardInstance { Uid = "shop-departure-" + index, Def = Content.GetCardDef(offer.Choice.AddCardIds[0]) };
            MerchantCard(shelf, card, shelfSizing, slot.Price, run.Gold >= slot.Price && Run.EventChoiceAvailable(run, offer.Choice), null,
                () => ResolveChoice(run, offer.Choice, offer.Name + $"（{slot.Price} G）", ci => Commit(() => Do(new RunCommand_ShopBuyDeparture { Index = index, CardIndex = ci }))),
                "SourceShopDepartureBuy" + index);
        }

        var servicePanel = MerchantPaper(layout.Right, "SourceShopServices");
        var services = Column(servicePanel);
        MerchantLabel(services, "店主のサービス", 23, SourcePalette.BrassInk);
        MerchantButton(services, $"ギアの棚  {shop.Gears?.Count ?? 0}枠 ／ 魔素 {shop.ManaPrice ?? Run.SHOP_MANA_PRICE} G", () => ShowShopShelf(true), true, false).Name = "SourceShopShelfOpen";
        if (shop.RelicId != null)
        {
            var relic = Content.GetRelicDef(shop.RelicId);
            var relicTile = MerchantOffer(services, "SourceShopRelic", relic.Name, relic.Description, "relics", relic.Id,
                () => Open(relic.Name, box => Text(ScrollColumn(box), relic.Description, 23)), RewardRarityName(relic.Rarity));
            MerchantPrice(relicTile, shop.RelicPrice, run.Gold >= shop.RelicPrice, null,
                () => Commit(() => Do(new RunCommand_ShopBuyRelic())), "SourceShopBuyRelic");
        }
        else MerchantLabel(services, "レリックは売切", 21, SourcePalette.InkSoft).Name = "SourceShopRelicSold";
        int removePrice = Run.ShopRemovalPrice(run), upgradePrice = Run.ShopUpgradePrice(run);
        MerchantService(services, "exhaust", "札を取り除く", removePrice, "デッキから1枚を永久に取り除く", run.Gold >= removePrice && run.Deck.Count > 5,
            run.Deck.Count <= 5 ? "デッキが5枚以下" : "", () => ChooseDeckCard(run, "取り除く", _ => run.Deck.Count > 5,
                index => Commit(() => Do(new RunCommand_ShopRemove { Index = index }))), "SourceShopRemove");
        MerchantService(services, "hammer", "札を鍛える", upgradePrice, "札を選んで、鍛えた後を確認", run.Gold >= upgradePrice && run.Deck.Any(Upgrade.CanUpgradeCard),
            run.Deck.Any(Upgrade.CanUpgradeCard) ? "" : "鍛えられる札がない", () => ChooseDeckCard(run, "鍛える", Upgrade.CanUpgradeCard,
                index => Commit(() => Do(new RunCommand_ShopUpgrade { Index = index })), upgrade: true), "SourceShopUpgrade");
        if (!PhoneLayout) MerchantLabel(services, SourceRunScreenPolicy.ServicePriceNote, 15, SourcePalette.InkSoft);
        MerchantButton(layout.Footer, "店を出る", () => Commit(() => Do(new RunCommand_ShopLeave())), true, false).Name = "SourceShopLeave";
    }

    private void BuildShopGearShelf(RunState run, ShopState shop)
    {
        Heading("ギアの棚", $"自ターンに1個・魔素を払って組む。持ち物 {run.Gears?.Count ?? 0}/{Gears.GEAR_CARRY_MAX}・魔素 {Gears.ManaLabel(run.Mana.GetValueOrDefault())}・所持金 {run.Gold} G");
        var decision = new SourceUiDecision(_session?.Revision ?? 0);
        void Commit(Action action) { if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked)) action(); }
        var scroll = MerchantScroll(_body, "gears");
        float width = MerchantWidth;
        var shelf = MerchantGrid(scroll, "SourceShopGearShelf", Math.Clamp((int)(width / (270 * _textScale)), 1, (shop.Gears?.Count ?? 0) + 1));
        shelf.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        for (int i = 0; i < (shop.Gears?.Count ?? 0); i++)
        {
            int index = i; var offer = shop.Gears[i]; var gear = Content.GetGearDef(offer.Id);
            var col = MerchantGearCard(shelf, gear, gear.Id, $"残り {gear.Charges ?? 1} 回", gear.Text);
            MerchantPrice(col, offer.Price, offer.Sold != true && run.Gold >= offer.Price, offer.Sold == true ? "売切" : null,
                () => TakeGear(run, discard => Commit(() => Do(new RunCommand_ShopBuyGear { Index = index, DiscardIndex = discard })), incomingId: gear.Id), "SourceShopBuyGear" + index);
            if (offer.Sold == true) AddMerchantStamp(col.GetParent<Control>(), "売切", true);
        }
        int manaPrice = shop.ManaPrice ?? Run.SHOP_MANA_PRICE;
        bool full = run.Mana.GetValueOrDefault() >= Gears.MANA_MAX;
        var manaDef = new GearDef { Id = "mana", Name = "魔素", Rarity = "common", Family = "interfere" };
        var mana = MerchantGearCard(shelf, manaDef, "mana", $"1個ぶん（{Gears.GEAR_MANA_COST}）", SourceRunScreenPolicy.ManaOffer(run));
        MerchantPrice(mana, manaPrice, !full && run.Gold >= manaPrice, full ? "魔素は上限" : null,
            () => Commit(() => Do(new RunCommand_ShopBuyMana())), "SourceShopBuyMana");

        Text(scroll, $"持ち物 {run.Gears?.Count ?? 0}/{Gears.GEAR_CARRY_MAX} · 「捨てる」で枠を整理する（戻せない）", 20, Gold);
        var inventory = MerchantGrid(scroll, "SourceShopGearInventory", Math.Clamp((int)(width / (320 * _textScale)), 1, 4));
        for (int i = 0; i < (run.Gears?.Count ?? 0); i++)
        {
            int index = i; var owned = run.Gears[i]; var def = Content.GetGearDef(owned.GearId);
            var tile = MerchantOffer(inventory, "SourceShopOwnedGear" + i, def.Name, def.Text, "gears", def.Id,
                () => Open(def.Name, box => Text(ScrollColumn(box), def.Text, 23)), $"残り {owned.Charges} 回");
            MerchantButton(tile, "捨てる", () => Confirm("ギアを手放す？", def.Name + "を持ち物から取り除きます。",
                () => Commit(() => Do(new RunCommand_DiscardGear { Index = index })))).Name = "SourceShopDiscardGear" + i;
        }
        if ((run.Gears?.Count ?? 0) == 0) Text(scroll, "ギアを持っていません。", 20, Muted);
        var back = MerchantButton(_body, "戻る", () => ShowShopShelf(false));
        back.Name = "SourceShopShelfBack";
        back.SizeFlagsHorizontal = PhoneLayout ? SizeFlags.ShrinkEnd : SizeFlags.ShrinkCenter;
        back.CustomMinimumSize = new Vector2(220 * RunUnit, MinimumHit);
    }

    private float MerchantWidth => Math.Max(1, RunSafeRect.Size.X - 20);
    private void BeginMerchantVisit(RunState run)
    {
        string visit = $"{run.Seed}:{run.Phase}:{run.Act}:{run.Row}:{run.Col}";
        if (_merchantVisit != visit) { _merchantVisit = visit; _merchantScroll.Clear(); }
    }

    private (VBoxContainer Left, VBoxContainer Right, VBoxContainer Footer, float LeftWidth) MerchantColumns(string name, string key)
    {
        float width = MerchantWidth;
        float sideWidth = Math.Clamp((PhoneLayout ? 420 : 370) * _textScale, 370, 520);
        bool stacked = width < sideWidth + 470;
        if (stacked)
        {
            var all = MerchantScroll(_body, key + "-stacked");
            all.Name = name;
            var left = Column(all); var right = Column(all); var footer = Column(_body);
            return (left, right, footer, width - 22);
        }
        var row = Row(_body); row.Name = name; row.SizeFlagsVertical = SizeFlags.ExpandFill;
        row.AddThemeConstantOverride("separation", 22);
        var main = MerchantScroll(row, key + "-shelf");
        var side = Column(row); side.CustomMinimumSize = new Vector2(sideWidth, 0); side.SizeFlagsHorizontal = SizeFlags.Fill;
        side.SizeFlagsVertical = SizeFlags.ExpandFill;
        var stock = MerchantScroll(side, key + "-services");
        var bottom = Column(side);
        return (main, stock, bottom, width - sideWidth - 22 - 20);
    }

    private VBoxContainer MerchantScroll(Node parent, string key)
    {
        var col = ScrollColumn(parent);
        var scroll = col.GetParent<ScrollContainer>();
        scroll.Name = "SourceMerchantScroll_" + key;
        scroll.ClipContents = true; scroll.FollowFocus = true;
        scroll.MouseFilter = MouseFilterEnum.Stop;
        var bar = scroll.GetVScrollBar();
        double saved = _merchantScroll.GetValueOrDefault(key);
        Callable.From(() => { if (GodotObject.IsInstanceValid(scroll)) scroll.ScrollVertical = (int)saved; }).CallDeferred();
        bar.ValueChanged += value => { if (scroll.IsVisibleInTree()) _merchantScroll[key] = value; };
        col.AddThemeConstantOverride("separation", 18);
        return col;
    }

    private GridContainer MerchantGrid(Node parent, string name, int columns)
    {
        var grid = new GridContainer { Name = name, Columns = Math.Max(1, columns), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", 18); grid.AddThemeConstantOverride("v_separation", 18);
        parent.AddChild(grid); return grid;
    }

    private PanelContainer MerchantPaper(Node parent, string name, bool night = false)
    {
        if (!night) return RunPaper(parent, name, padding: 10 / RunUnit);
        var panel = new PanelContainer { Name = name, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass };
        var style = Box(SourcePalette.Window, SourcePalette.NightEdge, 2);
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight = style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 3;
        panel.AddThemeStyleboxOverride("panel", style); parent.AddChild(panel); return panel;
    }

    private Label MerchantLabel(Node parent, string text, int size = 18, Color? color = null)
    {
        var label = Text(parent, text, size, color ?? SourcePalette.Ink);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return label;
    }

    private Button MerchantButton(Node parent, string caption, Action action, bool enabled = true, bool brass = false)
    {
        var button = RunButton(parent, caption, action, enabled, primary: brass);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.AddThemeFontSizeOverride("font_size", FontSize(17));
        return button;
    }

    private Button MerchantPrice(Node parent, int price, bool available, string state, Action action, string name, bool rare = false)
    {
        var button = MerchantButton(parent, state ?? ((rare ? "★ " : "") + price + " G で買う"), action, available, true);
        button.Name = name; button.TooltipText = state ?? (available ? $"{price} G で購入" : $"{price} G · 今は買えません");
        button.SetMeta("merchant_price", price); button.SetMeta("merchant_available", available); button.SetMeta("merchant_state", state ?? "stock");
        if (!available && state == null)
        {
            button.AddThemeStyleboxOverride("disabled", SourceRunUiDecor.Paper(new Color("d6c1b9"), RunUnit, 10, true));
            button.AddThemeColorOverride("font_disabled_color", SourcePalette.BadInk);
        }
        return button;
    }

    private void MerchantCard(Node parent, CardInstance card, SourceMerchantUi.Shelf sizing, int price, bool available, string state,
        Action buy, string name, bool rare = false)
    {
        var cell = Column(parent); cell.Name = name + "Cell";
        cell.CustomMinimumSize = new Vector2(sizing.CellWidth, 0);
        var face = SourceCard(cell, card, card, () => InspectCard(card), true, null, sizing.CardScale, SourceInspectState);
        face.SetMeta("merchant_available", available);
        if (!available && state == null) face.Modulate = new Color(.86f, .86f, .9f);
        if (state != null) AddMerchantStamp(face, state == "売切" ? "売切" : "買った", false);
        var tag = MerchantPrice(cell, price, available, state, buy, name, rare);
        tag.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        tag.CustomMinimumSize = new Vector2(Math.Max(176 * _textScale, SourceCardFace.W * sizing.CardScale), MinimumHit);
        async System.Threading.Tasks.Task FitShelfCard() {
            for (int frame = 0; frame < 4; frame++) {
                if (!GodotObject.IsInstanceValid(cell) || !cell.IsInsideTree()) return;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            ScrollContainer viewport = null;
            for (Node ancestor = cell.GetParent(); ancestor != null; ancestor = ancestor.GetParent())
                if (ancestor is ScrollContainer candidate) { viewport = candidate; break; }
            if (viewport == null || parent is not Control shelfControl) return;
            float available = viewport.GetGlobalRect().End.Y - shelfControl.GetGlobalRect().Position.Y
                - tag.GetCombinedMinimumSize().Y - cell.GetThemeConstant("separation") - 28;
            if (available < 160) return; // Do not accept transient collapsed layout.
            float fitted = Math.Min(sizing.CardScale, available / SourceCardFace.H);
            face.Scale = Vector2.One * fitted;
            face.SetMeta("merchant_fitted_scale", fitted);
            var holder = (Control)face.GetParent();
            holder.CustomMinimumSize = new Vector2(SourceCardFace.W * fitted + 16, SourceCardFace.H * fitted + 16);
            holder.Size = holder.CustomMinimumSize;
        }
        Callable.From(() => { _ = FitShelfCard(); }).CallDeferred();
    }

    private VBoxContainer MerchantOffer(Node parent, string name, string title, string body, string group, string image, Action details, string note = "")
    {
        var panel = MerchantPaper(parent, name); var col = Column(panel);
        col.AddThemeConstantOverride("separation", 8);
        var row = Row(col);
        Picture(row, group, image, new Vector2(58, 58));
        var titles = Column(row);
        // The title/illustration row opens the complete description. The price
        // is the only purchase target, matching the source shop interaction.
        var info = Button(titles, title + (string.IsNullOrEmpty(note) ? "" : " · " + note) + "  ›", details);
        info.Name = name + "Details"; info.TooltipText = "詳しく: " + Plain(title);
        info.Alignment = HorizontalAlignment.Left; info.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        info.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        info.AddThemeFontSizeOverride("font_size", FontSize(20));
        info.AddThemeFontOverride("font", SourceRunUiDecor.Display);
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" }) info.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        info.AddThemeColorOverride("font_color", SourcePalette.Ink);
        info.AddThemeColorOverride("font_hover_color", SourcePalette.BrassInk);
        info.AddThemeColorOverride("font_pressed_color", SourcePalette.Ink);
        var description = MerchantLabel(col, body, 16, SourcePalette.InkSoft);
        description.Name = "OfferDescription";
        description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        description.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        description.TooltipText = Plain(body);
        return col;
    }

    private VBoxContainer MerchantCarriedOffer(Node parent, DepartureOffer offer)
    {
        var panel = MerchantPaper(parent, "SourceCarriedOffer_" + offer.Id);
        var col = Column(panel); col.AddThemeConstantOverride("separation", 5);
        var info = MerchantButton(col, offer.Name + "  ›", () => Open(offer.Name,
            box => Text(ScrollColumn(box), SourceMerchantUi.Details(offer), 22)));
        info.Name = "SourceCarriedDetails_" + offer.Id;
        info.TooltipText = SourceMerchantUi.Summary(offer);
        info.AddThemeFontSizeOverride("font_size", FontSize(17));
        foreach (string state in new[] { "normal", "hover", "pressed" }) info.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        if (!PhoneLayout)
        {
            var label = MerchantLabel(col, SourceMerchantUi.Summary(offer), 15, SourcePalette.InkSoft);
            label.MaxLinesVisible = 1; label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        }
        return col;
    }

    private void MerchantService(Node parent, string icon, string title, int price, string text, bool available, string reason, Action action, string name)
    {
        var service = Column(parent);
        service.Name = name + "Service";
        service.AddThemeConstantOverride("separation", 6);
        var row = Row(service);
        row.AddThemeConstantOverride("separation", 12);
        var iconWell = new PanelContainer { CustomMinimumSize = new Vector2(48, 48), SizeFlagsHorizontal = SizeFlags.Fill, MouseFilter = MouseFilterEnum.Ignore };
        iconWell.AddThemeStyleboxOverride("panel", SourceRunUiDecor.Paper(SourcePalette.Paper2, RunUnit, 4, true));
        row.AddChild(iconWell);
        var serviceMark = MerchantLabel(iconWell, icon == "exhaust" ? "除" : "鍛", 22, SourcePalette.BrassInk);
        serviceMark.HorizontalAlignment = HorizontalAlignment.Center;
        serviceMark.VerticalAlignment = VerticalAlignment.Center;
        serviceMark.TooltipText = title;
        var actionButton = MerchantButton(row, title + $"  {price} G", action, available);
        actionButton.Name = name;
        MerchantLabel(service, reason.Length > 0 ? reason : text, 16, reason.Length > 0 ? SourcePalette.BadInk : SourcePalette.InkSoft);
        service.TooltipText = text;
    }

    private VBoxContainer MerchantGearCard(Node parent, GearDef gear, string image, string charge, string body)
    {
        var panel = MerchantPaper(parent, "SourceMerchantGear_" + image);
        panel.CustomMinimumSize = new Vector2((PhoneLayout ? 260 : 240) * _textScale, 0);
        var col = Column(panel);
        var label = MerchantLabel(col, gear.Name, 23); label.HorizontalAlignment = HorizontalAlignment.Center;
        var rarity = MerchantLabel(col, RewardRarityName(gear.Rarity), 16, SourcePalette.BrassInk); rarity.HorizontalAlignment = HorizontalAlignment.Center;
        var artPanel = MerchantPaper(col, "GearArtWell", night: true);
        Picture(artPanel, "gears", image, new Vector2(96, 96));
        var uses = MerchantLabel(col, charge, 17, image == "mana" ? SourcePalette.ManaInk : SourcePalette.BrassInk); uses.HorizontalAlignment = HorizontalAlignment.Center;
        var description = MerchantLabel(col, body, 18, SourcePalette.InkSoft);
        description.CustomMinimumSize = new Vector2(0, FontSize(18) * 4.6f);
        description.SizeFlagsVertical = SizeFlags.ExpandFill; // Align all stock prices at the bottom of the shelf.
        return col;
    }

    private void AddShopSoldCover(Control face) => AddMerchantStamp(face, "売切", false);

    private void AddMerchantStamp(Control face, string caption, bool small)
    {
        // This overlay has no minimum size: putting a stamp directly inside a
        // PanelContainer would make that container resize the stamp to the tile.
        var overlay = new Control { Name = "SourceMerchantStampOverlay", MouseFilter = MouseFilterEnum.Ignore };
        face.AddChild(overlay); overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var stamp = new PanelContainer { Name = "SourceShopSold", MouseFilter = MouseFilterEnum.Ignore,
            Rotation = -.08f };
        stamp.AddThemeStyleboxOverride("panel", Box(SourcePalette.Paper3, SourcePalette.BadInk, 2));
        overlay.AddChild(stamp);
        stamp.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        stamp.OffsetLeft = small ? -104 : -154; stamp.OffsetRight = -8;
        stamp.OffsetTop = small ? 5 : 105; stamp.OffsetBottom = stamp.OffsetTop + (small ? 32 : 48);
        var label = MerchantLabel(stamp, caption, small ? 17 : 27, SourcePalette.BadInk);
        label.HorizontalAlignment = HorizontalAlignment.Center; label.AutowrapMode = TextServer.AutowrapMode.Off;
        if (!small && face.GetNodeOrNull<Control>("SourceCardFace") is Control art) art.Modulate = new Color(.62f, .62f, .66f);
    }

    private void ShowShopShelf(bool gears)
    {
        _shopGearsScreen = gears;
        Rebuild();
    }
}
