using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RngState
{
	[JsonProperty("seed")]
	public long Seed { get; init; }

	[JsonProperty("counter")]
	public long Counter { get; init; }
}
