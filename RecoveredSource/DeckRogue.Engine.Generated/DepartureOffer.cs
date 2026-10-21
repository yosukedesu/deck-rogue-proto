using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record DepartureOffer
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("kind")]
	public string Kind { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("text")]
	public string Text { get; init; }

	[JsonProperty("choice")]
	public EventChoiceDef Choice { get; init; }

	[JsonProperty("price")]
	public int Price { get; init; }

	[JsonProperty("shopPrice", NullValueHandling = NullValueHandling.Ignore)]
	public int? ShopPrice { get; init; }
}
