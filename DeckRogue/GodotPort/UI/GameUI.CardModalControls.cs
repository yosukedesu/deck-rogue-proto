// Source-backed reconstruction of Unity RunUi.CardGrid/CardPopup/GearUi controls.
// Whole-run screenshot parity is not established; source geometry is used here.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private bool _showGridUpgraded;
    private sealed class SourceGridState { public string SelectedUid; }
    // The owner is the current overlay delegate or run snapshot. Returning from an
    // inspector restores the same owner; opening the picker again gets a fresh owner.
    private readonly ConditionalWeakTable<object, Dictionary<string, SourceGridState>> _sourceGridStates = new();

    private GameState SourceInspectState => CurrentRoute == "combat" ? InputCombat : null;

    private CardTile SourceCard(Node parent, CardInstance original, CardInstance shown, Action action,
        bool enabled, string badge, float scale, GameState state, bool drag = false, int? cost = null,
        bool selected = false, string corner = "")
    {
        var holder = new Control {
            Name = "SourceCardCell", CustomMinimumSize = new Vector2(SourceCardFace.W * scale + 16, SourceCardFace.H * scale + 16),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter, SizeFlagsVertical = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Ignore
        };
        parent.AddChild(holder);
        var tile = new CardTile {
            Name = "SourceGridCard", CardUid = original.Uid, Position = new Vector2(8, 8),
            Size = new Vector2(SourceCardFace.W, SourceCardFace.H), Scale = Vector2.One * scale,
            AllowDrag = drag && enabled && !IsInputBlocked,
            // A locked face still receives right-click / long-press inspection.
            Disabled = false
        };
        holder.AddChild(tile);
        SourceCardFace.Fill(tile, shown, state, _textScale, PhoneLayout, -1, cost, badge, enabled);
        tile.SetMeta("source_original_uid", original.Uid);
        tile.SetMeta("source_grid_selected", selected);
        tile.SetMeta("source_upgraded_preview", !ReferenceEquals(shown, original));
        tile.Inspect = () => { if (!IsInputBlocked && tile.IsVisibleInTree()) InspectCard(original); };
        tile.Pressed += () => {
            if (!IsInputBlocked && tile.IsVisibleInTree() && !tile.ConsumeInspection() && enabled) {
                Sound("ui.click"); action?.Invoke();
            }
        };
        if (selected || corner == "★")
        {
            var ring = new Panel { Name = "SelectedCardBorder", MouseFilter = MouseFilterEnum.Ignore,
                Position = new Vector2(3, 3), Size = holder.CustomMinimumSize - new Vector2(6, 6) };
            ring.AddThemeStyleboxOverride("panel", Box(new Color(0, 0, 0, 0), Gold, selected ? 3 : 1));
            holder.AddChild(ring);
        }
        if (!string.IsNullOrEmpty(corner))
        {
            var mark = new PanelContainer { Name = "CardCornerMark", MouseFilter = MouseFilterEnum.Ignore,
                Position = new Vector2(holder.CustomMinimumSize.X - 38, 0), CustomMinimumSize = new Vector2(36, 36) };
            mark.AddThemeStyleboxOverride("panel", Box(Gold, SourcePalette.BrassInk));
            holder.AddChild(mark);
            var label = Text(mark, corner, 19, SourcePalette.Ink);
            label.HorizontalAlignment = HorizontalAlignment.Center;
        }
        return tile;
    }

    private void SourceCardGrid(Node parent, IReadOnlyList<CardInstance> cards, Action<CardInstance> onPick,
        Func<CardInstance, bool> enabled, Func<CardInstance, string> badge, bool confirmPick,
        string actionLabel, Func<CardInstance, string> lockReason)
    {
        bool inspectOnly = onPick == null || onPick == InspectCard;
        bool phoneConfirm = PhoneLayout && confirmPick && !inspectOnly;
        object owner = (object)_overlay ?? (object)_view ?? this;
        string key = string.Join("\u001f", cards.Select(c => c.Uid));
        var owners = _sourceGridStates.GetOrCreateValue(owner);
        if (!owners.TryGetValue(key, out var selection)) owners[key] = selection = new SourceGridState();
        if (!cards.Any(c => c.Uid == selection.SelectedUid)) selection.SelectedUid = null;
        var revision = _session?.Revision ?? 0;
        bool OwnerCurrent() => ReferenceEquals(owner, (object)_overlay ?? (object)_view ?? this) && (_session?.Revision ?? 0) == revision;
        void Pick(CardInstance c) {
            if (!OwnerCurrent() || IsInputBlocked || !(enabled?.Invoke(c) ?? true) || !string.IsNullOrEmpty(lockReason?.Invoke(c))) return;
            onPick?.Invoke(c);
        }
        var scroll = ScrollColumn(parent);
        // Keep the bottom row and its action button clear of the scroll edge.
        var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        margin.AddThemeConstantOverride("margin_left", 6); margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 6); margin.AddThemeConstantOverride("margin_bottom", 28);
        scroll.AddChild(margin);
        var flow = Flow(margin);
        if (cards.Count == 0) Text(scroll, "（空）", 20, Muted);
        foreach (var card in cards)
        {
            var cell = Column(flow); cell.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            float scale = PhoneLayout ? .86f : .8f;
            cell.CustomMinimumSize = new Vector2(SourceCardFace.W * scale + 16, 0);
            string note = badge?.Invoke(card) ?? "";
            string locked = lockReason?.Invoke(card);
            string corner = SourceCardModalPolicy.CornerMark(note);
            bool allowed = (enabled?.Invoke(card) ?? true) && string.IsNullOrEmpty(locked);
            bool marked = corner == "A" || corner == "B" || corner == "✓" || (phoneConfirm && selection.SelectedUid == card.Uid);
            var shown = SourceCardModalPolicy.Preview(card, _showGridUpgraded);
            Action tap = inspectOnly || !PhoneLayout ? () => InspectCard(card) :
                phoneConfirm ? () => { if (OwnerCurrent()) { selection.SelectedUid = card.Uid; Rebuild(); } } : () => Pick(card);
            var tile = SourceCard(cell, card, shown, tap, allowed || inspectOnly, note, scale, SourceInspectState, selected: marked, corner: corner);
            if (!allowed && !inspectOnly) {
                // The face is dimmed, but its inspection hook remains active.
                Text(cell, locked ?? (string.IsNullOrEmpty(note) ? "選べません" : note), 15, Danger).CustomMinimumSize = new Vector2(0, 38);
            }
            if (!inspectOnly && !PhoneLayout) Button(cell, string.IsNullOrEmpty(note) ? actionLabel : note, () => Pick(card), allowed, marked ? Gold : null);
        }
        var controls = Flow(parent);
        Button(controls, (_showGridUpgraded ? "✓ " : "□ ") + "鍛えた後を見る", () => {
            if (OwnerCurrent()) { _showGridUpgraded = !_showGridUpgraded; Rebuild(); }
        }, true, _showGridUpgraded ? Gold : null);
        if (phoneConfirm) {
            var picked = cards.FirstOrDefault(c => c.Uid == selection.SelectedUid);
            bool allowed = picked != null && (enabled?.Invoke(picked) ?? true) && string.IsNullOrEmpty(lockReason?.Invoke(picked));
            Button(controls, picked == null ? "札を選んでください" : picked.Def.Name + " を" + actionLabel,
                () => { if (picked != null) Pick(picked); }, allowed, Gold).Name = "SourceGridConfirm";
        }
    }

    private void SourceInspectCard(CardInstance original)
    {
        bool upgraded = false;
        Open(original.Def.Name, box => {
            var shown = SourceCardModalPolicy.Preview(original, upgraded);
            var state = SourceInspectState;
            var scroll = ScrollColumn(box);
            var flow = Flow(scroll);
            var face = SourceCard(flow, original, shown, null, true, null, 1.6f, state);
            face.Inspect = null;
            var notes = Column(flow); notes.CustomMinimumSize = new Vector2(340, 0);
            string full = SourceCardFace.BodyText(shown, state, -1);
            Text(notes, "全文", 21, Gold);
            Text(notes, full, 21);
            foreach (string term in KeywordHelp.FindIn(full)) Text(notes, term + ": " + KeywordHelp.Terms[term], 18, Muted);
            var controls = Flow(box);
            if (Upgrade.CanUpgradeCard(original)) Button(controls, upgraded ? "元の札を見る" : "鍛えた後を見る", () => { upgraded = !upgraded; Rebuild(); }, true, Gold);
            else Text(controls, SourceCardModalPolicy.UpgradeState(original), 19, Muted);
            Button(controls, "閉じる", CloseOverlay);
        });
    }
}
