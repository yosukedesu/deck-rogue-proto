using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_GrowthDischarged : GameEvent
{
	[JsonProperty("spent")]
	public int Spent { get; init; }

	public const string TypeTag = "GrowthDischarged";

	public GameEvent_GrowthDischarged()
	{
		Type = "GrowthDischarged";
	}
}
