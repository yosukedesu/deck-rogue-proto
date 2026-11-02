using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunStateUnknownPity
{
	[JsonProperty("monster")]
	public int Monster { get; init; }

	[JsonProperty("shop")]
	public int Shop { get; init; }

	[JsonProperty("treasure")]
	public int Treasure { get; init; }
}
