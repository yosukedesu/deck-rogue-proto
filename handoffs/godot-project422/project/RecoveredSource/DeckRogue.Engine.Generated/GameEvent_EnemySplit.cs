using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_EnemySplit : GameEvent
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("into")]
	public string Into { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "EnemySplit";

	public GameEvent_EnemySplit()
	{
		Type = "EnemySplit";
	}
}
