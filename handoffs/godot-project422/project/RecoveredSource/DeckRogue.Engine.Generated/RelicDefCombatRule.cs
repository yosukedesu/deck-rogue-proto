using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RelicDefCombatRule
{
	[JsonProperty("setDamageReduction", NullValueHandling = NullValueHandling.Ignore)]
	public int? SetDamageReduction { get; init; }

	[JsonProperty("expireToHand", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ExpireToHand { get; init; }

	[JsonProperty("energyMaxRefBonus", NullValueHandling = NullValueHandling.Ignore)]
	public int? EnergyMaxRefBonus { get; init; }

	[JsonProperty("harvestKeep", NullValueHandling = NullValueHandling.Ignore)]
	public int? HarvestKeep { get; init; }

	[JsonProperty("retainHand", NullValueHandling = NullValueHandling.Ignore)]
	public bool? RetainHand { get; init; }

	[JsonProperty("energyCarry", NullValueHandling = NullValueHandling.Ignore)]
	public bool? EnergyCarry { get; init; }

	[JsonProperty("blockKeep", NullValueHandling = NullValueHandling.Ignore)]
	public int? BlockKeep { get; init; }

	[JsonProperty("xBonus", NullValueHandling = NullValueHandling.Ignore)]
	public int? XBonus { get; init; }

	[JsonProperty("hpLossReduce", NullValueHandling = NullValueHandling.Ignore)]
	public int? HpLossReduce { get; init; }

	[JsonProperty("smallHitToOne", NullValueHandling = NullValueHandling.Ignore)]
	public int? SmallHitToOne { get; init; }

	[JsonProperty("maxHpLossPerTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? MaxHpLossPerTurn { get; init; }

	[JsonProperty("maxHpLossCharges", NullValueHandling = NullValueHandling.Ignore)]
	public int? MaxHpLossCharges { get; init; }

	[JsonProperty("deathSave", NullValueHandling = NullValueHandling.Ignore)]
	public bool? DeathSave { get; init; }

	[JsonProperty("playCap", NullValueHandling = NullValueHandling.Ignore)]
	public int? PlayCap { get; init; }

	[JsonProperty("hideIntents", NullValueHandling = NullValueHandling.Ignore)]
	public bool? HideIntents { get; init; }

	[JsonProperty("brandsPlayable", NullValueHandling = NullValueHandling.Ignore)]
	public bool? BrandsPlayable { get; init; }

	[JsonProperty("artifact", NullValueHandling = NullValueHandling.Ignore)]
	public int? Artifact { get; init; }
}
