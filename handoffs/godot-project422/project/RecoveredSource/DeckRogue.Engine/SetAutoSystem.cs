using System;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public sealed class SetAutoSystem : IReactionSystem
{
	public string Mode => "set-auto";

	public bool CanHandle(GameState state, Command command)
	{
		return command is Command_SetCard;
	}

	public GameState HandleCommand(GameState state, Command command)
	{
		if (!(command is Command_SetCard c))
		{
			throw new InvalidOperationException("set-auto が処理できないコマンド: " + command.Type);
		}
		return SetBase.SetCard(state, c.CardUid);
	}

	public GameState OnEvent(GameState state, GameEvent ev)
	{
		if (!(ev is GameEvent_EnemyActionExecuting e))
		{
			if (!(ev is GameEvent_EnemyActionResolved e2))
			{
				if (ev is GameEvent_EnemyPhaseEnded)
				{
					return SetBase.EmitWhiffForRemainingSet(state);
				}
				return state;
			}
			ReactionWindow win = new ReactionWindow
			{
				Stage = "post",
				Kind = e2.Kind,
				HpLoss = e2.HpLoss,
				Actual = Effects.ReactionActionValue(state, e2.EnemyIndex)
			};
			CardInstance card = Effects.UsableSetCards(state, win).FirstOrDefault();
			if (card != null)
			{
				return SetBase.FireSetCard(state, card, e2.EnemyIndex);
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
		ReactionWindow win2 = Effects.PreWindowFor(state, e.EnemyIndex)with
		{
			Kind = e.Kind
		};
		CardInstance card2 = Effects.UsableSetCards(state, win2).FirstOrDefault();
		if (card2 != null)
		{
			return SetBase.FireSetCard(state, card2, e.EnemyIndex);
		}
		return state;
	}
}
