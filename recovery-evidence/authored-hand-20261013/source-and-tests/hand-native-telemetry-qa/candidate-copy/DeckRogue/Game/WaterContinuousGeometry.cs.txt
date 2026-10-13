using System;

namespace DeckRogue.Game;

internal static class WaterContinuousGeometry
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

		public readonly Uv[] SourceUV;

		public readonly double[] Alpha;

		public readonly double[] Gain;

		public readonly int[] Triangles;

		public Uv UvOffset;

		internal MeshData(int vertices, int triangles, int kind, double gain)
		{
			Kind = kind;
			Vertices = new Point[vertices];
			UV = new Uv[vertices];
			SourceUV = new Uv[vertices];
			Alpha = new double[vertices];
			Gain = new double[vertices];
			Triangles = new int[triangles * 3];
			for (int i = 0; i < vertices; i++)
			{
				Gain[i] = gain;
			}
		}
	}

	public sealed class Buffer
	{
		public readonly MeshData[] Batches;

		public readonly bool[] ParticleActive;

		public readonly Point[] ParticleCenters;

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		internal readonly Point[] Centers;

		internal readonly Point[] ParticleStarts;

		internal readonly Point[] ParticleVelocities;

		internal readonly double[] WidthBases;

		public MeshData Primary => Batches[0];

		public MeshData Secondary => Batches[1];

		public MeshData Core => Batches[2];

		public MeshData Particles => Batches[3];

		internal Buffer()
		{
			Batches = new MeshData[4]
			{
				new MeshData(147, 192, 0, 1.35),
				new MeshData(147, 192, 0, 1.1),
				new MeshData(4, 2, 1, 3.0),
				new MeshData(84, 72, 2, 1.1)
			};
			ParticleActive = new bool[12];
			ParticleCenters = new Point[12];
			ParticleStarts = new Point[12];
			ParticleVelocities = new Point[12];
			Centers = new Point[49];
			WidthBases = new double[49];
		}
	}

	public const int TextureKind = 0;

	public const int CoreKind = 1;

	public const int DropletKind = 2;

	public const int RibbonRows = 49;

	public const int ParticleCount = 12;

	public const double NativeLead = 0.16;

	public const double BodyEnd = 0.15;

	public const double CoreEnd = 0.09;

	public const double VisibleEnd = 0.354;

	public const double DiagnosticHorizon = 0.62;

	public const double ParticleLife = 0.24;

	private static readonly double[,] Knots = new double[6, 3]
	{
		{ -0.16, -0.95, 1.2 },
		{ -0.1, -0.82, 3.0 },
		{ 0.0, 0.0, 11.0 },
		{ 0.14, 0.88, 4.5 },
		{ 0.34, 1.45, 1.5 },
		{ 0.62, 1.68, 0.1 }
	};

	private static readonly double[,] Specs = new double[12, 4]
	{
		{ 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
		{ 0.004, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
		{ 0.009, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 },
		{ 0.035, 0.5538025156552036, 0.781381770579271, 0.0552109878293629 },
		{ 0.03900000000000001, -0.6328030518934236, 0.9867369945327584, 0.030297687296996265 },
		{ 0.044000000000000004, 0.061664644824827586, 0.8502843849475583, 0.043524457601980884 },
		{ 0.07, 0.1181540250242763, 0.9482632634123469, 0.05014185475559578 },
		{ 0.07400000000000001, 0.25177522002504815, 1.0755780824187333, 0.057097001477406185 },
		{ 0.079, -0.31071945638799014, 1.125430356417509, 0.050080117253616996 },
		{ 0.10500000000000001, 0.6239549361559444, 1.0962418684217539, 0.042639429252739454 },
		{ 0.10900000000000001, -0.6757082081078837, 1.1861443107606338, 0.0477089878612777 },
		{ 0.114, 0.27593909790993376, 0.6508527792538551, 0.03167280396479855 }
	};

	private static readonly Point[] DropLocal = new Point[7]
	{
		new Point(-0.55, 0.0, 0.0),
		new Point(-0.18, -0.22, 0.0),
		new Point(0.2, -0.14, 0.0),
		new Point(0.5, 0.0, 0.0),
		new Point(0.2, 0.14, 0.0),
		new Point(-0.18, 0.22, 0.0),
		new Point(0.0, 0.0, 0.12)
	};

	private static readonly Uv[] DropUV = new Uv[7]
	{
		new Uv(0.0, 0.5),
		new Uv(0.3, 0.0),
		new Uv(0.7, 0.2),
		new Uv(1.0, 0.5),
		new Uv(0.7, 0.8),
		new Uv(0.3, 1.0),
		new Uv(0.5, 0.5)
	};

	public static double Lead()
	{
		return 0.16;
	}

	public static double End()
	{
		return 0.354;
	}

	public static double Horizon()
	{
		return 0.62;
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

	private static double SafeAge(double age)
	{
		if (double.IsNaN(age) || age < -1000000.0)
		{
			return -0.160001;
		}
		if (age > 1000000.0)
		{
			return 0.62;
		}
		return age;
	}

	public static double Clock(double age, double availableAnticipation = double.NaN)
	{
		age = SafeAge(age);
		if (age >= 0.0 || double.IsNaN(availableAnticipation) || availableAnticipation >= 0.16)
		{
			return age;
		}
		if (availableAnticipation <= 0.0 || age < 0.0 - availableAnticipation)
		{
			return -0.160001;
		}
		double q = Clamp((age + availableAnticipation) / availableAnticipation);
		return -0.16 + 0.16 * (q + (availableAnticipation / 0.16 - 1.0) * (q * q * q - q * q));
	}

	public static double VisualClock(double age, double availableAnticipation = double.NaN)
	{
		return Clock(age, availableAnticipation);
	}

	private static double Head(double t)
	{
		int last = Knots.GetLength(0) - 1;
		if (t <= Knots[0, 0])
		{
			return Knots[0, 1];
		}
		if (t >= Knots[last, 0])
		{
			return Knots[last, 1];
		}
		int i;
		for (i = 0; i < last - 1 && t > Knots[i + 1, 0]; i++)
		{
		}
		double dt = Knots[i + 1, 0] - Knots[i, 0];
		double q = (t - Knots[i, 0]) / dt;
		double q2 = q * q;
		double q3 = q2 * q;
		return (2.0 * q3 - 3.0 * q2 + 1.0) * Knots[i, 1] + (q3 - 2.0 * q2 + q) * dt * Knots[i, 2] + (-2.0 * q3 + 3.0 * q2) * Knots[i + 1, 1] + (q3 - q2) * dt * Knots[i + 1, 2];
	}

	private static Point Center(double lead, double t, bool secondary, int i)
	{
		double u = (double)i / 48.0;
		double length = 1.9 * (secondary ? 0.87 : 1.0);
		double s = lead - length * (1.0 - u) - (secondary ? 0.025 : 0.0);
		return new Point(s, 0.09 * Math.Sin(Math.PI * s / 0.95) + 0.012 * Math.Sin(8.0 * t + u * 7.0) * Math.Sin(Math.PI * u) + (secondary ? (0.075 * Math.Sin(Math.PI * u)) : 0.0), 0.016 * Math.Sin(u * 5.0 + t * 6.0) + (secondary ? 0.013 : 0.0));
	}

	private static Point Emitter(double t)
	{
		return Center(Head(t) * 1.25, t, secondary: false, 46);
	}

	private static double BaseAlpha(double t)
	{
		return (-0.16 <= t && t < 0.62) ? (Smooth((t + 0.16) / 0.025) * (1.0 - Smooth((t - 0.32) / 0.3))) : 0.0;
	}

	public static double CoreAlpha(double visualAge)
	{
		double t = SafeAge(visualAge);
		return (0.0 <= t && t < 0.62) ? (1.0 - Smooth((t - 0.055) / 0.035)) : 0.0;
	}

	public static double Light(double visualAge)
	{
		double t = SafeAge(visualAge);
		double light = ((t >= 0.0) ? CoreAlpha(t) : ((-0.16 <= t && t < 0.62) ? (0.025 * Math.Pow(Smooth((t + 0.16) / 0.16), 3.0)) : 0.0));
		return light * (1.0 - Smooth((t - 0.1) / 0.15));
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
		for (int k = 0; k < 2; k++)
		{
			MeshData m = b.Batches[k];
			for (int i = 0; i < 49; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					m.UV[i * 3 + j] = (m.SourceUV[i * 3 + j] = new Uv((double)i / 48.0, 0.5 + 0.18 * (double)(j - 1)));
				}
			}
			for (int l = 0; l < 48; l++)
			{
				for (int n = 0; n < 2; n++)
				{
					Quad(m.Triangles, l * 2 + n, l * 3 + n, l * 3 + n + 1, (l + 1) * 3 + n + 1, (l + 1) * 3 + n);
				}
			}
		}
		Quad(b.Core.Triangles, 0, 0, 1, 2, 3);
		for (int num = 0; num < 49; num++)
		{
			b.WidthBases[num] = 0.112 * Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * ((double)num / 48.0))), 0.85);
		}
		for (int num2 = 0; num2 < 12; num2++)
		{
			int offset = num2 * 7;
			for (int num3 = 0; num3 < 7; num3++)
			{
				b.Particles.UV[offset + num3] = (b.Particles.SourceUV[offset + num3] = DropUV[num3]);
			}
			for (int num4 = 0; num4 < 6; num4++)
			{
				int n2 = (num2 * 6 + num4) * 3;
				b.Particles.Triangles[n2] = offset + num4;
				b.Particles.Triangles[n2 + 1] = offset + (num4 + 1) % 6;
				b.Particles.Triangles[n2 + 2] = offset + 6;
			}
			double born = Specs[num2, 0];
			double eps = 1E-05;
			Point start = Emitter(born);
			Point ep = Emitter(born + eps);
			Point em = Emitter(born - eps);
			b.ParticleStarts[num2] = start;
			b.ParticleVelocities[num2] = new Point((ep.X - em.X) / 2.0 / eps * 0.28 + Math.Cos(Specs[num2, 1]) * Specs[num2, 2] * 0.45, (ep.Y - em.Y) / 2.0 / eps * 0.28 + Math.Sin(Specs[num2, 1]) * Specs[num2, 2] + 0.45, (ep.Z - em.Z) / 2.0 / eps * 0.28 + 0.04 * (double)(num2 % 3 - 1));
		}
		return b;
	}

	private static void Ribbon(Buffer b, double t, bool secondary, bool mirrorX)
	{
		MeshData m = b.Batches[secondary ? 1 : 0];
		double lead = Head(t) * 1.25;
		double alpha = BaseAlpha(t) * (1.0 - Smooth((t - 0.08) / 0.07)) * (secondary ? 0.52 : 0.9);
		double narrowing = 1.0 - 0.62 * Smooth((t - 0.06) / 0.12);
		m.UvOffset = new Uv(0.012 * Math.Sin(t * 10.0 + (secondary ? 1.7 : 0.0)), 0.008 * Math.Sin(t * 13.0));
		for (int i = 0; i < 49; i++)
		{
			b.Centers[i] = Center(lead, t, secondary, i);
		}
		for (int j = 0; j < 49; j++)
		{
			double u = (double)j / 48.0;
			double pulse = Math.Sin(u * 8.0 - t * 10.0);
			double width = b.WidthBases[j] * (0.72 + 0.28 * pulse * pulse) * narrowing * (secondary ? 0.53 : 1.0);
			Point center = b.Centers[j];
			Point a = b.Centers[Math.Max(0, j - 1)];
			Point c = b.Centers[Math.Min(48, j + 1)];
			double dx = c.X - a.X;
			double dy = c.Y - a.Y;
			double r = Math.Sqrt(dx * dx + dy * dy);
			for (int k = 0; k < 3; k++)
			{
				int n = j * 3 + k;
				double side = k - 1;
				double x = center.X + (0.0 - dy) / r * width * side;
				m.Vertices[n] = new Point(mirrorX ? (0.0 - x) : x, center.Y + dx / r * width * side, center.Z + 0.014 * Math.Sin(u * 13.0 - t * 6.0) * (1.0 - side * side));
				m.UV[n] = new Uv(m.SourceUV[n].X + m.UvOffset.X, m.SourceUV[n].Y + m.UvOffset.Y);
				m.Alpha[n] = alpha;
			}
		}
	}

	private static void Core(Buffer b, double t, bool mirrorX)
	{
		double alpha = CoreAlpha(t);
		for (int i = 0; i < 4; i++)
		{
			double x = i switch
			{
				2 => 0.5, 
				0 => -0.5, 
				_ => 0.0, 
			};
			double y = i switch
			{
				3 => 0.045, 
				1 => -0.045, 
				_ => 0.0, 
			};
			b.Core.Vertices[i] = new Point((mirrorX ? (0.0 - x) : x) * 0.075, -0.008 + y * 0.075, 0.030434782608695657);
			b.Core.Alpha[i] = alpha;
		}
	}

	public static void Sample(Buffer b, double age, bool mirrorX = false, double availableAnticipation = double.NaN)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		double t = (b.VisualAge = Clock(age, availableAnticipation));
		b.LightEnvelope = Light(t);
		b.TargetAreaW = 40.0 * b.LightEnvelope;
		b.FloorAreaW = 18.0 * b.LightEnvelope;
		Ribbon(b, t, secondary: false, mirrorX);
		Ribbon(b, t, secondary: true, mirrorX);
		Core(b, t, mirrorX);
		for (int i = 0; i < 12; i++)
		{
			double ageParticle = t - Specs[i, 0];
			int offset = i * 7;
			if (!(-0.16 <= t) || !(t < 0.62) || !(0.0 <= ageParticle) || !(ageParticle < 0.24))
			{
				b.ParticleActive[i] = false;
				b.ParticleCenters[i] = default(Point);
				for (int j = 0; j < 7; j++)
				{
					b.Particles.Vertices[offset + j] = default(Point);
					b.Particles.Alpha[offset + j] = 0.0;
				}
				continue;
			}
			Point start = b.ParticleStarts[i];
			Point v = b.ParticleVelocities[i];
			double k = 4.0;
			double q = (1.0 - Math.Exp((0.0 - k) * ageParticle)) / k;
			Point center = new Point(start.X + v.X * q, start.Y + v.Y * q - 1.6 * (ageParticle / k - q / k), start.Z + v.Z * q + 0.026086956521739132);
			b.ParticleCenters[i] = new Point(mirrorX ? (0.0 - center.X) : center.X, center.Y, center.Z);
			double size = Specs[i, 3];
			double sx = size * (1.0 - 0.35 * ageParticle / 0.24);
			double sy = size * 0.58;
			double angle = Math.Atan2(v.Y - 1.6 * ageParticle, v.X);
			double c = Math.Cos(angle);
			double s = Math.Sin(angle);
			double alpha = (1.0 - Smooth(ageParticle / 0.24)) * Smooth(ageParticle / 0.012);
			b.ParticleActive[i] = alpha > 0.0;
			for (int l = 0; l < 7; l++)
			{
				Point p = DropLocal[l];
				double x = center.X + c * p.X * sx - s * p.Y * sy;
				b.Particles.Vertices[offset + l] = new Point(mirrorX ? (0.0 - x) : x, center.Y + s * p.X * sx + c * p.Y * sy, center.Z + p.Z * size);
				b.Particles.Alpha[offset + l] = alpha;
			}
		}
	}
}
