using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record StatusInflict
{
	[JsonProperty("status")]
	public string Status { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }
}
