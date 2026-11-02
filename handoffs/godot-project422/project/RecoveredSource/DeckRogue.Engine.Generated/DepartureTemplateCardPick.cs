using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record DepartureTemplateCardPick
{
	[JsonProperty("rarity")]
	public string Rarity { get; init; }

	[JsonProperty("count")]
	public int Count { get; init; }
}
