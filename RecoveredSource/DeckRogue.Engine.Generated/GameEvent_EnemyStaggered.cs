using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyStaggered : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	public const string TypeTag = "EnemyStaggered";

	public GameEvent_EnemyStaggered()
	{
		Type = "EnemyStaggered";
	}
}
