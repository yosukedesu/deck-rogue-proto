using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public static class SourceBattleDecor
{
	private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

	public static TextureRect Icon(Control parent, string name, Texture2D art, Rect2 rect, Color? tint = null)
	{
		TextureRect p = new TextureRect
		{
			Name = name,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = art,
			Position = rect.Position,
			Size = rect.Size,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Modulate = (tint ?? Colors.White)
		};
		parent.AddChild(p, forceReadableName: false, Node.InternalMode.Disabled);
		return p;
	}

	public static SourceHudLabel Text(Control parent, string name, string text, Rect2 rect, int size, Color ink, bool deco = false, bool bold = false)
	{
		SourceHudLabel l = new SourceHudLabel
		{
			Name = name,
			Text = text,
			SourceTextRect = rect,
			HorizontalAlignment = HorizontalAlignment.Left,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
        Font font = deco ? SourceRunUiDecor.Display : bold ? SourceRunUiDecor.Action : SourceRunUiDecor.Body;
		l.AddThemeFontOverride("font", font);
		l.AddThemeFontSizeOverride("font_size", size);
		l.AddThemeColorOverride("font_color", ink);
		parent.AddChild(l, forceReadableName: false, Node.InternalMode.Disabled);
		return l;
	}

	public static StyleBoxFlat Box(Color fill, Color edge, float unit, float line = 2f)
	{
		int border = Math.Max(1, (int)Math.Round(line * unit));
		int radius = (int)Math.Round(8f * unit);
		return new StyleBoxFlat
		{
			BgColor = fill,
			BorderColor = edge,
			BorderWidthLeft = border,
			BorderWidthRight = border,
			BorderWidthTop = border,
			BorderWidthBottom = border,
			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius,
			ContentMarginLeft = 0f,
			ContentMarginRight = 0f,
			ContentMarginTop = 0f,
			ContentMarginBottom = 0f
		};
	}

	public static void Shield(Control body, SourceHealthBar bar, Vector2 origin, float width, int block, float unit, float textScale, bool phone, bool enemy = false, bool compact = false)
	{
		float badgeHeight = compact ? (phone?18f:20f)*textScale*unit : (enemy ? (20f * textScale + (float)(phone ? 4 : 8)) : (22f * textScale + 8f)) * unit;
		float badgeWidth = badgeHeight + 16f * unit;
		float shift = ((block > 0) ? (badgeWidth - 6f * unit) : 0f);
		bar.Position = origin + new Vector2(shift, 0f);
		bar.Size = new Vector2(Math.Max(8f * unit, width - shift), bar.Size.Y);
		Control old = body.GetNodeOrNull<Control>(string.Concat(bar.Name, "Shield"));
		string key = block + "|" + unit + "|" + textScale + "|" + compact;
		if (old == null || !(old.GetMeta("source_shield_signature", "").AsString() == key))
		{
			if (old != null)
			{
				body.RemoveChild(old);
				old.QueueFree();
			}
			if (block > 0)
			{
				Panel badge = new Panel
				{
					Name = string.Concat(bar.Name, "Shield"),
					Position = origin - new Vector2(2f, compact?0f:4f) * unit,
					Size = new Vector2(badgeWidth, badgeHeight),
					MouseFilter = Control.MouseFilterEnum.Ignore
				};
				badge.SetMeta("source_shield_signature", key);
				badge.SetMeta("displayed_block", block);
				badge.AddThemeStyleboxOverride("panel", Box(new Color("1f344c"), new Color("2b486a"), unit, 1.5f));
				body.AddChild(badge, forceReadableName: false, Node.InternalMode.Disabled);
				int size = (int)Math.Round((float)(phone ? 15 : 16) * unit * textScale);
				float tw = SourceRunUiDecor.Display.GetStringSize(block.ToString(), HorizontalAlignment.Left, -1f, size, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
				float start = (badge.Size.X - 16f * unit - unit - tw) / 2f;
				Icon(badge, "ShieldIcon", SourceIntentGlyphs.Mono("shield"), new Rect2(start, (badgeHeight - 16f * unit) / 2f, 16f * unit, 16f * unit), new Color("a8c9ea"));
				Text(badge, "ShieldCount", block.ToString(), new Rect2(start + 17f * unit, 0f, tw, badgeHeight), size, new Color("a8c9ea"), deco: true);
			}
		}
	}

	public static void LedgerEdge(PanelContainer panel, bool selected, float unit)
	{
		if (panel.GetMeta("source_ledger_skin_unit", -1f).AsSingle() != unit)
		{
			panel.AddThemeStyleboxOverride("panel", Box(SourcePalette.Battle.Night, SourcePalette.Battle.Edge, unit));
			panel.SetMeta("source_ledger_skin_unit", unit);
		}
		if (panel.GetChildCount() != 0 && panel.GetChild(0) is Control body)
		{
			Panel edge = body.GetNodeOrNull<Panel>("SelectedOuterEdge");
			if (edge == null)
			{
				edge = new Panel
				{
					Name = "SelectedOuterEdge",
					ZIndex = -1,
					MouseFilter = Control.MouseFilterEnum.Ignore
				};
				body.AddChild(edge, forceReadableName: false, Node.InternalMode.Disabled);
			}
			edge.Position = new Vector2(-3f, -3f) * unit;
			edge.Size = panel.Size + Vector2.One * 6f * unit;
			edge.Visible = selected;
			if (edge.GetMeta("source_edge_unit", -1f).AsSingle() != unit)
			{
				edge.AddThemeStyleboxOverride("panel", Box(Colors.Transparent, SourcePalette.Brass, unit, 3f));
				edge.SetMeta("source_edge_unit", unit);
			}
		}
	}

	public static void Pile(Button button, string kind, string label, int count, float unit, float textScale, bool phone)
	{
		button.Text = "";
		string[] array = new string[6] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" };
		foreach (string state in array)
		{
			button.AddThemeStyleboxOverride(state, Box(SourcePalette.Battle.Night, SourcePalette.Battle.Edge, unit));
		}
		button.RotationDegrees = ((!(kind == "discard")) ? 1 : (-1));
		button.PivotOffset = button.Size / 2f;
		Control body = new Control
		{
			Name = "PileDetails",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		button.AddChild(body, forceReadableName: false, Node.InternalMode.Disabled);
		float cw = (phone ? 24 : 20);
		float ch = (phone ? 34 : 28);
		float dx = (phone ? 5 : 4);
		float dy = (phone ? 4 : 3);
		float glyphWidth = cw + dx + 2f;
		if (kind == "exhaust")
		{
			Icon(body, "ExhaustIcon", GD.Load<Texture2D>("res://assets/Art/icons/exhaust.png"), new Rect2(12f * unit, (button.Size.Y - 32f * unit) / 2f, 32f * unit, 32f * unit));
			glyphWidth = 32f;
		}
		else
		{
			Control host = new Control
			{
				Name = "PileGlyph",
				Position = new Vector2(12f * unit, (button.Size.Y - (ch + dy + 2f) * unit) / 2f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			body.AddChild(host, forceReadableName: false, Node.InternalMode.Disabled);
			int n = ((count < 2) ? 1 : 2);
			bool back = kind == "draw";
			bool empty = count <= 0;
			for (int j = 0; j < n; j++)
			{
				bool front = j == n - 1;
				float x;
				if (back)
				{
					if (n == 1)
					{
						x = dx / 2f + 1f;
					}
					else
					{
						x = ((j == 0) ? 1f : (1f + dx));
					}
				}
				else if (n == 1)
				{
					x = dx / 2f + 1f;
				}
				else
				{
					x = ((j == 0) ? 0f : (dx + 1f));
				}
				float y;
				if (back)
				{
					if (n == 1)
					{
						y = dy / 2f + 1f;
					}
					else
					{
						y = ((j == 0) ? (1f + dy) : 1f);
					}
				}
				else
				{
					y = dy / 2f + 1f;
				}
				Control card = new Control
				{
					Name = "MiniCard_" + j,
					Position = new Vector2(x, ch + dy + 2f - y - ch) * unit,
					Size = new Vector2(cw, ch) * unit,
					RotationDegrees = ((!back) ? ((n == 1) ? (-6) : ((j == 0) ? 8 : (-6))) : 0),
					MouseFilter = Control.MouseFilterEnum.Ignore
				};
				host.AddChild(card, forceReadableName: false, Node.InternalMode.Disabled);
				if (!empty)
				{
					Fill(card, new Rect2(Vector2.Zero, card.Size), back ? SourcePalette.Battle.Night : SourcePalette.Paper3);
				}
				Color ink;
				if (back)
				{
					ink = SourcePalette.Brass;
				}
				else
				{
					ink = (empty ? SourcePalette.InkSoft : SourcePalette.Ink);
				}
				float inset = ((back && !empty) ? (2f * unit) : 0f);
				Rect2 r = new Rect2(Vector2.One * inset, card.Size - Vector2.One * 2f * inset);
				Frame(card, r, ink, unit);
				if (!empty & front)
				{
					if (back)
					{
						Fill(card, new Rect2(card.Size / 2f - Vector2.One * 2.5f * unit, Vector2.One * 5f * unit), SourcePalette.Brass);
					}
					else
					{
						Fill(card, new Rect2(3f * unit, ch * 0.18f * unit, (cw - 6f) * unit, ch * 0.4f * unit), SourcePalette.Battle.Window);
					}
				}
			}
		}
		int fs = (int)Math.Round((float)(phone ? 19 : 17) * unit * textScale);
		int ls = (int)Math.Round((float)(phone ? 15 : 13) * unit * textScale);
		float x2 = (12f + glyphWidth + 6f) * unit;
		float tw = Text(body, "PileCount", count.ToString(), new Rect2(x2, 0f, 60f * unit, button.Size.Y), fs, SourcePalette.Paper, deco: true).GetThemeFont("font").GetStringSize(count.ToString(), HorizontalAlignment.Left, -1f, fs, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
		Text(body, "PileName", label, new Rect2(x2 + tw + 6f * unit, 0f, Math.Max(0f, button.Size.X - x2 - tw - 12f * unit), button.Size.Y), ls, SourcePalette.PaperDim);
	}

	private static void Fill(Control parent, Rect2 rect, Color color)
	{
		parent.AddChild(new ColorRect
		{
			Color = color,
			Position = rect.Position,
			Size = rect.Size,
			MouseFilter = Control.MouseFilterEnum.Ignore
		}, forceReadableName: false, Node.InternalMode.Disabled);
	}

	private static void Frame(Control p, Rect2 r, Color c, float u)
	{
		Fill(p, new Rect2(r.Position, new Vector2(r.Size.X, u)), c);
		Fill(p, new Rect2(r.Position.X, r.End.Y - u, r.Size.X, u), c);
		Fill(p, new Rect2(r.Position, new Vector2(u, r.Size.Y)), c);
		Fill(p, new Rect2(r.End.X - u, r.Position.Y, u, r.Size.Y), c);
	}

	public static void Pocket(Control parent, float width, float height, float unit)
	{
		string key = $"pocket:{width}:{height}:{unit}";
		if (!Cache.TryGetValue(key, out var texture))
		{
			float k = MathF.Round(Math.Clamp(unit, 1f, 4f) * 20f) / 20f;
			int tw = (int)MathF.Ceiling(width * k);
			int th = (int)MathF.Ceiling(height * k);
			float lw = 2f * k;
			float r = 6f * k;
			float dash = 4f * k;
			float gap = 3f * k;
			float hx = (float)tw / 2f;
			float hy = (float)th / 2f;
			float rc = Math.Max(0.5f, r - lw / 2f);
			float sx = Math.Max(0f, hx - r);
			float sy = Math.Max(0f, hy - r);
			float wc = 2f * sx;
			float hc = 2f * sy;
			float arc = (float)Math.PI / 2f * rc;
			float perim = 2f * wc + 2f * hc + 4f * arc;
			float period = perim / Math.Max(4f, MathF.Round(perim / (dash + gap)));
			float on = period * dash / (dash + gap);
			Image image = Image.CreateEmpty(tw, th, useMipmaps: false, Image.Format.Rgba8);
			for (int y = 0; y < th; y++)
			{
				for (int x = 0; x < tw; x++)
				{
					float cx = (float)x + 0.5f - hx;
					float cy = (float)y + 0.5f - hy;
					float qx = Math.Abs(cx) - sx;
					float qy = Math.Abs(cy) - sy;
					float d = r - MathF.Sqrt(Math.Max(qx, 0f) * Math.Max(qx, 0f) + Math.Max(qy, 0f) * Math.Max(qy, 0f)) - Math.Min(Math.Max(qx, qy), 0f);
					if (d < -0.5f)
					{
						continue;
					}
					float ea = Math.Clamp(d + 0.5f, 0f, 1f);
					float cov = ea * Math.Clamp(lw - d + 0.5f, 0f, 1f);
					if (cov > 0f)
					{
						float pos;
						if (!(qx > 0f) || !(qy > 0f))
						{
							if (hy - Math.Abs(cy) < hx - Math.Abs(cx))
							{
								pos = ((cy > 0f) ? (cx + sx) : (2f * arc + wc + hc + sx - cx));
							}
							else
							{
								pos = ((cx > 0f) ? (wc + arc + sy - cy) : (2f * wc + 3f * arc + hc + cy + sy));
							}
						}
						else
						{
							float phi = MathF.Atan2(cy - ((cy > 0f) ? sy : (0f - sy)), cx - ((cx > 0f) ? sx : (0f - sx)));
							if (cx > 0f && cy > 0f)
							{
								pos = wc + ((float)Math.PI / 2f - phi) * rc;
							}
							else if (cx > 0f)
							{
								pos = wc + arc + hc - phi * rc;
							}
							else if (cy < 0f)
							{
								if (phi > 0f)
								{
									phi -= (float)Math.PI * 2f;
								}
								pos = 2f * wc + 2f * arc + hc + (-(float)Math.PI / 2f - phi) * rc;
							}
							else
							{
								pos = 2f * wc + 3f * arc + 2f * hc + ((float)Math.PI - phi) * rc;
							}
						}
						float m = pos % period;
						if (m < 0f)
						{
							m += period;
						}
						cov *= Math.Clamp(Math.Min(m + 0.5f, on - m + 0.5f), 0f, 1f);
					}
					float la = cov * 0.6f;
					float fa = ((d >= 0f) ? (0.07f * ea) : 0f);
					Color c = SourcePalette.PaperDim;
					c.A = la + (1f - la) * fa;
					image.SetPixel(x, th - 1 - y, c);
				}
			}
			Texture2D texture2D = (Cache[key] = ImageTexture.CreateFromImage(image));
			texture = texture2D;
		}
		Icon(parent, "DashedPocket", texture, new Rect2(0f, 0f, width * unit, height * unit)).TextureFilter = CanvasItem.TextureFilterEnum.Linear;
	}

	public static IReadOnlyList<(string Icon, string Text)> Resources(GameState st)
	{
		PlayerState p = st.Player;
		List<(string, string)> r = new List<(string, string)>();
		int? playCap = st.PlayCap;
		if (playCap.HasValue)
		{
			int cap = playCap.GetValueOrDefault();
			r.Add((null, "首輪 あと" + Math.Max(0, cap - p.PlaysThisTurn.GetValueOrDefault()) + "枚"));
		}
		if (p.Growth > 0)
		{
			r.Add(("growth", "成長 " + p.Growth));
		}
		if (p.Momentum > 0)
		{
			r.Add(("momentum", "勢い " + p.Momentum));
		}
		if (p.Aether > 0)
		{
			r.Add(("energy", "霊気 " + p.Aether));
		}
		if (p.SparksPlayedThisCombat > 0)
		{
			r.Add(("draw", "火種 " + p.SparksPlayedThisCombat + "枚"));
		}
		if (p.NextCardDiscount > 0)
		{
			r.Add(("energy", "次のカード -" + p.NextCardDiscount));
		}
		if (p.SpellEchoes > 0)
		{
			r.Add(("draw", "反復 " + p.SpellEchoes));
		}
		if (p.Weak > 0)
		{
			r.Add(("weak", "弱体 " + p.Weak + "T"));
		}
		if (p.Vulnerable > 0)
		{
			r.Add(("exposed", "脆弱 " + p.Vulnerable + "T"));
		}
		if (p.Frail > 0)
		{
			r.Add(("frail", "虚弱 " + p.Frail + "T"));
		}
		if (p.Restrain > 0)
		{
			r.Add((null, "拘束 " + p.Restrain + "T"));
		}
		if (p.Mist > 0)
		{
			r.Add(("draw", "霞み " + p.Mist + "T"));
		}
		if (p.Slow > 0)
		{
			r.Add(("exposed", "重り " + p.Slow + "T"));
		}
		return r;
	}
}
