using System;

using System.Collections.Generic;

using System.ComponentModel;

using System.Globalization;

using System.Linq;

using System.Runtime.CompilerServices;

using System.Text;

using System.Text.RegularExpressions;

using DeckRogue.Engine;

using DeckRogue.Engine.Generated;

using DeckRogue.GodotPort.Core;

using DeckRogue.GodotPort.Presentation;

using DeckRogue.Platform;

using Godot;

using Godot.Bridge;

using Godot.Collections;

using Godot.NativeInterop;

using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{

	private void SourceCardGrid(Node parent, IReadOnlyList<CardInstance> cards, Action<CardInstance> onPick, Func<CardInstance, bool> enabled, Func<CardInstance, string> badge, bool confirmPick, string actionLabel, Func<CardInstance, string> lockReason)
	{
		bool inspectOnly = onPick == null || onPick == new Action<CardInstance>(InspectCard);
		bool phoneConfirm = (PhoneLayout & confirmPick) && !inspectOnly;
		bool mainGrid = parent == _body || (_body != null && _body.IsAncestorOf(parent));
        // Selection dialogs need a full first row above their fixed actions.
        // Recover vertical space from modal gaps, never from effect font size.
        if (PhoneLayout && !mainGrid && parent is VBoxContainer modalColumn)
            modalColumn.AddThemeConstantOverride("separation", Math.Max(8, (int)(12 * RunUnit)));
		object CurrentGridOwner() => mainGrid ? ((object)_view ?? this) : (_overlay ?? ((object)_view) ?? this);
		object owner = CurrentGridOwner();
		string key = string.Join("\u001f", cards.Select((CardInstance c) => c.Uid));
		System.Collections.Generic.Dictionary<string, SourceGridState> owners = _sourceGridStates.GetOrCreateValue(owner);
		if (!owners.TryGetValue(key, out var selection))
		{
			owners[key] = (selection = new SourceGridState());
		}
		if (!cards.Any((CardInstance c) => c.Uid == selection.SelectedUid))
		{
			selection.SelectedUid = null;
		}
		long revision = _session?.Revision ?? 0;
		VBoxContainer scroll = ScrollColumn(parent);
		RetainGridScroll((ScrollContainer)scroll.GetParent(), selection);
		MarginContainer margin = new MarginContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		margin.AddThemeConstantOverride("margin_left", 6);
		margin.AddThemeConstantOverride("margin_top", 6);
		margin.AddThemeConstantOverride("margin_right", 6);
		margin.AddThemeConstantOverride("margin_bottom", 28);
		scroll.AddChild(margin, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer flow = Flow(margin);
		if (cards.Count == 0)
		{
			string emptyTitle = mainGrid ? "表示する札はありません" : _overlayTitle.Contains("捨") ? "捨て札はありません" : _overlayTitle.Contains("除外") || _overlayTitle.Contains("廃棄") ? "除外された札はありません" : _overlayTitle.Contains("山札") ? "山札に札はありません" : "表示する札はありません";
            Text(scroll, emptyTitle, 23, Gold);
            Text(scroll, "札があるときは、ここで内容を確認できます。", 18, Muted);
		}
        var fittedCards = new System.Collections.Generic.List<(VBoxContainer Cell, CardTile Tile, float BaseScale)>();
        var searchEntries = new System.Collections.Generic.List<(Control Row, string Text)>();
		foreach (CardInstance card in cards)
		{
			VBoxContainer cell = Column(flow);
            searchEntries.Add((cell, card.Def.Name + "\n" + Plain(SourceCardFace.BodyText(card, SourceInspectState))));
			cell.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			// Run-grid cards use source units, independently of combat-hand scale.
			// A fixed .86 collapsed to tiny physical cards on the phone canvas.
			float scale = PhoneLayout ? 0.90f * Sizing.SourceScale : 0.72f;
			cell.CustomMinimumSize = new Vector2(SourceCardFace.W * scale + 16f, 0f);
			string note = badge?.Invoke(card) ?? "";
			string locked = lockReason?.Invoke(card);
			string corner = SourceCardModalPolicy.CornerMark(note);
			Func<CardInstance, bool> func = enabled;
			bool allowed = (func == null || func(card)) && string.IsNullOrEmpty(locked);
			int num;
			switch (corner)
			{
			default:
				num = ((phoneConfirm && selection.SelectedUid == card.Uid) ? 1 : 0);
				break;
			case "A":
			case "B":
			case "✓":
				num = 1;
				break;
			}
			bool marked = (byte)num != 0;
			CardInstance shown = SourceCardModalPolicy.Preview(card, _showGridUpgraded);
			Action tap;
			if (inspectOnly || !PhoneLayout)
			{
				tap = () =>
				{
					InspectCard(card);
				};
			}
			else
			{
				tap = (phoneConfirm ? ((Action)(() =>
				{
					if (OwnerCurrent())
					{
						selection.SelectedUid = card.Uid;
						Rebuild();
					}
				})) : ((Action)(() =>
				{
					Pick(card);
				})));
			}
			CardInstance original = card;
			bool enabled2 = allowed | inspectOnly;
			GameState sourceInspectState = SourceInspectState;
			bool selected = marked;
			string corner2 = corner;
			var tile = SourceCard(cell, original, shown, tap, enabled2, null, scale, sourceInspectState, drag: false, null, selected, corner2);
            fittedCards.Add((cell, tile, scale));
            // Status/expiry prose belongs below the card, not on its fixed-width
            // footer. Keeping it separate also preserves the full effect region.
            tile.SetMeta("source_card_badge", note);
            if (!string.IsNullOrWhiteSpace(note)) {
                bool reserveSelectionStatus = !inspectOnly && (note == "選ぶ" || note == "選択中");
                var captionColor = OnRunPaper(cell) && !OnModernModal(cell) ? SourcePalette.InkSoft : ModalMuted;
                var caption = Text(cell, reserveSelectionStatus && note == "選ぶ" ? "" : note, 16, captionColor);
                caption.Name = "SourceGridCardStatus";
                float reservedHeight = reserveSelectionStatus ? caption.GetThemeFont("font").GetHeight(caption.GetThemeFontSize("font_size")) + 4 : 0;
                caption.CustomMinimumSize = new Vector2(SourceCardFace.W * scale, reservedHeight);
                if (reserveSelectionStatus) caption.SetMeta("grid_reserved_caption", "選択中");
                caption.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            }
			if (!allowed && !inspectOnly && !string.IsNullOrEmpty(locked))
			{
				Text(cell, locked, 15, Danger).CustomMinimumSize = new Vector2(0f, 38f);
			}
			if (!inspectOnly && !PhoneLayout)
			{
				Button(cell, string.IsNullOrEmpty(note) ? actionLabel : note, () =>
				{
					Pick(card);
				}, allowed, marked ? new Color?(Gold) : ((Color?)null));
			}
		}
        // Fit one complete card row to the actual viewport after headers and
        // fixed actions have reserved their space. Keep face typography intact.
        var gridViewport = (ScrollContainer)scroll.GetParent();
        bool fittingRow = false;
        bool fittedRow = false;
        void FitFirstRow() {
            if (fittedRow || fittingRow || !GodotObject.IsInstanceValid(gridViewport) || gridViewport.Size.Y < 80) return;
            fittingRow = true;
            float AncillaryHeight(VBoxContainer cell, CardTile tile, float scale) {
                var holder = (Control)tile.GetParent();
                var caption = cell.GetNodeOrNull<Label>("SourceGridCardStatus");
                float height = 0;
                int children = 0;
                foreach (Node child in cell.GetChildren()) {
                    if (child == holder || child is not Control control || !control.Visible) continue;
                    if (control != caption) height += control.GetCombinedMinimumSize().Y;
                    children++;
                }
                height += children * cell.GetThemeConstant("separation");
                if (caption != null) {
                    Font font = caption.GetThemeFont("font");
                    int size = caption.GetThemeFontSize("font_size");
                    var measured = font.GetMultilineStringSize(caption.HasMeta("grid_reserved_caption") ? caption.GetMeta("grid_reserved_caption").AsString() : caption.Text, HorizontalAlignment.Left, Math.Max(1, SourceCardFace.W * scale), size, -1,
                        TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Adaptive);
                    float lineHeight = Math.Max(1, font.GetHeight(size));
                    height += measured.Y + Math.Max(0, (float)Math.Ceiling(measured.Y / lineHeight) - 1) * Math.Max(0, caption.GetThemeConstant("line_spacing")) + 4;
                }
                return height;
            }
            // Keep cards in the same comparison grid at one consistent scale.
            // Re-measure every caption when that shared width becomes narrower.
            float fitted = fittedCards.Count == 0 ? 1 : fittedCards.Min(e => e.BaseScale);
            for (int pass = 0; pass < 8; pass++) {
                float next = fitted;
                foreach (var entry in fittedCards) {
                    float available = gridViewport.Size.Y - 12 - 16 - AncillaryHeight(entry.Cell, entry.Tile, fitted);
                    next = Math.Min(next, Math.Max(.25f, available / SourceCardFace.H));
                }
                if (Math.Abs(next - fitted) < .0001f) break;
                fitted = next;
            }
            foreach (var entry in fittedCards) {
                var holder = (Control)entry.Tile.GetParent();
                var caption = entry.Cell.GetNodeOrNull<Label>("SourceGridCardStatus");
                float ancillary = AncillaryHeight(entry.Cell, entry.Tile, fitted);
                entry.Tile.SetMeta("grid_fit_viewport_height", gridViewport.Size.Y);
                entry.Tile.SetMeta("grid_fit_ancillary_height", ancillary);
                entry.Tile.SetMeta("grid_fitted_scale", fitted);
                entry.Tile.SetMeta("grid_base_scale", entry.BaseScale);
                entry.Tile.Scale = Vector2.One * fitted;
                holder.CustomMinimumSize = new Vector2(SourceCardFace.W * fitted + 16, SourceCardFace.H * fitted + 16);
                holder.Size = holder.CustomMinimumSize;
                entry.Cell.CustomMinimumSize = new Vector2(holder.CustomMinimumSize.X, 0);
                if (caption != null) caption.CustomMinimumSize = new Vector2(SourceCardFace.W * fitted, caption.CustomMinimumSize.Y);
                var ring = holder.GetNodeOrNull<Panel>("SelectedCardBorder");
                if (ring != null) ring.Size = holder.CustomMinimumSize - new Vector2(6, 6);
                var mark = holder.GetNodeOrNull<PanelContainer>("CardCornerMark");
                if (mark != null) mark.Position = new Vector2(holder.CustomMinimumSize.X - 38, 0);
            }
            fittedRow = true;
            fittingRow = false;
        }
        async System.Threading.Tasks.Task FitAfterInitialLayout() {
            // Footer rows can reserve height a frame after the scroll appears.
            // Wait only during construction; never reflow after user interaction.
            for (int frame = 0; frame < 4; frame++) {
                if (!GodotObject.IsInstanceValid(gridViewport) || !gridViewport.IsInsideTree()) return;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            FitFirstRow();
        }
        Callable.From(() => { _ = FitAfterInitialLayout(); }).CallDeferred();
        if (inspectOnly && !mainGrid && cards.Count > 6) InstallCardSearch(parent, (ScrollContainer)scroll.GetParent(), selection, searchEntries);
		HFlowContainer controls = Flow(parent);
		RunButton(controls, (_showGridUpgraded ? "✓ " : "□ ") + "鍛えた後を見る", () =>
		{
			if (OwnerCurrent())
			{
				_showGridUpgraded = !_showGridUpgraded;
				Rebuild();
			}
		}, enabled: true, primary: _showGridUpgraded);
		if (!phoneConfirm)
		{
			return;
		}
		CardInstance picked = cards.FirstOrDefault((CardInstance c) => c.Uid == selection.SelectedUid);
		int num2;
		if (picked != null)
		{
			Func<CardInstance, bool> func2 = enabled;
			if (func2 == null || func2(picked))
			{
				num2 = (string.IsNullOrEmpty(lockReason?.Invoke(picked)) ? 1 : 0);
				goto IL_04fc;
			}
		}
		num2 = 0;
		goto IL_04fc;
		IL_04fc:
		bool allowed2 = (byte)num2 != 0;
		if (picked != null)
        {
            var selectedName = Text(parent, "選択中: " + picked.Def.Name, 18, Gold);
            selectedName.Name = "SourceGridSelectedName";
            parent.MoveChild(selectedName, controls.GetIndex());
        }
        Button(controls, (picked == null) ? "札を選んでください" : actionLabel, () =>
		{
			if (picked != null)
			{
				Pick(picked);
			}
		}, allowed2, Gold).Name = "SourceGridConfirm";
		bool OwnerCurrent()
		{
			if (owner == CurrentGridOwner())
			{
				return (_session?.Revision ?? 0) == revision;
			}
			return false;
		}
		void Pick(CardInstance c)
		{
			if (OwnerCurrent() && !IsInputBlocked)
			{
				Func<CardInstance, bool> func3 = enabled;
				if ((func3 == null || func3(c)) && string.IsNullOrEmpty(lockReason?.Invoke(c)))
				{
					onPick?.Invoke(c);
				}
			}
		}
	}



    private bool _gridKeyboardNavigation;

    private void ObserveGridNavigationInput(InputEvent input)
    {
        bool? keyboard = input switch
        {
            InputEventMouseButton mouse when mouse.Pressed => false,
            InputEventScreenTouch touch when touch.Pressed => false,
            InputEventScreenDrag => false,
            InputEventPanGesture => false,
            InputEventKey key when key.Pressed && key.Keycode is Key.Tab or Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End or Key.Pageup or Key.Pagedown or Key.Enter or Key.KpEnter or Key.Space => true,
            InputEventJoypadButton button when button.Pressed => true,
            InputEventJoypadMotion axis when Math.Abs(axis.AxisValue) > .5f => true,
            _ => null
        };
        if (keyboard == null) return;
        _gridKeyboardNavigation = keyboard.Value;
        foreach (var state in _liveGridScrollStates)
            if (GodotObject.IsInstanceValid(state.ActiveScroll))
                state.ActiveScroll.FollowFocus = _gridKeyboardNavigation;
    }

    private readonly System.Collections.Generic.List<SourceGridState> _liveGridScrollStates = new();

    private void CaptureCardGridScrollBeforeRebuild()
    {
        foreach (var state in _liveGridScrollStates)
        {
            var scroll = state.ActiveScroll;
            if (state.ScrollReady && GodotObject.IsInstanceValid(scroll))
            {
                state.ScrollHorizontal = scroll.ScrollHorizontal;
                state.ScrollVertical = scroll.ScrollVertical;
            }
            // Hiding/removing old containers can synchronously clamp their scrollbars.
            // Such layout events must not replace the captured user position.
            state.ActiveScroll = null;
            state.ScrollReady = false;
        }
        _liveGridScrollStates.Clear();
    }

    private async void RetainGridScroll(ScrollContainer scroll, SourceGridState state, bool cardGrid = true)
    {
        if (cardGrid) scroll.Name = "SourceCardGridScroll";
        state.ActiveScroll = scroll;
        state.ScrollReady = false;
        _liveGridScrollStates.Add(state);
        bool ready = false;
        bool userIntervened = false;
        void Capture()
        {
            if (!ready || state.ActiveScroll != scroll || !GodotObject.IsInstanceValid(scroll)) return;
            state.ScrollHorizontal = scroll.ScrollHorizontal;
            state.ScrollVertical = scroll.ScrollVertical;
        }
        scroll.GetHScrollBar().ValueChanged += _ => Capture();
        scroll.GetVScrollBar().ValueChanged += _ => Capture();
        void UserScrolled()
        {
            if (ready) return;
            userIntervened = true;
            ready = true;
            state.ScrollReady = true;
            Capture();
        }
        void ObserveInput(InputEvent input)
        {
            if ((input is InputEventMouseButton mouse && mouse.Pressed)
                || (input is InputEventScreenTouch touch && touch.Pressed)
                || input is InputEventScreenDrag || input is InputEventPanGesture
                || (input is InputEventMouseMotion motion && motion.ButtonMask != 0)
                || (input is InputEventKey key && key.Pressed)) UserScrolled();
        }
        scroll.GuiInput += ObserveInput;
        scroll.GetHScrollBar().GuiInput += ObserveInput;
        scroll.GetVScrollBar().GuiInput += ObserveInput;
        scroll.ScrollStarted += UserScrolled;
        // Flow wrapping, margins and scrollbar pages settle on different layout passes.
        // Do not save an early clamped position as the user's retained position.
        Vector2 previousRange = new Vector2(-1, -1);
        int stableFrames = 0;
        for (int frame = 0; frame < 8; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (userIntervened || !GodotObject.IsInstanceValid(scroll) || !scroll.IsInsideTree() || state.ActiveScroll != scroll) return;
            var horizontal = scroll.GetHScrollBar();
            var vertical = scroll.GetVScrollBar();
            var range = new Vector2((float)(horizontal.MaxValue - horizontal.Page), (float)(vertical.MaxValue - vertical.Page));
            stableFrames = range.IsEqualApprox(previousRange) ? stableFrames + 1 : 0;
            previousRange = range;
            if (frame >= 3 && stableFrames >= 2) break;
        }
        scroll.ScrollHorizontal = state.ScrollHorizontal;
        scroll.ScrollVertical = state.ScrollVertical;
        ready = true;
        state.ScrollReady = true;
        Capture();
    }


	private void CardGrid(Node parent, IReadOnlyList<CardInstance> cards, Action<CardInstance>? onPick, Func<CardInstance, bool>? enabled, Func<CardInstance, string>? badge = null, bool confirmPick = false, string actionLabel = "選ぶ", Func<CardInstance, string>? lockReason = null)
	{
		SourceCardGrid(parent, cards, onPick, enabled, badge, confirmPick, actionLabel, lockReason);
	}

}
