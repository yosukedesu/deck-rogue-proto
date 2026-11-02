using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_RetainerLifeExtended : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("uid")]
	public string Uid { get; init; }

	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("persist", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Persist { get; init; }

	public const string TypeTag = "RetainerLifeExtended";

	public GameEvent_RetainerLifeExtended()
	{
		Type = "RetainerLifeExtended";
	}
}
