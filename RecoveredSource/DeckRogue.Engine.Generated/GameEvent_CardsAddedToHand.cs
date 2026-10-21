using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardsAddedToHand : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "CardsAddedToHand";

	public GameEvent_CardsAddedToHand()
	{
		Type = "CardsAddedToHand";
	}
}
