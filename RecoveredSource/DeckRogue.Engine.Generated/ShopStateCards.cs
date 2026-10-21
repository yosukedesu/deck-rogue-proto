using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record ShopStateCards
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("price")]
	public int Price { get; init; }

	[JsonProperty("sold", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Sold { get; init; }
}
