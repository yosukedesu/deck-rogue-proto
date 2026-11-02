namespace DeckRogue.Engine;

public sealed record DifficultyTax
{
	public double BossHeal { get; init; }

	public double Campfire { get; init; }

	public int EliteBonus { get; init; }

	public double ShopPrice { get; init; }

	public int RewardChoices { get; init; }

	public int StartGuilt { get; init; }
}
