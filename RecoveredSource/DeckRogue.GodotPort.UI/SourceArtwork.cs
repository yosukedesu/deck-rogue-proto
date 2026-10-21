using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace DeckRogue.GodotPort.UI;

public static class SourceArtwork
{
	private static readonly Dictionary<string, Texture2D?> Cache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

	private static readonly Regex FusedId = new Regex("^fused_(.+)__(.+)$");

	private static readonly Color ManaTeal = new Color(0.42f, 0.95f, 0.86f);

	public static Texture2D? Card(string id, string cardType)
	{
		return Card(id, CardTypeColor(cardType));
	}

	public static Texture2D? Card(string id, Color tint)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		string key = "cardart:" + id;
		if (Cache.TryGetValue(key, out Texture2D saved))
		{
			return saved;
		}
		Texture2D art = Art("cards", id) ?? Fused(id);
		if (art != null)
		{
			return Cache[key] = art;
		}
		Random rng = Seeded(id);
		Color[] pixels = new Color[384];
		Color dark = tint.Lerp(Colors.Black, 0.55f);
		Color light = tint.Lerp(Colors.White, 0.35f);
		bool[,] mask = new bool[12, 16];
		for (int y = 1; y < 15; y++)
		{
			for (int x = 0; x < 12; x++)
			{
				float cx = ((float)x + 0.5f) / 12f;
				float cy = 1f - Mathf.Abs(((float)y - 8f) / 8f);
				mask[x, y] = rng.NextDouble() < (double)(0.08f + 0.5f * cx * cy);
			}
		}
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 24; j++)
			{
				Color color = dark.Lerp(Colors.Black, 0.6f);
				if (At(j, i))
				{
					if (!At(j - 1, i) || !At(j + 1, i) || !At(j, i - 1) || !At(j, i + 1))
					{
						color = dark;
					}
					else
					{
						color = ((i > 8) ? light : tint);
					}
				}
				pixels[i * 24 + j] = color;
			}
		}
		return Cache[key] = MakeTexture(pixels, 24, 16, "cardart:" + id);
		bool At(int num, int num2)
		{
			if (num < 0 || num2 < 0 || num2 >= 16 || num >= 24)
			{
				return false;
			}
			int mx = ((num < 12) ? num : (23 - num));
			return mask[mx, num2];
		}
	}

	public static Texture2D? Gear(string id, string family)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		string key = "gearglyph:" + id;
		if (Cache.TryGetValue(key, out Texture2D saved))
		{
			return saved;
		}
		Texture2D art = Art("gears", id);
		if (art != null)
		{
			return Cache[key] = art;
		}
		Random rng = Seeded(id);
		int teeth = 6 + rng.Next(0, 4);
		float hub = 0.16f + 0.1f * (float)rng.NextDouble();
		float phase = (float)rng.NextDouble() * (float)Math.PI * 2f;
		bool teal = family == "interfere";
		Color body = new Color(teal ? "#3aa79b" : "#c99a3a");
		Color light = new Color(teal ? "#b5ddd6" : "#ead08a");
		Color dark = new Color(teal ? "#155650" : "#634410");
		Color edge = new Color("#2f2e35");
		Color[] pixels = new Color[400];
		float center = 9.5f;
		for (int y = 0; y < 20; y++)
		{
			for (int x = 0; x < 20; x++)
			{
				Color color = new Color(0f, 0f, 0f, 0f);
				if (Inside(x, y))
				{
					bool num = !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1) || !Inside(x, y + 1);
					float dx = (float)x - center;
					float dy = (float)y - center;
					if (num)
					{
						color = dark;
					}
					else
					{
						color = ((dx - dy > 2f) ? light : body);
					}
				}
				else if (Inside(x - 1, y) || Inside(x + 1, y) || Inside(x, y - 1) || Inside(x, y + 1))
				{
					color = edge;
				}
				pixels[y * 20 + x] = color;
			}
		}
		return Cache[key] = MakeTexture(pixels, 20, 20, "gearglyph:" + id);
		bool Inside(int num2, int num3)
		{
			if (num2 < 0 || num3 < 0 || num2 >= 20 || num3 >= 20)
			{
				return false;
			}
			float dx2 = (float)num2 - center;
			float dy2 = (float)num3 - center;
			float radius = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2) / 10f;
			if (radius < hub)
			{
				return false;
			}
			float angle = Mathf.Atan2(dy2, dx2) + phase;
			float tooth = 0.5f + 0.5f * Mathf.Cos(angle * (float)teeth);
			float outer = 0.72f + 0.22f * ((tooth > 0.55f) ? 1f : 0f);
			return radius <= outer;
		}
	}

	public static Texture2D? GearMuted(string id, string family)
	{
		string key = "gearglyph-muted:" + id;
		if (Cache.TryGetValue(key, out Texture2D saved))
		{
			return saved;
		}
		Texture2D source = Gear(id, family);
		if (source == null)
		{
			return Cache[key] = null;
		}
		try
		{
			int w = source.GetWidth();
			int h = source.GetHeight();
			Color[] pixels = PixelsOf(source, w, h);
			Color paper = new Color("#c4beb2");
			for (int i = 0; i < pixels.Length; i++)
			{
				Color c = pixels[i];
				float luma = 0.3f * c.R + 0.59f * c.G + 0.11f * c.B;
				float r = Mathf.Lerp(Mathf.Lerp(c.R, luma, 0.85f), paper.R, 0.35f);
				float g = Mathf.Lerp(Mathf.Lerp(c.G, luma, 0.85f), paper.G, 0.35f);
				float b = Mathf.Lerp(Mathf.Lerp(c.B, luma, 0.85f), paper.B, 0.35f);
				pixels[i] = new Color(Mathf.Round(r * 255f) / 255f, Mathf.Round(g * 255f) / 255f, Mathf.Round(b * 255f) / 255f, c.A);
			}
			return Cache[key] = MakeTexture(pixels, w, h, key);
		}
		catch (Exception ex)
		{
			GD.PushWarning("Source gear artwork could not be muted: " + ex.Message);
			return Cache[key] = null;
		}
	}

	public static Texture2D? Fused(string id)
	{
		if (string.IsNullOrEmpty(id) || !id.StartsWith("fused_", StringComparison.Ordinal))
		{
			return null;
		}
		string key = "fusedart:" + id;
		if (Cache.TryGetValue(key, out Texture2D saved))
		{
			return saved;
		}
		Match match = FusedId.Match(id);
		if (!match.Success)
		{
			return Cache[key] = null;
		}
		string idA = match.Groups[1].Value;
		string idB = match.Groups[2].Value;
		Texture2D a = Art("cards", idA) ?? Fused(idA);
		Texture2D b = Art("cards", idB) ?? Fused(idB);
		if (a == null || b == null)
		{
			return Cache[key] = null;
		}
		try
		{
			return Cache[key] = ((idA == idB) ? TrueForm(a) : Melt(a, b));
		}
		catch (Exception ex)
		{
			GD.PushWarning("Source fused artwork could not be read: " + id + ": " + ex.Message);
			return Cache[key] = null;
		}
	}

	private static HashSet<string>? ModernCardIds;

	public static Texture2D? ModernCard(string id)
	{
		if (ModernCardIds == null)
		{
			ModernCardIds = new HashSet<string>(StringComparer.Ordinal);
			const string path = "res://assets/Art/cards_modern/active-cards.json";
			try
			{
				if (FileAccess.FileExists(path))
				{
					string[] ids = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(FileAccess.GetFileAsString(path)) ?? Array.Empty<string>();
					foreach (string candidate in ids)
						if (!string.IsNullOrEmpty(candidate) && Regex.IsMatch(candidate, "^[a-z0-9_]+$")) ModernCardIds.Add(candidate);
				}
			}
			catch (Exception ex) { GD.PushWarning("Modern card allowlist could not be read: " + ex.Message); }
		}
		return ModernCardIds.Contains(id) ? Art("cards_modern", id) : null;
	}

	private static Texture2D? Art(string category, string id)
	{
		string key = "art:" + category + "/" + id;
		if (Cache.TryGetValue(key, out Texture2D saved))
		{
			return saved;
		}
		if (id.Contains('/') || id.Contains('\\') || id.Contains("..", StringComparison.Ordinal))
		{
			return Cache[key] = null;
		}
		if (category == "cards")
		{
			Texture2D modern = ModernCard(id);
			if (modern != null) return Cache[key] = modern;
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
		return Cache[key] = texture;
	}

	private static Color CardTypeColor(string type)
	{
		return type switch
		{
			"spell" => new Color("#6c4f9c"), 
			"reaction" => new Color("#2a7d74"), 
			"permanent" => new Color("#9c7a24"), 
			_ => new Color("#7d6146"), 
		};
	}

	private static Random Seeded(string id)
	{
		uint hash = 2166136261u;
		foreach (char ch in id)
		{
			hash ^= ch;
			hash *= 16777619;
		}
		return new Random((int)(hash & 0x7FFFFFFF));
	}

	private static Color[] PixelsOf(Texture2D texture, int w, int h)
	{
		Image sharedImage = texture.GetImage();
		if (sharedImage == null || sharedImage.IsEmpty())
		{
			throw new InvalidOperationException("Source image pixels are unavailable.");
		}
		using Image image = (Image)sharedImage.Duplicate();
		if (image.IsCompressed())
		{
			image.Decompress();
		}
		int sw = image.GetWidth();
		int sh = image.GetHeight();
		Color[] pixels = new Color[w * h];
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				pixels[y * w + x] = image.GetPixel(Math.Min(sw - 1, x * sw / w), sh - 1 - Math.Min(sh - 1, y * sh / h));
			}
		}
		return pixels;
	}

	private static Texture2D Melt(Texture2D a, Texture2D b)
	{
		Color[] pa = PixelsOf(a, 80, 48);
		Color[] pb = PixelsOf(b, 80, 48);
		Color[] pixels = new Color[3840];
		for (int y = 0; y < 48; y++)
		{
			float seam = 36f + 8f * (float)y / 47f;
			for (int x = 0; x < 80; x++)
			{
				float num = (float)x + 0.5f - seam;
				Color color = ((num < 0f) ? pa[y * 80 + x] : pb[y * 80 + x]);
				float abs = Math.Abs(num);
				if (abs < 0.75f)
				{
					color = color.Lerp(ManaTeal, 0.95f);
				}
				else if (abs < 1.75f)
				{
					color = color.Lerp(ManaTeal, 0.45f);
				}
				else if (abs < 2.75f)
				{
					color = color.Lerp(ManaTeal, 0.15f);
				}
				pixels[y * 80 + x] = color;
			}
		}
		return MakeTexture(pixels, 80, 48, "fused:" + a.ResourceName + "+" + b.ResourceName);
	}

	private static Texture2D TrueForm(Texture2D a)
	{
		int w = a.GetWidth();
		int h = a.GetHeight();
		Color[] pixels = PixelsOf(a, w, h);
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				float k = Math.Min(Math.Min(x, w - 1 - x), Math.Min(y, h - 1 - y)) switch
				{
					3 => 0.12f, 
					2 => 0.35f, 
					1 => 0.85f, 
					_ => 0f, 
				};
				if (k > 0f)
				{
					pixels[y * w + x] = pixels[y * w + x].Lerp(ManaTeal, k);
				}
			}
		}
		return MakeTexture(pixels, w, h, "true:" + a.ResourceName);
	}

	private static Texture2D MakeTexture(Color[] pixels, int w, int h, string name)
	{
		using Image image = Image.CreateEmpty(w, h, useMipmaps: false, Image.Format.Rgba8);
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				image.SetPixel(x, h - 1 - y, pixels[y * w + x]);
			}
		}
		ImageTexture imageTexture = ImageTexture.CreateFromImage(image);
		imageTexture.ResourceName = name;
		return imageTexture;
	}
}
