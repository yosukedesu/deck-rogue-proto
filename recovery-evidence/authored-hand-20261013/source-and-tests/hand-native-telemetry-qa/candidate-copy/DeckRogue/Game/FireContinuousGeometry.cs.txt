using System;

namespace DeckRogue.Game;

internal static class FireContinuousGeometry
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

	public struct Rgba
	{
		public double R;

		public double G;

		public double B;

		public double A;

		public Rgba(double r, double g, double b, double a)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}
	}

	public sealed class MeshData
	{
		public readonly string Name;

		public readonly int Kind;

		public readonly Point[] Vertices;

		public readonly Uv[] UV;

		public readonly double[] Alpha;

		public readonly double[] Gain;

		public readonly double[] Erosion;

		public readonly int[] Role;

		public readonly int[] Triangles;

		internal MeshData(string name, int count, int triangleCount, int kind, double gain, int role)
		{
			Name = name;
			Kind = kind;
			Vertices = new Point[count];
			UV = new Uv[count];
			Alpha = new double[count];
			Gain = new double[count];
			Erosion = new double[count];
			Role = new int[count];
			Triangles = new int[triangleCount * 3];
			for (int i = 0; i < count; i++)
			{
				Gain[i] = gain;
				Role[i] = role;
			}
		}
	}

	public sealed class Buffer
	{
		public readonly string Mode;

		public readonly bool IsProjectile;

		public readonly int TongueCount;

		public readonly int TongueOffset;

		public readonly MeshData[] Batches;

		public readonly bool[] EmberActive;

		public readonly Point[] EmberCenters;

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		internal double DirectionX;

		internal double DirectionY;

		internal double RotationCos;

		internal double RotationSin;

		public MeshData Flight => IsProjectile ? Batches[0] : null;

		public MeshData Core => Batches[TongueOffset + TongueCount];

		public MeshData Embers => Batches[Batches.Length - 1];

		public Point Source { get; internal set; }

		public bool SourceValid { get; internal set; }

		public bool FlightEnabled => IsProjectile && SourceValid;

		public string SourceFallbackReason => SourceValid ? null : "invalid_or_coincident_capture";

		internal Buffer(string mode, bool projectile)
		{
			Mode = mode;
			IsProjectile = projectile;
			TongueCount = (projectile ? 3 : 5);
			TongueOffset = (projectile ? 1 : 0);
			Batches = new MeshData[TongueOffset + TongueCount + 2];
			if (projectile)
			{
				Batches[0] = new MeshData("flight", 165, 256, 0, 1.75, 2);
			}
			for (int k = 0; k < TongueCount; k++)
			{
				Batches[TongueOffset + k] = new MeshData("flame_" + k, 99, 128, 1, 1.95, 2);
			}
			Batches[TongueOffset + TongueCount] = new MeshData("hot_contact", 9, 8, 2, 3.8, 3);
			Batches[Batches.Length - 1] = new MeshData("embers", 64, 32, 3, 1.85, 2);
			EmberActive = new bool[16];
			EmberCenters = new Point[16];
		}
	}

	public const int ProjectileKind = 0;

	public const int TongueKind = 1;

	public const int CoreKind = 2;

	public const int SolidKind = 3;

	public const int ShadowRole = 0;

	public const int PrimaryRole = 1;

	public const int EdgeRole = 2;

	public const int CoreRole = 3;

	public const int RibbonRows = 33;

	public const int EmberCount = 16;

	public const double NativeLead = 0.13;

	public const double ProjectileEnd = 0.276;

	public const double LocalBurstEnd = 0.284;

	public const double FlightEnd = 0.023;

	public const double CoreEnd = 0.057;

	public const double LightEnd = 0.115;

	private static readonly double[,] ProjectileFlames = new double[3, 5]
	{
		{ -0.12, 0.0, 0.17, 0.44, 0.08 },
		{ 0.55, 0.018, 0.205, 0.76, 0.09 },
		{ 1.3, 0.055, 0.16, 0.56, 0.055 }
	};

	private static readonly double[,] LocalFlames = new double[5, 5]
	{
		{ -0.18, 0.0, 0.18, 0.29, 0.083 },
		{ 0.48, 0.014, 0.21, 0.55, 0.066 },
		{ 1.14, 0.04, 0.2, 0.66, 0.096 },
		{ 1.86, 0.062, 0.18, 0.41, 0.047 },
		{ 2.6, 0.024, 0.18, 0.25, 0.052 }
	};

	private static readonly double[] ProjectileBirths = new double[16]
	{
		-0.085, -0.065, -0.045, -0.025, 0.0, 0.004, 0.01, 0.018, 0.024, 0.034,
		0.045, 0.055, 0.064, 0.074, 0.085, 0.095
	};

	private static readonly double[] LocalBirths = new double[16]
	{
		0.0, 0.003, 0.008, 0.013, 0.019, 0.027, 0.034, 0.042, 0.05, 0.059,
		0.067, 0.075, 0.082, 0.089, 0.096, 0.103
	};

	private static readonly Point[] Palette = new Point[4]
	{
		LinearColor(97, 34, 33),
		LinearColor(214, 90, 40),
		LinearColor(242, 160, 82),
		LinearColor(255, 232, 174)
	};

	public static readonly Point LightColorLinear = LinearColor(223, 170, 120);

	private static bool ParseMode(string mode)
	{
		if (mode == "projectile")
		{
			return true;
		}
		if (mode == "localburst")
		{
			return false;
		}
		throw new ArgumentException("Unknown fire mode; expected projectile or localburst.", "mode");
	}

	public static double Lead(string mode)
	{
		return ParseMode(mode) ? 0.13 : 0.0;
	}

	public static double Lead(Buffer b)
	{
		return b.IsProjectile ? 0.13 : 0.0;
	}

	public static double End(string mode)
	{
		return ParseMode(mode) ? 0.276 : 0.284;
	}

	public static double End(Buffer b)
	{
		return b.IsProjectile ? 0.276 : 0.284;
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
		return (0.0 <= t && t < end) ? (1.0 - Smooth((t - hold) / (end - hold))) : 0.0;
	}

	private static bool Finite(double x)
	{
		return !double.IsNaN(x) && !double.IsInfinity(x);
	}

	private static double Hypot(double x, double y)
	{
		x = Math.Abs(x);
		y = Math.Abs(y);
		double m = Math.Max(x, y);
		return (m == 0.0) ? 0.0 : (m * Math.Sqrt(x / m * (x / m) + y / m * (y / m)));
	}

	private static double SafeAge(double age)
	{
		if (double.IsNaN(age) || age < -1000000.0)
		{
			return -0.130001;
		}
		if (age > 1000000.0)
		{
			return 0.284;
		}
		return age;
	}

	private static double RemapClock(bool projectile, double age, double available)
	{
		age = SafeAge(age);
		if (!projectile || age >= 0.0 || double.IsNaN(available) || available >= 0.13)
		{
			return age;
		}
		if (available <= 0.0 || age < 0.0 - available)
		{
			return -0.130001;
		}
		double q = Clamp((age + available) / available);
		return -0.13 + 0.13 * (q + (available / 0.13 - 1.0) * (q * q * q - q * q));
	}

	public static double Clock(string mode, double age, double available = double.NaN)
	{
		return RemapClock(ParseMode(mode), age, available);
	}

	public static double Clock(Buffer b, double age, double available = double.NaN)
	{
		return RemapClock(b.IsProjectile, age, available);
	}

	public static double Light(Buffer b, double visualAge)
	{
		double t = SafeAge(visualAge);
		if (t >= 0.0)
		{
			return Fade(t, 0.025, 0.115);
		}
		return (b.FlightEnabled && t >= -0.13) ? (0.035 * Math.Pow(Smooth((t + 0.13) / 0.13), 3.0)) : 0.0;
	}

	public static void ConfigureSource(Buffer b, double x, double y, double z, bool valid = true)
	{
		valid = valid && Finite(x) && Finite(y) && Finite(z) && Hypot(x, y) > 0.05;
		b.SourceValid = valid;
		b.Source = (valid ? new Point(x, y, z) : new Point(-1.12, 0.04, 0.0));
		double length = Hypot(b.Source.X, b.Source.Y);
		b.DirectionX = (0.0 - b.Source.X) / length;
		b.DirectionY = (0.0 - b.Source.Y) / length;
		double delta = Math.Atan2(0.0 - b.Source.Y, 0.0 - b.Source.X) - Math.Atan2(-0.04, 1.12);
		b.RotationCos = (b.IsProjectile ? Math.Cos(delta) : 1.0);
		b.RotationSin = (b.IsProjectile ? Math.Sin(delta) : 0.0);
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

	public static Buffer Create(string mode)
	{
		Buffer b = new Buffer(mode, ParseMode(mode));
		ConfigureSource(b, 0.0, 0.0, 0.0, valid: false);
		for (int batch = 0; batch < b.TongueOffset + b.TongueCount; batch++)
		{
			MeshData m = b.Batches[batch];
			int columns = ((m.Kind == 0) ? 5 : 3);
			for (int i = 0; i < 32; i++)
			{
				for (int j = 0; j < columns - 1; j++)
				{
					Quad(m.Triangles, i * (columns - 1) + j, i * columns + j, i * columns + j + 1, (i + 1) * columns + j + 1, (i + 1) * columns + j);
				}
			}
		}
		b.Core.UV[0] = new Uv(0.5, 0.5);
		for (int k = 0; k < 8; k++)
		{
			int n = k * 3;
			b.Core.Triangles[n] = 0;
			b.Core.Triangles[n + 1] = 1 + k;
			b.Core.Triangles[n + 2] = 1 + (k + 1) % 8;
			double angle = Math.PI * 2.0 * (double)k / 8.0;
			b.Core.UV[k + 1] = new Uv(0.5 + 0.5 * Math.Cos(angle), 0.5 + 0.5 * Math.Sin(angle));
		}
		for (int l = 0; l < 16; l++)
		{
			int offset = l * 4;
			Quad(b.Embers.Triangles, l, offset, offset + 1, offset + 2, offset + 3);
			for (int num = 0; num < 4; num++)
			{
				b.Embers.Role[offset + num] = ((l % 4 == 0) ? 3 : 2);
			}
		}
		return b;
	}

	public static Point Tip(Buffer b, double visualAge)
	{
		double t = SafeAge(visualAge);
		if (t <= 0.0)
		{
			double q = Clamp((t + 0.13) / 0.13);
			double v = 0.24 * q + 0.76 * q * q;
			return new Point(b.Source.X * (1.0 - v), b.Source.Y * (1.0 - v), b.Source.Z * (1.0 - v));
		}
		double factor = 13.538461538461538 * (1.0 - Math.Exp(-35.0 * t)) / 35.0;
		return new Point((0.0 - b.Source.X) * factor, (0.0 - b.Source.Y) * factor, (0.0 - b.Source.Z) * factor);
	}

	private static Point Rotate(Buffer b, double x, double y, double z)
	{
		return new Point(b.RotationCos * x - b.RotationSin * y, b.RotationSin * x + b.RotationCos * y, z);
	}

	private static void Projectile(Buffer b, double t)
	{
		MeshData m = b.Flight;
		Point center = Tip(b, t);
		double dx = b.DirectionX;
		double dy = b.DirectionY;
		double nx = 0.0 - dy;
		double ny = dx;
		double alpha = ((-0.13 <= t && t < 0.023 && b.FlightEnabled) ? (Smooth((t + 0.13) / 0.018) * (1.0 - Smooth(Math.Max(0.0, t) / 0.023)) * 0.92) : 0.0);
		for (int i = 0; i < 33; i++)
		{
			double u = (double)i / 32.0;
			double x = (0.0 - (1.0 - u)) * 0.66;
			double y = 0.025 * Math.Sin(u * 7.0 - t * 42.0) * Math.Pow(1.0 - u, 1.4);
			double w = 0.115 * Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * u)), 0.34) * (0.6 + 0.4 * u);
			for (int k = 0; k < 5; k++)
			{
				int n = i * 5 + k;
				double side = (double)(k - 2) / 2.0;
				m.Vertices[n] = new Point(center.X + dx * x + nx * (y + side * w), center.Y + dy * x + ny * (y + side * w), center.Z + 0.022 + 0.006 * (1.0 - side * side));
				m.UV[n] = new Uv((380.0 + 1030.0 * u) / 1774.0, 1.0 - (475.0 + 210.0 * side) / 887.0 + 0.005 * Math.Sin(10.0 * u - 25.0 * t) * (1.0 - u));
				m.Alpha[n] = alpha;
			}
		}
	}

	private static void Flame(Buffer b, double t, int k)
	{
		double[,] specs = (b.IsProjectile ? ProjectileFlames : LocalFlames);
		double born = specs[k, 1];
		double age = Math.Max(0.0, t - born);
		double life = specs[k, 2];
		double alpha = Smooth((t - born) / 0.012) * Fade(t - born, 0.042, life) * 0.95;
		double growth = 0.18 + 0.82 * (1.0 - Math.Exp((0.0 - age) * 35.0));
		double length = specs[k, 3] * growth;
		double width = specs[k, 4] * (0.75 + 0.25 * growth);
		double angle = specs[k, 0] + 0.12 * Math.Sin((double)k * 1.4 + age * 7.0) * Smooth(age / 0.05);
		double ca = Math.Cos(angle);
		double sa = Math.Sin(angle);
		double phase = (double)k * 1.731;
		double erosion = 1.15 * Smooth((age - 0.045) / (life - 0.045));
		MeshData m = b.Batches[b.TongueOffset + k];
		for (int i = 0; i < 33; i++)
		{
			double u = (double)i / 32.0;
			double bend = 0.075 * Math.Sin(3.2 * u - age * (double)(13 + k) + phase) * u * u;
			double advance = 0.025 + age * (0.1 + 0.025 * (double)k);
			double x = length * u + advance;
			double w = width * Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * u)), 0.65) * (1.0 - 0.25 * u) * (1.0 + 0.15 * Smooth((u - 0.45) / 0.55) * Math.Sin(17.0 * u - age * (double)(27 + k) + phase));
			for (int j = 0; j < 3; j++)
			{
				int n = i * 3 + j;
				double side = j - 1;
				double y = bend + side * w;
				m.Vertices[n] = Rotate(b, ca * x - sa * y, sa * x + ca * y, 0.025 + (double)k * 0.004 + 0.008 * (1.0 - side * side));
				double px = 1385.0 - 970.0 * u - 42.0 * age * Math.Sin(Math.PI * u);
				double py = 475.0 - 45.0 * u + side * 175.0 + 10.644 * Math.Sin(7.0 * u - age * 24.0 + phase) * Math.Sin(Math.PI * u);
				m.UV[n] = new Uv(px / 1774.0, 1.0 - py / 887.0);
				m.Alpha[n] = alpha;
				m.Erosion[n] = erosion;
			}
		}
	}

	private static void HotCore(Buffer b, double t)
	{
		MeshData m = b.Core;
		double alpha = Fade(t, 0.018, 0.057);
		double growth = 1.0 + 0.35 * Smooth(Math.Max(0.0, t) / 0.04);
		m.Vertices[0] = new Point(0.0, 0.0, 0.061);
		m.Alpha[0] = alpha;
		for (int k = 0; k < 8; k++)
		{
			double angle = Math.PI * 2.0 * (double)k / 8.0;
			m.Vertices[k + 1] = Rotate(b, Math.Cos(angle) * 0.056 * growth, Math.Sin(angle) * 0.034 * growth, 0.057);
			m.Alpha[k + 1] = alpha;
		}
	}

	private static void Ember(Buffer b, double t, int i)
	{
		double born = (b.IsProjectile ? ProjectileBirths : LocalBirths)[i];
		double age = Math.Max(0.0, t - born);
		double life = 0.16 + 0.007 * (double)(i % 4);
		double phi = (double)i * 2.399963;
		Point p0;
		double vx;
		double vy;
		if (born < 0.0)
		{
			Point tip = Tip(b, born);
			p0 = new Point(tip.X - b.DirectionX * 0.28, tip.Y - b.DirectionY * 0.28, tip.Z + 0.025);
			vx = (0.0 - b.DirectionX) * (0.8 + 0.12 * (double)i);
			vy = 0.35 + Math.Sin(phi) * 0.5;
		}
		else
		{
			p0 = new Point(0.025 * Math.Cos(phi), 0.022 * Math.Sin(phi), 0.05);
			vx = Math.Cos(phi) * (1.15 + 0.14 * (double)(i % 4)) + (b.IsProjectile ? 0.7 : 0.0);
			vy = Math.Sin(phi) * (1.0 + 0.13 * (double)(i % 3)) + 0.62;
			if (b.IsProjectile)
			{
				double oldX = vx;
				vx = b.RotationCos * oldX - b.RotationSin * (vy - 0.62);
				vy = b.RotationSin * oldX + b.RotationCos * (vy - 0.62) + 0.62;
			}
		}
		double q = (1.0 - Math.Exp(-4.0 * age)) / 4.0;
		double x = p0.X + vx * q;
		double y = p0.Y + vy * q + 0.2 * age * age;
		double theta = Math.Atan2(vy + 0.4 * age, vx);
		double ca = Math.Cos(theta);
		double sa = Math.Sin(theta);
		double length = (0.027 + 0.008 * (double)(i % 3)) * (1.0 - 0.65 * Clamp(age / life));
		double width = 0.0055 + 0.001 * (double)(i % 2);
		double alpha = Smooth((t - born) / 0.008) * Fade(t - born, 0.035, life);
		if (born < 0.0 && !b.FlightEnabled)
		{
			alpha = 0.0;
		}
		b.EmberCenters[i] = new Point(x, y, p0.Z);
		b.EmberActive[i] = alpha > 0.0;
		for (int j = 0; j < 4; j++)
		{
			double a = j switch
			{
				2 => length / 2.0, 
				0 => (0.0 - length) / 2.0, 
				_ => 0.0, 
			};
			double side = j switch
			{
				3 => width / 2.0, 
				1 => (0.0 - width) / 2.0, 
				_ => 0.0, 
			};
			int n = i * 4 + j;
			b.Embers.Vertices[n] = new Point(x + ca * a - sa * side, y + sa * a + ca * side, p0.Z);
			b.Embers.Alpha[n] = alpha;
		}
	}

	public static void Sample(Buffer b, double age, double available = double.NaN)
	{
		double t = (b.VisualAge = Clock(b, age, available));
		if (b.IsProjectile)
		{
			Projectile(b, t);
		}
		for (int k = 0; k < b.TongueCount; k++)
		{
			Flame(b, t, k);
		}
		HotCore(b, t);
		for (int i = 0; i < 16; i++)
		{
			Ember(b, t, i);
		}
		b.LightEnvelope = Light(b, t);
		b.TargetAreaW = (double)(b.IsProjectile ? 55 : 62) * b.LightEnvelope;
		b.FloorAreaW = (double)(b.IsProjectile ? 20 : 22) * b.LightEnvelope;
	}

	public static double SrgbToLinear(double x)
	{
		return (x <= 0.04045) ? (x / 12.92) : Math.Pow((x + 0.055) / 1.055, 2.4);
	}

	private static Point LinearColor(int r, int g, int b)
	{
		return new Point(SrgbToLinear((double)r / 255.0), SrgbToLinear((double)g / 255.0), SrgbToLinear((double)b / 255.0));
	}

	public static Point RoleColorLinear(int role)
	{
		if (role < 0 || role > 3)
		{
			throw new ArgumentOutOfRangeException("role");
		}
		return Palette[role];
	}

	private static Point Lerp(Point a, Point b, double q)
	{
		return new Point(a.X + (b.X - a.X) * q, a.Y + (b.Y - a.Y) * q, a.Z + (b.Z - a.Z) * q);
	}

	public static Rgba Surface(int kind, Uv uv, Rgba textureLinear, double alpha, double gain, int role = 2, double erosion = 0.0)
	{
		if (kind < 0 || kind > 3)
		{
			throw new ArgumentOutOfRangeException("kind");
		}
		double a = Clamp(alpha);
		Point rgb = RoleColorLinear(role);
		if (kind == 0 || kind == 1)
		{
			double lum = textureLinear.R * 0.2126 + textureLinear.G * 0.7152 + textureLinear.B * 0.0722;
			rgb = ((lum <= 0.0) ? Palette[0] : ((lum >= 1.0) ? Palette[3] : ((lum <= 0.2) ? Lerp(Palette[0], Palette[1], lum / 0.2) : ((!(lum <= 0.65)) ? Lerp(Palette[2], Palette[3], (lum - 0.65) / 0.35) : Lerp(Palette[1], Palette[2], (lum - 0.2) / 0.45)))));
			a *= Clamp(textureLinear.A);
			if (!(0.0 <= uv.X) || !(uv.X <= 1.0) || !(0.0 <= uv.Y) || !(uv.Y <= 1.0))
			{
				a = 0.0;
			}
			if (kind == 1)
			{
				double u = (1385.0 - 1774.0 * uv.X) / 970.0;
				double field = 0.28 + 0.68 * u - 0.38 * u * u * u + 0.12 * Math.Sin(19.0 * u + 11.0 * uv.Y);
				a *= 1.0 - Smooth((erosion - field) / 0.16);
			}
		}
		else if (kind == 2)
		{
			double coverage = Math.Pow(Math.Max(0.0, 1.0 - Hypot(2.0 * uv.X - 1.0, 2.0 * uv.Y - 1.0)), 0.7);
			a *= coverage;
			rgb = Lerp(Palette[1], Palette[3], coverage);
		}
		return new Rgba(rgb.X * gain * a, rgb.Y * gain * a, rgb.Z * gain * a, a);
	}
}
