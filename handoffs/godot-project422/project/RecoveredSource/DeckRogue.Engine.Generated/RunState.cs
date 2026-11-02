using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RunState
{
	[JsonProperty("seed")]
	public int Seed { get; init; }

	[JsonProperty("mode")]
	public string Mode { get; init; }

	[JsonProperty("leaderId")]
	public string LeaderId { get; init; }

	[JsonProperty("colors")]
	public IReadOnlyList<string> Colors { get; init; }

	[JsonProperty("difficulty")]
	public int Difficulty { get; init; }

	[JsonProperty("setAnyCards", NullValueHandling = NullValueHandling.Ignore)]
	public bool? SetAnyCards { get; init; }

	[JsonProperty("rng")]
	public RngState Rng { get; init; }

	[JsonProperty("deck")]
	public IReadOnlyList<CardInstance> Deck { get; init; }

	[JsonProperty("hp")]
	public int Hp { get; init; }

	[JsonProperty("maxHp")]
	public int MaxHp { get; init; }

	[JsonProperty("act")]
	public int Act { get; init; }

	[JsonProperty("map")]
	public IReadOnlyList<IReadOnlyList<MapNode>> Map { get; init; }

	[JsonProperty("row")]
	public int Row { get; init; }

	[JsonProperty("col")]
	public int Col { get; init; }

	[JsonProperty("battlesWon")]
	public int BattlesWon { get; init; }

	[JsonProperty("gold")]
	public int Gold { get; init; }

	[JsonProperty("removalCount")]
	public int RemovalCount { get; init; }

	[JsonProperty("upgradeCount")]
	public int UpgradeCount { get; init; }

	[JsonProperty("shop")]
	public ShopState? Shop { get; init; }

	[JsonProperty("phase")]
	public string Phase { get; init; }

	[JsonProperty("combat")]
	public GameState? Combat { get; init; }

	[JsonProperty("rewardOptions")]
	public IReadOnlyList<string>? RewardOptions { get; init; }

	[JsonProperty("picks")]
	public IReadOnlyList<string> Picks { get; init; }

	[JsonProperty("relics")]
	public IReadOnlyList<string> Relics { get; init; }

	[JsonProperty("relicQueue")]
	public IReadOnlyList<string> RelicQueue { get; init; }

	[JsonProperty("relicOptions")]
	public IReadOnlyList<string>? RelicOptions { get; init; }

	[JsonProperty("relicPicksLeft", NullValueHandling = NullValueHandling.Ignore)]
	public int? RelicPicksLeft { get; init; }

	[JsonProperty("currentElite")]
	public bool CurrentElite { get; init; }

	[JsonProperty("victoryHealBonus")]
	public int VictoryHealBonus { get; init; }

	[JsonProperty("rewardChoicesBonus")]
	public int RewardChoicesBonus { get; init; }

	[JsonProperty("campfireRatio")]
	public double CampfireRatio { get; init; }

	[JsonProperty("goldPerVictoryBonus")]
	public int GoldPerVictoryBonus { get; init; }

	[JsonProperty("campfireForgeBonus")]
	public int CampfireForgeBonus { get; init; }

	[JsonProperty("campfireUpgradesUsed")]
	public int CampfireUpgradesUsed { get; init; }

	[JsonProperty("forgeBonusUsedAct", NullValueHandling = NullValueHandling.Ignore)]
	public int? ForgeBonusUsedAct { get; init; }

	[JsonProperty("unknownPity")]
	public RunStateUnknownPity UnknownPity { get; init; }

	[JsonProperty("lastRoomWasShop")]
	public bool LastRoomWasShop { get; init; }

	[JsonProperty("eventId")]
	public string? EventId { get; init; }

	[JsonProperty("seenEventIds")]
	public IReadOnlyList<string> SeenEventIds { get; init; }

	[JsonProperty("seenShrineIds")]
	public IReadOnlyList<string> SeenShrineIds { get; init; }

	[JsonProperty("relicState", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyDictionary<string, int>? RelicState { get; init; }

	[JsonProperty("rewardRoundsLeft", NullValueHandling = NullValueHandling.Ignore)]
	public int? RewardRoundsLeft { get; init; }

	[JsonProperty("pendingRelicChoice", NullValueHandling = NullValueHandling.Ignore)]
	public RunStatePendingRelicChoice? PendingRelicChoice { get; init; }

	[JsonProperty("workshopFusesUsed", NullValueHandling = NullValueHandling.Ignore)]
	public int? WorkshopFusesUsed { get; init; }

	[JsonProperty("gears", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<GearInstance>? Gears { get; init; }

	[JsonProperty("mana", NullValueHandling = NullValueHandling.Ignore)]
	public int? Mana { get; init; }

	[JsonProperty("gearPity", NullValueHandling = NullValueHandling.Ignore)]
	public int? GearPity { get; init; }

	[JsonProperty("seenGearIds", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? SeenGearIds { get; init; }

	[JsonProperty("gearOption", NullValueHandling = NullValueHandling.Ignore)]
	public string? GearOption { get; init; }

	[JsonProperty("departure", NullValueHandling = NullValueHandling.Ignore)]
	public RunStateDeparture? Departure { get; init; }
}
