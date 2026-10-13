using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class FlowRules
{
	public static readonly string[] Routes = new string[13]
	{
		"title", "departure", "map", "combat", "reward", "relic-reward", "relic-choose", "shop", "campfire", "workshop",
		"event", "won", "lost"
	};

	public static string Route(RunState? run)
	{
		return run?.Phase ?? "title";
	}

	public static bool ChoiceCardAllowed(RunState run, EventChoiceDef choice, CardInstance card)
	{
		return (choice.RemoveCard != true || run.Deck.Count > 5) && (choice.UpgradeCard != true || Upgrade.CanUpgradeCard(card)) && (choice.UnexhaustCard != true || Run.CanUnexhaustCard(card)) && (choice.TransformCard != true || Run.CanTransformCard(card));
	}

	public static string? PlayBlocked(GameState state, CardInstance card)
	{
		if (state.Phase != "player-turn" || state.PendingScry != null)
		{
			return "選択を先に決定してください";
		}
		if (!Effects.IsPlayableFromHand(card, state))
		{
			return "この札はプレイできません";
		}
		if (!Effects.RetainerRequirementMet(state, card))
		{
			return "人形が必要です";
		}
		if (Effects.EffectiveCost(state, card) > state.Player.Energy)
		{
			return "エナジー不足";
		}
		if (card.Def.LightCost.GetValueOrDefault() > state.Player.Light.GetValueOrDefault())
		{
			return "灯が足りません";
		}
		int impulse = RequiredImpulseCost(card.Def);
		if (impulse > ImpulseFuel(state, card).Count)
		{
			return "追加コストの衝動の札が足りません";
		}
		int? cap = Combat.PlayCapOf(state);
		if (cap.HasValue && state.Player.PlaysThisTurn.GetValueOrDefault() >= cap)
		{
			return "このターンのプレイ上限";
		}
		if (card.Def.DiscardCost.GetValueOrDefault() + card.Def.ExhaustCost.GetValueOrDefault() + impulse > state.Player.Hand.Count - 1)
		{
			return "追加コストの手札が足りません";
		}
		return null;
	}

	public static bool CanSkipKindle(CardDef def)
	{
		return def.KindleOptional == true && def.ExhaustCost.GetValueOrDefault() == 0;
	}

	public static int RequiredImpulseCost(CardDef def)
	{
		return (!CanSkipKindle(def)) ? def.ImpulseCost.GetValueOrDefault() : 0;
	}

	public static IReadOnlyList<CardInstance> ImpulseFuel(GameState state, CardInstance card)
	{
		return state.Player.Hand.Where((CardInstance c) => c.Uid != card.Uid && state.Player.ImpulseUids.Contains(c.Uid)).ToArray();
	}

	public static bool CanKindle(GameState state, CardInstance card)
	{
		return ImpulseFuel(state, card).Count >= card.Def.ImpulseCost.GetValueOrDefault() && state.Player.Hand.Count - 1 >= card.Def.DiscardCost.GetValueOrDefault() + card.Def.ExhaustCost.GetValueOrDefault() + card.Def.ImpulseCost.GetValueOrDefault();
	}

	public static IReadOnlyList<CardInstance> KeepImpulsePool(GameState state)
	{
		return state.Player.Permanents.Any((CardInstance p) => p.Def.Effects.Any((DeclarativeEffect e) => e.Effect == "keepImpulseOne")) ? state.Player.Hand.Where((CardInstance c) => state.Player.ImpulseUids.Contains(c.Uid)).ToArray() : Array.Empty<CardInstance>();
	}

	public static int GuardRedirect(GameState state, int requested)
	{
		if (requested < 0 || requested >= state.Enemies.Count)
		{
			return requested;
		}
		if (Content.GetEnemyDef(state.Enemies[requested].EnemyId).Guardian == true)
		{
			return requested;
		}
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			if (i != requested && state.Enemies[i].Hp > 0 && Content.GetEnemyDef(state.Enemies[i].EnemyId).Guardian == true)
			{
				return i;
			}
		}
		return requested;
	}

	public static string NodeName(string kind)
	{
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			"battle" => "戦闘", 
			"elite" => "強個体", 
			"campfire" => "焚き火", 
			"workshop" => "工房", 
			"shop" => "商店", 
			"event" => "？", 
			"treasure" => "宝箱", 
			"boss" => "幕の主", 
			_ => kind, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string PhaseName(string kind)
	{
		if (1 == 0)
		{
		}
		string result = kind switch
		{
			"departure" => "出立の店", 
			"map" => "坑道の地図", 
			"combat" => "戦闘", 
			"reward" => "戦利品", 
			"relic-reward" => "遺物を選ぶ", 
			"relic-choose" => "遺物の対象を選ぶ", 
			"campfire" => "焚き火", 
			"workshop" => "工房", 
			"shop" => "商店", 
			"event" => "坑道の出来事", 
			"won" => "走破！", 
			"lost" => "敗北", 
			_ => "DECK ROGUE", 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
