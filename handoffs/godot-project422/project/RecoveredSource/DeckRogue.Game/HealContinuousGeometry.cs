using System;

namespace DeckRogue.Game;

internal static class HealContinuousGeometry
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

		public readonly double[] Coverage;

		public readonly int[] Role;

		public readonly int[] Triangles;

		internal MeshData(int vertices, int triangles, int kind, double gain, int role)
		{
			Kind = kind;
			Vertices = new Point[vertices];
			UV = new Uv[vertices];
			Alpha = new double[vertices];
			Gain = new double[vertices];
			Coverage = new double[vertices];
			Role = new int[vertices];
			Triangles = new int[triangles * 3];
			for (int i = 0; i < vertices; i++)
			{
				Gain[i] = gain;
				Role[i] = role;
				Coverage[i] = 1.0;
			}
		}
	}

	public sealed class Buffer
	{
		public readonly MeshData[] Batches;

		public readonly Point[] MoteCenters;

		public readonly bool[] MoteActive;

		public readonly bool[] TailActive;

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		public MeshData Aura => Batches[0];

		public MeshData Tails => Batches[1];

		public MeshData Motes => Batches[2];

		internal Buffer()
		{
			Batches = new MeshData[3]
			{
				new MeshData(9, 8, 2, 1.0, 1),
				new MeshData(120, 128, 1, 1.15, 2),
				new MeshData(64, 32, 0, 1.65, 1)
			};
			MoteCenters = new Point[16];
			MoteActive = new bool[16];
			TailActive = new bool[8];
		}
	}

	public const int MoteKind = 0;

	public const int TailKind = 1;

	public const int AuraKind = 2;

	public const int ShadowRole = 0;

	public const int PrimaryRole = 1;

	public const int EdgeRole = 2;

	public const int CoreRole = 3;

	public const int MoteCount = 16;

	public const int TailCount = 8;

	public const int VertexCount = 193;

	public const int TriangleCount = 168;

	public const int BatchCount = 3;

	public const double NativeLead = 0.0;

	public const double VisibleEnd = 0.541;

	public const double AuraEnd = 0.205;

	public const double LightEnd = 0.24;

	public const double FeetAnchorUpH = 107.0 / 230.0;

	private static readonly double[] Births = new double[16]
	{
		0.0, 0.006, 0.013, 0.021, 0.03, 0.037, 0.046, 0.054, 0.063, 0.071,
		0.08, 0.088, 0.097, 0.107, 0.116, 0.126
	};

	public static double Lead()
	{
		return 0.0;
	}

	public static double End()
	{
		return 0.541;
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
			return -1.0;
		}
		if (age > 1000000.0)
		{
			return 0.541;
		}
		return age;
	}

	public static double Clock(double age)
	{
		return SafeAge(age);
	}

	public static double Light(double age)
	{
		double t = SafeAge(age);
		return Smooth(t / 0.03) * Fade(t, 0.055, 0.24);
	}

	private static void Quad(int[] tri, int q, int a, int b, int c, int d)
	{
		int n = q * 6;
		tri[n] = a;
		tri[n + 1] = b;
		tri[n + 2] = c;
		tri[n + 3] = a;
		tri[n + 4] = c;
		tri[n + 5] = d;
	}

	public static Buffer Create()
	{
		Buffer b = new Buffer();
		b.Aura.UV[0] = new Uv(0.5, 0.5);
		for (int k = 0; k < 8; k++)
		{
			b.Aura.UV[k + 1] = new Uv(0.5 + 0.5 * Math.Cos((double)k * Math.PI / 4.0), 0.5 + 0.5 * Math.Sin((double)k * Math.PI / 4.0));
			b.Aura.Triangles[k * 3] = 0;
			b.Aura.Triangles[k * 3 + 1] = 1 + k;
			b.Aura.Triangles[k * 3 + 2] = 1 + (k + 1) % 8;
		}
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				for (int l = 0; l < 3; l++)
				{
					b.Tails.UV[i * 15 + j * 3 + l] = new Uv((double)l / 2.0, (double)j / 4.0);
				}
			}
			for (int m = 0; m < 4; m++)
			{
				for (int n = 0; n < 2; n++)
				{
					int a = i * 15 + m * 3 + n;
					Quad(b.Tails.Triangles, i * 8 + m * 2 + n, a, a + 1, a + 4, a + 3);
				}
			}
		}
		for (int num = 0; num < 16; num++)
		{
			Quad(b.Motes.Triangles, num, num * 4, num * 4 + 1, num * 4 + 2, num * 4 + 3);
			for (int num2 = 0; num2 < 4; num2++)
			{
				int x = ((num2 != 0 && num2 != 3) ? 1 : (-1));
				int y = ((num2 < 2) ? 1 : (-1));
				b.Motes.UV[num * 4 + num2] = new Uv((double)(520 + x * 52) / 1774.0, 1.0 - (double)(154 + y * 52) / 887.0);
			}
		}
		return b;
	}

	private static Point Center(double t, int i, out double q, out double age, out double life)
	{
		age = Math.Max(0.0, t - Births[i]);
		life = 0.31 + 0.035 * (double)(i % 4);
		q = Clamp(age / life);
		double rise = (0.58 + 0.045 * (double)(i % 5)) * (0.65 * q + 0.35 * q * q);
		return new Point(0.24 * Math.Sin((double)i * 2.399963) * (1.0 - 0.14 * q) + 0.035 * Math.Sin((double)i * 1.33 + q * 2.5) * Math.Sin(Math.PI * q), -0.24 + 0.1 * (double)(i % 3) + rise, 0.04 + 0.002 * (double)(i % 4));
	}

	private static void Mote(Buffer b, double t, int i)
	{
		Point p = Center(t, i, out var q, out var _, out var life);
		double alpha = Smooth((t - Births[i]) / 0.026) * Fade(t - Births[i], life * 0.4, life) * 0.9;
		double r = (0.042 + 0.006 * (double)(i % 3)) * (0.7 + 0.3 * Math.Sin(Math.PI * q));
		double num = 0.08 * Math.Sin((double)i + q * 4.0);
		double ca = Math.Cos(num);
		double sa = Math.Sin(num);
		b.MoteCenters[i] = p;
		b.MoteActive[i] = alpha > 0.0;
		for (int j = 0; j < 4; j++)
		{
			double x = ((j == 0 || j == 3) ? (0.0 - r) : r);
			double y = ((j < 2) ? ((0.0 - r) * 1.15) : (r * 1.15));
			int n = i * 4 + j;
			b.Motes.Vertices[n] = new Point(p.X + ca * x - sa * y, p.Y + sa * x + ca * y, p.Z);
			b.Motes.Alpha[n] = alpha;
		}
	}

	private static void Tail(Buffer b, double t, int tail)
	{
		int i = tail * 2;
		Point p = Center(t, i, out var q, out var age, out var life);
		double alpha = Smooth((t - Births[i]) / 0.035) * Fade(t - Births[i], life * 0.3, life) * 0.28;
		double length = 0.085 * Smooth(age / 0.055) * (0.65 + 0.35 * q);
		b.TailActive[tail] = alpha > 0.0;
		for (int k = 0; k < 5; k++)
		{
			double u = (double)k / 4.0;
			double x = p.X - 0.015 * Math.Sin((double)i + q * 3.0) * (1.0 - u);
			double y = p.Y - (1.0 - u) * length;
			double w = 0.01 * Math.Pow(Math.Sin(Math.PI * u), 0.6);
			for (int j = 0; j < 3; j++)
			{
				int n = tail * 15 + k * 3 + j;
				b.Tails.Vertices[n] = new Point(x + (double)(j - 1) * w, y, p.Z - 0.006);
				b.Tails.Alpha[n] = alpha;
			}
		}
	}

	private static void Aura(Buffer b, double t)
	{
		double alpha = 0.15 * Smooth(t / 0.035) * Fade(t, 0.055, 0.205);
		double r = 0.11 + 0.025 * Smooth(t / 0.09);
		b.Aura.Vertices[0] = new Point(0.0, -0.14, 0.025);
		b.Aura.Alpha[0] = alpha;
		for (int k = 0; k < 8; k++)
		{
			b.Aura.Vertices[k + 1] = new Point(r * Math.Cos((double)k * Math.PI / 4.0), -0.14 + r * 1.65 * Math.Sin((double)k * Math.PI / 4.0), 0.024);
			b.Aura.Alpha[k + 1] = alpha;
		}
	}

    //363 owned HolyMaul recovery: two open rising side-wraps, never a filled torso disk.
    //Geometry stays in existing current-foot-relative camera plane; no receipt or clock changes.
    private static void OwnedMaulWrap(Buffer b,double t,double scale)
    {
        for(int i=0;i<b.Aura.Vertices.Length;i++)b.Aura.Alpha[i]=0.0;
        for(int tail=0;tail<TailCount;tail++)
        {
            int side=tail%2==0?-1:1;int segment=tail/2;
            double born=Births[tail*2];double age=t-born;
            double q=Clamp(age/.34);double envelope=Smooth(age/.035)*Fade(age,.11,.34)*.36;
            b.TailActive[tail]=envelope>0;
            for(int k=0;k<5;k++)
            {
                double u=k/4.0;
                double y=-.365+segment*.145+q*.10+(u-.5)*.145;
                double height=Clamp((y+.40)/.74);
                double x=side*(.355-.13*height+.024*Math.Sin(Math.PI*u+segment*.70));
                double width=.009*Math.Sin(Math.PI*u)*(1-.35*q);
                for(int j=0;j<3;j++)
                {
                    int n=tail*15+k*3+j;
                    b.Tails.Vertices[n]=new Point((x+(j-1)*width)*scale,y*scale,.035);
                    b.Tails.Alpha[n]=envelope*(side<0?1.0:.82);
                    b.Tails.Gain[n]=.72;
                }
            }
        }
        for(int i=0;i<MoteCount;i++)
        {
            double age=t-Births[i];double life=.27+.017*(i%4);double q=Clamp(age/life);
            int side=i%2==0?-1:1;
            double y=-.31+.055*(i%4)+.55*q;
            double x=side*(.345-.12*Clamp((y+.35)/.72)+.025*Math.Sin(Math.PI*q+i*.6));
            double alpha=Smooth(age/.026)*Fade(age,life*.35,life)*.65;
            double radius=(.015+.003*(i%3))*(.8+.2*Math.Sin(Math.PI*q));
            var center=new Point(x*scale,y*scale,.042+.001*(i%3));
            b.MoteCenters[i]=center;b.MoteActive[i]=alpha>0;
            for(int j=0;j<4;j++)
            {
                double dx=(j==0||j==3?-radius:radius)*scale;
                double dy=(j<2?-radius:radius)*1.2*scale;
                int n=i*4+j;b.Motes.Vertices[n]=new Point(center.X+dx,center.Y+dy,center.Z);
                b.Motes.Alpha[n]=alpha;b.Motes.Gain[n]=.72;
            }
        }
    }

	public static void Sample(Buffer b, double age, bool ownedHolyMaul = false, double ownedWrapScale = 1.0)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		double t = (b.VisualAge = SafeAge(age));
        //Reset payload gains even if a diagnostic reuses a buffer across optional variants.
        Array.Fill(b.Tails.Gain,1.15);Array.Fill(b.Motes.Gain,1.65);
		b.LightEnvelope = Light(t);
		b.TargetAreaW = 28.0 * b.LightEnvelope;
		b.FloorAreaW = 10.0 * b.LightEnvelope;
		Aura(b, t);
		for (int i = 0; i < 8; i++)
		{
			Tail(b, t, i);
		}
		for (int j = 0; j < 16; j++)
		{
			Mote(b, t, j);
		}
        if(ownedHolyMaul)OwnedMaulWrap(b,t,ownedWrapScale);
	}
}
