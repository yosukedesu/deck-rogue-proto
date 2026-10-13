using System;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class ReactionPreview
{
	public static string? Outcome(GameState state, int enemyIndex, Command command, bool fired)
	{
		if (enemyIndex < 0 || enemyIndex >= state.Enemies.Count)
		{
			return null;
		}
		GameState next;
		try
		{
			next = State.ApplyCommand(state, command);
		}
		catch (Exception)
		{
			return null;
		}
		if (next.EventLog == null || state.EventLog == null)
		{
			return null;
		}
		int loss = 0;
		int blocked = 0;
		int blockBefore = state.Player.Block + state.Player.IceBlock;
		bool hit = false;
		bool cancelled = false;
		bool triggered = false;
		bool enemySegmentDone = false;
		bool killed = false;
		int enemyLoss = 0;
		for (int i = state.EventLog.Count; i < next.EventLog.Count; i++)
		{
			GameEvent ev = next.EventLog[i];
			if (!hit)
			{
				if (ev is GameEvent_BlockGained { Target: "player" } bg)
				{
					blockBefore += bg.Amount;
				}
				if (ev is GameEvent_IceBlockGained ice)
				{
					blockBefore += ice.Amount;
				}
				if (ev is GameEvent_DamageDealt { Source: "enemy", EnemyIndex: var enemyIndex2 } dd && enemyIndex2 == enemyIndex)
				{
					loss = dd.HpLoss;
					blocked = dd.Blocked.GetValueOrDefault();
					hit = true;
				}
				if (ev is GameEvent_ActionNegated negated && negated.EnemyIndex == enemyIndex)
				{
					cancelled = true;
				}
				if (ev is GameEvent_EnemyDied died && died.EnemyIndex == enemyIndex)
				{
					cancelled = true;
				}
			}
			if (ev is GameEvent_ReactionTriggered)
			{
				triggered = true;
			}
			bool flag = triggered;
			bool flag2 = flag;
			if (flag2)
			{
				bool flag3 = ((ev is GameEvent_EnemyActionExecuting || ev is GameEvent_TurnStarted || ev is GameEvent_TurnEnded) ? true : false);
				flag2 = flag3 || (ev is GameEvent_DamageDealt gameEvent_DamageDealt && gameEvent_DamageDealt.Source == "enemy");
			}
			if (flag2)
			{
				enemySegmentDone = true;
			}
			if (triggered && !enemySegmentDone)
			{
				if (ev is GameEvent_DamageDealt { Source: "player", EnemyIndex: var enemyIndex3 } damage && enemyIndex3 == enemyIndex)
				{
					enemyLoss += damage.HpLoss;
				}
				if (ev is GameEvent_EnemyDied died2 && died2.EnemyIndex == enemyIndex)
				{
					killed = true;
				}
			}
		}
		string prefix = (fired ? "発動すると " : "温存すると ");
		string enemy = Content.GetEnemyDef(state.Enemies[enemyIndex].EnemyId).Name;
		string foe = (killed ? (enemy + "を倒す") : ((enemyLoss > 0) ? $"{enemy}に {enemyLoss}（{state.Enemies[enemyIndex].Hp} → {Math.Max(0, state.Enemies[enemyIndex].Hp - enemyLoss)}）" : null));
		if (state.PendingWindow?.Stage == "post")
		{
			return (foe == null) ? null : (prefix + foe);
		}
		if (hit)
		{
			cancelled = false;
		}
		if (cancelled)
		{
			return prefix + ((foe == null) ? "" : (foe + "・")) + "この攻撃は来ない";
		}
		if (Effects.EffectiveIntent(state, enemyIndex)?.Kind != "attack")
		{
			return (foe == null) ? null : (prefix + foe);
		}
		int left = ((state.NullifyNextAttack == true) ? blockBefore : Math.Max(0, blockBefore - blocked));
		string own = ((loss <= 0) ? "HP は減らない" : $"HP −{loss}（{state.Player.Hp} → {Math.Max(0, state.Player.Hp - loss)}）");
		if (left > 0 && foe == null)
		{
			own += $"・ブロック {left} が余る";
		}
		return prefix + ((foe == null) ? "" : (foe + "・")) + own;
	}
}
