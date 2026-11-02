namespace DeckRogue.Engine;

public sealed record DamageBreakdownStep
{
	public string Label { get; init; }

	public int Value { get; init; }
}
