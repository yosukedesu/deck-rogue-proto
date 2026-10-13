using System;

namespace DeckRogue.Game;

internal static class BoundedContactGeometry
{
	internal enum Shape
	{
		Neutral,
		Point,
		Wind,
		Slash,
		Sweep,
		Blunt,
		Bind,
		Engulf,
		Pair,
		Column,
		Smoke,
		Curve,
		Break,
		Object,
		CurvedObject,
		Whip,
		Curse
	}

	internal enum Surface
	{
		Neutral,
		Water,
		Cold,
		Wood,
		Plant,
		Fire,
		Smoke,
		Dark
	}

	internal sealed class Profile
	{
		public string Id;

		public Shape Form;

		public Surface Material;

		public Profile(string id, Shape shape, Surface surface)
		{
			Id = id;
			Form = shape;
			Material = surface;
		}
	}

	public sealed class Buffer
	{
		public readonly double[] X = new double[160];

		public readonly double[] Y = new double[160];

		public readonly double[] U = new double[160];

		public readonly double[] V = new double[160];

		public readonly double[] Alpha = new double[160];

		public readonly double[] Soft = new double[160];

		public readonly int[] Triangles = new int[240];

		public int Quads;

		public int Particles;

		public Buffer()
		{
			for (int i = 0; i < 40; i++)
			{
				int n = i * 4;
				int t = i * 6;
				Triangles[t] = n;
				Triangles[t + 1] = n + 2;
				Triangles[t + 2] = n + 1;
				Triangles[t + 3] = n;
				Triangles[t + 4] = n + 3;
				Triangles[t + 5] = n + 2;
			}
		}
	}

	internal static readonly Profile[] Profiles = new Profile[24]
	{
		new Profile("minimal_neutral_contact", Shape.Neutral, Surface.Neutral),
		new Profile("neutral_point_contact", Shape.Point, Surface.Neutral),
		new Profile("neutral_wind_contact", Shape.Wind, Surface.Neutral),
		new Profile("water_slash_contact", Shape.Slash, Surface.Water),
		new Profile("cold_sweep_contact", Shape.Sweep, Surface.Cold),
		new Profile("ice_blunt_contact", Shape.Blunt, Surface.Cold),
		new Profile("plant_bind_contact", Shape.Bind, Surface.Plant),
		new Profile("neutral_engulf_contact", Shape.Engulf, Surface.Neutral),
		new Profile("wood_blunt_contact", Shape.Blunt, Surface.Wood),
		new Profile("plant_point_contact", Shape.Point, Surface.Plant),
		new Profile("plant_fang_contact", Shape.Pair, Surface.Plant),
		new Profile("ember_streak_contact", Shape.Sweep, Surface.Fire),
		new Profile("fire_blade_contact", Shape.Slash, Surface.Fire),
		new Profile("vertical_fire_contact", Shape.Column, Surface.Fire),
		new Profile("smoke_local_contact", Shape.Smoke, Surface.Smoke),
		new Profile("curved_vine_contact", Shape.Curve, Surface.Plant),
		new Profile("fire_point_contact", Shape.Point, Surface.Fire),
		new Profile("ice_break_contact", Shape.Break, Surface.Cold),
		new Profile("neutral_thrown_object_contact", Shape.Object, Surface.Neutral),
		new Profile("dark_slash_contact", Shape.Slash, Surface.Dark),
		new Profile("neutral_projectile_contact", Shape.Neutral, Surface.Neutral),
		new Profile("curved_object_contact", Shape.CurvedObject, Surface.Neutral),
		new Profile("local_curse_contact", Shape.Curse, Surface.Dark),
		new Profile("neutral_whip_contact", Shape.Whip, Surface.Neutral)
	};

	public const double End = 0.18;

	public const int MaxQuads = 40;

	public static Profile Find(string family)
	{
		Profile[] profiles = Profiles;
		foreach (Profile p in profiles)
		{
			if (p.Id == family)
			{
				return p;
			}
		}
		return null;
	}

	public static bool Finite(double x)
	{
		return !double.IsNaN(x) && !double.IsInfinity(x);
	}

	public static double Clamp(double x, double lo, double hi)
	{
		return Math.Max(lo, Math.Min(hi, x));
	}

	public static int MoteCount(double particles)
	{
		return Math.Max(2, Math.Min(4, (int)Math.Round(3.0 * Clamp(particles, 0.0, 1.34))));
	}

	private static void Quad(Buffer b, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3, double alpha, bool soft = false)
	{
		if (b.Quads >= 40)
		{
			throw new InvalidOperationException("bounded_contact_topology_overflow");
		}
		int n = b.Quads++ * 4;
		b.X[n] = x0;
		b.Y[n] = y0;
		b.X[n + 1] = x1;
		b.Y[n + 1] = y1;
		b.X[n + 2] = x2;
		b.Y[n + 2] = y2;
		b.X[n + 3] = x3;
		b.Y[n + 3] = y3;
		for (int i = 0; i < 4; i++)
		{
			b.U[n + i] = ((i == 1 || i == 2) ? 1 : 0);
			b.V[n + i] = ((i >= 2) ? 1 : 0);
			b.Alpha[n + i] = alpha;
			b.Soft[n + i] = (soft ? 1 : 0);
		}
	}

	private static void SoftBox(Buffer b, double x, double y, double w, double h, double alpha)
	{
		Quad(b, x - w, y - h, x + w, y - h, x + w, y + h, x - w, y + h, alpha, soft: true);
	}

	private static void Point(Buffer b, double x, double y, double dx, double dy, double w, double alpha)
	{
		double len = Math.Sqrt(dx * dx + dy * dy);
		double px = (0.0 - dy) / len * w;
		double py = dx / len * w;
		Quad(b, x + px, y + py, x - px, y - py, x + dx, y + dy, x + dx, y + dy, alpha);
	}

	private static void Segment(Buffer b, double x0, double y0, double x1, double y1, double width, double alpha)
	{
		double dx = x1 - x0;
		double dy = y1 - y0;
		double len = Math.Sqrt(dx * dx + dy * dy);
		if (!(len <= 1E-09))
		{
			double px = (0.0 - dy) / len * width;
			double py = dx / len * width;
			Quad(b, x0 - px, y0 - py, x1 - px, y1 - py, x1 + px, y1 + py, x0 + px, y0 + py, alpha);
		}
	}

	private static void Arc(Buffer b, double cx, double cy, double radius, double start, double span, double width, double alpha)
	{
		for (int i = 0; i < 10; i++)
		{
			double a = start + span * (double)i / 10.0;
			double c = start + span * (double)(i + 1) / 10.0;
			Segment(b, cx + Math.Cos(a) * radius, cy + Math.Sin(a) * radius, cx + Math.Cos(c) * radius, cy + Math.Sin(c) * radius, width * Math.Sin(Math.PI * ((double)i + 0.5) / 10.0), alpha);
		}
	}

	public static void Sample(Buffer b, Profile profile, double age, int motes)
	{
		Array.Clear(b.Alpha, 0, b.Alpha.Length);
		b.Quads = 0;
		b.Particles = 0;
		if (profile == null || !Finite(age) || age < 0.0 || age >= 0.18)
		{
			return;
		}
		double q = age / 0.18;
		double fade = (1.0 - q) * (1.0 - q);
		double s = 0.8 + 0.2 * Math.Sin(Math.PI * Math.Min(1.0, q * 2.0));
		switch (profile.Form)
		{
		case Shape.Neutral:
			SoftBox(b, 0.0, 0.0, 0.11 * s, 0.1 * s, 0.76 * fade);
			break;
		case Shape.Point:
			Point(b, -0.15 * (1.0 - q * 0.15), -0.018, 0.235, 0.035, 0.055 * s, 0.9 * fade);
			break;
		case Shape.Pair:
			Point(b, -0.12, 0.105 * (1.0 - q * 0.35), 0.165, -0.09, 0.033, 0.9 * fade);
			Point(b, 0.12, -0.105 * (1.0 - q * 0.35), -0.165, 0.09, 0.033, 0.9 * fade);
			break;
		case Shape.Wind:
		case Shape.Sweep:
		{
			for (int m = 0; m < 3; m++)
			{
				SoftBox(b, -0.025 + q * 0.025, (double)(m - 1) * 0.052, 0.17 * (1.0 - 0.1 * (double)m), 0.021, 0.48 * fade);
			}
			break;
		}
		case Shape.Slash:
			Arc(b, 0.0, -0.085, 0.2, -0.1, 2.05, 0.025, 0.88 * fade);
			break;
		case Shape.Blunt:
			SoftBox(b, 0.0, 0.0, 0.15 * (1.0 - 0.22 * q), 0.105 * (1.0 + 0.18 * q), 0.86 * fade);
			break;
		case Shape.Bind:
		case Shape.Engulf:
		{
			double r = 0.16 - 0.04 * q;
			Arc(b, -0.025, 0.0, r, 0.7, 1.75, (profile.Form == Shape.Bind) ? 0.018 : 0.035, 0.75 * fade);
			Arc(b, 0.025, 0.0, r, 3.84, 1.75, (profile.Form == Shape.Bind) ? 0.018 : 0.035, 0.75 * fade);
			break;
		}
		case Shape.Column:
		{
			for (int l = 0; l < 3; l++)
			{
				Point(b, (double)(l - 1) * 0.065, -0.13, 0.018 * Math.Sin(q * 4.0 + (double)l), 0.3 - (double)Math.Abs(l - 1) * 0.06, 0.05, 0.72 * fade);
			}
			break;
		}
		case Shape.Smoke:
		{
			for (int k = 0; k < 3; k++)
			{
				SoftBox(b, (double)(k - 1) * 0.058, 0.025 * (double)k + q * 0.025, 0.09 + q * 0.018, 0.08 + q * 0.02, 0.2 * fade);
			}
			break;
		}
		case Shape.Curve:
			Arc(b, 0.0, 0.0, 0.16, 0.2, 2.5, 0.025, 0.8 * fade);
			break;
		case Shape.Break:
		{
			for (int j = 0; j < 3; j++)
			{
				double a = (double)(j * 2) * Math.PI / 3.0;
				double x2 = Math.Cos(a);
				double y = Math.Sin(a);
				Point(b, x2 * (0.025 + 0.03 * q), y * (0.025 + 0.03 * q), x2 * 0.1, y * 0.1, 0.028, 0.82 * fade);
			}
			break;
		}
		case Shape.Object:
			Quad(b, -0.085, -0.06, 0.073, -0.068, 0.095, 0.048, -0.059, 0.082, 0.75 * fade);
			break;
		case Shape.CurvedObject:
			Segment(b, -0.13, -0.05, 0.0, 0.055, 0.032, 0.82 * fade);
			Segment(b, 0.0, 0.055, 0.13, -0.05, 0.032, 0.82 * fade);
			break;
		case Shape.Whip:
		{
			for (int i = 0; i < 12; i++)
			{
				double x = ((double)i / 12.0 - 0.5) * 0.38;
				double xx = ((double)(i + 1) / 12.0 - 0.5) * 0.38;
				Segment(b, x, Math.Sin((double)i / 12.0 * Math.PI * 2.0) * 0.055, xx, Math.Sin((double)(i + 1) / 12.0 * Math.PI * 2.0) * 0.055, 0.009, 0.8 * fade);
			}
			break;
		}
		case Shape.Curse:
			SoftBox(b, 0.0, 0.0, 0.13 * s, 0.115 * s, 0.78 * fade);
			break;
		}
		motes = Math.Max(2, Math.Min(4, motes));
		b.Particles = motes;
		for (int n = 0; n < motes; n++)
		{
			double a2 = (double)n * Math.PI * 2.0 / (double)motes + 0.35;
			double travel = 0.095 + 0.045 * q;
			SoftBox(b, Math.Cos(a2) * travel, Math.Sin(a2) * travel, 0.011 * (1.0 - 0.4 * q), 0.011 * (1.0 - 0.4 * q), 0.62 * fade);
		}
	}
}
