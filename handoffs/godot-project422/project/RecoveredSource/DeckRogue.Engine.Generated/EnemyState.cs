using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record EnemyState
{
	[JsonProperty("hp")]
	public int Hp { get; init; }

	[JsonProperty("maxHp")]
	public int MaxHp { get; init; }

	[JsonProperty("block")]
	public int Block { get; init; }

	[JsonProperty("enemyId")]
	public string EnemyId { get; init; }

	[JsonProperty("intent")]
	public EnemyIntent? Intent { get; init; }

	[JsonProperty("strength")]
	public int Strength { get; init; }

	[JsonProperty("burn")]
	public int Burn { get; init; }

	[JsonProperty("confusion")]
	public int Confusion { get; init; }

	[JsonProperty("exposed")]
	public int Exposed { get; init; }

	[JsonProperty("node")]
	public string Node { get; init; }

	[JsonProperty("lastMoves", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? LastMoves { get; init; }

	[JsonProperty("usedOnce", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? UsedOnce { get; init; }

	[JsonProperty("firedInterrupts", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<int>? FiredInterrupts { get; init; }

	[JsonProperty("intentMoveId", NullValueHandling = NullValueHandling.Ignore)]
	public string? IntentMoveId { get; init; }

	[JsonProperty("intentNode", NullValueHandling = NullValueHandling.Ignore)]
	public string? IntentNode { get; init; }

	[JsonProperty("mournProcessed", NullValueHandling = NullValueHandling.Ignore)]
	public bool? MournProcessed { get; init; }

	[JsonProperty("damageThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public int? DamageThisTurn { get; init; }

	[JsonProperty("artifact", NullValueHandling = NullValueHandling.Ignore)]
	public int? Artifact { get; init; }

	[JsonProperty("moveUses", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyDictionary<string, int>? MoveUses { get; init; }

	[JsonProperty("weak", NullValueHandling = NullValueHandling.Ignore)]
	public int? Weak { get; init; }

	[JsonProperty("burrowActive", NullValueHandling = NullValueHandling.Ignore)]
	public bool? BurrowActive { get; init; }

	[JsonProperty("biteNext", NullValueHandling = NullValueHandling.Ignore)]
	public bool? BiteNext { get; init; }

	[JsonProperty("staggeredNext", NullValueHandling = NullValueHandling.Ignore)]
	public bool? StaggeredNext { get; init; }

	[JsonProperty("actionNegated", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ActionNegated { get; init; }

	[JsonProperty("summonBlocked", NullValueHandling = NullValueHandling.Ignore)]
	public bool? SummonBlocked { get; init; }

	[JsonProperty("interruptBlocked", NullValueHandling = NullValueHandling.Ignore)]
	public bool? InterruptBlocked { get; init; }

	[JsonProperty("noReactTable", NullValueHandling = NullValueHandling.Ignore)]
	public bool? NoReactTable { get; init; }

	[JsonProperty("armor", NullValueHandling = NullValueHandling.Ignore)]
	public int? Armor { get; init; }

	[JsonProperty("slippery", NullValueHandling = NullValueHandling.Ignore)]
	public int? Slippery { get; init; }

	[JsonProperty("sealed", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<CardInstance>? Sealed { get; init; }

	[JsonProperty("atkScale", NullValueHandling = NullValueHandling.Ignore)]
	public double? AtkScale { get; init; }

	[JsonProperty("damageTakenTotal", NullValueHandling = NullValueHandling.Ignore)]
	public int? DamageTakenTotal { get; init; }

	[JsonProperty("hpLostSinceRegen", NullValueHandling = NullValueHandling.Ignore)]
	public int? HpLostSinceRegen { get; init; }

	[JsonProperty("thorns", NullValueHandling = NullValueHandling.Ignore)]
	public int? Thorns { get; init; }

	[JsonProperty("stolenGold", NullValueHandling = NullValueHandling.Ignore)]
	public int? StolenGold { get; init; }

	[JsonProperty("prevIntentKind", NullValueHandling = NullValueHandling.Ignore)]
	public string? PrevIntentKind { get; init; }

	[JsonProperty("fled", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Fled { get; init; }

	[JsonProperty("split", NullValueHandling = NullValueHandling.Ignore)]
	public bool? Split { get; init; }
}
