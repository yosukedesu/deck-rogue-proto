using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyMove
{
	[JsonProperty("growPerUse", NullValueHandling = NullValueHandling.Ignore)]
	public int? GrowPerUse { get; init; }

	[JsonProperty("growHitsPerUse", NullValueHandling = NullValueHandling.Ignore)]
	public int? GrowHitsPerUse { get; init; }

	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("kind")]
	public string Kind { get; init; }

	[JsonProperty("min", NullValueHandling = NullValueHandling.Ignore)]
	public int? Min { get; init; }

	[JsonProperty("max", NullValueHandling = NullValueHandling.Ignore)]
	public int? Max { get; init; }

	[JsonProperty("hits", NullValueHandling = NullValueHandling.Ignore)]
	public int? Hits { get; init; }

	[JsonProperty("mirrorHits", NullValueHandling = NullValueHandling.Ignore)]
	public bool? MirrorHits { get; init; }

	[JsonProperty("inflict", NullValueHandling = NullValueHandling.Ignore)]
	public StatusInflict? Inflict { get; init; }

	[JsonProperty("alsoDefend", NullValueHandling = NullValueHandling.Ignore)]
	public int? AlsoDefend { get; init; }

	[JsonProperty("alsoBuff", NullValueHandling = NullValueHandling.Ignore)]
	public int? AlsoBuff { get; init; }

	[JsonProperty("alsoDestroySet", NullValueHandling = NullValueHandling.Ignore)]
	public bool? AlsoDestroySet { get; init; }

	[JsonProperty("strengthPerMilled", NullValueHandling = NullValueHandling.Ignore)]
	public int? StrengthPerMilled { get; init; }

	[JsonProperty("summon", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyMoveSummon? Summon { get; init; }

	[JsonProperty("damageFromPlayerHp", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyMoveDamageFromPlayerHp? DamageFromPlayerHp { get; init; }
}
