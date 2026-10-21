using System;

namespace DeckRogue.Game;

internal static class SystemImpactGeometry
{
	private static double Clamp(double x)
	{
		return Math.Max(0.0, Math.Min(1.0, x));
	}

	private static double Smooth(double x)
	{
		double q = Clamp(x);
		return q * q * (3.0 - 2.0 * q);
	}

	private static double Fade(double t, double h, double end)
	{
		if (!(t >= 0.0) || !(t < end))
		{
			return 0.0;
		}
		return 1.0 - Smooth((t - h) / (end - h));
	}

	public static bool IsFamily(string family)
	{
		if (!(family == "reflect"))
		{
			return family == "burn";
		}
		return true;
	}

	public static double End(string family)
	{
		if (!(family == "reflect"))
		{
			return 0.238;
		}
		return 0.14;
	}

	public static double Light(string family, double t)
	{
		if (!(family == "reflect"))
		{
			return 0.14 * Fade(t, 0.016, 0.07);
		}
		return 0.12 * Fade(t, 0.008, 0.055);
	}

	private static SystemOutcomeGeometry.MeshData Mesh(int n, int tris, int kind)
	{
		return new SystemOutcomeGeometry.MeshData
		{
			Kind = kind,
			Vertices = new SystemOutcomeGeometry.Point[n],
			UV = new SystemOutcomeGeometry.Uv[n],
			Alpha = new double[n],
			Gain = new double[n],
			Role = new int[n],
			Triangles = new int[tris * 3]
		};
	}

	private static void Quad(SystemOutcomeGeometry.MeshData m, int i, int a, int b, int c, int d)
	{
		int j = i * 6;
		m.Triangles[j] = a;
		m.Triangles[j + 1] = b;
		m.Triangles[j + 2] = c;
		m.Triangles[j + 3] = a;
		m.Triangles[j + 4] = c;
		m.Triangles[j + 5] = d;
	}

	private static void Vertex(SystemOutcomeGeometry.MeshData m, int i, double x, double y, double z, double alpha, double gain, int role = 0)
	{
		m.Vertices[i] = new SystemOutcomeGeometry.Point(x, y, z);
		m.Alpha[i] = alpha;
		m.Gain[i] = gain;
		m.Role[i] = role;
	}

	private static void Piece(SystemOutcomeGeometry.MeshData m, int start, double x, double y, double z, double length, double width, double angle, double alpha, int role, double gain)
	{
		double c = Math.Cos(angle);
		double s = Math.Sin(angle);
		for (int i = 0; i < 4; i++)
		{
			double a = i switch
			{
				2 => 0.5, 
				0 => -0.5, 
				_ => 0.0, 
			} * length;
			double b = i switch
			{
				3 => 0.5, 
				1 => -0.5, 
				_ => 0.0, 
			} * width;
			Vertex(m, start + i, x + c * a - s * b, y + s * a + c * b, z, alpha, gain, role);
		}
	}

	public static SystemOutcomeGeometry.Buffer Create(string family)
	{
		if (!IsFamily(family))
		{
			throw new ArgumentException("Unknown positive-loss system family", "family");
		}
		bool reflect = family == "reflect";
		SystemOutcomeGeometry.Buffer buffer = new SystemOutcomeGeometry.Buffer();
		buffer.Family = family;
		buffer.Batches = (reflect ? new SystemOutcomeGeometry.MeshData[3]
		{
			Mesh(100, 96, 4),
			Mesh(15, 9, 5),
			Mesh(24, 12, 0)
		} : new SystemOutcomeGeometry.MeshData[2]
		{
			Mesh(261, 336, 6),
			Mesh(28, 14, 0)
		});
		SystemOutcomeGeometry.Buffer b = buffer;
		if (reflect)
		{
			for (int k = 0; k < 2; k++)
			{
				for (int i = 0; i < 24; i++)
				{
					Quad(b.Batches[0], k * 24 + i, k * 50 + i * 2, k * 50 + i * 2 + 1, k * 50 + (i + 1) * 2 + 1, k * 50 + (i + 1) * 2);
				}
			}
			for (int j = 0; j < 3; j++)
			{
				for (int l = 0; l < 3; l++)
				{
					int j2 = (j * 3 + l) * 3;
					b.Batches[1].Triangles[j2] = j * 5;
					b.Batches[1].Triangles[j2 + 1] = j * 5 + l + 1;
					b.Batches[1].Triangles[j2 + 2] = j * 5 + l + 2;
				}
			}
			for (int m = 0; m < 6; m++)
			{
				Quad(b.Batches[2], m, m * 4, m * 4 + 1, m * 4 + 2, m * 4 + 3);
			}
		}
		else
		{
			for (int n = 0; n < 3; n++)
			{
				for (int num = 0; num < 28; num++)
				{
					for (int num2 = 0; num2 < 2; num2++)
					{
						Quad(b.Batches[0], n * 56 + num * 2 + num2, n * 87 + num * 3 + num2, n * 87 + num * 3 + num2 + 1, n * 87 + (num + 1) * 3 + num2 + 1, n * 87 + (num + 1) * 3 + num2);
					}
				}
			}
			for (int num3 = 0; num3 < 7; num3++)
			{
				Quad(b.Batches[1], num3, num3 * 4, num3 * 4 + 1, num3 * 4 + 2, num3 * 4 + 3);
			}
		}
		return b;
	}

	private static void Reflect(SystemOutcomeGeometry.Buffer b, double t)
	{
		double age = Math.Max(0.0, t);
		double a = Fade(t, 0.006, 0.09);
		double tail = -0.43 + 0.37 * Smooth(age / 0.08);
		SystemOutcomeGeometry.MeshData trace = b.Batches[0];
		for (int k = 0; k < 2; k++)
		{
			for (int i = 0; i < 25; i++)
			{
				double u = (double)i / 24.0;
				double x = tail + (-0.025 - tail) * u;
				double y = (double)(k * 2 - 1) * (0.023 + 0.028 * Math.Sin(u * Math.PI)) * (1.0 - u);
				double w = ((k == 0) ? 0.0055 : 0.0035) * Math.Pow(Math.Sin(Math.PI * u), 0.6);
				for (int j = 0; j < 2; j++)
				{
					int n = k * 50 + i * 2 + j;
					Vertex(trace, n, x, y + ((j == 0) ? (0.0 - w) : w), 0.022 + (double)k * 0.004, a, 1.15);
					trace.UV[n] = new SystemOutcomeGeometry.Uv(u, j);
				}
			}
		}
		for (int l = 0; l < 3; l++)
		{
			double angle = l switch
			{
				1 => 0.02, 
				0 => -0.28, 
				_ => 0.3, 
			};
			double L = l switch
			{
				1 => 0.16, 
				0 => 0.075, 
				_ => 0.058, 
			};
			double x2 = l switch
			{
				1 => -0.008, 
				0 => -0.08, 
				_ => 0.04, 
			} + age * 0.24;
			double y2 = l switch
			{
				1 => 0.004, 
				0 => -0.055, 
				_ => 0.07, 
			};
			double w2 = l switch
			{
				1 => 0.018, 
				0 => 0.009, 
				_ => 0.008, 
			};
			double c = Math.Cos(angle);
			double s = Math.Sin(angle);
			for (int m = 0; m < 5; m++)
			{
				double px = m switch
				{
					3 => -0.22 * L, 
					2 => 0.035, 
					1 => -0.1 * L, 
					0 => 0.0 - L, 
					_ => -0.72 * L, 
				};
				double num;
				switch (m)
				{
				default:
					num = w2 * 0.25;
					break;
				case 3:
					num = w2 * 0.65;
					break;
				case 1:
					num = 0.0 - w2;
					break;
				case 0:
				case 2:
					num = 0.0;
					break;
				}
				double py = num;
				int n2 = l * 5 + m;
				Vertex(b.Batches[1], n2, x2 + c * px - s * py, y2 + s * px + c * py, 0.033 + (double)l * 0.002, Fade(t, 0.01, 0.078), 0.98);
				SystemOutcomeGeometry.Uv[] uV = b.Batches[1].UV;
				int num2 = n2;
				uV[num2] = m switch
				{
					3 => new SystemOutcomeGeometry.Uv(0.65, 1.0), 
					2 => new SystemOutcomeGeometry.Uv(1.0, 0.5), 
					1 => new SystemOutcomeGeometry.Uv(0.7, 0.0), 
					0 => new SystemOutcomeGeometry.Uv(0.0, 0.5), 
					_ => new SystemOutcomeGeometry.Uv(0.2, 0.7), 
				};
			}
		}
		for (int num3 = 0; num3 < 6; num3++)
		{
			double angle2 = ((double)num3 - 2.5) * 0.39;
			double speed = 0.55 + 0.12 * (double)(num3 % 3);
			double q = (1.0 - Math.Exp(-7.0 * age)) / 7.0;
			Piece(b.Batches[2], num3 * 4, 0.018 + Math.Cos(angle2) * speed * q, 0.005 + Math.Sin(angle2) * speed * q - 0.3 * age * age, 0.05, 0.018 + 0.004 * (double)(num3 % 3), 0.0045, angle2 + age * 3.0, Smooth(t / 0.004) * Fade(t, 0.005, 0.14), 1, 1.3);
		}
	}

	private static void Burn(SystemOutcomeGeometry.Buffer b, double t)
	{
		SystemOutcomeGeometry.MeshData body = b.Batches[0];
		for (int k = 0; k < 3; k++)
		{
			double num = t;
			double local = num - k switch
			{
				1 => 0.0, 
				0 => 0.035, 
				_ => 0.055, 
			};
			double age = Math.Max(local, 0.0);
			double life = ((k == 1) ? 0.2 : 0.145);
			double alpha = ((k == 1) ? (0.24 + 0.76 * Smooth(local / 0.014)) : Smooth(local / 0.014)) * Fade(local, 0.025, life);
			double L = k switch
			{
				1 => 0.28, 
				0 => 0.15, 
				_ => 0.185, 
			} * (0.65 + 0.35 * Smooth(age / 0.035));
			double cx = k switch
			{
				1 => 0.003, 
				0 => -0.07, 
				_ => 0.073, 
			};
			double cy = k switch
			{
				1 => -0.15, 
				0 => -0.1, 
				_ => -0.115, 
			};
			double phase = (double)k * 1.7;
			for (int i = 0; i < 29; i++)
			{
				double u = (double)i / 28.0;
				double num2 = cy + L * u;
				num = age;
				double y = num2 + num * k switch
				{
					1 => 0.36, 
					0 => 0.28, 
					_ => 0.42, 
				};
				double num3 = cx;
				num = 6.0 * u;
				double x = num3 + 0.049 * Math.Sin(num - (double)(k switch
				{
					1 => 15, 
					0 => 19, 
					_ => 23, 
				}) * age + phase) * u * u + 0.014 * Math.Sin(11.0 * age + phase) * u;
				double profile = Math.Pow(Math.Sin(Math.PI * u), 0.35) * Math.Pow(1.0 - u, 1.15);
				double width = k switch
				{
					1 => 0.078, 
					0 => 0.055, 
					_ => 0.048, 
				} * profile * (1.0 - 0.35 * Smooth(age / life));
				for (int j = 0; j < 3; j++)
				{
					double side = j - 1;
					double asym = ((side < 0.0) ? (0.85 + 0.15 * Math.Sin(4.0 * u + phase)) : (1.06 + 0.1 * Math.Sin(5.0 * u + phase)));
					int n = k * 87 + i * 3 + j;
					Vertex(body, n, x + side * width * asym, y, 0.024 + (double)k * 0.004 + 0.005 * (1.0 - side * side), alpha * 0.9, 1.5);
					body.UV[n] = new SystemOutcomeGeometry.Uv(u, (side + 1.0) / 2.0);
				}
			}
		}
		for (int l = 0; l < 7; l++)
		{
			double born = 0.006 * (double)(l % 3);
			double age2 = Math.Max(0.0, t - born);
			double life2 = 0.176 + 0.01 * (double)l;
			double phi = (double)l * 2.39996;
			double cx2 = 0.07 * Math.Sin(phi);
			double cy2 = -0.06 + 0.055 * Math.Cos(phi);
			double vx = 0.16 * Math.Sin(phi);
			double vy = 0.65 + 0.045 * (double)l;
			double q = (1.0 - Math.Exp(-2.5 * age2)) / 2.5;
			Piece(b.Batches[1], l * 4, cx2 + vx * q, cy2 + vy * q, 0.04, 0.012 + 0.003 * (double)(l % 2), 0.005, Math.PI / 2.0 + 0.25 * Math.Sin(phi), Smooth((t - born) / 0.006) * Fade(t - born, 0.016, life2), (l % 3 != 0) ? 1 : 2, 1.5);
		}
	}

	public static void Sample(SystemOutcomeGeometry.Buffer b, double t)
	{
		if (b.Family == "reflect")
		{
			Reflect(b, t);
		}
		else
		{
			Burn(b, t);
		}
	}

	public static bool FitReflection(SystemOutcomeGeometry.Buffer b, double sourceX, double sourceY)
	{
		if (b.Family != "reflect")
		{
			return true;
		}
		double length = Math.Sqrt(sourceX * sourceX + sourceY * sourceY);
		if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 1E-05))
		{
			Array.Clear(b.Batches[0].Alpha, 0, b.Batches[0].Alpha.Length);
			return false;
		}
		double c = (0.0 - sourceX) / length;
		double s = (0.0 - sourceY) / length;
		SystemOutcomeGeometry.MeshData[] batches = b.Batches;
		foreach (SystemOutcomeGeometry.MeshData m in batches)
		{
			for (int j = 0; j < m.Vertices.Length; j++)
			{
				SystemOutcomeGeometry.Point p = m.Vertices[j];
				m.Vertices[j] = new SystemOutcomeGeometry.Point(c * p.X - s * p.Y, s * p.X + c * p.Y, p.Z);
			}
		}
		return true;
	}
}
