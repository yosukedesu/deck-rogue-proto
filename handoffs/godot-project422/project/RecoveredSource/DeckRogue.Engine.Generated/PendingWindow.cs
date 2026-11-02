using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record PendingWindow
{
	[JsonProperty("enemyIndex")]
	public int EnemyIndex { get; init; }

	[JsonProperty("stage")]
	public string Stage { get; init; }
}
