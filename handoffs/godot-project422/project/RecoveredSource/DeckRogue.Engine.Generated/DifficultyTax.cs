using Newtonsoft.Json;

namespace DeckRogue.Engine.Generated;

public sealed record DifficultyTax
{
	[JsonProperty("bossHeal")]
	public int BossHeal { get; init; }

	[JsonProperty("campfire")]
	public int Campfire { get; init; }

	[JsonProperty("eliteBonus")]
	public int EliteBonus { get; init; }

	[JsonProperty("shopPrice")]
	public int ShopPrice { get; init; }

	[JsonProperty("rewardChoices")]
	public int RewardChoices { get; init; }

	[JsonProperty("startGuilt")]
	public int StartGuilt { get; init; }
}
