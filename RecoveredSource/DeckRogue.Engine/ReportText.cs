using System;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public sealed class ReportText
{
	public Func<GameEvent, string?> LogLine = (GameEvent ev) => ev.Type;

	public Func<GameState, int, string> IntentText = (GameState st, int i) =>
	{
		EnemyIntent enemyIntent = Effects.EffectiveIntent(st, i);
		return (!(enemyIntent == null)) ? (enemyIntent.Kind + " " + enemyIntent.Actual + ((enemyIntent.Hits.HasValue && enemyIntent.Hits.Value > 1) ? ("×" + enemyIntent.Hits.Value) : "")) : "?";
	};
}
