using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceHudProjection
{
	public static RunState Ordinary(RunState current, GameState? display)
	{
		GameState now = current.Combat;
		if ((object)now == null || (object)display == null)
		{
			return current;
		}
		PlayerState player = now.Player with
		{
			Hp = display.Player.Hp,
			MaxHp = display.Player.MaxHp,
			Block = display.Player.Block,
			IceBlock = display.Player.IceBlock,
			Permanents = display.Player.Permanents
		};
		List<EnemyState> enemies = display.Enemies.Select((EnemyState enemy, int index) => (index >= now.Enemies.Count || !(now.Enemies[index].EnemyId == enemy.EnemyId)) ? enemy : now.Enemies[index]with
		{
			Hp = enemy.Hp,
			MaxHp = enemy.MaxHp,
			Block = enemy.Block
		}).ToList();
		return current with
		{
			Combat = now with
			{
				Player = player,
				Enemies = enemies
			}
		};
	}
}
