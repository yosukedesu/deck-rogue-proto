using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunStatePendingRelicChoice
{
	[JsonProperty("relicId")]
	public string RelicId { get; init; }

	[JsonProperty("mode")]
	public string Mode { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	[JsonProperty("resume")]
	public string Resume { get; init; }
}
