using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyIntentBranch
{
	[JsonProperty("kind")]
	public string Kind { get; init; }

	[JsonProperty("actual")]
	public int Actual { get; init; }

	[JsonProperty("base", NullValueHandling = NullValueHandling.Ignore)]
	public int? Base { get; init; }

	[JsonProperty("hits", NullValueHandling = NullValueHandling.Ignore)]
	public int? Hits { get; init; }

	[JsonProperty("inflict", NullValueHandling = NullValueHandling.Ignore)]
	public StatusInflict? Inflict { get; init; }

	[JsonProperty("alsoDefend", NullValueHandling = NullValueHandling.Ignore)]
	public int? AlsoDefend { get; init; }

	[JsonProperty("alsoBuff", NullValueHandling = NullValueHandling.Ignore)]
	public int? AlsoBuff { get; init; }

	[JsonProperty("alsoDestroySet", NullValueHandling = NullValueHandling.Ignore)]
	public bool? AlsoDestroySet { get; init; }
}
