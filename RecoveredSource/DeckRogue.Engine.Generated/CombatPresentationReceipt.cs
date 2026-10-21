using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record CombatPresentationReceipt
{
	[JsonProperty("version")]
	public int Version { get; init; }

	[JsonProperty("target")]
	public string Target { get; init; }

	[JsonProperty("enemyIndex", NullValueHandling = NullValueHandling.Ignore)]
	public int? EnemyIndex { get; init; }

	[JsonProperty("enemyId", NullValueHandling = NullValueHandling.Ignore)]
	public string? EnemyId { get; init; }

	[JsonProperty("before")]
	public CombatPresentationVitals Before { get; init; }

	[JsonProperty("after")]
	public CombatPresentationVitals After { get; init; }
}
