using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using Godot;
using DeckRogue.GodotPort.Core;

namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    // Each overlay renderer is its stable lifetime identity, including nested
    // inspector round-trips. A newly opened overlay intentionally starts at top.
    private readonly ConditionalWeakTable<object, Dictionary<int, SourceGridState>> _modalScrollStates = new();

    private sealed class ModalFocusState { public int[] Path = Array.Empty<int>(); public string Text = ""; public string Type = ""; }
    private readonly ConditionalWeakTable<object, ModalFocusState> _modalFocusStates = new();
    private object _renderedModalOwner;
    private void CaptureModalFocusBeforeRebuild()
    {
        if (_renderedModalOwner == null || _overlayNode == null || !GodotObject.IsInstanceValid(_overlayNode)) return;
        var focus = GetViewport().GuiGetFocusOwner();
        if (focus == null || !_overlayNode.IsAncestorOf(focus)) return;
        var path = new List<int>();
        for (Node n = focus; n != _overlayNode && n != null; n = n.GetParent()) path.Add(n.GetIndex());
        path.Reverse();
        var state = _modalFocusStates.GetOrCreateValue(_renderedModalOwner);
        state.Path = path.ToArray(); state.Type = focus.GetType().Name;
        state.Text = focus is Button b ? NormalizeFocusText(b.Text) : "";
    }
    private static string NormalizeFocusText(string text) => text.TrimStart('●', '○', '✓', '□', ' ');
    private Control RestoreModalFocus()
    {
        if (!_gridKeyboardNavigation || !_overlayCanClose || _overlay == null || _overlayNode == null || !_modalFocusStates.TryGetValue(_overlay, out var state)) return null;
        if (state.Text.Length > 0)
        {
            var matches = Buttons(_overlayNode).Where(b => !b.Disabled && NormalizeFocusText(b.Text) == state.Text).ToArray();
            if (matches.Length == 1) return matches[0];
            return null; // A vanished action must never transfer focus to a different action.
        }
        Node found = _overlayNode;
        foreach (int i in state.Path) { if (i < 0 || i >= found.GetChildCount()) return null; found = found.GetChild(i); }
        return found is Control c && c.GetType().Name == state.Type && c.FocusMode == FocusModeEnum.All && !(c is BaseButton bb && bb.Disabled) ? c : null;
    }

    private void RetainModalScrollPositions()
    {
        _renderedModalOwner = _overlay;
        if (_overlay == null || _overlayNode == null) return;
        var states = _modalScrollStates.GetOrCreateValue(_overlay);
        int ordinal = 0;
        foreach (var scroll in ModalScrollContainers(_overlayNode))
        {
            if (scroll.Name == "SourceCardGridScroll") continue;
            int index = ordinal++;
            if (!states.TryGetValue(index, out var state)) states[index] = state = new SourceGridState();
            RetainGridScroll(scroll, state, false);
        }
    }

    private static IEnumerable<ScrollContainer> ModalScrollContainers(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is ScrollContainer scroll) yield return scroll;
            foreach (var descendant in ModalScrollContainers(child)) yield return descendant;
        }
    }
    private void BuildSearchableKeywords(VBoxContainer box, Func<string> read, Action<string> write)
    {
        var searchRow = Row(box);
        var search = new LineEdit { Text = read(), PlaceholderText = "用語・説明を検索", SizeFlagsHorizontal = SizeFlags.ExpandFill, ClearButtonEnabled = true };
        search.Name = "KeywordSearch";
        searchRow.AddChild(search);
        StylePaperTextInput(search);
        var clear = Button(searchRow, "クリア", () => { });
        var count = Text(box, "", 17, Muted);
        var list = ScrollColumn(box);
        var entries = new List<(Control Row, string Haystack)>();
        foreach (var term in KeywordHelp.Terms)
        {
            var row = Column(list);
            Text(row, term.Key, 21, Gold);
            Text(row, term.Value, 18);
            entries.Add((row, term.Key + "\n" + Plain(term.Value)));
        }
        var empty = Text(list, "一致する用語がありません。別の言葉で検索するか、クリアしてください。", 19, Muted);
        void Filter(string value)
        {
            write(value);
            string needle = value.Trim(); int shown = 0;
            foreach (var entry in entries)
            {
                entry.Row.Visible = needle.Length == 0 || entry.Haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
                if (entry.Row.Visible) shown++;
            }
            count.Text = needle.Length == 0 ? $"{shown} 件の用語" : $"{shown} / {entries.Count} 件";
            clear.Disabled = value.Length == 0;
            empty.Visible = shown == 0;
        }
        clear.Pressed += () => { search.Text = ""; Filter(""); ((ScrollContainer)list.GetParent()).ScrollVertical = 0; search.GrabFocus(); };
        search.TextChanged += value => { Filter(value); ((ScrollContainer)list.GetParent()).ScrollVertical = 0; };
        Filter(read());
    }

    private sealed class GridFilterState { public string Query = ""; public int OriginalY; public int Change; }
    private readonly ConditionalWeakTable<SourceGridState, GridFilterState> _gridFilterStates = new();
    private void InstallCardSearch(Node parent, ScrollContainer scroll, SourceGridState grid, List<(Control Row, string Text)> entries)
    {
        var state = _gridFilterStates.GetOrCreateValue(grid);
        var bar = Column(parent); bar.Name = "CardSearchBar";
        parent.MoveChild(bar, scroll.GetIndex());
        var row = Row(bar);
        var search = new LineEdit { Name = "CardInventorySearch", Text = state.Query, PlaceholderText = "札の名前・効果を検索", SizeFlagsHorizontal = SizeFlags.ExpandFill, ClearButtonEnabled = true };
        row.AddChild(search);
        StylePaperTextInput(search);
        var clear = Button(row, "クリア", () => { });
        var count = Text(bar, "", 17, Muted);
        var empty = Text(bar, "一致する札がありません。クリアすると全ての札を表示します。", 18, Muted);
        void Filter(string query, bool changed)
        {
            bool wasSearching = state.Query.Length > 0;
            if (changed && !wasSearching && query.Length > 0) state.OriginalY = scroll.ScrollVertical;
            state.Query = query; int shown = 0;
            foreach (var entry in entries) { entry.Row.Visible = query.Trim().Length == 0 || entry.Text.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0; if (entry.Row.Visible) shown++; }
            count.Text = $"{shown} / {entries.Count} 枚"; empty.Visible = shown == 0; clear.Disabled = query.Length == 0;
            if (changed) RestoreFilteredCardScroll(scroll, state, query.Length == 0 && wasSearching ? state.OriginalY : 0, ++state.Change);
        }
        search.TextChanged += value => Filter(value, true);
        clear.Pressed += () => { search.Text = ""; Filter("", true); search.GrabFocus(); };
        Filter(state.Query, false);
    }
    private async void RestoreFilteredCardScroll(ScrollContainer scroll, GridFilterState state, int y, int change)
    {
        bool input = false;
        void Cancel(InputEvent e) { if (e is InputEventMouseButton or InputEventScreenTouch or InputEventScreenDrag or InputEventPanGesture) input = true; }
        scroll.GuiInput += Cancel;
        for (int i = 0; i < 4; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!GodotObject.IsInstanceValid(scroll) || !scroll.IsInsideTree()) return;
        }
        scroll.GuiInput -= Cancel;
        if (!input && state.Change == change) scroll.ScrollVertical = y;
    }

    private void OpenSearchableJournal()
    {
        string report = _session.ExportText(); string query = ""; int selected = 0;
        Open("ランの記録", box =>
        {
            var row = Row(box);
            var search = new LineEdit { Name = "JournalSearch", Text = query, PlaceholderText = "記録内を検索", SizeFlagsHorizontal = SizeFlags.ExpandFill, ClearButtonEnabled = true };
            row.AddChild(search);
        StylePaperTextInput(search);
            var previous = Button(row, "前へ", () => { });
            var next = Button(row, "次へ", () => { });
            var clear = Button(row, "クリア", () => { });
            var count = Text(box, "", 17, Muted);
            var text = new TextEdit { Name = "JournalText", Text = report, Editable = false, SizeFlagsVertical = SizeFlags.ExpandFill, WrapMode = TextEdit.LineWrappingMode.Boundary };
            box.AddChild(text);
            StylePaperTextInput(text);
            var matches = new List<(int Start, int End)>();
            (int Line, int Column) Position(int offset)
            {
                int line = 0, column = 0;
                for (int i = 0; i < offset; i++) { if (report[i] == '\n') { line++; column = 0; } else column++; }
                return (line, column);
            }
            void ShowMatch()
            {
                previous.Disabled = next.Disabled = matches.Count == 0;
                clear.Disabled = query.Length == 0;
                if (query.Length == 0) { count.Text = "ランの記録を全文表示しています"; text.Deselect(); return; }
                if (matches.Count == 0) { count.Text = "一致する箇所がありません。別の言葉で検索してください。"; text.Deselect(); return; }
                selected = Math.Clamp(selected, 0, matches.Count - 1);
                var from = Position(matches[selected].Start); var to = Position(matches[selected].End);
                text.SetCaretLine(from.Line); text.SetCaretColumn(from.Column); text.Select(from.Line, from.Column, to.Line, to.Column);
                count.Text = $"{selected + 1} / {matches.Count} 箇所";
            }
            void Filter(string value)
            {
                query = value; matches.Clear(); int offset = 0;
                while (value.Length > 0 && offset <= report.Length - value.Length)
                {
                    int index = report.IndexOf(value, offset, StringComparison.OrdinalIgnoreCase);
                    if (index < 0) break;
                    matches.Add((index, index + value.Length)); offset = index + value.Length;
                }
                ShowMatch();
            }
            search.TextChanged += value => { selected = 0; Filter(value); };
            previous.Pressed += () => { if (matches.Count > 0) { selected = (selected + matches.Count - 1) % matches.Count; ShowMatch(); } };
            next.Pressed += () => { if (matches.Count > 0) { selected = (selected + 1) % matches.Count; ShowMatch(); } };
            clear.Pressed += () => { search.Text = ""; selected = 0; Filter(""); search.GrabFocus(); };
            Filter(query);
            Button(box, "レポートを書き出す", OpenExport);
        });
    }

}
