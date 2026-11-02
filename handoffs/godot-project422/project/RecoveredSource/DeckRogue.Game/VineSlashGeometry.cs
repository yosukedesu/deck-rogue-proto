using System;

namespace DeckRogue.Game;

internal static class VineSlashGeometry
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
		public Point[] Vertices;

		public Uv[] UV;

		public double[] Alpha;

		public int[] Triangles;
	}

	public sealed class Buffer
	{
		public MeshData Primary;

		public MeshData Secondary;

		public MeshData Core;

		public MeshData Particles;

		public double VisualAge;

		public double LightEnvelope;
	}

	public const double Anticipation = 0.066666667;

	public const double VisibleEnd = 0.182;

	public const double Horizon = 0.333333333;

	private static readonly double[,] Knots;

	private static readonly double[,] UVProfile;

	private static readonly double[,] ParticleSpecs;

	private static readonly Point[] ParticleStarts;

	private static readonly Point[] ParticleVelocities;

	static VineSlashGeometry()
	{
		Knots = new double[6, 3]
		{
			{ -0.066666667, -0.8, 1.0 },
			{ -0.035, -0.65, 10.0 },
			{ 0.0, 0.0, 22.0 },
			{ 0.035, 0.67, 10.0 },
			{ 0.11, 1.05, 2.0 },
			{ 0.333333333, 1.25, 0.0 }
		};
		UVProfile = new double[49, 3]
		{
			{ 0.0, 0.5, 0.05 },
			{
				1.0 / 48.0,
				0.37430939226519333,
				0.013
			},
			{
				1.0 / 24.0,
				0.3604972375690608,
				0.013
			},
			{ 0.0625, 0.34806629834254144, 0.013 },
			{
				1.0 / 12.0,
				0.3370165745856354,
				0.013
			},
			{
				0.10416666666666666,
				119.0 / 362.0,
				0.013
			},
			{ 0.125, 0.32182320441988954, 0.013 },
			{ 0.14583333333333331, 0.3162983425414365, 0.015469613259668507 },
			{
				1.0 / 6.0,
				227.0 / 724.0,
				0.01740331491712707
			},
			{ 0.1875, 0.3107734806629834, 0.020303867403314916 },
			{ 0.20833333333333331, 0.3107734806629834, 0.04157458563535912 },
			{
				11.0 / 48.0,
				0.3121546961325967,
				0.04640883977900552
			},
			{ 0.25, 0.3149171270718232, 0.050276243093922646 },
			{
				13.0 / 48.0,
				0.3204419889502762,
				0.05414364640883977
			},
			{
				0.29166666666666663,
				235.0 / 724.0,
				0.08218232044198895
			},
			{
				0.3125,
				119.0 / 362.0,
				0.0841160220994475
			},
			{
				1.0 / 3.0,
				241.0 / 724.0,
				63.0 / 724.0
			},
			{
				0.35416666666666663,
				0.33563535911602205,
				63.0 / 724.0
			},
			{ 0.375, 0.33977900552486184, 0.08798342541436464 },
			{
				19.0 / 48.0,
				249.0 / 724.0,
				0.0947513812154696
			},
			{ 0.41666666666666663, 0.35635359116022103, 0.12859116022099448 },
			{
				0.4375,
				269.0 / 724.0,
				0.10732044198895027
			},
			{
				11.0 / 24.0,
				277.0 / 724.0,
				0.11505524861878452
			},
			{ 0.47916666666666663, 0.39364640883977897, 0.12182320441988949 },
			{
				0.5,
				147.0 / 362.0,
				91.0 / 724.0
			},
			{
				0.5208333333333333,
				149.0 / 362.0,
				0.12762430939226518
			},
			{
				13.0 / 24.0,
				0.419889502762431,
				0.13439226519337016
			},
			{ 0.5625, 0.425414364640884, 0.13632596685082873 },
			{
				0.5833333333333333,
				315.0 / 724.0,
				0.14212707182320442
			},
			{
				29.0 / 48.0,
				0.4475138121546961,
				0.10055248618784529
			},
			{
				0.625,
				169.0 / 362.0,
				0.11505524861878452
			},
			{ 0.6458333333333333, 0.4875690607734806, 0.1102209944751381 },
			{
				2.0 / 3.0,
				0.5013812154696133,
				0.10635359116022099
			},
			{
				0.6875,
				373.0 / 724.0,
				0.15566298342541435
			},
			{ 0.7083333333333333, 0.5193370165745856, 0.15759668508287292 },
			{
				35.0 / 48.0,
				191.0 / 362.0,
				0.10828729281767954
			},
			{
				0.75,
				197.0 / 362.0,
				0.10828729281767954
			},
			{ 0.7708333333333333, 0.569060773480663, 0.11312154696132597 },
			{
				19.0 / 24.0,
				213.0 / 362.0,
				0.11215469613259668
			},
			{ 0.8125, 0.6049723756906078, 0.10732044198895027 },
			{
				0.8333333333333333,
				447.0 / 724.0,
				0.08218232044198895
			},
			{
				41.0 / 48.0,
				231.0 / 362.0,
				0.02803867403314917
			},
			{ 0.875, 0.6574585635359116, 0.025138121546961323 },
			{ 0.8958333333333333, 0.6781767955801106, 0.025138121546961323 },
			{
				11.0 / 12.0,
				507.0 / 724.0,
				0.022237569060773482
			},
			{
				0.9375,
				263.0 / 362.0,
				7.0 / 362.0
			},
			{
				0.9583333333333333,
				275.0 / 362.0,
				0.013535911602209943
			},
			{
				47.0 / 48.0,
				581.0 / 724.0,
				0.013
			},
			{ 1.0, 0.5, 0.05 }
		};
		ParticleSpecs = new double[6, 4]
		{
			{ 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
			{ 0.0, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
			{ 0.0, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 },
			{ 0.022, 0.5538025156552036, 0.781381770579271, 0.0552109878293629 },
			{ 0.022, -0.6328030518934236, 0.9867369945327584, 0.030297687296996265 },
			{ 0.022, 0.061664644824827586, 0.8502843849475583, 0.043524457601980884 }
		};
		ParticleStarts = new Point[6];
		ParticleVelocities = new Point[6];
		for (int i = 0; i < 6; i++)
		{
			double num = ParticleSpecs[i, 0];
			double eps = 1E-05;
			Point p = Center(num, secondary: false, 46);
			Point a = Center(num + eps, secondary: false, 46);
			Point b = Center(num - eps, secondary: false, 46);
			double x = (a.X - b.X) / (2.0 * eps) * 0.3 + Math.Cos(ParticleSpecs[i, 1]) * ParticleSpecs[i, 2] * 0.45;
			double y = (a.Y - b.Y) / (2.0 * eps) * 0.3 + Math.Sin(ParticleSpecs[i, 1]) * ParticleSpecs[i, 2] + 0.45;
			double z = (a.Z - b.Z) / (2.0 * eps) * 0.3 + 0.04 * (double)(i % 3 - 1);
			double r = Math.Sqrt(x * x + y * y);
			double sign = ((i % 2 == 1) ? 2.1 : (-2.1));
			ParticleStarts[i] = p;
			ParticleVelocities[i] = new Point(x - y / r * sign, y + x / r * sign, z);
		}
	}

	private static double Clamp(double q)
	{
		return Math.Max(0.0, Math.Min(1.0, q));
	}

	private static double Smooth(double q)
	{
		q = Clamp(q);
		return q * q * (3.0 - 2.0 * q);
	}

	public static double VisualClock(double age, double available = -1.0)
	{
		if (age >= 0.0 || available < 0.0 || available >= 0.066666667 || available <= 0.0)
		{
			return age;
		}
		if (age < 0.0 - available)
		{
			return -0.066667667;
		}
		double q = Clamp((age + available) / available);
		double r = available / 0.066666667;
		return -0.066666667 + 0.066666667 * (q + (r - 1.0) * (q * q * q - q * q));
	}

	private static double Lead(double t)
	{
		if (t <= Knots[0, 0])
		{
			return Knots[0, 1];
		}
		if (t >= 0.333333333)
		{
			return Knots[5, 1];
		}
		int i;
		for (i = 0; i < 4 && t > Knots[i + 1, 0]; i++)
		{
		}
		double d = Knots[i + 1, 0] - Knots[i, 0];
		double q = (t - Knots[i, 0]) / d;
		double q2 = q * q;
		double q3 = q2 * q;
		return (2.0 * q3 - 3.0 * q2 + 1.0) * Knots[i, 1] + (q3 - 2.0 * q2 + q) * d * Knots[i, 2] + (-2.0 * q3 + 3.0 * q2) * Knots[i + 1, 1] + (q3 - q2) * d * Knots[i + 1, 2];
	}

	private static Point Center(double t, bool secondary, int i)
	{
		double u = (double)i / 48.0;
		double s = Lead(t - (secondary ? 0.028 : 0.0)) - 1.1 * (secondary ? 0.55 : 1.0) * (1.0 - u);
		double x = 2.0 * Math.Sin(s / 2.0);
		double y = 2.0 * (1.0 - Math.Cos(s / 2.0));
		double c = Math.Cos(-0.6);
		double a = Math.Sin(-0.6);
		return new Point(c * x - a * y, a * x + c * y - (secondary ? (0.11 * Math.Sin(Math.PI * u)) : 0.0), 0.016 * Math.Sin(u * 5.0 + t * 6.0) + (secondary ? 0.013 : 0.0));
	}

	private static double BaseAlpha(double t)
	{
		if (!(t >= -0.066666667) || !(t < 0.333333333))
		{
			return 0.0;
		}
		return Smooth((t + 0.066666667) / Math.Min(0.01666666675, 0.025)) * (1.0 - Smooth((t - 0.08) / 0.253333333));
	}

	public static double CoreAlpha(double t)
	{
		if (!(t >= 0.0) || !(t < 0.333333333))
		{
			return 0.0;
		}
		return 1.0 - Smooth((t - 0.045) / 0.035);
	}

	public static double Light(double t)
	{
		return ((t >= 0.0) ? CoreAlpha(t) : ((t >= -0.066666667) ? (0.025 * Math.Pow(Smooth((t + 0.066666667) / 0.066666667), 3.0)) : 0.0)) * (1.0 - Smooth((t - 0.1) / 0.15));
	}

	private static MeshData Mesh(int count, int quads)
	{
		return new MeshData
		{
			Vertices = new Point[count],
			UV = new Uv[count],
			Alpha = new double[count],
			Triangles = new int[quads * 6]
		};
	}

	private static void Quad(int[] t, int q, int a, int b, int c, int d)
	{
		int k = q * 6;
		t[k] = a;
		t[k + 1] = b;
		t[k + 2] = c;
		t[k + 3] = a;
		t[k + 4] = c;
		t[k + 5] = d;
	}

	public static Buffer Create()
	{
		Buffer b = new Buffer
		{
			Primary = Mesh(147, 96),
			Secondary = Mesh(147, 96),
			Core = Mesh(12, 3),
			Particles = Mesh(24, 6)
		};
		MeshData[] array = new MeshData[2] { b.Primary, b.Secondary };
		foreach (MeshData m in array)
		{
			for (int j = 0; j < 49; j++)
			{
				for (int k = 0; k < 3; k++)
				{
					m.UV[j * 3 + k] = new Uv(UVProfile[j, 0], UVProfile[j, 1] + UVProfile[j, 2] * (double)(k - 1));
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
		for (int num = 0; num < 3; num++)
		{
			Quad(b.Core.Triangles, num, num * 4, num * 4 + 1, num * 4 + 2, num * 4 + 3);
		}
		for (int num2 = 0; num2 < 6; num2++)
		{
			Quad(b.Particles.Triangles, num2, num2 * 4, num2 * 4 + 1, num2 * 4 + 2, num2 * 4 + 3);
		}
		return b;
	}

	private static void Ribbon(MeshData m, double t, bool secondary, bool mirror)
	{
		double alpha = BaseAlpha(t) * (secondary ? (0.55 * Smooth((t - 0.018) / 0.016) * (1.0 - Smooth((t - 0.055) / 0.08))) : (0.92 * (1.0 - Smooth((t - 0.04) / 0.065))));
		for (int i = 0; i < 49; i++)
		{
			Point p = Center(t, secondary, i);
			Point a = Center(t, secondary, Math.Max(0, i - 1));
			Point point = Center(t, secondary, Math.Min(48, i + 1));
			double dx = point.X - a.X;
			double dy = point.Y - a.Y;
			double r = Math.Sqrt(dx * dx + dy * dy);
			double width = 0.045 * Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * (double)i / 48.0)), 0.85) * (secondary ? 0.24 : 1.0);
			for (int j = 0; j < 3; j++)
			{
				double side = j - 1;
				double x = p.X - dy / r * width * side;
				m.Vertices[i * 3 + j] = new Point(mirror ? (0.0 - x) : x, p.Y + dx / r * width * side, p.Z + 0.008 * (1.0 - side * side));
				m.Alpha[i * 3 + j] = alpha;
			}
		}
	}

	private static void Diamond(MeshData m, int offset, Point p, double size, double angle, double halfWidth, double shrink, double alpha, bool mirror)
	{
		double c = Math.Cos(angle);
		double s = Math.Sin(angle);
		for (int j = 0; j < 4; j++)
		{
			double x = j switch
			{
				2 => 0.5 * shrink, 
				0 => -0.5 * shrink, 
				_ => 0.0, 
			};
			double y = j switch
			{
				3 => halfWidth, 
				1 => 0.0 - halfWidth, 
				_ => 0.0, 
			};
			double vx = p.X + size * (c * x - s * y);
			m.Vertices[offset + j] = new Point(mirror ? (0.0 - vx) : vx, p.Y + size * (s * x + c * y), p.Z);
			m.Alpha[offset + j] = alpha;
		}
	}

	public static void Sample(Buffer b, double age, bool mirrorX = false, double availableAnticipation = -1.0)
	{
		double t = (b.VisualAge = VisualClock(age, availableAnticipation));
		b.LightEnvelope = Light(t);
		Ribbon(b.Primary, t, secondary: false, mirrorX);
		Ribbon(b.Secondary, t, secondary: true, mirrorX);
		for (int i = 0; i < 3; i++)
		{
			MeshData core = b.Core;
			int offset = i * 4;
			Point p = new Point(0.012 * (double)i, 0.008 * (double)(i - 1), 0.030434782608695657);
			double size = 0.12 * (1.0 - 0.24 * (double)i);
			Diamond(core, offset, p, size, i switch
			{
				1 => -0.51, 
				0 => -0.44, 
				_ => -0.36, 
			}, 0.045, 1.0, CoreAlpha(t), mirrorX);
		}
		for (int j = 0; j < 6; j++)
		{
			double a = t - ParticleSpecs[j, 0];
			int o = j * 4;
			if (t < -0.066666667 || t >= 0.333333333 || a < 0.0 || a >= 0.16)
			{
				for (int k = 0; k < 4; k++)
				{
					b.Particles.Vertices[o + k] = default;
					b.Particles.Alpha[o + k] = 0.0;
				}
			}
			else
			{
				Point p2 = ParticleStarts[j];
				Point v = ParticleVelocities[j];
				double k2 = 2.4;
				double q = (1.0 - Math.Exp((0.0 - k2) * a)) / k2;
				Diamond(p: new Point(p2.X + v.X * q, p2.Y + v.Y * q - 1.6 * (a / k2 - q / k2), p2.Z + v.Z * q + 0.026086956521739132), m: b.Particles, offset: o, size: ParticleSpecs[j, 3] * 1.45, angle: Math.Atan2(v.Y - 1.6 * a, v.X), halfWidth: 0.12, shrink: 1.0 - 0.35 * a / 0.16, alpha: 1.0 - Smooth(a / 0.16), mirror: mirrorX);
			}
		}
	}
}
