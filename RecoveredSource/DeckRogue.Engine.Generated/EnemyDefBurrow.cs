using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyDefBurrow
{
	[JsonProperty("block")]
	public int Block { get; init; }

	[JsonProperty("bite")]
	public string Bite { get; init; }
}
