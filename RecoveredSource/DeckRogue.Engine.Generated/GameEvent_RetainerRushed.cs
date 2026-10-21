using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_RetainerRushed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "RetainerRushed";

	public GameEvent_RetainerRushed()
	{
		Type = "RetainerRushed";
	}
}
