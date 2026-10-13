// Reconstructed from the clean Unity ShopScreen/RunUi/SettingsUi/TitleScreen sources.
// These sources are not proven byte-identical to a7f86b5. This is not pixel-parity evidence.
using System;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private bool _shopGearsScreen;
    private Func<string> _effectiveQuality;
    private Vector2I _windowResolution = new(1280, 720);
    private string _savedSummaryKey;
    private RunState _savedSummaryRun;
    private string _savedSummaryPath;
    private bool _savedSummaryBackup;

    public void SetEffectiveQuality(Func<string> read) => _effectiveQuality = read;
    private static bool DesktopVideoAvailable => !OS.HasFeature("mobile") && DisplayServer.GetName() != "headless";
    private static bool IsFullscreenMode => DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;

    private Label RunHeaderText(Node parent, string text, int size, Color color)
    {
        var label = Text(parent, text, size, color);
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        return label;
    }

    private void ShowShopShelf(bool gears)
    {
        _shopGearsScreen = gears;
        Rebuild();
    }

    private void AddShopSoldCover(Control face)
    {
        var cover = new ColorRect { Name = "SourceShopSold", Color = new Color(0, 0, 0, .65f), MouseFilter = MouseFilterEnum.Ignore };
        face.AddChild(cover);
        cover.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var text = new Label { Text = "売切", HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        text.AddThemeColorOverride("font_color", Danger);
        text.AddThemeFontSizeOverride("font_size", FontSize(40));
        cover.AddChild(text);
        text.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    private void BuildShopGearShelf(RunState run, ShopState shop)
    {
        Heading("ギアの棚", $"自ターンに1個・魔素を払って組む。持ち物 {run.Gears?.Count ?? 0}/{Gears.GEAR_CARRY_MAX}・魔素 {Gears.ManaLabel(run.Mana.GetValueOrDefault())}・所持金 {run.Gold} G");
        var decision = new SourceUiDecision(_session?.Revision ?? 0);
        void Act(Action action) { if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked)) action(); }
        var scroll = ScrollColumn(_body);
        var shelf = Flow(scroll);
        shelf.Name = "SourceShopGearShelf";
        for (int i = 0; i < (shop.Gears?.Count ?? 0); i++)
        {
            int index = i;
            var offer = shop.Gears[i];
            var gear = Content.GetGearDef(offer.Id);
            var col = Tile(shelf, gear.Name + $" · 残り{gear.Charges ?? 1}回",
                CardText.RarityLabel(gear.Rarity) + " · " + gear.Text, "gears", gear.Id);
            col.GetParent<Control>().CustomMinimumSize = new Vector2(260, 220);
            if (offer.Sold == true) Text(col, "売切", 26, Danger);
            Button(col, offer.Sold == true ? "売切" : $"{offer.Price} G で買う", () => Act(() =>
                TakeGear(run, discard => Do(new RunCommand_ShopBuyGear { Index = index, DiscardIndex = discard }), incomingId: gear.Id)),
                offer.Sold != true && run.Gold >= offer.Price, Gold);
        }
        int manaPrice = shop.ManaPrice ?? Run.SHOP_MANA_PRICE;
        var mana = Tile(shelf, "魔素", SourceRunScreenPolicy.ManaOffer(run));
        mana.GetParent<Control>().CustomMinimumSize = new Vector2(260, 220);
        bool full = run.Mana.GetValueOrDefault() >= Gears.MANA_MAX;
        Button(mana, full ? "魔素は上限" : $"{manaPrice} G で買う", () => Act(() => Do(new RunCommand_ShopBuyMana())), !full && run.Gold >= manaPrice, Gold);
        Text(scroll, $"持ち物 {run.Gears?.Count ?? 0}/{Gears.GEAR_CARRY_MAX} · 「捨てる」で枠を整理する（戻せない）", 20, Gold);
        GearInventory(scroll, run, usable: false);
        Button(_body, "戻る", () => ShowShopShelf(false), true, Accent).Name = "SourceShopShelfBack";
    }

    private void BuildSavedRunSummary()
    {
        if (_session == null) return;
        string primary = _session.SaveDirectory.TrimEnd('/') + "/run.json";
        string backup = primary + ".bak";
        ulong stamp = FileAccess.FileExists(primary) ? FileAccess.GetModifiedTime(primary) : 0;
        ulong backupStamp = FileAccess.FileExists(backup) ? FileAccess.GetModifiedTime(backup) : 0;
        string key = primary + ":" + stamp + ":" + backupStamp + ":" + _session.Revision;
        if (_savedSummaryKey != key)
        {
            _savedSummaryKey = key;
            _savedSummaryRun = null;
            _savedSummaryPath = null;
            _savedSummaryBackup = false;
            var files = new GodotSessionFiles();
            var store = new SessionSaveStore(files, _session.SaveDirectory);
            var preview = SourceRunScreenPolicy.ReadSavedSummary(store);
            _savedSummaryRun = preview.Run;
            _savedSummaryBackup = preview.IsBackup;
            _savedSummaryPath = preview.Run == null ? null : preview.IsBackup ? backup : primary;
        }
        var run = _session.CurrentRun ?? _savedSummaryRun;
        if (run == null) { Text(_body, "進行中のラン · セーブの要約を読み取れません。続きからで確認できます。", 18, Muted); return; }
        var panel = new PanelContainer { Name = "SourceSavedRunSummary" };
        panel.AddThemeStyleboxOverride("panel", Box(Panel, Gold));
        _body.AddChild(panel);
        var row = Row(panel);
        Picture(row, "leaders", run.LeaderId + "_icon", new Vector2(56, 56));
        var text = Column(row);
        Text(text, "進行中のラン", 16, Gold);
        Text(text, SourceRunScreenPolicy.SaveSummary(run), 20);
        string saved = "";
        if (_savedSummaryPath != null)
        {
            ulong time = FileAccess.GetModifiedTime(_savedSummaryPath);
            if (time > 0) saved = "　最終保存 " + DateTimeOffset.FromUnixTimeSeconds((long)time).ToLocalTime().ToString("M/d HH:mm");
        }
        Text(text, SourceRunScreenPolicy.SaveDetail(run) + saved + (_session.AutoSave ? "　自動保存" : "　自動保存オフ") + (_savedSummaryBackup ? "　予備セーブ" : ""), 16, Muted);
    }

    private void ToggleFullscreen()
    {
        if (!DesktopVideoAvailable) return;
        SetDesktopMode(!IsFullscreenMode);
    }

    private void SetDesktopMode(bool full)
    {
        if (!DesktopVideoAvailable) return;
        if (full && !IsFullscreenMode) _windowResolution = DisplayServer.WindowGetSize();
        DisplayServer.WindowSetMode(full ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
        if (!full) DisplayServer.WindowSetSize(_windowResolution);
        FinishVideoChange();
    }

    private void SelectDesktopResolution(Vector2I resolution)
    {
        if (!DesktopVideoAvailable || resolution.X <= 0 || resolution.Y <= 0) return;
        _windowResolution = resolution;
        // Borderless fullscreen stays at native screen size. The chosen window size
        // is remembered, rather than forcing a fullscreen user back into a window.
        if (!IsFullscreenMode) DisplayServer.WindowSetSize(resolution);
        FinishVideoChange();
    }

    private async void FinishVideoChange()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInsideTree()) return;
        SaveSettings();
        Rebuild();
    }

    private void BuildDesktopVideoSettings(VBoxContainer box)
    {
        if (!DesktopVideoAvailable) return;
        Text(box, "画面", 21, Gold);
        var mode = Flow(box);
        Button(mode, (!IsFullscreenMode ? "● " : "○ ") + "ウィンドウ", () => SetDesktopMode(false));
        Button(mode, (IsFullscreenMode ? "● " : "○ ") + "フルスクリーン", () => SetDesktopMode(true));
        Text(box, "F11 か Alt+Enter でも切り替え", 17, Muted);
        var actual = DisplayServer.WindowGetSize();
        Text(box, $"いま {actual.X} × {actual.Y} · " + (IsFullscreenMode ? "フルスクリーン（画面の大きさ）" : "ウィンドウ"), 18, Muted);
        Text(box, "解像度（ウィンドウの大きさ。フルスクリーンは画面に合わせる）", 17, Muted);
        var resolutions = Flow(box);
        var screen = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
        foreach (var choice in SourceRunScreenPolicy.Resolutions(screen.X, screen.Y))
        {
            var resolution = new Vector2I(choice.Width, choice.Height);
            bool selected = _windowResolution == resolution;
            Button(resolutions, (selected ? "● " : "○ ") + $"{resolution.X} × {resolution.Y}", () => SelectDesktopResolution(resolution), true, selected ? Gold : null);
        }
    }
}
