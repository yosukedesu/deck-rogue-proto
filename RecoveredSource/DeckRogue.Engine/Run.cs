using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Run
{
	public sealed record CampfireOptionsInfo(bool Dig, bool Remove, int TrainLeft, bool Forge);

	public static readonly HashSet<string> REWARD_EXCLUDED = new HashSet<string>
	{
		"black_shiv_token", "white_spark_token", "white_perm_page", "green_strike", "green_guard", "green_basic_bash", "green_reaction_vine", "blue_strike", "blue_guard", "blue_counterspell",
		"green_entangle", "green_vine_wedge", "blue_tide_drop", "blue_current_lash", "blue_weight_of_wisdom", "red_spark", "red_ignite", "red_perm_flarecoat", "red_perm_thorn_flame", "red_mode_firecontrol",
		"white_shield_strike", "white_light_bolt", "white_mode_crossroad", "white_light_buckler", "black_dark_pact", "black_drain", "black_bursting_corpse", "black_shadow_blade", "white_reaction_ward", "black_reaction_curse",
		"black_reaction_grudge", "red_strike", "red_guard", "white_strike", "white_guard", "black_strike", "black_guard"
	};

	private const double CAMPFIRE_HEAL_RATIO = 0.25;

	private const int VICTORY_HEAL = 0;

	private const int ELITE_STRENGTH = 0;

	private const int STARTING_GOLD = 50;

	public const int DEPARTURE_PURSE = 100;

	private const int GOLD_PER_BATTLE_MIN = 12;

	private const int GOLD_PER_BATTLE_MAX = 18;

	private const int GOLD_ELITE_BONUS_MIN = 30;

	private const int GOLD_ELITE_BONUS_MAX = 40;

	private const int THIEF_BOUNTY = 10;

	private const int UNKNOWN_PITY_BASE_MONSTER = 10;

	private const int UNKNOWN_PITY_BASE_SHOP = 3;

	private const int UNKNOWN_PITY_BASE_TREASURE = 2;

	private const int SHRINE_CHANCE_PERCENT = 25;

	private const int SHOP_CARD_COUNT = 5;

	private const int SHOP_RELIC_PRICE = 150;

	public const int GEAR_DROP_BASE = 60;

	public const int GEAR_DROP_PITY = 10;

	public const int MANA_PER_WIN = 5;

	public const int MANA_PER_ELITE_BOSS = 10;

	public const int SHOP_GEAR_SLOTS = 3;

	public static readonly IReadOnlyDictionary<string, int> SHOP_GEAR_PRICE = new Dictionary<string, int>
	{
		{ "common", 40 },
		{ "uncommon", 60 },
		{ "rare", 90 }
	};

	public const int SHOP_MANA_PRICE = 30;

	private const int SHOP_REMOVAL_BASE = 50;

	private const int SHOP_REMOVAL_STEP = 25;

	private const int SHOP_UPGRADE_BASE = 100;

	private const int SHOP_UPGRADE_STEP = 50;

	public const int WORKSHOP_FUSE_PRICE = 100;

	public static readonly IReadOnlyList<DifficultyScaleRow> DIFFICULTY_TABLE = new DifficultyScaleRow[10]
	{
		new DifficultyScaleRow
		{
			Hp = new double[3] { 0.9, 0.9, 0.9 },
			Atk = new double[3] { 0.9, 0.9, 0.9 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 0.95, 0.95, 0.95 },
			Atk = new double[3] { 0.95, 0.95, 0.95 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 1.0, 1.0, 1.0 },
			Atk = new double[3] { 1.0, 1.0, 1.0 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 1.0, 1.03, 1.06 },
			Atk = new double[3] { 1.02, 1.06, 1.12 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 1.0, 1.06, 1.12 },
			Atk = new double[3] { 1.04, 1.12, 1.24 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 1.0, 1.09, 1.18 },
			Atk = new double[3] { 1.06, 1.18, 1.36 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 1.0, 1.12, 1.24 },
			Atk = new double[3] { 1.08, 1.24, 1.48 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 1.0, 1.15, 1.3 },
			Atk = new double[3] { 1.1, 1.3, 1.6 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 1.0, 1.18, 1.36 },
			Atk = new double[3] { 1.12, 1.36, 1.72 }
		},
		new DifficultyScaleRow
		{
			Hp = new double[3] { 1.0, 1.21, 1.42 },
			Atk = new double[3] { 1.14, 1.42, 1.84 }
		}
	};

	public static readonly IReadOnlyList<DifficultyTax> DIFFICULTY_TAX = new DifficultyTax[10]
	{
		new DifficultyTax
		{
			BossHeal = 1.0,
			Campfire = 0.25,
			EliteBonus = 0,
			ShopPrice = 1.0,
			RewardChoices = 0,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 1.0,
			Campfire = 0.25,
			EliteBonus = 0,
			ShopPrice = 1.0,
			RewardChoices = 0,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 1.0,
			Campfire = 0.25,
			EliteBonus = 0,
			ShopPrice = 1.0,
			RewardChoices = 0,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 0.75,
			Campfire = 0.25,
			EliteBonus = 0,
			ShopPrice = 1.0,
			RewardChoices = 0,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 0.75,
			Campfire = 0.2,
			EliteBonus = 0,
			ShopPrice = 1.0,
			RewardChoices = 0,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 0.75,
			Campfire = 0.2,
			EliteBonus = 1,
			ShopPrice = 1.0,
			RewardChoices = 0,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 0.5,
			Campfire = 0.2,
			EliteBonus = 1,
			ShopPrice = 1.0,
			RewardChoices = 0,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 0.5,
			Campfire = 0.2,
			EliteBonus = 1,
			ShopPrice = 1.2,
			RewardChoices = 0,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 0.5,
			Campfire = 0.2,
			EliteBonus = 1,
			ShopPrice = 1.2,
			RewardChoices = -1,
			StartGuilt = 0
		},
		new DifficultyTax
		{
			BossHeal = 0.25,
			Campfire = 0.2,
			EliteBonus = 1,
			ShopPrice = 1.2,
			RewardChoices = -1,
			StartGuilt = 1
		}
	};

	public const int DEFAULT_DIFFICULTY = 3;

	public const int UNKNOWN_SHOP_MIN_GOLD = 50;

	private static readonly HashSet<string> UNEXHAUST_BANNED = new HashSet<string>
	{
		"drawCards", "impulseDraw", "addCardToHand", "drawCardsPerLight", "drawCardsPerCardPlayed", "dischargeAetherDraw", "retrieveFromExhaust", "playFromExhaust", "gainEnergy", "discountNext",
		"doubleGrowth", "doubleMomentum", "doubleLight", "gainEnergyMax"
	};

	private static readonly HashSet<string> SET_RELICS = new HashSet<string> { "relic_talisman_pouch", "relic_quiet_bell", "relic_double_talisman", "relic_reading_glasses", "relic_retrieve_cord" };

	private static readonly Dictionary<string, string> EFFECT_AXIS = new Dictionary<string, string>
	{
		{ "addGrowth", "growth" },
		{ "doubleGrowth", "growth" },
		{ "dischargeGrowth", "growth" },
		{ "dischargeGrowthBlock", "growth" },
		{ "gainEnergyMax", "ramp" },
		{ "dealDamagePerEnergyMax", "ramp" },
		{ "gainBlockPerEnergyMax", "ramp" },
		{ "addMomentum", "trample" },
		{ "dealDamagePerMomentum", "trample" },
		{ "doubleMomentum", "trample" },
		{ "dischargeMomentumBurn", "burn" },
		{ "dischargeMomentumBlock", "trample" },
		{ "gainBlockPerMomentum", "trample" },
		{ "addGrowthPerMomentum", "trample" },
		{ "applyBurn", "burn" },
		{ "dischargeBurn", "burn" },
		{ "cascadeSameCost", "cascade" },
		{ "addAether", "aether" },
		{ "dischargeAether", "aether" },
		{ "dischargeAetherDraw", "aether" },
		{ "addLight", "light" },
		{ "addLightNextTurn", "light" },
		{ "dischargeLight", "light" },
		{ "dischargeLightRally", "light" },
		{ "doubleLight", "light" },
		{ "dealDamagePerLight", "light" },
		{ "dischargeLightWeaken", "light" },
		{ "consumeLight", "light" },
		{ "gainBlockPerLight", "light" },
		{ "drawCardsPerLight", "light" },
		{ "lightCarryHalf", "light" },
		{ "addCardToDraw", "spark" },
		{ "addCardToDiscard", "spark" },
		{ "transformDeckToToken", "spark" },
		{ "lightToSparks", "spark" },
		{ "dealDamagePerSpark", "spark" },
		{ "gainBlockPerSpark", "spark" },
		{ "triggerRandomRetainer", "spark" },
		{ "gainIceBlock", "ice" },
		{ "dealDamagePerIceBlock", "ice" },
		{ "gainIceBlockPerCardPlayed", "ice" },
		{ "negate", "permission" },
		{ "negateConvertIce", "permission" },
		{ "summonPermanent", "retinue" },
		{ "dealDamagePerPermanent", "retinue" },
		{ "gainBlockPerPermanent", "retinue" },
		{ "exhaustFromDeck", "graveyard" },
		{ "exhaustFromDeckChoose", "graveyard" },
		{ "dealDamagePerExhaust", "graveyard" },
		{ "retrieveFromExhaust", "graveyard" },
		{ "playFromExhaust", "graveyard" },
		{ "gainBlockPerExhaust", "graveyard" },
		{ "dealDamageDrainPerExhaust", "graveyard" },
		{ "loseHp", "selfharm" },
		{ "dealDamagePerSelfHpLost", "selfharm" },
		{ "dealDamagePerCardPlayed", "storm" },
		{ "drawCardsPerCardPlayed", "storm" },
		{ "addCasts", "storm" },
		{ "impulseDraw", "impulse" },
		{ "weakenEnemy", "oppress" },
		{ "dealDamagePerNegStrength", "oppress" },
		{ "gainHp", "heal" },
		{ "dealDamageDrain", "heal" },
		{ "dealDamagePerDamageTaken", "wrath" },
		{ "applyBurnPerDamageTaken", "wrath" },
		{ "dealDamagePerBlock", "fortress" },
		{ "shatterBlock", "shatter" },
		{ "shatterBlockConvert", "shatter" },
		{ "dealDamageRandom", "chaos" },
		{ "dealDamagePerRandomPlayed", "chaos" },
		{ "dealDamageExecute", "execute" },
		{ "exposeEnemy", "execute" },
		{ "confuse", "confuse" },
		{ "dealDamagePerHandCard", "grimoire" },
		{ "gainIceBlockPerHandCard", "grimoire" },
		{ "scry", "grimoire" },
		{ "dealDamagePerScry", "grimoire" },
		{ "retainHandUpTo", "grimoire" },
		{ "dealDamagePerTrapFired", "permission" },
		{ "extendTrapLife", "permission" },
		{ "trapsNeverExpire", "permission" },
		{ "retrieveZeroCostFromDiscard", "storm" },
		{ "drawTypeFromDeck", "permission" },
		{ "aetherCarryHalf", "aether" },
		{ "retainedCostDown", "grimoire" },
		{ "addSpellEcho", "echo" },
		{ "dealDamagePerCascade", "cascade" },
		{ "firstCardCascade", "cascade" },
		{ "doubleBurn", "burn" },
		{ "gainBlockPerBurn", "burn" },
		{ "dealDamagePerBurn", "burn" },
		{ "burnNoDecay", "burn" },
		{ "keepImpulseOne", "impulse" }
	};

	private static readonly Dictionary<string, string> TRIGGER_AXIS = new Dictionary<string, string>
	{
		{ "onPermanentEntered", "retinue" },
		{ "onCardExhausted", "graveyard" },
		{ "onCostExhausted", "graveyard" },
		{ "onHealed", "heal" },
		{ "onHpLost", "selfharm" },
		{ "onAetherGained", "aether" },
		{ "onLightGained", "light" },
		{ "onLightDischarged", "light" },
		{ "onSparkPlayed", "spark" },
		{ "onImpulsePlayed", "impulse" },
		{ "onImpulseExpired", "impulse" },
		{ "onBlazeIgnite", "burn" },
		{ "onCascade", "cascade" },
		{ "onRandomPlayed", "chaos" },
		{ "onSpellPlayed", "storm" },
		{ "onBlockGained", "fortress" },
		{ "onActionNegated", "permission" },
		{ "onSelfExhausted", "graveyard" }
	};

	private static int JsRound(double v)
	{
		return (int)Math.Floor(v + 0.5);
	}

	private static int JsFloor(double v)
	{
		return (int)Math.Floor(v);
	}

	private static int JsTrunc(double v)
	{
		return (int)v;
	}

	private static List<T> Append<T>(IReadOnlyList<T> a, T b)
	{
		return new List<T>(a) { b };
	}

	private static List<T> Concat<T>(IReadOnlyList<T> a, IEnumerable<T> b)
	{
		List<T> list = new List<T>(a);
		list.AddRange(b);
		return list;
	}

	private static RunStateUnknownPity UnknownPityBase()
	{
		return new RunStateUnknownPity
		{
			Monster = 10,
			Shop = 3,
			Treasure = 2
		};
	}

	public static CardDef WorkshopFuseResult(RunState run, CardInstance a, CardInstance b)
	{
		return WorkshopFuseResult(run, a, b, Fusion.FuseCards(a, b));
	}

	public static CardDef WorkshopFuseResult(RunState run, CardInstance a, CardInstance b, CardDef fusedDef)
	{
		CardInstance fused = new CardInstance
		{
			Uid = "preview",
			Def = fusedDef
		};
		if (!RelicFlag(run, "fusionUpgraded") || !Upgrade.CanUpgradeCard(fused))
		{
			return fusedDef;
		}
		return Upgrade.UpgradeCard(fused).Def;
	}

	public static int WorkshopFusePrice(RunState run)
	{
		return Math.Max(0, 100 - RelicBonusSum(run, "fusionDiscount"));
	}

	public static int RelicBonusSum(RunState run, string key)
	{
		int a = 0;
		foreach (string relic in run.Relics)
		{
			RelicDefBonus b = Content.GetRelicDef(relic).Bonus;
			if (!(b == null))
			{
				a += (key switch
				{
					"victoryHealFlat" => b.VictoryHealFlat, 
					"shopUpgradeDiscount" => b.ShopUpgradeDiscount, 
					"restMaxHp" => b.RestMaxHp, 
					"eliteGoldBonus" => b.EliteGoldBonus, 
					"fusionDiscount" => b.FusionDiscount, 
					"removalStepDelta" => b.RemovalStepDelta, 
					"eliteRelicPicks" => b.EliteRelicPicks, 
					"goldPerRow" => b.GoldPerRow, 
					"goldPerUnknown" => b.GoldPerUnknown, 
					"unknownChestEvery" => b.UnknownChestEvery, 
					"shopHeal" => b.ShopHeal, 
					"campfireTrain" => b.CampfireTrain, 
					"wingBoots" => b.WingBoots, 
					"extraRewardRounds" => b.ExtraRewardRounds, 
					"skipRewardMaxHp" => b.SkipRewardMaxHp, 
					"relicsOnPickup" => b.RelicsOnPickup, 
					"brandsOnPickup" => b.BrandsOnPickup, 
					"removeOnPickup" => b.RemoveOnPickup, 
					"transformOnPickup" => b.TransformOnPickup, 
					"workshopFuses" => b.WorkshopFuses, 
					"maxHpPerBrand" => b.MaxHpPerBrand, 
					"brandWard" => b.BrandWard, 
					_ => null, 
				}).GetValueOrDefault();
			}
		}
		return a;
	}

	public static bool RelicFlag(RunState run, string key)
	{
		foreach (string relic in run.Relics)
		{
			RelicDefBonus b = Content.GetRelicDef(relic).Bonus;
			if (!(b == null) && key switch
			{
				"campfireDig" => b.CampfireDig, 
				"campfireRemove" => b.CampfireRemove, 
				"noForge" => b.NoForge, 
				"fusionUpgraded" => b.FusionUpgraded, 
				"noRest" => b.NoRest, 
				_ => null, 
			} == true)
			{
				return true;
			}
		}
		return false;
	}

	public static int RelicStateOf(RunState run, string key)
	{
		if (run.RelicState == null)
		{
			return 0;
		}
		if (!run.RelicState.TryGetValue(key, out var v))
		{
			return 0;
		}
		return v;
	}

	private static RunState WithRelicState(RunState run, string key, int value)
	{
		Dictionary<string, int> map = new Dictionary<string, int>();
		if (run.RelicState != null)
		{
			foreach (KeyValuePair<string, int> kv in run.RelicState)
			{
				map[kv.Key] = kv.Value;
			}
		}
		map[key] = value;
		return run with
		{
			RelicState = map
		};
	}

	private static RunState SettleHpLossCap(RunState next, GameState combat)
	{
		if (!combat.MaxHpLossCharges.HasValue)
		{
			return next;
		}
		int total = 0;
		foreach (string id in next.Relics)
		{
			total += (Content.GetRelicDef(id).CombatRule?.MaxHpLossCharges).GetValueOrDefault();
		}
		if (total <= 0)
		{
			return next;
		}
		next = WithRelicState(next, "remnantUsed", Math.Max(0, Math.Min(total, total - combat.MaxHpLossCharges.Value)));
		if (combat.MaxHpLossCharges.Value > 0)
		{
			return next;
		}
		return next with
		{
			Relics = next.Relics.Where((string id2) => (Content.GetRelicDef(id2).CombatRule?.MaxHpLossCharges).GetValueOrDefault() == 0).ToList()
		};
	}

	public static int? RelicChargesLeft(RunState run, string relicId)
	{
		int? total = Content.GetRelicDef(relicId).CombatRule?.MaxHpLossCharges;
		if (!total.HasValue)
		{
			return null;
		}
		return Math.Max(0, total.Value - RelicStateOf(run, "remnantUsed"));
	}

	private static RunState BreakMawBank(RunState run)
	{
		if (RelicBonusSum(run, "goldPerRow") <= 0 || RelicStateOf(run, "mawBroken") == 1)
		{
			return run;
		}
		return WithRelicState(run, "mawBroken", 1);
	}

	public static IReadOnlyList<GearInstance> GearsOf(RunState run)
	{
		return run.Gears ?? new List<GearInstance>();
	}

	public static int ManaOf(RunState run)
	{
		return run.Mana.GetValueOrDefault();
	}

	public static bool GearFull(RunState run)
	{
		return GearsOf(run).Count >= 10;
	}

	private static (string Id, RngState Rng) RollGearId(RngState rng0, bool atLeastUncommon)
	{
		(int Value, RngState Next) tuple = Rng.NextInt(rng0, 0, 99);
		int roll = tuple.Value;
		RngState item = tuple.Next;
		string wanted;
		if (roll < 10)
		{
			wanted = "rare";
		}
		else
		{
			wanted = (((roll < 35) | atLeastUncommon) ? "uncommon" : "common");
		}
		List<GearDef> pool = Content.PoolGears.Where((GearDef g) => g.Rarity == wanted).ToList();
		if (pool.Count == 0)
		{
			pool = new List<GearDef>(Content.PoolGears);
		}
		var (i, rng2) = Rng.NextInt(item, 0, pool.Count - 1);
		return (Id: pool[i].Id, Rng: rng2);
	}

	private static RunState AddGear(RunState run, string gearId, string uidHint)
	{
		IReadOnlyList<string> seen = run.SeenGearIds ?? new List<string>();
		RunState withSeen = run with
		{
			SeenGearIds = seen.Contains(gearId) ? seen : Append(seen, gearId)
		};
		if (GearFull(withSeen))
		{
			return withSeen;
		}
		return withSeen with
		{
			Gears = Append(GearsOf(withSeen), Gears.MakeGear(gearId, "gear_" + uidHint))
		};
	}

	private static RunState SpendGear(RunState run, int index)
	{
		IReadOnlyList<GearInstance> gears = GearsOf(run);
		if (index < 0 || index >= gears.Count)
		{
			throw new InvalidOperationException($"不正なギア指定: {index}");
		}
		GearInstance gear = gears[index];
		int left = gear.Charges - 1;
		return run with
		{
			Mana = ManaOf(run) - 10,
			Gears = ((left > 0) ? gears.Select((GearInstance g, int i) => (i != index) ? g : g with
			{
				Charges = left
			}).ToList() : gears.Where((GearInstance _, int i) => i != index).ToList())
		};
	}

	private static RunState DiscardGearAt(RunState run, int index)
	{
		IReadOnlyList<GearInstance> gears = GearsOf(run);
		if (index < 0 || index >= gears.Count)
		{
			throw new InvalidOperationException($"不正なギア指定: {index}");
		}
		return run with
		{
			Gears = gears.Where((GearInstance _, int i) => i != index).ToList()
		};
	}

	public static CampfireOptionsInfo CampfireOptions(RunState run)
	{
		int trainMax = RelicBonusSum(run, "campfireTrain");
		return new CampfireOptionsInfo(RelicFlag(run, "campfireDig"), RelicFlag(run, "campfireRemove"), Math.Max(0, trainMax - RelicStateOf(run, "train")), !RelicFlag(run, "noForge"));
	}

	public static IReadOnlyList<int> WingChoices(RunState run)
	{
		List<int> outp = new List<int>();
		if (RelicStateOf(run, "wingBoots") <= 0)
		{
			return outp;
		}
		if (run.Row < 0 || run.Row >= run.Map.Count - 1)
		{
			return outp;
		}
		HashSet<int> normal = new HashSet<int>(NextChoices(run));
		for (int c = 0; c < run.Map[run.Row + 1].Count; c++)
		{
			if (!normal.Contains(c))
			{
				outp.Add(c);
			}
		}
		return outp;
	}

	public static bool IsBasicCard(CardInstance card)
	{
		string id = card.Def.Id;
		string[] array = new string[5] { "green", "blue", "red", "white", "black" };
		foreach (string color in array)
		{
			if (id == color + "_strike" || id == color + "_guard")
			{
				return true;
			}
		}
		return false;
	}

	public static bool RelicAllowedForColors(RelicDef def, IReadOnlyList<string> colors)
	{
		if (def.Colors == null)
		{
			return true;
		}
		foreach (string c in def.Colors)
		{
			if (colors.Contains(c))
			{
				return true;
			}
		}
		return false;
	}

	public static RunState AddCardsToRunDeck(RunState run, IReadOnlyList<CardInstance> cards)
	{
		HashSet<string> eggs = new HashSet<string>();
		foreach (string relic in run.Relics)
		{
			IReadOnlyList<string> u = Content.GetRelicDef(relic).Bonus?.UpgradeOnAdd;
			if (u == null)
			{
				continue;
			}
			foreach (string t in u)
			{
				eggs.Add(t);
			}
		}
		int perBrand = RelicBonusSum(run, "maxHpPerBrand");
		RunState next = run;
		List<CardInstance> added = new List<CardInstance>();
		foreach (CardInstance c in cards)
		{
			if (c.Def.Id == Content.BRAND_DEF.Id || c.Def.Id == Content.GUILT_DEF.Id)
			{
				if (RelicStateOf(next, "brandWard") > 0)
				{
					next = WithRelicState(next, "brandWard", RelicStateOf(next, "brandWard") - 1);
					continue;
				}
				if (perBrand > 0)
				{
					next = next with
					{
						MaxHp = next.MaxHp + perBrand,
						Hp = next.Hp + perBrand
					};
				}
				added.Add(c);
			}
			else
			{
				added.Add((eggs.Contains(c.Def.Type) && Upgrade.CanUpgradeCard(c)) ? Upgrade.UpgradeCard(c) : c);
			}
		}
		if (added.Count != 0)
		{
			return next with
			{
				Deck = Concat(next.Deck, added)
			};
		}
		return next;
	}

	public static RunState TransformCardAt(RunState run, int index, bool upgrade)
	{
		CardInstance card = ((index >= 0 && index < run.Deck.Count) ? run.Deck[index] : null);
		if (card == null)
		{
			throw new InvalidOperationException("対象カードを cardIndex で指定する");
		}
		if (!CanTransformCard(card))
		{
			throw new InvalidOperationException(card.Def.Name + " は変成できない（状態異常・烙印は除去で消す）");
		}
		string rarity = card.Def.Rarity ?? "common";
		List<CardDef> pool = (from c in RewardPool(run)
			where (c.Rarity ?? "common") == rarity && c.Id != card.Def.Id
			select c).ToList();
		if (pool.Count == 0)
		{
			return run;
		}
		(int Value, RngState Next) tuple = Rng.NextInt(run.Rng, 0, pool.Count - 1);
		int idx = tuple.Value;
		RngState rng = tuple.Next;
		HashSet<string> eggs = new HashSet<string>();
		foreach (string relic in run.Relics)
		{
			IReadOnlyList<string> u = Content.GetRelicDef(relic).Bonus?.UpgradeOnAdd;
			if (u == null)
			{
				continue;
			}
			foreach (string t in u)
			{
				eggs.Add(t);
			}
		}
		CardInstance replacement = new CardInstance
		{
			Uid = $"trans_a{run.Act}_r{run.Row}_{index}_{pool[idx].Id}",
			Def = pool[idx]
		};
		if ((upgrade || eggs.Contains(replacement.Def.Type)) && Upgrade.CanUpgradeCard(replacement))
		{
			replacement = Upgrade.UpgradeCard(replacement);
		}
		return run with
		{
			Rng = rng,
			Deck = run.Deck.Select((CardInstance c, int i) => (i != index) ? c : replacement).ToList()
		};
	}

	private static RunState EnterPendingChoice(RunState run, string resume)
	{
		if (run.PendingRelicChoice == null)
		{
			return run;
		}
		return run with
		{
			Phase = "relic-choose",
			PendingRelicChoice = run.PendingRelicChoice with
			{
				Resume = resume
			}
		};
	}

	public static RunState GainRelic(RunState run, string relicId)
	{
		RelicDef def = Content.GetRelicDef(relicId);
		RunState next = run with
		{
			Relics = Append(run.Relics, relicId)
		};
		next = ApplyRelicBonus(next, relicId);
		next = ApplyRandomUpgradesOnPickup(next, relicId);
		RelicDefBonus b = def.Bonus;
		if ((b?.WingBoots).GetValueOrDefault() > 0)
		{
			next = WithRelicState(next, "wingBoots", RelicStateOf(next, "wingBoots") + (b?.WingBoots).GetValueOrDefault());
		}
		if ((b?.BrandWard).GetValueOrDefault() > 0)
		{
			next = WithRelicState(next, "brandWard", RelicStateOf(next, "brandWard") + (b?.BrandWard).GetValueOrDefault());
		}
		if (def.ExpiresAfterBattles.HasValue)
		{
			next = WithRelicState(next, "exp_" + relicId, def.ExpiresAfterBattles.Value);
		}
		if ((object)b != null && b.HealFullOnPickup == true)
		{
			next = next with
			{
				Hp = next.MaxHp
			};
		}
		if ((object)b != null && b.TransformBasicsOnPickup == true)
		{
			for (int i = 0; i < next.Deck.Count; i++)
			{
				if (IsBasicCard(next.Deck[i]))
				{
					next = TransformCardAt(next, i, upgrade: false);
				}
			}
		}
		if ((b?.BrandsOnPickup).GetValueOrDefault() > 0)
		{
			List<CardInstance> list = new List<CardInstance>();
			for (int j = 0; j < (b?.BrandsOnPickup).GetValueOrDefault(); j++)
			{
				list.Add(new CardInstance
				{
					Uid = $"brand_relic_{relicId}_a{next.Act}_r{next.Row}_{j}",
					Def = Content.BRAND_DEF
				});
			}
			next = AddCardsToRunDeck(next, list);
		}
		if ((b?.RelicsOnPickup).GetValueOrDefault() > 0)
		{
			(IReadOnlyList<string> Options, RngState Rng) tuple = DrawRelicOptions(next, "chest", (b?.RelicsOnPickup).GetValueOrDefault());
			IReadOnlyList<string> drawn = tuple.Options;
			RngState rng = tuple.Rng;
			next = next with
			{
				Rng = rng
			};
			foreach (string id in drawn)
			{
				next = GainRelic(next, id);
			}
		}
		next = WithRelicGainBrands(next, run);
		if ((b?.RemoveOnPickup).GetValueOrDefault() > 0 || (b?.TransformOnPickup).GetValueOrDefault() > 0)
		{
			bool remove = (b?.RemoveOnPickup).GetValueOrDefault() > 0;
			next = next with
			{
				PendingRelicChoice = new RunStatePendingRelicChoice
				{
					RelicId = relicId,
					Mode = (remove ? "remove" : "transform"),
					Count = (remove ? (b?.RemoveOnPickup).GetValueOrDefault() : (b?.TransformOnPickup).GetValueOrDefault()),
					Resume = "map"
				}
			};
		}
		return next;
	}

	public static double ShopPriceRatio(RunState run)
	{
		double a = 1.0;
		foreach (string id in run.Relics)
		{
			a *= Content.GetRelicDef(id).Bonus?.ShopPriceRatio ?? 1.0;
		}
		return a * DifficultyTaxOf(run.Difficulty).ShopPrice;
	}

	private static double GoldMultiplier(RunState run)
	{
		double a = 1.0;
		foreach (string id in run.Relics)
		{
			a *= Content.GetRelicDef(id).Bonus?.GoldMultiplier ?? 1.0;
		}
		return a;
	}

	public static int ShopRemovalPrice(RunState run)
	{
		int step = Math.Max(0, 25 + RelicBonusSum(run, "removalStepDelta"));
		return JsFloor((double)(50 + step * run.RemovalCount) * ShopPriceRatio(run));
	}

	public static int ShopUpgradePrice(RunState run)
	{
		return JsFloor((double)(Math.Max(0, 100 - RelicBonusSum(run, "shopUpgradeDiscount")) + 50 * run.UpgradeCount) * ShopPriceRatio(run));
	}

	public static int DepthStrength(int row, int act = 1)
	{
		return (row >= MapGen.BossRowFor(act)) ? 1 : 0;
	}

	public static int DifficultyLevel(int? level)
	{
		int n = (level.HasValue ? level.Value : 3);
		return Math.Min(DIFFICULTY_TABLE.Count, Math.Max(1, n));
	}

	public static DifficultyScaleEntry DifficultyScale(int? level, int act = 1, int? row = null)
	{
		DifficultyScaleRow t = DIFFICULTY_TABLE[DifficultyLevel(level) - 1];
		int a = Math.Min(3, Math.Max(1, act)) - 1;
		if (a == 0 && row.HasValue && row.Value >= (int)Math.Floor((double)MapGen.BossRowFor(1) / 2.0))
		{
			return new DifficultyScaleEntry
			{
				Hp = MidScale(t.Hp[0], t.Hp[1]),
				Atk = MidScale(t.Atk[0], t.Atk[1])
			};
		}
		return new DifficultyScaleEntry
		{
			Hp = t.Hp[a],
			Atk = t.Atk[a]
		};
	}

	private static double MidScale(double x, double y)
	{
		return Math.Floor((x + y) / 2.0 * 1000.0 + 0.5) / 1000.0;
	}

	public static DifficultyTax DifficultyTaxOf(int? level)
	{
		return DIFFICULTY_TAX[DifficultyLevel(level) - 1];
	}

	public static int EliteCountFor(int? level, int act)
	{
		return 4 + ((act >= 2) ? DifficultyTaxOf(level).EliteBonus : 0);
	}

	public static string DifficultyDescription(int? level)
	{
		int n = DifficultyLevel(level);
		if (n == 3)
		{
			return n + ": 標準（基準線。幕1〜3とも×1.0・幕ボス撃破で全回復）";
		}
		DifficultyScaleRow row = DIFFICULTY_TABLE[n - 1];
		DifficultyTax t = DIFFICULTY_TAX[n - 1];
		DifficultyScaleEntry late = DifficultyScale(n, 1, MapGen.BossRowFor(1) - 1);
		List<string> list = new List<string>();
		list.Add("打点 幕1×" + Num(row.Atk[0]) + "（後半×" + Num(late.Atk) + "）・幕2×" + Num(row.Atk[1]) + "・幕3×" + Num(row.Atk[2]));
		list.Add("HP 幕1後半×" + Num(late.Hp) + "・幕2×" + Num(row.Hp[1]) + "・幕3×" + Num(row.Hp[2]));
		List<string> parts = list;
		if (t.BossHeal < 1.0)
		{
			parts.Add("幕ボス撃破の回復" + (int)Math.Round(t.BossHeal * 100.0) + "%");
		}
		if (t.Campfire < 0.25)
		{
			parts.Add("焚き火" + (int)Math.Round(t.Campfire * 100.0) + "%");
		}
		if (t.EliteBonus > 0)
		{
			parts.Add("幕2/3のエリート+" + t.EliteBonus);
		}
		if (t.ShopPrice > 1.0)
		{
			parts.Add("物価×" + Num(t.ShopPrice));
		}
		if (t.RewardChoices < 0)
		{
			parts.Add("報酬の提示" + t.RewardChoices);
		}
		if (t.StartGuilt > 0)
		{
			parts.Add("開始時に仮初の烙印" + t.StartGuilt);
		}
		return n + ": " + string.Join("／", parts) + ((n < 3) ? "（易しめ）" : "");
	}

	private static string Num(double v)
	{
		return v.ToString("0.###", CultureInfo.InvariantCulture);
	}

	public static double DepthHpScale(int row, int act = 1)
	{
		if (row >= MapGen.BossRowFor(act))
		{
			return 1.0;
		}
		bool late = row >= (int)Math.Floor((double)MapGen.BossRowFor(act) / 2.0);
		double[] pair = (new double[3][]
		{
			new double[2] { 0.62, 0.72 },
			new double[2] { 1.05, 1.15 },
			new double[2] { 1.2, 1.3 }
		})[act - 1];
		if (!late)
		{
			return pair[0];
		}
		return pair[1];
	}

	public static MapNode? CurrentNode(RunState run)
	{
		if (run.Row < 0)
		{
			return null;
		}
		return run.Map[run.Row][run.Col];
	}

	public static IReadOnlyList<int> NextChoices(RunState run)
	{
		if (run.Row < 0)
		{
			List<int> all = new List<int>();
			for (int c = 0; c < run.Map[0].Count; c++)
			{
				all.Add(c);
			}
			return all;
		}
		if (run.Row >= run.Map.Count - 1)
		{
			return new List<int>();
		}
		return CurrentNode(run)?.Next ?? new List<int>();
	}

	public static RunState DebugLaunchCombat(RunState run, string encounterId)
	{
		RunState r = run;
		if (CurrentNode(r) == null)
		{
			bool placed = false;
			for (int row = 0; row < r.Map.Count; row++)
			{
				if (placed)
				{
					break;
				}
				for (int col = 0; col < r.Map[row].Count; col++)
				{
					if (placed)
					{
						break;
					}
					if (r.Map[row][col].Type == "battle")
					{
						r = r with
						{
							Row = row,
							Col = col
						};
						placed = true;
					}
				}
			}
		}
		return LaunchCombat(r, elite: false, encounterId);
	}

	public static RunState DebugOpenTreasure(RunState run)
	{
		return OpenTreasure(run);
	}

	public static RunState DebugRollRewards(RunState run)
	{
		return RollRewards(run);
	}

	public static RunState DebugOpenEvent(RunState run, string? eventId)
	{
		string text = eventId;
		if (text == null)
		{
			if (Content.AllEvents.Count <= 0)
			{
				throw new InvalidOperationException("イベントが無い");
			}
			text = Content.AllEvents[0].Id;
		}
		string id = text;
		Content.GetEventDef(id);
		return run with
		{
			EventId = id,
			Phase = "event",
			Combat = null,
			RewardOptions = null
		};
	}

	private static RunState LaunchCombat(RunState run, bool elite, string? encounterOverride = null)
	{
		MapNode node = CurrentNode(run);
		string encounterId = encounterOverride ?? node?.EncounterId;
		if (node == null || encounterId == null)
		{
			throw new InvalidOperationException("戦闘ノードではない");
		}
		(int Value, RngState Next) tuple = Rng.NextInt(run.Rng, 0, int.MaxValue);
		int combatSeed = tuple.Value;
		RngState rng = tuple.Next;
		DifficultyScaleEntry diff = DifficultyScale(run.Difficulty, run.Act, run.Row);
		double[] bossHpByAct = new double[3] { 1.35, 2.3, 2.4 };
		int[] bossStrByAct = new int[3] { 1, 1, 2 };
		double[] atkByAct = new double[3] { 1.0, 1.15, 1.15 };
		List<CardInstance> relicPerms = new List<CardInstance>();
		foreach (string relic in run.Relics)
		{
			RelicDef r = Content.GetRelicDef(relic);
			if ((r.Effects?.Count ?? 0) > 0 && (r.EliteBossOnly != true || elite || !(node.Type != "boss")))
			{
				relicPerms.Add(Content.BuildRelicPermanent(r));
			}
		}
		int setDamageReduction = 0;
		foreach (string id in run.Relics)
		{
			setDamageReduction += (Content.GetRelicDef(id).CombatRule?.SetDamageReduction).GetValueOrDefault();
		}
		bool expireToHand = run.Relics.Any((string id4) => (Content.GetRelicDef(id4).CombatRule?.ExpireToHand == true));
		int energyMaxRefBonus = 0;
		foreach (string id2 in run.Relics)
		{
			energyMaxRefBonus += (Content.GetRelicDef(id2).CombatRule?.EnergyMaxRefBonus).GetValueOrDefault();
		}
		int harvestKeep = 0;
		foreach (string id3 in run.Relics)
		{
			harvestKeep += (Content.GetRelicDef(id3).CombatRule?.HarvestKeep).GetValueOrDefault();
		}
		List<RelicDefCombatRule> rules = new List<RelicDefCombatRule>();
		foreach (string relic2 in run.Relics)
		{
			RelicDefCombatRule cr = Content.GetRelicDef(relic2).CombatRule;
			if (cr != null)
			{
				rules.Add(cr);
			}
		}
		int blockKeep = RuleSum((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.BlockKeep);
		int xBonus = RuleSum((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.XBonus);
		int hpLossReduce = RuleSum((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.HpLossReduce);
		int smallHitToOne = RuleSum((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.SmallHitToOne);
		int maxHpLossPerTurn = RuleSum((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.MaxHpLossPerTurn);
		int playCap = RuleSum((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.PlayCap);
		int artifact = RuleSum((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.Artifact);
		int train = RelicStateOf(run, "train");
		int capTotal = RuleSum((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.MaxHpLossCharges);
		int capLeft = capTotal - RelicStateOf(run, "remnantUsed");
		GameState combat = Combat.StartCombatWithOptions(combatSeed, run.Mode, encounterId, new CombatOptions
		{
			Deck = run.Deck,
			LeaderId = run.LeaderId,
			PlayerHp = run.Hp,
			PlayerMaxHp = run.MaxHp,
			EnemyHpScale = (elite ? 1.0 : (DepthHpScale(run.Row, run.Act) * ((node.Type == "boss") ? bossHpByAct[run.Act - 1] : 1.0))) * diff.Hp,
			EnemyStrength = ((node.Type == "boss") ? bossStrByAct[run.Act - 1] : 0) + (elite ? 0 : 0),
			EnemyAtkScale = ((elite || node.Type == "boss") ? 1.0 : atkByAct[run.Act - 1]) * diff.Atk,
			RelicPermanents = relicPerms,
			SetDamageReduction = setDamageReduction,
			ExpireToHand = expireToHand,
			EnergyMaxRefBonus = energyMaxRefBonus,
			HarvestKeep = harvestKeep,
			SetAnyCards = ((run.SetAnyCards == true) ? new bool?(true) : ((bool?)null)),
			RetainHand = (RuleAny((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.RetainHand) ? new bool?(true) : ((bool?)null)),
			EnergyCarry = (RuleAny((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.EnergyCarry) ? new bool?(true) : ((bool?)null)),
			BlockKeep = ((blockKeep > 0) ? new int?(blockKeep) : ((int?)null)),
			XBonus = ((xBonus > 0) ? new int?(xBonus) : ((int?)null)),
			HpLossReduce = ((hpLossReduce > 0) ? new int?(hpLossReduce) : ((int?)null)),
			SmallHitToOne = ((smallHitToOne > 0) ? new int?(smallHitToOne) : ((int?)null)),
			MaxHpLossPerTurn = ((maxHpLossPerTurn > 0 && (capTotal == 0 || capLeft > 0)) ? new int?(maxHpLossPerTurn) : ((int?)null)),
			MaxHpLossCharges = ((capTotal > 0 && capLeft > 0) ? new int?(capLeft) : ((int?)null)),
			DeathSave = ((RuleAny((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.DeathSave) && RelicStateOf(run, "lizardUsed") == 0) ? new bool?(true) : ((bool?)null)),
			PlayCap = ((playCap > 0) ? new int?(playCap) : ((int?)null)),
			HideIntents = (RuleAny((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.HideIntents) ? new bool?(true) : ((bool?)null)),
			BrandsPlayable = (RuleAny((RelicDefCombatRule relicDefCombatRule) => relicDefCombatRule.BrandsPlayable) ? new bool?(true) : ((bool?)null)),
			Artifact = ((artifact > 0) ? new int?(artifact) : ((int?)null)),
			StartGrowth = ((train > 0) ? new int?(train) : ((int?)null)),
			GoldAvailable = run.Gold
		});
		return run with
		{
			Rng = rng,
			Combat = combat,
			Phase = "combat",
			RewardOptions = null,
			CurrentElite = elite
		};
		bool RuleAny(Func<RelicDefCombatRule, bool?> f)
		{
			foreach (RelicDefCombatRule r2 in rules)
			{
				if (f(r2) == true)
				{
					return true;
				}
			}
			return false;
		}
		int RuleSum(Func<RelicDefCombatRule, int?> f)
		{
			int a = 0;
			foreach (RelicDefCombatRule r2 in rules)
			{
				a += f(r2).GetValueOrDefault();
			}
			return a;
		}
	}

	private static RunState EnterNode(RunState run)
	{
		RunState next = EnterNodeInner(run);
		return next with
		{
			LastRoomWasShop = (next.Phase == "shop")
		};
	}

	private static RunState EnterNodeInner(RunState run)
	{
		MapNode? mapNode = CurrentNode(run);
		if (mapNode == null)
		{
			throw new InvalidOperationException("ノードにいない");
		}
		switch (mapNode.Type)
		{
		case "boss":
		case "battle":
			return LaunchCombat(run, elite: false);
		case "elite":
			return LaunchCombat(run, elite: true);
		case "campfire":
			return run with
			{
				Phase = "campfire",
				Combat = null,
				RewardOptions = null,
				CampfireUpgradesUsed = 0
			};
		case "workshop":
			return run with
			{
				Phase = "workshop",
				Combat = null,
				RewardOptions = null,
				WorkshopFusesUsed = 0
			};
		case "shop":
			return OpenShop(run);
		case "event":
			return ResolveUnknown(run);
		case "treasure":
			return OpenTreasure(run);
		default:
			return run;
		}
	}

	private static string RelicRarity(string id)
	{
		return Content.GetRelicDef(id).Rarity ?? "common";
	}

	public static (IReadOnlyList<string> Options, RngState Rng) DrawRelicOptions(RunState run, string source, int count = 3)
	{
		List<string> pool = run.RelicQueue.Where((string id) => !run.Relics.Contains(id) && run.Act <= (Content.GetRelicDef(id).ActMax ?? 99) && run.Act >= Content.GetRelicDef(id).ActMin.GetValueOrDefault() && RelicAllowedForColors(Content.GetRelicDef(id), run.Colors)).ToList();
		RngState rng = run.Rng;
		List<string> picked = new List<string>();
		for (int n = 0; n < count; n++)
		{
			if (source == "boss")
			{
				if (!Take("boss") && !Take("rare") && !Take("uncommon") && !Take("common"))
				{
					break;
				}
			}
			else
			{
				if ((source == "shop" && n == 0 && Take("shop")) || (source == "event" && Take("event")))
				{
					continue;
				}
				(int Value, RngState Next) tuple = Rng.NextInt(rng, 0, 99);
				int roll = tuple.Value;
				RngState r1 = tuple.Next;
				rng = r1;
				string tier;
				if (roll < 50)
				{
					tier = "common";
				}
				else
				{
					tier = ((roll < 83) ? "uncommon" : "rare");
				}
				string[] array;
				if (tier == "common")
				{
					array = new string[3] { "common", "uncommon", "rare" };
				}
				else
				{
					array = ((!(tier == "uncommon")) ? new string[3] { "rare", "uncommon", "common" } : new string[3] { "uncommon", "common", "rare" });
				}
				bool any = false;
				string[] array2 = array;
				foreach (string t in array2)
				{
					if (Take(t))
					{
						any = true;
						break;
					}
				}
				if (!any)
				{
					break;
				}
			}
		}
		return (Options: picked, Rng: rng);
		bool Take(string text)
		{
			List<string> cands = pool.Where((string id) => RelicRarity(id) == text && !picked.Contains(id)).ToList();
			if (cands.Count == 0)
			{
				return false;
			}
			(int Value, RngState Next) tuple2 = Rng.NextInt(rng, 0, cands.Count - 1);
			int i = tuple2.Value;
			RngState next = tuple2.Next;
			rng = next;
			picked.Add(cands[i]);
			return true;
		}
	}

	private static RunState OpenTreasure(RunState run)
	{
		(IReadOnlyList<string> Options, RngState Rng) tuple = DrawRelicOptions(run, "chest");
		IReadOnlyList<string> options = tuple.Options;
		RngState rng = tuple.Rng;
		RunState baseRun = run with
		{
			Rng = rng,
			Combat = null,
			RewardOptions = null
		};
		if (options.Count == 0)
		{
			return baseRun with
			{
				Phase = "map"
			};
		}
		return baseRun with
		{
			Phase = "relic-reward",
			RelicOptions = options
		};
	}

	private static RunState ResolveUnknown(RunState run)
	{
		RunStateUnknownPity pity = run.UnknownPity ?? UnknownPityBase();
		MapNode node = CurrentNode(run);
		bool nextHasShop = false;
		if (node != null)
		{
			foreach (int i in node.Next)
			{
				if (run.Row + 1 < run.Map.Count && i < run.Map[run.Row + 1].Count && run.Map[run.Row + 1][i].Type == "shop")
				{
					nextHasShop = true;
					break;
				}
			}
		}
		bool tooPoor = run.Gold < 50;
		int shopPct = ((!(run.LastRoomWasShop | nextHasShop | tooPoor)) ? pity.Shop : 0);
		run = run with
		{
			Gold = run.Gold + RelicBonusSum(run, "goldPerUnknown")
		};
		int chestEvery = RelicBonusSum(run, "unknownChestEvery");
		if (chestEvery > 0)
		{
			int nUnknown = RelicStateOf(run, "unknownsSinceChest") + 1;
			if (nUnknown >= chestEvery)
			{
				return OpenTreasure(WithRelicState(run, "unknownsSinceChest", 0)with
				{
					EventId = null
				});
			}
			run = WithRelicState(run, "unknownsSinceChest", nUnknown);
		}
		var (roll, rng) = Rng.NextInt(run.Rng, 0, 99);
		if (roll < pity.Monster)
		{
			IReadOnlyList<string> fullPool = MapGen.TierFor(run.Act, run.Row);
			HashSet<string> recent = new HashSet<string>();
			int[] array = new int[2]
			{
				run.Row - 1,
				run.Row - 2
			};
			foreach (int r in array)
			{
				if (r < 0 || r >= run.Map.Count)
				{
					continue;
				}
				foreach (MapNode n2 in run.Map[r])
				{
					if ((!(n2.Type == "battle") && !(n2.Type == "elite")) || string.IsNullOrEmpty(n2.EncounterId))
					{
						continue;
					}
					foreach (EncounterMember m in Content.ResolveEncounter(n2.EncounterId))
					{
						recent.Add(m.EnemyId);
					}
				}
			}
			List<string> filtered = fullPool.Where((string encId) => !Content.ResolveEncounter(encId).Any((EncounterMember encounterMember) => recent.Contains(encounterMember.EnemyId))).ToList();
			IReadOnlyList<string> readOnlyList;
			if (filtered.Count <= 0)
			{
				readOnlyList = fullPool;
			}
			else
			{
				IReadOnlyList<string> readOnlyList2 = filtered;
				readOnlyList = readOnlyList2;
			}
			IReadOnlyList<string> pool = readOnlyList;
			var (i2, r2) = Rng.NextInt(rng, 0, pool.Count - 1);
			return LaunchCombat(run with
			{
				Rng = r2,
				UnknownPity = Bump("monster"),
				EventId = null
			}, elite: false, pool[i2]);
		}
		if (roll < pity.Monster + shopPct)
		{
			return OpenShop(run with
			{
				Rng = rng,
				UnknownPity = Bump("shop"),
				EventId = null
			});
		}
		if (roll < pity.Monster + shopPct + pity.Treasure)
		{
			return OpenTreasure(run with
			{
				Rng = rng,
				UnknownPity = Bump("treasure"),
				EventId = null
			});
		}
		(string Id, RngState Rng) tuple3 = PickEvent(run, rng);
		string eventId = tuple3.Id;
		RngState r3 = tuple3.Rng;
		EventDef def = Content.GetEventDef(eventId);
		return run with
		{
			Rng = r3,
			UnknownPity = Bump("event"),
			EventId = eventId,
			SeenEventIds = Append(run.SeenEventIds ?? new List<string>(), eventId),
			SeenShrineIds = (def.Kind == "shrine") ? Append(run.SeenShrineIds ?? new List<string>(), eventId) : (run.SeenShrineIds ?? new List<string>()),
			Phase = "event",
			Combat = null,
			RewardOptions = null
		};
		RunStateUnknownPity Bump(string hit)
		{
			return new RunStateUnknownPity
			{
				Monster = ((hit == "monster") ? 10 : (pity.Monster + 10)),
				Shop = ((hit == "shop") ? 3 : (pity.Shop + 3)),
				Treasure = ((hit == "treasure") ? 2 : (pity.Treasure + 2))
			};
		}
	}

	public static (string Id, RngState Rng) PickEvent(RunState run, RngState rng0)
	{
		IReadOnlyList<string> seen = run.SeenEventIds ?? new List<string>();
		IReadOnlyList<string> seenShrine = run.SeenShrineIds ?? new List<string>();
		List<EventDef> shrinePool = Content.AllEvents.Where((EventDef e) => InAct(e) && EventPlayable(run, e) && ((e.Kind == "shrine" && !seenShrine.Contains(e.Id)) || (e.Kind == "oneTime" && !seen.Contains(e.Id)))).ToList();
		List<EventDef> actPool = Content.AllEvents.Where((EventDef e) => (e.Kind ?? "act") == "act" && InAct(e) && !seen.Contains(e.Id) && EventPlayable(run, e)).ToList();
		(int Value, RngState Next) tuple = Rng.NextInt(rng0, 0, 99);
		int roll = tuple.Value;
		RngState item = tuple.Next;
		bool flag = roll < 25;
		List<EventDef> primary = (flag ? shrinePool : actPool);
		List<EventDef> fallback = (flag ? actPool : shrinePool);
		List<EventDef> pool = ((primary.Count > 0) ? primary : fallback);
		List<EventDef> replay = Content.AllEvents.Where((EventDef e) => InAct(e) && EventPlayable(run, e)).ToList();
		List<EventDef> final;
		if (pool.Count > 0)
		{
			final = pool;
		}
		else
		{
			final = ((replay.Count > 0) ? replay : Content.AllEvents.Where(InAct).ToList());
		}
		var (i, r2) = Rng.NextInt(item, 0, final.Count - 1);
		return (Id: final[i].Id, Rng: r2);
		bool InAct(EventDef e)
		{
			if (e.Act.HasValue)
			{
				return e.Act == run.Act;
			}
			return true;
		}
	}

	public static RunState OpenShop(RunState run)
	{
		LeaderDef leader = Content.GetLeaderDef(run.LeaderId);
		bool canRamp = run.Colors.Contains("green");
		int costCap = leader.EnergyMax + (canRamp ? 2 : 0);
		List<CardDef> collection = Content.AllCards.Where((CardDef c) => run.Colors.Contains(c.Color) && !REWARD_EXCLUDED.Contains(c.Id) && c.Cost <= CapFor(c)).ToList();
		RngState rng = run.Rng;
		List<ShopStateCards> cards = new List<ShopStateCards>();
		List<CardDef> remaining = new List<CardDef>(collection);
		while (cards.Count < 5 && remaining.Count > 0)
		{
			(int Value, RngState Next) tuple = Rng.NextInt(rng, 0, remaining.Count - 1);
			int idx = tuple.Value;
			rng = tuple.Next;
			CardDef def = remaining[idx];
			remaining.RemoveAt(idx);
			int pricedCost = ((def.XCost == true) ? 3 : def.Cost);
			int roll;
			(roll, rng) = Rng.NextInt(rng, 0, 10);
			cards.Add(new ShopStateCards
			{
				Id = def.Id,
				Price = JsFloor((double)(40 + pricedCost * 10 + roll) * ShopPriceRatio(run))
			});
		}
		List<CardDef> rarePool = remaining.Where((CardDef c) => c.Rarity == "rare").ToList();
		if (rarePool.Count > 0)
		{
			(int Value, RngState Next) tuple3 = Rng.NextInt(rng, 0, rarePool.Count - 1);
			int ri = tuple3.Value;
			rng = tuple3.Next;
			CardDef def2 = rarePool[ri];
			int pricedCost2 = ((def2.XCost == true) ? 3 : def2.Cost);
			cards.Add(new ShopStateCards
			{
				Id = def2.Id,
				Price = JsFloor((double)(120 + pricedCost2 * 10) * ShopPriceRatio(run))
			});
		}
		(IReadOnlyList<string> Options, RngState Rng) tuple4 = DrawRelicOptions(run with
		{
			Rng = rng
		}, "shop", 1);
		IReadOnlyList<string> shopRelics = tuple4.Options;
		rng = tuple4.Rng;
		string relicId = ((shopRelics.Count > 0) ? shopRelics[0] : null);
		List<ShopStateGears> gearShelf = new List<ShopStateGears>();
		HashSet<string> gearSeen = new HashSet<string>();
		for (int i = 0; i < 3; i++)
		{
			string gid;
			(gid, rng) = RollGearId(rng, atLeastUncommon: false);
			if (!gearSeen.Contains(gid))
			{
				gearSeen.Add(gid);
				gearShelf.Add(new ShopStateGears
				{
					Id = gid,
					Price = JsFloor((double)SHOP_GEAR_PRICE[Content.GetGearDef(gid).Rarity] * ShopPriceRatio(run))
				});
			}
		}
		List<DepartureOffer> leftovers = ((run.Act == 1) ? (run.Departure?.Leftovers ?? new List<DepartureOffer>()).Where((DepartureOffer o) => o.ShopPrice.HasValue).ToList() : new List<DepartureOffer>());
		ShopState shop = new ShopState
		{
			Cards = cards,
			RelicId = relicId,
			RelicPrice = JsFloor(150.0 * ShopPriceRatio(run)),
			Gears = gearShelf,
			ManaPrice = JsFloor(30.0 * ShopPriceRatio(run)),
			Departures = ((leftovers.Count > 0) ? leftovers.Select((DepartureOffer o) => new ShopStateDepartures
			{
				Id = o.Id,
				Price = JsFloor((double)o.ShopPrice.GetValueOrDefault() * ShopPriceRatio(run))
			}).ToList() : null)
		};
		int heal = RelicBonusSum(run, "shopHeal");
		return run with
		{
			Rng = rng,
			Shop = shop,
			Phase = "shop",
			Combat = null,
			RewardOptions = null,
			Hp = Math.Min(run.MaxHp, run.Hp + heal)
		};
		int CapFor(CardDef c)
		{
			if (c.Retain != true)
			{
				return costCap;
			}
			return leader.EnergyMax + (canRamp ? 1 : 0);
		}
	}

	public static IReadOnlyList<CardDef> RewardPool(RunState run)
	{
		LeaderDef leader = Content.GetLeaderDef(run.LeaderId);
		bool canRamp = run.Colors.Contains("green");
		int costCap = leader.EnergyMax + (canRamp ? 2 : 0);
		return Content.AllCards.Where((CardDef c) => run.Colors.Contains(c.Color) && !REWARD_EXCLUDED.Contains(c.Id) && c.Cost <= CapFor(c)).ToList();
		int CapFor(CardDef c)
		{
			if (c.Retain != true)
			{
				return costCap;
			}
			return leader.EnergyMax + (canRamp ? 1 : 0);
		}
	}

	public static bool EventChoiceNeedsCard(EventChoiceDef choice)
	{
		if (choice.RemoveCard != true && choice.UnexhaustCard != true && choice.UpgradeCard != true && choice.TransformCard != true)
		{
			return choice.DuplicateCard == true;
		}
		return true;
	}

	public static bool EventChoiceAvailable(RunState run, EventChoiceDef choice)
	{
		if (choice.RelicId != null && run.Relics.Contains(choice.RelicId))
		{
			return false;
		}
		if (choice.RequireGold.HasValue && run.Gold < choice.RequireGold.Value)
		{
			return false;
		}
		if (EventChoiceNeedsCard(choice))
		{
			return DefaultEventCardIndex(run, choice).HasValue;
		}
		return true;
	}

	public static bool EventPlayable(RunState run, EventDef def)
	{
		return def.Choices.Any((EventChoiceDef c) => EventChoiceAvailable(run, c));
	}

	public static int? DefaultEventCardIndex(RunState run, EventChoiceDef choice)
	{
		if (choice.UpgradeCard == true)
		{
			for (int i = 0; i < run.Deck.Count; i++)
			{
				if (Upgrade.CanUpgradeCard(run.Deck[i]))
				{
					return i;
				}
			}
			return null;
		}
		if (choice.TransformCard == true)
		{
			for (int j = 0; j < run.Deck.Count; j++)
			{
				if (CanTransformCard(run.Deck[j]))
				{
					return j;
				}
			}
			return null;
		}
		if (choice.RemoveCard == true)
		{
			if (run.Deck.Count == 0 || run.Deck.Count <= 5)
			{
				return null;
			}
			for (int k = 0; k < run.Deck.Count; k++)
			{
				if (run.Deck[k].Def.Id.StartsWith("status_", StringComparison.Ordinal))
				{
					return k;
				}
			}
			return 0;
		}
		if (choice.DuplicateCard == true)
		{
			if (run.Deck.Count <= 0)
			{
				return null;
			}
			return 0;
		}
		if (choice.UnexhaustCard == true)
		{
			for (int l = 0; l < run.Deck.Count; l++)
			{
				if (CanUnexhaustCard(run.Deck[l]))
				{
					return l;
				}
			}
			return null;
		}
		return null;
	}

	public static bool CanTransformCard(CardInstance c)
	{
		return !c.Def.Id.StartsWith("status_", StringComparison.Ordinal);
	}

	public static bool CanUnexhaustCard(CardInstance c)
	{
		CardDef d = c.Def;
		if (d.Exhaust != true || d.Cost < 1 || d.XCost == true || d.NecroCost.HasValue)
		{
			return false;
		}
		foreach (DeclarativeEffect e in d.Effects)
		{
			if (UNEXHAUST_BANNED.Contains(e.Effect))
			{
				return false;
			}
		}
		if (d.Modes != null)
		{
			foreach (CardMode mode in d.Modes)
			{
				foreach (DeclarativeEffect e2 in mode.Effects)
				{
					if (UNEXHAUST_BANNED.Contains(e2.Effect))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private static bool EventOutcomeIsFree(int? gold, int? hp, double? hpRatio, int? wounds, int? brands, int? timedCurses)
	{
		if (gold.GetValueOrDefault() >= 0 && hp.GetValueOrDefault() >= 0 && hpRatio.GetValueOrDefault() >= 0.0 && wounds.GetValueOrDefault() == 0 && brands.GetValueOrDefault() == 0)
		{
			return timedCurses.GetValueOrDefault() == 0;
		}
		return false;
	}

	private static bool EventChoiceIsFree(EventChoiceDef c)
	{
		if (!EventOutcomeIsFree(c.Gold, c.Hp, c.HpRatio, c.Wounds, c.Brands, c.TimedCurses))
		{
			return false;
		}
		if (c.Gamble == null)
		{
			return true;
		}
		if (EventOutcomeIsFree(c.Gamble.Win.Gold, c.Gamble.Win.Hp, null, c.Gamble.Win.Wounds, null, null))
		{
			return EventOutcomeIsFree(c.Gamble.Lose.Gold, c.Gamble.Lose.Hp, null, c.Gamble.Lose.Wounds, null, null);
		}
		return false;
	}

	private static int EventWorstHpLoss(RunState run, EventChoiceDef c)
	{
		int baseLoss = LossOf(c.Hp, c.HpRatio);
		if (c.Gamble == null)
		{
			return baseLoss;
		}
		return baseLoss + Math.Max(LossOf(c.Gamble.Win.Hp, null), LossOf(c.Gamble.Lose.Hp, null));
		int LossOf(int? hp, double? hpRatio)
		{
			return Math.Max(0, Math.Max(-hp.GetValueOrDefault(), -(int)Math.Truncate((double)run.MaxHp * hpRatio.GetValueOrDefault())));
		}
	}

	public static RunCommand_EventChoice DefaultEventChoice(RunState run)
	{
		EventDef def = Content.GetEventDef(run.EventId ?? "");
		for (int i = def.Choices.Count - 1; i >= 0; i--)
		{
			if (EventChoiceAvailable(run, def.Choices[i]) && EventChoiceIsFree(def.Choices[i]))
			{
				return WithCard(i);
			}
		}
		for (int i2 = def.Choices.Count - 1; i2 >= 0; i2--)
		{
			if (EventChoiceAvailable(run, def.Choices[i2]) && EventWorstHpLoss(run, def.Choices[i2]) < run.Hp)
			{
				return WithCard(i2);
			}
		}
		for (int i3 = def.Choices.Count - 1; i3 >= 0; i3--)
		{
			if (EventChoiceAvailable(run, def.Choices[i3]))
			{
				return WithCard(i3);
			}
		}
		return WithCard(def.Choices.Count - 1);
		RunCommand_EventChoice WithCard(int index)
		{
			EventChoiceDef c = def.Choices[index];
			int? ci = (EventChoiceNeedsCard(c) ? DefaultEventCardIndex(run, c) : ((int?)null));
			if (ci.HasValue)
			{
				return new RunCommand_EventChoice
				{
					Index = index,
					CardIndex = ci
				};
			}
			return new RunCommand_EventChoice
			{
				Index = index
			};
		}
	}

	private static RunState ApplyEventChoice(RunState run, int choiceIndex, int? cardIndex)
	{
		EventDef def = Content.GetEventDef(run.EventId ?? throw new InvalidOperationException("イベントノードではない"));
		EventChoiceDef choice = ((choiceIndex >= 0 && choiceIndex < def.Choices.Count) ? def.Choices[choiceIndex] : null);
		if (choice == null)
		{
			throw new InvalidOperationException($"不正な選択肢: {choiceIndex}");
		}
		return ApplyChoiceDef(run, choice, cardIndex, "map");
	}

	public static RunState ApplyChoiceDef(RunState run, EventChoiceDef choice, int? cardIndex, string resume)
	{
		if (choice.RequireGold.HasValue && run.Gold < choice.RequireGold.Value)
		{
			throw new InvalidOperationException($"ゴールドが足りない (必要{choice.RequireGold.Value}G)");
		}
		RunState next = run;
		RngState rng = run.Rng;
		ApplyOutcome(choice.Gold, choice.Hp, choice.HpRatio, choice.Wounds, choice.Brands, choice.TimedCurses);
		if (choice.MaxHp.HasValue && choice.MaxHp.Value != 0)
		{
			next = next with
			{
				MaxHp = next.MaxHp + choice.MaxHp.Value,
				Hp = next.Hp + choice.MaxHp.Value
			};
		}
		if (choice.AddRandomCards.HasValue && choice.AddRandomCards.Value != 0)
		{
			IReadOnlyList<CardDef> pool = RewardPool(run);
			for (int i = 0; i < choice.AddRandomCards.Value; i++)
			{
				if (pool.Count <= 0)
				{
					break;
				}
				(int Value, RngState Next) tuple = Rng.NextInt(rng, 0, pool.Count - 1);
				int idx = tuple.Value;
				rng = tuple.Next;
				next = AddCardsToRunDeck(next, new List<CardInstance>
				{
					new CardInstance
					{
						Uid = $"event_a{run.Act}_r{run.Row}_{i}_{pool[idx].Id}",
						Def = pool[idx]
					}
				});
			}
		}
		if (choice.Relic == true)
		{
			IReadOnlyList<string> drawn;
			(drawn, rng) = DrawRelicOptions(next with
			{
				Rng = rng
			}, "event", 1);
			if (drawn.Count > 0)
			{
				next = GainRelic(next, drawn[0]);
			}
		}
		if (choice.RelicId != null && !next.Relics.Contains(choice.RelicId))
		{
			next = GainRelic(next, choice.RelicId);
		}
		if (choice.UnexhaustCard == true)
		{
			CardInstance card = ((cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null);
			if (card == null)
			{
				throw new InvalidOperationException("対象カードを cardIndex で指定する");
			}
			if (!CanUnexhaustCard(card))
			{
				throw new InvalidOperationException(card.Def.Name + " の消滅は外せない");
			}
			List<DeclarativeEffect> effs = new List<DeclarativeEffect>(card.Def.Effects)
			{
				new DeclarativeEffect
				{
					Trigger = "onPlay",
					Effect = "gainEnergy",
					Amount = 1
				}
			};
			CardDef soulDef = card.Def with
			{
				Exhaust = null,
				Name = card.Def.Name + "・魂",
				Effects = effs
			};
			int ci = cardIndex.Value;
			next = next with
			{
				Deck = next.Deck.Select((CardInstance cardInstance, int num) => (num != ci) ? cardInstance : cardInstance with
				{
					Def = soulDef
				}).ToList()
			};
		}
		if (choice.RemoveCard == true)
		{
			if (((cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null) == null)
			{
				throw new InvalidOperationException("対象カードを cardIndex で指定する");
			}
			if (next.Deck.Count <= 5)
			{
				throw new InvalidOperationException("これ以上デッキを減らせない");
			}
			next = next with
			{
				Deck = next.Deck.Where((CardInstance _, int num) => num != cardIndex.Value).ToList()
			};
		}
		if (choice.UpgradeCard == true)
		{
			CardInstance card2 = ((cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null);
			if (card2 == null)
			{
				throw new InvalidOperationException("対象カードを cardIndex で指定する");
			}
			if (Upgrade.IsUpgraded(card2))
			{
				throw new InvalidOperationException("すでに鍛えられている");
			}
			if (Upgrade.UpgradeTier(card2.Def) == "none")
			{
				throw new InvalidOperationException(card2.Def.Name + " は鍛えられない");
			}
			next = next with
			{
				Deck = next.Deck.Select((CardInstance cardInstance, int num) => (num != cardIndex.Value) ? cardInstance : Upgrade.UpgradeCard(cardInstance)).ToList()
			};
		}
		if (choice.TransformCard == true)
		{
			if (((cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null) == null)
			{
				throw new InvalidOperationException("対象カードを cardIndex で指定する");
			}
			next = TransformCardAt(next with
			{
				Rng = rng
			}, cardIndex.Value, upgrade: false);
			rng = next.Rng;
		}
		if (choice.DuplicateCard == true)
		{
			CardInstance card3 = ((cardIndex.HasValue && cardIndex.Value >= 0 && cardIndex.Value < next.Deck.Count) ? next.Deck[cardIndex.Value] : null);
			if (card3 == null)
			{
				throw new InvalidOperationException("対象カードを cardIndex で指定する");
			}
			CardInstance copy = new CardInstance
			{
				Uid = $"dup_a{run.Act}_r{run.Row}_{card3.Uid}",
				Def = card3.Def
			};
			next = next with
			{
				Deck = Append(next.Deck, copy)
			};
		}
		if (choice.UpgradeRandomCards.HasValue && choice.UpgradeRandomCards.Value != 0)
		{
			for (int i2 = 0; i2 < choice.UpgradeRandomCards.Value; i2++)
			{
				List<int> idxs = new List<int>();
				for (int j = 0; j < next.Deck.Count; j++)
				{
					CardInstance c = next.Deck[j];
					if (!Upgrade.IsUpgraded(c) && Upgrade.UpgradeTier(c.Def) != "none")
					{
						idxs.Add(j);
					}
				}
				if (idxs.Count == 0)
				{
					break;
				}
				(int Value, RngState Next) tuple3 = Rng.NextInt(rng, 0, idxs.Count - 1);
				int pick = tuple3.Value;
				rng = tuple3.Next;
				int target = idxs[pick];
				next = next with
				{
					Deck = next.Deck.Select((CardInstance cardInstance, int num) => (num != target) ? cardInstance : Upgrade.UpgradeCard(cardInstance)).ToList()
				};
			}
		}
		if (choice.RemoveAllWounds == true)
		{
			next = next with
			{
				Deck = next.Deck.Where((CardInstance cardInstance) => cardInstance.Def.Id != Content.WOUND_DEF.Id).ToList()
			};
		}
		if (choice.Gamble != null)
		{
			(int Value, RngState Next) tuple4 = Rng.NextInt(rng, 0, 999);
			int roll = tuple4.Value;
			rng = tuple4.Next;
			EventOutcome o = (((double)roll < choice.Gamble.Chance * 1000.0) ? choice.Gamble.Win : choice.Gamble.Lose);
			ApplyOutcome(o.Gold, o.Hp, null, o.Wounds, null, null);
		}
		if (choice.Gears != null)
		{
			foreach (string id in choice.Gears)
			{
				next = AddGear(next, id, $"a{run.Act}_r{run.Row}_{id}");
			}
		}
		if (choice.Mana.HasValue && choice.Mana.Value != 0)
		{
			next = next with
			{
				Mana = Math.Max(0, Math.Min(50, ManaOf(next) + choice.Mana.Value))
			};
		}
		if (choice.AddCardIds != null)
		{
			List<CardInstance> list = choice.AddCardIds.Select((string text, int value) => new CardInstance
			{
				Uid = $"given_a{run.Act}_r{run.Row}_{value}_{text}",
				Def = Content.GetCardDef(text)
			}).ToList();
			next = AddCardsToRunDeck(next, list);
		}
		if (next.Hp <= 0)
		{
			return next with
			{
				Rng = rng,
				Hp = 0,
				Phase = "lost"
			};
		}
		return EnterPendingChoice(next with
		{
			Rng = rng,
			Phase = resume
		}, resume);
		void ApplyOutcome(int? gold, int? hp, double? hpRatio, int? wounds, int? brands, int? timedCurses)
		{
			if (gold.HasValue && gold.Value != 0)
			{
				next = next with
				{
					Gold = Math.Max(0, next.Gold + gold.Value)
				};
			}
			if (hp.HasValue && hp.Value != 0)
			{
				next = next with
				{
					Hp = Math.Min(next.MaxHp, next.Hp + hp.Value)
				};
			}
			if (hpRatio.HasValue && hpRatio.Value != 0.0)
			{
				next = next with
				{
					Hp = Math.Min(next.MaxHp, next.Hp + JsTrunc((double)next.MaxHp * hpRatio.Value))
				};
			}
			if (wounds.HasValue && wounds.Value != 0)
			{
				List<CardInstance> list2 = new List<CardInstance>();
				for (int k = 0; k < wounds.Value; k++)
				{
					list2.Add(new CardInstance
					{
						Uid = $"wound_a{run.Act}_r{run.Row}_{k}",
						Def = Content.WOUND_DEF
					});
				}
				next = next with
				{
					Deck = Concat(next.Deck, list2)
				};
			}
			if (brands.HasValue && brands.Value != 0)
			{
				List<CardInstance> list3 = new List<CardInstance>();
				for (int l = 0; l < brands.Value; l++)
				{
					list3.Add(new CardInstance
					{
						Uid = $"brand_a{run.Act}_r{run.Row}_{l}",
						Def = Content.BRAND_DEF
					});
				}
				next = AddCardsToRunDeck(next, list3);
			}
			if (timedCurses.HasValue && timedCurses.Value != 0)
			{
				List<CardInstance> list4 = new List<CardInstance>();
				for (int m = 0; m < timedCurses.Value; m++)
				{
					list4.Add(new CardInstance
					{
						Uid = $"guilt_a{run.Act}_r{run.Row}_{m}",
						Def = Content.GUILT_DEF,
						ExpiresAfterBattles = 5
					});
				}
				next = AddCardsToRunDeck(next, list4);
			}
		}
	}

	public static RunState RollDepartureOffers(RunState run)
	{
		RngState rng = run.Rng;
		List<DepartureOffer> offers = new List<DepartureOffer>();
		List<string> usedRelics = new List<string>();
		List<string> usedCards = new List<string>();
		foreach (DepartureTemplate t in Content.AllDepartures)
		{
			EventChoiceDef choice = t.Choice with { };
			string name = t.Name;
			if (t.RelicRarity != null)
			{
				List<string> pool = run.RelicQueue.Where((string text) => RelicRarity(text) == t.RelicRarity && !usedRelics.Contains(text) && !run.Relics.Contains(text) && run.Act <= (Content.GetRelicDef(text).ActMax ?? 99) && run.Act >= Content.GetRelicDef(text).ActMin.GetValueOrDefault() && RelicAllowedForColors(Content.GetRelicDef(text), run.Colors)).ToList();
				if (pool.Count == 0)
				{
					continue;
				}
				(int Value, RngState Next) tuple = Rng.NextInt(rng, 0, pool.Count - 1);
				int j = tuple.Value;
				rng = tuple.Next;
				choice = choice with
				{
					RelicId = pool[j]
				};
				usedRelics.Add(pool[j]);
				name = Content.GetRelicDef(pool[j]).Name;
			}
			if (t.GearCount.GetValueOrDefault() > 0)
			{
				List<string> ids = new List<string>();
				int guard = 0;
				while (ids.Count < t.GearCount.GetValueOrDefault() && guard < 20)
				{
					string id;
					(id, rng) = RollGearId(rng, atLeastUncommon: false);
					if (!ids.Contains(id))
					{
						ids.Add(id);
					}
					guard++;
				}
				choice = choice with
				{
					Gears = ids
				};
			}
			if (t.CardPick != null && t.CardPick.Count > 0)
			{
				DepartureTemplateCardPick pick = t.CardPick;
				List<CardDef> pool2 = (from c in RewardPool(run)
					where c.Rarity == pick.Rarity && !usedCards.Contains(c.Id)
					select c).ToList();
				List<string> ids2 = new List<string>();
				int guard2 = 0;
				while (ids2.Count < pick.Count && ids2.Count < pool2.Count && guard2 < 20)
				{
					int j2;
					(j2, rng) = Rng.NextInt(rng, 0, pool2.Count - 1);
					if (!ids2.Contains(pool2[j2].Id))
					{
						ids2.Add(pool2[j2].Id);
					}
					guard2++;
				}
				if (ids2.Count == 0)
				{
					continue;
				}
				usedCards.AddRange(ids2);
				choice = choice with
				{
					AddCardIds = ids2
				};
				name = string.Join("・", ids2.Select((string id2) => Content.GetCardDef(id2).Name));
			}
			offers.Add(new DepartureOffer
			{
				Id = t.Id,
				Kind = t.Kind,
				Name = name,
				Text = t.Text,
				Choice = choice,
				Price = t.Price,
				ShopPrice = t.ShopPrice
			});
		}
		return run with
		{
			Rng = rng,
			Gold = run.Gold + 100,
			Departure = new RunStateDeparture
			{
				Offers = offers,
				Bought = new List<string>(),
				Leftovers = new List<DepartureOffer>()
			},
			Phase = "departure"
		};
	}

	public static bool DepartureOfferBought(RunState run, DepartureOffer offer)
	{
		return (run.Departure?.Bought ?? new List<string>()).Contains(offer.Id);
	}

	public static bool DepartureOfferAvailable(RunState run, DepartureOffer offer)
	{
		if (!DepartureOfferBought(run, offer) && run.Gold >= offer.Price)
		{
			return EventChoiceAvailable(run, offer.Choice);
		}
		return false;
	}

	public static RunCommand DefaultDepartureCommand(RunState run)
	{
		IReadOnlyList<DepartureOffer> offers = run.Departure?.Offers ?? new List<DepartureOffer>();
		if ((run.Departure?.Bought ?? new List<string>()).Count == 0)
		{
			for (int i = 0; i < offers.Count; i++)
			{
				DepartureOffer offer = offers[i];
				if (!(offer.Kind != "service") && DepartureOfferAvailable(run, offer))
				{
					int? cardIndex = (EventChoiceNeedsCard(offer.Choice) ? DefaultEventCardIndex(run, offer.Choice) : ((int?)null));
					if (cardIndex.HasValue)
					{
						return new RunCommand_BuyDeparture
						{
							Index = i,
							CardIndex = cardIndex
						};
					}
					return new RunCommand_BuyDeparture
					{
						Index = i
					};
				}
			}
		}
		return new RunCommand_LeaveDeparture();
	}

	public static RunState CreateRun(int seed, string mode, string leaderId = "leader_green", string? deckId = null, int difficulty = 3, RunOptions? opts = null)
	{
		LeaderDef leader = Content.GetLeaderDef(leaderId);
		IReadOnlyList<string>? source = leader.RunDeckChoices ?? new List<string> { leader.RunDeckId };
		string chosenDeck = deckId ?? leader.RunDeckId;
		if (!source.Contains(chosenDeck))
		{
			throw new InvalidOperationException("このリーダーでは選べない初期デッキ: " + chosenDeck);
		}
		RngState rng = Rng.Create(seed);
		int level = DifficultyLevel(difficulty);
		DifficultyTax tax = DifficultyTaxOf(level);
		(IReadOnlyList<IReadOnlyList<MapNode>> Map, RngState Rng) tuple = MapGen.GenerateMap(rng, 1, allowWorkshop: true, EliteCountFor(level, 1));
		IReadOnlyList<IReadOnlyList<MapNode>> map = tuple.Map;
		RngState item = tuple.Rng;
		bool canSet = Content.AllCards.Any((CardDef c) => leader.Colors.Contains(c.Color) && c.Type == "reaction");
		List<string> relicIds = (from r in Content.AllRelics
			where RelicAllowedForColors(r, leader.Colors)
			select r.Id into id
			where canSet || !SET_RELICS.Contains(id)
			select id).ToList();
		(string[] Items, RngState Next) tuple2 = Rng.Shuffle(item, relicIds);
		string[] relicQueue = tuple2.Items;
		RngState rngAfterRelics = tuple2.Next;
		RunState baseRun = new RunState
		{
			Seed = seed,
			Mode = mode,
			LeaderId = leaderId,
			SetAnyCards = (((object)opts != null && opts.SetAnyCards == true) ? new bool?(true) : ((bool?)null)),
			Colors = leader.Colors,
			Difficulty = level,
			Rng = rngAfterRelics,
			Deck = Concat(Content.BuildDeck(chosenDeck), from i in Enumerable.Range(0, tax.StartGuilt)
				select new CardInstance
				{
					Uid = "guilt_start_" + i,
					Def = Content.GUILT_DEF,
					ExpiresAfterBattles = 5
				}),
			Hp = leader.MaxHp,
			MaxHp = leader.MaxHp,
			Act = 1,
			Map = map,
			Row = -1,
			Col = 0,
			BattlesWon = 0,
			Gold = 50,
			RemovalCount = 0,
			UpgradeCount = 0,
			Shop = null,
			Phase = "map",
			Combat = null,
			RewardOptions = null,
			Picks = new List<string>(),
			Relics = new List<string>(),
			RelicQueue = relicQueue,
			RelicOptions = null,
			CurrentElite = false,
			VictoryHealBonus = 0,
			RewardChoicesBonus = 0,
			CampfireRatio = tax.Campfire,
			GoldPerVictoryBonus = 0,
			CampfireForgeBonus = 0,
			CampfireUpgradesUsed = 0,
			UnknownPity = UnknownPityBase(),
			LastRoomWasShop = false,
			EventId = null,
			SeenEventIds = new List<string>(),
			SeenShrineIds = new List<string>(),
			Gears = new List<GearInstance>(),
			Mana = 0,
			GearPity = 60,
			SeenGearIds = new List<string>(),
			GearOption = null
		};
		if ((object)opts == null || opts.Departure != false)
		{
			return RollDepartureOffers(baseRun);
		}
		return baseRun;
	}

	public static RunState ReplayInitialRun(ReplayOrigin origin)
	{
		if (origin.Kind == "checkpoint" && origin.Checkpoint != null)
		{
			return CreateDebugCheckpointRun(origin.Seed, "set-confirm", origin.LeaderId, origin.Checkpoint);
		}
		return CreateRun(origin.Seed, "set-confirm", origin.LeaderId, origin.DeckId, origin.Difficulty ?? 3, new RunOptions
		{
			SetAnyCards = ((origin.SetAnyCards == true) ? new bool?(true) : ((bool?)null))
		});
	}

	public static (List<RunState> States, string? Error) ReplayStates(RunJournal journal)
	{
		List<RunState> states = new List<RunState> { ReplayInitialRun(journal.Origin) };
		for (int i = 0; i < journal.Commands.Count; i++)
		{
			try
			{
				states.Add(ApplyRunCommand(states[states.Count - 1], journal.Commands[i]));
			}
			catch (Exception ex)
			{
				return (States: states, Error: $"コマンド{i + 1}/{journal.Commands.Count}で再現が分岐 (データ変更の可能性): {ex.Message}");
			}
		}
		return (States: states, Error: null);
	}

	public static RunState CreateDebugCheckpointRun(int seed, string mode, string leaderId, ReplayOriginCheckpoint opts, IReadOnlyList<string>? gearIds = null, int? mana = null)
	{
		RunState baseRun = CreateRun(seed, mode, leaderId, null, opts.Difficulty ?? 3, new RunOptions
		{
			Departure = false
		});
		int act = Math.Min(3, Math.Max(1, opts.Act));
		(IReadOnlyList<IReadOnlyList<MapNode>> Map, RngState Rng) tuple = MapGen.GenerateMap(baseRun.Rng, act, allowWorkshop: true, EliteCountFor(baseRun.Difficulty, act));
		IReadOnlyList<IReadOnlyList<MapNode>> map = tuple.Map;
		RngState rng = tuple.Rng;
		RunState run = baseRun with
		{
			Act = act,
			Map = map,
			Rng = rng,
			Row = -1,
			Col = 0,
			Deck = Content.BuildDeck(opts.DeckId),
			BattlesWon = act switch
			{
				2 => 10, 
				3 => 19, 
				_ => 0, 
			},
			Gold = (opts.Gold ?? 150)
		};
		foreach (string id in opts.RelicIds ?? new List<string>())
		{
			Content.GetRelicDef(id);
			run = GainRelic(run with
			{
				RelicQueue = run.RelicQueue.Where((string q) => q != id).ToList()
			}, id);
			if (run.PendingRelicChoice != null)
			{
				run = run with
				{
					PendingRelicChoice = null
				};
			}
		}
		if (gearIds != null)
		{
			foreach (string gid in gearIds)
			{
				run = AddGear(run, gid, "cp_" + gid);
			}
		}
		else if (act >= 2)
		{
			int want = ((act >= 3) ? 4 : 3);
			RngState r = run.Rng;
			for (int i = 0; i < want; i++)
			{
				(string Id, RngState Rng) tuple2 = RollGearId(r, atLeastUncommon: false);
				string gid2 = tuple2.Id;
				r = tuple2.Rng;
				run = AddGear(run with
				{
					Rng = r
				}, gid2, "cp" + i + "_" + gid2);
				r = run.Rng;
			}
		}
		run = run with
		{
			Mana = Math.Min(50, mana ?? ((act >= 3) ? 50 : ((act >= 2) ? 30 : 0)))
		};
		double ratio = Math.Min(1.0, Math.Max(0.05, opts.HpRatio ?? 1.0));
		return run with
		{
			Hp = Math.Max(1, JsRound((double)run.MaxHp * ratio))
		};
	}

	public static IReadOnlyList<string> AxesOf(CardDef def)
	{
		List<DeclarativeEffect> all = new List<DeclarativeEffect>(def.Effects);
		if (def.Modes != null)
		{
			foreach (CardMode m in def.Modes)
			{
				all.AddRange(m.Effects);
			}
		}
		List<string> order = new List<string>();
		HashSet<string> seen = new HashSet<string>();
		if (def.Axis != null)
		{
			foreach (string item in def.Axis)
			{
				Add(item);
			}
		}
		foreach (DeclarativeEffect e in all)
		{
			if (EFFECT_AXIS.TryGetValue(e.Effect, out string byEffect) && !string.IsNullOrEmpty(byEffect))
			{
				Add(byEffect);
			}
			if (TRIGGER_AXIS.TryGetValue(e.Trigger, out string byTrigger) && !string.IsNullOrEmpty(byTrigger))
			{
				Add(byTrigger);
			}
			if (e.ExhaustThreshold.HasValue)
			{
				Add("graveyard");
			}
			if (e.Pierce == true)
			{
				Add("trample");
			}
		}
		if (def.ExhaustCost.HasValue)
		{
			Add("graveyard");
		}
		if (def.ImpulseCost.HasValue)
		{
			Add("impulse");
		}
		if (def.Retainer == true)
		{
			Add("retinue");
		}
		if (def.Cascade.GetValueOrDefault() > 0)
		{
			Add("cascade");
		}
		return order;
		void Add(string s)
		{
			if (seen.Add(s))
			{
				order.Add(s);
			}
		}
	}

	private static RunState RollRewards(RunState run)
	{
		LeaderDef leader = Content.GetLeaderDef(run.LeaderId);
		bool canRamp = run.Colors.Contains("green");
		int costCap = leader.EnergyMax + (canRamp ? 2 : 0);
		List<CardDef> remaining = new List<CardDef>(Content.AllCards.Where((CardDef c) => run.Colors.Contains(c.Color) && !REWARD_EXCLUDED.Contains(c.Id) && c.Cost <= CapFor(c)).ToList());
		List<string> picked = new List<string>();
		RngState rng = run.Rng;
		int want = Math.Max(1, leader.RewardChoices + run.RewardChoicesBonus + DifficultyTaxOf(run.Difficulty).RewardChoices);
		while (picked.Count < want && remaining.Count > 0)
		{
			(int Value, RngState Next) tuple = Rng.NextInt(rng, 0, 99);
			int roll = tuple.Value;
			rng = tuple.Next;
			bool eliteEscaped = run.Combat != null && run.Combat.Enemies.Any((EnemyState e) => e.Fled == true);
			string[] array;
			if (!run.CurrentElite || picked.Count != 0 || eliteEscaped)
			{
				if (roll >= 3)
				{
					array = ((roll >= 40) ? new string[1] { "common" } : new string[2] { "uncommon", "common" });
				}
				else
				{
					array = new string[3] { "rare", "uncommon", "common" };
				}
			}
			else
			{
				array = new string[3] { "rare", "uncommon", "common" };
			}
			List<CardDef> candidates = new List<CardDef>();
			string[] array2 = array;
			foreach (string r in array2)
			{
				candidates = remaining.Where((CardDef c) => RarityOf(c) == r).ToList();
				if (candidates.Count > 0)
				{
					break;
				}
			}
			if (candidates.Count == 0)
			{
				candidates = remaining;
			}
			(int Value, RngState Next) tuple2 = Rng.NextInt(rng, 0, candidates.Count - 1);
			int idx = tuple2.Value;
			rng = tuple2.Next;
			CardDef chosen = candidates[idx];
			remaining.RemoveAt(remaining.FindIndex((CardDef x) => (object)x == chosen));
			if (chosen.FusionCatalyst == null || !picked.Any((string id) => Content.GetCardDef(id).FusionCatalyst != null))
			{
				picked.Add(chosen.Id);
			}
		}
		bool isBossNode = CurrentNode(run)?.Type == "boss";
		bool num2 = run.CurrentElite | isBossNode;
		string gearOption = null;
		int pity = run.GearPity ?? 60;
		if (num2)
		{
			(string Id, RngState Rng) tuple3 = RollGearId(rng, atLeastUncommon: true);
			string gid = tuple3.Id;
			rng = tuple3.Rng;
			gearOption = gid;
		}
		else
		{
			int roll2;
			(roll2, rng) = Rng.NextInt(rng, 0, 99);
			if (roll2 < pity)
			{
				(string Id, RngState Rng) tuple5 = RollGearId(rng, atLeastUncommon: false);
				string gid2 = tuple5.Id;
				rng = tuple5.Rng;
				gearOption = gid2;
				pity = 60;
			}
			else
			{
				pity += 10;
			}
		}
		return run with
		{
			Rng = rng,
			RewardOptions = picked,
			GearOption = gearOption,
			GearPity = pity,
			Phase = "reward"
		};
		int CapFor(CardDef c)
		{
			if (c.Retain != true)
			{
				return costCap;
			}
			return leader.EnergyMax + (canRamp ? 1 : 0);
		}
		static string RarityOf(CardDef c)
		{
			return c.Rarity ?? "common";
		}
	}

	private static RunState AfterVictory(RunState run, GameState combat)
	{
		bool isBoss = CurrentNode(run)?.Type == "boss";
		if (isBoss && run.Act >= 3)
		{
			return run with
			{
				Combat = combat,
				Hp = combat.Player.Hp,
				MaxHp = Math.Max(run.MaxHp, combat.Player.MaxHp),
				BattlesWon = run.BattlesWon + 1,
				Phase = "won"
			};
		}
		int rescueHeal = (((double)combat.Player.Hp <= (double)run.MaxHp * 0.3) ? run.VictoryHealBonus : 0);
		int maxHp = Math.Max(run.MaxHp, combat.Player.MaxHp);
		int hp = (isBoss ? Math.Min(maxHp, combat.Player.Hp + JsFloor((double)maxHp * DifficultyTaxOf(run.Difficulty).BossHeal)) : Math.Min(maxHp, combat.Player.Hp + rescueHeal + RelicBonusSum(run, "victoryHealFlat")));
		RngState rng = run.Rng;
		(int Value, RngState Next) tuple = Rng.NextInt(rng, 12, 18);
		int baseGold = tuple.Value;
		rng = tuple.Next;
		int gained = baseGold;
		if (run.CurrentElite)
		{
			(int Value, RngState Next) tuple2 = Rng.NextInt(rng, 30, 40);
			int bonus = tuple2.Value;
			rng = tuple2.Next;
			gained += bonus;
		}
		if (isBoss)
		{
			(int Value, RngState Next) tuple3 = Rng.NextInt(rng, 40, 50);
			int bonus2 = tuple3.Value;
			rng = tuple3.Next;
			gained += bonus2;
		}
		gained += run.GoldPerVictoryBonus;
		if (run.CurrentElite)
		{
			gained += RelicBonusSum(run, "eliteGoldBonus");
		}
		gained = JsFloor((double)gained * GoldMultiplier(run));
		int fledLoss = 0;
		foreach (EnemyState e in combat.Enemies)
		{
			if (e.Fled == true)
			{
				fledLoss += e.StolenGold.GetValueOrDefault();
			}
		}
		int bounty = combat.Enemies.Count((EnemyState enemyState) => enemyState.Fled != true && enemyState.StolenGold.GetValueOrDefault() > 0) * 10;
		gained += bounty - fledLoss;
		List<CardInstance> deckAfterCurses = (from c in run.Deck
			select (!c.ExpiresAfterBattles.HasValue) ? c : c with
			{
				ExpiresAfterBattles = c.ExpiresAfterBattles.Value - 1
			} into c
			where !c.ExpiresAfterBattles.HasValue || c.ExpiresAfterBattles.Value > 0
			select c).ToList();
		RunState next = run with
		{
			Rng = rng,
			Combat = combat,
			Hp = hp,
			Deck = deckAfterCurses,
			MaxHp = maxHp,
			BattlesWon = run.BattlesWon + 1,
			Gold = Math.Max(0, run.Gold + gained),
			RewardRoundsLeft = ((!run.CurrentElite && !isBoss) ? RelicBonusSum(run, "extraRewardRounds") : 0),
			Mana = Math.Min(50, run.Mana.GetValueOrDefault() + ((run.CurrentElite | isBoss) ? 10 : 5))
		};
		if (combat.DeathSaveUsed == true)
		{
			next = WithRelicState(next, "lizardUsed", 1);
		}
		next = SettleHpLossCap(next, combat);
		foreach (string id in new List<string>(next.Relics))
		{
			if (Content.GetRelicDef(id).ExpiresAfterBattles.HasValue)
			{
				int left = RelicStateOf(next, "exp_" + id) - 1;
				next = ((left > 0) ? WithRelicState(next, "exp_" + id, left) : WithRelicState(next, "exp_" + id, 0)with
				{
					Relics = next.Relics.Where((string r) => r != id).ToList()
				});
			}
		}
		if (run.CurrentElite | isBoss)
		{
			var (options, rng2) = DrawRelicOptions(next, isBoss ? "boss" : "elite");
			if (options.Count > 0)
			{
				int picks = ((!run.CurrentElite || isBoss) ? 1 : (1 + RelicBonusSum(next, "eliteRelicPicks")));
				return next with
				{
					Rng = rng2,
					Phase = "relic-reward",
					RelicOptions = options,
					RelicPicksLeft = ((picks > 1) ? new int?(picks) : ((int?)null))
				};
			}
		}
		return RollRewards(next);
	}

	private static RunState FinishReward(RunState run)
	{
		if (run.GearOption != null)
		{
			return run;
		}
		if (run.RewardOptions != null)
		{
			return run;
		}
		return AdvanceActIfBossCleared(run);
	}

	private static RunState AdvanceActIfBossCleared(RunState run)
	{
		if (CurrentNode(run)?.Type != "boss" || run.Act >= 3)
		{
			return run with
			{
				Phase = "map"
			};
		}
		int nextAct = run.Act + 1;
		var (map, rng) = MapGen.GenerateMap(run.Rng, nextAct, allowWorkshop: true, EliteCountFor(run.Difficulty, nextAct));
		return run with
		{
			Rng = rng,
			Act = nextAct,
			Map = map,
			Row = -1,
			Col = 0,
			Combat = null,
			Phase = "map",
			UnknownPity = UnknownPityBase(),
			SeenShrineIds = new List<string>(),
			EventId = null,
			LastRoomWasShop = false
		};
	}

	private static RunState WithRelicGainBrands(RunState next, RunState before)
	{
		int brands = 0;
		foreach (string id in before.Relics)
		{
			brands += (Content.GetRelicDef(id).Bonus?.BrandOnRelic).GetValueOrDefault();
		}
		if (brands <= 0)
		{
			return next;
		}
		List<CardInstance> add = new List<CardInstance>();
		for (int i = 0; i < brands; i++)
		{
			add.Add(new CardInstance
			{
				Uid = $"brand_key_a{next.Act}_r{next.Row}_{next.Relics.Count}_{i}",
				Def = Content.BRAND_DEF
			});
		}
		return AddCardsToRunDeck(next, add);
	}

	private static RunState ApplyRelicBonus(RunState run, string relicId)
	{
		RelicDefBonus b = Content.GetRelicDef(relicId).Bonus;
		if (b == null)
		{
			return run;
		}
		return run with
		{
			MaxHp = run.MaxHp + b.MaxHp.GetValueOrDefault(),
			Hp = Math.Min(run.MaxHp + b.MaxHp.GetValueOrDefault(), run.Hp + b.MaxHp.GetValueOrDefault()),
			VictoryHealBonus = run.VictoryHealBonus + b.VictoryHeal.GetValueOrDefault(),
			RewardChoicesBonus = run.RewardChoicesBonus + b.RewardChoices.GetValueOrDefault(),
			CampfireRatio = (b.CampfireRatio ?? run.CampfireRatio),
			GoldPerVictoryBonus = run.GoldPerVictoryBonus + b.GoldPerVictory.GetValueOrDefault(),
			CampfireForgeBonus = run.CampfireForgeBonus + b.CampfireForge.GetValueOrDefault(),
			Gold = run.Gold + b.GoldOnPickup.GetValueOrDefault()
		};
	}

	private static RunState ApplyRandomUpgradesOnPickup(RunState run, string relicId)
	{
		int n = (Content.GetRelicDef(relicId).Bonus?.UpgradeRandomOnPickup).GetValueOrDefault();
		RunState next = run;
		for (int k = 0; k < n; k++)
		{
			List<int> cands = new List<int>();
			for (int i = 0; i < next.Deck.Count; i++)
			{
				if (Upgrade.CanUpgradeCard(next.Deck[i]))
				{
					cands.Add(i);
				}
			}
			if (cands.Count == 0)
			{
				break;
			}
			(int Value, RngState Next) tuple = Rng.NextInt(next.Rng, 0, cands.Count - 1);
			int j = tuple.Value;
			RngState rng = tuple.Next;
			int idx = cands[j];
			List<CardInstance> deck = new List<CardInstance>(next.Deck.Count);
			for (int l = 0; l < next.Deck.Count; l++)
			{
				deck.Add((l == idx) ? Upgrade.UpgradeCard(next.Deck[l]) : next.Deck[l]);
			}
			next = next with
			{
				Rng = rng,
				Deck = deck
			};
		}
		return next;
	}

	public static int CampfireForgeAllowed(RunState run)
	{
		int bonus = run.CampfireForgeBonus;
		bool usable = bonus > 0 && run.ForgeBonusUsedAct != run.Act;
		return 1 + (usable ? bonus : 0);
	}

	public static RunState ApplyRunCommand(RunState run, RunCommand command)
	{
		int num;
		int num2;
		if (!(command is RunCommand_StartRun c))
		{
			if (!(command is RunCommand_Combat c2))
			{
				if (!(command is RunCommand_PickReward c3))
				{
					if (!(command is RunCommand_SkipReward))
					{
						if (!(command is RunCommand_UseGear c4))
						{
							if (!(command is RunCommand_TakeGear c5))
							{
								if (!(command is RunCommand_SkipGear))
								{
									if (!(command is RunCommand_DiscardGear c6))
									{
										RunCommand_ShopBuyGear runCommand_ShopBuyGear = command as RunCommand_ShopBuyGear;
										if ((object)runCommand_ShopBuyGear == null)
										{
											if (!(command is RunCommand_BuyDeparture c7))
											{
												if (!(command is RunCommand_LeaveDeparture))
												{
													RunCommand_ShopBuyDeparture runCommand_ShopBuyDeparture = command as RunCommand_ShopBuyDeparture;
													if ((object)runCommand_ShopBuyDeparture == null)
													{
														if (!(command is RunCommand_ShopBuyMana))
														{
															if (!(command is RunCommand_ChooseNode c8))
															{
																if (!(command is RunCommand_PickRelic c9))
																{
																	if (!(command is RunCommand_RelicChooseCards c10))
																	{
																		if (!(command is RunCommand_SkipRelic))
																		{
																			if (!(command is RunCommand_CampfireRest))
																			{
																				RunCommand_CampfireUpgrade runCommand_CampfireUpgrade = command as RunCommand_CampfireUpgrade;
																				if ((object)runCommand_CampfireUpgrade == null)
																				{
																					RunCommand_WorkshopFuse runCommand_WorkshopFuse = command as RunCommand_WorkshopFuse;
																					if ((object)runCommand_WorkshopFuse == null)
																					{
																						if (!(command is RunCommand_WorkshopSkip))
																						{
																							RunCommand_CampfireRemove runCommand_CampfireRemove = command as RunCommand_CampfireRemove;
																							if ((object)runCommand_CampfireRemove == null)
																							{
																								if (!(command is RunCommand_CampfireDig))
																								{
																									if (!(command is RunCommand_CampfireTrain))
																									{
																										RunCommand_ShopBuyCard runCommand_ShopBuyCard = command as RunCommand_ShopBuyCard;
																										if ((object)runCommand_ShopBuyCard == null)
																										{
																											if (!(command is RunCommand_ShopBuyRelic))
																											{
																												RunCommand_ShopRemove runCommand_ShopRemove = command as RunCommand_ShopRemove;
																												if ((object)runCommand_ShopRemove == null)
																												{
																													RunCommand_ShopUpgrade runCommand_ShopUpgrade = command as RunCommand_ShopUpgrade;
																													if ((object)runCommand_ShopUpgrade == null)
																													{
																														if (!(command is RunCommand_ShopLeave))
																														{
																															if (command is RunCommand_EventChoice c11)
																															{
																																if (run.Phase != "event")
																																{
																																	throw new InvalidOperationException("イベントではない");
																																}
																																return ApplyEventChoice(run, c11.Index, c11.CardIndex);
																															}
																															throw new InvalidOperationException("未知のランコマンド: " + command.Type);
																														}
																														if (run.Phase != "shop")
																														{
																															throw new InvalidOperationException("ショップではない");
																														}
																														return run with
																														{
																															Shop = null,
																															Phase = "map"
																														};
																													}
																													if (run.Phase != "shop" || run.Shop == null)
																													{
																														throw new InvalidOperationException("ショップではない");
																													}
																													int price = ShopUpgradePrice(run);
																													if (run.Gold < price)
																													{
																														throw new InvalidOperationException($"ゴールドが足りない ({price}G)");
																													}
																													CardInstance card = ((runCommand_ShopUpgrade.Index >= 0 && runCommand_ShopUpgrade.Index < run.Deck.Count) ? run.Deck[runCommand_ShopUpgrade.Index] : null);
																													if (card == null)
																													{
																														throw new InvalidOperationException($"不正な強化指定: {runCommand_ShopUpgrade.Index}");
																													}
																													if (Upgrade.IsUpgraded(card))
																													{
																														throw new InvalidOperationException("すでに鍛えられている");
																													}
																													if (Upgrade.UpgradeTier(card.Def) == "none")
																													{
																														throw new InvalidOperationException(card.Def.Name + " は鍛えられない (エナジー上限を上げる札は強化対象外)");
																													}
																													return BreakMawBank(run with
																													{
																														Gold = run.Gold - price,
																														Deck = run.Deck.Select((CardInstance x, int num4) => (num4 != runCommand_ShopUpgrade.Index) ? x : Upgrade.UpgradeCard(x)).ToList(),
																														UpgradeCount = run.UpgradeCount + 1
																													});
																												}
																												if (run.Phase != "shop" || run.Shop == null)
																												{
																													throw new InvalidOperationException("ショップではない");
																												}
																												int price2 = ShopRemovalPrice(run);
																												if (run.Gold < price2)
																												{
																													throw new InvalidOperationException($"ゴールドが足りない ({price2}G)");
																												}
																												if (((runCommand_ShopRemove.Index >= 0 && runCommand_ShopRemove.Index < run.Deck.Count) ? run.Deck[runCommand_ShopRemove.Index] : null) == null)
																												{
																													throw new InvalidOperationException($"不正な除去指定: {runCommand_ShopRemove.Index}");
																												}
																												if (run.Deck.Count <= 5)
																												{
																													throw new InvalidOperationException("これ以上デッキを減らせない");
																												}
																												return BreakMawBank(run with
																												{
																													Gold = run.Gold - price2,
																													Deck = run.Deck.Where((CardInstance _, int num4) => num4 != runCommand_ShopRemove.Index).ToList(),
																													RemovalCount = run.RemovalCount + 1
																												});
																											}
																											if (run.Phase != "shop" || run.Shop == null)
																											{
																												throw new InvalidOperationException("ショップではない");
																											}
																											if (run.Shop.RelicId == null)
																											{
																												throw new InvalidOperationException("レリックの在庫がない");
																											}
																											if (run.Gold < run.Shop.RelicPrice)
																											{
																												throw new InvalidOperationException($"ゴールドが足りない ({run.Shop.RelicPrice}G)");
																											}
																											string relicId = run.Shop.RelicId;
																											RunState next = GainRelic(BreakMawBank(run with
																											{
																												Gold = run.Gold - run.Shop.RelicPrice,
																												Shop = run.Shop with
																												{
																													RelicId = null
																												}
																											}), relicId);
																											double? ratio = Content.GetRelicDef(relicId).Bonus?.ShopPriceRatio;
																											if (ratio.HasValue && ratio.Value != 1.0 && next.Shop != null)
																											{
																												next = next with
																												{
																													Shop = next.Shop with
																													{
																														Cards = next.Shop.Cards.Select((ShopStateCards x) => (x.Sold != true) ? x with
																														{
																															Price = JsFloor((double)x.Price * ratio.Value)
																														} : x).ToList()
																													}
																												};
																											}
																											return EnterPendingChoice(next, "shop");
																										}
																										if (run.Phase != "shop" || run.Shop == null)
																										{
																											throw new InvalidOperationException("ショップではない");
																										}
																										ShopStateCards item = ((runCommand_ShopBuyCard.Index >= 0 && runCommand_ShopBuyCard.Index < run.Shop.Cards.Count) ? run.Shop.Cards[runCommand_ShopBuyCard.Index] : null);
																										if (item == null)
																										{
																											throw new InvalidOperationException($"不正な商品指定: {runCommand_ShopBuyCard.Index}");
																										}
																										if (item.Sold == true)
																										{
																											throw new InvalidOperationException("その商品は売り切れ");
																										}
																										if (run.Gold < item.Price)
																										{
																											throw new InvalidOperationException($"ゴールドが足りない ({item.Price}G)");
																										}
																										CardInstance card2 = new CardInstance
																										{
																											Uid = $"buy_a{run.Act}_r{run.Row}_{item.Id}",
																											Def = Content.GetCardDef(item.Id)
																										};
																										return AddCardsToRunDeck(BreakMawBank(run with
																										{
																											Gold = run.Gold - item.Price,
																											Picks = Append(run.Picks, item.Id),
																											Shop = run.Shop with
																											{
																												Cards = run.Shop.Cards.Select((ShopStateCards x, int num4) => (num4 != runCommand_ShopBuyCard.Index) ? x : x with
																												{
																													Sold = true
																												}).ToList()
																											}
																										}), new List<CardInstance> { card2 });
																									}
																									if (run.Phase != "campfire")
																									{
																										throw new InvalidOperationException("焚き火フェーズではない");
																									}
																									if (CampfireOptions(run).TrainLeft <= 0)
																									{
																										throw new InvalidOperationException("鍛錬できない (重石が無いか、もう鍛錬し尽くした)");
																									}
																									if (run.CampfireUpgradesUsed > 0)
																									{
																										throw new InvalidOperationException("この焚き火ではもう鍛えた (鍛錬は休む/鍛えると排他)");
																									}
																									return WithRelicState(run, "train", RelicStateOf(run, "train") + 1)with
																									{
																										Phase = "map"
																									};
																								}
																								if (run.Phase != "campfire")
																								{
																									throw new InvalidOperationException("焚き火フェーズではない");
																								}
																								if (!RelicFlag(run, "campfireDig"))
																								{
																									throw new InvalidOperationException("発掘の鶴嘴が無い");
																								}
																								if (run.CampfireUpgradesUsed > 0)
																								{
																									throw new InvalidOperationException("この焚き火ではもう鍛えた (発掘は休む/鍛えると排他)");
																								}
																								(IReadOnlyList<string> Options, RngState Rng) tuple = DrawRelicOptions(run, "chest", 1);
																								IReadOnlyList<string> drawn = tuple.Options;
																								RngState rng = tuple.Rng;
																								RunState next2 = run with
																								{
																									Rng = rng,
																									Phase = "map"
																								};
																								if (drawn.Count > 0)
																								{
																									next2 = GainRelic(next2, drawn[0]);
																								}
																								return EnterPendingChoice(next2, "map");
																							}
																							if (run.Phase != "campfire")
																							{
																								throw new InvalidOperationException("焚き火フェーズではない");
																							}
																							if (!RelicFlag(run, "campfireRemove"))
																							{
																								throw new InvalidOperationException("焚き火では除去できない (除去はショップのみ。焚き火は 休む/鍛える の二択)");
																							}
																							if (run.CampfireUpgradesUsed > 0)
																							{
																								throw new InvalidOperationException("この焚き火ではもう鍛えた (取り除くのは休む/鍛えると排他)");
																							}
																							if (((runCommand_CampfireRemove.Index >= 0 && runCommand_CampfireRemove.Index < run.Deck.Count) ? run.Deck[runCommand_CampfireRemove.Index] : null) == null)
																							{
																								throw new InvalidOperationException($"不正な除去指定: {runCommand_CampfireRemove.Index}");
																							}
																							if (run.Deck.Count <= 5)
																							{
																								throw new InvalidOperationException("これ以上デッキを減らせない");
																							}
																							return run with
																							{
																								Deck = run.Deck.Where((CardInstance _, int num4) => num4 != runCommand_CampfireRemove.Index).ToList(),
																								Phase = "map"
																							};
																						}
																						if (run.Phase != "workshop")
																						{
																							throw new InvalidOperationException("工房フェーズではない");
																						}
																						return run with
																						{
																							Phase = "map"
																						};
																					}
																					if (run.Phase != "workshop")
																					{
																						throw new InvalidOperationException("工房フェーズではない");
																					}
																					CardInstance a = ((runCommand_WorkshopFuse.IndexA >= 0 && runCommand_WorkshopFuse.IndexA < run.Deck.Count) ? run.Deck[runCommand_WorkshopFuse.IndexA] : null);
																					CardInstance b = ((runCommand_WorkshopFuse.IndexB >= 0 && runCommand_WorkshopFuse.IndexB < run.Deck.Count) ? run.Deck[runCommand_WorkshopFuse.IndexB] : null);
																					if (a == null || b == null)
																					{
																						throw new InvalidOperationException("不正な合成指定");
																					}
																					string reason = Fusion.FuseBlockReason(a, b);
																					if (reason != null)
																					{
																						throw new InvalidOperationException("合成できない: " + reason);
																					}
																					int price3 = WorkshopFusePrice(run);
																					if (run.Gold < price3)
																					{
																						throw new InvalidOperationException($"ゴールドが足りない (合成{price3}G・所持{run.Gold}G)");
																					}
																					CardDef fusedDef = Fusion.FuseCards(a, b);
																					int usedBefore = run.WorkshopFusesUsed.GetValueOrDefault();
																					CardInstance fused = new CardInstance
																					{
																						Uid = $"fused_a{run.Act}_r{run.Row}_{((usedBefore > 0) ? (usedBefore + "_") : "")}{fusedDef.Id}",
																						Def = fusedDef
																					};
																					fused = fused with
																					{
																						Def = WorkshopFuseResult(run, a, b, fusedDef)
																					};
																					List<CardInstance> deck = run.Deck.Where((CardInstance _, int num4) => num4 != runCommand_WorkshopFuse.IndexA && num4 != runCommand_WorkshopFuse.IndexB).ToList();
																					int used = usedBefore + 1;
																					int allowed = 1 + RelicBonusSum(run, "workshopFuses");
																					return run with
																					{
																						Deck = Append(deck, fused),
																						Gold = run.Gold - price3,
																						WorkshopFusesUsed = used,
																						Phase = ((used < allowed) ? "workshop" : "map")
																					};
																				}
																				if (run.Phase != "campfire")
																				{
																					throw new InvalidOperationException("焚き火フェーズではない");
																				}
																				CardInstance card3 = ((runCommand_CampfireUpgrade.Index >= 0 && runCommand_CampfireUpgrade.Index < run.Deck.Count) ? run.Deck[runCommand_CampfireUpgrade.Index] : null);
																				if (card3 == null)
																				{
																					throw new InvalidOperationException($"不正な強化指定: {runCommand_CampfireUpgrade.Index}");
																				}
																				if (Upgrade.IsUpgraded(card3))
																				{
																					throw new InvalidOperationException("すでに鍛えられている");
																				}
																				if (RelicFlag(run, "noForge"))
																				{
																					throw new InvalidOperationException("融合の鎚を持っている間、焚き火では鍛えられない");
																				}
																				if (Upgrade.UpgradeTier(card3.Def) == "none")
																				{
																					throw new InvalidOperationException(card3.Def.Name + " は鍛えられない (エナジー上限を上げる札は強化対象外)");
																				}
																				int used2 = run.CampfireUpgradesUsed + 1;
																				int allowed2 = CampfireForgeAllowed(run);
																				return run with
																				{
																					Deck = run.Deck.Select((CardInstance x, int num4) => (num4 != runCommand_CampfireUpgrade.Index) ? x : Upgrade.UpgradeCard(x)).ToList(),
																					CampfireUpgradesUsed = used2,
																					ForgeBonusUsedAct = ((used2 > 1) ? new int?(run.Act) : run.ForgeBonusUsedAct),
																					Phase = ((used2 < allowed2) ? "campfire" : "map")
																				};
																			}
																			if (run.Phase != "campfire")
																			{
																				throw new InvalidOperationException("焚き火フェーズではない");
																			}
																			bool noRest = run.Relics.Any((string id3) => (Content.GetRelicDef(id3).Bonus?.NoRest == true));
																			if (run.CampfireUpgradesUsed == 0)
																			{
																				num = ((!noRest) ? 1 : 0);
																				if (num != 0)
																				{
																					num2 = RelicBonusSum(run, "restMaxHp");
																					goto IL_1489;
																				}
																			}
																			else
																			{
																				num = 0;
																			}
																			num2 = 0;
																			goto IL_1489;
																		}
																		if (run.Phase != "relic-reward")
																		{
																			throw new InvalidOperationException("レリック報酬フェーズではない");
																		}
																		if (run.Combat == null)
																		{
																			return run with
																			{
																				RelicOptions = null,
																				Phase = "map"
																			};
																		}
																		return RollRewards(run with
																		{
																			RelicOptions = null,
																			RelicPicksLeft = null
																		});
																	}
																	if (run.Phase != "relic-choose" || run.PendingRelicChoice == null)
																	{
																		throw new InvalidOperationException("レリックの対象選択フェーズではない");
																	}
																	RunStatePendingRelicChoice p = run.PendingRelicChoice;
																	List<int> idx = new List<int>();
																	foreach (int i in c10.Indices)
																	{
																		if (!idx.Contains(i))
																		{
																			idx.Add(i);
																		}
																	}
																	if (idx.Count > p.Count)
																	{
																		throw new InvalidOperationException($"選べるのは{p.Count}枚まで");
																	}
																	foreach (int i2 in idx)
																	{
																		if (i2 < 0 || i2 >= run.Deck.Count)
																		{
																			throw new InvalidOperationException("不正な対象指定");
																		}
																	}
																	RunState next3 = run;
																	if (p.Mode == "remove")
																	{
																		if (run.Deck.Count - idx.Count < 5)
																		{
																			throw new InvalidOperationException("これ以上デッキを減らせない");
																		}
																		next3 = next3 with
																		{
																			Deck = next3.Deck.Where((CardInstance _, int item2) => !idx.Contains(item2)).ToList()
																		};
																	}
																	else
																	{
																		foreach (int i3 in idx)
																		{
																			if (!CanTransformCard(run.Deck[i3]))
																			{
																				throw new InvalidOperationException(run.Deck[i3].Def.Name + " は変成できない（状態異常・烙印は除去で消す）");
																			}
																		}
																		foreach (int i4 in idx)
																		{
																			next3 = TransformCardAt(next3, i4, upgrade: true);
																		}
																	}
																	return next3 with
																	{
																		PendingRelicChoice = null,
																		Phase = p.Resume
																	};
																}
																if (run.Phase != "relic-reward" || run.RelicOptions == null)
																{
																	throw new InvalidOperationException("レリック報酬フェーズではない");
																}
																string relicId2 = ((c9.Index >= 0 && c9.Index < run.RelicOptions.Count) ? run.RelicOptions[c9.Index] : null);
																if (relicId2 == null)
																{
																	throw new InvalidOperationException($"不正なレリック指定: {c9.Index}");
																}
																RunState next4 = GainRelic(run with
																{
																	RelicOptions = null
																}, relicId2);
																int picksLeft = (run.RelicPicksLeft ?? 1) - 1;
																List<string> remaining = new List<string>();
																foreach (string id in run.RelicOptions)
																{
																	if (id != relicId2)
																	{
																		remaining.Add(id);
																	}
																}
																RunState runState;
																if (run.Combat != null && run.CurrentElite && picksLeft > 0 && remaining.Count > 0)
																{
																	runState = next4 with
																	{
																		Phase = "relic-reward",
																		RelicOptions = remaining,
																		RelicPicksLeft = picksLeft
																	};
																}
																else
																{
																	runState = ((run.Combat == null) ? next4 with
																	{
																		RelicOptions = null,
																		Phase = "map"
																	} : RollRewards(next4 with
																	{
																		RelicPicksLeft = null
																	}));
																}
																return EnterPendingChoice(runState, runState.Phase);
															}
															if (run.Phase != "map")
															{
																throw new InvalidOperationException("マップフェーズではない");
															}
															IReadOnlyList<int> source = NextChoices(run);
															RunState next5 = run;
															if (!source.Contains(c8.Col))
															{
																if (!WingChoices(run).Contains(c8.Col))
																{
																	throw new InvalidOperationException($"進めないノード: {c8.Col}");
																}
																next5 = WithRelicState(next5, "wingBoots", RelicStateOf(next5, "wingBoots") - 1);
															}
															if (RelicStateOf(next5, "mawBroken") == 0)
															{
																next5 = next5 with
																{
																	Gold = next5.Gold + RelicBonusSum(next5, "goldPerRow")
																};
															}
															return EnterNode(next5 with
															{
																Row = next5.Row + 1,
																Col = c8.Col
															});
														}
														if (run.Phase != "shop" || run.Shop == null)
														{
															throw new InvalidOperationException("ショップではない");
														}
														int price4 = run.Shop.ManaPrice ?? 30;
														if (run.Gold < price4)
														{
															throw new InvalidOperationException("ゴールドが足りない");
														}
														if (ManaOf(run) >= 50)
														{
															throw new InvalidOperationException("魔素は上限");
														}
														return BreakMawBank(run with
														{
															Gold = run.Gold - price4,
															Mana = Math.Min(50, ManaOf(run) + 10)
														});
													}
													if (run.Phase != "shop" || run.Shop == null)
													{
														throw new InvalidOperationException("ショップではない");
													}
													IReadOnlyList<ShopStateDepartures> shelf = run.Shop.Departures ?? new List<ShopStateDepartures>();
													ShopStateDepartures slot = ((runCommand_ShopBuyDeparture.Index >= 0 && runCommand_ShopBuyDeparture.Index < shelf.Count) ? shelf[runCommand_ShopBuyDeparture.Index] : null);
													if (slot == null || slot.Sold == true)
													{
														throw new InvalidOperationException($"不正な支度の指定: {runCommand_ShopBuyDeparture.Index}");
													}
													DepartureOffer offer = run.Departure?.Leftovers.FirstOrDefault((DepartureOffer o) => o.Id == slot.Id);
													if (offer == null)
													{
														throw new InvalidOperationException("その支度は行商が持っていない");
													}
													if (run.Gold < slot.Price)
													{
														throw new InvalidOperationException($"ゴールドが足りない ({slot.Price}G)");
													}
													return ApplyChoiceDef(BreakMawBank(run with
													{
														Gold = run.Gold - slot.Price,
														Shop = run.Shop with
														{
															Departures = shelf.Select((ShopStateDepartures x, int num4) => (num4 != runCommand_ShopBuyDeparture.Index) ? x : x with
															{
																Sold = true
															}).ToList()
														},
														Departure = run.Departure with
														{
															Leftovers = run.Departure.Leftovers.Where((DepartureOffer o) => o.Id != slot.Id).ToList()
														}
													}), offer.Choice, runCommand_ShopBuyDeparture.CardIndex, "shop");
												}
												if (run.Phase != "departure" || run.Departure == null)
												{
													throw new InvalidOperationException("出立の店ではない");
												}
												IReadOnlyList<string> bought = run.Departure.Bought;
												List<DepartureOffer> leftovers = run.Departure.Offers.Where((DepartureOffer o) => !bought.Contains(o.Id) && o.ShopPrice.HasValue).ToList();
												return run with
												{
													Departure = run.Departure with
													{
														Leftovers = leftovers
													},
													Phase = "map"
												};
											}
											if (run.Phase != "departure" || run.Departure == null)
											{
												throw new InvalidOperationException("出立の店ではない");
											}
											IReadOnlyList<DepartureOffer> offers = run.Departure.Offers;
											DepartureOffer offer2 = ((c7.Index >= 0 && c7.Index < offers.Count) ? offers[c7.Index] : null);
											if (offer2 == null)
											{
												throw new InvalidOperationException($"不正な支度の指定: {c7.Index}");
											}
											if (DepartureOfferBought(run, offer2))
											{
												throw new InvalidOperationException("その支度は買った");
											}
											if (run.Gold < offer2.Price)
											{
												throw new InvalidOperationException($"ゴールドが足りない ({offer2.Price}G)");
											}
											return ApplyChoiceDef(run with
											{
												Gold = run.Gold - offer2.Price,
												Departure = run.Departure with
												{
													Bought = Append(run.Departure.Bought, offer2.Id)
												}
											}, offer2.Choice, c7.CardIndex, "departure");
										}
										if (run.Phase != "shop" || run.Shop == null)
										{
											throw new InvalidOperationException("ショップではない");
										}
										IReadOnlyList<ShopStateGears> shelf2 = run.Shop.Gears ?? new List<ShopStateGears>();
										ShopStateGears slot2 = ((runCommand_ShopBuyGear.Index >= 0 && runCommand_ShopBuyGear.Index < shelf2.Count) ? shelf2[runCommand_ShopBuyGear.Index] : null);
										if (slot2 == null || slot2.Sold == true)
										{
											throw new InvalidOperationException($"不正なギア指定: {runCommand_ShopBuyGear.Index}");
										}
										if (run.Gold < slot2.Price)
										{
											throw new InvalidOperationException("ゴールドが足りない");
										}
										RunState baseRun = run;
										if (GearFull(baseRun))
										{
											if (!runCommand_ShopBuyGear.DiscardIndex.HasValue)
											{
												throw new InvalidOperationException("持ち物が満杯 = 入れ替えるギアを選ぶ");
											}
											baseRun = DiscardGearAt(baseRun, runCommand_ShopBuyGear.DiscardIndex.Value);
										}
										RunState bought2 = AddGear(baseRun, slot2.Id, "shop_a" + run.Act + "_r" + run.Row + "_" + slot2.Id);
										return BreakMawBank(bought2 with
										{
											Gold = bought2.Gold - slot2.Price,
											Shop = run.Shop with
											{
												Gears = shelf2.Select((ShopStateGears g, int num4) => (num4 != runCommand_ShopBuyGear.Index) ? g : g with
												{
													Sold = true
												}).ToList()
											}
										});
									}
									return DiscardGearAt(run, c6.Index);
								}
								if (run.Phase != "reward")
								{
									throw new InvalidOperationException("報酬フェーズではない");
								}
								return FinishReward(run with
								{
									GearOption = null
								});
							}
							string id2 = run.GearOption;
							if (run.Phase != "reward" || id2 == null)
							{
								throw new InvalidOperationException("取れるギアがない");
							}
							RunState baseRun2 = run;
							if (GearFull(baseRun2))
							{
								if (!c5.DiscardIndex.HasValue)
								{
									throw new InvalidOperationException("持ち物が満杯 = 入れ替えるギアを選ぶ");
								}
								baseRun2 = DiscardGearAt(baseRun2, c5.DiscardIndex.Value);
							}
							RunState run2 = baseRun2;
							string[] array = new string[6]
							{
								"a",
								run.Act.ToString(),
								"_r",
								run.Row.ToString(),
								"_",
								id2
							};
							return FinishReward(AddGear(run2, id2, string.Concat(array))with
							{
								GearOption = null
							});
						}
						IReadOnlyList<GearInstance> gears = GearsOf(run);
						if (c4.Index < 0 || c4.Index >= gears.Count)
						{
							throw new InvalidOperationException($"不正なギア指定: {c4.Index}");
						}
						GearInstance gear = gears[c4.Index];
						GearDef def = Content.GetGearDef(gear.GearId);
						string blocked = Gears.GearBlockedReason((run.Phase == "combat") ? run.Combat : null, ManaOf(run), gear);
						if (blocked != null)
						{
							throw new InvalidOperationException(def.Name + " は組めない: " + blocked);
						}
						GameState combat = run.Combat;
						if (def.Special == "flee")
						{
							if (CurrentNode(run)?.Type == "boss")
							{
								throw new InvalidOperationException("幕ボスからは逃げられない");
							}
							return SettleHpLossCap(SpendGear(run, c4.Index), combat)with
							{
								Combat = null,
								Hp = combat.Player.Hp,
								Phase = "map",
								RewardOptions = null,
								GearOption = null
							};
						}
						GameState next6 = Gears.ResolveGear(combat, def, new Gears.UseGearOptions
						{
							TargetIndex = c4.TargetIndex,
							CardUid = c4.CardUid,
							AsGearId = c4.AsGearId,
							SeenGearIds = (run.SeenGearIds ?? new List<string>())
						});
						RunState after = SpendGear(run, c4.Index)with
						{
							Combat = next6 with
							{
								GearUsedThisTurn = true
							}
						};
						if (after.Combat.Phase == "won")
						{
							return AfterVictory(after, after.Combat);
						}
						if (after.Combat.Phase == "lost")
						{
							return after with
							{
								Hp = 0,
								Phase = "lost"
							};
						}
						return after;
					}
					if (run.Phase != "reward")
					{
						throw new InvalidOperationException("報酬フェーズではない");
					}
					int bowl = RelicBonusSum(run, "skipRewardMaxHp");
					bool num3 = run.RewardOptions == null && run.GearOption != null;
					RunState skipped = run with
					{
						RewardOptions = null,
						MaxHp = run.MaxHp + bowl,
						Hp = run.Hp + bowl
					};
					if (num3)
					{
						skipped = skipped with
						{
							GearOption = null
						};
					}
					int round = run.RewardRoundsLeft.GetValueOrDefault();
					if (round > 0)
					{
						return RollRewards(skipped with
						{
							RewardRoundsLeft = round - 1
						});
					}
					return FinishReward(skipped);
				}
				if (run.Phase != "reward" || run.RewardOptions == null)
				{
					throw new InvalidOperationException("報酬フェーズではない");
				}
				string cardId = ((c3.Index >= 0 && c3.Index < run.RewardOptions.Count) ? run.RewardOptions[c3.Index] : null);
				if (cardId == null)
				{
					throw new InvalidOperationException($"不正な報酬指定: {c3.Index}");
				}
				int round2 = run.RewardRoundsLeft.GetValueOrDefault();
				CardInstance card4 = new CardInstance
				{
					Uid = $"pick_a{run.Act}_r{run.Row}_{cardId}{((round2 > 0) ? ("_x" + round2) : "")}",
					Def = Content.GetCardDef(cardId)
				};
				RunState picked = AddCardsToRunDeck(run with
				{
					Picks = Append(run.Picks, cardId),
					RewardOptions = null
				}, new List<CardInstance> { card4 });
				if (round2 > 0)
				{
					return RollRewards(picked with
					{
						RewardRoundsLeft = round2 - 1
					});
				}
				return FinishReward(picked);
			}
			if (run.Phase != "combat" || run.Combat == null)
			{
				throw new InvalidOperationException("戦闘中ではない（直前の戦闘は決着済み＝残りの手札は打てない。次はランのコマンド）");
			}
			if (c2.Command.Type == "StartCombat")
			{
				throw new InvalidOperationException("ラン中の戦闘開始はランが管理する");
			}
			GameState combat2 = State.ApplyCommand(run.Combat, c2.Command);
			if (combat2.Phase == "lost")
			{
				return run with
				{
					Combat = combat2,
					Hp = 0,
					Phase = "lost"
				};
			}
			if (combat2.Phase == "won")
			{
				return AfterVictory(run, combat2);
			}
			return run with
			{
				Combat = combat2
			};
		}
		return CreateRun(c.Seed, run.Mode, run.LeaderId);
		IL_1489:
		int grow = num2;
		int maxHp = run.MaxHp + grow;
		int hp = ((num != 0) ? Math.Min(maxHp, run.Hp + grow + JsFloor((double)run.MaxHp * run.CampfireRatio)) : run.Hp);
		return run with
		{
			Hp = hp,
			MaxHp = maxHp,
			Phase = "map"
		};
	}
}
