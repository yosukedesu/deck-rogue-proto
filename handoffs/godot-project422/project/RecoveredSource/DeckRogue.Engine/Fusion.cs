using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Fusion
{
	public sealed record RecipePair(int IndexA, int IndexB, CardDef Recipe);

	private static readonly HashSet<string> REFILL = new HashSet<string> { "drawCards", "drawCardsPerLight", "drawCardsPerCardPlayed", "dischargeAetherDraw", "impulseDraw", "retrieveFromExhaust", "playFromExhaust", "retrieveZeroCostFromDiscard", "drawTypeFromDeck" };

	private static readonly (string Effect, string Word)[] WORD = new (string, string)[17]
	{
		("applyBurn", "焔"),
		("gainIceBlock", "氷"),
		("negate", "封"),
		("dealDamageDrain", "血"),
		("gainHp", "光"),
		("addGrowth", "蔦"),
		("doubleGrowth", "花"),
		("addMomentum", "角"),
		("gainEnergy", "樹"),
		("gainBlock", "皮"),
		("drawCards", "葉"),
		("gainHp", "露"),
		("counter", "棘"),
		("weakenEnemy", "根"),
		("dealDamage", "牙"),
		("dealDamageRandom", "賭"),
		("impulseDraw", "閃")
	};

	private static readonly Dictionary<string, (string Effect, string Word)[]> COLOR_WORD = new Dictionary<string, (string, string)[]>
	{
		["black"] = new (string, string)[8]
		{
			("dealDamageDrain", "血"),
			("exhaustFromDeck", "墓"),
			("loseHp", "贄"),
			("gainBlock", "骨"),
			("drawCards", "冥"),
			("gainHp", "宵"),
			("dealDamage", "影"),
			("gainEnergy", "燭")
		},
		["blue"] = new (string, string)[7]
		{
			("gainIceBlock", "氷"),
			("negate", "封"),
			("addAether", "霊"),
			("addSpellEcho", "谺"),
			("drawCards", "書"),
			("dealDamage", "潮"),
			("gainEnergy", "魔")
		},
		["red"] = new (string, string)[8]
		{
			("applyBurn", "焔"),
			("dealDamageRandom", "賭"),
			("impulseDraw", "閃"),
			("addMomentum", "烈"),
			("gainEnergy", "儀"),
			("gainBlock", "炭"),
			("drawCards", "燼"),
			("dealDamage", "火")
		},
		["white"] = new (string, string)[34]
		{
			("dischargeLight", "灯"),
			("dischargeLightRally", "点"),
			("dealDamagePerLight", "篭"),
			("dealDamagePerSpark", "火"),
			("gainBlockPerSpark", "火"),
			("addCardToDraw", "種"),
			("doubleLight", "満"),
			("lightCarryHalf", "残"),
			("lightToSparks", "火"),
			("gainEnergy", "芯"),
			("gainEnergyNextTurn", "芯"),
			("addLight", "灯"),
			("addLightNextTurn", "灯"),
			("blessRetainers", "照"),
			("triggerRetainersNow", "点"),
			("triggerRandomRetainer", "点"),
			("duplicateRetainers", "写"),
			("copyRetainer", "写"),
			("copyLastRetainer", "写"),
			("twinNextRetainer", "写"),
			("extendRetainerLife", "継"),
			("extendAllRetainersLife", "継"),
			("persistRetainer", "永"),
			("summonPermanent", "旗"),
			("dealDamagePerPermanent", "列"),
			("gainHp", "光"),
			("weakenEnemy", "威"),
			("dealDamagePerBlock", "壁"),
			("addCardToHand", "火"),
			("addCardToDiscard", "火"),
			("counter", "報"),
			("gainBlock", "盾"),
			("dealDamage", "輝"),
			("drawCards", "典")
		}
	};

	private static readonly Dictionary<string, string> NAME_FALLBACK = new Dictionary<string, string>
	{
		["red"] = "火",
		["blue"] = "水",
		["white"] = "光",
		["black"] = "影"
	};

	private const string PLAIN_NAME_KANJI = "一撃大小";

	private static readonly Dictionary<string, string> COMMON_NAME_KANJI = new Dictionary<string, string>
	{
		["white"] = "灯人形火光盾点",
		["green"] = "蔦樹根角風蔓",
		["red"] = "火業賭熱紅蓮",
		["blue"] = "氷渦流霊気",
		["black"] = "骨刃血死忘却亡骸"
	};

	private static readonly HashSet<string> REACTION_WINDOWS = new HashSet<string> { "onAttackIncoming", "onAttacked", "onEnemyAction", "onEnemyBuffed", "onEnemyDefended" };

	private static readonly HashSet<string> PERM_WINDOWS = new HashSet<string> { "onAttackIncoming", "onAttacked" };

	private static readonly HashSet<string> PLAYCARD_ONLY = new HashSet<string>
	{
		"searchDeck", "retrieveFromDiscard", "upgradeInHand", "upgradeAllInHand", "gainMaxHp", "addCopyToDiscard", "exhaustFromDeckChoose", "retrieveFromExhaust", "playFromExhaust", "gainSetSlot",
		"sacrificeRetainer", "duplicateRetainers", "triggerRetainersNow", "copyRetainer", "extendRetainerLife", "extendAllRetainersLife", "persistRetainer", "twinNextRetainer", "copyLastRetainer"
	};

	private static readonly HashSet<string> DIES_IN_WINDOW = new HashSet<string> { "drawCards", "impulseDraw", "gainEnergy", "addCasts", "scry" };

	private static readonly HashSet<string> DEAD_ON_PERMANENT = new HashSet<string>
	{
		"negate", "growSelf", "momentumCarryHalf", "doubleGrowth", "doubleMomentum", "dischargeGrowth", "dischargeGrowthBlock", "dischargeMomentumDamage", "dischargeMomentumBlock", "dischargeMomentumBurn",
		"dischargeMomentumGrowth", "dischargeMomentumVolley", "dischargeAether", "dischargeAetherDraw", "dischargeBurn", "dischargeLight", "dischargeLightRally", "doubleLight"
	};

	private static readonly Dictionary<string, double> DROP_VP = new Dictionary<string, double>
	{
		["gainEnergy"] = 5.0,
		["drawCards"] = 3.0,
		["impulseDraw"] = 2.0,
		["addCasts"] = 2.5,
		["negate"] = 12.0,
		["doubleGrowth"] = 8.0,
		["doubleMomentum"] = 6.0,
		["growSelf"] = 4.0,
		["searchDeck"] = 6.0,
		["retrieveFromDiscard"] = 5.0,
		["upgradeInHand"] = 6.0,
		["upgradeAllInHand"] = 9.0,
		["gainMaxHp"] = 6.0,
		["addCopyToDiscard"] = 3.0,
		["exhaustFromDeckChoose"] = 3.0,
		["retrieveFromExhaust"] = 5.0,
		["playFromExhaust"] = 8.0,
		["gainSetSlot"] = 6.0,
		["momentumCarryHalf"] = 8.0
	};

	private static readonly HashSet<string> DROP_AMOUNT_SCALED = new HashSet<string> { "gainEnergy", "drawCards", "impulseDraw", "addCasts" };

	private static readonly HashSet<string> QUANTITY = new HashSet<string> { "dealDamage", "counter", "gainBlock", "gainIceBlock", "gainHp", "applyBurn", "dealDamageDrain" };

	private static readonly Dictionary<string, double> FLAT_VP = new Dictionary<string, double>
	{
		["negate"] = 12.0,
		["shatterBlock"] = 4.0,
		["shatterBlockConvert"] = 10.0
	};

	private static readonly Dictionary<string, DeclarativeEffect> AXIS_BONUS = new Dictionary<string, DeclarativeEffect>
	{
		["growth"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "addGrowth",
			Amount = 1
		},
		["trample"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "addMomentum",
			Amount = 2
		},
		["ramp"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "discountNext",
			Amount = 1
		},
		["burn"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "applyBurn",
			Amount = 2
		},
		["ice"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "gainIceBlock",
			Amount = 2
		},
		["aether"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "addAether",
			Amount = 1
		},
		["light"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "addLight",
			Amount = 1
		},
		["spark"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "addCardToHand",
			SummonId = "white_spark_token",
			Amount = 1
		},
		["storm"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "addCasts",
			Amount = 1
		},
		["heal"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "gainHp",
			Amount = 2
		},
		["fortress"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "gainBlock",
			Amount = 3
		},
		["retinue"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "gainBlock",
			Amount = 2
		},
		["graveyard"] = new DeclarativeEffect
		{
			Trigger = "onPlay",
			Effect = "exhaustFromDeck",
			Amount = 1
		}
	};

	private static readonly Dictionary<string, string> PERM_SUFFIX = new Dictionary<string, string>
	{
		["red"] = "炉",
		["blue"] = "泉",
		["white"] = "祭壇",
		["black"] = "柩"
	};

	private static readonly HashSet<string> AOE_CATALYST_OK = new HashSet<string>
	{
		"dealDamage", "dealDamageRandom", "dealDamageDrain", "dealDamageExecute", "dealDamagePerMomentum", "dealDamagePerEnergyMax", "dealDamagePerAttackPlayed", "dealDamagePerHandCard", "dealDamagePerExhaust", "dealDamagePerSelfHpLost",
		"dealDamagePerPermanent", "dealDamagePerLight", "dealDamagePerSpark", "dealDamagePerHeal", "dealDamagePerWeak", "dealDamagePerCardPlayed", "dealDamagePerCardPlayedTotal", "dealDamagePerRandomPlayed"
	};

	private static readonly Dictionary<string, string> AXIS_JA = new Dictionary<string, string>
	{
		["growth"] = "成長+1",
		["trample"] = "勢い+2",
		["ramp"] = "次のカード-1",
		["burn"] = "延焼+2",
		["ice"] = "氷壁+2",
		["aether"] = "霊気+1",
		["light"] = "灯+1",
		["spark"] = "火種1",
		["storm"] = "詠唱+1",
		["heal"] = "回復+2",
		["fortress"] = "ブロック+3",
		["retinue"] = "ブロック+2",
		["graveyard"] = "ミル1"
	};

	private static IReadOnlyList<FusionRecipe> RECIPES => Content.AllFusions;

	private static (string Word, int Rank) WordRankOf(CardDef def)
	{
		string color = def.Color ?? "";
		(string, string)[] table = (COLOR_WORD.TryGetValue(color, out (string, string)[] t) ? t : WORD);
		for (int i = 0; i < table.Length; i++)
		{
			string eff = table[i].Item1;
			if (def.Effects.Any((DeclarativeEffect e) => e.Effect == eff))
			{
				return (Word: table[i].Item2, Rank: i);
			}
		}
		string f;
		return (Word: NAME_FALLBACK.TryGetValue(color, out f) ? f : "樹", Rank: table.Length);
	}

	private static string WordOf(CardDef def)
	{
		return WordRankOf(def).Word;
	}

	private static bool IsKanji(char ch)
	{
		if (ch < '㐀' || ch > '䶿')
		{
			if (ch >= '一')
			{
				return ch <= '鿿';
			}
			return false;
		}
		return true;
	}

	private static string NameKanji(CardDef def)
	{
		string common = "一撃大小" + (COMMON_NAME_KANJI.TryGetValue(def.Color ?? "", out string c) ? c : "");
		string name = def.Name;
		if (name.StartsWith("真・", StringComparison.Ordinal))
		{
			name = name.Substring(2);
		}
		if (name.EndsWith("+", StringComparison.Ordinal))
		{
			name = name.Substring(0, name.Length - 1);
		}
		string[] parts = name.Split('の');
		for (int p = parts.Length - 1; p >= 0; p--)
		{
			string text = parts[p];
			for (int i = 0; i < text.Length; i++)
			{
				char ch = text[i];
				if (IsKanji(ch) && common.IndexOf(ch) < 0)
				{
					return ch.ToString();
				}
			}
		}
		for (int p2 = parts.Length - 1; p2 >= 0; p2--)
		{
			string text = parts[p2];
			for (int i = 0; i < text.Length; i++)
			{
				char ch2 = text[i];
				if (IsKanji(ch2))
				{
					return ch2.ToString();
				}
			}
		}
		return "";
	}

	private static string ComputedStem(CardDef a, CardDef b, string suffix)
	{
		(string Word, int Rank) tuple = WordRankOf(a);
		string wa = tuple.Word;
		int item = tuple.Rank;
		(string Word, int Rank) tuple2 = WordRankOf(b);
		string wb = tuple2.Word;
		int rb = tuple2.Rank;
		bool same = wa == wb;
		string stem = (same ? wa : (wa + wb));
		int want = ((!same) ? 1 : 2);
		int added = 0;
		CardDef[] array = ((item < rb) ? new CardDef[2] { b, a } : new CardDef[2] { a, b });
		foreach (CardDef d in array)
		{
			if (added >= want)
			{
				break;
			}
			string k = NameKanji(d);
			if (k.Length > 0 && stem.IndexOf(k, StringComparison.Ordinal) < 0 && suffix.IndexOf(k, StringComparison.Ordinal) < 0)
			{
				stem += k;
				added++;
			}
		}
		if (!same || added != 0)
		{
			return stem;
		}
		return "大" + stem;
	}

	private static string SuffixOf(IReadOnlyList<DeclarativeEffect> effects)
	{
		List<DeclarativeEffect> dmgs = effects.Where((DeclarativeEffect e) => e.Effect == "dealDamage" || e.Effect == "dealDamageRandom").ToList();
		bool blk = effects.Any((DeclarativeEffect e) => e.Effect == "gainBlock" || e.Effect == "gainIceBlock");
		if (dmgs.Any((DeclarativeEffect e) => e.Target == "all"))
		{
			return "嵐";
		}
		if (dmgs.Count >= 2)
		{
			return "乱撃";
		}
		if (dmgs.Any((DeclarativeEffect e) => e.Pierce == true))
		{
			return "穿ち";
		}
		if ((dmgs.Count > 0) & blk)
		{
			return "構え";
		}
		if (blk)
		{
			return "盾";
		}
		if (dmgs.Count > 0)
		{
			return "一撃";
		}
		if (effects.Any((DeclarativeEffect e) => e.Effect == "applyBurn"))
		{
			return "熾火";
		}
		return "祝福";
	}

	public static List<RecipePair> RecipePairsInDeck(IReadOnlyList<CardInstance> deck)
	{
		List<RecipePair> out_ = new List<RecipePair>();
		for (int i = 0; i < deck.Count; i++)
		{
			for (int j = i + 1; j < deck.Count; j++)
			{
				if (!(deck[i].Def.Id == deck[j].Def.Id))
				{
					CardDef r = RecipeFor(deck[i].Def, deck[j].Def);
					if (r != null)
					{
						out_.Add(new RecipePair(i, j, r));
					}
				}
			}
		}
		return out_;
	}

	private static CardDef? RecipeFor(CardDef a, CardDef b)
	{
		foreach (FusionRecipe r in RECIPES)
		{
			if ((r.A == a.Id && r.B == b.Id) || (r.A == b.Id && r.B == a.Id))
			{
				return r.Result;
			}
		}
		return null;
	}

	private static int? TypeRank(string type)
	{
		return type switch
		{
			"permanent" => (int?)3, 
			"reaction" => 2, 
			"spell" => 1, 
			"physical" => 0, 
			_ => null, 
		};
	}

	private static bool RankGte(string x, string y)
	{
		int? rx = TypeRank(x);
		int? ry = TypeRank(y);
		if (!rx.HasValue || !ry.HasValue)
		{
			return false;
		}
		return rx.Value >= ry.Value;
	}

	private static bool RankLte(string x, string y)
	{
		int? rx = TypeRank(x);
		int? ry = TypeRank(y);
		if (!rx.HasValue || !ry.HasValue)
		{
			return false;
		}
		return rx.Value <= ry.Value;
	}

	private static (CardInstance Dom, CardInstance Sub) Dominance(CardInstance a, CardInstance b)
	{
		if (!RankGte(a.Def.Type, b.Def.Type))
		{
			return (Dom: b, Sub: a);
		}
		return (Dom: a, Sub: b);
	}

	private static int JsRound(double v)
	{
		return (int)Math.Floor(v + 0.5);
	}

	private static bool SameCondition(EffectCondition? x, EffectCondition? y)
	{
		return object.Equals(x, y);
	}

	private static CardDef MergeFusion(CardInstance x, CardInstance y)
	{
		CardInstance a0;
		CardInstance b0;
		if (string.CompareOrdinal(x.Def.Id, y.Def.Id) <= 0)
		{
			a0 = x;
			b0 = y;
		}
		else
		{
			a0 = y;
			b0 = x;
		}
		bool sameName = a0.Def.Id == b0.Def.Id;
		bool bothX = a0.Def.XCost == true && b0.Def.XCost == true;
		bool catalystX = a0.Def.XCost == true != (b0.Def.XCost == true) && ((a0.Def.XCost == true) ? b0 : a0).Def.FusionCatalyst != null;
		bool keepX = bothX | catalystX;
		CardInstance a1 = Materialize(a0);
		CardInstance b1 = Materialize(b0);
		(CardInstance Dom, CardInstance Sub) tuple = Dominance(a1, b1);
		CardInstance domi0 = tuple.Dom;
		CardInstance sub0 = tuple.Sub;
		int ca = CostAsMaterial(a1.Def);
		int cb = CostAsMaterial(b1.Def);
		int rawSumUncapped = ((ca == 0 || cb == 0) ? Math.Max(ca, cb) : Math.Max(1, ca + cb - 1));
		int rawSum = Math.Min(5, rawSumUncapped);
		CardInstance domi1 = domi0;
		CardInstance sub1 = sub0;
		if (domi0.Def.Type == "reaction" && sub0.Def.Type != "reaction" && !keepX && rawSum > 2)
		{
			domi1 = sub0;
			sub1 = domi0;
		}
		string resultType = domi1.Def.Type;
		bool keepModes = RankLte(resultType, "spell");
		string primaryWindow = ((!(resultType == "reaction")) ? "onPlay" : (domi1.Def.Effects.FirstOrDefault((DeclarativeEffect declarativeEffect) => REACTION_WINDOWS.Contains(declarativeEffect.Trigger))?.Trigger ?? "onAttacked"));
		double droppedVp = 0.0;
		List<List<DeclarativeEffect>> blocks = new List<List<DeclarativeEffect>>
		{
			ConvertAll(domi1),
			ConvertAll(sub1)
		};
		List<List<DeclarativeEffect>> ordered = ((blocks[1].Count > 0 && !HasDamage(blocks[1]) && HasDamage(blocks[0])) ? new List<List<DeclarativeEffect>>
		{
			blocks[1],
			blocks[0]
		} : blocks);
		List<DeclarativeEffect> merged = new List<DeclarativeEffect>();
		List<int> ownerOf = new List<int>();
		double collapsedFlatVp = 0.0;
		int dmgSeen = 0;
		for (int block = 0; block < ordered.Count; block++)
		{
			List<DeclarativeEffect> list = ordered[block];
			bool canMerge = !HasDamage(list);
			foreach (DeclarativeEffect raw in list)
			{
				if (raw.Effect == "dealDamage" && raw.Trigger != "onTurnStart")
				{
					bool consumed = false;
					if (sameName && block == 1)
					{
						List<(DeclarativeEffect, int)> rows = new List<(DeclarativeEffect, int)>();
						for (int k = 0; k < merged.Count; k++)
						{
							if (merged[k].Effect == "dealDamage" && merged[k].Trigger == raw.Trigger)
							{
								rows.Add((merged[k], k));
							}
						}
						int di = dmgSeen++;
						if (di >= 0 && di < rows.Count)
						{
							(DeclarativeEffect, int) hit = rows[di];
							if (hit.Item1.Pierce == raw.Pierce && hit.Item1.Target == raw.Target && hit.Item1.Amount.HasValue && raw.Amount.HasValue)
							{
								merged[hit.Item2] = hit.Item1 with
								{
									Amount = hit.Item1.Amount + raw.Amount
								};
								consumed = true;
							}
						}
					}
					if (!consumed)
					{
						merged.Add(raw);
						ownerOf.Add(block);
					}
					continue;
				}
				int t = ((canMerge || (sameName && block == 1 && raw.Effect.StartsWith("discharge"))) ? TwinOf(raw, block) : (-1));
				if (t >= 0 && merged[t].Amount.HasValue && raw.Amount.HasValue)
				{
					DeclarativeEffect n = merged[t]with
					{
						Amount = merged[t].Amount.GetValueOrDefault() + raw.Amount
					};
					if (merged[t].AmountMax.HasValue && raw.AmountMax.HasValue)
					{
						n = n with
						{
							AmountMax = merged[t].AmountMax.GetValueOrDefault() + raw.AmountMax
						};
					}
					merged[t] = n;
				}
				else if (t >= 0 && !merged[t].Amount.HasValue && !raw.Amount.HasValue)
				{
					collapsedFlatVp += (FLAT_VP.TryGetValue(raw.Effect, out var fv) ? fv : 0.0);
				}
				else
				{
					merged.Add(raw);
					ownerOf.Add(block);
				}
			}
		}
		IReadOnlyList<string> axesB = Run.AxesOf(b1.Def);
		string sharedAxis = Run.AxesOf(a1.Def).FirstOrDefault((string ax) => axesB.Contains(ax) && AXIS_BONUS.ContainsKey(ax));
		if (sharedAxis != null)
		{
			DeclarativeEffect bonus = AXIS_BONUS[sharedAxis]with
			{
				Trigger = ((resultType == "reaction") ? primaryWindow : "onPlay")
			};
			int k2 = -1;
			for (int k3 = 0; k3 < merged.Count; k3++)
			{
				DeclarativeEffect m = merged[k3];
				if (m.Trigger == bonus.Trigger && m.Effect == bonus.Effect && m.Condition == null && m.Target == null && m.Amount.HasValue)
				{
					k2 = k3;
					break;
				}
			}
			if (k2 >= 0)
			{
				merged[k2] = merged[k2]with
				{
					Amount = merged[k2].Amount.GetValueOrDefault() + bonus.Amount.GetValueOrDefault()
				};
			}
			else
			{
				merged.Insert(0, bonus);
				ownerOf.Insert(0, -1);
			}
		}
		List<DeclarativeEffect> effects = new List<DeclarativeEffect>();
		effects.AddRange(merged.Where(IsShatter));
		effects.AddRange(merged.Where((DeclarativeEffect e2) => !IsShatter(e2)));
		double unpaidVp = collapsedFlatVp + droppedVp;
		int costCut = 0;
		if (unpaidVp > 0.0)
		{
			if (resultType == "permanent")
			{
				int onPlayQ = -1;
				for (int i = 0; i < effects.Count; i++)
				{
					if (effects[i].Trigger == "onPlay" && QUANTITY.Contains(effects[i].Effect) && effects[i].Amount.HasValue)
					{
						onPlayQ = i;
						break;
					}
				}
				if (onPlayQ >= 0)
				{
					effects[onPlayQ] = effects[onPlayQ]with
					{
						Amount = effects[onPlayQ].Amount.GetValueOrDefault() + JsRound(unpaidVp)
					};
					unpaidVp = 0.0;
				}
				else
				{
					costCut = Math.Min((!keepX) ? Math.Max(0, rawSum - Math.Max(ca, cb)) : 0, (int)Math.Floor(unpaidVp / 6.0));
					unpaidVp -= (double)(costCut * 6);
					if (unpaidVp >= 3.0)
					{
						effects.Add(new DeclarativeEffect
						{
							Trigger = "onPlay",
							Effect = "gainBlock",
							Amount = JsRound(unpaidVp)
						});
					}
					unpaidVp = 0.0;
				}
			}
			else if (effects.Any((DeclarativeEffect declarativeEffect) => QUANTITY.Contains(declarativeEffect.Effect) && declarativeEffect.Amount.HasValue))
			{
				BoostLargest(effects, JsRound(unpaidVp));
				unpaidVp = 0.0;
			}
		}
		if (!keepX && rawSumUncapped > 5)
		{
			double ratio = 5.0 / (double)rawSumUncapped;
			for (int i2 = 0; i2 < effects.Count; i2++)
			{
				if (QUANTITY.Contains(effects[i2].Effect) && effects[i2].Amount.HasValue)
				{
					effects[i2] = effects[i2]with
					{
						Amount = Math.Max(1, (int)Math.Floor((double)effects[i2].Amount.GetValueOrDefault() * ratio))
					};
				}
			}
		}
		IReadOnlyList<CardMode> modes = null;
		if (keepModes && ((domi1.Def.Modes != null && domi1.Def.Modes.Count > 0) || (sub1.Def.Modes != null && sub1.Def.Modes.Count > 0)))
		{
			if (sameName && a1.Def.Modes != null)
			{
				List<CardMode> list2 = new List<CardMode>();
				for (int i3 = 0; i3 < a1.Def.Modes.Count; i3++)
				{
					CardMode m2 = a1.Def.Modes[i3];
					List<DeclarativeEffect> eff = new List<DeclarativeEffect>();
					for (int k4 = 0; k4 < m2.Effects.Count; k4++)
					{
						DeclarativeEffect e = m2.Effects[k4];
						DeclarativeEffect o = null;
						if (b1.Def.Modes != null && i3 < b1.Def.Modes.Count && k4 < b1.Def.Modes[i3].Effects.Count)
						{
							o = b1.Def.Modes[i3].Effects[k4];
						}
						eff.Add((o != null && o.Effect == e.Effect && e.Amount.HasValue && o.Amount.HasValue) ? e with
						{
							Amount = e.Amount + o.Amount
						} : e);
					}
					list2.Add(m2 with
					{
						Effects = eff
					});
				}
				modes = list2;
			}
			else
			{
				List<CardMode> list3 = new List<CardMode>();
				if (domi1.Def.Modes != null)
				{
					list3.AddRange(domi1.Def.Modes);
				}
				if (sub1.Def.Modes != null)
				{
					list3.AddRange(sub1.Def.Modes);
				}
				modes = list3;
			}
		}
		int cost = (keepX ? 1 : rawSum);
		if (resultType == "reaction" && !keepX && cost > 2)
		{
			for (int cut = cost - 2; cut > 0; cut--)
			{
				BoostLargest(effects, -6);
			}
			cost = 2;
		}
		if (unpaidVp >= 6.0 && !keepX)
		{
			cost = Math.Max(Math.Max(ca, cb), cost - (int)Math.Floor(unpaidVp / 6.0));
		}
		if (costCut > 0 && !keepX)
		{
			cost = Math.Max(Math.Max(ca, cb), cost - costCut);
		}
		string[] catalysts = new string[2]
		{
			a1.Def.FusionCatalyst,
			b1.Def.FusionCatalyst
		};
		int cheaper = catalysts.Count((string c) => c == "cheaper");
		int xBonus = a1.Def.XBonus.GetValueOrDefault() + b1.Def.XBonus.GetValueOrDefault();
		if (cheaper > 0 && !keepX)
		{
			cost = Math.Max(0, cost - cheaper);
		}
		if ((cheaper > 0) & keepX)
		{
			xBonus += cheaper;
		}
		if (Enumerable.Contains(catalysts, "aoe"))
		{
			for (int i4 = 0; i4 < effects.Count; i4++)
			{
				effects[i4] = ToAll(effects[i4]);
			}
			if (modes != null)
			{
				modes = modes.Select((CardMode cardMode) => cardMode with
				{
					Effects = cardMode.Effects.Select(ToAll).ToList()
				}).ToList();
			}
		}
		List<DeclarativeEffect> all = new List<DeclarativeEffect>(effects);
		if (modes != null)
		{
			foreach (CardMode m3 in modes)
			{
				all.AddRange(m3.Effects);
			}
		}
		bool exhaust = (a1.Def.Exhaust == true && Contributed(a1)) || (b1.Def.Exhaust == true && Contributed(b1));
		int? necroCost = ((a1.Def.NecroCost.HasValue || b1.Def.NecroCost.HasValue) ? new int?(Math.Min(a1.Def.NecroCost ?? 99, b1.Def.NecroCost ?? 99)) : ((int?)null));
		if (necroCost.HasValue)
		{
			exhaust = true;
		}
		if (all.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "doubleGrowth" || declarativeEffect.Effect == "doubleMomentum" || declarativeEffect.Effect == "doubleLight"))
		{
			exhaust = true;
		}
		if (effects.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "gainEnergyMax"))
		{
			exhaust = true;
		}
		if (all.Where((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "impulseDraw").Aggregate(0, (int acc, DeclarativeEffect declarativeEffect) => acc + declarativeEffect.Amount.GetValueOrDefault()) >= 4)
		{
			exhaust = true;
		}
		int net = all.Where((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "gainEnergy" || declarativeEffect.Effect == "discountNext").Aggregate(0, (int acc, DeclarativeEffect declarativeEffect) => acc + declarativeEffect.Amount.GetValueOrDefault());
		bool refills = all.Any((DeclarativeEffect declarativeEffect) => REFILL.Contains(declarativeEffect.Effect));
		bool freeIfPhysical = a1.Def.FreeIfHandAllPhysical == true || b1.Def.FreeIfHandAllPhysical == true;
		string freeIfHandAll = a1.Def.FreeIfHandAll ?? b1.Def.FreeIfHandAll ?? (freeIfPhysical ? "physical" : null);
		List<int> freeIfMomentum = new List<int>();
		if (a1.Def.FreeIfMomentumAtLeast.HasValue)
		{
			freeIfMomentum.Add(a1.Def.FreeIfMomentumAtLeast.Value);
		}
		if (b1.Def.FreeIfMomentumAtLeast.HasValue)
		{
			freeIfMomentum.Add(b1.Def.FreeIfMomentumAtLeast.Value);
		}
		bool conditionalFree = freeIfHandAll != null || freeIfMomentum.Count > 0;
		int fusedLightCost = a1.Def.LightCost.GetValueOrDefault() + b1.Def.LightCost.GetValueOrDefault();
		if ((!keepX & refills) && fusedLightCost == 0 && ((net - cost >= 0) | conditionalFree))
		{
			if (resultType != "permanent")
			{
				exhaust = true;
			}
			else
			{
				for (; net - cost >= 0 && cost < 5; cost++)
				{
				}
			}
		}
		if (resultType == "permanent")
		{
			exhaust = false;
		}
		bool dollResult = (a1.Def.Retainer == true || b1.Def.Retainer == true) && resultType == "permanent";
		string suffix;
		if (resultType == "permanent")
		{
			if (dollResult)
			{
				suffix = "人形";
			}
			else
			{
				suffix = (PERM_SUFFIX.TryGetValue(a1.Def.Color ?? "", out string ps) ? ps : "大樹");
			}
		}
		else
		{
			suffix = ((resultType == "reaction") ? "罠" : SuffixOf(effects));
		}
		string stem1;
		if (WorkshopMade(a1.Def) || WorkshopMade(b1.Def))
		{
			string wa = StemOf(a1.Def);
			string wb = StemOf(b1.Def);
			string uniq = UniqueCodePoints(wa, wb);
			string stem0 = ((wa == wb || uniq == wa || uniq == wb) ? ("大" + uniq) : uniq);
			stem1 = ((stem0.Length > 4) ? stem0.Substring(0, 4) : stem0);
		}
		else
		{
			stem1 = ComputedStem(a1.Def, b1.Def, suffix);
		}
		string name = (sameName ? ("真・" + a1.Def.Name) : (stem1 + "の" + suffix));
		string[] ids = new string[2]
		{
			a0.Def.Id,
			b0.Def.Id
		};
		return new CardDef
		{
			Id = "fused_" + ids[0] + "__" + ids[1],
			Name = name,
			Cost = cost,
			Type = resultType,
			Color = a1.Def.Color,
			Effects = effects,
			Modes = modes,
			XCost = (keepX ? new bool?(true) : ((bool?)null)),
			XBonus = ((keepX && xBonus > 0) ? new int?(xBonus) : ((int?)null)),
			Exhaust = (exhaust ? new bool?(true) : ((bool?)null)),
			Retain = (((a1.Def.Retain == true || b1.Def.Retain == true || Enumerable.Contains(catalysts, "retain")) && resultType != "permanent") ? new bool?(true) : ((bool?)null)),
			Echo = (((a1.Def.Echo == true || b1.Def.Echo == true || Enumerable.Contains(catalysts, "echo")) && resultType != "reaction") ? new bool?(true) : ((bool?)null)),
			Cascade = (((a1.Def.Cascade.GetValueOrDefault() > 0 || b1.Def.Cascade.GetValueOrDefault() > 0) && resultType != "reaction") ? new int?(Math.Max(a1.Def.Cascade.GetValueOrDefault(), b1.Def.Cascade.GetValueOrDefault())) : ((int?)null)),
			DiscardCost = ((a1.Def.DiscardCost.GetValueOrDefault() != 0 || b1.Def.DiscardCost.GetValueOrDefault() != 0) ? new int?(a1.Def.DiscardCost.GetValueOrDefault() + b1.Def.DiscardCost.GetValueOrDefault()) : ((int?)null)),
			ExhaustCost = ((a1.Def.ExhaustCost.GetValueOrDefault() != 0 || b1.Def.ExhaustCost.GetValueOrDefault() != 0) ? new int?(a1.Def.ExhaustCost.GetValueOrDefault() + b1.Def.ExhaustCost.GetValueOrDefault()) : ((int?)null)),
			ImpulseCost = ((a1.Def.ImpulseCost.GetValueOrDefault() != 0 || b1.Def.ImpulseCost.GetValueOrDefault() != 0) ? new int?(a1.Def.ImpulseCost.GetValueOrDefault() + b1.Def.ImpulseCost.GetValueOrDefault()) : ((int?)null)),
			LightCost = ((a1.Def.LightCost.GetValueOrDefault() != 0 || b1.Def.LightCost.GetValueOrDefault() != 0) ? new int?(a1.Def.LightCost.GetValueOrDefault() + b1.Def.LightCost.GetValueOrDefault()) : ((int?)null)),
			Retainer = (((a1.Def.Retainer == true || b1.Def.Retainer == true) && resultType == "permanent") ? new bool?(true) : ((bool?)null)),
			LifePersist = (((a1.Def.Retainer == true || b1.Def.Retainer == true) && resultType == "permanent" && (a1.Def.LifePersist == true || b1.Def.LifePersist == true)) ? new bool?(true) : ((bool?)null)),
			Life = (((a1.Def.Retainer == true || b1.Def.Retainer == true) && resultType == "permanent" && a1.Def.LifePersist != true && b1.Def.LifePersist != true) ? new int?(Math.Max((a1.Def.Retainer == true) ? (a1.Def.Life ?? 3) : 0, (b1.Def.Retainer == true) ? (b1.Def.Life ?? 3) : 0)) : ((int?)null)),
			NecroCost = necroCost,
			FreeIfHandAllPhysical = (freeIfPhysical ? new bool?(true) : ((bool?)null)),
			FreeIfHandAll = freeIfHandAll,
			RequiresRetainer = ((a1.Def.RequiresRetainer == true || b1.Def.RequiresRetainer == true) ? new bool?(true) : ((bool?)null)),
			FreeIfMomentumAtLeast = ((freeIfMomentum.Count > 0) ? new int?(freeIfMomentum.Min()) : ((int?)null)),
			BlazeDiscount = ((a1.Def.BlazeDiscount.HasValue || b1.Def.BlazeDiscount.HasValue) ? new int?(Math.Max(a1.Def.BlazeDiscount.GetValueOrDefault(), b1.Def.BlazeDiscount.GetValueOrDefault())) : ((int?)null)),
			ExhaustUnlessExposedEnemy = ((a1.Def.ExhaustUnlessExposedEnemy == true || b1.Def.ExhaustUnlessExposedEnemy == true) ? new bool?(true) : ((bool?)null)),
			Axis = ((a1.Def.Axis != null || b1.Def.Axis != null) ? UniqueStrings(a1.Def.Axis, b1.Def.Axis) : null)
		};
		static void BoostLargest(List<DeclarativeEffect> list4, int delta)
		{
			int best = -1;
			for (int j = 0; j < list4.Count; j++)
			{
				if (QUANTITY.Contains(list4[j].Effect) && list4[j].Amount.HasValue && (best < 0 || list4[j].Amount.GetValueOrDefault() > list4[best].Amount.GetValueOrDefault()))
				{
					best = j;
				}
			}
			if (best >= 0)
			{
				list4[best] = list4[best]with
				{
					Amount = Math.Max(1, list4[best].Amount.GetValueOrDefault() + delta)
				};
			}
		}
		bool Contributed(CardInstance c)
		{
			return ConvertAll(c).Count > 0;
		}
		List<DeclarativeEffect> ConvertAll(CardInstance c)
		{
			List<DeclarativeEffect> src;
			if (keepModes)
			{
				src = new List<DeclarativeEffect>(c.Def.Effects);
			}
			else
			{
				src = new List<DeclarativeEffect>(c.Def.Effects);
				if (c.Def.Modes != null && c.Def.Modes.Count > 0)
				{
					src.AddRange(c.Def.Modes[0].Effects);
				}
			}
			if (resultType == "permanent" && c.Def.Type != "permanent")
			{
				List<DeclarativeEffect> agg = new List<DeclarativeEffect>();
				foreach (DeclarativeEffect e2 in src)
				{
					if (DEAD_ON_PERMANENT.Contains(e2.Effect))
					{
						Drop(e2);
					}
					else
					{
						int twinIdx = -1;
						for (int j = 0; j < agg.Count; j++)
						{
							DeclarativeEffect m4 = agg[j];
							if (m4.Effect == e2.Effect && m4.Target == e2.Target && m4.Pierce == e2.Pierce && m4.SummonId == e2.SummonId && SameCondition(m4.Condition, e2.Condition) && m4.GrowthMultiplier == e2.GrowthMultiplier && m4.MomentumMultiplier == e2.MomentumMultiplier)
							{
								twinIdx = j;
								break;
							}
						}
						DeclarativeEffect twin = ((twinIdx >= 0) ? agg[twinIdx] : null);
						if (twin != null && twin.Amount.HasValue && e2.Amount.HasValue)
						{
							agg[twinIdx] = twin with
							{
								Amount = twin.Amount + e2.Amount
							};
						}
						else if (!(twin != null) || twin.Amount.HasValue || e2.Amount.HasValue)
						{
							agg.Add(e2 with { });
						}
					}
				}
				List<DeclarativeEffect> outAgg = new List<DeclarativeEffect>();
				{
					foreach (DeclarativeEffect e3 in agg)
					{
						if (!e3.Amount.HasValue || PLAYCARD_ONLY.Contains(e3.Effect))
						{
							outAgg.Add(e3 with
							{
								Trigger = (PERM_WINDOWS.Contains(e3.Trigger) ? e3.Trigger : "onPlay")
							});
						}
						else
						{
							int third = (int)Math.Floor((double)e3.Amount.GetValueOrDefault() / 3.0);
							if (third <= 0)
							{
								outAgg.Add(e3 with
								{
									Trigger = (PERM_WINDOWS.Contains(e3.Trigger) ? e3.Trigger : "onPlay")
								});
							}
							else
							{
								string trigger = (PERM_WINDOWS.Contains(e3.Trigger) ? e3.Trigger : "onTurnStart");
								DeclarativeEffect n2 = e3 with
								{
									Trigger = trigger,
									Amount = third
								};
								if (e3.AmountMax.HasValue)
								{
									n2 = n2 with
									{
										AmountMax = Math.Max(third, (int)Math.Floor((double)e3.AmountMax.GetValueOrDefault() / 3.0))
									};
								}
								outAgg.Add(n2);
							}
						}
					}
					return outAgg;
				}
			}
			if (resultType == "reaction" && c.Def.Type != "reaction")
			{
				List<DeclarativeEffect> outList = new List<DeclarativeEffect>();
				{
					foreach (DeclarativeEffect e4 in src)
					{
						if (REACTION_WINDOWS.Contains(e4.Trigger))
						{
							outList.Add(e4 with { });
						}
						else if (PLAYCARD_ONLY.Contains(e4.Effect) || DIES_IN_WINDOW.Contains(e4.Effect) || e4.Effect == "growSelf" || e4.Effect == "momentumCarryHalf")
						{
							Drop(e4);
						}
						else if (e4.Effect == "gainBlock" || e4.Effect == "gainIceBlock")
						{
							outList.Add(e4 with
							{
								Trigger = "onAttackIncoming"
							});
						}
						else
						{
							outList.Add(e4 with
							{
								Trigger = primaryWindow
							});
						}
					}
					return outList;
				}
			}
			if (resultType == "reaction" && c.Def.Type == "reaction" && (object)c != domi1)
			{
				return src.Select((DeclarativeEffect declarativeEffect) => (!REACTION_WINDOWS.Contains(declarativeEffect.Trigger)) ? declarativeEffect with { } : declarativeEffect with
				{
					Trigger = primaryWindow
				}).ToList();
			}
			if (resultType != "reaction" && c.Def.Type == "reaction")
			{
				List<DeclarativeEffect> outList2 = new List<DeclarativeEffect>();
				{
					foreach (DeclarativeEffect e5 in src)
					{
						if (e5.Effect == "negate")
						{
							Drop(e5);
						}
						else if (e5.Effect == "counter")
						{
							outList2.Add(e5 with
							{
								Effect = "dealDamage",
								Trigger = "onPlay"
							});
						}
						else
						{
							outList2.Add(e5 with
							{
								Trigger = "onPlay",
								Condition = null
							});
						}
					}
					return outList2;
				}
			}
			return src.Select((DeclarativeEffect declarativeEffect) => declarativeEffect with { }).ToList();
		}
		static int CostAsMaterial(CardDef d)
		{
			if (d.XCost != true)
			{
				return d.Cost;
			}
			return 3;
		}
		void Drop(DeclarativeEffect declarativeEffect)
		{
			bool has = DROP_VP.TryGetValue(declarativeEffect.Effect, out var vp);
			double baseVp = (has ? vp : 4.0);
			double mult = ((!(declarativeEffect.Amount.HasValue & has) || !DROP_AMOUNT_SCALED.Contains(declarativeEffect.Effect)) ? 1 : declarativeEffect.Amount.Value);
			droppedVp += baseVp * mult;
		}
		static bool HasDamage(IReadOnlyList<DeclarativeEffect> source)
		{
			return source.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Trigger == "onPlay" && (declarativeEffect.Effect == "dealDamage" || declarativeEffect.Effect.StartsWith("discharge") || declarativeEffect.Effect.StartsWith("dealDamage")));
		}
		static bool IsShatter(DeclarativeEffect declarativeEffect)
		{
			if (!(declarativeEffect.Effect == "shatterBlock"))
			{
				return declarativeEffect.Effect == "shatterBlockConvert";
			}
			return true;
		}
		CardInstance Materialize(CardInstance c)
		{
			if (c.Def.XCost != true || keepX)
			{
				return c;
			}
			List<DeclarativeEffect> eff2 = new List<DeclarativeEffect>();
			foreach (DeclarativeEffect e2 in c.Def.Effects)
			{
				if (e2.XHits == true)
				{
					for (int j = 0; j < 3; j++)
					{
						eff2.Add(e2 with
						{
							XHits = null
						});
					}
				}
				else
				{
					eff2.Add(e2);
				}
			}
			return c with
			{
				Def = c.Def with
				{
					XCost = null,
					Cost = 3,
					Effects = eff2
				}
			};
		}
		static string StemOf(CardDef d)
		{
			if (!WorkshopMade(d))
			{
				return WordOf(d);
			}
			string n2 = d.Name;
			if (n2.StartsWith("真・", StringComparison.Ordinal))
			{
				n2 = n2.Substring(2);
			}
			if (n2.EndsWith("+", StringComparison.Ordinal))
			{
				n2 = n2.Substring(0, n2.Length - 1);
			}
			int idx = n2.IndexOf("の", StringComparison.Ordinal);
			if (idx >= 0)
			{
				n2 = n2.Substring(0, idx);
			}
			if (n2.Length <= 3)
			{
				return n2;
			}
			return n2.Substring(0, 3);
		}
		static DeclarativeEffect ToAll(DeclarativeEffect declarativeEffect)
		{
			if (!AOE_CATALYST_OK.Contains(declarativeEffect.Effect) || declarativeEffect.Target != null)
			{
				return declarativeEffect;
			}
			return declarativeEffect with
			{
				Target = "all"
			};
		}
		int TwinOf(DeclarativeEffect declarativeEffect, int num)
		{
			for (int j = 0; j < merged.Count; j++)
			{
				DeclarativeEffect m4 = merged[j];
				if (ownerOf[j] != num && m4.Trigger == declarativeEffect.Trigger && m4.Effect == declarativeEffect.Effect && m4.Target == declarativeEffect.Target && m4.Pierce == declarativeEffect.Pierce && m4.SummonId == declarativeEffect.SummonId && SameCondition(m4.Condition, declarativeEffect.Condition) && m4.GrowthMultiplier == declarativeEffect.GrowthMultiplier && m4.MomentumMultiplier == declarativeEffect.MomentumMultiplier && m4.XHits == declarativeEffect.XHits)
				{
					return j;
				}
			}
			return -1;
		}
		static bool WorkshopMade(CardDef d)
		{
			if (!d.Id.StartsWith("fused_", StringComparison.Ordinal))
			{
				return d.Id.StartsWith("fusion_", StringComparison.Ordinal);
			}
			return true;
		}
	}

	private static string UniqueCodePoints(string wa, string wb)
	{
		HashSet<int> seen = new HashSet<int>();
		StringBuilder sb = new StringBuilder();
		string[] array = new string[2] { wa, wb };
		foreach (string s in array)
		{
			int len;
			for (int j = 0; j < s.Length; j += len)
			{
				int cp;
				if (char.IsHighSurrogate(s[j]) && j + 1 < s.Length && char.IsLowSurrogate(s[j + 1]))
				{
					cp = char.ConvertToUtf32(s[j], s[j + 1]);
					len = 2;
				}
				else
				{
					cp = s[j];
					len = 1;
				}
				if (seen.Add(cp))
				{
					sb.Append(s, j, len);
				}
			}
		}
		return sb.ToString();
	}

	private static IReadOnlyList<string> UniqueStrings(IReadOnlyList<string>? xa, IReadOnlyList<string>? xb)
	{
		HashSet<string> seen = new HashSet<string>();
		List<string> outList = new List<string>();
		IReadOnlyList<string>[] array = new IReadOnlyList<string>[2] { xa, xb };
		foreach (IReadOnlyList<string> src in array)
		{
			if (src == null)
			{
				continue;
			}
			foreach (string s in src)
			{
				if (seen.Add(s))
				{
					outList.Add(s);
				}
			}
		}
		return outList;
	}

	public static string? FuseBlockReason(CardInstance a, CardInstance b)
	{
		if (a.Uid == b.Uid)
		{
			return "同じカードは選べない";
		}
		if (RecipeFor(a.Def, b.Def) != null)
		{
			return null;
		}
		if (a.Def.Id.StartsWith("status_", StringComparison.Ordinal) || b.Def.Id.StartsWith("status_", StringComparison.Ordinal))
		{
			return "負傷・呪い・火傷は合成できない (使えない札)";
		}
		if (a.Def.Color != b.Def.Color)
		{
			return "合成は同じ色のカード同士のみ";
		}
		return null;
	}

	public static CardDef FuseCards(CardInstance a, CardInstance b)
	{
		bool anyUpgraded = Upgrade.IsUpgraded(a) || Upgrade.IsUpgraded(b);
		CardDef recipe = RecipeFor(a.Def, b.Def);
		if (recipe != null)
		{
			if (!anyUpgraded || !(Upgrade.UpgradeTier(recipe) != "none"))
			{
				return recipe;
			}
			return Upgrade.UpgradeCard(new CardInstance
			{
				Uid = "recipe",
				Def = recipe
			}).Def;
		}
		CardDef merged = MergeFusion(Lift(a), Lift(b));
		if (!anyUpgraded)
		{
			return merged;
		}
		if (!merged.Name.EndsWith("+", StringComparison.Ordinal))
		{
			return merged with
			{
				Name = merged.Name + "+"
			};
		}
		return merged;
		CardInstance Lift(CardInstance c)
		{
			if (!anyUpgraded || Upgrade.IsUpgraded(c) || !(Upgrade.UpgradeTier(c.Def) != "none"))
			{
				return c;
			}
			return Upgrade.UpgradeCard(c);
		}
	}

	public static IReadOnlyList<string> FusionNotes(CardInstance a, CardInstance b)
	{
		List<string> notes = new List<string>();
		if (RecipeFor(a.Def, b.Def) != null)
		{
			notes.Add("★レシピ: 手書きの一品");
		}
		IReadOnlyList<string> axesB = Run.AxesOf(b.Def);
		string shared = Run.AxesOf(a.Def).FirstOrDefault((string ax) => axesB.Contains(ax));
		if (shared != null && AXIS_JA.TryGetValue(shared, out string ja))
		{
			notes.Add($"軸一致 ({shared}): {ja} のおまけ");
		}
		if (Upgrade.IsUpgraded(a) || Upgrade.IsUpgraded(b))
		{
			notes.Add("鍛えの引き継ぎ: 鍛えていない側の素材も鍛えてから合体 (結果は+)");
		}
		int ca = ((a.Def.XCost == true) ? 3 : a.Def.Cost);
		int cb = ((b.Def.XCost == true) ? 3 : b.Def.Cost);
		if (a.Def.XCost == true != (b.Def.XCost == true))
		{
			CardDef other = ((a.Def.XCost == true) ? b.Def : a.Def);
			if (other.FusionCatalyst != null)
			{
				notes.Add((other.FusionCatalyst == "cheaper") ? "触媒 × X: X のまま。軽くなる触媒は「X+1」(払った量に+1して解決)" : "触媒 × X: X のまま (触媒の効果は固定量で乗る)");
			}
			else
			{
				notes.Add("X札は片方だけなら X=3 の固定量に畳む");
			}
		}
		if (ca == 0 || cb == 0)
		{
			notes.Add("0E素材は値引きにならない (高い方のコスト)");
		}
		if (ca + cb - 1 > 5 && ca > 0 && cb > 0)
		{
			notes.Add("5E上限: 超えた分だけ量を比例縮小");
		}
		string[] types = new string[2]
		{
			a.Def.Type,
			b.Def.Type
		};
		if (((a.Def.Modes != null && a.Def.Modes.Count > 0) || (b.Def.Modes != null && b.Def.Modes.Count > 0)) && (Enumerable.Contains(types, "permanent") || Enumerable.Contains(types, "reaction")))
		{
			notes.Add("選択式は置物化・罠化では最初のモードだけを採る");
		}
		if (Enumerable.Contains(types, "permanent") && (!(a.Def.Type == "permanent") || !(b.Def.Type == "permanent")))
		{
			notes.Add("置物化: 量は同種を合計して÷3で毎ターン化 (3未満は登場時1回)。打ち消し・倍化・放出は落ちて価値を振り替え");
		}
		if (Enumerable.Contains(types, "reaction") && (!(a.Def.Type == "reaction") || !(b.Def.Type == "reaction")))
		{
			notes.Add((ca + cb - 1 > 2) ? "重い札は罠に収まらない: 合計−1が2Eを超えるので相手側のタイプで出る (返し→ダメージ・窓ブロック→ブロック)" : "リアクション化: 相手の効果は罠の窓で解決 (ドロー・一時マナ・サーチは落ちて価値を振り替え)");
		}
		if (a.Def.Type == "reaction" && b.Def.Type == "reaction")
		{
			notes.Add("罠同士: 窓は支配側の主窓に揃う。2Eを超える分は量で払う");
		}
		return notes;
	}

	public static CardDef? ResolveFusedDef(string id)
	{
		foreach (FusionRecipe r in RECIPES)
		{
			if (r.Result.Id == id)
			{
				return r.Result;
			}
		}
		if (!id.StartsWith("fused_", StringComparison.Ordinal))
		{
			return null;
		}
		string body = id.Substring("fused_".Length);
		for (int k = body.IndexOf("__", StringComparison.Ordinal); k >= 0; k = body.IndexOf("__", k + 1, StringComparison.Ordinal))
		{
			CardDef a = MaterialDef(body.Substring(0, k));
			CardDef b = ((a != null) ? MaterialDef(body.Substring(k + 2)) : null);
			if (!(a == null) && !(b == null))
			{
				try
				{
					return FuseCards(new CardInstance
					{
						Uid = "resolve_a",
						Def = a
					}, new CardInstance
					{
						Uid = "resolve_b",
						Def = b
					});
				}
				catch
				{
					return null;
				}
			}
		}
		return null;
	}

	private static CardDef? MaterialDef(string id)
	{
		try
		{
			return Content.GetCardDef(id);
		}
		catch
		{
			return ResolveFusedDef(id);
		}
	}
}
