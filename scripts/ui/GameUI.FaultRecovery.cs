using System;
using System.Linq;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private bool _presentationFaultNotice;
    private bool _synchronizingFaultSnapshot;
    private bool _faultUiUnavailable;
    private CanvasLayer? _faultDiagnosticLayer;
    private bool _faultSaveAttempted;
    private bool _faultSaveConfirmed;
    private RunState? _faultSaveOwner;
    private long _faultSaveRevision = -1;
    private Label? _faultRestartInstruction;

    // The stage has discarded the failed presentation generation before this call.
    // InputLocked and Session.PresentationBusy both remain true
    // until this method acknowledges a complete, current UI. Never replay a command here.
    public bool SynchronizeFaultSnapshot(RunState committedRun, long expectedRevision, bool stageBlocked)
    {
        if (_synchronizingFaultSnapshot) return false;
        _synchronizingFaultSnapshot = true;
        try
        {
            if (_session == null || committedRun == null || _session.Revision != expectedRevision || !ReferenceEquals(_session.CurrentRun, committedRun))
                throw new InvalidOperationException("The presentation recovery snapshot is no longer current.");
            long revision = expectedRevision;
            if (stageBlocked)
            {
                _session.RecordFreezeEvent("GameUI.SynchronizeFaultSnapshot.stageBlocked", false);
                EnterBlockedFaultUi();
                return false;
            }
            _sparksRemaining = 0;
            _pumpingSparks = false;
            _play = null;
            _overlay = null;
            _overlayStack.Clear();
            _relicPicks.Clear();
            _scryPicks.Clear();
            _ratingQueued = false;
            _preferredTarget = -1;
            _activeChange = null;
            _costPresented = false;
            _renderedModalOwner = null;
            _sourceTokenFrame = null;
            _sourceTokenData = null;
            _sourceTokenRenderedStamp = "";
            _uxEnemyHudSnapshot = null;
            _uxEnemyHudBoardRevision = -1;
            UxResetHand();
            _view = committedRun;
            _showTitle = false;
            _notice = "";
            _error = false;
            _faultUiUnavailable = false;
            _presentationFaultNotice = true;
            Show();
            ProcessMode = ProcessModeEnum.Inherit;
            SetProcess(true);
            SetProcessInput(true);
            SetProcessUnhandledKeyInput(true);
            Rebuild();
            if (_session.Revision != revision || !ReferenceEquals(_session.CurrentRun, committedRun) || !ReferenceEquals(_view, committedRun))
                throw new InvalidOperationException("The run changed while rebuilding its recovered presentation.");
            if (GetChildren().OfType<ColorRect>().Any(c => c.Visible && c.MouseFilter == MouseFilterEnum.Stop && c.ZIndex == 99))
                throw new InvalidOperationException("A presentation input cover survived the recovered UI rebuild.");
            if (committedRun.Phase == "combat" && committedRun.Combat != null)
            {
                var expected = committedRun.Combat.Player.Hand.Select(c => c.Uid).OrderBy(x => x).ToArray();
                if (!_uxTiles.Keys.OrderBy(x => x).SequenceEqual(expected) || _uxTiles.Values.Any(t => !GodotObject.IsInstanceValid(t) || t.IsQueuedForDeletion() || t.Disabled))
                    throw new InvalidOperationException("The recovered hand does not match the committed run.");
            }
            DisposeFaultDiagnosticLayer();
            _session.RecordFreezeEvent("GameUI.SynchronizeFaultSnapshot.complete", true);
            return true;
        }
        catch (Exception error)
        {
            try { _session?.RecordFreezeEvent("GameUI.SynchronizeFaultSnapshot.exception", false, error); } catch { }
            EnterBlockedFaultUi();
            return false;
        }
        finally { _synchronizingFaultSnapshot = false; }
    }

    private void EnterBlockedFaultUi()
    {
        _faultUiUnavailable = true;
        if (!_faultSaveAttempted)
        {
            _faultSaveAttempted = true;
            try { _faultSaveOwner = _session?.CurrentRun; _faultSaveRevision = _session?.Revision ?? -1; _faultSaveConfirmed = _session?.Save().Success == true; } catch { _faultSaveConfirmed = false; }
            try { _session?.RecordFreezeEvent("GameUI.FaultSave", _faultSaveConfirmed); } catch { }
        }
        RefreshBlockedFaultSaveStatus(_session?.CurrentRun, _session?.Revision ?? -1);
        // No half-built board or ordinary action is exposed after failed recovery.
        Hide();
        ProcessMode = ProcessModeEnum.Disabled;
        SetProcess(false);
        SetProcessInput(false);
        SetProcessUnhandledKeyInput(false);
        try { ShowFaultDiagnosticPanel(); }
        catch (Exception panelError)
        {
            try { _session?.RecordFreezeEvent("GameUI.FaultDiagnosticPanel.exception", false, panelError); } catch { }
            GD.PushError("DeckRogue could not show its recovery panel. Check the diagnostic record; the current save has not been guaranteed.");
        }
    }

    private string FaultSaveInstruction => !_faultSaveConfirmed ? FaultSaveUnconfirmed : _faultSaveOwner?.Phase is "won" or "lost" ? "結果を保存しました。再起動してください。" : "再起動後、「続きから」で再開してください。";
    private const string FaultSaveUnconfirmed = "保存を確認できませんでした。アプリを閉じず、診断をコピーしてください。";
    private void RefreshBlockedFaultSaveStatus(RunState? run, long revision)
    {
        if (!ReferenceEquals(run, _faultSaveOwner) || revision != _faultSaveRevision) _faultSaveConfirmed = false;
        if (_faultRestartInstruction != null && GodotObject.IsInstanceValid(_faultRestartInstruction))
            _faultRestartInstruction.Text = FaultSaveInstruction;
    }

    private void AppendPresentationFaultNotice()
    {
        if (!_presentationFaultNotice || _faultUiUnavailable || _overlay != null || CurrentRoute != "combat") return;
        float scale = Math.Clamp(Size.Y / 675f, 0.7f, 1.5f);
        Rect2 safe = RunSafeRect;
        var panel = new PanelContainer { Name = "PresentationFaultNotice", ZIndex = 90, MouseFilter = MouseFilterEnum.Stop };
        AddChild(panel);
        panel.Position = safe.Position + new Vector2(8, 58) * scale;
        panel.Size = new Vector2(Math.Min(Math.Max(1, safe.Size.X - 16 * scale), 290 * scale), 0);
        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        panel.AddChild(box);
        var label = new Label { Text = "演出を一時停止しました", AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", (int)(16 * scale));
        box.AddChild(label);
        var button = new Button { Name = "PresentationFaultDiagnostics", Text = "動作診断を開く", CustomMinimumSize = new Vector2(0, 44 * scale) };
        button.AddThemeFontSizeOverride("font_size", (int)(17 * scale));
        box.AddChild(button);
        button.Pressed += () => { if (!IsInputBlocked) OpenFreezeDiagnostics(); };
    }

    private void ResetPresentationFaultUi()
    {
        if (_synchronizingFaultSnapshot || _faultUiUnavailable || _presentationFaultNotice) return;
        _presentationFaultNotice = false;
        _faultUiUnavailable = false;
        DisposeFaultDiagnosticLayer();
        Show();
        ProcessMode = ProcessModeEnum.Inherit;
        SetProcess(true);
        SetProcessInput(true);
        SetProcessUnhandledKeyInput(true);
    }

    private void DisposeFaultDiagnosticLayer()
    {
        if (_faultDiagnosticLayer != null && GodotObject.IsInstanceValid(_faultDiagnosticLayer))
        {
            _faultDiagnosticLayer.Hide();
            _faultDiagnosticLayer.QueueFree();
        }
        _faultDiagnosticLayer = null;
    }

    // Independent of Rebuild/Open/Button helpers, and a sibling of the hidden UI.
    // Current and previous copy buttons remain fixed while the log body scrolls.
    private void ShowFaultDiagnosticPanel()
    {
        DisposeFaultDiagnosticLayer();
        var layer = new CanvasLayer { Name = "PresentationFaultDiagnostics", Layer = 200 };
        _faultDiagnosticLayer = layer;
        GetTree().Root.AddChild(layer);
        var cover = new ColorRect { Name = "FaultDiagnosticCover", Color = new Color("101822"), MouseFilter = MouseFilterEnum.Stop };
        layer.AddChild(cover);
        cover.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var emergencyTheme = new Theme { DefaultFontSize = 14 };
        if (Theme != null && GodotObject.IsInstanceValid(Theme) && Theme.DefaultFont != null && GodotObject.IsInstanceValid(Theme.DefaultFont)) emergencyTheme.DefaultFont = Theme.DefaultFont;
        SourceRunUiDecor.ApplyFonts(emergencyTheme);
        cover.Theme = emergencyTheme;
        var margin = new MarginContainer { Name = "FaultDiagnosticSafeArea", MouseFilter = MouseFilterEnum.Stop };
        cover.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "top", "right", "bottom" })
        {
            int inset = 8;
            try { if (_safeMargin != null && GodotObject.IsInstanceValid(_safeMargin)) inset = Math.Max(inset, _safeMargin.GetThemeConstant("margin_" + edge)); } catch { }
            margin.AddThemeConstantOverride("margin_" + edge, inset);
        }
        var box = new VBoxContainer { Name = "FaultDiagnosticLayout", MouseFilter = MouseFilterEnum.Stop };
        margin.AddChild(box);
        box.AddThemeConstantOverride("separation", 4);
        var title = new Label { Text = "表示を再開できませんでした", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeFontOverride("font", SourceRunUiDecor.Display);
        box.AddChild(title);
        var instruction = new Label { Text = FaultSaveInstruction, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _faultRestartInstruction = instruction;
        instruction.AddThemeFontSizeOverride("font_size", 14);
        box.AddChild(instruction);
        string current = _freezeDiagnostics?.CurrentText ?? "", previous = _freezeDiagnostics?.PreviousText ?? "";
        var copies = new HBoxContainer { Name = "FaultDiagnosticCopyActions" };
        box.AddChild(copies);
        var feedback = new Label { Name = "FaultDiagnosticFeedback", Text = "コピー先はこの端末のクリップボードです。", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        AddCopy("Current", "今回をコピー", current);
        AddCopy("Previous", "前回をコピー", previous);
        feedback.AddThemeFontSizeOverride("font_size", 12);
        box.AddChild(feedback);
        var tabs = new TabContainer { Name = "FaultDiagnosticLogs", SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 40) };
        box.AddChild(tabs);
        AddLog("今回", current);
        AddLog("前回", previous);
        void AddCopy(string name, string text, string value)
        {
            var button = new Button { Name = "Copy" + name + "FaultLog", Text = text, CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = SizeFlags.ExpandFill, Disabled = string.IsNullOrEmpty(value) };
            button.AddThemeFontSizeOverride("font_size", 14);
            copies.AddChild(button);
            button.Pressed += () =>
            {
                try
                {
                    if (!ClipboardAvailable) { feedback.Text = "この端末ではコピーを利用できません。"; return; }
                    DisplayServer.ClipboardSet(value);
                    feedback.Text = DisplayServer.ClipboardGet() == value ? "コピーしました。" : "コピー内容を確認できませんでした。";
                }
                catch { feedback.Text = "コピーできませんでした。時間をおいて再試行してください。"; }
            };
        }
        void AddLog(string titleText, string value)
        {
            var viewer = new TextEdit { Name = titleText, Text = string.IsNullOrEmpty(value) ? "記録はありません。" : value, Editable = false, WrapMode = TextEdit.LineWrappingMode.Boundary, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
            viewer.AddThemeFontSizeOverride("font_size", 14);
            tabs.AddChild(viewer);
        }
    }
}
