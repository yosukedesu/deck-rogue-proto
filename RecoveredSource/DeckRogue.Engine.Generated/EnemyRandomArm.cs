using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyRandomArm
{
	[JsonProperty("to")]
	public string To { get; init; }

	[JsonProperty("weight")]
	public int Weight { get; init; }

	[JsonProperty("noRepeat", NullValueHandling = NullValueHandling.Ignore)]
	public bool? NoRepeat { get; init; }

	[JsonProperty("once", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Once { get; init; }

	[JsonProperty("maxRepeat", NullValueHandling = NullValueHandling.Ignore)]
	public int? MaxRepeat { get; init; }
}
