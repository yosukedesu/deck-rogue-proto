using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameEvent_DeckCardTransformed : GameEvent
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("into")]
	public string Into { get; init; }

	public const string TypeTag = "DeckCardTransformed";

	public GameEvent_DeckCardTransformed()
	{
		Type = "DeckCardTransformed";
	}
}
