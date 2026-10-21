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
	public const float W = 252f;

	public const float H = 352f;

	private const string FaceNodeName = "SourceCardFace";

	private static readonly Dictionary<string, Texture2D?> Textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

	private static readonly Dictionary<string, Font?> Fonts = new Dictionary<string, Font>(StringComparer.Ordinal);

	private static readonly Color GoodInk = new Color("276a34");

	private static readonly Color BadDown = new Color("a33a30");

	private static readonly Color DimTint = new Color(0.88f, 0.9f, 0.96f);

	private static readonly Regex Markup = new Regex("<[^>]*>", RegexOptions.Compiled);

	private static Font? _cardBodyFont;
    private static Font? BodyFont => _cardBodyFont ??= new FontVariation {
        BaseFont = LoadFont("NotoSansJP-Regular.otf"), VariationEmbolden = 0f,
        SpacingTop = 0, SpacingBottom = 0
    };

	private static Font? DecoFont => LoadFont("NotoSansJP-Bold.otf") ?? BodyFont;

    // Selected C / W09 face. Geometry is authored at 630 x 880 and uniformly
    // mapped to the existing 252 x 352 contract. Only the shell is cached;
    // names, effective costs, art, effects and rarity remain live data.
    public static void Fill(CardTile tile, CardInstance card, GameState? state, float textScale, bool phone, int target = -1, int? cost = null, string? badge = null, bool face = true)
    {
        if (tile == null || card == null) return;
        Control? old = tile.GetNodeOrNull<Control>(FaceNodeName);
        if (old != null) { tile.RemoveChild(old); old.QueueFree(); }
        tile.Text = "";
        tile.ClipContents = false;
        foreach (string key in new[] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" })
            tile.AddThemeStyleboxOverride(key, new StyleBoxEmpty());
        CardDef def = card.Def;
        string rarity = def.Rarity ?? "common";
        int rank = rarity == "rare" ? 2 : rarity == "uncommon" ? 1 : 0;
        string faction = FaceFaction(def);
        string type = def.Type == "trap" ? "reaction" : def.Type == "artifact" ? "permanent" : def.Type;
        var root = new Control { Name = FaceNodeName, Size = new Vector2(W, H), MouseFilter = Control.MouseFilterEnum.Ignore };
        tile.AddChild(root);
        Picture(root, "SelectedFrame", SelectedFrame(faction, type, rank), new Rect2(0, 0, W, H), nearest: false);
        var artRect = FaceRect(35, 158, 560, 336);
        Texture2D? art = SourceArtwork.ModernCard(def.Id) ?? Asset("cards", def.Id) ?? SourceArtwork.Fused(def.Id);
        if (art != null) {
            var artSpace = new Control { Name = "ArtworkDesign", Size = new Vector2(630, 880), Scale = Vector2.One * .4f, MouseFilter = Control.MouseFilterEnum.Ignore };
            root.AddChild(artSpace);
            var picture = Picture(artSpace, "Artwork", art, new Rect2(35, 158, 560, 336), nearest: true);
            // The 80 x 48 source is always shown in full at exactly 5:3.
            picture.StretchMode = TextureRect.StretchModeEnum.Scale;
            picture.ClipContents = false;
            picture.SetMeta("source_art_nearest", true);
            picture.SetMeta("source_art_crop", false);
            picture.SetMeta("source_art_original_size", art.GetSize());
        }
        int effectiveCost = cost ?? EffectiveCost(state, card);
        string costText = def.XCost == true ? "X" : effectiveCost.ToString(CultureInfo.InvariantCulture);
        Color costColor = def.XCost == true || effectiveCost == def.Cost ? new Color("f6f1d9")
            : effectiveCost < def.Cost ? new Color("9de1ba") : new Color("ffb6aa");
        int costSize = 82;
        while (costSize > 30 && (DecoFont?.GetStringSize(costText, HorizontalAlignment.Left, -1, costSize).X ?? 0) > 116) costSize--;
        AddFaceLabel(root, "Cost", costText, new Rect2(35, 27, 116, 114), costSize, costColor, DecoFont);
        int nameSize = Scaled(43, textScale);
        while (nameSize > 12 && (BodyFont?.GetStringSize(def.Name, HorizontalAlignment.Left, -1, nameSize).X ?? 0) > 392) nameSize--;
        AddFaceLabel(root, "Name", def.Name, new Rect2(177, 35, 420, 85), nameSize,
            new Color(faction == "white" ? "283035" : "fff5df"), BodyFont);
        AddFaceLabel(root, "Type", type == "reaction" ? "仕込み" : CardText.TypeJa(type), new Rect2(95, type == "spell" ? 527 : 515, 440, 54),
            type == "spell" ? 40 : 46, new Color("f6f1d9"), DecoFont);
        AddFaceLabel(root, "Rarity", rarity.ToUpperInvariant(), new Rect2(170, 840, 290, 23), 12,
            new Color(rank == 2 ? "ddbd70" : rank == 1 ? "56b69a" : "a8b8b9"), BodyFont);
        bool expanded = tile.HasMeta("source_expanded_reading") && tile.GetMeta("source_expanded_reading").AsBool();
        string markup = BodyMarkup(card, state, target, compactNotes: !expanded);
        string fullText = BodyText(card, state, target);
        var bodyRect = FaceRect(50, 577, 530, 212);
        var body = new SourceCardBody { Name = "Body", Position = bodyRect.Position, Size = bodyRect.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        int[] dividers = SemanticDividers(def);
        body.Configure(markup, BodyFont, new Color("f6f1d9"), textScale, requiresReading => {
            tile.SetMeta("source_body_truncated", body.HasMeta("source_body_truncated") && body.GetMeta("source_body_truncated").AsBool());
            tile.SetMeta("source_body_requires_enlargement", requiresReading);
        }, dividers, expanded);
        root.AddChild(body);
        if (!string.IsNullOrEmpty(badge))
            AddLabel(root, "Badge", badge, FaceRect(180, 864, 270, 14), 5, new Color("f6f1d9"), BodyFont);
        tile.SetMeta("source_body", fullText);
        tile.SetMeta("source_body_full", fullText);
        tile.SetMeta("source_body_face", Plain(markup));
        tile.SetMeta("source_body_markup", markup);
        tile.SetMeta("source_body_rect", bodyRect);
        tile.SetMeta("source_art_rect", artRect);
        tile.SetMeta("source_effective_cost", costText);
        tile.SetMeta("source_rarity_dot_count", rank + 1);
        tile.SetMeta("source_rarity", rarity);
        tile.SetMeta("source_faction", faction);
        tile.SetMeta("source_card_type", type);
        tile.SetMeta("source_semantic_dividers", string.Join(",", dividers));
        tile.SetMeta("source_card_name", def.Name);
        tile.SetMeta("source_card_badge", badge ?? "");
        tile.SetMeta("source_preview_enemy", target);
        tile.SetMeta("source_card_phone", phone);
        tile.SetMeta("source_face_enabled", face);
        tile.TooltipText = tile.IsBattleHand ? (phone ? "" : KeywordTooltip(fullText))
            : def.Name + "\n" + fullText + (string.IsNullOrWhiteSpace(badge) ? "" : "\n" + badge);
    }

    private static Label AddFaceLabel(Control root, string name, string text, Rect2 designRect, int fontSize, Color ink, Font? font)
    {
        // Rasterize glyphs at the authored scale, not at 252px followed by a
        // blurry upsample in the held/read view. The outer face API stays252x352.
        var space = new Control { Name = name + "Design", Position = designRect.Position * .4f,
            Size = designRect.Size, Scale = Vector2.One * .4f, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(space);
        return AddLabel(space, name, text, new Rect2(Vector2.Zero, designRect.Size), fontSize, ink, font);
    }

    private static Rect2 FaceRect(float x, float y, float w, float h) => new Rect2(x * .4f, y * .4f, w * .4f, h * .4f);

    private static string FaceFaction(CardDef def)
    {
        foreach (string color in new[] { "white", "red", "green", "blue", "black" })
            if (def.Color == color) return color;
        // Fusions retain their definition color when present. Neutral/status cards
        // receive a neutral dark band rather than an invented faction identity.
        return "neutral";
    }

    private static Texture2D SelectedFrame(string faction, string type, int rank)
    {
        string key = "selected_C_W09_" + faction + "_" + type + "_" + rank;
        if (Textures.TryGetValue(key, out var saved) && saved != null) return saved;
        string accent = new[] { "#a8b8b9", "#56b69a", "#ddbd70" }[rank];
        string shade = new[] { "#576e70", "#28775e", "#a0782a" }[rank];
        string dot = new[] { "#bcc8c9", "#77c6bd", "#d9b363" }[rank];
        string factionColor = faction switch { "white" => "#e8e2ce", "red" => "#994c3d", "green" => "#326447", "blue" => "#376b91", "black" => "#252429", _ => "#354d55" };
        var svg = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' width='630' height='880' viewBox='0 0 630 880'>");
        svg.Append($"<defs><linearGradient id='sheen' x1='0' y1='0' x2='0' y2='1'><stop offset='0' stop-color='#fff7e0' stop-opacity='.006'/><stop offset='.3' stop-color='#fff7e0' stop-opacity='{SourcePalette.Battle.CardSheenOpacity}'/><stop offset='.65' stop-color='#fff7e0' stop-opacity='0'/><stop offset='1' stop-color='{SourcePalette.Battle.CardShadowHex}' stop-opacity='.10'/></linearGradient></defs>");
        svg.Append($"<rect width='630' height='880' rx='3' fill='{SourcePalette.Battle.CardShellHex}'/>");
        svg.Append($"<g fill='none' stroke='{accent}' stroke-width='4'><rect x='4' y='12' width='622' height='856'/><rect x='12' y='4' width='606' height='872'/></g><rect x='17' y='17' width='596' height='846' fill='none' stroke='{shade}'/>");
        foreach (int side in new[] { 0, 1 }) foreach (int top in new[] { 0, 1 }) {
            int x = side == 0 ? 29 : 601, sx = side == 0 ? 1 : -1, y = top == 0 ? 25 : 855, sy = top == 0 ? 1 : -1;
            if (rank == 0) svg.Append($"<path d='M{x} {y+sy*17}V{y}H{x+sx*17}' fill='none' stroke='{accent}' stroke-width='3'/>");
            else if (rank == 1) svg.Append($"<path d='M{x} {y+sy*23}V{y}H{x+sx*23} M{x+sx*7} {y+sy*23}V{y+sy*7}H{x+sx*23}' fill='none' stroke='{accent}' stroke-width='3'/>");
            else svg.Append($"<path d='M{x} {y+sy*26}V{y+sy*8}L{x+sx*8} {y}H{x+sx*26}' fill='none' stroke='{accent}' stroke-width='5'/><path d='M{x+sx*9} {y+sy*9-6}l6 6-6 6-6-6z' fill='{accent}'/>");
        }
        // Faction C bands: title, art mat and bottom belt only. T08 is below T01.
        foreach (var r in new[] { new Rect2(169,29,438,118), new Rect2(24,150,582,349), new Rect2(25,806,581,34) }) {
            svg.Append(FormattableString.Invariant($"<rect x='{r.Position.X}' y='{r.Position.Y}' width='{r.Size.X}' height='{r.Size.Y}' fill='{factionColor}'/><rect x='{r.Position.X}' y='{r.Position.Y}' width='{r.Size.X}' height='{r.Size.Y}' fill='url(#sheen)'/><rect x='{r.Position.X+3.5f}' y='{r.Position.Y+3.5f}' width='{r.Size.X-7}' height='{r.Size.Y-7}' fill='none' stroke='#fff7e0' stroke-opacity='.25' stroke-width='2'/><rect x='{r.Position.X}' y='{r.Position.Y}' width='{r.Size.X}' height='{r.Size.Y}' fill='none' stroke='{SourcePalette.Battle.CardShadowHex}' stroke-opacity='.22' stroke-width='1'/>"));
        }
        svg.Append($"<path d='M32 22H154V27H159V141H154V146H32V141H27V27H32Z' fill='{accent}'/><path d='M35 26H151V31H155V137H151V142H35V137H31V31H35Z' fill='{SourcePalette.Battle.CardShellHex}'/>");
        svg.Append($"<rect x='25' y='500' width='580' height='305' fill='{SourcePalette.Battle.CardShellHex}'/><rect x='25' y='574' width='580' height='231' fill='{SourcePalette.Battle.CardBodyHex}'/>");
        if (type == "physical") svg.Append($"<path d='M55 504H575L599 528V548L575 572H55L31 548V528Z' fill='{SourcePalette.Battle.CardShellHex}' stroke='{SourcePalette.Battle.CardEdgeHex}' stroke-width='13'/><path d='M34 573V765L68 801H562L596 765V573' fill='none' stroke='{SourcePalette.Battle.CardLineHex}' stroke-width='7'/>");
        else if (type == "spell") svg.Append($"<path d='M89 573V548L150 526 238 509H392L480 526 541 548V573' fill='none' stroke='{SourcePalette.Battle.CardEdgeHex}' stroke-width='27' stroke-linejoin='round'/><path d='M34 575V773L46 793 66 801H564L584 793 596 773V575' fill='none' stroke='{SourcePalette.Battle.CardLineHex}' stroke-width='7' stroke-linejoin='round'/>");
        else if (type == "reaction") svg.Append($"<path d='M31 506H567L599 536H66Z' fill='{SourcePalette.Battle.CardShellHex}' stroke='{SourcePalette.Battle.CardEdgeHex}' stroke-width='15'/><path d='M66 536H599L564 571H31Z' fill='{SourcePalette.Battle.CardShellHex}' stroke='{SourcePalette.Battle.CardLineHex}' stroke-width='15'/><path d='M120 518H542L515 573H95Z' fill='{SourcePalette.Battle.CardShellHex}'/><path d='M34 576V801H596V576' fill='none' stroke='{SourcePalette.Battle.CardLineHex}' stroke-width='7'/>");
        else if (type == "permanent") svg.Append($"<path d='M38 522V799H592V522' fill='none' stroke='{SourcePalette.Battle.CardEdgeHex}' stroke-width='14'/><path d='M31 514H599' stroke='{SourcePalette.Battle.CardLineHex}' stroke-width='10'/><rect x='165' y='508' width='300' height='64' fill='{SourcePalette.Battle.CardShellHex}' stroke='{SourcePalette.Battle.CardEdgeHex}' stroke-width='7'/>");
        else svg.Append($"<rect x='31' y='508' width='568' height='64' fill='{SourcePalette.Battle.CardShellHex}' stroke='{SourcePalette.Battle.CardLineHex}' stroke-width='7'/><path d='M34 576V801H596V576' fill='none' stroke='{SourcePalette.Battle.CardLineHex}' stroke-width='7'/>");
        svg.Append($"<rect x='263' y='808' width='104' height='30' rx='7' fill='{SourcePalette.Battle.CardWellHex}'/><rect x='526' y='119' width='75' height='24' rx='5' fill='{SourcePalette.Battle.CardWellHex}'/>");
        for (int i = 0; i <= rank; i++) {
            float offset = i - rank / 2f;
            svg.Append(FormattableString.Invariant($"<circle cx='{315+offset*28}' cy='823' r='7' fill='{dot}'/><circle cx='{564+offset*22}' cy='131' r='6' fill='{dot}'/>"));
        }
        svg.Append("</svg>");
        using var image = new Image();
        if (image.LoadSvgFromString(svg.ToString(), 1f) != Error.Ok) throw new InvalidOperationException("Selected C frame could not render: " + key);
        return Textures[key] = ImageTexture.CreateFromImage(image);
    }

    private static int[] SemanticDividers(CardDef def)
    {
        bool old = CardText.MarkClauses;
        try {
            CardText.MarkClauses = false;
            string[] paragraphs = Plain(CardText.Body(def)).Split('\n');
            var cuts = new List<int>();
            for (int i = 0; i + 1 < paragraphs.Length; i++) {
                // Consecutive movement/addition of the same summoned card is one
                // meaning group. Cost and trailing explanations are never cut.
                bool cardGroup = false;
                if (def.Effects != null && def.Effects.Count == paragraphs.Length) {
                    DeclarativeEffect left = def.Effects[i], right = def.Effects[i + 1];
                    cardGroup = (left.Effect ?? "").StartsWith("addCardTo", StringComparison.Ordinal)
                        && (right.Effect ?? "").StartsWith("addCardTo", StringComparison.Ordinal)
                        && left.SummonId == right.SummonId && left.Trigger == right.Trigger;
                }
                if (!cardGroup) cuts.Add(i);
            }
            return cuts.ToArray();
        } finally { CardText.MarkClauses = old; }
    }

	public static int EffectiveCost(GameState? state, CardInstance card)
	{
		if (state == null)
		{
			return card.Def.Cost;
		}
		try
		{
			return Effects.EffectiveCost(state, card);
		}
		catch (Exception)
		{
			return card.Def.Cost;
		}
	}

	public static Func<int, int> MakeDamageModifier(GameState state, CardInstance card, int target = -1)
	{
		PlayerState p = state.Player;
		int grow = card.GrowBonus.GetValueOrDefault();
		bool hasTarget = target >= 0 && target < state.Enemies.Count && state.Enemies[target].Hp > 0;
		return (int baseAmount) =>
		{
			int num = baseAmount + grow;
			if (hasTarget)
			{
				try
				{
					DamageBreakdown damageBreakdown = Effects.DamageBreakdownOf(state, target, num);
					if (damageBreakdown != null && damageBreakdown.Steps.Count > 0)
					{
						int result = num;
						foreach (DamageBreakdownStep current in damageBreakdown.Steps)
						{
							string text = current.Label ?? "";
							if (text.StartsWith("敵ブロック", StringComparison.Ordinal) || text.StartsWith("貫通", StringComparison.Ordinal) || text.StartsWith("潜伏", StringComparison.Ordinal) || text.StartsWith("無形", StringComparison.Ordinal) || text.StartsWith("ターン装甲", StringComparison.Ordinal) || text.StartsWith("朧", StringComparison.Ordinal))
							{
								break;
							}
							result = current.Value;
						}
						return result;
					}
				}
				catch (Exception)
				{
				}
			}
			if (p.Growth > 0)
			{
				num += p.Growth;
			}
			if (p.Momentum > 0)
			{
				num += p.Momentum;
			}
			if (p.Weak > 0 && num > 0)
			{
				num = Math.Max(1, (int)Math.Floor((double)num * 0.75));
			}
			return num;
		};
	}

	public static string BodyText(CardInstance card, GameState? state = null, int target = -1)
	{
		return Plain(BodyMarkup(card, state, target, compactNotes: false));
	}

	public static string CardBodyText(CardInstance card, GameState? state = null, int target = -1)
	{
		return BodyText(card, state, target);
	}

    public static string BodyMarkup(CardInstance card, GameState? state = null, int target = -1, bool compactNotes = true, bool includeFixedTraits = true)
    {
        // Call the actual live text formatter. Presentation never paraphrases,
        // merges, abbreviates, or substitutes the card's original wording.
        Func<int, int>? previousModifier = CardText.DamageModifier;
        bool previousClauses = CardText.MarkClauses;
        try {
            CardText.DamageModifier = state == null ? null : MakeDamageModifier(state, card, target);
            CardText.MarkClauses = false;
            var parts = new List<string> { Plain(CardText.Body(card.Def)) };
            parts.AddRange(CardText.CostNotes(card.Def));
            var trails = CardText.TrailNotes(card.Def, compactNotes);
            if (!includeFixedTraits) trails.RemoveAll(note => note == "消滅" || note == "保持");
            parts.AddRange(trails);
            return string.Join("\n", parts);
        } finally {
            CardText.DamageModifier = previousModifier;
            CardText.MarkClauses = previousClauses;
        }
    }

	private static string Clause(string text)
	{
		return CardText.Emphasize("\ue000" + text + "\ue001", SourcePalette.InkSoft.ToHtml(includeAlpha: false));
	}

	private static string ColorTag(Color color, string text)
	{
		return "<color=#" + color.ToHtml(includeAlpha: false) + ">" + text + "</color>";
	}

	public static string Plain(string text)
	{
		return Markup.Replace(text ?? "", "").Replace('\ue000'.ToString(), "").Replace('\ue001'.ToString(), "");
	}

	public static string KeywordTooltip(string text)
	{
		List<string> lines = new List<string>();
		foreach (string term in KeywordHelp.FindIn(Plain(text)))
		{
			lines.Add(term + ": " + KeywordHelp.Terms[term]);
		}
		return string.Join("\n\n", lines);
	}

	private static List<string> EffectLines(IReadOnlyList<DeclarativeEffect>? effects, string type, Func<int, int>? modifier)
	{
		List<string> result = new List<string>();
		if (effects == null)
		{
			return result;
		}
		List<string> heads = new List<string>();
		List<string> bodies = new List<string>();
		foreach (DeclarativeEffect effect in effects)
		{
			string body = CardText.EffectLine(effect, type, hideTrigger: true);
			string full = CardText.EffectLine(effect, type);
			heads.Add(full.EndsWith(body, StringComparison.Ordinal) ? full.Substring(0, full.Length - body.Length) : "");
			if (effect.Effect == "dealDamage" && modifier != null && (effect.Trigger == null || effect.Trigger == "onPlay"))
			{
				int amount = effect.Amount.GetValueOrDefault();
				int shown = modifier(amount);
				if (shown != amount)
				{
					string token = "ダメージ" + shown.ToString(CultureInfo.InvariantCulture);
					int at = body.LastIndexOf(token, StringComparison.Ordinal);
					if (at >= 0)
					{
						body = body.Substring(0, at) + "ダメージ" + ColorTag((shown > amount) ? GoodInk : BadDown, shown.ToString(CultureInfo.InvariantCulture)) + body.Substring(at + token.Length);
					}
				}
			}
			bodies.Add(body);
		}
		string previousHead = null;
		int a = 0;
		while (a < bodies.Count)
		{
			bool folded = false;
			for (int k = 2; k <= 3; k++)
			{
				if (folded)
				{
					break;
				}
				if (a + 2 * k > bodies.Count)
				{
					break;
				}
				bool sameHead = true;
				bool allSame = true;
				for (int j = 1; j < k; j++)
				{
					sameHead &= heads[a + j] == heads[a];
					allSame &= bodies[a + j] == bodies[a];
				}
				if (!sameHead | allSame)
				{
					continue;
				}
				int repeats;
				for (repeats = 1; a + (repeats + 1) * k <= bodies.Count; repeats++)
				{
					bool match = true;
					for (int i = 0; i < k; i++)
					{
						if (heads[a + repeats * k + i] != heads[a + i] || bodies[a + repeats * k + i] != bodies[a + i])
						{
							match = false;
							break;
						}
					}
					if (!match)
					{
						break;
					}
				}
				if (repeats >= 2)
				{
					result.Add(((heads[a] == previousHead) ? "" : heads[a]) + "［" + string.Join(" → ", bodies.GetRange(a, k)) + "］×" + repeats + "回");
					previousHead = heads[a];
					a += repeats * k;
					folded = true;
				}
			}
			if (!folded)
			{
				int b;
				for (b = a; b + 1 < bodies.Count && heads[b + 1] == heads[a] && bodies[b + 1] == bodies[a]; b++)
				{
				}
				result.Add(((heads[a] == previousHead) ? "" : heads[a]) + bodies[a] + ((b > a) ? (" ×" + (b - a + 1) + "回") : ""));
				previousHead = heads[a];
				a = b + 1;
			}
		}
		return result;
	}

	internal static string Bbcode(string source, int fontSize, int minimumFontSize = 1)
	{
		StringBuilder output = new StringBuilder("[left]");
        var enlargedSizes = new Stack<bool>();
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
					if (tag == "nobr")
					{
						noBreak++;
					}
					else if (tag == "/nobr")
					{
						noBreak = Math.Max(0, noBreak - 1);
					}
					else if (tag.StartsWith("color=", StringComparison.Ordinal))
					{
						output.Append('[').Append(tag).Append(']');
					}
					else if (tag == "/color")
					{
						output.Append("[/color]");
					}
					else if (tag.StartsWith("size=", StringComparison.Ordinal))
					{
						if (float.TryParse(tag.Substring(5).TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
						{
							bool enlarged = percent > 100;
                            enlargedSizes.Push(enlarged);
                            output.Append("[font_size=").Append(Math.Max(minimumFontSize, (int)Math.Round(fontSize * Math.Min(percent, 110f) / 100f))).Append(']');
                            if (enlarged) output.Append("[b]");
						}
					}
					else if (tag == "/size")
					{
						if (enlargedSizes.Count > 0 && enlargedSizes.Pop()) output.Append("[/b]");
                        output.Append("[/font_size]");
					}
					else if (tag.StartsWith("mark=", StringComparison.Ordinal))
					{
						// Supplement text stays readable without a selection-like background.
					}
					else if (tag == "/mark")
					{
						// Matching mark close intentionally emits no visual decoration.
					}
					i = end;
					continue;
				}
			}
			switch (ch)
			{
			case '[':
				output.Append("[lb]");
				break;
			case ']':
				output.Append("[rb]");
				break;
			default:
				output.Append(ch);
				break;
			case '\ue000':
			case '\ue001':
				break;
			}
			if (noBreak > 0 && ch != '\n')
			{
				output.Append('\u2060');
			}
		}
		return output.Append("[/left]").ToString();
	}

	private static int Scaled(int size, float scale)
	{
		return Math.Max(1, (int)Math.Round((float)size * Math.Clamp(scale, 1f, 1.3f)));
	}

	private static Font? LoadFont(string name)
	{
		if (Fonts.TryGetValue(name, out Font font))
		{
			return font;
		}
		string path = "res://assets/Fonts/" + name;
		return Fonts[name] = (ResourceLoader.Exists(path) ? GD.Load<Font>(path) : null);
	}

	private static Color TypeColor(string type)
	{
		return new Color(type switch
		{
			"spell" => "bca6d6", 
			"reaction" => "7ab8b0", 
			"permanent" => "b3b67a", 
			_ => "c9a982", 
		});
	}

	private static Texture2D? Asset(string category, string? id)
	{
		if (string.IsNullOrEmpty(id) || id.Contains('/') || id.Contains('\\') || id.Contains("..", StringComparison.Ordinal))
		{
			return null;
		}
		string key = "asset:" + category + "/" + id;
		if (Textures.TryGetValue(key, out Texture2D found))
		{
			return found;
		}
		string path = "res://assets/Art/" + category + "/" + id + ".png";
		Texture2D texture = null;
		if (ResourceLoader.Exists(path))
		{
			texture = GD.Load<Texture2D>(path);
		}
		else if (FileAccess.FileExists(path))
		{
			using Image image = Image.LoadFromFile(path);
			if (!image.IsEmpty())
			{
				texture = ImageTexture.CreateFromImage(image);
			}
		}
		return Textures[key] = texture;
	}

	private static TextureRect Picture(Control parent, string name, Texture2D texture, Rect2 rect, bool nearest = true)
	{
		TextureRect picture = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Name = name,
			Texture = texture,
			Position = rect.Position,
			Size = rect.Size,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = (CanvasItem.TextureFilterEnum)(nearest ? 1 : 2),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		parent.AddChild(picture, forceReadableName: false, Node.InternalMode.Disabled);
		return picture;
	}

	private static void Rect(Control parent, string name, Rect2 rect, Color color)
	{
		parent.AddChild(new ColorRect
		{
			Name = name,
			Position = rect.Position,
			Size = rect.Size,
			Color = color,
			MouseFilter = Control.MouseFilterEnum.Ignore
		}, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private static Label AddLabel(Control parent, string name, string text, Rect2 rect, int size, Color color, Font? font, HorizontalAlignment align = HorizontalAlignment.Center)
	{
		SourceHudLabel label = new SourceHudLabel
		{
			Name = name,
			Text = text,
			SourceTextRect = rect,
			HorizontalAlignment = align,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Off,
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		if (font != null)
		{
			label.AddThemeFontOverride("font", font);
		}
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		label.Position = rect.Position;
		label.Size = rect.Size;
		parent.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);
		label.RefreshSourceAlignment();
		return label;
	}

	private static Texture2D MakeTexture(string key, int w, int h, Func<int, int, Color> pixel)
	{
		if (Textures.TryGetValue(key, out Texture2D existing) && existing != null)
		{
			return existing;
		}
		using Image image = Image.CreateEmpty(w, h, useMipmaps: false, Image.Format.Rgba8);
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				image.SetPixel(x, y, pixel(x, y));
			}
		}
		ImageTexture texture = ImageTexture.CreateFromImage(image);
		texture.ResourceName = key;
		Textures[key] = texture;
		return texture;
	}

	private static Texture2D Paper(string rarity)
	{
		Texture2D authored = Asset("ui", "paper_card_" + rarity);
		if (authored != null)
		{
			return authored;
		}
		Color edge;
		if (rarity == "rare")
		{
			edge = SourcePalette.Brass;
		}
		else
		{
			edge = ((rarity == "uncommon") ? new Color("6f95b8") : new Color(SourcePalette.Ink, 0.5f));
		}
		float width = ((rarity == "common") ? 1 : 2);
		return MakeTexture("paper:" + rarity, 52, 52, (int x, int y) =>
		{
			float val = Math.Abs((float)x + 0.5f - 26f) - 12f;
			float num = Math.Abs((float)y + 0.5f - 26f) - 12f;
			float num2 = 0f - (MathF.Sqrt(Math.Max(val, 0f) * Math.Max(val, 0f) + Math.Max(num, 0f) * Math.Max(num, 0f)) + Math.Min(Math.Max(val, num), 0f) - 14f);
			if (num2 < 0f)
			{
				return Colors.Transparent;
			}
			Color result = ((num2 < width) ? edge : SourcePalette.Paper);
			if (num2 < 1f)
			{
				result.A *= Math.Clamp(num2 + 0.5f, 0f, 1f);
			}
			return result;
		});
	}

	private static Texture2D Grain()
	{
		Random random = new Random(20260907);
		return MakeTexture("paper-grain", 128, 128, (int x, int y) =>
		{
			double num = random.NextDouble();
			return new Color(SourcePalette.Ink, (num < 0.55) ? 0f : ((float)((num - 0.55) / 0.45) * 0.09f));
		});
	}

	private static Texture2D Ribbon(Color color)
    {
        return MakeTexture("flat-category:" + color.ToHtml(), 150, 26, (int x, int y) =>
            color.Lerp(SourcePalette.Paper, .35f));
    }

	private static Texture2D Orb(Color color)
	{
		return MakeTexture("orb:" + color.ToHtml(), 52, 52, (int x, int y) =>
		{
			float num = (float)x + 0.5f - 26f;
			float num2 = (float)y + 0.5f - 26f;
			float num3 = MathF.Sqrt(num * num + num2 * num2);
			Color result = ((num3 < 20f) ? color.Lerp(Colors.White, 0.35f).Lerp(color.Lerp(Colors.Black, 0.18f), Math.Clamp((num + num2) / 40f + 0.5f, 0f, 1f)) : ((num3 < 22f) ? SourcePalette.Ink : ((num3 < 24f) ? SourcePalette.Paper : ((num3 < 25f) ? new Color(SourcePalette.Ink, 0.5f) : Colors.Transparent))));
			if (num3 >= 24f && num3 < 25.5f)
			{
				result.A *= Math.Clamp(25.5f - num3, 0f, 1f);
			}
			return result;
		});
	}

	private static Texture2D? MutedOrb(Texture2D source)
	{
		if (Textures.TryGetValue("cost-orb-muted", out Texture2D saved))
		{
			return saved;
		}
		Texture2D texture2D;
		try
		{
			using Image shared = source.GetImage();
			if (shared == null || shared.IsEmpty())
			{
				texture2D = (Textures["cost-orb-muted"] = null);
				texture2D = texture2D;
			}
			else
			{
				Image image = (Image)shared.Duplicate();
				try
				{
					if (image.IsCompressed())
					{
						image.Decompress();
					}
					texture2D = MakeTexture("cost-orb-muted", image.GetWidth(), image.GetHeight(), (int x, int y) =>
					{
						Color pixel = image.GetPixel(x, y);
						float num = 0.3f * pixel.R + 0.59f * pixel.G + 0.11f * pixel.B;
						Color color = pixel.Lerp(new Color(num, num, num, pixel.A), 0.7f).Lerp(new Color(SourcePalette.PaperDim, pixel.A), 0.15f);
						return new Color(MathF.Round(color.R * 255f) / 255f, MathF.Round(color.G * 255f) / 255f, MathF.Round(color.B * 255f) / 255f, pixel.A);
					});
				}
				finally
				{
					if (image != null)
					{
						((IDisposable)image).Dispose();
					}
				}
			}
		}
		catch (Exception)
		{
			Texture2D texture2D3 = (Textures["cost-orb-muted"] = null);
			texture2D = texture2D3;
		}
		return texture2D;
	}

	private static Texture2D Gem(string rarity)
	{
		string code;
		if (rarity == "rare")
		{
			code = "e0b84a";
		}
		else
		{
			code = ((rarity == "uncommon") ? "5aa0e0" : "9aa39c");
		}
		Color color = new Color(code);
		return MakeTexture("gem:" + rarity, 8, 8, (int x, int y) =>
		{
			int num = 7 - y;
			int num2 = Math.Abs(x - 3) + Math.Abs(num - 3) + ((x > 3) ? 1 : 0) + ((num > 3) ? 1 : 0);
			Color result = ((num2 <= 3) ? ((num2 != 3) ? ((x >= 4 || num <= 3) ? color : color.Lerp(Colors.White, 0.4f)) : color.Lerp(Colors.Black, 0.5f)) : Colors.Transparent);
			return result;
		});
	}

	private static Texture2D Crest(string type)
	{
		string[] rows = type switch
		{
			"spell" => new string[16]
			{
				".......--.......", "......-##-......", "...-..-##-..-...", "..-#-.-##-.-#-..", "...-#--##--#-...", "....-######-....", ".....-####-.....", "-######++######-", "-######++######-", ".....-####-.....",
				"....-######-....", "...-#--##--#-...", "..-#-.-##-.-#-..", "...-..-##-..-...", "......-##-......", ".......--......."
			}, 
			"reaction" => new string[16]
			{
				"................", "................", "................", ".....------.....", "...--######--...", "..-###----###-..", ".-##--####--##-.", "-##-##-##-##-##-", "-##-##-++-##-##-", ".-##--####--##-.",
				"..-###----###-..", "...--######--...", ".....------.....", "................", "................", "................"
			}, 
			"permanent" => new string[16]
			{
				".......--.......", "......-##-......", ".....-####-.....", "....-######-....", "...-########-...", "..-##########-..", ".-############-.", "-######++######-", "..-----##-----..", "......-##-......",
				"......-##-......", "......-##-......", ".....-####-.....", "....-######-....", "...-########-...", "...----------..."
			}, 
			_ => new string[16]
			{
				"-..............-", "#-............-#", "-#-..........-#-", ".-#-........-#-.", "..-#-......-#-..", "...-#-....-#-...", "....-#-..-#-....", ".....-#--#-.....", ".....-#--#-.....", "....-#-..-#-....",
				"...-#-....-#-...", "..-#-......-#-..", ".-#-........-#-.", "-#-..........-#-", "#-............-#", "-..............-"
			}, 
		};
		return MakeTexture("crest:" + type, 16, 16, (int x, int y) => rows[y][x] switch
		{
			'#' => SourcePalette.Paper2, 
			'+' => SourcePalette.Paper, 
			'-' => SourcePalette.Ink, 
			_ => Colors.Transparent, 
		});
	}
}
