using System;

namespace DeckRogue.Game;

internal static class SystemOutcomeGeometry
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
		public int Kind;

		public Point[] Vertices;

		public Uv[] UV;

		public double[] Alpha;

		public double[] Gain;

		public double[] Phase;

		public int[] Role;

		public int[] Triangles;
	}

	public sealed class Buffer
	{
		public string Family;

		public MeshData[] Batches;
	}

	private static readonly double[,] Ctrl = new double[6, 2]
	{
		{ -0.17, 0.15 },
		{ 0.0, 0.205 },
		{ 0.17, 0.15 },
		{ 0.145, -0.055 },
		{ 0.0, -0.245 },
		{ -0.145, -0.055 }
	};

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
		return (t >= 0.0 && t < end) ? (1.0 - Smooth((t - hold) / (end - hold))) : 0.0;
	}

	public static double End(string family)
	{
		return SystemSelfLossGeometry.IsFamily(family) ? SystemSelfLossGeometry.End(family) : (SystemImpactGeometry.IsFamily(family) ? SystemImpactGeometry.End(family) : ((family == "guard") ? 0.18 : 0.212));
	}

	public static double Light(string family, double t)
	{
		return SystemSelfLossGeometry.IsFamily(family) ? 0.0 : (SystemImpactGeometry.IsFamily(family) ? SystemImpactGeometry.Light(family, t) : ((family == "guard") ? (Fade(t, 0.01, 0.065) * 0.33) : 0.0));
	}

	private static MeshData Mesh(int vertices, int triangles, int kind)
	{
		MeshData meshData = new MeshData();
		meshData.Kind = kind;
		meshData.Vertices = new Point[vertices];
		meshData.UV = new Uv[vertices];
		meshData.Alpha = new double[vertices];
		meshData.Gain = new double[vertices];
		meshData.Role = new int[vertices];
		meshData.Triangles = new int[triangles * 3];
		return meshData;
	}

	private static void Quad(MeshData m, int index, int a, int b, int c, int d)
	{
		int k = index * 6;
		m.Triangles[k] = a;
		m.Triangles[k + 1] = b;
		m.Triangles[k + 2] = c;
		m.Triangles[k + 3] = a;
		m.Triangles[k + 4] = c;
		m.Triangles[k + 5] = d;
	}

	public static Buffer Create(string family)
	{
		if (SystemSelfLossGeometry.IsFamily(family))
		{
			return SystemSelfLossGeometry.Create(family);
		}
		if (SystemImpactGeometry.IsFamily(family))
		{
			return SystemImpactGeometry.Create(family);
		}
		if (family != "guard" && family != "nullified")
		{
			throw new ArgumentException("Unknown resource response", "family");
		}
		Buffer buffer = new Buffer();
		buffer.Family = family;
		buffer.Batches = ((!(family == "guard")) ? new MeshData[2]
		{
			Mesh(150, 144, 3),
			Mesh(64, 32, 0)
		} : new MeshData[3]
		{
			Mesh(195, 256, 1),
			Mesh(65, 64, 2),
			Mesh(24, 12, 0)
		});
		Buffer b = buffer;
		if (family == "guard")
		{
			for (int i = 0; i < 64; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					Quad(b.Batches[0], i * 2 + j, i * 3 + j, i * 3 + j + 1, (i + 1) * 3 + j + 1, (i + 1) * 3 + j);
				}
			}
			for (int k = 0; k < 64; k++)
			{
				b.Batches[1].Triangles[k * 3] = 0;
				b.Batches[1].Triangles[k * 3 + 1] = k + 1;
				b.Batches[1].Triangles[k * 3 + 2] = (k + 1) % 64 + 1;
			}
			for (int l = 0; l < 6; l++)
			{
				Quad(b.Batches[2], l, l * 4, l * 4 + 1, l * 4 + 2, l * 4 + 3);
			}
		}
		else
		{
			for (int m = 0; m < 3; m++)
			{
				for (int n = 0; n < 24; n++)
				{
					Quad(b.Batches[0], m * 24 + n, m * 50 + n * 2, m * 50 + n * 2 + 1, m * 50 + (n + 1) * 2 + 1, m * 50 + (n + 1) * 2);
				}
			}
			for (int num = 0; num < 16; num++)
			{
				Quad(b.Batches[1], num, num * 4, num * 4 + 1, num * 4 + 2, num * 4 + 3);
			}
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

	private static void Diamond(MeshData m, int offset, double x, double y, double z, double length, double width, double angle, double alpha, int role, double gain)
	{
		double c = Math.Cos(angle);
		double s = Math.Sin(angle);
		for (int i = 0; i < 4; i++)
		{
			double px = i switch
			{
				2 => 0.5, 
				0 => -0.5, 
				_ => 0.0, 
			} * length;
			double py = i switch
			{
				3 => 0.5, 
				1 => -0.5, 
				_ => 0.0, 
			} * width;
			Vertex(m, offset + i, x + c * px - s * py, y + s * px + c * py, z, alpha, gain, role);
		}
	}

	private static void Perimeter(double u, out double x, out double y)
	{
		double v = (u - Math.Floor(u)) * 6.0;
		int k = (int)v;
		double q = v - (double)k;
		double q2 = q * q;
		double q3 = q2 * q;
		x = Curve(Ctrl[(k + 5) % 6, 0], Ctrl[k % 6, 0], Ctrl[(k + 1) % 6, 0], Ctrl[(k + 2) % 6, 0], q, q2, q3);
		y = Curve(Ctrl[(k + 5) % 6, 1], Ctrl[k % 6, 1], Ctrl[(k + 1) % 6, 1], Ctrl[(k + 2) % 6, 1], q, q2, q3);
	}

	private static double Curve(double a, double b, double c, double d, double q, double q2, double q3)
	{
		return 0.5 * (2.0 * b + (0.0 - a + c) * q + (2.0 * a - 5.0 * b + 4.0 * c - d) * q2 + (0.0 - a + 3.0 * b - 3.0 * c + d) * q3);
	}

	private static void Guard(Buffer b, double t)
	{
		double a = Fade(t, 0.025, 0.18);
		double at = Math.Max(t, 0.0);
		double deflect = 0.025 * Math.Exp((0.0 - at) / 0.042) * Math.Sin(at * Math.PI / 0.045);
		MeshData edge = b.Batches[0];
		MeshData face = b.Batches[1];
		Vertex(face, 0, deflect - 0.025, -0.01, 0.04, a * 0.15, 0.72);
		face.UV[0] = new Uv(0.5, 0.5);
		for (int i = 0; i < 65; i++)
		{
			double u = (double)i / 64.0;
			Perimeter(u, out var x, out var y);
			double dist = Math.Min(Math.Abs(u - 0.87), 1.0 - Math.Abs(u - 0.87));
			x += deflect + 0.014 * Math.Exp(0.0 - Math.Pow(dist / 0.075, 2.0)) * Math.Exp((0.0 - at) / 0.033) - 0.025;
			Perimeter(u - 0.001, out var x2, out var y2);
			Perimeter(u + 0.001, out var x3, out var y3);
			double dx = x3 - x2;
			double dy = y3 - y2;
			double len = Math.Sqrt(dx * dx + dy * dy);
			double w = 0.0078 * (0.82 + 0.18 * Math.Sin(u * Math.PI * 6.0 + 0.2));
			for (int j = 0; j < 3; j++)
			{
				double side = j - 1;
				Vertex(edge, i * 3 + j, x - dy / len * w * side, y + dx / len * w * side, 0.02 + 0.005 * (1.0 - side * side), a * 0.92, 1.45);
				edge.UV[i * 3 + j] = new Uv(u, (side + 1.0) / 2.0);
			}
			if (i < 64)
			{
				Vertex(face, i + 1, x, y, 0.02, a * 0.15, 0.72);
				face.UV[i + 1] = new Uv(0.5 + x / 0.4, 0.5 + y / 0.52);
			}
		}
		Perimeter(0.87, out var px, out var py);
		px -= 0.025;
		MeshData solid = b.Batches[2];
		Diamond(solid, 0, px + deflect, py, 0.046, 0.07, 0.009, 0.32, Fade(t, 0.008, 0.044), 2, 2.1);
		for (int k = 0; k < 5; k++)
		{
			double angle = (double)(k - 2) * 0.3 + Math.PI * 13.0 / 25.0;
			double speed = 0.65 + 0.14 * (double)k;
			double q = (1.0 - Math.Exp(-7.0 * at)) / 7.0;
			double alpha = Smooth(t / 0.003) * Fade(t, 0.0, 0.115);
			Diamond(solid, (k + 1) * 4, px + Math.Cos(angle) * speed * q, py + Math.Sin(angle) * speed * q - 0.8 * at * at, 0.044, 0.022 + 0.003 * (double)(k % 2), 0.004, angle, alpha, 1, 1.3);
		}
	}

	private static void Strand(int i, double u, out double x, out double y, out double z)
	{
		x = -0.21 + 0.38 * u;
		y = (double)(i - 1) * 0.062 + 0.032 * Math.Sin(Math.PI * u + (double)i * 0.7);
		z = 0.023 + 0.003 * (double)i;
	}

	private static void Nullify(Buffer b, double t)
	{
		double a = Fade(t, 0.03, 0.185);
		double age = Math.Max(t, 0.0);
		MeshData traces = b.Batches[0];
		for (int k = 0; k < 3; k++)
		{
			for (int i = 0; i < 25; i++)
			{
				double u = (double)i / 24.0;
				Strand(k, u, out var x, out var y, out var z);
				x += 0.007 * Math.Sin(age * 24.0 + (double)k) * Smooth(age / 0.03);
				y += (double)(k - 1) * 0.16 * age;
				double w = ((k == 1) ? 0.0095 : 0.0068) * Math.Pow(Math.Sin(Math.PI * u), 0.55) * (1.0 - 0.2 * Smooth(age / 0.16));
				for (int j = 0; j < 2; j++)
				{
					int n = k * 50 + i * 2 + j;
					Vertex(traces, n, x - 0.34, y + ((j == 0) ? (0.0 - w) : w), z, a * 0.96, 1.28);
					traces.UV[n] = new Uv(u, j);
				}
			}
		}
		for (int l = 0; l < 16; l++)
		{
			double born = 0.006 * (double)(l % 4);
			double at = Math.Max(0.0, t - born);
			double life = 0.178 + 0.008 * (double)(l % 3);
			double alpha = Smooth((t - born) / 0.006) * Fade(t - born, 0.065, life);
			Strand(l % 3, Math.Min(0.95, 0.12 + 0.053 * (double)l), out var x2, out var y2, out var z2);
			double angle = ((l % 2 == 1) ? (Math.PI / 2.0) : (-Math.PI / 2.0)) + 0.24 * Math.Sin((double)l * 1.9);
			double speed = 0.7 + 0.08 * (double)(l % 4);
			double q = (1.0 - Math.Exp(-5.0 * at)) / 5.0;
			Diamond(b.Batches[1], l * 4, x2 + Math.Cos(angle) * speed * q - 0.34, y2 + Math.Sin(angle) * speed * q, z2 + 0.013, (0.023 + 0.006 * (double)(l % 4)) * (1.0 - 0.35 * Clamp(at / life)), 0.0065, angle + at * (double)((l % 2 == 1) ? 6 : (-5)), alpha, (l % 4 != 0) ? 1 : 2, 1.18);
		}
	}

	public static void Sample(Buffer b, double t)
	{
		if (SystemSelfLossGeometry.IsFamily(b.Family))
		{
			SystemSelfLossGeometry.Sample(b, t);
		}
		else if (SystemImpactGeometry.IsFamily(b.Family))
		{
			SystemImpactGeometry.Sample(b, t);
		}
		else if (b.Family == "guard")
		{
			Guard(b, t);
		}
		else
		{
			Nullify(b, t);
		}
	}

	public static bool FitIncoming(Buffer b, double sourceX, double sourceY)
	{
		if (SystemSelfLossGeometry.IsFamily(b.Family))
		{
			return true;
		}
		if (SystemImpactGeometry.IsFamily(b.Family))
		{
			return SystemImpactGeometry.FitReflection(b, sourceX, sourceY);
		}
		double len = Math.Sqrt(sourceX * sourceX + sourceY * sourceY);
		bool valid = !double.IsNaN(len) && !double.IsInfinity(len) && len > 1E-05;
		double c = (valid ? ((0.0 - sourceX) / len) : 1.0);
		double s = (valid ? ((0.0 - sourceY) / len) : 0.0);
		double mirror = ((!valid || !(sourceX > 0.0)) ? 1 : (-1));
		MeshData[] batches = b.Batches;
		foreach (MeshData m in batches)
		{
			for (int j = 0; j < m.Vertices.Length; j++)
			{
				Point p = m.Vertices[j];
				m.Vertices[j] = ((b.Family == "guard") ? new Point(p.X * mirror, p.Y, p.Z) : new Point(c * p.X - s * p.Y, s * p.X + c * p.Y, p.Z));
			}
		}
		return valid;
	}
}
