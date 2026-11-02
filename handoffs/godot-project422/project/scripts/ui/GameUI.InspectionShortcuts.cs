using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private sealed class DeckBrowseContext
    {
        public IReadOnlyList<CardInstance> Cards;
        public long Revision;
    }
    private sealed class InspectionShortcuts
    {
        public long Revision;
        public SourceUiDecision Decision;
        public CardInstance[] Cards = Array.Empty<CardInstance>();
        public int RewardIndex = -1;
        public string RewardId;
    }
    private readonly ConditionalWeakTable<object, DeckBrowseContext> _deckBrowseContexts = new();
    private readonly ConditionalWeakTable<object, InspectionShortcuts> _inspectionShortcuts = new();

    private void RegisterDeckBrowser(IReadOnlyList<CardInstance> deck)
    {
        if (_overlay != null)
            _deckBrowseContexts.Add(_overlay, new DeckBrowseContext { Cards = deck, Revision = _session?.Revision ?? 0 });
    }

    // Context is owned by the exact detail renderer, rather than a global index.
    // Rebuilding a preview must not revive an accepted action or use newer stock.
    private void AddInspectionShortcuts(VBoxContainer box, CardInstance original)
    {
        object owner = _overlay;
        if (owner == null) return;
        long revision = _session?.Revision ?? 0;
        if (!_inspectionShortcuts.TryGetValue(owner, out var context))
        {
            context = new InspectionShortcuts { Revision = revision, Decision = new SourceUiDecision(revision) };
            _inspectionShortcuts.Add(owner, context);
            if (_overlayStack.Count > 0 && _deckBrowseContexts.TryGetValue(_overlayStack.Peek().Item2, out var browse) && browse.Revision == revision)
            {
                string query = "";
                string key = string.Join("\u001f", browse.Cards.Select(c => c.Uid));
                if (_sourceGridStates.TryGetValue(_overlayStack.Peek().Item2, out var states) && states.TryGetValue(key, out var grid) && _gridFilterStates.TryGetValue(grid, out var filter)) query = filter.Query.Trim();
                context.Cards = browse.Cards.Where(c => query.Length == 0 || (c.Def.Name + "\n" + Plain(SourceCardFace.BodyText(c, SourceInspectState))).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            }
            var options = _session?.CurrentRun?.RewardOptions;
            if (CurrentRoute == "reward" && options != null && original.Uid.StartsWith("reward-", StringComparison.Ordinal) && int.TryParse(original.Uid.Substring(7), out int index) && index >= 0 && index < options.Count && options[index] == original.Def.Id)
            {
                context.RewardIndex = index;
                context.RewardId = options[index];
            }
        }
        bool Current() => ReferenceEquals(_overlay, owner) && (_session?.Revision ?? 0) == context.Revision && !IsInputBlocked;
        if (context.RewardIndex >= 0)
        {
            var take = RunButton(box, "「" + original.Def.Name + "」を取る", () =>
            {
                var options = _session?.CurrentRun?.RewardOptions;
                if (Current() && CurrentRoute == "reward" && options != null && context.RewardIndex < options.Count && options[context.RewardIndex] == context.RewardId && context.Decision.TryAccept(_session.Revision, IsInputBlocked))
                    Do(new RunCommand_PickReward { Index = context.RewardIndex });
            }, revision == context.Revision, true);
            take.Name = "SourceInspectTakeReward";
            take.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            take.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        }
        int current = Array.FindIndex(context.Cards, c => c.Uid == original.Uid);
        if (current < 0 || context.Cards.Length < 2) return;
        var row = Row(box); row.Name = "SourceInspectNavigation";
        void Navigate(int index)
        {
            if (!Current() || index < 0 || index >= context.Cards.Length) return;
            // Replace just the detail. Its parent list (and search/scroll state)
            // stays on the stack, so Back always returns in one operation.
            _overlay = null;
            InspectCard(context.Cards[index]);
        }
        var previous = RunButton(row, "前の札", () => Navigate(current - 1), current > 0 && revision == context.Revision);
        previous.Name = "SourceInspectPrevious";
        var count = Text(row, $"{current + 1} / {context.Cards.Length}", 18, SourcePalette.InkSoft);
        count.Name = "SourceInspectPosition";
        count.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        count.HorizontalAlignment = HorizontalAlignment.Center;
        count.VerticalAlignment = VerticalAlignment.Center;
        var next = RunButton(row, "次の札", () => Navigate(current + 1), current + 1 < context.Cards.Length && revision == context.Revision);
        next.Name = "SourceInspectNext";
    }
}
