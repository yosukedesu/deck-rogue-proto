using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyDef
{
	[JsonProperty("id")]
	public string Id { get; init; }

	[JsonProperty("name")]
	public string Name { get; init; }

	[JsonProperty("archetype")]
	public string Archetype { get; init; }

	[JsonProperty("flavor", NullValueHandling = NullValueHandling.Ignore)]
	public string? Flavor { get; init; }

	[JsonProperty("maxHp")]
	public int MaxHp { get; init; }

	[JsonProperty("hpRange", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<int>? HpRange { get; init; }

	[JsonProperty("moves")]
	public IReadOnlyList<EnemyMove> Moves { get; init; }

	[JsonProperty("nodes")]
	public IReadOnlyDictionary<string, EnemyNode> Nodes { get; init; }

	[JsonProperty("start")]
	public string Start { get; init; }

	[JsonProperty("startBySlot", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? StartBySlot { get; init; }

	[JsonProperty("interrupts", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<EnemyInterrupt>? Interrupts { get; init; }

	[JsonProperty("movesVsSet", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<EnemyRandomArm>? MovesVsSet { get; init; }

	[JsonProperty("movesVsTokens", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<EnemyRandomArm>? MovesVsTokens { get; init; }

	[JsonProperty("burnResist", NullValueHandling = NullValueHandling.Ignore)]
	public int? BurnResist { get; init; }

	[JsonProperty("thorns", NullValueHandling = NullValueHandling.Ignore)]
	public int? Thorns { get; init; }

	[JsonProperty("angerOnBlock", NullValueHandling = NullValueHandling.Ignore)]
	public int? AngerOnBlock { get; init; }

	[JsonProperty("regen", NullValueHandling = NullValueHandling.Ignore)]
	public int? Regen { get; init; }

	[JsonProperty("regenBreak", NullValueHandling = NullValueHandling.Ignore)]
	public int? RegenBreak { get; init; }

	[JsonProperty("enrage", NullValueHandling = NullValueHandling.Ignore)]
	public int? Enrage { get; init; }

	[JsonProperty("enrageEveryCards", NullValueHandling = NullValueHandling.Ignore)]
	public int? EnrageEveryCards { get; init; }

	[JsonProperty("enrageEveryDamage", NullValueHandling = NullValueHandling.Ignore)]
	public int? EnrageEveryDamage { get; init; }

	[JsonProperty("startingBlock", NullValueHandling = NullValueHandling.Ignore)]
	public int? StartingBlock { get; init; }

	[JsonProperty("splitInto", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyDefSplitInto? SplitInto { get; init; }

	[JsonProperty("guardian", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Guardian { get; init; }

	[JsonProperty("bondStrength", NullValueHandling = NullValueHandling.Ignore)]
	public int? BondStrength { get; init; }

	[JsonProperty("hatchInto", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyDefHatchInto? HatchInto { get; init; }

	[JsonProperty("mournStrength", NullValueHandling = NullValueHandling.Ignore)]
	public int? MournStrength { get; init; }

	[JsonProperty("turnArmor", NullValueHandling = NullValueHandling.Ignore)]
	public int? TurnArmor { get; init; }

	[JsonProperty("artifact", NullValueHandling = NullValueHandling.Ignore)]
	public int? Artifact { get; init; }

	[JsonProperty("burrow", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyDefBurrow? Burrow { get; init; }

	[JsonProperty("nemesis", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Nemesis { get; init; }

	[JsonProperty("imbalanced", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Imbalanced { get; init; }

	[JsonProperty("aura", NullValueHandling = NullValueHandling.Ignore)]
	public EnemyDefAura? Aura { get; init; }

	[JsonProperty("armor", NullValueHandling = NullValueHandling.Ignore)]
	public int? Armor { get; init; }

	[JsonProperty("slippery", NullValueHandling = NullValueHandling.Ignore)]
	public int? Slippery { get; init; }
}
