using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record MapNode
{
	[JsonProperty("type")]
	public string Type { get; init; }

	[JsonProperty("encounterId")]
	public string? EncounterId { get; init; }

	[JsonProperty("next")]
	public IReadOnlyList<int> Next { get; init; }

	[JsonProperty("col", NullValueHandling = NullValueHandling.Ignore)]
	public int? Col { get; init; }
}
