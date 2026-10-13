// Source-backed r2 phone token controls. Reconstruction, not measured occupied-state parity.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
using Godot;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private Control? _sourceTokenBand;
    private SourceTokenFrame? _sourceTokenFrame;
    private Func<long>? _sourceTokenActiveRevision;
    public void SetSourceTokenPresentationRevision(Func<long> activeRevision) => _sourceTokenActiveRevision = activeRevision;
    private SourceTokenData? _sourceTokenData;
    private string _sourceTokenRenderedStamp = "";
    public string SourceTokenAdaptation { get; private set; } = "unbuilt";
    public long SourceTokenBoardRevision => _sourceTokenFrame?.Revision ?? -1;
    public SourceTokenFrame? CapturedSourceTokenFrame => _sourceTokenFrame;
    public IReadOnlyList<Rect2> SourceTokenControlBounds => _sourceTokenBand == null || !GodotObject.IsInstanceValid(_sourceTokenBand)
        ? Array.Empty<Rect2>() : _sourceTokenBand.GetChildren().OfType<Button>().Select(b => b.GetGlobalRect()).ToArray();

    // Called only at the existing board/rebuild boundary. It reads the UI-selected board, never CurrentRun.
    // The original owner revision is captured here; later session revisions cannot make this frame actionable.
    private void CaptureSourceTokenBoard()
    {
        _sourceTokenFrame = _view?.Combat == null ? null : SourceTokenModel.SelectForBuild(_sourceTokenFrame, _view, _uxEpoch, _activeChange?.Revision ?? -1, IsInputBlocked, _activeChange, _session?.Revision ?? -1);
        _sourceTokenData = _sourceTokenFrame == null ? null : SourceTokenModel.Capture(_sourceTokenFrame);
    }
    // Called once from the existing UxPresentEvent callback, after capturing the actual frame at entry.
    // A missing provider/owner, stale event, replaced frame, ordinary callback, or cleared fence fails closed.
    private void RefreshSourceTokenEvent(GameEvent evt, SourceTokenFrame? expectedFrame, SessionChange? mappedOwner)
    {
        if (evt is not GameEvent_TurnStarted) return;
        var updated = SourceTokenModel.TryOwnedTurnStart(_sourceTokenFrame, expectedFrame, mappedOwner, evt, _uxEpoch,
            _sourceTokenActiveRevision?.Invoke() ?? -1, _session?.Revision ?? -1, IsInputBlocked,
            _uxOrdinary?.Invoke(evt) ?? true, _receiptValidator?.Invoke(evt, null!, null) == true);
        if (updated == null) return;
        _sourceTokenFrame = updated;
        _sourceTokenData = SourceTokenModel.Capture(updated);
        RefreshSourceTokens();
    }
    // RefreshUxReadouts may be called for any numeric receipt. It never recaptures live or _view state.
    private void RefreshSourceTokens()
    {
        if (!PhoneLayout || _sourceTokenData == null || _sourceTokenBand == null || !GodotObject.IsInstanceValid(_sourceTokenBand)) return;
        if (_sourceTokenRenderedStamp != _sourceTokenData.Stamp) BuildSourceTokenBand();
    }
    private void BuildSourceTokenBand()
    {
        if (_sourceTokenBand != null && GodotObject.IsInstanceValid(_sourceTokenBand)) { _sourceTokenBand.Hide(); _sourceTokenBand.QueueFree(); }
        _sourceTokenBand = null;
        if (_sourceTokenData is not {} data || _uxBattle == null || !GodotObject.IsInstanceValid(_uxBattle)) return;
        float u = BattleUnit;
        _sourceTokenBand = new Control { Name = "SourceTokenBand", Size = Size, MouseFilter = MouseFilterEnum.Ignore };
        _uxBattle.AddChild(_sourceTokenBand);
        _sourceTokenRenderedStamp = data.Stamp;
        string setTitle = "からくり " + data.Sets.Count + " / " + data.SetSlots;
        string gearTitle = "ギア " + data.Gears.Count + " / " + Gears.GEAR_CARRY_MAX + (data.GearMute == null ? "" : "・" + data.GearMute);
        string permTitle = "置物 " + data.Permanents.Count;
        // Same source enemy-center boundary, made stricter by actual native target/intent bounds.
        float right = data.Frame.Board.Combat!.Enemies.Select((e, i) => (_enemyProjection?.Invoke(i)?.Feet.X / u ?? Size.X / u * (.60f + i * .12f)) - 120)
            .DefaultIfEmpty(Size.X / u - 300).Min();
        foreach (var enemy in _uxEnemies)
        {
            if (!GodotObject.IsInstanceValid(enemy.Hit)) continue;
            var r = enemy.Hit.GetGlobalRect();
            if (r.Position.Y < 173 * u && r.End.Y > 50 * u) right = Math.Min(right, r.Position.X / u - 8);
            var intent = enemy.Intent.GetGlobalRect();
            if (intent.Position.Y < 173 * u && intent.End.Y > 50 * u) right = Math.Min(right, intent.Position.X / u - 8);
        }
        var plan = SourceTokenLayout.Pack(Size.X / u, right, data.SetSlots, data.Gears.Count, data.Permanents.Count,
            SourceTokenHeadingWidth(setTitle, 200), SourceTokenHeadingWidth(gearTitle, data.GearMute == null ? 160 : 260), SourceTokenHeadingWidth(permTitle, 120));
        SourceTokenAdaptation = plan.Adaptation;
        _sourceTokenBand.SetMeta("source_token_adaptation", plan.Adaptation);
        foreach (var section in plan.Sections)
        {
            string title = section.Kind == "set" ? setTitle : section.Kind == "gear" ? gearTitle : permTitle;
            var panel = UxPanel(_sourceTokenBand, "SourceTokenHeading_" + section.Kind, SourceTokenNativeRect(section.Heading));
            var label = UxText(panel, title, 15, SourcePalette.Paper);
            label.ClipText = true; label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            panel.TooltipText = title;
        }
        foreach (var cell in plan.Cells)
        {
            int index = cell.Index;
            switch (cell.Kind)
            {
                case "set": BuildSourceSetToken(data, index, cell.Rect); break;
                case "gear": BuildSourceGearToken(data, index, cell.Rect); break;
                case "permanent": BuildSourcePermanentToken(data, index, cell.Rect); break;
                case "set-more": SourceMoreToken("SetMore", cell, "+" + data.Sets.Count, () => ShowSourceSetList(data)); break;
                case "gear-more": SourceMoreToken("GearMore", cell, "+" + cell.Hidden, () => ShowSourceGearInventory(data)); break;
                case "permanent-more": SourceMoreToken("PermanentMore", cell, "+" + cell.Hidden + " …", () => ShowSourcePermanentList(data)); break;
                default: SourceMoreToken("BattleTokensMore", cell, "盤面 +" + (data.Sets.Count + data.Gears.Count + data.Permanents.Count), () => ShowSourceAllTokens(data)); break;
            }
        }
    }
    private float SourceTokenHeadingWidth(string text, float maximum)
    {
        var font = GD.Load<Font>("res://assets/Fonts/KleeOne-Regular.ttf");
        int size = (int)Math.Round(15 * BattleUnit * _textScale);
        return Math.Min(maximum * _textScale, font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X / BattleUnit + 20);
    }
    private Rect2 SourceTokenNativeRect(SourceTokenRect r) => new(r.X * BattleUnit, r.Y * BattleUnit, r.W * BattleUnit, r.H * BattleUnit);
    private void BuildSourceSetToken(SourceTokenData data, int index, SourceTokenRect rect)
    {
        var token = index < data.Sets.Count ? data.Sets[index] : null;
        var slot = SourceTokenButton(_sourceTokenBand!, "SetPocket_" + index, rect, () => {
            if (token == null) ShowSourceSetList(data); else ShowSourceTokenDetails(token.Card.Def.Name, token.Details);
        });
        slot.SetMeta("source_slot_index", index);
        if (token == null) { SourceBattleDecor.Pocket(slot, 68, 74, BattleUnit); return; }
        slot.SetMeta("source_uid", token.Card.Uid);
        var edge = SourceTokenPaper(slot, !token.Live ? SourcePalette.InkSoft : token.Ready ? SourcePalette.Mana : SourcePalette.ManaLight, 68, 74);
        if (token.Ready)
        {
            // Source 1.2-second one-shot edge pulse, deterministic presentation-only phase.
            float phase = SourceTokenModel.PulsePhase(token.Card.Uid);
            var tween = slot.CreateTween();
            tween.TweenMethod(Callable.From<float>(k => { if (GodotObject.IsInstanceValid(edge)) edge.Modulate = new Color(1, 1, 1, SourceTokenModel.PulseAlpha(k, phase)); }), 0f, 1f, 1.2);
        }
        SourceTokenFill(slot, new(1, 1, 66, 40), SourcePalette.Ink);
        var picture = SourceTokenIcon(slot, "SetArt", SourceArtwork.Card(token.Card.Def.Id, token.Card.Def.Type), new(2, 2, 64, 38));
        if (!token.Live) picture.Modulate = new Color(.75f, .75f, .75f);
        SourceTokenFill(slot, new(1, 45, 66, 28), !token.Live ? SourcePalette.InkSoft : token.Ready ? SourcePalette.ManaInk : SourcePalette.ManaBand);
        SourceTokenLabel(slot, "SetState", token.State, new(0, 45, 68, 28), SourcePalette.Paper, true);
        if (token.Ready) SourceTokenBadge(slot, "SetWindows", token.Windows?.ToString() ?? "∞", new(50, -4, 22, 22), SourcePalette.Ink);
        slot.TooltipText = token.Card.Def.Name + "\n" + token.Details;
    }
    private void BuildSourceGearToken(SourceTokenData data, int index, SourceTokenRect rect)
    {
        var token = data.Gears[index]; var def = token.Definition; bool muted = data.GearMute != null;
        var slot = SourceTokenButton(_sourceTokenBand!, "GearToken_" + index, rect, () => ShowSourceGearDetails(data, index));
        slot.SetMeta("source_uid", token.Gear.Uid);
        Color edge = def.Rarity == "rare" ? SourcePalette.Brass : def.Rarity == "uncommon" ? new Color("6f95b8") : new Color(SourcePalette.Ink, .5f);
        SourceTokenPaper(slot, edge, 68, 66, muted);
        var mutedArt = muted ? SourceArtwork.GearMuted(token.Gear.GearId, def.Family) : null;
        var pic = SourceTokenIcon(slot, "GearArt", mutedArt ?? SourceArtwork.Gear(token.Gear.GearId, def.Family), new(17, 5, 34, 34));
        if (muted && mutedArt == null) pic.Modulate = new Color(1, 1, 1, .6f);
        SourceTokenFill(slot, new(1, 43, 66, 22), muted ? SourcePalette.InkSoft : SourcePalette.Ink);
        SourceTokenLabel(slot, "GearName", def.Name, new(3, 43, 62, 22), SourcePalette.Paper, true, .68f);
        if (token.Gear.Charges > 1 || (def.Charges ?? 1) > 1) SourceTokenBadge(slot, "GearCharges", token.Gear.Charges.ToString(), new(50, -4, 22, 22), muted ? SourcePalette.InkSoft : SourcePalette.Ink, muted);
        slot.TooltipText = def.Name + "\n" + token.Details;
    }
    private void BuildSourcePermanentToken(SourceTokenData data, int index, SourceTokenRect rect)
    {
        var token = data.Permanents[index]; var card = token.Card;
        var slot = SourceTokenButton(_sourceTokenBand!, "PermanentToken_" + index, rect, () => ShowSourceTokenDetails(card.Def.Name, token.Details));
        slot.SetMeta("source_uid", card.Uid);
        SourceTokenPaper(slot, SourcePalette.InkSoft, 168, 40, border: false);
        SourceTokenFill(slot, new(4, 4.5f, 50, 31), SourcePalette.Ink);
        var art = SourceArtwork.Card(card.Def.Id, card.Def.Type);
        Texture2D? cropped = art;
        if (art != null) cropped = new AtlasTexture { Atlas = art, Region = new Rect2(Math.Max(0, (art.GetWidth() - 48) / 2f), Math.Max(0, (art.GetHeight() - 29) / 2f), Math.Min(48, art.GetWidth()), Math.Min(29, art.GetHeight())) };
        SourceTokenIcon(slot, "PermanentArt", cropped, new(5, 5.5f, 48, 29));
        var name = SourceTokenLabel(slot, "PermanentName", card.Def.Name, new(60, 0, 100, 40), SourcePalette.Ink, true);
        name.HorizontalAlignment = HorizontalAlignment.Left;
        if (token.Doll) SourceTokenBadge(slot, "DollLife", token.Life?.ToString() ?? "∞", new(35, 4, 20, 20), token.Life == 1 ? SourcePalette.BadInk : SourcePalette.Ink);
        slot.TooltipText = card.Def.Name + "\n" + token.Details;
    }
    private void SourceMoreToken(string name, SourceTokenCell cell, string text, Action open)
    {
        var slot = SourceTokenButton(_sourceTokenBand!, name, cell.Rect, open);
        SourceTokenPaper(slot, SourcePalette.Ink, cell.Rect.W, cell.Rect.H);
        SourceTokenLabel(slot, "MoreCount", text, new(2, 0, Math.Max(1, cell.Rect.W - 4), cell.Rect.H), SourcePalette.Ink);
        slot.TooltipText = "押すと一覧";
    }
    private Button SourceTokenButton(Control parent, string name, SourceTokenRect rect, Action action)
    {
        var native = SourceTokenNativeRect(rect);
        var b = new Button { Name = name, Position = native.Position, Size = native.Size, Text = "", MouseFilter = MouseFilterEnum.Stop };
        foreach (var style in new[] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" }) b.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
        parent.AddChild(b); b.Pressed += action; return b;
    }
    private TextureRect SourceTokenIcon(Control parent, string name, Texture2D? texture, SourceTokenRect rect)
    {
        // Important: IgnoreSize must precede Texture AND Size; otherwise intrinsic dimensions stick.
        var pic = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = texture,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore, TextureFilter = TextureFilterEnum.Nearest,
            Name = name, Position = new Vector2(rect.X, rect.Y) * BattleUnit, Size = new Vector2(rect.W, rect.H) * BattleUnit };
        parent.AddChild(pic); return pic;
    }
    private void SourceTokenFill(Control p, SourceTokenRect rect, Color color) => p.AddChild(new ColorRect { Position = new Vector2(rect.X, rect.Y) * BattleUnit, Size = new Vector2(rect.W, rect.H) * BattleUnit, Color = color, MouseFilter = MouseFilterEnum.Ignore });
    private Panel SourceTokenPaper(Control parent, Color ink, float w, float h, bool muted = false, bool border = true)
    {
        var edge = new Panel { Position = Vector2.One * -3 * BattleUnit, Size = new Vector2(w + 6, h + 6) * BattleUnit, MouseFilter = MouseFilterEnum.Ignore };
        edge.AddThemeStyleboxOverride("panel", SourceBattleDecor.Box(ink, ink, BattleUnit, 0));
        if (border) parent.AddChild(edge); else { edge.Hide(); parent.AddChild(edge); }
        var paper = new Panel { Size = new Vector2(w, h) * BattleUnit, MouseFilter = MouseFilterEnum.Ignore };
        paper.AddThemeStyleboxOverride("panel", SourceBattleDecor.Box(SourcePalette.Paper2, SourcePalette.Paper2, BattleUnit, 0));
        if (muted) paper.Modulate = new Color(.88f, .90f, .96f);
        parent.AddChild(paper); return edge;
    }
    private Label SourceTokenLabel(Control p, string name, string text, SourceTokenRect rect, Color ink, bool deco = false, float minimumWidthScale = 1)
    {
        var label = UxText(p, text, 15, ink, deco); label.Name = name;
        label.Position = new Vector2(rect.X, rect.Y) * BattleUnit; label.Size = new Vector2(rect.W, rect.H) * BattleUnit;
        label.HorizontalAlignment = HorizontalAlignment.Center; label.ClipText = true; label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        float need = label.GetThemeFont("font").GetStringSize(text, HorizontalAlignment.Left, -1, label.GetThemeFontSize("font_size")).X;
        float sx = need > 0 ? Math.Clamp(rect.W * BattleUnit / need, minimumWidthScale, 1) : 1;
        if (sx < 1) { label.Size = new Vector2(rect.W * BattleUnit / sx, rect.H * BattleUnit); label.Scale = new Vector2(sx, 1); }
        return label;
    }
    private void SourceTokenBadge(Control p, string name, string text, SourceTokenRect rect, Color ink, bool muted = false)
    {
        var disc = new Panel { Name = name, Position = new Vector2(rect.X, rect.Y) * BattleUnit, Size = new Vector2(rect.W, rect.H) * BattleUnit, MouseFilter = MouseFilterEnum.Ignore };
        var style = SourceBattleDecor.Box(SourcePalette.Paper, muted ? SourcePalette.InkSoft : SourcePalette.Ink, BattleUnit, 1.5f);
        int radius = (int)Math.Round(rect.W * BattleUnit / 2); style.CornerRadiusTopLeft = style.CornerRadiusTopRight = style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = radius;
        disc.AddThemeStyleboxOverride("panel", style); if (muted) disc.SelfModulate = new Color(.88f, .90f, .96f); p.AddChild(disc);
        SourceTokenLabel(disc, "BadgeValue", text, new(0, 0, rect.W, rect.H), ink, true);
    }
    private void ShowSourceTokenDetails(string title, string details) => Open(title, b => Text(ScrollColumn(b), details, 23));
    private void ShowSourceSetList(SourceTokenData data) => Open("からくり " + data.Sets.Count + " / " + data.SetSlots, b => {
        var col = ScrollColumn(b); foreach (var token in data.Sets) { Text(col, token.Card.Def.Name, 23, Gold); Text(col, token.Details, 21); }
        if (data.Sets.Count == 0) Text(col, "仕込み札を置く枠です。準備が明けると、条件に合う敵の行動に反応します。", 21);
    });
    private void ShowSourcePermanentList(SourceTokenData data) => Open("置物 " + data.Permanents.Count, b => {
        var col = ScrollColumn(b); foreach (var token in data.Permanents) { Text(col, token.Card.Def.Name, 23, Gold); Text(col, token.Details, 21); }
    });
    private void ShowSourceAllTokens(SourceTokenData data) => Open("盤面", b => {
        Button(b, "からくり " + data.Sets.Count + " / " + data.SetSlots, () => ShowSourceSetList(data));
        Button(b, "ギア " + data.Gears.Count, () => ShowSourceGearInventory(data));
        Button(b, "置物 " + data.Permanents.Count, () => ShowSourcePermanentList(data));
    });
    private void ShowSourceGearDetails(SourceTokenData data, int index)
    {
        var token = data.Gears[index]; var frame = data.Frame;
        Open(token.Definition.Name, b => {
            Text(ScrollColumn(b), token.Details, 23);
            var decision = new SourceUiDecision(frame.Revision); object? overlayOwner = _overlay;
            bool Current() => ReferenceEquals(overlayOwner, _overlay) && SourceTokenModel.GearCurrent(frame, _session?.CurrentRun, _session?.Revision ?? -1, IsInputBlocked, index, token.Gear.Uid, token.Gear.GearId, _uxEpoch);
            Button(b, token.Blocked ?? "組む（魔素1個）", () => {
                if (!Current() || !decision.TryAccept(_session!.Revision, IsInputBlocked)) return;
                BeginGear(_session!.CurrentRun, index, token.Definition);
            }, token.Blocked == null && Current(), Gold);
        });
    }
    private void ShowSourceGearInventory(SourceTokenData data) => Open("ギア " + data.Gears.Count, b => {
        // Reuse the existing inventory and command routes. Guard its existing callbacks, including discard,
        // before they open another choice/confirmation. Later confirmations retain their own revision gate.
        GearInventory(b, data.Frame.Board, usable: true);
        object? overlayOwner = _overlay;
        var decision = new SourceUiDecision(data.Frame.Revision);
        bool Current() => ReferenceEquals(overlayOwner, _overlay) && SourceTokenModel.InventoryCurrent(data.Frame, _session?.CurrentRun, _session?.Revision ?? -1, IsInputBlocked, _uxEpoch);
        foreach (var button in SourceTokenButtons(b))
        {
            var callbacks = button.GetSignalConnectionList(BaseButton.SignalName.Pressed).Select(c => c["callable"].AsCallable()).ToArray();
            foreach (var callback in callbacks) button.Disconnect(BaseButton.SignalName.Pressed, callback);
            button.Disabled |= !Current();
            button.Pressed += () => { if (!Current() || !decision.TryAccept(_session!.Revision, IsInputBlocked)) return; foreach (var callback in callbacks) callback.Call(); };
        }
    });
    private static IEnumerable<Button> SourceTokenButtons(Node root)
    {
        foreach (var child in root.GetChildren()) { if (child is Button button) yield return button; foreach (var nested in SourceTokenButtons(child)) yield return nested; }
    }
}
