using System;

namespace DeckRogue.Game;

internal static class PlayerContactGeometry
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
		public int Kind;

		public Point[] Vertices;

		public Uv[] UV;

		public double[] Alpha;

		public double[] Gain;

		public bool[] Particle;

		public int[] Role;

		public int[] Triangles;
	}

	public sealed class Buffer
	{
		public string Family;

		public MeshData[] Batches;

		internal EnemyContactGeometry.Buffer Blunt;
	}

	public const double Anticipation = 0.13;

	private static readonly double[,] Knots = new double[6, 3]
	{
		{ -0.13, -1.05, 1.5 },
		{ -0.06, -0.79, 7.0 },
		{ 0.0, 0.0, 15.0 },
		{ 0.035, 0.36, 5.0 },
		{ 0.095, 0.48, 0.0 },
		{ 0.17, 0.48, 0.0 }
	};

	private static double Clamp(double q)
	{
		return Math.Max(0.0, Math.Min(1.0, q));
	}

	private static double Smooth(double q)
	{
		q = Clamp(q);
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

	private static double Envelope(double t, double birth, double end, double fadein)
	{
		if (!(birth < t) || !(t < end))
		{
			return 0.0;
		}
		return Smooth((t - birth) / fadein) * (1.0 - Smooth((t - birth) / (end - birth)));
	}

	public static double End(string family)
	{
		if (!(family == "pierce"))
		{
			return 0.27;
		}
		return 0.178;
	}

	public static double Light(string family, double t)
	{
		return ((t >= 0.0 && t < 0.08) ? (1.0 - Smooth((t - 0.015) / 0.065)) : 0.0) * ((family == "pierce") ? 0.8 : 1.0);
	}

	public static double Clock(double age, double available)
	{
		if (age >= 0.0 || available >= 0.13 || available <= 0.0)
		{
			return age;
		}
		if (age < 0.0 - available)
		{
			return -0.130001;
		}
		double q = Clamp((age + available) / available);
		double ratio = available / 0.13;
		return -0.13 + 0.13 * (q + (ratio - 1.0) * (q * q * q - q * q));
	}

	private static MeshData Mesh(int n, int tris, int kind)
	{
		return new MeshData
		{
			Kind = kind,
			Vertices = new Point[n],
			UV = new Uv[n],
			Alpha = new double[n],
			Gain = new double[n],
			Particle = new bool[n],
			Role = new int[n],
			Triangles = new int[tris * 3]
		};
	}

	private static void Quad(MeshData m, int i, int a, int b, int c, int d)
	{
		int j = i * 6;
		m.Triangles[j] = a;
		m.Triangles[j + 1] = b;
		m.Triangles[j + 2] = c;
		m.Triangles[j + 3] = a;
		m.Triangles[j + 4] = c;
		m.Triangles[j + 5] = d;
	}

	private static void Vertex(MeshData m, int i, double x, double y, double z, double alpha, double gain, int role = 0)
	{
		m.Vertices[i] = new Point(x, y, z);
		m.Alpha[i] = alpha;
		m.Gain[i] = gain;
		m.Role[i] = role;
	}

	private static void Diamond(MeshData m, int start, double x, double y, double z, double length, double width, double angle, double alpha, int role, double gain)
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

	private static void AppendTriangles(MeshData m, int[] src, int vertexOffset, ref int index)
	{
		for (int i = 0; i < src.Length; i++)
		{
			m.Triangles[index++] = src[i] + vertexOffset;
		}
	}

	public static Buffer Create(string family)
	{
		if (family != "pierce" && family != "meteor")
		{
			throw new ArgumentException("Unknown physical delivery family");
		}
		Buffer buffer = new Buffer();
		buffer.Family = family;
		buffer.Batches = ((family == "pierce") ? new MeshData[3]
		{
			Mesh(123, 160, 1),
			Mesh(104, 98, 0),
			Mesh(32, 16, 0)
		} : new MeshData[4]
		{
			Mesh(73, 64, 2),
			Mesh(4, 2, 3),
			Mesh(246, 288, 0),
			Mesh(144, 116, 0)
		});
		Buffer b = buffer;
		if (family == "pierce")
		{
			for (int i = 0; i < 40; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					Quad(b.Batches[0], i * 2 + j, i * 3 + j, i * 3 + j + 1, (i + 1) * 3 + j + 1, (i + 1) * 3 + j);
				}
			}
			for (int k = 0; k < 2; k++)
			{
				for (int l = 0; l < 24; l++)
				{
					Quad(b.Batches[1], k * 24 + l, k * 50 + l * 2, k * 50 + l * 2 + 1, k * 50 + (l + 1) * 2 + 1, k * 50 + (l + 1) * 2);
				}
			}
			Quad(b.Batches[1], 48, 100, 101, 102, 103);
			for (int m = 0; m < 8; m++)
			{
				Quad(b.Batches[2], m, m * 4, m * 4 + 1, m * 4 + 2, m * 4 + 3);
			}
			for (int n = 0; n < 32; n++)
			{
				b.Batches[2].Particle[n] = true;
			}
		}
		else
		{
			int f = 0;
			int d = 0;
			int fi = 0;
			int di = 0;
			PlayerContactRockData.Chunk[] chunks = PlayerContactRockData.Chunks;
			foreach (PlayerContactRockData.Chunk c in chunks)
			{
				AppendTriangles(b.Batches[0], c.FrontTriangles, f, ref fi);
				AppendTriangles(b.Batches[2], c.DepthTriangles, d, ref di);
				for (int num2 = 0; num2 < c.UV.Length; num2++)
				{
					b.Batches[0].UV[f + num2] = c.UV[num2];
				}
				f += c.Front.Length;
				d += c.Depth.Length;
			}
			for (int num3 = 0; num3 < 2; num3++)
			{
				for (int num4 = 0; num4 < 24; num4++)
				{
					Quad(b.Batches[2], di / 6 + num3 * 24 + num4, d + num3 * 50 + num4 * 2, d + num3 * 50 + num4 * 2 + 1, d + num3 * 50 + (num4 + 1) * 2 + 1, d + num3 * 50 + (num4 + 1) * 2);
				}
			}
			b.Blunt = EnemyContactGeometry.Create("blunt");
			int ignored = 0;
			AppendTriangles(b.Batches[1], b.Blunt.Batches[0].Triangles, 0, ref ignored);
			ignored = 0;
			AppendTriangles(b.Batches[3], b.Blunt.Batches[1].Triangles, 0, ref ignored);
			AppendTriangles(b.Batches[3], b.Blunt.Batches[2].Triangles, 104, ref ignored);
			for (int num5 = 104; num5 < 144; num5++)
			{
				b.Batches[3].Particle[num5] = true;
			}
		}
		return b;
	}

	private static double Head(double t)
	{
		if (t <= Knots[0, 0])
		{
			return Knots[0, 1];
		}
		if (t >= Knots[5, 0])
		{
			return Knots[5, 1];
		}
		int k;
		for (k = 0; k < 4 && t > Knots[k + 1, 0]; k++)
		{
		}
		double dt = Knots[k + 1, 0] - Knots[k, 0];
		double q = (t - Knots[k, 0]) / dt;
		double q2 = q * q;
		double q3 = q2 * q;
		return (2.0 * q3 - 3.0 * q2 + 1.0) * Knots[k, 1] + (q3 - 2.0 * q2 + q) * dt * Knots[k, 2] + (-2.0 * q3 + 3.0 * q2) * Knots[k + 1, 1] + (q3 - q2) * dt * Knots[k + 1, 2];
	}

	private static void Pierce(Buffer b, double t)
	{
		double head = Head(t);
		double growth = 0.24 + 0.76 * Smooth((t + 0.13) / 0.13);
		double length = 0.95 * growth;
		double a = ((t >= -0.13 && t < 0.125) ? (Smooth((t + 0.13) / 0.02) * (1.0 - Smooth((t - 0.02) / 0.105))) : 0.0);
		MeshData body = b.Batches[0];
		for (int i = 0; i < 41; i++)
		{
			double u = (double)i / 40.0;
			double x = head - length * (1.0 - u);
			double y = -0.035 * x;
			double w = 0.09 * Math.Pow(1.0 - u, 0.65) + 5E-05;
			for (int j = 0; j < 3; j++)
			{
				double side = j - 1;
				int n = i * 3 + j;
				Vertex(body, n, x, y + side * w, 0.016 + 0.007 * (1.0 - side * side), a, 1.55);
				body.UV[n] = new Uv((69.0 + 768.0 * u) / 1774.0, 1.0 - (523.0 + -155.0 * (side + 1.0) / 2.0) / 887.0);
			}
		}
		MeshData solid = b.Batches[1];
		for (int k = 0; k < 2; k++)
		{
			for (int l = 0; l < 25; l++)
			{
				double u2 = (double)l / 24.0;
				double start = head - 0.82 * growth;
				double end = head - 0.045;
				double x2 = start + (end - start) * u2;
				double y2 = -0.035 * x2 + (double)(k * 2 - 1) * (0.029 * (1.0 - u2) + 0.008);
				double w2 = 0.0038 * Math.Sin(Math.PI * u2);
				for (int m = 0; m < 2; m++)
				{
					Vertex(solid, k * 50 + l * 2 + m, x2, y2 + ((m == 0) ? (0.0 - w2) : w2), 0.011, a * 0.48, 1.1, 2);
				}
			}
		}
		Diamond(solid, 100, 0.005, 0.0, 0.043, 0.1, 0.011, -0.035, Fade(t, 0.014, 0.052), 3, 2.8);
		for (int num = 0; num < 8; num++)
		{
			double born = 0.004 * (double)(num % 3);
			double age = Math.Max(0.0, t - born);
			double q = (1.0 - Math.Exp(-5.0 * age)) / 5.0;
			double angle = -0.035 + ((double)num - 3.5) * 0.105;
			double speed = 1.5 + 0.17 * (double)num;
			Diamond(b.Batches[2], num * 4, Math.Cos(angle) * speed * q, Math.Sin(angle) * speed * q - 0.5 * age * age, 0.05, 0.03 + 0.005 * (double)(num % 3), 0.0055, angle, Envelope(t, born, 0.17 + born, 0.003), 2, 1.65);
		}
	}

	private static Point Rotate(Point p, double zangle, double yangle = 0.0)
	{
		double c = Math.Cos(yangle);
		double s = Math.Sin(yangle);
		double x = c * p.X + s * p.Z;
		double z = (0.0 - s) * p.X + c * p.Z;
		c = Math.Cos(zangle);
		s = Math.Sin(zangle);
		return new Point(c * x - s * p.Y, s * x + c * p.Y, z);
	}

	private static Point RockPosition(Point p, Point center, double spin, double yaw, double zangle, double q, Point pre, Point travel)
	{
		Point v = Rotate(new Point(p.X - center.X, p.Y - center.Y, p.Z - center.Z), spin, yaw);
		v = Rotate(new Point(v.X + center.X, v.Y + center.Y, v.Z + center.Z), zangle, 0.08 * q);
		return new Point(v.X + pre.X + travel.X, v.Y + pre.Y + travel.Y, v.Z + pre.Z + travel.Z);
	}

	private static void CopyBlunt(EnemyContactGeometry.MeshData src, MeshData dst, int offset)
	{
		for (int i = 0; i < src.Vertices.Length; i++)
		{
			EnemyContactGeometry.Point p = src.Vertices[i];
			Vertex(dst, offset + i, p.X * 1.32, p.Y * 1.32, p.Z + 0.04, src.Alpha[i], src.Gain[i], src.Role[i] + 1);
			dst.UV[offset + i] = new Uv(src.UV[i].X, src.UV[i].Y);
		}
	}

	private static void Meteor(Buffer b, double t)
	{
		double q = Clamp((t + 0.13) / 0.13);
		double progress = 0.2 * q + 0.8 * q * q;
		double at = Math.Max(0.0, t);
		double zangle = -0.2 + 0.8 * q;
		double a = ((t >= -0.13 && t < 0.0) ? Smooth((t + 0.13) / 0.025) : Fade(t, 0.035, 0.27));
		Point pre = new Point(-0.38 * (1.0 - progress), 0.85 * (1.0 - progress), 0.022);
		int f = 0;
		int d = 0;
		PlayerContactRockData.Chunk[] chunks = PlayerContactRockData.Chunks;
		foreach (PlayerContactRockData.Chunk chunk in chunks)
		{
			int k = chunk.Index;
			Point center = chunk.Center;
			double angle = ((k == 0) ? 0.3 : Math.Atan2(center.Y, center.X));
			double speed = 1.4 + 0.15 * (double)(k % 3);
			double yv = 0.6 + Math.Abs(Math.Sin(angle)) * 1.4;
			double spin = at * (double)((k % 2 == 1) ? 5 : (-6));
			double yaw = at * (double)((k % 3 != 0) ? 4 : (-5));
			Point travel = new Point(Math.Cos(angle) * speed * at, yv * at - 4.5 * at * at, 0.035 * at * (double)(k % 3 - 1));
			for (int j = 0; j < chunk.Front.Length; j++)
			{
				Point p = RockPosition(chunk.Front[j], center, spin, yaw, zangle, q, pre, travel);
				Vertex(b.Batches[0], f++, p.X, p.Y, p.Z, a, 0.6);
			}
			for (int l = 0; l < chunk.Depth.Length; l++)
			{
				Point p2 = RockPosition(chunk.Depth[l], center, spin, yaw, zangle, q, pre, travel);
				Vertex(b.Batches[2], d++, p2.X, p2.Y, p2.Z, a, 0.75);
			}
		}
		double length = Math.Sqrt(0.8668999999999999);
		double nx = 0.85 / length;
		double ny = 0.38 / length;
		for (int m = 0; m < 2; m++)
		{
			double delay = ((m == 0) ? 0.075 : 0.055);
			double alpha = ((t >= -0.13 && t < delay) ? (Smooth((t + 0.13) / 0.025) * ((m == 0) ? 0.32 : 0.2) * (1.0 - Smooth(Math.Max(t, 0.0) / delay))) : 0.0);
			for (int n = 0; n < 25; n++)
			{
				double u = (double)n / 24.0;
				double qt = Clamp((t - delay * (1.0 - u) + 0.13) / 0.13);
				double prog = 0.2 * qt + 0.8 * qt * qt;
				double x = -0.38 * (1.0 - prog);
				double y = 0.85 * (1.0 - prog);
				double off = ((m == 0) ? 0.018 : (-0.025));
				double w = ((m == 0) ? 0.016 : 0.007) * Math.Pow(Math.Sin(Math.PI * u), 0.7);
				for (int num = 0; num < 2; num++)
				{
					double side = ((num != 0) ? 1 : (-1));
					Vertex(b.Batches[2], d + m * 50 + n * 2 + num, x + nx * (off + side * w), y + ny * (off + side * w), -0.02, alpha, 0.8, 2);
				}
			}
		}
		EnemyContactGeometry.Sample(b.Blunt, t);
		CopyBlunt(b.Blunt.Batches[0], b.Batches[1], 0);
		CopyBlunt(b.Blunt.Batches[1], b.Batches[3], 0);
		CopyBlunt(b.Blunt.Batches[2], b.Batches[3], 104);
	}

	public static void Sample(Buffer b, double t)
	{
		if (b.Family == "pierce")
		{
			Pierce(b, t);
			return;
		}
		if (b.Family == "meteor")
		{
			Meteor(b, t);
			return;
		}
		throw new ArgumentException("Unknown physical delivery");
	}

	public static bool FitIncoming(Buffer b, double sourceX, double sourceY)
	{
		if (b.Family == "meteor")
		{
			return true;
		}
		double length = Math.Sqrt(sourceX * sourceX + sourceY * sourceY);
		bool valid = !double.IsNaN(length) && !double.IsInfinity(length) && length > 1E-05;
		double c = (valid ? ((0.0 - sourceX) / length) : 1.0);
		double s = (valid ? ((0.0 - sourceY) / length) : 0.0);
		MeshData[] batches = b.Batches;
		foreach (MeshData m in batches)
		{
			for (int j = 0; j < m.Vertices.Length; j++)
			{
				Point p = m.Vertices[j];
				m.Vertices[j] = new Point(c * p.X - s * p.Y, s * p.X + c * p.Y, p.Z);
			}
		}
		return valid;
	}
}
