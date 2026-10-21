using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record DeckCardEntry
{
	[JsonProperty("cardId")]
	public string CardId { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }
}
