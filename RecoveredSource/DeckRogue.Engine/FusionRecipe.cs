using DeckRogue.Engine.Generated;
using Newtonsoft.Json;

namespace DeckRogue.Engine;

public sealed record FusionRecipe
{
	[JsonProperty("a")]
	public string A { get; init; }

	[JsonProperty("b")]
	public string B { get; init; }

	[JsonProperty("result")]
	public CardDef Result { get; init; }
}
