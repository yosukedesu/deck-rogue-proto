using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public sealed class HoldManualSystem : IReactionSystem
{
	public string Mode => "hold-manual";

	public static IReadOnlyList<CardInstance> PlayableReactions(GameState state)
	{
		if (state.Phase != "awaiting-reaction")
		{
			return new List<CardInstance>();
		}
		ReactionWindow win = Effects.WindowFromPending(state);
		if (win == null)
		{
			return new List<CardInstance>();
		}
		return state.Player.Hand.Where((CardInstance c) => c.Def.Type == "reaction" && Effects.ReactionMatches(state, c, win) && c.Def.Cost <= state.Player.Energy && (state.Player.Hp > 0 || Effects.CanSaveFromLethal(c, state))).ToList();
	}

	private static bool AnyPlayable(GameState state, ReactionWindow win)
	{
		return state.Player.Hand.Any((CardInstance c) => c.Def.Type == "reaction" && Effects.ReactionMatches(state, c, win) && c.Def.Cost <= state.Player.Energy && (state.Player.Hp > 0 || Effects.CanSaveFromLethal(c, state)));
	}

	public bool CanHandle(GameState state, Command command)
	{
		Command_ReactManual command_ReactManual = command as Command_ReactManual;
		if ((object)command_ReactManual == null)
		{
			if (command is Command_ConfirmReaction c)
			{
				if (state.Phase == "awaiting-reaction")
				{
					return !c.Fire;
				}
				return false;
			}
			return false;
		}
		return PlayableReactions(state).Any((CardInstance x) => x.Uid == command_ReactManual.CardUid);
	}

	public GameState HandleCommand(GameState state, Command command)
	{
		Command_ReactManual command_ReactManual = command as Command_ReactManual;
		if ((object)command_ReactManual == null)
		{
			if (command is Command_ConfirmReaction c)
			{
				if (state.Phase != "awaiting-reaction")
				{
					throw new InvalidOperationException("割り込みウィンドウ外でパスが来た");
				}
				if (c.Fire)
				{
					throw new InvalidOperationException("hold-manual の発動は ReactManual でカードを指定する");
				}
				return state;
			}
			throw new InvalidOperationException("hold-manual が処理できないコマンド: " + command.Type);
		}
		if (state.Phase != "awaiting-reaction" || state.PendingWindow == null)
		{
			throw new InvalidOperationException("割り込みウィンドウ外で ReactManual が来た");
		}
		CardInstance card = PlayableReactions(state).FirstOrDefault((CardInstance x) => x.Uid == command_ReactManual.CardUid);
		if (card == null)
		{
			throw new InvalidOperationException("発動できないカード: " + command_ReactManual.CardUid);
		}
		List<CardInstance> discard = new List<CardInstance>(state.Player.DiscardPile) { card };
		return Effects.ResolveReactionEffects(state with
		{
			ReactionUsedThisAction = true,
			Player = state.Player with
			{
				Energy = state.Player.Energy - card.Def.Cost,
				Hand = state.Player.Hand.Where((CardInstance x) => x.Uid != card.Uid).ToList(),
				DiscardPile = discard
			}
		}, card, state.PendingWindow.EnemyIndex);
	}

	public GameState OnEvent(GameState state, GameEvent ev)
	{
		if (!(ev is GameEvent_EnemyActionExecuting e))
		{
			if (!(ev is GameEvent_EnemyActionResolved e2))
			{
				if (ev is GameEvent_EnemyPhaseEnded)
				{
					GameState s = state;
					{
						foreach (CardInstance card in state.Player.Hand)
						{
							if (card.Def.Type == "reaction")
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
				return state;
			}
			if (AnyPlayable(state, new ReactionWindow
			{
				Stage = "post",
				Kind = e2.Kind,
				HpLoss = e2.HpLoss,
				Actual = Effects.ReactionActionValue(state, e2.EnemyIndex)
			}))
			{
				return state with
				{
					Phase = "awaiting-reaction",
					PendingWindow = new PendingWindow
					{
						EnemyIndex = e2.EnemyIndex,
						Stage = "post"
					}
				};
			}
			return state;
		}
		if (state.ReactionUsedThisAction)
		{
			return state;
		}
		if ((e.EnemyIndex < state.Enemies.Count && state.Enemies[e.EnemyIndex].ActionNegated == true) || state.NegateNextAction)
		{
			return state;
		}
		if (AnyPlayable(state, Effects.PreWindowFor(state, e.EnemyIndex)with
		{
			Kind = e.Kind
		}))
		{
			return state with
			{
				Phase = "awaiting-reaction",
				PendingWindow = new PendingWindow
				{
					EnemyIndex = e.EnemyIndex,
					Stage = "pre"
				}
			};
		}
		return state;
	}
}
