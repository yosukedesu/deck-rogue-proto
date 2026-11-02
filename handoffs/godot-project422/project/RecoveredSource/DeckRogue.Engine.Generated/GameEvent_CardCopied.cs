using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_CardCopied : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }

	public const string TypeTag = "CardCopied";

	public GameEvent_CardCopied()
	{
		Type = "CardCopied";
	}
}
