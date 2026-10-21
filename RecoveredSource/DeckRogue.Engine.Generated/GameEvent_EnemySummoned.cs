using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemySummoned : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("into")]
	public string Into { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "EnemySummoned";

	public GameEvent_EnemySummoned()
	{
		Type = "EnemySummoned";
	}
}
