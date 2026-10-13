using System;

namespace DeckRogue.Game;

internal static class WideProfileGeometry
{
	public enum Profile
	{
		Water,
		Fire
	}

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

	public struct Bounds
	{
		public Point Min;

		public Point Max;

		public Bounds(Point min, Point max)
		{
			Min = min;
			Max = max;
		}
	}

	public struct HeadSample
	{
		public double Position;

		public double Velocity;

		public HeadSample(double x, double v)
		{
			Position = x;
			Velocity = v;
		}
	}

	public sealed class MeshData
	{
		public readonly int Kind;

		public readonly Point[] Vertices;

		public readonly Uv[] UV;

		public readonly double[] Alpha;

		public readonly double[] Gain;

		public readonly double[] Phase;

		public readonly int[] Triangles;

		internal MeshData(int vertices, int triangles, int kind)
		{
			Kind = kind;
			Vertices = new Point[vertices];
			UV = new Uv[vertices];
			Alpha = new double[vertices];
			Gain = new double[vertices];
			Phase = new double[vertices];
			Triangles = new int[triangles * 3];
		}
	}

	public sealed class Buffer
	{
		public readonly Profile Family;

		public readonly int MirrorRight;

		public readonly int MaximumParticles = 16;

		public readonly MeshData Bands;

		public readonly MeshData Tongues;

		public readonly MeshData Core;

		public readonly MeshData Particles;

		public readonly MeshData[] Batches;

		public readonly bool[] ParticleActive = new bool[16];

		public readonly Point[] ParticleCenters = new Point[16];

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		public Bounds SampleBounds;

		internal Buffer(Profile profile, int mirror)
		{
			Family = profile;
			MirrorRight = mirror;
			bool water = profile == Profile.Water;
			Bands = new MeshData(410, 640, (!water) ? 1 : 0);
			Tongues = (water ? null : new MeshData(225, 288, 1));
			Core = new MeshData(4, 2, 4);
			Particles = new MeshData(water ? 96 : 64, water ? 64 : 32, water ? 2 : 3);
			Batches = ((!water) ? new MeshData[4] { Bands, Tongues, Core, Particles } : new MeshData[3] { Bands, Core, Particles });
		}
	}

	public const int WaterBandKind = 0;

	public const int FireBandKind = 1;

	public const int DropKind = 2;

	public const int EmberKind = 3;

	public const int CoreKind = 4;

	public const int MaximumParticleCount = 16;

	public const double NativeLead = 0.1;

	public const double CoreEnd = 0.065;

	public const double LightEnd = 0.105;

	public const double MaximumAbsoluteSampleAge = 1000000.0;

	private static readonly double[] KnotT = new double[5] { -0.1, 0.0, 0.08, 0.205, 0.36 };

	private static readonly double[] KnotX = new double[5] { -1.0, 0.0, 0.88, 1.4, 1.6 };

	private static readonly double[] KnotV = new double[5] { 2.0, 13.0, 7.0, 2.0, 0.3 };

	public static Bounds ConservativeVisibleBounds => new Bounds(new Point(-3.0, -0.6, -0.01), new Point(3.0, 0.6, 0.1));

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

	public static void CheckProfile(Profile profile)
	{
		if (profile != Profile.Water && profile != Profile.Fire)
		{
			throw new ArgumentOutOfRangeException("profile");
		}
	}

	private static void CheckAge(double t)
	{
		if (!Finite(t) || Math.Abs(t) > 1000000.0)
		{
			throw new ArgumentOutOfRangeException("t");
		}
	}

	public static string ProfileId(Profile p)
	{
		CheckProfile(p);
		return (p == Profile.Water) ? "wide_water" : "wide_fire";
	}

	public static int Count(Profile p)
	{
		CheckProfile(p);
		return 16;
	}

	public static double Lead()
	{
		return 0.1;
	}

	public static double End(Profile p)
	{
		CheckProfile(p);
		return (p == Profile.Water) ? 0.3 : 0.262;
	}

	public static double End(Buffer b)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		return End(b.Family);
	}

	public static double BodyEnd(Profile p)
	{
		CheckProfile(p);
		return (p == Profile.Water) ? 0.205 : 0.19;
	}

	public static HeadSample Head(double t)
	{
		CheckAge(t);
		if (t <= KnotT[0])
		{
			return new HeadSample(KnotX[0] + (t - KnotT[0]) * KnotV[0], KnotV[0]);
		}
		if (t >= KnotT[4])
		{
			return new HeadSample(KnotX[4] + (t - KnotT[4]) * KnotV[4], KnotV[4]);
		}
		for (int i = 0; i < 4; i++)
		{
			if (t <= KnotT[i + 1])
			{
				double d = KnotT[i + 1] - KnotT[i];
				double q = (t - KnotT[i]) / d;
				double q2 = q * q;
				double q3 = q2 * q;
				return new HeadSample((2.0 * q3 - 3.0 * q2 + 1.0) * KnotX[i] + (q3 - 2.0 * q2 + q) * d * KnotV[i] + (-2.0 * q3 + 3.0 * q2) * KnotX[i + 1] + (q3 - q2) * d * KnotV[i + 1], ((6.0 * q2 - 6.0 * q) * KnotX[i] + (3.0 * q2 - 4.0 * q + 1.0) * d * KnotV[i] + (-6.0 * q2 + 6.0 * q) * KnotX[i + 1] + (3.0 * q2 - 2.0 * q) * d * KnotV[i + 1]) / d);
			}
		}
		throw new InvalidOperationException();
	}

	public static double LightEnvelope(Profile p, double age)
	{
		CheckProfile(p);
		CheckAge(age);
		return (0.0 <= age && age <= 0.105) ? (1.0 - Smooth((age - 0.026) / 0.079)) : ((-0.1 <= age && age < 0.0) ? (0.018 * Smooth((age + 0.1) / 0.1)) : 0.0);
	}

	public static double? NativeAge(double age, double availableA, double nativeP = 0.1)
	{
		CheckAge(age);
		if (!Finite(availableA) || !Finite(nativeP) || nativeP < 0.0 || nativeP > 1000000.0)
		{
			throw new ArgumentOutOfRangeException("clockInputs");
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
		double q = age / a;
		return age - (nativeP - a) * q * q;
	}

	private static void Grid(MeshData m, int offset, int triangleOffset, int n, int cross)
	{
		int k = triangleOffset;
		for (int half = 0; half < 2; half++)
		{
			for (int i = 0; i < n - 1; i++)
			{
				for (int j = 0; j < cross - 1; j++)
				{
					int v = offset + i * cross + j;
					m.Triangles[k++] = v;
					m.Triangles[k++] = v + cross + ((half != 0) ? 1 : 0);
					m.Triangles[k++] = v + ((half != 0) ? 1 : (cross + 1));
				}
			}
		}
	}

	public static Buffer Create(Profile profile, int mirrorRight = 1)
	{
		CheckProfile(profile);
		if (mirrorRight != 1 && mirrorRight != -1)
		{
			throw new ArgumentOutOfRangeException("mirrorRight");
		}
		Buffer b = new Buffer(profile, mirrorRight);
		bool water = profile == Profile.Water;
		Grid(b.Bands, 0, 0, 41, 5);
		Grid(b.Bands, 205, 960, 41, 5);
		if (!water)
		{
			for (int n = 0; n < 3; n++)
			{
				Grid(b.Tongues, n * 75, n * 288, 25, 3);
			}
		}
		for (int i = 0; i < 4; i++)
		{
			b.Core.Vertices[i] = new Point((double)mirrorRight * ((i == 0 || i == 3) ? (-0.05) : 0.05), (i < 2) ? (-0.025) : 0.025, 0.07);
			b.Core.UV[i] = new Uv((i != 0 && i != 3) ? 1 : 0, (i >= 2) ? 1 : 0);
			b.Core.Gain[i] = 2.0;
		}
		int[] coreFaces = new int[6] { 0, 1, 2, 0, 2, 3 };
		Array.Copy(coreFaces, b.Core.Triangles, 6);
		int nv = (water ? 6 : 4);
		int nt = (water ? 4 : 2);
		for (int j = 0; j < 16; j++)
		{
			int start = j * nv;
			int k = j * nt * 3;
			for (int f = 1; f <= nt; f++)
			{
				b.Particles.Triangles[k++] = start;
				b.Particles.Triangles[k++] = start + f;
				b.Particles.Triangles[k++] = start + f + 1;
			}
			for (int l = 0; l < nv; l++)
			{
				double num;
				if (!water)
				{
					num = l switch
					{
						2 => 1.0, 
						0 => 0.0, 
						_ => 0.5, 
					};
				}
				else
				{
					switch (l)
					{
					default:
						num = 1.0;
						break;
					case 2:
					case 4:
						num = 0.75;
						break;
					case 1:
					case 5:
						num = 0.25;
						break;
					case 0:
						num = 0.0;
						break;
					}
				}
				double u = num;
				double v = ((l == 0 || l == (water ? 3 : 2)) ? 0.5 : ((double)((l >= (water ? 3 : 2)) ? 1 : 0)));
				b.Particles.UV[start + l] = new Uv(u, v);
				b.Particles.Gain[start + l] = (water ? 1.25 : 1.9);
				b.Particles.Phase[start + l] = (double)j * 2.399;
			}
		}
		Sample(b, 0.0);
		return b;
	}

	public static void Sample(Buffer b, double t, int count = 16)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		CheckAge(t);
		b.VisualAge = t;
		bool water = b.Family == Profile.Water;
		double end = BodyEnd(b.Family);
		double h = Head(t).Position;
		int ncount = Math.Max(0, Math.Min(16, count));
		for (int layer = 0; layer < 2; layer++)
		{
			double length = (water ? 1.72 : 1.6) * ((layer == 1) ? 0.85 : 1.0);
			double lead = h - ((layer == 1) ? 0.055 : 0.0);
			double alpha = ((-0.1 <= t && t <= end) ? (Smooth((t + 0.1) / 0.022) * (1.0 - Smooth((t - 0.075) / (end - 0.075)))) : 0.0);
			alpha *= ((layer == 1) ? 0.4 : 0.88);
			for (int i = 0; i < 41; i++)
			{
				double u = (double)i / 40.0;
				double x = lead - length * (1.0 - u);
				double su = Math.Sin(Math.PI * u);
				double y = 0.085 * Math.Sin(Math.PI * x / 0.95) + 0.027 * Math.Sin(9.0 * u - 8.0 * t) * su - ((layer == 1) ? (0.13 * su) : 0.0);
				double sw = Math.Sin(8.0 * u - 11.0 * t);
				double width = (water ? 0.265 : 0.195) * Math.Pow(Math.Max(0.0, su), 0.8) * (0.87 + 0.13 * sw * sw) * ((layer == 1) ? 0.48 : 1.0) * (1.0 - 0.4 * Smooth((t - 0.045) / 0.14));
				for (int j = 0; j < 5; j++)
				{
					double side = (double)j / 2.0 - 1.0;
					int v = layer * 205 + i * 5 + j;
					b.Bands.Vertices[v] = new Point((double)b.MirrorRight * x, y + width * side, 0.022 + 0.02 * Math.Sin(10.0 * u - 9.0 * t) * (1.0 - side * side) + 0.016 * (double)layer);
					b.Bands.UV[v] = (water ? new Uv(u + 0.12 * t + 0.009 * Math.Sin(6.0 * t + u), 0.5 + 0.18 * side + 0.006 * Math.Sin(7.0 * u - 9.0 * t)) : new Uv((415.0 + 970.0 * u + 28.0 * t * su) / 1774.0, 1.0 - (475.0 - 45.0 * u + side * 175.0 + 10.644 * Math.Sin(7.0 * u - 24.0 * t) * su) / 887.0));
					b.Bands.Alpha[v] = alpha;
					b.Bands.Gain[v] = (water ? 1.3 : 1.65);
				}
			}
		}
		if (!water)
		{
			for (int n = 0; n < 3; n++)
			{
				double birth = n switch
				{
					1 => 0.01, 
					0 => -0.015, 
					_ => 0.035, 
				};
				double life = n switch
				{
					1 => 0.15, 
					0 => 0.135, 
					_ => 0.145, 
				};
				double a = Math.Max(0.0, t - birth);
				double alpha2 = ((birth <= t && t <= birth + life) ? (Smooth(a / 0.012) * (1.0 - Smooth((a - life * 0.35) / (life * 0.65)))) : 0.0);
				for (int k = 0; k < 25; k++)
				{
					double u2 = (double)k / 24.0;
					double x2 = 0.35 * h - 0.15 + 0.2 * (double)n + 0.2 * a + 0.035 * Math.Sin(8.0 * u2 - 20.0 * a + (double)n) * u2;
					double y2 = 0.015 + (0.11 + 0.035 * (double)n) * u2 * (1.0 + 0.2 * Smooth(a / 0.07));
					double width2 = (0.05 + 0.008 * (double)n) * Math.Pow(Math.Sin(Math.PI * u2), 0.7) * (1.0 - 0.65 * u2);
					for (int l = 0; l < 3; l++)
					{
						double side2 = l - 1;
						int v2 = n * 75 + k * 3 + l;
						b.Tongues.Vertices[v2] = new Point((double)b.MirrorRight * (x2 + side2 * width2), y2, 0.06 + 0.003 * (double)n);
						b.Tongues.UV[v2] = new Uv((415.0 + 970.0 * (1.0 - u2)) / 1774.0, 1.0 - (470.0 + side2 * 150.0) / 887.0);
						b.Tongues.Alpha[v2] = alpha2 * 0.6;
						b.Tongues.Gain[v2] = 1.65;
					}
				}
			}
		}
		double core = ((0.0 <= t && t <= 0.065) ? (1.0 - Smooth((t - 0.023) / 0.042)) : 0.0);
		for (int m = 0; m < 4; m++)
		{
			b.Core.Alpha[m] = core * 0.7;
		}
		for (int num = 0; num < 16; num++)
		{
			double birth2 = 0.006 * (double)(num % 8) + ((num >= 8) ? 0.033 : 0.0);
			double life2 = (water ? 0.225 : (0.175 + 0.009 * (double)(num % 3)));
			double a2 = Math.Max(0.0, t - birth2);
			double alpha3 = ((birth2 <= t && t <= birth2 + life2 && num < ncount) ? (Smooth(a2 / 0.01) * (1.0 - Smooth((a2 - life2 * 0.32) / (life2 * 0.68)))) : 0.0);
			HeadSample atBirth = Head(birth2);
			double vx = 0.2 * atBirth.Velocity + 0.25 + 0.22 * Math.Sin((double)num * 1.8);
			double vy = 0.5 + 0.26 * Math.Sin((double)num * 2.4);
			double travel = (1.0 - Math.Exp(-4.0 * a2)) / 4.0;
			double x3 = 0.55 * atBirth.Position + 0.025 * Math.Sin((double)num * 2.399) + vx * travel;
			double y3 = 0.016 * Math.Cos((double)num * 1.4) + vy * travel + (water ? (-1.6 * a2 * a2) : (0.35 * a2 * a2));
			double angle = Math.Atan2(vy + (water ? (-3.2 * a2) : (0.7 * a2)), vx * Math.Exp(-4.0 * a2));
			double ca = Math.Cos(angle);
			double sa = Math.Sin(angle);
			double length2 = (0.025 + 0.004 * (double)(num % 4)) * (1.0 - 0.38 * Smooth(a2 / life2));
			double width3 = 0.009 + 0.002 * (double)(num % 3);
			double z = 0.075 + 0.001 * (double)num;
			int nv = (water ? 6 : 4);
			for (int num2 = 0; num2 < nv; num2++)
			{
				double num3;
				if (!water)
				{
					num3 = num2 switch
					{
						2 => length2, 
						0 => 0.0 - length2, 
						_ => 0.0, 
					};
				}
				else
				{
					switch (num2)
					{
					default:
						num3 = length2;
						break;
					case 2:
					case 4:
						num3 = length2 * 0.5;
						break;
					case 1:
					case 5:
						num3 = (0.0 - length2) * 0.5;
						break;
					case 0:
						num3 = 0.0 - length2;
						break;
					}
				}
				double u3 = num3;
				double v3 = ((num2 == 0 || num2 == (water ? 3 : 2)) ? 0.0 : ((num2 < (water ? 3 : 2)) ? (0.0 - width3) : width3));
				int k2 = num * nv + num2;
				b.Particles.Vertices[k2] = new Point((double)b.MirrorRight * (x3 + u3 * ca - v3 * sa), y3 + u3 * sa + v3 * ca, z);
				b.Particles.Alpha[k2] = alpha3;
			}
			b.ParticleActive[num] = alpha3 > 0.0;
			b.ParticleCenters[num] = new Point((double)b.MirrorRight * x3, y3, z);
		}
		b.LightEnvelope = LightEnvelope(b.Family, t);
		b.TargetAreaW = (double)(water ? 38 : 45) * b.LightEnvelope;
		b.FloorAreaW = (double)(water ? 14 : 16) * b.LightEnvelope;
		Point min = b.Core.Vertices[0];
		Point max = min;
		for (int num4 = 0; num4 < b.Batches.Length; num4++)
		{
			for (int num5 = 0; num5 < b.Batches[num4].Vertices.Length; num5++)
			{
				Point p = b.Batches[num4].Vertices[num5];
				min.X = Math.Min(min.X, p.X);
				min.Y = Math.Min(min.Y, p.Y);
				min.Z = Math.Min(min.Z, p.Z);
				max.X = Math.Max(max.X, p.X);
				max.Y = Math.Max(max.Y, p.Y);
				max.Z = Math.Max(max.Z, p.Z);
			}
		}
		b.SampleBounds = new Bounds(min, max);
	}

	public static bool SampleReceipt(Buffer b, double age, double availableA = 0.1, int count = 16)
	{
		double? native = NativeAge(age, availableA);
		Sample(b, native ?? (-0.1), count);
		if (native.HasValue)
		{
			return true;
		}
		for (int m = 0; m < b.Batches.Length; m++)
		{
			Array.Clear(b.Batches[m].Alpha, 0, b.Batches[m].Alpha.Length);
		}
		Array.Clear(b.ParticleActive, 0, b.ParticleActive.Length);
		b.LightEnvelope = (b.TargetAreaW = (b.FloorAreaW = 0.0));
		return false;
	}
}
