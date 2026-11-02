using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyInterrupt
{
	[JsonProperty("on")]
	public string On { get; init; }

	[JsonProperty("amount", NullValueHandling = NullValueHandling.Ignore)]
	public int? Amount { get; init; }

	[JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? From { get; init; }

	[JsonProperty("goto")]
	public string Goto { get; init; }
}
