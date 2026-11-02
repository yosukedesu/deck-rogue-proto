using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class MapGen
{
	public static readonly IReadOnlyList<IReadOnlyList<string>> ACT_MUST_APPEAR = new IReadOnlyList<string>[3]
	{
		new string[0],
		new string[0],
		new string[1] { "enemy_sludge_berserker" }
	};

	public static readonly IReadOnlyList<int> TURN_ARMOR_PATH_MIN = new int[3] { 0, 0, 1 };

	public static readonly IReadOnlyList<IReadOnlyList<string>> ELITE_POOLS = new IReadOnlyList<string>[3]
	{
		new string[4] { "enemy_elite_sergeant", "enc_elite_sentries", "enemy_elite_gold_raven", "enemy_elite_devourer" },
		new string[4] { "enemy_elite_iron_egg", "enemy_elite_slaver", "enemy_elite_mirror_djinn", "enemy_elite_owl" },
		new string[5] { "enemy_elite_stab_book", "enemy_elite_giant_face", "enemy_elite_doom_chanter", "enemy_elite_deathless", "enemy_elite_husk_3" }
	};

	public const int MAP_ROWS = 16;

	public const int BOSS_ROW = 15;

	public static readonly IReadOnlyList<int> ACT_MAP_ROWS = new int[3] { 16, 15, 14 };

	public static readonly IReadOnlyCollection<int> FORCED_CAMPFIRE_ROWS = new HashSet<int> { 14 };

	public const int TREASURE_ROW = 8;

	private const double ROOM_WEIGHT_SHOP = 0.05;

	private const double ROOM_WEIGHT_WORKSHOP = 0.05;

	private const double ROOM_WEIGHT_EVENT = 0.22;

	private const double ROOM_WEIGHT_CAMPFIRE = 0.08;

	private const int CAMPFIRE_MIN_ROW = 5;

	private const int WORKSHOP_MIN_ROW_ACT1 = 5;

	private const int CAMPFIRE_PATH_MAX = 4;

	private const int WORKSHOP_PATH_MAX = 1;

	public const int ELITE_COUNT = 4;

	private const int SHOP_COUNT = 3;

	private const int ELITE_MIN_ROW = 2;

	private static readonly IReadOnlyList<int> ELITE_MIN_ROW_BY_ACT = new int[3] { 4, 2, 2 };

	private const int ELITE_PATH_MIN = 3;

	private static readonly HashSet<string> PARENT_EXCLUSIVE = new HashSet<string> { "elite", "shop", "workshop", "campfire" };

	private const int MAX_PLACEMENT_TRIES = 5000;

	public const int GRID_COLS = 7;

	private const int PATH_WALKS = 6;

	private const int ANCESTOR_DEPTH = 5;

	private static readonly IReadOnlyList<IReadOnlyList<string>> ACT_POOLS = new IReadOnlyList<string>[3]
	{
		new string[25]
		{
			"enemy_wide_power", "enemy_thorn_squirrel", "enemy_apprentice_colossus", "enemy_cultist", "enemy_slug", "enemy_mud_lump", "enc_probe_pair", "enc_probe_trio", "enc_squirrel_trio", "enc_mud_mudlings",
			"enc_mudling_swarm", "enc_cultist_imp", "enc_thief_pair", "enc_squirrel_probe", "enc_beast_pair", "enc_thief_beast", "enemy_gaping_maw", "enemy_cog_construct", "enemy_vine_walker", "enemy_strangler_serpent",
			"enc_serpent_fruit", "enc_sporecap_fruit", "enc_sporecap_mudlings", "enc_serpent_mudlings", "enc_snapfruit_trio"
		},
		new string[25]
		{
			"enemy_set_wary", "enemy_set_breaker", "enemy_bomber", "enc_probe_trio", "enc_joker_drummer", "enc_bomber_healer", "enc_hexer_shadow", "enc_joker_hexer", "enc_joker_hexer_drummer", "enc_wary_bomber",
			"enc_bomber_drummer", "enemy_whetstone_colossus", "enemy_mimic_jester", "enemy_cinder_imp", "enemy_rock_beetle", "enemy_big_slime", "enc_squire_archer", "enc_raptor_nest", "enemy_maw_hunter", "enemy_bowl_bug",
			"enc_bowlbug_drummer", "enc_imp_jester", "enc_chomper_pair", "enc_scald_gnat_pair", "enc_moss_spawner_slime"
		},
		new string[28]
		{
			"enemy_burrow_worm", "enemy_nemesis_wraith", "enemy_brute", "enemy_moss", "enemy_axe_ogre", "enemy_shell_guard", "enemy_set_breaker", "enemy_devoted_sculptor", "enc_globe_drummer", "enc_sculptor_shadow",
			"enc_wolf_drummer", "enc_hexer_shadow", "enc_breaker_hexer", "enc_axe_drummer", "enc_shell_hexer", "enc_moss_healer", "enc_fang_twins", "enemy_brood_toad", "enc_mourn_beasts", "enemy_sludge_berserker",
			"enc_wolf_hexer_drummer", "enc_mourn_healer", "enc_axe_shadow", "enc_axe_automatons", "enemy_thunder_globe", "enemy_frog_knight", "enc_biting_scrolls_quad", "enc_lost_forgotten"
		}
	};

	private static readonly IReadOnlyList<int> WEAK_ROWS = new int[3] { 3, 2, 2 };

	private static readonly IReadOnlyList<IReadOnlyList<string>> WEAK_POOLS = new IReadOnlyList<string>[3]
	{
		new string[7] { "enemy_probe", "enemy_slug", "enc_probe_pair", "enc_mudling_trio", "enemy_sludge_spider", "enemy_iron_clam", "enc_spider_fruit" },
		new string[5] { "enemy_set_wary", "enemy_rock_beetle", "enemy_mimic_jester", "enc_imp_jester", "enc_beetle_wary" },
		new string[6] { "enemy_shell_guard", "enemy_axe_ogre", "enemy_set_breaker", "enc_axe_shadow", "enemy_devoted_sculptor", "enc_biting_scrolls_trio" }
	};

	public static readonly IReadOnlyList<IReadOnlyList<string>> ACT_BOSS_POOLS = new IReadOnlyList<string>[3]
	{
		new string[3] { "enemy_brute", "enc_kin_ritual", "enemy_haze_stag" },
		new string[3] { "enemy_turtle", "enc_kaiser_crab", "enemy_winch_warden" },
		new string[3] { "enemy_warden", "enemy_chimera_1", "enemy_ember_furnace" }
	};

	public static readonly IReadOnlyList<string> ACT_BOSSES = ACT_BOSS_POOLS.Select((IReadOnlyList<string> p) => p[0]).ToList();

	public const int ACT_COUNT = 3;

	public static int MapRowsFor(int act)
	{
		int i = act - 1;
		if (i < 0 || i >= ACT_MAP_ROWS.Count)
		{
			return 16;
		}
		return ACT_MAP_ROWS[i];
	}

	public static int BossRowFor(int act)
	{
		return MapRowsFor(act) - 1;
	}

	public static int TreasureRowFor(int act)
	{
		if (act <= 1)
		{
			return -1;
		}
		return BossRowFor(act) - 7;
	}

	public static IReadOnlyList<string> TierFor(int act, int row)
	{
		if (row >= BossRowFor(act))
		{
			return ACT_BOSS_POOLS[act - 1];
		}
		int weak = ((act - 1 >= 0 && act - 1 < WEAK_ROWS.Count) ? WEAK_ROWS[act - 1] : 0);
		if (row < weak)
		{
			return WEAK_POOLS[act - 1];
		}
		return ACT_POOLS[act - 1];
	}

	public static (IReadOnlyList<IReadOnlyList<MapNode>> Map, RngState Rng) GenerateMap(RngState rng0, int act = 1, bool allowWorkshop = true, int eliteCount = 4)
	{
		RngState rng1 = rng0;
		for (int attempt = 0; attempt <= 5000; attempt++)
		{
			int mapRows = MapRowsFor(act);
			int bossRow = mapRows - 1;
			int treasureRow = TreasureRowFor(act);
			int forcedCampfireRow = bossRow - 1;
			int walkRows = bossRow;
			List<HashSet<int>> visited = new List<HashSet<int>>();
			for (int i = 0; i < walkRows; i++)
			{
				visited.Add(new HashSet<int>());
			}
			List<Dictionary<int, HashSet<int>>> latEdges = new List<Dictionary<int, HashSet<int>>>();
			for (int j = 0; j < walkRows - 1; j++)
			{
				latEdges.Add(new Dictionary<int, HashSet<int>>());
			}
			List<Dictionary<int, HashSet<int>>> latParents = new List<Dictionary<int, HashSet<int>>>();
			for (int k = 0; k < walkRows; k++)
			{
				latParents.Add(new Dictionary<int, HashSet<int>>());
			}
			List<int> startCols = new List<int>();
			for (int p = 0; p < 6; p++)
			{
				int x;
				do
				{
					(int Value, RngState Next) tuple = Rng.NextInt(rng1, 0, 6);
					int v = tuple.Value;
					rng1 = tuple.Next;
					x = v;
				}
				while (p == 1 && x == startCols[0]);
				startCols.Add(x);
				visited[0].Add(x);
				for (int y = 0; y < walkRows - 1; y++)
				{
					(int Value, RngState Next) tuple2 = Rng.NextInt(rng1, -1, 1);
					int d = tuple2.Value;
					rng1 = tuple2.Next;
					int nx = ClampCol(x + d);
					List<int> otherParents = new List<int>();
					if (latParents[y + 1].TryGetValue(nx, out HashSet<int> pset))
					{
						foreach (int c in pset)
						{
							if (c != x)
							{
								otherParents.Add(c);
							}
						}
					}
					bool anyNear = false;
					foreach (int c2 in otherParents)
					{
						if (NearCommonAncestor(y, x, c2))
						{
							anyNear = true;
							break;
						}
					}
					if (anyNear)
					{
						(int Value, RngState Next) tuple3 = Rng.NextInt(rng1, -1, 1);
						int d2 = tuple3.Value;
						rng1 = tuple3.Next;
						nx = ClampCol(x + d2);
					}
					HashSet<int> left = null;
					if (x > 0)
					{
						latEdges[y].TryGetValue(x - 1, out left);
					}
					if (left != null && left.Count > 0)
					{
						nx = Math.Max(nx, left.Max());
					}
					HashSet<int> right = null;
					if (x < 6)
					{
						latEdges[y].TryGetValue(x + 1, out right);
					}
					if (right != null && right.Count > 0)
					{
						nx = Math.Min(nx, right.Min());
					}
					AddLatEdge(y, x, nx);
					visited[y + 1].Add(nx);
					x = nx;
				}
			}
			for (int l = 0; l < walkRows - 1; l++)
			{
				List<int> list = visited[l].ToList();
				list.Sort();
				foreach (int col in list)
				{
					if (!latEdges[l].TryGetValue(col, out HashSet<int> cur) || cur.Count != 1)
					{
						continue;
					}
					(int Value, RngState Next) tuple4 = Rng.NextInt(rng1, 0, 1);
					int coin = tuple4.Value;
					rng1 = tuple4.Next;
					int baseTo = cur.First();
					int[] array = ((coin != 1) ? new int[2]
					{
						baseTo - 1,
						baseTo + 1
					} : new int[2]
					{
						baseTo + 1,
						baseTo - 1
					});
					foreach (int cand in array)
					{
						if (Math.Abs(cand - col) > 1 || cand < 0 || cand >= 7 || !visited[l + 1].Contains(cand))
						{
							continue;
						}
						bool ok = true;
						foreach (KeyValuePair<int, HashSet<int>> kv in latEdges[l])
						{
							int c3 = kv.Key;
							if (c3 == col)
							{
								continue;
							}
							foreach (int t2 in kv.Value)
							{
								if ((c3 - col) * (t2 - cand) < 0)
								{
									ok = false;
									break;
								}
							}
							if (!ok)
							{
								break;
							}
						}
						if (ok)
						{
							AddLatEdge(l, col, cand);
							break;
						}
					}
				}
			}
			List<List<int>> colsOf = new List<List<int>>();
			foreach (HashSet<int> item in visited)
			{
				List<int> l2 = item.ToList();
				l2.Sort();
				colsOf.Add(l2);
			}
			List<int> widths = new List<int>();
			foreach (List<int> c4 in colsOf)
			{
				widths.Add(c4.Count);
			}
			widths.Add(1);
			List<List<List<int>>> edges = new List<List<List<int>>>();
			int r;
			for (r = 0; r < walkRows - 1; r++)
			{
				List<List<int>> rowEdges = new List<List<int>>();
				foreach (int col2 in colsOf[r])
				{
					List<int> tos = new List<int>();
					if (latEdges[r].TryGetValue(col2, out HashSet<int> set))
					{
						tos = set.ToList();
						tos.Sort();
					}
					rowEdges.Add(tos.Select((int item) => colsOf[r + 1].IndexOf(item)).ToList());
				}
				edges.Add(rowEdges);
			}
			List<List<int>> last = new List<List<int>>();
			foreach (int item2 in colsOf[walkRows - 1])
			{
				_ = item2;
				last.Add(new List<int> { 0 });
			}
			edges.Add(last);
			List<List<List<int>>> parents = new List<List<List<int>>>();
			foreach (int w in widths)
			{
				List<List<int>> row = new List<List<int>>();
				for (int i2 = 0; i2 < w; i2++)
				{
					row.Add(new List<int>());
				}
				parents.Add(row);
			}
			for (int r2 = 0; r2 < mapRows - 1; r2++)
			{
				for (int c5 = 0; c5 < widths[r2]; c5++)
				{
					foreach (int to in edges[r2][c5])
					{
						parents[r2 + 1][to].Add(c5);
					}
				}
			}
			int total = 0;
			foreach (int w2 in widths)
			{
				total += w2;
			}
			List<(string, int)> quota = new List<(string, int)>
			{
				("campfire", JsRound((double)total * 0.08)),
				("workshop", allowWorkshop ? ((act == 1) ? 1 : JsRound((double)total * 0.05)) : 0),
				("shop", 3),
				("event", JsRound((double)total * 0.22))
			};
			List<string[]> typeGrid = new List<string[]>();
			for (int r3 = 0; r3 < widths.Count; r3++)
			{
				string[] row2 = new string[widths[r3]];
				string t3;
				if (r3 == bossRow)
				{
					t3 = "boss";
				}
				else if (r3 == forcedCampfireRow)
				{
					t3 = "campfire";
				}
				else
				{
					t3 = ((r3 == treasureRow) ? "treasure" : "battle");
				}
				for (int c6 = 0; c6 < widths[r3]; c6++)
				{
					row2[c6] = t3;
				}
				typeGrid.Add(row2);
			}
			List<(int, int)> freeNodes = new List<(int, int)>();
			for (int r4 = 1; r4 < bossRow; r4++)
			{
				if (r4 != forcedCampfireRow && r4 != treasureRow)
				{
					for (int c7 = 0; c7 < widths[r4]; c7++)
					{
						freeNodes.Add((r4, c7));
					}
				}
			}
			bool eliteOk = false;
			for (int tryE = 0; tryE < 20; tryE++)
			{
				if (eliteOk)
				{
					break;
				}
				foreach (var (r5, c8) in freeNodes)
				{
					if (typeGrid[r5][c8] == "elite")
					{
						typeGrid[r5][c8] = "battle";
					}
				}
				bool failed = false;
				for (int k2 = 0; k2 < eliteCount; k2++)
				{
					List<(int, int)> cand2 = new List<(int, int)>();
					foreach (var (r6, c9) in freeNodes)
					{
						if (typeGrid[r6][c9] != "battle" || !Assignable(r6, c9, "elite"))
						{
							continue;
						}
						bool everyParentBranches = true;
						foreach (int p2 in parents[r6][c9])
						{
							if (edges[r6 - 1][p2].Count < 2)
							{
								everyParentBranches = false;
								break;
							}
						}
						if (everyParentBranches)
						{
							cand2.Add((r6, c9));
						}
					}
					if (cand2.Count == 0)
					{
						failed = true;
						break;
					}
					int i3;
					(i3, rng1) = Rng.NextInt(rng1, 0, cand2.Count - 1);
					typeGrid[cand2[i3].Item1][cand2[i3].Item2] = "elite";
				}
				eliteOk = !failed && MaxElites() >= 3.0;
			}
			if (!eliteOk)
			{
				continue;
			}
			bool placementFailed = false;
			foreach (var item3 in quota)
			{
				string t4 = item3.Item1;
				int n = item3.Item2;
				for (int k3 = 0; k3 < n; k3++)
				{
					Func<int, int, bool> guard;
					if (t4 == "campfire")
					{
						double[][] F = TypeMaxDP(forward: true, "campfire");
						double[][] B = TypeMaxDP(forward: false, "campfire");
						guard = (int num, int num2) => F[num][num2] + B[num][num2] + 1.0 <= 4.0;
					}
					else if (t4 == "workshop")
					{
						double[][] Fw = TypeMaxDP(forward: true, "workshop");
						double[][] Bw = TypeMaxDP(forward: false, "workshop");
						double[][] Fe = ((act == 1) ? TypeMaxDP(forward: true, "elite") : null);
						double[][] Be = ((act == 1) ? TypeMaxDP(forward: false, "elite") : null);
						guard = (int num, int num2) => Fw[num][num2] + Bw[num][num2] + 1.0 <= 1.0 && (Fe == null || Be == null || Fe[num][num2] + Be[num][num2] >= 1.0);
					}
					else
					{
						guard = (int num2, int num) => true;
					}
					List<(int, int)> cand3 = new List<(int, int)>();
					foreach (var (r7, c10) in freeNodes)
					{
						if (!(typeGrid[r7][c10] != "battle") && Assignable(r7, c10, t4) && guard(r7, c10))
						{
							cand3.Add((r7, c10));
						}
					}
					if (cand3.Count == 0)
					{
						placementFailed = true;
						break;
					}
					int i4;
					(i4, rng1) = Rng.NextInt(rng1, 0, cand3.Count - 1);
					typeGrid[cand3[i4].Item1][cand3[i4].Item2] = t4;
				}
				if (placementFailed)
				{
					break;
				}
			}
			if (placementFailed)
			{
				continue;
			}
			double[][] F2 = TypeMaxDP(forward: true, "shop");
			double[][] B2 = TypeMaxDP(forward: false, "shop");
			bool ok2 = true;
			for (int c11 = 0; c11 < widths[0]; c11++)
			{
				if (F2[0][c11] + B2[0][c11] < 1.0)
				{
					ok2 = false;
					break;
				}
			}
			if (!ok2)
			{
				continue;
			}
			List<List<string>> recentEnemies = new List<List<string>>();
			List<List<MapNode>> map = new List<List<MapNode>>();
			HashSet<string> usedElites = new HashSet<string>();
			HashSet<string> usedInAct = new HashSet<string>();
			for (int r8 = 0; r8 < mapRows; r8++)
			{
				List<MapNode> rowNodes = new List<MapNode>();
				List<string> rowEnemies = new List<string>();
				for (int c12 = 0; c12 < widths[r8]; c12++)
				{
					string type = TypeAt(r8, c12);
					string encounterId = null;
					IReadOnlyList<string> pool;
					if (type == "battle" || type == "elite" || type == "boss")
					{
						IReadOnlyList<string> basePool = TierFor(act, r8);
						if (type == "elite")
						{
							IReadOnlyList<string> all = ELITE_POOLS[act - 1];
							List<string> freshE = all.Where((string id) => !usedElites.Contains(id)).ToList();
							IReadOnlyList<string> readOnlyList;
							if (freshE.Count <= 0)
							{
								readOnlyList = all;
							}
							else
							{
								IReadOnlyList<string> readOnlyList2 = freshE;
								readOnlyList = readOnlyList2;
							}
							pool = readOnlyList;
						}
						else
						{
							pool = basePool;
						}
						List<string> recentIds = new List<string>();
						for (int k4 = Math.Max(0, recentEnemies.Count - 2); k4 < recentEnemies.Count; k4++)
						{
							recentIds.AddRange(recentEnemies[k4]);
						}
						recentIds.AddRange(rowEnemies);
						List<int> myParents = ((r8 > 0) ? parents[r8][c12] : new List<int>());
						List<string> parentIds = new List<string>();
						if (r8 > 0)
						{
							foreach (int p3 in myParents)
							{
								string e = map[r8 - 1][p3].EncounterId;
								if (e != null)
								{
									parentIds.Add(e);
								}
							}
						}
						List<string> ancestorIds = new List<string>(parentIds);
						if (r8 > 1)
						{
							List<int> gps = new List<int>();
							foreach (int p4 in myParents)
							{
								foreach (int g in parents[r8 - 1][p4])
								{
									if (!gps.Contains(g))
									{
										gps.Add(g);
									}
								}
							}
							foreach (int g2 in gps)
							{
								string e2 = map[r8 - 2][g2].EncounterId;
								if (e2 != null)
								{
									ancestorIds.Add(e2);
								}
							}
						}
						List<string> tier = new List<List<string>>
						{
							Avoiding(recentIds),
							Avoiding(ancestorIds),
							Avoiding(parentIds),
							pool.Where((string id) => !parentIds.Contains(id)).ToList()
						}.FirstOrDefault((List<string> list2) => list2.Count > 0);
						List<string> unused = (tier ?? new List<string>()).Where((string id) => type == "elite" || !usedInAct.Contains(id)).ToList();
						IReadOnlyList<string> readOnlyList3;
						if (unused.Count <= 0)
						{
							if (tier == null)
							{
								readOnlyList3 = pool;
							}
							else
							{
								IReadOnlyList<string> readOnlyList2 = tier;
								readOnlyList3 = readOnlyList2;
							}
						}
						else
						{
							IReadOnlyList<string> readOnlyList2 = unused;
							readOnlyList3 = readOnlyList2;
						}
						IReadOnlyList<string> candidates = readOnlyList3;
						(int Value, RngState Next) tuple10 = Rng.NextInt(rng1, 0, candidates.Count - 1);
						int idx = tuple10.Value;
						rng1 = tuple10.Next;
						encounterId = candidates[idx];
						if (type == "elite")
						{
							usedElites.Add(encounterId);
						}
						if (type == "battle")
						{
							usedInAct.Add(encounterId);
						}
						rowEnemies.Add(encounterId);
					}
					MapNode mapNode = new MapNode
					{
						Type = type,
						EncounterId = encounterId,
						Next = (r8 >= mapRows - 1) ? new List<int>() : edges[r8][c12],
						Col = ((r8 == bossRow) ? ((int)Math.Floor(3.5)) : colsOf[r8][c12])
					};
					rowNodes.Add(mapNode);
					List<string> Avoiding(IEnumerable<string> ids)
					{
						HashSet<string> ms = new HashSet<string>();
						foreach (string id in ids)
						{
							foreach (string m4 in MembersOf(id))
							{
								ms.Add(m4);
							}
						}
						return pool.Where((string id) => !MembersOf(id).Any((string item) => ms.Contains(item))).ToList();
					}
				}
				recentEnemies.Add(rowEnemies);
				map.Add(rowNodes);
			}
			IReadOnlyList<string> readOnlyList5;
			if (act - 1 < 0 || act - 1 >= ACT_MUST_APPEAR.Count)
			{
				IReadOnlyList<string> readOnlyList2 = new string[0];
				readOnlyList5 = readOnlyList2;
			}
			else
			{
				readOnlyList5 = ACT_MUST_APPEAR[act - 1];
			}
			foreach (string mustId in readOnlyList5)
			{
				if (map.Any((List<MapNode> source) => source.Any((MapNode mapNode2) => mapNode2.Type == "battle" && mapNode2.EncounterId == mustId)))
				{
					continue;
				}
				List<(int, int)> spots = new List<(int, int)>();
				for (int r9 = ((act - 1 >= 0 && act - 1 < WEAK_ROWS.Count) ? WEAK_ROWS[act - 1] : 0); r9 < bossRow; r9++)
				{
					for (int c13 = 0; c13 < map[r9].Count; c13++)
					{
						if (map[r9][c13].Type == "battle")
						{
							spots.Add((r9, c13));
						}
					}
				}
				if (spots.Count != 0)
				{
					int k5;
					(k5, rng1) = Rng.NextInt(rng1, 0, spots.Count - 1);
					var (rr, cc) = spots[k5];
					map[rr][cc] = map[rr][cc]with
					{
						EncounterId = mustId
					};
				}
			}
			int minBattles = ((act - 1 >= 0 && act - 1 < TURN_ARMOR_PATH_MIN.Count) ? TURN_ARMOR_PATH_MIN[act - 1] : 0);
			int weakRows;
			int last2;
			if (minBattles > 0)
			{
				List<string> vesselPool = ACT_POOLS[act - 1].Where((string id) => IsVessel(id)).ToList();
				weakRows = ((act - 1 >= 0 && act - 1 < WEAK_ROWS.Count) ? WEAK_ROWS[act - 1] : 0);
				last2 = map.Count - 1;
				for (int iter = 0; iter < 80; iter++)
				{
					if (vesselPool.Count <= 0)
					{
						break;
					}
					Func<int, int, bool> vesselNow = (int vr, int vc) => map[vr][vc].Type == "battle" && IsVessel(map[vr][vc].EncounterId);
					List<(int, int)> spots2 = Deficient(vesselNow);
					if (spots2.Count == 0)
					{
						break;
					}
					List<(int, int, int)> scored = new List<(int, int, int)>();
					foreach (var item4 in spots2)
					{
						int rr2 = item4.Item1;
						int cc2 = item4.Item2;
						int rr3 = rr2;
						int cc3 = cc2;
						scored.Add((Deficient((int num, int num2) => (num == rr3 && num2 == cc3) || vesselNow(num, num2)).Count, rr3, cc3));
					}
					int best = scored.Min(((int Score, int R, int C) tuple16) => tuple16.Score);
					List<(int, int)> bestSpots = scored.Where(((int Score, int R, int C) tuple16) => tuple16.Score == best).Select(((int Score, int R, int C) tuple16) => (R: tuple16.R, C: tuple16.C)).ToList();
					(int Value, RngState Next) tuple13 = Rng.NextInt(rng1, 0, bestSpots.Count - 1);
					int k6 = tuple13.Value;
					rng1 = tuple13.Next;
					(int, int) tuple14 = bestSpots[k6];
					int r10 = tuple14.Item1;
					int c14 = tuple14.Item2;
					HashSet<string> usedNow = new HashSet<string>();
					foreach (List<MapNode> item5 in map)
					{
						foreach (MapNode node in item5)
						{
							if (node.Type == "battle" && node.EncounterId != null)
							{
								usedNow.Add(node.EncounterId);
							}
						}
					}
					HashSet<string> near = new HashSet<string>();
					if (r10 - 1 >= 0)
					{
						foreach (MapNode item6 in map[r10 - 1])
						{
							foreach (string m2 in MembersOfN(item6.EncounterId))
							{
								near.Add(m2);
							}
						}
					}
					if (r10 + 1 < map.Count)
					{
						foreach (MapNode item7 in map[r10 + 1])
						{
							foreach (string m3 in MembersOfN(item7.EncounterId))
							{
								near.Add(m3);
							}
						}
					}
					List<string> freshV = vesselPool.Where((string id) => !MembersOfN(id).Any((string item) => near.Contains(item))).ToList();
					List<string> unusedFresh = freshV.Where((string id) => !usedNow.Contains(id)).ToList();
					List<string> unusedAny = vesselPool.Where((string id) => !usedNow.Contains(id)).ToList();
					List<string> cands;
					if (unusedFresh.Count > 0)
					{
						cands = unusedFresh;
					}
					else if (unusedAny.Count > 0)
					{
						cands = unusedAny;
					}
					else
					{
						cands = ((freshV.Count > 0) ? freshV : vesselPool);
					}
					int v5;
					(v5, rng1) = Rng.NextInt(rng1, 0, cands.Count - 1);
					map[r10][c14] = map[r10][c14]with
					{
						EncounterId = cands[v5]
					};
				}
			}
			return (Map: ((IEnumerable<List<MapNode>>)map).Select((Func<List<MapNode>, IReadOnlyList<MapNode>>)((List<MapNode> result) => result)).ToList(), Rng: rng1);
			void AddLatEdge(int num, int from, int num2)
			{
				if (!latEdges[num].TryGetValue(from, out HashSet<int> s))
				{
					s = new HashSet<int>();
					latEdges[num][from] = s;
				}
				s.Add(num2);
				if (!latParents[num + 1].TryGetValue(num2, out HashSet<int> p5))
				{
					p5 = new HashSet<int>();
					latParents[num + 1][num2] = p5;
				}
				p5.Add(from);
			}
			bool Assignable(int num, int num3, string text)
			{
				int eliteMin = ((act - 1 >= 0 && act - 1 < ELITE_MIN_ROW_BY_ACT.Count) ? ELITE_MIN_ROW_BY_ACT[act - 1] : 2);
				if (text == "elite" && num < eliteMin)
				{
					return false;
				}
				if (text == "campfire" && num < 5)
				{
					return false;
				}
				if (text == "workshop" && act == 1 && num < 5)
				{
					return false;
				}
				if (text == "campfire" && num >= bossRow - 3)
				{
					return false;
				}
				for (int num2 = 0; num2 < widths[num]; num2++)
				{
					if (num2 != num3 && !(TypeAt(num, num2) != text))
					{
						foreach (int p5 in parents[num][num2])
						{
							if (parents[num][num3].Contains(p5))
							{
								return false;
							}
						}
					}
				}
				if (PARENT_EXCLUSIVE.Contains(text))
				{
					foreach (int p6 in parents[num][num3])
					{
						if (TypeAt(num - 1, p6) == text)
						{
							return false;
						}
					}
					if (num + 1 < mapRows)
					{
						foreach (int to2 in edges[num][num3])
						{
							if (TypeAt(num + 1, to2) == text)
							{
								return false;
							}
						}
					}
				}
				return true;
			}
			double Battle(int num, int index)
			{
				return (num >= weakRows && num < last2 && map[num][index].Type == "battle") ? 1 : 0;
			}
			List<(int R, int C)> Deficient(Func<int, int, bool> vessel)
			{
				(double[][] F, double[][] B) tuple16 = MaxBattlesDP(vessel);
				double[][] F3 = tuple16.F;
				double[][] B3 = tuple16.B;
				List<(int, int)> outp = new List<(int, int)>();
				for (int num = weakRows; num < last2; num++)
				{
					for (int num2 = 0; num2 < map[num].Count; num2++)
					{
						if (map[num][num2].Type == "battle" && !vessel(num, num2) && !double.IsNegativeInfinity(F3[num][num2]) && !double.IsNegativeInfinity(B3[num][num2]) && F3[num][num2] + B3[num][num2] - 1.0 >= (double)minBattles)
						{
							outp.Add((num, num2));
						}
					}
				}
				return outp;
			}
			(double[][] F, double[][] B) MaxBattlesDP(Func<int, int, bool> vessel)
			{
				double[][] F3 = new double[map.Count][];
				double[][] B3 = new double[map.Count][];
				for (int num = 0; num < map.Count; num++)
				{
					F3[num] = new double[map[num].Count];
					B3[num] = new double[map[num].Count];
					for (int num2 = 0; num2 < map[num].Count; num2++)
					{
						F3[num][num2] = double.NegativeInfinity;
						B3[num][num2] = double.NegativeInfinity;
					}
				}
				for (int num3 = 0; num3 < map[0].Count; num3++)
				{
					if (!vessel(0, num3))
					{
						F3[0][num3] = Battle(0, num3);
					}
				}
				for (int num4 = 0; num4 < last2; num4++)
				{
					for (int num5 = 0; num5 < map[num4].Count; num5++)
					{
						if (!double.IsNegativeInfinity(F3[num4][num5]))
						{
							foreach (int to2 in map[num4][num5].Next)
							{
								if (!vessel(num4 + 1, to2))
								{
									F3[num4 + 1][to2] = Math.Max(F3[num4 + 1][to2], F3[num4][num5] + Battle(num4 + 1, to2));
								}
							}
						}
					}
				}
				for (int num6 = 0; num6 < map[last2].Count; num6++)
				{
					if (!vessel(last2, num6))
					{
						B3[last2][num6] = Battle(last2, num6);
					}
				}
				for (int r11 = last2 - 1; r11 >= 0; r11--)
				{
					for (int num7 = 0; num7 < map[r11].Count; num7++)
					{
						if (!vessel(r11, num7))
						{
							foreach (int to3 in map[r11][num7].Next)
							{
								if (!double.IsNegativeInfinity(B3[r11 + 1][to3]))
								{
									B3[r11][num7] = Math.Max(B3[r11][num7], B3[r11 + 1][to3] + Battle(r11, num7));
								}
							}
						}
					}
				}
				return (F: F3, B: B3);
			}
			double MaxElites()
			{
				double[] maxE = new double[widths[0]];
				for (int num = 0; num < mapRows - 1; num++)
				{
					double[] next2 = new double[widths[num + 1]];
					for (int num2 = 0; num2 < next2.Length; num2++)
					{
						next2[num2] = double.NegativeInfinity;
					}
					for (int num3 = 0; num3 < widths[num]; num3++)
					{
						foreach (int to2 in edges[num][num3])
						{
							double gain = ((TypeAt(num + 1, to2) == "elite") ? 1 : 0);
							next2[to2] = Math.Max(next2[to2], maxE[num3] + gain);
						}
					}
					maxE = next2;
				}
				return maxE[0];
			}
			bool NearCommonAncestor(int num, int a, int b)
			{
				int l3 = Math.Min(a, b);
				int r11 = Math.Max(a, b);
				int row3 = num;
				while (row3 > 0 && row3 > num - 5)
				{
					latParents[row3].TryGetValue(l3, out HashSet<int> lp);
					latParents[row3].TryGetValue(r11, out HashSet<int> rp);
					if (lp == null || lp.Count == 0 || rp == null || rp.Count == 0)
					{
						return false;
					}
					l3 = lp.Max();
					r11 = rp.Min();
					if (l3 == r11)
					{
						return true;
					}
					row3--;
				}
				return false;
			}
			string TypeAt(int index, int num)
			{
				return typeGrid[index][num];
			}
			double[][] TypeMaxDP(bool forward, string text)
			{
				double Gain(int index, int num)
				{
					return (typeGrid[index][num] == text) ? 1 : 0;
				}
				double[][] outp = new double[widths.Count][];
				for (int num = 0; num < widths.Count; num++)
				{
					outp[num] = new double[widths[num]];
					for (int num2 = 0; num2 < widths[num]; num2++)
					{
						outp[num][num2] = double.NegativeInfinity;
					}
				}
				if (forward)
				{
					for (int num3 = 0; num3 < widths[0]; num3++)
					{
						outp[0][num3] = Gain(0, num3);
					}
					for (int num4 = 0; num4 < mapRows - 1; num4++)
					{
						for (int num5 = 0; num5 < widths[num4]; num5++)
						{
							foreach (int to2 in edges[num4][num5])
							{
								outp[num4 + 1][to2] = Math.Max(outp[num4 + 1][to2], outp[num4][num5] + Gain(num4 + 1, to2));
							}
						}
					}
				}
				else
				{
					outp[mapRows - 1][0] = Gain(mapRows - 1, 0);
					for (int r11 = mapRows - 2; r11 >= 0; r11--)
					{
						for (int num6 = 0; num6 < widths[r11]; num6++)
						{
							foreach (int to3 in edges[r11][num6])
							{
								outp[r11][num6] = Math.Max(outp[r11][num6], outp[r11 + 1][to3] + Gain(r11, num6));
							}
						}
					}
				}
				return outp;
			}
		}
		throw new InvalidOperationException("マップ生成が収束しない");
		static int ClampCol(int val)
		{
			return Math.Max(0, Math.Min(6, val));
		}
		static bool IsVessel(string? encId)
		{
			if (encId != null)
			{
				return Content.ResolveEncounter(encId).Any((EncounterMember encounterMember) => Content.GetEnemyDef(encounterMember.EnemyId).TurnArmor.HasValue);
			}
			return false;
		}
		static IReadOnlyList<string> MembersOfN(string? encId)
		{
			if (encId != null)
			{
				return (from encounterMember in Content.ResolveEncounter(encId)
					select encounterMember.EnemyId).ToList();
			}
			return new string[0];
		}
	}

	private static IReadOnlyList<string> MembersOf(string encId)
	{
		return (from m in Content.ResolveEncounter(encId)
			select m.EnemyId).ToList();
	}

	private static int JsRound(double x)
	{
		return (int)Math.Floor(x + 0.5);
	}
}
