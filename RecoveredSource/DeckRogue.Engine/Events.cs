using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public static class Events
{
	public static CombatPresentationReceipt PlayerPresentationReceipt(PlayerState before, PlayerState after)
	{
		return new CombatPresentationReceipt
		{
			Version = 1,
			Target = "player",
			Before = new CombatPresentationVitals
			{
				Hp = before.Hp,
				MaxHp = before.MaxHp,
				Block = before.Block,
				IceBlock = before.IceBlock
			},
			After = new CombatPresentationVitals
			{
				Hp = after.Hp,
				MaxHp = after.MaxHp,
				Block = after.Block,
				IceBlock = after.IceBlock
			}
		};
	}

	public static CombatPresentationReceipt EnemyPresentationReceipt(int enemyIndex, EnemyState before, EnemyState after)
	{
		return new CombatPresentationReceipt
		{
			Version = 1,
			Target = "enemy",
			EnemyIndex = enemyIndex,
			EnemyId = before.EnemyId,
			Before = new CombatPresentationVitals
			{
				Hp = before.Hp,
				MaxHp = before.MaxHp,
				Block = before.Block
			},
			After = new CombatPresentationVitals
			{
				Hp = after.Hp,
				MaxHp = after.MaxHp,
				Block = after.Block
			}
		};
	}

	public static GameState Emit(GameState state, GameEvent ev)
	{
		List<GameEvent> log = new List<GameEvent>(state.EventLog.Count + 1);
		log.AddRange(state.EventLog);
		log.Add(ev);
		return state with
		{
			EventLog = log
		};
	}
}
