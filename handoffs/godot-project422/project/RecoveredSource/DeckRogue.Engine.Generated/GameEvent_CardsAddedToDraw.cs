using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardsAddedToDraw : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "CardsAddedToDraw";

	public GameEvent_CardsAddedToDraw()
	{
		Type = "CardsAddedToDraw";
	}
}
