using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Combat
{
	public const int PLAYER_MAX_HP = 75;

	private const int BASE_ENERGY = 3;

	private const int DRAW_PER_TURN = 5;

	public const int RESTRAIN_PLAY_CAP = 3;

	public const int MAX_ENEMIES_ON_FIELD = 4;

	public static readonly HashSet<string> DOLL_CHOICE_EFFECTS = new HashSet<string> { "copyRetainer", "extendRetainerLife", "persistRetainer" };

	public static int? PlayCapOf(GameState state)
	{
		int? cap = null;
		if (state.Player.Restrain > 0)
		{
			cap = 3;
		}
		if (state.PlayCap.HasValue)
		{
			return (!cap.HasValue) ? state.PlayCap : new int?(Math.Min(cap.Value, state.PlayCap.Value));
		}
		return cap;
	}

	private static void AssertPlayCap(GameState state)
	{
		int? cap = PlayCapOf(state);
		if (cap.HasValue && state.Player.PlaysThisTurn.GetValueOrDefault() >= cap.Value)
		{
			string why = ((state.Player.Restrain > 0 && cap.Value == 3) ? "拘束中は" : "天鵞絨の首輪により");
			throw new InvalidOperationException($"{why}1ターンに{cap.Value}枚までしかプレイできない (すでに{cap.Value}枚プレイ済み)");
		}
	}

	private static int JsRound(double v)
	{
		return (int)Math.Floor(v + 0.5);
	}

	private static List<T> MapIdx<T>(IReadOnlyList<T> src, Func<T, int, T> f)
	{
		List<T> outp = new List<T>(src.Count);
		for (int i = 0; i < src.Count; i++)
		{
			outp.Add(f(src[i], i));
		}
		return outp;
	}

	private static List<T> Concat<T>(IReadOnlyList<T> a, IEnumerable<T> b)
	{
		List<T> list = new List<T>(a);
		list.AddRange(b);
		return list;
	}

	private static List<T> Append<T>(IReadOnlyList<T> a, T b)
	{
		return new List<T>(a) { b };
	}

	private static GameState WithEnemy(GameState s, int index, Func<EnemyState, EnemyState> f)
	{
		List<EnemyState> list = new List<EnemyState>(s.Enemies);
		list[index] = f(list[index]);
		return s with
		{
			Enemies = list
		};
	}

	private static int FindAlive(IReadOnlyList<EnemyState> enemies)
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

	private static int FirstAliveOrZero(GameState s)
	{
		return Math.Max(0, FindAlive(s.Enemies));
	}

	private static bool IsOver(GameState s)
	{
		if (!(s.Phase == "won"))
		{
			return s.Phase == "lost";
		}
		return true;
	}

	public static GameState CreateInitialState(int seed, string reactionMode)
	{
		return new GameState
		{
			Rng = Rng.Create(seed),
			ReactionMode = reactionMode,
			Phase = "player-turn",
			Turn = 0,
			Player = new PlayerState
			{
				Hp = 75,
				MaxHp = 75,
				Block = 0,
				Energy = 3,
				EnergyMax = 3,
				EnergyMaxAtTurnStart = 3,
				DrawPerTurn = 5,
				Hand = new List<CardInstance>(),
				DrawPile = new List<CardInstance>(),
				DiscardPile = new List<CardInstance>(),
				SetCards = new List<CardInstance>(),
				SetSlots = 1,
				Permanents = new List<CardInstance>(),
				ExhaustPile = new List<CardInstance>(),
				Growth = 0,
				Momentum = 0,
				IceBlock = 0,
				CardsPlayedThisTurn = 0,
				SetsThisTurn = 0,
				PlaysThisTurn = 0,
				AttacksPlayedThisTurn = 0,
				HealsThisTurn = 0,
				WeakFreshThisPhase = 0,
				CardsPlayedTotal = 0,
				Aether = 0,
				Light = 0,
				HealsThisCombat = 0,
				NextCardDiscount = 0,
				ImpulseUids = new List<string>(),
				Weak = 0,
				Vulnerable = 0,
				Frail = 0,
				Restrain = 0,
				SelfHpLost = 0,
				RandomPlayedThisCombat = 0,
				DamageTakenLastEnemyPhase = 0,
				SpellEchoes = 0
			},
			Enemies = new List<EnemyState>(),
			PendingWindow = null,
			NegateNextAction = false,
			ReactionUsedThisAction = false,
			LastAction = null,
			EventLog = new List<GameEvent>()
		};
	}

	public static GameState StartCombatWithOptions(int seed, string reactionMode, string enemyId, CombatOptions options)
	{
		IReadOnlyList<EncounterMember> members = Content.ResolveEncounter(enemyId);
		GameState state = CreateInitialState(seed, reactionMode);
		LeaderDef leader = ((!string.IsNullOrEmpty(options.LeaderId)) ? Content.GetLeaderDef(options.LeaderId) : null);
		if (leader != null)
		{
			state = state with
			{
				Player = state.Player with
				{
					MaxHp = leader.MaxHp,
					Hp = leader.MaxHp,
					DrawPerTurn = leader.DrawPerTurn,
					Energy = leader.EnergyMax,
					EnergyMax = leader.EnergyMax,
					EnergyMaxAtTurnStart = leader.EnergyMax + options.EnergyMaxRefBonus.GetValueOrDefault(),
					SetSlots = (leader.SetSlots ?? 1),
					Permanents = new List<CardInstance> { Content.BuildLeaderPassive(leader) }
				}
			};
		}
		(CardInstance[] Items, RngState Next) tuple = Rng.Shuffle(state.Rng, options.Deck);
		CardInstance[] deck = tuple.Items;
		RngState rng = tuple.Next;
		List<EnemyState> enemies = new List<EnemyState>(members.Count);
		for (int mi = 0; mi < members.Count; mi++)
		{
			EncounterMember m = members[mi];
			EnemyDef def = Content.GetEnemyDef(m.EnemyId);
			double baseHp = def.MaxHp;
			if (def.HpRange != null && def.HpRange.Count >= 2)
			{
				(int Value, RngState Next) tuple2 = Rng.NextInt(rng, def.HpRange[0], def.HpRange[1]);
				int rolled = tuple2.Value;
				rng = tuple2.Next;
				baseHp = rolled;
			}
			int maxHp = JsRound(baseHp * (options.EnemyHpScale ?? 1.0) * (m.HpScale ?? 1.0));
			enemies.Add(new EnemyState
			{
				EnemyId = m.EnemyId,
				Hp = maxHp,
				MaxHp = maxHp,
				Block = (def.Burrow?.Block ?? def.StartingBlock.GetValueOrDefault()),
				BurrowActive = ((def.Burrow != null) ? new bool?(true) : ((bool?)null)),
				Intent = null,
				Strength = options.EnemyStrength.GetValueOrDefault() + m.Strength.GetValueOrDefault(),
				AtkScale = ((options.EnemyAtkScale.HasValue && options.EnemyAtkScale.GetValueOrDefault() != 1.0) ? options.EnemyAtkScale : ((double?)null)),
				Burn = 0,
				Confusion = 0,
				Exposed = 0,
				Node = EnemyGraph.StartNodeFor(def, mi, m.Start),
				NoReactTable = ((m.NoReactTable == true) ? new bool?(true) : ((bool?)null)),
				Thorns = def.Thorns,
				Artifact = def.Artifact,
				Armor = def.Armor,
				Slippery = def.Slippery
			});
		}
		state = state with
		{
			Rng = rng,
			Player = state.Player with
			{
				DrawPile = deck,
				MaxHp = (options.PlayerMaxHp ?? state.Player.MaxHp),
				Hp = Math.Min(options.PlayerHp ?? options.PlayerMaxHp ?? state.Player.MaxHp, options.PlayerMaxHp ?? state.Player.MaxHp),
				Permanents = Concat(state.Player.Permanents, options.RelicPermanents ?? new List<CardInstance>()),
				Artifact = ((options.Artifact.GetValueOrDefault() != 0) ? options.Artifact : ((int?)null))
			},
			Enemies = enemies,
			SetDamageReduction = ((options.SetDamageReduction.GetValueOrDefault() != 0) ? options.SetDamageReduction : ((int?)null)),
			GoldAvailable = options.GoldAvailable,
			ExpireToHand = ((options.ExpireToHand == true) ? new bool?(true) : ((bool?)null)),
			EnergyMaxRefBonus = ((options.EnergyMaxRefBonus.GetValueOrDefault() != 0) ? options.EnergyMaxRefBonus : ((int?)null)),
			HarvestKeep = ((options.HarvestKeep.GetValueOrDefault() != 0) ? options.HarvestKeep : ((int?)null)),
			SetAnyCards = ((options.SetAnyCards == true) ? new bool?(true) : ((bool?)null)),
			RetainHand = ((options.RetainHand == true) ? new bool?(true) : ((bool?)null)),
			EnergyCarry = ((options.EnergyCarry == true) ? new bool?(true) : ((bool?)null)),
			BlockKeep = ((options.BlockKeep.GetValueOrDefault() != 0) ? options.BlockKeep : ((int?)null)),
			XBonus = ((options.XBonus.GetValueOrDefault() != 0) ? options.XBonus : ((int?)null)),
			HpLossReduce = ((options.HpLossReduce.GetValueOrDefault() != 0) ? options.HpLossReduce : ((int?)null)),
			SmallHitToOne = ((options.SmallHitToOne.GetValueOrDefault() != 0) ? options.SmallHitToOne : ((int?)null)),
			MaxHpLossPerTurn = ((options.MaxHpLossPerTurn.GetValueOrDefault() != 0) ? options.MaxHpLossPerTurn : ((int?)null)),
			MaxHpLossCharges = options.MaxHpLossCharges,
			DeathSave = ((options.DeathSave == true) ? new bool?(true) : ((bool?)null)),
			PlayCap = ((options.PlayCap.GetValueOrDefault() != 0) ? options.PlayCap : ((int?)null)),
			HideIntents = ((options.HideIntents == true) ? new bool?(true) : ((bool?)null)),
			BrandsPlayable = ((options.BrandsPlayable == true) ? new bool?(true) : ((bool?)null))
		};
		state = Events.Emit(state, new GameEvent_CombatStarted
		{
			EnemyId = enemyId
		});
		GameState s = StartPlayerTurn(state, 1);
		s = Effects.RunPermanentTriggers(s, "onCombatStart", FirstAliveOrZero(s));
		if (options.StartGrowth.GetValueOrDefault() > 0)
		{
			s = Effects.ResolveEffectTargeted(s with
			{
				InnateResolving = true
			}, new DeclarativeEffect
			{
				Trigger = "onCombatStart",
				Effect = "addGrowth",
				Amount = options.StartGrowth
			}, FirstAliveOrZero(s));
			s = s with
			{
				InnateResolving = false
			};
		}
		return s;
	}

	public static GameState StartCombat(int seed, string reactionMode, string enemyId, string deckId = "starter", string leaderId = null, IReadOnlyList<string> cardIds = null)
	{
		IReadOnlyList<CardInstance> readOnlyList;
		if (cardIds == null || cardIds.Count <= 0)
		{
			readOnlyList = Content.BuildDeck(deckId ?? "starter");
		}
		else
		{
			IReadOnlyList<CardInstance> readOnlyList2 = BuildDeckFromIds(cardIds);
			readOnlyList = readOnlyList2;
		}
		IReadOnlyList<CardInstance> deck = readOnlyList;
		return StartCombatWithOptions(seed, reactionMode, enemyId, new CombatOptions
		{
			Deck = deck,
			LeaderId = leaderId
		});
	}

	public static List<CardInstance> BuildDeckFromIds(IReadOnlyList<string> ids)
	{
		List<CardInstance> outp = new List<CardInstance>(ids.Count);
		for (int i = 0; i < ids.Count; i++)
		{
			string id = ids[i];
			CardDef def = ((id.StartsWith("fused_") || id.StartsWith("fusion_")) ? Fusion.ResolveFusedDef(id) : Content.GetCardDef(id));
			if (def == null)
			{
				throw new InvalidOperationException("未定義カード: " + id);
			}
			outp.Add(new CardInstance
			{
				Uid = $"{id}#{i}",
				Def = def
			});
		}
		return outp;
	}

	private static GameState TickCardTimers(GameState state)
	{
		int total = state.Player.CardsPlayedTotal;
		GameState s = state;
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			EnemyState e = s.Enemies[i];
			if (e.Hp <= 0)
			{
				continue;
			}
			EnemyDef def = Content.GetEnemyDef(e.EnemyId);
			int? every = def.EnrageEveryCards;
			if (every.HasValue && !(every <= 0) && total != 0 && total % every.Value == 0)
			{
				int amount = def.Enrage.GetValueOrDefault();
				if (amount > 0)
				{
					s = Effects.GainEnemyStrength(s, i, amount);
					s = Events.Emit(s, new GameEvent_StrengthGained
					{
						EnemyIndex = i,
						Amount = amount,
						Reason = "enrage-cards"
					});
				}
			}
		}
		return s;
	}

	private static GameState DeclareIntents(GameState state)
	{
		GameState s = state;
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			if (s.Enemies[i].Hp > 0 && !(s.Enemies[i].Intent != null))
			{
				s = DeclareOne(s, i);
			}
		}
		return s;
	}

	internal static GameState DeclareOne(GameState state, int i)
	{
		GameState s = state;
		EnemyState enemyState = s.Enemies[i];
		EnemyDef def = Content.GetEnemyDef(enemyState.EnemyId);
		EnemyState enemy = enemyState with
		{
			Strength = Effects.EffectiveStrength(s, i)
		};
		EnemyMove fleeMove = null;
		for (int k = 0; k < def.Moves.Count; k++)
		{
			if (def.Moves[k].Kind == "flee")
			{
				fleeMove = def.Moves[k];
				break;
			}
		}
		if (fleeMove == null)
		{
			fleeMove = new EnemyMove
			{
				Id = "forced_flee",
				Kind = "flee"
			};
		}
		EnemyMove biteMove = null;
		if (def.Burrow != null)
		{
			for (int j = 0; j < def.Moves.Count; j++)
			{
				if (def.Moves[j].Id == def.Burrow.Bite)
				{
					biteMove = def.Moves[j];
					break;
				}
			}
		}
		if (enemy.BiteNext == true && biteMove != null)
		{
			(EnemyIntent, RngState) tuple = BuildIntent(s.Rng, biteMove, enemy.Strength, enemy.AtkScale ?? 1.0);
			EnemyIntent biteIntent = tuple.Item1;
			RngState rngB = tuple.Item2;
			List<EnemyState> enemiesB = MapIdx(s.Enemies, (EnemyState e, int num) => (num != i) ? e : e with
			{
				Intent = biteIntent,
				BiteNext = false,
				IntentMoveId = null,
				IntentNode = null
			});
			return Events.Emit(s with
			{
				Rng = rngB,
				Enemies = enemiesB
			}, new GameEvent_EnemyIntentDeclared
			{
				EnemyIndex = i,
				Intent = biteIntent,
				PresentationMove = CaptureEnemyMoveDeclaration(s, i, "forced-bite", biteMove.Id)
			});
		}
		if (enemy.StaggeredNext == true)
		{
			EnemyMove staggerMove = new EnemyMove
			{
				Id = "stagger",
				Kind = "rest"
			};
			(EnemyIntent, RngState) tuple = BuildIntent(s.Rng, staggerMove, enemy.Strength, enemy.AtkScale ?? 1.0);
			EnemyIntent restIntent = tuple.Item1;
			RngState rngS = tuple.Item2;
			List<EnemyState> enemiesS = MapIdx(s.Enemies, (EnemyState e, int num) => (num != i) ? e : e with
			{
				Intent = restIntent,
				StaggeredNext = false,
				IntentMoveId = null,
				IntentNode = null
			});
			return Events.Emit(s with
			{
				Rng = rngS,
				Enemies = enemiesS
			}, new GameEvent_EnemyIntentDeclared
			{
				EnemyIndex = i,
				Intent = restIntent,
				PresentationMove = CaptureEnemyMoveDeclaration(s, i, "stagger")
			});
		}
		if (enemy.StolenGold.GetValueOrDefault() > 0 && (enemy.Intent?.Kind ?? enemy.PrevIntentKind) != "flee")
		{
			(EnemyIntent, RngState) tuple = BuildIntent(s.Rng, fleeMove, enemy.Strength, enemy.AtkScale ?? 1.0);
			EnemyIntent fleeIntent = tuple.Item1;
			RngState rngF = tuple.Item2;
			List<EnemyState> enemies2 = MapIdx(s.Enemies, (EnemyState e, int num) => (num != i) ? e : e with
			{
				Intent = fleeIntent,
				IntentMoveId = null,
				IntentNode = null
			});
			return Events.Emit(s with
			{
				Rng = rngF,
				Enemies = enemies2
			}, new GameEvent_EnemyIntentDeclared
			{
				EnemyIndex = i,
				Intent = fleeIntent,
				PresentationMove = CaptureEnemyMoveDeclaration(s, i, "forced-flee", def.Moves.Any((EnemyMove m) => (object)m == fleeMove) ? fleeMove.Id : null)
			});
		}
		EnemyGraph.InterruptResult jumped = ((enemy.InterruptBlocked == true) ? new EnemyGraph.InterruptResult(enemy.Node, enemy.FiredInterrupts ?? new List<int>(), new List<int>()) : EnemyGraph.ApplyInterruptsTo(s, i, enemy.Node, enemy.FiredInterrupts));
		IReadOnlyList<EnemyRandomArm> vsSet = ((enemy.NoReactTable != true && def.MovesVsSet != null && def.MovesVsSet.Count > 0) ? def.MovesVsSet : null);
		IReadOnlyList<EnemyRandomArm> vsTokens = ((enemy.NoReactTable != true && def.MovesVsTokens != null && def.MovesVsTokens.Count > 0) ? def.MovesVsTokens : null);
		IReadOnlyList<EnemyRandomArm> reactTable = ((s.Player.SetCards.Count == 0 && Effects.HasHuntableTokens(s)) ? (vsTokens ?? vsSet) : (vsSet ?? vsTokens));
		string conditionalOn;
		if (reactTable == null)
		{
			conditionalOn = null;
		}
		else
		{
			conditionalOn = ((reactTable == vsSet) ? "set" : "tokens");
		}
		EnemyGraph.WalkResult walked = EnemyGraph.WalkToMove(s, i, jumped.Cursor, s.Rng);
		RngState rng = walked.Rng;
		EnemyMove move = walked.Move;
		string nextCursor = def.Nodes[walked.NodeId].Next ?? walked.NodeId;
		int usesSoFar = 0;
		if (enemy.MoveUses != null)
		{
			enemy.MoveUses.TryGetValue(move.Id, out usesSoFar);
		}
		(EnemyIntent Intent, RngState Rng) tuple2 = BuildIntent(rng, move, enemy.Strength, enemy.AtkScale ?? 1.0, usesSoFar, s.Player.Hp);
		EnemyIntent intentRaw = tuple2.Intent;
		RngState item = tuple2.Rng;
		EnemyIntent intent;
		if (enemy.BurrowActive == true)
		{
			if (intentRaw.Kind == "defend")
			{
				intent = intentRaw with
				{
					Kind = "rest",
					Actual = 0,
					AlsoBuff = null
				};
			}
			else
			{
				intent = (intentRaw.AlsoDefend.HasValue ? intentRaw with
				{
					AlsoDefend = null
				} : intentRaw);
			}
		}
		else
		{
			intent = intentRaw;
		}
		rng = item;
		Dictionary<string, int> nextUses = ((enemy.MoveUses != null) ? new Dictionary<string, int>((IDictionary<string, int>)enemy.MoveUses) : new Dictionary<string, int>());
		nextUses[move.Id] = usesSoFar + 1;
		EnemyIntentBranch alt = null;
		string alternateMoveId = null;
		if (reactTable != null && reactTable.Count > 0)
		{
			List<double> weightsA = new List<double>(reactTable.Count);
			for (int k2 = 0; k2 < reactTable.Count; k2++)
			{
				weightsA.Add(reactTable[k2].Weight);
			}
			(int Index, RngState Next) tuple3 = Rng.WeightedIndex(rng, weightsA);
			int altIdx = tuple3.Index;
			rng = tuple3.Next;
			EnemyMove altMove = EnemyGraph.MoveById(def, reactTable[altIdx].To);
			alternateMoveId = altMove.Id;
			if (altMove.Id == move.Id)
			{
				alt = ToBranch(intent);
			}
			else
			{
				(EnemyIntent Intent, RngState Rng) tuple4 = BuildIntent(rng, altMove, enemy.Strength, enemy.AtkScale ?? 1.0);
				EnemyIntent altIntent = tuple4.Intent;
				rng = tuple4.Rng;
				alt = ToBranch(altIntent);
			}
		}
		EnemyIntent declaredRaw = ((conditionalOn != null && alt != null) ? intent with
		{
			ConditionalOn = conditionalOn,
			Alt = alt
		} : intent);
		int? stealCap = null;
		if (declaredRaw.Kind == "steal-gold" && s.GoldAvailable.HasValue)
		{
			int alreadyStolen = 0;
			for (int k3 = 0; k3 < s.Enemies.Count; k3++)
			{
				alreadyStolen += s.Enemies[k3].StolenGold.GetValueOrDefault();
			}
			stealCap = Math.Max(0, s.GoldAvailable.Value - alreadyStolen);
		}
		EnemyIntent declared = ((declaredRaw.Kind == "steal-gold" && stealCap.HasValue && declaredRaw.Actual > stealCap.Value) ? declaredRaw with
		{
			Actual = stealCap.Value
		} : declaredRaw);
		int stolen = ((declared.Kind == "steal-gold") ? declared.Actual : 0);
		EnemyMove declaredMove = move;
		Dictionary<string, int> nextUsesLocal = nextUses;
		string nextCursorLocal = nextCursor;
		List<EnemyState> enemies3 = MapIdx(s.Enemies, (EnemyState e, int num) =>
		{
			if (num != i)
			{
				return e;
			}
			List<string> list = new List<string> { declaredMove.Id };
			if (e.LastMoves != null)
			{
				for (int l = 0; l < e.LastMoves.Count; l++)
				{
					if (list.Count >= 3)
					{
						break;
					}
					list.Add(e.LastMoves[l]);
				}
			}
			EnemyState enemyState2 = e with
			{
				Intent = declared,
				IntentMoveId = declaredMove.Id,
				IntentNode = walked.NodeId,
				Node = nextCursorLocal,
				LastMoves = list,
				MoveUses = nextUsesLocal
			};
			if (jumped.FiredNow.Count > 0)
			{
				enemyState2 = enemyState2 with
				{
					FiredInterrupts = jumped.Fired
				};
			}
			if (walked.OnceMoveIds.Count > 0)
			{
				List<string> list2 = ((e.UsedOnce != null) ? new List<string>(e.UsedOnce) : new List<string>());
				list2.AddRange(walked.OnceMoveIds);
				enemyState2 = enemyState2 with
				{
					UsedOnce = list2
				};
			}
			if (stolen > 0)
			{
				enemyState2 = enemyState2 with
				{
					StolenGold = e.StolenGold.GetValueOrDefault() + stolen
				};
			}
			return enemyState2;
		});
		s = Events.Emit(s with
		{
			Rng = rng,
			Enemies = enemies3
		}, new GameEvent_EnemyIntentDeclared
		{
			EnemyIndex = i,
			Intent = declared,
			PresentationMove = CaptureEnemyMoveDeclaration(s, i, "graph", move.Id, alternateMoveId, walked.NodeId)
		});
		if (stolen > 0)
		{
			s = Events.Emit(s, new GameEvent_GoldStolen
			{
				EnemyIndex = i,
				Amount = stolen
			});
		}
		return s;
	}

	private static EnemyIntentBranch ToBranch(EnemyIntent it)
	{
		return new EnemyIntentBranch
		{
			Kind = it.Kind,
			Actual = it.Actual,
			Base = it.Base,
			Hits = it.Hits,
			Inflict = it.Inflict,
			AlsoDefend = it.AlsoDefend,
			AlsoBuff = it.AlsoBuff,
			AlsoDestroySet = it.AlsoDestroySet
		};
	}

	private static (EnemyIntent Intent, RngState Rng) BuildIntent(RngState rng, EnemyMove move, int strength, double atkScale = 1.0, int uses = 0, int? playerHp = null)
	{
		int grow = move.GrowPerUse.GetValueOrDefault() * uses;
		int growHits = move.GrowHitsPerUse.GetValueOrDefault() * uses;
		int? gMin = (move.Min.HasValue ? new int?(move.Min.Value + grow) : ((int?)null));
		int? gMax = (move.Max.HasValue ? new int?(move.Max.Value + grow) : ((int?)null));
		int? gHits = ((move.Hits.HasValue || growHits > 0) ? new int?((move.Hits ?? 1) + growHits) : ((int?)null));
		int actual = 0;
		RngState next = rng;
		if (gMin.HasValue && gMax.HasValue)
		{
			(actual, next) = Rng.NextInt(rng, gMin.Value, gMax.Value);
		}
		if (move.Kind == "summon")
		{
			actual = move.Summon?.Count ?? 0;
		}
		if (move.DamageFromPlayerHp != null && playerHp.HasValue)
		{
			actual = Math.Max(0, playerHp.Value) / move.DamageFromPlayerHp.Divisor + move.DamageFromPlayerHp.Add;
		}
		int bonus = ((move.Kind == "attack") ? strength : 0);
		Func<int, int> scale = (int v) => (!(move.Kind == "attack")) ? v : JsRound((double)v * atkScale);
		Func<int, int> clamp = (int v) => (!(move.Kind == "attack")) ? v : Math.Max(1, v);
		return (Intent: new EnemyIntent
		{
			Kind = move.Kind,
			Actual = clamp(scale(actual) + bonus),
			Base = ((move.Kind == "attack") ? new int?(scale(actual)) : ((int?)null)),
			Hits = gHits,
			MirrorHits = ((move.MirrorHits == true) ? new bool?(true) : ((bool?)null)),
			Inflict = move.Inflict,
			AlsoDefend = move.AlsoDefend,
			AlsoBuff = move.AlsoBuff,
			AlsoDestroySet = ((move.AlsoDestroySet == true) ? new bool?(true) : ((bool?)null)),
			StrengthPerMilled = move.StrengthPerMilled
		}, Rng: next);
	}

	private static GameState StartPlayerTurn(GameState state, int turn)
	{
		int? nextTurnDraw = state.NextTurnDraw;
		int? nextTurnEnergy = state.NextTurnEnergy;
		int? nextTurnBlock = state.NextTurnBlock;
		int? nextTurnLight = state.NextTurnLight;
		bool carryOnce = state.EnergyCarryThisTurn == true;
		GameState s = state with
		{
			Turn = turn,
			Phase = "player-turn",
			NextTurnDraw = null,
			EnemyPhase = null,
			GearUsedThisTurn = null,
			RetainHandThisTurn = null,
			EnergyCarryThisTurn = null,
			NullifyNextAttack = null,
			MaxHpLossFiredThisTurn = null,
			NextTurnEnergy = null,
			NextTurnBlock = null,
			NextTurnLight = null,
			BellLocked = null,
			Player = state.Player with
			{
				Block = (state.BlockKeep.HasValue ? Math.Min(state.Player.Block, state.BlockKeep.Value) : 0),
				Energy = state.Player.EnergyMax + ((((state.EnergyCarry == true) | carryOnce) && turn > 1) ? state.Player.Energy : 0) + nextTurnEnergy.GetValueOrDefault(),
				EnergyMaxAtTurnStart = state.Player.EnergyMax + state.EnergyMaxRefBonus.GetValueOrDefault(),
				CardsPlayedThisTurn = 0,
				SetsThisTurn = 0,
				PlaysThisTurn = 0,
				AttacksPlayedThisTurn = 0,
				HealsThisTurn = 0,
				WeakFreshThisPhase = 0,
				HpLostThisTurn = 0,
				Permanents = MapIdx(state.Player.Permanents, (CardInstance p, int _) => (p.TurnTriggerCounts != null) ? p with
				{
					TurnTriggerCounts = null
				} : p)
			}
		};
		s = s with
		{
			Enemies = MapIdx(s.Enemies, (EnemyState e, int _) => (e.DamageThisTurn.GetValueOrDefault() <= 0) ? e : e with
			{
				DamageThisTurn = 0
			})
		};
		s = s with
		{
			Enemies = MapIdx(s.Enemies, (EnemyState e, int _) =>
			{
				if (e.Intent == null && e.IntentMoveId == null && e.IntentNode == null)
				{
					return e;
				}
				EnemyState enemyState = e with
				{
					Intent = null,
					IntentMoveId = null,
					IntentNode = null
				};
				return (!(e.Intent != null)) ? enemyState : enemyState with
				{
					PrevIntentKind = e.Intent.Kind
				};
			})
		};
		s = Events.Emit(s, new GameEvent_TurnStarted
		{
			Turn = turn,
			Hand = s.Player.Hand.Select((CardInstance c) => c.Def.Name).ToList()
		});
		s = Effects.DrawCards(s, Math.Max(0, ((s.Player.Mist.GetValueOrDefault() > 0) ? Math.Max(3, s.Player.DrawPerTurn - 2) : s.Player.DrawPerTurn) + nextTurnDraw.GetValueOrDefault()));
		if (nextTurnBlock.GetValueOrDefault() > 0)
		{
			s = Effects.GainPlayerBlock(s, nextTurnBlock.GetValueOrDefault(), FirstAliveOrZero(s));
		}
		if (nextTurnLight.GetValueOrDefault() > 0)
		{
			s = Effects.GainLight(s, nextTurnLight.GetValueOrDefault(), "card", FirstAliveOrZero(s));
		}
		s = Effects.RunPermanentTriggers(s, "onTurnStart", FirstAliveOrZero(s));
		s = CheckCombatEnd(s);
		if (s.Phase == "won" || s.Phase == "lost")
		{
			return s;
		}
		return DeclareIntents(s);
	}

	private static GameState ProcessSplits(GameState state)
	{
		GameState s = state;
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			EnemyState e = s.Enemies[i];
			if (e.Hp > 0 || e.Split == true || e.Fled == true)
			{
				continue;
			}
			EnemyDefSplitInto splitInto = Content.GetEnemyDef(e.EnemyId).SplitInto;
			if (splitInto == null)
			{
				continue;
			}
			if (e.SummonBlocked == true && splitInto.Count > 1)
			{
				s = WithEnemy(s, i, (EnemyState x) => x with
				{
					Split = true,
					SummonBlocked = false
				});
				continue;
			}
			s = WithEnemy(s, i, (EnemyState x) => x with
			{
				Split = true
			});
			s = Events.Emit(s, new GameEvent_EnemySplit
			{
				EnemyIndex = i,
				Into = splitInto.EnemyId,
				Count = splitInto.Count
			});
			s = SpawnEnemies(s, i, splitInto.EnemyId, splitInto.Count, splitInto.Stunned, splitInto.Strength, int.MaxValue);
		}
		return s;
	}

	private static GameState SpawnEnemies(GameState state, int parentIndex, string enemyId, int count, bool? stunnedFlag, int? strength, int cap)
	{
		GameState s = state;
		EnemyState parent = s.Enemies[parentIndex];
		EnemyDef parentDef = Content.GetEnemyDef(parent.EnemyId);
		EnemyDef childDef = Content.GetEnemyDef(enemyId);
		double hpRatio = ((parentDef.MaxHp > 0) ? ((double)parent.MaxHp / (double)parentDef.MaxHp) : 1.0);
		int scaledChildHp = Math.Max(1, JsRound((double)childDef.MaxHp * hpRatio));
		for (int k = 0; k < count; k++)
		{
			int alive = 0;
			for (int q = 0; q < s.Enemies.Count; q++)
			{
				if (s.Enemies[q].Hp > 0)
				{
					alive++;
				}
			}
			if (alive >= cap)
			{
				break;
			}
			int childStrength = strength.GetValueOrDefault();
			bool stunned = stunnedFlag == true;
			EnemyState child = new EnemyState
			{
				EnemyId = enemyId,
				Hp = scaledChildHp,
				MaxHp = scaledChildHp,
				Block = (childDef.Burrow?.Block ?? childDef.StartingBlock.GetValueOrDefault()),
				BurrowActive = ((childDef.Burrow != null) ? new bool?(true) : ((bool?)null)),
				Intent = (stunned ? new EnemyIntent
				{
					Kind = "rest",
					Actual = 0
				} : null),
				Strength = childStrength,
				AtkScale = parent.AtkScale,
				Burn = 0,
				Confusion = 0,
				Exposed = 0,
				Node = (stunned ? childDef.Start : EnemyGraph.StartNodeFor(childDef, k)),
				Thorns = childDef.Thorns,
				Artifact = childDef.Artifact,
				Armor = childDef.Armor,
				Slippery = childDef.Slippery
			};
			s = s with
			{
				Enemies = Append(s.Enemies, child)
			};
			if (stunned)
			{
				s = Events.Emit(s, new GameEvent_EnemyIntentDeclared
				{
					EnemyIndex = s.Enemies.Count - 1,
					Intent = child.Intent,
					PresentationMove = CaptureEnemyMoveDeclaration(s, s.Enemies.Count - 1, "spawn-rest")
				});
			}
			else if (s.EnemyPhase != true)
			{
				s = DeclareOne(s, s.Enemies.Count - 1);
			}
		}
		return s;
	}

	private static GameState ProcessUnseal(GameState state)
	{
		GameState s = state;
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			EnemyState e = s.Enemies[i];
			if (e.Hp > 0 || e.Sealed == null || e.Sealed.Count == 0)
			{
				continue;
			}
			List<CardInstance> cards = e.Sealed.ToList();
			s = WithEnemy(s, i, (EnemyState x) => x with
			{
				Sealed = new List<CardInstance>()
			});
			s = s with
			{
				Player = s.Player with
				{
					Hand = s.Player.Hand.Concat(cards).ToList()
				}
			};
			foreach (CardInstance c in cards)
			{
				s = Events.Emit(s, new GameEvent_CardUnsealed
				{
					EnemyIndex = i,
					CardId = c.Def.Id
				});
			}
		}
		return s;
	}

	private static int SealRarityRank(CardInstance c)
	{
		if (!(c.Def.Rarity == "rare"))
		{
			if (!(c.Def.Rarity == "uncommon"))
			{
				return (c.Def.Rarity == "common") ? 1 : 0;
			}
			return 2;
		}
		return 3;
	}

	private static GameState ProcessMourning(GameState state)
	{
		GameState s = state;
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			EnemyState dead = s.Enemies[i];
			if (dead.Hp > 0 || dead.Fled == true || dead.MournProcessed == true)
			{
				continue;
			}
			s = WithEnemy(s, i, (EnemyState x) => x with
			{
				MournProcessed = true
			});
			for (int j = 0; j < s.Enemies.Count; j++)
			{
				if (j != i && s.Enemies[j].Hp > 0)
				{
					int? amount = Content.GetEnemyDef(s.Enemies[j].EnemyId).MournStrength;
					if (amount.HasValue && !(amount <= 0))
					{
						int amt = amount.Value;
						s = Effects.GainEnemyStrength(s, j, amt);
						s = Events.Emit(s, new GameEvent_StrengthGained
						{
							EnemyIndex = j,
							Amount = amt,
							Reason = "mourn"
						});
					}
				}
			}
		}
		return s;
	}

	public static GameState ApplyPendingBites(GameState state)
	{
		if (state.Phase != "player-turn" || state.EnemyPhase == true)
		{
			return state;
		}
		GameState s = state;
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			EnemyState e = s.Enemies[i];
			if (e.Hp <= 0 || e.BiteNext != true)
			{
				continue;
			}
			EnemyDef def = Content.GetEnemyDef(e.EnemyId);
			EnemyMove bite = null;
			if (def.Burrow != null)
			{
				for (int k = 0; k < def.Moves.Count; k++)
				{
					if (def.Moves[k].Id == def.Burrow.Bite)
					{
						bite = def.Moves[k];
						break;
					}
				}
			}
			if (!(bite == null))
			{
				(EnemyIntent, RngState) tuple = BuildIntent(s.Rng, bite, e.Strength, e.AtkScale ?? 1.0);
				EnemyIntent intent = tuple.Item1;
				RngState rng = tuple.Item2;
				s = s with
				{
					Rng = rng,
					Enemies = MapIdx(s.Enemies, (EnemyState x, int j) => (j != i) ? x : x with
					{
						Intent = intent,
						BiteNext = false
					})
				};
				s = Events.Emit(s, new GameEvent_EnemyIntentDeclared
				{
					EnemyIndex = i,
					Intent = intent,
					PresentationMove = CaptureEnemyMoveDeclaration(s, i, "forced-bite", bite.Id)
				});
			}
		}
		return s;
	}

	public static GameState CheckCombatEnd(GameState state)
	{
		state = ApplyPendingBites(state);
		if (state.Phase == "won" || state.Phase == "lost")
		{
			return state;
		}
		state = ProcessSplits(state);
		state = ProcessMourning(state);
		state = ProcessUnseal(state);
		state = Effects.ApplyDeathInterrupts(state);
		state = Effects.RefreshIntentValues(state);
		if (state.Player.Hp <= 0)
		{
			if (state.GearDeathSave == true)
			{
				GameState next = state with
				{
					GearDeathSave = false,
					Player = state.Player with
					{
						Hp = 1
					}
				};
				state = Events.Emit(next, new GameEvent_DeathSaved
				{
					Hp = 1,
					Source = "gear",
					PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, next.Player)
				});
			}
			else
			{
				if (state.DeathSave != true || state.DeathSaveUsed == true)
				{
					return Events.Emit(state with
					{
						Phase = "lost"
					}, new GameEvent_CombatEnded
					{
						Result = "lost"
					});
				}
				int hp = Math.Max(1, (int)Math.Floor((double)state.Player.MaxHp / 2.0));
				GameState next2 = state with
				{
					DeathSaveUsed = true,
					Player = state.Player with
					{
						Hp = hp
					}
				};
				state = Events.Emit(next2, new GameEvent_DeathSaved
				{
					Hp = hp,
					Source = "relic",
					PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, next2.Player)
				});
			}
		}
		bool allDead = true;
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			if (state.Enemies[i].Hp > 0)
			{
				allDead = false;
				break;
			}
		}
		if (allDead)
		{
			return Events.Emit(state with
			{
				Phase = "won"
			}, new GameEvent_CombatEnded
			{
				Result = "won"
			});
		}
		return state;
	}

	public static IReadOnlyList<CardInstance> DeckChoosePool(GameState state, CardDef def)
	{
		switch (DeckChooseKindOf(def))
		{
		case "retrieveFromDiscard":
			return state.Player.DiscardPile;
		case "searchDeck":
		{
			string only = def.Effects.FirstOrDefault((DeclarativeEffect e) => e.Effect == "searchDeck")?.CardType;
			if (only != null)
			{
				return state.Player.DrawPile.Where((CardInstance c) => c.Def.Type == only).ToList();
			}
			return state.Player.DrawPile;
		}
		case "transformDeckToToken":
			return state.Player.DrawPile;
		default:
			return Concat(state.Player.DrawPile, state.Player.DiscardPile);
		}
	}

	public static string DeckChooseKindOf(CardDef def)
	{
		string[] array = new string[4] { "exhaustFromDeckChoose", "retrieveFromDiscard", "searchDeck", "transformDeckToToken" };
		foreach (string k in array)
		{
			for (int j = 0; j < def.Effects.Count; j++)
			{
				DeclarativeEffect e = def.Effects[j];
				if (e.Effect == k && e.Trigger == "onPlay")
				{
					return k;
				}
			}
		}
		return null;
	}

	public static GameState PlayCard(GameState state, string cardUid, int? modeIndex = null, IReadOnlyList<string> discardUids = null, int? targetIndex = null, IReadOnlyList<string> exhaustUids = null, string retrieveUid = null, IReadOnlyList<string> deckUids = null, IReadOnlyList<string> handUids = null, int? xAmount = null, string permanentUid = null)
	{
		if (state.Phase != "player-turn")
		{
			throw new InvalidOperationException("自ターン以外はカードをプレイできない");
		}
		CardInstance card = null;
		for (int i = 0; i < state.Player.Hand.Count; i++)
		{
			if (state.Player.Hand[i].Uid == cardUid)
			{
				card = state.Player.Hand[i];
				break;
			}
		}
		if (card == null)
		{
			throw new InvalidOperationException("手札にないカード: " + cardUid);
		}
		if (!Effects.IsPlayableFromHand(card, state))
		{
			throw new InvalidOperationException(card.Def.Name + " はプレイ不可 (リアクション専用)");
		}
		if (!Effects.RetainerRequirementMet(state, card))
		{
			throw new InvalidOperationException(card.Def.Name + " は場に人形が1体以上いる時だけプレイできる");
		}
		AssertPlayCap(state);
		int cost = Effects.EffectiveCost(state, card);
		bool consumesDiscount = card.Def.Cost > 0 && state.Player.NextCardDiscount > 0 && card.Def.XCost != true && card.FreeThisCombat != true;
		if (cost > state.Player.Energy)
		{
			throw new InvalidOperationException("エナジー不足: " + card.Def.Name);
		}
		int lightCost = card.Def.LightCost.GetValueOrDefault();
		if (lightCost > state.Player.Light.GetValueOrDefault())
		{
			throw new InvalidOperationException($"灯が足りない: {card.Def.Name} (灯{lightCost}が要る)");
		}
		// X is committed from the live state: all currently available energy.
		// Keep the nullable command field for old saves/replays, but it cannot
		// request a partial payment under the current rule.
		int paidX = card.Def.XCost == true ? Math.Max(0, state.Player.Energy) : 0;
		int effX = card.Def.XCost == true ? Math.Max(0, paidX + state.XBonus.GetValueOrDefault() + card.Def.XBonus.GetValueOrDefault()) : 0;
		CardInstance effCard = card.Def.XCost == true ? ExpandXEffects(card, effX) : card;
		if (state.BrandsPlayable == true && Effects.IsBrandCard(card))
		{
			effCard = effCard with
			{
				Def = effCard.Def with
				{
					Effects = new List<DeclarativeEffect>
					{
						new DeclarativeEffect
						{
							Trigger = "onPlay",
							Effect = "loseHp",
							Amount = 1
						}
					},
					Exhaust = true
				}
			};
		}
		if (card.Def.ShivToken == true)
		{
			int shivBonus = 0;
			for (int l = 0; l < state.Player.Permanents.Count; l++)
			{
				CardInstance p = state.Player.Permanents[l];
				for (int m = 0; m < p.Def.Effects.Count; m++)
				{
					DeclarativeEffect e2 = p.Def.Effects[m];
					if (e2.Effect == "empowerShivs")
					{
						shivBonus += e2.Amount.GetValueOrDefault();
					}
				}
			}
			if (shivBonus > 0)
			{
				List<DeclarativeEffect> boosted = new List<DeclarativeEffect>();
				for (int n = 0; n < effCard.Def.Effects.Count; n++)
				{
					DeclarativeEffect e3 = effCard.Def.Effects[n];
					boosted.Add((e3.Effect == "dealDamage") ? e3 with
					{
						Amount = e3.Amount.GetValueOrDefault() + shivBonus
					} : e3);
				}
				effCard = effCard with
				{
					Def = effCard.Def with
					{
						Effects = boosted
					}
				};
			}
		}
		if (card.GrowBonus.GetValueOrDefault() > 0)
		{
			int g = card.GrowBonus.GetValueOrDefault();
			List<DeclarativeEffect> grown = new List<DeclarativeEffect>();
			for (int num = 0; num < effCard.Def.Effects.Count; num++)
			{
				DeclarativeEffect e4 = effCard.Def.Effects[num];
				grown.Add((e4.Effect == "dealDamage" && e4.Trigger == "onPlay") ? e4 with
				{
					Amount = e4.Amount.GetValueOrDefault() + g
				} : e4);
			}
			effCard = effCard with
			{
				Def = effCard.Def with
				{
					Effects = grown
				}
			};
		}
		IReadOnlyList<CardMode> modes = card.Def.Modes ?? new List<CardMode>();
		CardMode chosenMode = null;
		if (modes.Count > 0)
		{
			if (!modeIndex.HasValue || modeIndex.Value < 0 || modeIndex.Value >= modes.Count)
			{
				throw new InvalidOperationException($"{card.Def.Name} は選択式: modeIndex (0〜{modes.Count - 1}) が必要");
			}
			chosenMode = modes[modeIndex.Value];
		}
		int discardCost = card.Def.DiscardCost.GetValueOrDefault();
		IReadOnlyList<string> discards = discardUids ?? new List<string>();
		if (discardCost > 0)
		{
			if (discards.Count != discardCost)
			{
				throw new InvalidOperationException($"{card.Def.Name} は追加コストとして手札{discardCost}枚の指定が必要");
			}
			if (new HashSet<string>(discards).Count != discards.Count || discards.Contains(cardUid))
			{
				throw new InvalidOperationException("捨てるカードの指定が不正 (重複または自分自身)");
			}
			foreach (string uid in discards)
			{
				if (!state.Player.Hand.Any((CardInstance cardInstance) => cardInstance.Uid == uid))
				{
					throw new InvalidOperationException("手札にないカード: " + uid);
				}
			}
		}
		int impulseCost = ((card.Def.KindleOptional != true || (exhaustUids != null && exhaustUids.Count != 0)) ? card.Def.ImpulseCost.GetValueOrDefault() : 0);
		int exhaustCost = card.Def.ExhaustCost.GetValueOrDefault() + impulseCost;
		IReadOnlyList<string> exhausts = exhaustUids ?? new List<string>();
		if (exhaustCost > 0)
		{
			if (exhausts.Count != exhaustCost)
			{
				throw new InvalidOperationException((impulseCost > 0) ? $"{card.Def.Name} は追加コストとして衝動の札{impulseCost}枚をくべる (消滅させる) 指定が必要" : $"{card.Def.Name} は追加コストとして手札{exhaustCost}枚の消滅指定が必要");
			}
			if (new HashSet<string>(exhausts).Count != exhausts.Count || exhausts.Contains(cardUid) || exhausts.Any((string value) => discards.Contains(value)))
			{
				throw new InvalidOperationException("消滅させるカードの指定が不正 (重複・自分自身・捨てコストとの重複)");
			}
			foreach (string uid2 in exhausts)
			{
				if (!state.Player.Hand.Any((CardInstance cardInstance) => cardInstance.Uid == uid2))
				{
					throw new InvalidOperationException("手札にないカード: " + uid2);
				}
			}
			if (impulseCost > 0 && exhausts.Count((string value) => state.Player.ImpulseUids.Contains(value)) < impulseCost)
			{
				throw new InvalidOperationException(card.Def.Name + " にくべられるのは衝動の札だけ");
			}
		}
		bool isRetrieve = card.Def.Effects.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "retrieveFromExhaust");
		bool isPlayFromExhaust = card.Def.Effects.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "playFromExhaust");
		CardInstance chosenFromExhaust = null;
		if (isRetrieve | isPlayFromExhaust)
		{
			if (retrieveUid == null)
			{
				throw new InvalidOperationException(card.Def.Name + " は消滅置き場のカード (retrieveUid) の指定が必要");
			}
			chosenFromExhaust = state.Player.ExhaustPile.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == retrieveUid);
			if (chosenFromExhaust == null)
			{
				throw new InvalidOperationException("消滅置き場にないカード: " + retrieveUid);
			}
			if (isPlayFromExhaust)
			{
				if (chosenFromExhaust.Def.Type == "reaction")
				{
					throw new InvalidOperationException("リアクションは直接プレイできない");
				}
				if ((chosenFromExhaust.Def.Modes?.Count ?? 0) > 0)
				{
					throw new InvalidOperationException("選択式カードは直接プレイできない");
				}
				if (chosenFromExhaust.Def.Effects.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "playFromExhaust" || declarativeEffect.Effect == "retrieveFromExhaust"))
				{
					throw new InvalidOperationException("コスト再利用カード自身は直接プレイできない (再帰の禁止)");
				}
			}
		}
		string chooseKind = DeckChooseKindOf(card.Def);
		int deckChooseN = 0;
		for (int i2 = 0; i2 < card.Def.Effects.Count; i2++)
		{
			DeclarativeEffect e5 = card.Def.Effects[i2];
			if (chooseKind != null && e5.Effect == chooseKind)
			{
				deckChooseN += e5.Amount ?? 1;
			}
		}
		IReadOnlyList<CardInstance> deckPool = DeckChoosePool(state, card.Def);
		string text;
		switch (chooseKind)
		{
		default:
			text = "山札か捨て札";
			break;
		case "searchDeck":
		case "transformDeckToToken":
			text = "山札";
			break;
		case "retrieveFromDiscard":
			text = "捨て札";
			break;
		}
		string poolLabel = text;
		object obj;
		if (deckChooseN <= 0)
		{
			IReadOnlyList<string> readOnlyList = new List<string>();
			obj = readOnlyList;
		}
		else
		{
			obj = deckUids ?? new List<string>();
		}
		IReadOnlyList<string> deckChooseUids = (IReadOnlyList<string>)obj;
		if (deckChooseN > 0)
		{
			int need = Math.Min(deckChooseN, deckPool.Count);
			if (deckChooseUids.Count != need)
			{
				throw new InvalidOperationException($"{card.Def.Name} は{poolLabel}から{need}枚の指定 (deckUids) が必要");
			}
			if (new HashSet<string>(deckChooseUids).Count != deckChooseUids.Count)
			{
				throw new InvalidOperationException("deckUids の指定が重複している");
			}
			foreach (string uid3 in deckChooseUids)
			{
				if (!deckPool.Any((CardInstance cardInstance) => cardInstance.Uid == uid3))
				{
					throw new InvalidOperationException(poolLabel + "に無いカード: " + uid3);
				}
			}
		}
		int upgradeN = 0;
		for (int i3 = 0; i3 < card.Def.Effects.Count; i3++)
		{
			DeclarativeEffect e6 = card.Def.Effects[i3];
			if (e6.Effect == "upgradeInHand" && e6.Trigger == "onPlay")
			{
				upgradeN += e6.Amount ?? 1;
			}
		}
		List<CardInstance> upgradable = ((upgradeN > 0) ? state.Player.Hand.Where((CardInstance cardInstance) => cardInstance.Uid != card.Uid && Upgrade.CanUpgradeInHand(cardInstance)).ToList() : new List<CardInstance>());
		object obj2;
		if (upgradeN <= 0)
		{
			IReadOnlyList<string> readOnlyList = new List<string>();
			obj2 = readOnlyList;
		}
		else
		{
			obj2 = handUids ?? new List<string>();
		}
		IReadOnlyList<string> upgradeUids = (IReadOnlyList<string>)obj2;
		if (upgradeN > 0)
		{
			int need2 = Math.Min(upgradeN, upgradable.Count);
			if (upgradeUids.Count != need2)
			{
				throw new InvalidOperationException($"{card.Def.Name} は手札から{need2}枚の指定 (handUids) が必要");
			}
			if (new HashSet<string>(upgradeUids).Count != upgradeUids.Count)
			{
				throw new InvalidOperationException("handUids の指定が重複している");
			}
			foreach (string uid4 in upgradeUids)
			{
				if (!upgradable.Any((CardInstance cardInstance) => cardInstance.Uid == uid4))
				{
					throw new InvalidOperationException("鍛えられる手札に無いカード: " + uid4);
				}
			}
		}
		int num2 = card.Def.Effects.Count((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "sacrificeRetainer" && declarativeEffect.Trigger == "onPlay");
		CardInstance sacrificed = null;
		if (num2 > 0)
		{
			if (permanentUid == null)
			{
				throw new InvalidOperationException(card.Def.Name + " は破壊する人形 (permanentUid) の指定が必要");
			}
			CardInstance t = state.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == permanentUid);
			if (t == null || t.Def.Retainer != true || t.Innate == true)
			{
				throw new InvalidOperationException("人形ではない、または場に無い置物: " + permanentUid);
			}
			sacrificed = t;
		}
		bool choosesDoll = CardChoosesDoll(card.Def);
		if (choosesDoll)
		{
			if (permanentUid == null)
			{
				throw new InvalidOperationException(card.Def.Name + " は人形 (permanentUid) の指定が必要");
			}
			CardInstance t2 = state.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == permanentUid);
			if (t2 == null || !Effects.IsDoll(t2))
			{
				throw new InvalidOperationException("人形ではない、または場に無い置物: " + permanentUid);
			}
		}
		int aliveCount = state.Enemies.Count((EnemyState enemyState) => enemyState.Hp > 0);
		if (targetIndex.HasValue)
		{
			EnemyState target = ((targetIndex.Value >= 0 && targetIndex.Value < state.Enemies.Count) ? state.Enemies[targetIndex.Value] : null);
			if (target == null)
			{
				throw new InvalidOperationException($"不正な対象: {targetIndex} (敵は{state.Enemies.Count}体)");
			}
			if (target.Hp <= 0)
			{
				List<int> alive = new List<int>();
				for (int i4 = 0; i4 < state.Enemies.Count; i4++)
				{
					if (state.Enemies[i4].Hp > 0)
					{
						alive.Add(i4);
					}
				}
				if (alive.Count != 1)
				{
					throw new InvalidOperationException($"対象 {targetIndex} はすでに{((target.Fled == true) ? "逃走" : "倒れて")}いる (targetIndexは撃破済みを含む並び順。生存: {string.Join(",", alive)})");
				}
				targetIndex = alive[0];
			}
		}
		if (!targetIndex.HasValue && aliveCount > 1 && Effects.CardNeedsTarget(card, modeIndex))
		{
			throw new InvalidOperationException(card.Def.Name + " は対象の指定 (targetIndex) が必要");
		}
		int? redirectedFrom = null;
		if (targetIndex.HasValue)
		{
			EnemyState t3 = ((targetIndex.Value >= 0 && targetIndex.Value < state.Enemies.Count) ? state.Enemies[targetIndex.Value] : null);
			if (t3 != null && Content.GetEnemyDef(t3.EnemyId).Guardian != true)
			{
				int g2 = -1;
				for (int i5 = 0; i5 < state.Enemies.Count; i5++)
				{
					if (state.Enemies[i5].Hp > 0 && Content.GetEnemyDef(state.Enemies[i5].EnemyId).Guardian == true)
					{
						g2 = i5;
						break;
					}
				}
				if (g2 >= 0 && g2 != targetIndex.Value)
				{
					redirectedFrom = targetIndex;
					targetIndex = g2;
				}
			}
		}
		int enemyIndex = targetIndex ?? FindAlive(state.Enemies);
		bool isPermanent = card.Def.Type == "permanent";
		bool isExhaust = effCard.Def.Exhaust == true && (card.Def.ExhaustUnlessExposedEnemy != true || !state.Enemies.Any((EnemyState enemyState) => enemyState.Hp > 0 && enemyState.Exposed > 0));
		HashSet<string> removed = new HashSet<string> { cardUid };
		foreach (string u in discards)
		{
			removed.Add(u);
		}
		foreach (string u2 in exhausts)
		{
			removed.Add(u2);
		}
		List<CardInstance> discardedCards = state.Player.Hand.Where((CardInstance cardInstance) => discards.Contains(cardInstance.Uid)).ToList();
		List<CardInstance> exhaustedCards = state.Player.Hand.Where((CardInstance cardInstance) => exhausts.Contains(cardInstance.Uid)).ToList();
		GameState s = state with
		{
			Player = state.Player with
			{
				Energy = state.Player.Energy - ((card.Def.XCost == true) ? paidX : cost),
				Light = state.Player.Light.GetValueOrDefault() - lightCost,
				NextCardDiscount = ((!consumesDiscount) ? state.Player.NextCardDiscount : 0),
				Hand = state.Player.Hand.Where((CardInstance cardInstance) => !removed.Contains(cardInstance.Uid)).ToList(),
				DiscardPile = Concat(state.Player.DiscardPile, discardedCards.Where((CardInstance cardInstance) => cardInstance.Def.Id != Content.SCALD_DEF.Id)),
				Permanents = isPermanent ? Append(state.Player.Permanents, card) : state.Player.Permanents,
				ExhaustPile = Concat(state.Player.ExhaustPile, exhaustedCards),
				ImpulseUids = (impulseCost > 0) ? state.Player.ImpulseUids.Where((string value) => !exhausts.Contains(value)).ToList() : state.Player.ImpulseUids
			}
		};
		if (discardedCards.Count > 0)
		{
			s = Events.Emit(s, new GameEvent_CardsDiscarded
			{
				CardIds = discardedCards.Select((CardInstance cardInstance) => cardInstance.Def.Id).ToList()
			});
		}
		if (impulseCost > 0)
		{
			s = Events.Emit(s, new GameEvent_ImpulseKindled
			{
				CardId = card.Def.Id,
				CardIds = exhaustedCards.Select((CardInstance cardInstance) => cardInstance.Def.Id).ToList()
			});
		}
		s = Events.Emit(s, new GameEvent_CardPlayed
		{
			CardId = card.Def.Id
		});
		if (lightCost > 0)
		{
			s = Events.Emit(s, new GameEvent_LightSpent
			{
				Amount = lightCost,
				CardId = card.Def.Id
			});
		}
		if (lightCost > 0)
		{
			s = Effects.RunPermanentTriggers(s, "onLightDischarged", enemyIndex);
		}
		if (redirectedFrom.HasValue)
		{
			s = Events.Emit(s, new GameEvent_GuardianRedirected
			{
				FromIndex = redirectedFrom.Value,
				ToIndex = enemyIndex
			});
		}
		int cascadeTimes = card.Def.Cascade.GetValueOrDefault() + ((state.Player.PlaysThisTurn.GetValueOrDefault() == 0 && Effects.HasStaticEffect(state, "firstCardCascade")) ? 1 : 0);
		for (int k2 = 0; k2 < cascadeTimes; k2++)
		{
			s = ResolveCascade(s, card.Def, enemyIndex);
		}
		if (cascadeTimes > 0 && enemyIndex >= 0 && enemyIndex < s.Enemies.Count && s.Enemies[enemyIndex].Hp <= 0)
		{
			int to = FindAlive(s.Enemies);
			if (to >= 0)
			{
				s = Events.Emit(s, new GameEvent_CascadeRetargeted
				{
					CardId = card.Def.Id,
					FromIndex = enemyIndex,
					ToIndex = to
				});
				enemyIndex = to;
			}
		}
		if (isPermanent)
		{
			if (lightCost > 0)
			{
				s = s with
				{
					Player = s.Player with
					{
						Light = s.Player.Light.GetValueOrDefault() + lightCost
					}
				};
			}
			s = Effects.EnterPermanent(s, card.Uid, enemyIndex);
			if (lightCost > 0)
			{
				s = s with
				{
					Player = s.Player with
					{
						Light = Math.Max(0, s.Player.Light.GetValueOrDefault() - lightCost)
					}
				};
			}
		}
		if (sacrificed != null)
		{
			CardInstance gone = sacrificed;
			s = s with
			{
				Player = s.Player with
				{
					Permanents = s.Player.Permanents.Where((CardInstance cardInstance) => cardInstance.Uid != gone.Uid).ToList()
				}
			};
			s = Events.Emit(s, new GameEvent_RetainerSacrificed
			{
				CardId = gone.Def.Id,
				Uid = gone.Uid
			});
		}
		foreach (CardInstance paid in exhaustedCards)
		{
			s = Events.Emit(s, new GameEvent_CardExhausted
			{
				CardId = paid.Def.Id
			});
			s = Effects.RunPermanentTriggers(s, "onCostExhausted", enemyIndex);
		}
		s = Effects.FireExhaustTriggers(s, exhaustedCards.Count, enemyIndex);
		s = Effects.FireNecroEffects(s, exhaustedCards, enemyIndex);
		if (deckChooseUids.Count > 0 && chooseKind == "transformDeckToToken")
		{
			string tokId = null;
			for (int i6 = 0; i6 < card.Def.Effects.Count; i6++)
			{
				if (card.Def.Effects[i6].Effect == "transformDeckToToken")
				{
					tokId = card.Def.Effects[i6].SummonId;
					break;
				}
			}
			CardDef tokDef = Content.GetCardDef(tokId ?? "");
			HashSet<string> chosenSet = new HashSet<string>(deckChooseUids);
			int k3 = 0;
			List<string> changed = new List<string>();
			int logLen = s.EventLog.Count;
			List<CardInstance> newDraw = new List<CardInstance>();
			for (int i7 = 0; i7 < s.Player.DrawPile.Count; i7++)
			{
				CardInstance c = s.Player.DrawPile[i7];
				if (!chosenSet.Contains(c.Uid))
				{
					newDraw.Add(c);
					continue;
				}
				changed.Add(c.Def.Id);
				newDraw.Add(new CardInstance
				{
					Uid = $"tokt_{logLen}_{k3++}_{tokDef.Id}",
					Def = tokDef,
					Token = true
				});
			}
			s = s with
			{
				Player = s.Player with
				{
					DrawPile = newDraw
				}
			};
			foreach (string id in changed)
			{
				s = Events.Emit(s, new GameEvent_DeckCardTransformed
				{
					CardId = id,
					Into = tokDef.Id
				});
			}
		}
		if (deckChooseUids.Count > 0 && (chooseKind == "searchDeck" || chooseKind == "retrieveFromDiscard"))
		{
			HashSet<string> chosenSet2 = new HashSet<string>(deckChooseUids);
			bool fromDraw = chooseKind == "searchDeck";
			List<CardInstance> moved = (fromDraw ? s.Player.DrawPile : s.Player.DiscardPile).Where((CardInstance cardInstance) => chosenSet2.Contains(cardInstance.Uid)).ToList();
			s = s with
			{
				Player = s.Player with
				{
					DrawPile = fromDraw ? s.Player.DrawPile.Where((CardInstance cardInstance) => !chosenSet2.Contains(cardInstance.Uid)).ToList() : s.Player.DrawPile,
					DiscardPile = fromDraw ? s.Player.DiscardPile : s.Player.DiscardPile.Where((CardInstance cardInstance) => !chosenSet2.Contains(cardInstance.Uid)).ToList(),
					Hand = Concat(s.Player.Hand, moved)
				}
			};
			s = Events.Emit(s, new GameEvent_CardsMovedToHand
			{
				CardIds = moved.Select((CardInstance cardInstance) => cardInstance.Def.Id).ToList(),
				From = (fromDraw ? "draw" : "discard")
			});
		}
		if (upgradeUids.Count > 0)
		{
			HashSet<string> set = new HashSet<string>(upgradeUids);
			s = s with
			{
				Player = s.Player with
				{
					Hand = s.Player.Hand.Select((CardInstance cardInstance) => (!set.Contains(cardInstance.Uid)) ? cardInstance : Upgrade.UpgradeCard(cardInstance)).ToList()
				}
			};
			foreach (CardInstance c2 in s.Player.Hand.Where((CardInstance cardInstance) => set.Contains(cardInstance.Uid)).ToList())
			{
				s = Events.Emit(s, new GameEvent_CardUpgradedInHand
				{
					CardId = c2.Def.Id
				});
			}
		}
		if (card.Def.Effects.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "upgradeAllInHand" && declarativeEffect.Trigger == "onPlay" && Effects.BlazeConditionMet(s, declarativeEffect, enemyIndex)))
		{
			HashSet<string> all = new HashSet<string>(from cardInstance in s.Player.Hand
				where cardInstance.Uid != card.Uid && Upgrade.CanUpgradeInHand(cardInstance)
				select cardInstance.Uid);
			s = s with
			{
				Player = s.Player with
				{
					Hand = s.Player.Hand.Select((CardInstance cardInstance) => (!all.Contains(cardInstance.Uid)) ? cardInstance : Upgrade.UpgradeCard(cardInstance)).ToList()
				}
			};
			foreach (CardInstance c3 in s.Player.Hand.Where((CardInstance cardInstance) => all.Contains(cardInstance.Uid)).ToList())
			{
				s = Events.Emit(s, new GameEvent_CardUpgradedInHand
				{
					CardId = c3.Def.Id
				});
			}
		}
		if (deckChooseUids.Count > 0 && chooseKind == "exhaustFromDeckChoose")
		{
			HashSet<string> chosenSet3 = new HashSet<string>(deckChooseUids);
			List<CardInstance> chosenCards = (from cardInstance in Concat(s.Player.DrawPile, s.Player.DiscardPile)
				where chosenSet3.Contains(cardInstance.Uid)
				select cardInstance).ToList();
			s = s with
			{
				Player = s.Player with
				{
					DrawPile = s.Player.DrawPile.Where((CardInstance cardInstance) => !chosenSet3.Contains(cardInstance.Uid)).ToList(),
					DiscardPile = s.Player.DiscardPile.Where((CardInstance cardInstance) => !chosenSet3.Contains(cardInstance.Uid)).ToList(),
					ExhaustPile = Concat(s.Player.ExhaustPile, chosenCards)
				}
			};
			foreach (CardInstance c4 in chosenCards)
			{
				s = Events.Emit(s, new GameEvent_CardExhausted
				{
					CardId = c4.Def.Id
				});
			}
			s = Effects.FireExhaustTriggers(s, chosenCards.Count, enemyIndex);
			s = Effects.FireNecroEffects(s, chosenCards, enemyIndex);
		}
		bool echoed = card.Def.Type == "spell" && state.Player.SpellEchoes > 0;
		if (echoed)
		{
			s = s with
			{
				Player = s.Player with
				{
					SpellEchoes = s.Player.SpellEchoes - 1
				}
			};
			s = Events.Emit(s, new GameEvent_SpellEchoed
			{
				CardId = card.Def.Id
			});
		}
		int echoPasses = 1 + (echoed ? 1 : 0) + ((card.Def.Echo == true) ? 1 : 0);
		if (choosesDoll)
		{
			s = s with
			{
				ChosenPermanentUid = permanentUid
			};
		}
		bool? prevKindled = s.ResolvingKindled;
		s = s with
		{
			ResolvingKindled = (impulseCost > 0)
		};
		for (int echoPass = 0; echoPass < echoPasses; echoPass++)
		{
			bool wasBlazing = Effects.IsBlazing(s);
			if (chosenMode != null)
			{
				s = s with
				{
					ResolvingCardPlay = true,
					AngerFiredThisPlay = false
				};
				for (int i8 = 0; i8 < effCard.Def.Effects.Count; i8++)
				{
					DeclarativeEffect effect = effCard.Def.Effects[i8];
					if (effect.Trigger == "onPlay")
					{
						s = Effects.ResolveEffectTargeted(s, effect, enemyIndex, new DamagePresentationSource
						{
							Kind = "card",
							DefinitionId = effCard.Def.Id,
							Uid = effCard.Uid
						});
					}
				}
				for (int i9 = 0; i9 < chosenMode.Effects.Count; i9++)
				{
					s = Effects.ResolveEffectTargeted(s, chosenMode.Effects[i9], enemyIndex, new DamagePresentationSource
					{
						Kind = "card",
						DefinitionId = effCard.Def.Id,
						Uid = effCard.Uid
					});
				}
				s = s with
				{
					ResolvingCardPlay = false
				};
			}
			else
			{
				s = Effects.ResolveOnPlayEffects(s, effCard, enemyIndex);
			}
			s = Effects.ResolveIgniteEffects(s, effCard, enemyIndex, wasBlazing);
		}
		s = s with
		{
			ResolvingKindled = prevKindled
		};
		if (choosesDoll)
		{
			s = s with
			{
				ChosenPermanentUid = null
			};
		}
		List<DeclarativeEffect> resolvedEffects = new List<DeclarativeEffect>();
		for (int i10 = 0; i10 < effCard.Def.Effects.Count; i10++)
		{
			if (effCard.Def.Effects[i10].Trigger == "onPlay")
			{
				resolvedEffects.Add(effCard.Def.Effects[i10]);
			}
		}
		if (chosenMode != null)
		{
			resolvedEffects.AddRange(chosenMode.Effects);
		}
		if (resolvedEffects.Any(Effects.IsDamageEffect))
		{
			s = s with
			{
				Player = s.Player with
				{
					AttacksPlayedThisTurn = s.Player.AttacksPlayedThisTurn.GetValueOrDefault() + 1
				}
			};
			s = Effects.RunPermanentTriggers(s, "onAttackPlayed", enemyIndex);
			s = FireSelfSetTriggers(s, "onAttackPlayed", enemyIndex);
		}
		s = Effects.RunPermanentTriggers(s, "onCardPlayed", enemyIndex);
		if (card.Def.Type == "spell")
		{
			s = FireSelfSetTriggers(s, "onSpellPlayed", enemyIndex);
			s = Effects.RunPermanentTriggers(s, "onSpellPlayed", enemyIndex);
		}
		if (state.Player.ImpulseUids.Contains(cardUid))
		{
			s = Effects.RunPermanentTriggers(s, "onImpulsePlayed", enemyIndex);
		}
		if (card.Def.Effects.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "dealDamageRandom"))
		{
			s = s with
			{
				Player = s.Player with
				{
					RandomPlayedThisCombat = s.Player.RandomPlayedThisCombat + 1
				}
			};
			s = Effects.RunPermanentTriggers(s, "onRandomPlayed", enemyIndex);
		}
		if (card.Def.SparkToken == true)
		{
			s = s with
			{
				Player = s.Player with
				{
					SparksPlayedThisCombat = s.Player.SparksPlayedThisCombat.GetValueOrDefault() + 1
				}
			};
			s = Effects.RunPermanentTriggers(s, "onSparkPlayed", enemyIndex);
		}
		s = s with
		{
			Player = s.Player with
			{
				CardsPlayedThisTurn = s.Player.CardsPlayedThisTurn + 1,
				CardsPlayedTotal = s.Player.CardsPlayedTotal + 1,
				PlaysThisTurn = s.Player.PlaysThisTurn.GetValueOrDefault() + 1
			}
		};
		s = TickCardTimers(s);
		bool hasCardOps = card.Def.Effects.Any((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "addCopyToDiscard" || declarativeEffect.Effect == "growSelf");
		int copies = 0;
		if (hasCardOps)
		{
			for (int i11 = 0; i11 < card.Def.Effects.Count; i11++)
			{
				DeclarativeEffect e7 = card.Def.Effects[i11];
				if (e7.Effect == "addCopyToDiscard" && e7.Trigger == "onPlay")
				{
					copies += e7.Amount ?? 1;
				}
			}
		}
		if (copies > 0)
		{
			List<CardInstance> made = new List<CardInstance>(copies);
			for (int i12 = 0; i12 < copies; i12++)
			{
				made.Add(new CardInstance
				{
					Uid = $"copy_{s.EventLog.Count}_{i12}_{card.Def.Id}",
					Def = card.Def,
					Token = true
				});
			}
			s = s with
			{
				Player = s.Player with
				{
					DiscardPile = Concat(s.Player.DiscardPile, made)
				}
			};
			s = Events.Emit(s, new GameEvent_CardCopied
			{
				CardId = card.Def.Id,
				Count = copies
			});
		}
		int grow = 0;
		if (hasCardOps)
		{
			for (int i13 = 0; i13 < card.Def.Effects.Count; i13++)
			{
				DeclarativeEffect e8 = card.Def.Effects[i13];
				if (e8.Effect == "growSelf" && e8.Trigger == "onPlay")
				{
					grow += e8.Amount.GetValueOrDefault();
				}
			}
		}
		CardInstance landed = ((grow > 0) ? card with
		{
			GrowBonus = card.GrowBonus.GetValueOrDefault() + grow
		} : card);
		if (grow > 0)
		{
			s = Events.Emit(s, new GameEvent_CardGrew
			{
				CardId = card.Def.Id,
				Bonus = landed.GrowBonus.GetValueOrDefault()
			});
		}
		if (isExhaust)
		{
			s = s with
			{
				Player = s.Player with
				{
					ExhaustPile = Append(s.Player.ExhaustPile, landed)
				}
			};
			s = Events.Emit(s, new GameEvent_CardExhausted
			{
				CardId = card.Def.Id
			});
			s = Effects.FireExhaustTriggers(s, 1, enemyIndex);
		}
		else if (!isPermanent)
		{
			s = s with
			{
				Player = s.Player with
				{
					DiscardPile = Append(s.Player.DiscardPile, landed)
				}
			};
		}
		if (isRetrieve && retrieveUid != null)
		{
			CardInstance chosen = s.Player.ExhaustPile.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == retrieveUid);
			if (chosen != null)
			{
				bool free = chosen.Def.XCost != true;
				s = s with
				{
					Player = s.Player with
					{
						ExhaustPile = s.Player.ExhaustPile.Where((CardInstance cardInstance) => cardInstance.Uid != retrieveUid).ToList(),
						Hand = Append(s.Player.Hand, free ? chosen with
						{
							FreeThisCombat = true
						} : chosen)
					}
				};
				s = Events.Emit(s, new GameEvent_CardRetrieved
				{
					CardId = chosen.Def.Id
				});
			}
		}
		if (isPlayFromExhaust && retrieveUid != null)
		{
			CardInstance chosen2 = s.Player.ExhaustPile.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == retrieveUid);
			if (chosen2 != null)
			{
				s = Events.Emit(s, new GameEvent_CardPlayedFromExhaust
				{
					CardId = chosen2.Def.Id
				});
				s = PlayCardDirect(s, chosen2, enemyIndex, "stay");
			}
		}
		return CheckCombatEnd(s);
	}

	private static CardInstance ExpandXEffects(CardInstance card, int repeats)
	{
		var expanded = new List<DeclarativeEffect>();
		foreach (var effect in card.Def.Effects)
		{
			if (effect.XHits != true) expanded.Add(effect);
			else for (int i = 0; i < repeats; i++) expanded.Add(effect with { XHits = null });
		}
		return card with { Def = card.Def with { Effects = expanded } };
	}

	public static GameState PlayCardDirect(GameState state, CardInstance card, int enemyIndex, string placement)
	{
		GameState s = state;
		CardInstance resolvedCard = card;
		if (card.Def.XCost == true)
		{
			int paidX = Math.Max(0, state.Player.Energy);
			int repeats = Math.Max(0, paidX + state.XBonus.GetValueOrDefault() + card.Def.XBonus.GetValueOrDefault());
			resolvedCard = ExpandXEffects(card, repeats);
			// Direct play is explicitly free; use the available energy as X without paying it.
		}

		bool isPermanent = card.Def.Type == "permanent";
		if (isPermanent)
		{
			s = s with
			{
				Player = s.Player with
				{
					ExhaustPile = (placement == "stay") ? s.Player.ExhaustPile.Where((CardInstance c) => c.Uid != card.Uid).ToList() : s.Player.ExhaustPile,
					Permanents = Append(s.Player.Permanents, card)
				}
			};
		}
		s = Events.Emit(s, new GameEvent_CardPlayed
		{
			CardId = card.Def.Id
		});
		for (int k = 0; k < card.Def.Cascade.GetValueOrDefault(); k++)
		{
			s = ResolveCascade(s, card.Def, enemyIndex);
		}
		if (card.Def.Cascade.GetValueOrDefault() > 0 && enemyIndex >= 0 && enemyIndex < s.Enemies.Count && s.Enemies[enemyIndex].Hp <= 0)
		{
			int to = FindAlive(s.Enemies);
			if (to >= 0)
			{
				s = Events.Emit(s, new GameEvent_CascadeRetargeted
				{
					CardId = card.Def.Id,
					FromIndex = enemyIndex,
					ToIndex = to
				});
				enemyIndex = to;
			}
		}
		if (isPermanent)
		{
			s = Effects.EnterPermanent(s, card.Uid, enemyIndex);
		}
		bool wasBlazing = Effects.IsBlazing(s);
		s = Effects.ResolveOnPlayEffects(s, resolvedCard, enemyIndex);
		s = Effects.ResolveIgniteEffects(s, resolvedCard, enemyIndex, wasBlazing);
		if (card.Def.Effects.Where((DeclarativeEffect e) => e.Trigger == "onPlay").Any(Effects.IsDamageEffect))
		{
			s = s with
			{
				Player = s.Player with
				{
					AttacksPlayedThisTurn = s.Player.AttacksPlayedThisTurn.GetValueOrDefault() + 1
				}
			};
			s = Effects.RunPermanentTriggers(s, "onAttackPlayed", enemyIndex);
			s = FireSelfSetTriggers(s, "onAttackPlayed", enemyIndex);
		}
		s = Effects.RunPermanentTriggers(s, "onCardPlayed", enemyIndex);
		if (card.Def.Type == "spell")
		{
			s = FireSelfSetTriggers(s, "onSpellPlayed", enemyIndex);
			s = Effects.RunPermanentTriggers(s, "onSpellPlayed", enemyIndex);
		}
		s = s with
		{
			Player = s.Player with
			{
				CardsPlayedThisTurn = s.Player.CardsPlayedThisTurn + 1,
				CardsPlayedTotal = s.Player.CardsPlayedTotal + 1,
				PlaysThisTurn = s.Player.PlaysThisTurn.GetValueOrDefault() + 1
			}
		};
		s = TickCardTimers(s);
		if (placement == "land" && !isPermanent)
		{
			if (card.Def.Exhaust == true)
			{
				s = s with
				{
					Player = s.Player with
					{
						ExhaustPile = Append(s.Player.ExhaustPile, card)
					}
				};
				s = Events.Emit(s, new GameEvent_CardExhausted
				{
					CardId = card.Def.Id
				});
				s = Effects.FireExhaustTriggers(s, 1, enemyIndex);
			}
			else
			{
				s = s with
				{
					Player = s.Player with
					{
						DiscardPile = Append(s.Player.DiscardPile, card)
					}
				};
			}
		}
		return s;
	}

	private static bool CascadeEligible(CardInstance card, int parentCost, bool sameCost)
	{
		CardDef d = card.Def;
		if (d.Type == "reaction" || (d.Modes?.Count ?? 0) > 0 || d.XCost == true)
		{
			return false;
		}
		if (sameCost ? (d.Cost != parentCost) : (d.Cost >= parentCost))
		{
			return false;
		}
		if (!(d.Type == "permanent") && d.Cascade.GetValueOrDefault() <= 0)
		{
			return d.Effects.Any((DeclarativeEffect e) => e.Trigger == "onPlay");
		}
		return true;
	}

	public static GameState ResolveCascade(GameState state, CardDef parent, int enemyIndex)
	{
		GameState s = state;
		List<CardInstance> drawPile = new List<CardInstance>(s.Player.DrawPile);
		List<CardInstance> discardPile = new List<CardInstance>(s.Player.DiscardPile);
		RngState rng = s.Rng;
		List<CardInstance> revealed = new List<CardInstance>();
		CardInstance hit = null;
		int shuffles = 0;
		bool sameCost = Effects.HasStaticEffect(s, "cascadeSameCost");
		while (true)
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
			CardInstance c = drawPile[0];
			drawPile.RemoveAt(0);
			if (CascadeEligible(c, parent.Cost, sameCost))
			{
				hit = c;
				break;
			}
			revealed.Add(c);
		}
		IReadOnlyList<CardInstance> bottom = revealed;
		if (revealed.Count > 1)
		{
			(CardInstance[] Items, RngState Next) tuple2 = Rng.Shuffle(rng, revealed);
			CardInstance[] mixed = tuple2.Items;
			RngState item2 = tuple2.Next;
			bottom = mixed;
			rng = item2;
		}
		s = s with
		{
			Rng = rng,
			Player = s.Player with
			{
				DrawPile = Concat(drawPile, bottom),
				DiscardPile = discardPile
			}
		};
		for (int k = 0; k < shuffles; k++)
		{
			s = Events.Emit(s, new GameEvent_DeckShuffled());
			s = Effects.RunPermanentTriggers(s, "onShuffle", FirstAliveOrZero(s));
		}
		s = Events.Emit(s, new GameEvent_Cascaded
		{
			CardId = parent.Id,
			HitId = hit?.Def.Id,
			Revealed = revealed.Select((CardInstance cardInstance) => cardInstance.Def.Id).ToList()
		});
		if (hit == null)
		{
			return s;
		}
		s = s with
		{
			Player = s.Player with
			{
				CascadesThisCombat = s.Player.CascadesThisCombat.GetValueOrDefault() + 1
			}
		};
		s = PlayCardDirect(s, hit, enemyIndex, "land");
		return Effects.RunPermanentTriggers(s, "onCascade", enemyIndex);
	}

	public static GameState PlayNecro(GameState state, string cardUid, int? targetIndex = null)
	{
		if (state.Phase != "player-turn")
		{
			throw new InvalidOperationException("自ターン以外はカードをプレイできない");
		}
		CardInstance card = state.Player.ExhaustPile.FirstOrDefault((CardInstance c) => c.Uid == cardUid);
		if (card == null)
		{
			throw new InvalidOperationException("消滅置き場にないカード: " + cardUid);
		}
		int? cost = card.Def.NecroCost;
		if (!cost.HasValue)
		{
			throw new InvalidOperationException(card.Def.Name + " は亡骸プレイを持たない");
		}
		AssertPlayCap(state);
		if (cost.Value > state.Player.Energy)
		{
			throw new InvalidOperationException("エナジー不足: " + card.Def.Name);
		}
		int aliveCount = state.Enemies.Count((EnemyState e) => e.Hp > 0);
		if (targetIndex.HasValue)
		{
			EnemyState target = ((targetIndex.Value >= 0 && targetIndex.Value < state.Enemies.Count) ? state.Enemies[targetIndex.Value] : null);
			if (target == null || target.Hp <= 0)
			{
				throw new InvalidOperationException($"不正な対象: {targetIndex}");
			}
		}
		if (!targetIndex.HasValue && aliveCount > 1 && Effects.CardNeedsTarget(card))
		{
			throw new InvalidOperationException(card.Def.Name + " は対象の指定 (targetIndex) が必要");
		}
		int enemyIndex = targetIndex ?? FindAlive(state.Enemies);
		GameState s = state with
		{
			Player = state.Player with
			{
				Energy = state.Player.Energy - cost.Value,
				ExhaustPile = state.Player.ExhaustPile.Where((CardInstance c) => c.Uid != cardUid).ToList()
			}
		};
		s = Events.Emit(s, new GameEvent_NecroPlayed
		{
			CardId = card.Def.Id
		});
		s = Events.Emit(s, new GameEvent_CardPlayed
		{
			CardId = card.Def.Id
		});
		s = Effects.ResolveOnPlayEffects(s, card, enemyIndex);
		if (card.Def.Effects.Where((DeclarativeEffect e) => e.Trigger == "onPlay").Any(Effects.IsDamageEffect))
		{
			s = Effects.RunPermanentTriggers(s, "onAttackPlayed", enemyIndex);
			s = FireSelfSetTriggers(s, "onAttackPlayed", enemyIndex);
		}
		s = Effects.RunPermanentTriggers(s, "onCardPlayed", enemyIndex);
		if (card.Def.Type == "spell")
		{
			s = FireSelfSetTriggers(s, "onSpellPlayed", enemyIndex);
			s = Effects.RunPermanentTriggers(s, "onSpellPlayed", enemyIndex);
		}
		s = s with
		{
			Player = s.Player with
			{
				CardsPlayedThisTurn = s.Player.CardsPlayedThisTurn + 1,
				CardsPlayedTotal = s.Player.CardsPlayedTotal + 1,
				PlaysThisTurn = s.Player.PlaysThisTurn.GetValueOrDefault() + 1
			}
		};
		s = TickCardTimers(s);
		return CheckCombatEnd(s);
	}

	public static GameState ResolveScry(GameState state, IReadOnlyList<string> discardUids)
	{
		GameStatePendingScry p = state.PendingScry;
		if (p == null)
		{
			throw new InvalidOperationException("占術の保留が無い");
		}
		List<CardInstance> look = state.Player.DrawPile.Take(Math.Min(p.Count, state.Player.DrawPile.Count)).ToList();
		HashSet<string> chosen = new HashSet<string>(discardUids);
		if (chosen.Count != discardUids.Count)
		{
			throw new InvalidOperationException("discardUids の指定が重複している");
		}
		foreach (string uid in discardUids)
		{
			if (!look.Any((CardInstance c) => c.Uid == uid))
			{
				throw new InvalidOperationException("占術で見ている札ではない: " + uid);
			}
		}
		List<CardInstance> discarded = look.Where((CardInstance c) => chosen.Contains(c.Uid)).ToList();
		GameState s = state with
		{
			PendingScry = null,
			Player = state.Player with
			{
				DrawPile = state.Player.DrawPile.Where((CardInstance c) => !chosen.Contains(c.Uid)).ToList(),
				DiscardPile = Concat(state.Player.DiscardPile, discarded),
				ScriedThisCombat = state.Player.ScriedThisCombat.GetValueOrDefault() + look.Count,
				ScryDiscardedThisCombat = state.Player.ScryDiscardedThisCombat.GetValueOrDefault() + discarded.Count
			}
		};
		s = Events.Emit(s, new GameEvent_Scried
		{
			Looked = look.Select((CardInstance c) => c.Def.Id).ToList(),
			Discarded = discarded.Select((CardInstance c) => c.Def.Id).ToList()
		});
		if (look.Count > 0)
		{
			int firstAlive = FirstAliveOrZero(s);
			for (int i = 0; i < p.Times; i++)
			{
				s = Effects.RunPermanentTriggers(s, "onScry", firstAlive);
			}
			for (int i2 = 0; i2 < discarded.Count; i2++)
			{
				s = Effects.RunPermanentTriggers(s, "onScryDiscard", firstAlive);
			}
		}
		return CheckCombatEnd(s);
	}

	public static int RetainHandMax(GameState state)
	{
		return state.Player.Permanents.Sum((CardInstance p) => p.Def.Effects.Where((DeclarativeEffect e) => e.Effect == "retainHandUpTo").Sum((DeclarativeEffect e) => e.Amount.GetValueOrDefault()));
	}

	public static GameState EndTurn(GameState state, int? hearthSparks = null, IReadOnlyList<string>? retainUids = null, string? keepUid = null)
	{
		if (state.Phase != "player-turn")
		{
			throw new InvalidOperationException("自ターン以外はターン終了できない");
		}
		if (hearthSparks.HasValue && hearthSparks.Value < 0)
		{
			throw new InvalidOperationException($"hearthSparks は 0 以上の整数 (hearthSparks={hearthSparks})");
		}
		if (keepUid != null)
		{
			if (!Effects.HasStaticEffect(state, "keepImpulseOne"))
			{
				throw new InvalidOperationException("燠の守りが場に無いので keepUid は指定できない");
			}
			if (!state.Player.Hand.Any((CardInstance cardInstance) => cardInstance.Uid == keepUid) || !state.Player.ImpulseUids.Contains(keepUid))
			{
				throw new InvalidOperationException("keepUid は手札の衝動の札だけ: " + keepUid);
			}
		}
		if (retainUids != null && retainUids.Count > 0)
		{
			int max = RetainHandMax(state);
			if (retainUids.Count > max)
			{
				throw new InvalidOperationException($"手札を残せるのは{max}枚まで (retainUids={retainUids.Count})");
			}
			if (new HashSet<string>(retainUids).Count != retainUids.Count)
			{
				throw new InvalidOperationException("retainUids の指定が重複している");
			}
			foreach (string uid in retainUids)
			{
				if (!state.Player.Hand.Any((CardInstance cardInstance) => cardInstance.Uid == uid))
				{
					throw new InvalidOperationException("手札に無いカード: " + uid);
				}
			}
			state = state with
			{
				RetainUids = retainUids.ToList()
			};
		}
		GameState s = Events.Emit(state with
		{
			EnemyPhase = true,
			HearthSparks = hearthSparks.GetValueOrDefault()
		}, new GameEvent_TurnEnded
		{
			Turn = state.Turn,
			Unplayed = state.Player.Hand.Select((CardInstance cardInstance) => cardInstance.Def.Name).ToList()
		});
		s = Effects.RunPermanentTriggers(s, "onTurnEnd", FirstAliveOrZero(s));
		s = s with
		{
			HearthSparks = null
		};
		s = CheckCombatEnd(s);
		if (IsOver(s))
		{
			return s;
		}
		bool carryHalf = s.Player.Permanents.Any((CardInstance p) => p.Def.Effects.Any((DeclarativeEffect e) => e.Effect == "momentumCarryHalf"));
		s = s with
		{
			Player = s.Player with
			{
				Momentum = (carryHalf ? ((int)Math.Floor((double)s.Player.Momentum / 2.0)) : 0),
				SpellEchoes = 0,
				Weak = Math.Max(0, s.Player.Weak - 1),
				Frail = Math.Max(0, s.Player.Frail - 1),
				Restrain = Math.Max(0, s.Player.Restrain - 1),
				Mist = Math.Max(0, s.Player.Mist.GetValueOrDefault() - 1)
			}
		};
		int scalds = s.Player.Hand.Count((CardInstance cardInstance) => cardInstance.Def.Id == Content.SCALD_DEF.Id);
		int brands = s.Player.Hand.Count((CardInstance cardInstance) => cardInstance.Def.Id == Content.BRAND_DEF.Id || cardInstance.Def.Id == Content.GUILT_DEF.Id);
		int burnHp = scalds * 2 + brands;
		if (burnHp > 0)
		{
			PlayerState beforeScald = s.Player;
			s = s with
			{
				Player = s.Player with
				{
					Hp = s.Player.Hp - burnHp
				}
			};
			s = Events.Emit(s, new GameEvent_ScaldTick
			{
				Count = scalds + brands,
				Amount = burnHp,
				Scalds = scalds,
				Brands = brands,
				PresentationReceipt = Events.PlayerPresentationReceipt(beforeScald, s.Player)
			});
			s = CheckCombatEnd(s);
			if (s.Phase == "lost")
			{
				return s;
			}
		}
		if (s.Player.ImpulseUids.Count > 0)
		{
			HashSet<string> impulse = new HashSet<string>(s.Player.ImpulseUids);
			List<CardInstance> expired = s.Player.Hand.Where((CardInstance cardInstance) => impulse.Contains(cardInstance.Uid)).ToList();
			CardInstance kept = null;
			if (expired.Count > 0 && Effects.HasStaticEffect(s, "keepImpulseOne"))
			{
				CardInstance chosen = ((keepUid != null) ? expired.FirstOrDefault((CardInstance cardInstance) => cardInstance.Uid == keepUid) : null);
				CardInstance best = expired[0];
				foreach (CardInstance c in expired)
				{
					if (c.Def.Cost > best.Def.Cost)
					{
						best = c;
					}
				}
				kept = chosen ?? best;
				string keptUidLocal = kept.Uid;
				expired = expired.Where((CardInstance cardInstance) => cardInstance.Uid != keptUidLocal).ToList();
			}
			string keptUid = kept?.Uid;
			GameState stBefore = s;
			List<CardInstance> returned = expired.Where((CardInstance card3) => Effects.ImpulseUnaffordable(stBefore, card3)).ToList();
			HashSet<string> returnedUids = new HashSet<string>(returned.Select((CardInstance cardInstance) => cardInstance.Uid));
			List<CardInstance> burned = expired.Where((CardInstance cardInstance) => !returnedUids.Contains(cardInstance.Uid)).ToList();
			s = s with
			{
				Player = s.Player with
				{
					Hand = (from cardInstance in s.Player.Hand
						where !impulse.Contains(cardInstance.Uid) || cardInstance.Uid == keptUid
						select (!(cardInstance.Uid == keptUid)) ? cardInstance : cardInstance with
						{
							KeptImpulse = true
						}).ToList(),
					DiscardPile = Concat(s.Player.DiscardPile, returned),
					ExhaustPile = Concat(s.Player.ExhaustPile, burned),
					ImpulseUids = new List<string>()
				}
			};
			if (kept != null)
			{
				s = Events.Emit(s, new GameEvent_ImpulseKept
				{
					CardId = kept.Def.Id
				});
			}
			foreach (CardInstance card in returned)
			{
				s = Events.Emit(s, new GameEvent_ImpulseReturned
				{
					CardId = card.Def.Id
				});
			}
			foreach (CardInstance card2 in burned)
			{
				s = Events.Emit(s, new GameEvent_CardExhausted
				{
					CardId = card2.Def.Id
				});
			}
			int aliveIdx = FirstAliveOrZero(s);
			for (int i = 0; i < expired.Count; i++)
			{
				s = Effects.RunPermanentTriggers(s, "onImpulseExpired", aliveIdx);
			}
			s = Effects.FireExhaustTriggers(s, burned.Count, aliveIdx);
			s = Effects.FireNecroEffects(s, burned, aliveIdx);
		}
		s = s with
		{
			Enemies = MapIdx(s.Enemies, (EnemyState e, int _) => (e.BurrowActive != true) ? e with
			{
				Block = 0
			} : e)
		};
		s = s with
		{
			Player = s.Player with
			{
				DamageTakenLastEnemyPhase = 0,
				AttacksReceivedThisPhase = 0
			}
		};
		for (int i2 = 0; i2 < s.Enemies.Count; i2++)
		{
			EnemyState enemy = s.Enemies[i2];
			if (enemy.Hp <= 0 || enemy.Burn <= 0)
			{
				continue;
			}
			int amount = enemy.Burn;
			int decay = ((!Effects.BurnNoDecay(s)) ? (1 + Content.GetEnemyDef(enemy.EnemyId).BurnResist.GetValueOrDefault()) : 0);
			bool flag = Effects.IsBlazing(s);
			s = WithEnemy(s, i2, (EnemyState e) => e with
			{
				Hp = e.Hp - amount,
				Burn = Math.Max(0, e.Burn - decay),
				DamageTakenTotal = e.DamageTakenTotal.GetValueOrDefault() + amount,
				HpLostSinceRegen = e.HpLostSinceRegen.GetValueOrDefault() + amount
			});
			s = Events.Emit(s, new GameEvent_BurnTick
			{
				EnemyIndex = i2,
				Amount = amount,
				PresentationReceipt = Events.EnemyPresentationReceipt(i2, enemy, s.Enemies[i2])
			});
			if (s.Enemies[i2].Hp <= 0)
			{
				s = Effects.FireEnemyDied(s, i2);
			}
			if (flag && !Effects.IsBlazing(s))
			{
				s = Events.Emit(s, new GameEvent_BlazeExtinguished());
			}
			s = Effects.ApplyDamageInterrupts(s, i2);
			EnemyState struck = s.Enemies[i2];
			EnemyDef defE = Content.GetEnemyDef(struck.EnemyId);
			if (defE.EnrageEveryDamage.HasValue && struck.Hp > 0)
			{
				int valueOrDefault = struck.DamageTakenTotal.GetValueOrDefault();
				int before = valueOrDefault - amount;
				int every = defE.EnrageEveryDamage.Value;
				int gain = ((int)Math.Floor((double)valueOrDefault / (double)every) - (int)Math.Floor((double)before / (double)every)) * (defE.Enrage ?? 2);
				if (gain > 0)
				{
					s = Effects.GainEnemyStrength(s, i2, gain);
					s = Events.Emit(s, new GameEvent_StrengthGained
					{
						EnemyIndex = i2,
						Amount = gain,
						Reason = "enrage-damage"
					});
				}
			}
		}
		s = CheckCombatEnd(s);
		if (IsOver(s))
		{
			return s;
		}
		return ProcessEnemyActions(s, 0);
	}

	private static GameState PostActionStage(GameState state, int enemyIndex)
	{
		GameStateLastAction act = state.LastAction;
		if (act == null || act.EnemyIndex != enemyIndex)
		{
			return state;
		}
		GameEvent_EnemyActionResolved resolved = new GameEvent_EnemyActionResolved
		{
			EnemyIndex = enemyIndex,
			Kind = act.Kind,
			HpLoss = act.HpLoss,
			Actual = act.Actual
		};
		GameState s = Events.Emit(state, resolved);
		s = Hooks.DispatchHooks(s, resolved);
		if (s.Phase == "awaiting-reaction")
		{
			return s;
		}
		return CheckCombatEnd(s);
	}

	public static GameState ContinueAfterWindow(GameState state)
	{
		PendingWindow pending = state.PendingWindow;
		if (pending == null)
		{
			throw new InvalidOperationException("割り込み情報がないのに再開が呼ばれた");
		}
		GameState s = state with
		{
			PendingWindow = null,
			Phase = "player-turn"
		};
		s = CheckCombatEnd(s);
		if (IsOver(s))
		{
			return s;
		}
		if (pending.Stage == "pre")
		{
			s = ExecuteEnemyAction(s, pending.EnemyIndex);
			if (s.Enemies.All((EnemyState e) => e.Hp <= 0))
			{
				return CheckCombatEnd(s);
			}
			s = PostActionStage(s, pending.EnemyIndex);
			if (s.Phase == "awaiting-reaction")
			{
				return s;
			}
			s = CheckCombatEnd(s);
			if (IsOver(s))
			{
				return s;
			}
		}
		return ProcessEnemyActions(s, pending.EnemyIndex + 1);
	}

	private static GameState ProcessEnemyActions(GameState state, int fromIndex)
	{
		GameState s = state;
		while (fromIndex < s.Enemies.Count)
		{
			EnemyState enemy = s.Enemies[fromIndex];
			if (enemy.Hp > 0 && !(enemy.Intent == null))
			{
				EnemyIntent acting = Effects.EffectiveIntent(s, fromIndex);
				EnemyActionPresentation presentationAction = CaptureEnemyExecution(s, fromIndex, (object)acting != enemy.Intent);
				EnemyIntent locked = new EnemyIntent
				{
					Kind = acting.Kind,
					Actual = acting.Actual,
					Base = acting.Base,
					Hits = acting.Hits,
					MirrorHits = ((acting.MirrorHits == true) ? new bool?(true) : ((bool?)null)),
					Inflict = acting.Inflict,
					AlsoDefend = acting.AlsoDefend,
					AlsoBuff = acting.AlsoBuff,
					AlsoDestroySet = ((acting.AlsoDestroySet == true) ? new bool?(true) : ((bool?)null)),
					StrengthPerMilled = acting.StrengthPerMilled
				};
				s = s with
				{
					Enemies = MapIdx(s.Enemies, (EnemyState e, int j) => (j != fromIndex) ? e : e with
					{
						Intent = locked
					}),
					LastAction = null,
					ReactionUsedThisAction = false
				};
				if (locked.AlsoDestroySet == true)
				{
					s = DestroySetCards(s, fromIndex);
				}
				s = s with
				{
					BellLocked = (s.Player.SetCards.Count > 0)
				};
				GameEvent_EnemyActionExecuting executing = new GameEvent_EnemyActionExecuting
				{
					EnemyIndex = fromIndex,
					Kind = locked.Kind,
					PresentationEnemyAction = presentationAction with
					{
						ExecutionEventIndex = s.EventLog.Count
					}
				};
				s = Events.Emit(s, executing);
				s = Hooks.DispatchHooks(s, executing);
				if (s.Phase == "awaiting-reaction")
				{
					return s;
				}
				s = CheckCombatEnd(s);
				if (IsOver(s))
				{
					return s;
				}
				s = ExecuteEnemyAction(s, fromIndex);
				if (s.Enemies.All((EnemyState e) => e.Hp <= 0))
				{
					return CheckCombatEnd(s);
				}
				s = PostActionStage(s, fromIndex);
				if (s.Phase == "awaiting-reaction")
				{
					return s;
				}
				s = CheckCombatEnd(s);
				if (IsOver(s))
				{
					return s;
				}
			}
			fromIndex++;
		}
		return FinishEnemyPhase(s);
	}

	private static GameState FireSelfSetTriggers(GameState state, string trigger, int enemyIndex)
	{
		List<CardInstance> firing = state.Player.SetCards.Where((CardInstance c) => Effects.IsTrapLive(state, c) && c.Def.Effects.Any((DeclarativeEffect e) => e.Trigger == trigger)).ToList();
		if (firing.Count == 0)
		{
			return state;
		}
		HashSet<string> firingUids = new HashSet<string>(firing.Select((CardInstance c) => c.Uid));
		GameState s = state with
		{
			Player = state.Player with
			{
				SetCards = state.Player.SetCards.Where((CardInstance c) => !firingUids.Contains(c.Uid)).ToList(),
				DiscardPile = Concat(state.Player.DiscardPile, firing)
			}
		};
		foreach (CardInstance card in firing)
		{
			s = Events.Emit(s, new GameEvent_ReactionTriggered
			{
				CardId = card.Def.Id,
				Mode = s.ReactionMode
			});
			for (int i = 0; i < card.Def.Effects.Count; i++)
			{
				DeclarativeEffect effect = card.Def.Effects[i];
				if (effect.Trigger == trigger)
				{
					s = Effects.ResolveEffectTargeted(s, effect, enemyIndex, new DamagePresentationSource
					{
						Kind = "reaction",
						DefinitionId = card.Def.Id,
						Uid = card.Uid
					});
				}
			}
		}
		return CheckCombatEnd(s);
	}

	private static GameState ApplyStatusToPlayer(GameState state, StatusInflict inflict)
	{
		string status = inflict.Status;
		int amount = inflict.Amount;
		if (state.Player.Artifact.GetValueOrDefault() > 0)
		{
			switch (status)
			{
			case "weak":
			case "vulnerable":
			case "frail":
			case "restrain":
			case "mist":
			case "slow":
				return Events.Emit(state with
				{
					Player = state.Player with
					{
						Artifact = state.Player.Artifact.GetValueOrDefault() - 1
					}
				}, new GameEvent_PlayerArtifactBlocked
				{
					Status = status
				});
			}
		}
		switch (status)
		{
		case "mist":
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Mist = state.Player.Mist.GetValueOrDefault() + amount
				}
			}, new GameEvent_StatusInflicted
			{
				Status = status,
				Amount = amount
			});
		case "slow":
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Slow = state.Player.Slow.GetValueOrDefault() + amount,
					SlowFresh = true
				}
			}, new GameEvent_StatusInflicted
			{
				Status = status,
				Amount = amount
			});
		case "weak":
		case "vulnerable":
		case "frail":
		case "restrain":
		{
			PlayerState player = status switch
			{
				"weak" => state.Player with
				{
					Weak = state.Player.Weak + amount,
					WeakFreshThisPhase = state.Player.WeakFreshThisPhase.GetValueOrDefault() + amount
				}, 
				"vulnerable" => state.Player with
				{
					Vulnerable = state.Player.Vulnerable + amount,
					VulnerableFresh = true
				}, 
				"frail" => state.Player with
				{
					Frail = state.Player.Frail + amount
				}, 
				_ => state.Player with
				{
					Restrain = state.Player.Restrain + amount
				}, 
			};
			return Events.Emit(state with
			{
				Player = player
			}, new GameEvent_StatusInflicted
			{
				Status = status,
				Amount = amount
			});
		}
		case "scald":
		{
			int existingScald = state.Player.ScaldsThisCombat.GetValueOrDefault();
			int addScald = Math.Min(amount, Effects.CardStatusRoom(state, "scald").GetValueOrDefault());
			if (addScald <= 0)
			{
				return state;
			}
			List<CardInstance> scalds = new List<CardInstance>(addScald);
			for (int i2 = 0; i2 < addScald; i2++)
			{
				scalds.Add(new CardInstance
				{
					Uid = $"{Content.SCALD_DEF.Id}#{existingScald + i2}_t{state.Turn}",
					Def = Content.SCALD_DEF,
					ScaldFresh = true
				});
			}
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					Hand = Concat(state.Player.Hand, scalds),
					ScaldsThisCombat = existingScald + addScald
				}
			}, new GameEvent_StatusInflicted
			{
				Status = "scald",
				Amount = addScald
			});
		}
		case "junk":
		{
			int existingJunk = state.Player.Hand.Count((CardInstance c) => c.Def.Id == Content.JUNK_DEF.Id) + state.Player.DrawPile.Count((CardInstance c) => c.Def.Id == Content.JUNK_DEF.Id) + state.Player.DiscardPile.Count((CardInstance c) => c.Def.Id == Content.JUNK_DEF.Id);
			int addJunk = Math.Min(amount, Effects.CardStatusRoom(state, "junk").GetValueOrDefault());
			if (addJunk <= 0)
			{
				return state;
			}
			List<CardInstance> drawPile = new List<CardInstance>(state.Player.DrawPile);
			RngState rng = state.Rng;
			for (int i3 = 0; i3 < addJunk; i3++)
			{
				int pos;
				(pos, rng) = Rng.NextInt(rng, 0, drawPile.Count);
				drawPile.Insert(pos, new CardInstance
				{
					Uid = $"{Content.JUNK_DEF.Id}#{existingJunk + i3}_t{state.Turn}",
					Def = Content.JUNK_DEF
				});
			}
			return Events.Emit(state with
			{
				Rng = rng,
				Player = state.Player with
				{
					DrawPile = drawPile
				}
			}, new GameEvent_StatusInflicted
			{
				Status = "junk",
				Amount = addJunk
			});
		}
		default:
		{
			int existing = state.Player.Hand.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id) + state.Player.DrawPile.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id) + state.Player.DiscardPile.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id) + state.Player.ExhaustPile.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id) + state.Player.SetCards.Count((CardInstance c) => c.Def.Id == Content.WOUND_DEF.Id);
			int add = Math.Min(amount, Effects.CardStatusRoom(state, "wound").GetValueOrDefault());
			if (add <= 0)
			{
				return state;
			}
			List<CardInstance> wounds = new List<CardInstance>(add);
			for (int i = 0; i < add; i++)
			{
				wounds.Add(new CardInstance
				{
					Uid = $"{Content.WOUND_DEF.Id}#{existing + i}_t{state.Turn}",
					Def = Content.WOUND_DEF
				});
			}
			return Events.Emit(state with
			{
				Player = state.Player with
				{
					DiscardPile = Concat(state.Player.DiscardPile, wounds)
				}
			}, new GameEvent_StatusInflicted
			{
				Status = "wound",
				Amount = add
			});
		}
		}
	}

	private static GameState ExecuteEnemyAction(GameState state, int enemyIndex)
	{
		EnemyState enemy = state.Enemies[enemyIndex];
		if (enemy.Hp <= 0 || enemy.Intent == null)
		{
			if (!state.NegateNextAction)
			{
				return state;
			}
			return state with
			{
				NegateNextAction = false
			};
		}
		if (enemy.ActionNegated == true)
		{
			state = state with
			{
				NegateNextAction = true,
				Enemies = MapIdx(state.Enemies, (EnemyState enemyState2, int j) => (j != enemyIndex) ? enemyState2 : enemyState2 with
				{
					ActionNegated = false
				})
			};
		}
		if (state.NegateNextAction)
		{
			GameState baseState = state with
			{
				NegateNextAction = false
			};
			EnemyIntent eff = Effects.EffectiveIntent(state, enemyIndex);
			if (eff != null && eff.Kind == "steal-gold" && enemy.StolenGold.GetValueOrDefault() > 0)
			{
				int back = eff.Actual;
				baseState = WithEnemy(baseState, enemyIndex, (EnemyState enemyState2) => enemyState2 with
				{
					StolenGold = Math.Max(0, enemyState2.StolenGold.GetValueOrDefault() - back)
				});
			}
			return Effects.RunPermanentTriggers(Events.Emit(baseState, new GameEvent_ActionNegated
			{
				EnemyIndex = enemyIndex
			}), "onActionNegated", enemyIndex);
		}
		EnemyIntent intent = Effects.EffectiveIntent(state, enemyIndex);
		EnemyActionPresentation presentationEnemyAction = CurrentEnemyExecution(state, enemyIndex);
		Func<GameState, int, GameState> markResolved = (GameState gameState, int hpLoss2) => gameState with
		{
			LastAction = new GameStateLastAction
			{
				EnemyIndex = enemyIndex,
				Kind = intent.Kind,
				HpLoss = hpLoss2,
				Actual = intent.Actual
			}
		};
		switch (intent.Kind)
		{
		case "attack":
		{
			if (enemy.Confusion > 0)
			{
				List<int> others = new List<int>();
				for (int i2 = 0; i2 < state.Enemies.Count; i2++)
				{
					if (state.Enemies[i2].Hp > 0 && i2 != enemyIndex)
					{
						others.Add(i2);
					}
				}
				GameState s4 = state;
				int targetIdx2 = enemyIndex;
				if (others.Count == 1)
				{
					targetIdx2 = others[0];
				}
				else if (others.Count > 1)
				{
					(int Value, RngState Next) tuple = Rng.NextInt(state.Rng, 0, others.Count - 1);
					int pick = tuple.Value;
					RngState rng = tuple.Next;
					targetIdx2 = others[pick];
					s4 = s4 with
					{
						Rng = rng
					};
				}
				int total = intent.Actual * (intent.Hits ?? 1);
				EnemyState target2 = s4.Enemies[targetIdx2];
				int blockedC = Math.Min(target2.Block, total);
				int hpLossC = total - blockedC;
				int ti = targetIdx2;
				s4 = s4 with
				{
					Enemies = MapIdx(s4.Enemies, (EnemyState enemyState3, int num2) =>
					{
						EnemyState enemyState2 = enemyState3;
						if (num2 == ti)
						{
							enemyState2 = enemyState2 with
							{
								Block = enemyState2.Block - blockedC,
								Hp = enemyState2.Hp - hpLossC
							};
						}
						if (num2 == enemyIndex)
						{
							enemyState2 = enemyState2 with
							{
								Confusion = enemyState2.Confusion - 1
							};
						}
						return enemyState2;
					})
				};
				return Events.Emit(s4, new GameEvent_ConfusedAttack
				{
					PresentationEnemyAction = presentationEnemyAction,
					EnemyIndex = enemyIndex,
					TargetIndex = targetIdx2,
					Amount = total,
					HpLoss = hpLossC,
					Blocked = blockedC,
					PresentationReceipt = Events.EnemyPresentationReceipt(targetIdx2, target2, s4.Enemies[targetIdx2])
				});
			}
			int hits = ((intent.MirrorHits == true) ? Math.Max(1, state.Player.CardsPlayedThisTurn + state.Player.SetsThisTurn.GetValueOrDefault()) : (intent.Hits ?? 1));
			List<EnemyAttackContribution> presentationContributions = new List<EnemyAttackContribution>();
			int presentationExecutedHits = 0;
			int block = state.Player.Block;
			int iceBlock = state.Player.IceBlock;
			int dealtTotal = 0;
			int hpLoss = 0;
			int blockedTotal = 0;
			bool capFired = false;
			bool nullify = state.NullifyNextAttack == true;
			for (int h = 0; h < hits; h++)
			{
				presentationExecutedHits++;
				int v = intent.Actual;
				v = Effects.ApplyEnemyWeak(v, state.Enemies[enemyIndex]?.Weak);
				if (Effects.SetBellActive(state))
				{
					v = Math.Max(1, v - state.SetDamageReduction.GetValueOrDefault());
				}
				if (state.Player.Vulnerable > 0)
				{
					v = (int)Math.Floor((double)v * 1.5);
				}
				if (state.Player.Slow.GetValueOrDefault() > 0 && state.Player.PlaysThisTurn.GetValueOrDefault() > 0)
				{
					v = (int)Math.Floor((double)v * (1.0 + 0.1 * (double)state.Player.PlaysThisTurn.GetValueOrDefault()));
				}
				dealtTotal += v;
				if (nullify)
				{
					blockedTotal += v;
					if (presentationContributions.Count < 256)
					{
						presentationContributions.Add(new EnemyAttackContribution
						{
							AmountContribution = v,
							HpLossContribution = 0,
							BlockContribution = 0,
							IceContribution = 0,
							NullifiedContribution = v,
							MitigatedContribution = 0
						});
					}
					continue;
				}
				int blocked = Math.Min(block, v);
				block -= blocked;
				int remaining = v - blocked;
				int iceBlocked = Math.Min(iceBlock, remaining);
				iceBlock -= iceBlocked;
				blockedTotal += blocked + iceBlocked;
				int hit = remaining - iceBlocked;
				if (hit > 0 && state.SmallHitToOne.HasValue && hit <= state.SmallHitToOne.Value)
				{
					hit = 1;
				}
				if (hit > 0 && state.HpLossReduce.HasValue)
				{
					hit = Math.Max(0, hit - state.HpLossReduce.Value);
				}
				bool capLive = !state.MaxHpLossCharges.HasValue || state.MaxHpLossCharges.Value > 0 || state.MaxHpLossFiredThisTurn == true;
				if ((hit > 0 && state.MaxHpLossPerTurn.HasValue) & capLive)
				{
					int num = Math.Min(hit, Math.Max(0, state.MaxHpLossPerTurn.Value - state.Player.HpLostThisTurn.GetValueOrDefault() - hpLoss));
					if (num < hit)
					{
						capFired = true;
					}
					hit = num;
				}
				hpLoss += hit;
				if (presentationContributions.Count < 256)
				{
					presentationContributions.Add(new EnemyAttackContribution
					{
						AmountContribution = v,
						HpLossContribution = hit,
						BlockContribution = blocked,
						IceContribution = iceBlocked,
						NullifiedContribution = 0,
						MitigatedContribution = v - blocked - iceBlocked - hit
					});
				}
			}
			bool capCharge = capFired && state.MaxHpLossFiredThisTurn != true && state.MaxHpLossCharges.HasValue;
			GameState sa = state with
			{
				MaxHpLossCharges = (capCharge ? new int?(state.MaxHpLossCharges.Value - 1) : state.MaxHpLossCharges),
				MaxHpLossFiredThisTurn = (capFired ? new bool?(true) : state.MaxHpLossFiredThisTurn),
				NullifyNextAttack = (nullify ? ((bool?)null) : state.NullifyNextAttack),
				Player = state.Player with
				{
					Block = block,
					IceBlock = iceBlock,
					Hp = state.Player.Hp - hpLoss,
					DamageTakenLastEnemyPhase = state.Player.DamageTakenLastEnemyPhase + hpLoss,
					AttacksReceivedThisPhase = state.Player.AttacksReceivedThisPhase.GetValueOrDefault() + 1,
					HpLostThisTurn = state.Player.HpLostThisTurn.GetValueOrDefault() + hpLoss
				}
			};
			sa = Events.Emit(sa, new GameEvent_DamageDealt
			{
				Source = "enemy",
				Amount = dealtTotal,
				HpLoss = hpLoss,
				EnemyIndex = enemyIndex,
				Nullified = (nullify ? new bool?(true) : ((bool?)null)),
				PresentationEnemyAction = presentationEnemyAction,
				PresentationEnemyAttack = new EnemyAttackPresentation
				{
					Version = 1,
					Resolution = "aggregate-final-mutation",
					EnemyId = enemy.EnemyId,
					EffectiveActual = intent.Actual,
					LoopBound = hits,
					ExecutedHits = presentationExecutedHits,
					MirrorHits = ((intent.MirrorHits == true) ? new bool?(true) : ((bool?)null)),
					Contributions = ((presentationExecutedHits <= 256) ? presentationContributions : null),
					ContributionsOmittedReason = ((presentationExecutedHits > 256) ? "presentation_capture_limit" : null)
				},
				Blocked = ((blockedTotal > 0) ? new int?(blockedTotal) : ((int?)null)),
				PresentationReceipt = Events.PlayerPresentationReceipt(state.Player, sa.Player)
			});
			if (capCharge)
			{
				sa = Events.Emit(sa, new GameEvent_HpLossCapped
				{
					Left = sa.MaxHpLossCharges.GetValueOrDefault()
				});
			}
			if (hpLoss > 0)
			{
				sa = Effects.RunPermanentTriggers(sa, "onDamageTaken", enemyIndex);
			}
			if (Content.GetEnemyDef(sa.Enemies[enemyIndex].EnemyId).Imbalanced == true && hpLoss == 0 && dealtTotal > 0)
			{
				sa = WithEnemy(sa, enemyIndex, (EnemyState enemyState2) => enemyState2 with
				{
					StaggeredNext = true
				});
				sa = Events.Emit(sa, new GameEvent_EnemyStaggered
				{
					EnemyIndex = enemyIndex
				});
			}
			if (intent.AlsoDefend.HasValue && intent.AlsoDefend > 0)
			{
				EnemyState enemyState = sa.Enemies[enemyIndex];
				if ((object)enemyState == null || enemyState.BurrowActive != true)
				{
					EnemyState beforeDefend = sa.Enemies[enemyIndex];
					int ad = intent.AlsoDefend.Value;
					sa = WithEnemy(sa, enemyIndex, (EnemyState enemyState2) => enemyState2 with
					{
						Block = enemyState2.Block + ad
					});
					sa = Events.Emit(sa, new GameEvent_BlockGained
					{
						Target = "enemy",
						Amount = ad,
						PresentationReceipt = Events.EnemyPresentationReceipt(enemyIndex, beforeDefend, sa.Enemies[enemyIndex])
					});
				}
			}
			if (intent.Inflict != null)
			{
				sa = ApplyStatusToPlayer(sa, intent.Inflict);
			}
			if (intent.AlsoBuff.HasValue && enemyIndex < sa.Enemies.Count && sa.Enemies[enemyIndex] != null && sa.Enemies[enemyIndex].Hp > 0)
			{
				int ab = intent.AlsoBuff.Value;
				sa = Effects.GainEnemyStrength(sa, enemyIndex, ab);
				sa = Events.Emit(sa, new GameEvent_StrengthGained
				{
					EnemyIndex = enemyIndex,
					Amount = ab
				});
			}
			sa = sa with
			{
				Enemies = MapIdx(sa.Enemies, (EnemyState enemyState2, int j) => (j != enemyIndex || enemyState2.Weak.GetValueOrDefault() <= 0) ? enemyState2 : enemyState2 with
				{
					Weak = enemyState2.Weak.Value - 1
				})
			};
			return markResolved(sa, hpLoss);
		}
		case "hex":
		{
			GameState s7 = state;
			if (intent.Inflict != null)
			{
				s7 = ApplyStatusToPlayer(s7, intent.Inflict);
			}
			return markResolved(s7, 0);
		}
		case "defend":
		{
			bool shellUp = (state.Enemies[enemyIndex]?.BurrowActive == true);
			List<EnemyState> enemies2 = MapIdx(state.Enemies, (EnemyState enemyState2, int num2) => (num2 != enemyIndex || shellUp) ? enemyState2 : enemyState2 with
			{
				Block = enemyState2.Block + intent.Actual
			});
			GameState s6 = Events.Emit(state with
			{
				Enemies = enemies2
			}, new GameEvent_BlockGained
			{
				Target = "enemy",
				Amount = intent.Actual,
				PresentationReceipt = Events.EnemyPresentationReceipt(enemyIndex, state.Enemies[enemyIndex], enemies2[enemyIndex])
			});
			if (intent.AlsoBuff.HasValue && enemyIndex < s6.Enemies.Count && s6.Enemies[enemyIndex] != null && s6.Enemies[enemyIndex].Hp > 0)
			{
				int ab2 = intent.AlsoBuff.Value;
				s6 = Effects.GainEnemyStrength(s6, enemyIndex, ab2);
				s6 = Events.Emit(s6, new GameEvent_StrengthGained
				{
					EnemyIndex = enemyIndex,
					Amount = ab2
				});
			}
			return markResolved(s6, 0);
		}
		case "buff":
		{
			IReadOnlyList<EnemyState> enemies = Effects.GainEnemyStrength(state, enemyIndex, intent.Actual).Enemies;
			return markResolved(Events.Emit(state with
			{
				Enemies = enemies
			}, new GameEvent_StrengthGained
			{
				EnemyIndex = enemyIndex,
				Amount = intent.Actual
			}), 0);
		}
		case "heal":
		{
			int targetIdx = enemyIndex;
			double worst = double.PositiveInfinity;
			for (int i = 0; i < state.Enemies.Count; i++)
			{
				EnemyState e = state.Enemies[i];
				if (e.Hp > 0)
				{
					double ratio = (double)e.Hp / (double)e.MaxHp;
					if (ratio < worst)
					{
						worst = ratio;
						targetIdx = i;
					}
				}
			}
			EnemyState target = state.Enemies[targetIdx];
			int healed = Math.Min(intent.Actual, target.MaxHp - target.Hp);
			int tIdx = targetIdx;
			GameState next = state with
			{
				Enemies = MapIdx(state.Enemies, (EnemyState enemyState2, int num2) => (num2 != tIdx) ? enemyState2 : enemyState2 with
				{
					Hp = enemyState2.Hp + healed
				})
			};
			GameState s2 = Events.Emit(next, new GameEvent_EnemyHealed
			{
				EnemyIndex = enemyIndex,
				TargetIndex = targetIdx,
				Amount = healed,
				PresentationReceipt = Events.EnemyPresentationReceipt(targetIdx, target, next.Enemies[targetIdx])
			});
			return markResolved(s2, 0);
		}
		case "steal-gold":
			return markResolved(state, 0);
		case "flee":
		{
			GameState s10 = Events.Emit(state with
			{
				Enemies = MapIdx(state.Enemies, (EnemyState enemyState2, int num2) => (num2 != enemyIndex) ? enemyState2 : enemyState2 with
				{
					Hp = 0,
					Fled = true,
					Intent = null
				})
			}, new GameEvent_EnemyFled
			{
				EnemyIndex = enemyIndex
			});
			return markResolved(s10, 0);
		}
		case "rest":
			return markResolved(state, 0);
		case "hatch":
		{
			EnemyDef def2 = Content.GetEnemyDef(enemy.EnemyId);
			EnemyDefHatchInto into = def2.HatchInto;
			if (into == null)
			{
				return markResolved(state, 0);
			}
			if (enemy.SummonBlocked == true)
			{
				return markResolved(WithEnemy(state, enemyIndex, (EnemyState enemyState2) => enemyState2 with
				{
					SummonBlocked = false
				}), 0);
			}
			EnemyDef newDef = Content.GetEnemyDef(into.EnemyId);
			double hatchRatio = ((def2.MaxHp > 0) ? ((double)enemy.MaxHp / (double)def2.MaxHp) : 1.0);
			int hatchedHp = Math.Max(1, JsRound((double)newDef.MaxHp * hatchRatio));
			GameState s9 = state with
			{
				Enemies = MapIdx(state.Enemies, (EnemyState enemyState2, int j) => (j != enemyIndex) ? enemyState2 : enemyState2 with
				{
					EnemyId = into.EnemyId,
					Hp = hatchedHp,
					MaxHp = hatchedHp,
					Block = 0,
					Strength = 0,
					Node = newDef.Start,
					LastMoves = null,
					UsedOnce = new List<string>(),
					MoveUses = new Dictionary<string, int>(),
					FiredInterrupts = new List<int>(),
					Artifact = newDef.Artifact.GetValueOrDefault(),
					Intent = null
				})
			};
			s9 = Events.Emit(s9, new GameEvent_EnemyHatched
			{
				EnemyIndex = enemyIndex,
				FromId = def2.Id,
				IntoId = into.EnemyId
			});
			return markResolved(s9, 0);
		}
		case "summon":
		{
			EnemyDef def = Content.GetEnemyDef(enemy.EnemyId);
			EnemyMove move = null;
			for (int k = 0; k < def.Moves.Count; k++)
			{
				if (def.Moves[k].Id == enemy.IntentMoveId)
				{
					move = def.Moves[k];
					break;
				}
			}
			if (move == null)
			{
				for (int k2 = 0; k2 < def.Moves.Count; k2++)
				{
					if (def.Moves[k2].Kind == "summon")
					{
						move = def.Moves[k2];
						break;
					}
				}
			}
			EnemyMoveSummon spawn = move?.Summon;
			if (spawn == null)
			{
				return markResolved(state, 0);
			}
			if (enemy.SummonBlocked == true)
			{
				return markResolved(WithEnemy(state, enemyIndex, (EnemyState enemyState2) => enemyState2 with
				{
					SummonBlocked = false
				}), 0);
			}
			int before = state.Enemies.Count;
			GameState s3 = SpawnEnemies(state, enemyIndex, spawn.EnemyId, spawn.Count, spawn.Stunned, spawn.Strength, 4);
			s3 = Events.Emit(s3, new GameEvent_EnemySummoned
			{
				EnemyIndex = enemyIndex,
				Into = spawn.EnemyId,
				Count = s3.Enemies.Count - before
			});
			return markResolved(s3, 0);
		}
		case "mill":
		{
			int eaten = Math.Max(0, Math.Min(intent.Actual, state.Player.DrawPile.Count));
			GameState sm = Effects.MillPlayerDeck(state, intent.Actual, enemyIndex);
			if (intent.StrengthPerMilled.HasValue && eaten > 0 && enemyIndex < sm.Enemies.Count && sm.Enemies[enemyIndex] != null && sm.Enemies[enemyIndex].Hp > 0)
			{
				int gain = eaten * intent.StrengthPerMilled.Value;
				sm = Effects.GainEnemyStrength(sm, enemyIndex, gain);
				sm = Events.Emit(sm, new GameEvent_StrengthGained
				{
					EnemyIndex = enemyIndex,
					Amount = gain
				});
			}
			return markResolved(sm, 0);
		}
		case "seal":
		{
			bool fromDraw = state.Player.DrawPile.Count > 0;
			IReadOnlyList<CardInstance> pile = (fromDraw ? state.Player.DrawPile : state.Player.DiscardPile);
			if (pile.Count == 0 || enemyIndex >= state.Enemies.Count || state.Enemies[enemyIndex] == null || state.Enemies[enemyIndex].Hp <= 0)
			{
				return markResolved(state, 0);
			}
			int top = ((IEnumerable<CardInstance>)pile).Max((Func<CardInstance, int>)SealRarityRank);
			List<int> cands = new List<int>();
			for (int k3 = 0; k3 < pile.Count; k3++)
			{
				if (SealRarityRank(pile[k3]) == top)
				{
					cands.Add(k3);
				}
			}
			GameState s11 = state;
			int pickK = cands[0];
			if (cands.Count > 1)
			{
				(int Value, RngState Next) tuple3 = Rng.NextInt(s11.Rng, 0, cands.Count - 1);
				int r = tuple3.Value;
				RngState rng3 = tuple3.Next;
				pickK = cands[r];
				s11 = s11 with
				{
					Rng = rng3
				};
			}
			CardInstance picked = pile[pickK];
			List<CardInstance> rest = pile.Where((CardInstance _, int num2) => num2 != pickK).ToList();
			s11 = s11 with
			{
				Player = (fromDraw ? s11.Player with
				{
					DrawPile = rest
				} : s11.Player with
				{
					DiscardPile = rest
				})
			};
			s11 = WithEnemy(s11, enemyIndex, (EnemyState x) => x with
			{
				Sealed = (x.Sealed ?? new List<CardInstance>()).Concat(new CardInstance[1] { picked }).ToList()
			});
			s11 = Events.Emit(s11, new GameEvent_CardSealed
			{
				EnemyIndex = enemyIndex,
				CardId = picked.Def.Id
			});
			return markResolved(s11, 0);
		}
		case "rally":
		{
			GameState s8 = state;
			for (int i3 = 0; i3 < s8.Enemies.Count; i3++)
			{
				if (s8.Enemies[i3].Hp > 0)
				{
					s8 = Effects.GainEnemyStrength(s8, i3, intent.Actual);
					s8 = Events.Emit(s8, new GameEvent_StrengthGained
					{
						EnemyIndex = i3,
						Amount = intent.Actual
					});
				}
			}
			return markResolved(s8, 0);
		}
		case "destroy-token":
		{
			List<CardInstance> tokens = state.Player.Permanents.Where((CardInstance p) => p.Token == true || p.Def.Retainer == true).ToList();
			if (tokens.Count == 0)
			{
				return markResolved(state, 0);
			}
			(int Value, RngState Next) tuple2 = Rng.NextInt(state.Rng, 0, tokens.Count - 1);
			int idx = tuple2.Value;
			RngState rng2 = tuple2.Next;
			CardInstance target3 = tokens[idx];
			GameState s5 = state with
			{
				Rng = rng2,
				Player = state.Player with
				{
					Permanents = state.Player.Permanents.Where((CardInstance p) => p.Uid != target3.Uid).ToList()
				}
			};
			s5 = Events.Emit(s5, new GameEvent_TokenDestroyed
			{
				CardId = target3.Def.Id,
				Uid = target3.Uid
			});
			return markResolved(s5, 0);
		}
		case "destroy-set":
		{
			if (state.Player.SetCards.Count == 0)
			{
				return markResolved(state, 0);
			}
			GameState s = DestroySetCards(state, enemyIndex);
			if (intent.Inflict != null)
			{
				s = ApplyStatusToPlayer(s, intent.Inflict);
			}
			return markResolved(CheckCombatEnd(s), 0);
		}
		default:
			return state;
		}
	}

	private static GameState DestroySetCards(GameState state, int enemyIndex)
	{
		List<CardInstance> targets = state.Player.SetCards.Where((CardInstance c) => Effects.IsTrapLive(state, c)).ToList();
		if (targets.Count == 0)
		{
			return state;
		}
		GameState s = state;
		foreach (CardInstance card in targets)
		{
			for (int i = 0; i < card.Def.Effects.Count; i++)
			{
				DeclarativeEffect effect = card.Def.Effects[i];
				if (effect.Trigger == "onSetDestroyed")
				{
					s = Effects.ResolveEffectTargeted(s, effect, enemyIndex, new DamagePresentationSource
					{
						Kind = "reaction",
						DefinitionId = card.Def.Id,
						Uid = card.Uid
					});
				}
			}
		}
		s = s with
		{
			Player = s.Player with
			{
				SetCards = s.Player.SetCards.Where((CardInstance c) => !targets.Contains(c)).ToList(),
				DiscardPile = Concat(s.Player.DiscardPile, targets)
			}
		};
		foreach (CardInstance card2 in targets)
		{
			s = Events.Emit(s, new GameEvent_SetCardDestroyed
			{
				CardId = card2.Def.Id
			});
		}
		return s;
	}

	public static bool CardChoosesDoll(CardDef def)
	{
		foreach (DeclarativeEffect e in def.Effects)
		{
			if (e.Trigger == "onPlay" && DOLL_CHOICE_EFFECTS.Contains(e.Effect))
			{
				return true;
			}
		}
		return false;
	}

	private static GameState ExpireTraps(GameState state)
	{
		List<CardInstance> expired = state.Player.SetCards.Where((CardInstance c) =>
		{
			int? num = Effects.TrapLifeOf(state, c);
			return num.HasValue && Effects.TrapAge(state, c) >= num.Value;
		}).ToList();
		if (expired.Count == 0)
		{
			return state;
		}
		int firstAlive = Math.Max(0, FindAlive(state.Enemies));
		HashSet<string> expiredUids = new HashSet<string>(expired.Select((CardInstance c) => c.Uid));
		GameState s = state with
		{
			Player = state.Player with
			{
				SetCards = state.Player.SetCards.Where((CardInstance c) => !expiredUids.Contains(c.Uid)).ToList()
			}
		};
		foreach (CardInstance card in expired)
		{
			for (int i = 0; i < card.Def.Effects.Count; i++)
			{
				DeclarativeEffect effect = card.Def.Effects[i];
				if (effect.Trigger == "onSetDestroyed")
				{
					s = Effects.ResolveEffectTargeted(s, effect, firstAlive, new DamagePresentationSource
					{
						Kind = "reaction",
						DefinitionId = card.Def.Id,
						Uid = card.Uid
					});
				}
			}
			string to;
			if (s.ExpireToHand == true)
			{
				to = "hand";
			}
			else
			{
				to = ((card.Def.Exhaust == true) ? "exhaust" : "discard");
			}
			CardInstance bare = card with
			{
				SetTurn = null,
				TrapLifeBonus = null
			};
			if (to == "hand")
			{
				s = s with
				{
					Player = s.Player with
					{
						Hand = Concat(s.Player.Hand, new List<CardInstance> { bare })
					}
				};
			}
			else
			{
				s = ((!(to == "discard")) ? s with
				{
					Player = s.Player with
					{
						ExhaustPile = Concat(s.Player.ExhaustPile, new List<CardInstance> { bare })
					}
				} : s with
				{
					Player = s.Player with
					{
						DiscardPile = Concat(s.Player.DiscardPile, new List<CardInstance> { bare })
					}
				});
			}
			s = Events.Emit(s, new GameEvent_SetCardExpired
			{
				CardId = card.Def.Id,
				To = to
			});
			s = Effects.RunPermanentTriggers(s, "onSetExpired", firstAlive);
			if (to == "exhaust")
			{
				s = Events.Emit(s, new GameEvent_CardExhausted
				{
					CardId = card.Def.Id
				});
				s = Effects.FireExhaustTriggers(s, 1, firstAlive);
				s = Effects.FireNecroEffects(s, new List<CardInstance> { bare }, firstAlive);
			}
		}
		return CheckCombatEnd(s);
	}

	private static GameState FinishEnemyPhase(GameState state)
	{
		GameEvent_EnemyPhaseEnded ended = new GameEvent_EnemyPhaseEnded
		{
			Turn = state.Turn
		};
		GameState s = Events.Emit(state, ended);
		s = s with
		{
			Player = s.Player with
			{
				PerfectBlockLastPhase = (s.Player.AttacksReceivedThisPhase.GetValueOrDefault() > 0 && s.Player.DamageTakenLastEnemyPhase == 0)
			}
		};
		s = Hooks.DispatchHooks(s, ended);
		IReadOnlyList<GameStatePendingPhaseEffects> pendingPhase = s.PendingPhaseEffects ?? new List<GameStatePendingPhaseEffects>();
		if (pendingPhase.Count > 0)
		{
			s = s with
			{
				PendingPhaseEffects = null
			};
			if (s.Player.PerfectBlockLastPhase == true)
			{
				foreach (GameStatePendingPhaseEffects pe in pendingPhase)
				{
					s = Effects.ResolveEffectTargeted(s, pe.Effect, pe.EnemyIndex, pe.PresentationSource);
				}
			}
		}
		HashSet<string> handBeforeExpire = new HashSet<string>(s.Player.Hand.Select((CardInstance c) => c.Uid));
		s = ExpireTraps(s);
		if (s.Phase == "won" || s.Phase == "lost")
		{
			return s;
		}
		s = Effects.ExpireRetainers(s);
		s = s with
		{
			Player = s.Player with
			{
				Vulnerable = ((s.Player.VulnerableFresh == true) ? s.Player.Vulnerable : Math.Max(0, s.Player.Vulnerable - 1)),
				VulnerableFresh = false,
				Slow = ((s.Player.SlowFresh == true) ? s.Player.Slow.GetValueOrDefault() : Math.Max(0, s.Player.Slow.GetValueOrDefault() - 1)),
				SlowFresh = false
			}
		};
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			EnemyState e = s.Enemies[i];
			if (e.Hp <= 0)
			{
				continue;
			}
			EnemyDef def = Content.GetEnemyDef(e.EnemyId);
			if (def.Regen.HasValue && (double)e.Hp > (double)e.MaxHp * 0.5)
			{
				if (def.RegenBreak.HasValue && e.HpLostSinceRegen.GetValueOrDefault() >= def.RegenBreak.Value)
				{
					s = Events.Emit(s, new GameEvent_RegenBroken
					{
						EnemyIndex = i
					});
				}
				else
				{
					int amount = Math.Min(def.Regen.Value, e.MaxHp - e.Hp);
					if (amount > 0)
					{
						s = WithEnemy(s, i, (EnemyState x) => x with
						{
							Hp = x.Hp + amount
						});
						s = Events.Emit(s, new GameEvent_RegenTicked
						{
							EnemyIndex = i,
							Amount = amount,
							PresentationReceipt = Events.EnemyPresentationReceipt(i, e, s.Enemies[i])
						});
					}
				}
			}
			s = WithEnemy(s, i, (EnemyState x) => x with
			{
				HpLostSinceRegen = 0
			});
			if (def.Enrage.HasValue && !def.EnrageEveryCards.HasValue)
			{
				int amount2 = def.Enrage.Value;
				s = Effects.GainEnemyStrength(s, i, amount2);
				s = Events.Emit(s, new GameEvent_StrengthGained
				{
					EnemyIndex = i,
					Amount = amount2,
					Reason = "enrage-phase"
				});
			}
		}
		IReadOnlyList<CardInstance> oldHand = s.Player.Hand;
		bool retainAll = s.RetainHand == true || s.RetainHandThisTurn == true;
		int retainedDown = s.Player.Permanents.Sum((CardInstance p) => p.Def.Effects.Where((DeclarativeEffect declarativeEffect) => declarativeEffect.Effect == "retainedCostDown").Sum((DeclarativeEffect declarativeEffect) => declarativeEffect.Amount.GetValueOrDefault()));
		s = s with
		{
			RetainUids = null,
			Player = s.Player with
			{
				Hand = (from c in oldHand.Where(Keeps)
					select (c.ScaldFresh != true) ? c : c with
					{
						ScaldFresh = false
					} into c
					select (c.KeptImpulse != true) ? c : c with
					{
						KeptImpulse = null
					} into c
					select (retainedDown <= 0 || c.Def.Id.StartsWith("status_", StringComparison.Ordinal)) ? c : c with
					{
						RetainDiscount = c.RetainDiscount.GetValueOrDefault() + retainedDown
					}).ToList(),
				DiscardPile = Concat(s.Player.DiscardPile, oldHand.Where((CardInstance c) => c.Def.Id != Content.SCALD_DEF.Id && !Keeps(c)))
			}
		};
		return StartPlayerTurn(s, s.Turn + 1);
		bool Keeps(CardInstance c)
		{
			if ((!(c.Def.Id == Content.SCALD_DEF.Id) || c.ScaldFresh != true) && c.Def.Retain != true && c.KeptImpulse != true && (!retainAll || !(c.Def.Id != Content.SCALD_DEF.Id)) && handBeforeExpire.Contains(c.Uid))
			{
				return s.RetainUids?.Contains(c.Uid) ?? false;
			}
			return true;
		}
	}

	private static EnemyMoveDeclarationPresentation CaptureEnemyMoveDeclaration(GameState state, int enemyIndex, string origin, string primaryMoveId = null, string alternateMoveId = null, string primaryNodeId = null)
	{
		return new EnemyMoveDeclarationPresentation
		{
			Version = 1,
			EventIndex = state.EventLog.Count,
			EnemyId = state.Enemies[enemyIndex].EnemyId,
			Origin = origin,
			PrimaryMoveId = primaryMoveId,
			AlternateMoveId = alternateMoveId,
			PrimaryNodeId = primaryNodeId
		};
	}

	private static bool EnemyPresentationBarrier(GameEvent ev, int enemyIndex)
	{
		if (!(ev is GameEvent_CombatStarted) && !(ev is GameEvent_TurnStarted) && (!(ev is GameEvent_EnemyActionExecuting ex) || ex.EnemyIndex != enemyIndex) && (!(ev is GameEvent_EnemyActionResolved re) || re.EnemyIndex != enemyIndex) && (!(ev is GameEvent_ActionNegated ne) || ne.EnemyIndex != enemyIndex) && (!(ev is GameEvent_EnemyDied de) || de.EnemyIndex != enemyIndex) && (!(ev is GameEvent_EnemyFled fl) || fl.EnemyIndex != enemyIndex))
		{
			if (ev is GameEvent_EnemyHatched ha)
			{
				return ha.EnemyIndex == enemyIndex;
			}
			return false;
		}
		return true;
	}

	private static bool ValidEnemyMoveDeclaration(GameState state, int enemyIndex, int index, EnemyMoveDeclarationPresentation d)
	{
		if (d == null || d.Version != 1 || d.EventIndex != index || d.EnemyId != state.Enemies[enemyIndex].EnemyId)
		{
			return false;
		}
		EnemyDef def = Content.GetEnemyDef(d.EnemyId);
		EnemyMove primary = def.Moves.FirstOrDefault((EnemyMove m) => m.Id == d.PrimaryMoveId);
		if (d.Origin == "graph")
		{
			if (primary == null || string.IsNullOrEmpty(d.PrimaryNodeId) || !def.Nodes.TryGetValue(d.PrimaryNodeId, out EnemyNode node) || node.Move != d.PrimaryMoveId)
			{
				return false;
			}
			if (d.AlternateMoveId != null && !def.Moves.Any((EnemyMove m) => m.Id == d.AlternateMoveId))
			{
				return false;
			}
			return true;
		}
		if (d.PrimaryNodeId != null || d.AlternateMoveId != null)
		{
			return false;
		}
		if (d.Origin == "forced-bite")
		{
			if (primary != null)
			{
				return d.PrimaryMoveId == def.Burrow?.Bite;
			}
			return false;
		}
		if (d.Origin == "forced-flee")
		{
			if (d.PrimaryMoveId != null)
			{
				return primary?.Kind == "flee";
			}
			return true;
		}
		if (d.Origin == "stagger" || d.Origin == "spawn-rest")
		{
			return d.PrimaryMoveId == null;
		}
		return false;
	}

	private static EnemyActionPresentation CaptureEnemyExecution(GameState state, int enemyIndex, bool alternate)
	{
		string enemyId = state.Enemies[enemyIndex].EnemyId;
		for (int j = state.EventLog.Count - 1; j >= 0; j--)
		{
			GameEvent ev = state.EventLog[j];
			if (ev is GameEvent_EnemyIntentDeclared declared && declared.EnemyIndex == enemyIndex)
			{
				EnemyMoveDeclarationPresentation d = declared.PresentationMove;
				if (!ValidEnemyMoveDeclaration(state, enemyIndex, j, d))
				{
					return Unknown("missing_or_invalid_latest_declaration");
				}
				string moveId = (alternate ? d.AlternateMoveId : d.PrimaryMoveId);
				if (string.IsNullOrEmpty(moveId))
				{
					return Unknown("synthetic_or_unproved_move")with
					{
						DeclaredEventIndex = j,
						Origin = d.Origin
					};
				}
				return new EnemyActionPresentation
				{
					Version = 1,
					ExecutionEventIndex = state.EventLog.Count,
					EnemyId = enemyId,
					DeclaredEventIndex = j,
					MoveId = moveId,
					Branch = (alternate ? "alternate" : "primary"),
					Origin = d.Origin
				};
			}
			if (EnemyPresentationBarrier(ev, enemyIndex))
			{
				break;
			}
		}
		return Unknown("missing_current_declaration");
		EnemyActionPresentation Unknown(string reason)
		{
			return new EnemyActionPresentation
			{
				Version = 1,
				ExecutionEventIndex = state.EventLog.Count,
				EnemyId = enemyId,
				UnresolvedReason = reason
			};
		}
	}

	private static EnemyActionPresentation CurrentEnemyExecution(GameState state, int enemyIndex)
	{
		string enemyId = state.Enemies[enemyIndex].EnemyId;
		for (int j = state.EventLog.Count - 1; j >= 0; j--)
		{
			GameEvent ev = state.EventLog[j];
			if (ev is GameEvent_EnemyActionExecuting executing && executing.EnemyIndex == enemyIndex)
			{
				EnemyActionPresentation a = executing.PresentationEnemyAction;
				if (a == null || a.Version != 1 || a.ExecutionEventIndex != j || a.EnemyId != enemyId)
				{
					return null;
				}
				if (a.MoveId != null)
				{
					int di = a.DeclaredEventIndex ?? (-1);
					if (di < 0 || di >= j || !(state.EventLog[di] is GameEvent_EnemyIntentDeclared d) || d.EnemyIndex != enemyIndex || !ValidEnemyMoveDeclaration(state, enemyIndex, di, d.PresentationMove) || a.Origin != d.PresentationMove.Origin || a.UnresolvedReason != null || ((a.Branch == "primary") ? d.PresentationMove.PrimaryMoveId : ((a.Branch == "alternate") ? d.PresentationMove.AlternateMoveId : null)) != a.MoveId)
					{
						return null;
					}
					for (int k = j - 1; k > di; k--)
					{
						GameEvent prior = state.EventLog[k];
						if ((prior is GameEvent_EnemyIntentDeclared newer && newer.EnemyIndex == enemyIndex) || EnemyPresentationBarrier(prior, enemyIndex))
						{
							return null;
						}
					}
				}
				return a;
			}
			if (ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyActionResolved || ev is GameEvent_ActionNegated || (ev is GameEvent_EnemyIntentDeclared declared && declared.EnemyIndex == enemyIndex) || EnemyPresentationBarrier(ev, enemyIndex))
			{
				break;
			}
		}
		return null;
	}
}
