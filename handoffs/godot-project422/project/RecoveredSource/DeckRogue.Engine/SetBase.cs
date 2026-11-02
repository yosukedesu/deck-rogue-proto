using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class SetBase
{
	private static int SetCostOf(CardInstance card)
	{
		if (card.Def.Type != "reaction")
		{
			return 1;
		}
		if (card.FreeThisCombat != true)
		{
			return card.Def.Cost;
		}
		return 0;
	}

	private static bool SettableType(GameState state, CardInstance card)
	{
		if (!(card.Def.Type == "reaction"))
		{
			if (state.SetAnyCards == true)
			{
				return SetAny.CanSetAsNormal(card.Def);
			}
			return false;
		}
		return true;
	}

	public static bool CanSetCard(GameState state, string cardUid)
	{
		if (state.Phase != "player-turn")
		{
			return false;
		}
		if (state.Player.SetCards.Count >= state.Player.SetSlots)
		{
			return false;
		}
		CardInstance card = state.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == cardUid);
		if (card == null)
		{
			return false;
		}
		if (!SettableType(state, card))
		{
			return false;
		}
		return SetCostOf(card) <= state.Player.Energy;
	}

	public static GameState SetCard(GameState state, string cardUid)
	{
		if (state.Phase != "player-turn")
		{
			throw new InvalidOperationException("自ターン以外は伏せられない");
		}
		if (state.Player.SetCards.Count >= state.Player.SetSlots)
		{
			throw new InvalidOperationException($"伏せは同時{state.Player.SetSlots}枚まで");
		}
		CardInstance card = state.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == cardUid);
		if (card == null)
		{
			throw new InvalidOperationException("手札にないカード: " + cardUid);
		}
		if (!SettableType(state, card))
		{
			throw new InvalidOperationException((state.SetAnyCards == true) ? (card.Def.Name + " は伏せられない (X・モード・追加コスト・ドロー/マナ系は対象外)") : (card.Def.Name + " は伏せられない (リアクションタイプのみ)"));
		}
		int setCost = SetCostOf(card);
		if (setCost > state.Player.Energy)
		{
			throw new InvalidOperationException("エナジー不足: " + card.Def.Name);
		}
		List<CardInstance> setCards = new List<CardInstance>(state.Player.SetCards) { card with
		{
			SetTurn = state.Turn,
			TrapLifeBonus = null
		} };
		GameState s = state with
		{
			Player = state.Player with
			{
				Energy = state.Player.Energy - setCost,
				Hand = state.Player.Hand.Where((CardInstance c) => c.Uid != cardUid).ToList(),
				SetCards = setCards,
				SetsThisTurn = state.Player.SetsThisTurn.GetValueOrDefault() + 1
			}
		};
		GameState revealed = s;
		int alive = -1;
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			if (s.Enemies[i].Hp > 0)
			{
				alive = i;
				break;
			}
		}
		return Effects.RunPermanentTriggers(Events.Emit(revealed, new GameEvent_CardSet
		{
			CardId = card.Def.Id
		}), "onCardSet", Math.Max(0, alive));
	}

	public static GameState RetrieveSetCard(GameState state, string cardUid)
	{
		throw new InvalidOperationException("回収は廃止された (2026-09-13 罠モデル): 罠は2窓で鳴らなければ期限切れで捨て札に戻る");
	}

	public static GameState FireSetCard(GameState state, CardInstance card, int enemyIndex)
	{
		bool exhausts = card.Def.Exhaust == true;
		int fireCost = SetAny.SetFireCost(card);
		if (fireCost > state.Player.Energy)
		{
			return state;
		}
		List<CardInstance> discardPile = new List<CardInstance>(state.Player.DiscardPile);
		List<CardInstance> exhaustPile = new List<CardInstance>(state.Player.ExhaustPile);
		if (exhausts)
		{
			exhaustPile.Add(card);
		}
		else
		{
			discardPile.Add(card);
		}
		GameState s = state with
		{
			ReactionUsedThisAction = true,
			Player = state.Player with
			{
				Energy = state.Player.Energy - fireCost,
				SetCards = state.Player.SetCards.Where((CardInstance c) => c.Uid != card.Uid).ToList(),
				DiscardPile = discardPile,
				ExhaustPile = exhaustPile
			}
		};
		s = Effects.ResolveReactionEffects(s, card, enemyIndex);
		if (exhausts)
		{
			s = Events.Emit(s, new GameEvent_CardExhausted
			{
				CardId = card.Def.Id
			});
			s = Effects.FireExhaustTriggers(s, 1, enemyIndex);
		}
		return s;
	}

	public static GameState EmitWhiffForRemainingSet(GameState state)
	{
		GameState s = state;
		foreach (CardInstance card in state.Player.SetCards)
		{
			if (Effects.IsTrapLive(state, card))
			{
				s = Events.Emit(s, new GameEvent_ReactionWhiffed
				{
					CardId = card.Def.Id
				});
			}
		}
		return s;
	}
}
