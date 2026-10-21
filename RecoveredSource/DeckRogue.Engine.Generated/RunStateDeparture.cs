using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunStateDeparture
{
	[JsonProperty("offers")]
	public IReadOnlyList<DepartureOffer> Offers { get; init; }

	[JsonProperty("bought")]
	public IReadOnlyList<string> Bought { get; init; }

	[JsonProperty("leftovers")]
	public IReadOnlyList<DepartureOffer> Leftovers { get; init; }
}
