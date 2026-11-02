using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record ShopState
{
	[JsonProperty("cards")]
	public IReadOnlyList<ShopStateCards> Cards { get; init; }

	[JsonProperty("relicId")]
	public string? RelicId { get; init; }

	[JsonProperty("relicPrice")]
	public int RelicPrice { get; init; }

	[JsonProperty("gears", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<ShopStateGears>? Gears { get; init; }

	[JsonProperty("manaPrice", NullValueHandling = NullValueHandling.Ignore)]
	public int? ManaPrice { get; init; }

	[JsonProperty("departures", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<ShopStateDepartures>? Departures { get; init; }
}
