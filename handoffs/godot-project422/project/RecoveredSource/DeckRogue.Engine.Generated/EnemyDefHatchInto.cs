using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyDefHatchInto
{
	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }
}
