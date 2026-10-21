using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Upgrade
{
	public static class UpgradeTiers
	{
		public const string Amount = "amount";

		public const string Cost = "cost";

		public const string Unit = "unit";

		public const string Bonus = "bonus";

		public const string None = "none";

		public const string Mult = "mult";

		public const string Threshold = "threshold";

		public const string Light = "light";

		public const string Range = "range";

		public const string Cascade = "cascade";
	}

	private static readonly HashSet<string> UPGRADABLE_EFFECTS = new HashSet<string> { "dealDamage", "gainBlock", "gainIceBlock", "applyBurn", "counter", "gainHp", "dealDamageDrain", "dealDamageRandom", "dealDamageExecute" };

	private static readonly HashSet<string> UNIT_EFFECTS = new HashSet<string>
	{
		"addCardToHand", "empowerShivs", "drawCards", "impulseDraw", "addGrowth", "addMomentum", "addAether", "gainEnergy", "discountNext", "dealDamagePerCardPlayed",
		"exhaustFromDeck"
	};

	private static readonly Dictionary<string, IReadOnlyList<DeclarativeEffect>> BONUS_UPGRADES = new Dictionary<string, IReadOnlyList<DeclarativeEffect>>
	{
		["blue_chain_thought"] = new DeclarativeEffect[1] { Eff("onPlay", "drawCards", 1) },
		["blue_aether_torrent"] = new DeclarativeEffect[1] { Eff("onPlay", "addAether", 2) },
		["blue_storm_lash"] = new DeclarativeEffect[1] { Eff("onPlay", "dealDamage", 5) },
		["blue_weight_of_wisdom"] = new DeclarativeEffect[1] { Eff("onPlay", "drawCards", 1) },
		["blue_knowledge_torrent"] = new DeclarativeEffect[1] { Eff("onPlay", "drawCards", 1) },
		["blue_ripple_blade"] = new DeclarativeEffect[1] { Eff("onPlay", "dealDamage", 3) },
		["blue_storm_echo"] = new DeclarativeEffect[1] { Eff("onAttacked", "dealDamage", 4) },
		["blue_ice_lance"] = new DeclarativeEffect[1] { Eff("onPlay", "gainIceBlock", 4) },
		["blue_echo"] = new DeclarativeEffect[1] { Eff("onPlay", "drawCards", 1) },
		["red_all_in"] = new DeclarativeEffect[1] { Eff("onPlay", "dealDamage", 6) },
		["white_rally"] = new DeclarativeEffect[1] { Eff("onPlay", "gainBlock", 4) },
		["blue_page_wind"] = new DeclarativeEffect[1] { Eff("onPlay", "drawCards", 1) },
		["blue_rolling_wave"] = new DeclarativeEffect[1] { Eff("onPlay", "drawCards", 1) },
		["black_grave_pressure"] = new DeclarativeEffect[1] { Eff("onPlay", "exhaustFromDeck", 2) },
		["red_streak_bet"] = new DeclarativeEffect[1] { Eff("onPlay", "dealDamage", 3) },
		["black_blade_procession"] = new DeclarativeEffect[1] { Eff("onPlay", "addCardToHand", 1, "black_shiv_token") },
		["black_seething_blood"] = new DeclarativeEffect[1] { Eff("onPlay", "dealDamageDrain", 4) },
		["green_sapling_strike"] = new DeclarativeEffect[1] { Eff("onPlay", "dealDamage", 4) }
	};

	private static readonly HashSet<string> REFILL_FOR_UPGRADE = new HashSet<string> { "addCardToHand", "drawCardsPerLight", "drawCards", "drawCardsPerCardPlayed", "dischargeAetherDraw", "impulseDraw", "retrieveFromExhaust", "playFromExhaust", "retrieveZeroCostFromDiscard", "drawTypeFromDeck" };

	private static readonly HashSet<string> MULT_EFFECTS = new HashSet<string>
	{
		"dealDamagePerBlock", "dealDamagePerPermanent", "gainBlockPerPermanent", "dealDamagePerEnergyMax", "gainBlockPerEnergyMax", "dealDamagePerAttackPlayed", "dealDamagePerWeak", "dealDamagePerNegStrength", "dealDamagePerDamageTaken", "applyBurnPerDamageTaken",
		"dealDamagePerRandomPlayed", "dealDamagePerHandCard", "gainIceBlockPerHandCard", "dischargeGrowth", "dischargeGrowthBlock", "dischargeMomentumDamage", "dischargeMomentumBlock", "dischargeMomentumBurn", "dischargeMomentumVolley", "gainBlockPerMomentum",
		"addGrowthPerMomentum", "dealDamagePerCardPlayed", "dealDamagePerExhaust", "dealDamageDrainPerExhaust", "gainBlockPerExhaust", "dealDamagePerSelfHpLost", "dealDamagePerHeal", "dischargeLight", "dealDamagePerLight", "dealDamagePerSpark",
		"gainBlockPerSpark", "gainBlockPerLight", "drawCardsPerLight", "dischargeLightWeaken", "dealDamagePerScry", "dealDamagePerTrapFired", "dealDamagePerCascade", "gainBlockPerBurn", "dealDamagePerBurn", "dischargeBurn"
	};

	private static readonly HashSet<string> UNIT_EFFECTS_V2 = new HashSet<string>
	{
		"drawCards", "impulseDraw", "addGrowth", "addMomentum", "addAether", "addLight", "addCasts", "gainEnergy", "exposeEnemy", "weakenEnemy",
		"summonPermanent", "upgradeInHand", "addCardToHand", "empowerShivs", "exhaustFromDeck", "addCardToDraw", "triggerRandomRetainer", "copyRetainer", "twinNextRetainer", "extendRetainerLife",
		"scry", "extendTrapLife"
	};

	private static readonly HashSet<string> AMOUNT_V2 = BuildAmountV2();

	private static readonly HashSet<string> V2_COLORS = new HashSet<string> { "green", "white", "blue", "red" };

	private static readonly HashSet<string> V2_LEGACY = new HashSet<string> { "blue_ice_lance", "blue_spark", "red_perm_pyre" };

	private static readonly HashSet<string> V2_BONUS = new HashSet<string> { "blue_echo" };

	private static readonly HashSet<string> V2_CASCADE_PLUS = new HashSet<string> { "red_brushfire", "red_twin_fire" };

	private static readonly HashSet<string> V2_COST_ONLY = new HashSet<string> { "white_perm_band", "white_perm_apostle", "blue_counterspell", "blue_spell_steal", "blue_perm_tidal_lectern", "blue_perm_deep_cloak", "red_perm_ledger", "red_burst_release" };

	private static readonly Dictionary<string, (int Max, int Min)> V2_RANGE = new Dictionary<string, (int, int)>
	{
		["blue_mana_leak"] = (5, 0),
		["blue_undertow"] = (0, -4)
	};

	private static DeclarativeEffect Eff(string trigger, string effect, int? amount = null, string? summonId = null)
	{
		return new DeclarativeEffect
		{
			Trigger = trigger,
			Effect = effect,
			Amount = amount,
			SummonId = summonId
		};
	}

	private static IReadOnlyList<DeclarativeEffect> AllEffectsOf(CardDef def)
	{
		List<DeclarativeEffect> list = new List<DeclarativeEffect>(def.Effects);
		if (def.Modes != null)
		{
			foreach (CardMode m in def.Modes)
			{
				list.AddRange(m.Effects);
			}
		}
		return list;
	}

	private static bool CostCutViolates(CardDef def)
	{
		if (def.Exhaust == true)
		{
			return false;
		}
		int newCost = def.Cost - 1;
		IReadOnlyList<DeclarativeEffect> eff = AllEffectsOf(def);
		if (!eff.Any((DeclarativeEffect e) => REFILL_FOR_UPGRADE.Contains(e.Effect)))
		{
			return false;
		}
		return eff.Where((DeclarativeEffect e) => e.Effect == "gainEnergy" || e.Effect == "discountNext").Aggregate(0, (int a, DeclarativeEffect e) => a + e.Amount.GetValueOrDefault()) - newCost >= 0;
	}

	private static HashSet<string> BuildAmountV2()
	{
		return new HashSet<string>(UPGRADABLE_EFFECTS) { "growSelf" };
	}

	private static bool HasMult(DeclarativeEffect e)
	{
		if ((!MULT_EFFECTS.Contains(e.Effect) || !e.Amount.HasValue) && !e.GrowthMultiplier.HasValue)
		{
			return e.MomentumMultiplier.HasValue;
		}
		return true;
	}

	private static bool HasThreshold(DeclarativeEffect e)
	{
		EffectCondition? condition = e.Condition;
		if ((object)condition == null || !condition.MinGrowth.HasValue)
		{
			EffectCondition? condition2 = e.Condition;
			if ((object)condition2 == null || !condition2.MinMomentum.HasValue)
			{
				return e.Condition?.MinLight.HasValue ?? false;
			}
		}
		return true;
	}

	private static bool IsGreenRule(CardDef def)
	{
		if (!V2_LEGACY.Contains(def.Id))
		{
			if (!def.Id.StartsWith("green_", StringComparison.Ordinal))
			{
				if (def.Color != null)
				{
					return V2_COLORS.Contains(def.Color);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private static string TierV2(IReadOnlyList<DeclarativeEffect> effects, CardDef? def = null)
	{
		if (effects.Any(HasMult))
		{
			return "mult";
		}
		bool hasAmount = effects.Any((DeclarativeEffect e) => AMOUNT_V2.Contains(e.Effect) && e.Amount.HasValue);
		if (effects.Any((DeclarativeEffect e) => UNIT_EFFECTS_V2.Contains(e.Effect) && e.Amount.HasValue && (!(((def?.Retainer == true)) & hasAmount) || !(e.Effect == "addLight"))))
		{
			return "unit";
		}
		if (!effects.Any(HasThreshold))
		{
			CardDef? cardDef = def;
			if ((object)cardDef == null || !cardDef.FreeIfMomentumAtLeast.HasValue)
			{
				if (effects.Any((DeclarativeEffect e) => AMOUNT_V2.Contains(e.Effect) && e.Amount.HasValue))
				{
					return "amount";
				}
				return "none";
			}
		}
		return "threshold";
	}

	private static DeclarativeEffect Boost50(DeclarativeEffect e, int min)
	{
		if (!AMOUNT_V2.Contains(e.Effect) || !e.Amount.HasValue || !(e.Amount >= min))
		{
			return e;
		}
		DeclarativeEffect n = e with
		{
			Amount = (int)Math.Ceiling((double)e.Amount.GetValueOrDefault() * 1.5)
		};
		if (e.AmountMax.HasValue)
		{
			n = n with
			{
				AmountMax = (int)Math.Ceiling((double)e.AmountMax.GetValueOrDefault() * 1.5)
			};
		}
		return n;
	}

	private static IReadOnlyList<DeclarativeEffect> ApplyV2(IReadOnlyList<DeclarativeEffect> effects, CardDef def)
	{
		switch (TierV2(effects, def))
		{
		case "mult":
		{
			bool onlyFirst = effects.Any((DeclarativeEffect declarativeEffect) => MULT_EFFECTS.Contains(declarativeEffect.Effect) && declarativeEffect.Amount == 1);
			bool done = false;
			List<DeclarativeEffect> outList = new List<DeclarativeEffect>();
			{
				foreach (DeclarativeEffect e in effects)
				{
					if (onlyFirst & done)
					{
						outList.Add(e);
						continue;
					}
					DeclarativeEffect n = e;
					if (MULT_EFFECTS.Contains(e.Effect) && e.Amount.HasValue)
					{
						n = n with
						{
							Amount = e.Amount + 1
						};
					}
					if (e.GrowthMultiplier.HasValue)
					{
						n = n with
						{
							GrowthMultiplier = e.GrowthMultiplier + 1.0
						};
					}
					if (e.MomentumMultiplier.HasValue)
					{
						n = n with
						{
							MomentumMultiplier = e.MomentumMultiplier + 1.0
						};
					}
					if ((object)n != e)
					{
						done = true;
					}
					outList.Add(n);
				}
				return outList;
			}
		}
		case "unit":
		{
			bool done2 = false;
			List<DeclarativeEffect> outList2 = new List<DeclarativeEffect>();
			{
				foreach (DeclarativeEffect e2 in effects)
				{
					if (!done2 && UNIT_EFFECTS_V2.Contains(e2.Effect) && e2.Amount.HasValue)
					{
						done2 = true;
						outList2.Add(e2 with
						{
							Amount = e2.Amount + ((!(e2.Effect == "addMomentum")) ? 1 : 2)
						});
					}
					else
					{
						outList2.Add(Boost50(e2, 4));
					}
				}
				return outList2;
			}
		}
		case "threshold":
		{
			List<DeclarativeEffect> outList3 = new List<DeclarativeEffect>();
			{
				foreach (DeclarativeEffect e3 in effects)
				{
					EffectCondition c = e3.Condition;
					DeclarativeEffect n2 = e3;
					if (c != null && (c.MinGrowth.HasValue || c.MinMomentum.HasValue || c.MinLight.HasValue))
					{
						EffectCondition nc = c;
						if (c.MinGrowth.HasValue)
						{
							nc = nc with
							{
								MinGrowth = Math.Max(1, c.MinGrowth.GetValueOrDefault() - 1)
							};
						}
						if (c.MinMomentum.HasValue)
						{
							nc = nc with
							{
								MinMomentum = Math.Max(1, c.MinMomentum.GetValueOrDefault() - 1)
							};
						}
						if (c.MinLight.HasValue)
						{
							nc = nc with
							{
								MinLight = Math.Max(1, c.MinLight.GetValueOrDefault() - 1)
							};
						}
						n2 = e3 with
						{
							Condition = nc
						};
					}
					outList3.Add(Boost50(n2, 1));
				}
				return outList3;
			}
		}
		case "amount":
			return effects.Select((DeclarativeEffect e4) => Boost50(e4, 1)).ToList();
		default:
			return effects;
		}
	}

	public static bool IsFusedPerTriggerPermanent(CardDef def)
	{
		if (def.Type != "permanent")
		{
			return false;
		}
		if (!def.Id.StartsWith("fused_", StringComparison.Ordinal) && !def.Id.StartsWith("fusion_", StringComparison.Ordinal))
		{
			return false;
		}
		return def.Effects.Any((DeclarativeEffect e) => e.Trigger != "onPlay" && e.Trigger != "onTurnStart");
	}

	public static string UpgradeTier(CardDef def)
	{
		if (IsFusedPerTriggerPermanent(def))
		{
			return "none";
		}
		IReadOnlyList<DeclarativeEffect> eff = AllEffectsOf(def);
		if (IsGreenRule(def))
		{
			if (eff.Any((DeclarativeEffect e) => e.Effect == "gainEnergyMax") && def.Cost >= 1 && !CostCutViolates(def))
			{
				return "cost";
			}
			if (V2_COST_ONLY.Contains(def.Id) && def.Cost >= 1 && !CostCutViolates(def))
			{
				return "cost";
			}
			if (V2_RANGE.ContainsKey(def.Id))
			{
				return "range";
			}
			if (V2_BONUS.Contains(def.Id) && BONUS_UPGRADES.ContainsKey(def.Id))
			{
				return "bonus";
			}
			if (V2_CASCADE_PLUS.Contains(def.Id) && def.Cascade.GetValueOrDefault() > 0)
			{
				return "cascade";
			}
			if (def.LightCost.GetValueOrDefault() >= 1)
			{
				return "light";
			}
			string t = TierV2(eff, def);
			if (t != "none")
			{
				return t;
			}
			if (def.Cost >= 1 && !CostCutViolates(def))
			{
				return "cost";
			}
			if (BONUS_UPGRADES.ContainsKey(def.Id))
			{
				return "bonus";
			}
			return "none";
		}
		if (eff.Any((DeclarativeEffect e) => e.Effect == "gainEnergyMax") && def.Cost >= 1 && !CostCutViolates(def))
		{
			return "cost";
		}
		if (def.Id == "green_perm_growth_tree" && def.Cost >= 1 && !CostCutViolates(def))
		{
			return "cost";
		}
		if (eff.Any((DeclarativeEffect e) => UPGRADABLE_EFFECTS.Contains(e.Effect) && e.Amount.HasValue))
		{
			return "amount";
		}
		if (eff.Any((DeclarativeEffect e) => e.Effect.StartsWith("dealDamagePer", StringComparison.Ordinal) && e.Effect != "dealDamagePerEnergyMax") && def.Cost >= 1 && def.Cost <= 2)
		{
			if (!BONUS_UPGRADES.ContainsKey(def.Id))
			{
				return "none";
			}
			return "bonus";
		}
		if (eff.Any((DeclarativeEffect e) => e.Effect == "dealDamagePerEnergyMax" || e.Effect == "gainBlockPerEnergyMax") && def.Cost == 1)
		{
			if (!BONUS_UPGRADES.ContainsKey(def.Id))
			{
				return "none";
			}
			return "bonus";
		}
		if (def.Cost >= 1 && !CostCutViolates(def))
		{
			return "cost";
		}
		if (eff.Any((DeclarativeEffect e) => UNIT_EFFECTS.Contains(e.Effect) && e.Amount.HasValue))
		{
			return "unit";
		}
		if (BONUS_UPGRADES.ContainsKey(def.Id))
		{
			return "bonus";
		}
		return "none";
	}

	public static bool IsUpgraded(CardInstance card)
	{
		return card.Def.Name.EndsWith("+", StringComparison.Ordinal);
	}

	public static bool CanUpgradeCard(CardInstance card)
	{
		if (!IsUpgraded(card))
		{
			return UpgradeTier(card.Def) != "none";
		}
		return false;
	}

	public static CardInstance UpgradeCard(CardInstance card)
	{
		string tier = UpgradeTier(card.Def);
		(int Max, int Min) w;
		if (tier == "range")
		{
			w = V2_RANGE[card.Def.Id];
			CardDef rangeDef = card.Def with
			{
				Name = card.Def.Name + "+",
				Effects = card.Def.Effects.Select(Widen).ToList()
			};
			if (card.Def.Modes != null)
			{
				rangeDef = rangeDef with
				{
					Modes = card.Def.Modes.Select((CardMode m) => m with
					{
						Effects = m.Effects.Select(Widen).ToList()
					}).ToList()
				};
			}
			return card with
			{
				Def = LegalizeUpgrade(rangeDef)
			};
		}
		if (IsGreenRule(card.Def) && tier != "cost" && tier != "bonus" && tier != "none" && tier != "light" && tier != "cascade")
		{
			CardDef baseDef = card.Def;
			CardDef greenDef = baseDef with
			{
				Name = baseDef.Name + "+",
				Effects = ApplyV2(baseDef.Effects, baseDef)
			};
			if (baseDef.Modes != null)
			{
				greenDef = greenDef with
				{
					Modes = baseDef.Modes.Select((CardMode m) => m with
					{
						Effects = ApplyV2(m.Effects, baseDef)
					}).ToList()
				};
			}
			if (baseDef.FreeIfMomentumAtLeast.HasValue && TierV2(baseDef.Effects, baseDef) == "threshold")
			{
				greenDef = greenDef with
				{
					FreeIfMomentumAtLeast = Math.Max(1, baseDef.FreeIfMomentumAtLeast.GetValueOrDefault() - 1)
				};
			}
			return card with
			{
				Def = LegalizeUpgrade(greenDef)
			};
		}
		Func<DeclarativeEffect, DeclarativeEffect> boostAmount = (DeclarativeEffect declarativeEffect) =>
		{
			if (!UPGRADABLE_EFFECTS.Contains(declarativeEffect.Effect) || !declarativeEffect.Amount.HasValue)
			{
				return declarativeEffect;
			}
			DeclarativeEffect declarativeEffect2 = declarativeEffect with
			{
				Amount = (int)Math.Ceiling((double)declarativeEffect.Amount.GetValueOrDefault() * 1.5)
			};
			if (declarativeEffect.AmountMax.HasValue)
			{
				declarativeEffect2 = declarativeEffect2 with
				{
					AmountMax = (int)Math.Ceiling((double)declarativeEffect.AmountMax.GetValueOrDefault() * 1.5)
				};
			}
			return declarativeEffect2;
		};
		Func<DeclarativeEffect, DeclarativeEffect> boostUnit = (DeclarativeEffect declarativeEffect) => (!UNIT_EFFECTS.Contains(declarativeEffect.Effect) || !declarativeEffect.Amount.HasValue) ? declarativeEffect : declarativeEffect with
		{
			Amount = declarativeEffect.Amount + 1
		};
		CardDef def = card.Def with
		{
			Name = card.Def.Name + "+"
		};
		switch (tier)
		{
		case "amount":
		case "unit":
		{
			Func<DeclarativeEffect, DeclarativeEffect> fn = ((tier == "amount") ? boostAmount : boostUnit);
			def = def with
			{
				Effects = card.Def.Effects.Select(fn).ToList()
			};
			if (card.Def.Modes != null)
			{
				def = def with
				{
					Modes = card.Def.Modes.Select((CardMode m) => m with
					{
						Effects = m.Effects.Select(fn).ToList()
					}).ToList()
				};
			}
			break;
		}
		case "cost":
			def = def with
			{
				Cost = card.Def.Cost - 1
			};
			break;
		case "light":
			def = def with
			{
				LightCost = Math.Max(0, card.Def.LightCost.GetValueOrDefault() - 1)
			};
			break;
		case "cascade":
			def = def with
			{
				Cascade = card.Def.Cascade.GetValueOrDefault() + 1
			};
			break;
		case "bonus":
		{
			List<DeclarativeEffect> acc = new List<DeclarativeEffect>(card.Def.Effects);
			IReadOnlyList<DeclarativeEffect> readOnlyList2;
			if (!BONUS_UPGRADES.TryGetValue(card.Def.Id, out IReadOnlyList<DeclarativeEffect> bl))
			{
				IReadOnlyList<DeclarativeEffect> readOnlyList = new List<DeclarativeEffect>();
				readOnlyList2 = readOnlyList;
			}
			else
			{
				readOnlyList2 = bl;
			}
			foreach (DeclarativeEffect b in readOnlyList2)
			{
				int i = -1;
				for (int k = 0; k < acc.Count; k++)
				{
					DeclarativeEffect e = acc[k];
					if (e.Effect == b.Effect && e.Trigger == b.Trigger && e.Target == b.Target && e.Amount.HasValue && b.Amount.HasValue)
					{
						i = k;
						break;
					}
				}
				if (i >= 0)
				{
					acc[i] = acc[i]with
					{
						Amount = acc[i].Amount.GetValueOrDefault() + b.Amount.GetValueOrDefault()
					};
					continue;
				}
				List<DeclarativeEffect> list = new List<DeclarativeEffect>();
				list.Add(b);
				list.AddRange(acc);
				acc = list;
			}
			def = def with
			{
				Effects = acc
			};
			break;
		}
		}
		def = LegalizeUpgradeModes(def, tier, boostUnit);
		return card with
		{
			Def = LegalizeUpgrade(def)
		};
		DeclarativeEffect Widen(DeclarativeEffect declarativeEffect)
		{
			EffectCondition c = declarativeEffect.Condition;
			if (c == null || (!c.MaxActionValue.HasValue && !c.MinActionValue.HasValue))
			{
				return declarativeEffect;
			}
			EffectCondition nc = c;
			if (c.MaxActionValue.HasValue && w.Max != 0)
			{
				nc = nc with
				{
					MaxActionValue = c.MaxActionValue + w.Max
				};
			}
			if (c.MinActionValue.HasValue && w.Min != 0)
			{
				nc = nc with
				{
					MinActionValue = Math.Max(1, c.MinActionValue.Value + w.Min)
				};
			}
			return declarativeEffect with
			{
				Condition = nc
			};
		}
	}

	private static CardDef LegalizeUpgradeModes(CardDef def, string tier, Func<DeclarativeEffect, DeclarativeEffect> boostUnit)
	{
		if (tier == "amount" && def.Modes != null)
		{
			return def with
			{
				Modes = def.Modes.Select((CardMode m) => (!m.Effects.Any((DeclarativeEffect e) => UPGRADABLE_EFFECTS.Contains(e.Effect) && e.Amount.HasValue)) ? m with
				{
					Effects = m.Effects.Select(boostUnit).ToList()
				} : m).ToList()
			};
		}
		return def;
	}

	private static CardDef LegalizeUpgrade(CardDef def0)
	{
		CardDef def1 = def0;
		string[] REFILL_FOR_LEGALITY = new string[9] { "drawCards", "drawCardsPerLight", "drawCardsPerCardPlayed", "dischargeAetherDraw", "impulseDraw", "retrieveFromExhaust", "playFromExhaust", "retrieveZeroCostFromDiscard", "drawTypeFromDeck" };
		IReadOnlyList<DeclarativeEffect> allEffects = AllEffectsOf(def1);
		int num = allEffects.Where((DeclarativeEffect e) => e.Effect == "gainEnergy" || e.Effect == "discountNext").Aggregate(0, (int a, DeclarativeEffect e) => a + e.Amount.GetValueOrDefault());
		bool refills = allEffects.Any((DeclarativeEffect e) => Enumerable.Contains(REFILL_FOR_LEGALITY, e.Effect));
		if (((num - def1.Cost >= 0) & refills) && def1.Exhaust != true)
		{
			def1 = def1 with
			{
				Exhaust = true
			};
		}
		return def1;
	}

	public static bool CanUpgradeInHand(CardInstance card)
	{
		if (card.Def.Rarity == "rare")
		{
			return false;
		}
		if (card.Def.Id.StartsWith("fused_", StringComparison.Ordinal) || card.Def.Id.StartsWith("fusion_", StringComparison.Ordinal))
		{
			return false;
		}
		return CanUpgradeCard(card);
	}
}
