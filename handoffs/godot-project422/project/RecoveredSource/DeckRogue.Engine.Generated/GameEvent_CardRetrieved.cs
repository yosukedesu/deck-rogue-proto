using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardRetrieved : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "CardRetrieved";

	public GameEvent_CardRetrieved()
	{
		Type = "CardRetrieved";
	}
}
