using System;

namespace DeckRogue.Game;

internal static class SystemSelfLossGeometry
{
	private static readonly double[,] Facets = new double[4, 4]
	{
		{ -0.19, 0.095, 0.14, -0.65 },
		{ -0.1, -0.035, 0.095, -1.1 },
		{ 0.09, 0.035, 0.105, -1.9 },
		{ 0.19, 0.1, 0.15, -2.48 }
	};

	private static readonly double[] Births = new double[3] { 0.0, 0.025, 0.055 };

	private static readonly double[] Lives = new double[3] { 0.19, 0.165, 0.145 };

	private static readonly double[] Cx = new double[3] { -0.15, 0.028, 0.16 };

	private static readonly double[] Cy = new double[3] { 0.075, 0.14, 0.025 };

	private static readonly double[] Lengths = new double[3] { 0.25, 0.18, 0.215 };

	private static readonly double[] Widths = new double[3] { 0.016, 0.015, 0.018 };

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
		if (!(family == "selfloss"))
		{
			return family == "affliction";
		}
		return true;
	}

	public static double End(string family)
	{
		if (!(family == "selfloss"))
		{
			return 0.2;
		}
		return 0.182;
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
			Phase = new double[n],
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
				1 => -0.12, 
				0 => -0.5, 
				_ => 0.12, 
			} * length;
			double b = i switch
			{
				3 => 0.5, 
				1 => -0.5, 
				_ => 0.0, 
			} * width;
			Vertex(m, start + i, x + c * a - s * b, y + s * a + c * b, z, alpha, gain, role);
			SystemOutcomeGeometry.Uv[] uV = m.UV;
			int num = start + i;
			uV[num] = new SystemOutcomeGeometry.Uv(i switch
			{
				2 => 1.0, 
				1 => 0.38, 
				0 => 0.0, 
				_ => 0.62, 
			}, i switch
			{
				3 => 1.0, 
				1 => 0.0, 
				_ => 0.5, 
			});
		}
	}

	public static SystemOutcomeGeometry.Buffer Create(string family)
	{
		if (!IsFamily(family))
		{
			throw new ArgumentException("Unknown local loss family", "family");
		}
		bool self = family == "selfloss";
		SystemOutcomeGeometry.Buffer buffer = new SystemOutcomeGeometry.Buffer();
		buffer.Family = family;
		buffer.Batches = (self ? new SystemOutcomeGeometry.MeshData[1] { Mesh(40, 20, 7) } : new SystemOutcomeGeometry.MeshData[2]
		{
			Mesh(114, 108, 8),
			Mesh(32, 16, 7)
		});
		SystemOutcomeGeometry.Buffer b = buffer;
		if (self)
		{
			for (int i = 0; i < 10; i++)
			{
				Quad(b.Batches[0], i, i * 4, i * 4 + 1, i * 4 + 2, i * 4 + 3);
			}
		}
		else
		{
			for (int k = 0; k < 3; k++)
			{
				for (int j = 0; j < 18; j++)
				{
					Quad(b.Batches[0], k * 18 + j, k * 38 + j * 2, k * 38 + j * 2 + 1, k * 38 + (j + 1) * 2 + 1, k * 38 + (j + 1) * 2);
				}
			}
			for (int l = 0; l < 8; l++)
			{
				Quad(b.Batches[1], l, l * 4, l * 4 + 1, l * 4 + 2, l * 4 + 3);
			}
		}
		return b;
	}

	private static void SelfLoss(SystemOutcomeGeometry.Buffer b, double t)
	{
		SystemOutcomeGeometry.MeshData m = b.Batches[0];
		double q = Smooth(Math.Max(0.0, t) / 0.085);
		double a = Fade(t, 0.03, 0.18);
		for (int i = 0; i < 4; i++)
		{
			Piece(m, i * 4, Facets[i, 0] * (1.0 - 0.58 * q), Facets[i, 1] - 0.11 * q, 0.03 + (double)i * 0.003, Facets[i, 2] * (1.0 - 0.32 * q), (i % 2 == 0) ? 0.025 : 0.021, Facets[i, 3] + 0.12 * q, a, 1, 1.3);
		}
		for (int j = 0; j < 6; j++)
		{
			double born = 0.022 + 0.006 * (double)(j % 3);
			double at = Math.Max(0.0, t - born);
			double x = ((double)j - 2.5) * 0.031;
			double vx = (0.0 - x) * 0.6;
			double y = -0.035 - 0.019 * (double)(j % 2);
			double life = 0.13 + 0.009 * (double)(j % 3);
			Piece(m, (j + 4) * 4, x + vx * at, y - 0.38 * at - 0.45 * at * at, 0.045, 0.015 + 0.004 * (double)(j % 2), 0.006, -Math.PI / 2.0 + 0.25 * Math.Sin(j), Smooth((t - born) / 0.008) * Fade(t - born, 0.01, life), (j % 3 == 0) ? 1 : 0, 1.12);
		}
	}

	private static void Affliction(SystemOutcomeGeometry.Buffer b, double t)
	{
		SystemOutcomeGeometry.MeshData thread = b.Batches[0];
		for (int k = 0; k < 3; k++)
		{
			double local = t - Births[k];
			double age = Math.Max(0.0, local);
			double a = ((k == 0) ? (0.24 + 0.76 * Smooth(local / 0.012)) : Smooth(local / 0.012)) * Fade(local, 0.055, Lives[k]);
			for (int i = 0; i < 19; i++)
			{
				double u = (double)i / 18.0;
				double x = Cx[k] + 0.018 * Math.Sin(5.0 * u + (double)k * 1.8) + 0.026 * Smooth(age / 0.11) * (double)((k == 2) ? 1 : (-1));
				double y = Cy[k] - Lengths[k] * u - age * 0.42;
				double w = Widths[k] * Math.Pow(Math.Sin(Math.PI * u), 0.45);
				for (int j = 0; j < 2; j++)
				{
					int n = k * 38 + i * 2 + j;
					Vertex(thread, n, x + ((j == 0) ? (0.0 - w) : w), y, 0.031 + (double)k * 0.003, a, 1.3, 1);
					thread.UV[n] = new SystemOutcomeGeometry.Uv(u, j);
					thread.Phase[n] = (double)k * 0.7;
				}
			}
		}
		for (int l = 0; l < 8; l++)
		{
			double born = 0.012 + 0.007 * (double)l;
			double age2 = Math.Max(0.0, t - born);
			double life = 0.125 + 0.006 * (double)(l % 3);
			double cy = 0.025 - 0.02 * (double)(l % 4);
			double vx = 0.09 * Math.Sin((double)l * 1.7);
			Piece(b.Batches[1], l * 4, Cx[l % 3] + vx * age2, cy - 0.55 * age2 - 0.6 * age2 * age2, 0.049, 0.028 + 0.005 * (double)(l % 3), 0.009, -1.3 + 0.5 * Math.Sin(l), Smooth((t - born) / 0.008) * Fade(t - born, 0.022, life), 1, 1.22);
		}
	}

	public static void Sample(SystemOutcomeGeometry.Buffer b, double t)
	{
		if (b.Family == "selfloss")
		{
			SelfLoss(b, t);
			return;
		}
		if (b.Family == "affliction")
		{
			Affliction(b, t);
			return;
		}
		throw new ArgumentException("Unknown local loss family");
	}
}
