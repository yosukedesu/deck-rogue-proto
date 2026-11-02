using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyAttackPresentation
{
	[JsonProperty("version")]
	public int Version { get; init; }

	[JsonProperty("resolution")]
	public string Resolution { get; init; }

	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }

	[JsonProperty("effectiveActual")]
	public int EffectiveActual { get; init; }

	[JsonProperty("loopBound")]
	public int LoopBound { get; init; }

	[JsonProperty("executedHits")]
	public int ExecutedHits { get; init; }

	[JsonProperty("mirrorHits", NullValueHandling = NullValueHandling.Ignore)]
	public bool? MirrorHits { get; init; }

	[JsonProperty("contributions", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<EnemyAttackContribution>? Contributions { get; init; }

	[JsonProperty("contributionsOmittedReason", NullValueHandling = NullValueHandling.Ignore)]
	public string? ContributionsOmittedReason { get; init; }
}
