using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class State
{
	public static GameState CreateInitialState(int seed, string reactionMode)
	{
		return Combat.CreateInitialState(seed, reactionMode);
	}

	public static GameState ApplyCommand(GameState state, Command command)
	{
		if (state.PendingScry != null && !(command is Command_ResolveScry) && !(command is Command_StartCombat))
		{
			throw new InvalidOperationException("占術で捨てる札を選んでから (ResolveScry)");
		}
		return SettleRetainDiscount(SettleScry(ApplyCommandRaw(state, command)));
	}

	private static GameState SettleRetainDiscount(GameState state)
	{
		PlayerState p = state.Player;
		if (!Has(p.DrawPile) && !Has(p.DiscardPile) && !Has(p.ExhaustPile) && !Has(p.Permanents) && !Has(p.SetCards))
		{
			return state;
		}
		return state with
		{
			Player = p with
			{
				DrawPile = Strip(p.DrawPile),
				DiscardPile = Strip(p.DiscardPile),
				ExhaustPile = Strip(p.ExhaustPile),
				Permanents = Strip(p.Permanents),
				SetCards = Strip(p.SetCards)
			}
		};
		static bool Has(IReadOnlyList<CardInstance> cs)
		{
			return cs.Any((CardInstance c) => c.RetainDiscount.HasValue);
		}
		static IReadOnlyList<CardInstance> Strip(IReadOnlyList<CardInstance> cs)
		{
			if (!Has(cs))
			{
				return cs;
			}
			return cs.Select((CardInstance c) => c.RetainDiscount.HasValue ? c with
			{
				RetainDiscount = null
			} : c).ToList();
		}
	}

	private static GameState SettleScry(GameState state)
	{
		if (state.PendingScry == null)
		{
			return state;
		}
		if (state.Player.DrawPile.Count > 0 && state.Phase != "won" && state.Phase != "lost")
		{
			return state;
		}
		return state with
		{
			PendingScry = null
		};
	}

	private static GameState ApplyCommandRaw(GameState state, Command command)
	{
		if (!(command is Command_ResolveScry rs))
		{
			if (!(command is Command_StartCombat c))
			{
				if (!(command is Command_PlayCard c2))
				{
					if (!(command is Command_EndTurn et))
					{
						if (!(command is Command_RetrieveSetCard c3))
						{
							if (!(command is Command_PlayNecro c4))
							{
								if (command is Command_SetCard || command is Command_ReactManual || command is Command_ConfirmReaction)
								{
									IReactionSystem system = ReactionSystems.Get(state.ReactionMode);
									if (!system.CanHandle(state, command))
									{
										throw new InvalidOperationException(state.ReactionMode + " では受け付けないコマンド: " + command.Type);
									}
									bool flag = state.Phase == "awaiting-reaction";
									GameState next = system.HandleCommand(state, command);
									if (!flag)
									{
										return next;
									}
									return Combat.ContinueAfterWindow(next);
								}
								return state;
							}
							return Combat.PlayNecro(state, c4.CardUid, c4.TargetIndex);
						}
						return SetBase.RetrieveSetCard(state, c3.CardUid);
					}
					return Combat.EndTurn(state, et.HearthSparks, et.RetainUids, et.KeepUid);
				}
				return Combat.PlayCard(state, c2.CardUid, c2.ModeIndex, c2.DiscardUids, c2.TargetIndex, c2.ExhaustUids, c2.RetrieveUid, c2.DeckUids, c2.HandUids, c2.XAmount, c2.PermanentUid);
			}
			return Combat.StartCombat(c.Seed, state.ReactionMode, c.EnemyId, c.DeckId, c.LeaderId, c.CardIds);
		}
		return Combat.ResolveScry(state, rs.DiscardUids);
	}
}
