using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GearDef
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("rarity")]
	public string Rarity { get; init; }

	[JsonProperty("family")]
	public string Family { get; init; }

	[JsonProperty("charges", NullValueHandling = NullValueHandling.Ignore)]
	public int? Charges { get; init; }

	[JsonProperty("text")]
	public string Text { get; init; }

	[JsonProperty("needsTarget", NullValueHandling = NullValueHandling.Ignore)]
	public bool? NeedsTarget { get; init; }

	[JsonProperty("needsCard", NullValueHandling = NullValueHandling.Ignore)]
	public string? NeedsCard { get; init; }

	[JsonProperty("special", NullValueHandling = NullValueHandling.Ignore)]
	public string? Special { get; init; }

	[JsonProperty("retired", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Retired { get; init; }

	[JsonProperty("effects")]
	public IReadOnlyList<DeclarativeEffect> Effects { get; init; }
}
