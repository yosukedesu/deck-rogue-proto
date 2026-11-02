using System;

namespace DeckRogue.Game;

internal static class EnemyContactGeometry
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

		public double[] Gain;

		public int[] Role;

		public int[] Triangles;
	}

	public sealed class Buffer
	{
		public string Family;

		public MeshData[] Batches;

		public double LightEnvelope;
	}

	public const double ClawAnticipation = 0.066666667;

	public const double ClawEnd = 0.172;

	public const double BluntEnd = 0.222;

	public const double FangAnticipation = 0.09;

	public const double FangEnd = 0.14;

	public const double BeamAnticipation = 0.08;

	public const double BeamEnd = 0.16;

	private static readonly double[,] UVProfile = new double[49, 3]
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

	private static readonly double[,] Knots = new double[6, 3]
	{
		{ -0.066666667, -0.8, 1.0 },
		{ -0.035, -0.65, 10.0 },
		{ 0.0, 0.0, 22.0 },
		{ 0.035, 0.67, 10.0 },
		{ 0.11, 1.05, 2.0 },
		{ 0.19, 1.15, 0.0 }
	};

	private static double Clamp(double v)
	{
		return Math.Max(0.0, Math.Min(1.0, v));
	}

	private static double Smooth(double v)
	{
		double q = Clamp(v);
		return q * q * (3.0 - 2.0 * q);
	}

	private static double Envelope(double t, double birth, double end, double fadein)
	{
		if (!(birth < t) || !(t < end))
		{
			return 0.0;
		}
		return Smooth((t - birth) / fadein) * (1.0 - Smooth((t - birth) / (end - birth)));
	}

	public static double Light(double t)
	{
		if (!(t >= 0.0) || !(t < 0.08))
		{
			return 0.0;
		}
		return 1.0 - Smooth((t - 0.015) / 0.065);
	}

	public static double End(string family)
	{
		return family switch
		{
			"beam" => 0.16, 
			"fang" => 0.14, 
			"claw" => 0.172, 
			_ => 0.222, 
		};
	}

	public static double Light(string family, double t)
	{
		if (family == "fang" || family == "beam")
		{
			if (!(t >= 0.0) || !(t < 0.074))
			{
				return 0.0;
			}
			return 1.0 - Smooth((t - 0.014) / 0.06);
		}
		return Light(t);
	}

	public static double BeamClock(double age, double available)
	{
		if (age >= 0.0 || available >= 0.08 || available <= 0.0)
		{
			return age;
		}
		if (age < 0.0 - available)
		{
			return -0.080001;
		}
		double q = Clamp((age + available) / available);
		double ratio = available / 0.08;
		return -0.08 + 0.08 * (q + (ratio - 1.0) * (q * q * q - q * q));
	}

	private static MeshData Mesh(int n, int quads)
	{
		return new MeshData
		{
			Vertices = new Point[n],
			UV = new Uv[n],
			Alpha = new double[n],
			Gain = new double[n],
			Role = new int[n],
			Triangles = new int[quads * 6]
		};
	}

	private static void Quad(int[] a, int quad, int v0, int v1, int v2, int v3)
	{
		int j = quad * 6;
		a[j] = v0;
		a[j + 1] = v1;
		a[j + 2] = v2;
		a[j + 3] = v0;
		a[j + 4] = v2;
		a[j + 5] = v3;
	}

	public static Buffer Create(string family)
	{
		if (family == "fang" || family == "beam")
		{
			return CreateFangBeam(family);
		}
		if (family != "blunt" && family != "claw")
		{
			throw new ArgumentException("Unknown physical family", "family");
		}
		bool claw = family == "claw";
		Buffer buffer = new Buffer();
		buffer.Family = family;
		buffer.Batches = (claw ? new MeshData[3]
		{
			Mesh(441, 288),
			Mesh(12, 3),
			Mesh(32, 8)
		} : new MeshData[3]
		{
			Mesh(4, 1),
			Mesh(104, 48),
			Mesh(40, 10)
		});
		Buffer b = buffer;
		MeshData surface = b.Batches[0];
		if (claw)
		{
			for (int k = 0; k < 3; k++)
			{
				for (int i = 0; i < 49; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						surface.UV[k * 147 + i * 3 + j] = new Uv(UVProfile[i, 0], UVProfile[i, 1] + UVProfile[i, 2] * (double)(j - 1));
					}
				}
				for (int l = 0; l < 48; l++)
				{
					for (int m = 0; m < 2; m++)
					{
						Quad(surface.Triangles, k * 96 + l * 2 + m, k * 147 + l * 3 + m, k * 147 + l * 3 + m + 1, k * 147 + (l + 1) * 3 + m + 1, k * 147 + (l + 1) * 3 + m);
					}
				}
			}
		}
		else
		{
			surface.UV[0] = new Uv(0.0, 0.0);
			surface.UV[1] = new Uv(0.5, 0.0);
			surface.UV[2] = new Uv(0.5, 1.0);
			surface.UV[3] = new Uv(0.0, 1.0);
			Quad(surface.Triangles, 0, 0, 1, 2, 3);
		}
		MeshData solid = b.Batches[1];
		if (claw)
		{
			for (int n = 0; n < 3; n++)
			{
				Quad(solid.Triangles, n, n * 4, n * 4 + 1, n * 4 + 2, n * 4 + 3);
			}
		}
		else
		{
			for (int num = 0; num < 4; num++)
			{
				for (int num2 = 0; num2 < 12; num2++)
				{
					Quad(solid.Triangles, num * 12 + num2, num * 26 + num2 * 2, num * 26 + num2 * 2 + 1, num * 26 + (num2 + 1) * 2 + 1, num * 26 + (num2 + 1) * 2);
				}
			}
		}
		MeshData particles = b.Batches[2];
		for (int num3 = 0; num3 < (claw ? 8 : 10); num3++)
		{
			Quad(particles.Triangles, num3, num3 * 4, num3 * 4 + 1, num3 * 4 + 2, num3 * 4 + 3);
		}
		return b;
	}

	private static void Vertex(MeshData m, int i, double x, double y, double z, double alpha, double gain, int role = 0)
	{
		m.Vertices[i] = new Point(x, y, z);
		m.Alpha[i] = alpha;
		m.Gain[i] = gain;
		m.Role[i] = role;
	}

	private static void Diamond(MeshData m, int o, double cx, double cy, double cz, double length, double width, double angle, double alpha, int role, double gain)
	{
		double c = Math.Cos(angle);
		double s = Math.Sin(angle);
		for (int j = 0; j < 4; j++)
		{
			double x = j switch
			{
				2 => 0.5, 
				0 => -0.5, 
				_ => 0.0, 
			} * length;
			double y = j switch
			{
				3 => 0.5, 
				1 => -0.5, 
				_ => 0.0, 
			} * width;
			Vertex(m, o + j, cx + c * x - s * y, cy + s * x + c * y, cz, alpha, gain, role);
		}
	}

	private static double Lead(double t)
	{
		if (t <= Knots[0, 0])
		{
			return Knots[0, 1];
		}
		if (t >= Knots[5, 0])
		{
			return Knots[5, 1];
		}
		int i;
		for (i = 0; i < 4 && t > Knots[i + 1, 0]; i++)
		{
		}
		double dt = Knots[i + 1, 0] - Knots[i, 0];
		double q = (t - Knots[i, 0]) / dt;
		double q2 = q * q;
		double q3 = q2 * q;
		return (2.0 * q3 - 3.0 * q2 + 1.0) * Knots[i, 1] + (q3 - 2.0 * q2 + q) * dt * Knots[i, 2] + (-2.0 * q3 + 3.0 * q2) * Knots[i + 1, 1] + (q3 - q2) * dt * Knots[i + 1, 2];
	}

	private static Point Center(double lead, double length, double grow, double off, int i)
	{
		double u = (double)i / 48.0;
		double s = lead - length * grow * (1.0 - u);
		double x = 2.0 * Math.Sin(s / 2.0);
		double y = 2.0 * (1.0 - Math.Cos(s / 2.0));
		double ca = Math.Cos(-0.6);
		double sa = Math.Sin(-0.6);
		return new Point(ca * x - sa * y - sa * off, sa * x + ca * y + ca * off, 0.0);
	}

	private static void Claw(Buffer b, double t)
	{
		MeshData surface = b.Batches[0];
		double ang = -0.6;
		double ca = Math.Cos(ang);
		double sa = Math.Sin(ang);
		double grow = 0.05 + 0.95 * Smooth((t + 0.056) / 0.056);
		double lead = Lead(t);
		double alpha = ((t >= -0.066666667 && t < 0.115) ? (Smooth((t + 0.066666667) / 0.016) * (1.0 - Smooth((t - 0.025) / 0.09))) : 0.0);
		for (int k = 0; k < 3; k++)
		{
			double length = k switch
			{
				1 => 0.8, 
				0 => 0.66, 
				_ => 0.6, 
			};
			double w = k switch
			{
				1 => 0.032, 
				0 => 0.024, 
				_ => 0.023, 
			};
			double off = k switch
			{
				1 => 0.0, 
				0 => 0.13, 
				_ => -0.13, 
			};
			for (int i = 0; i < 49; i++)
			{
				double u = (double)i / 48.0;
				Point p = Center(lead, length, grow, off, i);
				Point p2 = Center(lead, length, grow, off, Math.Max(0, i - 1));
				Point point = Center(lead, length, grow, off, Math.Min(48, i + 1));
				double dx = point.X - p2.X;
				double dy = point.Y - p2.Y;
				double r = Math.Sqrt(dx * dx + dy * dy);
				double width = w * Math.Pow(Math.Sin(Math.PI * u), 0.82) * Math.Sqrt(grow);
				for (int j = 0; j < 3; j++)
				{
					double side = j - 1;
					Vertex(surface, k * 147 + i * 3 + j, p.X - dy / r * width * side, p.Y + dx / r * width * side, 0.012 + 0.005 * (double)k + 0.006 * (1.0 - side * side), alpha * ((k == 1) ? 1.0 : 0.83), (k == 1) ? 1.8 : 1.55);
				}
			}
			double core = ((t >= 0.0 && t < 0.04) ? (1.0 - Smooth((t - 0.014) / 0.026)) : 0.0);
			Diamond(b.Batches[1], k * 4, (0.0 - sa) * off, ca * off, 0.052, (k == 1) ? 0.085 : 0.058, 0.008, ang, core, 2, 3.0);
		}
		for (int l = 0; l < 8; l++)
		{
			double birth = 0.006 * (double)(l % 3);
			double age = t - birth;
			double at = Math.Max(0.0, age);
			double q = (1.0 - Math.Exp(-5.0 * at)) / 5.0;
			double v = 3.2 + 0.21 * (double)l;
			double angle = ang + 0.21 * Math.Sin((double)l * 2.9);
			double off2;
			if (l % 3 != 0)
			{
				off2 = ((l % 3 == 1) ? 0.0 : (-0.13));
			}
			else
			{
				off2 = 0.13;
			}
			double x = (0.0 - sa) * off2 + Math.Cos(angle) * v * q;
			double y = ca * off2 + Math.Sin(angle) * v * q - 0.9 * at * at;
			Diamond(b.Batches[2], l * 4, x, y, 0.058, (0.043 + 0.006 * (double)(l % 3)) * (1.0 - 0.4 * Clamp(age / 0.16)), 0.007, angle, Envelope(t, birth, birth + 0.16, 0.003), (l % 3 != 0) ? 1 : 2, 1.7);
		}
	}

	private static void Blunt(Buffer b, double t)
	{
		double alpha = ((t >= 0.0 && t < 0.049) ? (1.0 - Smooth((t - 0.012) / 0.037)) : 0.0);
		for (int j = 0; j < 4; j++)
		{
			double u = ((j == 1 || j == 2) ? 1 : 0);
			double v = ((j >= 2) ? 1 : 0);
			Vertex(b.Batches[0], j, (u - 0.607) * 1.15, (v - 0.487) * 1.15, 0.018, alpha * 0.95, 1.3);
		}
		double age = Math.Max(0.0, t);
		double q = 1.0 - Math.Exp(-9.0 * age);
		double radius = 0.065 + 0.28 * q;
		for (int i = 0; i < 4; i++)
		{
			double start = i switch
			{
				2 => 2.6, 
				1 => 1.1, 
				0 => -0.52, 
				_ => 4.2, 
			};
			double end = i switch
			{
				2 => 3.32, 
				1 => 1.75, 
				0 => 0.28, 
				_ => 4.71, 
			};
			for (int k = 0; k < 13; k++)
			{
				double u2 = (double)k / 12.0;
				double angle = start + (end - start) * u2;
				double w = 0.014 * (1.0 - 0.5 * q) * Math.Sin(Math.PI * u2);
				for (int l = 0; l < 2; l++)
				{
					double side = ((l != 0) ? 1 : (-1));
					double r = radius + 0.007 * q * Math.Sin(angle * 17.0 + (double)i * 1.7) + side * w;
					Vertex(b.Batches[1], i * 26 + k * 2 + l, Math.Cos(angle) * r, Math.Sin(angle) * r * 0.58, 0.025, Envelope(t, 0.0, 0.165, 0.004) * 0.43, 0.75);
				}
			}
		}
		for (int m = 0; m < 10; m++)
		{
			double birth = 0.003 * (double)(m % 3);
			double at = Math.Max(0.0, t - birth);
			double life = 0.18 + 0.013 * (double)(m % 4);
			double angle2 = Math.PI * 2.0 * (double)m / 10.0 + 0.13 * Math.Sin((double)m * 3.1);
			double speed = 2.0 + 0.19 * (double)(m % 4);
			double travel = (1.0 - Math.Exp(-4.0 * at)) / 4.0;
			double x = Math.Cos(angle2) * speed * travel;
			double y = Math.Sin(angle2) * speed * travel * 0.66 - 1.65 * at * at;
			Diamond(b.Batches[2], m * 4, x, y, 0.034 + 0.002 * (double)(m % 3), (0.028 + 0.005 * (double)(m % 3)) * (1.0 - 0.25 * Clamp(at / life)), 0.016, angle2 + at * (double)((m % 2 != 0) ? 8 : (-6)), Envelope(t, birth, birth + life, 0.004), (m % 3 == 0) ? 1 : 0, (m % 3 == 0) ? 1.1 : 0.72);
		}
	}

	private static Buffer CreateFangBeam(string family)
	{
		bool fang = family == "fang";
		Buffer buffer = new Buffer();
		buffer.Family = family;
		buffer.Batches = (fang ? new MeshData[3]
		{
			Mesh(8, 2),
			Mesh(8, 2),
			Mesh(32, 8)
		} : new MeshData[3]
		{
			Mesh(287, 240),
			Mesh(12, 3),
			Mesh(36, 9)
		});
		Buffer b = buffer;
		MeshData surface = b.Batches[0];
		if (fang)
		{
			for (int i = 0; i < 2; i++)
			{
				int o = i * 4;
				Quad(surface.Triangles, i, o, o + 1, o + 2, o + 3);
				surface.UV[o] = new Uv(0.0, 0.0);
				surface.UV[o + 1] = new Uv(1.0, 0.0);
				surface.UV[o + 2] = new Uv(1.0, 1.0);
				surface.UV[o + 3] = new Uv(0.0, 1.0);
			}
		}
		else
		{
			for (int j = 0; j < 40; j++)
			{
				for (int k = 0; k < 6; k++)
				{
					Quad(surface.Triangles, j * 6 + k, j * 7 + k, j * 7 + k + 1, (j + 1) * 7 + k + 1, (j + 1) * 7 + k);
				}
			}
		}
		for (int l = 1; l < 3; l++)
		{
			MeshData m = b.Batches[l];
			for (int n = 0; n < m.Vertices.Length / 4; n++)
			{
				Quad(m.Triangles, n, n * 4, n * 4 + 1, n * 4 + 2, n * 4 + 3);
			}
		}
		return b;
	}

	private static void Fang(Buffer b, double t)
	{
		double q = Clamp((t + 0.09) / 0.09);
		double gape = ((t < 0.0) ? (0.18 * (1.0 - Math.Pow(q, 2.5))) : (0.05 * Smooth((t - 0.01) / 0.055)));
		double a = ((t >= -0.09 && t < 0.14) ? (Smooth((t + 0.09) / 0.018) * (1.0 - Smooth((t - 0.04) / 0.1))) : 0.0);
		for (int i = 0; i < 2; i++)
		{
			double side = ((i == 0) ? 1 : (-1));
			double width = ((i == 0) ? 0.62 : 0.57);
			double height = width * 809.0 / 1942.0;
			for (int j = 0; j < 4; j++)
			{
				double u = ((j == 1 || j == 2) ? 1 : 0);
				double v = ((j >= 2) ? 1 : 0);
				Vertex(b.Batches[0], i * 4 + j, side * width * (u - 0.5), side * (height * (v - 0.14833127317676142) + gape), 0.016, a * 0.91, 0.96);
			}
		}
		double core = ((t >= 0.0 && t < 0.035) ? (1.0 - Smooth((t - 0.01) / 0.025)) : 0.0);
		for (int k = 0; k < 2; k++)
		{
			Diamond(b.Batches[1], k * 4, (k == 0) ? (-0.174) : 0.174, 0.0, 0.05, 0.055, 0.007, 0.0, core, 2, 2.3);
		}
		for (int l = 0; l < 8; l++)
		{
			double age = Math.Max(t, 0.0);
			double direction = ((l % 2 != 0) ? 1 : (-1));
			double angle = ((direction == 1.0) ? 0.0 : Math.PI) + 0.28 * Math.Sin((double)l * 1.7);
			double travel = (1.0 - Math.Exp(-5.0 * age)) / 5.0;
			double speed = 1.6 + 0.2 * (double)(l % 3);
			Diamond(b.Batches[2], l * 4, direction * 0.174 + Math.Cos(angle) * speed * travel, Math.Sin(angle) * speed * travel - 1.6 * age * age, 0.045, 0.022 + 0.003 * (double)(l % 3), 0.008, angle + age * (double)((l % 2 != 0) ? 7 : (-7)), Envelope(t, 0.0, 0.135, 0.004), 1, 1.3);
		}
	}

	private static void Beam(Buffer b, double t)
	{
		double q = Smooth((t + 0.08) / 0.08);
		double head = ((t < 0.0) ? (-1.08 * (1.0 - q)) : 0.0);
		double tail = ((t < 0.02) ? (-1.08) : (-1.08 * (1.0 - Smooth((t - 0.02) / 0.085))));
		double alpha = ((t >= -0.08 && t < 0.105) ? (Smooth((t + 0.08) / 0.016) * (1.0 - Smooth((t - 0.08) / 0.025))) : 0.0);
		for (int i = 0; i < 41; i++)
		{
			double u = (double)i / 40.0;
			double x = tail + (head - tail) * u;
			double y = (0.0 - x) * 0.065;
			double w = 0.03 * Math.Pow(Math.Sin(Math.PI * u), 0.32) * Math.Min(1.0, Math.Sqrt(Math.Max(0.0, head - tail) / 0.25));
			for (int j = 0; j < 7; j++)
			{
				double v = (double)j / 6.0;
				double side = v * 2.0 - 1.0;
				int k = i * 7 + j;
				Vertex(b.Batches[0], k, x, y + w * side, 0.02 + 0.006 * (1.0 - side * side), alpha, 1.8);
				b.Batches[0].UV[k] = new Uv((x + 1.08) / 1.08, v);
			}
		}
		double core = ((t >= 0.0 && t < 0.06) ? (1.0 - Smooth((t - 0.012) / 0.048)) : 0.0);
		for (int l = 0; l < 3; l++)
		{
			MeshData m = b.Batches[1];
			int o = l * 4;
			double cx = 0.018 + 0.018 * (double)l;
			double cy = 0.014 * (double)(l - 1);
			Diamond(m, o, cx, cy, 0.051, l switch
			{
				1 => 0.07, 
				0 => 0.095, 
				_ => 0.058, 
			}, 0.007, l switch
			{
				1 => -0.15, 
				0 => 0.1, 
				_ => 0.25, 
			}, core, 2, 2.7);
		}
		for (int n = 0; n < 9; n++)
		{
			double birth = 0.005 * (double)(n % 3);
			double age = Math.Max(0.0, t - birth);
			double angle = -0.9 + 0.22 * (double)n;
			double speed = 1.9 + 0.2 * (double)(n % 4);
			double travel = (1.0 - Math.Exp(-4.0 * age)) / 4.0;
			Diamond(b.Batches[2], n * 4, Math.Cos(angle) * speed * travel, Math.Sin(angle) * speed * travel - 0.6 * age * age, 0.048, 0.026 + 0.005 * (double)(n % 3), 0.005, angle, Envelope(t, birth, birth + 0.15, 0.004), (n % 3 != 0) ? 1 : 2, 1.4);
		}
	}

	private static bool Finite(double v)
	{
		if (!double.IsNaN(v))
		{
			return !double.IsInfinity(v);
		}
		return false;
	}

	public static bool FitBeam(Buffer b, Point sourceH, double size = 1.0)
	{
		if (b.Family != "beam")
		{
			return false;
		}
		double length = Math.Sqrt(sourceH.X * sourceH.X + sourceH.Y * sourceH.Y);
		bool num = Finite(length) && Finite(sourceH.Z) && length > 1E-05;
		size = (Finite(size) ? Math.Max(0.5, Math.Min(1.5, size)) : 1.0);
		if (!num)
		{
			for (int i = 0; i < b.Batches[0].Alpha.Length; i++)
			{
				b.Batches[0].Alpha[i] = 0.0;
			}
			for (int j = 1; j < 3; j++)
			{
				for (int k = 0; k < b.Batches[j].Vertices.Length; k++)
				{
					Point v = b.Batches[j].Vertices[k];
					b.Batches[j].Vertices[k] = new Point(v.X * size, v.Y * size, v.Z * size);
				}
			}
			return false;
		}
		double dx = (0.0 - sourceH.X) / length;
		double dy = (0.0 - sourceH.Y) / length;
		double px = 0.0 - dy;
		double py = dx;
		MeshData surface = b.Batches[0];
		for (int l = 0; l < surface.Vertices.Length; l++)
		{
			Point p = surface.Vertices[l];
			double f = (0.0 - p.X) / 1.08;
			double width = (p.Y + p.X * 0.065) * size;
			surface.Vertices[l] = new Point(sourceH.X * f + px * width, sourceH.Y * f + py * width, sourceH.Z * f + p.Z);
			surface.UV[l] = new Uv(1.0 - f * length / 1.08, surface.UV[l].Y);
		}
		double num2 = Math.Atan2(dy, dx) - Math.Atan2(-0.065, 1.0);
		double c = Math.Cos(num2);
		double s = Math.Sin(num2);
		for (int batch = 1; batch < 3; batch++)
		{
			MeshData m = b.Batches[batch];
			for (int n = 0; n < m.Vertices.Length; n++)
			{
				Point p2 = m.Vertices[n];
				m.Vertices[n] = new Point(size * (c * p2.X - s * p2.Y), size * (s * p2.X + c * p2.Y), p2.Z * size);
			}
		}
		return true;
	}

	public static void Sample(Buffer b, double age)
	{
		if (b.Family == "claw")
		{
			Claw(b, age);
		}
		else if (b.Family == "fang")
		{
			Fang(b, age);
		}
		else if (b.Family == "beam")
		{
			Beam(b, age);
		}
		else
		{
			Blunt(b, age);
		}
		b.LightEnvelope = Light(b.Family, age);
	}
}
