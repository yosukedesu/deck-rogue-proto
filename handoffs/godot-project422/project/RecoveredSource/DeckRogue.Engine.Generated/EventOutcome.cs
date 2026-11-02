using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EventOutcome
{
	[JsonProperty("gold", NullValueHandling = NullValueHandling.Ignore)]
	public int? Gold { get; init; }

	[JsonProperty("hp", NullValueHandling = NullValueHandling.Ignore)]
	public int? Hp { get; init; }

	[JsonProperty("wounds", NullValueHandling = NullValueHandling.Ignore)]
	public int? Wounds { get; init; }
}
