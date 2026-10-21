using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Gears
{
	public sealed record GearNeedsInfo(bool Target, string? Card, bool Gear);

	public sealed class UseGearOptions
	{
		public int? TargetIndex;

		public string? CardUid;

		public string? AsGearId;

		public IReadOnlyList<string>? SeenGearIds;
	}

	public const int GEAR_CARRY_MAX = 10;

	public const int MANA_MAX = 50;

	public const int GEAR_MANA_COST = 10;

	public const int GEAR_RARITY_WEIGHT_COMMON = 65;

	public const int GEAR_RARITY_WEIGHT_UNCOMMON = 25;

	public const int GEAR_RARITY_WEIGHT_RARE = 10;

	private static readonly HashSet<string> StatusCardIds = new HashSet<string> { "status_wound", "status_scald", "status_junk", "status_brand", "status_guilt" };

	public static GearDef GearDefOf(string gearId)
	{
		return Content.GetGearDef(gearId);
	}

	public static GearInstance MakeGear(string gearId, string uid)
	{
		return new GearInstance
		{
			Uid = uid,
			GearId = gearId,
			Charges = (Content.GetGearDef(gearId).Charges ?? 1)
		};
	}

	public static GearNeedsInfo GearNeeds(GearDef def)
	{
		return new GearNeedsInfo(def.NeedsTarget == true, def.NeedsCard, def.Special == "nameless");
	}

	public static string? GearBlockedReason(GameState? state, int mana, GearInstance gear)
	{
		if (mana < 10)
		{
			return "魔素がない";
		}
		if (gear.Charges <= 0)
		{
			return "使い切っている";
		}
		if (state == null)
		{
			return "戦闘中でない";
		}
		if (state.Phase != "player-turn")
		{
			return "自分の番ではない";
		}
		if (state.EnemyPhase == true)
		{
			return "敵の番には使えない";
		}
		if (state.GearUsedThisTurn == true)
		{
			return "このターンはもう組んだ";
		}
		if (state.PendingScry != null)
		{
			return "占術で捨てる札を選んでから";
		}
		return null;
	}

	public static IReadOnlyList<CardInstance> GearCardChoices(GameState state, GearDef def)
	{
		switch (def.NeedsCard)
		{
		case "hand":
		{
			bool upgrade = false;
			foreach (DeclarativeEffect effect in def.Effects)
			{
				if (effect.Effect == "upgradeInHand")
				{
					upgrade = true;
				}
			}
			if (!upgrade)
			{
				return state.Player.Hand;
			}
			List<CardInstance> list2 = new List<CardInstance>();
			{
				foreach (CardInstance c in state.Player.Hand)
				{
					if (Upgrade.CanUpgradeInHand(c))
					{
						list2.Add(c);
					}
				}
				return list2;
			}
		}
		case "discard":
			return state.Player.DiscardPile;
		case "draw":
		{
			List<CardInstance> list = new List<CardInstance>(state.Player.DrawPile);
			list.Sort((CardInstance a, CardInstance b) => string.Compare(a.Def.Name, b.Def.Name, StringComparison.Ordinal));
			return list;
		}
		default:
			return new List<CardInstance>();
		}
	}

	private static List<int> AliveIndices(GameState state)
	{
		List<int> alive = new List<int>();
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			if (state.Enemies[i].Hp > 0)
			{
				alive.Add(i);
			}
		}
		return alive;
	}

	public static string? GearLiveDamage(GameState state, GearDef def, int? targetIndex = null)
	{
		List<int> alive = AliveIndices(state);
		List<string> parts = new List<string>();
		foreach (DeclarativeEffect e in def.Effects)
		{
			if (e.Effect != "dealDamage" || !e.Amount.HasValue)
			{
				continue;
			}
			int live = Effects.PlayerDamageAfterModifiers(state, e.Amount.Value);
			object obj;
			if (e.Target == "all")
			{
				obj = alive;
			}
			else
			{
				obj = (targetIndex.HasValue ? new List<int> { targetIndex.Value } : alive.Take(1).ToList());
			}
			List<string> each = new List<string>();
			foreach (int i in (List<int>)obj)
			{
				DamageBreakdown bd = Effects.DamageBreakdownOf(state, i, e.Amount.Value, e.Pierce == true, applyExpose: true, withMomentum: false);
				each.Add((bd != null) ? $"敵{i}:{bd.HpLoss}" : $"敵{i}:{live}");
			}
			parts.Add($"{e.Amount.Value}→{string.Join(" / ", each)}");
		}
		if (parts.Count == 0)
		{
			return null;
		}
		string growth = ((state.Player.Growth > 0) ? $"成長+{state.Player.Growth}・" : "");
		return $"実際に与える値: {string.Join("、", parts)}（{growth}急所・装甲・敵ブロック込み。勢いは乗らない）";
	}

	public static string ManaLabel(int mana)
	{
		return $"{mana}/{50}（あと{(int)Math.Floor((double)mana / 10.0)}個）";
	}

	public static string? GearNoEffectReason(GameState state, GearDef def, int? targetIndex = null)
	{
		List<int> alive = AliveIndices(state);
		int target;
		if (targetIndex.HasValue)
		{
			target = targetIndex.Value;
		}
		else
		{
			target = ((alive.Count == 1) ? alive[0] : (-1));
		}
		EnemyState e = ((target >= 0 && target < state.Enemies.Count) ? state.Enemies[target] : null);
		PlayerState p = state.Player;
		foreach (DeclarativeEffect effect in def.Effects)
		{
			switch (effect.Effect)
			{
			case "blockEnemySummon":
			{
				if (e == null)
				{
					return null;
				}
				EnemyDef d = Content.GetEnemyDef(e.EnemyId);
				if ((d.SplitInto != null && d.SplitInto.Count > 1) || d.HatchInto != null || (d.Moves ?? new List<EnemyMove>()).Any((EnemyMove m) => m.Kind == "summon"))
				{
					return null;
				}
				return (d.SplitInto != null) ? "この敵の残機（再起動）は止められない（止まるのは召喚・分裂・孵化）" : "この敵は召喚も分裂も孵化もしない";
			}
			case "blockEnemyInterrupt":
			{
				if (e == null)
				{
					return null;
				}
				EnemyDef enemyDef = Content.GetEnemyDef(e.EnemyId);
				IReadOnlyList<int> fired = e.FiredInterrupts ?? new List<int>();
				int left = 0;
				IReadOnlyList<EnemyInterrupt> ints = enemyDef.Interrupts ?? new List<EnemyInterrupt>();
				for (int i = 0; i < ints.Count; i++)
				{
					if (!fired.Contains(i))
					{
						left++;
					}
				}
				return (left > 0) ? null : "この敵には残っている割り込み（豹変・目覚め）が無い";
			}
			case "stripRider":
			{
				if (e == null)
				{
					return null;
				}
				EnemyIntent it = e.Intent;
				return (it != null && (it.Inflict != null || it.AlsoBuff.HasValue || it.AlsoDefend.HasValue || it.AlsoDestroySet == true || it.StrengthPerMilled.HasValue)) ? null : "この敵のいまの行動に付随物（状態異常・同時強化・同時防御・からくり壊し）は無い";
			}
			case "singleHit":
			{
				if (e == null)
				{
					return null;
				}
				EnemyIntent it2 = e.Intent;
				return (it2 != null && it2.Kind == "attack" && ((it2.Hits ?? 1) > 1 || it2.MirrorHits == true)) ? null : "この敵のいまの行動は連撃ではない";
			}
			case "nullifyNextEnemyAttack":
			{
				bool anyAttack = false;
				foreach (EnemyState x in state.Enemies)
				{
					if (x.Hp > 0 && x.Intent?.Kind == "attack")
					{
						anyAttack = true;
					}
				}
				return anyAttack ? null : "いま攻撃を宣言している敵がいない";
			}
			case "clearEnemyStrength":
				if (e == null)
				{
					return null;
				}
				return (e.Strength > 0) ? null : "この敵の筋力は0以下（マイナスは戻さない）";
			case "shatterBlock":
				if (e == null)
				{
					return null;
				}
				return (e.Block > 0 || e.BurrowActive == true) ? null : "この敵はブロックも殻も持っていない";
			case "cleanseStatuses":
				return (p.Weak == 0 && p.Vulnerable == 0 && p.Frail == 0 && p.Restrain == 0 && p.Mist.GetValueOrDefault() == 0 && p.Slow.GetValueOrDefault() == 0) ? "消せる状態異常を受けていない" : null;
			case "purgeHandStatus":
				return p.Hand.Any((CardInstance c) => StatusCardIds.Contains(c.Def.Id)) ? null : "手札に負傷・火傷・がらくた・烙印が無い";
			case "gainHpRatio":
			case "gainHp":
				return (p.Hp >= p.MaxHp) ? "HPは満タン" : null;
			case "retrieveFromDiscard":
				return (p.DiscardPile.Count == 0) ? "捨て札が無い" : null;
			case "searchDeck":
				return (p.DrawPile.Count == 0) ? "山札が空" : null;
			}
		}
		return null;
	}

	public static GameState ResolveGear(GameState state, GearDef def, UseGearOptions? opts = null)
	{
		if (opts == null)
		{
			opts = new UseGearOptions();
		}
		state = Events.Emit(state, new GameEvent_GearUsed
		{
			GearId = def.Id,
			Name = def.Name
		});
		if (def.Special == "flee")
		{
			return state;
		}
		if (def.Special == "nameless")
		{
			string id = opts.AsGearId;
			if (id == null)
			{
				throw new InvalidOperationException("無銘の部品には化ける先 (asGearId) が要る");
			}
			if (opts.SeenGearIds != null && !opts.SeenGearIds.Contains(id))
			{
				throw new InvalidOperationException("まだ拾ったことのないギアには化けられない");
			}
			GearDef inner = Content.GetGearDef(id);
			if (inner.Special == "nameless")
			{
				throw new InvalidOperationException("無銘の部品には化けられない");
			}
			return ResolveGear(state, inner, new UseGearOptions
			{
				TargetIndex = opts.TargetIndex,
				CardUid = opts.CardUid,
				AsGearId = null,
				SeenGearIds = opts.SeenGearIds
			});
		}
		List<int> alive = AliveIndices(state);
		int target = opts.TargetIndex ?? (-1);
		if (def.NeedsTarget == true)
		{
			if (target < 0)
			{
				if (alive.Count != 1)
				{
					throw new InvalidOperationException(def.Name + " には対象が要る");
				}
				target = alive[0];
			}
			if (target >= state.Enemies.Count || state.Enemies[target].Hp <= 0)
			{
				throw new InvalidOperationException("倒れた敵は対象にできない");
			}
		}
		else
		{
			target = ((alive.Count > 0) ? alive[0] : 0);
		}
		GameState s = state;
		foreach (DeclarativeEffect effect in def.Effects)
		{
			switch (effect.Effect)
			{
			case "retrieveFromDiscard":
			case "searchDeck":
				s = MoveChosenCard(s, effect.Effect, opts.CardUid, def);
				continue;
			case "upgradeInHand":
				s = UpgradeChosenCard(s, opts.CardUid, def);
				continue;
			case "copyCardInHand":
				s = CopyChosenCard(s, opts.CardUid, def);
				continue;
			case "transformInHand":
				s = TransformChosenCard(s, opts.CardUid, def);
				continue;
			}
			s = Effects.ResolveEffectTargeted(s, effect, target, new DamagePresentationSource
			{
				Kind = "gear",
				DefinitionId = def.Id
			});
		}
		return Combat.CheckCombatEnd(s);
	}

	private static GameState MoveChosenCard(GameState state, string kind, string? cardUid, GearDef def)
	{
		IReadOnlyList<CardInstance> from = ((kind == "searchDeck") ? state.Player.DrawPile : state.Player.DiscardPile);
		if (cardUid != null && !from.Any((CardInstance c) => c.Uid == cardUid))
		{
			throw new InvalidOperationException(def.Name + ": 選んだ札が" + ((kind == "searchDeck") ? "山札" : "捨て札") + "に無い");
		}
		if (from.Count == 0)
		{
			return state;
		}
		CardInstance card = ((cardUid == null) ? from[0] : from.FirstOrDefault((CardInstance c) => c.Uid == cardUid));
		if (card == null)
		{
			throw new InvalidOperationException(def.Name + ": 選んだ札が見つからない");
		}
		List<CardInstance> rest = from.Where((CardInstance c) => c.Uid != card.Uid).ToList();
		List<CardInstance> hand = new List<CardInstance>(state.Player.Hand) { card };
		PlayerState player = ((kind == "searchDeck") ? state.Player with
		{
			Hand = hand,
			DrawPile = rest
		} : state.Player with
		{
			Hand = hand,
			DiscardPile = rest
		});
		return state with
		{
			Player = player
		};
	}

	private static GameState UpgradeChosenCard(GameState state, string? cardUid, GearDef def)
	{
		List<CardInstance> choices = state.Player.Hand.Where((CardInstance c) => Upgrade.CanUpgradeInHand(c)).ToList();
		if (choices.Count == 0)
		{
			return state;
		}
		CardInstance card = ((cardUid == null) ? choices[0] : choices.FirstOrDefault((CardInstance c) => c.Uid == cardUid));
		if (card == null)
		{
			throw new InvalidOperationException(def.Name + ": 鍛えられない札は選べない");
		}
		return state with
		{
			Player = state.Player with
			{
				Hand = state.Player.Hand.Select((CardInstance c) => (!(c.Uid == card.Uid)) ? c : Upgrade.UpgradeCard(c)).ToList()
			}
		};
	}

	private static GameState CopyChosenCard(GameState state, string? cardUid, GearDef def)
	{
		IReadOnlyList<CardInstance> hand = state.Player.Hand;
		if (hand.Count == 0)
		{
			return state;
		}
		CardInstance card = ((cardUid == null) ? hand[0] : hand.FirstOrDefault((CardInstance c) => c.Uid == cardUid));
		if (card == null)
		{
			throw new InvalidOperationException(def.Name + ": 選んだ札が見つからない");
		}
		CardInstance copy = card with
		{
			Uid = $"{card.Uid}_copy{hand.Count}",
			Token = true
		};
		return state with
		{
			Player = state.Player with
			{
				Hand = new List<CardInstance>(hand) { copy }
			}
		};
	}

	private static GameState TransformChosenCard(GameState state, string? cardUid, GearDef def)
	{
		IReadOnlyList<CardInstance> hand = state.Player.Hand;
		if (hand.Count == 0)
		{
			return state;
		}
		CardInstance card = ((cardUid == null) ? hand[0] : hand.FirstOrDefault((CardInstance c) => c.Uid == cardUid));
		if (card == null)
		{
			throw new InvalidOperationException(def.Name + ": 選んだ札が見つからない");
		}
		string baseId = (card.Def.Id.EndsWith("+", StringComparison.Ordinal) ? card.Def.Id.Substring(0, card.Def.Id.Length - 1) : card.Def.Id);
		CardDef src = Content.AllCards.FirstOrDefault((CardDef c) => c.Id == baseId) ?? card.Def;
		string srcRarity = src.Rarity ?? "common";
		List<CardDef> pool = Content.AllCards.Where((CardDef c) => c.Color == src.Color && (c.Rarity ?? "common") == srcRarity && c.Id != src.Id).ToList();
		if (pool.Count == 0)
		{
			return state;
		}
		(int Value, RngState Next) tuple = Rng.NextInt(state.Rng, 0, pool.Count - 1);
		int i = tuple.Value;
		RngState rng = tuple.Next;
		CardInstance into = new CardInstance
		{
			Uid = card.Uid + "_morph",
			Def = Content.GetCardDef(pool[i].Id),
			Token = true
		};
		return state with
		{
			Rng = rng,
			Player = state.Player with
			{
				Hand = hand.Select((CardInstance c) => (!(c.Uid == card.Uid)) ? c : into).ToList()
			}
		};
	}
}
