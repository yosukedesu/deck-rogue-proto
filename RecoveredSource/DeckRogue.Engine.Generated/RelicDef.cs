using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RelicDef
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("sprite")]
	public string Sprite { get; init; }

	[JsonProperty("description")]
	public string Description { get; init; }

	[JsonProperty("rarity", NullValueHandling = NullValueHandling.Ignore)]
	public string? Rarity { get; init; }

	[JsonProperty("eliteBossOnly", NullValueHandling = NullValueHandling.Ignore)]
	public bool? EliteBossOnly { get; init; }

	[JsonProperty("actMax", NullValueHandling = NullValueHandling.Ignore)]
	public int? ActMax { get; init; }

	[JsonProperty("actMin", NullValueHandling = NullValueHandling.Ignore)]
	public int? ActMin { get; init; }

	[JsonProperty("colors", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? Colors { get; init; }

	[JsonProperty("expiresAfterBattles", NullValueHandling = NullValueHandling.Ignore)]
	public int? ExpiresAfterBattles { get; init; }

	[JsonProperty("effects", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<DeclarativeEffect>? Effects { get; init; }

	[JsonProperty("bonus", NullValueHandling = NullValueHandling.Ignore)]
	public RelicDefBonus? Bonus { get; init; }

	[JsonProperty("combatRule", NullValueHandling = NullValueHandling.Ignore)]
	public RelicDefCombatRule? CombatRule { get; init; }
}
