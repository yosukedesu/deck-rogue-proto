using System;

namespace DeckRogue.Game;

internal static class PhysicalSlashGeometry
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
		public readonly int Kind;

		public readonly Point[] Vertices;

		public readonly Uv[] UV;

		public readonly double[] Alpha;

		public readonly double[] Gain;

		public readonly int[] Triangles;

		internal MeshData(int count, int quads, int kind, double gain)
		{
			Kind = kind;
			Vertices = new Point[count];
			UV = new Uv[count];
			Alpha = new double[count];
			Gain = new double[count];
			Triangles = new int[quads * 6];
			for (int i = 0; i < count; i++)
			{
				Gain[i] = gain;
			}
		}
	}

	public sealed class Buffer
	{
		public readonly string RequestedTier;

		public readonly string Tier;

		public readonly MeshData[] Batches;

		public readonly bool[] ParticleActive;

		public readonly Point[] ParticleCenters;

		public double VisualAge;

		public double LightEnvelope;

		public double TargetAreaW;

		public double FloorAreaW;

		internal readonly TierData Data;

		internal readonly Point[] Centers = new Point[49];

		internal readonly Point[] ParticleStarts;

		internal readonly Point[] ParticleVelocities;

		internal readonly double[] Widths = new double[49];

		public bool Fallback => RequestedTier != Tier;

		public MeshData Primary => Batches[0];

		public MeshData Secondary => Batches[1];

		public MeshData Core => Batches[2];

		public MeshData Particles => Batches[3];

		internal Buffer(string requested, TierData data)
		{
			RequestedTier = requested;
			Tier = data.Name;
			Data = data;
			Batches = new MeshData[4]
			{
				new MeshData(147, 96, 0, (Tier == "small") ? 1.2 : ((Tier == "large") ? 1.45 : 1.35)),
				new MeshData((!(Tier == "small")) ? 147 : 0, (!(Tier == "small")) ? 96 : 0, 0, (Tier == "large") ? 1.12 : 1.0),
				new MeshData(data.CoreCount * 4, data.CoreCount, 1, data.CoreGain),
				new MeshData(data.ParticleCount * 4, data.ParticleCount, 1, data.ParticleGain)
			};
			ParticleActive = new bool[data.ParticleCount];
			ParticleCenters = new Point[data.ParticleCount];
			ParticleStarts = new Point[data.ParticleCount];
			ParticleVelocities = new Point[data.ParticleCount];
		}
	}

	internal sealed class TierData
	{
		internal readonly string Name;

		internal readonly int CoreCount;

		internal readonly int ParticleCount;

		internal readonly double P;

		internal readonly double E;

		internal readonly double Hold;

		internal readonly double Fade;

		internal readonly double Length;

		internal readonly double Width;

		internal readonly double Life;

		internal readonly double End;

		internal readonly double CoreLength;

		internal readonly double CoreGain;

		internal readonly double ParticleGain;

		internal readonly double ParticleScale;

		internal readonly double TargetW;

		internal readonly double FloorW;

		internal readonly double[,] Knots;

		internal readonly double[,] Specs;

		internal TierData(string name, int cores, double p, double e, double hold, double fade, double length, double width, double life, double end, double coreLength, double coreGain, double particleGain, double particleScale, double targetW, double floorW, double[,] knots, double[,] specs)
		{
			Name = name;
			CoreCount = cores;
			P = p;
			E = e;
			Hold = hold;
			Fade = fade;
			Length = length;
			Width = width;
			Life = life;
			End = end;
			CoreLength = coreLength;
			CoreGain = coreGain;
			ParticleGain = particleGain;
			ParticleScale = particleScale;
			TargetW = targetW;
			FloorW = floorW;
			Knots = knots;
			Specs = specs;
			ParticleCount = specs.GetLength(0);
		}
	}

	public const int TextureKind = 0;

	public const int CoreKind = 1;

	public const int RibbonRows = 49;

	private static readonly TierData Small = new TierData("small", 1, 0.045, 0.16, 0.02, 0.02, 0.72, 0.027, 0.128, 0.14, 0.08, 3.3, 1.9, 1.1, 28.0, 8.0, new double[6, 3]
	{
		{ -0.045, -0.42, 1.0 },
		{ -0.022, -0.28, 10.0 },
		{ 0.0, 0.0, 21.0 },
		{ 0.022, 0.38, 9.0 },
		{ 0.075, 0.6, 0.0 },
		{ 0.16, 0.64, 0.0 }
	}, new double[3, 4]
	{
		{ 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
		{ 0.006, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
		{ 0.012, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 }
	});

	private static readonly TierData Medium = new TierData("medium", 3, 0.066666667, 0.333333333, 0.066666667, 0.035, 1.5, 0.045, 0.23, 0.252, 0.12, 3.8, 2.2, 1.45, 48.0, 20.0, new double[6, 3]
	{
		{ -0.066666667, -0.8, 1.0 },
		{ -0.035, -0.65, 10.0 },
		{ 0.0, 0.0, 22.0 },
		{ 0.035, 0.67, 10.0 },
		{ 0.11, 1.05, 2.0 },
		{ 0.333333333, 1.25, 0.0 }
	}, new double[6, 4]
	{
		{ 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
		{ 0.0, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
		{ 0.0, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 },
		{ 0.022, 0.5538025156552036, 0.781381770579271, 0.0552109878293629 },
		{ 0.022, -0.6328030518934236, 0.9867369945327584, 0.030297687296996265 },
		{ 0.022, 0.061664644824827586, 0.8502843849475583, 0.043524457601980884 }
	});

	private static readonly TierData Large = new TierData("large", 3, 0.13, 0.4, 0.085, 0.035, 1.7, 0.073, 0.27, 0.32, 0.145, 4.0, 2.4, 1.7, 60.0, 24.0, new double[7, 3]
	{
		{ -0.13, -1.0, 0.5 },
		{ -0.07, -0.8, 4.0 },
		{ -0.025, -0.45, 14.0 },
		{ 0.0, 0.0, 24.0 },
		{ 0.035, 0.65, 10.0 },
		{ 0.125, 0.9, 0.6 },
		{ 0.4, 0.96, 0.0 }
	}, new double[9, 4]
	{
		{ 0.0, 0.029539525758989438, 0.8404689841383481, 0.04461985664440854 },
		{ 0.004, 0.44553787567599534, 1.0254498667804997, 0.053233251352732006 },
		{ 0.008, 0.05997113211962557, 1.334334127933852, 0.038769154668414345 },
		{ 0.018, 0.5538025156552036, 0.781381770579271, 0.0552109878293629 },
		{ 0.022, -0.6328030518934236, 0.9867369945327584, 0.030297687296996265 },
		{ 0.026, 0.061664644824827586, 0.8502843849475583, 0.043524457601980884 },
		{ 0.04, 0.1181540250242763, 0.9482632634123469, 0.05014185475559578 },
		{ 0.045, 0.25177522002504815, 1.0755780824187333, 0.057097001477406185 },
		{ 0.05, -0.31071945638799014, 1.125430356417509, 0.050080117253616996 }
	});

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

	private static TierData Data(string tier)
	{
		return (tier == "small") ? Small : ((tier == "large") ? Large : Medium);
	}

	public static string ResolveTier(string tier)
	{
		return Data(tier).Name;
	}

	public static double Lead(string tier)
	{
		return Data(tier).P;
	}

	public static double End(string tier)
	{
		return Data(tier).End;
	}

	public static double Horizon(string tier)
	{
		return Data(tier).E;
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

	private static double SafeAge(TierData p, double age)
	{
		return (double.IsNaN(age) || double.IsNegativeInfinity(age)) ? (0.0 - p.P - 1E-06) : (double.IsPositiveInfinity(age) ? p.E : age);
	}

	public static double VisualClock(string tier, double age, double availableAnticipation = double.NaN)
	{
		return Clock(Data(tier), age, availableAnticipation);
	}

	private static double Clock(TierData p, double age, double available)
	{
		age = SafeAge(p, age);
		if (age >= 0.0 || double.IsNaN(available) || available >= p.P)
		{
			return age;
		}
		if (available <= 0.0 || age < 0.0 - available)
		{
			return 0.0 - p.P - 1E-06;
		}
		double q = Clamp((age + available) / available);
		return 0.0 - p.P + p.P * (q + (available / p.P - 1.0) * (q * q * q - q * q));
	}

	private static double Head(TierData p, double t)
	{
		double[,] k = p.Knots;
		int last = k.GetLength(0) - 1;
		if (t <= k[0, 0])
		{
			return k[0, 1];
		}
		if (t >= k[last, 0])
		{
			return k[last, 1];
		}
		int i;
		for (i = 0; i < last - 1 && t > k[i + 1, 0]; i++)
		{
		}
		double dt = k[i + 1, 0] - k[i, 0];
		double q = (t - k[i, 0]) / dt;
		double q2 = q * q;
		double q3 = q2 * q;
		return (2.0 * q3 - 3.0 * q2 + 1.0) * k[i, 1] + (q3 - 2.0 * q2 + q) * dt * k[i, 2] + (-2.0 * q3 + 3.0 * q2) * k[i + 1, 1] + (q3 - q2) * dt * k[i + 1, 2];
	}

	private static Point Center(TierData p, double lead, double t, bool secondary, int i)
	{
		double u = (double)i / 48.0;
		double length = ((!secondary) ? p.Length : ((p.Name == "large") ? 0.95 : (p.Length * 0.55)));
		double angle = ((secondary && p.Name == "large") ? (-0.5) : 0.6);
		double s = lead - length * (1.0 - u);
		double sx = 2.0 * Math.Sin(s / 2.0);
		double sy = 2.0 * (1.0 - Math.Cos(s / 2.0));
		double x = Math.Cos(angle) * sx - Math.Sin(angle) * sy;
		double y = Math.Sin(angle) * sx + Math.Cos(angle) * sy;
		if (secondary && p.Name == "large")
		{
			x += 0.1;
			y -= 0.04;
		}
		else if (secondary)
		{
			y -= 0.11 * Math.Sin(Math.PI * u);
		}
		return new Point(x, y, 0.016 * Math.Sin(u * 5.0 + t * 6.0) + (secondary ? 0.013 : 0.0));
	}

	private static Point Emitter(TierData p, double t)
	{
		return Center(p, Head(p, t), t, secondary: false, 46);
	}

	private static double BaseAlpha(TierData p, double t)
	{
		return (0.0 - p.P <= t && t < p.E) ? (Smooth((t + p.P) / Math.Min(p.P * 0.25, 0.025)) * (1.0 - Smooth((t - 0.08) / (p.E - 0.08)))) : 0.0;
	}

	private static double CoreAlpha(TierData p, double t)
	{
		return (0.0 <= t && t < p.E) ? (1.0 - Smooth((t - p.Hold) / p.Fade)) : 0.0;
	}

	public static double CoreAlpha(string tier, double t)
	{
		TierData p = Data(tier);
		return CoreAlpha(p, SafeAge(p, t));
	}

	public static double Light(string tier, double visualAge)
	{
		TierData p = Data(tier);
		return Light(p, SafeAge(p, visualAge));
	}

	private static double Light(TierData p, double t)
	{
		double light = ((t >= 0.0) ? CoreAlpha(p, t) : ((0.0 - p.P <= t && t < p.E) ? (0.025 * Math.Pow(Smooth((t + p.P) / p.P), 3.0)) : 0.0));
		return light * (1.0 - Smooth((t - 0.1) / 0.15));
	}

	private static void Quad(int[] indices, int q, int a, int b, int c, int d)
	{
		int o = q * 6;
		indices[o] = a;
		indices[o + 1] = b;
		indices[o + 2] = c;
		indices[o + 3] = a;
		indices[o + 4] = c;
		indices[o + 5] = d;
	}

	public static Buffer Create(string tier)
	{
		TierData p = Data(tier);
		Buffer b = new Buffer(tier, p);
		for (int k = 0; k < 2; k++)
		{
			MeshData m = b.Batches[k];
			if (m.Vertices.Length == 0)
			{
				continue;
			}
			for (int i = 0; i < 49; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					m.UV[i * 3 + j] = new Uv(UVProfile[i, 0], UVProfile[i, 1] + UVProfile[i, 2] * (double)(j - 1));
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
		for (int num = 2; num < 4; num++)
		{
			for (int num2 = 0; num2 < b.Batches[num].Vertices.Length / 4; num2++)
			{
				Quad(b.Batches[num].Triangles, num2, num2 * 4, num2 * 4 + 1, num2 * 4 + 2, num2 * 4 + 3);
			}
		}
		for (int num3 = 0; num3 < 49; num3++)
		{
			b.Widths[num3] = p.Width * Math.Pow(Math.Max(0.0, Math.Sin(Math.PI * ((double)num3 / 48.0))), 0.85);
		}
		for (int num4 = 0; num4 < p.ParticleCount; num4++)
		{
			double born = p.Specs[num4, 0];
			double eps = 1E-05;
			Point start = Emitter(p, born);
			Point ep = Emitter(p, born + eps);
			Point em = Emitter(p, born - eps);
			double x = (ep.X - em.X) / 2.0 / eps * 0.3 + Math.Cos(p.Specs[num4, 1]) * p.Specs[num4, 2] * 0.45;
			double y = (ep.Y - em.Y) / 2.0 / eps * 0.3 + Math.Sin(p.Specs[num4, 1]) * p.Specs[num4, 2] + 0.45;
			double z = (ep.Z - em.Z) / 2.0 / eps * 0.3 + 0.04 * (double)(num4 % 3 - 1);
			double radius = Math.Sqrt(x * x + y * y);
			double sign = ((num4 % 2 == 1) ? 2.1 : (-2.1));
			b.ParticleStarts[num4] = start;
			b.ParticleVelocities[num4] = new Point(x + (0.0 - y) / radius * sign, y + x / radius * sign, z);
		}
		return b;
	}

	private static void Ribbon(Buffer b, double t, bool secondary, bool mirrorX)
	{
		TierData p = b.Data;
		MeshData m = b.Batches[secondary ? 1 : 0];
		if (m.Vertices.Length == 0)
		{
			return;
		}
		double lead = Head(p, t - (secondary ? 0.028 : 0.0));
		double alpha = BaseAlpha(p, t);
		alpha = ((p.Name == "small") ? (alpha * (0.82 * (1.0 - Smooth((t - 0.025) / 0.05)))) : ((!(p.Name == "large")) ? (alpha * (secondary ? (0.55 * Smooth((t - 0.018) / 0.016) * (1.0 - Smooth((t - 0.095) / 0.08))) : (0.87 * (1.0 - Smooth((t - 0.06) / 0.075))))) : (alpha * (secondary ? (0.65 * Smooth((t - 0.02) / 0.016) * (1.0 - Smooth((t - 0.1) / 0.13))) : (0.95 * (1.0 - Smooth((t - 0.06) / 0.1)))))));
		double widthScale = ((!secondary) ? 1.0 : ((p.Name == "large") ? 0.52 : 0.24));
		for (int i = 0; i < 49; i++)
		{
			b.Centers[i] = Center(p, lead, t, secondary, i);
		}
		for (int j = 0; j < 49; j++)
		{
			Point center = b.Centers[j];
			Point a = b.Centers[Math.Max(0, j - 1)];
			Point c = b.Centers[Math.Min(48, j + 1)];
			double dx = c.X - a.X;
			double dy = c.Y - a.Y;
			double r = Math.Sqrt(dx * dx + dy * dy);
			double width = b.Widths[j] * widthScale;
			for (int k = 0; k < 3; k++)
			{
				int n = j * 3 + k;
				double side = k - 1;
				double x = center.X + (0.0 - dy) / r * width * side;
				m.Vertices[n] = new Point(mirrorX ? (0.0 - x) : x, center.Y + dx / r * width * side, center.Z + 0.008 * (1.0 - side * side));
				m.Alpha[n] = alpha;
			}
		}
	}

	private static void Diamond(MeshData m, int offset, Point center, double size, double angle, double halfWidth, double shrink, double alpha, bool mirrorX)
	{
		double c = Math.Cos(angle);
		double s = Math.Sin(angle);
		for (int i = 0; i < 4; i++)
		{
			double x = i switch
			{
				2 => 0.5 * shrink, 
				0 => -0.5 * shrink, 
				_ => 0.0, 
			};
			double y = i switch
			{
				3 => halfWidth, 
				1 => 0.0 - halfWidth, 
				_ => 0.0, 
			};
			double vx = center.X + size * (c * x - s * y);
			m.Vertices[offset + i] = new Point(mirrorX ? (0.0 - vx) : vx, center.Y + size * (s * x + c * y), center.Z);
			m.Alpha[offset + i] = alpha;
		}
	}

	public static void Sample(Buffer b, double age, bool mirrorX = false, double availableAnticipation = double.NaN)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		TierData p = b.Data;
		double t = (b.VisualAge = Clock(p, age, availableAnticipation));
		b.LightEnvelope = Light(p, t);
		b.TargetAreaW = p.TargetW * b.LightEnvelope;
		b.FloorAreaW = p.FloorW * b.LightEnvelope;
		Ribbon(b, t, secondary: false, mirrorX);
		Ribbon(b, t, secondary: true, mirrorX);
		double coreAlpha = CoreAlpha(p, t);
		for (int i = 0; i < p.CoreCount; i++)
		{
			Diamond(b.Core, i * 4, new Point(0.012 * (double)i, 0.008 * (double)(i - 1), 0.030434782608695657), p.CoreLength * (1.0 - 0.24 * (double)i), i switch
			{
				1 => 0.51, 
				0 => 0.44, 
				_ => 0.36, 
			}, 0.045, 1.0, coreAlpha, mirrorX);
		}
		for (int j = 0; j < p.ParticleCount; j++)
		{
			double ageParticle = t - p.Specs[j, 0];
			int offset = j * 4;
			bool active = 0.0 - p.P <= t && t < p.E && 0.0 <= ageParticle && ageParticle < p.Life;
			b.ParticleActive[j] = active;
			if (!active)
			{
				b.ParticleCenters[j] = default(Point);
				for (int k = 0; k < 4; k++)
				{
					b.Particles.Vertices[offset + k] = default(Point);
					b.Particles.Alpha[offset + k] = 0.0;
				}
			}
			else
			{
				Point start = b.ParticleStarts[j];
				Point v = b.ParticleVelocities[j];
				double k2 = 2.4;
				double q = (1.0 - Math.Exp((0.0 - k2) * ageParticle)) / k2;
				Point center = new Point(start.X + v.X * q, start.Y + v.Y * q - 1.6 * (ageParticle / k2 - q / k2), start.Z + v.Z * q + 0.026086956521739132);
				b.ParticleCenters[j] = new Point(mirrorX ? (0.0 - center.X) : center.X, center.Y, center.Z);
				Diamond(b.Particles, offset, center, p.Specs[j, 3] * p.ParticleScale, Math.Atan2(v.Y - 1.6 * ageParticle, v.X), 0.12, 1.0 - 0.35 * ageParticle / p.Life, 1.0 - Smooth(ageParticle / p.Life), mirrorX);
			}
		}
	}
}
