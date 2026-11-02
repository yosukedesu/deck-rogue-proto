using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_DiscountGained : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	public const string TypeTag = "DiscountGained";

	public GameEvent_DiscountGained()
	{
		Type = "DiscountGained";
	}
}
