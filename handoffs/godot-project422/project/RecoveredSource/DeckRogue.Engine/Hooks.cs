using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Hooks
{
	public static GameState RunPermanentTriggers(GameState state, string trigger, int enemyIndex)
	{
		return Effects.RunPermanentTriggers(state, trigger, enemyIndex);
	}

	public static GameState DispatchHooks(GameState state, GameEvent ev)
	{
		GameState s = state;
		if (ev is GameEvent_EnemyActionExecuting { Kind: "attack" } exec1)
		{
			s = Effects.RunPermanentTriggers(s, "onAttackIncoming", exec1.EnemyIndex);
		}
		if (ev is GameEvent_EnemyActionResolved { Kind: "attack" } res1)
		{
			s = Effects.RunPermanentTriggers(s, "onAttacked", res1.EnemyIndex);
		}
		if (ev is GameEvent_EnemyActionResolved resAny)
		{
			s = Effects.RunPermanentTriggers(s, "onEnemyActed", resAny.EnemyIndex);
		}
		if (ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyActionResolved)
		{
			int idx = ((ev is GameEvent_EnemyActionExecuting e2) ? e2.EnemyIndex : ((GameEvent_EnemyActionResolved)ev).EnemyIndex);
			EnemyState enemy = ((idx >= 0 && idx < s.Enemies.Count) ? s.Enemies[idx] : null);
			if (enemy != null && enemy.Hp <= 0)
			{
				return s;
			}
			if (s.Player.Hp <= 0 && ev is GameEvent_EnemyActionExecuting)
			{
				return s;
			}
		}
		return ReactionSystems.Get(s.ReactionMode).OnEvent(s, ev);
	}
}
