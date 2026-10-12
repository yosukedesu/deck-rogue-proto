using System;

namespace DeckRogue.Game;

internal static class LightningR9Geometry
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

	public struct Quad
	{
		public double X;

		public double Y;

		public double Z;

		public double W;

		public Quad(double x, double y, double z, double w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}
	}

	public sealed class MeshData
	{
		public readonly Point[] Vertices;

		public readonly Quad[] UV0;

		public readonly Quad[] UV1;

		public readonly Quad[] UV2;

		public readonly double[] GroundWeight;

		public readonly int[] Triangles;

		public readonly bool Floor;

		public readonly int VertexCount;

		public readonly int TriangleCount;

		public bool Visible;

		internal MeshData(int segments, int fields, bool floor)
		{
			Floor = floor;
			VertexCount = segments * 8 + fields * 4;
			TriangleCount = segments * 6 + fields * 2;
			Vertices = new Point[VertexCount];
			UV0 = new Quad[VertexCount];
			UV1 = new Quad[VertexCount];
			UV2 = new Quad[VertexCount];
			GroundWeight = new double[VertexCount];
			Triangles = new int[TriangleCount * 3];
			int at = 0;
			for (int s = 0; s < segments; s++)
			{
				for (int row = 0; row < 3; row++)
				{
					int v = s * 8 + row * 2;
					Triangles[at++] = v;
					Triangles[at++] = v + 1;
					Triangles[at++] = v + 3;
					Triangles[at++] = v;
					Triangles[at++] = v + 3;
					Triangles[at++] = v + 2;
				}
			}
			for (int f = 0; f < fields; f++)
			{
				int v2 = segments * 8 + f * 4;
				Triangles[at++] = v2;
				Triangles[at++] = v2 + 1;
				Triangles[at++] = v2 + 2;
				Triangles[at++] = v2;
				Triangles[at++] = v2 + 2;
				Triangles[at++] = v2 + 3;
			}
		}
	}

	internal sealed class PathSpec
	{
		internal readonly string Name;

		internal readonly int Kind;

		internal readonly int Seed;

		internal readonly bool Floor;

		internal readonly double Birth;

		internal readonly double Tau;

		internal readonly double FragmentStart;

		internal readonly double FragmentSpan;

		internal readonly double FragmentMax;

		internal readonly double Depth;

		internal readonly double[] XY;

		internal readonly double[] Widths;

		internal readonly double[] Energy;

		internal readonly double[] Random;

		internal readonly double[] Lengths;

		internal readonly double[] Cumulative;

		internal readonly double[] ClipLengths;

		internal readonly double TotalLength;

		internal readonly double ClipTotal;

		internal readonly double Radius;

		internal readonly double MaxEnergy;

		internal readonly int Count;

		internal PathSpec(string name, int kind, bool floor, int seed, double birth, double tau, double fragmentStart, double fragmentSpan, double fragmentMax, double depth, double[] xy, double[] widths, double[] energy, double[] random, double[] clipLengths)
		{
			Name = name;
			Kind = kind;
			Floor = floor;
			Seed = seed;
			Birth = birth;
			Tau = tau;
			FragmentStart = fragmentStart;
			FragmentSpan = fragmentSpan;
			FragmentMax = fragmentMax;
			Depth = depth;
			XY = xy;
			Widths = widths;
			Energy = energy;
			Random = random;
			Count = widths.Length;
			Lengths = new double[Count];
			Cumulative = new double[Count + 1];
			double maxWidth = 0.0;
			for (int i = 0; i < Count; i++)
			{
				double dx = XY[i * 2 + 2] - XY[i * 2];
				double dy = XY[i * 2 + 3] - XY[i * 2 + 1];
				Lengths[i] = Math.Sqrt(dx * dx + dy * dy);
				Cumulative[i + 1] = Cumulative[i] + Lengths[i];
				maxWidth = Math.Max(maxWidth, widths[i]);
				MaxEnergy = Math.Max(MaxEnergy, energy[i]);
			}
			TotalLength = Cumulative[Count];
			ClipLengths = clipLengths ?? Lengths;
			for (int j = 0; j < Count; j++)
			{
				ClipTotal += ClipLengths[j];
			}
			double sigma = ((kind == 0) ? 14.0 : ((kind == 1) ? 2.4 : ((kind == 2) ? 1.7 : ((kind == 4) ? 3.2 : ((double)((kind == 7) ? 2 : 0))))));
			if (kind == 0)
			{
				maxWidth *= 2.55;
			}
			Radius = Math.Max(2.0 * maxWidth, 4.0 * sigma + maxWidth);
		}
	}

	internal sealed class PathState
	{
		internal readonly int Batch;

		internal readonly int VertexOffset;

		internal readonly bool[] Active;

		internal readonly double[] Fraction;

		internal readonly double[] ToStart;

		internal readonly double[] ToEnd;

		internal PathState(PathSpec p, int offset)
		{
			Batch = (p.Floor ? 1 : 0);
			VertexOffset = offset;
			Active = new bool[p.Count];
			Fraction = new double[p.Count];
			ToStart = new double[p.Count];
			ToEnd = new double[p.Count];
		}
	}

	public sealed class Buffer
	{
		public readonly MeshData[] Batches;

		public readonly int VertexCount;

		public readonly int TriangleCount;

		internal readonly PathState[] States;

		internal readonly int FieldsOffset;

		public double VisualAge;

		public double MainCoreEnvelope;

		public double PlasmaEnvelope;

		public double AirEnvelope;

		public double ScatterEnvelope;

		public double TargetLightEnvelope;

		public double FloorLightEnvelope;

		internal Buffer()
		{
			int body = 0;
			int floor = 0;
			for (int i = 0; i < Paths.Length; i++)
			{
				if (Paths[i].Floor)
				{
					floor += Paths[i].Count;
				}
				else
				{
					body += Paths[i].Count;
				}
			}
			Batches = new MeshData[2]
			{
				new MeshData(body, 2, floor: false),
				new MeshData(floor, 0, floor: true)
			};
			FieldsOffset = body * 8;
			States = new PathState[Paths.Length];
			int bodyOffset = 0;
			int floorOffset = 0;
			for (int j = 0; j < Paths.Length; j++)
			{
				PathSpec p = Paths[j];
				States[j] = new PathState(p, p.Floor ? floorOffset : bodyOffset);
				if (p.Floor)
				{
					floorOffset += p.Count * 8;
				}
				else
				{
					bodyOffset += p.Count * 8;
				}
			}
			VertexCount = Batches[0].VertexCount + Batches[1].VertexCount;
			TriangleCount = Batches[0].TriangleCount + Batches[1].TriangleCount;
		}
	}

	public const double NativeLead = 0.14;

	public const double VisibleEnd = 0.26;

	public const double SourcePixelsPerHeight = 192.0;

	public const double FootX = 11.0 / 96.0;

	public const double FootY = -1.0;

	public const double SourcePixelWorld = 0.007569444166666666;

	public const int BatchCount = 2;

	public const int RowsPerSegment = 4;

	public const int VerticesPerSegment = 8;

	public const int MainKind = 0;

	public const int BranchKind = 1;

	public const int CrownKind = 2;

	public const int TertiaryKind = 3;

	public const int LateralKind = 4;

	public const int QuietGroundKind = 5;

	public const int FloorForkKind = 6;

	public const int LeaderKind = 7;

	public const int ContactAirKind = 8;

	public const int HotKind = 9;

	public const int StreamerKind = 10;

	public static readonly Point ReferenceFoot = new Point(11.0 / 96.0, -1.0, 0.0);

	private static readonly Point Shadow = Color(3355991);

	private static readonly Point Primary = Color(9604294);

	private static readonly Point Edge = Color(12236003);

	private static readonly Point Core = Color(15657722);

	private static readonly Point Violet = Color(7824824);

	internal static readonly PathSpec[] Paths = new PathSpec[21]
	{
		new PathSpec("main", 0, floor: false, 77, 0.0, 0.019, 0.045, 0.11, 0.89, 0.0, new double[32]
		{
			135.0, -14.0, 154.0, 22.0, 180.0, 38.0, 176.0, 50.0, 218.0, 93.0,
			220.0, 111.0, 250.0, 137.0, 265.0, 162.0, 287.0, 180.0, 300.0, 195.0,
			320.0, 188.0, 333.0, 207.0, 351.0, 195.0, 368.0, 210.0, 390.0, 201.0,
			406.0, 213.0
		}, new double[15]
		{
			0.7437340264725795, 0.6860886076258363, 0.8294887643798307, 0.6565119568285454, 0.9040286219408146, 1.0961495479110037, 1.7990633851600528, 2.867019839591288, 2.2725447656057485, 2.456415222367752,
			1.6358909708511118, 1.2289475079801289, 1.095458306824502, 0.7203566003227864, 0.8469598892768168
		}, new double[15]
		{
			5.121749373914638, 5.808462600834684, 4.876390709126953, 6.3509670144159625, 5.194010360078647, 7.1952034836461785, 9.358210835229942, 11.365843538882103, 13.72176434321002, 9.916848841499906,
			10.388633453614093, 6.726181028666794, 6.476788506295255, 5.657567996378757, 5.014131663405274
		}, new double[15]
		{
			0.7859323085424931, 0.5511719558003152, 0.24388819695343822, 0.3344532064779281, 0.3186901977677823, 0.3901540111651641, 0.8013006796423605, 0.0908016399916799, 0.37362439276446435, 0.7913229002641682,
			0.758671464568362, 0.6039750393802742, 0.13074783281680769, 0.3598446831507598, 0.8708048524974217
		}, null),
		new PathSpec("branch_0", 1, floor: false, 3, 0.0, 0.041, 0.028, 0.052, 0.94, 0.055, new double[16]
		{
			265.0, 162.0, 258.5350565044402, 168.15253660703183, 255.90405131897612, 167.10405000303436, 254.04959984930912, 163.84972185989918, 252.73427337302348, 164.5712521768814,
			249.33363703169437, 167.59424190601766, 244.9779047487149, 173.4070911176799, 237.0, 174.0
		}, new double[7] { 0.64, 0.5742804119760665, 0.5066188968086338, 0.4366096452482233, 0.36365732727203237, 0.2868118681836302, 0.2043379195593194 }, new double[7] { 1.8, 1.59540616562596, 1.3840076869359001, 1.1641659694101023, 0.9333287550955546, 0.6870202037312718, 0.41549757290127753 }, new double[7] { 0.08564916714362436, 0.2368105065960997, 0.8012744652063969, 0.5821620360643678, 0.09412864224039919, 0.4331269402364738, 0.479051298140834 }, null),
		new PathSpec("branch_1", 1, floor: false, 4, 0.002, 0.067, 0.049, 0.052, 0.94, 0.055, new double[16]
		{
			300.0, 195.0, 300.7785455487393, 197.0760876374829, 301.0134984586888, 197.45773821220874, 306.3453322179056, 199.1300690666898, 307.14405353080076, 209.69804266401528,
			311.4294639812353, 206.66467081664192, 311.35411923103925, 211.10685619570017, 317.0, 217.0
		}, new double[7] { 0.64, 0.5742804119760665, 0.5066188968086338, 0.4366096452482233, 0.36365732727203237, 0.2868118681836302, 0.2043379195593194 }, new double[7] { 1.8, 1.59540616562596, 1.3840076869359001, 1.1641659694101023, 0.9333287550955546, 0.6870202037312718, 0.41549757290127753 }, new double[7] { 0.9430561055723676, 0.5113275528143616, 0.9762437057077041, 0.08083602389560218, 0.6073558319950296, 0.37648658437727256, 0.8019012069858072 }, null),
		new PathSpec("branch_2", 1, floor: false, 5, 0.004, 0.093, 0.07, 0.052, 0.94, 0.055, new double[16]
		{
			333.0, 207.0, 329.7406360633397, 208.74993466805978, 323.95019890086616, 212.06664623614614, 321.42691010149355, 211.20336665938848, 321.52773605351325, 212.15566802986504,
			320.4680936209279, 211.88745541179773, 311.40169244605084, 214.53613090180767, 299.0, 222.0
		}, new double[7] { 0.64, 0.5742804119760665, 0.5066188968086338, 0.4366096452482233, 0.36365732727203237, 0.2868118681836302, 0.2043379195593194 }, new double[7] { 1.8, 1.59540616562596, 1.3840076869359001, 1.1641659694101023, 0.9333287550955546, 0.6870202037312718, 0.41549757290127753 }, new double[7] { 0.8050029237453802, 0.8079407897364937, 0.515325561042142, 0.2858013800881416, 0.053930702381656426, 0.38336888078551823, 0.40847320541999865 }, null),
		new PathSpec("branch_3", 1, floor: false, 6, 0.0, 0.041, 0.028, 0.052, 0.94, 0.055, new double[16]
		{
			368.0, 210.0, 374.6196123255276, 213.8025863161753, 377.49524883393076, 218.99660319623487, 378.010068510973, 218.69337652015977, 384.30514417516684, 222.76943909746967,
			383.4156352607704, 223.5927586622023, 386.2413877983962, 228.22602183278815, 391.0, 235.0
		}, new double[7] { 0.64, 0.5742804119760665, 0.5066188968086338, 0.4366096452482233, 0.36365732727203237, 0.2868118681836302, 0.2043379195593194 }, new double[7] { 1.8, 1.59540616562596, 1.3840076869359001, 1.1641659694101023, 0.9333287550955546, 0.6870202037312718, 0.41549757290127753 }, new double[7] { 0.5381643514719432, 0.34327086981333843, 0.36906723979537825, 0.37449676558788236, 0.9874449901864666, 0.6327562726071461, 0.6743239305044936 }, null),
		new PathSpec("tertiary_0", 3, floor: false, 53, 0.0, 0.044, 0.025, 0.06, 0.9, 0.055, new double[10] { 252.73427337302343, 164.5712521768814, 257.31516883117615, 167.12577414317346, 266.7820353988039, 168.07337197104465, 266.9273928209569, 168.4591463435667, 272.7342733730234, 172.5712521768814 }, new double[4] { 0.5, 0.4, 0.30000000000000004, 0.2 }, new double[4] { 1.65, 1.2, 0.75, 0.3 }, new double[4] { 0.011126994564266246, 0.7189106537677009, 0.3311271911509952, 0.9330886636200264 }, null),
		new PathSpec("tertiary_3", 3, floor: false, 56, 0.0, 0.044, 0.025, 0.06, 0.9, 0.055, new double[10] { 384.30514417516684, 222.76943909746967, 378.6035675462647, 223.5979855783411, 370.4877776028749, 226.78114979992347, 369.5708724320084, 228.81950390305246, 367.0514236969112, 229.33559928685688 }, new double[4] { 0.5, 0.4, 0.30000000000000004, 0.2 }, new double[4] { 1.65, 1.2, 0.75, 0.3 }, new double[4] { 0.72612604996354, 0.01690281447864317, 0.9808010677692758, 0.7246042121238003 }, null),
		new PathSpec("crown_0", 2, floor: false, 321, 0.0, 0.032, 0.025, 0.052, 0.94, 0.075, new double[14]
		{
			287.0, 180.0, 278.40181986827446, 173.09680750746065, 277.8407105461927, 173.64628761808703, 268.3547656771252, 170.95141877994277, 245.19083970060439, 158.48532392842623,
			242.82845509870455, 158.3153567111565, 239.0, 157.0
		}, new double[6] { 2.1, 1.73, 1.36, 0.9900000000000002, 0.6200000000000001, 0.25 }, new double[6] { 6.5, 5.3100000000000005, 4.12, 2.93, 1.7400000000000002, 0.55 }, new double[6] { 0.6587666953866232, 0.9083083579615744, 0.4834383285257762, 0.7254101662830692, 0.5558565071474987, 0.9005752423612045 }, null),
		new PathSpec("crown_1", 2, floor: false, 322, 0.0, 0.046, 0.037000000000000005, 0.052, 0.94, 0.075, new double[14]
		{
			287.0, 180.0, 282.9827151376296, 175.5428383474617, 271.88240532922237, 155.43517730497823, 264.5383114063284, 157.41201502257996, 265.97881497447645, 154.26997466795498,
			261.5241363500142, 144.7489403882338, 255.0, 138.0
		}, new double[6] { 1.5, 1.25, 1.0, 0.75, 0.5, 0.25 }, new double[6] { 4.8, 3.9499999999999997, 3.0999999999999996, 2.25, 1.4, 0.55 }, new double[6] { 0.30865841803565464, 0.47613583535314485, 0.2756046976830532, 0.260604485018868, 0.5781722124294643, 0.1886597917425321 }, null),
		new PathSpec("crown_2", 2, floor: false, 323, 0.0, 0.06, 0.049, 0.052, 0.94, 0.075, new double[14]
		{
			287.0, 180.0, 287.48304081125104, 171.2557942295813, 282.1423308504191, 160.05802014736338, 280.9648240706695, 156.65830256301123, 280.935750463027, 152.4019772065168,
			281.0849774493509, 152.06049669980845, 280.0, 148.0
		}, new double[6] { 1.5, 1.25, 1.0, 0.75, 0.5, 0.25 }, new double[6] { 4.8, 3.9499999999999997, 3.0999999999999996, 2.25, 1.4, 0.55 }, new double[6] { 0.278812721563513, 0.8351916586260162, 0.9738793266391149, 0.7429897207741973, 0.12169238144260341, 0.7087880307290088 }, null),
		new PathSpec("crown_3", 2, floor: false, 324, 0.0, 0.032, 0.025, 0.052, 0.94, 0.075, new double[14]
		{
			287.0, 180.0, 298.0514473676336, 167.5498022014047, 294.60496812372827, 163.68568458073494, 298.22704647301197, 163.28567301740824, 302.02896254392164, 156.64244861662928,
			304.4053822481904, 154.80698780143098, 308.0, 152.0
		}, new double[6] { 2.1, 1.73, 1.36, 0.9900000000000002, 0.6200000000000001, 0.25 }, new double[6] { 6.5, 5.3100000000000005, 4.12, 2.93, 1.7400000000000002, 0.55 }, new double[6] { 0.24408523539999183, 0.6028541110667572, 0.5592913990623791, 0.3115539947888526, 0.5692071095471275, 0.8369817404662427 }, null),
		new PathSpec("crown_4", 2, floor: false, 325, 1.0 / 60.0, 0.046, 0.037000000000000005, 0.052, 0.94, 0.075, new double[14]
		{
			287.0, 180.0, 311.70724223991436, 177.4357903598671, 319.5331775705035, 174.94902542240257, 320.0010635367488, 175.30397134591226, 326.30146525662883, 170.5126438423387,
			327.42917024354676, 172.46553722321258, 332.0, 171.0
		}, new double[6] { 1.5, 1.25, 1.0, 0.75, 0.5, 0.25 }, new double[6] { 4.8, 3.9499999999999997, 3.0999999999999996, 2.25, 1.4, 0.55 }, new double[6] { 0.46855820995129116, 0.37993473037976866, 0.6507403489073045, 0.6362118329446164, 0.1968689984895272, 0.09930328709881964 }, null),
		new PathSpec("crown_5", 2, floor: false, 326, 1.0 / 60.0, 0.06, 0.049, 0.052, 0.94, 0.075, new double[14]
		{
			287.0, 180.0, 290.4681072149597, 181.55040753105283, 300.23152946437904, 181.99984209398121, 299.7135009818745, 189.26748529387092, 308.1430458912458, 186.71763530028682,
			309.8440699634726, 190.60127572631234, 312.7742741032706, 192.31155035510102
		}, new double[6] { 1.5, 1.25, 1.0, 0.75, 0.5, 0.25 }, new double[6] { 4.8, 3.9499999999999997, 3.0999999999999996, 2.25, 1.4, 0.55 }, new double[6] { 0.047688918705065864, 0.5275966692685, 0.8701618885329616, 0.9297560789308774, 0.438973955822318, 0.6757573648127135 }, null),
		new PathSpec("crown_6", 2, floor: false, 327, 1.0 / 60.0, 0.032, 0.025, 0.052, 0.94, 0.075, new double[14]
		{
			287.0, 180.0, 273.5239173219245, 188.6022563339016, 262.7566455188916, 188.5226823035024, 263.57456730940805, 191.07919605927353, 261.9915022392129, 190.7318626195199,
			257.37391753803723, 192.27629393668863, 250.36569880436082, 196.415400473468
		}, new double[6] { 2.1, 1.73, 1.36, 0.9900000000000002, 0.6200000000000001, 0.25 }, new double[6] { 6.5, 5.3100000000000005, 4.12, 2.93, 1.7400000000000002, 0.55 }, new double[6] { 0.6371600358718973, 0.2247769528841198, 0.5848005325738626, 0.1427971051501683, 0.7734903071893954, 0.09434197224786711 }, null),
		new PathSpec("lateral", 4, floor: false, 919, 0.0, 0.052, 0.045, 0.07, 0.92, 0.075, new double[12]
		{
			289.0, 181.0, 296.0, 201.0, 312.0, 198.0, 317.0, 217.0, 341.0, 210.0,
			353.0, 224.0
		}, new double[5] { 1.1, 1.1, 1.1, 1.1, 1.1 }, new double[5] { 2.6, 2.6, 2.6, 2.6, 2.6 }, new double[5] { 0.951217803728873, 0.7965552881727637, 0.5468225622318904, 0.23743503737116844, 0.4659264546617691 }, null),
		new PathSpec("quiet_ground", 5, floor: false, 661, 0.0, 0.083, 0.029, 0.09, 0.95, 0.0, new double[14]
		{
			289.0, 182.0, 291.0, 217.0, 303.0, 241.0, 297.0, 271.0, 310.0, 296.0,
			300.0, 329.0, 309.0, 372.0
		}, new double[6] { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 }, new double[6] { 0.22, 0.22, 0.22, 0.22, 0.22, 0.22 }, new double[6] { 0.2167967234311472, 0.5476183966846363, 0.7067182835925018, 0.8511968892358329, 0.03605452264297737, 0.7725334192503636 }, null),
		new PathSpec("ground_fork_0", 6, floor: true, 37, 0.0, 0.059, 0.047, 0.12, 0.9, 0.0, new double[18]
		{
			309.0, 372.0, 346.71388455520724, 376.88665273949465, 350.9880297396662, 372.58355997277823, 361.0110735381491, 375.18324935888455, 362.0427585458587, 377.55544810343696,
			363.66271212786603, 376.50058478364235, 368.9628898190403, 377.1458210495912, 372.1750770193859, 376.8585873847415, 378.0757386189416, 377.245958487737
		}, new double[8] { 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85 }, new double[8] { 0.371, 0.371, 0.371, 0.371, 0.371, 0.371, 0.371, 0.371 }, new double[8] { 0.7041530316135696, 0.6616687208024741, 0.06899323623202247, 0.7028016471097188, 0.3189622141142259, 0.45009567421035723, 0.9806145334024735, 0.06443209671845207 }, null),
		new PathSpec("ground_fork_1", 6, floor: true, 38, 0.0, 0.059, 0.047, 0.12, 0.9, 0.0, new double[18]
		{
			309.0, 372.0, 304.86374840258594, 372.0985432477434, 296.9585153519427, 367.3181714836833, 296.57409021694247, 368.7022924750663, 295.6763780009853, 368.43924814778745,
			293.2827217631551, 370.63753810665355, 292.5286772398162, 367.20694853027555, 288.2664023456175, 365.3258915661975, 271.92126532936715, 364.463137807213
		}, new double[8] { 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85 }, new double[8] { 0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7 }, new double[8] { 0.48576104483786753, 0.24973585906507756, 0.7203811664111007, 0.6346140062618605, 0.9325717420086586, 0.03952515455075356, 0.8350903230139075, 0.49659704805823346 }, null),
		new PathSpec("ground_fork_2", 6, floor: true, 39, 0.0, 0.059, 0.047, 0.12, 0.9, 0.0, new double[18]
		{
			309.0, 372.0, 296.54747894996154, 370.470699979395, 294.0047519023489, 368.5132864286747, 293.8415724079132, 367.50477799079977, 293.5595794338311, 367.90867216939967,
			291.94432022561267, 366.3294552754758, 288.35985926422063, 365.95932109976434, 269.9329188197759, 359.77823268188234, 264.93626301618775, 359.34439323494564
		}, new double[8] { 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85, 0.85 }, new double[8] { 0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7, 0.7 }, new double[8] { 0.5017119571514081, 0.5393354062064886, 0.13587420195528843, 0.306386568421659, 0.5791846788017333, 0.7912866394965615, 0.15107398269944206, 0.22916386669926614 }, null),
		new PathSpec("leader", 7, floor: false, 0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, new double[32]
		{
			135.0, -14.0, 154.0, 22.0, 180.0, 38.0, 176.0, 50.0, 218.0, 93.0,
			220.0, 111.0, 250.0, 137.0, 265.0, 162.0, 287.0, 180.0, 300.0, 195.0,
			320.0, 188.0, 333.0, 207.0, 351.0, 195.0, 368.0, 210.0, 390.0, 201.0,
			406.0, 213.0
		}, new double[15]
		{
			0.72, 0.72, 0.72, 0.72, 0.72, 0.72, 0.72, 0.72, 0.72, 0.72,
			0.72, 0.72, 0.72, 0.72, 0.72
		}, new double[15]
		{
			0.2, 0.2, 0.2, 0.2, 0.2, 0.2, 0.2, 0.2, 0.2, 0.2,
			0.2, 0.2, 0.2, 0.2, 0.2
		}, new double[15]
		{
			0.6369616873214543, 0.2697867137638703, 0.04097352393619469, 0.016527635528529094, 0.8132702392002724, 0.9127555772777217, 0.6066357757671799, 0.7294965609839984, 0.5436249914654229, 0.9350724237877682,
			0.8158535541215322, 0.002738500170148095, 0.8574042765875693, 0.033585575305464355, 0.7296554464299441
		}, new double[15]
		{
			40.70626487409524, 30.528675044947494, 12.649110640673518, 60.108235708594876, 18.110770276274835, 39.69886648255846, 29.154759474226502, 28.42534080710379, 25.755821668303398, 19.55515542118263,
			30.281376228362156, 20.160469695530338, 28.714983306222532, 21.860019655138753, 24.870109978070108
		}),
		new PathSpec("streamer", 10, floor: false, 0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, new double[8] { 309.0000000000001, 371.99999999999994, 305.7331668532408, 346.5561292661246, 323.3394734673537, 324.3953386269427, 322.5092102010305, 300.5930079404141 }, new double[3] { 0.6, 0.6, 0.6 }, new double[3] { 0.23, 0.23, 0.23 }, new double[3] { 0.6369616873214543, 0.2697867137638703, 0.04097352393619469 }, new double[3] { 33.24154027718932, 28.792360097775937, 30.364452901377952 })
	};

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
		x = Clamp(x);
		return x * x * (3.0 - 2.0 * x);
	}

	public static double Lead()
	{
		return 0.14;
	}

	public static double End()
	{
		return 0.26;
	}

	public static Buffer Create()
	{
		Buffer b = new Buffer();
		Sample(b, 0.0);
		return b;
	}

	public static void Sample(Buffer b, double age)
	{
		if (b == null)
		{
			throw new ArgumentNullException("b");
		}
		if (!Finite(age))
		{
			throw new ArgumentException("Finite age required.", "age");
		}
		b.VisualAge = age;
		bool post = age >= 0.0 && age < 0.26;
		bool pre = age > -0.14 && age < 0.0;
		b.MainCoreEnvelope = (post ? Math.Exp((0.0 - age) / 0.019) : 0.0);
		b.PlasmaEnvelope = (post ? Math.Exp((0.0 - age) / 0.036) : 0.0);
		b.AirEnvelope = (post ? Math.Exp((0.0 - age) / 0.043) : 0.0);
		b.ScatterEnvelope = (post ? Math.Exp((0.0 - age) / 0.062) : 0.0);
		b.TargetLightEnvelope = b.AirEnvelope;
		b.FloorLightEnvelope = (post ? Math.Exp((0.0 - age) / 0.058) : 0.0);
		for (int i = 0; i < b.Batches.Length; i++)
		{
			MeshData m = b.Batches[i];
			m.Visible = false;
			Array.Clear(m.UV1, 0, m.UV1.Length);
		}
		for (int j = 0; j < Paths.Length; j++)
		{
			SamplePath(b, Paths[j], b.States[j], age, pre, post);
		}
		SampleField(b, 0, 8, 124.0, 124.0, 0.13, post ? (4.8 * Math.Exp((0.0 - age) / 0.045)) : 0.0);
		SampleField(b, 1, 9, 32.0, 20.0, 0.15, post ? (3.8 * Math.Exp((0.0 - age) / 0.022)) : 0.0);
	}

	private static void SamplePath(Buffer b, PathSpec p, PathState s, double age, bool pre, bool post)
	{
		bool precursor = p.Kind == 7 || p.Kind == 10;
		double localAge = age - p.Birth;
		bool enabled = (precursor ? pre : (post && localAge >= 0.0 && (p.Kind != 3 || age < 0.083)));
		double clipFraction = 1.0;
		double leaderU = 0.0;
		if (p.Kind == 7)
		{
			leaderU = Smooth((age + 0.14) / 0.14);
			clipFraction = 0.04 + 0.91 * leaderU;
		}
		if (p.Kind == 10)
		{
			enabled = enabled && age > -0.034;
			clipFraction = Clamp((age + 0.034) / 0.034);
		}
		double remaining = p.ClipTotal * clipFraction;
		double fragment = (precursor ? 0.0 : (Smooth((localAge - p.FragmentStart) / p.FragmentSpan) * p.FragmentMax));
		for (int k = 0; k < p.Count; k++)
		{
			double fraction = (precursor ? Clamp(remaining / p.ClipLengths[k]) : 1.0);
			remaining -= p.ClipLengths[k];
			s.Fraction[k] = fraction;
			bool tipSkip = p.Kind == 6 && p.Cumulative[k] / p.TotalLength > 0.78 && (k + p.Seed) % 3 != 0;
			s.Active[k] = enabled && fraction > 0.0 && (!(fragment > 0.0) || !(p.Random[k] < fragment)) && !tipSkip;
		}
		double run = 0.0;
		for (int i = 0; i < p.Count; i++)
		{
			s.ToStart[i] = run;
			run = (s.Active[i] ? (run + p.Lengths[i] * s.Fraction[i]) : 0.0);
		}
		run = 0.0;
		for (int k2 = p.Count - 1; k2 >= 0; k2--)
		{
			s.ToEnd[k2] = run;
			run = (s.Active[k2] ? (run + p.Lengths[k2] * s.Fraction[k2]) : 0.0);
		}
		MeshData m = b.Batches[s.Batch];
		for (int j = 0; j < p.Count; j++)
		{
			bool active = s.Active[j];
			if (active)
			{
				m.Visible = true;
			}
			double env = ((enabled && !precursor) ? Math.Exp((0.0 - localAge) / p.Tau) : 0.0);
			Quad strength = default(Quad);
			if (active)
			{
				double e = p.Energy[j];
				strength = p.Kind switch
				{
					0 => new Quad(e * b.MainCoreEnvelope, 2.5 * b.PlasmaEnvelope, 5.3 * b.AirEnvelope, 5.2 * b.ScatterEnvelope), 
					1 => new Quad(e * env, 1.9 * e * env, 0.0, 0.0), 
					2 => new Quad(e * env, 1.15 * e * env, 0.0, 0.0), 
					4 => new Quad(e * env, 0.0, 7.74 * Math.Exp((0.0 - age) / 0.056), 0.0), 
					7 => new Quad(0.2 + 0.23 * leaderU, 0.0, 0.7, 0.0), 
					10 => new Quad(0.23, 0.0, 0.0, 0.0), 
					_ => new Quad(e * env, 0.0, 0.0, 0.0), 
				};
				if (strength.X < 0.001)
				{
					strength.X = 0.0;
				}
				if (p.Kind == 1 && 1.9 * p.MaxEnergy * env < 0.001)
				{
					strength.Y = 0.0;
				}
				else if (p.Kind == 2 && 1.15 * p.MaxEnergy * env < 0.001)
				{
					strength.Y = 0.0;
				}
				else if (p.Kind != 1 && p.Kind != 2 && strength.Y < 0.001)
				{
					strength.Y = 0.0;
				}
				if (strength.Z < 0.001)
				{
					strength.Z = 0.0;
				}
				if (strength.W < 0.001)
				{
					strength.W = 0.0;
				}
			}
			double ax = p.XY[j * 2];
			double ay = p.XY[j * 2 + 1];
			double dx = p.XY[j * 2 + 2] - ax;
			double dy = p.XY[j * 2 + 3] - ay;
			double fraction2 = s.Fraction[j];
			double length = p.Lengths[j] * fraction2;
			for (int row = 0; row < 4; row++)
			{
				double t = row switch
				{
					2 => 0.75, 
					1 => 0.25, 
					0 => 0.0, 
					_ => 1.0, 
				};
				double st = t * fraction2;
				double x = ax + dx * st;
				double y = ay + dy * st;
				double q = (p.Cumulative[j] + p.Lengths[j] * st) / p.TotalLength;
				double nx = (0.0 - dy) / p.Lengths[j];
				double ny = dx / p.Lengths[j];
				int neighbor = ((row == 0) ? (j - 1) : ((row == 3 && fraction2 == 1.0) ? (j + 1) : (-1)));
				if (neighbor >= 0 && neighbor < p.Count && active && s.Active[neighbor])
				{
					double ndx = p.XY[neighbor * 2 + 2] - p.XY[neighbor * 2];
					double ndy = p.XY[neighbor * 2 + 3] - p.XY[neighbor * 2 + 1];
					nx += (0.0 - ndy) / p.Lengths[neighbor];
					ny += ndx / p.Lengths[neighbor];
					double n = Math.Sqrt(nx * nx + ny * ny);
					if (n > 1E-08)
					{
						nx /= n;
						ny /= n;
					}
					else
					{
						nx = (0.0 - dy) / p.Lengths[j];
						ny = dx / p.Lengths[j];
					}
				}
				double width = p.Widths[j];
				if (p.Kind == 4 || p.Kind == 5 || p.Kind == 6)
				{
					double wave = Math.Sin(((double)j + st) * 1.73 + 0.4);
					width *= (0.66 + 0.37 * wave * wave) * (1.0 - 0.32 * q);
					if (p.Kind == 6)
					{
						width *= 1.0 - 0.68 * p.Cumulative[j] / p.TotalLength;
					}
				}
				double endDistance = Math.Min(s.ToStart[j] + length * t, s.ToEnd[j] + length * (1.0 - t));
				double depth = p.Depth / 1.4533332799999998 * Smooth(q / 0.25);
				for (int side = 0; side < 2; side++)
				{
					int v = s.VertexOffset + j * 8 + row * 2 + side;
					double distance = ((side == 0) ? (0.0 - p.Radius) : p.Radius);
					double originX = (p.Floor ? 309 : 287);
					double originY = (p.Floor ? 372 : 180);
					m.Vertices[v] = new Point((x + nx * distance - originX) / 192.0, (originY - y - ny * distance) / 192.0, depth);
					m.GroundWeight[v] = (p.Floor ? 0.0 : Clamp((y - 180.0) / 192.0));
					m.UV0[v] = new Quad(distance, t, width, p.Kind);
					m.UV1[v] = strength;
					m.UV2[v] = new Quad(x, y, p.Radius, Math.Max(0.0, endDistance));
				}
			}
		}
	}

	private static void SampleField(Buffer b, int field, int kind, double width, double height, double depth, double strength)
	{
		MeshData m = b.Batches[0];
		if (strength > 0.0)
		{
			m.Visible = true;
		}
		for (int corner = 0; corner < 4; corner++)
		{
			double x = ((corner != 0 && corner != 3) ? 1 : (-1));
			double y = ((corner >= 2) ? 1 : (-1));
			int v = b.FieldsOffset + field * 4 + corner;
			m.Vertices[v] = new Point(x * width * 0.5 / 192.0, y * height * 0.5 / 192.0, depth / 1.4533332799999998);
			m.GroundWeight[v] = 0.0;
			m.UV0[v] = new Quad(x, y, 0.0, kind);
			m.UV1[v] = new Quad(strength, 0.0, 0.0, 0.0);
			m.UV2[v] = default(Quad);
		}
	}

	public static double Erf(double value)
	{
		double a = Math.Abs(value);
		double t = 1.0 / (1.0 + 0.3275911 * a);
		double result = 1.0 - ((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp((0.0 - a) * a);
		return (value < 0.0) ? (0.0 - result) : result;
	}

	public static double Profile(double distance, double width, double sigma)
	{
		return Math.Max(0.0, 0.5 * (Erf((distance + width * 0.5) / (1.4142135623730951 * sigma)) - Erf((distance - width * 0.5) / (1.4142135623730951 * sigma))));
	}

	private static double Gaussian(double x, double y, double cx, double cy, double sx, double sy)
	{
		x = (x - cx) / sx;
		y = (y - cy) / sy;
		return Math.Exp(-0.5 * (x * x + y * y));
	}

	public static double Srgb(double s)
	{
		return (s <= 0.04045) ? (s / 12.92) : Math.Pow((s + 0.055) / 1.055, 2.4);
	}

	private static Point Color(int hex)
	{
		return new Point(Srgb((double)((hex >> 16) & 0xFF) / 255.0), Srgb((double)((hex >> 8) & 0xFF) / 255.0), Srgb((double)(hex & 0xFF) / 255.0));
	}

	private static Point Sum(Point a, double sa, Point b, double sb, Point c, double sc, Point d, double sd)
	{
		return new Point(a.X * sa + b.X * sb + c.X * sc + d.X * sd, a.Y * sa + b.Y * sb + c.Y * sc + d.Y * sd, a.Z * sa + b.Z * sb + c.Z * sc + d.Z * sd);
	}

	public static Point EvaluateRadiance(Quad uv0, Quad energy, Quad uv2)
	{
		int kind = (int)Math.Round(uv0.W);
		if (kind == 8 || kind == 9)
		{
			double u = (uv0.X + 1.0) * 0.5;
			double v = (uv0.Y + 1.0) * 0.5;
			double window = Smooth(u / 0.1) * Smooth((1.0 - u) / 0.1) * Smooth(v / 0.1) * Smooth((1.0 - v) / 0.1);
			double x = uv0.X * (double)((kind == 9) ? 16 : 62);
			double y = (0.0 - uv0.Y) * (double)((kind == 9) ? 10 : 62);
			double density = ((kind == 9) ? (Gaussian(x, y, 0.0, 0.0, 3.3, 2.0) + 0.38 * Gaussian(x, y, -6.0, -2.0, 4.5, 1.8)) : ((0.82 * Gaussian(x, y, -10.0, -4.0, 17.0, 13.0) + 0.42 * Gaussian(x, y, 18.0, 5.0, 20.0, 8.0) + 0.24 * Gaussian(x, y, 4.0, -23.0, 11.0, 21.0)) * (1.0 - 0.67 * Gaussian(x, y, 6.0, 7.0, 8.0, 13.0))));
			return Sum((kind == 9) ? Core : Violet, energy.X * density * window, Core, 0.0, Core, 0.0, Core, 0.0);
		}
		double d = uv0.X;
		double width = uv0.Z;
		double radius = uv2.Z;
		if (Math.Abs(d) >= radius || uv2.W <= 0.0 || width <= 0.0)
		{
			return default(Point);
		}
		double coreCoverage = Smooth((width * 0.5 - Math.Abs(d)) / Math.Max(0.06, width * 0.15)) * Smooth(uv2.W / Math.Max(0.1, width));
		double plasma = 0.0;
		double air = 0.0;
		double scatter = 0.0;
		double endWindow = Smooth(uv2.W / 7.0);
		Point coreColor = Core;
		Point plasmaColor = Primary;
		Point airColor = Primary;
		if (kind == 0)
		{
			double w = width * 2.55;
			plasma = Profile(d, w, 0.7) * Smooth((radius - Math.Abs(d)) / 1.0);
			double wave = Math.Sin(uv2.X * 0.064 + uv2.Y * 0.041);
			air = Profile(d, w, 5.4) * (0.54 + 0.46 * wave * wave) * Smooth((radius - Math.Abs(d)) / 3.24);
			scatter = Profile(d, w, 14.0) * Smooth((radius - Math.Abs(d)) / 8.4);
		}
		else if (kind == 1)
		{
			coreColor = Edge;
			plasmaColor = Violet;
			plasma = Profile(d, width, 2.4) * Smooth((radius - Math.Abs(d)) / 1.44);
		}
		else if (kind == 2)
		{
			plasma = Profile(d, width, 1.7) * Smooth((radius - Math.Abs(d)) / 1.02);
		}
		else if (kind == 4)
		{
			coreColor = Edge;
			airColor = Violet;
			air = Profile(d, width, 3.2) * Smooth((radius - Math.Abs(d)) / 1.92);
		}
		else if (kind != 7)
		{
			coreColor = ((kind != 5 && kind != 10) ? Edge : Primary);
		}
		else
		{
			coreColor = Primary;
			airColor = Shadow;
			air = Profile(d, width, 2.0) * Smooth((radius - Math.Abs(d)) / 1.2);
		}
		return Sum(coreColor, energy.X * coreCoverage, plasmaColor, energy.Y * plasma * endWindow, airColor, energy.Z * air * endWindow, Shadow, energy.W * scatter * endWindow);
	}
}
