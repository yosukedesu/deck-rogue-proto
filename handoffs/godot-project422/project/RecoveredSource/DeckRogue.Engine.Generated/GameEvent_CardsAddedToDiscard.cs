using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardsAddedToDiscard : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "CardsAddedToDiscard";

	public GameEvent_CardsAddedToDiscard()
	{
		Type = "CardsAddedToDiscard";
	}
}
