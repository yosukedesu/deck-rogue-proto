using System;

namespace DeckRogue.Game;

internal static class DarkDrainGeometry
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

		public readonly double[] Coverage;

		public readonly int[] Role;

		public readonly int[] Triangles;

		internal MeshData(int vertices, int triangles, int kind, double gain, int role)
		{
			Kind = kind;
			Vertices = new Point[vertices];
			UV = new Uv[vertices];
			Alpha = new double[vertices];
			Gain = new double[vertices];
			Coverage = new double[vertices];
			Role = new int[vertices];
			Triangles = new int[triangles * 3];
			for (int i = 0; i < vertices; i++)
			{
				Gain[i] = gain;
				Coverage[i] = 1.0;
				Role[i] = role;
			}
		}
	}

	public sealed class Buffer
	{
		public readonly MeshData[] Batches;

		public readonly bool[] IntakeActive;

		public readonly bool[] ReturnActive;

		public readonly Point[] IntakeCenters;

		public readonly Point[] ReturnCenters;

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		public MeshData Main => Batches[0];

		public MeshData Core => Batches[1];

		public MeshData Intake => Batches[2];

		public MeshData Return => Batches[3];

		public bool SourceValid { get; internal set; }

		public bool ReturnEnabled => SourceValid;

		public Point SourceH { get; internal set; }

		internal Buffer()
		{
			Batches = new MeshData[4]
			{
				new MeshData(330, 512, 0, 2.1, 1),
				new MeshData(9, 8, 1, 3.4, 3),
				new MeshData(32, 16, 2, 1.8, 2),
				new MeshData(48, 24, 2, 2.2, 2)
			};
			IntakeActive = new bool[8];
			ReturnActive = new bool[12];
			IntakeCenters = new Point[8];
			ReturnCenters = new Point[12];
		}
	}

	public const int TextureKind = 0;

	public const int CoreKind = 1;

	public const int SolidKind = 2;

	public const int ShadowRole = 0;

	public const int PrimaryRole = 1;

	public const int EdgeRole = 2;

	public const int CoreRole = 3;

	public const int RibbonRows = 33;

	public const int RibbonColumns = 5;

	public const int IntakeCount = 8;

	public const int ReturnCount = 12;

	public const int VertexCount = 419;

	public const int TriangleCount = 560;

	public const int BatchCount = 4;

	public const double NativeLead = 0.13;

	public const double BodyEnd = 0.092;

	public const double CoreEnd = 0.062;

	public const double ValidSourceEnd = 0.301;

	public const double InvalidSourceEnd = 0.104;

	private static readonly double[,] Curves = new double[2, 6]
	{
		{ -0.46, 0.3, -0.62, -0.37, 0.24, -0.12 },
		{ 0.18, 0.55, 0.58, 0.48, 0.22, 0.055 }
	};

	private static readonly double[] ReturnBirths = new double[12]
	{
		0.0, 0.006, 0.011, 0.019, 0.028, 0.035, 0.042, 0.052, 0.059, 0.066,
		0.074, 0.082
	};

	public static double Lead()
	{
		return 0.13;
	}

	public static double End()
	{
		return 0.301;
	}

	public static double End(bool sourceValid)
	{
		if (!sourceValid)
		{
			return 0.104;
		}
		return 0.301;
	}

	public static double End(Buffer b)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		return End(b.SourceValid);
	}

	private static bool Finite(double x)
	{
		if (!double.IsNaN(x))
		{
			return !double.IsInfinity(x);
		}
		return false;
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

	private static double Hypot(double x, double y)
	{
		x = Math.Abs(x);
		y = Math.Abs(y);
		double big = Math.Max(x, y);
		double small = Math.Min(x, y);
		if (big == 0.0)
		{
			return 0.0;
		}
		double q = small / big;
		return big * Math.Sqrt(1.0 + q * q);
	}

	private static double SafeAge(double age)
	{
		if (double.IsNaN(age) || age < -1000000.0)
		{
			return -0.130001;
		}
		if (age > 1000000.0)
		{
			return 0.301;
		}
		return age;
	}

	public static double Clock(double age, double availableAnticipation = double.NaN)
	{
		age = SafeAge(age);
		if (age >= 0.0 || double.IsNaN(availableAnticipation) || availableAnticipation >= 0.13)
		{
			return age;
		}
		if (availableAnticipation <= 0.0 || age < 0.0 - availableAnticipation)
		{
			return -0.130001;
		}
		double r = Clamp((0.0 - age) / availableAnticipation);
		return age - (0.13 - availableAnticipation) * r * r * (2.0 - r);
	}

	public static double VisualClock(double age, double availableAnticipation = double.NaN)
	{
		return Clock(age, availableAnticipation);
	}

	public static double Light(double visualAge)
	{
		double t = SafeAge(visualAge);
		double s = Smooth((t + 0.13) / 0.13);
		if (!(t >= 0.0))
		{
			return 0.045 * s * s;
		}
		return Fade(t, 0.022, 0.104);
	}

	public static void ConfigureSource(Buffer b, Point sourceH, bool sourceValid = true)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		bool flag = (b.SourceValid = sourceValid && Finite(sourceH.X) && Finite(sourceH.Y) && Finite(sourceH.Z) && Hypot(sourceH.X, sourceH.Y) > 0.05);
		bool valid = flag;
		b.SourceH = (valid ? sourceH : new Point(-1.1, 0.035, 0.0));
	}

	public static void ConfigureSource(Buffer b, double x, double y, double z, bool sourceValid = true)
	{
		ConfigureSource(b, new Point(x, y, z), sourceValid);
	}

	private static void Quad(int[] tri, int q, int a, int b, int c, int d)
	{
		int n = q * 6;
		tri[n] = a;
		tri[n + 1] = b;
		tri[n + 2] = c;
		tri[n + 3] = a;
		tri[n + 4] = c;
		tri[n + 5] = d;
	}

	public static Buffer Create()
	{
		Buffer b = new Buffer();
		ConfigureSource(b, default, sourceValid: false);
		for (int k = 0; k < 2; k++)
		{
			for (int i = 0; i < 32; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					int a = k * 165 + i * 5 + j;
					Quad(b.Main.Triangles, k * 128 + i * 4 + j, a, a + 1, a + 6, a + 5);
				}
			}
		}
		b.Core.UV[0] = new Uv(0.5, 0.5);
		for (int l = 0; l < 8; l++)
		{
			b.Core.UV[l + 1] = new Uv(0.5 + 0.5 * Math.Cos((double)l * Math.PI / 4.0), 0.5 + 0.5 * Math.Sin((double)l * Math.PI / 4.0));
			b.Core.Triangles[l * 3] = 0;
			b.Core.Triangles[l * 3 + 1] = l + 1;
			b.Core.Triangles[l * 3 + 2] = 1 + (l + 1) % 8;
		}
		for (int m = 0; m < 8; m++)
		{
			Quad(b.Intake.Triangles, m, m * 4, m * 4 + 1, m * 4 + 2, m * 4 + 3);
		}
		for (int n = 0; n < 12; n++)
		{
			Quad(b.Return.Triangles, n, n * 4, n * 4 + 1, n * 4 + 2, n * 4 + 3);
			for (int num = 0; num < 4; num++)
			{
				b.Return.Role[n * 4 + num] = ((n % 4 == 0) ? 3 : 2);
			}
		}
		return b;
	}

	private static Point Path(double q, int k)
	{
		if (q < 0.0)
		{
			return new Point(Curves[k, 0] + 3.0 * q * (Curves[k, 2] - Curves[k, 0]), Curves[k, 1] + 3.0 * q * (Curves[k, 3] - Curves[k, 1]), 0.0);
		}
		if (q > 1.0)
		{
			return new Point(3.0 * (q - 1.0) * (0.0 - Curves[k, 4]), 3.0 * (q - 1.0) * (0.0 - Curves[k, 5]), 0.0);
		}
		double a = Math.Pow(1.0 - q, 3.0);
		double c = 3.0 * Math.Pow(1.0 - q, 2.0) * q;
		double d = 3.0 * (1.0 - q) * q * q;
		return new Point(a * Curves[k, 0] + c * Curves[k, 2] + d * Curves[k, 4], a * Curves[k, 1] + c * Curves[k, 3] + d * Curves[k, 5], 0.0);
	}

	private static void Ribbon(Buffer b, double t, int k)
	{
		MeshData m = b.Main;
		double q = (t + 0.13) / 0.13;
		double alpha = ((-0.13 <= t && t < 0.092) ? (0.94 * Smooth((t + 0.13) / 0.02)) : 0.0);
		for (int i = 0; i < 33; i++)
		{
			double u = (double)i / 32.0;
			double s = q - (1.0 - u) * 0.55;
			Point c = Path(s, k);
			Point a = Path(s - 0.0001, k);
			Point point = Path(s + 0.0001, k);
			double dx = point.X - a.X;
			double dy = point.Y - a.Y;
			double len = Math.Max(1E-07, Hypot(dx, dy));
			double nx = (0.0 - dy) / len;
			double ny = dx / len;
			double w = ((k == 0) ? 0.042 : 0.033) * Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * u)), 0.45) * (0.75 + 0.25 * u);
			double v = Clamp(u + 0.018 * Math.Sin(t * 21.0 + (double)k));
			double px = 560.0 + 300.0 * v - 150.0 * v * v;
			double py = 470.0 - 420.0 * v + 60.0 * v * v;
			double coverage = Smooth(s / 0.07) * (1.0 - Smooth((s - 0.98) / 0.17));
			for (int j = 0; j < 5; j++)
			{
				int n = k * 165 + i * 5 + j;
				double side = (double)(j - 2) / 2.0;
				m.Vertices[n] = new Point(c.X + nx * side * w, c.Y + ny * side * w, 0.023 + 0.009 * (double)k + 0.005 * (1.0 - side * side));
				m.UV[n] = new Uv((px + side * 62.0) / 1774.0, 1.0 - (py + side * 32.0) / 887.0);
				m.Alpha[n] = alpha;
				m.Coverage[n] = coverage;
			}
		}
	}

	private static void Core(Buffer b, double t)
	{
		double alpha = Fade(t, 0.016, 0.062);
		double r = 0.027 * (1.0 + 0.3 * Smooth(t / 0.025));
		b.Core.Vertices[0] = new Point(0.0, 0.0, 0.059);
		b.Core.Alpha[0] = alpha;
		for (int k = 0; k < 8; k++)
		{
			b.Core.Vertices[k + 1] = new Point(r * Math.Cos((double)k * Math.PI / 4.0), r * 0.78 * Math.Sin((double)k * Math.PI / 4.0), 0.056);
			b.Core.Alpha[k + 1] = alpha;
		}
	}

	private static void Diamond(MeshData m, int particle, Point center, double theta, double length, double width, double alpha)
	{
		double ca = Math.Cos(theta);
		double sa = Math.Sin(theta);
		for (int j = 0; j < 4; j++)
		{
			double a = j switch
			{
				2 => length / 2.0, 
				0 => (0.0 - length) / 2.0, 
				_ => 0.0, 
			};
			double c = j switch
			{
				3 => width / 2.0, 
				1 => (0.0 - width) / 2.0, 
				_ => 0.0, 
			};
			int n = particle * 4 + j;
			m.Vertices[n] = new Point(center.X + ca * a - sa * c, center.Y + sa * a + ca * c, center.Z);
			m.Alpha[n] = alpha;
		}
	}

	private static void IntakeParticle(Buffer b, double t, int i)
	{
		double born = -0.115 + 0.008 * (double)i;
		double life = 0.135 + 0.003 * (double)(i % 3);
		double age = t - born;
		double q = Clamp(age / life);
		double angle = 2.399963 * (double)i + 0.3 * q;
		double r = (0.28 + 0.025 * (double)(i % 4)) * (1.0 - q);
		Point center = new Point(Math.Cos(angle) * r, Math.Sin(angle) * r, 0.05);
		double alpha = Smooth(age / 0.012) * Fade(age, 0.06, life);
		b.IntakeCenters[i] = center;
		b.IntakeActive[i] = alpha > 0.0;
		Diamond(b.Intake, i, center, angle + Math.PI + 0.25, 0.021 * (1.0 - 0.4 * q), 0.0085, alpha);
	}

	private static void ReturnParticle(Buffer b, double t, int i)
	{
		double life = 0.195 + 0.012 * (double)(i % 3);
		double age = t - ReturnBirths[i];
		double q = Clamp(age / life);
		double p = 1.0 - Math.Pow(1.0 - q, 1.65);
		double len = Math.Max(0.001, Hypot(b.SourceH.X, b.SourceH.Y));
		double nx = (0.0 - b.SourceH.Y) / len;
		double ny = b.SourceH.X / len;
		double amplitude = (0.035 + 0.009 * (double)(i % 4)) * (double)((i % 2 != 1) ? 1 : (-1));
		double offset = amplitude * Math.Sin(Math.PI * p);
		Point center = new Point(b.SourceH.X * p + nx * offset, b.SourceH.Y * p + ny * offset, b.SourceH.Z * p + 0.05);
		double dx = b.SourceH.X + nx * amplitude * Math.PI * Math.Cos(Math.PI * p);
		double dy = b.SourceH.Y + ny * amplitude * Math.PI * Math.Cos(Math.PI * p);
		double length = (0.025 + 0.01 * (double)(i % 3)) * (0.35 + 0.65 * Math.Sin(Math.PI * q));
		double width = (0.01 + 0.002 * (double)(i % 2)) * (1.0 - 0.4 * q);
		double alpha = (b.SourceValid ? (Smooth(age / 0.012) * Fade(age, life * 0.55, life)) : 0.0);
		b.ReturnCenters[i] = center;
		b.ReturnActive[i] = alpha > 0.0;
		Diamond(b.Return, i, center, Math.Atan2(dy, dx), length, width, alpha);
	}

	public static void Sample(Buffer b, double age, double availableAnticipation = double.NaN)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		double t = (b.VisualAge = Clock(age, availableAnticipation));
		b.LightEnvelope = Light(t);
		b.TargetAreaW = 38.0 * b.LightEnvelope;
		b.FloorAreaW = 13.0 * b.LightEnvelope;
		Ribbon(b, t, 0);
		Ribbon(b, t, 1);
		Core(b, t);
		for (int i = 0; i < 8; i++)
		{
			IntakeParticle(b, t, i);
		}
		for (int j = 0; j < 12; j++)
		{
			ReturnParticle(b, t, j);
		}
	}
}
