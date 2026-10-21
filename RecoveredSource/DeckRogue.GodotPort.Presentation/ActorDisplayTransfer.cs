using System;
using System.Numerics;

namespace DeckRogue.GodotPort.Presentation;

public sealed class ActorDisplayTransfer
{
	public const int Size = 32;

	public const int Width = 1024;

	public const int Height = 32;

	public readonly SourcePost Post;

	public readonly byte[] LutRgb;

	public readonly Vector3 PreparedLift;

	public readonly Vector3 PreparedGamma;

	public readonly Vector3 PreparedGain;

	private const float A = 0.88f;

	private const float B = 0.6f;

	private const float C = 0.1f;

	private const float D = 0.2f;

	private const float E = 0.01f;

	private const float F = 0.3f;

	public static readonly float FilmicWhite = FilmicRaw(1f);

	private static readonly Vector3 Ap1Y = new Vector3(0.272229f, 0.674082f, 0.0536895f);

	private static readonly Vector3 RecY = new Vector3(0.2126729f, 0.7151522f, 0.072175f);

	private static float Clamp01(float x)
	{
		return Math.Clamp(x, 0f, 1f);
	}

	private static Vector3 Clip(Vector3 x)
	{
		return Vector3.Clamp(x, Vector3.Zero, Vector3.One);
	}

	private static Vector3 Map(Vector3 x, Func<float, float> f)
	{
		return new Vector3(f(x.X), f(x.Y), f(x.Z));
	}

	private static Vector3 Rows(Vector3 x, Vector3 r, Vector3 g, Vector3 b)
	{
		return new Vector3(Vector3.Dot(x, r), Vector3.Dot(x, g), Vector3.Dot(x, b));
	}

	private static Vector3 Ap1ToXyz(Vector3 x)
	{
		return Rows(x, new Vector3(0.6624542f, 106f / (291f * (float)Math.E), 0.15618768f), new Vector3(0.27222872f, 0.67408174f, 0.053689517f), new Vector3(-0.0055746497f, 0.0040607336f, 1.0103391f));
	}

	private static Vector3 XyzToAp1(Vector3 x)
	{
		return Rows(x, new Vector3(1.6410234f, -0.3248033f, -0.2364247f), new Vector3(-0.66366285f, 1.6153316f, 0.016756348f), new Vector3(0.011721894f, -0.008284442f, 0.98839486f));
	}

	private static Vector3 Desaturate(Vector3 x, float amount)
	{
		return Vector3.Lerp(new Vector3(Vector3.Dot(x, Ap1Y)), x, amount);
	}

	public ActorDisplayTransfer(SourcePost post)
	{
		Post = post ?? throw new ArgumentNullException("post");
		PreparedLift = Prepare(post.Lift, 0.15f, 0);
		PreparedGamma = Prepare(post.Gamma, 0.8f, 1);
		PreparedGain = Prepare(post.Gain, 0.8f, 2);
		LutRgb = new byte[98304];
		for (int b = 0; b < 32; b++)
		{
			for (int g = 0; g < 32; g++)
			{
				for (int r = 0; r < 32; r++)
				{
					Vector3 x = GradeNeutral(new Vector3((float)r / 31f, (float)g / 31f, (float)b / 31f));
					int o = 3 * (g * 1024 + b * 32 + r);
					LutRgb[o] = Quantize(x.X);
					LutRgb[o + 1] = Quantize(x.Y);
					LutRgb[o + 2] = Quantize(x.Z);
				}
			}
		}
	}

	private static byte Quantize(float x)
	{
		return (byte)MathF.Floor(Clamp01(x) * 255f + 0.5f);
	}

	private float DecodeTrackball(float x)
	{
		if (x <= 0.04045f)
		{
			return x / 12.92f;
		}
		if (x > 1f && Post.GammaAboveOneUsesPower22)
		{
			return MathF.Pow(x, 2.2f);
		}
		return MathF.Pow((x + 0.055f) / 1.055f, 2.4f);
	}

	private Vector3 Prepare(Vector4 t, float scale, int kind)
	{
		Vector3 v = Map(new Vector3(t.X, t.Y, t.Z), DecodeTrackball) * scale;
		v -= new Vector3(Vector3.Dot(v, RecY) - t.W);
		if (kind == 0)
		{
			return v;
		}
		v += Vector3.One;
		if (kind != 1)
		{
			return v;
		}
		return Map(v, (float x) => 1f / MathF.Max(x, 0.001f));
	}

	public Vector3 GradeNeutral(Vector3 color)
	{
		color = Map(color, (float x) =>
		{
			float num = 0.244161f * MathF.Log10(5.555556f * x + 0.047996f) + 0.386036f;
			num = 0.4135884f + (num - 0.4135884f) * (1f + Post.ContrastPercent / 100f);
			return MathF.Max(0f, (MathF.Pow(10f, (num - 0.386036f) / 0.244161f) - 0.047996f) / 5.555556f);
		});
		color = color * PreparedGain + PreparedLift;
		color = new Vector3(SignedPower(color.X, PreparedGamma.X), SignedPower(color.Y, PreparedGamma.Y), SignedPower(color.Z, PreparedGamma.Z));
		return Clip(color);
	}

	private static float SignedPower(float x, float gamma)
	{
		return MathF.CopySign(MathF.Pow(MathF.Abs(x), gamma), x);
	}

	private Vector3 Read(int r, int g, int b)
	{
		int i = 3 * (g * 1024 + b * 32 + r);
		return new Vector3((int)LutRgb[i], (int)LutRgb[i + 1], (int)LutRgb[i + 2]) / 255f;
	}

	public Vector3 SampleLut(Vector3 mapped)
	{
		Vector3 p = Clip(mapped) * 31f;
		int r = Math.Min(30, (int)p.X);
		int g = Math.Min(30, (int)p.Y);
		int b = Math.Min(30, (int)p.Z);
		Vector3 f = p - new Vector3(r, g, b);
		Vector3 value = Vector3.Lerp(Vector3.Lerp(Read(r, g, b), Read(r + 1, g, b), f.X), Vector3.Lerp(Read(r, g + 1, b), Read(r + 1, g + 1, b), f.X), f.Y);
		Vector3 z1 = Vector3.Lerp(Vector3.Lerp(Read(r, g, b + 1), Read(r + 1, g, b + 1), f.X), Vector3.Lerp(Read(r, g + 1, b + 1), Read(r + 1, g + 1, b + 1), f.X), f.Y);
		return Vector3.Lerp(value, z1, f.Z);
	}

	public Vector3 SourceLinearDisplay(Vector3 prepost, Vector2 sourceScreenUv, float aspect = 1f)
	{
		Vector2 delta = Vector2.Abs(sourceScreenUv - Post.VignetteCenter) * (3f * Post.VignetteIntensity);
		if (Post.VignetteRounded)
		{
			delta.X *= aspect;
		}
		float falloff = MathF.Pow(Clamp01(1f - delta.LengthSquared()), 5f * Post.VignetteSmoothness);
		Vector3 vignette = Vector3.Lerp(Post.VignetteColor, Vector3.One, falloff);
		Vector3 x = prepost * vignette * MathF.Pow(2f, Post.ExposureEv);
		return SampleLut(Clip(AcesRgb(x)));
	}

	public Vector3 SourceDisplay(Vector3 prepost, Vector2 sourceScreenUv, float aspect = 1f)
	{
		return Map(SourceLinearDisplay(prepost, sourceScreenUv, aspect), EncodeSrgb);
	}

	public Vector3 ActorAlbedo(Vector3 prepost, Vector2 sourceScreenUv, float aspect = 1f)
	{
		return Map(SourceDisplay(prepost, sourceScreenUv, aspect), CompensateChannel);
	}

	public static float EncodeSrgb(float x)
	{
		if (!(x <= 0.0031308f))
		{
			return 1.055f * MathF.Pow(x, 5f / 12f) - 0.055f;
		}
		return x * 12.92f;
	}

	public static Vector3 AcesRgb(Vector3 rgb)
	{
		Vector3 a = Rows(rgb, new Vector3(0.439701f, 0.382978f, 0.177335f), new Vector3(0.0897923f, 0.813423f, 0.0967616f), new Vector3(0.017544f, 0.111544f, 0.870704f));
		float lo = MathF.Min(a.X, MathF.Min(a.Y, a.Z));
		float hi = MathF.Max(a.X, MathF.Max(a.Y, a.Z));
		float sat = (MathF.Max(hi, 0.0001f) - MathF.Max(lo, 0.0001f)) / MathF.Max(hi, 0.01f);
		float chroma = MathF.Sqrt(MathF.Max(0f, ((a.X - a.Y) * (a.X - a.Y) + (a.Y - a.Z) * (a.Y - a.Z) + (a.Z - a.X) * (a.Z - a.X)) * 0.5f));
		float yc = (a.X + a.Y + a.Z + 1.75f * chroma) / 3f;
		float z = (sat - 0.4f) / 0.2f;
		float t = MathF.Max(0f, 1f - MathF.Abs(z) * 0.5f);
		float shape = (1f + (float)MathF.Sign(z) * (1f - t * t)) * 0.5f;
		float glow = 0.05f * shape * ((yc <= 0.05333333f) ? 1f : ((yc >= 0.16f) ? 0f : (0.08f / yc - 0.5f)));
		a *= 1f + glow;
		float hue = MathF.Atan2(MathF.Sqrt(3f) * (a.Y - a.Z), 2f * a.X - a.Y - a.Z) * 180f / (float)Math.PI;
		float h = Clamp01(1f - MathF.Abs(hue) / 67.5f);
		float s = h * h * (3f - 2f * h);
		a.X += (0.03f - a.X) * 0.18f * s * s * sat;
		Vector3 p = Rows(a, new Vector3(1.4514393f, -0.23651075f, -0.21492857f), new Vector3(-0.07655378f, 1.1762297f, -0.09967592f), new Vector3(0.008316148f, -0.0060324497f, 0.9977163f));
		p = Desaturate(Vector3.Max(Vector3.Zero, p), 0.96f);
		p = Map(p, (float v) => (v * v + 0.0245786f * v - 9.0537E-05f) / (0.983729f * v * v + 0.432951f * v + 0.238081f));
		Vector3 xyz = Ap1ToXyz(p);
		float total = MathF.Max(xyz.X + xyz.Y + xyz.Z, 0.0001f);
		float cx = xyz.X / total;
		float cy = xyz.Y / total;
		float lum = MathF.Pow(Math.Clamp(xyz.Y, 0f, 65504f), 0.9811f);
		float mult = lum / MathF.Max(cy, 0.0001f);
		xyz = new Vector3(cx * mult, lum, (1f - cx - cy) * mult);
		p = Desaturate(XyzToAp1(xyz), 0.93f);
		xyz = Ap1ToXyz(p);
		xyz = Rows(xyz, new Vector3(0.987224f, -0.00611327f, 0.0159533f), new Vector3(-0.00759836f, 1.00186f, 0.0053302f), new Vector3(0.00307257f, -0.00509595f, 1.08168f));
		return Rows(xyz, new Vector3(3.24097f, -1.5373832f, -0.49861076f), new Vector3(-0.96924365f, 1.8759675f, 0.04155506f), new Vector3(0.05563008f, -0.20397696f, 1.0569715f));
	}

	public static float FilmicRaw(float x)
	{
		return (0.88f * x * x + 0.060000002f * x + 0.002f) / (0.88f * x * x + 0.6f * x + 0.060000002f) - 0.03333333f;
	}

	public static float GlDecode(float s)
	{
		return s * (s * (0.30530602f * s + 0.6821711f) + 0.012522878f);
	}

	public static float GlOutput(float albedo)
	{
		float y = FilmicRaw(1.12f * GlDecode(albedo)) / FilmicWhite;
		return MathF.Max(0f, 1.055f * MathF.Pow(MathF.Max(0f, y), 5f / 12f) - 0.055f);
	}

	public static float CompensateChannel(float display)
	{
		if (!float.IsFinite(display) || display < 0f || display > 1.000001f)
		{
			throw new ArgumentOutOfRangeException("display");
		}
		if (display <= 0f)
		{
			return 0f;
		}
		display = MathF.Min(1f, display);
		float q = MathF.Pow((display + 0.055f) / 1.055f, 2.4f) * FilmicWhite + 0.03333333f;
		float aa = 0.88f * (1f - q);
		float bb = 0.6f * (0.1f - q);
		float cc = 0.2f * (0.01f - 0.3f * q);
		if (aa <= 0f)
		{
			throw new InvalidOperationException("Target outside Filmic inverse domain");
		}
		float num = bb * bb - 4f * aa * cc;
		if (num < 0f)
		{
			throw new InvalidOperationException("Invalid Filmic inverse discriminant");
		}
		float root = MathF.Sqrt(num);
		float linear = ((bb >= 0f) ? (-2f * cc / (bb + root)) : ((0f - bb + root) / (2f * aa))) / 1.12f;
		if (!float.IsFinite(linear) || linear < 0f || linear > 1f)
		{
			throw new InvalidOperationException("Target outside GL ALBEDO range");
		}
		float x = EncodeSrgb(linear);
		for (int i = 0; i < 6; i++)
		{
			x = Clamp01(x - (GlDecode(x) - linear) / (0.91591805f * x * x + 1.3643422f * x + 0.012522878f));
		}
		return x;
	}
}
