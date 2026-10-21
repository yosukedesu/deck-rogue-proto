using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyActionPresentation
{
	[JsonProperty("version")]
	public int Version { get; init; }

	[JsonProperty("executionEventIndex")]
	public int ExecutionEventIndex { get; init; }

	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }

	[JsonProperty("declaredEventIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? DeclaredEventIndex { get; init; }

	[JsonProperty("moveId", NullValueHandling = NullValueHandling.Ignore)]
	public string? MoveId { get; init; }

	[JsonProperty("branch", NullValueHandling = NullValueHandling.Ignore)]
	public string? Branch { get; init; }

	[JsonProperty("origin", NullValueHandling = NullValueHandling.Ignore)]
	public string? Origin { get; init; }

	[JsonProperty("unresolvedReason", NullValueHandling = NullValueHandling.Ignore)]
	public string? UnresolvedReason { get; init; }
}
