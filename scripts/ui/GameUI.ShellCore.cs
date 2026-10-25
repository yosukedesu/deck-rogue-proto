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

	private void Heading(string title, string subtitle = "")
	{
        Node titleParent = _body;
        if (PhoneLayout && subtitle.Length > 0)
        {
            var titleRow = Row(_body);
            titleRow.Name = "SourceScreenHeadingRow";
            titleParent = titleRow;
            var help = Button(titleRow, "説明", () => Open(title, box => Text(ScrollColumn(box), subtitle, 21)));
            help.Name = "SourceScreenDescription";
            help.TooltipText = "この画面の説明を読む";
            help.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SourceRunUiDecor.Button(help, RunUnit);
        }
		Label heading = Text(titleParent, title, PhoneLayout ? 20 : 30, Gold);
        if (titleParent != _body) titleParent.MoveChild(heading, 0);
		heading.TooltipText = title + "\n" + subtitle;
        heading.AddThemeFontOverride("font", SourceRunUiDecor.Display);
		if (subtitle.Length > 0)
		{
			var sub = Text(_body, subtitle, PhoneLayout ? 15 : 18, Muted);
			if (PhoneLayout) { sub.AutowrapMode = TextServer.AutowrapMode.Off; sub.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; sub.TooltipText = subtitle; }
		}
	}


    // Modal-local styling. Card artwork/faces retain their authored materials.
    private static readonly Color ModalSurface = new Color("141e30");
    private static readonly Color ModalRaised = new Color("23334b");
    private static readonly Color ModalText = new Color("edf3ff");
    private static readonly Color ModalMuted = new Color("b7c6dd");
    private static readonly Color ModalAccent = new Color("70d7d0");
    private const string ModernModalMarker = "source_modern_modal";

    private static bool OnModernModal(Node node)
    {
        for (Node current = node; current != null; current = current.GetParent())
            if (current.HasMeta(ModernModalMarker)) return true;
        return false;
    }
    private static Color ModalInk(Color color)
    {
        if (color == new Color("ffacb8") || color == SourcePalette.BadInk || color == Danger) return new Color("ffacb8");
        if (color == ModalAccent || color == SourcePalette.ManaInk || color == Accent) return ModalAccent;
        if (color == new Color("f5d698") || color == SourcePalette.BrassInk || color == Gold) return new Color("f5d698");
        if (color == ModalMuted || color == SourcePalette.InkSoft || color == Muted) return ModalMuted;
        return ModalText;
    }
    private StyleBoxFlat ModalBox(Color fill, float padding = 12, Color? border = null)
    {
        int radius = Math.Max(4, (int)(12 * RunUnit));
        return new StyleBoxFlat {
            BgColor = fill, BorderColor = border ?? new Color("3b4e69"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
            ContentMarginLeft = padding * RunUnit, ContentMarginRight = padding * RunUnit,
            ContentMarginTop = padding * RunUnit, ContentMarginBottom = padding * RunUnit
        };
    }
    private Theme ModernModalTheme()
    {
        var theme = new Theme { DefaultFont = SourceRunUiDecor.Body, DefaultFontSize = FontSize(20) };
        SourceRunUiDecor.ApplyFonts(theme);
        foreach (string type in new[] { "LineEdit", "TextEdit" }) {
            theme.SetFont("font", type, SourceRunUiDecor.Body);
            theme.SetStylebox("normal", type, ModalBox(new Color("0c1524")));
            theme.SetStylebox("read_only", type, ModalBox(new Color("101a2b")));
            theme.SetStylebox("focus", type, ModalBox(Colors.Transparent, 0, ModalAccent));
            foreach (string key in new[] { "font_color", "font_selected_color", "caret_color", type == "TextEdit" ? "font_readonly_color" : "font_uneditable_color" })
                theme.SetColor(key, type, ModalText);
            theme.SetColor("font_placeholder_color", type, ModalMuted);
            theme.SetColor("selection_color", type, new Color("365d79"));
            theme.SetColor("font_outline_color", type, Colors.Transparent);
            theme.SetConstant("caret_width", type, 2);
            if (type == "LineEdit") {
                theme.SetColor("clear_button_color", type, ModalMuted);
                theme.SetColor("clear_button_color_pressed", type, ModalAccent);
            } else {
                theme.SetColor("background_color", type, Colors.Transparent);
                theme.SetColor("caret_background_color", type, ModalSurface);
                theme.SetColor("current_line_color", type, Colors.Transparent);
                theme.SetColor("word_highlighted_color", type, new Color("365d79"));
                theme.SetColor("search_result_color", type, new Color("665236"));
                theme.SetColor("search_result_border_color", type, new Color("f5d698"));
            }
        }
        foreach (string direction in new[] { "up", "down" }) {
            theme.SetColor(direction + "_icon_modulate", "SpinBox", ModalText);
            theme.SetColor(direction + "_hover_icon_modulate", "SpinBox", ModalAccent);
            theme.SetColor(direction + "_pressed_icon_modulate", "SpinBox", ModalAccent);
            theme.SetColor(direction + "_disabled_icon_modulate", "SpinBox", new Color("71829c"));
        }
        foreach (string type in new[] { "HScrollBar", "VScrollBar" }) {
            theme.SetStylebox("scroll", type, ModalBox(new Color("0d1726"), 3));
            theme.SetStylebox("grabber", type, ModalBox(new Color("536b86"), 5));
            theme.SetStylebox("grabber_highlight", type, ModalBox(new Color("77a6bd"), 5));
            theme.SetStylebox("grabber_pressed", type, ModalBox(ModalAccent, 5));
        }
        return theme;
    }
    private static Texture2D _modalKnob;
    private static Texture2D ModalKnob()
    {
        if (_modalKnob != null) return _modalKnob;
        using var image = Image.CreateEmpty(30, 30, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        for (int y = 0; y < 30; y++) for (int x = 0; x < 30; x++) {
            float d = new Vector2(x-14.5f, y-14.5f).Length();
            if (d <= 14) image.SetPixel(x, y, d > 11 ? new Color("b6fff0") : new Color("65d9cb"));
        }
        return _modalKnob = ImageTexture.CreateFromImage(image);
    }
    private void StyleModernModal(Node root, Theme theme)
    {
        if (root is CardTile || root.Name == "SourceCardFace") return;
        if (root is Label label) {
            label.AddThemeColorOverride("font_color", ModalInk(label.GetThemeColor("font_color")));
            if (!label.HasThemeFontOverride("font")) label.AddThemeFontOverride("font", SourceRunUiDecor.Body);
        }
        if (root is RichTextLabel rich) {
            rich.AddThemeColorOverride("default_color", ModalText);
            rich.AddThemeFontOverride("normal_font", SourceRunUiDecor.Body);
        }
        if (root is PanelContainer panel) {
            panel.Theme = theme;
            var prior = panel.GetThemeStylebox("panel");
            var style = ModalBox(panel.HasMeta(ModernModalMarker) ? ModalSurface : new Color("1a283c"), 0);
            if (prior != null) {
                style.ContentMarginLeft = prior.ContentMarginLeft; style.ContentMarginRight = prior.ContentMarginRight;
                style.ContentMarginTop = prior.ContentMarginTop; style.ContentMarginBottom = prior.ContentMarginBottom;
            }
            panel.AddThemeStyleboxOverride("panel", style);
        }
        if (root is Button button) {
            bool primary = (button.HasMeta("source_primary_action") && button.GetMeta("source_primary_action").AsBool()) || (button.ToggleMode && button.ButtonPressed);
            bool danger = button.HasMeta("source_danger_action") || button.GetThemeColor("font_color") == SourcePalette.BadInk;
            var normal = danger ? new Color("432a3a") : primary ? new Color("65d9cb") : ModalRaised;
            var foreground = primary && !danger ? new Color("0b272d") : danger ? new Color("ffb5c0") : ModalText;
            var edge = danger ? new Color("a3566b") : primary ? ModalAccent : new Color("3b4e69");
            button.AddThemeFontOverride("font", SourceRunUiDecor.Action);
            button.AddThemeStyleboxOverride("normal", ModalBox(normal, 12, edge));
            button.AddThemeStyleboxOverride("hover", ModalBox(danger ? new Color("5e3144") : primary ? new Color("94ece1") : new Color("344d6c"), 12, ModalAccent));
            button.AddThemeStyleboxOverride("pressed", ModalBox(danger ? new Color("38202e") : primary ? new Color("4cb9af") : new Color("28666d"), 12, ModalAccent));
            button.AddThemeStyleboxOverride("hover_pressed", ModalBox(danger ? new Color("38202e") : primary ? new Color("4cb9af") : new Color("28666d"), 12, ModalAccent));
            button.AddThemeStyleboxOverride("disabled", ModalBox(new Color("1b2739")));
            var focus = ModalBox(Colors.Transparent, 0, new Color("fff0c7"));
            focus.BorderWidthLeft = focus.BorderWidthRight = focus.BorderWidthTop = focus.BorderWidthBottom = 2;
            focus.ExpandMarginLeft = focus.ExpandMarginRight = focus.ExpandMarginTop = focus.ExpandMarginBottom = 2;
            button.AddThemeStyleboxOverride("focus", focus);
            foreach (string key in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" })
                button.AddThemeColorOverride(key, foreground);
            button.AddThemeColorOverride("font_disabled_color", new Color("8191a9"));
            if (button.ToggleMode && !button.HasMeta("modern_toggle_hook")) {
                button.SetMeta("modern_toggle_hook", true);
                button.Toggled += _ => StyleModernModal(button, theme);
            }
        }
        if (root is Slider slider) {
            StyleBoxFlat Track(Color color) => new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4 };
            slider.AddThemeStyleboxOverride("slider", Track(new Color("36465e")));
            slider.AddThemeStyleboxOverride("grabber_area", Track(new Color("49afa9")));
            slider.AddThemeStyleboxOverride("grabber_area_highlight", Track(ModalAccent));
            foreach (string key in new[] { "grabber", "grabber_highlight", "grabber_disabled" }) slider.AddThemeIconOverride(key, ModalKnob());
        }
        if (root is LineEdit || root is TextEdit || root is SpinBox) ((Control)root).Theme = theme;
        foreach (var child in root.GetChildren()) StyleModernModal(child, theme);
    }

	private void BuildOverlay()
	{
        bool reactionSheet = !PhoneLayout && !_overlayCanClose && _overlayTitle.StartsWith("からくりの確認 · ");
		ColorRect scrim = new ColorRect
		{
			Color = new Color(0.008f, 0.014f, 0.028f, reactionSheet ? 0.25f : 0.74f),
			MouseFilter = MouseFilterEnum.Stop
		};
		AddChild(scrim, forceReadableName: false, InternalMode.Disabled);
		scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		_overlayNode = scrim;
		scrim.ZIndex = 100;
		MarginContainer margin = new MarginContainer();
		scrim.AddChild(margin, forceReadableName: false, InternalMode.Disabled);
		PlaceRunModal(margin);
		if (reactionSheet)
		{
			float u = BattleUnit;
			Control hero = _uxBattle?.GetNodeOrNull<Control>("PlayerFootLedger");
			float bottom = (hero != null ? hero.Position.Y : Size.Y - 405f * u) - 12f * u;
			float right = Math.Min(Size.X - 24f * u, 904f * u);
			int activeEnemy = _view?.Combat?.PendingWindow?.EnemyIndex ?? -1;
			foreach (var enemy in _uxEnemies)
			{
				if (enemy.Index == activeEnemy && GodotObject.IsInstanceValid(enemy.Intent))
					right = Math.Min(right, enemy.Intent.GetGlobalRect().Position.X - 12f * u);
			}
			margin.AnchorLeft = margin.AnchorRight = margin.AnchorTop = margin.AnchorBottom = 0;
			margin.OffsetLeft = 24f * u;
			margin.OffsetRight = right;
			margin.OffsetTop = 180f * u;
			margin.OffsetBottom = bottom;
		}
		PanelContainer pan = new PanelContainer
		{
			MouseFilter = MouseFilterEnum.Stop
		};
		pan.SetMeta(RunPaperMarker, true);
        pan.SetMeta(ModernModalMarker, true);
		pan.Theme = SourceRunUiDecor.PaperInputs(RunUnit, FontSize(20));
		pan.AddThemeStyleboxOverride("panel", SourceRunUiDecor.Paper(SourcePalette.Paper, RunUnit, 26));
		margin.AddChild(pan, forceReadableName: false, InternalMode.Disabled);
		bool pressedOutside = false;
        int outsidePointer = int.MinValue;
		Vector2 outsidePress = Vector2.Zero;
		scrim.GuiInput += (InputEvent evt) =>
		{
			if (_overlayCanClose && _overlayNode == scrim && !IsInputBlocked)
			{
				Vector2 position;
				bool pressed;
                int pointer;
				if (evt is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
				{
					position = inputEventMouseButton.Position;
					pressed = inputEventMouseButton.Pressed;
                    pointer = -1;
				}
				else
				{
					if (!(evt is InputEventScreenTouch inputEventScreenTouch))
					{
						return;
					}
					position = inputEventScreenTouch.Position;
					pressed = inputEventScreenTouch.Pressed;
                    pointer = inputEventScreenTouch.Index;
				}
				Vector2 point = pan.GetGlobalTransform().AffineInverse() * (scrim.GetGlobalTransform() * position);
				bool flag = !new Rect2(Vector2.Zero, pan.Size).HasPoint(point);
				if (pressed)
				{
                    if (outsidePointer != int.MinValue) return;
                    outsidePointer = pointer;
					pressedOutside = flag;
					outsidePress = position;
				}
				else
				{
                    if (outsidePointer != pointer) return;
                    outsidePointer = int.MinValue;
					bool num = (pressedOutside & flag) && position.DistanceTo(outsidePress) <= 14f;
					pressedOutside = false;
					if (num)
					{
						scrim.AcceptEvent();
						CloseOverlay();
					}
				}
			}
		};
		VBoxContainer col = new VBoxContainer();
		col.AddThemeConstantOverride("separation", Math.Max(12, (int)(18 * RunUnit)));
		pan.AddChild(col, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer top = Row(col);
		var titleLabel = Text(top, _overlayTitle, PhoneLayout ? 28 : 30, SourcePalette.Ink);
		titleLabel.AddThemeFontOverride("font", SourceRunUiDecor.Display);
		titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		if (_overlayCanClose)
		{
            var back = Button(top, _overlayStack.Count > 0 ? "戻る" : "閉じる", CloseOverlay);
            back.Name = "SourceOverlayBack";
            back.TooltipText = _overlayStack.Count > 0 ? "前の画面へ戻る" : "ゲーム画面へ戻る";
		}
        var divider = new HSeparator { MouseFilter = MouseFilterEnum.Ignore };
        divider.AddThemeStyleboxOverride("separator", new StyleBoxLine { Color = new Color("3b536e"), Thickness = 1 });
        col.AddChild(divider);
        if (_notice.Length > 0 && _overlayTitle != "お知らせ") ShowBoundedNotice(col);
		var overlayContent = _overlayTitle == "メニュー" ? ScrollColumn(col) : col;
        _overlay?.Invoke(overlayContent);
        StyleModernModal(pan, ModernModalTheme());
        titleLabel.AddThemeFontOverride("font", SourceRunUiDecor.Display);
        if (_overlayTitle == "メニュー") {
            void FitMenu() {
                if (!GodotObject.IsInstanceValid(margin) || !GodotObject.IsInstanceValid(pan)) return;
                float safe = RunSafeRect.Size.Y;
                float contentHeight = pan.GetCombinedMinimumSize().Y + overlayContent.GetCombinedMinimumSize().Y + 12;
                margin.Size = new Vector2(margin.Size.X, Math.Clamp(contentHeight, Math.Min(240 * RunUnit, safe), safe));
            }
            Callable.From(() => { FitMenu(); Callable.From(FitMenu).CallDeferred(); }).CallDeferred();
        }
        // A short decision should not look like a mostly empty full-screen form.
        // Lists, editors and card detail retain their constrained scroll viewport.
        bool NeedsViewport(Node node) {
            if (node is ScrollContainer || node is TextEdit || node is Slider || node.Name == "SourceCardFace") return true;
            foreach (var child in node.GetChildren()) if (NeedsViewport(child)) return true;
            return false;
        }
        bool cardDetail = col.HasMeta("modal_kind") && col.GetMeta("modal_kind").AsString() == "card-detail";
        if (!reactionSheet && _overlayTitle != "メニュー" && (cardDetail || !NeedsViewport(col))) {
            float w = cardDetail ? margin.Size.X : Math.Min(RunSafeRect.Size.X, (PhoneLayout ? 860 : 760) * RunUnit);
            margin.Size = new Vector2(Math.Min(RunSafeRect.Size.X, Math.Max(360, w)), margin.Size.Y);
            void FitShort() {
                if (!GodotObject.IsInstanceValid(margin) || !GodotObject.IsInstanceValid(pan)) return;
                float safe = RunSafeRect.Size.Y;
                float h = Math.Clamp(pan.GetCombinedMinimumSize().Y, Math.Min(220 * RunUnit, safe), safe);
                margin.Size = new Vector2(margin.Size.X, h);
                margin.Position = RunSafeRect.Position + (RunSafeRect.Size - new Vector2(margin.Size.X, h)) / 2;
            }
            Callable.From(() => { FitShort(); Callable.From(FitShort).CallDeferred(); }).CallDeferred();
        }

	}


    private void ShowBoundedNotice(Node parent)
    {
        string detail = _notice;
        bool longNotice = detail.Length > 100 || detail.Count(c => c == '\n') > 1;
        Node line = longNotice ? Row(parent) : parent;
        var label = Text(line, detail, 17, _error ? Danger : Accent);
        label.MaxLinesVisible = 2;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        if (longNotice)
        {
            var details = Button(line, "詳細", () => Open("お知らせ", box => Text(ScrollColumn(box), detail, 20)));
            details.Name = "SourceNoticeDetails";
        }
    }

    private Label SectionHeading(Node parent, string text, int size = 23, Color? color = null)
    {
        var label = Text(parent, text, size, color);
        label.AddThemeFontOverride("font", SourceRunUiDecor.Display);
        return label;
    }

	private Label Text(Node parent, string text, int size = 19, Color? color = null)
	{
		Label label = new Label
		{
			Text = Plain(text),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		label.AddThemeFontSizeOverride("font_size", FontSize(size));
		ApplyRunText(label, parent, color ?? Ink);
		parent.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		return label;
	}


	private Button Button(Node parent, string text, Action action, bool enabled = true, Color? color = null)
	{
		Button button = new Button
		{
			Text = text,
			Disabled = (!enabled || IsInputBlocked),
			CustomMinimumSize = new Vector2(MinimumHit, MinimumHit),
			FocusMode = FocusModeEnum.All
		};
		if (color.HasValue)
		{
			button.AddThemeColorOverride("font_color", color.Value);
            if (color.Value == Danger || color.Value == SourcePalette.BadInk) button.SetMeta("source_danger_action", true);
		}
		button.Pressed += () =>
		{
			if (!IsInputBlocked && !button.Disabled && button.IsVisibleInTree())
			{
				Sound("ui.click");
				action();
			}
		};
		parent.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		if (OnRunPaper(parent)) SourceRunUiDecor.Button(button, RunUnit);
        if (OnModernModal(parent)) StyleModernModal(button, ModernModalTheme());
		return button;
	}

}
