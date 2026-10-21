using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardPlayed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "CardPlayed";

	public GameEvent_CardPlayed()
	{
		Type = "CardPlayed";
	}
}
