// Reward and relic presentation follows the verified Unity 7a84644 RewardScreen.
// Narrow layouts intentionally wrap/scroll offers, keeping decisions outside the
// scroll viewport. This is source-backed reconstruction, not a7f86b5 pixel parity.
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private void BuildReward(RunState run)
    {
        bool cardsPending = run.RewardOptions != null;
        bool hasGear = run.GearOption != null;
        bool arrival = BeginRewardArrival("reward");
        Heading(cardsPending ? "カード報酬 · 1枚を選ぶ" : "ギア報酬");
        var decision = new SourceUiDecision(_session?.Revision ?? 0);
        void Act(Action action) { if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked)) action(); }
        var offers = RewardGallery(_body, "SourceRewardOffers");
        float scale = RewardCardScale;
        if (cardsPending)
        {
            for (int i = 0; i < run.RewardOptions.Count; i++)
            {
                int index = i;
                var card = new CardInstance { Uid = "reward-" + i, Def = Content.GetCardDef(run.RewardOptions[i]) };
                var cell = RewardCell(offers, SourceCardFace.W * scale + 16);
                cell.Name = "SourceRewardCardOffer_" + i;
                // The common heading carries the instruction; no repeated numbering/caption.
                cell.TooltipText = "札を詳しく見る・下のボタンで取得";
                var tile = SourceCard(cell, card, card, () => InspectCard(card), true, null, scale, null);
                RewardOfferHover(tile);
                RewardArrivalAccent(tile, arrival);
                RewardButton(cell, "札を取る", () => Act(() => Do(new RunCommand_PickReward { Index = index })), true, true).Name = "SourceRewardTakeCard_" + i;
            }
            if (run.RewardOptions.Count == 0)
            {
                var none = RewardCell(offers, 320 * RewardUnit);
                RewardLabel(none, "候補がありません", 23, Gold);
                RewardLabel(none, "札を見送って先へ進めます。", 18, Muted);
            }
        }
        if (hasGear)
        {
            var gear = Content.GetGearDef(run.GearOption);
            var cell = RewardCell(offers, SourceCardFace.W * scale + 16);
            cell.Name = "SourceRewardGearOffer";
            RewardLabel(cell, cardsPending ? "追加報酬 · ギア" : "ギア", 18, Gold);
            var gearFace = RewardGearFace(cell, gear, gear.Charges ?? 1, SourceCardFace.W * scale + 16, SourceCardFace.H * scale - 24 * RewardUnit);
            RewardArrivalAccent(gearFace, arrival);
            RewardButton(cell, Run.GearFull(run) ? "ギアを入れ替える" : $"ギアを取る（{(run.Gears?.Count ?? 0) + 1}/{Gears.GEAR_CARRY_MAX}）",
                () => Act(() => TakeGear(run, discard => Do(new RunCommand_TakeGear { DiscardIndex = discard }))), true, true).Name = "SourceRewardTakeGear";
            if (cardsPending && !PhoneLayout)
                RewardButton(cell, "ギアを見送る", () => Act(() => Do(new RunCommand_SkipGear()))).Name = "SourceRewardSkipGear";
            else if (!cardsPending) RewardLabel(cell, "魔素 " + Gears.ManaLabel(run.Mana.GetValueOrDefault()), 16, Muted);
        }
        var actions = RewardActions(_body);
        RewardButton(actions, cardsPending ? "札を見送る" : "ギアを見送る", () => Act(() => {
            if (cardsPending) Do(new RunCommand_SkipReward()); else Do(new RunCommand_SkipGear());
        })).Name = cardsPending ? "SourceRewardSkipCards" : "SourceRewardSkipGear";
        if (PhoneLayout && cardsPending && hasGear)
            RewardButton(actions, "ギアを見送る", () => Act(() => Do(new RunCommand_SkipGear()))).Name = "SourceRewardSkipGear";
        if ((_session?.History.Count ?? 0) > 0) RewardButton(actions, "直前の戦闘を評価", OpenRating);
    }

    private void BuildRelicReward(RunState run)
    {
        int left = run.RelicPicksLeft ?? 1;
        bool arrival = BeginRewardArrival("relic-reward");
        Heading("レリック", left > 1 ? $"あと {left} 個選べる。見送ってもよい。" : "1個選んで持ち帰る。見送ってもよい。");
        var decision = new SourceUiDecision(_session?.Revision ?? 0);
        void Act(Action action) { if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked)) action(); }
        var offers = RewardGallery(_body, "SourceRelicOffers");
        var options = run.RelicOptions ?? Array.Empty<string>();
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var relic = Content.GetRelicDef(options[i]);
            var panel = RewardPaper(offers, "SourceRelicOffer_" + i, (PhoneLayout ? 300 : 340) * RewardUnit, 280 * RewardUnit);
            RewardArrivalAccent((Control)panel.GetParent(), arrival);
            Picture(panel, "relics", relic.Id, new Vector2(84, 84) * RewardUnit);
            RewardLabel(panel, relic.Name, 25, SourcePalette.Ink, true);
            RewardLabel(panel, RewardRarityName(relic.Rarity), 16, SourcePalette.BrassInk);
            var description = RewardLabel(panel, relic.Description, 19, SourcePalette.Ink);
            description.SizeFlagsVertical = SizeFlags.ExpandFill;
            description.VerticalAlignment = VerticalAlignment.Center;
            RewardButton(panel, "取る", () => Act(() => Do(new RunCommand_PickRelic { Index = index })), true, true).Name = "SourceRewardTakeRelic_" + i;
        }
        if (options.Count == 0) RewardLabel(offers, "候補がありません", 23, Muted);
        var actions = RewardActions(_body);
        RewardButton(actions, "レリックを見送る", () => Act(() => Do(new RunCommand_SkipRelic()))).Name = "SourceRewardSkipRelic";
    }

    private void BuildRelicChoose(RunState run)
    {
        var pending = run.PendingRelicChoice;
        if (pending == null) return;
        bool remove = pending.Mode == "remove";
        int limit = remove ? Math.Min(pending.Count, Math.Max(0, run.Deck.Count - 5)) : pending.Count;
        string verb = remove ? "取り除く" : "変成して鍛える";
        Heading(Content.GetRelicDef(pending.RelicId).Name,
            $"{limit}枚まで選んで決定。選ばなくてもよい。" + (remove ? " デッキは5枚を下回れません。" : "同じレア度の別の札に変わり、鍛えた状態で入ります。状態異常・烙印は選べません。"));
        int Index(CardInstance card) => run.Deck.ToList().FindIndex(c => c.Uid == card.Uid);
        CardGrid(_body, run.Deck, card => {
            int index = Index(card);
            if (!_relicPicks.Remove(index) && _relicPicks.Count < limit) _relicPicks.Add(index);
            Rebuild();
        }, card => (remove || Run.CanTransformCard(card)) && (_relicPicks.Contains(Index(card)) || _relicPicks.Count < limit),
            card => _relicPicks.Contains(Index(card)) ? "選択中" : "選ぶ",
            lockReason: card => !remove && !Run.CanTransformCard(card) ? SourceDecisionInfo.TransformLockReason : null);
        var actions = RewardActions(_body);
        var count = RewardLabel(actions, $"選択 {_relicPicks.Count} / {limit} 枚", 20, Gold);
        count.Name = "SourceRelicChooseCount";
        count.AutowrapMode = TextServer.AutowrapMode.Off;
        count.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        count.VerticalAlignment = VerticalAlignment.Center;
        var decision = new SourceUiDecision(_session?.Revision ?? 0);
        RewardButton(actions, _relicPicks.Count == 0 ? "選ばずに決定" : $"決定（{_relicPicks.Count}枚を{verb}）", () => {
            if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked))
                Do(new RunCommand_RelicChooseCards { Indices = _relicPicks.OrderBy(i => i).ToArray() });
        }, true, true).Name = "SourceRelicChooseConfirm";
    }

    // Signature is also used by the merchant. Opening, inspecting and cancelling
    // never mutates inventory; the captured revision allows one accepted swap.
    private void TakeGear(RunState run, Action<int?> submit, string? incomingId = null)
    {
        if (!Run.GearFull(run)) { submit(null); return; }
        incomingId ??= run.GearOption;
        Open("ギアの入れ替え", box => {
            var decision = new SourceUiDecision(_session?.Revision ?? 0);
            RewardLabel(box, $"所持 {run.Gears.Count}/{Gears.GEAR_CARRY_MAX} 個 · 手放すギアを1個選んで入れ替える", 21, Gold);
            var scroll = ScrollColumn(box);
            var row = Flow(scroll); row.Name = "SourceGearSwapOffers"; row.Alignment = FlowContainer.AlignmentMode.Center;
            if (!string.IsNullOrEmpty(incomingId))
            {
                var incoming = Content.GetGearDef(incomingId);
                var offer = RewardCell(row, 250 * RewardUnit);
                RewardLabel(offer, "受け取るギア", 20, Gold);
                RewardGearFace(offer, incoming, incoming.Charges ?? 1, 250 * RewardUnit, 280 * RewardUnit);
                RewardLabel(offer, "入れ替え後に持ち物へ", 17, Muted);
            }
            for (int i = 0; i < run.Gears.Count; i++)
            {
                int index = i;
                var owned = run.Gears[i];
                var def = Content.GetGearDef(owned.GearId);
                var offer = RewardCell(row, 250 * RewardUnit);
                offer.Name = "SourceGearSwapOwned_" + i;
                RewardLabel(offer, "持ち物 " + (i + 1), 20, Muted);
                RewardGearFace(offer, def, owned.Charges, 250 * RewardUnit, 280 * RewardUnit);
                RewardButton(offer, "これと入れ替える", () => {
                    if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked)) submit(index);
                }, true, true).Name = "SourceGearSwapSelect_" + i;
            }
            RewardButton(RewardActions(box), "やめる", CloseOverlay).Name = "SourceGearSwapCancel";
        });
    }

    private static string RewardRarityName(string rarity) => rarity switch {
        "common" => "コモン", "uncommon" => "アンコモン", "rare" => "レア",
        "boss" => "ボス報酬", "event" => "イベント", "shop" => "商店限定", _ => CardText.RarityLabel(rarity)
    };

    private float RewardUnit => PhoneLayout ? Sizing.SourceScale : 1f;
    private float RewardCardScale => PhoneLayout ? .90f * RewardUnit : 1.05f;

    // A centered gallery when it fits; wrapping plus vertical scrolling at small
    // widths or large text. Bottom decisions are siblings, so remain reachable.
    private HFlowContainer RewardGallery(Node parent, string name)
    {
        var column = ScrollColumn(parent);
        column.Name = name + "Viewport";
        column.SizeFlagsVertical = SizeFlags.ExpandFill;
        column.AddThemeConstantOverride("separation", 0);
        var top = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        column.AddChild(top);
        var inset = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        inset.AddThemeConstantOverride("margin_top", 10);
        inset.AddThemeConstantOverride("margin_bottom", 16);
        column.AddChild(inset);
        var flow = Flow(inset);
        flow.Name = name;
        flow.Alignment = FlowContainer.AlignmentMode.Center;
        flow.AddThemeConstantOverride("h_separation", (int)(36 * RewardUnit));
        flow.AddThemeConstantOverride("v_separation", (int)(24 * RewardUnit));
        var bottom = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        column.AddChild(bottom);
        return flow;
    }

    private VBoxContainer RewardCell(Node parent, float width)
    {
        var cell = Column(parent);
        cell.CustomMinimumSize = new Vector2(width, 0);
        cell.SizeFlagsHorizontal = SizeFlags.Fill;
        cell.AddThemeConstantOverride("separation", 8);
        return cell;
    }

    private VBoxContainer RewardPaper(Node parent, string name, float width, float minHeight)
    {
        var panel = new PanelContainer {
            Name = name, CustomMinimumSize = new Vector2(width, minHeight),
            SizeFlagsHorizontal = SizeFlags.Fill, SizeFlagsVertical = SizeFlags.Fill,
            MouseFilter = MouseFilterEnum.Pass
        };
        panel.SetMeta(RunPaperMarker, true);
        var style = SourceRunUiDecor.Paper(SourcePalette.Paper, RewardUnit, 18);
        panel.AddThemeStyleboxOverride("panel", style);
        parent.AddChild(panel);
        var column = Column(panel);
        column.AddThemeConstantOverride("separation", 8);
        return column;
    }

    private VBoxContainer RewardGearFace(Node parent, GearDef def, int charges, float width, float minHeight)
    {
        var face = RewardPaper(parent, "SourceRewardGearFace_" + def.Id, width, minHeight);
        Picture(face, "gears", def.Id, new Vector2(76, 76) * RewardUnit);
        RewardLabel(face, def.Name, 23, SourcePalette.Ink, true);
        RewardLabel(face, RewardRarityName(def.Rarity) + $" · 残り{charges}回", 16, SourcePalette.BrassInk);
        var body = RewardLabel(face, def.Text, 18, SourcePalette.Ink);
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.VerticalAlignment = VerticalAlignment.Center;
        return face;
    }

    private Label RewardLabel(Node parent, string text, int size, Color color, bool decorative = false)
    {
        var label = Text(parent, text, size, color);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeFontOverride("font", SourceRunUiDecor.Body);
        if (decorative)
            label.AddThemeFontOverride("font", SourceRunUiDecor.Display);
        return label;
    }

    private HFlowContainer RewardActions(Node parent)
    {
        var actions = Flow(parent);
        actions.Name = "SourceRewardActions";
        actions.Alignment = FlowContainer.AlignmentMode.Center;
        return actions;
    }

    private Button RewardButton(Node parent, string text, Action action, bool enabled = true, bool primary = false)
    {
        var button = Button(parent, text, action, enabled, primary ? SourcePalette.Ink : null);
        SourceRunUiDecor.Button(button, RewardUnit, primary);
        button.SizeFlagsHorizontal = parent is VBoxContainer ? SizeFlags.ExpandFill : SizeFlags.ShrinkCenter;
        button.CustomMinimumSize = new Vector2(parent is VBoxContainer ? 0 : 220 * RewardUnit, Math.Max(MinimumHit, 48 * RewardUnit));
        return button;
    }
}
