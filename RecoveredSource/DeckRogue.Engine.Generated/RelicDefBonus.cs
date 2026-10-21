using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record RelicDefBonus
{
	[JsonProperty("maxHp", NullValueHandling = NullValueHandling.Ignore)]
	public int? MaxHp { get; init; }

	[JsonProperty("victoryHeal", NullValueHandling = NullValueHandling.Ignore)]
	public int? VictoryHeal { get; init; }

	[JsonProperty("rewardChoices", NullValueHandling = NullValueHandling.Ignore)]
	public int? RewardChoices { get; init; }

	[JsonProperty("campfireRatio", NullValueHandling = NullValueHandling.Ignore)]
	public double? CampfireRatio { get; init; }

	[JsonProperty("goldPerVictory", NullValueHandling = NullValueHandling.Ignore)]
	public int? GoldPerVictory { get; init; }

	[JsonProperty("campfireForge", NullValueHandling = NullValueHandling.Ignore)]
	public int? CampfireForge { get; init; }

	[JsonProperty("goldOnPickup", NullValueHandling = NullValueHandling.Ignore)]
	public int? GoldOnPickup { get; init; }

	[JsonProperty("upgradeRandomOnPickup", NullValueHandling = NullValueHandling.Ignore)]
	public int? UpgradeRandomOnPickup { get; init; }

	[JsonProperty("eliteRelicPicks", NullValueHandling = NullValueHandling.Ignore)]
	public int? EliteRelicPicks { get; init; }

	[JsonProperty("noRest", NullValueHandling = NullValueHandling.Ignore)]
	public bool? NoRest { get; init; }

	[JsonProperty("brandOnRelic", NullValueHandling = NullValueHandling.Ignore)]
	public int? BrandOnRelic { get; init; }

	[JsonProperty("victoryHealFlat", NullValueHandling = NullValueHandling.Ignore)]
	public int? VictoryHealFlat { get; init; }

	[JsonProperty("shopUpgradeDiscount", NullValueHandling = NullValueHandling.Ignore)]
	public int? ShopUpgradeDiscount { get; init; }

	[JsonProperty("restMaxHp", NullValueHandling = NullValueHandling.Ignore)]
	public int? RestMaxHp { get; init; }

	[JsonProperty("eliteGoldBonus", NullValueHandling = NullValueHandling.Ignore)]
	public int? EliteGoldBonus { get; init; }

	[JsonProperty("fusionDiscount", NullValueHandling = NullValueHandling.Ignore)]
	public int? FusionDiscount { get; init; }

	[JsonProperty("shopPriceRatio", NullValueHandling = NullValueHandling.Ignore)]
	public double? ShopPriceRatio { get; init; }

	[JsonProperty("goldMultiplier", NullValueHandling = NullValueHandling.Ignore)]
	public double? GoldMultiplier { get; init; }

	[JsonProperty("removalStepDelta", NullValueHandling = NullValueHandling.Ignore)]
	public int? RemovalStepDelta { get; init; }

	[JsonProperty("goldPerRow", NullValueHandling = NullValueHandling.Ignore)]
	public int? GoldPerRow { get; init; }

	[JsonProperty("goldPerUnknown", NullValueHandling = NullValueHandling.Ignore)]
	public int? GoldPerUnknown { get; init; }

	[JsonProperty("unknownChestEvery", NullValueHandling = NullValueHandling.Ignore)]
	public int? UnknownChestEvery { get; init; }

	[JsonProperty("shopHeal", NullValueHandling = NullValueHandling.Ignore)]
	public int? ShopHeal { get; init; }

	[JsonProperty("campfireDig", NullValueHandling = NullValueHandling.Ignore)]
	public bool? CampfireDig { get; init; }

	[JsonProperty("campfireRemove", NullValueHandling = NullValueHandling.Ignore)]
	public bool? CampfireRemove { get; init; }

	[JsonProperty("campfireTrain", NullValueHandling = NullValueHandling.Ignore)]
	public int? CampfireTrain { get; init; }

	[JsonProperty("wingBoots", NullValueHandling = NullValueHandling.Ignore)]
	public int? WingBoots { get; init; }

	[JsonProperty("extraRewardRounds", NullValueHandling = NullValueHandling.Ignore)]
	public int? ExtraRewardRounds { get; init; }

	[JsonProperty("skipRewardMaxHp", NullValueHandling = NullValueHandling.Ignore)]
	public int? SkipRewardMaxHp { get; init; }

	[JsonProperty("upgradeOnAdd", NullValueHandling = NullValueHandling.Ignore)]
	public IReadOnlyList<string>? UpgradeOnAdd { get; init; }

	[JsonProperty("healFullOnPickup", NullValueHandling = NullValueHandling.Ignore)]
	public bool? HealFullOnPickup { get; init; }

	[JsonProperty("relicsOnPickup", NullValueHandling = NullValueHandling.Ignore)]
	public int? RelicsOnPickup { get; init; }

	[JsonProperty("brandsOnPickup", NullValueHandling = NullValueHandling.Ignore)]
	public int? BrandsOnPickup { get; init; }

	[JsonProperty("removeOnPickup", NullValueHandling = NullValueHandling.Ignore)]
	public int? RemoveOnPickup { get; init; }

	[JsonProperty("transformOnPickup", NullValueHandling = NullValueHandling.Ignore)]
	public int? TransformOnPickup { get; init; }

	[JsonProperty("transformBasicsOnPickup", NullValueHandling = NullValueHandling.Ignore)]
	public bool? TransformBasicsOnPickup { get; init; }

	[JsonProperty("noForge", NullValueHandling = NullValueHandling.Ignore)]
	public bool? NoForge { get; init; }

	[JsonProperty("workshopFuses", NullValueHandling = NullValueHandling.Ignore)]
	public int? WorkshopFuses { get; init; }

	[JsonProperty("fusionUpgraded", NullValueHandling = NullValueHandling.Ignore)]
	public bool? FusionUpgraded { get; init; }

	[JsonProperty("maxHpPerBrand", NullValueHandling = NullValueHandling.Ignore)]
	public int? MaxHpPerBrand { get; init; }

	[JsonProperty("brandWard", NullValueHandling = NullValueHandling.Ignore)]
	public int? BrandWard { get; init; }
}
