using System;

namespace DeckRogue.Game;

internal static class IceCrystalGeometry
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
		public readonly int Kind;

		public readonly Point[] Vertices;

		public readonly Uv[] UV;

		public readonly double[] Alpha;

		public readonly double[] Gain;

		public readonly double[] Front;

		public readonly int[] Role;

		public readonly int[] Triangles;

		internal MeshData(int vertices, int triangles, int kind, double gain)
		{
			Kind = kind;
			Vertices = new Point[vertices];
			UV = new Uv[vertices];
			Alpha = new double[vertices];
			Gain = new double[vertices];
			Front = new double[vertices];
			Role = new int[vertices];
			Triangles = new int[triangles * 3];
			for (int i = 0; i < vertices; i++)
			{
				Gain[i] = gain;
				Role[i] = 2;
				Front[i] = 2.0;
			}
		}
	}

	public sealed class Buffer
	{
		public readonly MeshData[] Batches;

		public readonly bool[] ChipActive;

		public readonly Point[] ChipCenters;

		public double VisualAge;

		public double LightEnvelope;

		public double RevealFront;

		public double TargetAreaW;

		public double FloorAreaW;

		public MeshData Crystals => Batches[0];

		public MeshData Main => Batches[0];

		public MeshData Core => Batches[1];

		public MeshData Chips => Batches[2];

		internal Buffer()
		{
			Batches = new MeshData[3]
			{
				new MeshData(44, 26, 0, 1.75),
				new MeshData(9, 8, 1, 3.0),
				new MeshData(56, 28, 2, 1.55)
			};
			ChipActive = new bool[14];
			ChipCenters = new Point[14];
		}
	}

	public const int CrystalKind = 0;

	public const int CoreKind = 1;

	public const int SolidKind = 2;

	public const int ShadowRole = 0;

	public const int PrimaryRole = 1;

	public const int EdgeRole = 2;

	public const int CoreRole = 3;

	public const int VertexCount = 109;

	public const int TriangleCount = 62;

	public const int BatchCount = 3;

	public const int CrystalCount = 9;

	public const int ChipCount = 14;

	public const double NativeLead = 0.13;

	public const double VisibleEnd = 0.305;

	public const double RigidHoldEnd = 0.055;

	public const double PivotX = 0.5411499436302142;

	public const double PivotY = 0.5264937993235626;

	private static readonly int[] CellStarts = new int[10] { 0, 8, 13, 18, 22, 26, 31, 35, 40, 44 };

	private static readonly double[] Rotation = new double[9] { 0.1, 0.21, -0.17, 0.27, -0.25, 0.19, -0.21, 0.24, -0.16 };

	private static readonly Uv[] Cells = new Uv[44]
	{
		new Uv(0.4405822836871397, 0.4963086080275954),
		new Uv(0.5163468214160081, 0.43997903105578556),
		new Uv(0.6335475840541117, 0.42131454260392154),
		new Uv(0.6484986630565808, 0.5024885407846669),
		new Uv(0.6492035959125354, 0.5786896634169102),
		new Uv(0.5983706141377628, 0.6262474702668746),
		new Uv(0.5207017551078363, 0.6141400861026001),
		new Uv(0.44858381306299416, 0.5760570061590002),
		new Uv(0.4405822836871397, 0.4963086080275954),
		new Uv(0.0, 0.36406867600334225),
		new Uv(0.0, 0.0),
		new Uv(0.39020819910371524, 0.0),
		new Uv(0.5163468214160081, 0.43997903105578556),
		new Uv(0.5163468214160081, 0.43997903105578556),
		new Uv(0.39020819910371524, 0.0),
		new Uv(1.0, 0.0),
		new Uv(1.0, 0.00416976969112226),
		new Uv(0.6335475840541117, 0.42131454260392154),
		new Uv(0.6335475840541117, 0.42131454260392154),
		new Uv(1.0, 0.004169769691121872),
		new Uv(1.0, 0.42388601515433544),
		new Uv(0.6484986630565808, 0.5024885407846669),
		new Uv(0.6484986630565808, 0.5024885407846669),
		new Uv(1.0, 0.42388601515433544),
		new Uv(1.0, 0.7481436434114404),
		new Uv(0.6492035959125354, 0.5786896634169102),
		new Uv(0.6492035959125354, 0.5786896634169102),
		new Uv(1.0, 0.7481436434114401),
		new Uv(1.0, 1.0),
		new Uv(0.8127624270202721, 1.0),
		new Uv(0.5983706141377628, 0.6262474702668746),
		new Uv(0.5983706141377628, 0.6262474702668746),
		new Uv(0.812762427020272, 1.0),
		new Uv(0.43067927323710853, 1.0),
		new Uv(0.5207017551078363, 0.6141400861026001),
		new Uv(0.5207017551078363, 0.6141400861026001),
		new Uv(0.4306792732371086, 1.0),
		new Uv(0.0, 1.0),
		new Uv(0.0, 0.8162447743376113),
		new Uv(0.44858381306299416, 0.5760570061590002),
		new Uv(0.44858381306299416, 0.5760570061590002),
		new Uv(0.0, 0.8162447743376113),
		new Uv(0.0, 0.36406867600334214),
		new Uv(0.4405822836871397, 0.4963086080275954)
	};

	private static readonly Uv[] Centers = MakeCenters();

	private static Uv[] MakeCenters()
	{
		Uv[] result = new Uv[9];
		for (int k = 0; k < 9; k++)
		{
			double x = 0.0;
			double y = 0.0;
			int count = CellStarts[k + 1] - CellStarts[k];
			for (int i = CellStarts[k]; i < CellStarts[k + 1]; i++)
			{
				x += Cells[i].X;
				y += Cells[i].Y;
			}
			result[k] = new Uv(x / (double)count, y / (double)count);
		}
		return result;
	}

	public static double Lead()
	{
		return 0.13;
	}

	public static double End()
	{
		return 0.305;
	}

	public static double End(Buffer b)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		return 0.305;
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

	private static double Fade(double t, double hold, double end)
	{
		if (!(0.0 <= t) || !(t < end))
		{
			return 0.0;
		}
		return 1.0 - Smooth((t - hold) / (end - hold));
	}

	private static double SafeAge(double age)
	{
		if (double.IsNaN(age) || age < -1000000.0)
		{
			return -0.13;
		}
		if (!(age > 1000000.0))
		{
			return age;
		}
		return 0.305;
	}

	public static double Clock(double age, double availableAnticipation = double.NaN)
	{
		age = SafeAge(age);
		if (age >= 0.0)
		{
			return age;
		}
		double a = (double.IsNaN(availableAnticipation) ? 0.13 : Math.Min(0.13, Math.Max(0.0, availableAnticipation)));
		if (a <= 0.0)
		{
			return double.NaN;
		}
		if (age < 0.0 - a)
		{
			return -0.13;
		}
		double q = age / a;
		return age - (0.13 - a) * (q * q);
	}

	public static double VisualClock(double age, double availableAnticipation = double.NaN)
	{
		return Clock(age, availableAnticipation);
	}

	public static double Light(double visualAge)
	{
		double t = SafeAge(visualAge);
		if (t >= 0.0)
		{
			return Fade(t, 0.025, 0.13);
		}
		double q = Smooth((t + 0.13) / 0.13);
		return 0.025 * q * q;
	}

	public static double Front(double visualAge)
	{
		double t = SafeAge(visualAge);
		if (t <= 0.0)
		{
			return 0.55 * Math.Pow(Clamp((t + 0.13) / 0.13), 2.3636363636363633);
		}
		return 0.55 + 10.0 * t;
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
		int triangle = 0;
		for (int k = 0; k < 9; k++)
		{
			int start = CellStarts[k];
			int count = CellStarts[k + 1] - start;
			for (int i = 0; i < count; i++)
			{
				b.Crystals.UV[start + i] = new Uv(Cells[start + i].X * 0.5, Cells[start + i].Y);
			}
			for (int j = 1; j < count - 1; j++)
			{
				b.Crystals.Triangles[triangle++] = start;
				b.Crystals.Triangles[triangle++] = start + j;
				b.Crystals.Triangles[triangle++] = start + j + 1;
			}
		}
		b.Core.Vertices[0] = new Point(0.0, 0.0, 0.07);
		b.Core.UV[0] = new Uv(0.5, 0.5);
		for (int l = 0; l < 8; l++)
		{
			double c = Math.Cos((double)l * Math.PI / 4.0);
			double s = Math.Sin((double)l * Math.PI / 4.0);
			b.Core.Vertices[l + 1] = new Point(0.034 * c, 0.027200000000000002 * s, 0.067);
			b.Core.UV[l + 1] = new Uv(0.5 + 0.5 * c, 0.5 + 0.5 * s);
			b.Core.Triangles[l * 3] = 0;
			b.Core.Triangles[l * 3 + 1] = l + 1;
			b.Core.Triangles[l * 3 + 2] = 1 + (l + 1) % 8;
		}
		for (int m = 0; m < 14; m++)
		{
			Quad(b.Chips.Triangles, m, m * 4, m * 4 + 1, m * 4 + 2, m * 4 + 3);
			for (int n = 0; n < 4; n++)
			{
				b.Chips.Role[m * 4 + n] = ((m % 3 == 0) ? 3 : 2);
			}
		}
		return b;
	}

	private static void Crystal(Buffer b, double t, int k, double front, double alpha)
	{
		double a = Math.Max(0.0, t - 0.055);
		double separation = a * a / (a + 0.045);
		double cx = Centers[k].X;
		double cy = Centers[k].Y;
		double dx = cx - 0.5411499436302142;
		double dy = cy - 0.5264937993235626;
		double n = Math.Max(0.07, Math.Sqrt(dx * dx + dy * dy));
		dx /= n;
		dy /= n;
		double speed = ((k == 0) ? 0.0 : (0.65 + 0.11 * (double)(k % 3)));
		double tx = dx * speed * separation;
		double ty = dy * speed * separation - ((k == 0) ? 0.2 : 0.8) * a * a;
		double num = Rotation[k] * separation;
		double ca = Math.Cos(num);
		double sa = Math.Sin(num);
		for (int i = CellStarts[k]; i < CellStarts[k + 1]; i++)
		{
			double x = Cells[i].X - cx;
			double y = Cells[i].Y - cy;
			b.Crystals.Vertices[i] = new Point(cx - 0.5411499436302142 + ca * x - sa * y + tx, cy - 0.5264937993235626 + sa * x + ca * y + ty, 0.026 + 0.002 * (double)k);
			b.Crystals.Alpha[i] = alpha;
			b.Crystals.Front[i] = front;
		}
	}

	private static void Chip(Buffer b, double t, int i)
	{
		double born = 0.055 + 0.006 * (double)(i % 7);
		double age = Math.Max(0.0, t - born);
		double life = 0.17;
		double num = (double)i * 2.399963;
		double speed = 0.8 + 0.17 * (double)(i % 4);
		double q = (1.0 - Math.Exp(-4.0 * age)) / 4.0;
		double x = Math.Cos(num) * speed * q;
		double y = Math.Sin(num) * speed * q * 0.75 - 1.5 * age * age;
		double num2 = num + age * (double)((i % 2 != 0) ? 8 : (-6));
		double ca = Math.Cos(num2);
		double sa = Math.Sin(num2);
		double length = 0.028 + 0.006 * (double)(i % 3);
		double width = 0.014 + 0.003 * (double)(i % 2);
		double alpha = Smooth((t - born) / 0.008) * Fade(t - born, 0.025, life);
		b.ChipCenters[i] = new Point(x, y, 0.055);
		b.ChipActive[i] = alpha > 0.0;
		for (int j = 0; j < 4; j++)
		{
			double a = j switch
			{
				2 => length * 0.5, 
				0 => (0.0 - length) * 0.5, 
				_ => 0.0, 
			};
			double w = j switch
			{
				3 => width * 0.5, 
				1 => (0.0 - width) * 0.5, 
				_ => 0.0, 
			};
			int n = i * 4 + j;
			b.Chips.Vertices[n] = new Point(x + ca * a - sa * w, y + sa * a + ca * w, 0.055);
			b.Chips.Alpha[n] = alpha;
		}
	}

	public static void Sample(Buffer b, double age, double availableAnticipation = double.NaN)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		double mapped = Clock(age, availableAnticipation);
		double t = (b.VisualAge = (double.IsNaN(mapped) ? (-0.13) : mapped));
		b.LightEnvelope = Light(t);
		b.RevealFront = Front(t);
		b.TargetAreaW = 42.0 * b.LightEnvelope;
		b.FloorAreaW = 16.0 * b.LightEnvelope;
		double alpha = ((-0.13 <= t && t < 0.305) ? (Smooth((t + 0.13) / 0.014) * (1.0 - Smooth((t - 0.155) / 0.15))) : 0.0);
		for (int k = 0; k < 9; k++)
		{
			Crystal(b, t, k, b.RevealFront, alpha);
		}
		double coreAlpha = Fade(t, 0.018, 0.079);
		for (int i = 0; i < 9; i++)
		{
			b.Core.Alpha[i] = coreAlpha;
		}
		for (int j = 0; j < 14; j++)
		{
			Chip(b, t, j);
		}
	}
}
