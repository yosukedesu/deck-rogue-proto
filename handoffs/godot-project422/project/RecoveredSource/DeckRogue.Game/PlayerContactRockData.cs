namespace DeckRogue.Game;

internal static class PlayerContactRockData
{
	internal sealed class Chunk
	{
		public int Index;

		public PlayerContactGeometry.Point Center;

		public PlayerContactGeometry.Point[] Front;

		public PlayerContactGeometry.Point[] Depth;

		public PlayerContactGeometry.Uv[] UV;

		public int[] FrontTriangles;

		public int[] DepthTriangles;
	}

	internal static readonly Chunk[] Chunks = new Chunk[9]
	{
		new Chunk
		{
			Index = 0,
			Center = new PlayerContactGeometry.Point(-9.251678553159212E-05, -0.00015320856721892274, 0.0),
			Front = new PlayerContactGeometry.Point[9]
			{
				new PlayerContactGeometry.Point(-9.251678553159212E-05, -0.00015320856721892274, 0.0),
				new PlayerContactGeometry.Point(0.07170237825594564, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.045990609432580265, 0.045990609432580265, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.0773123442808607, 0.0),
				new PlayerContactGeometry.Point(-0.044874934271143016, 0.044874934271143016, 0.0),
				new PlayerContactGeometry.Point(-0.07380611551528878, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.04785006803497571, -0.04785006803497571, 0.0),
				new PlayerContactGeometry.Point(0.0, -0.07345549263873158, 0.0),
				new PlayerContactGeometry.Point(0.048097995848628394, -0.04809799584862844, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[18]
			{
				new PlayerContactGeometry.Point(-9.251678553159212E-05, -0.00015320856721892274, 0.0),
				new PlayerContactGeometry.Point(0.07170237825594564, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.045990609432580265, 0.045990609432580265, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.0773123442808607, 0.0),
				new PlayerContactGeometry.Point(-0.044874934271143016, 0.044874934271143016, 0.0),
				new PlayerContactGeometry.Point(-0.07380611551528878, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.04785006803497571, -0.04785006803497571, 0.0),
				new PlayerContactGeometry.Point(0.0, -0.07345549263873158, 0.0),
				new PlayerContactGeometry.Point(0.048097995848628394, -0.04809799584862844, 0.0),
				new PlayerContactGeometry.Point(-8.604061054438067E-05, -0.00014248396751359815, -0.035),
				new PlayerContactGeometry.Point(0.06668321177802945, -0.0, -0.035),
				new PlayerContactGeometry.Point(0.04277126677229965, 0.04277126677229965, -0.035),
				new PlayerContactGeometry.Point(0.0, 0.07190048018120046, -0.035),
				new PlayerContactGeometry.Point(-0.041733688872163006, 0.041733688872163006, -0.035),
				new PlayerContactGeometry.Point(-0.06863968742921857, -0.0, -0.035),
				new PlayerContactGeometry.Point(-0.04450056327252742, -0.04450056327252742, -0.035),
				new PlayerContactGeometry.Point(0.0, -0.06831360815402038, -0.035),
				new PlayerContactGeometry.Point(0.04473113613922441, -0.044731136139224456, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[9]
			{
				new PlayerContactGeometry.Uv(0.5034449892502849, 0.4582488302655937),
				new PlayerContactGeometry.Uv(0.6438731576040515, 0.45854850057583274),
				new PlayerContactGeometry.Uv(0.5935818855168824, 0.548504437420083),
				new PlayerContactGeometry.Uv(0.5036259486726321, 0.6097685962696127),
				new PlayerContactGeometry.Uv(0.4158522313205603, 0.5463222179279045),
				new PlayerContactGeometry.Uv(0.35926390720532747, 0.45854850057583274),
				new PlayerContactGeometry.Uv(0.4100329793414174, 0.364955531244618),
				new PlayerContactGeometry.Uv(0.5036259486726321, 0.31487226453117567),
				new PlayerContactGeometry.Uv(0.5977038556687753, 0.3644705935796895)
			},
			FrontTriangles = new int[24]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				8, 0, 8, 1
			},
			DepthTriangles = new int[72]
			{
				1, 2, 11, 1, 11, 10, 2, 3, 12, 2,
				12, 11, 3, 4, 13, 3, 13, 12, 4, 5,
				14, 4, 14, 13, 5, 6, 15, 5, 15, 14,
				6, 7, 16, 6, 16, 15, 7, 8, 17, 7,
				17, 16, 8, 1, 10, 8, 10, 17, 9, 11,
				10, 9, 12, 11, 9, 13, 12, 9, 14, 13,
				9, 15, 14, 9, 16, 15, 9, 17, 16, 9,
				10, 17
			}
		},
		new Chunk
		{
			Index = 1,
			Center = new PlayerContactGeometry.Point(0.1121509273243526, 0.04488520473588396, 0.0),
			Front = new PlayerContactGeometry.Point[8]
			{
				new PlayerContactGeometry.Point(0.1121509273243526, 0.04488520473588396, 0.0),
				new PlayerContactGeometry.Point(0.07170237825594564, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.1667497168742922, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.14555164568973872, 0.02895202242015111, 0.0),
				new PlayerContactGeometry.Point(0.13183331947499677, 0.05460714889920875, 0.0),
				new PlayerContactGeometry.Point(0.11627391588575116, 0.07769174674208434, 0.0),
				new PlayerContactGeometry.Point(0.10695490565716338, 0.10695490565716338, 0.0),
				new PlayerContactGeometry.Point(0.045990609432580265, 0.045990609432580265, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[16]
			{
				new PlayerContactGeometry.Point(0.1121509273243526, 0.04488520473588396, 0.0),
				new PlayerContactGeometry.Point(0.07170237825594564, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.1667497168742922, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.14555164568973872, 0.02895202242015111, 0.0),
				new PlayerContactGeometry.Point(0.13183331947499677, 0.05460714889920875, 0.0),
				new PlayerContactGeometry.Point(0.11627391588575116, 0.07769174674208434, 0.0),
				new PlayerContactGeometry.Point(0.10695490565716338, 0.10695490565716338, 0.0),
				new PlayerContactGeometry.Point(0.045990609432580265, 0.045990609432580265, 0.0),
				new PlayerContactGeometry.Point(0.10430036241164792, 0.041743240404372085, -0.035),
				new PlayerContactGeometry.Point(0.06668321177802945, -0.0, -0.035),
				new PlayerContactGeometry.Point(0.15507723669309173, -0.0, -0.035),
				new PlayerContactGeometry.Point(0.135363030491457, 0.026925380850740532, -0.035),
				new PlayerContactGeometry.Point(0.122604987111747, 0.05078464847626414, -0.035),
				new PlayerContactGeometry.Point(0.10813474177374859, 0.07225332447013845, -0.035),
				new PlayerContactGeometry.Point(0.09946806226116195, 0.09946806226116195, -0.035),
				new PlayerContactGeometry.Point(0.04277126677229965, 0.04277126677229965, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[8]
			{
				new PlayerContactGeometry.Uv(0.7229890286553614, 0.5463423065783702),
				new PlayerContactGeometry.Uv(0.6438731576040515, 0.45854850057583274),
				new PlayerContactGeometry.Uv(0.8297822485131424, 0.45854850057583274),
				new PlayerContactGeometry.Uv(0.7883196026333783, 0.5151775892631298),
				new PlayerContactGeometry.Uv(0.761487062212465, 0.56535807101266),
				new PlayerContactGeometry.Uv(0.7310534423088146, 0.6105106934990572),
				new PlayerContactGeometry.Uv(0.7128258017987956, 0.6677483537019963),
				new PlayerContactGeometry.Uv(0.5935818855168824, 0.548504437420083)
			},
			FrontTriangles = new int[21]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				1
			},
			DepthTriangles = new int[63]
			{
				1, 2, 10, 1, 10, 9, 2, 3, 11, 2,
				11, 10, 3, 4, 12, 3, 12, 11, 4, 5,
				13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
				6, 7, 15, 6, 15, 14, 7, 1, 9, 7,
				9, 15, 8, 10, 9, 8, 11, 10, 8, 12,
				11, 8, 13, 12, 8, 14, 13, 8, 15, 14,
				8, 9, 15
			}
		},
		new Chunk
		{
			Index = 2,
			Center = new PlayerContactGeometry.Point(0.04930670443844345, 0.1267376367414242, 0.0),
			Front = new PlayerContactGeometry.Point[8]
			{
				new PlayerContactGeometry.Point(0.04930670443844345, 0.1267376367414242, 0.0),
				new PlayerContactGeometry.Point(0.045990609432580265, 0.045990609432580265, 0.0),
				new PlayerContactGeometry.Point(0.10695490565716338, 0.10695490565716338, 0.0),
				new PlayerContactGeometry.Point(0.08969659390631315, 0.13424043933165442, 0.0),
				new PlayerContactGeometry.Point(0.0659966399553294, 0.15932998325121034, 0.0),
				new PlayerContactGeometry.Point(0.03650818211771808, 0.18353902574612663, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.17979614949037373, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.0773123442808607, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[16]
			{
				new PlayerContactGeometry.Point(0.04930670443844345, 0.1267376367414242, 0.0),
				new PlayerContactGeometry.Point(0.045990609432580265, 0.045990609432580265, 0.0),
				new PlayerContactGeometry.Point(0.10695490565716338, 0.10695490565716338, 0.0),
				new PlayerContactGeometry.Point(0.08969659390631315, 0.13424043933165442, 0.0),
				new PlayerContactGeometry.Point(0.0659966399553294, 0.15932998325121034, 0.0),
				new PlayerContactGeometry.Point(0.03650818211771808, 0.18353902574612663, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.17979614949037373, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.0773123442808607, 0.0),
				new PlayerContactGeometry.Point(0.04585523512775241, 0.1178660021695245, -0.035),
				new PlayerContactGeometry.Point(0.04277126677229965, 0.04277126677229965, -0.035),
				new PlayerContactGeometry.Point(0.09946806226116195, 0.09946806226116195, -0.035),
				new PlayerContactGeometry.Point(0.08341783233287124, 0.12484360857843861, -0.035),
				new PlayerContactGeometry.Point(0.06137687515845634, 0.14817688442362562, -0.035),
				new PlayerContactGeometry.Point(0.03395260936947782, 0.17069129394389776, -0.035),
				new PlayerContactGeometry.Point(0.0, 0.16721041902604758, -0.035),
				new PlayerContactGeometry.Point(0.0, 0.07190048018120046, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[8]
			{
				new PlayerContactGeometry.Uv(0.6000680451176648, 0.7064426465147782),
				new PlayerContactGeometry.Uv(0.5935818855168824, 0.548504437420083),
				new PlayerContactGeometry.Uv(0.7128258017987956, 0.6677483537019963),
				new PlayerContactGeometry.Uv(0.6790691801525065, 0.721117851829268),
				new PlayerContactGeometry.Uv(0.632712943800569, 0.7701920749396878),
				new PlayerContactGeometry.Uv(0.5750346072096361, 0.8175440697186419),
				new PlayerContactGeometry.Uv(0.5036259486726321, 0.8102231417241581),
				new PlayerContactGeometry.Uv(0.5036259486726321, 0.6097685962696127)
			},
			FrontTriangles = new int[21]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				1
			},
			DepthTriangles = new int[63]
			{
				1, 2, 10, 1, 10, 9, 2, 3, 11, 2,
				11, 10, 3, 4, 12, 3, 12, 11, 4, 5,
				13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
				6, 7, 15, 6, 15, 14, 7, 1, 9, 7,
				9, 15, 8, 10, 9, 8, 11, 10, 8, 12,
				11, 8, 13, 12, 8, 14, 13, 8, 15, 14,
				8, 9, 15
			}
		},
		new Chunk
		{
			Index = 3,
			Center = new PlayerContactGeometry.Point(-0.04612028639469994, 0.11866623356822173, 0.0),
			Front = new PlayerContactGeometry.Point[8]
			{
				new PlayerContactGeometry.Point(-0.04612028639469994, 0.11866623356822173, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.0773123442808607, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.17979614949037373, 0.0),
				new PlayerContactGeometry.Point(-0.031020024021590483, 0.15594819181043437, 0.0),
				new PlayerContactGeometry.Point(-0.059911843363703296, 0.144639984795425, 0.0),
				new PlayerContactGeometry.Point(-0.0826748908479906, 0.12373171807084311, 0.0),
				new PlayerContactGeometry.Point(-0.1043603122584721, 0.10436031225847212, 0.0),
				new PlayerContactGeometry.Point(-0.044874934271143016, 0.044874934271143016, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[16]
			{
				new PlayerContactGeometry.Point(-0.04612028639469994, 0.11866623356822173, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.0773123442808607, 0.0),
				new PlayerContactGeometry.Point(0.0, 0.17979614949037373, 0.0),
				new PlayerContactGeometry.Point(-0.031020024021590483, 0.15594819181043437, 0.0),
				new PlayerContactGeometry.Point(-0.059911843363703296, 0.144639984795425, 0.0),
				new PlayerContactGeometry.Point(-0.0826748908479906, 0.12373171807084311, 0.0),
				new PlayerContactGeometry.Point(-0.1043603122584721, 0.10436031225847212, 0.0),
				new PlayerContactGeometry.Point(-0.044874934271143016, 0.044874934271143016, 0.0),
				new PlayerContactGeometry.Point(-0.04289186634707095, 0.11035959721844621, -0.035),
				new PlayerContactGeometry.Point(0.0, 0.07190048018120046, -0.035),
				new PlayerContactGeometry.Point(0.0, 0.16721041902604758, -0.035),
				new PlayerContactGeometry.Point(-0.02884862234007915, 0.14503181838370396, -0.035),
				new PlayerContactGeometry.Point(-0.05571801432824407, 0.13451518585974526, -0.035),
				new PlayerContactGeometry.Point(-0.07688764848863126, 0.1150704978058841, -0.035),
				new PlayerContactGeometry.Point(-0.09705509040037906, 0.09705509040037907, -0.035),
				new PlayerContactGeometry.Point(-0.041733688872163006, 0.041733688872163006, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[8]
			{
				new PlayerContactGeometry.Uv(0.41341636847034596, 0.6906552794185135),
				new PlayerContactGeometry.Uv(0.5036259486726321, 0.6097685962696127),
				new PlayerContactGeometry.Uv(0.5036259486726321, 0.8102231417241581),
				new PlayerContactGeometry.Uv(0.44295192507909936, 0.7635774155337751),
				new PlayerContactGeometry.Uv(0.3864405913933701, 0.7414589794309636),
				new PlayerContactGeometry.Uv(0.3419169095560813, 0.7005631803927618),
				new PlayerContactGeometry.Uv(0.2995010245980466, 0.6626734246504182),
				new PlayerContactGeometry.Uv(0.4158522313205603, 0.5463222179279045)
			},
			FrontTriangles = new int[21]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				1
			},
			DepthTriangles = new int[63]
			{
				1, 2, 10, 1, 10, 9, 2, 3, 11, 2,
				11, 10, 3, 4, 12, 3, 12, 11, 4, 5,
				13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
				6, 7, 15, 6, 15, 14, 7, 1, 9, 7,
				9, 15, 8, 10, 9, 8, 11, 10, 8, 12,
				11, 8, 13, 12, 8, 14, 13, 8, 15, 14,
				8, 9, 15
			}
		},
		new Chunk
		{
			Index = 4,
			Center = new PlayerContactGeometry.Point(-0.11525832226417242, 0.04518957923159781, 0.0),
			Front = new PlayerContactGeometry.Point[8]
			{
				new PlayerContactGeometry.Point(-0.11525832226417242, 0.04518957923159781, 0.0),
				new PlayerContactGeometry.Point(-0.044874934271143016, 0.044874934271143016, 0.0),
				new PlayerContactGeometry.Point(-0.1043603122584721, 0.10436031225847212, 0.0),
				new PlayerContactGeometry.Point(-0.11864685294464407, 0.0792772925939636, 0.0),
				new PlayerContactGeometry.Point(-0.1367299856269252, 0.05663541442975078, 0.0),
				new PlayerContactGeometry.Point(-0.15674792612741095, 0.031179101067855108, 0.0),
				new PlayerContactGeometry.Point(-0.17164212910532275, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.07380611551528878, -0.0, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[16]
			{
				new PlayerContactGeometry.Point(-0.11525832226417242, 0.04518957923159781, 0.0),
				new PlayerContactGeometry.Point(-0.044874934271143016, 0.044874934271143016, 0.0),
				new PlayerContactGeometry.Point(-0.1043603122584721, 0.10436031225847212, 0.0),
				new PlayerContactGeometry.Point(-0.11864685294464407, 0.0792772925939636, 0.0),
				new PlayerContactGeometry.Point(-0.1367299856269252, 0.05663541442975078, 0.0),
				new PlayerContactGeometry.Point(-0.15674792612741095, 0.031179101067855108, 0.0),
				new PlayerContactGeometry.Point(-0.17164212910532275, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.07380611551528878, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.10719023970568035, 0.04202630868538597, -0.035),
				new PlayerContactGeometry.Point(-0.041733688872163006, 0.041733688872163006, -0.035),
				new PlayerContactGeometry.Point(-0.09705509040037906, 0.09705509040037907, -0.035),
				new PlayerContactGeometry.Point(-0.110341573238519, 0.07372788211238615, -0.035),
				new PlayerContactGeometry.Point(-0.12715888663304045, 0.052670935419668226, -0.035),
				new PlayerContactGeometry.Point(-0.1457755712984922, 0.02899656399310525, -0.035),
				new PlayerContactGeometry.Point(-0.15962718006795018, -0.0, -0.035),
				new PlayerContactGeometry.Point(-0.06863968742921857, -0.0, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[8]
			{
				new PlayerContactGeometry.Uv(0.278184918725653, 0.5469376518727955),
				new PlayerContactGeometry.Uv(0.4158522313205603, 0.5463222179279045),
				new PlayerContactGeometry.Uv(0.2995010245980466, 0.6626734246504182),
				new PlayerContactGeometry.Uv(0.27155707761530284, 0.6136119627423884),
				new PlayerContactGeometry.Uv(0.2361871366298912, 0.569325283628885),
				new PlayerContactGeometry.Uv(0.19703278286875142, 0.5195336730083067),
				new PlayerContactGeometry.Uv(0.16790027084169107, 0.45854850057583274),
				new PlayerContactGeometry.Uv(0.35926390720532747, 0.45854850057583274)
			},
			FrontTriangles = new int[21]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				1
			},
			DepthTriangles = new int[63]
			{
				1, 2, 10, 1, 10, 9, 2, 3, 11, 2,
				11, 10, 3, 4, 12, 3, 12, 11, 4, 5,
				13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
				6, 7, 15, 6, 15, 14, 7, 1, 9, 7,
				9, 15, 8, 10, 9, 8, 11, 10, 8, 12,
				11, 8, 13, 12, 8, 14, 13, 8, 15, 14,
				8, 9, 15
			}
		},
		new Chunk
		{
			Index = 5,
			Center = new PlayerContactGeometry.Point(-0.11925581009263522, -0.04815301907870987, 0.0),
			Front = new PlayerContactGeometry.Point[8]
			{
				new PlayerContactGeometry.Point(-0.11925581009263522, -0.04815301907870987, 0.0),
				new PlayerContactGeometry.Point(-0.07380611551528878, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.17164212910532275, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.15754766044438753, -0.03133817811411964, 0.0),
				new PlayerContactGeometry.Point(-0.140119985270568, -0.058039598258587545, 0.0),
				new PlayerContactGeometry.Point(-0.13254548428958812, -0.08856406115497072, 0.0),
				new PlayerContactGeometry.Point(-0.11127922798831558, -0.11127922798831558, 0.0),
				new PlayerContactGeometry.Point(-0.04785006803497571, -0.04785006803497571, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[16]
			{
				new PlayerContactGeometry.Point(-0.11925581009263522, -0.04815301907870987, 0.0),
				new PlayerContactGeometry.Point(-0.07380611551528878, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.17164212910532275, -0.0, 0.0),
				new PlayerContactGeometry.Point(-0.15754766044438753, -0.03133817811411964, 0.0),
				new PlayerContactGeometry.Point(-0.140119985270568, -0.058039598258587545, 0.0),
				new PlayerContactGeometry.Point(-0.13254548428958812, -0.08856406115497072, 0.0),
				new PlayerContactGeometry.Point(-0.11127922798831558, -0.11127922798831558, 0.0),
				new PlayerContactGeometry.Point(-0.04785006803497571, -0.04785006803497571, 0.0),
				new PlayerContactGeometry.Point(-0.11090790338615077, -0.04478230774320018, -0.035),
				new PlayerContactGeometry.Point(-0.06863968742921857, -0.0, -0.035),
				new PlayerContactGeometry.Point(-0.15962718006795018, -0.0, -0.035),
				new PlayerContactGeometry.Point(-0.1465193242132804, -0.029144505646131264, -0.035),
				new PlayerContactGeometry.Point(-0.13031158630162823, -0.05397682638048642, -0.035),
				new PlayerContactGeometry.Point(-0.12326730038931696, -0.08236457687412277, -0.035),
				new PlayerContactGeometry.Point(-0.10348968202913349, -0.10348968202913349, -0.035),
				new PlayerContactGeometry.Point(-0.04450056327252742, -0.04450056327252742, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[8]
			{
				new PlayerContactGeometry.Uv(0.2703659798798868, 0.3643629701697969),
				new PlayerContactGeometry.Uv(0.35926390720532747, 0.45854850057583274),
				new PlayerContactGeometry.Uv(0.16790027084169107, 0.45854850057583274),
				new PlayerContactGeometry.Uv(0.19546853202281328, 0.3972521793044176),
				new PlayerContactGeometry.Uv(0.229556422281724, 0.3450251857115477),
				new PlayerContactGeometry.Uv(0.2443718670057299, 0.28532046141262357),
				new PlayerContactGeometry.Uv(0.285967880460505, 0.24089043236370555),
				new PlayerContactGeometry.Uv(0.4100329793414174, 0.364955531244618)
			},
			FrontTriangles = new int[21]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				1
			},
			DepthTriangles = new int[63]
			{
				1, 2, 10, 1, 10, 9, 2, 3, 11, 2,
				11, 10, 3, 4, 12, 3, 12, 11, 4, 5,
				13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
				6, 7, 15, 6, 15, 14, 7, 1, 9, 7,
				9, 15, 8, 10, 9, 8, 11, 10, 8, 12,
				11, 8, 13, 12, 8, 14, 13, 8, 15, 14,
				8, 9, 15
			}
		},
		new Chunk
		{
			Index = 6,
			Center = new PlayerContactGeometry.Point(-0.04909854860772477, -0.12113440911481399, 0.0),
			Front = new PlayerContactGeometry.Point[8]
			{
				new PlayerContactGeometry.Point(-0.04909854860772477, -0.12113440911481399, 0.0),
				new PlayerContactGeometry.Point(-0.04785006803497571, -0.04785006803497571, 0.0),
				new PlayerContactGeometry.Point(-0.11127922798831558, -0.11127922798831558, 0.0),
				new PlayerContactGeometry.Point(-0.09150864630846087, -0.13695236739896055, 0.0),
				new PlayerContactGeometry.Point(-0.06131602719254018, -0.14802998443906773, 0.0),
				new PlayerContactGeometry.Point(-0.03173587072978111, -0.159546996236829, 0.0),
				new PlayerContactGeometry.Point(-4.635023961470189E-17, -0.17082672706681762, 0.0),
				new PlayerContactGeometry.Point(0.0, -0.07345549263873158, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[16]
			{
				new PlayerContactGeometry.Point(-0.04909854860772477, -0.12113440911481399, 0.0),
				new PlayerContactGeometry.Point(-0.04785006803497571, -0.04785006803497571, 0.0),
				new PlayerContactGeometry.Point(-0.11127922798831558, -0.11127922798831558, 0.0),
				new PlayerContactGeometry.Point(-0.09150864630846087, -0.13695236739896055, 0.0),
				new PlayerContactGeometry.Point(-0.06131602719254018, -0.14802998443906773, 0.0),
				new PlayerContactGeometry.Point(-0.03173587072978111, -0.159546996236829, 0.0),
				new PlayerContactGeometry.Point(-4.635023961470189E-17, -0.17082672706681762, 0.0),
				new PlayerContactGeometry.Point(0.0, -0.07345549263873158, 0.0),
				new PlayerContactGeometry.Point(-0.04566165020518404, -0.11265500047677701, -0.035),
				new PlayerContactGeometry.Point(-0.04450056327252742, -0.04450056327252742, -0.035),
				new PlayerContactGeometry.Point(-0.10348968202913349, -0.10348968202913349, -0.035),
				new PlayerContactGeometry.Point(-0.08510304106686861, -0.1273657016810333, -0.035),
				new PlayerContactGeometry.Point(-0.05702390528906237, -0.137667885528333, -0.035),
				new PlayerContactGeometry.Point(-0.029514359778696434, -0.14837870650025098, -0.035),
				new PlayerContactGeometry.Point(-4.310572284167276E-17, -0.1588688561721404, -0.035),
				new PlayerContactGeometry.Point(0.0, -0.06831360815402038, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[8]
			{
				new PlayerContactGeometry.Uv(0.40759099735989734, 0.22161406134053951),
				new PlayerContactGeometry.Uv(0.4100329793414174, 0.364955531244618),
				new PlayerContactGeometry.Uv(0.285967880460505, 0.24089043236370555),
				new PlayerContactGeometry.Uv(0.32463840948609357, 0.1906747179839442),
				new PlayerContactGeometry.Uv(0.38369405958213726, 0.16900730737253478),
				new PlayerContactGeometry.Uv(0.44155175530386387, 0.1464804568111685),
				new PlayerContactGeometry.Uv(0.503625948672632, 0.12441771907663024),
				new PlayerContactGeometry.Uv(0.5036259486726321, 0.31487226453117567)
			},
			FrontTriangles = new int[21]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				1
			},
			DepthTriangles = new int[63]
			{
				1, 2, 10, 1, 10, 9, 2, 3, 11, 2,
				11, 10, 3, 4, 12, 3, 12, 11, 4, 5,
				13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
				6, 7, 15, 6, 15, 14, 7, 1, 9, 7,
				9, 15, 8, 10, 9, 8, 11, 10, 8, 12,
				11, 8, 13, 12, 8, 14, 13, 8, 15, 14,
				8, 9, 15
			}
		},
		new Chunk
		{
			Index = 7,
			Center = new PlayerContactGeometry.Point(0.04855608083298809, -0.12010358144776648, 0.0),
			Front = new PlayerContactGeometry.Point[8]
			{
				new PlayerContactGeometry.Point(0.04855608083298809, -0.12010358144776648, 0.0),
				new PlayerContactGeometry.Point(0.0, -0.07345549263873158, 0.0),
				new PlayerContactGeometry.Point(-4.635023961470189E-17, -0.17082672706681762, 0.0),
				new PlayerContactGeometry.Point(0.03141771663725191, -0.15794752760287584, 0.0),
				new PlayerContactGeometry.Point(0.06131602719254013, -0.14802998443906773, 0.0),
				new PlayerContactGeometry.Point(0.08720502185335992, -0.13051153823910852, 0.0),
				new PlayerContactGeometry.Point(0.11185580429913583, -0.11185580429913587, 0.0),
				new PlayerContactGeometry.Point(0.048097995848628394, -0.04809799584862844, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[16]
			{
				new PlayerContactGeometry.Point(0.04855608083298809, -0.12010358144776648, 0.0),
				new PlayerContactGeometry.Point(0.0, -0.07345549263873158, 0.0),
				new PlayerContactGeometry.Point(-4.635023961470189E-17, -0.17082672706681762, 0.0),
				new PlayerContactGeometry.Point(0.03141771663725191, -0.15794752760287584, 0.0),
				new PlayerContactGeometry.Point(0.06131602719254013, -0.14802998443906773, 0.0),
				new PlayerContactGeometry.Point(0.08720502185335992, -0.13051153823910852, 0.0),
				new PlayerContactGeometry.Point(0.11185580429913583, -0.11185580429913587, 0.0),
				new PlayerContactGeometry.Point(0.048097995848628394, -0.04809799584862844, 0.0),
				new PlayerContactGeometry.Point(0.04515715517467893, -0.11169633074642284, -0.035),
				new PlayerContactGeometry.Point(0.0, -0.06831360815402038, -0.035),
				new PlayerContactGeometry.Point(-4.310572284167276E-17, -0.1588688561721404, -0.035),
				new PlayerContactGeometry.Point(0.029218476472644278, -0.14689120067067454, -0.035),
				new PlayerContactGeometry.Point(0.05702390528906232, -0.137667885528333, -0.035),
				new PlayerContactGeometry.Point(0.08110067032362472, -0.12137573056237093, -0.035),
				new PlayerContactGeometry.Point(0.10402589799819632, -0.10402589799819636, -0.035),
				new PlayerContactGeometry.Point(0.04473113613922441, -0.044731136139224456, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[8]
			{
				new PlayerContactGeometry.Uv(0.5985998530132498, 0.22363032226115565),
				new PlayerContactGeometry.Uv(0.5036259486726321, 0.31487226453117567),
				new PlayerContactGeometry.Uv(0.503625948672632, 0.12441771907663024),
				new PlayerContactGeometry.Uv(0.5650778443635177, 0.1496089585030448),
				new PlayerContactGeometry.Uv(0.6235578377631268, 0.16900730737253478),
				new PlayerContactGeometry.Uv(0.6741957570558431, 0.2032727424127705),
				new PlayerContactGeometry.Uv(0.7224117788962209, 0.23976267035224375),
				new PlayerContactGeometry.Uv(0.5977038556687753, 0.3644705935796895)
			},
			FrontTriangles = new int[21]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				1
			},
			DepthTriangles = new int[63]
			{
				1, 2, 10, 1, 10, 9, 2, 3, 11, 2,
				11, 10, 3, 4, 12, 3, 12, 11, 4, 5,
				13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
				6, 7, 15, 6, 15, 14, 7, 1, 9, 7,
				9, 15, 8, 10, 9, 8, 11, 10, 8, 12,
				11, 8, 13, 12, 8, 14, 13, 8, 15, 14,
				8, 9, 15
			}
		},
		new Chunk
		{
			Index = 8,
			Center = new PlayerContactGeometry.Point(0.11870734292588293, -0.048408819873608504, 0.0),
			Front = new PlayerContactGeometry.Point[8]
			{
				new PlayerContactGeometry.Point(0.11870734292588293, -0.048408819873608504, 0.0),
				new PlayerContactGeometry.Point(0.048097995848628394, -0.04809799584862844, 0.0),
				new PlayerContactGeometry.Point(0.11185580429913583, -0.11185580429913587, 0.0),
				new PlayerContactGeometry.Point(0.13085052924752172, -0.08743152840362843, 0.0),
				new PlayerContactGeometry.Point(0.14614665130371066, -0.06053592506540862, 0.0),
				new PlayerContactGeometry.Point(0.1555483246519461, -0.03094048549845831, 0.0),
				new PlayerContactGeometry.Point(0.1667497168742922, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.07170237825594564, -0.0, 0.0)
			},
			Depth = new PlayerContactGeometry.Point[16]
			{
				new PlayerContactGeometry.Point(0.11870734292588293, -0.048408819873608504, 0.0),
				new PlayerContactGeometry.Point(0.048097995848628394, -0.04809799584862844, 0.0),
				new PlayerContactGeometry.Point(0.11185580429913583, -0.11185580429913587, 0.0),
				new PlayerContactGeometry.Point(0.13085052924752172, -0.08743152840362843, 0.0),
				new PlayerContactGeometry.Point(0.14614665130371066, -0.06053592506540862, 0.0),
				new PlayerContactGeometry.Point(0.1555483246519461, -0.03094048549845831, 0.0),
				new PlayerContactGeometry.Point(0.1667497168742922, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.07170237825594564, -0.0, 0.0),
				new PlayerContactGeometry.Point(0.11039782892107114, -0.04502020248245591, -0.035),
				new PlayerContactGeometry.Point(0.04473113613922441, -0.044731136139224456, -0.035),
				new PlayerContactGeometry.Point(0.10402589799819632, -0.10402589799819636, -0.035),
				new PlayerContactGeometry.Point(0.1216909922001952, -0.08131132141537445, -0.035),
				new PlayerContactGeometry.Point(0.1359163857124509, -0.05629841031083002, -0.035),
				new PlayerContactGeometry.Point(0.14465994192630988, -0.02877465151356623, -0.035),
				new PlayerContactGeometry.Point(0.15507723669309173, -0.0, -0.035),
				new PlayerContactGeometry.Point(0.06668321177802945, -0.0, -0.035)
			},
			UV = new PlayerContactGeometry.Uv[8]
			{
				new PlayerContactGeometry.Uv(0.7358131359036143, 0.363862633243748),
				new PlayerContactGeometry.Uv(0.5977038556687753, 0.3644705935796895),
				new PlayerContactGeometry.Uv(0.7224117788962209, 0.23976267035224375),
				new PlayerContactGeometry.Uv(0.7595647607530008, 0.2875356537292886),
				new PlayerContactGeometry.Uv(0.7894834116825038, 0.3401424624915783),
				new PlayerContactGeometry.Uv(0.8078727382076055, 0.3980300514017704),
				new PlayerContactGeometry.Uv(0.8297822485131424, 0.45854850057583274),
				new PlayerContactGeometry.Uv(0.6438731576040515, 0.45854850057583274)
			},
			FrontTriangles = new int[21]
			{
				0, 1, 2, 0, 2, 3, 0, 3, 4, 0,
				4, 5, 0, 5, 6, 0, 6, 7, 0, 7,
				1
			},
			DepthTriangles = new int[63]
			{
				1, 2, 10, 1, 10, 9, 2, 3, 11, 2,
				11, 10, 3, 4, 12, 3, 12, 11, 4, 5,
				13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
				6, 7, 15, 6, 15, 14, 7, 1, 9, 7,
				9, 15, 8, 10, 9, 8, 11, 10, 8, 12,
				11, 8, 13, 12, 8, 14, 13, 8, 15, 14,
				8, 9, 15
			}
		}
	};
}
