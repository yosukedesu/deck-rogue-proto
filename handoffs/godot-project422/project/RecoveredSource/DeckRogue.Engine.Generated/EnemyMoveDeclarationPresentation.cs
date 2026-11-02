using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyMoveDeclarationPresentation
{
	[JsonProperty("version")]
	public int Version { get; init; }

	[JsonProperty("eventIndex")]
	public int EventIndex { get; init; }

	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }

	[JsonProperty("origin")]
	public string Origin { get; init; }

	[JsonProperty("primaryMoveId", NullValueHandling = NullValueHandling.Ignore)]
	public string? PrimaryMoveId { get; init; }

	[JsonProperty("alternateMoveId", NullValueHandling = NullValueHandling.Ignore)]
	public string? AlternateMoveId { get; init; }

	[JsonProperty("primaryNodeId", NullValueHandling = NullValueHandling.Ignore)]
	public string? PrimaryNodeId { get; init; }
}
