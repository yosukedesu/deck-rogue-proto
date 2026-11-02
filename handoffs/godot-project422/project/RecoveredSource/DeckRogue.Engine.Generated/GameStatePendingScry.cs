using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameStatePendingScry
{
	[JsonProperty("count")]
	public int Count { get; init; }

	[JsonProperty("times")]
	public int Times { get; init; }
}
