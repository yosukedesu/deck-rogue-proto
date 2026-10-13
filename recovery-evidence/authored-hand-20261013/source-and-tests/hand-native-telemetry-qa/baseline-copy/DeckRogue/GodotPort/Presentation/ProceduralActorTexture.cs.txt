using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public static class ProceduralActorTexture
{
	private static int RoundToInt(float x)
	{
		return (int)MathF.Round(x, MidpointRounding.ToEven);
	}

	private static Texture2D FromPixels(Color[] pixels, int n)
	{
		Image image = Image.CreateEmpty(n, n, useMipmaps: false, Image.Format.Rgba8);
		for (int y = 0; y < n; y++)
		{
			for (int x = 0; x < n; x++)
			{
				image.SetPixel(x, n - 1 - y, pixels[y * n + x]);
			}
		}
		return ImageTexture.CreateFromImage(image);
	}

	private static uint Hash(string s)
	{
		uint h = 2166136261u;
		for (int i = 0; i < s.Length; i++)
		{
			h ^= s[i];
			h *= 16777619;
		}
		return h;
	}

	public static Texture2D Generate(string id, bool friendly, int size)
	{
		int n = Mathf.Max(16, size);
		float u = (float)n / 64f;
		uint h = Hash(id);
		Random rng = new Random((int)(h & 0x7FFFFFFF));
		float hue = (friendly ? (0.33f + (float)(h % 30) / 300f) : ((float)(h % 360) / 360f));
		Color main = Color.FromHsv(hue, friendly ? 0.5f : 0.55f, friendly ? 0.72f : 0.64f);
		Color shade = Color.FromHsv(hue, 0.64f, 0.4f);
		Color light = Color.FromHsv(hue, 0.38f, 0.88f);
		Color hilite = Color.FromHsv(hue, 0.2f, 1f);
		Color outline = Color.FromHsv(hue, 0.7f, 0.14f);
		float cx = (float)n * 0.5f;
		float bodyRx = (float)n * (0.22f + (float)rng.NextDouble() * 0.12f);
		float bodyRy = (float)n * (0.16f + (float)rng.NextDouble() * 0.08f);
		float bodyCy = (float)n * (0.3f + (float)rng.NextDouble() * 0.06f);
		float headR = (float)n * (0.14f + (float)rng.NextDouble() * 0.08f);
		float headCx = cx + ((rng.NextDouble() < 0.5) ? (-1f) : 1f) * (float)n * (float)rng.NextDouble() * 0.05f;
		float headCy = bodyCy + bodyRy * 0.55f + headR * 0.6f;
		int legs = ((rng.NextDouble() < 0.55) ? 2 : 4);
		float legW = (float)n * 0.06f;
		float legTop = bodyCy;
		float legSpread = bodyRx * ((legs == 2) ? 0.45f : 0.75f);
		bool horns = rng.NextDouble() < 0.5;
		bool ears = !horns && rng.NextDouble() < 0.6;
		bool cyclops = rng.NextDouble() < 0.18;
		Color[] px = new Color[n * n];
		for (int y = 0; y < n; y++)
		{
			for (int x = 0; x < n; x++)
			{
				Color c = new Color(0f, 0f, 0f, 0f);
				if (Solid(x, y))
				{
					bool edge = !Solid(x - 1, y) || !Solid(x + 1, y) || !Solid(x, y - 1) || !Solid(x, y + 1);
					int toLight = 0;
					int toShade = 0;
					for (int k = 1; k <= 6 && Solid(x + k, y + k); k++)
					{
						toLight = k;
					}
					for (int i = 1; i <= 6 && Solid(x - i, y - i); i++)
					{
						toShade = i;
					}
					bool dither = ((x + y) & 1) == 0;
					int L = Mathf.Max(1, RoundToInt(u));
					c = (edge ? outline : ((toLight <= L) ? hilite : ((toLight <= 3 * L) ? (dither ? light : main) : ((toShade <= 2 * L) ? shade : ((toShade > 4 * L) ? main : (dither ? shade : main))))));
					float hx = (float)x + 0.5f - headCx;
					float hy = (float)y + 0.5f - headCy;
					float hd = Mathf.Sqrt(hx * hx + hy * hy);
					if (!edge && hd > headR - 1.2f * u && hd <= headR && (float)y + 0.5f < headCy)
					{
						c = shade;
					}
					if (!edge && (x * 7 + y * 13) % 31 == 0 && (float)y + 0.5f < bodyCy + bodyRy * 0.6f)
					{
						c = c.Lerp(shade, 0.6f);
					}
				}
				px[y * n + x] = c;
			}
		}
		int eyeW = Mathf.Max(3, RoundToInt(4f * u));
		int ey = RoundToInt(headCy - (float)eyeW * 0.5f + headR * 0.05f);
		int[] eyeXs = ((!cyclops) ? new int[2]
		{
			RoundToInt(headCx - headR * 0.5f - (float)eyeW * 0.5f),
			RoundToInt(headCx + headR * 0.5f - (float)eyeW * 0.5f)
		} : new int[1] { RoundToInt(headCx - (float)eyeW * 0.5f) });
		int[] array = eyeXs;
		foreach (int bx in array)
		{
			for (int dy = 0; dy < eyeW; dy++)
			{
				for (int dx = 0; dx < eyeW; dx++)
				{
					if (Solid(bx + dx, ey + dy))
					{
						px[(ey + dy) * n + bx + dx] = Colors.White;
					}
				}
			}
			int pw = Mathf.Max(1, eyeW / 2);
			for (int l = 0; l < pw; l++)
			{
				for (int m = 0; m < pw; m++)
				{
					if (bx + m + 1 < n && ey + l + 1 < n)
					{
						px[(ey + l + 1) * n + bx + m + 1] = Colors.Black;
					}
				}
			}
			if (bx + pw + 1 < n && ey + pw + 1 < n)
			{
				px[(ey + pw + 1) * n + bx + pw + 1 - 1] = new Color(0.85f, 0.9f, 1f);
			}
			for (int num = -1; num <= eyeW; num++)
			{
				if (Solid(bx + num, ey - 1))
				{
					px[(ey - 1) * n + bx + num] = outline;
				}
				if (Solid(bx + num, ey + eyeW))
				{
					px[(ey + eyeW) * n + bx + num] = outline;
				}
			}
		}
		int my = RoundToInt(headCy - headR * 0.45f);
		int mw = RoundToInt(headR * 0.5f);
		for (int num2 = -mw; num2 <= mw; num2++)
		{
			int mx = RoundToInt(headCx) + num2;
			if (Solid(mx, my))
			{
				px[my * n + mx] = outline;
			}
		}
		if (rng.NextDouble() < 0.5)
		{
			for (int num3 = -mw; num3 <= mw; num3 += Mathf.Max(1, mw))
			{
				int mx2 = RoundToInt(headCx) + num3;
				if (Solid(mx2, my - 1))
				{
					px[(my - 1) * n + mx2] = Colors.White;
				}
			}
		}
		return FromPixels(px, n);
		bool Solid(int num4, int num5)
		{
			if (num4 < 0 || num5 < 0 || num4 >= n || num5 >= n)
			{
				return false;
			}
			float fx = (float)num4 + 0.5f;
			float fy = (float)num5 + 0.5f;
			float dx2 = (fx - cx) / bodyRx;
			float dy2 = (fy - bodyCy) / bodyRy;
			if (dx2 * dx2 + dy2 * dy2 <= 1f)
			{
				return true;
			}
			float hx2 = fx - headCx;
			float hy2 = fy - headCy;
			if (hx2 * hx2 + hy2 * hy2 <= headR * headR)
			{
				return true;
			}
			for (int num6 = 0; num6 < legs; num6++)
			{
				float lx = ((legs == 2) ? (cx + ((num6 == 0) ? (0f - legSpread) : legSpread)) : (cx + ((float)num6 - 1.5f) * legSpread * 0.66f));
				if (Mathf.Abs(fx - lx) <= legW * 0.5f && fy >= (float)n * 0.04f && fy <= legTop)
				{
					return true;
				}
				if (fy < (float)n * 0.04f + legW * 0.6f && fy >= (float)n * 0.04f && Mathf.Abs(fx - lx) <= legW * 0.8f)
				{
					return true;
				}
			}
			if (horns)
			{
				for (int sgn = -1; sgn <= 1; sgn += 2)
				{
					float bx2 = headCx + (float)sgn * headR * 0.6f;
					float by = headCy + headR * 0.7f;
					float t = (fy - by) / (headR * 0.9f);
					if (t >= 0f && t <= 1f && Mathf.Abs(fx - (bx2 + (float)sgn * t * headR * 0.35f)) <= (1f - t) * legW * 0.6f + 0.6f)
					{
						return true;
					}
				}
			}
			if (ears)
			{
				for (int num7 = -1; num7 <= 1; num7 += 2)
				{
					float ex = headCx + (float)num7 * headR * 0.8f;
					float ey2 = headCy + headR * 0.65f;
					float r = headR * 0.42f;
					if ((fx - ex) * (fx - ex) + (fy - ey2) * (fy - ey2) <= r * r)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public static Texture2D Doll(string id, int size)
	{
		int n = Mathf.Max(16, size);
		float u = (float)n / 32f;
		Color[] px = new Color[n * n];
		Color clear = new Color(0f, 0f, 0f, 0f);
		for (int i = 0; i < px.Length; i++)
		{
			px[i] = clear;
		}
		Color white = Color.FromHtml("#e9e6df");
		Color whiteShade = Color.FromHtml("#b9b6b0");
		Color whiteDark = Color.FromHtml("#8a8884");
		Color ink = Color.FromHtml("#2f2e35");
		Color brass = Color.FromHtml("#c99a3a");
		Color brassDark = Color.FromHtml("#8a6520");
		Color hollow = Color.FromHtml("#1a1620");
		Color amber = Color.FromHtml("#ffc45a");
		if (!id.Contains("hound"))
		{
			for (int y = X(3f); y <= X(15f); y++)
			{
				float t = (float)(y - X(3f)) / (float)Mathf.Max(1, X(15f) - X(3f));
				int half = RoundToInt(Mathf.Lerp(4f, 6f, t) * u);
				for (int x = X(15f) - half; x <= X(16f) + half; x++)
				{
					Put(x, y, (x < X(15f)) ? whiteShade : white);
				}
			}
			for (int j = X(1f); j < X(4f); j++)
			{
				for (int k = X(11f); k <= X(13f); k++)
				{
					Put(k, j, whiteDark);
				}
				for (int l = X(18f); l <= X(20f); l++)
				{
					Put(l, j, whiteDark);
				}
			}
			for (int m = X(10f); m <= X(21f); m++)
			{
				Put(m, X(8f), brass);
			}
			Put(X(12f), X(12f), brass);
			Put(X(19f), X(12f), brass);
			Put(X(15f), X(5f), brassDark);
			for (int num = X(15f); num <= X(27f); num++)
			{
				float t2 = (float)(num - X(15f)) / (float)Mathf.Max(1, X(27f) - X(15f));
				int half2 = RoundToInt(((t2 < 0.75f) ? 6.5f : Mathf.Lerp(6.5f, 1.5f, (t2 - 0.75f) / 0.25f)) * u);
				for (int num2 = X(15f) - half2; num2 <= X(16f) + half2; num2++)
				{
					Put(num2, num, (num2 < X(14f)) ? whiteShade : white);
				}
			}
			Put(X(16f), X(28f), white);
			Put(X(17f), X(29f), whiteShade);
			for (int num3 = X(18f); num3 <= X(23f); num3++)
			{
				for (int num4 = X(13f); num4 <= X(20f); num4++)
				{
					Put(num4, num3, hollow);
				}
			}
			Put(X(14f), X(21f), amber);
			Put(X(15f), X(21f), amber);
			Put(X(18f), X(21f), amber);
			Put(X(19f), X(21f), amber);
			Put(X(9f), X(13f), brassDark);
			Put(X(9f), X(14f), brass);
			Put(X(9f), X(12f), brassDark);
			if (id.Contains("squire") || id.Contains("page"))
			{
				for (int num5 = X(9f); num5 <= X(24f); num5++)
				{
					Put(X(24f), num5, (num5 > X(11f)) ? brass : brassDark);
				}
				Put(X(23f), X(11f), brassDark);
				Put(X(25f), X(11f), brassDark);
			}
			else if (id.Contains("shield"))
			{
				for (int num6 = X(6f); num6 <= X(16f); num6++)
				{
					for (int num7 = X(5f); num7 <= X(11f); num7++)
					{
						float dx = num7 - X(8f);
						float dy = num6 - X(11f);
						if (dx * dx / (9f * u * u) + dy * dy / (25f * u * u) <= 1f)
						{
							Put(num7, num6, (Mathf.Abs(dx) > 2.2f * u || Mathf.Abs(dy) > 4f * u) ? brass : white);
						}
					}
				}
			}
			else if (id.Contains("archer"))
			{
				for (int num8 = X(20f); num8 <= X(27f); num8++)
				{
					Put(num8, X(12f), brassDark);
				}
				for (int num9 = X(9f); num9 <= X(15f); num9++)
				{
					Put(X(25f), num9, brass);
				}
			}
			else if (id.Contains("band"))
			{
				for (int num10 = X(12f); num10 <= X(17f); num10++)
				{
					for (int num11 = X(23f); num11 <= X(27f); num11++)
					{
						Put(num11, num10, (num10 == X(12f)) ? brassDark : brass);
					}
				}
				Put(X(25f), X(18f), brassDark);
			}
			else if (id.Contains("candle"))
			{
				for (int num12 = X(10f); num12 <= X(18f); num12++)
				{
					Put(X(24f), num12, white);
				}
				Put(X(24f), X(19f), amber);
				Put(X(24f), X(20f), amber);
				Put(X(24f), X(9f), brass);
			}
			else if (id.Contains("banneret"))
			{
				for (int num13 = X(4f); num13 <= X(30f); num13++)
				{
					Put(X(25f), num13, brassDark);
				}
				for (int num14 = X(20f); num14 <= X(29f); num14++)
				{
					for (int num15 = X(26f); num15 <= X(31f); num15++)
					{
						Put(num15, num14, ((num15 + num14) % 3 == 0) ? whiteShade : white);
					}
				}
			}
			else if (id.Contains("choir") || id.Contains("monk"))
			{
				for (int num16 = X(21f); num16 <= X(26f); num16++)
				{
					Put(num16, X(13f), brass);
				}
				for (int num17 = X(22f); num17 <= X(25f); num17++)
				{
					Put(num17, X(14f), amber);
					Put(num17, X(12f), brassDark);
				}
			}
		}
		else
		{
			for (int num18 = X(6f); num18 <= X(14f); num18++)
			{
				for (int num19 = X(6f); num19 <= X(24f); num19++)
				{
					Put(num19, num18, (num18 < X(9f)) ? whiteShade : white);
				}
			}
			for (int num20 = X(8f); num20 <= X(22f); num20 += X(4f))
			{
				for (int num21 = X(1f); num21 < X(6f); num21++)
				{
					Put(num20, num21, whiteDark);
					Put(num20 + 1, num21, whiteDark);
				}
			}
			for (int num22 = X(10f); num22 <= X(19f); num22++)
			{
				for (int num23 = X(21f); num23 <= X(29f); num23++)
				{
					Put(num23, num22, white);
				}
			}
			for (int num24 = X(12f); num24 <= X(16f); num24++)
			{
				for (int num25 = X(23f); num25 <= X(28f); num25++)
				{
					Put(num25, num24, hollow);
				}
			}
			Put(X(25f), X(14f), amber);
			Put(X(27f), X(14f), amber);
			for (int num26 = X(8f); num26 <= X(22f); num26++)
			{
				Put(num26, X(10f), brass);
			}
			Put(X(5f), X(13f), whiteShade);
			Put(X(4f), X(14f), whiteShade);
			Put(X(3f), X(15f), whiteDark);
			Put(X(14f), X(15f), brassDark);
			Put(X(14f), X(16f), brass);
		}
		Color[] src = (Color[])px.Clone();
		for (int num27 = 0; num27 < n; num27++)
		{
			for (int num28 = 0; num28 < n; num28++)
			{
				if (!(src[num27 * n + num28].A > 0f) && ((num28 > 0 && src[num27 * n + num28 - 1].A > 0f) || (num28 + 1 < n && src[num27 * n + num28 + 1].A > 0f) || (num27 > 0 && src[(num27 - 1) * n + num28].A > 0f) || (num27 + 1 < n && src[(num27 + 1) * n + num28].A > 0f)))
				{
					px[num27 * n + num28] = ink;
				}
			}
		}
		return FromPixels(px, n);
		void Put(int num29, int num30, Color c)
		{
			if (num29 >= 0 && num30 >= 0 && num29 < n && num30 < n)
			{
				px[num30 * n + num29] = c;
			}
		}
		int X(float v)
		{
			return RoundToInt(v * u);
		}
	}
}
