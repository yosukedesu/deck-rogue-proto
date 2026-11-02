using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record GameState
{
	[JsonProperty("rng")]
	public RngState Rng { get; init; }

	[JsonProperty("reactionMode")]
	public string ReactionMode { get; init; }

	[JsonProperty("phase")]
	public string Phase { get; init; }

	[JsonProperty("turn")]
	public int Turn { get; init; }

	[JsonProperty("player")]
	public PlayerState Player { get; init; }

	[JsonProperty("enemies")]
	public IReadOnlyList<EnemyState> Enemies { get; init; }

	[JsonProperty("pendingWindow")]
	public PendingWindow? PendingWindow { get; init; }

	[JsonProperty("innateResolving", NullValueHandling = NullValueHandling.Ignore)]
	public bool? InnateResolving { get; init; }

	[JsonProperty("resolvingCardPlay", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ResolvingCardPlay { get; init; }

	[JsonProperty("resolvingKindled", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ResolvingKindled { get; init; }

	[JsonProperty("resolvingGainTrigger", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ResolvingGainTrigger { get; init; }

	[JsonProperty("suppressLightGain", NullValueHandling = NullValueHandling.Ignore)]
	public bool? SuppressLightGain { get; init; }

	[JsonProperty("nextRetainerTwin", NullValueHandling = NullValueHandling.Ignore)]
	public int? NextRetainerTwin { get; init; }

	[JsonProperty("summonSeq", NullValueHandling = NullValueHandling.Ignore)]
	public int? SummonSeq { get; init; }

	[JsonProperty("chosenPermanentUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? ChosenPermanentUid { get; init; }

	[JsonProperty("hearthSparks", NullValueHandling = NullValueHandling.Ignore)]
	public int? HearthSparks { get; init; }

	[JsonProperty("negateNextAction")]
	public bool NegateNextAction { get; init; }

	[JsonProperty("enemyPhase", NullValueHandling = NullValueHandling.Ignore)]
	public bool? EnemyPhase { get; init; }

	[JsonProperty("reactionUsedThisAction")]
	public bool ReactionUsedThisAction { get; init; }

	[JsonProperty("lastAction")]
	public GameStateLastAction? LastAction { get; init; }

	[JsonProperty("angerFiredThisPlay", NullValueHandling = NullValueHandling.Ignore)]
	public bool? AngerFiredThisPlay { get; init; }

	[JsonProperty("lastEnteredPermanentUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? LastEnteredPermanentUid { get; init; }

	[JsonProperty("resolvingPermanentUid", NullValueHandling = NullValueHandling.Ignore)]
	public string? ResolvingPermanentUid { get; init; }

	[JsonProperty("eventLog")]
	public IReadOnlyList<GameEvent> EventLog { get; init; }

	[JsonProperty("setDamageReduction", NullValueHandling = NullValueHandling.Ignore)]
	public int? SetDamageReduction { get; init; }

	[JsonProperty("bellLocked", NullValueHandling = NullValueHandling.Ignore)]
	public bool? BellLocked { get; init; }

	[JsonProperty("goldAvailable", NullValueHandling = NullValueHandling.Ignore)]
	public int? GoldAvailable { get; init; }

	[JsonProperty("setAnyCards", NullValueHandling = NullValueHandling.Ignore)]
	public bool? SetAnyCards { get; init; }

	[JsonProperty("expireToHand", NullValueHandling = NullValueHandling.Ignore)]
	public bool? ExpireToHand { get; init; }

	[JsonProperty("pendingPhaseEffects", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<GameStatePendingPhaseEffects>? PendingPhaseEffects { get; init; }

	[JsonProperty("pendingScry", NullValueHandling = NullValueHandling.Ignore)]
	public GameStatePendingScry? PendingScry { get; init; }

	[JsonProperty("retainUids", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? RetainUids { get; init; }

	[JsonProperty("resolvingExposedAtStart", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<int>? ResolvingExposedAtStart { get; init; }

	[JsonProperty("energyMaxRefBonus", NullValueHandling = NullValueHandling.Ignore)]
	public int? EnergyMaxRefBonus { get; init; }

	[JsonProperty("harvestKeep", NullValueHandling = NullValueHandling.Ignore)]
	public int? HarvestKeep { get; init; }

	[JsonProperty("nextTurnDraw", NullValueHandling = NullValueHandling.Ignore)]
	public int? NextTurnDraw { get; init; }

	[JsonProperty("nextTurnEnergy", NullValueHandling = NullValueHandling.Ignore)]
	public int? NextTurnEnergy { get; init; }

	[JsonProperty("nextTurnLight", NullValueHandling = NullValueHandling.Ignore)]
	public int? NextTurnLight { get; init; }

	[JsonProperty("nextTurnBlock", NullValueHandling = NullValueHandling.Ignore)]
	public int? NextTurnBlock { get; init; }

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

	[JsonProperty("maxHpLossFiredThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public bool? MaxHpLossFiredThisTurn { get; init; }

	[JsonProperty("deathSave", NullValueHandling = NullValueHandling.Ignore)]
	public bool? DeathSave { get; init; }

	[JsonProperty("deathSaveUsed", NullValueHandling = NullValueHandling.Ignore)]
	public bool? DeathSaveUsed { get; init; }

	[JsonProperty("playCap", NullValueHandling = NullValueHandling.Ignore)]
	public int? PlayCap { get; init; }

	[JsonProperty("hideIntents", NullValueHandling = NullValueHandling.Ignore)]
	public bool? HideIntents { get; init; }

	[JsonProperty("brandsPlayable", NullValueHandling = NullValueHandling.Ignore)]
	public bool? BrandsPlayable { get; init; }

	[JsonProperty("gearUsedThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public bool? GearUsedThisTurn { get; init; }

	[JsonProperty("retainHandThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public bool? RetainHandThisTurn { get; init; }

	[JsonProperty("energyCarryThisTurn", NullValueHandling = NullValueHandling.Ignore)]
	public bool? EnergyCarryThisTurn { get; init; }

	[JsonProperty("gearDeathSave", NullValueHandling = NullValueHandling.Ignore)]
	public bool? GearDeathSave { get; init; }

	[JsonProperty("nullifyNextAttack", NullValueHandling = NullValueHandling.Ignore)]
	public bool? NullifyNextAttack { get; init; }
}
