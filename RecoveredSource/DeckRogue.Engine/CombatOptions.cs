using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine;

public sealed record CombatOptions
{
	public IReadOnlyList<CardInstance> Deck { get; init; }

	public string LeaderId { get; init; }

	public int? PlayerHp { get; init; }

	public int? PlayerMaxHp { get; init; }

	public double? EnemyHpScale { get; init; }

	public int? EnemyStrength { get; init; }

	public double? EnemyAtkScale { get; init; }

	public IReadOnlyList<CardInstance> RelicPermanents { get; init; }

	public int? SetDamageReduction { get; init; }

	public int? GoldAvailable { get; init; }

	public bool? SetAnyCards { get; init; }

	public bool? ExpireToHand { get; init; }

	public int? EnergyMaxRefBonus { get; init; }

	public int? HarvestKeep { get; init; }

	public bool? RetainHand { get; init; }

	public bool? EnergyCarry { get; init; }

	public int? BlockKeep { get; init; }

	public int? XBonus { get; init; }

	public int? HpLossReduce { get; init; }

	public int? SmallHitToOne { get; init; }

	public int? MaxHpLossPerTurn { get; init; }

	public int? MaxHpLossCharges { get; init; }

	public bool? DeathSave { get; init; }

	public int? PlayCap { get; init; }

	public bool? HideIntents { get; init; }

	public bool? BrandsPlayable { get; init; }

	public int? Artifact { get; init; }

	public int? StartGrowth { get; init; }
}
