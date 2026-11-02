using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemyFled : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	public const string TypeTag = "EnemyFled";

	public GameEvent_EnemyFled()
	{
		Type = "EnemyFled";
	}
}
