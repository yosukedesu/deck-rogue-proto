using System;

namespace DeckRogue.Game;

internal static class LocalMaterialProfileGeometry
{
	public enum Profile
	{
		Dark,
		Light,
		Botanical,
		Physical,
		Water
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

		public readonly int[] Role;

		public readonly int[] Triangles;

		internal MeshData(int nv, int nt, int kind)
		{
			Kind = kind;
			Vertices = new Point[nv];
			UV = new Uv[nv];
			Alpha = new double[nv];
			Gain = new double[nv];
			Phase = new double[nv];
			Role = new int[nv];
			Triangles = new int[nt * 3];
		}
	}

	public sealed class ObjectRange
	{
		public readonly string Name;

		public readonly string Material;

		public readonly int Batch;

		public readonly int VertexOffset;

		public readonly int VertexCount;

		public readonly int TriangleOffset;

		public readonly int TriangleCount;

		internal ObjectRange(string name, string material, int batch, int vo, int nv, int to, int nt)
		{
			Name = name;
			Material = material;
			Batch = batch;
			VertexOffset = vo;
			VertexCount = nv;
			TriangleOffset = to;
			TriangleCount = nt;
		}
	}

	public sealed class Buffer
	{
		public readonly Profile Family;

		public readonly int MirrorRight;

		public readonly int MaximumParticles = 12;

		public readonly MeshData Body;

		public readonly MeshData Core;

		public readonly MeshData Particles;

		public readonly MeshData[] Batches;

		public readonly ObjectRange[] Objects;

		public readonly bool[] ParticleActive = new bool[12];

		public readonly Point[] ParticleCenters = new Point[12];

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		public Bounds SampleBounds;

		internal Buffer(Profile p, int mirror)
		{
			Family = p;
			MirrorRight = mirror;
			int n = BodyObjectCount(p);
			int nv = BodyObjectVertices(p);
			int nt = BodyObjectTriangles(p);
			Body = new MeshData(n * nv, n * nt, p switch
			{
				Profile.Water => 1, 
				Profile.Botanical => 5, 
				_ => 0, 
			});
			Core = new MeshData(4, 2, 3);
			Particles = new MeshData(48, 24, p switch
			{
				Profile.Water => 2, 
				Profile.Physical => 6, 
				Profile.Botanical => 4, 
				_ => 7, 
			});
			Batches = new MeshData[3] { Body, Core, Particles };
			Objects = new ObjectRange[n + 13];
			for (int k = 0; k < n; k++)
			{
				Objects[k] = new ObjectRange(p switch
				{
					Profile.Physical => "compression_accent", 
					Profile.Botanical => "pod_shell_" + k, 
					Profile.Light => "ray_" + k, 
					Profile.Dark => "tear_" + k, 
					_ => "water_band_" + k, 
				}, p switch
				{
					Profile.Physical => "blunt", 
					Profile.Botanical => "pod", 
					Profile.Light => "light", 
					Profile.Dark => "dark", 
					_ => "water", 
				}, 0, k * nv, nv, k * nt, nt);
			}
			Objects[n] = new ObjectRange("core", "core", 1, 0, 4, 0, 2);
			for (int i = 0; i < 12; i++)
			{
				Objects[n + 1 + i] = new ObjectRange("particle_" + i, p switch
				{
					Profile.Water => "waterdrop", 
					Profile.Physical => "facet", 
					Profile.Botanical => "spore", 
					_ => "solid", 
				}, 2, i * 4, 4, i * 2, 2);
			}
		}
	}

	public const int MappedKind = 0;

	public const int WaterKind = 1;

	public const int WaterdropKind = 2;

	public const int CoreKind = 3;

	public const int SporeKind = 4;

	public const int PodKind = 5;

	public const int FacetKind = 6;

	public const int SolidKind = 7;

	public const int ShadowRole = 0;

	public const int PrimaryRole = 1;

	public const int EdgeRole = 2;

	public const int CoreRole = 3;

	public const int MaximumParticleCount = 12;

	public const int BatchCount = 3;

	public const double NativeLead = 0.0;

	public const double CoreHold = 0.014;

	public const double CoreEnd = 0.056;

	public const double LightHold = 0.018;

	public const double LightEnd = 0.095;

	public const double MaximumAbsoluteSampleAge = 1000000.0;

	private static readonly double[] TearBirth = new double[5] { 0.0, 0.003, 0.009, 0.014, 0.021 };

	private static readonly double[] TearLife = new double[5] { 0.18, 0.205, 0.172, 0.198, 0.17 };

	private static readonly double[] TearAngle = new double[5] { -0.2, 1.42, 2.82, 3.45, 5.78 };

	private static readonly double[] TearLength = new double[5] { 0.34, 0.22, 0.3, 0.18, 0.27 };

	private static readonly double[] RayBirth = new double[7] { 0.0, 0.002, 0.005, 0.009, 0.014, 0.018, 0.023 };

	private static readonly double[] RayAngle = new double[7] { -0.15, 0.27, 1.02, 2.45, 3.27, 4.48, 5.55 };

	private static readonly double[] RaySpeed = new double[7] { 5.8, 4.2, 3.8, 4.5, 5.5, 3.8, 4.1 };

	private static readonly double[] RayLength = new double[7] { 0.42, 0.28, 0.3, 0.37, 0.39, 0.26, 0.29 };

	private static readonly double[] PodAngle = new double[3] { 0.28, 2.8, 4.9 };

	private static readonly double[] PodSpin = new double[3] { -9.0, 7.0, -6.0 };

	public static Bounds ConservativeVisibleBounds => new Bounds(new Point(-0.85, -0.8, 0.015), new Point(0.85, 0.8, 0.085));

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

	public static void CheckProfile(Profile p)
	{
		if (p < Profile.Dark || p > Profile.Water)
		{
			throw new ArgumentOutOfRangeException("profile");
		}
	}

	private static void CheckAge(double t)
	{
		if (!Finite(t) || Math.Abs(t) > 1000000.0)
		{
			throw new ArgumentOutOfRangeException("age");
		}
	}

	private static double Fade(double t, double hold, double end)
	{
		return (0.0 <= t && t < end) ? (1.0 - Smooth((t - hold) / (end - hold))) : 0.0;
	}

	private static double Envelope(double t, double born, double life, double rise = 0.008)
	{
		return Smooth((t - born) / rise) * Fade(t - born, 0.024, life);
	}

	public static string ProfileId(Profile p)
	{
		CheckProfile(p);
		return p switch
		{
			Profile.Physical => "local_physical", 
			Profile.Botanical => "local_botanical", 
			Profile.Light => "local_light", 
			Profile.Dark => "local_dark", 
			_ => "local_water_flow", 
		};
	}

	public static int Count(Profile p)
	{
		CheckProfile(p);
		return 12;
	}

	public static int BodyObjectCount(Profile p)
	{
		CheckProfile(p);
		return p switch
		{
			Profile.Physical => 1, 
			Profile.Botanical => 3, 
			Profile.Light => 7, 
			Profile.Dark => 5, 
			_ => 2, 
		};
	}

	public static int BodyObjectVertices(Profile p)
	{
		CheckProfile(p);
		int result;
		switch (p)
		{
		default:
			result = 165;
			break;
		case Profile.Physical:
			result = 4;
			break;
		case Profile.Botanical:
			result = 39;
			break;
		case Profile.Dark:
		case Profile.Light:
			result = 75;
			break;
		}
		return result;
	}

	public static int BodyObjectTriangles(Profile p)
	{
		CheckProfile(p);
		int result;
		switch (p)
		{
		default:
			result = 256;
			break;
		case Profile.Physical:
			result = 2;
			break;
		case Profile.Botanical:
			result = 48;
			break;
		case Profile.Dark:
		case Profile.Light:
			result = 96;
			break;
		}
		return result;
	}

	public static double Lead()
	{
		return 0.0;
	}

	public static double End(Profile p)
	{
		CheckProfile(p);
		return p switch
		{
			Profile.Physical => 0.238, 
			Profile.Botanical => 0.273, 
			Profile.Light => 0.198, 
			Profile.Dark => 0.218, 
			_ => 0.263, 
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

	public static double BodyEnd(Profile p)
	{
		CheckProfile(p);
		return p switch
		{
			Profile.Physical => 0.055, 
			Profile.Botanical => 0.212, 
			Profile.Light => 0.172, 
			Profile.Dark => 0.212, 
			_ => 0.17, 
		};
	}

	public static double LightEnvelope(Profile p, double t)
	{
		CheckProfile(p);
		CheckAge(t);
		double num = Fade(t, 0.018, 0.095);
		double num2;
		switch (p)
		{
		default:
			num2 = 0.4;
			break;
		case Profile.Botanical:
			num2 = 0.3;
			break;
		case Profile.Light:
			num2 = 0.6;
			break;
		case Profile.Dark:
		case Profile.Physical:
			num2 = 0.45;
			break;
		}
		return num * num2;
	}

	public static double? NativeAge(double age, double availableA = 0.0)
	{
		CheckAge(age);
		if (!Finite(availableA))
		{
			throw new ArgumentOutOfRangeException("availableA");
		}
		return (age < 0.0) ? ((double?)null) : new double?(age);
	}

	private static void QuadTriangles(MeshData m, int vo, int to)
	{
		int k = to * 3;
		m.Triangles[k] = vo;
		m.Triangles[k + 1] = vo + 1;
		m.Triangles[k + 2] = vo + 2;
		m.Triangles[k + 3] = vo;
		m.Triangles[k + 4] = vo + 2;
		m.Triangles[k + 5] = vo + 3;
	}

	private static void Grid(MeshData m, int vo, int to, int rows, int cross)
	{
		int f = to * 3;
		for (int i = 0; i < rows - 1; i++)
		{
			for (int j = 0; j < cross - 1; j++)
			{
				int a = vo + i * cross + j;
				m.Triangles[f++] = a;
				m.Triangles[f++] = a + 1;
				m.Triangles[f++] = a + cross + 1;
				m.Triangles[f++] = a;
				m.Triangles[f++] = a + cross + 1;
				m.Triangles[f++] = a + cross;
			}
		}
	}

	public static Buffer Create(Profile p, int mirrorRight = 1)
	{
		CheckProfile(p);
		if (mirrorRight != 1 && mirrorRight != -1)
		{
			throw new ArgumentOutOfRangeException("mirrorRight");
		}
		Buffer b = new Buffer(p, mirrorRight);
		int n = BodyObjectCount(p);
		int nv = BodyObjectVertices(p);
		int nt = BodyObjectTriangles(p);
		for (int k = 0; k < n; k++)
		{
			if (p == Profile.Physical)
			{
				QuadTriangles(b.Body, 0, 0);
				continue;
			}
			Grid(b.Body, k * nv, k * nt, p switch
			{
				Profile.Water => 33, 
				Profile.Botanical => 13, 
				_ => 25, 
			}, (p == Profile.Water) ? 5 : 3);
		}
		QuadTriangles(b.Core, 0, 0);
		for (int i = 0; i < 12; i++)
		{
			QuadTriangles(b.Particles, i * 4, i * 2);
		}
		Sample(b, 0.0);
		return b;
	}

	private static void Put(Buffer b, MeshData m, int i, double x, double y, double z, double u, double v, double alpha, double gain, int role = 2, double phase = 0.0)
	{
		m.Vertices[i] = new Point((double)b.MirrorRight * x, y, z);
		m.UV[i] = new Uv(u, v);
		m.Alpha[i] = alpha;
		m.Gain[i] = gain;
		m.Role[i] = role;
		m.Phase[i] = phase;
	}

	public static void Sample(Buffer b, double t, int count = 12)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		CheckAge(t);
		b.VisualAge = t;
		Profile p = b.Family;
		int ncount = Math.Max(0, Math.Min(12, count));
		if (p == Profile.Dark || p == Profile.Light)
		{
			for (int k = 0; k < BodyObjectCount(p); k++)
			{
				bool dark = p == Profile.Dark;
				double born = (dark ? TearBirth[k] : RayBirth[k]);
				double a = Math.Max(0.0, t - born);
				double ang = (dark ? TearAngle[k] : RayAngle[k]);
				double ca = Math.Cos(ang);
				double sa = Math.Sin(ang);
				double q = (1.0 - Math.Exp((double)(-(dark ? 12 : 18)) * a)) / (double)(dark ? 12 : 18);
				double lead = (dark ? (3.3 + 0.55 * (double)(k % 3)) : RaySpeed[k]) * q;
				double L = (dark ? (TearLength[k] * (0.2 + 0.8 * (1.0 - Math.Exp(-40.0 * a)))) : (RayLength[k] * (0.25 + 0.75 * (1.0 - Math.Exp(-75.0 * a)))));
				double alpha = (dark ? (Envelope(t, born, TearLife[k]) * 0.9) : Envelope(t, born, 0.145 + 0.009 * (double)(k % 2), 0.005));
				for (int i = 0; i < 25; i++)
				{
					double u = (double)i / 24.0;
					double s = lead - L * (1.0 - u);
					double bend = (dark ? (0.035 * Math.Sin(Math.PI * u) * Math.Sin(12.0 * a + (double)k)) : 0.0);
					double w = (dark ? (0.026 + 0.006 * (double)(k % 2)) : ((k == 0 || k == 4) ? 0.018 : 0.01)) * Math.Pow(Math.Sin(Math.PI * u), 0.6);
					for (int j = 0; j < 3; j++)
					{
						int side = j - 1;
						double dx = bend + (double)side * w;
						Put(b, b.Body, k * 75 + i * 3 + j, ca * s - sa * dx, sa * s + ca * dx, dark ? (0.032 + (double)k * 0.003) : (0.026 + (double)k * 0.002), dark ? ((560.0 + 300.0 * u - 150.0 * u * u + (double)(side * 62)) / 1774.0) : ((double)(528 + side * 32) / 1774.0), dark ? (1.0 - (470.0 - 420.0 * u + 60.0 * u * u + (double)(side * 32)) / 887.0) : (1.0 - (835.0 - 740.0 * u) / 887.0), alpha, dark ? 1.9 : 2.15);
					}
				}
			}
		}
		else
		{
			switch (p)
			{
			case Profile.Botanical:
			{
				for (int num = 0; num < 3; num++)
				{
					double a3 = Math.Max(0.0, t - 0.004 * (double)num);
					double q2 = (1.0 - Math.Exp(-8.0 * a3)) / 8.0;
					double ang2 = PodAngle[num];
					double cx = Math.Cos(ang2) * (2.7 + 0.4 * (double)num) * q2;
					double cy = Math.Sin(ang2) * (2.7 + 0.4 * (double)num) * q2 - 1.2 * a3 * a3;
					double theta = ang2 + PodSpin[num] * a3;
					double ca2 = Math.Cos(theta);
					double sa2 = Math.Sin(theta);
					double L3 = 0.105 - 0.008 * (double)num;
					double alpha3 = Envelope(t, 0.004 * (double)num, 0.18 + 0.012 * (double)num);
					for (int num2 = 0; num2 < 13; num2++)
					{
						double u3 = (double)num2 / 12.0;
						double x2 = (u3 - 0.5) * L3;
						double mid = 0.023 * Math.Sin(Math.PI * u3);
						double hw = 0.022 * Math.Pow(Math.Sin(Math.PI * u3), 0.8);
						for (int num3 = 0; num3 < 3; num3++)
						{
							int side3 = num3 - 1;
							Put(b, b.Body, num * 39 + num2 * 3 + num3, cx + ca2 * x2 - sa2 * (mid + (double)side3 * hw), cy + sa2 * x2 + ca2 * (mid + (double)side3 * hw), 0.03 + (double)num * 0.002, u3, (double)(side3 + 1) / 2.0, alpha3, 1.5, 2, (double)num * 1.3);
						}
					}
				}
				break;
			}
			case Profile.Physical:
			{
				for (int num4 = 0; num4 < 4; num4++)
				{
					double u4 = ((num4 != 0 && num4 != 3) ? 1 : 0);
					double v = ((num4 >= 2) ? 1 : 0);
					Put(b, b.Body, num4, (u4 - 0.607) * 1.05, (v - 0.487) * 1.05, 0.02, u4 * 0.5, v, Fade(t, 0.012, 0.055) * 0.9, 1.3);
				}
				break;
			}
			default:
			{
				for (int l = 0; l < 2; l++)
				{
					double a2 = Math.Max(0.0, t);
					double lead2 = (5.8 - 1.1 * (double)l) * (1.0 - Math.Exp(-12.0 * a2)) / 12.0;
					double L2 = ((l == 0) ? 0.8 : 0.62) * (0.22 + 0.78 * (1.0 - Math.Exp(-65.0 * a2)));
					double alpha2 = Envelope(t, 0.0, 0.17) * ((l == 0) ? 0.9 : 0.42);
					for (int m = 0; m < 33; m++)
					{
						double u2 = (double)m / 32.0;
						double x = lead2 - L2 * (1.0 - u2);
						double y = 0.05 * Math.Sin(5.0 * u2 - 7.0 * a2) + ((l == 0) ? (-0.025) : 0.06);
						double w2 = ((l == 0) ? 0.15 : 0.08) * Math.Pow(Math.Sin(Math.PI * u2), 0.8) * (1.0 - 0.6 * Smooth((a2 - 0.04) / 0.1));
						for (int n = 0; n < 5; n++)
						{
							double side2 = (double)n * 0.5 - 1.0;
							Put(b, b.Body, l * 165 + m * 5 + n, x, y + side2 * w2, 0.027 + (double)l * 0.008 + 0.01 * Math.Sin(9.0 * u2 - 11.0 * a2) * (1.0 - side2 * side2), u2 + 0.12 * t, 0.5 + 0.18 * side2, alpha2, 1.4);
						}
					}
				}
				break;
			}
			}
		}
		double coreW = p switch
		{
			Profile.Physical => 0.13, 
			Profile.Botanical => 0.125, 
			Profile.Light => 0.2, 
			Profile.Dark => 0.095, 
			_ => 0.16, 
		};
		for (int num5 = 0; num5 < 4; num5++)
		{
			double u5 = ((num5 != 0 && num5 != 3) ? 1 : 0);
			double v2 = ((num5 >= 2) ? 1 : 0);
			Put(b, b.Core, num5, (u5 - 0.5) * coreW, (v2 - 0.5) * coreW * 0.25, 0.08, u5, v2, Fade(t, 0.014, 0.056), 2.7, 3);
		}
		for (int num6 = 0; num6 < 12; num6++)
		{
			double born2 = 0.004 * (double)(num6 % 6);
			double a4 = Math.Max(0.0, t - born2);
			double life = p switch
			{
				Profile.Physical => 0.2, 
				Profile.Botanical => 0.235, 
				Profile.Light => 0.16, 
				Profile.Dark => 0.18, 
				_ => 0.225, 
			} + 0.009 * (double)(num6 % 3);
			double ang3 = (double)num6 * 2.399963 + 0.18;
			double speed = p switch
			{
				Profile.Physical => 2.5, 
				Profile.Botanical => 1.6, 
				Profile.Light => 2.8, 
				Profile.Dark => 2.2, 
				_ => 2.1, 
			} + 0.17 * (double)(num6 % 4);
			double q3 = (1.0 - Math.Exp(-5.0 * a4)) / 5.0;
			if (p == Profile.Water)
			{
				ang3 = -0.7 + 0.25 * (double)(num6 % 7);
				speed += 0.7;
			}
			double cx2 = Math.Cos(ang3) * speed * q3;
			double cy2 = Math.Sin(ang3) * speed * q3 * ((p == Profile.Botanical) ? 0.65 : 0.8) - ((p == Profile.Physical || p == Profile.Water) ? 1.9 : 0.55) * a4 * a4;
			double alpha4 = Envelope(t, born2, life, 0.007);
			double theta2 = ang3 + a4 * (double)((num6 % 2 != 0) ? 5 : (-6));
			double L4 = p switch
			{
				Profile.Water => 0.032 + 0.007 * (double)(num6 % 3), 
				Profile.Botanical => 0.05 + 0.005 * (double)(num6 % 3), 
				_ => 0.037 + 0.005 * (double)(num6 % 3), 
			};
			double w3 = p switch
			{
				Profile.Water => 0.012, 
				Profile.Botanical => 0.034, 
				_ => 0.009, 
			};
			double gain = p switch
			{
				Profile.Light => 1.9, 
				Profile.Water => 1.1, 
				Profile.Botanical => 1.75, 
				Profile.Physical => 0.78, 
				_ => 1.7, 
			};
			int role = ((p == Profile.Physical || p == Profile.Water) ? 2 : ((num6 % 4 == 0) ? 3 : 2));
			double phase = ((p == Profile.Botanical) ? ((double)num6 * 1.713) : 0.0);
			if (p == Profile.Botanical)
			{
				alpha4 *= 0.82;
			}
			if (num6 >= ncount || t < 0.0)
			{
				alpha4 = 0.0;
			}
			if (p == Profile.Water)
			{
				cx2 += 0.05 * Math.Sin((double)num6 * 2.3);
				cy2 += 0.03 * Math.Cos((double)num6 * 1.7);
			}
			double ca3 = Math.Cos((p == Profile.Botanical || p == Profile.Physical) ? theta2 : ang3);
			double sa3 = Math.Sin((p == Profile.Botanical || p == Profile.Physical) ? theta2 : ang3);
			for (int num7 = 0; num7 < 4; num7++)
			{
				double x3;
				double y2;
				double u6;
				double v3;
				switch (p)
				{
				case Profile.Physical:
				{
					double sz = 0.026 + 0.005 * (double)(num6 % 3);
					x3 = num7 switch
					{
						2 => 0.62, 
						1 => 0.16, 
						0 => -0.65, 
						_ => -0.2, 
					} * sz;
					y2 = num7 switch
					{
						2 => 0.18, 
						1 => -0.5, 
						0 => -0.2, 
						_ => 0.36 + 0.06 * (double)(num6 % 2), 
					} * sz;
					u6 = ((num7 != 0 && num7 != 3) ? 1 : 0);
					v3 = ((num7 >= 2) ? 1 : 0);
					break;
				}
				case Profile.Botanical:
					x3 = ((num7 == 0 || num7 == 3) ? (-0.5) : 0.5) * L4;
					y2 = ((num7 < 2) ? (-0.5) : 0.5) * w3;
					u6 = ((num7 != 0 && num7 != 3) ? 1 : 0);
					v3 = ((num7 >= 2) ? 1 : 0);
					break;
				default:
					x3 = num7 switch
					{
						2 => 0.5, 
						0 => -0.5, 
						_ => 0.0, 
					} * L4;
					y2 = num7 switch
					{
						3 => 0.5, 
						1 => -0.5, 
						_ => 0.0, 
					} * w3;
					u6 = num7 switch
					{
						2 => 1.0, 
						0 => 0.0, 
						_ => 0.5, 
					};
					v3 = num7 switch
					{
						3 => 1.0, 
						1 => 0.0, 
						_ => 0.5, 
					};
					break;
				}
				Put(b, b.Particles, num6 * 4 + num7, cx2 + ca3 * x3 - sa3 * y2, cy2 + sa3 * x3 + ca3 * y2, 0.05, u6, v3, alpha4, gain, role, phase);
			}
			b.ParticleCenters[num6] = new Point((double)b.MirrorRight * cx2, cy2, 0.05);
			b.ParticleActive[num6] = alpha4 > 0.0;
		}
		if (t < 0.0)
		{
			Array.Clear(b.Body.Alpha, 0, b.Body.Alpha.Length);
			Array.Clear(b.Core.Alpha, 0, 4);
		}
		b.LightEnvelope = LightEnvelope(p, t);
		b.TargetAreaW = 30.0 * b.LightEnvelope;
		b.FloorAreaW = 10.0 * b.LightEnvelope;
		Point min = b.Core.Vertices[0];
		Point max = min;
		for (int num8 = 0; num8 < 3; num8++)
		{
			for (int num9 = 0; num9 < b.Batches[num8].Vertices.Length; num9++)
			{
				Point v4 = b.Batches[num8].Vertices[num9];
				min.X = Math.Min(min.X, v4.X);
				min.Y = Math.Min(min.Y, v4.Y);
				min.Z = Math.Min(min.Z, v4.Z);
				max.X = Math.Max(max.X, v4.X);
				max.Y = Math.Max(max.Y, v4.Y);
				max.Z = Math.Max(max.Z, v4.Z);
			}
		}
		b.SampleBounds = new Bounds(min, max);
	}

	public static bool SampleReceipt(Buffer b, double age, double availableA = 0.0, int count = 12)
	{
		double? native = NativeAge(age, availableA);
		Sample(b, age, count);
		return native.HasValue;
	}
}
