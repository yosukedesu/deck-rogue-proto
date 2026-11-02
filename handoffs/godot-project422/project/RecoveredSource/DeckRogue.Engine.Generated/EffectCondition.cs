using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EffectCondition
{
	[JsonProperty("hpAtOrBelowRatio", NullValueHandling = NullValueHandling.Ignore)]
	public double? HpAtOrBelowRatio { get; init; }

	[JsonProperty("minDamageTaken", NullValueHandling = NullValueHandling.Ignore)]
	public int? MinDamageTaken { get; init; }

	[JsonProperty("maxActionValue", NullValueHandling = NullValueHandling.Ignore)]
	public int? MaxActionValue { get; init; }

	[JsonProperty("minActionValue", NullValueHandling = NullValueHandling.Ignore)]
	public int? MinActionValue { get; init; }

	[JsonProperty("minGrowth", NullValueHandling = NullValueHandling.Ignore)]
	public int? MinGrowth { get; init; }

	[JsonProperty("minMomentum", NullValueHandling = NullValueHandling.Ignore)]
	public int? MinMomentum { get; init; }

	[JsonProperty("minLight", NullValueHandling = NullValueHandling.Ignore)]
	public int? MinLight { get; init; }

	[JsonProperty("minEnergyMax", NullValueHandling = NullValueHandling.Ignore)]
	public int? MinEnergyMax { get; init; }

	[JsonProperty("blaze", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Blaze { get; init; }

	[JsonProperty("kindled", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Kindled { get; init; }

	[JsonProperty("enemyIntent", NullValueHandling = NullValueHandling.Ignore)]
	public string? EnemyIntent { get; init; }

	[JsonProperty("enemyIntentNot", NullValueHandling = NullValueHandling.Ignore)]
	public string? EnemyIntentNot { get; init; }

	[JsonProperty("enemyExposed", NullValueHandling = NullValueHandling.Ignore)]
	public bool? EnemyExposed { get; init; }

	[JsonProperty("perfectBlockLastPhase", NullValueHandling = NullValueHandling.Ignore)]
	public bool? PerfectBlockLastPhase { get; init; }

	[JsonProperty("targetDead", NullValueHandling = NullValueHandling.Ignore)]
	public bool? TargetDead { get; init; }

	[JsonProperty("actionKinds", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? ActionKinds { get; init; }

	[JsonProperty("actionKindsNot", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? ActionKindsNot { get; init; }

	[JsonProperty("lastActionNoHpLoss", NullValueHandling = NullValueHandling.Ignore)]
	public bool? LastActionNoHpLoss { get; init; }

	[JsonProperty("perfectBlockThisPhase", NullValueHandling = NullValueHandling.Ignore)]
	public bool? PerfectBlockThisPhase { get; init; }

	[JsonProperty("targetAlive", NullValueHandling = NullValueHandling.Ignore)]
	public bool? TargetAlive { get; init; }

	[JsonProperty("healedThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public bool? HealedThisTurn { get; init; }

	[JsonProperty("turn", NullValueHandling = NullValueHandling.Ignore)]
	public int? Turn { get; init; }

	[JsonProperty("blockZero", NullValueHandling = NullValueHandling.Ignore)]
	public bool? BlockZero { get; init; }

	[JsonProperty("noAttackThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public bool? NoAttackThisTurn { get; init; }

	[JsonProperty("maxPlaysThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? MaxPlaysThisTurn { get; init; }
}
