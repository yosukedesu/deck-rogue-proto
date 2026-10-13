using System;

namespace DeckRogue.Game;

internal static class ScatterProfileGeometry
{
	public enum Profile
	{
		Fire,
		WarmSpark,
		Ice,
		Spore
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

		public readonly int MaximumParticles;

		public readonly MeshData Core;

		public readonly MeshData Particles;

		public readonly MeshData[] Batches;

		public readonly bool[] ParticleActive;

		public readonly Point[] ParticleCenters;

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		public Bounds SampleBounds;

		internal Buffer(Profile profile, int mirror)
		{
			Family = profile;
			MirrorRight = mirror;
			MaximumParticles = Count(profile);
			Core = new MeshData(6, 4, 0);
			Particles = new MeshData(4 * MaximumParticles, 2 * MaximumParticles, 1);
			Batches = new MeshData[2] { Core, Particles };
			ParticleActive = new bool[MaximumParticles];
			ParticleCenters = new Point[MaximumParticles];
		}
	}

	public const int CoreKind = 0;

	public const int ParticleKind = 1;

	public const int BatchCount = 2;

	public const int TextureCount = 0;

	public const double NativeLead = 0.0;

	public const double CoreEnd = 0.05;

	public const double LightEnd = 0.07;

	public const double MaximumAbsoluteSampleAge = 1000000.0;

	public static Bounds ConservativeVisibleBounds => new Bounds(new Point(-0.4, -0.4, 0.02), new Point(0.4, 0.4, 0.033));

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

	public static void CheckProfile(Profile family)
	{
		if (family < Profile.Fire || family > Profile.Spore)
		{
			throw new ArgumentOutOfRangeException("family");
		}
	}

	public static int Count(Profile family)
	{
		CheckProfile(family);
		return (family == Profile.Ice) ? 10 : 12;
	}

	public static string ProfileId(Profile family)
	{
		CheckProfile(family);
		return family switch
		{
			Profile.Fire => "scatter_fire", 
			Profile.WarmSpark => "scatter_warm_spark", 
			Profile.Ice => "scatter_ice", 
			_ => "scatter_spore", 
		};
	}

	public static double End(Profile family)
	{
		CheckProfile(family);
		return family switch
		{
			Profile.Fire => 0.205, 
			Profile.WarmSpark => 0.19, 
			Profile.Ice => 0.219, 
			_ => 0.26, 
		};
	}

	public static double End(Buffer b)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		return End(b.Family);
	}

	public static double Lead()
	{
		return 0.0;
	}

	public static double LightEnvelope(Profile family, double age)
	{
		CheckProfile(family);
		if (!Finite(age))
		{
			throw new ArgumentOutOfRangeException("age");
		}
		double maximum = family switch
		{
			Profile.Ice => 0.24, 
			Profile.WarmSpark => 0.26, 
			Profile.Fire => 0.32, 
			_ => 0.15, 
		};
		return (0.0 <= age && age <= 0.07) ? (maximum * (1.0 - Smooth((age - 0.012) / 0.058))) : 0.0;
	}

	public static double? NativeAge(double age, double availableA = 0.0)
	{
		if (!Finite(age) || !Finite(availableA))
		{
			throw new ArgumentException("Finite clock inputs required.");
		}
		return (age < 0.0) ? ((double?)null) : new double?(age);
	}

	public static Buffer Create(Profile family, int mirrorRight = 1)
	{
		CheckProfile(family);
		if (mirrorRight != -1 && mirrorRight != 1)
		{
			throw new ArgumentOutOfRangeException("mirrorRight");
		}
		Buffer b = new Buffer(family, mirrorRight);
		double carrier = 1.0 / Math.Cos(Math.PI / 6.0);
		double radius = family switch
		{
			Profile.Ice => 0.018, 
			Profile.Fire => 0.022, 
			_ => 0.02, 
		};
		for (int i = 0; i < 6; i++)
		{
			double theta = (double)i * (Math.PI * 2.0) / 6.0;
			double ca = Math.Cos(theta);
			double sa = Math.Sin(theta);
			b.Core.Vertices[i] = new Point((double)mirrorRight * carrier * radius * ca, carrier * radius * 0.7 * sa, 0.02);
			b.Core.UV[i] = new Uv(0.5 + 0.5 * carrier * ca, 0.5 + 0.5 * carrier * sa);
			b.Core.Gain[i] = 1.7;
		}
		for (int j = 0; j < 4; j++)
		{
			b.Core.Triangles[j * 3] = 0;
			b.Core.Triangles[j * 3 + 1] = j + 1;
			b.Core.Triangles[j * 3 + 2] = j + 2;
		}
		for (int k = 0; k < b.MaximumParticles; k++)
		{
			int v = k * 4;
			int f = k * 6;
			b.Particles.Triangles[f] = v;
			b.Particles.Triangles[f + 1] = v + 1;
			b.Particles.Triangles[f + 2] = v + 2;
			b.Particles.Triangles[f + 3] = v;
			b.Particles.Triangles[f + 4] = v + 2;
			b.Particles.Triangles[f + 5] = v + 3;
			bool spore = family == Profile.Spore;
			b.Particles.UV[v] = new Uv(0.0, spore ? 0.0 : 0.5);
			b.Particles.UV[v + 1] = new Uv(spore ? 1.0 : 0.42, 0.0);
			b.Particles.UV[v + 2] = new Uv(1.0, spore ? 1.0 : 0.5);
			b.Particles.UV[v + 3] = new Uv(spore ? 0.0 : 0.45, 1.0);
			double gain = family switch
			{
				Profile.Ice => 1.3, 
				Profile.WarmSpark => 1.45, 
				Profile.Fire => 1.6, 
				_ => 0.95, 
			};
			for (int l = 0; l < 4; l++)
			{
				b.Particles.Phase[v + l] = (double)k * 1.913;
				b.Particles.Gain[v + l] = gain;
			}
		}
		Sample(b, 0.0);
		return b;
	}

	public static void Sample(Buffer b, double t, int count = int.MaxValue)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		if (!Finite(t) || Math.Abs(t) > 1000000.0)
		{
			throw new ArgumentOutOfRangeException("t");
		}
		b.VisualAge = t;
		int n = Math.Max(0, Math.Min(b.MaximumParticles, count));
		Profile family = b.Family;
		double core = ((0.0 <= t && t <= 0.05) ? (1.0 - Smooth((t - 0.008) / 0.042)) : 0.0);
		core *= family switch
		{
			Profile.Ice => 0.5, 
			Profile.WarmSpark => 0.7, 
			Profile.Fire => 0.8, 
			_ => 0.35, 
		};
		for (int j = 0; j < 6; j++)
		{
			b.Core.Alpha[j] = core;
		}
		for (int i = 0; i < b.MaximumParticles; i++)
		{
			int q = i % 4;
			double birth = family switch
			{
				Profile.Spore => 0.002, 
				Profile.Ice => 0.003, 
				_ => 0.004, 
			} * (double)q;
			double life = family switch
			{
				Profile.Ice => 0.18 + 0.01 * (double)q, 
				Profile.WarmSpark => 0.16 + 0.006 * (double)q, 
				Profile.Fire => 0.169 + 0.008 * (double)q, 
				_ => 0.23 + 0.008 * (double)q, 
			};
			double a = Math.Max(0.0, t - birth);
			double phase = (double)i * 1.913;
			double alpha = ((birth <= t && t <= birth + life && i < n) ? (Smooth(a / 0.008) * (1.0 - Smooth((a - life * 0.28) / (life * 0.72)))) : 0.0);
			double x;
			double y;
			double angle;
			double length;
			double width;
			switch (family)
			{
			case Profile.Fire:
			{
				double vx2 = 1.3 * Math.Sin(phase) + 0.3;
				double vy2 = 0.64 + 0.26 * (double)(i % 5);
				double travel2 = (1.0 - Math.Exp(-5.0 * a)) / 5.0;
				x = 0.045 * Math.Sin((double)i * 2.3) + vx2 * travel2;
				y = -0.018 + 0.014 * (double)(i % 4) + vy2 * travel2 + 0.2 * a * a;
				angle = Math.Atan2(vy2 + 0.4 * a, vx2);
				length = (0.015 + 0.0045 * (double)(i % 3)) * (1.0 - 0.45 * Smooth(a / life));
				width = 0.0045 + 0.001 * (double)(i % 2);
				break;
			}
			case Profile.WarmSpark:
			{
				int sign = ((i % 2 == 0) ? 1 : (-1));
				double vx = (double)sign * (0.85 + 0.18 * (double)(i % 5));
				double vy = 0.32 * Math.Sin(phase) + 0.1 * (double)(i % 3 - 1);
				double travel = (1.0 - Math.Exp(-9.0 * a)) / 9.0;
				x = 0.02 * (double)sign + 0.011 * Math.Sin((double)i * 1.7) + vx * travel;
				y = 0.035 * Math.Sin((double)i * 2.4) + vy * travel - 0.25 * a * a;
				angle = Math.Atan2(vy - 0.5 * a, vx);
				length = (0.014 + 0.0035 * (double)(i % 3)) * (1.0 - 0.5 * Smooth(a / life));
				width = 0.0055;
				break;
			}
			case Profile.Ice:
				x = ((double)i - 4.5) * 0.027 + 0.25 * Math.Sin(phase) * a;
				y = 0.042 * Math.Sin((double)i * 2.1) - (0.23 + 0.1 * (double)(i % 3)) * a - 1.5 * a * a;
				angle = phase + ((i % 2 != 0) ? (-2.8) : 2.1) * a;
				length = 0.022 + 0.006 * (double)(i % 3);
				width = 0.011 + 0.0025 * (double)(i % 2);
				break;
			default:
				x = 0.14 * Math.Sin((double)i * 2.399) + 0.11 * Math.Cos(phase) * a + 0.014 * Math.Sin(18.0 * a + phase) * Smooth(a / 0.03);
				y = 0.08 * Math.Cos((double)i * 1.7) - (0.09 + 0.035 * (double)(i % 3)) * a - 0.3 * a * a;
				angle = 0.18 * Math.Sin(phase + a * 5.0);
				length = 0.022 + 0.006 * (double)(i % 3);
				width = length * (0.72 + 0.09 * (double)(i % 2));
				alpha *= 0.82;
				break;
			}
			double ca = Math.Cos(angle);
			double sa = Math.Sin(angle);
			double z = 0.03 + 0.0002 * (double)i;
			for (int k = 0; k < 4; k++)
			{
				double u;
				double w;
				if (family == Profile.Spore)
				{
					u = ((k == 0 || k == 3) ? (0.0 - width) : width);
					w = ((k < 2) ? (0.0 - length) : length);
				}
				else
				{
					u = k switch
					{
						2 => length, 
						1 => (0.0 - length) * 0.16, 
						0 => 0.0 - length, 
						_ => (0.0 - length) * 0.1, 
					};
					w = k switch
					{
						3 => width, 
						1 => 0.0 - width, 
						_ => 0.0, 
					};
				}
				int v = i * 4 + k;
				b.Particles.Vertices[v] = new Point((double)b.MirrorRight * (x + u * ca - w * sa), y + u * sa + w * ca, z);
				b.Particles.Alpha[v] = alpha;
			}
			b.ParticleCenters[i] = new Point((double)b.MirrorRight * x, y, z);
			b.ParticleActive[i] = alpha > 0.0;
		}
		b.LightEnvelope = LightEnvelope(family, t);
		b.TargetAreaW = 32.0 * b.LightEnvelope;
		b.FloorAreaW = 8.0 * b.LightEnvelope;
		Point min = b.Core.Vertices[0];
		Point max = min;
		for (int l = 0; l < 2; l++)
		{
			for (int m = 0; m < b.Batches[l].Vertices.Length; m++)
			{
				Point p = b.Batches[l].Vertices[m];
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

	public static bool SampleReceipt(Buffer b, double age, double availableA = 0.0, int count = int.MaxValue)
	{
		double? native = NativeAge(age, availableA);
		Sample(b, age, count);
		return native.HasValue;
	}
}
