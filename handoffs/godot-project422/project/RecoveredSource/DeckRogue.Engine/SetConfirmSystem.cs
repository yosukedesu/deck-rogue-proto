using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public sealed class SetConfirmSystem : IReactionSystem
{
	public string Mode => "set-confirm";

	private static GameState NoteUnaffordable(GameState state, ReactionWindow win)
	{
		GameState s = state;
		foreach (CardInstance c in Effects.UnaffordableSetCards(state, win))
		{
			s = Events.Emit(s, new GameEvent_ReactionUnaffordable
			{
				CardId = c.Def.Id,
				Cost = SetAny.SetFireCost(c),
				Energy = state.Player.Energy
			});
		}
		return s;
	}

	public bool CanHandle(GameState state, Command command)
	{
		if (!(command is Command_SetCard))
		{
			if (command is Command_ConfirmReaction)
			{
				return state.Phase == "awaiting-reaction";
			}
			return false;
		}
		return true;
	}

	public GameState HandleCommand(GameState state, Command command)
	{
		if (!(command is Command_SetCard c))
		{
			Command_ConfirmReaction command_ConfirmReaction = command as Command_ConfirmReaction;
			if ((object)command_ConfirmReaction != null)
			{
				if (state.Phase != "awaiting-reaction" || state.PendingWindow == null)
				{
					throw new InvalidOperationException("確認待ちではないのに ConfirmReaction が来た");
				}
				ReactionWindow win = Effects.WindowFromPending(state);
				IReadOnlyList<CardInstance> readOnlyList2;
				if (!(win != null))
				{
					IReadOnlyList<CardInstance> readOnlyList = new List<CardInstance>();
					readOnlyList2 = readOnlyList;
				}
				else
				{
					IReadOnlyList<CardInstance> readOnlyList = Effects.UsableSetCards(state, win);
					readOnlyList2 = readOnlyList;
				}
				IReadOnlyList<CardInstance> candidates = readOnlyList2;
				if (!command_ConfirmReaction.Fire)
				{
					return Events.Emit(state, new GameEvent_ReactionHeld
					{
						EnemyIndex = state.PendingWindow.EnemyIndex,
						Stage = state.PendingWindow.Stage,
						Kind = ((win != null) ? win.Kind : "attack"),
						Value = ((win != null) ? ((win.Stage == "pre") ? win.Actual : win.HpLoss) : 0),
						CandidateIds = candidates.Select((CardInstance x) => x.Def.Id).ToList()
					});
				}
				CardInstance card = ((command_ConfirmReaction.CardUid != null) ? candidates.FirstOrDefault((CardInstance x) => x.Uid == command_ConfirmReaction.CardUid) : candidates.FirstOrDefault());
				if (card == null)
				{
					throw new InvalidOperationException("この窓で発動できる伏せカードがない");
				}
				return SetBase.FireSetCard(state, card, state.PendingWindow.EnemyIndex);
			}
			throw new InvalidOperationException("set-confirm が処理できないコマンド: " + command.Type);
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
			if (Effects.UsableSetCards(state, win).Count > 0)
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
			return NoteUnaffordable(state, win);
		}
		if (state.ReactionUsedThisAction)
		{
			return state;
		}
		if ((e.EnemyIndex < state.Enemies.Count && state.Enemies[e.EnemyIndex].ActionNegated == true) || state.NegateNextAction)
		{
			return state;
		}
		if (e.Kind == "rest")
		{
			return state;
		}
		ReactionWindow win2 = Effects.PreWindowFor(state, e.EnemyIndex)with
		{
			Kind = e.Kind
		};
		if (Effects.UsableSetCards(state, win2).Count > 0)
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
		return NoteUnaffordable(state, win2);
	}
}
