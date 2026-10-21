using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardPlayedFromExhaust : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "CardPlayedFromExhaust";

	public GameEvent_CardPlayedFromExhaust()
	{
		Type = "CardPlayedFromExhaust";
	}
}
