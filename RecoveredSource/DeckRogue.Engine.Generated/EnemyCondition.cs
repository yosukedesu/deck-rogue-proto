using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyCondition
{
	[JsonProperty("hpBelowHalf", NullValueHandling = NullValueHandling.Ignore)]
	public bool? HpBelowHalf { get; init; }

	[JsonProperty("alone", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Alone { get; init; }

	[JsonProperty("allyAlive", NullValueHandling = NullValueHandling.Ignore)]
	public bool? AllyAlive { get; init; }

	[JsonProperty("usesAtLeast", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyConditionUsesAtLeast? UsesAtLeast { get; init; }

	[JsonProperty("damageTakenAtLeast", NullValueHandling = NullValueHandling.Ignore)]
	public int? DamageTakenAtLeast { get; init; }

	[JsonProperty("turnParity", NullValueHandling = NullValueHandling.Ignore)]
	public string? TurnParity { get; init; }

	[JsonProperty("alliesFewerThan", NullValueHandling = NullValueHandling.Ignore)]
	public int? AlliesFewerThan { get; init; }
}
