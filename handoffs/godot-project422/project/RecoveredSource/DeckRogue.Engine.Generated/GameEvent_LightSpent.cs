using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_LightSpent : GameEvent
{
	[JsonProperty("amount")]
	public int Amount { get; init; }

	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "LightSpent";

	public GameEvent_LightSpent()
	{
		Type = "LightSpent";
	}
}
