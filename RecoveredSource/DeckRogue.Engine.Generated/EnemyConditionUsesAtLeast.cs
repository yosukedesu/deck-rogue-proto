using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyConditionUsesAtLeast
{
	[JsonProperty("move")]
	public string Move { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }
}
