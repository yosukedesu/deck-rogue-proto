using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyDefSplitInto
{
	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	[JsonProperty("stunned", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Stunned { get; init; }

	[JsonProperty("strength", NullValueHandling = NullValueHandling.Ignore)]
	public int? Strength { get; init; }
}
