using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardGrew : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("bonus")]
	public int Bonus { get; init; }

	public const string TypeTag = "CardGrew";

	public GameEvent_CardGrew()
	{
		Type = "CardGrew";
	}
}
