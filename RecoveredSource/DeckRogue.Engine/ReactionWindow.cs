namespace DeckRogue.Engine;

public sealed record ReactionWindow
{
	public string Stage { get; init; }

	public string Kind { get; init; }

	public int Actual { get; init; }

	public int HpLoss { get; init; }

	public bool? Confused { get; init; }
}
