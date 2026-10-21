using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyNode
{
	[JsonProperty("move", NullValueHandling = NullValueHandling.Ignore)]
	public string? Move { get; init; }

	[JsonProperty("next", NullValueHandling = NullValueHandling.Ignore)]
	public string? Next { get; init; }

	[JsonProperty("random", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<EnemyRandomArm>? Random { get; init; }

	[JsonProperty("if", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyCondition? If { get; init; }

	[JsonProperty("then", NullValueHandling = NullValueHandling.Ignore)]
	public string? Then { get; init; }

	[JsonProperty("else", NullValueHandling = NullValueHandling.Ignore)]
	public string? Else { get; init; }
}
