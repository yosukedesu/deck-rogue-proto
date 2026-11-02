using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardExhausted : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	public const string TypeTag = "CardExhausted";

	public GameEvent_CardExhausted()
	{
		Type = "CardExhausted";
	}
}
