using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record CardDef
{
	[JsonProperty("rarity", NullValueHandling = NullValueHandling.Ignore)]
	public string? Rarity { get; init; }

	[JsonProperty("xCost", NullValueHandling = NullValueHandling.Ignore)]
	public bool? XCost { get; init; }

	[JsonProperty("xBonus", NullValueHandling = NullValueHandling.Ignore)]
	public int? XBonus { get; init; }

	[JsonProperty("blazeDiscount", NullValueHandling = NullValueHandling.Ignore)]
	public int? BlazeDiscount { get; init; }

	[JsonProperty("axis", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? Axis { get; init; }

	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("cost")]
	public int Cost { get; init; }

	[JsonProperty("type")]
	public string Type { get; init; }

	[JsonProperty("color")]
	public string Color { get; init; }

	[JsonProperty("effects")]
	public IReadOnlyList<DeclarativeEffect> Effects { get; init; }

	[JsonProperty("modes", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<CardMode>? Modes { get; init; }

	[JsonProperty("exhaust", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Exhaust { get; init; }

	[JsonProperty("retain", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Retain { get; init; }

	[JsonProperty("trapPersist", NullValueHandling = NullValueHandling.Ignore)]
	public bool? TrapPersist { get; init; }

	[JsonProperty("freeIfHandAllPhysical", NullValueHandling = NullValueHandling.Ignore)]
	public bool? FreeIfHandAllPhysical { get; init; }

	[JsonProperty("freeIfHandAll", NullValueHandling = NullValueHandling.Ignore)]
	public string? FreeIfHandAll { get; init; }

	[JsonProperty("requiresRetainer", NullValueHandling = NullValueHandling.Ignore)]
	public bool? RequiresRetainer { get; init; }

	[JsonProperty("freeIfMomentumAtLeast", NullValueHandling = NullValueHandling.Ignore)]
	public int? FreeIfMomentumAtLeast { get; init; }

	[JsonProperty("exhaustUnlessExposedEnemy", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ExhaustUnlessExposedEnemy { get; init; }

	[JsonProperty("necroCost", NullValueHandling = NullValueHandling.Ignore)]
	public int? NecroCost { get; init; }

	[JsonProperty("discardCost", NullValueHandling = NullValueHandling.Ignore)]
	public int? DiscardCost { get; init; }

	[JsonProperty("exhaustCost", NullValueHandling = NullValueHandling.Ignore)]
	public int? ExhaustCost { get; init; }

	[JsonProperty("impulseCost", NullValueHandling = NullValueHandling.Ignore)]
	public int? ImpulseCost { get; init; }

	[JsonProperty("kindleOptional", NullValueHandling = NullValueHandling.Ignore)]
	public bool? KindleOptional { get; init; }

	[JsonProperty("lightCost", NullValueHandling = NullValueHandling.Ignore)]
	public int? LightCost { get; init; }

	[JsonProperty("retainer", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Retainer { get; init; }

	[JsonProperty("life", NullValueHandling = NullValueHandling.Ignore)]
	public int? Life { get; init; }

	[JsonProperty("lifePersist", NullValueHandling = NullValueHandling.Ignore)]
	public bool? LifePersist { get; init; }

	[JsonProperty("shivToken", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ShivToken { get; init; }

	[JsonProperty("sparkToken", NullValueHandling = NullValueHandling.Ignore)]
	public bool? SparkToken { get; init; }

	[JsonProperty("fusionCatalyst", NullValueHandling = NullValueHandling.Ignore)]
	public string? FusionCatalyst { get; init; }

	[JsonProperty("echo", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Echo { get; init; }

	[JsonProperty("cascade", NullValueHandling = NullValueHandling.Ignore)]
	public int? Cascade { get; init; }
}
