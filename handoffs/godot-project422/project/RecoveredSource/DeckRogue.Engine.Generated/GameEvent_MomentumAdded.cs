using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_MomentumAdded : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "MomentumAdded";

	public GameEvent_MomentumAdded()
	{
		Type = "MomentumAdded";
	}
}
