using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyDied : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	public const string TypeTag = "EnemyDied";

	public GameEvent_EnemyDied()
	{
		Type = "EnemyDied";
	}
}
