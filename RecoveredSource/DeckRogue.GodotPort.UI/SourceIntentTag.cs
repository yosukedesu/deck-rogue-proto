using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DeckRogue.GodotPort.UI;

public static class SourceIntentTag
{
	private static readonly Color NightHi = SourcePalette.Battle.Raised;

	private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

	private static Font _numberFont;

	private static Font _riderFont;

    // Constrain only previously overflowing compact tags. Normal tags remain byte-for-byte
    // equivalent in layout. Decorative text wraps; game intent values are never changed.
    private static float LineWidth(Font font,string text,int size) => text.Split('\n').Max(line=>font.GetStringSize(line,HorizontalAlignment.Left,-1f,size).X);
    private static string WrapText(Font font,string text,int size,float width)
    {
        var lines=new List<string>();
        foreach(var original in text.Split('\n'))
        {
            string line="";
            foreach(char c in original)
            {
                if(line.Length>0 && font.GetStringSize(line+c,HorizontalAlignment.Left,-1f,size).X>width){lines.Add(line);line="";}
                line+=c;
            }
            lines.Add(line);
        }
        return string.Join("\n",lines);
    }
    private static float RiderWidth(SourceIntentLayout.Rider r,Font font,int size,float unit) => LineWidth(font,r.Text,size)+(r.Icon==null?8f:25f)*unit;
    private static float RiderHeight(SourceIntentLayout.Rider r,float unit,float ts) => r.Text.Contains('\n') ? SourceRunUiDecor.Action.GetHeight((int)Math.Round(15f*unit*ts))*r.Text.Split('\n').Length+4f*unit : (18f*ts+2f)*unit;
    private static List<(int Start,int End,float Width,float Height)> RiderRows(IReadOnlyList<SourceIntentLayout.Rider> riders,float[] widths,float unit,float ts,float available)
    {
        var rows=new List<(int,int,float,float)>();int start=0;float width=0,height=0;
        for(int i=0;i<riders.Count;i++)
        {
            float next=width+(i>start?4f*unit:0f)+widths[i];
            if(i>start && next>available+.01f){rows.Add((start,i,width,height));start=i;width=0;height=0;next=widths[i];}
            width=next;height=Math.Max(height,RiderHeight(riders[i],unit,ts));
        }
        if(start<riders.Count)rows.Add((start,riders.Count,width,height));return rows;
    }
    public static SourceIntentLayout.Spec ConstrainCompact(SourceIntentLayout.Spec spec,float unit,bool phone,float textScale,float maxWidth)
    {
        float cap=Math.Max(72f,maxWidth);if(spec.Width<=cap+.01f)return spec;
        Font font=SourceRunUiDecor.Action;int rfs=(int)Math.Round(15f*unit*textScale);
        var riders=spec.Riders.Select(r=>r with{Text=WrapText(font,r.Text,rfs,Math.Max(1f,(cap-16f-(r.Icon==null?8f:25f))*unit))}).ToArray();
        float[] widths=riders.Select(r=>RiderWidth(r,font,rfs,unit)).ToArray();
        var rows=RiderRows(riders,widths,unit,textScale,(cap-16f)*unit);
        float extra=rows.Sum(r=>r.Height)/unit+Math.Max(0,rows.Count-1)*4f+2f;
        Font mainFont=SourceRunUiDecor.Display;int fs=(int)Math.Round(spec.FontSize*unit*textScale);
        string caption=WrapText(mainFont,spec.Caption,fs,Math.Max(1f,(cap-16f-spec.IconSize-(phone?6f:10f))*unit));
        float mainHeight=caption.Contains('\n')?Math.Max(spec.RowHeight,mainFont.GetHeight(fs)*caption.Split('\n').Length/unit+4f):spec.RowHeight;
        return spec with{Caption=caption,Stack=riders.Length>0,Width=cap,RowHeight=mainHeight,Height=mainHeight+(riders.Length>0?extra:0f),Riders=riders};
    }

	public static void Update(PanelContainer panel, Label number, SourceIntentLayout.Spec spec, float unit, bool phone, float textScale, bool compact = false)
	{
		if (spec == null)
		{
			panel.Visible = false;
			number.Text = "";
			return;
		}
		panel.Visible = true;
		string signature = spec.Kind + "|" + spec.Caption + "|" + spec.Minor + "|" + spec.Stack + "|" + spec.Width + "|" + spec.Height + "|" + unit + "|" + textScale + "|" + compact + "|" + string.Join("|", spec.Riders.Select((SourceIntentLayout.Rider rider) => rider.ToString()));
		number.Text = spec.Caption;
		if (panel.GetMeta("source_intent_signature", "").AsString() == signature)
		{
			return;
		}
		panel.SetMeta("source_intent_signature", signature);
		if (number.GetParent() != null)
		{
			number.GetParent().RemoveChild(number);
		}
		foreach (Node child in panel.GetChildren())
		{
			panel.RemoveChild(child);
			child.QueueFree();
		}
		panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		panel.Size = new Vector2(spec.Width, spec.Height) * unit;
		panel.SetMeta("source_intent_width", panel.Size.X);
		panel.SetMeta("source_intent_height", panel.Size.Y);
		panel.SetMeta("source_intent_minor", spec.Minor);
		panel.SetMeta("source_intent_stack", spec.Stack);
		panel.SetMeta("source_intent_riders", spec.Riders.Count);
		panel.SetMeta("source_intent_unit", unit);
		Control body = new Control
		{
			Name = "IntentParts",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		panel.AddChild(body, forceReadableName: false, Node.InternalMode.Disabled);
		Sheet(body, "IntentEdge", new Rect2(-3f * unit, -3f * unit, (spec.Width + 6f) * unit, (spec.Height + 6f) * unit), SourcePalette.Brass, SourcePalette.Brass, 2f, unit).Visible = false;
		Sheet(body, "IntentPaper", new Rect2(Vector2.Zero, panel.Size), spec.Minor ? SourcePalette.Battle.Night : NightHi, spec.Minor ? SourcePalette.Battle.Edge : SourcePalette.Paper2, 2f, unit);
		Picture(body, "IntentTail", Tail(spec.Minor), new Rect2((spec.Width / 2f - 7f) * unit, (spec.Height - 3f) * unit, 14f * unit, 14f * unit), nearest: false);
		if (_numberFont == null)
		{
			_numberFont = SourceRunUiDecor.Display;
		}
		if (_riderFont == null)
		{
			_riderFont = SourceRunUiDecor.Action;
		}
		int fs = (int)Math.Round((float)spec.FontSize * unit * textScale);
		int rfs = (int)Math.Round(15f * unit * textScale);
		number.AddThemeFontSizeOverride("font_size", fs);
		SourceHudLabel.NightHalo(number, _numberFont, unit, textScale, 0f, deferSmallUnderlay: true);
		StringName name = "font_color";
		string kind = spec.Kind;
		bool flag = ((kind == "buff" || kind == "rally") ? true : false);
		number.AddThemeColorOverride(name, flag ? SourcePalette.BrassLight : SourcePalette.Paper);
		number.HorizontalAlignment = HorizontalAlignment.Left;
		number.VerticalAlignment = VerticalAlignment.Center;
		number.MouseFilter = Control.MouseFilterEnum.Ignore;
		body.AddChild(number, forceReadableName: false, Node.InternalMode.Disabled);
		float textWidth = LineWidth(number.GetThemeFont("font"),spec.Caption,fs);
		float gap = (float)(phone ? 6 : 10) * unit;
		float icon = spec.IconSize * unit;
		float rowHeight = spec.RowHeight * unit;
		float[] riderWidths = spec.Riders.Select((SourceIntentLayout.Rider rider) => LineWidth(_riderFont,rider.Text,rfs) + (float)((rider.Icon == null) ? 8 : 25) * unit).ToArray();
		float ridersWidth = riderWidths.Sum() + (float)(Math.Max(0, riderWidths.Length - 1) * (spec.Stack ? 4 : (phone ? 6 : 10))) * unit;
		float rowWidth = icon + gap + textWidth + ((!spec.Stack && riderWidths.Length != 0) ? (gap + ridersWidth) : 0f);
		float x = (panel.Size.X - rowWidth) / 2f;
		Texture2D art = Load("intent_" + spec.Kind);
		if (art == null)
		{
			art = SourceIntentGlyphs.Mono(FallbackIcon(spec.Kind));
		}
		Picture(body, "IntentIcon", art, new Rect2(x, (rowHeight - icon) / 2f, icon, icon), nearest: true);
		Rect2 textRect = new Rect2(x + icon + gap, 0f, Math.Max(14f * unit, textWidth), rowHeight);
        bool wrappedCaption=compact && spec.Caption.Contains('\n');number.Visible=!wrappedCaption;
        if(wrappedCaption)
        {
            var multi=new Label{Name="IntentNumberWrapped",Text=spec.Caption,Position=textRect.Position,Size=textRect.Size,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore};
            multi.AddThemeFontOverride("font",number.GetThemeFont("font"));multi.AddThemeFontSizeOverride("font_size",fs);multi.AddThemeColorOverride("font_color",number.GetThemeColor("font_color"));body.AddChild(multi);
        }
		if (!wrappedCaption && number is SourceHudLabel precise)
		{
			precise.FollowParentRect = false;
			precise.SourceTextRect = textRect;
			precise.RefreshSourceAlignment();
		}
		else
		{
			number.Position = textRect.Position;
			number.Size = textRect.Size;
		}
		float rx = (spec.Stack ? ((panel.Size.X - ridersWidth) / 2f) : (x + icon + gap + textWidth + gap));
		float ph = (compact ? 18f*textScale+2f : (phone?22f:24f)*textScale)*unit;
		float ry = (spec.Stack ? (panel.Size.Y - ((compact?2f:6f*textScale)*unit + ph)) : ((rowHeight - ph) / 2f));
        var compactRows=compact && spec.Stack ? RiderRows(spec.Riders,riderWidths,unit,textScale,panel.Size.X-16f*unit) : null;
        bool multipleRows=compactRows!=null && (compactRows.Count>1 || spec.Riders.Any(r=>r.Text.Contains('\n')));
        float rowY=rowHeight;
		for (int i = 0; i < spec.Riders.Count; i++)
		{
            if(multipleRows)
            {
                var row=compactRows.First(r=>i>=r.Start&&i<r.End);
                if(i==row.Start){rx=(panel.Size.X-row.Width)/2f;ry=rowY;rowY+=row.Height+4f*unit;}
                ph=row.Height;
            }
			SourceIntentLayout.Rider r = spec.Riders[i];
			float rw = riderWidths[i];
			Color fill = new Color(r.Fill);
			Color ink = new Color(r.Ink);
			Control pill = new Control
			{
				Name = "IntentRider_" + i,
				Position = new Vector2(rx, ry),
				Size = new Vector2(rw, ph),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			body.AddChild(pill, forceReadableName: false, Node.InternalMode.Disabled);
			Sheet(pill, "RiderPaper", new Rect2(Vector2.Zero, pill.Size), fill, new Color(fill.R / 0.72f, fill.G / 0.72f, fill.B / 0.72f), 1.5f, unit);
			float tx = 2f * unit;
			if (r.Icon != null)
			{
				Picture(pill, "RiderIcon_" + r.Icon, SourceIntentGlyphs.Mono(r.Icon), new Rect2(2f * unit, (ph - 16f * unit) / 2f, 16f * unit, 16f * unit), nearest: true).Modulate = ink;
				tx += 17f * unit;
			}
            bool wrapped=r.Text.Contains('\n');
			SourceHudLabel label = new SourceHudLabel
			{
				Name = "RiderText",
				Text = r.Text,
				SourceTextRect = new Rect2(tx, 0f, rw - tx - 6f * unit, ph),
				HorizontalAlignment = HorizontalAlignment.Center,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			label.AddThemeFontOverride("font", _riderFont);
			label.AddThemeFontSizeOverride("font_size", rfs);
			label.AddThemeColorOverride("font_color", ink);
            if(wrapped)
            {
                var multi=new Label{Name="RiderText",Text=r.Text,Position=new Vector2(tx,0f),Size=new Vector2(rw-tx-6f*unit,ph),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore};
                multi.AddThemeFontOverride("font",_riderFont);multi.AddThemeFontSizeOverride("font_size",rfs);multi.AddThemeColorOverride("font_color",ink);pill.AddChild(multi);label.Free();
            }
            else {pill.AddChild(label, forceReadableName: false, Node.InternalMode.Disabled);label.RefreshSourceAlignment();}
			rx += rw + (float)(spec.Stack ? 4 : (phone ? 6 : 10)) * unit;
		}
	}

	public static void Highlight(PanelContainer panel, bool selected)
	{
		CanvasItem edge = panel.GetNodeOrNull<CanvasItem>("IntentParts/IntentEdge");
		if (edge != null)
		{
			edge.Visible = selected;
		}
	}

	private static TextureRect Picture(Control parent, string name, Texture2D texture, Rect2 rect, bool nearest)
	{
		TextureRect p = new TextureRect
		{
			Name = name,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = texture,
			Position = rect.Position,
			Size = rect.Size,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = (CanvasItem.TextureFilterEnum)(nearest ? 1 : 2),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		parent.AddChild(p, forceReadableName: false, Node.InternalMode.Disabled);
		return p;
	}

	private static NinePatchRect Sheet(Control parent, string name, Rect2 rect, Color fill, Color edge, float line, float unit)
	{
		NinePatchRect p = new NinePatchRect
		{
			Name = name,
			Position = rect.Position,
			Size = rect.Size,
			Texture = Nine(fill, edge, line),
			RegionRect = new Rect2(0f, 0f, 32f, 32f),
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		Side[] values = Enum.GetValues<Side>();
		foreach (Side s in values)
		{
			p.SetPatchMargin(s, 10);
		}
		p.Scale = Vector2.One;
		parent.AddChild(p, forceReadableName: false, Node.InternalMode.Disabled);
		p.Size = rect.Size / unit;
		p.Position = rect.Position;
		p.Scale = Vector2.One * unit;
		return p;
	}

	private static Texture2D Nine(Color fill, Color edge, float line)
	{
		string key = fill.ToHtml() + edge.ToHtml() + line;
		if (Textures.TryGetValue(key, out var t))
		{
			return t;
		}
		Image image = Image.CreateEmpty(32, 32, useMipmaps: false, Image.Format.Rgba8);
		for (int y = 0; y < 32; y++)
		{
			for (int x = 0; x < 32; x++)
			{
				float qx = Math.Abs((float)x + 0.5f - 16f) - 8f;
				float qy = Math.Abs((float)y + 0.5f - 16f) - 8f;
				float d = 8f - MathF.Sqrt(Math.Max(qx, 0f) * Math.Max(qx, 0f) + Math.Max(qy, 0f) * Math.Max(qy, 0f)) - Math.Min(Math.Max(qx, qy), 0f);
				Color c;
				if (d < 0f)
				{
					c = Colors.Transparent;
				}
				else
				{
					c = ((d < line) ? edge : fill);
				}
				if (d >= 0f && d < 1f)
				{
					c.A *= Math.Clamp(d + 0.5f, 0f, 1f);
				}
				image.SetPixel(x, y, c);
			}
		}
		return Textures[key] = ImageTexture.CreateFromImage(image);
	}

	private static Texture2D Tail(bool minor)
	{
		string key = "tail" + minor;
		if (Textures.TryGetValue(key, out var t))
		{
			return t;
		}
		Image image = Image.CreateEmpty(30, 22, useMipmaps: false, Image.Format.Rgba8);
		for (int y = 0; y < 22; y++)
		{
			for (int x = 0; x < 30; x++)
			{
				float k = 1f - ((float)y + 0.5f) / 22f;
				float left = Mathf.Lerp(2f, 8f, k);
				float right = Mathf.Lerp(26f, 12f, k);
				int num;
				int num2;
				if ((float)x + 0.5f > left)
				{
					num = (((float)x + 0.5f < right) ? 1 : 0);
					if (num != 0)
					{
						num2 = (((float)x + 0.5f < left + 2f || (float)x + 0.5f > right - 2f || y < 2) ? 1 : 0);
						goto IL_00cb;
					}
				}
				else
				{
					num = 0;
				}
				num2 = 0;
				goto IL_00cb;
				IL_00cb:
				bool edge = (byte)num2 != 0;
				Color c;
				if (num != 0)
				{
					c = (edge ? SourcePalette.Paper2 : NightHi);
				}
				else
				{
					c = Colors.Transparent;
				}
				if (minor)
				{
					c *= new Color(SourcePalette.Paper2.R / SourcePalette.Paper.R, SourcePalette.Paper2.G / SourcePalette.Paper.G, SourcePalette.Paper2.B / SourcePalette.Paper.B);
				}
				image.SetPixel(x, 21 - y, c);
			}
		}
		return Textures[key] = ImageTexture.CreateFromImage(image);
	}

	private static Texture2D Load(string name)
	{
		string path = "res://assets/Art/icons/" + name + ".png";
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return GD.Load<Texture2D>(path);
	}

	private static string FallbackIcon(string kind)
	{
		switch (kind)
		{
		case "attack":
			return "sword";
		case "defend":
			return "shield";
		case "buff":
		case "rally":
			return "strength_up";
		case "heal":
			return "heart";
		case "hex":
			return "burn";
		case "destroy-set":
		case "destroy-token":
			return "exhaust";
		case "mill":
			return "draw";
		default:
			return "question";
		}
	}
}
