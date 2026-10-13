using System;

namespace DeckRogue.Game;

internal static class LightningDischargeGeometry
{
	public struct Point
	{
		public double X;

		public double Y;

		public double Z;

		public Point(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}
	}

	public struct Uv
	{
		public double X;

		public double Y;

		public Uv(double x, double y)
		{
			X = x;
			Y = y;
		}
	}

	public sealed class MeshData
	{
		public readonly int Kind;

		public readonly Point[] Vertices;

		public readonly Uv[] UV;

		public readonly double[] Alpha;

		public readonly double[] Gain;

		public readonly int[] Role;

		public readonly int[] Triangles;

		internal MeshData(int vertices, int triangles, int kind, double gain)
		{
			Kind = kind;
			Vertices = new Point[vertices];
			UV = new Uv[vertices];
			Alpha = new double[vertices];
			Gain = new double[vertices];
			Role = new int[vertices];
			Triangles = new int[triangles * 3];
			for (int i = 0; i < vertices; i++)
			{
				Gain[i] = gain;
				Role[i] = 1;
			}
		}
	}

	public sealed class Buffer
	{
		public readonly MeshData Branches;

		public readonly MeshData Core;

		public readonly MeshData Sparks;

		public readonly MeshData[] Batches;

		public readonly bool[] ParticleActive;

		public readonly Point[] ParticleCenters;

		public readonly int MirrorRight;

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		internal Buffer(int mirrorRight)
		{
			MirrorRight = mirrorRight;
			Branches = new MeshData(4, 2, 0, 2.35);
			Core = new MeshData(13, 12, 1, 3.0);
			Sparks = new MeshData(48, 24, 2, 2.1);
			Batches = new MeshData[3] { Branches, Core, Sparks };
			ParticleActive = new bool[12];
			ParticleCenters = new Point[12];
		}
	}

	public const int BranchKind = 0;

	public const int CoreKind = 1;

	public const int SparkKind = 2;

	public const int ShadowRole = 0;

	public const int PrimaryRole = 1;

	public const int EdgeRole = 2;

	public const int CoreRole = 3;

	public const int BatchCount = 3;

	public const int VertexCount = 65;

	public const int TriangleCount = 38;

	public const int ParticleCount = 12;

	public const double NativeLead = 0.1;

	public const double VisibleEnd = 0.2;

	public const double BranchEnd = 0.11;

	public const double CoreEnd = 0.06;

	public const double LightEnd = 0.075;

	public const double Size = 1.5;

	public const double PivotX = 0.5377677564825254;

	public const double PivotY = 0.5016910935738443;

	private static readonly double[] Angles = new double[12]
	{
		2.35, 2.57, 3.0, 3.48, 3.92, 4.55, 4.85, 5.55, 5.92, 0.42,
		0.88, 1.18
	};

	public static bool Finite(double x)
	{
		return !double.IsNaN(x) && !double.IsInfinity(x);
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

	public static double Lead()
	{
		return 0.1;
	}

	public static double End()
	{
		return 0.2;
	}

	public static double End(Buffer b)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		return 0.2;
	}

	public static double? NativeAge(double age, double availableA, double nativeP = 0.1)
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
		return age - (nativeP - a) * Math.Pow(age / a, 2.0);
	}

	private static void Quad(MeshData m, int index)
	{
		int v = index * 4;
		int f = index * 6;
		m.Triangles[f] = v;
		m.Triangles[f + 1] = v + 1;
		m.Triangles[f + 2] = v + 2;
		m.Triangles[f + 3] = v;
		m.Triangles[f + 4] = v + 2;
		m.Triangles[f + 5] = v + 3;
		m.UV[v] = new Uv(0.0, 0.0);
		m.UV[v + 1] = new Uv(1.0, 0.0);
		m.UV[v + 2] = new Uv(1.0, 1.0);
		m.UV[v + 3] = new Uv(0.0, 1.0);
	}

	public static Buffer Create(int mirrorRight = 1)
	{
		if (mirrorRight != 1 && mirrorRight != -1)
		{
			throw new ArgumentOutOfRangeException("mirrorRight");
		}
		Buffer b = new Buffer(mirrorRight);
		Quad(b.Branches, 0);
		for (int j = 0; j < 4; j++)
		{
			Uv uv = b.Branches.UV[j];
			b.Branches.Vertices[j] = new Point((double)mirrorRight * (uv.X - 0.5377677564825254) * 1.5, (uv.Y - 0.5016910935738443) * 1.5, 0.0);
			b.Branches.UV[j] = new Uv(uv.X * 0.5, uv.Y);
		}
		b.Core.Vertices[0] = new Point(0.0, 0.0, 0.025);
		b.Core.UV[0] = new Uv(0.5, 0.5);
		for (int i = 0; i < 12; i++)
		{
			double theta = (double)i * (Math.PI * 2.0) / 12.0;
			double ca = Math.Cos(theta);
			double sa = Math.Sin(theta);
			b.Core.Vertices[i + 1] = new Point((double)mirrorRight * 0.065 * ca, 0.065 * sa, 0.025);
			b.Core.UV[i + 1] = new Uv(0.5 + 0.5 * ca, 0.5 + 0.5 * sa);
			b.Core.Triangles[i * 3] = 0;
			b.Core.Triangles[i * 3 + 1] = i + 1;
			b.Core.Triangles[i * 3 + 2] = (i + 1) % 12 + 1;
			Quad(b.Sparks, i);
		}
		return b;
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
		double branches = ((-0.1 < t && t < 0.11) ? 1 : 0);
		for (int i = 0; i < 4; i++)
		{
			b.Branches.Alpha[i] = branches;
		}
		double core = ((t < 0.0) ? 0.0 : (1.0 - Smooth((t - 0.018) / 0.042)));
		for (int j = 0; j < 13; j++)
		{
			b.Core.Alpha[j] = core;
		}
		for (int k = 0; k < 12; k++)
		{
			double ang = Angles[k];
			double birth = 0.006 * (double)(k % 4);
			double life = 0.16 + 0.011 * (double)(k % 3);
			double age = Math.Max(0.0, t - birth);
			double alpha = ((birth <= t && t <= birth + life) ? (Smooth(age / 0.008) * (1.0 - Smooth((age - life * 0.46) / (life * 0.54)))) : 0.0);
			double originR = 0.13 + 0.035 * (double)(k % 5);
			double speed = 0.7 + 0.12 * (double)(k % 4);
			double dist = originR + speed * (1.0 - Math.Exp(-7.0 * age)) / 7.0;
			double ca = Math.Cos(ang);
			double sa = Math.Sin(ang);
			double x = ca * dist;
			double y = sa * dist - 0.4 * age * age;
			double length = (0.026 + 0.009 * (double)(k % 3)) * (1.0 - 0.6 * Smooth(age / life));
			double w = 0.005 + 0.0015 * (double)(k % 2);
			for (int l = 0; l < 4; l++)
			{
				double u = ((l == 0 || l == 3) ? (0.0 - length) : length);
				double v = ((l < 2) ? (0.0 - w) : w);
				b.Sparks.Vertices[4 * k + l] = new Point((double)b.MirrorRight * (x + ca * u - sa * v), y + sa * u + ca * v, 0.04);
				b.Sparks.Alpha[4 * k + l] = alpha;
			}
			b.ParticleCenters[k] = new Point((double)b.MirrorRight * x, y, 0.04);
			b.ParticleActive[k] = alpha > 0.0;
		}
		b.LightEnvelope = ((-0.1 <= t && t < 0.0) ? (0.018 * Smooth((t + 0.1) / 0.1)) : ((0.0 <= t && t <= 0.075) ? (1.0 - Smooth((t - 0.015) / 0.06)) : 0.0));
		b.TargetAreaW = 42.0 * b.LightEnvelope;
		b.FloorAreaW = 14.0 * b.LightEnvelope;
	}

	public static bool SampleReceipt(Buffer b, double age, double availableA)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		double? native = NativeAge(age, availableA);
		if (native.HasValue)
		{
			Sample(b, native.Value);
			return true;
		}
		b.VisualAge = age;
		b.LightEnvelope = (b.TargetAreaW = (b.FloorAreaW = 0.0));
		for (int i = 0; i < b.Batches.Length; i++)
		{
			Array.Clear(b.Batches[i].Alpha, 0, b.Batches[i].Alpha.Length);
		}
		Array.Clear(b.ParticleActive, 0, b.ParticleActive.Length);
		return false;
	}
}
