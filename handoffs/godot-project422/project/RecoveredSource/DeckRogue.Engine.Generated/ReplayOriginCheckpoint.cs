using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record ReplayOriginCheckpoint
{
	[JsonProperty("act")]
	public int Act { get; init; }

	[JsonProperty("deckId")]
	public string DeckId { get; init; }

	[JsonProperty("relicIds", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? RelicIds { get; init; }

	[JsonProperty("hpRatio", NullValueHandling = NullValueHandling.Ignore)]
	public double? HpRatio { get; init; }

	[JsonProperty("gold", NullValueHandling = NullValueHandling.Ignore)]
	public int? Gold { get; init; }

	[JsonProperty("difficulty", NullValueHandling = NullValueHandling.Ignore)]
	public int? Difficulty { get; init; }
}
