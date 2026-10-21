using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record DeckDef
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("description")]
	public string Description { get; init; }

	[JsonProperty("color")]
	public string Color { get; init; }

	[JsonProperty("cards")]
	public IReadOnlyList<DeckCardEntry> Cards { get; init; }
}
