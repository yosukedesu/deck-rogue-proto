using System;
using System.Collections.Generic;
using Godot;

namespace DeckRogue.GodotPort.UI;

public static class SourceEnemyLedgerGlyphs
{
	private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

	private static readonly Dictionary<string, string[]> Rows = new Dictionary<string, string[]>
	{
		{
			"counter",
			new string[16]
			{
				"................", "................", "....--..........", "...-#-..........", "..-#-----.......", ".-######-.......", "..-#-----#-.....", "...-#-...-#-....", "....--....-#-...", "...........#-...",
				"..........-#-...", ".....---.-#-....", ".....-###-......", "......---.......", "................", "................"
			}
		},
		{
			"armor",
			new string[16]
			{
				"................", "..###......###..", "..####....####..", "..############..", "...##########...", "...##########...", "...##########...", "................", "...##########...", "...##########...",
				"................", "...##########...", "....########....", "................", "................", "................"
			}
		}
	};

	public static void PillPaper(Control parent, Color fill, float unit)
	{
		string key = "pill:" + fill.ToHtml();
		if (!Cache.TryGetValue(key, out var texture))
		{
			using Image image = Image.CreateEmpty(32, 32, useMipmaps: false, Image.Format.Rgba8);
			Color edge = new Color(fill.R / 0.72f, fill.G / 0.72f, fill.B / 0.72f, fill.A);
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
						c = ((d < 1.5f) ? edge : fill);
					}
					if (d >= 0f && d < 1f)
					{
						c.A *= Math.Clamp(d + 0.5f, 0f, 1f);
					}
					image.SetPixel(x, y, c);
				}
			}
			texture = (Cache[key] = ImageTexture.CreateFromImage(image));
		}
		NinePatchRect paper = new NinePatchRect
		{
			Name = "PillPaper",
			Texture = texture,
			RegionRect = new Rect2(0f, 0f, 32f, 32f),
			Size = parent.Size / unit,
			Scale = Vector2.One * unit,
			TextureFilter = CanvasItem.TextureFilterEnum.Linear,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		Side[] values = Enum.GetValues<Side>();
		foreach (Side side in values)
		{
			paper.SetPatchMargin(side, 10);
		}
		parent.AddChild(paper, forceReadableName: false, Node.InternalMode.Disabled);
	}

	public static Texture2D Mono(string name)
	{
		if (!Rows.TryGetValue(name, out var rows))
		{
			return SourceIntentGlyphs.Mono(name);
		}
		if (Cache.TryGetValue(name, out var t))
		{
			return t;
		}
		using Image image = Image.CreateEmpty(16, 16, useMipmaps: false, Image.Format.Rgba8);
		for (int y = 0; y < 16; y++)
		{
			for (int x = 0; x < 16; x++)
			{
				image.SetPixel(x, y, (rows[y][x] == '#' || rows[y][x] == '+') ? Colors.White : Colors.Transparent);
			}
		}
		return Cache[name] = ImageTexture.CreateFromImage(image);
	}
}
