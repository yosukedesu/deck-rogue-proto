using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_LightDischarged : GameEvent
{
	[JsonProperty("spent")]
	public int Spent { get; init; }

	[JsonProperty("sparks", NullValueHandling = NullValueHandling.Ignore)]
	public int? Sparks { get; init; }

	[JsonProperty("paid", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Paid { get; init; }

	public const string TypeTag = "LightDischarged";

	public GameEvent_LightDischarged()
	{
		Type = "LightDischarged";
	}
}
