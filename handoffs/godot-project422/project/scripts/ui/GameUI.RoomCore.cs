// Room presentation reconstructed from the pinned Unity implementation.
// Commands, eligibility and fusion results remain engine-owned.
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private Button RoomButton(Node parent, string text, Action action, bool enabled = true, bool primary = false)
    {
        var button = RunButton(parent, text, action, enabled, primary);
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return button;
    }

    private VBoxContainer RoomOption(Node parent, string id, string title, string description, string icon)
    {
        var paper = RunPaper(parent, id);
        paper.SizeFlagsHorizontal = SizeFlags.Fill;
        paper.CustomMinimumSize = new Vector2(Math.Min(340, Math.Max(240, Size.X - 100)), 280);
        var column = Column(paper);
        Picture(column, "icons", icon, new Vector2(72, 72));
        var heading = Text(column, title, 27, Gold);
        heading.AddThemeFontOverride("font", SourceRunUiDecor.Display);
        Text(column, description, 19).SizeFlagsVertical = SizeFlags.ExpandFill;
        return column;
    }

    private void BuildCampfire(RunState run)
    {
        var opt = Run.CampfireOptions(run);
        bool fresh = run.CampfireUpgradesUsed == 0;
        bool noRest = run.Relics.Any(id => Content.GetRelicDef(id).Bonus?.NoRest == true);
        int remain = opt.Forge ? Math.Max(0, Run.CampfireForgeAllowed(run) - run.CampfireUpgradesUsed) : 0;
        Heading("焚き火", "どれか1つを選ぶ。鍛えた後は回復なしで立ち去る。");
        var scroll = ScrollColumn(_body);
        var scene = Row(scroll);
        scene.Alignment = BoxContainer.AlignmentMode.Center;
        Picture(scene, "scenes", "campfire", new Vector2(360, PhoneLayout ? 140 : 150));
        var flow = Flow(scroll);
        flow.Name = "CampfireChoices";
        flow.Alignment = FlowContainer.AlignmentMode.Center;
        flow.AddThemeConstantOverride("h_separation", 24);
        flow.AddThemeConstantOverride("v_separation", 24);
        var rest = RoomOption(flow, "CampfireRest", "休む", SourceDecisionInfo.CampfireRest(run), "burn");
        RoomButton(rest, fresh && !noRest ? "休む" : "回復なしで立ち去る", () => Do(new RunCommand_CampfireRest()), true);
        var forge = RoomOption(flow, "CampfireForge", "鍛える", SourceDecisionInfo.CampfireForge(run), "hammer");
        RoomButton(forge, "鍛える札を選ぶ", () => ChooseDeckCard(run, "鍛える", Upgrade.CanUpgradeCard,
            i => Do(new RunCommand_CampfireUpgrade { Index = i }), upgrade: true), remain > 0 && run.Deck.Any(Upgrade.CanUpgradeCard));
        if (opt.Dig && fresh)
        {
            var dig = RoomOption(flow, "CampfireDig", "発掘", "発掘の鶴嘴: レリックを1個掘る\n休む・鍛えるとは排他", "chest");
            RoomButton(dig, "発掘: 遺物を掘る", () => Do(new RunCommand_CampfireDig()));
        }
        if (opt.TrainLeft > 0 && fresh)
        {
            var train = RoomOption(flow, "CampfireTrain", "鍛錬", SourceDecisionInfo.CampfireTrain(run), "growth");
            RoomButton(train, $"鍛錬: 成長 +1 (あと {opt.TrainLeft} 回)", () => Do(new RunCommand_CampfireTrain()));
        }
        if (opt.Remove && fresh)
        {
            var remove = RoomOption(flow, "CampfireRemove", "取り除く", "安らぎの煙管: デッキの1枚を永久に除去\n休む・鍛えるとは排他" + (run.Deck.Count <= 5 ? "\nデッキが5枚以下のため取り除けない" : ""), "skull");
            RoomButton(remove, "取り除く: 安らぎの煙管", () => ChooseDeckCard(run, "取り除く", c => run.Deck.Count > 5,
                i => Do(new RunCommand_CampfireRemove { Index = i })), run.Deck.Count > 5);
        }
    }

    private void WorkshopMaterial(Node parent, RunState run, int index, string slot, Action clear)
    {
        var box = Column(parent);
        box.Name = "WorkshopMaterial" + slot;
        Text(box, "素材 " + slot, 18, Gold);
        // A full-width row preserves a readable horizontal touch label at130%.
        var remove = RoomButton(box, slot + "を外す", clear, index >= 0);
        remove.AutowrapMode = TextServer.AutowrapMode.Off;
        remove.Name = "WorkshopClear" + slot;
        remove.TooltipText = "素材 " + slot + " を外す";
        remove.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (index < 0)
        {
            var placeholder = RunPaper(box, "WorkshopEmptySlot" + slot, 12);
            var hint = Text(placeholder, "素材 " + slot + "\n札を選択", 17, Muted);
            hint.HorizontalAlignment = HorizontalAlignment.Center;
            hint.VerticalAlignment = VerticalAlignment.Center;
            placeholder.CustomMinimumSize = new Vector2(0, 100);
        }
        else
        {
            var card = run.Deck[index];
            SourceCard(box, card, card, () => InspectCard(card), true, null, .48f, SourceInspectState);
        }
    }

    private void BuildWorkshop(RunState run)
    {
        Heading("工房", "同じ色の札を2枚選んで合成する。素材の札は失われます。");
        if (_workshopA >= run.Deck.Count) _workshopA = -1;
        if (_workshopB >= run.Deck.Count) _workshopB = -1;
        var pairs = Fusion.RecipePairsInDeck(run.Deck);
        var row = Row(_body);
        row.SizeFlagsVertical = SizeFlags.ExpandFill;
        var deck = Column(row);
        CardGrid(deck, run.Deck, card =>
        {
            int index = run.Deck.ToList().FindIndex(c => c.Uid == card.Uid);
            (_workshopA, _workshopB) = SourceRoomUiPolicy.SelectMaterial(_workshopA, _workshopB, index);
            Rebuild();
        }, null, card =>
        {
            int index = run.Deck.ToList().FindIndex(c => c.Uid == card.Uid);
            return index == _workshopA ? "素材 A" : index == _workshopB ? "素材 B" :
                SourceRoomUiPolicy.RecipeMaterial(pairs, _workshopA, _workshopB, index) ? "★ レシピの素材" : "選ぶ";
        });
        var paper = RunPaper(row, "WorkshopSide", 12);
        paper.SizeFlagsHorizontal = SizeFlags.Fill;
        paper.CustomMinimumSize = new Vector2(Math.Clamp(Size.X * .30f, 290, 400), 0);
        var side = Column(paper);
        // Only the content scrolls. Confirm/skip remain reachable at larger text sizes.
        var content = ScrollColumn(side);
        content.GetParent().Name = "WorkshopPreviewScroll";
        var slots = Row(content);
        WorkshopMaterial(slots, run, _workshopA, "A", () => { _workshopA = -1; Rebuild(); });
        WorkshopMaterial(slots, run, _workshopB, "B", () => { _workshopB = -1; Rebuild(); });
        Text(content, "▼ 合成結果", 18, Gold);
        string blocked = SourceRoomUiPolicy.WorkshopBlocked(run, _workshopA, _workshopB);
        if (_workshopA >= 0 && _workshopB >= 0)
        {
            var a = run.Deck[_workshopA];
            var b = run.Deck[_workshopB];
            if (Fusion.FuseBlockReason(a, b) == null)
            {
                var result = new CardInstance { Uid = "fusion-preview", Def = Run.WorkshopFuseResult(run, a, b) };
                float scale = SourceRoomUiPolicy.WorkshopResultScale(Size.Y, 470 * _textScale, PhoneLayout);
                SourceCard(content, result, result, () => InspectCard(result), true, "合成結果", scale, SourceInspectState);
                Text(content, string.Join("\n", Fusion.FusionNotes(a, b)), 17, Muted);
            }
        }
        else Text(content, SourceRoomUiPolicy.PartnerGuide(run, pairs, _workshopA, _workshopB), 18, Muted);
        if (blocked != null && _workshopA >= 0 && _workshopB >= 0)
            Text(content, blocked, 17, Danger);
        int price = Run.WorkshopFusePrice(run);
        var confirm = RoomButton(side, $"合成する  {price} G", () =>
        {
            int a = _workshopA, b = _workshopB;
            _workshopA = _workshopB = -1;
            Do(new RunCommand_WorkshopFuse { IndexA = a, IndexB = b });
        }, blocked == null, true);
        confirm.Name = "WorkshopConfirm";
        RoomButton(side, "見送る", () => { _workshopA = _workshopB = -1; Do(new RunCommand_WorkshopSkip()); }).Name = "WorkshopSkip";
    }

    private void BuildEvent(RunState run)
    {
        if (run.EventId == null)
        {
            Heading("出来事を表示できません", "イベント情報が見つかりません。進行データを変更せず、メニューから記録を書き出せます。");
            RoomButton(_body, "メニューを開く", OpenMenu);
            return;
        }
        var ev = Content.GetEventDef(run.EventId);
        Heading(ev.Name);
        var scroll = ScrollColumn(_body);
        scroll.GetParent().Name = "EventStoryScroll";
        var center = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(center);
        var paper = RunPaper(center, "EventStoryPaper", 24);
        paper.CustomMinimumSize = new Vector2(Math.Min(Size.X - 80 * RunUnit, 1040 * RunUnit), 0);
        var page = Column(paper);
        var story = Row(page);
        Picture(story, "scenes", "event", new Vector2(PhoneLayout ? 190 : 280, PhoneLayout ? 128 : 180));
        var flavor = Text(story, ev.Flavor, 22);
        flavor.AddThemeConstantOverride("line_spacing", 6);
        for (int i = 0; i < ev.Choices.Count; i++)
        {
            int index = i;
            var choice = ev.Choices[i];
            var choiceBox = Column(page);
            choiceBox.Name = "EventChoice_" + index;
            var button = RoomButton(choiceBox, SourceDecisionInfo.EventLabel(run, choice), () =>
                ResolveChoice(run, choice, choice.Label, card => Do(new RunCommand_EventChoice { Index = index, CardIndex = card })),
                Run.EventChoiceAvailable(run, choice));
            button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            button.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            button.Alignment = HorizontalAlignment.Left;
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            string hint = SourceDecisionInfo.ChoiceHint(choice);
            if (hint.Length > 0 && !button.Text.Contains(hint)) Text(choiceBox, hint, 18, Gold);
        }
    }
}
