using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_GrowthAdded : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "GrowthAdded";

	public GameEvent_GrowthAdded()
	{
		Type = "GrowthAdded";
	}
}
