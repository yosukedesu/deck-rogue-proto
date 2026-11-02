using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Effects
{
	public const int BLAZE_THRESHOLD = 8;

	private static readonly HashSet<string> DAMAGE_EFFECTS = new HashSet<string>
	{
		"dealDamage", "dealDamagePerCascade", "dealDamagePerBurn", "dealDamageRandom", "dealDamagePerCardPlayed", "dealDamagePerCardPlayedTotal", "dealDamagePerEnergyMax", "dealDamagePerAttackPlayed", "dealDamagePerWeak", "dealDamagePerMomentum",
		"dealDamagePerLight", "dealDamagePerSpark", "dealDamagePerHeal", "recycleExhaust", "dischargeAether", "dischargeLight", "dischargeGrowth", "dealDamageCleave", "dealDamagePerBlock", "dealDamagePerPermanent",
		"dealDamageDrain", "dealDamagePerExhaust", "dealDamageDrainPerExhaust", "dealDamagePerSelfHpLost", "dealDamagePerNegStrength", "dischargeBurn", "dischargeMomentumBurn", "shatterBlockConvert", "dealDamageExecute", "dealDamagePerDamageTaken",
		"dealDamagePerRandomPlayed", "dealDamagePerIceBlock", "dealDamagePerHandCard", "dealDamagePerScry", "dealDamagePerTrapFired", "counter"
	};

	private static readonly HashSet<string> ENEMY_TARGETED = new HashSet<string>
	{
		"dealDamage", "dealDamagePerCascade", "dealDamagePerBurn", "gainBlockPerBurn", "doubleBurn", "dealDamagePerScry", "dealDamagePerTrapFired", "dealDamageRandom", "dealDamagePerCardPlayed", "dealDamagePerCardPlayedTotal",
		"dealDamagePerEnergyMax", "dealDamagePerMomentum", "dealDamagePerLight", "dealDamagePerSpark", "dischargeAether", "dischargeLight", "applyBurn", "shatterBlock", "confuse", "exposeEnemy",
		"dischargeGrowth", "dealDamageCleave", "weakenEnemy", "dealDamagePerBlock", "dealDamagePerPermanent", "dealDamageDrain", "dealDamagePerExhaust", "dealDamageDrainPerExhaust", "dealDamagePerSelfHpLost", "dealDamagePerNegStrength",
		"dischargeBurn", "shatterBlockConvert", "dealDamageExecute", "dealDamagePerDamageTaken", "dealDamagePerRandomPlayed", "applyBurnPerDamageTaken", "dealDamagePerIceBlock", "dealDamagePerHandCard", "recycleExhaust", "playFromExhaust"
	};

	public static readonly HashSet<string> ANTHEM_EFFECTS = new HashSet<string> { "dealDamage", "dealDamageRandom", "dealDamageCleave", "dealDamageDrain", "gainBlock", "gainIceBlock", "gainHp" };

	public static readonly HashSet<string> DOLL_GROWTH_EFFECTS = new HashSet<string> { "dealDamage", "dealDamageRandom", "dealDamageCleave", "gainBlock" };

	public const int JUNK_CAP = 4;

	public const int WOUND_CAP = 5;

	public const int SCALD_CAP = 5;

	private static readonly HashSet<string> LIGHT_EFFECTS = new HashSet<string>
	{
		"addLight", "addLightNextTurn", "doubleLight", "lightCarryHalf", "dischargeLight", "dischargeLightRally", "dischargeLightWeaken", "dealDamagePerLight", "gainBlockPerLight", "drawCardsPerLight",
		"lightToSparks", "dismissUnlessLight"
	};

	private static readonly HashSet<string> FIRST_ALIVE_TRIGGERS = new HashSet<string> { "onCombatStart", "onTurnStart", "onTurnEnd", "onShuffle", "onImpulseExpired", "onScry", "onScryDiscard", "onSetExpired" };

	private static readonly HashSet<string> ENEMY_SINGLE_EFFECTS = new HashSet<string>
	{
		"dealDamage", "dealDamageRandom", "dealDamageDrain", "dealDamageExecute", "dealDamageCleave", "applyBurn", "exposeEnemy", "weakenEnemy", "confuse", "shatterBlock",
		"shatterBlockConvert", "doubleBurn", "dealDamagePerBurn", "gainBlockPerBurn", "strengthenEnemy"
	};

	private static readonly HashSet<string> REACTION_TRIGGERS = new HashSet<string> { "onAttackIncoming", "onAttacked", "onEnemyAction", "onEnemyBuffed", "onEnemyDefended" };

	private static EnemyState? EnemyAt(GameState state, int index)
	{
		if (index < 0 || index >= state.Enemies.Count)
		{
			return null;
		}
		return state.Enemies[index];
	}

	private static int FindAliveIndex(IReadOnlyList<EnemyState> enemies)
	{
		for (int i = 0; i < enemies.Count; i++)
		{
			if (enemies[i].Hp > 0)
			{
				return i;
			}
		}
		return -1;
	}

	private static IReadOnlyList<EnemyState> MapEnemy(IReadOnlyList<EnemyState> enemies, int index, Func<EnemyState, EnemyState> f)
	{
		List<EnemyState> list = new List<EnemyState>(enemies.Count);
		for (int i = 0; i < enemies.Count; i++)
		{
			list.Add((i == index) ? f(enemies[i]) : enemies[i]);
		}
		return list;
	}

	public static int BlazeTotal(GameState state)
	{
		int acc = 0;
		foreach (EnemyState e in state.Enemies)
		{
			acc += ((e.Hp > 0) ? e.Burn : 0);
		}
		return acc;
	}

	public static bool IsBlazing(GameState state)
	{
		return BlazeTotal(state) >= 8;
	}

	private static bool CardHasDamage(CardDef def)
	{
		foreach (DeclarativeEffect effect in def.Effects)
		{
			if (IsDamageEffect(effect))
			{
				return true;
			}
		}
		if (def.Modes != null)
		{
			foreach (CardMode mode in def.Modes)
			{
				foreach (DeclarativeEffect effect2 in mode.Effects)
				{
					if (IsDamageEffect(effect2))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public static int AuraCostUp(GameState state, CardInstance card)
	{
		int up = 0;
		foreach (EnemyState e in state.Enemies)
		{
			if (e.Hp > 0)
			{
				EnemyDefAura aura = Content.GetEnemyDef(e.EnemyId).Aura;
				if (!(aura == null) && (aura.CardType == null || !(aura.CardType != card.Def.Type)) && (aura.AttacksOnly != true || CardHasDamage(card.Def)))
				{
					up += aura.CostUp;
				}
			}
		}
		return up;
	}

	public static int EffectiveCost(GameState state, CardInstance card)
	{
		if (card.Def.XCost == true)
		{
			return Math.Max(0, state.Player.Energy);
		}
		if (card.FreeThisCombat == true)
		{
			return 0;
		}
		int up = AuraCostUp(state, card);
		string freeType = card.Def.FreeIfHandAll ?? ((card.Def.FreeIfHandAllPhysical == true) ? "physical" : null);
		if (freeType != null)
		{
			bool all = true;
			foreach (CardInstance c in state.Player.Hand)
			{
				if (!(c.Uid == card.Uid) && !((freeType == "nonphysical") ? (c.Def.Type != "physical") : (c.Def.Type == freeType)))
				{
					all = false;
					break;
				}
			}
			if (all)
			{
				return up;
			}
		}
		if (card.Def.FreeIfMomentumAtLeast.HasValue && state.Player.Momentum >= card.Def.FreeIfMomentumAtLeast.Value)
		{
			return up;
		}
		if (card.Def.Cost == 0)
		{
			return up;
		}
		int blaze = ((card.Def.BlazeDiscount.HasValue && IsBlazing(state)) ? card.Def.BlazeDiscount.Value : 0);
		return Math.Max(0, card.Def.Cost + up - blaze - state.Player.NextCardDiscount - card.RetainDiscount.GetValueOrDefault());
	}

	public static bool RetainerRequirementMet(GameState state, CardInstance card)
	{
		if (card.Def.RequiresRetainer != true)
		{
			return true;
		}
		foreach (CardInstance p in state.Player.Permanents)
		{
			if (p.Def.Retainer == true && p.Innate != true)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsPlayableFromHand(CardInstance card, GameState? state = null)
	{
		if (card.Def.Type == "reaction")
		{
			return false;
		}
		if (state != null && state.BrandsPlayable == true && IsBrandCard(card))
		{
			return true;
		}
		if (card.Def.Type == "permanent")
		{
			return true;
		}
		if ((card.Def.Modes?.Count ?? 0) > 0)
		{
			return true;
		}
		if (card.Def.Cascade.GetValueOrDefault() > 0)
		{
			return true;
		}
		foreach (DeclarativeEffect effect in card.Def.Effects)
		{
			if (effect.Trigger == "onPlay")
			{
				return true;
			}
		}
		return false;
	}

	public static bool ImpulseUnaffordable(GameState state, CardInstance card)
	{
		if (IsPlayableFromHand(card, state))
		{
			return EffectiveCost(state, card) > state.Player.Energy;
		}
		return false;
	}

	public static bool HasStaticEffect(GameState state, string effectName)
	{
		return state.Player.Permanents.Any((CardInstance p) => p.Def.Effects.Any((DeclarativeEffect e) => e.Effect == effectName));
	}

	public static bool BurnNoDecay(GameState state)
	{
		return HasStaticEffect(state, "burnNoDecay");
	}

	public static bool IsBrandCard(CardInstance card)
	{
		if (!(card.Def.Id == "status_brand"))
		{
			return card.Def.Id == "status_guilt";
		}
		return true;
	}

	public static bool IsDamageEffect(DeclarativeEffect effect)
	{
		return DAMAGE_EFFECTS.Contains(effect.Effect);
	}

	public static bool CardNeedsTarget(CardInstance card, int? modeIndex = null)
	{
		IReadOnlyList<CardMode> modes = card.Def.Modes ?? Array.Empty<CardMode>();
		IReadOnlyList<DeclarativeEffect> effects = ((modes.Count <= 0 || !modeIndex.HasValue || modeIndex.Value < 0 || modeIndex.Value >= modes.Count) ? card.Def.Effects.Where((DeclarativeEffect declarativeEffect) => declarativeEffect.Trigger == "onPlay").ToList() : modes[modeIndex.Value].Effects);
		foreach (DeclarativeEffect e in effects)
		{
			if (ENEMY_TARGETED.Contains(e.Effect) && e.Target != "all")
			{
				return true;
			}
		}
		return false;
	}

	public static int HearthSparkMax(GameState state)
	{
		DeclarativeEffect hearth = null;
		foreach (CardInstance permanent in state.Player.Permanents)
		{
			foreach (DeclarativeEffect e in permanent.Def.Effects)
			{
				if (hearth == null && e.Trigger == "onTurnEnd" && e.Effect == "lightToSparks")
				{
					hearth = e;
				}
			}
		}
		if (hearth == null)
		{
			return 0;
		}
		int per = Math.Max(1, hearth.Amount ?? 3);
		return state.Player.Light.GetValueOrDefault() / per;
	}

	private static GameState AfterLightDischarge(GameState state, int spent, int enemyIndex)
	{
		bool flag = state.Player.Permanents.Any((CardInstance p) => p.Def.Effects.Any((DeclarativeEffect e) => e.Effect == "lightCarryHalf"));
		GameState s = state;
		if (flag && spent >= 2)
		{
			int kept = spent / 2;
			s = s with
			{
				Player = s.Player with
				{
					Light = s.Player.Light.GetValueOrDefault() + kept
				}
			};
			s = Events.Emit(s, new GameEvent_LightGained
			{
				Amount = kept,
				Source = "carry"
			});
		}
		return RunPermanentTriggers(s, "onLightDischarged", enemyIndex);
	}

	public static bool IsDoll(CardInstance p)
	{
		if (p.Def.Retainer == true)
		{
			return p.Innate != true;
		}
		return false;
	}

	public static int DollAge(GameState state, CardInstance p)
	{
		if (p.EnteredTurn.HasValue)
		{
			return Math.Max(0, state.Turn - p.EnteredTurn.Value);
		}
		return 0;
	}

	public static int? DollLifeTotal(CardInstance p)
	{
		if (p.LifePersist == true || p.Def.LifePersist == true)
		{
			return null;
		}
		return (p.Def.Life ?? 3) + p.LifeBonus.GetValueOrDefault();
	}

	public static int? DollLifeLeft(GameState state, CardInstance p)
	{
		int? total = DollLifeTotal(p);
		if (total.HasValue)
		{
			return Math.Max(0, total.Value - DollAge(state, p));
		}
		return null;
	}

	public static int DollGrowth(GameState state, CardInstance p)
	{
		if (!IsDoll(p))
		{
			return 0;
		}
		return DollAge(state, p);
	}

	public static int? DollEffectAmount(GameState state, CardInstance p, DeclarativeEffect e, int anthem)
	{
		if (!e.Amount.HasValue)
		{
			return null;
		}
		if (p.Def.Retainer != true)
		{
			return e.Amount;
		}
		int a = ((anthem > 0 && ANTHEM_EFFECTS.Contains(e.Effect)) ? anthem : 0);
		int g = (DOLL_GROWTH_EFFECTS.Contains(e.Effect) ? DollGrowth(state, p) : 0);
		return e.Amount.Value + a + g;
	}

	public static int AnthemTotal(GameState state)
	{
		int n = 0;
		foreach (CardInstance permanent in state.Player.Permanents)
		{
			foreach (DeclarativeEffect e in permanent.Def.Effects)
			{
				if (e.Effect == "blessRetainers")
				{
					n += e.Amount.GetValueOrDefault();
				}
			}
		}
		return n;
	}

	public static (int Count, int Damage, int Block, int Heal) RallyPreview(GameState state, int lightAfter, IReadOnlyList<string>? extraIds = null)
	{
		int anthem = AnthemTotal(state);
		List<CardInstance> dolls = new List<CardInstance>();
		foreach (CardInstance p in state.Player.Permanents)
		{
			if (IsDoll(p))
			{
				dolls.Add(p);
			}
		}
		if (extraIds != null)
		{
			for (int i = 0; i < extraIds.Count; i++)
			{
				dolls.Add(new CardInstance
				{
					Uid = $"preview_{i}",
					Def = Content.GetCardDef(extraIds[i]),
					Token = true,
					EnteredTurn = state.Turn
				});
			}
		}
		int damage = 0;
		int block = 0;
		int heal = 0;
		int alive = Math.Max(1, state.Enemies.Count((EnemyState enemyState) => enemyState.Hp > 0));
		foreach (CardInstance p2 in dolls)
		{
			foreach (DeclarativeEffect e in p2.Def.Effects)
			{
				if (e.Trigger == "onPermanentEntered" || e.Trigger == "onPlay")
				{
					continue;
				}
				int amount = DollEffectAmount(state, p2, e, anthem).GetValueOrDefault();
				int aoe = ((!(e.Target == "all")) ? 1 : alive);
				if (e.Effect == "dealDamage")
				{
					damage += PlayerDamageAfterModifiers(state, amount) * aoe;
				}
				else if (e.Effect == "dealDamagePerLight")
				{
					int lit = Math.Max(0, lightAfter) / 2 * (e.Amount ?? 1);
					if (lit > 0)
					{
						damage += PlayerDamageAfterModifiers(state, lit) * aoe;
					}
				}
				else if (e.Effect == "gainBlock")
				{
					block += amount;
				}
				else if (e.Effect == "gainHp")
				{
					heal += amount;
				}
			}
		}
		return (Count: dolls.Count, Damage: damage, Block: block, Heal: heal);
	}

	public static (GameState State, string Uid) NewSummonUid(GameState state, string id)
	{
		int n = state.SummonSeq.GetValueOrDefault();
		return (State: state with
		{
			SummonSeq = n + 1
		}, Uid: $"summon_s{n}_{id}");
	}

	private static GameState CopyDoll(GameState state0, CardInstance src, int enemyIndex, bool noTwin)
	{
		(GameState State, string Uid) tuple = NewSummonUid(state0, src.Def.Id);
		GameState state1 = tuple.State;
		string uid = tuple.Uid;
		CardInstance token = new CardInstance
		{
			Uid = uid,
			Def = src.Def,
			Token = true,
			EnteredTurn = src.EnteredTurn,
			LifeBonus = src.LifeBonus,
			LifePersist = ((src.LifePersist == true) ? new bool?(true) : ((bool?)null))
		};
		return EnterPermanent(Events.Emit(state1 with
		{
			Player = state1.Player with
			{
				Permanents = state1.Player.Permanents.Concat(new CardInstance[1] { token }).ToList()
			}
		}, new GameEvent_RetainerCopied
		{
			CardId = src.Def.Id,
			Uid = token.Uid,
			FromUid = src.Uid
		}), token.Uid, enemyIndex, null, noTwin);
	}

	public static GameState ExpireRetainers(GameState state)
	{
		List<CardInstance> gone = new List<CardInstance>();
		foreach (CardInstance p in state.Player.Permanents)
		{
			if (IsDoll(p))
			{
				int? total = DollLifeTotal(p);
				if (total.HasValue && DollAge(state, p) >= total.Value - 1)
				{
					gone.Add(p);
				}
			}
		}
		if (gone.Count == 0)
		{
			return state;
		}
		GameState s = state with
		{
			Player = state.Player with
			{
				Permanents = state.Player.Permanents.Where((CardInstance item) => !gone.Contains(item)).ToList()
			}
		};
		foreach (CardInstance p2 in gone)
		{
			s = Events.Emit(s, new GameEvent_RetainerExpired
			{
				CardId = p2.Def.Id,
				Uid = p2.Uid
			});
		}
		return s;
	}

	public static GameState RunPermanentTriggers(GameState state, string trigger, int enemyIndex, Func<CardInstance, bool>? only = null)
	{
		EnemyState at = EnemyAt(state, enemyIndex);
		int alive = ((at != null && at.Hp > 0) ? enemyIndex : FindAliveIndex(state.Enemies));
		bool prevCardPlay = state.ResolvingCardPlay == true;
		string prevPermanentUid = state.ResolvingPermanentUid;
		GameState s = state with
		{
			ResolvingCardPlay = false
		};
		int anthem = 0;
		foreach (CardInstance permanent2 in state.Player.Permanents)
		{
			foreach (DeclarativeEffect e in permanent2.Def.Effects)
			{
				if (e.Effect == "blessRetainers")
				{
					anthem += e.Amount.GetValueOrDefault();
				}
			}
		}
		foreach (CardInstance permanent in state.Player.Permanents)
		{
			if (only != null && !only(permanent))
			{
				continue;
			}
			for (int ei = 0; ei < permanent.Def.Effects.Count; ei++)
			{
				DeclarativeEffect effect = permanent.Def.Effects[ei];
				if (!(effect.Trigger == trigger) || !BlazeConditionMet(s, effect, enemyIndex))
				{
					continue;
				}
				if (effect.Every.HasValue || effect.Once != null)
				{
					bool turnScope = effect.Once == "turn" || effect.EveryScope == "turn";
					CardInstance cur = null;
					foreach (CardInstance p in s.Player.Permanents)
					{
						if (p.Uid == permanent.Uid)
						{
							cur = p;
							break;
						}
					}
					if (cur == null)
					{
						continue;
					}
					Dictionary<string, int> map = new Dictionary<string, int>();
					IReadOnlyDictionary<string, int> src = (turnScope ? cur.TurnTriggerCounts : cur.TriggerCounts);
					if (src != null)
					{
						foreach (KeyValuePair<string, int> kv in src)
						{
							map[kv.Key] = kv.Value;
						}
					}
					string key = ei.ToString();
					int n = (map[key] = (map.TryGetValue(key, out var prev) ? prev : 0) + 1);
					List<CardInstance> perms = new List<CardInstance>(s.Player.Permanents.Count);
					foreach (CardInstance p2 in s.Player.Permanents)
					{
						List<CardInstance> list = perms;
						CardInstance item;
						if (p2.Uid == permanent.Uid)
						{
							item = (turnScope ? p2 with
							{
								TurnTriggerCounts = map
							} : p2 with
							{
								TriggerCounts = map
							});
						}
						else
						{
							item = p2;
						}
						list.Add(item);
					}
					s = s with
					{
						Player = s.Player with
						{
							Permanents = perms
						}
					};
					if (!((effect.Once != null) ? (n == (effect.Every ?? 1)) : (n % (effect.Every ?? 1) == 0)))
					{
						continue;
					}
				}
				int? liveAmount = DollEffectAmount(state, permanent, effect, anthem);
				DeclarativeEffect boosted = ((liveAmount.HasValue && liveAmount != effect.Amount) ? effect with
				{
					Amount = liveAmount
				} : effect);
				bool isInnate = permanent.Innate == true;
				s = s with
				{
					ResolvingPermanentUid = permanent.Uid
				};
				int target = alive;
				if (boosted.Target == "mostHp")
				{
					int best = -1;
					for (int i = 0; i < s.Enemies.Count; i++)
					{
						if (s.Enemies[i].Hp > 0 && (best < 0 || s.Enemies[i].Hp > s.Enemies[best].Hp))
						{
							best = i;
						}
					}
					if (best >= 0)
					{
						target = best;
					}
				}
				else if (boosted.Target == "mostBurn")
				{
					int best2 = -1;
					for (int j = 0; j < s.Enemies.Count; j++)
					{
						if (s.Enemies[j].Hp > 0 && (best2 < 0 || s.Enemies[j].Burn > s.Enemies[best2].Burn))
						{
							best2 = j;
						}
					}
					if (best2 >= 0)
					{
						target = best2;
					}
				}
				else if (permanent.Def.Retainer == true && permanent.Innate != true && IsDamageEffect(boosted) && boosted.Target != "all")
				{
					List<int> aliveIdx = new List<int>();
					for (int k = 0; k < s.Enemies.Count; k++)
					{
						if (s.Enemies[k].Hp > 0)
						{
							aliveIdx.Add(k);
						}
					}
					if (aliveIdx.Count > 1)
					{
						(int Value, RngState Next) tuple = Rng.NextInt(s.Rng, 0, aliveIdx.Count - 1);
						int k2 = tuple.Value;
						RngState rng = tuple.Next;
						s = s with
						{
							Rng = rng
						};
						target = aliveIdx[k2];
					}
				}
				for (int rep = 0; rep < ((permanent.Def.Echo != true) ? 1 : 2); rep++)
				{
					GameState next = ResolveEffectTargeted(isInnate ? s with
					{
						InnateResolving = true
					} : s, boosted, target, new DamagePresentationSource
					{
						Kind = "permanent",
						DefinitionId = permanent.Def.Id,
						Uid = permanent.Uid
					});
					if (isInnate)
					{
						next = next with
						{
							InnateResolving = false
						};
					}
					s = next;
				}
			}
		}
		return s with
		{
			ResolvingCardPlay = prevCardPlay,
			ResolvingPermanentUid = prevPermanentUid
		};
	}

	public static GameState HealPlayer(GameState state, int amount, int enemyIndex)
	{
		if (amount <= 0)
		{
			return state;
		}
		int healed = Math.Min(amount, state.Player.MaxHp - state.Player.Hp);
		GameState s = state with
		{
			Player = state.Player with
			{
				Hp = state.Player.Hp + Math.Max(0, healed),
				HealsThisCombat = state.Player.HealsThisCombat + 1,
				HealsThisTurn = state.Player.HealsThisTurn.GetValueOrDefault() + ((state.ResolvingCardPlay == true) ? 1 : 0)
			}
		};
		s = Events.Emit(s, new GameEvent_HpHealed
		{
			Amount = healed,
			SourceUid = state.ResolvingPermanentUid,
			PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, s.Player)
		});
		s = RunPermanentTriggers(s, "onHealed", enemyIndex);
		return GainLight(s, 1, "heal", enemyIndex);
	}

	public static GameState GainLight(GameState state, int amount, string source, int enemyIndex)
	{
		if (amount <= 0)
		{
			return state;
		}
		if (state.SuppressLightGain == true)
		{
			return state;
		}
		return FireGainTrigger(Events.Emit(state with
		{
			Player = state.Player with
			{
				Light = state.Player.Light.GetValueOrDefault() + amount
			}
		}, new GameEvent_LightGained
		{
			Amount = amount,
			Source = source,
			SourceUid = state.ResolvingPermanentUid
		}), "onLightGained", enemyIndex);
	}

	public static GameState EnterPermanent(GameState state, string uid, int enemyIndex, Func<CardInstance, bool>? only = null, bool noTwin = false)
	{
		CardInstance entered0 = state.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == uid);
		if (entered0 == null)
		{
			return state;
		}
		GameState s = state with
		{
			LastEnteredPermanentUid = uid
		};
		if (IsDoll(entered0) && !entered0.EnteredTurn.HasValue)
		{
			List<CardInstance> perms = new List<CardInstance>(s.Player.Permanents.Count);
			foreach (CardInstance p in s.Player.Permanents)
			{
				perms.Add((p.Uid == uid) ? p with
				{
					EnteredTurn = state.Turn
				} : p);
			}
			s = s with
			{
				Player = s.Player with
				{
					Permanents = perms
				}
			};
		}
		CardInstance entered1 = s.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == uid) ?? entered0;
		s = Events.Emit(s, new GameEvent_PermanentPlayed
		{
			CardId = entered1.Def.Id
		});
		s = RunPermanentTriggers(s, "onPermanentEntered", enemyIndex, only);
		if (entered1.Def.Retainer != true || entered1.Innate == true)
		{
			return s;
		}
		List<string> triggers = new List<string>();
		foreach (DeclarativeEffect e in entered1.Def.Effects)
		{
			if (e.Trigger != "onPermanentEntered" && e.Trigger != "onPlay" && !triggers.Contains(e.Trigger))
			{
				triggers.Add(e.Trigger);
			}
		}
		if (triggers.Count > 0)
		{
			foreach (string tr in triggers)
			{
				s = RunPermanentTriggers(s, tr, enemyIndex, (CardInstance cardInstance) => cardInstance.Uid == uid);
			}
			s = Events.Emit(s, new GameEvent_RetainerRushed
			{
				CardId = entered1.Def.Id
			});
		}
		if (!noTwin && s.NextRetainerTwin.GetValueOrDefault() > 0)
		{
			s = s with
			{
				NextRetainerTwin = s.NextRetainerTwin.GetValueOrDefault() - 1
			};
			CardInstance src = s.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == uid);
			if (src != null)
			{
				s = CopyDoll(s, src, enemyIndex, noTwin: true);
			}
		}
		return s;
	}

	private static GameState RallyRetainers(GameState state, int enemyIndex)
	{
		Func<CardInstance, bool> isRetainer = (CardInstance cardInstance) => cardInstance.Def.Retainer == true && cardInstance.Innate != true;
		List<string> triggers = new List<string>();
		foreach (CardInstance p in state.Player.Permanents)
		{
			if (!isRetainer(p))
			{
				continue;
			}
			foreach (DeclarativeEffect e in p.Def.Effects)
			{
				if (e.Trigger != "onPermanentEntered" && e.Trigger != "onPlay" && !triggers.Contains(e.Trigger))
				{
					triggers.Add(e.Trigger);
				}
			}
		}
		bool prev = state.SuppressLightGain == true;
		GameState s = state with
		{
			SuppressLightGain = true
		};
		foreach (string tr in triggers)
		{
			s = RunPermanentTriggers(s, tr, enemyIndex, isRetainer);
		}
		return s with
		{
			SuppressLightGain = prev
		};
	}

	public static GameState MillPlayerDeck(GameState state, int amount, int enemyIndex)
	{
		int n = Math.Min(amount, state.Player.DrawPile.Count);
		if (n <= 0)
		{
			return state;
		}
		List<CardInstance> milled = state.Player.DrawPile.Take(n).ToList();
		return FireNecroEffects(FireExhaustTriggers(Events.Emit(state with
		{
			Player = state.Player with
			{
				DrawPile = state.Player.DrawPile.Skip(n).ToList(),
				ExhaustPile = state.Player.ExhaustPile.Concat(milled).ToList()
			}
		}, new GameEvent_CardsMilled
		{
			Count = n,
			CardIds = milled.Select((CardInstance c) => c.Def.Id).ToList()
		}), n, enemyIndex), milled, enemyIndex);
	}

	public static GameState GainPlayerBlock(GameState state, int amount, int enemyIndex)
	{
		if (amount <= 0)
		{
			return state;
		}
		if (state.ResolvingCardPlay == true && state.Player.Frail > 0)
		{
			amount = Math.Max(1, (int)Math.Floor((double)amount * 0.75));
		}
		GameState s = state with
		{
			Player = state.Player with
			{
				Block = state.Player.Block + amount
			}
		};
		s = Events.Emit(s, new GameEvent_BlockGained
		{
			Target = "player",
			Amount = amount,
			SourceUid = state.ResolvingPermanentUid,
			PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, s.Player)
		});
		s = AngerGuardWatchers(s);
		return RunPermanentTriggers(s, "onBlockGained", enemyIndex);
	}

	private static GameState AngerGuardWatchers(GameState state)
	{
		if (state.InnateResolving == true)
		{
			return state;
		}
		if (state.ResolvingCardPlay != true || state.AngerFiredThisPlay == true)
		{
			return state;
		}
		bool anyWatcher = false;
		foreach (EnemyState e in state.Enemies)
		{
			if (e.Hp > 0 && Content.GetEnemyDef(e.EnemyId).AngerOnBlock.HasValue)
			{
				anyWatcher = true;
				break;
			}
		}
		if (!anyWatcher)
		{
			return state;
		}
		GameState s = state with
		{
			AngerFiredThisPlay = true
		};
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			int? anger = Content.GetEnemyDef(s.Enemies[i].EnemyId).AngerOnBlock;
			if (s.Enemies[i].Hp > 0 && anger.HasValue)
			{
				int gain = anger.Value;
				s = GainEnemyStrength(s, i, gain);
				s = Events.Emit(s, new GameEvent_StrengthGained
				{
					EnemyIndex = i,
					Amount = gain
				});
			}
		}
		return s;
	}

	public static GameState GainPlayerIceBlock(GameState state, int amount)
	{
		if (amount <= 0)
		{
			return state;
		}
		GameState s = state with
		{
			Player = state.Player with
			{
				IceBlock = state.Player.IceBlock + amount
			}
		};
		s = Events.Emit(s, new GameEvent_IceBlockGained
		{
			Amount = amount,
			PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, s.Player)
		});
		return AngerGuardWatchers(s);
	}

	public static GameState LosePlayerHp(GameState state, int amount, int enemyIndex)
	{
		if (amount <= 0)
		{
			return state;
		}
		GameState s = state with
		{
			Player = state.Player with
			{
				Hp = state.Player.Hp - amount,
				SelfHpLost = state.Player.SelfHpLost + amount
			}
		};
		s = Events.Emit(s, new GameEvent_HpLost
		{
			Amount = amount,
			PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, s.Player)
		});
		return RunPermanentTriggers(s, "onHpLost", enemyIndex);
	}

	public static GameState FireExhaustTriggers(GameState state, int count, int enemyIndex)
	{
		GameState s = state;
		for (int i = 0; i < count; i++)
		{
			s = RunPermanentTriggers(s, "onCardExhausted", enemyIndex);
		}
		return s;
	}

	public static GameState FireNecroEffects(GameState state, IReadOnlyList<CardInstance> cards, int enemyIndex)
	{
		GameState s = state;
		foreach (CardInstance card in cards)
		{
			List<DeclarativeEffect> necro = card.Def.Effects.Where((DeclarativeEffect e) => e.Trigger == "onSelfExhausted").ToList();
			if (necro.Count == 0)
			{
				continue;
			}
			s = Events.Emit(s, new GameEvent_NecroFired
			{
				CardId = card.Def.Id
			});
			foreach (DeclarativeEffect effect in necro)
			{
				EnemyState at = EnemyAt(s, enemyIndex);
				int idx = ((at != null && at.Hp > 0) ? enemyIndex : Math.Max(0, FindAliveIndex(s.Enemies)));
				s = ResolveEffectTargeted(s, effect, idx, new DamagePresentationSource
				{
					Kind = "necro",
					DefinitionId = card.Def.Id,
					Uid = card.Uid
				});
			}
		}
		return s;
	}

	public static GameState ResolveEffectTargeted(GameState state, DeclarativeEffect effect, int enemyIndex, DamagePresentationSource? presentationSource = null)
	{
		GameState s = state;
		if (effect.Target != "all")
		{
			s = ResolveEffect(state, effect, enemyIndex);
		}
		else
		{
			for (int i = 0; i < s.Enemies.Count; i++)
			{
				if (s.Enemies[i].Hp > 0)
				{
					s = ResolveEffect(s, effect, i);
				}
			}
		}
		return AnnotateDamagePresentation(s, state.EventLog.Count, presentationSource);
	}

	public static GameState AnnotateDamagePresentation(GameState state, int from, DamagePresentationSource? source)
	{
		if (source == null)
		{
			return state;
		}
		List<GameEvent> log = null;
		for (int i = from; i < state.EventLog.Count; i++)
		{
			if (state.EventLog[i] is GameEvent_DamageDealt hit && !(hit.Source != "player") && !(hit.PresentationSource != null))
			{
				if (log == null)
				{
					log = new List<GameEvent>(state.EventLog);
				}
				log[i] = hit with
				{
					PresentationSource = source
				};
			}
		}
		if (log != null)
		{
			return state with
			{
				EventLog = log
			};
		}
		return state;
	}

	public static bool ReactionMatches(GameState state, CardInstance card, ReactionWindow win)
	{
		foreach (DeclarativeEffect e in SetAny.SetEffectsOf(card))
		{
			bool num;
			if (!(win.Stage == "pre"))
			{
				if ((!(e.Trigger == "onAttacked") || !(win.Kind == "attack")) && (!(e.Trigger == "onEnemyBuffed") || (!(win.Kind == "buff") && !(win.Kind == "rally"))))
				{
					if (!(e.Trigger == "onEnemyDefended"))
					{
						continue;
					}
					num = win.Kind == "defend";
					goto IL_00fe;
				}
			}
			else if (!(e.Trigger == "onEnemyAction"))
			{
				if (!(e.Trigger == "onAttackIncoming") || !(win.Kind == "attack"))
				{
					continue;
				}
				num = win.Confused != true;
				goto IL_00fe;
			}
			goto IL_0103;
			IL_00fe:
			if (!num)
			{
				continue;
			}
			goto IL_0103;
			IL_0103:
			EffectCondition c = e.Condition;
			if (c == null)
			{
				return true;
			}
			if ((!c.HpAtOrBelowRatio.HasValue || !((double)state.Player.Hp > (double)state.Player.MaxHp * c.HpAtOrBelowRatio.Value)) && (!c.MinDamageTaken.HasValue || (!(win.Stage != "post") && win.HpLoss >= c.MinDamageTaken.Value)) && (!c.MaxActionValue.HasValue || (!(win.Stage != "pre") && win.Actual <= c.MaxActionValue.Value)) && (!c.MinActionValue.HasValue || win.Actual >= c.MinActionValue.Value) && (c.Blaze != true || IsBlazing(state)) && (!c.MinGrowth.HasValue || state.Player.Growth >= c.MinGrowth.Value) && (!c.MinMomentum.HasValue || state.Player.Momentum >= c.MinMomentum.Value) && (!c.MinLight.HasValue || state.Player.Light.GetValueOrDefault() >= c.MinLight.Value) && (!c.MinEnergyMax.HasValue || state.Player.EnergyMaxAtTurnStart >= c.MinEnergyMax.Value) && (c.ActionKinds == null || c.ActionKinds.Contains(win.Kind)) && (c.ActionKindsNot == null || !c.ActionKindsNot.Contains(win.Kind)) && (c.HealedThisTurn != true || state.Player.HealsThisTurn.GetValueOrDefault() > 0))
			{
				return true;
			}
		}
		return false;
	}

	public static int TrapAge(GameState state, CardInstance card)
	{
		return state.Turn - (card.SetTurn ?? (state.Turn - 1));
	}

	public static bool AetherCarriesHalf(GameState state)
	{
		return state.Player.Permanents.Any((CardInstance p) => p.Def.Effects.Any((DeclarativeEffect e) => e.Effect == "aetherCarryHalf"));
	}

	public static bool TrapsNeverExpire(GameState state)
	{
		return state.Player.Permanents.Any((CardInstance p) => p.Def.Effects.Any((DeclarativeEffect e) => e.Effect == "trapsNeverExpire"));
	}

	public static int? TrapLifeOf(GameState state, CardInstance card)
	{
		if (card.Def.TrapPersist == true || TrapsNeverExpire(state))
		{
			return null;
		}
		return 2 + card.TrapLifeBonus.GetValueOrDefault();
	}

	public static bool IsTrapLive(GameState state, CardInstance card)
	{
		int age = TrapAge(state, card);
		int? life = TrapLifeOf(state, card);
		if (age >= 1)
		{
			if (life.HasValue)
			{
				return age <= life.Value;
			}
			return true;
		}
		return false;
	}

	public static bool SetBellActive(GameState state)
	{
		if (state.SetDamageReduction.GetValueOrDefault() > 0)
		{
			return state.BellLocked ?? (state.Player.SetCards.Count > 0);
		}
		return false;
	}

	public static int AttackHitsOf(GameState state, bool? mirrorHits, int? hits)
	{
		if (mirrorHits != true)
		{
			return hits ?? 1;
		}
		return Math.Max(1, state.Player.CardsPlayedThisTurn + state.Player.SetsThisTurn.GetValueOrDefault());
	}

	public static int AttackHitsOf(GameState state, EnemyIntent it)
	{
		return AttackHitsOf(state, it.MirrorHits, it.Hits);
	}

	public static int ReactionActionValue(GameState state, int enemyIndex)
	{
		EnemyIntent it = EffectiveIntent(state, enemyIndex);
		if (it == null)
		{
			if (!(state.LastAction != null) || state.LastAction.EnemyIndex != enemyIndex)
			{
				return 0;
			}
			return state.LastAction.Actual;
		}
		if (!(it.Kind == "attack"))
		{
			return it.Actual;
		}
		return it.Actual * AttackHitsOf(state, it);
	}

	public static ReactionWindow PreWindowFor(GameState state, int enemyIndex)
	{
		string kind = EffectiveIntent(state, enemyIndex)?.Kind ?? "attack";
		bool confused = kind == "attack" && (EnemyAt(state, enemyIndex)?.Confusion ?? 0) > 0;
		return new ReactionWindow
		{
			Stage = "pre",
			Kind = kind,
			Actual = ReactionActionValue(state, enemyIndex),
			Confused = (confused ? new bool?(true) : ((bool?)null))
		};
	}

	public static bool TrapCanFireThisPhase(GameState state, CardInstance card)
	{
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			if (state.Enemies[i].Hp <= 0)
			{
				continue;
			}
			EnemyIntent it = EffectiveIntent(state, i);
			if (!(it == null))
			{
				int v = ReactionActionValue(state, i);
				if (ReactionMatches(state, card, PreWindowFor(state, i)))
				{
					return true;
				}
				if (ReactionMatches(state, card, new ReactionWindow
				{
					Stage = "post",
					Kind = it.Kind,
					Actual = v,
					HpLoss = v
				}))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static string TrapStatusText(GameState state, CardInstance card)
	{
		if (!TrapLifeOf(state, card).HasValue)
		{
			if (TrapAge(state, card) != 0)
			{
				return "期限なし";
			}
			return "準備中（次のターンから発動できる・期限なし）";
		}
		if (TrapAge(state, card) <= 0)
		{
			if (card.TrapLifeBonus.GetValueOrDefault() <= 0)
			{
				return "準備中（次のターンから発動できる）";
			}
			return $"準備中（次のターンから発動できる・あと{TrapWindowsLeft(state, card)}回）";
		}
		int left = TrapWindowsLeft(state, card).GetValueOrDefault();
		bool quiet = state.Phase == "player-turn" && !TrapCanFireThisPhase(state, card);
		if (left >= 3)
		{
			if (quiet)
			{
				return $"あと{left}回の敵ターン。今ターンの敵の行動では発動しない＝実質あと{left - 1}回";
			}
			return $"あと{left}回の敵ターン（発動しなければ期限切れで捨て札へ）";
		}
		if (left >= 2)
		{
			if (!quiet)
			{
				return "あと2回の敵ターン（発動しなければ期限切れで捨て札へ）";
			}
			return "あと2回の敵ターン。今ターンの敵の行動では発動しない＝実質あと1回";
		}
		if (!quiet)
		{
			return "あと1回（このターンで発動しなければ期限切れで捨て札へ）";
		}
		return "あと1回。今ターンの敵の行動では発動しない＝このターンの終わりに期限切れ";
	}

	public static string TrapStatusTextKarakuri(GameState state, CardInstance card)
	{
		if (!TrapLifeOf(state, card).HasValue)
		{
			if (TrapAge(state, card) != 0)
			{
				return "期限なし";
			}
			return "準備中（次のターンから鳴る・期限なし）";
		}
		if (TrapAge(state, card) <= 0)
		{
			if (card.TrapLifeBonus.GetValueOrDefault() <= 0)
			{
				return "準備中（次のターンから鳴る）";
			}
			return $"準備中（次のターンから鳴る・あと{TrapWindowsLeft(state, card)}回）";
		}
		int left = TrapWindowsLeft(state, card).GetValueOrDefault();
		bool quiet = state.Phase == "player-turn" && !TrapCanFireThisPhase(state, card);
		if (left >= 3)
		{
			if (quiet)
			{
				return $"あと{left}回。今の敵の構えでは鳴らない＝実質あと{left - 1}回";
			}
			return $"あと{left}回（鳴らなければ期限切れ）";
		}
		if (left >= 2)
		{
			if (!quiet)
			{
				return "あと2回（鳴らなければ期限切れ）";
			}
			return "あと2回。今の敵の構えでは鳴らない＝実質あと1回";
		}
		if (!quiet)
		{
			return "あと1回（鳴らなければ期限切れ）";
		}
		return "あと1回。今の敵の構えでは鳴らない＝このターンの終わりに期限切れ";
	}

	public static int? TrapWindowsLeft(GameState state, CardInstance card)
	{
		int? life = TrapLifeOf(state, card);
		if (!life.HasValue)
		{
			return null;
		}
		int age = TrapAge(state, card);
		return Math.Max(0, life.Value + 1 - Math.Max(1, age));
	}

	public static EnemyIntent? EffectiveIntent(GameState state, int enemyIndex)
	{
		EnemyIntent intent = EnemyAt(state, enemyIndex)?.Intent;
		if (intent == null)
		{
			return null;
		}
		if (intent.ConditionalOn == null || intent.Alt == null)
		{
			return intent;
		}
		if (!((!(intent.ConditionalOn == "set")) ? HasHuntableTokens(state) : state.Player.SetCards.Any((CardInstance c) => IsTrapLive(state, c))))
		{
			return intent;
		}
		return new EnemyIntent
		{
			Kind = intent.Alt.Kind,
			Actual = intent.Alt.Actual,
			Hits = intent.Alt.Hits,
			Inflict = intent.Alt.Inflict,
			AlsoDefend = intent.Alt.AlsoDefend,
			AlsoBuff = intent.Alt.AlsoBuff,
			AlsoDestroySet = intent.Alt.AlsoDestroySet,
			ConditionalOn = intent.ConditionalOn,
			Alt = intent.Alt
		};
	}

	public static IReadOnlyList<int> SetBranchFlipRisks(GameState state)
	{
		PendingWindow pending = state.PendingWindow;
		if (pending == null || state.Player.SetCards.Count != 1)
		{
			return Array.Empty<int>();
		}
		List<int> risks = new List<int>();
		for (int i = pending.EnemyIndex + 1; i < state.Enemies.Count; i++)
		{
			EnemyState e = state.Enemies[i];
			if (e.Hp > 0 && e.Intent?.ConditionalOn == "set" && e.Intent.Alt != null)
			{
				risks.Add(i);
			}
		}
		return risks;
	}

	public static int CountedPermanents(GameState state)
	{
		int n = 0;
		foreach (CardInstance permanent in state.Player.Permanents)
		{
			if (permanent.Innate != true)
			{
				n++;
			}
		}
		return n;
	}

	public static bool HasHuntableTokens(GameState state)
	{
		foreach (CardInstance p in state.Player.Permanents)
		{
			if (p.Token == true || p.Def.Retainer == true)
			{
				return true;
			}
		}
		return false;
	}

	public static bool CanSaveFromLethal(CardInstance card, GameState? state = null)
	{
		int healCap = 0;
		foreach (DeclarativeEffect e in card.Def.Effects)
		{
			if (e.Effect == "gainHp")
			{
				healCap += e.Amount.GetValueOrDefault();
			}
			else if (e.Effect == "dealDamageDrain")
			{
				healCap += (int)Math.Floor((double)e.Amount.GetValueOrDefault() / 2.0);
			}
			else if (e.Effect == "dealDamageDrainPerExhaust")
			{
				int n = state?.Player.ExhaustPile.Count ?? 0;
				healCap += (int)Math.Floor((double)e.Amount.GetValueOrDefault() * (double)n / 2.0);
			}
		}
		if (healCap <= 0)
		{
			return false;
		}
		if (state != null && state.Player.Hp <= 0)
		{
			return healCap >= 1 - state.Player.Hp;
		}
		return true;
	}

	public static IReadOnlyList<CardInstance> UsableSetCards(GameState state, ReactionWindow win)
	{
		List<CardInstance> matched = state.Player.SetCards.Where((CardInstance c) => IsTrapLive(state, c) && ReactionMatches(state, c, win) && SetAny.SetFireCost(c) <= state.Player.Energy).ToList();
		if (state.Player.Hp > 0)
		{
			return matched;
		}
		return matched.Where((CardInstance c) => CanSaveFromLethal(c, state)).ToList();
	}

	public static IReadOnlyList<CardInstance> UnaffordableSetCards(GameState state, ReactionWindow win)
	{
		return state.Player.SetCards.Where((CardInstance c) => IsTrapLive(state, c) && ReactionMatches(state, c, win) && SetAny.SetFireCost(c) > state.Player.Energy).ToList();
	}

	public static bool PlayerCanSet(GameState state)
	{
		IReadOnlyList<CardInstance>[] array = new IReadOnlyList<CardInstance>[4]
		{
			state.Player.Hand,
			state.Player.DrawPile,
			state.Player.DiscardPile,
			state.Player.SetCards
		};
		for (int i = 0; i < array.Length; i++)
		{
			foreach (CardInstance item in array[i])
			{
				if (item.Def.Type == "reaction")
				{
					return true;
				}
			}
		}
		return false;
	}

	public static ReactionWindow? WindowFromPending(GameState state)
	{
		PendingWindow pending = state.PendingWindow;
		if (pending == null)
		{
			return null;
		}
		EnemyIntent intent = EffectiveIntent(state, pending.EnemyIndex);
		if (intent == null)
		{
			return null;
		}
		if (pending.Stage == "pre")
		{
			return PreWindowFor(state, pending.EnemyIndex);
		}
		return new ReactionWindow
		{
			Stage = "post",
			Kind = intent.Kind,
			HpLoss = (state.LastAction?.HpLoss ?? 0),
			Actual = ReactionActionValue(state, pending.EnemyIndex)
		};
	}

	public static int PlayerDamageAfterModifiers(GameState state, int baseAmount)
	{
		int momentum = ((state.ResolvingCardPlay == true) ? state.Player.Momentum : 0);
		int amount = baseAmount + state.Player.Growth + momentum;
		if (((state.Phase == "player-turn") ? state.Player.Weak : (state.Player.Weak - state.Player.WeakFreshThisPhase.GetValueOrDefault())) <= 0 || amount <= 0)
		{
			return amount;
		}
		return Math.Max(1, (int)Math.Floor((double)amount * 0.75));
	}

	public static string SetCardLiveDamage(GameState state, CardDef def, int? enemyIndex = null)
	{
		List<string> vals = new List<string>();
		ReactionWindow win = (enemyIndex.HasValue ? WindowFromPending(state) : null);
		if (def.Effects != null)
		{
			for (int i = 0; i < def.Effects.Count; i++)
			{
				DeclarativeEffect e = def.Effects[i];
				if ((e.Effect != "dealDamage" && e.Effect != "counter") || !e.Amount.HasValue)
				{
					continue;
				}
				EffectCondition c = e.Condition;
				if (win != null && c != null)
				{
					if ((c.MinActionValue.HasValue && win.Actual < c.MinActionValue.Value) || (c.MaxActionValue.HasValue && win.Actual > c.MaxActionValue.Value) || (c.LastActionNoHpLoss == true && (win.Stage != "post" || win.HpLoss > 0)) || (c.MinDamageTaken.HasValue && (win.Stage != "post" || win.HpLoss < c.MinDamageTaken.Value)))
					{
						continue;
					}
					if (c.PerfectBlockThisPhase == true)
					{
						vals.Add(((e.Effect == "counter") ? "返し" : "ダメージ") + PlayerDamageAfterModifiers(state, e.Amount.Value) + "（この敵フェーズを完全に凌いだら）");
						continue;
					}
				}
				int live = PlayerDamageAfterModifiers(state, e.Amount.Value);
				DamageBreakdown bd = (enemyIndex.HasValue ? DamageBreakdownOf(state, enemyIndex.Value, e.Amount.Value, e.Pierce == true, applyExpose: true, withMomentum: false) : null);
				if (bd != null)
				{
					vals.Add(((e.Effect == "counter") ? "返し" : "ダメージ") + live + " → HP-" + bd.HpLoss);
				}
				else if (live != e.Amount.Value)
				{
					vals.Add(((e.Effect == "counter") ? "返し" : "ダメージ") + live);
				}
			}
		}
		if (vals.Count == 0)
		{
			return null;
		}
		int weak = ((state.Phase == "player-turn") ? state.Player.Weak : (state.Player.Weak - state.Player.WeakFreshThisPhase.GetValueOrDefault()));
		List<string> parts = new List<string>();
		if (state.Player.Growth > 0)
		{
			parts.Add("成長+" + state.Player.Growth);
		}
		if (weak > 0)
		{
			parts.Add("弱体-25%");
		}
		if (enemyIndex.HasValue)
		{
			parts.Add("急所・装甲・ブロック込み");
		}
		string note = ((!enemyIndex.HasValue && state.Phase == "player-turn" && weak > 0) ? "※鳴る時の弱体で変わる" : "");
		return "実際の値: " + string.Join("・", vals.ToArray()) + "（" + string.Join("・", parts.ToArray()) + "）" + note;
	}

	public static DamageBreakdown? DamageBreakdownOf(GameState state, int enemyIndex, int baseAmount, bool pierce = false, bool applyExpose = true, bool withMomentum = true)
	{
		EnemyState enemy = EnemyAt(state, enemyIndex);
		if (enemy == null || enemy.Hp <= 0)
		{
			return null;
		}
		PlayerState p = state.Player;
		List<DamageBreakdownStep> steps = new List<DamageBreakdownStep>
		{
			new DamageBreakdownStep
			{
				Label = "基礎",
				Value = baseAmount
			}
		};
		int amount = baseAmount;
		if (p.Growth > 0)
		{
			amount += p.Growth;
			steps.Add(new DamageBreakdownStep
			{
				Label = $"成長+{p.Growth}",
				Value = amount
			});
		}
		if (withMomentum && p.Momentum > 0)
		{
			amount += p.Momentum;
			steps.Add(new DamageBreakdownStep
			{
				Label = $"勢い+{p.Momentum}",
				Value = amount
			});
		}
		if (p.Weak > 0 && amount > 0)
		{
			int w = Math.Max(1, (int)Math.Floor((double)amount * 0.75));
			if (w != amount)
			{
				amount = w;
				steps.Add(new DamageBreakdownStep
				{
					Label = "弱体-25%",
					Value = amount
				});
			}
		}
		if (applyExpose && enemy.Exposed > 0)
		{
			amount = (int)Math.Floor((double)amount * 1.5);
			steps.Add(new DamageBreakdownStep
			{
				Label = "急所×1.5",
				Value = amount
			});
		}
		if (enemy.Armor.HasValue && amount > enemy.Armor.Value)
		{
			amount = enemy.Armor.Value;
			steps.Add(new DamageBreakdownStep
			{
				Label = $"装甲上限{enemy.Armor}",
				Value = amount
			});
		}
		bool shellUp = enemy.BurrowActive == true && enemy.Block > 0;
		int blocked = ((!pierce || shellUp) ? Math.Min(enemy.Block, amount) : 0);
		if (shellUp)
		{
			steps.Add(new DamageBreakdownStep
			{
				Label = $"潜伏の殻-{blocked}(HPには通らない)",
				Value = 0
			});
		}
		else if (blocked > 0)
		{
			steps.Add(new DamageBreakdownStep
			{
				Label = $"敵ブロック-{blocked}",
				Value = amount - blocked
			});
		}
		if ((!shellUp & pierce) && enemy.Block > 0)
		{
			steps.Add(new DamageBreakdownStep
			{
				Label = "貫通(ブロック無視)",
				Value = amount
			});
		}
		int hpLoss = ((!shellUp) ? (amount - blocked) : 0);
		if (Content.GetEnemyDef(enemy.EnemyId).Nemesis == true && IsIntangibleTurn(state) && hpLoss > 1)
		{
			hpLoss = 1;
			steps.Add(new DamageBreakdownStep
			{
				Label = "無形=1固定",
				Value = 1
			});
		}
		if (enemy.Slippery.GetValueOrDefault() > 0 && hpLoss > 0)
		{
			if (hpLoss > 1)
			{
				steps.Add(new DamageBreakdownStep
				{
					Label = $"朧=1(残り{enemy.Slippery}回)",
					Value = 1
				});
			}
			hpLoss = 1;
		}
		int? turnArmor = Content.GetEnemyDef(enemy.EnemyId).TurnArmor;
		if (turnArmor.HasValue)
		{
			int remaining = Math.Max(0, turnArmor.Value - enemy.DamageThisTurn.GetValueOrDefault());
			if (hpLoss > remaining)
			{
				hpLoss = remaining;
				steps.Add(new DamageBreakdownStep
				{
					Label = $"ターン装甲(残り{remaining})",
					Value = hpLoss
				});
			}
		}
		return new DamageBreakdown
		{
			Steps = steps,
			Blocked = blocked,
			HpLoss = hpLoss
		};
	}

	public static int ApplyEnemyWeak(int value, int? weak)
	{
		if (weak.GetValueOrDefault() <= 0)
		{
			return value;
		}
		return Math.Max(1, (int)Math.Floor((double)value * 0.75));
	}

	public static int ModifiedHit(GameState s, int enemyIndex, int actual)
	{
		int v = ApplyEnemyWeak(actual, ((enemyIndex >= 0 && enemyIndex < s.Enemies.Count) ? s.Enemies[enemyIndex] : null)?.Weak);
		if (SetBellActive(s))
		{
			v = Math.Max(1, v - s.SetDamageReduction.GetValueOrDefault());
		}
		if (s.Player.Vulnerable > 0)
		{
			v = (int)Math.Floor((double)v * 1.5);
		}
		if (s.Player.Slow.GetValueOrDefault() > 0 && s.Player.PlaysThisTurn.GetValueOrDefault() > 0)
		{
			v = (int)Math.Floor((double)v * (1.0 + 0.1 * (double)s.Player.PlaysThisTurn.GetValueOrDefault()));
		}
		return v;
	}

	private static bool IsStatusCardId(string id)
	{
		switch (id)
		{
		default:
			return id == "status_guilt";
		case "status_wound":
		case "status_scald":
		case "status_junk":
		case "status_brand":
			return true;
		}
	}

	public static int? CardStatusRoom(GameState state, string status)
	{
		PlayerState p = state.Player;
		switch (status)
		{
		case "scald":
			return 5 - p.ScaldsThisCombat.GetValueOrDefault();
		case "junk":
		{
			int existing2 = p.Hand.Count((CardInstance c) => c.Def.Id == Content.JUNK_DEF.Id) + p.DrawPile.Count((CardInstance c) => c.Def.Id == Content.JUNK_DEF.Id) + p.DiscardPile.Count((CardInstance c) => c.Def.Id == Content.JUNK_DEF.Id);
			return 4 - existing2;
		}
		case "wound":
		{
			int existing = p.Hand.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id) + p.DrawPile.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id) + p.DiscardPile.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id) + p.ExhaustPile.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id) + p.SetCards.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id);
			return 5 - existing;
		}
		default:
			return null;
		}
	}

	public static StatusInflict? DisplayedInflict(GameState state, StatusInflict? inflict)
	{
		if (inflict == null)
		{
			return null;
		}
		int? room = CardStatusRoom(state, inflict.Status);
		if (!room.HasValue)
		{
			return inflict;
		}
		if (room.Value <= 0)
		{
			return null;
		}
		if (room.Value >= inflict.Amount)
		{
			return inflict;
		}
		return inflict with
		{
			Amount = room.Value
		};
	}

	public static int DisplayedIntentValue(GameState s, int enemyIndex, string kind, int actual)
	{
		if (!(kind == "attack"))
		{
			return actual;
		}
		return ModifiedHit(s, enemyIndex, actual);
	}

	public static List<string> IntentModifierNotes(GameState s, int enemyIndex, string kind)
	{
		List<string> notes = new List<string>();
		if (kind != "attack")
		{
			return notes;
		}
		EnemyState obj = ((enemyIndex >= 0 && enemyIndex < s.Enemies.Count) ? s.Enemies[enemyIndex] : null);
		if ((object)obj != null && obj.ActionNegated == true)
		{
			notes.Add("打ち消し済み＝この行動は起きない");
		}
		if ((obj?.Weak).GetValueOrDefault() > 0)
		{
			notes.Add("威圧で-25%");
		}
		if (SetBellActive(s))
		{
			notes.Add("鈴で-" + s.SetDamageReduction);
		}
		if (s.Player.Vulnerable > 0)
		{
			notes.Add("脆弱で+50%");
		}
		if (s.Player.Slow.GetValueOrDefault() > 0 && s.Player.PlaysThisTurn.GetValueOrDefault() > 0)
		{
			notes.Add("重りで+" + 10 * s.Player.PlaysThisTurn.GetValueOrDefault() + "%");
		}
		return notes;
	}

	public static int IntentHits(GameState s, bool? mirrorHits, int? hits)
	{
		return AttackHitsOf(s, mirrorHits, hits);
	}

	public static int IncomingFrom(GameState s, int enemyIndex)
	{
		EnemyState e = s.Enemies[enemyIndex];
		if (e.Hp <= 0 || e.Confusion > 0)
		{
			return 0;
		}
		if (e.ActionNegated == true)
		{
			return 0;
		}
		EnemyIntent it = EffectiveIntent(s, enemyIndex);
		if (it == null || it.Kind != "attack")
		{
			return 0;
		}
		return ModifiedHit(s, enemyIndex, it.Actual) * IntentHits(s, it.MirrorHits, it.Hits);
	}

	public static int IncomingTotal(GameState s)
	{
		int num = ((s.EnemyPhase == true && s.PendingWindow != null) ? s.PendingWindow.EnemyIndex : 0);
		bool nullify = s.NullifyNextAttack == true;
		List<int> burnout = BurnoutEnemies(s);
		int sum = 0;
		for (int i = num; i < s.Enemies.Count; i++)
		{
			if (!burnout.Contains(i))
			{
				int v = IncomingFrom(s, i);
				if ((v > 0) & nullify)
				{
					nullify = false;
				}
				else
				{
					sum += v;
				}
			}
		}
		return sum;
	}

	public static int ExpiryBlockPreview(GameState s)
	{
		if (s.EnemyPhase == true || s.Phase != "player-turn")
		{
			return 0;
		}
		int per = 0;
		bool keep = false;
		foreach (CardInstance permanent in s.Player.Permanents)
		{
			foreach (DeclarativeEffect e in permanent.Def.Effects)
			{
				if (e.Trigger == "onImpulseExpired" && e.Effect == "gainBlock")
				{
					per += e.Amount.GetValueOrDefault();
				}
				if (e.Effect == "keepImpulseOne")
				{
					keep = true;
				}
			}
		}
		if (per <= 0)
		{
			return 0;
		}
		int inHand = 0;
		foreach (CardInstance c in s.Player.Hand)
		{
			if (s.Player.ImpulseUids.Contains(c.Uid))
			{
				inHand++;
			}
		}
		return per * Math.Max(0, inHand - (keep ? 1 : 0));
	}

	public static bool LightMatters(GameState s)
	{
		PlayerState p = s.Player;
		foreach (CardInstance x in p.Permanents)
		{
			if (x.Innate == true && x.Def.Effects.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "addLight"))
			{
				return true;
			}
		}
		foreach (CardInstance c in p.Hand.Concat(p.DrawPile).Concat(p.DiscardPile).Concat(p.ExhaustPile)
			.Concat(p.Permanents)
			.Concat(p.SetCards))
		{
			if (c.Def.LightCost.GetValueOrDefault() > 0)
			{
				return true;
			}
			foreach (DeclarativeEffect e in c.Def.Effects)
			{
				if (!LIGHT_EFFECTS.Contains(e.Effect))
				{
					EffectCondition? condition = e.Condition;
					if ((object)condition == null || !condition.MinLight.HasValue)
					{
						continue;
					}
				}
				return true;
			}
			if (c.Def.Modes == null)
			{
				continue;
			}
			foreach (CardMode mode in c.Def.Modes)
			{
				foreach (DeclarativeEffect e2 in mode.Effects)
				{
					if (!LIGHT_EFFECTS.Contains(e2.Effect))
					{
						EffectCondition? condition2 = e2.Condition;
						if ((object)condition2 == null || !condition2.MinLight.HasValue)
						{
							continue;
						}
					}
					return true;
				}
			}
		}
		return false;
	}

	public static List<int> BurnoutEnemies(GameState s)
	{
		List<int> list = new List<int>();
		if (s.EnemyPhase == true)
		{
			return list;
		}
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			EnemyState e = s.Enemies[i];
			if (e.Hp > 0 && e.Burn > 0 && e.Burn >= e.Hp)
			{
				list.Add(i);
			}
		}
		return list;
	}

	public static string PermanentTargetNote(DeclarativeEffect e, string holderType)
	{
		if (holderType != "permanent" || e.Target != null)
		{
			return "";
		}
		if (e.Trigger == null || !FIRST_ALIVE_TRIGGERS.Contains(e.Trigger))
		{
			return "";
		}
		if (e.Effect == null || !ENEMY_SINGLE_EFFECTS.Contains(e.Effect))
		{
			return "";
		}
		return "先頭の敵に";
	}

	public static bool IsIntangibleTurn(GameState state)
	{
		return state.Turn % 2 == 1;
	}

	public static GameState BreakBurrowIfCracked(GameState state, int enemyIndex)
	{
		EnemyState e = EnemyAt(state, enemyIndex);
		if (e == null || e.BurrowActive != true || e.Block > 0 || e.Hp <= 0)
		{
			return state;
		}
		IReadOnlyList<EnemyState> enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState x) => x with
		{
			BurrowActive = false,
			BiteNext = true
		});
		return Events.Emit(state with
		{
			Enemies = enemies
		}, new GameEvent_BurrowBroken
		{
			EnemyIndex = enemyIndex
		});
	}

	public static int EffectiveStrength(GameState state, int enemyIndex)
	{
		EnemyState e = state.Enemies[enemyIndex];
		EnemyDef def = Content.GetEnemyDef(e.EnemyId);
		bool anyOther = false;
		for (int j = 0; j < state.Enemies.Count; j++)
		{
			if (j != enemyIndex && state.Enemies[j].Hp > 0)
			{
				anyOther = true;
				break;
			}
		}
		int bond = ((def.BondStrength.HasValue & anyOther) ? def.BondStrength.Value : 0);
		return e.Strength + bond;
	}

	public static int AttackValue(GameState state, int enemyIndex, string kind, int actual, int? baseValue)
	{
		if (kind != "attack" || !baseValue.HasValue)
		{
			return actual;
		}
		return Math.Max(1, baseValue.Value + EffectiveStrength(state, enemyIndex));
	}

	public static GameState RefreshIntentValues(GameState state)
	{
		bool changed = false;
		List<EnemyState> enemies = new List<EnemyState>(state.Enemies.Count);
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			EnemyState e = state.Enemies[i];
			EnemyIntent it = e.Intent;
			if (it == null || e.Hp <= 0)
			{
				enemies.Add(e);
				continue;
			}
			int actual = AttackValue(state, i, it.Kind, it.Actual, it.Base);
			EnemyIntentBranch alt = it.Alt;
			if (alt != null)
			{
				alt = alt with
				{
					Actual = AttackValue(state, i, alt.Kind, alt.Actual, alt.Base)
				};
			}
			if (actual == it.Actual && (alt == null || alt.Actual == it.Alt.Actual))
			{
				enemies.Add(e);
				continue;
			}
			changed = true;
			enemies.Add(e with
			{
				Intent = it with
				{
					Actual = actual,
					Alt = alt
				}
			});
		}
		if (!changed)
		{
			return state;
		}
		return state with
		{
			Enemies = enemies
		};
	}

	public static GameState GainEnemyStrength(GameState state, int enemyIndex, int amount)
	{
		if (amount == 0)
		{
			return state;
		}
		IReadOnlyList<EnemyState> enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState e) => e with
		{
			Strength = e.Strength + amount
		});
		return RefreshIntentValues(state with
		{
			Enemies = enemies
		});
	}

	public static GameState ApplyInterrupts(GameState state, int enemyIndex, IReadOnlyList<string> only = null)
	{
		EnemyState e = EnemyAt(state, enemyIndex);
		if (e == null || e.Hp <= 0)
		{
			return state;
		}
		if (e.InterruptBlocked == true)
		{
			return state;
		}
		bool inPlayerTurn = state.Phase == "player-turn" && state.EnemyPhase != true;
		EnemyGraph.InterruptResult r = EnemyGraph.ApplyInterruptsTo(state, enemyIndex, e.Node, e.FiredInterrupts, only, inPlayerTurn ? e.IntentNode : null);
		if (r.FiredNow.Count == 0)
		{
			return state;
		}
		string trigger = Content.GetEnemyDef(e.EnemyId).Interrupts[r.FiredNow[r.FiredNow.Count - 1]].On;
		bool replace = inPlayerTurn && e.Intent != null;
		IReadOnlyList<EnemyState> enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState x) =>
		{
			EnemyState enemyState = x with
			{
				Node = r.Cursor,
				FiredInterrupts = r.Fired
			};
			if (!replace || x.IntentMoveId == null)
			{
				return enemyState;
			}
			Dictionary<string, int> dictionary = ((x.MoveUses != null) ? new Dictionary<string, int>((IDictionary<string, int>)x.MoveUses) : new Dictionary<string, int>());
			int value = 0;
			dictionary.TryGetValue(x.IntentMoveId, out value);
			dictionary[x.IntentMoveId] = Math.Max(0, value - 1);
			List<string> list = ((x.LastMoves != null) ? new List<string>(x.LastMoves) : new List<string>());
			if (list.Count > 0 && list[0] == x.IntentMoveId)
			{
				list.RemoveAt(0);
			}
			return enemyState with
			{
				MoveUses = dictionary,
				LastMoves = list,
				IntentMoveId = null,
				IntentNode = null
			};
		});
		GameState s = state with
		{
			Enemies = enemies
		};
		if (!replace)
		{
			return Events.Emit(s, new GameEvent_EnemyInterrupted
			{
				EnemyIndex = enemyIndex,
				Trigger = trigger,
				Replaced = false
			});
		}
		EnemyIntent before = e.Intent;
		s = Combat.DeclareOne(s, enemyIndex);
		return Events.Emit(s, new GameEvent_EnemyInterrupted
		{
			EnemyIndex = enemyIndex,
			Trigger = trigger,
			Replaced = true,
			Before = before,
			After = s.Enemies[enemyIndex].Intent
		});
	}

	public static GameState ApplyDamageInterrupts(GameState state, int enemyIndex)
	{
		return ApplyInterrupts(state, enemyIndex, new string[2] { "hpBelowHalf", "damageTaken" });
	}

	public static GameState ApplyDeathInterrupts(GameState state)
	{
		bool anyDead = false;
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			if (state.Enemies[i].Hp <= 0 && state.Enemies[i].Fled != true)
			{
				anyDead = true;
				break;
			}
		}
		if (!anyDead)
		{
			return state;
		}
		GameState s = state;
		for (int j = 0; j < s.Enemies.Count; j++)
		{
			s = ApplyInterrupts(s, j, new string[2] { "allyDied", "alone" });
		}
		return s;
	}

	public static (GameState State, bool Blocked) TryArtifactBlock(GameState state, int enemyIndex, string effectName)
	{
		EnemyState e = EnemyAt(state, enemyIndex);
		if (e == null || e.Hp <= 0 || e.Artifact.GetValueOrDefault() <= 0)
		{
			return (State: state, Blocked: false);
		}
		IReadOnlyList<EnemyState> enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState x) => x with
		{
			Artifact = x.Artifact.GetValueOrDefault() - 1
		});
		return (State: Events.Emit(state with
		{
			Enemies = enemies
		}, new GameEvent_ArtifactBlocked
		{
			EnemyIndex = enemyIndex,
			Effect = effectName
		}), Blocked: true);
	}

	public static GameState DealDamageToEnemy(GameState state, int enemyIndex, int baseAmount, bool pierce = false, bool applyExpose = true)
	{
		int amount = PlayerDamageAfterModifiers(state, baseAmount);
		EnemyState enemy = EnemyAt(state, enemyIndex);
		if (enemy == null || enemy.Hp <= 0)
		{
			return state;
		}
		bool flag = IsBlazing(state);
		bool exposed = applyExpose && enemy.Exposed > 0;
		if (exposed)
		{
			amount = (int)Math.Floor((double)amount * 1.5);
		}
		int armorCut = ((enemy.Armor.HasValue && amount > enemy.Armor.Value) ? (amount - enemy.Armor.Value) : 0);
		if (armorCut > 0)
		{
			amount = enemy.Armor.Value;
		}
		bool shellUp = enemy.BurrowActive == true && enemy.Block > 0;
		int blocked = ((!pierce || shellUp) ? Math.Min(enemy.Block, amount) : 0);
		bool flag2 = shellUp;
		int hpLoss = ((!flag2) ? (amount - blocked) : 0);
		int burrowCut = (flag2 ? (amount - blocked) : 0);
		int nemesisCut = 0;
		if (Content.GetEnemyDef(enemy.EnemyId).Nemesis == true && IsIntangibleTurn(state) && hpLoss > 1)
		{
			nemesisCut = hpLoss - 1;
			hpLoss = 1;
		}
		bool slipped = enemy.Slippery.GetValueOrDefault() > 0 && hpLoss > 0;
		int slipperyCut = (slipped ? (hpLoss - 1) : 0);
		if (slipped)
		{
			hpLoss = 1;
		}
		int? turnArmor = Content.GetEnemyDef(enemy.EnemyId).TurnArmor;
		int turnArmorCut = 0;
		if (turnArmor.HasValue)
		{
			int remaining = Math.Max(0, turnArmor.Value - enemy.DamageThisTurn.GetValueOrDefault());
			if (hpLoss > remaining)
			{
				turnArmorCut = hpLoss - remaining;
				hpLoss = remaining;
			}
		}
		int blockedF = blocked;
		int hpLossF = hpLoss;
		bool exposedF = exposed;
		IReadOnlyList<EnemyState> enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState e) => e with
		{
			Block = e.Block - blockedF,
			Hp = e.Hp - hpLossF,
			Exposed = (exposedF ? (e.Exposed - 1) : e.Exposed),
			Slippery = (slipped ? new int?(e.Slippery.GetValueOrDefault() - 1) : e.Slippery),
			HpLostSinceRegen = e.HpLostSinceRegen.GetValueOrDefault() + hpLossF,
			DamageTakenTotal = e.DamageTakenTotal.GetValueOrDefault() + hpLossF,
			DamageThisTurn = e.DamageThisTurn.GetValueOrDefault() + hpLossF
		});
		GameState s = Events.Emit(state with
		{
			Enemies = enemies
		}, new GameEvent_DamageDealt
		{
			Source = "player",
			PresentationReceipt = Events.EnemyPresentationReceipt(enemyIndex, enemy, enemies[enemyIndex]),
			Amount = amount,
			HpLoss = hpLoss,
			EnemyIndex = enemyIndex,
			ArmorCut = ((armorCut > 0) ? new int?(armorCut) : ((int?)null)),
			TurnArmorCut = ((turnArmorCut > 0) ? new int?(turnArmorCut) : ((int?)null)),
			BurrowCut = ((burrowCut > 0) ? new int?(burrowCut) : ((int?)null)),
			NemesisCut = ((nemesisCut > 0) ? new int?(nemesisCut) : ((int?)null)),
			SlipperyCut = ((slipperyCut > 0) ? new int?(slipperyCut) : ((int?)null)),
			Exposed = (exposed ? new bool?(true) : ((bool?)null)),
			Pierced = ((pierce && !shellUp && enemy.Block > 0) ? new bool?(true) : ((bool?)null)),
			Blocked = ((blocked > 0) ? new int?(blocked) : ((int?)null)),
			SourceUid = state.ResolvingPermanentUid
		});
		s = ApplyDamageInterrupts(s, enemyIndex);
		s = BreakBurrowIfCracked(s, enemyIndex);
		if (hpLoss > 0 && s.Enemies[enemyIndex].Hp <= 0)
		{
			s = FireEnemyDied(s, enemyIndex);
		}
		EnemyState struck0 = s.Enemies[enemyIndex];
		EnemyDef defE = Content.GetEnemyDef(struck0.EnemyId);
		if (defE.EnrageEveryDamage.HasValue && struck0.Hp > 0 && hpLoss > 0)
		{
			int before = struck0.DamageTakenTotal.GetValueOrDefault() - hpLoss;
			int gain = ((int)Math.Floor((double)struck0.DamageTakenTotal.GetValueOrDefault() / (double)defE.EnrageEveryDamage.Value) - (int)Math.Floor((double)before / (double)defE.EnrageEveryDamage.Value)) * (defE.Enrage ?? 2);
			if (gain > 0)
			{
				s = GainEnemyStrength(s, enemyIndex, gain);
				s = Events.Emit(s, new GameEvent_StrengthGained
				{
					EnemyIndex = enemyIndex,
					Amount = gain,
					Reason = "enrage-damage"
				});
			}
		}
		EnemyState struck1 = s.Enemies[enemyIndex];
		if (struck1.Thorns.GetValueOrDefault() > 0 && struck1.Hp > 0 && s.ResolvingCardPlay == true)
		{
			PlayerState beforeReflect = s.Player;
			int reflect = struck1.Thorns.Value;
			int pBlocked = Math.Min(s.Player.Block, reflect);
			int pIceBlocked = Math.Min(s.Player.IceBlock, reflect - pBlocked);
			int pHpLoss = reflect - pBlocked - pIceBlocked;
			s = s with
			{
				Player = s.Player with
				{
					Block = s.Player.Block - pBlocked,
					IceBlock = s.Player.IceBlock - pIceBlocked,
					Hp = s.Player.Hp - pHpLoss
				}
			};
			s = Events.Emit(s, new GameEvent_ThornsReflected
			{
				EnemyIndex = enemyIndex,
				Amount = reflect,
				HpLoss = pHpLoss,
				PresentationReceipt = Events.PlayerPresentationReceipt(beforeReflect, s.Player)
			});
		}
		if (flag && !IsBlazing(s))
		{
			s = Events.Emit(s, new GameEvent_BlazeExtinguished());
		}
		return s;
	}

	public static GameState DrawCards(GameState state, int n)
	{
		List<CardInstance> drawPile = new List<CardInstance>(state.Player.DrawPile);
		List<CardInstance> discardPile = new List<CardInstance>(state.Player.DiscardPile);
		RngState rng = state.Rng;
		List<CardInstance> drawn = new List<CardInstance>();
		int shuffles = 0;
		for (int i = 0; i < n; i++)
		{
			if (drawPile.Count == 0)
			{
				if (discardPile.Count == 0)
				{
					break;
				}
				(CardInstance[] Items, RngState Next) tuple = Rng.Shuffle(rng, discardPile);
				CardInstance[] reshuffled = tuple.Items;
				RngState item = tuple.Next;
				drawPile = new List<CardInstance>(reshuffled);
				discardPile = new List<CardInstance>();
				rng = item;
				shuffles++;
			}
			drawn.Add(drawPile[0]);
			drawPile.RemoveAt(0);
		}
		GameState next = state with
		{
			Rng = rng,
			Player = state.Player with
			{
				DrawPile = drawPile,
				DiscardPile = discardPile,
				Hand = state.Player.Hand.Concat(drawn).ToList()
			}
		};
		if (drawn.Count > 0)
		{
			next = Events.Emit(next, new GameEvent_CardsDrawn
			{
				Count = drawn.Count,
				Cards = drawn.Select((CardInstance c) => c.Def.Name).ToList()
			});
		}
		for (int k = 0; k < shuffles; k++)
		{
			next = Events.Emit(next, new GameEvent_DeckShuffled());
			next = RunPermanentTriggers(next, "onShuffle", Math.Max(0, FindAliveIndex(next.Enemies)));
		}
		return next;
	}

	public static GameState FireEnemyDied(GameState state, int enemyIndex)
	{
		return RunPermanentTriggers(Events.Emit(state, new GameEvent_EnemyDied
		{
			EnemyIndex = enemyIndex
		}), "onEnemyDied", enemyIndex);
	}

	public static GameState ResolveEffect(GameState state, DeclarativeEffect effect, int enemyIndex)
	{
		if (effect.ExhaustThreshold.HasValue && state.Player.ExhaustPile.Count >= effect.ExhaustThreshold.Value)
		{
			effect = effect with
			{
				Amount = effect.AmountMax
			};
		}
		switch (effect.Effect)
		{
		case "dealDamage":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() + (effect.GrowthMultiplier.HasValue ? ((int)((double)state.Player.Growth * (effect.GrowthMultiplier.Value - 1.0))) : 0) + (effect.MomentumMultiplier.HasValue ? ((int)((double)state.Player.Momentum * (effect.MomentumMultiplier.Value - 1.0))) : 0), effect.Pierce == true);
		case "dealDamagePerAttackPlayed":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.AttacksPlayedThisTurn.GetValueOrDefault(), effect.Pierce == true);
		case "dealDamagePerHeal":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.HealsThisCombat, effect.Pierce == true);
		case "dealDamagePerMomentum":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.Momentum, effect.Pierce == true);
		case "dealDamagePerEnergyMax":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.EnergyMaxAtTurnStart, effect.Pierce == true);
		case "counter":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault(), effect.Pierce == true);
		case "gainEnergy":
		{
			int amount2 = effect.Amount.GetValueOrDefault();
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Energy = state.Player.Energy + amount2
				}
			}, new GameEvent_EnergyGained
			{
				Amount = amount2
			});
		}
		case "addMomentum":
		{
			int amount5 = effect.Amount.GetValueOrDefault();
			GameState emitted2 = Events.Emit(state with
			{
				Player = state.Player with
				{
					Momentum = state.Player.Momentum + amount5
				}
			}, new GameEvent_MomentumAdded
			{
				Amount = amount5
			});
			if (amount5 <= 0)
			{
				return emitted2;
			}
			return FireGainTrigger(emitted2, "onMomentumGained", enemyIndex);
		}
		case "gainBlock":
		{
			int amount19 = effect.Amount.GetValueOrDefault();
			return GainPlayerBlock(state, amount19, enemyIndex);
		}
		case "gainIceBlock":
			return GainPlayerIceBlock(state, effect.Amount.GetValueOrDefault());
		case "dealDamagePerCardPlayedTotal":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.CardsPlayedTotal, effect.Pierce == true);
		case "dealDamagePerCardPlayed":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.CardsPlayedThisTurn, effect.Pierce == true);
		case "gainIceBlockPerCardPlayed":
			return GainPlayerIceBlock(state, effect.Amount.GetValueOrDefault() * state.Player.CardsPlayedThisTurn);
		case "drawCardsPerCardPlayed":
			return DrawCards(state, effect.Amount.GetValueOrDefault() * state.Player.CardsPlayedThisTurn);
		case "dealDamagePerHandCard":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.Hand.Count, effect.Pierce == true);
		case "gainIceBlockPerHandCard":
			return GainPlayerIceBlock(state, effect.Amount.GetValueOrDefault() * state.Player.Hand.Count);
		case "gainBlockPerHandCard":
			return GainPlayerBlock(state, effect.Amount.GetValueOrDefault() * state.Player.Hand.Count, enemyIndex);
		case "staggerEnemy":
			return state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => (enemyState.Hp <= 0) ? enemyState : enemyState with
				{
					StaggeredNext = true
				})
			};
		case "drawCardsNextTurn":
			return state with
			{
				NextTurnDraw = state.NextTurnDraw.GetValueOrDefault() + effect.Amount.GetValueOrDefault()
			};
		case "gainHpRatio":
			return HealPlayer(state, Math.Max(1, (int)Math.Floor((double)(state.Player.MaxHp * effect.Amount.GetValueOrDefault()) / 100.0)), enemyIndex);
		case "negateEnemyAction":
			return state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => (enemyState.Hp <= 0) ? enemyState : enemyState with
				{
					ActionNegated = true
				})
			};
		case "blockEnemySummon":
			return state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => (enemyState.Hp <= 0) ? enemyState : enemyState with
				{
					SummonBlocked = true
				})
			};
		case "blockEnemyInterrupt":
			return state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => (enemyState.Hp <= 0) ? enemyState : enemyState with
				{
					InterruptBlocked = true
				})
			};
		case "cleanseStatuses":
		{
			PlayerState p3 = state.Player;
			if (p3.Weak == 0 && p3.Vulnerable == 0 && p3.Frail == 0 && p3.Restrain == 0 && p3.Mist.GetValueOrDefault() == 0 && p3.Slow.GetValueOrDefault() == 0)
			{
				return state;
			}
			return state with
			{
				Player = p3 with
				{
					Weak = 0,
					Vulnerable = 0,
					Frail = 0,
					Restrain = 0,
					Mist = 0,
					Slow = 0
				}
			};
		}
		case "purgeHandStatus":
		{
			List<CardInstance> purged = new List<CardInstance>();
			List<CardInstance> kept = new List<CardInstance>();
			foreach (CardInstance c in state.Player.Hand)
			{
				if (IsStatusCardId(c.Def.Id))
				{
					purged.Add(c);
				}
				else
				{
					kept.Add(c);
				}
			}
			if (purged.Count == 0)
			{
				return state;
			}
			List<CardInstance> ex = new List<CardInstance>(state.Player.ExhaustPile);
			ex.AddRange(purged);
			return FireExhaustTriggers(state with
			{
				Player = state.Player with
				{
					Hand = kept,
					ExhaustPile = ex
				}
			}, purged.Count, enemyIndex);
		}
		case "gainArtifact":
			return state with
			{
				Player = state.Player with
				{
					Artifact = state.Player.Artifact.GetValueOrDefault() + effect.Amount.GetValueOrDefault()
				}
			};
		case "redrawHand":
		{
			int n7 = state.Player.Hand.Count;
			if (n7 == 0)
			{
				return state;
			}
			List<CardInstance> disc = new List<CardInstance>(state.Player.DiscardPile);
			disc.AddRange(state.Player.Hand);
			return DrawCards(state with
			{
				Player = state.Player with
				{
					Hand = new List<CardInstance>(),
					DiscardPile = disc,
					ImpulseUids = new List<string>()
				}
			}, n7);
		}
		case "clearEnemyStrength":
			return RefreshIntentValues(state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => (enemyState.Hp <= 0 || enemyState.Strength <= 0) ? enemyState : enemyState with
				{
					Strength = 0
				})
			});
		case "retainHandOnce":
			return state with
			{
				RetainHandThisTurn = true
			};
		case "energyCarryOnce":
			return state with
			{
				EnergyCarryThisTurn = true
			};
		case "gainDeathSaveOne":
			return state with
			{
				GearDeathSave = true
			};
		case "stripRider":
			return state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => (enemyState.Hp <= 0 || !(enemyState.Intent != null)) ? enemyState : enemyState with
				{
					Intent = enemyState.Intent with
					{
						Inflict = null,
						AlsoBuff = null,
						AlsoDefend = null,
						AlsoDestroySet = null,
						StrengthPerMilled = null
					}
				})
			};
		case "singleHit":
			return state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => (enemyState.Hp <= 0 || !(enemyState.Intent != null)) ? enemyState : enemyState with
				{
					Intent = enemyState.Intent with
					{
						Hits = null,
						MirrorHits = null
					}
				})
			};
		case "nullifyNextEnemyAttack":
			return state with
			{
				NullifyNextAttack = true
			};
		case "transformInHand":
		case "copyCardInHand":
			return state;
		case "gainEnergyNextTurn":
			return state with
			{
				NextTurnEnergy = state.NextTurnEnergy.GetValueOrDefault() + effect.Amount.GetValueOrDefault()
			};
		case "addLightNextTurn":
			return state with
			{
				NextTurnLight = state.NextTurnLight.GetValueOrDefault() + effect.Amount.GetValueOrDefault()
			};
		case "gainBlockNextTurn":
			return state with
			{
				NextTurnBlock = state.NextTurnBlock.GetValueOrDefault() + effect.Amount.GetValueOrDefault()
			};
		case "blessRetainers":
			return state;
		case "addCasts":
			return state with
			{
				Player = state.Player with
				{
					CardsPlayedThisTurn = state.Player.CardsPlayedThisTurn + effect.Amount.GetValueOrDefault()
				}
			};
		case "addSpellEcho":
			return state with
			{
				Player = state.Player with
				{
					SpellEchoes = state.Player.SpellEchoes + effect.Amount.GetValueOrDefault()
				}
			};
		case "addAether":
		{
			int amount6 = effect.Amount.GetValueOrDefault();
			return RunPermanentTriggers(Events.Emit(state with
			{
				Player = state.Player with
				{
					Aether = state.Player.Aether + amount6
				}
			}, new GameEvent_AetherGained
			{
				Amount = amount6
			}), "onAetherGained", enemyIndex);
		}
		case "addLight":
			return GainLight(state, effect.Amount.GetValueOrDefault(), (state.ResolvingPermanentUid != null && state.ResolvingPermanentUid.StartsWith("leader_", StringComparison.Ordinal)) ? "passive" : "card", enemyIndex);
		case "doubleLight":
		{
			int cur = state.Player.Light.GetValueOrDefault();
			if (cur <= 0)
			{
				return state;
			}
			return GainLight(state, cur, "card", enemyIndex);
		}
		case "applyBurn":
		{
			int amount18 = effect.Amount.GetValueOrDefault();
			EnemyState enemy12 = EnemyAt(state, enemyIndex);
			if (enemy12 == null || enemy12.Hp <= 0)
			{
				return state;
			}
			bool flag2 = IsBlazing(state);
			IReadOnlyList<EnemyState> enemies5 = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
			{
				Burn = enemyState.Burn + amount18
			});
			GameState s20 = Events.Emit(state with
			{
				Enemies = enemies5
			}, new GameEvent_BurnApplied
			{
				EnemyIndex = enemyIndex,
				Amount = amount18
			});
			if (!flag2 && IsBlazing(s20))
			{
				s20 = Events.Emit(s20, new GameEvent_BlazeIgnited());
				if (s20.Phase == "player-turn")
				{
					foreach (CardInstance permanent in s20.Player.Permanents)
					{
						foreach (DeclarativeEffect pe in permanent.Def.Effects)
						{
							if (pe.Trigger == "onTurnStart")
							{
								EffectCondition? condition = pe.Condition;
								if ((object)condition != null && condition.Blaze == true)
								{
									s20 = ResolveEffectTargeted(s20, pe, enemyIndex, new DamagePresentationSource
									{
										Kind = "permanent",
										DefinitionId = permanent.Def.Id,
										Uid = permanent.Uid
									});
								}
							}
						}
					}
					s20 = RunPermanentTriggers(s20, "onBlazeIgnite", enemyIndex);
				}
			}
			return s20;
		}
		case "confuse":
		{
			int amount17 = effect.Amount.GetValueOrDefault();
			EnemyState enemy11 = EnemyAt(state, enemyIndex);
			if (enemy11 == null || enemy11.Hp <= 0)
			{
				return state;
			}
			(GameState State, bool Blocked) tuple7 = TryArtifactBlock(state, enemyIndex, "confuse");
			var (sa2, _) = tuple7;
			if (tuple7.Blocked)
			{
				return sa2;
			}
			IReadOnlyList<EnemyState> enemies4 = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
			{
				Confusion = enemyState.Confusion + amount17
			});
			return Events.Emit(state with
			{
				Enemies = enemies4
			}, new GameEvent_EnemyConfused
			{
				EnemyIndex = enemyIndex,
				Amount = amount17
			});
		}
		case "gainHp":
			return HealPlayer(state, effect.Amount.GetValueOrDefault(), enemyIndex);
		case "strengthenEnemy":
		{
			int amount16 = effect.Amount.GetValueOrDefault();
			EnemyState enemy8 = EnemyAt(state, enemyIndex);
			if (enemy8 == null || enemy8.Hp <= 0 || amount16 == 0)
			{
				return state;
			}
			return Events.Emit(GainEnemyStrength(state, enemyIndex, amount16), new GameEvent_StrengthGained
			{
				EnemyIndex = enemyIndex,
				Amount = amount16
			});
		}
		case "weakenEnemy":
		{
			int amount15 = effect.Amount.GetValueOrDefault();
			EnemyState enemy7 = EnemyAt(state, enemyIndex);
			if (enemy7 == null || enemy7.Hp <= 0)
			{
				return state;
			}
			(GameState State, bool Blocked) tuple5 = TryArtifactBlock(state, enemyIndex, "weakenEnemy");
			var (sa, _) = tuple5;
			if (tuple5.Blocked)
			{
				return sa;
			}
			IReadOnlyList<EnemyState> enemies3 = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
			{
				Weak = enemyState.Weak.GetValueOrDefault() + amount15
			});
			return Events.Emit(state with
			{
				Enemies = enemies3
			}, new GameEvent_EnemyWeakened
			{
				EnemyIndex = enemyIndex,
				Amount = amount15
			});
		}
		case "dealDamagePerBlock":
		{
			GameState dealt3 = DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.Block, effect.Pierce == true, applyExpose: false);
			if (effect.SpendBlock != true)
			{
				return dealt3;
			}
			return dealt3 with
			{
				Player = dealt3.Player with
				{
					Block = 0
				}
			};
		}
		case "dealDamagePerPermanent":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * CountedPermanents(state), effect.Pierce == true);
		case "dealDamagePerLight":
		{
			int lit = (int)Math.Floor((double)state.Player.Light.GetValueOrDefault() / 2.0) * (effect.Amount ?? 1);
			if (lit > 0)
			{
				return DealDamageToEnemy(state, enemyIndex, lit, effect.Pierce == true);
			}
			return state;
		}
		case "dealDamageDrain":
		{
			int amount14 = effect.Amount.GetValueOrDefault();
			int dealt2 = PlayerDamageAfterModifiers(state, amount14);
			return HealPlayer(DealDamageToEnemy(state, enemyIndex, amount14, effect.Pierce == true), (int)Math.Floor((double)dealt2 / 2.0), enemyIndex);
		}
		case "dealDamageDrainPerExhaust":
		{
			int amount13 = effect.Amount.GetValueOrDefault() * state.Player.ExhaustPile.Count;
			int dealt = PlayerDamageAfterModifiers(state, amount13);
			return HealPlayer(DealDamageToEnemy(state, enemyIndex, amount13, effect.Pierce == true), (int)Math.Floor((double)dealt / 2.0), enemyIndex);
		}
		case "dealDamagePerSelfHpLost":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.SelfHpLost, effect.Pierce == true);
		case "retrieveFromExhaust":
		case "playFromExhaust":
			return state;
		case "duplicateRetainers":
		{
			List<CardInstance> snapshot = state.Player.Permanents.Where((CardInstance cardInstance) => cardInstance.Def.Retainer == true && cardInstance.Innate != true).ToList();
			GameState s9 = state;
			HashSet<string> batch = new HashSet<string>();
			foreach (CardInstance src4 in snapshot)
			{
				(GameState State, string Uid) tuple4 = NewSummonUid(s9, src4.Def.Id);
				GameState s10 = tuple4.State;
				string uid = tuple4.Uid;
				s9 = s10;
				CardInstance token = new CardInstance
				{
					Uid = uid,
					Def = src4.Def,
					Token = true,
					EnteredTurn = src4.EnteredTurn,
					LifeBonus = src4.LifeBonus,
					LifePersist = ((src4.LifePersist == true) ? new bool?(true) : ((bool?)null))
				};
				batch.Add(token.Uid);
				s9 = s9 with
				{
					Player = s9.Player with
					{
						Permanents = s9.Player.Permanents.Concat(new CardInstance[1] { token }).ToList()
					}
				};
				s9 = EnterPermanent(s9, token.Uid, enemyIndex, (CardInstance cardInstance) => cardInstance.Uid == token.Uid || !batch.Contains(cardInstance.Uid));
			}
			return Events.Emit(s9, new GameEvent_RetainersDuplicated
			{
				Count = snapshot.Count
			});
		}
		case "sacrificeRetainer":
			return state;
		case "activateEnteredRetainer":
			return state;
		case "copyRetainer":
		{
			CardInstance src3 = state.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == state.ChosenPermanentUid && IsDoll(cardInstance));
			if (src3 == null)
			{
				return state;
			}
			GameState s7 = state;
			for (int i6 = 0; i6 < (effect.Amount ?? 1); i6++)
			{
				s7 = CopyDoll(s7, src3, enemyIndex, noTwin: false);
			}
			return s7;
		}
		case "copyLastRetainer":
		{
			CardInstance src2 = null;
			foreach (CardInstance p2 in state.Player.Permanents)
			{
				if (IsDoll(p2))
				{
					src2 = p2;
				}
			}
			if (src2 == null)
			{
				return state;
			}
			GameState s5 = state;
			for (int i4 = 0; i4 < (effect.Amount ?? 1); i4++)
			{
				s5 = CopyDoll(s5, src2, enemyIndex, noTwin: false);
			}
			return s5;
		}
		case "twinNextRetainer":
			return state with
			{
				NextRetainerTwin = state.NextRetainerTwin.GetValueOrDefault() + (effect.Amount ?? 1)
			};
		case "extendRetainerLife":
		{
			CardInstance src = state.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == state.ChosenPermanentUid && IsDoll(cardInstance));
			if (src == null || !DollLifeTotal(src).HasValue)
			{
				return state;
			}
			int n3 = effect.Amount ?? 1;
			List<CardInstance> perms = new List<CardInstance>(state.Player.Permanents.Count);
			foreach (CardInstance p in state.Player.Permanents)
			{
				perms.Add((p.Uid == src.Uid) ? p with
				{
					LifeBonus = p.LifeBonus.GetValueOrDefault() + n3
				} : p);
			}
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Permanents = perms
				}
			}, new GameEvent_RetainerLifeExtended
			{
				CardId = src.Def.Id,
				Uid = src.Uid,
				Amount = n3
			});
		}
		case "extendAllRetainersLife":
		{
			int n12 = effect.Amount ?? 1;
			List<CardInstance> targets = state.Player.Permanents.Where((CardInstance p6) => IsDoll(p6) && DollLifeTotal(p6).HasValue).ToList();
			if (targets.Count == 0)
			{
				return state;
			}
			List<CardInstance> perms4 = new List<CardInstance>(state.Player.Permanents.Count);
			foreach (CardInstance p5 in state.Player.Permanents)
			{
				perms4.Add(targets.Any((CardInstance cardInstance) => cardInstance.Uid == p5.Uid) ? p5 with
				{
					LifeBonus = p5.LifeBonus.GetValueOrDefault() + n12
				} : p5);
			}
			GameState s22 = state with
			{
				Player = state.Player with
				{
					Permanents = perms4
				}
			};
			{
				foreach (CardInstance t in targets)
				{
					s22 = Events.Emit(s22, new GameEvent_RetainerLifeExtended
					{
						CardId = t.Def.Id,
						Uid = t.Uid,
						Amount = n12
					});
				}
				return s22;
			}
		}
		case "persistRetainer":
		{
			CardInstance src5 = state.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == state.ChosenPermanentUid && IsDoll(cardInstance));
			if (src5 == null || !DollLifeTotal(src5).HasValue)
			{
				return state;
			}
			List<CardInstance> perms3 = new List<CardInstance>(state.Player.Permanents.Count);
			foreach (CardInstance p4 in state.Player.Permanents)
			{
				perms3.Add((p4.Uid == src5.Uid) ? p4 with
				{
					LifePersist = true
				} : p4);
			}
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Permanents = perms3
				}
			}, new GameEvent_RetainerLifeExtended
			{
				CardId = src5.Def.Id,
				Uid = src5.Uid,
				Amount = 0,
				Persist = true
			});
		}
		case "triggerRetainersNow":
		{
			int n10 = state.Player.Permanents.Count((CardInstance cardInstance) => cardInstance.Def.Retainer == true && cardInstance.Innate != true);
			return Events.Emit(RallyRetainers(state, enemyIndex), new GameEvent_RetainersTriggered
			{
				Count = n10
			});
		}
		case "summonPermanent":
		{
			CardDef def4 = Content.GetCardDef(effect.SummonId ?? "");
			GameState s17 = state;
			for (int i10 = 0; i10 < (effect.Amount ?? 1); i10++)
			{
				(GameState State, string Uid) tuple10 = NewSummonUid(s17, def4.Id);
				GameState s18 = tuple10.State;
				string uid2 = tuple10.Uid;
				s17 = s18;
				CardInstance token2 = new CardInstance
				{
					Uid = uid2,
					Def = def4,
					Token = true
				};
				s17 = s17 with
				{
					Player = s17.Player with
					{
						Permanents = s17.Player.Permanents.Concat(new CardInstance[1] { token2 }).ToList()
					}
				};
				s17 = EnterPermanent(s17, token2.Uid, enemyIndex);
			}
			return s17;
		}
		case "dealDamagePerWeak":
		case "dealDamagePerNegStrength":
		{
			EnemyState enemy10 = EnemyAt(state, enemyIndex);
			if (enemy10 == null || enemy10.Hp <= 0 || enemy10.Weak.GetValueOrDefault() <= 0)
			{
				return state;
			}
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * enemy10.Weak.GetValueOrDefault(), effect.Pierce == true);
		}
		case "dischargeBurn":
		{
			EnemyState enemy9 = EnemyAt(state, enemyIndex);
			if (enemy9 == null || enemy9.Hp <= 0 || enemy9.Burn <= 0)
			{
				return state;
			}
			int burn2 = enemy9.Burn;
			bool flag = IsBlazing(state);
			GameState s15 = state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
				{
					Burn = 0
				})
			};
			s15 = Events.Emit(s15, new GameEvent_BurnDischarged
			{
				EnemyIndex = enemyIndex,
				Amount = burn2
			});
			if (flag && !IsBlazing(s15))
			{
				s15 = Events.Emit(s15, new GameEvent_BlazeExtinguished());
			}
			return DealDamageToEnemy(s15, enemyIndex, burn2 * effect.Amount.GetValueOrDefault(), effect.Pierce == true);
		}
		case "shatterBlockConvert":
		{
			EnemyState enemy6 = EnemyAt(state, enemyIndex);
			if (enemy6 == null || enemy6.Hp <= 0)
			{
				return state;
			}
			int shattered = enemy6.Block;
			GameState s12 = state with
			{
				Enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
				{
					Block = 0
				})
			};
			if (shattered <= 0)
			{
				return s12;
			}
			s12 = Events.Emit(s12, new GameEvent_BlockShattered
			{
				EnemyIndex = enemyIndex,
				Amount = shattered,
				PresentationReceipt = Events.EnemyPresentationReceipt(enemyIndex, enemy6, s12.Enemies[enemyIndex])
			});
			return DealDamageToEnemy(s12, enemyIndex, shattered, effect.Pierce == true);
		}
		case "dealDamageExecute":
		{
			EnemyState enemy5 = EnemyAt(state, enemyIndex);
			if (enemy5 == null || enemy5.Hp <= 0)
			{
				return state;
			}
			bool execute = enemy5.Hp <= (int)Math.Floor((double)enemy5.MaxHp * 0.25);
			return DealDamageToEnemy(state, enemyIndex, (!execute) ? effect.Amount.GetValueOrDefault() : (effect.AmountMax ?? effect.Amount.GetValueOrDefault()), effect.Pierce == true);
		}
		case "dealDamagePerRandomPlayed":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.RandomPlayedThisCombat, effect.Pierce == true);
		case "applyBurnPerDamageTaken":
		{
			int burn = effect.Amount.GetValueOrDefault() * state.Player.DamageTakenLastEnemyPhase;
			EnemyState target2 = EnemyAt(state, enemyIndex);
			if (burn <= 0 || target2 == null || target2.Hp <= 0)
			{
				return state;
			}
			IReadOnlyList<EnemyState> enemies = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
			{
				Burn = enemyState.Burn + burn
			});
			return Events.Emit(state with
			{
				Enemies = enemies
			}, new GameEvent_BurnApplied
			{
				EnemyIndex = enemyIndex,
				Amount = burn
			});
		}
		case "dealDamagePerDamageTaken":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.DamageTakenLastEnemyPhase, effect.Pierce == true);
		case "gainBlockPerPermanent":
		{
			int amount10 = effect.Amount.GetValueOrDefault() * CountedPermanents(state);
			return GainPlayerBlock(state, amount10, enemyIndex);
		}
		case "gainBlockPerEnergyMax":
		{
			int amount12 = effect.Amount.GetValueOrDefault() * state.Player.EnergyMaxAtTurnStart;
			return GainPlayerBlock(state, amount12, enemyIndex);
		}
		case "gainBlockPerExhaust":
		{
			int amount9 = effect.Amount.GetValueOrDefault() * state.Player.ExhaustPile.Count;
			return GainPlayerBlock(state, amount9, enemyIndex);
		}
		case "exhaustFromDeck":
			return MillPlayerDeck(state, effect.Amount.GetValueOrDefault(), enemyIndex);
		case "exhaustFromDeckChoose":
			return state;
		case "searchDeck":
		case "upgradeAllInHand":
		case "addCopyToDiscard":
		case "retrieveFromDiscard":
		case "growSelf":
		case "upgradeInHand":
			return state;
		case "gainMaxHp":
		{
			int amount8 = effect.Amount.GetValueOrDefault();
			if (amount8 <= 0)
			{
				return state;
			}
			GameState next = state with
			{
				Player = state.Player with
				{
					MaxHp = state.Player.MaxHp + amount8,
					Hp = state.Player.Hp + amount8
				}
			};
			return Events.Emit(next, new GameEvent_MaxHpGained
			{
				Amount = amount8,
				PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, next.Player)
			});
		}
		case "gainBlockPerMomentum":
		{
			int block = state.Player.Momentum * effect.Amount.GetValueOrDefault();
			if (block <= 0)
			{
				return state;
			}
			GameState s6 = state with
			{
				Player = state.Player with
				{
					Block = state.Player.Block + block
				}
			};
			s6 = Events.Emit(s6, new GameEvent_BlockGained
			{
				Target = "player",
				Amount = block,
				SourceUid = state.ResolvingPermanentUid,
				PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, s6.Player)
			});
			return RunPermanentTriggers(s6, "onBlockGained", enemyIndex);
		}
		case "addGrowthPerMomentum":
		{
			int gained = state.Player.Momentum / 2 * (effect.Amount ?? 1);
			if (gained <= 0)
			{
				return state;
			}
			return FireGainTrigger(Events.Emit(state with
			{
				Player = state.Player with
				{
					Growth = state.Player.Growth + gained
				}
			}, new GameEvent_GrowthAdded
			{
				Amount = gained
			}), "onGrowthGained", enemyIndex);
		}
		case "gainSetSlot":
		{
			int amount7 = effect.Amount ?? 1;
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					SetSlots = state.Player.SetSlots + amount7
				}
			}, new GameEvent_SetSlotGained
			{
				Amount = amount7
			});
		}
		case "addCardToDiscard":
		{
			CardDef def2 = Content.GetCardDef(effect.SummonId ?? "");
			List<CardInstance> made2 = new List<CardInstance>();
			for (int i5 = 0; i5 < (effect.Amount ?? 1); i5++)
			{
				made2.Add(new CardInstance
				{
					Uid = $"tokd_{state.EventLog.Count}_{i5}_{def2.Id}",
					Def = def2,
					Token = true
				});
			}
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					DiscardPile = state.Player.DiscardPile.Concat(made2).ToList()
				}
			}, new GameEvent_CardsAddedToDiscard
			{
				CardId = def2.Id,
				Count = made2.Count
			});
		}
		case "transformDeckToToken":
			return state;
		case "addCardToHand":
		{
			CardDef def = Content.GetCardDef(effect.SummonId ?? "");
			List<CardInstance> made = new List<CardInstance>();
			for (int i2 = 0; i2 < (effect.Amount ?? 1); i2++)
			{
				made.Add(new CardInstance
				{
					Uid = $"tok_{state.EventLog.Count}_{i2}_{def.Id}",
					Def = def,
					Token = true
				});
			}
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Hand = state.Player.Hand.Concat(made).ToList()
				}
			}, new GameEvent_CardsAddedToHand
			{
				CardId = def.Id,
				Count = made.Count
			});
		}
		case "recycleExhaust":
		{
			int n2 = state.Player.ExhaustPile.Count;
			if (n2 == 0)
			{
				return state;
			}
			List<CardInstance> pool = state.Player.DrawPile.Concat(state.Player.ExhaustPile).ToList();
			var (mixed, rng) = Rng.Shuffle(state.Rng, pool);
			return DealDamageToEnemy(Events.Emit(state with
			{
				Rng = rng,
				Player = state.Player with
				{
					DrawPile = mixed.ToList(),
					ExhaustPile = new List<CardInstance>()
				}
			}, new GameEvent_ExhaustRecycled
			{
				Count = n2
			}), enemyIndex, effect.Amount.GetValueOrDefault() * n2, effect.Pierce == true);
		}
		case "empowerShivs":
			return state;
		case "dealDamagePerExhaust":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.ExhaustPile.Count, effect.Pierce == true);
		case "exposeEnemy":
		{
			int amount20 = effect.Amount.GetValueOrDefault();
			EnemyState enemy13 = EnemyAt(state, enemyIndex);
			if (enemy13 == null || enemy13.Hp <= 0)
			{
				return state;
			}
			(GameState State, bool Blocked) tuple11 = TryArtifactBlock(state, enemyIndex, "exposeEnemy");
			var (sa3, _) = tuple11;
			if (tuple11.Blocked)
			{
				return sa3;
			}
			IReadOnlyList<EnemyState> enemies7 = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
			{
				Exposed = enemyState.Exposed + amount20
			});
			return Events.Emit(state with
			{
				Enemies = enemies7
			}, new GameEvent_ExposedApplied
			{
				EnemyIndex = enemyIndex,
				Amount = amount20
			});
		}
		case "dischargeGrowth":
		{
			int spent11 = state.Player.Growth;
			int keep = Math.Min(spent11, state.HarvestKeep.GetValueOrDefault());
			return Events.Emit(DealDamageToEnemy(state with
			{
				Player = state.Player with
				{
					Growth = keep
				}
			}, enemyIndex, spent11 * effect.Amount.GetValueOrDefault(), effect.Pierce == true), new GameEvent_GrowthDischarged
			{
				Spent = spent11
			});
		}
		case "dischargeGrowthBlock":
		{
			int spent10 = state.Player.Growth;
			GameState s23 = GainPlayerBlock(state, spent10 * effect.Amount.GetValueOrDefault(), enemyIndex);
			s23 = s23 with
			{
				Player = s23.Player with
				{
					Growth = 0
				}
			};
			return Events.Emit(s23, new GameEvent_GrowthDischarged
			{
				Spent = spent10
			});
		}
		case "dischargeMomentumBurn":
		{
			int spent8 = state.Player.Momentum;
			int burn3 = spent8 * effect.Amount.GetValueOrDefault();
			EnemyState target3 = EnemyAt(state, enemyIndex);
			if (burn3 <= 0 || target3 == null || target3.Hp <= 0)
			{
				return state;
			}
			IReadOnlyList<EnemyState> enemies6 = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
			{
				Burn = enemyState.Burn + burn3
			});
			return Events.Emit(state with
			{
				Enemies = enemies6,
				Player = state.Player with
				{
					Momentum = 0
				}
			}, new GameEvent_BurnApplied
			{
				EnemyIndex = enemyIndex,
				Amount = burn3
			});
		}
		case "dischargeMomentumBlock":
		{
			int block2 = state.Player.Momentum * effect.Amount.GetValueOrDefault();
			if (block2 <= 0)
			{
				return state;
			}
			GameState s19 = state with
			{
				Player = state.Player with
				{
					Momentum = 0,
					Block = state.Player.Block + block2
				}
			};
			s19 = Events.Emit(s19, new GameEvent_BlockGained
			{
				Target = "player",
				Amount = block2,
				SourceUid = state.ResolvingPermanentUid,
				PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, s19.Player)
			});
			return RunPermanentTriggers(s19, "onBlockGained", enemyIndex);
		}
		case "dischargeMomentumDamage":
		{
			int spent7 = state.Player.Momentum;
			if (spent7 <= 0)
			{
				return state;
			}
			if (!((effect.Target == "all") ? state.Enemies.Any((EnemyState enemyState) => enemyState.Hp > 0) : ((EnemyAt(state, enemyIndex)?.Hp ?? 0) > 0)))
			{
				return state;
			}
			int dmg2 = spent7 * effect.Amount.GetValueOrDefault();
			GameState s16 = state with
			{
				Player = state.Player with
				{
					Momentum = 0
				}
			};
			s16 = Events.Emit(s16, new GameEvent_MomentumDischarged
			{
				Spent = spent7
			});
			if (effect.Target == "all")
			{
				for (int i8 = 0; i8 < s16.Enemies.Count; i8++)
				{
					if (s16.Enemies[i8].Hp > 0)
					{
						s16 = DealDamageToEnemy(s16, i8, dmg2, effect.Pierce == true);
					}
				}
				return s16;
			}
			return DealDamageToEnemy(s16, enemyIndex, dmg2, effect.Pierce == true);
		}
		case "dischargeMomentumVolley":
		{
			int spent6 = state.Player.Momentum;
			if (spent6 <= 0)
			{
				return state;
			}
			if ((EnemyAt(state, enemyIndex)?.Hp ?? 0) <= 0)
			{
				return state;
			}
			int per = spent6 * (effect.Amount ?? 1);
			GameState s14 = state with
			{
				Player = state.Player with
				{
					Momentum = 0
				}
			};
			s14 = Events.Emit(s14, new GameEvent_MomentumDischarged
			{
				Spent = spent6
			});
			for (int h2 = 0; h2 < (effect.VolleyHits ?? 3); h2++)
			{
				if ((EnemyAt(s14, enemyIndex)?.Hp ?? 0) <= 0)
				{
					break;
				}
				s14 = DealDamageToEnemy(s14, enemyIndex, per, effect.Pierce == true);
			}
			return s14;
		}
		case "momentumCarryHalf":
			return state;
		case "dischargeMomentumGrowth":
		{
			int spent5 = state.Player.Momentum;
			if (spent5 <= 0)
			{
				return state;
			}
			int gained2 = (int)Math.Ceiling((double)spent5 / (double)Math.Max(1, effect.Amount ?? 2));
			GameState s13 = state with
			{
				Player = state.Player with
				{
					Momentum = 0,
					Growth = state.Player.Growth + gained2
				}
			};
			s13 = Events.Emit(s13, new GameEvent_MomentumDischarged
			{
				Spent = spent5
			});
			s13 = Events.Emit(s13, new GameEvent_GrowthAdded
			{
				Amount = gained2
			});
			if (gained2 <= 0)
			{
				return s13;
			}
			return FireGainTrigger(s13, "onGrowthGained", enemyIndex);
		}
		case "dealDamageCleave":
		{
			GameState s11 = DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault(), effect.Pierce == true);
			EnemyState hit = EnemyAt(s11, enemyIndex);
			if (hit != null && hit.Hp <= 0)
			{
				int nextIdx = -1;
				for (int i7 = 0; i7 < s11.Enemies.Count; i7++)
				{
					if (i7 != enemyIndex && s11.Enemies[i7].Hp > 0)
					{
						nextIdx = i7;
						break;
					}
				}
				if (nextIdx >= 0)
				{
					s11 = DealDamageToEnemy(s11, nextIdx, effect.Amount.GetValueOrDefault(), effect.Pierce == true);
				}
			}
			return s11;
		}
		case "shatterBlock":
		{
			EnemyState enemy4 = EnemyAt(state, enemyIndex);
			if (enemy4 == null || enemy4.Block == 0)
			{
				return state;
			}
			IReadOnlyList<EnemyState> enemies2 = MapEnemy(state.Enemies, enemyIndex, (EnemyState enemyState) => enemyState with
			{
				Block = 0
			});
			return BreakBurrowIfCracked(Events.Emit(state with
			{
				Enemies = enemies2
			}, new GameEvent_BlockShattered
			{
				EnemyIndex = enemyIndex,
				Amount = enemy4.Block,
				PresentationReceipt = Events.EnemyPresentationReceipt(enemyIndex, enemy4, enemies2[enemyIndex])
			}), enemyIndex);
		}
		case "dealDamageRandom":
			var (roll, rng3) = Rng.NextInt(state.Rng, effect.Amount.GetValueOrDefault(), effect.AmountMax ?? effect.Amount.GetValueOrDefault());
			return DealDamageToEnemy(state with
			{
				Rng = rng3
			}, enemyIndex, roll, effect.Pierce == true);
		case "impulseDraw":
		{
			HashSet<string> before = new HashSet<string>(state.Player.Hand.Select((CardInstance cardInstance) => cardInstance.Uid));
			GameState s8 = DrawCards(state, effect.Amount.GetValueOrDefault());
			List<string> drawnUids = (from cardInstance in s8.Player.Hand
				where !before.Contains(cardInstance.Uid)
				select cardInstance.Uid).ToList();
			if (drawnUids.Count == 0)
			{
				return s8;
			}
			s8 = s8 with
			{
				Player = s8.Player with
				{
					ImpulseUids = s8.Player.ImpulseUids.Concat(drawnUids).ToList()
				}
			};
			return Events.Emit(s8, new GameEvent_ImpulseDrawn
			{
				Count = drawnUids.Count
			});
		}
		case "loseHp":
			return LosePlayerHp(state, effect.Amount.GetValueOrDefault(), enemyIndex);
		case "discountNext":
		{
			int amount11 = effect.Amount.GetValueOrDefault();
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					NextCardDiscount = state.Player.NextCardDiscount + amount11
				}
			}, new GameEvent_DiscountGained
			{
				Amount = amount11
			});
		}
		case "dischargeAether":
		{
			int spent4 = state.Player.Aether;
			if (spent4 == 0)
			{
				return state;
			}
			return DealDamageToEnemy(Events.Emit(state with
			{
				Player = state.Player with
				{
					Aether = (AetherCarriesHalf(state) ? (spent4 / 2) : 0)
				}
			}, new GameEvent_AetherDischarged
			{
				Spent = spent4
			}), enemyIndex, spent4 * effect.Amount.GetValueOrDefault(), effect.Pierce == true);
		}
		case "dischargeLight":
		{
			int spent3 = state.Player.Light.GetValueOrDefault();
			if (spent3 <= 0)
			{
				return state;
			}
			bool num;
			if (!(effect.Target == "all"))
			{
				if (enemyIndex < 0 || enemyIndex >= state.Enemies.Count)
				{
					goto IL_462e;
				}
				num = state.Enemies[enemyIndex].Hp > 0;
			}
			else
			{
				num = state.Enemies.Any((EnemyState enemyState) => enemyState.Hp > 0);
			}
			if (num)
			{
				GameState s3 = state with
				{
					Player = state.Player with
					{
						Light = 0
					}
				};
				s3 = Events.Emit(s3, new GameEvent_LightDischarged
				{
					Spent = spent3
				});
				if (effect.Target == "all")
				{
					int dmg = spent3 * effect.Amount.GetValueOrDefault();
					for (int i3 = 0; i3 < s3.Enemies.Count; i3++)
					{
						if (s3.Enemies[i3].Hp > 0)
						{
							s3 = DealDamageToEnemy(s3, i3, dmg, effect.Pierce == true);
						}
					}
					return AfterLightDischarge(s3, spent3, enemyIndex);
				}
				for (int h = 0; h < spent3; h++)
				{
					EnemyState en = EnemyAt(s3, enemyIndex);
					if (en == null || en.Hp <= 0)
					{
						break;
					}
					s3 = DealDamageToEnemy(s3, enemyIndex, effect.Amount.GetValueOrDefault(), effect.Pierce == true);
				}
				return AfterLightDischarge(s3, spent3, enemyIndex);
			}
			goto IL_462e;
		}
		case "dischargeLightRally":
		{
			int spent2 = state.Player.Light.GetValueOrDefault();
			if (spent2 <= 0)
			{
				return state;
			}
			int n = state.Player.Permanents.Count((CardInstance cardInstance) => cardInstance.Def.Retainer == true && cardInstance.Innate != true);
			if (n == 0)
			{
				return state;
			}
			GameState s = state with
			{
				Player = state.Player with
				{
					Light = 0
				}
			};
			s = Events.Emit(s, new GameEvent_LightDischarged
			{
				Spent = spent2
			});
			int times = spent2 / Math.Max(1, effect.Amount ?? 1);
			if (times <= 0)
			{
				return state;
			}
			for (int i = 0; i < times; i++)
			{
				s = RallyRetainers(s, enemyIndex);
			}
			s = Events.Emit(s, new GameEvent_RetainersTriggered
			{
				Count = n * times
			});
			return AfterLightDischarge(s, spent2, enemyIndex);
		}
		case "dischargeLightWeaken":
		{
			int spent12 = state.Player.Light.GetValueOrDefault();
			int stacks = spent12 / 3 * (effect.Amount ?? 1);
			if (spent12 <= 0 || stacks <= 0)
			{
				return state;
			}
			GameState s24 = state with
			{
				Player = state.Player with
				{
					Light = 0
				}
			};
			s24 = Events.Emit(s24, new GameEvent_LightDischarged
			{
				Spent = spent12
			});
			for (int i11 = 0; i11 < s24.Enemies.Count; i11++)
			{
				if (s24.Enemies[i11].Hp > 0)
				{
					s24 = ResolveEffect(s24, new DeclarativeEffect
					{
						Trigger = effect.Trigger,
						Effect = "weakenEnemy",
						Amount = stacks
					}, i11);
				}
			}
			return AfterLightDischarge(s24, spent12, enemyIndex);
		}
		case "consumeLight":
		{
			int have = state.Player.Light.GetValueOrDefault();
			int spent9 = (effect.Amount.HasValue ? Math.Min(have, effect.Amount.Value) : have);
			if (spent9 <= 0)
			{
				return state;
			}
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Light = have - spent9
				}
			}, new GameEvent_LightDischarged
			{
				Spent = spent9,
				Paid = true
			});
		}
		case "gainBlockPerLight":
		{
			int block3 = state.Player.Light.GetValueOrDefault() / 2 * effect.Amount.GetValueOrDefault();
			if (block3 <= 0)
			{
				return state;
			}
			GameState s21 = state with
			{
				Player = state.Player with
				{
					Block = state.Player.Block + block3
				}
			};
			s21 = Events.Emit(s21, new GameEvent_BlockGained
			{
				Target = "player",
				Amount = block3,
				SourceUid = state.ResolvingPermanentUid,
				PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, s21.Player)
			});
			return RunPermanentTriggers(s21, "onBlockGained", enemyIndex);
		}
		case "drawCardsPerLight":
		{
			int n11 = Math.Min(effect.AmountMax ?? 99, state.Player.Light.GetValueOrDefault() / 2 * (effect.Amount ?? 1));
			if (n11 <= 0)
			{
				return state;
			}
			return DrawCards(state, n11);
		}
		case "dismissUnlessLight":
		{
			string uid3 = state.ResolvingPermanentUid;
			CardInstance me = ((uid3 == null) ? null : state.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == uid3));
			if (me == null || state.Player.Light.GetValueOrDefault() >= effect.Amount.GetValueOrDefault())
			{
				return state;
			}
			List<CardInstance> perms2 = state.Player.Permanents.Where((CardInstance cardInstance) => cardInstance.Uid != uid3).ToList();
			List<CardInstance> discard = new List<CardInstance>(state.Player.DiscardPile) { me };
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Permanents = perms2,
					DiscardPile = discard
				}
			}, new GameEvent_PermanentDismissed
			{
				CardId = me.Def.Id,
				Uid = me.Uid
			});
		}
		case "lightCarryHalf":
			return state;
		case "addCardToDraw":
		{
			CardDef def3 = Content.GetCardDef(effect.SummonId ?? "");
			int n9 = effect.Amount ?? 1;
			List<CardInstance> drawPile = state.Player.DrawPile.ToList();
			RngState rng4 = state.Rng;
			for (int i9 = 0; i9 < n9; i9++)
			{
				int pos;
				(pos, rng4) = Rng.NextInt(rng4, 0, drawPile.Count);
				drawPile.Insert(pos, new CardInstance
				{
					Uid = $"tok_{state.EventLog.Count}_{i9}_{def3.Id}",
					Def = def3,
					Token = true
				});
			}
			return Events.Emit(state with
			{
				Rng = rng4,
				Player = state.Player with
				{
					DrawPile = drawPile
				}
			}, new GameEvent_CardsAddedToDraw
			{
				CardId = def3.Id,
				Count = n9
			});
		}
		case "lightToSparks":
		{
			int per2 = Math.Max(1, effect.Amount ?? 3);
			int n8 = Math.Min(state.HearthSparks.GetValueOrDefault(), state.Player.Light.GetValueOrDefault() / per2);
			if (n8 <= 0)
			{
				return state;
			}
			return ResolveEffect(Events.Emit(state with
			{
				Player = state.Player with
				{
					Light = state.Player.Light.GetValueOrDefault() - n8 * per2
				}
			}, new GameEvent_LightDischarged
			{
				Spent = n8 * per2,
				Sparks = n8
			}), new DeclarativeEffect
			{
				Trigger = effect.Trigger,
				Effect = "addCardToDraw",
				SummonId = (effect.SummonId ?? "white_spark_token"),
				Amount = n8
			}, enemyIndex);
		}
		case "dealDamagePerSpark":
			if (state.Player.SparksPlayedThisCombat.GetValueOrDefault() > 0)
			{
				return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.SparksPlayedThisCombat.GetValueOrDefault(), effect.Pierce == true);
			}
			return state;
		case "dealDamagePerScry":
			if (state.Player.ScryDiscardedThisCombat.GetValueOrDefault() > 0)
			{
				return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.ScryDiscardedThisCombat.GetValueOrDefault(), effect.Pierce == true);
			}
			return state;
		case "dealDamagePerTrapFired":
			if (state.Player.TrapsFiredThisCombat.GetValueOrDefault() > 0)
			{
				return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.TrapsFiredThisCombat.GetValueOrDefault(), effect.Pierce == true);
			}
			return state;
		case "scry":
		{
			int n6 = effect.Amount.GetValueOrDefault();
			if (n6 <= 0 || state.EnemyPhase == true)
			{
				return state;
			}
			GameStatePendingScry prev2 = state.PendingScry;
			return state with
			{
				PendingScry = new GameStatePendingScry
				{
					Count = (prev2?.Count ?? 0) + n6,
					Times = (prev2?.Times ?? 0) + 1
				}
			};
		}
		case "extendTrapLife":
		{
			int n5 = effect.Amount.GetValueOrDefault();
			if (n5 <= 0 || state.Player.SetCards.Count == 0)
			{
				return state;
			}
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					SetCards = state.Player.SetCards.Select((CardInstance cardInstance) => cardInstance with
					{
						TrapLifeBonus = cardInstance.TrapLifeBonus.GetValueOrDefault() + n5
					}).ToList()
				}
			}, new GameEvent_TrapLifeExtended
			{
				Amount = n5,
				Count = state.Player.SetCards.Count
			});
		}
		case "retrieveZeroCostFromDiscard":
		{
			List<CardInstance> back = state.Player.DiscardPile.Where((CardInstance cardInstance) => cardInstance.Def.Cost == 0 && cardInstance.Def.XCost != true && !cardInstance.Def.Id.StartsWith("status_", StringComparison.Ordinal)).ToList();
			if (back.Count == 0)
			{
				return state;
			}
			HashSet<string> ids2 = new HashSet<string>(back.Select((CardInstance cardInstance) => cardInstance.Uid));
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					DiscardPile = state.Player.DiscardPile.Where((CardInstance cardInstance) => !ids2.Contains(cardInstance.Uid)).ToList(),
					Hand = state.Player.Hand.Concat(back).ToList()
				}
			}, new GameEvent_CardsMovedToHand
			{
				CardIds = back.Select((CardInstance cardInstance) => cardInstance.Def.Id).ToList(),
				From = "discard"
			});
		}
		case "drawTypeFromDeck":
		{
			int n4 = effect.Amount ?? 1;
			List<CardInstance> picked = state.Player.DrawPile.Where((CardInstance cardInstance) => effect.CardType == null || cardInstance.Def.Type == effect.CardType).Take(n4).ToList();
			if (picked.Count == 0)
			{
				return state;
			}
			HashSet<string> ids = new HashSet<string>(picked.Select((CardInstance cardInstance) => cardInstance.Uid));
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					DrawPile = state.Player.DrawPile.Where((CardInstance cardInstance) => !ids.Contains(cardInstance.Uid)).ToList(),
					Hand = state.Player.Hand.Concat(picked).ToList()
				}
			}, new GameEvent_CardsMovedToHand
			{
				CardIds = picked.Select((CardInstance cardInstance) => cardInstance.Def.Id).ToList(),
				From = "draw"
			});
		}
		case "burnNoDecay":
		case "retainedCostDown":
		case "trapsNeverExpire":
		case "firstCardCascade":
		case "aetherCarryHalf":
		case "cascadeSameCost":
		case "retainHandUpTo":
		case "keepImpulseOne":
			return state;
		case "dealDamagePerCascade":
			if (state.Player.CascadesThisCombat.GetValueOrDefault() > 0)
			{
				return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.CascadesThisCombat.GetValueOrDefault(), effect.Pierce == true);
			}
			return state;
		case "doubleBurn":
		{
			EnemyState enemy3 = ((enemyIndex >= 0 && enemyIndex < state.Enemies.Count) ? state.Enemies[enemyIndex] : null);
			if (enemy3 == null || enemy3.Hp <= 0 || enemy3.Burn <= 0)
			{
				return state;
			}
			return ResolveEffect(state, new DeclarativeEffect
			{
				Trigger = effect.Trigger,
				Effect = "applyBurn",
				Amount = enemy3.Burn
			}, enemyIndex);
		}
		case "gainBlockPerBurn":
		{
			EnemyState enemy2 = ((enemyIndex >= 0 && enemyIndex < state.Enemies.Count) ? state.Enemies[enemyIndex] : null);
			if (enemy2 == null || enemy2.Hp <= 0 || enemy2.Burn <= 0)
			{
				return state;
			}
			return GainPlayerBlock(state, effect.Amount.GetValueOrDefault() * enemy2.Burn, enemyIndex);
		}
		case "dealDamagePerBurn":
		{
			EnemyState enemy = ((enemyIndex >= 0 && enemyIndex < state.Enemies.Count) ? state.Enemies[enemyIndex] : null);
			if (enemy == null || enemy.Hp <= 0 || enemy.Burn <= 0)
			{
				return state;
			}
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * enemy.Burn, effect.Pierce == true);
		}
		case "gainBlockPerSpark":
			if (state.Player.SparksPlayedThisCombat.GetValueOrDefault() > 0)
			{
				return GainPlayerBlock(state, effect.Amount.GetValueOrDefault() * state.Player.SparksPlayedThisCombat.GetValueOrDefault(), enemyIndex);
			}
			return state;
		case "triggerRandomRetainer":
		{
			List<CardInstance> retainers = state.Player.Permanents.Where((CardInstance cardInstance) => cardInstance.Def.Retainer == true && cardInstance.Innate != true).ToList();
			if (retainers.Count == 0)
			{
				return state;
			}
			RngState rng2 = state.Rng;
			int pick = 0;
			if (retainers.Count > 1)
			{
				(int Value, RngState Next) tuple2 = Rng.NextInt(rng2, 0, retainers.Count - 1);
				int r = tuple2.Value;
				rng2 = tuple2.Next;
				pick = r;
			}
			CardInstance target = retainers[pick];
			List<string> triggers = new List<string>();
			foreach (DeclarativeEffect e in target.Def.Effects)
			{
				if (e.Trigger != "onPermanentEntered" && e.Trigger != "onPlay" && !triggers.Contains(e.Trigger))
				{
					triggers.Add(e.Trigger);
				}
			}
			bool prev = state.SuppressLightGain == true;
			GameState s4 = state with
			{
				Rng = rng2,
				SuppressLightGain = true
			};
			foreach (string tr in triggers)
			{
				s4 = RunPermanentTriggers(s4, tr, enemyIndex, (CardInstance cardInstance) => cardInstance.Uid == target.Uid);
			}
			s4 = s4 with
			{
				SuppressLightGain = prev
			};
			return Events.Emit(s4, new GameEvent_RetainersTriggered
			{
				Count = 1
			});
		}
		case "gainEnergyMax":
		{
			int amount4 = effect.Amount.GetValueOrDefault();
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					EnergyMax = state.Player.EnergyMax + amount4
				}
			}, new GameEvent_EnergyMaxGained
			{
				Amount = amount4
			});
		}
		case "addGrowth":
		{
			int amount3 = effect.Amount.GetValueOrDefault();
			GameState emitted = Events.Emit(state with
			{
				Player = state.Player with
				{
					Growth = state.Player.Growth + amount3
				}
			}, new GameEvent_GrowthAdded
			{
				Amount = amount3
			});
			if (amount3 <= 0)
			{
				return emitted;
			}
			return FireGainTrigger(emitted, "onGrowthGained", enemyIndex);
		}
		case "doubleMomentum":
			if (state.Player.Momentum == 0)
			{
				return state;
			}
			return FireGainTrigger(state with
			{
				Player = state.Player with
				{
					Momentum = state.Player.Momentum * 2
				}
			}, "onMomentumGained", enemyIndex);
		case "doubleGrowth":
		{
			int amount = state.Player.Growth;
			if (amount == 0)
			{
				return state;
			}
			return FireGainTrigger(Events.Emit(state with
			{
				Player = state.Player with
				{
					Growth = state.Player.Growth * 2
				}
			}, new GameEvent_GrowthAdded
			{
				Amount = amount
			}), "onGrowthGained", enemyIndex);
		}
		case "drawCards":
			return DrawCards(state, effect.Amount.GetValueOrDefault());
		case "negate":
			return state with
			{
				NegateNextAction = true
			};
		case "negateConvertIce":
		{
			int actual = ReactionActionValue(state, enemyIndex);
			GameState s2 = state with
			{
				NegateNextAction = true
			};
			if (actual > 0)
			{
				s2 = GainPlayerIceBlock(s2, actual);
			}
			return s2;
		}
		case "dealDamagePerIceBlock":
			return DealDamageToEnemy(state, enemyIndex, effect.Amount.GetValueOrDefault() * state.Player.IceBlock, effect.Pierce == true, applyExpose: false);
		case "dischargeAetherDraw":
		{
			int spent = state.Player.Aether;
			if (spent == 0)
			{
				return state;
			}
			return DrawCards(Events.Emit(state with
			{
				Player = state.Player with
				{
					Aether = (AetherCarriesHalf(state) ? (spent / 2) : 0)
				}
			}, new GameEvent_AetherDischarged
			{
				Spent = spent
			}), spent * (effect.Amount ?? 1));
		}
		case "script":
			throw new InvalidOperationException("未登録のスクリプト効果: " + effect.ScriptId);
		default:
			{
				throw new NotImplementedException("effect: " + effect.Effect);
			}
			IL_462e:
			return state;
		}
	}

	public static bool BlazeConditionMet(GameState state, DeclarativeEffect effect, int? enemyIndex = null)
	{
		EffectCondition? condition = effect.Condition;
		if ((object)condition != null && condition.Blaze == true && !IsBlazing(state))
		{
			return false;
		}
		EffectCondition? condition2 = effect.Condition;
		if ((object)condition2 != null && condition2.Kindled == true && state.ResolvingKindled != true)
		{
			return false;
		}
		EffectCondition c = effect.Condition;
		if (c != null)
		{
			if (effect.Trigger == "onEnemyActed" && (c.ActionKinds != null || c.ActionKindsNot != null))
			{
				string k = state.LastAction?.Kind;
				if (k == null)
				{
					return false;
				}
				if (c.ActionKinds != null && !c.ActionKinds.Contains(k))
				{
					return false;
				}
				if (c.ActionKindsNot != null && c.ActionKindsNot.Contains(k))
				{
					return false;
				}
			}
			EnemyState e = (enemyIndex.HasValue ? EnemyAt(state, enemyIndex.Value) : null);
			if (c.EnemyIntent != null || c.EnemyIntentNot != null)
			{
				string kind = ((!enemyIndex.HasValue) ? null : EffectiveIntent(state, enemyIndex.Value)?.Kind);
				if (c.EnemyIntent != null && kind != c.EnemyIntent)
				{
					return false;
				}
				if (c.EnemyIntentNot != null && (kind == null || kind == c.EnemyIntentNot))
				{
					return false;
				}
			}
			if (c.EnemyExposed == true)
			{
				int exposed = 0;
				if (enemyIndex.HasValue)
				{
					IReadOnlyList<int> snap = state.ResolvingExposedAtStart;
					exposed = ((snap != null && enemyIndex.Value >= 0 && enemyIndex.Value < snap.Count) ? new int?(snap[enemyIndex.Value]) : ((int?)null)) ?? e?.Exposed ?? 0;
				}
				if (exposed <= 0)
				{
					return false;
				}
			}
			if (c.PerfectBlockLastPhase == true && state.Player.PerfectBlockLastPhase != true)
			{
				return false;
			}
			if (c.TargetDead == true && (!(e != null) || e.Hp > 0))
			{
				return false;
			}
			if (c.TargetAlive == true && (!(e != null) || e.Hp <= 0))
			{
				return false;
			}
			if (c.LastActionNoHpLoss == true && (!(state.LastAction != null) || !(state.LastAction.Kind == "attack") || state.LastAction.HpLoss != 0))
			{
				return false;
			}
			if (c.Turn.HasValue && state.Turn != c.Turn.Value)
			{
				return false;
			}
			if (c.BlockZero == true && state.Player.Block > 0)
			{
				return false;
			}
			if (c.NoAttackThisTurn == true && state.Player.AttacksPlayedThisTurn.GetValueOrDefault() > 0)
			{
				return false;
			}
			if (c.MaxPlaysThisTurn.HasValue && state.Player.PlaysThisTurn.GetValueOrDefault() > c.MaxPlaysThisTurn.Value)
			{
				return false;
			}
		}
		EffectCondition? condition3 = effect.Condition;
		if ((object)condition3 != null && condition3.MinGrowth.HasValue && state.Player.Growth < effect.Condition.MinGrowth.Value)
		{
			return false;
		}
		EffectCondition? condition4 = effect.Condition;
		if ((object)condition4 != null && condition4.MinMomentum.HasValue && state.Player.Momentum < effect.Condition.MinMomentum.Value)
		{
			return false;
		}
		EffectCondition? condition5 = effect.Condition;
		if ((object)condition5 != null && condition5.MinLight.HasValue && state.Player.Light.GetValueOrDefault() < effect.Condition.MinLight.Value)
		{
			return false;
		}
		EffectCondition? condition6 = effect.Condition;
		if ((object)condition6 != null && condition6.MinEnergyMax.HasValue && state.Player.EnergyMaxAtTurnStart < effect.Condition.MinEnergyMax.Value)
		{
			return false;
		}
		EffectCondition? condition7 = effect.Condition;
		if ((object)condition7 != null && condition7.HealedThisTurn == true && state.Player.HealsThisTurn.GetValueOrDefault() <= 0)
		{
			return false;
		}
		EffectCondition? condition8 = effect.Condition;
		if ((object)condition8 != null && condition8.HpAtOrBelowRatio.HasValue && (double)state.Player.Hp > (double)state.Player.MaxHp * effect.Condition.HpAtOrBelowRatio.Value)
		{
			return false;
		}
		return true;
	}

	private static GameState FireGainTrigger(GameState state, string trigger, int enemyIndex)
	{
		if (state.ResolvingGainTrigger == true)
		{
			return state;
		}
		bool any = false;
		foreach (CardInstance permanent in state.Player.Permanents)
		{
			foreach (DeclarativeEffect effect in permanent.Def.Effects)
			{
				if (effect.Trigger == trigger)
				{
					any = true;
					break;
				}
			}
			if (any)
			{
				break;
			}
		}
		if (!any)
		{
			return state;
		}
		GameState state2 = state with
		{
			ResolvingGainTrigger = true
		};
		return RunPermanentTriggers(state2, trigger, enemyIndex)with
		{
			ResolvingGainTrigger = false
		};
	}

	public static GameState ResolveOnPlayEffects(GameState state, CardInstance card, int enemyIndex)
	{
		bool prev = state.ResolvingCardPlay == true;
		IReadOnlyList<int> prevExposed = state.ResolvingExposedAtStart;
		GameState s = state with
		{
			ResolvingCardPlay = true,
			AngerFiredThisPlay = false,
			ResolvingExposedAtStart = state.Enemies.Select((EnemyState e) => e.Exposed).ToList()
		};
		foreach (DeclarativeEffect effect in card.Def.Effects)
		{
			if (effect.Trigger == "onPlay" && BlazeConditionMet(s, effect, enemyIndex))
			{
				s = ResolveEffectTargeted(s, effect, enemyIndex, new DamagePresentationSource
				{
					Kind = "card",
					DefinitionId = card.Def.Id,
					Uid = card.Uid
				});
			}
		}
		return s with
		{
			ResolvingCardPlay = prev,
			ResolvingExposedAtStart = prevExposed
		};
	}

	public static GameState ResolveIgniteEffects(GameState state, CardInstance card, int enemyIndex, bool wasBlazing)
	{
		if (wasBlazing || !IsBlazing(state))
		{
			return state;
		}
		List<DeclarativeEffect> effects = card.Def.Effects.Where((DeclarativeEffect e) => e.Trigger == "onBlazeIgnite").ToList();
		if (effects.Count == 0)
		{
			return state;
		}
		bool prev = state.ResolvingCardPlay == true;
		GameState s = state with
		{
			ResolvingCardPlay = true
		};
		foreach (DeclarativeEffect effect in effects)
		{
			s = ResolveEffectTargeted(s, effect, enemyIndex, new DamagePresentationSource
			{
				Kind = "card",
				DefinitionId = card.Def.Id,
				Uid = card.Uid
			});
		}
		return s with
		{
			ResolvingCardPlay = prev
		};
	}

	public static GameState ResolveReactionEffects(GameState state, CardInstance card, int enemyIndex)
	{
		bool prevCardPlay = state.ResolvingCardPlay == true;
		GameState s = Events.Emit(state with
		{
			ResolvingCardPlay = false
		}, new GameEvent_ReactionTriggered
		{
			CardId = card.Def.Id,
			Mode = state.ReactionMode
		});
		int actionActual = ReactionActionValue(s, enemyIndex);
		foreach (DeclarativeEffect effect in SetAny.SetEffectsOf(card))
		{
			if (!REACTION_TRIGGERS.Contains(effect.Trigger))
			{
				continue;
			}
			EffectCondition c = effect.Condition;
			if (((object)c == null || !c.MinActionValue.HasValue || actionActual >= c.MinActionValue.Value) && ((object)c == null || !c.MaxActionValue.HasValue || actionActual <= c.MaxActionValue.Value))
			{
				if ((object)c != null && c.PerfectBlockThisPhase == true)
				{
					List<GameStatePendingPhaseEffects> pending = ((s.PendingPhaseEffects != null) ? new List<GameStatePendingPhaseEffects>(s.PendingPhaseEffects) : new List<GameStatePendingPhaseEffects>());
					pending.Add(new GameStatePendingPhaseEffects
					{
						Effect = effect,
						EnemyIndex = enemyIndex,
						PresentationSource = new DamagePresentationSource
						{
							Kind = "reaction",
							DefinitionId = card.Def.Id,
							Uid = card.Uid
						}
					});
					s = s with
					{
						PendingPhaseEffects = pending
					};
				}
				else if (BlazeConditionMet(s, effect, enemyIndex))
				{
					s = ResolveEffectTargeted(s, effect, enemyIndex, new DamagePresentationSource
					{
						Kind = "reaction",
						DefinitionId = card.Def.Id,
						Uid = card.Uid
					});
				}
			}
		}
		GameState counted = ((card.Def.Type == "reaction") ? s with
		{
			Player = s.Player with
			{
				TrapsFiredThisCombat = s.Player.TrapsFiredThisCombat.GetValueOrDefault() + 1
			}
		} : s);
		GameState obj = ((card.Def.Type == "reaction") ? RunPermanentTriggers(counted, "onReactionFired", enemyIndex) : counted);
		return obj with
		{
			ResolvingCardPlay = prevCardPlay
		};
	}
}
