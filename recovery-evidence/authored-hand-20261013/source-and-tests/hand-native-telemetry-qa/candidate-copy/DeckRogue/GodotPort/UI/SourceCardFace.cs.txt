// Reconstructed 2026-10-12 from the original Unity CardView/PaperFx/Theme sources
// and ux-ref-a7f86b5 H5/H10 reference pixels. This is not recovered exact Godot source.
// Presentation only: no card definitions, effects, targets or game state are mutated.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public static class SourceCardFace
{
    public const float W = 200f;
    public const float H = 290f;
    private const string FaceNodeName = "SourceCardFace";
    private static readonly Dictionary<string, Texture2D?> Textures = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Font?> Fonts = new(StringComparer.Ordinal);
    private static readonly Color GoodInk = new("276a34");
    private static readonly Color BadDown = new("a33a30");
    private static readonly Color DimTint = new(0.88f, 0.90f, 0.96f);
    private static readonly Regex Markup = new("<[^>]*>", RegexOptions.Compiled);

    /// <summary>Fill one 200×290 local-unit face. The caller owns scaling, rotation,
    /// hit testing, dragging, inspection and whether a card is playable OR settable.
    /// 'face' is the visual enabled state; it is independent of Button.Disabled.</summary>
    public static void Fill(CardTile tile, CardInstance card, GameState? state, float textScale,
        bool phone, int target = -1, int? cost = null, string? badge = null, bool face = true)
    {
        if (tile == null || card == null) return;
        var old = tile.GetNodeOrNull<Control>(FaceNodeName);
        if (old != null) { tile.RemoveChild(old); old.QueueFree(); }
        tile.Text = "";
        tile.ClipContents = false; // The 52-unit cost orb deliberately protrudes at (-6,-6).
        foreach (string key in new[] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" })
            tile.AddThemeStyleboxOverride(key, new StyleBoxEmpty());

        var def = card.Def;
        Color ink = face ? SourcePalette.Ink : SourcePalette.InkSoft;
        string rarity = def.Rarity ?? (def.Id != null && def.Id.StartsWith("fusion_", StringComparison.Ordinal) ? "rare" : "common");
        Color typeColor = TypeColor(def.Type);
        var root = new Control { Name = FaceNodeName, Size = new Vector2(W, H), MouseFilter = Control.MouseFilterEnum.Ignore };
        tile.AddChild(root);

        var paper = new NinePatchRect {
            Name = "Paper", Texture = Paper(rarity), Size = new Vector2(W, H),
            PatchMarginLeft = 16, PatchMarginRight = 16, PatchMarginTop = 16, PatchMarginBottom = 16,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear, MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = face ? Colors.White : DimTint
        };
        root.AddChild(paper);
        var grain = Picture(root, "Grain", Grain(), new Rect2(0, 0, W, H), false);
        grain.StretchMode = TextureRect.StretchModeEnum.Tile;
        grain.Modulate = new Color(1, 1, 1, 0.7f);

        Rect(root, "ArtEdge", new Rect2(12, 44, 176, 100), SourcePalette.Ink);
        Rect(root, "ArtNight", new Rect2(13.5f, 45.5f, 173, 97), SourcePalette.Window);
        // Do not call SourceArtwork.Card: that helper has a random thumbnail fallback.
        var art = Asset("cards", def.Id) ?? SourceArtwork.Fused(def.Id);
        if (art != null)
        {
            float aw = Math.Min(160, art.GetWidth() * 2f), ah = Math.Min(96, art.GetHeight() * 2f);
            var pic = Picture(root, "Artwork", art, new Rect2(100 - aw / 2, 94 - ah / 2, aw, ah));
            pic.Modulate = face ? Colors.White : new Color(0.8f, 0.8f, 0.8f);
        }
        else
        {
            Rect(root, "TypeWash", new Rect2(13.5f, 45.5f, 173, 97), new Color(typeColor, 0.3f));
            var authoredCrest = Asset("icons", "crest_" + def.Type);
            float side = authoredCrest != null ? 64 : 48;
            var crest = Picture(root, "TypeCrest", authoredCrest ?? Crest(def.Type), new Rect2(100 - side / 2, 94 - side / 2, side, side));
            crest.Modulate = new Color(1, 1, 1, face ? 1 : 0.7f);
        }

        var orbArt = Asset("ui", "cost_orb");
        var orb = face ? orbArt ?? Orb(SourcePalette.Brass) :
            (orbArt != null ? MutedOrb(orbArt) ?? Orb(SourcePalette.Brass.Lerp(SourcePalette.PaperDim, 0.75f)) : Orb(SourcePalette.Brass.Lerp(SourcePalette.PaperDim, 0.75f)));
        Picture(root, "CostOrb", orb, new Rect2(-6, -6, 52, 52), orbArt != null);
        int effectiveCost = cost ?? EffectiveCost(state, card);
        var costColor = def.XCost == true || effectiveCost == def.Cost ? SourcePalette.Ink : effectiveCost < def.Cost ? GoodInk : BadDown;
        AddLabel(root, "Cost", def.XCost == true ? "X" : effectiveCost.ToString(CultureInfo.InvariantCulture),
            new Rect2(-6, -5, 52, 50), Scaled(22, textScale), costColor, DecoFont);

        bool compactName = phone && tile.IsBattleHand && state != null && state.Player.Hand.Count >= 7;
        int nameSize = def.Name.Length > 5 ? 17 : def.Name.Length > 4 ? 18 : 20;
        AddLabel(root, "Name", def.Name, compactName ? new Rect2(48, 8, 142, 32) : new Rect2(44, 8, 144, 32),
            Scaled(nameSize, textScale), ink, DecoFont, compactName ? HorizontalAlignment.Left : HorizontalAlignment.Center);

        Picture(root, "TypeRibbon", Ribbon(face ? typeColor : typeColor.Lerp(Colors.Gray, 0.3f)), new Rect2(25, 136, 150, 26), false);
        var gemArt = Asset("ui", "gem_" + rarity);
        float gemSide = gemArt != null ? 24 : 16;
        string typeName = CardText.TypeJa(def.Type);
        int typeSize = Scaled(16, textScale);
        float typeWidth = BodyFont?.GetStringSize(typeName, HorizontalAlignment.Left, -1, typeSize).X ?? typeName.Length * typeSize;
        float rowWidth = gemSide + 5 + typeWidth;
        float rowX = 100 - rowWidth / 2;
        Picture(root, "RarityGem", gemArt ?? Gem(rarity), new Rect2(rowX, 149 - gemSide / 2, gemSide, gemSide));
        AddLabel(root, "Type", typeName, new Rect2(rowX + gemSide + 5, 138, typeWidth, 22), typeSize, SourcePalette.Ink, BodyFont);

        string markup = BodyMarkup(card, state, target, true);
        string fullText = BodyText(card, state, target);
        var body = new SourceCardBody {
            Name = "Body", Position = new Vector2(14, 172), Size = new Vector2(172, string.IsNullOrEmpty(badge) ? 104 : 86),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        body.Configure(markup, BodyFont, ink, textScale, truncated => tile.SetMeta("source_body_truncated", truncated));
        root.AddChild(body);
        // Full text remains available even when the source-sized face cannot show every line.
        tile.SetMeta("source_body", fullText);
        tile.SetMeta("source_card_name", def.Name);
        tile.SetMeta("source_card_badge", badge ?? "");
        tile.SetMeta("source_preview_enemy", target);
        tile.SetMeta("source_card_phone", phone);
        tile.SetMeta("source_face_enabled", face);
        if (!string.IsNullOrEmpty(badge))
            AddLabel(root, "Badge", badge, new Rect2(14, 260, 172, 18), Scaled(13, textScale), SourcePalette.BrassInk, BodyFont);

        // Desktop live-hand hover explains keywords only. Phone live hand has no passive tooltip.
        // Long-press/Inspect remains entirely owned by the caller.
        tile.TooltipText = tile.IsBattleHand ? (phone ? "" : KeywordTooltip(fullText)) :
            def.Name + "\n" + fullText + (string.IsNullOrWhiteSpace(badge) ? "" : "\n" + badge);
    }

    public static int EffectiveCost(GameState? state, CardInstance card)
    {
        if (state == null) return card.Def.Cost;
        try { return Effects.EffectiveCost(state, card); }
        catch (Exception) { return card.Def.Cost; }
    }

    /// <summary>Read-only source damage display: grow bonus, growth, momentum, weak,
    /// then the selected enemy's expose and armor. Enemy block/HP-loss reducers are excluded.</summary>
    public static Func<int, int> MakeDamageModifier(GameState state, CardInstance card, int target = -1)
    {
        var p = state.Player;
        int grow = card.GrowBonus ?? 0;
        bool hasTarget = target >= 0 && target < state.Enemies.Count && state.Enemies[target].Hp > 0;
        return baseAmount => {
            int amount = baseAmount + grow;
            if (hasTarget)
            {
                try
                {
                    var breakdown = Effects.DamageBreakdownOf(state, target, amount, false);
                    if (breakdown != null && breakdown.Steps.Count > 0)
                    {
                        int value = amount;
                        foreach (var step in breakdown.Steps)
                        {
                            string label = step.Label ?? "";
                            if (label.StartsWith("敵ブロック", StringComparison.Ordinal) || label.StartsWith("貫通", StringComparison.Ordinal) ||
                                label.StartsWith("潜伏", StringComparison.Ordinal) || label.StartsWith("無形", StringComparison.Ordinal) ||
                                label.StartsWith("ターン装甲", StringComparison.Ordinal) || label.StartsWith("朧", StringComparison.Ordinal)) break;
                            value = step.Value;
                        }
                        return value;
                    }
                }
                catch (Exception) { /* Fall back to source untargeted arithmetic. */ }
            }
            if (p.Growth > 0) amount += p.Growth;
            if (p.Momentum > 0) amount += p.Momentum;
            if (p.Weak > 0 && amount > 0) amount = Math.Max(1, (int)Math.Floor(amount * 0.75));
            return amount;
        };
    }

    /// <summary>Full, untruncated effect text plus full cost/trailing notes for Inspect.</summary>
    public static string BodyText(CardInstance card, GameState? state = null, int target = -1) => Plain(BodyMarkup(card, state, target, false));
    public static string CardBodyText(CardInstance card, GameState? state = null, int target = -1) => BodyText(card, state, target);

    /// <summary>Unity-style source markup. It is converted to Godot BBCode at layout time.</summary>
    public static string BodyMarkup(CardInstance card, GameState? state = null, int target = -1, bool compactNotes = true)
    {
        var def = card.Def;
        var oldModifier = CardText.DamageModifier;
        bool oldClauses = CardText.MarkClauses;
        Func<int, int>? modifier = state != null ? MakeDamageModifier(state, card, target) : null;
        string body;
        try
        {
            CardText.DamageModifier = modifier;
            CardText.MarkClauses = true;
            // Use the source's line folding AFTER damage has been colored. The baseline
            // CardText.Colored loses color, so this small adapter restores it locally.
            if (def.Id != null && def.Id.StartsWith("status_", StringComparison.Ordinal)) body = CardText.Body(def);
            else
            {
                var lines = EffectLines(def.Effects, def.Type, modifier);
                if (def.Cascade.GetValueOrDefault() > 0)
                    lines.Insert(0, "チェイン" + (def.Cascade > 1 ? "×" + def.Cascade : "") + ": 山札からより低コストの札をコストを払わずにプレイ(本体より先)");
                if (def.Modes != null)
                    foreach (var mode in def.Modes) lines.Add("◆" + string.Join("、", EffectLines(mode.Effects, def.Type, modifier)));
                body = string.Join("\n", lines);
            }
            body = CardText.Emphasize(body, SourcePalette.InkSoft.ToHtml(false));
            if (def.Modes != null && def.Modes.Count > 0) body = ColorTag(SourcePalette.InkSoft, "どちらか一つ") + "\n" + body;
            var costNotes = CardText.CostNotes(def);
            if (costNotes.Count > 0)
            {
                var parts = new List<string>();
                foreach (string note in costNotes) parts.Add(Clause(note));
                body = string.Join("\n", parts) + "\n" + body;
            }
            var trail = CardText.TrailNotes(def, compactNotes);
            if (trail.Count > 0)
            {
                var parts = new List<string>();
                foreach (string note in trail)
                    parts.Add(note == "消滅" || note == "保持" || note == "骨のナイフ" ? ColorTag(SourcePalette.BrassInk, "<nobr>" + note + "</nobr>") : Clause(note));
                body += "\n" + string.Join("・", parts);
            }
            return body;
        }
        finally { CardText.DamageModifier = oldModifier; CardText.MarkClauses = oldClauses; }
    }

    private static string Clause(string text) => CardText.Emphasize(CardText.ClauseOpen + text + CardText.ClauseClose, SourcePalette.InkSoft.ToHtml(false));
    private static string ColorTag(Color color, string text) => "<color=#" + color.ToHtml(false) + ">" + text + "</color>";
    public static string Plain(string text) => Markup.Replace(text ?? "", "").Replace(CardText.ClauseOpen.ToString(), "").Replace(CardText.ClauseClose.ToString(), "");
    public static string KeywordTooltip(string text)
    {
        var lines = new List<string>();
        foreach (string term in KeywordHelp.FindIn(Plain(text))) lines.Add(term + ": " + KeywordHelp.Terms[term]);
        return string.Join("\n\n", lines);
    }

    // Same ordered fold as source CardText.LinesOf: adjacent identical tokens become
    // a×n, repeated mixed two/three-effect cycles become [a → b]×n. No totals are invented.
    private static List<string> EffectLines(IReadOnlyList<DeclarativeEffect>? effects, string type, Func<int, int>? modifier)
    {
        var result = new List<string>();
        if (effects == null) return result;
        var heads = new List<string>();
        var bodies = new List<string>();
        foreach (var effect in effects)
        {
            string body = CardText.EffectLine(effect, type, true);
            string full = CardText.EffectLine(effect, type, false);
            heads.Add(full.EndsWith(body, StringComparison.Ordinal) ? full.Substring(0, full.Length - body.Length) : "");
            if (effect.Effect == "dealDamage" && modifier != null && (effect.Trigger == null || effect.Trigger == "onPlay"))
            {
                int amount = effect.Amount ?? 0, shown = modifier(amount);
                if (shown != amount)
                {
                    string token = "ダメージ" + shown.ToString(CultureInfo.InvariantCulture);
                    int at = body.LastIndexOf(token, StringComparison.Ordinal);
                    if (at >= 0) body = body.Substring(0, at) + "ダメージ" + ColorTag(shown > amount ? GoodInk : BadDown, shown.ToString(CultureInfo.InvariantCulture)) + body.Substring(at + token.Length);
                }
            }
            bodies.Add(body);
        }
        string? previousHead = null;
        for (int a = 0; a < bodies.Count;)
        {
            bool folded = false;
            for (int k = 2; k <= 3 && !folded && a + 2 * k <= bodies.Count; k++)
            {
                bool sameHead = true, allSame = true;
                for (int j = 1; j < k; j++) { sameHead &= heads[a + j] == heads[a]; allSame &= bodies[a + j] == bodies[a]; }
                if (!sameHead || allSame) continue;
                int repeats = 1;
                while (a + (repeats + 1) * k <= bodies.Count)
                {
                    bool match = true;
                    for (int j = 0; j < k; j++)
                        if (heads[a + repeats * k + j] != heads[a + j] || bodies[a + repeats * k + j] != bodies[a + j]) { match = false; break; }
                    if (!match) break;
                    repeats++;
                }
                if (repeats < 2) continue;
                result.Add((heads[a] == previousHead ? "" : heads[a]) + "［" + string.Join(" → ", bodies.GetRange(a, k)) + "］×" + repeats + "回");
                previousHead = heads[a]; a += repeats * k; folded = true;
            }
            if (folded) continue;
            int b = a;
            while (b + 1 < bodies.Count && heads[b + 1] == heads[a] && bodies[b + 1] == bodies[a]) b++;
            result.Add((heads[a] == previousHead ? "" : heads[a]) + bodies[a] + (b > a ? " ×" + (b - a + 1) + "回" : ""));
            previousHead = heads[a]; a = b + 1;
        }
        return result;
    }

    internal static string Bbcode(string source, int fontSize)
    {
        var output = new StringBuilder("[center]");
        int noBreak = 0;
        for (int i = 0; i < source.Length; i++)
        {
            char ch = source[i];
            if (ch == '<')
            {
                int end = source.IndexOf('>', i);
                if (end >= 0)
                {
                    string tag = source.Substring(i + 1, end - i - 1);
                    if (tag == "nobr") noBreak++;
                    else if (tag == "/nobr") noBreak = Math.Max(0, noBreak - 1);
                    else if (tag.StartsWith("color=", StringComparison.Ordinal)) output.Append('[').Append(tag).Append(']');
                    else if (tag == "/color") output.Append("[/color]");
                    else if (tag.StartsWith("size=", StringComparison.Ordinal))
                    {
                        string value = tag.Substring(5).TrimEnd('%');
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float percent))
                            output.Append("[font_size=").Append(Math.Max(1, (int)Math.Round(fontSize * percent / 100f))).Append(']');
                    }
                    else if (tag == "/size") output.Append("[/font_size]");
                    else if (tag.StartsWith("mark=", StringComparison.Ordinal)) output.Append("[bgcolor=").Append(tag.Substring(5)).Append(']');
                    else if (tag == "/mark") output.Append("[/bgcolor]");
                    i = end; continue;
                }
            }
            if (ch == '[') output.Append("[lb]");
            else if (ch == ']') output.Append("[rb]");
            else if (ch != CardText.ClauseOpen && ch != CardText.ClauseClose) output.Append(ch);
            if (noBreak > 0 && ch != '\n') output.Append('\u2060'); // Godot equivalent of TMP's <nobr>.
        }
        return output.Append("[/center]").ToString();
    }

    private static int Scaled(int size, float scale) => Math.Max(1, (int)Math.Round(size * Math.Clamp(scale, 1, 1.3f)));
    private static Font? BodyFont => LoadFont("KleeOne-SemiBold.ttf") ?? LoadFont("NotoSansJP-Bold.otf");
    private static Font? DecoFont => LoadFont("KaiseiDecol-Bold.ttf") ?? BodyFont;
    private static Font? LoadFont(string name)
    {
        if (Fonts.TryGetValue(name, out var font)) return font;
        string path = "res://assets/Fonts/" + name;
        return Fonts[name] = ResourceLoader.Exists(path) ? GD.Load<Font>(path) : null;
    }
    private static Color TypeColor(string type) => new(type switch { "spell" => "bca6d6", "reaction" => "7ab8b0", "permanent" => "b3b67a", _ => "c9a982" });
    private static Texture2D? Asset(string category, string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Contains('/') || id.Contains('\\') || id.Contains("..", StringComparison.Ordinal)) return null;
        string key = "asset:" + category + "/" + id;
        if (Textures.TryGetValue(key, out var found)) return found;
        string path = "res://assets/Art/" + category + "/" + id + ".png";
        Texture2D? texture = null;
        if (ResourceLoader.Exists(path)) texture = GD.Load<Texture2D>(path);
        else if (Godot.FileAccess.FileExists(path)) { using var image = Image.LoadFromFile(path); if (!image.IsEmpty()) texture = ImageTexture.CreateFromImage(image); }
        return Textures[key] = texture;
    }
    private static TextureRect Picture(Control parent, string name, Texture2D texture, Rect2 rect, bool nearest = true)
    {
        // Godot 4.6.3 keeps an intrinsic texture minimum if Texture/Size is set
        // before IgnoreSize. Order matters, even outside a Container.
        var picture = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Name = name, Texture = texture, Position = rect.Position, Size = rect.Size,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = nearest ? CanvasItem.TextureFilterEnum.Nearest : CanvasItem.TextureFilterEnum.Linear,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(picture); return picture;
    }
    private static void Rect(Control parent, string name, Rect2 rect, Color color) => parent.AddChild(new ColorRect {
        Name = name, Position = rect.Position, Size = rect.Size, Color = color, MouseFilter = Control.MouseFilterEnum.Ignore });
    private static Label AddLabel(Control parent, string name, string text, Rect2 rect, int size, Color color, Font? font,
        HorizontalAlignment align = HorizontalAlignment.Center)
    {
        // TMP centers the painted glyphs, not Godot's potentially taller font
        // line minimum. Keep the source rectangle/font size and align actual ink.
        var label = new SourceHudLabel { Name = name, Text = text, SourceTextRect = rect,
            HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Off, TextureFilter = CanvasItem.TextureFilterEnum.Linear, MouseFilter = Control.MouseFilterEnum.Ignore };
        if (font != null) label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color);
        // Assign the rectangle only after font overrides establish the real minimum.
        label.Position = rect.Position; label.Size = rect.Size;
        parent.AddChild(label); label.RefreshSourceAlignment(); return label;
    }
    private static Texture2D MakeTexture(string key, int w, int h, Func<int, int, Color> pixel)
    {
        if (Textures.TryGetValue(key, out var existing) && existing != null) return existing;
        using var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) image.SetPixel(x, y, pixel(x, y));
        var texture = ImageTexture.CreateFromImage(image); texture.ResourceName = key; Textures[key] = texture; return texture;
    }
    private static Texture2D Paper(string rarity)
    {
        var authored = Asset("ui", "paper_card_" + rarity);
        if (authored != null) return authored;
        Color edge = rarity == "rare" ? SourcePalette.Brass : rarity == "uncommon" ? new Color("6f95b8") : new Color(SourcePalette.Ink, 0.5f);
        float width = rarity == "common" ? 1 : 2;
        return MakeTexture("paper:" + rarity, 52, 52, (x, y) => {
            float qx = Math.Abs(x + 0.5f - 26) - 12, qy = Math.Abs(y + 0.5f - 26) - 12;
            float d = -(MathF.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) + Math.Max(qy, 0) * Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - 14);
            if (d < 0) return Colors.Transparent;
            Color c = d < width ? edge : d < 4 ? SourcePalette.Paper : d < 6 ? SourcePalette.Ink : SourcePalette.Paper;
            if (d < 1) c.A *= Math.Clamp(d + 0.5f, 0, 1); return c;
        });
    }
    private static Texture2D Grain()
    {
        var random = new Random(20260907);
        return MakeTexture("paper-grain", 128, 128, (x, y) => { double value = random.NextDouble(); return new Color(SourcePalette.Ink, value < 0.55 ? 0 : (float)((value - 0.55) / 0.45) * 0.09f); });
    }
    private static Texture2D Ribbon(Color color)
    {
        Color edge = color.Lerp(SourcePalette.Ink, 0.55f);
        bool Inside(float x, float y, float margin)
        {
            float yy = Math.Abs(y - 13);
            if (yy > 13 - margin) return false;
            if (x >= 8 + margin && x <= 142 - margin) return true;
            float tx = (x < 75 ? x : 150 - x) - margin * 1.3f;
            return tx >= 0 && yy <= 13 * tx / 8 - margin * 0.5f;
        }
        return MakeTexture("ribbon:" + color.ToHtml(), 150, 26, (x, y) => !Inside(x + 0.5f, y + 0.5f, 0) ? Colors.Transparent : !Inside(x + 0.5f, y + 0.5f, 1.5f) ? edge : color);
    }
    private static Texture2D Orb(Color color) => MakeTexture("orb:" + color.ToHtml(), 52, 52, (x, y) => {
        float dx = x + 0.5f - 26, dy = y + 0.5f - 26, r = MathF.Sqrt(dx * dx + dy * dy);
        Color c = r < 20 ? color.Lerp(Colors.White, 0.35f).Lerp(color.Lerp(Colors.Black, 0.18f), Math.Clamp((dx + dy) / 40 + 0.5f, 0, 1)) :
            r < 22 ? SourcePalette.Ink : r < 24 ? SourcePalette.Paper : r < 25 ? new Color(SourcePalette.Ink, 0.5f) : Colors.Transparent;
        if (r >= 24 && r < 25.5f) c.A *= Math.Clamp(25.5f - r, 0, 1); return c;
    });
    private static Texture2D? MutedOrb(Texture2D source)
    {
        const string key = "cost-orb-muted";
        if (Textures.TryGetValue(key, out var saved)) return saved;
        try
        {
            using var shared = source.GetImage();
            if (shared == null || shared.IsEmpty()) return Textures[key] = null;
            using var image = (Image)shared.Duplicate();
            if (image.IsCompressed()) image.Decompress();
            return MakeTexture(key, image.GetWidth(), image.GetHeight(), (x, y) => {
                Color c = image.GetPixel(x, y); float luma = 0.3f * c.R + 0.59f * c.G + 0.11f * c.B;
                Color muted = c.Lerp(new Color(luma, luma, luma, c.A), 0.7f).Lerp(new Color(SourcePalette.PaperDim, c.A), 0.15f);
                return new Color(MathF.Round(muted.R * 255) / 255, MathF.Round(muted.G * 255) / 255, MathF.Round(muted.B * 255) / 255, c.A);
            });
        }
        catch (Exception) { return Textures[key] = null; }
    }
    private static Texture2D Gem(string rarity)
    {
        Color color = new(rarity == "rare" ? "e0b84a" : rarity == "uncommon" ? "5aa0e0" : "9aa39c");
        return MakeTexture("gem:" + rarity, 8, 8, (x, y) => {
            int sy = 7 - y, m = Math.Abs(x - 3) + Math.Abs(sy - 3) + (x > 3 ? 1 : 0) + (sy > 3 ? 1 : 0);
            return m > 3 ? Colors.Transparent : m == 3 ? color.Lerp(Colors.Black, 0.5f) : x < 4 && sy > 3 ? color.Lerp(Colors.White, 0.4f) : color;
        });
    }
    private static Texture2D Crest(string type)
    {
        // Exact source Theme.IconArt silhouettes, not card-id-seeded decorative art.
        string[] rows = type switch {
            "spell" => new[] { ".......--.......", "......-##-......", "...-..-##-..-...", "..-#-.-##-.-#-..", "...-#--##--#-...", "....-######-....", ".....-####-.....", "-######++######-", "-######++######-", ".....-####-.....", "....-######-....", "...-#--##--#-...", "..-#-.-##-.-#-..", "...-..-##-..-...", "......-##-......", ".......--......." },
            "reaction" => new[] { "................", "................", "................", ".....------.....", "...--######--...", "..-###----###-..", ".-##--####--##-.", "-##-##-##-##-##-", "-##-##-++-##-##-", ".-##--####--##-.", "..-###----###-..", "...--######--...", ".....------.....", "................", "................", "................" },
            "permanent" => new[] { ".......--.......", "......-##-......", ".....-####-.....", "....-######-....", "...-########-...", "..-##########-..", ".-############-.", "-######++######-", "..-----##-----..", "......-##-......", "......-##-......", "......-##-......", ".....-####-.....", "....-######-....", "...-########-...", "...----------..." },
            _ => new[] { "-..............-", "#-............-#", "-#-..........-#-", ".-#-........-#-.", "..-#-......-#-..", "...-#-....-#-...", "....-#-..-#-....", ".....-#--#-.....", ".....-#--#-.....", "....-#-..-#-....", "...-#-....-#-...", "..-#-......-#-..", ".-#-........-#-.", "-#-..........-#-", "#-............-#", "-..............-" }
        };
        return MakeTexture("crest:" + type, 16, 16, (x, y) => rows[y][x] switch { '#' => SourcePalette.Paper2, '+' => SourcePalette.Paper, '-' => SourcePalette.Ink, _ => Colors.Transparent });
    }
}

/// <summary>Source-sized rich body, fitted at 18/16 down to the source exception of 12.
/// The complete text is retained separately on the card; this control never edits it.</summary>
