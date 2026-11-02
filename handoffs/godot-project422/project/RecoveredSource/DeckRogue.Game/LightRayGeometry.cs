using System;

namespace DeckRogue.Game;

internal static class LightRayGeometry
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
				Role[i] = 2;
			}
		}
	}

	public sealed class Buffer
	{
		public readonly MeshData[] Batches;

		public readonly bool[] AiryActive;

		public readonly bool[] MoteActive;

		public readonly Point[] MoteCenters;

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		public MeshData Main => Batches[0];

		public MeshData Airy => Batches[1];

		public MeshData Core => Batches[2];

		public MeshData Motes => Batches[3];

		internal Buffer()
		{
			Batches = new MeshData[4]
			{
				new MeshData(99, 128, 0, 2.5),
				new MeshData(81, 96, 1, 1.8),
				new MeshData(9, 8, 2, 3.6),
				new MeshData(40, 20, 3, 1.8)
			};
			AiryActive = new bool[3];
			MoteActive = new bool[10];
			MoteCenters = new Point[10];
		}
	}

	public const int RayKind = 0;

	public const int AiryKind = 1;

	public const int CoreKind = 2;

	public const int SolidKind = 3;

	public const int ShadowRole = 0;

	public const int PrimaryRole = 1;

	public const int EdgeRole = 2;

	public const int CoreRole = 3;

	public const int VertexCount = 229;

	public const int TriangleCount = 252;

	public const int BatchCount = 4;

	public const int RayRows = 33;

	public const int RayColumns = 3;

	public const int AiryCount = 3;

	public const int MoteCount = 10;

	public const double NativeLead = 0.1;

	public const double VisibleEnd = 0.2;

	public const double MainEnd = 0.085;

	public const double Angle = -0.16;

	private static readonly double Cos = Math.Cos(-0.16);

	private static readonly double Sin = Math.Sin(-0.16);

	private static readonly double[] AiryBirths = new double[3] { 0.0, 0.011, 0.024 };

	private static readonly double[] AiryLives = new double[3] { 0.155, 0.172, 0.148 };

	private static readonly double[] AiryY = new double[3] { -0.068, 0.095, 0.028 };

	private static readonly double[] AiryLength = new double[3] { 0.3, 0.24, 0.18 };

	private static readonly double[] AiryWidth = new double[3] { 0.01, 0.007, 0.008 };

	private static readonly double[] AiryAngle = new double[3] { -0.035, 0.09, -0.09 };

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

	private static double Clamp(double x)
	{
		return Math.Max(0.0, Math.Min(1.0, x));
	}

	private static double Smooth(double x)
	{
		double q = Clamp(x);
		return q * q * (3.0 - 2.0 * q);
	}

	private static double Fade(double t, double hold, double end)
	{
		if (!(0.0 <= t) || !(t < end))
		{
			return 0.0;
		}
		return 1.0 - Smooth((t - hold) / (end - hold));
	}

	private static double SafeAge(double age)
	{
		if (double.IsNaN(age) || age < -1000000.0)
		{
			return -0.1;
		}
		if (!(age > 1000000.0))
		{
			return age;
		}
		return 0.2;
	}

	public static double Clock(double age, double availableAnticipation = double.NaN)
	{
		age = SafeAge(age);
		if (age >= 0.0)
		{
			return age;
		}
		double a = (double.IsNaN(availableAnticipation) ? 0.1 : Math.Min(0.1, Math.Max(0.0, availableAnticipation)));
		if (a <= 0.0)
		{
			return double.NaN;
		}
		if (age < 0.0 - a)
		{
			return -0.1;
		}
		double q = age / a;
		return age - (0.1 - a) * (q * q);
	}

	public static double VisualClock(double age, double availableAnticipation = double.NaN)
	{
		return Clock(age, availableAnticipation);
	}

	public static double Light(double visualAge)
	{
		double t = SafeAge(visualAge);
		if (t >= 0.0)
		{
			return Fade(t, 0.022, 0.11);
		}
		double q = Smooth((t + 0.1) / 0.1);
		return 0.018 * q * q * q;
	}

	public static double Head(double visualAge)
	{
		double t = SafeAge(visualAge);
		if (t <= 0.0)
		{
			double q = Clamp((t + 0.1) / 0.1);
			return -0.9 * (1.0 - (1.2 * q * q - 0.2 * q * q * q));
		}
		return 16.2 * (1.0 - Math.Exp(-24.0 * t)) / 24.0;
	}

	private static Point Rotate(double x, double y, double z)
	{
		return new Point(Cos * x - Sin * y, Sin * x + Cos * y, z);
	}

	private static Point Rotate(double x, double y, double z, double angle)
	{
		double c = Math.Cos(angle);
		double s = Math.Sin(angle);
		return new Point(c * x - s * y, s * x + c * y, z);
	}

	private static void Quad(int[] triangles, int q, int a, int b, int c, int d)
	{
		int n = q * 6;
		triangles[n] = a;
		triangles[n + 1] = b;
		triangles[n + 2] = c;
		triangles[n + 3] = a;
		triangles[n + 4] = c;
		triangles[n + 5] = d;
	}

	public static Buffer Create()
	{
		Buffer b = new Buffer();
		for (int row = 0; row < 33; row++)
		{
			for (int col = 0; col < 3; col++)
			{
				double u = (double)row / 32.0;
				double side = col - 1;
				b.Main.UV[row * 3 + col] = new Uv((528.0 + side * 32.0) / 1774.0, 1.0 - (835.0 - 740.0 * u) / 887.0);
				if (row < 32 && col < 2)
				{
					int n = row * 3 + col;
					Quad(b.Main.Triangles, row * 2 + col, n, n + 1, n + 4, n + 3);
				}
			}
		}
		for (int k = 0; k < 3; k++)
		{
			for (int i = 0; i < 9; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					int n2 = k * 27 + i * 3 + j;
					b.Airy.UV[n2] = new Uv((double)i / 8.0, (double)j / 2.0);
					if (i < 8 && j < 2)
					{
						Quad(b.Airy.Triangles, k * 16 + i * 2 + j, n2, n2 + 1, n2 + 4, n2 + 3);
					}
				}
			}
		}
		b.Core.Vertices[0] = new Point(0.0, 0.0, 0.064);
		b.Core.UV[0] = new Uv(0.5, 0.5);
		for (int l = 0; l < 8; l++)
		{
			double c = Math.Cos((double)l * Math.PI / 4.0);
			double s = Math.Sin((double)l * Math.PI / 4.0);
			b.Core.Vertices[l + 1] = Rotate(0.14 * c, 0.026 * s, 0.06);
			b.Core.UV[l + 1] = new Uv(0.5 + 0.5 * c, 0.5 + 0.5 * s);
			b.Core.Triangles[l * 3] = 0;
			b.Core.Triangles[l * 3 + 1] = l + 1;
			b.Core.Triangles[l * 3 + 2] = 1 + (l + 1) % 8;
		}
		for (int m = 0; m < 10; m++)
		{
			Quad(b.Motes.Triangles, m, m * 4, m * 4 + 1, m * 4 + 2, m * 4 + 3);
			for (int num = 0; num < 4; num++)
			{
				b.Motes.Role[m * 4 + num] = ((m % 3 == 0) ? 3 : 2);
			}
		}
		return b;
	}

	private static void Ray(Buffer b, double t)
	{
		double h = Head(t);
		double alpha = ((-0.1 <= t && t < 0.085) ? (Smooth((t + 0.1) / 0.024) * (1.0 - Smooth(Math.Max(0.0, t - 0.02) / 0.065))) : 0.0);
		for (int row = 0; row < 33; row++)
		{
			double u = (double)row / 32.0;
			double x = h - 0.78 * (1.0 - u);
			double w = 0.027 * Math.Pow(Math.Sin(Math.PI * u), 0.55);
			for (int col = 0; col < 3; col++)
			{
				int n = row * 3 + col;
				double side = col - 1;
				b.Main.Vertices[n] = Rotate(x, side * w, 0.026 + 0.003 * (1.0 - side * side));
				b.Main.Alpha[n] = alpha;
			}
		}
	}

	private static void AfterRay(Buffer b, double t, int k)
	{
		double age = Math.Max(0.0, t - AiryBirths[k]);
		double life = AiryLives[k];
		double x = 0.025 + (2.1 + 0.3 * (double)k) * (1.0 - Math.Exp(-8.0 * age)) / 8.0;
		double y = AiryY[k] * (1.0 + 0.35 * age);
		double length = AiryLength[k] * (1.0 - 0.65 * Clamp(age / life));
		double alpha = 0.58 * Smooth((t - AiryBirths[k]) / 0.012) * Fade(t - AiryBirths[k], 0.022, life);
		b.AiryActive[k] = alpha > 0.0;
		for (int row = 0; row < 9; row++)
		{
			double u = (double)row / 8.0;
			for (int col = 0; col < 3; col++)
			{
				int n = k * 27 + row * 3 + col;
				double side = col - 1;
				b.Airy.Vertices[n] = Rotate(x + (u - 0.5) * length, y + side * AiryWidth[k] * Math.Sin(Math.PI * u), 0.035 + 0.003 * (double)k, -0.16 + AiryAngle[k]);
				b.Airy.Alpha[n] = alpha;
			}
		}
	}

	private static void Mote(Buffer b, double t, int i)
	{
		double born = 0.004 + 0.004 * (double)i;
		double age = Math.Max(0.0, t - born);
		double life = 0.132 + 0.012 * (double)(i % 4);
		double alpha = Smooth((t - born) / 0.01) * Fade(t - born, 0.025, life);
		double phi = -0.46 + 0.16 * (double)(i % 6);
		double v = 1.2 + 0.19 * (double)(i % 4);
		double q = (1.0 - Math.Exp(-5.0 * age)) / 5.0;
		double x = 0.025 + Math.Cos(phi) * v * q;
		double y = (double)(i % 3 - 1) * 0.018 + Math.Sin(phi) * v * q;
		double length = (0.024 + 0.005 * (double)(i % 3)) * (1.0 - 0.5 * Clamp(age / life));
		b.MoteCenters[i] = Rotate(x, y, 0.051);
		b.MoteActive[i] = alpha > 0.0;
		for (int j = 0; j < 4; j++)
		{
			double a = j switch
			{
				2 => length / 2.0, 
				0 => (0.0 - length) / 2.0, 
				_ => 0.0, 
			};
			double w = j switch
			{
				3 => 0.0035, 
				1 => -0.0035, 
				_ => 0.0, 
			};
			int n = i * 4 + j;
			b.Motes.Vertices[n] = Rotate(x + a, y + w, 0.051);
			b.Motes.Alpha[n] = alpha;
		}
	}

	public static void Sample(Buffer b, double age, double availableAnticipation = double.NaN)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		double mapped = Clock(age, availableAnticipation);
		double t = (b.VisualAge = (double.IsNaN(mapped) ? (-0.1) : mapped));
		b.LightEnvelope = Light(t);
		b.TargetAreaW = 60.0 * b.LightEnvelope;
		b.FloorAreaW = 20.0 * b.LightEnvelope;
		Ray(b, t);
		for (int k = 0; k < 3; k++)
		{
			AfterRay(b, t, k);
		}
		double coreAlpha = Fade(t, 0.026, 0.072);
		for (int i = 0; i < 9; i++)
		{
			b.Core.Alpha[i] = coreAlpha;
		}
		for (int j = 0; j < 10; j++)
		{
			Mote(b, t, j);
		}
	}
}
