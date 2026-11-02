using System;

namespace DeckRogue.Game;

internal static class EarthStompGeometry
{
	public struct Point(double x, double y, double z)
	{
		public double X = x;

		public double Y = y;

		public double Z = z;
	}

	public struct Uv(double x, double y)
	{
		public double X = x;

		public double Y = y;
	}

	public sealed class MeshData
	{
		public readonly int Kind;

		public readonly int Space;

		public readonly Point[] Vertices;

		public readonly Point[] Normals;

		public readonly Point[] GroundAnchors;

		public readonly Uv[] UV;

		public readonly double[] Alpha;

		public readonly double[] Gain;

		public readonly double[] Phase;

		public readonly int[] Role;

		public readonly int[] Triangles;

		internal MeshData(int vertices, int triangles, int kind, int space)
		{
			Kind = kind;
			Space = space;
			Vertices = new Point[vertices];
			Normals = new Point[vertices];
			GroundAnchors = new Point[vertices];
			UV = new Uv[vertices];
			Alpha = new double[vertices];
			Gain = new double[vertices];
			Phase = new double[vertices];
			Role = new int[vertices];
			Triangles = new int[triangles * 3];
			for (int i = 0; i < vertices; i++)
			{
				Gain[i] = ((kind == 0) ? 0.22 : 0.65);
				Role[i] = ((kind == 0) ? 1 : 2);
			}
		}
	}

	public sealed class Buffer
	{
		public readonly int Family;

		public readonly MeshData Rocks;

		public readonly MeshData Dust;

		public readonly MeshData[] Batches;

		public readonly Point[] BirthOffsets;

		public readonly Point[] ParticleCenters;

		public readonly bool[] ParticleActive;

		public Point GravityUp;

		public double VisualAge;

		public double GroundAlpha;

		public double Front;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		public double CapturedHfx { get; private set; }

		internal Buffer(int family)
		{
			Family = family;
			Rocks = new MeshData((family == 0) ? 96 : 48, (family == 0) ? 32 : 16, 0, 0);
			Dust = ((family == 1) ? new MeshData(32, 16, 1, 1) : null);
			Batches = ((family == 0) ? new MeshData[1] { Rocks } : new MeshData[2] { Rocks, Dust });
			BirthOffsets = new Point[(family == 0) ? 8 : 12];
			ParticleCenters = new Point[BirthOffsets.Length];
			ParticleActive = new bool[BirthOffsets.Length];
			GravityUp = new Point(0.0, 1.0, 0.0);
			CapturedHfx = 1.0;
			for (int i = 0; i < Rocks.Triangles.Length; i++)
			{
				Rocks.Triangles[i] = i;
			}
			if (Dust != null)
			{
				for (int j = 0; j < 8; j++)
				{
					int v = 4 * j;
					int f = 6 * j;
					Dust.Triangles[f] = v;
					Dust.Triangles[f + 1] = v + 1;
					Dust.Triangles[f + 2] = v + 2;
					Dust.Triangles[f + 3] = v;
					Dust.Triangles[f + 4] = v + 2;
					Dust.Triangles[f + 5] = v + 3;
					Dust.UV[v] = new Uv(0.0, 0.0);
					Dust.UV[v + 1] = new Uv(1.0, 0.0);
					Dust.UV[v + 2] = new Uv(1.0, 1.0);
					Dust.UV[v + 3] = new Uv(0.0, 1.0);
				}
			}
		}

		public void SetCapture(Point[] offsets, Point gravityUp, double hfx)
		{
			if (offsets == null || offsets.Length != BirthOffsets.Length)
			{
				throw new ArgumentException("One full XYZ offset per declared birth is required.", "offsets");
			}
			if (!Finite(gravityUp) || !Finite(hfx) || hfx <= 0.0)
			{
				throw new ArgumentException("Finite gravity and positive captured Hfx required.");
			}
			for (int i = 0; i < offsets.Length; i++)
			{
				if (!Finite(offsets[i]))
				{
					throw new ArgumentException("Birth offsets must be finite.", "offsets");
				}
			}
			Array.Copy(offsets, BirthOffsets, offsets.Length);
			GravityUp = gravityUp;
			CapturedHfx = hfx;
		}
	}

	public const int Earth = 0;

	public const int Stomp = 1;

	public const int GroundSpace = 0;

	public const int GroundBillboardSpace = 1;

	public const int RockKind = 0;

	public const int DustKind = 1;

	public const int PrimaryRole = 1;

	public const int EdgeRole = 2;

	public const int EarthRockVertices = 96;

	public const int EarthRockTriangles = 32;

	public const int StompRockVertices = 48;

	public const int StompRockTriangles = 16;

	public const int StompDustVertices = 32;

	public const int StompDustTriangles = 16;

	private static readonly Point[] Births = new Point[8]
	{
		new Point(0.27, 0.0, 0.06),
		new Point(0.57, 0.0, 0.01),
		new Point(0.46, 0.0, 0.25),
		new Point(0.72, 0.0, -0.18),
		new Point(-0.28, 0.0, 0.04),
		new Point(-0.55, 0.0, -0.06),
		new Point(-0.42, 0.0, 0.26),
		new Point(-0.09, 0.0, -0.44)
	};

	private static readonly Point[] TetraBase = new Point[4]
	{
		new Point(-0.9, 0.0, -0.42),
		new Point(0.85, 0.0, -0.34),
		new Point(0.05, 0.0, 0.78),
		new Point(0.12, 1.12, 0.06)
	};

	private static readonly int[] TetraIndices = new int[12]
	{
		0, 1, 2, 0, 3, 1, 1, 3, 2, 2,
		3, 0
	};

	public static bool Finite(double x)
	{
		if (!double.IsNaN(x))
		{
			return !double.IsInfinity(x);
		}
		return false;
	}

	private static bool Finite(Point p)
	{
		if (Finite(p.X) && Finite(p.Y))
		{
			return Finite(p.Z);
		}
		return false;
	}

	private static void CheckFamily(int family)
	{
		if (family != 0 && family != 1)
		{
			throw new ArgumentOutOfRangeException("family");
		}
	}

	public static double Clamp(double x)
	{
		return Math.Max(0.0, Math.Min(1.0, x));
	}

	public static double Smooth(double x)
	{
		double q = Clamp(x);
		return q * q * (3.0 - 2.0 * q);
	}

	public static double Envelope(double t, double birth, double life, double rise = 0.01)
	{
		if (!(birth <= t) || !(t <= birth + life))
		{
			return 0.0;
		}
		return Smooth((t - birth) / rise) * (1.0 - Smooth((t - birth - life * 0.55) / (life * 0.45)));
	}

	public static double Lead(int family)
	{
		CheckFamily(family);
		if (family != 0)
		{
			return 0.0;
		}
		return 0.14;
	}

	public static double End(int family)
	{
		CheckFamily(family);
		if (family != 0)
		{
			return 0.267;
		}
		return 0.42;
	}

	public static double End(Buffer b)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		return End(b.Family);
	}

	public static double? NativeAge(double age, double availableA, double nativeP = 0.14)
	{
		if (!Finite(age) || !Finite(availableA) || !Finite(nativeP) || nativeP < 0.0)
		{
			throw new ArgumentException("Finite clock inputs and nonnegative native P required.");
		}
		if (age >= 0.0)
		{
			return age;
		}
		double a = Math.Min(nativeP, Math.Max(0.0, availableA));
		if (a <= 0.0)
		{
			return null;
		}
		if (age < 0.0 - a)
		{
			return 0.0 - nativeP;
		}
		return age - (nativeP - a) * (age / a) * (age / a);
	}

	public static Buffer Create(int family)
	{
		CheckFamily(family);
		return new Buffer(family);
	}

	public static Point BirthTangent(int family, int index)
	{
		CheckFamily(family);
		if (index < 0 || index >= ((family == 0) ? 8 : 12))
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (family == 0)
		{
			return Births[index];
		}
		double angle = ((index < 4) ? (0.35 + (double)index * 0.8) : (0.2 + (double)(index - 4) * 0.38));
		double r = ((index < 4) ? 0.16 : 0.14);
		return new Point(r * Math.Cos(angle), 0.0, r * Math.Sin(angle));
	}

	private static Point Grounded(Buffer b, Point p, int index, double a, double g)
	{
		Point o = b.BirthOffsets[index];
		Point u = b.GravityUp;
		return new Point(p.X + o.X - g * a * a * u.X, p.Y + o.Y - g * a * a * u.Y, p.Z + o.Z - g * a * a * u.Z);
	}

	private static Point Cross(Point a, Point b)
	{
		return new Point(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
	}

	private static Point Sub(Point a, Point b)
	{
		return new Point(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	}

	private static void Tetra(Buffer b, int particle, Point c, double size, double rotation, double alpha)
	{
		MeshData m = b.Rocks;
		int v = particle * 12;
		double ca = Math.Cos(rotation);
		double sa = Math.Sin(rotation);
		for (int j = 0; j < 12; j++)
		{
			Point p = TetraBase[TetraIndices[j]];
			m.Vertices[v + j] = new Point(c.X + size * (p.X * ca - p.Z * sa), c.Y + size * p.Y, c.Z + size * (p.X * sa + p.Z * ca));
			m.Alpha[v + j] = alpha;
		}
		for (int i = 0; i < 12; i += 3)
		{
			Point n = Cross(Sub(m.Vertices[v + i + 1], m.Vertices[v + i]), Sub(m.Vertices[v + i + 2], m.Vertices[v + i]));
			double length = Math.Sqrt(n.X * n.X + n.Y * n.Y + n.Z * n.Z);
			n = new Point(n.X / length, n.Y / length, n.Z / length);
			m.Normals[v + i] = n;
			m.Normals[v + i + 1] = n;
			m.Normals[v + i + 2] = n;
		}
		b.ParticleCenters[particle] = c;
		b.ParticleActive[particle] = alpha > 0.0;
	}

	private static void DustQuad(Buffer b, int index, Point anchor, double width, double height, double angle, double alpha)
	{
		MeshData m = b.Dust;
		int v = 4 * index;
		double ca = Math.Cos(angle);
		double sa = Math.Sin(angle);
		for (int j = 0; j < 4; j++)
		{
			double x = ((j == 0 || j == 3) ? (0.0 - width) : width);
			double y = ((j < 2) ? (0.0 - height) : height);
			m.Vertices[v + j] = new Point(ca * x - sa * y, sa * x + ca * y, 0.0);
			m.Normals[v + j] = new Point(0.0, 0.0, 1.0);
			m.GroundAnchors[v + j] = anchor;
			m.Alpha[v + j] = alpha;
			m.Phase[v + j] = (double)index * 0.37;
		}
		b.ParticleCenters[index + 4] = anchor;
		b.ParticleActive[index + 4] = alpha > 0.0;
	}

	public static void Sample(Buffer b, double t)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		if (!Finite(t))
		{
			throw new ArgumentOutOfRangeException("t");
		}
		b.VisualAge = t;
		if (b.Family == 0)
		{
			b.GroundAlpha = Smooth((t + 0.14) / 0.04) * (1.0 - Smooth((t - 0.23) / 0.19));
			b.Front = ((t < 0.0) ? (0.115 + 0.12 * Smooth((t + 0.14) / 0.14)) : (0.235 + 0.75 * Smooth(t / 0.06)));
			for (int i = 0; i < 8; i++)
			{
				double x = Births[i].X;
				double z = Births[i].Z;
				double birth = 0.012 + 0.015 * (double)i;
				double a = Math.Max(0.0, t - birth);
				double angle = Math.Atan2(z, x);
				double drift = 0.3 + 0.04 * (double)(i % 3);
				Point c = Grounded(b, new Point(x + Math.Cos(angle) * drift * a, 1.12 * a, z + Math.Sin(angle) * drift * a), i, a, 4.0);
				Tetra(b, i, c, 0.027 + 0.01 * (double)(i % 3), (double)i * 0.73 + a * ((i % 2 != 0) ? 2.3 : (-2.8)), Envelope(t, birth, 0.23, 0.012));
			}
			ref double lightEnvelope = ref b.LightEnvelope;
			double num;
			if (!(-0.14 <= t) || !(t <= 0.21))
			{
				num = 0.0;
			}
			else
			{
				num = ((t < 0.0) ? (0.025 * Smooth((t + 0.14) / 0.14)) : (1.0 - Smooth((t - 0.026) / 0.184)));
			}
			lightEnvelope = num;
		}
		else
		{
			b.GroundAlpha = Envelope(t, 0.0, 0.18, 0.008);
			b.Front = 0.0;
			for (int j = 0; j < 4; j++)
			{
				double theta = 0.35 + (double)j * 0.8;
				double birth2 = 0.016 + 0.004 * (double)j;
				double a2 = Math.Max(0.0, t - birth2);
				double r = 0.16 + (0.65 + 0.05 * (double)j) * a2;
				Point c2 = Grounded(b, new Point(r * Math.Cos(theta), 0.96 * a2, r * Math.Sin(theta)), j, a2, 4.0);
				Tetra(b, j, c2, 0.02 + 0.004 * (double)(j % 2), (double)j + a2 * 4.0, Envelope(t, birth2, 0.204));
			}
			for (int k = 0; k < 8; k++)
			{
				double theta2 = 0.2 + (double)k * 0.38;
				double birth3 = 0.008 + 0.003 * (double)(k % 4);
				double a3 = Math.Max(0.0, t - birth3);
				double r2 = 0.14 + (0.85 + 0.1 * (double)(k % 3)) * a3;
				Point c3 = Grounded(b, new Point(r2 * Math.Cos(theta2), 0.018 + 0.68 * a3, r2 * Math.Sin(theta2)), k + 4, a3, 2.4);
				DustQuad(b, k, c3, 0.09 + 0.16 * a3, 0.043 + 0.13 * a3, 0.23 * Math.Sin(k), 0.6 * Envelope(t, birth3, 0.25, 0.018));
			}
			b.LightEnvelope = ((0.0 <= t && t <= 0.145) ? (1.0 - Smooth((t - 0.018) / 0.127)) : 0.0);
		}
		b.TargetAreaW = b.LightEnvelope * (double)((b.Family == 0) ? 35 : 30);
		b.FloorAreaW = b.LightEnvelope * (double)((b.Family == 0) ? 23 : 20);
	}

	public static bool SampleReceipt(Buffer b, double age, double availableA)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		double? native = NativeAge(age, availableA, Lead(b.Family));
		if (native.HasValue)
		{
			Sample(b, native.Value);
			return true;
		}
		b.VisualAge = age;
		b.GroundAlpha = (b.Front = (b.LightEnvelope = (b.TargetAreaW = (b.FloorAreaW = 0.0))));
		for (int j = 0; j < b.Batches.Length; j++)
		{
			Array.Clear(b.Batches[j].Alpha, 0, b.Batches[j].Alpha.Length);
		}
		Array.Clear(b.ParticleActive, 0, b.ParticleActive.Length);
		return false;
	}
}
